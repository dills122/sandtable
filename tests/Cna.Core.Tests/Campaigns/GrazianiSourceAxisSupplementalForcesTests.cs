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
        // RUL-GRA-28 moves 1-ccnn-hq and 4-ccnn-hq from explicitly-unknown
        // TOE to a known one-point artillery TOE with its weapon identity
        // still explicitly unresolved (see the dedicated assertions below) -
        // 87 -> 89 known, 23 -> 21 unknown.
        Assert.Equal(89, supplement.Forces.Count(value => value.InitialToes is not null));
        Assert.DoesNotContain(supplement.Forces, value =>
            predecessor.Forces.Any(previous => previous.ElementId == value.ElementId));
        var catanzaro = Assert.Single(supplement.Groups, value => value.GroupId == "catanzaro-source-candidate");
        Assert.Equal("c4707", catanzaro.LocationId);
        Assert.Equal(13, catanzaro.Forces.Count);
        var infantry = Assert.Single(catanzaro.Forces, value => value.ElementId == "1-141-infantry");
        Assert.Equal(7, Assert.Single(infantry.InitialToes!).Toe);
        // Catanzaro's own HQ is untouched by RUL-GRA-28 (it is scoped to the
        // 1st/4th CCNN division HQs only) and still has no recorded TOE.
        Assert.Null(Assert.Single(catanzaro.Forces, value => value.ElementId == "64-catanzaro-hq").InitialToes);
        var maletti = Assert.Single(supplement.Groups, value => value.GroupId == "maletti-source-candidate");
        Assert.Equal("c3617", maletti.LocationId);
        var firstCcnn = Assert.Single(supplement.Groups, value => value.GroupId == "1-ccnn-source-candidate");
        var firstHq = Assert.Single(firstCcnn.Forces, value => value.ElementId == "1-ccnn-hq");
        var firstHqToe = Assert.Single(firstHq.InitialToes!);
        Assert.Equal("1-ccnn-hq." + Cna1979GrazianiSourceFacts.CcnnHqUnidentifiedArtilleryComponentType, firstHqToe.ComponentId);
        Assert.Equal(1, firstHqToe.Toe);
        var fourthCcnn = Assert.Single(supplement.Groups, value => value.GroupId == "4-ccnn-source-candidate");
        var fourthHq = Assert.Single(fourthCcnn.Forces, value => value.ElementId == "4-ccnn-hq");
        var fourthHqToe = Assert.Single(fourthHq.InitialToes!);
        Assert.Equal("4-ccnn-hq." + Cna1979GrazianiSourceFacts.CcnnHqUnidentifiedArtilleryComponentType, fourthHqToe.ComponentId);
        Assert.Equal(1, fourthHqToe.Toe);
        // The engineers remain out of RUL-GRA-28's scope and still have no recorded TOE.
        Assert.Null(Assert.Single(firstCcnn.Forces, value => value.ElementId == "201-ccnn-engineer").InitialToes);
        Assert.Null(Assert.Single(fourthCcnn.Forces, value => value.ElementId == "204-ccnn-engineer").InitialToes);
        using var json = JsonDocument.Parse(supplement.SerializeCanonical());
        Assert.False(json.RootElement.GetProperty("worldCoverageComplete").GetBoolean());
        Assert.Equal("candidate-not-independently-reviewed", json.RootElement.GetProperty("sourceReviewStatus").GetString());
        Assert.Equal("sha256:0646362c46b0354969caaf61a955fbde111664c29f4f072dd4ffb30d9bcc76fc", supplement.Hash());
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
