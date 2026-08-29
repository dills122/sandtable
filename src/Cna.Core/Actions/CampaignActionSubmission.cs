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

    internal CampaignActionAcceptanceReceipt(
        string campaignId,
        long priorStateVersion,
        long committedStateVersion,
        string resultingPositionId,
        CampaignActionAudience audience,
        string actionId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(priorStateVersion, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            committedStateVersion,
            checked(priorStateVersion + 1));
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ContractVersion = CurrentContractVersion;
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        PriorStateVersion = priorStateVersion;
        CommittedStateVersion = committedStateVersion;
        ResultingPositionId = ContentContractGuards.RequireStableId(
            resultingPositionId,
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
    private CampaignActionSubmissionResult(
        CampaignAuthorityHandle? successorHandle,
        CampaignActionAcceptanceReceipt? receipt,
        CampaignActionSubmissionRejectionReason reason)
    {
        SuccessorHandle = successorHandle;
        Receipt = receipt;
        RejectionReason = reason;
    }

    public bool IsAccepted => SuccessorHandle is not null;
    public CampaignAuthorityHandle? SuccessorHandle { get; }
    public CampaignActionAcceptanceReceipt? Receipt { get; }
    public CampaignActionSubmissionRejectionReason RejectionReason { get; }

    internal static CampaignActionSubmissionResult Accepted(
        CampaignAuthorityHandle handle,
        CampaignActionAcceptanceReceipt receipt) => new(
            handle,
            receipt,
            CampaignActionSubmissionRejectionReason.None);

    internal static CampaignActionSubmissionResult Rejected(
        CampaignActionSubmissionRejectionReason reason) => new(null, null, reason);
}

public static class CampaignActionSubmissionSerializer
{
    public static byte[] SerializeCanonical(CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
        {
            throw new ArgumentException("The action submission is invalid.", nameof(submission));
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", submission.ContractVersion);
            writer.WriteString("campaignId", submission.CampaignId);
            writer.WriteNumber("expectedStateVersion", submission.ExpectedStateVersion);
            writer.WriteString("expectedPositionId", submission.ExpectedPositionId);
            writer.WriteString("audience", CampaignLegalActionSerializer.FormatAudience(
                submission.Audience));
            writer.WriteString("actionId", submission.ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Reads canonical submission data. Current authority and membership still require Submit.
    /// </summary>
    public static CampaignActionSubmission DeserializeCanonical(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = Parse(utf8Json);
            var root = document.RootElement;
            CampaignLegalActionSerializer.RequireProperties(
                root,
                "contractVersion",
                "campaignId",
                "expectedStateVersion",
                "expectedPositionId",
                "audience",
                "actionId");
            var submission = new CampaignActionSubmission(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("expectedStateVersion").GetInt64(),
                root.GetProperty("expectedPositionId").GetString()!,
                CampaignLegalActionSerializer.ParseAudience(
                    root.GetProperty("audience").GetString()),
                root.GetProperty("actionId").GetString()!);
            if (!CampaignActionContractValidator.IsValidSubmission(submission)
                || !utf8Json.SequenceEqual(SerializeCanonical(submission)))
            {
                throw new JsonException("The action submission is not canonical JSON.");
            }

            return submission;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or FormatException
            or KeyNotFoundException
            or OverflowException)
        {
            throw new JsonException("The action submission JSON is invalid.", exception);
        }
    }

    internal static JsonDocument Parse(ReadOnlySpan<byte> utf8Json) => JsonDocument.Parse(
        utf8Json.ToArray(),
        new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 8,
        });
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
            writer.WriteString(
                "audience",
                CampaignLegalActionSerializer.FormatAudience(receipt.Audience));
            writer.WriteString("actionId", receipt.ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Reads a canonical receipt as transition evidence without exposing authority state.
    /// </summary>
    public static CampaignActionAcceptanceReceipt DeserializeCanonical(
        ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = CampaignActionSubmissionSerializer.Parse(utf8Json);
            var root = document.RootElement;
            CampaignLegalActionSerializer.RequireProperties(
                root,
                "contractVersion",
                "campaignId",
                "priorStateVersion",
                "committedStateVersion",
                "resultingPositionId",
                "audience",
                "actionId");
            if (root.GetProperty("contractVersion").GetInt32()
                != CampaignActionAcceptanceReceipt.CurrentContractVersion)
            {
                throw new JsonException("The action receipt version is unsupported.");
            }

            var receipt = new CampaignActionAcceptanceReceipt(
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("committedStateVersion").GetInt64(),
                root.GetProperty("resultingPositionId").GetString()!,
                CampaignLegalActionSerializer.ParseAudience(
                    root.GetProperty("audience").GetString()),
                root.GetProperty("actionId").GetString()!);
            if (!utf8Json.SequenceEqual(Serialize(receipt)))
            {
                throw new JsonException("The action receipt is not canonical JSON.");
            }

            return receipt;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or FormatException
            or KeyNotFoundException
            or OverflowException)
        {
            throw new JsonException("The action receipt JSON is invalid.", exception);
        }
    }
}
