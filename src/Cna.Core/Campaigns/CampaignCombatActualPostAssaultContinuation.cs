using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualPostAssaultContinuationCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualPostAssaultContinuationResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualPostAssaultContinuationResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> continuationInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = continuationInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> ContinuationInputs => inputs.Select(x => x.ToArray()).ToArray();
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

/// <summary>Private native actual paid lineage through empty Release and guarded repeat/finish.</summary>
internal static class CampaignCombatActualPostAssaultContinuation
{
    private static readonly string[] EmptyOrdinaryCollections = ["custodyLots", "guards", "replacementEntitlements", "brokenVehicleLots"];
    private static readonly string[] ImmediateCuts = ["disposition", "losses", "retreat", "relationships"];
    internal sealed record Frame(JsonObject Base, string BaseHash, JsonObject Cycle, JsonObject Boundary,
        JsonObject Paid, JsonArray Members, JsonObject MovementEnd, JsonArray Progress,
        CombatDecisionConfiguration Configuration, JsonObject Content);
    public static CombatActualPostAssaultContinuationResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, dependencies);
        return Result(packet, frame, state, inputs);
    }
    public static CombatActualPostAssaultContinuationResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, dependencies);
        var owned = newInput.ToArray(); var (after, accepted, receipt) = Transition(frame, state, Object(owned, "Input"));
        if (accepted is not null) { packet["continuationEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted)); inputs.Add(owned); }
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1); CheckDependencies(dependencies);
        return Result(packet, frame, after, inputs, accepted, receipt, accepted is null && receipt is not null);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> continuation, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, context, selection, rounds, paid, continuation, dependencies).SourceBytes;
    public static CombatActualPostAssaultContinuationResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, dependencies);
        _ = Parse(claim, "Control"); Require(claim.SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualPostAssaultContinuationResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, dependencies);
        _ = Parse(claim, "Proof"); Require(claim.SequenceEqual(result.ProofBytes), 6); return result;
    }
    private static (JsonObject Packet, Frame Frame, JsonObject State, List<byte[]> Inputs) Replay(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, Func<string, byte[]> dependencies)
    {
        CheckDependencies(dependencies); var packet = Object(source, "Source");
        Require(context is not null && selection is not null && rounds is not null && paid is not null && continuation is not null, 1);
        var frame = Derive(Encoding.ASCII.GetBytes(T(packet, "paidSourceCanonicalUtf8")), context, selection!, rounds!, paid!, dependencies);
        Require(continuation!.Count <= 4 && continuation.All(x => x is not null), 1);
        var inputs = continuation.Select(x => { var copy = x.ToArray(); _ = Parse(copy, "Input"); return copy; }).ToList();
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 8192, 1);
        var events = packet["continuationEventCanonicalUtf8"]!.AsArray(); Require(events.Count == inputs.Count, 4);
        var state = Initial(frame);
        for (var i = 0; i < inputs.Count; i++)
        {
            var actual = Encoding.ASCII.GetBytes(events[i]!.GetValue<string>()); _ = Parse(actual, "Event");
            var (after, expected, _) = Transition(frame, state, Object(inputs[i], "Input"));
            Require(expected is not null && actual.AsSpan().SequenceEqual(expected), 6); state = after;
        }
        CheckDependencies(dependencies); return (packet, frame, state, inputs);
    }
    private static CombatActualPostAssaultContinuationResult Result(JsonObject source, Frame frame, JsonObject state,
        IReadOnlyList<byte[]> inputs, byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var sourceBytes = Canonical(source, "Source"); var digest = Digest("source", sourceBytes);
        var proof = O(("contractVersion", 1), ("sourceId", "apcont." + digest), ("sourceHash", "sha256:" + digest), ("base", frame.Base), ("control", state));
        return new(sourceBytes, Canonical(state, "Control"), Canonical(proof, "Proof"), inputs, accepted, receipt, duplicate);
    }
    internal static Frame Derive(byte[] source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        CheckDependencies(dependencies);
        CombatActualPaidAssaultResult replay;
        try { replay = CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, paid, dependencies); }
        catch (JsonException e)
        {
            var code = e.Message.StartsWith("CMB-APA-", StringComparison.Ordinal) && int.TryParse(e.Message.AsSpan(8, 3), out var n) ? n : 4;
            throw new JsonException($"CMB-PAC-{code:000}", e);
        }
        var proof = JsonNode.Parse(replay.ProofBytes)!.AsObject(); var state = JsonNode.Parse(replay.ControlBytes)!.AsObject();
        var content = JsonNode.Parse(CampaignCombatActualPostAssaultContinuationCodec.Consume("docs/specs/fixtures/combat-content-v7.canonical.json", dependencies))!.AsObject();
        var selectionProof = proof["base"]!["admissionProof"]!["actualRoundProof"]!["base"]!["actualSelectionProof"]!;
        var boundary = selectionProof["boundary"]!.AsObject(); var cycle = boundary["cycle"]!.AsObject(); var world = state["world"]!.AsObject();
        Require(N(cycle, "gameTurn") == 1 && N(cycle, "operationStage") == 1 && T(cycle, "playerPhaseSlot") == "first-acting-side" && N(cycle, "ordinal") == 1, 3);
        Require(T(state, "status") == "closed" && B(state, "closed") && N(state, "stateVersion") == 33 && state["window"] is null && state["roundClosureReceiptId"] is not null && state["caCompletionReceiptId"] is not null, 4);
        Require(N(state["randomState"]!, "seed") == 1 && N(state["randomState"]!, "nextByteCursor") == 10 && state["result"]!["draws"]!.AsArray().Count == 8 && state["receipts"]!.AsArray().Count == 8 && paid.Count == 8, 3);
        Require(world["settlements"]!.AsArray().Count == 1 && T(world["settlements"]![0]!["relationships"]!, "kind") == "contact" &&
            EmptyOrdinaryCollections.All(k => world[k]!.AsArray().Count == 0), 3);
        var entry = selectionProof["positiveEntryProof"]!["entry"]!; var members = entry["members"]!.DeepClone().AsArray(); var end = boundary["movementEnd"]!.DeepClone().AsObject();
        var originals = world["elements"]!.AsArray().Select(e => Unit(content, world, e!)).Where(u => T(u, "originalSide") == T(cycle, "actingSide")).OrderBy(Key, StringComparer.Ordinal).ToArray();
        Require(members.Count == 1 && members.Select(m => Key(m!["unit"]!)).SequenceEqual(originals.Select(Key)) && end.ToJsonString() == entry["movementEnd"]!.ToJsonString() && T(end, "completionReceiptId").StartsWith("iml.", StringComparison.Ordinal), 4);
        var locations = end["endLocations"]!.AsArray(); var units = world["elements"]!.AsArray().Select(e => Unit(content, world, e!)).OrderBy(Key, StringComparer.Ordinal).ToArray();
        Require(N(end, "ordinal") == 1 && Canonical(end["scope"]!, "ReleaseScope").AsSpan().SequenceEqual(Canonical(Scope(cycle), "ReleaseScope")) &&
            locations.Select(x => Key(x!["unit"]!)).SequenceEqual(units.Select(Key)), 4);
        foreach (var m in members)
        {
            var element = world["elements"]!.AsArray().Single(e => Key(Unit(content, world, e!)) == Key(m!["unit"]!)); var op = element!["operationalState"]!;
            Require(T(m!, "status") == "none" && T(element, "reserveStatus") == "none" && N(m!, "baseCpa") == 10 && Canonical(m!["history"]!["scope"]!, "ReleaseScope").AsSpan().SequenceEqual(Canonical(Scope(cycle), "ReleaseScope")), 4);
            Require(m["history"]!.AsObject().All(f => f.Key == "scope" || f.Value is null), 3);
            Require(N(element["ammunition"]!, "points") == 0 && N(element["components"]![0]!, "currentToe") == 10 && N(op["capabilityPointsExpended"]!, "numerator") == 5 && N(op["capabilityPointsExpended"]!, "denominator") == 1 && N(op, "ledgerGameTurn") == 1 && N(op, "ledgerOperationStage") == 1 && op["vehicleBreakdownState"] is null && op["movementEnded"] is null, 3);
            m["spentCp"] = op["capabilityPointsExpended"]!.DeepClone();
        }
        var packet = CampaignCombatActualPaidAssaultCodec.Object(source, "PaidSource");
        var events = packet["paidEventCanonicalUtf8"]!.AsArray().Select(e => Encoding.ASCII.GetBytes(e!.GetValue<string>())).ToArray();
        var commitmentEvents = events.Where(e => T(JsonNode.Parse(e)!, "eventType") == "combat-actual-paid-attack-committed").ToArray(); Require(commitmentEvents.Length == 1, 4);
        var committed = JsonNode.Parse(commitmentEvents[0])!; var history = state["attackHistory"]!.AsArray();
        Require(T(committed["effect"]!, "commitmentId") == T(state, "commitmentId") && T(committed, "receiptId") != T(state, "commitmentId") && history.Count == 1 && T(history[0]!, "commitmentId") == T(state, "commitmentId") && Key(history[0]!["attacker"]!) == Key(members[0]!["unit"]!) && N(history[0]!, "gameTurn") == 1 && N(history[0]!, "operationStage") == 1, 4);
        Require(state["receipts"]!.AsArray().Any(r => T(r!, "receiptId") == T(committed, "receiptId") && T(r!, "eventHash") == Hash(commitmentEvents[0])), 4);
        var last = JsonNode.Parse(events[^1])!; var prior = proof["base"]!["admissionProof"]!["actualRoundProof"]!["control"]!;
        Require(T(last, "receiptId") == T(state, "caCompletionReceiptId") && T(last["effect"]!, "kind") == "ca-completed" && T(last["effect"]!, "roundClosureReceiptId") == T(state, "roundClosureReceiptId") && T(last["effect"]!, "previousStepReceiptId") == prior["stepReceipts"]!.AsArray()[^1]!.GetValue<string>() && T(last["effect"]!, "toPositionId") == Position("release"), 4);
        var basis = O(("contractVersion", 1), ("dependencyManifestHash", CampaignCombatActualPostAssaultContinuationCodec.Manifest()), ("paidProof", proof), ("controlClockPolicyId", "sandtable.combat.cycle-control-clock.v1"));
        var progress = new JsonArray(O(("eventType", T(committed, "eventType")), ("receiptId", T(committed, "receiptId")), ("eventHash", Hash(commitmentEvents[0]))));
        return new(basis, "sha256:" + Digest("base", Canonical(basis, "Base")), cycle.DeepClone().AsObject(), boundary.DeepClone().AsObject(), state, members, end, progress, context.Configuration, content);
    }
    internal static JsonObject Initial(Frame frame)
    {
        var p = frame.Paid; var release = "aprel." + Digest("release", Encode(O(("baseHash", frame.BaseHash), ("cycleId", T(frame.Boundary, "cycleId")), ("positionId", Position("release")))));
        var control = "apctl." + Digest("control", Encode(O(("baseHash", frame.BaseHash), ("releaseId", release))));
        return O(("contractVersion", 1), ("baseHash", frame.BaseHash), ("paidSourceHash", frame.Base["paidProof"]!["sourceHash"]), ("controlId", control), ("releaseId", release),
            ("stateVersion", N(p, "stateVersion")), ("prefix", p["prefix"]), ("status", "release-unopened"), ("positionId", Position("release")), ("originalCycle", frame.Cycle), ("activeCycle", frame.Cycle), ("closure", null),
            ("releaseOpeningReceiptId", null), ("releaseCompletionReceiptId", null), ("decisionId", null), ("timing", null), ("openingClockFailure", false), ("acceptedHighWater", null), ("assessment", null),
            ("movementEnd", frame.MovementEnd), ("members", frame.Members), ("world", p["world"]), ("randomState", p["randomState"]), ("attackHistory", p["attackHistory"]), ("targetUses", p["targetUses"]), ("nextCycleProgress", frame.Progress), ("receipts", new JsonArray()), ("closed", false));
    }
    internal static JsonObject Assessment(Frame frame, JsonObject state)
    {
        Require(state["releaseCompletionReceiptId"] is not null && T(state, "status") == "release-completed", 4);
        Require(JsonNode.DeepEquals(state["world"], frame.Paid["world"]) && JsonNode.DeepEquals(state["members"], frame.Members) && JsonNode.DeepEquals(state["movementEnd"], frame.MovementEnd), 4);
        var world = state["world"]!; Require(world["settlements"]!.AsArray().All(s => ImmediateCuts.All(k => s![k] is not null)) && world["brokenVehicleLots"]!.AsArray().Count == 0, 3);
        var excluded = frame.MovementEnd["excludedBefore"]!.AsArray().Select(x => Key(x!)).ToHashSet(StringComparer.Ordinal);
        var enemies = frame.MovementEnd["endLocations"]!.AsArray().Where(x => T(x!["unit"]!, "originalSide") != T(frame.Cycle, "actingSide")).Select(x => T(x!, "locationId")).ToArray();
        var witnesses = new JsonArray();
        foreach (var m in state["members"]!.AsArray())
        {
            var unit = m!["unit"]!; var element = world["elements"]!.AsArray().Single(e => Key(Unit(frame.Content, world, e!)) == Key(unit))!; var op = element["operationalState"]!;
            Require(N(element["ammunition"]!, "points") == 0 && N(element["components"]![0]!, "currentToe") > 0 && JsonNode.DeepEquals(m["spentCp"], op["capabilityPointsExpended"]) && N(m["spentCp"]!, "denominator") == 1 && T(m, "status") == "none" && m["history"]!["nextMovement"] is null, 3);
            var reps = world["representations"]!.AsArray().Where(r => T(r!, "bindingKind") == "independent-element" && r!["boundElementIds"]!.AsArray().Count == 1 && r["boundElementIds"]![0]!.GetValue<string>() == T(unit, "elementId")).ToArray(); Require(reps.Length == 1 && T(reps[0]!, "currentLocationId") == T(element, "currentLocationId"), 4);
            var original = frame.MovementEnd["endLocations"]!.AsArray().Single(x => Key(x!["unit"]!) == Key(unit))!;
            if (excluded.Contains(Key(unit)) || !enemies.Any(enemy => Distance(frame, T(original, "locationId"), enemy) <= 2)) continue;
            var relationships = world["relationships"]!.AsArray().Where(r => B(r!, "active")).ToArray(); Require(relationships.All(r => N(r!, "gameTurn") == N(frame.Cycle, "gameTurn") && N(r!, "operationStage") == N(frame.Cycle, "operationStage")), 4);
            var kinds = relationships.Where(r => Key(r!["attacker"]!) == Key(unit) || Key(r!["defender"]!) == Key(unit)).Select(r => T(r!, "kind")).ToArray(); Require(kinds.All(k => k is "contact" or "engaged"), 3);
            var breakOff = kinds.Contains("engaged", StringComparer.Ordinal) ? 4 : kinds.Contains("contact", StringComparer.Ordinal) ? 2 : 0;
            var spent = N(m["spentCp"]!, "numerator"); var cpa = N(m, "baseCpa"); var origin = T(element, "currentLocationId");
            foreach (var destination in Neighbors(frame, origin).Order(StringComparer.Ordinal))
            {
                if (DestinationBlocked(world, element, destination)) continue;
                var edge = frame.Content["edges"]!.AsArray().Single(e => (T(e!, "firstLocationId") == origin && T(e!, "secondLocationId") == destination) || (T(e!, "secondLocationId") == origin && T(e!, "firstLocationId") == destination))!;
                Require(edge["features"]!.AsArray().Count == 0 && frame.Content["locations"]!.AsArray().Any(l => T(l!, "locationId") == destination && T(l!, "terrainId") == "land.terrain.clear"), 3);
                Require(spent <= long.MaxValue - 2 - breakOff, 7); var after = spent + 2 + breakOff; if (after > 3 * cpa / 2) continue;
                var dp = Math.Max(0, after - cpa) - Math.Max(0, spent - cpa); Require(N(op, "cohesionLevel") - dp >= int.MinValue, 7);
                witnesses.Add(O(("kind", "movement"), ("unit", unit), ("destinationLocationId", destination), ("terrainCost", 2), ("breakOffCost", breakOff), ("afterCp", after), ("excessCpDp", dp), ("usesReleaseException", false)));
            }
        }
        return O(("releaseCompletionReceiptId", state["releaseCompletionReceiptId"]), ("movementCompletionReceiptId", frame.MovementEnd["completionReceiptId"]), ("progress", frame.Progress), ("witnesses", witnesses), ("combatAssessment", "no-own-ammunition"), ("witnessOutcome", "supported"), ("unsupportedReason", null));
    }
    internal static string Mode(JsonNode assessment)
    { Require(T(assessment, "witnessOutcome") == "supported" && assessment["unsupportedReason"] is null, 3); return assessment["witnesses"]!.AsArray().Count == 0 ? "no-continuation" : assessment["progress"]!.AsArray().Count == 0 ? "no-material-progress" : "owner-choice"; }
    internal static JsonObject Command(Frame frame, JsonObject state, string kind)
        => O(("contractVersion", 1), ("kind", kind), ("baseHash", frame.BaseHash), ("cycleId", T(frame.Boundary, "cycleId")), ("expectedPriorVersion", kind is "expire" or "unavailable" ? null : state["stateVersion"]), ("decisionId", kind is "release-open" or "release-complete" or "control-open" ? null : state["decisionId"]));
    internal static (JsonObject State, byte[]? Event, string? Receipt) Transition(Frame frame, JsonObject prior, JsonObject input)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); var cmd = input["command"]!; var kind = T(cmd, "kind"); var actor = T(input, "actor"); var c = frame.Cycle; var timer = kind is "expire" or "unavailable";
        Require(kind is "release-open" or "release-complete" or "control-open" or "repeat" or "finish" or "expire" or "unavailable" or "fallback-step", 3);
        Require(T(cmd, "baseHash") == T(prior, "baseHash") && T(prior, "baseHash") == frame.BaseHash && T(cmd, "cycleId") == T(frame.Boundary, "cycleId") && JsonNode.DeepEquals(prior["originalCycle"], c), 4);
        Require(actor == (kind is "repeat" or "finish" ? T(c, "actingSide") : "system"), 4); Require((cmd["expectedPriorVersion"] is null) == timer, 3);
        Require(kind is not ("release-open" or "release-complete" or "control-open") || cmd["decisionId"] is null, 3);
        if (kind.StartsWith("release-", StringComparison.Ordinal)) Require(input["admittedAt"] is null && B(input, "clockAvailable"), 3);
        var commandHash = Hash(Canonical(cmd, "Command")); var duplicate = prior["receipts"]!.AsArray().FirstOrDefault(r => T(r!, "commandHash") == commandHash && T(r!, "actor") == actor);
        if (duplicate is not null) return (prior.DeepClone().AsObject(), null, T(duplicate, "receiptId"));
        if (timer && (T(prior, "status") != "control-open" || !JsonNode.DeepEquals(cmd["decisionId"], prior["decisionId"]))) return (prior.DeepClone().AsObject(), null, null);
        Require(!B(prior, "closed") && prior["receipts"]!.AsArray().Count < 4 && N(prior, "stateVersion") < long.MaxValue, 7);
        if (!timer) Require(N(cmd, "expectedPriorVersion") == N(prior, "stateVersion"), 6);
        var s = prior.DeepClone().AsObject(); var author = actor; JsonObject effect; long? now = input["admittedAt"] is null ? null : N(input, "admittedAt");
        if (kind == "release-open")
        { Require(T(s, "status") == "release-unopened", 6); s["status"] = "release-open"; effect = O(("kind", "release-opened"), ("releaseId", s["releaseId"]), ("pending", new JsonArray()), ("decisionId", null), ("timing", null)); }
        else if (kind == "release-complete")
        { Require(T(s, "status") == "release-open" && s["releaseOpeningReceiptId"] is not null, 6); s["status"] = "release-completed"; effect = O(("kind", "release-completed"), ("releaseId", s["releaseId"]), ("openingReceiptId", s["releaseOpeningReceiptId"]), ("reason", "units-resolved"), ("retainedUnits", new JsonArray()), ("dispositionReceipts", new JsonArray()), ("timing", null)); }
        else
        {
            var reason = "owner-choice"; string outcome; JsonNode? next = null;
            if (kind == "control-open")
            {
                Require(T(s, "status") == "release-completed", 6); s["assessment"] = Assessment(frame, s); var mode = Mode(s["assessment"]!);
                if (mode != "owner-choice") { outcome = "phase-finished"; reason = mode; }
                else
                {
                    Require(N(c, "ordinal") < int.MaxValue && N(s, "stateVersion") <= long.MaxValue - 2, 7); s["decisionId"] = T(s, "controlId") + ".decision"; Require(T(s, "decisionId").Length <= 128, 7);
                    var budget = frame.Configuration.Windows.Single(w => w.Kind == "cycle-control").DecisionBudgetMilliseconds;
                    var valid = B(input, "clockAvailable") && now is not null && now <= CampaignCombatActualPaidAssaultSupport.MaxUtc - budget;
                    s["timing"] = valid ? O(("contractVersion", 1), ("configHash", frame.Configuration.Hash), ("kind", "cycle-control"), ("decisionBudgetMilliseconds", budget), ("openedAtUnixMilliseconds", now), ("deadlineUnixMilliseconds", checked(now!.Value + budget)), ("highWaterUnixMilliseconds", now)) : null;
                    s["openingClockFailure"] = !valid; if (valid) s["acceptedHighWater"] = now; outcome = "control-opened"; s["status"] = "control-open";
                }
            }
            else
            {
                Require(T(s, "status") == "control-open" && JsonNode.DeepEquals(cmd["decisionId"], s["decisionId"]), 6); var timing = s["timing"];
                var lost = B(s, "openingClockFailure") || !B(input, "clockAvailable") || now is null || s["acceptedHighWater"] is not null && now < N(s, "acceptedHighWater");
                var late = timing is not null && now >= N(timing, "deadlineUnixMilliseconds");
                if (kind == "fallback-step") Require(B(s, "openingClockFailure"), 5);
                if (kind == "expire" && !lost && !late) return (prior.DeepClone().AsObject(), null, null);
                if (kind is "repeat" or "finish" && !lost) Require(!late, 5);
                if (!lost) { s["acceptedHighWater"] = now; s["timing"]!["highWaterUnixMilliseconds"] = now; }
                if (lost || timer || kind == "fallback-step") { author = "system"; outcome = "phase-finished"; reason = B(s, "openingClockFailure") ? "opening-clock-unavailable" : lost ? "clock-unavailable" : kind == "expire" ? "deadline" : "controller-unavailable"; }
                else outcome = kind == "repeat" ? "cycle-repeated" : "phase-finished";
            }
            var successor = T(s, "positionId");
            if (outcome == "cycle-repeated")
            {
                Require(Mode(s["assessment"]!) == "owner-choice" && N(c, "ordinal") < int.MaxValue, 7); next = c.DeepClone(); next["ordinal"] = checked((int)N(c, "ordinal") + 1); next["openedAuthorityVersion"] = N(prior, "stateVersion") + 1; next["openingPrefix"] = prior["prefix"]!.DeepClone(); _ = CycleId(next);
                successor = Position("movement"); s["activeCycle"] = next.DeepClone(); s["targetUses"] = new JsonArray(); s["nextCycleProgress"] = new JsonArray(); s["status"] = "repeated"; s["closed"] = true;
            }
            else if (outcome == "phase-finished") { successor = Position("finish"); s["activeCycle"] = null; s["status"] = "finished"; s["closed"] = true; }
            effect = O(("kind", outcome), ("reason", reason), ("assessment", s["assessment"]), ("timing", s["timing"]), ("openingClockFailure", s["openingClockFailure"]), ("successorPositionId", successor), ("nextCycle", next), ("expiredUnits", new JsonArray())); s["positionId"] = successor;
        }
        var eventNode = O(("contractVersion", 1), ("eventType", "combat-actual-post-assault-" + T(effect, "kind")), ("author", author), ("campaignId", c["campaignId"]), ("rulesetHash", c["rulesetHash"]), ("configurationHash", c["admittedPolicyBundleDigest"]), ("paidSourceHash", prior["paidSourceHash"]), ("baseHash", frame.BaseHash), ("cycleId", frame.Boundary["cycleId"]), ("positionId", prior["positionId"]), ("priorVersion", N(prior, "stateVersion")), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", input), ("effect", effect));
        var receipt = "apc." + Digest("receipt", Encode(eventNode)); eventNode["receiptId"] = receipt; var bytes = Canonical(eventNode, "Event");
        if (T(effect, "kind") == "release-opened") s["releaseOpeningReceiptId"] = receipt;
        else if (T(effect, "kind") == "release-completed") s["releaseCompletionReceiptId"] = receipt;
        else if (T(effect, "kind") != "control-opened") s["closure"] = O(("cycleId", frame.Boundary["cycleId"]), ("ordinal", N(c, "ordinal")), ("outcome", s["status"]), ("receiptId", receipt));
        s["stateVersion"] = eventNode["stateVersion"]!.DeepClone(); s["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(T(prior, "prefix"), bytes);
        s["receipts"]!.AsArray().Add(O(("commandHash", commandHash), ("eventHash", Hash(bytes)), ("receiptId", receipt), ("actor", actor), ("stateVersion", s["stateVersion"]))); _ = Canonical(s, "Control"); return (s, bytes, receipt);
    }
    internal static bool DestinationBlocked(JsonNode world, JsonNode element, string destination)
        => world["elements"]!.AsArray().Any(e => !ReferenceEquals(e, element) && T(e!, "currentLocationId") == destination) || world["guards"]!.AsArray().Any(g => T(g!, "currentLocationId") == destination);
    private static IEnumerable<string> Neighbors(Frame frame, string location) => frame.Content["edges"]!.AsArray().Where(e => T(e!, "firstLocationId") == location || T(e!, "secondLocationId") == location).Select(e => T(e!, T(e!, "firstLocationId") == location ? "secondLocationId" : "firstLocationId"));
    private static int Distance(Frame frame, string start, string end)
    {
        var queue = new Queue<(string Location, int Depth)>(); queue.Enqueue((start, 0)); var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        while (queue.TryDequeue(out var item)) { if (item.Location == end) return item.Depth; foreach (var other in Neighbors(frame, item.Location)) if (seen.Add(other)) queue.Enqueue((other, item.Depth + 1)); }
        throw new JsonException("CMB-PAC-003");
    }
    private static JsonObject Unit(JsonNode content, JsonNode world, JsonNode element) => O(("creationBinding", world["creationBinding"]), ("originalSide", content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(element, "elementId"))!["sideId"]), ("elementId", element["elementId"]));
    private static string Key(JsonNode unit) => T(unit, "creationBinding") + "\0" + T(unit, "originalSide") + "\0" + T(unit, "elementId");
    private static JsonObject Scope(JsonNode cycle) => O(("gameTurn", cycle["gameTurn"]), ("operationStage", cycle["operationStage"]), ("playerPhaseSlot", cycle["playerPhaseSlot"]), ("actingSide", cycle["actingSide"]));
    private static string Position(string kind)
    {
        var suffix = kind == "finish" ? "truck-convoy-movement" : "movement-and-combat." + (kind == "movement" ? "movement" : "reserve-release");
        var id = "land.position.operation-1.first-player." + suffix; return Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == id).PositionId;
    }
    private static string CycleId(JsonNode c) => CampaignCombatReserveCompletionCodec.CycleId(new((int)N(c, "contractVersion"), T(c, "campaignId"), T(c, "rulesetHash"), T(c, "setupId"), T(c, "setupHash"), T(c, "contentPackId"), T(c, "contentHash"), T(c, "scenarioId"), (int)N(c, "gameTurn"), (int)N(c, "operationStage"), T(c, "playerPhaseSlot"), T(c, "actingSide") == "axis" ? LandSide.Axis : LandSide.Commonwealth, (int)N(c, "ordinal"), N(c, "openedAuthorityVersion"), T(c, "openingPrefix"), T(c, "admittedPolicyBundleDigest")));
    private static string Digest(string kind, byte[] value) => Domain("sandtable.combat.actual-post-assault-continuation." + kind + ".v1", value);
    private static void CheckDependencies(Func<string, byte[]> dependencies) => CampaignCombatActualPostAssaultContinuationCodec.VerifyDependencies(dependencies);
}
