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
        var ownerCut = CampaignReserveReleaseOwnerCutV27.Project(authority,
            session.Context, firstSide, member.ElementId);
        Assert.Equal(2, ownerCut.ActionIds.Count);
        var hiddenAuthorityChange = authority with
        {
            StateVersion = authority.StateVersion + 1,
            HeadEventHash = "sha256:" + new string('2', 64),
        };
        var equalVisibleCut = CampaignReserveReleaseOwnerCutV27.Project(
            hiddenAuthorityChange, session.Context, firstSide, member.ElementId);
        Assert.Equal(ownerCut.VisibleBytes, equalVisibleCut.VisibleBytes);
        Assert.Equal(ownerCut.ActionIds, equalVisibleCut.ActionIds);
        Assert.Throws<InvalidOperationException>(() =>
            CampaignReserveReleaseOwnerCutV27.Project(authority,
                session.Context, otherSide, member.ElementId));
        var (chosen, choiceEvent) = authority.Choose(session.Context,
            member.ElementId, CampaignReserveReleaseChoice.ConvertToII);
        Assert.Equal(CampaignElementReserveStatus.ReserveII,
            chosen.World.Elements.Single(value =>
                value.ElementId == member.ElementId).ReserveStatus);
        Assert.Empty(chosen.Pending(session.Context));
        var (finished, completionEvent) = chosen.Complete(session.Context);
        Assert.Equal(LandSegmentIds.ReserveRelease,
            finished.Position.SegmentId);
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

        var (releasedAuthority, releaseChoiceEvent) = authority.Choose(
            session.Context, member.ElementId, CampaignReserveReleaseChoice.ReleaseI);
        var (completedRelease, releasedCompletionEvent) =
            releasedAuthority.Complete(session.Context);
        var contactCut = session.CurrentContacts
            .AdvanceNativeReserveRelease(releaseChoiceEvent)
            .AdvanceNativeReserveRelease(releasedCompletionEvent);
        var witness = CampaignCycleContinuationMovementWitnessV28.Find(
            completedRelease, session.Context, contactCut);
        Assert.NotNull(witness);
        var controlCut = CampaignCycleControlOwnerCutV28.Project(
            completedRelease, session.Context, contactCut, firstSide);
        Assert.Equal([CampaignNativeCycleControlChoiceV28.Repeat,
            CampaignNativeCycleControlChoiceV28.Finish], controlCut.Choices);
        var repeat = CampaignNativeCycleControlV28.Create(completedRelease,
            session.Context, contactCut, CampaignNativeCycleControlChoiceV28.Repeat);
        Assert.Equal(2, repeat.NextCycleOrdinal);
        Assert.Equal(witness.ActionId, repeat.MovementWitnessActionId);
        Assert.Equal(repeat, CampaignNativeCycleControlV28.Replay(
            completedRelease, session.Context, contactCut, repeat.SerializeCanonical()));
        var repeatContacts = contactCut.AdvanceNativeCycleControl(repeat);
        var repeatedMove = CampaignNativeRepeatMovementV29.Create(
            completedRelease, repeat, session.Context, repeatContacts,
            witness.ActionId);
        var repeatedWorld = repeatedMove.ProjectWorld(completedRelease,
            repeat, session.Context, repeatContacts);
        Assert.Equal(witness.DestinationLocationId,
            repeatedWorld.Elements.Single(value =>
                value.ElementId == witness.ElementId).CurrentLocationId);
        var afterMovementContacts = repeatContacts.AdvanceNativeRepeatMove(
            completedRelease.World, repeatedWorld, repeatedMove, session.Context);
        Assert.Equal(repeatedMove.StateVersion,
            afterMovementContacts.AuthorityVersion);
        Assert.Equal(repeatedMove, CampaignNativeRepeatMovementV29.Replay(
            completedRelease, repeat, session.Context, repeatContacts,
            repeatedMove.SerializeCanonical()));
        Assert.Equal(repeatedMove.SerializeCanonical(),
            CampaignCurrentEventSerializer.Serialize(
                CampaignCurrentEventSerializer.Deserialize(
                    repeatedMove.SerializeCanonical())));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeRepeatMovementV29.Replay(completedRelease,
                repeat, session.Context, repeatContacts,
                (repeatedMove with { ResultWorldHash = "sha256:" + new string('0', 64) })
                    .SerializeCanonical()));
        var hiddenPrefixFork = completedRelease with
        {
            HeadEventHash = "sha256:" + new string('3', 64),
        };
        var repeatFork = CampaignNativeCycleControlV28.Create(hiddenPrefixFork,
            session.Context, contactCut, CampaignNativeCycleControlChoiceV28.Repeat);
        var visibleMovement = CampaignRepeatMovementOwnerCutV29.Project(
            completedRelease, repeat, session.Context, repeatContacts, firstSide);
        var forkMovement = CampaignRepeatMovementOwnerCutV29.Project(
            hiddenPrefixFork, repeatFork, session.Context,
            contactCut.AdvanceNativeCycleControl(repeatFork), firstSide);
        Assert.Equal(visibleMovement.VisibleBytes, forkMovement.VisibleBytes);
        Assert.Equal(visibleMovement.ActionIds, forkMovement.ActionIds);
        var equalVisibleControl = CampaignCycleControlOwnerCutV28.Project(
            hiddenPrefixFork, session.Context, contactCut, firstSide);
        Assert.Equal(controlCut.VisibleBytes, equalVisibleControl.VisibleBytes);
        Assert.Equal(controlCut.ActionIds, equalVisibleControl.ActionIds);
        Assert.Equal(repeat.MovementWitnessActionId,
            repeatFork.MovementWitnessActionId);
        Assert.NotEqual(repeat.PriorEventHash, repeatFork.PriorEventHash);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCycleControlV28.Replay(completedRelease,
                session.Context, contactCut, repeatFork.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCycleControlV28.Create(finished, session.Context,
                session.CurrentContacts.AdvanceNativeReserveRelease(choiceEvent)
                    .AdvanceNativeReserveRelease(completionEvent),
                CampaignNativeCycleControlChoiceV28.Repeat));
        var noReleaseCut = CampaignCycleControlOwnerCutV28.Project(finished,
            session.Context,
            session.CurrentContacts.AdvanceNativeReserveRelease(choiceEvent)
                .AdvanceNativeReserveRelease(completionEvent), firstSide);
        Assert.Equal([CampaignNativeCycleControlChoiceV28.Finish],
            noReleaseCut.Choices);

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
