using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatSettledControlCodec;

namespace Cna.Core.Campaigns;

/// <summary>Admission retains only owned immutable bytes; projections are freshly parsed.</summary>
internal sealed class CombatSettledControlSource
{
    private readonly byte[] basis;
    private readonly byte[] proof;
    private CombatSettledControlSource(ReadOnlySpan<byte> basis, ReadOnlySpan<byte> proof, CombatDecisionConfiguration configuration)
    {
        this.basis = basis.ToArray(); this.proof = proof.ToArray();
        ConfigurationHash = configuration.Hash;
        Budget = configuration.Windows.Single(w => w.Kind == "cycle-control").DecisionBudgetMilliseconds;
    }
    internal static CombatSettledControlSource Admit(ReadOnlySpan<byte> bytes, CampaignCombatCreationContext context)
    {
        var node = Parse(bytes, "SettledControlBase"); Require(node.GetProperty("contractVersion").GetInt32() == 1, 3);
        try
        {
            var proof = CampaignCombatSettledContinuationCodec.ReadProof(Encoding.UTF8.GetBytes(Text(node, "proofCanonicalUtf8")),
                Encoding.UTF8.GetBytes(Text(node, "packetCanonicalUtf8")), context);
            return new(bytes, proof.CanonicalBytes, context.Configuration);
        }
        catch (JsonException e) { throw new JsonException("CMB-SCC-004", e); }
    }
    public byte[] CanonicalBytes => basis.ToArray();
    internal JsonObject Proof => JsonNode.Parse(proof)!.AsObject();
    internal string ProofId => "sct." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.settled-continuation.v1", proof)[7..];
    internal string ConfigurationHash { get; }
    internal long Budget { get; }
}
internal sealed class CombatSettledControlState
{
    private readonly byte[] bytes;
    internal CombatSettledControlState(JsonObject state) => bytes = Checked(state, "SettledControlState");
    public byte[] CanonicalBytes => bytes.ToArray();
    public JsonElement Projection => Parse(bytes, "SettledControlState");
}
internal sealed class CombatSettledControlResult(CombatSettledControlState state, CombatStepsDisposition disposition, byte[]? eventBytes, string? receiptId)
{
    private readonly byte[]? bytes = eventBytes?.ToArray();
    public CombatSettledControlState State { get; } = state;
    public CombatStepsDisposition Disposition { get; } = disposition;
    public byte[]? EventBytes => bytes?.ToArray();
    public string? ReceiptId { get; } = receiptId;
}

