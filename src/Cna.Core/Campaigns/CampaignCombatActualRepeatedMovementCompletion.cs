using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedMovementCompletionCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualRepeatedMovementCompletionResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualRepeatedMovementCompletionResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> lifecycleInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = lifecycleInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> LifecycleInputs => inputs.Select(x => x.ToArray()).ToArray();
    public byte[]? EventBytes => acceptedEvent?.ToArray();
    public JsonElement Control => Read(control);
    public JsonElement Proof => Read(proof);
    public string? ReceiptId { get; }
    public bool Duplicate { get; }

    private static JsonElement Read(byte[] data)
    {
        using var document = JsonDocument.Parse(data);
        return document.RootElement.Clone();
    }
}

/// <summary>Private native lifecycle completion from authenticated actual repeated Movement.</summary>
internal static class CampaignCombatActualRepeatedMovementCompletion
{
    internal sealed record Frame(JsonObject Base, string BaseHash, JsonObject Prior, JsonObject Cycle, JsonObject Flow, JsonObject Content, int EventCap);
    private static readonly string[] Preserved = ["originalCycle", "activeCycle", "closure", "members", "world", "randomState", "attackHistory", "targetUses", "nextCycleProgress", "tracks"];
    public static CombatActualRepeatedMovementCompletionResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, movement, lifecycle, dependencies);
        return Result(packet, frame, state, inputs);
    }
    public static CombatActualRepeatedMovementCompletionResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, movement, lifecycle, dependencies);
        var owned = newInput.ToArray(); var (after, accepted, receipt) = Transition(frame, state, Object(owned, "Input"));
        if (accepted is not null) { packet["lifecycleEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted)); inputs.Add(owned); }
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1); CampaignCombatActualRepeatedMovementCompletionCodec.VerifyDependencies(dependencies);
        return Result(packet, frame, after, inputs, accepted, receipt, accepted is null);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> lifecycle, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, dependencies).SourceBytes;
    public static CombatActualRepeatedMovementCompletionResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, dependencies);
        _ = Parse(claim, "Control"); Require(claim.SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualRepeatedMovementCompletionResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, dependencies);
        _ = Parse(claim, "Proof"); Require(claim.SequenceEqual(result.ProofBytes), 6); return result;
    }
    private static (JsonObject Packet, Frame Frame, JsonObject State, List<byte[]> Inputs) Replay(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, Func<string, byte[]> dependencies)
    {
        CampaignCombatActualRepeatedMovementCompletionCodec.VerifyDependencies(dependencies); var packet = Object(source, "Source");
        var frame = Derive(Encoding.ASCII.GetBytes(T(packet, "movementSourceCanonicalUtf8")), context, selection, rounds, paid, continuation, movement, dependencies);
        Require(lifecycle is not null && lifecycle.Count <= frame.EventCap && lifecycle.All(x => x is not null), 1);
        var inputs = lifecycle.Select(x => { var copy = x.ToArray(); _ = Parse(copy, "Input"); return copy; }).ToList();
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1);
        var events = packet["lifecycleEventCanonicalUtf8"]!.AsArray(); Require(events.Count == inputs.Count, 4); var state = Initial(frame);
        for (var i = 0; i < inputs.Count; i++)
        {
            var actual = Encoding.ASCII.GetBytes(events[i]!.GetValue<string>()); _ = Parse(actual, "Event");
            var (after, expected, _) = Transition(frame, state, Object(inputs[i], "Input"));
            Require(expected is not null && actual.AsSpan().SequenceEqual(expected), 6); state = after;
        }
        CampaignCombatActualRepeatedMovementCompletionCodec.VerifyDependencies(dependencies); return (packet, frame, state, inputs);
    }
    private static CombatActualRepeatedMovementCompletionResult Result(JsonObject packet, Frame frame, JsonObject state,
        IReadOnlyList<byte[]> inputs, byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var source = Canonical(packet, "Source"); var digest = Digest("source", source);
        var proof = O(("contractVersion", 1), ("sourceId", "armcomp." + digest), ("sourceHash", "sha256:" + digest), ("base", frame.Base), ("control", state));
        return new(source, Canonical(state, "Control"), Canonical(proof, "Proof"), inputs, accepted, receipt, duplicate);
    }
    internal static Frame Derive(byte[] source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation,
        IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    {
        CombatActualRepeatedMovementResult ancestor;
        try { ancestor = CampaignCombatActualRepeatedMovement.ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, dependencies); }
        catch (JsonException error)
        {
            var code = error.Message.StartsWith("CMB-RMV-", StringComparison.Ordinal) && int.TryParse(error.Message.AsSpan(8, 3), out var n) ? n : 4;
            throw new JsonException($"CMB-RMC-{code:000}", error);
        }
        var prior = JsonNode.Parse(ancestor.ControlBytes)!.AsObject(); var cycle = prior["activeCycle"]!.AsObject(); var count = ancestor.MovementInputs.Count;
        Require(N(cycle, "ordinal") == 2 && N(prior["movementEnd"]!, "ordinal") == 1 && count <= 4 && N(prior, "stateVersion") == 37 + count && T(prior, "positionId") == "land.position.operation-1.first-player.movement-and-combat.movement", 3);
        var members = prior["members"]!.AsArray(); Require(members.Count == 1 && T(members[0]!, "status") == "none" && members[0]!["history"]!.AsObject().All(k => k.Key == "scope" || k.Value is null) && prior["targetUses"]!.AsArray().Count == 0, 3);
        Require(prior["tracks"]!.AsArray().Count == (count > 0 ? 1 : 0) && prior["receipts"]!.AsArray().Count == count && prior["nextCycleProgress"]!.AsArray().Count == count, 4);
        var content = JsonNode.Parse(CampaignCombatActualRepeatedMovementCompletionCodec.Consume("docs/specs/fixtures/combat-content-v7.canonical.json", dependencies))!.AsObject();
        Require(members.All(m => content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(m!["unit"]!, "elementId"))!["breakdownVehicleCohort"] is null), 3);
        JsonObject flow;
        if (count == 0) flow = O(("kind", "idle"));
        else
        {
            var track = prior["tracks"]![0]!; var route = track["route"]!.AsArray();
            var element = prior["world"]!["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(members[0]!["unit"]!, "elementId"))!;
            Require(JsonNode.DeepEquals(track["unit"], members[0]!["unit"]) && route.Count == count + 1 && route[^1]!.GetValue<string>() == T(element, "currentLocationId"), 4);
            flow = O(("kind", "moving"), ("track", track), ("lastMoveReceiptId", prior["receipts"]!.AsArray()[^1]!["receiptId"]));
        }
        var basis = O(("contractVersion", 1), ("dependencyManifestHash", Manifest()), ("movementProof", JsonNode.Parse(ancestor.ProofBytes))); _ = Canonical(basis, "Base");
        Require(N(prior, "stateVersion") <= long.MaxValue - (count > 0 ? 3 : 1), 7);
        return new(basis, "sha256:" + Digest("base", Canonical(basis, "Base")), prior, cycle.DeepClone().AsObject(), flow, content, count > 0 ? 3 : 1);
    }
    internal static JsonObject Initial(Frame frame)
    {
        var p = frame.Prior; var state = O(("contractVersion", 1), ("baseHash", frame.BaseHash), ("movementSourceHash", frame.Base["movementProof"]!["sourceHash"]), ("stateVersion", p["stateVersion"]), ("prefix", p["prefix"]), ("positionId", p["positionId"]));
        foreach (var field in Preserved) state[field] = p[field]!.DeepClone();
        state["priorMovementEnd"] = p["movementEnd"]!.DeepClone(); state["movementEnd"] = null; state["movementReceipts"] = p["receipts"]!.DeepClone(); state["flow"] = frame.Flow.DeepClone(); state["interruptContext"] = null; state["receipts"] = new JsonArray(); state["closed"] = false; return state;
    }
    internal static JsonObject Command(Frame frame, JsonObject state, string kind)
        => O(("command", O(("contractVersion", 1), ("kind", kind), ("baseHash", frame.BaseHash), ("cycleId", CycleId(frame.Cycle)), ("expectedPositionId", ExpectedPosition(frame, kind)), ("expectedPriorVersion", state["stateVersion"]), ("capabilityId", Capability(frame, state, kind)))), ("actor", kind == "resolve-breakdown-stop" ? "system" : T(frame.Cycle, "actingSide")));
    private static string ExpectedPosition(Frame frame, string kind) => kind == "resolve-breakdown-stop" ? "land.position.breakdown-stop" : T(frame.Prior, "positionId");
    private static string Capability(Frame frame, JsonObject state, string kind)
        => "sha256:" + Digest("capability", Encode(O(("baseHash", frame.BaseHash), ("cycleId", CycleId(frame.Cycle)), ("kind", kind), ("stateVersion", state["stateVersion"]), ("positionId", state["positionId"]), ("flow", JsonNode.Parse(Canonical(state["flow"]!, "Flow"))), ("interruptContext", state["interruptContext"] is null ? null : JsonNode.Parse(Canonical(state["interruptContext"]!, "Context"))))));
    internal static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame frame, JsonObject prior, JsonObject input)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); var cmd = input["command"]!; var kind = T(cmd, "kind"); var actor = T(input, "actor"); var cycle = frame.Cycle;
        Require(kind is "stop-element-movement" or "resolve-breakdown-stop" or "complete-movement-segment", 3);
        Require(T(cmd, "baseHash") == T(prior, "baseHash") && T(prior, "baseHash") == frame.BaseHash && T(cmd, "cycleId") == CycleId(cycle) && T(cmd, "expectedPositionId") == ExpectedPosition(frame, kind) && JsonNode.DeepEquals(prior["activeCycle"], cycle), 4);
        Require(actor == (kind == "resolve-breakdown-stop" ? "system" : T(cycle, "actingSide")), 4); var commandHash = Hash(Canonical(cmd, "Command"));
        var old = prior["receipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == commandHash && T(r!, "actor") == actor);
        if (old is not null) return (prior.DeepClone().AsObject(), null, T(old, "receiptId"));
        Require(!B(prior, "closed") && prior["receipts"]!.AsArray().Count < frame.EventCap && N(prior, "stateVersion") < long.MaxValue, 7);
        Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion") && T(cmd, "expectedPositionId") == T(prior, "positionId"), 6); Require(T(cmd, "capabilityId") == Capability(frame, prior, kind), 5);
        var state = prior.DeepClone().AsObject(); var flow = prior["flow"]!; var movement = T(frame.Prior, "positionId");
        var context = O(("cycle", cycle), ("cycleId", CycleId(cycle)), ("suspendedPositionId", movement)); JsonObject effect;
        if (kind == "stop-element-movement")
        {
            Require(T(flow, "kind") == "moving" && prior["interruptContext"] is null && prior["movementEnd"] is null && T(prior, "positionId") == movement, 6);
            Require(JsonNode.DeepEquals(flow, frame.Flow) && JsonNode.DeepEquals(flow["track"], prior["tracks"]![0]) && T(flow, "lastMoveReceiptId") == T(prior["movementReceipts"]!.AsArray()[^1]!, "receiptId"), 4);
            var descriptor = O(("recordedStateVersion", N(prior, "stateVersion") + 1), ("track", flow["track"]), ("lastMoveReceiptId", flow["lastMoveReceiptId"]), ("reason", "deliberate"), ("weatherKind", "normal"), ("cohortInputs", new JsonArray()));
            var sid = "armstop." + Digest("stop", Encode(O(("baseHash", frame.BaseHash), ("cycleId", CycleId(cycle)), ("descriptor", descriptor))));
            var stop = O(("stopId", sid)); foreach (var field in descriptor) stop[field.Key] = field.Value?.DeepClone();
            state["flow"] = O(("kind", "phasing-stop"), ("stop", stop)); state["interruptContext"] = context.DeepClone(); state["positionId"] = "land.position.breakdown-stop";
            effect = O(("kind", "movement-stopped"), ("stop", stop), ("interruptContext", context), ("successorPositionId", state["positionId"]));
        }
        else if (kind == "resolve-breakdown-stop")
        {
            Require(T(flow, "kind") == "phasing-stop" && JsonNode.DeepEquals(prior["interruptContext"], context) && T(prior, "positionId") == "land.position.breakdown-stop" && prior["movementEnd"] is null, 6);
            var stop = flow["stop"]!; Require(JsonNode.DeepEquals(stop["track"], frame.Flow["track"]) && JsonNode.DeepEquals(stop["lastMoveReceiptId"], frame.Flow["lastMoveReceiptId"]) && stop["cohortInputs"]!.AsArray().Count == 0 && N(stop, "recordedStateVersion") == N(prior, "stateVersion"), 4);
            state["flow"] = O(("kind", "idle")); state["interruptContext"] = null; state["positionId"] = movement;
            effect = O(("kind", "stop-resolved"), ("stopId", stop["stopId"]), ("beforeRandomState", prior["randomState"]), ("afterRandomState", prior["randomState"]), ("checks", new JsonArray()), ("createdLots", new JsonArray()), ("sources", new JsonArray("spi-1979-land-rules:21.24-21.26")), ("successorPositionId", movement));
        }
        else
        {
            Require(JsonNode.DeepEquals(flow, O(("kind", "idle"))) && prior["interruptContext"] is null && prior["movementEnd"] is null && T(prior, "positionId") == movement, 6);
            if (frame.EventCap == 3) Require(prior["receipts"]!.AsArray().Count == 2, 6);
            var preview = ExpireMovement(frame, prior, "pending");
            var terminal = Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.breakdown-determination").PositionId;
            state["positionId"] = terminal; state["closed"] = true;
            effect = O(("kind", "movement-completed"), ("endLocations", preview["endLocations"]), ("excludedUnits", preview["excludedBefore"]), ("progress", prior["nextCycleProgress"]), ("successorPositionId", terminal));
        }
        var ev = O(("contractVersion", 1), ("eventType", "combat-actual-repeated-" + T(effect, "kind")), ("author", actor), ("campaignId", cycle["campaignId"]), ("rulesetHash", cycle["rulesetHash"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]), ("movementSourceHash", prior["movementSourceHash"]), ("baseHash", frame.BaseHash), ("cycleId", CycleId(cycle)), ("positionId", prior["positionId"]), ("priorVersion", N(prior, "stateVersion")), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", JsonNode.Parse(Canonical(input, "Input"))), ("effect", JsonNode.Parse(Canonical(effect, "Effect"))));
        var receipt = "armc." + Digest("receipt", Encode(ev)); ev["receiptId"] = receipt; var bytes = Canonical(ev, "Event");
        if (kind == "complete-movement-segment") state["movementEnd"] = ExpireMovement(frame, prior, receipt);
        state["stateVersion"] = ev["stateVersion"]!.DeepClone(); state["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(T(prior, "prefix"), bytes);
        state["receipts"]!.AsArray().Add(O(("commandHash", commandHash), ("eventHash", Hash(bytes)), ("receiptId", receipt), ("actor", actor), ("stateVersion", state["stateVersion"]))); _ = Canonical(state, "Control"); return (state, bytes, receipt);
    }
    // Bounded native port of the already pinned pure expiry kernel for the admitted NONE profile.
    internal static JsonObject ExpireMovement(Frame frame, JsonObject state, string receipt)
    {
        var cycle = frame.Cycle; var old = state["priorMovementEnd"]!; var world = state["world"]!;
        var locations = A(world["elements"]!.AsArray().Select(e => O(("unit", Unit(frame.Content, world, e!)), ("locationId", e!["currentLocationId"]))).OrderBy(l => Key(l["unit"]!), StringComparer.Ordinal));
        Require(JsonNode.DeepEquals(old["scope"], Scope(cycle)) && N(old, "ordinal") == N(cycle, "ordinal") - 1 && old["endLocations"]!.AsArray().Select(l => Key(l!["unit"]!)).SequenceEqual(locations.Select(l => Key(l!["unit"]!)), StringComparer.Ordinal), 4);
        Require(state["members"]!.AsArray().All(m => T(m!, "status") == "none" && JsonNode.DeepEquals(m!["history"]!["scope"], Scope(cycle))), 4);
        bool Near(string from, string to)
        {
            if (from == to) return true;
            var edges = frame.Content["edges"]!.AsArray();
            bool Linked(string a, string b) => edges.Any(e => T(e!, "firstLocationId") == a && T(e!, "secondLocationId") == b || T(e!, "secondLocationId") == a && T(e!, "firstLocationId") == b);
            return Linked(from, to) || edges.Where(e => T(e!, "firstLocationId") == from || T(e!, "secondLocationId") == from).Any(e => Linked(T(e!, T(e!, "firstLocationId") == from ? "secondLocationId" : "firstLocationId"), to));
        }
        JsonArray Excluded(JsonArray at, JsonArray retained) => A(at.Where(l => T(l!["unit"]!, "originalSide") == T(cycle, "actingSide") && (retained.Any(u => Key(u!) == Key(l["unit"]!)) || !at.Any(enemy => T(enemy!["unit"]!, "originalSide") != T(cycle, "actingSide") && Near(T(l, "locationId"), T(enemy, "locationId"))))).Select(l => l!["unit"]));
        var historical = Excluded(old["endLocations"]!.AsArray(), old["excludedBefore"]!.AsArray());
        return O(("scope", Scope(cycle)), ("ordinal", cycle["ordinal"]), ("completionReceiptId", receipt), ("endLocations", locations), ("excludedBefore", Excluded(locations, historical)));
    }
    private static JsonObject Unit(JsonNode content, JsonNode world, JsonNode element) => O(("creationBinding", world["creationBinding"]), ("originalSide", content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(element, "elementId"))!["sideId"]), ("elementId", element["elementId"]));
    private static string Key(JsonNode unit) => T(unit, "creationBinding") + "\0" + T(unit, "originalSide") + "\0" + T(unit, "elementId");
    private static JsonObject Scope(JsonNode cycle) => O(("gameTurn", cycle["gameTurn"]), ("operationStage", cycle["operationStage"]), ("playerPhaseSlot", cycle["playerPhaseSlot"]), ("actingSide", cycle["actingSide"]));
    private static string CycleId(JsonNode c) => CampaignCombatReserveCompletionCodec.CycleId(new((int)N(c, "contractVersion"), T(c, "campaignId"), T(c, "rulesetHash"), T(c, "setupId"), T(c, "setupHash"), T(c, "contentPackId"), T(c, "contentHash"), T(c, "scenarioId"), (int)N(c, "gameTurn"), (int)N(c, "operationStage"), T(c, "playerPhaseSlot"), T(c, "actingSide") == "axis" ? LandSide.Axis : LandSide.Commonwealth, (int)N(c, "ordinal"), N(c, "openedAuthorityVersion"), T(c, "openingPrefix"), T(c, "admittedPolicyBundleDigest")));
    private static string Digest(string kind, byte[] bytes) => Domain("sandtable.combat.actual-repeated-movement-completion." + kind + ".v1", bytes);
}
