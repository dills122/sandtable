using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceConvoyPlanTests
{
    [Fact]
    public void FinalizationCapturesActualAxisChoicesAndFutureDestinationsWithoutDelivery()
    {
        var initial = Create(0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceConvoyPlan.Create(initial));
        var rolled = initial.Apply(new PregameAxisSupplyRollAction()).State;
        var first = rolled.Apply(new PregameAxisSupplyAllocationAction(6, 3, 1)).State;
        var second = first.Apply(new PregameAxisSupplyAllocationAction(2, 1, 1)).State;
        var plan = CampaignSourceConvoyPlan.Create(second);

        Assert.Equal(6, plan.Die);
        Assert.Equal(3, plan.MaximumSupply);
        Assert.Equal(2, plan.AllocatedSupply);
        Assert.Collection(plan.Allocations,
            value => Assert.Equal(new CampaignSourceConvoyAllocation(2, 1, 1, "West Sicily", "Tripoli"), value),
            value => Assert.Equal(new CampaignSourceConvoyAllocation(6, 3, 1, "Italy", "Tobruk"), value));
        using var json = JsonDocument.Parse(plan.SerializeCanonical());
        Assert.Equal(2, json.RootElement.GetProperty("targetGameTurn").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("unusedAvailability").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("allocations").GetArrayLength());
        Assert.Equal(plan.Hash(), CampaignSourceConvoyPlan.Create(second).Hash());
        Assert.NotEqual(plan.Hash(), CampaignSourceConvoyPlan.Create(first).Hash());
        Assert.Equal(2, plan.AllocatedSupply); // Captured plan owns its allocation bytes.
    }

    [Fact]
    public void ZeroAvailabilityFinalizesAnEmptyPlanWithoutInventingCargo()
    {
        var rolled = Create(14).Apply(new PregameAxisSupplyRollAction()).State;
        var plan = CampaignSourceConvoyPlan.Create(rolled);
        Assert.Equal(1, plan.Die);
        Assert.Equal(0, plan.MaximumSupply);
        Assert.Empty(plan.Allocations);
        using var json = JsonDocument.Parse(plan.SerializeCanonical());
        Assert.Equal(0, json.RootElement.GetProperty("unusedAvailability").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("allocations").EnumerateArray());
    }

    private static CampaignPregameSourceState Create(ulong seed)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-convoy-plan", Cna1979Ruleset.Manifest.Hash, seed,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignPregameSourceState.Create(request, artifact, init);
    }
}
