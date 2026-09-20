using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReactionLifecycleIdentity(int ContractVersion, string ActionId, string CreationBinding,
    string CreationEventHash, string CycleId, long ExpectedPriorVersion, string ExpectedPositionId);
internal abstract record CampaignCombatReactionLifecycleCommand(CampaignCombatReactionLifecycleIdentity Identity)
{
    internal sealed record Move(CampaignCombatReactionLifecycleIdentity Identity, string WindowId, string OpportunityId,
        string OriginLocationId, string DestinationLocationId) : CampaignCombatReactionLifecycleCommand(Identity);
    internal sealed record Complete(CampaignCombatReactionLifecycleIdentity Identity, string WindowId, string OpportunityId) : CampaignCombatReactionLifecycleCommand(Identity);
    internal sealed record Resolve(CampaignCombatReactionLifecycleIdentity Identity, string StopId) : CampaignCombatReactionLifecycleCommand(Identity);
    internal sealed record Close(CampaignCombatReactionLifecycleIdentity Identity, string WindowId) : CampaignCombatReactionLifecycleCommand(Identity);
    public string Kind => this switch
    {
        Move => "move-reacting-element",
        Complete => "complete-reaction-participant",
        Resolve => "resolve-breakdown-stop",
        Close => "close-reaction-window-no-eligible-reactor",
        _ => throw new System.Text.Json.JsonException("Unknown Reaction lifecycle command."),
    };
}
internal sealed record CampaignCombatReactionLifecycleInput(CampaignCombatReactionLifecycleCommand Command, CampaignOpeningPreambleActor Actor);

/// <summary>Lifecycle overlay retains the immutable, actual F1 trigger authority.</summary>
internal sealed class CampaignCombatReactionLifecycleWindow
{
    internal CampaignCombatReactionLifecycleWindow(CampaignCombatReactionWindow trigger,
        IEnumerable<CampaignReactionOpportunityId> resolved, CampaignReactionOpportunityId? active)
    { Trigger = trigger; ResolvedOpportunityIds = Array.AsReadOnly(resolved.OrderBy(x => x.Value, StringComparer.Ordinal).ToArray()); ActiveOpportunityId = active; }
    public CampaignCombatReactionWindow Trigger { get; }
    public IReadOnlyList<CampaignReactionOpportunityId> ResolvedOpportunityIds { get; }
    public CampaignReactionOpportunityId? ActiveOpportunityId { get; }
}
internal sealed record CampaignCombatReactionLifecycleEvent(CampaignCombatReactionLifecycleState Before,
    CampaignCombatReactionLifecycleInput Input, CampaignCombatReactionLifecycleWindow? Window, CampaignBreakdownFlow Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public string ReceiptId => (Input.Command is CampaignCombatReactionLifecycleCommand.Resolve ? "iml." : "irl.") +
        CampaignOpeningPreambleCodec.HashWithDomain(CampaignCombatReactionLifecycleCodec.ReceiptDomain(Input.Command),
            CampaignCombatReactionLifecycleCodec.SerializeEvent(this, false))[7..];
}
internal sealed class CampaignCombatReactionLifecycleState
{
    internal CampaignCombatReactionLifecycleState(CampaignCombatReactionTriggerState trigger, long version, string prefix,
        CampaignWorldSnapshotV7 world, CampaignCombatReactionLifecycleWindow? window, CampaignBreakdownFlow flow,
        IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<CampaignCombatInheritedTrack> tracks,
        IEnumerable<CampaignCombatInheritedProgress> progress, IEnumerable<CampaignCombatReactionLifecycleEvent> events)
    {
        Trigger = trigger; StateVersion = version; Prefix = prefix; World = world; ReactionWindow = window; BreakdownFlow = flow;
        Receipts = Array.AsReadOnly(receipts.ToArray()); Tracks = Array.AsReadOnly(tracks.ToArray());
        ActualProgressRefs = Array.AsReadOnly(progress.ToArray()); Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatReactionTriggerState Trigger { get; }
    public CampaignCombatReserveOpeningState Opening => Trigger.Opening;
    public LandSequencePosition SequencePosition => Trigger.SequencePosition;
    public long StateVersion { get; }
    public string Prefix { get; }
    public CampaignWorldSnapshotV7 World { get; }
    public CampaignCombatReactionLifecycleWindow? ReactionWindow { get; }
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks { get; }
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs { get; }
    internal IReadOnlyList<CampaignCombatReactionLifecycleEvent> Events { get; }
}
internal sealed record CampaignCombatReactionLifecycleResult(CampaignCombatReactionLifecycleState State, byte[] EventBytes, bool Duplicate);
