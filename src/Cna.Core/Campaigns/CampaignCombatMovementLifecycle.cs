using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Actual stop, empty System resolution, and Movement completion after retained ordinary moves.</summary>
internal static class CampaignCombatMovementLifecycle
{
    public static CampaignCombatMovementLifecycleState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(moves); ArgumentNullException.ThrowIfNull(events);
        if (moves.Count == 0 || events.Count > 3) throw new JsonException("Lifecycle requires actual moves and at most three lifecycle events.");
        var movement = CampaignCombatInheritedMovement.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves);
        if (movement.BreakdownFlow is null || movement.BreakdownFlow.Route.CohortIds.Count != 0)
            throw new JsonException("Lifecycle requires the actual nonvehicle moving route.");
        var state = new CampaignCombatMovementLifecycleState(movement, movement.StateVersion, movement.Prefix,
            movement.SequencePosition, movement.BreakdownFlow, null, null, movement.Receipts, []);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized lifecycle event.");
            var bytes = retained.ToArray();
            var (after, emitted) = Emit(state, CampaignCombatMovementLifecycleCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatMovementLifecycleCodec.SerializeEvent(emitted)))
                throw new JsonException("Lifecycle event differs from actual route and history-derived transition.");
            state = after;
        }
        return state;
    }
    public static CampaignCombatMovementLifecycleResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> events, CampaignCombatMovementLifecycleInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.Identity.ExpectedPriorVersion != input.Command.Identity.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting lifecycle retry at consumed occurrence.");
            return new(state, CampaignCombatMovementLifecycleCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new(after, CampaignCombatMovementLifecycleCodec.SerializeEvent(emitted), false);
    }
    public static CampaignCombatMovementLifecycleState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized lifecycle projection.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, events);
        if (!bytes.SequenceEqual(CampaignCombatMovementLifecycleCodec.SerializeState(state)))
            throw new JsonException("Lifecycle projection differs from actual retained history.");
        return state;
    }
    public static CampaignCombatMovementLifecycleInput Command(CampaignCombatMovementLifecycleState state)
    {
        var index = state.Events.Count;
        if (state.MovementEnd is not null || index > 2 || index == 0 && state.BreakdownFlow is not CampaignBreakdownFlow.Moving ||
            index == 1 && state.BreakdownFlow is not CampaignBreakdownFlow.PhasingStop || index == 2 && state.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new JsonException("No supported lifecycle command at this cut.");
        var capability = index < 2 ? CampaignCombatMovementLifecycleCodec.Capability(state, index) : null;
        var creation = state.Movement.Opening.Predecessor.Stage.Weather.Opening.Creation;
        var identity = new CampaignCombatMovementLifecycleIdentity(2, CampaignCombatMovementLifecycleCodec.Action(index, capability),
            creation.CreationReceipt.CreationBinding, creation.CreationReceipt.CreationEventHash, state.Movement.Opening.CycleId!,
            state.StateVersion, state.SequencePosition.PositionId);
        CampaignCombatMovementLifecycleCommand command = index switch
        {
            0 => new CampaignCombatMovementLifecycleCommand.Stop(identity, capability!),
            1 => new CampaignCombatMovementLifecycleCommand.Resolve(identity, capability!),
            _ => new CampaignCombatMovementLifecycleCommand.Complete(identity),
        };
        return new(command, index == 1 ? CampaignOpeningPreambleActor.System : Owner(state));
    }
    private static void Authorize(CampaignCombatMovementLifecycleState state, CampaignCombatMovementLifecycleInput input)
    {
        _ = CampaignCombatMovementLifecycleCodec.SerializeInput(input);
        var expectedActor = input.Command is CampaignCombatMovementLifecycleCommand.Resolve ? CampaignOpeningPreambleActor.System : Owner(state);
        if (input.Actor != expectedActor) throw new JsonException("Lifecycle command has wrong trusted actor.");
        var receipt = state.Movement.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.Identity.CreationBinding != receipt.CreationBinding || input.Command.Identity.CreationEventHash != receipt.CreationEventHash ||
            input.Command.Identity.CycleId != state.Movement.Opening.CycleId)
            throw new JsonException("Lifecycle provenance differs from actual creation and cycle.");
    }
    private static CampaignOpeningPreambleActor Owner(CampaignCombatMovementLifecycleState state) =>
        state.Movement.Opening.Cycle!.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
    private static (CampaignCombatMovementLifecycleState State, CampaignCombatMovementLifecycleEvent Event) Emit(
        CampaignCombatMovementLifecycleState state, CampaignCombatMovementLifecycleInput input)
    {
        Authorize(state, input);
        if (input != Command(state)) throw new JsonException("Lifecycle capability or occurrence does not match current route.");
        var cycle = state.Movement.Opening.Cycle!;
        var index = state.Events.Count;
        CampaignBreakdownFlow flow;
        LandSequencePosition position;
        CampaignCombatMovementInterrupt? interrupt = null;
        CampaignCombatEndLocation[] locations = [];
        CampaignCombatUnitKey[] excluded = [];
        if (index == 0)
        {
            if (state.SequencePosition != state.Movement.SequencePosition || state.InterruptContext is not null)
                throw new JsonException("Stop must capture actual Movement scope.");
            var route = ((CampaignBreakdownFlow.Moving)state.BreakdownFlow).Route;
            var stop = CampaignBreakdownStop.Create(cycle.CampaignId, cycle.RulesetHash, checked(state.StateVersion + 1),
                route, CampaignBreakdownStopReason.Deliberate, BreakdownWeatherKind.Normal, []);
            flow = new CampaignBreakdownFlow.PhasingStop(stop);
            position = new(5, Cna1979LandSequenceV4.BreakdownStopPositionId, 1, 1, LandStageIds.Operation,
                LandPhaseIds.MovementAndCombat, LandSegmentIds.BreakdownDetermination, null, LandActorRole.None, null,
                [Cna1979LandSequence.SourceReference, new("spi-1979-land-rules", "21.24-21.26")]);
            interrupt = new(cycle, state.Movement.Opening.CycleId!, state.SequencePosition);
        }
        else if (index == 1)
        {
            if (state.SequencePosition.PositionId != Cna1979LandSequenceV4.BreakdownStopPositionId || state.InterruptContext is not { } captured ||
                captured.Cycle != cycle || captured.CycleId != state.Movement.Opening.CycleId || captured.SequencePosition != state.Movement.SequencePosition)
                throw new JsonException("Resolution requires exact suspended cycle and Movement scope.");
            if (((CampaignBreakdownFlow.PhasingStop)state.BreakdownFlow).Stop.CohortInputs.Count != 0)
                throw new JsonException("Positive vehicle resolution is unsupported.");
            flow = new CampaignBreakdownFlow.Idle(); position = captured.SequencePosition;
        }
        else
        {
            if (state.SequencePosition != state.Movement.SequencePosition || state.InterruptContext is not null)
                throw new JsonException("Movement completion requires resolved actual route.");
            flow = new CampaignBreakdownFlow.Idle();
            var target = Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.breakdown-determination");
            position = new(5, target.PositionId, target.GameTurn, target.OperationStage, target.StageId, target.PhaseId,
                target.SegmentId, target.StepId, target.ActorRole, target.ActiveSide, target.Sources);
            var world = state.Movement.World;
            var pack = state.Movement.Opening.Predecessor.Stage.Weather.Opening.Creation.Setup.Artifact.Definition;
            locations = world.Elements.Select(e => new CampaignCombatEndLocation(new(world.CreationBinding,
                    pack.Elements.Single(f => f.ElementId == e.ElementId).SideId, e.ElementId), e.CurrentLocationId))
                .OrderBy(x => x.Unit.CreationBinding, StringComparer.Ordinal).ThenBy(x => x.Unit.OriginalSide, StringComparer.Ordinal)
                .ThenBy(x => x.Unit.ElementId, StringComparer.Ordinal).ToArray();
            var owner = CampaignSnapshotSerializer.FormatSide(cycle.ActingSide);
            var enemies = locations.Where(x => x.Unit.OriginalSide != owner).Select(x => x.LocationId).ToHashSet(StringComparer.Ordinal);
            // Bounded port of cycle-control's exclusion predicate; this first proof has no prior exclusions.
            excluded = locations.Where(x => x.Unit.OriginalSide == owner && !WithinTwo(pack.Edges, x.LocationId, enemies)).Select(x => x.Unit).ToArray();
        }
        var emitted = new CampaignCombatMovementLifecycleEvent(state, input, position, flow, interrupt, locations, excluded);
        var bytes = CampaignCombatMovementLifecycleCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatMovementLifecycleCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        var proof = index == 2 ? new CampaignCombatMovementEndProof(new(1, 1, "first-acting-side", cycle.ActingSide), emitted.ReceiptId, locations, excluded) : null;
        return (new(state.Movement, emitted.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), position, flow,
            interrupt, proof, state.Receipts.Append(receipt), state.Events.Append(emitted)), emitted);
    }
    private static bool WithinTwo(IReadOnlyList<ContentHexEdge> edges, string origin, HashSet<string> enemies)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { origin };
        var frontier = new[] { origin };
        for (var distance = 0; distance <= 2; distance++)
        {
            if (frontier.Any(enemies.Contains)) return true;
            if (distance == 2) break;
            frontier = frontier.SelectMany(location => edges.Where(e => e.FirstLocationId == location || e.SecondLocationId == location)
                .Select(e => e.FirstLocationId == location ? e.SecondLocationId : e.FirstLocationId)).Where(visited.Add).ToArray();
        }
        return false;
    }
}
