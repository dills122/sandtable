using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Exercises;

public sealed class TurnContinuationSentinelTests
{
    [Theory]
    [InlineData(1, 2UL)]
    [InlineData(1, 3UL)]
    [InlineData(12, 12345UL)]
    public void SuccessorTurnFirstDecisionMustAdvanceTheCurrentCut(int initialTurn, ulong seed)
    {
        var opened = SequencingScenarios.DriveTurnBoundary(initialTurn, seed).Samples[^1].Session;
        Assert.True(CampaignExercises.Reconstruct(opened).IsVerified);
        var declaration = opened.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Last();
        var rollover = Assert.Single(opened.CurrentHistory.OfType<CampaignNativeEndOfTurnV55>());
        Assert.Equal(initialTurn + 1, rollover.NextGameTurn);
        var checkpoint = CampaignExercises.QueryCheckpoint(opened);
        var audience = declaration.FirstSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var submission = new CampaignActionSubmission(1, checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, audience, new CompleteReserveDesignationAction().ActionId);

        var accepted = CampaignExercises.SubmitWithCombat(opened, submission);
        Assert.True(accepted.IsAccepted, $"Successor turn {initialTurn + 1} first decision: {accepted.RejectionReason}");
        var successor = Assert.IsType<ExerciseSession>(accepted.SuccessorSession);
        var record = Assert.IsType<CampaignNativeSecondReserveV43>(successor.CurrentHistory[^1]);
        Assert.Equal(declaration.FirstSide, record.Side);
        Assert.Equal(checkpoint.StateVersion + 1, record.StateVersion);
        Assert.Equal(record.StateVersion, CampaignExercises.QueryCheckpoint(successor).StateVersion);
        Assert.Equal(record.NextPositionId, CampaignExercises.QueryCheckpoint(successor).PositionId);
        Assert.True(CampaignExercises.Reconstruct(successor).IsVerified);
    }

    // Diagnostic only: append the existing factory's valid successor record to a
    // detached test copy, without treating it as a live accepted submission. This
    // isolates the pending/checkpoint cascade from the closed submission gate.
    [Theory]
    [InlineData(1, 2UL)]
    [InlineData(1, 3UL)]
    [InlineData(12, 12345UL)]
    public void NewJournalHeadMustNotBeShadowedByPriorTurnFields(int initialTurn, ulong seed)
    {
        var opened = SequencingScenarios.DriveTurnBoundary(initialTurn, seed).Samples[^1].Session;
        var (repair, world) = CampaignExercises.QueryTurnStartReserve(opened);
        var declaration = opened.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Last();
        var (record, resultWorld) = CampaignNativeSecondReserveV43.Create(repair, world, opened.Context,
            CampaignExercises.TurnStartOrders(opened), declaration.FirstSide, null, initialTurn + 1, repair.StateVersion);
        Assert.All(resultWorld.Elements, element =>
        {
            Assert.Equal(initialTurn + 1, element.OperationalState.LedgerGameTurn);
            Assert.Equal(1, element.OperationalState.LedgerOperationStage);
            Assert.Equal(CapabilityPointAmount.Zero, element.OperationalState.CapabilityPointsExpended);
        });
        var candidate = SessionTamper.Clone(opened);
        SessionTamper.Set(candidate, nameof(ExerciseSession.CurrentHistory), opened.CurrentHistory.Add(record));
        SessionTamper.Set(candidate, nameof(ExerciseSession.CurrentContacts), opened.CurrentContacts.AdvanceSecondReserve(record));
        SessionTamper.Set(candidate, nameof(ExerciseSession.TurnBoundaryPredecessor), opened);
        Assert.Equal(record.StateVersion, candidate.CurrentContacts.AuthorityVersion);
        var actual = CampaignExercises.QueryCheckpoint(candidate);
        Assert.True(actual.StateVersion == record.StateVersion && actual.PositionId == record.NextPositionId,
            $"Expected journal head {record.StateVersion} at {record.NextPositionId}; got {actual.StateVersion} " +
            $"at {actual.PositionId} ({candidate.Pending?.GetType().Name}).");
    }
}
