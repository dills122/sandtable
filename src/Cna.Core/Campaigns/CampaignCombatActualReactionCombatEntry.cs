using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Content;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualReactionCombatEntryCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualReactionCombatEntryResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? command, accepted;
    private readonly byte[][] prefixInputs, lifecycleInputs, breakdownInputs;
    internal CombatActualReactionCombatEntryResult(Packet packet, CampaignCombatBreakdownCompletionState state,
        CampaignCombatCandidate? candidate, byte[] closureControl, byte[][] prefix, byte[][] lifecycle, byte[][] breakdown, byte[]? ev = null, bool duplicate = false)
    {
        State = state; Candidate = candidate; source = Write(packet);
        control = SerializeControl(closureControl, state);
        command = state.Completion is not null ? null : state.Lifecycle.Events.Count < 3 ?
            CampaignCombatMovementLifecycleCodec.SerializeInput(CampaignCombatMovementLifecycle.Command(state.Lifecycle)) :
            CampaignCombatBreakdownCompletionCodec.SerializeInput(CampaignCombatBreakdownCompletion.Command(state));
        accepted = ev?.ToArray(); Duplicate = duplicate;
        prefixInputs = prefix.Select(i => i.ToArray()).ToArray();
        lifecycleInputs = lifecycle.Select(i => i.ToArray()).ToArray();
        breakdownInputs = breakdown.Select(i => i.ToArray()).ToArray();
        var final = state.Completion is null ? null : CampaignCombatBreakdownCompletionCodec.SerializeEvent(state.Completion);
        proof = Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceHash"] = Hash(source),
            ["entry"] = JsonNode.Parse(control),
            ["candidate"] = candidate is null ? null : JsonNode.Parse(CampaignCombatIdentityCodec.SerializeCandidate(candidate)),
            ["finalBreakdownEventCanonicalUtf8"] = final is null ? null : Encoding.ASCII.GetString(final),
            ["finalBreakdownEventHash"] = final is null ? null : Hash(final),
            ["finalBreakdownReceiptId"] = state.BreakdownCompletionReceiptId
        });
        _ = Object(control); _ = Object(proof);
    }
    internal CampaignCombatBreakdownCompletionState State { get; }
    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? CommandBytes => command?.ToArray();
    public byte[]? EventBytes => accepted?.ToArray();
    public byte[]? FinalBreakdownEventBytes => State.Completion is null ? null : CampaignCombatBreakdownCompletionCodec.SerializeEvent(State.Completion);
    public IReadOnlyList<byte[]> PrefixInputs => prefixInputs.Select(i => i.ToArray()).ToArray();
    public IReadOnlyList<byte[]> LifecycleInputs => lifecycleInputs.Select(i => i.ToArray()).ToArray();
    public IReadOnlyList<byte[]> BreakdownInputs => breakdownInputs.Select(i => i.ToArray()).ToArray();
    public bool Duplicate { get; }
    public string? ReceiptId { get { if (accepted is null) return null; using var doc = JsonDocument.Parse(accepted); return doc.RootElement.GetProperty("receiptId").GetString(); } }
    public JsonElement Control { get { using var doc = JsonDocument.Parse(control); return doc.RootElement.Clone(); } }
    public JsonElement Proof { get { using var doc = JsonDocument.Parse(proof); return doc.RootElement.Clone(); } }
    public CampaignCombatCandidate? Candidate { get; }
    public CampaignWorldSnapshotV7 World => State.Lifecycle.Movement.World;
}

