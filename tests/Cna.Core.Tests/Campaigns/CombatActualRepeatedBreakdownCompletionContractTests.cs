using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedBreakdownCompletionCodec;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;
using static Cna.Core.Tests.Campaigns.CombatActualRepeatedBreakdownCompletionTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRepeatedBreakdownCompletionContractTests(ITestOutputHelper output)
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
    public void SystemActionContextCapabilityAndRetryStayBound(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var initial = EmptySource(trace); var final = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "breakdownLedger");
        foreach (var actor in new[] { "axis", "commonwealth" }) { var bad = Packet(ledger[0]); bad["actor"] = actor; Reject(4, () => Apply(initial, a, [], Encode(bad))); Reject(4, () => Apply(final, a, ledger, Encode(bad))); }
        foreach (var (field, value, code) in new (string, JsonNode, int)[] { ("operationStage", JsonValue.Create(2)!, 3), ("kind", JsonValue.Create("finish-cycle")!, 3), ("actionId", JsonValue.Create("sha256:" + new string('0', 64))!, 5), ("baseHash", JsonValue.Create("sha256:" + new string('0', 64))!, 4), ("cycleId", JsonValue.Create("sha256:" + new string('0', 64))!, 4), ("expectedPositionId", JsonValue.Create("land.position.breakdown-stop")!, 4), ("expectedPriorVersion", JsonValue.Create(0)!, 6), ("capabilityId", JsonValue.Create("sha256:" + new string('0', 64))!, 5) })
        {
            var bad = Packet(ledger[0]); bad["command"]![field] = value.DeepClone(); Reject(code, () => Apply(initial, a, [], Encode(bad))); Reject(field == "capabilityId" ? 6 : code, () => Apply(final, a, ledger, Encode(bad)));
        }
        foreach (var invalid in new JsonNode[] { JsonValue.Create(true)!, JsonValue.Create(-1)! }) { var bad = Packet(ledger[0]); bad["command"]!["expectedPriorVersion"] = invalid.DeepClone(); Reject(invalid.GetValueKind() == JsonValueKind.True ? 1 : 2, () => Apply(final, a, ledger, Encode(bad))); }
        var frame = Frame(trace, a); var terminal = State(trace, 1); Reject(7, () => Apply(final, a, ledger, Encode(CampaignCombatActualRepeatedBreakdownCompletion.Command(frame, terminal))));
        var overflow = State(trace, 0); overflow["stateVersion"] = long.MaxValue; Reject(7, () => CampaignCombatActualRepeatedBreakdownCompletion.Transition(frame, overflow, CampaignCombatActualRepeatedBreakdownCompletion.Command(frame, overflow)));
        var fork = State(trace, 0); fork["activeCycle"]!["ordinal"] = 3; Reject(4, () => CampaignCombatActualRepeatedBreakdownCompletion.Transition(frame, fork, CampaignCombatActualRepeatedBreakdownCompletion.Command(frame, fork)));
    }
    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(14)]
    [InlineData(19)]
    public void SevenLedgersAndResignedEffectsCannotForgeOwnedClaims(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "breakdownLedger"); var original = Replay(source, a, ledger);
        foreach (var (field, value) in new[] { ("kind", "invented"), ("movementCompletionReceiptId", "forged"), ("successorPositionId", "land.position.operation-1.first-player.movement-and-combat.breakdown-determination") })
        {
            var packet = Packet(source); var ev = Packet(Encoding.ASCII.GetBytes(packet["breakdownEventCanonicalUtf8"]![0]!.GetValue<string>())); ev["effect"]![field] = value; Resign(ev); packet["breakdownEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(Canonical(ev, "Event")); Reject(6, () => Replay(Encode(packet), a, ledger));
        }
        foreach (var field in new[] { "sourceId", "locator" }) { var packet = Packet(source); var ev = Packet(Encoding.ASCII.GetBytes(packet["breakdownEventCanonicalUtf8"]![0]!.GetValue<string>())); ev["effect"]!["sources"]![0]![field] = "forged"; Resign(ev); packet["breakdownEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(Canonical(ev, "Event")); Reject(6, () => Replay(Encode(packet), a, ledger)); }
        foreach (var field in new[] { "movementEnd", "priorMovementEnd" }) { var proof = Packet(original.ProofBytes); proof["control"]![field]!["completionReceiptId"] = "forged"; Reject(6, () => ReadProof(Encode(proof), source, a, ledger)); }
        var forged = Packet(original.ProofBytes); forged["control"]!["world"]!["elements"]![0]!["currentLocationId"] = "forged"; Reject(6, () => ReadProof(Encode(forged), source, a, ledger));
        forged = Packet(original.ProofBytes); forged["control"]!["randomState"]!["nextByteCursor"] = 11; Reject(6, () => ReadProof(Encode(forged), source, a, ledger));
        forged = Packet(original.ProofBytes); forged["control"]!["nextCycleProgress"] = new JsonArray(); Reject(6, () => ReadProof(Encode(forged), source, a, ledger));
        forged = Packet(original.ProofBytes); forged["base"]!["repeatedMovementCompletionProof"]!["control"]!["closed"] = false; Reject(6, () => ReadProof(Encode(forged), source, a, ledger));
        Reject(4, () => Replay(source, a, [])); Reject(1, () => Replay(source, a, [ledger[0], ledger[0]])); Reject(1, () => Replay(source, a, null!));
        var badInput = Packet(ledger[0]); badInput["actor"] = "axis"; Reject(4, () => Replay(source, a, [Encode(badInput)]));
        var all = new[] { a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle };
        for (var which = 0; which < 6; which++) { var altered = all.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(); altered[which] = null!; Reject(1, () => CampaignCombatActualRepeatedBreakdownCompletion.ReadControl([], source, Context(), altered[0], altered[1], altered[2], altered[3], altered[4], altered[5], null!, Dependency)); }
        var broken = Packet(source); var ancestor = Packet(Parent(source)); ancestor["contractVersion"] = -1; broken["repeatedMovementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(ancestor)); Reject(2, () => ReadProof([], Encode(broken), a, null!));
        broken.Remove("contractVersion"); Reject(1, () => ReadProof([], Encode(broken), a, null!));
        var context = Context(); var artifact = Cna1979CombatContentCatalog.Artifact; var cfg = JsonNode.Parse(CombatDecisionConfigurationCodec.Serialize(context.Configuration))!.AsObject(); cfg["configId"] = "foreign-context"; var foreign = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, context.Setup, artifact, artifact.Definition.Scenarios.Single(), CombatDecisionConfigurationCodec.Deserialize(Encode(cfg), Cna1979CombatRuleset.Manifest));
        Reject(4, () => CampaignCombatActualRepeatedBreakdownCompletion.ReadProof([], source, foreign, a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, null!, Dependency));
    }
    [Fact]
    public void All52AuthenticNonterminalMovementCompletionCutsRejectBeforeSeventhSyntax()
    {
        var cuts = 0;
        for (var index = 0; index < 20; index++)
        {
            var trace = Trace(index); var a = Arguments(trace); var rmc = CombatActualRepeatedMovementCompletionTests.Trace(index);
            for (var cut = 0; cut < a.Lifecycle.Length; cut++) { var packet = Packet(EmptySource(trace)); packet["repeatedMovementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CombatActualRepeatedMovementCompletionTests.Cut(rmc, cut)); var altered = a with { Lifecycle = a.Lifecycle.Take(cut).ToArray() }; Reject(3, () => Replay(Encode(packet), altered, null!)); cuts++; }
        }
        Assert.Equal(52, cuts);
    }
    [Fact]
    public void ClosedShapesCanonicalNumbersAndPreparseCapsReject()
    {
        var trace = Trace(4); var packet = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); var input = Packet(Ledger(trace, "breakdownLedger")[0]);
        foreach (var kind in new[] { "Source", "Control", "Proof", "Event", "Input" }) Reject(1, () => Parse(new byte[ByteLimit(kind) + 1], kind));
        foreach (var (kind, value) in new[] { ("Source", packet), ("Input", input), ("Control", State(trace, 1)), ("Proof", Packet(Bytes(trace.GetProperty("proofCanonicalUtf8")))) })
        {
            foreach (var field in value.Select(x => x.Key).ToArray()) { var bad = value.DeepClone().AsObject(); bad.Remove(field); Reject(1, () => Parse(Encode(bad), kind)); }
            var canonical = Canonical(value, kind); Reject(8, () => Parse([.. canonical, (byte)' '], kind)); var unknown = value.DeepClone().AsObject(); unknown["unknown"] = null; Reject(1, () => Parse(Encode(unknown), kind));
        }
        foreach (var raw in new[] { "{\"contractVersion\":1,\"contractVersion\":1}", new string('[', 44) + "0" + new string(']', 44), "[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]", "{" + string.Join(',', Enumerable.Range(0, 129).Select(i => "\"k" + i + "\":0")) + "}" }) Reject(1, () => Parse(Encoding.ASCII.GetBytes(raw), "Source"));
        foreach (var (value, code) in new (JsonNode, int)[] { (JsonValue.Create(true)!, 1), (JsonValue.Create(-1)!, 2), (JsonValue.Create(1.5)!, 1), (JsonValue.Create(2)!, 3) }) { var bad = input.DeepClone(); bad["command"]!["contractVersion"] = value.DeepClone(); Reject(code, () => Parse(Encode(bad), "Input")); }
        var largeNumber = input.DeepClone(); largeNumber["command"]!["expectedPriorVersion"] = JsonNode.Parse("9223372036854775808"); Reject(2, () => Parse(Encode(largeNumber), "Input"));
        foreach (var field in new[] { "clock", "deadline", "rng", "fallback", "checks" }) { var timed = input.DeepClone().AsObject(); timed[field] = null; Reject(1, () => Canonical(timed, "Input")); }
        var duplicate = packet.DeepClone(); duplicate["breakdownEventCanonicalUtf8"]!.AsArray().Add(duplicate["breakdownEventCanonicalUtf8"]![0]!.DeepClone()); Reject(1, () => Canonical(duplicate, "Source"));
        var tooLarge = packet.DeepClone(); tooLarge["repeatedMovementCompletionSourceCanonicalUtf8"] = new string('x', 67_108_865); Reject(1, () => Canonical(tooLarge, "Source"));
    }
    [Fact]
    public void Exactly72PhysicalDeliveriesAndAll360ColdWarmFaultsReject()
    {
        var trace = Trace(0); var a = Arguments(trace); var source = EmptySource(trace); var paths = new HashSet<string>(StringComparer.Ordinal); byte[] Observe(string p) { paths.Add(p); return Dependency(p); }
        _ = CampaignCombatActualRepeatedBreakdownCompletion.ReplayTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, [], Observe); Assert.Equal(72, paths.Count);
        foreach (var fixture in new[] { "combat-actual-paid-assault-v1.json", "combat-actual-post-assault-continuation-v1.json", "combat-actual-repeated-movement-v1.json", "combat-actual-repeated-movement-completion-v1.json", "combat-actual-repeated-breakdown-completion-v1.json" }) Assert.DoesNotContain("docs/specs/fixtures/" + fixture, paths);
        var checks = 0;
        foreach (var path in paths)
        {
            byte[] Changed(string p) => p == path ? [0] : Dependency(p); byte[] Missing(string p) => p == path ? throw new FileNotFoundException(p) : Dependency(p); byte[] Unreadable(string p) => p == path ? throw new UnauthorizedAccessException(p) : Dependency(p); byte[] Null(string p) => p == path ? null! : Dependency(p);
            foreach (var bytes in new Func<string, byte[]>[] { Changed, Missing, Unreadable, Null }) { Reject(9, () => CampaignCombatActualRepeatedBreakdownCompletion.ReadSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, [], bytes)); checks++; }
            var hits = 0; byte[] Drift(string p) => p == path && ++hits > 1 ? [0] : Dependency(p); Reject(9, () => CampaignCombatActualRepeatedBreakdownCompletion.ReadSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, [], Drift)); checks++;
        }
        Assert.Equal(360, checks); var observed = paths.ToArray(); paths.Clear(); _ = CampaignCombatActualRepeatedBreakdownCompletion.ReplayTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, [], Observe); Assert.Equal(observed.Order(StringComparer.Ordinal), paths.Order(StringComparer.Ordinal));
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-repeated-breakdown-completion-v1.schema.json")); var manifest = new JsonObject(); foreach (var item in schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)) manifest[item.Name] = item.Value.GetString(); Assert.Equal(70, manifest.Count); Assert.Equal("sha256:" + Domain("sandtable.combat.actual-repeated-breakdown-completion.dependency-manifest.v1", Encode(manifest)), Manifest());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void SupportedFixtureNativeAllocationAndLatencyAreMeasuredWithoutNearLimitClaim(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "breakdownLedger"); var context = Context();
        _ = CampaignCombatActualRepeatedBreakdownCompletion.ReplayTrustedSource(source, context, a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, ledger, Dependency);
        var before = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp(); var result = CampaignCombatActualRepeatedBreakdownCompletion.ReplayTrustedSource(source, context, a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, ledger, Dependency); var elapsed = Stopwatch.GetElapsedTime(start); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(Bytes(trace.GetProperty("proofCanonicalUtf8")), result.ProofBytes); Assert.InRange(source.Length, 1, 134_217_728); Assert.InRange(result.ProofBytes.Length, 1, 4_194_304); Assert.True(allocated > 0);
        output.WriteLine($"NBC native fixture index={index} Source={source.Length} Proof={result.ProofBytes.Length} allocatedCurrentThread={allocated} elapsedMilliseconds={elapsed.TotalMilliseconds:F3}; warm supported-fixture observation, not near-limit bound");
    }
    [Fact]
    public void GenuineInheritedClockVariantRebuildsSevenLedgersThroughBreakdown()
    {
        var trace = Trace(0); var a = Arguments(trace); var movementPacket = CampaignCombatActualRepeatedMovementCodec.Object(CombatActualRepeatedMovementCompletionTests.Parent(CombatActualRepeatedMovementCompletionTests.EmptySource(CombatActualRepeatedMovementCompletionTests.Trace(0))), "Source");
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
            var input = CampaignCombatActualRepeatedMovementCompletion.Command(frame, state, kind); var result = CampaignCombatActualRepeatedMovementCompletion.ApplyTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, continuation, movement, lifecycle, CampaignCombatActualRepeatedMovementCompletionCodec.Canonical(input, "Input"), Dependency); source = result.SourceBytes; lifecycle = result.LifecycleInputs.Select(x => x.ToArray()).ToArray(); state = CampaignCombatActualRepeatedMovementCompletionCodec.Object(result.ControlBytes, "Control");
        }
        Assert.True(B(state, "closed")); Assert.Equal(41, N(state, "stateVersion"));
        var predecessor = source; var packet = new JsonObject { ["contractVersion"] = 1, ["repeatedMovementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(source), ["breakdownEventCanonicalUtf8"] = new JsonArray() };
        var actualFrame = CampaignCombatActualRepeatedBreakdownCompletion.Derive(predecessor, Context(), a.Selection, a.Rounds, a.Paid, continuation, movement, lifecycle, Dependency); var actualState = CampaignCombatActualRepeatedBreakdownCompletion.Initial(actualFrame); var command = CampaignCombatActualRepeatedBreakdownCompletion.Command(actualFrame, actualState);
        var completed = CampaignCombatActualRepeatedBreakdownCompletion.ApplyTrustedSource(Encode(packet), Context(), a.Selection, a.Rounds, a.Paid, continuation, movement, lifecycle, [], Canonical(command, "Input"), Dependency); var resultState = Object(completed.ControlBytes, "Control");
        Assert.Equal(42, N(resultState, "stateVersion")); Assert.True(B(resultState, "closed")); Assert.Equal(2, N(resultState["activeCycle"]!, "ordinal")); Assert.Equal(9, N(resultState["members"]![0]!["spentCp"]!, "numerator")); Assert.Equal(10, N(resultState["randomState"]!, "nextByteCursor"));
        Assert.True(JsonNode.DeepEquals(state["movementEnd"], resultState["movementEnd"])); Assert.True(JsonNode.DeepEquals(state["priorMovementEnd"], resultState["priorMovementEnd"])); Assert.Equal(completed.ReceiptId, T(resultState, "breakdownCompletionReceiptId"));
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", T(resultState, "positionId"));
    }
    private static CampaignCombatActualRepeatedBreakdownCompletion.Frame Frame(JsonElement trace, Inputs a) => CampaignCombatActualRepeatedBreakdownCompletion.Derive(Parent(EmptySource(trace)), Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, Dependency);
    private static JsonObject State(JsonElement trace, int cut) => Packet(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]));
    private static CombatActualRepeatedBreakdownCompletionResult ReadProof(byte[] claim, byte[] source, Inputs a, IReadOnlyList<byte[]> ledger) => CampaignCombatActualRepeatedBreakdownCompletion.ReadProof(claim, source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, ledger, Dependency);
    private static byte[] Encode(JsonNode value) => CampaignCombatActualRepeatedBreakdownCompletionCodec.Encode(value);
    private static void Resign(JsonObject ev) { ev.Remove("receiptId"); ev["receiptId"] = "arbc." + Domain("sandtable.combat.actual-repeated-breakdown-completion.receipt.v1", Encode(ev)); }
}
