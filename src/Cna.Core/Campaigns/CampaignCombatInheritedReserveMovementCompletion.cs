using System.Text.Json;
using Cna.Core.Rules;
using Codec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCompletionCodec;
using MoveCodec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCodec;

namespace Cna.Core.Campaigns;

/// <summary>Owns the one retained move and independently authenticates its complete creation-rooted history.</summary>
internal sealed class CombatInheritedReserveMovementCompletionSource
{
    private readonly CycleMoveInput[] inputs;
    private readonly byte[][] events;
    public CombatInheritedReserveMovementCompletionSource(CombatInheritedReserveMovementSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != 1 || events.Count != 1) throw new JsonException("Completion requires one retained released-I move.");
        Source = source; this.inputs = inputs.ToArray(); this.events = events.Select(MoveCodec.Copy).ToArray();
        foreach (var input in this.inputs) _ = CampaignCombatCycleMovementCodec.SerializeInput(input);
    }
    public CombatInheritedReserveMovementSource Source { get; }
    internal ReserveMovementCompletionBasis Derive()
    {
        var move = CampaignCombatInheritedReserveMovement.Replay(Source, inputs, events);
        var cycle = move.Basis.Control.ActiveCycle!;
        var expected = cycle.ActingSide == LandSide.Axis
            ? "sha256:4f3762fcfb6d650e5cc028a3f2e97f1043e3602441240cc5f6ab808af1868c6d"
            : "sha256:6b54f83c338d9538e897a8b33af096a0e9ee2a4cbe64f04deade498e82235f30";
        var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide);
        if (CampaignOpeningPreambleCodec.Hash(MoveCodec.SerializeState(move)) != expected || move.StateVersion != 28 ||
            move.Tracks.Count != 1 || move.Receipts.Count != 1 || cycle.Ordinal != 2 ||
            move.ReleaseMember.History.NextMovement != new CombatReleaseMovementException(scope, 2, "pending", null))
            throw new JsonException("Completion requires exact authenticated frozen3l terminal.");
        var interrupt = new LandSequencePosition(5, Cna1979LandSequenceV4.BreakdownStopPositionId, 1, 1, LandStageIds.Operation,
            LandPhaseIds.MovementAndCombat, LandSegmentIds.BreakdownDetermination, null, LandActorRole.None, null,
            [Cna1979LandSequence.SourceReference, new("spi-1979-land-rules", "21.24-21.26")]);
        var target = Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.breakdown-determination");
        var breakdown = new LandSequencePosition(5, target.PositionId, target.GameTurn, target.OperationStage, target.StageId,
            target.PhaseId, target.SegmentId, target.StepId, target.ActorRole, target.ActiveSide, target.Sources);
        return new(move, Array.AsReadOnly(inputs.ToArray()), Array.AsReadOnly(events.Select(MoveCodec.Copy).ToArray()), interrupt, breakdown);
    }
}
internal sealed record ReserveMovementCompletionBasis(InheritedReserveMoveState Movement, IReadOnlyList<CycleMoveInput> Inputs,
    IReadOnlyList<byte[]> Events, LandSequencePosition InterruptPosition, LandSequencePosition BreakdownPosition);
internal sealed record ReserveMovementCompletionCommand(int ContractVersion, string Kind, string BaseHash, string CycleId,
    string PositionId, long ExpectedPriorVersion, string? CapabilityId, string ActionId);
internal sealed record ReserveMovementCompletionInput(ReserveMovementCompletionCommand Command, CampaignOpeningPreambleActor Actor);
internal sealed record ReserveMovementStop(string StopId, long RecordedStateVersion, CycleMoveTrack Track, string MovementReceiptId);
internal sealed record ReserveMovementCompletionState(ReserveMovementCompletionBasis Basis, string BaseHash, long StateVersion,
    string Prefix, LandSequencePosition Position, ReserveMovementStop? Stop, CampaignCombatMovementInterrupt? InterruptContext,
    CombatContinuationMovementEnd? MovementEnd, CombatReleaseMember ReleaseMember, IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts);
internal sealed class ReserveMovementCompletionResult(ReserveMovementCompletionState state, byte[] eventBytes, bool duplicate)
{
    private readonly byte[] bytes = eventBytes.ToArray();
    public ReserveMovementCompletionState State { get; } = state;
    public byte[] EventBytes => bytes.ToArray();
    public bool Duplicate { get; } = duplicate;
}

