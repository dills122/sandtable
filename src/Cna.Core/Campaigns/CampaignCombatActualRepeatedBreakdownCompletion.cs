using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedBreakdownCompletionCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatActualRepeatedBreakdownCompletionResult
{
    private readonly byte[] source;
    private readonly byte[] control;
    private readonly byte[] proof;
    private readonly byte[][] inputs;
    private readonly byte[]? acceptedEvent;

    internal CombatActualRepeatedBreakdownCompletionResult(byte[] sourceBytes, byte[] controlBytes, byte[] proofBytes,
        IReadOnlyList<byte[]> breakdownInputs, byte[]? eventBytes = null, string? receiptId = null, bool duplicate = false)
    {
        source = sourceBytes.ToArray();
        control = controlBytes.ToArray();
        proof = proofBytes.ToArray();
        inputs = breakdownInputs.Select(x => x.ToArray()).ToArray();
        acceptedEvent = eventBytes?.ToArray();
        ReceiptId = receiptId;
        Duplicate = duplicate;
    }

    public byte[] SourceBytes => source.ToArray();
    public byte[] ControlBytes => control.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public IReadOnlyList<byte[]> BreakdownInputs => inputs.Select(x => x.ToArray()).ToArray();
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

/// <summary>Private native Breakdown completion from authentic terminal RMC ancestry.</summary>
internal static class CampaignCombatActualRepeatedBreakdownCompletion
{

    internal sealed record Frame(JsonObject Base, string BaseHash, JsonObject Prior, JsonObject Cycle, LandSequencePosition Boundary, LandSequencePosition Terminal);
    private const string Kind = "complete-breakdown-segment";
    private static readonly string ActionId = Hash(Encode(O(("contractVersion", 1), ("kind", Kind), ("operationStage", 1))));
    public static CombatActualRepeatedBreakdownCompletionResult ReplayTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, movement, lifecycle, breakdown, dependencies);
        return Result(packet, frame, state, inputs);
    }
    public static CombatActualRepeatedBreakdownCompletionResult ApplyTrustedSource(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, ReadOnlySpan<byte> newInput, Func<string, byte[]> dependencies)
    {
        var (packet, frame, state, inputs) = Replay(source, context, selection, rounds, paid, continuation, movement, lifecycle, breakdown, dependencies);
        var owned = newInput.ToArray(); var (after, accepted, receipt) = Transition(frame, state, Object(owned, "Input"));
        if (accepted is not null) { packet["breakdownEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(accepted)); inputs.Add(owned); }
        Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 2048, 1); CampaignCombatActualRepeatedBreakdownCompletionCodec.VerifyDependencies(dependencies);
        return Result(packet, frame, after, inputs, accepted, receipt, accepted is null);
    }
    public static byte[] ReadSource(ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid,
        IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> lifecycle,
        IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
        => ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, breakdown, dependencies).SourceBytes;
    public static CombatActualRepeatedBreakdownCompletionResult ReadControl(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, breakdown, dependencies);
        _ = Parse(claim, "Control"); Require(claim.SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualRepeatedBreakdownCompletionResult ReadProof(ReadOnlySpan<byte> claim, ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
    {
        var result = ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, breakdown, dependencies);
        _ = Parse(claim, "Proof"); Require(claim.SequenceEqual(result.ProofBytes), 6); return result;
    }
    private static (JsonObject Packet, Frame Frame, JsonObject State, List<byte[]> Inputs) Replay(ReadOnlySpan<byte> source,
        CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> rounds,
        IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation, IReadOnlyList<byte[]> movement,
        IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, Func<string, byte[]> dependencies)
    {
        CampaignCombatActualRepeatedBreakdownCompletionCodec.VerifyDependencies(dependencies); var packet = Object(source, "Source");
        var frame = Derive(Encoding.ASCII.GetBytes(T(packet, "repeatedMovementCompletionSourceCanonicalUtf8")), context, selection, rounds, paid, continuation, movement, lifecycle, dependencies);
        Require(breakdown is not null && breakdown.Count <= 1 && breakdown.All(x => x is not null), 1);
        var inputs = breakdown.Select(x => { var copy = x.ToArray(); _ = Parse(copy, "Input"); return copy; }).ToList(); Require(Encode(A(inputs.Select(x => JsonNode.Parse(x)))).Length <= 2048, 1);
        var events = packet["breakdownEventCanonicalUtf8"]!.AsArray(); Require(events.Count == inputs.Count, 4); var state = Initial(frame);
        for (var i = 0; i < inputs.Count; i++)
        {
            var actual = Encoding.ASCII.GetBytes(events[i]!.GetValue<string>()); _ = Parse(actual, "Event"); var (after, expected, _) = Transition(frame, state, Object(inputs[i], "Input"));
            Require(expected is not null && actual.AsSpan().SequenceEqual(expected), 6); state = after;
        }
        CampaignCombatActualRepeatedBreakdownCompletionCodec.VerifyDependencies(dependencies); return (packet, frame, state, inputs);
    }
    private static CombatActualRepeatedBreakdownCompletionResult Result(JsonObject packet, Frame frame, JsonObject state,
        IReadOnlyList<byte[]> inputs, byte[]? accepted = null, string? receipt = null, bool duplicate = false)
    {
        var source = Canonical(packet, "Source"); var digest = Digest("source", source);
        var proof = O(("contractVersion", 1), ("sourceId", "arbcomp." + digest), ("sourceHash", "sha256:" + digest), ("base", frame.Base), ("control", state));
        return new(source, Canonical(state, "Control"), Canonical(proof, "Proof"), inputs, accepted, receipt, duplicate);
    }
    internal static Frame Derive(byte[] source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> selection,
        IReadOnlyList<byte[]> rounds, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> continuation,
        IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> lifecycle, Func<string, byte[]> dependencies)
    {
        CombatActualRepeatedMovementCompletionResult ancestor;
        try { ancestor = CampaignCombatActualRepeatedMovementCompletion.ReplayTrustedSource(source, context, selection, rounds, paid, continuation, movement, lifecycle, dependencies); }
        catch (JsonException error)
        {
            var code = error.Message.StartsWith("CMB-RMC-", StringComparison.Ordinal) && int.TryParse(error.Message.AsSpan(8, 3), out var n) ? n : 4;
            throw new JsonException($"CMB-ABC-{code:000}", error);
        }
        var prior = JsonNode.Parse(ancestor.ControlBytes)!.AsObject(); var cycle = prior["activeCycle"]!.AsObject();
        Require(N(cycle, "gameTurn") == 1 && N(cycle, "operationStage") == 1 && T(cycle, "playerPhaseSlot") == "first-acting-side" && N(cycle, "ordinal") == 2, 3);
        var boundary = Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.breakdown-determination"); var terminal = Cna1979LandSequence.GetNext(boundary);
        Require(B(prior, "closed") && T(prior["flow"]!, "kind") == "idle" && prior["interruptContext"] is null && T(prior, "positionId") == boundary.PositionId, 3);
        var end = prior["movementEnd"]; Require(end is not null && N(end, "ordinal") == 2 && JsonNode.DeepEquals(end["scope"], prior["priorMovementEnd"]!["scope"]) && T(end, "completionReceiptId") == T(prior["receipts"]!.AsArray()[^1]!, "receiptId"), 4);
        Require(ancestor.LifecycleInputs.Count == (prior["tracks"]!.AsArray().Count == 0 ? 1 : 3), 4);
        var content = JsonNode.Parse(CampaignCombatActualRepeatedBreakdownCompletionCodec.Consume("docs/specs/fixtures/combat-content-v7.canonical.json", dependencies))!;
        Require(prior["members"]!.AsArray().All(m => content["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(m!["unit"]!, "elementId"))!["breakdownVehicleCohort"] is null), 3);
        var basis = O(("contractVersion", 1), ("dependencyManifestHash", Manifest()), ("repeatedMovementCompletionProof", JsonNode.Parse(ancestor.ProofBytes))); _ = Canonical(basis, "Base"); Require(N(prior, "stateVersion") < long.MaxValue, 7);
        return new(basis, "sha256:" + Digest("base", Canonical(basis, "Base")), prior, cycle.DeepClone().AsObject(), boundary, terminal);
    }
    internal static JsonObject Initial(Frame frame)
    {
        var state = frame.Prior.DeepClone().AsObject(); state["baseHash"] = frame.BaseHash; state["repeatedMovementCompletionSourceHash"] = frame.Base["repeatedMovementCompletionProof"]!["sourceHash"]!.DeepClone(); state["priorLifecycleReceipts"] = state["receipts"]!.DeepClone(); state["receipts"] = new JsonArray(); state["breakdownCompletionReceiptId"] = null; state["closed"] = false; return state;
    }
    internal static JsonObject Command(Frame frame, JsonObject state)
        => O(("command", O(("contractVersion", 1), ("kind", Kind), ("operationStage", 1), ("actionId", ActionId), ("baseHash", frame.BaseHash), ("cycleId", CycleId(frame.Cycle)), ("expectedPositionId", frame.Boundary.PositionId), ("expectedPriorVersion", state["stateVersion"]), ("capabilityId", Capability(frame, state)))), ("actor", "system"));
    private static string Capability(Frame frame, JsonObject state)
        => "sha256:" + Digest("capability", Encode(O(("baseHash", frame.BaseHash), ("cycleId", CycleId(frame.Cycle)), ("kind", Kind), ("stateVersion", state["stateVersion"]), ("positionId", state["positionId"]), ("movementCompletionReceiptId", state["movementEnd"]!["completionReceiptId"]))));
    internal static (JsonObject State, byte[]? Event, string Receipt) Transition(Frame frame, JsonObject prior, JsonObject input)
    {
        _ = Canonical(prior, "Control"); _ = Canonical(input, "Input"); Require(false, 7); return (prior, null, "unsupported");
    }
    private static string CycleId(JsonNode c) => CampaignCombatReserveCompletionCodec.CycleId(new((int)N(c, "contractVersion"), T(c, "campaignId"), T(c, "rulesetHash"), T(c, "setupId"), T(c, "setupHash"), T(c, "contentPackId"), T(c, "contentHash"), T(c, "scenarioId"), (int)N(c, "gameTurn"), (int)N(c, "operationStage"), T(c, "playerPhaseSlot"), T(c, "actingSide") == "axis" ? LandSide.Axis : LandSide.Commonwealth, (int)N(c, "ordinal"), N(c, "openedAuthorityVersion"), T(c, "openingPrefix"), T(c, "admittedPolicyBundleDigest")));
    private static string Digest(string kind, byte[] bytes) => Domain("sandtable.combat.actual-repeated-breakdown-completion." + kind + ".v1", bytes);
}
