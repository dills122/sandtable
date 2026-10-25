using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Private, immutable evidence for one pinned A1 terminal cut; never an action authority.</summary>
public sealed class P01A1ValidatedHostImportV1
{
    private readonly byte[] importBytes;
    private readonly byte[] controlBytes;
    private readonly byte[] checkpointBytes;
    private readonly byte[] attestationBytes;

    internal P01A1ValidatedHostImportV1(string campaignId, byte[] import, byte[] control,
        byte[] checkpoint, byte[] attestation)
    {
        CampaignId = campaignId;
        importBytes = import.ToArray();
        controlBytes = control.ToArray();
        checkpointBytes = checkpoint.ToArray();
        attestationBytes = attestation.ToArray();
    }

    public string CampaignId { get; }
    public byte[] ImportBytes => importBytes.ToArray();
    public byte[] TerminalControlBytes => controlBytes.ToArray();
    public byte[] CheckpointBytes => checkpointBytes.ToArray();
    public byte[] SameCutAttestationBytes => attestationBytes.ToArray();
}

/// <summary>
/// Private physical-evidence adapter for the sole reviewed r012-c01 root. The input directories
/// select where independently pinned files live; they cannot select a row, rules, or expected hash.
/// </summary>
public static class P01A1PhysicalHostImportV1
{
    private const string ManifestHash = "39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35";
    private const string ChunkHash = "9f79444d6efe3577f7739b4e1dac363f20bb589e7ad0661e58d7293d3006f5cb";
    private const string CatalogueHash = "cf3f8ecfe9e4040d5678a6e0e9cc2441525e1e8fe3a3f35cf316207a9f45ec4e";
    private const string ImportGzipHash = "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa";
    private const string ImportHash = "486a221fd37af2ddef84086f2f3c416a13fad9f60ce92e3842998807c64ea971";
    private const string ControlGzipHash = "3c4eb5a37af03f5e07c03e38403fed371d802af51bdd0099552e5fc3f3037cf4";
    private const string ControlHash = "1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c";
    private const string AuthorityEnvelopeHash = "adf7b05e15f863b08f366809adf91c97b39480e404ce2efff15e838fa4043eaa";
    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static P01A1ValidatedHostImportV1 ValidatePhysical(string archiveDirectory,
        string trustedRepositoryDirectory)
    {
        RequireDirectory(archiveDirectory);
        RequireDirectory(trustedRepositoryDirectory);
        var manifest = PinnedFile(Path.Combine(archiveDirectory, "manifest.json"), ManifestHash, 8_192);
        using var manifestDocument = JsonDocument.Parse(manifest);
        var manifestRoot = manifestDocument.RootElement;
        Require(manifestRoot.GetProperty("counts").GetProperty("rows").GetInt32() == 300);
        Require(manifestRoot.GetProperty("counts").GetProperty("parentInputLedgersPerRow").GetInt32() == 11);
        Require(manifestRoot.GetProperty("counts").GetProperty("runtimeSourcePins").GetInt32() == 108);
        Require(manifestRoot.GetProperty("sourceHashes").GetProperty("nativeHashCatalogue").GetString() == CatalogueHash);
        var chunks = manifestRoot.GetProperty("chunks").EnumerateArray().Where(x =>
            x.GetProperty("name").GetString() == "rows-036-067.jsonl.gz").ToArray();
        Require(chunks.Length == 1 && chunks[0].GetProperty("sha256").GetString() == ChunkHash);
        var chunk = PinnedFile(Path.Combine(archiveDirectory, "rows-036-067.jsonl.gz"), ChunkHash, 4_000_000);
        var catalogueBytes = PinnedFile(Path.Combine(trustedRepositoryDirectory,
            "docs/specs/fixtures/combat-actual-moved-repeat-breakdown-native-hashes-v1.json"),
            CatalogueHash, 300_000);
        using var catalogue = JsonDocument.Parse(catalogueBytes);
        var frozenRows = catalogue.RootElement.GetProperty("rows").EnumerateArray().Where(x =>
            x.GetProperty("rowKey").GetString() == "r012-c01").ToArray();
        Require(frozenRows.Length == 1);
        JsonElement row = default;
        var matches = 0;
        using (var compressed = new GZipStream(new MemoryStream(chunk), CompressionMode.Decompress))
        using (var reader = new StreamReader(compressed, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
        {
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                Require(line.Length <= 2_500_000);
                using var document = JsonDocument.Parse(line);
                var candidate = document.RootElement.GetProperty("r03");
                if (candidate.GetProperty("rowKey").GetString() != "r012-c01") continue;
                row = candidate.Clone();
                matches++;
            }
        }
        Require(matches == 1 && row.GetProperty("rootOrdinal").GetInt32() == 12 &&
            row.GetProperty("movementCut").GetInt32() == 1);
        foreach (var property in frozenRows[0].EnumerateObject())
            Require(row.GetProperty(property.Name).GetRawText() == property.Value.GetRawText());
        var vectors = row.GetProperty("vectors");
        var ledgers = vectors.GetProperty("parentInputLedgersCanonicalUtf8").EnumerateArray().ToArray();
        Require(ledgers.Length == 11);
        var import = new P01R03ImportV1(
            JsonSerializer.SerializeToUtf8Bytes(ledgers[0], CanonicalOptions),
            ledgers.Skip(1).Select(x => x.EnumerateArray().Select(y => Encoding.ASCII.GetBytes(y.GetString()!)).ToArray()).ToArray(),
            [Encoding.ASCII.GetBytes(vectors.GetProperty("inputCanonicalUtf8").GetString()!)],
            Encoding.ASCII.GetBytes(vectors.GetProperty("sourceCanonicalUtf8").GetString()!)).Serialize();
        var control = Encoding.ASCII.GetBytes(vectors.GetProperty("afterControlCanonicalUtf8").GetString()!);
        Require(Hash(import) == ImportHash && Hash(control) == ControlHash &&
            row.GetProperty("afterControlHash").GetString() == "sha256:" + ControlHash);
        var fixtureImport = ReadPinnedGzip(Path.Combine(trustedRepositoryDirectory,
            "docs/specs/fixtures/p01-r03-a1-r012-c01-import-v1.json.gz"), ImportGzipHash, P01R03ImportV1.MaximumBytes);
        var fixtureControl = ReadPinnedGzip(Path.Combine(trustedRepositoryDirectory,
            "docs/specs/fixtures/p01-r03-a1-r012-c01-after-control-v1.json.gz"),
            ControlGzipHash, P01R03SameCutSourceAttestationCodecV1.MaximumControlBytes);
        Require(import.AsSpan().SequenceEqual(fixtureImport) && control.AsSpan().SequenceEqual(fixtureControl));
        return ValidateRetained(import, control, trustedRepositoryDirectory);
    }

    public static P01A1ValidatedHostImportV1 ValidateRetained(ReadOnlySpan<byte> import,
        ReadOnlySpan<byte> terminalControl, string trustedRepositoryDirectory)
    {
        RequireDirectory(trustedRepositoryDirectory);
        Require(Hash(import) == ImportHash && Hash(terminalControl) == ControlHash);
        var envelope = PinnedFile(Path.Combine(trustedRepositoryDirectory,
            "docs/specs/fixtures/combat-authority-envelope-v1.json"), AuthorityEnvelopeHash, 100_000);
        using var document = JsonDocument.Parse(envelope);
        using var created = JsonDocument.Parse(document.RootElement.GetProperty("goldens")
            .GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = created.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = artifact.Definition.Scenarios.Single();
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var configuration = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var context = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, configuration);
        byte[] Dependency(string path)
        {
            Require(!Path.IsPathRooted(path) && !path.Split('/', '\\').Contains("..", StringComparer.Ordinal));
            var file = Path.Combine(trustedRepositoryDirectory, path);
            return File.ReadAllBytes(file);
        }
        // This adapter is permanently bound to the original r012-c01 evidence bytes.
        using var legacy = new CombatLegacyDependencyArchive(Dependency);
        var checkpoint = P01R03ValidationCheckpointV1.Validate(import, context, legacy.Read);
        var attestation = P01R03SameCutSourceAttestationCodecV1.Attest(import, terminalControl, context, legacy.Read);
        using var checkpointDocument = JsonDocument.Parse(checkpoint.CanonicalBytes);
        var campaignId = checkpointDocument.RootElement.GetProperty("campaignId").GetString()!;
        using var originalImport = JsonDocument.Parse(import.ToArray());
        var creationRequest = CampaignCombatCreationRequestCodec.Deserialize(
            Encoding.ASCII.GetBytes(originalImport.RootElement.GetProperty("creationRequestCanonicalUtf8").GetString()!), context);
        Require(creationRequest.CampaignId == campaignId && !checkpoint.Admitted && !attestation.Admitted);
        return new P01A1ValidatedHostImportV1(campaignId, import.ToArray(), terminalControl.ToArray(),
            checkpoint.CanonicalBytes, attestation.CanonicalBytes);
    }

    private static byte[] ReadPinnedGzip(string path, string expectedHash, int maximumBytes)
    {
        var source = PinnedFile(path, expectedHash, 4_000_000);
        using var input = new GZipStream(new MemoryStream(source), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        Require(output.Length is > 0 && output.Length <= maximumBytes);
        return output.ToArray();
    }

    private static byte[] PinnedFile(string path, string expectedHash, int maximumBytes)
    {
        var info = new FileInfo(path);
        Require(info.Exists && info.Length is > 0 && info.Length <= maximumBytes);
        var bytes = File.ReadAllBytes(path);
        Require(Hash(bytes) == expectedHash);
        return bytes;
    }

    private static void RequireDirectory(string path)
    {
        if (!Path.IsPathFullyQualified(path) || !Directory.Exists(path))
            throw new JsonException("P01-A1H-001");
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static void Require(bool condition)
    {
        if (!condition) throw new JsonException("P01-A1H-001");
    }
}
