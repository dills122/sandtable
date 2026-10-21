using Cna.Core.Campaigns;
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
}
