using Cna.Core.Rules;
namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReactingPosition(LandSequencePosition SuspendedMovementPosition, LandSide PhasingSide, LandSide ReactingSide);
internal sealed record CampaignCombatReactionTriggerAuthority(int MoveContractVersion, string ElementId,
    CampaignMapRepresentationState TriggeringRepresentation, string OriginLocationId, string DestinationLocationId);
internal sealed class CampaignCombatReactionWindow
{
    internal CampaignCombatReactionWindow(CampaignReactionWindowId windowId, long version, LandSide phasingSide, LandSide reactingSide,
        CampaignCombatReactingPosition position, CampaignCombatReactionTriggerAuthority authority, CampaignApparentReactionTrigger apparent,
        IEnumerable<CampaignFrozenReactionOpportunity> opportunities)
    {
        WindowId = windowId; TriggerCommittedStateVersion = version; PhasingSide = phasingSide; ReactingSide = reactingSide;
        ReactingPosition = position; TriggerAuthority = authority; ApparentTrigger = apparent;
        FrozenOpportunities = Array.AsReadOnly(opportunities.OrderBy(o => o.OpportunityId.Value, StringComparer.Ordinal).ToArray());
    }
    public CampaignReactionWindowId WindowId { get; }
    public long TriggerCommittedStateVersion { get; }
    public LandSide PhasingSide { get; }
    public LandSide ReactingSide { get; }
    public CampaignCombatReactingPosition ReactingPosition { get; }
    public CampaignCombatReactionTriggerAuthority TriggerAuthority { get; }
    public CampaignApparentReactionTrigger ApparentTrigger { get; }
    public IReadOnlyList<CampaignFrozenReactionOpportunity> FrozenOpportunities { get; }
    public IReadOnlyList<CampaignReactionOpportunityId> ResolvedOpportunityIds { get; } = Array.Empty<CampaignReactionOpportunityId>();
    public CampaignReactionOpportunityId? ActiveOpportunityId { get; }
}
internal sealed record CampaignCombatReactionTriggerEvent(CampaignCombatInheritedMovementState Before,
    CampaignCombatInheritedMovementInput Input, string RepresentationId, CapabilityPointAmount Cost,
    IReadOnlyList<RuleReference> Sources, CampaignCombatReactionWindow Window, CampaignBreakdownFlow.Reacting Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public CampaignElementStateV6 Element => Before.World.Elements.Single(e => e.ElementId == Input.Command.Unit.ElementId);
    public CapabilityPointAmount AfterCp => new(checked(Element.OperationalState.CapabilityPointsExpended.Numerator + Cost.Numerator), 1);
    public int AfterCohesion => Element.OperationalState.CohesionLevel;
    public string ReceiptId => "irt." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-reaction-trigger-receipt.v1",
        CampaignCombatReactionTriggerCodec.SerializeEvent(this, false))[7..];
}
internal sealed class CampaignCombatReactionTriggerState
{
    internal CampaignCombatReactionTriggerState(CampaignCombatInheritedMovementState predecessor, CampaignWorldSnapshotV7 world,
        IEnumerable<CampaignCombatReserveMember> members, IEnumerable<CampaignOpeningPreambleReceipt> receipts,
        IEnumerable<CampaignCombatInheritedTrack> tracks, IEnumerable<CampaignCombatInheritedProgress> progress,
        CampaignCombatReactionTriggerEvent? trigger, string prefix)
    {
        Predecessor = predecessor; World = world; Members = Array.AsReadOnly(members.ToArray()); Receipts = Array.AsReadOnly(receipts.ToArray());
        Tracks = Array.AsReadOnly(tracks.ToArray()); ActualProgressRefs = Array.AsReadOnly(progress.ToArray()); Trigger = trigger; Prefix = prefix;
    }
    public CampaignCombatInheritedMovementState Predecessor { get; }
    public CampaignCombatReserveOpeningState Opening => Predecessor.Opening;
    public long StateVersion => Trigger?.StateVersion ?? Predecessor.StateVersion;
    public string Prefix { get; }
    public LandSequencePosition SequencePosition => Predecessor.SequencePosition;
    public CampaignWorldSnapshotV7 World { get; }
    public IReadOnlyList<CampaignCombatReserveMember> Members { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks { get; }
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs { get; }
    public CampaignCombatReactionWindow? ReactionWindow => Trigger?.Window;
    public CampaignBreakdownFlow? BreakdownFlow => Trigger is null ? Predecessor.BreakdownFlow : Trigger.Flow;
    internal CampaignCombatReactionTriggerEvent? Trigger { get; }
}
internal sealed record CampaignCombatReactionTriggerResult(CampaignCombatReactionTriggerState State, byte[] EventBytes, bool Duplicate);
