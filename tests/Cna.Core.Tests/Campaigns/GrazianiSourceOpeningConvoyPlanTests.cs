using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceOpeningConvoyPlanTests
{
    [Fact]
    public void OpeningRollUsesRecordedSuccessorRandomStateAndCannotSkipAvailability()
    {
        var initial = Create(0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceOpeningConvoyPlan.Create(initial));
        var gt2 = initial.Apply(new PregameAxisSupplyRollAction()).State;
        var plan = CampaignSourceOpeningConvoyPlan.Create(gt2);
        Assert.Null(plan.Die);
        Assert.False(plan.IsFinished);
        Assert.Throws<InvalidOperationException>(() => plan.Finish());
        Assert.Throws<InvalidOperationException>(() => plan.Adjust(2, 1, 1));
        var expected = SandtableRandom.RollD6(gt2.RandomState);
        var rolled = plan.Roll();
        Assert.Equal(expected.Value, rolled.Die);
        Assert.Equal(expected.State, rolled.RandomState);
        Assert.Throws<InvalidOperationException>(() => rolled.Roll());
        using var snapshot = JsonDocument.Parse(rolled.SerializeCanonical());
        Assert.Equal("gt2-then-gt1", snapshot.RootElement.GetProperty("rngOrder").GetString());
        Assert.Equal("1940-september-i", snapshot.RootElement.GetProperty("planningPeriod").GetString());
        Assert.Equal("provisional", snapshot.RootElement.GetProperty("policyStatus").GetString());
    }

    [Fact]
    public void PositiveRollEnforcesCapacityAndFinishesOnlyThePlayersActualChoices()
    {
        var plan = Enumerable.Range(0, 128).Select(seed => Rolled((ulong)seed))
            .First(value => value.MaximumSupply == 3);
        var first = plan.Adjust(6, 3, 1).Adjust(2, 1, 1);
        Assert.Equal(2, first.Allocations().Sum(value => value.SupplyUnits));
        Assert.Throws<InvalidOperationException>(() => first.Adjust(1, 1, 1));
        Assert.Throws<InvalidOperationException>(() => first.Adjust(2, 4, 1));
        Assert.Throws<InvalidOperationException>(() => first.Adjust(3, 1, -1));
        var adjusted = first.Adjust(6, 3, -1).Adjust(3, 2, 1);
        var finished = adjusted.Finish();
        Assert.True(finished.IsFinished);
        Assert.Collection(finished.Allocations(),
            value => Assert.Equal(new CampaignSourceConvoyAllocation(2, 1, 1, "West Sicily", "Tripoli"), value),
            value => Assert.Equal(new CampaignSourceConvoyAllocation(3, 2, 1, "Italy", "Benghazi"), value));
        Assert.Throws<InvalidOperationException>(() => finished.Finish());
        Assert.Throws<InvalidOperationException>(() => finished.Adjust(2, 1, -1));
        Assert.NotEqual(adjusted.Hash(), finished.Hash());
    }

    [Fact]
    public void RolledZeroAndUnrolledAreDistinctAndZeroCanBeExplicitlyFinished()
    {
        var zero = Enumerable.Range(0, 128).Select(seed => Rolled((ulong)seed))
            .First(value => value.MaximumSupply == 0);
        Assert.Equal(1, zero.Die);
        Assert.Empty(zero.Allocations());
        Assert.Empty(zero.ValidDeltas(2, 1));
        var finished = zero.Finish();
        Assert.True(finished.IsFinished);
        Assert.Empty(finished.Allocations());
        Assert.NotEqual(zero.Hash(), finished.Hash());
    }

    private static CampaignSourceOpeningConvoyPlan Rolled(ulong seed)
    {
        var gt2 = Create(seed).Apply(new PregameAxisSupplyRollAction()).State;
        return CampaignSourceOpeningConvoyPlan.Create(gt2).Roll();
    }

    private static CampaignPregameSourceState Create(ulong seed)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-opening-convoy", Cna1979Ruleset.Manifest.Hash, seed,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignPregameSourceState.Create(request, artifact, init);
    }
}
