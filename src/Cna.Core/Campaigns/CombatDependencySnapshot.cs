using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Owned, authenticated bytes from one complete, explicitly registered evidence revision.</summary>
internal sealed class CombatDependencySnapshot
{
    private readonly Dictionary<string, byte[]> bytes;
    private readonly IReadOnlyDictionary<string, string> pins;
    private readonly string error;
    private readonly Func<string, byte[]> source;

    private CombatDependencySnapshot(string revision, Dictionary<string, byte[]> bytes,
        IReadOnlyDictionary<string, string> pins, string error, Func<string, byte[]> source)
    {
        Revision = revision;
        this.bytes = bytes;
        this.pins = pins;
        this.error = error;
        this.source = source;
    }

    internal string Revision { get; }

    internal void Revalidate()
    {
        if (source.Target is CombatDependencySnapshot parent) parent.Revalidate();
        foreach (var (path, expected) in bytes)
        {
            try
            {
                var current = source(path);
                if (current is null || !expected.AsSpan().SequenceEqual(current)) throw new JsonException(error);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException)
            {
                throw new JsonException(error, exception);
            }
        }
    }

    internal byte[] Read(string path)
    {
        if (!bytes.TryGetValue(path, out var value)) throw new JsonException(error);
        return value.ToArray();
    }

    internal string HashFor(string path)
    {
        if (!pins.TryGetValue(path, out var value)) throw new JsonException(error);
        return value;
    }

    internal static CombatDependencySnapshot Capture(Func<string, byte[]> read,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> revisions,
        long maximumBytes, string error, string? requiredRevision = null)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (revisions.Count == 0) throw new ArgumentException("At least one frozen revision is required.", nameof(revisions));
        // Take ownership of registration metadata before invoking caller code.
        var owned = revisions.ToDictionary(r => r.Key,
            r => r.Value.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal), StringComparer.Ordinal);
        var paths = owned.First().Value.Keys.ToHashSet(StringComparer.Ordinal);
        if (owned.Any(r => !paths.SetEquals(r.Value.Keys)))
            throw new ArgumentException("Revisions must authenticate the same dependency paths.", nameof(revisions));
        if (read.Target is CombatDependencySnapshot parent)
        {
            if (requiredRevision is not null && requiredRevision != parent.Revision) throw new JsonException(error);
            requiredRevision = parent.Revision;
        }
        var candidates = owned.Where(r => requiredRevision is null || r.Key == requiredRevision).ToList();
        if (candidates.Count == 0) throw new JsonException(error);
        var acquired = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var path in paths.Order(StringComparer.Ordinal))
        {
            byte[] data;
            try
            {
                var returned = read(path);
                if (returned is null || returned.LongLength > maximumBytes) throw new JsonException(error);
                data = returned.ToArray();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException)
            {
                throw new JsonException(error, exception);
            }
            var hash = Convert.ToHexStringLower(SHA256.HashData(data));
            candidates.RemoveAll(r => !StringComparer.Ordinal.Equals(r.Value[path], hash));
            if (candidates.Count == 0) throw new JsonException(error);
            acquired.Add(path, data);
        }
        // Identical subsets use deterministic registration order; nested snapshots inherit their root revision.
        var selected = candidates[0];
        return new(selected.Key, acquired, selected.Value, error, read);
    }
}
