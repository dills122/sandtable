using System.Text;
using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSuccessorEventSerializer
{
    public static byte[] Serialize(CampaignSuccessorEvent campaignEvent)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            switch (campaignEvent)
            {
                case CampaignCreatedV9 created:
                    WriteCreated(writer, created);
                    break;
                case ElementMovedV2 moved:
                    WriteMoved(writer, moved);
                    break;
                default:
                    throw new JsonException("The Campaign successor event type is unsupported.");
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSuccessorEvent Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSuccessorEvent result = root.GetProperty("eventType").GetString() switch
            {
                "campaign-created" => ParseCreated(root),
                "element-moved" => ParseMoved(root),
                var value => throw new JsonException(
                    $"Unknown Campaign successor event type '{value}'."),
            };
            if (!canonicalJson.Span.SequenceEqual(Serialize(result)))
            {
                throw new JsonException("The Campaign successor event is not canonical JSON.");
            }

            return result;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or FormatException
            or InvalidOperationException
            or KeyNotFoundException)
        {
            throw new JsonException("The Campaign successor event JSON is invalid.", exception);
        }
    }

    private static void WriteCreated(Utf8JsonWriter writer, CampaignCreatedV9 created)
    {
        ValidateCreated(created);
        writer.WriteNumber("contractVersion", created.ContractVersion);
        writer.WriteString("eventType", "campaign-created");
        writer.WriteString("campaignId", created.CampaignId);
        writer.WriteNumber("stateVersion", created.StateVersion);
        writer.WriteString("rulesetHash", created.RulesetHash);
        CampaignV10CanonicalCodec.WriteSetup(writer, created.Setup);
        CampaignV10CanonicalCodec.WriteWorld(writer, "initialWorld", created.InitialWorld);
        CampaignSnapshotSerializer.WriteRandomState(writer, created.RandomState);
        CampaignV10CanonicalCodec.WritePosition(
            writer,
            "sequencePosition",
            created.SequencePosition);
    }

    private static CampaignCreatedV9 ParseCreated(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "rulesetHash",
            "setup",
            "initialWorld",
            "randomState",
            "sequencePosition");
        var result = new CampaignCreatedV9(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("rulesetHash").GetString()!,
            CampaignV10CanonicalCodec.ParseSetup(root.GetProperty("setup")),
            CampaignV10CanonicalCodec.ParseWorld(root.GetProperty("initialWorld")),
            CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomState")),
            CampaignV10CanonicalCodec.ParsePosition(root.GetProperty("sequencePosition")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The CampaignCreated v9 version is invalid.");
        }

        ValidateCreated(result);
        return result;
    }

    private static void WriteMoved(Utf8JsonWriter writer, ElementMovedV2 moved)
    {
        ValidateMoved(moved);
        writer.WriteNumber("contractVersion", moved.ContractVersion);
        writer.WriteString("eventType", "element-moved");
        writer.WriteString("campaignId", moved.CampaignId);
        writer.WriteNumber("stateVersion", moved.StateVersion);
        writer.WriteNumber("priorStateVersion", moved.PriorStateVersion);
        writer.WriteString("fromPositionId", moved.FromPositionId);
        writer.WriteNumber("gameTurn", moved.GameTurn);
        writer.WriteNumber("operationStage", moved.OperationStage);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(moved.ActingSide));
        writer.WriteString("elementId", moved.ElementId);
        writer.WriteString("representationId", moved.RepresentationId);
        writer.WriteString("originLocationId", moved.OriginLocationId);
        writer.WriteString("destinationLocationId", moved.DestinationLocationId);
        writer.WriteString("mobilityId", moved.MobilityId);
        writer.WriteStartArray("mobilitySources");
        foreach (var source in moved.MobilitySources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        CampaignEventSerializer.WriteMovementCost(writer, moved.Cost);
        writer.WritePropertyName("capabilityPointsExpendedBefore");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedBefore);
        writer.WritePropertyName("capabilityPointsExpendedAfter");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedAfter);
        writer.WriteNumber("cohesionBefore", moved.CohesionBefore);
        writer.WriteNumber("cohesionAfter", moved.CohesionAfter);
        CampaignV10CanonicalCodec.WriteMovementEnded(
            writer,
            moved.MovementEndedAfter,
            "movementEndedAfter");
        CampaignV10CanonicalCodec.WritePosition(
            writer,
            "sequencePosition",
            moved.SequencePosition);
        CampaignV10CanonicalCodec.WriteReactionWindow(
            writer,
            "openedReactionWindow",
            moved.OpenedReactionWindow);
    }

    private static ElementMovedV2 ParseMoved(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "elementId",
            "representationId",
            "originLocationId",
            "destinationLocationId",
            "mobilityId",
            "mobilitySources",
            "cost",
            "capabilityPointsExpendedBefore",
            "capabilityPointsExpendedAfter",
            "cohesionBefore",
            "cohesionAfter",
            "movementEndedAfter",
            "sequencePosition",
            "openedReactionWindow");
        var result = new ElementMovedV2(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("actingSide").GetString()),
            root.GetProperty("elementId").GetString()!,
            root.GetProperty("representationId").GetString()!,
            root.GetProperty("originLocationId").GetString()!,
            root.GetProperty("destinationLocationId").GetString()!,
            root.GetProperty("mobilityId").GetString()!,
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("mobilitySources")),
            CampaignEventSerializer.ParseMovementCost(root.GetProperty("cost")),
            ParseCapabilityPoints(root.GetProperty("capabilityPointsExpendedBefore")),
            ParseCapabilityPoints(root.GetProperty("capabilityPointsExpendedAfter")),
            root.GetProperty("cohesionBefore").GetInt32(),
            root.GetProperty("cohesionAfter").GetInt32(),
            CampaignV10CanonicalCodec.ParseMovementEnded(
                root.GetProperty("movementEndedAfter")),
            CampaignV10CanonicalCodec.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignV10CanonicalCodec.ParseReactionWindow(
                root.GetProperty("openedReactionWindow")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ElementMoved v2 version is invalid.");
        }

        ValidateMoved(result);
        return result;
    }

    private static void ValidateCreated(CampaignCreatedV9 created)
    {
        var expectedPosition = Cna1979LandSequence.CreateTurn(
            created.Setup.InitialGameTurn)[0];
        if (created.ContractVersion != CampaignCreatedV9.CurrentContractVersion
            || created.StateVersion != 1
            || !Cna1979Ruleset.IsCanonicalHash(created.RulesetHash)
            || created.Setup.Content.Pack.SchemaVersion != 5
            || !string.Equals(created.Setup.Content.Pack.FormatId,
                "sandtable.content-json.v4", StringComparison.Ordinal)
            || created.InitialWorld.ContractVersion
                != CampaignWorldSnapshotV5.CurrentContractVersion
            || created.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(created.RandomState.AlgorithmId,
                SandtableRandom.AlgorithmId, StringComparison.Ordinal)
            || created.RandomState.NextByteCursor != 0
            || created.SequencePosition != expectedPosition)
        {
            throw new JsonException("The CampaignCreated v9 contract is invalid.");
        }
    }

    private static void ValidateMoved(ElementMovedV2 moved)
    {
        try
        {
            moved.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException("The ElementMoved v2 contract is invalid.", exception);
        }
    }

    private static CapabilityPointAmount ParseCapabilityPoints(JsonElement value) =>
        CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetRawText()));
}
