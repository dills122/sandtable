using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 021A0 (evidence only, no product change): which owner Combat decisions the bounded
/// profile reaches through ordinary Core authority, compared with the six round kinds the frozen
/// <c>combat-audience-decision-v1</c> contract can serve. The fog spike's activation rule requires
/// every reachable family to have a defined disclosure and owner action before a campaign is
/// activated. These tests pin where v1 alone fails that closure, so the capability manifest work
/// in Task 021 starts from measured reachability rather than assumed coverage. Task 020E later
/// served these decisions with contract-v2 stages; the v1-only evidence here stays true.
/// </summary>
public sealed class CombatCycleAdmissionReachabilityTests
{
    private const string SetupId = "rules-lab.breakdown.reaction.combat-double-fresh.v1";

    private static readonly string[] FrozenStages =
    [
        CampaignCombatAudienceDecisions.SelectionStage,
        CampaignCombatAudienceDecisions.ForceAssignmentStage,
        CampaignCombatAudienceDecisions.ReserveReleaseStage,
        CampaignCombatAudienceDecisions.CycleControlStage,
        CampaignCombatAudienceDecisions.CustodyStage,
        CampaignCombatAudienceDecisions.RbaDeclineStage,
    ];

    private static readonly string[] Subjects =
    [
        "axis-infantry", "axis-infantry-second", "axis-truck", "commonwealth-infantry",
        "commonwealth-infantry-west", "commonwealth-truck", CampaignCombatAudienceDecisions.CycleControlSubject,
    ];

    [Fact]
    public void TheSixV1StagesAreDeclaredOnThePublicSurface()
    {
        // Guard for the evidence below, which enumerates the six v1 stages explicitly. The public
        // surface also declares later stages (contract v2, Task 020E); they are deliberately not part
        // of this list, because the finding here is what v1 alone could serve.
        var declared = typeof(CampaignCombatAudienceDecisions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.Name.EndsWith("Stage", StringComparison.Ordinal))
            .Select(field => (string)field.GetRawConstantValue()!).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(6, FrozenStages.Distinct(StringComparer.Ordinal).Count());
        Assert.All(FrozenStages, stage => Assert.Contains(stage, declared));
    }

    [Fact]
    public void FrozenReserveReleaseIsReachedOnlyThroughAPostAssaultRetreatDecisionOutsideV1()
    {
        // Seed 6 is the frozen `reserve-release-axis` / `cycle-control-axis` golden path
        // (combat-audience-decision-v1 fixture). Its assault result requires a defender retreat.
        // (Seed 5, the `custody-axis` golden path, requires none: a System step closes retreat there.)
        Assert.Equal(0, AtAssaultResult("campaign-admission-reach-no-retreat", 5)
            .CurrentCombatAssaultResult!.Facts.RequiredRetreat);
        var session = AtAssaultResult("campaign-admission-reach-retreat", 6);
        Assert.Equal(1, session.CurrentCombatAssaultResult!.Facts.RequiredRetreat);

        // Ordinary Core opens a defender-owned post-assault retreat decision here.
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
        Assert.Equal(LandSide.Commonwealth, cut.DefenderSide);
        var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(session,
            CampaignActionAudience.Commonwealth, cut.DefenderElementId);
        Assert.NotEmpty(draft.PrivateActionIds);

        // No v1 stage serves this cut, for either side or any subject: the v1 contract alone could
        // not disclose the decision that precedes its own Reserve Release golden. (Since Task 020E1 a
        // contract-v2 stage serves it; that is the coverage the capability manifest v2 records.)
        AssertNoFrozenDecision(session);
        Assert.Equal(CampaignCombatAudienceDecision.SuccessorContractVersion,
            CampaignCombatAudienceDecisions.Query(session, CampaignActionAudience.Commonwealth,
                CampaignCombatAudienceDecisions.PostAssaultRetreatStage, cut.DefenderElementId)!.ContractVersion);

        // The golden Reserve Release decision exists only after that owner choice is made
        // (refusal, as on the golden path), followed by the ordinary System settlement steps.
        session = Accept(CampaignExercises.SubmitContactPostResultRetreat(session,
            Command(session, CampaignActionAudience.Commonwealth, cut.RefuseOffer.ActionId), cut.DefenderElementId));
        session = Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
        if (CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Axis, "axis-infantry") is { } custody)
            session = Accepted(CampaignCombatAudienceDecisions.Submit(session, custody.Choose(custody.OfferRefs.Count - 1)));
        else session = Accept(CampaignHostAdapter.CompleteNoNativeCombatCustody(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatSuccessor(session));
        var release = CampaignCombatAudienceDecisions.QueryReserveRelease(session, CampaignActionAudience.Axis,
            "axis-infantry-second");
        Assert.NotNull(release);
        var golden = CampaignCombatAudienceDecisions.QueryReserveRelease(
            CombatCycleDisclosureReserveCycleTests.AtReserveRelease("campaign-admission-reach-retreat", 6),
            CampaignActionAudience.Axis, "axis-infantry-second")!;
        Assert.Equal(golden.SerializeCanonical(), release.SerializeCanonical());
    }

