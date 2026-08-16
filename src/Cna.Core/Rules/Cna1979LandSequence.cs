namespace Cna.Core.Rules;

public static class Cna1979LandSequence
{
    public const int ContractVersion = 1;

    public static RuleReference SourceReference { get; } = new(
        "spi-1979-land-rules",
        "5.2");

    public static RuleReference OperationStageOrderSourceReference { get; } = new(
        "spi-1979-land-rules",
        "7.12");

    private static IReadOnlyList<RuleReference> PlayerExecutionSources { get; } =
        Array.AsReadOnly(
        [
            SourceReference,
            OperationStageOrderSourceReference,
        ]);

    public static IReadOnlyList<LandSequencePosition> CreateTurn(
        int gameTurn,
        LandSide firstPlayer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);

        if (!Enum.IsDefined(firstPlayer))
        {
            throw new ArgumentOutOfRangeException(nameof(firstPlayer));
        }

        var secondPlayer = firstPlayer == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var positions = new List<LandSequencePosition>();

        AddPhase(
            positions,
            gameTurn,
            0,
            "initiative-determination",
            LandStageIds.InitiativeDetermination,
            LandPhaseIds.InitiativeDetermination);
        AddPhase(
            positions,
            gameTurn,
            0,
            "naval-convoy.schedule",
            LandStageIds.NavalConvoy,
            LandPhaseIds.NavalConvoySchedule);
        AddPhase(
            positions,
            gameTurn,
            0,
            "naval-convoy.tactical-shipping",
            LandStageIds.NavalConvoy,
            LandPhaseIds.TacticalShipping);

        for (var operationStage = 1; operationStage <= 3; operationStage++)
        {
            var firstActingSide = operationStage == 2 ? secondPlayer : firstPlayer;
            var secondActingSide = operationStage == 2 ? firstPlayer : secondPlayer;

            AddOperationPrelude(positions, gameTurn, operationStage, firstPlayer);
            AddPlayerPhase(positions, gameTurn, operationStage, "first-player", firstActingSide);
            AddPlayerPhase(positions, gameTurn, operationStage, "second-player", secondActingSide);
        }

        AddPhase(
            positions,
            gameTurn,
            0,
            "end-of-turn",
            LandStageIds.EndOfTurn,
            LandPhaseIds.EndOfTurn);

