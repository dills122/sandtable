using System.Security.Cryptography;

namespace Cna.Core.Content;

public sealed record ContentPackIdentity
{
    public ContentPackIdentity(
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        string hash)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            schemaVersion,
            ContentPackDefinition.CurrentSchemaVersion);

        if (!string.Equals(
            formatId,
            ContentPackDefinition.CanonicalFormatId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The only supported format is '{ContentPackDefinition.CanonicalFormatId}'.",
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

public sealed class InvalidContentPackException : Exception
{
    public InvalidContentPackException(IEnumerable<ContentValidationIssue> issues)
        : base("Content Pack validation failed; canonical bytes and identity are unavailable.")
    {
        Issues = new ContentValidationResult(issues).Issues;
    }

    public IReadOnlyList<ContentValidationIssue> Issues { get; }
}

public sealed class ContentPackArtifact
{
    private readonly byte[] canonicalBytes;

    private ContentPackArtifact(ContentPackDefinition definition, byte[] canonicalBytes)
    {
        Definition = definition;
        this.canonicalBytes = canonicalBytes.ToArray();
        Identity = new ContentPackIdentity(
            definition.SchemaVersion,
            definition.FormatId,
            definition.PackId,
            definition.RulesetId,
            $"sha256:{Convert.ToHexString(SHA256.HashData(this.canonicalBytes)).ToLowerInvariant()}");
    }

    public ContentPackDefinition Definition { get; }

    public ContentPackIdentity Identity { get; }

    public int CanonicalByteCount => canonicalBytes.Length;

    public static ContentPackArtifact Create(ContentPackDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ContentPackArtifact(
            definition,
            ContentPackSerializer.SerializeCanonical(definition));
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
