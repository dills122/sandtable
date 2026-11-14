using System.Security.Cryptography;

namespace Cna.Core.Content;

/// <summary>Canonical source data. It does not certify playable rules or source review.</summary>
public sealed class ContentSourceScheduledCombatPackArtifact
{
    private readonly byte[] canonicalBytes;

    private ContentSourceScheduledCombatPackArtifact(ContentPackV5Definition definition, byte[] bytes)
    {
        Definition = definition;
        canonicalBytes = bytes.ToArray();
        Identity = new ContentScheduledPackIdentity(ContentScheduledCombatPackSerializer.SourceFormat,
            definition.LegacyDefinition.PackId, definition.LegacyDefinition.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    public ContentPackV5Definition Definition { get; }
    public ContentScheduledPackIdentity Identity { get; }
    public byte[] CanonicalBytes => canonicalBytes.ToArray();

    public static ContentSourceScheduledCombatPackArtifact Create(ContentPackV5Definition definition) =>
        new(definition, ContentScheduledCombatPackSerializer.SerializeSourceCanonical(definition));

    public static ContentSourceScheduledCombatPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var retained = bytes.ToArray();
        return new(ContentScheduledCombatPackSerializer.DeserializeSource(retained), retained);
    }

    public static ContentSourceScheduledCombatPackArtifact Read(byte[] bytes, ContentScheduledPackIdentity expectedIdentity)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(expectedIdentity);
        var artifact = Read(bytes);
        if (artifact.Identity != expectedIdentity)
            throw new InvalidOperationException("Source content identity does not match the expected identity.");
        return artifact;
    }
}
