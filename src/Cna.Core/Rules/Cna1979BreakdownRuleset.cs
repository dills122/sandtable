namespace Cna.Core.Rules;

internal static class Cna1979BreakdownRuleset
{
    public const int ContractVersion = 9;
    public static RulesetManifest Manifest { get; } = new(
        Cna1979Ruleset.RulesetId,
        ContractVersion,
        Cna1979Ruleset.HistoricalManifestV8.Artifacts.Select(artifact => artifact.ArtifactId switch
        {
            Cna1979LandSequenceV4.ArtifactId => Cna1979LandSequenceV4.CreateArtifact(),
            Cna1979Breakdown.ArtifactId => Cna1979BreakdownAdjudication.CreateArtifact(),
            _ => artifact,
        }),
        Cna1979Ruleset.HistoricalManifestV8.Rulings.Concat(Cna1979BreakdownAdjudication.CreateRulings()));

    public static bool IsCanonicalHash(string? hash) => string.Equals(hash, Manifest.Hash, StringComparison.Ordinal);
}
