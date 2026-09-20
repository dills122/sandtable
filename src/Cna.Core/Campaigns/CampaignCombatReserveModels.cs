using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReserveCommand(int ContractVersion, string Kind,
    string CreationBinding, string CreationEventHash, long ExpectedPriorVersion, string ExpectedPositionId, string ElementId);
internal sealed record CampaignCombatReserveInput(CampaignCombatReserveCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatReserveScope(int GameTurn, int OperationStage, string PlayerPhaseSlot, LandSide ActingSide);
/// <summary>Designation-only history: every other release-history field is canonically null.</summary>
internal sealed record CampaignCombatReserveHistory(CampaignCombatReserveScope Scope, string? DesignationReceiptId);
internal sealed record CampaignCombatReserveMember(CampaignCombatUnitKey Unit, CampaignElementReserveStatus Status,
    int BaseCpa, CapabilityPointAmount SpentCp, CampaignCombatReserveHistory History);
internal sealed record CampaignCombatReserveEvent(CampaignCombatReserveState Before, CampaignCombatReserveInput Input)
{
    public long StateVersion => Before.StateVersion + 1;
    public string ReceiptId => "rd." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.reserve-designation-receipt.v2", CampaignCombatReserveCodec.SerializeEvent(this, false))[7..];
}

/// <summary>Precompletion private projection. Admission always replays complete retained history.</summary>
internal sealed class CampaignCombatReserveState
{
    internal CampaignCombatReserveState(CampaignCombatStageEntryState stage, long version, string prefix,
        LandSide firstActingSide, CampaignWorldSnapshotV7 world, IEnumerable<CampaignCombatReserveMember> members,
        IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<CampaignCombatReserveEvent> events)
    {
        Stage = stage;
        StateVersion = version;
        Prefix = prefix;
        FirstActingSide = firstActingSide;
        World = world;
        Members = Array.AsReadOnly(members.ToArray());
        Receipts = Array.AsReadOnly(receipts.ToArray());
        Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatStageEntryState Stage { get; }
    public long StateVersion { get; }
    public string Prefix { get; }
    public LandSequencePosition SequencePosition => Stage.SequencePosition;
    public LandSide FirstActingSide { get; }
    public CampaignWorldSnapshotV7 World { get; }
    public IReadOnlyList<CampaignCombatReserveMember> Members { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    internal IReadOnlyList<CampaignCombatReserveEvent> Events { get; }
}
internal sealed record CampaignCombatReserveResult(CampaignCombatReserveState State, byte[] EventBytes, bool Duplicate);
