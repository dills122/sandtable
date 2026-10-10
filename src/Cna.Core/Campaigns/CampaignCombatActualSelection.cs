using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualSelectionCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualSelectionResult
{
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[]? acceptedEvent;
    internal CombatActualSelectionResult(byte[] controlBytes, byte[] proofBytes, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    { control = controlBytes.ToArray(); proof = proofBytes.ToArray(); acceptedEvent = eventBytes?.ToArray(); ReceiptId = receiptId; Duplicate = duplicate; }
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => acceptedEvent?.ToArray();
    public JsonElement Control => Parse(control, "Control");
    public JsonElement Proof => Parse(proof, "ActualSelectionProof");
    public string? ReceiptId { get; }
    public bool Duplicate { get; }
}

/// <summary>
/// Dormant native actual selection. Replay establishes consistency only relative to an independently
/// authenticated and retained caller ledger. Embedded actors/times are never authenticators.
/// Dependency digests use a caller lookup on every entry. Future authoritative callers must supply
/// retained in-memory bytes and perform acquisition I/O outside grain turns.
/// </summary>
internal static class CampaignCombatActualSelection
{
    private sealed record Frame(JsonObject Packet, JsonObject EntryProof, JsonObject Boundary, JsonObject Control, byte[][] Events);
    public static CombatActualSelectionResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedInputs, Func<string, byte[]> dependencyBytes)
    {
        var frame = Replay(source, context, trustedInputs, dependencyBytes);
        return Result(frame, frame.Control);
    }
    public static CombatActualSelectionResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedHistoryInputs, ReadOnlySpan<byte> trustedCurrentInput, Func<string, byte[]> dependencyBytes)
    {
        VerifyDependencies(dependencyBytes);
        var input = Object(trustedCurrentInput, "Input");
        var frame = Replay(source, context, trustedHistoryInputs, dependencyBytes);
        var (state, accepted, receipt, duplicate) = Transition(frame.Boundary, frame.Control, input, context.Configuration);
        if (duplicate) accepted = frame.Events.Single(e => Parse(e, "Event").GetProperty("receiptId").GetString() == receipt).ToArray();
        if (accepted is not null && !duplicate)
        {
            var packet = frame.Packet.DeepClone().AsObject();
            packet["selectionEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted));
            frame = frame with { Packet = packet };
        }
        return Result(frame, state, accepted, receipt, duplicate);
    }
    private static CombatActualSelectionResult Result(Frame frame, JsonObject control, byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var digest = Domain("source", Canonical(frame.Packet, "ActualSelectionSource"));
        var proof = new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "asrc." + digest[7..],
            ["sourceHash"] = digest,
            ["positiveEntryProof"] = frame.EntryProof.DeepClone(),
            ["boundary"] = frame.Boundary.DeepClone(),
            ["control"] = control.DeepClone(),
        };
        return new(Canonical(control, "Control"), Canonical(proof, "ActualSelectionProof"), accepted, receipt, duplicate);
    }
    private static Frame Replay(ReadOnlySpan<byte> source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> trustedInputs, Func<string, byte[]> dependencyBytes)
    {
        VerifyDependencies(dependencyBytes);
        var packet = Object(source, "ActualSelectionSource");
        Require(Number(packet, "contractVersion") == 1, 3);
        var texts = packet["selectionEventCanonicalUtf8"]!.AsArray();
        Require(texts.Count <= 16 && trustedInputs.Count == texts.Count, 1);
        var events = texts.Select(t => Encoding.ASCII.GetBytes(t!.GetValue<string>())).ToArray();
        var ledger = trustedInputs.Select(i => Object(i.ToArray(), "Input")).ToArray();
        JsonObject entryProof;
        try
        {
            var original = Encoding.ASCII.GetBytes(Text(packet, "positiveEntrySourceCanonicalUtf8"));
            var entry = CampaignCombatPositiveEntryCodec.ReadSource(original, context);
            entryProof = JsonNode.Parse(CampaignCombatPositiveEntry.Replay(entry).ProofBytes)!.AsObject();
            var retained = CampaignCombatPositiveEntryCodec.Parse(original, "PositiveSource");
            Require(entryProof["candidate"] is not null && Number(entryProof["entry"]!, "stateVersion") == 13 && retained.GetProperty("entryEventCanonicalUtf8").GetArrayLength() == 2, 4);
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-ASE-", StringComparison.Ordinal))
        { throw new JsonException("CMB-ASE-004", e); }
        var boundary = Boundary(packet, entryProof);
        var boundaryBytes = Canonical(boundary, "ActualSelectionBoundary");
        var state = new JsonObject
        {
            ["contractVersion"] = 1,
            ["boundaryHash"] = Hash(boundaryBytes),
            ["segmentId"] = "asg." + Domain("segment", boundaryBytes)[7..],
            ["stateVersion"] = Number(boundary, "priorVersion"),
            ["prefix"] = Text(boundary, "priorPrefix"),
            ["stepIndex"] = 0,
            ["selectionOutcome"] = "unopened",
            ["selection"] = null,
            ["selectionReceiptId"] = null,
            ["declineReceiptId"] = null,
            ["cancellationReceiptId"] = null,
            ["selectionWindow"] = null,
            ["rbaWindow"] = null,
            ["stepReceipts"] = new JsonArray(),
            ["receipts"] = new JsonArray(),
            ["closed"] = false,
        };
        for (var i = 0; i < events.Length; i++)
        {
            _ = Parse(events[i], "Event");
            var (after, expected, _, duplicate) = Transition(boundary, state, ledger[i], context.Configuration);
            Require(!duplicate && expected is not null && events[i].AsSpan().SequenceEqual(expected), 6);
            state = after;
        }
        return new(packet, entryProof, boundary, state, events);
    }
    private static JsonObject Boundary(JsonObject source, JsonObject proof)
    {
        var entry = proof["entry"]!;
        var packet = JsonNode.Parse(Text(source, "positiveEntrySourceCanonicalUtf8"))!;
        var weatherBytes = Encoding.ASCII.GetBytes(packet["weatherEventCanonicalUtf8"]![0]!.GetValue<string>());
        var weatherEvent = JsonNode.Parse(weatherBytes)!;
        var breakdownBytes = Encoding.ASCII.GetBytes(packet["entryEventCanonicalUtf8"]![1]!.GetValue<string>());
        var weatherKind = entry["operationStageWeather"]![0]!["kind"]!.DeepClone();
        return new JsonObject
        {
            ["contractVersion"] = 1,
            ["positiveEntrySourceId"] = proof["sourceId"]!.DeepClone(),
            ["positiveEntrySourceHash"] = proof["sourceHash"]!.DeepClone(),
            ["creationBinding"] = entry["creationBinding"]!.DeepClone(),
            ["creationEventHash"] = entry["creationEventHash"]!.DeepClone(),
            ["cycle"] = entry["cycle"]!.DeepClone(),
            ["cycleId"] = entry["cycleId"]!.DeepClone(),
            ["firstActingSide"] = entry["firstActingSide"]!.DeepClone(),
            ["priorVersion"] = entry["stateVersion"]!.DeepClone(),
            ["priorPrefix"] = entry["prefix"]!.DeepClone(),
            ["reserveCompletionReceiptId"] = entry["completionReceiptId"]!.DeepClone(),
            ["movementCompletionReceiptId"] = entry["movementEnd"]!["completionReceiptId"]!.DeepClone(),
            ["breakdownCompletionReceiptId"] = entry["breakdownCompletionReceiptId"]!.DeepClone(),
            ["breakdownCompletionEventHash"] = Hash(breakdownBytes),
            ["position"] = entry["sequencePosition"]!.DeepClone(),
            ["world"] = entry["world"]!.DeepClone(),
            ["randomState"] = entry["randomState"]!.DeepClone(),
            ["weather"] = new JsonObject { ["gameTurn"] = 1, ["operationStage"] = 1, ["attackerKind"] = weatherKind, ["defenderKind"] = weatherKind.DeepClone(), ["weatherReceiptId"] = weatherEvent["receiptId"]!.DeepClone(), ["weatherEventHash"] = Hash(weatherBytes) },
            ["breakdownFlow"] = entry["breakdownFlow"]!.DeepClone(),
            ["reactionWindow"] = null,
            ["movementEnd"] = entry["movementEnd"]!.DeepClone(),
            ["candidate"] = proof["candidate"]!.DeepClone(),
        };
    }
    /// <summary>Syntax/mechanics helper over already derived facts; never a standalone admission API.</summary>
    internal static (JsonObject State, byte[]? Event, string? Receipt, bool Duplicate) Transition(JsonObject boundary, JsonObject prior, JsonObject input, CombatDecisionConfiguration configuration)
    {
        input = Object(Canonical(input, "Input"), "Input");
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
            _ => throw new JsonException("CMB-ASE-003"),
        };
        Require(Number(cmd, "contractVersion") == 1, 3);
        foreach (var field in new[] { "decisionId", "fromPositionId", "expectedPriorVersion", "choice", "candidate", "participant" }) Require(cmd[field] is null || allowed.Contains(field, StringComparer.Ordinal), 3);
        Require(Text(cmd, "segmentId") == Text(prior, "segmentId"), 4);
        Require(kind is "choose-selection" or "decline-rba" ? actor is "axis" or "commonwealth" : actor == "system", 4);
        var commandHash = Hash(Canonical(cmd.AsObject(), "Command"));
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
                var opened = available ? Window(boundary, prior, "selection", now!.Value, configuration) : null;
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
                var rba = Window(boundary, prior, "rba", now!.Value, configuration); state["rbaWindow"] = rba.DeepClone();
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
            ["eventType"] = "actual-combat-" + Text(effect, "kind"),
            ["campaignId"] = boundary["cycle"]!["campaignId"]!.DeepClone(),
            ["rulesetHash"] = boundary["cycle"]!["rulesetHash"]!.DeepClone(),
            ["configurationHash"] = boundary["cycle"]!["admittedPolicyBundleDigest"]!.DeepClone(),
            ["positiveEntrySourceHash"] = boundary["positiveEntrySourceHash"]!.DeepClone(),
            ["cycleId"] = boundary["cycleId"]!.DeepClone(),
            ["segmentId"] = prior["segmentId"]!.DeepClone(),
            ["priorVersion"] = Number(prior, "stateVersion"),
            ["stateVersion"] = checked(Number(prior, "stateVersion") + 1),
            ["priorPrefix"] = prior["prefix"]!.DeepClone(),
            ["input"] = input.DeepClone(),
            ["effect"] = effect,
        };
        var receipt = "asc." + Domain("receipt", Encode(acceptedEvent))[7..]; acceptedEvent["receiptId"] = receipt; var bytes = Canonical(acceptedEvent, "Event");
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
        _ = Canonical(state, "Control"); return (state, bytes, receipt, false);
    }
    private static JsonObject Window(JsonObject boundary, JsonObject state, string kind, long now, CombatDecisionConfiguration configuration)
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
    private static string Text(JsonNode node, string field) => node[field]!.GetValue<string>();
    private static string? TextOrNull(JsonNode node, string field) => node[field]?.GetValue<string>();
    private static long Number(JsonNode node, string field) => JsonSerializer.SerializeToElement(node[field]).GetInt64();
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static string Domain(string kind, byte[] bytes) => CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-selection." + kind + ".v1", bytes);
    internal static void VerifyDependencies(Func<string, byte[]> dependencyBytes)
    {
        ArgumentNullException.ThrowIfNull(dependencyBytes);
        foreach (var pin in Dependencies)
        {
            try { Require(Hash(dependencyBytes(pin.Key).ToArray()) == "sha256:" + pin.Value, 9); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or KeyNotFoundException) { throw new JsonException("CMB-ASE-009", e); }
        }
    }
    private static readonly Dictionary<string, string> Dependencies = new(StringComparer.Ordinal)
    {
        ["docs/specs/combat-positive-entry-v1.schema.json"] = "73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36",
        ["docs/specs/fixtures/combat-positive-entry-v1.json"] = "eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e",
        ["docs/specs/verify-combat-positive-entry-v1.py"] = "b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172",
        ["docs/specs/combat-reserve-designation-v1.schema.json"] = "f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9",
        ["docs/specs/fixtures/combat-reserve-designation-v1.json"] = "6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2",
        ["docs/specs/verify-combat-reserve-designation-v1.py"] = "fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465",
        ["docs/specs/verify-combat-stage-entry-v1.py"] = "85cd2b32f9ad104ec29034180775e1ac1905e5aa7f78caa3c1a26e6675901435",
        ["docs/specs/verify-combat-inherited-movement-lifecycle-v1.py"] = "f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920",
        ["docs/specs/combat-inherited-movement-lifecycle-v1.schema.json"] = "aac4e07f9546d457defd9042ae701434683e7c22fab2071fe88f872d8d6bc072",
        ["docs/specs/verify-combat-inherited-breakdown-completion-v1.py"] = "db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba",
        ["docs/specs/combat-inherited-breakdown-completion-v1.schema.json"] = "1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3",
        ["docs/specs/verify-combat-inherited-selection-v1.py"] = "99e0cdb1cc78089e4af93b8e995ba04483f6bdab86edaf7c6e2e251442cd0bb7",
        ["docs/specs/verify-combat-selection-steps-v1.py"] = "dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86",
        ["docs/specs/combat-selection-steps-v1.schema.json"] = "ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d",
        ["docs/specs/verify-combat-sealed-round-v2.py"] = "d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9",
        ["docs/specs/verify-combat-result-settlement-v2.py"] = "a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2",
    };
}
