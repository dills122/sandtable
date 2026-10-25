using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualRepeatedMovementCodec;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;
using static Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRepeatedMovementContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ReactionAndFifthMoveRejectBeforePublishingWithContextBeforeRetry(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var all = Ledger(trace, "movementLedger"); var rear = Cut(trace, 1); var initialCommand = Packet(all[0]); var reaction = Packet(all[1]); reaction["command"]!["destinationLocationId"] = initialCommand["command"]!["originLocationId"]!.DeepClone();
        var before = Replay(rear, args, all.Take(1).ToArray()); Reject(3, () => Apply(rear, args, all.Take(1).ToArray(), Encode(reaction))); Assert.Equal(rear, before.SourceBytes);
        var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var final = Replay(source, args, all); var fifth = Packet(all[^1]); fifth["command"]!["expectedPriorVersion"] = 41; fifth["command"]!["originLocationId"] = Packet(all[^1])["command"]!["destinationLocationId"]!.DeepClone(); fifth["command"]!["destinationLocationId"] = Packet(all[^1])["command"]!["originLocationId"]!.DeepClone(); Reject(5, () => Apply(source, args, all, Encode(fifth))); Assert.Equal(source, final.SourceBytes);
        foreach (var input in all)
        {
            var bad = Packet(input); bad["actor"] = trace.GetProperty("owner").GetString() == "axis" ? "commonwealth" : "axis"; Reject(4, () => Apply(source, args, all, Encode(bad)));
            bad = Packet(input); bad["command"]!["baseHash"] = "sha256:" + new string('0', 64); Reject(4, () => Apply(source, args, all, Encode(bad)));
            bad = Packet(input); bad["command"]!["expectedPriorVersion"] = true; Reject(1, () => Apply(source, args, all, Encode(bad)));
            bad = Packet(input); bad["command"]!["kind"] = "stop"; Reject(3, () => Apply(source, args, all, Encode(bad)));
        }
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SourceFirstClaimsCausalForgeriesAndFiveIndependentLedgers(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = Ledger(trace, "movementLedger"); var original = Replay(source, args, ledger);
        for (var n = 0; n < 4; n++)
        {
            var p = Packet(source); var ev = Packet(Encoding.ASCII.GetBytes(p["movementEventCanonicalUtf8"]![n]!.GetValue<string>())); ev["effect"]!["costs"]!["afterCp"] = CampaignCombatActualPaidAssaultSupport.N(ev["effect"]!["costs"]!, "afterCp") - 1; Resign(ev); p["movementEventCanonicalUtf8"]![n] = Encoding.ASCII.GetString(Canonical(ev, "Event")); Reject(6, () => Replay(Encode(p), args, ledger));
        }
        foreach (var name in new[] { "movementEnd", "nextCycleProgress", "members" })
        {
            var proof = Packet(original.ProofBytes); if (name == "movementEnd") proof["control"]![name]!["ordinal"] = 2; else if (name == "nextCycleProgress") proof["control"]![name] = new JsonArray(); else proof["control"]![name]![0]!["spentCp"]!["numerator"] = 5;
            Reject(6, () => CampaignCombatActualRepeatedMovement.ReadProof(Encode(proof), source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, ledger, Dependency));
        }
        Reject(4, () => Replay(source, args, ledger.Take(3).ToArray())); Reject(6, () => Replay(source, args, ledger.Reverse().ToArray()));
        var broken = Packet(source); broken["continuationSourceCanonicalUtf8"] = "{}"; Reject(1, () => CampaignCombatActualRepeatedMovement.ReadControl([], Encode(broken), Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, null!, Dependency));
        var ledgers = new[] { args.Selection, args.Rounds, args.Paid, args.Continuation, ledger };
        for (var which = 0; which < 5; which++)
        {
            var all = ledgers.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(); var bad = Packet(all[which][0]); bad["command"]!["contractVersion"] = true; all[which][0] = Encode(bad);
            Reject(1, () => CampaignCombatActualRepeatedMovement.ReadProof([], source, Context(), all[0], all[1], all[2], all[3], all[4], Dependency));
        }
    }
    [Fact]
    public void ClosedSyntaxSignedCostsCanonicalVersionsAndPreparseBounds()
    {
        var trace = Trace(0); var input = Packet(Ledger(trace, "movementLedger")[0]);
        foreach (var n in new long[] { -1, long.MaxValue }) { var bad = input.DeepClone(); bad["command"]!["expectedPriorVersion"] = n; if (n < 0) Reject(2, () => Parse(Encode(bad), "Input")); else _ = Parse(Encode(bad), "Input"); }
        var version = input.DeepClone(); version["command"]!["contractVersion"] = 2; Reject(3, () => Parse(Encode(version), "Input")); version["command"]!["contractVersion"] = false; Reject(1, () => Parse(Encode(version), "Input"));
        var source = Bytes(trace.GetProperty("sourceCanonicalUtf8")); foreach (var kind in new[] { "Source", "Control", "Proof", "Event", "Input" }) Reject(1, () => Parse(new byte[ByteLimit(kind) + 1], kind));
        Reject(8, () => Parse([.. source, (byte)' '], "Source")); Reject(1, () => Parse(Encoding.ASCII.GetBytes("{\"contractVersion\":1,\"contractVersion\":1}"), "Source")); Reject(1, () => Parse(Encoding.ASCII.GetBytes(new string('[', 40) + "0" + new string(']', 40)), "Source")); Reject(1, () => Parse(Encoding.ASCII.GetBytes("[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]"), "Source"));
        var ev = Packet(Encoding.ASCII.GetBytes(Packet(source)["movementEventCanonicalUtf8"]![3]!.GetValue<string>())); var cost = ev["effect"]!["costs"]!; Assert.Equal(-5, CampaignCombatActualPaidAssaultSupport.N(cost, "afterCohesion")); _ = Canonical(cost, "Cost"); cost["afterCohesion"] = (long)int.MinValue - 1; Reject(2, () => Canonical(cost, "Cost")); cost["afterCohesion"] = true; Reject(1, () => Canonical(cost, "Cost"));
        cost["afterCohesion"] = -5; ev["effect"]!["kind"] = "complete"; Reject(3, () => Canonical(ev, "Event"));
    }
    [Fact]
    public void Exactly68PhysicalPathsAndEveryColdWarmDriftFaultRemainVisible()
    {
        var trace = Trace(0); var args = Arguments(trace); var source = EmptySource(trace); var paths = new HashSet<string>(StringComparer.Ordinal);
        byte[] Observe(string path) { paths.Add(path); return Dependency(path); }
        _ = CampaignCombatActualRepeatedMovement.ReplayTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, [], Observe); Assert.Equal(68, paths.Count);
        foreach (var test in new[] { "combat-actual-paid-assault-v1.json", "combat-actual-post-assault-continuation-v1.json", "combat-actual-repeated-movement-v1.json" }) Assert.DoesNotContain("docs/specs/fixtures/" + test, paths);
        foreach (var path in paths)
        {
            byte[] Changed(string p) => p == path ? [0] : Dependency(p); byte[] Missing(string p) => p == path ? throw new FileNotFoundException(p) : Dependency(p); byte[] Unreadable(string p) => p == path ? throw new UnauthorizedAccessException(p) : Dependency(p); byte[] Null(string p) => p == path ? null! : Dependency(p);
            foreach (var bytes in new Func<string, byte[]>[] { Changed, Missing, Unreadable, Null }) Reject(9, () => CampaignCombatActualRepeatedMovement.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, [], bytes));
            var hits = 0; byte[] Drift(string p) => p == path && ++hits > 1 ? [0] : Dependency(p); Reject(9, () => CampaignCombatActualRepeatedMovement.ReadSource(source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, [], Drift));
        }
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-repeated-movement-v1.schema.json")); var manifest = new JsonObject(); foreach (var p in schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)) manifest[p.Name] = p.Value.GetString(); Assert.Equal(66, manifest.Count);
        Assert.Equal("sha256:" + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-movement.dependency-manifest.v1", Encode(manifest)), Manifest(CampaignCombatActualRepeatedMovementCodec.VerifyDependencies(Dependency)));
    }
    [Fact]
    public void AuthenticRoundClockVariantRegeneratesAncestryAndAdmitsActualMove()
    {
        var trace = CombatActualPostAssaultContinuationTests.Trace(0); var args = CombatActualPostAssaultContinuationTests.Arguments(trace); var context = Context(); var source = CampaignCombatActualPostAssaultContinuationCodec.Object(CombatActualPostAssaultContinuationTests.Bytes(trace.GetProperty("sourceCanonicalUtf8")), "Source");
        var paidPacket = CampaignCombatActualPaidAssaultCodec.Object(Encoding.ASCII.GetBytes(CampaignCombatActualPaidAssaultSupport.T(source, "paidSourceCanonicalUtf8")), "PaidSource"); var admission = CampaignCombatActualPaidAssaultCodec.Object(Encoding.ASCII.GetBytes(CampaignCombatActualPaidAssaultSupport.T(paidPacket, "admissionSourceCanonicalUtf8")), "AdmissionSource"); var roundPacket = CampaignCombatActualRoundEntryCodec.Object(Encoding.ASCII.GetBytes(CampaignCombatActualPaidAssaultSupport.T(admission, "actualRoundSourceCanonicalUtf8")), "ActualRoundSource"); roundPacket["roundEventCanonicalUtf8"] = new JsonArray(); var rounds = new List<byte[]>();
        foreach (var previous in args.Rounds)
        {
            var input = JsonNode.Parse(previous)!.AsObject(); if (CampaignCombatActualPaidAssaultSupport.T(input["command"]!, "kind") == "seal-choice") input["admittedAt"] = CampaignCombatActualPaidAssaultSupport.N(input, "admittedAt") + 17; var bytes = CampaignCombatActualRoundEntryCodec.Canonical(input, "RoundInput");
            var result = CampaignCombatActualRoundEntry.ApplyTrustedSource(CampaignCombatActualRoundEntryCodec.Canonical(roundPacket, "ActualRoundSource"), context, args.Selection, rounds, bytes, Dependency); roundPacket["roundEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(result.EventBytes!)); rounds.Add(bytes);
        }
        admission["actualRoundSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualRoundEntryCodec.Canonical(roundPacket, "ActualRoundSource")); paidPacket["admissionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualPaidAssaultCodec.Canonical(admission, "AdmissionSource")); paidPacket["paidEventCanonicalUtf8"] = new JsonArray(); var paidSource = CampaignCombatActualPaidAssaultCodec.Canonical(paidPacket, "PaidSource"); byte[][] paid = [];
        for (var n = 0; n < 8; n++)
        {
            var result = CampaignCombatActualPaidAssault.ReplayTrustedSource(paidSource, context, args.Selection, rounds, paid, Dependency); var control = JsonNode.Parse(result.ControlBytes)!.AsObject(); var prior = JsonNode.Parse(result.ProofBytes)!["base"]!["admissionProof"]!["actualRoundProof"]!["control"]!;
            var cmd = new JsonObject { ["contractVersion"] = 1, ["kind"] = n == 0 ? "commit" : n == 1 ? "resolve" : "advance", ["baseHash"] = control["baseHash"]!.DeepClone(), ["roundId"] = prior["roundId"]!.DeepClone(), ["expectedPriorVersion"] = control["stateVersion"]!.DeepClone(), ["commitmentId"] = control["commitmentId"]?.DeepClone(), ["decisionId"] = null, ["choice"] = null }; var input = new JsonObject { ["command"] = cmd, ["actor"] = "system", ["admittedAt"] = null, ["clockAvailable"] = true };
            var accepted = CampaignCombatActualPaidAssault.ApplyTrustedSource(paidSource, context, args.Selection, rounds, paid, CampaignCombatActualPaidAssaultCodec.Canonical(input, "PaidInput"), Dependency); paidSource = accepted.SourceBytes; paid = accepted.PaidInputs.Select(x => x.ToArray()).ToArray();
        }
        Assert.NotEqual(Encoding.ASCII.GetBytes(CampaignCombatActualPaidAssaultSupport.T(source, "paidSourceCanonicalUtf8")), paidSource); source["paidSourceCanonicalUtf8"] = Encoding.ASCII.GetString(paidSource); source["continuationEventCanonicalUtf8"] = new JsonArray(); var next = CampaignCombatActualPostAssaultContinuationCodec.Canonical(source, "Source"); byte[][] continuation = [];
        var frame = CampaignCombatActualPostAssaultContinuation.Derive(paidSource, context, args.Selection, rounds, paid, Dependency); var state = CampaignCombatActualPostAssaultContinuation.Initial(frame); var originalWorld = state["world"]!.DeepClone();
        foreach (var kind in new[] { "release-open", "release-complete", "control-open", "repeat" })
        {
            var input = new JsonObject { ["command"] = CampaignCombatActualPostAssaultContinuation.Command(frame, state, kind), ["actor"] = kind == "repeat" ? CampaignCombatActualPaidAssaultSupport.T(frame.Cycle, "actingSide") : "system", ["admittedAt"] = kind == "control-open" ? 1000 : kind == "repeat" ? 1001 : null, ["clockAvailable"] = true }; var accepted = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(next, context, args.Selection, rounds, paid, continuation, CampaignCombatActualPostAssaultContinuationCodec.Canonical(input, "Input"), Dependency); next = accepted.SourceBytes; continuation = accepted.ContinuationInputs.Select(x => x.ToArray()).ToArray(); state = CampaignCombatActualPostAssaultContinuationCodec.Object(accepted.ControlBytes, "Control");
        }
        Assert.Equal("repeated", CampaignCombatActualPaidAssaultSupport.T(state, "status")); Assert.True(JsonNode.DeepEquals(originalWorld, state["world"])); Assert.Equal(37, CampaignCombatActualPaidAssaultSupport.N(state, "stateVersion"));
        var own = new JsonObject { ["contractVersion"] = 1, ["continuationSourceCanonicalUtf8"] = Encoding.ASCII.GetString(next), ["movementEventCanonicalUtf8"] = new JsonArray() };
        var derived = CampaignCombatActualRepeatedMovement.Derive(next, context, args.Selection, rounds, paid, continuation, Dependency); var initial = CampaignCombatActualRepeatedMovement.Initial(derived);
        var move = CampaignCombatActualRepeatedMovement.Command(derived, initial, CampaignCombatActualPaidAssaultSupport.T(derived.Cycle, "actingSide") + "-rear");
        var moved = CampaignCombatActualRepeatedMovement.ApplyTrustedSource(Encode(own), context, args.Selection, rounds, paid, continuation, [], Canonical(move, "Input"), Dependency);
        Assert.Equal(38, moved.Control.GetProperty("stateVersion").GetInt64()); Assert.Equal(9, moved.Control.GetProperty("members")[0].GetProperty("spentCp").GetProperty("numerator").GetInt64());
    }

    [Fact]
    public void AuthenticForeignContextFinishAndOriginalEligibilityCapacityReject()
    {
        var trace = Trace(0); var args = Arguments(trace); var source = EmptySource(trace); var input = Ledger(trace, "movementLedger")[0]; var context = Context();
        var config = JsonNode.Parse(CombatDecisionConfigurationCodec.Serialize(context.Configuration))!.AsObject(); config["configId"] = "rules-lab.foreign-context";
        var configuration = CombatDecisionConfigurationCodec.Deserialize(CampaignCombatActualPaidAssaultCodec.Encode(config), Cna1979CombatRuleset.Manifest); var artifact = Cna1979CombatContentCatalog.Artifact;
        var foreign = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, context.Setup, artifact, artifact.Definition.Scenarios.Single(), configuration);
        Reject(4, () => CampaignCombatActualRepeatedMovement.ReadProof([], source, foreign, args.Selection, args.Rounds, args.Paid, args.Continuation, [], Dependency));
        using var f = CombatActualPostAssaultContinuationTests.Fixture(); var finished = f.RootElement.GetProperty("traces").EnumerateArray().Single(x => x.GetProperty("owner").GetString() == trace.GetProperty("owner").GetString() && x.GetProperty("firstSeal").GetString() == trace.GetProperty("firstSeal").GetString() && x.GetProperty("outcome").GetString() == "finish");
        var finishPacket = Packet(source); finishPacket["continuationSourceCanonicalUtf8"] = finished.GetProperty("sourceCanonicalUtf8").GetString(); Reject(3, () => CampaignCombatActualRepeatedMovement.ReplayTrustedSource(Encode(finishPacket), context, args.Selection, args.Rounds, args.Paid, Ledger(finished, "continuationLedger"), [], Dependency));
        var frame = CampaignCombatActualRepeatedMovement.Derive(Ancestor(trace), context, args.Selection, args.Rounds, args.Paid, args.Continuation, Dependency); var state = CampaignCombatActualRepeatedMovement.Initial(frame); var original = Canonical(state, "Control"); var cmd = Packet(input);
        var excluded = state.DeepClone().AsObject(); excluded["movementEnd"]!["excludedBefore"] = new JsonArray(excluded["members"]![0]!["unit"]!.DeepClone()); Reject(5, () => CampaignCombatActualRepeatedMovement.Transition(frame, excluded, cmd)); Assert.Equal(original, Canonical(state, "Control"));
        var overflow = state.DeepClone().AsObject(); overflow["stateVersion"] = long.MaxValue; var overflowInput = cmd.DeepClone().AsObject(); overflowInput["command"]!["expectedPriorVersion"] = long.MaxValue; Reject(7, () => CampaignCombatActualRepeatedMovement.Transition(frame, overflow, overflowInput));
        var low = state.DeepClone().AsObject(); var id = low["members"]![0]!["unit"]!["elementId"]!.GetValue<string>(); var own = low["world"]!["elements"]!.AsArray().Single(x => x!["elementId"]!.GetValue<string>() == id)!; low["members"]![0]!["spentCp"] = CampaignCombatActualPaidAssaultSupport.Cp(9); own["operationalState"]!["capabilityPointsExpended"] = CampaignCombatActualPaidAssaultSupport.Cp(9); own["operationalState"]!["cohesionLevel"] = int.MinValue; Reject(7, () => CampaignCombatActualRepeatedMovement.Transition(frame, low, cmd));
        var occupied = state.DeepClone().AsObject(); occupied["world"]!["elements"]!.AsArray().Single(x => x!["elementId"]!.GetValue<string>() != id)!["currentLocationId"] = cmd["command"]!["destinationLocationId"]!.DeepClone(); Reject(5, () => CampaignCombatActualRepeatedMovement.Transition(frame, occupied, cmd));
    }
    private static byte[] Encode(JsonNode value) => CampaignCombatActualRepeatedMovementCodec.Encode(value);
    private static void Resign(JsonObject ev) { ev.Remove("receiptId"); ev["receiptId"] = "armv." + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-movement.receipt.v1", Encode(ev)); }
    private static void Reject(int code, Action action) => Assert.StartsWith($"CMB-RMV-{code:000}", Assert.Throws<JsonException>(action).Message);
}
