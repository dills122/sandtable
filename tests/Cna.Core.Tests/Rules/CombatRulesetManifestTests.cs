using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class CombatRulesetManifestTests
{
    [Fact]
    public void Rules10MatchesFrozenCanonicalManifestAndLeavesRules9Unchanged()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        var golden = fixture.RootElement.GetProperty("goldens").GetProperty("manifest");
        var bytes = Encoding.UTF8.GetBytes(golden.GetProperty("canonicalUtf8").GetString()!);

        Assert.Equal(10_495, bytes.Length);
        Assert.Equal(11, Cna1979CombatRuleset.Manifest.Artifacts.Count);
        Assert.Equal(11, Cna1979CombatRuleset.Manifest.Rulings.Count);
        Assert.Equal(golden.GetProperty("sha256").GetString(),
            "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant());
        Assert.Equal("8af256c6c2bbf71cb72ea7e29922db9c4c897c2535121a68a6849bbd06e0bd18",
            Cna1979CombatRuleset.Manifest.Hash);
        Assert.Equal("17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a",
            Cna1979Ruleset.Manifest.Hash);
    }

    [Fact]
    public void Rules10RejectsHistoricalAndAlteredManifestIdentities()
    {
        Assert.True(Cna1979CombatRuleset.IsCanonicalHash(Cna1979CombatRuleset.Manifest.Hash));
        Assert.False(Cna1979CombatRuleset.IsCanonicalHash(Cna1979Ruleset.Manifest.Hash));
        Assert.False(Cna1979CombatRuleset.IsCanonicalHash(Cna1979CombatRuleset.Manifest.Hash.ToUpperInvariant()));
        Assert.False(Cna1979CombatRuleset.IsCanonicalHash("sha256:" + Cna1979CombatRuleset.Manifest.Hash));

        var missingArtifact = new RulesetManifest(Cna1979Ruleset.RulesetId, 10,
            Cna1979CombatRuleset.Manifest.Artifacts.Skip(1), Cna1979CombatRuleset.Manifest.Rulings);
        Assert.False(Cna1979CombatRuleset.IsCanonicalHash(missingArtifact.Hash));
        var missingRuling = new RulesetManifest(Cna1979Ruleset.RulesetId, 10,
            Cna1979CombatRuleset.Manifest.Artifacts, Cna1979CombatRuleset.Manifest.Rulings.Skip(1));
        Assert.False(Cna1979CombatRuleset.IsCanonicalHash(missingRuling.Hash));
        var alteredArtifact = new RulesetManifest(Cna1979Ruleset.RulesetId, 10,
            Cna1979CombatRuleset.Manifest.Artifacts.Select(artifact =>
                artifact.ArtifactId == "cna-1979.1.combat-selected-inputs.v1"
                    ? new RulesetArtifact(artifact.ArtifactId, "sha256:" + new string('0', 64), artifact.Sources)
                    : artifact), Cna1979CombatRuleset.Manifest.Rulings);
        Assert.False(Cna1979CombatRuleset.IsCanonicalHash(alteredArtifact.Hash));
    }
}
