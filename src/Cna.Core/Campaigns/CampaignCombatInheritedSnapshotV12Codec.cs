using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Literal Snapshot12 at a retained, causally replayed Initial H cut. No publication or host admission.</summary>
internal static class CampaignCombatInheritedSnapshotV12Codec
{
    public static byte[] Serialize(CampaignCombatCreationRequest request, ReadOnlySpan<byte> retainedCreated,
        IReadOnlyList<byte[]> events) => Compose(request, CampaignCombatHistoryReplay.Replay(request, retainedCreated, events));

    public static CampaignCombatHistoryResult Restore(ReadOnlySpan<byte> snapshotBytes,
        CampaignCombatCreationRequest request, byte[]? retainedCreated, IReadOnlyList<byte[]> events, bool admissionEnabled)
    {
        ValidateBounds(snapshotBytes);
        // Restore must never turn absent evidence into a fresh creation, even when admission is enabled.
        if (retainedCreated is not { Length: > 0 }) throw new JsonException("Restore requires independently retained Created11.");
        var history = CampaignCombatRetainedHistory.Capture(retainedCreated, events);
        var creation = CampaignCombatCreationCut.Decide(history.Created, request, admissionEnabled);
        if (creation.RequiresPublication) throw new JsonException("Restore cannot publish fresh creation.");
        var result = CampaignCombatHistoryReplay.Replay(request, creation.CreatedBytes, history.Events);
        if (!snapshotBytes.SequenceEqual(Compose(request, result)))
            throw new JsonException("Snapshot12 differs from canonical trusted retained history.");
        return result;
    }

