using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCreationBindingTests
{
    [Fact]
    public void RequestAndCreated11MatchFrozenBytesAndBinding()
    {
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, Context());
        Assert.Equal(741, Golden("request").Length);
        Assert.Equal(Golden("request"), CampaignCombatCreationRequestCodec.Serialize(request));
        Assert.Equal("creation." + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes("sandtable.combat.creation-request.v1\0")
                .Concat(Golden("request")).ToArray())).ToLowerInvariant(), request.CreationBinding);
        var created = CampaignCreatedV11.Create(request);
        Assert.Equal(7_520, Golden("created").Length);
        Assert.Equal(Golden("created"), CampaignCreatedV11Serializer.Serialize(created));
        var read = CampaignCreatedV11Serializer.Deserialize(Golden("created"), request);
        Assert.Equal(request.CreationBinding, read.InitialWorld.CreationBinding);
        Assert.Equal(0UL, read.Request.RandomState.NextByteCursor);
        Assert.Equal(5, read.SequencePosition.ContractVersion);
        Assert.Equal(0, read.SequencePosition.OperationStage);
        Assert.Null(read.SequencePosition.ActiveSide);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(9223372036854775808UL)]
    [InlineData(18446744073709551615UL)]
    public void FullUnsignedSeedRangeRoundTripsWithoutDraws(ulong seed)
    {
        var context = Context();
        var request = CampaignCombatCreationRequest.Create("seed-test", seed, context);
        var read = CampaignCombatCreationRequestCodec.Deserialize(
            CampaignCombatCreationRequestCodec.Serialize(request), context);
        Assert.Equal(seed, read.RandomState.Seed);
        Assert.Equal(0UL, read.RandomState.NextByteCursor);
        Assert.Equal(request.CreationBinding, read.CreationBinding);
        Assert.Equal(CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request)),
            CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(read)));
    }

    [Fact]
    public void RetainedCreationWinsClosedAdmissionAndConflictingRequestsFail()
    {
        var context = Context();
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, context);
        var first = CampaignCombatCreationCut.Decide(null, request, true);
        Assert.True(first.RequiresPublication);
        var retained = first.CreatedBytes;
        var retry = CampaignCombatCreationCut.Decide(retained, request, false);
        Assert.False(retry.RequiresPublication);
        Assert.Equal(retained, retry.CreatedBytes);
        Assert.Throws<JsonException>(() => CampaignCombatCreationCut.Decide(null, request, false));
        foreach (var changed in new[]
        {
            CampaignCombatCreationRequest.Create(request.CampaignId, 1, context),
            CampaignCombatCreationRequest.Create("different-campaign", 0, context),
            CampaignCombatCreationRequest.Create(request.CampaignId, 0, Context(true)),
        })
        {
            Assert.NotEqual(request.CreationBinding, changed.CreationBinding);
            Assert.Throws<JsonException>(() => CampaignCombatCreationCut.Decide(retained, changed, true));
        }
        Assert.Equal(Golden("created"), retained);
    }

    [Fact]
    public void TrustedConfigurationAndRulesCannotBeReplacedByEmbeddedInputs()
    {
        var original = Context();
        Assert.Throws<JsonException>(() => CampaignCombatCreationRequestCodec.Deserialize(
            Golden("request"), Context(true)));
        Assert.Throws<JsonException>(() => new CampaignCombatCreationContext(Cna1979Ruleset.Manifest,
            original.Setup, original.Setup.Artifact, original.Setup.Scenario, original.Configuration));
        var changed = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, Context(true));
        Assert.Throws<JsonException>(() => CampaignCreatedV11Serializer.Deserialize(Golden("created"), changed));
    }

    [Theory]
    [InlineData("setup", 128)]
    [InlineData("setup", 129)]
    [InlineData("configuration", 128)]
    [InlineData("configuration", 129)]
    public void TrustedInputIdsRespectEnvelopeBounds(string kind, int length)
    {
        var original = Context();
        var setup = original.Setup;
        var configuration = original.Configuration;
        var id = new string('a', length);
        if (kind == "setup")
            setup = CampaignSetupSnapshotV7.Create(id, setup.InitialGameTurn, setup.InitialInitiative,
                setup.OpeningPreamble, setup.Weather, setup.StageEntry, setup.CombatInitialization,
                setup.Artifact, setup.Scenario, setup.Sources);
        else
            configuration = new CombatDecisionConfiguration(id, configuration.RulesInputHash,
                configuration.Windows);

        CampaignCombatCreationContext CreateContext() => new(Cna1979CombatRuleset.Manifest, setup,
            setup.Artifact, setup.Scenario, configuration);
        if (length > 128)
        {
            Assert.Throws<JsonException>(() => CreateContext());
            return;
        }

        var request = CampaignCombatCreationRequest.Create("id-boundary", 0, CreateContext());
        var bytes = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        Assert.Equal(bytes, CampaignCreatedV11Serializer.Serialize(
            CampaignCreatedV11Serializer.Deserialize(bytes, request)));
    }

    [Theory]
    [InlineData("request")]
    [InlineData("created")]
    public void RawByteAndSemanticMutationsAreRejected(string kind)
    {
        var original = Encoding.UTF8.GetString(Golden(kind));
        var context = Context();
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, context);
        var mutations = new[]
        {
            " " + original, original + "\n", "\uFEFF" + original,
            original.Replace("\"campaignId\":", "\"campaignId\":\"duplicate\",\"campaignId\":", StringComparison.Ordinal),
            original.Replace("rules-lab.combat-creation.1", "rules-lab.combat-creation.\\u0031", StringComparison.Ordinal),
            original.Replace("\"seed\":0", "\"seed\":-1", StringComparison.Ordinal),
            original.Replace("\"seed\":0", "\"seed\":1e0", StringComparison.Ordinal),
            original.Replace("\"seed\":0", "\"seed\":18446744073709551616", StringComparison.Ordinal),
            original.Replace("\"nextByteCursor\":0", "\"nextByteCursor\":1", StringComparison.Ordinal),
            original.Replace("\"campaignId\":\"rules-lab.combat-creation.1\",", "", StringComparison.Ordinal),
            original[..^1] + ",\"unknown\":null}",
            original.Replace("\"contractVersion\":" + (kind == "request" ? "1" : "11") + ",",
                "\"contractVersion\":99,", StringComparison.Ordinal),
            original.Replace("\"rulesetHash\":\"8", "\"rulesetHash\":\"9", StringComparison.Ordinal),
            original.Replace("\"setupHash\":\"sha256:2", "\"setupHash\":\"sha256:3", StringComparison.Ordinal),
        };
        foreach (var value in mutations)
        {
            Assert.NotEqual(original, value);
            var bytes = Encoding.UTF8.GetBytes(value);
            if (kind == "request") Assert.ThrowsAny<JsonException>(() => CampaignCombatCreationRequestCodec.Deserialize(bytes, context));
            else Assert.Throws<JsonException>(() => CampaignCreatedV11Serializer.Deserialize(bytes, request));
        }
    }

    public static IEnumerable<object[]> FrozenCreationMutations()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        foreach (var vector in fixture.RootElement.GetProperty("negativeVectors").EnumerateArray())
        {
            var target = vector.GetProperty("target").GetString()!;
            if (target is "request" or "created")
                yield return [target, vector.GetProperty("path").GetString()!, vector.GetProperty("value").GetRawText()];
        }
    }

    [Theory]
    [MemberData(nameof(FrozenCreationMutations))]
    public void FrozenRequestAndCreatedNegativeVectorsAreRejected(string target, string path, string value)
    {
        var original = Golden(target);
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
        var context = Context();
        if (target == "request")
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCreationRequestCodec.Deserialize(bytes, context));
        else
            Assert.Throws<JsonException>(() => CampaignCreatedV11Serializer.Deserialize(bytes,
                CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, context)));
    }

    [Fact]
    public void ReadersRejectOversizeAndWrongRootInputs()
    {
        var context = Context();
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, context);
        foreach (var bytes in new[] { Array.Empty<byte>(), "null"u8.ToArray(), "[]"u8.ToArray(),
            "{}"u8.ToArray(), new byte[1_048_577] })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCreationRequestCodec.Deserialize(bytes, context));
            Assert.Throws<JsonException>(() => CampaignCreatedV11Serializer.Deserialize(bytes, request));
        }
    }

    [Fact]
    public void SetupContentAndSeedForksHaveDistinctBindingsAndRejectOldEvidence()
    {
        var original = Context();
        var originalRequest = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0, original);
        var originalBytes = Golden("created");
        var holderSetup = RebindSetup(original.Setup, original.Setup.Artifact, LandSide.Commonwealth);
        var content = JsonNode.Parse(original.Setup.Artifact.GetCanonicalBytes())!;
        var reference = content["scenarios"]![0]!["initialPlacements"]![0]!["initialAmmunition"]!["origin"]!["references"]![0]!;
        reference["locator"] = reference["locator"]!.GetValue<string>() + ".fork";
        var parsed = ContentPackV7Serializer.Deserialize(Encoding.UTF8.GetBytes(content.ToJsonString()));
        Assert.True(parsed.IsSuccess, parsed.Message);
        var artifact = ContentPackV7Artifact.Create(parsed.Definition!);
        Assert.Equal(original.Setup.Artifact.Identity.PackId, artifact.Identity.PackId);
        Assert.NotEqual(original.Setup.Artifact.Identity.Hash, artifact.Identity.Hash);
        Assert.Throws<JsonException>(() => new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest,
            original.Setup, artifact, Assert.Single(artifact.Definition.Scenarios), original.Configuration));
        var contentSetup = RebindSetup(original.Setup, artifact, LandSide.Axis);
        var contexts = new[]
        {
            original, Context(true),
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, holderSetup,
                holderSetup.Artifact, holderSetup.Scenario, original.Configuration),
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, contentSetup,
                artifact, contentSetup.Scenario, original.Configuration),
        };
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var context in contexts)
            foreach (var seed in new[] { 0UL, 1UL, ulong.MaxValue })
            {
                var request = CampaignCombatCreationRequest.Create(originalRequest.CampaignId, seed, context);
                Assert.True(bindings.Add(request.CreationBinding));
                var bytes = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
                Assert.Equal(bytes, CampaignCreatedV11Serializer.Serialize(
                    CampaignCreatedV11Serializer.Deserialize(bytes, request)));
                if (request.CreationBinding == originalRequest.CreationBinding) continue;
                Assert.Throws<JsonException>(() => CampaignCreatedV11Serializer.Deserialize(bytes, originalRequest));
                Assert.Throws<JsonException>(() => CampaignCombatCreationCut.Decide(originalBytes, request, false));
            }
        Assert.Equal(12, bindings.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(111)]
    public void CertifiedTurnBoundariesFlowIntoPreambleAndInitialWorld(int turn)
    {
        var original = Context();
        var content = JsonNode.Parse(original.Setup.Artifact.GetCanonicalBytes())!;
        var scenario = content["scenarios"]![0]!;
        scenario["start"]!["gameTurn"] = turn;
        scenario["end"]!["gameTurn"] = turn;
        foreach (var placement in scenario["initialPlacements"]!.AsArray())
            placement!["initialReadiness"]!["gameTurn"] = turn;
        var parsed = ContentPackV7Serializer.Deserialize(Encoding.UTF8.GetBytes(content.ToJsonString()));
        Assert.True(parsed.IsSuccess, parsed.Message);
        var artifact = ContentPackV7Artifact.Create(parsed.Definition!);
        var setup = RebindSetup(original.Setup, artifact, LandSide.Axis);
        var context = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup,
            artifact, setup.Scenario, original.Configuration);
        var request = CampaignCombatCreationRequest.Create("turn-boundary", 0, context);
        var created = CampaignCreatedV11.Create(request);
        var restored = CampaignCreatedV11Serializer.Deserialize(CampaignCreatedV11Serializer.Serialize(created), request);
        Assert.Equal(turn, restored.SequencePosition.GameTurn);
        Assert.All(restored.InitialWorld.Elements, element =>
        {
            Assert.Equal(turn, element.OperationalState.LedgerGameTurn);
            Assert.Equal(turn, element.Readiness.GameTurn);
        });
    }

    [Theory]
    [InlineData("A:_..-")]
    [InlineData("a")]
    public void CampaignIdUsesFrozenEnvelopeGrammar(string campaignId)
    {
        var context = Context();
        var request = CampaignCombatCreationRequest.Create(campaignId, 0, context);
        Assert.Equal(campaignId, CampaignCombatCreationRequestCodec.Deserialize(
            CampaignCombatCreationRequestCodec.Serialize(request), context).CampaignId);
        Assert.Throws<ArgumentException>(() => CampaignCombatCreationRequest.Create(new string('a', 129), 0, context));
        Assert.Throws<ArgumentException>(() => CampaignCombatCreationRequest.Create("_leading", 0, context));
    }

    private static CampaignSetupSnapshotV7 RebindSetup(CampaignSetupSnapshotV7 original,
        ContentPackV7Artifact artifact, LandSide holder)
    {
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var turn = scenario.Start.GameTurn;
        return CampaignSetupSnapshotV7.Create(original.SetupId, turn, new PredeterminedInitiative(holder),
            original.OpeningPreamble, original.Weather,
            new CampaignStageEntryPolicy(1, turn, 1, StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone, original.StageEntry.Sources),
            new CampaignCombatInitializationPolicy(1, turn, 1, CapabilityPointAmount.Zero, 0,
                CampaignElementReserveStatus.None, original.CombatInitialization.Origin),
            artifact, scenario, original.Sources);
    }

    private static CampaignCombatCreationContext Context(bool changedBudget = false)
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        using var created = JsonDocument.Parse(Golden("created"));
        var root = created.RootElement;
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        if (changedBudget) config = new CombatDecisionConfiguration(config.ConfigId, config.RulesInputHash,
            config.Windows.Select(w => w with { DecisionBudgetMilliseconds = w.DecisionBudgetMilliseconds + 1 }));
        return new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config);
    }

    private static byte[] Golden(string kind)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        return Encoding.UTF8.GetBytes(fixture.RootElement.GetProperty("goldens").GetProperty(kind)
            .GetProperty("canonicalUtf8").GetString()!);
    }
}
