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
        if (reason == CampaignObservationRejectionReason.None
            || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CampaignObservationProjectionResult(null, reason);
    }
}

public sealed record CampaignObservationV6ProjectionResult
{
    private CampaignObservationV6ProjectionResult(
        CampaignObservationV6? observation,
        CampaignObservationRejectionReason rejectionReason)
    {
        Observation = observation;
        RejectionReason = rejectionReason;
    }

    public bool IsProjected => Observation is not null;

    public CampaignObservationV6? Observation { get; }

    public CampaignObservationRejectionReason RejectionReason { get; }

    public static CampaignObservationV6ProjectionResult Projected(
        CampaignObservationV6 observation) => new(
        observation ?? throw new ArgumentNullException(nameof(observation)),
        CampaignObservationRejectionReason.None);

    public static CampaignObservationV6ProjectionResult Rejected(
        CampaignObservationRejectionReason reason)
    {
        if (reason == CampaignObservationRejectionReason.None || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CampaignObservationV6ProjectionResult(null, reason);
    }
}

public sealed record CampaignObservationV7ProjectionResult
{
    private CampaignObservationV7ProjectionResult(
        CampaignObservationV7? observation,
        CampaignObservationRejectionReason rejectionReason)
    {
        Observation = observation;
        RejectionReason = rejectionReason;
    }

    public bool IsProjected => Observation is not null;

    public CampaignObservationV7? Observation { get; }

    public CampaignObservationRejectionReason RejectionReason { get; }

    public static CampaignObservationV7ProjectionResult Projected(
        CampaignObservationV7 observation) => new(
        observation ?? throw new ArgumentNullException(nameof(observation)),
        CampaignObservationRejectionReason.None);

    public static CampaignObservationV7ProjectionResult Rejected(
        CampaignObservationRejectionReason reason)
    {
        if (reason == CampaignObservationRejectionReason.None || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CampaignObservationV7ProjectionResult(null, reason);
    }
}
