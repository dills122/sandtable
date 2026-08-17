using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public static class CampaignObservationSerializer
{
    public static byte[] SerializeCanonical(CampaignObservation observation)
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
            writer.WriteString("observer", FormatSide(observation.Observer));
            WritePosition(writer, observation.Position);
            WriteLocations(writer, observation.Locations);
            WriteEdges(writer, observation.Edges);
            WriteOwnElements(writer, observation.OwnElements);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static void WritePosition(
        Utf8JsonWriter writer,
        CampaignObservationPosition position)
    {
        writer.WriteStartObject("position");
        writer.WriteString("positionId", position.PositionId);
        writer.WriteNumber("gameTurn", position.GameTurn);
        writer.WriteNumber("operationStage", position.OperationStage);
        writer.WriteString("stageId", position.StageId);
        writer.WriteString("phaseId", position.PhaseId);
        WriteNullableString(writer, "segmentId", position.SegmentId);
        WriteNullableString(writer, "stepId", position.StepId);
        writer.WriteString("actorRole", FormatActorRole(position.ActorRole));
        WriteNullableSide(writer, "activeSide", position.ActiveSide);
        WriteNullableSide(writer, "initiativeHolder", position.InitiativeHolder);
        writer.WriteEndObject();
    }

    private static void WriteLocations(
        Utf8JsonWriter writer,
        IEnumerable<CampaignObservationLocation> locations)
    {
        writer.WriteStartArray("locations");

        foreach (var location in locations)
        {
            writer.WriteStartObject();
            writer.WriteString("locationId", location.LocationId);
            writer.WriteString("terrainId", location.TerrainId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteEdges(
        Utf8JsonWriter writer,
        IEnumerable<CampaignObservationEdge> edges)
    {
        writer.WriteStartArray("edges");

        foreach (var edge in edges)
        {
            writer.WriteStartObject();
            writer.WriteString("firstLocationId", edge.FirstLocationId);
            writer.WriteString("secondLocationId", edge.SecondLocationId);
            writer.WriteStartArray("features");

            foreach (var feature in edge.Features)
            {
                writer.WriteStartObject();
                writer.WriteString("featureId", feature.FeatureId);
                WriteNullableString(
                    writer,
                    "directionFromLocationId",
                    feature.DirectionFromLocationId);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteOwnElements(
        Utf8JsonWriter writer,
        IEnumerable<ObservedOwnElement> ownElements)
    {
        writer.WriteStartArray("ownElements");

        foreach (var element in ownElements)
        {
            writer.WriteStartObject();
            writer.WriteString("elementId", element.ElementId);
            writer.WriteString("parentFormationId", element.ParentFormationId);
            writer.WriteString("organizationId", element.OrganizationId);
            writer.WriteNumber(
                "baseCapabilityPointAllowance",
                element.BaseCapabilityPointAllowance);
            writer.WriteString("currentLocationId", element.CurrentLocationId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static string FormatActorRole(LandActorRole role) => role switch
    {
        LandActorRole.None => "none",
        LandActorRole.Commonwealth => "commonwealth",
        LandActorRole.InitiativeHolder => "initiative-holder",
        LandActorRole.FirstActingSide => "first-acting-side",
        LandActorRole.SecondActingSide => "second-acting-side",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static void WriteNullableSide(
        Utf8JsonWriter writer,
        string propertyName,
        LandSide? side) => WriteNullableString(
            writer,
            propertyName,
            side is null ? null : FormatSide(side.Value));

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }
}
