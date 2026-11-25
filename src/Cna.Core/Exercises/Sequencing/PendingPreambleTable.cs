using Cna.Core.Rules;

namespace Cna.Core.Exercises.Sequencing;

/// <summary>
/// Preamble (Initiative Declaration through Fleet Repair) subtype names, keyed by the completed
/// catalog position's (operation stage, phase, segment). The names are irregular
/// ("StageEntryWeatherPending" beside "NoObligation*Pending"), so they are data.
/// </summary>
internal static class PendingPreambleTable
{
    private static readonly Dictionary<(int Stage, string Phase, string? Segment), string> Rows = new()
    {
        [(2, LandPhaseIds.InitiativeDeclaration, null)] = "InitiativeDeclarationPending",
        [(2, LandPhaseIds.WeatherDetermination, null)] = "StageEntryWeatherPending",
        [(2, LandPhaseIds.Organization, null)] = "NoObligationOrganizationPending",
        [(2, LandPhaseIds.NavalConvoyArrival, null)] = "NoObligationNavalConvoyArrivalPending",
        [(2, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "NoObligationFleetAssignmentPending",
        [(2, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "NoObligationFleetRepairPending",
        [(3, LandPhaseIds.InitiativeDeclaration, null)] = "ThirdStageInitiativeDeclarationPending",
        [(3, LandPhaseIds.WeatherDetermination, null)] = "ThirdStageWeatherPending",
        [(3, LandPhaseIds.Organization, null)] = "ThirdStageNoObligationOrganizationPending",
        [(3, LandPhaseIds.NavalConvoyArrival, null)] = "ThirdStageNoObligationNavalConvoyArrivalPending",
        [(3, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment)] = "ThirdStageNoObligationFleetAssignmentPending",
        [(3, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair)] = "ThirdStageNoObligationFleetRepairPending",
    };

    internal static string? Find(int operationStage, string phaseId, string? segmentId) =>
        Rows.GetValueOrDefault((operationStage, phaseId, segmentId));
}
