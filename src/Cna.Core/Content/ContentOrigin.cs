using Cna.Core.Rules;

namespace Cna.Core.Content;

public enum ContentOriginKind
{
    SourceDerived,
    Synthetic,
}

public enum ContentSourceKind
{
    PublishedPrimary,
    AdoptedRuling,
    RepositorySynthetic,
}

public sealed record ContentSourceIndexEntry
{
    public ContentSourceIndexEntry(string sourceId, ContentSourceKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        SourceId = ContentContractGuards.RequireStableId(sourceId, nameof(sourceId));
        Kind = kind;
    }

    public string SourceId { get; }

    public ContentSourceKind Kind { get; }
}

public sealed record ContentOrigin
{
    public ContentOrigin(ContentOriginKind kind, IEnumerable<RuleReference> references)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var referenceCopy = ContentContractGuards.CopyValues(references, nameof(references));

        if (referenceCopy.Length == 0)
        {
            throw new ArgumentException(
                "At least one origin reference is required.",
                nameof(references));
        }

        foreach (var reference in referenceCopy)
        {
            ContentContractGuards.RequireStableId(reference.SourceId, nameof(references));
            ContentContractGuards.RequireSourceAtom(reference.Locator, nameof(references));
        }

        if (referenceCopy.Distinct().Count() != referenceCopy.Length)
        {
            throw new ArgumentException(
                "Duplicate origin references are not allowed.",
                nameof(references));
        }

        Kind = kind;
        References = Array.AsReadOnly(referenceCopy
            .OrderBy(reference => reference.SourceId, StringComparer.Ordinal)
            .ThenBy(reference => reference.Locator, StringComparer.Ordinal)
            .ToArray());
    }

    public ContentOriginKind Kind { get; }

    public IReadOnlyList<RuleReference> References { get; }

    public bool Equals(ContentOrigin? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Kind == other.Kind
            && References.SequenceEqual(other.References));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);

        foreach (var reference in References)
        {
            hash.Add(reference);
        }

        return hash.ToHashCode();
    }
}
