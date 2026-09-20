using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReactionCompletionEvent(CampaignCombatReactionCompletionState Before,
    CampaignCombatReactionLifecycleInput Input, CampaignCombatReactionLifecycleWindow? Window, CampaignBreakdownFlow Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public string ReceiptId => (Input.Command is CampaignCombatReactionLifecycleCommand.Resolve ? "iml." : "irl.") +
        CampaignOpeningPreambleCodec.HashWithDomain(CampaignCombatReactionLifecycleCodec.ReceiptDomain(Input.Command),
            CampaignCombatReactionCompletionCodec.SerializeEvent(this, false))[7..];
}
internal sealed class CampaignCombatReactionCompletionState
{
    internal CampaignCombatReactionCompletionState(CampaignCombatReactionSecondMoveState predecessor, long version, string prefix,
        CampaignWorldSnapshotV7 world, CampaignCombatReactionLifecycleWindow? window, CampaignBreakdownFlow flow,
        IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<CampaignCombatInheritedTrack> tracks,
        IEnumerable<CampaignCombatInheritedProgress> progress, IEnumerable<CampaignCombatReactionCompletionEvent> events)
    {
        Predecessor = predecessor; StateVersion = version; Prefix = prefix; World = world; ReactionWindow = window; BreakdownFlow = flow;
        Receipts = Array.AsReadOnly(receipts.ToArray()); Tracks = Array.AsReadOnly(tracks.ToArray());
        ActualProgressRefs = Array.AsReadOnly(progress.ToArray()); Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatReactionSecondMoveState Predecessor { get; }
    public CampaignCombatReserveOpeningState Opening => Predecessor.Opening;
    public LandSequencePosition SequencePosition => Predecessor.SequencePosition;
    public long StateVersion { get; }
    public string Prefix { get; }
    public CampaignWorldSnapshotV7 World { get; }
    public CampaignCombatReactionLifecycleWindow? ReactionWindow { get; }
    public CampaignBreakdownFlow BreakdownFlow { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks { get; }
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs { get; }
    internal IReadOnlyList<CampaignCombatReactionCompletionEvent> Events { get; }
}
internal sealed record CampaignCombatReactionCompletionResult(CampaignCombatReactionCompletionState State, byte[] EventBytes, bool Duplicate);
