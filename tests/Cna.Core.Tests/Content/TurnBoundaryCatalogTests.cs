using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class TurnBoundaryCatalogTests
{
    [Fact]
    public void ExistingSetupAndRulesetIdentitiesRemainFrozen()
    {
        var identities = Cna1979BreakdownSetupCatalog.Definitions
            .Where(setup => !setup.SetupId.StartsWith("rules-lab.breakdown.turn-boundary.", StringComparison.Ordinal))
            .Select(setup => $"{setup.SetupId} {setup.Hash} {setup.Content.Pack.Hash}")
            .Order(StringComparer.Ordinal);
        var actual = string.Join('\n', identities.Append(Cna1979Ruleset.Manifest.Hash).Concat(Cna1979Ruleset.Manifest.Artifacts.Select(artifact => $"{artifact.ArtifactId} {artifact.ContentHash}")));
        Assert.Equal(
            """
            rules-lab.breakdown.battalion-contested.v1 sha256:1d6d7c838ac07221f4dcd5b6c08c6144b8a0ef605cadccf7427dd5ed6ca24b23 sha256:1be529d7079b836f4d26d920c4bfda0ce95d74169ed9ba0b62baf100cec7efa1
            rules-lab.breakdown.battalion-matrix.v1 sha256:9bbd5cdd437fdc574bc65c5d5a90a1ae130827c5c57084474258db9d8dc7cd7d sha256:1be529d7079b836f4d26d920c4bfda0ce95d74169ed9ba0b62baf100cec7efa1
            rules-lab.breakdown.reaction.adjacent.v1 sha256:977695c69112f752fddc2f2ca48da71d9d8b4aa38c8c589ff4370044ca61a200 sha256:8dc4a1fa9aa6fc244eefe3343d473f6ea547e76ca2f1035461e0996f2bd3a56b
            rules-lab.breakdown.reaction.combat-assault.v1 sha256:69db7f2c961f88f5469627a9e82f76c6a8b85330aa48a94ee3eaf0d22d18bbb3 sha256:d2d569a3f04f4915108e2220ac27ddfc25c97fcc047a4d4a67b496a6d62ca19c
            rules-lab.breakdown.reaction.combat-double-fresh.v1 sha256:d02a9996e2c87b4f5feda75bd0b6cf4a6756b0abbb83abb36ffe037e6fac8b5d sha256:44a76f21f1b492493c898495721f5f684acc83a14c391289c5e8d66220b6a648
            rules-lab.breakdown.reaction.combat-double.v1 sha256:19e44833a77c844c2bb90e672172d92cf517d5a9fc40381c31d097f6bfd1ff54 sha256:223dfe9d07ab34d498e13fd94c7fa3df45370b468393cd254564372c60a013ac
            rules-lab.breakdown.reaction.combat-infantry.v1 sha256:18734d3693747a35d855205f869917dc53dfdbdb9333ee32cb7764cbddaff3ac sha256:f6c8e7cbed61be1e99c877323471857b2d6b43a5bcfbe9475a59df9599e3498e
            rules-lab.breakdown.reaction.combat-triple.v1 sha256:9383b8fda7d2b33cedbd699d5bd564cbbea9900f520c00a9d3827d6447328049 sha256:eb81034809fcbb7403de47db4e3f3bd6abb42fec430f9017603484f7764c7d71
            rules-lab.breakdown.reaction.headquarters.v1 sha256:04ca8772d1540ea27faa6eb5924ef8d30dce7227c41b78f8971ea3c8010e33b9 sha256:8b7b2d5da9f39b017eee5ede052bb40553f02678444572f5faca9e74650f9e02
            rules-lab.breakdown.reaction.last-cp.v1 sha256:809a5f9410f60e3823379d9ed056af4c49009b2469e031568130983873b0532d sha256:58ee84eed7135cc261c1939f180e6ea20869438773d8697971d216f69430463f
            rules-lab.breakdown.reaction.low-defense.v1 sha256:d9c90688711a4c6aec3790ae57f52e132efaa963161dbd5bf8815c1e41e8e8b2 sha256:d1bd720f3d43966604b57de4312b3c11f98aaa275581b01d26183e87ea554eb3
            rules-lab.breakdown.reaction.noncombat.v1 sha256:7585241f7d597e330f7e1373786324073a29d745026f22e8384aed3f16ebc3f7 sha256:1819958227628d4406002885c6385b9f59d84dd454fd405fd84519bf0e808564
            rules-lab.breakdown.reaction.recurrence.v1 sha256:ef1a2a1f6ebf9662f018fbe228b7e950ae6531531a798f1cd22e0838efdd8fb9 sha256:43524eb63f5e6ee99375c7fc711271be611e2f5726992040b2e3919ddc0ef82e
            rules-lab.breakdown.reaction.truck-mover.v1 sha256:ca900c88a72b4bb3a1b29d42f03c0582e9edfa60b4dba83cd061b43fc1517baf sha256:5627550f42cd77d1b4cd182983b8cbc17cbe5069674d1a72e2ab50517413dd6c
            rules-lab.breakdown.reaction.v1 sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd
            rules-lab.breakdown.truck-cost.v1 sha256:f1616ec0f821051dc6d9f0ea1c09c8fecf4d519af1fa142e26cdf76a2aa220b2 sha256:f340426b9476bcfc887d8136124f0fcdb9547713b01b0ec5edd00ec58730eba6
            rules-lab.breakdown.truck-one-point.v1 sha256:4b64bfbfa152731ff8ef8adc7d1cd94ba69931239320e464d160906532181232 sha256:18c973657b462e27f35557c6903bf6cc81cd5fb5faa21c6323b78f434b333e1b
            rules-lab.breakdown.truck.v1 sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3 sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee
            17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a
            cna-1979.1.land-sequence sha256:42b4e721252ddf5f1107927d12b3a7f0c7ed956b08c766a3b9e2d28e3601ab9c
            cna-1979.1.initiative-ratings sha256:d9219cffbba133c7531f00471e9619b27602cf9dfd87ee105f103cb646122c96
            cna-1979.1.content-vocabulary sha256:d12a8f280cc0b7546981e102824ca88edb12cf96cb8fe0f870c786254aa94e7f
            cna-1979.1.random-procedure sha256:f677c8726634fd6997f7335e02684f13f18a042264164712ce003acf4a56165a
            cna-1979.1.weather-tables sha256:10c92c736d61c6f88359b203d0a735df0d8a676b60e170b79b46053cfd223037
            cna-1979.1.reserve-designation sha256:3d5fb13758e4539a14c89f0c884abd230f8a3a14f0e57be68aa914716278c0ca
            cna-1979.1.movement-tables sha256:9d016292838fb9ad3397d699ecfab10e0d867eeee09ed3d7c4f78a26b3394ba5
            cna-1979.1.zoc-rules sha256:2bf4d85240fbd373ed0dd3703f038a0ac712027a2175436344c71547e79bb0a2
            cna-1979.1.breakdown-tables sha256:0459f4a9ebb4ece6237f789000d3ec4fcddbf7ee3d341012d81ce77fb6ef3e08
            """, actual);
    }

    [Theory]
    [InlineData(1, "sha256:e6f9d8de65a7f01fec9089d3f58e8f94871214a7d628c52508c9aecadca68dc6", "sha256:b0ac6f247766a4fbf45f11dae2d69c2893af0128f834a3c5683e6642522563aa")]
    [InlineData(12, "sha256:faba55d645dc442d1fb0a163c0a74357800c0029e6a5c5a9e86704ebb455fab7", "sha256:0998845c6cf3d317b391a50a22b94a13046e3cf81ee01a64b2caee6e5803b74c")]
    public void DedicatedSyntheticVariantsDeclareTwoTurnsAndContestedInitiative(int start, string setupHash, string contentHash)
    {
        var setup = Assert.Single(Cna1979BreakdownSetupCatalog.Definitions,
            value => value.SetupId == $"rules-lab.breakdown.turn-boundary.gt{start}.v1");
        var artifact = Cna1979SyntheticContentCatalog.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash).Artifact!;
        var scenario = Assert.Single(artifact.Definition.LegacyDefinition.Scenarios);
        Assert.Equal(start, scenario.Start.GameTurn);
        Assert.Equal(start + 1, scenario.End.GameTurn);
        Assert.IsType<ContestedInitiative>(setup.InitialInitiative);
        Assert.True(setup.IsSynthetic);
        Assert.Equal(2, setup.OpeningPreamble.ContractVersion);
        Assert.Equal(setupHash, setup.Hash);
        Assert.Equal(contentHash, artifact.Identity.Hash);
        Assert.Equal(setup.Snapshot, CampaignSetupV6Codec.Deserialize(CampaignSetupV6Codec.Serialize(setup.Snapshot)));
        Assert.Equal(artifact.GetCanonicalBytes(), ContentPackV6Serializer.SerializeCanonical(artifact.Definition));
    }
}
