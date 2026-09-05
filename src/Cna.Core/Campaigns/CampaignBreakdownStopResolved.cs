using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record BreakdownStopResolved : CampaignSuccessorEvent
{
    public const int CurrentContractVersion = 1;
    public BreakdownStopResolved(string campaignId, long stateVersion, long priorStateVersion,
        string rulesetHash, string fromPositionId, string actionId, CampaignBreakdownStop stop,
        RandomStreamState randomStateBefore, IEnumerable<CampaignBreakdownCheck> checks,
        IEnumerable<CampaignBrokenVehicleLot> createdLots, RandomStreamState randomStateAfter,
        CampaignBreakdownFlow breakdownFlowAfter, IEnumerable<RuleReference> sources)
        : base(CurrentContractVersion, campaignId, stateVersion)
    {
        if (priorStateVersion < 1 || checked(priorStateVersion + 1) != stateVersion
            || !Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash)) throw new ArgumentException("Invalid resolution identity.");
        ArgumentNullException.ThrowIfNull(stop); ArgumentNullException.ThrowIfNull(randomStateBefore);
        ArgumentNullException.ThrowIfNull(randomStateAfter); ArgumentNullException.ThrowIfNull(breakdownFlowAfter);
        PriorStateVersion = priorStateVersion; RulesetHash = rulesetHash;
        FromPositionId = ContentContractGuards.RequireStableId(fromPositionId, nameof(fromPositionId));
        ActionId = ContentContractGuards.RequireSha256(actionId, nameof(actionId));
        Stop = stop; RandomStateBefore = randomStateBefore; RandomStateAfter = randomStateAfter;
        var checkCopy = ContentContractGuards.CopyValues(checks, nameof(checks));
        var lots = ContentContractGuards.CopyValues(createdLots, nameof(createdLots));
        if (checkCopy.Select(x => x.Input.CohortId).Distinct(StringComparer.Ordinal).Count() != checkCopy.Length
            || lots.Select(x => x.LotId).Distinct(StringComparer.Ordinal).Count() != lots.Length)
            throw new ArgumentException("Duplicate check or lot.");
        Checks = Array.AsReadOnly(checkCopy.OrderBy(x => x.Input.VehicleTypeId, StringComparer.Ordinal)
            .ThenBy(x => x.Input.ProfileId, StringComparer.Ordinal).ThenBy(x => x.Input.CohortId, StringComparer.Ordinal).ToArray());
        CreatedLots = Array.AsReadOnly(lots.OrderBy(x => x.LotId, StringComparer.Ordinal).ToArray());
        BreakdownFlowAfter = breakdownFlowAfter;
        Sources = CampaignBreakdownStep.CopySources(sources);
        if (FromPositionId != Cna1979LandSequenceV4.BreakdownStopPositionId || stop.RecordedStateVersion > priorStateVersion
            || randomStateBefore.ContractVersion != SandtableRandom.ContractVersion || randomStateAfter.ContractVersion != SandtableRandom.ContractVersion
            || randomStateBefore.AlgorithmId != SandtableRandom.AlgorithmId || randomStateAfter.AlgorithmId != SandtableRandom.AlgorithmId
            || randomStateBefore.Seed != randomStateAfter.Seed || randomStateAfter.NextByteCursor < randomStateBefore.NextByteCursor
            || breakdownFlowAfter is not (CampaignBreakdownFlow.Idle or CampaignBreakdownFlow.Moving or CampaignBreakdownFlow.Reacting or CampaignBreakdownFlow.PhasingStop))
            throw new ArgumentException("Invalid resolution shape.");
        stop.ValidateIdentity(campaignId, rulesetHash);
    }
    public long PriorStateVersion { get; }
    public string RulesetHash { get; }
    public string FromPositionId { get; }
    public string ActionId { get; }
    public CampaignBreakdownStop Stop { get; }
    public RandomStreamState RandomStateBefore { get; }
    public IReadOnlyList<CampaignBreakdownCheck> Checks { get; }
    public IReadOnlyList<CampaignBrokenVehicleLot> CreatedLots { get; }
    public RandomStreamState RandomStateAfter { get; }
    public CampaignBreakdownFlow BreakdownFlowAfter { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
    public bool Equals(BreakdownStopResolved? other) => ReferenceEquals(this, other) || (other is not null
        && CampaignId == other.CampaignId && StateVersion == other.StateVersion && PriorStateVersion == other.PriorStateVersion
        && RulesetHash == other.RulesetHash && FromPositionId == other.FromPositionId && ActionId == other.ActionId
        && Stop == other.Stop && RandomStateBefore == other.RandomStateBefore && RandomStateAfter == other.RandomStateAfter
        && Checks.SequenceEqual(other.Checks) && CreatedLots.SequenceEqual(other.CreatedLots)
        && BreakdownFlowAfter == other.BreakdownFlowAfter && Sources.SequenceEqual(other.Sources));
    public override int GetHashCode() => HashCode.Combine(CampaignId, StateVersion, Stop, RandomStateAfter);
}
