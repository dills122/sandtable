using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>First-cycle projection retaining its genuine precompletion World/member authority.</summary>
internal sealed class CampaignCombatReserveOpeningState
{
    internal CampaignCombatReserveOpeningState(CampaignCombatReserveState predecessor,
        CampaignCombatReserveCompletionEvent? completion)
    {
        Predecessor = predecessor;
        Completion = completion;
        if (completion is null)
        {
            Prefix = predecessor.Prefix;
            Receipts = Array.AsReadOnly(predecessor.Receipts.ToArray());
        }
        else
        {
            var bytes = CampaignCombatReserveCompletionCodec.SerializeEvent(completion);
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(predecessor.Prefix, bytes);
            var receipt = new CampaignOpeningPreambleReceipt(
                CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveCompletionCodec.SerializeInput(completion.Input)),
                CampaignOpeningPreambleCodec.Hash(bytes), completion.ReceiptId, completion.Input.Actor, completion.StateVersion);
            Receipts = Array.AsReadOnly(predecessor.Receipts.Append(receipt).ToArray());
        }
    }
    public CampaignCombatReserveState Predecessor { get; }
    internal CampaignCombatReserveCompletionEvent? Completion { get; }
    public long StateVersion => Completion?.StateVersion ?? Predecessor.StateVersion;
    public string Prefix { get; }
    public LandSequencePosition SequencePosition => Completion?.SequencePosition ?? Predecessor.SequencePosition;
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
    public CampaignCombatCycleAuthority? Cycle => Completion?.Cycle;
    public string? CycleId => Completion?.CycleId;
    public string? OpeningBaseHash => Completion?.OpeningBase.Hash;
    public string? CompletionReceiptId => Completion?.ReceiptId;
}

internal sealed record CampaignCombatReserveOpeningResult(CampaignCombatReserveOpeningState State, byte[] EventBytes, bool Duplicate);
