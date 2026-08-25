using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class StageEntryFleetRepairTests
{
    [Fact]
    public void AdmittedRepairEmitsOneExactEventToUnmaterializedReserveAuthority()
    {
        var execution = ReachRepair();
        var repair = execution.Snapshot!;
        var successor = Cna1979LandSequence.GetNext(repair.SequencePosition);

        var decision = CampaignTestHarness.Decide(repair,
            new ResolveNoObligationFleetRepair(repair.StateVersion,
                repair.SequencePosition.PositionId));

        Assert.True(decision.IsAccepted);
        var resolved = Assert.IsType<NoObligationFleetRepairResolved>(
            Assert.Single(decision.Events));
        Assert.Equal(repair.CampaignId, resolved.CampaignId);
        Assert.Equal(checked(repair.StateVersion + 1), resolved.StateVersion);
        Assert.Equal(repair.SequencePosition.PositionId, resolved.FromPositionId);
        Assert.Equal((repair.GameTurn, repair.OperationStage),
            (resolved.GameTurn, resolved.OperationStage));
        Assert.Equal(successor, resolved.SequencePosition);
        Assert.Equal(NoObligationFleetRepairResolved.RequiredSources, resolved.Sources);
        Assert.Equal(LandPhaseIds.ReserveDesignation, successor.PhaseId);
        Assert.Equal(LandActorRole.FirstActingSide, successor.ActorRole);
        Assert.Null(successor.ActiveSide);

        var projected = CampaignTestHarness.Apply(repair, resolved);
        var expected = repair with
        {
            StateVersion = checked(repair.StateVersion + 1),
            SequencePosition = successor,
        };
        Assert.Equal(LandSide.Axis, projected.InitiativeHolder);
        Assert.Equal(LandSide.Commonwealth,
            Assert.Single(projected.OperationStageOrders).FirstSide);
        Assert.Null(projected.ActiveSide);
        Assert.Equal(CampaignSnapshotSerializer.Serialize(expected),
            CampaignSnapshotSerializer.Serialize(projected));
        Assert.Equal(CampaignSnapshotSerializer.Serialize(projected),
            CampaignSnapshotSerializer.Serialize(CampaignTestHarness.Replay(
                execution.Events.Append(resolved))));
        _ = Cna1979LandSequence.GetNext(projected.SequencePosition);
    }

    [Fact]
    public void CurrentSystemRepairSubmissionIsAcceptedOnceAndExposesNoReserveAction()
    {
        var repair = ReachRepair().Snapshot!;
        var handle = new CampaignAuthorityHandle(repair,
            CampaignTestHarness.ContextFor(repair));
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
        Assert.Equal(10, accepted.SuccessorHandle!.Snapshot.StateVersion);
        Assert.Equal(LandPhaseIds.ReserveDesignation,
            accepted.SuccessorHandle.Snapshot.PhaseId);
        Assert.Null(accepted.SuccessorHandle.Snapshot.ActiveSide);
        Assert.Equal(10, accepted.Receipt!.CommittedStateVersion);
        Assert.Empty(Query(accepted.SuccessorHandle, CampaignActionAudience.System).Candidates);
        var firstAudience = FirstActingSideResolver.Resolve(
            accepted.SuccessorHandle.Snapshot) == LandSide.Axis
                ? CampaignActionAudience.Axis
                : CampaignActionAudience.Commonwealth;
        var secondAudience = firstAudience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;
        Assert.Equal(3, Query(accepted.SuccessorHandle, firstAudience).Candidates.Count);
        Assert.Empty(Query(accepted.SuccessorHandle, secondAudience).Candidates);

        var duplicate = CampaignLegalActions.Submit(accepted.SuccessorHandle, submission);
        Assert.False(duplicate.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            duplicate.RejectionReason);
        Assert.Null(duplicate.SuccessorHandle);
        Assert.Null(duplicate.Receipt);
    }

    [Fact]
    public void InvalidRepairCommandsRejectWithZeroEvents()
    {
        var execution = ReachRepair();
        var repair = execution.Snapshot!;
        var assignment = CampaignTestHarness.Replay(
            execution.Events.Take(execution.Events.Count - 1));
        var resolved = Assert.IsType<NoObligationFleetRepairResolved>(Assert.Single(
            CampaignTestHarness.Decide(repair,
                new ResolveNoObligationFleetRepair(repair.StateVersion,
                    repair.SequencePosition.PositionId)).Events));
        var reserve = CampaignTestHarness.Apply(repair, resolved);
        (CampaignSnapshot Snapshot, ResolveNoObligationFleetRepair Command,
            CampaignCommandRejectionReason Reason)[] cases =
        [
            (repair,
                new ResolveNoObligationFleetRepair(repair.StateVersion - 1,
                    repair.SequencePosition.PositionId),
                CampaignCommandRejectionReason.StaleState),
            (repair,
                new ResolveNoObligationFleetRepair(repair.StateVersion,
                    reserve.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnexpectedSequenceStep),
            (repair,
                new ResolveNoObligationFleetRepair(repair.StateVersion,
                    repair.SequencePosition.PositionId) with { ContractVersion = 2 },
                CampaignCommandRejectionReason.InvalidCommand),
            (assignment,
                new ResolveNoObligationFleetRepair(assignment.StateVersion,
                    assignment.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (reserve,
                new ResolveNoObligationFleetRepair(reserve.StateVersion,
                    reserve.SequencePosition.PositionId),
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
    public void RepairReaderProjectionAndValidatorRejectMaterializedOrForgedReserveHistory()
    {
        var execution = ReachRepair();
        var repair = execution.Snapshot!;
        var assignment = CampaignTestHarness.Replay(
            execution.Events.Take(execution.Events.Count - 1));
        var valid = Assert.IsType<NoObligationFleetRepairResolved>(Assert.Single(
            CampaignTestHarness.Decide(repair,
                new ResolveNoObligationFleetRepair(repair.StateVersion,
                    repair.SequencePosition.PositionId)).Events));
        var reserve = CampaignTestHarness.Apply(repair, valid);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(repair, valid with { CampaignId = "campaign-forged" }));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(repair,
                valid with { StateVersion = valid.StateVersion + 1 }));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(assignment, valid));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(reserve, valid));

        var canonical = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(valid));
        var materializedEvent = canonical.Replace(
            "\"actorRole\":\"first-acting-side\",\"activeSide\":null",
            "\"actorRole\":\"first-acting-side\",\"activeSide\":\"commonwealth\"",
            StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(materializedEvent)));

        var position = reserve.SequencePosition;
        var materializedPosition = new LandSequencePosition(position.ContractVersion,
            position.PositionId, position.GameTurn, position.OperationStage,
            position.StageId, position.PhaseId, position.SegmentId, position.StepId,
            position.ActorRole, LandSide.Commonwealth, position.Sources);
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(
            reserve with { SequencePosition = materializedPosition }));
    }

    private static CampaignLegalActionSet Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(handle, audience);
        Assert.True(result.IsSuccessful);
        return result.ActionSet!;
    }

    private static CampaignReplayResult ReachRepair()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create("campaign-stage-entry-repair",
                Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2,
                "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3,
                "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(4,
                "land.position.operation-1.initiative-declaration", 1,
                LandSide.Axis, InitiativeOrderChoice.ActLast),
            new ResolveWeather(5, "land.position.operation-1.weather-determination"),
            new ResolveNoObligationOrganization(6,
                "land.position.operation-1.organization"),
            new ResolveNoObligationNavalConvoyArrival(7,
                "land.position.operation-1.naval-convoy-arrival"),
            new ResolveNoObligationFleetAssignment(8,
                "land.position.operation-1.commonwealth-fleet.assignment"),
        ];
        var execution = CampaignTestHarness.Execute(commands);
        Assert.True(execution.IsAccepted);
        Assert.Equal(9, execution.Snapshot!.StateVersion);
        Assert.Equal(LandSegmentIds.FleetRepair, execution.Snapshot.SegmentId);
        return execution;
    }
}
