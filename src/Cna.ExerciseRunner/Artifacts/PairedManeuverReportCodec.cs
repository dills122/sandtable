using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;

namespace Cna.ExerciseRunner.Artifacts;

public static class PairedManeuverReportCodec
{
    public static byte[] Serialize(PairedManeuverReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", report.ContractVersion);
            writer.WriteString("schemeId", report.ContractSchemeId);
            writer.WritePropertyName("deterministic");
            writer.WriteRawValue(SerializeDeterministic(report.Deterministic));
            writer.WriteString("reportFingerprint", report.ReportFingerprint);
            WriteDiagnostics(writer, report.Diagnostics);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static PairedManeuverReport Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(
                root,
                ["contractVersion", "schemeId", "deterministic", "reportFingerprint", "diagnostics"]);
            if (root.GetProperty("contractVersion").GetInt32()
                != PairedManeuverReport.CurrentContractVersion)
                throw new JsonException("Unknown paired Maneuver report contract version.");
            if (!string.Equals(
                    root.GetProperty("schemeId").GetString(),
                    PairedManeuverReport.SchemeId,
                    StringComparison.Ordinal))
                throw new JsonException("Unknown paired Maneuver report scheme.");

            var report = new PairedManeuverReport(
                ReadDeterministic(root.GetProperty("deterministic")),
                ReadDiagnostics(root.GetProperty("diagnostics")));
            var suppliedFingerprint = RequiredString(root, "reportFingerprint");
            ReplayProofValidation.RequireSha256(suppliedFingerprint, "reportFingerprint");
            if (!string.Equals(
                    report.ReportFingerprint,
                    suppliedFingerprint,
                    StringComparison.Ordinal))
                throw new JsonException("The paired Maneuver report fingerprint does not match.");
            if (!Serialize(report).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException("The paired Maneuver report is not canonically encoded.");
            return report;
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
            throw new JsonException("The paired Maneuver report is invalid.", exception);
        }
    }

