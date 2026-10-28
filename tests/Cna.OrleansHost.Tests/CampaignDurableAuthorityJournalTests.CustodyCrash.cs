using System.Text;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static readonly string[] CustodyCrashVisibleTables =
        ["campaigns", "accepted_steps", "custody_rounds", "reserve_rounds", "cycle_rounds", "owner_actions", "owner_openings"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedCustodyCrashAtFallbackBoundaryRecoversAtomically(bool afterCommit)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var journal = original.Create(clock);
        var (request, defender, defenderId, opening) = PrepareEntryClockFinalSeal(journal, custody: true, repeated: true);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId, opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var campaignId = request.CampaignId;
        var before = journal.ReadVerified(campaignId);
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(before));
        var version = CampaignExercises.QueryCheckpoint(before).StateVersion;
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        using var settlement = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
        var lot = Assert.Single(settlement.RootElement.GetProperty("pendingCaptives").EnumerateArray());
        Assert.Equal("axis-infantry-second", lot.GetProperty("captorElementId").GetString());
        Assert.Equal("commonwealth-infantry-west", lot.GetProperty("victimElementId").GetString());
        Assert.Equal(1, lot.GetProperty("quantity").GetInt32());
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        Assert.Equal(RepeatedCustodySecondDice, paid.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        Assert.Equal(19UL, paid.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
        var settlementHash = HashRepeatedCustody(Encoding.UTF8.GetBytes(settlement.RootElement.GetRawText()));
        var (opened, deadline) = FreshProcessRoundClock(original.Path, "custody_rounds", campaignId);
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var ownerOpenings = IntegratedCampaignRows(original.Path, "owner_openings");
        var saves = new List<JournalFixture>();
        try
        {
            var expectedDisk = IntegratedCampaignSave(original.Path, clock, saves);
            var trace = new NativeCrashTraceClock(deadline);
            var expected = expectedDisk.Open(trace);
            trace.Arm();
            Assert.True(expected.TickCustodyRound(campaignId));
            var (cycleOpened, _) = FreshProcessRoundClock(expectedDisk.Path, "cycle_rounds", campaignId);
            // Persisted Cycle entry identifies the clock read after Custody timeout,
            // timed-out row and successor writes, still inside the uncommitted transaction.
            var cycleRead = Assert.Single(trace.Reads, item => item.Observed == cycleOpened);
            Assert.True(cycleRead.Index > 1);
            var completedVersion = CampaignExercises.QueryCheckpoint(expected.ReadVerified(campaignId)).StateVersion;
            Assert.True(completedVersion > version);
            using var timeout = CustodyTimeoutEvent(expectedDisk.Path, campaignId);
            var result = timeout.RootElement;
            Assert.Equal(settlementHash, result.GetProperty("settlementHash").GetString());
            Assert.Equal("combat.captive." + settlementHash[7..], result.GetProperty("lotId").GetString());
            Assert.Equal("leave-unguarded", result.GetProperty("fallback").GetString());
            Assert.Equal("deadline", result.GetProperty("reason").GetString());
            Assert.Equal(opened, result.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            Assert.Equal(settlement.RootElement.GetProperty("settledWorldHash").GetString(), result.GetProperty("worldHash").GetString());

            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            await KillNativeCrashChild(disk.Path, campaignId, deadline, cycleRead.Index, cycleOpened, afterCommit,
                () =>
                {
                    foreach (var table in CustodyCrashVisibleTables)
                        Assert.Equal(IntegratedCampaignRows(afterCommit ? expectedDisk.Path : original.Path, table),
                            IntegratedCampaignRows(disk.Path, table));
                }, "--native-custody-crash-probe");
            IntegratedCampaignCompare(afterCommit ? expected : journal,
                afterCommit ? expectedDisk.Path : original.Path, disk.Open(clock), disk.Path, campaignId);
            var recovered = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline, "--native-custody-crash-probe");
            Assert.NotEqual(Environment.ProcessId, recovered.ProcessId);
            Assert.True(recovered.IsVerified);
            Assert.Equal(afterCommit ? completedVersion : version, recovered.BeforeStateVersion);
            Assert.Equal(completedVersion, recovered.AfterStateVersion);
            IntegratedCampaignCompare(expected, expectedDisk.Path, disk.Open(clock), disk.Path, campaignId);
            var replay = CampaignExercises.Reconstruct(expected.ReadVerified(campaignId));
            Assert.Equal(replay.EventStreamHash, recovered.EventStreamHash);
            Assert.Equal(replay.ReconstructedSnapshotHash, recovered.ReconstructedSnapshotHash);
            Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(disk.Open(clock).ReadVerified(campaignId)));
            var rows = IntegratedCampaignRows(disk.Path, "accepted_steps");
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline, "--native-custody-crash-probe");
            Assert.True(retry.IsVerified);
            Assert.Equal(completedVersion, retry.BeforeStateVersion);
            Assert.Equal(completedVersion, retry.AfterStateVersion);
            Assert.Equal(recovered.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(recovered.ReconstructedSnapshotHash, retry.ReconstructedSnapshotHash);
            Assert.Equal(rows, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, "contact-combat-custody"));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            using var unchangedPaid = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(paid.RootElement.GetRawText(), unchangedPaid.RootElement.GetRawText());
            using var successor = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.Equal(result.GetProperty("custodyStateHash").GetString(), successor.RootElement.GetProperty("custodyStateHash").GetString());
            Assert.Equal(result.GetProperty("worldHash").GetString(), successor.RootElement.GetProperty("worldHash").GetString());
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(ownerOpenings, IntegratedCampaignRows(disk.Path, "owner_openings"));
            Assert.Contains(campaignId, disk.Open(clock).CollectingCycleCampaigns("", 16));
            IntegratedCampaignCompare(expected, expectedDisk.Path, disk.Open(clock), disk.Path, campaignId);
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }
}
