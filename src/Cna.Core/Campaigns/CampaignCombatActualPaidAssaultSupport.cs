using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatActualPaidAssaultCodec;

namespace Cna.Core.Campaigns;

internal sealed record PaidAssaultFrame(JsonObject Base, JsonObject Prior, JsonObject Selection,
    JsonObject Boundary, JsonObject Cycle, CombatDecisionConfiguration Configuration, JsonObject Content);

/// <summary>Pure native arithmetic and owned World projection. No synthetic Round authority.</summary>
internal static class CampaignCombatActualPaidAssaultSupport
{
    private static readonly string[] Roles = ["attacker", "defender"];
    internal const string ClockPolicy = "sandtable.combat.mandatory-window-clock.v2";
    internal const long MaxUtc = 253402300799999;
    internal static JsonObject O(params (string Key, object? Value)[] fields)
    {
        var result = new JsonObject();
        foreach (var (key, value) in fields) result.Add(key, value is JsonNode node ? node.DeepClone() : JsonSerializer.SerializeToNode(value));
        return result;
    }
    internal static string T(JsonNode node, string key) => node[key]!.GetValue<string>();
    internal static long N(JsonNode node, string key)
    { var value = node[key]!.AsValue(); return value.TryGetValue<long>(out var n) ? n : value.GetValue<int>(); }
    internal static bool B(JsonNode node, string key) => node[key]!.GetValue<bool>();
    internal static string Hash(byte[] data) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(data));
    internal static string Domain(string domain, byte[] data) => CampaignOpeningPreambleCodec.HashWithDomain(domain, data)[7..];
    internal static string PaidDigest(string kind, JsonNode value, string shape)
        => Domain("sandtable.combat.actual-paid-assault." + kind + ".v1", Canonical(value, shape));
    internal static JsonObject Cp(long points) => O(("numerator", points), ("denominator", 1));
    internal static JsonArray A(IEnumerable<JsonNode?> values) => new(values.Select(x => x?.DeepClone()).ToArray());
    internal static byte[] Consume(string path, Func<string, byte[]> bytes)
    {
        Require(Pins.TryGetValue(path, out var expected), 9);
        try { var acquired = bytes(path); Require(acquired is not null, 9); var data = acquired!.ToArray(); Require(Hash(data) == "sha256:" + CombatEvidenceRevisions.Expected(bytes, path, expected!), 9); return data; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException)
        { throw new JsonException("CMB-APA-009", e); }
    }
    internal static Func<string, byte[]> VerifyDependencies(Func<string, byte[]> bytes)
        => CombatEvidenceRevisions.Capture(bytes, Pins, Delivery, int.MaxValue, "CMB-APA-009", revalidateExisting: true);
    internal static string Manifest(bool admission, Func<string, byte[]> dependencies)
    {
        var value = new JsonObject();
        foreach (var path in (admission ? AdmissionPaths.AsEnumerable() : Pins.Keys).Order(StringComparer.Ordinal)) value[path] = CombatEvidenceRevisions.Expected(dependencies, path, Pins[path]);
        var data = Encode(value);
        return admission ? Hash(data) : "sha256:" + Domain("sandtable.combat.actual-paid-assault.dependency-manifest.v1", data);
    }
    internal static JsonObject Facts(int diff, int ac, int dc, int? die)
    {
        var rules = Cna1979CombatAdjudication.Definition;
        var effect = rules.Effects[diff + 2];
        var asum = ac / 10 + ac % 10; var dsum = dc / 10 + dc % 10;
        var ca = effect.AttackerCaptureSums.Contains(asum); var cd = effect.DefenderCaptureSums.Contains(dsum);
        Require(!ca || !cd, 7); var role = ca ? "attacker" : cd ? "defender" : null;
        Require((role is not null) == die.HasValue && (!die.HasValue || die is >= 1 and <= 6), 6);
        return O(("differential", diff), ("attackerCoordinate", ac), ("defenderCoordinate", dc), ("captureDie", die),
            ("attackerPercent", Cna1979CombatAdjudication.LookupLossPercent(CombatRole.Attacker, diff, ac)),
            ("defenderPercent", Cna1979CombatAdjudication.LookupLossPercent(CombatRole.Defender, diff, dc)),
            ("rawEngaged", effect.AttackerEngagedSums.Contains(asum)), ("requiredRetreat", effect.DefenderRetreatOneHexSums.Contains(dsum) ? 1 : 0),
            ("capturedRole", role), ("captureShare", die.HasValue ? rules.CaptureShares[die.Value - 1].Percent : 0));
    }
    internal static IEnumerable<JsonObject> Catalogue()
    {
        var coords = Enumerable.Range(1, 6).SelectMany(a => Enumerable.Range(1, 6).Select(d => 10 * a + d)).ToArray();
        var ordinal = 0;
        foreach (var a in coords) foreach (var d in coords)
            yield return O(("kind", "morale"), ("ordinal", ordinal++), ("attackerMoraleCoordinate", a), ("defenderMoraleCoordinate", d),
                ("attackerAdjustment", Cna1979CombatAdjudication.LookupMoraleAdjustment(a)), ("defenderAdjustment", Cna1979CombatAdjudication.LookupMoraleAdjustment(d)),
                ("differential", Cna1979CombatAdjudication.CalculateDifferential(a, d)));
        for (var diff = -2; diff <= 2; diff++) foreach (var a in coords) foreach (var d in coords)
        {
            var effect = Cna1979CombatAdjudication.Definition.Effects[diff + 2];
            var captured = effect.AttackerCaptureSums.Contains(a / 10 + a % 10) || effect.DefenderCaptureSums.Contains(d / 10 + d % 10);
            var facts = Facts(diff, a, d, captured ? 1 : null);
            var row = O(("kind", "assault-cell"), ("ordinal", ordinal++));
            foreach (var (key, value) in facts) if (key is not "captureDie" and not "captureShare") row[key] = value?.DeepClone();
            row["captureTriggered"] = captured; yield return row;
        }
        for (var diff = -2; diff <= 2; diff++) foreach (var a in coords) foreach (var d in coords)
        {
            var effect = Cna1979CombatAdjudication.Definition.Effects[diff + 2];
            var captured = effect.AttackerCaptureSums.Contains(a / 10 + a % 10) || effect.DefenderCaptureSums.Contains(d / 10 + d % 10);
            foreach (var die in captured ? Enumerable.Range(1, 6).Select(x => (int?)x) : new int?[] { null })
            {
                var f = Facts(diff, a, d, die);
                for (var refusal = 0; refusal <= N(f, "requiredRetreat"); refusal++)
                {
                    var al = (10 * N(f, "attackerPercent") + 99) / 100; var dl = 10 * (N(f, "defenderPercent") + 10 * refusal) / 100;
                    var ca = f["capturedRole"]?.GetValue<string>() == "attacker" ? (al * N(f, "captureShare") + 99) / 100 : 0;
                    var cd = f["capturedRole"]?.GetValue<string>() == "defender" ? (dl * N(f, "captureShare") + 99) / 100 : 0;
                    Require(ca >= 0 && ca <= al && al <= 3 && cd >= 0 && cd <= dl && dl <= 3, 7);
                    yield return O(("kind", "capture-refusal"), ("ordinal", ordinal++), ("differential", diff), ("attackerCoordinate", a),
                        ("defenderCoordinate", d), ("captureDie", die), ("refusalHexes", refusal), ("facts", f),
                        ("attackerLossToe", al), ("defenderLossToe", dl), ("attackerCapturedToe", ca), ("defenderCapturedToe", cd),
                        ("attackerOtherLossToe", al - ca), ("defenderOtherLossToe", dl - cd), ("attackerRemainingToe", 10 - al),
                        ("defenderRemainingToe", 10 - dl), ("attackerLossDp", al >= 3 ? 3 : 0), ("defenderLossDp", dl >= 3 ? 3 : 0));
                }
            }
        }
    }
    internal static (JsonObject World, JsonArray Costs) PaidWorld(PaidAssaultFrame ctx)
    {
        var p = ctx.Prior; var w = p["world"]!.DeepClone().AsObject(); var costs = new JsonArray();
        Require(p["attackHistory"]!.AsArray().Count == 0 && p["targetUses"]!.AsArray().Count == 0 && w["settlements"]!.AsArray().Count == 0 && w["custodyLots"]!.AsArray().Count == 0, 7);
        foreach (var (role, cost) in new[] { ("attacker", 5), ("defender", 3) })
        {
            var allocation = p["slots"]!.AsArray().Single(x => T(x!, "role") == role)!["allocation"]!;
            var unit = ctx.Selection[role]!["unit"]!;
            Require(JsonNode.DeepEquals(allocation["unit"], unit) && N(allocation, "committedToe") == 10, 7);
            var e = w["elements"]!.AsArray().Single(x => T(x!, "elementId") == T(unit, "elementId"))!;
            var cp = e["operationalState"]!["capabilityPointsExpended"]!;
            Require(N(cp, "denominator") == 1 && N(cp, "numerator") == 0 && N(e["ammunition"]!, "points") == 10 && N(e["components"]![0]!, "currentToe") == 10, 7);
            e["operationalState"]!["capabilityPointsExpended"] = Cp(cost); e["ammunition"]!["points"] = 0;
            costs.Add(O(("unit", unit), ("beforeCp", 0), ("afterCp", cost), ("beforeAmmo", 10), ("afterAmmo", 0)));
        }
        return (w, costs);
    }
    internal static JsonObject Resolve(JsonObject state)
    {
        var before = state["randomState"]!.AsObject();
        var current = new RandomStreamState(1, T(before, "algorithmId"), before["seed"]!.GetValue<ulong>(), before["nextByteCursor"]!.GetValue<ulong>());
        var draws = new List<CombatResolutionDraw>(); var dice = new List<int>();
        void Draw(string purpose)
        {
            var start = current.NextByteCursor; var consumed = new List<int>();
            while (true)
            {
                Require(consumed.Count < 512, 7); var next = SandtableRandom.NextByte(current); current = next.State; consumed.Add(next.Value);
                if (next.Value >= 252) continue;
                var die = next.Value % 6 + 1; dice.Add(die); draws.Add(new(purpose, start, current.NextByteCursor, die, consumed)); return;
            }
        }
        try
        {
            var rules = Cna1979CombatAdjudication.Definition;
            foreach (var purpose in rules.Procedure.OrderedPurposes) Draw(purpose);
            Require(dice.Count == 8, 7);
            var am = 10 * dice[0] + dice[1]; var dm = 10 * dice[2] + dice[3]; var ac = 10 * dice[4] + dice[5]; var dc = 10 * dice[6] + dice[7];
            var diff = Cna1979CombatAdjudication.CalculateDifferential(am, dm); var effect = rules.Effects[diff + 2];
            var role = effect.AttackerCaptureSums.Contains(dice[4] + dice[5]) ? "attacker" : effect.DefenderCaptureSums.Contains(dice[6] + dice[7]) ? "defender" : null;
            if (role is not null) Draw(role + ".capture.share"); int? die = role is null ? null : dice[^1];
            var f = Facts(diff, ac, dc, die);
            var facts = new CampaignCombatResultFacts(diff, ac, dc, die, (int)N(f, "attackerPercent"), (int)N(f, "defenderPercent"), B(f, "rawEngaged"), (int)N(f, "requiredRetreat"), role, (int)N(f, "captureShare"));
            var result = new CombatAssaultResult(T(state, "commitmentId"), Cna1979CombatAdjudication.ContentHash, rules.Procedure.ProcedureId,
                new RandomStreamState(1, T(before, "algorithmId"), before["seed"]!.GetValue<ulong>(), before["nextByteCursor"]!.GetValue<ulong>()), current,
                draws, am, dm, Cna1979CombatAdjudication.LookupMoraleAdjustment(am), Cna1979CombatAdjudication.LookupMoraleAdjustment(dm), facts);
            var value = JsonNode.Parse(CampaignCombatResolutionCodec.SerializeResultPreimage(result))!.AsObject();
            var output = O(("resultId", result.ResultId)); foreach (var (key, child) in value) output[key] = child?.DeepClone();
            _ = Canonical(output, "AssaultResult"); return output;
        }
        catch (OverflowException e) { throw new JsonException("CMB-APA-007", e); }
    }
    internal static string[] Route(PaidAssaultFrame ctx, string origin, string destination)
    {
        var queue = new Queue<string[]>(); queue.Enqueue([origin]); var seen = new HashSet<string>(StringComparer.Ordinal) { origin };
        while (queue.TryDequeue(out var path))
        {
            if (path[^1] == destination) return path;
            foreach (var other in Neighbors(ctx, path[^1]).Order(StringComparer.Ordinal))
                if (seen.Add(other)) queue.Enqueue([.. path, other]);
        }
        throw new JsonException("CMB-APA-007");
    }
    private static IEnumerable<string> Neighbors(PaidAssaultFrame ctx, string location) => ctx.Content["edges"]!.AsArray()
        .Where(x => T(x!, "firstLocationId") == location || T(x!, "secondLocationId") == location)
        .Select(x => T(x!, T(x!, "firstLocationId") == location ? "secondLocationId" : "firstLocationId"));
    private static JsonObject Component(PaidAssaultFrame ctx, JsonNode element)
    {
        var unit = Roles.Select(r => ctx.Selection[r]!["unit"]!)
            .Single(u => T(u, "elementId") == T(element, "elementId"));
        return O(("unit", unit), ("componentId", element["components"]![0]!["componentId"]));
    }
    internal static JsonObject Project(PaidAssaultFrame ctx, JsonObject paid, JsonObject result, string cut,
        string? retreatChoice = null, string? custodyChoice = null)
    {
        var world = paid.DeepClone().AsObject();
        var sid = "set." + Domain("sandtable.combat.settlement.v2", Encode(O(("commitmentId", result["commitmentId"]), ("resultId", result["resultId"]))));
        var facts = result["facts"]!; var scope = O(("gameTurn", ctx.Cycle["gameTurn"]), ("operationStage", ctx.Cycle["operationStage"]));
        var elements = world["elements"]!.AsArray().ToDictionary(x => T(x!, "elementId"), x => x!, StringComparer.Ordinal);
        var attacker = elements[T(ctx.Selection["attacker"]!["unit"]!, "elementId")];
        var defender = elements[T(ctx.Selection["defender"]!["unit"]!, "elementId")];
        var frozen = world["elements"]!.DeepClone();
        var frozenById = frozen.AsArray().ToDictionary(x => T(x!, "elementId"), x => x!, StringComparer.Ordinal);
        var st = O(("settlementId", sid), ("commitmentId", result["commitmentId"]), ("resultId", result["resultId"]),
            ("gameTurn", scope["gameTurn"]), ("operationStage", scope["operationStage"]), ("attacker", ctx.Selection["attacker"]!["unit"]),
            ("defender", ctx.Selection["defender"]!["unit"]), ("preLossElements", frozen), ("result", facts),
            ("disposition", null), ("losses", null), ("retreat", null), ("custody", null), ("relationships", null));
        Require(world["settlements"]!.AsArray().Count == 0, 7); world["settlements"]!.AsArray().Add(st);
        if (cut == "resolved") return world;
        var required = N(facts, "requiredRetreat"); var choice = retreatChoice ?? "not-required";
        Require(required > 0 ? choice is "retreat" or "refuse-retreat" : choice == "not-required", 6);
        var origin = T(defender, "currentLocationId"); var planned = choice == "retreat" ? 1 : 0;
        var anchor = ctx.Content["scenarios"]![0]!["retreatSupplyAnchors"]!.AsArray().Single(x => T(x!, "sideId") == T(st["defender"]!, "originalSide"))!;
        var path = planned > 0 ? Route(ctx, origin, T(anchor, "locationId")).Take(2).ToArray() : [origin];
        Require(planned == 0 || path.Length == 2, 7);
        st["disposition"] = O(("receiptId", sid + ".disposition"), ("predecessorReceiptId", result["resultId"]), ("kind", choice),
            ("requiredDistance", required), ("plannedDistance", planned), ("unfulfilledDistance", required - planned), ("route", path));
        if (cut == "disposition") return world;
        void Cause(JsonNode element, string receipt, string kind, long points)
        {
            if (points == 0) return;
            var before = N(element["operationalState"]!, "cohesionLevel");
            var after = kind == "assault-victory-rp" ? Math.Min(10, before + points) : before - points;
            Require(after is >= int.MinValue and <= int.MaxValue, 7); element["operationalState"]!["cohesionLevel"] = after;
            var side = T(Component(ctx, element)["unit"]!, "originalSide");
            world["cohesionCauses"]!.AsArray().Add(O(("causeId", sid + "." + kind + "." + side), ("ordinal", world["cohesionCauses"]!.AsArray().Count + 1),
                ("receiptId", receipt), ("elementId", element["elementId"]), ("gameTurn", scope["gameTurn"]), ("operationStage", scope["operationStage"]),
                ("kind", kind), ("points", points), ("before", before), ("after", after)));
        }
        var roles = new JsonArray();
        foreach (var (role, element) in new[] { ("attacker", attacker), ("defender", defender) })
        {
            var toe = N(frozenById[T(element, "elementId")]["components"]![0]!, "currentToe"); Require(toe == 10, 7);
            var percent = N(facts, role + "Percent"); var refusal = role == "defender" ? 10 * (required - planned) : 0;
            var loss = role == "attacker" ? (toe * (percent + refusal) + 99) / 100 : toe * (percent + refusal) / 100;
            var captured = facts["capturedRole"]?.GetValue<string>() == role ? (loss * N(facts, "captureShare") + 99) / 100 : 0;
            Require(captured >= 0 && captured <= loss && loss <= toe, 7);
            roles.Add(O(("role", role), ("component", Component(ctx, element)), ("committedToe", toe), ("tablePercent", percent),
                ("refusalPercent", refusal), ("lossToe", loss), ("capturedToe", captured), ("otherLossToe", loss - captured), ("remainingToe", toe - loss), ("lossDp", loss >= 3 ? 3 : 0)));
            element["components"]![0]!["currentToe"] = toe - loss; Cause(element, sid + ".losses", "loss-dp", loss >= 3 ? 3 : 0);
        }
        st["losses"] = O(("receiptId", sid + ".losses"), ("predecessorReceiptId", sid + ".disposition"), ("roles", roles));
        var capturedRole = roles.FirstOrDefault(x => N(x!, "capturedToe") > 0);
        JsonNode? victim = null, captor = null; JsonObject? lot = null;
        if (capturedRole is not null)
        {
            victim = T(capturedRole, "role") == "attacker" ? attacker : defender; captor = ReferenceEquals(victim, attacker) ? defender : attacker;
            var location = frozenById[T(victim, "elementId")]["currentLocationId"];
            lot = O(("lotId", sid + ".captives"), ("lossReceiptId", sid + ".losses"), ("originalComponent", Component(ctx, victim)),
                ("captor", Component(ctx, captor)["unit"]), ("quantity", capturedRole["capturedToe"]), ("originLocationId", location),
                ("currentLocationId", location), ("status", "pending"), ("guardId", null), ("escapeReceiptId", null));
            world["custodyLots"]!.AsArray().Add(lot);
        }
        if (cut == "losses") return world;
        var cp = defender["operationalState"]!["capabilityPointsExpended"]!; Require(N(cp, "denominator") == 1, 7);
        var beforeCp = N(cp, "numerator"); Require(beforeCp <= long.MaxValue - planned, 7); var afterCp = beforeCp + planned;
        var excess = Math.Max(0, afterCp - 10) - Math.Max(0, beforeCp - 10);
        defender["operationalState"]!["capabilityPointsExpended"] = Cp(afterCp); defender["currentLocationId"] = path[^1];
        foreach (var representation in world["representations"]!.AsArray())
            if (representation!["boundElementIds"]!.AsArray().Count == 1 && representation["boundElementIds"]![0]!.GetValue<string>() == T(defender, "elementId")) representation["currentLocationId"] = path[^1];
        Cause(defender, sid + ".retreat", "retreat-excess-dp", excess); Cause(attacker, sid + ".retreat", "assault-victory-rp", planned > 0 ? 3 : 0);
        st["retreat"] = O(("receiptId", sid + ".retreat"), ("predecessorReceiptId", sid + ".losses"), ("kind", choice), ("route", path),
            ("completedDistance", planned), ("beforeCp", Cp(beforeCp)), ("afterCp", Cp(afterCp)), ("excessCpDp", excess), ("attackerVictoryRp", planned > 0 ? 3 : 0));
        if (cut == "retreat") return world;
        if (capturedRole is not null)
        {
            Require(custodyChoice is "relocate-and-guard" or "leave-unguarded", 6);
            var guarded = custodyChoice == "relocate-and-guard"; var destination = T(guarded ? captor! : victim!, "currentLocationId");
            var custodyRoute = Route(ctx, T(lot!, "originLocationId"), destination); Require(custodyRoute.Length - 1 <= (guarded ? 3 : 8), 7);
            var donor = N(captor!["components"]![0]!, "currentToe");
            if (guarded)
            {
                Require(donor >= 1 && !custodyRoute.Skip(1).Contains(T(victim!, "currentLocationId"), StringComparer.Ordinal), 7);
                captor["components"]![0]!["currentToe"] = donor - 1;
                var guard = O(("guardId", sid + ".guard"), ("formationReceiptId", sid + ".custody"), ("lotId", lot!["lotId"]),
                    ("originComponent", Component(ctx, captor)), ("currentLocationId", destination), ("toe", 1), ("baseCapabilityPointAllowance", 10),
                    ("offensiveCloseAssaultRating", 0), ("defensiveCloseAssaultRating", 1), ("operationalState", captor["operationalState"]),
                    ("ammunition", captor["ammunition"]), ("readiness", captor["readiness"]));
                world["guards"]!.AsArray().Add(guard); lot["status"] = "guarded"; lot["currentLocationId"] = destination; lot["guardId"] = sid + ".guard";
                world["futureObligations"]!.AsArray().Add(O(("obligationId", sid + ".upkeep"), ("receiptId", sid + ".custody"), ("kind", "guard-priority-upkeep"),
                    ("subjectId", guard["guardId"]), ("earnedScope", scope), ("eligibleScope", null), ("activationGate", "before-prisoner-upkeep-or-guard-action"), ("status", "retained-unimplemented")));
            }
            else
            {
                var ordinal = (N(scope, "gameTurn") - 1) * 3 + N(scope, "operationStage") - 1 + 12;
                var eligible = O(("gameTurn", ordinal / 3 + 1), ("operationStage", ordinal % 3 + 1));
                var entitlement = O(("entitlementId", sid + ".replacement"), ("escapeReceiptId", sid + ".custody"), ("lotId", lot!["lotId"]),
                    ("originalComponent", Component(ctx, victim!)), ("quantity", lot["quantity"]), ("reunionLocationId", destination), ("earnedScope", scope),
                    ("delayOperationStages", 12), ("eligibleScope", eligible), ("status", "awaiting-eligibility-and-training"));
                world["replacementEntitlements"]!.AsArray().Add(entitlement); lot["status"] = "escaped"; lot["currentLocationId"] = null; lot["escapeReceiptId"] = sid + ".custody";
                world["futureObligations"]!.AsArray().Add(O(("obligationId", sid + ".training"), ("receiptId", sid + ".custody"), ("kind", "replacement-training-gate"),
                    ("subjectId", entitlement["entitlementId"]), ("earnedScope", scope), ("eligibleScope", eligible), ("activationGate", "before-replacement-eligibility-training"), ("status", "retained-unimplemented")));
            }
            st["custody"] = O(("receiptId", sid + ".custody"), ("predecessorReceiptId", sid + ".retreat"), ("lotId", lot!["lotId"]), ("kind", custodyChoice),
                ("route", custodyRoute), ("guardId", guarded ? sid + ".guard" : null), ("entitlementId", guarded ? null : sid + ".replacement"),
                ("donorToeBefore", donor), ("donorToeAfter", captor["components"]![0]!["currentToe"]));
        }
        else Require(custodyChoice is null && cut != "custody", 6);
        if (cut == "custody") return world;
        var adjacent = Neighbors(ctx, T(attacker, "currentLocationId")).Contains(T(defender, "currentLocationId"), StringComparer.Ordinal);
        var relation = adjacent && N(attacker["components"]![0]!, "currentToe") > 0 && N(defender["components"]![0]!, "currentToe") > 0
            ? B(facts, "rawEngaged") && required == 0 ? "engaged" : "contact" : null;
        if (relation is not null) world["relationships"]!.AsArray().Add(O(("relationId", sid + ".relation"), ("creationReceiptId", sid + ".relationships"),
            ("kind", relation), ("attacker", st["attacker"]), ("defender", st["defender"]), ("gameTurn", scope["gameTurn"]), ("operationStage", scope["operationStage"]),
            ("active", true), ("endedByReceiptId", null), ("endingCause", null)));
        st["relationships"] = O(("receiptId", sid + ".relationships"), ("predecessorReceiptId", sid + (capturedRole is not null ? ".custody" : ".retreat")),
            ("relationshipId", relation is not null ? sid + ".relation" : null), ("kind", relation));
        return world;
    }
    internal static JsonObject Projected(PaidAssaultFrame ctx, JsonObject state, string cut, string? retreatChoice = null, string? custodyChoice = null)
    {
        var settlements = state["world"]!["settlements"]!.AsArray(); var st = settlements.Count > 0 ? settlements[0] : null;
        retreatChoice ??= st?["disposition"]?["kind"]?.GetValue<string>(); custodyChoice ??= st?["custody"]?["kind"]?.GetValue<string>();
        return Project(ctx, PaidWorld(ctx).World, state["result"]!.AsObject(), cut, retreatChoice, custodyChoice);
    }
    internal static JsonObject? MakeWindow(PaidAssaultFrame ctx, string kind, string owner, string sid, long? now, bool available)
    {
        if (!available || !now.HasValue) return null;
        var budget = ctx.Configuration.Windows.Single(x => x.Kind == kind).DecisionBudgetMilliseconds;
        if (now > MaxUtc - budget) return null;
        var timing = O(("contractVersion", 1), ("configHash", ctx.Cycle["admittedPolicyBundleDigest"]), ("kind", kind), ("decisionBudgetMilliseconds", budget),
            ("openedAtUnixMilliseconds", now), ("deadlineUnixMilliseconds", now + budget), ("highWaterUnixMilliseconds", now));
        var window = O(("decisionId", sid + ".choice." + kind), ("kind", kind), ("owner", owner), ("timing", timing)); _ = Canonical(window, "ChoiceWindow"); return window;
    }
    internal static void CertifySupport(PaidAssaultFrame ctx, IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? capacities = null, IEnumerable<JsonObject>? records = null)
    {
        Require(new HashSet<string>(RequiredHandlers, StringComparer.Ordinal).SetEquals(handlers ?? SupportedHandlers()), 7);
        var limits = capacities ?? SupportCapacities();
        foreach (var (kind, bound) in RequiredCapacities) Require(limits.TryGetValue(kind, out var capacity) && capacity >= bound, 7);
        var paid = PaidWorld(ctx).World; var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var summaries = new Dictionary<string, JsonObject>(StringComparer.Ordinal); var ordinal = 0;
        using var supplied = (records ?? Catalogue()).GetEnumerator();
        foreach (var expected in Catalogue())
        {
            Require(supplied.MoveNext() && JsonNode.DeepEquals(supplied.Current, expected), 7); var row = supplied.Current;
            Require(N(row, "ordinal") == ordinal++, 7); var kind = T(row, "kind"); counts[kind] = counts.GetValueOrDefault(kind) + 1;
            _ = Canonical(row, kind == "morale" ? "MoraleRow" : kind == "assault-cell" ? "AssaultCell" : "VariantRow");
            if (kind != "capture-refusal") continue;
            var facts = row["facts"]!; var key = O(("attackerPercent", facts["attackerPercent"]), ("defenderPercent", facts["defenderPercent"]),
                ("rawEngaged", facts["rawEngaged"]), ("requiredRetreat", facts["requiredRetreat"]), ("capturedRole", facts["capturedRole"]),
                ("captureShare", facts["captureShare"]), ("refusalHexes", row["refusalHexes"]));
            summaries[Encoding.ASCII.GetString(Encode(key))] = row;
        }
        Require(!supplied.MoveNext(), 7);
        Require(ordinal == 16616 && counts["morale"] == 1296 && counts["assault-cell"] == 6480 && counts["capture-refusal"] == 8840, 7);
        foreach (var row in summaries.Values)
        {
            var f = row["facts"]!; var retreat = N(f, "requiredRetreat") == 0 ? "not-required" : N(row, "refusalHexes") > 0 ? "refuse-retreat" : "retreat";
            var captured = N(row, "attackerCapturedToe") + N(row, "defenderCapturedToe") > 0;
            var result = O(("resultId", "res." + new string('f', 64)), ("commitmentId", "cmt." + new string('f', 64)), ("facts", f));
            foreach (var custody in captured ? new[] { "relocate-and-guard", "leave-unguarded" } : new string?[] { null })
            {
                JsonObject? world = null;
                foreach (var cut in new[] { "resolved", "disposition", "losses", "retreat", "custody", "relationships" })
                {
                    if (cut == "custody" && !captured) continue;
                    world = Project(ctx, paid, result, cut, retreat, custody); _ = Canonical(world, "World");
                }
                Require(world!["elements"]!.AsArray().Sum(x => N(x!["components"]![0]!, "currentToe")) + world["guards"]!.AsArray().Sum(x => N(x!, "toe")) + N(row, "attackerLossToe") + N(row, "defenderLossToe") == 20, 7);
            }
        }
        foreach (var kind in new[] { "retreat", "custody" }) foreach (var owner in new[] { "axis", "commonwealth" })
        {
            Require(ctx.Configuration.Windows.Single(x => x.Kind == kind).Fallback == (kind == "retreat" ? "refuse-retreat" : "leave-unguarded"), 7);
            Require(MakeWindow(ctx, kind, owner, "set." + new string('f', 64), 10000, true) is not null && MakeWindow(ctx, kind, owner, "set." + new string('f', 64), null, true) is null && MakeWindow(ctx, kind, owner, "set." + new string('f', 64), MaxUtc, true) is null, 7);
        }
        Require(ctx.Prior["randomState"]!["seed"]!.GetValue<ulong>() == 1 && ctx.Prior["randomState"]!["nextByteCursor"]!.GetValue<ulong>() == 2, 7);
    }
    // Native dispatch capability is fixed in production. Optional copies permit fail-closed
    // sensitivity tests without mutable global flags or changing an authentic source.
    private static readonly string[] RequiredHandlers = ["commit", "resolve", "disposition", "losses", "retreat", "custody-guard", "custody-escape", "relationships", "round-close", "ca-complete", "retreat-window", "custody-window", "owner-choice", "expiry", "controller-unavailable", "opening-fallback", "choice-clock-fallback", "retry", "stale-timer"];
    private static readonly Dictionary<string, int> RequiredCapacities = new(StringComparer.Ordinal)
    {
        ["PaidInput"] = 956,
        ["PaidEvent"] = 26552,
        ["PaidControl"] = 224827,
        ["PaidProof"] = 1273983,
        ["AssaultResult"] = 23656,
        ["World"] = 192965,
        ["PaidSource"] = 2681411,
        ["PaidLedger"] = 10528,
        ["acceptedEvents"] = 11,
        ["acceptedReceipts"] = 11,
        ["draws"] = 9,
        ["consumedBytesPerDraw"] = 512,
        ["routes"] = 9,
    };
    internal static HashSet<string> SupportedHandlers() => new(RequiredHandlers, StringComparer.Ordinal);
    internal static Dictionary<string, int> SupportCapacities() => new(StringComparer.Ordinal)
    {
        ["PaidInput"] = ByteLimit("PaidInput"),
        ["PaidEvent"] = ByteLimit("PaidEvent"),
        ["PaidControl"] = ByteLimit("PaidControl"),
        ["PaidProof"] = ByteLimit("PaidProof"),
        ["AssaultResult"] = ByteLimit("AssaultResult"),
        ["World"] = ByteLimit("World"),
        ["PaidSource"] = ByteLimit("PaidSource"),
        ["PaidLedger"] = 16384,
        ["acceptedEvents"] = 11,
        ["acceptedReceipts"] = 11,
        ["draws"] = 9,
        ["consumedBytesPerDraw"] = 512,
        ["routes"] = 9,
    };
    private static readonly Dictionary<string, string> Pins = new(StringComparer.Ordinal)
    {
        ["docs/research/fixtures/combat-selected-source-v1.json"] = "e225bc650c4bb942e41245a33da20c21c576ab117807a06805e2558a2c6468df",
        ["docs/research/verify-combat-rng.py"] = "f3c7c468f2be0efa583e44c4d04b8fe6d20eef51aae01554bfa535b0ecc0ea30",
        ["docs/research/verify-combat-source-freeze.py"] = "f21a97150e344337f6ee4c2c1a1752ac7d41b8b8c8bf6a61a06a1fe5a0483e7e",
        ["docs/specs/combat-actual-assault-v1.schema.json"] = "a16d852d5a4463a295e315c824e5298f4ff22b979a7c307187c2a8d926136f0d",
        ["docs/specs/combat-actual-round-entry-v1.schema.json"] = "677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8",
        ["docs/specs/combat-actual-selection-v1.schema.json"] = "da6256deb94bb6061e8e98e2448f915c4474373b7f0de6bb76eded2ae88c39b1",
        ["docs/specs/combat-authority-envelope-v1.schema.json"] = "91f90ba47cdfff088b3d8e5dba55c6c283d41c96381e56c7bba115bfb5390550",
        ["docs/specs/combat-cycle-control-v1.schema.json"] = "8c517db6daa734d415bd1d474280c2cf6e65013490c4a2568795d50e6112f7da",
        ["docs/specs/combat-cycle-sequence-v1.schema.json"] = "6e09dc5bc6ca79d8580db6ed1e76ad5007c26a30df11b4dd9da0a9c7b5f5e0c5",
        ["docs/specs/combat-inherited-breakdown-completion-v1.schema.json"] = "1ff841c704e1aa46e71513ead562a0ca1c54c05b35bdf97018d6854d8773fec3",
        ["docs/specs/combat-inherited-movement-lifecycle-v1.schema.json"] = "aac4e07f9546d457defd9042ae701434683e7c22fab2071fe88f872d8d6bc072",
        ["docs/specs/combat-inherited-movement-v1.schema.json"] = "a9ad67ee9b9302f17582fb5087f6dd6e7e62574741e6cea7b5a3e58bf7fe2069",
        ["docs/specs/combat-inherited-successors-v1.schema.json"] = "474d8b5b22457921082c1799c89a42d9ec448b5c6ea131a3458f364ff980babc",
        ["docs/specs/combat-opening-preamble-v1.schema.json"] = "d01bd4eb60ce3a83a4d119631ac72ccce082ff13cdf92f861d42fb83566e7d59",
        ["docs/specs/combat-ordinary-movement-v1.schema.json"] = "039d5f3cdb908dc761540b4809bfb9c6e77c9ab6efd8c965e69955fbbcf0728a",
        ["docs/specs/combat-positive-entry-v1.schema.json"] = "73724384ba3cf61b462ef7b47ae70e5108281a6bbaf0b80878c55247e4e6fa36",
        ["docs/specs/combat-reserve-designation-v1.schema.json"] = "f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9",
        ["docs/specs/combat-reserve-release-v1.schema.json"] = "10ba1ca46e05fbed52c6eef5174c2ac84c4282df0957c44eab6e86079342bc32",
        ["docs/specs/combat-result-settlement-v1.schema.json"] = "f65c13687bb2fe3fbabdbd82d48298430330dab756583fbfda17dae50bc73ab0",
        ["docs/specs/combat-result-settlement-v2.schema.json"] = "719634c4720b4a92669b2e83e60df58cd32d0e556ec9ef66c546aed1f9c7e9d6",
        ["docs/specs/combat-rules-inputs-v1.schema.json"] = "1477a9e755a091bbf4539d00184711b18eab2f6289f643596d841b8c314f4797",
        ["docs/specs/combat-sealed-round-v1.schema.json"] = "5556404caa296ce41020654f8fb64c0349fd13051edabf6b4934e47d675c32db",
        ["docs/specs/combat-selection-steps-v1.schema.json"] = "ef24e125012ab390bb0c932a09472b8220d7e8c95c1b19c29bdb611be532162d",
        ["docs/specs/combat-stage-entry-v1.schema.json"] = "5d0f85fec61f219e7fd3575037894c147580ec9fe1570603d621c9d76b38889e",
        ["docs/specs/combat-weather-v1.schema.json"] = "44f2fa44af30bf723ecac88db08fc9efac85e9880624635fc73f4dc52bd9459f",
        ["docs/specs/combat-world-settlement-v1.schema.json"] = "f892545f5dac693c55d1180ad771d95ffeec508176a0d6139b82752ed46bcf0e",
        ["docs/specs/fixtures/combat-actual-selection-v1.json"] = "019d1a3ff0f121d83f377ddfb19d274b4a8aa89172bad8aeb289c3b98228e604",
        ["docs/specs/fixtures/combat-authority-envelope-v1.json"] = "adf7b05e15f863b08f366809adf91c97b39480e404ce2efff15e838fa4043eaa",
        ["docs/specs/fixtures/combat-content-v7.canonical.json"] = "ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
        ["docs/specs/fixtures/combat-creation-ledger-v1.json"] = "4b7f87f73a5882800e4e4befb4c06c20bf19c419f92edba5028b355e37442684",
        ["docs/specs/fixtures/combat-cycle-sequence-v1.json"] = "de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f",
        ["docs/specs/fixtures/combat-positive-entry-v1.json"] = "eb59146f34bfdc47753b9f0aff7f40709eba4266aff522655ac6886be15b951e",
        ["docs/specs/fixtures/combat-reserve-designation-v1.json"] = "6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2",
        ["docs/specs/fixtures/combat-rules-inputs-v1.json"] = "5401cd9a690be8b81f19795f1f66c0c6fe24f5055a06cda5facd6193b3e0a29a",
        ["docs/specs/verify-combat-actual-assault-v1.py"] = "1ab27601eb209f48febc440ad0f1cfebd08155b9a747e06c003f11a1a16e64a9",
        ["docs/specs/verify-combat-actual-round-entry-v1.py"] = "2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c",
        ["docs/specs/verify-combat-actual-selection-v1.py"] = "f37a7cfa26b60469168d9f4424465b14f1fe45666f4222d69c840c2527d2e66f",
        ["docs/specs/verify-combat-authority-envelope-v1.py"] = "a12d8ce1ab142b9a7f1c58e07e5b0d7928c4693d869423aa24ade81cb21e6231",
        ["docs/specs/verify-combat-content-v7.py"] = "eaab770988e8d2b0f11cb9a8a39b45f961977aa9048f95de8bf32f46e361bcbd",
        ["docs/specs/verify-combat-creation-ledger-v1.py"] = "04777d27347b943cbb4ec06efeee6f3006694d1acd88b9bc8e25eb5dcb5841e4",
        ["docs/specs/verify-combat-cycle-control-v1.py"] = "9039f9e1067b2cdd2e0a57ae16e711350b4bbdceb500d22bbbd715f39e8ccc9f",
        ["docs/specs/verify-combat-cycle-sequence-v1.py"] = "d43ae974e1f6e2c30067efe4635df655c6d2e4791968ece00dfd473145c3cc8a",
        ["docs/specs/verify-combat-inherited-breakdown-completion-v1.py"] = "db903e019bb457c02930876e42a70b9204387494012419b0b97572c3c9e12eba",
        ["docs/specs/verify-combat-inherited-movement-lifecycle-v1.py"] = "f2ee2292df3fe370ace289dcd01747153dac6d78b82c924b2f3da27df1d81920",
        ["docs/specs/verify-combat-inherited-movement-v1.py"] = "c10c51cd97fca3f3f04fc0983e90d474a35a6b58f4a2cce396272916910579e1",
        ["docs/specs/verify-combat-inherited-selection-v1.py"] = "99e0cdb1cc78089e4af93b8e995ba04483f6bdab86edaf7c6e2e251442cd0bb7",
        ["docs/specs/verify-combat-inherited-successors-v1.py"] = "f9cbf73f2045c304c96fc8dcf4d09ba10026db87ff5446c751ad22e19e4fe5de",
        ["docs/specs/verify-combat-opening-preamble-v1.py"] = "50f75c1032f5b17000accb288107a1e047940cadedd5c8d30c469db167c0e5d0",
        ["docs/specs/verify-combat-ordinary-movement-v1.py"] = "cc75e52a6d160d4b0c6c880dc43bc65742068f83ef85cc7fb4babbf8756e0dff",
        ["docs/specs/verify-combat-positive-entry-v1.py"] = "b461a8bc6a194def5f03767783b49b5797a5630b6a7c26a986f7bfe2ee97c172",
        ["docs/specs/verify-combat-reserve-designation-v1.py"] = "fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465",
        ["docs/specs/verify-combat-reserve-release-v1.py"] = "105ef18d9db6364ce42f9831afd3c33010fccc71b0ae60af78cec7c185da892b",
        ["docs/specs/verify-combat-result-settlement-v1.py"] = "1f7dff5b87ce036c406562686f4dc1ce983a4a8dfa15b2401eae72a0d9879ffd",
        ["docs/specs/verify-combat-result-settlement-v2.py"] = "a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2",
        ["docs/specs/verify-combat-rules-inputs-v1.py"] = "ffb6daf98db7097d307ac7c185bf7d68a2ff1d0ac9a357ea063a746aab42081d",
        ["docs/specs/verify-combat-sealed-round-v1.py"] = "497e295e71c4b4da43539ee9b6c30de6d33f409a2f14b6cc24d9030390525ae0",
        ["docs/specs/verify-combat-sealed-round-v2.py"] = "d1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9",
        ["docs/specs/verify-combat-selection-steps-v1.py"] = "dc037282f74c34246d350faebeea55e81677d27dd181de858b8270e4eaa73c86",
        ["docs/specs/verify-combat-stage-entry-v1.py"] = "85cd2b32f9ad104ec29034180775e1ac1905e5aa7f78caa3c1a26e6675901435",
        ["docs/specs/verify-combat-weather-v1.py"] = "c5f92de66c8c341553fe204a5ec1b0acffb162e61ee90332c14fd608f986ade8",
        ["docs/specs/verify-combat-world-settlement-v1.py"] = "d5f49e5a4165ec568f0a8896ea491341a639da256bcfd573998f529e80ef9f25",
        ["tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.weather-tables.v1.golden.json"] = "7f134b9c5dcc356d48a9dfc51976a166b60f02fa93e73b4c0ccfdd354380c182",
    };
    private static readonly string[] AdmissionPaths =
    [
        "docs/research/fixtures/combat-selected-source-v1.json",
        "docs/research/verify-combat-rng.py",
        "docs/research/verify-combat-source-freeze.py",
        "docs/specs/combat-actual-round-entry-v1.schema.json",
        "docs/specs/combat-actual-selection-v1.schema.json",
        "docs/specs/combat-authority-envelope-v1.schema.json",
        "docs/specs/combat-cycle-control-v1.schema.json",
        "docs/specs/combat-cycle-sequence-v1.schema.json",
        "docs/specs/combat-inherited-breakdown-completion-v1.schema.json",
        "docs/specs/combat-inherited-movement-lifecycle-v1.schema.json",
        "docs/specs/combat-inherited-movement-v1.schema.json",
        "docs/specs/combat-inherited-successors-v1.schema.json",
        "docs/specs/combat-opening-preamble-v1.schema.json",
        "docs/specs/combat-ordinary-movement-v1.schema.json",
        "docs/specs/combat-positive-entry-v1.schema.json",
        "docs/specs/combat-reserve-designation-v1.schema.json",
        "docs/specs/combat-reserve-release-v1.schema.json",
        "docs/specs/combat-result-settlement-v1.schema.json",
        "docs/specs/combat-rules-inputs-v1.schema.json",
        "docs/specs/combat-sealed-round-v1.schema.json",
        "docs/specs/combat-selection-steps-v1.schema.json",
        "docs/specs/combat-stage-entry-v1.schema.json",
        "docs/specs/combat-weather-v1.schema.json",
        "docs/specs/combat-world-settlement-v1.schema.json",
        "docs/specs/fixtures/combat-actual-selection-v1.json",
        "docs/specs/fixtures/combat-authority-envelope-v1.json",
        "docs/specs/fixtures/combat-content-v7.canonical.json",
        "docs/specs/fixtures/combat-creation-ledger-v1.json",
        "docs/specs/fixtures/combat-cycle-sequence-v1.json",
        "docs/specs/fixtures/combat-positive-entry-v1.json",
        "docs/specs/fixtures/combat-reserve-designation-v1.json",
        "docs/specs/fixtures/combat-rules-inputs-v1.json",
        "docs/specs/verify-combat-actual-round-entry-v1.py",
        "docs/specs/verify-combat-actual-selection-v1.py",
        "docs/specs/verify-combat-authority-envelope-v1.py",
        "docs/specs/verify-combat-content-v7.py",
        "docs/specs/verify-combat-creation-ledger-v1.py",
        "docs/specs/verify-combat-cycle-control-v1.py",
        "docs/specs/verify-combat-cycle-sequence-v1.py",
        "docs/specs/verify-combat-inherited-breakdown-completion-v1.py",
        "docs/specs/verify-combat-inherited-movement-lifecycle-v1.py",
        "docs/specs/verify-combat-inherited-movement-v1.py",
        "docs/specs/verify-combat-inherited-selection-v1.py",
        "docs/specs/verify-combat-inherited-successors-v1.py",
        "docs/specs/verify-combat-opening-preamble-v1.py",
        "docs/specs/verify-combat-ordinary-movement-v1.py",
        "docs/specs/verify-combat-positive-entry-v1.py",
        "docs/specs/verify-combat-reserve-designation-v1.py",
        "docs/specs/verify-combat-reserve-release-v1.py",
        "docs/specs/verify-combat-result-settlement-v1.py",
        "docs/specs/verify-combat-result-settlement-v2.py",
        "docs/specs/verify-combat-rules-inputs-v1.py",
        "docs/specs/verify-combat-sealed-round-v1.py",
        "docs/specs/verify-combat-sealed-round-v2.py",
        "docs/specs/verify-combat-selection-steps-v1.py",
        "docs/specs/verify-combat-stage-entry-v1.py",
        "docs/specs/verify-combat-weather-v1.py",
        "docs/specs/verify-combat-world-settlement-v1.py",
        "tests/Cna.Core.Tests/Rules/Fixtures/cna-1979.1.weather-tables.v1.golden.json",
    ];
    private static readonly Dictionary<string, string> Delivery = new(StringComparer.Ordinal)
    {
        ["docs/specs/combat-actual-paid-assault-v1.schema.json"] = "be1c5fcc36641ae1ee9f58b4b1c73baca098e7a8cde43d8dc568197f59399324",
        ["docs/specs/verify-combat-actual-paid-assault-v1.py"] = "744dc23429b6d343743f1a4c42adcba22502f9a21d9dc433dd3ee6e49a8e4dd1",
    };
}
