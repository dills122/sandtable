using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class StageEntryCampaignTests
{
    [Theory]
    [InlineData(0, 12345, LandSide.Axis)]
    [InlineData(1, 7, LandSide.Commonwealth)]
    public void BothSetupsReachReserveThroughExactEventsAndCurrentActions(
        int setupIndex,
        int seed,
        LandSide expectedInitiativeHolder)
    {
        var evidence = StageEntryCampaignTestData.Execute(
            Cna1979SetupCatalog.Definitions[setupIndex],
            (ulong)seed,
            InitiativeOrderChoice.ActLast);
        Type[] expectedEventTypes =
        [
            typeof(CampaignCreated),
            typeof(InitiativeDetermined),
            typeof(NoObligationNavalConvoyScheduleResolved),
            typeof(NoObligationTacticalShippingResolved),
            typeof(InitiativeOrderDeclared),
            typeof(WeatherDetermined),
            typeof(NoObligationOrganizationResolved),
            typeof(NoObligationNavalConvoyArrivalResolved),
            typeof(NoObligationFleetAssignmentResolved),
            typeof(NoObligationFleetRepairResolved),
        ];
        string[][] expectedSystemKinds =
        [
            ["resolve-initiative"],
            ["resolve-no-obligation-naval-convoy-schedule"],
            ["resolve-no-obligation-tactical-shipping"],
            [],
            ["resolve-weather"],
            ["resolve-no-obligation-organization"],
            ["resolve-no-obligation-naval-convoy-arrival"],
            ["resolve-no-obligation-fleet-assignment"],
            ["resolve-no-obligation-fleet-repair"],
            [],
        ];

        Assert.Equal(Enumerable.Range(1, 10).Select(value => (long)value),
            evidence.Snapshots.Select(snapshot => snapshot.StateVersion));
        Assert.Equal(expectedEventTypes, evidence.Events.Select(value => value.GetType()));
        Assert.Equal(expectedInitiativeHolder, evidence.Snapshot.InitiativeHolder);
        Assert.Equal(
            expectedInitiativeHolder == LandSide.Axis
                ? LandSide.Commonwealth
                : LandSide.Axis,
            Assert.Single(evidence.Snapshot.OperationStageOrders).FirstSide);
        Assert.Equal(LandPhaseIds.ReserveDesignation, evidence.Snapshot.PhaseId);
        Assert.Equal(LandActorRole.FirstActingSide,
            evidence.Snapshot.SequencePosition.ActorRole);
        Assert.Null(evidence.Snapshot.ActiveSide);

        for (var index = 0; index < evidence.Snapshots.Count; index++)
        {
            var snapshot = evidence.Snapshots[index];
            var handle = new CampaignAuthorityHandle(snapshot, evidence.Context);
            Assert.Equal(expectedSystemKinds[index], Query(handle,
                CampaignActionAudience.System).Candidates.Select(value => value.Kind));

            foreach (var audience in new[]
                {
                    CampaignActionAudience.Axis,
                    CampaignActionAudience.Commonwealth,
                })
            {
                var sideSet = Query(handle, audience);
                var isHolderAtDeclaration = snapshot.StateVersion == 4
                    && audience == (expectedInitiativeHolder == LandSide.Axis
                        ? CampaignActionAudience.Axis
                        : CampaignActionAudience.Commonwealth);
                Assert.Equal(
                    isHolderAtDeclaration ? ["act-first", "act-last"] : [],
                    sideSet.Candidates.Select(value => value.Kind));
            }
        }

        foreach (var snapshot in evidence.Snapshots.Where(value => value.StateVersion >= 6))
        {
            var generic = CampaignEngine.Decide(
                snapshot,
                new CompleteCurrentSequenceStep(
                    snapshot.StateVersion,
                    snapshot.SequencePosition.PositionId),
                evidence.Context);
            Assert.False(generic.IsAccepted);
            Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition,
                generic.RejectionReason);
            Assert.Empty(generic.Events);
        }
    }

    private static CampaignLegalActionSet Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(handle, audience);
        Assert.True(result.IsSuccessful);
        return result.ActionSet!;
    }
}

internal sealed record StageEntryCampaignEvidence(
    IReadOnlyList<CampaignEvent> Events,
    IReadOnlyList<CampaignSnapshot> Snapshots,
    CampaignContentContext Context)
{
    public CampaignSnapshot Snapshot => Snapshots[^1];
}

internal static class StageEntryCampaignTestData
{
    public static StageEntryCampaignEvidence Execute(
        CampaignSetupDefinition setup,
        ulong seed,
        InitiativeOrderChoice choice)
    {
        var creation = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-stage-entry-complete",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                setup.SetupId,
                setup.Hash));
        var created = Assert.IsType<CampaignCreated>(Assert.Single(creation.Events));
        var snapshot = CampaignTestHarness.Apply(null, created);
        var context = CampaignTestHarness.ContextFor(snapshot);
        var advanced = Advance(snapshot, context, choice);

        return new StageEntryCampaignEvidence(
            Array.AsReadOnly(new CampaignEvent[] { created }.Concat(advanced.Events).ToArray()),
            advanced.Snapshots,
            context);
    }

    public static StageEntryCampaignEvidence Advance(
        CampaignSnapshot initial,
        CampaignContentContext context,
        InitiativeOrderChoice choice)
    {
        var events = new List<CampaignEvent>();
        var snapshots = new List<CampaignSnapshot> { initial };
        var snapshot = initial;

        Apply(new ResolveInitiative(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new ResolveNoObligationNavalConvoySchedule(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new ResolveNoObligationTacticalShipping(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new DeclareInitiativeOrder(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId,
            snapshot.OperationStage,
            snapshot.InitiativeHolder!.Value,
            choice));
        Apply(new ResolveWeather(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new ResolveNoObligationOrganization(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new ResolveNoObligationNavalConvoyArrival(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new ResolveNoObligationFleetAssignment(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));
        Apply(new ResolveNoObligationFleetRepair(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId));

        return new StageEntryCampaignEvidence(
            Array.AsReadOnly(events.ToArray()),
            Array.AsReadOnly(snapshots.ToArray()),
            context);

        void Apply(CampaignCommand command)
        {
            var decision = CampaignEngine.Decide(snapshot, command, context);
            Assert.True(decision.IsAccepted);
            var campaignEvent = Assert.Single(decision.Events);
            snapshot = CampaignProjector.Apply(snapshot, campaignEvent, context);
            events.Add(campaignEvent);
            snapshots.Add(snapshot);
        }
    }
}
