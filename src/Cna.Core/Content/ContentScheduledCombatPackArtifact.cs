namespace Cna.Core.Content;

// Canonical source content, not an admitted campaign capability.
public sealed class ContentScheduledCombatPackArtifact
{
    private readonly byte[] canonicalBytes;

    private ContentScheduledCombatPackArtifact(ContentPackV5Definition definition, byte[] bytes)
    {
        Definition = definition;
        canonicalBytes = bytes.ToArray();
        Identity = new ContentScheduledPackIdentity(ContentScheduledCombatPackSerializer.Format,
            definition.LegacyDefinition.PackId, definition.LegacyDefinition.RulesetId,
            ContentScheduledCombatPackSerializer.CalculateHash(definition));
    }

    public ContentPackV5Definition Definition { get; }
    public ContentScheduledPackIdentity Identity { get; }
    public byte[] CanonicalBytes => canonicalBytes.ToArray();

    public static ContentScheduledCombatPackArtifact Create(ContentPackV5Definition definition) =>
        new(definition, ContentScheduledCombatPackSerializer.SerializeCanonical(definition));

    public static ContentScheduledCombatPackArtifact Read(byte[] bytes, ContentScheduledPackIdentity expectedIdentity)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(expectedIdentity);
        var definition = ContentScheduledCombatPackSerializer.Deserialize(bytes);
        var artifact = new ContentScheduledCombatPackArtifact(definition, bytes);
        if (artifact.Identity != expectedIdentity)
            throw new InvalidOperationException("Scheduled combat content identity does not match the expected identity.");
        return artifact;
    }
}
