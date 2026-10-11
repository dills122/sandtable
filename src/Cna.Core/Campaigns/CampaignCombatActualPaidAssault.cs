using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultCodec;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualPaidAssaultResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualPaidAssaultResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> paidInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = paidInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> PaidInputs => inputs.Select(x => x.ToArray()).ToArray();
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

/// <summary>Private native replay over full carriers and independently retained input ledgers.</summary>
internal static class CampaignCombatActualPaidAssault
{
    private static readonly string[] MandatoryCuts = ["disposition", "losses", "retreat", "relationships"];
    private static readonly string[] SettlementCuts = ["disposition", "losses", "retreat", "custody", "relationships"];
    public static CombatActualPaidAssaultResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedSelectionInputs, IReadOnlyList<byte[]> trustedRoundInputs, IReadOnlyList<byte[]> trustedPaidInputs, Func<string, byte[]> dependencyBytes)
    {
        var (packet, frame, state, inputs) = Replay(source, context, trustedSelectionInputs, trustedRoundInputs, trustedPaidInputs, dependencyBytes);
        return Result(packet, frame, state, inputs);
    }
    public static CombatActualPaidAssaultResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedSelectionInputs, IReadOnlyList<byte[]> trustedRoundInputs, IReadOnlyList<byte[]> trustedPaidInputs,
        ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencyBytes, bool admissionEnabled = true)
    {
        var (packet, frame, state, inputs) = Replay(source, context, trustedSelectionInputs, trustedRoundInputs, trustedPaidInputs, dependencyBytes);
        var owned = newInput.ToArray(); var input = Object(owned, "PaidInput");
        var (after, accepted, receipt) = Transition(frame, state, input, admissionEnabled);
        if (accepted is not null) { packet["paidEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted)); inputs.Add(owned); }
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 16_384, 1); VerifyDependencies(dependencyBytes);
        return Result(packet, frame, after, inputs, accepted, receipt, accepted is null && receipt is not null);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, context, selection, rounds, paid, dependencies).SourceBytes;
    public static CombatActualPaidAssaultResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, dependencies);
        _ = Parse(claim, "PaidControl"); Require(claim.SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualPaidAssaultResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, dependencies);
        _ = Parse(claim, "PaidProof"); Require(claim.SequenceEqual(result.ProofBytes), 6); return result;
    }
    private static (JsonObject Packet, PaidAssaultFrame Frame, JsonObject State, List<byte[]> Inputs) Replay(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, Func<string, byte[]> dependencies)
    {
        VerifyDependencies(dependencies);
        Require(context is not null && selection is not null && rounds is not null && paid is not null, 1);
        Require(selection!.All(x => x is not null) && rounds!.All(x => x is not null) && paid!.All(x => x is not null), 1);
        var packet = Object(source, "PaidSource"); Require(paid!.Count <= 11, 1);
        var inputs = paid.Select(x => { var copy = x.ToArray(); _ = Parse(copy, "PaidInput"); return copy; }).ToList();
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 16_384, 1);
        var events = packet["paidEventCanonicalUtf8"]!.AsArray(); Require(inputs.Count == events.Count, 4);
        var frame = CampaignCombatActualAssaultAdmission.Admit(Encoding.ASCII.GetBytes(T(packet, "admissionSourceCanonicalUtf8")), context,
            selection.Select(x => x.ToArray()).ToArray(), rounds.Select(x => x.ToArray()).ToArray(), dependencies);
        var state = Initial(frame);
        for (var i = 0; i < inputs.Count; i++)
        {
            var actual = Encoding.ASCII.GetBytes(events[i]!.GetValue<string>()); _ = Parse(actual, "PaidEvent");
            var (after, expected, _) = Transition(frame, state, Object(inputs[i], "PaidInput"), false, replay: true);
            Require(expected is not null && actual.AsSpan().SequenceEqual(expected), 6); state = after;
        }
        VerifyDependencies(dependencies); return (packet, frame, state, inputs);
    }
    private static CombatActualPaidAssaultResult Result(JsonObject source, PaidAssaultFrame frame, JsonObject state, IReadOnlyList<byte[]> inputs,
        byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var hash = PaidDigest("source", source, "PaidSource");
        var proof = O(("contractVersion", 1), ("sourceId", "apass." + hash), ("sourceHash", "sha256:" + hash), ("base", frame.Base), ("control", state));
        return new(Canonical(source, "PaidSource"), Canonical(state, "PaidControl"), Canonical(proof, "PaidProof"), inputs, accepted, receipt, duplicate);
    }
    internal static JsonObject Initial(PaidAssaultFrame ctx)
    {
        var p = ctx.Prior;
        Require(p["randomState"]!["seed"]!.GetValue<ulong>() == 1 && p["randomState"]!["nextByteCursor"]!.GetValue<ulong>() == 2, 4);
        return O(("contractVersion", 1), ("baseHash", "sha256:" + PaidDigest("base", ctx.Base, "PaidBase")),
            ("roundClockConfigurationHash", p["clockConfigurationHash"]), ("resultClockPolicyId", ClockPolicy), ("stateVersion", 25), ("prefix", p["prefix"]),
            ("status", "prepared"), ("commitmentId", null), ("result", null), ("settlementId", null), ("window", null),
            ("acceptedHighWater", p["timing"]!["openingFloorUnixMilliseconds"]), ("world", p["world"]), ("randomState", p["randomState"]),
            ("attackHistory", new JsonArray()), ("targetUses", new JsonArray()), ("roundClosureReceiptId", null), ("caCompletionReceiptId", null), ("receipts", new JsonArray()), ("closed", false));
    }
    // Isolated arithmetic branches exercise this handler; those states never become ingress authority.
    internal static (JsonObject State, byte[]? Event, string? Receipt) Transition(PaidAssaultFrame ctx, JsonObject prior, JsonObject input, bool admissionEnabled, bool replay = false)
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
            s["world"] = Projected(ctx, s, cut, wk == "retreat" ? choice : null, wk == "custody" ? choice : null);
            effect = O(("kind", wk == "retreat" ? "disposition-recorded" : "custody-settled"), ("reason", reason), ("timing", timing), ("payload", s["world"]!["settlements"]![0]![cut]));
            s["status"] = cut; s["window"] = null;
        }
        if (kind == "commit")
        {
            Require(T(prior, "status") == "prepared" && now is null && available, 6); if (!replay) { Require(admissionEnabled, 7); CertifySupport(ctx); }
            var (paid, costs) = PaidWorld(ctx); var allocations = A(ctx.Prior["slots"]!.AsArray().Select(x => x!["allocation"]));
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
            Require(T(prior, "status") == "committed" && now is null && available, 6); var result = Resolve(s); s["result"] = result; s["randomState"] = result["afterRandomState"]!.DeepClone(); s["world"] = Projected(ctx, s, "resolved");
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
                    var cut = status == "disposition" ? "losses" : status == "losses" ? "retreat" : "relationships"; s["world"] = Projected(ctx, s, cut); s["status"] = cut; effect = O(("kind", cut + "-settled"), ("payload", s["world"]!["settlements"]![0]![cut]));
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
                else throw new JsonException("CMB-APA-006");
            }
        }
        var cycle = ctx.Cycle;
        var acceptedEvent = O(("contractVersion", 1), ("eventType", "combat-actual-paid-" + T(effect!, "kind")), ("author", author), ("campaignId", cycle["campaignId"]),
            ("rulesetHash", cycle["rulesetHash"]), ("configurationHash", cycle["admittedPolicyBundleDigest"]), ("roundClockConfigurationHash", prior["roundClockConfigurationHash"]), ("resultClockPolicyId", ClockPolicy),
            ("admissionSourceHash", ctx.Base["admissionProof"]!["sourceHash"]), ("baseHash", prior["baseHash"]), ("cycleId", ctx.Boundary["cycleId"]), ("segmentId", ctx.Prior["segmentId"]), ("roundId", ctx.Prior["roundId"]),
            ("priorVersion", prior["stateVersion"]), ("stateVersion", N(prior, "stateVersion") + 1), ("priorPrefix", prior["prefix"]), ("input", JsonNode.Parse(Canonical(input, "PaidInput"))), ("effect", JsonNode.Parse(Canonical(effect!, "PaidEffect"))));
        var receipt = "cmb." + Domain("sandtable.combat.actual-paid-assault.receipt.v1", Encode(acceptedEvent)); acceptedEvent["receiptId"] = receipt; var data = Canonical(acceptedEvent, "PaidEvent"); var tag = T(effect!, "kind");
        if (tag == "round-closed") s["roundClosureReceiptId"] = receipt; if (tag == "ca-completed") s["caCompletionReceiptId"] = receipt;
        s["stateVersion"] = acceptedEvent["stateVersion"]!.DeepClone(); s["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(T(prior, "prefix"), data);
        s["receipts"]!.AsArray().Add(O(("commandHash", ch), ("eventHash", Hash(data)), ("receiptId", receipt), ("actor", actor), ("stateVersion", s["stateVersion"]))); _ = Canonical(s, "PaidControl"); return (s, data, receipt);
    }
}
