using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignWorldV8Tests
{
    private static readonly string[] SourceExcerptElements = ["2-scots-guards", "31-field-artillery", "1-royal-sussex"];

    [Fact]
    public void SharedWorldComponentValidationEnforcesCireneArtilleryPoolForCurrentState()
    {
        var facts = Assert.Single(Cna1979GrazianiSourceFacts.CireneMarmaricaArtilleryCombatFacts,
            value => value.ElementId == "45-artillery");
        var seed = Assert.Single(Cna1979GrazianiSourceFacts.CreateCireneMarmaricaArtilleryToeSeeds("graziani"),
            value => value.ElementId == facts.ElementId);
        var valid = seed.InitialComponentToes.Select(value =>
            new CampaignComponentToeState(value.ComponentId, value.CurrentToe, value.Origin)).ToArray();
        Assert.True(CampaignWorldV6Validator.HasValidComponents(valid, facts, seed.InitialComponentToes));
        // Total remains twelve, but ten guns exceed the separate nine-gun limit.
        var overGunPool = valid.Select(value => new CampaignComponentToeState(value.ComponentId,
            value.ComponentId.EndsWith(".75-18-gun-howitzer", StringComparison.Ordinal) ? 7
                : value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal) ? 2 : value.CurrentToe,
            value.InitialToeOrigin)).ToArray();
        Assert.False(CampaignWorldV6Validator.HasValidComponents(overGunPool, facts, seed.InitialComponentToes));
    }

    [Theory]
    [InlineData("203-artillery")]
    [InlineData("201-ccnn-artillery")]
    [InlineData("204-ccnn-artillery")]
    public void SourceArtilleryGunPoolCannotBeExceededByTradingAaStrength(string elementId)
    {
        var facts = Cna1979GrazianiSourceFacts.CatanzaroCombatCandidates
            .Concat(Cna1979GrazianiSourceFacts.FirstCcnnCombatCandidates)
            .Concat(Cna1979GrazianiSourceFacts.FourthCcnnCombatCandidates)
            .Single(value => value.ElementId == elementId);
        var seed = Cna1979GrazianiSourceFacts.CreateCatanzaroToeCandidates("graziani")
            .Concat(Cna1979GrazianiSourceFacts.CreateFirstCcnnToeCandidates("graziani"))
            .Concat(Cna1979GrazianiSourceFacts.CreateFourthCcnnToeCandidates("graziani"))
            .Single(value => value.ElementId == elementId);
        var current = seed.InitialComponentToes.Select(value =>
            new CampaignComponentToeState(value.ComponentId, value.CurrentToe, value.Origin)).ToArray();
        Assert.True(CampaignWorldV6Validator.HasValidComponents(current, facts, seed.InitialComponentToes));
        var invalid = current.Select(value => new CampaignComponentToeState(value.ComponentId,
            value.ComponentId.EndsWith(".heavy-aa", StringComparison.Ordinal)
                || value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal) ? value.CurrentToe - 1
                : value.CurrentToe == 6 ? 7 : value.CurrentToe, value.InitialToeOrigin)).ToArray();
        Assert.Equal(current.Sum(value => value.CurrentToe), invalid.Sum(value => value.CurrentToe));
        Assert.False(CampaignWorldV6Validator.HasValidComponents(invalid, facts, seed.InitialComponentToes));
    }

    private static ContentScheduledCombatPackArtifact BuildSourceExcerpt(bool attachmentOnly = false)
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var original = Assert.Single(baseline.Scenarios);
        var fixture = ContentTestData.Origin("fixture.world-v8");
        var sourceGroup = Cna1979GrazianiSourceFacts.CommonwealthInitialGroupCandidates[0];
        var group = sourceGroup;
        var sourceArrival = Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival;
        var arrival = new ContentScheduledArrival(sourceArrival.GroupId, sourceArrival.SideId,
            sourceArrival.Arrival, ["1-royal-sussex"], sourceArrival.EntryAreaId, sourceArrival.Origin,
            sourceArrival.Trucks);
        // Arrival membership is excerpted; topology, formation and mobility are test placeholders.
        var formation = new ContentFormation("fixture-commonwealth", "commonwealth", null,
            "land.organization.regiment", fixture);
        var elements = SourceExcerptElements.Select(id =>
            new ContentCombatElement(id, "commonwealth", formation.FormationId, "land.organization.battalion",
                Cna1979Movement.MotorizedMobilityId, 20,
                attachmentOnly && id == "2-scots-guards" ? ContentPlacementMode.AttachmentOnly : ContentPlacementMode.Independent,
                fixture));
        var scenario = new ContentScenario(original.ScenarioId, original.Start, new(6, 3),
            original.InitialPlacements, [arrival],
            [new ContentLocationArea(Cna1979GrazianiSourceFacts.CairoAreaId, ["east"], fixture)], [group], original.Origin);
        var legacy = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("c4131", "land.terrain.clear", null, fixture)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "c4131", [], fixture)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. elements], scenarios: [scenario]);
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, fixture)], fixture);
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element",
            [new("fixture-axis-toe", 10, fixture)]);
        var scots = Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts.Single(value => value.ElementId == "2-scots-guards");
        var artillery = Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts.Single(value => value.ElementId == "31-field-artillery");
        var sussex = Cna1979GrazianiSourceFacts.ArrivingInfantryCombatFacts.Single(value => value.ElementId == "1-royal-sussex");
        var seeds = Cna1979GrazianiSourceFacts.CreateCommonwealthInitialToeSeeds(scenario.ScenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateCommonwealthArrivalToeSeeds(scenario.ScenarioId))
            .Where(value => value.ElementId is "2-scots-guards" or "31-field-artillery" or "1-royal-sussex");
        return ContentScheduledCombatPackArtifact.Create(new ContentPackV5Definition(legacy,
            [axis, scots, artillery, sussex], [axisSeed, .. seeds]));
    }

    [Fact]
    public void CreatesGroupedSourceExcerptAndKeepsScheduledMemberOffMap()
    {
        var artifact = BuildSourceExcerpt();
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        var expanded = ContentScenarioInitialPlacements.Expand(scenario).ToArray();
        Assert.Equal("c4131", Assert.Single(expanded, value => value.ElementId == "2-scots-guards").LocationId);
        Assert.Equal("c4131", Assert.Single(expanded, value => value.ElementId == "31-field-artillery").LocationId);
        Assert.DoesNotContain(expanded, value => value.ElementId == "1-royal-sussex");
        var world = CampaignWorldV8Factory.CreateInitial(artifact, "minimal-lab",
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        Assert.Equal(artifact.Identity, world.ContentIdentity);
        var scots = Assert.Single(world.World.Elements, value => value.ElementId == "2-scots-guards");
        Assert.Equal("c4131", scots.CurrentLocationId);
        Assert.Equal(6, Assert.Single(scots.Components).CurrentToe);
        var artillery = Assert.Single(world.World.Elements, value => value.ElementId == "31-field-artillery");
        Assert.Equal("c4131", artillery.CurrentLocationId);
        Assert.Equal(6, Assert.Single(artillery.Components).CurrentToe);
        var groupRepresentations = world.World.Representations.Where(value => value.CurrentLocationId == "c4131").ToArray();
        Assert.Equal(2, groupRepresentations.Length);
        Assert.All(groupRepresentations, representation =>
        {
            Assert.Equal(CampaignMapRepresentationBindingKind.IndependentElement, representation.BindingKind);
            Assert.Single(representation.BoundElementIds);
        });
        Assert.DoesNotContain(world.World.Elements, value => value.ElementId == "1-royal-sussex");
        Assert.DoesNotContain(world.World.Representations, value => value.BoundElementIds.Contains("1-royal-sussex"));
        Assert.Equal("1-royal-sussex", Assert.Single(Assert.Single(world.PendingArrivals).ElementIds));
        Assert.Equal(new ContentScenarioBoundary(5, 1), Assert.Single(world.PendingArrivals).Arrival);
        var combatIssues = ContentPackV5Validator.ValidateScheduledContent(artifact.Definition).Issues;
        Assert.Contains(combatIssues, issue => issue.Code == "content.combat.offensive-rating-unsupported");
        Assert.Contains(combatIssues, issue => issue.Code == "content.combat.barrage-rating-unsupported");
        Assert.Throws<ArgumentException>(() => CampaignElementMovedV3Factory.DeriveZocAuthority(world.World,
            artifact.Definition, LandSide.Commonwealth));
    }

    [Fact]
    public void CreatesSixteenthBrigadeSourceGroupWithoutInventingAttachments()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var original = Assert.Single(baseline.Scenarios);
        var source = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate;
        var placeholder = ContentTestData.Origin("fixture.sixteenth-topology-and-mobility");
        var formation = new ContentFormation("fixture-sixteenth", "commonwealth", null,
            "land.organization.regiment", placeholder);
        var elements = source.ElementIds.Select(id => new ContentCombatElement(id, "commonwealth",
            formation.FormationId, "land.organization.battalion", Cna1979Movement.NonMotorizedMobilityId,
            20, ContentPlacementMode.Independent, placeholder));
        var scenario = new ContentScenario(original.ScenarioId, original.Start, original.End,
            original.InitialPlacements, [], [], [source], original.Origin);
        var legacy = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("e1829", "land.terrain.clear", null, placeholder)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "e1829", [], placeholder)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. elements], scenarios: [scenario]);
        var fixture = ContentTestData.Origin("fixture.axis");
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, fixture)], fixture);
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element",
            [new("fixture-axis-toe", 10, fixture)]);
        var artifact = ContentScheduledCombatPackArtifact.Create(new ContentPackV5Definition(legacy,
            [axis, .. Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeCombatCandidates],
            [axisSeed, .. Cna1979GrazianiSourceFacts.CreateSixteenthInfantryBrigadeToeCandidates(scenario.ScenarioId)]));
        var world = CampaignWorldV8Factory.CreateInitial(artifact, scenario.ScenarioId,
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        Assert.Equal(4, world.World.Elements.Count(value => value.CurrentLocationId == "e1829"));
        Assert.All(world.World.Representations.Where(value => value.CurrentLocationId == "e1829"), value =>
        {
            Assert.Equal(CampaignMapRepresentationBindingKind.IndependentElement, value.BindingKind);
            Assert.Single(value.BoundElementIds);
        });
        Assert.Equal(3, Assert.Single(Assert.Single(world.World.Elements,
            value => value.ElementId == "16-infantry-brigade-hq").Components).CurrentToe);
    }

    [Fact]
    public void CatanzaroSourceSubsetRoundTripsAndInitializesItsMixedComponents()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var original = Assert.Single(baseline.Scenarios);
        var source = Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.CatanzaroCombatCandidates;
        var placeholder = ContentTestData.Origin("fixture.catanzaro-topology-and-mobility");
        var subset = new ContentInitialGroup("catanzaro-tested-subset", source.SideId, source.LocationId,
            facts.Select(value => value.ElementId), source.Origin);
        var formation = new ContentFormation("fixture-catanzaro", "axis", null,
            "land.organization.regiment", placeholder);
        var elements = facts.Select(value => new ContentCombatElement(value.ElementId, "axis",
            formation.FormationId, "land.organization.battalion", Cna1979Movement.NonMotorizedMobilityId,
            20, ContentPlacementMode.Independent, placeholder));
        var scenario = new ContentScenario(original.ScenarioId, original.Start, original.End,
            original.InitialPlacements, [], [], [subset], original.Origin);
        var legacy = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.AxisChartsSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("c4707", "land.terrain.clear", null, placeholder)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "c4707", [], placeholder)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. elements], scenarios: [scenario]);
        var fixture = ContentTestData.Origin("fixture.axis");
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, fixture)], fixture);
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element",
            [new("fixture-axis-toe", 10, fixture)]);
        var artifact = ContentScheduledCombatPackArtifact.Create(new ContentPackV5Definition(legacy,
            [axis, .. facts], [axisSeed, .. Cna1979GrazianiSourceFacts.CreateCatanzaroToeCandidates(scenario.ScenarioId)]));
        var reopened = ContentScheduledCombatPackArtifact.Read(artifact.CanonicalBytes, artifact.Identity);
        var world = CampaignWorldV8Factory.CreateInitial(reopened, scenario.ScenarioId,
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        Assert.Equal(11, world.World.Elements.Count(value => value.CurrentLocationId == "c4707"));
        var artillery = Assert.Single(world.World.Elements, value => value.ElementId == "203-artillery");
        Assert.Equal(12, artillery.Components.Sum(value => value.CurrentToe));
        Assert.Equal(3, Assert.Single(artillery.Components,
            value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.DoesNotContain(world.World.Elements, value => value.ElementId is "64-catanzaro-hq" or "64-engineer");
    }

    [Fact]
    public void NormalOwnerProjectionOmitsDeferredUnitAndOpposingForces()
    {
        var artifact = BuildSourceExcerpt();
        var world = CampaignWorldV8Factory.CreateInitial(artifact, "minimal-lab",
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        var definition = artifact.Definition.LegacyDefinition;
        var commonwealth = CampaignOwnElementProjection.ProjectPlaced(definition, world.World, LandSide.Commonwealth);
        Assert.Contains(commonwealth, unit => unit.ElementId == "2-scots-guards");
        Assert.Contains(commonwealth, unit => unit.ElementId == "31-field-artillery");
        Assert.DoesNotContain(commonwealth, unit => unit.ElementId == "1-royal-sussex");
        Assert.DoesNotContain(commonwealth, unit => unit.ElementId == "axis-element");
        var axis = CampaignOwnElementProjection.ProjectPlaced(definition, world.World, LandSide.Axis);
        Assert.Equal("axis-element", Assert.Single(axis).ElementId);

        // Exercise the same owner projection at the two world cuts surrounding
        // a placement; this does not claim an admitted arrival or legal hex.
        var template = Assert.Single(world.World.Elements, value => value.ElementId == "2-scots-guards");
        var seed = Assert.Single(artifact.Definition.InitialPlacementCombatFacts,
            value => value.ElementId == "1-royal-sussex");
        var placed = new CampaignElementStateV5("1-royal-sussex", "east", CampaignElementReserveStatus.None,
            template.OperationalState, seed.InitialComponentToes.Select(value =>
                new CampaignComponentToeState(value.ComponentId, value.CurrentToe, value.Origin)));
        var withoutRepresentation = new CampaignWorldSnapshotV6(6, [.. world.World.Elements, placed],
            world.World.Representations, world.World.BrokenVehicleLots);
        Assert.DoesNotContain(CampaignOwnElementProjection.ProjectPlaced(definition, withoutRepresentation, LandSide.Commonwealth),
            value => value.ElementId == "1-royal-sussex");
        var representation = new CampaignMapRepresentationState("arrival-test-representation", "east",
            CampaignMapRepresentationBindingKind.IndependentElement, ["1-royal-sussex"]);
        var withRepresentation = new CampaignWorldSnapshotV6(6, withoutRepresentation.Elements,
            [.. withoutRepresentation.Representations, representation], withoutRepresentation.BrokenVehicleLots);
        Assert.Contains(CampaignOwnElementProjection.ProjectPlaced(definition, withRepresentation, LandSide.Commonwealth),
            value => value.ElementId == "1-royal-sussex");
        Assert.DoesNotContain(CampaignOwnElementProjection.ProjectPlaced(definition, withRepresentation, LandSide.Axis),
            value => value.ElementId == "1-royal-sussex");

    }

    [Fact]
    public void ReopenedContentProducesSameWorldAndUnknownScenarioIsRejected()
    {
        var artifact = BuildSourceExcerpt();
        var reopened = ContentScheduledCombatPackArtifact.Read(artifact.CanonicalBytes, artifact.Identity);
        var initialization = new CampaignWorldV8Initialization(CapabilityPointAmount.Zero, 0,
            CampaignElementReserveStatus.None);
        var first = CampaignWorldV8Factory.CreateInitial(artifact, "minimal-lab", initialization);
        var second = CampaignWorldV8Factory.CreateInitial(reopened, "minimal-lab", initialization);
        Assert.Equal(first.ContentIdentity, second.ContentIdentity);
        Assert.Equal(first.World, second.World);
        Assert.Equal(first.PendingArrivals, second.PendingArrivals);
        Assert.Equal(first, second);
        Assert.Throws<ArgumentException>(() => CampaignWorldV8Factory.CreateInitial(reopened, "unknown", initialization));
    }

    [Fact]
    public void DoesNotInventAttachmentForGroupedAttachmentOnlyElement()
    {
        var artifact = BuildSourceExcerpt(attachmentOnly: true);
        Assert.Throws<InvalidOperationException>(() => CampaignWorldV8Factory.CreateInitial(artifact,
            "minimal-lab", new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None)));
    }

    [Fact]
    public void RejectsPendingGroupsThatRepeatOrAreAlreadyOnMap()
    {
        var artifact = BuildSourceExcerpt();
        var world = CampaignWorldV8Factory.CreateInitial(artifact, "minimal-lab",
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        var arrival = Assert.Single(world.PendingArrivals);
        Assert.Throws<ArgumentException>(() => new CampaignWorldSnapshotV8(world.ContentIdentity,
            world.ScenarioId, world.Initialization, world.World, [arrival, arrival]));
        var alreadyPresent = new ContentScheduledArrival("duplicate", arrival.SideId, arrival.Arrival,
            ["2-scots-guards"], arrival.EntryAreaId, arrival.Origin);
        Assert.Throws<ArgumentException>(() => new CampaignWorldSnapshotV8(world.ContentIdentity,
            world.ScenarioId, world.Initialization, world.World, [alreadyPresent]));
    }

    [Fact]
    public void RejectsInitialExpenditureAboveAnElementsAllowance()
    {
        var artifact = BuildSourceExcerpt();
        Assert.Throws<InvalidOperationException>(() => CampaignWorldV8Factory.CreateInitial(artifact,
            "minimal-lab", new(new CapabilityPointAmount(21, 1), 0, CampaignElementReserveStatus.None)));
    }

    [Fact]
    public void DoesNotApplyOneSidesReserveDesignationToBothSides()
    {
        var artifact = BuildSourceExcerpt();
        Assert.Throws<InvalidOperationException>(() => CampaignWorldV8Factory.CreateInitial(artifact,
            "minimal-lab", new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.ReserveI)));
    }
}
