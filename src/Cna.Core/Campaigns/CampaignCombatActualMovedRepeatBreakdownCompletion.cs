using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Cna.Core.Campaigns.CampaignCombatActualMovedRepeatBreakdownCompletionCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedRepeatBreakdownCompletionResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    internal CombatActualMovedRepeatBreakdownCompletionResult(JsonObject packet, JsonObject basis, JsonObject state, byte[]? ev = null, bool duplicate = false, string? receiptId = null)
    {
        source = Canonical(packet, "Source"); control = Canonical(state, "Control"); accepted = ev?.ToArray(); Duplicate = duplicate;
        ReceiptId = receiptId ?? (ev is null ? null : JsonNode.Parse(ev)!["receiptId"]!.GetValue<string>());
        var sourceId = HashWithDomain("sandtable.combat.actual-moved-repeat-breakdown-completion.source.v1", source)[7..];
        proof = Canonical(new JsonObject { ["contractVersion"] = 1, ["sourceId"] = "mrbcomp." + sourceId, ["sourceHash"] = Hash(source), ["base"] = basis.DeepClone(), ["control"] = state.DeepClone() }, "Proof");
    }
    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => accepted?.ToArray();
    public bool Duplicate { get; }
    public string? ReceiptId { get; }
    public JsonElement Control { get { using var document = JsonDocument.Parse(control); return document.RootElement.Clone(); } }
    public JsonElement Proof { get { using var document = JsonDocument.Parse(proof); return document.RootElement.Clone(); } }
}

/// <summary>Private R03 continuation over a fully authenticated R02 terminal.</summary>
internal static class CampaignCombatActualMovedRepeatBreakdownCompletion
{
    private sealed record Frame(JsonObject Packet, JsonObject Base, string BaseHash, JsonObject Parent, JsonObject Boundary, JsonObject Successor, JsonObject Cycle, string CycleId, CampaignCombatCreationRequest Request, byte[][][] Ledgers, Func<string, byte[]> Dependencies);
    private const string Kind = "complete-breakdown-segment";
    private static string ActionId => Hash(Encode(O(("contractVersion", 1), ("kind", Kind), ("operationStage", 1))));

    public static CombatActualMovedRepeatBreakdownCompletionResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    { var (frame, state) = Replay(source, request, [prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion], own, dependencies); return new(frame.Packet, frame.Base, state); }

