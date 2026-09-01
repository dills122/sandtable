using System.Text;
using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

/// <summary>
/// Strict dormant Observation 6 action-set codec. The active codec intentionally remains closed to
/// these successor-only kinds until coordinated activation.
/// </summary>
internal static class CampaignObservationV6LegalActionSerializer
{
    public static byte[] Serialize(CampaignLegalActionSet actionSet)
    {
        ArgumentNullException.ThrowIfNull(actionSet);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", actionSet.ContractVersion);
            writer.WriteString("policyId", actionSet.PolicyId);
            writer.WriteString("campaignId", actionSet.CampaignId);
            writer.WriteNumber("stateVersion", actionSet.StateVersion);
            writer.WriteString("rulesetHash", actionSet.RulesetHash);
            writer.WriteString("positionId", actionSet.PositionId);
            writer.WriteString(
                "audience",
                CampaignLegalActionSerializer.FormatAudience(actionSet.Audience));
            writer.WriteStartArray("candidates");
            foreach (var candidate in actionSet.Candidates)
            {
                WriteCandidate(writer, candidate);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignLegalActionSet DeserializeCanonical(ReadOnlySpan<byte> canonicalJson)
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
            CampaignLegalActionSerializer.RequireProperties(
                root,
                "contractVersion",
                "policyId",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "positionId",
                "audience",
                "candidates");
            var result = new CampaignLegalActionSet(
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("positionId").GetString()!,
                CampaignLegalActionSerializer.ParseAudience(
                    root.GetProperty("audience").GetString()),
                root.GetProperty("candidates").EnumerateArray()
                    .Select(ParseCandidate)
                    .ToArray());
            if (root.GetProperty("contractVersion").GetInt32()
                    != CampaignLegalActionSet.CurrentContractVersion
                || !string.Equals(
                    root.GetProperty("policyId").GetString(),
                    CampaignLegalActionSet.CurrentPolicyId,
                    StringComparison.Ordinal)
                || !canonicalJson.SequenceEqual(Serialize(result)))
            {
                throw new JsonException(
                    "The Observation 6 legal-action set is not canonical JSON.");
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
            throw new JsonException(
                "The Observation 6 legal-action set JSON is invalid.",
                exception);
        }
    }

    public static CampaignLegalActionSet DeserializeCurrent(
        ReadOnlySpan<byte> canonicalJson,
        CampaignObservationV6 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var parsed = DeserializeCanonical(canonicalJson);
        var expected = parsed.Audience == CampaignActionAudience.System
            ? CampaignObservationV6ActionDerivation.DeriveSystem(observation)
            : CampaignObservationV6ActionDerivation.DerivePlayer(observation);
        if (parsed != expected)
        {
            throw new JsonException(
                "The Observation 6 legal-action set is not the exact current membership.");
        }

        return parsed;
    }

    private static void WriteCandidate(Utf8JsonWriter writer, CampaignActionCandidate candidate)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", candidate.ContractVersion);
        writer.WriteString("actionId", candidate.ActionId);
        writer.WriteString("kind", candidate.Kind);
        switch (candidate)
        {
            case MoveElementAction move:
                WriteMovement(writer, move.ElementId, move.OriginLocationId,
                    move.DestinationLocationId, move.CostBreakdown);
                break;
            case CompleteMovementSegmentAction:
                break;
            case MoveReactingElementAction move:
                writer.WriteString("windowId", move.WindowId);
                writer.WriteString("opportunityId", move.OpportunityId);
                writer.WriteString("originLocationId", move.OriginLocationId);
                writer.WriteString("destinationLocationId", move.DestinationLocationId);
                MovementActionJson.WriteCostBreakdown(writer, move.CostBreakdown);
                break;
            case CompleteReactionParticipantAction complete:
                WriteParticipant(writer, complete.WindowId, complete.OpportunityId);
                break;
            case ReactionWindowAction close:
                writer.WriteString("windowId", close.WindowId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(candidate));
        }

        writer.WriteEndObject();
    }

    private static CampaignActionCandidate ParseCandidate(JsonElement candidate)
    {
        if (candidate.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A legal-action candidate must be an object.");
        }

        var kind = candidate.GetProperty("kind").GetString();
        CampaignActionCandidate parsed = kind switch
        {
            "move-element" => ParseMove(candidate),
            "complete-movement-segment" => ParsePayloadless(
                candidate,
                new CompleteMovementSegmentAction()),
            "move-reacting-element" => ParseReactionMove(candidate),
            "complete-reaction-participant" => ParseParticipant(candidate),
            "decline-reaction-window" => ParseWindow(
                candidate,
                value => new DeclineReactionWindowAction(value)),
            "close-reaction-window-scripted-unavailable" => ParseWindow(
                candidate,
                value => new CloseReactionWindowUnavailableAction(value)),
            "close-reaction-window-timeout" => ParseWindow(
                candidate,
                value => new CloseReactionWindowTimeoutAction(value)),
            "close-reaction-window-no-eligible-reactor" => ParseWindow(
                candidate,
                value => new CloseReactionWindowNoEligibleAction(value)),
            _ => throw new JsonException($"Unknown Observation 6 action kind '{kind}'."),
        };
        if (candidate.GetProperty("contractVersion").GetInt32()
                != CampaignActionCandidate.CurrentContractVersion
            || !string.Equals(
                candidate.GetProperty("actionId").GetString(),
                parsed.ActionId,
                StringComparison.Ordinal))
        {
            throw new JsonException("The legal-action candidate identity is invalid.");
        }

        return parsed;
    }

