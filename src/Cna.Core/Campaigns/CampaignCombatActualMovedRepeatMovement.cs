using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualMovedRepeatMovementCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedRepeatMovementResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    private readonly byte[][][] inputs;
    internal CombatActualMovedRepeatMovementResult(JsonObject packet, JsonObject state, byte[][][] ledgers, byte[]? ev = null, bool duplicate = false)
    {
        source = Canonical(packet, "Source"); control = Canonical(state, "Control"); accepted = ev?.ToArray(); Duplicate = duplicate;
        inputs = ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var hash = Hash(source);
        var events = packet["movementEventCanonicalUtf8"]!.AsArray();
        proof = Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "mrms." + hash[7..],
            ["sourceHash"] = hash,
            ["parentSourceHash"] = state["parentSourceHash"]!.DeepClone(),
            ["dependencyManifestHash"] = ManifestHash,
            ["control"] = state.DeepClone(),
            ["lastEventHash"] = events.Count == 0 ? null : Hash(Encoding.ASCII.GetBytes(events[^1]!.GetValue<string>())),
            ["publicationReceiptId"] = state["publicationReceiptId"]?.DeepClone()
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
    public IReadOnlyList<byte[]> MovementInputs => Ledger(8);
}
internal static class CampaignCombatActualMovedRepeatMovement
{
    private sealed record Frame(JsonObject Packet, JsonObject Parent, JsonObject Base, string BaseHash, CampaignCombatCreationRequest Request, byte[][][] Ledgers, Func<string, byte[]> Dependencies, JsonObject Content);
    private const string SchemaPath = "docs/specs/combat-actual-moved-repeat-movement-v1.schema.json";
    public static CombatActualMovedRepeatMovementResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    { var (f, s) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, dependencies); return new(f.Packet, s, f.Ledgers); }
    public static CombatActualMovedRepeatMovementResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var current = Copy(currentInput, "Input");
        var (f, s) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, dependencies);
        var (afterState, ev, rid) = Transition(f, s, Object(current, "Input"), admissionEnabled);
        if (ev is null)
        {
            var old = rid is null ? null : f.Packet["movementEventCanonicalUtf8"]!.AsArray().Select(e => Encoding.ASCII.GetBytes(e!.GetValue<string>())).Single(e => T(JsonNode.Parse(e)!, "receiptId") == rid);
            return new(f.Packet, s, f.Ledgers, old, rid is not null);
        }
        var packet = f.Packet.DeepClone().AsObject(); packet["movementEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var ledgers = f.Ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); ledgers[8] = [.. ledgers[8], current];
        var (after, state) = Replay(Canonical(packet, "Source"), request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], f.Dependencies);
        Require(JsonNode.DeepEquals(afterState, state), 6); return new(after.Packet, state, after.Ledgers, ev);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, dependencies).SourceBytes;
    public static CombatActualMovedRepeatMovementResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Control"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, dependencies); _ = Object(owned, "Control"); Require(owned.AsSpan().SequenceEqual(result.ControlBytes), 6); return result; }
    public static CombatActualMovedRepeatMovementResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Proof"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, dependencies); _ = Object(owned, "Proof"); Require(owned.AsSpan().SequenceEqual(result.ProofBytes), 6); return result; }
    private static (Frame Frame, JsonObject State) Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies)
    {
        var packet = Object(source, "Source"); SourceBounds(packet); var parentBytes = Encoding.ASCII.GetBytes(T(packet, "controlSourceCanonicalUtf8"));
        Require(prefix.Count == 13 && lifecycle.Count == 3 && breakdown.Count == 1 && selection.Count <= 16 && round.Count <= 16 && paid.Count <= 11 && release.Count == 2 && control.Count == 2 && movement.Count <= 6, 4);
        // Own all independent bytes before any dependency callback; parse successor inputs only after parent authentication.
        var ledgers = new[] { Own(prefix, 13, "AncestorInput"), Own(lifecycle, 3, "AncestorInput"), Own(breakdown, 1, "AncestorInput"), Own(selection, selection.Count, "AncestorInput"), Own(round, round.Count, "AncestorInput"), Own(paid, paid.Count, "AncestorInput"), Own(release, 2, "AncestorInput"), Own(control, 2, "AncestorInput"), Own(movement, packet["movementEventCanonicalUtf8"]!.AsArray().Count, "Input") };
        Require(ledgers[8].Sum(b => b.Length) <= 24_576, 4); var retained = SnapshotDependencies(dependencies);
        var result = CampaignCombatActualMovedCycleControl.ReplayTrustedSource(parentBytes, request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], retained);
        var p = JsonNode.Parse(result.ControlBytes)!.AsObject();
        Require(B(p, "closed") && T(p, "phase") == "repeat-intent-pending" && p["pendingRepeatIntent"] is not null && p["closure"] is null && JsonNode.DeepEquals(p["activeCycle"], p["cycle"]) && N(p["cycle"]!, "ordinal") == 1, 4);
        var a = p["assessment"]!; var intent = p["pendingRepeatIntent"]!;
        Require(T(a, "worldHash") == Hash(Canonical(p["world"]!, "World")) && T(a, "sourceOccurrence") == T(p, "cycleId") && a["witnesses"]!.AsArray().Count > 0 && !B(a, "executionAvailable"), 4);
        var expectedIntent = O(("contractVersion", 1), ("kind", "pending-repeat-intent"), ("sourceOccurrence", p["cycleId"]), ("sourceOrdinal", 1), ("expectedNextOrdinal", 2), ("parentSourceHash", p["parentSourceHash"]), ("worldHash", a["worldHash"]), ("assessmentHash", Hash(Canonical(a, "Assessment"))), ("configurationHash", p["cycle"]!["admittedPolicyBundleDigest"]), ("decisionId", p["decisionId"]), ("witnessesHash", Hash(Encode(a["witnesses"]!))), ("executionAvailable", false));
        Require(JsonNode.DeepEquals(intent, expectedIntent), 4);
        var basis = O(("contractVersion", 1), ("parentSourceHash", Hash(parentBytes)), ("sourceOccurrence", p["cycleId"]), ("intentReceiptId", p["pendingIntentReceiptId"]), ("worldHash", a["worldHash"]), ("assessmentHash", intent["assessmentHash"]), ("configurationHash", intent["configurationHash"]), ("decisionId", intent["decisionId"]), ("witnessesHash", intent["witnessesHash"]), ("dependencyManifestHash", ManifestHash));
        var f = new Frame(packet, p, basis, "sha256:" + Domain("base", Canonical(basis, "Base")), request, ledgers, retained, JsonNode.Parse(retained("docs/specs/fixtures/combat-content-v7.canonical.json"))!.AsObject());
        var state = Initial(f);
        for (var i = 0; i < ledgers[8].Length; i++)
        { var (after, ev, _) = Transition(f, state, Object(ledgers[8][i], "Input"), true, true); Require(ev is not null && ev.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes(packet["movementEventCanonicalUtf8"]![i]!.GetValue<string>())), 6); state = after; }
        return (f, state);
    }
    private static JsonObject Initial(Frame f)
    {
        var p = f.Parent;
        return O(("contractVersion", 1), ("profile", p["profile"]), ("parentSourceHash", f.Base["parentSourceHash"]), ("baseHash", f.BaseHash), ("phase", "repeat-pending"), ("stateVersion", p["stateVersion"]), ("prefix", p["prefix"]),
            ("sequencePosition", p["sequencePosition"]), ("originalCycle", p["cycle"]), ("activeCycle", p["activeCycle"]), ("cycleId", p["cycleId"]), ("world", p["world"]), ("randomState", p["randomState"]), ("originalMovementEnd", p["movementEnd"]),
            ("priorCycleProgress", p["actualProgressRefs"]), ("actualProgressRefs", p["actualProgressRefs"]), ("members", p["members"]), ("attackHistory", p["attackHistory"]), ("priorCycleTargetUses", p["targetUses"]), ("targetUses", p["targetUses"]),
            ("assessment", p["assessment"]), ("pendingRepeatIntent", p["pendingRepeatIntent"]), ("pendingIntentReceiptId", p["pendingIntentReceiptId"]), ("publicationReceiptId", null), ("closure", null), ("tracks", new JsonArray()), ("receipts", p["receipts"]), ("movementReceipts", new JsonArray()));
    }
    private static JsonObject NextCycle(Frame f)
    { var c = f.Parent["cycle"]!.DeepClone().AsObject(); c["ordinal"] = 2; c["openedAuthorityVersion"] = N(f.Parent, "stateVersion") + 1; c["openingPrefix"] = f.Parent["prefix"]!.DeepClone(); return c; }
    private static JsonObject MovementPosition(Frame f)
    {
        var p = f.Parent["sequencePosition"]!.DeepClone().AsObject(); Require(N(p, "contractVersion") == 5 && T(p, "positionId") == "land.position.operation-1.first-player.movement-and-combat.reserve-release" && T(p, "segmentId") == "land.segment.reserve-release" && p["stepId"] is null, 4);
        p["positionId"] = "land.position.operation-1.first-player.movement-and-combat.movement"; p["segmentId"] = "land.segment.movement"; return p;
    }
    private static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame f, JsonObject prior, JsonObject input, bool admissionEnabled, bool replay = false)
    {
        _ = Canonical(input, "Input"); var cmd = input["command"]!; var publish = T(cmd, "kind") == "repeat-publish"; var actor = T(input, "actor"); var nc = NextCycle(f); var nid = CycleId(nc); var p = f.Parent; var unit = p["members"]![0]!["unit"]!;
        Require(N(cmd, "contractVersion") == 1 && BindingFields.All(k => T(cmd, k) == T(f.Base, k)) && T(cmd, "baseHash") == f.BaseHash, 4);
        Require(actor == (publish ? "system" : T(nc, "actingSide")) && T(cmd, "cycleId") == (publish ? T(p, "cycleId") : nid) && T(cmd, "positionId") == T(publish ? p["sequencePosition"]! : MovementPosition(f), "positionId"), 4);
        Require(publish ? cmd["unit"] is null && cmd["originLocationId"] is null && cmd["destinationLocationId"] is null : JsonNode.DeepEquals(cmd["unit"], unit) && cmd["originLocationId"] is not null && cmd["destinationLocationId"] is not null, 4);
        var ch = Hash(Canonical(cmd, "Command")); var duplicate = prior["movementReceipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == ch && T(r!, "actor") == actor);
        if (duplicate is not null) return (prior.DeepClone().AsObject(), null, T(duplicate, "receiptId"));
        Require(N(prior, "stateVersion") < long.MaxValue, 7); Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion"), 6);
        if (!replay) Require(admissionEnabled && B(JsonNode.Parse(f.Dependencies(SchemaPath))!, "closureFrozen"), 7);
        var s = prior.DeepClone().AsObject(); JsonObject effect; JsonNode? element = null, representation = null; JsonNode[] affected = []; long afterCp = 0, dp = 0, cohesion = 0; string? destination = null;
        if (publish)
        {
            Require(T(prior, "phase") == "repeat-pending" && prior["publicationReceiptId"] is null && JsonNode.DeepEquals(prior["activeCycle"], p["cycle"]) && JsonNode.DeepEquals(prior["world"], p["world"]), 6);
            if (!replay) Certify(f);
            effect = O(("kind", "repeat-published"), ("intentReceiptId", f.Base["intentReceiptId"]), ("priorCycleId", p["cycleId"]), ("nextCycleId", nid), ("nextCycle", nc), ("successorPositionId", MovementPosition(f)["positionId"]), ("worldHash", f.Base["worldHash"]), ("assessmentHash", f.Base["assessmentHash"]), ("witnessesHash", f.Base["witnessesHash"]));
            s["phase"] = "movement"; s["activeCycle"] = nc.DeepClone(); s["cycleId"] = nid; s["sequencePosition"] = MovementPosition(f); s["targetUses"] = new JsonArray(); s["actualProgressRefs"] = new JsonArray();
        }
        else
        {
            Require(T(prior, "phase") == "movement" && JsonNode.DeepEquals(prior["activeCycle"], nc) && prior["publicationReceiptId"] is not null, 6);
            var w = s["world"]!; element = w["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(unit, "elementId"))!; var op = element["operationalState"]!; var member = s["members"]![0]!; var origin = T(cmd, "originLocationId"); destination = T(cmd, "destinationLocationId");
            Require(T(element, "currentLocationId") == origin && Neighbors(f, origin).Contains(destination, StringComparer.Ordinal), 5);
            Require(T(member, "status") == "none" && T(element, "reserveStatus") == "none" && N(member, "baseCpa") == 10 && JsonNode.DeepEquals(member["history"], p["members"]![0]!["history"]) && JsonNode.DeepEquals(member["spentCp"], op["capabilityPointsExpended"]) && N(member["spentCp"]!, "denominator") == 1, 3);
            var facts = f.Content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(unit, "elementId"))!;
            Require(N(facts, "baseCapabilityPointAllowance") == 10 && facts["breakdownVehicleCohort"] is null && op["vehicleBreakdownState"] is null && op["movementEnded"] is null && N(op, "ledgerGameTurn") == N(nc, "gameTurn") && N(op, "ledgerOperationStage") == N(nc, "operationStage"), 3);
            Require(w["representations"]!.AsArray().Where(r => r!["boundElementIds"]!.AsArray().Any(id => Side(f, id!.GetValue<string>()) != actor)).All(r => !Neighbors(f, destination).Contains(T(r!, "currentLocationId"), StringComparer.Ordinal)), 3);
            var edge = f.Content["edges"]!.AsArray().Single(e => new[] { T(e!, "firstLocationId"), T(e!, "secondLocationId") }.ToHashSet(StringComparer.Ordinal).SetEquals([origin, destination]))!;
            Require(edge["features"]!.AsArray().Count == 0 && T(f.Content["locations"]!.AsArray().Single(l => T(l!, "locationId") == destination)!, "terrainId") == "land.terrain.clear", 3);
            var witness = LiveWitnesses(f, prior).SingleOrDefault(x => JsonNode.DeepEquals(x["unit"], unit) && T(x, "destinationLocationId") == destination); Require(witness is not null, 5);
            var reps = w["representations"]!.AsArray().Where(r => T(r!, "bindingKind") == "independent-element" && r!["boundElementIds"]!.AsArray().Count == 1 && T(r["boundElementIds"]![0]!, null) == T(unit, "elementId")).ToArray(); Require(reps.Length == 1 && T(reps[0]!, "currentLocationId") == origin, 4); representation = reps[0]!;
            affected = w["relationships"]!.AsArray().Where(r => B(r!, "active") && (JsonNode.DeepEquals(r!["attacker"], unit) || JsonNode.DeepEquals(r["defender"], unit))).OrderBy(r => T(r!, "relationId"), StringComparer.Ordinal).Select(r => r!).ToArray();
            var before = N(op["capabilityPointsExpended"]!, "numerator"); afterCp = N(witness, "afterCp"); dp = N(witness, "excessCpDp"); cohesion = N(op, "cohesionLevel"); Require(prior["actualProgressRefs"]!.AsArray().Count < 5 && cohesion - dp >= int.MinValue, 7);
            var ends = new JsonArray(affected.Select(r => (JsonNode?)O(("relationId", r["relationId"]), ("creationReceiptId", r["creationReceiptId"]), ("kind", r["kind"]), ("attacker", r["attacker"]), ("defender", r["defender"]), ("endingCause", "ordinary-break-off"))).ToArray());
            var costs = O(("terrainCost", 2), ("breakOffCost", witness["breakOffCost"]), ("totalCost", afterCp - before), ("baseCpa", 10), ("voluntaryCeiling", 15), ("beforeCp", before), ("afterCp", afterCp), ("excessCpDp", dp), ("beforeCohesion", cohesion), ("afterCohesion", cohesion - dp));
            effect = O(("kind", "ordinary-move"), ("unit", unit), ("representationId", representation["representationId"]), ("originLocationId", origin), ("destinationLocationId", destination), ("costs", costs), ("endedMemberships", ends));
        }
        Require(prior["movementReceipts"]!.AsArray().Count < 6, 7);
        var ev = O(("contractVersion", 1), ("eventType", "combat-actual-moved-" + T(effect, "kind")), ("author", actor), ("campaignId", nc["campaignId"]), ("rulesetHash", nc["rulesetHash"]), ("configurationHash", nc["admittedPolicyBundleDigest"]), ("profile", p["profile"]), ("parentSourceHash", f.Base["parentSourceHash"]), ("baseHash", f.BaseHash), ("cycleId", cmd["cycleId"]), ("priorVersion", prior["stateVersion"]), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", input), ("effect", effect));
        var rid = "mrm." + Domain("receipt", Encode(ev)); ev["receiptId"] = rid; var bytes = Canonical(ev, "Event"); s["stateVersion"] = ev["stateVersion"]!.DeepClone(); s["prefix"] = EventPrefix(T(prior, "prefix"), bytes);
        var record = O(("commandHash", ch), ("eventHash", Hash(bytes)), ("receiptId", rid), ("actor", actor), ("stateVersion", s["stateVersion"])); s["receipts"]!.AsArray().Add(record.DeepClone()); s["movementReceipts"]!.AsArray().Add(record);
        if (publish) { s["publicationReceiptId"] = rid; s["closure"] = O(("cycleId", p["cycleId"]), ("ordinal", 1), ("outcome", "repeated"), ("receiptId", rid)); }
        else
        {
            element!["currentLocationId"] = destination; element["operationalState"]!["capabilityPointsExpended"] = Cp(afterCp); element["operationalState"]!["cohesionLevel"] = cohesion - dp; s["members"]![0]!["spentCp"] = Cp(afterCp); representation!["currentLocationId"] = destination;
            foreach (var r in affected) { r["active"] = false; r["endedByReceiptId"] = rid; r["endingCause"] = "ordinary-break-off"; }
            if (dp > 0) { var causes = s["world"]!["cohesionCauses"]!.AsArray(); Require(causes.Count < 512 && (rid + ".dp").Length <= 128, 7); causes.Add(O(("causeId", rid + ".dp"), ("ordinal", causes.Count + 1), ("receiptId", rid), ("elementId", unit["elementId"]), ("gameTurn", nc["gameTurn"]), ("operationStage", nc["operationStage"]), ("kind", "ordinary-movement-excess-cp-dp"), ("points", dp), ("before", cohesion), ("after", cohesion - dp))); }
            var tracks = s["tracks"]!.AsArray(); if (tracks.Count > 0) { var route = tracks[0]!["route"]!.AsArray(); Require(T(route[^1]!, null) == T(cmd, "originLocationId") && route.Count < 6, 7); route.Add(destination); } else tracks.Add(O(("unit", unit), ("route", new JsonArray(T(cmd, "originLocationId"), destination))));
            s["actualProgressRefs"]!.AsArray().Add(O(("eventType", ev["eventType"]), ("receiptId", rid), ("eventHash", Hash(bytes))));
        }
        _ = Canonical(s, "Control"); return (s, bytes, rid);
    }
    private static List<JsonObject> LiveWitnesses(Frame f, JsonObject s)
    {
        var w = s["world"]!; var cycle = f.Parent["cycle"]!; var witnesses = new List<JsonObject>(); var unsupported = 0;
        Require(f.Parent["releaseCompletionReceiptId"] is not null && w["settlements"]!.AsArray().All(x => MandatoryCuts.All(k => x![k] is not null)) && w["brokenVehicleLots"]!.AsArray().Count == 0, 3);
        foreach (var member in s["members"]!.AsArray())
        {
            var unit = member!["unit"]!; var e = w["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(unit, "elementId"))!; var op = e["operationalState"]!;
            Require(N(e["ammunition"]!, "points") == (T(f.Parent, "profile") == "paid" ? 0 : 10) && N(e["components"]![0]!, "currentToe") > 0, 3);
            var reps = w["representations"]!.AsArray().Where(r => T(r!, "bindingKind") == "independent-element" && r!["boundElementIds"]!.AsArray().Count == 1 && T(r["boundElementIds"]![0]!, null) == T(unit, "elementId")).ToArray(); Require(reps.Length == 1 && T(reps[0]!, "currentLocationId") == T(e, "currentLocationId"), 4);
            if (f.Parent["movementEnd"]!["excludedBefore"]!.AsArray().Any(x => JsonNode.DeepEquals(x, unit))) continue;
            var original = f.Parent["movementEnd"]!["endLocations"]!.AsArray().Single(l => JsonNode.DeepEquals(l!["unit"], unit))!;
            if (!f.Parent["movementEnd"]!["endLocations"]!.AsArray().Where(l => T(l!["unit"]!, "originalSide") != T(cycle, "actingSide")).Any(l => Distance(f, T(original, "locationId"), T(l!, "locationId")) <= 2)) continue;
            var active = w["relationships"]!.AsArray().Where(r => B(r!, "active")).ToArray(); Require(active.All(r => N(r!, "gameTurn") == N(cycle, "gameTurn") && N(r!, "operationStage") == N(cycle, "operationStage")), 4);
            var kinds = active.Where(r => JsonNode.DeepEquals(r!["attacker"], unit) || JsonNode.DeepEquals(r["defender"], unit)).Select(r => T(r!, "kind")).ToArray(); Require(kinds.All(k => k is "contact" or "engaged"), 3);
            var bo = kinds.Contains("engaged", StringComparer.Ordinal) ? 4 : kinds.Contains("contact", StringComparer.Ordinal) ? 2 : 0;
            foreach (var dest in Neighbors(f, T(e, "currentLocationId")))
            {
                if (w["elements"]!.AsArray().Any(x => T(x!, "currentLocationId") == dest) || w["guards"]!.AsArray().Any(x => T(x!, "currentLocationId") == dest)) continue;
                var spent = N(op["capabilityPointsExpended"]!, "numerator"); var after = spent + 2 + bo; if (after > 15) continue;
                var dp = Math.Max(0, after - 10) - Math.Max(0, spent - 10); Require(N(op, "cohesionLevel") - dp >= int.MinValue, 7);
                var witness = O(("kind", "movement"), ("unit", unit), ("destinationLocationId", dest), ("terrainCost", 2), ("breakOffCost", bo), ("afterCp", after), ("excessCpDp", dp), ("usesReleaseException", false));
                if (w["elements"]!.AsArray().Where(x => T(x!, "elementId") != T(unit, "elementId")).Any(x => Neighbors(f, dest).Contains(T(x!, "currentLocationId"), StringComparer.Ordinal))) unsupported++;
                else witnesses.Add(witness);
            }
        }
        Require(witnesses.Count <= 2 && unsupported <= 2, 7); return witnesses;
    }
    private static void Certify(Frame f, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null)
    {
        var schema = JsonNode.Parse(f.Dependencies(SchemaPath))!;
        Require(schema["supportHandlers"]!.AsArray().Select(x => x!.GetValue<string>()).ToHashSet(StringComparer.Ordinal).SetEquals(handlers ?? Handlers.ToHashSet(StringComparer.Ordinal)) && Handlers.ToHashSet(StringComparer.Ordinal).SetEquals(schema["supportHandlers"]!.AsArray().Select(x => x!.GetValue<string>())), 7);
        foreach (var pair in schema["limits"]!.AsObject()) Require(capacities is null || capacities.TryGetValue(pair.Key, out var actual) && actual >= pair.Value!.GetValue<int>(), 7);
        CampaignCombatActualMovedCycleControl.CertifyTrustedSource(Encoding.ASCII.GetBytes(T(f.Packet, "controlSourceCanonicalUtf8")), f.Request, f.Ledgers[0], f.Ledgers[1], f.Ledgers[2], f.Ledgers[3], f.Ledgers[4], f.Ledgers[5], f.Ledgers[6], f.Ledgers[7], f.Dependencies);
        var ctx = SupportContext(f); var paid = ctx.Prior["world"]!.DeepClone().AsObject();
        foreach (var (role, cost) in SupportCosts)
        {
            var e = paid["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(ctx.Selection[role]!["unit"]!, "elementId"))!;
            var before = N(e["operationalState"]!["capabilityPointsExpended"]!, "numerator"); Require(before == (role == "attacker" ? 4 : 0), 7);
            e["operationalState"]!["capabilityPointsExpended"] = Cp(before + cost); e["ammunition"]!["points"] = 0;
        }
        var summaries = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var row in CampaignCombatActualPaidAssaultSupport.Catalogue())
        {
            if (T(row, "kind") != "capture-refusal") continue; var facts = row["facts"]!;
            var key = O(("attackerPercent", facts["attackerPercent"]), ("defenderPercent", facts["defenderPercent"]), ("rawEngaged", facts["rawEngaged"]), ("requiredRetreat", facts["requiredRetreat"]), ("capturedRole", facts["capturedRole"]), ("captureShare", facts["captureShare"]), ("refusalHexes", row["refusalHexes"]));
            summaries[Encoding.ASCII.GetString(Encode(key))] = row;
        }
        var observed = 0;
        foreach (var row in summaries.Values)
        {
            var facts = row["facts"]!; var retreat = N(facts, "requiredRetreat") == 0 ? "not-required" : N(row, "refusalHexes") > 0 ? "refuse-retreat" : "retreat";
            var captured = N(row, "attackerCapturedToe") + N(row, "defenderCapturedToe") > 0;
            foreach (var custody in captured ? CustodyChoices : NoCustody)
            {
                var result = O(("resultId", "res." + new string('f', 64)), ("commitmentId", "cmt." + new string('f', 64)), ("facts", facts));
                var world = CampaignCombatActualPaidAssaultSupport.Project(ctx, paid, result, "relationships", retreat, custody);
                // Isolated policy facts only. They never enter Source, Proof, ledger or RNG ancestry.
                var parent = f.Parent.DeepClone().AsObject(); parent["profile"] = "paid"; var isolated = f with { Parent = parent };
                var state = Initial(isolated); state["world"] = world.DeepClone(); state["phase"] = "movement"; state["activeCycle"] = NextCycle(f); state["cycleId"] = CycleId(NextCycle(f)); state["sequencePosition"] = MovementPosition(f); state["actualProgressRefs"] = new JsonArray(); state["targetUses"] = new JsonArray(); state["publicationReceiptId"] = "mrm." + new string('f', 64);
                foreach (var member in state["members"]!.AsArray()) member!["spentCp"] = world["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(member["unit"]!, "elementId"))!["operationalState"]!["capabilityPointsExpended"]!.DeepClone();
                var witnesses = LiveWitnesses(isolated, state); Require(witnesses.Count > 0, 7);
                foreach (var witness in witnesses)
                {
                    var (after, ev, _) = Transition(isolated, state, Command(state, "move", T(witness, "destinationLocationId")), true, true);
                    Require(ev is not null && after["actualProgressRefs"]!.AsArray().Count == 1, 7);
                    foreach (var field in RetainedWorldFields) Require(JsonNode.DeepEquals(after["world"]![field], state["world"]![field]), 7);
                    foreach (var field in RetainedFields) Require(JsonNode.DeepEquals(after[field], state[field]), 7);
                }
                observed++;
            }
        }
        Require(observed > 0, 7);
    }
    private static PaidAssaultFrame SupportContext(Frame f)
    {
        var n3 = JsonNode.Parse(T(f.Packet, "controlSourceCanonicalUtf8"))!; var n2 = JsonNode.Parse(T(n3, "continuationSourceCanonicalUtf8"))!; var n1Bytes = Encoding.ASCII.GetBytes(T(n2, "movedAssaultSourceCanonicalUtf8")); var n1 = JsonNode.Parse(n1Bytes)!;
        var original = CampaignCombatActualMovedAssault.ReplayTrustedSource(n1Bytes, f.Request, f.Ledgers[0], f.Ledgers[1], f.Ledgers[2], f.Ledgers[3], f.Ledgers[4], f.Ledgers[5], f.Dependencies);
        var p = JsonNode.Parse(original.ControlBytes)!; var candidate = p["candidate"]!;
        var entryResult = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(Encoding.ASCII.GetBytes(T(n1, "reactionEntrySourceCanonicalUtf8")), f.Request, f.Ledgers[0], f.Ledgers[1], f.Ledgers[2], f.Dependencies); var entry = JsonNode.Parse(entryResult.ControlBytes)!;
        var prior = O(("world", entry["world"]), ("randomState", entry["randomState"]), ("attackHistory", new JsonArray()), ("targetUses", new JsonArray()), ("slots", new JsonArray(SupportCosts.Select(x => (JsonNode?)O(("role", x.Role), ("allocation", O(("unit", candidate[x.Role]!["unit"]), ("committedToe", 10))))).ToArray())));
        var basis = O(("contractVersion", 1), ("profile", f.Parent["profile"]), ("parentSourceHash", Hash(n1Bytes)), ("cycleId", f.Parent["cycleId"]), ("configurationHash", f.Parent["cycle"]!["admittedPolicyBundleDigest"]));
        return new(basis, prior, O(("attacker", candidate["attacker"]), ("defender", candidate["defender"])), O(("cycleId", f.Parent["cycleId"]), ("position", entry["sequencePosition"])), f.Parent["cycle"]!.AsObject(), f.Request.Context.Configuration, f.Content);
    }
    internal static void CertifyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control,
        IReadOnlyList<byte[]> movement, Func<string, byte[]> dependencies, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null)
    { var (f, _) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, dependencies); Certify(f, handlers, capacities); }
    internal static JsonObject Command(JsonObject state, string kind, string? destination = null)
    {
        var publish = kind == "repeat-publish"; var unit = publish ? null : state["members"]![0]!["unit"]; var element = publish ? null : state["world"]!["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(unit!, "elementId"));
        return O(("command", O(("contractVersion", 1), ("kind", kind), ("parentSourceHash", state["parentSourceHash"]), ("baseHash", state["baseHash"]), ("sourceOccurrence", state["pendingRepeatIntent"]!["sourceOccurrence"]), ("intentReceiptId", state["pendingIntentReceiptId"]), ("configurationHash", state["pendingRepeatIntent"]!["configurationHash"]), ("decisionId", state["pendingRepeatIntent"]!["decisionId"]), ("expectedPriorVersion", state["stateVersion"]), ("cycleId", state["cycleId"]), ("positionId", state["sequencePosition"]!["positionId"]), ("unit", unit), ("originLocationId", element?["currentLocationId"]), ("destinationLocationId", destination))), ("actor", publish ? JsonValue.Create("system") : state["activeCycle"]!["actingSide"]));
    }
    private static readonly string[] BindingFields = ["parentSourceHash", "sourceOccurrence", "intentReceiptId", "configurationHash", "decisionId"];
    private static readonly string[] MandatoryCuts = ["disposition", "losses", "retreat", "relationships"];
    private static readonly string[] RetainedWorldFields = ["settlements", "custodyLots", "guards", "replacementEntitlements", "futureObligations", "brokenVehicleLots"];
    private static readonly string[] RetainedFields = ["randomState", "originalMovementEnd", "priorCycleProgress", "priorCycleTargetUses", "attackHistory", "assessment", "pendingRepeatIntent", "pendingIntentReceiptId"];
    private static readonly (string Role, int Cost)[] SupportCosts = [("attacker", 5), ("defender", 3)];
    private static readonly string?[] CustodyChoices = ["relocate-and-guard", "leave-unguarded"];
    private static readonly string?[] NoCustody = [null];
    private static readonly string[] Handlers = ["strict-n3-nine-ledger-replay", "both-paid-and-armed", "intent-current-world-config-decision-witness-binding", "complete-parent-support-certificate", "exactly-once-ordinal2-publication", "canonical-authority-and-position", "publication-only-cycle-resets", "ordinary-cumulative-cost-and-dp", "current-geometry-reaction-boundary", "world-assets-duties-history-retained", "owned-before-callback", "fresh-admission-only", "claims-full-replay", "capacity-closure"];
    private static IEnumerable<string> Neighbors(Frame f, string location) => f.Content["edges"]!.AsArray().Where(e => T(e!, "firstLocationId") == location || T(e!, "secondLocationId") == location).Select(e => T(e!, T(e!, "firstLocationId") == location ? "secondLocationId" : "firstLocationId")).Order(StringComparer.Ordinal);
    private static int Distance(Frame f, string start, string end)
    {
        var q = new Queue<(string Location, int Depth)>(); q.Enqueue((start, 0)); var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        while (q.TryDequeue(out var x)) { if (x.Location == end) return x.Depth; foreach (var other in Neighbors(f, x.Location)) if (seen.Add(other)) q.Enqueue((other, x.Depth + 1)); }
        throw new JsonException("CMB-MRM-003");
    }
    private static string Side(Frame f, string id) => T(f.Content["elements"]!.AsArray().Single(x => T(x!, "elementId") == id)!, "sideId");
    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static JsonObject Cp(long value) => O(("numerator", value), ("denominator", 1));
    private static string T(JsonNode node, string? key) => key is null ? node.GetValue<string>() : node[key]!.GetValue<string>();
    private static long N(JsonNode node, string key) => JsonSerializer.SerializeToElement(node[key]).GetInt64();
    private static bool B(JsonNode node, string key) => node[key]!.GetValue<bool>();
    private static string Domain(string kind, byte[] bytes) => HashWithDomain("sandtable.combat.actual-moved-repeat-movement." + kind + ".v1", bytes)[7..];
    private static string CycleId(JsonNode c) => CampaignCombatReserveCompletionCodec.CycleId(new((int)N(c, "contractVersion"), T(c, "campaignId"), T(c, "rulesetHash"), T(c, "setupId"), T(c, "setupHash"), T(c, "contentPackId"), T(c, "contentHash"), T(c, "scenarioId"), (int)N(c, "gameTurn"), (int)N(c, "operationStage"), T(c, "playerPhaseSlot"), T(c, "actingSide") == "axis" ? LandSide.Axis : LandSide.Commonwealth, (int)N(c, "ordinal"), N(c, "openedAuthorityVersion"), T(c, "openingPrefix"), T(c, "admittedPolicyBundleDigest")));
}
