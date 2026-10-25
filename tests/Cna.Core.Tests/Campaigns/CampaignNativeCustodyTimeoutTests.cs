using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignNativeCustodyTimeoutTests
{
    [Fact]
    public void DeadlineLeavesCaptivesUnguardedWithSystemEvidenceAndColdReplay()
    {
        var session = AtCustody();
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)!;
        var accepted = Timeout(session);
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        var next = accepted.SuccessorSession!;
        var evidence = Assert.IsType<CampaignNativeCombatCustodyTimeoutV36>(next.CurrentCombatCustodyDisposition);
        Assert.Equal(CampaignActionAudience.System, accepted.Evidence!.Receipt.Audience);
        Assert.Equal(CampaignActionAudience.System, CampaignHostAdapter.NativeCombatCustodyTimeoutCommand(session, accepted).Audience);
        Assert.Equal(36, CampaignExercises.QueryCheckpoint(next).ContractVersion);
        Assert.Equal(session.CurrentCombatSettledWorld, next.CurrentCombatCustodyWorld);
        Assert.Equal(session.CurrentSnapshot.RandomState, next.CurrentSnapshot.RandomState);
        var custody = next.CurrentCombatCustodyState!;
        Assert.Equal("escaped", custody.Status);
        Assert.Null(custody.Guard);
        Assert.Null(custody.CurrentLocationId);
        Assert.Equal(cut.Quantity, custody.Replacement!.Quantity);
        Assert.True(custody.Replacement.TrainingPending);
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var successor = CampaignHostAdapter.CompleteNativeCombatSuccessor(next);
        Assert.True(successor.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(successor.SuccessorSession!).IsVerified);
        Assert.False(Timeout(next).IsAccepted);
        using var json = JsonDocument.Parse(evidence.SerializeCanonical());
        Assert.False(json.RootElement.TryGetProperty("choiceKind", out _));
        Assert.Equal("leave-unguarded", json.RootElement.GetProperty("fallback").GetString());
    }

    [Fact]
    public void InvalidTimingDoesNotMutatePendingCustody()
    {
        var session = AtCustody();
        var timing = Timing(session);
        var binding = CampaignHostAdapter.QueryNativeCombatCustodyRoundBinding(session);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatCustody(session, timing, 1000, 31000, 1000, 30999, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatCustody(session, timing, 1000, 31001, 1000, 31001, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCombatCustody(session, null, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        foreach (var foreign in new[] {
            new CampaignDecisionTimingConfigurationV1(timing.ConfigId, ZeroHash, timing.ContentHash, timing.Windows),
            new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash, ZeroHash, timing.Windows) })
            Assert.False(CampaignHostAdapter.TimeoutNativeCombatCustody(session, foreign, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.Equal(binding, CampaignHostAdapter.QueryNativeCombatCustodyRoundBinding(session));
        Assert.Null(session.CurrentCombatCustodyDisposition);
    }

    [Theory]
    [InlineData("clock-unavailable", null)]
    [InlineData("clock-regression", 999L)]
    [InlineData("controller-unavailable", null)]
    public void ClockFailuresKeepExplicitSystemReason(string reason, long? observed)
    {
        var session = AtCustody();
        var accepted = CampaignHostAdapter.TimeoutNativeCombatCustody(session, Timing(session), 1000, 31000, 1000, observed, reason);
        Assert.True(accepted.IsAccepted);
        Assert.Equal(reason, Assert.IsType<CampaignNativeCombatCustodyTimeoutV36>(accepted.SuccessorSession!.CurrentCombatCustodyDisposition).Reason);
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void UnavailableEntryHasNoInventedClock()
    {
        var session = AtCustody();
        var accepted = CampaignHostAdapter.TimeoutNativeCombatCustody(session, null, -1, -1, -1, null, "custody-unavailable");
        Assert.True(accepted.IsAccepted);
        var evidence = Assert.IsType<CampaignNativeCombatCustodyTimeoutV36>(accepted.SuccessorSession!.CurrentCombatCustodyDisposition);
        Assert.Equal(ZeroHash, evidence.ConfigurationHash);
        Assert.Equal(-1, evidence.OpenedAtUnixMilliseconds);
        Assert.True(CampaignExercises.Reconstruct(accepted.SuccessorSession!).IsVerified);
    }

    [Fact]
    public void TamperedLotRouteAndTimingEvidenceRejectReplay()
    {
        var session = AtCustody();
        var evidence = Assert.IsType<CampaignNativeCombatCustodyTimeoutV36>(Timeout(session).SuccessorSession!.CurrentCombatCustodyDisposition);
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session);
        foreach (var forged in new[] { evidence with { SettlementHash = ZeroHash }, evidence with { WorldHash = ZeroHash },
            evidence with { CustodyStateHash = ZeroHash }, evidence with { LotId = "foreign-lot" }, evidence with { Route = ["foreign-location"] } })
            Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeCombatCustodyDispositionV25.Replay(
                session.CurrentSnapshot, session.CurrentCombatSettlement!, session.CurrentCombatSettledWorld!, cut, session.Context, forged.SerializeCanonical()));
        var bytes = evidence.SerializeCanonical();
        Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(bytes)));
        Assert.Throws<JsonException>(() => (evidence with { CancelledAtUnixMilliseconds = 30999 }).SerializeCanonical());
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Throws<JsonException>(() => CampaignNativeCombatCustodyTimeoutV36.Deserialize(Encoding.UTF8.GetBytes(text[..^1] + ",\"owner\":true}")));
    }

    [Fact]
    public void AcceptedOwnerDispositionCannotBeOverriddenAndUnsupportedFallbackRejects()
    {
        var session = AtCustody();
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)!;
        var choice = cut.Choices[0];
        var side = session.Context.ArtifactV6!.Definition.LegacyDefinition.Elements.Single(value => value.ElementId == cut.CaptorElementId).SideId;
        var owner = CampaignExercises.SubmitNativeCombatCustody(session, new CampaignActionSubmission(1, cut.CampaignId,
            cut.AuthorityVersion, session.CurrentCombatSettlement!.PositionId,
            side == "axis" ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth, choice.ActionId));
        Assert.True(owner.IsAccepted);
        Assert.Equal(25, owner.SuccessorSession!.CurrentCombatCustodyDisposition!.ContractVersion);
        Assert.False(Timeout(owner.SuccessorSession).IsAccepted);
        var guardOnly = cut with { Choices = cut.Choices.Where(value => value.Kind != "leave-unguarded").ToArray() };
        Assert.Throws<InvalidOperationException>(() => CampaignNativeCombatCustodyTimeoutV36.CreateTimeout(session.CurrentSnapshot,
            session.CurrentCombatSettlement, session.CurrentCombatSettledWorld!, guardOnly, session.Context, Timing(session).Hash,
            1000, 31000, 1000, 31000, "deadline"));
        Assert.Null(session.CurrentCombatCustodyDisposition);
    }

    [Fact]
    public void NativeSingleLotEscapeUsesAtMostOneClearEdgeWithinSourceBudget()
    {
        var session = AtCustody();
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)!;
        var escape = cut.Choices.Single(value => value.Kind == "leave-unguarded");
        Assert.InRange(escape.Route.Count, 1, 2);
        var cost = Cna1979Movement.LookupTerrain("land.terrain.clear", Cna1979Movement.NonMotorizedMobilityId);
        Assert.True(cost.IsSupported);
        Assert.Equal(1, cost.Value.Cost.Denominator);
        Assert.InRange(cost.Value.Cost.Numerator, 0, 8);
        Assert.Equal(session.CurrentCombatSettlement!.PendingCaptives[0].OriginLocationId, escape.Route[0]);
        Assert.Equal(session.CurrentCombatSettledWorld!.Elements.Single(value => value.ElementId == cut.VictimElementId).CurrentLocationId, escape.Route[^1]);
    }

    private const string ZeroHash = CampaignNativeCombatCustodyTimeoutV36.NoTimingConfigurationHash;
    private static ExerciseStepResult Timeout(ExerciseSession session) =>
        CampaignHostAdapter.TimeoutNativeCombatCustody(session, Timing(session), 1000, 31000, 1000, 31000, "deadline");
    private static CampaignDecisionTimingConfigurationV1 Timing(ExerciseSession session) => new(
        "rules-lab.custody-timing.1", session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
        [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
            new("force-assignment", 30000, "cancel-incomplete-voluntary-round"), new("rba", 30000, "cancel-without-decline"),
            new("reserve-release", 30000, "convert-unresolved-i-retain-ii"), new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);

    private static ExerciseSession AtCustody()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var request = new CampaignCreationRequest(1, "campaign-custody-timeout", Cna1979Ruleset.Manifest.Hash,
            5, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
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
        if (session.CurrentCombatAssaultResult!.Facts.RequiredRetreat == 1)
        {
            var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
            session = Accept(CampaignExercises.SubmitContactPostResultRetreat(session,
                Command(session, CampaignActionAudience.Commonwealth, cut.MoveOffers[0].ActionId), cut.DefenderElementId));
        }
        else session = Accept(CampaignHostAdapter.CompleteNoPostAssaultRetreat(session));
        session = Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
        Assert.NotNull(CampaignExercises.QueryNativeCombatCustodyCut(session));
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
