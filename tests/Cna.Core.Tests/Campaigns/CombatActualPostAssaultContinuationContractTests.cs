using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPostAssaultContinuationCodec;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;
using static Cna.Core.Tests.Campaigns.CombatActualPostAssaultContinuationTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualPostAssaultContinuationContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void NativeIndependentClockDeadlineFallbackAndTerminalTimers(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var frame = Frame(trace, args); var state = Object(Bytes(trace.GetProperty("controlCanonicalUtf8")[3]), "Control");
        Assert.Equal(1000, Num(state, "acceptedHighWater")); Assert.True(Num(frame.Paid, "acceptedHighWater") > 1000);
        var deadline = Num(state["timing"]!, "deadlineUnixMilliseconds"); var owner = Text(frame.Cycle, "actingSide");
        foreach (var now in new[] { deadline, deadline + 1 }) Reject(5, () => Step(frame, state, "repeat", owner, now));
        foreach (var now in new long?[] { null, 999 }) { var result = Step(frame, state, "repeat", owner, now); Assert.Equal("finished", Text(result.State, "status")); Assert.Equal("system", Text(JsonNode.Parse(result.Event!)!, "author")); }
        Assert.Equal("finished", Text(Step(frame, state, "repeat", owner, 1001, false).State, "status"));
        Assert.Equal("finished", Text(Step(frame, state, "expire", "system", deadline).State, "status"));
        Assert.Equal("finished", Text(Step(frame, state, "unavailable", "system", 1001).State, "status"));
        var early = Step(frame, state, "expire", "system", deadline - 1); Assert.Null(early.Event); Assert.Null(early.Receipt); Assert.Equal(Canonical(state, "Control"), Canonical(early.State, "Control"));
        Reject(4, () => Step(frame, state, "repeat", owner == "axis" ? "commonwealth" : "axis", null));
        foreach (var now in new long?[] { null, CampaignCombatActualPaidAssaultSupport.MaxUtc })
        {
            var prior = Object(Bytes(trace.GetProperty("controlCanonicalUtf8")[2]), "Control"); var opened = Step(frame, prior, "control-open", "system", now); Assert.True(opened.State["openingClockFailure"]!.GetValue<bool>()); Assert.Null(opened.State["timing"]);
            var closed = Step(frame, opened.State, "fallback-step", "system", null); Assert.Equal("finished", Text(closed.State, "status"));
        }
        Reject(5, () => Step(frame, state, "fallback-step", "system", 1001));
        var final = Object(Bytes(trace.GetProperty("controlCanonicalUtf8")[4]), "Control"); Assert.Null(Step(frame, final, "expire", "system", null).Event); Reject(7, () => Step(frame, final, "repeat", owner, 1002));
        var bad = Input(frame, final, "repeat", owner, 1001); bad["admittedAt"] = true; Reject(1, () => CampaignCombatActualPostAssaultContinuation.Transition(frame, final, bad));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void CoherentResignedSourcesClaimsAndIndependentLedgerForgeriesReject(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "continuationLedger"); var packet = Object(source, "Source"); var frame = Frame(trace, args);
        for (var n = 0; n < 4; n++)
        {
            var eventNode = Object(Encoding.ASCII.GetBytes(packet["continuationEventCanonicalUtf8"]![n]!.GetValue<string>()), "Event");
            foreach (var field in EventMutationFields)
            {
                var forgedEvent = eventNode.DeepClone().AsObject(); if (field == "author") forgedEvent[field] = Text(eventNode, field) == "axis" ? "commonwealth" : "axis"; else if (field == "stateVersion") forgedEvent[field] = Num(eventNode, field) + 1; else forgedEvent[field] = "sha256:" + new string('0', 64);
                Resign(forgedEvent); var forged = packet.DeepClone().AsObject(); forged["continuationEventCanonicalUtf8"]![n] = Encoding.ASCII.GetString(Canonical(forgedEvent, "Event")); Reject(6, () => Read(Encode(forged), args, ledger));
            }
            if (n >= 2)
            {
                var forgedEvent = eventNode.DeepClone().AsObject(); forgedEvent["effect"]!["assessment"]!["progress"]![0]!["receiptId"] = frame.Paid["commitmentId"]!.DeepClone(); Resign(forgedEvent);
                var forged = packet.DeepClone().AsObject(); forged["continuationEventCanonicalUtf8"]![n] = Encoding.ASCII.GetString(Canonical(forgedEvent, "Event")); Reject(6, () => Read(Encode(forged), args, ledger));
            }
        }
        Reject(4, () => Read(source, args, ledger.Take(3).ToArray())); Reject(1, () => Read(source, args, [.. ledger, ledger[0]])); Reject(6, () => Read(source, args, ledger.Reverse().ToArray()));
        for (var n = 0; n < 4; n++) { var bad = ledger.Select(x => x.ToArray()).ToArray(); var input = Object(bad[n], "Input"); input["actor"] = "commonwealth"; if (Text(input["command"]!, "kind") == "repeat" && Text(frame.Cycle, "actingSide") == "commonwealth") input["actor"] = "axis"; bad[n] = Encode(input); Reject(4, () => Read(source, args, bad)); }
        var claim = Object(Bytes(trace.GetProperty("proofCanonicalUtf8")), "Proof"); claim["control"]!["members"]![0]!["spentCp"]!["numerator"] = 0; Reject(6, () => CampaignCombatActualPostAssaultContinuation.ReadProof(Canonical(claim, "Proof"), source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency));
        var own = Object(Bytes(trace.GetProperty("controlCanonicalUtf8")[4]), "Control"); own["movementEnd"]!["completionReceiptId"] = "synthetic.movement-completed"; Reject(6, () => CampaignCombatActualPostAssaultContinuation.ReadControl(Canonical(own, "Control"), source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency));
        for (var cut = 0; cut < 8; cut++)
        {
            var forged = packet.DeepClone().AsObject(); var paidPacket = CampaignCombatActualPaidAssaultCodec.Object(Encoding.ASCII.GetBytes(Text(forged, "paidSourceCanonicalUtf8")), "PaidSource"); var events = paidPacket["paidEventCanonicalUtf8"]!.AsArray(); while (events.Count > cut) events.RemoveAt(events.Count - 1); forged["paidSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualPaidAssaultCodec.Canonical(paidPacket, "PaidSource")); Reject(4, () => Read(Encode(forged), args with { Paid = args.Paid.Take(cut).ToArray() }, ledger));
        }
    }
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public void PredecessorLedgerDiagnosticsAndPhysicalPrecedenceRetained(bool rounds, int code)
    {
        var trace = Trace(0); var args = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "continuationLedger"); var selected = rounds ? args.Rounds : args.Selection; var input = JsonNode.Parse(selected[0])!.AsObject();
        if (code == 1) input["admittedAt"] = true; else if (code == 2) input["admittedAt"] = -1; else input["command"]!["contractVersion"] = 2; selected[0] = Encode(input);
        byte[] Changed(string path) => path == "docs/specs/combat-actual-post-assault-continuation-v1.schema.json" ? [0] : Dependency(path);
        Reject(9, () => CampaignCombatActualPostAssaultContinuation.ReadProof([], source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Changed));
        Reject(code, () => CampaignCombatActualPostAssaultContinuation.ReadProof([], source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency));
        Reject(code, () => CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, ledger, ledger[^1], Dependency));
    }
    [Fact]
    public void ClosedNativeGrammarBoundsCanonicalAndRetryPrimitives()
    {
        var trace = Trace(0); var input = Object(Encode(trace.GetProperty("continuationLedger")[3]), "Input");
        foreach (var n in new[] { -1L, CampaignCombatActualPaidAssaultSupport.MaxUtc + 1 }) { var bad = input.DeepClone().AsObject(); bad["admittedAt"] = n; Reject(2, () => Parse(Encode(bad), "Input")); }
        var boolean = input.DeepClone().AsObject(); boolean["admittedAt"] = true; Reject(1, () => Parse(Encode(boolean), "Input"));
        var version = input.DeepClone().AsObject(); version["command"]!["contractVersion"] = 2; Reject(3, () => Parse(Encode(version), "Input")); version["command"]!["contractVersion"] = -1; Reject(2, () => Parse(Encode(version), "Input"));
        var bytes = Canonical(input, "Input"); Reject(8, () => Parse([.. bytes, (byte)' '], "Input")); Reject(1, () => Parse([0xef, 0xbb, 0xbf, .. bytes], "Input"));
        foreach (var kind in ByteKinds) Reject(1, () => Parse(new byte[ByteLimit(kind) + 1], kind));
        Reject(1, () => Parse(Encoding.ASCII.GetBytes("{\"contractVersion\":1,\"contractVersion\":1}"), "Source")); Reject(1, () => Parse(Encoding.ASCII.GetBytes(new string('[', 38) + "0" + new string(']', 38)), "Source")); Reject(1, () => Parse(Encoding.ASCII.GetBytes("[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]"), "Source"));
        var eventNode = Object(Encoding.ASCII.GetBytes(Object(Bytes(trace.GetProperty("sourceCanonicalUtf8")), "Source")["continuationEventCanonicalUtf8"]![3]!.GetValue<string>()), "Event"); eventNode["effect"]!["kind"] = "unknown-arm"; Reject(3, () => Parse(Encode(eventNode), "Event"));
    }
    [Fact]
    public void Exactly66PhysicalPathsAndAllColdWarmDriftFaultsRemainVisible()
    {
        var trace = Trace(0); var args = Arguments(trace); var source = EmptySource(trace); var paths = new HashSet<string>(StringComparer.Ordinal);
        byte[] Observe(string path) { paths.Add(path); return Dependency(path); }
        _ = CampaignCombatActualPostAssaultContinuation.ReplayTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, [], Observe); Assert.Equal(66, paths.Count); Assert.DoesNotContain("docs/specs/fixtures/combat-actual-post-assault-continuation-v1.json", paths);
        foreach (var path in paths)
        {
            byte[] Changed(string p) => p == path ? [0] : Dependency(p); Reject(9, () => CampaignCombatActualPostAssaultContinuation.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, [], Changed));
            byte[] Missing(string p) => p == path ? throw new FileNotFoundException(p) : Dependency(p); Reject(9, () => CampaignCombatActualPostAssaultContinuation.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, [], Missing));
            byte[] Unreadable(string p) => p == path ? throw new UnauthorizedAccessException(p) : Dependency(p); Reject(9, () => CampaignCombatActualPostAssaultContinuation.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, [], Unreadable));
            byte[] Null(string p) => p == path ? null! : Dependency(p); Reject(9, () => CampaignCombatActualPostAssaultContinuation.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, [], Null));
            var hits = 0; byte[] Drift(string p) => p == path && ++hits > 1 ? [0] : Dependency(p); Reject(9, () => CampaignCombatActualPostAssaultContinuation.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, [], Drift));
        }
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-post-assault-continuation-v1.schema.json")); var manifest = new JsonObject(); foreach (var p in schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)) manifest[p.Name] = p.Value.GetString();
        Assert.Equal("sha256:" + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-post-assault-continuation.dependency-manifest.v1", Encode(manifest)), Manifest());
    }
    [Fact]
    public void IsolatedPolicyPreservesFutureWorkAndRejectsForgedContextAndOverflow()
    {
        var trace = Trace(0); var args = Arguments(trace); var frame = Frame(trace, args); var state = Object(Bytes(trace.GetProperty("controlCanonicalUtf8")[2]), "Control"); var a = CampaignCombatActualPostAssaultContinuation.Assessment(frame, state); Assert.Equal("owner-choice", CampaignCombatActualPostAssaultContinuation.Mode(a));
        var noProgress = a.DeepClone().AsObject(); noProgress["progress"] = new JsonArray(); Assert.Equal("no-material-progress", CampaignCombatActualPostAssaultContinuation.Mode(noProgress)); var noWitness = a.DeepClone().AsObject(); noWitness["witnesses"] = new JsonArray(); Assert.Equal("no-continuation", CampaignCombatActualPostAssaultContinuation.Mode(noWitness));
        var unsupported = a.DeepClone().AsObject(); unsupported["witnessOutcome"] = "unsupported"; Reject(3, () => CampaignCombatActualPostAssaultContinuation.Mode(unsupported));
        var forged = state.DeepClone().AsObject(); forged["members"] = new JsonArray(); Reject(4, () => CampaignCombatActualPostAssaultContinuation.Assessment(frame, forged));
        foreach (var n in new[] { long.MaxValue - 1, long.MaxValue }) { var overflow = state.DeepClone().AsObject(); overflow["stateVersion"] = n; Reject(7, () => Step(frame, overflow, "control-open", "system", 1000)); }
        var geometry = new JsonObject { ["elements"] = new JsonArray(new JsonObject { ["currentLocationId"] = "a" }, new JsonObject { ["currentLocationId"] = "b" }), ["guards"] = new JsonArray(new JsonObject { ["currentLocationId"] = "c" }) }; var element = geometry["elements"]![0]!; Assert.True(CampaignCombatActualPostAssaultContinuation.DestinationBlocked(geometry, element, "b")); Assert.True(CampaignCombatActualPostAssaultContinuation.DestinationBlocked(geometry, element, "c")); Assert.False(CampaignCombatActualPostAssaultContinuation.DestinationBlocked(geometry, element, "a"));
    }
    [Fact]
    public void ValidInheritedRoundTimestampVariantRegeneratesNativePaidAndContinuation()
    {
        var trace = Trace(0); var args = Arguments(trace); var context = Context(); var source = Object(Bytes(trace.GetProperty("sourceCanonicalUtf8")), "Source");
        var paidPacket = CampaignCombatActualPaidAssaultCodec.Object(Encoding.ASCII.GetBytes(Text(source, "paidSourceCanonicalUtf8")), "PaidSource"); var admission = CampaignCombatActualPaidAssaultCodec.Object(Encoding.ASCII.GetBytes(Text(paidPacket, "admissionSourceCanonicalUtf8")), "AdmissionSource"); var roundPacket = CampaignCombatActualRoundEntryCodec.Object(Encoding.ASCII.GetBytes(Text(admission, "actualRoundSourceCanonicalUtf8")), "ActualRoundSource"); roundPacket["roundEventCanonicalUtf8"] = new JsonArray(); var rounds = new List<byte[]>();
        foreach (var previous in args.Rounds)
        {
            var input = JsonNode.Parse(previous)!.AsObject(); if (Text(input["command"]!, "kind") == "seal-choice") input["admittedAt"] = Num(input, "admittedAt") + 17; var bytes = CampaignCombatActualRoundEntryCodec.Canonical(input, "RoundInput");
            var result = CampaignCombatActualRoundEntry.ApplyTrustedSource(CampaignCombatActualRoundEntryCodec.Canonical(roundPacket, "ActualRoundSource"), context, args.Selection, rounds, bytes, Dependency); roundPacket["roundEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(result.EventBytes!)); rounds.Add(bytes);
        }
        admission["actualRoundSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualRoundEntryCodec.Canonical(roundPacket, "ActualRoundSource")); paidPacket["admissionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualPaidAssaultCodec.Canonical(admission, "AdmissionSource")); paidPacket["paidEventCanonicalUtf8"] = new JsonArray(); var paidSource = CampaignCombatActualPaidAssaultCodec.Canonical(paidPacket, "PaidSource"); byte[][] paid = [];
        for (var n = 0; n < 8; n++)
        {
            var result = CampaignCombatActualPaidAssault.ReplayTrustedSource(paidSource, context, args.Selection, rounds, paid, Dependency); var control = JsonNode.Parse(result.ControlBytes)!.AsObject(); var prior = JsonNode.Parse(result.ProofBytes)!["base"]!["admissionProof"]!["actualRoundProof"]!["control"]!;
            var cmd = new JsonObject { ["contractVersion"] = 1, ["kind"] = n == 0 ? "commit" : n == 1 ? "resolve" : "advance", ["baseHash"] = control["baseHash"]!.DeepClone(), ["roundId"] = prior["roundId"]!.DeepClone(), ["expectedPriorVersion"] = control["stateVersion"]!.DeepClone(), ["commitmentId"] = control["commitmentId"]?.DeepClone(), ["decisionId"] = null, ["choice"] = null }; var input = new JsonObject { ["command"] = cmd, ["actor"] = "system", ["admittedAt"] = null, ["clockAvailable"] = true };
            var accepted = CampaignCombatActualPaidAssault.ApplyTrustedSource(paidSource, context, args.Selection, rounds, paid, CampaignCombatActualPaidAssaultCodec.Canonical(input, "PaidInput"), Dependency); paidSource = accepted.SourceBytes; paid = accepted.PaidInputs.Select(x => x.ToArray()).ToArray();
        }
        Assert.NotEqual(Encoding.ASCII.GetBytes(Text(source, "paidSourceCanonicalUtf8")), paidSource); source["paidSourceCanonicalUtf8"] = Encoding.ASCII.GetString(paidSource); source["continuationEventCanonicalUtf8"] = new JsonArray(); var next = Canonical(source, "Source"); byte[][] continuation = [];
        var frame = CampaignCombatActualPostAssaultContinuation.Derive(paidSource, context, args.Selection, rounds, paid, Dependency); var state = CampaignCombatActualPostAssaultContinuation.Initial(frame); var originalWorld = state["world"]!.DeepClone();
        foreach (var kind in NativePathKinds)
        {
            var input = Input(frame, state, kind, kind == "repeat" ? Text(frame.Cycle, "actingSide") : "system", kind == "control-open" ? 1000 : kind == "repeat" ? 1001 : null); var accepted = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(next, context, args.Selection, rounds, paid, continuation, Canonical(input, "Input"), Dependency); next = accepted.SourceBytes; continuation = accepted.ContinuationInputs.Select(x => x.ToArray()).ToArray(); state = Object(accepted.ControlBytes, "Control");
        }
        Assert.Equal("repeated", Text(state, "status")); Assert.True(JsonNode.DeepEquals(originalWorld, state["world"])); Assert.Equal(37, Num(state, "stateVersion"));
    }
    [Fact]
    public void IsolatedForcedFinishOriginalExclusionsAndFutureDutyPreservation()
    {
        var trace = Trace(0); var args = Arguments(trace); var frame = Frame(trace, args); var state = Object(Bytes(trace.GetProperty("controlCanonicalUtf8")[2]), "Control"); var noProgress = frame with { Progress = new JsonArray() }; var forced = Step(noProgress, state, "control-open", "system", null); Assert.Equal("finished", Text(forced.State, "status")); Assert.Null(forced.State["timing"]); Assert.Null(forced.State["decisionId"]);
        var excluded = frame.MovementEnd.DeepClone().AsObject(); excluded["excludedBefore"] = new JsonArray(frame.Members[0]!["unit"]!.DeepClone()); var noWitness = frame with { MovementEnd = excluded }; var originalExcluded = state.DeepClone().AsObject(); originalExcluded["movementEnd"] = excluded.DeepClone(); Assert.Equal("finished", Text(Step(noWitness, originalExcluded, "control-open", "system", null).State, "status"));
        var paid = frame.Paid.DeepClone().AsObject(); paid["world"]!["futureObligations"]!.AsArray().Add(new JsonObject { ["isolatedProbe"] = "not-source-admission" }); var future = frame with { Paid = paid }; var retained = state.DeepClone().AsObject(); retained["world"] = paid["world"]!.DeepClone(); Assert.Single(CampaignCombatActualPostAssaultContinuation.Assessment(future, retained)["witnesses"]!.AsArray()); Assert.True(JsonNode.DeepEquals(paid["world"]!["futureObligations"], retained["world"]!["futureObligations"]));
        var pendingPaid = frame.Paid.DeepClone().AsObject(); pendingPaid["world"]!["settlements"]![0]!["relationships"] = null; var pending = frame with { Paid = pendingPaid }; var bad = state.DeepClone().AsObject(); bad["world"] = pendingPaid["world"]!.DeepClone(); Reject(3, () => CampaignCombatActualPostAssaultContinuation.Assessment(pending, bad));
        var engagedPaid = frame.Paid.DeepClone().AsObject(); engagedPaid["world"]!["relationships"]![0]!["kind"] = "engaged"; var engaged = frame with { Paid = engagedPaid }; var probe = state.DeepClone().AsObject(); probe["world"] = engagedPaid["world"]!.DeepClone(); var witness = Assert.Single(CampaignCombatActualPostAssaultContinuation.Assessment(engaged, probe)["witnesses"]!.AsArray()); Assert.Equal(11, Num(witness!, "afterCp")); Assert.Equal(1, Num(witness!, "excessCpDp"));
        foreach (var spent in new long[] { 9, 10 })
        {
            var changedWorld = engagedPaid.DeepClone().AsObject(); var changedMembers = frame.Members.DeepClone().AsArray(); changedMembers[0]!["spentCp"]!["numerator"] = spent;
            var ownId = Text(changedMembers[0]!["unit"]!, "elementId"); changedWorld["world"]!["elements"]!.AsArray().Single(e => Text(e!, "elementId") == ownId)!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = spent;
            var costFrame = engaged with { Paid = changedWorld, Members = changedMembers }; var costState = state.DeepClone().AsObject(); costState["world"] = changedWorld["world"]!.DeepClone(); costState["members"] = changedMembers.DeepClone(); var costs = CampaignCombatActualPostAssaultContinuation.Assessment(costFrame, costState)["witnesses"]!.AsArray();
            if (spent == 9) { var end = Assert.Single(costs); Assert.Equal(15, Num(end!, "afterCp")); Assert.Equal(5, Num(end!, "excessCpDp")); } else Assert.Empty(costs);
        }
    }
    [Fact]
    public void ForeignAuthenticCreationContextAndMalformedContinuationCannotAuthenticateClaims()
    {
        var trace = Trace(0); var args = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "continuationLedger"); var context = Context();
        var config = JsonNode.Parse(CombatDecisionConfigurationCodec.Serialize(context.Configuration))!.AsObject(); config["configId"] = "rules-lab.foreign-context";
        var configuration = CombatDecisionConfigurationCodec.Deserialize(CampaignCombatActualPaidAssaultCodec.Encode(config), Cna1979CombatRuleset.Manifest); var artifact = Cna1979CombatContentCatalog.Artifact;
        var foreign = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, context.Setup, artifact, artifact.Definition.Scenarios.Single(), configuration);
        Reject(4, () => CampaignCombatActualPostAssaultContinuation.ReadProof([], source, foreign, args.Selection, args.Rounds, args.Paid, ledger, Dependency));
        var bad = ledger.Select(x => x.ToArray()).ToArray(); var input = Object(bad[0], "Input"); input["admittedAt"] = true; bad[0] = Encode(input);
        Reject(1, () => CampaignCombatActualPostAssaultContinuation.ReadControl([], source, context, args.Selection, args.Rounds, args.Paid, bad, Dependency));
    }
    private static readonly string[] NativePathKinds = ["release-open", "release-complete", "control-open", "repeat"];
    private static readonly string[] EventMutationFields = ["author", "stateVersion", "priorPrefix"];
    private static readonly string[] ByteKinds = ["Source", "Proof", "Control", "Event", "Input"];
    internal static CampaignCombatActualPostAssaultContinuation.Frame Frame(JsonElement trace, Inputs args) => CampaignCombatActualPostAssaultContinuation.Derive(Encoding.ASCII.GetBytes(Text(Object(Bytes(trace.GetProperty("sourceCanonicalUtf8")), "Source"), "paidSourceCanonicalUtf8")), Context(), args.Selection, args.Rounds, args.Paid, Dependency);
    private static (JsonObject State, byte[]? Event, string? Receipt) Step(CampaignCombatActualPostAssaultContinuation.Frame frame, JsonObject state, string kind, string actor, long? now, bool available = true) => CampaignCombatActualPostAssaultContinuation.Transition(frame, state, Input(frame, state, kind, actor, now, available));
    private static JsonObject Input(CampaignCombatActualPostAssaultContinuation.Frame frame, JsonObject state, string kind, string actor, long? now, bool available = true) => new() { ["command"] = CampaignCombatActualPostAssaultContinuation.Command(frame, state, kind), ["actor"] = actor, ["admittedAt"] = now, ["clockAvailable"] = available };
    private static string Text(JsonNode value, string key) => value[key]!.GetValue<string>();
    private static long Num(JsonNode value, string key) => value[key]!.GetValue<long>();
    private static byte[] Encode(JsonElement value) => CombatActualPaidAssaultTests.Encode(value);
    private static byte[] Encode(JsonNode value) => CampaignCombatActualPostAssaultContinuationCodec.Encode(value);
    private static void Read(byte[] source, Inputs args, IReadOnlyList<byte[]> ledger) => CampaignCombatActualPostAssaultContinuation.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency);
    private static void Resign(JsonObject value) { value.Remove("receiptId"); value["receiptId"] = "apc." + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-post-assault-continuation.receipt.v1", Encode(value)); }
    private static void Reject(int code, Action action) => Assert.StartsWith($"CMB-PAC-{code:000}", Assert.Throws<JsonException>(action).Message);
}
