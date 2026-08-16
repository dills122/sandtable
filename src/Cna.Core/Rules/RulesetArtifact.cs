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
        ArgumentNullException.ThrowIfNull(sources);

        ArtifactId = artifactId;
        ContentHash = contentHash;
        Sources = Array.AsReadOnly(sources.ToArray());

        if (Sources.Count == 0)
        {
            throw new ArgumentException("At least one source reference is required.", nameof(sources));
        }
    }

    public string ArtifactId { get; }

    public string ContentHash { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}