/// <summary>Dormant actual Reaction-resolved entry. Caller ledgers are independent authenticators;
/// dependency acquisition belongs outside authoritative turns. No caller projection is admitted.</summary>
internal static class CampaignCombatActualReactionCombatEntry
{
    public static CombatActualReactionCombatEntryResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefixInputs, IReadOnlyList<byte[]> lifecycleInputs,
        IReadOnlyList<byte[]> breakdownInputs, Func<string, byte[]> dependencies)
    {
        var (packet, state, prefix, lifecycle, breakdown, closureControl) = Replay(source, request, prefixInputs, lifecycleInputs, breakdownInputs, dependencies);
        return Result(packet, state, request, closureControl, prefix, lifecycle, breakdown);
    }
    public static CombatActualReactionCombatEntryResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefixInputs, IReadOnlyList<byte[]> lifecycleInputs,
        IReadOnlyList<byte[]> breakdownInputs, ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies)
    {
        var current = Copy(currentInput);
        _ = Object(current);
        var (packet, state, prefix, lifecycle, breakdown, closureControl) = Replay(source, request, prefixInputs, lifecycleInputs, breakdownInputs, dependencies);
        // Validate canonical input and compare occurrence before resolving a retained retry.
        var node = Object(current);
        if (node["command"] is not JsonObject cmd || cmd["kind"] is not JsonValue value || !value.TryGetValue<string>(out var kind)) throw new JsonException("CMB-RCE-001");
        var canonical = kind == "complete-breakdown-segment" ?
            CampaignCombatBreakdownCompletionCodec.SerializeInput(CampaignCombatBreakdownCompletionCodec.DeserializeInput(current)) :
            CampaignCombatMovementLifecycleCodec.SerializeInput(CampaignCombatMovementLifecycleCodec.DeserializeInput(current));
        if (!current.AsSpan().SequenceEqual(canonical)) throw new JsonException("CMB-RCE-008");
        var occurrence = JsonNode.Parse(current)!["command"]!["expectedPriorVersion"]!.GetValue<long>();
        for (var i = 0; i < lifecycle.Length + breakdown.Length; i++)
        {
            var old = i < lifecycle.Length ? lifecycle[i] : breakdown[i - lifecycle.Length];
            if (JsonNode.Parse(old)!["command"]!["expectedPriorVersion"]!.GetValue<long>() != occurrence) continue;
            if (!old.AsSpan().SequenceEqual(current)) throw new JsonException("CMB-RCE-004");
            var ev = i < packet.Lifecycle.Length ? packet.Lifecycle[i] : packet.Breakdown[i - packet.Lifecycle.Length];
            return Result(packet, state, request, closureControl, prefix, lifecycle, breakdown, ev, true);
        }
        if (state.Completion is not null) throw new JsonException("CMB-RCE-006");
        byte[] accepted;
        if (state.Lifecycle.Events.Count < 3)
        {
            var (next, ev) = Emit(state.Lifecycle, CampaignCombatMovementLifecycleCodec.DeserializeInput(current));
            accepted = CampaignCombatMovementLifecycleCodec.SerializeEvent(ev);
            packet = packet with { Lifecycle = [.. packet.Lifecycle, accepted] };
            lifecycle = [.. lifecycle, current]; state = new(next, null);
        }
        else
        {
            var ev = EmitBreakdown(state, CampaignCombatBreakdownCompletionCodec.DeserializeInput(current));
            accepted = CampaignCombatBreakdownCompletionCodec.SerializeEvent(ev);
            packet = packet with { Breakdown = [accepted] }; breakdown = [current]; state = new(state.Lifecycle, ev);
        }
        return Result(packet, state, request, closureControl, prefix, lifecycle, breakdown, accepted);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix,
        IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, dependencies).SourceBytes;
    public static CombatActualReactionCombatEntryResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
    {
        var owned = Copy(claim); _ = Object(owned);
        var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, dependencies);
        if (!owned.AsSpan().SequenceEqual(result.ControlBytes)) throw new JsonException("CMB-RCE-006"); return result;
    }
    public static CombatActualReactionCombatEntryResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
    {
        var owned = Copy(claim); _ = Object(owned);
        var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, dependencies);
        if (!owned.AsSpan().SequenceEqual(result.ProofBytes)) throw new JsonException("CMB-RCE-006"); return result;
    }
    private static (Packet Packet, CampaignCombatBreakdownCompletionState State, byte[][] Prefix, byte[][] Lifecycle, byte[][] Breakdown, byte[] ClosureControl)
        Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefixInputs,
        IReadOnlyList<byte[]> lifecycleInputs, IReadOnlyList<byte[]> breakdownInputs, Func<string, byte[]> dependencies)
    {
        ArgumentNullException.ThrowIfNull(request);
        var packet = Parse(source);
        var prefix = Inputs(prefixInputs, packet.Prefix.Length);
        var lifecycle = Inputs(lifecycleInputs, packet.Lifecycle.Length);
        var breakdown = Inputs(breakdownInputs, packet.Breakdown.Length);
        Dependencies(dependencies);
        if (request.RandomState.Seed != 1) throw new JsonException("CMB-RCE-003");
        for (var i = 0; i < prefix.Length; i++) CompareInput(packet.Prefix[i], prefix[i]);
        var history = CampaignCombatHistoryReplay.Replay(request, packet.Created, packet.Prefix);
        if (history.Projection is not CampaignCombatHistoryProjection.ReactionClosure projection ||
            projection.State.Accepted is null || projection.State.StateVersion != 14 ||
            projection.State.BreakdownFlow is not CampaignBreakdownFlow.Moving moving || projection.State.ReactionWindow is not null)
            throw new JsonException("CMB-RCE-004");
        var closure = projection.State;
        using var facts = JsonDocument.Parse(CampaignCombatReactionClosureCodec.SerializeState(closure));
        var random = facts.RootElement.GetProperty("randomState");
        var weather = facts.RootElement.GetProperty("operationStageWeather");
        if (random.GetProperty("seed").GetUInt64() != 1 || random.GetProperty("nextByteCursor").GetUInt64() != 2 ||
            weather.GetArrayLength() != 1 || weather[0].GetProperty("kind").GetString() != "normal") throw new JsonException("CMB-RCE-003");
        // Typed reconstruction only from independently authenticated, strictly replayed closure.
        _ = Certify(request, packet.Created, closure.World, closure.Opening.Cycle!);
        var movement = new CampaignCombatInheritedMovementState(closure.Opening, closure.StateVersion, closure.Prefix,
            closure.World, closure.Predecessor.Trigger.Members, closure.Receipts, closure.Tracks, closure.ActualProgressRefs, moving, []);
        var initial = new CampaignCombatMovementLifecycleState(movement, closure.StateVersion, closure.Prefix,
            movement.SequencePosition, moving, null, null, closure.Receipts, []);
        var state = new CampaignCombatBreakdownCompletionState(initial, null);
        for (var i = 0; i < packet.Lifecycle.Length; i++)
        {
            CompareInput(packet.Lifecycle[i], lifecycle[i]);
            var (next, ev) = Emit(state.Lifecycle, CampaignCombatMovementLifecycleCodec.DeserializeInput(lifecycle[i]));
            if (!packet.Lifecycle[i].AsSpan().SequenceEqual(CampaignCombatMovementLifecycleCodec.SerializeEvent(ev))) throw new JsonException("CMB-RCE-006");
            state = new(next, null);
        }
        if (packet.Breakdown.Length > 0)
        {
            CompareInput(packet.Breakdown[0], breakdown[0]);
            var ev = EmitBreakdown(state, CampaignCombatBreakdownCompletionCodec.DeserializeInput(breakdown[0]));
            if (!packet.Breakdown[0].AsSpan().SequenceEqual(CampaignCombatBreakdownCompletionCodec.SerializeEvent(ev))) throw new JsonException("CMB-RCE-006");
            state = new(state.Lifecycle, ev);
        }
        return (packet, state, prefix, lifecycle, breakdown, CampaignCombatReactionClosureCodec.SerializeState(closure));
    }
    private static CampaignCombatCandidate Certify(CampaignCombatCreationRequest request, byte[] created,
        CampaignWorldSnapshotV7 world, CampaignCombatCycleAuthority cycle)
        => CampaignCombatCertification.CertifyInitialProfileFacts(request, created, world, cycle, cycle.ActingSide,
            WeatherKind.Normal, WeatherKind.Normal) ?? throw new JsonException("CMB-RCE-003");
    private static CombatActualReactionCombatEntryResult Result(Packet packet, CampaignCombatBreakdownCompletionState state,
        CampaignCombatCreationRequest request, byte[] closureControl, byte[][] prefix, byte[][] lifecycle, byte[][] breakdown, byte[]? accepted = null, bool duplicate = false)
        => new(packet, state, state.Completion is null ? null : Certify(request, packet.Created, state.Lifecycle.Movement.World,
            state.Lifecycle.Movement.Opening.Cycle!), closureControl, prefix, lifecycle, breakdown, accepted, duplicate);

    // Bounded typed ports of the frozen private transition bodies; admission above is different.
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
        if (input != CampaignCombatMovementLifecycle.Command(state)) throw new JsonException("Lifecycle capability or occurrence does not match current route.");
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
    private static CampaignCombatBreakdownCompletionEvent EmitBreakdown(CampaignCombatBreakdownCompletionState state,
        CampaignCombatBreakdownCompletionInput input)
    {
        _ = CampaignCombatBreakdownCompletionCodec.SerializeInput(input);
        if (state.Completion is not null || input != CampaignCombatBreakdownCompletion.Command(state) ||
            state.Lifecycle.Events.Count != 3 || state.SequencePosition.PositionId != "land.position.operation-1.first-player.movement-and-combat.breakdown-determination" ||
            state.Lifecycle.MovementEnd is null || state.Lifecycle.BreakdownFlow is not CampaignBreakdownFlow.Idle ||
            state.Lifecycle.InterruptContext is not null || state.StateVersion == long.MaxValue || state.Receipts.Count >= 512)
            throw new JsonException("CMB-RCE-006");
        var next = Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.combat.position-determination");
        return new(state.Lifecycle, input, new(5, next.PositionId, next.GameTurn, next.OperationStage, next.StageId,
            next.PhaseId, next.SegmentId, next.StepId, next.ActorRole, next.ActiveSide, next.Sources));
    }
}
