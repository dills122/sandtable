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
    internal static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame frame, JsonObject prior, JsonObject input)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); Require(false, 7);
        return (prior.DeepClone().AsObject(), null, string.Empty);
    }
    private static string Digest(string kind, byte[] bytes) => Domain("sandtable.combat.actual-repeated-movement-completion." + kind + ".v1", bytes);
}
