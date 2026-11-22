using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// MTS-TASK-002 slice 2 (2026-11-22): the fourth native-chain preamble event.
// Reuses CampaignV11Preamble.HasDueOrOverdueArrival directly -- the
// scheduled-arrival check itself is already content/scenario-driven and
// stage-generic (it only needs `scenario`, `gameTurn`, `operationStage`),
// not reimplemented here. Mirrors the original's own fail-closed behavior:
// a due or overdue scheduled arrival makes the no-obligation resolution
// illegal, the same as CampaignV11Preamble.Create's ResolveNoObligationNavalConvoyArrivalAction
// case (CampaignV11Preamble.cs:45-48) -- this release's admitted synthetic
// content never schedules one, so the positive-obligation path remains
// genuinely unbuilt, not silently bypassed.
internal sealed record CampaignNativeNoObligationNavalConvoyArrivalV52(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int OperationStage,
    LandSide FirstSide, LandSide SecondSide)
{
    internal const int CurrentContractVersion = 52;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeNoObligationNavalConvoyArrivalV52 Create(
        CampaignNativeNoObligationOrganizationV51 predecessor, CampaignContentContext context, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(context);
        if (contactVersion != predecessor.StateVersion)
            throw new InvalidOperationException("Naval Convoy Arrival belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(predecessor.GameTurn)
            .Single(value => value.PositionId == predecessor.NextPositionId);
        if (position.PhaseId != LandPhaseIds.NavalConvoyArrival || position.SegmentId is not null)
            throw new InvalidOperationException("This phase does not admit Naval Convoy Arrival.");
        if (CampaignV11Preamble.HasDueOrOverdueArrival(context.Scenario, position.GameTurn, position.OperationStage))
            throw new InvalidOperationException("Scheduled arrivals require successor authority.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.CommonwealthFleet || next.SegmentId != LandSegmentIds.FleetAssignment
            || next.GameTurn != position.GameTurn || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Naval Convoy Arrival must advance to Fleet Assignment.");
        var priorBytes = predecessor.SerializeCanonical();
        return new(CurrentContractVersion, predecessor.CampaignId, predecessor.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)),
            position.PositionId, next.PositionId, predecessor.WorldHash, position.GameTurn, position.OperationStage,
            predecessor.FirstSide, predecessor.SecondSide);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !IsHash(PriorEventHash)
            || !IsHash(WorldHash) || GameTurn < 1 || OperationStage is < 1 or > 3
            || !Enum.IsDefined(FirstSide) || !Enum.IsDefined(SecondSide) || FirstSide == SecondSide)
            throw new JsonException("Invalid Naval Convoy Arrival resolution.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "native-no-obligation-naval-convoy-arrival-resolved");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteNumber("gameTurn", GameTurn);
            writer.WriteNumber("operationStage", OperationStage);
            writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(FirstSide));
            writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(SecondSide));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeNoObligationNavalConvoyArrivalV52 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "positionId", "nextPositionId", "worldHash", "gameTurn",
                "operationStage", "firstSide", "secondSide");
            if (root.GetProperty("eventType").GetString() != "native-no-obligation-naval-convoy-arrival-resolved")
                throw new JsonException("Invalid event type.");
            LandSide ParseSide(string property) => root.GetProperty(property).GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeNoObligationNavalConvoyArrivalV52(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!,
                root.GetProperty("gameTurn").GetInt32(), root.GetProperty("operationStage").GetInt32(),
                ParseSide("firstSide"), ParseSide("secondSide"));
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Naval Convoy Arrival resolution.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
