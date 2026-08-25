using System.Text;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class StageEntryRulingTests
{
    private const string RulingId =
        "cna-1979.1.ruling.explicit-empty-stage-entry-resolution";

    [Fact]
    public void CanonicalManifestContainsTheAcceptedExplicitEmptyStageEntryRuling()
    {
        var manifest = Cna1979Ruleset.Manifest;
        var ruling = Assert.Single(
            manifest.Rulings,
            candidate => candidate.RulingId == RulingId);

        Assert.Equal(5, manifest.ContractVersion);
        Assert.Equal("cna-1979.1.conflict.empty-stage-entry-phase", ruling.ConflictId);
        Assert.Equal(
            [
                "reject-empty-stage-entry-as-unsupported",
                "resolve-explicitly-admitted-empty-stage-entry",
            ],
            ruling.AlternativeIds);
        Assert.Equal(
            "resolve-explicitly-admitted-empty-stage-entry",
            ruling.SelectedBehaviorId);
        Assert.Equal(
            [
                "STG-AC-001",
                "STG-AC-002",
                "STG-AC-004",
                "STG-AC-005",
                "STG-AC-006",
                "STG-AC-009",
                "STG-AC-010",
            ],
            ruling.ProtectingTestIds);
        Assert.Equal(2, ruling.Sources.Count);
        Assert.Contains(
            new RuleReference("spi-1979-land-rules", "5.2"),
            ruling.Sources);
        Assert.Contains(
            new RuleReference(
                "sandtable-rules-lab",
                "stage-entry.no-obligations.v1"),
            ruling.Sources);
    }

    [Fact]
    public void AcceptedStageEntryRulingHasExactCanonicalBytes()
    {
        var ruling = Assert.Single(
            Cna1979Ruleset.Manifest.Rulings,
            candidate => candidate.RulingId == RulingId);

        Assert.Equal(
            "{\"rulingId\":\"cna-1979.1.ruling.explicit-empty-stage-entry-resolution\","
            + "\"conflictId\":\"cna-1979.1.conflict.empty-stage-entry-phase\","
            + "\"alternativeIds\":[\"reject-empty-stage-entry-as-unsupported\","
            + "\"resolve-explicitly-admitted-empty-stage-entry\"],"
            + "\"selectedBehaviorId\":\"resolve-explicitly-admitted-empty-stage-entry\","
            + "\"protectingTestIds\":[\"STG-AC-001\",\"STG-AC-002\","
            + "\"STG-AC-004\",\"STG-AC-005\",\"STG-AC-006\","
            + "\"STG-AC-009\",\"STG-AC-010\"],"
            + "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\","
            + "\"locator\":\"stage-entry.no-obligations.v1\"},"
            + "{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"5.2\"}]}",
            Encoding.UTF8.GetString(RulesetManifest.SerializeCanonicalRuling(ruling)));
    }
}
