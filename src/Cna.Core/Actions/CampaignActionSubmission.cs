using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;

namespace Cna.Core.Actions;

public sealed record CampaignActionSubmission(
    int ContractVersion,
    string CampaignId,
    long ExpectedStateVersion,
    string ExpectedPositionId,
    CampaignActionAudience Audience,
    string ActionId)
{
    public const int CurrentContractVersion = 1;
}

public enum CampaignActionSubmissionRejectionReason
{
    None,
    InvalidSubmission,
    InvalidAuthority,
    CampaignMismatch,
    StaleState,
    UnexpectedPosition,
    ActionNotLegal,
}

public sealed record CampaignActionAcceptanceReceipt
{
    public const int CurrentContractVersion = 1;
    internal CampaignActionAcceptanceReceipt(string campaignId, long priorStateVersion,
        long committedStateVersion, string resultingPositionId, CampaignActionAudience audience,
        string actionId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(priorStateVersion, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(committedStateVersion,
            checked(priorStateVersion + 1));
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ContractVersion = CurrentContractVersion;
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        PriorStateVersion = priorStateVersion;
        CommittedStateVersion = committedStateVersion;
        ResultingPositionId = ContentContractGuards.RequireStableId(resultingPositionId,
            nameof(resultingPositionId));
        Audience = audience;
        ActionId = ContentContractGuards.RequireSha256(actionId, nameof(actionId));
    }
    public int ContractVersion { get; }
    public string CampaignId { get; }
    public long PriorStateVersion { get; }
    public long CommittedStateVersion { get; }
    public string ResultingPositionId { get; }
    public CampaignActionAudience Audience { get; }
    public string ActionId { get; }
}

public sealed record CampaignActionSubmissionResult
{
    private CampaignActionSubmissionResult(CampaignAuthorityHandle? successorHandle,
        CampaignActionAcceptanceReceipt? receipt, CampaignActionSubmissionRejectionReason reason)
    { SuccessorHandle = successorHandle; Receipt = receipt; RejectionReason = reason; }
    public bool IsAccepted => SuccessorHandle is not null;
    public CampaignAuthorityHandle? SuccessorHandle { get; }
    public CampaignActionAcceptanceReceipt? Receipt { get; }
    public CampaignActionSubmissionRejectionReason RejectionReason { get; }
    internal static CampaignActionSubmissionResult Accepted(CampaignAuthorityHandle handle,
        CampaignActionAcceptanceReceipt receipt) => new(handle, receipt,
            CampaignActionSubmissionRejectionReason.None);
    internal static CampaignActionSubmissionResult Rejected(CampaignActionSubmissionRejectionReason reason) =>
        new(null, null, reason);
}

public static class CampaignActionAcceptanceReceiptSerializer
{
    public static byte[] Serialize(CampaignActionAcceptanceReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", receipt.ContractVersion);
            writer.WriteString("campaignId", receipt.CampaignId);
            writer.WriteNumber("priorStateVersion", receipt.PriorStateVersion);
            writer.WriteNumber("committedStateVersion", receipt.CommittedStateVersion);
            writer.WriteString("resultingPositionId", receipt.ResultingPositionId);
            writer.WriteString("audience", CampaignLegalActionSerializer.FormatAudience(receipt.Audience));
            writer.WriteString("actionId", receipt.ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
