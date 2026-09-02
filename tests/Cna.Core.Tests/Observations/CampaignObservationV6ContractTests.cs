using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

[Trait("Boundary", "UserSpace")]
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
        Assert.Empty(observation.MovementEndedElementIds);
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
        var authorityWindow = snapshot.ReactionWindow!;
        Assert.NotNull(authorityWindow);
        var phasingJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(phasing));
        var reactingJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(reacting));

        Assert.Equal(phasingState.WindowId, reactingState.WindowId);
        Assert.NotEqual(authorityWindow.WindowId.Value, reactingState.WindowId);
        Assert.Equal(
            authorityWindow.ApparentTrigger.ApparentRepresentationId,
            reactingState.ApparentTrigger.ApparentRepresentationId);
        Assert.Equal(
            authorityWindow.ApparentTrigger.OriginLocationId,
            reactingState.ApparentTrigger.OriginLocationId);
        Assert.Equal(
            authorityWindow.ApparentTrigger.DestinationLocationId,
            reactingState.ApparentTrigger.DestinationLocationId);
        Assert.Empty(reacting.OwnElements);
        Assert.NotEmpty(phasing.OwnElements);
        Assert.Empty(reactingState.OwnOpportunities);
        using var reactingDocument = JsonDocument.Parse(reactingJson);
        var opportunitiesJson = reactingDocument.RootElement
            .GetProperty("decisionState")
            .GetProperty("ownOpportunities");
        Assert.Equal(0, opportunitiesJson.GetArrayLength());
        Assert.Null(reactingState.ActiveParticipant);
        Assert.DoesNotContain("frozenOpportunities", phasingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("opportunityId", phasingJson, StringComparison.Ordinal);
        Assert.DoesNotContain(
            authorityWindow.ApparentTrigger.ApparentRepresentationId,
            phasingJson,
            StringComparison.Ordinal);
        Assert.DoesNotContain("boundElementIds", reactingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("adjacencyEvidence", reactingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("sources", reactingJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reason", reactingJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InactiveZeroCapabilityOpportunityProjectsAsNoEligibleMembership()
    {
        var fixture = CampaignV10TestData.Create();
        var observation = CampaignObservationV6Projector.Project(
            ApplyTrigger(fixture, fixture.TriggeringMove),
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);

        Assert.Empty(state.OwnOpportunities);
        Assert.Null(state.ActiveParticipant);
        Assert.Empty(CampaignObservationV6ActionDerivation.DerivePlayer(observation).Candidates);
        var system = CampaignObservationV6ActionDerivation.DeriveSystem(observation);
        Assert.Single(system.Candidates.OfType<CloseReactionWindowNoEligibleAction>());
        Assert.Empty(system.Candidates.OfType<CloseReactionWindowUnavailableAction>());
        Assert.Empty(system.Candidates.OfType<CloseReactionWindowTimeoutAction>());
    }

    [Fact]
    public void InactiveOpportunityWithCurrentMoveOptionsRemainsProjectedAndActionable()
    {
        var fixture = CampaignV10TestData.Create(includeReactionExit: true);
        var observation = CampaignObservationV6Projector.Project(
            ApplyTrigger(fixture, fixture.TriggeringMove),
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts([]));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);

        Assert.Null(state.ActiveParticipant);
        var opportunity = Assert.Single(state.OwnOpportunities);
        Assert.Contains(
            opportunity.MoveOptions,
            option => option.OriginLocationId == "west"
                && option.DestinationLocationId == "center");
        var player = CampaignObservationV6ActionDerivation.DerivePlayer(observation);
        Assert.Contains(
            player.Candidates.OfType<MoveReactingElementAction>(),
            move => move.OpportunityId == opportunity.OpportunityId
                && move.OriginLocationId == "west"
                && move.DestinationLocationId == "center");
        Assert.Single(player.Candidates.OfType<DeclineReactionWindowAction>());
        Assert.Empty(player.Candidates.OfType<CompleteReactionParticipantAction>());
    }

    [Fact]
    public void PhasingBytesIgnoreTheReactingOnlyApparentTriggerIdentity()
    {
        var fixture = CampaignV10TestData.Create();
        var baseline = ApplyTrigger(fixture, fixture.TriggeringMove);
        var changed = ApplyTrigger(
            fixture,
            CampaignV10TestData.CreateTriggeringMove(
                fixture.MovementSnapshot,
                apparentRepresentationId: "apparent-axis-alternate"));

        Assert.Equal(
            CampaignObservationV6Serializer.SerializeCanonical(Project(baseline)),
            CampaignObservationV6Serializer.SerializeCanonical(Project(changed)));

        CampaignObservationV6 Project(CampaignSnapshotV10 snapshot) =>
            CampaignObservationV6Projector.Project(
                snapshot,
                fixture.Artifact,
                fixture.Scenario,
                LandSide.Axis,
                Facts(["east"]));
    }

    [Fact]
    public void ReactingOutputsDoNotEchoChangedPriorOwnElementIdentity()
    {
        var baselineFixture = CampaignV10TestData.Create();
        var changedFixture = CampaignV10TestData.Create("commonwealth-hidden-reactor");
        var baselineBefore = Project(baselineFixture, baselineFixture.MovementSnapshot);
        var changedBefore = Project(changedFixture, changedFixture.MovementSnapshot);
        var baseline = Project(
            baselineFixture,
            ApplyTrigger(baselineFixture, baselineFixture.TriggeringMove));
        var changed = Project(
            changedFixture,
            ApplyTrigger(changedFixture, changedFixture.TriggeringMove));

        Assert.NotEqual(
            CampaignObservationV6Serializer.SerializeCanonical(baselineBefore),
            CampaignObservationV6Serializer.SerializeCanonical(changedBefore));
        Assert.Equal(
            CampaignObservationV6Serializer.SerializeCanonical(baseline),
            CampaignObservationV6Serializer.SerializeCanonical(changed));
        Assert.Equal(
            CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
                CampaignProjectedDecisionHistory.Project(baseline)),
            CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
                CampaignProjectedDecisionHistory.Project(changed)));
        Assert.Equal(
            CampaignObservationV6LegalActionSerializer.Serialize(
                CampaignObservationV6ActionDerivation.DerivePlayer(baseline)),
            CampaignObservationV6LegalActionSerializer.Serialize(
                CampaignObservationV6ActionDerivation.DerivePlayer(changed)));

        CampaignObservationV6 Project(
            CampaignV10Fixture fixture,
            CampaignSnapshotV10 snapshot) =>
            CampaignObservationV6Projector.Project(
                snapshot,
                fixture.Artifact,
                fixture.Scenario,
                LandSide.Commonwealth,
                Facts(["west"]));
    }

    [Fact]
    public void DisclosureHandlesBindOnlySharedWindowFactsAndCurrentPublishedCapabilities()
    {
        var fixture = CampaignV10TestData.Create();
        var window = fixture.TriggeringMove.OpenedReactionWindow!;
        var first = CampaignObservationV6DisclosureIdentity.CreateWindow(
            fixture.MovementSnapshot.CampaignId,
            fixture.MovementSnapshot.RulesetHash,
            window.TriggerCommittedStateVersion,
            window.ReactingSide);

        Assert.Equal(first, CampaignObservationV6DisclosureIdentity.CreateWindow(
            fixture.MovementSnapshot.CampaignId,
            fixture.MovementSnapshot.RulesetHash,
            window.TriggerCommittedStateVersion,
            window.ReactingSide));
        Assert.NotEqual(first, CampaignObservationV6DisclosureIdentity.CreateWindow(
            $"{fixture.MovementSnapshot.CampaignId}.alternate",
            fixture.MovementSnapshot.RulesetHash,
            window.TriggerCommittedStateVersion,
            window.ReactingSide));
        Assert.NotEqual(first, CampaignObservationV6DisclosureIdentity.CreateWindow(
            fixture.MovementSnapshot.CampaignId,
            fixture.MovementSnapshot.RulesetHash,
            window.TriggerCommittedStateVersion + 1,
            window.ReactingSide));
        Assert.Equal(
            [typeof(string), typeof(string), typeof(long), typeof(LandSide)],
            typeof(CampaignObservationV6DisclosureIdentity)
                .GetMethod(nameof(CampaignObservationV6DisclosureIdentity.CreateWindow))!
                .GetParameters()
                .Select(parameter => parameter.ParameterType));

        var emptyCapability = CampaignObservationV6DisclosureIdentity.CreateCapabilityKey([]);
        var firstOpportunity = CampaignObservationV6DisclosureIdentity.CreateOpportunity(
            first,
            fixture.MovementSnapshot.StateVersion,
            emptyCapability);
        Assert.Equal(
            firstOpportunity,
            CampaignObservationV6DisclosureIdentity.CreateOpportunity(
                first,
                fixture.MovementSnapshot.StateVersion,
                emptyCapability));
        Assert.NotEqual(
            firstOpportunity,
            CampaignObservationV6DisclosureIdentity.CreateOpportunity(
                first,
                fixture.MovementSnapshot.StateVersion + 1,
                emptyCapability));
        Assert.NotEqual(
            firstOpportunity,
            CampaignObservationV6DisclosureIdentity.CreateOpportunity(
                first,
                fixture.MovementSnapshot.StateVersion,
                "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"));
    }

    [Fact]
    public void CapabilityAliasesIgnoreAuthorityOrderingAndRejectIndistinguishableParticipants()
    {
        const string windowId =
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var exactOne = new CapabilityPointAmount(1, 1);
        var option = new ObservedReactionMoveOption(
            "west",
            "east",
            new MovementActionCostBreakdown("clear", exactOne, null, [], exactOne));
        CampaignObservationV6DisclosureCapability[] first =
        [
            new("authority.1", [option], false),
            new("authority.2", [], false),
        ];
        CampaignObservationV6DisclosureCapability[] renamedAndReordered =
        [
            new("authority.z", [], false),
            new("authority.y", [option], false),
        ];

        Assert.Equal(
            PublicSurface(first),
            PublicSurface(renamedAndReordered));
        var nextState = CampaignObservationV6DisclosureIdentity.CreateAliases(
            windowId,
            13,
            [first[0]]);
        Assert.DoesNotContain(
            Assert.Single(nextState).PublicId,
            PublicSurface(first).Select(value => value.PublicId));

        CampaignObservationV6DisclosureCapability[] activeTie =
        [
            new("authority.1", [], false),
            new("authority.2", [], true),
        ];
        CampaignObservationV6DisclosureCapability[] renamedActiveTie =
        [
            new("authority.z", [], true),
            new("authority.y", [], false),
        ];
        Assert.Throws<InvalidOperationException>(() =>
            CampaignObservationV6DisclosureIdentity.CreateAliases(
                windowId,
                12,
                activeTie.Select(value => value with { IsActive = false })));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignObservationV6DisclosureIdentity.CreateAliases(
            windowId,
            12,
            activeTie));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignObservationV6DisclosureIdentity.CreateAliases(
            windowId,
            12,
            renamedActiveTie));

        IReadOnlyList<(string CapabilityKey, string PublicId)> PublicSurface(
            IEnumerable<CampaignObservationV6DisclosureCapability> capabilities) =>
            CampaignObservationV6DisclosureIdentity.CreateAliases(
                    windowId,
                    12,
                    capabilities)
                .Select(value => (
                    CampaignObservationV6DisclosureIdentity.CreateCapabilityKey(
                        value.MoveOptions),
                    value.PublicId))
                .OrderBy(value => value.Item1, StringComparer.Ordinal)
                .ToArray();
    }

    [Fact]
    public void RetainedTranscriptCannotJoinAReactingRepresentationToAnOwnElementFingerprint()
    {
        var fixture = CampaignV10TestData.Create();
        var before = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var after = ProjectReactingWithMoveOptions(fixture);
        var beforeJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(before));
        var afterJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(after));
        var historyJson = Encoding.UTF8.GetString(
            CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
                CampaignProjectedDecisionHistory.Project(after)));

        Assert.Contains("\"organizationId\":", beforeJson, StringComparison.Ordinal);
        foreach (var forbiddenFingerprintField in new[]
        {
            "organizationId",
            "baseCapabilityPointAllowance",
            "reserveStatus",
            "mobilityId",
            "ledgerGameTurn",
            "ledgerOperationStage",
            "capabilityPointsExpended",
            "cohesionLevel",
            "ownStacking",
        })
        {
            Assert.DoesNotContain(
                $"\"{forbiddenFingerprintField}\":",
                afterJson,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                $"\"{forbiddenFingerprintField}\":",
                historyJson,
                StringComparison.Ordinal);
        }

        Assert.Contains("\"moveOptions\":", afterJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ReactingSemanticAdmissionRejectsIdentityBearingRootRows()
    {
        var fixture = CampaignV10TestData.Create();
        var before = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var after = CampaignObservationV6Projector.Project(
            ApplyTrigger(fixture, fixture.TriggeringMove),
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var element = Assert.Single(before.OwnElements);

        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            after,
            after.DecisionState,
            [element]));

        using var beforeDocument = JsonDocument.Parse(
            CampaignObservationV6Serializer.SerializeCanonical(before));
        var ownElements = beforeDocument.RootElement.GetProperty("ownElements").GetRawText();
        var afterJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(after));
        AssertRejects(afterJson.Replace(
            "\"ownElements\":[]",
            $"\"ownElements\":{ownElements}",
            StringComparison.Ordinal));
        AssertRejects(afterJson
            .Replace(
                "\"ownElements\":[]",
                $"\"ownElements\":{ownElements}",
                StringComparison.Ordinal)
            .Replace(
                "\"movementEndedElementIds\":[]",
                $"\"movementEndedElementIds\":[\"{element.ElementId}\"]",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ReactingSemanticAdmissionRejectsMoveOptionsIncoherentWithVisibleFacts()
    {
        var fixture = CampaignV10TestData.Create();
        var observation = CampaignObservationV6Projector.Project(
            ApplyTrigger(fixture, fixture.TriggeringMove),
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts([]));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);
        Assert.Empty(state.OwnOpportunities);
        var destination = observation.Locations.Single(value => value.LocationId == "east");
        var exactOne = new CapabilityPointAmount(1, 1);
        var coherentCost = new MovementActionCostBreakdown(
            destination.TerrainId,
            exactOne,
            null,
            [],
            exactOne);
        var occupiedOption = new ObservedReactionMoveOption("west", "east", coherentCost);
        var occupiedDecision = new CampaignObservationReactingDecisionState(
            state.WindowId,
            state.ApparentTrigger,
            [BoundOpportunity(state.WindowId, observation.StateVersion, [occupiedOption])],
            null);

        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            occupiedDecision));

        var mismatchedCost = new MovementActionCostBreakdown(
            "secret-terrain",
            exactOne,
            null,
            [],
            exactOne);
        var mismatchedDecision = new CampaignObservationReactingDecisionState(
            state.WindowId,
            state.ApparentTrigger,
            [BoundOpportunity(
                state.WindowId,
                observation.StateVersion,
                [new ObservedReactionMoveOption("west", "east", mismatchedCost)])],
            null);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            mismatchedDecision,
            apparentOpposingPresences: []));
    }

    [Fact]
    public void ReactingSemanticAdmissionBindsOpportunityIdentityToPublishedCapability()
    {
        var fixture = CampaignV10TestData.Create();
        var observation = ProjectReactingWithMoveOptions(fixture);
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);
        var opportunity = Assert.Single(state.OwnOpportunities);
        const string forgedId =
            "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

        var forgedOpportunity = new ObservedReactionOpportunity(
            forgedId,
            opportunity.MoveOptions);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(state, [forgedOpportunity])));
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(state, [opportunity, forgedOpportunity])));

        var option = Assert.Single(opportunity.MoveOptions);
        var route = option.CostBreakdown.RouteAdjustment;
        Assert.NotNull(route);
        var changedTerrainCost = option.CostBreakdown.DestinationTerrainCost
            + new CapabilityPointAmount(1, 1);
        var changedCost = new MovementActionCostBreakdown(
            option.CostBreakdown.DestinationTerrainId,
            changedTerrainCost,
            route,
            option.CostBreakdown.CrossedHexsideCosts,
            option.CostBreakdown.TotalCost);
        var changedOption = new ObservedReactionMoveOption(
            option.OriginLocationId,
            option.DestinationLocationId,
            changedCost);
        var staleCapability = new ObservedReactionOpportunity(
            opportunity.OpportunityId,
            [changedOption]);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(state, [staleCapability])));
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            state,
            stateVersion: observation.StateVersion + 1));

        const string otherWindowId =
            "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            new CampaignObservationReactingDecisionState(
                otherWindowId,
                state.ApparentTrigger,
                state.OwnOpportunities,
                state.ActiveParticipant)));

        var observationJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(observation));
        AssertRejects(observationJson.Replace(
            opportunity.OpportunityId,
            forgedId,
            StringComparison.Ordinal));
        AssertRejects(observationJson.Replace(
            $"\"stateVersion\":{observation.StateVersion}",
            $"\"stateVersion\":{observation.StateVersion + 1}",
            StringComparison.Ordinal));

        var historyJson = Encoding.UTF8.GetString(
            CampaignProjectedDecisionHistorySerializer.SerializeCanonical(
                CampaignProjectedDecisionHistory.Project(observation)));
        AssertHistoryRejects(historyJson.Replace(
            opportunity.OpportunityId,
            forgedId,
            StringComparison.Ordinal));
    }

    [Fact]
    public void ReactingSemanticAdmissionRejectsCostClaimsOutsideSelectedPublishedEdge()
    {
        var fixture = CampaignV10TestData.Create();
        var observation = ProjectReactingWithMoveOptions(fixture);
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);
        var opportunity = Assert.Single(state.OwnOpportunities);
        var option = Assert.Single(opportunity.MoveOptions);
        var cost = option.CostBreakdown;
        var route = cost.RouteAdjustment;
        Assert.NotNull(route);

        var missingPublishedFeatureCost = new MovementActionCostBreakdown(
            cost.DestinationTerrainId,
            cost.DestinationTerrainCost,
            null,
            [],
            cost.DestinationTerrainCost);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(
                state,
                [BoundOpportunity(
                    state.WindowId,
                    observation.StateVersion,
                    [new ObservedReactionMoveOption(
                        option.OriginLocationId,
                        option.DestinationLocationId,
                        missingPublishedFeatureCost)])])));

        var borrowedRouteCost = new MovementActionCostBreakdown(
            cost.DestinationTerrainId,
            cost.DestinationTerrainCost,
            new MovementActionRouteAdjustment(
                "land.edge.track",
                route.CostKind,
                route.Amount),
            [],
            cost.TotalCost);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(
                state,
                [BoundOpportunity(
                    state.WindowId,
                    observation.StateVersion,
                    [new ObservedReactionMoveOption(
                        option.OriginLocationId,
                        option.DestinationLocationId,
                        borrowedRouteCost)])])));

        var addedCost = new CapabilityPointAmount(1, 1);
        var borrowedHexsideCost = new MovementActionCostBreakdown(
            cost.DestinationTerrainId,
            cost.DestinationTerrainCost,
            null,
            [new MovementActionHexsideCost(
                "land.edge.ridge",
                MovementHexsideDirection.Either,
                addedCost)],
            cost.DestinationTerrainCost + addedCost);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(
                state,
                [BoundOpportunity(
                    state.WindowId,
                    observation.StateVersion,
                    [new ObservedReactionMoveOption(
                        option.OriginLocationId,
                        option.DestinationLocationId,
                        borrowedHexsideCost)])])));

        var directionalEdges = observation.Edges.Select(edge =>
            edge.FirstLocationId == "east" && edge.SecondLocationId == "west"
                ? new CampaignObservationEdge(
                    edge.FirstLocationId,
                    edge.SecondLocationId,
                    [new CampaignObservationEdgeFeature("land.edge.slope", "west")])
                : edge).ToArray();
        var wrongDirectionCost = new MovementActionCostBreakdown(
            cost.DestinationTerrainId,
            cost.DestinationTerrainCost,
            null,
            [new MovementActionHexsideCost(
                "land.edge.slope",
                MovementHexsideDirection.Down,
                addedCost)],
            cost.DestinationTerrainCost + addedCost);
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            observation,
            ReactingState(
                state,
                [BoundOpportunity(
                    state.WindowId,
                    observation.StateVersion,
                    [new ObservedReactionMoveOption(
                        option.OriginLocationId,
                        option.DestinationLocationId,
                        wrongDirectionCost)])]),
            edges: directionalEdges));

        var correctAddedCost = new CapabilityPointAmount(2, 1);
        var correctDirectionCost = new MovementActionCostBreakdown(
            cost.DestinationTerrainId,
            cost.DestinationTerrainCost,
            null,
            [new MovementActionHexsideCost(
                "land.edge.slope",
                MovementHexsideDirection.Up,
                correctAddedCost)],
            cost.DestinationTerrainCost + correctAddedCost);
        var directional = CopyWithDecision(
            observation,
            ReactingState(
                state,
                [BoundOpportunity(
                    state.WindowId,
                    observation.StateVersion,
                    [new ObservedReactionMoveOption(
                        option.OriginLocationId,
                        option.DestinationLocationId,
                        correctDirectionCost)])]),
            edges: directionalEdges);
        Assert.Equal(
            directional,
            CampaignObservationV6Serializer.DeserializeCanonical(
                CampaignObservationV6Serializer.SerializeCanonical(directional)));
    }

    [Fact]
    public void DecisionStateAdmissionCorrelatesAudienceWithActiveSide()
    {
        var fixture = CampaignV10TestData.Create();
        var snapshot = ApplyTrigger(fixture, fixture.TriggeringMove);
        var reacting = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts([]));
        var phasing = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            Facts([]));

        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            reacting,
            reacting.DecisionState,
            observer: LandSide.Axis));
        Assert.Throws<ArgumentException>(() => CopyWithDecision(
            phasing,
            phasing.DecisionState,
            observer: LandSide.Commonwealth));

        var reactingJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(reacting));
        var phasingJson = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(phasing));
        AssertRejects(reactingJson.Replace(
            "\"observer\":\"commonwealth\"",
            "\"observer\":\"axis\"",
            StringComparison.Ordinal));
        AssertRejects(phasingJson.Replace(
            "\"observer\":\"axis\"",
            "\"observer\":\"commonwealth\"",
            StringComparison.Ordinal));
    }

    [Fact]
    public void InactiveHiddenOpportunityChangesLeaveBothAudienceBytesIdentical()
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
        Assert.Equal(
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
        Assert.Empty(Assert.IsType<CampaignObservationReactingDecisionState>(
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
        var idleObservation = CampaignObservationV6Projector.Project(
            ApplyTrigger(fixture, fixture.TriggeringMove),
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));
        var idleState = Assert.IsType<CampaignObservationReactingDecisionState>(
            idleObservation.DecisionState);
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

        Assert.Empty(idleState.OwnOpportunities);
        Assert.Equal(idleState.WindowId, state.WindowId);
        var activeOpportunity = Assert.Single(state.OwnOpportunities);
        Assert.Empty(activeOpportunity.MoveOptions);
        Assert.Equal(
            new ObservedReactionParticipant(
                activeOpportunity.OpportunityId),
            state.ActiveParticipant);
        var player = CampaignObservationV6ActionDerivation.DerivePlayer(observation);
        Assert.Single(player.Candidates.OfType<CompleteReactionParticipantAction>());
        Assert.Empty(player.Candidates.OfType<MoveReactingElementAction>());
        Assert.Empty(player.Candidates.OfType<DeclineReactionWindowAction>());
        var system = CampaignObservationV6ActionDerivation.DeriveSystem(observation);
        Assert.Single(system.Candidates.OfType<CloseReactionWindowUnavailableAction>());
        Assert.Single(system.Candidates.OfType<CloseReactionWindowTimeoutAction>());
        Assert.Empty(system.Candidates.OfType<CloseReactionWindowNoEligibleAction>());
    }

    [Fact]
    public void MovementEndedProjectsOnlyForItsExactOwnerAndMovementScope()
    {
        var fixture = CampaignV10TestData.Create();
        var movement = fixture.MovementSnapshot.CurrentPosition.SequencePosition!;
        var endedMove = CampaignV10TestData.CopyMove(
            fixture.TriggeringMove,
            new CampaignMovementEndedState(movement),
            fixture.TriggeringMove.OpenedReactionWindow);
        var snapshot = ApplyTrigger(fixture, endedMove);

        var phasing = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            Facts(["east"]));
        var reacting = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts(["west"]));

        Assert.Equal([endedMove.ElementId], phasing.MovementEndedElementIds);
        Assert.Empty(reacting.MovementEndedElementIds);
    }

    [Fact]
    public void MovementEndedMembershipParticipatesInObservationHashing()
    {
        var fixture = CampaignV10TestData.Create();
        var baseline = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            Facts([]));
        var changed = new CampaignObservationV6(
            baseline.ContractVersion,
            baseline.PolicyId,
            baseline.CampaignId,
            baseline.StateVersion,
            baseline.RulesetHash,
            baseline.ScenarioId,
            baseline.Observer,
            baseline.Position,
            baseline.Weather,
            baseline.Locations,
            baseline.Edges,
            baseline.OwnElements,
            baseline.ApparentOpposingPresences,
            baseline.ApparentEnemyControlledLocationIds,
            [baseline.OwnElements.Single().ElementId],
            baseline.DecisionState);

        Assert.NotEqual(baseline, changed);
        Assert.NotEqual(baseline.GetHashCode(), changed.GetHashCode());
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
        var baseline = ProjectReactingWithMoveOptions(fixture);
        var baselineState = Assert.IsType<CampaignObservationReactingDecisionState>(
            baseline.DecisionState);
        var first = Assert.Single(baselineState.OwnOpportunities);
        var second = BoundOpportunity(
            baselineState.WindowId,
            baseline.StateVersion,
            []);
        var ordered = new CampaignObservationReactingDecisionState(
            baselineState.WindowId,
            baselineState.ApparentTrigger,
            [second, first],
            new ObservedReactionParticipant(second.OpportunityId));
        var changed = CopyWithDecision(baseline, ordered);
        var canonical = Encoding.UTF8.GetString(
            CampaignObservationV6Serializer.SerializeCanonical(changed));
        var orderedJson = ordered.OwnOpportunities
            .Select(OpportunityJson)
            .ToArray();
        var canonicalOpportunities =
            $"\"ownOpportunities\":[{string.Join(',', orderedJson)}]";

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
            [second],
            null));
        Assert.Throws<ArgumentException>(() => new CampaignObservationReactingDecisionState(
            baselineState.WindowId,
            baselineState.ApparentTrigger,
            [first],
            new ObservedReactionParticipant(
                second.OpportunityId)));
        Assert.Equal(
            changed,
            CampaignObservationV6Serializer.DeserializeCanonical(
                CampaignObservationV6Serializer.SerializeCanonical(changed)));
        var activeParticipantJson =
            $"\"activeParticipant\":{{\"opportunityId\":\"{second.OpportunityId}\"}}";
        Assert.Contains(activeParticipantJson, canonical, StringComparison.Ordinal);
        AssertRejects(canonical.Replace(
            activeParticipantJson,
            "\"activeParticipant\":null",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            canonicalOpportunities,
            $"\"ownOpportunities\":[{orderedJson[1]},{orderedJson[0]}]",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            canonicalOpportunities,
            $"\"ownOpportunities\":[{orderedJson[0]},{orderedJson[0]},{orderedJson[1]}]",
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
            "\"movementEndedElementIds\":[],",
            string.Empty,
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"movementEndedElementIds\":[]",
            "\"movementEndedElementIds\":[\"secret\",\"secret\"]",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"activeParticipant\":null",
            "\"ownStacking\":[],\"activeParticipant\":null",
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

    private static CampaignObservationV6 ProjectReactingWithMoveOptions(
        CampaignV10Fixture fixture)
    {
        var before = CampaignObservationV6Projector.Project(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts([]));
        var projected = CampaignObservationV6Projector.Project(
            ApplyTrigger(fixture, fixture.TriggeringMove),
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            Facts([]));
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            projected.DecisionState);
        var element = Assert.Single(before.OwnElements);
        var stacking = Cna1979Movement.LookupStackingValue(element.OrganizationId);
        Assert.True(stacking.IsSupported);
        var options = CampaignObservationV6ActionDerivation.DeriveReactionMoveOptions(
            projected.Position,
            projected.Locations,
            projected.Edges,
            [],
            [],
            element,
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [element.CurrentLocationId] = stacking.Value.StackingValue,
            });
        var opportunity = BoundOpportunity(
            state.WindowId,
            projected.StateVersion,
            options);
        return CopyWithDecision(
            projected,
            ReactingState(state, [opportunity]),
            apparentOpposingPresences: []);
    }

    private static CampaignObservationV6 CopyWithDecision(
        CampaignObservationV6 source,
        CampaignObservationDecisionState decisionState,
        IReadOnlyList<ObservedOwnElement>? ownElements = null,
        IReadOnlyList<ObservedApparentPresence>? apparentOpposingPresences = null,
        LandSide? observer = null,
        long? stateVersion = null,
        IReadOnlyList<CampaignObservationEdge>? edges = null) => new(
            source.ContractVersion,
            source.PolicyId,
            source.CampaignId,
            stateVersion ?? source.StateVersion,
            source.RulesetHash,
            source.ScenarioId,
            observer ?? source.Observer,
            source.Position,
            source.Weather,
            source.Locations,
            edges ?? source.Edges,
            ownElements ?? source.OwnElements,
            apparentOpposingPresences ?? source.ApparentOpposingPresences,
            source.ApparentEnemyControlledLocationIds,
            source.MovementEndedElementIds,
            decisionState);

    private static CampaignObservationReactingDecisionState ReactingState(
        CampaignObservationReactingDecisionState source,
        IReadOnlyList<ObservedReactionOpportunity> opportunities) => new(
            source.WindowId,
            source.ApparentTrigger,
            opportunities,
            null);

    private static ObservedReactionOpportunity BoundOpportunity(
        string windowId,
        long stateVersion,
        IReadOnlyList<ObservedReactionMoveOption> moveOptions) => new(
            CampaignObservationV6DisclosureIdentity.CreateOpportunity(
                windowId,
                stateVersion,
                CampaignObservationV6DisclosureIdentity.CreateCapabilityKey(moveOptions)),
            moveOptions);

    private static string OpportunityJson(ObservedReactionOpportunity opportunity)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("opportunityId", opportunity.OpportunityId);
            writer.WriteStartArray("moveOptions");
            foreach (var option in opportunity.MoveOptions)
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

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void AssertRejects(string json) => Assert.Throws<JsonException>(() =>
        CampaignObservationV6Serializer.DeserializeCanonical(Encoding.UTF8.GetBytes(json)));

    private static void AssertHistoryRejects(string json) => Assert.Throws<JsonException>(() =>
        CampaignProjectedDecisionHistorySerializer.DeserializeCanonical(
            Encoding.UTF8.GetBytes(json)));
}
