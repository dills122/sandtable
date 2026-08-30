using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

internal sealed record CampaignMovementEvidence(
    IReadOnlyList<CampaignEvent> Events,
    CampaignSnapshot Snapshot,
    CampaignContentContext Context,
    LandSide ActingSide);

internal static class CampaignMovementTestData
{
    public static CampaignMovementEvidence ReachMovement(int reserveCount = 0)
    {
        var reserve = CampaignReserveActionTestData.ExecuteToReserve(
            0,
            InitiativeOrderChoice.ActLast);
        var snapshot = reserve.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(snapshot);
        var accepted = new List<CampaignEvent>(reserve.Events);

        foreach (var elementId in snapshot.World.Elements
                     .Where(element => SideOf(element.ElementId, reserve.Context) == actingSide)
                     .Select(element => element.ElementId)
                     .Order(StringComparer.Ordinal)
                     .Take(reserveCount))
        {
            Apply(new DesignateReserveElement(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                actingSide,
                elementId));
        }

        Apply(new CompleteReserveDesignation(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId,
            actingSide));

        return new CampaignMovementEvidence(
            Array.AsReadOnly(accepted.ToArray()),
            snapshot,
            reserve.Context,
            actingSide);

        void Apply(CampaignCommand command)
        {
            var decision = CampaignEngine.Decide(snapshot, command, reserve.Context);
            Assert.True(decision.IsAccepted);
            var campaignEvent = Assert.Single(decision.Events);
            snapshot = CampaignProjector.Apply(snapshot, campaignEvent, reserve.Context);
            accepted.Add(campaignEvent);
        }
    }

    public static MoveElementAction FindMove(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        LandSide actingSide,
        string elementId,
        string destinationLocationId)
    {
        var projection = CampaignObservationProjector.Project(snapshot, context, actingSide);
        var observation = Assert.IsType<CampaignObservation>(projection.Observation);
        return Assert.Single(
            CampaignMovementActionDerivation.Derive(observation).OfType<MoveElementAction>(),
            move => move.ElementId == elementId
                && move.DestinationLocationId == destinationLocationId);
    }

    public static MoveElement CommandFor(
        CampaignSnapshot snapshot,
        LandSide actingSide,
        MoveElementAction candidate) => new(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId,
            actingSide,
            candidate.ActionId,
            candidate.ElementId,
            candidate.OriginLocationId,
            candidate.DestinationLocationId);

    private static LandSide SideOf(
        string elementId,
        CampaignContentContext context) => context.Artifact.Definition.Elements
            .Single(element => element.ElementId == elementId).SideId switch
        {
            "axis" => LandSide.Axis,
            "commonwealth" => LandSide.Commonwealth,
            _ => throw new InvalidOperationException(),
        };
}
