using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Hosting;

// Spike A W4 (tripwire T3, Host half): the Host continuation after an accepted voluntary finish is
// resolved from the CURRENT occurrence slot (live Pending subtype), never from retained Stage 1 fields.
// Probes recreate Spike A StaleAliasing (stale predicates/completions) and HostDraftReach (drafts at
// each cut) against the real sequencing samples.
public sealed class OccurrenceSlotContinuationTests
{
    [Theory]
    [InlineData("op1.second", CampaignCombatSelectionSlot.StageOneSecond, true)]
    [InlineData("op2.first", CampaignCombatSelectionSlot.StageTwoFirst, false)]
    [InlineData("op2.second", CampaignCombatSelectionSlot.StageTwoSecond, true)]
    [InlineData("op3.first", CampaignCombatSelectionSlot.StageThreeFirst, false)]
    [InlineData("op3.second", CampaignCombatSelectionSlot.StageThreeSecond, true)]
    public void SlotIsDerivedFromTheCurrentOccurrenceAtBothSelectionCuts(
        string occurrence, CampaignCombatSelectionSlot slot, bool second)
    {
        foreach (var cut in new[] { "breakdown", "combat-selection" })
        {
            var session = Sample(occurrence, cut);
            Assert.Equal(slot, CampaignHostAdapter.QueryCombatSelectionSlot(session));
            Assert.Equal(second, CampaignHostAdapter.IsSecondSideCombatPending(session));
        }
        // After the side's Combat closes, no Selection occurrence is current any more.
        Assert.Null(CampaignHostAdapter.QueryCombatSelectionSlot(Sample(occurrence, "reserve-release")));
    }

    [Theory]
    [InlineData("op1.second", true)]
    [InlineData("op2.first", false)]
    [InlineData("op2.second", true)]
    [InlineData("op3.first", false)]
    [InlineData("op3.second", true)]
    public void VoluntaryFinishRunsTheSlotsOwnNoAttackAndNeverAnotherStages(string occurrence, bool second)
    {
        var selected = Sample(occurrence, "combat-selection");
        var expected = Sample(occurrence, "reserve-release");

        var accepted = second
            ? CampaignHostAdapter.CompleteSecondCombatNoAttack(selected)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(selected);
        Assert.True(accepted.IsAccepted);
        var command = second
            ? CampaignHostAdapter.SecondCombatNoAttackCommand(selected, accepted)
            : CampaignHostAdapter.NativeCombatNoAttackCommand(selected, accepted);
        Assert.Equal(CampaignActionAudience.System, command.Audience);
        Assert.Equal(CampaignExercises.QueryCheckpoint(selected).PositionId, command.ExpectedPositionId);
        // The completion advances exactly one authority step and the live session cold-replays.
        Assert.Equal(CampaignExercises.QueryCheckpoint(selected).StateVersion + 1,
            CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion);
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
        Assert.True(CampaignExercises.QueryCheckpoint(accepted.SuccessorSession!).StateVersion
            <= CampaignExercises.QueryCheckpoint(expected).StateVersion);
    }

    [Theory]
    [InlineData("op2.first", false)]
    [InlineData("op2.second", true)]
    [InlineData("op3.first", false)]
    [InlineData("op3.second", true)]
    public void StageOneSecondSideCompletionsCannotRunAtLaterStages(string occurrence, bool second)
    {
        var selected = Sample(occurrence, "combat-selection");
        // Spike A StaleAliasing: the Stage 1 second-side fields are stale-true forever after Stage 1.
        Assert.NotNull(selected.CurrentSecondBreakdown);
        Assert.False(CampaignExercises.CompleteSecondCombatNoAttack(selected).IsAccepted);
        Assert.False(CampaignExercises.CompleteNativeCombatNoAttack(selected).IsAccepted);
        // The slot-resolved completion is the only one that applies, and only on its own side.
        Assert.True((second
            ? CampaignHostAdapter.CompleteSecondCombatNoAttack(selected)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(selected)).IsAccepted);
        Assert.False((second
            ? CampaignHostAdapter.CompleteNativeCombatNoAttack(selected)
            : CampaignHostAdapter.CompleteSecondCombatNoAttack(selected)).IsAccepted);
    }

