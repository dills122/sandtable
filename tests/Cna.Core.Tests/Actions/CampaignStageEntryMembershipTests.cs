using System.Reflection;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignStageEntryMembershipTests
{
    [Fact]
    public void AdmittedStageEntryPositionsExposeOnlyTheirExactSystemMechanicAndCommand()
    {
        var organization = AdvanceToOrganization();
        (long StateVersion, string Kind, Type CommandType, Type? EventType)[] cases =
        [
            (6, "resolve-no-obligation-organization",
                typeof(ResolveNoObligationOrganization),
                typeof(NoObligationOrganizationResolved)),
            (7, "resolve-no-obligation-naval-convoy-arrival",
                typeof(ResolveNoObligationNavalConvoyArrival),
                typeof(NoObligationNavalConvoyArrivalResolved)),
            (8, "resolve-no-obligation-fleet-assignment",
                typeof(ResolveNoObligationFleetAssignment),
                typeof(NoObligationFleetAssignmentResolved)),
            (9, "resolve-no-obligation-fleet-repair",
                typeof(ResolveNoObligationFleetRepair),
                typeof(NoObligationFleetRepairResolved)),
        ];

        foreach (var value in cases)
        {
            var handle = AtState(organization, value.StateVersion);
            var system = Query(handle, CampaignActionAudience.System);
            var candidate = Assert.Single(system.Candidates);

            Assert.Equal(value.Kind, candidate.Kind);
            Assert.Empty(Query(handle, CampaignActionAudience.Axis).Candidates);
            Assert.Empty(Query(handle, CampaignActionAudience.Commonwealth).Candidates);

            var command = CampaignActionExecution.ToCommand(
                handle.Snapshot,
                CampaignActionAudience.System,
                candidate);
            Assert.Equal(value.CommandType, command.GetType());
            Assert.Equal(1, command.ContractVersion);
            Assert.Equal(value.StateVersion, command.ExpectedStateVersion);
            Assert.Equal(
                handle.Snapshot.SequencePosition.PositionId,
                command.GetType().GetProperty("ExpectedPositionId")!.GetValue(command));

            var decision = CampaignEngine.Decide(handle.Snapshot, command, handle.Context);
            var execution = CampaignActionExecution.Execute(
                handle.Snapshot,
                handle.Context,
                Bind(system, candidate));

            if (value.EventType is not null)
            {
                Assert.True(decision.IsAccepted);
                Assert.IsType(value.EventType, Assert.Single(decision.Events));
                Assert.True(execution.IsAccepted);
                Assert.IsType(value.EventType, execution.AcceptedEvent);
                Assert.NotNull(execution.SuccessorSnapshot);
                Assert.NotNull(execution.Receipt);
                continue;
            }

            Assert.Equal(CampaignCommandRejectionReason.InvalidCommand,
                decision.RejectionReason);
            Assert.Empty(decision.Events);
            Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
                execution.RejectionReason);
            Assert.Null(execution.AcceptedEvent);
            Assert.Null(execution.SuccessorSnapshot);
            Assert.Null(execution.Receipt);
        }
    }

    [Fact]
    public void StageEntryCommandsHaveFrozenClosedAuthorityBinding()
    {
        Type[] commandTypes =
        [
            typeof(ResolveNoObligationOrganization),
            typeof(ResolveNoObligationNavalConvoyArrival),
            typeof(ResolveNoObligationFleetAssignment),
            typeof(ResolveNoObligationFleetRepair),
        ];

        Assert.All(commandTypes, type =>
        {
            Assert.True(type.IsNotPublic);
            Assert.True(type.IsSealed);
            Assert.True(type.IsAssignableTo(typeof(CampaignCommand)));
            var constructor = Assert.Single(type.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                value => value.GetParameters().Length == 2);
            Assert.Equal(
                ["ExpectedStateVersion", "ExpectedPositionId"],
                constructor.GetParameters().Select(parameter => parameter.Name));
            Assert.Equal(
                [typeof(long), typeof(string)],
                constructor.GetParameters().Select(parameter => parameter.ParameterType));
            Assert.Equal(
                ["ExpectedPositionId"],
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.DeclaredOnly).Select(property => property.Name));
        });
    }

    [Fact]
    public void WrongAudienceOrUnsupportedPolicyPairProducesNoExecutableEvent()
    {
        var handle = AdvanceToOrganization();
        var system = Query(handle, CampaignActionAudience.System);
        var candidate = Assert.Single(system.Candidates);
        var wrongAudience = Bind(system, candidate) with
        {
            Audience = CampaignActionAudience.Axis,
        };

        var wrongAudienceExecution = CampaignActionExecution.Execute(
            handle.Snapshot,
            handle.Context,
            wrongAudience);

        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            wrongAudienceExecution.RejectionReason);
        Assert.Null(wrongAudienceExecution.AcceptedEvent);
        Assert.Null(wrongAudienceExecution.SuccessorSnapshot);
        Assert.Null(wrongAudienceExecution.Receipt);

        CampaignStageEntryPolicy[] unsupportedPolicies =
        [
            CreatePolicy(handle.Snapshot.GameTurn, StageEntryObligationKind.HasObligations),
            CreatePolicy(checked(handle.Snapshot.GameTurn + 1),
                StageEntryObligationKind.ExplicitNone),
        ];

        foreach (var policy in unsupportedPolicies)
        {
            var forged = handle.Snapshot with
            {
                Setup = WithStageEntryPolicy(handle.Snapshot.Setup, policy),
            };
            var query = CampaignLegalActions.Query(
                new CampaignAuthorityHandle(forged, handle.Context),
                CampaignActionAudience.System);
            var decision = CampaignEngine.Decide(
                forged,
                new ResolveNoObligationOrganization(
                    forged.StateVersion,
                    forged.SequencePosition.PositionId),
                handle.Context);

            Assert.False(query.IsSuccessful);
            Assert.Null(query.ActionSet);
            Assert.Equal(CampaignLegalActionQueryRejectionReason.InvalidState,
                query.RejectionReason);
            Assert.Equal(CampaignCommandRejectionReason.InvalidState,
                decision.RejectionReason);
            Assert.Empty(decision.Events);
        }
    }

    private static CampaignAuthorityHandle AdvanceToOrganization()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var request = new CampaignCreationRequest(1, "campaign-stage-entry-membership",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var creation = CampaignAuthority.Create(request);
        Assert.True(creation.IsCreated);
        var handle = creation.Handle!;

        handle = SubmitOnly(handle, CampaignActionAudience.System, "resolve-initiative");
        handle = SubmitOnly(handle, CampaignActionAudience.System,
            "resolve-no-obligation-naval-convoy-schedule");
        handle = SubmitOnly(handle, CampaignActionAudience.System,
            "resolve-no-obligation-tactical-shipping");
        handle = SubmitOnly(handle, CampaignActionAudience.Axis, "act-last");
        return SubmitOnly(handle, CampaignActionAudience.System, "resolve-weather");
    }

    private static CampaignAuthorityHandle AtState(
        CampaignAuthorityHandle organization,
        long stateVersion)
    {
        var position = Cna1979LandSequence.CreateTurn(
            organization.Snapshot.GameTurn)[checked((int)stateVersion - 1)];
        return new CampaignAuthorityHandle(
            organization.Snapshot with
            {
                StateVersion = stateVersion,
                SequencePosition = position,
            },
            organization.Context);
    }

    private static CampaignStageEntryPolicy CreatePolicy(
        int gameTurn,
        StageEntryObligationKind organization) => new(
        CampaignStageEntryPolicy.CurrentContractVersion,
        gameTurn,
        1,
        organization,
        StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind.ExplicitNone,
        [CampaignStageEntryPolicy.SourceReference]);

    private static CampaignSetupSnapshot WithStageEntryPolicy(
        CampaignSetupSnapshot setup,
        CampaignStageEntryPolicy policy)
    {
        var hash = CampaignSetupHash.Calculate(
            setup.SchemaVersion,
            setup.SetupId,
            setup.IsSynthetic,
            setup.InitialGameTurn,
            setup.InitialInitiative,
            setup.OpeningPreamble,
            setup.Weather,
            policy,
            setup.Content,
            setup.Sources);
        return new CampaignSetupSnapshot(
            setup.SchemaVersion,
            setup.SetupId,
            hash,
            setup.IsSynthetic,
            setup.InitialGameTurn,
            setup.InitialInitiative,
            setup.OpeningPreamble,
            setup.Weather,
            policy,
            setup.Content,
            setup.Sources);
    }

    private static CampaignLegalActionSet Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(handle, audience);
        Assert.True(result.IsSuccessful);
        return result.ActionSet!;
    }

    private static CampaignAuthorityHandle SubmitOnly(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience,
        string kind)
    {
        var set = Query(handle, audience);
        var candidate = Assert.Single(set.Candidates, value => value.Kind == kind);
        var result = CampaignLegalActions.Submit(handle, Bind(set, candidate));
        Assert.True(result.IsAccepted);
        return result.SuccessorHandle!;
    }

    private static CampaignActionSubmission Bind(
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(
        CampaignActionSubmission.CurrentContractVersion,
        set.CampaignId,
        set.StateVersion,
        set.PositionId,
        set.Audience,
        candidate.ActionId);
}
