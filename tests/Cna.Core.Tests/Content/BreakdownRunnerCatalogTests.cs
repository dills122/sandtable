using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class BreakdownRunnerCatalogTests
{
    [Theory]
    [InlineData("reaction.adjacent")]
    [InlineData("reaction.low-defense")]
    [InlineData("reaction.headquarters")]
    [InlineData("reaction.noncombat")]
    [InlineData("reaction.recurrence")]
    [InlineData("reaction.last-cp")]
    [InlineData("reaction.truck-mover")]
    [InlineData("truck-cost")]
    [InlineData("truck-one-point")]
    [InlineData("battalion-matrix")]
    public void RunnerPackIsCertifiedCanonicalAndEveryScenarioHasAnAdmittedSetup(string variant)
    {
        var artifact = Find(variant);
        Assert.Equal(ContentPackV6Definition.SupportedCapabilityProfileId, artifact.Definition.CapabilityProfileId);
        Assert.Equal(artifact.GetCanonicalBytes(), ContentPackV6Serializer.SerializeCanonical(artifact.Definition));
        Assert.Same(artifact, Cna1979SyntheticContentCatalog.ResolveV6(artifact.Identity.PackId, artifact.Identity.Hash).Artifact);
        Assert.Equal(ContentCatalogRejectionReason.HashMismatch,
            Cna1979SyntheticContentCatalog.ResolveV6(artifact.Identity.PackId, Cna1979SyntheticContentCatalog.ArtifactV6.Identity.Hash).RejectionReason);
        foreach (var scenario in artifact.Definition.LegacyDefinition.Scenarios)
        {
            var setup = Assert.Single(Cna1979BreakdownSetupCatalog.Definitions,
                value => value.Content.Pack == artifact.Identity && value.Content.ScenarioId == scenario.ScenarioId);
            Assert.True(CampaignWorldV6Validator.IsValidInitial(CampaignWorldV6Factory.CreateInitial(artifact, scenario), artifact, scenario));
            var created = CampaignAuthority.Create(new CampaignCreationRequest(1, $"catalog-{variant}",
                Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash,
                artifact.Identity.PackId, artifact.Identity.Hash, scenario.ScenarioId));
            Assert.True(created.IsCreated, created.RejectionReason.ToString());
        }
    }

    [Fact]
    public void ZeroCohortBattalionsUseTheSameProfileWithoutClaimingACohortCapability()
    {
        var artifact = Find("battalion-matrix");
        Assert.All(artifact.Definition.LegacyDefinition.Elements, element => Assert.Null(element.BreakdownVehicleCohort));
        Assert.DoesNotContain("land.breakdown-cohorts", artifact.Definition.Capabilities);
        Assert.True(ContentPackV6Validator.Validate(artifact.Definition).IsValid);
    }

    [Theory]
    [InlineData("truck-cost", false)]
    [InlineData("battalion-matrix", true)]
    public void CohortCapabilityMustMatchActualStaticCohortPresence(string variant, bool declareCohorts)
    {
        var root = JsonNode.Parse(Find(variant).GetCanonicalBytes())!.AsObject();
        var capabilities = root["capabilities"]!.AsArray().Select(value => value!.GetValue<string>())
            .Where(value => value != "land.breakdown-cohorts").ToList();
        if (declareCohorts) capabilities.Add("land.breakdown-cohorts");
        var changed = new JsonArray();
        foreach (var capability in capabilities.Order(StringComparer.Ordinal)) changed.Add(capability);
        root["capabilities"] = changed;
        Assert.False(ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())).IsSuccess);
    }

    [Fact]
    public void RunnerCatalogIdentitiesMatchPinnedCanonicalInputs()
    {
        (string SetupId, string SetupHash, string ContentHash)[] identities =
        [
            ("rules-lab.breakdown.reaction.adjacent.v1",
                "sha256:977695c69112f752fddc2f2ca48da71d9d8b4aa38c8c589ff4370044ca61a200",
                "sha256:8dc4a1fa9aa6fc244eefe3343d473f6ea547e76ca2f1035461e0996f2bd3a56b"),
            ("rules-lab.breakdown.reaction.low-defense.v1",
                "sha256:d9c90688711a4c6aec3790ae57f52e132efaa963161dbd5bf8815c1e41e8e8b2",
                "sha256:d1bd720f3d43966604b57de4312b3c11f98aaa275581b01d26183e87ea554eb3"),
            ("rules-lab.breakdown.reaction.headquarters.v1",
                "sha256:04ca8772d1540ea27faa6eb5924ef8d30dce7227c41b78f8971ea3c8010e33b9",
                "sha256:8b7b2d5da9f39b017eee5ede052bb40553f02678444572f5faca9e74650f9e02"),
            ("rules-lab.breakdown.reaction.noncombat.v1",
                "sha256:7585241f7d597e330f7e1373786324073a29d745026f22e8384aed3f16ebc3f7",
                "sha256:1819958227628d4406002885c6385b9f59d84dd454fd405fd84519bf0e808564"),
            ("rules-lab.breakdown.reaction.recurrence.v1",
                "sha256:ef1a2a1f6ebf9662f018fbe228b7e950ae6531531a798f1cd22e0838efdd8fb9",
                "sha256:43524eb63f5e6ee99375c7fc711271be611e2f5726992040b2e3919ddc0ef82e"),
            ("rules-lab.breakdown.reaction.last-cp.v1",
                "sha256:809a5f9410f60e3823379d9ed056af4c49009b2469e031568130983873b0532d",
                "sha256:58ee84eed7135cc261c1939f180e6ea20869438773d8697971d216f69430463f"),
            ("rules-lab.breakdown.reaction.truck-mover.v1",
                "sha256:ca900c88a72b4bb3a1b29d42f03c0582e9edfa60b4dba83cd061b43fc1517baf",
                "sha256:5627550f42cd77d1b4cd182983b8cbc17cbe5069674d1a72e2ab50517413dd6c"),
            ("rules-lab.breakdown.truck-cost.v1",
                "sha256:f1616ec0f821051dc6d9f0ea1c09c8fecf4d519af1fa142e26cdf76a2aa220b2",
                "sha256:f340426b9476bcfc887d8136124f0fcdb9547713b01b0ec5edd00ec58730eba6"),
            ("rules-lab.breakdown.truck-one-point.v1",
                "sha256:4b64bfbfa152731ff8ef8adc7d1cd94ba69931239320e464d160906532181232",
                "sha256:18c973657b462e27f35557c6903bf6cc81cd5fb5faa21c6323b78f434b333e1b"),
            ("rules-lab.breakdown.battalion-contested.v1",
                "sha256:1d6d7c838ac07221f4dcd5b6c08c6144b8a0ef605cadccf7427dd5ed6ca24b23",
                "sha256:1be529d7079b836f4d26d920c4bfda0ce95d74169ed9ba0b62baf100cec7efa1"),
            ("rules-lab.breakdown.battalion-matrix.v1",
                "sha256:9bbd5cdd437fdc574bc65c5d5a90a1ae130827c5c57084474258db9d8dc7cd7d",
                "sha256:1be529d7079b836f4d26d920c4bfda0ce95d74169ed9ba0b62baf100cec7efa1"),
        ];
        foreach (var (setupId, setupHash, contentHash) in identities)
        {
            Assert.True(Cna1979BreakdownSetupCatalog.TryGet(setupId, out var setup));
            Assert.Equal(setupHash, setup.Hash);
            Assert.Equal(contentHash, setup.Content.Pack.Hash);
        }
    }

    [Theory]
    [InlineData("adjacent", 2)]
    [InlineData("low-defense", 2)]
    [InlineData("headquarters", 0)]
    [InlineData("recurrence", 2)]
    [InlineData("last-cp", 2)]
    public void FirstPhasingMoveOpensTheRequiredSeparatedReactorWindow(string variant, int opportunities)
    {
        var handle = Movement(Find($"reaction.{variant}"));
        var set = CampaignLegalActions.Query(handle, CampaignActionAudience.Axis).ActionSet!;
        var move = Assert.Single(set.Candidates.OfType<MoveElementAction>(),
            candidate => candidate.ElementId == "axis-phasing-a" && candidate.DestinationLocationId == "trigger-a");
        handle = Submit(handle, set, move);
        Assert.Equal(CampaignPositionV11Kind.Reaction, handle.CurrentSnapshot!.CurrentPosition.Kind);
        Assert.Equal(opportunities, handle.CurrentSnapshot.ReactionWindow!.FrozenOpportunities.Count);
    }

    [Fact]
    public void TruckOnlyNeighborDoesNotOpenAReactionWindow()
    {
        var handle = Movement(Find("reaction.noncombat"));
        var set = CampaignLegalActions.Query(handle, CampaignActionAudience.Axis).ActionSet!;
        handle = Submit(handle, set, Assert.Single(set.Candidates.OfType<MoveElementAction>()));
        Assert.Null(handle.CurrentSnapshot!.ReactionWindow);
        Assert.Equal(CampaignPositionV11Kind.Sequence, handle.CurrentSnapshot.CurrentPosition.Kind);
    }

    private static ContentPackV6Artifact Find(string variant) => Assert.Single(
        Cna1979SyntheticContentCatalog.BreakdownRunnerArtifactsV6,
        artifact => artifact.Identity.PackId == $"rules-lab.content.breakdown-{variant}.v1");

    private static CampaignAuthorityHandle Movement(ContentPackV6Artifact artifact)
    {
        var setup = Assert.Single(Cna1979BreakdownSetupCatalog.Definitions,
            value => value.Content.Pack == artifact.Identity);
        var created = CampaignAuthority.Create(new CampaignCreationRequest(1, "runner-catalog-trajectory",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash,
            artifact.Identity.PackId, artifact.Identity.Hash, setup.Content.ScenarioId));
        Assert.True(created.IsCreated, created.RejectionReason.ToString());
        var handle = created.Handle!;
        for (var step = 0; step < 20 && handle.CurrentSnapshot!.CurrentPosition.SequenceContext.SegmentId != LandSegmentIds.Movement; step++)
        {
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignLegalActions.Query(handle, audience).ActionSet!)
                .Single(value => value.Candidates.Count > 0);
            handle = Submit(handle, set, set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is ActFirstAction) ?? set.Candidates[0]);
        }
        Assert.Equal(LandSegmentIds.Movement, handle.CurrentSnapshot!.CurrentPosition.SequenceContext.SegmentId);
        return handle;
    }

    private static CampaignAuthorityHandle Submit(CampaignAuthorityHandle handle, CampaignLegalActionSet set,
        CampaignActionCandidate candidate)
    {
        var result = CampaignLegalActions.Submit(handle, new CampaignActionSubmission(1, set.CampaignId,
            set.StateVersion, set.PositionId, set.Audience, candidate.ActionId));
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorHandle!;
    }
}
