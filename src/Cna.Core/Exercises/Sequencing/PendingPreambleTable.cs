using Cna.Core.Rules;

namespace Cna.Core.Exercises.Sequencing;

/// <summary>
/// Preamble (Initiative Declaration through Fleet Repair) subtype names, keyed by the completed
/// catalog position's (operation stage, phase, segment). The names are irregular
/// ("StageEntryWeatherPending" beside "NoObligation*Pending"), so they are data.
/// </summary>
internal static class PendingPreambleTable
{
    private static readonly Dictionary<(bool IsFirstTurn, int Stage, string Phase, string? Segment), string> Rows = new()
    {
        [(false, 0, LandPhaseIds.EndOfTurn, null)] = "TurnBoundaryPending",
        [(false, 0, LandPhaseIds.InitiativeDetermination, null)] = "TurnBoundaryPending",
        [(false, 0, LandPhaseIds.NavalConvoySchedule, null)] = "TurnBoundaryPending",
        [(false, 0, LandPhaseIds.TacticalShipping, null)] = "TurnBoundaryPending",
        [(false, 1, LandPhaseIds.InitiativeDeclaration, null)] = "TurnBoundaryPending",
        [(false, 1, LandPhaseIds.WeatherDetermination, null)] = "TurnBoundaryPending",
        [(false, 1, LandPhaseIds.Organization, null)] = "TurnBoundaryPending",
        [(false, 1, LandPhaseIds.NavalConvoyArrival, null)] = "TurnBoundaryPending",
        [(false, 1, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "TurnBoundaryPending",
        [(false, 1, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "TurnBoundaryPending",
        [(true, 2, LandPhaseIds.InitiativeDeclaration, null)] = "InitiativeDeclarationPending",
        [(true, 2, LandPhaseIds.WeatherDetermination, null)] = "StageEntryWeatherPending",
        [(true, 2, LandPhaseIds.Organization, null)] = "NoObligationOrganizationPending",
        [(true, 2, LandPhaseIds.NavalConvoyArrival, null)] = "NoObligationNavalConvoyArrivalPending",
        [(true, 2, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "NoObligationFleetAssignmentPending",
        [(true, 2, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "NoObligationFleetRepairPending",
        [(true, 3, LandPhaseIds.InitiativeDeclaration, null)] = "ThirdStageInitiativeDeclarationPending",
        [(true, 3, LandPhaseIds.WeatherDetermination, null)] = "ThirdStageWeatherPending",
        [(true, 3, LandPhaseIds.Organization, null)] = "ThirdStageNoObligationOrganizationPending",
        [(true, 3, LandPhaseIds.NavalConvoyArrival, null)] = "ThirdStageNoObligationNavalConvoyArrivalPending",
        [(true, 3, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "ThirdStageNoObligationFleetAssignmentPending",
        [(true, 3, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "ThirdStageNoObligationFleetRepairPending",
    };

    internal static string? Find(int operationStage, string phaseId, string? segmentId, bool isFirstTurn) =>
        Rows.GetValueOrDefault((isFirstTurn, operationStage, phaseId, segmentId));
}
