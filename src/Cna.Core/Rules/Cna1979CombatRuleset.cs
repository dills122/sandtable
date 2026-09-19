namespace Cna.Core.Rules;

/// <summary>The frozen, unregistered Combat contract10 manifest.</summary>
internal static class Cna1979CombatRuleset
{
    public const int ContractVersion = 10;

    public static RulesetManifest Manifest { get; } = new(
        Cna1979Ruleset.RulesetId,
        ContractVersion,
        Cna1979Ruleset.Manifest.Artifacts.Select(artifact => artifact.ArtifactId switch
        {
            "cna-1979.1.land-sequence" => new RulesetArtifact(artifact.ArtifactId,
                "sha256:9f0828f10511fa49bd650c51eb5d5909637aa663b0593c712a2740a0b670ceb1",
                [
                    new RuleReference("spi-1979-land-rules", "21.24-21.26"),
                    new RuleReference("spi-1979-land-rules", "5.2"),
                    new RuleReference("spi-1979-land-rules", "7.11"),
                    new RuleReference("spi-1979-land-rules", "7.14"),
                ]),
            _ => artifact,
        }).Concat(
        [
            new RulesetArtifact("cna-1979.1.combat-selected-inputs.v1",
                "sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029",
                [
                    new RuleReference("sandtable-rules-lab", "CMB-POL-001-008;CMB-SRC-RUL-001"),
                    new RuleReference("spi-1979-common-charts", "15.79;15.89;17.4"),
                    new RuleReference("spi-1979-compilation", "Logistics50.0-50.17"),
                    new RuleReference("spi-1979-land-rules", "6.21-6.24;11.21-11.27;15.61-15.87;20.21;28.24"),
                    new RuleReference("spi-1979-september-errata", "15.27;20.72;50.2;50.12"),
                ]),
            new RulesetArtifact("cna-1979.1.cycle-identity.v1",
                "sha256:6b442b9a9b6d49ea8629bb47b048e8bebcda14dbd6a6cf84596aef2d5629e9dc",
                [
                    new RuleReference("sandtable-rules-lab", "CYCLE-DES-001:CYCLE-COMP-001-002"),
                    new RuleReference("spi-1979-land-rules", "5.2"),
                    new RuleReference("spi-1979-land-rules", "7.11"),
                    new RuleReference("spi-1979-land-rules", "7.14"),
                ]),
        ]),
        Cna1979Ruleset.Manifest.Rulings.Append(new Ruling(
            "cna-1979.1.ruling.combat-source-gap", "CMB-SRC-GAP-001",
            ["defer-positive-assault", "fill-defender-plus2-34-35-36-with10"],
            "fill-defender-plus2-34-35-36-with10", ["combat-envelope.source-ruling"],
            [
                new RuleReference("sandtable-rules-lab", "CMB-SRC-RUL-001"),
                new RuleReference("spi-1979-common-charts", "15.79"),
            ])));

    public static bool IsCanonicalHash(string? hash) =>
        string.Equals(hash, Manifest.Hash, StringComparison.Ordinal);
}
