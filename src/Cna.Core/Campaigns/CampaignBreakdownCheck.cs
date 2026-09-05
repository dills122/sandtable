using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignBreakdownCheckStatus
{
    NoWorkingPoints, RawBpNotAboveThree, BelowCheckSurface, BandNotHigher, Rolled,
}

internal sealed record CampaignBreakdownRoll(int FirstDie, int SecondDie, int Coordinate,
    ulong RandomCursorBefore, ulong RandomCursorAfter, int PrintedLabel,
    BreakdownPointAmount Fraction, string RulingId, int LossCount);

internal sealed record CampaignBreakdownCheck
{
    public CampaignBreakdownCheck(string checkId, CampaignBreakdownCheckInput input, string rawBandId,
        string? effectiveBandId, CampaignBreakdownCheckStatus status, CampaignBreakdownRoll? roll,
        int workingAfter, int brokenBefore, int brokenAfter, string? highestEffectiveCheckedBandIdAfter,
        IEnumerable<RuleReference> sources)
    {
        CheckId = ContentContractGuards.RequireSha256(checkId, nameof(checkId));
        ArgumentNullException.ThrowIfNull(input);
        if (!Cna1979Breakdown.IsSupportedBandId(rawBandId)
            || (effectiveBandId is not null && !Cna1979Breakdown.IsCheckEligibleBandId(effectiveBandId))
            || (highestEffectiveCheckedBandIdAfter is not null && !Cna1979Breakdown.IsCheckEligibleBandId(highestEffectiveCheckedBandIdAfter))
            || !Enum.IsDefined(status)) throw new ArgumentException("Invalid Breakdown check bands/status.");
        ArgumentOutOfRangeException.ThrowIfNegative(workingAfter);
        ArgumentOutOfRangeException.ThrowIfNegative(brokenBefore);
        ArgumentOutOfRangeException.ThrowIfNegative(brokenAfter);
        if (checked((long)workingAfter + brokenAfter) != checked((long)input.WorkingPointCount + brokenBefore))
            throw new ArgumentException("Breakdown check must conserve vehicle points.");
        if (status == CampaignBreakdownCheckStatus.Rolled)
        {
            ArgumentNullException.ThrowIfNull(roll);
            var loss = Cna1979BreakdownAdjudication.CalculateLoss(input.WorkingPointCount, roll.PrintedLabel);
            if (effectiveBandId is null || roll.Coordinate != Cna1979Breakdown.CreateSequentialDiceCoordinate(roll.FirstDie, roll.SecondDie)
                || roll.RandomCursorAfter <= roll.RandomCursorBefore || roll.RandomCursorAfter - roll.RandomCursorBefore < 2
                || Cna1979BreakdownAdjudication.LookupOutcome(effectiveBandId, roll.Coordinate).PrintedLabel != roll.PrintedLabel
                || roll.Fraction != loss.Outcome.Fraction || roll.RulingId != loss.Outcome.RulingId
                || roll.LossCount != loss.LossCount || workingAfter != input.WorkingPointCount - loss.LossCount
                || highestEffectiveCheckedBandIdAfter != effectiveBandId)
                throw new ArgumentException("Invalid Breakdown roll evidence.");
        }
        else if (roll is not null || workingAfter != input.WorkingPointCount || brokenBefore != brokenAfter
            || highestEffectiveCheckedBandIdAfter != input.HighestEffectiveCheckedBandId)
            throw new ArgumentException("No-roll checks must retain counts and memory.");
        Input = input; RawBandId = rawBandId; EffectiveBandId = effectiveBandId; Status = status; Roll = roll;
        WorkingAfter = workingAfter; BrokenBefore = brokenBefore; BrokenAfter = brokenAfter;
        HighestEffectiveCheckedBandIdAfter = highestEffectiveCheckedBandIdAfter;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }
    public string CheckId { get; }
    public CampaignBreakdownCheckInput Input { get; }
    public string RawBandId { get; }
    public string? EffectiveBandId { get; }
    public CampaignBreakdownCheckStatus Status { get; }
    public CampaignBreakdownRoll? Roll { get; }
    public int WorkingAfter { get; }
    public int BrokenBefore { get; }
    public int BrokenAfter { get; }
    public string? HighestEffectiveCheckedBandIdAfter { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
    public bool Equals(CampaignBreakdownCheck? other) => ReferenceEquals(this, other) || (other is not null
        && CheckId == other.CheckId && Input == other.Input && RawBandId == other.RawBandId
        && EffectiveBandId == other.EffectiveBandId && Status == other.Status && Roll == other.Roll
        && WorkingAfter == other.WorkingAfter && BrokenBefore == other.BrokenBefore && BrokenAfter == other.BrokenAfter
        && HighestEffectiveCheckedBandIdAfter == other.HighestEffectiveCheckedBandIdAfter && Sources.SequenceEqual(other.Sources));
    public override int GetHashCode() => HashCode.Combine(CheckId, Status, WorkingAfter, BrokenAfter);
}

internal sealed record CampaignBreakdownCheckBatch(IReadOnlyList<CampaignBreakdownCheck> Checks,
    IReadOnlyList<CampaignBrokenVehicleLot> CreatedLots, RandomStreamState RandomStateAfter);
