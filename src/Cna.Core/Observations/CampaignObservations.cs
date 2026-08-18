using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public static class CampaignObservations
{
    public static CampaignObservationProjectionResult Query(
        CampaignAuthorityHandle handle,
        LandSide observer)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return CampaignObservationProjector.Project(handle.Snapshot, handle.Context, observer);
    }
}
