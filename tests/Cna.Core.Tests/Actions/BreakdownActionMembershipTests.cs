using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Actions;

public sealed class BreakdownActionMembershipTests
{
    [Fact]
    public void NoSurvivingTruckPointsRemovesEveryTruckMove()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var truck = prior.World.Elements.Single(value => value.ElementId == "axis-truck");
        var cohort = BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == truck.ElementId).BreakdownVehicleCohort!;
        var lot = CampaignBrokenVehicleLot.Create(prior.CampaignId, prior.RulesetHash,
            CampaignBreakdownCodec.EmptyHash, LandSide.Axis, cohort.CohortId, cohort.VehicleTypeId,
            12, "west", 10, [new RuleReference("spi-1979-land-rules", "21.41")]);
        var snapshot = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Idle(),
            world: BreakdownWorldTests.WithTruck(prior.World, truck, "west", 0, 12, [lot]));
        var set = CampaignObservationV7ActionDerivation.DerivePlayer(Project(snapshot));
        Assert.DoesNotContain(set.Candidates.OfType<MoveElementAction>(), value => value.ElementId == truck.ElementId);
        Assert.Contains(set.Candidates, value => value is CompleteMovementSegmentAction);
    }

    [Fact]
    public void OwnCombatStackBoundFiltersOnlyDestinationCreatingOversizedStack()
    {
        var source = Project(BreakdownSnapshotTests.Create(true));
        var mover = source.OwnElements.Single(value => value.ElementId == "axis-infantry");
        var occupant = new ObservedOwnElement("axis-other-infantry", "axis-other-formation",
            mover.OrganizationId, mover.BaseCapabilityPointAllowance, "north", mover.ReserveStatus,
            mover.MobilityId, mover.LedgerGameTurn, mover.LedgerOperationStage,
            mover.CapabilityPointsExpended, mover.CohesionLevel, null);
        var blocked = Copy(source, source.OwnElements.Append(occupant));
        Assert.Contains(CampaignObservationV7ActionDerivation.DerivePlayer(source).Candidates.OfType<MoveElementAction>(),
            value => value.ElementId == mover.ElementId && value.DestinationLocationId == "north");
        var actions = CampaignObservationV7ActionDerivation.DerivePlayer(blocked);
        Assert.DoesNotContain(actions.Candidates.OfType<MoveElementAction>(),
            value => value.ElementId == mover.ElementId && value.DestinationLocationId == "north");
        Assert.Contains(actions.Candidates.OfType<MoveElementAction>(), value => value.ElementId == "axis-truck");
    }

    [Fact]
    public void OpenRouteRetainsStopWhenNoFurtherGeometryIsPublished()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var moved = BreakdownMovementTests.Move(prior, "axis-truck", "west", "center");
        var snapshot = CampaignV11MoveProjector.ApplyMovement(prior, moved,
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        var observation = Project(snapshot);
        var stuck = Copy(observation, observation.OwnElements, edges: []);
        var set = CampaignObservationV7ActionDerivation.DerivePlayer(stuck);
        var stop = Assert.IsType<StopElementMovementAction>(Assert.Single(set.Candidates));
        Assert.Equal(CampaignBreakdownLifecycleFactory.CreateStopActionId(snapshot), stop.ActionId);
        Assert.Equal(set, CampaignObservationV7LegalActionSerializer.DeserializeCurrent(
            CampaignLegalActionSerializer.Serialize(set), stuck));
    }

    [Theory]
    [InlineData("missing-stop")]
    [InlineData("policy")]
    [InlineData("rules")]
    [InlineData("audience")]
    [InlineData("state")]
    [InlineData("extra-field")]
    public void CurrentMembershipReaderRejectsChangedEnvelopeOrCapabilities(string mutation)
    {
        var observation = Project(BreakdownResolutionReplayTests.PendingTruck());
        var set = CampaignObservationV7ActionDerivation.DeriveSystem(observation);
        var root = JsonNode.Parse(CampaignLegalActionSerializer.Serialize(set))!;
        switch (mutation)
        {
            case "missing-stop": root["candidates"] = new JsonArray(); break;
            case "policy": root["policyId"] = "sandtable.legal-actions.v2"; break;
            case "rules": root["rulesetHash"] = Cna1979Ruleset.HistoricalManifestV8.Hash; break;
            case "audience": root["audience"] = "axis"; break;
            case "state": root["stateVersion"] = set.StateVersion + 1; break;
            case "extra-field": root["candidates"]![0]!["trustedStopId"] = CampaignBreakdownCodec.EmptyHash; break;
            default: throw new InvalidOperationException(mutation);
        }
        Assert.Throws<JsonException>(() => CampaignObservationV7LegalActionSerializer.DeserializeCurrent(
            Encoding.UTF8.GetBytes(root.ToJsonString()), observation));
    }

    [Fact]
    public void HistoricalAndCurrentActionReadersRemainDisjoint()
    {
        var current = CampaignObservationV7ActionDerivation.DerivePlayer(Project(BreakdownSnapshotTests.Create(true)));
        var historical = new CampaignLegalActionSet(current.CampaignId, current.StateVersion,
            Cna1979Ruleset.HistoricalManifestV8.Hash, current.PositionId, current.Audience,
            [new CompleteMovementSegmentAction()], CampaignLegalActionSet.HistoricalPolicyIdV2);
        var oldBytes = CampaignLegalActionSerializer.SerializeHistoricalV2(historical);
        Assert.Equal(historical, CampaignLegalActionSerializer.DeserializeHistoricalV2(oldBytes));
        Assert.Throws<JsonException>(() => CampaignLegalActionSerializer.DeserializeCanonical(oldBytes));
        Assert.Throws<JsonException>(() => CampaignLegalActionSerializer.DeserializeHistoricalV2(CampaignLegalActionSerializer.Serialize(current)));
        Assert.Equal(CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId(),
            new CompleteBreakdownSegmentAction().ActionId);
    }

    private static CampaignObservationV7 Project(CampaignSnapshotV11 snapshot) =>
        CampaignObservationV7Projector.Project(snapshot, BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario(), LandSide.Axis, new CampaignObservationV6AuthorityFacts([], []));

    private static CampaignObservationV7 Copy(CampaignObservationV7 source,
        IEnumerable<ObservedOwnElement> ownElements, IEnumerable<CampaignObservationEdge>? edges = null) => new(
            source.ContractVersion, source.PolicyId, source.CampaignId, source.StateVersion,
            source.RulesetHash, source.ScenarioId, source.CapabilityProfileId, source.Observer,
            source.Position, source.Weather, source.Locations, edges ?? source.Edges, ownElements,
            source.ApparentOpposingPresences, source.ApparentEnemyControlledLocationIds,
            source.MovementEndedElementIds, source.DecisionState, source.OwnBrokenVehicleLots);
}
