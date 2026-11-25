using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Setups;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Content;

public sealed class FrozenTurnBoundaryIdentityTests
{
    [Fact]
    public void ExistingCanonicalIdentities()
    {
        var legacySetups = Cna1979SetupCatalog.Definitions.Select(setup => $"legacy {setup.SetupId} {setup.Hash} {setup.Content.Pack.Hash}").Order(StringComparer.Ordinal);
        var v6Bytes = Cna1979BreakdownSetupCatalog.Definitions.Where(setup => !setup.SetupId.StartsWith("rules-lab.breakdown.turn-boundary.", StringComparison.Ordinal)).Select(setup =>
            $"v6 {setup.SetupId} {Convert.ToHexStringLower(SHA256.HashData(CampaignSetupV6Codec.Serialize(setup.Snapshot)))}").Order(StringComparer.Ordinal);
        var streams = SequencingScenarios.All.Select(s => s.Samples[^1]).Where(s => !s.Scenario.StartsWith("turn-boundary-", StringComparison.Ordinal)).Select(s =>
            $"stream {s.Scenario} {CampaignExercises.Reconstruct(s.Session).EventStreamHash}").Order(StringComparer.Ordinal);
        Assert.Equal(
            """
            legacy rules-lab.initiative.contested sha256:8ae5d9d7c922c45376690bd8fd1de93f9937ff757dca0084ca9372dd954c014a sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79
            legacy rules-lab.initiative.predetermined sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79
            legacy rules-lab.reaction.adjacent sha256:4fd85fa0cb89f61cf2cb7fed02acb195c9b221f793a13e39de1bea166f2d16a1 sha256:a727253192758145893c13a9fa8b99980400c2f0c00949331cb1174d912b28bd
            legacy rules-lab.reaction.headquarters sha256:5c7c1822900d9a1990a89ec1fed166284555a9966a75bab310138aa692592d8d sha256:74bfbb74455564e2ef48e51b76049d87732728563392727b6b94f56717d4d4dd
            legacy rules-lab.reaction.low-defense sha256:c3937f349169b40f30e3179d17a97ec49a22c12561c4789e5a4f747af4471c9c sha256:c103101aadf6a89b4595f41a4d6a08481d365bb64b9897edbc6ce1cfaf70f11e
            legacy rules-lab.reaction.noncombat sha256:52645c55148bcf79ad9b50ea3d0ae546c27b7652bafd30a9fff41eae76a884a4 sha256:4b2b8bd225176da42e7b1a2f8ab4818094e92a37f788eb47cbd77fc6f5cacaff
            legacy rules-lab.reaction.positive-zoc sha256:83840e03f27c89b32c1156deae9fbadbfa5a6bfc5ac0e29c89d7742d0c77cea2 sha256:7ac47f836c3ae25bee9926ffb5b4f3f4355b04cc4fb1eff00468a1519a38265c
            legacy rules-lab.reaction.recurrence sha256:cc23ac1cf7600ee083a410e41a3c027a14aabdbe3fdcf37ce884ff8edc42e3f5 sha256:f23fa5c42251dd2941e9321bce2efdc6f47152af27c978731a031824ce8800c0
            legacy rules-lab.reaction.remote-zoc sha256:06ae89932d146d15ac3ce942c44d9872d0e9583788054012209e747f92132c8e sha256:5a3262777c882feccefa3c3ed49867b8ccae780dbac11975ed2a364fae2e0652
            v6 rules-lab.breakdown.battalion-contested.v1 d056553bf96a404c905127a25f2fae4f07878b0de33fe8543d05d0435209fa1d
            v6 rules-lab.breakdown.battalion-matrix.v1 2d4dbdeddb72e0945f9a498108b25db2cd02d58c7df3047d8e5e66e47be48ccb
            v6 rules-lab.breakdown.reaction.adjacent.v1 04be07c100b976af41c583f8bca1da206067813c6930986506e945a87d5bf3a8
            v6 rules-lab.breakdown.reaction.combat-assault.v1 81f82ae656512d9a600fd20df7ff9de0bed3402a6c177f389629bbea6541c926
            v6 rules-lab.breakdown.reaction.combat-double-fresh.v1 93f54896e879e94025cfd0cd6f63a0a0ef010b38b2e9878af0a098fc885f2afd
            v6 rules-lab.breakdown.reaction.combat-double.v1 9be3267705cc6df76823cf0470a0fefb15ad1e8dbaa3b6c82b035557a2b12772
            v6 rules-lab.breakdown.reaction.combat-infantry.v1 47cb7100fa681985ade11ffe70051c9afb56047d0c044d9b4442d611a038524c
            v6 rules-lab.breakdown.reaction.combat-triple.v1 30d54b873e57a2bc7309cf570fcce2902912eaef0ec859855e4f5fddb7c03b8f
            v6 rules-lab.breakdown.reaction.headquarters.v1 5c43b07f33b7e24dcb9ba198f86d432d3515fd9638c63d8b4dbf29059e143eed
            v6 rules-lab.breakdown.reaction.last-cp.v1 1fe113c16c57f2c1b73cf1ee2b088929f108b5eaeca7571839f395183f6d99c9
            v6 rules-lab.breakdown.reaction.low-defense.v1 29b74cfab71a58380ef4746c27746e365736eec90f912ad7d27c7a82c7b8ce89
            v6 rules-lab.breakdown.reaction.noncombat.v1 ac94bf11c83db5e410ed1b4f1580a8828ee6ba2ebc1d243caad5885362fd7032
            v6 rules-lab.breakdown.reaction.recurrence.v1 38b6ad1ae40582235055c6c4855b0a63fe730a9fa9efaf7c20e58646a98afefa
            v6 rules-lab.breakdown.reaction.truck-mover.v1 f30949bbc51f7f5b1bc105e15a5fa0c88a15c28cfa750db90351dcf1b025b164
            v6 rules-lab.breakdown.reaction.v1 fc066cfeb0173d3e32f00edd3ee882cb608bf1091a93cf37cf3fd492a3054115
            v6 rules-lab.breakdown.truck-cost.v1 cd64068c7a1ef06e574d65df9216f157883aabf694ccf33e1c756858efa78189
            v6 rules-lab.breakdown.truck-one-point.v1 79f3c31270701655c4b336b9dfd484195c4034c4d5e8045b86c1a34993d6c794
            v6 rules-lab.breakdown.truck.v1 fc528041ba1b8929445ddb56fb7fb4dd8ece0945d0ae0169b62e218c5720cec6
            stream pregame-no-attack sha256:1708a7faa17883c85e82427bb59d1e1a2a7327d8ff5b6fdffe55f38006faa78d
            stream real-attack sha256:a683044cd7e699ddfeb89c1865f387ba6a22fdeeb3ef7781236e9fada63d0c2c
            stream real-attack-repeat sha256:91c489431465902a9573c02f9a1f5a8511bda4cfb06e55ed13278383dc8e0160
            """, string.Join('\n', legacySetups.Concat(v6Bytes).Concat(streams)));
    }
}
