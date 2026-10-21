using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactOwnerMovementCutTests
{
    [Fact]
    public void HiddenEnemyPositionDoesNotChangeOwnerContactsOrMovementOffers()
    {
        var original = BreakdownSnapshotTests.Create(true);
        var left = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "east");
        var right = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "south-east");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);

        var first = CampaignContactOwnerMovementCut.Project(left, context,
            CampaignContactJournal.Initialize(left, context), LandSide.Axis, "axis-infantry");
        var second = CampaignContactOwnerMovementCut.Project(right, context,
            CampaignContactJournal.Initialize(right, context), LandSide.Axis, "axis-infantry");

        Assert.Empty(first.ContactObservation.CurrentTargets);
        Assert.Equal(first.ContactObservation.SerializeCanonical(),
            second.ContactObservation.SerializeCanonical());
        Assert.NotEmpty(first.MovementOffers);
        Assert.Equal(first.MovementOffers.Select(offer => offer.ActionId),
            second.MovementOffers.Select(offer => offer.ActionId));
        var visible = CampaignContactOwnerVisibleCutV1.SerializeCanonical(first);
        Assert.Equal(visible, CampaignContactOwnerVisibleCutV1.SerializeCanonical(second));
        var json = Encoding.UTF8.GetString(visible);
        Assert.DoesNotContain("authorityVersion", json, StringComparison.Ordinal);
        Assert.DoesNotContain("representationId", json, StringComparison.Ordinal);
        Assert.DoesNotContain(first.MovementOffers[0].ActionId, json, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactAtDestinationChangesOfferFromTheSameCut()
    {
        var original = BreakdownSnapshotTests.Create(true);
        var hidden = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "east");
        var visible = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "north");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);

        var unseenCut = CampaignContactOwnerMovementCut.Project(hidden, context,
            CampaignContactJournal.Initialize(hidden, context), LandSide.Axis, "axis-infantry");
        var seenCut = CampaignContactOwnerMovementCut.Project(visible, context,
            CampaignContactJournal.Initialize(visible, context), LandSide.Axis, "axis-infantry");

        Assert.Contains(unseenCut.MovementOffers, offer => offer.DestinationLocationId == "north");
        var offered = unseenCut.MovementOffers.Single(offer => offer.DestinationLocationId == "north");
        var accepted = CampaignElementMovedV3Factory.Create(hidden, context.ArtifactV6!,
            context.Scenario, new ElementMovedV3ReplayInput(hidden.CampaignId,
                hidden.StateVersion, hidden.CurrentPosition.SequencePosition!.PositionId,
                LandSide.Axis, offered.ElementId, offered.OriginLocationId,
                offered.DestinationLocationId));
        Assert.Equal(offered.DestinationLocationId, accepted.DestinationLocationId);
        Assert.Contains(seenCut.ContactObservation.CurrentTargets,
            target => target.ApparentLocationId == "north");
        Assert.DoesNotContain(seenCut.MovementOffers, offer => offer.DestinationLocationId == "north");
    }

    [Fact]
    public void OwnerCutRejectsAContactJournalFromAnotherAuthorityVersion()
    {
        var current = BreakdownSnapshotTests.Create(true);
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var journal = CampaignContactJournal.Initialize(current, context);
        var next = new CampaignSnapshotV11(11, current.CampaignId, current.StateVersion + 1,
            current.RulesetHash, current.Setup, current.World, current.InitiativeHolder,
            current.OperationStageOrders, current.OperationStageWeather, current.RandomState,
            current.CurrentPosition, current.ReactionWindow, current.BreakdownFlow);

        Assert.Throws<ArgumentException>(() => CampaignContactOwnerMovementCut.Project(next,
            context, journal, LandSide.Axis, "axis-infantry"));
        Assert.Equal(next.StateVersion, journal.AdvanceNonMovement(current, next).AuthorityVersion);
    }

    [Fact]
    public void NonVisibleAuthorityAdvanceDoesNotChangeOwnerVisibleCutBytes()
    {
        var current = BreakdownSnapshotTests.Create(true);
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var journal = CampaignContactJournal.Initialize(current, context);
        var initial = CampaignContactOwnerMovementCut.Project(current, context,
            journal, LandSide.Axis, "axis-infantry");
        var next = new CampaignSnapshotV11(11, current.CampaignId, current.StateVersion + 1,
            current.RulesetHash, current.Setup, current.World, current.InitiativeHolder,
            current.OperationStageOrders, current.OperationStageWeather, current.RandomState,
            current.CurrentPosition, current.ReactionWindow, current.BreakdownFlow);
        var successor = CampaignContactOwnerMovementCut.Project(next, context,
            journal.AdvanceNonMovement(current, next), LandSide.Axis, "axis-infantry");

        Assert.NotEqual(initial.AuthorityVersion, successor.AuthorityVersion);
        Assert.Equal(CampaignContactOwnerVisibleCutV1.SerializeCanonical(initial),
            CampaignContactOwnerVisibleCutV1.SerializeCanonical(successor));
    }

    [Fact]
    public void TrustedHostDraftKeepsPrivateFenceAndActionMapOutOfVisibleBytes()
    {
        var snapshot = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "east");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var session = new ExerciseSession(snapshot, context, [],
            CampaignContactJournal.Initialize(snapshot, context));

        var draft = CampaignHostAdapter.QueryContactOwnerDraft(session,
            CampaignActionAudience.Axis, "axis-infantry");
        Assert.Equal(snapshot.StateVersion, draft.AuthorityVersion);
        Assert.NotEmpty(draft.PrivateMovementActions);
        var bytes = draft.VisibleBytes;
        Assert.DoesNotContain("authorityVersion", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.DoesNotContain(draft.PrivateMovementActions[0].ActionId,
            Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.Equal("{}", JsonSerializer.Serialize(draft));
        bytes[0] ^= 1;
        Assert.NotEqual(bytes, draft.VisibleBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignHostAdapter.QueryContactOwnerDraft(
            session, CampaignActionAudience.System, "axis-infantry"));
        Assert.Throws<ArgumentException>(() => CampaignHostAdapter.QueryContactOwnerDraft(
            session, CampaignActionAudience.Commonwealth, "axis-infantry"));
    }
}
