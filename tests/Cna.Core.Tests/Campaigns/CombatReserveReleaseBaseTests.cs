using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReserveReleaseBaseTests
{
    [Fact]
    public void FortyFourIsolatedBasesMatchFrozenHashesAndExpectedContextReadback()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-reserve-release-v1.json"));
        Assert.Equal("sha256:70ed683c21f95a511c06362765dbe0971e0bb024bfcee74be70865be70601af9", Hash(bytes));
        using var fixture = JsonDocument.Parse(bytes);
        var count = 0; var excluded = 0;
        var names = new HashSet<string>(StringComparer.Ordinal) { "first-release", "first-convert", "first-timeout-midway", "first-clock-regression", "first-unavailable", "first-opening-clock-loss", "later-release-retain", "later-complete-intent", "later-expiry", "empty", "prior-release-retained" };
        foreach (var golden in fixture.RootElement.GetProperty("goldens").EnumerateArray())
        {
            var name = Text(golden, "case");
            var recipe = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => Text(c, "name") == name);
            if (recipe.TryGetProperty("settledCase", out _))
            {
                Assert.True(name is "settled-guard-empty" or "settled-escape-empty"); excluded++; continue;
            }
            Assert.Contains(name, names);
            var (basis, request) = Build(recipe, Text(golden, "side"), Text(golden, "slot"));
            var canonical = CampaignCombatReserveReleaseCodec.SerializeBase(basis, request);
            Assert.Equal(Text(golden, "baseHash"), Hash(canonical));
            Assert.Equal(canonical, CampaignCombatReserveReleaseCodec.SerializeBase(CampaignCombatReserveReleaseCodec.ReadBase(canonical, basis, request), request));
            count++;
        }
        Assert.Equal(44, count); Assert.Equal(4, excluded);
    }

    [Fact]
    public void EveryCanonicalFieldForgeryRejectsAgainstIndependentExpectedValue()
    {
        var (basis, request) = Example(); var bytes = Write(basis, request);
        var root = JsonNode.Parse(bytes)!.AsObject();
        var changed = 0;
        foreach (var path in Leaves(root))
        {
            var forged = JsonNode.Parse(bytes)!;
            var parent = forged;
            foreach (var key in path[..^1]) parent = parent is JsonArray array ? array[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)]! : parent[key]!;
            var last = path[^1];
            var value = parent is JsonArray values ? values[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : parent[last];
            if (value is null) continue;
            JsonNode replacement;
            if (value.GetValueKind() == JsonValueKind.Number) replacement = JsonValue.Create(value.GetValue<long>() + 1)!;
            else
            {
                var original = value.GetValue<string>();
                var foreignValue = original switch
                {
                    "axis" => "commonwealth",
                    "commonwealth" => "axis",
                    _ when original.StartsWith("sha256:", StringComparison.Ordinal) || original.Length == 64 => original[..^1] + (original[^1] == '0' ? "1" : "0"),
                    _ => original + ".foreign"
                };
                replacement = JsonValue.Create(foreignValue)!;
            }
            if (last == "denominator") parent["numerator"] = 1; // Keep rational candidate reduced.
            if (parent is JsonArray items) items[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else parent[last] = replacement;
            var failure = Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadBase(JsonSerializer.SerializeToUtf8Bytes(forged), basis, request));
            Assert.True(failure.Message.Contains("differs from independently expected", StringComparison.Ordinal), string.Join(".", path) + ": " + failure.Message); changed++;
        }
        Assert.True(changed >= 35);
        var foreign = CampaignCombatCreationRequest.Create("foreign.campaign", 0, request.Context);
        Assert.ThrowsAny<JsonException>(() => Write(basis, foreign));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadBase(bytes, basis, foreign));
        foreach (var invalid in new[]
        {
            basis with { Cycle = basis.Cycle with { ContractVersion = 2 } }, basis with { Cycle = basis.Cycle with { GameTurn = 0 } },
            basis with { Cycle = basis.Cycle with { GameTurn = 112 } }, basis with { Cycle = basis.Cycle with { OperationStage = 4 } },
            basis with { Cycle = basis.Cycle with { Ordinal = 0 } }, basis with { Cycle = basis.Cycle with { OpenedAuthorityVersion = 0 } },
            basis with { Cycle = basis.Cycle with { OpenedAuthorityVersion = 101 } }, basis with { PriorVersion = 19 },
            basis with { FirstActingSide = LandSide.Commonwealth }, basis with { Cycle = basis.Cycle with { PlayerPhaseSlot = "second-acting-side" } },
            basis with { PositionId = "foreign.position" }, basis with { Profile = "settled-empty-release" }, basis with { ContractVersion = 2 },
            basis with { AcceptedHighWater = -1 }, basis with { AcceptedHighWater = 253402300800000 }, basis with { PriorPrefix = "sha256:bad" }
        }) Assert.ThrowsAny<JsonException>(() => Write(invalid, request));
    }

    [Fact]
    public void IllegalReserveHistoriesAndOffensiveLinksReject()
    {
        var (basis, request) = Example(); var member = basis.Members[0]; var h = member.History;
        foreach (var bad in new[]
        {
            member with { BaseCpa = 0 }, member with { Status = (CampaignElementReserveStatus)99 }, member with { Status = CampaignElementReserveStatus.ReserveII },
            member with { History = h with { DesignationReceiptId = null } }, member with { History = h with { ConversionReceiptId = "conversion" } },
            member with { History = h with { Scope = h.Scope with { GameTurn = 2 } } }, member with { History = h with { ReleaseReceiptId = "release" } },
            member with { History = h with { ReleaseCycle = 1 } }, member with { History = h with { CpaBasis = 10 } },
            member with { History = h with { VoluntaryCeiling = 10 } }, member with { History = h with { OffensiveCommitmentId = "attack" } },
            member with { History = h with { NextMovement = new(h.Scope, 2, "expired", "completion") } },
            member with { Unit = new(request.CreationBinding, "commonwealth", member.Unit.ElementId) },
            member with { Unit = new("foreign", "axis", member.Unit.ElementId) }, member with { SpentCp = null! }
        }) Assert.ThrowsAny<JsonException>(() => Write(Copy(basis, [bad]), request));
        var (later, laterRequest) = Example("later-release-retain");
        Assert.ThrowsAny<JsonException>(() => Write(Copy(later, [later.Members[0] with { History = later.Members[0].History with { ConversionReceiptId = null } }]), laterRequest));
        var (prior, priorRequest) = Example("prior-release-retained"); var released = prior.Members[0]; var old = released.History;
        foreach (var badHistory in new[]
        {
            old with { DesignationReceiptId = null }, old with { ReleaseReceiptId = null }, old with { ReleasedType = "III" },
            old with { ConversionReceiptId = "conversion" }, old with { ReleaseCycle = 0 }, old with { ReleaseCycle = 2 }, old with { ReleaseCycle = 3 },
            old with { CpaBasis = 9 }, old with { VoluntaryCeiling = 5 }, old with { NextMovement = null },
            old with { NextMovement = old.NextMovement! with { Status = "pending" } }, old with { NextMovement = old.NextMovement! with { Ordinal = 3 } },
            old with { NextMovement = old.NextMovement! with { Scope = old.Scope with { OperationStage = 2 } } },
            old with { NextMovement = old.NextMovement! with { CompletionReceiptId = null } }, old with { OffensiveCommitmentId = "attack" }
        }) Assert.ThrowsAny<JsonException>(() => Write(Copy(prior, [released with { History = badHistory }]), priorRequest));
        var linked = released with { History = old with { OffensiveCommitmentId = "attack" } };
        var attack = new CombatRoundAttackHistory("attack", basis.PriorPrefix, "segment", released.Unit, new(request.CreationBinding, "commonwealth", "defender"), "target", 1, 1);
        var valid = Copy(prior, [linked], [attack]);
        Assert.Equal(Write(valid, priorRequest), Write(CampaignCombatReserveReleaseCodec.ReadBase(Write(valid, priorRequest), valid, priorRequest), priorRequest));
        foreach (var records in new[] { new[] { attack, attack }, new[] { attack with { Attacker = attack.Defender } }, new[] { attack with { GameTurn = 2 } }, new[] { attack with { OperationStage = 2 } } })
            Assert.ThrowsAny<JsonException>(() => Write(Copy(prior, [linked], records), priorRequest));
        // Legal historical II and retained overspending are independent structural probes, not additional goldens.
        var second = old with { ReleasedType = "II", ConversionReceiptId = "conversion", ReleaseCycle = 2, VoluntaryCeiling = 5, NextMovement = old.NextMovement! with { Ordinal = 3 } };
        _ = Write(Copy(prior, [released with { History = second, SpentCp = new(99, 1) }]), priorRequest);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapabilityPointAmount(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapabilityPointAmount(1, 0));
    }

    [Fact]
    public void RawMalformedShapesFailBeforeTrustedContextIsTouched()
    {
        var (basis, request) = Example(); var canonical = Write(basis, request); var text = Encoding.UTF8.GetString(canonical);
        var malformed = new List<byte[]>
        {
            Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(text[..^1]), new byte[] { 0xef, 0xbb, 0xbf }.Concat(canonical).ToArray(),
            Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":true", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":2147483648", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"priorVersion\":100", "\"priorVersion\":9223372036854775808", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"seed\":0", "\"seed\":18446744073709551616", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"seed\":0", "\"seed\":-1", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"denominator\":1", "\"denominator\":2", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"numerator\":0", "\"numerator\":-1", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"extra\":0,\"contractVersion\":1", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"profile\":\"isolated-ledger\",", "", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"profile\":\"isolated-ledger\"", "\"profile\":null", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"profile\":\"isolated-ledger\"", "\"profile\":\"é\"", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"profile\":\"isolated-ledger\"", "\"profile\":\"-invalid\"", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1,\"contractVersion\":1", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33))
        };
        var invalidUtf8 = canonical.ToArray(); invalidUtf8[Array.IndexOf(invalidUtf8, (byte)'i')] = 0xff; malformed.Add(invalidUtf8);
        var oversizedMembers = JsonNode.Parse(canonical)!; var member = oversizedMembers["members"]![0]!.DeepClone();
        oversizedMembers["members"] = new JsonArray(Enumerable.Range(0, 33).Select(_ => member.DeepClone()).ToArray()); malformed.Add(JsonSerializer.SerializeToUtf8Bytes(oversizedMembers));
        var oversizedAttacks = JsonNode.Parse(canonical)!; oversizedAttacks["attackHistory"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode)new JsonObject()).ToArray()); malformed.Add(JsonSerializer.SerializeToUtf8Bytes(oversizedAttacks));
        foreach (var bytes in malformed) Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadBase(bytes, null!, null!));
        foreach (var changed in new[] { " " + text, text + "\n", text.Replace("\"seed\":0", "\"seed\":-0", StringComparison.Ordinal), text.Replace("isolated-ledger", "isolated\\u002dledger", StringComparison.Ordinal), text.Replace("\"contractVersion\":1,\"profile\":\"isolated-ledger\"", "\"profile\":\"isolated-ledger\",\"contractVersion\":1", StringComparison.Ordinal) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadBase(Encoding.UTF8.GetBytes(changed), null!, null!));
    }

    [Fact]
    public void UnreferencedAttackCycleRequiresHashBeforeTrustedContext()
    {
        var (basis, request) = Example();
        var attack = new CombatRoundAttackHistory("attack", "cycle", "segment", basis.Members[0].Unit, new(request.CreationBinding, "commonwealth", "defender"), "target", 1, 1);
        Assert.ThrowsAny<JsonException>(() => Write(Copy(basis, basis.Members, [attack]), request));
        var valid = Copy(basis, basis.Members, [attack with { CycleId = basis.PriorPrefix }]);
        var malformed = Encoding.UTF8.GetString(Write(valid, request)).Replace("\"cycleId\":\"" + basis.PriorPrefix + "\"", "\"cycleId\":\"cycle\"", StringComparison.Ordinal);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveReleaseCodec.ReadBase(Encoding.UTF8.GetBytes(malformed), null!, null!));
    }

    [Fact]
    public void CollectionsAndOutputBytesAreOwnedWithExactStructuralBounds()
    {
        var (basis, request) = Example(); var members = basis.Members.ToArray();
        var attack = new CombatRoundAttackHistory("attack", basis.PriorPrefix, "segment", members[0].Unit, new(request.CreationBinding, "commonwealth", "defender"), "target", 1, 1);
        var attacks = new[] { attack }; var owned = Copy(basis, members, attacks); var bytes = Write(owned, request); var expected = bytes.ToArray();
        members[0] = members[0] with { History = members[0].History with { DesignationReceiptId = "changed" } }; attacks[0] = attack with { CommitmentId = "changed" }; bytes[0] = 0;
        Assert.Equal(expected, Write(owned, request)); Assert.Equal(expected, Write(owned, request));
        Assert.Throws<NotSupportedException>(() => ((IList<CombatReleaseMember>)owned.Members)[0] = members[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<CombatRoundAttackHistory>)owned.AttackHistory)[0] = attacks[0]);
        var many = Enumerable.Range(0, 32).Select(i => basis.Members[0] with { Unit = new(request.CreationBinding, "axis", $"probe.{i:D2}") }).ToArray();
        var bounded = Copy(basis, many, Enumerable.Repeat(attack, 512)); _ = Write(bounded, request);
        Assert.ThrowsAny<JsonException>(() => Write(Copy(basis, [.. many, many[0]]), request));
        Assert.ThrowsAny<JsonException>(() => Write(Copy(basis, many.Reverse()), request));
        Assert.ThrowsAny<JsonException>(() => Write(Copy(basis, [many[0], many[0]]), request));
        Assert.ThrowsAny<JsonException>(() => Write(Copy(basis, many, Enumerable.Repeat(attack, 513)), request));
        _ = Write(basis with { AcceptedHighWater = null }, request);
        _ = Write(basis with { AcceptedHighWater = 253402300799999, PriorVersion = long.MaxValue }, request);
    }

    private static IEnumerable<string[]> Leaves(JsonNode? node)
    {
        if (node is JsonObject obj) foreach (var pair in obj) foreach (var tail in Leaves(pair.Value)) yield return [pair.Key, .. tail];
        else if (node is JsonArray array) for (var i = 0; i < array.Count; i++) foreach (var tail in Leaves(array[i])) yield return [i.ToString(System.Globalization.CultureInfo.InvariantCulture), .. tail];
        else yield return [];
    }
    private static (CombatReleaseBase Base, CampaignCombatCreationRequest Request) Example(string name = "first-release")
    { using var fixture = Load("Campaigns", "combat-reserve-release-v1.json"); return Build(fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => Text(c, "name") == name)); }
    private static byte[] Write(CombatReleaseBase basis, CampaignCombatCreationRequest request) => CampaignCombatReserveReleaseCodec.SerializeBase(basis, request);
    private static CombatReleaseBase Copy(CombatReleaseBase b, IEnumerable<CombatReleaseMember> members, IEnumerable<CombatRoundAttackHistory>? attacks = null) => new(b.ContractVersion, b.Profile, b.Cycle, b.FirstActingSide, b.PositionId, b.PriorVersion, b.PriorPrefix, b.CombatCompletionReceiptId, b.RetainedWorldHash, b.RandomState, b.AcceptedHighWater, members, attacks ?? b.AttackHistory);

    // Independent frozen base_for recipe: authority Created11 + Content7 + Result2 cycle template.
    // No expected base hash, event, or state is used to construct these isolated probe values.
    private static (CombatReleaseBase Base, CampaignCombatCreationRequest Request) Build(JsonElement recipe, string side = "axis", string slot = "first-acting-side")
    {
        using var authority = Load("Rules", "combat-authority-envelope-v1.json");
        using var created = JsonDocument.Parse(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var artifact = Cna1979CombatContentCatalog.Artifact; var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, new(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
        using var result = Load("Campaigns", "combat-result-settlement-v2.json");
        using var resultBase = JsonDocument.Parse(result.RootElement.GetProperty("traces")[0].GetProperty("baseCanonicalUtf8").GetString()!);
        var c = resultBase.RootElement.GetProperty("boundary").GetProperty("cycle");
        var cycle = new CampaignCombatCycleAuthority(1, Text(c, "campaignId"), Text(c, "rulesetHash"), Text(c, "setupId"), Text(c, "setupHash"), Text(c, "contentPackId"), Text(c, "contentHash"), Text(c, "scenarioId"),
            c.GetProperty("gameTurn").GetInt32(), c.GetProperty("operationStage").GetInt32(), slot, Side(side), recipe.GetProperty("ordinal").GetInt32(), 20, Text(c, "openingPrefix"), Text(c, "admittedPolicyBundleDigest"));
        using var content = Load("Content", "combat-content-v7.canonical.json");
        var elementId = Text(content.RootElement.GetProperty("elements").EnumerateArray().First(e => Text(e, "sideId") == side), "elementId");
        var scope = new CombatReleaseScope(cycle.GameTurn, cycle.OperationStage, slot, cycle.ActingSide);
        var members = recipe.GetProperty("statuses").EnumerateArray().Select((s, i) =>
        {
            var status = s.GetString();
            var cpa = recipe.TryGetProperty("cpa", out var amounts) ? amounts[i].GetInt32() : 10;
            var spent = recipe.TryGetProperty("spent", out var spending) ? spending[i].GetInt64() : 0;
            var history = new CombatReleaseHistory(scope, status is "I" or "II" ? $"probe.designation.{i}" : null, status == "II" ? $"probe.conversion.{i}" : null);
            if (recipe.TryGetProperty("priorRelease", out _)) history = new(scope, "probe.designation.0", null, "I", "probe.release.0", 1, cpa, cpa, null, new(scope, 2, "expired", "probe.movement-completed"));
            return new CombatReleaseMember(new(request.CreationBinding, side, elementId + (i == 0 ? "" : $".probe-{i + 1}")), status switch { "I" => CampaignElementReserveStatus.ReserveI, "II" => CampaignElementReserveStatus.ReserveII, _ => CampaignElementReserveStatus.None }, cpa, new(spent, 1), history);
        }).ToArray();
        var first = slot == "first-acting-side" ? Side(side) : Side(side == "axis" ? "commonwealth" : "axis");
        var actor = slot == "first-acting-side" ? LandActorRole.FirstActingSide : LandActorRole.SecondActingSide;
        var position = Cna1979LandSequence.CreateTurn(cycle.GameTurn).Single(p => p.OperationStage == cycle.OperationStage && p.SegmentId == LandSegmentIds.ReserveRelease && p.ActorRole == actor);
        var prefix = Hash(JsonSerializer.SerializeToUtf8Bytes(new { probe = Text(recipe, "name"), side, slot }));
        return (new(1, "isolated-ledger", cycle, first, position.PositionId, 100, prefix, "probe.combat-completed", Hash("synthetic retained World for isolated ledger probe"u8.ToArray()), request.RandomState, 1000, members, []), request);
    }
    private static JsonDocument Load(string area, string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, area, "Fixtures", name)));
    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetString()!;
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static LandSide Side(string side) => side == "axis" ? LandSide.Axis : LandSide.Commonwealth;
}
