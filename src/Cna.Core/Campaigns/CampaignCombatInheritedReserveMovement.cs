using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Codec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCodec;

namespace Cna.Core.Campaigns;

/// <summary>Owns independent control inputs and bytes; every admission replays the full source.</summary>
internal sealed class CombatInheritedReserveMovementSource
{
    private readonly CombatCycleControlInput[] inputs;
    private readonly byte[][] events;
    public CombatInheritedReserveMovementSource(CombatInheritedCycleControlSource source,
        IReadOnlyList<CombatCycleControlInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != 2 || events.Count != 2) throw new JsonException("Released-I Movement requires two control events.");
        Source = source; this.inputs = inputs.ToArray(); this.events = events.Select(Codec.Copy).ToArray();
        foreach (var input in this.inputs) _ = CampaignCombatInheritedCycleControlCodec.SerializeInput(input);
    }
    public CombatInheritedCycleControlSource Source { get; }
    internal InheritedReserveMoveBasis Derive()
    {
        var control = CampaignCombatInheritedCycleControl.Replay(Source, inputs, events);
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatInheritedCycleControlCodec.SerializeState(control));
        var expected = control.Basis.Proof.Source.Base.ReleaseBase.Cycle.ActingSide == LandSide.Axis
            ? "sha256:d7324702abaa949f57c55ada4b9c9278971539010a8615081ae1da37eb9aa18b"
            : "sha256:a95f8bf91878076c4f5b0685efc4139d093235f83a878519dda3cf5e489622f5";
        if (hash != expected || control.Status != "repeated" || control.StateVersion != 27 || control.ActiveCycle?.Ordinal != 2)
            throw new JsonException("Released-I Movement requires exact authenticated frozen3k repeat terminal.");
        var cycle = control.ActiveCycle; var member = control.Members.Single(); var history = member.History;
        if (member.Status != CampaignElementReserveStatus.None || member.SpentCp != new CapabilityPointAmount(0, 1) ||
            history.ReleasedType != "I" || history.VoluntaryCeiling != 10 || history.NextMovement !=
            new CombatReleaseMovementException(new(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide), 2, "pending", null))
            throw new JsonException("Released-I Movement ledger differs from repeat scope.");
        var p = Cna1979LandSequence.CreateTurn(cycle.GameTurn).Single(p => p.PositionId == control.PositionId);
        var position = new LandSequencePosition(5, p.PositionId, p.GameTurn, p.OperationStage, p.StageId, p.PhaseId,
            p.SegmentId, p.StepId, p.ActorRole, cycle.ActingSide, p.Sources);
        return new(control, Array.AsReadOnly(inputs.ToArray()), Array.AsReadOnly(events.Select(Codec.Copy).ToArray()), position);
    }
}
internal sealed record InheritedReserveMoveBasis(CombatCycleControlState Control, IReadOnlyList<CombatCycleControlInput> Inputs,
    IReadOnlyList<byte[]> Events, LandSequencePosition Position);
internal sealed record InheritedReserveMoveState(InheritedReserveMoveBasis Basis, string BaseHash, long StateVersion,
    string Prefix, CampaignWorldSnapshotV7 World, CombatReleaseMember ReleaseMember,
    IReadOnlyList<CycleMoveTrack> Tracks, IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts);
internal sealed class InheritedReserveMoveResult(InheritedReserveMoveState state, byte[] eventBytes, bool duplicate)
{
    private readonly byte[] bytes = eventBytes.ToArray();
    public InheritedReserveMoveState State { get; } = state;
    public byte[] EventBytes => bytes.ToArray();
    public bool Duplicate { get; } = duplicate;
}