    [Theory]
    [InlineData("op1.second")]
    [InlineData("op2.first")]
    [InlineData("op2.second")]
    [InlineData("op3.first")]
    [InlineData("op3.second")]
    public void EmptyReserveReleaseAndCycleBindingFollowTheSlot(string occurrence)
    {
        var selected = Sample(occurrence, "combat-selection");
        var second = CampaignHostAdapter.IsSecondSideCombatPending(selected);
        var noAttack = (second
            ? CampaignHostAdapter.CompleteSecondCombatNoAttack(selected)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(selected)).SuccessorSession!;
        // The Host's AppendEmptyReserveRelease reads only these slot-dispatched Core queries.
        Assert.True(CampaignHostAdapter.IsNativeReserveReleaseEmpty(noAttack));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(noAttack)); // empty: no owner round to arm
        var released = CampaignHostAdapter.CompleteNativeReserveRelease(noAttack);
        Assert.True(released.IsAccepted);
        var cycle = CampaignHostAdapter.QueryNativeCycleControlRoundBinding(released.SuccessorSession!);
        // Live chain lands on exactly the sequencing driver's own cut, and cold-replays there.
        var expected = Sample(occurrence, "reserve-release");
        Assert.Equal(CampaignExercises.QueryCheckpoint(expected).StateVersion, cycle.StateVersion);
        Assert.Equal(CampaignExercises.QueryCheckpoint(expected).PositionId,
            CampaignExercises.QueryCheckpoint(released.SuccessorSession!).PositionId);
        Assert.True(CampaignExercises.Reconstruct(released.SuccessorSession!).IsVerified);
    }

    [Theory]
    [InlineData("op1.second")]
    [InlineData("op2.first")]
    [InlineData("op2.second")]
    [InlineData("op3.first")]
    [InlineData("op3.second")]
    public void SelectionRoundBindingAndTimeoutBindToTheCurrentBreakdown(string occurrence)
    {
        var breakdown = Sample(occurrence, "breakdown");
        var binding = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(breakdown);
        Assert.Equal(CampaignExercises.QueryCheckpoint(breakdown).StateVersion, binding.StateVersion);
        var timed = CampaignHostAdapter.TimeoutNativeCombatSelection(breakdown, null, -1, -1, -1, null,
            "selection-unavailable");
        Assert.True(timed.IsAccepted);
        var command = CampaignHostAdapter.NativeCombatSelectionTimeoutCommand(breakdown, timed);
        Assert.Equal(CampaignActionAudience.System, command.Audience);
        // The timeout leaves the occurrence's own voluntary finish pending for the slot continuation.
        var slot = CampaignHostAdapter.QueryCombatSelectionSlot(timed.SuccessorSession!)!.Value;
        var second = slot is CampaignCombatSelectionSlot.StageOneSecond or CampaignCombatSelectionSlot.StageTwoSecond
            or CampaignCombatSelectionSlot.StageThreeSecond;
        Assert.Equal(second, CampaignHostAdapter.IsNativeSecondCombatVoluntaryFinish(timed.SuccessorSession!));
        Assert.Equal(!second, CampaignHostAdapter.IsNativeCombatVoluntaryFinish(timed.SuccessorSession!));
        var completed = second
            ? CampaignHostAdapter.CompleteSecondCombatNoAttack(timed.SuccessorSession!)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(timed.SuccessorSession!);
        Assert.True(completed.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(completed.SuccessorSession!).IsVerified);
    }

    private static ExerciseSession Sample(string occurrence, string cut) =>
        SequencingScenarios.All[0].Samples.Single(sample => sample.Label == $"{occurrence}.{cut}").Session;
}