    private static CampaignActionCandidate ParsePayloadless(
        JsonElement candidate,
        CampaignActionCandidate parsed)
    {
        CampaignLegalActionSerializer.RequireProperties(
            candidate,
            "contractVersion",
            "actionId",
            "kind");
        return parsed;
    }

    private static MoveElementAction ParseMove(JsonElement candidate)
    {
        CampaignLegalActionSerializer.RequireProperties(
            candidate,
            "contractVersion",
            "actionId",
            "kind",
            "elementId",
            "originLocationId",
            "destinationLocationId",
            "costBreakdown");
        return new MoveElementAction(
            candidate.GetProperty("elementId").GetString()!,
            candidate.GetProperty("originLocationId").GetString()!,
            candidate.GetProperty("destinationLocationId").GetString()!,
            ParseCostBreakdown(candidate.GetProperty("costBreakdown")));
    }

    private static MoveReactingElementAction ParseReactionMove(JsonElement candidate)
    {
        CampaignLegalActionSerializer.RequireProperties(
            candidate,
            "contractVersion",
            "actionId",
            "kind",
            "windowId",
            "opportunityId",
            "originLocationId",
            "destinationLocationId",
            "costBreakdown");
        return new MoveReactingElementAction(
            candidate.GetProperty("windowId").GetString()!,
            candidate.GetProperty("opportunityId").GetString()!,
            candidate.GetProperty("originLocationId").GetString()!,
            candidate.GetProperty("destinationLocationId").GetString()!,
            ParseCostBreakdown(candidate.GetProperty("costBreakdown")));
    }

    private static CompleteReactionParticipantAction ParseParticipant(JsonElement candidate)
    {
        CampaignLegalActionSerializer.RequireProperties(
            candidate,
            "contractVersion",
            "actionId",
            "kind",
            "windowId",
            "opportunityId");
        return new CompleteReactionParticipantAction(
            candidate.GetProperty("windowId").GetString()!,
            candidate.GetProperty("opportunityId").GetString()!);
    }

    private static CampaignActionCandidate ParseWindow(
        JsonElement candidate,
        Func<string, CampaignActionCandidate> create)
    {
        CampaignLegalActionSerializer.RequireProperties(
            candidate,
            "contractVersion",
            "actionId",
            "kind",
            "windowId");
        return create(candidate.GetProperty("windowId").GetString()!);
    }

    internal static MovementActionCostBreakdown ParseCostBreakdown(JsonElement breakdown)
    {
        CampaignLegalActionSerializer.RequireProperties(
            breakdown,
            "destinationTerrainId",
            "destinationTerrainCost",
            "routeAdjustment",
            "crossedHexsideCosts",
            "totalCost");
        return new MovementActionCostBreakdown(
            breakdown.GetProperty("destinationTerrainId").GetString()!,
            ParseAmount(breakdown.GetProperty("destinationTerrainCost")),
            ParseRouteAdjustment(breakdown.GetProperty("routeAdjustment")),
            breakdown.GetProperty("crossedHexsideCosts").EnumerateArray()
                .Select(ParseHexsideCost)
                .ToArray(),
            ParseAmount(breakdown.GetProperty("totalCost")));
    }

    private static MovementActionRouteAdjustment? ParseRouteAdjustment(JsonElement adjustment)
    {
        if (adjustment.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        CampaignLegalActionSerializer.RequireProperties(
            adjustment,
            "routeId",
            "costKind",
            "amount");
        return new MovementActionRouteAdjustment(
            adjustment.GetProperty("routeId").GetString()!,
            MovementActionJson.ParseRouteCostKind(
                adjustment.GetProperty("costKind").GetString()),
            ParseAmount(adjustment.GetProperty("amount")));
    }

    private static MovementActionHexsideCost ParseHexsideCost(JsonElement cost)
    {
        CampaignLegalActionSerializer.RequireProperties(
            cost,
            "hexsideId",
            "direction",
            "addedCost");
        return new MovementActionHexsideCost(
            cost.GetProperty("hexsideId").GetString()!,
            MovementActionJson.ParseHexsideDirection(
                cost.GetProperty("direction").GetString()),
            ParseAmount(cost.GetProperty("addedCost")));
    }

    private static CapabilityPointAmount ParseAmount(JsonElement amount) =>
        CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(amount.GetRawText()));

    private static void WriteMovement(
        Utf8JsonWriter writer,
        string elementId,
        string originLocationId,
        string destinationLocationId,
        MovementActionCostBreakdown costBreakdown)
    {
        writer.WriteString("elementId", elementId);
        writer.WriteString("originLocationId", originLocationId);
        writer.WriteString("destinationLocationId", destinationLocationId);
        MovementActionJson.WriteCostBreakdown(writer, costBreakdown);
    }

    private static void WriteParticipant(
        Utf8JsonWriter writer,
        string windowId,
        string opportunityId)
    {
        writer.WriteString("windowId", windowId);
        writer.WriteString("opportunityId", opportunityId);
    }
}
