using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactKnowledgeTests
{
    [Fact]
    public void CurrentContactCanBeOfferedThenBecomesStaleUntilRediscovery()
    {
        var knowledge = new CampaignContactKnowledge("campaign", LandSide.Axis);
        var handle = knowledge.NextHandle();
        knowledge.Apply(Transition(1, "sight-1", CampaignContactTransitionKind.Observed, handle));

        Assert.Equal(1, knowledge.AudienceRevision);
        Assert.Equal("enemy-private", knowledge.ResolvePrivateTarget("observer", handle));
        Assert.Equal([new CampaignContactTargetCandidate("observer", handle, "hex-1")],
            knowledge.CurrentTargetsFor("observer"));
        Assert.Empty(knowledge.CurrentTargetsFor("other-formation"));

        knowledge.Apply(Transition(2, "sight-loss", CampaignContactTransitionKind.Lost, handle));
        Assert.Equal(2, knowledge.AudienceRevision);
        Assert.False(Assert.Single(knowledge.ObserveFor("observer")).IsCurrent);
        Assert.Empty(knowledge.CurrentTargetsFor("observer"));
        Assert.Throws<InvalidOperationException>(() => knowledge.ResolvePrivateTarget("observer", handle));

        knowledge.Apply(Transition(3, "sight-2", CampaignContactTransitionKind.Rediscovered,
            handle, "hex-2"));
        Assert.Equal(3, knowledge.AudienceRevision);
        Assert.Equal("hex-2", Assert.Single(knowledge.CurrentTargetsFor("observer")).ApparentLocationId);
    }

    [Fact]
    public void HiddenAuthorityChangesDoNotChangeVisibleRevisionOrOffers()
    {
        var left = new CampaignContactKnowledge("campaign", LandSide.Axis);
        var right = new CampaignContactKnowledge("campaign", LandSide.Axis);
        left.Apply(Transition(1, "sight-1", CampaignContactTransitionKind.Observed,
            left.NextHandle()));
        right.Apply(Transition(1, "sight-1", CampaignContactTransitionKind.Observed,
            right.NextHandle()) with
        { BoundElementId = "different-hidden-enemy" });

        // A private source transition at a later authority cut changes no visible field.
        right.Apply(Transition(2, "private-source-2", CampaignContactTransitionKind.Updated,
            "c-00000001") with
        { BoundElementId = "different-hidden-enemy" });

        Assert.Equal(left.AudienceRevision, right.AudienceRevision);
        Assert.Equal(left.ObserveFor("observer"), right.ObserveFor("observer"));
        Assert.Equal(left.CurrentTargetsFor("observer"), right.CurrentTargetsFor("observer"));
        Assert.Equal(2, right.LastAuthorityVersion);
        Assert.NotEqual(left.ResolvePrivateTarget("observer", "c-00000001"),
            right.ResolvePrivateTarget("observer", "c-00000001"));
        Assert.Equal(CampaignContactObservationV1.Project(left, "observer").SerializeCanonical(),
            CampaignContactObservationV1.Project(right, "observer").SerializeCanonical());
    }

    [Fact]
    public void SideAndFormationAccessDoNotExposePrivateBinding()
    {
        var axis = new CampaignContactKnowledge("campaign", LandSide.Axis);
        var commonwealth = new CampaignContactKnowledge("campaign", LandSide.Commonwealth);
        axis.Apply(Transition(1, "sight-1", CampaignContactTransitionKind.Observed, axis.NextHandle()));

        Assert.Empty(commonwealth.ObserveFor("observer"));
        Assert.Empty(axis.ObserveFor("other-formation"));
        Assert.DoesNotContain("enemy-private", Assert.Single(axis.ObserveFor("observer")).ToString(),
            StringComparison.Ordinal);
        var wire = Encoding.UTF8.GetString(CampaignContactObservationV1.Project(axis, "observer").SerializeCanonical());
        Assert.DoesNotContain("enemy-private", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("authorityVersion", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("sourceEvent", wire, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => commonwealth.Apply(Transition(1, "wrong-side",
            CampaignContactTransitionKind.Observed, commonwealth.NextHandle())));
    }

    [Fact]
    public void ReplayRejectsDuplicateSourceAndInventedHandle()
    {
        var knowledge = new CampaignContactKnowledge("campaign", LandSide.Axis);
        Assert.Throws<ArgumentException>(() => knowledge.Apply(Transition(1, "sight-1",
            CampaignContactTransitionKind.Observed, "c-00000002")));
        knowledge.Apply(Transition(1, "sight-1", CampaignContactTransitionKind.Observed,
            knowledge.NextHandle()));
        Assert.Throws<ArgumentException>(() => knowledge.Apply(Transition(2, "sight-1",
            CampaignContactTransitionKind.Updated, "c-00000001")));
        Assert.Throws<ArgumentException>(() => knowledge.Apply(Transition(2, "bad-binding",
            CampaignContactTransitionKind.Updated, "c-00000001") with
        { BoundElementId = "other" }));
        Assert.Throws<ArgumentException>(() => knowledge.Apply(Transition(2, "duplicate-binding",
            CampaignContactTransitionKind.Observed, knowledge.NextHandle())));
        Assert.Equal(1, knowledge.AudienceRevision);
    }

    [Fact]
    public void CanonicalTransitionReadbackReconstructsSameSideView()
    {
        var original = new CampaignContactKnowledge("campaign", LandSide.Axis);
        var replay = new CampaignContactKnowledge("campaign", LandSide.Axis);
        var events = new[]
        {
            Transition(1, "sight-1", CampaignContactTransitionKind.Observed, "c-00000001"),
            Transition(2, "sight-loss", CampaignContactTransitionKind.Lost, "c-00000001"),
            Transition(3, "sight-2", CampaignContactTransitionKind.Rediscovered, "c-00000001", "hex-2"),
        };
        foreach (var value in events)
        {
            original.Apply(value);
            replay.Apply(CampaignContactTransitionCodec.DeserializeCanonical(
                CampaignContactTransitionCodec.SerializeCanonical(value)));
        }

        Assert.Equal(CampaignContactObservationV1.Project(original, "observer").SerializeCanonical(),
            CampaignContactObservationV1.Project(replay, "observer").SerializeCanonical());
        var expanded = Encoding.UTF8.GetString(CampaignContactTransitionCodec.SerializeCanonical(events[0]))
            .Replace("\"access\":\"observer\"", "\"access\":\"observer\",\"extra\":true",
                StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => CampaignContactTransitionCodec.DeserializeCanonical(
            Encoding.UTF8.GetBytes(expanded)));
    }

    [Fact]
    public void CommittedReactionMovementCreatesContactForActualReactingFormation()
    {
        var prior = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "north-east");
        var moved = BreakdownMovementTests.Move(prior, "axis-infantry", "north-west", "north");
        Assert.Single(moved.OpenedReactionWindow!.FrozenOpportunities);
        var knowledge = new CampaignContactKnowledge(prior.CampaignId, LandSide.Commonwealth);

        var transition = Assert.Single(CampaignContactReactionSource.Derive(moved, knowledge));
        Assert.Equal(0, knowledge.AudienceRevision);
        knowledge.Apply(transition);
        Assert.Equal(CampaignContactReactionSource.RuleId, transition.SourceRuleId);
        Assert.Equal("axis-infantry", knowledge.ResolvePrivateTarget("commonwealth-infantry", transition.ContactHandle));
        Assert.Equal("north", Assert.Single(knowledge.CurrentTargetsFor("commonwealth-infantry")).ApparentLocationId);
        Assert.Empty(knowledge.ObserveFor("other-commonwealth-formation"));
    }

    [Fact]
    public void ReactionSightSourceDoesNotRevealOrdinaryMoveOrWrongSide()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var ordinary = BreakdownMovementTests.Move(prior, "axis-truck", "west", "center");
        var commonwealth = new CampaignContactKnowledge(prior.CampaignId, LandSide.Commonwealth);
        Assert.Empty(CampaignContactReactionSource.Derive(ordinary, commonwealth));
        Assert.Equal(0, commonwealth.AudienceRevision);

        var reactingPrior = BreakdownMovementTests.ChangeElement(prior,
            "commonwealth-infantry", "north-east");
        var reactingMove = BreakdownMovementTests.Move(reactingPrior, "axis-infantry", "north-west", "north");
        Assert.Throws<ArgumentException>(() => CampaignContactReactionSource.Derive(reactingMove,
            new CampaignContactKnowledge(prior.CampaignId, LandSide.Axis)));
    }

    private static CampaignContactTransition Transition(long version, string source,
        CampaignContactTransitionKind kind, string handle, string location = "hex-1") => new(
        CampaignContactKnowledge.CurrentContractVersion,
        "campaign",
        LandSide.Axis,
        version,
        source,
        "visibility.adjacent-sight.v1",
        kind,
        handle,
        "enemy-private",
        "observer",
        location,
        CampaignContactIdentification.Presence,
        CampaignContactAccess.Observer);
}
