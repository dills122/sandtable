using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatStageEntryCommand(int ContractVersion, string Kind,
    string CreationBinding, string CreationEventHash, long ExpectedPriorVersion, string ExpectedPositionId);

/// <summary>System actor is trusted submission metadata, including Commonwealth fleet positions.</summary>
internal sealed record CampaignCombatStageEntryInput(CampaignCombatStageEntryCommand Command,
    CampaignOpeningPreambleActor Actor);

internal sealed record CampaignCombatStageEntryEvent(CampaignCombatStageEntryState Before,
    CampaignCombatStageEntryInput Input, LandSequencePosition SequencePosition, IReadOnlyList<RuleReference> Sources)
{
    public long StateVersion => Before.StateVersion + 1;
    public string ReceiptId => "ste." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.stage-entry-receipt.v1", CampaignCombatStageEntryCodec.SerializeEvent(this, false))[7..];
}

internal sealed record CampaignCombatStageEntryEdge(string Command, string EventType,
    IReadOnlyList<RuleReference> Sources);

/// <summary>Private replay projection; complete retained history remains necessary for admission.</summary>
internal sealed class CampaignCombatStageEntryState
{
    internal CampaignCombatStageEntryState(CampaignCombatWeatherState weather, long stateVersion,
        string prefix, LandSequencePosition position, IEnumerable<CampaignOpeningPreambleReceipt> receipts,
        IEnumerable<CampaignCombatStageEntryEvent> events)
    {
        Weather = weather;
        StateVersion = stateVersion;
        Prefix = prefix;
        SequencePosition = position;
        Receipts = Array.AsReadOnly(receipts.ToArray());
        Events = Array.AsReadOnly(events.ToArray());
    }
    public CampaignCombatWeatherState Weather { get; }
    public long StateVersion { get; }
    public string Prefix { get; }
    public LandSequencePosition SequencePosition { get; }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    internal IReadOnlyList<CampaignCombatStageEntryEvent> Events { get; }
}

internal sealed record CampaignCombatStageEntryResult(CampaignCombatStageEntryState State,
    byte[] EventBytes, bool Duplicate);
