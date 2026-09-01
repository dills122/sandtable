using System.Security.Cryptography;

namespace Cna.Core.Content;

public sealed record ContentPackV5Identity
{
    public ContentPackV5Identity(
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        string hash)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            schemaVersion,
            ContentPackV5Definition.SchemaVersion);
        if (!string.Equals(
            formatId,
            ContentPackV5Definition.CanonicalFormatId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The only supported v5 format is '{ContentPackV5Definition.CanonicalFormatId}'.",
                nameof(formatId));
        }

        SchemaVersion = schemaVersion;
        FormatId = formatId;
        PackId = ContentContractGuards.RequireStableId(packId, nameof(packId));
        RulesetId = ContentContractGuards.RequireStableId(rulesetId, nameof(rulesetId));
        Hash = ContentContractGuards.RequireSha256(hash, nameof(hash));
    }

    public int SchemaVersion { get; }

    public string FormatId { get; }

    public string PackId { get; }

    public string RulesetId { get; }

    public string Hash { get; }
}

public sealed class ContentPackV5Artifact
{
    private readonly byte[] canonicalBytes;

    private ContentPackV5Artifact(ContentPackV5Definition definition, byte[] canonicalBytes)
    {
        Definition = definition;
        this.canonicalBytes = canonicalBytes.ToArray();
        Identity = new ContentPackV5Identity(
            ContentPackV5Definition.SchemaVersion,
            ContentPackV5Definition.CanonicalFormatId,
            definition.PackId,
            definition.RulesetId,
            $"sha256:{Convert.ToHexString(SHA256.HashData(this.canonicalBytes)).ToLowerInvariant()}");
    }

    public ContentPackV5Definition Definition { get; }

    public ContentPackV5Identity Identity { get; }

    public int CanonicalByteCount => canonicalBytes.Length;

    public static ContentPackV5Artifact Create(ContentPackV5Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ContentPackV5Artifact(
            definition,
            ContentPackV5Serializer.SerializeCanonical(definition));
    }

    public byte[] GetCanonicalBytes() => canonicalBytes.ToArray();

    public void CopyCanonicalBytes(Span<byte> destination)
    {
        if (destination.Length != canonicalBytes.Length)
        {
            throw new ArgumentException(
                $"Destination length must be exactly {canonicalBytes.Length} bytes.",
                nameof(destination));
        }

        canonicalBytes.CopyTo(destination);
    }
}
