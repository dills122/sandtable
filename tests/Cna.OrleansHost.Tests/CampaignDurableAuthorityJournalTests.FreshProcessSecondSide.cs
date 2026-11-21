using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

// Restart recovery for the second side's Combat rounds: a separate OS process
// opens only the committed journal copy and recovers the expired round. The
// result must match the in-process control's own timeout byte for byte, be
// idempotent on retry, and let both journals finish Operation Stage 1.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData("selection_rounds")]
    [InlineData("rba_rounds")]
    [InlineData("cycle_rounds")]
    public async Task FreshProcessRecoversAnExpiredSecondSideRound(string table)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var control = original.Create(clock);
        var (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(control,
            table == "rba_rounds" ? PositiveCombatRequest() : null);
        if (table == "rba_rounds") SubmitSecondSideAssault(control, commonwealth);
        if (table == "cycle_rounds")
        {
            var opening = control.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
            Assert.True(control.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
                opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        }
        var campaignId = request.CampaignId;
        var version = CampaignExercises.QueryCheckpoint(control.ReadVerified(campaignId)).StateVersion;
        var saves = new List<JournalFixture>();
        try
        {
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var (_, deadline) = FreshProcessRoundClock(disk.Path, table, campaignId);
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            SweepControl();
            var early = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline - 1);
            Assert.Equal(version, early.AfterStateVersion);
            Compare(early);

            clock.Advance(TimeSpan.FromMilliseconds(1));
            SweepControl();
            var expired = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(version, expired.BeforeStateVersion);
            Assert.True(expired.AfterStateVersion > version);
            Compare(expired);

            var accepted = IntegratedCampaignRows(disk.Path, "accepted_steps");
            SweepControl();
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(expired.AfterStateVersion, retry.AfterStateVersion);
            Assert.Equal(expired.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(accepted, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Compare(retry);

            // Both the control and the recovered copy finish the second side's block.
            FinishSecondSideOperationStage(original, clock, request, commonwealth);
            FinishSecondSideOperationStage(disk, clock, request, commonwealth);
            IntegratedCampaignCompare(control, original.Path, disk.Open(clock), disk.Path, campaignId);

            void SweepControl() => new CampaignCombatTimeoutService(CampaignDurableAuthorityJournalStore.ForSingleCampaign(control), clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);

            void Compare(NativeChildResult result)
            {
                Assert.NotEqual(Environment.ProcessId, result.ProcessId);
                Assert.True(result.IsVerified);
                IntegratedCampaignCompare(control, original.Path, disk.Open(clock), disk.Path, campaignId);
                var replay = CampaignExercises.Reconstruct(control.ReadVerified(campaignId));
                Assert.Equal(replay.EventStreamHash, result.EventStreamHash);
                Assert.Equal(replay.ReconstructedSnapshotHash, result.ReconstructedSnapshotHash);
            }
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }
}
