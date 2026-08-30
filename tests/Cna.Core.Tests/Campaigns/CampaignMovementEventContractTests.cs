using System.Reflection;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementEventContractTests
{
    [Fact]
    public void InternalCommandAndEventFreezeTheExactImmutableContract()
    {
        Assert.Equal(
            ["ActingSide", "CandidateId", "DestinationLocationId", "ElementId",
                "ExpectedPositionId", "OriginLocationId"],
            DeclaredPropertyNames<MoveElement>());
        Assert.Equal(
            ["ActingSide", "CapabilityPointsExpendedAfter",
                "CapabilityPointsExpendedBefore", "CohesionAfter", "CohesionBefore",
                "Cost", "DestinationLocationId", "ElementId", "FromPositionId",
                "GameTurn", "MobilityId", "MobilitySources", "OperationStage",
                "OriginLocationId", "PriorStateVersion", "RepresentationId",
                "SequencePosition"],
            DeclaredPropertyNames<ElementMoved>());
        Assert.Equal(
            ["CrossedHexsideCosts", "DestinationTerrainCost",
                "DestinationTerrainId", "DestinationTerrainSources", "RouteAdjustment",
                "TotalCost"],
            DeclaredPropertyNames<CampaignMovementCost>());
        Assert.Equal(
            ["Amount", "CostKind", "RouteId", "Sources"],
            DeclaredPropertyNames<CampaignMovementRouteAdjustment>());
        Assert.Equal(
            ["AddedCost", "Direction", "HexsideId", "Sources"],
            DeclaredPropertyNames<CampaignMovementHexsideCost>());

        Assert.All(
            new[]
            {
                typeof(ElementMoved), typeof(CampaignMovementCost),
                typeof(CampaignMovementRouteAdjustment),
                typeof(CampaignMovementHexsideCost),
            },
            type => Assert.All(type.GetProperties(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.DeclaredOnly),
                property => Assert.False(property.SetMethod?.IsPublic ?? false)));
    }

    [Fact]
    public void AcceptedMoveHasOneStrictCanonicalRoundTrip()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var command = CampaignMovementTestData.CommandFor(
            evidence.Snapshot,
            evidence.ActingSide,
            candidate);

        var moved = CampaignMovementEventFactory.Create(
            evidence.Snapshot,
            evidence.Context,
            command);
        var canonical = CampaignEventSerializer.Serialize(moved);
        var parsed = Assert.IsType<ElementMoved>(
            CampaignEventSerializer.Deserialize(canonical));

        Assert.Equal(1, command.ContractVersion);
        Assert.Equal(1, moved.ContractVersion);
        Assert.Equal(moved, parsed);
        Assert.Equal(canonical, CampaignEventSerializer.Serialize(parsed));
        Assert.Equal(new CapabilityPointAmount(1, 2), moved.Cost.TotalCost);
        Assert.Equal(CapabilityPointAmount.Zero,
            moved.CapabilityPointsExpendedBefore);
        Assert.Equal(new CapabilityPointAmount(1, 2),
            moved.CapabilityPointsExpendedAfter);
        Assert.Equal(moved.CohesionBefore, moved.CohesionAfter);
        Assert.Equal(moved.FromPositionId, moved.SequencePosition.PositionId);
        Assert.NotEmpty(moved.MobilitySources);
        Assert.NotEmpty(moved.Cost.DestinationTerrainSources);
        Assert.NotEmpty(moved.Cost.RouteAdjustment!.Sources);

        var json = Encoding.UTF8.GetString(canonical);
        Assert.Contains("\"eventType\":\"element-moved\"", json);
        Assert.Contains("\"representationId\":\"map-representation.0003\"", json);
        Assert.DoesNotContain("costBreakdown", json, StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(json.Replace(
                "\"numerator\":1,\"denominator\":2",
                "\"numerator\":2,\"denominator\":4",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(json.Replace(
                "\"eventType\":\"element-moved\"",
                "\"eventType\":\"element-moved\",\"injected\":true",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(json.Replace(
                $"\"priorStateVersion\":{moved.PriorStateVersion},",
                string.Empty,
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(json.Replace(
                "\"eventType\":\"element-moved\",",
                "\"eventType\":\"element-moved\",\"eventType\":\"element-moved\",",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(json.Replace(
                $"\"eventType\":\"element-moved\",\"campaignId\":\"{moved.CampaignId}\"",
                $"\"campaignId\":\"{moved.CampaignId}\",\"eventType\":\"element-moved\"",
                StringComparison.Ordinal))));
    }

    private static string[] DeclaredPropertyNames<T>() => typeof(T)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Select(property => property.Name)
        .Order(StringComparer.Ordinal)
        .ToArray();
}
