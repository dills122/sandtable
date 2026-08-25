using System.Text.Json;
using Cna.Core.Actions;

namespace Cna.ExerciseRunner.Artifacts;

public sealed record ExerciseAcceptedActionRecord
{
    internal ExerciseAcceptedActionRecord(
        int stepOrdinal,
        string campaignId,
        long priorStateVersion,
        long committedStateVersion,
        string resultingPositionId,
        CampaignActionAudience audience,
        string actionId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stepOrdinal);
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentOutOfRangeException.ThrowIfLessThan(priorStateVersion, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            committedStateVersion,
            checked(priorStateVersion + 1));
        ArgumentException.ThrowIfNullOrWhiteSpace(resultingPositionId);
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ReplayProofValidation.RequireSha256(actionId, nameof(actionId));
        StepOrdinal = stepOrdinal;
        CampaignId = campaignId;
        PriorStateVersion = priorStateVersion;
        CommittedStateVersion = committedStateVersion;
        ResultingPositionId = resultingPositionId;
        Audience = audience;
        ActionId = actionId;
    }

    public int StepOrdinal { get; }
    public string CampaignId { get; }
    public long PriorStateVersion { get; }
    public long CommittedStateVersion { get; }
    public string ResultingPositionId { get; }
    public CampaignActionAudience Audience { get; }
    public string ActionId { get; }
}

public sealed record ExerciseStepEvidenceRecord
{
    internal ExerciseStepEvidenceRecord(
        int stepOrdinal,
        string campaignId,
        long stateVersion,
        string positionId,
        CampaignActionAudience audience,
        string actionId,
        string eventsHash,
        string snapshotHash)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stepOrdinal);
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(positionId);
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ReplayProofValidation.RequireSha256(actionId, nameof(actionId));
        ReplayProofValidation.RequireSha256(eventsHash, nameof(eventsHash));
        ReplayProofValidation.RequireSha256(snapshotHash, nameof(snapshotHash));
        StepOrdinal = stepOrdinal;
        CampaignId = campaignId;
        StateVersion = stateVersion;
        PositionId = positionId;
        Audience = audience;
        ActionId = actionId;
        EventsHash = eventsHash;
        SnapshotHash = snapshotHash;
    }

    public int StepOrdinal { get; }
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string PositionId { get; }
    public CampaignActionAudience Audience { get; }
    public string ActionId { get; }
    public string EventsHash { get; }
    public string SnapshotHash { get; }
}

public sealed class ExerciseCanonicalEventRecord
{
    private readonly byte[] canonicalBytes;

    internal ExerciseCanonicalEventRecord(
        byte[] canonicalBytes,
        string campaignId,
        long stateVersion,
        string fromPositionId,
        string positionId)
    {
        this.canonicalBytes = canonicalBytes.ToArray();
        CampaignId = campaignId;
        StateVersion = stateVersion;
        FromPositionId = fromPositionId;
        PositionId = positionId;
    }

    public byte[] CanonicalBytes => canonicalBytes.ToArray();
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string FromPositionId { get; }
    public string PositionId { get; }
}

public static class ExerciseEvidenceCodec
{
    // The trusted v1 evidence profile is a clean-cut contract over Core's current v6 snapshot.
    private const int CampaignSnapshotContractVersion = 6;

    private static readonly string[] AcceptedActionProperties =
    [
        "contractVersion", "schemeId", "stepOrdinal", "campaignId", "priorStateVersion",
        "committedStateVersion", "resultingPositionId", "audience", "actionId",
    ];

    private static readonly string[] StepEvidenceProperties =
    [
        "contractVersion", "schemeId", "stepOrdinal", "campaignId", "stateVersion",
        "positionId", "audience", "actionId", "eventsHash", "snapshotHash",
    ];

    private static readonly string[] InitiativeEventProperties =
    [
        "contractVersion", "eventType", "campaignId", "stateVersion", "fromPositionId",
        "outcome", "randomAlgorithmId", "randomCursorBefore", "randomCursorAfter",
        "sequencePosition", "sources",
    ];

    private static readonly string[] AdvanceEventProperties =
    [
        "contractVersion", "eventType", "campaignId", "stateVersion", "fromPositionId",
        "sequencePosition", "sources",
    ];

    private static readonly string[] InitiativeOrderEventProperties =
    [
        "contractVersion", "eventType", "campaignId", "stateVersion", "fromPositionId",
        "operationStage", "declaringHolder", "firstSide", "secondSide", "sequencePosition",
        "sources",
    ];

    private static readonly string[] WeatherEventProperties =
    [
        "contractVersion", "eventType", "campaignId", "stateVersion", "fromPositionId",
        "gameTurn", "operationStage", "determiningSide", "season", "firstDie", "secondDie",
        "kind", "scope", "locationDie", "affectedAreas", "fuelWaterReductionSubjectCount",
        "restoredWellCount", "damagedGroundedAircraftCount", "randomCursorAfter",
        "sequencePosition", "sources",
    ];

