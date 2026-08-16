namespace Cna.Core.Rules;

public static class LandStageIds
{
    public const string InitiativeDetermination = "land.stage.initiative-determination";
    public const string NavalConvoy = "land.stage.naval-convoy";
    public const string Operation = "land.stage.operation";
    public const string EndOfTurn = "land.stage.end-of-turn";
}

public static class LandPhaseIds
{
    public const string InitiativeDetermination = "land.phase.initiative-determination";
    public const string NavalConvoySchedule = "land.phase.naval-convoy-schedule";
    public const string TacticalShipping = "land.phase.tactical-shipping";
    public const string InitiativeDeclaration = "land.phase.initiative-declaration";
    public const string WeatherDetermination = "land.phase.weather-determination";
    public const string Organization = "land.phase.organization";
    public const string NavalConvoyArrival = "land.phase.naval-convoy-arrival";
    public const string CommonwealthFleet = "land.phase.commonwealth-fleet";
    public const string ReserveDesignation = "land.phase.reserve-designation";
    public const string MovementAndCombat = "land.phase.movement-and-combat";
    public const string TruckConvoyMovement = "land.phase.truck-convoy-movement";
    public const string CommonwealthRailMovement = "land.phase.commonwealth-rail-movement";
    public const string Repair = "land.phase.repair";
    public const string Patrol = "land.phase.patrol";
    public const string EndOfTurn = "land.phase.end-of-turn";
}

public static class LandSegmentIds
{
    public const string Reorganization = "land.segment.reorganization";
    public const string Construction = "land.segment.construction";
    public const string Training = "land.segment.training";
    public const string FleetAssignment = "land.segment.fleet-assignment";
    public const string FleetRepair = "land.segment.fleet-repair";
    public const string Movement = "land.segment.movement";
    public const string BreakdownDetermination = "land.segment.breakdown-determination";
    public const string Combat = "land.segment.combat";
    public const string ReserveRelease = "land.segment.reserve-release";
    public const string Towing = "land.segment.towing";
    public const string Maintenance = "land.segment.maintenance";
}

public static class LandStepIds
{
    public const string ConstructionCompletion = "land.step.construction-completion";
    public const string ConstructionInitiationContinuation = "land.step.construction-initiation-continuation";
    public const string TrainingCompletion = "land.step.training-completion";
    public const string TrainingInitiationContinuation = "land.step.training-initiation-continuation";
    public const string PositionDetermination = "land.step.position-determination";
    public const string Barrage = "land.step.barrage";
    public const string RetreatBeforeAssault = "land.step.retreat-before-assault";
    public const string ForceAssignment = "land.step.force-assignment";
    public const string AntiArmor = "land.step.anti-armor";
    public const string CloseAssault = "land.step.close-assault";
}