    private static byte[] Compose(CampaignCombatCreationRequest request, CampaignCombatHistoryResult result)
    {
        // Both JSON trees originate exclusively from strict typed writers after causal replay.
        // No caller projection, family tag, JSON tree or expected-state cache enters this mapping.
        var root = JsonNode.Parse(CampaignCreationSnapshotV12Codec.Serialize(
            CampaignCreationSnapshotV12.Create(result.History.CreatedSpan, request)))!.AsObject();
        var state = JsonNode.Parse(SerializeState(result.Projection))!.AsObject();
        var events = result.History.Events;
        Require(state["configurationHash"]!.GetValue<string>() == Digest(System.Text.Encoding.UTF8.GetBytes(root["configuration"]!.ToJsonString())), "configuration binding");
        Require(JsonNode.DeepEquals(state["creationBinding"], root["creationReceipt"]!["creationBinding"]) &&
            state["creationEventHash"]!.GetValue<string>() == Digest(result.History.CreatedSpan), "creation binding");
        foreach (var field in new[] { "campaignId", "rulesetHash" }) Require(JsonNode.DeepEquals(root[field], state[field]), "identity");
        Require(result.Projection.StateVersion == events.Count + 1 && result.Projection.Receipts.Count == events.Count, "complete ledger");
        var prefix = root["chroniclePrefix"]!.GetValue<string>();
        var eventTypes = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < events.Count; index++)
        {
            var receipt = state["receipts"]![index]!;
            Require(receipt["eventHash"]!.GetValue<string>() == Digest(events[index]) &&
                receipt["stateVersion"]!.GetValue<long>() == index + 2, "receipt history");
            prefix = CampaignOpeningPreambleCodec.EventPrefix(prefix, events[index]);
            using var document = JsonDocument.Parse(events[index]);
            eventTypes.Add(document.RootElement.GetProperty("eventType").GetString()!);
        }
        Require(prefix == result.Projection.Prefix, "trusted history prefix");
        foreach (var field in new[] { "stateVersion", "world", "initiativeHolder", "operationStageOrders", "randomState" }) root[field] = state[field]?.DeepClone();
        if (state.ContainsKey("operationStageWeather")) root["operationStageWeather"] = state["operationStageWeather"]!.DeepClone();
        else Require(events.Count <= 4, "absent Weather before Weather only");
        var flow = state["breakdownFlow"];
        if (flow is null)
        {
            Require(Empty(state["tracks"]) && Empty(state["actualProgressRefs"]) && state["reactionWindow"] is null &&
                state["interruptContext"] is null && !eventTypes.Overlaps(["element-moved", "reacting-element-moved", "element-movement-stopped", "reaction-participant-completed"]), "idle proof");
            flow = new JsonObject { ["kind"] = "idle" };
        }
        root["breakdownFlow"] = flow.DeepClone();
        root["currentPosition"] = state.ContainsKey("currentPosition") ? state["currentPosition"]!.DeepClone() : new JsonObject
        {
            ["kind"] = flow["kind"]!.GetValue<string>() == "phasing-stop" ? "breakdown-stop" : "sequence",
            ["sequencePosition"] = state["sequencePosition"]!.DeepClone(),
        };
        root["reactionWindow"] = state["reactionWindow"]?.DeepClone();
        root["chroniclePrefix"] = prefix;
        root["commandReceipts"] = state["receipts"]!.DeepClone();
        if (state.ContainsKey("members"))
        {
            var arm = new JsonObject { ["kind"] = "reserve-designation", ["firstActingSide"] = state["firstActingSide"]!.DeepClone(), ["members"] = state["members"]!.DeepClone() };
            if (state["cycle"] is not null)
            {
                arm["kind"] = "inherited-cycle";
                foreach (var field in new[] { "cycle", "cycleId", "openingBaseHash", "completionReceiptId", "sequencePosition" }) arm[field] = state[field]!.DeepClone();
                foreach (var field in new[] { "tracks", "actualProgressRefs" })
                {
                    if (!state.ContainsKey(field)) Require(events.Count <= 11 &&
                        state["cycle"]!["openedAuthorityVersion"]!.GetValue<long>() == result.Projection.StateVersion, "absent movement evidence");
                    arm[field] = state[field]?.DeepClone() ?? new JsonArray();
                }
                foreach (var (field, eventType) in new[] { ("interruptContext", "element-movement-stopped"),
                    ("movementEnd", "movement-segment-completed"), ("breakdownCompletionReceiptId", "breakdown-segment-completed") })
                {
                    if (!state.ContainsKey(field)) Require(!eventTypes.Contains(eventType), "absent " + field + " proof");
                    arm[field] = state[field]?.DeepClone();
                }
            }
            root["cycleState"] = arm;
        }
        else Require(events.Count < 9, "pre-Reserve proof");
        var bytes = System.Text.Encoding.UTF8.GetBytes(root.ToJsonString());
        ValidateBounds(bytes);
        return bytes;
    }

    private static bool Empty(JsonNode? value) => value is null || value.AsArray().Count == 0;
    private static string Digest(ReadOnlySpan<byte> bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new JsonException("Inherited Snapshot12 violates " + reason + ".");
    }

    private static void ValidateBounds(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized inherited Snapshot12.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        Check(document.RootElement, 0, false, false);
        static void Check(JsonElement value, int depth, bool world, bool worldCauses)
        {
            if (depth > 32) throw new JsonException("Inherited Snapshot12 exceeds depth limit.");
            if (value.ValueKind == JsonValueKind.Array)
            {
                if (value.GetArrayLength() > (worldCauses ? 4096 : 512)) throw new JsonException("Inherited Snapshot12 exceeds array limit.");
                foreach (var item in value.EnumerateArray()) Check(item, depth + 1, false, false);
            }
            else if (value.ValueKind == JsonValueKind.Object)
                foreach (var property in value.EnumerateObject())
                    Check(property.Value, depth + 1, depth == 0 && property.NameEquals("world"), world && property.NameEquals("cohesionCauses"));
        }
    }

    private static byte[] SerializeState(CampaignCombatHistoryProjection projection) => projection switch
    {
        CampaignCombatHistoryProjection.Preamble p => CampaignOpeningPreambleCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.Weather p => CampaignCombatWeatherCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.StageEntry p => CampaignCombatStageEntryCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReserveOpening p => CampaignCombatReserveOpeningCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.Movement p => CampaignCombatInheritedMovementCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.MovementLifecycle p => CampaignCombatMovementLifecycleCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.BreakdownCompletion p => CampaignCombatBreakdownCompletionCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionTrigger p => CampaignCombatReactionTriggerCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionLifecycle p => CampaignCombatReactionLifecycleCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionClosure p => CampaignCombatReactionClosureCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionFallback p => CampaignCombatReactionFallbackCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionSecondMove p => CampaignCombatReactionSecondMoveCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionCompletion p => CampaignCombatReactionCompletionCodec.SerializeState(p.State),
        _ => throw new JsonException("Unsupported inherited Snapshot12 projection."),
    };
}
