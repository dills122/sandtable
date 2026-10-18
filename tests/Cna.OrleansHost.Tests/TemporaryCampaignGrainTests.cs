using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed class TemporaryCampaignGrainTests
{
    private const string Generation = "11111111-1111-4111-8111-111111111111";
    private const string OtherGeneration = "22222222-2222-4222-8222-222222222222";
    [Fact]
    public void PrivateTrackerAdvancesBothSeatsAcrossUnreadVisibilityLoss()
    {
        const string campaign = "test-campaign";
        const string pin = "source-hash";
        var tracker = TemporaryAudienceTracker.Create(campaign, pin,
            "scenario", "setup-pin", "content-pin",
            Side(CampaignActionAudience.Axis, 1, "private-axis-contact"),
            Side(CampaignActionAudience.Commonwealth, 1, "private-commonwealth-contact"));
        Assert.NotNull(tracker);
        var firstAxis = tracker.Read(CampaignActionAudience.Axis, 1, Generation)!;
        var firstCommonwealth = tracker.Read(CampaignActionAudience.Commonwealth, 1, Generation)!;
        Assert.NotEqual(firstAxis.Contacts[0].ContactId,
            firstCommonwealth.Contacts[0].ContactId);
        Assert.DoesNotContain("private-axis-contact", firstAxis.Contacts[0].ContactId);

        Assert.True(tracker.Advance(
            Side(CampaignActionAudience.Axis, 2, "private-axis-contact"),
            Side(CampaignActionAudience.Commonwealth, 2, "private-commonwealth-contact")));
        var hiddenOnly = tracker.Read(CampaignActionAudience.Axis, 2, Generation)!;
        Assert.Equal(firstAxis.AudienceRevision, hiddenOnly.AudienceRevision);
        Assert.Equal(firstAxis.ActionSetToken, hiddenOnly.ActionSetToken);
        Assert.Equal(firstAxis.Contacts[0].ContactId, hiddenOnly.Contacts[0].ContactId);

        // No read between absence and reappearance; continuity follows committed cuts.
        Assert.True(tracker.Advance(Side(CampaignActionAudience.Axis, 3),
            Side(CampaignActionAudience.Commonwealth, 3, "private-commonwealth-contact")));
        Assert.True(tracker.Advance(
            Side(CampaignActionAudience.Axis, 4, "private-axis-contact"),
            Side(CampaignActionAudience.Commonwealth, 4, "private-commonwealth-contact")));
        var returned = tracker.Read(CampaignActionAudience.Axis, 4, Generation)!;
        Assert.NotEqual(firstAxis.Contacts[0].ContactId, returned.Contacts[0].ContactId);
        Assert.Equal(firstAxis.AudienceRevision + 2, returned.AudienceRevision);
        Assert.Equal(firstCommonwealth.AudienceRevision,
            tracker.Read(CampaignActionAudience.Commonwealth, 4, Generation)!.AudienceRevision);
        Assert.NotEqual(returned.Contacts[0].ContactId,
            tracker.Read(CampaignActionAudience.Axis, 4, OtherGeneration)!.Contacts[0].ContactId);
        Assert.Null(tracker.Read(CampaignActionAudience.System, 4, Generation));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 3, Generation));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 4, "not-a-session-generation"));
        Assert.False(tracker.Advance(Side(CampaignActionAudience.Axis, 6),
            Side(CampaignActionAudience.Commonwealth, 6)));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 4, Generation));
    }

    [Fact]
    public void PrivateTrackerRejectsIncoherentSourceWithoutPublishingEitherSide()
    {
        Assert.Null(TemporaryAudienceTracker.Create("test-campaign", "source-hash",
            "wrong-scenario", "setup-pin", "content-pin",
            Side(CampaignActionAudience.Axis, 1),
            Side(CampaignActionAudience.Commonwealth, 1)));
        var axis = Side(CampaignActionAudience.Axis, 1);
        var commonwealth = Side(CampaignActionAudience.Commonwealth, 1);
        foreach (var mismatched in new[]
        {
            commonwealth with { GameTurn = 2 },
            commonwealth with { PhaseId = "other-phase" },
            commonwealth with { StageId = "other-stage" },
        })
        {
            Assert.Null(TemporaryAudienceTracker.Create("test-campaign", "source-hash",
                "scenario", "setup-pin", "content-pin", axis, mismatched));
        }
        var tracker = TemporaryAudienceTracker.Create("test-campaign", "source-hash",
            "scenario", "setup-pin", "content-pin",
            Side(CampaignActionAudience.Axis, 1),
            Side(CampaignActionAudience.Commonwealth, 1))!;
        Assert.False(tracker.Advance(Side(CampaignActionAudience.Axis, 2),
            Side(CampaignActionAudience.Axis, 2)));
        Assert.Null(tracker.Read(CampaignActionAudience.Axis, 1, Generation));
        Assert.Null(tracker.Read(CampaignActionAudience.Commonwealth, 1, Generation));
    }

    [Fact]
    public void PrivateActionHandleSurvivesHiddenOnlyCoreIdRemap()
    {
        var axis = Side(CampaignActionAudience.Axis, 1);
        var commonwealth = Side(CampaignActionAudience.Commonwealth, 1);
        var tracker = TemporaryAudienceTracker.Create("test-campaign", "source-hash",
            "scenario", "setup-pin", "content-pin", axis, commonwealth)!;
        var original = tracker.Read(CampaignActionAudience.Axis, 1, Generation)!;
        Assert.True(tracker.Advance(axis with
        {
            StateVersion = 2,
            Actions = [new CampaignHostPrivateAction("new-core-action", "action")],
        }, commonwealth with { StateVersion = 2 }));
        var next = tracker.Read(CampaignActionAudience.Axis, 2, Generation)!;
        Assert.Equal(original.ActionSetToken, next.ActionSetToken);
        Assert.Equal(original.Actions[0].OutwardActionId, next.Actions[0].OutwardActionId);
        Assert.Equal("new-core-action", tracker.Resolve(CampaignActionAudience.Axis,
            2, Generation, original.ActionSetToken, original.Actions[0].OutwardActionId));
        Assert.Null(tracker.Resolve(CampaignActionAudience.Commonwealth,
            2, Generation, original.ActionSetToken, original.Actions[0].OutwardActionId));
    }

    [Fact]
    public async Task PrivateOutwardIngressDeduplicatesBeforeRemapAndAuthorizesRetainedGeneration()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            Assert.Equal(TemporaryCampaignStatus.Created,
                (await grain.CreateAsync("operator-1", "create-1")).Status);
            Assert.Equal(TemporaryCampaignStatus.StepLimit,
                (await grain.AdvanceSystemAsync(3)).Status);
            var sessions = new LocalSessionStore(TimeProvider.System);
            var operatorSession = sessions.Bootstrap(null)!;
            var grants = sessions.IssueGrants(operatorSession.OperatorId,
                TemporaryCampaignGrain.FixtureCampaignId);
            var axis = (await sessions.RedeemAsync(operatorSession,
                TemporaryCampaignGrain.FixtureCampaignId, grants.Axis))!;
            var commonwealthOperator = sessions.Bootstrap(null)!;
            var otherGrants = sessions.IssueGrants(commonwealthOperator.OperatorId,
                TemporaryCampaignGrain.FixtureCampaignId);
            var commonwealth = (await sessions.RedeemAsync(commonwealthOperator,
                TemporaryCampaignGrain.FixtureCampaignId, otherGrants.Commonwealth))!;
            var ingress = new TemporaryOutwardActionIngress(sessions,
                host.Services.GetRequiredService<IGrainFactory>(),
                host.Services.GetRequiredService<TemporaryCampaignAccess>());
            var cut = (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, axis.Generation!)).Cut!;
            var request = new TemporaryOutwardActionRequest(1, cut.CampaignId,
                Guid.NewGuid().ToString("D"), axis.Generation!, cut.PositionId,
                cut.Actions.Single(a => a.Kind == "act-first").OutwardActionId,
                1, cut.ActionSetToken);

            var replies = await Task.WhenAll(ingress.SubmitAsync(axis.Token, request),
                ingress.SubmitAsync(axis.Token, request));
            Assert.All(replies, reply => Assert.Equal(TemporaryCampaignStatus.Accepted,
                reply.Status));
            Assert.Equal(replies[0], replies[1]);
            Assert.Equal(cut.StateVersion + 1,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View!.StateVersion);
            Assert.Equal(replies[0], await ingress.FindRequestAsync(axis.Token,
                cut.CampaignId, request.RequestId));
            Assert.Equal(replies[0], await ingress.SubmitAsync(axis.Token, request));
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await ingress.SubmitAsync(axis.Token, request with
                {
                    ActionId = Guid.NewGuid().ToString("D"),
                })).Status);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await ingress.SubmitAsync(commonwealth.Token, request with
                {
                    SessionGeneration = commonwealth.Generation!,
                })).Status);
            Assert.Equal(TemporaryCampaignStatus.Conflict,
                (await ingress.FindRequestAsync(commonwealth.Token,
                    cut.CampaignId, request.RequestId)).Status);

            var beforeStale = await grain.QueryAsync(CampaignActionAudience.Axis);
            var stale = request with { RequestId = Guid.NewGuid().ToString("D") };
            var staleReply = await ingress.SubmitAsync(axis.Token, stale);
            Assert.Equal(TemporaryCampaignStatus.Rejected, staleReply.Status);
            Assert.Equal("stale-view", staleReply.Reason);
            Assert.Equal(staleReply, await ingress.SubmitAsync(axis.Token, stale));
            var current = (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, axis.Generation!)).Cut!;
            var forged = request with
            {
                RequestId = Guid.NewGuid().ToString("D"),
                ExpectedPositionId = current.PositionId,
                ActionSetToken = current.ActionSetToken,
                ActionId = Guid.NewGuid().ToString("D"),
            };
            var forgedReply = await ingress.SubmitAsync(axis.Token, forged);
            Assert.Equal(TemporaryCampaignStatus.Rejected, forgedReply.Status);
            Assert.Equal("invalid-action", forgedReply.Reason);
            Assert.Equal(beforeStale.View,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View);
            Assert.Equal(beforeStale.LegalActions,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).LegalActions);
            Assert.True(await sessions.RelinquishAsync(axis, cut.CampaignId));
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await ingress.FindRequestAsync(axis.Token,
                    cut.CampaignId, request.RequestId)).Status);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task SeatRelinquishmentWaitsForAdmittedActionCommit()
    {
        using var host = BuildHost();
        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var grain = GetTrustedGrain(host);
            await grain.CreateAsync("operator-1", "create-1");
            await grain.AdvanceSystemAsync(3);
            var sessions = new LocalSessionStore(TimeProvider.System);
            var operatorSession = sessions.Bootstrap(null)!;
            var grants = sessions.IssueGrants(operatorSession.OperatorId,
                TemporaryCampaignGrain.FixtureCampaignId);
            var axis = (await sessions.RedeemAsync(operatorSession,
                TemporaryCampaignGrain.FixtureCampaignId, grants.Axis))!;
            var cut = (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, axis.Generation!)).Cut!;
            var request = new TemporaryOutwardActionRequest(1, cut.CampaignId,
                Guid.NewGuid().ToString("D"), axis.Generation!, cut.PositionId,
                cut.Actions.Single(action => action.Kind == "act-first").OutwardActionId,
                1, cut.ActionSetToken);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var inFlight = sessions.WithAuthorizedSeatAsync(axis.Token, cut.CampaignId,
                async session =>
                {
                    entered.SetResult();
                    await release.Task;
                    return await grain.Inner.SubmitOutwardAsync(grain.Capability,
                        new TemporaryAuthenticatedSeat(session.OperatorId, cut.CampaignId,
                            CampaignActionAudience.Axis, session.Generation!), request);
                }, new TemporaryOutwardActionReply(TemporaryCampaignStatus.Rejected, "unauthorized"));
            await entered.Task;
            var relinquish = sessions.RelinquishAsync(axis, cut.CampaignId);
            Assert.False(relinquish.IsCompleted);
            var lateAdmission = sessions.WithAuthorizedSeatAsync(axis.Token,
                cut.CampaignId, _ => Task.FromResult(true), false);
            Assert.Equal(cut.StateVersion,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View!.StateVersion);
            release.SetResult();
            Assert.Equal(TemporaryCampaignStatus.Accepted, (await inFlight).Status);
            Assert.True(await relinquish);
            Assert.False(await lateAdmission);
            Assert.Equal(cut.StateVersion + 1,
                (await grain.QueryAsync(CampaignActionAudience.Axis)).View!.StateVersion);
            var ingress = new TemporaryOutwardActionIngress(sessions,
                host.Services.GetRequiredService<IGrainFactory>(),
                host.Services.GetRequiredService<TemporaryCampaignAccess>());
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await ingress.FindRequestAsync(axis.Token, cut.CampaignId,
                    request.RequestId)).Status);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await ingress.SubmitAsync(axis.Token, request with
                {
                    RequestId = Guid.NewGuid().ToString("D"),
                })).Status);
        }
        finally { await host.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task SeatHandoffWaitsForAdmittedAction()
    {
        var sessions = new LocalSessionStore(TimeProvider.System);
        var operatorSession = sessions.Bootstrap(null)!;
        var grants = sessions.IssueGrants(operatorSession.OperatorId,
            TemporaryCampaignGrain.FixtureCampaignId);
        var axis = (await sessions.RedeemAsync(operatorSession,
            TemporaryCampaignGrain.FixtureCampaignId, grants.Axis))!;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inFlight = sessions.WithAuthorizedSeatAsync(axis.Token, axis.CampaignId!,
            async _ =>
            {
                entered.SetResult();
                await release.Task;
                return true;
            }, false);
        await entered.Task;
        var handoff = sessions.RedeemAsync(axis, axis.CampaignId!, grants.Commonwealth);
        Assert.False(handoff.IsCompleted);
        release.SetResult();
        Assert.True(await inFlight);
        var next = await handoff;
        Assert.Equal("commonwealth", next!.Seat);
        Assert.Null(sessions.Find(axis.Token));
        Assert.NotEqual(axis.Generation, next.Generation);
    }

    private static CampaignHostPrivateSideCut Side(CampaignActionAudience side, long version,
        string? representation = null) => new("test-campaign", version, "source-hash",
        "scenario", "position", 1, "phase", "stage", side,
        [new CampaignHostPrivateLocation("location", "terrain")],
        [new CampaignHostPrivateOwnElement($"own-{side}", $"formation-{side}", "location", "none")],
        representation is null ? [] : [new CampaignHostPrivateContact(representation, "location", false)],
        [new CampaignHostPrivateAction("core-action", "action")], "normal");

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
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.Inner.QueryPrivateSideAsync(forged, CampaignActionAudience.Axis,
                    Generation)).Status);
            Assert.Equal(TemporaryCampaignStatus.Unavailable,
                (await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                    CampaignActionAudience.System, Generation)).Status);
            var axisPrivate = await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Axis, Generation);
            var commonwealthPrivate = await grain.Inner.QueryPrivateSideAsync(grain.Capability,
                CampaignActionAudience.Commonwealth, Generation);
            Assert.Equal(TemporaryCampaignStatus.Current, axisPrivate.Status);
            Assert.Equal(TemporaryCampaignStatus.Current, commonwealthPrivate.Status);
            Assert.Equal(axisPrivate.Cut!.StateVersion, commonwealthPrivate.Cut!.StateVersion);
            Assert.Equal(axisPrivate.Cut.PositionId, commonwealthPrivate.Cut.PositionId);
            Assert.Equal(axisPrivate.Cut.SourcePin, commonwealthPrivate.Cut.SourcePin);
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
                    var privateCut = (await grain.QueryPrivateSideAsync(side)).Cut!;
                    Assert.Equal(expected.Observation.OwnElements.Select(e => e.ElementId),
                        privateCut.OwnElements.Select(e => e.ElementId));
                    Assert.Equal(expected.Observation.ApparentOpposingPresences
                            .Select(p => p.RepresentationId),
                        privateCut.Contacts.Select(c => c.RepresentationId));
                    Assert.Equal(expected.LegalActions!.Candidates.Select(a => a.ActionId),
                        privateCut.Actions.Select(a => a.CoreActionId));
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
                    var beforePrivate = (await grain.QueryPrivateSideAsync(
                        CampaignActionAudience.Axis)).Cut!;
                    var priorActivation = await grain.ActivationIdAsync();
                    await grain.DeactivateAsync();
                    Assert.NotEqual(priorActivation, await grain.ActivationIdAsync());
                    var afterPrivate = (await grain.QueryPrivateSideAsync(
                        CampaignActionAudience.Axis)).Cut!;
                    Assert.Equal(beforePrivate.StateVersion, afterPrivate.StateVersion);
                    Assert.Equal(beforePrivate.ActionSetToken, afterPrivate.ActionSetToken);
                    Assert.Equal(beforePrivate.Contacts.Select(c => c.ContactId),
                        afterPrivate.Contacts.Select(c => c.ContactId));
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
                var afterSystem = await grain.QueryPrivateSideAsync(CampaignActionAudience.Axis);
                Assert.Equal(TemporaryCampaignStatus.Current, afterSystem.Status);
                Assert.Equal(2, afterSystem.Cut!.StateVersion);
                var progress = await grain.AdvanceSystemAsync(8);
                Assert.Equal(TemporaryCampaignStatus.NeedsHuman, progress.Status);
                Assert.Equal(2, progress.Steps);
                Assert.Equal(4, (await grain.QueryPrivateSideAsync(
                    CampaignActionAudience.Commonwealth)).Cut!.StateVersion);
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
                (await grain.QueryPrivateSideAsync(CampaignActionAudience.Axis)).Status);
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
            var expectedKinds = new[]
            {
                "resolve-initiative", "resolve-no-obligation-naval-convoy-schedule",
                "resolve-no-obligation-tactical-shipping", "act-first", "resolve-weather",
                "resolve-no-obligation-organization", "resolve-no-obligation-naval-convoy-arrival",
                "resolve-no-obligation-fleet-assignment", "resolve-no-obligation-fleet-repair",
                "complete-reserve-designation", "complete-movement-segment",
                "complete-breakdown-segment",
            };
            var sawTypedSubject = false;
            for (var index = 0; index < expectedKinds.Length; index++)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(direct, audience).ActionSet!)
                    .First(value => value.Candidates.Count > 0);
                var candidate = set.Candidates.FirstOrDefault(value => value.Kind == "act-first")
                    ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-reserve-designation")
                    ?? set.Candidates.FirstOrDefault(value => value.Kind == "complete-movement-segment")
                    ?? set.Candidates[0];
                Assert.Equal(expectedKinds[index], candidate.Kind);
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
                    var expectedPrivate = CampaignHostAdapter.QueryPrivateSideCut(direct, side)!;
                    var actualPrivate = (await grain.QueryPrivateSideAsync(side)).Cut!;
                    Assert.Equal(expectedPrivate.Actions.Select(a =>
                            (a.CoreActionId, a.ElementId, a.TargetLocationId, a.OpportunityId)),
                        actualPrivate.Actions.Select(a =>
                            (a.CoreActionId, a.ElementId, a.TargetLocationId, a.OpportunityId)));
                    sawTypedSubject |= expectedPrivate.Actions.Any(a =>
                        a.Kind is "move-element" or "designate-reserve" && a.ElementId is not null);
                }
                Assert.Equal(CampaignLegalActionSerializer.Serialize(
                        CampaignHostAdapter.QuerySystem(direct).ActionSet!),
                    (await grain.QueryAsync(CampaignActionAudience.System)).LegalActions);
            }
            var checkpoint = CampaignExercises.QueryCheckpoint(direct);
            Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination",
                checkpoint.PositionId);
            var stored = host.Services.GetRequiredService<TemporaryCampaignMemoryStore>()
                .Get("host-rsh-001");
            var snapshotBefore = CampaignExercises.Reconstruct(stored.Committed!).ReconstructedSnapshotBytes!;
            Assert.Equal("222b36760b250ab6ea8eb14a7913ffb56bab1b1786cb7a9d9b1f720360ca3c84",
                Convert.ToHexStringLower(SHA256.HashData(snapshotBefore)));
            var randomBefore = RandomBytes(snapshotBefore);
            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            {
                var legal = CampaignLegalActionSerializer.DeserializeCanonical(
                    (await grain.QueryAsync(audience)).LegalActions!);
                Assert.Empty(legal.Candidates);
            }
            var forgedCombat = new CampaignActionSubmission(1, checkpoint.CampaignId,
                checkpoint.StateVersion, checkpoint.PositionId, CampaignActionAudience.Axis,
                "sha256:" + new string('0', 64));
            Assert.False(CampaignHostAdapter.Submit(direct, forgedCombat).IsAccepted);
            Assert.Equal(TemporaryCampaignStatus.Rejected,
                (await grain.SubmitAsync("seat-1", "forged-combat", CampaignActionAudience.Axis,
                    CampaignActionSubmissionSerializer.SerializeCanonical(forgedCombat))).Status);
            Assert.Equal(TemporaryCampaignStatus.UnsupportedPhase,
                (await grain.AdvanceSystemAsync(4)).Status);
            var snapshotAfter = CampaignExercises.Reconstruct(stored.Committed!).ReconstructedSnapshotBytes!;
            Assert.Equal(randomBefore, RandomBytes(snapshotAfter));
            Assert.Equal(snapshotBefore,
                snapshotAfter);
            foreach (var audience in Enum.GetValues<CampaignActionAudience>())
                Assert.Empty(CampaignLegalActionSerializer.DeserializeCanonical(
                    (await grain.QueryAsync(audience)).LegalActions!).Candidates);
            Assert.Equal(13, CampaignExercises.QueryCheckpoint(direct).StateVersion);
            Assert.True(sawTypedSubject);
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
        internal byte[] Capability => capability.ToArray();
        internal Task<TemporaryCampaignReply> CreateAsync(string actor, string id) =>
            inner.CreateAsync(capability, actor, id);
        internal Task<TemporaryCampaignReply> QueryAsync(CampaignActionAudience audience) =>
            inner.QueryAsync(capability, audience);
        internal Task<TemporaryPrivateCutReply> QueryPrivateSideAsync(
            CampaignActionAudience audience) =>
            inner.QueryPrivateSideAsync(capability, audience, Generation);
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

    private static byte[] RandomBytes(byte[] snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        return Encoding.UTF8.GetBytes(document.RootElement.GetProperty("randomState").GetRawText());
    }
}
