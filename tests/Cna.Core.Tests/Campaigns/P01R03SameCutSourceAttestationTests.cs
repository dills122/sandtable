using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class P01R03SameCutSourceAttestationTests
{
    private const string ImportPath = "docs/specs/fixtures/p01-r03-a1-r012-c01-import-v1.json.gz";
    private const string ControlPath = "docs/specs/fixtures/p01-r03-a1-r012-c01-after-control-v1.json.gz";
    private const string ImportFileHash = "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa";
    private const string ControlFileHash = "3c4eb5a37af03f5e07c03e38403fed371d802af51bdd0099552e5fc3f3037cf4";

    [Fact]
    public void AuthenticatedTerminalCutAttestsPrivateSourceFactsOnly()
    {
        var import = ReadFixture(ImportPath, ImportFileHash);
        var control = ReadFixture(ControlPath, ControlFileHash);
        var first = P01R03SameCutSourceAttestationCodecV1.Attest(import, control, Context(), Dependency);
        var second = P01R03SameCutSourceAttestationCodecV1.Attest(import, control, Context(), Dependency);
        Assert.False(first.Admitted);
        Assert.Equal(first.CanonicalBytes, second.CanonicalBytes);
        Assert.Equal(first.CanonicalBytes, P01R03SameCutSourceAttestationCodecV1.ReadAttestation(
            first.CanonicalBytes, import, control, Context(), Dependency).CanonicalBytes);
        using var document = JsonDocument.Parse(first.CanonicalBytes);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("contractVersion").GetInt32());
        Assert.Equal("rules-lab.combat-creation.1", root.GetProperty("campaignId").GetString());
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination",
            root.GetProperty("positionId").GetString());
        Assert.Equal(2, root.GetProperty("cycleOrdinal").GetInt32());
        Assert.Equal("axis", root.GetProperty("actingSide").GetString());
        Assert.Equal("sha256:1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c",
            root.GetProperty("terminalControlHash").GetString());
        var elements = root.GetProperty("sourceElements").EnumerateArray().ToArray();
        Assert.Equal(2, elements.Length);
        Assert.Equal(("axis-assault-battalion", "axis", "map-representation.0001", "assault-west"), Facts(elements[0]));
        Assert.Equal(("commonwealth-assault-battalion", "commonwealth", "map-representation.0002", "assault-east"), Facts(elements[1]));
        Assert.Equal("trusted-content7.elements", elements[0].GetProperty("provenance").GetProperty("side").GetString());
        Assert.Equal("authenticated-r03-control.world.elements.currentLocationId",
            elements[1].GetProperty("provenance").GetProperty("location").GetString());
        Assert.False(root.GetProperty("admitted").GetBoolean());
        var text = Encoding.UTF8.GetString(first.CanonicalBytes);
        foreach (var forbidden in new[] { "pub.", "owner", "seat", "journal", "opening", "legalAction", "receiptRoute" })
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ForeignControlCutImportAndClaimCannotAttest()
    {
        var import = ReadFixture(ImportPath, ImportFileHash);
        var control = ReadFixture(ControlPath, ControlFileHash);
        var alteredControl = control.ToArray(); alteredControl[100] ^= 1;
        Assert.Equal("P01-A1S-002", Assert.Throws<JsonException>(() =>
            P01R03SameCutSourceAttestationCodecV1.Attest(import, alteredControl, Context(), Dependency)).Message);
        Assert.Equal("P01-A1S-001", Assert.Throws<JsonException>(() =>
            P01R03SameCutSourceAttestationCodecV1.Attest(import, new byte[P01R03SameCutSourceAttestationCodecV1.MaximumControlBytes + 1], Context(), Dependency)).Message);
        Assert.Throws<JsonException>(() => P01R03SameCutSourceAttestationCodecV1.Attest(
            [.. import, (byte)' '], control, Context(), Dependency));
        Assert.Throws<JsonException>(() => P01R03SameCutSourceAttestationCodecV1.Attest(import, control, Context(),
            path => path == "docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py" ? [0] : Dependency(path)));

        var original = P01R03SameCutSourceAttestationCodecV1.Attest(import, control, Context(), Dependency).CanonicalBytes;
        var forged = original.ToArray(); forged[^2] = (byte)'1';
        Assert.Equal("P01-A1S-004", Assert.Throws<JsonException>(() =>
            P01R03SameCutSourceAttestationCodecV1.ReadAttestation(forged, import, control, Context(), Dependency)).Message);
    }

    private static (string?, string?, string?, string?) Facts(JsonElement element) => (
        element.GetProperty("elementId").GetString(), element.GetProperty("sideId").GetString(),
        element.GetProperty("representationId").GetString(), element.GetProperty("locationId").GetString());

    private static byte[] ReadFixture(string path, string expectedHash)
    {
        var compressed = File.ReadAllBytes(Path.Combine(Repository(), path));
        Assert.Equal(expectedHash, Convert.ToHexStringLower(SHA256.HashData(compressed)));
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
