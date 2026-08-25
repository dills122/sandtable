using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class StageEntryOrganizationTests
{
    [Fact]
    public void AdmittedOrganizationCommandEmitsOneExactEventAndPreservesAuthority()
    {
        var execution = ReachOrganization();
        var organization = execution.Snapshot!;
        var expectedSuccessor = Cna1979LandSequence.GetNext(organization.SequencePosition);

        var decision = CampaignTestHarness.Decide(
            organization,
            new ResolveNoObligationOrganization(
                organization.StateVersion,
                organization.SequencePosition.PositionId));

        Assert.True(decision.IsAccepted);
        var resolved = Assert.IsType<NoObligationOrganizationResolved>(
            Assert.Single(decision.Events));
        Assert.Equal(1, resolved.ContractVersion);
        Assert.Equal(organization.CampaignId, resolved.CampaignId);
        Assert.Equal(checked(organization.StateVersion + 1), resolved.StateVersion);
        Assert.Equal(organization.SequencePosition.PositionId, resolved.FromPositionId);
        Assert.Equal(organization.GameTurn, resolved.GameTurn);
        Assert.Equal(organization.OperationStage, resolved.OperationStage);
        Assert.Equal(expectedSuccessor, resolved.SequencePosition);
        Assert.Equal(NoObligationOrganizationResolved.RequiredSources, resolved.Sources);

        var projected = CampaignTestHarness.Apply(organization, resolved);
        var expected = organization with
        {
            StateVersion = checked(organization.StateVersion + 1),
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
    public void CurrentSystemOrganizationSubmissionIsAcceptedOnce()
    {
        var organization = ReachOrganization().Snapshot!;
        var handle = new CampaignAuthorityHandle(
            organization,
            CampaignTestHarness.ContextFor(organization));
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
        Assert.Equal(7, accepted.SuccessorHandle!.Snapshot.StateVersion);
        Assert.Equal(
            LandPhaseIds.NavalConvoyArrival,
            accepted.SuccessorHandle.Snapshot.PhaseId);
        Assert.Equal(7, accepted.Receipt!.CommittedStateVersion);
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
    public void InvalidOrganizationCommandsRejectWithZeroEvents()
    {
        var organizationExecution = ReachOrganization();
        var organization = organizationExecution.Snapshot!;
        var weather = CampaignTestHarness.Replay(
            organizationExecution.Events.Take(organizationExecution.Events.Count - 1));
        var accepted = Assert.IsType<NoObligationOrganizationResolved>(Assert.Single(
            CampaignTestHarness.Decide(
                organization,
                new ResolveNoObligationOrganization(
                    organization.StateVersion,
                    organization.SequencePosition.PositionId)).Events));
        var arrival = CampaignTestHarness.Apply(organization, accepted);

        (CampaignSnapshot Snapshot, ResolveNoObligationOrganization Command,
            CampaignCommandRejectionReason Reason)[] cases =
        [
            (organization,
                new ResolveNoObligationOrganization(
                    organization.StateVersion - 1,
                    organization.SequencePosition.PositionId),
                CampaignCommandRejectionReason.StaleState),
            (organization,
                new ResolveNoObligationOrganization(
                    organization.StateVersion,
                    arrival.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnexpectedSequenceStep),
            (organization,
                new ResolveNoObligationOrganization(
                    organization.StateVersion,
                    organization.SequencePosition.PositionId) with
                    {
                        ContractVersion = 2,
                    },
                CampaignCommandRejectionReason.InvalidCommand),
            (weather,
                new ResolveNoObligationOrganization(
                    weather.StateVersion,
                    weather.SequencePosition.PositionId),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (arrival,
                new ResolveNoObligationOrganization(
                    arrival.StateVersion,
                    arrival.SequencePosition.PositionId),
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
    public void ProjectionRejectsForgedOrOutOfOrderOrganizationHistory()
    {
        var execution = ReachOrganization();
        var organization = execution.Snapshot!;
        var weather = CampaignTestHarness.Replay(
            execution.Events.Take(execution.Events.Count - 1));
        var valid = Assert.IsType<NoObligationOrganizationResolved>(Assert.Single(
            CampaignTestHarness.Decide(
                organization,
                new ResolveNoObligationOrganization(
                    organization.StateVersion,
                    organization.SequencePosition.PositionId)).Events));
        var successor = CampaignTestHarness.Apply(organization, valid);
        CampaignEvent[] forgedEvents =
        [
            valid with { CampaignId = "campaign-forged" },
            valid with { StateVersion = valid.StateVersion + 1 },
        ];

        foreach (var forged in forgedEvents)
        {
            Assert.Throws<InvalidCampaignHistoryException>(() =>
                CampaignTestHarness.Apply(organization, forged));
        }

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(weather, valid));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(successor, valid));
    }

    private static CampaignReplayResult ReachOrganization()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create(
                "campaign-stage-entry-organization",
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
        ];

        var execution = CampaignTestHarness.Execute(commands);
        Assert.True(execution.IsAccepted);
        Assert.Equal(6, execution.Snapshot!.StateVersion);
        Assert.Equal(LandPhaseIds.Organization, execution.Snapshot.PhaseId);
        return execution;
    }
}
