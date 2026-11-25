using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed class GrazianiPregameJournalTests
{
    private const string Secret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    /// <summary>
    /// Phase C3a of issue #439: <c>CampaignDurableOutwardApi</c>'s <c>/{campaignId}/actions</c>
    /// route now dispatches through <c>ICampaignGrain</c>, so the one test in this file that
    /// exercises that route needs a real Orleans silo backing it -- see
    /// <c>CampaignDurableAuthorityJournalTests.StartOutwardApiSiloAsync</c>'s remarks for the
    /// same pattern.
    /// </summary>
    private static async Task<IHost> StartOutwardApiSiloAsync(CampaignDurableAuthorityJournalStore store)
    {
        var siloBuilder = Host.CreateApplicationBuilder();
        siloBuilder.Services.AddSingleton(store);
        // See CampaignDurableAuthorityJournalTests.StartOutwardApiSiloAsync's remarks: the
        // round-timeout grain CampaignGrain may arm/disarm needs this resolvable even when this
        // test never exercises deadline behavior itself.
        siloBuilder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
        siloBuilder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: 0);
            silo.AddReminders();
            silo.UseInMemoryReminderService();
        });
        var silo = siloBuilder.Build();
        await silo.StartAsync(TestContext.Current.CancellationToken);
        return silo;
    }

    private static async Task StopOutwardApiSiloAsync(IHost silo)
    {
        await silo.StopAsync(TestContext.Current.CancellationToken);
        silo.Dispose();
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [Fact]
    public void OwnerSetupFinalizationSurvivesReplayWithoutExposingOpposingChoice()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-owner-finalization-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-owner-finalization", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, init);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var opening = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, opening.Revision, Assert.Single(opening.PublicActionRefs)).IsAccepted);
            opening = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, opening.Revision, Assert.Single(opening.PublicActionRefs)).IsAccepted);
            var commonwealth = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, Secret)));

            Assert.Throws<UnauthorizedAccessException>(() => journal.QuerySourceCounterContacts(
                commonwealth with { Secret = new string('0', 64) }, "scheferzen-garrison"));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCounterContacts(
                commonwealth, "scheferzen-garrison"));

            var cwBefore = journal.QuerySourceActionsOpening(commonwealth);
            var axisBefore = journal.QuerySourceActionsOpening(axis);
            var axisFinish = FindFinalization(axisBefore.VisibleBytes, "finish");
            Assert.True(journal.SubmitSourceActions(axis, axisBefore.Revision, axisBefore.PublicActionRefs[axisFinish]).IsAccepted);
            var cwAfter = journal.QuerySourceActionsOpening(commonwealth);
            Assert.Equal(cwBefore.VisibleBytes, cwAfter.VisibleBytes);
            Assert.Equal(cwBefore.PublicActionRefs, cwAfter.PublicActionRefs);
            var cwFinish = FindFinalization(cwAfter.VisibleBytes, "finish");
            Assert.True(journal.SubmitSourceActions(commonwealth, cwAfter.Revision, cwAfter.PublicActionRefs[cwFinish]).IsAccepted);
            var axisFinalized = journal.QuerySourceActionsOpening(axis);
            Assert.True(FindFinalization(axisFinalized.VisibleBytes, "reopen") >= 0);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            var afterReplay = reopened.QuerySourceActionsOpening(axis);
            Assert.Equal(axisFinalized.VisibleBytes, afterReplay.VisibleBytes);
            Assert.Equal(axisFinalized.PublicActionRefs, afterReplay.PublicActionRefs);
            var cwFinalized = reopened.QuerySourceActionsOpening(commonwealth);
            var axisReopen = FindFinalization(afterReplay.VisibleBytes, "reopen");
            Assert.True(reopened.SubmitSourceActions(axis, afterReplay.Revision, afterReplay.PublicActionRefs[axisReopen]).IsAccepted);
            var cwAfterReopen = reopened.QuerySourceActionsOpening(commonwealth);
            Assert.Equal(cwFinalized.VisibleBytes, cwAfterReopen.VisibleBytes);
            Assert.Equal(cwFinalized.PublicActionRefs, cwAfterReopen.PublicActionRefs);
            Assert.Throws<InvalidOperationException>(() => reopened.StartSourceTurn(request.CampaignId, Secret));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static int FindFinalization(byte[] visible, string kind)
    {
        using var document = JsonDocument.Parse(visible);
        var choices = document.RootElement.GetProperty("choices").EnumerateArray().ToArray();
        var index = Array.FindIndex(choices, choice => choice.GetProperty("family").GetString() == "owner-finalization"
            && choice.GetProperty("choice").GetProperty("kind").GetString() == kind);
        Assert.True(index >= 0);
        return index;
    }

    [Fact]
    public void HorseRulingUsesPrivateOwnerChoicesAndSurvivesHostReplay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-horse-ruling-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-horse-ruling", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, init);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var supply = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, supply.Revision, Assert.Single(supply.PublicActionRefs)).IsAccepted);
            supply = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, supply.Revision, Assert.Single(supply.PublicActionRefs)).IsAccepted);
            var cw = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, Secret)));

            var before = journal.QuerySourceActionsOpening(cw);
            var axisOpening = journal.QuerySourceActionsOpening(axis);
            var axisVote = FindSourceChoice(axisOpening.VisibleBytes, "central-india-horse-ruling", "approve");
            Assert.True(journal.SubmitSourceActions(axis, axisOpening.Revision, axisOpening.PublicActionRefs[axisVote]).IsAccepted);
            var after = journal.QuerySourceActionsOpening(cw);
            Assert.Equal(before.VisibleBytes, after.VisibleBytes);
            Assert.Equal(before.PublicActionRefs, after.PublicActionRefs);
            var cwVote = FindSourceChoice(after.VisibleBytes, "central-india-horse-ruling", "approve");
            Assert.True(journal.SubmitSourceActions(cw, after.Revision, after.PublicActionRefs[cwVote]).IsAccepted);
            var agreed = journal.QuerySourceActionsOpening(cw);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            var replayed = reopened.QuerySourceActionsOpening(cw);
            Assert.Equal(agreed.VisibleBytes, replayed.VisibleBytes);
            Assert.Equal(agreed.PublicActionRefs, replayed.PublicActionRefs);
            Assert.True(FindSourceChoice(replayed.VisibleBytes, "central-india-horse-ruling", "withdraw") >= 0);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static int FindSourceChoice(byte[] visible, string family, string kind)
    {
        using var document = JsonDocument.Parse(visible);
        var choices = document.RootElement.GetProperty("choices").EnumerateArray().ToArray();
        var index = Array.FindIndex(choices, choice => choice.GetProperty("family").GetString() == family
            && choice.GetProperty("choice").GetProperty("kind").GetString() == kind);
        Assert.True(index >= 0);
        return index;
    }

    [Fact]
    public async Task OperatorBatchRetainsEachSourceMigrationWithoutChoosingForEitherSide()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-source-batch-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-source-batch", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, init);
            var before = ReadHead(path, request.CampaignId);
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitAvailableSourceProfiles(request.CampaignId,
                new string('0', 64)));
            Assert.Throws<InvalidOperationException>(() => journal.AdmitAvailableSourceProfiles(request.CampaignId, Secret));
            Assert.Equal(before, ReadHead(path, request.CampaignId));
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var finish = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, Assert.Single(finish.PublicActionRefs)).IsAccepted);
            Assert.True(journal.AdmitSourceCompactCheckpoint(request.CampaignId, Secret));
            Assert.Equal(7, journal.AdmitAvailableSourceProfiles(request.CampaignId, Secret));
            Assert.Equal(0, journal.AdmitAvailableSourceProfiles(request.CampaignId, Secret));
            var checkpoint = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
            Assert.Equal(11, checkpoint.StateVersion);
            Assert.Equal("land.graziani-source-setup", checkpoint.PositionId);
            Assert.Equal(10, ReadHead(path, request.CampaignId).StepCount);
            var commonwealth = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, Secret)));
            using (var cairo = JsonDocument.Parse(journal.QuerySourceCairoSetupOpening(commonwealth).VisibleBytes))
            {
                Assert.Empty(cairo.RootElement.GetProperty("placed").EnumerateArray().ToArray());
                Assert.NotEmpty(cairo.RootElement.GetProperty("choices").EnumerateArray().ToArray());
            }
            using (var axisCargo = JsonDocument.Parse(journal.QuerySourceUnattachedCargoOpening(axis).VisibleBytes))
            using (var commonwealthCargo = JsonDocument.Parse(journal.QuerySourceUnattachedCargoOpening(commonwealth).VisibleBytes))
            {
                Assert.Equal("setup-incomplete", axisCargo.RootElement.GetProperty("status").GetString());
                Assert.Equal("setup-incomplete", commonwealthCargo.RootElement.GetProperty("status").GetString());
                Assert.Empty(axisCargo.RootElement.GetProperty("loads").EnumerateArray());
                Assert.Empty(commonwealthCargo.RootElement.GetProperty("loads").EnumerateArray());
            }
            var axisActions = journal.QuerySourceActionsOpening(axis);
            var cwActions = journal.QuerySourceActionsOpening(commonwealth);
            using (var axisView = JsonDocument.Parse(axisActions.VisibleBytes))
            using (var cwView = JsonDocument.Parse(cwActions.VisibleBytes))
            {
                Assert.DoesNotContain("cw-supply", axisView.RootElement.GetRawText(), StringComparison.Ordinal);
                Assert.DoesNotContain("axis-dump", cwView.RootElement.GetRawText(), StringComparison.Ordinal);
                Assert.DoesNotContain("actionId", axisView.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
                var gt1Context = axisView.RootElement.GetProperty("contexts").EnumerateArray()
                    .Single(value => value.GetProperty("family").GetString() == "axis-gt1-supply").GetProperty("state");
                Assert.Equal("provisional", gt1Context.GetProperty("policyStatus").GetString());
                Assert.Equal(JsonValueKind.Null, gt1Context.GetProperty("die").ValueKind);
                Assert.Empty(gt1Context.GetProperty("allocations").EnumerateArray());
                var choices = axisView.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var dump = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "axis-dump");
                Assert.True(dump >= 0);
                Assert.False(journal.SubmitSourceActions(commonwealth, axisActions.Revision, axisActions.PublicActionRefs[dump]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(axis, axisActions.Revision, axisActions.PublicActionRefs[dump]).IsAccepted);
            }
            var cwActionsAfter = journal.QuerySourceActionsOpening(commonwealth);
            Assert.Equal(cwActions.VisibleBytes, cwActionsAfter.VisibleBytes);
            Assert.Equal(cwActions.PublicActionRefs, cwActionsAfter.PublicActionRefs);
            Assert.NotEmpty(cwActions.PublicActionRefs);
            Assert.True(journal.SubmitSourceActions(commonwealth, cwActions.Revision, cwActions.PublicActionRefs[0]).IsAccepted);
            checkpoint = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
            using (var compactConnection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
            {
                compactConnection.Open();
                using var compactQuery = compactConnection.CreateCommand();
                compactQuery.CommandText = "SELECT snapshot_checkpoint FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence DESC LIMIT 1";
                compactQuery.Parameters.AddWithValue("$id", request.CampaignId);
                var compactBytes = (byte[])compactQuery.ExecuteScalar()!;
                Assert.True(compactBytes.Length < 16_384);
                using var compact = JsonDocument.Parse(compactBytes);
                Assert.Equal(30, compact.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.Equal(JsonValueKind.Null, compact.RootElement.GetProperty("gameClock").ValueKind);
            }
            var child = await ReadInFreshProcess(path, request.CampaignId, "--source-authority-read");
            Assert.Equal(checkpoint.StateVersion, child.StateVersion);
            Assert.NotEqual(Environment.ProcessId, child.ProcessId);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ZeroGt1SupplyIsRolledAndFinishedRatherThanInvented()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-zero-gt1-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            // Seed 2 yields GT2 die 5, then GT1 die 1 under the pinned provisional draw order.
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-zero-gt1", Cna1979Ruleset.Manifest.Hash, 2,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, init);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var finishGt2 = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, finishGt2.Revision,
                Assert.Single(finishGt2.PublicActionRefs)).IsAccepted);
            var gt1 = journal.QuerySourceAxisGt1SupplyOpening(axis);
            using (var unrolled = JsonDocument.Parse(gt1.VisibleBytes))
                Assert.Equal(JsonValueKind.Null, unrolled.RootElement.GetProperty("maximumSupply").ValueKind);
            Assert.True(journal.SubmitSourceAxisGt1Supply(axis, gt1.Revision, Assert.Single(gt1.PublicActionRefs)).IsAccepted);
            gt1 = journal.QuerySourceAxisGt1SupplyOpening(axis);
            using (var rolled = JsonDocument.Parse(gt1.VisibleBytes))
            {
                Assert.Equal(0, rolled.RootElement.GetProperty("maximumSupply").GetInt32());
                Assert.Equal("finish", Assert.Single(rolled.RootElement.GetProperty("choices").EnumerateArray().ToArray())
                    .GetProperty("kind").GetString());
            }
            Assert.True(journal.SubmitSourceAxisGt1Supply(axis, gt1.Revision, Assert.Single(gt1.PublicActionRefs)).IsAccepted);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            var checkpoint = CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId));
            Assert.Equal(5, checkpoint.StateVersion);
            using var finished = JsonDocument.Parse(reopened.QuerySourceAxisGt1SupplyOpening(axis).VisibleBytes);
            Assert.True(finished.RootElement.GetProperty("finished").GetBoolean());
            Assert.Equal(0, finished.RootElement.GetProperty("allocations").GetArrayLength());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-source-authority-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-authority", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, init);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            Assert.Throws<InvalidOperationException>(() => journal.IssueSeatGrant(request.CampaignId,
                CampaignActionAudience.Commonwealth, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QueryPregameAxisSupplyFinishOpening(axis));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var plan = journal.QueryPregameAxisSupplyOpening(axis);
            var ordinal = Array.FindIndex(Choices(plan.VisibleBytes), value => value.Kind == "add" && value.Lane == 2 && value.Stage == 1);
            var staleOrdinal = Array.FindIndex(Choices(plan.VisibleBytes), value => value.Kind == "add" && value.Lane == 3 && value.Stage == 1);
            Assert.True(ordinal >= 0);
            Assert.True(staleOrdinal >= 0);
            Assert.True(journal.SubmitPregameAxisSupply(axis, plan.Revision, plan.PublicActionRefs[ordinal]).IsAccepted);
            var finish = journal.QueryPregameAxisSupplyFinishOpening(axis);
            using (var visible = JsonDocument.Parse(finish.VisibleBytes))
            {
                Assert.Equal("setup-incomplete", visible.RootElement.GetProperty("status").GetString());
                Assert.Equal(1, visible.RootElement.GetProperty("allocatedSupply").GetInt32());
                Assert.Equal(2, visible.RootElement.GetProperty("unusedAvailability").GetInt32());
            }
            var reference = Assert.Single(finish.PublicActionRefs);
            var accepted = journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, reference);
            Assert.True(accepted.IsAccepted);
            var retry = journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, reference);
            Assert.True(retry.IsAccepted);
            Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
            Assert.False(journal.SubmitPregameAxisSupply(axis, plan.Revision, plan.PublicActionRefs[staleOrdinal]).IsAccepted);
            var checkpoint = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
            Assert.Equal(4, checkpoint.StateVersion);
            Assert.Equal("land.graziani-source-setup", checkpoint.PositionId);
            var commonwealth = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, Secret)));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAxisDumpOpening(commonwealth));
            Assert.Throws<InvalidOperationException>(() => journal.QueryPregameAxisSupplyFinishOpening(commonwealth));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCommonwealthSupplyOpening(axis));
            var dumpOpening = journal.QuerySourceAxisDumpOpening(axis);
            using (var dumpVisible = JsonDocument.Parse(dumpOpening.VisibleBytes))
            {
                Assert.False(dumpVisible.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
                Assert.True(dumpVisible.RootElement.GetProperty("choices").GetArrayLength() >= 4);
            }
            var cwOpening = journal.QuerySourceCommonwealthSupplyOpening(commonwealth);
            Assert.DoesNotContain("axis-dump", Encoding.UTF8.GetString(cwOpening.VisibleBytes), StringComparison.Ordinal);
            Assert.DoesNotContain("cw-", Encoding.UTF8.GetString(dumpOpening.VisibleBytes), StringComparison.Ordinal);
            Assert.NotEmpty(cwOpening.PublicActionRefs);
            Assert.True(journal.SubmitSourceCommonwealthSupply(commonwealth, cwOpening.Revision,
                cwOpening.PublicActionRefs[0]).IsAccepted);
            var axisAfterHiddenChoice = journal.QuerySourceAxisDumpOpening(axis);
            Assert.Equal(dumpOpening.VisibleBytes, axisAfterHiddenChoice.VisibleBytes);
            Assert.Equal(dumpOpening.PublicActionRefs, axisAfterHiddenChoice.PublicActionRefs);
            var firstDumpRef = dumpOpening.PublicActionRefs[0];
            Assert.True(journal.SubmitSourceAxisDump(axis, dumpOpening.Revision, firstDumpRef).IsAccepted);
            Assert.True(journal.SubmitSourceAxisDump(axis, dumpOpening.Revision, firstDumpRef).IsAccepted);
            Assert.False(journal.SubmitSourceAxisDump(axis, dumpOpening.Revision, dumpOpening.PublicActionRefs[1]).IsAccepted);
            var secondDumpOpening = journal.QuerySourceAxisDumpOpening(axis);
            Assert.NotEmpty(secondDumpOpening.PublicActionRefs);
            Assert.True(journal.SubmitSourceAxisDump(axis, secondDumpOpening.Revision,
                secondDumpOpening.PublicActionRefs[0]).IsAccepted);
            for (var placed = 1; placed < 7; placed++)
            {
                cwOpening = journal.QuerySourceCommonwealthSupplyOpening(commonwealth);
                Assert.NotEmpty(cwOpening.PublicActionRefs);
                Assert.True(journal.SubmitSourceCommonwealthSupply(commonwealth, cwOpening.Revision,
                    cwOpening.PublicActionRefs[0]).IsAccepted);
            }
            Assert.Empty(journal.QuerySourceCommonwealthSupplyOpening(commonwealth).PublicActionRefs);
            var axisDummy = journal.QuerySourceDummyOpening(axis);
            var cwDummy = journal.QuerySourceDummyOpening(commonwealth);
            Assert.DoesNotContain("cw-dummy", Encoding.UTF8.GetString(axisDummy.VisibleBytes), StringComparison.Ordinal);
            Assert.DoesNotContain("axis-dummy", Encoding.UTF8.GetString(cwDummy.VisibleBytes), StringComparison.Ordinal);
            Assert.False(journal.SubmitSourceDummy(axis, cwDummy.Revision, cwDummy.PublicActionRefs[0]).IsAccepted);
            Assert.True(journal.SubmitSourceDummy(commonwealth, cwDummy.Revision, cwDummy.PublicActionRefs[0]).IsAccepted);
            var axisDummyAfter = journal.QuerySourceDummyOpening(axis);
            Assert.Equal(axisDummy.VisibleBytes, axisDummyAfter.VisibleBytes);
            Assert.Equal(axisDummy.PublicActionRefs, axisDummyAfter.PublicActionRefs);
            using var axisDummyView = JsonDocument.Parse(axisDummy.VisibleBytes);
            var dummyChoices = axisDummyView.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            var placeDummy = Array.FindIndex(dummyChoices, value => value.GetProperty("locationId").ValueKind == JsonValueKind.String);
            Assert.True(placeDummy >= 0);
            Assert.True(journal.SubmitSourceDummy(axis, axisDummy.Revision, axisDummy.PublicActionRefs[placeDummy]).IsAccepted);
            var remainingDummy = journal.QuerySourceDummyOpening(axis);
            Assert.True(journal.SubmitSourceDummy(axis, remainingDummy.Revision, remainingDummy.PublicActionRefs[0]).IsAccepted);
            Assert.Empty(journal.QuerySourceDummyOpening(axis).PublicActionRefs);
            Assert.Empty(journal.QuerySourceDummyOpening(commonwealth).PublicActionRefs);
            var axisMotor = journal.QuerySourceMotorizationOpening(axis);
            var cwMotor = journal.QuerySourceMotorizationOpening(commonwealth);
            Assert.DoesNotContain("cw-", Encoding.UTF8.GetString(axisMotor.VisibleBytes), StringComparison.Ordinal);
            Assert.DoesNotContain("axis-", Encoding.UTF8.GetString(cwMotor.VisibleBytes), StringComparison.Ordinal);
            Assert.False(journal.SubmitSourceMotorization(axis, cwMotor.Revision, cwMotor.PublicActionRefs[0]).IsAccepted);
            Assert.True(journal.SubmitSourceMotorization(commonwealth, cwMotor.Revision, cwMotor.PublicActionRefs[0]).IsAccepted);
            var axisMotorAfter = journal.QuerySourceMotorizationOpening(axis);
            Assert.Equal(axisMotor.VisibleBytes, axisMotorAfter.VisibleBytes);
            Assert.Equal(axisMotor.PublicActionRefs, axisMotorAfter.PublicActionRefs);
            Assert.True(journal.SubmitSourceMotorization(axis, axisMotor.Revision, axisMotor.PublicActionRefs[0]).IsAccepted);
            var changedMotor = journal.QuerySourceMotorizationOpening(axis);
            using var motorView = JsonDocument.Parse(changedMotor.VisibleBytes);
            var motorChoices = motorView.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            var removeMotor = Array.FindIndex(motorChoices, value => value.GetProperty("delta").GetInt32() == -1);
            Assert.True(removeMotor >= 0);
            Assert.True(journal.SubmitSourceMotorization(axis, changedMotor.Revision,
                changedMotor.PublicActionRefs[removeMotor]).IsAccepted);
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAxisGt1SupplyOpening(commonwealth));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCommonwealthSupplyPlanOpening(commonwealth));
            var gt1 = journal.QuerySourceAxisGt1SupplyOpening(axis);
            using (var beforeRoll = JsonDocument.Parse(gt1.VisibleBytes))
            {
                Assert.Equal(JsonValueKind.Null, beforeRoll.RootElement.GetProperty("die").ValueKind);
                Assert.Equal("provisional", beforeRoll.RootElement.GetProperty("policyStatus").GetString());
            }
            Assert.True(journal.SubmitSourceAxisGt1Supply(axis, gt1.Revision, Assert.Single(gt1.PublicActionRefs)).IsAccepted);
            gt1 = journal.QuerySourceAxisGt1SupplyOpening(axis);
            using (var rolledGt1 = JsonDocument.Parse(gt1.VisibleBytes))
            {
                Assert.Equal(3, rolledGt1.RootElement.GetProperty("maximumSupply").GetInt32());
                var choices = rolledGt1.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var add = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "adjust"
                    && value.GetProperty("lane").GetInt32() == 2
                    && value.GetProperty("operationStage").GetInt32() == 1
                    && value.GetProperty("delta").GetInt32() == 1);
                Assert.True(add >= 0);
                Assert.True(journal.SubmitSourceAxisGt1Supply(axis, gt1.Revision, gt1.PublicActionRefs[add]).IsAccepted);
            }
            gt1 = journal.QuerySourceAxisGt1SupplyOpening(axis);
            using (var plannedGt1 = JsonDocument.Parse(gt1.VisibleBytes))
            {
                var choices = plannedGt1.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var finishGt1 = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "finish");
                Assert.True(finishGt1 >= 0);
                Assert.True(journal.SubmitSourceAxisGt1Supply(axis, gt1.Revision, gt1.PublicActionRefs[finishGt1]).IsAccepted);
            }
            gt1 = journal.QuerySourceAxisGt1SupplyOpening(axis);
            using (var finishedGt1 = JsonDocument.Parse(gt1.VisibleBytes))
                Assert.True(finishedGt1.RootElement.GetProperty("finished").GetBoolean());
            Assert.Empty(gt1.PublicActionRefs);
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCommonwealthSupplyPlanOpening(axis));
            var cwPlan = journal.QuerySourceCommonwealthSupplyPlanOpening(commonwealth);
            using (var planView = JsonDocument.Parse(cwPlan.VisibleBytes))
            {
                Assert.Equal("provisional", planView.RootElement.GetProperty("policyStatus").GetString());
                var choices = planView.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var skip = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "skip"
                    && value.GetProperty("targetGameTurn").GetInt32() == 1);
                Assert.True(skip >= 0);
                Assert.True(journal.SubmitSourceCommonwealthSupplyPlan(commonwealth, cwPlan.Revision,
                    cwPlan.PublicActionRefs[skip]).IsAccepted);
            }
            for (var target = 2; target <= 4; target++)
            {
                cwPlan = journal.QuerySourceCommonwealthSupplyPlanOpening(commonwealth);
                using var planView = JsonDocument.Parse(cwPlan.VisibleBytes);
                var choice = Assert.Single(planView.RootElement.GetProperty("choices").EnumerateArray().ToArray());
                Assert.Equal("roll", choice.GetProperty("kind").GetString());
                Assert.Equal(target, choice.GetProperty("targetGameTurn").GetInt32());
                Assert.True(journal.SubmitSourceCommonwealthSupplyPlan(commonwealth, cwPlan.Revision,
                    Assert.Single(cwPlan.PublicActionRefs)).IsAccepted);
            }
            cwPlan = journal.QuerySourceCommonwealthSupplyPlanOpening(commonwealth);
            Assert.Empty(cwPlan.PublicActionRefs);
            using (var planView = JsonDocument.Parse(cwPlan.VisibleBytes))
            {
                var entries = planView.RootElement.GetProperty("plans").EnumerateArray().ToArray();
                Assert.Equal(4, entries.Length);
                Assert.All(entries.Skip(1), entry => Assert.Equal(JsonValueKind.Number, entry.GetProperty("supplyUnits").ValueKind));
            }
            Assert.DoesNotContain("axis-september-supply", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(commonwealth).VisibleBytes), StringComparison.Ordinal);
            foreach (var (kind, target) in new[] { ("begin", 0), ("roll", 3), ("finish", 3), ("roll", 4), ("finish", 4) })
            {
                var opening = journal.QuerySourceActionsOpening(axis);
                using var view = JsonDocument.Parse(opening.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var selected = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "axis-september-supply"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == kind
                    && (target == 0 || value.GetProperty("choice").GetProperty("targetGameTurn").GetInt32() == target));
                Assert.True(selected >= 0);
                Assert.False(journal.SubmitSourceActions(commonwealth, opening.Revision, opening.PublicActionRefs[selected]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(axis, opening.Revision, opening.PublicActionRefs[selected]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceAxisSupplement(request.CampaignId, new string('0', 64)));
            Assert.Throws<InvalidOperationException>(() => journal.AdmitSourceCommonwealthSupplement(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.AdmitSourcePriorityMotorization(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.AdmitSourceAxisMinorGarrisons(request.CampaignId, Secret));
            Assert.True(journal.AdmitSourceAxisSupplement(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceAxisSupplement(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAutoblindaOpening(commonwealth));
            var autoblinda = journal.QuerySourceAutoblindaOpening(axis);
            using (var view = JsonDocument.Parse(autoblinda.VisibleBytes))
            {
                Assert.Equal("provisional-either-or-no-mixing", view.RootElement.GetProperty("policyStatus").GetString());
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var replacement = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "replacements");
                Assert.True(replacement >= 0);
                Assert.True(journal.SubmitSourceAutoblinda(axis, autoblinda.Revision,
                    autoblinda.PublicActionRefs[replacement]).IsAccepted);
            }
            autoblinda = journal.QuerySourceAutoblindaOpening(axis);
            using (var view = JsonDocument.Parse(autoblinda.VisibleBytes))
            {
                Assert.Equal(2, view.RootElement.GetProperty("replacementPoints").GetInt32());
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.DoesNotContain(choices, value => value.GetProperty("kind").GetString() == "attach");
                var reset = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "reset");
                Assert.True(reset >= 0);
                Assert.True(journal.SubmitSourceAutoblinda(axis, autoblinda.Revision,
                    autoblinda.PublicActionRefs[reset]).IsAccepted);
            }
            for (var count = 0; count < 2; count++)
            {
                autoblinda = journal.QuerySourceAutoblindaOpening(axis);
                using var view = JsonDocument.Parse(autoblinda.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var attach = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "attach");
                Assert.True(attach >= 0);
                Assert.True(journal.SubmitSourceAutoblinda(axis, autoblinda.Revision,
                    autoblinda.PublicActionRefs[attach]).IsAccepted);
            }
            using (var view = JsonDocument.Parse(journal.QuerySourceAutoblindaOpening(axis).VisibleBytes))
            {
                var targets = view.RootElement.GetProperty("attachments").EnumerateArray().Select(value => value.GetString()).ToArray();
                Assert.Equal(2, targets.Length);
                Assert.NotEqual(targets[0], targets[1]);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCommonwealthSupplement(request.CampaignId,
                new string('0', 64)));
            Assert.True(journal.AdmitSourceCommonwealthSupplement(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCommonwealthSupplement(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourcePriorityMotorization(request.CampaignId,
                new string('0', 64)));
            Assert.True(journal.AdmitSourcePriorityMotorization(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourcePriorityMotorization(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourcePriorityMotorizationOpening(axis));
            var priority = journal.QuerySourcePriorityMotorizationOpening(commonwealth);
            using (var view = JsonDocument.Parse(priority.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var assign = Array.FindIndex(choices, value => value.GetProperty("delta").GetInt32() == 6
                    && value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.True(assign >= 0);
                Assert.True(journal.SubmitSourcePriorityMotorization(commonwealth, priority.Revision,
                    priority.PublicActionRefs[assign]).IsAccepted);
            }
            priority = journal.QuerySourcePriorityMotorizationOpening(commonwealth);
            using (var view = JsonDocument.Parse(priority.VisibleBytes))
            {
                var target = view.RootElement.GetProperty("targets").EnumerateArray()
                    .Single(value => value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.Equal(6, target.GetProperty("assignedPoints").GetInt32());
                Assert.Equal(14, target.GetProperty("remainingPoolPoints").GetInt32());
                Assert.True(target.GetProperty("infantryComponentLiftSatisfied").GetBoolean());
                Assert.Equal("unproven", view.RootElement.GetProperty("fullMotorizationStatus").GetString());
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.DoesNotContain(choices, value => value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps"
                    && value.GetProperty("delta").GetInt32() > 0);
                var undo = Array.FindIndex(choices, value => value.GetProperty("delta").GetInt32() == -6
                    && value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.True(undo >= 0);
                Assert.True(journal.SubmitSourcePriorityMotorization(commonwealth, priority.Revision,
                    priority.PublicActionRefs[undo]).IsAccepted);
            }
            using (var view = JsonDocument.Parse(journal.QuerySourcePriorityMotorizationOpening(commonwealth).VisibleBytes))
            {
                var target = view.RootElement.GetProperty("targets").EnumerateArray()
                    .Single(value => value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.Equal(0, target.GetProperty("assignedPoints").GetInt32());
                Assert.Equal(20, target.GetProperty("remainingPoolPoints").GetInt32());
                Assert.False(target.GetProperty("infantryComponentLiftSatisfied").GetBoolean());
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceAxisPriorityMotorization(request.CampaignId,
                new string('0', 64)));
            Assert.True(journal.AdmitSourceAxisPriorityMotorization(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceAxisPriorityMotorization(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAxisPriorityOpening(commonwealth));
            var axisPriority = journal.QuerySourceAxisPriorityOpening(axis);
            using (var view = JsonDocument.Parse(axisPriority.VisibleBytes))
                Assert.Empty(view.RootElement.GetProperty("choices").EnumerateArray());
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceAxisMinorGarrisons(request.CampaignId,
                new string('0', 64)));
            Assert.True(journal.AdmitSourceAxisMinorGarrisons(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceAxisMinorGarrisons(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceBattlefield(request.CampaignId,
                new string('0', 64)));
            Assert.True(journal.AdmitSourceBattlefield(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceBattlefield(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCairoSetup(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCairoSetup(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCairoSetup(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.AdmitSourceCairoWorld(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCairoSetupOpening(axis));
            var cairo = journal.QuerySourceCairoSetupOpening(commonwealth);
            using (var view = JsonDocument.Parse(cairo.VisibleBytes))
            {
                Assert.False(view.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
                Assert.Equal("incomplete", view.RootElement.GetProperty("otherCairoForcesStatus").GetString());
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.NotEmpty(choices);
                Assert.DoesNotContain(choices, value => value.GetProperty("locationId").GetString() is "e1829" or "e1931");
            }
            var firstCairoRef = cairo.PublicActionRefs[0];
            Assert.True(journal.SubmitSourceCairoUnit(commonwealth, cairo.Revision, firstCairoRef).IsAccepted);
            Assert.True(journal.SubmitSourceCairoUnit(commonwealth, cairo.Revision, firstCairoRef).IsAccepted);
            Assert.False(journal.SubmitSourceCairoUnit(commonwealth, cairo.Revision, cairo.PublicActionRefs[1]).IsAccepted);
            for (var count = 1; count < 4; count++)
            {
                cairo = journal.QuerySourceCairoSetupOpening(commonwealth);
                Assert.NotEmpty(cairo.PublicActionRefs);
                Assert.True(journal.SubmitSourceCairoUnit(commonwealth, cairo.Revision, cairo.PublicActionRefs[0]).IsAccepted);
            }
            cairo = journal.QuerySourceCairoSetupOpening(commonwealth);
            Assert.Empty(cairo.PublicActionRefs);
            using (var view = JsonDocument.Parse(cairo.VisibleBytes))
                Assert.Equal(4, view.RootElement.GetProperty("placed").GetArrayLength());
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCairoWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCairoWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCairoWorld(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceMaletti(request.CampaignId, new string('0', 64)));
            Assert.False(journal.PromoteSourceMaletti(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceIndependent(request.CampaignId, new string('0', 64)));
            Assert.True(journal.PromoteSourceIndependent(request.CampaignId, Secret));
            Assert.False(journal.PromoteSourceIndependent(request.CampaignId, Secret));
            var firstIndependentContentHash = ReadCheckpointString(path, request.CampaignId, "independentActiveContentHash");
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceLocalMap(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceLocalMap(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceLocalMap(request.CampaignId, Secret));
            var localMapContentHash = ReadCheckpointString(path, request.CampaignId, "localMapActiveContentHash");
            Assert.Equal(firstIndependentContentHash, ReadCheckpointString(path, request.CampaignId, "independentActiveContentHash"));
            Assert.Equal(localMapContentHash, ReadCheckpointString(path, request.CampaignId, "localMapActiveContentHash"));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceExpandedIndependent(request.CampaignId, new string('0', 64)));
            Assert.True(journal.PromoteSourceExpandedIndependent(request.CampaignId, Secret));
            Assert.False(journal.PromoteSourceExpandedIndependent(request.CampaignId, Secret));
            priority = journal.QuerySourcePriorityMotorizationOpening(commonwealth);
            using (var view = JsonDocument.Parse(priority.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var assign = Array.FindIndex(choices, value => value.GetProperty("delta").GetInt32() == 6
                    && value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.True(assign >= 0);
                Assert.True(journal.SubmitSourcePriorityMotorization(commonwealth, priority.Revision,
                    priority.PublicActionRefs[assign]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceAttachedInitialLoads(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceAttachedInitialLoads(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceAttachedInitialLoads(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAttachedInitialLoadsOpening(axis));
            var loads = journal.QuerySourceAttachedInitialLoadsOpening(commonwealth);
            using (var view = JsonDocument.Parse(loads.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var attach = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "attach"
                    && value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.True(attach >= 0);
                Assert.True(journal.SubmitSourceAttachedInitialLoads(commonwealth, loads.Revision, loads.PublicActionRefs[attach]).IsAccepted);
            }
            loads = journal.QuerySourceAttachedInitialLoadsOpening(commonwealth);
            using (var view = JsonDocument.Parse(loads.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var ammo = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "ammo"
                    && value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
                Assert.True(ammo >= 0);
                Assert.True(journal.SubmitSourceAttachedInitialLoads(commonwealth, loads.Revision, loads.PublicActionRefs[ammo]).IsAccepted);
            }
            loads = journal.QuerySourceAttachedInitialLoadsOpening(commonwealth);
            using (var view = JsonDocument.Parse(loads.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.DoesNotContain(choices, value => value.GetProperty("kind").GetString() == "detach"
                    && value.GetProperty("elementId").GetString() == "1-kings-royal-rifle-corps");
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceInitialWrecks(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceInitialWrecks(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceInitialWrecks(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceInitialWrecksOpening(axis));
            for (var index = 0; index < 3; index++)
            {
                var wrecks = journal.QuerySourceInitialWrecksOpening(commonwealth);
                Assert.NotEmpty(wrecks.PublicActionRefs);
                Assert.True(journal.PlaceSourceInitialWreck(commonwealth, wrecks.Revision, wrecks.PublicActionRefs[0]).IsAccepted);
            }
            using (var view = JsonDocument.Parse(journal.QuerySourceInitialWrecksOpening(commonwealth).VisibleBytes))
            {
                Assert.Equal("not-adjudicated", view.RootElement.GetProperty("repairStatus").GetString());
                Assert.Equal(3, view.RootElement.GetProperty("stock").GetArrayLength());
                Assert.Empty(view.RootElement.GetProperty("choices").EnumerateArray());
            }
            // Capture a pre-v19 head for the retained pre-audit journal fixture.
            var legacySession = journal.ReadVerified(request.CampaignId);
            var legacyCheckpoint = CampaignExercises.QueryCheckpoint(legacySession);
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceAxisFreeSetup(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceAxisFreeSetup(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceAxisFreeSetup(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAxisFreeSetupOpening(commonwealth));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceAxisFreeWorld(request.CampaignId, new string('0', 64)));
            Assert.False(journal.PromoteSourceAxisFreeWorld(request.CampaignId, Secret));
            for (var index = 0; index < 4; index++)
            {
                var free = journal.QuerySourceAxisFreeSetupOpening(axis);
                Assert.NotEmpty(free.PublicActionRefs);
                using var view = JsonDocument.Parse(free.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                if (index >= 2)
                    Assert.DoesNotContain(choices, value => value.GetProperty("locationId").GetString() == "c4407");
                Assert.True(journal.PlaceSourceAxisFreeUnit(axis, free.Revision, free.PublicActionRefs[0]).IsAccepted);
                Assert.True(journal.PromoteSourceAxisFreeWorld(request.CampaignId, Secret));
                Assert.False(journal.PromoteSourceAxisFreeWorld(request.CampaignId, Secret));
            }
            using (var view = JsonDocument.Parse(journal.QuerySourceAxisFreeSetupOpening(axis).VisibleBytes))
            {
                Assert.Equal(4, view.RootElement.GetProperty("units").GetArrayLength());
                Assert.Empty(view.RootElement.GetProperty("choices").EnumerateArray());
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceAxisCorpsSetup(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceAxisCorpsSetup(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceAxisCorpsSetup(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceAxisCorpsSetupOpening(commonwealth));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceAxisCorpsWorld(request.CampaignId, new string('0', 64)));
            Assert.False(journal.PromoteSourceAxisCorpsWorld(request.CampaignId, Secret));
            for (var index = 0; index < 2; index++)
            {
                var corps = journal.QuerySourceAxisCorpsSetupOpening(axis);
                using var view = JsonDocument.Parse(corps.VisibleBytes);
                Assert.Equal("setup-incomplete", view.RootElement.GetProperty("status").GetString());
                Assert.Equal("provisional-unique-xxvi-corps-artillery-counter", view.RootElement.GetProperty("xxviIdentity").GetString());
                Assert.NotEmpty(corps.PublicActionRefs);
                Assert.True(journal.PlaceSourceAxisCorpsUnit(axis, corps.Revision, corps.PublicActionRefs[0]).IsAccepted);
                Assert.True(journal.PromoteSourceAxisCorpsWorld(request.CampaignId, Secret));
                Assert.False(journal.PromoteSourceAxisCorpsWorld(request.CampaignId, Secret));
            }
            using (var view = JsonDocument.Parse(journal.QuerySourceAxisCorpsSetupOpening(axis).VisibleBytes))
                Assert.Empty(view.RootElement.GetProperty("choices").EnumerateArray());
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceMalettiHierarchy(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceMalettiHierarchy(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceMalettiHierarchy(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceMalettiHierarchyOpening(commonwealth));
            var hierarchy = journal.QuerySourceMalettiHierarchyOpening(axis);
            using (var view = JsonDocument.Parse(hierarchy.VisibleBytes))
            {
                Assert.Contains("provisional", view.RootElement.GetProperty("ruling").GetString(), StringComparison.Ordinal);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.True(journal.SubmitSourceMalettiHierarchy(axis, hierarchy.Revision, hierarchy.PublicActionRefs[confirm]).IsAccepted);
            }
            hierarchy = journal.QuerySourceMalettiHierarchyOpening(axis);
            using (var view = JsonDocument.Parse(hierarchy.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var detach = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "detach"
                    && value.GetProperty("elementId").GetString() == "1-1-libyan-motor-infantry");
                Assert.True(detach >= 0);
                Assert.True(journal.SubmitSourceMalettiHierarchy(axis, hierarchy.Revision, hierarchy.PublicActionRefs[detach]).IsAccepted);
            }
            hierarchy = journal.QuerySourceMalettiHierarchyOpening(axis);
            using (var view = JsonDocument.Parse(hierarchy.VisibleBytes))
                Assert.DoesNotContain(view.RootElement.GetProperty("choices").EnumerateArray(), value => value.GetProperty("kind").GetString() == "detach");
            axisPriority = journal.QuerySourceAxisPriorityOpening(axis);
            using (var view = JsonDocument.Parse(axisPriority.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var assign = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "adjust"
                    && value.GetProperty("elementId").GetString() == "1-1-libyan-motor-infantry" && value.GetProperty("delta").GetInt32() == 5);
                Assert.True(assign >= 0);
                Assert.True(journal.SubmitSourceAxisPriority(axis, axisPriority.Revision, axisPriority.PublicActionRefs[assign]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceCertifiedMalettiWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.PromoteSourceCertifiedMalettiWorld(request.CampaignId, Secret));
            Assert.False(journal.PromoteSourceCertifiedMalettiWorld(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceMatruhFormation(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceMatruhFormation(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceMatruhFormation(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceMatruhFormationOpening(axis));
            var matruh = journal.QuerySourceMatruhFormationOpening(commonwealth);
            using (var view = JsonDocument.Parse(matruh.VisibleBytes))
            {
                Assert.Equal("setup-incomplete", view.RootElement.GetProperty("status").GetString());
                Assert.Contains("provisional", view.RootElement.GetProperty("assignmentRuling").GetString(), StringComparison.Ordinal);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.True(journal.SubmitSourceMatruhFormation(commonwealth, matruh.Revision, matruh.PublicActionRefs[confirm]).IsAccepted);
            }
            matruh = journal.QuerySourceMatruhFormationOpening(commonwealth);
            using (var view = JsonDocument.Parse(matruh.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var detach = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "detach"
                    && value.GetProperty("elementId").GetString() == "1-essex");
                Assert.True(detach >= 0);
                Assert.True(journal.SubmitSourceMatruhFormation(commonwealth, matruh.Revision, matruh.PublicActionRefs[detach]).IsAccepted);
            }
            matruh = journal.QuerySourceMatruhFormationOpening(commonwealth);
            using (var view = JsonDocument.Parse(matruh.VisibleBytes))
            {
                Assert.Equal("1-essex", view.RootElement.GetProperty("detachedId").GetString());
                Assert.Equal(4, view.RootElement.GetProperty("attachedIds").GetArrayLength());
                Assert.DoesNotContain(view.RootElement.GetProperty("choices").EnumerateArray(), value => value.GetProperty("kind").GetString() == "detach");
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceMatruhDetachedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.PromoteSourceMatruhDetachedWorld(request.CampaignId, Secret));
            Assert.False(journal.PromoteSourceMatruhDetachedWorld(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceGt2Correction(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceGt2Correction(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceGt2Correction(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceGt2CorrectionOpening(commonwealth));
            var gt2 = journal.QuerySourceGt2CorrectionOpening(axis);
            using (var view = JsonDocument.Parse(gt2.VisibleBytes))
            {
                Assert.Equal("supply-only-other-cargo-unresolved", view.RootElement.GetProperty("cargoCompleteness").GetString());
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var remove = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "adjust"
                    && value.GetProperty("lane").GetInt32() == 2 && value.GetProperty("stage").GetInt32() == 1
                    && value.GetProperty("delta").GetInt32() == -1);
                Assert.True(remove >= 0);
                Assert.True(journal.SubmitSourceGt2Correction(axis, gt2.Revision, gt2.PublicActionRefs[remove]).IsAccepted);
            }
            gt2 = journal.QuerySourceGt2CorrectionOpening(axis);
            using (var view = JsonDocument.Parse(gt2.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                Assert.DoesNotContain(choices, value => value.GetProperty("kind").GetString() == "finish");
                var add = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "adjust"
                    && value.GetProperty("lane").GetInt32() == 3 && value.GetProperty("stage").GetInt32() == 1
                    && value.GetProperty("delta").GetInt32() == 1);
                Assert.True(add >= 0);
                Assert.True(journal.SubmitSourceGt2Correction(axis, gt2.Revision, gt2.PublicActionRefs[add]).IsAccepted);
            }
            gt2 = journal.QuerySourceGt2CorrectionOpening(axis);
            using (var view = JsonDocument.Parse(gt2.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var finishIndex = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "finish");
                Assert.True(finishIndex >= 0);
                Assert.True(journal.SubmitSourceGt2Correction(axis, gt2.Revision, gt2.PublicActionRefs[finishIndex]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCwPriorityCatalog(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCwPriorityCatalog(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCwPriorityCatalog(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCwPriorityCatalogOpening(axis));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceCwPriorityWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.PromoteSourceCwPriorityWorld(request.CampaignId, Secret));
            Assert.False(journal.PromoteSourceCwPriorityWorld(request.CampaignId, Secret));
            var cwCatalog = journal.QuerySourceCwPriorityCatalogOpening(commonwealth);
            using (var view = JsonDocument.Parse(cwCatalog.VisibleBytes))
            {
                Assert.False(view.RootElement.GetProperty("globalPriorityComplete").GetBoolean());
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var assign = Array.FindIndex(choices, value => value.GetProperty("elementId").GetString() == "1-french-motor-marines"
                    && value.GetProperty("delta").GetInt32() == 1);
                Assert.True(assign >= 0);
                Assert.True(journal.SubmitSourceCwPriorityCatalog(commonwealth, cwCatalog.Revision, cwCatalog.PublicActionRefs[assign]).IsAccepted);
            }
            cwCatalog = journal.QuerySourceCwPriorityCatalogOpening(commonwealth);
            using (var view = JsonDocument.Parse(cwCatalog.VisibleBytes))
            {
                var french = view.RootElement.GetProperty("targets").EnumerateArray()
                    .Single(value => value.GetProperty("elementId").GetString() == "1-french-motor-marines");
                Assert.Equal(1, french.GetProperty("assignedPoints").GetInt32());
                Assert.Equal(1, french.GetProperty("remainingPoolPoints").GetInt32());
            }
            var renewedAxis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            Assert.Null(journal.AuthenticateSeat(axis));
            Assert.NotNull(journal.AuthenticateSeat(renewedAxis));
            var postMigrationMotor = journal.QuerySourceMotorizationOpening(renewedAxis);
            Assert.NotEmpty(postMigrationMotor.PublicActionRefs);
            Assert.True(journal.SubmitSourceMotorization(renewedAxis, postMigrationMotor.Revision,
                postMigrationMotor.PublicActionRefs[0]).IsAccepted);
            checkpoint = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
            Assert.Equal(91, checkpoint.StateVersion);
            var child = await ReadInFreshProcess(path, request.CampaignId, "--source-authority-read");
            Assert.NotEqual(Environment.ProcessId, child.ProcessId);
            Assert.Equal(checkpoint.StateVersion, child.StateVersion);
            Assert.Equal(checkpoint.PositionId, child.PositionId);
            using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
            connection.Open();
            using var sql = connection.CreateCommand();
            sql.CommandText = "SELECT snapshot_checkpoint FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence DESC LIMIT 1";
            sql.Parameters.AddWithValue("$id", request.CampaignId);
            var finalCheckpointBytes = (byte[])sql.ExecuteScalar()!;
            Assert.InRange(finalCheckpointBytes.Length, 2_097_153, 24 * 1024 * 1024);
            using (var metrics = connection.CreateCommand())
            {
                metrics.CommandText = "SELECT COUNT(*), SUM(length(snapshot_checkpoint)) FROM accepted_steps WHERE campaign_id=$id";
                metrics.Parameters.AddWithValue("$id", request.CampaignId);
                using var metricsRow = metrics.ExecuteReader();
                Assert.True(metricsRow.Read());
                Console.WriteLine($"Source v22 metrics: checkpoint={finalCheckpointBytes.Length}, steps={metricsRow.GetInt64(0)}, checkpoint_total={metricsRow.GetInt64(1)}, db={new FileInfo(path).Length}");
            }
            using var final = JsonDocument.Parse(finalCheckpointBytes);
            Assert.Equal("setup-in-progress", final.RootElement.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, final.RootElement.GetProperty("gameClock").ValueKind);
            using var convoy = JsonDocument.Parse(final.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
            Assert.Contains("\"lane\":2", convoy.RootElement.GetRawText(), StringComparison.Ordinal);
            using var supplyWorld = JsonDocument.Parse(final.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
            Assert.Equal(2, supplyWorld.RootElement.GetProperty("dumps").GetArrayLength());
            using var battlefield = JsonDocument.Parse(final.RootElement.GetProperty("battlefieldBase64").GetBytesFromBase64());
            Assert.False(battlefield.RootElement.GetProperty("battlefieldComplete").GetBoolean());
            Assert.False(battlefield.RootElement.GetProperty("controlCertified").GetBoolean());
            Assert.Contains("axis-1-1ar-artillery", battlefield.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("source-1ar-active", battlefield.RootElement.GetRawText(), StringComparison.Ordinal);
            using var cwPriorityWorld = JsonDocument.Parse(final.RootElement.GetProperty("cwPriorityWorldBase64").GetBytesFromBase64());
            Assert.Contains("1-french-motor-marines", cwPriorityWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", cwPriorityWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            using var matruhWorld = JsonDocument.Parse(final.RootElement.GetProperty("matruhDetachedWorldBase64").GetBytesFromBase64());
            Assert.Contains("1-essex", matruhWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", matruhWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            using var certifiedMalettiWorld = JsonDocument.Parse(final.RootElement.GetProperty("certifiedMalettiWorldBase64").GetBytesFromBase64());
            Assert.Contains("1-1-libyan-motor-infantry", certifiedMalettiWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", certifiedMalettiWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            using var corpsWorld = JsonDocument.Parse(final.RootElement.GetProperty("axisCorpsWorldBase64").GetBytesFromBase64());
            Assert.Contains("axis-xxi-corps-artillery.light-aa", corpsWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("axis-xxvi-corps-artillery.light-aa", corpsWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", corpsWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            using var axisFreeWorld = JsonDocument.Parse(final.RootElement.GetProperty("axisFreeWorldBase64").GetBytesFromBase64());
            Assert.Contains("axis-1-1ar-artillery", axisFreeWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("axis-4-1ar-artillery", axisFreeWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", axisFreeWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            using var expandedWorld = JsonDocument.Parse(final.RootElement.GetProperty("expandedIndependentActiveWorldBase64").GetBytesFromBase64());
            Assert.Contains("1-royal-northumberland-fusiliers", expandedWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("2-scots-guards", expandedWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("3-coldstream-guards", expandedWorld.RootElement.GetRawText(), StringComparison.Ordinal);
            using var activeWorld = JsonDocument.Parse(final.RootElement.GetProperty("activeWorldBase64").GetBytesFromBase64());
            var active = activeWorld.RootElement.GetRawText();
            Assert.Contains("1-welch", active, StringComparison.Ordinal);
            Assert.Contains("2-kings-own-royal-regiment", active, StringComparison.Ordinal);
            Assert.Contains("1-cheshire-machine-gun", active, StringComparison.Ordinal);
            Assert.Contains("8-field-artillery", active, StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", active, StringComparison.Ordinal);
            using var independentWorld = JsonDocument.Parse(final.RootElement.GetProperty("independentActiveWorldBase64").GetBytesFromBase64());
            var expanded = independentWorld.RootElement.GetRawText();
            Assert.Contains("1-kings-royal-rifle-corps", expanded, StringComparison.Ordinal);
            Assert.Contains("2-rifle-brigade", expanded, StringComparison.Ordinal);
            Assert.DoesNotContain("3-1-libyan-motor-infantry", expanded, StringComparison.Ordinal);
            Assert.Contains("graziani-polish-brigade", expanded, StringComparison.Ordinal);
            using var localMap = JsonDocument.Parse(final.RootElement.GetProperty("localMapActiveContentBase64").GetBytesFromBase64());
            Assert.False(localMap.RootElement.GetProperty("mapCoverageComplete").GetBoolean());
            Assert.Contains("c3320", localMap.RootElement.GetRawText(), StringComparison.Ordinal);
            Assert.Contains("c3617", localMap.RootElement.GetRawText(), StringComparison.Ordinal);
            byte[] creation, initial;
            using (var genesis = connection.CreateCommand())
            {
                genesis.CommandText = "SELECT creation_event, initial_snapshot FROM campaigns WHERE campaign_id=$id";
                genesis.Parameters.AddWithValue("$id", request.CampaignId);
                using var row = genesis.ExecuteReader();
                Assert.True(row.Read());
                creation = row.GetFieldValue<byte[]>(0);
                initial = row.GetFieldValue<byte[]>(1);
            }
            var steps = new List<CampaignHostAcceptedStep>();
            using (var query = connection.CreateCommand())
            {
                query.CommandText = """
                    SELECT canonical_command, canonical_event, canonical_receipt, snapshot_checkpoint, action_kind, acting_element_id
                    FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence
                    """;
                query.Parameters.AddWithValue("$id", request.CampaignId);
                using var rows = query.ExecuteReader();
                while (rows.Read())
                    steps.Add(new CampaignHostAcceptedStep(rows.GetFieldValue<byte[]>(0), [rows.GetFieldValue<byte[]>(1)],
                        rows.GetFieldValue<byte[]>(2), rows.GetFieldValue<byte[]>(3), rows.GetString(4),
                        rows.IsDBNull(5) ? null : rows.GetString(5)));
            }
            Assert.True(CampaignHostAdapter.ImportPregameSource(request, creation, initial, steps).IsVerified);
            Assert.True(CampaignHostAdapter.ImportPregameSourceStream(request, creation, initial,
                steps.Select(step => (step, (IReadOnlyList<byte[]>)Array.Empty<byte[]>())), []).IsVerified);
            // Recreate an accepted pre-audit command against the retained real source head.
            // Reflection accesses the internal replay path only to build a historical fixture.
            var legacyId = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes("graziani.setup.axis-priority.v1\0detach\01-1-libyan-motor-infantry\00")));
            var legacyCommand = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                request.CampaignId, legacyCheckpoint.StateVersion, legacyCheckpoint.PositionId, CampaignActionAudience.Axis, legacyId);
            Assert.False(CampaignHostAdapter.SubmitSourceAxisPriority(legacySession, legacyCommand).IsAccepted);
            var replayMethod = typeof(CampaignExercises).GetMethod("ReplaySourceAxisPriority", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(replayMethod);
            var legacyResult = Assert.IsType<ExerciseStepResult>(replayMethod.Invoke(null, [legacySession, legacyCommand]));
            Assert.True(legacyResult.IsAccepted);
            var legacyEvidence = Assert.IsType<ExerciseStepEvidence>(legacyResult.Evidence);
            var legacySteps = steps.Take(checked((int)legacyCheckpoint.StateVersion - 1)).ToList();
            legacySteps.Add(new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(legacyCommand),
                legacyEvidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(legacyEvidence.Receipt),
                legacyEvidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind,
                "land.graziani-axis-priority-motorization"));
            var afterLegacy = Assert.IsType<ExerciseSession>(legacyResult.SuccessorSession);
            var motorDraft = CampaignHostAdapter.QuerySourceMotorizationDraft(afterLegacy, CampaignActionAudience.Axis);
            Assert.NotEmpty(motorDraft.PrivateActionIds);
            var afterLegacyCheckpoint = CampaignExercises.QueryCheckpoint(afterLegacy);
            var motorCommand = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                request.CampaignId, afterLegacyCheckpoint.StateVersion, afterLegacyCheckpoint.PositionId,
                CampaignActionAudience.Axis, motorDraft.PrivateActionIds[0]);
            var motorResult = CampaignHostAdapter.AdjustSourceMotorization(afterLegacy, motorCommand);
            Assert.True(motorResult.IsAccepted);
            var motorEvidence = Assert.IsType<ExerciseStepEvidence>(motorResult.Evidence);
            legacySteps.Add(new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(motorCommand),
                motorEvidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(motorEvidence.Receipt),
                motorEvidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceMotorizationKind, "land.graziani-motorization"));
            Assert.True(CampaignHostAdapter.ImportPregameSource(request, creation, initial, legacySteps).IsVerified);
            var late = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
            var lateId = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes("graziani.setup.axis-priority.v1\0detach\03-1-libyan-motor-infantry\00")));
            var lateCommand = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                request.CampaignId, late.StateVersion, late.PositionId, CampaignActionAudience.Axis, lateId);
            var lateSteps = steps.ToList();
            lateSteps.Add(new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(lateCommand),
                legacyEvidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(legacyEvidence.Receipt),
                legacyEvidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind,
                "land.graziani-axis-priority-motorization"));
            Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
                CampaignHostAdapter.ImportPregameSource(request, creation, initial, lateSteps).RejectionReason);
            var last = steps[^1];
            var forged = JsonNode.Parse(Assert.Single(last.EventRecords))!.AsObject();
            forged["actor"] = forged["actor"]!.GetValue<string>() == "axis" ? "commonwealth" : "axis";
            steps[^1] = new CampaignHostAcceptedStep(last.CanonicalCommand,
                [Encoding.UTF8.GetBytes(forged.ToJsonString())], last.CanonicalReceipt,
                last.SnapshotCheckpoint, last.Kind, last.ActingElementId);
            Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
                CampaignHostAdapter.ImportPregameSource(request, creation, initial, steps).RejectionReason);
            var legacyHeadSequence = checkpoint.StateVersion - 1;
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCompactCheckpoint(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCompactCheckpoint(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCompactCheckpoint(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCwFreeGuns(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCwFreeGuns(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCwFreeGuns(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.QuerySourceCwFreeGunsOpening(renewedAxis));
            Assert.Throws<UnauthorizedAccessException>(() => journal.PromoteSourceCwFreeGunsWorld(request.CampaignId, new string('0', 64)));
            Assert.False(journal.PromoteSourceCwFreeGunsWorld(request.CampaignId, Secret));
            for (var index = 0; index < 3; index++)
            {
                var guns = journal.QuerySourceCwFreeGunsOpening(commonwealth);
                Assert.NotEmpty(guns.PublicActionRefs);
                Assert.True(journal.PlaceSourceCwFreeGun(commonwealth, guns.Revision, guns.PublicActionRefs[0]).IsAccepted);
                Assert.True(journal.PromoteSourceCwFreeGunsWorld(request.CampaignId, Secret));
                Assert.False(journal.PromoteSourceCwFreeGunsWorld(request.CampaignId, Secret));
            }
            using (var guns = JsonDocument.Parse(journal.QuerySourceCwFreeGunsOpening(commonwealth).VisibleBytes))
                Assert.Empty(guns.RootElement.GetProperty("choices").EnumerateArray());
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceFourthIndianWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceFourthIndianWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceFourthIndianWorld(request.CampaignId, Secret));
            var fourthAxis = journal.QuerySourceActionsOpening(renewedAxis);
            Assert.DoesNotContain("fourth-indian-motorization", Encoding.UTF8.GetString(fourthAxis.VisibleBytes), StringComparison.Ordinal);
            var fourthCw = journal.QuerySourceActionsOpening(commonwealth);
            using (var view = JsonDocument.Parse(fourthCw.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "fourth-indian-motorization"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.False(journal.SubmitSourceActions(renewedAxis, fourthCw.Revision, fourthCw.PublicActionRefs[confirm]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(commonwealth, fourthCw.Revision, fourthCw.PublicActionRefs[confirm]).IsAccepted);
            }
            fourthCw = journal.QuerySourceActionsOpening(commonwealth);
            using (var view = JsonDocument.Parse(fourthCw.VisibleBytes))
            {
                var context = view.RootElement.GetProperty("contexts").EnumerateArray()
                    .Single(value => value.GetProperty("family").GetString() == "fourth-indian-motorization").GetProperty("state");
                Assert.Equal("global-priority-incomplete", context.GetProperty("priorityStatus").GetString());
                Assert.Contains("provisional", context.GetProperty("horseRuling").GetString(), StringComparison.Ordinal);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var assign = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "fourth-indian-motorization"
                    && value.GetProperty("choice").GetProperty("elementId").GetString() == "central-india-horse"
                    && value.GetProperty("choice").GetProperty("delta").GetInt32() == 1);
                Assert.True(assign >= 0);
                Assert.False(journal.SubmitSourceActions(renewedAxis, fourthCw.Revision, fourthCw.PublicActionRefs[assign]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(commonwealth, fourthCw.Revision, fourthCw.PublicActionRefs[assign]).IsAccepted);
            }
            using (var view = JsonDocument.Parse(journal.QuerySourceActionsOpening(commonwealth).VisibleBytes))
            {
                var context = view.RootElement.GetProperty("contexts").EnumerateArray()
                    .Single(value => value.GetProperty("family").GetString() == "fourth-indian-motorization").GetProperty("state");
                Assert.Equal(21, context.GetProperty("remainingMotorPoints").GetInt32());
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceMatruhGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceMatruhGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceMatruhGroupedWorld(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceMalettiGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceMalettiGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceMalettiGroupedWorld(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceLibyanGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceLibyanGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceLibyanGroupedWorld(request.CampaignId, Secret));
            Assert.DoesNotContain("libyan-disposition", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(commonwealth).VisibleBytes), StringComparison.Ordinal);
            var confirmedLibyanGroups = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 2; index++)
            {
                var opening = journal.QuerySourceActionsOpening(renewedAxis);
                using var view = JsonDocument.Parse(opening.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "libyan-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.True(confirmedLibyanGroups.Add(choices[confirm].GetProperty("choice").GetProperty("groupId").GetString()!));
                Assert.False(journal.SubmitSourceActions(commonwealth, opening.Revision, opening.PublicActionRefs[confirm]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(renewedAxis, opening.Revision, opening.PublicActionRefs[confirm]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceItalianDivisionsGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceItalianDivisionsGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceItalianDivisionsGroupedWorld(request.CampaignId, Secret));
            Assert.DoesNotContain("italian-division-disposition", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(commonwealth).VisibleBytes), StringComparison.Ordinal);
            var confirmedItalianGroups = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 2; index++)
            {
                var opening = journal.QuerySourceActionsOpening(renewedAxis);
                using var view = JsonDocument.Parse(opening.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "italian-division-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.True(confirmedItalianGroups.Add(choices[confirm].GetProperty("choice").GetProperty("groupId").GetString()!));
                Assert.False(journal.SubmitSourceActions(commonwealth, opening.Revision, opening.PublicActionRefs[confirm]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(renewedAxis, opening.Revision, opening.PublicActionRefs[confirm]).IsAccepted);
            }
            Assert.Equal(2, confirmedItalianGroups.Count);
            Assert.Contains("cirene-division", confirmedItalianGroups);
            Assert.Contains("marmarica-division", confirmedItalianGroups);
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceSeventhArmoredGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceSeventhArmoredGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceSeventhArmoredGroupedWorld(request.CampaignId, Secret));
            Assert.DoesNotContain("seventh-armored-disposition", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(renewedAxis).VisibleBytes), StringComparison.Ordinal);
            var seventhOpening = journal.QuerySourceActionsOpening(commonwealth);
            using (var view = JsonDocument.Parse(seventhOpening.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "seventh-armored-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.False(journal.SubmitSourceActions(renewedAxis, seventhOpening.Revision, seventhOpening.PublicActionRefs[confirm]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(commonwealth, seventhOpening.Revision, seventhOpening.PublicActionRefs[confirm]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceSixteenthBrigadeGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceSixteenthBrigadeGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceSixteenthBrigadeGroupedWorld(request.CampaignId, Secret));
            Assert.DoesNotContain("sixteenth-brigade-disposition", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(renewedAxis).VisibleBytes), StringComparison.Ordinal);
            var sixteenthOpening = journal.QuerySourceActionsOpening(commonwealth);
            using (var view = JsonDocument.Parse(sixteenthOpening.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "sixteenth-brigade-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.False(journal.SubmitSourceActions(renewedAxis, sixteenthOpening.Revision, sixteenthOpening.PublicActionRefs[confirm]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(commonwealth, sixteenthOpening.Revision, sixteenthOpening.PublicActionRefs[confirm]).IsAccepted);
            }
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCatanzaroGroupedWorld(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCatanzaroGroupedWorld(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCatanzaroGroupedWorld(request.CampaignId, Secret));
            Assert.DoesNotContain("catanzaro-disposition", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(commonwealth).VisibleBytes), StringComparison.Ordinal);
            var catanzaroOpening = journal.QuerySourceActionsOpening(renewedAxis);
            using (var view = JsonDocument.Parse(catanzaroOpening.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var confirm = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "catanzaro-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "confirm");
                Assert.True(confirm >= 0);
                Assert.False(journal.SubmitSourceActions(commonwealth, catanzaroOpening.Revision, catanzaroOpening.PublicActionRefs[confirm]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(renewedAxis, catanzaroOpening.Revision, catanzaroOpening.PublicActionRefs[confirm]).IsAccepted);
            }
            var ccnnOpening = journal.QuerySourceActionsOpening(renewedAxis);
            using (var view = JsonDocument.Parse(ccnnOpening.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var accept = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "ccnn-batch-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "accept"
                    && value.GetProperty("choice").GetProperty("groupId").GetString() == "1-and-4-ccnn");
                Assert.True(accept >= 0);
                Assert.False(journal.SubmitSourceActions(commonwealth, ccnnOpening.Revision, ccnnOpening.PublicActionRefs[accept]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(renewedAxis, ccnnOpening.Revision, ccnnOpening.PublicActionRefs[accept]).IsAccepted);
            }
            var arescaOpening = journal.QuerySourceActionsOpening(renewedAxis);
            using (var view = JsonDocument.Parse(arescaOpening.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var accept = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "aresca-disposition"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == "accept"
                    && value.GetProperty("choice").GetProperty("groupId").GetString() == "aresca-tank-regiment");
                Assert.True(accept >= 0);
                Assert.False(journal.SubmitSourceActions(commonwealth, arescaOpening.Revision, arescaOpening.PublicActionRefs[accept]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(renewedAxis, arescaOpening.Revision, arescaOpening.PublicActionRefs[accept]).IsAccepted);
            }
            var australianOpening = journal.QuerySourceActionsOpening(commonwealth);
            using (var view = JsonDocument.Parse(australianOpening.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var place = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "sixth-australian-placement"
                    && value.GetProperty("choice").GetProperty("locationId").GetString() == "e1829");
                Assert.True(place >= 0);
                Assert.False(journal.SubmitSourceActions(renewedAxis, australianOpening.Revision, australianOpening.PublicActionRefs[place]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(commonwealth, australianOpening.Revision, australianOpening.PublicActionRefs[place]).IsAccepted);
            }
            Assert.DoesNotContain("sixth-australian-placement", Encoding.UTF8.GetString(journal.QuerySourceActionsOpening(renewedAxis).VisibleBytes), StringComparison.Ordinal);
            Assert.Throws<UnauthorizedAccessException>(() => journal.CorrectSourceSeventhShell(request.CampaignId, new string('0', 64)));
            Assert.True(journal.CorrectSourceSeventhShell(request.CampaignId, Secret));
            Assert.False(journal.CorrectSourceSeventhShell(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceAxisMinorGarrisons(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceAxisMinorGarrisons(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceAxisMinorGarrisons(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceAxisGarrisonRemainder(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceAxisGarrisonRemainder(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceAxisGarrisonRemainder(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceGiarabubMotorMachineGun(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceGiarabubMotorMachineGun(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceGiarabubMotorMachineGun(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceCwFrontline(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceCwFrontline(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceCwFrontline(request.CampaignId, Secret));
            // #580: ordering is enforced; later steps are unavailable until their predecessor ran.
            Assert.Throws<InvalidOperationException>(() => journal.ActivateSourceTobrukBardiaGarrison(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.AdmitSourceBardiaGarrisonRemainder(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.ActivateSourceTripoliTankRegiment(request.CampaignId, Secret));
            Assert.Throws<InvalidOperationException>(() => journal.ActivateSourceCwAttachedArtillery(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceTobrukBardiaGarrison(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceTobrukBardiaGarrison(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceTobrukBardiaGarrison(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceTobrukBardiaGarrison(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceTobrukBardiaGarrison(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceTobrukBardiaGarrison(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceBardiaGarrisonRemainder(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceBardiaGarrisonRemainder(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceBardiaGarrisonRemainder(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceBardiaGarrisonRemainder(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceBardiaGarrisonRemainder(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceBardiaGarrisonRemainder(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceTripoliTankRegiment(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceTripoliTankRegiment(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceTripoliTankRegiment(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceTripoliTankRegiment(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceTripoliTankRegiment(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceTripoliTankRegiment(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceCwAttachedArtillery(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceCwAttachedArtillery(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceCwAttachedArtillery(request.CampaignId, Secret));
            Assert.Throws<UnauthorizedAccessException>(() => journal.ActivateSourceCwAttachedArtillery(request.CampaignId, new string('0', 64)));
            Assert.True(journal.ActivateSourceCwAttachedArtillery(request.CampaignId, Secret));
            Assert.False(journal.ActivateSourceCwAttachedArtillery(request.CampaignId, Secret));
            var frontlineChoices = Enumerable.Repeat((Kind: "frontline-attach", ElementId: "1-rtr"), 5)
                .Concat(Enumerable.Repeat((Kind: "frontline-attach", ElementId: "11-hussars"), 3))
                .Concat(new[] { (Kind: "frontline-finish", ElementId: "frontline"),
                    (Kind: "frontline-reopen", ElementId: "frontline"), (Kind: "frontline-fuel", ElementId: "1-rtr") });
            foreach (var (kind, elementId) in frontlineChoices)
            {
                var opening = journal.QuerySourceActionsOpening(commonwealth);
                using var view = JsonDocument.Parse(opening.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var index = Array.FindIndex(choices, value => value.GetProperty("family").GetString() == "attached-loads"
                    && value.GetProperty("choice").GetProperty("kind").GetString() == kind
                    && value.GetProperty("choice").GetProperty("elementId").GetString() == elementId);
                Assert.True(index >= 0);
                Assert.False(journal.SubmitSourceActions(renewedAxis, opening.Revision, opening.PublicActionRefs[index]).IsAccepted);
                Assert.True(journal.SubmitSourceActions(commonwealth, opening.Revision, opening.PublicActionRefs[index]).IsAccepted);
            }
            var cwBeforeMotorPolicy = journal.QuerySourceMotorizationOpening(commonwealth);
            var axisBeforeMotorPolicy = journal.QuerySourceMotorizationOpening(renewedAxis);
            Assert.NotEmpty(axisBeforeMotorPolicy.PublicActionRefs);
            Assert.Throws<UnauthorizedAccessException>(() => journal.AdmitSourceGiarabubMotorPlacementPolicy(request.CampaignId, new string('0', 64)));
            Assert.True(journal.AdmitSourceGiarabubMotorPlacementPolicy(request.CampaignId, Secret));
            Assert.False(journal.AdmitSourceGiarabubMotorPlacementPolicy(request.CampaignId, Secret));
            var cwAfterMotorPolicy = journal.QuerySourceMotorizationOpening(commonwealth);
            Assert.Equal(cwBeforeMotorPolicy.VisibleBytes, cwAfterMotorPolicy.VisibleBytes);
            Assert.Equal(cwBeforeMotorPolicy.PublicActionRefs, cwAfterMotorPolicy.PublicActionRefs);
            var giarabubMotor = journal.QuerySourceMotorizationOpening(renewedAxis);
            Assert.False(journal.SubmitSourceMotorization(renewedAxis, axisBeforeMotorPolicy.Revision,
                axisBeforeMotorPolicy.PublicActionRefs[0]).IsAccepted);
            using (var view = JsonDocument.Parse(giarabubMotor.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var place = Array.FindIndex(choices, value => value.GetProperty("poolId").GetString() == "axis-libya-unattached"
                    && value.GetProperty("locationId").GetString() == "c1014" && value.GetProperty("delta").GetInt32() == 1);
                Assert.True(place >= 0);
                Assert.False(journal.SubmitSourceMotorization(commonwealth, giarabubMotor.Revision, giarabubMotor.PublicActionRefs[place]).IsAccepted);
                Assert.True(journal.SubmitSourceMotorization(renewedAxis, giarabubMotor.Revision, giarabubMotor.PublicActionRefs[place]).IsAccepted);
            }
            // Complete actual Commonwealth unattached pool placement before starting cargo.
            for (var attempts = 0; attempts < 20; attempts++)
            {
                var cargoCandidate = journal.QuerySourceUnattachedCargoOpening(commonwealth);
                if (cargoCandidate.PublicActionRefs.Count != 0) break;
                var pool = journal.QuerySourceMotorizationOpening(commonwealth);
                using var view = JsonDocument.Parse(pool.VisibleBytes);
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var next = Enumerable.Range(0, choices.Length).Where(index => choices[index].GetProperty("delta").GetInt32() > 0)
                    .OrderByDescending(index => choices[index].GetProperty("delta").GetInt32()).FirstOrDefault(-1);
                Assert.True(next >= 0);
                Assert.True(journal.SubmitSourceMotorization(commonwealth, pool.Revision, pool.PublicActionRefs[next]).IsAccepted);
            }
            var cwCargo = journal.QuerySourceUnattachedCargoOpening(commonwealth);
            var hiddenAxisCargo = journal.QuerySourceUnattachedCargoOpening(renewedAxis);
            using (var view = JsonDocument.Parse(cwCargo.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var begin = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "begin");
                Assert.True(begin >= 0);
                Assert.False(journal.SubmitSourceUnattachedCargo(renewedAxis, cwCargo.Revision, cwCargo.PublicActionRefs[begin]).IsAccepted);
                Assert.True(journal.SubmitSourceUnattachedCargo(commonwealth, cwCargo.Revision, cwCargo.PublicActionRefs[begin]).IsAccepted);
            }
            cwCargo = journal.QuerySourceUnattachedCargoOpening(commonwealth);
            using (var view = JsonDocument.Parse(cwCargo.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var load = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "adjust" && value.GetProperty("delta").GetInt32() == 1);
                Assert.True(load >= 0);
                Assert.True(journal.SubmitSourceUnattachedCargo(commonwealth, cwCargo.Revision, cwCargo.PublicActionRefs[load]).IsAccepted);
            }
            cwCargo = journal.QuerySourceUnattachedCargoOpening(commonwealth);
            using (var view = JsonDocument.Parse(cwCargo.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var finishIndex = Array.FindIndex(choices, value => value.GetProperty("kind").GetString() == "finish");
                Assert.True(finishIndex >= 0);
                Assert.True(journal.SubmitSourceUnattachedCargo(commonwealth, cwCargo.Revision, cwCargo.PublicActionRefs[finishIndex]).IsAccepted);
            }
            var axisCargoAfter = journal.QuerySourceUnattachedCargoOpening(renewedAxis);
            Assert.Equal(hiddenAxisCargo.VisibleBytes, axisCargoAfter.VisibleBytes);
            Assert.Equal(hiddenAxisCargo.PublicActionRefs, axisCargoAfter.PublicActionRefs);
            var compactOwner = journal.QuerySourceMotorizationOpening(renewedAxis);
            Assert.NotEmpty(compactOwner.PublicActionRefs);
            using (var view = JsonDocument.Parse(compactOwner.VisibleBytes))
            {
                var choices = view.RootElement.GetProperty("choices").EnumerateArray().ToArray();
                var other = Array.FindIndex(choices, value => value.GetProperty("locationId").GetString() != "c1014");
                Assert.True(other >= 0);
                Assert.True(journal.SubmitSourceMotorization(renewedAxis, compactOwner.Revision, compactOwner.PublicActionRefs[other]).IsAccepted);
            }
            checkpoint = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
            var compactChild = await ReadInFreshProcess(path, request.CampaignId, "--source-authority-read");
            Assert.Equal(checkpoint.StateVersion, compactChild.StateVersion);
            var reopenedMotor = CampaignDurableAuthorityJournal.OpenExisting(path).QuerySourceMotorizationOpening(renewedAxis);
            var reopenedLoads = CampaignDurableAuthorityJournal.OpenExisting(path).QuerySourceAttachedInitialLoadsOpening(commonwealth);
            using (var view = JsonDocument.Parse(reopenedLoads.VisibleBytes))
                Assert.Contains(view.RootElement.GetProperty("targets").EnumerateArray(), value =>
                    value.GetProperty("elementId").GetString() == "1-rtr"
                    && value.GetProperty("attachedPoints").GetInt32() == 5
                    && value.GetProperty("fuelPoints").GetInt32() == 3);
            using (var view = JsonDocument.Parse(reopenedMotor.VisibleBytes))
                Assert.Contains(view.RootElement.GetProperty("allocations").EnumerateArray(), value =>
                    value.GetProperty("poolId").GetString() == "axis-libya-unattached"
                    && value.GetProperty("locationId").GetString() == "c1014"
                    && value.GetProperty("points").GetInt32() == 1);
            using (var compactQuery = connection.CreateCommand())
            {
                compactQuery.CommandText = "SELECT snapshot_checkpoint FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence DESC LIMIT 1";
                compactQuery.Parameters.AddWithValue("$id", request.CampaignId);
                var compactBytes = (byte[])compactQuery.ExecuteScalar()!;
                Assert.True(compactBytes.Length < 16_384);
                using var compact = JsonDocument.Parse(compactBytes);
                Assert.Equal(63, compact.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.StartsWith("sha256:", compact.RootElement.GetProperty("cwFrontlineContentHash").GetString());
                using var motorMachineGun = JsonDocument.Parse(compact.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64());
                Assert.Equal(62, motorMachineGun.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.StartsWith("sha256:", motorMachineGun.RootElement.GetProperty("motorMachineGunContentHash").GetString());
                using var remainder = JsonDocument.Parse(motorMachineGun.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64());
                Assert.Equal(61, remainder.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.StartsWith("sha256:", remainder.RootElement.GetProperty("garrisonRemainderContentHash").GetString());
                using var minorGarrisons = JsonDocument.Parse(remainder.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64());
                Assert.Equal(60, minorGarrisons.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.StartsWith("sha256:", minorGarrisons.RootElement.GetProperty("minorGarrisonContentHash").GetString());
                using var compactBase = JsonDocument.Parse(minorGarrisons.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64());
                Assert.Equal(52, compactBase.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.Equal(JsonValueKind.Null, compactBase.RootElement.GetProperty("gameClock").ValueKind);
                Assert.StartsWith("sha256:", compactBase.RootElement.GetProperty("legacyProjectionHash").GetString());
                compactQuery.CommandText = "SELECT snapshot_checkpoint FROM accepted_steps WHERE campaign_id=$id AND sequence=$seq";
                compactQuery.Parameters.AddWithValue("$seq", legacyHeadSequence);
                Assert.Equal(finalCheckpointBytes, (byte[])compactQuery.ExecuteScalar()!);
            }
            using var corruptConnection = new SqliteConnection($"Data Source={path};Mode=ReadWrite;Pooling=False");
            corruptConnection.Open();
            void CheckCorruptRecordHash(long sequence)
            {
                using var read = corruptConnection.CreateCommand();
                read.CommandText = "SELECT record_hash FROM accepted_steps WHERE campaign_id=$id AND sequence=$seq";
                read.Parameters.AddWithValue("$id", request.CampaignId);
                read.Parameters.AddWithValue("$seq", sequence);
                var saved = (string)read.ExecuteScalar()!;
                using var update = corruptConnection.CreateCommand();
                update.CommandText = "UPDATE accepted_steps SET record_hash=$hash WHERE campaign_id=$id AND sequence=$seq";
                update.Parameters.AddWithValue("$id", request.CampaignId);
                update.Parameters.AddWithValue("$seq", sequence);
                var hashParameter = update.Parameters.AddWithValue("$hash", new string('0', 64));
                Assert.Equal(1, update.ExecuteNonQuery());
                Assert.Throws<InvalidOperationException>(() => CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
                hashParameter.Value = saved;
                Assert.Equal(1, update.ExecuteNonQuery());
            }
            CheckCorruptRecordHash(1);
            using (var sourceDivision = corruptConnection.CreateCommand())
            {
                sourceDivision.CommandText = "SELECT sequence FROM accepted_steps WHERE campaign_id=$id AND action_kind=$kind";
                sourceDivision.Parameters.AddWithValue("$id", request.CampaignId);
                var kind = sourceDivision.Parameters.AddWithValue("$kind", "source-italian-divisions-grouped-world");
                CheckCorruptRecordHash(Assert.IsType<long>(sourceDivision.ExecuteScalar()));
                kind.Value = "source-seventh-armored-grouped-world";
                CheckCorruptRecordHash(Assert.IsType<long>(sourceDivision.ExecuteScalar()));
                kind.Value = "source-sixteenth-brigade-grouped-world";
                CheckCorruptRecordHash(Assert.IsType<long>(sourceDivision.ExecuteScalar()));
                kind.Value = "source-catanzaro-grouped-world";
                CheckCorruptRecordHash(Assert.IsType<long>(sourceDivision.ExecuteScalar()));
            }
            CheckCorruptRecordHash(checkpoint.StateVersion - 1);
            using (var missing = corruptConnection.CreateCommand())
            {
                missing.CommandText = "UPDATE accepted_steps SET sequence=$to WHERE campaign_id=$id AND sequence=$from";
                missing.Parameters.AddWithValue("$id", request.CampaignId);
                var from = missing.Parameters.AddWithValue("$from", checkpoint.StateVersion - 1);
                var to = missing.Parameters.AddWithValue("$to", checkpoint.StateVersion + 1000);
                Assert.Equal(1, missing.ExecuteNonQuery());
                Assert.Throws<InvalidOperationException>(() => CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
                from.Value = checkpoint.StateVersion + 1000;
                to.Value = checkpoint.StateVersion - 1;
                Assert.Equal(1, missing.ExecuteNonQuery());
            }
            using (var truncated = corruptConnection.CreateCommand())
            {
                truncated.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id AND sequence=$seq";
                truncated.Parameters.AddWithValue("$id", request.CampaignId);
                truncated.Parameters.AddWithValue("$seq", checkpoint.StateVersion - 1);
                var savedEvent = (byte[])truncated.ExecuteScalar()!;
                truncated.CommandText = "UPDATE accepted_steps SET canonical_event=$event WHERE campaign_id=$id AND sequence=$seq";
                var eventParameter = truncated.Parameters.AddWithValue("$event", savedEvent[..^1]);
                Assert.Equal(1, truncated.ExecuteNonQuery());
                Assert.Throws<InvalidOperationException>(() => CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
                eventParameter.Value = savedEvent;
                Assert.Equal(1, truncated.ExecuteNonQuery());
            }
            using (var head = corruptConnection.CreateCommand())
            {
                head.CommandText = "SELECT head_hash FROM campaigns WHERE campaign_id=$id";
                head.Parameters.AddWithValue("$id", request.CampaignId);
                var savedHead = (string)head.ExecuteScalar()!;
                head.CommandText = "UPDATE campaigns SET head_hash=$hash WHERE campaign_id=$id";
                var headHash = head.Parameters.AddWithValue("$hash", new string('0', 64));
                Assert.Equal(1, head.ExecuteNonQuery());
                Assert.Throws<InvalidOperationException>(() => CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
                headHash.Value = savedHead;
                Assert.Equal(1, head.ExecuteNonQuery());
            }
            using var corrupt = corruptConnection.CreateCommand();
            corrupt.CommandText = "UPDATE accepted_steps SET snapshot_checkpoint=zeroblob(25165825) WHERE campaign_id=$id AND sequence=(SELECT MAX(sequence) FROM accepted_steps WHERE campaign_id=$id)";
            corrupt.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1, corrupt.ExecuteNonQuery());
            Assert.Throws<InvalidOperationException>(() => CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task BundledGrazianiSetupUsesTheExistingJournalAndSourceIdentity()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-bundled-api-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal));
        await using var app = builder.Build();
        app.MapDurableCampaignApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            // Not a same-origin-gate test (besides the explicit crossOrigin cases below);
            // simulate a genuine same-origin fetch so the v2 middleware's stricter
            // header-presence check (#319) doesn't block the other requests here.
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            var body = new
            {
                contractVersion = 2,
                campaignId = "graziani-bundled",
                seed = 0,
                capabilityNumerator = 0,
                capabilityDenominator = 1,
                cohesion = 0
            };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source/graziani",
                body, TestContext.Current.CancellationToken)).StatusCode);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            using (var crossOrigin = new HttpRequestMessage(HttpMethod.Post, "/api/v2/campaigns/pregame-source/graziani")
            { Content = JsonContent.Create(body) })
            {
                crossOrigin.Headers.Add("Origin", "https://untrusted.example");
                Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossOrigin,
                    TestContext.Current.CancellationToken)).StatusCode);
            }
            var created = await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source/graziani", body,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var acknowledgment = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal("incomplete", acknowledgment.RootElement.GetProperty("status").GetString());
            Assert.Equal("provisional", acknowledgment.RootElement.GetProperty("policyStatus").GetString());
            Assert.DoesNotContain("seed", acknowledgment.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
            var invalidCounterQuery = new
            {
                contractVersion = 2,
                credential = new
                {
                    campaignId = "graziani-bundled",
                    audience = "axis",
                    epoch = 1,
                    secret = new string('0', 64),
                    expiresUnixMs = long.MaxValue
                },
                counterId = "scheferzen-garrison"
            };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
                "/api/v2/campaigns/graziani-bundled/source-counter-contacts", invalidCounterQuery,
                TestContext.Current.CancellationToken)).StatusCode);
            const string italianRoute = "/api/v2/campaigns/graziani-bundled/source-italian-divisions-grouped-world";
            const string seventhRoute = "/api/v2/campaigns/graziani-bundled/source-seventh-armored-grouped-world";
            const string sixteenthRoute = "/api/v2/campaigns/graziani-bundled/source-sixteenth-brigade-grouped-world";
            const string catanzaroRoute = "/api/v2/campaigns/graziani-bundled/source-catanzaro-grouped-world";
            const string sourceStartRoute = "/api/v2/campaigns/graziani-bundled/source-turn/start";
            const string sourceInitiativeRoute = "/api/v2/campaigns/graziani-bundled/source-turn/initiative";
            const string sourceSchedulingRoute = "/api/v2/campaigns/graziani-bundled/source-turn/complete-gt1-scheduling";
            const string sourceWeatherRoute = "/api/v2/campaigns/graziani-bundled/source-turn/gt1-weather";
            const string sourceLaneRoute = "/api/v2/campaigns/graziani-bundled/source-turn/gt1-lane2";
            const string sourceLane3Route = "/api/v2/campaigns/graziani-bundled/source-turn/gt1-lane3";
            const string shellCorrectionRoute = "/api/v2/campaigns/graziani-bundled/source-seventh-shell-correction";
            const string minorGarrisonsRoute = "/api/v2/campaigns/graziani-bundled/source-axis-minor-garrisons-activation";
            const string remainderRoute = "/api/v2/campaigns/graziani-bundled/source-axis-garrison-remainder-activation";
            const string motorMachineGunRoute = "/api/v2/campaigns/graziani-bundled/source-giarabub-motor-machine-gun-activation";
            const string cwFrontlineRoute = "/api/v2/campaigns/graziani-bundled/source-cw-frontline-activation";
            const string giarabubPolicyRoute = "/api/v2/campaigns/graziani-bundled/source-giarabub-motor-placement-policy";
            const string sourceMultiLaneRoute = "/api/v2/campaigns/graziani-bundled/source-turn/gt1-multiple-lanes";
            client.DefaultRequestHeaders.Remove("X-Sandtable-Operator");
            foreach (var route in new[] { italianRoute, seventhRoute, sixteenthRoute, catanzaroRoute, sourceStartRoute, sourceInitiativeRoute, sourceSchedulingRoute, sourceWeatherRoute, sourceLaneRoute, sourceLane3Route, shellCorrectionRoute, minorGarrisonsRoute, remainderRoute, motorMachineGunRoute, cwFrontlineRoute, giarabubPolicyRoute, sourceMultiLaneRoute })
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(route, null,
                    TestContext.Current.CancellationToken)).StatusCode);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            foreach (var route in new[] { italianRoute, seventhRoute, sixteenthRoute, catanzaroRoute, sourceStartRoute, sourceInitiativeRoute, sourceSchedulingRoute, sourceWeatherRoute, sourceLaneRoute, sourceLane3Route, shellCorrectionRoute, minorGarrisonsRoute, remainderRoute, motorMachineGunRoute, cwFrontlineRoute, giarabubPolicyRoute, sourceMultiLaneRoute })
                Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(route, null,
                    TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source/graziani",
                body, TestContext.Current.CancellationToken)).StatusCode);
            var conflict = new
            {
                body.contractVersion,
                body.campaignId,
                seed = 1,
                body.capabilityNumerator,
                body.capabilityDenominator,
                body.cohesion
            };
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source/graziani",
                conflict, TestContext.Current.CancellationToken)).StatusCode);

            // The older source upload endpoint and the bundled endpoint bind the same request.
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var upload = new
            {
                body.contractVersion,
                body.campaignId,
                rulesetHash = Cna1979Ruleset.Manifest.Hash,
                body.seed,
                scenarioId = Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
                expectedContentHash = content.Identity.Hash,
                contentBase64 = Convert.ToBase64String(content.CanonicalBytes),
                body.capabilityNumerator,
                body.capabilityDenominator,
                body.cohesion
            };
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source", upload,
                TestContext.Current.CancellationToken)).StatusCode);
            var grant = journal.IssueSeatGrant(body.campaignId, CampaignActionAudience.Axis, Secret);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(grant));
            var opening = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, opening.Revision,
                Assert.Single(opening.PublicActionRefs)).IsAccepted);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(body.campaignId);
            var checkpoint = CampaignExercises.QueryCheckpoint(reopened);
            Assert.Equal(2, checkpoint.StateVersion);
            Assert.True(CampaignExercises.Query(reopened, CampaignActionAudience.Axis).IsSuccessful);
            Assert.False(CampaignExercises.Query(reopened, CampaignActionAudience.Commonwealth).IsSuccessful);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task LocalApiAuthorizesOnlyAxisAndRedactsPregameChoicesAndReceipts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-pregame-api-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
        var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal);
        var silo = await StartOutwardApiSiloAsync(store);
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(silo.Services.GetRequiredService<IGrainFactory>());
        await using var app = builder.Build();
        app.MapDurableCampaignApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            // Not a same-origin-gate test (besides the explicit crossOrigin cases below);
            // simulate a genuine same-origin fetch so the v2 middleware's stricter
            // header-presence check (#319) doesn't block the other requests here.
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var body = new
            {
                contractVersion = 2,
                campaignId = "graziani-pregame-api",
                rulesetHash = Cna1979Ruleset.Manifest.Hash,
                seed = 0,
                scenarioId = Cna1979GrazianiCairoSourceExcerpt.ScenarioId,
                expectedContentHash = content.Identity.Hash,
                contentBase64 = Convert.ToBase64String(content.CanonicalBytes),
                capabilityNumerator = 0,
                capabilityDenominator = 1,
                cohesion = 0
            };
            var denied = await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source", body, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            using (var crossOrigin = new HttpRequestMessage(HttpMethod.Post, "/api/v2/campaigns/pregame-source")
            { Content = JsonContent.Create(body) })
            {
                crossOrigin.Headers.Add("Origin", "https://untrusted.example");
                Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossOrigin,
                    TestContext.Current.CancellationToken)).StatusCode);
            }
            var bad = new
            {
                body.contractVersion,
                campaignId = "wrong-content-hash",
                body.rulesetHash,
                body.seed,
                body.scenarioId,
                expectedContentHash = "sha256:" + new string('0', 64),
                body.contentBase64,
                body.capabilityNumerator,
                body.capabilityDenominator,
                body.cohesion
            };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source", bad,
                TestContext.Current.CancellationToken)).StatusCode);
            var created = await client.PostAsJsonAsync("/api/v2/campaigns/pregame-source", body, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var acknowledgment = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal("incomplete", acknowledgment.RootElement.GetProperty("status").GetString());
            Assert.Equal("provisional", acknowledgment.RootElement.GetProperty("policyStatus").GetString());
            Assert.DoesNotContain("seed", acknowledgment.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(
                "/api/v2/campaigns/graziani-pregame-api/grants/commonwealth", new { }, TestContext.Current.CancellationToken)).StatusCode);
            var grantResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/grants/axis",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
            using var grant = JsonDocument.Parse(await grantResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var seatResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/seats",
                new { contractVersion = 2, grant = grant.RootElement.GetProperty("grant") }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, seatResponse.StatusCode);
            using var seat = JsonDocument.Parse(await seatResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var credential = seat.RootElement.GetProperty("credential");
            client.DefaultRequestHeaders.Remove("X-Sandtable-Operator");
            var ordinary = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings",
                new { contractVersion = 2, credential, stage = "movement", actingElementId = "axis-infantry" },
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, ordinary.StatusCode);
            // API shape-review, 2026-11-23, Recommendation 6: requestId is ASP.NET Core's own
            // per-request trace identifier, so it's checked for presence rather than folded into
            // the exact-body literal below, which still pins contractVersion/code precisely.
            using var ordinaryBody = JsonDocument.Parse(
                await ordinary.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, ordinaryBody.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("unavailable", ordinaryBody.RootElement.GetProperty("code").GetString());
            Assert.False(string.IsNullOrEmpty(
                ordinaryBody.RootElement.GetProperty("requestId").GetString()));
            var openingBody = new
            {
                contractVersion = 2,
                credential,
                stage = "pregame-axis-supply",
                actingElementId = "land.pregame-axis-supply"
            };
            var forgedCredential = new
            {
                campaignId = body.campaignId,
                audience = "axis",
                epoch = credential.GetProperty("epoch").GetInt64(),
                secret = new string('0', 64),
                expiresUnixMs = credential.GetProperty("expiresUnixMs").GetInt64()
            };
            var unauthorized = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings",
                new
                {
                    contractVersion = 2,
                    credential = forgedCredential,
                    stage = "pregame-axis-supply",
                    actingElementId = "land.pregame-axis-supply"
                }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
            using (var crossOrigin = new HttpRequestMessage(HttpMethod.Post, "/api/v2/campaigns/graziani-pregame-api/openings")
            { Content = JsonContent.Create(openingBody) })
            {
                crossOrigin.Headers.Add("Origin", "https://untrusted.example");
                Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossOrigin,
                    TestContext.Current.CancellationToken)).StatusCode);
            }
            var openingResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings", openingBody,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, openingResponse.StatusCode);
            using var opening = JsonDocument.Parse(await openingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var visible = Convert.FromBase64String(opening.RootElement.GetProperty("visibleCutBase64").GetString()!);
            AssertSafeVisible(visible, CampaignExercises.Query(journal.ReadVerified(body.campaignId), CampaignActionAudience.Axis).ActionSet!);
            var reference = Assert.Single(opening.RootElement.GetProperty("publicActionRefs").EnumerateArray().ToArray()).GetString();
            var action = new
            {
                contractVersion = 2,
                credential,
                stage = "pregame-axis-supply",
                actingElementId = "land.pregame-axis-supply",
                openingRevision = opening.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = reference
            };
            var accepted = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions", action,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            var receipt = await accepted.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("actionId", receipt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("seed", receipt, StringComparison.OrdinalIgnoreCase);
            var retry = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions", action,
                TestContext.Current.CancellationToken);
            Assert.Equal(receipt, await retry.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var allocationResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings", openingBody,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, allocationResponse.StatusCode);
            using var allocationOpening = JsonDocument.Parse(await allocationResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var allocationVisible = Convert.FromBase64String(allocationOpening.RootElement.GetProperty("visibleCutBase64").GetString()!);
            AssertRoutes(allocationVisible);
            var options = Choices(allocationVisible);
            var selected = Array.FindIndex(options, value => value.Kind == "add" && value.Lane == 2 && value.Stage == 1);
            var stale = Array.FindIndex(options, value => value.Kind == "add" && value.Lane == 3 && value.Stage == 1);
            Assert.True(selected >= 0 && stale >= 0);
            var references = allocationOpening.RootElement.GetProperty("publicActionRefs").EnumerateArray().ToArray();
            var revision = allocationOpening.RootElement.GetProperty("revision").GetInt64();
            var allocate = new
            {
                contractVersion = 2,
                credential,
                stage = "pregame-axis-supply",
                actingElementId = "land.pregame-axis-supply",
                openingRevision = revision,
                publicActionRef = references[selected].GetString()
            };
            var allocationResult = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions", allocate,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, allocationResult.StatusCode);
            var staleAction = new
            {
                contractVersion = 2,
                credential,
                stage = "pregame-axis-supply",
                actingElementId = "land.pregame-axis-supply",
                openingRevision = revision,
                publicActionRef = references[stale].GetString()
            };
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions",
                staleAction, TestContext.Current.CancellationToken)).StatusCode);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(body.campaignId);
            Assert.Equal(3, CampaignExercises.QueryCheckpoint(reopened).StateVersion);
            var finishBody = new
            {
                contractVersion = 2,
                credential,
                stage = "pregame-axis-supply-finish",
                actingElementId = "land.pregame-axis-supply-finish"
            };
            var finishResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings", finishBody,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);
            using var finishOpening = JsonDocument.Parse(await finishResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var finishAction = new
            {
                finishBody.contractVersion,
                finishBody.credential,
                finishBody.stage,
                finishBody.actingElementId,
                openingRevision = finishOpening.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = Assert.Single(finishOpening.RootElement.GetProperty("publicActionRefs").EnumerateArray().ToArray()).GetString()
            };
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions",
                finishAction, TestContext.Current.CancellationToken)).StatusCode);
            var dumpBody = new
            {
                contractVersion = 2,
                credential,
                stage = "source-actions",
                actingElementId = "land.graziani-source-actions"
            };
            var dumpResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings", dumpBody,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, dumpResponse.StatusCode);
            using var dumpOpening = JsonDocument.Parse(await dumpResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            using var sourceView = JsonDocument.Parse(dumpOpening.RootElement.GetProperty("visibleCutBase64").GetBytesFromBase64());
            var sourceChoices = sourceView.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            var dumpIndex = Array.FindIndex(sourceChoices, value => value.GetProperty("family").GetString() == "axis-dump");
            Assert.True(dumpIndex >= 0);
            Assert.DoesNotContain("actionId", sourceView.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
            var dumpAction = new
            {
                dumpBody.contractVersion,
                dumpBody.credential,
                dumpBody.stage,
                dumpBody.actingElementId,
                openingRevision = dumpOpening.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = dumpOpening.RootElement.GetProperty("publicActionRefs")[dumpIndex].GetString()
            };
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions",
                dumpAction, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(5, CampaignExercises.QueryCheckpoint(CampaignDurableAuthorityJournal.OpenExisting(path)
                .ReadVerified(body.campaignId)).StateVersion);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            var cwGrantResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/grants/commonwealth",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cwGrantResponse.StatusCode);
            using var cwGrant = JsonDocument.Parse(await cwGrantResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var cwSeatResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/seats",
                new { contractVersion = 2, grant = cwGrant.RootElement.GetProperty("grant") }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cwSeatResponse.StatusCode);
            using var cwSeat = JsonDocument.Parse(await cwSeatResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var cwCredential = cwSeat.RootElement.GetProperty("credential");
            client.DefaultRequestHeaders.Remove("X-Sandtable-Operator");
            var cwBody = new
            {
                contractVersion = 2,
                credential = cwCredential,
                stage = "source-commonwealth-supply",
                actingElementId = "land.graziani-commonwealth-supply"
            };
            var cwResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings", cwBody,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cwResponse.StatusCode);
            using var cwOpening = JsonDocument.Parse(await cwResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var cwAction = new
            {
                cwBody.contractVersion,
                cwBody.credential,
                cwBody.stage,
                cwBody.actingElementId,
                openingRevision = cwOpening.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = cwOpening.RootElement.GetProperty("publicActionRefs")[0].GetString()
            };
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions",
                cwAction, TestContext.Current.CancellationToken)).StatusCode);
            var crossSide = new { contractVersion = 2, credential = cwCredential, stage = "source-axis-dump", actingElementId = "land.graziani-axis-dumps" };
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings",
                crossSide, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
                "/api/v2/campaigns/graziani-pregame-api/source-axis-supplement", new { }, TestContext.Current.CancellationToken)).StatusCode);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            var supplementResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-axis-supplement",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, supplementResponse.StatusCode);
            using var supplement = JsonDocument.Parse(await supplementResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.True(supplement.RootElement.GetProperty("admitted").GetBoolean());
            var retrySupplement = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-axis-supplement",
                new { }, TestContext.Current.CancellationToken);
            using var retryBody = JsonDocument.Parse(await retrySupplement.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.False(retryBody.RootElement.GetProperty("admitted").GetBoolean());
            var cwSupplement = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-commonwealth-supplement",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cwSupplement.StatusCode);
            using var cwSupplementBody = JsonDocument.Parse(await cwSupplement.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.True(cwSupplementBody.RootElement.GetProperty("admitted").GetBoolean());
            var priorityPolicy = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-priority-motorization",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, priorityPolicy.StatusCode);
            var axisPriorityPolicy = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-axis-priority-motorization",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, axisPriorityPolicy.StatusCode);
            var minorGarrisons = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-axis-minor-garrisons",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, minorGarrisons.StatusCode);
            var battlefield = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-battlefield",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, battlefield.StatusCode);
            var cairoSetup = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-cairo-setup",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cairoSetup.StatusCode);
            var profilesResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/source-profiles",
                new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, profilesResponse.StatusCode);
            using (var profiles = JsonDocument.Parse(await profilesResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
                Assert.Equal(0, profiles.RootElement.GetProperty("admitted").GetInt32());
            client.DefaultRequestHeaders.Remove("X-Sandtable-Operator");
            var cairoBody = new
            {
                contractVersion = 2,
                credential = cwCredential,
                stage = "source-cairo-setup",
                actingElementId = "land.graziani-commonwealth-cairo-setup"
            };
            var cairoResponse = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/openings", cairoBody,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cairoResponse.StatusCode);
            using var cairoOpening = JsonDocument.Parse(await cairoResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var cairoAction = new
            {
                cairoBody.contractVersion,
                cairoBody.credential,
                cairoBody.stage,
                cairoBody.actingElementId,
                openingRevision = cairoOpening.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = cairoOpening.RootElement.GetProperty("publicActionRefs")[0].GetString()
            };
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/campaigns/graziani-pregame-api/actions",
                cairoAction, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
                "/api/v2/campaigns/graziani-pregame-api/source-cairo-world", new { }, TestContext.Current.CancellationToken)).StatusCode);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(
                "/api/v2/campaigns/graziani-pregame-api/source-cairo-world", new { }, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(14, CampaignExercises.QueryCheckpoint(CampaignDurableAuthorityJournal.OpenExisting(path)
                .ReadVerified(body.campaignId)).StateVersion);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            await StopOutwardApiSiloAsync(silo);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RealAxisPregameSupplyChoicesUsePrivateRefsAndColdReplay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-pregame-journal-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            const ulong seed = 0; // The seeded first d6 roll is 6: three supply units.
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-axis-pregame", Cna1979Ruleset.Manifest.Hash, seed,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, init),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            var session = journal.CreatePregameSourceCampaign(request, content, init);
            var initial = CampaignExercises.QueryCheckpoint(session);
            Assert.Equal(1, initial.StateVersion);
            Assert.Equal(initial.StateVersion, CampaignExercises.QueryCheckpoint(
                journal.CreatePregameSourceCampaign(request, content, init)).StateVersion);
            Assert.False(CampaignExercises.Query(session, CampaignActionAudience.Commonwealth).IsSuccessful);
            var rawSet = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
            var rawCommand = new CampaignActionSubmission(1, rawSet.CampaignId, rawSet.StateVersion,
                rawSet.PositionId, rawSet.Audience, Assert.Single(rawSet.Candidates).ActionId);
            var beforeBypass = ReadHead(path, request.CampaignId);
            Assert.Throws<InvalidOperationException>(() => journal.Append(request.CampaignId, rawCommand));
            Assert.Equal(beforeBypass, ReadHead(path, request.CampaignId));
            Assert.Equal(initial.StateVersion, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
            var directRoll = SubmitDirect(session, candidate => candidate is PregameAxisSupplyRollAction);
            Assert.True(directRoll.IsAccepted);
            var directAllocation = SubmitDirect(directRoll.SuccessorSession!, candidate =>
                candidate is PregameAxisSupplyAllocationAction { Lane: 2, Stage: 1, Delta: 1 });
            Assert.True(directAllocation.IsAccepted);
            Assert.Throws<InvalidOperationException>(() => journal.IssueSeatGrant(request.CampaignId,
                CampaignActionAudience.Commonwealth, Secret));
            var grant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(grant));

            var rollOpening = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.Equal(CampaignActionAudience.Axis, rollOpening.Audience);
            var rollRef = Assert.Single(rollOpening.PublicActionRefs);
            AssertSafeVisible(rollOpening.VisibleBytes, CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!);
            var rolled = journal.SubmitPregameAxisSupply(axis, rollOpening.Revision, rollRef);
            Assert.True(rolled.IsAccepted);
            var retried = journal.SubmitPregameAxisSupply(axis, rollOpening.Revision, rollRef);
            Assert.True(retried.IsAccepted);
            Assert.Equal(rolled.CanonicalReceipt, retried.CanonicalReceipt);

            var allocationOpening = journal.QueryPregameAxisSupplyOpening(axis);
            AssertRoutes(allocationOpening.VisibleBytes);
            var choices = Choices(allocationOpening.VisibleBytes);
            var chosen = Array.FindIndex(choices, value => value.Kind == "add" && value.Lane == 2 && value.Stage == 1);
            var stale = Array.FindIndex(choices, value => value.Kind == "add" && value.Lane == 3 && value.Stage == 1);
            Assert.True(chosen >= 0 && stale >= 0);
            var accepted = journal.SubmitPregameAxisSupply(axis, allocationOpening.Revision,
                allocationOpening.PublicActionRefs[chosen]);
            Assert.True(accepted.IsAccepted);
            Assert.False(journal.SubmitPregameAxisSupply(axis, allocationOpening.Revision,
                allocationOpening.PublicActionRefs[stale]).IsAccepted);
            var after = journal.QueryPregameAxisSupplyOpening(axis);
            using (var visible = JsonDocument.Parse(after.VisibleBytes))
            {
                Assert.Equal("incomplete", visible.RootElement.GetProperty("status").GetString());
                Assert.Equal("provisional", visible.RootElement.GetProperty("policyStatus").GetString());
            }
            Assert.DoesNotContain(Choices(after.VisibleBytes), choice => choice.Kind == "finish");
            var afterSession = journal.ReadVerified(request.CampaignId);
            var current = CampaignExercises.QueryCheckpoint(afterSession);
            Assert.Equal(3, current.StateVersion);
            Assert.Equal(initial.PositionId, current.PositionId);
            AssertSafeVisible(after.VisibleBytes, CampaignExercises.Query(afterSession, CampaignActionAudience.Axis).ActionSet!);

            byte[] creation, initialSnapshot, rolledCommand, rolledEvent, rolledReceipt, rolledCheckpoint,
                allocationCommand, allocationEvent, allocationReceipt, finalCheckpoint;
            using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
            {
                connection.Open();
                using var query = connection.CreateCommand();
                query.CommandText = """
                    SELECT c.creation_event, c.initial_snapshot,
                      s1.canonical_command, s1.canonical_event, s1.canonical_receipt, s1.snapshot_checkpoint,
                      s2.canonical_command, s2.canonical_event, s2.canonical_receipt, s2.snapshot_checkpoint,
                      (SELECT count(*) FROM accepted_steps WHERE campaign_id=c.campaign_id)
                    FROM campaigns c JOIN accepted_steps s1 ON s1.campaign_id=c.campaign_id AND s1.sequence=1
                    JOIN accepted_steps s2 ON s2.campaign_id=c.campaign_id AND s2.sequence=2
                    WHERE c.campaign_id=$id
                    """;
                query.Parameters.AddWithValue("$id", request.CampaignId);
                using var row = query.ExecuteReader();
                Assert.True(row.Read());
                creation = row.GetFieldValue<byte[]>(0);
                initialSnapshot = row.GetFieldValue<byte[]>(1);
                rolledCommand = row.GetFieldValue<byte[]>(2);
                rolledEvent = row.GetFieldValue<byte[]>(3);
                rolledReceipt = row.GetFieldValue<byte[]>(4);
                rolledCheckpoint = row.GetFieldValue<byte[]>(5);
                allocationCommand = row.GetFieldValue<byte[]>(6);
                allocationEvent = row.GetFieldValue<byte[]>(7);
                allocationReceipt = row.GetFieldValue<byte[]>(8);
                finalCheckpoint = row.GetFieldValue<byte[]>(9);
                Assert.Equal(2, row.GetInt32(10));
            }
            Assert.Equal(Assert.Single(directRoll.Evidence!.EventRecords), rolledEvent);
            Assert.Equal(directRoll.Evidence.SnapshotCheckpoint, rolledCheckpoint);
            Assert.Equal(Assert.Single(directAllocation.Evidence!.EventRecords), allocationEvent);
            Assert.Equal(directAllocation.Evidence.SnapshotCheckpoint, finalCheckpoint);
            var rollStep = new CampaignHostAcceptedStep(rolledCommand, [rolledEvent], rolledReceipt,
                rolledCheckpoint, CampaignHostAcceptedStep.PregameAxisSupplyKind, "land.pregame-axis-supply");
            var allocationStep = new CampaignHostAcceptedStep(allocationCommand, [allocationEvent], allocationReceipt,
                finalCheckpoint, CampaignHostAcceptedStep.PregameAxisSupplyKind, "land.pregame-axis-supply");
            Assert.True(CampaignHostAdapter.ImportPregameSource(request, creation, initialSnapshot,
                [rollStep, allocationStep]).IsVerified);
            var malformedBase64 = JsonNode.Parse(creation)!.AsObject();
            malformedBase64["sourceCreationBase64"] = "%%%";
            Assert.Equal(CampaignHostImportRejectionReason.InvalidCreation,
                CampaignHostAdapter.ImportPregameSource(request,
                    Encoding.UTF8.GetBytes(malformedBase64.ToJsonString()), initialSnapshot, []).RejectionReason);
            var missingNestedSetup = JsonNode.Parse(creation)!.AsObject();
            missingNestedSetup["sourceCreationBase64"] = Convert.ToBase64String("{}"u8.ToArray());
            Assert.Equal(CampaignHostImportRejectionReason.InvalidCreation,
                CampaignHostAdapter.ImportPregameSource(request,
                    Encoding.UTF8.GetBytes(missingNestedSetup.ToJsonString()), initialSnapshot, []).RejectionReason);
            var wrongSourceKind = new CampaignHostAcceptedStep(rolledCommand, [rolledEvent], rolledReceipt,
                rolledCheckpoint);
            Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
                CampaignHostAdapter.ImportPregameSource(request, creation, initialSnapshot,
                    [wrongSourceKind]).RejectionReason);
            var normalRequest = new CampaignCreationRequest(CampaignCreationRequest.CurrentContractVersion,
                "normal-kind-crosscheck", Cna1979Ruleset.Manifest.Hash, 12345,
                "rules-lab.breakdown.truck.v1", TemporaryCampaignGrain.SetupHash,
                "rules-lab.content.breakdown-truck.v1", TemporaryCampaignGrain.ContentHash,
                TemporaryCampaignGrain.ScenarioId);
            var normalStart = CampaignHostAdapter.Create(normalRequest);
            Assert.True(normalStart.IsStarted);
            Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
                CampaignHostAdapter.Import(normalRequest, normalStart.CreationEventBytes!,
                    normalStart.InitialSnapshotBytes!, [rollStep]).RejectionReason);
            var forgedEvent = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(allocationEvent)
                .Replace("\"lane\":2", "\"lane\":3", StringComparison.Ordinal));
            Assert.False(forgedEvent.AsSpan().SequenceEqual(allocationEvent));
            var forgedStep = new CampaignHostAcceptedStep(allocationCommand, [forgedEvent], allocationReceipt,
                finalCheckpoint, CampaignHostAcceptedStep.PregameAxisSupplyKind, "land.pregame-axis-supply");
            Assert.Equal(CampaignHostImportRejectionReason.InvalidStep,
                CampaignHostAdapter.ImportPregameSource(request, creation, initialSnapshot,
                    [rollStep, forgedStep]).RejectionReason);
            using (var initialJson = JsonDocument.Parse(initialSnapshot))
            using (var rolledJson = JsonDocument.Parse(rolledCheckpoint))
            using (var finalJson = JsonDocument.Parse(finalCheckpoint))
            {
                var firstCursor = initialJson.RootElement.GetProperty("randomCursor").GetInt64();
                var rollCursor = rolledJson.RootElement.GetProperty("randomCursor").GetInt64();
                Assert.Equal(0, firstCursor);
                Assert.True(rollCursor > firstCursor);
                Assert.Equal(rollCursor, finalJson.RootElement.GetProperty("randomCursor").GetInt64());
                Assert.Equal(seed, finalJson.RootElement.GetProperty("randomSeed").GetUInt64());
                Assert.True(finalJson.RootElement.GetProperty("maximumSupply").GetInt32() > 0);
                var allocation = Assert.Single(finalJson.RootElement.GetProperty("allocations").EnumerateArray().ToArray());
                Assert.Equal(2, allocation.GetProperty("lane").GetInt32());
                Assert.Equal(1, allocation.GetProperty("operationStage").GetInt32());
                Assert.Equal(1, allocation.GetProperty("units").GetInt32());
                var source = finalJson.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64();
                Assert.Equal(initialJson.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64(), source);
                using var sourceJson = JsonDocument.Parse(source);
                var pending = sourceJson.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray()
                    .Select(value => value.GetString()!).Order(StringComparer.Ordinal);
                Assert.Equal(new[] { Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId,
                    Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId }.Order(StringComparer.Ordinal), pending);
            }
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            var freshOpening = reopened.QueryPregameAxisSupplyOpening(axis);
            Assert.Equal(after.VisibleBytes, freshOpening.VisibleBytes);
            Assert.Equal(after.PublicActionRefs, freshOpening.PublicActionRefs);
            var child = await ReadInFreshProcess(path, request.CampaignId);
            Assert.NotEqual(Environment.ProcessId, child.ProcessId);
            Assert.Equal(current.StateVersion, child.StateVersion);
            Assert.Equal(current.PositionId, child.PositionId);
            Assert.Equal(after.VisibleBytes, Convert.FromBase64String(child.VisibleBase64));

            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var sql = connection.CreateCommand();
                var forgedSecret = new string('a', 64);
                var digestMethod = typeof(CampaignDurableAuthorityJournal).GetMethod(
                    "CredentialDigest", BindingFlags.NonPublic | BindingFlags.Static)!;
                var grantDigest = (byte[])digestMethod.Invoke(null, ["seat-grant", forgedSecret])!;
                var credentialDigest = (byte[])digestMethod.Invoke(null, ["seat-credential", forgedSecret])!;
                sql.CommandText = "INSERT INTO seats VALUES ($id,'commonwealth',1,$grant,$expires,$credential,$expires)";
                sql.Parameters.AddWithValue("$id", request.CampaignId);
                sql.Parameters.AddWithValue("$grant", grantDigest);
                sql.Parameters.AddWithValue("$credential", credentialDigest);
                sql.Parameters.AddWithValue("$expires", long.MaxValue);
                Assert.Equal(1, sql.ExecuteNonQuery());
                Assert.Null(journal.RedeemSeatGrant(new CampaignSeatGrant(request.CampaignId,
                    CampaignActionAudience.Commonwealth, 1, forgedSecret, long.MaxValue)));
                Assert.Null(journal.AuthenticateSeat(new CampaignSeatCredential(request.CampaignId,
                    CampaignActionAudience.Commonwealth, 1, forgedSecret, long.MaxValue)));
                Assert.Throws<InvalidOperationException>(() => journal.ReadVerified(request.CampaignId));
                sql.CommandText = "DELETE FROM seats WHERE campaign_id=$id AND side='commonwealth'";
                Assert.Equal(1, sql.ExecuteNonQuery());
                sql.CommandText = "UPDATE accepted_steps SET canonical_event=x'00' WHERE campaign_id=$id AND sequence=2";
                Assert.Equal(1, sql.ExecuteNonQuery());
            }
            Assert.Throws<InvalidOperationException>(() =>
                CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static void AssertSafeVisible(byte[] bytes, CampaignLegalActionSet actions)
    {
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var forbidden in new[] { "randomSeed", "randomCursor", "sourceSnapshotBase64", "actionId", "commonwealth" })
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        foreach (var candidate in actions.Candidates)
            Assert.DoesNotContain(candidate.ActionId, text, StringComparison.Ordinal);
    }

    private static string ReadCheckpointString(string path, string campaignId, string property)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var sql = connection.CreateCommand();
        sql.CommandText = "SELECT snapshot_checkpoint FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence DESC LIMIT 1";
        sql.Parameters.AddWithValue("$id", campaignId);
        using var checkpoint = JsonDocument.Parse((byte[])sql.ExecuteScalar()!);
        return checkpoint.RootElement.GetProperty(property).GetString()!;
    }

    private static (long Sequence, string Hash, long StepCount) ReadHead(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = """
            SELECT head_sequence,head_hash,(SELECT count(*) FROM accepted_steps WHERE campaign_id=$id)
            FROM campaigns WHERE campaign_id=$id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var row = query.ExecuteReader();
        Assert.True(row.Read());
        return (row.GetInt64(0), row.GetString(1), row.GetInt64(2));
    }

    private static void AssertRoutes(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var choices = document.RootElement.GetProperty("choices").EnumerateArray().ToArray();
        foreach (var (lane, origin, destination) in new[]
                 { (2, "West Sicily", "Tripoli"), (3, "Italy", "Benghazi"), (6, "Italy", "Tobruk") })
        {
            var route = choices.First(value => value.GetProperty("lane").GetInt32() == lane);
            Assert.Equal(origin, route.GetProperty("origin").GetString());
            Assert.Equal(destination, route.GetProperty("destination").GetString());
        }
    }

    private static ExerciseStepResult SubmitDirect(ExerciseSession session, Func<CampaignActionCandidate, bool> predicate)
    {
        var set = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
        var action = set.Candidates.Single(predicate);
        return CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
            set.StateVersion, set.PositionId, set.Audience, action.ActionId));
    }

    private static Choice[] Choices(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.GetProperty("choices").EnumerateArray().Select(value => new Choice(
            value.GetProperty("kind").GetString()!,
            value.TryGetProperty("lane", out var lane) ? lane.GetInt32() : 0,
            value.TryGetProperty("operationStage", out var stage) ? stage.GetInt32() : 0)).ToArray();
    }

    private static async Task<ChildResult> ReadInFreshProcess(string path, string campaignId, string mode = "--pregame-axis-read")
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child), $"Built child missing: {child}");
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(child); start.ArgumentList.Add(mode);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start replay child.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { Path = path, CampaignId = campaignId }));
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(timeout.Token);
            var output = await stdout;
            Assert.True(process.ExitCode == 0, $"Replay child failed: {await stderr}");
            var result = JsonSerializer.Deserialize<ChildResult>(output)!;
            Assert.Equal(process.Id, result.ProcessId);
            return result;
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }

    private sealed record Choice(string Kind, int Lane, int Stage);
    private sealed record ChildResult(int ProcessId, string CampaignId, long StateVersion, string PositionId, string VisibleBase64);
}
