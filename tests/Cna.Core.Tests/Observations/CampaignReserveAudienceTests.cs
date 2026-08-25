using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignReserveAudienceTests
{
    [Theory]
    [InlineData(false, LandSide.Axis)]
    [InlineData(true, LandSide.Commonwealth)]
    public void ReserveAudienceComesFromStageOrderWithoutMaterializingAuthority(
        bool actLast,
        LandSide expectedFirstSide)
    {
        var reserve = ReachReserve(actLast);
        var context = CampaignTestHarness.ContextFor(reserve);
        var authorityBytes = CampaignSnapshotSerializer.Serialize(reserve);

        Assert.Equal(LandSide.Axis, reserve.InitiativeHolder);
        Assert.Equal(expectedFirstSide,
            Assert.Single(reserve.OperationStageOrders).FirstSide);
        Assert.Equal(expectedFirstSide, FirstActingSideResolver.Resolve(reserve));
        Assert.Equal(LandActorRole.FirstActingSide, reserve.SequencePosition.ActorRole);
        Assert.Null(reserve.SequencePosition.ActiveSide);

        foreach (var observer in Enum.GetValues<LandSide>())
        {
            var projected = CampaignObservationProjector.Project(reserve, context, observer);
            var observation = Assert.IsType<CampaignObservation>(projected.Observation);

            Assert.Equal(expectedFirstSide, observation.Position.ActiveSide);
            Assert.Equal(LandActorRole.FirstActingSide, observation.Position.ActorRole);
            Assert.Equal(LandSide.Axis, observation.Position.InitiativeHolder);
            Assert.DoesNotContain("stageEntry",
                Encoding.UTF8.GetString(
                    CampaignObservationSerializer.SerializeCanonical(observation)),
                StringComparison.OrdinalIgnoreCase);

            var audience = observer == LandSide.Axis
                ? CampaignActionAudience.Axis
                : CampaignActionAudience.Commonwealth;
            var legalActions = CampaignLegalActions.Query(
                new CampaignAuthorityHandle(reserve, context), audience);
            Assert.True(legalActions.IsSuccessful);
            Assert.Equal(audience, legalActions.ActionSet!.Audience);
            Assert.Equal(observer == expectedFirstSide ? 3 : 0,
                legalActions.ActionSet.Candidates.Count);
        }

        Assert.Equal(authorityBytes, CampaignSnapshotSerializer.Serialize(reserve));
        var successor = Cna1979LandSequence.GetNext(reserve.SequencePosition);
        Assert.Equal(LandPhaseIds.MovementAndCombat, successor.PhaseId);
        Assert.Equal(LandSegmentIds.Movement, successor.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide, successor.ActorRole);
        Assert.Null(successor.ActiveSide);
    }

    private static CampaignSnapshot ReachReserve(bool actLast)
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create("campaign-reserve-audience",
                Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2,
                "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3,
                "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(4,
                "land.position.operation-1.initiative-declaration", 1,
                LandSide.Axis, actLast
                    ? InitiativeOrderChoice.ActLast
                    : InitiativeOrderChoice.ActFirst),
            new ResolveWeather(5, "land.position.operation-1.weather-determination"),
            new ResolveNoObligationOrganization(6,
                "land.position.operation-1.organization"),
            new ResolveNoObligationNavalConvoyArrival(7,
                "land.position.operation-1.naval-convoy-arrival"),
            new ResolveNoObligationFleetAssignment(8,
                "land.position.operation-1.commonwealth-fleet.assignment"),
            new ResolveNoObligationFleetRepair(9,
                "land.position.operation-1.commonwealth-fleet.repair"),
        ];
        var execution = CampaignTestHarness.Execute(commands);

        Assert.True(execution.IsAccepted);
        Assert.Equal(10, execution.Snapshot!.StateVersion);
        return execution.Snapshot;
    }
}
