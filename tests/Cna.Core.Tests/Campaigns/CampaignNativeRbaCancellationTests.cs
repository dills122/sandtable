using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignNativeRbaCancellationTests
{
    [Fact]
    public void DeadlineCancelsWithoutDeclineAndReconstructsUnchangedWorld()
    {
        var session = AtRba();
        var before = session.CurrentSnapshot;
        var result = Cancel(session);
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        var after = result.SuccessorSession!;
        var cancelled = after.CurrentCombatRbaCancellation!;
        Assert.Equal(session.CurrentContacts.AuthorityVersion + 1, after.CurrentContacts.AuthorityVersion);
        Assert.Null(after.CurrentCombatRetreatDecline);
        Assert.Empty(after.CurrentCombatForceSeals);
        Assert.Null(after.CurrentCombatAssaultResult);
        Assert.Equal(before.World, after.CurrentSnapshot.World);
        Assert.Equal(before.RandomState, after.CurrentSnapshot.RandomState);
        Assert.Contains("reserve-release", CampaignExercises.QueryCheckpoint(after).PositionId, StringComparison.Ordinal);
        Assert.Equal(cancelled, CampaignCurrentEventSerializer.Deserialize(cancelled.SerializeCanonical()));
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.False(Cancel(after).IsAccepted);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryNativeCombatRbaRoundBinding(after));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryContactRbaCut(after));
        Assert.Equal(before.World, CampaignExercises.QueryNativeReserveReleaseState(after).World);
        var released = CampaignExercises.CompleteNativeReserveRelease(after);
        Assert.True(released.IsAccepted, released.RejectionReason.ToString());
        Assert.NotNull(released.SuccessorSession!.CurrentCombatRbaCancellation);
        Assert.True(CampaignExercises.Reconstruct(released.SuccessorSession).IsVerified);
    }

    [Fact]
    public void RejectionAndClockBoundariesDoNotMutateRba()
    {
        var session = AtRba();
        var timing = Timing(session);
        Assert.False(CampaignExercises.CancelNativeCombatRba(session, timing, 1000, 31000, 2000, 30999, "deadline").IsAccepted);
        Assert.False(CampaignExercises.CancelNativeCombatRba(session, timing, 1000, 31000, 2000, 2000, "clock-regression").IsAccepted);
        Assert.True(CampaignExercises.CancelNativeCombatRba(session, timing, 1000, 31000, 2000, 1999, "clock-regression").IsAccepted);
        Assert.True(CampaignExercises.CancelNativeCombatRba(session, timing, 1000, 31000, 2000, null, "clock-unavailable").IsAccepted);
        Assert.False(CampaignExercises.CancelNativeCombatRba(session, timing, 1000, 31001, 2000, 31001, "deadline").IsAccepted);
        Assert.False(CampaignExercises.CancelNativeCombatRba(session, null, 1000, 31000, 2000, 31000, "deadline").IsAccepted);
        Assert.False(CampaignExercises.CancelNativeCombatRba(session, timing, -1, 31000, -1, null, "rba-unavailable").IsAccepted);
        Assert.True(CampaignExercises.CancelNativeCombatRba(session, null, -1, -1, -1, null, "rba-unavailable").IsAccepted);
        Assert.Null(session.CurrentCombatRbaCancellation);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var declined = CampaignExercises.SubmitContactDefenderRbaDecline(session,
            new CampaignActionSubmission(1, session.CurrentSnapshot.CampaignId, cut.AuthorityVersion,
                session.CurrentCombatBarrage!.NextPositionId, CampaignActionAudience.Commonwealth, cut.DeclineOffer.ActionId), cut.DefenderElementId);
        Assert.True(declined.IsAccepted);
        Assert.False(Cancel(declined.SuccessorSession!).IsAccepted);
    }

    [Fact]
    public void StrictReplayRejectsChangedBarrageBindingAndUnknownFields()
    {
        var session = AtRba();
        var cancelled = Cancel(session).SuccessorSession!.CurrentCombatRbaCancellation!;
        var forged = cancelled with { BarrageEventHash = CampaignNativeCombatRbaCancellationV33.NoTimingConfigurationHash };
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeCombatRbaCancellationV33.Replay(
            session.CurrentCombatDecision!, session.CurrentCombatBarrage!, null, forged.SerializeCanonical()));
        var text = Encoding.UTF8.GetString(cancelled.SerializeCanonical());
        Assert.Throws<JsonException>(() => CampaignNativeCombatRbaCancellationV33.Deserialize(
            Encoding.UTF8.GetBytes(text[..^1] + ",\"extra\":true}")));
    }

    [Fact]
    public void AcceptedRetreatRemainsAppliedAndDeadlineDoesNotRestart()
    {
        var session = AtRba();
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var move = Assert.Single(cut.MoveOffers.Take(1));
        var binding = CampaignHostAdapter.QueryNativeCombatRbaRoundBinding(session);
        var moved = CampaignExercises.SubmitContactDefenderRbaMove(session,
            new CampaignActionSubmission(1, session.CurrentSnapshot.CampaignId, cut.AuthorityVersion,
                session.CurrentCombatBarrage!.NextPositionId, CampaignActionAudience.Commonwealth, move.ActionId),
            cut.DefenderElementId);
        Assert.True(moved.IsAccepted, moved.RejectionReason.ToString());
        var movedSession = moved.SuccessorSession!;
        Assert.Equal(binding, CampaignHostAdapter.QueryNativeCombatRbaRoundBinding(movedSession));
        var cancelled = Cancel(movedSession);
        Assert.True(cancelled.IsAccepted, cancelled.RejectionReason.ToString());
        Assert.NotNull(cancelled.SuccessorSession!.CurrentCombatRbaCancellation!.RetainedRetreatHash);
        Assert.Equal(movedSession.CurrentCombatRetreatWorld,
            CampaignExercises.QueryNativeReserveReleaseState(cancelled.SuccessorSession).World);
        Assert.True(CampaignExercises.Reconstruct(cancelled.SuccessorSession).IsVerified);
        Assert.False(CampaignExercises.CancelNativeCombatRba(movedSession, null, -1, -1, -1, null, "rba-unavailable").IsAccepted);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void RepeatedRuntimeCancelsAtItsActualOrdinalWithoutRandomDraw(int ordinal)
    {
        var session = AtRba();
        var initial = session.CurrentCombatDecision!;
        var decision = initial.Decision;
        var selection = new CampaignNativeRepeatCombatDecisionV31(31, initial.CampaignId,
            session.CurrentContacts.AuthorityVersion, CampaignNativeCombatRbaCancellationV33.NoTimingConfigurationHash,
            ordinal, decision.PositionId, decision.NextPositionId, decision.Choice, decision.Side,
            decision.ActingElementId, decision.ContactHandle, decision.ApparentLocationId,
            decision.BoundRepresentationId, decision.ActionId);
        var frame = CampaignCombatSelectionFrame.FromRepeat(session.CurrentSnapshot, selection,
            session.CurrentSnapshot.World, session.CurrentSnapshot.RandomState);
        var contacts = session.CurrentContacts.AdvanceNativeRepeatCombatSelection(selection);
        var barrage = CampaignNativeCombatBarrageV14.Create(frame, session.Context);
        var cancellation = CampaignNativeCombatRbaCancellationV33.Create(frame, barrage, null,
            Timing(session).Hash, 1000, 31000, 1000, 31000, "deadline");
        var runtime = CampaignNativeRepeatAssaultRuntime.Replay(session.CurrentSnapshot, selection,
            frame.World, frame.RandomState, session.Context, contacts, session.CurrentHistory,
            [barrage, cancellation]);
        Assert.Equal(ordinal, runtime.Release!.CycleOrdinal);
        Assert.Equal(frame.World, runtime.Release.World);
        Assert.Equal(frame.RandomState, runtime.Selection.RandomState);
        Assert.Null(runtime.Decline);
        Assert.Null(runtime.Result);
        Assert.Equal(cancellation, runtime.RbaCancellation);
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeRepeatAssaultRuntime.Replay(
            session.CurrentSnapshot, selection, frame.World, frame.RandomState, session.Context, contacts,
            session.CurrentHistory, [barrage, cancellation, cancellation]));
    }

    private static ExerciseStepResult Cancel(ExerciseSession session) =>
        CampaignExercises.CancelNativeCombatRba(session, Timing(session), 1000, 31000, 1000, 31000, "deadline");

    private static CampaignDecisionTimingConfigurationV1 Timing(ExerciseSession session) => new(
        "rules-lab.rba-timing.1", session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
        [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
            new("force-assignment", 30000, "cancel-incomplete-voluntary-round"),
            new("rba", 30000, "cancel-without-decline"), new("reserve-release", 30000, "convert-unresolved-i-retain-ii"),
            new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);

    private static ExerciseSession AtRba()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(
            value => value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var request = new CampaignCreationRequest(1, "campaign-rba-cancellation", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination) break;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = position.SegmentId == LandSegmentIds.Movement
                ? set.Candidates.OfType<CompleteMovementSegmentAction>().FirstOrDefault() ?? set.Candidates[0] : set.Candidates[0];
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        var offer = Assert.Single(CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, LandSide.Axis, "axis-infantry").AssaultOffers);
        var selected = CampaignNativeCombatDecisionStateV12.Select(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, LandSide.Axis, "axis-infantry", offer.ActionId);
        var barrage = CampaignNativeCombatBarrageV14.Create(selected, session.Context);
        return new ExerciseSession(session.CurrentSnapshot, session.Context, session.CurrentHistory.Add(selected.Decision).Add(barrage),
            session.CurrentContacts.AdvanceNativeCombatSelection(selected).AdvanceNativeCombatBarrage(barrage),
            selected, combatBarrage: barrage);
    }
}
