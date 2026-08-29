using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

public static class CampaignLegalActionSerializer
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
            writer.WriteString("audience", FormatAudience(actionSet.Audience));
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

    /// <summary>
    /// Reads canonical action-set data without authorizing or executing any candidate.
    /// </summary>
    public static CampaignLegalActionSet DeserializeCanonical(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 32,
                });
            var root = document.RootElement;
            RequireProperties(
                root,
                "contractVersion",
                "policyId",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "positionId",
                "audience",
                "candidates");
            var set = new CampaignLegalActionSet(
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("positionId").GetString()!,
                ParseAudience(root.GetProperty("audience").GetString()),
                root.GetProperty("candidates").EnumerateArray().Select(ParseCandidate).ToArray());
            if (root.GetProperty("contractVersion").GetInt32()
                    != CampaignLegalActionSet.CurrentContractVersion
                || root.GetProperty("policyId").GetString()
                    != CampaignLegalActionSet.CurrentPolicyId
                || !utf8Json.SequenceEqual(Serialize(set)))
            {
                throw new JsonException("The legal-action set is not canonical JSON.");
            }

            return set;
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
            throw new JsonException("The legal-action set JSON is invalid.", exception);
        }
    }

    private static void WriteCandidate(Utf8JsonWriter writer, CampaignActionCandidate candidate)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", candidate.ContractVersion);
        writer.WriteString("actionId", candidate.ActionId);
        writer.WriteString("kind", candidate.Kind);

        switch (candidate)
        {
            case DesignateReserveAction designation:
                writer.WriteString("elementId", designation.ElementId);
                break;
            case MoveElementAction move:
                writer.WriteString("elementId", move.ElementId);
                writer.WriteString("originLocationId", move.OriginLocationId);
                writer.WriteString("destinationLocationId", move.DestinationLocationId);
                MovementActionJson.WriteCostBreakdown(writer, move.CostBreakdown);
                break;
            case ActFirstAction:
            case ActLastAction:
                writer.WriteNumber("operationStage", candidate.OperationStage!.Value);
                break;
            case ResolveInitiativeAction:
            case ResolveNoObligationNavalConvoyScheduleAction:
            case ResolveNoObligationTacticalShippingAction:
            case ResolveWeatherAction:
            case ResolveNoObligationOrganizationAction:
            case ResolveNoObligationNavalConvoyArrivalAction:
            case ResolveNoObligationFleetAssignmentAction:
            case ResolveNoObligationFleetRepairAction:
            case CompleteReserveDesignationAction:
            case CompleteMovementSegmentAction:
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
            "resolve-initiative" => ParsePayloadless(candidate, new ResolveInitiativeAction()),
            "resolve-no-obligation-naval-convoy-schedule" => ParsePayloadless(
                candidate,
                new ResolveNoObligationNavalConvoyScheduleAction()),
            "resolve-no-obligation-tactical-shipping" => ParsePayloadless(
                candidate,
                new ResolveNoObligationTacticalShippingAction()),
            "resolve-weather" => ParsePayloadless(candidate, new ResolveWeatherAction()),
            "resolve-no-obligation-organization" => ParsePayloadless(
                candidate,
                new ResolveNoObligationOrganizationAction()),
            "resolve-no-obligation-naval-convoy-arrival" => ParsePayloadless(
                candidate,
                new ResolveNoObligationNavalConvoyArrivalAction()),
            "resolve-no-obligation-fleet-assignment" => ParsePayloadless(
                candidate,
                new ResolveNoObligationFleetAssignmentAction()),
            "resolve-no-obligation-fleet-repair" => ParsePayloadless(
                candidate,
                new ResolveNoObligationFleetRepairAction()),
            "complete-reserve-designation" => ParsePayloadless(
                candidate,
                new CompleteReserveDesignationAction()),
            "complete-movement-segment" => ParsePayloadless(
                candidate,
                new CompleteMovementSegmentAction()),
            "act-first" => ParseStage(candidate, true),
            "act-last" => ParseStage(candidate, false),
            "designate-reserve" => ParseDesignation(candidate),
            "move-element" => ParseMove(candidate),
            _ => throw new JsonException($"Unknown legal-action kind '{kind}'."),
        };
        if (candidate.GetProperty("contractVersion").GetInt32()
                != CampaignActionCandidate.CurrentContractVersion
            || candidate.GetProperty("actionId").GetString() != parsed.ActionId)
        {
            throw new JsonException("The legal-action candidate identity is invalid.");
        }

        return parsed;
    }

    private static CampaignActionCandidate ParsePayloadless(
        JsonElement candidate,
        CampaignActionCandidate parsed)
    {
        RequireProperties(candidate, "contractVersion", "actionId", "kind");
        return parsed;
    }

    private static CampaignActionCandidate ParseStage(JsonElement candidate, bool actFirst)
    {
        RequireProperties(candidate, "contractVersion", "actionId", "kind", "operationStage");
        var operationStage = candidate.GetProperty("operationStage").GetInt32();
        return actFirst ? new ActFirstAction(operationStage) : new ActLastAction(operationStage);
    }

    private static DesignateReserveAction ParseDesignation(JsonElement candidate)
    {
        RequireProperties(candidate, "contractVersion", "actionId", "kind", "elementId");
        return new DesignateReserveAction(candidate.GetProperty("elementId").GetString()!);
    }

    private static MoveElementAction ParseMove(JsonElement candidate)
    {
        RequireProperties(
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

    private static MovementActionCostBreakdown ParseCostBreakdown(JsonElement breakdown)
    {
        RequireProperties(
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

        RequireProperties(adjustment, "routeId", "costKind", "amount");
        return new MovementActionRouteAdjustment(
            adjustment.GetProperty("routeId").GetString()!,
            MovementActionJson.ParseRouteCostKind(
                adjustment.GetProperty("costKind").GetString()),
            ParseAmount(adjustment.GetProperty("amount")));
    }

    private static MovementActionHexsideCost ParseHexsideCost(JsonElement cost)
    {
        RequireProperties(cost, "hexsideId", "direction", "addedCost");
        return new MovementActionHexsideCost(
            cost.GetProperty("hexsideId").GetString()!,
            MovementActionJson.ParseHexsideDirection(cost.GetProperty("direction").GetString()),
            ParseAmount(cost.GetProperty("addedCost")));
    }

    private static CapabilityPointAmount ParseAmount(JsonElement amount) =>
        CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(amount.GetRawText()));

    internal static void RequireProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.EnumerateObject().Select(property => property.Name)
                .SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new JsonException("The legal-action property contract is invalid.");
        }
    }

    internal static string FormatAudience(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.System => "system",
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };

    internal static CampaignActionAudience ParseAudience(string? audience) => audience switch
    {
        "system" => CampaignActionAudience.System,
        "axis" => CampaignActionAudience.Axis,
        "commonwealth" => CampaignActionAudience.Commonwealth,
        _ => throw new JsonException($"Unknown action audience '{audience}'."),
    };
}
