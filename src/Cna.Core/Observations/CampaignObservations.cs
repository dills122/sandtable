using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public static class CampaignObservations
{
    public static CampaignObservationV7ProjectionResult Query(
        CampaignAuthorityHandle handle,
        LandSide observer)
    {
        ArgumentNullException.ThrowIfNull(handle);
        var snapshot = handle.CurrentSnapshot;
        if (!Enum.IsDefined(observer)
            || snapshot is null
            || handle.Context.ArtifactV6 is null
            || !CampaignSnapshotV11Admission.IsValid(
                snapshot,
                handle.Context.ArtifactV6,
                handle.Context.Scenario))
        {
            return CampaignObservationV7ProjectionResult.Rejected(
                !Enum.IsDefined(observer)
                    ? CampaignObservationRejectionReason.InvalidObserver
                    : CampaignObservationRejectionReason.InvalidState);
        }

        var controlledLocations = CampaignElementMovedV3Factory.DeriveControlledLocationIds(
            snapshot.World, handle.Context.ArtifactV6, handle.Context.Scenario,
            observer == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis);
        return CampaignObservationV7ProjectionResult.Projected(
            CampaignObservationV7Projector.Project(
                snapshot,
                handle.Context.ArtifactV6,
                handle.Context.Scenario,
                observer,
                new CampaignObservationV6AuthorityFacts(
                    controlledLocations,
                    [])));
    }
}
