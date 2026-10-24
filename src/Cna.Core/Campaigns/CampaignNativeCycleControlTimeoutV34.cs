using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// System finish projection shares the cycle cursor, never the owner event bytes.
internal sealed record CampaignNativeCycleControlTimeoutV34(
    string CampaignId, long PriorStateVersion, string PriorEventHash,
    int PriorCycleOrdinal, string ResultWorldHash, string NextPositionId,
    string ConfigurationHash, long OpenedAtUnixMilliseconds,
    long DeadlineUnixMilliseconds, long HighWaterUnixMilliseconds,
    long? CancelledAtUnixMilliseconds, string Reason)
    : CampaignNativeCycleControlV28(34, CampaignId, PriorStateVersion,
        PriorEventHash, CampaignNativeCycleControlChoiceV28.Finish,
        PriorCycleOrdinal, ResultWorldHash, NextPositionId, null)
{
    internal new const int CurrentContractVersion = 34;
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";

    internal static CampaignNativeCycleControlTimeoutV34 Create(
        CampaignNativeReserveReleaseStateV27 release, CampaignContentContext context,
        CampaignContactJournal contacts, string configurationHash,
        long opened, long deadline, long highWater, long? cancelledAt, string reason)
    {
        var projection = CampaignNativeCycleControlV28.Create(release, context,
            contacts, CampaignNativeCycleControlChoiceV28.Finish);
        var result = new CampaignNativeCycleControlTimeoutV34(projection.CampaignId,
            projection.PriorStateVersion, projection.PriorEventHash,
            projection.PriorCycleOrdinal, projection.ResultWorldHash,
            projection.NextPositionId, configurationHash, opened, deadline,
            highWater, cancelledAt, reason);
        result.RequireClock();
        return result;
    }

    private void RequireClock()
    {
        if (!IsHash(ConfigurationHash)
            || (Reason == "cycle-control-unavailable"
                ? OpenedAtUnixMilliseconds != -1 || DeadlineUnixMilliseconds != -1
                    || HighWaterUnixMilliseconds != -1
                : OpenedAtUnixMilliseconds < 0 || DeadlineUnixMilliseconds <= OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds < OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds >= DeadlineUnixMilliseconds)
            || Reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "cycle-control-unavailable")
            || (Reason == "deadline" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < DeadlineUnixMilliseconds))
            || (Reason == "clock-regression" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < 0 || CancelledAtUnixMilliseconds >= HighWaterUnixMilliseconds))
            || ((Reason is "clock-unavailable" or "controller-unavailable" or "cycle-control-unavailable")
                && CancelledAtUnixMilliseconds is not null))
            throw new JsonException("Cycle timeout clock and reason differ.");
    }

    internal override byte[] SerializeCanonical()
    {
        RequireClock();
        if (ContractVersion != CurrentContractVersion || Choice != CampaignNativeCycleControlChoiceV28.Finish
            || MovementWitnessActionId is not null || PriorStateVersion < 1 || PriorCycleOrdinal < 1
            || !IsHash(PriorEventHash) || !IsHash(ResultWorldHash))
            throw new JsonException("Invalid cycle timeout authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("eventType", "movement-combat-cycle-timeout");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteNumber("priorCycleOrdinal", PriorCycleOrdinal);
            writer.WriteString("resultWorldHash", ResultWorldHash);
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("fallback", "finish-phase");
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

    internal new static CampaignNativeCycleControlTimeoutV34 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Cycle timeout exceeds byte bound.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var hasTime = root.TryGetProperty("cancelledAtUnixMilliseconds", out var time);
            var properties = new List<string> { "contractVersion", "eventType", "campaignId", "priorStateVersion",
                "priorEventHash", "priorCycleOrdinal", "resultWorldHash", "nextPositionId", "fallback",
                "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds", "highWaterUnixMilliseconds" };
            if (hasTime) properties.Add("cancelledAtUnixMilliseconds");
            properties.Add("reason");
            CampaignSnapshotSerializer.RequireProperties(root, properties.ToArray());
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion
                || root.GetProperty("eventType").GetString() != "movement-combat-cycle-timeout"
                || root.GetProperty("fallback").GetString() != "finish-phase")
                throw new JsonException("Unknown cycle timeout event.");
            var result = new CampaignNativeCycleControlTimeoutV34(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(), root.GetProperty("priorEventHash").GetString()!,
                root.GetProperty("priorCycleOrdinal").GetInt32(), root.GetProperty("resultWorldHash").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("configurationHash").GetString()!,
                root.GetProperty("openedAtUnixMilliseconds").GetInt64(), root.GetProperty("deadlineUnixMilliseconds").GetInt64(),
                root.GetProperty("highWaterUnixMilliseconds").GetInt64(), hasTime ? time.GetInt64() : null,
                root.GetProperty("reason").GetString()!);
            if (!bytes.Span.SequenceEqual(result.SerializeCanonical())) throw new JsonException("Noncanonical cycle timeout.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid cycle timeout.", exception); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
