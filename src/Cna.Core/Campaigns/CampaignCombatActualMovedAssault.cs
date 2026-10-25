using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualMovedAssaultCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedAssaultResult
{
    private static readonly string[] EventFields = ["paidEventCanonicalUtf8", "roundEventCanonicalUtf8", "selectionEventCanonicalUtf8"];
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    private readonly byte[][][] inputs;
    internal CombatActualMovedAssaultResult(JsonObject packet, JsonObject state, string parentHash, byte[][][] ledgers, string manifestHash, byte[]? ev = null, bool duplicate = false)
    {
        source = Canonical(packet, "Source"); control = Canonical(state, "FamilyControl");
        accepted = ev?.ToArray(); Duplicate = duplicate;
        inputs = ledgers.Select(l => l.Select(i => i.ToArray()).ToArray()).ToArray();
        var sourceHash = Hash(source);
        var latest = EventFields
            .Select(key => packet[key]!.AsArray()).FirstOrDefault(events => events.Count > 0);
        var lastEventHash = latest is null ? null : Hash(Encoding.ASCII.GetBytes(latest[^1]!.GetValue<string>()));
        proof = Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "mvas." + sourceHash[7..],
            ["sourceHash"] = sourceHash,
            ["parentSourceHash"] = parentHash,
            ["dependencyManifestHash"] = manifestHash,
            ["control"] = JsonNode.Parse(control),
            ["lastEventHash"] = lastEventHash,
            ["terminalReceiptId"] = state["closed"]!.GetValue<bool>() ? state["receipts"]!.AsArray()[^1]!["receiptId"]!.DeepClone() : null,
            ["resultSupport"] = null
        }, "FamilyProof");
    }
    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => accepted?.ToArray();
    public bool Duplicate { get; }
    public string? ReceiptId { get { if (accepted is null) return null; using var d = JsonDocument.Parse(accepted); return d.RootElement.GetProperty("receiptId").GetString(); } }
    public JsonElement Control { get { using var d = JsonDocument.Parse(control); return d.RootElement.Clone(); } }
    public JsonElement Proof { get { using var d = JsonDocument.Parse(proof); return d.RootElement.Clone(); } }
    private byte[][] Ledger(int index) => inputs[index].Select(i => i.ToArray()).ToArray();
    public IReadOnlyList<byte[]> PrefixInputs => Ledger(0);
    public IReadOnlyList<byte[]> LifecycleInputs => Ledger(1);
    public IReadOnlyList<byte[]> BreakdownInputs => Ledger(2);
    public IReadOnlyList<byte[]> SelectionInputs => Ledger(3);
    public IReadOnlyList<byte[]> RoundInputs => Ledger(4);
    public IReadOnlyList<byte[]> PaidInputs => Ledger(5);
}

