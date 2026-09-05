using Cna.Core.Randomness;
using Cna.Core.Rules;
namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownCheckResolver
{
    public static CampaignBreakdownCheckBatch Resolve(string campaignId, string rulesetHash,
        CampaignBreakdownStop stop, IReadOnlyDictionary<string, int> brokenBeforeByCohort,
        RandomStreamState randomState, long createdStateVersion)
    {
        ArgumentNullException.ThrowIfNull(stop); ArgumentNullException.ThrowIfNull(brokenBeforeByCohort);
        ArgumentNullException.ThrowIfNull(randomState);
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash)) throw new ArgumentException("Unsupported Breakdown ruleset.");
        stop.ValidateIdentity(campaignId, rulesetHash);
        if (randomState.ContractVersion != SandtableRandom.ContractVersion || randomState.AlgorithmId != SandtableRandom.AlgorithmId)
            throw new ArgumentException("Unsupported Breakdown random stream.", nameof(randomState));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdStateVersion, stop.RecordedStateVersion);
        if (brokenBeforeByCohort.Count != stop.CohortInputs.Count
            || stop.CohortInputs.Any(input => !brokenBeforeByCohort.TryGetValue(input.CohortId, out var count)
                || count < 0 || (long)count + input.WorkingPointCount > int.MaxValue
                || !Cna1979Breakdown.IsSupportedVehicleProfile(input.VehicleTypeId, input.ProfileId)))
            throw new ArgumentException("Invalid Breakdown batch cohort binding.", nameof(brokenBeforeByCohort));

        // Evaluate the complete immutable batch before drawing. No partial result escapes.
        var prepared = stop.CohortInputs.OrderBy(input => input.VehicleTypeId, StringComparer.Ordinal)
            .ThenBy(input => input.ProfileId, StringComparer.Ordinal).ThenBy(input => input.CohortId, StringComparer.Ordinal)
            .Select(input => (Input: input, Eligibility: Cna1979BreakdownAdjudication.EvaluateEligibility(
                input.WorkingPointCount, input.CumulativeBreakdownPoints, input.ProfileId, stop.WeatherKind,
                input.SandstormAttributedBreakdownPoints, input.HighestEffectiveCheckedBandId))).ToArray();
        var checks = new List<CampaignBreakdownCheck>(); var lots = new List<CampaignBrokenVehicleLot>();
        var current = randomState;
        foreach (var (input, eligibility) in prepared)
        {
            var status = eligibility.Status switch
            {
                BreakdownCheckEligibilityStatus.NoWorkingPoints => CampaignBreakdownCheckStatus.NoWorkingPoints,
                BreakdownCheckEligibilityStatus.RawBpNotAboveThree => CampaignBreakdownCheckStatus.RawBpNotAboveThree,
                BreakdownCheckEligibilityStatus.BelowCheckSurface => CampaignBreakdownCheckStatus.BelowCheckSurface,
                BreakdownCheckEligibilityStatus.BandNotHigher => CampaignBreakdownCheckStatus.BandNotHigher,
                BreakdownCheckEligibilityStatus.Eligible => CampaignBreakdownCheckStatus.Rolled,
                _ => throw new ArgumentException("Unsupported Breakdown eligibility."),
            };
            var sources = Sources(input, eligibility, stop.WeatherKind).ToList();
            CampaignBreakdownRoll? roll = null;
            var lossCount = 0; var memory = input.HighestEffectiveCheckedBandId;
            if (status == CampaignBreakdownCheckStatus.Rolled)
            {
                var before = current.NextByteCursor;
                var first = SandtableRandom.RollD6(current); var second = SandtableRandom.RollD6(first.State);
                current = second.State;
                var coordinate = Cna1979Breakdown.CreateSequentialDiceCoordinate(first.Value, second.Value);
                var outcome = Cna1979BreakdownAdjudication.LookupOutcome(eligibility.EffectiveBand!.BandId, coordinate);
                var loss = Cna1979BreakdownAdjudication.CalculateLoss(input.WorkingPointCount, outcome.PrintedLabel);
                lossCount = loss.LossCount; memory = eligibility.EffectiveBand.BandId;
                roll = new(first.Value, second.Value, coordinate, before, current.NextByteCursor, outcome.PrintedLabel,
                    loss.Outcome.Fraction, loss.Outcome.RulingId, lossCount);
                sources.AddRange(outcome.Sources); sources.AddRange(loss.Outcome.Sources);
                sources.AddRange(Cna1979Breakdown.Definition.DiceCoordinate.Sources);
                if (lossCount > 0) lots.Add(CampaignBrokenVehicleLot.Create(campaignId, rulesetHash, stop.StopId,
                    stop.Route.Owner, input.CohortId, input.VehicleTypeId, lossCount, stop.Route.CurrentLocationId,
                    createdStateVersion, [new RuleReference("spi-1979-land-rules", "21.41")]));
            }
            var brokenBefore = brokenBeforeByCohort[input.CohortId];
            checks.Add(new(CampaignBreakdownCodec.CheckId(campaignId, rulesetHash, stop.StopId, input.CohortId), input,
                eligibility.RawBand.BandId, eligibility.EffectiveBand?.BandId, status, roll,
                input.WorkingPointCount - lossCount, brokenBefore, checked(brokenBefore + lossCount), memory, sources.Distinct()));
        }
        return new(Array.AsReadOnly(checks.ToArray()), Array.AsReadOnly(lots.OrderBy(lot => lot.LotId, StringComparer.Ordinal).ToArray()), current);
    }

    private static IEnumerable<RuleReference> Sources(CampaignBreakdownCheckInput input,
        BreakdownCheckEligibility eligibility, BreakdownWeatherKind weather)
    {
        var definition = Cna1979Breakdown.Definition;
        return definition.Bands.Where(band => band.BandId == eligibility.RawBand.BandId
                || band.BandId == eligibility.EffectiveBand?.BandId).SelectMany(band => band.Sources)
            .Concat([new RuleReference("spi-1979-land-rules", "21.24-21.26"), new RuleReference("spi-1979-land-rules", "21.27-21.29")])
            .Concat(Cna1979Breakdown.LookupVehicleType(input.VehicleTypeId).Sources)
            .Concat(Cna1979Breakdown.LookupProfile(input.ProfileId).Sources)
            .Concat(definition.WeatherShifts.Where(value => value.WeatherKind == weather).SelectMany(value => value.Sources))
            .Concat(definition.WeatherInputTransformations.Where(value => value.WeatherKind == weather).SelectMany(value => value.Sources));
    }
}
