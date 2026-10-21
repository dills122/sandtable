using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

/// <summary>Shared owner-only fields for historical and contact-based observations.</summary>
internal static class CampaignOwnElementProjection
{
    public static ObservedOwnElement Project(ContentCombatElement content,
        CampaignElementStateV5 state) => new(
        content.ElementId,
        content.ParentFormationId,
        content.OrganizationId,
        content.BaseCapabilityPointAllowance,
        state.CurrentLocationId,
        ProjectReserveStatus(state.ReserveStatus),
        content.MobilityId,
        state.OperationalState.LedgerGameTurn,
        state.OperationalState.LedgerOperationStage,
        state.OperationalState.CapabilityPointsExpended,
        state.OperationalState.CohesionLevel,
        ProjectVehicleBreakdownRisk(content, state));

    public static bool IsMovementEndedFor(CampaignMovementEndedState? ended,
        LandSequencePosition position) => ended is not null
        && ended.SequenceContractVersion == position.ContractVersion
        && string.Equals(ended.PositionId, position.PositionId, StringComparison.Ordinal)
        && ended.GameTurn == position.GameTurn
        && ended.OperationStage == position.OperationStage
        && string.Equals(ended.StageId, position.StageId, StringComparison.Ordinal)
        && string.Equals(ended.PhaseId, position.PhaseId, StringComparison.Ordinal)
        && string.Equals(ended.SegmentId, position.SegmentId, StringComparison.Ordinal)
        && ended.PhasingSide == position.ActiveSide;

    private static ObservedOwnVehicleBreakdownRisk? ProjectVehicleBreakdownRisk(
        ContentCombatElement content, CampaignElementStateV5 state)
    {
        var cohort = content.BreakdownVehicleCohort;
        var breakdown = state.OperationalState.VehicleBreakdownState;
        return cohort is null || breakdown is null
            ? null
            : new ObservedOwnVehicleBreakdownRisk(
                breakdown.CohortId,
                cohort.VehicleTypeId,
                cohort.ProfileId,
                breakdown.CumulativeBreakdownPoints,
                breakdown.SandstormAttributedBreakdownPoints,
                breakdown.HighestEffectiveCheckedBandId,
                breakdown.WorkingPointCount,
                breakdown.BrokenPointCount);
    }

    private static CampaignObservationReserveStatus ProjectReserveStatus(
        CampaignElementReserveStatus status) => status switch
        {
            CampaignElementReserveStatus.None => CampaignObservationReserveStatus.None,
            CampaignElementReserveStatus.ReserveI => CampaignObservationReserveStatus.ReserveI,
            CampaignElementReserveStatus.ReserveII => CampaignObservationReserveStatus.ReserveII,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
