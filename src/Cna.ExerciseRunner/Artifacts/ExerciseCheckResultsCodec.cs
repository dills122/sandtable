using System.Text.Json;
using Cna.Core.Actions;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseCheckResultsCodec
{
    private static readonly string[] ResultProperties =
    [
        "contractVersion", "schemeId", "checkId", "stepOrdinal", "audience", "status",
        "failureCode",
    ];

    public static byte[] Serialize(ExerciseCheckResults checks)
    {
        ArgumentNullException.ThrowIfNull(checks);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", checks.ContractVersion);
            writer.WriteString("schemeId", checks.ContractSchemeId);
            writer.WriteStartArray("results");
            foreach (var result in checks.Results)
            {
                writer.WriteStartObject();
                writer.WriteNumber("contractVersion", result.ContractVersion);
                writer.WriteString("schemeId", result.ContractSchemeId);
                writer.WriteString("checkId", FormatCheckId(result.CheckId));
                if (result.StepOrdinal.HasValue)
                    writer.WriteNumber("stepOrdinal", result.StepOrdinal.Value);
                else writer.WriteNull("stepOrdinal");
                if (result.Audience.HasValue)
                    writer.WriteString("audience", FormatAudience(result.Audience.Value));
                else writer.WriteNull("audience");
                writer.WriteString("status", result.IsPassed ? "passed" : "failed");
                if (result.IsPassed) writer.WriteNull("failureCode");
                else writer.WriteString("failureCode", FormatFailureCode(result.FailureCode));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ExerciseCheckResults Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, ["contractVersion", "schemeId", "results"]);
            RequireHeader(root);
            var resultsElement = root.GetProperty("results");
            if (resultsElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Check results must be an array.");
            var results = resultsElement.EnumerateArray().Select(ReadResult).ToArray();
            var checks = new ExerciseCheckResults(results);
            if (!Serialize(checks).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException("Check results are not canonically encoded.");
            return checks;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException
            or FormatException)
        {
            throw new JsonException("The Exercise check results are invalid.", exception);
        }
    }

    private static ExerciseCheckResult ReadResult(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ResultProperties);
        RequireHeader(element);
        var checkId = ParseCheckId(element.GetProperty("checkId").GetString());
        var stepElement = element.GetProperty("stepOrdinal");
        var step = stepElement.ValueKind == JsonValueKind.Null
            ? (int?)null
            : stepElement.GetInt32();
        var audienceElement = element.GetProperty("audience");
        var audience = audienceElement.ValueKind == JsonValueKind.Null
            ? (CampaignActionAudience?)null
            : ParseAudience(audienceElement.GetString());
        var status = element.GetProperty("status").GetString();
        var failureElement = element.GetProperty("failureCode");
        return status switch
        {
            "passed" when failureElement.ValueKind == JsonValueKind.Null =>
                ExerciseCheckResult.Passed(checkId, step, audience),
            "failed" when failureElement.ValueKind == JsonValueKind.String =>
                ExerciseCheckResult.Failed(
                    checkId,
                    step,
                    audience,
                    ParseFailureCode(failureElement.GetString())),
            _ => throw new JsonException("Check status and failure code are contradictory."),
        };
    }

    private static void RequireHeader(JsonElement element)
    {
        if (element.GetProperty("contractVersion").GetInt32()
            != ExerciseCheckResults.CurrentContractVersion)
            throw new JsonException("Unknown check contract version.");
        if (!string.Equals(
                element.GetProperty("schemeId").GetString(),
                ExerciseCheckResults.SchemeId,
                StringComparison.Ordinal))
            throw new JsonException("Unknown check scheme.");
    }

    private static string FormatCheckId(ExerciseCheckId value) => value switch
    {
        ExerciseCheckId.AuthorityQueryValid => "authority-query-valid",
        ExerciseCheckId.ActiveAudienceCardinality => "active-audience-cardinality",
        ExerciseCheckId.SelectedActionMembership => "selected-action-membership",
        ExerciseCheckId.AcceptedEventCardinality => "accepted-event-cardinality",
        ExerciseCheckId.CheckpointContinuity => "checkpoint-continuity",
        ExerciseCheckId.TerminalBoundary => "terminal-boundary",
        ExerciseCheckId.HistoryReconstruction => "history-reconstruction",
        ExerciseCheckId.Readjudication => "readjudication",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseCheckId ParseCheckId(string? value)
    {
        foreach (var candidate in Enum.GetValues<ExerciseCheckId>())
        {
            if (string.Equals(FormatCheckId(candidate), value, StringComparison.Ordinal))
                return candidate;
        }
        throw new JsonException("Unknown check ID.");
    }

    private static string FormatAudience(CampaignActionAudience value) => value switch
    {
        CampaignActionAudience.System => "system",
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static CampaignActionAudience ParseAudience(string? value) => value switch
    {
        "system" => CampaignActionAudience.System,
        "axis" => CampaignActionAudience.Axis,
        "commonwealth" => CampaignActionAudience.Commonwealth,
        _ => throw new JsonException("Unknown check audience."),
    };

    private static string FormatFailureCode(ExerciseCheckFailureCode value) => value switch
    {
        ExerciseCheckFailureCode.AuthorityQueryRejected => "authority-query-rejected",
        ExerciseCheckFailureCode.AuthorityQueryCoordinateMismatch =>
            "authority-query-coordinate-mismatch",
        ExerciseCheckFailureCode.NoActiveAudience => "no-active-audience",
        ExerciseCheckFailureCode.MultipleActiveAudiences => "multiple-active-audiences",
        ExerciseCheckFailureCode.SelectedActionNotCurrent => "selected-action-not-current",
        ExerciseCheckFailureCode.ActionRejected => "action-rejected",
        ExerciseCheckFailureCode.EventCardinalityMismatch => "event-cardinality-mismatch",
        ExerciseCheckFailureCode.CampaignMismatch => "campaign-mismatch",
        ExerciseCheckFailureCode.RulesetMismatch => "ruleset-mismatch",
        ExerciseCheckFailureCode.StateVersionDiscontinuity => "state-version-discontinuity",
        ExerciseCheckFailureCode.PositionMismatch => "position-mismatch",
        ExerciseCheckFailureCode.TerminalBoundaryNotReached => "terminal-boundary-not-reached",
        ExerciseCheckFailureCode.ReconstructionMismatch => "reconstruction-mismatch",
        ExerciseCheckFailureCode.ReadjudicationMismatch => "readjudication-mismatch",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseCheckFailureCode ParseFailureCode(string? value)
    {
        foreach (var candidate in Enum.GetValues<ExerciseCheckFailureCode>().Where(
            value => value != ExerciseCheckFailureCode.None))
        {
            if (string.Equals(FormatFailureCode(candidate), value, StringComparison.Ordinal))
                return candidate;
        }
        throw new JsonException("Unknown check failure code.");
    }
}
