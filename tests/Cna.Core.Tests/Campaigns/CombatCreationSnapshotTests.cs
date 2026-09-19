using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCreationSnapshotTests
{
    [Fact]
    public void SnapshotMatchesFrozenBytesAndIndependentCreationReceiptAndPrefix()
    {
        var request = TrustedRequest();
        var created = Golden("created");
        var snapshot = CampaignCreationSnapshotV12.Create(created, request);

        Assert.Equal(7_903, Golden("snapshot").Length);
        Assert.Equal(Golden("snapshot"), CampaignCreationSnapshotV12Codec.Serialize(snapshot));
        Assert.Equal("sha256:a36c8abd3864442c62d13c9beb41af2c88048534b31bd47bc82a897c808bf798",
            snapshot.CreationReceipt.CreationEventHash);
        Assert.Equal(Digest(created), snapshot.CreationReceipt.CreationEventHash);
        // Frozen7520-byte event length is BE64 0x1d60, independently assembled without codec helpers.
        var prefixPreimage = Encoding.ASCII.GetBytes("sandtable.cycle.prefix.creation.v1\0")
            .Concat(new byte[] { 0, 0, 0, 0, 0, 0, 0x1d, 0x60 }).Concat(created).ToArray();
        Assert.Equal(Digest(prefixPreimage), snapshot.ChroniclePrefix);
        Assert.Equal("sha256:07ef36fec645c632463dcb1e7446b1d914cc8f91506bac968c2bac84a55def45",
            snapshot.ChroniclePrefix);
        Assert.Equal(request.CreationBinding, snapshot.CreationReceipt.CreationBinding);
        Assert.Same(request, snapshot.CreationReceipt.Request);
        Assert.Equal(1, snapshot.StateVersion);
        Assert.Equal(0UL, snapshot.RandomState.NextByteCursor);
        Assert.Equal(snapshot.World.CreationBinding, request.CreationBinding);
        Assert.Null(snapshot.SequencePosition.ActiveSide);
    }

    [Fact]
    public void CallerByteMutationCannotChangeValidatedSnapshotOrReceipt()
    {
        var evidence = Golden("created");
        var snapshot = CampaignCreationSnapshotV12.Create(evidence, TrustedRequest());
        Array.Fill(evidence, (byte)0);
        var returned = CampaignCreationSnapshotV12Codec.Serialize(snapshot);
        Array.Fill(returned, (byte)0);
        Assert.Equal(Golden("snapshot"), CampaignCreationSnapshotV12Codec.Serialize(snapshot));
        Assert.Equal(Digest(Golden("created")), snapshot.CreationReceipt.CreationEventHash);
    }

    [Fact]
    public void RetainedSnapshotReadsAfterFreshAdmissionCloses()
    {
        var request = TrustedRequest();
        var retained = Golden("created");
        Assert.Throws<JsonException>(() => CampaignCombatCreationCut.Decide(null, request, false));
        var retry = CampaignCombatCreationCut.Decide(retained, request, false);
        Assert.False(retry.RequiresPublication);
        var restored = CampaignCreationSnapshotV12Codec.Deserialize(Golden("snapshot"), retry.CreatedBytes, request);
        Assert.Equal(Golden("snapshot"), CampaignCreationSnapshotV12Codec.Serialize(restored));
        Assert.Equal(retained, retry.CreatedBytes);
        Assert.Equal(0UL, restored.RandomState.NextByteCursor);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(18446744073709551615UL)]
    public void SnapshotReceiptAndPrefixBindExactCreationForEachSeed(ulong seed)
    {
        var request = TrustedRequest(seed);
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        var snapshot = CampaignCreationSnapshotV12.Create(created, request);
        var canonical = CampaignCreationSnapshotV12Codec.Serialize(snapshot);
        var restored = CampaignCreationSnapshotV12Codec.Deserialize(canonical, created, request);
        Assert.Equal(canonical, CampaignCreationSnapshotV12Codec.Serialize(restored));
        Assert.Equal(seed, restored.RandomState.Seed);
        Assert.Equal(Digest(created), restored.CreationReceipt.CreationEventHash);
        var length = BitConverter.GetBytes((ulong)created.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        Assert.Equal(Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.creation.v1\0")
            .Concat(length).Concat(created).ToArray()), restored.ChroniclePrefix);
    }

    [Fact]
    public void MissingMalformedOrSubstitutedCreationEvidenceIsRejected()
    {
        var request = TrustedRequest();
        var changedRequest = TrustedRequest(1);
        var foreign = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(changedRequest));
        foreach (var created in new[]
        {
            Array.Empty<byte>(), "{}"u8.ToArray(), new byte[1_048_577], foreign,
            Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(Golden("created"))),
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Golden("created")).Replace(
                "\"points\":10", "\"points\":9", StringComparison.Ordinal)),
        })
        {
            Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12.Create(created, request));
            Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(Golden("snapshot"), created, request));
        }
    }

    [Fact]
    public void RehashedForeignCreationAndSnapshotCannotReplaceTrustedRequest()
    {
        var trusted = TrustedRequest();
        var foreign = TrustedRequest(1);
        var foreignCreated = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(foreign));
        var foreignSnapshot = CampaignCreationSnapshotV12Codec.Serialize(CampaignCreationSnapshotV12.Create(foreignCreated, foreign));
        Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(foreignSnapshot, foreignCreated, trusted));
        Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(foreignSnapshot, Golden("created"), trusted));
        Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(Golden("snapshot"), foreignCreated, foreign));
    }

    [Fact]
    public void SnapshotRequiresExactCanonicalBytesAndCreationOnlySlots()
    {
        var request = TrustedRequest();
        var original = Encoding.UTF8.GetString(Golden("snapshot"));
        var mutations = new[]
        {
            " " + original, original + "\n", "\uFEFF" + original,
            original.Replace("\"stateVersion\":1", "\"stateVersion\":1.0", StringComparison.Ordinal),
            original.Replace("\"seed\":0", "\"seed\":0e0", StringComparison.Ordinal),
            original.Replace("\"stateVersion\":1", "\"stateVersion\":2", StringComparison.Ordinal),
            original.Replace("rules-lab.combat-creation.1", "rules-lab.combat-creation.\\u0031", StringComparison.Ordinal),
            original.Replace("\"stateVersion\":1", "\"stateVersion\":1,\"stateVersion\":1", StringComparison.Ordinal),
            original.Replace("\"initiativeHolder\":null,", "", StringComparison.Ordinal),
            original.Replace("\"operationStageOrders\":[]", "\"operationStageOrders\":[{}]", StringComparison.Ordinal),
            original.Replace("\"reactionWindow\":null", "\"reactionWindow\":{}", StringComparison.Ordinal),
            original.Replace("\"cycleState\":null", "\"cycleState\":{}", StringComparison.Ordinal),
            original.Replace("\"combatState\":null", "\"combatState\":{}", StringComparison.Ordinal),
            original.Replace("\"commandReceipts\":[]", "\"commandReceipts\":[{}]", StringComparison.Ordinal),
            original.Replace("\"brokenVehicleLots\":[]", "\"brokenVehicleLots\":[{}]", StringComparison.Ordinal),
            original[..^1] + ",\"unexpected\":0}", "null", "[]", "{}", "",
        };
        foreach (var value in mutations)
        {
            Assert.NotEqual(original, value);
            Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(
                Encoding.UTF8.GetBytes(value), Golden("created"), request));
        }
        Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(
            new byte[1_048_577], Golden("created"), request));
    }

    public static IEnumerable<object[]> FrozenSnapshotMutations()
    {
        using var fixture = Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("negativeVectors").EnumerateArray())
            if (vector.GetProperty("target").GetString() == "snapshot")
                yield return [vector.GetProperty("path").GetString()!, vector.GetProperty("value").GetRawText()];
    }

    [Theory]
    [MemberData(nameof(FrozenSnapshotMutations))]
    public void FrozenSnapshotNegativeVectorsAreRejected(string path, string value)
    {
        var original = Golden("snapshot");
        var mutated = JsonNode.Parse(original)!;
        var segments = path.Split('/').Skip(1).ToArray();
        var parent = mutated;
        foreach (var segment in segments[..^1])
            parent = parent is JsonArray array
                ? array[int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture)]!
                : parent[segment]!;
        parent[segments[^1]] = JsonNode.Parse(value);
        var bytes = Encoding.UTF8.GetBytes(mutated.ToJsonString());
        Assert.NotEqual(original, bytes);
        Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, Golden("created"), TrustedRequest()));
    }

    private static CampaignCombatCreationRequest TrustedRequest(ulong seed = 0)
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        using var created = JsonDocument.Parse(Golden("created"));
        var root = created.RootElement;
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var context = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config);
        return CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", seed, context);
    }

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Rules", "Fixtures", "combat-authority-envelope-v1.json")));

    private static byte[] Golden(string kind)
    {
        using var fixture = Fixture();
        return Encoding.UTF8.GetBytes(fixture.RootElement.GetProperty("goldens").GetProperty(kind)
            .GetProperty("canonicalUtf8").GetString()!);
    }

    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
