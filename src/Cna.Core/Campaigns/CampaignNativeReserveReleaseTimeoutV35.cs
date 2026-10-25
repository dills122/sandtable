using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// System disposition batch, never synthesized owner choice events.
internal sealed record CampaignNativeReserveReleaseTimeoutV35(
    string CampaignId, long PriorStateVersion, string PriorEventHash,
    string ResultWorldHash, string NextPositionId, string ReleaseEventHash, int CycleOrdinal,
    IReadOnlyList<string> ConvertedElementIds, IReadOnlyList<string> RetainedElementIds,
    string ConfigurationHash, long OpenedAtUnixMilliseconds,
    long DeadlineUnixMilliseconds, long HighWaterUnixMilliseconds,
    long? CancelledAtUnixMilliseconds, string Reason)
    : CampaignNativeReserveReleaseEventV27(35, CampaignId, PriorStateVersion,
        PriorEventHash, "completed", null, null, ResultWorldHash, NextPositionId)
{
    internal new const int CurrentContractVersion = 35;
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";

    private void RequireClock()
    {
        if (!IsHash(ConfigurationHash)
            || (Reason == "reserve-release-unavailable"
                ? OpenedAtUnixMilliseconds != -1 || DeadlineUnixMilliseconds != -1
                    || HighWaterUnixMilliseconds != -1
                : OpenedAtUnixMilliseconds < 0 || DeadlineUnixMilliseconds <= OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds < OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds >= DeadlineUnixMilliseconds)
            || Reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "reserve-release-unavailable")
            || (Reason == "deadline" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < DeadlineUnixMilliseconds))
            || (Reason == "clock-regression" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < 0 || CancelledAtUnixMilliseconds >= HighWaterUnixMilliseconds))
            || ((Reason is "clock-unavailable" or "controller-unavailable" or "reserve-release-unavailable")
                && CancelledAtUnixMilliseconds is not null))
            throw new JsonException("Reserve Release timeout clock and reason differ.");
    }

    internal override byte[] SerializeCanonical()
    {
        RequireClock();
        if (ContractVersion != CurrentContractVersion || Kind != "completed" || ElementId is not null
            || Choice is not null || PriorStateVersion < 1 || CycleOrdinal < 1
            || !IsHash(PriorEventHash) || !IsHash(ResultWorldHash) || !IsHash(ReleaseEventHash)
            || ConvertedElementIds.Count + RetainedElementIds.Count is < 1 or > 10000
            || (CycleOrdinal == 1 ? RetainedElementIds.Count != 0 : ConvertedElementIds.Count != 0)
            || !ConvertedElementIds.SequenceEqual(ConvertedElementIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            || !RetainedElementIds.SequenceEqual(RetainedElementIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))
            throw new JsonException("Invalid Reserve Release timeout authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("eventType", "reserve-release-timeout");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("releaseEventHash", ReleaseEventHash);
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            WriteIds(writer, "convertedElementIds", ConvertedElementIds);
            WriteIds(writer, "retainedElementIds", RetainedElementIds);
            writer.WriteString("resultWorldHash", ResultWorldHash);
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("fallback", "convert-unresolved-i-retain-ii");
            writer.WriteString("configurationHash", ConfigurationHash);
            writer.WriteNumber("openedAtUnixMilliseconds", OpenedAtUnixMilliseconds);
            writer.WriteNumber("deadlineUnixMilliseconds", DeadlineUnixMilliseconds);
            writer.WriteNumber("highWaterUnixMilliseconds", HighWaterUnixMilliseconds);
            if (CancelledAtUnixMilliseconds is { } cancelled) writer.WriteNumber("cancelledAtUnixMilliseconds", cancelled);
            writer.WriteString("reason", Reason);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal new static CampaignNativeReserveReleaseTimeoutV35 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 1048576) throw new JsonException("Reserve Release timeout exceeds byte bound.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var hasTime = root.TryGetProperty("cancelledAtUnixMilliseconds", out var time);
            var properties = new List<string> { "contractVersion", "eventType", "campaignId", "priorStateVersion",
                "priorEventHash", "releaseEventHash", "cycleOrdinal", "convertedElementIds", "retainedElementIds", "resultWorldHash", "nextPositionId", "fallback",
                "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds", "highWaterUnixMilliseconds" };
            if (hasTime) properties.Add("cancelledAtUnixMilliseconds");
            properties.Add("reason");
            CampaignSnapshotSerializer.RequireProperties(root, properties.ToArray());
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion
                || root.GetProperty("eventType").GetString() != "reserve-release-timeout"
                || root.GetProperty("fallback").GetString() != "convert-unresolved-i-retain-ii")
                throw new JsonException("Unknown Reserve Release timeout event.");
            var result = new CampaignNativeReserveReleaseTimeoutV35(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(), root.GetProperty("priorEventHash").GetString()!,
                root.GetProperty("resultWorldHash").GetString()!, root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("releaseEventHash").GetString()!, root.GetProperty("cycleOrdinal").GetInt32(),
                root.GetProperty("convertedElementIds").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                root.GetProperty("retainedElementIds").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(), root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64(), hasTime ? time.GetInt64() : null,
                root.GetProperty("reason").GetString()!);
            if (!bytes.Span.SequenceEqual(result.SerializeCanonical())) throw new JsonException("Noncanonical Reserve Release timeout.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid Reserve Release timeout.", exception); }
    }

    private static void WriteIds(Utf8JsonWriter writer, string name, IReadOnlyList<string> ids)
    {
        writer.WriteStartArray(name);
        foreach (var id in ids) writer.WriteStringValue(ContentContractGuards.RequireStableId(id, name));
        writer.WriteEndArray();
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
