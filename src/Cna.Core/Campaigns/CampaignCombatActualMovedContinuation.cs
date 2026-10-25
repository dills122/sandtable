using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualMovedContinuationCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualMovedContinuationResult
{
    private readonly byte[] source, control, proof;
    private readonly byte[]? accepted;
    private readonly byte[][][] inputs;
    internal CombatActualMovedContinuationResult(JsonObject packet, JsonObject state, byte[][][] ledgers, string manifestHash, byte[]? ev = null, bool duplicate = false)
    {
        source = Canonical(packet, "Source"); control = Canonical(state, "Control"); accepted = ev?.ToArray(); Duplicate = duplicate;
        inputs = ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var hash = Hash(source);
        var events = packet["releaseEventCanonicalUtf8"]!.AsArray();
        proof = Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "mcts." + hash[7..],
            ["sourceHash"] = hash,
            ["parentSourceHash"] = state["parentSourceHash"]!.DeepClone(),
            ["dependencyManifestHash"] = manifestHash,
            ["control"] = state.DeepClone(),
            ["lastEventHash"] = events.Count == 0 ? null : Hash(Encoding.ASCII.GetBytes(events[^1]!.GetValue<string>())),
            ["terminalReceiptId"] = state["releaseCompletionReceiptId"]?.DeepClone()
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
}
internal static class CampaignCombatActualMovedContinuation
{
    private const string ContentPath = "docs/specs/fixtures/combat-content-v7.canonical.json";
    private const string SchemaPath = "docs/specs/combat-actual-moved-continuation-v1.schema.json";
    private static readonly string[] EmptyArmed = ["settlements", "custodyLots", "guards", "relationships", "futureObligations", "replacementEntitlements"];
    private static readonly string[] MandatoryCuts = ["disposition", "losses", "retreat", "relationships"];
    private static readonly Lazy<string> CatalogueDigest = new(() => "sha256:" + Domain("catalogue", Encode(CampaignCombatActualPaidAssaultSupport.A(CampaignCombatActualPaidAssaultSupport.Catalogue()))));
    private sealed record Frame(JsonObject Packet, JsonObject Parent, JsonObject Entry, JsonArray Members, JsonObject Content,
        JsonObject Base, string BaseHash, CampaignCombatCreationRequest Request, byte[][][] Ledgers, Func<string, byte[]> Dependencies);
    public static CombatActualMovedContinuationResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies)
    { var (f, s) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, dependencies); return new(f.Packet, s, f.Ledgers, ManifestHashFor(f.Dependencies)); }
    public static CombatActualMovedContinuationResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release,
        ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    {
        var current = Copy(currentInput, "Input"); var input = Object(current, "Input");
        var (f, s) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, dependencies);
        var (_, ev, receipt) = Transition(f, s, input, admissionEnabled);
        if (ev is null)
        {
            var old = f.Packet["releaseEventCanonicalUtf8"]!.AsArray().Select(e => Encoding.ASCII.GetBytes(e!.GetValue<string>())).Single(e => T(JsonNode.Parse(e)!, "receiptId") == receipt);
            return new(f.Packet, s, f.Ledgers, ManifestHashFor(f.Dependencies), old, true);
        }
        var packet = f.Packet.DeepClone().AsObject(); packet["releaseEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(ev));
        var ledgers = f.Ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); ledgers[6] = [.. ledgers[6], current];
        var (after, state) = Replay(Canonical(packet, "Source"), request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], f.Dependencies);
        return new(after.Packet, state, after.Ledgers, ManifestHashFor(after.Dependencies), ev);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, dependencies).SourceBytes;
    public static CombatActualMovedContinuationResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Control"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, dependencies); _ = Object(owned, "Control"); Require(owned.AsSpan().SequenceEqual(result.ControlBytes), 6); return result; }
    public static CombatActualMovedContinuationResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies)
    { var owned = Copy(claim, "Proof"); var result = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, dependencies); _ = Object(owned, "Proof"); Require(owned.AsSpan().SequenceEqual(result.ProofBytes), 6); return result; }
    private static (Frame Frame, JsonObject State) Replay(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies)
    {
        var packet = Object(source, "Source"); SourceBounds(packet); var parentBytes = Encoding.ASCII.GetBytes(T(packet, "movedAssaultSourceCanonicalUtf8"));
        var parentPacket = CampaignCombatActualMovedAssaultCodec.Object(parentBytes, "Source"); CampaignCombatActualMovedAssaultCodec.SourceBounds(parentPacket);
        var ledgers = new[] { Own(prefix, 13, "ParentSource"), Own(lifecycle, 3, "ParentSource"), Own(breakdown, 1, "ParentSource"),
            Own(selection, parentPacket["selectionEventCanonicalUtf8"]!.AsArray().Count, "ParentSource"), Own(round, parentPacket["roundEventCanonicalUtf8"]!.AsArray().Count, "ParentSource"),
            Own(paid, parentPacket["paidEventCanonicalUtf8"]!.AsArray().Count, "ParentSource"), Own(release, packet["releaseEventCanonicalUtf8"]!.AsArray().Count, "Input") };
        var retained = SnapshotDependencies(dependencies);
        var parentResult = CampaignCombatActualMovedAssault.ReplayTrustedSource(parentBytes, request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], retained);
        var parent = JsonNode.Parse(parentResult.ControlBytes)!.AsObject(); Require(B(parent, "closed") && T(parent, "phase") == "reserve-entry", 4);
        var profile = T(packet, "profile"); Require((parent["paid"] is not null) == (profile == "paid"), 4);
        var entryResult = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(Encoding.ASCII.GetBytes(T(parentPacket, "reactionEntrySourceCanonicalUtf8")), request, ledgers[0], ledgers[1], ledgers[2], retained);
        var entry = JsonNode.Parse(entryResult.ControlBytes)!.AsObject(); var cycle = parent["cycle"]!;
        Require(N(cycle, "ordinal") == 1 && N(cycle, "gameTurn") == 1 && N(cycle, "operationStage") == 1 && T(cycle, "playerPhaseSlot") == "first-acting-side", 3);
        var members = entry["members"]!.DeepClone().AsArray(); var content = JsonNode.Parse(retained(ContentPath))!.AsObject(); var world = parent["world"]!;
        var originals = content["elements"]!.AsArray().Where(e => T(e!, "sideId") == T(cycle, "actingSide")).Select(e => Unit(content, world, e!)).OrderBy(Key, StringComparer.Ordinal).ToArray();
        Require(members.Count == 1 && members.Select(m => Key(m!["unit"]!)).SequenceEqual(originals.Select(Key)), 4);
        foreach (var member in members)
        {
            var e = world["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(member!["unit"]!, "elementId"))!; var op = e["operationalState"]!;
            Require(T(member!, "status") == "none" && T(e, "reserveStatus") == "none" && N(member!, "baseCpa") == 10 && member!["history"]!.AsObject().All(p => p.Key == "scope" || p.Value is null), 3);
            Require(N(op["capabilityPointsExpended"]!, "numerator") == (profile == "paid" ? 9 : 4) && N(op["capabilityPointsExpended"]!, "denominator") == 1 && N(e["ammunition"]!, "points") == (profile == "paid" ? 0 : 10), 3);
            member["spentCp"] = op["capabilityPointsExpended"]!.DeepClone(); _ = Canonical(member, "ReserveMember");
        }
        Require(parent["actualProgressRefs"]!.AsArray().Count == (profile == "paid" ? 3 : 2) && JsonNode.DeepEquals(parent["movementEnd"], entry["movementEnd"]), 4);
        if (profile == "paid") Require(parent["paid"]!["window"] is null && !world["custodyLots"]!.AsArray().Any(l => T(l!, "status") == "pending"), 4);
        else Require(EmptyArmed.All(k => world[k]!.AsArray().Count == 0) && N(parent["randomState"]!, "seed") == 1 && N(parent["randomState"]!, "nextByteCursor") == 2, 3);
        var basis = O(("contractVersion", 1), ("profile", profile), ("parentSourceHash", Hash(parentBytes)), ("cycleId", parent["cycleId"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]));
        var frame = new Frame(packet, parent, entry, members, content, basis, "sha256:" + Domain("base", Canonical(basis, "Base")), request, ledgers, retained);
        var state = Initial(frame);
        for (var i = 0; i < ledgers[6].Length; i++)
        { var (after, ev, _) = Transition(frame, state, Object(ledgers[6][i], "Input"), true, true); Require(ev is not null && ev.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes(packet["releaseEventCanonicalUtf8"]![i]!.GetValue<string>())), 6); state = after; }
        return (frame, state);
    }
    private static JsonObject Initial(Frame f)
    {
        var p = f.Parent; var release = "mrel." + Domain("release", Encode(O(("baseHash", f.BaseHash), ("cycleId", p["cycleId"]), ("positionId", p["sequencePosition"]!["positionId"]))));
        return O(("contractVersion", 1), ("profile", f.Packet["profile"]), ("parentSourceHash", f.Base["parentSourceHash"]), ("baseHash", f.BaseHash), ("releaseId", release), ("phase", "release-unopened"),
            ("stateVersion", p["stateVersion"]), ("prefix", p["prefix"]), ("sequencePosition", p["sequencePosition"]), ("cycle", p["cycle"]), ("cycleId", p["cycleId"]), ("world", p["world"]), ("randomState", p["randomState"]),
            ("movementEnd", p["movementEnd"]), ("actualProgressRefs", p["actualProgressRefs"]), ("members", f.Members), ("attackHistory", p["paid"]?["attackHistory"] ?? new JsonArray()), ("targetUses", p["paid"]?["targetUses"] ?? new JsonArray()),
            ("releaseOpeningReceiptId", null), ("releaseCompletionReceiptId", null), ("assessment", null), ("controlWindow", null), ("receipts", p["receipts"]), ("releaseReceipts", new JsonArray()), ("closed", false));
    }
    private static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame f, JsonObject prior, JsonObject input, bool admissionEnabled, bool replay = false)
    {
        _ = Canonical(input, "Input"); var cmd = input["command"]!; Require(T(input, "actor") == "system", 4);
        Require(N(cmd, "contractVersion") == 1 && T(cmd, "parentSourceHash") == T(f.Base, "parentSourceHash") && T(cmd, "releaseId") == T(prior, "releaseId"), 4);
        var hash = Hash(Canonical(cmd, "Command")); var duplicate = prior["releaseReceipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == hash && T(r!, "actor") == "system");
        if (duplicate is not null) return (prior.DeepClone().AsObject(), null, T(duplicate, "receiptId"));
        Require(!B(prior, "closed") && prior["releaseReceipts"]!.AsArray().Count < 2 && N(cmd, "expectedPriorVersion") == N(prior, "stateVersion"), 6);
        var state = prior.DeepClone().AsObject(); JsonObject effect; var kind = T(cmd, "kind");
        if (kind == "open-empty-release")
        { Require(T(prior, "phase") == "release-unopened", 6); if (!replay) { Require(admissionEnabled, 7); Certify(f); } state["phase"] = "release-open"; effect = O(("kind", "release-opened"), ("releaseId", state["releaseId"]), ("pending", new JsonArray())); }
        else
        { Require(T(prior, "phase") == "release-open" && prior["releaseOpeningReceiptId"] is not null, 6); state["phase"] = "assessed"; state["closed"] = true; effect = O(("kind", "release-completed"), ("releaseId", state["releaseId"]), ("openingReceiptId", state["releaseOpeningReceiptId"]), ("retainedUnits", new JsonArray()), ("dispositionReceipts", new JsonArray())); }
        var cycle = state["cycle"]!;
        var ev = O(("contractVersion", 1), ("eventType", "moved-continuation-" + T(effect, "kind")), ("author", "system"), ("campaignId", cycle["campaignId"]), ("rulesetHash", cycle["rulesetHash"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]),
            ("profile", f.Packet["profile"]), ("parentSourceHash", state["parentSourceHash"]), ("baseHash", f.BaseHash), ("cycleId", state["cycleId"]), ("priorVersion", prior["stateVersion"]), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", input), ("effect", effect));
        var receipt = "mct." + Domain("receipt", Encode(ev)); ev["receiptId"] = receipt; var bytes = Canonical(ev, "Event");
        state[kind == "open-empty-release" ? "releaseOpeningReceiptId" : "releaseCompletionReceiptId"] = receipt; state["stateVersion"] = ev["stateVersion"]!.DeepClone(); state["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(T(prior, "prefix"), bytes);
        var record = O(("commandHash", hash), ("eventHash", Hash(bytes)), ("receiptId", receipt), ("actor", "system"), ("stateVersion", state["stateVersion"])); state["receipts"]!.AsArray().Add(record.DeepClone()); state["releaseReceipts"]!.AsArray().Add(record);
        if (kind == "complete-empty-release") state["assessment"] = Assessment(f, state, state["world"]!.AsObject(), T(f.Packet, "profile"), f.Members);
        _ = Canonical(state, "Control"); return (state, bytes, receipt);
    }
    private static JsonObject Assessment(Frame f, JsonObject state, JsonObject world, string profile, JsonArray members)
    {
        Require(state["releaseCompletionReceiptId"] is not null && world["settlements"]!.AsArray().All(s => MandatoryCuts.All(k => s![k] is not null)) && world["brokenVehicleLots"]!.AsArray().Count == 0, 3);
        var witnesses = new JsonArray(); var unsupported = new JsonArray(); var cycle = f.Parent["cycle"]!;
        foreach (var member in members)
        {
            var unit = member!["unit"]!; var e = world["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(unit, "elementId"))!; var op = e["operationalState"]!;
            Require(N(e["ammunition"]!, "points") == (profile == "paid" ? 0 : 10) && N(e["components"]![0]!, "currentToe") > 0, 3);
            var reps = world["representations"]!.AsArray().Where(r => T(r!, "bindingKind") == "independent-element" && r!["boundElementIds"]!.AsArray().Count == 1 && T(r["boundElementIds"]![0]!, null) == T(unit, "elementId")).ToArray();
            Require(reps.Length == 1 && T(reps[0]!, "currentLocationId") == T(e, "currentLocationId"), 4);
            if (f.Parent["movementEnd"]!["excludedBefore"]!.AsArray().Any(x => Key(x!) == Key(unit))) continue;
            var original = f.Parent["movementEnd"]!["endLocations"]!.AsArray().Single(l => Key(l!["unit"]!) == Key(unit))!;
            if (!f.Parent["movementEnd"]!["endLocations"]!.AsArray().Where(l => T(l!["unit"]!, "originalSide") != T(cycle, "actingSide")).Any(l => Distance(f, T(original, "locationId"), T(l!, "locationId")) <= 2)) continue;
            var active = world["relationships"]!.AsArray().Where(r => B(r!, "active")).ToArray(); Require(active.All(r => N(r!, "gameTurn") == N(cycle, "gameTurn") && N(r!, "operationStage") == N(cycle, "operationStage")), 4);
            var kinds = active.Where(r => Key(r!["attacker"]!) == Key(unit) || Key(r!["defender"]!) == Key(unit)).Select(r => T(r!, "kind")).ToArray(); Require(kinds.All(k => k is "contact" or "engaged"), 3);
            var bo = kinds.Contains("engaged", StringComparer.Ordinal) ? 4 : kinds.Contains("contact", StringComparer.Ordinal) ? 2 : 0;
            foreach (var destination in Neighbors(f, T(e, "currentLocationId")))
            {
                if (CampaignCombatActualPostAssaultContinuation.DestinationBlocked(world, e, destination)) continue;
                var spent = N(op["capabilityPointsExpended"]!, "numerator"); var after = spent + 2 + bo; if (after > 15) continue;
                var dp = Math.Max(0, after - 10) - Math.Max(0, spent - 10); Require(N(op, "cohesionLevel") - dp >= int.MinValue, 7);
                var witness = O(("kind", "movement"), ("unit", unit), ("destinationLocationId", destination), ("terrainCost", 2), ("breakOffCost", bo), ("afterCp", after), ("excessCpDp", dp), ("usesReleaseException", false));
                if (world["elements"]!.AsArray().Where(x => T(x!, "elementId") != T(unit, "elementId")).Any(x => Neighbors(f, destination).Contains(T(x!, "currentLocationId"), StringComparer.Ordinal))) unsupported.Add(O(("witness", witness), ("reason", "requires-reaction")));
                else witnesses.Add(witness);
            }
        }
        Require(witnesses.Count <= 2 && unsupported.Count <= 2, 7);
        var rulesHash = Cna1979CombatAdjudication.ContentHash; var catHash = CatalogueDigest.Value;
        var schema = JsonNode.Parse(f.Dependencies(SchemaPath))!;
        var cert = O(("contractVersion", 1), ("profile", profile), ("sourceOccurrence", f.Parent["cycleId"]), ("sourceOrdinal", 1), ("nextOrdinal", 2), ("worldHash", Hash(Canonical(world, "World"))), ("configurationHash", cycle["admittedPolicyBundleDigest"]),
            ("selectedRulesHash", rulesHash), ("catalogueHash", catHash), ("moralePairs", 1296), ("assaultPairs", 6480), ("captureRefusalVariants", 8840), ("supportHandlersHash", "sha256:" + Domain("handlers", Encode(schema["supportHandlers"]!))), ("capacitiesHash", "sha256:" + Domain("capacities", Encode(schema["limits"]!))));
        return O(("contractVersion", 1), ("mode", profile), ("sourceOccurrence", f.Parent["cycleId"]), ("sourceOrdinal", 1), ("nextOrdinal", 2), ("releaseCompletionReceiptId", state["releaseCompletionReceiptId"]), ("movementCompletionReceiptId", f.Parent["movementEnd"]!["completionReceiptId"]),
            ("worldHash", Hash(Canonical(world, "World"))), ("membersHash", Hash(Canonical(members, "ReserveMember[]"))), ("progress", f.Parent["actualProgressRefs"]), ("witnesses", witnesses), ("unsupportedWitnesses", unsupported), ("combatAssessment", profile == "paid" ? "no-own-ammunition" : "armed-ordinary-prospective"), ("witnessOutcome", "supported"), ("unsupportedReason", null), ("executionAvailable", false), ("supportCertificate", cert));
    }
    private static void Certify(Frame f, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null, IEnumerable<JsonObject>? records = null)
    {
        var schema = JsonNode.Parse(f.Dependencies(SchemaPath))!;
        var required = schema["supportHandlers"]!.AsArray().Select(x => x!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        Require(required.SetEquals(handlers ?? required), 7);
        foreach (var key in SupportCapacityFields)
            Require(capacities is null || capacities.TryGetValue(key, out var n) && n >= N(schema["limits"]!, key), 7);
        using var supplied = (records ?? CampaignCombatActualPaidAssaultSupport.Catalogue()).GetEnumerator(); var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var summaries = new Dictionary<string, JsonObject>(StringComparer.Ordinal); var ordinal = 0;
        foreach (var expected in CampaignCombatActualPaidAssaultSupport.Catalogue())
        {
            Require(supplied.MoveNext() && JsonNode.DeepEquals(supplied.Current, expected) && N(supplied.Current, "ordinal") == ordinal++, 7);
            var row = supplied.Current; var kind = T(row, "kind"); counts[kind] = counts.GetValueOrDefault(kind) + 1;
            if (kind != "capture-refusal") continue;
            var facts = row["facts"]!; var key = O(("attackerPercent", facts["attackerPercent"]), ("defenderPercent", facts["defenderPercent"]), ("rawEngaged", facts["rawEngaged"]),
                ("requiredRetreat", facts["requiredRetreat"]), ("capturedRole", facts["capturedRole"]), ("captureShare", facts["captureShare"]), ("refusalHexes", row["refusalHexes"]));
            summaries[Encoding.ASCII.GetString(Encode(key))] = row;
        }
        Require(!supplied.MoveNext() && ordinal == 16616 && counts["morale"] == 1296 && counts["assault-cell"] == 6480 && counts["capture-refusal"] == 8840, 7);
        var ctx = SupportContext(f); CampaignCombatActualMovedAssault.CertifyMovedSupport(ctx);
        var paid = ctx.Prior["world"]!.DeepClone().AsObject();
        foreach (var (role, cost) in SupportCosts)
        {
            var unit = ctx.Selection[role]!["unit"]!; var e = paid["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(unit, "elementId"))!;
            var before = N(e["operationalState"]!["capabilityPointsExpended"]!, "numerator"); Require(before == (role == "attacker" ? 4 : 0), 7);
            e["operationalState"]!["capabilityPointsExpended"] = O(("numerator", before + cost), ("denominator", 1)); e["ammunition"]!["points"] = 0;
        }
        foreach (var row in summaries.Values)
        {
            var facts = row["facts"]!; var retreat = N(facts, "requiredRetreat") == 0 ? "not-required" : N(row, "refusalHexes") > 0 ? "refuse-retreat" : "retreat";
            var captured = N(row, "attackerCapturedToe") + N(row, "defenderCapturedToe") > 0;
            foreach (var custody in captured ? CustodyChoices : NoCustody)
            {
                var result = O(("resultId", "res." + new string('f', 64)), ("commitmentId", "cmt." + new string('f', 64)), ("facts", facts));
                var world = CampaignCombatActualPaidAssaultSupport.Project(ctx, paid, result, "relationships", retreat, custody); _ = Canonical(world, "World");
                var members = f.Members.DeepClone().AsArray(); foreach (var member in members) member!["spentCp"] = world["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(member["unit"]!, "elementId"))!["operationalState"]!["capabilityPointsExpended"]!.DeepClone();
                // Isolated policy facts only; never appended to a Source, Proof, event or RNG history.
                var state = O(("world", world), ("releaseCompletionReceiptId", "mct." + new string('f', 64)));
                var assessment = Assessment(f, state, world, "paid", members); Require(assessment["witnesses"]!.AsArray().Count > 0, 7);
            }
        }
        if (T(f.Packet, "profile") == "armed")
        {
            var a = Assessment(f, O(("releaseCompletionReceiptId", "mct." + new string('f', 64))), f.Parent["world"]!.AsObject(), "armed", f.Members);
            Require(a["witnesses"]!.AsArray().Count > 0 && a["witnesses"]!.AsArray().All(w => N(w!, "afterCp") == 6 && N(w!, "excessCpDp") == 0), 7);
        }
    }
    private static readonly string[] SupportCapacityFields = ["sourceBytes", "parentSourceBytes", "controlBytes", "proofBytes", "eventBytes", "inputBytes", "depth", "members", "progress", "witnesses", "unsupportedWitnesses", "releaseEvents", "releaseReceipts"];
    private static readonly (string Role, int Cost)[] SupportCosts = [("attacker", 5), ("defender", 3)];
    private static readonly string?[] CustodyChoices = ["relocate-and-guard", "leave-unguarded"];
    private static readonly string?[] NoCustody = [null];
    private static PaidAssaultFrame SupportContext(Frame f)
    {
        var candidate = f.Parent["candidate"]!;
        var prior = O(("world", f.Entry["world"]), ("randomState", f.Entry["randomState"]), ("attackHistory", new JsonArray()), ("targetUses", new JsonArray()),
            ("slots", new JsonArray(SupportCosts.Select(x => (JsonNode?)O(("role", x.Role), ("allocation", O(("unit", candidate[x.Role]!["unit"]), ("committedToe", 10))))).ToArray())));
        return new(f.Base, prior, O(("attacker", candidate["attacker"]), ("defender", candidate["defender"])),
            O(("cycleId", f.Parent["cycleId"]), ("position", f.Entry["sequencePosition"])), f.Parent["cycle"]!.AsObject(), f.Request.Context.Configuration, f.Content);
    }
    internal static void CertifyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request,
        IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, Func<string, byte[]> dependencies,
        IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null, IEnumerable<JsonObject>? records = null)
    { var (f, _) = Replay(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, dependencies); Certify(f, handlers, capacities, records); }

    private static IEnumerable<string> Neighbors(Frame f, string location) => f.Content["edges"]!.AsArray().Where(e => T(e!, "firstLocationId") == location || T(e!, "secondLocationId") == location).Select(e => T(e!, T(e!, "firstLocationId") == location ? "secondLocationId" : "firstLocationId")).Order(StringComparer.Ordinal);
    private static int Distance(Frame f, string start, string end)
    {
        var pending = new Queue<(string Location, int Distance)>(); pending.Enqueue((start, 0)); var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        while (pending.TryDequeue(out var p)) { if (p.Location == end) return p.Distance; foreach (var n in Neighbors(f, p.Location)) if (seen.Add(n)) pending.Enqueue((n, p.Distance + 1)); }
        throw new JsonException("CMB-MCT-003");
    }
    private static JsonObject O(params (string Key, object? Value)[] fields) => CampaignCombatActualPaidAssaultSupport.O(fields);
    private static string T(JsonNode node, string? key) => key is null ? node.GetValue<string>() : node[key]!.GetValue<string>();
    private static long N(JsonNode node, string key) => JsonSerializer.SerializeToElement(node[key]).GetInt64();
    private static bool B(JsonNode node, string key) => node[key]!.GetValue<bool>();
    private static string Domain(string kind, byte[] bytes) => CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-moved-continuation." + kind + ".v1", bytes)[7..];
    private static string Key(JsonNode unit) => T(unit, "creationBinding") + "\0" + T(unit, "originalSide") + "\0" + T(unit, "elementId");
    private static JsonObject Unit(JsonObject content, JsonNode world, JsonNode element) => O(("creationBinding", world["creationBinding"]), ("originalSide", content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(element, "elementId"))!["sideId"]), ("elementId", element["elementId"]));
}
