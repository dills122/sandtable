using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Dormant, first-side original-infantry none-to-I designation. Completion is a later gate.</summary>
internal static class CampaignCombatReserveDesignation
{
    internal static readonly System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> Sources = Array.AsReadOnly<RuleReference>(
        [new("spi-1979-land-rules", "18.11"), new("spi-1979-land-rules", "18.12"), new("spi-1979-land-rules", "18.15")]);

    public static CampaignCombatReserveState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(stageEvents);
        ArgumentNullException.ThrowIfNull(events);
        if (stageEvents.Count != 4 || events.Count > 1)
            throw new JsonException("Reserve designation requires four stage entries and at most one designation.");
        var stage = CampaignCombatStageEntry.Replay(request, createdBytes, preamble, weatherEvents, stageEvents);
        var state = Initial(stage);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized Reserve event.");
            var bytes = retained.ToArray();
            var (after, emitted) = Emit(state, CampaignCombatReserveCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatReserveCodec.SerializeEvent(emitted)))
                throw new JsonException("Reserve event differs from history-derived canonical authority.");
            state = after;
        }
        return state;
    }

    public static CampaignCombatReserveResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> events, CampaignCombatReserveInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.ExpectedPriorVersion != input.Command.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting Reserve designation retry.");
            return new(state, CampaignCombatReserveCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new(after, CampaignCombatReserveCodec.SerializeEvent(emitted), false);
    }

    public static CampaignCombatReserveState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Reserve state.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, events);
        if (!bytes.SequenceEqual(CampaignCombatReserveCodec.SerializeState(state)))
            throw new JsonException("Reserve projection differs from separately trusted creation and complete history.");
        return state;
    }

    public static CampaignCombatReserveInput Command(CampaignCombatReserveState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.StateVersion != 10 || state.Members.Count != 1 || state.Members[0].Status != CampaignElementReserveStatus.None)
            throw new JsonException("No further designation at this cut.");
        var receipt = state.Stage.Weather.Opening.Creation.CreationReceipt;
        return new(new(2, "designate-reserve-element", receipt.CreationBinding, receipt.CreationEventHash,
            10, state.SequencePosition.PositionId, state.Members[0].Unit.ElementId),
            state.FirstActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);
    }

    private static CampaignCombatReserveState Initial(CampaignCombatStageEntryState stage)
    {
        if (stage.StateVersion != 10 || stage.Receipts.Count != 9 ||
            stage.SequencePosition.PositionId != "land.position.operation-1.first-player.reserve-designation" ||
            stage.SequencePosition.ActiveSide is not null || stage.SequencePosition.ActorRole != LandActorRole.FirstActingSide)
            throw new JsonException("Reserve requires complete unmaterialized first-side entry.");
        var creation = stage.Weather.Opening.Creation;
        var side = stage.Weather.Opening.Orders[0].FirstSide;
        var sideId = CampaignSnapshotSerializer.FormatSide(side);
        var scope = new CampaignCombatReserveScope(1, 1, "first-acting-side", side);
        var own = creation.Setup.Artifact.Definition.Elements.Where(e => e.SideId == sideId).ToArray();
        if (own.Length != 1) throw new JsonException("Selected Reserve profile requires one original own infantry element.");
        var element = creation.World.Elements.Single(e => e.ElementId == own[0].ElementId);
        if (element.ReserveStatus != CampaignElementReserveStatus.None ||
            element.OperationalState.CapabilityPointsExpended != CapabilityPointAmount.Zero || own[0].BaseCapabilityPointAllowance != 10)
            throw new JsonException("Reserve requires untouched original member resources.");
        var member = new CampaignCombatReserveMember(new(creation.World.CreationBinding, sideId, element.ElementId),
            CampaignElementReserveStatus.None, own[0].BaseCapabilityPointAllowance,
            element.OperationalState.CapabilityPointsExpended, new(scope, null));
        return new(stage, 10, stage.Prefix, side, creation.World, [member], stage.Receipts, []);
    }

    private static void Authorize(CampaignCombatReserveState state, CampaignCombatReserveInput input)
    {
        _ = CampaignCombatReserveCodec.SerializeInput(input);
        var actor = state.FirstActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
        if (input.Actor != actor) throw new JsonException("Only the trusted first acting side may designate its Reserve member.");
    }

    private static (CampaignCombatReserveState State, CampaignCombatReserveEvent Event) Emit(CampaignCombatReserveState state,
        CampaignCombatReserveInput input)
    {
        Authorize(state, input);
        if (input != Command(state)) throw new JsonException("Designation does not match current owner, member, or causal occurrence.");
        var emitted = new CampaignCombatReserveEvent(state, input);
        var bytes = CampaignCombatReserveCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        var member = state.Members[0] with
        {
            Status = CampaignElementReserveStatus.ReserveI,
            History = state.Members[0].History with { DesignationReceiptId = emitted.ReceiptId }
        };
        return (new(state.Stage, 11, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), state.FirstActingSide,
            DesignatedWorld(state.World, member.Unit.ElementId), [member], state.Receipts.Append(receipt), [emitted]), emitted);
    }

    internal static CampaignWorldSnapshotV7 ExpectedWorld(CampaignCombatReserveState state)
    {
        var initial = Initial(state.Stage);
        if (state.StateVersion == 10 && state.Events.Count == 0) return initial.World;
        if (state.StateVersion == 11 && state.Events.Count == 1 && state.Events[0].Input == Command(initial))
            return DesignatedWorld(initial.World, initial.Members[0].Unit.ElementId);
        throw new JsonException("World is outside the derived precompletion Reserve profile.");
    }

    private static CampaignWorldSnapshotV7 DesignatedWorld(CampaignWorldSnapshotV7 world, string elementId) => new(7,
        world.CreationBinding, world.Elements.Select(element => element.ElementId == elementId
            ? new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, CampaignElementReserveStatus.ReserveI,
                element.OperationalState, element.Components, element.SourceParentFormationId, element.CurrentParentFormationId,
                element.Ammunition, element.Readiness) : element), world.Representations, world.BrokenVehicleLots,
        world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements,
        world.FutureObligations, world.Settlements);
}
