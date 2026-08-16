using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class LandSequenceTests
{
    [Fact]
    public void OpeningSequenceReachesTheFirstActingSidesMovementSegment()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);

        var movement = positions.First(position =>
            position.OperationStage == 1
            && position.ActorRole == LandActorRole.FirstActingSide
            && position.SegmentId == LandSegmentIds.Movement);

        Assert.Equal(LandPhaseIds.MovementAndCombat, movement.PhaseId);
        Assert.Null(movement.ActiveSide);
    }

    [Fact]
    public void EveryPositionHasAUniqueStableIdAndSerializableVersion2Contract()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);

        Assert.All(positions, position =>
        {
            Assert.Equal(2, position.ContractVersion);
            Assert.False(string.IsNullOrWhiteSpace(position.PositionId));
            Assert.Contains(Cna1979LandSequence.SourceReference, position.Sources);

            var json = JsonSerializer.Serialize(position);
            var roundTrip = JsonSerializer.Deserialize<LandSequencePosition>(json);

            Assert.Equal(position, roundTrip);
        });
        Assert.Equal(
            positions.Count,
            positions.Select(position => position.PositionId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RelativeActorPositionsCarryExactActorSemanticsSources()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);
        var relativePositions = positions.Where(position => position.ActorRole is
            LandActorRole.InitiativeHolder
            or LandActorRole.FirstActingSide
            or LandActorRole.SecondActingSide);

        Assert.NotEmpty(relativePositions);
        Assert.All(relativePositions, position => Assert.Equal(
            [
                Cna1979LandSequence.SourceReference,
                Cna1979LandSequence.InitiativeSideSourceReference,
                Cna1979LandSequence.StageChoiceSourceReference,
            ],
            position.Sources));
    }

    [Fact]
    public void PositionsWithoutRelativeActorsDoNotClaimActorChoiceSources()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);
        var unrelated = positions.Where(position => position.ActorRole is
            LandActorRole.None or LandActorRole.Commonwealth);

        Assert.NotEmpty(unrelated);
        Assert.All(unrelated, position => Assert.Equal(
            [Cna1979LandSequence.SourceReference],
            position.Sources));
    }

    [Fact]
    public void PositionDefensivelyCopiesItsSourceCollection()
    {
        var sources = new List<RuleReference>
        {
            Cna1979LandSequence.SourceReference,
            Cna1979LandSequence.InitiativeSideSourceReference,
        };
        var position = new LandSequencePosition(
            2,
            "land.position.test",
            1,
            1,
            LandStageIds.Operation,
            LandPhaseIds.ReserveDesignation,
            null,
            null,
            LandActorRole.FirstActingSide,
            null,
            sources);

        sources.Clear();

        Assert.Equal(2, position.Sources.Count);
    }

    [Fact]
    public void PositionRequiresAtLeastOneSource()
    {
        var exception = Assert.Throws<ArgumentException>(() => new LandSequencePosition(
            2,
            "land.position.test",
            1,
            1,
            LandStageIds.Operation,
            LandPhaseIds.ReserveDesignation,
            null,
            null,
            LandActorRole.FirstActingSide,
            null,
            []));

        Assert.Equal("sources", exception.ParamName);
    }

    [Fact]
    public void EveryOperationStageRetainsUnresolvedFirstAndSecondActorRoles()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);

        for (var operationStage = 1; operationStage <= 3; operationStage++)
        {
            var reserveDesignations = positions
                .Where(position =>
                    position.OperationStage == operationStage
                    && position.PhaseId == LandPhaseIds.ReserveDesignation)
                .ToArray();

            Assert.Equal(2, reserveDesignations.Length);
            Assert.Equal(
                [LandActorRole.FirstActingSide, LandActorRole.SecondActingSide],
                reserveDesignations.Select(position => position.ActorRole));
            Assert.All(reserveDesignations, position => Assert.Null(position.ActiveSide));
        }
    }

    [Fact]
    public void ActorSemanticsReferenceTheirExactPrimarySources()
    {
        Assert.Equal(
            new RuleReference("spi-1979-land-rules", "7.11"),
            Cna1979LandSequence.InitiativeSideSourceReference);
        Assert.Equal(
            new RuleReference("spi-1979-land-rules", "7.14"),
            Cna1979LandSequence.StageChoiceSourceReference);
    }

    [Fact]
    public void InitiativeDeclarationAndWeatherBelongToTheUnresolvedHolder()
    {
        var holderOwned = Cna1979LandSequence.CreateTurn(1).Where(position =>
            position.OperationStage == 1
            && (position.PhaseId == LandPhaseIds.InitiativeDeclaration
                || position.PhaseId == LandPhaseIds.WeatherDetermination));

        Assert.All(holderOwned, position =>
        {
            Assert.Equal(LandActorRole.InitiativeHolder, position.ActorRole);
            Assert.Null(position.ActiveSide);
        });
    }

    [Fact]
    public void CommonwealthFleetHasAConcreteCommonwealthActor()
    {
        var fleetPositions = Cna1979LandSequence.CreateTurn(1).Where(position =>
            position.PhaseId == LandPhaseIds.CommonwealthFleet);

        Assert.NotEmpty(fleetPositions);
        Assert.All(fleetPositions, position =>
        {
            Assert.Equal(LandActorRole.Commonwealth, position.ActorRole);
            Assert.Equal(LandSide.Commonwealth, position.ActiveSide);
        });
    }

    [Fact]
    public void PositionRejectsActorAndActiveSideContradictions()
    {
        Assert.Throws<ArgumentException>(() => new LandSequencePosition(
            2,
            "land.position.test",
            1,
            0,
            LandStageIds.NavalConvoy,
            LandPhaseIds.NavalConvoySchedule,
            null,
            null,
            LandActorRole.None,
            LandSide.Axis,
            [Cna1979LandSequence.SourceReference]));
        Assert.Throws<ArgumentException>(() => new LandSequencePosition(
            2,
            "land.position.test",
            1,
            1,
            LandStageIds.Operation,
            LandPhaseIds.CommonwealthFleet,
            null,
            null,
            LandActorRole.Commonwealth,
            LandSide.Axis,
            [Cna1979LandSequence.SourceReference]));
    }

    [Fact]
    public void CombatUsesThePublishedSegmentAndStepHierarchy()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);
        var movement = positions.First(position =>
            position.OperationStage == 1
            && position.ActorRole == LandActorRole.FirstActingSide
            && position.SegmentId == LandSegmentIds.Movement);
        var positionDetermination = positions.First(position =>
            position.OperationStage == 1
            && position.ActorRole == LandActorRole.FirstActingSide
            && position.StepId == LandStepIds.PositionDetermination);

        Assert.Null(movement.StepId);
        Assert.Equal(LandPhaseIds.MovementAndCombat, positionDetermination.PhaseId);
        Assert.Equal(LandSegmentIds.Combat, positionDetermination.SegmentId);
    }
}
