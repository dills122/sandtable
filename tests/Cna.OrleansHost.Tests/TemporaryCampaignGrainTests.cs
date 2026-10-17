using System.Net;
using System.Net.Sockets;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed class TemporaryCampaignGrainTests
{
    [Fact]
    public async Task InProcessHostMatchesDirectCoreAndDeduplicatesConcurrentRequests()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            Assert.Equal(0, host.Services.GetRequiredService<IOptions<EndpointOptions>>()
                .Value.GatewayPort);
            var grain = GetTrustedGrain(host);
            var directStart = CampaignHostAdapter.Create(Request());
            var direct = directStart.Session!;
            var creation = await grain.CreateAsync("operator-1", "create-1");
            Assert.Equal(TemporaryCampaignStatus.Created, creation.Status);
            Assert.Equal(directStart.CreationEventBytes, creation.Event);
            Assert.Equal(directStart.InitialSnapshotBytes, creation.Snapshot);
            Assert.Equal(creation.Event, (await grain.CreateAsync("operator-1", "create-1")).Event);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await grain.CreateAsync("operator-2", "create-1")).Status);
            var forged = new byte[32];
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.CreateAsync(forged, "operator-1", "create-2")).Status);
            var privateRead = await grain.Inner.QueryAsync(forged, CampaignActionAudience.Axis);
            Assert.Equal(TemporaryCampaignStatus.Rejected, privateRead.Status);
            Assert.Null(privateRead.View);
            Assert.Null(privateRead.LegalActions);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.AdvanceSystemAsync(forged, 1)).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.SubmitAsync(forged, "host-system", "forged",
                    CampaignActionAudience.System, [])).Status);

            for (var index = 0; index < 4; index++)
            {
                foreach (var side in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
                {
                    var hosted = await grain.QueryAsync(side);
                    var expected = CampaignHostAdapter.QueryAudience(direct, side);
                    Assert.Equal(TemporaryCampaignStatus.Current, hosted.Status);
                    Assert.Equal(expected.Observation!.CampaignId, hosted.View!.CampaignId);
                    Assert.Equal(expected.Observation.StateVersion, hosted.View.StateVersion);
                    Assert.Equal(expected.Observation.Position.PositionId, hosted.View.PositionId);
                    Assert.Equal(expected.Observation.Observer.ToString(), hosted.View.Observer);
                    Assert.Equal(expected.Observation.OwnElements.Count, hosted.View.OwnElementCount);
                    Assert.Equal(expected.Observation.ApparentOpposingPresences.Count,
                        hosted.View.ApparentOpposingPresenceCount);
                    Assert.Equal(CampaignLegalActionSerializer.Serialize(expected.LegalActions!),
                        hosted.LegalActions);
                }
                var sets = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(direct, audience).ActionSet!).ToArray();
                Assert.Equal(CampaignLegalActionSerializer.Serialize(sets[0]),
                    (await grain.QueryAsync(CampaignActionAudience.System)).LegalActions);
                var chosen = sets.First(value => value.Candidates.Count > 0);
                var candidate = chosen.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                    ?? chosen.Candidates[0];
                var command = new CampaignActionSubmission(1, chosen.CampaignId,
                    chosen.StateVersion, chosen.PositionId, chosen.Audience, candidate.ActionId);
                var bytes = CampaignActionSubmissionSerializer.SerializeCanonical(command);
                if (chosen.Audience == CampaignActionAudience.System)
                    Assert.Equal(TemporaryCampaignStatus.Rejected,
                        (await grain.SubmitAsync("seat-1", $"impersonate-{index}",
                            CampaignActionAudience.System, bytes)).Status);
                var directStep = CampaignHostAdapter.Submit(direct, command);
                Assert.True(directStep.IsAccepted);
                var actor = chosen.Audience == CampaignActionAudience.System
                    ? "host-system" : "seat-1";
                var id = $"request-{index}";
                var replies = await Task.WhenAll(
                    grain.SubmitAsync(actor, id, chosen.Audience, bytes),
                    grain.SubmitAsync(actor, id, chosen.Audience, bytes));
                Assert.All(replies, reply => Assert.Equal(TemporaryCampaignStatus.Accepted, reply.Status));
                Assert.Equal(replies[0].Receipt, replies[1].Receipt);
                Assert.Equal(directStep.Evidence!.EventRecords.Single(), replies[0].Event);
                Assert.Equal(directStep.Evidence.SnapshotCheckpoint, replies[0].Snapshot);
                Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(directStep.Evidence.Receipt),
                    replies[0].Receipt);
                Assert.Equal(replies[0].Receipt,
                    (await grain.FindRequestAsync(actor, id, chosen.Audience)).Receipt);
                Assert.Equal(TemporaryCampaignStatus.Conflict,
                    (await grain.SubmitAsync("different-seat", id, chosen.Audience, bytes)).Status);
                Assert.Equal(TemporaryCampaignStatus.Conflict,
                    (await grain.SubmitAsync(actor, id,
                        chosen.Audience == CampaignActionAudience.System
                            ? CampaignActionAudience.Axis : CampaignActionAudience.System,
                        bytes)).Status);
                Assert.Equal(TemporaryCampaignStatus.Conflict,
                    (await grain.SubmitAsync(actor, id, chosen.Audience,
                        CampaignActionSubmissionSerializer.SerializeCanonical(command with
                        { ActionId = "sha256:" + new string('0', 64) }))).Status);
                var beforeStale = await grain.QueryAsync(CampaignActionAudience.Axis);
                Assert.Equal(TemporaryCampaignStatus.Rejected,
                    (await grain.SubmitAsync(actor, $"fresh-stale-{index}", chosen.Audience,
                        bytes)).Status);
                var afterStale = await grain.QueryAsync(CampaignActionAudience.Axis);
                Assert.Equal(beforeStale.LegalActions, afterStale.LegalActions);
                Assert.Equal(beforeStale.View, afterStale.View);
                if (index == 0)
                {
                    var priorActivation = await grain.ActivationIdAsync();
                    await grain.DeactivateAsync();
                    Assert.NotEqual(priorActivation, await grain.ActivationIdAsync());
                    Assert.Equal(replies[0].Receipt,
                        (await grain.FindRequestAsync(actor, id, chosen.Audience)).Receipt);
                    Assert.Equal(beforeStale.LegalActions,
                        (await grain.QueryAsync(CampaignActionAudience.Axis)).LegalActions);
                }
                direct = directStep.SuccessorSession!;
            }
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task SystemProgressionStopsForHumanAndRestartLosesTemporaryCampaign()
    {
        var port = FreePort();
        using (var firstHost = BuildHost(port))
        {
            await firstHost.StartAsync(TestContext.Current.CancellationToken);
            try
            {
                var grain = GetTrustedGrain(firstHost);
                await grain.CreateAsync("operator-1", "create-1");
                Assert.Equal(TemporaryCampaignStatus.Rejected,
                    (await grain.AdvanceSystemAsync(0)).Status);
                Assert.Equal(TemporaryCampaignStatus.StepLimit,
                    (await grain.AdvanceSystemAsync(1)).Status);
                var progress = await grain.AdvanceSystemAsync(8);
                Assert.Equal(TemporaryCampaignStatus.NeedsHuman, progress.Status);
                Assert.Equal(2, progress.Steps);
                Assert.Equal(TemporaryCampaignStatus.Missing,
                    (await grain.FindRequestAsync("seat-1", "absent", CampaignActionAudience.Axis)).Status);
            }
            finally { await firstHost.StopAsync(TestContext.Current.CancellationToken); }
        }
        using var secondHost = BuildHost(port);
        await secondHost.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(secondHost);
            Assert.Equal(TemporaryCampaignStatus.Missing,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).Status);
            Assert.Equal(TemporaryCampaignStatus.Missing,
                (await grain.FindRequestAsync("host-system", "system-1",
                    CampaignActionAudience.System)).Status);
        }
        finally { await secondHost.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task TwelveStepTraceStopsAtUnsupportedCombatWithoutGuessingTerminal()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            await grain.CreateAsync("operator-1", "create-1");
            var direct = CampaignHostAdapter.Create(Request()).Session!;
            for (var index = 0; index < 12; index++)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(direct, audience).ActionSet!)
                    .First(value => value.Candidates.Count > 0);
                var candidate = set.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                    ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-reserve-designation")
                    ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-movement-segment")
                    ?? set.Candidates[0];
                var command = new CampaignActionSubmission(1, set.CampaignId,
                    set.StateVersion, set.PositionId, set.Audience, candidate.ActionId);
                var directStep = CampaignHostAdapter.Submit(direct, command);
                Assert.True(directStep.IsAccepted);
                var actor = set.Audience == CampaignActionAudience.System
                    ? "host-system" : "seat-1";
                var hosted = await grain.SubmitAsync(actor, $"trace-{index}", set.Audience,
                    CampaignActionSubmissionSerializer.SerializeCanonical(command));
                Assert.Equal(TemporaryCampaignStatus.Accepted, hosted.Status);
                Assert.Equal(directStep.Evidence!.EventRecords.Single(), hosted.Event);
                Assert.Equal(CampaignActionAcceptanceReceiptSerializer.Serialize(directStep.Evidence.Receipt),
                    hosted.Receipt);
                Assert.Equal(directStep.Evidence.SnapshotCheckpoint, hosted.Snapshot);
                direct = directStep.SuccessorSession!;
                foreach (var side in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
                {
                    var expected = CampaignHostAdapter.QueryAudience(direct, side);
                    var actual = await grain.QueryAsync(side);
                    Assert.Equal(CampaignLegalActionSerializer.Serialize(expected.LegalActions!),
                        actual.LegalActions);
                    Assert.Equal(expected.Observation!.StateVersion, actual.View!.StateVersion);
                }
                Assert.Equal(CampaignLegalActionSerializer.Serialize(
                        CampaignHostAdapter.QuerySystem(direct).ActionSet!),
                    (await grain.QueryAsync(CampaignActionAudience.System)).LegalActions);
            }
            Assert.Equal(TemporaryCampaignStatus.UnsupportedPhase,
                (await grain.AdvanceSystemAsync(4)).Status);
            Assert.Equal(13, CampaignExercises.QueryCheckpoint(direct).StateVersion);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    private static IHost BuildHost(int? port = null)
    {
        var siloPort = port ?? FreePort();
        return new HostBuilder().UseOrleans(silo => silo
            .UseTemporaryCampaignDevelopmentSilo(siloPort)).Build();
    }

    private static TrustedGrain GetTrustedGrain(IHost host) => new(
        host.Services.GetRequiredService<IGrainFactory>()
            .GetGrain<ITemporaryCampaignGrain>("host-rsh-001"),
        host.Services.GetRequiredService<TemporaryCampaignAccess>().Capability);

    private sealed class TrustedGrain(ITemporaryCampaignGrain inner, byte[] capability)
    {
        internal ITemporaryCampaignGrain Inner => inner;
        internal Task<TemporaryCampaignReply> CreateAsync(string actor, string id) =>
            inner.CreateAsync(capability, actor, id);
        internal Task<TemporaryCampaignReply> QueryAsync(CampaignActionAudience audience) =>
            inner.QueryAsync(capability, audience);
        internal Task<TemporaryCampaignReply> SubmitAsync(string actor, string id,
            CampaignActionAudience audience, byte[] command) =>
            inner.SubmitAsync(capability, actor, id, audience, command);
        internal Task<TemporaryCampaignReply> FindRequestAsync(string actor, string id,
            CampaignActionAudience audience) =>
            inner.FindRequestAsync(capability, actor, id, audience);
        internal Task<TemporaryCampaignReply> AdvanceSystemAsync(int budget) =>
            inner.AdvanceSystemAsync(capability, budget);
        internal Task<string> ActivationIdAsync() => inner.ActivationIdForTestAsync(capability);
        internal Task DeactivateAsync() => inner.DeactivateForTestAsync(capability);
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static CampaignCreationRequest Request() => new(1, "host-rsh-001",
        Cna1979Ruleset.Manifest.Hash, 12345, "rules-lab.breakdown.truck.v1",
        "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
        "rules-lab.content.breakdown-truck.v1",
        "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee",
        "breakdown-truck-lab");
}
