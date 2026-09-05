using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownSetupTests
{
    [Fact]
    public void Setup6RoundTripsAndBindsContent6AndProfile()
    {
        var setup = CreateSetup();
        Assert.Equal(6, setup.SchemaVersion);
        Assert.Equal(6, setup.Content.Pack.SchemaVersion);
        Assert.Equal(ContentPackV6Definition.SupportedCapabilityProfileId, setup.CapabilityProfileId);
        var bytes = CampaignSetupV6Codec.Serialize(setup);
        Assert.Equal(setup, CampaignSetupV6Codec.Deserialize(bytes));
        var hashBytes = CampaignSetupV6Codec.SerializeCanonicalHash(setup);
        Assert.Equal($"sha256:{Convert.ToHexString(SHA256.HashData(hashBytes)).ToLowerInvariant()}", setup.SetupHash);
        var json = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\"isSynthetic\":true,\"capabilityProfileId\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain("setupHash", Encoding.UTF8.GetString(hashBytes), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("setup-schema")]
    [InlineData("content-schema")]
    [InlineData("content-format")]
    [InlineData("profile")]
    [InlineData("missing-profile")]
    [InlineData("nonsynthetic")]
    [InlineData("hash")]
    [InlineData("unknown")]
    public void RejectsMixedIdentityAndTamperedFields(string mutation)
    {
        var root = JsonNode.Parse(CampaignSetupV6Codec.Serialize(CreateSetup()))!.AsObject();
        switch (mutation)
        {
            case "setup-schema": root["schemaVersion"] = 5; break;
            case "content-schema": root["content"]!["schemaVersion"] = 5; break;
            case "content-format": root["content"]!["formatId"] = "sandtable.content-json.v4"; break;
            case "profile": root["capabilityProfileId"] = "unknown-profile"; break;
            case "missing-profile": root.Remove("capabilityProfileId"); break;
            case "nonsynthetic": root["isSynthetic"] = false; break;
            case "hash": root["setupHash"] = "sha256:" + new string('0', 64); break;
            case "unknown": root["unknown"] = 1; break;
            default: throw new InvalidOperationException(mutation);
        }

        Assert.ThrowsAny<Exception>(() => CampaignSetupV6Codec.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())));
    }

    [Fact]
    public void RejectsDuplicateAndNoncanonicalBytes()
    {
        var json = Encoding.UTF8.GetString(CampaignSetupV6Codec.Serialize(CreateSetup()));
        Assert.ThrowsAny<Exception>(() => CampaignSetupV6Codec.Deserialize(Encoding.UTF8.GetBytes(json + "\n")));
        Assert.ThrowsAny<Exception>(() => CampaignSetupV6Codec.Deserialize(Encoding.UTF8.GetBytes(json.Replace(
            "\"schemaVersion\":6", "\"schemaVersion\":6,\"schemaVersion\":6", StringComparison.Ordinal))));
    }

    [Fact]
    public void DefinitionKeepsDisplayNameOutsideAuthorityHash()
    {
        var setup = CreateSetup();
        var first = Definition("Breakdown Truck Exercise");
        var renamed = Definition("Another Display Name");
        Assert.NotEqual(first.DisplayName, renamed.DisplayName);
        Assert.Equal(first.Hash, renamed.Hash);
        Assert.Equal(setup, CampaignSetupSnapshotV6.FromDefinition(first));

        CampaignSetupDefinitionV6 Definition(string name) => new(
            setup.SchemaVersion, setup.SetupId, name, setup.IsSynthetic, setup.CapabilityProfileId,
            setup.InitialGameTurn, setup.InitialInitiative, setup.OpeningPreamble, setup.Weather,
            setup.StageEntry, setup.Content, setup.Sources);
    }

    [Fact]
    public void HashBytesPreserveFrozenPredecessorExceptVersionContentAndProfile()
    {
        var setup = CreateSetup();
        var predecessor = JsonNode.Parse(CampaignSetupHash.SerializeCanonical(Cna1979SetupCatalog.Definitions[0]))!.AsObject();
        predecessor["schemaVersion"] = 6;
        predecessor["content"]!["schemaVersion"] = 6;
        predecessor["content"]!["formatId"] = "sandtable.content-json.v5";
        predecessor["content"]!["packId"] = setup.Content.Pack.PackId;
        predecessor["content"]!["hash"] = setup.Content.Pack.Hash;
        predecessor["content"]!["scenarioId"] = setup.Content.ScenarioId;
        var expected = new JsonObject();
        foreach (var property in predecessor)
        {
            expected.Add(property.Key, property.Value?.DeepClone());
            if (property.Key == "isSynthetic")
                expected.Add("capabilityProfileId", ContentPackV6Definition.SupportedCapabilityProfileId);
        }

        Assert.Equal(Encoding.UTF8.GetBytes(expected.ToJsonString()), CampaignSetupV6Codec.SerializeCanonicalHash(setup));
    }

    [Theory]
    [InlineData("nonsynthetic")]
    [InlineData("profile")]
    public void RehashingCannotCertifyUnsupportedSetup(string mutation)
    {
        var root = JsonNode.Parse(CampaignSetupV6Codec.Serialize(CreateSetup()))!.AsObject();
        if (mutation == "nonsynthetic")
            root["isSynthetic"] = false;
        else
            root["capabilityProfileId"] = "unknown-profile";
        var hashFields = root.DeepClone().AsObject();
        hashFields.Remove("setupHash");
        root["setupHash"] = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashFields.ToJsonString()))).ToLowerInvariant()}";

        var exception = Assert.Throws<ArgumentException>(() => CampaignSetupV6Codec.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())));
        Assert.Contains("BRK-CERT-001", exception.Message, StringComparison.Ordinal);
    }

    internal static CampaignSetupSnapshotV6 CreateSetup() => CampaignSetupSnapshotV6.FromPredecessor(
        CampaignSetupSnapshot.FromDefinition(Cna1979SetupCatalog.Definitions[0]),
        new CampaignContentV6Selection(BreakdownContentFixture.Artifact().Identity, "breakdown-truck-lab"));
}