/// <summary>One frozen3l released-I move, with exception still pending. No Movement completion.</summary>
internal static class CampaignCombatInheritedReserveMovement
{
    public static InheritedReserveMoveState Replay(CombatInheritedReserveMovementSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source); var (trusted, retained) = Capture(inputs, events);
        var basis = source.Derive(); var control = basis.Control;
        var state = new InheritedReserveMoveState(basis, CampaignOpeningPreambleCodec.Hash(Codec.SerializeBase(basis)),
            control.StateVersion, control.Prefix, control.World, control.Members.Single(), [], []);
        foreach (var (input, bytes) in trusted.Zip(retained))
        {
            var result = Advance(state, input);
            if (!bytes.AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Released-I Movement event differs from replay.");
            state = result.State;
        }
        _ = Codec.SerializeState(state); return state;
    }
    public static CycleMoveInput Command(InheritedReserveMoveState state, string destination)
    {
        var unit = state.ReleaseMember.Unit;
        return new(new(1, "move", CampaignCombatReserveCompletionCodec.CycleId(state.Basis.Control.ActiveCycle!),
            state.Basis.Position.PositionId, state.StateVersion, unit,
            state.World.Elements.Single(e => e.ElementId == unit.ElementId).CurrentLocationId, destination),
            unit.OriginalSide == "axis" ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);
    }
    public static InheritedReserveMoveResult Apply(CombatInheritedReserveMovementSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events, CycleMoveInput input, byte[]? cachedState = null)
    {
        var cache = cachedState is null ? null : Codec.Copy(cachedState);
        var (trusted, retained) = Capture(inputs, events); var state = Replay(source, trusted, retained);
        if (cache is not null && !cache.AsSpan().SequenceEqual(Codec.SerializeState(state))) throw new JsonException("Released-I Movement cache differs from replay.");
        Authorize(state, input);
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatCycleMovementCodec.SerializeCommand(input.Command));
        for (var i = 0; i < state.Receipts.Count; i++)
            if (state.Receipts[i].CommandHash == hash && state.Receipts[i].Actor == input.Actor) return new(state, retained[i], true);
        return Advance(state, input);
    }
    private static void Authorize(InheritedReserveMoveState state, CycleMoveInput input)
    {
        _ = CampaignCombatCycleMovementCodec.SerializeInput(input);
        var command = input.Command; var unit = state.ReleaseMember.Unit; var cycle = state.Basis.Control.ActiveCycle!;
        if (command.ContractVersion != 1 || command.Kind != "move" || command.Unit != unit ||
            CampaignCombatCycleMovementCodec.Actor(input.Actor) != unit.OriginalSide ||
            unit.OriginalSide != CampaignSnapshotSerializer.FormatSide(cycle.ActingSide) ||
            command.CycleId != CampaignCombatReserveCompletionCodec.CycleId(cycle) || command.PositionId != state.Basis.Position.PositionId)
            throw new JsonException("Foreign released-I Movement actor/unit/cycle.");
    }
    private static InheritedReserveMoveResult Advance(InheritedReserveMoveState prior, CycleMoveInput input)
    {
        Authorize(prior, input); var cmd = input.Command; var world = prior.World;
        if (prior.Receipts.Count != 0 || cmd.ExpectedPriorVersion != prior.StateVersion || prior.StateVersion == long.MaxValue)
            throw new JsonException("Released-I Movement is stale or exceeds one-event capacity.");
        var pack = prior.Basis.Control.Basis.Proof.Request.Context.Setup.Artifact.Definition;
        var element = world.Elements.Single(e => e.ElementId == cmd.Unit.ElementId);
        var facts = pack.Elements.Single(e => e.ElementId == cmd.Unit.ElementId);
        var edge = pack.Edges.SingleOrDefault(e => e.FirstLocationId == cmd.OriginLocationId && e.SecondLocationId == cmd.DestinationLocationId ||
            e.SecondLocationId == cmd.OriginLocationId && e.FirstLocationId == cmd.DestinationLocationId);
        var destination = pack.Locations.SingleOrDefault(l => l.LocationId == cmd.DestinationLocationId);
        var cycle = prior.Basis.Control.ActiveCycle!; var op = element.OperationalState;
        if (cmd.OriginLocationId != element.CurrentLocationId || edge is null || destination?.TerrainId != "land.terrain.clear" || edge.Features.Count != 0 ||
            facts.MobilityId != Cna1979Movement.NonMotorizedMobilityId || facts.PlacementMode != ContentPlacementMode.Independent ||
            facts.BaseCapabilityPointAllowance != 10 || facts.Components.Any(c => c.ComponentClassId != "land.combat-component.infantry") ||
            element.ReserveStatus != CampaignElementReserveStatus.None || element.Ammunition.Points != 10 || element.Components.Sum(c => c.CurrentToe) != 10 ||
            op.LedgerGameTurn != cycle.GameTurn || op.LedgerOperationStage != cycle.OperationStage ||
            op.VehicleBreakdownState is not null || op.MovementEnded is not null || prior.ReleaseMember.SpentCp != op.CapabilityPointsExpended ||
            world.Elements.Any(e => e.ElementId != element.ElementId && e.CurrentLocationId == cmd.DestinationLocationId) ||
            world.Guards.Any(g => g.CurrentLocationId == cmd.DestinationLocationId))
            throw new JsonException("Outside released-I adjacent free Clear Movement profile.");
        var terrain = Cna1979Movement.LookupTerrain(destination.TerrainId, facts.MobilityId);
        if (!terrain.IsSupported || terrain.Value.Cost != new CapabilityPointAmount(2, 1)) throw new JsonException("Unsupported released-I terrain cost.");
        CombatCycleMovementCharge charge;
        try
        {
            charge = CampaignCombatCycleMovementRules.AssessAndCharge(cmd.Unit, op, 10, CampaignCombatSpendCeiling.ReleasedReserveI,
            2, world.Relationships, "mov.preview", world.CohesionCauses);
        }
        catch (Exception e) when (e is ArgumentException or OverflowException) { throw new JsonException("Released-I charge rejected.", e); }
        // Exact source admits no settlement/relationship work; preserve that boundary rather than widen World validation.
        if (charge.Assessment.Affected.Count != 0 || charge.Spending.Cause is not null || world.Settlements.Count != 0)
            throw new JsonException("Outside frozen released-I no-settlement profile.");
        var rep = world.Representations.Single(r => r.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement && r.BoundElementIds.SequenceEqual([element.ElementId]));
        var effect = CampaignCombatCycleMovementCodec.Value(new
        {
            kind = "ordinary-move",
            unit = cmd.Unit,
            representationId = rep.RepresentationId,
            originLocationId = cmd.OriginLocationId,
            destinationLocationId = cmd.DestinationLocationId,
            costs = new
            {
                terrainCost = 2,
                breakOffCost = charge.Assessment.BreakOffCost,
                totalCost = charge.Assessment.TotalCost.Numerator,
                baseCpa = 10,
                voluntaryCeiling = 10,
                beforeCp = op.CapabilityPointsExpended.Numerator,
                afterCp = charge.Spending.State.CapabilityPointsExpended.Numerator,
                excessCpDp = 0,
                beforeCohesion = op.CohesionLevel,
                afterCohesion = charge.Spending.State.CohesionLevel
            },
            endedMemberships = Array.Empty<object>()
        });
        var unsigned = Codec.Event(prior, input, effect, null);
        var receipt = "mov." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.ordinary-movement-receipt.v1", unsigned)[7..];
        var bytes = Codec.Event(prior, input, effect, receipt);
        var moved = new CampaignElementStateV6(element.ElementId, cmd.DestinationLocationId, element.ReserveStatus, charge.Spending.State,
            element.Components, element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
        var projected = new CampaignWorldSnapshotV7(world.ContractVersion, world.CreationBinding,
            world.Elements.Select(e => e.ElementId == moved.ElementId ? moved : e),
            world.Representations.Select(r => r == rep ? new CampaignMapRepresentationState(r.RepresentationId, cmd.DestinationLocationId, r.BindingKind, r.BoundElementIds) : r),
            world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards,
            world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
        var state = new InheritedReserveMoveState(prior.Basis, prior.BaseHash, checked(prior.StateVersion + 1),
            CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes), projected,
            prior.ReleaseMember with { SpentCp = charge.Spending.State.CapabilityPointsExpended },
            Array.AsReadOnly(new[] { new CycleMoveTrack(cmd.Unit, Array.AsReadOnly(new[] { cmd.OriginLocationId, cmd.DestinationLocationId })) }),
            Array.AsReadOnly(new[] { new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatCycleMovementCodec.SerializeCommand(cmd)),
                CampaignOpeningPreambleCodec.Hash(bytes), receipt, input.Actor, checked(prior.StateVersion + 1)) }));
        _ = Codec.SerializeState(state); return new(state, bytes, false);
    }
    private static (CycleMoveInput[], byte[][]) Capture(IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != events.Count || events.Count > 1) throw new JsonException("Released-I Movement suffix count/capacity mismatch.");
        var trusted = inputs.ToArray(); var retained = events.Select(Codec.Copy).ToArray();
        foreach (var input in trusted) _ = CampaignCombatCycleMovementCodec.SerializeInput(input);
        return (trusted, retained);
    }
}
