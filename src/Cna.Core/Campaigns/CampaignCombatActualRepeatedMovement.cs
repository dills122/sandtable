using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedMovementCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualRepeatedMovementResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualRepeatedMovementResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> movementInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = movementInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> MovementInputs => inputs.Select(x => x.ToArray()).ToArray();
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

/// <summary>Private native ordinary moves from fully authenticated post-assault repeat lineage.</summary>
internal static class CampaignCombatActualRepeatedMovement
{
    private static readonly string[] RetainedFields = ["originalCycle", "activeCycle", "closure", "movementEnd", "members", "world", "randomState", "attackHistory"];
    private static readonly string[] EmptyCollections = ["guards", "custodyLots", "replacementEntitlements", "brokenVehicleLots"];
    private static readonly string[] ImmediateCuts = ["disposition", "losses", "retreat", "relationships"];
    internal sealed record Frame(JsonObject Base, string BaseHash, JsonObject Prior, JsonObject Cycle, JsonObject Content);
    public static CombatActualRepeatedMovementResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, movement, dependencies);
        return Result(packet, frame, state, inputs);
    }
    public static CombatActualRepeatedMovementResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, movement, dependencies);
        var owned = newInput.ToArray(); var (after, accepted, receipt) = Transition(frame, state, Object(owned, "Input"));
        if (accepted is not null) { packet["movementEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted)); inputs.Add(owned); }
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1); dependencies = CheckDependencies(dependencies);
        return Result(packet, frame, after, inputs, accepted, receipt, accepted is null);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, dependencies).SourceBytes;
    public static CombatActualRepeatedMovementResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, dependencies);
        _ = Parse(claim, "Control"); Require(claim.SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualRepeatedMovementResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, dependencies);
        _ = Parse(claim, "Proof"); Require(claim.SequenceEqual(result.ProofBytes), 6); return result;
    }
    private static (JsonObject Packet, Frame Frame, JsonObject State, List<byte[]> Inputs) Replay(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    {
        dependencies = CheckDependencies(dependencies); var packet = Object(source, "Source");
        var frame = Derive(Encoding.ASCII.GetBytes(T(packet, "continuationSourceCanonicalUtf8")), context, selection, rounds, paid, continuation, dependencies);
        Require(movement is not null && movement.Count <= 4 && movement.All(x => x is not null), 1);
        var inputs = movement.Select(x => { var copy = x.ToArray(); _ = Parse(copy, "Input"); return copy; }).ToList();
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1);
        var events = packet["movementEventCanonicalUtf8"]!.AsArray(); Require(events.Count == inputs.Count, 4); var state = Initial(frame);
        for (var i = 0; i < inputs.Count; i++)
        {
            var actual = Encoding.ASCII.GetBytes(events[i]!.GetValue<string>()); _ = Parse(actual, "Event");
            var (after, expected, _) = Transition(frame, state, Object(inputs[i], "Input"));
            Require(expected is not null && actual.AsSpan().SequenceEqual(expected), 6); state = after;
        }
        dependencies = CheckDependencies(dependencies); return (packet, frame, state, inputs);
    }
    private static CombatActualRepeatedMovementResult Result(JsonObject packet, Frame frame, JsonObject state,
        IReadOnlyList<byte[]> inputs, byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var source = Canonical(packet, "Source"); var digest = Digest("source", source);
        var proof = O(("contractVersion", 1), ("sourceId", "armove." + digest), ("sourceHash", "sha256:" + digest), ("base", frame.Base), ("control", state));
        return new(source, Canonical(state, "Control"), Canonical(proof, "Proof"), inputs, accepted, receipt, duplicate);
    }
    internal static Frame Derive(byte[] source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> continuation, Func<string, byte[]> dependencies)
    {
        dependencies = CheckDependencies(dependencies); CombatActualPostAssaultContinuationResult ancestor;
        try { ancestor = CampaignCombatActualPostAssaultContinuation.ReplayTrustedSource(source, context, selection, rounds, paid, continuation, dependencies); }
        catch (JsonException error)
        {
            var code = error.Message.StartsWith("CMB-PAC-", StringComparison.Ordinal) && int.TryParse(error.Message.AsSpan(8, 3), out var n) ? n : 4;
            throw new JsonException($"CMB-RMV-{code:000}", error);
        }
        var prior = JsonNode.Parse(ancestor.ControlBytes)!.AsObject();
        Require(T(prior, "status") == "repeated" && B(prior, "closed") && N(prior, "stateVersion") == 37 && prior["activeCycle"] is not null && prior["closure"] is not null && T(prior["closure"]!, "outcome") == "repeated", 3);
        var cycle = prior["activeCycle"]!.AsObject(); var world = prior["world"]!; var members = prior["members"]!.AsArray(); var end = prior["movementEnd"]!;
        Require(N(cycle, "gameTurn") == 1 && N(cycle, "operationStage") == 1 && T(cycle, "playerPhaseSlot") == "first-acting-side" && N(cycle, "ordinal") == 2 && N(prior["originalCycle"]!, "ordinal") == 1, 3);
        Require(N(cycle, "openedAuthorityVersion") == 37 && N(prior["closure"]!, "ordinal") == 1 && T(prior["closure"]!, "receiptId") == T(prior["receipts"]!.AsArray()[^1]!, "receiptId") && T(prior, "positionId") == "land.position.operation-1.first-player.movement-and-combat.movement", 4);
        Require(prior["targetUses"]!.AsArray().Count == 0 && prior["nextCycleProgress"]!.AsArray().Count == 0 && members.Count == 1 && end["endLocations"]!.AsArray().Count == 2 && end["excludedBefore"]!.AsArray().Count <= 2 && N(end, "ordinal") == 1, 4);
        Require(N(prior["randomState"]!, "seed") == 1 && N(prior["randomState"]!, "nextByteCursor") == 10 && prior["attackHistory"]!.AsArray().Count == 1 && world["settlements"]!.AsArray().Count == 1 && world["relationships"]!.AsArray().Count == 1 && world["cohesionCauses"]!.AsArray().Count == 0, 3);
        Require(EmptyCollections.All(k => world[k]!.AsArray().Count == 0) && world["settlements"]!.AsArray().All(s => ImmediateCuts.All(k => s![k] is not null)), 3);
        var content = JsonNode.Parse(CampaignCombatActualRepeatedMovementCodec.Consume("docs/specs/fixtures/combat-content-v7.canonical.json", dependencies))!.AsObject();
        var m = members[0]!; var unit = m["unit"]!; var e = world["elements"]!.AsArray().Single(x => Key(Unit(content, world, x!)) == Key(unit))!; var op = e["operationalState"]!;
        Require(T(unit, "originalSide") == T(cycle, "actingSide") && T(m, "status") == "none" && T(e, "reserveStatus") == "none" && N(m, "baseCpa") == 10 && JsonNode.DeepEquals(m["spentCp"], Cp(5)) && JsonNode.DeepEquals(m["spentCp"], op["capabilityPointsExpended"]), 3);
        Require(m["history"]!.AsObject().All(k => k.Key == "scope" || k.Value is null) && JsonNode.DeepEquals(m["history"]!["scope"], Scope(cycle)), 3);
        Require(N(op, "cohesionLevel") == 0 && op["vehicleBreakdownState"] is null && op["movementEnded"] is null && N(e["ammunition"]!, "points") == 0 && N(e["components"]![0]!, "currentToe") == 10, 3);
        var basis = O(("contractVersion", 1), ("dependencyManifestHash", Manifest(dependencies)), ("continuationProof", JsonNode.Parse(ancestor.ProofBytes))); _ = Canonical(basis, "Base");
        return new(basis, "sha256:" + Digest("base", Canonical(basis, "Base")), prior, cycle.DeepClone().AsObject(), content);
    }
    internal static JsonObject Initial(Frame frame)
    {
        var p = frame.Prior; var state = O(("contractVersion", 1), ("baseHash", frame.BaseHash), ("continuationSourceHash", frame.Base["continuationProof"]!["sourceHash"]), ("stateVersion", p["stateVersion"]), ("prefix", p["prefix"]), ("positionId", p["positionId"]));
        foreach (var field in RetainedFields) state[field] = p[field]!.DeepClone();
        state["targetUses"] = new JsonArray(); state["nextCycleProgress"] = new JsonArray(); state["tracks"] = new JsonArray(); state["receipts"] = new JsonArray(); return state;
    }
    internal static JsonObject Command(Frame frame, JsonObject state, string destination)
    {
        var unit = state["members"]![0]!["unit"]!; var element = state["world"]!["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(unit, "elementId"))!;
        return O(("command", O(("contractVersion", 1), ("kind", "move"), ("baseHash", frame.BaseHash), ("cycleId", CycleId(frame.Cycle)), ("positionId", state["positionId"]), ("expectedPriorVersion", state["stateVersion"]), ("unit", unit), ("originLocationId", element["currentLocationId"]), ("destinationLocationId", destination))), ("actor", frame.Cycle["actingSide"]));
    }
    internal static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame frame, JsonObject prior, JsonObject input)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); var cmd = input["command"]!; var actor = T(input, "actor"); var unit = cmd["unit"]!; var c = frame.Cycle; var world = prior["world"]!;
        Require(T(cmd, "kind") == "move", 3);
        Require(T(cmd, "baseHash") == T(prior, "baseHash") && T(prior, "baseHash") == frame.BaseHash && T(cmd, "cycleId") == CycleId(c) && T(cmd, "positionId") == T(prior, "positionId") && T(prior, "positionId") == T(frame.Prior, "positionId") && JsonNode.DeepEquals(prior["activeCycle"], c), 4);
        Require(actor == T(c, "actingSide") && actor == T(unit, "originalSide") && T(unit, "creationBinding") == T(world, "creationBinding") && JsonNode.DeepEquals(unit, frame.Prior["members"]![0]!["unit"]), 4);
        var ch = Hash(Canonical(cmd, "Command")); var previous = prior["receipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == ch && T(r!, "actor") == actor);
        if (previous is not null) return (prior.DeepClone().AsObject(), null, T(previous, "receiptId"));
        Require(N(prior, "stateVersion") < long.MaxValue, 7); Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion"), 6);
        var e = world["elements"]!.AsArray().Single(x => Key(Unit(frame.Content, world, x!)) == Key(unit))!; var op = e["operationalState"]!; var m = prior["members"]![0]!; var origin = T(cmd, "originLocationId"); var destination = T(cmd, "destinationLocationId");
        Require(T(e, "currentLocationId") == origin && Neighbors(frame, origin).Contains(destination, StringComparer.Ordinal), 5);
        Require(!world["representations"]!.AsArray().Any(r => r!["boundElementIds"]!.AsArray().Any(id => Side(frame, id!.GetValue<string>()) != actor) && Neighbors(frame, destination).Contains(T(r!, "currentLocationId"), StringComparer.Ordinal)), 3);
        var end = prior["movementEnd"]!; var original = end["endLocations"]!.AsArray().Single(x => Key(x!["unit"]!) == Key(unit))!;
        Require(!end["excludedBefore"]!.AsArray().Any(x => Key(x!) == Key(unit)) && end["endLocations"]!.AsArray().Where(x => T(x!["unit"]!, "originalSide") != actor).Any(enemy => Distance(frame, T(original, "locationId"), T(enemy!, "locationId")) <= 2), 5);
        Require(!world["elements"]!.AsArray().Any(x => !ReferenceEquals(x, e) && T(x!, "currentLocationId") == destination) && !world["guards"]!.AsArray().Any(x => T(x!, "currentLocationId") == destination), 5);
        Require(T(m, "status") == "none" && T(e, "reserveStatus") == "none" && N(m, "baseCpa") == 10 && JsonNode.DeepEquals(m["history"], frame.Prior["members"]![0]!["history"]) && JsonNode.DeepEquals(m["spentCp"], op["capabilityPointsExpended"]) && N(m["spentCp"]!, "denominator") == 1, 3);
        var facts = frame.Content["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(e, "elementId"))!;
        Require(N(facts, "baseCapabilityPointAllowance") == 10 && facts["breakdownVehicleCohort"] is null && op["vehicleBreakdownState"] is null && op["movementEnded"] is null && N(e["ammunition"]!, "points") == 0 && N(e["components"]![0]!, "currentToe") == 10, 3);
        Require(N(op, "ledgerGameTurn") == N(c, "gameTurn") && N(op, "ledgerOperationStage") == N(c, "operationStage"), 4);
        var reps = world["representations"]!.AsArray().Where(r => T(r!, "bindingKind") == "independent-element" && r!["boundElementIds"]!.AsArray().Count == 1 && r["boundElementIds"]![0]!.GetValue<string>() == T(unit, "elementId")).ToArray(); Require(reps.Length == 1 && T(reps[0]!, "currentLocationId") == origin, 4);
        var active = world["relationships"]!.AsArray().Where(r => B(r!, "active")).ToArray(); Require(active.All(r => N(r!, "gameTurn") == N(c, "gameTurn") && N(r!, "operationStage") == N(c, "operationStage")), 4);
        var affected = active.Where(r => Key(r!["attacker"]!) == Key(unit) || Key(r!["defender"]!) == Key(unit)).OrderBy(r => T(r!, "relationId"), StringComparer.Ordinal).ToArray(); Require(affected.All(r => T(r!, "kind") is "contact" or "engaged"), 3);
        var edge = frame.Content["edges"]!.AsArray().Single(x => (T(x!, "firstLocationId") == origin && T(x!, "secondLocationId") == destination) || (T(x!, "secondLocationId") == origin && T(x!, "firstLocationId") == destination))!;
        Require(edge["features"]!.AsArray().Count == 0 && frame.Content["locations"]!.AsArray().Any(l => T(l!, "locationId") == destination && T(l!, "terrainId") == "land.terrain.clear"), 3);
        var spent = N(m["spentCp"]!, "numerator"); var breakOff = affected.Any(r => T(r!, "kind") == "engaged") ? 4 : affected.Length > 0 ? 2 : 0;
        Require(spent <= long.MaxValue - 2 - breakOff, 7); var after = spent + 2 + breakOff; Require(after <= 15, 5); var dp = Math.Max(0, after - 10) - Math.Max(0, spent - 10); var cohesion = N(op, "cohesionLevel");
        Require(cohesion - dp >= int.MinValue && prior["receipts"]!.AsArray().Count < 4, 7);
        var ends = A(affected.Select(r => O(("relationId", r!["relationId"]), ("creationReceiptId", r["creationReceiptId"]), ("kind", r["kind"]), ("attacker", r["attacker"]), ("defender", r["defender"]), ("endingCause", "ordinary-break-off"))));
        var costs = O(("terrainCost", 2), ("breakOffCost", breakOff), ("totalCost", after - spent), ("baseCpa", 10), ("voluntaryCeiling", 15), ("beforeCp", spent), ("afterCp", after), ("excessCpDp", dp), ("beforeCohesion", cohesion), ("afterCohesion", cohesion - dp));
        var effect = O(("kind", "ordinary-move"), ("unit", unit), ("representationId", reps[0]!["representationId"]), ("originLocationId", origin), ("destinationLocationId", destination), ("costs", costs), ("endedMemberships", ends));
        var ev = O(("contractVersion", 1), ("eventType", "combat-actual-repeated-element-moved"), ("author", actor), ("campaignId", c["campaignId"]), ("rulesetHash", c["rulesetHash"]), ("configurationHash", c["admittedPolicyBundleDigest"]), ("continuationSourceHash", prior["continuationSourceHash"]), ("baseHash", frame.BaseHash), ("cycleId", cmd["cycleId"]), ("positionId", cmd["positionId"]), ("priorVersion", N(prior, "stateVersion")), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", input), ("effect", effect));
        var rid = "armv." + Digest("receipt", Encode(ev)); ev["receiptId"] = rid; var bytes = Canonical(ev, "Event"); Require((rid + ".dp").Length <= 128, 7);
        var s = prior.DeepClone().AsObject(); var w = s["world"]!; var moved = w["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(e, "elementId"))!;
        moved["currentLocationId"] = destination; moved["operationalState"]!["capabilityPointsExpended"] = Cp(after); moved["operationalState"]!["cohesionLevel"] = cohesion - dp; s["members"]![0]!["spentCp"] = Cp(after);
        w["representations"]!.AsArray().Single(x => T(x!, "representationId") == T(reps[0]!, "representationId"))!["currentLocationId"] = destination;
        foreach (var r in w["relationships"]!.AsArray().Where(r => affected.Any(a => T(a!, "relationId") == T(r!, "relationId")))) { r!["active"] = false; r["endedByReceiptId"] = rid; r["endingCause"] = "ordinary-break-off"; }
        if (dp > 0)
        {
            var causes = w["cohesionCauses"]!.AsArray(); Require(causes.Count < 3, 7); causes.Add(O(("causeId", rid + ".dp"), ("ordinal", causes.Count + 1), ("receiptId", rid), ("elementId", e["elementId"]), ("gameTurn", c["gameTurn"]), ("operationStage", c["operationStage"]), ("kind", "ordinary-movement-excess-cp-dp"), ("points", dp), ("before", cohesion), ("after", cohesion - dp)));
        }
        var tracks = s["tracks"]!.AsArray(); if (tracks.Count > 0) { var route = tracks[0]!["route"]!.AsArray(); Require(route[^1]!.GetValue<string>() == origin, 6); route.Add(destination); } else tracks.Add(O(("unit", unit), ("route", new JsonArray(origin, destination))));
        s["stateVersion"] = N(prior, "stateVersion") + 1; s["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(T(prior, "prefix"), bytes);
        s["receipts"]!.AsArray().Add(O(("commandHash", ch), ("eventHash", Hash(bytes)), ("receiptId", rid), ("actor", actor), ("stateVersion", s["stateVersion"]))); s["nextCycleProgress"]!.AsArray().Add(O(("eventType", ev["eventType"]), ("receiptId", rid), ("eventHash", Hash(bytes))));
        _ = Canonical(s, "Control"); return (s, bytes, rid);
    }
    private static IEnumerable<string> Neighbors(Frame f, string location) => f.Content["edges"]!.AsArray().Where(e => T(e!, "firstLocationId") == location || T(e!, "secondLocationId") == location).Select(e => T(e!, T(e!, "firstLocationId") == location ? "secondLocationId" : "firstLocationId"));
    private static int Distance(Frame f, string start, string end)
    {
        var q = new Queue<(string Location, int Depth)>(); q.Enqueue((start, 0)); var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        while (q.TryDequeue(out var x)) { if (x.Location == end) return x.Depth; foreach (var other in Neighbors(f, x.Location)) if (seen.Add(other)) q.Enqueue((other, x.Depth + 1)); }
        throw new JsonException("CMB-RMV-003");
    }
    private static string Side(Frame f, string id) => T(f.Content["elements"]!.AsArray().Single(x => T(x!, "elementId") == id)!, "sideId");
    private static JsonObject Unit(JsonNode content, JsonNode world, JsonNode element) => O(("creationBinding", world["creationBinding"]), ("originalSide", content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(element, "elementId"))!["sideId"]), ("elementId", element["elementId"]));
    private static string Key(JsonNode unit) => T(unit, "creationBinding") + "\0" + T(unit, "originalSide") + "\0" + T(unit, "elementId");
    private static JsonObject Scope(JsonNode c) => O(("gameTurn", c["gameTurn"]), ("operationStage", c["operationStage"]), ("playerPhaseSlot", c["playerPhaseSlot"]), ("actingSide", c["actingSide"]));
    private static string CycleId(JsonNode c) => CampaignCombatReserveCompletionCodec.CycleId(new((int)N(c, "contractVersion"), T(c, "campaignId"), T(c, "rulesetHash"), T(c, "setupId"), T(c, "setupHash"), T(c, "contentPackId"), T(c, "contentHash"), T(c, "scenarioId"), (int)N(c, "gameTurn"), (int)N(c, "operationStage"), T(c, "playerPhaseSlot"), T(c, "actingSide") == "axis" ? LandSide.Axis : LandSide.Commonwealth, (int)N(c, "ordinal"), N(c, "openedAuthorityVersion"), T(c, "openingPrefix"), T(c, "admittedPolicyBundleDigest")));
    private static string Digest(string kind, byte[] bytes) => Domain("sandtable.combat.actual-repeated-movement." + kind + ".v1", bytes);
    private static Func<string, byte[]> CheckDependencies(Func<string, byte[]> dependencies) => CampaignCombatActualRepeatedMovementCodec.VerifyDependencies(dependencies);
}
