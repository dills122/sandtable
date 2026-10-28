using System.Text;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static readonly string[] RetreatCrashVisibleTables =
        ["campaigns", "accepted_steps", "retreat_rounds", "custody_rounds", "reserve_rounds", "cycle_rounds", "owner_actions", "owner_openings"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedRequiredRetreatCrashAtFallbackBoundaryRecoversAtomically(bool afterCommit)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var journal = original.Create(clock);
        var (request, defender, defenderId, opening) = PrepareEntryClockFinalSeal(journal, custody: false, repeated: true);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId, opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var campaignId = request.CampaignId;
        var before = journal.ReadVerified(campaignId);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(before).ContractVersion);
        var version = CampaignExercises.QueryCheckpoint(before).StateVersion;
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        Assert.Equal(RepeatedRetreatSecondDice, paid.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        Assert.Equal(10UL, paid.RootElement.GetProperty("beforeRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(18UL, paid.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(1, paid.RootElement.GetProperty("facts").GetProperty("requiredRetreat").GetInt32());
        Assert.Equal(5, paid.RootElement.GetProperty("facts").GetProperty("defenderPercent").GetInt32());
        Assert.Equal(JsonValueKind.Null, paid.RootElement.GetProperty("facts").GetProperty("capturedRole").ValueKind);
        var resultHash = HashRepeatedCustody(Encoding.UTF8.GetBytes(paid.RootElement.GetRawText()));
        Assert.Equal(resultHash, CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(before).ResultHash);
        var (opened, deadline) = FreshProcessRoundClock(original.Path, "retreat_rounds", campaignId);
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var ownerOpenings = IntegratedCampaignRows(original.Path, "owner_openings");
        var saves = new List<JournalFixture>();
        try
        {
            // Only a separate reference branch queries/submits legal owner refusal.
            var ownerDisk = IntegratedCampaignSave(original.Path, clock, saves);
            var ownerJournal = ownerDisk.Open(clock);
            var refusal = ownerJournal.QueryPostAssaultRetreatOwnerOpening(defender, defenderId);
            Assert.True(ownerJournal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
                refusal.Revision, refusal.PublicActionRefs[^1]).IsAccepted);
            using var ownerSettlement = IntegratedCampaignRecord(ownerDisk.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
            Assert.True(CampaignExercises.Reconstruct(ownerJournal.ReadVerified(campaignId)).IsVerified);

            var expectedDisk = IntegratedCampaignSave(original.Path, clock, saves);
            var trace = new NativeCrashTraceClock(deadline);
            var expected = expectedDisk.Open(trace);
            trace.Arm();
            Assert.True(expected.TickRetreatRound(campaignId));
            var (cycleOpened, _) = FreshProcessRoundClock(expectedDisk.Path, "cycle_rounds", campaignId);
            // Persisted Cycle entry locates the read after timeout/settlement/System
            // successor writes, before Cycle insert and the enclosing Commit.
            var cycleRead = Assert.Single(trace.Reads, item => item.Observed == cycleOpened);
            Assert.True(cycleRead.Index > 1);
            var completed = CampaignExercises.QueryCheckpoint(expected.ReadVerified(campaignId));
            Assert.True(completed.StateVersion > version);
            Assert.NotEqual(22, completed.ContractVersion);
            using var timeout = RetreatTimeoutEvent(expectedDisk.Path, campaignId);
            var result = timeout.RootElement;
            Assert.Equal(resultHash, result.GetProperty("resultHash").GetString());
            Assert.Equal(defenderId, result.GetProperty("defenderElementId").GetString());
            Assert.Equal("Commonwealth", result.GetProperty("defenderSide").GetString());
            Assert.Equal(paid.RootElement.GetProperty("decisionActionId").GetString(), result.GetProperty("decisionActionId").GetString());
            Assert.Equal(paid.RootElement.GetProperty("paidWorldHash").GetString(), result.GetProperty("paidWorldHash").GetString());
            Assert.Equal("refuse-retreat", result.GetProperty("fallback").GetString());
            Assert.Equal("deadline", result.GetProperty("reason").GetString());
            Assert.False(result.TryGetProperty("actionId", out _));
            Assert.False(result.TryGetProperty("destinationLocationId", out _));
            Assert.Equal(opened, result.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            using var settlement = IntegratedCampaignRecord(expectedDisk.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
            Assert.Equal(HashRepeatedCustody(Encoding.UTF8.GetBytes(result.GetRawText())), settlement.RootElement.GetProperty("retreatHash").GetString());
            Assert.NotEqual(paid.RootElement.GetProperty("paidWorldHash").GetString(), settlement.RootElement.GetProperty("settledWorldHash").GetString());
            Assert.Equal(ownerSettlement.RootElement.GetProperty("settledWorldHash").GetString(), settlement.RootElement.GetProperty("settledWorldHash").GetString());
            Assert.Empty(settlement.RootElement.GetProperty("pendingCaptives").EnumerateArray());

            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            await KillNativeCrashChild(disk.Path, campaignId, deadline, cycleRead.Index, cycleOpened, afterCommit,
                () =>
                {
                    foreach (var table in RetreatCrashVisibleTables)
                        Assert.Equal(IntegratedCampaignRows(afterCommit ? expectedDisk.Path : original.Path, table),
                            IntegratedCampaignRows(disk.Path, table));
                }, "--native-retreat-crash-probe");
            IntegratedCampaignCompare(afterCommit ? expected : journal,
                afterCommit ? expectedDisk.Path : original.Path, disk.Open(clock), disk.Path, campaignId);
            var recovered = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline, "--native-retreat-crash-probe");
            Assert.NotEqual(Environment.ProcessId, recovered.ProcessId);
            Assert.True(recovered.IsVerified);
            Assert.Equal(afterCommit ? completed.StateVersion : version, recovered.BeforeStateVersion);
            Assert.Equal(completed.StateVersion, recovered.AfterStateVersion);
            Assert.Equal(completed.ContractVersion, recovered.ContractVersion);
            IntegratedCampaignCompare(expected, expectedDisk.Path, disk.Open(clock), disk.Path, campaignId);
            var replay = CampaignExercises.Reconstruct(expected.ReadVerified(campaignId));
            Assert.Equal(replay.EventStreamHash, recovered.EventStreamHash);
            Assert.Equal(replay.ReconstructedSnapshotHash, recovered.ReconstructedSnapshotHash);
            var rows = IntegratedCampaignRows(disk.Path, "accepted_steps");
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline, "--native-retreat-crash-probe");
            Assert.True(retry.IsVerified);
            Assert.Equal(completed.StateVersion, retry.BeforeStateVersion);
            Assert.Equal(completed.StateVersion, retry.AfterStateVersion);
            Assert.Equal(recovered.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(recovered.ReconstructedSnapshotHash, retry.ReconstructedSnapshotHash);
            Assert.Equal(rows, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, "contact-post-assault-retreat"));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            using var unchangedPaid = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(paid.RootElement.GetRawText(), unchangedPaid.RootElement.GetRawText());
            using var successor = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.Equal(settlement.RootElement.GetProperty("settledWorldHash").GetString(), successor.RootElement.GetProperty("worldHash").GetString());
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(ownerOpenings, IntegratedCampaignRows(disk.Path, "owner_openings"));
            Assert.Contains(campaignId, disk.Open(clock).CollectingCycleCampaigns("", 16));
            IntegratedCampaignCompare(expected, expectedDisk.Path, disk.Open(clock), disk.Path, campaignId);
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }
}
