using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class BreakdownOutcomeArtifactTests
{
    [Fact]
    public void SuccessorMatchesIndependentlyAssembledGoldenAndKeepsContinuityFieldsExact()
    {
        var canonical = BreakdownRulesV2ArtifactCodec.SerializeCanonical(Cna1979BreakdownAdjudication.Definition);
        var golden = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures",
            "cna-1979.1.breakdown-tables.v2.golden.json"));
        Assert.Equal((byte)'\n', golden[^1]);
        Assert.Equal(golden.AsSpan(0, golden.Length - 1).ToArray(), canonical);
        Assert.Equal(canonical, BreakdownRulesV2ArtifactCodec.SerializeCanonical(
            BreakdownRulesV2ArtifactCodec.Deserialize(canonical)));
        using var current = JsonDocument.Parse(BreakdownRulesArtifactCodec.SerializeCanonical(Cna1979Breakdown.Definition));
        using var successor = JsonDocument.Parse(canonical);
        foreach (var field in current.RootElement.EnumerateObject())
        {
            if (field.Name is not ("schemaVersion" or "sources"))
            {
                Assert.Equal(field.Value.GetRawText(), successor.RootElement.GetProperty(field.Name).GetRawText());
            }
        }
        Assert.Equal(2, successor.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(6, successor.RootElement.GetProperty("outcomeFractions").GetArrayLength());
        Assert.Equal(324, successor.RootElement.GetProperty("outcomeCells").GetArrayLength());
        Assert.Equal(["diceCoordinate", "outcomeFractions", "outcomeCells", "sources"],
            successor.RootElement.EnumerateObject().TakeLast(4).Select(p => p.Name));
        var artifact = Cna1979BreakdownAdjudication.CreateArtifact();
        Assert.Equal(Cna1979Breakdown.ArtifactId, artifact.ArtifactId);
        Assert.Equal(Hash(canonical), artifact.ContentHash);
        Assert.Equal(Cna1979BreakdownAdjudication.Definition.Sources, artifact.Sources);
        Assert.Contains(new RuleReference("spi-1979-land-rules", "21.35"), artifact.Sources);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("fraction")]
    [InlineData("nonreduced-fraction")]
    [InlineData("rounding-source")]
    [InlineData("ruling")]
    [InlineData("label")]
    [InlineData("coordinate")]
    [InlineData("band")]
    [InlineData("cell-source")]
    [InlineData("missing-cell")]
    [InlineData("duplicate-cell")]
    [InlineData("reordered-cells")]
    [InlineData("unknown-field")]
    public void SemanticallyAlteredArtifactsRejectEvenWithRecomputedHashes(string mutation)
    {
        var canonical = BreakdownRulesV2ArtifactCodec.SerializeCanonical(Cna1979BreakdownAdjudication.Definition);
        var changed = JsonNode.Parse(canonical)!.AsObject();
        var fractions = changed["outcomeFractions"]!.AsArray();
        var cells = changed["outcomeCells"]!.AsArray();
        switch (mutation)
        {
            case "schema": changed["schemaVersion"] = 3; break;
            case "fraction": fractions[3]!["fraction"]!["numerator"] = 33; fractions[3]!["fraction"]!["denominator"] = 100; break;
            case "nonreduced-fraction": fractions[3]!["fraction"]!["numerator"] = 2; fractions[3]!["fraction"]!["denominator"] = 6; break;
            case "rounding-source": fractions[3]!["sources"]![2]!["locator"] = "21.36"; break;
            case "ruling": fractions[3]!["rulingId"] = "unapproved"; break;
            case "label": cells[0]!["printedLabel"] = 10; break;
            case "coordinate": cells[0]!["coordinate"] = 17; break;
            case "band": cells[0]!["bandId"] = "land.breakdown.band.4-10"; break;
            case "cell-source": cells[0]!["sources"]![0]!["locator"] = "21.39"; break;
            case "missing-cell": cells.RemoveAt(0); break;
            case "duplicate-cell": cells[1] = cells[0]!.DeepClone(); break;
            case "reordered-cells": var first = cells[0]!.DeepClone(); cells[0] = cells[1]!.DeepClone(); cells[1] = first; break;
            case "unknown-field": changed["trusted"] = true; break;
            default: throw new InvalidOperationException("Unknown test mutation.");
        }
        var bytes = Encoding.UTF8.GetBytes(changed.ToJsonString());
        Assert.NotEqual(Hash(canonical), Hash(bytes));
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.Deserialize(bytes));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    public void InvalidJsonOrEmptyAuthorityRejects(string json) => Assert.Throws<JsonException>(() =>
        BreakdownRulesV2ArtifactCodec.Deserialize(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public void DuplicateFieldsTrailingWhitespaceAndMixedVersionsReject()
    {
        var old = BreakdownRulesArtifactCodec.SerializeCanonical(Cna1979Breakdown.Definition);
        var next = BreakdownRulesV2ArtifactCodec.SerializeCanonical(Cna1979BreakdownAdjudication.Definition);
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.Deserialize(old));
        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(next));
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.Deserialize([.. next, (byte)'\n']));
        var duplicate = Encoding.UTF8.GetString(next).Replace("\"schemaVersion\":2",
            "\"schemaVersion\":2,\"schemaVersion\":2", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.Deserialize(Encoding.UTF8.GetBytes(duplicate)));
    }

    [Fact]
    public void TypedDefinitionMutationsCannotProduceAcceptedAuthority()
    {
        var original = Cna1979BreakdownAdjudication.Definition;
        var fractions = original.OutcomeFractions.ToArray();
        fractions[3] = fractions[3] with { Fraction = new BreakdownPointAmount(33, 100) };
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.SerializeCanonical(
            original with { OutcomeFractions = fractions }));
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.SerializeCanonical(
            original with { OutcomeCells = original.OutcomeCells.Skip(1).ToArray() }));
        Assert.Throws<JsonException>(() => BreakdownRulesV2ArtifactCodec.SerializeCanonical(
            original with { SchemaVersion = 1 }));
        Assert.Equal(new BreakdownPointAmount(1, 3), original.OutcomeFractions[3].Fraction);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<BreakdownOutcomeCell>)original.OutcomeCells)[0] = original.OutcomeCells[1]);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<RuleReference>)original.OutcomeFractions[0].Sources).Clear());
    }

    [Fact]
    public void AcceptedRulingsAreCompleteButNotRegisteredWithActiveRuleset()
    {
        var rulings = Cna1979BreakdownAdjudication.CreateRulings();
        Assert.Equal(["BRK-DEC-004", "BRK-DEC-005", "BRK-DEC-006", "BRK-DEC-007"],
            rulings.Select(r => r.ConflictId));
        Assert.Equal(["exact-fractions-ceiling-single-point-exception", "reaction-before-phasing-stop",
                "certified-unladen-truck-battalion", "persistent-stop-location-lots"],
            rulings.Select(r => r.SelectedBehaviorId));
        Assert.All(rulings, ruling =>
        {
            Assert.Equal(2, ruling.AlternativeIds.Count);
            Assert.Contains(ruling.SelectedBehaviorId, ruling.AlternativeIds);
            Assert.NotEmpty(ruling.Sources);
            Assert.NotEmpty(ruling.ProtectingTestIds);
            Assert.DoesNotContain(Cna1979Ruleset.Manifest.Rulings, r => r.RulingId == ruling.RulingId);
        });
        Assert.Equal(Cna1979BreakdownAdjudication.ExactFractionsRulingId, rulings[0].RulingId);
        Assert.Equal(new[] { new RuleReference("spi-1979-land-rules", "21.34"),
            new RuleReference("spi-1979-land-rules", "21.35") }, rulings[0].Sources);
        Assert.All(Cna1979BreakdownAdjudication.Definition.OutcomeFractions,
            f => Assert.Equal(rulings[0].RulingId, f.RulingId));
        Assert.Equal(8, Cna1979Ruleset.Manifest.ContractVersion);
        Assert.Equal("0e80a8ba917113b401ea709f9f2a6cd7fb7cfec03b8adbdae978f1b219e141e0",
            Cna1979Ruleset.Manifest.Hash);
        Assert.Equal("sha256:c7061325838dfcdd2f2388be3c6f6ec998bfa96df14b4cd6e733dd1c5d16c747",
            Cna1979Breakdown.CreateArtifact().ContentHash);
        Assert.NotEqual(Cna1979Breakdown.CreateArtifact().ContentHash,
            Cna1979BreakdownAdjudication.CreateArtifact().ContentHash);
    }

    private static string Hash(byte[] bytes) => $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";
}
