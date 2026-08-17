namespace Cna.Core.Observations;

public enum CampaignObservationRejectionReason
{
    None,
    InvalidObserver,
    InvalidState,
}

public sealed record CampaignObservationProjectionResult
{
    private CampaignObservationProjectionResult(
        CampaignObservation? observation,
        CampaignObservationRejectionReason rejectionReason)
    {
        Observation = observation;
        RejectionReason = rejectionReason;
    }

    public bool IsProjected => Observation is not null;

    public CampaignObservation? Observation { get; }

    public CampaignObservationRejectionReason RejectionReason { get; }

    public static CampaignObservationProjectionResult Projected(
        CampaignObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return new CampaignObservationProjectionResult(
            observation,
            CampaignObservationRejectionReason.None);
    }

    public static CampaignObservationProjectionResult Rejected(
        CampaignObservationRejectionReason reason)
    {
        if (reason == CampaignObservationRejectionReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CampaignObservationProjectionResult(null, reason);
    }
}
