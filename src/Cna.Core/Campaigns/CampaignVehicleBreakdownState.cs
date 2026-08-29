using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignVehicleBreakdownState
{
    public CampaignVehicleBreakdownState(
        string cohortId,
        BreakdownPointAmount cumulativeBreakdownPoints,
        BreakdownPointAmount sandstormAttributedBreakdownPoints,
        string? highestEffectiveCheckedBandId,
        int workingPointCount,
        int brokenPointCount)
    {
        ArgumentNullException.ThrowIfNull(cumulativeBreakdownPoints);
        ArgumentNullException.ThrowIfNull(sandstormAttributedBreakdownPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(workingPointCount);
        ArgumentOutOfRangeException.ThrowIfNegative(brokenPointCount);

        if (cumulativeBreakdownPoints.CompareTo(BreakdownPointAmount.Zero) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cumulativeBreakdownPoints));
        }

        if (sandstormAttributedBreakdownPoints.CompareTo(BreakdownPointAmount.Zero) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sandstormAttributedBreakdownPoints));
        }

        if (sandstormAttributedBreakdownPoints.CompareTo(cumulativeBreakdownPoints) > 0)
        {
            throw new ArgumentException(
                "Sandstorm-attributed Breakdown Points cannot exceed cumulative Breakdown Points.",
                nameof(sandstormAttributedBreakdownPoints));
        }

        if (highestEffectiveCheckedBandId is not null
            && !Cna1979Breakdown.IsCheckEligibleBandId(highestEffectiveCheckedBandId))
        {
            throw new ArgumentException(
                "The highest effective checked Breakdown band is not check-eligible.",
                nameof(highestEffectiveCheckedBandId));
        }

        CohortId = ContentContractGuards.RequireStableId(cohortId, nameof(cohortId));
        CumulativeBreakdownPoints = cumulativeBreakdownPoints;
        SandstormAttributedBreakdownPoints = sandstormAttributedBreakdownPoints;
        HighestEffectiveCheckedBandId = highestEffectiveCheckedBandId;
        WorkingPointCount = workingPointCount;
        BrokenPointCount = brokenPointCount;
    }

    public string CohortId { get; }

    public BreakdownPointAmount CumulativeBreakdownPoints { get; }

    public BreakdownPointAmount SandstormAttributedBreakdownPoints { get; }

    public string? HighestEffectiveCheckedBandId { get; }

    public int WorkingPointCount { get; }

    public int BrokenPointCount { get; }
}
