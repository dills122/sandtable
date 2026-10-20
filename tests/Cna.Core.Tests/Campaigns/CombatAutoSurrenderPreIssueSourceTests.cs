using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatAutoSurrenderPreIssueSourceTests
{
    [Fact]
    public void SealRetainsExactPinnedSourceAndClosedInventoryAfterCallerMutation()
    {
        var content = Fixture("content-v8");
        var expectedContent = (byte[])content.Clone();
        var seal = CampaignCombatAutoSurrenderPreIssueSourceV1.Read(
            content, Fixture("rules-source-v1"), Fixture("setup-v8"));
        content[0] ^= 1;

        var encoded = seal.Serialize();
        var roundTrip = CampaignCombatAutoSurrenderPreIssueSourceV1.Deserialize(encoded);
        Assert.Equal(19_725, encoded.Length);
        Assert.Equal(encoded, roundTrip.Serialize());
        Assert.Equal(seal.SourceSealId, roundTrip.SourceSealId);
        Assert.Equal("source.740dd3c33bab93108c6638a4c5d1dc2f6109ae9926da5c5d79650ebdfaf8c648",
            seal.SourceSealId);
        using var document = JsonDocument.Parse(encoded);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("contractVersion").GetInt32());
        Assert.Equal(expectedContent,
            Convert.FromBase64String(root.GetProperty("content").GetProperty("bytesBase64").GetString()!));
        Assert.Equal("sha256:" + Convert.ToHexString(SHA256.HashData(expectedContent)).ToLowerInvariant(),
            root.GetProperty("content").GetProperty("hash").GetString());
        var inventory = root.GetProperty("closedInitialInventory");
        Assert.Equal("axis-assault-battalion", inventory.GetProperty("attackerElementId").GetString());
        Assert.Equal(10, inventory.GetProperty("attackerAmmunition").GetInt32());
        Assert.Equal("commonwealth-assault-battalion", inventory.GetProperty("defenderElementId").GetString());
        Assert.Equal(0, inventory.GetProperty("defenderAmmunition").GetInt32());
        Assert.Equal(0, inventory.GetProperty("cohesionLevel").GetInt32());
        Assert.Empty(inventory.GetProperty("externalAmmunitionSources").EnumerateArray());
    }

    [Fact]
    public void LegacyContent7CannotBecomeAnAsymmetricSourceByRehashingTheEnvelope()
    {
        var legacy = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Content", "Fixtures", "combat-content-v7.canonical.json"));
        Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderPreIssueSourceV1.Read(
            legacy, Fixture("rules-source-v1"), Fixture("setup-v8")));

        var envelope = JsonNode.Parse(Canonical())!;
        envelope["content"]!["bytesBase64"] = Convert.ToBase64String(legacy);
        envelope["content"]!["hash"] = "sha256:" +
            Convert.ToHexString(SHA256.HashData(legacy)).ToLowerInvariant();
        Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderPreIssueSourceV1.Deserialize(
            JsonSerializer.SerializeToUtf8Bytes(envelope)));
    }

    [Fact]
    public void CrossProfileSpliceAndAlteredClaimsFailClosed()
    {
        var approved = Canonical();
        var legacy = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Content", "Fixtures", "combat-content-v7.canonical.json"));
        var changes = new Action<JsonNode>[]
        {
            node => node["rulesSource"]!["bytesBase64"] =
                node["content"]!["bytesBase64"]!.GetValue<string>(),
            node => node["content"]!["hash"] = "sha256:" + new string('0', 64),
            node => node["closedInitialInventory"]!["defenderAmmunition"] = 1,
            node => node["closedInitialInventory"]!["externalAmmunitionSources"] =
                new JsonArray("supply-dump"),
            node => node["content"]!["bytesBase64"] = Convert.ToBase64String(legacy),
            node => node["kind"] = "campaign-created",
            node => node["unexpected"] = true,
        };
        foreach (var change in changes)
        {
            var node = JsonNode.Parse(approved)!;
            change(node);
            Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderPreIssueSourceV1.Deserialize(
                JsonSerializer.SerializeToUtf8Bytes(node)));
        }
        Assert.Throws<JsonException>(() => CampaignCombatAutoSurrenderPreIssueSourceV1.Deserialize(
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(approved))));
    }

    private static byte[] Canonical() => CampaignCombatAutoSurrenderPreIssueSourceV1.Read(
        Fixture("content-v8"), Fixture("rules-source-v1"), Fixture("setup-v8")).Serialize();

    private static byte[] Fixture(string kind) => File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Campaigns", "Fixtures", $"combat-auto-surrender-{kind}.canonical.json"));
}
