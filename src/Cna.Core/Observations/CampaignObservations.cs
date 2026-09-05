using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public static class CampaignObservations
{
    public static CampaignObservationV6ProjectionResult Query(
        CampaignAuthorityHandle handle,
        LandSide observer)
    {
        ArgumentNullException.ThrowIfNull(handle);
        var snapshot = handle.CurrentSnapshot;
        if (!Enum.IsDefined(observer)
            || snapshot is null
            || handle.Context.ArtifactV5 is null
            || !CampaignSnapshotV10Validator.IsValid(
                snapshot,
                handle.Context.ArtifactV5,
                handle.Context.Scenario))
        {
            return CampaignObservationV6ProjectionResult.Rejected(
                !Enum.IsDefined(observer)
                    ? CampaignObservationRejectionReason.InvalidObserver
                    : CampaignObservationRejectionReason.InvalidState);
        }

        var authority = CampaignElementMovedV2Factory.DeriveZocAuthority(
            snapshot.World,
            handle.Context.ArtifactV5,
            handle.Context.Scenario,
            observer == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis);
        return CampaignObservationV6ProjectionResult.Projected(
            CampaignObservationV6Projector.Project(
                snapshot,
                handle.Context.ArtifactV5,
                handle.Context.Scenario,
                observer,
                new CampaignObservationV6AuthorityFacts(
                    authority.ControlledLocationIds,
                    authority.SourceRepresentationIds)));
    }
}
