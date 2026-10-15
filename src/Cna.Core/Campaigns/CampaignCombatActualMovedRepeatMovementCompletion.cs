using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Cna.Core.Campaigns.CampaignCombatActualMovedRepeatMovementCompletionCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedRepeatMovementCompletionResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    private readonly byte[][][] inputs;
    internal CombatActualMovedRepeatMovementCompletionResult(JsonObject packet, JsonObject basis, JsonObject state, byte[][][] ledgers, byte[]? ev = null, bool duplicate = false, string? receiptId = null)
    {
        source = Canonical(packet, "Source"); control = Canonical(state, "Control"); accepted = ev?.ToArray(); Duplicate = duplicate;
        ReceiptId = receiptId ?? (ev is null ? null : JsonNode.Parse(ev)!["receiptId"]!.GetValue<string>());
        inputs = ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var hash = Hash(source);
        proof = Canonical(new JsonObject { ["contractVersion"] = 1, ["sourceId"] = "mrmcomp." + hash[7..], ["sourceHash"] = hash, ["base"] = basis.DeepClone(), ["control"] = state.DeepClone() }, "Proof");
    }
    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => accepted?.ToArray();
    public bool Duplicate { get; }
    public string? ReceiptId { get; }
    public JsonElement Control => View(control);
    public JsonElement Proof => View(proof);
    private static JsonElement View(byte[] bytes) { using var document = JsonDocument.Parse(bytes); return document.RootElement.Clone(); }
    private byte[][] Ledger(int index) => inputs[index].Select(b => b.ToArray()).ToArray();
    public IReadOnlyList<byte[]> PrefixInputs => Ledger(0);
    public IReadOnlyList<byte[]> LifecycleInputs => Ledger(1);
    public IReadOnlyList<byte[]> BreakdownInputs => Ledger(2);
    public IReadOnlyList<byte[]> SelectionInputs => Ledger(3);
    public IReadOnlyList<byte[]> RoundInputs => Ledger(4);
    public IReadOnlyList<byte[]> PaidInputs => Ledger(5);
    public IReadOnlyList<byte[]> ReleaseInputs => Ledger(6);
    public IReadOnlyList<byte[]> ControlInputs => Ledger(7);
    public IReadOnlyList<byte[]> MovementInputs => Ledger(8);
    public IReadOnlyList<byte[]> CompletionInputs => Ledger(9);
}
internal static class CampaignCombatActualMovedRepeatMovementCompletion
{
    private sealed record Frame(JsonObject Packet, JsonObject Parent, JsonObject Base, string BaseHash, CampaignCombatCreationRequest Request, byte[][][] Ledgers, Func<string, byte[]> Dependencies, JsonObject Content, JsonObject Catalog, JsonObject InitialFlow, int EventCap);
    private const string SchemaPath = "docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json";
    public static CombatActualMovedRepeatMovementCompletionResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies)
    { var (f, state) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies); return new(f.Packet, f.Base, state, f.Ledgers); }
    public static CombatActualMovedRepeatMovementCompletionResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var current = Copy(currentInput, "Input"); var (f, state) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies);
        var (afterState, ev, rid) = Transition(f, state, Object(current, "Input"), admissionEnabled);
        if (ev is null) return new(f.Packet, f.Base, state, f.Ledgers, duplicate: true, receiptId: rid);
        var packet = f.Packet.DeepClone().AsObject(); packet["lifecycleEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var ledgers = f.Ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); ledgers[9] = [.. ledgers[9], current];
        // Callbacks may mutate original caller buffers. Post-append replay uses only this owned frame.
        var (after, rebuilt) = Replay(Canonical(packet, "Source"), f.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], f.Dependencies);
        Require(JsonNode.DeepEquals(afterState, rebuilt), 6); return new(after.Packet, after.Base, rebuilt, after.Ledgers, ev, receiptId: rid);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies) => ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies).SourceBytes;
    public static CombatActualMovedRepeatMovementCompletionResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Control"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies); _ = Object(owned, "Control"); Require(owned.AsSpan().SequenceEqual(result.ControlBytes), 6); return result; }
    public static CombatActualMovedRepeatMovementCompletionResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Proof"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies); _ = Object(owned, "Proof"); Require(owned.AsSpan().SequenceEqual(result.ProofBytes), 6); return result; }
    private static (Frame Frame, JsonObject State) Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies)
    {
        var packet = Object(source, "Source"); SourceBounds(packet);
        IReadOnlyList<byte[]>[] supplied = [prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion];
        Require(supplied.All(l => l is not null), 4);
        Require(prefix.Count == 13 && lifecycle.Count == 3 && breakdown.Count == 1 && selection.Count <= 16 && round.Count <= 16 && paid.Count <= 11 && release.Count == 2 && control.Count == 2 && movement.Count <= 6 && completion.Count <= 3, 4);
        var ledgers = supplied.Select((ledger, index) => Own(ledger, index == 9 ? packet["lifecycleEventCanonicalUtf8"]!.AsArray().Count : ledger.Count, index == 9 ? "Input" : "AncestorInput")).ToArray();
        Require(ledgers[8].Sum(b => b.Length) <= 24576 && ledgers[9].Sum(b => b.Length) <= 12288, 4);
        var retained = SnapshotDependencies(dependencies); var parentBytes = Encoding.ASCII.GetBytes(T(packet, "movementSourceCanonicalUtf8"));
        var parent = Inherited(() => CampaignCombatActualMovedRepeatMovement.ReplayTrustedSource(parentBytes, request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], retained));
        var p = JsonNode.Parse(parent.ControlBytes)!.AsObject(); var c = p["activeCycle"]!; var count = p["actualProgressRefs"]!.AsArray().Count;
        Require(T(p, "phase") == "movement" && p["publicationReceiptId"] is not null && N(c, "ordinal") == 2 && N(p["originalMovementEnd"]!, "ordinal") == 1 && N(p["closure"]!, "ordinal") == 1 && T(p["closure"]!, "outcome") == "repeated", 4);
        Require(ledgers[8].Length == p["movementReceipts"]!.AsArray().Count && ledgers[8].Length == count + 1 && count <= (T(p, "profile") == "paid" ? 2 : 5) && p["tracks"]!.AsArray().Count == (count > 0 ? 1 : 0) && p["targetUses"]!.AsArray().Count == 0, 4);
        var content = JsonNode.Parse(retained("docs/specs/fixtures/combat-content-v7.canonical.json"))!.AsObject();
        Require(p["members"]!.AsArray().All(m => T(m!, "status") == "none" && content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(m!["unit"]!, "elementId"))!["breakdownVehicleCohort"] is null), 3);
        EnsureTraceCapacity(N(p, "stateVersion"), count); var eventCap = count == 0 ? 1 : 3; var flow = O(("kind", "idle"));
        if (count > 0)
        {
            var track = p["tracks"]![0]!; var location = p["world"]!["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(track["unit"]!, "elementId"))!["currentLocationId"];
            Require(JsonNode.DeepEquals(track["unit"], p["members"]![0]!["unit"]) && track["route"]!.AsArray().Count == count + 1 && JsonNode.DeepEquals(track["route"]!.AsArray()[^1], location), 4);
            flow = O(("kind", "moving"), ("track", track), ("lastMoveReceiptId", p["movementReceipts"]!.AsArray()[^1]!["receiptId"]));
        }
        var basis = O(("contractVersion", 1), ("movementSourceHash", Hash(parentBytes)), ("movementControlHash", Hash(parent.ControlBytes)), ("movementProofHash", Hash(parent.ProofBytes)), ("dependencyManifestHash", ManifestHash), ("publicationReceiptId", p["publicationReceiptId"]), ("cycleId", p["cycleId"]), ("configurationHash", c["admittedPolicyBundleDigest"]));
        var catalog = JsonNode.Parse(JsonNode.Parse(retained("docs/specs/fixtures/combat-cycle-sequence-v1.json"))!["predecessor"]!["canonicalUtf8"]!.GetValue<string>())!.AsObject();
        foreach (var position in catalog["positions"]!.AsArray().Concat(catalog["interruptPositions"]!.AsArray())) position!["contractVersion"] = 5;
        var f = new Frame(packet, p, basis, "sha256:" + Domain("base", Canonical(basis, "Base")), request, ledgers, retained, content, catalog, flow, eventCap);
        Require(ledgers[9].Length <= eventCap, 4); var state = Initial(f);
        for (var i = 0; i < ledgers[9].Length; i++)
        {
            var (after, ev, _) = Transition(f, state, Object(ledgers[9][i], "Input"), true, replay: true);
            Require(ev is not null && ev.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes(packet["lifecycleEventCanonicalUtf8"]![i]!.GetValue<string>())), 6); state = after;
        }
        return (f, state);
    }
    internal static void EnsureTraceCapacity(long stateVersion, int moveCount) => Require(stateVersion >= 0 && moveCount is >= 0 and <= 5 && stateVersion <= long.MaxValue - (moveCount == 0 ? 1 : 3), 7);
    private static JsonObject Initial(Frame f)
    {
        var s = f.Parent.DeepClone().AsObject(); s["movementBaseHash"] = s["baseHash"]!.DeepClone(); s["baseHash"] = f.BaseHash; s["movementSourceHash"] = f.Base["movementSourceHash"]!.DeepClone(); s["flow"] = f.InitialFlow.DeepClone(); s["interruptContext"] = null; s["movementEnd"] = null; s["lifecycleReceipts"] = new JsonArray(); s["closed"] = false; return s;
    }
    private static JsonObject Position(Frame f, string kind)
    {
        if (kind == "movement") return f.Parent["sequencePosition"]!.DeepClone().AsObject();
        var id = kind == "stop" ? "land.position.breakdown-stop" : "land.position.operation-1.first-player.movement-and-combat.breakdown-determination";
        return f.Catalog["positions"]!.AsArray().Concat(f.Catalog["interruptPositions"]!.AsArray()).Single(p => T(p!, "positionId") == id)!.DeepClone().AsObject();
    }
    internal static JsonObject Command(JsonObject state, string kind)
    {
        var position = kind == "resolve-breakdown-stop" ? "land.position.breakdown-stop" : "land.position.operation-1.first-player.movement-and-combat.movement";
        return O(("command", O(("contractVersion", 1), ("kind", kind), ("baseHash", state["baseHash"]), ("cycleId", state["cycleId"]), ("expectedPositionId", position), ("expectedPriorVersion", state["stateVersion"]), ("capabilityId", Capability(state, kind)))), ("actor", kind == "resolve-breakdown-stop" ? JsonValue.Create("system") : state["activeCycle"]!["actingSide"]));
    }
    private static string Capability(JsonObject state, string kind) => "sha256:" + Domain("capability", Encode(O(("baseHash", state["baseHash"]), ("cycleId", state["cycleId"]), ("kind", kind), ("stateVersion", state["stateVersion"]), ("sequencePosition", JsonNode.Parse(Canonical(state["sequencePosition"]!, "Position"))), ("flow", JsonNode.Parse(Canonical(state["flow"]!, "Flow"))), ("interruptContext", state["interruptContext"] is null ? null : JsonNode.Parse(Canonical(state["interruptContext"]!, "Context"))))));
    internal static void CertifyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null)
    { var hs = handlers?.ToHashSet(StringComparer.Ordinal); var cs = capacities?.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal); var (f, _) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies); Certify(f, hs, cs); }
    private static void Certify(Frame f, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null)
    {
        var schema = JsonNode.Parse(f.Dependencies(SchemaPath))!; Require(B(schema, "closureFrozen"), 7);
        var required = schema["supportHandlers"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        Require(Handlers.ToHashSet(StringComparer.Ordinal).SetEquals(required) && (handlers ?? Handlers.ToHashSet(StringComparer.Ordinal)).IsSupersetOf(required), 7);
        foreach (var pair in schema["limits"]!.AsObject()) Require((capacities ?? Capacities).TryGetValue(pair.Key, out var available) && available >= pair.Value!.GetValue<int>(), 7);
    }
    private static readonly string[] Handlers = ["strict-r01-ten-ledger-replay", "both-paid-and-armed", "published-not-pending", "true-zero-and-five-move", "actual-track-stop", "full-canonical-position-context", "mandatory-empty-system-resolution", "owner-completion-actual-proof", "old-proof-exclusion-monotone", "world-resources-progress-retained", "owned-before-callback", "fresh-admission-only", "claims-full-replay", "capacity-closure"];
    private static readonly Dictionary<string, int> Capacities = new(StringComparer.Ordinal)
    {
        ["sourceBytes"] = 134217728,
        ["parentSourceBytes"] = 67108864,
        ["controlBytes"] = 1048576,
        ["proofBytes"] = 2097152,
        ["eventBytes"] = 65536,
        ["inputBytes"] = 4096,
        ["movementLedgerBytes"] = 24576,
        ["depth"] = 40,
        ["arrays"] = 512,
        ["objectFields"] = 128,
        ["movementEvents"] = 6,
        ["movementReceipts"] = 6,
        ["moves"] = 5,
        ["members"] = 1,
        ["tracks"] = 1,
        ["routeLocations"] = 6,
        ["newProgress"] = 5,
        ["priorProgress"] = 3,
        ["witnesses"] = 2,
        ["unsupportedWitnesses"] = 2,
        ["worldBytes"] = 1048576,
        ["newDpCauses"] = 5,
        ["lifecycleLedgerBytes"] = 12288,
        ["lifecycleEvents"] = 3,
        ["lifecycleReceipts"] = 3,
    };

    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static string T(JsonNode node, string key) => node[key]!.GetValue<string>();
    private static long N(JsonNode node, string key) => JsonSerializer.SerializeToElement(node[key]).GetInt64();
    private static bool B(JsonNode node, string key) => node[key]!.GetValue<bool>();
    private static string Domain(string kind, byte[] bytes) => HashWithDomain("sandtable.combat.actual-moved-repeat-movement-completion." + kind + ".v1", bytes)[7..];
    private static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame f, JsonObject prior, JsonObject input, bool admissionEnabled, bool replay = false)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input");
        var cmd = input["command"]!; var kind = T(cmd, "kind"); var actor = T(input, "actor"); var cycle = f.Parent["activeCycle"]!;
        var movement = Position(f, "movement"); var expected = Position(f, kind == "resolve-breakdown-stop" ? "stop" : "movement");
        Require(T(cmd, "baseHash") == T(prior, "baseHash") && T(prior, "baseHash") == f.BaseHash && T(cmd, "cycleId") == T(prior, "cycleId") && T(prior, "cycleId") == T(f.Parent, "cycleId") && JsonNode.DeepEquals(prior["activeCycle"], cycle) && T(prior, "movementSourceHash") == T(f.Base, "movementSourceHash") && T(cmd, "expectedPositionId") == T(expected, "positionId"), 4);
        Require(actor == (kind == "resolve-breakdown-stop" ? "system" : T(cycle, "actingSide")), 4);
        var commandHash = Hash(Canonical(cmd, "Command")); var duplicate = prior["lifecycleReceipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == commandHash && T(r!, "actor") == actor);
        if (duplicate is not null) return (prior.DeepClone().AsObject(), null, T(duplicate, "receiptId"));
        Require(!B(prior, "closed") && prior["lifecycleReceipts"]!.AsArray().Count < f.EventCap && N(prior, "stateVersion") < long.MaxValue, 7);
        Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion") && T(cmd, "expectedPositionId") == T(prior["sequencePosition"]!, "positionId"), 6);
        Require(T(cmd, "capabilityId") == Capability(prior, kind), 5);
        var state = prior.DeepClone().AsObject(); var flow = prior["flow"]!; var context = O(("cycle", cycle), ("cycleId", prior["cycleId"]), ("suspendedPosition", movement)); JsonObject effect;
        if (kind == "stop-element-movement")
        {
            Require(T(flow, "kind") == "moving" && prior["interruptContext"] is null && prior["movementEnd"] is null && JsonNode.DeepEquals(prior["sequencePosition"], movement), 6);
            Require(JsonNode.DeepEquals(flow, f.InitialFlow) && JsonNode.DeepEquals(flow["track"], prior["tracks"]![0]) && JsonNode.DeepEquals(flow["lastMoveReceiptId"], prior["movementReceipts"]!.AsArray()[^1]!["receiptId"]), 4);
            var descriptor = O(("recordedStateVersion", N(prior, "stateVersion") + 1), ("track", flow["track"]), ("lastMoveReceiptId", flow["lastMoveReceiptId"]), ("reason", "deliberate"), ("weatherKind", "normal"), ("cohortInputs", new JsonArray()));
            var stop = O(("stopId", "mrmstop." + Domain("stop", Encode(O(("baseHash", f.BaseHash), ("cycleId", prior["cycleId"]), ("descriptor", descriptor))))));
            foreach (var pair in descriptor) stop[pair.Key] = pair.Value?.DeepClone();
            state["flow"] = O(("kind", "phasing-stop"), ("stop", stop)); state["interruptContext"] = context.DeepClone(); state["sequencePosition"] = Position(f, "stop");
            effect = O(("kind", "movement-stopped"), ("stop", stop), ("interruptContext", context), ("successorPosition", state["sequencePosition"]));
        }
        else if (kind == "resolve-breakdown-stop")
        {
            Require(T(flow, "kind") == "phasing-stop" && JsonNode.DeepEquals(prior["interruptContext"], context) && JsonNode.DeepEquals(prior["sequencePosition"], Position(f, "stop")) && prior["movementEnd"] is null && T(f.InitialFlow, "kind") == "moving", 6);
            var stop = flow["stop"]!; Require(JsonNode.DeepEquals(stop["track"], f.InitialFlow["track"]) && JsonNode.DeepEquals(stop["lastMoveReceiptId"], f.InitialFlow["lastMoveReceiptId"]) && stop["cohortInputs"]!.AsArray().Count == 0 && N(stop, "recordedStateVersion") == N(prior, "stateVersion"), 4);
            state["flow"] = O(("kind", "idle")); state["interruptContext"] = null; state["sequencePosition"] = movement.DeepClone();
            effect = O(("kind", "stop-resolved"), ("stopId", stop["stopId"]), ("beforeRandomState", prior["randomState"]), ("afterRandomState", prior["randomState"]), ("checks", new JsonArray()), ("createdLots", new JsonArray()), ("sources", new JsonArray("spi-1979-land-rules:21.24-21.26")), ("successorPosition", movement));
        }
        else
        {
            Require(JsonNode.DeepEquals(flow, O(("kind", "idle"))) && prior["interruptContext"] is null && prior["movementEnd"] is null && JsonNode.DeepEquals(prior["sequencePosition"], movement), 6);
            if (f.EventCap == 3) Require(prior["lifecycleReceipts"]!.AsArray().Count == 2, 6);
            var preview = EndProof(f.Content, prior, "pending"); state["sequencePosition"] = Position(f, "breakdown"); state["phase"] = "breakdown"; state["closed"] = true;
            effect = O(("kind", "movement-completed"), ("endLocations", preview["endLocations"]), ("excludedUnits", preview["excludedBefore"]), ("progress", prior["actualProgressRefs"]), ("successorPosition", state["sequencePosition"]));
        }
        if (!replay) { Require(admissionEnabled, 7); Certify(f); }
        effect = JsonNode.Parse(Canonical(effect, "Effect"))!.AsObject();
        var ev = O(("contractVersion", 1), ("eventType", "combat-actual-moved-repeat-" + T(effect, "kind")), ("author", actor), ("campaignId", cycle["campaignId"]), ("rulesetHash", cycle["rulesetHash"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]), ("profile", prior["profile"]), ("movementSourceHash", prior["movementSourceHash"]), ("baseHash", f.BaseHash), ("cycleId", prior["cycleId"]), ("positionId", prior["sequencePosition"]!["positionId"]), ("priorVersion", prior["stateVersion"]), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", input), ("effect", effect));
        var rid = "mrmc." + Domain("receipt", Encode(ev)); ev["receiptId"] = rid; var bytes = Canonical(ev, "Event");
        if (kind == "complete-movement-segment") state["movementEnd"] = EndProof(f.Content, prior, rid);
        state["stateVersion"] = ev["stateVersion"]!.DeepClone(); state["prefix"] = EventPrefix(T(prior, "prefix"), bytes);
        var record = O(("commandHash", commandHash), ("eventHash", Hash(bytes)), ("receiptId", rid), ("actor", actor), ("stateVersion", state["stateVersion"]));
        state["receipts"]!.AsArray().Add(record.DeepClone()); state["lifecycleReceipts"]!.AsArray().Add(record);
        _ = Canonical(state, "Control"); return (state, bytes, rid);
    }
    internal static JsonObject EndProof(JsonObject content, JsonObject state, string receiptId)
    {
        var cycle = state["activeCycle"]!; var old = state["originalMovementEnd"]!; var world = state["world"]!;
        var scope = O(("gameTurn", cycle["gameTurn"]), ("operationStage", cycle["operationStage"]), ("playerPhaseSlot", cycle["playerPhaseSlot"]), ("actingSide", cycle["actingSide"]));
        var at = new JsonArray(world["elements"]!.AsArray().Select(e => (JsonNode?)O(("unit", O(("creationBinding", world["creationBinding"]), ("originalSide", content["elements"]!.AsArray().Single(f => T(f!, "elementId") == T(e!, "elementId"))!["sideId"]), ("elementId", e!["elementId"]))), ("locationId", e!["currentLocationId"])))
            .OrderBy(e => T(e!["unit"]!, "creationBinding"), StringComparer.Ordinal).ThenBy(e => T(e!["unit"]!, "originalSide"), StringComparer.Ordinal).ThenBy(e => T(e!["unit"]!, "elementId"), StringComparer.Ordinal).ToArray());
        Require(JsonNode.DeepEquals(old["scope"], scope) && N(old, "ordinal") == N(cycle, "ordinal") - 1 && old["endLocations"]!.AsArray().Count == at.Count && old["endLocations"]!.AsArray().Zip(at).All(pair => JsonNode.DeepEquals(pair.First!["unit"], pair.Second!["unit"])) && state["members"]!.AsArray().All(m => T(m!, "status") == "none" && JsonNode.DeepEquals(m!["history"]!["scope"], scope)), 4);
        bool Near(string start, string end)
        {
            var found = new HashSet<string>(StringComparer.Ordinal) { start }; var front = new HashSet<string>(found, StringComparer.Ordinal);
            for (var depth = 0; depth < 2; depth++)
            {
                front = content["edges"]!.AsArray().Where(e => front.Contains(T(e!, "firstLocationId")) || front.Contains(T(e!, "secondLocationId"))).Select(e => front.Contains(T(e!, "firstLocationId")) ? T(e!, "secondLocationId") : T(e!, "firstLocationId")).ToHashSet(StringComparer.Ordinal); found.UnionWith(front);
            }
            return found.Contains(end);
        }
        JsonArray Excluded(JsonArray locations, JsonArray retained) => new(locations.Where(e => T(e!["unit"]!, "originalSide") == T(cycle, "actingSide") && (retained.Any(u => JsonNode.DeepEquals(u, e["unit"])) || !locations.Any(other => T(other!["unit"]!, "originalSide") != T(cycle, "actingSide") && Near(T(e, "locationId"), T(other, "locationId"))))).Select(e => e!["unit"]!.DeepClone()).ToArray());
        var historical = Excluded(old["endLocations"]!.AsArray(), old["excludedBefore"]!.AsArray());
        return O(("scope", scope), ("ordinal", cycle["ordinal"]), ("completionReceiptId", receiptId), ("endLocations", at), ("excludedBefore", Excluded(at, historical)));
    }

}
