using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class P01A1PhysicalHostImportTests
{
    [Fact]
    public void RetainedPinnedBytesRevalidateWithoutOpeningAnAction()
    {
        var import = Fixture("p01-r03-a1-r012-c01-import-v1.json.gz",
            "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa");
        var control = Fixture("p01-r03-a1-r012-c01-after-control-v1.json.gz",
            "3c4eb5a37af03f5e07c03e38403fed371d802af51bdd0099552e5fc3f3037cf4");
        var validated = P01A1PhysicalHostImportV1.ValidateRetained(import, control, Repository());
        Assert.Equal("rules-lab.combat-creation.1", validated.CampaignId);
        Assert.Equal(import, validated.ImportBytes);
        Assert.Equal(control, validated.TerminalControlBytes);
        using var checkpoint = JsonDocument.Parse(validated.CheckpointBytes);
        using var sameCut = JsonDocument.Parse(validated.SameCutAttestationBytes);
        Assert.False(checkpoint.RootElement.GetProperty("admitted").GetBoolean());
        Assert.False(sameCut.RootElement.GetProperty("admitted").GetBoolean());
        Assert.Equal("sha256:" + Convert.ToHexStringLower(SHA256.HashData(validated.CheckpointBytes)),
            sameCut.RootElement.GetProperty("a1CheckpointHash").GetString());
        validated.ImportBytes[0] ^= 1;
        Assert.Equal(import, P01A1PhysicalHostImportV1.ValidateRetained(import, control, Repository()).ImportBytes);
    }

    [Fact]
    public void RetainedBytesAndForeignRootsFailClosed()
    {
        var import = Fixture("p01-r03-a1-r012-c01-import-v1.json.gz",
            "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa");
        var control = Fixture("p01-r03-a1-r012-c01-after-control-v1.json.gz",
            "3c4eb5a37af03f5e07c03e38403fed371d802af51bdd0099552e5fc3f3037cf4");
        import[100] ^= 1;
        Assert.Equal("P01-A1H-001", Assert.Throws<JsonException>(() =>
            P01A1PhysicalHostImportV1.ValidateRetained(import, control, Repository())).Message);
        import[100] ^= 1;
        control[100] ^= 1;
        Assert.Equal("P01-A1H-001", Assert.Throws<JsonException>(() =>
            P01A1PhysicalHostImportV1.ValidateRetained(import, control, Repository())).Message);
        Assert.Equal("P01-A1H-001", Assert.Throws<JsonException>(() =>
            P01A1PhysicalHostImportV1.ValidateRetained(import, control, "relative/path")).Message);
    }

    private static byte[] Fixture(string name, string hash)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Repository(), "docs/specs/fixtures", name));
        Assert.Equal(hash, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        using var gzip = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
        using var target = new MemoryStream();
        gzip.CopyTo(target);
        return target.ToArray();
    }
}
