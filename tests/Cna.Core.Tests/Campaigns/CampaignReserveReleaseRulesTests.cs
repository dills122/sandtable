using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReserveReleaseRulesTests
{
    [Fact]
    public void DesignatedReserveHasOwnerChoicesAndCarriesNextCycleAllowance()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var request = new CampaignCreationRequest(1,
            "campaign-native-reserve-release", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        for (var index = 0; index < 20; index++)
        {
            if (session.CurrentSnapshot.CurrentPosition.SequenceContext.PhaseId
                == LandPhaseIds.ReserveDesignation)
                break;
            var actionSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(actionSet);
            var accepted = CampaignExercises.Submit(session,
                new CampaignActionSubmission(
                    CampaignActionSubmission.CurrentContractVersion,
                    actionSet.CampaignId, actionSet.StateVersion,
                    actionSet.PositionId, actionSet.Audience,
                    actionSet.Candidates[0].ActionId));
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            session = accepted.SuccessorSession!;
        }
        Assert.Equal(LandPhaseIds.ReserveDesignation,
            session.CurrentSnapshot.CurrentPosition.SequenceContext.PhaseId);
        var firstSide = session.CurrentSnapshot.OperationStageOrders.Single().FirstSide;
        var owner = firstSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var actionSetAtReserve = CampaignExercises.Query(session, owner).ActionSet!;
        var designation = actionSetAtReserve.Candidates
            .OfType<DesignateReserveAction>().First();
        var designated = CampaignExercises.Submit(session,
            new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                actionSetAtReserve.CampaignId, actionSetAtReserve.StateVersion,
                actionSetAtReserve.PositionId, owner, designation.ActionId));
        Assert.True(designated.IsAccepted);
        session = designated.SuccessorSession!;

        var world = session.CurrentSnapshot.World;
        var pending = CampaignReserveReleaseRules.Pending(
            world, session.Context, firstSide, 1);
        var member = Assert.Single(pending);
        Assert.Equal(designation.ElementId, member.ElementId);
        Assert.Equal([CampaignReserveReleaseChoice.ReleaseI,
            CampaignReserveReleaseChoice.ConvertToII], member.Choices);
        var otherSide = firstSide == LandSide.Axis
            ? LandSide.Commonwealth : LandSide.Axis;
        Assert.Empty(CampaignReserveReleaseRules.Pending(
            world, session.Context, otherSide, 1));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignReserveReleaseRules.Apply(world, session.Context, otherSide,
                1, member.ElementId, CampaignReserveReleaseChoice.ReleaseI));

        var released = CampaignReserveReleaseRules.Apply(world, session.Context,
            firstSide, 1, member.ElementId, CampaignReserveReleaseChoice.ReleaseI);
        Assert.Equal(CampaignElementReserveStatus.None, released.ResultingStatus);
        Assert.Equal(2, released.NextMovement!.NextCycleOrdinal);
        Assert.Equal(member.BaseCapabilityPointAllowance,
            released.NextMovement.CapabilityPointCeiling);
        Assert.Empty(CampaignReserveReleaseRules.Pending(
            released.World, session.Context, firstSide, 1));

        var converted = CampaignReserveReleaseRules.Apply(world, session.Context,
            firstSide, 1, member.ElementId,
            CampaignReserveReleaseChoice.ConvertToII);
        Assert.Equal(CampaignElementReserveStatus.ReserveII,
            converted.ResultingStatus);
        Assert.Null(converted.NextMovement);
        Assert.Empty(CampaignReserveReleaseRules.Pending(
            converted.World, session.Context, firstSide, 1));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignReserveReleaseRules.Pending(world, session.Context,
                firstSide, 2));
        var nextCycle = Assert.Single(CampaignReserveReleaseRules.Pending(
            converted.World, session.Context, firstSide, 2));
        Assert.Equal([CampaignReserveReleaseChoice.ReleaseII,
            CampaignReserveReleaseChoice.RetainII], nextCycle.Choices);
        var secondRelease = CampaignReserveReleaseRules.Apply(converted.World,
            session.Context, firstSide, 2, member.ElementId,
            CampaignReserveReleaseChoice.ReleaseII);
        Assert.Equal(member.BaseCapabilityPointAllowance / 2,
            secondRelease.NextMovement!.CapabilityPointCeiling);

        var releasePosition = Cna1979LandSequenceV4.CreateTurn(1).Single(
            position => position.OperationStage == 1
                && position.ActorRole == LandActorRole.FirstActingSide
                && position.SegmentId == LandSegmentIds.ReserveRelease);
        var authority = new CampaignNativeReserveReleaseStateV27(
            request.CampaignId, session.CurrentSnapshot.StateVersion,
            "sha256:" + new string('1', 64), firstSide, 1,
            releasePosition, world, [], [], false);
        var (chosen, choiceEvent) = authority.Choose(session.Context,
            member.ElementId, CampaignReserveReleaseChoice.ConvertToII);
        Assert.Equal(CampaignElementReserveStatus.ReserveII,
            chosen.World.Elements.Single(value =>
                value.ElementId == member.ElementId).ReserveStatus);
        Assert.Empty(chosen.Pending(session.Context));
        var (finished, completionEvent) = chosen.Complete(session.Context);
        Assert.Equal(LandPhaseIds.TruckConvoyMovement,
            finished.Position.PhaseId);
        var replayed = CampaignNativeReserveReleaseStateV27.Replay(
            authority, session.Context,
            [choiceEvent.SerializeCanonical(), completionEvent.SerializeCanonical()]);
        Assert.Equal(finished.HeadEventHash, replayed.HeadEventHash);
        Assert.Equal(finished.World, replayed.World);
        Assert.Equal(finished.Position, replayed.Position);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeReserveReleaseStateV27.Replay(authority, session.Context,
                [(choiceEvent with
                {
                    ElementId = "forged-element",
                }).SerializeCanonical()]));

        var secondCycleAuthority = authority with
        {
            World = converted.World,
            CycleOrdinal = 2,
            ResolvedElementIds = [],
        };
        var (retained, retainEvent) = secondCycleAuthority.Choose(
            session.Context, member.ElementId,
            CampaignReserveReleaseChoice.RetainII);
        Assert.Empty(retained.Pending(session.Context));
        Assert.Equal(CampaignElementReserveStatus.ReserveII,
            retained.World.Elements.Single(value =>
                value.ElementId == member.ElementId).ReserveStatus);
        var (afterRetain, retainCompletion) = retained.Complete(session.Context);
        var replayedRetain = CampaignNativeReserveReleaseStateV27.Replay(
            secondCycleAuthority, session.Context,
            [retainEvent.SerializeCanonical(), retainCompletion.SerializeCanonical()]);
        Assert.Equal(afterRetain.HeadEventHash, replayedRetain.HeadEventHash);
        Assert.Equal(afterRetain.World, replayedRetain.World);
    }
}
