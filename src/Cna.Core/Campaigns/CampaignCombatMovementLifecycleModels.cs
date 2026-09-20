using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatMovementLifecycleIdentity(int ContractVersion, string ActionId, string CreationBinding,
    string CreationEventHash, string CycleId, long ExpectedPriorVersion, string ExpectedPositionId);
internal abstract record CampaignCombatMovementLifecycleCommand(CampaignCombatMovementLifecycleIdentity Identity)
{
    internal sealed record Stop(CampaignCombatMovementLifecycleIdentity Identity, string RouteId) : CampaignCombatMovementLifecycleCommand(Identity);
    internal sealed record Resolve(CampaignCombatMovementLifecycleIdentity Identity, string StopId) : CampaignCombatMovementLifecycleCommand(Identity);
    internal sealed record Complete(CampaignCombatMovementLifecycleIdentity Identity) : CampaignCombatMovementLifecycleCommand(Identity);
    public string Kind => this switch
    {
        Stop => "stop-element-movement",
        Resolve => "resolve-breakdown-stop",
        Complete => "complete-movement-segment",
        _ => throw new System.Text.Json.JsonException("Unsupported movement lifecycle command."),
    };
}
internal sealed record CampaignCombatMovementLifecycleInput(CampaignCombatMovementLifecycleCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatMovementInterrupt(CampaignCombatCycleAuthority Cycle, string CycleId, LandSequencePosition SequencePosition);
internal sealed record CampaignCombatEndLocation(CampaignCombatUnitKey Unit, string LocationId);
internal sealed class CampaignCombatMovementEndProof
{
    internal CampaignCombatMovementEndProof(CampaignCombatReserveScope scope, string receipt,
        IEnumerable<CampaignCombatEndLocation> locations, IEnumerable<CampaignCombatUnitKey> excluded)
    { Scope = scope; CompletionReceiptId = receipt; EndLocations = Array.AsReadOnly(locations.ToArray()); ExcludedBefore = Array.AsReadOnly(excluded.ToArray()); }
    public CampaignCombatReserveScope Scope { get; }
    public int Ordinal { get; } = 1;
    public string CompletionReceiptId { get; }
    public IReadOnlyList<CampaignCombatEndLocation> EndLocations { get; }
    public IReadOnlyList<CampaignCombatUnitKey> ExcludedBefore { get; }
}
internal sealed class CampaignCombatMovementLifecycleEvent
{
    internal CampaignCombatMovementLifecycleEvent(CampaignCombatMovementLifecycleState before, CampaignCombatMovementLifecycleInput input,
        LandSequencePosition position, CampaignBreakdownFlow flow, CampaignCombatMovementInterrupt? interrupt,
        IEnumerable<CampaignCombatEndLocation> locations, IEnumerable<CampaignCombatUnitKey> excluded)
    {
        Before = before; Input = input; SequencePosition = position; BreakdownFlow = flow; InterruptContext = interrupt;
        EndLocations = Array.AsReadOnly(locations.ToArray()); ExcludedUnits = Array.AsReadOnly(excluded.ToArray());
    }
    public CampaignCombatMovementLifecycleState Before { get; }
    public CampaignCombatMovementLifecycleInput Input { get; }
    public long StateVersion => checked(Before.StateVersion + 1);
    public LandSequencePosition SequencePosition { get; }
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public CampaignCombatMovementInterrupt? InterruptContext { get; }
    public IReadOnlyList<CampaignCombatEndLocation> EndLocations { get; }
    public IReadOnlyList<CampaignCombatUnitKey> ExcludedUnits { get; }
    public string ReceiptId => "iml." + CampaignOpeningPreambleCodec.HashWithDomain(
        CampaignCombatMovementLifecycleCodec.ReceiptDomain(Input.Command), CampaignCombatMovementLifecycleCodec.SerializeEvent(this, false))[7..];
}
internal sealed class CampaignCombatMovementLifecycleState
{
    internal CampaignCombatMovementLifecycleState(CampaignCombatInheritedMovementState movement, long version, string prefix,
        LandSequencePosition position, CampaignBreakdownFlow flow, CampaignCombatMovementInterrupt? interrupt,
        CampaignCombatMovementEndProof? end, IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<CampaignCombatMovementLifecycleEvent> events)
    {
        Movement = movement; StateVersion = version; Prefix = prefix; SequencePosition = position; BreakdownFlow = flow;
        InterruptContext = interrupt; MovementEnd = end; Receipts = Array.AsReadOnly(receipts.ToArray()); Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatInheritedMovementState Movement { get; }
    public long StateVersion { get; }
    public string Prefix { get; }
    public LandSequencePosition SequencePosition { get; }
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public CampaignCombatMovementInterrupt? InterruptContext { get; }
    public CampaignCombatMovementEndProof? MovementEnd { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    internal IReadOnlyList<CampaignCombatMovementLifecycleEvent> Events { get; }
}
internal sealed record CampaignCombatMovementLifecycleResult(CampaignCombatMovementLifecycleState State, byte[] EventBytes, bool Duplicate);
