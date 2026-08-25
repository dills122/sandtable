using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class ReserveRulesTests
{
    [Fact]
    public void CanonicalReserveArtifactCarriesTheExactV1Authority()
    {
        var definition = Cna1979Reserve.Definition;
        var canonical = ReserveRulesArtifactCodec.SerializeCanonical(definition);
        var parsed = ReserveRulesArtifactCodec.Deserialize(canonical);

        Assert.Equal(1, definition.SchemaVersion);
        Assert.Equal("resolved-first-acting-side", definition.EligibleOwner);
        Assert.Equal("reserve-designation", definition.AssignmentTiming);
        Assert.Equal("reserve-i", definition.AssignmentResult);
        Assert.Equal(0, definition.CapabilityPointCost);
        Assert.Equal("none", definition.SupportedTransition.From);
        Assert.Equal("reserve-i", definition.SupportedTransition.To);
        Assert.Equal(
            [
                new RuleReference("spi-1979-land-rules", "18.11"),
                new RuleReference("spi-1979-land-rules", "18.12"),
                new RuleReference("spi-1979-land-rules", "18.15"),
                new RuleReference("spi-1979-land-rules", "18.21"),
                new RuleReference("spi-1979-land-rules", "18.26"),
                new RuleReference("spi-1979-land-rules", "5.2.reserve-designation"),
            ],
            definition.Sources);
        Assert.Equal(definition, parsed);
        Assert.Equal(canonical, ReserveRulesArtifactCodec.SerializeCanonical(parsed));
        Assert.DoesNotContain("commandPoint", Encoding.UTF8.GetString(canonical),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "{\"schemaVersion\":1,\"eligibleOwner\":\"resolved-first-acting-side\"," +
            "\"assignmentTiming\":\"reserve-designation\",\"assignmentResult\":\"reserve-i\"," +
            "\"capabilityPointCost\":0,\"supportedTransition\":{\"from\":\"none\"," +
            "\"to\":\"reserve-i\"},\"sources\":[{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"18.11\"},{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"18.12\"},{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"18.15\"},{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"18.21\"},{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"18.26\"},{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"5.2.reserve-designation\"}]}",
            Encoding.UTF8.GetString(canonical));

        var artifact = Cna1979Reserve.CreateArtifact();
        Assert.Equal("cna-1979.1.reserve-designation", artifact.ArtifactId);
        Assert.Equal(definition.Sources, artifact.Sources);
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}",
            artifact.ContentHash);
        Assert.Equal(
            "sha256:3d5fb13758e4539a14c89f0c884abd230f8a3a14f0e57be68aa914716278c0ca",
            artifact.ContentHash);
    }

    [Fact]
    public void ReserveArtifactCodecRejectsNoncanonicalOrChangedAuthority()
    {
        var baseline = Cna1979Reserve.Definition;
        ReserveRulesArtifactDefinition[] mutations =
        [
            Copy(baseline, schemaVersion: baseline.SchemaVersion + 1),
            Copy(baseline, eligibleOwner: "phasing-side"),
            Copy(baseline, assignmentTiming: "movement"),
            Copy(baseline, assignmentResult: "reserve-ii"),
            Copy(baseline, capabilityPointCost: 1),
            Copy(baseline, supportedTransition: new("reserve-i", "reserve-ii")),
            Copy(baseline, sources: baseline.Sources.Reverse()),
            Copy(baseline, sources: baseline.Sources.Skip(1)),
        ];

        Assert.All(mutations, mutation => Assert.Throws<JsonException>(
            () => ReserveRulesArtifactCodec.SerializeCanonical(mutation)));

        var canonical = Encoding.UTF8.GetString(
            ReserveRulesArtifactCodec.SerializeCanonical(baseline));
        var reordered = canonical.Replace(
            "{\"schemaVersion\":1,\"eligibleOwner\"",
            "{\"eligibleOwner\":\"resolved-first-acting-side\",\"schemaVersion\":1,\"ignored\"",
            StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => ReserveRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(reordered)));
        Assert.Throws<JsonException>(() => ReserveRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(canonical + "\n")));
    }

    [Fact]
    public void CanonicalManifestCarriesTheAcceptedEmptyDesignationRuling()
    {
        var ruling = Assert.Single(
            Cna1979Ruleset.Manifest.Rulings,
            value => value.RulingId == Cna1979Reserve.EmptySelectionRulingId);

        Assert.Equal("cna-1979.1.conflict.reserve-designation-minimum", ruling.ConflictId);
        Assert.Equal(
            [
                "require-at-least-one-reserve-designation",
                "allow-empty-reserve-designation",
            ],
            ruling.AlternativeIds);
        Assert.Equal("allow-empty-reserve-designation", ruling.SelectedBehaviorId);
        Assert.Equal(["RES-AC-002", "RES-AC-006", "RES-AC-009"], ruling.ProtectingTestIds);
        Assert.Equal(
            [
                new RuleReference("spi-1979-land-rules", "18.11"),
                new RuleReference("spi-1979-land-rules", "5.2.reserve-designation"),
            ],
            ruling.Sources);
        Assert.Equal(
            "{\"rulingId\":\"cna-1979.1.ruling.empty-reserve-designation\"," +
            "\"conflictId\":\"cna-1979.1.conflict.reserve-designation-minimum\"," +
            "\"alternativeIds\":[\"allow-empty-reserve-designation\"," +
            "\"require-at-least-one-reserve-designation\"]," +
            "\"selectedBehaviorId\":\"allow-empty-reserve-designation\"," +
            "\"protectingTestIds\":[\"RES-AC-002\",\"RES-AC-006\",\"RES-AC-009\"]," +
            "\"sources\":[{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"18.11\"}," +
            "{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"5.2.reserve-designation\"}]}",
            Encoding.UTF8.GetString(RulesetManifest.SerializeCanonicalRuling(ruling)));
    }

    private static ReserveRulesArtifactDefinition Copy(
        ReserveRulesArtifactDefinition definition,
        int? schemaVersion = null,
        string? eligibleOwner = null,
        string? assignmentTiming = null,
        string? assignmentResult = null,
        int? capabilityPointCost = null,
        ReserveStatusTransitionDefinition? supportedTransition = null,
        IEnumerable<RuleReference>? sources = null) => new(
            schemaVersion ?? definition.SchemaVersion,
            eligibleOwner ?? definition.EligibleOwner,
            assignmentTiming ?? definition.AssignmentTiming,
            assignmentResult ?? definition.AssignmentResult,
            capabilityPointCost ?? definition.CapabilityPointCost,
            supportedTransition ?? definition.SupportedTransition,
            sources ?? definition.Sources);
}
