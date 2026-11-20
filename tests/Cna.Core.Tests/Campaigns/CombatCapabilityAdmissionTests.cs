using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// Task 021A2 (dormant, internal): the activation gate over the closed capability manifest. Not
/// wired into creation, Combat entry or restore (T021-Q5/Q3 open). It derives the latest manifest
/// (v2 since 021A1b). Under v1 every setup was rejected (T021-Q1 option c).
/// </summary>
public sealed class CombatCapabilityAdmissionTests
{
    private const string DoubleFresh = "rules-lab.breakdown.reaction.combat-double-fresh.v1";

    // Task 021A1b: the first admissions. Each is a configuration where one side fields no element with a
    // combat component, so no assault result is reachable (CombatCapabilityAudienceTraceTests measures
    // that these reach Combat with a public finish-only Selection decision).
    private static readonly string[] AdmittedSetups =
    [
        "rules-lab.breakdown.reaction.noncombat.v1",
        "rules-lab.breakdown.truck-cost.v1",
        "rules-lab.breakdown.truck-one-point.v1",
    ];

    [Fact]
    public void OnlyConfigurationsWithoutAReachableAssaultResultAreAdmitted()
    {
        var admitted = new List<string>();
        foreach (var definition in Cna1979BreakdownSetupCatalog.Definitions)
        {
            var (setup, artifact) = Configuration(definition.SetupId);
            var result = CampaignCombatCapabilityAdmission.Admit(Cna1979Ruleset.Manifest.Hash, setup, artifact);
            if (result.IsAdmitted)
            {
                admitted.Add(definition.SetupId);
                Assert.Equal(CampaignCreationRejectionReason.None, result.RejectionReason);
                var manifest = result.Manifest!;
                Assert.Equal(CampaignCombatCapabilityManifest.VersionTwo, manifest.ContractVersion);
                Assert.True(manifest.IsClosed);
                Assert.True(CampaignCombatCapabilityManifest.Revalidate(manifest.SerializeCanonical(),
                    Cna1979Ruleset.Manifest.Hash, setup, artifact));
                // Under v1 the same configuration is not closed: second-side selection was uncovered.
                Assert.False(CampaignCombatCapabilityManifest.Derive(Cna1979Ruleset.Manifest.Hash, setup, artifact)
                    .IsClosed);
            }
            else
            {
                Assert.Null(result.Manifest);
                Assert.Equal(CampaignCreationRejectionReason.UnsupportedCapabilityProfile, result.RejectionReason);
            }
        }
        Assert.Equal(AdmittedSetups.Order(StringComparer.Ordinal), admitted.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryRejectionCauseIsIndistinguishable()
    {
        var (setup, artifact) = Configuration(DoubleFresh);
        var (otherSetup, otherArtifact) = Configuration("rules-lab.breakdown.reaction.combat-assault.v1");
        var derived = CampaignCombatCapabilityManifest.Derive(Cna1979Ruleset.Manifest.Hash, setup, artifact);
        // Forged closed manifest: every uncovered family relabelled as absent. The register forbids
        // that for always-reachable families, so the re-read refuses it.
        var forgedClosed = Rebuild(derived, derived.Families.Select(value =>
            value.Disposition != CampaignCombatCapabilityDisposition.Uncovered ? value
                : value with { Disposition = CampaignCombatCapabilityDisposition.SourceConditionAbsent }).ToArray());
        Assert.True(forgedClosed.IsClosed);
        var unknownFamily = Rebuild(derived, derived.Families.Append(new CampaignCombatCapabilityFamily(
            "surprise-engagement", CampaignCombatCapabilityDisposition.Covered, "x",
            new CampaignCombatCapabilityCoverage("combat-selection", "x", "contact-combat-selection"))).ToArray());
        var droppedFamily = Rebuild(derived, derived.Families.Where(value => value.FamilyId != "post-assault-retreat").ToArray());

        var results = new[]
        {
            CampaignCombatCapabilityAdmission.Admit(Cna1979Ruleset.Manifest.Hash, setup, artifact),
            CampaignCombatCapabilityAdmission.Admit(Cna1979Ruleset.Manifest.Hash, null, artifact),
            CampaignCombatCapabilityAdmission.Admit(Cna1979Ruleset.Manifest.Hash, setup, null),
            CampaignCombatCapabilityAdmission.Admit(new string('0', 64), setup, artifact),
            CampaignCombatCapabilityAdmission.Admit(Cna1979Ruleset.Manifest.Hash, setup, otherArtifact),
            CampaignCombatCapabilityAdmission.Admit(Cna1979Ruleset.Manifest.Hash, otherSetup, artifact),
            CampaignCombatCapabilityAdmission.Admit(derived),
            CampaignCombatCapabilityAdmission.Admit(forgedClosed),
            CampaignCombatCapabilityAdmission.Admit(unknownFamily),
            CampaignCombatCapabilityAdmission.Admit(droppedFamily),
            CampaignCombatCapabilityAdmission.Admit(null),
        };
        Assert.All(results, result =>
        {
            Assert.Same(CampaignCombatCapabilityAdmissionResult.Rejected, result);
            Assert.Equal(CampaignCreationRejectionReason.UnsupportedCapabilityProfile, result.RejectionReason);
        });
    }

    [Fact]
    public void NoRegisterVersionOneManifestCanBeAdmitted()
    {
        // Families that are reachable for every configuration and have no frozen disclosure can only be
        // `uncovered`, so register v1 admits nothing. A first admission needs a register successor once
        // those round kinds have audience disclosure (or an owner-ruled proof of absence).
        var alwaysOpen = CampaignCombatCapabilityManifest.FamilyIds.Where(id =>
            Enum.GetValues<CampaignCombatCapabilityDisposition>().All(disposition =>
                CampaignCombatCapabilityManifest.Allows(id, disposition)
                == (disposition == CampaignCombatCapabilityDisposition.Uncovered))).ToArray();
        Assert.Equal(["second-side-combat-selection"], alwaysOpen);
    }

    [Fact]
    public void NonInfantryInvariantIsNamedAndAdmittedContentNeverTripsIt()
    {
        Assert.Equal("Unsupported: non-infantry combat component exceeds the bounded Combat profile.",
            CampaignCombatCapabilityAdmission.NonInfantryExceedsBoundedProfile);
        foreach (var definition in Cna1979BreakdownSetupCatalog.Definitions)
        {
            var (_, artifact) = Configuration(definition.SetupId);
            Assert.All(artifact.Definition.ElementCombatFacts.SelectMany(value => value.Components),
                component => Assert.Equal(Cna1979Combat.InfantryComponentClassId, component.ComponentClassId));
        }
    }

    private static CampaignCombatCapabilityManifest Rebuild(CampaignCombatCapabilityManifest source,
        IReadOnlyList<CampaignCombatCapabilityFamily> families) =>
        CampaignCombatCapabilityManifest.FromParts(source.RulesetHash, source.SetupId, source.SetupHash, source.Pack,
            source.CapabilityProfileId, families);

    private static (CampaignSetupSnapshotV6 Setup, ContentPackV6Artifact Artifact) Configuration(string setupId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == setupId));
        var resolution = Cna1979SyntheticContentResolver.Instance.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash);
        Assert.True(resolution.IsResolved);
        return (setup, resolution.Artifact!);
    }
}
