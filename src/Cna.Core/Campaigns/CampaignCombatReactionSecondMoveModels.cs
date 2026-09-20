using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReactionSecondMoveInput(CampaignCombatReactionLifecycleCommand.Move Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatReactionSecondMoveEvent(CampaignCombatReactionSecondMoveState Before,
    CampaignCombatReactionSecondMoveInput Input, CampaignBreakdownFlow.Reacting Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public string ReceiptId => "irl." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.inherited-reacting-element-moved-receipt.v3", CampaignCombatReactionSecondMoveCodec.SerializeEvent(this, false))[7..];
}

/// <summary>Second move retains actual first participant authority and its open route.</summary>
internal sealed class CampaignCombatReactionSecondMoveState
{
    internal CampaignCombatReactionSecondMoveState(CampaignCombatReactionLifecycleState predecessor, long version, string prefix,
        CampaignWorldSnapshotV7 world, CampaignBreakdownFlow flow, IEnumerable<CampaignOpeningPreambleReceipt> receipts,
        IEnumerable<CampaignCombatInheritedTrack> tracks, IEnumerable<CampaignCombatInheritedProgress> progress,
        IEnumerable<CampaignCombatReactionSecondMoveEvent> events)
    {
        Predecessor = predecessor; StateVersion = version; Prefix = prefix; World = world; BreakdownFlow = flow;
        Receipts = Array.AsReadOnly(receipts.ToArray()); Tracks = Array.AsReadOnly(tracks.ToArray());
        ActualProgressRefs = Array.AsReadOnly(progress.ToArray()); Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatReactionLifecycleState Predecessor { get; }
    public CampaignCombatReserveOpeningState Opening => Predecessor.Opening;
    public LandSequencePosition SequencePosition => Predecessor.SequencePosition;
    public long StateVersion { get; }
    public string Prefix { get; }
    public CampaignWorldSnapshotV7 World { get; }
    public CampaignCombatReactionLifecycleWindow ReactionWindow => Predecessor.ReactionWindow!;
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks { get; }
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs { get; }
    internal IReadOnlyList<CampaignCombatReactionSecondMoveEvent> Events { get; }
}
internal sealed record CampaignCombatReactionSecondMoveResult(CampaignCombatReactionSecondMoveState State, byte[] EventBytes, bool Duplicate);
