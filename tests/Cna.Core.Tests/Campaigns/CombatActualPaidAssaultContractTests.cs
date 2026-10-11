using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultCodec;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultSupport;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualPaidAssaultContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GenuineForgeryAndIndependentLedgerMismatchAreRejected(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index]; var context = Context();
        var full = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!); var selection = Ledger(row, "selectionLedger");
        var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger");
        for (var cut = 1; cut <= paid.Length; cut++)
        {
            var source = Object(full, "PaidSource"); while (source["paidEventCanonicalUtf8"]!.AsArray().Count > cut) source["paidEventCanonicalUtf8"]!.AsArray().RemoveAt(cut);
            var events = source["paidEventCanonicalUtf8"]!.AsArray(); var accepted = Object(Encoding.ASCII.GetBytes(events[^1]!.GetValue<string>()), "PaidEvent");
            accepted["campaignId"] = T(accepted, "campaignId") + "-forged"; accepted.Remove("receiptId");
            accepted["receiptId"] = "cmb." + Domain("sandtable.combat.actual-paid-assault.receipt.v1", CampaignCombatActualPaidAssaultCodec.Encode(accepted));
            events[^1] = Encoding.ASCII.GetString(Canonical(accepted, "PaidEvent")); var data = Canonical(source, "PaidSource");
            Reject(6, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(data, context, selection, rounds, paid.Take(cut).ToArray(), Dependency));
        }
        Reject(4, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(full, context, selection, rounds, paid[..^1], Dependency));
        var bad = paid.Select(x => x.ToArray()).ToArray(); var last = Object(bad[^1], "PaidInput"); last["actor"] = "axis"; bad[^1] = Canonical(last, "PaidInput");
        Reject(4, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(full, context, selection, rounds, bad, Dependency));
        Reject(4, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(full, context, selection[..^1], rounds, paid, Dependency));
        Reject(4, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(full, context, selection, rounds[..^1], paid, Dependency));
        var proof = Object(Encoding.ASCII.GetBytes(row.GetProperty("proofCanonicalUtf8").GetString()!), "PaidProof"); proof["control"]!["closed"] = false;
        Reject(6, () => CampaignCombatActualPaidAssault.ReadProof(Canonical(proof, "PaidProof"), full, context, selection, rounds, paid, Dependency));
    }
    [Fact]
    public void EveryPhysicalDependencyIsCheckedOnWarmRecoveryBeforeClaims()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var source = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!);
        var selection = Ledger(row, "selectionLedger"); var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger"); var context = Context();
        var observed = new HashSet<string>(StringComparer.Ordinal);
        byte[] Read(string path) { observed.Add(path); return Dependency(path); }
        var result = CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, paid, Read);
        Assert.Equal(64, observed.Count);
        foreach (var path in observed)
        {
            byte[] Changed(string current) => current == path ? [0] : Dependency(current);
            Reject(9, () => CampaignCombatActualPaidAssault.ReadProof([0], source, context, selection, rounds, paid, Changed));
            byte[] Missing(string current) => current == path ? throw new FileNotFoundException() : Dependency(current);
            Reject(9, () => CampaignCombatActualPaidAssault.ReadControl([0], source, context, selection, rounds, paid, Missing));
            byte[] NullBytes(string current) => current == path ? null! : Dependency(current);
            Reject(9, () => CampaignCombatActualPaidAssault.ReadSource(source, context, selection, rounds, paid, NullBytes));
        }
        Assert.Equal(source, result.SourceBytes);
    }
    [Fact]
    public void StrictGrammarRejectsMalformedPrimitivesVersionsBoundsAndNoncanonicalBytes()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var input = Object(Ledger(row, "paidLedger")[0], "PaidInput");
        void Bad(string field, JsonNode value, int code) { var v = input.DeepClone().AsObject(); v["command"]![field] = value; Reject(code, () => Parse(CampaignCombatActualPaidAssaultCodec.Encode(v), "PaidInput")); }
        Bad("expectedPriorVersion", JsonValue.Create(true)!, 1); Bad("expectedPriorVersion", JsonValue.Create(-1)!, 2);
        Bad("contractVersion", JsonValue.Create(2)!, 3); Bad("roundId", JsonValue.Create(new string('a', 129))!, 2);
        var overflow = input.DeepClone().AsObject(); overflow["admittedAt"] = MaxUtc + 1; Reject(2, () => Parse(CampaignCombatActualPaidAssaultCodec.Encode(overflow), "PaidInput"));
        var bytes = Canonical(input, "PaidInput"); Reject(8, () => Parse([.. bytes, (byte)' '], "PaidInput"));
        Reject(1, () => Parse([0xef, 0xbb, 0xbf, .. bytes], "PaidInput"));
        foreach (var kind in ByteBoundKinds) Reject(1, () => Parse(new byte[ByteLimit(kind) + 1], kind));
        Reject(1, () => Parse(Encoding.ASCII.GetBytes("{\"contractVersion\":1,\"contractVersion\":1}"), "PaidSource"));
        Reject(1, () => Parse(Encoding.ASCII.GetBytes(new string('[', 36) + "0" + new string(']', 36)), "PaidSource"));
        Reject(1, () => Parse(Encoding.ASCII.GetBytes("[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]"), "PaidSource"));
        var claim = Object(Encoding.ASCII.GetBytes(row.GetProperty("proofCanonicalUtf8").GetString()!), "PaidProof");
        claim["base"]!["admissionProof"]!["arithmeticAssessment"]!["paidAdmissionEnabled"] = true;
        Reject(2, () => Parse(CampaignCombatActualPaidAssaultCodec.Encode(claim), "PaidProof"));
    }
    [Fact]
    public void IsolatedAxisSettlementMatrix() => SettlementMatrix(0);
    [Fact]
    public void IsolatedCommonwealthSettlementMatrix() => SettlementMatrix(2);
    private static void SettlementMatrix(int index)
    {
        var ctx = Frame(index); var paid = PaidWorld(ctx).World; var count = 0; var captures = new HashSet<string>(StringComparer.Ordinal); var zeroEngaged = false; var refusalDp = false;
        foreach (var row in Catalogue().Where(x => T(x, "kind") == "capture-refusal"))
        {
            var f = row["facts"]!; var captured = N(row, "attackerCapturedToe") + N(row, "defenderCapturedToe") > 0;
            var retreat = N(f, "requiredRetreat") == 0 ? "not-required" : N(row, "refusalHexes") > 0 ? "refuse-retreat" : "retreat";
            var result = O(("resultId", "res." + new string('f', 64)), ("commitmentId", "cmt." + new string('f', 64)), ("facts", f));
            foreach (var custody in captured ? new[] { "relocate-and-guard", "leave-unguarded" } : new string?[] { null })
            {
                JsonObject? world = null;
                foreach (var cut in ProjectionCuts)
                {
                    if (cut == "custody" && !captured) continue;
                    world = Project(ctx, paid, result, cut, retreat, custody); _ = Canonical(world, "World");
                    Assert.Equal(paid["creationBinding"]!.GetValue<string>(), world["creationBinding"]!.GetValue<string>());
                    if (cut == "resolved") Assert.True(JsonNode.DeepEquals(paid["elements"], world["elements"]));
                    else Assert.Equal(retreat, T(world["settlements"]![0]!["disposition"]!, "kind"));
                }
                var st = world!["settlements"]![0]!; var roles = st["losses"]!["roles"]!.AsArray();
                foreach (var role in roles)
                {
                    var attacker = T(role!, "role") == "attacker"; var percent = N(f, attacker ? "attackerPercent" : "defenderPercent");
                    var refusal = attacker ? 0 : 10 * N(row, "refusalHexes"); var loss = attacker ? (10 * (percent + refusal) + 99) / 100 : 10 * (percent + refusal) / 100;
                    var capture = f["capturedRole"]?.GetValue<string>() == T(role!, "role") ? (loss * N(f, "captureShare") + 99) / 100 : 0;
                    Assert.Equal(loss, N(role!, "lossToe")); Assert.Equal(capture, N(role!, "capturedToe")); Assert.Equal(loss - capture, N(role!, "otherLossToe"));
                    Assert.Equal(10 - loss, N(role!, "remainingToe")); Assert.Equal(loss >= 3 ? 3 : 0, N(role!, "lossDp"));
                    var element = world["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(role!["component"]!["unit"]!, "elementId"))!;
                    var original = paid["elements"]!.AsArray().Single(e => T(e!, "elementId") == T(element, "elementId"))!;
                    Assert.True(JsonNode.DeepEquals(original["origin"], element["origin"])); Assert.True(JsonNode.DeepEquals(original["ammunition"], element["ammunition"]));
                    Assert.True(JsonNode.DeepEquals(original["readiness"], element["readiness"]));
                }
                Assert.Equal(20, world["elements"]!.AsArray().Sum(e => N(e!["components"]![0]!, "currentToe")) + world["guards"]!.AsArray().Sum(g => N(g!, "toe")) + roles.Sum(r => N(r!, "lossToe")));
                if (captured)
                {
                    captures.Add(f["capturedRole"]!.GetValue<string>()); var lot = world["custodyLots"]![0]!; Assert.Equal(N(row, "attackerCapturedToe") + N(row, "defenderCapturedToe"), N(lot, "quantity"));
                    var obligation = Assert.Single(world["futureObligations"]!.AsArray())!; Assert.Equal("retained-unimplemented", T(obligation, "status"));
                    if (custody == "relocate-and-guard") { Assert.Equal("guarded", T(lot, "status")); Assert.Equal(1, N(world["guards"]![0]!, "toe")); Assert.Empty(world["replacementEntitlements"]!.AsArray()); }
                    else
                    {
                        Assert.Equal("escaped", T(lot, "status")); Assert.Null(lot["currentLocationId"]); Assert.Empty(world["guards"]!.AsArray()); var entitlement = world["replacementEntitlements"]![0]!;
                        Assert.Equal(12, N(entitlement, "delayOperationStages")); Assert.Equal(5, N(entitlement["eligibleScope"]!, "gameTurn")); Assert.Equal(1, N(entitlement["eligibleScope"]!, "operationStage"));
                    }
                }
                Assert.Equal(retreat == "retreat" ? 1 : 0, N(st["retreat"]!, "completedDistance"));
                if (N(f, "requiredRetreat") > 0) Assert.NotEqual("engaged", st["relationships"]!["kind"]?.GetValue<string>());
                zeroEngaged |= B(f, "rawEngaged") && N(row, "attackerLossToe") + N(row, "defenderLossToe") == 0;
                refusalDp |= N(row, "refusalHexes") > 0 && N(row, "defenderLossDp") > 0; count++;
            }
        }
        Assert.Equal(9728, count); Assert.Equal(2, captures.Count); Assert.True(zeroEngaged); Assert.True(refusalDp);
        var facts = Catalogue().First(x => T(x, "kind") == "capture-refusal" && N(x["facts"]!, "requiredRetreat") > 0 && x["facts"]!["capturedRole"] is null && N(x, "attackerLossDp") == 0)["facts"]!;
        var boundary = paid.DeepClone().AsObject(); var a = boundary["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(ctx.Selection["attacker"]!["unit"]!, "elementId"))!;
        var d = boundary["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(ctx.Selection["defender"]!["unit"]!, "elementId"))!;
        a["operationalState"]!["cohesionLevel"] = 9; d["operationalState"]!["capabilityPointsExpended"] = Cp(10);
        var projected = Project(ctx, boundary, O(("resultId", "res." + new string('f', 64)), ("commitmentId", "cmt." + new string('f', 64)), ("facts", facts)), "relationships", "retreat");
        Assert.Equal(11, N(projected["settlements"]![0]!["retreat"]!["afterCp"]!, "numerator")); Assert.Equal(1, N(projected["settlements"]![0]!["retreat"]!, "excessCpDp"));
        Assert.Equal(10, N(projected["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(a, "elementId"))!["operationalState"]!, "cohesionLevel"));
    }
    private static readonly string[] ByteBoundKinds = ["PaidSource", "PaidProof", "PaidControl", "PaidEvent", "PaidInput", "AssaultResult", "World", "AdmissionSource"];
    private static readonly string[] ProjectionCuts = ["resolved", "disposition", "losses", "retreat", "custody", "relationships"];
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void IsolatedMandatoryWindowsValidateOwnersClockFallbackRetriesAndClosure(int index)
    {
        var ctx = Frame(index); var rows = Catalogue().Where(x => T(x, "kind") == "capture-refusal").ToArray();
        var facts = new[] { rows.First(x => N(x["facts"]!, "requiredRetreat") > 0 && x["facts"]!["capturedRole"] is null)["facts"]!,
            rows.First(x => x["facts"]!["capturedRole"]?.GetValue<string>() == "attacker" && N(x, "attackerCapturedToe") > 0)["facts"]!,
            rows.First(x => x["facts"]!["capturedRole"]?.GetValue<string>() == "defender" && N(x, "defenderCapturedToe") > 0)["facts"]! };
        foreach (var f in facts)
        {
            var state = IsolatedResolved(ctx, f); var isRetreat = f["capturedRole"] is null;
            if (!isRetreat) { if (N(f, "requiredRetreat") > 0) state = Step(ctx, state, "advance").State; else state = Step(ctx, state, "advance").State; for (var i = 0; i < 2; i++) state = Step(ctx, state, "advance").State; }
            var waiting = Step(ctx, state, "advance", now: 10000).State; var w = waiting["window"]!; var kind = T(w, "kind"); var owner = T(w, "owner");
            Assert.Equal(isRetreat ? "retreat" : "custody", kind); Assert.Equal(10000, N(w["timing"]!, "highWaterUnixMilliseconds")); Assert.Equal(40000, N(w["timing"]!, "deadlineUnixMilliseconds"));
            foreach (var choice in isRetreat ? new[] { "retreat", "refuse-retreat" } : ["relocate-and-guard", "leave-unguarded"])
            {
                var chosen = Step(ctx, waiting, "choose", choice, owner, 10001); Assert.Null(chosen.State["window"]);
                using var ev = JsonDocument.Parse(chosen.Event!); Assert.Equal(owner, ev.RootElement.GetProperty("author").GetString());
                var retry = Step(ctx, chosen.State, "choose", choice, owner, 10001, decision: T(w, "decisionId")); Assert.Null(retry.Event); Assert.Equal(chosen.Receipt, retry.Receipt);
                var completed = chosen.State; while (!B(completed, "closed")) completed = Step(ctx, completed, "advance").State;
                Assert.NotNull(completed["caCompletionReceiptId"]); Assert.NotNull(completed["world"]!["settlements"]![0]!["relationships"]);
            }
            var defaultChoice = isRetreat ? "retreat" : "relocate-and-guard";
            Reject(5, () => Step(ctx, waiting, "choose", defaultChoice, owner, 40000));
            Reject(4, () => Step(ctx, waiting, "choose", defaultChoice, owner == "axis" ? "commonwealth" : "axis", null, false));
            Reject(4, () => Step(ctx, waiting, "choose", "invalid", owner, null, false));
            Reject(6, () => Step(ctx, waiting, "advance"));
            foreach (var (now, available) in new (long?, bool)[] { (null, true), (10001, false), (9999, true) })
            {
                var fallback = Step(ctx, waiting, "choose", defaultChoice, owner, now, available); Assert.Null(fallback.State["window"]);
                using var ev = JsonDocument.Parse(fallback.Event!); Assert.Equal("system", ev.RootElement.GetProperty("author").GetString()); Assert.Equal(owner, ev.RootElement.GetProperty("input").GetProperty("actor").GetString());
            }
            var early = Step(ctx, waiting, "expire", now: 10001); Assert.Null(early.Event); Assert.Null(early.Receipt); Assert.True(JsonNode.DeepEquals(waiting, early.State));
            Assert.Null(Step(ctx, waiting, "expire", now: 40000).State["window"]); Assert.Null(Step(ctx, waiting, "unavailable", now: 10001).State["window"]);
            foreach (var now in new long?[] { null, MaxUtc }) { var fallback = Step(ctx, state, "advance", now: now); using var ev = JsonDocument.Parse(fallback.Event!); Assert.Null(fallback.State["window"]); Assert.Equal(JsonValueKind.Null, ev.RootElement.GetProperty("effect").GetProperty("timing").ValueKind); }
            Reject(2, () => Step(ctx, state, "advance", now: MaxUtc + 1));
        }
    }
    internal static JsonObject IsolatedResolved(PaidAssaultFrame ctx, JsonNode facts)
    {
        var s = CampaignCombatActualPaidAssault.Initial(ctx); s["world"] = PaidWorld(ctx).World; s["commitmentId"] = "cmt." + new string('f', 64); s["status"] = "committed";
        var r = Resolve(s); r["facts"] = facts.DeepClone(); r.Remove("resultId"); r["resultId"] = "res." + Domain("sandtable.combat.assault-result.v2", CampaignCombatActualPaidAssaultCodec.Encode(r));
        s["result"] = r; s["world"] = Project(ctx, s["world"]!.AsObject(), r, "resolved"); s["settlementId"] = s["world"]!["settlements"]![0]!["settlementId"]!.DeepClone(); s["status"] = "resolved"; s["stateVersion"] = 27; return s;
    }
    internal static (JsonObject State, byte[]? Event, string? Receipt) Step(PaidAssaultFrame ctx, JsonObject state, string kind, string? choice = null, string actor = "system", long? now = null, bool available = true, string? decision = null)
    {
        var structural = kind is "commit" or "resolve" or "advance";
        var cmd = O(("contractVersion", 1), ("kind", kind), ("baseHash", state["baseHash"]), ("roundId", ctx.Prior["roundId"]),
            ("expectedPriorVersion", structural ? state["stateVersion"] : null), ("commitmentId", kind == "commit" ? null : state["commitmentId"]),
            ("decisionId", structural ? null : decision is not null ? JsonValue.Create(decision) : state["window"]?["decisionId"] ?? JsonValue.Create("stale-timer")), ("choice", choice));
        return CampaignCombatActualPaidAssault.Transition(ctx, state, O(("command", cmd), ("actor", actor), ("admittedAt", now), ("clockAvailable", available)), false, replay: true);
    }

    [Fact]
    public void IsolatedNativeRngMatchesTwelvePinnedVectorsAndExhaustsAtomically()
    {
        var ctx = Frame(0); var paid = CampaignCombatActualPaidAssault.Initial(ctx); paid["world"] = PaidWorld(ctx).World;
        paid["commitmentId"] = "cmt." + new string('f', 64); paid["status"] = "committed";
        using var vectors = JsonDocument.Parse("""
[[0,0,"66241223","8311c7e78aaf9de6",-1,20,10,false,0,0,0,[2,1,0,0,0,0,0]],[7,0,"54565626","acb75efd838ef5f765",0,0,10,false,0,0,0,[0,1,0,0,0,0,0]],[15,0,"53661522","f40efb4d9cd62b13",1,15,15,false,1,0,0,[2,1,0,0,0,0,3]],[18,0,"65525532","65c46ae5fa64f243",0,0,10,true,1,0,0,[0,1,0,0,0,0,3]],[26,0,"36541166","9ef5fa5dc04ee9e9",0,25,0,false,0,0,0,[3,0,0,0,3,0,0]],[47,0,"223514112","5bf7b6a6067b3cc69d",0,15,20,false,0,0,25,[2,2,0,1,0,0,0]],[208,0,"21665513","c7e42911a034d87a",1,0,20,true,1,1,0,[0,3,0,0,0,3,0]],[1296,0,"11665665","6660d795881dfda164",2,0,0,true,0,0,0,[0,0,0,0,0,0,0]],[4983,0,"66113545","bf1d3c7e2028ab46",-2,10,0,false,1,0,0,[1,0,0,0,0,0,3]],[31707,0,"661111635","3b11728a60307de082",-2,25,0,false,1,0,50,[3,0,2,0,3,0,3]],[0,30,"66443222","4dd79fabb6070dc7",-1,10,10,false,0,0,0,[1,1,0,0,0,0,0]],[0,129,"21636133","fee57211e0ef72202c",0,0,5,false,1,0,0,[0,0,0,0,0,0,3]]]
""");
        var count = 0; var roles = new HashSet<string>(StringComparer.Ordinal); var rejected = false; var crossed = false;
        foreach (var v in vectors.RootElement.EnumerateArray())
        {
            var state = paid.DeepClone().AsObject(); var seed = v[0].GetUInt64(); var cursor = v[1].GetUInt64(); state["randomState"]!["seed"] = seed; state["randomState"]!["nextByteCursor"] = cursor;
            var before = CampaignCombatActualPaidAssaultCodec.Encode(state); var result = Resolve(state); var draws = result["draws"]!.AsArray();
            Assert.Equal(v[2].GetString(), string.Concat(draws.Select(x => N(x!, "die"))));
            var consumed = draws.SelectMany(x => x!["consumed"]!.AsArray()).Select(x => (byte)x!.GetValue<int>()).ToArray(); Assert.Equal(v[3].GetString(), Convert.ToHexStringLower(consumed));
            var facts = result["facts"]!; Assert.Equal(v[4].GetInt32(), N(facts, "differential")); Assert.Equal(v[5].GetInt32(), N(facts, "attackerPercent")); Assert.Equal(v[6].GetInt32(), N(facts, "defenderPercent"));
            Assert.Equal(v[7].GetBoolean(), B(facts, "rawEngaged")); Assert.Equal(v[8].GetInt32(), N(facts, "requiredRetreat")); Assert.Equal(v[10].GetInt32(), N(facts, "captureShare"));
            var end = result["afterRandomState"]!["nextByteCursor"]!.GetValue<ulong>(); Assert.Equal(cursor + (ulong)consumed.Length, end); Assert.Equal(before, CampaignCombatActualPaidAssaultCodec.Encode(state));
            if (draws.Count == 9) { var role = facts["capturedRole"]!.GetValue<string>(); roles.Add(role); Assert.Equal(role + ".capture.share", T(draws[^1]!, "purpose")); } else Assert.Equal(8, draws.Count);
            rejected |= draws.Any(x => x!["consumed"]!.AsArray().Count > 1); crossed |= cursor / 32 != end / 32; count++;
        }
        Assert.Equal(12, count); Assert.Equal(2, roles.Count); Assert.True(rejected); Assert.True(crossed);
        foreach (var cursor in new[] { ulong.MaxValue, ulong.MaxValue - 6 })
        {
            var state = paid.DeepClone().AsObject(); state["randomState"]!["nextByteCursor"] = cursor; var before = CampaignCombatActualPaidAssaultCodec.Encode(state);
            Reject(7, () => Resolve(state)); Assert.Equal(before, CampaignCombatActualPaidAssaultCodec.Encode(state));
        }
    }
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void IsolatedWindowLocalClockIsIndependentOfRetainedAuditHighWater(int index)
    {
        var ctx = Frame(index); var f = Catalogue().First(x => T(x, "kind") == "capture-refusal" && N(x["facts"]!, "requiredRetreat") > 0 && x["facts"]!["capturedRole"]?.GetValue<string>() == "attacker" && N(x, "attackerCapturedToe") > 0)["facts"]!;
        var resolved = IsolatedResolved(ctx, f); var priors = new List<JsonObject>();
        foreach (var acceptedAt in new long[] { 10001, 11000 })
        {
            var state = Step(ctx, resolved, "advance", now: 10000).State; state = Step(ctx, state, "choose", "retreat", T(state["window"]!, "owner"), acceptedAt).State;
            state = Step(ctx, state, "advance").State; state = Step(ctx, state, "advance").State; priors.Add(state);
        }
        Assert.True(JsonNode.DeepEquals(priors[0]["world"], priors[1]["world"]));
        var pair = priors.Select(s => Step(ctx, s, "advance", now: 10500).State).ToArray(); Assert.True(JsonNode.DeepEquals(pair[0]["window"], pair[1]["window"]));
        Assert.Equal(11000, N(pair[1], "acceptedHighWater")); Assert.Equal(10500, N(pair[1]["window"]!["timing"]!, "highWaterUnixMilliseconds"));
        var outcomes = pair.Select(s => Step(ctx, s, "choose", "relocate-and-guard", T(s["window"]!, "owner"), 10600).State).ToArray(); Assert.True(JsonNode.DeepEquals(outcomes[0]["world"], outcomes[1]["world"]));
        Assert.All(outcomes, x => Assert.Null(x["window"]));
    }

    [Fact]
    public void PredebitCertificationRejectsMissingHandlersCapacityResourcesAndCatalogueDamage()
    {
        var ctx = Frame(0); var before = CampaignCombatActualPaidAssaultCodec.Encode(ctx.Prior);
        foreach (var missing in SupportedHandlers()) { var handlers = SupportedHandlers(); handlers.Remove(missing); Reject(7, () => CertifySupport(ctx, handlers)); }
        var extra = SupportedHandlers(); extra.Add("unexpected"); Reject(7, () => CertifySupport(ctx, extra));
        foreach (var key in SupportCapacities().Keys) { var capacities = SupportCapacities(); capacities[key] = 0; Reject(7, () => CertifySupport(ctx, capacities: capacities)); }
        var rows = Catalogue().ToArray(); Reject(7, () => CertifySupport(ctx, records: rows[..^1])); Reject(7, () => CertifySupport(ctx, records: rows.Prepend(rows[0])));
        var changed = rows.Select(x => x.DeepClone().AsObject()).ToArray(); changed[1300]["attackerPercent"] = N(changed[1300], "attackerPercent") + 1; Reject(7, () => CertifySupport(ctx, records: changed));
        Assert.Equal(before, CampaignCombatActualPaidAssaultCodec.Encode(ctx.Prior));
        var invalidPrior = ctx.Prior.DeepClone().AsObject(); invalidPrior["world"]!["elements"]![0]!["ammunition"]!["points"] = 0;
        var invalid = ctx with { Prior = invalidPrior }; var badBefore = CampaignCombatActualPaidAssaultCodec.Encode(invalidPrior);
        Reject(7, () => CertifySupport(invalid)); Assert.Equal(badBefore, CampaignCombatActualPaidAssaultCodec.Encode(invalidPrior));
    }

    [Fact]
    public void CanonicalTextCountsScalarsAndPreservesPythonAsciiEscapes()
    {
        var bytes = Encoding.ASCII.GetBytes("\"" + string.Concat(Enumerable.Repeat("\\ud83d\\ude00", 256)) + "\"");
        Assert.Equal(bytes, Canonical(Parse(bytes, "string"), "string"));
        Reject(2, () => Parse(Encoding.ASCII.GetBytes("\"" + string.Concat(Enumerable.Repeat("\\ud83d\\ude00", 257)) + "\""), "string"));
        var lone = Encoding.ASCII.GetBytes("\"\\ud800\""); Assert.Equal(lone, Canonical(Parse(lone, "string"), "string"));
        var literal = Encoding.ASCII.GetBytes("\"\\\\u0041\""); Assert.Equal(literal, Canonical(Parse(literal, "string"), "string"));
        Reject(8, () => Parse(Encoding.ASCII.GetBytes("\"\\u0041\""), "string"));
        Reject(8, () => Parse(Encoding.ASCII.GetBytes("\"\\u00E9\""), "string"));
    }

    [Fact]
    public void PhysicalChangesAfterFirstReadRemainVisibleAtMaterialBoundaries()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var source = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!);
        var selection = Ledger(row, "selectionLedger"); var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger"); var context = Context(); var paths = new HashSet<string>(StringComparer.Ordinal);
        byte[] Observe(string path) { paths.Add(path); return Dependency(path); }
        _ = CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, paid, Observe);
        foreach (var path in paths)
        {
            var reads = 0;
            byte[] Changed(string current) { if (current == path && ++reads > 1) return [0]; return Dependency(current); }
            Reject(9, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, paid, Changed));
            byte[] Unreadable(string current) => current == path ? throw new UnauthorizedAccessException() : Dependency(current);
            Reject(9, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, paid, Unreadable));
        }
    }
    [Fact]
    public void GenuineClosedHistoryRejectsMalformedRetriesReorderedLedgersAndNewCommandsButIgnoresStaleTimers()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var source = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!);
        var selection = Ledger(row, "selectionLedger"); var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger"); var context = Context();
        var result = CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, paid, Dependency);
        var nullLedger = paid.ToArray(); nullLedger[0] = null!;
        Reject(1, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, rounds, nullLedger, Dependency));
        var malformed = Object(paid[0], "PaidInput"); malformed["command"]!["expectedPriorVersion"] = true;
        Reject(1, () => CampaignCombatActualPaidAssault.ApplyTrustedSource(source, context, selection, rounds, paid, CampaignCombatActualPaidAssaultCodec.Encode(malformed), Dependency, false));
        var reordered = rounds.ToArray(); (reordered[0], reordered[1]) = (reordered[1], reordered[0]);
        Reject(4, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, selection, reordered, paid, Dependency));
        reordered = selection.ToArray(); (reordered[0], reordered[1]) = (reordered[1], reordered[0]);
        Reject(4, () => CampaignCombatActualPaidAssault.ReplayTrustedSource(source, context, reordered, rounds, paid, Dependency));
        var timer = Object(paid[^1], "PaidInput"); var cmd = timer["command"]!; cmd["kind"] = "expire"; cmd["expectedPriorVersion"] = null; cmd["decisionId"] = "stale-timer"; timer["admittedAt"] = MaxUtc;
        var same = CampaignCombatActualPaidAssault.ApplyTrustedSource(source, context, selection, rounds, paid, Canonical(timer, "PaidInput"), Dependency, false);
        Assert.Equal(source, same.SourceBytes); Assert.Null(same.EventBytes); Assert.Null(same.ReceiptId); Assert.False(same.Duplicate);
        cmd["kind"] = "advance"; cmd["decisionId"] = null; cmd["expectedPriorVersion"] = 33; timer["admittedAt"] = null;
        Reject(7, () => CampaignCombatActualPaidAssault.ApplyTrustedSource(source, context, selection, rounds, paid, Canonical(timer, "PaidInput"), Dependency, false));
        var controlBytes = result.ControlBytes; var proofBytes = result.ProofBytes; Array.Fill(controlBytes, (byte)0); Array.Fill(proofBytes, (byte)0);
        Assert.Equal(Encoding.ASCII.GetBytes(row.GetProperty("proofCanonicalUtf8").GetString()!), result.ProofBytes);
        Assert.Equal(33, result.Control.GetProperty("stateVersion").GetInt64());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public void MalformedIndependentPredecessorInputsRetainSyntaxDiagnosticsBeforeReplay(bool roundLedger, int code)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context();
        var source = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!);
        var proof = Encoding.ASCII.GetBytes(row.GetProperty("proofCanonicalUtf8").GetString()!); var control = Canonical(Object(proof, "PaidProof")["control"]!, "PaidControl");
        var selection = Ledger(row, "selectionLedger"); var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger");
        var inputs = roundLedger ? rounds : selection; var mutated = JsonNode.Parse(inputs[0])!.AsObject();
        if (code == 1) mutated["admittedAt"] = true;
        else if (code == 2) mutated["admittedAt"] = -1;
        else mutated["command"]!["contractVersion"] = 2;
        inputs[0] = CampaignCombatActualPaidAssaultCodec.Encode(mutated);
        byte[] Changed(string path) => path == "docs/specs/combat-actual-paid-assault-v1.schema.json" ? [0] : Dependency(path);
        AssertEntryPoints(9, Changed);
        AssertEntryPoints(code, Dependency);
        void AssertEntryPoints(int expected, Func<string, byte[]> dependencies)
        {
            Reject(expected, () => CampaignCombatActualPaidAssault.ReadSource(source, context, selection, rounds, paid, dependencies));
            Reject(expected, () => CampaignCombatActualPaidAssault.ReadProof(proof, source, context, selection, rounds, paid, dependencies));
            Reject(expected, () => CampaignCombatActualPaidAssault.ReadControl(control, source, context, selection, rounds, paid, dependencies));
            Reject(expected, () => CampaignCombatActualPaidAssault.ApplyTrustedSource(source, context, selection, rounds, paid, paid[0], dependencies, false));
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoherentIndependentPredecessorInputMismatchRemainsAuthenticationFailure(bool roundLedger)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context();
        var source = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!); var proof = Encoding.ASCII.GetBytes(row.GetProperty("proofCanonicalUtf8").GetString()!);
        var control = Canonical(Object(proof, "PaidProof")["control"]!, "PaidControl");
        var selection = Ledger(row, "selectionLedger"); var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger");
        var inputs = roundLedger ? rounds : selection; var mutated = JsonNode.Parse(inputs[0])!.AsObject(); mutated["admittedAt"] = 123456789;
        inputs[0] = CampaignCombatActualPaidAssaultCodec.Encode(mutated);
        Reject(4, () => CampaignCombatActualPaidAssault.ReadSource(source, context, selection, rounds, paid, Dependency));
        Reject(4, () => CampaignCombatActualPaidAssault.ReadProof(proof, source, context, selection, rounds, paid, Dependency));
        Reject(4, () => CampaignCombatActualPaidAssault.ReadControl(control, source, context, selection, rounds, paid, Dependency));
        Reject(4, () => CampaignCombatActualPaidAssault.ApplyTrustedSource(source, context, selection, rounds, paid, paid[0], Dependency, false));
    }

    internal static PaidAssaultFrame Frame(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index]; var packet = Object(Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!), "PaidSource");
        return CampaignCombatActualAssaultAdmission.Admit(Encoding.ASCII.GetBytes(T(packet, "admissionSourceCanonicalUtf8")), Context(), Ledger(row, "selectionLedger"), Ledger(row, "roundLedger"), Dependency);
    }
    internal static void Reject(int code, Action action) => Assert.Equal($"CMB-APA-{code:000}", Assert.Throws<JsonException>(action).Message);
}
