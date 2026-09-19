using System.Text.Json;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

/// <summary>Four dormant explicit-none stage gates, ending at unmaterialized Reserve entry.</summary>
internal static class CampaignCombatStageEntry
{
    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<CampaignCombatStageEntryEdge> Edges = Array.AsReadOnly<CampaignCombatStageEntryEdge>(
    [
        new("resolve-no-obligation-organization", "no-obligation-organization-resolved", NoObligationOrganizationResolved.RequiredSources),
        new("resolve-no-obligation-naval-convoy-arrival", "no-obligation-naval-convoy-arrival-resolved", NoObligationNavalConvoyArrivalResolved.RequiredSources),
        new("resolve-no-obligation-fleet-assignment", "no-obligation-fleet-assignment-resolved", NoObligationFleetAssignmentResolved.RequiredSources),
        new("resolve-no-obligation-fleet-repair", "no-obligation-fleet-repair-resolved", NoObligationFleetRepairResolved.RequiredSources),
    ]);

    public static CampaignCombatStageEntryState Replay(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(weatherEvents);
        ArgumentNullException.ThrowIfNull(events);
        if (weatherEvents.Count != 1 || events.Count > 4)
            throw new JsonException("Stage entry requires one Weather event and at most four stage events.");
        // Check all policy gates before predecessor replay, never infer absence from World contents.
        RequirePolicy(request.Context.Setup.StageEntry);
        var weather = CampaignCombatWeather.Replay(request, createdBytes, preamble, weatherEvents);
        if (weather.StateVersion != 6 || weather.SequencePosition != Position(5) ||
            weather.Receipts.Count != 5 || weather.Weather.Count != 1)
            throw new JsonException("Stage entry requires complete Organization-entry authority.");
        var state = new CampaignCombatStageEntryState(weather, 6, weather.Prefix,
            weather.SequencePosition, weather.Receipts, []);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 })
                throw new JsonException("Stage entry event is missing or exceeds byte limit.");
            var bytes = retained.ToArray();
            var input = CampaignCombatStageEntryCodec.ReadEventInput(bytes);
            var (after, emitted) = Emit(state, input);
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatStageEntryCodec.SerializeEvent(emitted)))
                throw new JsonException("Stage entry event differs from canonical source and history-derived authority.");
            state = after;
        }
        return state;
    }

    public static CampaignCombatStageEntryResult Apply(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> events, CampaignCombatStageEntryInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, events);
        Authorize(input); // Actor, kind and version must pass even for a retained receipt.
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.ExpectedPriorVersion != input.Command.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting stage entry command retry.");
            return new CampaignCombatStageEntryResult(state, CampaignCombatStageEntryCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new CampaignCombatStageEntryResult(after, CampaignCombatStageEntryCodec.SerializeEvent(emitted), false);
    }

    public static CampaignCombatStageEntryState ReadState(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length > 1_048_576) throw new JsonException("Stage entry state exceeds byte limit.");
        var expected = Replay(request, createdBytes, preamble, weatherEvents, events);
        if (!bytes.SequenceEqual(CampaignCombatStageEntryCodec.SerializeState(expected)))
            throw new JsonException("Stage entry projection differs from separately supplied complete history.");
        return expected;
    }

    public static CampaignCombatStageEntryInput Command(CampaignCombatStageEntryState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.StateVersion is < 6 or > 9) throw new JsonException("No stage entry successor at this cut.");
        var receipt = state.Weather.Opening.Creation.CreationReceipt;
        return new CampaignCombatStageEntryInput(new CampaignCombatStageEntryCommand(2,
            Edges[(int)state.StateVersion - 6].Command, receipt.CreationBinding, receipt.CreationEventHash,
            state.StateVersion, state.SequencePosition.PositionId), CampaignOpeningPreambleActor.System);
    }

    internal static CampaignCombatStageEntryEdge Edge(string command) =>
        Edges.SingleOrDefault(edge => edge.Command == command) ?? throw new JsonException("Unsupported stage entry command kind.");

    internal static void RequirePolicy(CampaignStageEntryPolicy? policy)
    {
        if (policy is null || policy.ContractVersion != 1 || policy.GameTurn != 1 || policy.OperationStage != 1 ||
            policy.Organization != StageEntryObligationKind.ExplicitNone ||
            policy.NavalConvoyArrival != StageEntryObligationKind.ExplicitNone ||
            policy.FleetAssignment != StageEntryObligationKind.ExplicitNone ||
            policy.FleetRepair != StageEntryObligationKind.ExplicitNone ||
            !policy.Sources.SequenceEqual([CampaignStageEntryPolicy.SourceReference]))
            throw new JsonException("Stage entry requires the complete frozen explicit-none policy.");
    }

    private static void Authorize(CampaignCombatStageEntryInput input)
    {
        _ = CampaignCombatStageEntryCodec.SerializeInput(input);
        if (input.Actor != CampaignOpeningPreambleActor.System)
            throw new JsonException("Only trusted System actor may resolve stage entry.");
    }

    private static (CampaignCombatStageEntryState State, CampaignCombatStageEntryEvent Event) Emit(
        CampaignCombatStageEntryState state, CampaignCombatStageEntryInput input)
    {
        Authorize(input);
        RequirePolicy(state.Weather.Opening.Creation.Setup.StageEntry);
        if (state.StateVersion is < 6 or > 9 || state.SequencePosition != Position((int)state.StateVersion - 1) ||
            input != Command(state))
            throw new JsonException("Stage entry command does not match the current causal occurrence.");
        var emitted = new CampaignCombatStageEntryEvent(state, input, Position((int)state.StateVersion), Edge(input.Command.Kind).Sources);
        var bytes = CampaignCombatStageEntryCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(
            CampaignOpeningPreambleCodec.Hash(CampaignCombatStageEntryCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        return (new CampaignCombatStageEntryState(state.Weather, emitted.StateVersion,
            CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), emitted.SequencePosition,
            state.Receipts.Append(receipt), state.Events.Append(emitted)), emitted);
    }

    private static LandSequencePosition Position(int index)
    {
        // Catalog5 retains these stage entries; preserve Commonwealth fleet actors and null Reserve actor.
        var position = Cna1979LandSequence.CreateTurn(1)[index];
        return new LandSequencePosition(5, position.PositionId, position.GameTurn, position.OperationStage,
            position.StageId, position.PhaseId, position.SegmentId, position.StepId,
            position.ActorRole, position.ActiveSide, position.Sources);
    }
}
