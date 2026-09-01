using System.Text.Json;
using Cna.Core.Actions;

namespace Cna.Core.Observations;

internal static class CampaignObservationV6Serializer
{
    public static byte[] SerializeCanonical(CampaignObservationV6 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", observation.ContractVersion);
            writer.WriteString("policyId", observation.PolicyId);
            writer.WriteString("campaignId", observation.CampaignId);
            writer.WriteNumber("stateVersion", observation.StateVersion);
            writer.WriteString("rulesetHash", observation.RulesetHash);
            writer.WriteString("scenarioId", observation.ScenarioId);
            writer.WriteString(
                "observer",
                CampaignObservationSerializer.FormatSide(observation.Observer));
            CampaignObservationSerializer.WritePosition(writer, observation.Position);
            CampaignObservationSerializer.WriteWeather(writer, observation.Weather);
            CampaignObservationSerializer.WriteLocations(writer, observation.Locations);
            CampaignObservationSerializer.WriteEdges(writer, observation.Edges);
            CampaignObservationSerializer.WriteOwnElements(writer, observation.OwnElements);
            CampaignObservationSerializer.WriteApparentOpposingPresences(
                writer,
                observation.ApparentOpposingPresences);
            writer.WriteStartArray("apparentEnemyControlledLocationIds");
            foreach (var locationId in observation.ApparentEnemyControlledLocationIds)
            {
                writer.WriteStringValue(locationId);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("movementEndedElementIds");
            foreach (var elementId in observation.MovementEndedElementIds)
            {
                writer.WriteStringValue(elementId);
            }

            writer.WriteEndArray();
            WriteDecisionState(writer, observation.DecisionState);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignObservationV6 DeserializeCanonical(ReadOnlySpan<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(
                canonicalJson.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 32,
                });
            var root = document.RootElement;
            CampaignObservationSerializer.RequireProperties(
                root,
                "contractVersion",
                "policyId",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "scenarioId",
                "observer",
                "position",
                "weather",
                "locations",
                "edges",
                "ownElements",
                "apparentOpposingPresences",
                "apparentEnemyControlledLocationIds",
                "movementEndedElementIds",
                "decisionState");
            var result = new CampaignObservationV6(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("policyId").GetString()!,
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("scenarioId").GetString()!,
                CampaignObservationSerializer.ParseSide(
                    root.GetProperty("observer").GetString()),
                CampaignObservationSerializer.ParsePosition(root.GetProperty("position")),
                CampaignObservationSerializer.ParseWeather(root.GetProperty("weather")),
                CampaignObservationSerializer.ParseLocations(root.GetProperty("locations")),
                CampaignObservationSerializer.ParseEdges(root.GetProperty("edges")),
                CampaignObservationSerializer.ParseOwnElements(root.GetProperty("ownElements")),
                CampaignObservationSerializer.ParseApparentOpposingPresences(
                    root.GetProperty("apparentOpposingPresences")),
                root.GetProperty("apparentEnemyControlledLocationIds")
                    .EnumerateArray().Select(value => value.GetString()!).ToArray(),
                root.GetProperty("movementEndedElementIds")
                    .EnumerateArray().Select(value => value.GetString()!).ToArray(),
                ParseDecisionState(root.GetProperty("decisionState")));
            if (!canonicalJson.SequenceEqual(SerializeCanonical(result)))
            {
                throw new JsonException("The Campaign Observation v6 is not canonical JSON.");
            }

            return result;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or FormatException
            or KeyNotFoundException
            or OverflowException)
        {
            throw new JsonException("The Campaign Observation v6 JSON is invalid.", exception);
        }
    }

    internal static void WriteDecisionState(
        Utf8JsonWriter writer,
        CampaignObservationDecisionState decisionState)
    {
        writer.WriteStartObject("decisionState");
        switch (decisionState)
        {
            case CampaignObservationNormalDecisionState:
                writer.WriteString("kind", "normal");
                break;
            case CampaignObservationPhasingWaitingDecisionState waiting:
                writer.WriteString("kind", "phasing-waiting");
                writer.WriteString("windowId", waiting.WindowId);
                break;
            case CampaignObservationReactingDecisionState reacting:
                writer.WriteString("kind", "reacting");
                writer.WriteString("windowId", reacting.WindowId);
                writer.WriteStartObject("apparentTrigger");
                writer.WriteString(
                    "apparentRepresentationId",
                    reacting.ApparentTrigger.ApparentRepresentationId);
                writer.WriteString(
                    "originLocationId",
                    reacting.ApparentTrigger.OriginLocationId);
                writer.WriteString(
                    "destinationLocationId",
                    reacting.ApparentTrigger.DestinationLocationId);
                writer.WriteEndObject();
                writer.WriteStartArray("ownOpportunities");
                foreach (var opportunity in reacting.OwnOpportunities)
                {
                    WriteOpportunity(writer, opportunity);
                }

                writer.WriteEndArray();
                if (reacting.ActiveParticipant is null)
                {
                    writer.WriteNull("activeParticipant");
                }
                else
                {
                    writer.WritePropertyName("activeParticipant");
                    WriteParticipant(writer, reacting.ActiveParticipant);
                }

                break;
            default:
                throw new JsonException("Unknown Campaign Observation v6 decision state.");
        }

        writer.WriteEndObject();
    }

