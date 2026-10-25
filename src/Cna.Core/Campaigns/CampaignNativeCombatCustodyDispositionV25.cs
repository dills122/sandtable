using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// Accepted owner or System disposition after the V24 settlement. Cold replay
// reprojects the World and custody assets before accepting these bytes.
internal record CampaignNativeCombatCustodyDispositionV25(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PositionId, string SettlementHash, string ChoiceKind,
    string? LotId, IReadOnlyList<string> Route,
    string WorldHash, string CustodyStateHash)
{
    internal const int CurrentContractVersion = 25;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeCombatCustodyDispositionV25 Create(
        CampaignSnapshotV11 priorSnapshot,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignWorldSnapshotV6 settledWorld,
        CampaignNativeCombatCustodyCutV25? cut,
        string choiceKind,
        CampaignContentContext context)
    {
        var projection = CampaignNativeCombatCustodyProjectionV25.Project(
            priorSnapshot, settlement, settledWorld, cut, choiceKind, context);
        var choice = cut?.Choices.SingleOrDefault(value => value.Kind == choiceKind);
        if (settlement.PendingCaptives.Count > 0 && choice is null)
            throw new InvalidOperationException("Custody action is not in the owner cut.");
        return new(CurrentContractVersion, settlement.CampaignId,
            settlement.StateVersion, settlement.PositionId,
            Hash(settlement.SerializeCanonical()), choiceKind, cut?.LotId,
            choice?.Route ?? [], CampaignNativeCombatPaidWorldV22.Hash(projection.World),
            Hash(CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                projection.Custody)));
    }

    internal static CampaignNativeCombatCustodyDispositionV25 Replay(
        CampaignSnapshotV11 priorSnapshot,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignWorldSnapshotV6 settledWorld,
        CampaignNativeCombatCustodyCutV25? cut,
        CampaignContentContext context, ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = parsed is CampaignNativeCombatCustodyTimeoutV36 timeout
            ? CampaignNativeCombatCustodyTimeoutV36.CreateTimeout(priorSnapshot, settlement, settledWorld, cut, context,
                timeout.ConfigurationHash, timeout.OpenedAtUnixMilliseconds, timeout.DeadlineUnixMilliseconds,
                timeout.HighWaterUnixMilliseconds, timeout.CancelledAtUnixMilliseconds, timeout.Reason)
            : Create(priorSnapshot, settlement, settledWorld, cut, parsed.ChoiceKind, context);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Custody disposition differs from accepted World and contact authority.");
        return expected;
    }

    internal virtual byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(SettlementHash) || !IsHash(WorldHash)
            || !IsHash(CustodyStateHash) || Route.Count > 16
            || ChoiceKind is not ("not-required" or "relocate-and-guard"
                or "leave-unguarded")
            || (ChoiceKind == "not-required") != (LotId is null)
            || (LotId is null) != (Route.Count == 0))
            throw new JsonException("Invalid native Combat custody event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-custody-disposed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("settlementHash", SettlementHash);
            writer.WriteString("choiceKind", ChoiceKind);
            writer.WriteString("lotId", LotId is null ? null
                : ContentContractGuards.RequireStableId(LotId, nameof(LotId)));
            writer.WriteStartArray("route");
            foreach (var location in Route)
                writer.WriteStringValue(ContentContractGuards.RequireStableId(
                    location, nameof(Route)));
            writer.WriteEndArray();
            writer.WriteString("worldHash", WorldHash);
            writer.WriteString("custodyStateHash", CustodyStateHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty)
            throw new ArgumentException("Custody requires retained V24 authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", PositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("disposition");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatCustodyDispositionV25 Deserialize(
        ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() == 36)
                return CampaignNativeCombatCustodyTimeoutV36.Deserialize(bytes);
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion", "positionId",
                "settlementHash", "choiceKind", "lotId", "route",
                "worldHash", "custodyStateHash");
            if (root.GetProperty("eventType").GetString() != "combat-custody-disposed")
                throw new JsonException("Unknown native Combat custody event.");
            var lot = root.GetProperty("lotId");
            var parsed = new CampaignNativeCombatCustodyDispositionV25(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("settlementHash").GetString()!,
                root.GetProperty("choiceKind").GetString()!,
                lot.ValueKind == JsonValueKind.Null ? null : lot.GetString(),
                Array.AsReadOnly(root.GetProperty("route").EnumerateArray()
                    .Select(value => value.GetString()!).ToArray()),
                root.GetProperty("worldHash").GetString()!,
                root.GetProperty("custodyStateHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical native Combat custody event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException
            or NullReferenceException)
        {
            throw new JsonException("Invalid native Combat custody event.", exception);
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value)
    {
        if (value is not { Length: 71 }
            || !value.StartsWith("sha256:", StringComparison.Ordinal)) return false;
        foreach (var digit in value.AsSpan(7))
            if (digit is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
                return false;
        return true;
    }
}
