using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Completes the owner's Movement segment without resolving the following Breakdown segment.
internal sealed record CampaignNativeSecondMovementCompletedV45(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    LandSide Side, string PositionId, string NextPositionId, string WorldHash)
{
    internal const int CurrentContractVersion = 45;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeSecondMovementCompletedV45 Create(object predecessor,
        CampaignWorldSnapshotV6 world, CampaignContentContext context,
        IReadOnlyList<CampaignOperationStageOrder> orders, LandSide side, int gameTurn, long contactVersion)
    {
        var (_, _, position) = CampaignNativeSecondMoveV44.Source(predecessor, world, context,
            orders, side, gameTurn, contactVersion);
        var campaignId = predecessor switch
        {
            CampaignNativeSecondReserveV43 reserve => reserve.CampaignId,
            CampaignNativeSecondMoveV44 move => move.CampaignId,
            _ => throw new InvalidOperationException("Movement completion requires an accepted predecessor."),
        };
        var catalogPosition = Cna1979LandSequenceV4.CreateTurn(gameTurn)
            .Single(value => value.PositionId == position.PositionId);
        var next = Cna1979LandSequenceV4.GetNext(catalogPosition);
        // MTS-A (2026-11-22): generalized from a hardcoded SecondActingSide check
        // to "the same acting role Source already validated for `position`" --
        // a side-block's acting role is constant across its own Movement ->
        // Breakdown transition (Cna1979LandSequence.AddPlayerPhase passes one
        // actorRole through the whole block), so this is not a new assumption,
        // just no longer hardcoded to "second" specifically.
        if (next.SegmentId != LandSegmentIds.BreakdownDetermination
            || next.ActorRole != position.ActorRole)
            throw new InvalidOperationException("Movement must lead to the second-side Breakdown segment.");
        return new(CurrentContractVersion, campaignId, contactVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(CampaignCurrentEventSerializer.Serialize(predecessor))),
            side, position.PositionId, next.PositionId, CampaignNativeCombatPaidWorldV22.Hash(world));
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion is < 1 or long.MaxValue
            || !Enum.IsDefined(Side) || !IsHash(PriorEventHash) || !IsHash(WorldHash))
            throw new JsonException("Invalid second Movement completion.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "second-movement-completed");
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

    internal static CampaignNativeSecondMovementCompletedV45 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "side", "positionId", "nextPositionId", "worldHash");
            if (root.GetProperty("eventType").GetString() != "second-movement-completed")
                throw new JsonException("Invalid event type.");
            var side = root.GetProperty("side").GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeSecondMovementCompletedV45(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, side, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid second Movement completion.", error); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