/// <summary>Frozen3m stop, empty resolution and atomic exception expiry. Ends before Breakdown execution.</summary>
internal static class CampaignCombatInheritedReserveMovementCompletion
{
    public static ReserveMovementCompletionState Replay(CombatInheritedReserveMovementCompletionSource source,
        IReadOnlyList<ReserveMovementCompletionInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source); var (trusted, retained) = Capture(inputs, events);
        var basis = source.Derive(); var move = basis.Movement;
        var state = new ReserveMovementCompletionState(basis, CampaignOpeningPreambleCodec.Hash(Codec.SerializeBase(basis)),
            move.StateVersion, move.Prefix, move.Basis.Position, null, null, null, move.ReleaseMember, []);
        foreach (var (input, bytes) in trusted.Zip(retained))
        {
            var result = Advance(state, input);
            if (!bytes.AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Completion event differs from replay.");
            state = result.State;
        }
        _ = Codec.SerializeState(state); return state;
    }
    public static ReserveMovementCompletionInput Command(ReserveMovementCompletionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var index = state.Receipts.Count;
        if (index > 2 || state.MovementEnd is not null || index == 0 && (state.Stop is not null || state.InterruptContext is not null) ||
            index == 1 && (state.Stop is null || state.InterruptContext is null) || index == 2 && (state.Stop is not null || state.InterruptContext is not null))
            throw new JsonException("No supported completion command at this cut.");
        var kind = Codec.Kinds[index]; var actor = index == 1 ? CampaignOpeningPreambleActor.System : Owner(state);
        var command = new ReserveMovementCompletionCommand(1, kind, state.BaseHash, CycleId(state), state.Position.PositionId,
            state.StateVersion, index < 2 ? Codec.Capability(state, index) : null, "pending");
        command = command with { ActionId = CampaignOpeningPreambleCodec.Hash(Codec.SerializeCommand(command, false)) };
        return new(command, actor);
    }
    public static ReserveMovementCompletionResult Apply(CombatInheritedReserveMovementCompletionSource source,
        IReadOnlyList<ReserveMovementCompletionInput> inputs, IReadOnlyList<byte[]> events, ReserveMovementCompletionInput input, byte[]? cachedState = null)
    {
        var cache = cachedState is null ? null : MoveCodec.Copy(cachedState);
        var (trusted, retained) = Capture(inputs, events); var state = Replay(source, trusted, retained);
        if (cache is not null && !cache.AsSpan().SequenceEqual(Codec.SerializeState(state))) throw new JsonException("Completion cache differs from replay.");
        Authorize(state, input);
        var hash = CampaignOpeningPreambleCodec.Hash(Codec.SerializeInput(input));
        for (var i = 0; i < state.Receipts.Count; i++)
            if (state.Receipts[i].CommandHash == hash && state.Receipts[i].Actor == input.Actor) return new(state, retained[i], true);
        return Advance(state, input);
    }
    private static void Authorize(ReserveMovementCompletionState state, ReserveMovementCompletionInput input)
    {
        _ = Codec.SerializeInput(input);
        if (input.Actor != (input.Command.Kind == Codec.Kinds[1] ? CampaignOpeningPreambleActor.System : Owner(state)) ||
            input.Command.ContractVersion != 1 || input.Command.BaseHash != state.BaseHash || input.Command.CycleId != CycleId(state))
            throw new JsonException("Foreign completion actor or provenance.");
    }
    private static ReserveMovementCompletionResult Advance(ReserveMovementCompletionState prior, ReserveMovementCompletionInput input)
    {
        Authorize(prior, input);
        if (prior.StateVersion == long.MaxValue || prior.Receipts.Count >= 3 || input != Command(prior))
            throw new JsonException("Stale or unsupported completion occurrence.");
        var index = prior.Receipts.Count; var version = checked(prior.StateVersion + 1); var next = prior;
        if (index == 0)
        {
            if (prior.Position != prior.Basis.Movement.Basis.Position) throw new JsonException("Stop requires actual Movement position.");
            var stop = new ReserveMovementStop("pending", version, prior.Basis.Movement.Tracks.Single(), prior.Basis.Movement.Receipts.Single().ReceiptId);
            stop = stop with { StopId = Codec.StopId(prior, stop) };
            next = prior with
            {
                Position = prior.Basis.InterruptPosition,
                Stop = stop,
                InterruptContext = new(Cycle(prior), CycleId(prior), prior.Position)
            };
        }
        else if (index == 1)
        {
            if (prior.Position != prior.Basis.InterruptPosition || prior.InterruptContext!.Cycle != Cycle(prior) ||
                prior.InterruptContext.CycleId != CycleId(prior) || prior.InterruptContext.SequencePosition != prior.Basis.Movement.Basis.Position)
                throw new JsonException("Resolution requires exact suspended Movement.");
            next = prior with { Position = prior.InterruptContext.SequencePosition, Stop = null, InterruptContext = null };
        }
        else
        {
            if (prior.Position != prior.Basis.Movement.Basis.Position) throw new JsonException("Completion requires resumed Movement.");
            next = prior with { Position = prior.Basis.BreakdownPosition };
        }
        // Derive locations/proximity before emission; bind expiry only after the accepted receipt exists.
        var preview = index == 2 ? ExpireMovement(prior, "pending").Proof : null;
        var unsigned = Codec.Event(prior, next, input, preview, null);
        var receiptId = "irmc." + CampaignOpeningPreambleCodec.HashWithDomain(Codec.Domains[index], unsigned)[7..];
        var bytes = Codec.Event(prior, next, input, preview, receiptId);
        if (index == 2)
        {
            var expiry = ExpireMovement(prior, receiptId);
            next = next with { ReleaseMember = expiry.Member, MovementEnd = expiry.Proof };
        }
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(Codec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), receiptId, input.Actor, version);
        next = next with
        {
            StateVersion = version,
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            Receipts = Array.AsReadOnly(prior.Receipts.Append(receipt).ToArray())
        };
        _ = Codec.SerializeState(next); return new(next, bytes, false);
    }
    // Bounded port of D2b.2 expire_movement; predecessor is authenticated, not a caller projection.
    private static (CombatReleaseMember Member, CombatContinuationMovementEnd Proof) ExpireMovement(ReserveMovementCompletionState state, string receipt)
    {
        var cycle = Cycle(state); var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide);
        var retained = state.Basis.Movement.Basis.Control.Basis.Proof.Source.Base.Predecessor.MovementEnd!;
        var prior = new CombatContinuationMovementEnd(new(retained.Scope.GameTurn, retained.Scope.OperationStage,
            retained.Scope.PlayerPhaseSlot, retained.Scope.ActingSide), retained.Ordinal, retained.CompletionReceiptId,
            retained.EndLocations.Select(l => new CombatMovementEndLocation(l.Unit, l.LocationId)), retained.ExcludedBefore);
        var world = state.Basis.Movement.World;
        var pack = state.Basis.Movement.Basis.Control.Basis.Proof.Request.Context.Setup.Artifact.Definition;
        var locations = world.Elements.Select(e => new CombatMovementEndLocation(new(world.CreationBinding,
            pack.Elements.Single(f => f.ElementId == e.ElementId).SideId, e.ElementId), e.CurrentLocationId))
            .OrderBy(l => l.Unit.CreationBinding, StringComparer.Ordinal).ThenBy(l => l.Unit.OriginalSide, StringComparer.Ordinal)
            .ThenBy(l => l.Unit.ElementId, StringComparer.Ordinal).ToArray();
        if (prior.Scope != scope || prior.Ordinal != cycle.Ordinal - 1 || !prior.Locations.Select(l => l.Unit).SequenceEqual(locations.Select(l => l.Unit)))
            throw new JsonException("Movement proof must retain exact prior unit coverage and scope.");
        var side = CampaignSnapshotSerializer.FormatSide(cycle.ActingSide);
        CampaignCombatUnitKey[] Excluded(IReadOnlyList<CombatMovementEndLocation> at, IReadOnlyList<CampaignCombatUnitKey> old) =>
            at.Where(l => l.Unit.OriginalSide == side && (old.Contains(l.Unit) || !at.Any(enemy => enemy.Unit.OriginalSide != side &&
                Near(l.LocationId, enemy.LocationId)))).Select(l => l.Unit).ToArray();
        bool Near(string origin, string destination) => origin == destination || pack.Edges.Any(e =>
            e.FirstLocationId == origin && e.SecondLocationId == destination || e.SecondLocationId == origin && e.FirstLocationId == destination) ||
            pack.Edges.Where(e => e.FirstLocationId == origin || e.SecondLocationId == origin).Any(e =>
            {
                var middle = e.FirstLocationId == origin ? e.SecondLocationId : e.FirstLocationId;
                return pack.Edges.Any(edge => edge.FirstLocationId == middle && edge.SecondLocationId == destination ||
                    edge.SecondLocationId == middle && edge.FirstLocationId == destination);
            });
        var member = state.ReleaseMember; var h = member.History; var ex = h.NextMovement;
        if (h.Scope != scope || ex != new CombatReleaseMovementException(scope, cycle.Ordinal, "pending", null) || ex.Ordinal != h.ReleaseCycle + 1 ||
            !locations.Where(l => l.Unit.OriginalSide == side).Select(l => l.Unit).SequenceEqual([member.Unit]))
            throw new JsonException("Movement exception does not match completion scope.");
        var proof = new CombatContinuationMovementEnd(scope, cycle.Ordinal, receipt, locations,
            Excluded(locations, Excluded(prior.Locations, prior.ExcludedBefore)));
        return (member with { History = h with { NextMovement = ex with { Status = "expired", CompletionReceiptId = receipt } } }, proof);
    }
    internal static CampaignCombatCycleAuthority Cycle(ReserveMovementCompletionState state) => state.Basis.Movement.Basis.Control.ActiveCycle!;
    internal static string CycleId(ReserveMovementCompletionState state) => CampaignCombatReserveCompletionCodec.CycleId(Cycle(state));
    private static CampaignOpeningPreambleActor Owner(ReserveMovementCompletionState state) => Cycle(state).ActingSide == LandSide.Axis
        ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
    private static (ReserveMovementCompletionInput[], byte[][]) Capture(IReadOnlyList<ReserveMovementCompletionInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != events.Count || events.Count > 3) throw new JsonException("Completion suffix count/capacity mismatch.");
        var trusted = inputs.ToArray(); var retained = events.Select(MoveCodec.Copy).ToArray();
        foreach (var input in trusted) _ = Codec.SerializeInput(input);
        return (trusted, retained);
    }
}
