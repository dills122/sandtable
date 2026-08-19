using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Observations;

internal static class CampaignObservationTestData
{
    public static (CampaignSnapshot BaselineSnapshot, CampaignContentContext BaselineContext,
        CampaignSnapshot ChangedSnapshot, CampaignContentContext ChangedContext)
        CreateOpponentOnlyPair(LandSide observer)
    {
        var baselineArtifact = Cna1979SyntheticContentCatalog.Artifact;
        var changedArtifact = CreateChangedOpponentArtifact(baselineArtifact.Definition, observer);
        var baselineContext = CampaignContentContext.Create(
            baselineArtifact,
            "movement-contact-lab");
        var changedContext = CampaignContentContext.Create(
            changedArtifact,
            "movement-contact-lab");

        return (
            CreateSnapshot(baselineContext),
            baselineContext,
            CreateSnapshot(changedContext),
            changedContext);
    }

    private static ContentPackArtifact CreateChangedOpponentArtifact(
        ContentPackDefinition baseline,
        LandSide observer)
    {
        var observerSideId = observer switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => throw new ArgumentOutOfRangeException(nameof(observer)),
        };
        var opponentSideId = observer == LandSide.Axis ? "commonwealth" : "axis";
        var source = baseline.SourceIndex[0].SourceId;
        var origin = new Func<string, ContentOrigin>(locator => new(
            ContentOriginKind.Synthetic,
            [new RuleReference(source, $"privacy.{locator}")]));
        var observerFormations = baseline.Formations
            .Where(formation => formation.SideId == observerSideId)
            .ToArray();
        var enemyFormation = new ContentFormation(
            "enemy-sentinel-formation",
            opponentSideId,
            null,
            "land.organization.battalion",
            origin("formation.enemy-sentinel"));
        var observerElements = baseline.Elements
            .Where(element => element.SideId == observerSideId)
            .ToArray();
        var observerElementIds = observerElements
            .Select(element => element.ElementId)
            .ToHashSet(StringComparer.Ordinal);
        ContentCombatElement[] enemyElements =
        [
            Enemy("enemy-sentinel-a", 31, "land.organization.regiment"),
            Enemy("enemy-sentinel-b", 32, "land.organization.battalion"),
            Enemy("enemy-sentinel-c", 33, "land.organization.regiment"),
        ];
        var changedScenarios = baseline.Scenarios.Select(ChangeScenario).ToArray();
        var definition = new ContentPackDefinition(
            baseline.SchemaVersion,
            baseline.FormatId,
            baseline.PackId,
            baseline.RulesetId,
            baseline.Capabilities,
            baseline.SourceIndex,
            baseline.Locations,
            baseline.WeatherAreaAssignments,
            baseline.Edges,
            observerFormations.Append(enemyFormation),
            observerElements.Concat(enemyElements),
            changedScenarios);

        var contractValidation = ContentPackValidator.Validate(definition);
        var compatibilityValidation = Cna1979ContentCompatibilityValidator.Validate(definition);

        Assert.True(
            contractValidation.IsValid,
            string.Join(Environment.NewLine, contractValidation.Issues.Select(issue =>
                $"{issue.Code} {issue.Path}: {issue.Message}")));
        Assert.True(
            compatibilityValidation.IsValid,
            string.Join(Environment.NewLine, compatibilityValidation.Issues.Select(issue =>
                $"{issue.Code} {issue.Path}: {issue.Message}")));
        return ContentPackArtifact.Create(definition);

        ContentCombatElement Enemy(string elementId, int capability, string organizationId) => new(
            elementId,
            opponentSideId,
            enemyFormation.FormationId,
            organizationId,
            capability,
            ContentPlacementMode.Independent,
            origin($"element.{elementId}"));

        ContentInitialPlacement Placement(string elementId, string locationId) => new(
            elementId,
            locationId,
            origin($"placement.{elementId}.{locationId}"));

        ContentScenario ChangeScenario(ContentScenario scenario) => new(
            scenario.ScenarioId,
            scenario.Start,
            scenario.End,
            scenario.InitialPlacements
                .Where(placement => observerElementIds.Contains(placement.ElementId))
                .Concat(
                [
                    Placement("enemy-sentinel-a", "center"),
                    Placement("enemy-sentinel-b", "north-east"),
                    Placement("enemy-sentinel-c", "south"),
                ]),
            origin($"scenario.{scenario.ScenarioId}.changed-opponent"));
    }

    private static CampaignSnapshot CreateSnapshot(CampaignContentContext context)
    {
        var catalogSetup = Cna1979SetupCatalog.Definitions[0];
        var setup = new CampaignSetupDefinition(
            catalogSetup.SchemaVersion,
            catalogSetup.SetupId,
            catalogSetup.DisplayName,
            catalogSetup.IsSynthetic,
            catalogSetup.InitialGameTurn,
            catalogSetup.InitialInitiative,
            catalogSetup.OpeningPreamble,
            catalogSetup.Weather,
            context.Selection,
            catalogSetup.Sources);

        return new CampaignSnapshot(
            4,
            "campaign-privacy",
            1,
            Cna1979Ruleset.Manifest.Hash,
            CampaignSetupSnapshot.FromDefinition(setup),
            CampaignWorldFactory.CreateInitial(context.Artifact, context.Scenario),
            null,
            [],
            SandtableRandom.Create(12345),
            Cna1979LandSequence.CreateTurn(setup.InitialGameTurn)[0]);
    }
}
