using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatInheritedMovementCommand(int ContractVersion, string Kind, string CreationBinding,
    string CreationEventHash, string CycleId, long ExpectedPriorVersion, string ExpectedPositionId,
    CampaignCombatUnitKey Unit, string OriginLocationId, string DestinationLocationId);
internal sealed record CampaignCombatInheritedMovementInput(CampaignCombatInheritedMovementCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatInheritedProgress(string EventType, string ReceiptId, string EventHash);
internal sealed class CampaignCombatInheritedTrack
{
    internal CampaignCombatInheritedTrack(CampaignCombatUnitKey unit, IEnumerable<string> route)
    { Unit = unit; Route = Array.AsReadOnly(route.ToArray()); }
    public CampaignCombatUnitKey Unit { get; }
    public IReadOnlyList<string> Route { get; }
}
internal sealed record CampaignCombatInheritedMovementEvent(CampaignCombatInheritedMovementState Before,
    CampaignCombatInheritedMovementInput Input, string RepresentationId, CapabilityPointAmount Cost,
    IReadOnlyList<RuleReference> Sources, CampaignBreakdownFlow.Moving Flow)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public CampaignElementStateV6 Element => Before.World.Elements.Single(e => e.ElementId == Input.Command.Unit.ElementId);
    public CapabilityPointAmount AfterCp => new(checked(Element.OperationalState.CapabilityPointsExpended.Numerator + Cost.Numerator), 1);
    public int AfterCohesion => checked(Element.OperationalState.CohesionLevel - (int)(Math.Max(0, AfterCp.Numerator - 10)
        - Math.Max(0, Element.OperationalState.CapabilityPointsExpended.Numerator - 10)));
    public string ReceiptId => "imv." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.inherited-movement-receipt.v4", CampaignCombatInheritedMovementCodec.SerializeEvent(this, false))[7..];
}
internal sealed class CampaignCombatInheritedMovementState
{
    internal CampaignCombatInheritedMovementState(CampaignCombatReserveOpeningState opening, long version, string prefix,
        CampaignWorldSnapshotV7 world, IEnumerable<CampaignCombatReserveMember> members,
        IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<CampaignCombatInheritedTrack> tracks,
        IEnumerable<CampaignCombatInheritedProgress> progress, CampaignBreakdownFlow.Moving? flow,
        IEnumerable<CampaignCombatInheritedMovementEvent> events)
    {
        Opening = opening; StateVersion = version; Prefix = prefix; World = world;
        Members = Array.AsReadOnly(members.ToArray()); Receipts = Array.AsReadOnly(receipts.ToArray());
        Tracks = Array.AsReadOnly(tracks.ToArray()); ActualProgressRefs = Array.AsReadOnly(progress.ToArray());
        BreakdownFlow = flow; Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatReserveOpeningState Opening { get; }
    public long StateVersion { get; }
    public string Prefix { get; }
    public LandSequencePosition SequencePosition => Opening.SequencePosition;
    public CampaignWorldSnapshotV7 World { get; }
    public IReadOnlyList<CampaignCombatReserveMember> Members { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public IReadOnlyList<CampaignCombatInheritedTrack> Tracks { get; }
    public IReadOnlyList<CampaignCombatInheritedProgress> ActualProgressRefs { get; }
    public CampaignBreakdownFlow.Moving? BreakdownFlow { get; }
    internal IReadOnlyList<CampaignCombatInheritedMovementEvent> Events { get; }
}
internal sealed record CampaignCombatInheritedMovementResult(CampaignCombatInheritedMovementState State, byte[] EventBytes, bool Duplicate);
