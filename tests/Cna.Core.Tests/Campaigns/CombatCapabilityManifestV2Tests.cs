using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// Task 021A1b (dormant, internal): manifest v2, a superset of v1 over the contract-v2 audience stages
/// (Task 020E). Same family IDs and order; v1 bytes and pin are unchanged (CombatCapabilityManifestTests).
/// </summary>
public sealed class CombatCapabilityManifestV2Tests
{
    private const string DoubleFresh = "rules-lab.breakdown.reaction.combat-double-fresh.v1";
    private const int V1 = CampaignCombatCapabilityManifest.VersionOne;
    private const int V2 = CampaignCombatCapabilityManifest.VersionTwo;

    private static readonly string[] NowCovered =
        ["post-assault-retreat", "repeat-movement", "repeat-combat-selection", "second-side-combat-selection"];
    private static readonly string[] Reworded = ["two-runtime-assaults", "released-reserve-offensive-combat"];

    [Fact]
    public void V2KeepsTheV1FamilyListAndChangesOnlyTheNewlyDisclosedFamilies()
    {
        Assert.Equal(CampaignCombatCapabilityManifest.FamilyIdsFor(V1), CampaignCombatCapabilityManifest.FamilyIdsFor(V2));
        Assert.Equal(CampaignCombatCapabilityManifest.LatestContractVersion, V2);
        foreach (var id in CampaignCombatCapabilityManifest.FamilyIdsFor(V1))
        {
            var changed = NowCovered.Contains(id) || Reworded.Contains(id);
            Assert.Equal(!changed, CampaignCombatCapabilityManifest.AuthorityOf(id, V1)
                == CampaignCombatCapabilityManifest.AuthorityOf(id, V2));
            Assert.Equal(!NowCovered.Contains(id), CampaignCombatCapabilityManifest.CoverageOf(id, V1)
                == CampaignCombatCapabilityManifest.CoverageOf(id, V2));
            foreach (var disposition in Enum.GetValues<CampaignCombatCapabilityDisposition>())
                if (!NowCovered.Contains(id))
                    Assert.Equal(CampaignCombatCapabilityManifest.Allows(id, disposition, V1),
                        CampaignCombatCapabilityManifest.Allows(id, disposition, V2));
        }
        Assert.All(NowCovered, id => Assert.True(CampaignCombatCapabilityManifest.Allows(id,
            CampaignCombatCapabilityDisposition.Covered, V2)));
        Assert.All(NowCovered, id => Assert.False(CampaignCombatCapabilityManifest.Allows(id,
            CampaignCombatCapabilityDisposition.Uncovered, V2)));
    }

    [Fact]
    public void V2CoverageUsesEveryPublicStageAndExistingDurableStepKinds()
    {
        var stages = PublicConstants(typeof(CampaignCombatAudienceDecisions), "Stage");
        var stepKinds = PublicConstants(typeof(CampaignHostAcceptedStep), "Kind");
        var covered = Derive(DoubleFresh, V2).Families.Where(value => value.Coverage is not null)
            .Select(value => value.Coverage!).ToArray();
        Assert.All(covered, coverage => Assert.Contains(coverage.Handler, stepKinds));
        Assert.Equal(stages.Order(StringComparer.Ordinal),
            covered.Select(value => value.AudienceStage).Distinct().Order(StringComparer.Ordinal));
        Assert.Equal(CampaignCombatAudienceDecisions.SelectionStage,
            CampaignCombatCapabilityManifest.CoverageOf("second-side-combat-selection", V2)!.AudienceStage);
    }

    [Fact]
    public void AssaultCapableContentStaysOpenOnlyOnTheRepeatedAssaultCapabilities()
    {
        foreach (var definition in Cna1979BreakdownSetupCatalog.Definitions)
        {
            var manifest = Derive(definition.SetupId, V2);
            Assert.True(manifest.IsClosed || manifest.UncoveredFamilyIds.SequenceEqual(Reworded), definition.SetupId);
        }
        var fresh = Derive(DoubleFresh, V2);
        Assert.Equal(Reworded, fresh.UncoveredFamilyIds);
        Assert.All(NowCovered, id => Assert.Equal(CampaignCombatCapabilityDisposition.Covered,
            fresh.Families.Single(value => value.FamilyId == id).Disposition));
    }

