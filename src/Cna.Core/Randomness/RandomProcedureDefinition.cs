using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Randomness;

public sealed record RandomProcedureDefinition
{
    public RandomProcedureDefinition(
        int schemaVersion,
        string algorithmId,
        string domainAscii,
        byte separatorByte,
        string integerEncoding,
        int blockBytes,
        int d6AcceptBelow,
        int d6Modulo,
        int d6Offset,
        IReadOnlyList<string> initiativeDrawOrder,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithmId);
        ArgumentException.ThrowIfNullOrWhiteSpace(domainAscii);
        ArgumentException.ThrowIfNullOrWhiteSpace(integerEncoding);
        ArgumentOutOfRangeException.ThrowIfLessThan(blockBytes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(d6AcceptBelow, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(d6AcceptBelow, 256);
        ArgumentOutOfRangeException.ThrowIfLessThan(d6Modulo, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(d6Offset, 1);

        SchemaVersion = schemaVersion;
        AlgorithmId = algorithmId;
        DomainAscii = domainAscii;
        SeparatorByte = separatorByte;
        IntegerEncoding = integerEncoding;
        BlockBytes = blockBytes;
        D6AcceptBelow = d6AcceptBelow;
        D6Modulo = d6Modulo;
        D6Offset = d6Offset;
        InitiativeDrawOrder = CopyDrawOrder(initiativeDrawOrder);
        Sources = CopySources(sources);
    }

    public int SchemaVersion { get; }

    public string AlgorithmId { get; }

    public string DomainAscii { get; }

    public byte SeparatorByte { get; }

    public string IntegerEncoding { get; }

    public int BlockBytes { get; }

    public int D6AcceptBelow { get; }

    public int D6Modulo { get; }

    public int D6Offset { get; }

    public IReadOnlyList<string> InitiativeDrawOrder { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(RandomProcedureDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(AlgorithmId, other.AlgorithmId, StringComparison.Ordinal)
            && string.Equals(DomainAscii, other.DomainAscii, StringComparison.Ordinal)
            && SeparatorByte == other.SeparatorByte
            && string.Equals(IntegerEncoding, other.IntegerEncoding, StringComparison.Ordinal)
            && BlockBytes == other.BlockBytes
            && D6AcceptBelow == other.D6AcceptBelow
            && D6Modulo == other.D6Modulo
            && D6Offset == other.D6Offset
            && InitiativeDrawOrder.SequenceEqual(other.InitiativeDrawOrder)
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(AlgorithmId, StringComparer.Ordinal);
        hash.Add(DomainAscii, StringComparer.Ordinal);
        hash.Add(SeparatorByte);
        hash.Add(IntegerEncoding, StringComparer.Ordinal);
        hash.Add(BlockBytes);
        hash.Add(D6AcceptBelow);
        hash.Add(D6Modulo);
        hash.Add(D6Offset);

        foreach (var side in InitiativeDrawOrder)
        {
            hash.Add(side, StringComparer.Ordinal);
        }

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<string> CopyDrawOrder(
        IReadOnlyList<string> initiativeDrawOrder)
    {
        ArgumentNullException.ThrowIfNull(initiativeDrawOrder);
        var copy = initiativeDrawOrder.ToArray();

        if (copy.Length == 0 || copy.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one nonblank draw-order identifier is required.",
                nameof(initiativeDrawOrder));
        }

        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate draw-order identifiers are not allowed.",
                nameof(initiativeDrawOrder));
        }

        return Array.AsReadOnly(copy);
    }

    private static ReadOnlyCollection<RuleReference> CopySources(
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var copy = sources.ToArray();

        if (copy.Length == 0 || copy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                nameof(sources));
        }

        if (copy.Distinct().Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                nameof(sources));
        }

        return Array.AsReadOnly(copy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }
}
