using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class StageEntryArrivalTests
{
    [Fact]
    public void AdmittedArrivalCommandEmitsOneExactEventWithoutInventingAuthority()
    {
        var execution = ReachArrival();
        var arrival = execution.Snapshot!;
        var expectedSuccessor = Cna1979LandSequence.GetNext(arrival.SequencePosition);

        var decision = CampaignTestHarness.Decide(
            arrival,
            new ResolveNoObligationNavalConvoyArrival(
                arrival.StateVersion,
                arrival.SequencePosition.PositionId));

        Assert.True(decision.IsAccepted);
        var resolved = Assert.IsType<NoObligationNavalConvoyArrivalResolved>(
            Assert.Single(decision.Events));
        Assert.Equal(1, resolved.ContractVersion);
        Assert.Equal(arrival.CampaignId, resolved.CampaignId);
        Assert.Equal(checked(arrival.StateVersion + 1), resolved.StateVersion);
        Assert.Equal(arrival.SequencePosition.PositionId, resolved.FromPositionId);
        Assert.Equal(arrival.GameTurn, resolved.GameTurn);
        Assert.Equal(arrival.OperationStage, resolved.OperationStage);
        Assert.Equal(expectedSuccessor, resolved.SequencePosition);
        Assert.Equal(NoObligationNavalConvoyArrivalResolved.RequiredSources, resolved.Sources);

        var projected = CampaignTestHarness.Apply(arrival, resolved);
        var expected = arrival with
        {
            StateVersion = checked(arrival.StateVersion + 1),
            SequencePosition = expectedSuccessor,
        };

        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(expected),
            CampaignSnapshotSerializer.Serialize(projected));
        var replayed = CampaignTestHarness.Replay(execution.Events.Append(resolved));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(projected),
            CampaignSnapshotSerializer.Serialize(replayed));
    }

    [Fact]
    public void CurrentSystemArrivalSubmissionIsAcceptedOnce()
    {
        var arrival = ReachArrival().Snapshot!;
        var handle = new CampaignAuthorityHandle(
            arrival,
            CampaignTestHarness.ContextFor(arrival));
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
        Assert.Equal(8, accepted.SuccessorHandle!.Snapshot.StateVersion);
        Assert.Equal(
            LandSegmentIds.FleetAssignment,
            accepted.SuccessorHandle.Snapshot.SegmentId);
        Assert.Equal(8, accepted.Receipt!.CommittedStateVersion);
        Assert.Equal(
            accepted.SuccessorHandle.Snapshot.SequencePosition.PositionId,
            accepted.Receipt.ResultingPositionId);

        var duplicate = CampaignLegalActions.Submit(accepted.SuccessorHandle, submission);
        Assert.False(duplicate.IsAccepted);
        Assert.Equal(
            CampaignActionSubmissionRejectionReason.StaleState,
            duplicate.RejectionReason);
        Assert.Null(duplicate.SuccessorHandle);
        Assert.Null(duplicate.Receipt);
    }

    [Fact]
    public void InvalidArrivalCommandsRejectWithZeroEvents()
    {
        var arrivalExecution = ReachArrival();
        var arrival = arrivalExecution.Snapshot!;
        var organization = CampaignTestHarness.Replay(
            arrivalExecution.Events.Take(arrivalExecution.Events.Count - 1));
        var accepted = Assert.IsType<NoObligationNavalConvoyArrivalResolved>(Assert.Single(
            CampaignTestHarness.Decide(
                arrival,
                new ResolveNoObligationNavalConvoyArrival(
                    arrival.StateVersion,
                    arrival.SequencePosition.PositionId)).Events));
        var assignment = CampaignTestHarness.Apply(arrival, accepted);

        (CampaignSnapshot Snapshot, ResolveNoObligationNavalConvoyArrival Command,
            CampaignCommandRejectionReason Reason)[] cases =
        [
            (arrival,
                new ResolveNoObligationNavalConvoyArrival(
                    arrival.StateVersion - 1,
                    arrival.SequencePosition.PositionId),
                CampaignCommandRejectionReason.StaleState),
            (arrival,
                new ResolveNoObligationNavalConvoyArrival(
                    arrival.StateVersion,
                    assignment.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnexpectedSequenceStep),
            (arrival,
                new ResolveNoObligationNavalConvoyArrival(
                    arrival.StateVersion,
                    arrival.SequencePosition.PositionId) with
                    {
                        ContractVersion = 2,
                    },
                CampaignCommandRejectionReason.InvalidCommand),
            (organization,
                new ResolveNoObligationNavalConvoyArrival(
                    organization.StateVersion,
                    organization.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (assignment,
                new ResolveNoObligationNavalConvoyArrival(
                    assignment.StateVersion,
                    assignment.SequencePosition.PositionId),
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
    public void ProjectionRejectsForgedOrOutOfOrderArrivalHistory()
    {
        var execution = ReachArrival();
        var arrival = execution.Snapshot!;
        var organization = CampaignTestHarness.Replay(
            execution.Events.Take(execution.Events.Count - 1));
        var valid = Assert.IsType<NoObligationNavalConvoyArrivalResolved>(Assert.Single(
            CampaignTestHarness.Decide(
                arrival,
                new ResolveNoObligationNavalConvoyArrival(
                    arrival.StateVersion,
                    arrival.SequencePosition.PositionId)).Events));
        var successor = CampaignTestHarness.Apply(arrival, valid);
        CampaignEvent[] forgedEvents =
        [
            valid with { CampaignId = "campaign-forged" },
            valid with { StateVersion = valid.StateVersion + 1 },
        ];

        foreach (var forged in forgedEvents)
        {
            Assert.Throws<InvalidCampaignHistoryException>(() =>
                CampaignTestHarness.Apply(arrival, forged));
        }

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(organization, valid));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(successor, valid));
    }

    private static CampaignReplayResult ReachArrival()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create(
                "campaign-stage-entry-arrival",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(
                2,
                "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(
                3,
                "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(
                4,
                "land.position.operation-1.initiative-declaration",
                1,
                LandSide.Axis,
                InitiativeOrderChoice.ActFirst),
            new ResolveWeather(5, "land.position.operation-1.weather-determination"),
            new ResolveNoObligationOrganization(
                6,
                "land.position.operation-1.organization"),
        ];

        var execution = CampaignTestHarness.Execute(commands);
        Assert.True(execution.IsAccepted);
        Assert.Equal(7, execution.Snapshot!.StateVersion);
        Assert.Equal(LandPhaseIds.NavalConvoyArrival, execution.Snapshot.PhaseId);
        return execution;
    }
}
