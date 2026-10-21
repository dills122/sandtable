using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;

namespace Cna.Core.Tests.Content;

public sealed class ContentCombatSourceTests
{
    private static readonly ContentPackV6Artifact Artifact =
        Cna1979SyntheticContentCatalog.BreakdownRunnerArtifactsV6.Single(value =>
            value.Identity.PackId == "rules-lab.content.breakdown-reaction.combat-assault.v1");

    [Fact]
    public void NormalCampaignResolvesPinnedCombatFactsFromItsContentPack()
    {
        var source = Assert.IsType<ContentCombatSourceV1>(ContentCombatSourceV1.Resolve(Artifact));
        Assert.Equal(Artifact.Identity.Hash, source.ContentHash);
        Assert.Equal(0, source.BasicMorale("axis-infantry.formation"));
        Assert.Equal(0, source.BasicMorale("commonwealth-infantry.formation"));
        Assert.Equal(1, source.OffensiveRating("axis-infantry.toe"));
        Assert.Equal(1, source.OffensiveRating("commonwealth-infantry.toe"));
        Assert.StartsWith("sha256:", source.Hash);
        Assert.All(Artifact.Definition.ElementCombatFacts.SelectMany(value => value.Components),
            component => Assert.Equal(10, component.MaximumToe));
        Assert.All(Assert.Single(Artifact.Definition.LegacyDefinition.Scenarios).InitialPlacements
                .SelectMany(placement => Artifact.Definition.InitialPlacementCombatFacts
                    .Where(facts => facts.ElementId == placement.ElementId)
                    .SelectMany(facts => facts.InitialComponentToes)),
            toe => Assert.Equal(10, toe.CurrentToe));
        Assert.NotNull(CampaignContentContext.Create(Artifact,
            Assert.Single(Artifact.Definition.LegacyDefinition.Scenarios).ScenarioId).CombatSource);
        Assert.Null(ContentCombatSourceV1.Resolve(Cna1979SyntheticContentCatalog.ArtifactV6));
        var shorterInfantry = Cna1979SyntheticContentCatalog.BreakdownRunnerArtifactsV6.Single(value =>
            value.Identity.PackId == "rules-lab.content.breakdown-reaction.combat-infantry.v1");
        Assert.Null(ContentCombatSourceV1.Resolve(shorterInfantry));
    }

    [Fact]
    public void MutatedPackBindingMembershipProvenanceAndBytesFailClosed()
    {
        var bytes = ReadResource();
        var root = JsonNode.Parse(bytes)!.AsObject();
        var wrongHash = root.DeepClone().AsObject();
        wrongHash["contentHash"] = "sha256:" + new string('0', 64);
        Assert.Throws<JsonException>(() => ContentCombatSourceV1.Parse(
            Encoding.UTF8.GetBytes(wrongHash.ToJsonString()), Artifact));

        var missingComponent = root.DeepClone().AsObject();
        missingComponent["componentRatings"]!.AsArray().RemoveAt(0);
        Assert.Throws<JsonException>(() => ContentCombatSourceV1.Parse(
            Encoding.UTF8.GetBytes(missingComponent.ToJsonString()), Artifact));

        var unindexedSource = root.DeepClone().AsObject();
        unindexedSource["formationMorale"]![0]!["sourceId"] = "unindexed-source";
        Assert.Throws<JsonException>(() => ContentCombatSourceV1.Parse(
            Encoding.UTF8.GetBytes(unindexedSource.ToJsonString()), Artifact));

        Assert.Throws<JsonException>(() => ContentCombatSourceV1.Parse(
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes) + " "), Artifact));
    }

    private static byte[] ReadResource()
    {
        using var source = typeof(ContentPackV6Artifact).Assembly.GetManifestResourceStream(
            "Cna.Core.Content.Fixtures.rules-lab.content.breakdown-reaction.combat-assault.v1.combat-source-v1.json");
        using var stream = new MemoryStream();
        (source ?? throw new InvalidOperationException("Combat source resource is missing.")).CopyTo(stream);
        return stream.ToArray();
    }
}
