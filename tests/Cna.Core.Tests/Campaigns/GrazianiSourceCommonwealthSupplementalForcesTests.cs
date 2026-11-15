using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceCommonwealthSupplementalForcesTests
{
    [Fact]
    public void NamedStartingCountersRetainLocationStrengthAndSourceConflictWithoutDuplicates()
    {
        var state = Create();
        var predecessor = CampaignSourceCommonwealthInitialForces.Create(state);
        var supplement = CampaignSourceCommonwealthSupplementalForces.Create(state, predecessor);
        Assert.Equal(predecessor.Hash(), supplement.PredecessorHash);
        Assert.Equal(12, supplement.Forces.Count);
        Assert.DoesNotContain(supplement.Forces, value => predecessor.Forces.Any(previous => previous.ElementId == value.ElementId));
        var guards = Assert.Single(supplement.Forces, value => value.ElementId == "2-scots-guards");
        Assert.Equal("c4131", guards.LocationId);
        Assert.Equal(6, Assert.Single(guards.InitialToes!).Toe);
        var tank = Assert.Single(supplement.Forces, value => value.ElementId == "1-rtr");
        Assert.Equal("c3520", tank.LocationId);
        Assert.Equal(7, Assert.Single(tank.InitialToes!).Toe);
        var marine = Assert.Single(supplement.Forces, value => value.ElementId == "1-french-motor-marines");
        Assert.Equal("c3926", marine.LocationId);
        Assert.Equal("scenario-setup-vs-oa-arrival-errata-unresolved", marine.IdentityStatus);
        using var snapshot = JsonDocument.Parse(supplement.SerializeCanonical());
        Assert.False(snapshot.RootElement.GetProperty("worldCoverageComplete").GetBoolean());
        Assert.Equal("candidate-not-independently-reviewed", snapshot.RootElement.GetProperty("sourceReviewStatus").GetString());
        Assert.Equal("sha256:a8fa59467db15c42e8f811caae738efc39e5ffb0976d50cf0e071406a03d0407", supplement.Hash());
    }

    private static CampaignPregameSourceState Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-commonwealth-supplement", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignPregameSourceState.Create(request, artifact, init);
    }
}