/// <summary>Private Result2 control; never executes successor Movement or Truck Convoy.</summary>
internal static class CampaignCombatSettledControl
{
    public static byte[] Replay(byte[] basis, CampaignCombatCreationContext context, IReadOnlyList<byte[]> inputs, IReadOnlyList<byte[]> events) =>
        Replay(ReadBase(basis, context), inputs, events).CanonicalBytes;
    public static CombatSettledControlState Replay(CombatSettledControlSource source, IReadOnlyList<byte[]> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source); var (captured, retained) = Capture(inputs, events);
        var state = Initial(source);
        for (var i = 0; i < captured.Length; i++)
        {
            var result = Transition(source, state, captured[i]);
            Require(result.Disposition == CombatStepsDisposition.Accepted && retained[i].AsSpan().SequenceEqual(result.EventBytes), 6);
            state = result.State;
        }
        return state;
    }
    public static CombatSettledControlResult Apply(CombatSettledControlSource source, IReadOnlyList<byte[]> inputs, IReadOnlyList<byte[]> events,
        byte[] input, byte[]? cachedState = null)
    {
        var cache = cachedState?.ToArray(); var (captured, retained) = Capture(inputs, events);
        var state = Replay(source, captured, retained);
        if (cache is not null) _ = ReadState(cache, source, captured, retained);
        var result = Transition(source, state, Owned(input, "SettledControlInput"));
        if (result.Disposition != CombatStepsDisposition.Duplicate) return result;
        var receipts = state.Projection.GetProperty("receipts").EnumerateArray().ToArray();
        var index = Array.FindIndex(receipts, r => Text(r, "receiptId") == result.ReceiptId);
        return new(state, result.Disposition, retained[index], result.ReceiptId);
    }
    public static CombatCycleControlCommand Command(CombatSettledControlSource source, CombatSettledControlState state, string kind)
    {
        var s = state.Projection;
        return new(1, kind, Text(s, "controlId"), CycleId(source.Proof["cycle"]!), kind is "expire" or "unavailable" ? null : s.GetProperty("stateVersion").GetInt64(),
            kind == "open" ? null : s.GetProperty("decisionId").GetString());
    }
    private static (byte[][], byte[][]) Capture(IReadOnlyList<byte[]> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        Require(inputs.Count == events.Count && events.Count <= 2, 1);
        var captured = inputs.Select(b => Owned(b, "SettledControlInput")).ToArray(); var retained = events.Select(b => Owned(b, "SettledControlEvent")).ToArray();
        return (captured, retained);
    }
    private static byte[] Owned(byte[] bytes, string kind)
    {
        if (bytes is null) throw new JsonException("CMB-SCC-001");
        _ = Parse(bytes, kind); return bytes.ToArray();
    }
    private static CombatSettledControlState Initial(CombatSettledControlSource source)
    {
        var p = source.Proof;
        var s = new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceProofId"] = source.ProofId,
            ["baseHash"] = CampaignOpeningPreambleCodec.Hash(source.CanonicalBytes),
            ["controlId"] = "sctl." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.settled-control.v1", source.CanonicalBytes)[7..],
            ["stateVersion"] = p["stateVersion"]!.DeepClone(),
            ["prefix"] = p["prefix"]!.DeepClone(),
            ["status"] = "unopened",
            ["positionId"] = p["positionId"]!.DeepClone(),
            ["activeCycle"] = p["cycle"]!.DeepClone(),
            ["closure"] = null,
            ["decisionId"] = null,
            ["timing"] = null,
            ["openingClockFailure"] = false,
            ["acceptedHighWater"] = null,
            ["assessment"] = null
        };
        foreach (var key in new[] { "members", "world", "randomState", "attackHistory", "targetUses" }) s[key] = p[key]!.DeepClone();
        s["nextCycleProgress"] = new JsonArray(); s["receipts"] = new JsonArray(); return new(s);
    }
    private static JsonObject Assessment(CombatSettledControlSource source)
    {
        var p = source.Proof;
        Require(Str(p, "witnessOutcome") == "supported" && p["unsupportedReason"] is null, 3);
        return new JsonObject
        {
            ["releaseCompletionReceiptId"] = p["releaseCompletionReceiptId"]!.DeepClone(),
            ["movementCompletionReceiptId"] = p["movementEnd"]!["proof"]!["completionReceiptId"]!.DeepClone(),
            ["progress"] = p["progress"]!.DeepClone(),
            ["witnesses"] = p["witnesses"]!.DeepClone(),
            ["combatAssessment"] = "no-own-ammunition",
            ["witnessOutcome"] = "supported",
            ["unsupportedReason"] = null,
            ["sourceProofId"] = source.ProofId
        };
    }
    internal static string Mode(JsonElement assessment)
    {
        Require(Text(assessment, "witnessOutcome") == "supported", 3);
        return assessment.GetProperty("witnesses").GetArrayLength() == 0 ? "no-continuation" : assessment.GetProperty("progress").GetArrayLength() == 0 ? "no-material-progress" : "owner-choice";
    }
    private static CombatSettledControlResult Transition(CombatSettledControlSource source, CombatSettledControlState priorState, byte[] inputBytes)
    {
        var input = Parse(inputBytes, "SettledControlInput"); var cmd = input.GetProperty("command");
        var prior = JsonNode.Parse(priorState.CanonicalBytes)!.AsObject(); var s = prior.DeepClone().AsObject();
        var c = source.Proof["cycle"]!; var kind = Text(cmd, "kind"); var actor = Text(input, "actor"); var timer = kind is "expire" or "unavailable";
        Require(cmd.GetProperty("contractVersion").GetInt32() == 1 && kind is "open" or "repeat" or "finish" or "expire" or "unavailable" or "fallback-step", 3);
        Require(Text(cmd, "controlId") == Str(prior, "controlId") && Text(cmd, "cycleId") == CycleId(c), 4);
        Require(actor == (kind is "repeat" or "finish" ? Str(c, "actingSide") : "system"), 4);
        Require((cmd.GetProperty("expectedPriorVersion").ValueKind == JsonValueKind.Null) == timer && (kind != "open" || cmd.GetProperty("decisionId").ValueKind == JsonValueKind.Null), 3);
        var commandHash = CampaignOpeningPreambleCodec.Hash(Bytes(cmd)); var receipts = prior["receipts"]!.AsArray();
        var duplicate = receipts.FirstOrDefault(r => Str(r!, "commandHash") == commandHash && Str(r!, "actor") == actor);
        if (duplicate is not null) return new(priorState, CombatStepsDisposition.Duplicate, null, Str(duplicate, "receiptId"));
        var decision = cmd.GetProperty("decisionId").GetString(); var status = Str(prior, "status"); var version = Num(prior, "stateVersion");
        if (timer && (status != "open" || decision != prior["decisionId"]?.GetValue<string>())) return new(priorState, CombatStepsDisposition.NoOp, null, null);
        Require(status is "unopened" or "open" && receipts.Count < 2 && version < long.MaxValue, 7);
        Require(timer || cmd.GetProperty("expectedPriorVersion").GetInt64() == version, 6);
        Require(kind == "open" || decision == prior["decisionId"]?.GetValue<string>(), 6);
        var now = input.GetProperty("admittedAt").ValueKind == JsonValueKind.Null ? (long?)null : input.GetProperty("admittedAt").GetInt64();
        var author = actor; var reason = "owner-choice"; string effect;
        if (kind == "open")
        {
            Require(status == "unopened", 6); s["assessment"] = Assessment(source);
            using var assessment = JsonDocument.Parse(Encode(s["assessment"]!)); var mode = Mode(assessment.RootElement);
            if (mode != "owner-choice") { effect = "phase-finished"; reason = mode; }
            else
            {
                Require(Num(c, "ordinal") < int.MaxValue && version <= long.MaxValue - 2, 7);
                var lost = !input.GetProperty("clockAvailable").GetBoolean() || now is null || now < prior["acceptedHighWater"]?.GetValue<long>();
                var valid = !lost && now <= CampaignCombatSelectionSteps.UtcMaximum - source.Budget;
                s["decisionId"] = Str(prior, "controlId") + ".decision";
                s["timing"] = valid ? new JsonObject
                {
                    ["contractVersion"] = 1,
                    ["configHash"] = source.ConfigurationHash,
                    ["kind"] = "cycle-control",
                    ["decisionBudgetMilliseconds"] = source.Budget,
                    ["openedAtUnixMilliseconds"] = now,
                    ["deadlineUnixMilliseconds"] = checked(now!.Value + source.Budget),
                    ["highWaterUnixMilliseconds"] = now
                } : null;
                s["openingClockFailure"] = !valid; if (valid) s["acceptedHighWater"] = now;
                effect = "control-opened"; s["status"] = "open";
            }
        }
        else
        {
            Require(status == "open", 5); var openingLost = prior["openingClockFailure"]!.GetValue<bool>();
            var lost = openingLost || !input.GetProperty("clockAvailable").GetBoolean() || now is null || now < prior["acceptedHighWater"]?.GetValue<long>();
            var late = prior["timing"] is not null && now >= Num(prior["timing"]!, "deadlineUnixMilliseconds");
            if (kind == "fallback-step") Require(openingLost, 5);
            if (kind == "expire" && !lost && !late) return new(priorState, CombatStepsDisposition.NoOp, null, null);
            if (kind is "repeat" or "finish" && !lost) Require(!late, 5);
            if (!lost) { s["acceptedHighWater"] = now; s["timing"]!["highWaterUnixMilliseconds"] = now; }
            if (lost || timer || kind == "fallback-step")
            {
                author = "system"; effect = "phase-finished";
                reason = openingLost ? "opening-clock-unavailable" : lost ? "clock-unavailable" : kind == "expire" ? "deadline" : "controller-unavailable";
            }
            else effect = kind == "repeat" ? "cycle-repeated" : "phase-finished";
        }
        JsonNode? successor = null; var expired = new JsonArray();
        if (effect == "cycle-repeated")
        {
            Require(Num(c, "ordinal") < int.MaxValue, 7); successor = c.DeepClone(); successor["ordinal"] = checked((int)Num(c, "ordinal") + 1);
            successor["openedAuthorityVersion"] = checked(version + 1); successor["openingPrefix"] = prior["prefix"]!.DeepClone();
            _ = CycleId(successor); s["status"] = "repeated"; s["activeCycle"] = successor.DeepClone();
            s["positionId"] = Position(c, true); s["targetUses"] = new JsonArray(); s["nextCycleProgress"] = new JsonArray();
        }
        else if (effect == "phase-finished")
        {
            s["status"] = "finished"; s["activeCycle"] = null; s["positionId"] = Position(c, false);
            foreach (var m in s["members"]!.AsArray()) if (m!["history"]!["nextMovement"] is { } exception && Str(exception, "status") == "pending") expired.Add(m["unit"]!.DeepClone());
        }
        var eventNode = new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceProofId"] = source.ProofId,
            ["eventType"] = effect switch { "control-opened" => "movement-combat-control-opened", "cycle-repeated" => "movement-combat-cycle-repeated", _ => "movement-combat-phase-finished" },
            ["author"] = author,
            ["campaignId"] = c["campaignId"]!.DeepClone(),
            ["rulesetHash"] = c["rulesetHash"]!.DeepClone(),
            ["configurationHash"] = c["admittedPolicyBundleDigest"]!.DeepClone(),
            ["cycleId"] = Text(cmd, "cycleId"),
            ["positionId"] = source.Proof["positionId"]!.DeepClone(),
            ["baseHash"] = prior["baseHash"]!.DeepClone(),
            ["controlId"] = prior["controlId"]!.DeepClone(),
            ["priorVersion"] = version,
            ["stateVersion"] = checked(version + 1),
            ["priorPrefix"] = prior["prefix"]!.DeepClone(),
            ["input"] = JsonNode.Parse(inputBytes),
            ["effect"] = new JsonObject
            {
                ["kind"] = effect,
                ["reason"] = reason,
                ["assessment"] = s["assessment"]!.DeepClone(),
                ["timing"] = s["timing"]?.DeepClone(),
                ["openingClockFailure"] = s["openingClockFailure"]!.DeepClone(),
                ["successorPositionId"] = s["positionId"]!.DeepClone(),
                ["nextCycle"] = successor,
                ["expiredUnits"] = expired
            }
        };
        var receipt = "scc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.settled-control-receipt.v1", Encode(eventNode))[7..];
        eventNode["receiptId"] = receipt; var bytes = Checked(eventNode, "SettledControlEvent");
        foreach (var m in s["members"]!.AsArray()) if (expired.Any(u => JsonNode.DeepEquals(u, m!["unit"])))
        { m!["history"]!["nextMovement"]!["status"] = "expired"; m["history"]!["nextMovement"]!["completionReceiptId"] = receipt; }
        if (effect != "control-opened") s["closure"] = new JsonObject { ["cycleId"] = Text(cmd, "cycleId"), ["ordinal"] = c["ordinal"]!.DeepClone(), ["outcome"] = s["status"]!.DeepClone(), ["receiptId"] = receipt };
        s["stateVersion"] = checked(version + 1); s["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(Str(prior, "prefix"), bytes);
        s["receipts"]!.AsArray().Add(new JsonObject { ["commandHash"] = commandHash, ["eventHash"] = CampaignOpeningPreambleCodec.Hash(bytes), ["receiptId"] = receipt, ["actor"] = actor, ["stateVersion"] = checked(version + 1) });
        return new(new(s), CombatStepsDisposition.Accepted, bytes, receipt);
    }
    private static string Position(JsonNode c, bool repeat)
    {
        Require(Num(c, "gameTurn") == 1 && Num(c, "operationStage") == 1 && Str(c, "playerPhaseSlot") == "first-acting-side", 4);
        var id = repeat ? "land.position.operation-1.first-player.movement-and-combat.movement" : "land.position.operation-1.first-player.truck-convoy-movement";
        return Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == id).PositionId;
    }
    private static string CycleId(JsonNode c) => CampaignCombatReserveCompletionCodec.CycleId(new(
        (int)Num(c, "contractVersion"), Str(c, "campaignId"), Str(c, "rulesetHash"), Str(c, "setupId"), Str(c, "setupHash"), Str(c, "contentPackId"), Str(c, "contentHash"), Str(c, "scenarioId"),
        (int)Num(c, "gameTurn"), (int)Num(c, "operationStage"), Str(c, "playerPhaseSlot"), Str(c, "actingSide") == "axis" ? LandSide.Axis : LandSide.Commonwealth,
        (int)Num(c, "ordinal"), Num(c, "openedAuthorityVersion"), Str(c, "openingPrefix"), Str(c, "admittedPolicyBundleDigest")));
    private static string Str(JsonNode node, string key) => node[key]!.GetValue<string>();
    private static long Num(JsonNode node, string key) => node[key]!.Deserialize<long>();
}