    public static IReadOnlyList<ExerciseAcceptedActionRecord> DeserializeAcceptedActions(
        ReadOnlyMemory<byte> canonicalJsonLines) =>
        Array.AsReadOnly(ReadRecords(canonicalJsonLines)
            .Select(ReadAcceptedAction)
            .ToArray());

    public static IReadOnlyList<ExerciseCanonicalEventRecord> DeserializeCanonicalEvents(
        ReadOnlyMemory<byte> canonicalJsonLines) =>
        Array.AsReadOnly(ReadRecords(canonicalJsonLines)
            .Select(ReadCanonicalEvent)
            .ToArray());

    public static IReadOnlyList<ExerciseStepEvidenceRecord> DeserializeStepEvidence(
        ReadOnlyMemory<byte> canonicalJsonLines) =>
        Array.AsReadOnly(ReadRecords(canonicalJsonLines)
            .Select(ReadStepEvidence)
            .ToArray());

    internal static byte[] SerializeReceipt(ExerciseAcceptedActionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("campaignId", record.CampaignId);
            writer.WriteNumber("priorStateVersion", record.PriorStateVersion);
            writer.WriteNumber("committedStateVersion", record.CommittedStateVersion);
            writer.WriteString("resultingPositionId", record.ResultingPositionId);
            writer.WriteString("audience", FormatAudience(record.Audience));
            writer.WriteString("actionId", record.ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static ExerciseSnapshotFacts DeserializeSnapshot(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(
                root,
                [
                    "contractVersion", "campaignId", "stateVersion", "rulesetHash", "setup",
                    "world", "initiativeHolder", "operationStageOrders", "operationStageWeather",
                    "randomState", "sequencePosition",
                ]);
            RequireCanonical(root, canonicalJson.Span);
            if (root.GetProperty("contractVersion").GetInt32()
                != CampaignSnapshotContractVersion)
                throw new JsonException("Unknown campaign snapshot contract version.");
            var campaignId = RequireString(root, "campaignId");
            var stateVersion = root.GetProperty("stateVersion").GetInt64();
            if (stateVersion < 1) throw new JsonException("Snapshot state version is invalid.");
            var rulesetHash = RequireString(root, "rulesetHash");
            var position = root.GetProperty("sequencePosition");
            if (position.ValueKind != JsonValueKind.Object)
                throw new JsonException("Snapshot sequence position must be an object.");
            var positionId = RequireString(position, "positionId");
            return new ExerciseSnapshotFacts(
                campaignId,
                stateVersion,
                rulesetHash,
                positionId);
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
            throw new JsonException("The campaign snapshot evidence is invalid.", exception);
        }
    }

    private static ExerciseAcceptedActionRecord ReadAcceptedAction(byte[] canonicalRecord)
    {
        using var document = JsonDocument.Parse(canonicalRecord);
        var root = document.RootElement;
        StrictJson.RequireExactProperties(root, AcceptedActionProperties);
        RequireHeader(root, ArtifactSchema.AcceptedActionsSchemaId);
        var result = new ExerciseAcceptedActionRecord(
            root.GetProperty("stepOrdinal").GetInt32(),
            RequireString(root, "campaignId"),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("committedStateVersion").GetInt64(),
            RequireString(root, "resultingPositionId"),
            ParseAudience(root.GetProperty("audience").GetString()),
            RequireString(root, "actionId"));
        if (!SerializeAcceptedAction(result).AsSpan().SequenceEqual(canonicalRecord))
            throw new JsonException("Accepted-action evidence is not canonically encoded.");
        return result;
    }

    private static ExerciseStepEvidenceRecord ReadStepEvidence(byte[] canonicalRecord)
    {
        using var document = JsonDocument.Parse(canonicalRecord);
        var root = document.RootElement;
        StrictJson.RequireExactProperties(root, StepEvidenceProperties);
        RequireHeader(root, ArtifactSchema.StepEvidenceSchemaId);
        var result = new ExerciseStepEvidenceRecord(
            root.GetProperty("stepOrdinal").GetInt32(),
            RequireString(root, "campaignId"),
            root.GetProperty("stateVersion").GetInt64(),
            RequireString(root, "positionId"),
            ParseAudience(root.GetProperty("audience").GetString()),
            RequireString(root, "actionId"),
            RequireString(root, "eventsHash"),
            RequireString(root, "snapshotHash"));
        if (!SerializeStepEvidence(result).AsSpan().SequenceEqual(canonicalRecord))
            throw new JsonException("Step evidence is not canonically encoded.");
        return result;
    }

    private static ExerciseCanonicalEventRecord ReadCanonicalEvent(byte[] canonicalRecord)
    {
        using var document = JsonDocument.Parse(canonicalRecord);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Canonical event evidence must be an object.");
        var eventType = RequireString(root, "eventType");
        var (expectedVersion, expectedProperties) = eventType switch
        {
            "campaign-created" => throw new JsonException(
                "Creation events are excluded from accepted-step canonical evidence."),
            "initiative-determined" => (2, InitiativeEventProperties),
            "no-obligation-naval-convoy-schedule-resolved"
                or "no-obligation-tactical-shipping-resolved" => (1, AdvanceEventProperties),
            "initiative-order-declared" => (1, InitiativeOrderEventProperties),
            "weather-determined" => (1, WeatherEventProperties),
            _ => throw new JsonException("Unknown canonical campaign event type."),
        };
        StrictJson.RequireExactProperties(root, expectedProperties);
        RequireCanonical(root, canonicalRecord);
        if (root.GetProperty("contractVersion").GetInt32() != expectedVersion)
            throw new JsonException("Unknown campaign event contract version.");
        var campaignId = RequireString(root, "campaignId");
        var stateVersion = root.GetProperty("stateVersion").GetInt64();
        if (stateVersion < 1) throw new JsonException("Event state version is invalid.");
        var position = root.GetProperty("sequencePosition");
        if (position.ValueKind != JsonValueKind.Object)
            throw new JsonException("Event sequence position must be an object.");
        return new ExerciseCanonicalEventRecord(
            canonicalRecord,
            campaignId,
            stateVersion,
            RequireString(root, "fromPositionId"),
            RequireString(position, "positionId"));
    }

    private static byte[] SerializeAcceptedAction(ExerciseAcceptedActionRecord record)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("schemeId", ArtifactSchema.AcceptedActionsSchemaId);
            writer.WriteNumber("stepOrdinal", record.StepOrdinal);
            writer.WriteString("campaignId", record.CampaignId);
            writer.WriteNumber("priorStateVersion", record.PriorStateVersion);
            writer.WriteNumber("committedStateVersion", record.CommittedStateVersion);
            writer.WriteString("resultingPositionId", record.ResultingPositionId);
            writer.WriteString("audience", FormatAudience(record.Audience));
            writer.WriteString("actionId", record.ActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static byte[] SerializeStepEvidence(ExerciseStepEvidenceRecord record)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("schemeId", ArtifactSchema.StepEvidenceSchemaId);
            writer.WriteNumber("stepOrdinal", record.StepOrdinal);
            writer.WriteString("campaignId", record.CampaignId);
            writer.WriteNumber("stateVersion", record.StateVersion);
            writer.WriteString("positionId", record.PositionId);
            writer.WriteString("audience", FormatAudience(record.Audience));
            writer.WriteString("actionId", record.ActionId);
            writer.WriteString("eventsHash", record.EventsHash);
            writer.WriteString("snapshotHash", record.SnapshotHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static List<byte[]> ReadRecords(ReadOnlyMemory<byte> jsonLines)
    {
        if (jsonLines.IsEmpty) return [];
        var span = jsonLines.Span;
        if (span[^1] != (byte)'\n')
            throw new JsonException("JSONL must end every record with LF.");
        var records = new List<byte[]>();
        var start = 0;
        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] != (byte)'\n') continue;
            var length = index - start;
            if (length == 0 || span[index - 1] == (byte)'\r')
                throw new JsonException("JSONL records must be nonempty and LF-framed.");
            records.Add(span.Slice(start, length).ToArray());
            start = index + 1;
        }
        return records;
    }

    private static void RequireHeader(JsonElement root, string schemeId)
    {
        if (root.GetProperty("contractVersion").GetInt32() != 1)
            throw new JsonException("Unknown evidence contract version.");
        if (!string.Equals(
                root.GetProperty("schemeId").GetString(),
                schemeId,
                StringComparison.Ordinal))
            throw new JsonException("Unknown evidence scheme.");
    }

    private static void RequireCanonical(JsonElement root, ReadOnlySpan<byte> canonicalJson)
    {
        RequireNoDuplicateProperties(root);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) root.WriteTo(writer);
        if (!stream.GetBuffer().AsSpan(0, checked((int)stream.Length)).SequenceEqual(canonicalJson))
            throw new JsonException("Evidence JSON is not canonically encoded.");
    }

    private static void RequireNoDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new JsonException("Evidence JSON contains duplicate properties.");
                RequireNoDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) RequireNoDuplicateProperties(item);
        }
    }

    private static string RequireString(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName).GetString()
        ?? throw new JsonException($"{propertyName} must be a string.");

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
        _ => throw new JsonException("Unknown evidence audience."),
    };
}

internal sealed record ExerciseSnapshotFacts(
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    string PositionId);