    internal static CampaignObservationDecisionState ParseDecisionState(JsonElement state)
    {
        var kind = state.GetProperty("kind").GetString();
        return kind switch
        {
            "normal" => ParseNormal(state),
            "phasing-waiting" => ParsePhasing(state),
            "reacting" => ParseReacting(state),
            _ => throw new JsonException(
                $"Unknown Campaign Observation v6 decision kind '{kind}'."),
        };
    }

    private static CampaignObservationNormalDecisionState ParseNormal(JsonElement state)
    {
        CampaignObservationSerializer.RequireProperties(state, "kind");
        return new CampaignObservationNormalDecisionState();
    }

    private static CampaignObservationPhasingWaitingDecisionState ParsePhasing(JsonElement state)
    {
        CampaignObservationSerializer.RequireProperties(state, "kind", "windowId");
        return new CampaignObservationPhasingWaitingDecisionState(
            state.GetProperty("windowId").GetString()!);
    }

    private static CampaignObservationReactingDecisionState ParseReacting(JsonElement state)
    {
        CampaignObservationSerializer.RequireProperties(
            state,
            "kind",
            "windowId",
            "apparentTrigger",
            "ownOpportunities",
            "activeParticipant");
        var trigger = state.GetProperty("apparentTrigger");
        CampaignObservationSerializer.RequireProperties(
            trigger,
            "apparentRepresentationId",
            "originLocationId",
            "destinationLocationId");
        var active = state.GetProperty("activeParticipant");
        return new CampaignObservationReactingDecisionState(
            state.GetProperty("windowId").GetString()!,
            new ObservedApparentReactionTrigger(
                trigger.GetProperty("apparentRepresentationId").GetString()!,
                trigger.GetProperty("originLocationId").GetString()!,
                trigger.GetProperty("destinationLocationId").GetString()!),
            state.GetProperty("ownOpportunities").EnumerateArray()
                .Select(ParseOpportunity).ToArray(),
            active.ValueKind == JsonValueKind.Null
                ? null
                : ParseParticipant(active));
    }

    private static ObservedReactionOpportunity ParseOpportunity(JsonElement value)
    {
        CampaignObservationSerializer.RequireProperties(
            value,
            "opportunityId",
            "moveOptions");
        return new ObservedReactionOpportunity(
            value.GetProperty("opportunityId").GetString()!,
            value.GetProperty("moveOptions").EnumerateArray()
                .Select(ParseMoveOption).ToArray());
    }

    private static ObservedReactionParticipant ParseParticipant(JsonElement value)
    {
        CampaignObservationSerializer.RequireProperties(
            value,
            "opportunityId");
        return new ObservedReactionParticipant(
            value.GetProperty("opportunityId").GetString()!);
    }

    private static void WriteOpportunity(Utf8JsonWriter writer, ObservedReactionOpportunity value)
    {
        writer.WriteStartObject();
        writer.WriteString("opportunityId", value.OpportunityId);
        writer.WriteStartArray("moveOptions");
        foreach (var option in value.MoveOptions)
        {
            writer.WriteStartObject();
            writer.WriteString("originLocationId", option.OriginLocationId);
            writer.WriteString("destinationLocationId", option.DestinationLocationId);
            MovementActionJson.WriteCostBreakdown(writer, option.CostBreakdown);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteParticipant(Utf8JsonWriter writer, ObservedReactionParticipant value)
    {
        writer.WriteStartObject();
        writer.WriteString("opportunityId", value.OpportunityId);
        writer.WriteEndObject();
    }

    private static ObservedReactionMoveOption ParseMoveOption(JsonElement option)
    {
        CampaignObservationSerializer.RequireProperties(
            option,
            "originLocationId",
            "destinationLocationId",
            "costBreakdown");
        return new ObservedReactionMoveOption(
            option.GetProperty("originLocationId").GetString()!,
            option.GetProperty("destinationLocationId").GetString()!,
            CampaignObservationV6LegalActionSerializer.ParseCostBreakdown(
                option.GetProperty("costBreakdown")));
    }
}
