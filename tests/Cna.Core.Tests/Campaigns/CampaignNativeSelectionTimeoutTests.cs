using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignNativeSelectionTimeoutTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeadlineClosesActualSelectionWithoutOwnerChoiceOrNewDraw(bool repeated)
    {
        var session = AtSelection(repeated);
        var beforeWorld = repeated ? session.CurrentRepeatMoveWorld! : session.CurrentSnapshot.World;
        var accepted = Timeout(session);
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        var next = accepted.SuccessorSession!;
        var evidence = CampaignSelectionTimeoutEvidenceV38.Deserialize(accepted.Evidence!.EventRecords[0]);
        Assert.Equal(repeated ? 2 : 1, evidence.CycleOrdinal);
        Assert.Equal(CampaignActionAudience.System, accepted.Evidence.Receipt.Audience);
        Assert.Equal(CampaignActionAudience.System, CampaignHostAdapter.NativeCombatSelectionTimeoutCommand(session, accepted).Audience);
        Assert.Equal(38, CampaignExercises.QueryCheckpoint(next).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var completed = Accept(repeated ? CampaignHostAdapter.CompleteNativeRepeatCombatNoAttack(next)
            : CampaignHostAdapter.CompleteNativeCombatNoAttack(next));
        if (repeated) Assert.Equal(evidence.Hash,
            CampaignNativeRepeatCombatDecisionV31.Deserialize(next.CurrentRepeatCombatSelection!.SerializeCanonical()).ActionId);
        else Assert.Equal(evidence.Hash, completed.CurrentNoAttackCompletion!.DecisionActionId);
        var release = CampaignExercises.QueryNativeReserveReleaseState(completed);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(beforeWorld), CampaignNativeCombatPaidWorldV22.Hash(release.World));
        if (repeated)
        {
            Assert.Equal(session.CurrentCombatAssaultResult!.AfterRandomState, completed.CurrentPostRepeat!.RandomState);
            Assert.NotEqual(session.CurrentCombatAssaultResult.BeforeRandomState, completed.CurrentPostRepeat.RandomState);
        }
        else Assert.Equal(session.CurrentSnapshot.RandomState, completed.CurrentSnapshot.RandomState);
        Assert.True(CampaignExercises.Reconstruct(completed).IsVerified);
        Assert.False(Timeout(next).IsAccepted);
        using var json = JsonDocument.Parse(accepted.Evidence.EventRecords[0]);
        foreach (var property in new[] { "actionId", "choice", "actingElementId", "contactHandle", "boundRepresentationId" })
            Assert.False(json.RootElement.TryGetProperty(property, out _));
        Assert.Equal("no-selection", json.RootElement.GetProperty("fallback").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EarlyForeignConfigurationAndAlreadySelectedRejectWithoutMutation(bool repeated)
    {
        var session = AtSelection(repeated);
        var binding = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(session);
        var timing = Timing(session);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatSelection(session, timing, 1000, 31000, 1000, 30999, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatSelection(session, timing, 1000, 31001, 1000, 31001, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatSelection(session, null, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        foreach (var foreign in new[] { new CampaignDecisionTimingConfigurationV1(timing.ConfigId, ZeroHash, timing.ContentHash, timing.Windows),
            new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash, ZeroHash, timing.Windows) })
            Assert.False(CampaignHostAdapter.TimeoutNativeCombatSelection(session, foreign, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.Equal(binding, CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(session));
        var draft = repeated ? CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry")
            : CampaignHostAdapter.QueryContactCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
        var owner = repeated ? CampaignHostAdapter.SubmitNativeRepeatCombat(session, Command(session, CampaignActionAudience.Axis, draft.PrivateCombatActionIds[^1]), "axis-infantry")
            : CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, Command(session, CampaignActionAudience.Axis, draft.PrivateCombatActionIds[^1]), "axis-infantry");
        Assert.True(owner.IsAccepted);
        Assert.False(Timeout(owner.SuccessorSession!).IsAccepted);
    }

    [Theory]
    [InlineData("clock-unavailable", null)]
    [InlineData("clock-regression", 999L)]
    [InlineData("controller-unavailable", null)]
    public void FailureReasonsHaveExplicitReplayableEvidence(string reason, long? observed)
    {
        var session = AtSelection();
        var accepted = CampaignHostAdapter.TimeoutNativeCombatSelection(session, Timing(session), 1000, 31000, 1000, observed, reason);
        Assert.True(accepted.IsAccepted);
        Assert.Equal(reason, CampaignSelectionTimeoutEvidenceV38.Deserialize(accepted.Evidence!.EventRecords[0]).Reason);
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void UnavailableEntryHasNoInventedClockAndTamperingRejectsReplay()
    {
        var session = AtSelection();
        var accepted = CampaignHostAdapter.TimeoutNativeCombatSelection(session, null, -1, -1, -1, null, "selection-unavailable");
        Assert.True(accepted.IsAccepted);
        var evidence = CampaignSelectionTimeoutEvidenceV38.Deserialize(accepted.Evidence!.EventRecords[0]);
        Assert.Equal(ZeroHash, evidence.ConfigurationHash);
        Assert.Equal(-1, evidence.OpenedAtUnixMilliseconds);
        foreach (var forged in new[] { evidence with { EntryHash = ZeroHash }, evidence with { WorldHash = ZeroHash } })
            Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeCombatDecisionStateV12.Replay(session.CurrentSnapshot,
                session.Context, session.CurrentContacts, forged.SerializeCanonical()));
        Assert.Equal(accepted.Evidence.EventRecords[0], CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(accepted.Evidence.EventRecords[0])));
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void BindingRejectsOutsidePendingSelectionWithoutMutatingAuthority()
    {
        var pending = AtSelection();
        var setup = pending.CurrentSnapshot.Setup;
        var fresh = CampaignExercises.Begin(new CampaignCreationRequest(1, "campaign-selection-not-open",
            Cna1979Ruleset.Manifest.Hash, 6, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId)).Session!;
        var before = CampaignExercises.QueryCheckpoint(fresh);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(fresh));
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatSelection(fresh, null, -1, -1, -1, null, "selection-unavailable").IsAccepted);
        Assert.Equal(before.StateVersion, CampaignExercises.QueryCheckpoint(fresh).StateVersion);
        Assert.Null(fresh.CurrentCombatDecision);
    }

    private const string ZeroHash = CampaignSelectionTimeoutEvidenceV38.NoTimingConfigurationHash;
    private static ExerciseStepResult Timeout(ExerciseSession session) =>
        CampaignHostAdapter.TimeoutNativeCombatSelection(session, Timing(session), 1000, 31000, 1000, 31000, "deadline");
    private static CampaignDecisionTimingConfigurationV1 Timing(ExerciseSession session) => new(
        "rules-lab.selection-timing.1", session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
        [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
            new("force-assignment", 30000, "cancel-incomplete-voluntary-round"), new("rba", 30000, "cancel-without-decline"),
            new("reserve-release", 30000, "convert-unresolved-i-retain-ii"), new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);

    private static ExerciseSession AtSelection(bool repeated = false)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == (repeated ? "rules-lab.breakdown.reaction.combat-double-fresh.v1" : "rules-lab.breakdown.reaction.combat-assault.v1")));
        var request = new CampaignCreationRequest(1, "campaign-selection-timeout", Cna1979Ruleset.Manifest.Hash,
            6, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        var reserveDesignated = false;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination) break;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var reserve = repeated && !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == "axis-infantry-second") : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        if (!repeated) return session;
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
        session = Accept(CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, Command(session, CampaignActionAudience.Axis, CampaignExercises.QueryContactOwnerCombatCut(session, LandSide.Axis, "axis-infantry").AssaultOffers.Single(value => value.ApparentLocationId == "north-east").ActionId), "axis-infantry"));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        session = Accept(CampaignExercises.SubmitContactDefenderRbaDecline(session, Command(session, CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId));
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var element = audience == CampaignActionAudience.Axis ? "axis-infantry" : rba.DefenderElementId;
            var force = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element);
            session = Accept(CampaignHostAdapter.SubmitContactOwnerForceSeal(session, Command(session, audience, force.PrivateActionIds[0]), element));
        }
        session = Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        session = Accept(CampaignHostAdapter.ResolveNativeCombatAssault(session));
        if (session.CurrentCombatAssaultResult!.Facts.RequiredRetreat == 1)
        {
            var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
            session = Accept(CampaignExercises.SubmitContactPostResultRetreat(session,
                Command(session, CampaignActionAudience.Commonwealth, cut.RefuseOffer.ActionId), cut.DefenderElementId));
        }
        else session = Accept(CampaignHostAdapter.CompleteNoPostAssaultRetreat(session));
        session = Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
        var custody = CampaignExercises.QueryNativeCombatCustodyCut(session);
        if (custody is not null)
        {
            var unguarded = custody.Choices.Single(value => value.Kind == "leave-unguarded");
            session = Accept(CampaignExercises.SubmitNativeCombatCustody(session, Command(session, CampaignActionAudience.Axis, unguarded.ActionId)));
        }
        else session = Accept(CampaignHostAdapter.CompleteNoNativeCombatCustody(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatSuccessor(session));
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        if (release.Pending(session.Context).Count > 0)
        {
            var releaseDraft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry-second");
            session = Accept(CampaignHostAdapter.SubmitNativeReserveReleaseChoice(session,
                Command(session, CampaignActionAudience.Axis, releaseDraft.PrivateActionIds[0]), "axis-infantry-second"));
        }
        session = Accept(CampaignHostAdapter.CompleteNativeReserveRelease(session));
        release = CampaignExercises.QueryNativeReserveReleaseState(session);
        var cycleCut = CampaignCycleControlOwnerCutV28.Project(release, session.Context, session.CurrentContacts, LandSide.Axis);
        var repeatIndex = cycleCut.Choices.ToList().IndexOf(CampaignNativeCycleControlChoiceV28.Repeat);
        Assert.True(repeatIndex >= 0);
        session = Accept(CampaignHostAdapter.SubmitNativeCycleControl(session, Command(session, CampaignActionAudience.Axis, cycleCut.ActionIds[repeatIndex])));
        var moves = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis);
        session = Accept(CampaignHostAdapter.SubmitNativeRepeatMovement(session, Command(session, CampaignActionAudience.Axis, moves.PrivateActionIds[0])));
        session = Accept(CampaignHostAdapter.CompleteNativeRepeatBreakdown(session));
        return session;
    }
    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }
    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
