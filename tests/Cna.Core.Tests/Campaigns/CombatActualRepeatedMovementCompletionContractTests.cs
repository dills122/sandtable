using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedMovementCompletionCodec;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;
using static Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRepeatedMovementCompletionContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(19)]
    public void ActorOrderContextCapabilityAndRetryRejectBeforePublication(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var all = Ledger(trace, "lifecycleLedger"); var frame = Frame(trace, a); var initial = State(trace, 0); var empty = EmptySource(trace); var moves = trace.GetProperty("moves").GetInt32();
        foreach (var kind in moves == 0 ? new[] { "stop-element-movement", "resolve-breakdown-stop" } : ["resolve-breakdown-stop", "complete-movement-segment"])
        {
            var input = CampaignCombatActualRepeatedMovementCompletion.Command(frame, initial, kind); Reject(6, () => Apply(empty, a, [], Encode(input)));
        }
        if (moves > 0)
        {
            var pending = State(trace, 1);
            foreach (var kind in new[] { "stop-element-movement", "complete-movement-segment" }) Reject(6, () => Apply(Cut(trace, 1), a, all.Take(1).ToArray(), Encode(CampaignCombatActualRepeatedMovementCompletion.Command(frame, pending, kind))));
        }
        for (var i = 0; i < all.Length; i++)
        {
            var source = Cut(trace, i); var ledger = all.Take(i).ToArray(); var bad = Packet(all[i]); bad["actor"] = T(bad, "actor") == "system" ? trace.GetProperty("owner").GetString() : "system"; Reject(4, () => Apply(source, a, ledger, Encode(bad)));
            foreach (var (field, value, code) in new (string, JsonNode, int)[] { ("baseHash", JsonValue.Create("sha256:" + new string('0', 64))!, 4), ("cycleId", JsonValue.Create("sha256:" + new string('0', 64))!, 4), ("expectedPositionId", JsonValue.Create("wrong")!, 4), ("expectedPriorVersion", JsonValue.Create(0)!, 6), ("capabilityId", JsonValue.Create("sha256:" + new string('0', 64))!, 5), ("kind", JsonValue.Create("repeat")!, 3) })
            {
                bad = Packet(all[i]); bad["command"]![field] = value.DeepClone(); Reject(code, () => Apply(source, a, ledger, Encode(bad)));
            }
            bad = Packet(all[i]); bad["actor"] = T(bad, "actor") == "system" ? trace.GetProperty("owner").GetString() : "system"; Reject(4, () => Apply(Bytes(trace.GetProperty("sourceCanonicalUtf8")), a, all, Encode(bad)));
        }
        var final = State(trace, all.Length); Reject(7, () => Apply(Bytes(trace.GetProperty("sourceCanonicalUtf8")), a, all, Encode(CampaignCombatActualRepeatedMovementCompletion.Command(frame, final, "complete-movement-segment"))));
        Assert.Equal(empty, EmptySource(trace));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(14)]
    [InlineData(19)]
    public void SixIndependentLedgersAndCoherentEventsCannotForgeClaims(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "lifecycleLedger"); var original = Replay(source, a, ledger);
        for (var i = 0; i < ledger.Length; i++)
        {
            var packet = Packet(source); var ev = Packet(Encoding.ASCII.GetBytes(packet["lifecycleEventCanonicalUtf8"]![i]!.GetValue<string>())); ev["stateVersion"] = N(ev, "stateVersion") + 1; Resign(ev); packet["lifecycleEventCanonicalUtf8"]![i] = Encoding.ASCII.GetString(Canonical(ev, "Event")); Reject(6, () => Replay(Encode(packet), a, ledger));
        }
        foreach (var (field, ordinal) in new[] { ("movementEnd", 1), ("priorMovementEnd", 2) })
        {
            var proof = Packet(original.ProofBytes); proof["control"]![field]!["ordinal"] = ordinal; Reject(6, () => ReadProof(Encode(proof), source, a, ledger));
        }
        var forged = Packet(original.ProofBytes); forged["control"]!["nextCycleProgress"] = new JsonArray(); Reject(6, () => ReadProof(Encode(forged), source, a, ledger));
        Reject(4, () => Replay(source, a, ledger.Take(2).ToArray())); Reject(1, () => Replay(source, a, [.. ledger, ledger[0]])); Reject(6, () => Replay(source, a, ledger.Reverse().ToArray()));
        var broken = Packet(source); broken["movementSourceCanonicalUtf8"] = "{}"; Reject(1, () => ReadProof([], Encode(broken), a, null!));
        var ancestors = new[] { a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement };
        for (var which = 0; which < 5; which++)
        {
            var all = ancestors.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(); all[which] = null!; Reject(1, () => CampaignCombatActualRepeatedMovementCompletion.ReadControl([], source, Context(), all[0], all[1], all[2], all[3], all[4], null!, Dependency));
        }
        Reject(1, () => ReadProof([], source, a, null!));
    }

    [Fact]
    public void PendingContextActualTrackAndHistoricalExclusionStayBound()
    {
        var trace = Trace(4); var a = Arguments(trace); var frame = Frame(trace, a); var pending = State(trace, 1); var bad = pending.DeepClone().AsObject(); bad["interruptContext"]!["cycle"]!["ordinal"] = 3;
        Reject(6, () => CampaignCombatActualRepeatedMovementCompletion.Transition(frame, bad, CampaignCombatActualRepeatedMovementCompletion.Command(frame, bad, "resolve-breakdown-stop")));
        foreach (var field in new[] { "lastMoveReceiptId", "recordedStateVersion", "track" })
        {
            bad = pending.DeepClone().AsObject(); var stop = bad["flow"]!["stop"]!;
            if (field == "recordedStateVersion") stop[field] = 0; else if (field == "lastMoveReceiptId") stop[field] = "fake"; else stop["track"]!["route"]!.AsArray()[^1] = "axis-front";
            Reject(4, () => CampaignCombatActualRepeatedMovementCompletion.Transition(frame, bad, CampaignCombatActualRepeatedMovementCompletion.Command(frame, bad, "resolve-breakdown-stop")));
        }
        var packet = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); var ev = Packet(Encoding.ASCII.GetBytes(packet["lifecycleEventCanonicalUtf8"]![0]!.GetValue<string>())); ev["effect"]!["interruptContext"]!["cycle"]!["ordinal"] = 3; Resign(ev); packet["lifecycleEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(Canonical(ev, "Event")); Reject(6, () => Replay(Encode(packet), a, Ledger(trace, "lifecycleLedger")));
        var zero = Trace(0); var zeroFrame = Frame(zero, Arguments(zero)); var initial = State(zero, 0); initial["priorMovementEnd"]!["excludedBefore"] = new JsonArray(initial["members"]![0]!["unit"]!.DeepClone()); var proof = CampaignCombatActualRepeatedMovementCompletion.ExpireMovement(zeroFrame, initial, "isolated-probe"); Assert.Single(proof["excludedBefore"]!.AsArray()); Assert.True(JsonNode.DeepEquals(initial["members"], zeroFrame.Prior["members"]));
        initial["stateVersion"] = long.MaxValue; Reject(7, () => CampaignCombatActualRepeatedMovementCompletion.Transition(zeroFrame, initial, CampaignCombatActualRepeatedMovementCompletion.Command(zeroFrame, initial, "complete-movement-segment")));
    }

    [Fact]
    public void ClosedShapesVersionsCanonicalAndPreparseCapsReject()
    {
        var trace = Trace(4); var packet = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); var input = Packet(Ledger(trace, "lifecycleLedger")[0]);
        foreach (var kind in new[] { "Source", "Control", "Proof", "Event", "Input" }) Reject(1, () => Parse(new byte[ByteLimit(kind) + 1], kind));
        foreach (var version in new JsonNode[] { JsonValue.Create(true)!, JsonValue.Create(2)! }) { var bad = input.DeepClone(); bad["command"]!["contractVersion"] = version.DeepClone(); Reject(version.GetValueKind() == JsonValueKind.True ? 1 : 3, () => Parse(Encode(bad), "Input")); }
        foreach (var (value, code) in new (JsonNode, int)[] { (JsonValue.Create(true)!, 1), (JsonValue.Create(-1)!, 2), (JsonValue.Create(1.5)!, 1) }) { var bad = input.DeepClone(); bad["command"]!["expectedPriorVersion"] = value.DeepClone(); Reject(code, () => Parse(Encode(bad), "Input")); }
        foreach (var (kind, value) in new[] { ("Source", packet), ("Input", input), ("Control", State(trace, 1)), ("Proof", Packet(Bytes(trace.GetProperty("proofCanonicalUtf8")))) })
        {
            foreach (var field in value.Select(x => x.Key).ToArray()) { var bad = value.DeepClone().AsObject(); bad.Remove(field); Reject(1, () => Parse(Encode(bad), kind)); }
            var canonical = Canonical(value, kind); Reject(8, () => Parse([.. canonical, (byte)' '], kind)); var unknown = value.DeepClone().AsObject(); unknown["unknown"] = null; Reject(1, () => Parse(Encode(unknown), kind));
        }
        foreach (var raw in new[] { "{\"contractVersion\":1,\"contractVersion\":1}", new string('[', 42) + "0" + new string(']', 42), "[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]", "{" + string.Join(',', Enumerable.Range(0, 129).Select(i => "\"k" + i + "\":0")) + "}" }) Reject(1, () => Parse(Encoding.ASCII.GetBytes(raw), "Source"));
        var flow = State(trace, 0)["flow"]!.DeepClone(); flow["kind"] = "unknown"; Reject(3, () => Canonical(flow, "Flow")); var timed = input.DeepClone().AsObject(); timed["admittedAt"] = 1000; Reject(1, () => Canonical(timed, "Input"));
        var tooLarge = packet.DeepClone(); tooLarge["movementSourceCanonicalUtf8"] = new string('x', 33_554_433); Reject(1, () => Canonical(tooLarge, "Source"));
    }

    [Fact]
    public void Exactly70PhysicalDeliveriesAndAll350ColdWarmFaultsReject()
    {
        var trace = Trace(0); var a = Arguments(trace); var source = EmptySource(trace); var paths = new HashSet<string>(StringComparer.Ordinal); byte[] Observe(string p) { paths.Add(p); return Dependency(p); }
        _ = CampaignCombatActualRepeatedMovementCompletion.ReplayTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, [], Observe); Assert.Equal(70, paths.Count);
        foreach (var fixture in new[] { "combat-actual-paid-assault-v1.json", "combat-actual-post-assault-continuation-v1.json", "combat-actual-repeated-movement-v1.json", "combat-actual-repeated-movement-completion-v1.json" }) Assert.DoesNotContain("docs/specs/fixtures/" + fixture, paths);
        foreach (var path in paths)
        {
            byte[] Changed(string p) => p == path ? [0] : Dependency(p); byte[] Missing(string p) => p == path ? throw new FileNotFoundException(p) : Dependency(p); byte[] Unreadable(string p) => p == path ? throw new UnauthorizedAccessException(p) : Dependency(p); byte[] Null(string p) => p == path ? null! : Dependency(p);
            foreach (var bytes in new Func<string, byte[]>[] { Changed, Missing, Unreadable, Null }) Reject(9, () => CampaignCombatActualRepeatedMovementCompletion.ReadSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, [], bytes));
            var hits = 0; byte[] Drift(string p) => p == path && ++hits > 1 ? [0] : Dependency(p); Reject(9, () => CampaignCombatActualRepeatedMovementCompletion.ReadSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, [], Drift));
        }
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-repeated-movement-completion-v1.schema.json")); var manifest = new JsonObject(); foreach (var item in schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)) manifest[item.Name] = item.Value.GetString(); Assert.Equal(68, manifest.Count); Assert.Equal("sha256:" + Domain("sandtable.combat.actual-repeated-movement-completion.dependency-manifest.v1", Encode(manifest)), Manifest(CampaignCombatActualRepeatedMovementCompletionCodec.VerifyDependencies(Dependency)));
    }

    [Fact]
    public void GenuineInheritedClockVariantRebuildsNativeAncestryThroughCompletion()
    {
        var trace = Trace(0); var a = Arguments(trace); var movementPacket = CampaignCombatActualRepeatedMovementCodec.Object(Parent(EmptySource(trace)), "Source");
        var continuationSource = Encoding.ASCII.GetBytes(T(movementPacket, "continuationSourceCanonicalUtf8")); var continuationPacket = CampaignCombatActualPostAssaultContinuationCodec.Object(continuationSource, "Source"); var paidSource = Encoding.ASCII.GetBytes(T(continuationPacket, "paidSourceCanonicalUtf8"));
        continuationPacket["continuationEventCanonicalUtf8"] = new JsonArray(); var source = CampaignCombatActualPostAssaultContinuationCodec.Canonical(continuationPacket, "Source"); byte[][] continuation = [];
        var pacFrame = CampaignCombatActualPostAssaultContinuation.Derive(paidSource, Context(), a.Selection, a.Rounds, a.Paid, Dependency); var pacState = CampaignCombatActualPostAssaultContinuation.Initial(pacFrame);
        foreach (var kind in new[] { "release-open", "release-complete", "control-open", "repeat" })
        {
            var input = new JsonObject { ["command"] = CampaignCombatActualPostAssaultContinuation.Command(pacFrame, pacState, kind), ["actor"] = kind == "repeat" ? T(pacFrame.Cycle, "actingSide") : "system", ["admittedAt"] = kind == "control-open" ? 2000 : kind == "repeat" ? 2001 : null, ["clockAvailable"] = true };
            var result = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, continuation, CampaignCombatActualPostAssaultContinuationCodec.Canonical(input, "Input"), Dependency); source = result.SourceBytes; continuation = result.ContinuationInputs.Select(x => x.ToArray()).ToArray(); pacState = CampaignCombatActualPostAssaultContinuationCodec.Object(result.ControlBytes, "Control");
        }
        Assert.NotEqual(continuationSource, source); var ordinary = new JsonObject { ["contractVersion"] = 1, ["continuationSourceCanonicalUtf8"] = Encoding.ASCII.GetString(source), ["movementEventCanonicalUtf8"] = new JsonArray() };
        var moveFrame = CampaignCombatActualRepeatedMovement.Derive(source, Context(), a.Selection, a.Rounds, a.Paid, continuation, Dependency); var moveState = CampaignCombatActualRepeatedMovement.Initial(moveFrame); var moveInput = CampaignCombatActualRepeatedMovement.Command(moveFrame, moveState, T(moveFrame.Cycle, "actingSide") + "-rear");
        var moved = CampaignCombatActualRepeatedMovement.ApplyTrustedSource(CampaignCombatActualRepeatedMovementCodec.Encode(ordinary), Context(), a.Selection, a.Rounds, a.Paid, continuation, [], CampaignCombatActualRepeatedMovementCodec.Canonical(moveInput, "Input"), Dependency);
        var movement = moved.MovementInputs.Select(x => x.ToArray()).ToArray(); var own = new JsonObject { ["contractVersion"] = 1, ["movementSourceCanonicalUtf8"] = Encoding.ASCII.GetString(moved.SourceBytes), ["lifecycleEventCanonicalUtf8"] = new JsonArray() }; source = Encode(own); byte[][] lifecycle = [];
        var frame = CampaignCombatActualRepeatedMovementCompletion.Derive(moved.SourceBytes, Context(), a.Selection, a.Rounds, a.Paid, continuation, movement, Dependency); var state = CampaignCombatActualRepeatedMovementCompletion.Initial(frame);
        foreach (var kind in new[] { "stop-element-movement", "resolve-breakdown-stop", "complete-movement-segment" })
        {
            var input = CampaignCombatActualRepeatedMovementCompletion.Command(frame, state, kind); var result = CampaignCombatActualRepeatedMovementCompletion.ApplyTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, continuation, movement, lifecycle, Canonical(input, "Input"), Dependency); source = result.SourceBytes; lifecycle = result.LifecycleInputs.Select(x => x.ToArray()).ToArray(); state = Object(result.ControlBytes, "Control");
        }
        Assert.True(B(state, "closed")); Assert.Equal(41, N(state, "stateVersion")); Assert.Equal(2, N(state["movementEnd"]!, "ordinal")); Assert.Equal(1, N(state["priorMovementEnd"]!, "ordinal")); Assert.Equal(9, N(state["members"]![0]!["spentCp"]!, "numerator")); Assert.Equal(10, N(state["randomState"]!, "nextByteCursor")); Assert.Equal(3, lifecycle.Length);
    }

    private static CampaignCombatActualRepeatedMovementCompletion.Frame Frame(JsonElement trace, Inputs a) => CampaignCombatActualRepeatedMovementCompletion.Derive(Parent(EmptySource(trace)), Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, Dependency);
    private static JsonObject State(JsonElement trace, int cut) => Packet(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]));
    private static CombatActualRepeatedMovementCompletionResult ReadProof(byte[] claim, byte[] source, Inputs a, IReadOnlyList<byte[]> ledger) => CampaignCombatActualRepeatedMovementCompletion.ReadProof(claim, source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, ledger, Dependency);
    private static byte[] Encode(JsonNode value) => CampaignCombatActualRepeatedMovementCompletionCodec.Encode(value);
    private static void Resign(JsonObject ev) { ev.Remove("receiptId"); ev["receiptId"] = "armc." + Domain("sandtable.combat.actual-repeated-movement-completion.receipt.v1", Encode(ev)); }
}
