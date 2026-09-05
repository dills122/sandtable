using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignWorldV5Tests
{
    [Fact]
    public void InitialWorldCopiesExactComponentSeedsAndProvenance()
    {
        var artifact = ContentPackV5Artifact.Create(
            ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();

        var world = CampaignWorldV5Factory.CreateInitial(artifact, scenario);

        Assert.Equal(5, world.ContractVersion);
        Assert.Equal(
            [ZocReactionContentTestData.FirstElementId,
                ZocReactionContentTestData.SecondElementId],
            world.Elements.Select(element => element.ElementId));
        foreach (var element in world.Elements)
        {
            var component = Assert.Single(element.Components);
            var seed = artifact.Definition.InitialPlacementCombatFacts
                .Single(value => value.ElementId == element.ElementId)
                .InitialComponentToes.Single();
            Assert.Equal(seed.ComponentId, component.ComponentId);
            Assert.Equal(5, component.CurrentToe);
            Assert.Equal(seed.Origin, component.InitialToeOrigin);
            Assert.Null(element.OperationalState.MovementEnded);
        }

        Assert.True(CampaignWorldV5Validator.IsValidInitial(world, artifact, scenario));
    }

    [Fact]
    public void SuccessorStateDefensivelyCopiesAndCanonicallyOrdersComponents()
    {
        var components = new List<CampaignComponentToeState>
        {
            Component("component-b", 2),
            Component("component-a", 1),
        };
        var element = new CampaignElementStateV5(
            "axis-element",
            "west",
            CampaignElementReserveStatus.None,
            Operational(),
            components);

        components.Clear();

        Assert.Equal(
            ["component-a", "component-b"],
            element.Components.Select(component => component.ComponentId));
        Assert.Equal(
            ["ComponentId", "CurrentToe", "InitialToeOrigin"],
            typeof(CampaignComponentToeState).GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignElementStateV5(
            "axis-element",
            "west",
            CampaignElementReserveStatus.None,
            Operational(),
            [Component("component-a", 1), Component("component-a", 2)]));
    }

    [Fact]
    public void CurrentRawDefenseIsDerivedFromWorldToeAndImmutableContentRatings()
    {
        var artifact = ContentPackV5Artifact.Create(
            ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var world = CampaignWorldV5Factory.CreateInitial(artifact, scenario);
        var element = world.Elements.Single(value =>
            value.ElementId == ZocReactionContentTestData.FirstElementId);
        var changed = ReplaceElement(
            world,
            new CampaignElementStateV5(
                element.ElementId,
                element.CurrentLocationId,
                element.ReserveStatus,
                element.OperationalState,
                element.Components.Select(component => new CampaignComponentToeState(
                    component.ComponentId,
                    3,
                    component.InitialToeOrigin)).ToArray()));

        var initial = CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
            world,
            artifact,
            scenario,
            world.Representations.Select(value => value.RepresentationId));
        var current = CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
            changed,
            artifact,
            scenario,
            changed.Representations.Select(value => value.RepresentationId));

        Assert.Equal(10, initial.RawDefensiveCloseAssaultPoints);
        Assert.Equal(8, current.RawDefensiveCloseAssaultPoints);
        Assert.True(CampaignWorldV5Validator.IsValid(changed, artifact, scenario));
        Assert.Throws<ArgumentException>(() =>
            CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
                world,
                artifact,
                scenario,
                [world.Representations[0].RepresentationId,
                    world.Representations[0].RepresentationId]));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
                world,
                artifact,
                scenario,
                ["map-representation.9999"]));
    }

    [Fact]
    public void DerivationRejectsUnknownMissingAndOverMaximumComponentState()
    {
        var artifact = ContentPackV5Artifact.Create(
            ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var world = CampaignWorldV5Factory.CreateInitial(artifact, scenario);
        var element = world.Elements[0];

        CampaignWorldSnapshotV5 ChangeComponents(params CampaignComponentToeState[] components) =>
            ReplaceElement(world, new CampaignElementStateV5(
                element.ElementId,
                element.CurrentLocationId,
                element.ReserveStatus,
                element.OperationalState,
                components));

        var missing = ChangeComponents();
        var unknown = ChangeComponents(Component("unknown-component", 1));
        var overMaximum = ChangeComponents(new CampaignComponentToeState(
            element.Components[0].ComponentId,
            6,
            element.Components[0].InitialToeOrigin));

        Assert.False(CampaignWorldV5Validator.IsValid(missing, artifact, scenario));
        Assert.False(CampaignWorldV5Validator.IsValid(unknown, artifact, scenario));
        Assert.False(CampaignWorldV5Validator.IsValid(overMaximum, artifact, scenario));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
                missing, artifact, scenario,
                missing.Representations.Select(value => value.RepresentationId)));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
                unknown, artifact, scenario,
                unknown.Representations.Select(value => value.RepresentationId)));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
                overMaximum, artifact, scenario,
                overMaximum.Representations.Select(value => value.RepresentationId)));
    }

    [Fact]
    public void DerivationRejectsARepresentationSetThatCombinesOpposingSides()
    {
        var artifact = CreateMixedSideArtifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var world = CampaignWorldV5Factory.CreateInitial(artifact, scenario);

        Assert.Throws<InvalidOperationException>(() =>
            CampaignWorldV5CombatDerivation.CalculateRawDefensiveCloseAssaultPoints(
                world,
                artifact,
                scenario,
                world.Representations.Select(value => value.RepresentationId)));
    }

    [Fact]
    public void HistoricalFactoryIdentityRemainsAvailableAlongsideActiveSequence()
    {
        Assert.Equal(4, CampaignWorldSnapshot.CurrentContractVersion);
        Assert.Equal(9, CampaignSnapshot.CurrentContractVersion);
        Assert.Equal(3, Cna1979LandSequence.ContractVersion);
        Assert.Equal(3, Cna1979LandSequence.CatalogSchemaVersion);
        Assert.Equal(
            [typeof(ContentPackArtifact), typeof(ContentScenario)],
            typeof(CampaignWorldFactory).GetMethod(nameof(CampaignWorldFactory.CreateInitial))!
                .GetParameters().Select(parameter => parameter.ParameterType));
    }

    private static CampaignWorldSnapshotV5 ReplaceElement(
        CampaignWorldSnapshotV5 world,
        CampaignElementStateV5 replacement) => new(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            world.Elements.Select(element => element.ElementId == replacement.ElementId
                ? replacement
                : element).ToArray(),
            world.Representations);

    private static CampaignComponentToeState Component(string id, int currentToe) => new(
        id,
        currentToe,
        ContentTestData.Origin($"content.seed.{id}"));

    private static CampaignElementOperationalStateV5 Operational() => new(
        1,
        1,
        CapabilityPointAmount.Zero,
        0,
        null,
        null);

    private static ContentPackV5Artifact CreateMixedSideArtifact()
    {
        var definition = ZocReactionContentTestData.CreatePositiveFixture();
        var legacy = definition.LegacyDefinition;
        var commonwealthFormation = new ContentFormation(
            "commonwealth-formation",
            "commonwealth",
            null,
            legacy.Formations.Single().OrganizationId,
            ContentTestData.Origin("content.formation.commonwealth"));
        var elements = legacy.Elements.Select(element =>
            element.ElementId == ZocReactionContentTestData.SecondElementId
                ? new ContentCombatElement(
                    element.ElementId,
                    "commonwealth",
                    commonwealthFormation.FormationId,
                    element.OrganizationId,
                    element.MobilityId,
                    element.BaseCapabilityPointAllowance,
                    element.PlacementMode,
                    element.Origin,
                    element.BreakdownVehicleCohort)
                : element).ToArray();
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
            [.. legacy.Formations, commonwealthFormation],
            elements,
            legacy.Scenarios);
        return ContentPackV5Artifact.Create(new ContentPackV5Definition(
            changedLegacy,
            definition.ElementCombatFacts,
            definition.InitialPlacementCombatFacts));
    }
}
