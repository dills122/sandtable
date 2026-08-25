using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class StageEntryFleetAssignmentTests
{
    [Fact]
    public void AdmittedAssignmentEmitsOneExactEventWithoutInventingShipAuthority()
    {
        var execution = ReachAssignment();
        var assignment = execution.Snapshot!;
        var successor = Cna1979LandSequence.GetNext(assignment.SequencePosition);

        var decision = CampaignTestHarness.Decide(assignment,
            new ResolveNoObligationFleetAssignment(assignment.StateVersion,
                assignment.SequencePosition.PositionId));

        Assert.True(decision.IsAccepted);
        var resolved = Assert.IsType<NoObligationFleetAssignmentResolved>(
            Assert.Single(decision.Events));
        Assert.Equal(assignment.CampaignId, resolved.CampaignId);
        Assert.Equal(checked(assignment.StateVersion + 1), resolved.StateVersion);
        Assert.Equal(assignment.SequencePosition.PositionId, resolved.FromPositionId);
        Assert.Equal((assignment.GameTurn, assignment.OperationStage),
            (resolved.GameTurn, resolved.OperationStage));
        Assert.Equal(successor, resolved.SequencePosition);
        Assert.Equal(NoObligationFleetAssignmentResolved.RequiredSources, resolved.Sources);

        var projected = CampaignTestHarness.Apply(assignment, resolved);
        var expected = assignment with
        {
            StateVersion = checked(assignment.StateVersion + 1),
            SequencePosition = successor,
        };
        Assert.Equal(CampaignSnapshotSerializer.Serialize(expected),
            CampaignSnapshotSerializer.Serialize(projected));
        Assert.Equal(CampaignSnapshotSerializer.Serialize(projected),
            CampaignSnapshotSerializer.Serialize(CampaignTestHarness.Replay(
                execution.Events.Append(resolved))));
    }

    [Fact]
    public void CurrentSystemAssignmentSubmissionIsAcceptedOnce()
    {
        var assignment = ReachAssignment().Snapshot!;
        var handle = new CampaignAuthorityHandle(assignment,
            CampaignTestHarness.ContextFor(assignment));
        var query = CampaignLegalActions.Query(handle, CampaignActionAudience.System);
        Assert.True(query.IsSuccessful);
        var candidate = Assert.Single(query.ActionSet!.Candidates);
        var submission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            query.ActionSet.CampaignId,
            query.ActionSet.StateVersion,
            query.ActionSet.PositionId,
            query.ActionSet.Audience,
            candidate.ActionId);

        var accepted = CampaignLegalActions.Submit(handle, submission);

        Assert.True(accepted.IsAccepted);
        Assert.Equal(9, accepted.SuccessorHandle!.Snapshot.StateVersion);
        Assert.Equal(LandSegmentIds.FleetRepair,
            accepted.SuccessorHandle.Snapshot.SegmentId);
        Assert.Equal(9, accepted.Receipt!.CommittedStateVersion);

        var duplicate = CampaignLegalActions.Submit(accepted.SuccessorHandle, submission);
        Assert.False(duplicate.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            duplicate.RejectionReason);
        Assert.Null(duplicate.SuccessorHandle);
        Assert.Null(duplicate.Receipt);
    }

    [Fact]
    public void InvalidAssignmentCommandsRejectWithZeroEvents()
    {
        var execution = ReachAssignment();
        var assignment = execution.Snapshot!;
        var arrival = CampaignTestHarness.Replay(
            execution.Events.Take(execution.Events.Count - 1));
        var resolved = Assert.IsType<NoObligationFleetAssignmentResolved>(Assert.Single(
            CampaignTestHarness.Decide(assignment,
                new ResolveNoObligationFleetAssignment(assignment.StateVersion,
                    assignment.SequencePosition.PositionId)).Events));
        var repair = CampaignTestHarness.Apply(assignment, resolved);
        (CampaignSnapshot Snapshot, ResolveNoObligationFleetAssignment Command,
            CampaignCommandRejectionReason Reason)[] cases =
        [
            (assignment,
                new ResolveNoObligationFleetAssignment(assignment.StateVersion - 1,
                    assignment.SequencePosition.PositionId),
                CampaignCommandRejectionReason.StaleState),
            (assignment,
                new ResolveNoObligationFleetAssignment(assignment.StateVersion,
                    repair.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnexpectedSequenceStep),
            (assignment,
                new ResolveNoObligationFleetAssignment(assignment.StateVersion,
                    assignment.SequencePosition.PositionId) with { ContractVersion = 2 },
                CampaignCommandRejectionReason.InvalidCommand),
            (arrival,
                new ResolveNoObligationFleetAssignment(arrival.StateVersion,
                    arrival.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (repair,
                new ResolveNoObligationFleetAssignment(repair.StateVersion,
                    repair.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnsupportedTransition),
        ];

        foreach (var (snapshot, command, reason) in cases)
        {
            var decision = CampaignTestHarness.Decide(snapshot, command);
            Assert.False(decision.IsAccepted);
            Assert.Equal(reason, decision.RejectionReason);
            Assert.Empty(decision.Events);
        }
    }

    [Fact]
    public void ProjectionRejectsForgedOrOutOfOrderAssignmentHistory()
    {
        var execution = ReachAssignment();
        var assignment = execution.Snapshot!;
        var arrival = CampaignTestHarness.Replay(
            execution.Events.Take(execution.Events.Count - 1));
        var valid = Assert.IsType<NoObligationFleetAssignmentResolved>(Assert.Single(
            CampaignTestHarness.Decide(assignment,
                new ResolveNoObligationFleetAssignment(assignment.StateVersion,
                    assignment.SequencePosition.PositionId)).Events));
        var successor = CampaignTestHarness.Apply(assignment, valid);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(assignment, valid with { CampaignId = "campaign-forged" }));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(assignment,
                valid with { StateVersion = valid.StateVersion + 1 }));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(arrival, valid));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(successor, valid));
    }

    private static CampaignReplayResult ReachAssignment()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create("campaign-stage-entry-assignment",
                Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2,
                "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3,
                "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(4,
                "land.position.operation-1.initiative-declaration", 1,
                LandSide.Axis, InitiativeOrderChoice.ActFirst),
            new ResolveWeather(5, "land.position.operation-1.weather-determination"),
            new ResolveNoObligationOrganization(6,
                "land.position.operation-1.organization"),
            new ResolveNoObligationNavalConvoyArrival(7,
                "land.position.operation-1.naval-convoy-arrival"),
        ];
        var execution = CampaignTestHarness.Execute(commands);
        Assert.True(execution.IsAccepted);
        Assert.Equal(8, execution.Snapshot!.StateVersion);
        Assert.Equal(LandSegmentIds.FleetAssignment, execution.Snapshot.SegmentId);
        return execution;
    }
}
