using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CombatReleaseScope(int GameTurn, int OperationStage, string PlayerPhaseSlot, LandSide ActingSide);
internal sealed record CombatReleaseMovementException(CombatReleaseScope Scope, int Ordinal, string Status, string? CompletionReceiptId);
internal sealed record CombatReleaseHistory(CombatReleaseScope Scope, string? DesignationReceiptId = null,
    string? ConversionReceiptId = null, string? ReleasedType = null, string? ReleaseReceiptId = null,
    int? ReleaseCycle = null, int? CpaBasis = null, int? VoluntaryCeiling = null,
    string? OffensiveCommitmentId = null, CombatReleaseMovementException? NextMovement = null);
internal sealed record CombatReleaseMember(CampaignCombatUnitKey Unit, CampaignElementReserveStatus Status,
    int BaseCpa, CapabilityPointAmount SpentCp, CombatReleaseHistory History);

/// <summary>Owned, isolated ledger probe. Does not establish campaign provenance or admit gameplay.</summary>
internal sealed record CombatReleaseBase
{
    public CombatReleaseBase(int contractVersion, string profile, CampaignCombatCycleAuthority cycle,
        LandSide firstActingSide, string positionId, long priorVersion, string priorPrefix,
        string combatCompletionReceiptId, string retainedWorldHash, RandomStreamState randomState,
        long? acceptedHighWater, IEnumerable<CombatReleaseMember> members, IEnumerable<CombatRoundAttackHistory> attackHistory)
    {
        ContractVersion = contractVersion; Profile = profile; Cycle = cycle; FirstActingSide = firstActingSide;
        PositionId = positionId; PriorVersion = priorVersion; PriorPrefix = priorPrefix;
        CombatCompletionReceiptId = combatCompletionReceiptId; RetainedWorldHash = retainedWorldHash;
        RandomState = randomState; AcceptedHighWater = acceptedHighWater;
        Members = Array.AsReadOnly(members.ToArray()); AttackHistory = Array.AsReadOnly(attackHistory.ToArray());
    }
    public int ContractVersion { get; init; }
    public string Profile { get; init; }
    public CampaignCombatCycleAuthority Cycle { get; init; }
    public LandSide FirstActingSide { get; init; }
    public string PositionId { get; init; }
    public long PriorVersion { get; init; }
    public string PriorPrefix { get; init; }
    public string CombatCompletionReceiptId { get; init; }
    public string RetainedWorldHash { get; init; }
    public RandomStreamState RandomState { get; init; }
    public long? AcceptedHighWater { get; init; }
    public IReadOnlyList<CombatReleaseMember> Members { get; }
    public IReadOnlyList<CombatRoundAttackHistory> AttackHistory { get; }
}
