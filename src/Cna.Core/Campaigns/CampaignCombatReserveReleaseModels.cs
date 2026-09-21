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

internal sealed record CombatReleaseCommand(int ContractVersion, string Kind, string ReleaseId,
    long? ExpectedPriorVersion, string? DecisionId = null, CampaignCombatUnitKey? Unit = null, string? Choice = null);

/// <summary>Actor and time are separately admitted inputs, never trusted from persisted events.</summary>
internal sealed record CombatReleaseInput(CombatReleaseCommand Command, CampaignOpeningPreambleActor Actor,
    long? AdmittedAt = null, bool ClockAvailable = true);
internal sealed record CombatReleaseDisposition(string ReceiptId, CampaignCombatUnitKey Unit, string Choice,
    CampaignElementReserveStatus BeforeStatus, CampaignElementReserveStatus AfterStatus);

internal abstract record CombatReleaseEffect(string Kind)
{
    internal sealed record Open(string? DecisionId, CombatStepsTiming? Timing, bool OpeningClockFailure,
        IReadOnlyList<CampaignCombatUnitKey> Pending) : CombatReleaseEffect("release-opened");
    internal sealed record UnitDisposition(CampaignCombatUnitKey Unit, string Choice, CampaignElementReserveStatus BeforeStatus,
        CampaignElementReserveStatus AfterStatus, string Reason, int CpaBasis, int? VoluntaryCeiling,
        CombatReleaseMovementException? NextMovement, CombatStepsTiming? Timing, bool FallbackLocked) : CombatReleaseEffect("unit-disposition");
    internal sealed record Complete(string Reason, IReadOnlyList<CampaignCombatUnitKey> RetainedUnits,
        IReadOnlyList<string> DispositionReceipts, CombatStepsTiming? Timing, bool FallbackLocked) : CombatReleaseEffect("release-completed");
}

/// <summary>Immutable projection. Recovered only by replay from independently retained base and inputs.</summary>
internal sealed record CombatReleaseState(string BaseHash, string ReleaseId, long StateVersion, string Prefix,
    string RetainedWorldHash, RandomStreamState RandomState)
{
    private IReadOnlyList<CombatReleaseMember> members = Array.Empty<CombatReleaseMember>();
    private IReadOnlyList<CampaignCombatUnitKey> pending = Array.Empty<CampaignCombatUnitKey>();
    private IReadOnlyList<CombatReleaseDisposition> dispositions = Array.Empty<CombatReleaseDisposition>();
    private IReadOnlyList<CombatRoundAttackHistory> attackHistory = Array.Empty<CombatRoundAttackHistory>();
    private IReadOnlyList<CampaignOpeningPreambleReceipt> receipts = Array.Empty<CampaignOpeningPreambleReceipt>();
    public string Status { get; init; } = "unopened";
    public string? OpeningReceiptId { get; init; }
    public string? DecisionId { get; init; }
    public CombatStepsTiming? Timing { get; init; }
    public bool OpeningClockFailure { get; init; }
    public long? AcceptedHighWater { get; init; }
    public bool FallbackLocked { get; init; }
    public string? FallbackReason { get; init; }
    public IReadOnlyList<CombatReleaseMember> Members { get => members; init => members = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<CampaignCombatUnitKey> Pending { get => pending; init => pending = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<CombatReleaseDisposition> Dispositions { get => dispositions; init => dispositions = Array.AsReadOnly(value.ToArray()); }
    public string? CompletionReceiptId { get; init; }
    public IReadOnlyList<CombatRoundAttackHistory> AttackHistory { get => attackHistory; init => attackHistory = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get => receipts; init => receipts = Array.AsReadOnly(value.ToArray()); }
}

internal sealed class CombatReleaseResult(CombatReleaseState state, CombatStepsDisposition disposition, byte[]? eventBytes, string? receiptId)
{
    private readonly byte[]? bytes = eventBytes?.ToArray();
    public CombatReleaseState State { get; } = state;
    public CombatStepsDisposition Disposition { get; } = disposition;
    public byte[]? EventBytes => bytes?.ToArray();
    public string? ReceiptId { get; } = receiptId;
}
