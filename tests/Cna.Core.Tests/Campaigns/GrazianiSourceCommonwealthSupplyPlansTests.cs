using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceCommonwealthSupplyPlansTests
{
    private static readonly int[] PeriodIUnits = [1, 1, 2, 2, 3, 3];
    [Fact]
    public void CommonwealthPlanningUsesRetainedAxisRngAndCannotSkipRequiredTurns()
    {
        var axis = AxisPlan();
        Assert.Throws<InvalidOperationException>(() => CampaignSourceCommonwealthSupplyPlans.Create(axis));
        var completedAxis = axis.Roll().Finish();
        var plans = CampaignSourceCommonwealthSupplyPlans.Create(completedAxis);
        Assert.Equal(1, plans.NextTargetGameTurn);
        Assert.Throws<InvalidOperationException>(() => plans.Roll(2));
        var skipped = plans.SkipGt1();
        Assert.Equal(completedAxis.RandomState, skipped.RandomState);
        Assert.Equal("skipped-opening", skipped.Entries[0].Status);
        Assert.Null(skipped.Entries[0].SupplyUnits);
        Assert.Throws<InvalidOperationException>(() => skipped.SkipGt1());
        var expected = SandtableRandom.RollD6(skipped.RandomState);
        var second = skipped.Roll(2);
        Assert.Equal(expected.State, second.RandomState);
        Assert.Equal(expected.Value, second.Entries[1].Die);
        Assert.Equal(PeriodIUnits[expected.Value - 1], second.Entries[1].SupplyUnits);
        Assert.Throws<InvalidOperationException>(() => second.Roll(4));
        var complete = second.Roll(3).Roll(4);
        Assert.Null(complete.NextTargetGameTurn);
        Assert.All(complete.Entries.Skip(1), entry => Assert.InRange(entry.SupplyUnits!.Value, 1, 3));
        Assert.Throws<InvalidOperationException>(() => complete.Roll(4));
        Assert.NotEqual(plans.Hash(), complete.Hash());
        using var snapshot = JsonDocument.Parse(complete.SerializeCanonical());
        Assert.Equal("provisional", snapshot.RootElement.GetProperty("policyStatus").GetString());
        Assert.Equal("i-by-arrival-turn", snapshot.RootElement.GetProperty("tablePeriod").GetString());
    }

    [Fact]
    public void OptionalStartingTurnRollIsDistinctFromExplicitSkip()
    {
        var start = CampaignSourceCommonwealthSupplyPlans.Create(AxisPlan().Roll().Finish());
        var rolled = start.Roll(1);
        var skipped = start.SkipGt1();
        Assert.Equal("rolled", rolled.Entries[0].Status);
        Assert.InRange(rolled.Entries[0].SupplyUnits!.Value, 1, 3);
        Assert.Equal("skipped-opening", skipped.Entries[0].Status);
        Assert.Null(skipped.Entries[0].SupplyUnits);
        Assert.NotEqual(rolled.Hash(), skipped.Hash());
        Assert.NotEqual(rolled.RandomState, skipped.RandomState);
    }

    private static CampaignSourceOpeningConvoyPlan AxisPlan()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-commonwealth-plans", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var initial = CampaignPregameSourceState.Create(request, artifact, init);
        var gt2 = initial.Apply(new PregameAxisSupplyRollAction()).State;
        return CampaignSourceOpeningConvoyPlan.Create(gt2);
    }
}
