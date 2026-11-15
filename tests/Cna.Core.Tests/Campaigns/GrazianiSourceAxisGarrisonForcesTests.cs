using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAxisGarrisonForcesTests
{
    [Fact]
    public void RealGiarabubAndMinorGarrisonsRetainPositionsToesAndUnresolvedPriority()
    {
        var state = Create();
        var baseline = CampaignSourceAxisInitialForces.Create(state);
        var mainland = CampaignSourceAxisSupplementalForces.Create(state, baseline);
        var garrisons = CampaignSourceAxisGarrisonForces.Create(state, baseline, mainland);
        Assert.Equal(13, garrisons.Forces.Count);
        Assert.Equal(6, garrisons.Forces.Count(value => value.LocationId == "c1014"));
        Assert.DoesNotContain(garrisons.Forces, value => baseline.Forces.Any(existing => existing.ElementId == value.ElementId));
        Assert.DoesNotContain(garrisons.Forces, value => mainland.Forces.Any(existing => existing.ElementId == value.ElementId));
        var motor = Assert.Single(garrisons.Forces, value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.Equal(1, Assert.Single(motor.InitialToes!).Toe);
        Assert.Equal("historical-motorized-hh-8-plus-battalion-vs-company-eq-unresolved", motor.IdentityStatus);
        Assert.Equal("c3019", Assert.Single(garrisons.Forces, value => value.ElementId == "fort-maddalena-garrison").LocationId);
        Assert.Equal("a4130", Assert.Single(garrisons.Forces, value => value.ElementId == "soluch-artillery-regiment").LocationId);
        using var json = JsonDocument.Parse(garrisons.SerializeCanonical());
        Assert.False(json.RootElement.GetProperty("worldCoverageComplete").GetBoolean());
        Assert.Equal("priority-needs-organization-and-transport", json.RootElement.GetProperty("giarabubMotorizationStatus").GetString());
        Assert.Equal(CampaignSourceAxisGarrisonForces.ContentHash, garrisons.Hash());
    }

    private static CampaignPregameSourceState Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-axis-garrisons", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignPregameSourceState.Create(request, artifact, init);
    }
}
