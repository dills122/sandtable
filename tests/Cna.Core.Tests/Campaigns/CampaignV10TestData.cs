using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

internal sealed record CampaignV10Fixture(
    ContentPackV5Artifact Artifact,
    ContentScenario Scenario,
    CampaignCreatedV9 Created,
    CampaignSnapshotV10 MovementSnapshot,
    ElementMovedV2 TriggeringMove);

internal static class CampaignV10TestData
{
    public static CampaignV10Fixture Create(
        string reactorElementId = ZocReactionContentTestData.SecondElementId,
        bool includeReactionExit = false) => CreateWithReactors(
            [reactorElementId],
            includeReactionExit);

    public static CampaignV10Fixture CreateWithReactors(
        IReadOnlyList<string> reactorElementIds,
        bool includeReactionExit = false,
        string? reactorClassificationId = null,
        string reactorLocationId = "west",
        IReadOnlyDictionary<string, string>? reactorClassificationIds = null,
        bool includeRemoteArea = false,
        bool includeReactionContinuation = false,
        IReadOnlyDictionary<string, string>? reactorLocationIds = null,
        bool includePhasingZocSupport = false)
    {
        ArgumentNullException.ThrowIfNull(reactorElementIds);
        if (reactorElementIds.Count == 0)
        {
            throw new ArgumentException("At least one reactor is required.", nameof(reactorElementIds));
        }

        var artifact = CreateMixedSideArtifact(
            reactorElementIds,
            includeReactionExit,
            reactorClassificationId ?? Cna1979Combat.CombatUnitClassificationId,
            reactorLocationId,
            reactorClassificationIds,
            includeRemoteArea,
            includeReactionContinuation,
            reactorLocationIds,
            includePhasingZocSupport);
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var setup = CreateSetup(artifact, scenario);
        var created = CampaignCreationV9Factory.Create(
            "campaign-v10",
            Cna1979Ruleset.Manifest.Hash,
            setup,
            artifact,
            scenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0),
            Cna1979LandSequence.CreateTurn(1)[0]);
        var movement = MaterializedMovement();
        var snapshot = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            created.CampaignId,
            11,
            created.RulesetHash,
            created.Setup,
            created.InitialWorld,
            LandSide.Axis,
            [new CampaignOperationStageOrder(
                CampaignOperationStageOrder.CurrentContractVersion,
                1,
                1,
                LandSide.Axis,
                LandSide.Commonwealth)],
            [],
            created.RandomState,
            CampaignPositionV10.FromSequence(movement),
            null);
        var triggeringMove = CreateTriggeringMove(
            snapshot,
            reactorElementId: reactorElementIds[0]);
        return new CampaignV10Fixture(
            artifact,
            scenario,
            created,
            snapshot,
            triggeringMove);
    }

    public static ElementMovedV2 CreateTriggeringMove(
        CampaignSnapshotV10 snapshot,
        IReadOnlyList<CampaignFrozenReactionOpportunity>? opportunities = null,
        string apparentRepresentationId = "apparent-axis-alpha",
        string reactorElementId = ZocReactionContentTestData.SecondElementId)
    {
        var movement = snapshot.CurrentPosition.SequencePosition!;
        var triggerBefore = snapshot.World.Representations.Single(value =>
            value.BoundElementIds.Contains(ZocReactionContentTestData.FirstElementId));
        var reactor = snapshot.World.Representations.Single(value =>
            value.BoundElementIds.Contains(reactorElementId));
        var triggerAfter = new CampaignMapRepresentationState(
            triggerBefore.RepresentationId,
            "east",
            triggerBefore.BindingKind,
            triggerBefore.BoundElementIds);
        var windowId = CampaignReactionIdentity.CreateWindow(
            snapshot.CampaignId,
            snapshot.RulesetHash,
            ElementMovedV2.CurrentContractVersion,
            checked(snapshot.StateVersion + 1),
            triggerAfter,
            "west",
            "east",
            LandSide.Commonwealth);
        var frozen = opportunities ??
        [
            new CampaignFrozenReactionOpportunity(
                CampaignReactionIdentity.CreateOpportunity(windowId, reactor),
                reactor,
                new CampaignReactionAdjacencyEvidence(
                    reactor.CurrentLocationId,
                    "east",
                    true,
                    [Source("8.3.reaction-adjacency")]))
        ];
        var window = new CampaignReactionWindow(
            windowId,
            checked(snapshot.StateVersion + 1),
            LandSide.Axis,
            LandSide.Commonwealth,
            new CampaignReactingPosition(movement),
            new CampaignReactionTriggerAuthority(
                ElementMovedV2.CurrentContractVersion,
                ZocReactionContentTestData.FirstElementId,
                triggerAfter,
                "west",
                "east"),
            new CampaignApparentReactionTrigger(
                apparentRepresentationId,
                "west",
                "east"),
            frozen,
            [],
            null);
        var cost = new CampaignMovementCost(
            "clear",
            new CapabilityPointAmount(1, 1),
            [Source("7.11.clear")],
            null,
            [],
            new CapabilityPointAmount(1, 1));

        return new ElementMovedV2(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.StateVersion,
            movement.PositionId,
            movement.GameTurn,
            movement.OperationStage,
            LandSide.Axis,
            ZocReactionContentTestData.FirstElementId,
            triggerBefore.RepresentationId,
            "west",
            "east",
            Cna1979Movement.MotorizedMobilityId,
            [Source("7.11.motorized")],
            cost,
            CapabilityPointAmount.Zero,
            new CapabilityPointAmount(1, 1),
            0,
            0,
            null,
            movement,
            window);
    }

    public static ElementMovedV2 CreateNonTriggeringMove(CampaignSnapshotV10 snapshot)
    {
        var triggering = CreateTriggeringMove(snapshot);
        return new ElementMovedV2(
            triggering.CampaignId,
            triggering.StateVersion,
            triggering.PriorStateVersion,
            triggering.FromPositionId,
            triggering.GameTurn,
            triggering.OperationStage,
            triggering.ActingSide,
            triggering.ElementId,
            triggering.RepresentationId,
            triggering.OriginLocationId,
            triggering.DestinationLocationId,
            triggering.MobilityId,
            triggering.MobilitySources,
            triggering.Cost,
            triggering.CapabilityPointsExpendedBefore,
            triggering.CapabilityPointsExpendedAfter,
            triggering.CohesionBefore,
            triggering.CohesionAfter,
            triggering.MovementEndedAfter,
            triggering.SequencePosition,
            null);
    }

    public static ElementMovedV2 CopyMove(
        ElementMovedV2 moved,
        CampaignMovementEndedState? movementEndedAfter,
        CampaignReactionWindow? openedReactionWindow) => new(
            moved.CampaignId,
            moved.StateVersion,
            moved.PriorStateVersion,
            moved.FromPositionId,
            moved.GameTurn,
            moved.OperationStage,
            moved.ActingSide,
            moved.ElementId,
            moved.RepresentationId,
            moved.OriginLocationId,
            moved.DestinationLocationId,
            moved.MobilityId,
            moved.MobilitySources,
            moved.Cost,
            moved.CapabilityPointsExpendedBefore,
            moved.CapabilityPointsExpendedAfter,
            moved.CohesionBefore,
            moved.CohesionAfter,
            movementEndedAfter,
            moved.SequencePosition,
            openedReactionWindow);

    private static LandSequencePosition MaterializedMovement()
    {
        var position = Cna1979LandSequence.CreateTurn(1).Single(candidate =>
            candidate.OperationStage == 1
            && candidate.ActorRole == LandActorRole.FirstActingSide
            && candidate.SegmentId == LandSegmentIds.Movement);
        return new LandSequencePosition(
            position.ContractVersion,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            position.PhaseId,
            position.SegmentId,
            position.StepId,
            position.ActorRole,
            LandSide.Axis,
            position.Sources);
    }

    private static CampaignSetupSnapshot CreateSetup(
        ContentPackV5Artifact artifact,
        ContentScenario scenario)
    {
        var template = Cna1979SetupCatalog.Definitions[0];
        var legacyArtifact = ContentPackArtifact.Create(artifact.Definition.LegacyDefinition);
        return CampaignSetupSnapshot.FromDefinition(new CampaignSetupDefinition(
            template.SchemaVersion,
            "zoc-reaction-v10-setup",
            "ZOC reaction v10 setup",
            true,
            scenario.Start.GameTurn,
            template.InitialInitiative,
            template.OpeningPreamble,
            template.Weather,
            template.StageEntry,
            new CampaignContentSelection(legacyArtifact.Identity, scenario.ScenarioId),
            template.Sources));
    }

    private static ContentPackV5Artifact CreateMixedSideArtifact(
        IReadOnlyList<string> reactorElementIds,
        bool includeReactionExit,
        string reactorClassificationId,
        string reactorLocationId,
        IReadOnlyDictionary<string, string>? reactorClassificationIds,
        bool includeRemoteArea,
        bool includeReactionContinuation,
        IReadOnlyDictionary<string, string>? reactorLocationIds,
        bool includePhasingZocSupport)
    {
        var reactorElementId = reactorElementIds[0];
        var definition = ZocReactionContentTestData.CreatePositiveFixture();
        if (!string.Equals(
            reactorElementId,
            ZocReactionContentTestData.SecondElementId,
            StringComparison.Ordinal))
        {
            definition = RenameReactor(definition, reactorElementId);
        }

        var legacy = definition.LegacyDefinition;
        var commonwealthFormation = new ContentFormation(
            "commonwealth-formation",
            "commonwealth",
            null,
            legacy.Formations.Single().OrganizationId,
            ContentTestData.Origin("content.formation.commonwealth"));
        var reactorTemplate = legacy.Elements.Single(element =>
            element.ElementId == reactorElementId);
        var reactors = reactorElementIds.Select(elementId => new ContentCombatElement(
            elementId,
            "commonwealth",
            commonwealthFormation.FormationId,
            reactorTemplate.OrganizationId,
            reactorTemplate.MobilityId,
            reactorTemplate.BaseCapabilityPointAllowance,
            reactorTemplate.PlacementMode,
            reactorTemplate.Origin,
            reactorTemplate.BreakdownVehicleCohort)).ToArray();
        const string phasingSupportId = "axis-zoc-support";
        var phasingTemplate = legacy.Elements.Single(element =>
            element.ElementId == ZocReactionContentTestData.FirstElementId);
        ContentCombatElement[] phasingSupport = includePhasingZocSupport
            ? [new ContentCombatElement(
                phasingSupportId,
                phasingTemplate.SideId,
                phasingTemplate.ParentFormationId,
                phasingTemplate.OrganizationId,
                phasingTemplate.MobilityId,
                phasingTemplate.BaseCapabilityPointAllowance,
                phasingTemplate.PlacementMode,
                phasingTemplate.Origin,
                phasingTemplate.BreakdownVehicleCohort)]
            : [];
        var elements = legacy.Elements
            .Where(element => element.ElementId != reactorElementId)
            .Concat(reactors)
            .Concat(phasingSupport)
            .ToArray();
        var reactorLocations = reactorElementIds.ToDictionary(
            elementId => elementId,
            elementId => reactorLocationIds?.GetValueOrDefault(elementId) ?? reactorLocationId,
            StringComparer.Ordinal);
        var locations = legacy.Locations.ToList();
        foreach (var locationId in reactorLocations.Values.Distinct(StringComparer.Ordinal))
        {
            if (locations.All(value => value.LocationId != locationId))
            {
                locations.Add(new ContentHex(
                    locationId,
                    "land.terrain.clear",
                    null,
                    ContentTestData.Origin($"content.hex.reactor.{locationId}")));
            }
        }

        if ((includeReactionExit || includeReactionContinuation)
            && locations.All(value => value.LocationId != "center"))
        {
            locations.Add(new ContentHex(
                "center",
                "land.terrain.clear",
                null,
                ContentTestData.Origin("content.hex.center")));
        }

        if (includeReactionContinuation && locations.All(value => value.LocationId != "south"))
        {
            locations.Add(new ContentHex(
                "south",
                "land.terrain.clear",
                null,
                ContentTestData.Origin("content.hex.south")));
        }

        if (includeRemoteArea)
        {
            locations.Add(new ContentHex(
                "remote-source",
                "land.terrain.clear",
                null,
                ContentTestData.Origin("content.hex.remote-source")));
            locations.Add(new ContentHex(
                "remote-neighbor",
                "land.terrain.clear",
                null,
                ContentTestData.Origin("content.hex.remote-neighbor")));
        }

        var edges = legacy.Edges.ToList();
        foreach (var locationId in reactorLocations.Values
                     .Distinct(StringComparer.Ordinal)
                     .Where(value => value != "west"))
        {
            edges.Add(new ContentHexEdge(
                "east",
                locationId,
                [new ContentEdgeFeature(
                    "land.edge.road",
                    null,
                    ContentTestData.Origin($"content.edge.trigger-road.{locationId}"))],
                ContentTestData.Origin($"content.edge.trigger.{locationId}")));
        }

        if (includeReactionExit)
        {
            edges.Add(new ContentHexEdge(
                "west",
                "center",
                [new ContentEdgeFeature(
                    "land.edge.road",
                    null,
                    ContentTestData.Origin("content.edge.reaction-exit-road"))],
                ContentTestData.Origin("content.edge.reaction-exit")));
            foreach (var locationId in reactorLocations.Values
                         .Distinct(StringComparer.Ordinal)
                         .Where(value => value != "west"))
            {
                edges.Add(new ContentHexEdge(
                    locationId,
                    "center",
                    [new ContentEdgeFeature(
                        "land.edge.road",
                        null,
                        ContentTestData.Origin($"content.edge.reactor-exit-road.{locationId}"))],
                    ContentTestData.Origin($"content.edge.reactor-exit.{locationId}")));
            }
        }

        if (includeReactionContinuation)
        {
            edges.Add(new ContentHexEdge(
                "center",
                "south",
                [new ContentEdgeFeature(
                    "land.edge.road",
                    null,
                    ContentTestData.Origin("content.edge.reaction-continuation-road"))],
                ContentTestData.Origin("content.edge.reaction-continuation")));
        }

        if (includeRemoteArea)
        {
            edges.Add(new ContentHexEdge(
                "remote-source",
                "remote-neighbor",
                [new ContentEdgeFeature(
                    "land.edge.road",
                    null,
                    ContentTestData.Origin("content.edge.remote-road"))],
                ContentTestData.Origin("content.edge.remote")));
            edges.Add(new ContentHexEdge(
                "remote-neighbor",
                reactorLocationId,
                [new ContentEdgeFeature(
                    "land.edge.road",
                    null,
                    ContentTestData.Origin("content.edge.remote-connector-road"))],
                ContentTestData.Origin("content.edge.remote-connector")));
        }
        var changedScenarios = legacy.Scenarios.Select(value => new ContentScenario(
            value.ScenarioId,
            value.Start,
            value.End,
            value.InitialPlacements
                .Where(placement => placement.ElementId != reactorElementId)
                .Concat(reactorElementIds.Select(elementId => new ContentInitialPlacement(
                    elementId,
                    reactorLocations[elementId],
                    value.InitialPlacements.Single(placement =>
                        placement.ElementId == reactorElementId).Origin)))
                .Concat(phasingSupport.Select(element => new ContentInitialPlacement(
                    element.ElementId,
                    "west",
                    value.InitialPlacements.Single(placement =>
                        placement.ElementId == ZocReactionContentTestData.FirstElementId).Origin))),
            value.Origin)).ToArray();
        var changedLegacy = new ContentPackDefinition(
            legacy.SchemaVersion,
            legacy.FormatId,
            legacy.PackId,
            legacy.RulesetId,
            legacy.Capabilities,
            legacy.SourceIndex,
            locations,
            legacy.WeatherAreaAssignments,
            edges,
            [.. legacy.Formations, commonwealthFormation],
            elements,
            changedScenarios);
        var reactorFactsTemplate = definition.ElementCombatFacts.Single(value =>
            value.ElementId == reactorElementId);
        var combatFacts = definition.ElementCombatFacts
            .Where(value => value.ElementId != reactorElementId)
            .Concat(reactorElementIds.Select(elementId => new ContentElementCombatFacts(
                elementId,
                reactorClassificationIds is not null
                    && reactorClassificationIds.TryGetValue(elementId, out var classificationId)
                        ? classificationId
                        : reactorClassificationId,
                reactorFactsTemplate.Components.Select(component => new ContentCombatComponent(
                    component.ComponentId.Replace(
                        reactorElementId,
                        elementId,
                        StringComparison.Ordinal),
                    component.ComponentClassId,
                    component.MaximumToe,
                    component.DefensiveCloseAssaultRating,
                    component.Origin)),
                reactorFactsTemplate.Origin)))
            .Concat(phasingSupport.Select(element =>
            {
                var template = definition.ElementCombatFacts.Single(value =>
                    value.ElementId == ZocReactionContentTestData.FirstElementId);
                return new ContentElementCombatFacts(
                    element.ElementId,
                    template.CombatClassificationId,
                    template.Components.Select(component => new ContentCombatComponent(
                        component.ComponentId.Replace(
                            ZocReactionContentTestData.FirstElementId,
                            element.ElementId,
                            StringComparison.Ordinal),
                        component.ComponentClassId,
                        component.MaximumToe,
                        component.DefensiveCloseAssaultRating,
                        component.Origin)),
                    template.Origin);
            }))
            .ToArray();
        var placementTemplate = definition.InitialPlacementCombatFacts.Single(value =>
            value.ElementId == reactorElementId);
        var placementFacts = definition.InitialPlacementCombatFacts
            .Where(value => value.ElementId != reactorElementId)
            .Concat(reactorElementIds.Select(elementId =>
                new ContentInitialPlacementCombatFacts(
                    placementTemplate.ScenarioId,
                    elementId,
                    placementTemplate.InitialComponentToes.Select(toe =>
                        new ContentInitialComponentToe(
                            toe.ComponentId.Replace(
                                reactorElementId,
                                elementId,
                                StringComparison.Ordinal),
                            toe.CurrentToe,
                            toe.Origin)))))
            .Concat(phasingSupport.Select(element =>
            {
                var template = definition.InitialPlacementCombatFacts.Single(value =>
                    value.ElementId == ZocReactionContentTestData.FirstElementId);
                return new ContentInitialPlacementCombatFacts(
                    template.ScenarioId,
                    element.ElementId,
                    template.InitialComponentToes.Select(toe => new ContentInitialComponentToe(
                        toe.ComponentId.Replace(
                            ZocReactionContentTestData.FirstElementId,
                            element.ElementId,
                            StringComparison.Ordinal),
                        toe.CurrentToe,
                        toe.Origin)));
            }))
            .ToArray();
        return ContentPackV5Artifact.Create(new ContentPackV5Definition(
            changedLegacy,
            combatFacts,
            placementFacts));
    }

    private static ContentPackV5Definition RenameReactor(
        ContentPackV5Definition definition,
        string reactorElementId)
    {
        const string originalElementId = ZocReactionContentTestData.SecondElementId;
        var legacy = definition.LegacyDefinition;
        var originalFacts = definition.ElementCombatFacts.Single(value =>
            value.ElementId == originalElementId);
        var componentIds = originalFacts.Components.ToDictionary(
            value => value.ComponentId,
            value => value.ComponentId.Replace(
                originalElementId,
                reactorElementId,
                StringComparison.Ordinal),
            StringComparer.Ordinal);
        var elements = legacy.Elements.Select(element =>
            element.ElementId == originalElementId
                ? new ContentCombatElement(
                    reactorElementId,
                    element.SideId,
                    element.ParentFormationId,
                    element.OrganizationId,
                    element.MobilityId,
                    element.BaseCapabilityPointAllowance,
                    element.PlacementMode,
                    element.Origin,
                    element.BreakdownVehicleCohort)
                : element).ToArray();
        var scenarios = legacy.Scenarios.Select(scenario => new ContentScenario(
            scenario.ScenarioId,
            scenario.Start,
            scenario.End,
            scenario.InitialPlacements.Select(placement =>
                placement.ElementId == originalElementId
                    ? new ContentInitialPlacement(
                        reactorElementId,
                        placement.LocationId,
                        placement.Origin)
                    : placement),
            scenario.Origin)).ToArray();
        var changedLegacy = new ContentPackDefinition(
            legacy.SchemaVersion,
            legacy.FormatId,
            legacy.PackId,
            legacy.RulesetId,
            legacy.Capabilities,
            legacy.SourceIndex,
            legacy.Locations,
            legacy.WeatherAreaAssignments,
            legacy.Edges,
            legacy.Formations,
            elements,
            scenarios);
        var combatFacts = definition.ElementCombatFacts.Select(facts =>
            facts.ElementId == originalElementId
                ? new ContentElementCombatFacts(
                    reactorElementId,
                    facts.CombatClassificationId,
                    facts.Components.Select(component => new ContentCombatComponent(
                        componentIds[component.ComponentId],
                        component.ComponentClassId,
                        component.MaximumToe,
                        component.DefensiveCloseAssaultRating,
                        component.Origin)),
                    facts.Origin)
                : facts).ToArray();
        var placementFacts = definition.InitialPlacementCombatFacts.Select(facts =>
            facts.ElementId == originalElementId
                ? new ContentInitialPlacementCombatFacts(
                    facts.ScenarioId,
                    reactorElementId,
                    facts.InitialComponentToes.Select(toe => new ContentInitialComponentToe(
                        componentIds[toe.ComponentId],
                        toe.CurrentToe,
                        toe.Origin)))
                : facts).ToArray();
        return new ContentPackV5Definition(changedLegacy, combatFacts, placementFacts);
    }

    private static RuleReference Source(string locator) =>
        new("spi-1979-land-rules", locator);
}
