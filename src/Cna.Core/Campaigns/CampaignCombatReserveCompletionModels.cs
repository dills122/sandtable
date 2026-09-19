using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatReserveCompletionCommand(int ContractVersion, string Kind, string BaseHash,
    long ExpectedPriorVersion, string ExpectedPositionId);
internal sealed record CampaignCombatReserveCompletionInput(CampaignCombatReserveCompletionCommand Command,
    CampaignOpeningPreambleActor Actor);

/// <summary>Compatibility carrier derived privately from full history; never an admission input.</summary>
internal sealed record CampaignCombatOpeningBase(CampaignCombatCreationRequest Request, CampaignCombatReserveState Predecessor)
{
    public string Hash => CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveCompletionCodec.SerializeBase(this));
}

internal sealed record CampaignCombatCycleAuthority(int ContractVersion, string CampaignId, string RulesetHash,
    string SetupId, string SetupHash, string ContentPackId, string ContentHash, string ScenarioId,
    int GameTurn, int OperationStage, string PlayerPhaseSlot, LandSide ActingSide, int Ordinal,
    long OpenedAuthorityVersion, string OpeningPrefix, string AdmittedPolicyBundleDigest);

internal sealed record CampaignCombatReserveCompletionEvent(CampaignCombatOpeningBase OpeningBase,
    CampaignCombatReserveCompletionInput Input, LandSequencePosition SequencePosition, CampaignCombatCycleAuthority Cycle)
{
    public long StateVersion => Cycle.OpenedAuthorityVersion;
    public string CycleId => CampaignCombatReserveCompletionCodec.CycleId(Cycle);
    public string ReceiptId => "rc." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.reserve-completion-receipt.v2", CampaignCombatReserveCompletionCodec.SerializeEvent(this, false))[7..];
}

/// <summary>Validated completion bytes and actual predecessor. Applying its terminal projection is a later gate.</summary>
internal sealed class CampaignCombatReserveCompletionEvidence
{
    internal CampaignCombatReserveCompletionEvidence(CampaignCombatReserveCompletionEvent value) => Event = value;
    public CampaignCombatReserveCompletionEvent Event { get; }
    public CampaignCombatOpeningBase OpeningBase => Event.OpeningBase;
    public CampaignCombatReserveState Predecessor => OpeningBase.Predecessor;
    public byte[] EventBytes => CampaignCombatReserveCompletionCodec.SerializeEvent(Event);
}
