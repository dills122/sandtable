using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

// An accepted system event seals normal World settlement and pending custody.
// Its hashes are checked by re-deriving the entire projection on cold replay.
internal sealed record CampaignNativeCombatSettlementV24(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string DecisionActionId, string PositionId, string RetreatHash,
    string PaidWorldHash, string SettledWorldHash,
    IReadOnlyList<CampaignNativeCombatSettledWorldV24.PendingCaptiveLot> PendingCaptives)
{
    internal const int CurrentContractVersion = 24;
    internal long StateVersion => checked(PriorStateVersion + 1);
    internal string NextPositionId => PositionId;

    internal static CampaignNativeCombatSettlementV24 Create(
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignNativeCombatAssaultInputs inputs,
        CampaignContentContext context)
    {
        var projection = CampaignNativeCombatSettledWorldV24.Project(
            result, retreat, inputs, context);
        return new(CurrentContractVersion, result.CampaignId, retreat.StateVersion,
            result.DecisionActionId, result.PositionId,
            Hash(retreat.SerializeCanonical()), result.PaidWorldHash,
            CampaignNativeCombatPaidWorldV22.Hash(projection.World),
            projection.PendingCaptives);
    }

    internal static CampaignNativeCombatSettlementV24 Replay(
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignNativeCombatAssaultInputs inputs,
        CampaignContentContext context, ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(result, retreat, inputs, context);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Native Combat settlement differs from sealed World authority.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(RetreatHash) || !IsHash(PaidWorldHash)
            || !IsHash(SettledWorldHash) || PendingCaptives.Count > 2
            || PendingCaptives.Any(value => value.Quantity <= 0))
            throw new JsonException("Invalid native Combat settlement event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-world-settled");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("retreatHash", RetreatHash);
            writer.WriteString("paidWorldHash", PaidWorldHash);
            writer.WriteString("settledWorldHash", SettledWorldHash);
            writer.WriteStartArray("pendingCaptives");
            foreach (var lot in PendingCaptives.OrderBy(value => value.VictimElementId,
                StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("victimElementId", ContentContractGuards.RequireStableId(
                    lot.VictimElementId, nameof(lot.VictimElementId)));
                writer.WriteString("victimComponentId", ContentContractGuards.RequireStableId(
                    lot.VictimComponentId, nameof(lot.VictimComponentId)));
                writer.WriteString("captorElementId", ContentContractGuards.RequireStableId(
                    lot.CaptorElementId, nameof(lot.CaptorElementId)));
                writer.WriteNumber("quantity", lot.Quantity);
                writer.WriteString("originLocationId", ContentContractGuards.RequireStableId(
                    lot.OriginLocationId, nameof(lot.OriginLocationId)));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty)
            throw new ArgumentException("Settlement requires retained retreat authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("settlement");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatSettlementV24 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "retreatHash", "paidWorldHash", "settledWorldHash", "pendingCaptives");
            if (root.GetProperty("eventType").GetString() != "combat-world-settled")
                throw new JsonException("Unknown native Combat settlement event.");
            var lots = root.GetProperty("pendingCaptives").EnumerateArray().Select(value =>
            {
                CampaignSnapshotSerializer.RequireProperties(value, "victimElementId",
                    "victimComponentId", "captorElementId", "quantity", "originLocationId");
                return new CampaignNativeCombatSettledWorldV24.PendingCaptiveLot(
                    value.GetProperty("victimElementId").GetString()!,
                    value.GetProperty("victimComponentId").GetString()!,
                    value.GetProperty("captorElementId").GetString()!,
                    value.GetProperty("quantity").GetInt32(),
                    value.GetProperty("originLocationId").GetString()!);
            }).ToArray();
            var parsed = new CampaignNativeCombatSettlementV24(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("retreatHash").GetString()!,
                root.GetProperty("paidWorldHash").GetString()!,
                root.GetProperty("settledWorldHash").GetString()!,
                Array.AsReadOnly(lots));
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical native Combat settlement event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException
            or NullReferenceException)
        {
            throw new JsonException("Invalid native Combat settlement event.", exception);
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
