using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactCombatSelectionCutTests
{
    [Fact]
    public void AssaultOffersUseOnlyCurrentAdjacentContactHandlesAndApparentHexes()
    {
        CampaignObservationEdge[] edges =
        [
            new("hex-a", "hex-b", []),
            new("hex-b", "hex-c", []),
        ];
        CampaignContactTargetCandidate[] current =
        [
            new("axis-formation", "contact-adjacent", "hex-b"),
            new("axis-formation", "contact-remote", "hex-c"),
            new("other-formation", "contact-other", "hex-b"),
        ];

        var offers = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, current);

        var offer = Assert.Single(offers);
        Assert.Equal("axis-element", offer.ActingElementId);
        Assert.Equal("contact-adjacent", offer.ContactHandle);
        Assert.Equal("hex-b", offer.ApparentLocationId);
        Assert.Empty(CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, []));
    }

    [Fact]
    public void SameVisibleContactsProduceSameStableActionReferences()
    {
        CampaignObservationEdge[] edges = [new("hex-a", "hex-b", [])];
        CampaignContactTargetCandidate[] first =
        [
            new("axis-formation", "contact-two", "hex-b"),
            new("axis-formation", "contact-one", "hex-b"),
        ];
        CampaignContactTargetCandidate[] reversed = [first[1], first[0]];

        var left = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, first);
        var right = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, reversed);

        Assert.Equal(left.Select(value => value.ActionId), right.Select(value => value.ActionId));
        Assert.Equal(2, left.Select(value => value.ActionId).Distinct().Count());
    }

    [Fact]
    public void RealCombatCutSelectsCurrentContactAndRejectsForgedPrivateBinding()
    {
        var prior = BreakdownMovementTests.ChangeElement(
            BreakdownSnapshotTests.Create(true), "commonwealth-infantry", "north");
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var movement = CampaignBreakdownLifecycleFactory.CreateMovementCompletion(
            prior, artifact, scenario);
        var atBreakdown = new CampaignSnapshotV11(11, prior.CampaignId,
            movement.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(movement.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
        var completion = CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(
            atBreakdown, artifact, scenario,
            CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId());
        var atCombat = new CampaignSnapshotV11(11, prior.CampaignId,
            completion.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(completion.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
        Assert.Equal(LandStepIds.PositionDetermination,
            atCombat.CurrentPosition.SequenceContext.StepId);
        Assert.Equal(LandPhaseIds.MovementAndCombat,
            atCombat.CurrentPosition.SequenceContext.PhaseId);
        Assert.Equal(LandSegmentIds.Combat,
            atCombat.CurrentPosition.SequenceContext.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide,
            atCombat.CurrentPosition.SequenceContext.ActorRole);
        Assert.Equal(LandSide.Axis, atCombat.InitiativeHolder);
        var contacts = CampaignContactJournal.Initialize(atCombat, context);
        var cut = CampaignContactCombatSelectionCut.Project(atCombat, context,
            contacts, LandSide.Axis, "axis-infantry");
        var offer = Assert.Single(cut.AssaultOffers);

        var selected = CampaignNativeCombatSelectionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", offer.ActionId);
        var canonicalEvent = selected.Selection.SerializeCanonical();
        var replayed = CampaignNativeCombatSelectionStateV12.Replay(atCombat,
            context, contacts, canonicalEvent);
        var readback = CampaignNativeCombatSelectionStateV12.DeserializeCanonical(
            selected.SerializeCanonical(), context, contacts);

        Assert.Equal(atCombat.StateVersion + 1, selected.StateVersion);
        Assert.Equal(selected.SerializeCanonical(), replayed.SerializeCanonical());
        Assert.Equal(selected.SerializeCanonical(), readback.SerializeCanonical());
        Assert.Equal(offer.ContactHandle, selected.Selection.ContactHandle);
        Assert.NotEqual("forged", selected.Selection.BoundRepresentationId);
        var forged = selected.Selection with { BoundRepresentationId = "forged" };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatSelectionStateV12.Replay(atCombat, context,
                contacts, forged.SerializeCanonical()));
    }
}
