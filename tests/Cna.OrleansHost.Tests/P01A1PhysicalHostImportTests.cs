using System.Security.Cryptography;
using System.Text;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed class P01A1PhysicalHostImportTests
{
    private const string Campaign = "rules-lab.combat-creation.1";
    private const string Operator = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void FailedPhysicalSelectionLeavesNoCampaignOrImport()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        Assert.ThrowsAny<Exception>(() => store.ImportPhysicalA1(
            Path.Combine(fixture.Directory, "missing-archive"), Repository(), Operator));
        Assert.Null(store.ReadHeadForTest(Campaign));
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM a1_imports";
        Assert.Equal(0L, (long)command.ExecuteScalar()!);
    }

    [Fact]
    public void ForgedRetainedBytesAndCampaignBindingCannotReadBack()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign,
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", Operator);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO a1_imports VALUES ($campaign, x'01', x'02', x'03', x'04')
                """;
            command.Parameters.AddWithValue("$campaign", Campaign);
            command.ExecuteNonQuery();
        }
        Assert.Throws<InvalidOperationException>(() => fixture.Open().ReadPhysicalA1(Campaign, Repository()));
        Assert.Throws<InvalidOperationException>(() => fixture.Open().AcquireActivationFence(Campaign, Operator));
    }

    [Theory]
    [InlineData("import_bytes", 2_097_153)]
    [InlineData("control_bytes", 65_537)]
    [InlineData("checkpoint_bytes", 2_097_153)]
    [InlineData("attestation_bytes", 4_097)]
    public void OversizedRetainedBlobIsRejectedBeforeMaterialization(string column, int length)
    {
        const string checkpoint = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign, checkpoint, Operator);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE campaigns SET origin_kind='physical-a1', origin_binding=$binding
                WHERE campaign_id=$campaign;
                INSERT INTO a1_imports VALUES ($campaign, x'01', x'02', x'03', x'04')
                """;
            command.Parameters.AddWithValue("$campaign", Campaign);
            command.Parameters.AddWithValue("$binding", PhysicalOriginBinding(checkpoint));
            Assert.Equal(2, command.ExecuteNonQuery());
            command.Parameters.Clear();
            command.CommandText = $"UPDATE a1_imports SET {column}=zeroblob($length) WHERE campaign_id=$campaign";
            command.Parameters.AddWithValue("$length", length);
            command.Parameters.AddWithValue("$campaign", Campaign);
            Assert.Equal(1, command.ExecuteNonQuery());
        }
        var error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Open().ReadPhysicalA1(Campaign, Repository()));
        Assert.Equal("Private A1 import blob exceeds limit.", error.Message);
    }

    [Fact]
    public void MissingImportedEvidenceCannotBecomeAnIssuableSeed()
    {
        const string checkpoint = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign, checkpoint, Operator);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE campaigns SET origin_kind='physical-a1', origin_binding=$binding
                WHERE campaign_id=$campaign;
                INSERT INTO a1_imports VALUES ($campaign, x'01', x'02', x'03', x'04');
                DELETE FROM a1_imports WHERE campaign_id=$campaign;
                """;
            command.Parameters.AddWithValue("$campaign", Campaign);
            command.Parameters.AddWithValue("$binding", PhysicalOriginBinding(checkpoint));
            command.ExecuteNonQuery();
        }
        Assert.NotNull(fixture.Open().ReadHeadForTest(Campaign));
        Assert.Equal("Physical A1 evidence is missing.",
            Assert.Throws<InvalidOperationException>(() => fixture.Open()
                .ReadPhysicalA1(Campaign, Repository())).Message);
        Assert.Equal("Imported A1 campaign is unissuable.",
            Assert.Throws<InvalidOperationException>(() => fixture.Open()
                .AcquireActivationFence(Campaign, Operator)).Message);
        var noLease = new P01ActivationLease(Campaign, 0, Operator, Operator);
        Assert.Equal("Imported A1 campaign is unissuable.",
            Assert.Throws<InvalidOperationException>(() => fixture.Open()
                .IssueGrant(Campaign, "axis", noLease, Operator)).Message);
    }

    [Fact]
    public void ChangedImportOriginWithoutMatchingBindingCannotActivate()
    {
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.SeedUnissuableCampaign(Campaign,
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", Operator);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE campaigns SET origin_kind='physical-a1' WHERE campaign_id=$campaign";
        command.Parameters.AddWithValue("$campaign", Campaign);
        Assert.Equal(1, command.ExecuteNonQuery());
        Assert.Equal("Private campaign origin binding changed.",
            Assert.Throws<InvalidOperationException>(() => fixture.Open()
                .AcquireActivationFence(Campaign, Operator)).Message);
    }

    // Physical source remains outside the repository. CI exercises retained-byte revalidation
    // and DB rejection above; the pinned archive path enables this additional local proof.
    [Fact]
    public void RetainedCoreProofAndConfiguredPhysicalArchiveImport()
    {
        // Hosted CI has only the pinned retained fixtures; a local archive adds the physical gate.
        var importGzip = File.ReadAllBytes(System.IO.Path.Combine(Repository(),
            "docs/specs/fixtures/p01-r03-a1-r012-c01-import-v1.json.gz"));
        var controlGzip = File.ReadAllBytes(System.IO.Path.Combine(Repository(),
            "docs/specs/fixtures/p01-r03-a1-r012-c01-after-control-v1.json.gz"));
        static byte[] Decompress(byte[] bytes)
        {
            using var gzip = new System.IO.Compression.GZipStream(new MemoryStream(bytes),
                System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream(); gzip.CopyTo(output); return output.ToArray();
        }
        Assert.Equal(Campaign, Cna.Core.Campaigns.P01A1PhysicalHostImportV1.ValidateRetained(
            Decompress(importGzip), Decompress(controlGzip), Repository()).CampaignId);
        var archive = Environment.GetEnvironmentVariable("SANDTABLE_P01_PHYSICAL_ARCHIVE");
        if (archive is null) return;
        using var fixture = new StoreFixture();
        var store = fixture.Create();
        store.ImportPhysicalA1(archive, Repository(), Operator);
        var before = store.ReadPhysicalA1(Campaign, Repository())!;
        Assert.Equal(Campaign, before.CampaignId);
        Assert.Equal(0, store.ReadHeadForTest(Campaign)!.Sequence);
        Assert.Throws<InvalidOperationException>(() => store.AcquireActivationFence(Campaign, Operator));
        var after = fixture.Open().ReadPhysicalA1(Campaign, Repository())!;
        Assert.Equal(before.ImportBytes, after.ImportBytes);
        Assert.Equal(before.CheckpointBytes, after.CheckpointBytes);
        Assert.Equal(before.SameCutAttestationBytes, after.SameCutAttestationBytes);
        Assert.Null(fixture.Open().ReadPhysicalA1("rules-lab.other", Repository()));
        Assert.Throws<SqliteException>(() => fixture.Open().ImportPhysicalA1(archive, Repository(), Operator));
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM a1_imports";
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
        var corruptedArchive = System.IO.Path.Combine(fixture.Directory, "altered-archive");
        System.IO.Directory.CreateDirectory(corruptedArchive);
        var manifest = File.ReadAllBytes(System.IO.Path.Combine(archive, "manifest.json"));
        manifest[^2] ^= 1;
        File.WriteAllBytes(System.IO.Path.Combine(corruptedArchive, "manifest.json"), manifest);
        Assert.ThrowsAny<Exception>(() => store.ImportPhysicalA1(corruptedArchive,
            Repository(), Operator));
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
        using (var mutation = connection.CreateCommand())
        {
            mutation.CommandText = "UPDATE a1_imports SET import_bytes=x'01' WHERE campaign_id=$campaign";
            mutation.Parameters.AddWithValue("$campaign", Campaign);
            Assert.Equal(1, mutation.ExecuteNonQuery());
        }
        Assert.ThrowsAny<Exception>(() => fixture.Open().ReadPhysicalA1(Campaign, Repository()));
        using (var deletion = connection.CreateCommand())
        {
            deletion.CommandText = "DELETE FROM a1_imports WHERE campaign_id=$campaign";
            deletion.Parameters.AddWithValue("$campaign", Campaign);
            Assert.Equal(1, deletion.ExecuteNonQuery());
        }
        Assert.Equal("Physical A1 evidence is missing.",
            Assert.Throws<InvalidOperationException>(() => fixture.Open()
                .ReadPhysicalA1(Campaign, Repository())).Message);
        Assert.Throws<InvalidOperationException>(() => fixture.Open()
            .AcquireActivationFence(Campaign, Operator));
    }

    private static string Repository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository not found.");
    }

    private static string PhysicalOriginBinding(string checkpointHash)
    {
        var domain = Encoding.ASCII.GetBytes("sandtable.p01.campaign-origin.v1\0");
        var fields = Encoding.UTF8.GetBytes(string.Join('\0', Campaign, checkpointHash, "physical-a1"));
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(domain.Concat(fields).ToArray()));
    }

    private sealed class StoreFixture : IDisposable
    {
        internal string Directory { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-p01-a1-import-" + Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(Directory, "authority.sqlite");
        internal P01PrivateHostFoundation Create() => P01PrivateHostFoundation.CreateNew(Path, Operator);
        internal P01PrivateHostFoundation Open() => P01PrivateHostFoundation.OpenExisting(Path);
        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
