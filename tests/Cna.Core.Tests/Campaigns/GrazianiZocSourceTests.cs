using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiZocSourceTests
{
    private static readonly string[] Members = ["3-coldstream-guards", "1-kings-royal-rifle-corps"];
    private static readonly LandSide[] Sides = [LandSide.Axis, LandSide.Commonwealth];

    private static (ContentScheduledCombatPackArtifact Artifact, CampaignWorldSnapshotV6 World) SourceExcerpt()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var original = Assert.Single(baseline.Scenarios);
        var placeholder = ContentTestData.Origin("fixture.zoc-topology");
        var recorded = Cna1979GrazianiSourceFacts.CommonwealthInitialGroupCandidates[1];
        var group = new ContentInitialGroup(recorded.GroupId, recorded.SideId, recorded.LocationId,
            Members, recorded.Origin);
        // Only two real co-located members are selected; topology, mobility and formation are placeholders.
        var formation = new ContentFormation("fixture-cw", "commonwealth", null,
            "land.organization.regiment", placeholder);
        var elements = Members.Select(id => new ContentCombatElement(id, "commonwealth", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, placeholder));
        var scenario = new ContentScenario(original.ScenarioId, original.Start, original.End,
            original.InitialPlacements, [], [], [group], original.Origin);
        var legacy = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("c3922", "land.terrain.clear", null, placeholder)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "c3922", [], placeholder)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. elements], scenarios: [scenario]);
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, placeholder)], placeholder);
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element",
            [new("fixture-axis-toe", 10, placeholder)]);
        var facts = Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts.Where(value => Members.Contains(value.ElementId));
        var seeds = Cna1979GrazianiSourceFacts.CreateCommonwealthInitialToeSeeds(scenario.ScenarioId)
            .Where(value => Members.Contains(value.ElementId));
        var artifact = ContentScheduledCombatPackArtifact.Create(new ContentPackV5Definition(legacy,
            [axis, .. facts], [axisSeed, .. seeds]));
        var world = CampaignWorldV8Factory.CreateInitial(artifact, scenario.ScenarioId,
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        return (artifact, world.World);
    }

    [Fact]
    public void RealColocatedInfantryProduceZocThroughNormalWorldCalculation()
    {
        var (artifact, world) = SourceExcerpt();
        var result = CampaignElementMovedV3Factory.DeriveZocAuthority(world,
            artifact.Definition, LandSide.Commonwealth);
        Assert.Equal("west", Assert.Single(result.ControlledLocationIds));
        Assert.Equal(2, result.SourceRepresentationIds.Count);

        var withoutSecond = new CampaignWorldSnapshotV6(6,
            world.Elements.Where(value => value.ElementId != Members[1]),
            world.Representations.Where(value => !value.BoundElementIds.Contains(Members[1])), []);
        Assert.Empty(CampaignElementMovedV3Factory.DeriveZocAuthority(withoutSecond,
            artifact.Definition, LandSide.Commonwealth).ControlledLocationIds);
    }

    [Fact]
    public void StackBelowDefensiveThresholdDoesNotProjectZoc()
    {
        var (artifact, world) = SourceExcerpt();
        var weakened = new CampaignWorldSnapshotV6(6, world.Elements.Select(element =>
            Members.Contains(element.ElementId)
                ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                    element.OperationalState, element.Components.Select(component =>
                        new CampaignComponentToeState(component.ComponentId, 2, component.InitialToeOrigin)))
                : element), world.Representations, []);
        Assert.Empty(CampaignElementMovedV3Factory.DeriveZocAuthority(weakened,
            artifact.Definition, LandSide.Commonwealth).ControlledLocationIds);
    }

    [Fact]
    public void ExactlyTenDefensivePointsQualifyButDepletedCohesionDoesNot()
    {
        var (artifact, world) = SourceExcerpt();
        var atThreshold = new CampaignWorldSnapshotV6(6, world.Elements.Select(element =>
            Members.Contains(element.ElementId)
                ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                    element.OperationalState, element.Components.Select(component =>
                        new CampaignComponentToeState(component.ComponentId,
                            element.ElementId == Members[0] ? 2 : 3, component.InitialToeOrigin)))
                : element), world.Representations, []);
        Assert.Equal("west", Assert.Single(CampaignElementMovedV3Factory.DeriveZocAuthority(atThreshold,
            artifact.Definition, LandSide.Commonwealth).ControlledLocationIds));
        var depleted = new CampaignWorldSnapshotV6(6, atThreshold.Elements.Select(element =>
            Members.Contains(element.ElementId)
                ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                    new CampaignElementOperationalStateV5(element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage, element.OperationalState.CapabilityPointsExpended,
                        -26, element.OperationalState.VehicleBreakdownState, element.OperationalState.MovementEnded),
                    element.Components)
                : element), atThreshold.Representations, []);
        Assert.Empty(CampaignElementMovedV3Factory.DeriveZocAuthority(depleted,
            artifact.Definition, LandSide.Commonwealth).ControlledLocationIds);
    }

    [Fact]
    public void HistoricalArmorAndAntiTankRatingsContributeToZocWithoutAdmittingCombat()
    {
        var armor = Assert.Single(Cna1979GrazianiSourceFacts.SecondRoyalTankRegimentCombatFacts.Components);
        var antiTank = Assert.Single(Cna1979GrazianiSourceFacts.PolishAntiTankCombatFacts.Components);
        var source = new[]
        {
            new ZocDefensiveCloseAssaultComponentFact(armor.ComponentClassId, 2, armor.DefensiveCloseAssaultRating),
            new ZocDefensiveCloseAssaultComponentFact(antiTank.ComponentClassId, 2, antiTank.DefensiveCloseAssaultRating),
        };
        Assert.Equal(8, Cna1979Combat.CalculateRawDefensivePointsForZoc(source).RawDefensiveCloseAssaultPoints);
        Assert.Throws<ArgumentException>(() => Cna1979Combat.CalculateRawDefensiveCloseAssaultPoints(source));
        var artillery = Assert.Single(Cna1979GrazianiSourceFacts.NzFieldArtilleryCombatFacts.Components);
        Assert.Throws<ArgumentException>(() => Cna1979Combat.CalculateRawDefensivePointsForZoc(
            [new ZocDefensiveCloseAssaultComponentFact(artillery.ComponentClassId, 6, artillery.DefensiveCloseAssaultRating)]));

        var (artifact, world) = SourceExcerpt();
        var weakened = new CampaignWorldSnapshotV6(6, world.Elements.Select(element =>
            Members.Contains(element.ElementId)
                ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                    element.OperationalState, element.Components.Select(component =>
                        new CampaignComponentToeState(component.ComponentId, 2, component.InitialToeOrigin)))
                : element), world.Representations, []);
        Assert.Empty(CampaignElementMovedV3Factory.DeriveZocAuthority(weakened,
            artifact.Definition, LandSide.Commonwealth).ControlledLocationIds);
        // Hypothetical placement checks the shared World calculation; the tank
        // itself remains a scheduled source record, not an initial deployment.
        var facts = artifact.Definition.ElementCombatFacts.Select(value =>
        {
            if (value.ElementId != Members[0]) return value;
            var component = Assert.Single(value.Components);
            return new ContentElementCombatFacts(value.ElementId, value.CombatClassificationId,
                [new ContentCombatComponent(component.ComponentId, armor.ComponentClassId,
                    component.MaximumToe, armor.DefensiveCloseAssaultRating, component.Origin)], value.Origin);
        }).ToArray();
        var definition = new ContentPackV5Definition(artifact.Definition.LegacyDefinition, facts,
            artifact.Definition.InitialPlacementCombatFacts);
        Assert.Equal("west", Assert.Single(CampaignElementMovedV3Factory.DeriveZocAuthority(weakened,
            definition, LandSide.Commonwealth).ControlledLocationIds));
    }

    [Fact]
    public void World6CalculationMatchesExistingSourceBackedWorld5Rules()
    {
        var artifact = ContentPackV5Artifact.Create(ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        var prior = CampaignWorldV5Factory.CreateInitial(artifact, scenario);
        var current = new CampaignWorldSnapshotV6(6, prior.Elements, prior.Representations, []);
        foreach (var side in Sides)
        {
            var expected = CampaignElementMovedV2Factory.DeriveZocAuthority(prior, artifact, scenario, side);
            var actual = CampaignElementMovedV3Factory.DeriveZocAuthority(current, artifact.Definition, side);
            Assert.Equal(expected.ControlledLocationIds, actual.ControlledLocationIds);
            Assert.Equal(expected.SourceRepresentationIds, actual.SourceRepresentationIds);
        }
    }

    [Fact]
    public void DuplicateRepresentationCannotInflateZocStacking()
    {
        var (artifact, world) = SourceExcerpt();
        var duplicate = new CampaignMapRepresentationState("forged-representation", "c3922",
            CampaignMapRepresentationBindingKind.IndependentElement, [Members[0]]);
        var forged = new CampaignWorldSnapshotV6(6, world.Elements, [.. world.Representations, duplicate], []);
        Assert.Throws<InvalidOperationException>(() => CampaignElementMovedV3Factory.DeriveZocAuthority(forged,
            artifact.Definition, LandSide.Commonwealth));
    }

    [Fact]
    public void OpposingClassificationDoesNotChangeOwnZocCalculation()
    {
        var (artifact, world) = SourceExcerpt();
        var axis = artifact.Definition.ElementCombatFacts.Single(value => value.ElementId == "axis-element");
        var unknownAxis = new ContentElementCombatFacts(axis.ElementId, "land.combat-classification.unresolved",
            axis.Components, axis.Origin);
        var changed = artifact.Definition.ElementCombatFacts.Select(value =>
            value.ElementId == axis.ElementId ? unknownAxis : value).ToArray();
        var changedDefinition = new ContentPackV5Definition(artifact.Definition.LegacyDefinition, changed,
            artifact.Definition.InitialPlacementCombatFacts);
        var result = CampaignElementMovedV3Factory.DeriveZocAuthority(world,
            changedDefinition, LandSide.Commonwealth);
        Assert.Equal("west", Assert.Single(result.ControlledLocationIds));
        Assert.Throws<InvalidOperationException>(() => CampaignElementMovedV3Factory.DeriveZocAuthority(world,
            changedDefinition, LandSide.Axis));
    }

    [Fact]
    public void UnresolvedParenthesizedDefenseCannotBeUsedAsOrdinaryStrength()
    {
        var (artifact, world) = SourceExcerpt();
        var original = artifact.Definition.ElementCombatFacts.Single(value => value.ElementId == Members[0]);
        var component = Assert.Single(original.Components);
        var marked = new ContentCombatComponent(component.ComponentId, component.ComponentClassId,
            component.MaximumToe, component.DefensiveCloseAssaultRating, component.Origin,
            defensiveRatingParenthesized: true);
        var changed = new ContentElementCombatFacts(original.ElementId, original.CombatClassificationId,
            [marked], original.Origin);
        var facts = artifact.Definition.ElementCombatFacts.Select(value =>
            value.ElementId == changed.ElementId ? changed : value).ToArray();
        var definition = new ContentPackV5Definition(artifact.Definition.LegacyDefinition, facts,
            artifact.Definition.InitialPlacementCombatFacts);
        Assert.Throws<InvalidOperationException>(() => CampaignElementMovedV3Factory.DeriveZocAuthority(world,
            definition, LandSide.Commonwealth));
    }

    [Fact]
    public void HeadquartersCannotBeIgnoredWhileAttachmentAuthorityIsUnknown()
    {
        var (artifact, world) = SourceExcerpt();
        var original = artifact.Definition.ElementCombatFacts.Single(value => value.ElementId == Members[0]);
        var headquarters = new ContentElementCombatFacts(original.ElementId,
            Cna1979Combat.HeadquartersClassificationId, original.Components, original.Origin);
        var facts = artifact.Definition.ElementCombatFacts.Select(value =>
            value.ElementId == headquarters.ElementId ? headquarters : value).ToArray();
        var definition = new ContentPackV5Definition(artifact.Definition.LegacyDefinition, facts,
            artifact.Definition.InitialPlacementCombatFacts);
        Assert.Throws<InvalidOperationException>(() => CampaignElementMovedV3Factory.DeriveZocAuthority(
            world, definition, LandSide.Commonwealth));
    }
}
