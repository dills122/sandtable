namespace Cna.Core.Rules;

public sealed record RulesetArtifact
{
    public RulesetArtifact(
        string artifactId,
        string contentHash,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        ArtifactId = artifactId;
        ContentHash = contentHash;
        Sources = RuleReferenceValidation.CopySources(
            sources,
            nameof(sources),
            sortAndDeduplicate: false);
    }

    public string ArtifactId { get; }

    public string ContentHash { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}
