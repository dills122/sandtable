using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualRoundEntryCodec;
namespace Cna.Core.Campaigns;

internal sealed class CombatActualRoundEntryResult
{
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[]? acceptedEvent;
    internal CombatActualRoundEntryResult(byte[] controlBytes, byte[] proofBytes, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    { control = controlBytes.ToArray(); proof = proofBytes.ToArray(); acceptedEvent = eventBytes?.ToArray(); ReceiptId = receiptId; Duplicate = duplicate; }
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public byte[]? EventBytes => acceptedEvent?.ToArray();
    public JsonElement Control => Parse(control, "RoundControl");
    public JsonElement Proof => Parse(proof, "ActualRoundProof");
    public string? ReceiptId { get; }
    public bool Duplicate { get; }
}

/// <summary>
/// Private actual round entry, stopping before costs or positive Close Assault completion.
/// Both caller ledgers must be independently authenticated and retained; embedded input is only
/// consistency evidence. Dependency byte acquisition belongs outside authoritative turns.
/// No decoded Base or prior control is accepted as an admission authority.
/// </summary>
internal static class CampaignCombatActualRoundEntry
{
    internal sealed record ReplayFacts(JsonObject Base, string BaseHash, string ClockHash, string[] Route);
    private sealed record Frame(JsonObject Packet, JsonObject Base, JsonObject Control, byte[][] Events, ReplayFacts Facts, ReplayMemo.Entry? MemoEntry = null, ReplayMemo? Memo = null);
    // Cache only successful authenticated history; fresh command outcomes never enter this memo.
    private static readonly ReplayMemo Memo = new(128, 16 * 1024 * 1024, 1024 * 1024);
    internal sealed class ReplayMemo
    {
        internal const int MaximumKeyBytes = 2 * 1024 * 1024;
        private readonly object gate = new();
        private readonly Dictionary<string, LinkedListNode<Entry>> entries = new(StringComparer.Ordinal);
        private readonly LinkedList<Entry> order = new();
        private long retainedBytes;
        internal ReplayMemo(int maximumEntries, long maximumBytes, long maximumEntryBytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maximumEntries);
            ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
            ArgumentOutOfRangeException.ThrowIfNegative(maximumEntryBytes);
            MaximumEntries = maximumEntries; MaximumBytes = maximumBytes; MaximumEntryBytes = maximumEntryBytes;
        }
        internal int MaximumEntries { get; }
        internal long MaximumBytes { get; }
        internal long MaximumEntryBytes { get; }
        internal (int Count, long Bytes) Usage { get { lock (gate) return (entries.Count, retainedBytes); } }
        internal sealed class Entry(byte[] evidence, byte[] baseBytes, byte[] control, byte[][] events, ReplayFacts facts, bool consumedAa)
        {
            internal readonly byte[] Evidence = evidence;
            internal readonly byte[] BaseBytes = baseBytes;
            internal readonly byte[] ControlBytes = control;
            internal readonly byte[][] Events = events;
            internal readonly string BaseHash = facts.BaseHash;
            internal readonly string ClockHash = facts.ClockHash;
            internal readonly string[] Route = facts.Route.ToArray();
            internal readonly bool ConsumedAa = consumedAa;
            internal readonly string Key = Hash(evidence);
            internal long Bytes = evidence.LongLength + baseBytes.LongLength + control.LongLength + events.Sum(x => x.LongLength) + 512 + facts.Route.Sum(x => x.Length * 2L);
            internal CombatActualRoundEntryResult? Result;
        }
        internal Entry? Find(byte[] evidence)
        {
            var key = Hash(evidence);
            lock (gate)
            {
                if (!entries.TryGetValue(key, out var node) || !evidence.AsSpan().SequenceEqual(node.Value.Evidence)) return null;
                order.Remove(node); order.AddLast(node); return node.Value;
            }
        }
        internal Entry? Retain(Entry entry)
        {
            if (MaximumEntries == 0 || entry.Bytes > MaximumEntryBytes || entry.Bytes > MaximumBytes) return null;
            lock (gate)
            {
                if (entries.TryGetValue(entry.Key, out var existing)) return entry.Evidence.AsSpan().SequenceEqual(existing.Value.Evidence) ? existing.Value : null;
                while (entries.Count >= MaximumEntries || retainedBytes + entry.Bytes > MaximumBytes)
                {
                    var oldest = order.First!; order.RemoveFirst(); entries.Remove(oldest.Value.Key); retainedBytes -= oldest.Value.Bytes;
                }
                entries.Add(entry.Key, order.AddLast(entry)); retainedBytes += entry.Bytes; return entry;
            }
        }
        internal CombatActualRoundEntryResult? ReadResult(Entry entry) { lock (gate) return entry.Result; }
        internal void RetainResult(Entry entry, CombatActualRoundEntryResult result)
        {
            var extra = result.ControlBytes.LongLength + result.ProofBytes.LongLength + 128;
            lock (gate)
            {
                if (entry.Result is not null || !entries.TryGetValue(entry.Key, out var node) || !ReferenceEquals(node.Value, entry) ||
                    entry.Bytes + extra > MaximumEntryBytes || retainedBytes + extra > MaximumBytes) return;
                entry.Result = result; entry.Bytes += extra; retainedBytes += extra;
            }
        }
    }
    private sealed record MemoEvidence(byte[] Bytes, byte[][] SelectionInputs);
    private static MemoEvidence? SnapshotEvidence(byte[] source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> selectionInputs, byte[][] roundInputs)
    {
        // Ineligible or oversize keys follow the unchanged cold validation path.
        if (context is null || selectionInputs is null || selectionInputs.Count > 512 || selectionInputs.Any(x => x is null)) return null;
        long size = source.LongLength + roundInputs.Sum(x => x.LongLength) + 192;
        foreach (var item in selectionInputs) { size += item.LongLength + 4; if (size > ReplayMemo.MaximumKeyBytes) return null; }
        var setup = CampaignSetupV7Codec.Serialize(context.Setup);
        var configuration = CombatDecisionConfigurationCodec.Serialize(context.Configuration);
        size += setup.LongLength + configuration.LongLength + roundInputs.Length * 4L;
        if (size > ReplayMemo.MaximumKeyBytes) return null;
        var originals = selectionInputs.Select(x => x.ToArray()).ToArray();
        using var stream = new MemoryStream((int)size);
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write("sandtable.actual-round-entry.memo.v1");
            void Bytes(byte[] value) { writer.Write(value.Length); writer.Write(value); }
            Bytes(source); Bytes(setup); Bytes(configuration); writer.Write(context.RulesetHash);
            writer.Write(originals.Length); foreach (var item in originals) Bytes(item);
            writer.Write(roundInputs.Length); foreach (var item in roundInputs) Bytes(item);
        }
        return stream.Length <= ReplayMemo.MaximumKeyBytes ? new(stream.ToArray(), originals) : null;
    }
    private const long MaximumUtc = 253402300799999;
    private const string ContentPath = "docs/specs/fixtures/combat-content-v7.canonical.json";
    private static readonly string[] Unsupported = ["commit-attack", "resolve-attack", "release-reserves", "repeat-cycle", "finish-cycle", "open-later-stage", "consume-lineage", "refund", "reseed"];

    public static CombatActualRoundEntryResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedSelectionInputs, IReadOnlyList<byte[]> trustedRoundInputs, Func<string, byte[]> dependencyBytes)
    { return ReplayTrustedSource(source, context, trustedSelectionInputs, trustedRoundInputs, dependencyBytes, Memo); }

    internal static CombatActualRoundEntryResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedSelectionInputs, IReadOnlyList<byte[]> trustedRoundInputs, Func<string, byte[]> dependencyBytes, ReplayMemo memo)
    { var frame = Replay(source, context, trustedSelectionInputs, trustedRoundInputs, dependencyBytes, memo); return Result(frame, frame.Control); }

    public static CombatActualRoundEntryResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedSelectionInputs, IReadOnlyList<byte[]> trustedRoundInputs, ReadOnlySpan<byte> currentInput,
        Func<string, byte[]> dependencyBytes, bool admissionEnabled = true)
        => ApplyTrustedSource(source, context, trustedSelectionInputs, trustedRoundInputs, currentInput, dependencyBytes, admissionEnabled, Memo);

    internal static CombatActualRoundEntryResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> trustedSelectionInputs, IReadOnlyList<byte[]> trustedRoundInputs, ReadOnlySpan<byte> currentInput,
        Func<string, byte[]> dependencyBytes, bool admissionEnabled, ReplayMemo memo)
    {
        VerifyDependencies(dependencyBytes);
        var input = Object(currentInput, "RoundInput");
        var frame = Replay(source, context, trustedSelectionInputs, trustedRoundInputs, dependencyBytes, memo);
        var (state, accepted, receipt, duplicate) = Transition(frame.Facts, frame.Control, input, dependencyBytes, admissionEnabled);
        if (duplicate) accepted = frame.Events.Single(e => Parse(e, "RoundEvent").GetProperty("receiptId").GetString() == receipt).ToArray();
        if (accepted is not null && !duplicate)
        {
            var packet = frame.Packet.DeepClone().AsObject();
            packet["roundEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted));
            frame = frame with { Packet = packet };
        }
        return Result(frame, state, accepted, receipt, duplicate);
    }

    private static CombatActualRoundEntryResult Result(Frame frame, JsonObject state, byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var reusable = ReferenceEquals(state, frame.Control) && accepted is null;
        if (reusable && frame.MemoEntry is not null && frame.Memo!.ReadResult(frame.MemoEntry) is { } retained)
            return new(retained.ControlBytes, retained.ProofBytes);
        var sourceHash = Domain("source", Canonical(frame.Packet, "ActualRoundSource"));
        var proof = new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "arsrc." + sourceHash[7..],
            ["sourceHash"] = sourceHash,
            ["base"] = frame.Base.DeepClone(),
            ["control"] = state.DeepClone(),
        };
        var result = new CombatActualRoundEntryResult(Canonical(state, "RoundControl"), Canonical(proof, "ActualRoundProof"), accepted, receipt, duplicate);
        if (reusable && frame.MemoEntry is not null) frame.Memo!.RetainResult(frame.MemoEntry, result);
        return result;
    }

    private static Frame Replay(ReadOnlySpan<byte> source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> selectionInputs,
        IReadOnlyList<byte[]> roundInputs, Func<string, byte[]> dependencyBytes, ReplayMemo? memo = null)
    {
        VerifyDependencies(dependencyBytes);
        memo ??= Memo;
        var ownedSource = source.Length <= 1_048_576 ? source.ToArray() : null;
        ReadOnlySpan<byte> retainedSource = ownedSource is null ? source : ownedSource;
        var packet = Object(retainedSource, "ActualRoundSource"); Require(Number(packet, "contractVersion") == 1, 3);
        var texts = packet["roundEventCanonicalUtf8"]!.AsArray();
        Require(texts.Count <= 16 && roundInputs is not null && roundInputs.Count == texts.Count, 1);
        var retainedInputs = roundInputs!.Select(i =>
        {
            Require(i is not null, 1);
            if (i!.Length > 1_048_576) _ = Object(i, "RoundInput");
            var bytes = i.ToArray(); return (Bytes: bytes, Input: Object(bytes, "RoundInput"));
        }).ToArray();
        var inputs = retainedInputs.Select(x => x.Input).ToArray();
        var evidence = SnapshotEvidence(ownedSource!, context, selectionInputs, retainedInputs.Select(x => x.Bytes).ToArray());
        if (evidence is not null && memo.Find(evidence.Bytes) is { } hit)
        {
            if (hit.ConsumedAa) _ = PinnedBytes(ContentPath, dependencyBytes);
            var retainedBase = JsonNode.Parse(hit.BaseBytes)!.AsObject();
            var retainedFacts = new ReplayFacts(retainedBase, hit.BaseHash, hit.ClockHash, hit.Route.ToArray());
            return new(packet, retainedBase, JsonNode.Parse(hit.ControlBytes)!.AsObject(), hit.Events.Select(x => x.ToArray()).ToArray(), retainedFacts, hit, memo);
        }
        var events = texts.Select(t => Encoding.ASCII.GetBytes(t!.GetValue<string>())).ToArray();
        var derived = DeriveBase(packet, context, evidence?.SelectionInputs ?? selectionInputs, dependencyBytes);
        var facts = new ReplayFacts(derived, BaseHash(derived), ClockHash(derived), Route(derived));
        var state = Initial(facts);
        for (var i = 0; i < events.Length; i++)
        {
            _ = Parse(events[i], "RoundEvent");
            var (after, expected, _, duplicate) = Transition(facts, state, inputs[i], dependencyBytes);
            Require(!duplicate && expected is not null && events[i].AsSpan().SequenceEqual(expected), 6); state = after;
        }
        var entry = evidence is null ? null : memo.Retain(new ReplayMemo.Entry(evidence.Bytes, Encode(derived), Encode(state),
            events.Select(x => x.ToArray()).ToArray(), facts, Text(state, "status") == "prepared" && Number(state, "stepIndex") >= 5));
        return new(packet, derived, state, events, facts, entry, memo);
    }

    private static JsonObject DeriveBase(JsonObject packet, CampaignCombatCreationContext context, IReadOnlyList<byte[]> selectionInputs, Func<string, byte[]> dependencyBytes)
    {
        var configuration = new JsonObject
        {
            ["contractVersion"] = 1,
            ["parentConfigurationHash"] = context.Configuration.Hash,
            ["timingPolicyId"] = "sandtable.combat.public-opening-clock.v2",
            ["decisionBudgetMilliseconds"] = context.Configuration.Windows.Single(x => x.Kind == "force-assignment").DecisionBudgetMilliseconds,
        };
        Require(JsonNode.DeepEquals(packet["clockConfiguration"], configuration), 4);
        JsonObject selectionProof;
        try
        {
            Require(selectionInputs is not null && selectionInputs.All(x => x is not null), 4);
            var retained = Encoding.ASCII.GetBytes(Text(packet, "actualSelectionSourceCanonicalUtf8"));
            var result = CampaignCombatActualSelection.ReplayTrustedSource(retained, context, selectionInputs!, dependencyBytes);
            selectionProof = JsonNode.Parse(result.ProofBytes)!.AsObject();
            Require(Object(retained, "ActualSelectionSource")["selectionEventCanonicalUtf8"]!.AsArray().Count == 7, 4);
        }
        catch (JsonException e) { throw new JsonException("CMB-ARE-004", e); }
        var selected = selectionProof["control"]!;
        Require(Number(selected, "stateVersion") == 20 && Number(selected, "stepIndex") == 3 && Text(selected, "selectionOutcome") == "selected" &&
            selected["declineReceiptId"] is not null && selected["cancellationReceiptId"] is null && selected["stepReceipts"]!.AsArray().Count == 3 && !selected["closed"]!.GetValue<bool>(), 4);
        return Object(Canonical(new JsonObject { ["contractVersion"] = 1, ["actualSelectionProof"] = selectionProof, ["clockConfiguration"] = configuration }, "Base"), "Base");
    }

    private static JsonObject Initial(ReplayFacts facts)
    {
        var derived = facts.Base;
        var prior = Selection(derived); var boundary = Boundary(derived);
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

    /// <summary>Private mechanics over derived facts. Public APIs always replay the original source and both ledgers first.</summary>
    private static (JsonObject State, byte[]? Event, string? Receipt, bool Duplicate) Transition(ReplayFacts facts, JsonObject prior,
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
                    ["actualSelectionSourceHash"] = derived["actualSelectionProof"]!["sourceHash"]!.DeepClone(),
                    ["segmentId"] = state["segmentId"]!.DeepClone(),
                    ["cycleId"] = Boundary(derived)["cycleId"]!.DeepClone(),
                    ["positionId"] = route[3],
                    ["candidate"] = Selection(derived)["selection"]!.DeepClone(),
                    ["declineReceiptId"] = Selection(derived)["declineReceiptId"]!.DeepClone(),
                };
                state["opportunityId"] = "aopp." + Domain("opportunity", Encode(identity))[7..];
                state["roundId"] = "arnd." + Domain("round", Encode(new JsonObject
                {
                    ["baseHash"] = facts.BaseHash,
                    ["opportunityId"] = state["opportunityId"]!.DeepClone(),
                    ["openingAuthorityVersion"] = state["stateVersion"]!.DeepClone(),
                    ["openingHistoryPrefix"] = state["prefix"]!.DeepClone(),
                    ["timing"] = timing.DeepClone(),
                }))[7..];
                foreach (var role in new[] { "attacker", "defender" })
                {
                    var participant = Selection(derived)["selection"]![role]!;
                    state["slots"]!.AsArray().Add(new JsonObject
                    {
                        ["role"] = role,
                        ["owner"] = participant["unit"]!["originalSide"]!.DeepClone(),
                        ["slotId"] = "aslt." + Domain("slot", Encode(new JsonObject { ["roundId"] = state["roundId"]!.DeepClone(), ["role"] = role }))[7..],
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
            default: Require(now is null && available, 5); throw new JsonException("CMB-ARE-007");
        }
        var acceptedEvent = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = "actual-round-" + Text(effect, "kind"),
            ["author"] = author,
            ["campaignId"] = Boundary(derived)["cycle"]!["campaignId"]!.DeepClone(),
            ["rulesetHash"] = Boundary(derived)["cycle"]!["rulesetHash"]!.DeepClone(),
            ["configurationHash"] = facts.ClockHash,
            ["predecessorConfigurationHash"] = derived["clockConfiguration"]!["parentConfigurationHash"]!.DeepClone(),
            ["actualSelectionSourceHash"] = derived["actualSelectionProof"]!["sourceHash"]!.DeepClone(),
            ["baseHash"] = facts.BaseHash,
            ["cycleId"] = Boundary(derived)["cycleId"]!.DeepClone(),
            ["segmentId"] = state["segmentId"]!.DeepClone(),
            ["roundId"] = state["roundId"]!.DeepClone(),
            ["priorVersion"] = Number(prior, "stateVersion"),
            ["stateVersion"] = checked(Number(prior, "stateVersion") + 1),
            ["priorPrefix"] = prior["prefix"]!.DeepClone(),
            ["input"] = input.DeepClone(),
            ["effect"] = Object(Canonical(effect, "RoundEffect"), EffectKind(effect)),
        };
        var receipt = "arc." + Domain("receipt", Encode(acceptedEvent))[7..]; acceptedEvent["receiptId"] = receipt;
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
        var candidate = Selection(derived)["selection"]!; var ids = new JsonArray();
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
        return Domain("empty-aa", Canonical(certificate, "EmptyAaCertificate"));
    }
    private static string EffectKind(JsonObject effect) => Text(effect, "kind") switch { "round-opened" => "RoundOpen", "choice-sealed" => "RoundSeal", "round-cancelled" => "RoundCancel", _ => "RoundStep" };
    private static string Gate(JsonNode timing, long? now, bool available) => !available || now is null || now < Number(timing, "openingFloorUnixMilliseconds") ? "unavailable" : now >= Number(timing, "deadlineUnixMilliseconds") ? "expired" : "live";
    private static JsonNode Selection(JsonObject derived) => derived["actualSelectionProof"]!["control"]!;
    private static JsonNode Boundary(JsonObject derived) => derived["actualSelectionProof"]!["boundary"]!;
    private static string[] Route(JsonObject derived) => Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != Text(Boundary(derived)["position"]!, "positionId")).Take(7).Select(p => p.PositionId).ToArray();
    private static string BaseHash(JsonObject derived) => Domain("base", Canonical(derived, "Base"));
    private static string ClockHash(JsonObject derived) => Domain("configuration", Canonical(derived["clockConfiguration"]!, "ClockConfiguration"));
    private static string Text(JsonNode value, string field) => value[field]!.GetValue<string>();
    private static string? TextOrNull(JsonNode value, string field) => value[field]?.GetValue<string>();
    private static long Number(JsonNode value, string field) => JsonSerializer.SerializeToElement(value[field]).GetInt64();
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static string Domain(string kind, byte[] bytes) => CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-round-entry." + kind + ".v1", bytes);
    private static byte[] PinnedBytes(string path, Func<string, byte[]> dependencyBytes)
    {
        try { var data = dependencyBytes(path).ToArray(); Require(Hash(data) == "sha256:" + Dependencies[path], 9); return data; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or KeyNotFoundException or NullReferenceException) { throw new JsonException("CMB-ARE-009", e); }
    }
    internal static void VerifyDependencies(Func<string, byte[]> dependencyBytes)
    { ArgumentNullException.ThrowIfNull(dependencyBytes); foreach (var path in Dependencies.Keys) _ = PinnedBytes(path, dependencyBytes); }
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
        ["docs/research/fixtures/combat-selected-source-v1.json"] = "e225bc650c4bb942e41245a33da20c21c576ab117807a06805e2558a2c6468df",
        ["docs/research/verify-combat-rng.py"] = "f3c7c468f2be0efa583e44c4d04b8fe6d20eef51aae01554bfa535b0ecc0ea30",
        ["docs/research/verify-combat-source-freeze.py"] = "f21a97150e344337f6ee4c2c1a1752ac7d41b8b8c8bf6a61a06a1fe5a0483e7e",
        ["docs/specs/combat-actual-selection-v1.schema.json"] = "da6256deb94bb6061e8e98e2448f915c4474373b7f0de6bb76eded2ae88c39b1",
        ["docs/specs/combat-authority-envelope-v1.schema.json"] = "91f90ba47cdfff088b3d8e5dba55c6c283d41c96381e56c7bba115bfb5390550",
        ["docs/specs/combat-cycle-control-v1.schema.json"] = "8c517db6daa734d415bd1d474280c2cf6e65013490c4a2568795d50e6112f7da",
        ["docs/specs/combat-cycle-sequence-v1.schema.json"] = "6e09dc5bc6ca79d8580db6ed1e76ad5007c26a30df11b4dd9da0a9c7b5f5e0c5",
        ["docs/specs/combat-inherited-movement-v1.schema.json"] = "a9ad67ee9b9302f17582fb5087f6dd6e7e62574741e6cea7b5a3e58bf7fe2069",
        ["docs/specs/combat-inherited-successors-v1.schema.json"] = "474d8b5b22457921082c1799c89a42d9ec448b5c6ea131a3458f364ff980babc",
        ["docs/specs/combat-opening-preamble-v1.schema.json"] = "d01bd4eb60ce3a83a4d119631ac72ccce082ff13cdf92f861d42fb83566e7d59",
        ["docs/specs/combat-ordinary-movement-v1.schema.json"] = "039d5f3cdb908dc761540b4809bfb9c6e77c9ab6efd8c965e69955fbbcf0728a",
        ["docs/specs/combat-reserve-release-v1.schema.json"] = "10ba1ca46e05fbed52c6eef5174c2ac84c4282df0957c44eab6e86079342bc32",
        ["docs/specs/combat-result-settlement-v1.schema.json"] = "f65c13687bb2fe3fbabdbd82d48298430330dab756583fbfda17dae50bc73ab0",
        ["docs/specs/combat-rules-inputs-v1.schema.json"] = "1477a9e755a091bbf4539d00184711b18eab2f6289f643596d841b8c314f4797",
        ["docs/specs/combat-sealed-round-v1.schema.json"] = "5556404caa296ce41020654f8fb64c0349fd13051edabf6b4934e47d675c32db",
        ["docs/specs/combat-stage-entry-v1.schema.json"] = "5d0f85fec61f219e7fd3575037894c147580ec9fe1570603d621c9d76b38889e",
        ["docs/specs/combat-weather-v1.schema.json"] = "44f2fa44af30bf723ecac88db08fc9efac85e9880624635fc73f4dc52bd9459f",
        ["docs/specs/combat-world-settlement-v1.schema.json"] = "f892545f5dac693c55d1180ad771d95ffeec508176a0d6139b82752ed46bcf0e",
        ["docs/specs/fixtures/combat-actual-selection-v1.json"] = "019d1a3ff0f121d83f377ddfb19d274b4a8aa89172bad8aeb289c3b98228e604",
        ["docs/specs/fixtures/combat-authority-envelope-v1.json"] = "adf7b05e15f863b08f366809adf91c97b39480e404ce2efff15e838fa4043eaa",
        ["docs/specs/fixtures/combat-content-v7.canonical.json"] = "ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
        ["docs/specs/fixtures/combat-creation-ledger-v1.json"] = "4b7f87f73a5882800e4e4befb4c06c20bf19c419f92edba5028b355e37442684",
        ["docs/specs/fixtures/combat-cycle-sequence-v1.json"] = "de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f",
        ["docs/specs/fixtures/combat-rules-inputs-v1.json"] = "5401cd9a690be8b81f19795f1f66c0c6fe24f5055a06cda5facd6193b3e0a29a",
        ["docs/specs/verify-combat-actual-selection-v1.py"] = "f37a7cfa26b60469168d9f4424465b14f1fe45666f4222d69c840c2527d2e66f",
        ["docs/specs/verify-combat-authority-envelope-v1.py"] = "a12d8ce1ab142b9a7f1c58e07e5b0d7928c4693d869423aa24ade81cb21e6231",
        ["docs/specs/verify-combat-content-v7.py"] = "eaab770988e8d2b0f11cb9a8a39b45f961977aa9048f95de8bf32f46e361bcbd",
        ["docs/specs/verify-combat-creation-ledger-v1.py"] = "04777d27347b943cbb4ec06efeee6f3006694d1acd88b9bc8e25eb5dcb5841e4",
        ["docs/specs/verify-combat-cycle-control-v1.py"] = "9039f9e1067b2cdd2e0a57ae16e711350b4bbdceb500d22bbbd715f39e8ccc9f",
        ["docs/specs/verify-combat-cycle-sequence-v1.py"] = "d43ae974e1f6e2c30067efe4635df655c6d2e4791968ece00dfd473145c3cc8a",
        ["docs/specs/verify-combat-inherited-movement-v1.py"] = "c10c51cd97fca3f3f04fc0983e90d474a35a6b58f4a2cce396272916910579e1",
        ["docs/specs/verify-combat-inherited-successors-v1.py"] = "f9cbf73f2045c304c96fc8dcf4d09ba10026db87ff5446c751ad22e19e4fe5de",
        ["docs/specs/verify-combat-opening-preamble-v1.py"] = "50f75c1032f5b17000accb288107a1e047940cadedd5c8d30c469db167c0e5d0",
        ["docs/specs/verify-combat-ordinary-movement-v1.py"] = "cc75e52a6d160d4b0c6c880dc43bc65742068f83ef85cc7fb4babbf8756e0dff",
        ["docs/specs/verify-combat-reserve-release-v1.py"] = "105ef18d9db6364ce42f9831afd3c33010fccc71b0ae60af78cec7c185da892b",
        ["docs/specs/verify-combat-result-settlement-v1.py"] = "1f7dff5b87ce036c406562686f4dc1ce983a4a8dfa15b2401eae72a0d9879ffd",
        ["docs/specs/verify-combat-rules-inputs-v1.py"] = "ffb6daf98db7097d307ac7c185bf7d68a2ff1d0ac9a357ea063a746aab42081d",
        ["docs/specs/verify-combat-sealed-round-v1.py"] = "497e295e71c4b4da43539ee9b6c30de6d33f409a2f14b6cc24d9030390525ae0",
        ["docs/specs/verify-combat-weather-v1.py"] = "c5f92de66c8c341553fe204a5ec1b0acffb162e61ee90332c14fd608f986ade8",
        ["docs/specs/verify-combat-world-settlement-v1.py"] = "d5f49e5a4165ec568f0a8896ea491341a639da256bcfd573998f529e80ef9f25",
        ["tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.weather-tables.v1.golden.json"] = "7f134b9c5dcc356d48a9dfc51976a166b60f02fa93e73b4c0ccfdd354380c182",
    };
}
