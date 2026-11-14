using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiAxisInitialForcesTests
{
    [Fact]
    public void SourceCountersRetainStrengthsUnknownHeadquartersAndBardiaConflict()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "tobruk-source-forces", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var pregame = CampaignPregameSourceState.Create(request, artifact, initialization);
        var forces = CampaignSourceAxisInitialForces.Create(pregame);

        Assert.Equal(33, forces.Forces.Count);
        Assert.Equal(22, forces.Forces.Count(force => force.LocationId == "c4807"));
        Assert.Equal(11, forces.Forces.Count(force => force.LocationId == "c4321"));
        Assert.Equal(4, forces.Forces.Count(force => force.InitialToes is null));
        Assert.Equal(29, forces.Forces.Count(force => force.InitialToes is { Count: > 0 }));
        Assert.Equal(5, Assert.Single(forces.Forces.Single(force => force.ElementId == "1-31-gaf-sdic").InitialToes!).Toe);
        Assert.Equal(6, Assert.Single(forces.Forces.Single(force => force.ElementId == "1-32-gaf-sdic").InitialToes!).Toe);
        Assert.Equal(2, Assert.Single(forces.Forces.Single(force => force.ElementId == "5-tobruk-artiglieria").InitialToes!).Toe);
        Assert.Equal("bardia-30-32-gaf-source-conflict", forces.Forces.Single(force => force.ElementId == "30-gaf-sdic-hq").IdentityStatus);
        Assert.Equal("bardia-30-32-gaf-source-conflict", forces.Forces.Single(force => force.ElementId == "32-gaf-sdic-hq").IdentityStatus);
        using var document = JsonDocument.Parse(forces.SerializeCanonical());
        Assert.False(document.RootElement.GetProperty("worldCoverageComplete").GetBoolean());
        Assert.All(document.RootElement.GetProperty("forces").EnumerateArray(), force =>
        {
            Assert.Equal(JsonValueKind.Null, force.GetProperty("mobilityId").ValueKind);
            Assert.Equal(JsonValueKind.Null, force.GetProperty("attachmentId").ValueKind);
            Assert.Equal("unresolved", force.GetProperty("landOnlyDisposition").GetString());
        });
        Assert.Equal(forces.Hash(), CampaignSourceAxisInitialForces.Create(pregame).Hash());
    }
}
