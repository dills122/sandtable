using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// Task 021A1b real-data basis for manifest v2. Owner Combat decisions are taken only through the public
/// audience surface (v1 and contract-v2 stages), by ordinal over the published offers; System steps use
/// the trusted Host adapter, as the Host does. The private cut is read only to identify the bound
/// defender, which the test needs to address its decision.
/// </summary>
public sealed class CombatCapabilityAudienceTraceTests
{
    private const string SetupId = "rules-lab.breakdown.reaction.combat-double-fresh.v1";
    private static readonly string[] AxisInfantry = ["axis-infantry", "axis-infantry-second"];
    private static readonly string[] Infantry =
        ["axis-infantry", "axis-infantry-second", "commonwealth-infantry", "commonwealth-infantry-west"];
    private static readonly ulong[] AdmittedSeeds = [1, 2, 3, 5, 6];

    public static TheoryData<ulong> Seeds => new() { 5, 6, 11, 14 };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void EveryOwnerDecisionThroughTheRepeatedForceRoundIsServedByTheAudienceSurface(ulong seed)
    {
        var trace = new List<string>();
        var session = AtRepeatedSelection(trace, $"campaign-021a1b-trace-{seed}", seed, out var actor);
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.Axis, actor)), session, 0);
        Assert.True(CampaignHostAdapter.IsNativeRepeatCombatSelectedAssault(session));
        session = ToAntiArmor(trace, session).Session;
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);

        string[] expected =
        [
            CampaignCombatAudienceDecisions.SelectionStage, CampaignCombatAudienceDecisions.RbaDeclineStage,
            CampaignCombatAudienceDecisions.ForceAssignmentStage, CampaignCombatAudienceDecisions.ReserveReleaseStage,
            CampaignCombatAudienceDecisions.CycleControlStage, CampaignCombatAudienceDecisions.RepeatMovementStage,
            CampaignCombatAudienceDecisions.RepeatCombatSelectionStage,
        ];
        Assert.All(expected, stage => Assert.Contains(stage, trace));
        Assert.Equal(2, trace.Count(stage => stage == CampaignCombatAudienceDecisions.RbaDeclineStage));
        Assert.Equal(4, trace.Count(stage => stage == CampaignCombatAudienceDecisions.ForceAssignmentStage));
        Assert.Equal(seed != 5, trace.Contains(CampaignCombatAudienceDecisions.PostAssaultRetreatStage));
    }

    // Finding behind manifest v2's `two-runtime-assaults` = uncovered. The only repeated assault the
    // bounded fixture offers is the first pair again; after the first result its roles no longer meet
    // the bounded profile's full-TOE/Cohesion/CP/ammo bounds. The Host skips the V19 force-unavailable
    // check for repeat rounds (IsInitialNativeCombatForceRound is false), so the round opens and both
    // seals are accepted, and then neither the V21 cancellation nor the V22 result accepts: no
    // adjudication handler applies. Recorded, not fixed (Core/Host Combat logic, outside Task 021).
    // When it is fixed, this test fails and manifest v2's disposition must be revisited.
    [Fact]
    public void RepeatedAssaultOnTheSpentFirstPairHasNoResultOrCancellationHandler()
    {
        var trace = new List<string>();
        var session = AtRepeatedSelection(trace, "campaign-021a1b-repeat-gap", 6, out var actor);
        var repeat = CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session, CampaignActionAudience.Axis, actor)!;
        Assert.Equal(2, repeat.OfferRefs.Count); // one assault plus finish
        session = Submit(repeat, session, 0);
        Assert.False(CampaignHostAdapter.IsInitialNativeCombatForceRound(session));
        session = ToAntiArmor(trace, session).Session;
        Assert.False(CampaignHostAdapter.CompleteNativeCombatCancelledAssault(session).IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignHostAdapter.ResolveNativeCombatAssault(session).RejectionReason);
    }

    // Disposition basis for manifest v2's first admissions: where one side fields no element with a
    // combat component, no assault offer that ordinary play reaches can open a payable Force round, so the
    // result-dependent families are unreachable (T020-Q8: the V19 force-unavailable cancellation).
    [Theory]
    [InlineData("rules-lab.breakdown.reaction.noncombat.v1")]
    [InlineData("rules-lab.breakdown.truck-cost.v1")]
    [InlineData("rules-lab.breakdown.truck-one-point.v1")]
    public void AdmittedConfigurationsCannotReachAPayableAssault(string setupId)
    {
        var elements = SetupElements(setupId);
        var reachedCombat = 0;
        var selections = 0;
        var assaults = 0;
        var finishes = 0;
        foreach (var seed in AdmittedSeeds)
        {
            var session = AtCombatPosition($"campaign-021a1b-admitted-{seed}", seed, setupId);
            if (!AtPositionDetermination(session)) continue;
            reachedCombat++;
            foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
            {
                foreach (var element in elements)
                {
                    if (CampaignCombatAudienceDecisions.QuerySelection(session, audience, element) is not { } selection)
                        continue;
                    selections++;
                    for (var ordinal = 0; ordinal < selection.OfferRefs.Count; ordinal++)
                    {
                        var chosen = Submit(selection, session, ordinal);
                        if (!CampaignHostAdapter.IsNativeCombatSelectedAssault(chosen))
                        {
                            finishes += FinishReachesCycleControl(chosen, audience) ? 1 : 0;
                            continue;
                        }
                        assaults++;
                        var barrage = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(chosen));
                        Assert.False(CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(barrage),
                            $"{setupId} seed {seed}: payable assault");
                    }
                }
            }
        }
        // Measured: every seed reaches the Combat position and the acting side gets a public Selection
        // decision, but no assault is offered (no contact), so Combat reduces to finish-without-attack.
        Assert.Equal(AdmittedSeeds.Length, reachedCombat);
        Assert.Equal(AdmittedSeeds.Length, selections);
        Assert.Equal(0, assaults);
        Assert.Equal(AdmittedSeeds.Length, finishes);
    }

    // After a public finish, the System no-attack completion enters Reserve Release; every remaining
    // owner decision up to Cycle Control is served by a public audience decision.
    private static bool FinishReachesCycleControl(ExerciseSession session, CampaignActionAudience audience)
    {
        session = Accept(CampaignHostAdapter.CompleteNativeCombatNoAttack(session));
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        foreach (var member in release.Pending(session.Context).Select(value => value.ElementId).ToArray())
            session = Submit(Decision([], CampaignCombatAudienceDecisions.QueryReserveRelease(session, audience, member)),
                session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeReserveRelease(session));
        var control = CampaignCombatAudienceDecisions.QueryCycleControl(session, audience);
        return control is not null && CampaignCombatAudienceDecisions.Submit(session, control.Choose(0)).IsAccepted;
    }

    private static string[] SetupElements(string setupId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == setupId));
        return Cna1979SyntheticContentResolver.Instance.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash)
            .Artifact!.Definition.LegacyDefinition.Elements.Select(value => value.ElementId).ToArray();
    }

    private static ExerciseSession AtRepeatedSelection(List<string> trace, string campaignId, ulong seed, out string actor)
    {
        var session = AtCombatPosition(campaignId, seed);
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, "axis-infantry")), session, 0);
        session = SettleFirstAssault(trace, ToAntiArmor(trace, session));
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Axis, "axis-infantry-second")), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeReserveRelease(session));
        var control = Decision(trace, CampaignCombatAudienceDecisions.QueryCycleControl(session, CampaignActionAudience.Axis));
        string[] choices;
        using (var visible = JsonDocument.Parse(control.VisibleBytes))
            choices = visible.RootElement.GetProperty("choices").EnumerateArray().Select(value => value.GetString()!).ToArray();
        session = Submit(control, session, Array.IndexOf(choices, "Repeat"));
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QueryRepeatMovement(session,
            CampaignActionAudience.Axis)), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeRepeatBreakdown(session));
        var current = session;
        actor = AxisInfantry.First(element =>
            CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(current, CampaignActionAudience.Axis, element)
                is { OfferRefs.Count: > 1 });
        return session;
    }

    private sealed record ForceOutcome(ExerciseSession Session, string Attacker, CampaignActionAudience AttackerAudience,
        string Defender, CampaignActionAudience DefenderAudience);

    private static ForceOutcome ToAntiArmor(List<string> trace, ExerciseSession session)
    {
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session); // identifies the bound defender only
        var defender = rba.DefenderElementId;
        var defenderAudience = rba.DefenderSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var attackerAudience = defenderAudience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QueryRbaDecline(session, defenderAudience, defender)), session, 0);
        var current = session;
        var attacker = Infantry.Single(element => element != defender
            && CampaignCombatAudienceDecisions.QueryForceAssignment(current, attackerAudience, element) is not null);
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QueryForceAssignment(session, attackerAudience, attacker)), session, 0);
        session = Submit(Decision(trace, CampaignCombatAudienceDecisions.QueryForceAssignment(session, defenderAudience, defender)), session, 0);
        session = Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        return new(session, attacker, attackerAudience, defender, defenderAudience);
    }

    private static ExerciseSession SettleFirstAssault(List<string> trace, ForceOutcome outcome)
    {
        var session = outcome.Session;
        Assert.False(CampaignHostAdapter.CompleteNativeCombatCancelledAssault(session).IsAccepted);
        session = Accept(CampaignHostAdapter.ResolveNativeCombatAssault(session));
        if (CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session, outcome.DefenderAudience, outcome.Defender)
            is { } retreat)
            session = Submit(Decision(trace, retreat), session, retreat.OfferRefs.Count - 1); // refusal is last
        else session = Accept(CampaignHostAdapter.CompleteNoPostAssaultRetreat(session));
        session = Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
        if (CampaignCombatAudienceDecisions.QueryCustody(session, outcome.AttackerAudience, outcome.Attacker) is { } custody)
            session = Submit(Decision(trace, custody), session, custody.OfferRefs.Count - 1);
        else session = Accept(CampaignHostAdapter.CompleteNoNativeCombatCustody(session));
        return Accept(CampaignHostAdapter.CompleteNativeCombatSuccessor(session));
    }

    private static CampaignCombatAudienceDecision Decision(List<string> trace, CampaignCombatAudienceDecision? decision)
    {
        Assert.NotNull(decision);
        trace.Add(decision.Stage);
        return decision;
    }

    private static ExerciseSession Submit(CampaignCombatAudienceDecision decision, ExerciseSession session, int ordinal)
    {
        Assert.InRange(ordinal, 0, decision.OfferRefs.Count - 1);
        var result = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
        Assert.True(result.IsAccepted, decision.Stage);
        return result.TrustedStep!.SuccessorSession!;
    }

    private static bool AtPositionDetermination(ExerciseSession session)
    {
        var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
        return position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination;
    }

    private static ExerciseSession AtCombatPosition(string campaignId, ulong seed, string setupId = SetupId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == setupId));
        var session = CampaignExercises.Begin(new CampaignCreationRequest(1, campaignId, Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId)).Session!;
        var reserveDesignated = false;
        for (var index = 0; index < 80 && !AtPositionDetermination(session); index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            if (set is null) break;
            var reserve = !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == "axis-infantry-second") : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        return session;
    }

    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
