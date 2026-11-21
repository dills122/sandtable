using Cna.Core.Campaigns;
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
    [InlineData("force_rounds")]
    [InlineData("retreat_rounds")]
    [InlineData("custody_rounds")]
    [InlineData("reserve_rounds")]
    public async Task FreshProcessRecoversAnExpiredSecondSideRound(string table)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        CampaignDurableAuthorityJournal control;
        CampaignCreationRequest request;
        CampaignSeatCredential commonwealth;
        if (table is "selection_rounds" or "rba_rounds" or "cycle_rounds")
        {
            control = original.Create(clock);
            (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(control,
                table == "rba_rounds" ? PositiveCombatRequest() : null);
            if (table == "rba_rounds") SubmitSecondSideAssault(control, commonwealth);
            if (table == "cycle_rounds")
            {
                var opening = control.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
                Assert.True(control.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
                    opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
            }
        }
        else
        {
            // Force: one seal pending. Retreat (seed 6) and Custody (seed 5):
            // both seals, the result leaves that round open. Reserve: a
            // designated second-side Reserve I member is pending after the assault.
            var payable = table switch
            {
                "retreat_rounds" => PositiveCombatRequest(6),
                "custody_rounds" => PositiveCombatRequest(5),
                "reserve_rounds" => PositiveTripleContactRequest(12345),
                _ => PositiveCombatRequest(),
            };
            CampaignSeatCredential axis;
            (request, axis, commonwealth) = SecondSideAssaultThroughRba(original, clock, payable,
                table == "reserve_rounds" ? "commonwealth-infantry-far-north-west" : null);
            control = original.Open(clock);
            var attackerForce = control.QueryForceOwnerOpening(commonwealth, "commonwealth-infantry");
            var defenderForce = control.QueryForceOwnerOpening(axis, "axis-infantry");
            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.True(control.SubmitOwnerForceSeal(commonwealth, "commonwealth-infantry",
                attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
            if (table != "force_rounds")
            {
                clock.Advance(TimeSpan.FromSeconds(1));
                Assert.True(control.SubmitOwnerForceSeal(axis, "axis-infantry",
                    defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
            }
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
