using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

internal static class CampaignObservationTestData
{
    public static (CampaignSnapshot BaselineSnapshot, CampaignContentContext BaselineContext,
        CampaignSnapshot ChangedSnapshot, CampaignContentContext ChangedContext)
        CreateApparentEquivalentPair(LandSide observer)
    {
        var baselineArtifact = Cna1979SyntheticContentCatalog.Artifact;
        var changedArtifact = CreateHiddenOpponentArtifact(
            baselineArtifact.Definition,
            observer);
        return CreatePair(baselineArtifact, changedArtifact);
    }

    public static (CampaignSnapshot BaselineSnapshot, CampaignContentContext BaselineContext,
        CampaignSnapshot ChangedSnapshot, CampaignContentContext ChangedContext)
        CreateApparentLocationDeltaPair(LandSide observer)
    {
        var baselineArtifact = Cna1979SyntheticContentCatalog.Artifact;
        var changedArtifact = CreateApparentLocationDeltaArtifact(
            baselineArtifact.Definition,
            observer);
        return CreatePair(baselineArtifact, changedArtifact);
    }

    public static IReadOnlyList<CampaignSnapshot> AdvanceThroughMovement(
        CampaignSnapshot initial,
        CampaignContentContext context)
    {
        var advanced = StageEntryCampaignTestData.Advance(
            initial,
            context,
            InitiativeOrderChoice.ActLast);
        var reserve = advanced.Snapshot;
        var firstSide = FirstActingSideResolver.Resolve(reserve);
        var decision = CampaignEngine.Decide(
            reserve,
            new CompleteReserveDesignation(
                reserve.StateVersion,
                reserve.SequencePosition.PositionId,
                firstSide),
            context);
        Assert.True(decision.IsAccepted);
        var completed = Assert.Single(decision.Events);
        var movement = CampaignProjector.Apply(reserve, completed, context);

        return Array.AsReadOnly(advanced.Snapshots.Append(movement).ToArray());
    }

    private static (CampaignSnapshot BaselineSnapshot,
        CampaignContentContext BaselineContext,
        CampaignSnapshot ChangedSnapshot,
        CampaignContentContext ChangedContext) CreatePair(
            ContentPackArtifact baselineArtifact,
            ContentPackArtifact changedArtifact)
    {
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

    private static ContentPackArtifact CreateHiddenOpponentArtifact(
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
        var hiddenPrefix = $"{opponentSideId}-hidden";
        ContentCombatElement[] enemyElements =
        [
            Enemy($"{hiddenPrefix}-a", 31, "land.organization.regiment", false),
            Enemy($"{hiddenPrefix}-b", 32, "land.organization.regiment", true),
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

        ContentCombatElement Enemy(
            string elementId,
            int capability,
            string organizationId,
            bool isMotorized) => new(
            elementId,
            opponentSideId,
            enemyFormation.FormationId,
            organizationId,
            isMotorized
                ? Cna1979Movement.MotorizedMobilityId
                : Cna1979Movement.NonMotorizedMobilityId,
            capability,
            ContentPlacementMode.Independent,
            origin($"element.{elementId}"),
            isMotorized
                ? new ContentBreakdownVehicleCohort(
                    $"{elementId}.vehicle-cohort.trucks",
                    Cna1979Breakdown.VehicleTypeTruckId,
                    1,
                    Cna1979Breakdown.ProfileTruckId,
                    origin($"element.{elementId}.breakdown-cohort.trucks"))
                : null);

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
                    Placement($"{hiddenPrefix}-a", OpponentLocation("a")),
                    Placement($"{hiddenPrefix}-b", OpponentLocation("b")),
                ]),
            origin($"scenario.{scenario.ScenarioId}.changed-opponent"));

        string OpponentLocation(string suffix) => (opponentSideId, suffix) switch
        {
            ("axis", "a") => "west",
            ("axis", "b") => "north-west",
            ("commonwealth", "a") => "east",
            ("commonwealth", "b") => "south-east",
            _ => throw new ArgumentOutOfRangeException(nameof(suffix)),
        };
    }

    private static ContentPackArtifact CreateApparentLocationDeltaArtifact(
        ContentPackDefinition baseline,
        LandSide observer)
    {
        var opponentSideId = observer switch
        {
            LandSide.Axis => "commonwealth",
            LandSide.Commonwealth => "axis",
            _ => throw new ArgumentOutOfRangeException(nameof(observer)),
        };
        var source = baseline.SourceIndex[0].SourceId;
        ContentOrigin Origin(string locator) => new(
            ContentOriginKind.Synthetic,
            [new RuleReference(source, $"privacy.visible-delta.{locator}")]);
        var elementsById = baseline.Elements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);
        var changedElementId = baseline.Elements
            .Where(element => string.Equals(
                element.SideId,
                opponentSideId,
                StringComparison.Ordinal))
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .First()
            .ElementId;
        var changedLocationId = opponentSideId == "axis" ? "south-west" : "north-east";
        var changedScenarios = baseline.Scenarios.Select(scenario => new ContentScenario(
            scenario.ScenarioId,
            scenario.Start,
            scenario.End,
            scenario.InitialPlacements.Select(placement =>
                string.Equals(placement.ElementId, changedElementId, StringComparison.Ordinal)
                    ? new ContentInitialPlacement(
                        placement.ElementId,
                        changedLocationId,
                        Origin($"placement.{placement.ElementId}.{changedLocationId}"))
                    : placement),
            Origin($"scenario.{scenario.ScenarioId}"))).ToArray();
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
            baseline.Formations,
            elementsById.Values,
            changedScenarios);

        AssertValid(definition);
        return ContentPackArtifact.Create(definition);
    }

    private static void AssertValid(ContentPackDefinition definition)
    {
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
            catalogSetup.StageEntry,
            context.Selection,
            catalogSetup.Sources);

        return new CampaignSnapshot(
            CampaignSnapshot.CurrentContractVersion,
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
