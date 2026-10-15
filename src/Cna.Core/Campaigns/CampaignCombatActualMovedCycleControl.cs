using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Cna.Core.Campaigns.CampaignCombatActualMovedCycleControlCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedCycleControlResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    private readonly byte[][][] inputs;
    internal CombatActualMovedCycleControlResult(JsonObject packet, JsonObject state, byte[][][] ledgers, byte[]? ev = null, bool duplicate = false)
    {
        source = Canonical(packet, "Source"); control = Canonical(state, "Control"); accepted = ev?.ToArray(); Duplicate = duplicate;
        inputs = ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var hash = Hash(source);
        var events = packet["controlEventCanonicalUtf8"]!.AsArray();
        proof = Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "mccs." + hash[7..],
            ["sourceHash"] = hash,
            ["parentSourceHash"] = state["parentSourceHash"]!.DeepClone(),
            ["dependencyManifestHash"] = ManifestHash,
            ["control"] = state.DeepClone(),
            ["lastEventHash"] = events.Count == 0 ? null : Hash(Encoding.ASCII.GetBytes(events[^1]!.GetValue<string>())),
            ["terminalReceiptId"] = (state["closure"]?["receiptId"] ?? state["pendingIntentReceiptId"])?.DeepClone()
        }, "Proof");
    }
    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => accepted?.ToArray();
    public bool Duplicate { get; }
    public string? ReceiptId => accepted is null ? null : JsonNode.Parse(accepted)!["receiptId"]!.GetValue<string>();
    public JsonElement Control => View(control);
    public JsonElement Proof => View(proof);
    private static JsonElement View(byte[] bytes) { using var d = JsonDocument.Parse(bytes); return d.RootElement.Clone(); }
    private byte[][] Ledger(int index) => inputs[index].Select(b => b.ToArray()).ToArray();
    public IReadOnlyList<byte[]> PrefixInputs => Ledger(0);
    public IReadOnlyList<byte[]> LifecycleInputs => Ledger(1);
    public IReadOnlyList<byte[]> BreakdownInputs => Ledger(2);
    public IReadOnlyList<byte[]> SelectionInputs => Ledger(3);
    public IReadOnlyList<byte[]> RoundInputs => Ledger(4);
    public IReadOnlyList<byte[]> PaidInputs => Ledger(5);
    public IReadOnlyList<byte[]> ReleaseInputs => Ledger(6);
    public IReadOnlyList<byte[]> ControlInputs => Ledger(7);
}
internal static class CampaignCombatActualMovedCycleControl
{
    private sealed record Frame(JsonObject Packet, JsonObject Parent, JsonObject Base, string BaseHash, CampaignCombatCreationRequest Request, byte[][][] Ledgers, Func<string, byte[]> Dependencies);
    private const string SchemaPath = "docs/specs/combat-actual-moved-cycle-control-v1.schema.json";
    private static readonly string[] Handlers = ["strict-n2-eight-ledger-replay", "both-paid-and-armed", "authentic-current-occurrence", "full-parent-support-certificate", "safe-vs-reaction-witness", "active-control-open", "window-local-high-water", "owner-finish", "non-executable-repeat-intent", "system-clock-fallback-finish", "same-slot-truck-convoy-entry", "complete-state-retention", "claims-replay", "fresh-admission-only", "owned-callback-bytes", "capacity-closure"];
    public static CombatActualMovedCycleControlResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, Func<string, byte[]> dependencies)
    { var (f, s) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies); return new(f.Packet, s, f.Ledgers); }
    public static CombatActualMovedCycleControlResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var current = Copy(currentInput, "Input"); var input = Object(current, "Input");
        var (f, s) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies);
        var (_, ev, rid) = Transition(f, s, input, admissionEnabled);
        if (ev is null)
        {
            var old = rid is null ? null : f.Packet["controlEventCanonicalUtf8"]!.AsArray().Select(e => Encoding.ASCII.GetBytes(e!.GetValue<string>())).Single(e => T(JsonNode.Parse(e)!, "receiptId") == rid);
            return new(f.Packet, s, f.Ledgers, old, rid is not null);
        }
        var packet = f.Packet.DeepClone().AsObject(); packet["controlEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var ledgers = f.Ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); ledgers[7] = [.. ledgers[7], current];
        var (after, state) = Replay(Canonical(packet, "Source"), request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], f.Dependencies);
        return new(after.Packet, state, after.Ledgers, ev);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies).SourceBytes;
    public static CombatActualMovedCycleControlResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Control"); _ = Object(owned, "Control"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies); Require(owned.AsSpan().SequenceEqual(result.ControlBytes), 6); return result; }
    public static CombatActualMovedCycleControlResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Proof"); _ = Object(owned, "Proof"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies); Require(owned.AsSpan().SequenceEqual(result.ProofBytes), 6); return result; }
    private static (Frame Frame, JsonObject State) Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, Func<string, byte[]> dependencies)
    {
        var packet = Object(source, "Source"); SourceBounds(packet); var parentBytes = Encoding.ASCII.GetBytes(T(packet, "continuationSourceCanonicalUtf8"));
        // Own every ledger before callbacks; predecessor replay independently authenticates exact counts and inputs.
        Require(prefix.Count == 13 && lifecycle.Count == 3 && breakdown.Count == 1 && selection.Count <= 16 && round.Count <= 16 && paid.Count <= 11 && release.Count == 2 && control.Count <= 2, 4);
        var ledgers = new[] { Own(prefix, prefix.Count, "AncestorInput"), Own(lifecycle, lifecycle.Count, "AncestorInput"), Own(breakdown, breakdown.Count, "AncestorInput"), Own(selection, selection.Count, "AncestorInput"), Own(round, round.Count, "AncestorInput"), Own(paid, paid.Count, "AncestorInput"), Own(release, release.Count, "AncestorInput"), Own(control, packet["controlEventCanonicalUtf8"]!.AsArray().Count, "Input") };
        var retained = SnapshotDependencies(dependencies);
        var parentResult = CampaignCombatActualMovedContinuation.ReplayTrustedSource(parentBytes, request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], retained);
        var parent = JsonNode.Parse(parentResult.ControlBytes)!.AsObject(); var a = parent["assessment"]!;
        Require(B(parent, "closed") && T(parent, "phase") == "assessed" && T(a, "witnessOutcome") == "supported" && !B(a, "executionAvailable") && a["witnesses"]!.AsArray().Count > 0, 4);
        Require(T(parent["cycle"]!, "admittedPolicyBundleDigest") == request.Context.Configuration.Hash, 4);
        var basis = O(("contractVersion", 1), ("parentSourceHash", Hash(parentBytes)), ("cycleId", parent["cycleId"]), ("assessmentHash", Hash(CampaignCombatActualMovedContinuationCodec.Canonical(a, "Assessment"))), ("configurationHash", parent["cycle"]!["admittedPolicyBundleDigest"]), ("dependencyManifestHash", ManifestHash));
        var f = new Frame(packet, parent, basis, "sha256:" + Domain("base", Canonical(basis, "Base")), request, ledgers, retained); var state = Initial(f);
        for (var i = 0; i < ledgers[7].Length; i++)
        { var (after, ev, _) = Transition(f, state, Object(ledgers[7][i], "Input"), true, true); Require(ev is not null && ev.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes(packet["controlEventCanonicalUtf8"]![i]!.GetValue<string>())), 6); state = after; }
        return (f, state);
    }
    private static JsonObject Initial(Frame f)
    {
        var s = f.Parent.DeepClone().AsObject(); s["parentSourceHash"] = f.Base["parentSourceHash"]!.DeepClone(); s["baseHash"] = f.BaseHash; s["phase"] = "control-unopened"; s["closed"] = false;
        s["controlId"] = "mcc." + Domain("control", Canonical(f.Base, "Base")); s["decisionId"] = T(s, "controlId") + ".decision";
        s["activeCycle"] = s["cycle"]!.DeepClone(); s["closure"] = null; s["openingClockFailure"] = false; s["acceptedHighWater"] = null; s["pendingRepeatIntent"] = null; s["pendingIntentReceiptId"] = null; s["controlReceipts"] = new JsonArray();
        _ = Canonical(s, "Control"); return s;
    }
    private static (JsonObject State, byte[]? Event, string? Receipt) Transition(Frame f, JsonObject prior, JsonObject input, bool admissionEnabled, bool replay = false)
    {
        _ = Canonical(input, "Input"); var cmd = input["command"]!; var kind = T(cmd, "kind"); var actor = T(input, "actor"); var p = f.Parent; var c = p["cycle"]!; var timer = kind is "expire" or "unavailable"; var opening = kind == "control-open";
        Require(N(cmd, "contractVersion") == 1 && T(cmd, "parentSourceHash") == T(f.Base, "parentSourceHash") && T(cmd, "controlId") == T(prior, "controlId") && T(cmd, "cycleId") == T(p, "cycleId") && T(cmd, "configurationHash") == T(f.Base, "configurationHash"), 4);
        Require(actor == (kind is "finish" or "repeat-intent" ? T(c, "actingSide") : "system") && (cmd["expectedPriorVersion"] is null) == timer && JsonNode.DeepEquals(cmd["decisionId"], opening ? null : prior["decisionId"]), 4);
        var ch = Hash(Canonical(cmd, "Command")); var duplicate = prior["controlReceipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == ch && T(r!, "actor") == actor);
        if (duplicate is not null) return (prior.DeepClone().AsObject(), null, T(duplicate, "receiptId"));
        if (timer && T(prior, "phase") != "control-open") return (prior.DeepClone().AsObject(), null, null);
        Require(!B(prior, "closed") && prior["controlReceipts"]!.AsArray().Count < 2 && N(prior, "stateVersion") < long.MaxValue, 6);
        if (!timer) Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion"), 6);
        var s = prior.DeepClone().AsObject(); long? now = input["admittedAt"] is null ? null : N(input, "admittedAt"); var reason = "owner-choice"; var author = actor; string outcome; JsonObject? intent = null;
        if (opening)
        {
            Require(T(prior, "phase") == "control-unopened" && N(prior, "stateVersion") <= long.MaxValue - 2, 6);
            if (!replay) { Require(admissionEnabled, 7); Certify(f); }
            var budget = f.Request.Context.Configuration.Windows.Single(w => w.Kind == "cycle-control").DecisionBudgetMilliseconds;
            var valid = B(input, "clockAvailable") && now is not null && now <= CampaignCombatActualPaidAssaultSupport.MaxUtc - budget;
            s["openingClockFailure"] = !valid; s["acceptedHighWater"] = valid ? now : null;
            s["controlWindow"] = valid ? O(("contractVersion", 1), ("configHash", f.Base["configurationHash"]), ("kind", "cycle-control"), ("decisionBudgetMilliseconds", budget), ("openedAtUnixMilliseconds", now), ("deadlineUnixMilliseconds", checked(now!.Value + budget)), ("highWaterUnixMilliseconds", now)) : null;
            outcome = "control-opened"; reason = valid ? "window-opened" : "opening-clock-unavailable"; s["phase"] = "control-open";
        }
        else
        {
            Require(T(prior, "phase") == "control-open", 6); var timing = prior["controlWindow"];
            var lost = B(prior, "openingClockFailure") || !B(input, "clockAvailable") || now is null || prior["acceptedHighWater"] is not null && now < N(prior, "acceptedHighWater");
            var late = timing is not null && now is not null && now >= N(timing, "deadlineUnixMilliseconds");
            if (kind == "fallback-step") Require(B(prior, "openingClockFailure"), 5);
            if (kind == "expire" && !lost && !late) return (prior.DeepClone().AsObject(), null, null);
            if (!lost) { s["acceptedHighWater"] = now; s["controlWindow"]!["highWaterUnixMilliseconds"] = now; }
            if (lost || late || timer || kind == "fallback-step")
            { author = "system"; outcome = "phase-finished"; reason = B(prior, "openingClockFailure") ? "opening-clock-unavailable" : lost ? "clock-unavailable" : late ? "deadline" : "controller-unavailable"; }
            else outcome = kind == "repeat-intent" ? "repeat-intent-pending" : "phase-finished";
            s["closed"] = true; s["phase"] = outcome == "phase-finished" ? "finished" : outcome;
            if (outcome == "phase-finished") { s["activeCycle"] = null; s["sequencePosition"] = FinishPosition(prior["sequencePosition"]!); }
            else
            {
                var a = prior["assessment"]!; Require(a["witnesses"]!.AsArray().Count > 0 && !B(a, "executionAvailable") && T(a, "sourceOccurrence") == T(p, "cycleId"), 7);
                intent = O(("contractVersion", 1), ("kind", "pending-repeat-intent"), ("sourceOccurrence", p["cycleId"]), ("sourceOrdinal", c["ordinal"]), ("expectedNextOrdinal", N(c, "ordinal") + 1), ("parentSourceHash", f.Base["parentSourceHash"]), ("worldHash", a["worldHash"]), ("assessmentHash", f.Base["assessmentHash"]), ("configurationHash", f.Base["configurationHash"]), ("decisionId", prior["decisionId"]), ("witnessesHash", Hash(Encode(a["witnesses"]!))), ("executionAvailable", false));
                s["pendingRepeatIntent"] = intent.DeepClone();
            }
        }
        var effect = O(("kind", outcome), ("reason", reason), ("decisionId", s["decisionId"]), ("timing", s["controlWindow"]), ("openingClockFailure", s["openingClockFailure"]), ("successorPositionId", s["sequencePosition"]!["positionId"]), ("intent", intent));
        var ev = O(("contractVersion", 1), ("eventType", "moved-cycle-control-" + outcome), ("author", author), ("campaignId", c["campaignId"]), ("rulesetHash", c["rulesetHash"]), ("configurationHash", c["admittedPolicyBundleDigest"]), ("profile", p["profile"]), ("parentSourceHash", f.Base["parentSourceHash"]), ("baseHash", f.BaseHash), ("controlId", s["controlId"]), ("cycleId", p["cycleId"]), ("priorVersion", prior["stateVersion"]), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", input), ("effect", effect));
        var rid = "mcce." + Domain("receipt", Encode(ev)); ev["receiptId"] = rid; var bytes = Canonical(ev, "Event"); s["stateVersion"] = ev["stateVersion"]!.DeepClone(); s["prefix"] = EventPrefix(T(prior, "prefix"), bytes);
        var record = O(("commandHash", ch), ("eventHash", Hash(bytes)), ("receiptId", rid), ("actor", actor), ("stateVersion", s["stateVersion"])); s["receipts"]!.AsArray().Add(record.DeepClone()); s["controlReceipts"]!.AsArray().Add(record);
        if (outcome == "phase-finished") s["closure"] = O(("cycleId", p["cycleId"]), ("ordinal", c["ordinal"]), ("outcome", "finished"), ("receiptId", rid));
        if (outcome == "repeat-intent-pending") s["pendingIntentReceiptId"] = rid;
        _ = Canonical(s, "Control"); return (s, bytes, rid);
    }
    private static void Certify(Frame f, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null)
    {
        var schema = JsonNode.Parse(f.Dependencies(SchemaPath))!; var expected = schema["supportHandlers"]!.AsArray().Select(h => h!.GetValue<string>()).ToArray();
        Require(expected.SequenceEqual(Handlers) && (handlers is null || handlers.SetEquals(Handlers)), 7);
        foreach (var pair in schema["limits"]!.AsObject()) Require(capacities is null || capacities.TryGetValue(pair.Key, out var actual) && actual >= pair.Value!.GetValue<int>(), 7);
        CampaignCombatActualMovedContinuation.CertifyTrustedSource(Encoding.ASCII.GetBytes(T(f.Packet, "continuationSourceCanonicalUtf8")), f.Request, f.Ledgers[0], f.Ledgers[1], f.Ledgers[2], f.Ledgers[3], f.Ledgers[4], f.Ledgers[5], f.Ledgers[6], f.Dependencies);
        // N2 proves nonempty safe witnesses over every supported projection class. N3's
        // terminal policy does not read or mutate resource classes, so certify its
        // uniform retention/closure law independently of those isolated worlds.
        var initial = Initial(f); var opened = Transition(f, initial, CertificationInput(initial, "control-open", 1000), true, true).State;
        foreach (var kind in new[] { "finish", "repeat-intent", "expire", "unavailable" })
        {
            var now = kind == "expire" ? N(opened["controlWindow"]!, "deadlineUnixMilliseconds") : 1001;
            var terminal = Transition(f, opened, CertificationInput(opened, kind, now), true, true).State;
            foreach (var field in new[] { "world", "randomState", "movementEnd", "actualProgressRefs", "members", "attackHistory", "targetUses", "cycle", "assessment" }) Require(JsonNode.DeepEquals(terminal[field], opened[field]), 7);
            var repeat = kind == "repeat-intent"; Require(B(terminal, "closed") && T(terminal, "phase") == (repeat ? "repeat-intent-pending" : "finished"), 7);
            Require(repeat ? JsonNode.DeepEquals(terminal["activeCycle"], opened["activeCycle"]) && terminal["closure"] is null && !B(terminal["pendingRepeatIntent"]!, "executionAvailable") : terminal["activeCycle"] is null && N(terminal["closure"]!, "ordinal") == 1 && T(terminal["sequencePosition"]!, "positionId") == "land.position.operation-1.first-player.truck-convoy-movement", 7);
        }
        // The isolated effects are not appended to Source, an input ledger or Proof.
    }
    private static JsonObject CertificationInput(JsonObject state, string kind, long now)
        => O(("command", O(("contractVersion", 1), ("kind", kind), ("parentSourceHash", state["parentSourceHash"]), ("controlId", state["controlId"]), ("cycleId", state["cycleId"]), ("configurationHash", state["cycle"]!["admittedPolicyBundleDigest"]), ("expectedPriorVersion", kind is "expire" or "unavailable" ? null : state["stateVersion"]), ("decisionId", kind == "control-open" ? null : state["decisionId"]))), ("actor", kind is "finish" or "repeat-intent" ? state["cycle"]!["actingSide"] : JsonValue.Create("system")), ("admittedAt", now), ("clockAvailable", true));
    internal static void CertifyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, Func<string, byte[]> dependencies, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null)
    { var (f, _) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, dependencies); Certify(f, handlers, capacities); }
    private static JsonObject FinishPosition(JsonNode originalPosition)
    {
        Require(N(originalPosition, "contractVersion") == 5 && N(originalPosition, "gameTurn") == 1 && N(originalPosition, "operationStage") == 1 && T(originalPosition, "positionId") == "land.position.operation-1.first-player.movement-and-combat.reserve-release" && T(originalPosition, "phaseId") == "land.phase.movement-and-combat" && T(originalPosition, "segmentId") == "land.segment.reserve-release" && originalPosition["stepId"] is null, 4);
        // The admitted private v5 position carries the same slot, actor role, stage,
        // turn and source references as its canonical Truck Convoy successor.
        var successor = originalPosition.DeepClone().AsObject();
        successor["positionId"] = "land.position.operation-1.first-player.truck-convoy-movement";
        successor["phaseId"] = "land.phase.truck-convoy-movement";
        successor["segmentId"] = null;
        return successor;
    }
    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static string T(JsonNode node, string key) => node[key]!.GetValue<string>();
    private static long N(JsonNode node, string key) => JsonSerializer.SerializeToElement(node[key]).GetInt64();
    private static bool B(JsonNode node, string key) => node[key]!.GetValue<bool>();
    private static string Domain(string kind, byte[] bytes) => HashWithDomain("sandtable.combat.actual-moved-cycle-control." + kind + ".v1", bytes)[7..];
}
