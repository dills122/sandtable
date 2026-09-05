namespace Cna.Core.Content;

public sealed class ContentPackV6CatalogResolution
{
    private ContentPackV6CatalogResolution(ContentPackV6Artifact? artifact,
        ContentCatalogRejectionReason rejectionReason)
    {
        Artifact = artifact;
        RejectionReason = rejectionReason;
    }

    public bool IsResolved => Artifact is not null;
    public ContentPackV6Artifact? Artifact { get; }
    public ContentCatalogRejectionReason RejectionReason { get; }

    public static ContentPackV6CatalogResolution Resolved(ContentPackV6Artifact artifact) =>
        new(artifact ?? throw new ArgumentNullException(nameof(artifact)), ContentCatalogRejectionReason.None);

    public static ContentPackV6CatalogResolution Rejected(ContentCatalogRejectionReason reason)
    {
        if (reason == ContentCatalogRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
        return new(null, reason);
    }
}
