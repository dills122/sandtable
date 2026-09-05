using System.Security.Cryptography;

namespace Cna.Core.Content;

public sealed record ContentPackV6Identity
{
    public ContentPackV6Identity(
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        string hash)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            schemaVersion,
            ContentPackV6Definition.SchemaVersion);
        if (!string.Equals(
            formatId,
            ContentPackV6Definition.CanonicalFormatId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The only supported v6 format is '{ContentPackV6Definition.CanonicalFormatId}'.",
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

public sealed class ContentPackV6Artifact
{
    private readonly byte[] canonicalBytes;

    private ContentPackV6Artifact(ContentPackV6Definition definition, byte[] canonicalBytes)
    {
        Definition = definition;
        this.canonicalBytes = canonicalBytes.ToArray();
        Identity = new ContentPackV6Identity(
            ContentPackV6Definition.SchemaVersion,
            ContentPackV6Definition.CanonicalFormatId,
            definition.PackId,
            definition.RulesetId,
            $"sha256:{Convert.ToHexString(SHA256.HashData(this.canonicalBytes)).ToLowerInvariant()}");
    }

    public ContentPackV6Definition Definition { get; }

    public ContentPackV6Identity Identity { get; }

    public int CanonicalByteCount => canonicalBytes.Length;

    public static ContentPackV6Artifact Create(ContentPackV6Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ContentPackV6Artifact(
            definition,
            ContentPackV6Serializer.SerializeCanonical(definition));
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
