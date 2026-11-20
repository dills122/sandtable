using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// Task 021A1 (dormant, internal): closed Combat capability manifest. No runtime caller; activation
/// (021A2) and restore wiring (021A3) are later slices. Owner ruling T021-Q1 option c: today's
/// Combat-capable content must fail closure on the round kinds the frozen audience contract cannot
/// serve, rather than the manifest pretending they are covered.
/// </summary>
public sealed class CombatCapabilityManifestTests
{
    private const string DoubleFresh = "rules-lab.breakdown.reaction.combat-double-fresh.v1";

    private static readonly string[] ExpectedRegister =
    [
        "combat-selection", "retreat-before-assault", "force-assignment", "noncombat-contact-assault",
        "post-assault-retreat", "custody", "reserve-release", "cycle-control", "repeat-movement",
        "repeat-combat-selection", "second-side-combat-selection", "multiple-opportunity-reaction",
        "positive-vehicle-breakdown", "two-runtime-assaults", "general-resupply", "real-rba-movement",
        "released-reserve-offensive-combat", "motorized-retreat", "gun-armor-combat", "mandatory-attacks",
        "new-phase-slot-execution", "hosted-intelligence", "voluntary-surrender",
    ];

    // Owner decisions that ordinary Core reaches without a frozen audience decision (021A0 evidence),
    // the two Task 004 capabilities the native repeat cycle realizes, and fail-closed second side.
    private static readonly string[] UncoveredWhenAssaultCapable =
    [
        "post-assault-retreat", "repeat-movement", "repeat-combat-selection", "second-side-combat-selection",
        "two-runtime-assaults", "released-reserve-offensive-combat",
    ];

    [Fact]
    public void EveryBreakdownSetupDerivesTheClosedRegisterDeterministically()
    {
        Assert.Equal(ExpectedRegister, CampaignCombatCapabilityManifest.FamilyIds);
        foreach (var definition in Cna1979BreakdownSetupCatalog.Definitions)
        {
            var (setup, artifact) = Configuration(definition.SetupId);
            var manifest = Derive(setup, artifact);
            Assert.Equal(ExpectedRegister, manifest.Families.Select(value => value.FamilyId));
            var bytes = manifest.SerializeCanonical();
            Assert.Equal(bytes, Derive(setup, artifact).SerializeCanonical());
            Assert.Equal(bytes, CampaignCombatCapabilityManifestCodec.Read(bytes).SerializeCanonical());
            Assert.True(CampaignCombatCapabilityManifest.Revalidate(bytes, Cna1979Ruleset.Manifest.Hash, setup, artifact));
            Assert.Matches("^sha256:[0-9a-f]{64}$", manifest.Hash);
            Assert.Equal(artifact.Definition.CapabilityProfileId, manifest.CapabilityProfileId);
            // Every family either has the full coverage triple or none at all.
            Assert.All(manifest.Families, family => Assert.Equal(
                family.Disposition == CampaignCombatCapabilityDisposition.Covered, family.Coverage is not null));
        }
    }

    [Fact]
    public void TodaysContentFailsClosureOnlyOnTheUncoveredRoundKinds()
    {
        foreach (var definition in Cna1979BreakdownSetupCatalog.Definitions)
        {
            var manifest = Derive(Configuration(definition.SetupId));
            Assert.False(manifest.IsClosed, definition.SetupId);
            Assert.Subset(UncoveredWhenAssaultCapable.ToHashSet(), manifest.UncoveredFamilyIds.ToHashSet());
            Assert.Contains("second-side-combat-selection", manifest.UncoveredFamilyIds);
            using var document = JsonDocument.Parse(manifest.SerializeCanonical());
            Assert.False(document.RootElement.GetProperty("closed").GetBoolean());
        }

        // The frozen goldens' fixture is assault-capable: exactly the full uncovered set.
        var fresh = Derive(Configuration(DoubleFresh));
        Assert.Equal(UncoveredWhenAssaultCapable, fresh.UncoveredFamilyIds);
        Assert.Equal(CampaignCombatCapabilityDisposition.SourceConditionAbsent, Disposition(fresh, "gun-armor-combat"));
        Assert.Equal(CampaignCombatCapabilityDisposition.SourceConditionAbsent, Disposition(fresh, "motorized-retreat"));
        Assert.Equal(CampaignCombatCapabilityDisposition.SourceConditionAbsent, Disposition(fresh, "mandatory-attacks"));
        Assert.Equal(CampaignCombatCapabilityDisposition.PolicyExcluded, Disposition(fresh, "real-rba-movement"));
        Assert.Equal(CampaignCombatCapabilityDisposition.PolicyExcluded, Disposition(fresh, "voluntary-surrender"));
    }

