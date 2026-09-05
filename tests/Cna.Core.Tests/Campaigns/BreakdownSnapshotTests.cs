using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownSnapshotTests
{
    [Fact]
    public void InitialSnapshotRoundTripsAndBindsCompleteSuccessorSet()
    {
        var snapshot = Create();
        var bytes = CampaignSnapshotV11Serializer.Serialize(snapshot);
        Assert.Equal(snapshot, CampaignSnapshotV11Serializer.Deserialize(bytes));
        Assert.True(CampaignSnapshotV11Validator.IsValid(snapshot, BreakdownContentFixture.Artifact(), Scenario()));
        Assert.Throws<JsonException>(() => CampaignSnapshotV10Serializer.Deserialize(bytes));
    }

    [Theory]
    [InlineData("snapshot-version")]
    [InlineData("world-version")]
    [InlineData("sequence-version")]
    [InlineData("ruleset")]
    [InlineData("unknown-root")]
    [InlineData("idle-route")]
    [InlineData("initial-moving")]
    public void RejectsMixedOrImpossibleCanonicalSnapshot(string mutation)
    {
        var root = JsonNode.Parse(CampaignSnapshotV11Serializer.Serialize(Create()))!.AsObject();
        switch (mutation)
        {
            case "snapshot-version": root["contractVersion"] = 10; break;
            case "world-version": root["world"]!["contractVersion"] = 5; break;
            case "sequence-version": root["currentPosition"]!["sequencePosition"]!["contractVersion"] = 3; break;
            case "ruleset": root["rulesetHash"] = Cna1979Ruleset.Manifest.Hash; break;
            case "unknown-root": root["extra"] = false; break;
            case "idle-route": root["breakdownFlow"]!["route"] = null; break;
            case "initial-moving": root["breakdownFlow"] = JsonNode.Parse(CampaignBreakdownCodec.Serialize(new CampaignBreakdownFlow.Moving(Route(Create(true), "axis-infantry")))); break;
        }
        Assert.Throws<JsonException>(() => CampaignSnapshotV11Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())));
    }

    [Fact]
    public void MovementAndPhasingStopRequireExactOwnerRouteAndInputs()
    {
        var prior = Create(true);
        var route = Route(prior, "axis-truck");
        var moving = Copy(prior, new CampaignBreakdownFlow.Moving(route));
        Assert.True(CampaignSnapshotV11Validator.IsValid(moving, BreakdownContentFixture.Artifact(), Scenario()));
        var content = BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements.Single(element => element.ElementId == "axis-truck");
        var ledger = prior.World.Elements.Single(element => element.ElementId == "axis-truck").OperationalState.VehicleBreakdownState!;
        var input = new CampaignBreakdownCheckInput(ledger.CohortId, content.BreakdownVehicleCohort!.VehicleTypeId,
            content.BreakdownVehicleCohort.ProfileId, ledger.WorkingPointCount,
            ledger.CumulativeBreakdownPoints, ledger.SandstormAttributedBreakdownPoints, ledger.HighestEffectiveCheckedBandId);
        var stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, 11, route,
            CampaignBreakdownStopReason.Deliberate, BreakdownWeatherKind.Normal, [input]);
        var stopped = Copy(prior, new CampaignBreakdownFlow.PhasingStop(stop), CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext));
        Assert.True(CampaignSnapshotV11Validator.IsValid(stopped, BreakdownContentFixture.Artifact(), Scenario()));
        Assert.Equal(stopped, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(stopped)));
        Assert.Throws<ArgumentException>(() => Copy(prior, new CampaignBreakdownFlow.PhasingStop(stop)));
        var foreign = Route(prior, "commonwealth-truck");
        Assert.Throws<ArgumentException>(() => Copy(prior, new CampaignBreakdownFlow.Moving(foreign)));
    }

    [Fact]
    public void RehashedRouteWithWrongImmutableCohortMembershipFailsCertification()
    {
        var prior = Create(true);
        var actual = Route(prior, "axis-truck");
        var forged = CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, 10, actual.ElementId,
            actual.RepresentationId, actual.Owner, actual.OriginLocationId, actual.CurrentLocationId, []);
        Assert.False(CampaignSnapshotV11Validator.IsValid(Copy(prior, new CampaignBreakdownFlow.Moving(forged)),
            BreakdownContentFixture.Artifact(), Scenario()));
    }

    internal static ContentScenario Scenario() => BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Scenarios.Single();
    internal static CampaignSnapshotV11 Create(bool movement = false)
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var setup = CampaignSetupSnapshotV6.FromPredecessor(CampaignSetupSnapshot.FromDefinition(Cna1979SetupCatalog.Definitions[0]),
            new CampaignContentV6Selection(artifact.Identity, scenario.ScenarioId));
        var position = Cna1979LandSequenceV4.CreateTurn(1)[0];
        if (movement)
        {
            var source = Cna1979LandSequenceV4.CreateTurn(1).First(value => value.SegmentId == LandSegmentIds.Movement);
            position = new LandSequencePosition(4, source.PositionId, source.GameTurn, source.OperationStage,
                source.StageId, source.PhaseId, source.SegmentId, source.StepId, source.ActorRole, LandSide.Axis, source.Sources);
        }
        return new CampaignSnapshotV11(11, "breakdown-snapshot", movement ? 11 : 1,
            Cna1979BreakdownRuleset.Manifest.Hash, setup, CampaignWorldV6Factory.CreateInitial(artifact, scenario),
            movement ? LandSide.Axis : null,
            movement ? [new CampaignOperationStageOrder(2, 1, 1, LandSide.Axis, LandSide.Commonwealth)] : [], movement ? [NormalWeather()] : [],
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0), CampaignPositionV11.FromSequence(position), null,
            new CampaignBreakdownFlow.Idle());
    }
    internal static CampaignOperationStageWeather NormalWeather()
    {
        var season = Cna1979Weather.GetSeason(1);
        var coordinate = (from first in Enumerable.Range(1, 6)
                          from second in Enumerable.Range(1, 6)
                          where Cna1979Weather.GetKind(season, first * 10 + second) == WeatherKind.Normal
                          select (First: first, Second: second)).First();
        return new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, season, coordinate.First,
            coordinate.Second, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);
    }
    internal static CampaignBreakdownRoute Route(CampaignSnapshotV11 snapshot, string elementId)
    {
        var representation = snapshot.World.Representations.Single(value => value.BoundElementIds.Contains(elementId));
        var element = snapshot.World.Elements.Single(value => value.ElementId == elementId);
        var owner = elementId.StartsWith("axis", StringComparison.Ordinal) ? LandSide.Axis : LandSide.Commonwealth;
        var cohort = element.OperationalState.VehicleBreakdownState;
        return CampaignBreakdownRoute.Create(snapshot.CampaignId, snapshot.RulesetHash, 10, elementId,
            representation.RepresentationId, owner, "center", element.CurrentLocationId, cohort is null ? [] : [cohort.CohortId]);
    }
    internal static CampaignSnapshotV11 Copy(CampaignSnapshotV11 prior, CampaignBreakdownFlow flow,
        CampaignPositionV11? position = null, CampaignReactionWindow? window = null, CampaignWorldSnapshotV6? world = null) =>
        new(11, prior.CampaignId, prior.StateVersion, prior.RulesetHash, prior.Setup, world ?? prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather, prior.RandomState,
            position ?? prior.CurrentPosition, window, flow);
}
