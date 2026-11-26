using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Spike B, D-12/D-13: rollover only. No world mutation or terminal scoring.
internal sealed record CampaignNativeEndOfTurnV55(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int NextGameTurn)
{
    internal const int CurrentContractVersion = 55;
    internal const int LastRolloverTurn = Cna1979Weather.MaxGameTurn - 1;
    internal long StateVersion => checked(PriorStateVersion + 1);

    // V42 has no turn stamp. The caller must derive gameTurn from accepted history.
    internal static CampaignNativeEndOfTurnV55 Create(
        CampaignNativePostCombatPassV42 predecessor, int gameTurn, long contactVersion, int scenarioEndGameTurn)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        if (gameTurn >= Math.Min(scenarioEndGameTurn, Cna1979Weather.MaxGameTurn))
            throw new CampaignRuleNotImplementedException("End of Turn is not implemented at or beyond the terminal turn.");
        if (contactVersion != predecessor.StateVersion)
            throw new InvalidOperationException("End of Turn belongs to the current cut.");
        var positions = Cna1979LandSequenceV4.CreateTurn(gameTurn);
        var position = positions[^1];
        if (position.PhaseId != LandPhaseIds.EndOfTurn || predecessor.NextPositionId != position.PositionId
            || predecessor.PositionId != positions[^2].PositionId)
            throw new InvalidOperationException("End of Turn requires the terminal post-combat pass.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.InitiativeDetermination || next.GameTurn != gameTurn + 1)
            throw new InvalidOperationException("End of Turn must advance to the next turn's initiative determination.");
        return new(CurrentContractVersion, predecessor.CampaignId, predecessor.StateVersion,
            TurnBoundaryCanonical.Hash(predecessor.SerializeCanonical()), position.PositionId,
            next.PositionId, predecessor.WorldHash, gameTurn, next.GameTurn);
    }

    internal byte[] SerializeCanonical()
    {
        if (GameTurn is < 1 or > LastRolloverTurn || NextGameTurn != GameTurn + 1
            || NextGameTurn > Cna1979Weather.MaxGameTurn
            || PositionId != "land.position.end-of-turn" || NextPositionId != "land.position.initiative-determination")
            throw new JsonException("Invalid End of Turn.");
        return TurnBoundaryCanonical.Write(ContractVersion, CurrentContractVersion, "native-end-of-turn",
            CampaignId, PriorStateVersion, PriorEventHash, PositionId, NextPositionId, WorldHash, GameTurn,
            writer => writer.WriteNumber("nextGameTurn", NextGameTurn));
    }

    internal static CampaignNativeEndOfTurnV55 Deserialize(ReadOnlyMemory<byte> bytes) =>
        TurnBoundaryCanonical.Read(bytes, "native-end-of-turn", ["nextGameTurn"], root => new CampaignNativeEndOfTurnV55(
            root.GetProperty("contractVersion").GetInt32(), root.GetProperty("campaignId").GetString()!,
            root.GetProperty("priorStateVersion").GetInt64(), root.GetProperty("priorEventHash").GetString()!,
            root.GetProperty("positionId").GetString()!, root.GetProperty("nextPositionId").GetString()!,
            root.GetProperty("worldHash").GetString()!, root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("nextGameTurn").GetInt32()), value => value.SerializeCanonical());
}

// Shared only by the new boundary contracts; does not change any existing event wire layout.
internal static class TurnBoundaryCanonical
{
    private const int MaxBytes = 8192;

    internal static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static byte[] Write(int version, int expectedVersion, string eventType, string campaignId,
        long priorStateVersion, string priorEventHash, string positionId, string nextPositionId,
        string worldHash, int gameTurn, Action<Utf8JsonWriter> writeFields)
    {
        if (version != expectedVersion || priorStateVersion is < 1 or long.MaxValue
            || !IsHash(priorEventHash) || !IsHash(worldHash) || gameTurn < 1)
            throw new JsonException("Invalid turn-boundary event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", version);
            writer.WriteString("eventType", eventType);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(campaignId, nameof(campaignId)));
            writer.WriteNumber("priorStateVersion", priorStateVersion);
            writer.WriteString("priorEventHash", priorEventHash);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(positionId, nameof(positionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(nextPositionId, nameof(nextPositionId)));
            writer.WriteString("worldHash", worldHash);
            writer.WriteNumber("gameTurn", gameTurn);
            writeFields(writer);
            writer.WriteEndObject();
        }
        if (stream.Length > MaxBytes) throw new JsonException("Invalid event size.");
        return stream.ToArray();
    }

    internal static T Read<T>(ReadOnlyMemory<byte> bytes, string eventType, string[] fields,
        Func<JsonElement, T> parse, Func<T, byte[]> serialize)
    {
        try
        {
            if (bytes.Length is < 1 or > MaxBytes) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, ["contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "positionId", "nextPositionId", "worldHash", "gameTurn", .. fields]);
            if (root.GetProperty("eventType").GetString() != eventType)
                throw new JsonException("Invalid event type.");
            var parsed = parse(root);
            if (!bytes.Span.SequenceEqual(serialize(parsed))) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException or OverflowException)
        {
            throw new JsonException("Invalid turn-boundary event.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