        return Array.AsReadOnly(positions.ToArray());
    }

    public static LandSequencePosition GetNext(
        LandSequencePosition current,
        LandSide firstPlayer)
    {
        ArgumentNullException.ThrowIfNull(current);

        var positions = CreateTurn(current.GameTurn, firstPlayer);
        var currentIndex = positions
            .Select((position, index) => (position, index))
            .SingleOrDefault(candidate => candidate.position == current)
            .index;

        if (currentIndex == 0 && positions[0] != current)
        {
            throw new ArgumentException(
                "The current position does not belong to the declared Land turn.",
                nameof(current));
        }

        return currentIndex == positions.Count - 1
            ? CreateTurn(checked(current.GameTurn + 1), firstPlayer)[0]
            : positions[currentIndex + 1];
    }

    private static void AddOperationPrelude(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        LandSide firstPlayer)
    {
        AddOperationPhase(positions, gameTurn, operationStage, "initiative-declaration", LandPhaseIds.InitiativeDeclaration, firstPlayer);
        AddOperationPhase(positions, gameTurn, operationStage, "weather-determination", LandPhaseIds.WeatherDetermination, firstPlayer);
        AddOperationSegment(positions, gameTurn, operationStage, "organization.reorganization", LandPhaseIds.Organization, LandSegmentIds.Reorganization);
        AddOperationStep(positions, gameTurn, operationStage, "organization.construction.completion", LandPhaseIds.Organization, LandSegmentIds.Construction, LandStepIds.ConstructionCompletion);
        AddOperationStep(positions, gameTurn, operationStage, "organization.construction.initiation-continuation", LandPhaseIds.Organization, LandSegmentIds.Construction, LandStepIds.ConstructionInitiationContinuation);
        AddOperationStep(positions, gameTurn, operationStage, "organization.training.completion", LandPhaseIds.Organization, LandSegmentIds.Training, LandStepIds.TrainingCompletion);
        AddOperationStep(positions, gameTurn, operationStage, "organization.training.initiation-continuation", LandPhaseIds.Organization, LandSegmentIds.Training, LandStepIds.TrainingInitiationContinuation);
        AddOperationPhase(positions, gameTurn, operationStage, "naval-convoy-arrival", LandPhaseIds.NavalConvoyArrival);
        AddOperationSegment(positions, gameTurn, operationStage, "commonwealth-fleet.assignment", LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment, LandSide.Commonwealth);
        AddOperationSegment(positions, gameTurn, operationStage, "commonwealth-fleet.repair", LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair, LandSide.Commonwealth);
    }

    private static void AddPlayerPhase(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string playerOrderId,
        LandSide activeSide)
    {
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.reserve-designation", LandPhaseIds.ReserveDesignation, activeSide, PlayerExecutionSources);

        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.movement", LandPhaseIds.MovementAndCombat, LandSegmentIds.Movement, activeSide, PlayerExecutionSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.breakdown-determination", LandPhaseIds.MovementAndCombat, LandSegmentIds.BreakdownDetermination, activeSide, PlayerExecutionSources);

        var combatSteps = new[]
        {
            LandStepIds.PositionDetermination,
            LandStepIds.Barrage,
            LandStepIds.RetreatBeforeAssault,
            LandStepIds.ForceAssignment,
            LandStepIds.AntiArmor,
            LandStepIds.CloseAssault,
        };

        foreach (var stepId in combatSteps)
        {
            var suffix = stepId[(stepId.LastIndexOf('.') + 1)..];
            AddOperationStep(
                positions,
                gameTurn,
                operationStage,
                $"{playerOrderId}.movement-and-combat.combat.{suffix}",
                LandPhaseIds.MovementAndCombat,
                LandSegmentIds.Combat,
                stepId,
                activeSide,
                PlayerExecutionSources);
        }

        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.reserve-release", LandPhaseIds.MovementAndCombat, LandSegmentIds.ReserveRelease, activeSide, PlayerExecutionSources);

        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.truck-convoy-movement", LandPhaseIds.TruckConvoyMovement, activeSide, PlayerExecutionSources);
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.commonwealth-rail-movement", LandPhaseIds.CommonwealthRailMovement, activeSide, PlayerExecutionSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.repair.towing", LandPhaseIds.Repair, LandSegmentIds.Towing, activeSide, PlayerExecutionSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.repair.maintenance", LandPhaseIds.Repair, LandSegmentIds.Maintenance, activeSide, PlayerExecutionSources);
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.patrol", LandPhaseIds.Patrol, activeSide, PlayerExecutionSources);
    }

    private static void AddOperationPhase(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string stepSuffix,
        string phaseId,
        LandSide? activeSide = null,
        IEnumerable<RuleReference>? sources = null) => AddPhase(
            positions,
            gameTurn,
            operationStage,
            $"operation-{operationStage}.{stepSuffix}",
            LandStageIds.Operation,
            phaseId,
            activeSide: activeSide,
            sources: sources);

    private static void AddOperationSegment(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string stepSuffix,
        string phaseId,
        string segmentId,
        LandSide? activeSide = null,
        IEnumerable<RuleReference>? sources = null) => AddPhase(
            positions,
            gameTurn,
            operationStage,
            $"operation-{operationStage}.{stepSuffix}",
            LandStageIds.Operation,
            phaseId,
            segmentId,
            activeSide: activeSide,
            sources: sources);

    private static void AddOperationStep(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string positionSuffix,
        string phaseId,
        string segmentId,
        string stepId,
        LandSide? activeSide = null,
        IEnumerable<RuleReference>? sources = null) => AddPhase(
            positions,
            gameTurn,
            operationStage,
            $"operation-{operationStage}.{positionSuffix}",
            LandStageIds.Operation,
            phaseId,
            segmentId,
            stepId,
            activeSide,
            sources);

    private static void AddPhase(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string positionSuffix,
        string stageId,
        string phaseId,
        string? segmentId = null,
        string? stepId = null,
        LandSide? activeSide = null,
        IEnumerable<RuleReference>? sources = null) => positions.Add(new LandSequencePosition(
            ContractVersion,
            $"land.position.{positionSuffix}",
            gameTurn,
            operationStage,
            stageId,
            phaseId,
            segmentId,
            stepId,
            sources ?? [SourceReference],
            activeSide));
}
