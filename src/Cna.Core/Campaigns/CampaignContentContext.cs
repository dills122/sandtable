using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

public interface IContentPackResolver
{
    ContentCatalogResolution Resolve(string packId, string expectedHash);
}

public interface IContentPackV5Resolver
{
    ContentPackV5CatalogResolution ResolveV5(string packId, string expectedHash);
}

public sealed class Cna1979SyntheticContentResolver : IContentPackResolver, IContentPackV5Resolver
{
    public static Cna1979SyntheticContentResolver Instance { get; } = new();

    private Cna1979SyntheticContentResolver()
    {
    }

    public ContentCatalogResolution Resolve(string packId, string expectedHash) =>
        Cna1979SyntheticContentCatalog.Resolve(packId, expectedHash);

    public ContentPackV5CatalogResolution ResolveV5(string packId, string expectedHash) =>
        Cna1979SyntheticContentCatalog.ResolveV5(packId, expectedHash);
}

internal sealed class CampaignContentContext
{
    private CampaignContentContext(
        ContentPackArtifact artifact,
        ContentPackV5Artifact? artifactV5,
        ContentScenario scenario)
    {
        Artifact = artifact;
        ArtifactV5 = artifactV5;
        Scenario = scenario;
    }

    public ContentPackArtifact Artifact { get; }

    public ContentPackV5Artifact? ArtifactV5 { get; }

    public ContentScenario Scenario { get; }

    public CampaignContentSelection Selection => new(Artifact.Identity, Scenario.ScenarioId);

    public static CampaignContentContext Create(
        ContentPackArtifact artifact,
        string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);

        if (!string.Equals(
            artifact.Identity.RulesetId,
            Cna1979Ruleset.RulesetId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Content Pack does not target the supported ruleset.",
                nameof(artifact));
        }

        var scenario = artifact.Definition.Scenarios.SingleOrDefault(candidate => string.Equals(
            candidate.ScenarioId,
            scenarioId,
            StringComparison.Ordinal)) ?? throw new ArgumentException(
                "The scenario does not exist in the Content Pack.",
                nameof(scenarioId));

        return new CampaignContentContext(artifact, null, scenario);
    }

    public static CampaignContentContext Create(
        ContentPackV5Artifact artifact,
        string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        CampaignWorldV5Validator.RequireValidContent(artifact);
        if (!string.Equals(
                artifact.Identity.RulesetId,
                Cna1979Ruleset.RulesetId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Content Pack v5 does not target the supported ruleset.",
                nameof(artifact));
        }

        var scenario = artifact.Definition.LegacyDefinition.Scenarios.SingleOrDefault(
            candidate => string.Equals(
                candidate.ScenarioId,
                scenarioId,
                StringComparison.Ordinal)) ?? throw new ArgumentException(
            "The scenario does not exist in the Content Pack v5.",
            nameof(scenarioId));
        return new CampaignContentContext(
            ContentPackArtifact.Create(artifact.Definition.LegacyDefinition),
            artifact,
            scenario);
    }
}
