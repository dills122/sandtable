using System.Text.Json;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

/// <summary>One dormant Weather successor, validated from complete creation and opening history.</summary>
internal static class CampaignCombatWeather
{
    public static CampaignCombatWeatherState Replay(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(preamble);
        ArgumentNullException.ThrowIfNull(events);
        if (preamble.Count != 4 || events.Count > 1)
            throw new JsonException("Weather requires four opening events and zero or one Weather event.");
        var opening = CampaignOpeningPreamble.Replay(request, createdBytes, preamble);
        RequireWeatherAuthority(opening);
        var state = new CampaignCombatWeatherState(opening, 5, opening.Prefix,
            opening.SequencePosition, opening.Creation.RandomState, [], opening.Receipts, null);
        if (events.Count == 0) return state;
        if (events[0] is not { Length: > 0 and <= 1_048_576 } retained)
            throw new JsonException("Weather event is missing or exceeds byte limit.");
        var bytes = retained.ToArray();
        var input = CampaignCombatWeatherCodec.ReadEventInput(bytes);
        var (after, emitted) = Emit(state, input);
        if (!bytes.AsSpan().SequenceEqual(CampaignCombatWeatherCodec.SerializeEvent(emitted)))
            throw new JsonException("Weather event differs from RNG and history-derived canonical authority.");
        return after;
    }

    public static CampaignCombatWeatherResult Apply(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> events,
        CampaignCombatWeatherInput input)
    {
        var state = Replay(request, createdBytes, preamble, events);
        Authorize(input); // Exact retries still require trusted System admission.
        if (state.AcceptedEvent is { } accepted)
        {
            if (input != accepted.Input) throw new JsonException("Conflicting Weather command retry.");
            return new CampaignCombatWeatherResult(state, CampaignCombatWeatherCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new CampaignCombatWeatherResult(after, CampaignCombatWeatherCodec.SerializeEvent(emitted), false);
    }

    public static CampaignCombatWeatherState ReadState(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length > 1_048_576) throw new JsonException("Weather state exceeds byte limit.");
        var state = Replay(request, createdBytes, preamble, events);
        if (!bytes.SequenceEqual(CampaignCombatWeatherCodec.SerializeState(state)))
            throw new JsonException("Weather projection differs from separately supplied history.");
        return state;
    }

    public static CampaignCombatWeatherInput Command(CampaignCombatWeatherState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.StateVersion != 5) throw new JsonException("No Weather successor at this cut.");
        var receipt = state.Opening.Creation.CreationReceipt;
        return new CampaignCombatWeatherInput(new CampaignCombatWeatherCommand(2, "resolve-weather",
            receipt.CreationBinding, receipt.CreationEventHash, 5, state.SequencePosition.PositionId),
            CampaignOpeningPreambleActor.System);
    }

    private static void Authorize(CampaignCombatWeatherInput input)
    {
        _ = CampaignCombatWeatherCodec.SerializeInput(input);
        if (input.Actor != CampaignOpeningPreambleActor.System)
            throw new JsonException("Only trusted System actor may resolve Weather.");
    }

    private static (CampaignCombatWeatherState State, CampaignCombatWeatherEvent Event) Emit(
        CampaignCombatWeatherState state, CampaignCombatWeatherInput input)
    {
        Authorize(input);
        if (state.StateVersion != 5 || state.SequencePosition != Position(4) ||
            state.Weather.Count != 0 || input != Command(state))
            throw new JsonException("Weather command does not match the unresolved causal occurrence.");
        var resolved = Cna1979Weather.Resolve(1, state.RandomState);
        var weather = new CampaignOperationStageWeather(1, 1, 1, state.Opening.InitiativeHolder!.Value,
            resolved.Season, resolved.FirstDie, resolved.SecondDie, resolved.Kind, resolved.Scope,
            resolved.LocationDie, resolved.AffectedAreas, 0, 0, 0);
        var emitted = new CampaignCombatWeatherEvent(state, input, weather, resolved.RandomState, Position(5),
            Array.AsReadOnly(WeatherEventFactory.GetSources(resolved.Kind).ToArray()));
        var bytes = CampaignCombatWeatherCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(
            CampaignOpeningPreambleCodec.Hash(CampaignCombatWeatherCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, 6);
        return (new CampaignCombatWeatherState(state.Opening, 6,
            CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), emitted.SequencePosition,
            resolved.RandomState, [weather], state.Receipts.Append(receipt), emitted), emitted);
    }

    private static void RequireWeatherAuthority(CampaignOpeningPreambleState opening)
    {
        var artifact = Cna1979Weather.CreateArtifact();
        var manifest = Cna1979CombatRuleset.Manifest.Artifacts.Single(value => value.ArtifactId == Cna1979Weather.ArtifactId);
        if (artifact.ContentHash != "sha256:10c92c736d61c6f88359b203d0a735df0d8a676b60e170b79b46053cfd223037" ||
            manifest.ContentHash != artifact.ContentHash || !manifest.Sources.SequenceEqual(artifact.Sources))
            throw new JsonException("Weather rules artifact or manifest sources differ from frozen authority.");
        var policy = opening.Creation.Setup.Weather;
        if (policy.ContractVersion != 1 || policy.Kind != CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects ||
            !policy.Sources.SequenceEqual([new RuleReference("sandtable-rules-lab", "weather.no-immediate-effect-subjects.v1")]))
            throw new JsonException("Weather requires explicit absence of immediate effect subjects.");
        if (opening.StateVersion != 5 || opening.SequencePosition != Position(4) ||
            opening.InitiativeHolder != LandSide.Axis || opening.Orders.Count != 1 ||
            opening.Orders[0].GameTurn != 1 || opening.Orders[0].OperationStage != 1)
            throw new JsonException("Weather requires the complete selected opening order and holder.");
    }

    private static LandSequencePosition Position(int index)
    {
        // Catalog5 preserves Weather/Organization's existing positions and sources.
        var position = Cna1979LandSequence.CreateTurn(1)[index];
        return new LandSequencePosition(5, position.PositionId, position.GameTurn, position.OperationStage,
            position.StageId, position.PhaseId, position.SegmentId, position.StepId,
            position.ActorRole, null, position.Sources);
    }
}