    public static CombatActualMovedRepeatBreakdownCompletionResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, IReadOnlyList<byte[]> own, ReadOnlySpan<byte> input,
        Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var owned = Copy(input, "Input"); var (frame, prior) = Replay(source, request, [prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion], own, dependencies);
        var (after, ev, receipt) = Transition(frame, prior, Object(owned, "Input"), admissionEnabled);
        if (ev is null) return new(frame.Packet, frame.Base, prior, duplicate: true, receiptId: receipt);
        var packet = frame.Packet.DeepClone().AsObject(); packet["breakdownEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var ledger = own.Select(b => b.ToArray()).Append(owned).ToArray();
        Require(Encode(new JsonArray(ledger.Select(x => (JsonNode?)JsonNode.Parse(x)).ToArray())).Length <= 8192, 1);
        var (rebuiltFrame, rebuilt) = Replay(Canonical(packet, "Source"), frame.Request, frame.Ledgers, ledger, frame.Dependencies);
        Require(JsonNode.DeepEquals(after, rebuilt), 6); return new(rebuiltFrame.Packet, rebuiltFrame.Base, rebuilt, ev, receiptId: receipt);
    }

    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]>[] parents, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    { var (frame, _) = Replay(source, request, parents, own, dependencies); return Canonical(frame.Packet, "Source"); }

    public static CombatActualMovedRepeatBreakdownCompletionResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]>[] parents, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    { var bytes = Copy(claim, "Control"); var (f, state) = Replay(source, request, parents, own, dependencies); _ = Object(bytes, "Control"); var result = new CombatActualMovedRepeatBreakdownCompletionResult(f.Packet, f.Base, state); Require(bytes.AsSpan().SequenceEqual(result.ControlBytes), 6); return result; }

    public static CombatActualMovedRepeatBreakdownCompletionResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]>[] parents, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    { var bytes = Copy(claim, "Proof"); var (f, state) = Replay(source, request, parents, own, dependencies); _ = Object(bytes, "Proof"); var result = new CombatActualMovedRepeatBreakdownCompletionResult(f.Packet, f.Base, state); Require(bytes.AsSpan().SequenceEqual(result.ProofBytes), 6); return result; }

    internal static JsonObject Command(JsonObject state)
    {
        var binding = O(("baseHash", state["baseHash"]), ("cycleId", state["cycleId"]), ("kind", Kind), ("stateVersion", state["stateVersion"]),
            ("sequencePosition", JsonNode.Parse(Canonical(state["sequencePosition"]!, "Position"))), ("movementCompletionReceiptId", state["movementEnd"]!["completionReceiptId"]));
        var capability = HashWithDomain("sandtable.combat.actual-moved-repeat-breakdown-completion.capability.v1", Encode(binding));
        return O(("command", O(("contractVersion", 1), ("kind", Kind), ("operationStage", 1), ("actionId", ActionId),
            ("baseHash", state["baseHash"]), ("cycleId", state["cycleId"]), ("expectedPositionId", state["sequencePosition"]!["positionId"]),
            ("expectedPriorVersion", state["stateVersion"]), ("capabilityId", capability))), ("actor", "system"));
    }

    private static (Frame Frame, JsonObject State) Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]>[] parents, IReadOnlyList<byte[]> own, Func<string, byte[]> dependencies)
    {
        var packet = Object(source, "Source"); SourceBounds(packet);
        Require(parents is not null && parents.Length == 10 && parents.All(x => x is not null), 1);
        var ledgers = parents.Select(p => Own(p, p.Count, "AncestorInput")).ToArray();
        var retained = SnapshotDependencies(dependencies);
        var parentBytes = Encoding.ASCII.GetBytes(packet["movementCompletionSourceCanonicalUtf8"]!.GetValue<string>());
        var parent = Inherited(() => CampaignCombatActualMovedRepeatMovementCompletion.ReplayTrustedSource(parentBytes, request,
            ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], ledgers[9], retained));
        var state = JsonNode.Parse(parent.ControlBytes)!.AsObject(); var cycle = state["activeCycle"]!.DeepClone().AsObject();
        Require(state["profile"]!.GetValue<string>() is "paid" or "armed" && N(cycle, "gameTurn") == 1 && N(cycle, "operationStage") == 1 &&
            T(cycle, "playerPhaseSlot") == "first-acting-side" && N(cycle, "ordinal") == 2 && T(state, "phase") == "breakdown" && B(state, "closed") &&
            T(state["flow"]!, "kind") == "idle" && state["interruptContext"] is null && state["movementEnd"] is not null, 3);
        Require(N(state["movementEnd"]!, "ordinal") == 2 && N(state["originalMovementEnd"]!, "ordinal") == 1 &&
            T(state["movementEnd"]!, "completionReceiptId") == T(state["lifecycleReceipts"]!.AsArray()[^1]!, "receiptId") &&
            ledgers[9].Length == (state["tracks"]!.AsArray().Count == 0 ? 1 : 3), 4);
        var catalog = JsonNode.Parse(JsonNode.Parse(retained("docs/specs/fixtures/combat-cycle-sequence-v1.json"))!["predecessor"]!["canonicalUtf8"]!.GetValue<string>())!.AsObject();
        foreach (var position in catalog["positions"]!.AsArray().Concat(catalog["interruptPositions"]!.AsArray())) position!["contractVersion"] = 5;
        var positions = catalog["positions"]!.AsArray(); var boundary = state["sequencePosition"]!.DeepClone().AsObject();
        var index = positions.Select((p, i) => (p, i)).Where(x => JsonNode.DeepEquals(x.p, boundary)).Select(x => x.i).DefaultIfEmpty(-1).Single();
        Require(index >= 0 && index + 1 < positions.Count, 4); var successor = positions[index + 1]!.DeepClone().AsObject();
        Require(T(successor, "positionId").EndsWith(".combat.position-determination", StringComparison.Ordinal), 4);
        var basis = O(("contractVersion", 1), ("dependencyManifestHash", ManifestHash), ("movedRepeatMovementCompletionProof", JsonNode.Parse(parent.ProofBytes)));
        var baseHash = HashWithDomain("sandtable.combat.actual-moved-repeat-breakdown-completion.base.v1", Canonical(basis, "Base"));
        Require(N(state, "stateVersion") < long.MaxValue, 7);
        var frame = new Frame(packet, basis, baseHash, state, boundary, successor, cycle, T(state, "cycleId"), request, ledgers, retained);
        Require(own is not null && own.Count <= 1, 1);
        var inputs = own.Select(x => Object(Copy(x, "Input"), "Input")).ToArray();
        Require(Encode(new JsonArray(inputs.Select(x => (JsonNode?)x.DeepClone()).ToArray())).Length <= 8192, 1);
        Require(inputs.Length == packet["breakdownEventCanonicalUtf8"]!.AsArray().Count, 4);
        var current = Initial(frame);
        for (var i = 0; i < inputs.Length; i++)
        {
            var actual = Object(Encoding.ASCII.GetBytes(packet["breakdownEventCanonicalUtf8"]![i]!.GetValue<string>()), "Event");
            var (after, expected, _) = Transition(frame, current, inputs[i], true);
            Require(expected is not null && Canonical(actual, "Event").AsSpan().SequenceEqual(expected), 6); current = after;
        }
        return (frame, current);
    }

    private static JsonObject Initial(Frame frame)
    {
        var state = frame.Parent.DeepClone().AsObject(); state["r02BaseHash"] = state["baseHash"]!.DeepClone(); state["r02SourceHash"] = Hash(Encoding.ASCII.GetBytes(frame.Packet["movementCompletionSourceCanonicalUtf8"]!.GetValue<string>()));
        state["baseHash"] = frame.BaseHash; state["breakdownReceipts"] = new JsonArray(); state["breakdownCompletionReceiptId"] = null; state["closed"] = false;
        return JsonNode.Parse(Canonical(state, "Control"))!.AsObject();
    }

    private static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame frame, JsonObject prior, JsonObject input, bool admission)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); var cmd = input["command"]!;
        Require(T(cmd, "kind") == Kind && N(cmd, "operationStage") == 1, 3);
        Require(T(cmd, "baseHash") == T(prior, "baseHash") && T(prior, "baseHash") == frame.BaseHash &&
            T(cmd, "cycleId") == T(prior, "cycleId") && T(prior, "cycleId") == frame.CycleId &&
            T(cmd, "expectedPositionId") == T(frame.Boundary, "positionId") && JsonNode.DeepEquals(prior["activeCycle"], frame.Cycle), 4);
        Require(T(input, "actor") == "system", 4); Require(T(cmd, "actionId") == ActionId, 5);
        var commandHash = Hash(Canonical(cmd, "Command"));
        var duplicate = prior["breakdownReceipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == commandHash && T(r!, "actor") == "system");
        if (duplicate is not null) return (prior.DeepClone().AsObject(), null, T(duplicate, "receiptId"));
        Require(admission && !B(prior, "closed") && prior["breakdownReceipts"]!.AsArray().Count == 0 && N(prior, "stateVersion") < long.MaxValue, 7);
        Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion") && JsonNode.DeepEquals(prior["sequencePosition"], frame.Boundary) &&
            T(prior["flow"]!, "kind") == "idle" && prior["interruptContext"] is null && prior["breakdownCompletionReceiptId"] is null, 6);
        Require(T(cmd, "capabilityId") == T(Command(prior)["command"]!, "capabilityId"), 5);
        var effect = O(("kind", "breakdown-completed"), ("movementCompletionReceiptId", prior["movementEnd"]!["completionReceiptId"]),
            ("sources", frame.Boundary["sources"]), ("successorPosition", frame.Successor));
        var ev = O(("contractVersion", 1), ("eventType", "combat-actual-moved-repeat-breakdown-completed"), ("author", "system"),
            ("campaignId", frame.Cycle["campaignId"]), ("rulesetHash", frame.Cycle["rulesetHash"]),
            ("configurationHash", frame.Cycle["admittedPolicyBundleDigest"]), ("profile", prior["profile"]),
            ("movementCompletionSourceHash", prior["r02SourceHash"]), ("baseHash", frame.BaseHash), ("cycleId", frame.CycleId),
            ("positionId", prior["sequencePosition"]!["positionId"]), ("priorVersion", prior["stateVersion"]),
            ("stateVersion", checked(N(prior, "stateVersion") + 1)), ("priorPrefix", prior["prefix"]),
            ("input", JsonNode.Parse(Canonical(input, "Input"))), ("effect", JsonNode.Parse(Canonical(effect, "Effect"))));
        var receipt = "mrbc." + HashWithDomain("sandtable.combat.actual-moved-repeat-breakdown-completion.receipt.v1", Encode(ev))[7..];
        ev["receiptId"] = receipt; var bytes = Canonical(ev, "Event"); var after = prior.DeepClone().AsObject();
        after["stateVersion"] = ev["stateVersion"]!.DeepClone(); after["prefix"] = EventPrefix(T(prior, "prefix"), bytes);
        after["sequencePosition"] = frame.Successor.DeepClone(); after["breakdownCompletionReceiptId"] = receipt; after["closed"] = true;
        var record = O(("commandHash", commandHash), ("eventHash", Hash(bytes)), ("receiptId", receipt), ("actor", "system"), ("stateVersion", after["stateVersion"]));
        after["receipts"]!.AsArray().Add(record.DeepClone()); after["breakdownReceipts"]!.AsArray().Add(record);
        _ = Canonical(after, "Control"); return (after, bytes, receipt);
    }

    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static string T(JsonNode node, string key) => node[key]!.GetValue<string>();
    private static long N(JsonNode node, string key) => JsonSerializer.SerializeToElement(node[key]).GetInt64();
    private static bool B(JsonNode node, string key) => node[key]!.GetValue<bool>();
}
