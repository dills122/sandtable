using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAxisSupplementalForcesTests
{
    [Fact]
    public void SupplementalForcesRetainActualPositionsToesAndExplicitUnknowns()
    {
        var state = Create();
        var predecessor = CampaignSourceAxisInitialForces.Create(state);
        var supplement = CampaignSourceAxisSupplementalForces.Create(state, predecessor);
        Assert.Equal(predecessor.Hash(), supplement.PredecessorHash);
        Assert.Equal(10, supplement.Groups.Count);
        Assert.Equal(110, supplement.Forces.Count());
        Assert.Equal(87, supplement.Forces.Count(value => value.InitialToes is not null));
        Assert.DoesNotContain(supplement.Forces, value =>
            predecessor.Forces.Any(previous => previous.ElementId == value.ElementId));
        var catanzaro = Assert.Single(supplement.Groups, value => value.GroupId == "catanzaro-source-candidate");
        Assert.Equal("c4707", catanzaro.LocationId);
        Assert.Equal(13, catanzaro.Forces.Count);
        var infantry = Assert.Single(catanzaro.Forces, value => value.ElementId == "1-141-infantry");
        Assert.Equal(7, Assert.Single(infantry.InitialToes!).Toe);
        Assert.Null(Assert.Single(catanzaro.Forces, value => value.ElementId == "64-catanzaro-hq").InitialToes);
        var maletti = Assert.Single(supplement.Groups, value => value.GroupId == "maletti-source-candidate");
        Assert.Equal("c3617", maletti.LocationId);
        using var json = JsonDocument.Parse(supplement.SerializeCanonical());
        Assert.False(json.RootElement.GetProperty("worldCoverageComplete").GetBoolean());
        Assert.Equal("candidate-not-independently-reviewed", json.RootElement.GetProperty("sourceReviewStatus").GetString());
        Assert.Equal("sha256:1a34826c4d5e1a3fe20891f9064b61e8e6b58f5ecee7935d5ac076839e083372", supplement.Hash());
    }

    private static CampaignPregameSourceState Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-supplement", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignPregameSourceState.Create(request, artifact, init);
    }
}
