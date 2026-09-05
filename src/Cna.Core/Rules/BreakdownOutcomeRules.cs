namespace Cna.Core.Rules;

internal enum BreakdownCheckEligibilityStatus
{
    Eligible,
    NoWorkingPoints,
    RawBpNotAboveThree,
    BelowCheckSurface,
    BandNotHigher,
}

internal sealed record BreakdownCheckEligibility(
    BreakdownBandRule RawBand,
    BreakdownBandRule? EffectiveBand,
    BreakdownCheckEligibilityStatus Status);

internal sealed record BreakdownOutcomeFraction(
    int PrintedLabel,
    BreakdownPointAmount Fraction,
    string RulingId,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownOutcomeCell(
    string BandId,
    int Coordinate,
    int PrintedLabel,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownLossResult(
    int WorkingPointCount,
    int LossCount,
    BreakdownOutcomeFraction Outcome);

internal sealed record BreakdownRulesV2ArtifactDefinition(
    int SchemaVersion,
    BreakdownRulesArtifactDefinition Continuity,
    IReadOnlyList<BreakdownOutcomeFraction> OutcomeFractions,
    IReadOnlyList<BreakdownOutcomeCell> OutcomeCells,
    IReadOnlyList<RuleReference> Sources);
