using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Independently trusted C3a facts. Construction does not authenticate history, Weather,
/// completed Breakdown or seat ownership. No current actual history supplies this positive path.
/// Idle Breakdown and null Reaction are the only representable arms in this bounded profile.
/// </summary>
internal sealed record CombatStepsBoundary(int ContractVersion, string CreationBinding,
    CampaignCombatCycleAuthority Cycle, LandSide FirstActingSide, long PriorVersion, string PriorPrefix,
    string CompletedBreakdownReceipt, LandSequencePosition Position, CampaignWorldSnapshotV7 World,
    RandomStreamState RandomState, CombatStepsWeather Weather);

internal sealed record CombatStepsWeather(int GameTurn, int OperationStage, WeatherKind AttackerKind,
    WeatherKind DefenderKind, string WeatherReceiptHash);

internal sealed record CombatStepsTiming(int ContractVersion, string ConfigHash, string Kind,
    int DecisionBudgetMilliseconds, long OpenedAtUnixMilliseconds, long DeadlineUnixMilliseconds,
    long HighWaterUnixMilliseconds);

internal sealed record CombatStepsWindow(string DecisionId, LandSide Owner, CombatStepsTiming Timing);

internal sealed record CombatStepsCommand(int ContractVersion, string Kind, string SegmentId,
    string? DecisionId = null, string? FromPositionId = null, long? ExpectedPriorVersion = null,
    string? Choice = null, CampaignCombatCandidate? Candidate = null, CampaignCombatUnitKey? Participant = null);

/// <summary>Actor/time originate in trusted admission, never authenticated by an imported event.</summary>
internal sealed record CombatStepsInput(CombatStepsCommand Command, CampaignOpeningPreambleActor Actor,
    long? AdmittedAt = null, bool ClockAvailable = true);

internal abstract record CombatStepsEffect(string Kind)
{
    internal sealed record Open(string BoundaryHash, CombatStepsWindow? Window) : CombatStepsEffect("segment-opened");
    internal sealed record Selection(string Outcome, CampaignCombatCandidate? Candidate, CombatStepsTiming? Timing) : CombatStepsEffect("selection-closed");
    internal sealed record RbaOpen(string SelectionReceiptId, CombatStepsWindow Window) : CombatStepsEffect("rba-opened");
    internal sealed record Decline(string SelectionReceiptId, CampaignCombatUnitKey Participant, CombatStepsTiming Timing) : CombatStepsEffect("rba-declined");
    internal sealed record Cancel(string SelectionReceiptId, CombatStepsTiming? Timing) : CombatStepsEffect("selection-cancelled");
    internal sealed record Step(string FromPositionId, string ToPositionId, string PreviousStepReceiptId,
        string DispositionReceiptId, string ProofKind) : CombatStepsEffect("step-completed");
}

internal sealed record CombatStepsControl(string BoundaryHash, string SegmentId, long StateVersion, string Prefix)
{
    private IReadOnlyList<string> stepReceipts = Array.Empty<string>();
    private IReadOnlyList<CampaignOpeningPreambleReceipt> receipts = Array.Empty<CampaignOpeningPreambleReceipt>();
    public int StepIndex { get; init; }
    public string SelectionOutcome { get; init; } = "unopened";
    public CampaignCombatCandidate? Selection { get; init; }
    public string? SelectionReceiptId { get; init; }
    public string? DeclineReceiptId { get; init; }
    public string? CancellationReceiptId { get; init; }
    public CombatStepsWindow? SelectionWindow { get; init; }
    public CombatStepsWindow? RbaWindow { get; init; }
    public IReadOnlyList<string> StepReceipts { get => stepReceipts; init => stepReceipts = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get => receipts; init => receipts = Array.AsReadOnly(value.ToArray()); }
    public bool Closed { get; init; }
}

internal enum CombatStepsDisposition { Accepted, Duplicate, NoOp }

internal sealed class CombatStepsResult(CombatStepsControl control, CombatStepsDisposition disposition,
    byte[]? eventBytes, string? receiptId)
{
    private readonly byte[]? bytes = eventBytes?.ToArray();
    public CombatStepsControl Control { get; } = control;
    public CombatStepsDisposition Disposition { get; } = disposition;
    public byte[]? EventBytes => bytes?.ToArray();
    public string? ReceiptId { get; } = receiptId;
}
