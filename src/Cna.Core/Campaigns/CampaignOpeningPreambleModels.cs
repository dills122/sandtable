using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignOpeningCommandKind { Initiative, ConvoySchedule, TacticalShipping, InitiativeOrder }
internal enum CampaignOpeningPreambleActor { System, Axis, Commonwealth }

internal sealed record CampaignOpeningPreambleCommand(int ContractVersion, CampaignOpeningCommandKind Kind,
    string CreationBinding, string CreationEventHash, long ExpectedPriorVersion, string ExpectedPositionId,
    int? OperationStage, LandSide? DeclaringSide, InitiativeOrderChoice? Choice);

/// <summary>Actor is trusted submission metadata, never authentication asserted by a remote command.</summary>
internal sealed record CampaignOpeningPreambleInput(CampaignOpeningPreambleCommand Command,
    CampaignOpeningPreambleActor Actor);

internal sealed record CampaignOpeningPreambleOrder(int GameTurn, int OperationStage,
    LandSide FirstSide, LandSide SecondSide);
internal sealed record CampaignOpeningPreambleReceipt(string CommandHash, string EventHash,
    string ReceiptId, CampaignOpeningPreambleActor Actor, long StateVersion);

internal abstract record CampaignOpeningPreambleEvent(CampaignOpeningPreambleState Before,
    CampaignOpeningPreambleInput Input, LandSequencePosition SequencePosition, IReadOnlyList<RuleReference> Sources)
{
    public long StateVersion => Before.StateVersion + 1;
    public string ReceiptId => "pre." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.opening-preamble-receipt.v1", CampaignOpeningPreambleCodec.SerializeEvent(this, false))[7..];
}
internal sealed record CampaignOpeningInitiativeEvent(CampaignOpeningPreambleState Before,
    CampaignOpeningPreambleInput Input, LandSequencePosition SequencePosition, IReadOnlyList<RuleReference> Sources,
    LandSide Holder) : CampaignOpeningPreambleEvent(Before, Input, SequencePosition, Sources);
internal sealed record CampaignOpeningAdvanceEvent(CampaignOpeningPreambleState Before,
    CampaignOpeningPreambleInput Input, LandSequencePosition SequencePosition, IReadOnlyList<RuleReference> Sources)
    : CampaignOpeningPreambleEvent(Before, Input, SequencePosition, Sources);
internal sealed record CampaignOpeningOrderEvent(CampaignOpeningPreambleState Before,
    CampaignOpeningPreambleInput Input, LandSequencePosition SequencePosition, IReadOnlyList<RuleReference> Sources,
    LandSide DeclaringHolder, LandSide FirstSide, LandSide SecondSide)
    : CampaignOpeningPreambleEvent(Before, Input, SequencePosition, Sources);

/// <summary>Private replay projection; never a general Snapshot12 or published archive head.</summary>
internal sealed class CampaignOpeningPreambleState
{
    internal CampaignOpeningPreambleState(CampaignCreationSnapshotV12 creation, long stateVersion,
        string prefix, LandSequencePosition position, LandSide? holder,
        IEnumerable<CampaignOpeningPreambleOrder> orders, IEnumerable<CampaignOpeningPreambleReceipt> receipts,
        IEnumerable<CampaignOpeningPreambleEvent> events)
    {
        Creation = creation;
        StateVersion = stateVersion;
        Prefix = prefix;
        SequencePosition = position;
        InitiativeHolder = holder;
        Orders = Array.AsReadOnly(orders.ToArray());
        Receipts = Array.AsReadOnly(receipts.ToArray());
        Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCreationSnapshotV12 Creation { get; }
    public long StateVersion { get; }
    public string Prefix { get; }
    public LandSequencePosition SequencePosition { get; }
    public LandSide? InitiativeHolder { get; }
    public IReadOnlyList<CampaignOpeningPreambleOrder> Orders { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    internal IReadOnlyList<CampaignOpeningPreambleEvent> Events { get; }
}

internal sealed record CampaignOpeningPreambleResult(CampaignOpeningPreambleState State,
    byte[] EventBytes, bool Duplicate);