    [Fact]
    public void CoveredFamiliesUseExactlyTheFrozenStagesAndDurableStepKinds()
    {
        var frozenStages = typeof(CampaignCombatAudienceDecisions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.Name.EndsWith("Stage", StringComparison.Ordinal))
            .Select(field => (string)field.GetRawConstantValue()!).ToHashSet();
        var stepKinds = typeof(CampaignHostAcceptedStep)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.Name.EndsWith("Kind", StringComparison.Ordinal))
            .Select(field => (string)field.GetRawConstantValue()!).ToHashSet();
        var covered = Derive(Configuration(DoubleFresh)).Families
            .Where(value => value.Coverage is not null).Select(value => value.Coverage!).ToArray();
        Assert.All(covered, coverage => Assert.Contains(coverage.AudienceStage, frozenStages));
        Assert.All(covered, coverage => Assert.Contains(coverage.Handler, stepKinds));
        Assert.Equal(frozenStages.Order(StringComparer.Ordinal),
            covered.Select(value => value.AudienceStage).Distinct().Order(StringComparer.Ordinal));
        // No uncovered family names a frozen stage: the Host stage names have no audience decision.
        foreach (var family in UncoveredWhenAssaultCapable)
            Assert.DoesNotContain(family, frozenStages);
    }

    [Fact]
    public void NonInfantryComponentIsRefusedAtAdmissionAndStillDerivesUncovered()
    {
        var (setup, artifact) = Configuration(DoubleFresh);
        var definition = artifact.Definition;
        var target = definition.ElementCombatFacts.First(value => value.Components.Count > 0);
        var armored = new ContentPackV6Definition(definition.LegacyDefinition,
            definition.ElementCombatFacts.Select(value => value.ElementId != target.ElementId ? value
                : new ContentElementCombatFacts(value.ElementId, value.CombatClassificationId,
                    value.Components.Select((component, index) => index != 0 ? component
                        : new ContentCombatComponent(component.ComponentId, Cna1979Combat.ArmorComponentClassId,
                            component.MaximumToe, component.DefensiveCloseAssaultRating, component.Origin)),
                    value.Origin)),
            definition.InitialPlacementCombatFacts, definition.CapabilityProfileId);
        // Content V6 admission already refuses the armored component (BRK-CERT-004: the component
        // class vocabulary is infantry-only), so the Barrage V14 / Anti Armor V20 infantry-only throws
        // are unreachable for admitted content, as 020C showed for ZOC. The manifest still derives the family from
        // the facts, so a later vocabulary change cannot slip past closure.
        var validation = ContentPackV6Validator.Validate(armored);
        Assert.False(validation.IsValid);
        Assert.Equal(["BRK-CERT-004"], validation.Issues.Select(issue => issue.Code).Distinct());
        Assert.All(validation.Issues, issue => Assert.EndsWith("/componentClassId", issue.Path));
        var families = CampaignCombatCapabilityManifest.DeriveFamilies(armored);
        Assert.Equal(CampaignCombatCapabilityDisposition.Uncovered,
            families.Single(value => value.FamilyId == "gun-armor-combat").Disposition);
        Assert.Equal(CampaignCombatCapabilityDisposition.SourceConditionAbsent,
            CampaignCombatCapabilityManifest.DeriveFamilies(definition)
                .Single(value => value.FamilyId == "gun-armor-combat").Disposition);
        // No V6 artifact can carry it, and the setup pins the original pack in any case.
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Artifact.Create(armored));
        Assert.Equal(artifact.Identity, setup.Content.Pack);
    }

    [Fact]
    public void MotorizedCombatElementMakesMotorizedRetreatReachable()
    {
        var definition = Configuration(DoubleFresh).Artifact.Definition;
        var legacy = definition.LegacyDefinition;
        var combat = definition.ElementCombatFacts.First(value => value.Components.Count > 0).ElementId;
        var motorized = new ContentPackV6Definition(ContentTestData.Copy(legacy, elements: legacy.Elements
                .Select(value => value.ElementId != combat ? value
                    : new ContentCombatElement(value.ElementId, value.SideId, value.ParentFormationId,
                        value.OrganizationId, Cna1979Movement.MotorizedMobilityId, value.BaseCapabilityPointAllowance,
                        value.PlacementMode, value.Origin, value.BreakdownVehicleCohort))),
            definition.ElementCombatFacts, definition.InitialPlacementCombatFacts, definition.CapabilityProfileId);
        Assert.False(ContentPackV6Validator.Validate(motorized).IsValid);
        Assert.Equal(CampaignCombatCapabilityDisposition.Uncovered,
            CampaignCombatCapabilityManifest.DeriveFamilies(motorized)
                .Single(value => value.FamilyId == "motorized-retreat").Disposition);
    }

    [Fact]
    public void OneSidedCombatContentMakesAssaultResultFamiliesAbsent()
    {
        var definition = Configuration(DoubleFresh).Artifact.Definition;
        var legacy = definition.LegacyDefinition;
        var commonwealth = legacy.Elements.Where(value => value.SideId == "commonwealth")
            .Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var oneSided = new ContentPackV6Definition(legacy,
            definition.ElementCombatFacts.Select(value => !commonwealth.Contains(value.ElementId) ? value
                : ContentElementCombatFacts.CreateV6(value.ElementId, Cna1979Combat.TruckConvoyClassificationId, [],
                    value.Origin)),
            definition.InitialPlacementCombatFacts, definition.CapabilityProfileId);
        var families = CampaignCombatCapabilityManifest.DeriveFamilies(oneSided);
        foreach (var id in new[] { "post-assault-retreat", "repeat-movement", "repeat-combat-selection",
                     "two-runtime-assaults", "released-reserve-offensive-combat", "force-assignment", "custody" })
            Assert.Equal(CampaignCombatCapabilityDisposition.SourceConditionAbsent,
                families.Single(value => value.FamilyId == id).Disposition);
        // Selection, the decline and the second side stay reachable; only the fail-closed side is open.
        Assert.Equal(["second-side-combat-selection"], families
            .Where(value => value.Disposition == CampaignCombatCapabilityDisposition.Uncovered).Select(value => value.FamilyId));
    }

    [Fact]
    public void StrictReaderRejectsEveryRegisterMutation()
    {
        var bytes = Derive(Configuration(DoubleFresh)).SerializeCanonical();
        var mutations = new (string Name, Action<JsonObject> Mutate)[]
        {
            ("delete family", root => Families(root).RemoveAt(4)),
            ("unknown family", root => Families(root).Add(new JsonObject
            {
                ["familyId"] = "surprise-engagement", ["disposition"] = "uncovered",
                ["authority"] = "x", ["coverage"] = null,
            })),
            ("rename family", root => Families(root)[19]!["familyId"] = "zoc-assault"),
            ("reorder families", root =>
            {
                var first = Families(root)[0]!.DeepClone();
                Families(root).RemoveAt(0);
                Families(root).Add(first);
            }),
            ("duplicate family", root => Families(root).Insert(1, Families(root)[0]!.DeepClone())),
            ("policy flipped to absent", root => Family(root, "voluntary-surrender")["disposition"] = "source-condition-absent"),
            ("policy flipped to uncovered", root => Family(root, "real-rba-movement")["disposition"] = "uncovered"),
            ("absent flipped to covered", root => Family(root, "mandatory-attacks")["disposition"] = "covered"),
            ("always-reachable family marked absent", root => Family(root, "combat-selection")["disposition"] = "source-condition-absent"),
            ("uncovered marked covered", root => Family(root, "post-assault-retreat")["disposition"] = "covered"),
            ("unknown disposition", root => Family(root, "custody")["disposition"] = "allowed"),
            ("coverage handler changed", root => Family(root, "custody")["coverage"]!["handler"] = "contact-rba-move"),
            ("coverage stage invented", root => Family(root, "post-assault-retreat")["coverage"] = new JsonObject
            {
                ["audienceStage"] = "post-assault-retreat", ["action"] = "refuse", ["handler"] = "contact-post-assault-retreat",
            }),
            ("coverage removed", root => Family(root, "force-assignment")["coverage"] = null),
            ("authority changed", root => Family(root, "voluntary-surrender")["authority"] = "CMB-POL-001"),
            ("stale closed bit", root => root["closed"] = true),
            ("extra field", root => root["certified"] = true),
            ("manifest id", root => root["manifestId"] = "sandtable.combat-capability-manifest.v2"),
            ("contract version", root => root["contractVersion"] = 2),
            ("family field order", root =>
            {
                var family = Family(root, "custody");
                var authority = family["authority"]!.DeepClone();
                family.Remove("authority");
                family["authority"] = authority;
            }),
        };
        foreach (var (name, mutate) in mutations)
        {
            var root = JsonNode.Parse(bytes)!.AsObject();
            mutate(root);
            var mutated = Encoding.UTF8.GetBytes(root.ToJsonString());
            Assert.False(mutated.AsSpan().SequenceEqual(bytes), name);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCapabilityManifestCodec.Read(mutated));
        }

        var indented = Encoding.UTF8.GetBytes(JsonNode.Parse(bytes)!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCapabilityManifestCodec.Read(indented));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCapabilityManifestCodec.Read(bytes.AsSpan(0, bytes.Length - 1)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCapabilityManifestCodec.Read([]));
    }

    [Fact]
    public void RevalidationRejectsAnyDriftFromAFreshDerivation()
    {
        var (setup, artifact) = Configuration(DoubleFresh);
        var bytes = Derive(setup, artifact).SerializeCanonical();
        var ruleset = Cna1979Ruleset.Manifest.Hash;
        Assert.True(CampaignCombatCapabilityManifest.Revalidate(bytes, ruleset, setup, artifact));

        // A register-legal, canonical, re-signed flip still differs from what the content derives.
        var root = JsonNode.Parse(bytes)!.AsObject();
        Family(root, "gun-armor-combat")["disposition"] = "uncovered";
        var flipped = Encoding.UTF8.GetBytes(root.ToJsonString());
        Assert.Equal(flipped, CampaignCombatCapabilityManifestCodec.Read(flipped).SerializeCanonical());
        Assert.False(CampaignCombatCapabilityManifest.Revalidate(flipped, ruleset, setup, artifact));

        // Another configuration's manifest, a foreign ruleset, and tampered identity all fail.
        var (otherSetup, otherArtifact) = Configuration("rules-lab.breakdown.reaction.combat-assault.v1");
        var other = Derive(otherSetup, otherArtifact).SerializeCanonical();
        Assert.False(CampaignCombatCapabilityManifest.Revalidate(other, ruleset, setup, artifact));
        Assert.False(CampaignCombatCapabilityManifest.Revalidate(bytes, new string('0', 64), setup, artifact));
        Assert.False(CampaignCombatCapabilityManifest.Revalidate(bytes, ruleset, otherSetup, otherArtifact));
        var tampered = JsonNode.Parse(bytes)!.AsObject();
        tampered["setupHash"] = otherSetup.SetupHash;
        Assert.False(CampaignCombatCapabilityManifest.Revalidate(Encoding.UTF8.GetBytes(tampered.ToJsonString()),
            ruleset, setup, artifact));
        Assert.Throws<ArgumentException>(() => CampaignCombatCapabilityManifest.Derive(ruleset, setup, otherArtifact));
    }

    [Fact]
    public void DoubleFreshManifestBytesArePinned()
    {
        // Draft pin for owner review (not frozen). Any register, authority or encoding change must
        // fail here and move to a new manifest contract version.
        var manifest = Derive(Configuration(DoubleFresh));
        Assert.Equal(PinnedDoubleFreshHash, manifest.Hash);
    }

    private const string PinnedDoubleFreshHash = "sha256:0d2f026d28e4cde685b43189653b20665753cb4dadb2c529a600ce45ebf3a4eb";

    private static JsonArray Families(JsonObject root) => root["families"]!.AsArray();

    private static JsonObject Family(JsonObject root, string familyId) =>
        Families(root).Select(value => value!.AsObject()).Single(value => (string?)value["familyId"] == familyId);

    private static CampaignCombatCapabilityDisposition Disposition(CampaignCombatCapabilityManifest manifest, string familyId) =>
        manifest.Families.Single(value => value.FamilyId == familyId).Disposition;

    private static CampaignCombatCapabilityManifest Derive((CampaignSetupSnapshotV6 Setup, ContentPackV6Artifact Artifact) value) =>
        Derive(value.Setup, value.Artifact);

    private static CampaignCombatCapabilityManifest Derive(CampaignSetupSnapshotV6 setup, ContentPackV6Artifact artifact) =>
        CampaignCombatCapabilityManifest.Derive(Cna1979Ruleset.Manifest.Hash, setup, artifact);

    private static (CampaignSetupSnapshotV6 Setup, ContentPackV6Artifact Artifact) Configuration(string setupId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == setupId));
        var resolution = Cna1979SyntheticContentResolver.Instance.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash);
        Assert.True(resolution.IsResolved);
        return (setup, resolution.Artifact!);
    }
}
