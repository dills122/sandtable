using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Explicit original-evidence dependency reader; current evidence never selects it implicitly.</summary>
internal sealed class CombatLegacyDependencyArchive : IDisposable
{
    internal const string ArchivePath = "docs/specs/fixtures/combat-evidence-legacy-v1.zip";
    private const string ArchiveHash = "3d7614b580ad5c82727170e4ff193e03f4931afbc7e7ed7b14d5eb49c45a6a98";
    private readonly ZipArchive archive;
    private readonly Func<string, byte[]> unchangedDependency;
    private readonly Dictionary<string, byte[]> cache = new(StringComparer.Ordinal);

    internal CombatLegacyDependencyArchive(Func<string, byte[]> read)
    {
        var returned = read(ArchivePath);
        if (returned is null) throw new JsonException("Missing original evidence dependency archive.");
        var bytes = returned.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != ArchiveHash)
            throw new JsonException("Invalid original evidence dependency archive.");
        archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        unchangedDependency = read;
    }

    internal byte[] Read(string path)
    {
        if (Path.IsPathRooted(path) || path.Split('/', '\\').Contains("..", StringComparer.Ordinal))
            throw new JsonException("Invalid original evidence dependency path.");
        lock (cache)
        {
            if (!cache.TryGetValue(path, out var bytes))
            {
                var entry = archive.GetEntry(path);
                if (entry is null) bytes = unchangedDependency(path).ToArray();
                else
                {
                    if (entry.Length > 134_217_728) throw new JsonException("Oversized original evidence dependency.");
                    using var input = entry.Open();
                    using var output = new MemoryStream();
                    input.CopyTo(output);
                    bytes = output.ToArray();
                }
                cache.Add(path, bytes);
            }
            return bytes.ToArray();
        }
    }

    public void Dispose() => archive.Dispose();
}
