using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignNativeReserveReleaseTimeoutTests
{
    [Fact]
    public void TimeoutConvertsUnresolvedIWithoutMovementAllowanceOrOwnerEvent()
    {
        var (state, context) = Fixture(1);
        var before = state.World;
        var (next, emitted) = Timeout(state, context);
        var timeout = Assert.IsType<CampaignNativeReserveReleaseTimeoutV35>(emitted);
        Assert.True(next.Completed);
        Assert.Empty(next.Pending(context));
        Assert.Empty(next.NextMovement);
        Assert.Equal(35, next.EventContractVersion);
        Assert.Equal(state.Pending(context).Select(value => value.ElementId), timeout.ConvertedElementIds);
        Assert.Empty(timeout.RetainedElementIds);
        foreach (var element in before.Elements)
        {
            var after = next.World.Elements.Single(value => value.ElementId == element.ElementId);
            Assert.Equal(element.CurrentLocationId, after.CurrentLocationId);
            Assert.Equal(element.Components, after.Components);
            Assert.Equal(element.OperationalState, after.OperationalState);
            Assert.Equal(element.ReserveStatus == CampaignElementReserveStatus.ReserveI
                ? CampaignElementReserveStatus.ReserveII : element.ReserveStatus, after.ReserveStatus);
        }
        using var json = JsonDocument.Parse(emitted.SerializeCanonical());
        Assert.False(json.RootElement.TryGetProperty("choice", out _));
        Assert.False(json.RootElement.TryGetProperty("elementId", out _));
        Assert.Equal("convert-unresolved-i-retain-ii", json.RootElement.GetProperty("fallback").GetString());
        var replay = CampaignNativeReserveReleaseStateV27.Replay(state, context, [emitted.SerializeCanonical()]);
        Assert.Equal(next.SerializeCanonical(), replay.SerializeCanonical());
        Assert.Throws<InvalidOperationException>(() => Timeout(next, context));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void LaterCycleRetainsIIAndResolvesEveryRemainingMemberOnce(int ordinal)
    {
        var (state, context) = Fixture(ordinal);
        var (next, emitted) = Timeout(state, context);
        var timeout = Assert.IsType<CampaignNativeReserveReleaseTimeoutV35>(emitted);
        Assert.Empty(timeout.ConvertedElementIds);
        Assert.Equal(state.Pending(context).Select(value => value.ElementId), timeout.RetainedElementIds);
        Assert.Equal(state.World, next.World);
        Assert.Empty(next.Pending(context));
        Assert.True(next.Completed);
        Assert.Equal(next.SerializeCanonical(), CampaignNativeReserveReleaseStateV27.Replay(
            state, context, [emitted.SerializeCanonical()]).SerializeCanonical());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void AcceptedOwnerReleaseSurvivesTimeoutAndEntryAnchorDoesNotMove(int ordinal)
    {
        var (state, context) = Fixture(ordinal);
        var member = state.Pending(context)[0];
        var (chosen, ownerEvent) = state.Choose(context, member.ElementId, ordinal == 1
            ? CampaignReserveReleaseChoice.ReleaseI : CampaignReserveReleaseChoice.ReleaseII);
        Assert.Equal(state.ReleaseEventHash, chosen.ReleaseEventHash);
        Assert.NotEqual(state.HeadEventHash, chosen.HeadEventHash);
        var (next, emitted) = Timeout(chosen, context);
        Assert.Equal(chosen.NextMovement, next.NextMovement);
        Assert.Equal(CampaignElementReserveStatus.None,
            next.World.Elements.Single(value => value.ElementId == member.ElementId).ReserveStatus);
        var timeout = Assert.IsType<CampaignNativeReserveReleaseTimeoutV35>(emitted);
        Assert.DoesNotContain(member.ElementId, timeout.ConvertedElementIds.Concat(timeout.RetainedElementIds));
        Assert.Equal(state.ReleaseEventHash, timeout.ReleaseEventHash);
        Assert.Equal(next.SerializeCanonical(), CampaignNativeReserveReleaseStateV27.Replay(state, context,
            [ownerEvent.SerializeCanonical(), emitted.SerializeCanonical()]).SerializeCanonical());
    }

    [Fact]
    public void ReplayRejectsChangedMembersAnchorWorldAndUnknownFields()
    {
        var (state, context) = Fixture(1);
        var timeout = Assert.IsType<CampaignNativeReserveReleaseTimeoutV35>(Timeout(state, context).Event);
        foreach (var forged in new[] { timeout with { ReleaseEventHash = ZeroHash },
            timeout with { ResultWorldHash = ZeroHash }, timeout with { ConvertedElementIds = ["foreign-element"] } })
            Assert.Throws<InvalidCampaignHistoryException>(() => CampaignNativeReserveReleaseStateV27.Replay(
                state, context, [forged.SerializeCanonical()]));
        var bytes = timeout.SerializeCanonical();
        Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(bytes)));
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Throws<JsonException>(() => CampaignNativeReserveReleaseTimeoutV35.Deserialize(
            Encoding.UTF8.GetBytes(text[..^1] + ",\"ownerChoice\":\"ReleaseI\"}")));
        Assert.Throws<JsonException>(() => (timeout with { CancelledAtUnixMilliseconds = 30999 }).SerializeCanonical());
        Assert.Throws<JsonException>(() => (timeout with { ConvertedElementIds = ["x", "x"] }).SerializeCanonical());
    }

    [Fact]
    public void UnavailableOpeningHasNoInventedClockAndLegacyChoiceBytesStayVersion27()
    {
        var (state, context) = Fixture(1);
        var next = state.Timeout(context, ZeroHash, -1, -1, -1, null, "reserve-release-unavailable");
        Assert.True(next.State.Completed);
        Assert.Equal(next.State.SerializeCanonical(), CampaignNativeReserveReleaseStateV27.Replay(
            state, context, [next.Event.SerializeCanonical()]).SerializeCanonical());
        Assert.Throws<JsonException>(() => state.Timeout(context, ZeroHash, 0, 30000, 0, null, "reserve-release-unavailable"));
        var owner = state.Choose(context, state.Pending(context)[0].ElementId, CampaignReserveReleaseChoice.ConvertToII).Event;
        Assert.Equal(27, owner.ContractVersion);
        Assert.Equal(owner.SerializeCanonical(), CampaignNativeReserveReleaseEventV27.Deserialize(owner.SerializeCanonical()).SerializeCanonical());
    }

    [Fact]
    public void LargeBatchCodecDispatchesBeforeLegacyByteLimit()
    {
        var (state, context) = Fixture(1);
        var timeout = Assert.IsType<CampaignNativeReserveReleaseTimeoutV35>(Timeout(state, context).Event) with
        { ConvertedElementIds = Enumerable.Range(0, 1000).Select(index => $"element-{index:D4}").ToArray() };
        var bytes = timeout.SerializeCanonical();
        Assert.True(bytes.Length > 8192);
        Assert.Equal(bytes, CampaignNativeReserveReleaseEventV27.Deserialize(bytes).SerializeCanonical());
    }

    [Fact]
    public void SessionTimeoutValidatesTimingAndReconstructsSystemCheckpoint()
    {
        var session = PendingSession();
        var timing = new CampaignDecisionTimingConfigurationV1("rules-lab.reserve-timing.1",
            session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
            [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
                new("force-assignment", 30000, "cancel-incomplete-voluntary-round"),
                new("rba", 30000, "cancel-without-decline"), new("reserve-release", 30000, "convert-unresolved-i-retain-ii"),
                new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);
        var binding = CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(session);
        var wrongContent = new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash, ZeroHash, timing.Windows);
        var wrongRules = new CampaignDecisionTimingConfigurationV1(timing.ConfigId, ZeroHash, timing.ContentHash, timing.Windows);
        foreach (var wrong in new[] { wrongContent, wrongRules })
            Assert.False(CampaignHostAdapter.TimeoutNativeReserveRelease(session, wrong, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeReserveRelease(session, timing, 1000, 31001, 1000, 31001, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeReserveRelease(session, timing, 1000, 31000, 1000, 30999, "deadline").IsAccepted);
        Assert.False(CampaignHostAdapter.TimeoutNativeReserveRelease(session, null, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.Equal(binding, CampaignHostAdapter.QueryNativeReserveReleaseRoundBinding(session));
        var accepted = CampaignHostAdapter.TimeoutNativeReserveRelease(session, timing, 1000, 31000, 1000, 31000, "deadline");
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        Assert.Equal(CampaignActionAudience.System, accepted.Evidence!.Receipt.Audience);
        Assert.Equal(CampaignActionAudience.System, CampaignHostAdapter.NativeReserveReleaseTimeoutCommand(session, accepted).Audience);
        var next = accepted.SuccessorSession!;
        Assert.Equal(35, CampaignExercises.QueryCheckpoint(next).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        Assert.Equal(session.CurrentSnapshot.RandomState, next.CurrentSnapshot.RandomState);
        Assert.False(CampaignHostAdapter.TimeoutNativeReserveRelease(next, timing, 1000, 31000, 1000, 31000, "deadline").IsAccepted);
        Assert.Equal(binding.CycleOrdinal, CampaignHostAdapter.QueryNativeCycleControlRoundBinding(next).CycleOrdinal);
    }

    private static ExerciseSession PendingSession()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(
            value => value.SetupId == "rules-lab.breakdown.reaction.combat-double-fresh.v1"));
        var request = new CampaignCreationRequest(1, "campaign-reserve-timeout-session", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        var designated = false;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination) break;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var reserve = designated ? null : set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == "axis-infantry");
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) designated = true;
            var result = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId));
            Assert.True(result.IsAccepted);
            session = result.SuccessorSession!;
        }
        Assert.True(designated);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var selected = CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, new CampaignActionSubmission(1,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, CampaignActionAudience.Axis,
            draft.PrivateCombatActionIds[^1]), "axis-infantry");
        Assert.True(selected.IsAccepted);
        var completed = CampaignHostAdapter.CompleteNativeCombatNoAttack(selected.SuccessorSession!);
        Assert.True(completed.IsAccepted);
        return completed.SuccessorSession!;
    }

    private const string ZeroHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";
    private static (CampaignNativeReserveReleaseStateV27 State, CampaignNativeReserveReleaseEventV27 Event)
        Timeout(CampaignNativeReserveReleaseStateV27 state, CampaignContentContext context) =>
        state.Timeout(context, ZeroHash, 1000, 31000, 1000, 31000, "deadline");

    private static (CampaignNativeReserveReleaseStateV27 State, CampaignContentContext Context) Fixture(int ordinal)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(
            value => value.SetupId == "rules-lab.breakdown.reaction.combat-triple.v1"));
        var request = new CampaignCreationRequest(1, "campaign-reserve-timeout", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        var ownIds = session.Context.ArtifactV6!.Definition.LegacyDefinition.Elements
            .Where(value => value.SideId == "axis" && value.BreakdownVehicleCohort is null).Select(value => value.ElementId).Order(StringComparer.Ordinal).Take(2).ToHashSet();
        Assert.Equal(2, ownIds.Count);
        var original = session.CurrentSnapshot.World;
        var world = new CampaignWorldSnapshotV6(6, original.Elements.Select(element => ownIds.Contains(element.ElementId)
            ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId,
                ordinal == 1 ? CampaignElementReserveStatus.ReserveI : CampaignElementReserveStatus.ReserveII,
                element.OperationalState, element.Components) : element.ElementId == "axis-infantry-third"
                ? new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, CampaignElementReserveStatus.ReserveII,
                    element.OperationalState, element.Components) : element), original.Representations, original.BrokenVehicleLots);
        var position = Cna1979LandSequenceV4.CreateTurn(1).First(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.ReserveRelease);
        return (new CampaignNativeReserveReleaseStateV27(request.CampaignId, session.CurrentSnapshot.StateVersion,
            "sha256:" + new string('1', 64), LandSide.Axis, ordinal, position, world, [], [], false, true), session.Context);
    }
}
