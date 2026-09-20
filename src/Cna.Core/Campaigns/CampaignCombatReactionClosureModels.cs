using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReactionClosureIdentity(int ContractVersion, string ActionId, string CreationBinding,
    string CreationEventHash, string CycleId, long ExpectedPriorVersion, string ExpectedPositionId);
internal sealed record CampaignCombatReactionClosureCommand(CampaignCombatReactionClosureIdentity Identity, string Kind, string WindowId)
{
    public string Reason => Kind switch
    {
        "decline-reaction-window" => "player-decline",
        "close-reaction-window-scripted-unavailable" => "scripted-unavailable",
        "close-reaction-window-timeout" => "timeout",
        _ => throw new System.Text.Json.JsonException("Unknown direct Reaction closure kind."),
    };
}
internal sealed record CampaignCombatReactionClosureInput(CampaignCombatReactionClosureCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatReactionClosureEvent(CampaignCombatReactionClosureState Before,
    CampaignCombatReactionClosureInput Input, CampaignBreakdownFlow.Moving Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public string ReceiptId => "irl." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.inherited-reaction-window-closed-receipt.v3",
        CampaignCombatReactionClosureCodec.SerializeEvent(this, false))[7..];
}

/// <summary>Direct closure retains the actual inactive F1 authority; no F2 participant event is admitted.</summary>
internal sealed class CampaignCombatReactionClosureState
{
    internal CampaignCombatReactionClosureState(CampaignCombatReactionLifecycleState predecessor, long version, string prefix,
        CampaignBreakdownFlow flow, IEnumerable<CampaignOpeningPreambleReceipt> receipts, CampaignCombatReactionClosureEvent? accepted)
    {
        Predecessor = predecessor; StateVersion = version; Prefix = prefix; BreakdownFlow = flow;
        Receipts = Array.AsReadOnly(receipts.ToArray()); Accepted = accepted;
    }
    public CampaignCombatReactionLifecycleState Predecessor { get; }
    public CampaignCombatReserveOpeningState Opening => Predecessor.Opening;
    public LandSequencePosition SequencePosition => Predecessor.SequencePosition;
    public long StateVersion { get; }
    public string Prefix { get; }
    public CampaignWorldSnapshotV7 World => Predecessor.World;
    public CampaignCombatReactionLifecycleWindow? ReactionWindow => Accepted is null ? Predecessor.ReactionWindow : null;
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks => Predecessor.Tracks;
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs => Predecessor.ActualProgressRefs;
    internal CampaignCombatReactionClosureEvent? Accepted { get; }
}
internal sealed record CampaignCombatReactionClosureResult(CampaignCombatReactionClosureState State, byte[] EventBytes, bool Duplicate);
