using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAutoblindaSetupTests
{
    [Fact]
    public void ReplacementChoicePlacesTwoTrainedFueledPointsInTobruk()
    {
        var initial = Create();
        Assert.Equal(CampaignSourceAutoblindaSetup.InitialHash, initial.Hash());
        Assert.True(initial.CanChooseReplacements);
        Assert.False(initial.IsComplete);
        var chosen = initial.ChooseTobrukReplacements();
        Assert.True(chosen.IsComplete);
        Assert.Equal(2, chosen.ReplacementPoints);
        Assert.Empty(chosen.Targets);
        Assert.Empty(chosen.ValidAttachmentTargets());
        Assert.Throws<InvalidOperationException>(() => chosen.Attach("1-141-infantry"));
        using var snapshot = JsonDocument.Parse(chosen.SerializeCanonical());
        Assert.Equal("c4807", snapshot.RootElement.GetProperty("replacementLocationId").GetString());
        Assert.Equal("trained", snapshot.RootElement.GetProperty("replacementTraining").GetString());
        Assert.Equal("fueled", snapshot.RootElement.GetProperty("replacementFuel").GetString());
        Assert.False(snapshot.RootElement.GetProperty("targetCoverageComplete").GetBoolean());
        Assert.Equal(initial.Hash(), chosen.Reset().Hash());
    }

    [Fact]
    public void AttachedPointsAreDistinctAndDoNotInventTrainingOrReplacement()
    {
        var initial = Create();
        Assert.Equal(["1-141-infantry", "2-141-infantry"], initial.ValidAttachmentTargets());
        var first = initial.Attach("1-141-infantry");
        Assert.False(first.IsComplete);
        Assert.Equal(["2-141-infantry"], first.ValidAttachmentTargets());
        Assert.Throws<InvalidOperationException>(() => first.Attach("1-141-infantry"));
        Assert.Throws<InvalidOperationException>(() => first.ChooseTobrukReplacements());
        Assert.Throws<InvalidOperationException>(() => first.Attach("tobruk-light-aa-battalion"));
        var attached = first.Attach("2-141-infantry");
        Assert.True(attached.IsComplete);
        Assert.Equal(0, attached.ReplacementPoints);
        Assert.Empty(attached.ValidAttachmentTargets());
        using var snapshot = JsonDocument.Parse(attached.SerializeCanonical());
        var values = snapshot.RootElement.GetProperty("attachments").EnumerateArray().ToArray();
        Assert.Equal(2, values.Length);
        Assert.All(values, value =>
        {
            Assert.Equal(1, value.GetProperty("toePoints").GetInt32());
            Assert.Equal("unknown", value.GetProperty("training").GetString());
            Assert.Equal("unknown", value.GetProperty("fuel").GetString());
        });
        Assert.Equal(initial.Hash(), attached.Reset().Hash());
    }

    private static CampaignSourceAutoblindaSetup Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-autoblinda", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var state = CampaignPregameSourceState.Create(request, artifact, init);
        var baseline = CampaignSourceAxisInitialForces.Create(state);
        var supplement = CampaignSourceAxisSupplementalForces.Create(state, baseline);
        return CampaignSourceAutoblindaSetup.Create(supplement);
    }
}
