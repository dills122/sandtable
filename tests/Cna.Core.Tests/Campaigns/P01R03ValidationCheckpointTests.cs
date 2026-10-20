using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class P01R03ValidationCheckpointTests
{
    private const string Fixture = "docs/specs/fixtures/p01-r03-a1-r012-c01-import-v1.json.gz";
    private const string FixtureHash = "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa";

    [Fact]
    public void AuthenticRootProducesDeterministicUnissuableCheckpoint()
    {
        var importBytes = AuthenticImport();
        var context = Context();
        var first = P01R03ValidationCheckpointV1.Validate(importBytes, context, Dependency);
        var second = P01R03ValidationCheckpointV1.Validate(importBytes, context, Dependency);
        Assert.False(first.Admitted);
        Assert.Equal(first.CanonicalBytes, second.CanonicalBytes);
        Assert.Equal(first.CanonicalBytes, P01R03ValidationCheckpointV1.ReadCheckpoint(first.CanonicalBytes, importBytes, context, Dependency).CanonicalBytes);
        using var document = JsonDocument.Parse(first.CanonicalBytes);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("contractVersion").GetInt32());
        Assert.False(root.GetProperty("admitted").GetBoolean());
        Assert.Equal("rules-lab.combat-creation.1", root.GetProperty("campaignId").GetString());
        Assert.Equal(11, root.GetProperty("orderedLedgerHashesAndLengths").GetArrayLength());
        Assert.Equal("sha256:7dd4a0860b65d8bd1db51f65cd2eadc7b57110cf95c0bf5d32447f2937573638", root.GetProperty("r03SourceHash").GetString());
        Assert.Equal("sha256:1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c", root.GetProperty("r03ControlHash").GetString());
        Assert.Equal(33, root.GetProperty("terminalStateVersion").GetInt64());
        Assert.Equal("mrbc.af668a6a66dd890e432e4012f3690f10cfa8fb4e5a23c4ec4f228836a30f61d9", root.GetProperty("terminalReceiptId").GetString());
        Assert.DoesNotContain("opening", Encoding.UTF8.GetString(first.CanonicalBytes), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("journal", Encoding.UTF8.GetString(first.CanonicalBytes), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClosedImportAndCheckpointRejectMalformedOrClaimedBytes()
    {
        var importBytes = AuthenticImport();
        var value = P01R03ImportV1.Deserialize(importBytes);
        Assert.Equal(importBytes, value.Serialize());
        Assert.Equal("P01-A1-001", Assert.Throws<JsonException>(() => P01R03ImportV1.Deserialize([(byte)'{'])).Message);
        var duplicate = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(importBytes).Replace("\"contractVersion\":1,", "\"contractVersion\":1,\"contractVersion\":1,", StringComparison.Ordinal));
        Assert.Equal("P01-A1-001", Assert.Throws<JsonException>(() => P01R03ImportV1.Deserialize(duplicate)).Message);
        var unknown = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(importBytes).Replace("\"contractVersion\":1,", "\"contractVersion\":1,\"claimedAdmission\":true,", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => P01R03ImportV1.Deserialize(unknown));
        Assert.Throws<JsonException>(() => P01R03ImportV1.Deserialize([.. importBytes, (byte)' ']));
        Assert.Throws<JsonException>(() => P01R03ImportV1.Deserialize(new byte[P01R03ImportV1.MaximumBytes + 1]));
        Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(importBytes, Context(), p => p == "docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py" ? [0] : Dependency(p)));
        var checkpoint = P01R03ValidationCheckpointV1.Validate(importBytes, Context(), Dependency).CanonicalBytes;
        Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.ReadCheckpoint(new byte[4_097], importBytes, Context(), Dependency));
        var altered = checkpoint.ToArray(); altered[^2] = (byte)'1';
        Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.ReadCheckpoint(altered, importBytes, Context(), Dependency));
        foreach (var field in new[] { "contractVersion", "terminalStateVersion", "terminalPrefix", "terminalReceiptId" })
        {
            var claim = JsonNode.Parse(checkpoint)!.AsObject();
            claim[field] = field is "contractVersion" or "terminalStateVersion" ? JsonValue.Create(2) : JsonValue.Create("forged");
            Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.ReadCheckpoint(Encode(claim), importBytes, Context(), Dependency));
        }
    }

    [Fact]
    public void MutatedSourceLedgerOrderAndCreationRequestCannotProduceCheckpoint()
    {
        var original = P01R03ImportV1.Deserialize(AuthenticImport());
        var ledgers = original.Predecessors.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray();
        (ledgers[0], ledgers[1]) = (ledgers[1], ledgers[0]);
        Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(
            new P01R03ImportV1(original.CreationRequest, ledgers, original.R03Input, original.R03Source).Serialize(), Context(), Dependency));
        for (var ordinal = 0; ordinal < original.Predecessors.Length; ordinal++)
        {
            ledgers = original.Predecessors.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray();
            if (ledgers[ordinal].Length == 0) ledgers[ordinal] = [original.Predecessors[0][0].ToArray()];
            else ledgers[ordinal][0][20] ^= 1;
            Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(
                new P01R03ImportV1(original.CreationRequest, ledgers, original.R03Input, original.R03Source).Serialize(), Context(), Dependency));
        }
        var request = original.CreationRequest.ToArray(); request[30] ^= 1;
        Assert.Equal("P01-A1-002", Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(
            new P01R03ImportV1(request, original.Predecessors, original.R03Input, original.R03Source).Serialize(), Context(), Dependency)).Message);
        var own = original.R03Input.Select(x => x.ToArray()).ToArray(); own[0][20] ^= 1;
        Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(
            new P01R03ImportV1(original.CreationRequest, original.Predecessors, own, original.R03Source).Serialize(), Context(), Dependency));
        var source = original.R03Source.ToArray(); source[^100] ^= 1;
        Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(
            new P01R03ImportV1(original.CreationRequest, original.Predecessors, original.R03Input, source).Serialize(), Context(), Dependency));
        var context = Context();
        var config = JsonNode.Parse(CombatDecisionConfigurationCodec.Serialize(context.Configuration))!.AsObject();
        config["configId"] = "foreign-context";
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var foreign = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, context.Setup, artifact,
            artifact.Definition.Scenarios.Single(), CombatDecisionConfigurationCodec.Deserialize(Encode(config), Cna1979CombatRuleset.Manifest));
        Assert.Equal("P01-A1-002", Assert.Throws<JsonException>(() => P01R03ValidationCheckpointV1.Validate(AuthenticImport(), foreign, Dependency)).Message);
    }

    private static byte[] AuthenticImport()
    {
        var gzip = File.ReadAllBytes(Path.Combine(Repository(), Fixture));
        Assert.Equal(FixtureHash, Convert.ToHexStringLower(SHA256.HashData(gzip)));
        using var source = new MemoryStream(gzip);
        using var decompressor = new GZipStream(source, CompressionMode.Decompress);
        using var target = new MemoryStream(); decompressor.CopyTo(target);
        return target.ToArray();
    }
}
