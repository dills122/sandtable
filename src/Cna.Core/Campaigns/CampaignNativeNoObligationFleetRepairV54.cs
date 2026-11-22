using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// MTS-TASK-002 slice 3 (2026-11-22): the sixth and terminal native-chain
// preamble event. Its successor is the new stage's first-acting-side
// Reserve Designation (Cna1979LandSequence.AddPlayerPhase's own ordering:
// Reserve Designation is the first step of a side-block). Per the owner's
// ruling on the orders-list sub-question, this event -- not a dedicated
// ExerciseSession field -- is where the chain's resolved
// CampaignOperationStageOrder becomes available to whatever extends
// CampaignNativeSecondReserveV43.Create's predecessor switch to accept it.
internal sealed record CampaignNativeNoObligationFleetRepairV54(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int OperationStage,
    LandSide FirstSide, LandSide SecondSide)
{
    internal const int CurrentContractVersion = 54;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeNoObligationFleetRepairV54 Create(
        CampaignNativeNoObligationFleetAssignmentV53 predecessor, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (contactVersion != predecessor.StateVersion)
            throw new InvalidOperationException("Fleet Repair belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(predecessor.GameTurn)
            .Single(value => value.PositionId == predecessor.NextPositionId);
        if (position.PhaseId != LandPhaseIds.CommonwealthFleet || position.SegmentId != LandSegmentIds.FleetRepair
            || position.ActorRole != LandActorRole.Commonwealth)
            throw new InvalidOperationException("This phase does not admit Fleet Repair.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.ReserveDesignation || next.ActorRole != LandActorRole.FirstActingSide
            || next.GameTurn != position.GameTurn || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Fleet Repair must advance to the first acting side's Reserve Designation.");
        var priorBytes = predecessor.SerializeCanonical();
        return new(CurrentContractVersion, predecessor.CampaignId, predecessor.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)),
            position.PositionId, next.PositionId, predecessor.WorldHash, position.GameTurn, position.OperationStage,
            predecessor.FirstSide, predecessor.SecondSide);
    }

    // The resolved order for this stage, read directly off this terminal
    // event's own fields -- see CampaignNativeInitiativeDeclarationV49.ToOrder
    // for the same pattern at the start of this chain, and the owner's
    // ruling in the design doc for why this is self-contained rather than a
    // dedicated ExerciseSession field.
    internal CampaignOperationStageOrder ToOrder() => new(CampaignOperationStageOrder.CurrentContractVersion,
        GameTurn, OperationStage, FirstSide, SecondSide);

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !IsHash(PriorEventHash)
            || !IsHash(WorldHash) || GameTurn < 1 || OperationStage is < 1 or > 3
            || !Enum.IsDefined(FirstSide) || !Enum.IsDefined(SecondSide) || FirstSide == SecondSide)
            throw new JsonException("Invalid Fleet Repair resolution.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "native-no-obligation-fleet-repair-resolved");
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

    internal static CampaignNativeNoObligationFleetRepairV54 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "positionId", "nextPositionId", "worldHash", "gameTurn",
                "operationStage", "firstSide", "secondSide");
            if (root.GetProperty("eventType").GetString() != "native-no-obligation-fleet-repair-resolved")
                throw new JsonException("Invalid event type.");
            LandSide ParseSide(string property) => root.GetProperty(property).GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeNoObligationFleetRepairV54(root.GetProperty("contractVersion").GetInt32(),
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
            throw new JsonException("Invalid Fleet Repair resolution.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
