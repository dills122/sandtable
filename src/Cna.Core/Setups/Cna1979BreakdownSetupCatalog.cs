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
        ]);

    public static bool TryGet(string? setupId,
        [NotNullWhen(true)] out CampaignSetupDefinitionV6? definition)
    {
        definition = Definitions.SingleOrDefault(value => value.SetupId == setupId);
        return definition is not null;
    }

    private static CampaignSetupDefinitionV6 CreateSetup(string setupId, string displayName,
        ContentPackV6Artifact artifact, string sourceLocator)
    {
        return new CampaignSetupDefinitionV6(
            SchemaVersion,
            setupId,
            displayName,
            true,
            ContentPackV6Definition.SupportedCapabilityProfileId,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.WeatherPolicy,
            new CampaignStageEntryPolicy(
                CampaignStageEntryPolicy.CurrentContractVersion, 1, 1,
                StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone,
                [CampaignStageEntryPolicy.SourceReference]),
            new CampaignContentV6Selection(artifact.Identity,
                artifact.Definition.LegacyDefinition.Scenarios.Single().ScenarioId),
            [new RuleReference("sandtable-breakdown-lab", sourceLocator)]);
    }
}
