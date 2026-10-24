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

public sealed class CampaignNativeCycleControlTimeoutTests
{
    [Fact]
    public void DeadlineFinishesAsSystemAndReconstructsWithoutOwnerChoice()
    {
        var session = AtRelease();
        var binding = CampaignHostAdapter.QueryNativeCycleControlRoundBinding(session);
        var world = CampaignExercises.QueryNativeReserveReleaseState(session).World;
        var result = Timeout(session);
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        var next = result.SuccessorSession!;
        var timeout = Assert.IsType<CampaignNativeCycleControlTimeoutV34>(next.CurrentCycleControl);
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(next).ContractVersion);
        Assert.Equal(binding.ReleaseEventHash, timeout.PriorEventHash);
        Assert.Equal(CampaignActionAudience.System, result.Evidence!.Receipt.Audience);
        Assert.Equal(CampaignActionAudience.System, CampaignHostAdapter.NativeCycleControlTimeoutCommand(session, result).Audience);
        Assert.Equal(world, CampaignExercises.QueryNativeReserveReleaseState(next).World);
        Assert.Equal(session.CurrentSnapshot.RandomState, next.CurrentSnapshot.RandomState);
        Assert.Contains("truck-convoy", timeout.NextPositionId, StringComparison.Ordinal);
        Assert.Null(timeout.MovementWitnessActionId);
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        Assert.False(Timeout(next).IsAccepted);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryNativeCycleControlRoundBinding(next));
        Assert.False(CampaignExercises.SubmitNativeCycleControl(next, CampaignActionAudience.Axis,
            CampaignNativeCycleControlChoiceV28.Finish, timeout.PriorEventHash).IsAccepted);
    }

    [Fact]
    public void ClockAndBudgetRejectionsLeavePendingDecisionUnchanged()
    {
        var session = AtRelease();
        var timing = Timing(session);
        var binding = CampaignHostAdapter.QueryNativeCycleControlRoundBinding(session);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, timing, 1000, 31000, 2000, 30999, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, timing, 1000, 31001, 2000, 31001, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, timing, 1000, 31000, 2000, 2000, "clock-regression").IsAccepted);
        Assert.True(CampaignHostAdapter.TimeoutNativeCycleControl(session, timing, 1000, 31000, 2000, 1999, "clock-regression").IsAccepted);
        Assert.True(CampaignHostAdapter.TimeoutNativeCycleControl(session, timing, 1000, 31000, 2000, null, "clock-unavailable").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, null, 1000, 31000, 2000, 31000, "deadline").IsAccepted);
        var foreignContent = new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash,
            CampaignHostAdapter.NoTimingCycleControlTimeoutHash, timing.Windows);
        var foreignRules = new CampaignDecisionTimingConfigurationV1(timing.ConfigId,
            CampaignHostAdapter.NoTimingCycleControlTimeoutHash, timing.ContentHash, timing.Windows);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, foreignContent, 1000, 31000, 2000, 31000, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, foreignRules, 1000, 31000, 2000, 31000, "deadline").IsAccepted);
        Assert.Equal(binding, CampaignHostAdapter.QueryNativeCycleControlRoundBinding(session));
        Assert.Null(session.CurrentCycleControl);
    }

    [Fact]
    public void UnavailableOpeningHasNoInventedClock()
    {
        var session = AtRelease();
        var result = CampaignHostAdapter.TimeoutNativeCycleControl(session, null, -1, -1, -1, null, "cycle-control-unavailable");
        Assert.True(result.IsAccepted);
        var timeout = Assert.IsType<CampaignNativeCycleControlTimeoutV34>(result.SuccessorSession!.CurrentCycleControl);
        Assert.Equal(CampaignHostAdapter.NoTimingCycleControlTimeoutHash, timeout.ConfigurationHash);
        Assert.True(CampaignExercises.Reconstruct(result.SuccessorSession).IsVerified);
        Assert.False(CampaignHostAdapter.TimeoutNativeCycleControl(session, null, 0, 30000, 0, null, "cycle-control-unavailable").IsAccepted);
    }

    [Fact]
    public void StrictReplayRejectsChangedReleaseWorldOrdinalAndUnknownFields()
    {
        var session = AtRelease();
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        var timeout = Assert.IsType<CampaignNativeCycleControlTimeoutV34>(Timeout(session).SuccessorSession!.CurrentCycleControl);
        foreach (var forged in new[] { timeout with { PriorEventHash = CampaignHostAdapter.NoTimingCycleControlTimeoutHash },
            timeout with { ResultWorldHash = CampaignHostAdapter.NoTimingCycleControlTimeoutHash },
            timeout with { PriorCycleOrdinal = 99 } })
            Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeCycleControlV28.Replay(
                release, session.Context, session.CurrentContacts, forged.SerializeCanonical()));
        var text = Encoding.UTF8.GetString(timeout.SerializeCanonical());
        Assert.Throws<JsonException>(() => CampaignNativeCycleControlTimeoutV34.Deserialize(
            Encoding.UTF8.GetBytes(text[..^1] + ",\"extra\":true}")));
        Assert.Equal(timeout, CampaignCurrentEventSerializer.Deserialize(timeout.SerializeCanonical()));
    }

    [Fact]
    public void OwnerFinishRemainsVersion28AndTimeoutBytesStayDistinct()
    {
        var session = AtRelease();
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        var owner = CampaignNativeCycleControlV28.Create(release, session.Context, session.CurrentContacts,
            CampaignNativeCycleControlChoiceV28.Finish);
        var bytes = owner.SerializeCanonical();
        Assert.Equal(owner, CampaignNativeCycleControlV28.Deserialize(bytes));
        using var ownerJson = JsonDocument.Parse(bytes);
        Assert.Equal(28, ownerJson.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal("Finish", ownerJson.RootElement.GetProperty("choice").GetString());
        using var timeoutJson = JsonDocument.Parse(Timeout(session).Evidence!.EventRecords[0]);
        Assert.False(timeoutJson.RootElement.TryGetProperty("choice", out _));
        Assert.Equal("finish-phase", timeoutJson.RootElement.GetProperty("fallback").GetString());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void RepeatedRuntimeRetainsExplicitTimeoutAtActualOrdinal(int ordinal)
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
            frame.World, frame.RandomState, session.Context, contacts, session.CurrentHistory, [barrage, cancellation]);
        var complete = runtime.Release!.Complete(session.Context);
        runtime = CampaignNativeRepeatAssaultRuntime.Replay(session.CurrentSnapshot, selection,
            frame.World, frame.RandomState, session.Context, contacts, session.CurrentHistory, [barrage, cancellation, complete.Event]);
        var timeout = CampaignNativeCycleControlTimeoutV34.Create(runtime.Release!, session.Context,
            runtime.Contacts, Timing(session).Hash, 31000, 61000, 31000, 61000, "deadline");
        var terminal = CampaignNativeRepeatAssaultRuntime.Replay(session.CurrentSnapshot, selection,
            frame.World, frame.RandomState, session.Context, contacts, session.CurrentHistory,
            [barrage, cancellation, complete.Event, timeout]);
        Assert.Equal(timeout, Assert.IsType<CampaignNativeCycleControlTimeoutV34>(terminal.Control));
        Assert.Equal(ordinal, terminal.Control!.PriorCycleOrdinal);
        Assert.Equal(frame.World, terminal.Release!.World);
        Assert.Equal(frame.RandomState, terminal.Selection.RandomState);
    }

    private static ExerciseStepResult Timeout(ExerciseSession session) =>
        CampaignHostAdapter.TimeoutNativeCycleControl(session, Timing(session), 1000, 31000, 1000, 31000, "deadline");

    private static ExerciseSession AtRelease()
    {
        var session = AtRba();
        var cancellation = CampaignExercises.CancelNativeCombatRba(session, Timing(session), 1000, 31000, 1000, 31000, "deadline");
        Assert.True(cancellation.IsAccepted);
        var complete = CampaignExercises.CompleteNativeReserveRelease(cancellation.SuccessorSession!);
        Assert.True(complete.IsAccepted);
        return complete.SuccessorSession!;
    }

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