    internal static string Fingerprint(PairedManeuverReportDeterministic deterministic) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(SerializeDeterministic(deterministic)))}";

    internal static byte[] SerializeDeterministic(
        PairedManeuverReportDeterministic deterministic)
    {
        ArgumentNullException.ThrowIfNull(deterministic);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("manifest");
            writer.WriteRawValue(PairedManeuverManifestCodec.Serialize(deterministic.Manifest));
            writer.WriteString("status", Format(deterministic.Status));
            WriteCounts(writer, deterministic.Counts);
            writer.WriteStartArray("terminalCounts");
            foreach (var count in deterministic.TerminalCounts)
            {
                writer.WriteStartObject();
                WriteOutcomeProperties(writer, count.Outcome);
                writer.WriteNumber("count", count.Count);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("failureCounts");
            foreach (var count in deterministic.FailureCounts)
            {
                writer.WriteStartObject();
                writer.WriteString("category", ExerciseContractText.FormatFailure(count.Category));
                writer.WriteNumber("count", count.Count);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("aggregationFailureCounts");
            foreach (var count in deterministic.AggregationFailureCounts)
            {
                writer.WriteStartObject();
                writer.WriteString("category", Format(count.Category));
                writer.WriteNumber("count", count.Count);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("entries");
            foreach (var entry in deterministic.Entries) WriteEntry(writer, entry);
            writer.WriteEndArray();
            writer.WriteStartArray("comparisons");
            foreach (var comparison in deterministic.Comparisons)
                WriteComparison(writer, comparison);
            writer.WriteEndArray();
            writer.WriteString("interpretation", PairedManeuverReport.Interpretation);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static PairedManeuverReportDeterministic ReadDeterministic(JsonElement element)
    {
        StrictJson.RequireExactProperties(
            element,
            [
                "manifest", "status", "counts", "terminalCounts", "failureCounts",
                "aggregationFailureCounts", "entries", "comparisons", "interpretation",
            ]);
        if (!string.Equals(
                element.GetProperty("interpretation").GetString(),
                PairedManeuverReport.Interpretation,
                StringComparison.Ordinal))
            throw new JsonException("The paired Maneuver interpretation is not canonical.");
        return new PairedManeuverReportDeterministic(
            PairedManeuverManifestCodec.Deserialize(
                Encoding.UTF8.GetBytes(element.GetProperty("manifest").GetRawText())),
            ParseStatus(element.GetProperty("status").GetString()),
            ReadCounts(element.GetProperty("counts")),
            ReadArray(element, "terminalCounts", ReadTerminalCount),
            ReadArray(element, "failureCounts", ReadFailureCount),
            ReadArray(element, "aggregationFailureCounts", ReadAggregationFailureCount),
            ReadArray(element, "entries", ReadEntry),
            ReadArray(element, "comparisons", ReadComparison));
    }

    private static void WriteCounts(Utf8JsonWriter writer, ManeuverReportCounts counts)
    {
        writer.WriteStartObject("counts");
        writer.WriteNumber("requestedExerciseCount", counts.RequestedExerciseCount);
        writer.WriteNumber("attemptedExerciseCount", counts.AttemptedExerciseCount);
        writer.WriteNumber("validatedExerciseCount", counts.ValidatedExerciseCount);
        writer.WriteNumber("succeededExerciseCount", counts.SucceededExerciseCount);
        writer.WriteNumber("failedExerciseCount", counts.FailedExerciseCount);
        writer.WriteNumber(
            "aggregationFailedExerciseCount",
            counts.AggregationFailedExerciseCount);
        writer.WriteNumber("notRunExerciseCount", counts.NotRunExerciseCount);
        writer.WriteEndObject();
    }

    private static ManeuverReportCounts ReadCounts(JsonElement element)
    {
        StrictJson.RequireExactProperties(
            element,
            [
                "requestedExerciseCount", "attemptedExerciseCount", "validatedExerciseCount",
                "succeededExerciseCount", "failedExerciseCount",
                "aggregationFailedExerciseCount", "notRunExerciseCount",
            ]);
        return new ManeuverReportCounts(
            element.GetProperty("requestedExerciseCount").GetInt32(),
            element.GetProperty("attemptedExerciseCount").GetInt32(),
            element.GetProperty("validatedExerciseCount").GetInt32(),
            element.GetProperty("succeededExerciseCount").GetInt32(),
            element.GetProperty("failedExerciseCount").GetInt32(),
            element.GetProperty("aggregationFailedExerciseCount").GetInt32(),
            element.GetProperty("notRunExerciseCount").GetInt32());
    }

    private static void WriteEntry(Utf8JsonWriter writer, ManeuverReportEntry entry)
    {
        writer.WriteStartObject();
        writer.WriteNumber("ordinal", entry.Ordinal);
        writer.WriteString("exerciseId", entry.ExerciseId);
        writer.WriteString("variant", Format(entry.Variant));
        writer.WriteString("status", Format(entry.Status));
        if (entry.TerminalOutcome is null) writer.WriteNull("terminalOutcome");
        else
        {
            writer.WriteStartObject("terminalOutcome");
            WriteOutcomeProperties(writer, entry.TerminalOutcome);
            writer.WriteEndObject();
        }
        WriteNullable(
            writer,
            "failureCategory",
            entry.FailureCategory,
            ExerciseContractText.FormatFailure);
        WriteNullable(
            writer,
            "aggregationFailureCategory",
            entry.AggregationFailureCategory,
            Format);
        WriteNullable(writer, "notRunReason", entry.NotRunReason, Format);
        WriteNullable(writer, "acceptedStepCount", entry.AcceptedStepCount);
        WriteNullable(writer, "passedCheckCount", entry.PassedCheckCount);
        WriteNullable(writer, "failedCheckCount", entry.FailedCheckCount);
        WriteNullable(writer, "normalizedManifestSha256", entry.NormalizedManifestSha256);
        WriteNullable(writer, "seedLedgerSha256", entry.SeedLedgerSha256);
        writer.WriteEndObject();
    }

    private static ManeuverReportEntry ReadEntry(JsonElement element)
    {
        StrictJson.RequireExactProperties(
            element,
            [
                "ordinal", "exerciseId", "variant", "status", "terminalOutcome",
                "failureCategory", "aggregationFailureCategory", "notRunReason",
                "acceptedStepCount", "passedCheckCount", "failedCheckCount",
                "normalizedManifestSha256", "seedLedgerSha256",
            ]);
        var outcome = element.GetProperty("terminalOutcome");
        return new ManeuverReportEntry(
            element.GetProperty("ordinal").GetInt32(),
            RequiredString(element, "exerciseId"),
            ParseVariant(element.GetProperty("variant").GetString()),
            ParseEntryStatus(element.GetProperty("status").GetString()),
            outcome.ValueKind == JsonValueKind.Null ? null : ReadOutcome(outcome),
            ReadNullable(element, "failureCategory", ExerciseContractText.ParseFailure),
            ReadNullable(
                element,
                "aggregationFailureCategory",
                ParseAggregationFailure),
            ReadNullable(element, "notRunReason", ParseNotRunReason),
            ReadNullableInt32(element, "acceptedStepCount"),
            ReadNullableInt32(element, "passedCheckCount"),
            ReadNullableInt32(element, "failedCheckCount"),
            ReadNullableString(element, "normalizedManifestSha256"),
            ReadNullableString(element, "seedLedgerSha256"));
    }

    private static void WriteComparison(
        Utf8JsonWriter writer,
        PairedManeuverComparison comparison)
    {
        writer.WriteStartObject();
        writer.WriteString("pairKey", comparison.PairKey);
        writer.WriteNumber("repetition", comparison.Repetition);
        writer.WriteNumber("baselineEntryOrdinal", comparison.BaselineEntryOrdinal);
        writer.WriteNumber("candidateEntryOrdinal", comparison.CandidateEntryOrdinal);
        writer.WriteString("status", Format(comparison.Status));
        WriteNullable(writer, "creationInputsSha256", comparison.CreationInputsSha256);
        WriteNullable(
            writer,
            "baselineInitialSnapshotSha256",
            comparison.BaselineInitialSnapshotSha256);
        WriteNullable(
            writer,
            "candidateInitialSnapshotSha256",
            comparison.CandidateInitialSnapshotSha256);
        WriteNullable(writer, "seedLedgerSha256", comparison.SeedLedgerSha256);
        WriteNullable(
            writer,
            "baselineControllerConfigurationSha256",
            comparison.BaselineControllerConfigurationSha256);
        WriteNullable(
            writer,
            "candidateControllerConfigurationSha256",
            comparison.CandidateControllerConfigurationSha256);
        WriteAcceptedActions(writer, "baselineAcceptedActions", comparison.BaselineAcceptedActions);
        WriteAcceptedActions(writer, "candidateAcceptedActions", comparison.CandidateAcceptedActions);
        if (comparison.FirstDivergence is null) writer.WriteNull("firstDivergence");
        else WriteDivergence(writer, comparison.FirstDivergence);
        WriteNullable(writer, "acceptedStepCountDelta", comparison.AcceptedStepCountDelta);
        WriteNullable(writer, "terminalOutcomeEqual", comparison.TerminalOutcomeEqual);
        WriteNullable(writer, "failureCategoryEqual", comparison.FailureCategoryEqual);
        writer.WriteEndObject();
    }

    private static PairedManeuverComparison ReadComparison(JsonElement element)
    {
        StrictJson.RequireExactProperties(
            element,
            [
                "pairKey", "repetition", "baselineEntryOrdinal", "candidateEntryOrdinal",
                "status", "creationInputsSha256", "baselineInitialSnapshotSha256",
                "candidateInitialSnapshotSha256",
                "seedLedgerSha256", "baselineControllerConfigurationSha256",
                "candidateControllerConfigurationSha256", "baselineAcceptedActions",
                "candidateAcceptedActions", "firstDivergence",
                "acceptedStepCountDelta", "terminalOutcomeEqual", "failureCategoryEqual",
            ]);
        var divergence = element.GetProperty("firstDivergence");
        return new PairedManeuverComparison(
            RequiredString(element, "pairKey"),
            element.GetProperty("repetition").GetInt32(),
            element.GetProperty("baselineEntryOrdinal").GetInt32(),
            element.GetProperty("candidateEntryOrdinal").GetInt32(),
            ParseComparisonStatus(element.GetProperty("status").GetString()),
            ReadNullableString(element, "creationInputsSha256"),
            ReadNullableString(element, "baselineInitialSnapshotSha256"),
            ReadNullableString(element, "candidateInitialSnapshotSha256"),
            ReadNullableString(element, "seedLedgerSha256"),
            ReadNullableString(element, "baselineControllerConfigurationSha256"),
            ReadNullableString(element, "candidateControllerConfigurationSha256"),
            ReadAcceptedActions(element, "baselineAcceptedActions"),
            ReadAcceptedActions(element, "candidateAcceptedActions"),
            divergence.ValueKind == JsonValueKind.Null ? null : ReadDivergence(divergence),
            ReadNullableInt32(element, "acceptedStepCountDelta"),
            ReadNullableBoolean(element, "terminalOutcomeEqual"),
            ReadNullableBoolean(element, "failureCategoryEqual"));
    }

    private static void WriteAcceptedActions(
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyList<PairedAcceptedActionIdentity>? actions)
    {
        if (actions is null)
        {
            writer.WriteNull(propertyName);
            return;
        }
        writer.WriteStartArray(propertyName);
        foreach (var action in actions)
        {
            writer.WriteStartObject();
            writer.WriteNumber("stepOrdinal", action.StepOrdinal);
            writer.WriteString("audience", Format(action.Audience));
            writer.WriteString("actionId", action.ActionId);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static PairedAcceptedActionIdentity[]? ReadAcceptedActions(
        JsonElement element,
        string propertyName)
    {
        var actions = element.GetProperty(propertyName);
        if (actions.ValueKind == JsonValueKind.Null) return null;
        if (actions.ValueKind != JsonValueKind.Array)
            throw new JsonException($"{propertyName} must be an array or null.");
        return actions.EnumerateArray().Select(action =>
        {
            StrictJson.RequireExactProperties(action, ["stepOrdinal", "audience", "actionId"]);
            return new PairedAcceptedActionIdentity(
                action.GetProperty("stepOrdinal").GetInt32(),
                ParseAudience(action.GetProperty("audience").GetString()),
                RequiredString(action, "actionId"));
        }).ToArray();
    }

    private static void WriteDivergence(
        Utf8JsonWriter writer,
        PairedAcceptedActionDivergence divergence)
    {
        writer.WriteStartObject("firstDivergence");
        writer.WriteString("kind", Format(divergence.Kind));
        WriteNullable(writer, "stepOrdinal", divergence.StepOrdinal);
        WriteNullable(writer, "baselineAudience", divergence.BaselineAudience, Format);
        WriteNullable(writer, "baselineActionId", divergence.BaselineActionId);
        WriteNullable(writer, "candidateAudience", divergence.CandidateAudience, Format);
        WriteNullable(writer, "candidateActionId", divergence.CandidateActionId);
        writer.WriteEndObject();
    }

    private static PairedAcceptedActionDivergence ReadDivergence(JsonElement element)
    {
        StrictJson.RequireExactProperties(
            element,
            [
                "kind", "stepOrdinal", "baselineAudience", "baselineActionId",
                "candidateAudience", "candidateActionId",
            ]);
        return new PairedAcceptedActionDivergence(
            ParseDivergenceKind(element.GetProperty("kind").GetString()),
            ReadNullableInt32(element, "stepOrdinal"),
            ReadNullable(element, "baselineAudience", ParseAudience),
            ReadNullableString(element, "baselineActionId"),
            ReadNullable(element, "candidateAudience", ParseAudience),
            ReadNullableString(element, "candidateActionId"));
    }

    private static ManeuverTerminalCount ReadTerminalCount(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ["kind", "positionId", "victor", "count"]);
        if (!string.Equals(
                element.GetProperty("kind").GetString(),
                "boundary-reached",
                StringComparison.Ordinal)
            || element.GetProperty("victor").ValueKind != JsonValueKind.Null)
            throw new JsonException("Unknown paired Maneuver terminal kind.");
        return new ManeuverTerminalCount(
            new BoundaryReached(RequiredString(element, "positionId")),
            element.GetProperty("count").GetInt32());
    }

    private static ManeuverFailureCount ReadFailureCount(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ["category", "count"]);
        return new ManeuverFailureCount(
            ExerciseContractText.ParseFailure(element.GetProperty("category").GetString()),
            element.GetProperty("count").GetInt32());
    }

    private static ManeuverAggregationFailureCount ReadAggregationFailureCount(
        JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ["category", "count"]);
        return new ManeuverAggregationFailureCount(
            ParseAggregationFailure(element.GetProperty("category").GetString()),
            element.GetProperty("count").GetInt32());
    }

    private static void WriteDiagnostics(
        Utf8JsonWriter writer,
        ManeuverReportDiagnostics diagnostics)
    {
        writer.WriteStartObject("diagnostics");
        writer.WriteNumber("elapsedMicroseconds", diagnostics.ElapsedMicroseconds);
        writer.WriteStartObject("throughput");
        writer.WriteNumber(
            "validatedExerciseCount",
            diagnostics.Throughput.ValidatedExerciseCount);
        writer.WriteNumber("elapsedMicroseconds", diagnostics.Throughput.ElapsedMicroseconds);
        writer.WriteEndObject();
        writer.WriteStartArray("entries");
        foreach (var entry in diagnostics.Entries)
        {
            writer.WriteStartObject();
            writer.WriteNumber("ordinal", entry.Ordinal);
            WriteNullable(writer, "elapsedMicroseconds", entry.ElapsedMicroseconds);
            WriteNullable(writer, "observedBundlePath", entry.ObservedBundlePath);
            WriteNullable(writer, "artifactManifestSha256", entry.ArtifactManifestSha256);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static ManeuverReportDiagnostics ReadDiagnostics(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ["elapsedMicroseconds", "throughput", "entries"]);
        var throughput = element.GetProperty("throughput");
        StrictJson.RequireExactProperties(
            throughput,
            ["validatedExerciseCount", "elapsedMicroseconds"]);
        return new ManeuverReportDiagnostics(
            element.GetProperty("elapsedMicroseconds").GetInt64(),
            new ManeuverThroughput(
                throughput.GetProperty("validatedExerciseCount").GetInt32(),
                throughput.GetProperty("elapsedMicroseconds").GetInt64()),
            ReadArray(element, "entries", ReadDiagnosticEntry));
    }

    private static ManeuverDiagnosticEntry ReadDiagnosticEntry(JsonElement element)
    {
        StrictJson.RequireExactProperties(
            element,
            ["ordinal", "elapsedMicroseconds", "observedBundlePath", "artifactManifestSha256"]);
        return new ManeuverDiagnosticEntry(
            element.GetProperty("ordinal").GetInt32(),
            ReadNullableInt64(element, "elapsedMicroseconds"),
            ReadNullableString(element, "observedBundlePath"),
            ReadNullableString(element, "artifactManifestSha256"));
    }

    private static void WriteOutcomeProperties(
        Utf8JsonWriter writer,
        ExerciseTerminalOutcome outcome)
    {
        if (outcome is not BoundaryReached boundary)
            throw new ArgumentOutOfRangeException(nameof(outcome));
        writer.WriteString("kind", "boundary-reached");
        writer.WriteString("positionId", boundary.PositionId);
        writer.WriteNull("victor");
    }

    private static BoundaryReached ReadOutcome(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ["kind", "positionId", "victor"]);
        if (!string.Equals(
                element.GetProperty("kind").GetString(),
                "boundary-reached",
                StringComparison.Ordinal)
            || element.GetProperty("victor").ValueKind != JsonValueKind.Null)
            throw new JsonException("Unknown paired Maneuver terminal kind.");
        return new BoundaryReached(RequiredString(element, "positionId"));
    }

    private static T[] ReadArray<T>(
        JsonElement parent,
        string propertyName,
        Func<JsonElement, T> read)
    {
        var element = parent.GetProperty(propertyName);
        if (element.ValueKind != JsonValueKind.Array)
            throw new JsonException($"{propertyName} must be an array.");
        return element.EnumerateArray().Select(read).ToArray();
    }

    private static T? ReadNullable<T>(
        JsonElement parent,
        string propertyName,
        Func<string?, T> parse) where T : struct
    {
        var element = parent.GetProperty(propertyName);
        return element.ValueKind == JsonValueKind.Null ? null : parse(element.GetString());
    }

    private static int? ReadNullableInt32(JsonElement parent, string propertyName)
    {
        var element = parent.GetProperty(propertyName);
        return element.ValueKind == JsonValueKind.Null ? null : element.GetInt32();
    }

    private static long? ReadNullableInt64(JsonElement parent, string propertyName)
    {
        var element = parent.GetProperty(propertyName);
        return element.ValueKind == JsonValueKind.Null ? null : element.GetInt64();
    }

    private static bool? ReadNullableBoolean(JsonElement parent, string propertyName)
    {
        var element = parent.GetProperty(propertyName);
        return element.ValueKind == JsonValueKind.Null ? null : element.GetBoolean();
    }

    private static string? ReadNullableString(JsonElement parent, string propertyName)
    {
        var element = parent.GetProperty(propertyName);
        return element.ValueKind == JsonValueKind.Null
            ? null
            : element.GetString()
                ?? throw new JsonException($"{propertyName} must be a string.");
    }

    private static string RequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
            ?? throw new JsonException($"{propertyName} must be a string.");

    private static void WriteNullable<T>(
        Utf8JsonWriter writer,
        string propertyName,
        T? value,
        Func<T, string> format) where T : struct
    {
        if (value.HasValue) writer.WriteString(propertyName, format(value.Value));
        else writer.WriteNull(propertyName);
    }

    private static void WriteNullable(Utf8JsonWriter writer, string propertyName, int? value)
    {
        if (value.HasValue) writer.WriteNumber(propertyName, value.Value);
        else writer.WriteNull(propertyName);
    }

    private static void WriteNullable(Utf8JsonWriter writer, string propertyName, long? value)
    {
        if (value.HasValue) writer.WriteNumber(propertyName, value.Value);
        else writer.WriteNull(propertyName);
    }

    private static void WriteNullable(Utf8JsonWriter writer, string propertyName, bool? value)
    {
        if (value.HasValue) writer.WriteBoolean(propertyName, value.Value);
        else writer.WriteNull(propertyName);
    }

    private static void WriteNullable(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null) writer.WriteNull(propertyName);
        else writer.WriteString(propertyName, value);
    }

    private static string Format(ManeuverReportStatus value) => value switch
    {
        ManeuverReportStatus.Succeeded => "succeeded",
        ManeuverReportStatus.ExerciseFailed => "exercise-failed",
        ManeuverReportStatus.AggregationFailed => "aggregation-failed",
        ManeuverReportStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverReportStatus ParseStatus(string? value) => value switch
    {
        "succeeded" => ManeuverReportStatus.Succeeded,
        "exercise-failed" => ManeuverReportStatus.ExerciseFailed,
        "aggregation-failed" => ManeuverReportStatus.AggregationFailed,
        "cancelled" => ManeuverReportStatus.Cancelled,
        _ => throw new JsonException("Unknown paired Maneuver status."),
    };

    private static string Format(ManeuverEntryStatus value) => value switch
    {
        ManeuverEntryStatus.Succeeded => "succeeded",
        ManeuverEntryStatus.Failed => "failed",
        ManeuverEntryStatus.AggregationFailed => "aggregation-failed",
        ManeuverEntryStatus.NotRun => "not-run",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverEntryStatus ParseEntryStatus(string? value) => value switch
    {
        "succeeded" => ManeuverEntryStatus.Succeeded,
        "failed" => ManeuverEntryStatus.Failed,
        "aggregation-failed" => ManeuverEntryStatus.AggregationFailed,
        "not-run" => ManeuverEntryStatus.NotRun,
        _ => throw new JsonException("Unknown paired Maneuver entry status."),
    };

    private static string Format(ManeuverVariant value) => value switch
    {
        ManeuverVariant.Baseline => "baseline",
        ManeuverVariant.Candidate => "candidate",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverVariant ParseVariant(string? value) => value switch
    {
        "baseline" => ManeuverVariant.Baseline,
        "candidate" => ManeuverVariant.Candidate,
        _ => throw new JsonException("Unknown paired Maneuver variant."),
    };

    private static string Format(PairedComparisonStatus value) => value switch
    {
        PairedComparisonStatus.Compared => "compared",
        PairedComparisonStatus.Incomplete => "incomplete",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static PairedComparisonStatus ParseComparisonStatus(string? value) => value switch
    {
        "compared" => PairedComparisonStatus.Compared,
        "incomplete" => PairedComparisonStatus.Incomplete,
        _ => throw new JsonException("Unknown paired comparison status."),
    };

    private static string Format(PairedDivergenceKind value) => value switch
    {
        PairedDivergenceKind.None => "none",
        PairedDivergenceKind.AcceptedAction => "accepted-action",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static PairedDivergenceKind ParseDivergenceKind(string? value) => value switch
    {
        "none" => PairedDivergenceKind.None,
        "accepted-action" => PairedDivergenceKind.AcceptedAction,
        _ => throw new JsonException("Unknown paired divergence kind."),
    };

    private static string Format(ManeuverAggregationFailureCategory value) => value switch
    {
        ManeuverAggregationFailureCategory.CompletedBundleMissing => "completed-bundle-missing",
        ManeuverAggregationFailureCategory.BundleInvalid => "bundle-invalid",
        ManeuverAggregationFailureCategory.BundleIdentityMismatch => "bundle-identity-mismatch",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverAggregationFailureCategory ParseAggregationFailure(string? value) =>
        value switch
        {
            "completed-bundle-missing" =>
                ManeuverAggregationFailureCategory.CompletedBundleMissing,
            "bundle-invalid" => ManeuverAggregationFailureCategory.BundleInvalid,
            "bundle-identity-mismatch" =>
                ManeuverAggregationFailureCategory.BundleIdentityMismatch,
            _ => throw new JsonException("Unknown aggregation-failure category."),
        };

    private static string Format(ManeuverNotRunReason value) => value switch
    {
        ManeuverNotRunReason.Cancelled => "cancelled",
        ManeuverNotRunReason.AggregationStopped => "aggregation-stopped",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverNotRunReason ParseNotRunReason(string? value) => value switch
    {
        "cancelled" => ManeuverNotRunReason.Cancelled,
        "aggregation-stopped" => ManeuverNotRunReason.AggregationStopped,
        _ => throw new JsonException("Unknown not-run reason."),
    };

    private static string Format(CampaignActionAudience value) => value switch
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
        _ => throw new JsonException("Unknown paired divergence audience."),
    };
}
