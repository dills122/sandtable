using System.Text.Json;

namespace Cna.Core.Campaigns;

internal sealed record CombatRoundClockConfiguration(int ContractVersion, string ParentConfigurationHash,
    string TimingPolicyId, int DecisionBudgetMilliseconds);
internal sealed record CombatRoundTiming(int ContractVersion, string ClockConfigurationHash, string Kind,
    int DecisionBudgetMilliseconds, long OpenedAtUnixMilliseconds, long DeadlineUnixMilliseconds,
    long OpeningFloorUnixMilliseconds);
internal sealed record CombatRoundAllocation(string Kind, CampaignCombatUnitKey Unit, string ComponentId, int CommittedToe);
internal sealed record CombatRoundSlot(string Role, string Owner, string SlotId, CombatRoundAllocation Allocation,
    string? SealedReceiptId = null, long? SealedAt = null);
internal sealed record CombatRoundCommand(int ContractVersion, string Kind, string ClockConfigurationHash,
    string SegmentId, string? RoundId, string? SlotId = null, long? ExpectedPriorVersion = null,
    CombatRoundAllocation? Allocation = null);
/// <summary>Actor, time and confidence are independently trusted; event.input cannot authenticate them.</summary>
internal sealed record CombatRoundInput(CombatRoundCommand Command, CampaignOpeningPreambleActor Actor,
    long? AdmittedAt = null, bool ClockAvailable = true);

/// <summary>Reconstructed synthetic C3a authority, not evidence of positive campaign history.</summary>
internal sealed class CombatRoundBase
{
    private readonly byte[] boundaryBytes;
    private readonly byte[] worldBytes;
    internal CombatRoundBase(CombatStepsBoundary boundary, byte[] bytes, CombatStepsControl steps,
        CombatRoundClockConfiguration configuration)
    {
        Boundary = boundary;
        boundaryBytes = bytes.ToArray();
        using var document = JsonDocument.Parse(boundaryBytes);
        worldBytes = System.Text.Encoding.UTF8.GetBytes(document.RootElement.GetProperty("world").GetRawText());
        Steps = steps;
        Configuration = configuration;
        ConfigurationHash = CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.clock-configuration.v2",
            CampaignCombatSealedRoundCodec.SerializeConfiguration(configuration));
        BaseHash = CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.base-fragment.v2",
            CampaignCombatSealedRoundCodec.SerializeBase(this));
    }
    public CombatStepsBoundary Boundary { get; }
    public CombatStepsControl Steps { get; }
    public CombatRoundClockConfiguration Configuration { get; }
    public string ConfigurationHash { get; }
    public string BaseHash { get; }
    internal ReadOnlySpan<byte> BoundaryBytes => boundaryBytes;
    internal ReadOnlySpan<byte> WorldBytes => worldBytes;
}

internal sealed record CombatRoundState
{
    internal CombatRoundState(CombatRoundBase basis)
    {
        Base = basis;
        StateVersion = basis.Steps.StateVersion;
        Prefix = basis.Steps.Prefix;
        StepReceipts = basis.Steps.StepReceipts;
    }
    private IReadOnlyList<CombatRoundSlot> slots = Array.Empty<CombatRoundSlot>();
    private IReadOnlyList<string> steps = Array.Empty<string>();
    private IReadOnlyList<CampaignOpeningPreambleReceipt> receipts = Array.Empty<CampaignOpeningPreambleReceipt>();
    public CombatRoundBase Base { get; }
    public string? OpportunityId { get; init; }
    public string? RoundId { get; init; }
    public long StateVersion { get; init; }
    public string Prefix { get; init; }
    public int StepIndex { get; init; } = 3;
    public string Status { get; init; } = "unopened";
    public string? OpeningReceiptId { get; init; }
    public CombatRoundTiming? Timing { get; init; }
    public IReadOnlyList<CombatRoundSlot> Slots { get => slots; init => slots = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<string> StepReceipts { get => steps; init => steps = Array.AsReadOnly(value.ToArray()); }
    public string? CancellationReceiptId { get; init; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get => receipts; init => receipts = Array.AsReadOnly(value.ToArray()); }
    public bool Closed { get; init; }
}

internal abstract record CombatRoundEffect(string Kind)
{
    internal sealed record Open(string BaseHash, string OpportunityId, CombatRoundTiming Timing,
        IReadOnlyList<CombatRoundSlot> Slots) : CombatRoundEffect("round-opened");
    internal sealed record Seal(string SlotId, CombatRoundAllocation Allocation, CombatRoundTiming Timing,
        bool Prepared) : CombatRoundEffect("choice-sealed");
    internal sealed record Cancel(string Cause, CombatRoundTiming Timing) : CombatRoundEffect("round-cancelled");
    internal sealed record Step(string FromPositionId, string ToPositionId, string PreviousStepReceiptId,
        IReadOnlyList<string> ProofReceipts) : CombatRoundEffect("step-completed");
}

internal sealed class CombatRoundResult(CombatRoundState state, CombatStepsDisposition disposition, byte[]? eventBytes, string? receiptId)
{
    private readonly byte[]? bytes = eventBytes?.ToArray();
    public CombatRoundState State { get; } = state;
    public CombatStepsDisposition Disposition { get; } = disposition;
    public byte[]? EventBytes => bytes?.ToArray();
    public string? ReceiptId { get; } = receiptId;
}
