using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignNativeCombatCustodyTimeoutV36(
    string CampaignId, long PriorStateVersion, string PositionId, string SettlementHash,
    string LotId, IReadOnlyList<string> Route, string WorldHash, string CustodyStateHash,
    string ConfigurationHash, long OpenedAtUnixMilliseconds, long DeadlineUnixMilliseconds,
    long HighWaterUnixMilliseconds, long? CancelledAtUnixMilliseconds, string Reason)
    : CampaignNativeCombatCustodyDispositionV25(36, CampaignId, PriorStateVersion,
        PositionId, SettlementHash, "leave-unguarded", LotId, Route, WorldHash, CustodyStateHash)
{
    internal new const int CurrentContractVersion = 36;
    internal const string NoTimingConfigurationHash = "sha256:" +
        "0000000000000000000000000000000000000000000000000000000000000000";

    internal static CampaignNativeCombatCustodyTimeoutV36 CreateTimeout(
        CampaignSnapshotV11 prior, CampaignNativeCombatSettlementV24 settlement,
        CampaignWorldSnapshotV6 world, CampaignNativeCombatCustodyCutV25? cut,
        CampaignContentContext context, string configurationHash, long opened,
        long deadline, long highWater, long? cancelledAt, string reason)
    {
        if (cut is null || !cut.Choices.Any(value => value.Kind == "leave-unguarded"))
            throw new InvalidOperationException("Custody timeout requires a source-legal unguarded disposition.");
        var disposition = Create(prior, settlement, world, cut, "leave-unguarded", context);
        var result = new CampaignNativeCombatCustodyTimeoutV36(disposition.CampaignId,
            disposition.PriorStateVersion, disposition.PositionId, disposition.SettlementHash,
            disposition.LotId!, disposition.Route, disposition.WorldHash, disposition.CustodyStateHash,
            configurationHash, opened, deadline, highWater, cancelledAt, reason);
        _ = result.SerializeCanonical();
        return result;
    }

    internal override byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || ChoiceKind != "leave-unguarded"
            || PriorStateVersion < 1 || LotId is null || Route.Count is < 1 or > 16
            || !IsHash(SettlementHash) || !IsHash(WorldHash) || !IsHash(CustodyStateHash)
            || !IsHash(ConfigurationHash)
            || Reason is not ("deadline" or "clock-unavailable" or "clock-regression"
                or "controller-unavailable" or "custody-unavailable")
            || (Reason == "custody-unavailable"
                ? OpenedAtUnixMilliseconds != -1 || DeadlineUnixMilliseconds != -1 || HighWaterUnixMilliseconds != -1
                : OpenedAtUnixMilliseconds < 0 || DeadlineUnixMilliseconds <= OpenedAtUnixMilliseconds
                    || HighWaterUnixMilliseconds < OpenedAtUnixMilliseconds || HighWaterUnixMilliseconds >= DeadlineUnixMilliseconds)
            || (Reason == "deadline" && (CancelledAtUnixMilliseconds is null || CancelledAtUnixMilliseconds < DeadlineUnixMilliseconds))
            || (Reason == "clock-regression" && (CancelledAtUnixMilliseconds is null
                || CancelledAtUnixMilliseconds < 0 || CancelledAtUnixMilliseconds >= HighWaterUnixMilliseconds))
            || ((Reason is "clock-unavailable" or "controller-unavailable" or "custody-unavailable") && CancelledAtUnixMilliseconds is not null))
            throw new JsonException("Invalid native custody timeout.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-custody-timeout");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("settlementHash", SettlementHash);
            writer.WriteString("lotId", ContentContractGuards.RequireStableId(LotId, nameof(LotId)));
            writer.WriteStartArray("route");
            foreach (var location in Route) writer.WriteStringValue(ContentContractGuards.RequireStableId(location, nameof(Route)));
            writer.WriteEndArray();
            writer.WriteString("worldHash", WorldHash);
            writer.WriteString("custodyStateHash", CustodyStateHash);
            writer.WriteString("fallback", "leave-unguarded");
            writer.WriteString("configurationHash", ConfigurationHash);
            writer.WriteNumber("openedAtUnixMilliseconds", OpenedAtUnixMilliseconds);
            writer.WriteNumber("deadlineUnixMilliseconds", DeadlineUnixMilliseconds);
            writer.WriteNumber("highWaterUnixMilliseconds", HighWaterUnixMilliseconds);
            if (CancelledAtUnixMilliseconds is { } observed) writer.WriteNumber("cancelledAtUnixMilliseconds", observed);
            writer.WriteString("reason", Reason);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal new static CampaignNativeCombatCustodyTimeoutV36 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Custody timeout exceeds byte bound.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var hasTime = root.TryGetProperty("cancelledAtUnixMilliseconds", out var time);
            var fields = new List<string> { "contractVersion", "eventType", "campaignId", "priorStateVersion",
                "positionId", "settlementHash", "lotId", "route", "worldHash", "custodyStateHash", "fallback",
                "configurationHash", "openedAtUnixMilliseconds", "deadlineUnixMilliseconds", "highWaterUnixMilliseconds" };
            if (hasTime) fields.Add("cancelledAtUnixMilliseconds");
            fields.Add("reason");
            CampaignSnapshotSerializer.RequireProperties(root, fields.ToArray());
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion
                || root.GetProperty("eventType").GetString() != "combat-custody-timeout"
                || root.GetProperty("fallback").GetString() != "leave-unguarded")
                throw new JsonException("Unknown custody timeout.");
            var result = new CampaignNativeCombatCustodyTimeoutV36(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(), root.GetProperty("positionId").GetString()!,
                root.GetProperty("settlementHash").GetString()!, root.GetProperty("lotId").GetString()!,
                root.GetProperty("route").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                root.GetProperty("worldHash").GetString()!, root.GetProperty("custodyStateHash").GetString()!,
                root.GetProperty("configurationHash").GetString()!, root.GetProperty("openedAtUnixMilliseconds").GetInt64(),
                root.GetProperty("deadlineUnixMilliseconds").GetInt64(), root.GetProperty("highWaterUnixMilliseconds").GetInt64(),
                hasTime ? time.GetInt64() : null, root.GetProperty("reason").GetString()!);
            if (!bytes.Span.SequenceEqual(result.SerializeCanonical())) throw new JsonException("Noncanonical custody timeout.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid custody timeout.", exception); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
