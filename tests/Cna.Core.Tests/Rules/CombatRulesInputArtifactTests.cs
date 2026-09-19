using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class CombatRulesInputArtifactTests
{
    [Fact]
    public void CanonicalArtifactExactlyMatchesFrozenRulesInputBytesAndHash()
    {
        var expected = Golden();
        var actual = CombatRulesInputArtifactCodec.SerializeCanonical(Cna1979CombatAdjudication.Definition);
        Assert.Equal(30395, actual.Length);
        Assert.Equal(expected, actual);
        Assert.Equal(Cna1979CombatAdjudication.ContentHash, Hash(actual));
        Assert.Equal(actual, CombatRulesInputArtifactCodec.SerializeCanonical(
            CombatRulesInputArtifactCodec.Deserialize(actual, Hash(actual))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":true}")]
    public void NoncanonicalRawArtifactsReject(string text) => Assert.Throws<JsonException>(() =>
        CombatRulesInputArtifactCodec.Deserialize(Encoding.UTF8.GetBytes(text),
            Cna1979CombatAdjudication.ContentHash));

    private static byte[] Golden()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-rules-inputs-v1.json")));
        var value = fixture.RootElement.GetProperty("goldens").EnumerateArray()
            .Single(item => item.GetProperty("kind").GetString() == "RulesInput");
        return Encoding.UTF8.GetBytes(value.GetProperty("canonicalUtf8").GetString()!);
    }

    private static string Hash(byte[] bytes) => $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";

    [Fact]
    public void ShuffledConstructionArraysAndNumericSetsEmitSameCanonicalAuthority()
    {
        var definition = Cna1979CombatAdjudication.Definition;
        var amendment = new CombatSourceAmendment(definition.Amendment.RulingId,
            definition.Amendment.Role, definition.Amendment.Differential,
            definition.Amendment.Coordinates.Reverse(), definition.Amendment.LossPercent);
        var effects = definition.Effects.Reverse().Select(value => new CombatEffectRow(value.Differential,
            value.AttackerEngagedSums.Reverse(), value.DefenderRetreatOneHexSums.Reverse(),
            value.AttackerCaptureSums.Reverse(), value.DefenderCaptureSums.Reverse()));
        var shuffled = Rebuild(sources: definition.Sources.Reverse(), evidence: definition.SourceEvidence.Reverse(),
            amendment: amendment, morale: definition.Morale.Reverse(), losses: definition.Losses.Reverse(),
            effects: effects, shares: definition.CaptureShares.Reverse(), policies: definition.Policies.Reverse());
        Assert.Equal(Golden(), CombatRulesInputArtifactCodec.SerializeCanonical(shuffled));
        // Sorting must be on owned copies, not mutate supplied construction order.
        Assert.Equal(definition.Losses[^1], shuffled.Losses[0]);
        Assert.Equal(36, shuffled.Amendment.Coordinates[0]);
        Assert.Equal(definition.Procedure.OrderedPurposes, shuffled.Procedure.OrderedPurposes);
    }

    [Theory]
    [InlineData("sources")]
    [InlineData("sourceEvidence")]
    [InlineData("morale")]
    [InlineData("losses")]
    [InlineData("effects")]
    [InlineData("captureShares")]
    [InlineData("policies")]
    [InlineData("amendment")]
    [InlineData("effect-set")]
    public void DuplicateConstructionKeysRejectInsteadOfDeduplicating(string field)
    {
        var d = Cna1979CombatAdjudication.Definition;
        var changed = field switch
        {
            "sources" => Rebuild(sources: d.Sources.Append(d.Sources[0])),
            "sourceEvidence" => Rebuild(evidence: d.SourceEvidence.Append(d.SourceEvidence[0])),
            "morale" => Rebuild(morale: d.Morale.Append(d.Morale[0])),
            "losses" => Rebuild(losses: d.Losses.Append(d.Losses[0])),
            "effects" => Rebuild(effects: d.Effects.Append(d.Effects[0])),
            "captureShares" => Rebuild(shares: d.CaptureShares.Append(d.CaptureShares[0])),
            "policies" => Rebuild(policies: d.Policies.Append(d.Policies[0])),
            "amendment" => Rebuild(amendment: new CombatSourceAmendment("CMB-SRC-RUL-001",
                CombatRole.Defender, 2, [34, 34, 35, 36], 10)),
            "effect-set" => Rebuild(effects: d.Effects.Select(value => new CombatEffectRow(value.Differential,
                value.AttackerEngagedSums.Append(value.AttackerEngagedSums[0]), value.DefenderRetreatOneHexSums,
                value.AttackerCaptureSums, value.DefenderCaptureSums))),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(changed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProcedurePurposeOrderCannotBeSortedOrRepaired(bool conditional)
    {
        var p = Cna1979CombatAdjudication.Definition.Procedure;
        var changed = new CombatProcedure(p.ProcedureId, p.StreamAlgorithm, p.AcceptedByteUpperExclusive, p.Faces,
            conditional ? p.OrderedPurposes : p.OrderedPurposes.Reverse(),
            conditional ? p.ConditionalCapturePurposes.Reverse() : p.ConditionalCapturePurposes);
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(Rebuild(procedure: changed)));
    }

    [Theory]
    [InlineData("schemaVersion")]
    [InlineData("artifactId")]
    [InlineData("profileId")]
    [InlineData("sources")]
    [InlineData("sourceEvidence")]
    [InlineData("amendment")]
    [InlineData("morale")]
    [InlineData("losses")]
    [InlineData("effects")]
    [InlineData("captureShares")]
    [InlineData("procedure")]
    [InlineData("costs")]
    [InlineData("settlement")]
    [InlineData("policies")]
    [InlineData("selfHash")]
    public void AlteredAuthorityRejectsWithOriginalOrForgedMatchingHash(string field)
    {
        var value = JsonNode.Parse(Golden())!.AsObject();
        switch (field)
        {
            case "schemaVersion": value[field] = 2; break;
            case "artifactId": case "profileId": value[field] = "forged"; break;
            case "sources": value[field]![0]!["locator"] = "forged"; break;
            case "sourceEvidence": value[field]![0]!["sha256"] = "sha256:" + new string('0', 64); break;
            case "amendment": value[field]!["lossPercent"] = 5; break;
            case "morale": value[field]![0]!["adjustment"] = 0; break;
            case "losses": value[field]![0]!["lossPercent"] = 20; break;
            case "effects": value[field]![0]!["attackerEngagedSums"]![0] = 9; break;
            case "captureShares": value[field]![2]!["percent"] = 34; break;
            case "procedure": value[field]!["orderedPurposes"]![0] = "defender.morale.tens"; break;
            case "costs": value[field]!["basicMoralePerRole"] = 1; break;
            case "settlement": value[field]!["lossDpPoints"] = 4; break;
            case "policies": value[field]![0]!["selectedBehaviorId"] = "forged"; break;
            case "selfHash": value["contentHash"] = Cna1979CombatAdjudication.ContentHash; break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
        var bytes = Encoding.UTF8.GetBytes(value.ToJsonString());
        Assert.NotEqual(Cna1979CombatAdjudication.ContentHash, Hash(bytes));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(bytes, Hash(bytes)));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(bytes, Cna1979CombatAdjudication.ContentHash));
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("nested-duplicate")]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("reordered-properties")]
    [InlineData("reordered-array")]
    [InlineData("float")]
    [InlineData("bool")]
    [InlineData("exponent")]
    [InlineData("numeric-string")]
    [InlineData("integer-overflow")]
    [InlineData("escaped")]
    [InlineData("bom")]
    [InlineData("newline")]
    [InlineData("whitespace")]
    [InlineData("trailing-object")]
    [InlineData("non-ascii")]
    public void AlternateRawSpellingsAndFramingReject(string mutation)
    {
        var text = Encoding.UTF8.GetString(Golden());
        var changed = mutation switch
        {
            "duplicate" => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal),
            "nested-duplicate" => text.Replace("\"die\":1,", "\"die\":1,\"die\":1,", StringComparison.Ordinal),
            "missing" => text.Replace("\"schemaVersion\":1,", "", StringComparison.Ordinal),
            "unknown" => text.Insert(1, "\"unknown\":1,"),
            "float" => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":1.0", StringComparison.Ordinal),
            "bool" => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":true", StringComparison.Ordinal),
            "exponent" => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":1e0", StringComparison.Ordinal),
            "numeric-string" => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":\"1\"", StringComparison.Ordinal),
            "integer-overflow" => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":2147483648", StringComparison.Ordinal),
            "escaped" => text.Replace("cna-1979", "\\u0063na-1979", StringComparison.Ordinal),
            "bom" => "\uFEFF" + text,
            "newline" => text + "\n",
            "whitespace" => " " + text,
            "trailing-object" => text + "{}",
            "non-ascii" => text.Replace("sandtable-rules-lab", "sandtable-rulés-lab", StringComparison.Ordinal),
            "reordered-properties" => ReorderProperties(text),
            "reordered-array" => ReverseMorale(text),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        Assert.NotEqual(text, changed);
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(changed), Cna1979CombatAdjudication.ContentHash));
    }

    [Fact]
    public void ByteDepthArrayAndScalarLimitsFailClosedWithoutTruncation()
    {
        byte[] tooManyBytes = new byte[1_048_577];
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(tooManyBytes,
            Cna1979CombatAdjudication.ContentHash));
        var deep = new string('[', 33) + "0" + new string(']', 33);
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(Encoding.UTF8.GetBytes(deep),
            Cna1979CombatAdjudication.ContentHash));
        var value = JsonNode.Parse(Golden())!.AsObject();
        value["morale"] = new JsonArray(Enumerable.Range(0, 513)
            .Select(_ => value["morale"]![0]!.DeepClone()).ToArray());
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(value.ToJsonString()), Cna1979CombatAdjudication.ContentHash));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(
            Rebuild(morale: Enumerable.Range(0, 513).Select(i => new CombatMoraleCell(i, 0)))));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(
            Rebuild(sources: [new RuleReference("a", new string('x', 513))])));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(
            Rebuild(artifactId: new string('x', 129))));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(
            Rebuild(artifactId: "not/an/id")));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(
            Rebuild(evidence: [new CombatSourceEvidence("a", "sha256:" + new string('A', 64))])));
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.SerializeCanonical(
            Rebuild(losses: [new CombatLossCell((CombatRole)2, 0, 11, 25)])));
    }

    [Fact]
    public void ExpectedHashAndHistoricalArtifactBoundariesRemainClosed()
    {
        var bytes = Golden();
        foreach (var hash in new[] { "", "sha256:" + new string('0', 64), Cna1979CombatAdjudication.ContentHash.ToUpperInvariant() })
        {
            Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(bytes, hash));
        }
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(bytes, null!));
        var old = BreakdownRulesArtifactCodec.SerializeCanonical(Cna1979Breakdown.Definition);
        Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(old,
            Cna1979CombatAdjudication.ContentHash));
        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(bytes));
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-rules-inputs-v1.json")));
        foreach (var other in fixture.RootElement.GetProperty("goldens").EnumerateArray()
            .Where(item => item.GetProperty("kind").GetString() != "RulesInput"))
        {
            Assert.Throws<JsonException>(() => CombatRulesInputArtifactCodec.Deserialize(
                Encoding.UTF8.GetBytes(other.GetProperty("canonicalUtf8").GetString()!),
                Cna1979CombatAdjudication.ContentHash));
        }
    }

    [Fact]
    public void ReturnedBytesAndReaderInputsCannotMutateAuthorityOrRegisterArtifact()
    {
        var expected = Golden();
        var bytes = CombatRulesInputArtifactCodec.SerializeCanonical(Cna1979CombatAdjudication.Definition);
        var restored = CombatRulesInputArtifactCodec.Deserialize(bytes, Hash(bytes));
        Array.Fill(bytes, (byte)'x');
        Assert.Equal(expected, CombatRulesInputArtifactCodec.SerializeCanonical(restored));
        var second = CombatRulesInputArtifactCodec.SerializeCanonical(restored);
        Assert.NotSame(expected, second);
        second[0] = (byte)'[';
        Assert.Equal(expected, CombatRulesInputArtifactCodec.SerializeCanonical(restored));
        var hash = Cna1979Ruleset.Manifest.Hash;
        var artifact = Cna1979CombatAdjudication.CreateArtifact();
        Assert.Equal(Cna1979CombatAdjudication.ArtifactId, artifact.ArtifactId);
        Assert.Equal(Cna1979CombatAdjudication.ContentHash, artifact.ContentHash);
        Assert.Equal(restored.Sources, artifact.Sources);
        Assert.Equal(hash, Cna1979Ruleset.Manifest.Hash);
        Assert.DoesNotContain(Cna1979Ruleset.Manifest.Artifacts, item => item.ArtifactId == artifact.ArtifactId);
        Assert.Throws<NotSupportedException>(() => ((IList<RuleReference>)artifact.Sources)[0] =
            new RuleReference("changed", "changed"));
    }

    private static string ReorderProperties(string text)
    {
        var value = JsonNode.Parse(text)!.AsObject();
        return new JsonObject(value.Reverse().Select(pair =>
            KeyValuePair.Create<string, JsonNode?>(pair.Key, pair.Value!.DeepClone()))).ToJsonString();
    }

    private static string ReverseMorale(string text)
    {
        var value = JsonNode.Parse(text)!.AsObject();
        value["morale"] = new JsonArray(value["morale"]!.AsArray().Reverse()
            .Select(item => item!.DeepClone()).ToArray());
        return value.ToJsonString();
    }

    private static CombatRulesInputDefinition Rebuild(string? artifactId = null,
        IEnumerable<RuleReference>? sources = null, IEnumerable<CombatSourceEvidence>? evidence = null,
        CombatSourceAmendment? amendment = null, IEnumerable<CombatMoraleCell>? morale = null,
        IEnumerable<CombatLossCell>? losses = null, IEnumerable<CombatEffectRow>? effects = null,
        IEnumerable<CombatCaptureShare>? shares = null, CombatProcedure? procedure = null,
        IEnumerable<CombatPolicy>? policies = null)
    {
        var d = Cna1979CombatAdjudication.Definition;
        return new CombatRulesInputDefinition(d.SchemaVersion, artifactId ?? d.ArtifactId, d.ProfileId,
            sources ?? d.Sources, evidence ?? d.SourceEvidence, amendment ?? d.Amendment,
            morale ?? d.Morale, losses ?? d.Losses, effects ?? d.Effects, shares ?? d.CaptureShares,
            procedure ?? d.Procedure, d.Costs, d.Settlement, policies ?? d.Policies);
    }
}
