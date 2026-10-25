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

public sealed class CampaignNativeRetreatTimeoutTests
{
    [Fact]
    public void DeadlineRefusesWithSourceLossesSystemEvidenceAndColdReplay()
    {
        var session = AtRetreat();
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
        var owner = CampaignExercises.SubmitContactPostResultRetreat(session,
            Command(session, CampaignActionAudience.Commonwealth, cut.RefuseOffer.ActionId), cut.DefenderElementId);
        Assert.True(owner.IsAccepted);
        var accepted = Timeout(session);
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        var next = accepted.SuccessorSession!;
        var evidence = Assert.IsType<CampaignNativeCombatRetreatTimeoutV37>(next.CurrentCombatPostResultRetreat);
        Assert.Equal("refused", evidence.Disposition);
        Assert.Null(evidence.ActionId);
        Assert.Equal(CampaignActionAudience.System, accepted.Evidence!.Receipt.Audience);
        Assert.Equal(CampaignActionAudience.System, CampaignHostAdapter.NativeCombatRetreatTimeoutCommand(session, accepted).Audience);
        Assert.Equal(37, CampaignExercises.QueryCheckpoint(next).ContractVersion);
        Assert.Equal(session.CurrentSnapshot.RandomState, next.CurrentSnapshot.RandomState);
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var settled = Accept(CampaignHostAdapter.SettleNativeCombatWorld(next));
        var ownerSettled = Accept(CampaignHostAdapter.SettleNativeCombatWorld(owner.SuccessorSession!));
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(ownerSettled.CurrentCombatSettledWorld!), CampaignNativeCombatPaidWorldV22.Hash(settled.CurrentCombatSettledWorld!));
        Assert.True(CampaignExercises.Reconstruct(settled).IsVerified);
        Assert.False(Timeout(next).IsAccepted);
        Assert.False(Timeout(owner.SuccessorSession!).IsAccepted);
        using var json = JsonDocument.Parse(evidence.SerializeCanonical());
        Assert.False(json.RootElement.TryGetProperty("actionId", out _));
        Assert.Equal("refuse-retreat", json.RootElement.GetProperty("fallback").GetString());
    }

    [Fact]
    public void InvalidTimingDoesNotMutatePendingRetreat()
    {
        var session = AtRetreat();
        var timing = Timing(session);
        var binding = CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(session);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatRetreat(session, timing, 1000, 31000, 1000, 30999, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatRetreat(session, timing, 1000, 31001, 1000, 31001, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatRetreat(session, null, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        foreach (var foreign in new[] {
            new CampaignDecisionTimingConfigurationV1(timing.ConfigId, ZeroHash, timing.ContentHash, timing.Windows),
            new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash, ZeroHash, timing.Windows) })
            Assert.False(CampaignHostAdapter.TimeoutNativeCombatRetreat(session, foreign, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.Equal(binding, CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(session));
        Assert.Null(session.CurrentCombatPostResultRetreat);
    }

    [Theory]
    [InlineData("clock-unavailable", null)]
    [InlineData("clock-regression", 999L)]
    [InlineData("controller-unavailable", null)]
    public void ClockFailuresKeepExplicitSystemReason(string reason, long? observed)
    {
        var session = AtRetreat();
        var accepted = CampaignHostAdapter.TimeoutNativeCombatRetreat(session, Timing(session), 1000, 31000, 1000, observed, reason);
        Assert.True(accepted.IsAccepted);
        Assert.Equal(reason, Assert.IsType<CampaignNativeCombatRetreatTimeoutV37>(accepted.SuccessorSession!.CurrentCombatPostResultRetreat).Reason);
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void UnavailableEntryHasNoInventedClock()
    {
        var session = AtRetreat();
        var accepted = CampaignHostAdapter.TimeoutNativeCombatRetreat(session, null, -1, -1, -1, null, "retreat-unavailable");
        Assert.True(accepted.IsAccepted);
        var evidence = Assert.IsType<CampaignNativeCombatRetreatTimeoutV37>(accepted.SuccessorSession!.CurrentCombatPostResultRetreat);
        Assert.Equal(ZeroHash, evidence.ConfigurationHash);
        Assert.Equal(-1, evidence.OpenedAtUnixMilliseconds);
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void TamperedResultWorldAndTimingEvidenceRejectReplay()
    {
        var session = AtRetreat();
        var evidence = Assert.IsType<CampaignNativeCombatRetreatTimeoutV37>(Timeout(session).SuccessorSession!.CurrentCombatPostResultRetreat);
        foreach (var forged in new[] { evidence with { ResultHash = ZeroHash }, evidence with { PaidWorldHash = ZeroHash },
            evidence with { RetreatSourceHash = ZeroHash }, evidence with { DefenderElementId = "foreign-element" } })
            Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeCombatRetreatAfterAssaultV23.Replay(
                session.CurrentCombatDecision!, session.CurrentCombatAssaultResult!, session.CurrentCombatPaidWorld!, session.Context,
                session.CurrentContacts, forged.SerializeCanonical()));
        var bytes = evidence.SerializeCanonical();
        Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(bytes)));
        Assert.Throws<JsonException>(() => (evidence with { CancelledAtUnixMilliseconds = 30999 }).SerializeCanonical());
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Throws<JsonException>(() => CampaignNativeCombatRetreatTimeoutV37.Deserialize(Encoding.UTF8.GetBytes(text[..^1] + ",\"owner\":true}")));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void RepeatedRuntimeRetainsActualOrdinalAndRefusalEvidence(int ordinal)
    {
        var session = AtRetreat(stopAfterSelection: true);
        var initial = session.CurrentCombatDecision!;
        var decision = initial.Decision;
        var selection = new CampaignNativeRepeatCombatDecisionV31(31, initial.CampaignId,
            session.CurrentContacts.AuthorityVersion, ZeroHash, ordinal, decision.PositionId,
            decision.NextPositionId, decision.Choice, decision.Side, decision.ActingElementId,
            decision.ContactHandle, decision.ApparentLocationId, decision.BoundRepresentationId, decision.ActionId);
        var frame = CampaignCombatSelectionFrame.FromRepeat(session.CurrentSnapshot, selection,
            session.CurrentSnapshot.World, session.CurrentSnapshot.RandomState);
        var contacts = session.CurrentContacts.AdvanceNativeRepeatCombatSelection(selection);
        var afterSelection = contacts;
        var barrage = CampaignNativeCombatBarrageV14.Create(frame, session.Context);
        contacts = contacts.AdvanceNativeCombatBarrage(barrage);
        var rbaCut = CampaignContactRetreatBeforeAssaultCut.Project(frame, barrage, session.Context, contacts);
        var decline = CampaignNativeCombatRetreatDeclineV15.Create(frame, barrage, session.Context, contacts, rbaCut.DeclineOffer.ActionId);
        contacts = contacts.AdvanceNativeCombatRetreatDecline(decline);
        var seals = new List<CampaignNativeCombatForceSealV17>();
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
        {
            var cut = CampaignContactForceAssignmentCut.Project(frame, decline, frame.World, session.Context, contacts, side);
            var seal = CampaignNativeCombatForceSealV17.Create(frame, decline, frame.World, session.Context, contacts,
                side, cut.SealOffer.ActionId, seals.FirstOrDefault());
            seals.Add(seal);
            contacts = contacts.AdvanceNativeCombatForceSeal(seal);
        }
        var completion = CampaignNativeCombatForceCompletionV18.Create(frame, decline, seals);
        contacts = contacts.AdvanceNativeCombatForceCompletion(completion);
        var antiArmor = CampaignNativeCombatAntiArmorV20.Create(frame, completion, null, session.Context);
        contacts = contacts.AdvanceNativeCombatAntiArmor(antiArmor);
        var inputs = CampaignNativeCombatAssaultInputs.Create(frame, completion, antiArmor, seals, frame.World,
            session.Context, CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(session.Context, session.CurrentHistory));
        var result = CampaignNativeCombatAssaultResultV22.Create(frame, antiArmor, inputs, frame.RandomState);
        contacts = contacts.AdvanceNativeCombatAssaultResult(result);
        Assert.Equal(1, result.Facts.RequiredRetreat);
        var timeout = CampaignNativeCombatRetreatTimeoutV37.CreateTimeout(frame, result, inputs.PaidWorld,
            session.Context, contacts, Timing(session).Hash, 1000, 31000, 1000, 31000, "deadline");
        object[] events = [barrage, decline, .. seals, completion, antiArmor, result, timeout];
        var replay = CampaignNativeRepeatAssaultRuntime.Replay(session.CurrentSnapshot, selection, frame.World,
            frame.RandomState, session.Context, afterSelection, session.CurrentHistory, events);
        Assert.Equal(ordinal, CampaignNativeRepeatCombatDecisionV31.Deserialize(replay.Selection.SerializeCanonical()).CycleOrdinal);
        Assert.Equal(timeout.SerializeCanonical(), replay.PostResultRetreat!.SerializeCanonical());
        Assert.Equal(result.AfterRandomState, replay.Result!.AfterRandomState);
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeRepeatAssaultRuntime.Replay(
            session.CurrentSnapshot, selection, frame.World, frame.RandomState, session.Context,
            afterSelection, session.CurrentHistory, [.. events, timeout]));
    }

    private const string ZeroHash = CampaignNativeCombatRetreatTimeoutV37.NoTimingConfigurationHash;
    private static ExerciseStepResult Timeout(ExerciseSession session) =>
        CampaignHostAdapter.TimeoutNativeCombatRetreat(session, Timing(session), 1000, 31000, 1000, 31000, "deadline");
    private static CampaignDecisionTimingConfigurationV1 Timing(ExerciseSession session) => new(
        "rules-lab.retreat-timing.1", session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
        [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
            new("force-assignment", 30000, "cancel-incomplete-voluntary-round"), new("rba", 30000, "cancel-without-decline"),
            new("reserve-release", 30000, "convert-unresolved-i-retain-ii"), new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);

    private static ExerciseSession AtRetreat(bool stopAfterSelection = false)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var request = new CampaignCreationRequest(1, "campaign-retreat-timeout", Cna1979Ruleset.Manifest.Hash,
            6, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
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
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
        session = Accept(CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, Command(session, CampaignActionAudience.Axis, draft.PrivateCombatActionIds[0]), "axis-infantry"));
        if (stopAfterSelection) return session;
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        session = Accept(CampaignExercises.SubmitContactDefenderRbaDecline(session, Command(session, CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId));
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var element = audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
            var force = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element);
            session = Accept(CampaignHostAdapter.SubmitContactOwnerForceSeal(session, Command(session, audience, force.PrivateActionIds[0]), element));
        }
        session = Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        session = Accept(CampaignHostAdapter.ResolveNativeCombatAssault(session));
        Assert.Equal(1, session.CurrentCombatAssaultResult!.Facts.RequiredRetreat);
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
