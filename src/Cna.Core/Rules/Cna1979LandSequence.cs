namespace Cna.Core.Rules;

public static class Cna1979LandSequence
{
    public const int ContractVersion = 3;
    public const int CatalogSchemaVersion = 3;

    public static RuleReference SourceReference { get; } = new("spi-1979-land-rules", "5.2");
    public static RuleReference InitiativeSideSourceReference { get; } = new("spi-1979-land-rules", "7.11");
    public static RuleReference StageChoiceSourceReference { get; } = new("spi-1979-land-rules", "7.14");

    private static IReadOnlyList<RuleReference> RelativeActorSources { get; } =
        Array.AsReadOnly(
        [
            SourceReference,
            InitiativeSideSourceReference,
            StageChoiceSourceReference,
        ]);

    public static IReadOnlyList<LandSequencePosition> CreateTurn(int gameTurn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        var positions = new List<LandSequencePosition>();

        AddPhase(positions, gameTurn, 0, "initiative-determination", LandStageIds.InitiativeDetermination, LandPhaseIds.InitiativeDetermination);
        AddPhase(positions, gameTurn, 0, "naval-convoy.schedule", LandStageIds.NavalConvoy, LandPhaseIds.NavalConvoySchedule);
        AddPhase(positions, gameTurn, 0, "naval-convoy.tactical-shipping", LandStageIds.NavalConvoy, LandPhaseIds.TacticalShipping);

        for (var operationStage = 1; operationStage <= 3; operationStage++)
        {
            AddOperationPrelude(positions, gameTurn, operationStage);
            AddPlayerPhase(positions, gameTurn, operationStage, "first-player", LandActorRole.FirstActingSide);
            AddPlayerPhase(positions, gameTurn, operationStage, "second-player", LandActorRole.SecondActingSide);
        }

        AddPhase(positions, gameTurn, 0, "end-of-turn", LandStageIds.EndOfTurn, LandPhaseIds.EndOfTurn);
        return Array.AsReadOnly(positions.ToArray());
    }

    public static LandSequencePosition GetNext(LandSequencePosition current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var positions = CreateTurn(current.GameTurn);
        var currentIndex = positions.ToList().FindIndex(position => position == current);

        if (currentIndex < 0)
        {
            throw new ArgumentException(
                "The current position does not belong to the declared Land turn.",
                nameof(current));
        }

        return currentIndex == positions.Count - 1
            ? CreateTurn(checked(current.GameTurn + 1))[0]
            : positions[currentIndex + 1];
    }

    private static void AddOperationPrelude(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage)
    {
        AddOperationPhase(positions, gameTurn, operationStage, "initiative-declaration", LandPhaseIds.InitiativeDeclaration, LandActorRole.InitiativeHolder, RelativeActorSources);
        AddOperationPhase(positions, gameTurn, operationStage, "weather-determination", LandPhaseIds.WeatherDetermination, LandActorRole.InitiativeHolder, RelativeActorSources);
        AddOperationPhase(positions, gameTurn, operationStage, "organization", LandPhaseIds.Organization);
        AddOperationPhase(positions, gameTurn, operationStage, "naval-convoy-arrival", LandPhaseIds.NavalConvoyArrival);
        AddOperationSegment(positions, gameTurn, operationStage, "commonwealth-fleet.assignment", LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment, LandActorRole.Commonwealth);
        AddOperationSegment(positions, gameTurn, operationStage, "commonwealth-fleet.repair", LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair, LandActorRole.Commonwealth);
    }

    private static void AddPlayerPhase(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string playerOrderId,
        LandActorRole actorRole)
    {
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.reserve-designation", LandPhaseIds.ReserveDesignation, actorRole, RelativeActorSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.movement", LandPhaseIds.MovementAndCombat, LandSegmentIds.Movement, actorRole, RelativeActorSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.breakdown-determination", LandPhaseIds.MovementAndCombat, LandSegmentIds.BreakdownDetermination, actorRole, RelativeActorSources);

        string[] combatSteps =
        [
            LandStepIds.PositionDetermination,
            LandStepIds.Barrage,
            LandStepIds.RetreatBeforeAssault,
            LandStepIds.ForceAssignment,
            LandStepIds.AntiArmor,
            LandStepIds.CloseAssault,
        ];

        foreach (var stepId in combatSteps)
        {
            var suffix = stepId[(stepId.LastIndexOf('.') + 1)..];
            AddOperationStep(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.combat.{suffix}", LandPhaseIds.MovementAndCombat, LandSegmentIds.Combat, stepId, actorRole, RelativeActorSources);
        }

        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.movement-and-combat.reserve-release", LandPhaseIds.MovementAndCombat, LandSegmentIds.ReserveRelease, actorRole, RelativeActorSources);
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.truck-convoy-movement", LandPhaseIds.TruckConvoyMovement, actorRole, RelativeActorSources);
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.commonwealth-rail-movement", LandPhaseIds.CommonwealthRailMovement, actorRole, RelativeActorSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.repair.towing", LandPhaseIds.Repair, LandSegmentIds.Towing, actorRole, RelativeActorSources);
        AddOperationSegment(positions, gameTurn, operationStage, $"{playerOrderId}.repair.maintenance", LandPhaseIds.Repair, LandSegmentIds.Maintenance, actorRole, RelativeActorSources);
        AddOperationPhase(positions, gameTurn, operationStage, $"{playerOrderId}.patrol", LandPhaseIds.Patrol, actorRole, RelativeActorSources);
    }

    private static void AddOperationPhase(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string stepSuffix,
        string phaseId,
        LandActorRole actorRole = LandActorRole.None,
        IEnumerable<RuleReference>? sources = null) => AddPhase(
            positions,
            gameTurn,
            operationStage,
            $"operation-{operationStage}.{stepSuffix}",
            LandStageIds.Operation,
            phaseId,
            actorRole: actorRole,
            sources: sources);

    private static void AddOperationSegment(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string stepSuffix,
        string phaseId,
        string segmentId,
        LandActorRole actorRole = LandActorRole.None,
        IEnumerable<RuleReference>? sources = null) => AddPhase(
            positions,
            gameTurn,
            operationStage,
            $"operation-{operationStage}.{stepSuffix}",
            LandStageIds.Operation,
            phaseId,
            segmentId,
            actorRole: actorRole,
            sources: sources);

    private static void AddOperationStep(
        ICollection<LandSequencePosition> positions,
        int gameTurn,
        int operationStage,
        string positionSuffix,
        string phaseId,
        string segmentId,
        string stepId,
        LandActorRole actorRole = LandActorRole.None,
        IEnumerable<RuleReference>? sources = null) => AddPhase(
            positions,
            gameTurn,
            operationStage,
            $"operation-{operationStage}.{positionSuffix}",
            LandStageIds.Operation,
            phaseId,
            segmentId,
            stepId,
            actorRole,
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
        LandActorRole actorRole = LandActorRole.None,
        IEnumerable<RuleReference>? sources = null) => positions.Add(new LandSequencePosition(
            ContractVersion,
            $"land.position.{positionSuffix}",
            gameTurn,
            operationStage,
            stageId,
            phaseId,
            segmentId,
            stepId,
            actorRole,
            actorRole == LandActorRole.Commonwealth ? LandSide.Commonwealth : null,
            (sources ?? [SourceReference]).ToArray()));
}