    [Fact]
    public void ReaderAndRevalidationRejectCrossVersionMutations()
    {
        var (setup, artifact) = Configuration(DoubleFresh);
        var ruleset = Cna1979Ruleset.Manifest.Hash;
        var v1 = CampaignCombatCapabilityManifest.Derive(ruleset, setup, artifact, V1).SerializeCanonical();
        var v2 = CampaignCombatCapabilityManifest.Derive(ruleset, setup, artifact, V2).SerializeCanonical();
        Assert.Equal(V2, CampaignCombatCapabilityManifestCodec.Read(v2).ContractVersion);
        Assert.True(CampaignCombatCapabilityManifest.Revalidate(v1, ruleset, setup, artifact));
        Assert.True(CampaignCombatCapabilityManifest.Revalidate(v2, ruleset, setup, artifact));

        var mutations = new (string Name, byte[] Source, Action<JsonObject> Mutate)[]
        {
            ("v2 labelled v1", v2, root => { root["contractVersion"] = 1; root["manifestId"] = "sandtable.combat-capability-manifest.v1"; }),
            ("v1 labelled v2", v1, root => { root["contractVersion"] = 2; root["manifestId"] = "sandtable.combat-capability-manifest.v2"; }),
            ("v2 id mismatch", v2, root => root["manifestId"] = "sandtable.combat-capability-manifest.v1"),
            ("unknown version", v2, root => { root["contractVersion"] = 3; root["manifestId"] = "sandtable.combat-capability-manifest.v3"; }),
            ("retreat back to uncovered", v2, root => Uncover(root, "post-assault-retreat")),
            ("second side back to uncovered", v2, root => Uncover(root, "second-side-combat-selection")),
            ("v2 coverage on v1 stage", v2, root => Family(root, "repeat-movement")["coverage"]!["audienceStage"] = "cycle-control"),
        };
        foreach (var (name, source, mutate) in mutations)
        {
            var root = JsonNode.Parse(source)!.AsObject();
            mutate(root);
            var bytes = Encoding.UTF8.GetBytes(root.ToJsonString());
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCapabilityManifestCodec.Read(bytes));
            Assert.False(CampaignCombatCapabilityManifest.Revalidate(bytes, ruleset, setup, artifact), name);
        }
    }

    [Fact]
    public void DoubleFreshV2ManifestBytesArePinned()
    {
        // Draft pin for owner review (not frozen).
        Assert.Equal(PinnedV2Hash, Derive(DoubleFresh, V2).Hash);
    }

    private const string PinnedV2Hash = "sha256:9bc704fea6172e625982796d61b85381bd319c4ab322a9d4946894545ab2fff1";

    private static void Uncover(JsonObject root, string familyId)
    {
        var family = Family(root, familyId);
        family["disposition"] = "uncovered";
        family["coverage"] = null;
    }

    private static JsonObject Family(JsonObject root, string familyId) =>
        root["families"]!.AsArray().Select(value => value!.AsObject())
            .Single(value => (string?)value["familyId"] == familyId);

    private static HashSet<string> PublicConstants(Type type, string suffix) => type
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.IsLiteral && field.Name.EndsWith(suffix, StringComparison.Ordinal))
        .Select(field => (string)field.GetRawConstantValue()!).ToHashSet(StringComparer.Ordinal);

    private static CampaignCombatCapabilityManifest Derive(string setupId, int version)
    {
        var (setup, artifact) = Configuration(setupId);
        return CampaignCombatCapabilityManifest.Derive(Cna1979Ruleset.Manifest.Hash, setup, artifact, version);
    }

    private static (CampaignSetupSnapshotV6 Setup, ContentPackV6Artifact Artifact) Configuration(string setupId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == setupId));
        var resolution = Cna1979SyntheticContentResolver.Instance.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash);
        Assert.True(resolution.IsResolved);
        return (setup, resolution.Artifact!);
    }
}
