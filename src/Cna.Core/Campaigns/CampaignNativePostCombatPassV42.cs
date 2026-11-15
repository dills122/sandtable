using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Owner declines this phase's available activity; no absence of resources is inferred.
internal sealed record CampaignNativePostCombatPassV42(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    LandSide Side, string PositionId, string NextPositionId, string WorldHash)
{
    internal const int CurrentContractVersion = 42;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativePostCombatPassV42 Create(object predecessor, LandSide side,
        int gameTurn, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        var (campaignId, version, priorSide, positionId, worldHash, priorBytes) = predecessor switch
        {
            CampaignNativeTruckConvoyPassV41 convoy => (convoy.CampaignId, convoy.StateVersion,
                convoy.Side, convoy.NextPositionId, convoy.WorldHash, convoy.SerializeCanonical()),
            CampaignNativePostCombatPassV42 phase => (phase.CampaignId, phase.StateVersion,
                phase.Side, phase.NextPositionId, phase.WorldHash, phase.SerializeCanonical()),
            _ => throw new InvalidOperationException("Post-combat pass requires an accepted predecessor."),
        };
        if (side != priorSide || contactVersion != version)
            throw new InvalidOperationException("Post-combat pass belongs to the acting side and current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(gameTurn).Single(value => value.PositionId == positionId);
        if (position.ActorRole != LandActorRole.FirstActingSide
            || !(position.PhaseId is LandPhaseIds.CommonwealthRailMovement or LandPhaseIds.Patrol
                || position.PhaseId == LandPhaseIds.Repair
                    && position.SegmentId is LandSegmentIds.Towing or LandSegmentIds.Maintenance))
            throw new InvalidOperationException("This phase does not admit a post-combat pass.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        return new(CurrentContractVersion, campaignId, version,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)), side,
            position.PositionId, next.PositionId, worldHash);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !Enum.IsDefined(Side)
            || !IsHash(PriorEventHash) || !IsHash(WorldHash))
            throw new JsonException("Invalid post-combat pass.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "post-combat-phase-passed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativePostCombatPassV42 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "side", "positionId", "nextPositionId", "worldHash");
            if (root.GetProperty("eventType").GetString() != "post-combat-phase-passed")
                throw new JsonException("Invalid event type.");
            var side = root.GetProperty("side").GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativePostCombatPassV42(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, side, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid post-combat pass.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
