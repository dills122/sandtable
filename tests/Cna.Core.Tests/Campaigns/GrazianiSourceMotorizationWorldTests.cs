using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceMotorizationWorldTests
{
    [Fact]
    public void AxisPoolsPreserveRealTripoliAndAllocateLibyaWithoutInventingAttachments()
    {
        var state = Create(LandSide.Axis);
        Assert.Equal(0, state.Remaining("axis-tripoli-unattached"));
        Assert.Contains(state.Allocations, value => value.PoolId == "axis-tripoli-unattached"
            && value.LocationId == "box.tripoli" && value.Points == 180);
        Assert.Equal(125, state.Remaining("axis-libya-unattached"));
        Assert.Equal(5, state.Remaining("axis-tobruk-attached"));
        Assert.Equal(13, state.Pools.Count(value => value.Attached));
        Assert.Equal(305, state.Pools.Where(value => value.Attached).Sum(value => value.Points));
        Assert.Equal(55, state.Pools.Where(value => value.Attached).Sum(value => value.LightTrucks ?? 0));
        Assert.Equal(260, state.Pools.Where(value => value.Attached).Sum(value => value.MediumTrucks ?? 0));
        Assert.Equal(45, state.Pools.Where(value => value.Attached).Sum(value => value.HeavyTrucks ?? 0));
        var saharan = Assert.Single(state.Pools, value => value.Id == "axis-saharan-attached");
        Assert.Equal(5, saharan.LightTrucks);
        Assert.Null(saharan.MediumTrucks);
        Assert.Null(saharan.HeavyTrucks);
        Assert.All(state.Pools.Where(value => value.Attached), value => Assert.Empty(state.ValidDestinations(value.Id)));
        Assert.Empty(state.ValidDestinations("axis-tobruk-attached"));
        Assert.Equal(["c4507", "c4707"], state.ValidDestinations("axis-libya-unattached"));
        var next = state.Assign("axis-libya-unattached", "c4507", 50)
            .Assign("axis-libya-unattached", "c4707", 25);
        Assert.Equal(50, next.Remaining("axis-libya-unattached"));
        Assert.Equal(125, state.Remaining("axis-libya-unattached"));
        Assert.Throws<InvalidOperationException>(() => next.Assign("axis-libya-unattached", "c4131", 1));
        Assert.Throws<InvalidOperationException>(() => next.Assign("axis-libya-unattached", "c4507", 100));
        Assert.Throws<InvalidOperationException>(() => next.Assign("axis-libya-unattached", "c4707", -50));
        var amended = next.Assign("axis-libya-unattached", "c4507", -25);
        Assert.Equal(75, amended.Remaining("axis-libya-unattached"));
        Assert.Equal(25, amended.Allocations.Single(value => value.PoolId == "axis-libya-unattached"
            && value.LocationId == "c4507").Points);
        using var json = JsonDocument.Parse(amended.SerializeCanonical());
        Assert.Equal("not-selected", json.RootElement.GetProperty("optionalInitialLoadsStatus").GetString());
        Assert.Equal("unresolved-land-32.53-32.54", json.RootElement.GetProperty("attachedEligibilityStatus").GetString());
    }

    [Fact]
    public void CommonwealthUnattachedPoolsUseActualCitiesAndFriendlySupplyLocations()
    {
        var initial = Create(LandSide.Commonwealth);
        Assert.Equal(50, initial.Remaining("cw-cairo-unattached"));
        Assert.Equal(20, initial.Remaining("cw-alexandria-unattached"));
        Assert.Equal(45, initial.Remaining("cw-any-map-unattached"));
        Assert.Equal(20, initial.Remaining("cw-16-brigade-attached"));
        Assert.Equal(12, initial.Pools.Count(value => value.Attached));
        Assert.Equal(147, initial.Pools.Where(value => value.Attached).Sum(value => value.Points));
        Assert.Equal(30, initial.Pools.Where(value => value.Attached).Sum(value => value.LightTrucks ?? 0));
        Assert.Equal(125, initial.Pools.Where(value => value.Attached).Sum(value => value.MediumTrucks ?? 0));
        Assert.Equal(22, initial.Pools.Where(value => value.Attached).Sum(value => value.HeavyTrucks ?? 0));
        Assert.All(initial.Pools.Where(value => value.Attached), value => Assert.Empty(initial.ValidDestinations(value.Id)));
        Assert.Empty(initial.ValidDestinations("cw-16-brigade-attached"));
        var result = initial.Assign("cw-cairo-unattached", "e1829", 50)
            .Assign("cw-alexandria-unattached", "e3613", 10)
            .Assign("cw-alexandria-unattached", "e3714", 10)
            .Assign("cw-any-map-unattached", "c4131", 25)
            .Assign("cw-any-map-unattached", "d3714", 10)
            .Assign("cw-any-map-unattached", "d3714", 10);
        Assert.Equal(0, result.Remaining("cw-cairo-unattached"));
        Assert.Equal(0, result.Remaining("cw-alexandria-unattached"));
        Assert.Equal(0, result.Remaining("cw-any-map-unattached"));
        Assert.Equal(115, result.Allocations.Sum(value => value.Points));
        Assert.Throws<InvalidOperationException>(() => result.Assign("cw-cairo-unattached", "e1829", 1));
        Assert.Throws<InvalidOperationException>(() => initial.Assign("cw-any-map-unattached", "c4507", 1));
        Assert.NotEqual(initial.Hash(), result.Hash());
    }

    private static CampaignSourceMotorizationWorld Create(LandSide side)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-motorization", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignSourceMotorizationWorld.Create(CampaignPregameSourceState.Create(request, artifact, init), side);
    }
}
