using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public sealed record ObservedOwnVehicleBreakdownRisk
{
    internal ObservedOwnVehicleBreakdownRisk(
        string cohortId,
        string vehicleTypeId,
        string profileId,
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
        ArgumentOutOfRangeException.ThrowIfLessThan(
            checked(workingPointCount + brokenPointCount),
            1);

        if (!Cna1979Breakdown.IsSupportedVehicleProfile(vehicleTypeId, profileId))
        {
            throw new ArgumentException(
                "The observed vehicle type and Breakdown profile are not supported.");
        }

        if (sandstormAttributedBreakdownPoints > cumulativeBreakdownPoints)
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
        VehicleTypeId = ContentContractGuards.RequireStableId(vehicleTypeId, nameof(vehicleTypeId));
        ProfileId = ContentContractGuards.RequireStableId(profileId, nameof(profileId));
        CumulativeBreakdownPoints = cumulativeBreakdownPoints;
        SandstormAttributedBreakdownPoints = sandstormAttributedBreakdownPoints;
        HighestEffectiveCheckedBandId = highestEffectiveCheckedBandId;
        WorkingPointCount = workingPointCount;
        BrokenPointCount = brokenPointCount;
    }

    public string CohortId { get; }

    public string VehicleTypeId { get; }

    public string ProfileId { get; }

    public BreakdownPointAmount CumulativeBreakdownPoints { get; }

    public BreakdownPointAmount SandstormAttributedBreakdownPoints { get; }

    public string? HighestEffectiveCheckedBandId { get; }

    public int WorkingPointCount { get; }

    public int BrokenPointCount { get; }
}
