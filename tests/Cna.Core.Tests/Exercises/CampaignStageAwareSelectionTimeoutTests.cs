using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Exercises;

public sealed class CampaignStageAwareSelectionTimeoutTests
{
    [Theory]
    [InlineData("op1.second.breakdown")]
    [InlineData("op2.first.breakdown")]
    [InlineData("op2.second.breakdown")]
    [InlineData("op3.first.breakdown")]
    [InlineData("op3.second.breakdown")]
    public void BindingAndTimeoutUseCurrentBreakdownAndReconstruct(string label)
    {
        var session = SequencingScenarios.All[0].Samples.Single(sample => sample.Label == label).Session;
        var breakdown = Assert.IsType<CampaignNativeSecondBreakdownV46>(session.CurrentHistory[^1]);
        var binding = CampaignExercises.QueryNativeCombatSelectionRoundBinding(session);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryContactOwnerMovementCut(session,
            breakdown.Side, breakdown.Side == Cna.Core.Rules.LandSide.Axis ? "axis-infantry" : "commonwealth-infantry"));
        Assert.Equal((CampaignSelectionTimeoutEvidenceV38.Digest(breakdown.SerializeCanonical()), breakdown.StateVersion), binding);
        var timing = new CampaignDecisionTimingConfigurationV1("rules-lab.selection-timing.1",
            session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
            [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
                new("force-assignment", 30000, "cancel-incomplete-voluntary-round"), new("rba", 30000, "cancel-without-decline"),
                new("reserve-release", 30000, "convert-unresolved-i-retain-ii"), new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);
        Assert.False(CampaignExercises.TimeoutNativeCombatSelection(session, timing, 1000, 31000, 1000, 30999, "deadline").IsAccepted);
        var accepted = CampaignExercises.TimeoutNativeCombatSelection(session, timing, 1000, 31000, 1000, 31000, "deadline");
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        var successor = accepted.SuccessorSession!;
        var timeout = Assert.IsType<CampaignNativeSecondCombatSelectionTimeoutV48>(successor.CurrentHistory[^1]);
        Assert.Equal(binding.EntryHash, timeout.Evidence.EntryHash);
        Assert.Equal(breakdown.NextPositionId, timeout.Evidence.PositionId);
        Assert.Equal(breakdown.Side, timeout.Side);
        Assert.Equal(CampaignActionAudience.System, CampaignHostAdapter.NativeCombatSelectionTimeoutCommand(session, accepted).Audience);
        var bytes = accepted.Evidence!.EventRecords.Single();
        var retained = Assert.IsType<CampaignNativeSecondCombatSelectionTimeoutV48>(CampaignCurrentEventSerializer.Deserialize(bytes));
        Assert.Equal(bytes, retained.SerializeCanonical());
        // Re-admit the deserialized event onto the retained predecessor without using the accepted successor/runtime.
        var state = new CampaignNativeSecondCombatDecisionStateV47(session.CurrentSnapshot,
            session.Pending switch
            {
                SecondBreakdownPending => session.CurrentSecondMoveWorld ?? session.CurrentSecondReserveWorld!,
                StageEntryBreakdownPending => session.CurrentStageEntryMoveWorld ?? session.CurrentStageEntryReserveWorld!,
                StageEntrySecondBreakdownPending => session.CurrentStageEntrySecondMoveWorld ?? session.CurrentStageEntrySecondReserveWorld!,
                ThirdStageBreakdownPending => session.CurrentThirdStageMoveWorld ?? session.CurrentThirdStageReserveWorld!,
                ThirdStageSecondBreakdownPending => session.CurrentThirdStageSecondMoveWorld ?? session.CurrentThirdStageSecondReserveWorld!,
                _ => throw new InvalidOperationException(),
            },
            ExerciseSession.LatestCursor(session.CurrentSnapshot.RandomState, session.CurrentHistory), retained);
        var contacts = session.CurrentContacts.AdvanceSecondCombatSelection(retained);
        var cold = session.Pending switch
        {
            SecondBreakdownPending => session.WithSecondCombatSelection(state, contacts),
            StageEntryBreakdownPending => session.WithStageEntryCombatSelection(state, contacts),
            StageEntrySecondBreakdownPending => session.WithStageEntrySecondCombatSelection(state, contacts),
            ThirdStageBreakdownPending => session.WithThirdStageCombatSelection(state, contacts),
            ThirdStageSecondBreakdownPending => session.WithThirdStageSecondCombatSelection(state, contacts),
            _ => throw new InvalidOperationException(),
        };
        var reconstructed = CampaignExercises.Reconstruct(cold);
        Assert.True(reconstructed.IsVerified, reconstructed.FailureReason.ToString());
        Assert.Equal(bytes, reconstructed.ReconstructedSnapshotBytes);
        // A retained predecessor failure must report the hash of this occurrence's head, not Stage 1.
        var broken = SessionTamper.BlankPredecessorSlot(cold, cold.Pending!.GetType().Name,
            "Current" + session.Pending!.GetType().Name[..^"Pending".Length]);
        var invalid = CampaignExercises.Reconstruct(broken);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, invalid.FailureReason);
        Assert.Equal("sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)), invalid.ExpectedSnapshotHash);
        var noAttack = cold.Pending switch
        {
            SecondCombatSelectionPending => CampaignExercises.CompleteSecondCombatNoAttack(cold),
            StageEntryCombatSelectionPending => CampaignExercises.CompleteStageEntryCombatNoAttack(cold),
            StageEntrySecondCombatSelectionPending => CampaignExercises.CompleteStageEntrySecondCombatNoAttack(cold),
            ThirdStageCombatSelectionPending => CampaignExercises.CompleteThirdStageCombatNoAttack(cold),
            ThirdStageSecondCombatSelectionPending => CampaignExercises.CompleteThirdStageSecondCombatNoAttack(cold),
            _ => throw new InvalidOperationException(),
        };
        Assert.True(noAttack.IsAccepted, noAttack.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(noAttack.SuccessorSession!).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeCombatSelectionRoundBinding(successor));
        Assert.False(CampaignExercises.TimeoutNativeCombatSelection(successor, timing, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.Equal(ExerciseSession.LatestCursor(session.CurrentSnapshot.RandomState, session.CurrentHistory),
            ExerciseSession.LatestCursor(successor.CurrentSnapshot.RandomState, successor.CurrentHistory));
    }

    [Theory]
    [InlineData("op2.first.reserve")]
    [InlineData("op2.second.reserve")]
    [InlineData("op3.first.reserve")]
    [InlineData("op3.second.reserve")]
    public void RetainedEarlierBreakdownsCannotBindOrTimeOutDuringReserve(string label)
    {
        var session = SequencingScenarios.All[0].Samples.Single(sample => sample.Label == label).Session;
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeCombatSelectionRoundBinding(session));
        Assert.False(CampaignExercises.TimeoutNativeCombatSelection(session, null, -1, -1, -1, null, "selection-unavailable").IsAccepted);
        Assert.Null(CampaignCombatExerciseActions.Query(session, CampaignActionAudience.Axis));
        Assert.Null(CampaignCombatExerciseActions.Query(session, CampaignActionAudience.Commonwealth));
    }

}
