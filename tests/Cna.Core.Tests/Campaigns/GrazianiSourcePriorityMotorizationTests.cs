using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourcePriorityMotorizationTests
{
    [Fact]
    public void ProvenPriorityAssignmentsConserveSixPointsPerFullInfantryComponent()
    {
        var (state, motor) = Create();
        Assert.Equal(CampaignSourcePriorityMotorization.InitialHash, state.Hash());
        Assert.Equal(20, state.Remaining("cw-c3922-attached", motor));
        Assert.Equal(10, state.Remaining("cw-c3320-attached", motor));
        Assert.Equal([1, 5, 6], state.ValidDeltas("1-kings-royal-rifle-corps", motor));
        var first = state.Adjust("1-kings-royal-rifle-corps", 6, motor);
        Assert.Equal(14, first.Remaining("cw-c3922-attached", motor));
        Assert.Equal(20, state.Remaining("cw-c3922-attached", motor));
        Assert.Equal([-6, -5, -1], first.ValidDeltas("1-kings-royal-rifle-corps", motor));
        Assert.Throws<InvalidOperationException>(() => first.Adjust("1-kings-royal-rifle-corps", 1, motor));
        Assert.Throws<InvalidOperationException>(() => first.Adjust("2-scots-guards", 1, motor));
        var both = first.Adjust("2-rifle-brigade", 6, motor);
        Assert.Equal(14, both.Remaining("cw-c3922-attached", motor));
        Assert.Equal(4, both.Remaining("cw-c3320-attached", motor));
        Assert.Equal(2, both.AssignedTargets.Count);
        var removed = both.Adjust("1-kings-royal-rifle-corps", -1, motor);
        Assert.Equal(15, removed.Remaining("cw-c3922-attached", motor));
        Assert.Equal(4, removed.Remaining("cw-c3320-attached", motor));
        using var snapshot = JsonDocument.Parse(both.SerializeCanonical());
        Assert.Equal("incomplete", snapshot.RootElement.GetProperty("globalPriorityStatus").GetString());
        Assert.Equal("unproven", snapshot.RootElement.GetProperty("fullMotorizationStatus").GetString());
        Assert.All(snapshot.RootElement.GetProperty("targets").EnumerateArray(), value =>
            Assert.True(value.GetProperty("infantryComponentLiftSatisfied").GetBoolean()));
        Assert.False(snapshot.RootElement.GetProperty("targetCoverageComplete").GetBoolean());
    }

    [Fact]
    public void UnattachedChangesCannotDuplicateOrEraseReservedFirstLinePoints()
    {
        var (state, motor) = Create();
        var assigned = state.Adjust("2-rifle-brigade", 1, motor);
        var amended = motor.Assign("cw-cairo-unattached", "e1829", 50);
        Assert.Equal(9, assigned.Remaining("cw-c3320-attached", amended));
        Assert.Equal([-1, 1, 5], assigned.ValidDeltas("2-rifle-brigade", amended));
        Assert.NotEqual(state.Hash(), assigned.Hash());
    }

    private static (CampaignSourcePriorityMotorization State, CampaignSourceMotorizationWorld Motor) Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-priority-motor", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var pregame = CampaignPregameSourceState.Create(request, artifact, init);
        var baseline = CampaignSourceCommonwealthInitialForces.Create(pregame);
        var supplement = CampaignSourceCommonwealthSupplementalForces.Create(pregame, baseline);
        var motor = CampaignSourceMotorizationWorld.Create(pregame, LandSide.Commonwealth);
        return (CampaignSourcePriorityMotorization.Create(supplement, motor), motor);
    }
}
