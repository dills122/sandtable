using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatBreakdownCompletionCommand(int ContractVersion, string Kind, int OperationStage,
    string ActionId, string CreationBinding, string CreationEventHash, string CycleId, long ExpectedPriorVersion, string ExpectedPositionId);
internal sealed record CampaignCombatBreakdownCompletionInput(CampaignCombatBreakdownCompletionCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record CampaignCombatBreakdownCompletionEvent(CampaignCombatMovementLifecycleState Before,
    CampaignCombatBreakdownCompletionInput Input, LandSequencePosition SequencePosition)
{
    public long StateVersion => checked(Before.StateVersion + 1);
    public string ReceiptId => "ibc." + CampaignOpeningPreambleCodec.HashWithDomain(
        "sandtable.combat.inherited-breakdown-completion-receipt.v2", CampaignCombatBreakdownCompletionCodec.SerializeEvent(this, false))[7..];
}
/// <summary>Combat entry with unchanged, actual completed Movement lifecycle authority.</summary>
internal sealed class CampaignCombatBreakdownCompletionState
{
    internal CampaignCombatBreakdownCompletionState(CampaignCombatMovementLifecycleState lifecycle, CampaignCombatBreakdownCompletionEvent? completion)
    {
        Lifecycle = lifecycle; Completion = completion;
        if (completion is null)
        {
            Prefix = lifecycle.Prefix;
            Receipts = Array.AsReadOnly(lifecycle.Receipts.ToArray());
        }
        else
        {
            var bytes = CampaignCombatBreakdownCompletionCodec.SerializeEvent(completion);
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(lifecycle.Prefix, bytes);
            var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatBreakdownCompletionCodec.SerializeInput(completion.Input)),
                CampaignOpeningPreambleCodec.Hash(bytes), completion.ReceiptId, completion.Input.Actor, completion.StateVersion);
            Receipts = Array.AsReadOnly(lifecycle.Receipts.Append(receipt).ToArray());
        }
    }
    public CampaignCombatMovementLifecycleState Lifecycle { get; }
    internal CampaignCombatBreakdownCompletionEvent? Completion { get; }
    public long StateVersion => Completion?.StateVersion ?? Lifecycle.StateVersion;
    public string Prefix { get; }
    public LandSequencePosition SequencePosition => Completion?.SequencePosition ?? Lifecycle.SequencePosition;
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public string? BreakdownCompletionReceiptId => Completion?.ReceiptId;
}
internal sealed record CampaignCombatBreakdownCompletionResult(CampaignCombatBreakdownCompletionState State, byte[] EventBytes, bool Duplicate);
