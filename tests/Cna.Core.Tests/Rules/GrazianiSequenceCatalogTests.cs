using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

// Checks the static catalogue across the scenario horizon, not campaign execution.
public sealed class GrazianiSequenceCatalogTests
{
    [Fact]
    public void SixTurnCatalogPreservesStagePreludeAndRelativePlayerOrder()
    {
        var allPositions = 0;
        for (var turn = 1; turn <= 6; turn++)
        {
            var positions = Cna1979LandSequence.CreateTurn(turn);
            allPositions += positions.Count;
            Assert.All(positions, position => Assert.Equal(turn, position.GameTurn));
            for (var stage = 1; stage <= 3; stage++)
            {
                var stagePositions = positions.Where(position => position.OperationStage == stage).ToArray();
                Assert.Equal(
                [
                    LandPhaseIds.InitiativeDeclaration,
                    LandPhaseIds.WeatherDetermination,
                    LandPhaseIds.Organization,
                    LandPhaseIds.NavalConvoyArrival,
                    LandPhaseIds.CommonwealthFleet,
                    LandPhaseIds.CommonwealthFleet,
                ], stagePositions.Take(6).Select(position => position.PhaseId));
                Assert.Equal(LandSegmentIds.FleetAssignment, stagePositions[4].SegmentId);
                Assert.Equal(LandSegmentIds.FleetRepair, stagePositions[5].SegmentId);
                var reserves = stagePositions.Where(position => position.PhaseId == LandPhaseIds.ReserveDesignation).ToArray();
                Assert.Equal([LandActorRole.FirstActingSide, LandActorRole.SecondActingSide],
                    reserves.Select(position => position.ActorRole));
                Assert.All(reserves, position => Assert.Null(position.ActiveSide));
                var afterStage = Cna1979LandSequence.GetNext(stagePositions[^1]);
                if (stage < 3)
                {
                    Assert.Equal(stage + 1, afterStage.OperationStage);
                    Assert.Equal(LandPhaseIds.InitiativeDeclaration, afterStage.PhaseId);
                }
                else
                {
                    Assert.Equal(LandPhaseIds.EndOfTurn, afterStage.PhaseId);
                }
            }
            Assert.Equal(turn + 1, Cna1979LandSequence.GetNext(positions[^1]).GameTurn);
        }
        Assert.Equal(672, allPositions);
    }
}
