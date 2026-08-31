using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationV6ContractTests
{
    [Fact]
    public void NormalProjectionRoundTripsCanonicalControlledLocations()
    {
        var fixture = CampaignV10TestData.Create();
        var facts = new CampaignObservationV6AuthorityFacts(
            ["west", "east"],
            []);

        var observation = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            facts);
        var canonical = CampaignObservationV6Serializer.SerializeCanonical(observation);
        var roundTripped = CampaignObservationV6Serializer.DeserializeCanonical(canonical);

        Assert.Equal(6, observation.ContractVersion);
        Assert.Equal(
            "sandtable.observation.zoc-reaction-side-safe.v1",
            observation.PolicyId);
        Assert.Equal(["east", "west"], observation.ApparentEnemyControlledLocationIds);
        Assert.IsType<CampaignObservationNormalDecisionState>(observation.DecisionState);
        Assert.Equal(observation, roundTripped);
        Assert.Equal(canonical, CampaignObservationV6Serializer.SerializeCanonical(roundTripped));
        Assert.All(observation.ApparentOpposingPresences, presence =>
            Assert.False(presence.ExertsZoc));
    }

    [Fact]
    public void ControlledLocationChangeAltersOnlyAggregateMembership()
    {
        var fixture = CampaignV10TestData.Create();
        var east = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            Facts(["east"]));
        var west = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            Facts(["west"]));

        Assert.Equal(east.CampaignId, west.CampaignId);
        Assert.Equal(east.StateVersion, west.StateVersion);
        Assert.Equal(east.Observer, west.Observer);
        Assert.Equal(east.Position, west.Position);
        Assert.Equal(east.Weather, west.Weather);
        Assert.Equal(east.Locations, west.Locations);
        Assert.Equal(east.Edges, west.Edges);
        Assert.Equal(east.OwnElements, west.OwnElements);
        Assert.Equal(east.ApparentOpposingPresences, west.ApparentOpposingPresences);
        Assert.Equal(east.DecisionState, west.DecisionState);
        Assert.Equal(["east"], east.ApparentEnemyControlledLocationIds);
        Assert.Equal(["west"], west.ApparentEnemyControlledLocationIds);
        Assert.NotEqual(
            CampaignObservationV6Serializer.SerializeCanonical(east),
            CampaignObservationV6Serializer.SerializeCanonical(west));
        Assert.DoesNotContain(
            typeof(CampaignObservationV6).GetProperties(),
            property => property.Name.Contains("Source", StringComparison.Ordinal)
                || property.Name.Contains("Reason", StringComparison.Ordinal)
                || property.Name.Contains("Mapping", StringComparison.Ordinal));
    }

    [Fact]
    public void ReactionProjectionPublishesOnlyAudienceSpecificDecisionFacts()
    {
        var fixture = CampaignV10TestData.Create();
        var snapshot = ApplyTrigger(fixture, fixture.TriggeringMove);
        var facts = Facts(["east"]);

        var phasing = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            facts);
        var reacting = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var phasingState = Assert.IsType<CampaignObservationPhasingWaitingDecisionState>(
            phasing.DecisionState);
        var reactingState = Assert.IsType<CampaignObservationReactingDecisionState>(
            reacting.DecisionState);
        var opportunity = Assert.Single(reactingState.OwnOpportunities);
        var authorityOpportunity = Assert.Single(snapshot.ReactionWindow!.FrozenOpportunities);
        var phasingJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(phasing));
        var reactingJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(reacting));

        Assert.Equal(snapshot.ReactionWindow.WindowId.Value, phasingState.WindowId);
        Assert.Equal(snapshot.ReactionWindow.WindowId.Value, reactingState.WindowId);
        Assert.Equal(
            snapshot.ReactionWindow.ApparentTrigger.ApparentRepresentationId,
            reactingState.ApparentTrigger.ApparentRepresentationId);
        Assert.Equal(
            snapshot.ReactionWindow.ApparentTrigger.OriginLocationId,
            reactingState.ApparentTrigger.OriginLocationId);
        Assert.Equal(
            snapshot.ReactionWindow.ApparentTrigger.DestinationLocationId,
            reactingState.ApparentTrigger.DestinationLocationId);
        Assert.Equal(authorityOpportunity.OpportunityId.Value, opportunity.OpportunityId);
        Assert.Equal(
            authorityOpportunity.ReactingRepresentation.RepresentationId,
            opportunity.RepresentationId);
        Assert.Null(reactingState.ActiveParticipant);
        Assert.DoesNotContain("frozenOpportunities", phasingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("opportunityId", phasingJson, StringComparison.Ordinal);
        Assert.DoesNotContain(
            snapshot.ReactionWindow.ApparentTrigger.ApparentRepresentationId,
            phasingJson,
            StringComparison.Ordinal);
        Assert.DoesNotContain("boundElementIds", reactingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("adjacencyEvidence", reactingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("sources", reactingJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reason", reactingJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HiddenOpportunityChangesLeavePhasingBytesIdenticalAndConfineReactingDelta()
    {
        var fixture = CampaignV10TestData.Create();
        var withOpportunity = ApplyTrigger(fixture, fixture.TriggeringMove);
        var emptyMove = CampaignV10TestData.CreateTriggeringMove(
            fixture.MovementSnapshot,
            []);
        var withoutOpportunity = ApplyTrigger(fixture, emptyMove);
        var phasingFacts = Facts(["east"]);
        var reactingFacts = Facts(["west"]);

        var phasingWith = Project(withOpportunity, LandSide.Axis, phasingFacts);
        var phasingWithout = Project(withoutOpportunity, LandSide.Axis, phasingFacts);
        var reactingWith = Project(withOpportunity, LandSide.Commonwealth, reactingFacts);
        var reactingWithout = Project(withoutOpportunity, LandSide.Commonwealth, reactingFacts);

        Assert.Equal(
            CampaignObservationV6Serializer.SerializeCanonical(phasingWith),
            CampaignObservationV6Serializer.SerializeCanonical(phasingWithout));
        Assert.NotEqual(
            CampaignObservationV6Serializer.SerializeCanonical(reactingWith),
            CampaignObservationV6Serializer.SerializeCanonical(reactingWithout));
        Assert.Equal(reactingWith.Locations, reactingWithout.Locations);
        Assert.Equal(reactingWith.Edges, reactingWithout.Edges);
        Assert.Equal(reactingWith.OwnElements, reactingWithout.OwnElements);
        Assert.Equal(
            reactingWith.ApparentOpposingPresences,
            reactingWithout.ApparentOpposingPresences);
        Assert.Equal(
            reactingWith.ApparentEnemyControlledLocationIds,
            reactingWithout.ApparentEnemyControlledLocationIds);
        Assert.Single(Assert.IsType<CampaignObservationReactingDecisionState>(
            reactingWith.DecisionState).OwnOpportunities);
        Assert.Empty(Assert.IsType<CampaignObservationReactingDecisionState>(
            reactingWithout.DecisionState).OwnOpportunities);

        CampaignObservationV6 Project(
            CampaignSnapshotV10 snapshot,
            LandSide observer,
            CampaignObservationV6AuthorityFacts facts) =>
            CampaignObservationV6Projector.Project(
                snapshot,
                fixture.Artifact,
                fixture.Scenario,
                observer,
                facts);
    }

    [Fact]
    public void ActiveParticipantProjectsOnlyItsOwnStablePublicIdentity()
    {
        var fixture = CampaignV10TestData.Create();
        var window = fixture.TriggeringMove.OpenedReactionWindow!;
        var opportunity = Assert.Single(window.FrozenOpportunities);
        var activeWindow = new CampaignReactionWindow(
            window.WindowId,
            window.TriggerCommittedStateVersion,
            window.PhasingSide,
            window.ReactingSide,
            window.ReactingPosition,
            window.TriggerAuthority,
            window.ApparentTrigger,
            window.FrozenOpportunities,
            [],
            opportunity.OpportunityId);
        var activeMove = CampaignV10TestData.CopyMove(
            fixture.TriggeringMove,
            fixture.TriggeringMove.MovementEndedAfter,
            activeWindow);
        var snapshot = ApplyTrigger(fixture, activeMove);

        var observation = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);

        Assert.Equal(
            new ObservedReactionParticipant(
                opportunity.OpportunityId.Value,
                opportunity.ReactingRepresentation.RepresentationId),
            state.ActiveParticipant);
    }

    [Fact]
    public void AuthorityFactsRejectDuplicatesUnknownLocationsAndInvalidPresenceMembership()
    {
        var fixture = CampaignV10TestData.Create();
        var opposing = fixture.MovementSnapshot.World.Representations.Single(representation =>
            representation.BoundElementIds.Contains(
                fixture.Artifact.Definition.LegacyDefinition.Elements.Single(element =>
                    element.SideId == "commonwealth").ElementId));

        Assert.Throws<ArgumentException>(() => new CampaignObservationV6AuthorityFacts(
            ["east", "east"],
            []));
        Assert.Throws<ArgumentException>(() => CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts(["unknown-location"], [])));
        Assert.Throws<ArgumentException>(() => CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts([], ["unknown-representation"])));

        var exerting = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts([], [opposing.RepresentationId]));
        Assert.True(Assert.Single(exerting.ApparentOpposingPresences).ExertsZoc);
    }

    [Fact]
    public void ReactingDecisionCanonicallyOrdersAndRejectsInvalidOpportunityState()
    {
        var fixture = CampaignV10TestData.Create();
        var snapshot = ApplyTrigger(fixture, fixture.TriggeringMove);
        var baseline = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var baselineState = Assert.IsType<CampaignObservationReactingDecisionState>(
            baseline.DecisionState);
        var first = Assert.Single(baselineState.OwnOpportunities);
        var second = new ObservedReactionOpportunity(
            "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            "map-representation.9998");
        var ordered = new CampaignObservationReactingDecisionState(
            baselineState.WindowId,
            baselineState.ApparentTrigger,
            [second, first],
            null);
        var changed = CopyWithDecision(baseline, ordered);
        var canonical = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(changed));
        var firstJson = OpportunityJson(first);
        var secondJson = OpportunityJson(second);

        Assert.Equal(
            ordered.OwnOpportunities.Select(value => value.OpportunityId)
                .Order(StringComparer.Ordinal),
            ordered.OwnOpportunities.Select(value => value.OpportunityId));
        Assert.Throws<ArgumentException>(() => new CampaignObservationReactingDecisionState(
            baselineState.WindowId,
            baselineState.ApparentTrigger,
            [first, first],
            null));
        Assert.Throws<ArgumentException>(() => new CampaignObservationReactingDecisionState(
            baselineState.WindowId,
            baselineState.ApparentTrigger,
            [first],
            new ObservedReactionParticipant(
                second.OpportunityId,
                second.RepresentationId)));
        AssertRejects(canonical.Replace(
            $"\"ownOpportunities\":[{firstJson},{secondJson}]",
            $"\"ownOpportunities\":[{secondJson},{firstJson}]",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            $"\"ownOpportunities\":[{firstJson},{secondJson}]",
            $"\"ownOpportunities\":[{firstJson},{firstJson},{secondJson}]",
            StringComparison.Ordinal));
    }

    [Fact]
    public void StrictReaderRejectsLegacyMissingExtraDuplicateReorderedAndSourceMappedShapes()
    {
        var fixture = CampaignV10TestData.Create();
        var snapshot = ApplyTrigger(fixture, fixture.TriggeringMove);
        var observation = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["east", "west"]));
        var canonical = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(observation));

        AssertRejects(canonical.Replace(
            "\"contractVersion\":6",
            "\"contractVersion\":5",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"policyId\":\"sandtable.observation.zoc-reaction-side-safe.v1\",",
            string.Empty,
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "{\"contractVersion\":6,",
            "{\"contractVersion\":6,\"contractVersion\":6,",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"campaignId\":",
            "\"sourceMappings\":{},\"campaignId\":",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "{\"contractVersion\":6,\"policyId\":\"sandtable.observation.zoc-reaction-side-safe.v1\",",
            "{\"policyId\":\"sandtable.observation.zoc-reaction-side-safe.v1\",\"contractVersion\":6,",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"apparentEnemyControlledLocationIds\":[\"east\",\"west\"]",
            "\"apparentEnemyControlledLocationIds\":[\"west\",\"east\"]",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"apparentEnemyControlledLocationIds\":[\"east\",\"west\"]",
            "\"apparentEnemyControlledLocationIds\":[\"east\",\"east\",\"west\"]",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"apparentRepresentationId\":",
            "\"boundElementIds\":[\"secret\"],\"apparentRepresentationId\":",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"destinationLocationId\":\"east\"",
            "\"destinationLocationId\":\"unknown-location\"",
            StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignObservationSerializer.DeserializeCanonical(
            Encoding.UTF8.GetBytes(canonical)));
    }

    [Fact]
    public void RedactedHistoryRoundTripsWithoutAuthorityOrClosureReason()
    {
        var fixture = CampaignV10TestData.Create();
        var snapshot = ApplyTrigger(fixture, fixture.TriggeringMove);
        var reacting = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var phasing = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            Facts(["east"]));
        var reactingHistory = CampaignProjectedDecisionHistory.Project(reacting);
        var phasingHistory = CampaignProjectedDecisionHistory.Project(phasing);
        var reactingBytes = CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
            reactingHistory);
        var phasingJson = Encoding.UTF8.GetString(
            CampaignProjectedDecisionHistorySerializer.SerializeCanonical(phasingHistory));
        var reactingJson = Encoding.UTF8.GetString(reactingBytes);

        Assert.Equal(
            reactingHistory,
            CampaignProjectedDecisionHistorySerializer.DeserializeCanonical(reactingBytes));
        Assert.DoesNotContain("boundElementIds", reactingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("adjacencyEvidence", reactingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("sources", reactingJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reason", reactingJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("opportunityId", phasingJson, StringComparison.Ordinal);
        Assert.DoesNotContain(
            snapshot.ReactionWindow!.ApparentTrigger.ApparentRepresentationId,
            phasingJson,
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            CampaignProjectedDecisionHistorySerializer.DeserializeCanonical(
                Encoding.UTF8.GetBytes(reactingJson.Replace(
                    "\"campaignId\":",
                    "\"internalReason\":\"timeout\",\"campaignId\":",
                    StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() =>
            CampaignProjectedDecisionHistorySerializer.DeserializeCanonical(
                Encoding.UTF8.GetBytes(reactingJson.Replace(
                    "\"decisionState\":",
                    "\"observer\":\"commonwealth\",\"decisionState\":",
                    StringComparison.Ordinal))));
        AssertHistoryRejects(reactingJson.Replace(
            "\"contractVersion\":1",
            "\"contractVersion\":2",
            StringComparison.Ordinal));
        var decisionIndex = reactingJson.IndexOf(
            ",\"decisionState\":",
            StringComparison.Ordinal);
        Assert.True(decisionIndex > 0);
        AssertHistoryRejects($"{reactingJson[..decisionIndex]}}}");
        AssertHistoryRejects(reactingJson.Replace(
            $"{{\"contractVersion\":1,\"campaignId\":\"{reactingHistory.CampaignId}\",",
            $"{{\"campaignId\":\"{reactingHistory.CampaignId}\",\"contractVersion\":1,",
            StringComparison.Ordinal));
    }

    [Fact]
    public void ActiveObservationFivePathRemainsExecutedAndClosedToSuccessorBytes()
    {
        var movement = CampaignMovementTestData.ReachMovement();
        var active = Assert.IsType<CampaignObservation>(
            CampaignObservationProjector.Project(
                movement.Snapshot,
                movement.Context,
                movement.ActingSide).Observation);
        var fixture = CampaignV10TestData.Create();
        var dormant = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts([], []));

        Assert.Equal(5, active.ContractVersion);
        Assert.Equal("sandtable.observation.movement-side-safe.v1", active.PolicyId);
        Assert.Throws<JsonException>(() => CampaignObservationSerializer.DeserializeCanonical(
            CampaignObservationV6Serializer.SerializeCanonical(dormant)));
    }

    private static CampaignSnapshotV10 ApplyTrigger(
        CampaignV10Fixture fixture,
        ElementMovedV2 moved) => CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            moved,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => moved);

    private static CampaignObservationV6AuthorityFacts Facts(
        IReadOnlyList<string> controlledLocations) => new(controlledLocations, []);

    private static CampaignObservationV6 CopyWithDecision(
        CampaignObservationV6 source,
        CampaignObservationDecisionState decisionState) => new(
            source.ContractVersion,
            source.PolicyId,
            source.CampaignId,
            source.StateVersion,
            source.RulesetHash,
            source.ScenarioId,
            source.Observer,
            source.Position,
            source.Weather,
            source.Locations,
            source.Edges,
            source.OwnElements,
            source.ApparentOpposingPresences,
            source.ApparentEnemyControlledLocationIds,
            decisionState);

    private static string OpportunityJson(ObservedReactionOpportunity opportunity) =>
        $"{{\"opportunityId\":\"{opportunity.OpportunityId}\",\"representationId\":\"{opportunity.RepresentationId}\"}}";

    private static void AssertRejects(string json) => Assert.Throws<JsonException>(() =>
        CampaignObservationV6Serializer.DeserializeCanonical(Encoding.UTF8.GetBytes(json)));

    private static void AssertHistoryRejects(string json) => Assert.Throws<JsonException>(() =>
        CampaignProjectedDecisionHistorySerializer.DeserializeCanonical(
            Encoding.UTF8.GetBytes(json)));
}
