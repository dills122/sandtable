using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Full retained-history admission for designation and its atomic first-cycle completion.</summary>
internal static class CampaignCombatReserveOpening
{
    public static CampaignCombatReserveOpeningState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> events)
    {
        var retained = CopyEvents(events);
        var designation = new List<byte[]>();
        byte[]? completion = null;
        foreach (var bytes in retained)
        {
            if (completion is not null) throw new JsonException("No event may follow first-cycle completion in this reader.");
            var kind = Kind(bytes);
            if (kind == "reserve-element-designated")
            {
                if (designation.Count != 0) throw new JsonException("Repeated Reserve designation.");
                designation.Add(bytes);
            }
            else completion = bytes;
        }
        if (completion is null)
            return new(CampaignCombatReserveDesignation.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, designation), null);
        var evidence = CampaignCombatReserveCompletion.ReadEvent(completion, request, createdBytes, preamble, weatherEvents, stageEvents, designation);
        return new(evidence.Predecessor, evidence.Event);
    }

    public static CampaignCombatReserveOpeningResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> events, CampaignCombatReserveInput input)
    {
        var retained = CopyEvents(events);
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, retained);
        _ = CampaignCombatReserveCodec.SerializeInput(input);
        Authorize(state, input.Actor);
        foreach (var accepted in state.Predecessor.Events)
        {
            if (accepted.Input.Command.ExpectedPriorVersion != input.Command.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting consumed Reserve designation occurrence.");
            return new(state, CampaignCombatReserveCodec.SerializeEvent(accepted), true);
        }
        if (state.Completion?.Input.Command.ExpectedPriorVersion == input.Command.ExpectedPriorVersion)
            throw new JsonException("Occurrence was consumed by completion, not designation.");
        if (state.Completion is not null) throw new JsonException("First-cycle opening is already complete.");
        var result = CampaignCombatReserveDesignation.Apply(request, createdBytes, preamble, weatherEvents, stageEvents, retained, input);
        return new(Replay(request, createdBytes, preamble, weatherEvents, stageEvents, retained.Append(result.EventBytes).ToArray()), result.EventBytes, false);
    }

    public static CampaignCombatReserveOpeningResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> events, CampaignCombatReserveCompletionInput input)
    {
        var retained = CopyEvents(events);
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, retained);
        _ = CampaignCombatReserveCompletionCodec.SerializeInput(input);
        Authorize(state, input.Actor);
        if (state.Predecessor.Events.Any(accepted => accepted.Input.Command.ExpectedPriorVersion == input.Command.ExpectedPriorVersion))
            throw new JsonException("Occurrence was consumed by designation, not completion.");
        if (state.Completion is { } completion && completion.Input.Command.ExpectedPriorVersion == input.Command.ExpectedPriorVersion)
        {
            if (completion.Input != input) throw new JsonException("Conflicting consumed Reserve completion occurrence.");
            return new(state, CampaignCombatReserveCompletionCodec.SerializeEvent(completion), true);
        }
        if (state.Completion is not null) throw new JsonException("First-cycle opening is already complete.");
        var result = CampaignCombatReserveCompletion.Create(request, createdBytes, preamble, weatherEvents, stageEvents, retained, input);
        var bytes = result.EventBytes;
        return new(Replay(request, createdBytes, preamble, weatherEvents, stageEvents, retained.Append(bytes).ToArray()), bytes, false);
    }

    public static CampaignCombatReserveOpeningState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized first-cycle projection.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, events);
        if (!bytes.SequenceEqual(CampaignCombatReserveOpeningCodec.SerializeState(state)))
            throw new JsonException("First-cycle projection differs from complete retained history.");
        return state;
    }

    private static void Authorize(CampaignCombatReserveOpeningState state, CampaignOpeningPreambleActor actor)
    {
        var expected = state.Predecessor.FirstActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
        if (actor != expected) throw new JsonException("Only trusted first acting side may submit this Reserve occurrence.");
    }
    private static byte[][] CopyEvents(IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count > 2) throw new JsonException("First opening admits at most two Reserve events.");
        return events.Select(bytes => bytes is { Length: > 0 and <= 1_048_576 }
            ? bytes.ToArray() : throw new JsonException("Missing or oversized Reserve event.")).ToArray();
    }
    private static string Kind(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 2)
                throw new JsonException("Unsupported Reserve event version.");
            return root.GetProperty("eventType").GetString() switch
            {
                "reserve-element-designated" => "reserve-element-designated",
                "reserve-designation-completed" => "reserve-designation-completed",
                _ => throw new JsonException("Unsupported Reserve event kind."),
            };
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid Reserve event.", error); }
    }
}
