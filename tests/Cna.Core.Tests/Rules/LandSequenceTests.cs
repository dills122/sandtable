using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class LandSequenceTests
{
    [Fact]
    public void OpeningSequenceReachesTheFirstPlayersMovementSegment()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Axis);

        var opening = positions[0];
        var movement = positions.First(position =>
            position.OperationStage == 1
            && position.ActiveSide == LandSide.Axis
            && position.PhaseId == LandPhaseIds.MovementAndCombat
            && position.SegmentId == LandSegmentIds.Movement);

        Assert.Equal(LandStageIds.InitiativeDetermination, opening.StageId);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, opening.PhaseId);
        Assert.Null(opening.SegmentId);
        Assert.Null(opening.ActiveSide);
        Assert.Equal(LandStageIds.Operation, movement.StageId);
        Assert.Equal(1, movement.GameTurn);
    }

    [Fact]
    public void EveryPositionHasAUniqueStableIdAndSerializableContractVersion()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth);

        Assert.All(positions, position =>
        {
            Assert.Equal(1, position.ContractVersion);
            Assert.False(string.IsNullOrWhiteSpace(position.PositionId));
            Assert.Equal(Cna1979LandSequence.SourceReference, position.Source);

            var json = JsonSerializer.Serialize(position);
            var roundTrip = JsonSerializer.Deserialize<LandSequencePosition>(json);

            Assert.Equal(position, roundTrip);
        });
        Assert.Equal(
            positions.Count,
            positions
                .Select(position => position.PositionId)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void EachOperationStageContainsBothPlayerPhasesInInitiativeOrder()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth);

        for (var operationStage = 1; operationStage <= 3; operationStage++)
        {
            var reserveDesignations = positions
                .Where(position =>
                    position.OperationStage == operationStage
                    && position.PhaseId == LandPhaseIds.ReserveDesignation)
                .ToArray();

            Assert.Equal(2, reserveDesignations.Length);
            Assert.Equal(LandSide.Commonwealth, reserveDesignations[0].ActiveSide);
            Assert.Equal(LandSide.Axis, reserveDesignations[1].ActiveSide);
        }
    }

    [Fact]
    public void InitiativePlayerOwnsInitiativeDeclarationAndWeatherDetermination()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth);
        var playerOwnedPrelude = positions.Where(position =>
            position.OperationStage == 1
            && (position.PhaseId == LandPhaseIds.InitiativeDeclaration
                || position.PhaseId == LandPhaseIds.WeatherDetermination));

        Assert.All(
            playerOwnedPrelude,
            position => Assert.Equal(LandSide.Commonwealth, position.ActiveSide));
    }

    [Fact]
    public void UndefinedFirstPlayerIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Cna1979LandSequence.CreateTurn(1, (LandSide)999));
    }

    [Fact]
    public void CombatUsesThePublishedSegmentAndStepHierarchy()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Axis);

        var movement = positions.First(position =>
            position.OperationStage == 1
            && position.ActiveSide == LandSide.Axis
            && position.SegmentId == LandSegmentIds.Movement);
        var positionDetermination = positions.First(position =>
            position.OperationStage == 1
            && position.ActiveSide == LandSide.Axis
            && position.StepId == LandStepIds.PositionDetermination);

        Assert.Null(movement.StepId);
        Assert.Equal(LandPhaseIds.MovementAndCombat, positionDetermination.PhaseId);
        Assert.Equal(LandSegmentIds.Combat, positionDetermination.SegmentId);
    }
}
