using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReactionFallbackIdentity(int ContractVersion, string ActionId, string CreationBinding,
    string CreationEventHash, string CycleId, long ExpectedPriorVersion, string ExpectedPositionId);
internal sealed record CampaignCombatReactionFallbackCommand(CampaignCombatReactionFallbackIdentity Identity, string Kind, string Handle)
{
    public string Reason => Kind switch
    {
        "close-reaction-window-scripted-unavailable" => "scripted-unavailable",
        "close-reaction-window-timeout" => "timeout",
        _ => throw new System.Text.Json.JsonException("Unknown active Reaction fallback kind."),
    };
}
internal sealed record CampaignCombatReactionFallbackInput(CampaignCombatReactionFallbackCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatReactionFallbackEvent(CampaignCombatReactionFallbackState Before,
    CampaignCombatReactionFallbackInput Input, CampaignBreakdownFlow Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public string ReceiptId => (Input.Command.Kind == "resolve-breakdown-stop" ? "iml." : "irl.") + CampaignOpeningPreambleCodec.HashWithDomain(
        Input.Command.Kind == "resolve-breakdown-stop" ? "sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2" : "sandtable.combat.inherited-reaction-window-closed-receipt.v3",
        CampaignCombatReactionFallbackCodec.SerializeEvent(this, false))[7..];
}

/// <summary>Active fallback retains the actual F2 first participant move and mandatory closed reactor stop.</summary>
internal sealed class CampaignCombatReactionFallbackState
{
    internal CampaignCombatReactionFallbackState(CampaignCombatReactionLifecycleState predecessor, long version, string prefix,
        CampaignBreakdownFlow flow, IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<CampaignCombatReactionFallbackEvent> events)
    {
        Predecessor = predecessor; StateVersion = version; Prefix = prefix; BreakdownFlow = flow;
        Receipts = Array.AsReadOnly(receipts.ToArray()); Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatReactionLifecycleState Predecessor { get; }
    public CampaignCombatReserveOpeningState Opening => Predecessor.Opening;
    public LandSequencePosition SequencePosition => Predecessor.SequencePosition;
    public long StateVersion { get; }
    public string Prefix { get; }
    public CampaignWorldSnapshotV7 World => Predecessor.World;
    public CampaignCombatReactionLifecycleWindow? ReactionWindow => Events.Count == 0 ? Predecessor.ReactionWindow : null;
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks => Predecessor.Tracks;
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs => Predecessor.ActualProgressRefs;
    internal IReadOnlyList<CampaignCombatReactionFallbackEvent> Events { get; }
}
internal sealed record CampaignCombatReactionFallbackResult(CampaignCombatReactionFallbackState State, byte[] EventBytes, bool Duplicate);
