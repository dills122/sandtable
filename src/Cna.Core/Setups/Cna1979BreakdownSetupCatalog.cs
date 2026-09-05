using System.Diagnostics.CodeAnalysis;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal static class Cna1979BreakdownSetupCatalog
{
    public const int SchemaVersion = 6;
    public const string TruckSetupId = "rules-lab.breakdown.truck.v1";
    public const string ReactionSetupId = "rules-lab.breakdown.reaction.v1";

    public static IReadOnlyList<CampaignSetupDefinitionV6> Definitions { get; } =
        Array.AsReadOnly<CampaignSetupDefinitionV6>([
            CreateSetup(TruckSetupId, "Rules Lab: Certified Truck Breakdown", Cna1979SyntheticContentCatalog.ArtifactV6,
                "breakdown.truck-setup.v1"),
            CreateSetup(ReactionSetupId, "Rules Lab: Certified Breakdown Reaction", Cna1979SyntheticContentCatalog.ArtifactBreakdownReactionV6,
                "breakdown.reaction-setup.v1"),
            .. Cna1979SyntheticContentCatalog.BreakdownRunnerArtifactsV6.SelectMany(artifact =>
                artifact.Definition.LegacyDefinition.Scenarios.Select(scenario => CreateRunnerSetup(artifact, scenario))),
        ]);

    public static bool TryGet(string? setupId,
        [NotNullWhen(true)] out CampaignSetupDefinitionV6? definition)
    {
        definition = Definitions.SingleOrDefault(value => value.SetupId == setupId);
        return definition is not null;
    }

    private static CampaignSetupDefinitionV6 CreateSetup(string setupId, string displayName,
        ContentPackV6Artifact artifact, string sourceLocator, ContentScenario? selectedScenario = null,
        InitiativePolicy? initiative = null)
    {
        var scenario = selectedScenario ?? artifact.Definition.LegacyDefinition.Scenarios.Single();
        return new CampaignSetupDefinitionV6(
            SchemaVersion,
            setupId,
            displayName,
            true,
            ContentPackV6Definition.SupportedCapabilityProfileId,
            scenario.Start.GameTurn,
            initiative ?? new PredeterminedInitiative(LandSide.Axis),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.WeatherPolicy,
            new CampaignStageEntryPolicy(
                CampaignStageEntryPolicy.CurrentContractVersion, scenario.Start.GameTurn, 1,
                StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone,
                [CampaignStageEntryPolicy.SourceReference]),
            new CampaignContentV6Selection(artifact.Identity,
                scenario.ScenarioId),
            [new RuleReference("sandtable-breakdown-lab", sourceLocator)]);
    }

    private static CampaignSetupDefinitionV6 CreateRunnerSetup(ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        var variant = scenario.ScenarioId == "breakdown-battalion-contested"
            ? "battalion-contested" : artifact.Identity.PackId["rules-lab.content.breakdown-".Length..^3];
        InitiativePolicy? initiative = variant == "battalion-contested"
            ? new ContestedInitiative(new AxisInitiativeSourceFacts(AxisInitiativeLocation.OffMapOrUnavailable,
                [AxisInitiativeLocation.QualifyingGameMap])) : null;
        return CreateSetup($"rules-lab.breakdown.{variant}.v1", $"Rules Lab: Certified Breakdown {variant}",
            artifact, $"breakdown.runner.{variant}.v1", scenario, initiative);
    }
}
