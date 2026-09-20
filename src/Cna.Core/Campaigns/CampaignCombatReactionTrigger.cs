using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
namespace Cna.Core.Campaigns;

/// <summary>Creation-rooted single-opportunity Reaction trigger; no participant authority is admitted.</summary>
internal static class CampaignCombatReactionTrigger
{
    public static CampaignCombatReactionTriggerState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> movementEvents, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(movementEvents); ArgumentNullException.ThrowIfNull(events);
        if (movementEvents.Count != 1 || events.Count > 1) throw new JsonException("Reaction trigger requires one Movement predecessor and at most one trigger.");
        var predecessor = CampaignCombatInheritedMovement.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, movementEvents);
        var state = Initial(predecessor);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized Reaction trigger.");
            var bytes = retained.ToArray();
            state = Emit(predecessor, CampaignCombatInheritedMovementCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatReactionTriggerCodec.SerializeEvent(state.Trigger!)))
                throw new JsonException("Reaction trigger differs from actual history-derived effects.");
        }
        return state;
    }
    public static CampaignCombatReactionTriggerResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> movementEvents, IReadOnlyList<byte[]> events, CampaignCombatInheritedMovementInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, movementEvents, events);
        Authorize(state.Predecessor, input);
        if (state.Trigger is not null)
        {
            if (input != state.Trigger.Input) throw new JsonException("Conflicting Reaction trigger retry.");
            return new(state, CampaignCombatReactionTriggerCodec.SerializeEvent(state.Trigger), true);
        }
        var after = Emit(state.Predecessor, input);
        return new(after, CampaignCombatReactionTriggerCodec.SerializeEvent(after.Trigger!), false);
    }
    public static CampaignCombatReactionTriggerState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> movementEvents, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576 || events.Count != 1) throw new JsonException("Reaction readback requires a retained trigger and bounded state bytes.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, movementEvents, events);
        if (!bytes.SequenceEqual(CampaignCombatReactionTriggerCodec.SerializeState(state))) throw new JsonException("Reaction cache differs from actual retained history.");
        return state;
    }
    public static CampaignCombatInheritedMovementInput Command(CampaignCombatReactionTriggerState state)
    {
        if (state.Trigger is not null) throw new JsonException("Reaction trigger is already committed.");
        return CampaignCombatInheritedMovement.Command(state.Predecessor, state.Predecessor.Tracks[0].Route[0]);
    }
    private static CampaignCombatReactionTriggerState Initial(CampaignCombatInheritedMovementState state)
    {
        var side = state.Opening.Cycle!.ActingSide == LandSide.Axis ? "axis" : "commonwealth";
        var assault = side == "axis" ? "assault-west" : "assault-east";
        var element = state.World.Elements.Single(e => e.ElementId == state.Members[0].Unit.ElementId);
        if (state.StateVersion != 12 || state.Events.Count != 1 || state.Receipts.Count != 11 ||
            element.ElementId != side + "-assault-battalion" || element.CurrentLocationId != side + "-rear" ||
            element.OperationalState.CapabilityPointsExpended != new CapabilityPointAmount(2, 1) || element.OperationalState.CohesionLevel != 0 ||
            state.Tracks.Count != 1 || !state.Tracks[0].Route.SequenceEqual([assault, side + "-rear"]) ||
            state.BreakdownFlow is null || state.BreakdownFlow.Route.OriginLocationId != assault ||
            state.BreakdownFlow.Route.CurrentLocationId != side + "-rear" || state.BreakdownFlow.Route.FirstMoveStateVersion != 12 ||
            state.BreakdownFlow.Route.Owner != state.Opening.Cycle.ActingSide || state.BreakdownFlow.Route.CohortIds.Count != 0)
            throw new JsonException("Reaction requires actual first assault-to-rear CP2 Movement prefix.");
        return new(state, state.World, state.Members, state.Receipts, state.Tracks, state.ActualProgressRefs, null, state.Prefix);
    }
    private static void Authorize(CampaignCombatInheritedMovementState state, CampaignCombatInheritedMovementInput input)
    {
        _ = CampaignCombatInheritedMovementCodec.SerializeInput(input);
        var expected = CampaignCombatInheritedMovement.Command(state, state.Tracks[0].Route[0]);
        if (input != expected) throw new JsonException("Reaction trigger actor, identity, occurrence or route differs from actual predecessor.");
    }
    private static CampaignCombatReactionTriggerState Emit(CampaignCombatInheritedMovementState state, CampaignCombatInheritedMovementInput input)
    {
        Authorize(state, input);
        var cmd = input.Command;
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
        // Discover opposing combat adjacency before evaluating eligibility.
        var adjacent = world.Representations.Where(r => r.BoundElementIds.Count > 0 &&
            r.BoundElementIds.All(id => pack.Elements.Single(e => e.ElementId == id).SideId != cmd.Unit.OriginalSide) &&
            pack.Edges.Any(e => Connects(e, r.CurrentLocationId, cmd.DestinationLocationId))).ToArray();
        if (adjacent.Length != 1) throw new JsonException("Trigger requires exactly one opposing adjacent representation.");
        var opponent = adjacent[0];
        var opponentId = cmd.Unit.OriginalSide == "axis" ? "commonwealth-assault-battalion" : "axis-assault-battalion";
        var opponentAssault = cmd.Unit.OriginalSide == "axis" ? "assault-east" : "assault-west";
        if (opponent.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement || !opponent.BoundElementIds.SequenceEqual([opponentId]) ||
            opponent.CurrentLocationId != opponentAssault) throw new JsonException("Unexpected adjacent trigger profile.");
        var opponentElement = world.Elements.Single(e => e.ElementId == opponentId);
        var opponentFacts = pack.Elements.Single(e => e.ElementId == opponentId);
        if (opponentFacts.CombatClassificationId != "land.combat-classification.combat-unit" || opponentFacts.PlacementMode != ContentPlacementMode.Independent ||
            opponentElement.ReserveStatus != CampaignElementReserveStatus.None || opponentElement.OperationalState.CohesionLevel <= -26 ||
            opponentElement.OperationalState.MovementEnded is not null) throw new JsonException("Adjacent opponent is ineligible.");
        var terrain = Cna1979Movement.LookupTerrain(destination.TerrainId, facts.MobilityId);
        if (!terrain.IsSupported || terrain.Value.Cost != new CapabilityPointAmount(2, 1))
            throw new JsonException("Unexpected Clear terrain cost.");
        var route = state.BreakdownFlow!.Route.AtLocation(cmd.DestinationLocationId);
        var owner = state.Opening.Cycle!.ActingSide;
        var reactingSide = owner == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var committedRepresentation = new CampaignMapRepresentationState(representation.RepresentationId, cmd.DestinationLocationId,
            representation.BindingKind, representation.BoundElementIds);
        var windowId = CampaignReactionIdentity.CreateWindow(state.Opening.Cycle.CampaignId, state.Opening.Cycle.RulesetHash, 4,
            checked(state.StateVersion + 1), committedRepresentation, cmd.OriginLocationId, cmd.DestinationLocationId, reactingSide);
        var opportunity = new CampaignFrozenReactionOpportunity(CampaignReactionIdentity.CreateOpportunity(windowId, opponent), opponent,
            new(opponent.CurrentLocationId, cmd.DestinationLocationId, true, [new RuleReference("spi-1979-land-rules", "8.51")]));
        var window = new CampaignCombatReactionWindow(windowId, checked(state.StateVersion + 1), owner, reactingSide,
            new(state.SequencePosition, owner, reactingSide), new(4, element.ElementId, committedRepresentation, cmd.OriginLocationId, cmd.DestinationLocationId),
            new(representation.RepresentationId, cmd.OriginLocationId, cmd.DestinationLocationId), [opportunity]);
        var emitted = new CampaignCombatReactionTriggerEvent(state, input, representation.RepresentationId, terrain.Value.Cost, terrain.Sources,
            window, new(new CampaignPhasingContinuation.ResumeRoute(route), null));
        // Event excludes projected World: obtain its real receipt before attaching causal DP.
        var bytes = CampaignCombatReactionTriggerCodec.SerializeEvent(emitted);
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
        return new(state, projected, [state.Members[0] with { SpentCp = spent.State.CapabilityPointsExpended }], state.Receipts.Append(receipt),
            [new(cmd.Unit, locations)], state.ActualProgressRefs.Append(new("element-moved", receipt.ReceiptId, receipt.EventHash)),
            emitted, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes));
    }
    internal static CampaignWorldSnapshotV7 ExpectedWorld(CampaignCombatReactionTriggerState state)
    {
        _ = Initial(state.Predecessor);
        if (state.Predecessor.World != CampaignCombatInheritedMovement.ExpectedWorld(state.Predecessor))
            throw new JsonException("Reaction predecessor World differs from retained Movement.");
        return state.Trigger is null ? state.Predecessor.World : Emit(state.Predecessor, state.Trigger.Input).World;
    }
    private static bool Connects(ContentHexEdge edge, string first, string second) =>
        edge.FirstLocationId == first && edge.SecondLocationId == second || edge.FirstLocationId == second && edge.SecondLocationId == first;
}
