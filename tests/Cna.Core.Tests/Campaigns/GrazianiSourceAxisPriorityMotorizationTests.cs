using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAxisPriorityMotorizationTests
{
    [Fact]
    public void DetachmentMustBeExplicitBeforeSourcePriorityLiftAndSharedPoolIsConserved()
    {
        var (initial, motor) = Create();
        Assert.Equal(CampaignSourceAxisPriorityMotorization.InitialHash, initial.Hash());
        Assert.Equal(35, initial.Remaining(motor));
        Assert.Empty(initial.ValidDeltas("1-1-libyan-motor-infantry", motor));
        Assert.Throws<InvalidOperationException>(() => initial.Adjust("1-1-libyan-motor-infantry", 1, motor));
        var detached = initial.Detach("1-1-libyan-motor-infantry");
        Assert.Equal([1, 5], detached.ValidDeltas("1-1-libyan-motor-infantry", motor));
        var first = detached.Adjust("1-1-libyan-motor-infantry", 5, motor);
        Assert.Equal(30, first.Remaining(motor));
        Assert.Equal(35, initial.Remaining(motor));
        Assert.Equal([-5, -1], first.ValidDeltas("1-1-libyan-motor-infantry", motor));
        Assert.Throws<InvalidOperationException>(() => first.UndoDetachment("1-1-libyan-motor-infantry"));
        Assert.Throws<InvalidOperationException>(() => first.Detach("17-5-libyan-infantry"));
        var both = first.Detach("3-1-libyan-motor-infantry").Adjust("3-1-libyan-motor-infantry", 5, motor);
        Assert.Equal(25, both.Remaining(motor));
        var all = both.Detach("5-1-libyan-motor-infantry").Adjust("5-1-libyan-motor-infantry", 5, motor);
        Assert.Equal(20, all.Remaining(motor));
        Assert.Equal(15, CampaignSourceAxisPriorityMotorization.Targets.Sum(all.AssignedPoints));
        using var snapshot = JsonDocument.Parse(all.SerializeCanonical());
        Assert.Equal("incomplete", snapshot.RootElement.GetProperty("globalPriorityStatus").GetString());
        Assert.Equal("unproven", snapshot.RootElement.GetProperty("fullUnitMobilityStatus").GetString());
        Assert.All(snapshot.RootElement.GetProperty("targets").EnumerateArray(), value =>
        {
            Assert.Equal("1-libyan-motor-regiment-hq", value.GetProperty("historicalAssignedParentId").GetString());
            Assert.Equal("detached", value.GetProperty("currentAttachment").GetString());
            Assert.True(value.GetProperty("infantryComponentLiftSatisfied").GetBoolean());
        });
    }

    [Fact]
    public void AssignmentMustBeReversedBeforeDetachmentCanReturnToUnresolved()
    {
        var (initial, motor) = Create();
        var selected = initial.Detach("1-1-libyan-motor-infantry").Adjust("1-1-libyan-motor-infantry", 5, motor);
        var cleared = selected.Adjust("1-1-libyan-motor-infantry", -5, motor)
            .UndoDetachment("1-1-libyan-motor-infantry");
        Assert.Equal(initial.Hash(), cleared.Hash());
        Assert.Equal(35, cleared.Remaining(motor));
        Assert.Empty(cleared.DetachedTargets);
        Assert.Throws<InvalidOperationException>(() => cleared.UndoDetachment("1-1-libyan-motor-infantry"));
    }

    private static (CampaignSourceAxisPriorityMotorization State, CampaignSourceMotorizationWorld Motor) Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-axis-priority-motor", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var pregame = CampaignPregameSourceState.Create(request, artifact, init);
        var baseline = CampaignSourceAxisInitialForces.Create(pregame);
        var supplement = CampaignSourceAxisSupplementalForces.Create(pregame, baseline);
        var motor = CampaignSourceMotorizationWorld.Create(pregame, LandSide.Axis);
        return (CampaignSourceAxisPriorityMotorization.Create(supplement, motor), motor);
    }
}