    [Fact]
    public void FrozenCycleControlOffersRepeatIntoRepeatMovementAndSelectionOutsideV1()
    {
        // Seed 6 is the frozen `reserve-release-axis` / `cycle-control-axis` golden path.
        var session = CombatCycleDisclosureReserveCycleTests.AtReserveRelease("campaign-admission-reach-repeat", 6);
        var release = CampaignCombatAudienceDecisions.QueryReserveRelease(session, CampaignActionAudience.Axis,
            "axis-infantry-second")!;
        session = Accepted(CampaignCombatAudienceDecisions.Submit(session, release.Choose(0)));
        session = CombatCycleDisclosureReserveCycleTests.Complete(session);

        // The frozen Cycle Control decision lists Repeat and the public surface accepts it.
        var control = CampaignCombatAudienceDecisions.QueryCycleControl(session, CampaignActionAudience.Axis)!;
        string[] choices;
        using (var visible = JsonDocument.Parse(control.VisibleBytes))
            choices = visible.RootElement.GetProperty("choices").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var repeat = Array.IndexOf(choices, "Repeat");
        Assert.True(repeat >= 0, "Bounded content offers Repeat at the frozen Cycle Control cut.");
        session = Accepted(CampaignCombatAudienceDecisions.Submit(session, control.Choose(repeat)));

        // Next: an owner repeated-Movement decision that no v1 stage serves.
        var moves = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis);
        Assert.NotEmpty(moves.PrivateActionIds);
        AssertNoFrozenDecision(session);

        // After the move and System Breakdown: an owner repeated Combat selection, also outside v1.
        session = Accept(CampaignHostAdapter.SubmitNativeRepeatMovement(session,
            Command(session, CampaignActionAudience.Axis, moves.PrivateActionIds[0])));
        session = Accept(CampaignHostAdapter.CompleteNativeRepeatBreakdown(session));
        var actors = Subjects.Where(subject => subject.StartsWith("axis-", StringComparison.Ordinal))
            .Where(subject => TryRepeatCombatOffers(session, subject) > 0).ToArray();
        Assert.NotEmpty(actors);
        AssertNoFrozenDecision(session);
    }

    private static int TryRepeatCombatOffers(ExerciseSession session, string actingElementId)
    {
        try
        {
            return CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(session, CampaignActionAudience.Axis,
                actingElementId).PrivateCombatActionIds.Count;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return 0;
        }
    }

    private static void AssertNoFrozenDecision(ExerciseSession session)
    {
        foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            foreach (var stage in FrozenStages)
                foreach (var subject in Subjects)
                    Assert.True(CampaignCombatAudienceDecisions.Query(session, audience, stage, subject) is null,
                        $"Unexpected frozen decision: {audience}/{stage}/{subject}.");
    }

    // Same trusted path as CombatCycleDisclosureCustodyTests.AtCustody, stopped at the result.
    private static ExerciseSession AtAssaultResult(string campaignId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == SetupId));
        var request = new CampaignCreationRequest(1, campaignId, Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        var reserveDesignated = false;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination) break;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var reserve = !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == "axis-infantry-second") : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        session = Accept(CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, Command(session,
            CampaignActionAudience.Axis, CampaignExercises.QueryContactOwnerCombatCut(session, LandSide.Axis, "axis-infantry")
                .AssaultOffers.Single(value => value.ApparentLocationId == "north-east").ActionId), "axis-infantry"));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        session = Accept(CampaignExercises.SubmitContactDefenderRbaDecline(session,
            Command(session, CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId));
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var element = audience == CampaignActionAudience.Axis ? "axis-infantry" : rba.DefenderElementId;
            var force = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element);
            session = Accept(CampaignHostAdapter.SubmitContactOwnerForceSeal(session,
                Command(session, audience, force.PrivateActionIds[0]), element));
        }
        session = Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        return Accept(CampaignHostAdapter.ResolveNativeCombatAssault(session));
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }

    private static ExerciseSession Accepted(CampaignCombatAudienceSubmissionResult result)
    {
        Assert.True(result.IsAccepted);
        return result.TrustedStep!.SuccessorSession!;
    }

    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
