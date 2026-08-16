using System.Collections.ObjectModel;

namespace Cna.Core.Content;

public sealed record ContentPresentationCatalog
{
    public ContentPresentationCatalog(
        string packId,
        string displayName,
        string notice,
        IReadOnlyDictionary<string, string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        var labelCopy = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in labels)
        {
            labelCopy.Add(
                ContentContractGuards.RequireStableId(key, nameof(labels)),
                ContentContractGuards.RequirePresentationText(value, nameof(labels)));
        }

        PackId = ContentContractGuards.RequireStableId(packId, nameof(packId));
        DisplayName = ContentContractGuards.RequirePresentationText(displayName, nameof(displayName));
        Notice = ContentContractGuards.RequirePresentationText(notice, nameof(notice));
        Labels = new ReadOnlyDictionary<string, string>(labelCopy);
    }

    public string PackId { get; }

    public string DisplayName { get; }

    public string Notice { get; }

    public IReadOnlyDictionary<string, string> Labels { get; }

    public bool Equals(ContentPresentationCatalog? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(PackId, other.PackId, StringComparison.Ordinal)
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && string.Equals(Notice, other.Notice, StringComparison.Ordinal)
            && Labels.SequenceEqual(other.Labels));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PackId, StringComparer.Ordinal);
        hash.Add(DisplayName, StringComparer.Ordinal);
        hash.Add(Notice, StringComparer.Ordinal);

        foreach (var label in Labels)
        {
            hash.Add(label.Key, StringComparer.Ordinal);
            hash.Add(label.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
