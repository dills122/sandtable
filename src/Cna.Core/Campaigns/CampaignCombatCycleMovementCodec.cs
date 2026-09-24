using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatCycleMovementCodec
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    internal static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    internal static string Actor(CampaignOpeningPreambleActor actor) => CampaignCombatSealedRoundCodec.Actor(actor);
    internal static JsonNode Value<T>(T value) => JsonSerializer.SerializeToNode(value, Options)!;
    public static byte[] SerializeCommand(CycleMoveCommand command)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(command.Unit);
        foreach (var id in new[] { command.Kind, command.PositionId, command.OriginLocationId, command.DestinationLocationId })
            ContentContractGuards.RequireStableId(id, nameof(command));
        if (command.CycleId is null || command.CycleId.Length != 71 || !command.CycleId.StartsWith("sha256:", StringComparison.Ordinal) ||
            command.CycleId[7..].Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new JsonException("Invalid Movement cycle hash.");
        return Encode(Value(command));
    }
    internal static JsonNode Input(CycleMoveInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new JsonObject { ["command"] = JsonNode.Parse(SerializeCommand(input.Command)), ["actor"] = Actor(input.Actor) };
    }
    public static byte[] SerializeInput(CycleMoveInput input) => Encode(Input(input));
    private static JsonNode Result(CycleMoveBasis basis) => JsonNode.Parse(CampaignCombatResolutionCodec.SerializeState(basis.Result))!;
    private static JsonNode Written(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream(); using (var writer = new Utf8JsonWriter(stream)) write(writer);
        return JsonNode.Parse(stream.ToArray())!;
    }
    public static byte[] SerializeBase(CycleMoveBasis basis)
    {
        var result = Result(basis);
        var position = Written(w => { w.WriteStartObject(); CampaignV11CanonicalCodec.WritePosition(w, "position", basis.Position); w.WriteEndObject(); })["position"]!.DeepClone();
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["profile"] = "isolated-next-movement",
            ["settledStateHash"] = Hash(CampaignCombatResolutionCodec.SerializeState(basis.Result)),
            ["settlementCaReceiptId"] = basis.Result.CaCompletionReceiptId,
            ["cycle"] = Written(w => CampaignCombatReserveCompletionCodec.WriteCycle(w, basis.Cycle)),
            ["firstActingSide"] = CampaignSnapshotSerializer.FormatSide(basis.Result.Context.Committed.Base.Boundary.FirstActingSide),
            ["position"] = position,
            ["stateVersion"] = basis.Cycle.OpenedAuthorityVersion,
            ["prefix"] = basis.Cycle.OpeningPrefix,
            ["world"] = result["world"]!.DeepClone(),
            ["randomState"] = result["randomState"]!.DeepClone(),
            ["attackHistory"] = Value(basis.Result.Context.Committed.AttackHistory)
        });
    }
    internal static JsonNode World(CycleMoveState state)
    {
        var world = Result(state.Basis)["world"]!.DeepClone();
        foreach (var item in world["elements"]!.AsArray())
        {
            var e = state.World.Elements.Single(e => e.ElementId == item!["elementId"]!.GetValue<string>());
            item!["currentLocationId"] = e.CurrentLocationId;
            item["operationalState"]!["capabilityPointsExpended"] = Value(e.OperationalState.CapabilityPointsExpended);
            item["operationalState"]!["cohesionLevel"] = e.OperationalState.CohesionLevel;
        }
        foreach (var item in world["representations"]!.AsArray())
            item!["currentLocationId"] = state.World.Representations.Single(r => r.RepresentationId == item["representationId"]!.GetValue<string>()).CurrentLocationId;
        world["relationships"] = Value(state.World.Relationships);
        world["cohesionCauses"] = Value(state.World.CohesionCauses);
        return world;
    }
    public static byte[] SerializeState(CycleMoveState state) => Encode(new JsonObject
    {
        ["contractVersion"] = 1,
        ["baseHash"] = state.BaseHash,
        ["stateVersion"] = state.StateVersion,
        ["prefix"] = state.Prefix,
        ["world"] = World(state),
        ["randomState"] = Result(state.Basis)["randomState"]!.DeepClone(),
        ["attackHistory"] = Value(state.Basis.Result.Context.Committed.AttackHistory),
        ["tracks"] = Value(state.Tracks),
        ["receipts"] = new JsonArray(state.Receipts.Select(r => (JsonNode)new JsonObject
        {
            ["commandHash"] = r.CommandHash,
            ["eventHash"] = r.EventHash,
            ["receiptId"] = r.ReceiptId,
            ["actor"] = Actor(r.Actor),
            ["stateVersion"] = r.StateVersion
        }).ToArray())
    });
    internal static byte[] Event(CycleMoveState prior, CycleMoveInput input, JsonNode effect, string? receipt)
    {
        var c = prior.Basis.Cycle;
        var node = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = "combat-cycle-element-moved",
            ["author"] = Actor(input.Actor),
            ["campaignId"] = c.CampaignId,
            ["rulesetHash"] = c.RulesetHash,
            ["configurationHash"] = c.AdmittedPolicyBundleDigest,
            ["cycleId"] = CampaignCombatReserveCompletionCodec.CycleId(c),
            ["positionId"] = prior.Basis.Position.PositionId,
            ["baseHash"] = prior.BaseHash,
            ["priorVersion"] = prior.StateVersion,
            ["stateVersion"] = checked(prior.StateVersion + 1),
            ["priorPrefix"] = prior.Prefix,
            ["input"] = Input(input),
            ["effect"] = effect.DeepClone()
        };
        if (receipt is not null) node["receiptId"] = receipt;
        return Encode(node);
    }
    public static CycleMoveBasis ReadBase(ReadOnlySpan<byte> bytes, CombatResultReleaseSource source)
    {
        Check(bytes); var basis = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], []).Basis;
        if (!bytes.SequenceEqual(SerializeBase(basis))) throw new JsonException("Movement base differs from replayed Result2 source.");
        return basis;
    }
    public static CycleMoveState ReadState(ReadOnlySpan<byte> bytes, CombatResultReleaseSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        Check(bytes); var state = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, inputs, events);
        if (!bytes.SequenceEqual(SerializeState(state))) throw new JsonException("Movement state differs from replay.");
        return state;
    }
    internal static byte[] Encode(JsonNode node)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(node, Options); Check(bytes); return bytes;
    }
    internal static void Check(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Movement record size exceeds bounds.");
        foreach (var b in bytes) if (b <= 32 || b >= 127 || b == 92) throw new JsonException("Alternate Movement bytes.");
        using var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        Walk(doc.RootElement);
        static void Walk(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Array)
            {
                if (e.GetArrayLength() > 512) throw new JsonException("Movement array capacity exceeded.");
                foreach (var child in e.EnumerateArray()) Walk(child);
            }
            if (e.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in e.EnumerateObject()) { if (!names.Add(p.Name)) throw new JsonException("Duplicate Movement key."); Walk(p.Value); }
            }
        }
    }
}
