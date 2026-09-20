using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Closed ordinary-infantry Move4 profile; retained creation-to-opening history is mandatory.</summary>
internal static class CampaignCombatInheritedMovement
{
    public static CampaignCombatInheritedMovementState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count > 32) throw new JsonException("Inherited movement record capacity exceeded.");
        var state = Initial(CampaignCombatReserveOpening.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents));
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized inherited movement event.");
            var bytes = retained.ToArray();
            var (after, emitted) = Emit(state, CampaignCombatInheritedMovementCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatInheritedMovementCodec.SerializeEvent(emitted)))
                throw new JsonException("Move4 differs from canonical history-derived effects.");
            state = after;
        }
        return state;
    }
    public static CampaignCombatInheritedMovementResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> events, CampaignCombatInheritedMovementInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.ExpectedPriorVersion != input.Command.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting inherited movement retry.");
            return new(state, CampaignCombatInheritedMovementCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new(after, CampaignCombatInheritedMovementCodec.SerializeEvent(emitted), false);
    }
    public static CampaignCombatInheritedMovementState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized inherited movement projection.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, events);
        if (!bytes.SequenceEqual(CampaignCombatInheritedMovementCodec.SerializeState(state)))
            throw new JsonException("Movement projection differs from full retained history.");
        return state;
    }
    public static CampaignCombatInheritedMovementInput Command(CampaignCombatInheritedMovementState state, string destination)
    {
        var member = state.Members[0];
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        return new(new(2, "move-element", receipt.CreationBinding, receipt.CreationEventHash, state.Opening.CycleId!,
            state.StateVersion, state.SequencePosition.PositionId, member.Unit,
            state.World.Elements.Single(e => e.ElementId == member.Unit.ElementId).CurrentLocationId, destination),
            state.Opening.Cycle!.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);
    }
    private static CampaignCombatInheritedMovementState Initial(CampaignCombatReserveOpeningState opening)
    {
        if (opening.Cycle is not { Ordinal: 1, GameTurn: 1, OperationStage: 1, PlayerPhaseSlot: "first-acting-side" } cycle ||
            opening.StateVersion != 11 || opening.Receipts.Count != 10 || opening.CompletionReceiptId is null ||
            opening.SequencePosition.PositionId != "land.position.operation-1.first-player.movement-and-combat.movement" ||
            opening.SequencePosition.ActiveSide is not null || cycle.ActingSide != opening.Predecessor.FirstActingSide ||
            opening.Predecessor.Stage.Weather.Weather[0].Kind != WeatherKind.Normal ||
            opening.Predecessor.World.Elements.Any(e => e.ReserveStatus != CampaignElementReserveStatus.None))
            throw new JsonException("Move4 requires actual ordinary NONE first opening and Normal Weather.");
        return new(opening, opening.StateVersion, opening.Prefix, opening.Predecessor.World, opening.Predecessor.Members,
            opening.Receipts, [], [], null, []);
    }
    private static void Authorize(CampaignCombatInheritedMovementState state, CampaignCombatInheritedMovementInput input)
    {
        _ = CampaignCombatInheritedMovementCodec.SerializeInput(input);
        var expected = Command(state, input.Command.DestinationLocationId);
        if (input.Actor != expected.Actor || input.Command.Unit != expected.Command.Unit)
            throw new JsonException("Move4 requires trusted cycle owner and original own member.");
        if (input.Command.CreationBinding != expected.Command.CreationBinding || input.Command.CreationEventHash != expected.Command.CreationEventHash ||
            input.Command.CycleId != expected.Command.CycleId || input.Command.ExpectedPositionId != expected.Command.ExpectedPositionId)
            throw new JsonException("Movement provenance or position does not match actual opening.");
    }
    private static (CampaignCombatInheritedMovementState State, CampaignCombatInheritedMovementEvent Event) Emit(
        CampaignCombatInheritedMovementState state, CampaignCombatInheritedMovementInput input)
    {
        Authorize(state, input);
        var cmd = input.Command;
        if (cmd.ExpectedPriorVersion != state.StateVersion || state.Events.Count >= 32)
            throw new JsonException("Stale movement occurrence or exhausted capacity.");
        var world = state.World;
        var element = world.Elements.Single(e => e.ElementId == cmd.Unit.ElementId);
        var pack = state.Opening.Predecessor.Stage.Weather.Opening.Creation.Setup.Artifact.Definition;
        var facts = pack.Elements.Single(e => e.ElementId == element.ElementId);
        var edge = pack.Edges.SingleOrDefault(e => Connects(e, cmd.OriginLocationId, cmd.DestinationLocationId));
        var destination = pack.Locations.SingleOrDefault(l => l.LocationId == cmd.DestinationLocationId);
        if (cmd.OriginLocationId != element.CurrentLocationId || edge is null || destination is null)
            throw new JsonException("Move requires current origin and adjacent destination.");
        if (edge.Features.Count != 0 || destination.TerrainId != "land.terrain.clear" ||
            facts.MobilityId != Cna1979Movement.NonMotorizedMobilityId || facts.PlacementMode != ContentPlacementMode.Independent ||
            facts.BaseCapabilityPointAllowance != 10 || facts.CombatClassificationId != "land.combat-classification.combat-unit" ||
            facts.Components.Any(c => c.ComponentClassId != "land.combat-component.infantry") ||
            element.ReserveStatus != CampaignElementReserveStatus.None || element.OperationalState.VehicleBreakdownState is not null ||
            element.OperationalState.MovementEnded is not null || element.Components.Any(c => c.CurrentToe <= 0) || element.Ammunition.Points != 10 ||
            world.Relationships.Count != 0 || world.Guards.Count != 0 || world.BrokenVehicleLots.Count != 0 ||
            world.FutureObligations.Count != 0 || world.Settlements.Count != 0)
            throw new JsonException("Move lies outside featureless Clear nonmotorized ordinary-infantry profile.");
        if (element.OperationalState.LedgerGameTurn != 1 || element.OperationalState.LedgerOperationStage != 1 ||
            element.OperationalState.CapabilityPointsExpended.Denominator != 1 || state.Members[0].SpentCp != element.OperationalState.CapabilityPointsExpended)
            throw new JsonException("Movement member and current CP ledger differ.");
        if (world.Elements.Any(e => e != element && e.CurrentLocationId == cmd.DestinationLocationId) ||
            world.Representations.Any(r => r.CurrentLocationId == cmd.DestinationLocationId))
            throw new JsonException("Occupied destination is unsupported.");
        var representation = world.Representations.Single(r => r.BoundElementIds.SequenceEqual([element.ElementId]));
        if (representation.CurrentLocationId != cmd.OriginLocationId || representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement)
            throw new JsonException("Independent representation does not match current member location.");
        // Adjacency itself requires Reaction, before any hypothetical eligibility filtering.
        if (world.Representations.Any(r => r.BoundElementIds.Any(id => pack.Elements.Single(e => e.ElementId == id).SideId != cmd.Unit.OriginalSide) &&
            pack.Edges.Any(e => Connects(e, r.CurrentLocationId, cmd.DestinationLocationId))))
            throw new JsonException("Destination requires unsupported positive Reaction.");
        var terrain = Cna1979Movement.LookupTerrain(destination.TerrainId, facts.MobilityId);
        if (!terrain.IsSupported || terrain.Value.Cost != new CapabilityPointAmount(2, 1))
            throw new JsonException("Unexpected Clear terrain cost.");
        var route = state.BreakdownFlow is null
            ? CampaignBreakdownRoute.Create(state.Opening.Cycle!.CampaignId, state.Opening.Cycle.RulesetHash,
                checked(state.StateVersion + 1), element.ElementId, representation.RepresentationId, state.Opening.Cycle.ActingSide,
                cmd.OriginLocationId, cmd.DestinationLocationId, [])
            : state.BreakdownFlow.Route.AtLocation(cmd.DestinationLocationId);
        if (state.BreakdownFlow is not null && (state.BreakdownFlow.Route.ElementId != element.ElementId || state.BreakdownFlow.Route.CurrentLocationId != cmd.OriginLocationId))
            throw new JsonException("Moving route does not match current member.");
        var emitted = new CampaignCombatInheritedMovementEvent(state, input, representation.RepresentationId, terrain.Value.Cost, terrain.Sources, new(route));
        // Event excludes projected World: obtain its real receipt before attaching causal DP.
        var bytes = CampaignCombatInheritedMovementCodec.SerializeEvent(emitted);
        CampaignCombatSpendResult spent;
        try
        {
            spent = CampaignCombatSpending.ChargeOrdinary(element.OperationalState, terrain.Value.Cost, 10,
            CampaignCombatSpendCeiling.Ordinary, element.ElementId, emitted.ReceiptId, world.CohesionCauses);
        }
        catch (Exception error) when (error is ArgumentException or OverflowException) { throw new JsonException("Ordinary movement spending rejected.", error); }
        var moved = new CampaignElementStateV6(element.ElementId, cmd.DestinationLocationId, element.ReserveStatus, spent.State,
            element.Components, element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
        var projected = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == element ? moved : e),
            world.Representations.Select(r => r == representation ? new CampaignMapRepresentationState(r.RepresentationId, cmd.DestinationLocationId, r.BindingKind, r.BoundElementIds) : r),
            world.BrokenVehicleLots, spent.Causes, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatInheritedMovementCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        var locations = state.Tracks.Count == 0 ? new[] { cmd.OriginLocationId, cmd.DestinationLocationId }
            : state.Tracks[0].Route.Append(cmd.DestinationLocationId).ToArray();
        return (new(state.Opening, emitted.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), projected,
            [state.Members[0] with { SpentCp = spent.State.CapabilityPointsExpended }], state.Receipts.Append(receipt),
            [new(cmd.Unit, locations)], state.ActualProgressRefs.Append(new("element-moved", receipt.ReceiptId, receipt.EventHash)),
            emitted.Flow, state.Events.Append(emitted)), emitted);
    }
    internal static CampaignWorldSnapshotV7 ExpectedWorld(CampaignCombatInheritedMovementState state)
    {
        var expected = Initial(state.Opening);
        foreach (var retained in state.Events)
        {
            var (after, emitted) = Emit(expected, retained.Input);
            if (!CampaignCombatInheritedMovementCodec.SerializeEvent(retained).AsSpan().SequenceEqual(CampaignCombatInheritedMovementCodec.SerializeEvent(emitted)))
                throw new JsonException("Moved World evidence is not derived from actual opening.");
            expected = after;
        }
        return expected.World;
    }
    private static bool Connects(ContentHexEdge edge, string first, string second) =>
        edge.FirstLocationId == first && edge.SecondLocationId == second || edge.FirstLocationId == second && edge.SecondLocationId == first;
}
