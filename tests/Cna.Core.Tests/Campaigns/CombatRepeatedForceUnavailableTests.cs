using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// #476 (owner ruling 2026-11-20): a repeated Force round gets the same pre-opening V19
/// force-unavailable check as the initial round. On combat-double-fresh the only repeated assault is
/// the spent first pair, so the round cannot be paid: it must take the cancellation and continue
/// through the existing V20/V21 cancelled-assault path instead of stalling.
/// </summary>
public sealed class CombatRepeatedForceUnavailableTests
{
    private const string SetupId = "rules-lab.breakdown.reaction.combat-double-fresh.v1";
    private static readonly string[] AxisInfantry = ["axis-infantry", "axis-infantry-second"];

    public static TheoryData<ulong> Seeds => new() { 5, 6, 11, 14 };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void UnpayableRepeatedRoundTakesTheForceUnavailableCancellationAndContinues(ulong seed)
    {
        var session = AtRepeatedForceRound($"campaign-476-repeat-{seed}", seed);
        Assert.False(CampaignHostAdapter.IsInitialNativeCombatForceRound(session));
        Assert.True(CampaignHostAdapter.IsRepeatedNativeCombatForceRound(session));
        Assert.False(CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(session));

        var cancelled = CampaignHostAdapter.CancelNativeCombatForceAssignment(session, null,
            -1, -1, -1, null, "force-unavailable");
        Assert.True(cancelled.IsAccepted, cancelled.RejectionReason.ToString());
        using (var document = JsonDocument.Parse(cancelled.Evidence!.EventRecords[0]))
        {
            var root = document.RootElement;
            Assert.Equal("force-unavailable", root.GetProperty("reason").GetString());
            Assert.Equal(CampaignHostAdapter.NoTimingForceCancellationHash,
                root.GetProperty("configurationHash").GetString());
            Assert.Equal(-1, root.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.False(root.TryGetProperty("retainedSealHash", out _));
        }
        session = cancelled.SuccessorSession!;
        Assert.Equal("force-unavailable", session.CurrentRepeatAssault!.ForceCancellation!.Reason);
        Assert.False(CampaignHostAdapter.IsRepeatedNativeCombatForceRound(session));
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);

        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatCancelledAssault(session));
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.NotNull(session.CurrentRepeatAssault!.CancelledAssault);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignHostAdapter.ResolveNativeCombatAssault(session).RejectionReason);
        // Not stalled: the cancelled-assault successor is the next repeated-cycle Reserve Release cut.
        Assert.EndsWith(".reserve-release", CampaignExercises.QueryCheckpoint(session).PositionId, StringComparison.Ordinal);
        Assert.Equal(LandSegmentIds.ReserveRelease, CampaignExercises.QueryNativeReserveReleaseState(session).Position.SegmentId);
    }

    [Fact]
    public void RepeatedForceUnavailableKeepsTheInitialRoundsRules()
    {
        var session = AtRepeatedForceRound("campaign-476-rules", 6);
        var timing = Timing(session);

        // Clock evidence or a non-standard reason without timing is refused, as for the initial round.
        Assert.False(CampaignHostAdapter.CancelNativeCombatForceAssignment(session, null,
            0, 30_000, 0, null, "force-unavailable").IsAccepted);
        Assert.False(CampaignHostAdapter.CancelNativeCombatForceAssignment(session, null,
            -1, -1, -1, null, "deadline").IsAccepted);
        // With timing authority an unpayable round is still cancelled force-unavailable, bound to that
        // configuration's hash.
        var timed = CampaignHostAdapter.CancelNativeCombatForceAssignment(session, timing,
            -1, -1, -1, null, "force-unavailable");
        Assert.True(timed.IsAccepted, timed.RejectionReason.ToString());
        using (var document = JsonDocument.Parse(timed.Evidence!.EventRecords[0]))
            Assert.Equal(timing.Hash, document.RootElement.GetProperty("configurationHash").GetString());

        // Once a seal exists the round has opened: force-unavailable no longer applies (the Host
        // verifies that such a cancellation retains no seal).
        var attacker = AxisInfantry.Single(element =>
            CampaignCombatAudienceDecisions.QueryForceAssignment(session, CampaignActionAudience.Axis, element) is not null);
        var sealedOnce = Submit(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Axis, attacker), session, 0);
        Assert.False(CampaignHostAdapter.IsRepeatedNativeCombatForceRound(sealedOnce));
        Assert.False(CampaignHostAdapter.CancelNativeCombatForceAssignment(sealedOnce, null,
            -1, -1, -1, null, "force-unavailable").IsAccepted);
        Assert.False(CampaignHostAdapter.CancelNativeCombatForceAssignment(sealedOnce, timing,
            -1, -1, -1, null, "force-unavailable").IsAccepted);
    }

    [Fact]
    public void InitialRoundBehaviorIsUnchanged()
    {
        var session = AtRepeatedSelection("campaign-476-initial", 6, out _, stopAtFirstForce: true);
        Assert.True(CampaignHostAdapter.IsInitialNativeCombatForceRound(session));
        Assert.False(CampaignHostAdapter.IsRepeatedNativeCombatForceRound(session));
        // The first pair is fresh: the initial round is payable, so force-unavailable with timing is refused.
        Assert.True(CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(session));
        Assert.False(CampaignHostAdapter.CancelNativeCombatForceAssignment(session, Timing(session),
            -1, -1, -1, null, "force-unavailable").IsAccepted);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RepeatedRoundPayabilityAgreesWithTheRepeatedResult(ulong seed)
    {
        // Every repeated assault the fixture offers, across both repeated-Movement choices: a payable
        // round (CanComplete) must settle a result, and an unpayable one must take the cancellation.
        var atMovement = AtRepeatedSelection($"campaign-476-agree-{seed}", seed, out _, atRepeatMovement: true);
        var movement = CampaignCombatAudienceDecisions.QueryRepeatMovement(atMovement, CampaignActionAudience.Axis)!;
        var payable = 0;
        var unpayable = 0;
        for (var move = 0; move < movement.OfferRefs.Count; move++)
        {
            var atSelection = Accept(CampaignHostAdapter.CompleteNativeRepeatBreakdown(Submit(movement, atMovement, move)));
            foreach (var actor in AxisInfantry)
            {
                if (CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(atSelection, CampaignActionAudience.Axis, actor)
                    is not { } selection) continue;
                for (var offer = 0; offer < selection.OfferRefs.Count - 1; offer++) // last is finish
                {
                    var barrage = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(Submit(selection, atSelection, offer)));
                    var rba = CampaignExercises.QueryContactRbaCut(barrage);
                    var defenderAudience = rba.DefenderSide == LandSide.Axis
                        ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
                    var round = Submit(CampaignCombatAudienceDecisions.QueryRbaDecline(barrage, defenderAudience,
                        rba.DefenderElementId), barrage, 0);
                    Assert.True(CampaignHostAdapter.IsRepeatedNativeCombatForceRound(round));
                    if (CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(round))
                    {
                        payable++;
                        Assert.False(CampaignHostAdapter.CancelNativeCombatForceAssignment(round, Timing(round),
                            -1, -1, -1, null, "force-unavailable").IsAccepted);
                        var sealedRound = Submit(CampaignCombatAudienceDecisions.QueryForceAssignment(round,
                            CampaignActionAudience.Axis, actor), round, 0);
                        sealedRound = Submit(CampaignCombatAudienceDecisions.QueryForceAssignment(sealedRound,
                            defenderAudience, rba.DefenderElementId), sealedRound, 0);
                        var armored = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(
                            Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(sealedRound))));
                        Assert.True(CampaignHostAdapter.ResolveNativeCombatAssault(armored).IsAccepted);
                    }
                    else
                    {
                        unpayable++;
                        var cancelled = Accept(CampaignHostAdapter.CancelNativeCombatForceAssignment(round, null,
                            -1, -1, -1, null, "force-unavailable"));
                        var armored = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(cancelled));
                        Assert.True(CampaignHostAdapter.CompleteNativeCombatCancelledAssault(armored).IsAccepted);
                    }
                }
            }
        }
        Assert.True(payable > 0 && unpayable > 0, $"payable={payable} unpayable={unpayable}");
    }

    private static CampaignDecisionTimingConfigurationV1 Timing(ExerciseSession session) => new(
        "rules-lab.force-timing.1", session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
        [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
            new("force-assignment", 30000, "cancel-incomplete-voluntary-round"),
            new("rba", 30000, "cancel-without-decline"), new("reserve-release", 30000, "convert-unresolved-i-retain-ii"),
            new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);

    private static ExerciseSession AtRepeatedForceRound(string campaignId, ulong seed)
    {
        var session = AtRepeatedRba(campaignId, seed);
        var rba = CampaignExercises.QueryContactRbaCut(session);
        var audience = rba.DefenderSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        return Submit(CampaignCombatAudienceDecisions.QueryRbaDecline(session, audience, rba.DefenderElementId), session, 0);
    }

    private static ExerciseSession AtRepeatedRba(string campaignId, ulong seed)
    {
        var session = AtRepeatedSelection(campaignId, seed, out var actor);
        session = Submit(CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.Axis, actor), session, 0);
        Assert.True(CampaignHostAdapter.IsNativeRepeatCombatSelectedAssault(session));
        return Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
    }

    private static ExerciseSession AtRepeatedSelection(string campaignId, ulong seed, out string actor,
        bool stopAtFirstForce = false, bool atRepeatMovement = false)
    {
        var session = AtCombatPosition(campaignId, seed);
        session = Submit(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, "axis-infantry"), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        var defender = rba.DefenderElementId;
        var defenderAudience = rba.DefenderSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var attackerAudience = defenderAudience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        session = Submit(CampaignCombatAudienceDecisions.QueryRbaDecline(session, defenderAudience, defender), session, 0);
        actor = "";
        if (stopAtFirstForce) return session;
        session = Submit(CampaignCombatAudienceDecisions.QueryForceAssignment(session, attackerAudience, "axis-infantry"), session, 0);
        session = Submit(CampaignCombatAudienceDecisions.QueryForceAssignment(session, defenderAudience, defender), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        session = Accept(CampaignHostAdapter.ResolveNativeCombatAssault(session));
        if (CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session, defenderAudience, defender) is { } retreat)
            session = Submit(retreat, session, retreat.OfferRefs.Count - 1);
        else session = Accept(CampaignHostAdapter.CompleteNoPostAssaultRetreat(session));
        session = Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
        if (CampaignCombatAudienceDecisions.QueryCustody(session, attackerAudience, "axis-infantry") is { } custody)
            session = Submit(custody, session, custody.OfferRefs.Count - 1);
        else session = Accept(CampaignHostAdapter.CompleteNoNativeCombatCustody(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatSuccessor(session));
        session = Submit(CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Axis, "axis-infantry-second"), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeReserveRelease(session));
        var control = CampaignCombatAudienceDecisions.QueryCycleControl(session, CampaignActionAudience.Axis)!;
        string[] choices;
        using (var visible = JsonDocument.Parse(control.VisibleBytes))
            choices = visible.RootElement.GetProperty("choices").EnumerateArray().Select(value => value.GetString()!).ToArray();
        session = Submit(control, session, Array.IndexOf(choices, "Repeat"));
        if (atRepeatMovement) return session;
        session = Submit(CampaignCombatAudienceDecisions.QueryRepeatMovement(session, CampaignActionAudience.Axis), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeRepeatBreakdown(session));
        var current = session;
        actor = AxisInfantry.First(element =>
            CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(current, CampaignActionAudience.Axis, element)
                is { OfferRefs.Count: > 1 });
        return session;
    }

    private static ExerciseSession Submit(CampaignCombatAudienceDecision? decision, ExerciseSession session, int ordinal)
    {
        Assert.NotNull(decision);
        var result = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
        Assert.True(result.IsAccepted, decision.Stage);
        return result.TrustedStep!.SuccessorSession!;
    }

    private static ExerciseSession AtCombatPosition(string campaignId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == SetupId));
        var session = CampaignExercises.Begin(new CampaignCreationRequest(1, campaignId, Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId)).Session!;
        var reserveDesignated = false;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var reserve = !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == "axis-infantry-second") : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
