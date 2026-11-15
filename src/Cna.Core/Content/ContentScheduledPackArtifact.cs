namespace Cna.Core.Content;

public sealed record ContentScheduledPackIdentity(string FormatId, string PackId, string RulesetId, string Hash);

// Content identity is not campaign admission or evidence of source approval.
public sealed class ContentScheduledPackArtifact
{
    private readonly byte[] canonicalBytes;

    private ContentScheduledPackArtifact(ContentPackDefinition definition, byte[] bytes)
    {
        Definition = definition;
        canonicalBytes = bytes.ToArray();
        Identity = new ContentScheduledPackIdentity(ContentScheduledPackSerializer.Format,
            definition.PackId, definition.RulesetId, ContentScheduledPackSerializer.CalculateHash(definition));
    }

    public ContentPackDefinition Definition { get; }
    public ContentScheduledPackIdentity Identity { get; }
    public byte[] CanonicalBytes => canonicalBytes.ToArray();

    public static ContentScheduledPackArtifact Create(ContentPackDefinition definition) =>
        new(definition, ContentScheduledPackSerializer.SerializeCanonical(definition));

    public static ContentScheduledPackArtifact Read(byte[] bytes, ContentScheduledPackIdentity expectedIdentity)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(expectedIdentity);
        var definition = ContentScheduledPackSerializer.Deserialize(bytes);
        var artifact = new ContentScheduledPackArtifact(definition, bytes);
        if (artifact.Identity != expectedIdentity)
            throw new InvalidOperationException("Scheduled content identity does not match the expected identity.");
        return artifact;
    }
}
