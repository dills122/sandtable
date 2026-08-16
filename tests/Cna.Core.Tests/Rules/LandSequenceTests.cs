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
            Assert.Contains(Cna1979LandSequence.SourceReference, position.Sources);

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
    public void PlayerExecutionPositionsCarrySequenceAndActingOrderSources()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth);
        var playerExecutionPositions = positions.Where(position =>
            position.PositionId.Contains(".first-player.", StringComparison.Ordinal)
            || position.PositionId.Contains(".second-player.", StringComparison.Ordinal));

        Assert.NotEmpty(playerExecutionPositions);
        Assert.All(
            playerExecutionPositions,
            position => Assert.Equal(
                [
                    Cna1979LandSequence.SourceReference,
                    Cna1979LandSequence.OperationStageOrderSourceReference,
                ],
                position.Sources));
    }

    [Fact]
    public void PositionsUnrelatedToPlayerExecutionOrderDoNotClaimItsSource()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth);
        var unrelatedPositions = positions.Where(position =>
            position.OperationStage == 0
            || !position.PositionId.Contains("-player.", StringComparison.Ordinal));

        Assert.NotEmpty(unrelatedPositions);
        Assert.All(
            unrelatedPositions,
            position => Assert.Equal(
                [Cna1979LandSequence.SourceReference],
                position.Sources));
    }

    [Fact]
    public void PositionDefensivelyCopiesItsSourceCollection()
    {
        var sources = new List<RuleReference>
        {
            Cna1979LandSequence.SourceReference,
            Cna1979LandSequence.OperationStageOrderSourceReference,
        };
        var position = new LandSequencePosition(
            1,
            "land.position.test",
            1,
            1,
            LandStageIds.Operation,
            LandPhaseIds.ReserveDesignation,
            null,
            null,
            sources,
            LandSide.Axis);

        sources.Clear();

        Assert.Equal(2, position.Sources.Count);
    }

    [Fact]
    public void PositionRequiresAtLeastOneSource()
    {
        var exception = Assert.Throws<ArgumentException>(() => new LandSequencePosition(
            1,
            "land.position.test",
            1,
            1,
            LandStageIds.Operation,
            LandPhaseIds.ReserveDesignation,
            null,
            null,
            Array.Empty<RuleReference>(),
            LandSide.Axis));

        Assert.Equal("sources", exception.ParamName);
    }

    [Fact]
    public void InitiativeHolderActsFirstInStagesOneAndThreeAndLastInStageTwo()
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth);
        var expectedOrderByStage = new Dictionary<int, LandSide[]>
        {
            [1] = [LandSide.Commonwealth, LandSide.Axis],
            [2] = [LandSide.Axis, LandSide.Commonwealth],
            [3] = [LandSide.Commonwealth, LandSide.Axis],
        };

        for (var operationStage = 1; operationStage <= 3; operationStage++)
        {
            var reserveDesignations = positions
                .Where(position =>
                    position.OperationStage == operationStage
                    && position.PhaseId == LandPhaseIds.ReserveDesignation)
                .ToArray();

            Assert.Equal(2, reserveDesignations.Length);
            Assert.Equal(
                expectedOrderByStage[operationStage],
                reserveDesignations.Select(position => position.ActiveSide!.Value));
            Assert.EndsWith(
                ".first-player.reserve-designation",
                reserveDesignations[0].PositionId,
                StringComparison.Ordinal);
            Assert.EndsWith(
                ".second-player.reserve-designation",
                reserveDesignations[1].PositionId,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OperationStageOrderReferencesItsPrimarySource()
    {
        Assert.Equal(
            new RuleReference("spi-1979-land-rules", "7.12"),
            Cna1979LandSequence.OperationStageOrderSourceReference);
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