/// <summary>Specific private moved-source family; full actual ancestry and independent ledgers precede every adjudication.</summary>
internal static class CampaignCombatActualMovedAssault
{
    private sealed record RoundFacts(JsonObject Base, string BaseHash, string ClockHash, string[] Route);
    private sealed record Frame(JsonObject Packet, CampaignCombatCreationRequest Request, CombatActualReactionCombatEntryResult Parent,
        JsonObject Boundary, JsonObject SelectionState, JsonObject? RoundState, JsonObject? PaidState,
        RoundFacts? Round, PaidAssaultFrame? Paid, byte[][][] Ledgers, Func<string, byte[]> Dependencies);
    private const long MaximumUtc = 253402300799999;
    private const string ContentPath = "docs/specs/fixtures/combat-content-v7.canonical.json";
    private const string ClockPolicy = "sandtable.combat.mandatory-window-clock.v2";
    private static readonly string[] Unsupported = ["commit-attack", "resolve-attack", "release-reserves", "repeat-cycle", "finish-cycle", "open-later-stage", "consume-lineage", "refund", "reseed"];
    private static readonly string[] MandatoryCuts = ["disposition", "losses", "retreat", "relationships"];
    private static readonly string[] SettlementCuts = ["disposition", "losses", "retreat", "custody", "relationships"];
    public static CombatActualMovedAssaultResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
        => Result(Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, dependencies));
    public static CombatActualMovedAssaultResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, ReadOnlySpan<byte> currentInput,
        Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var current = Copy(currentInput, "Envelope"); var envelope = Object(current, "Envelope");
        var frame = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, dependencies);
        var phase = T(envelope, "phase"); var input = envelope["input"]!.AsObject();
        JsonObject after; byte[]? ev; string? receipt; bool duplicate;
        if (phase == "selection")
        {
            var old = frame.SelectionState;
            var commandHash = Hash(Canonical(input["command"]!, "SelectionCommand"));
            var existing = old["receipts"]!.AsArray().Any(r => T(r!, "commandHash") == commandHash && T(r!, "actor") == T(input, "actor"));
            if (T(input["command"]!, "kind") == "open-segment" && !existing) Require(admissionEnabled, 7);
            (after, ev, receipt, duplicate) = SelectionTransition(frame.Boundary, old, input, request.Context.Configuration);
        }
        else if (phase == "round")
        {
            Require(frame.Round is not null && frame.RoundState is not null, 4);
            (after, ev, receipt, duplicate) = RoundTransition(frame.Round, frame.RoundState, input, frame.Dependencies, admissionEnabled);
        }
        else
        {
            Require(frame.Paid is not null && frame.PaidState is not null, 4);
            (after, ev, receipt) = PaidTransition(frame.Paid, frame.PaidState, input, admissionEnabled);
            duplicate = ev is null && receipt is not null;
        }
        if (duplicate)
        {
            var eventTexts = frame.Packet[phase + "EventCanonicalUtf8"]!.AsArray();
            var original = eventTexts.Select(x => Encoding.ASCII.GetBytes(x!.GetValue<string>())).Single(x => T(Object(x, phase == "selection" ? "SelectionEvent" : phase == "round" ? "RoundEvent" : "PaidEvent"), "receiptId") == receipt);
            return Result(frame, original, true);
        }
        if (ev is null) return Result(frame);
        var packet = frame.Packet.DeepClone().AsObject(); packet[phase + "EventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var ledgers = frame.Ledgers.Select(l => l.Select(i => i.ToArray()).ToArray()).ToArray();
        var index = phase == "selection" ? 3 : phase == "round" ? 4 : 5; ledgers[index] = [.. ledgers[index], current];
        var replayed = Replay(Canonical(packet, "Source"), request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], frame.Dependencies);
        return Result(replayed, ev);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, dependencies).SourceBytes;
    public static CombatActualMovedAssaultResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        var owned = Copy(claim, "FamilyControl"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, dependencies);
        _ = Object(owned, "FamilyControl"); Require(owned.AsSpan().SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualMovedAssaultResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        var owned = Copy(claim, "FamilyProof"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, dependencies);
        _ = Object(owned, "FamilyProof"); Require(owned.AsSpan().SequenceEqual(result.ProofBytes), 6); return result;
    }
    private static Frame Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        ArgumentNullException.ThrowIfNull(request);
        var packet = Object(source, "Source"); SourceBounds(packet);
        var ledgers = new[] { Own(prefix, 13, "Source"), Own(lifecycle, 3, "Source"), Own(breakdown, 1, "Source"),
            Own(selection, packet["selectionEventCanonicalUtf8"]!.AsArray().Count, "Envelope"),
            Own(round, packet["roundEventCanonicalUtf8"]!.AsArray().Count, "Envelope"), Own(paid, packet["paidEventCanonicalUtf8"]!.AsArray().Count, "Envelope") };
        var retained = SnapshotDependencies(dependencies);
        var parent = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(Encoding.ASCII.GetBytes(T(packet, "reactionEntrySourceCanonicalUtf8")), request, ledgers[0], ledgers[1], ledgers[2], retained);
        Require(parent.Candidate is not null && parent.FinalBreakdownEventBytes is not null && parent.Control.GetProperty("stateVersion").GetInt64() == 18, 4);
        var entry = JsonNode.Parse(parent.ControlBytes)!.AsObject(); var parentProof = JsonNode.Parse(parent.ProofBytes)!;
        var boundary = DeriveBoundary(packet, entry, parentProof);
        var state = SelectionInitial(boundary);
        for (var i = 0; i < ledgers[3].Length; i++)
        {
            var input = StageInput(ledgers[3][i], "selection"); var expectedText = T(packet["selectionEventCanonicalUtf8"]![i]!, null);
            var (next, ev, _, duplicate) = SelectionTransition(boundary, state, input, request.Context.Configuration);
            Require(!duplicate && ev is not null && Encoding.ASCII.GetBytes(expectedText).AsSpan().SequenceEqual(ev), 6); state = next;
        }
        RoundFacts? roundFacts = null; JsonObject? roundState = null;
        var selected = T(state, "selectionOutcome") == "selected" && N(state, "stepIndex") == 3 && state["declineReceiptId"] is not null && state["cancellationReceiptId"] is null;
        if (selected)
        {
            var proof = O(("contractVersion", 1), ("sourceHash", PrefixSource(packet, "selection")), ("boundary", boundary), ("control", state));
            var clock = O(("contractVersion", 1), ("parentConfigurationHash", request.Context.Configuration.Hash),
                ("timingPolicyId", "sandtable.combat.public-opening-clock.v2"), ("decisionBudgetMilliseconds", request.Context.Configuration.Windows.Single(w => w.Kind == "force-assignment").DecisionBudgetMilliseconds));
            var basis = O(("contractVersion", 1), ("selectionProof", proof), ("clockConfiguration", clock));
            roundFacts = new(basis, RoundBaseHash(basis), RoundClockHash(basis), RoundRoute(basis)); roundState = RoundInitial(roundFacts);
        }
        else Require(ledgers[4].Length == 0 && ledgers[5].Length == 0, 4);
        for (var i = 0; i < ledgers[4].Length; i++)
        {
            var input = StageInput(ledgers[4][i], "round"); var expected = Encoding.ASCII.GetBytes(T(packet["roundEventCanonicalUtf8"]![i]!, null));
            var (next, ev, _, duplicate) = RoundTransition(roundFacts!, roundState!, input, retained);
            Require(!duplicate && ev is not null && expected.AsSpan().SequenceEqual(ev), 6); roundState = next;
        }
        PaidAssaultFrame? paidFacts = null; JsonObject? paidState = null;
        var prepared = roundState is not null && T(roundState, "status") == "prepared" && N(roundState, "stepIndex") == 5 &&
            roundState["stepReceipts"]!.AsArray().Count == 5 && roundState["slots"]!.AsArray().All(s => s!["sealedReceiptId"] is not null);
        if (prepared)
        {
            var basis = O(("contractVersion", 1), ("entrySourceHash", parentProof["sourceHash"]), ("selectionSourceHash", PrefixSource(packet, "selection")),
                ("preparedSourceHash", PrefixSource(packet, "round")), ("preparedWorldHash", Hash(Canonical(roundState!["world"]!, "World"))), ("configurationHash", request.Context.Configuration.Hash));
            _ = Canonical(basis, "PreparedBase");
            paidFacts = new(basis, roundState!, state["selection"]!.AsObject(), boundary, boundary["cycle"]!.AsObject(),
                request.Context.Configuration, JsonNode.Parse(retained(ContentPath))!.AsObject());
            paidState = PaidInitial(paidFacts);
        }
        else Require(ledgers[5].Length == 0, 4);
        for (var i = 0; i < ledgers[5].Length; i++)
        {
            var input = StageInput(ledgers[5][i], "paid"); var expected = Encoding.ASCII.GetBytes(T(packet["paidEventCanonicalUtf8"]![i]!, null));
            var (next, ev, _) = PaidTransition(paidFacts!, paidState!, input, true, true);
            Require(ev is not null && expected.AsSpan().SequenceEqual(ev), 6); paidState = next;
        }
        return new(packet, request, parent, boundary, state, roundState, paidState, roundFacts, paidFacts, ledgers, retained);
    }
    private static JsonObject StageInput(byte[] bytes, string phase)
    { var input = Object(bytes, "Envelope"); Require(T(input, "phase") == phase, 4); return input["input"]!.AsObject(); }
    private static JsonObject DeriveBoundary(JsonObject packet, JsonObject entry, JsonNode proof)
    {
        var parent = CampaignCombatActualReactionCombatEntryCodec.Parse(Encoding.ASCII.GetBytes(T(packet, "reactionEntrySourceCanonicalUtf8")));
        var weather = parent.Prefix.Select(p => JsonNode.Parse(p)!).Single(n => T(n, "eventType") == "weather-determined");
        return O(("contractVersion", 1), ("entrySourceHash", proof["sourceHash"]), ("creationBinding", entry["creationBinding"]),
            ("creationEventHash", entry["creationEventHash"]), ("cycle", entry["cycle"]), ("cycleId", entry["cycleId"]), ("firstActingSide", entry["firstActingSide"]),
            ("priorVersion", entry["stateVersion"]), ("priorPrefix", entry["prefix"]), ("reserveCompletionReceiptId", entry["completionReceiptId"]),
            ("movementCompletionReceiptId", entry["movementEnd"]!["completionReceiptId"]), ("breakdownCompletionReceiptId", entry["breakdownCompletionReceiptId"]),
            ("breakdownCompletionEventHash", proof["finalBreakdownEventHash"]), ("position", entry["sequencePosition"]), ("world", entry["world"]), ("randomState", entry["randomState"]),
            ("weather", O(("gameTurn", 1), ("operationStage", 1), ("attackerKind", "normal"), ("defenderKind", "normal"),
                ("weatherReceiptId", weather["receiptId"]), ("weatherEventHash", Hash(CampaignCombatActualReactionCombatEntryCodec.Encode(weather))))),
            ("breakdownFlow", O(("kind", "idle"))), ("reactionWindow", null), ("movementEnd", entry["movementEnd"]), ("candidate", proof["candidate"]));
    }
    private static string PrefixSource(JsonObject packet, string phase)
    { var value = packet.DeepClone().AsObject(); value["paidEventCanonicalUtf8"] = new JsonArray(); if (phase == "selection") value["roundEventCanonicalUtf8"] = new JsonArray(); return Hash(Canonical(value, "Source")); }
    private static CombatActualMovedAssaultResult Result(Frame frame, byte[]? ev = null, bool duplicate = false)
        => new(frame.Packet, Control(frame), T(frame.Parent.Proof.GetProperty("sourceHash")), frame.Ledgers, ManifestHashFor(frame.Dependencies), ev, duplicate);
    private static JsonObject Control(Frame frame)
    {
        var entry = JsonNode.Parse(frame.Parent.ControlBytes)!; var state = frame.PaidState ?? frame.RoundState ?? frame.SelectionState;
        var closed = B(state, "closed"); var phase = closed ? "reserve-entry" : frame.PaidState is not null ? "paid" : frame.RoundState is not null ? "round" : "selection";
        var index = closed ? 6 : frame.PaidState is not null ? 5 : (int)N(state, "stepIndex");
        var route = Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != T(frame.Boundary["position"]!, "positionId")).Take(7).ToArray();
        var position = JsonNode.Parse(Cna.Core.Campaigns.CampaignCombatPositiveEntryCodec.Canonical(JsonSerializer.SerializeToElement(frame.Boundary["position"]), "Position"));
        if (index > 0)
        {
            var p = route[index];
            position = PositionNode(new(5, p.PositionId, p.GameTurn, p.OperationStage, p.StageId, p.PhaseId, p.SegmentId, p.StepId, p.ActorRole, p.ActiveSide, p.Sources));
        }
        var receipts = new JsonArray();
        foreach (var group in new[] { entry["receipts"]!.AsArray(), frame.SelectionState["receipts"]!.AsArray(), frame.RoundState?["receipts"]?.AsArray(), frame.PaidState?["receipts"]?.AsArray() })
            if (group is not null) foreach (var r in group) receipts.Add(r!.DeepClone());
        var progress = entry["actualProgressRefs"]!.DeepClone().AsArray();
        foreach (var text in frame.Packet["paidEventCanonicalUtf8"]!.AsArray())
        { var bytes = Encoding.ASCII.GetBytes(text!.GetValue<string>()); var e = Object(bytes, "PaidEvent"); if (T(e["effect"]!, "kind") == "attack-committed") progress.Add(O(("eventType", e["eventType"]), ("receiptId", e["receiptId"]), ("eventHash", Hash(bytes)))); }
        Require(receipts.Count == N(state, "stateVersion") - 1, 6);
        return O(("contractVersion", 1), ("phase", phase), ("stateVersion", state["stateVersion"]), ("prefix", state["prefix"]), ("sequencePosition", position),
            ("cycle", entry["cycle"]), ("cycleId", entry["cycleId"]), ("world", frame.PaidState?["world"] ?? entry["world"]), ("randomState", frame.PaidState?["randomState"] ?? entry["randomState"]),
            ("movementEnd", entry["movementEnd"]), ("actualProgressRefs", progress), ("candidate", JsonNode.Parse(frame.Parent.ProofBytes)!["candidate"]), ("receipts", receipts),
            ("selection", frame.SelectionState), ("round", frame.RoundState), ("paid", frame.PaidState), ("closed", closed));
    }
    private static JsonNode PositionNode(LandSequencePosition position)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) { writer.WriteStartObject(); CampaignV11CanonicalCodec.WritePosition(writer, "position", position); writer.WriteEndObject(); }
        return JsonNode.Parse(stream.ToArray())!["position"]!.DeepClone();
    }
    private static JsonObject SelectionInitial(JsonObject b)
    {
        var bytes = Canonical(b, "MovedBoundary"); var id = "asg." + SelectionDomain("segment", bytes)[7..];
        return O(("contractVersion", 1), ("boundaryHash", Hash(bytes)), ("segmentId", id), ("stateVersion", b["priorVersion"]), ("prefix", b["priorPrefix"]),
            ("stepIndex", 0), ("selectionOutcome", "unopened"), ("selection", null), ("selectionReceiptId", null), ("declineReceiptId", null), ("cancellationReceiptId", null),
            ("selectionWindow", null), ("rbaWindow", null), ("stepReceipts", new JsonArray()), ("receipts", new JsonArray()), ("closed", false));
    }
    private static (JsonObject World, JsonArray Costs) CumulativePaidWorld(PaidAssaultFrame ctx)
    {
        var p = ctx.Prior; var w = p["world"]!.DeepClone().AsObject(); var costs = new JsonArray();
        Require(p["attackHistory"]!.AsArray().Count == 0 && p["targetUses"]!.AsArray().Count == 0 && w["settlements"]!.AsArray().Count == 0 && w["custodyLots"]!.AsArray().Count == 0, 7);
        foreach (var (role, cost) in new[] { ("attacker", 5), ("defender", 3) })
        {
            var allocation = p["slots"]!.AsArray().Single(x => T(x!, "role") == role)!["allocation"]!; var unit = ctx.Selection[role]!["unit"]!;
            Require(JsonNode.DeepEquals(allocation["unit"], unit) && N(allocation, "committedToe") == 10, 7);
            var e = w["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(unit, "elementId"))!; var cp = e["operationalState"]!["capabilityPointsExpended"]!;
            var before = N(cp, "numerator"); Require(N(cp, "denominator") == 1 && before == (role == "attacker" ? 4 : 0) &&
                N(e["ammunition"]!, "points") == 10 && N(e["components"]![0]!, "currentToe") == 10, 7);
            var after = checked(before + cost); Require(after <= 10, 7);
            e["operationalState"]!["capabilityPointsExpended"] = O(("numerator", after), ("denominator", 1)); e["ammunition"]!["points"] = 0;
            costs.Add(O(("unit", unit), ("beforeCp", before), ("afterCp", after), ("beforeAmmo", 10), ("afterAmmo", 0)));
        }
        return (w, costs);
    }
    private static JsonObject MovedProjected(PaidAssaultFrame ctx, JsonObject state, string cut, string? retreatChoice = null, string? custodyChoice = null)
    {
        var st = state["world"]!["settlements"]!.AsArray().FirstOrDefault();
        retreatChoice ??= st?["disposition"]?["kind"]?.GetValue<string>(); custodyChoice ??= st?["custody"]?["kind"]?.GetValue<string>();
        return CampaignCombatActualPaidAssaultSupport.Project(ctx, CumulativePaidWorld(ctx).World, state["result"]!.AsObject(), cut, retreatChoice, custodyChoice);
    }
    internal static void CertifyMovedSupport(PaidAssaultFrame ctx, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null, IEnumerable<JsonObject>? records = null)
    {
        Require(new HashSet<string>(CampaignCombatActualPaidAssaultSupport.SupportedHandlers(), StringComparer.Ordinal).SetEquals(handlers ?? CampaignCombatActualPaidAssaultSupport.SupportedHandlers()), 7);
        var limits = capacities ?? MovedSupportCapacities();
        foreach (var (kind, bound) in RequiredMovedCapacities) Require(limits.TryGetValue(kind, out var capacity) && capacity >= bound, 7);
        var paid = CumulativePaidWorld(ctx).World; var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var summaries = new Dictionary<string, JsonObject>(StringComparer.Ordinal); var ordinal = 0;
        using var supplied = (records ?? CampaignCombatActualPaidAssaultSupport.Catalogue()).GetEnumerator();
        foreach (var expected in CampaignCombatActualPaidAssaultSupport.Catalogue())
        {
            Require(supplied.MoveNext() && JsonNode.DeepEquals(supplied.Current, expected), 7); var row = supplied.Current;
            Require(N(row, "ordinal") == ordinal++, 7); var kind = T(row, "kind"); counts[kind] = counts.GetValueOrDefault(kind) + 1;
            _ = Canonical(row, kind == "morale" ? "MoraleRow" : kind == "assault-cell" ? "AssaultCell" : "VariantRow");
            if (kind != "capture-refusal") continue;
            var facts = row["facts"]!; var key = O(("attackerPercent", facts["attackerPercent"]), ("defenderPercent", facts["defenderPercent"]),
                ("rawEngaged", facts["rawEngaged"]), ("requiredRetreat", facts["requiredRetreat"]), ("capturedRole", facts["capturedRole"]),
                ("captureShare", facts["captureShare"]), ("refusalHexes", row["refusalHexes"]));
            summaries[Encoding.ASCII.GetString(Encode(key))] = row;
        }
        Require(!supplied.MoveNext(), 7);
        Require(ordinal == 16616 && counts["morale"] == 1296 && counts["assault-cell"] == 6480 && counts["capture-refusal"] == 8840, 7);
        foreach (var row in summaries.Values)
        {
            var f = row["facts"]!; var retreat = N(f, "requiredRetreat") == 0 ? "not-required" : N(row, "refusalHexes") > 0 ? "refuse-retreat" : "retreat";
            var captured = N(row, "attackerCapturedToe") + N(row, "defenderCapturedToe") > 0;
            var result = O(("resultId", "res." + new string('f', 64)), ("commitmentId", "cmt." + new string('f', 64)), ("facts", f));
            foreach (var custody in captured ? new[] { "relocate-and-guard", "leave-unguarded" } : new string?[] { null })
            {
                JsonObject? world = null;
                foreach (var cut in new[] { "resolved", "disposition", "losses", "retreat", "custody", "relationships" })
                {
                    if (cut == "custody" && !captured) continue;
                    world = CampaignCombatActualPaidAssaultSupport.Project(ctx, paid, result, cut, retreat, custody); _ = Canonical(world, "World");
                }
                Require(world!["elements"]!.AsArray().Sum(x => N(x!["components"]![0]!, "currentToe")) + world["guards"]!.AsArray().Sum(x => N(x!, "toe")) + N(row, "attackerLossToe") + N(row, "defenderLossToe") == 20, 7);
            }
        }
        foreach (var kind in new[] { "retreat", "custody" }) foreach (var owner in new[] { "axis", "commonwealth" })
        {
            Require(ctx.Configuration.Windows.Single(x => x.Kind == kind).Fallback == (kind == "retreat" ? "refuse-retreat" : "leave-unguarded"), 7);
            Require(MakeWindow(ctx, kind, owner, "set." + new string('f', 64), 10000, true) is not null && MakeWindow(ctx, kind, owner, "set." + new string('f', 64), null, true) is null && MakeWindow(ctx, kind, owner, "set." + new string('f', 64), MaximumUtc, true) is null, 7);
        }
        Require(ctx.Prior["randomState"]!["seed"]!.GetValue<ulong>() == 1 && ctx.Prior["randomState"]!["nextByteCursor"]!.GetValue<ulong>() == 2, 7);
    }
    private static readonly Dictionary<string, int> RequiredMovedCapacities = new(StringComparer.Ordinal)
    {
        ["PaidInput"] = 4096,
        ["PaidEvent"] = 65536,
        ["PaidControl"] = 524288,
        ["PaidProof"] = 2097152,
        ["AssaultResult"] = 65536,
        ["World"] = 524288,
        ["PaidSource"] = 7733760,
        ["PaidLedger"] = 45056,
        ["acceptedEvents"] = 11,
        ["acceptedReceipts"] = 11,
        ["draws"] = 9,
        ["consumedBytesPerDraw"] = 512,
        ["routes"] = 9
    };
    private static Dictionary<string, int> MovedSupportCapacities() => new(StringComparer.Ordinal)
    {
        ["PaidInput"] = 4096,
        ["PaidEvent"] = 65536,
        ["PaidControl"] = 524288,
        ["PaidProof"] = 2097152,
        ["AssaultResult"] = 65536,
        ["World"] = 524288,
        ["PaidSource"] = 8388608,
        ["PaidLedger"] = 45056,
        ["acceptedEvents"] = 11,
        ["acceptedReceipts"] = 11,
        ["draws"] = 9,
        ["consumedBytesPerDraw"] = 512,
        ["routes"] = 9
    };
    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static JsonArray A(IEnumerable<JsonNode?> nodes) => CampaignCombatActualPaidAssaultSupport.A(nodes);
    private static string T(JsonElement value) => value.GetString()!;
    private static string T(JsonNode value, string? key) => key is null ? value.GetValue<string>() : value[key]!.GetValue<string>();
    private static string Text(JsonNode value, string key) => T(value, key);
    private static string? TextOrNull(JsonNode value, string key) => value[key]?.GetValue<string>();
    private static long N(JsonNode value, string key) => JsonSerializer.SerializeToElement(value[key]).GetInt64();
    private static long Number(JsonNode value, string key) => N(value, key);
    private static bool B(JsonNode value, string key) => value[key]!.GetValue<bool>();
    private static string Domain(string domain, byte[] bytes) => CampaignOpeningPreambleCodec.HashWithDomain(domain, bytes)[7..];
    private static string SelectionDomain(string kind, byte[] bytes) => CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-moved-assault.selection." + kind + ".v1", bytes);
    private static string RoundDomain(string kind, byte[] bytes) => CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-moved-assault.round." + kind + ".v1", bytes);
    private static string PaidDigest(string kind, JsonNode value, string shape) => Domain("sandtable.combat.actual-moved-assault.paid." + kind + ".v1", Canonical(value, shape));
    private static byte[] PinnedBytes(string path, Func<string, byte[]> retained) => retained(path);
    private static JsonObject Resolve(JsonObject state) => CampaignCombatActualPaidAssaultSupport.Resolve(state);
    private static JsonObject? MakeWindow(PaidAssaultFrame ctx, string kind, string owner, string id, long? now, bool available)
        => CampaignCombatActualPaidAssaultSupport.MakeWindow(ctx, kind, owner, id, now, available);

    // Bounded source-independent ports of the frozen phase mechanics.
    internal static (JsonObject State, byte[]? Event, string? Receipt, bool Duplicate) SelectionTransition(JsonObject boundary, JsonObject prior, JsonObject input, CombatDecisionConfiguration configuration)
    {
        input = Object(Canonical(input, "SelectionInput"), "SelectionInput");
        prior = prior.DeepClone().AsObject();
        boundary = boundary.DeepClone().AsObject();
        var cmd = input["command"]!; var kind = Text(cmd, "kind"); var actor = Text(input, "actor");
        string[] allowed = kind switch
        {
            "open-segment" or "close-empty-selection" => ["expectedPriorVersion"],
            "choose-selection" => ["decisionId", "choice", "candidate"],
            "decline-rba" => ["decisionId", "participant"],
            "complete-step" or "open-rba" => ["fromPositionId", "expectedPriorVersion"],
            "expire-window" or "controller-unavailable" => ["decisionId"],
            _ => throw new JsonException("CMB-MAS-003"),
        };
        Require(Number(cmd, "contractVersion") == 1, 3);
        foreach (var field in new[] { "decisionId", "fromPositionId", "expectedPriorVersion", "choice", "candidate", "participant" }) Require(cmd[field] is null || allowed.Contains(field, StringComparer.Ordinal), 3);
        Require(Text(cmd, "segmentId") == Text(prior, "segmentId"), 4);
        Require(kind is "choose-selection" or "decline-rba" ? actor is "axis" or "commonwealth" : actor == "system", 4);
        var commandHash = Hash(Canonical(cmd.AsObject(), "SelectionCommand"));
        var duplicate = prior["receipts"]!.AsArray().FirstOrDefault(r => Text(r!, "commandHash") == commandHash);
        if (duplicate is not null) { Require(Text(duplicate, "actor") == actor, 4); return (prior, null, Text(duplicate, "receiptId"), true); }
        var outcome = Text(prior, "selectionOutcome"); var step = (int)Number(prior, "stepIndex");
        var window = outcome == "pending" ? prior["selectionWindow"] : outcome == "selected" && prior["declineReceiptId"] is null ? prior["rbaWindow"] : null;
        var beforeRba = kind == "controller-unavailable" && outcome == "selected" && step == 2 && prior["rbaWindow"] is null && TextOrNull(cmd, "decisionId") == Text(prior, "segmentId") + ".rba";
        if (!beforeRba && kind is "expire-window" or "controller-unavailable" && (window is null || TextOrNull(cmd, "decisionId") != Text(window, "decisionId"))) return (prior, null, null, false);
        Require(!prior["closed"]!.GetValue<bool>() && prior["receipts"]!.AsArray().Count < 16 && Number(prior, "stateVersion") < long.MaxValue, 6);
        var route = Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != Text(boundary["position"]!, "positionId")).Take(7).ToArray();
        if (kind is "open-segment" or "close-empty-selection" or "complete-step" or "open-rba") Require(cmd["expectedPriorVersion"]?.GetValue<long>() == Number(prior, "stateVersion"), 6);
        if (kind is "complete-step" or "open-rba") Require(TextOrNull(cmd, "fromPositionId") == route[step].PositionId, 6);
        var state = prior.DeepClone().AsObject(); JsonObject effect;
        var now = input["admittedAt"]?.GetValue<long>(); var available = input["clockAvailable"]!.GetValue<bool>();
        switch (kind)
        {
            case "open-segment":
                Require(outcome == "unopened" && prior["receipts"]!.AsArray().Count == 0, 6);
                Require(!available || now is not null, 5);
                var opened = available ? SelectionWindow(boundary, prior, "selection", now!.Value, configuration) : null;
                state["selectionOutcome"] = opened is null ? "system-no-selection" : "pending"; state["selectionWindow"] = opened?.DeepClone();
                effect = new() { ["kind"] = "segment-opened", ["boundaryHash"] = prior["boundaryHash"]!.DeepClone(), ["window"] = opened }; break;
            case "close-empty-selection":
                Require(outcome == "system-no-selection" && prior["selectionWindow"] is null, 6); Require(now is null && available, 5);
                effect = new() { ["kind"] = "selection-closed", ["outcome"] = "no-selection", ["candidate"] = null, ["timing"] = null }; break;
            case "choose-selection":
                Require(outcome == "pending" && window is not null && TextOrNull(cmd, "decisionId") == Text(window, "decisionId"), 6);
                Require(actor == Text(window!, "owner"), 4); var choice = TextOrNull(cmd, "choice");
                Require(choice is "select-close-assault" or "finish-without-attack", 3); var selected = choice == "select-close-assault";
                Require(selected ? JsonNode.DeepEquals(cmd["candidate"], boundary["candidate"]) : cmd["candidate"] is null, 4);
                Require(ClockBefore(window!, now, available), 5);
                effect = new() { ["kind"] = "selection-closed", ["outcome"] = selected ? "selected" : "no-selection", ["candidate"] = selected ? boundary["candidate"]!.DeepClone() : null, ["timing"] = Timing(window!, now) }; break;
            case "open-rba":
                Require(step == 2 && outcome == "selected" && prior["rbaWindow"] is null, 6);
                Require(available && now is not null && now >= Number(prior["selectionWindow"]!["timing"]!, "highWaterUnixMilliseconds"), 5);
                var rba = SelectionWindow(boundary, prior, "rba", now!.Value, configuration); state["rbaWindow"] = rba.DeepClone();
                effect = new() { ["kind"] = "rba-opened", ["selectionReceiptId"] = prior["selectionReceiptId"]!.DeepClone(), ["window"] = rba }; break;
            case "decline-rba":
                Require(step == 2 && outcome == "selected" && window is not null && TextOrNull(cmd, "decisionId") == Text(window, "decisionId"), 6);
                Require(actor == Text(window!, "owner") && JsonNode.DeepEquals(cmd["participant"], prior["selection"]!["defender"]!["unit"]), 4);
                Require(ClockBefore(window!, now, available), 5);
                effect = new() { ["kind"] = "rba-declined", ["selectionReceiptId"] = prior["selectionReceiptId"]!.DeepClone(), ["participant"] = cmd["participant"]!.DeepClone(), ["timing"] = Timing(window!, now) }; break;
            case "expire-window":
            case "controller-unavailable":
                if (beforeRba) effect = new() { ["kind"] = "selection-cancelled", ["selectionReceiptId"] = prior["selectionReceiptId"]!.DeepClone(), ["timing"] = null };
                else
                {
                    Require(window is not null, 6); if (kind == "expire-window" && ClockBefore(window!, now, available)) return (prior, null, null, false);
                    effect = outcome == "pending" ? new() { ["kind"] = "selection-closed", ["outcome"] = "no-selection", ["candidate"] = null, ["timing"] = Timing(window!, now) }
                        : new() { ["kind"] = "selection-cancelled", ["selectionReceiptId"] = prior["selectionReceiptId"]!.DeepClone(), ["timing"] = Timing(window!, now) };
                }
                break;
            default:
                Require(outcome is "selected" or "no-selection" or "cancelled", 6); var noAttack = outcome != "selected";
                Require(noAttack || step != 2 || prior["declineReceiptId"] is not null, 6); Require(now is null && available, 5); Require(noAttack || step < 3, 7);
                var disposition = prior["cancellationReceiptId"] ?? (step >= 2 ? prior["declineReceiptId"] : null) ?? prior["selectionReceiptId"];
                effect = new()
                {
                    ["kind"] = "step-completed",
                    ["fromPositionId"] = route[step].PositionId,
                    ["toPositionId"] = route[step + 1].PositionId,
                    ["previousStepReceiptId"] = (prior["stepReceipts"]!.AsArray().Count == 0 ? prior["receipts"]![0]!["receiptId"] : prior["stepReceipts"]!.AsArray()[^1])!.DeepClone(),
                    ["dispositionReceiptId"] = disposition!.DeepClone(),
                    ["proofKind"] = noAttack ? "no-attack" : new[] { "no-gun-positions", "no-barrage-work", "accepted-decline" }[step]
                }; break;
        }
        var acceptedEvent = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = "moved-selection-" + Text(effect, "kind"),
            ["campaignId"] = boundary["cycle"]!["campaignId"]!.DeepClone(),
            ["rulesetHash"] = boundary["cycle"]!["rulesetHash"]!.DeepClone(),
            ["configurationHash"] = boundary["cycle"]!["admittedPolicyBundleDigest"]!.DeepClone(),
            ["entrySourceHash"] = boundary["entrySourceHash"]!.DeepClone(),
            ["cycleId"] = boundary["cycleId"]!.DeepClone(),
            ["segmentId"] = prior["segmentId"]!.DeepClone(),
            ["priorVersion"] = Number(prior, "stateVersion"),
            ["stateVersion"] = checked(Number(prior, "stateVersion") + 1),
            ["priorPrefix"] = prior["prefix"]!.DeepClone(),
            ["input"] = input.DeepClone(),
            ["effect"] = effect,
        };
        var receipt = "asc." + SelectionDomain("receipt", Encode(acceptedEvent))[7..]; acceptedEvent["receiptId"] = receipt; var bytes = Canonical(acceptedEvent, "SelectionEvent");
        switch (Text(effect, "kind"))
        {
            case "selection-closed":
                state["selectionOutcome"] = effect["outcome"]!.DeepClone(); state["selection"] = effect["candidate"]?.DeepClone(); state["selectionReceiptId"] = receipt;
                if (state["selectionWindow"] is not null) state["selectionWindow"]!["timing"] = effect["timing"]?.DeepClone(); break;
            case "rba-declined": state["declineReceiptId"] = receipt; state["rbaWindow"]!["timing"] = effect["timing"]!.DeepClone(); break;
            case "selection-cancelled":
                state["selectionOutcome"] = "cancelled"; state["cancellationReceiptId"] = receipt;
                if (state["rbaWindow"] is not null) state["rbaWindow"]!["timing"] = effect["timing"]?.DeepClone(); break;
            case "step-completed": state["stepReceipts"]!.AsArray().Add(receipt); state["stepIndex"] = step + 1; state["closed"] = step == 5; break;
        }
        state["stateVersion"] = acceptedEvent["stateVersion"]!.DeepClone(); state["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(Text(prior, "prefix"), bytes);
        state["receipts"]!.AsArray().Add(new JsonObject { ["commandHash"] = commandHash, ["eventHash"] = Hash(bytes), ["receiptId"] = receipt, ["actor"] = actor, ["stateVersion"] = state["stateVersion"]!.DeepClone() });
        _ = Canonical(state, "SelectionControl"); return (state, bytes, receipt, false);
    }
    private static JsonObject SelectionWindow(JsonObject boundary, JsonObject state, string kind, long now, CombatDecisionConfiguration configuration)
    {
        var budget = configuration.Windows.Single(w => w.Kind == kind).DecisionBudgetMilliseconds;
        Require(now <= 253402300799999 - budget, 5); var side = Text(boundary["cycle"]!, "actingSide");
        return new()
        {
            ["decisionId"] = Text(state, "segmentId") + "." + kind,
            ["owner"] = kind == "selection" ? side : side == "axis" ? "commonwealth" : "axis",
            ["timing"] = new JsonObject
            {
                ["contractVersion"] = 1,
                ["configHash"] = configuration.Hash,
                ["kind"] = kind,
                ["decisionBudgetMilliseconds"] = budget,
                ["openedAtUnixMilliseconds"] = now,
                ["deadlineUnixMilliseconds"] = now + budget,
                ["highWaterUnixMilliseconds"] = now
            }
        };
    }
    private static bool ClockBefore(JsonNode window, long? now, bool available) => available && now is not null && now >= Number(window["timing"]!, "highWaterUnixMilliseconds") && now < Number(window["timing"]!, "deadlineUnixMilliseconds");
    private static JsonNode Timing(JsonNode window, long? now)
    {
        var result = window["timing"]!.DeepClone(); if (now is not null) result["highWaterUnixMilliseconds"] = Math.Max(now.Value, Number(result, "highWaterUnixMilliseconds")); return result;
    }

    private static JsonObject RoundInitial(RoundFacts facts)
    {
        var derived = facts.Base;
        var prior = RoundSelection(derived); var boundary = RoundBoundary(derived);
        return new()
        {
            ["contractVersion"] = 1,
            ["baseHash"] = facts.BaseHash,
            ["clockConfigurationHash"] = facts.ClockHash,
            ["segmentId"] = prior["segmentId"]!.DeepClone(),
            ["opportunityId"] = null,
            ["roundId"] = null,
            ["stateVersion"] = prior["stateVersion"]!.DeepClone(),
            ["prefix"] = prior["prefix"]!.DeepClone(),
            ["stepIndex"] = 3,
            ["status"] = "unopened",
            ["openingReceiptId"] = null,
            ["timing"] = null,
            ["slots"] = new JsonArray(),
            ["stepReceipts"] = prior["stepReceipts"]!.DeepClone(),
            ["world"] = boundary["world"]!.DeepClone(),
            ["randomState"] = boundary["randomState"]!.DeepClone(),
            ["attackHistory"] = new JsonArray(),
            ["targetUses"] = new JsonArray(),
            ["commitmentId"] = null,
            ["cancellationReceiptId"] = null,
            ["receipts"] = new JsonArray(),
            ["closed"] = false,
        };
    }

    /// <summary>Private mechanics over derived facts. Entry points always replay the original source and all six independent ledgers first.</summary>
    private static (JsonObject State, byte[]? Event, string? Receipt, bool Duplicate) RoundTransition(RoundFacts facts, JsonObject prior,
        JsonObject input, Func<string, byte[]> dependencyBytes, bool admissionEnabled = true)
    {
        var derived = facts.Base;
        input = Object(Canonical(input, "RoundInput"), "RoundInput");
        var cmd = input["command"]!; var actor = Text(input, "actor"); var kind = Text(cmd, "kind");
        Require(Number(cmd, "contractVersion") == 1 && (kind is "open-round" or "seal-choice" or "expire-round" or "controller-unavailable" or "complete-step" || Unsupported.Contains(kind, StringComparer.Ordinal)), 3);
        var structural = kind is "open-round" or "complete-step";
        Require((cmd["expectedPriorVersion"] is not null) == structural, 3);
        Require((cmd["fromPositionId"] is not null) == (kind == "complete-step"), 3);
        Require(kind == "seal-choice" ? cmd["slotId"] is not null && cmd["allocation"] is not null : cmd["slotId"] is null && cmd["allocation"] is null, 3);
        Require((cmd["roundId"] is null) == (kind == "open-round"), 3);
        Require(Text(cmd, "segmentId") == Text(prior, "segmentId") && Text(cmd, "clockConfigurationHash") == facts.ClockHash, 4);
        Require(kind == "seal-choice" ? actor is "axis" or "commonwealth" : actor == "system", 4);
        var commandHash = Hash(Canonical(cmd, "RoundCommand"));
        var duplicate = prior["receipts"]!.AsArray().FirstOrDefault(x => Text(x!, "commandHash") == commandHash);
        if (duplicate is not null) { Require(Text(duplicate, "actor") == actor, 4); return (prior.DeepClone().AsObject(), null, Text(duplicate, "receiptId"), true); }
        if (kind is "expire-round" or "controller-unavailable" && (TextOrNull(cmd, "roundId") != TextOrNull(prior, "roundId") || Text(prior, "status") != "collecting")) return (prior.DeepClone().AsObject(), null, null, false);
        Require(!prior["closed"]!.GetValue<bool>() && Number(prior, "stateVersion") < long.MaxValue && prior["receipts"]!.AsArray().Count < 16, 6);
        if (kind != "open-round") Require(TextOrNull(cmd, "roundId") == TextOrNull(prior, "roundId"), 4);
        if (structural) Require(Number(cmd, "expectedPriorVersion") == Number(prior, "stateVersion"), 6);
        var route = facts.Route; var index = (int)Number(prior, "stepIndex");
        if (kind == "complete-step") Require(index < 6 && Text(cmd, "fromPositionId") == route[index], 6);
        var state = prior.DeepClone().AsObject(); var now = input["admittedAt"]?.GetValue<long>(); var available = input["clockAvailable"]!.GetValue<bool>(); var author = actor;
        JsonObject effect;
        switch (kind)
        {
            case "open-round":
                Require(Text(state, "status") == "unopened" && index == 3 && state["receipts"]!.AsArray().Count == 0, 6);
                Require(admissionEnabled, 7); Require(available && now is not null && now <= MaximumUtc - 30000, 5);
                var timing = new JsonObject
                {
                    ["contractVersion"] = 1,
                    ["clockConfigurationHash"] = facts.ClockHash,
                    ["kind"] = "force-assignment",
                    ["decisionBudgetMilliseconds"] = 30000,
                    ["openedAtUnixMilliseconds"] = now,
                    ["deadlineUnixMilliseconds"] = now + 30000,
                    ["openingFloorUnixMilliseconds"] = now,
                };
                var identity = new JsonObject
                {
                    ["baseHash"] = facts.BaseHash,
                    ["selectionSourceHash"] = derived["selectionProof"]!["sourceHash"]!.DeepClone(),
                    ["segmentId"] = state["segmentId"]!.DeepClone(),
                    ["cycleId"] = RoundBoundary(derived)["cycleId"]!.DeepClone(),
                    ["positionId"] = route[3],
                    ["candidate"] = RoundSelection(derived)["selection"]!.DeepClone(),
                    ["declineReceiptId"] = RoundSelection(derived)["declineReceiptId"]!.DeepClone(),
                };
                state["opportunityId"] = "aopp." + RoundDomain("opportunity", Encode(identity))[7..];
                state["roundId"] = "arnd." + RoundDomain("round", Encode(new JsonObject
                {
                    ["baseHash"] = facts.BaseHash,
                    ["opportunityId"] = state["opportunityId"]!.DeepClone(),
                    ["openingAuthorityVersion"] = state["stateVersion"]!.DeepClone(),
                    ["openingHistoryPrefix"] = state["prefix"]!.DeepClone(),
                    ["timing"] = timing.DeepClone(),
                }))[7..];
                foreach (var role in new[] { "attacker", "defender" })
                {
                    var participant = RoundSelection(derived)["selection"]![role]!;
                    state["slots"]!.AsArray().Add(new JsonObject
                    {
                        ["role"] = role,
                        ["owner"] = participant["unit"]!["originalSide"]!.DeepClone(),
                        ["slotId"] = "aslt." + RoundDomain("slot", Encode(new JsonObject { ["roundId"] = state["roundId"]!.DeepClone(), ["role"] = role }))[7..],
                        ["allocation"] = new JsonObject { ["kind"] = "full-close-assault", ["unit"] = participant["unit"]!.DeepClone(), ["componentId"] = participant["componentIds"]![0]!.DeepClone(), ["committedToe"] = 10 },
                        ["sealedReceiptId"] = null,
                        ["sealedAt"] = null,
                    });
                }
                state["status"] = "collecting"; state["timing"] = timing.DeepClone();
                effect = new() { ["kind"] = "round-opened", ["opportunityId"] = state["opportunityId"]!.DeepClone(), ["timing"] = timing.DeepClone(), ["slots"] = state["slots"]!.DeepClone() }; break;
            case "seal-choice":
                Require(Text(state, "status") == "collecting" && index == 3, 6);
                var slot = state["slots"]!.AsArray().FirstOrDefault(x => Text(x!, "slotId") == Text(cmd, "slotId"));
                Require(slot is not null && Text(slot, "owner") == actor, 4); Require(slot!["sealedReceiptId"] is null, 6);
                Require(JsonNode.DeepEquals(cmd["allocation"], slot["allocation"]), 4);
                var sealClock = Gate(state["timing"]!, now, available); Require(sealClock != "expired", 5);
                if (sealClock == "unavailable")
                { effect = new() { ["kind"] = "round-cancelled", ["cause"] = "clock-unavailable", ["timing"] = state["timing"]!.DeepClone() }; author = "system"; }
                else effect = new() { ["kind"] = "choice-sealed", ["slotId"] = slot["slotId"]!.DeepClone(), ["allocation"] = slot["allocation"]!.DeepClone(), ["timing"] = state["timing"]!.DeepClone(), ["prepared"] = state["slots"]!.AsArray().Any(x => x!["sealedReceiptId"] is not null) };
                break;
            case "expire-round":
            case "controller-unavailable":
                var clock = Gate(state["timing"]!, now, available);
                if (kind == "expire-round" && clock == "live") return (prior.DeepClone().AsObject(), null, null, false);
                effect = new() { ["kind"] = "round-cancelled", ["cause"] = kind == "controller-unavailable" ? "controller-unavailable" : clock == "expired" ? "deadline" : "clock-unavailable", ["timing"] = state["timing"]!.DeepClone() }; break;
            case "complete-step":
                Require(Text(state, "status") is "prepared" or "cancelled" && index is >= 3 and <= 5, 6);
                Require(now is null && available, 5); var cancelled = Text(state, "status") == "cancelled";
                Require(index < 5 || cancelled, 7);
                var proofReceipts = cancelled ? new JsonArray(state["cancellationReceiptId"]!.DeepClone()) : new JsonArray(state["slots"]!.AsArray().Select(x => x!["sealedReceiptId"]?.DeepClone()).ToArray());
                Require(proofReceipts.All(x => x is not null), 6);
                effect = new()
                {
                    ["kind"] = "step-completed",
                    ["fromPositionId"] = route[index],
                    ["toPositionId"] = route[index + 1],
                    ["previousStepReceiptId"] = state["stepReceipts"]!.AsArray()[^1]!.DeepClone(),
                    ["proofKind"] = cancelled ? "no-attack" : index == 3 ? "prepared-full-assignment" : "certified-empty-aa",
                    ["proofReceipts"] = proofReceipts,
                    ["certificateHash"] = index == 4 && !cancelled ? EmptyAa(derived, state, dependencyBytes) : null,
                }; break;
            default: Require(now is null && available, 5); throw new JsonException("CMB-MAS-007");
        }
        var acceptedEvent = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = "moved-round-" + Text(effect, "kind"),
            ["author"] = author,
            ["campaignId"] = RoundBoundary(derived)["cycle"]!["campaignId"]!.DeepClone(),
            ["rulesetHash"] = RoundBoundary(derived)["cycle"]!["rulesetHash"]!.DeepClone(),
            ["configurationHash"] = facts.ClockHash,
            ["predecessorConfigurationHash"] = derived["clockConfiguration"]!["parentConfigurationHash"]!.DeepClone(),
            ["selectionSourceHash"] = derived["selectionProof"]!["sourceHash"]!.DeepClone(),
            ["baseHash"] = facts.BaseHash,
            ["cycleId"] = RoundBoundary(derived)["cycleId"]!.DeepClone(),
            ["segmentId"] = state["segmentId"]!.DeepClone(),
            ["roundId"] = state["roundId"]!.DeepClone(),
            ["priorVersion"] = Number(prior, "stateVersion"),
            ["stateVersion"] = checked(Number(prior, "stateVersion") + 1),
            ["priorPrefix"] = prior["prefix"]!.DeepClone(),
            ["input"] = input.DeepClone(),
            ["effect"] = Object(Canonical(effect, "RoundEffect"), EffectKind(effect)),
        };
        var receipt = "arc." + RoundDomain("receipt", Encode(acceptedEvent))[7..]; acceptedEvent["receiptId"] = receipt;
        var bytes = Canonical(acceptedEvent, "RoundEvent");
        switch (Text(effect, "kind"))
        {
            case "round-opened": state["openingReceiptId"] = receipt; break;
            case "choice-sealed":
                var acceptedSlot = state["slots"]!.AsArray().Single(x => Text(x!, "slotId") == Text(effect, "slotId"));
                acceptedSlot!["sealedReceiptId"] = receipt; acceptedSlot["sealedAt"] = now; state["status"] = effect["prepared"]!.GetValue<bool>() ? "prepared" : "collecting"; break;
            case "round-cancelled": state["status"] = "cancelled"; state["cancellationReceiptId"] = receipt; break;
            default: state["stepReceipts"]!.AsArray().Add(receipt); state["stepIndex"] = index + 1; state["closed"] = index + 1 == 6; break;
        }
        state["stateVersion"] = acceptedEvent["stateVersion"]!.DeepClone(); state["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(Text(prior, "prefix"), bytes);
        state["receipts"]!.AsArray().Add(new JsonObject { ["commandHash"] = commandHash, ["eventHash"] = Hash(bytes), ["receiptId"] = receipt, ["actor"] = actor, ["stateVersion"] = state["stateVersion"]!.DeepClone() });
        return (Object(Canonical(state, "RoundControl"), "RoundControl"), bytes, receipt, false);
    }

    private static string EmptyAa(JsonObject derived, JsonObject state, Func<string, byte[]> dependencyBytes)
    {
        var data = PinnedBytes(ContentPath, dependencyBytes);
        using var document = JsonDocument.Parse(data); var content = JsonNode.Parse(document.RootElement.GetRawText())!;
        var candidate = RoundSelection(derived)["selection"]!; var ids = new JsonArray();
        Require(state["slots"]!.AsArray().Count == 2 && state["slots"]!.AsArray().All(x => x!["sealedReceiptId"] is not null), 6);
        foreach (var slot in state["slots"]!.AsArray())
        {
            var participant = candidate[Text(slot!, "role")]!; var allocation = slot!["allocation"]!; var elementId = Text(participant["unit"]!, "elementId");
            var element = content["elements"]!.AsArray().FirstOrDefault(x => Text(x!, "elementId") == elementId);
            var current = state["world"]!["elements"]!.AsArray().FirstOrDefault(x => Text(x!, "elementId") == elementId);
            Require(element is not null && current is not null, 4);
            Require(element!["components"]!.AsArray().Count == 1 && current!["components"]!.AsArray().Count == 1, 4);
            var component = element["components"]![0]!; var live = current!["components"]![0]!;
            Require(Text(component, "componentClassId") == "land.combat-component.infantry" && Number(component, "maximumToe") == 10 &&
                Text(live, "componentId") == Text(component, "componentId") && Text(component, "componentId") == Text(allocation, "componentId") &&
                Number(live, "currentToe") == 10 && Number(allocation, "committedToe") == 10 &&
                JsonNode.DeepEquals(participant["componentIds"], new JsonArray(component["componentId"]!.DeepClone())) && JsonNode.DeepEquals(allocation["unit"], participant["unit"]), 4);
            ids.Add(component["componentId"]!.DeepClone());
        }
        var certificate = new JsonObject
        {
            ["contractVersion"] = 1,
            ["contentHash"] = Hash(data),
            ["worldHash"] = Hash(Canonical(state["world"]!, "World")),
            ["candidate"] = candidate.DeepClone(),
            ["allocations"] = new JsonArray(state["slots"]!.AsArray().Select(x => x!["allocation"]!.DeepClone()).ToArray()),
            ["assignmentReceiptId"] = state["stepReceipts"]!.AsArray()[^1]!.DeepClone(),
            ["componentIds"] = ids,
        };
        return RoundDomain("empty-aa", Canonical(certificate, "EmptyAaCertificate"));
    }
    private static string EffectKind(JsonObject effect) => Text(effect, "kind") switch { "round-opened" => "RoundOpen", "choice-sealed" => "RoundSeal", "round-cancelled" => "RoundCancel", _ => "RoundStep" };
    private static string Gate(JsonNode timing, long? now, bool available) => !available || now is null || now < Number(timing, "openingFloorUnixMilliseconds") ? "unavailable" : now >= Number(timing, "deadlineUnixMilliseconds") ? "expired" : "live";
    private static JsonNode RoundSelection(JsonObject derived) => derived["selectionProof"]!["control"]!;
    private static JsonNode RoundBoundary(JsonObject derived) => derived["selectionProof"]!["boundary"]!;
    private static string[] RoundRoute(JsonObject derived) => Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != Text(RoundBoundary(derived)["position"]!, "positionId")).Take(7).Select(p => p.PositionId).ToArray();
    private static string RoundBaseHash(JsonObject derived) => RoundDomain("base", Canonical(derived, "RoundBase"));
    private static string RoundClockHash(JsonObject derived) => RoundDomain("configuration", Canonical(derived["clockConfiguration"]!, "ClockConfiguration"));

    internal static JsonObject PaidInitial(PaidAssaultFrame ctx)
    {
        var p = ctx.Prior;
        Require(p["randomState"]!["seed"]!.GetValue<ulong>() == 1 && p["randomState"]!["nextByteCursor"]!.GetValue<ulong>() == 2, 4);
        return O(("contractVersion", 1), ("baseHash", "sha256:" + PaidDigest("base", ctx.Base, "PreparedBase")),
            ("roundClockConfigurationHash", p["clockConfigurationHash"]), ("resultClockPolicyId", ClockPolicy), ("stateVersion", N(p, "stateVersion")), ("prefix", p["prefix"]),
            ("status", "prepared"), ("commitmentId", null), ("result", null), ("settlementId", null), ("window", null),
            ("acceptedHighWater", p["timing"]!["openingFloorUnixMilliseconds"]), ("world", p["world"]), ("randomState", p["randomState"]),
            ("attackHistory", new JsonArray()), ("targetUses", new JsonArray()), ("roundClosureReceiptId", null), ("caCompletionReceiptId", null), ("receipts", new JsonArray()), ("closed", false));
    }
    // Isolated arithmetic branches exercise this handler; those states never become ingress authority.
    internal static (JsonObject State, byte[]? Event, string? Receipt) PaidTransition(PaidAssaultFrame ctx, JsonObject prior, JsonObject input, bool admissionEnabled, bool replay = false)
    {
        _ = Canonical(prior, "PaidControl"); _ = Canonical(input, "PaidInput");
        var cmd = input["command"]!; var kind = T(cmd, "kind"); var actor = T(input, "actor"); var now = input["admittedAt"]?.GetValue<long>(); var available = B(input, "clockAvailable");
        Require(kind is "commit" or "resolve" or "advance" or "choose" or "expire" or "unavailable", 3);
        Require(N(cmd, "contractVersion") == 1 && T(cmd, "baseHash") == T(prior, "baseHash") && T(cmd, "roundId") == T(ctx.Prior, "roundId"), 4);
        var structural = kind is "commit" or "resolve" or "advance";
        Require((cmd["expectedPriorVersion"] is not null) == structural && (cmd["choice"] is not null) == (kind == "choose") && (cmd["decisionId"] is null) == structural, 3);
        Require(kind == "choose" ? actor is "axis" or "commonwealth" : actor == "system", 4);
        Require(JsonNode.DeepEquals(cmd["commitmentId"], kind == "commit" ? null : prior["commitmentId"]), 4);
        var ch = Hash(Canonical(cmd, "PaidCommand")); var retained = prior["receipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == ch && T(r!, "actor") == actor);
        if (retained is not null) return (prior.DeepClone().AsObject(), null, T(retained, "receiptId"));
        var window = prior["window"];
        if ((kind is "expire" or "unavailable") && (window is null || !JsonNode.DeepEquals(cmd["decisionId"], window["decisionId"]))) return (prior.DeepClone().AsObject(), null, null);
        Require(!B(prior, "closed") && prior["receipts"]!.AsArray().Count < 11 && N(prior, "stateVersion") < long.MaxValue, 7);
        Require(structural ? N(cmd, "expectedPriorVersion") == N(prior, "stateVersion") : window is not null && JsonNode.DeepEquals(cmd["decisionId"], window["decisionId"]), 6);
        var s = prior.DeepClone().AsObject(); var author = actor; JsonObject? effect = null;
        void ChoiceRecord(string choice, string reason, JsonNode? timing)
        {
            var wk = window is not null ? T(window, "kind") : T(s, "status") == "resolved" ? "retreat" : "custody"; var cut = wk == "retreat" ? "disposition" : "custody";
            s["world"] = MovedProjected(ctx, s, cut, wk == "retreat" ? choice : null, wk == "custody" ? choice : null);
            effect = O(("kind", wk == "retreat" ? "disposition-recorded" : "custody-settled"), ("reason", reason), ("timing", timing), ("payload", s["world"]!["settlements"]![0]![cut]));
            s["status"] = cut; s["window"] = null;
        }
        if (kind == "commit")
        {
            Require(T(prior, "status") == "prepared" && now is null && available, 6); if (!replay) { Require(admissionEnabled, 7); CertifyMovedSupport(ctx); }
            var (paid, costs) = CumulativePaidWorld(ctx); var allocations = A(ctx.Prior["slots"]!.AsArray().Select(x => x!["allocation"]));
            var preimage = O(("baseHash", prior["baseHash"]), ("priorVersion", prior["stateVersion"]), ("priorPrefix", prior["prefix"]), ("roundId", ctx.Prior["roundId"]),
                ("allocations", allocations), ("costs", costs), ("preResultRandomState", prior["randomState"]));
            var commitment = "cmt." + PaidDigest("commitment", preimage, "CommitmentPreimage");
            effect = O(("kind", "attack-committed"), ("commitmentId", commitment), ("allocations", allocations), ("costs", costs), ("preResultRandomState", prior["randomState"]));
            s["world"] = paid; s["commitmentId"] = commitment; s["status"] = "committed";
            s["attackHistory"] = new JsonArray(O(("commitmentId", commitment), ("cycleId", ctx.Boundary["cycleId"]), ("segmentId", ctx.Prior["segmentId"]),
                ("attacker", ctx.Selection["attacker"]!["unit"]), ("defender", ctx.Selection["defender"]!["unit"]), ("targetLocationId", ctx.Selection["targetLocationId"]), ("gameTurn", ctx.Cycle["gameTurn"]), ("operationStage", ctx.Cycle["operationStage"])));
            s["targetUses"] = new JsonArray(O(("commitmentId", commitment), ("segmentId", ctx.Prior["segmentId"]), ("targetLocationId", ctx.Selection["targetLocationId"])));
        }
        else if (kind is "choose" or "expire" or "unavailable")
        {
            Require(window is not null, 6); var choices = T(window!, "kind") == "retreat" ? new[] { "retreat", "refuse-retreat" } : ["relocate-and-guard", "leave-unguarded"];
            if (kind == "choose") Require(actor == T(window!, "owner") && choices.Contains(T(cmd, "choice"), StringComparer.Ordinal), 4);
            var timing = window!["timing"]!.DeepClone(); var lost = !available || now is null || now < N(timing, "highWaterUnixMilliseconds"); var late = now is not null && now >= N(timing, "deadlineUnixMilliseconds");
            if (kind == "expire" && !lost && !late) return (prior.DeepClone().AsObject(), null, null); if (kind == "choose" && !lost) Require(!late, 5);
            var fallback = kind != "choose" || lost;
            if (!lost) { s["acceptedHighWater"] = Math.Max(N(s, "acceptedHighWater"), now!.Value); timing["highWaterUnixMilliseconds"] = Math.Max(N(timing, "highWaterUnixMilliseconds"), now.Value); }
            var reason = fallback ? lost ? "clock-unavailable" : kind == "unavailable" ? "controller-unavailable" : "deadline" : "owner-choice";
            if (fallback) author = "system"; ChoiceRecord(fallback ? choices[^1] : T(cmd, "choice"), reason, timing);
        }
        else if (kind == "resolve")
        {
            Require(T(prior, "status") == "committed" && now is null && available, 6); var result = Resolve(s); s["result"] = result; s["randomState"] = result["afterRandomState"]!.DeepClone(); s["world"] = MovedProjected(ctx, s, "resolved");
            s["settlementId"] = s["world"]!["settlements"]![0]!["settlementId"]!.DeepClone(); s["status"] = "resolved"; effect = O(("kind", "assault-resolved"), ("result", result), ("settlementId", s["settlementId"]));
        }
        else
        {
            Require(window is null, 6); var status = T(s, "status"); var needsRetreat = status == "resolved" && N(s["result"]!["facts"]!, "requiredRetreat") > 0; var needsCustody = status == "retreat" && s["world"]!["custodyLots"]!.AsArray().Any(x => T(x!, "status") == "pending");
            if (needsRetreat || needsCustody)
            {
                var wk = needsRetreat ? "retreat" : "custody"; var owner = needsRetreat ? T(ctx.Selection["defender"]!["unit"]!, "originalSide") : T(s["world"]!["custodyLots"]![0]!["captor"]!, "originalSide");
                var opened = MakeWindow(ctx, wk, owner, T(s, "settlementId"), now, available);
                if (opened is null) ChoiceRecord(needsRetreat ? "refuse-retreat" : "leave-unguarded", "opening-clock-unavailable", null);
                else { s["acceptedHighWater"] = Math.Max(N(s, "acceptedHighWater"), now!.Value); s["window"] = opened; s["status"] = "waiting-" + wk; effect = O(("kind", "choice-opened"), ("window", opened)); }
            }
            else
            {
                Require(now is null && available, 3);
                if (status == "resolved") ChoiceRecord("not-required", "not-required", null);
                else if (status is "disposition" or "losses" or "retreat" or "custody")
                {
                    var cut = status == "disposition" ? "losses" : status == "losses" ? "retreat" : "relationships"; s["world"] = MovedProjected(ctx, s, cut); s["status"] = cut; effect = O(("kind", cut + "-settled"), ("payload", s["world"]!["settlements"]![0]![cut]));
                }
                else if (status == "relationships")
                {
                    var st = s["world"]!["settlements"]![0]!; Require(MandatoryCuts.All(k => st[k] is not null) && !s["world"]!["custodyLots"]!.AsArray().Any(x => T(x!, "status") == "pending"), 7);
                    s["status"] = "round-closed"; effect = O(("kind", "round-closed"), ("settlementId", s["settlementId"]), ("proofReceipts", A(SettlementCuts.Where(k => st[k] is not null).Select(k => st[k]!["receiptId"]))));
                }
                else if (status == "round-closed")
                {
                    Require(s["roundClosureReceiptId"] is not null && ctx.Prior["stepReceipts"]!.AsArray().Count == 5, 7); var route = Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != T(ctx.Boundary["position"]!, "positionId")).Take(7).Select(p => p.PositionId).ToArray();
                    Require(route.Length == 7, 7); s["status"] = "closed"; s["closed"] = true; effect = O(("kind", "ca-completed"), ("fromPositionId", route[5]), ("toPositionId", route[6]), ("previousStepReceiptId", ctx.Prior["stepReceipts"]!.AsArray()[^1]), ("roundClosureReceiptId", s["roundClosureReceiptId"]));
                }
                else throw new JsonException("CMB-MAS-006");
            }
        }
        var cycle = ctx.Cycle;
        var acceptedEvent = O(("contractVersion", 1), ("eventType", "moved-paid-" + T(effect!, "kind")), ("author", author), ("campaignId", cycle["campaignId"]),
            ("rulesetHash", cycle["rulesetHash"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]), ("roundClockConfigurationHash", prior["roundClockConfigurationHash"]), ("resultClockPolicyId", ClockPolicy),
            ("preparedSourceHash", ctx.Base["preparedSourceHash"]), ("baseHash", prior["baseHash"]), ("cycleId", ctx.Boundary["cycleId"]), ("segmentId", ctx.Prior["segmentId"]), ("roundId", ctx.Prior["roundId"]),
            ("priorVersion", prior["stateVersion"]), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", JsonNode.Parse(Canonical(input, "PaidInput"))), ("effect", JsonNode.Parse(Canonical(effect!, "PaidEffect"))));
        var receipt = "cmb." + Domain("sandtable.combat.actual-moved-assault.paid.receipt.v1", Encode(acceptedEvent)); acceptedEvent["receiptId"] = receipt; var data = Canonical(acceptedEvent, "PaidEvent"); var tag = T(effect!, "kind");
        if (tag == "round-closed") s["roundClosureReceiptId"] = receipt; if (tag == "ca-completed") s["caCompletionReceiptId"] = receipt;
        s["stateVersion"] = acceptedEvent["stateVersion"]!.DeepClone(); s["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(T(prior, "prefix"), data);
        s["receipts"]!.AsArray().Add(O(("commandHash", ch), ("eventHash", Hash(data)), ("receiptId", receipt), ("actor", actor), ("stateVersion", s["stateVersion"]))); _ = Canonical(s, "PaidControl"); return (s, data, receipt);
    }
}
