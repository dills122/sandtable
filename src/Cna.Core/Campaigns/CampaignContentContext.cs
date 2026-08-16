using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

public interface IContentPackResolver
{
    ContentCatalogResolution Resolve(string packId, string expectedHash);
}

public sealed class Cna1979SyntheticContentResolver : IContentPackResolver
{
    public static Cna1979SyntheticContentResolver Instance { get; } = new();

    private Cna1979SyntheticContentResolver()
    {
    }

    public ContentCatalogResolution Resolve(string packId, string expectedHash) =>
        Cna1979SyntheticContentCatalog.Resolve(packId, expectedHash);
}

public sealed class CampaignContentContext
{
    private CampaignContentContext(ContentPackArtifact artifact, ContentScenario scenario)
    {
        Artifact = artifact;
        Scenario = scenario;
    }

    public ContentPackArtifact Artifact { get; }

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

        return new CampaignContentContext(artifact, scenario);
    }
}
