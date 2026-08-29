using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignMovementActionContractTests
{
    private const string MoveSemantics =
        "{\"contractVersion\":1,\"kind\":\"move-element\",\"elementId\":\"axis-element-a\"," +
        "\"originLocationId\":\"west\",\"destinationLocationId\":\"center\"," +
        "\"costBreakdown\":{\"destinationTerrainId\":\"land.terrain.desert\"," +
        "\"destinationTerrainCost\":{\"numerator\":2,\"denominator\":1}," +
        "\"routeAdjustment\":{\"routeId\":\"land.route.track\"," +
        "\"costKind\":\"scale-underlying\",\"amount\":{\"numerator\":1,\"denominator\":2}}," +
        "\"crossedHexsideCosts\":[{\"hexsideId\":\"land.hexside.ridge\"," +
        "\"direction\":\"either\",\"addedCost\":{\"numerator\":1,\"denominator\":1}}," +
        "{\"hexsideId\":\"land.hexside.slope\",\"direction\":\"up\"," +
        "\"addedCost\":{\"numerator\":1,\"denominator\":2}}]," +
        "\"totalCost\":{\"numerator\":5,\"denominator\":2}}}";

    [Fact]
    public void MovementCandidatesHaveFrozenTypedSemanticsAndIds()
    {
        var move = CreateMove();
        var completion = new CompleteMovementSegmentAction();

        Assert.Equal(CampaignActionCandidate.CurrentContractVersion, move.ContractVersion);
        Assert.Equal("move-element", move.Kind);
        Assert.Equal("axis-element-a", move.ElementId);
        Assert.Equal("west", move.OriginLocationId);
        Assert.Equal("center", move.DestinationLocationId);
        Assert.Null(move.OperationStage);
        Assert.Equal("land.terrain.desert", move.CostBreakdown.DestinationTerrainId);
        Assert.Equal(new CapabilityPointAmount(2, 1),
            move.CostBreakdown.DestinationTerrainCost);
        Assert.Equal("land.route.track", move.CostBreakdown.RouteAdjustment!.RouteId);
        Assert.Equal(MovementRouteCostKind.ScaleUnderlying,
            move.CostBreakdown.RouteAdjustment.CostKind);
        Assert.Equal(new CapabilityPointAmount(1, 2),
            move.CostBreakdown.RouteAdjustment.Amount);
        Assert.Equal(
            ["land.hexside.ridge", "land.hexside.slope"],
            move.CostBreakdown.CrossedHexsideCosts.Select(value => value.HexsideId));
        Assert.Equal(new CapabilityPointAmount(5, 2), move.CostBreakdown.TotalCost);
        Assert.Equal(Hash(MoveSemantics), move.ActionId);
        Assert.Equal(
            "sha256:d2b8b443b6bb9862e4f2974748540b077db73debb267faa2b93771023c590070",
            move.ActionId);

        Assert.Equal("complete-movement-segment", completion.Kind);
        Assert.Null(completion.OperationStage);
        Assert.Equal(
            "sha256:054322426e5956a4340cca2ef3d0e9f3388848e998319ae8084a6e708a465bc9",
            completion.ActionId);
        Assert.Equal(
            Hash("{\"contractVersion\":1,\"kind\":\"complete-movement-segment\"}"),
            completion.ActionId);
    }

    [Fact]
    public void MovementCostBreakdownIsImmutableOrderedAndStructurallyEqual()
    {
        var crossed = new List<MovementActionHexsideCost>
        {
            new("land.hexside.ridge", MovementHexsideDirection.Either,
                new CapabilityPointAmount(1, 1)),
            new("land.hexside.slope", MovementHexsideDirection.Up,
                new CapabilityPointAmount(1, 2)),
        };
        var first = CreateBreakdown(crossed);
        var equivalent = CreateBreakdown(crossed.ToArray());

        crossed.Clear();

        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(2, first.CrossedHexsideCosts.Count);
        Assert.IsAssignableFrom<IReadOnlyList<MovementActionHexsideCost>>(
            first.CrossedHexsideCosts);
        Assert.Equal(MovementHexsideDirection.Either,
            first.CrossedHexsideCosts[0].Direction);
        Assert.Equal(MovementHexsideDirection.Up,
            first.CrossedHexsideCosts[1].Direction);
        Assert.Equal(CreateMove(), CreateMove());
        Assert.Equal(CreateMove().GetHashCode(), CreateMove().GetHashCode());
    }

    [Fact]
    public void MovementContractsRejectUnstableAndLocallyIncoherentValues()
    {
        Assert.Throws<ArgumentException>(() => new MoveElementAction(
            "bad id",
            "west",
            "center",
            CreateBreakdown(CreateHexsideCosts())));
        Assert.Throws<ArgumentException>(() => new MovementActionRouteAdjustment(
            "bad id",
            MovementRouteCostKind.Override,
            new CapabilityPointAmount(1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MovementActionHexsideCost(
            "land.hexside.ridge",
            (MovementHexsideDirection)99,
            new CapabilityPointAmount(1, 1)));
        Assert.Throws<ArgumentException>(() => new MovementActionCostBreakdown(
            "land.terrain.desert",
            new CapabilityPointAmount(2, 1),
            new MovementActionRouteAdjustment(
                "land.route.track",
                MovementRouteCostKind.ScaleUnderlying,
                new CapabilityPointAmount(1, 2)),
            CreateHexsideCosts(),
            new CapabilityPointAmount(7, 2)));
        Assert.Throws<ArgumentException>(() => new MovementActionCostBreakdown(
            "land.terrain.desert",
            new CapabilityPointAmount(2, 1),
            null,
            [
                new MovementActionHexsideCost(
                    "land.hexside.ridge",
                    MovementHexsideDirection.Either,
                    new CapabilityPointAmount(1, 1)),
                new MovementActionHexsideCost(
                    "land.hexside.ridge",
                    MovementHexsideDirection.Either,
                    new CapabilityPointAmount(1, 1)),
            ],
            new CapabilityPointAmount(4, 1)));
        Assert.Throws<ArgumentException>(() => new MovementActionCostBreakdown(
            "land.terrain.desert",
            new CapabilityPointAmount(2, 1),
            null,
            [
                new MovementActionHexsideCost(
                    "land.hexside.slope",
                    MovementHexsideDirection.Up,
                    new CapabilityPointAmount(1, 2)),
                new MovementActionHexsideCost(
                    "land.hexside.slope",
                    MovementHexsideDirection.Down,
                    new CapabilityPointAmount(1, 2)),
            ],
            new CapabilityPointAmount(3, 1)));
    }

    [Fact]
    public void MovementCandidatesAndCostValuesAreClosedOutputOnlyContracts()
    {
        Type[] types =
        [
            typeof(MoveElementAction),
            typeof(CompleteMovementSegmentAction),
            typeof(MovementActionCostBreakdown),
            typeof(MovementActionRouteAdjustment),
            typeof(MovementActionHexsideCost),
        ];

        Assert.All(types, type =>
        {
            Assert.True(type.IsPublic);
            Assert.True(type.IsSealed);
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        });
        Assert.Equal(
            [nameof(MoveElementAction.ElementId), nameof(MoveElementAction.OriginLocationId),
                nameof(MoveElementAction.DestinationLocationId),
                nameof(MoveElementAction.CostBreakdown)],
            DeclaredPropertyNames<MoveElementAction>());
        Assert.Empty(DeclaredPropertyNames<CompleteMovementSegmentAction>());
        Assert.Equal(
            [nameof(MovementActionCostBreakdown.DestinationTerrainId),
                nameof(MovementActionCostBreakdown.DestinationTerrainCost),
                nameof(MovementActionCostBreakdown.RouteAdjustment),
                nameof(MovementActionCostBreakdown.CrossedHexsideCosts),
                nameof(MovementActionCostBreakdown.TotalCost)],
            DeclaredPropertyNames<MovementActionCostBreakdown>());
        Assert.Equal(
            [nameof(MovementActionRouteAdjustment.RouteId),
                nameof(MovementActionRouteAdjustment.CostKind),
                nameof(MovementActionRouteAdjustment.Amount)],
            DeclaredPropertyNames<MovementActionRouteAdjustment>());
        Assert.Equal(
            [nameof(MovementActionHexsideCost.HexsideId),
                nameof(MovementActionHexsideCost.Direction),
                nameof(MovementActionHexsideCost.AddedCost)],
            DeclaredPropertyNames<MovementActionHexsideCost>());

        var declaredPropertyTypes = types
            .SelectMany(type => type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(property => property.PropertyType)
            .ToArray();
        Assert.DoesNotContain(declaredPropertyTypes, type => type == typeof(RuleReference));
        Assert.DoesNotContain(declaredPropertyTypes,
            type => type.Namespace is not null
                && (type.Namespace.Contains("Campaigns", StringComparison.Ordinal)
                    || type.Namespace.Contains("Content", StringComparison.Ordinal)
                    || type.Namespace.Contains("Setups", StringComparison.Ordinal)));
    }

    private static MoveElementAction CreateMove() => new(
        "axis-element-a",
        "west",
        "center",
        CreateBreakdown(CreateHexsideCosts()));

    private static MovementActionCostBreakdown CreateBreakdown(
        IReadOnlyList<MovementActionHexsideCost> crossedHexsideCosts) => new(
        "land.terrain.desert",
        new CapabilityPointAmount(2, 1),
        new MovementActionRouteAdjustment(
            "land.route.track",
            MovementRouteCostKind.ScaleUnderlying,
            new CapabilityPointAmount(1, 2)),
        crossedHexsideCosts,
        new CapabilityPointAmount(5, 2));

    private static MovementActionHexsideCost[] CreateHexsideCosts() =>
    [
        new MovementActionHexsideCost(
            "land.hexside.ridge",
            MovementHexsideDirection.Either,
            new CapabilityPointAmount(1, 1)),
        new MovementActionHexsideCost(
            "land.hexside.slope",
            MovementHexsideDirection.Up,
            new CapabilityPointAmount(1, 2)),
    ];

    private static string[] DeclaredPropertyNames<T>() => typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Select(property => property.Name)
        .ToArray();

    private static string Hash(string semantics) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(semantics)))}";
}
