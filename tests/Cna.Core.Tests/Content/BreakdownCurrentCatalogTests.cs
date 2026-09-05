using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class BreakdownCurrentCatalogTests
{
    [Fact]
    public void CurrentRulesetActivatesExactBreakdownCompositionAndRetainsHistoricalEight()
    {
        Assert.Equal(9, Cna1979Ruleset.ContractVersion);
        Assert.Equal(9, Cna1979Ruleset.Manifest.ContractVersion);
        Assert.Equal(Cna1979BreakdownRuleset.Manifest.Hash, Cna1979Ruleset.Manifest.Hash);
        Assert.Equal(8, Cna1979Ruleset.HistoricalManifestV8.ContractVersion);
        Assert.Equal("0e80a8ba917113b401ea709f9f2a6cd7fb7cfec03b8adbdae978f1b219e141e0",
            Cna1979Ruleset.HistoricalManifestV8.Hash);
        Assert.NotEqual(Cna1979Ruleset.HistoricalManifestV8.Hash, Cna1979Ruleset.Manifest.Hash);
        Assert.True(Cna1979Ruleset.IsCanonicalHash(Cna1979Ruleset.Manifest.Hash));
        Assert.False(Cna1979Ruleset.IsCanonicalHash(Cna1979Ruleset.HistoricalManifestV8.Hash));
        Assert.True(Cna1979Ruleset.IsHistoricalHashV8(Cna1979Ruleset.HistoricalManifestV8.Hash));
        Assert.False(Cna1979Ruleset.IsHistoricalHashV8(Cna1979Ruleset.Manifest.Hash));
    }

    [Fact]
    public void ProductionCatalogOwnsCertifiedTruckBytesAndResolvesOnlyExactSuccessorIdentity()
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        Assert.Equal(BreakdownContentFixture.Bytes(), artifact.GetCanonicalBytes());
        Assert.Equal(6, artifact.Identity.SchemaVersion);
        Assert.Equal(ContentPackV6Definition.SupportedCapabilityProfileId, artifact.Definition.CapabilityProfileId);
        Assert.NotEqual(Cna1979SyntheticContentCatalog.ArtifactV5.Identity.PackId, artifact.Identity.PackId);
        var exact = Cna1979SyntheticContentCatalog.ResolveV6(artifact.Identity.PackId, artifact.Identity.Hash);
        Assert.True(exact.IsResolved);
        Assert.Same(artifact, exact.Artifact);
        Assert.Equal(ContentCatalogRejectionReason.HashMismatch,
            Cna1979SyntheticContentCatalog.ResolveV6(artifact.Identity.PackId,
                Cna1979SyntheticContentCatalog.ArtifactV5.Identity.Hash).RejectionReason);
        Assert.Equal(ContentCatalogRejectionReason.UnknownPackId,
            Cna1979SyntheticContentCatalog.ResolveV6(Cna1979SyntheticContentCatalog.ArtifactV5.Identity.PackId,
                Cna1979SyntheticContentCatalog.ArtifactV5.Identity.Hash).RejectionReason);
    }

    [Fact]
    public void CertifiedSetupHasNewIdentityAndAdmittedInitialWorld()
    {
        var setup = Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId);
        Assert.Equal(Cna1979BreakdownSetupCatalog.TruckSetupId, setup.SetupId);
        Assert.Equal(6, setup.SchemaVersion);
        Assert.Equal(ContentPackV6Definition.SupportedCapabilityProfileId, setup.CapabilityProfileId);
        Assert.True(Cna1979BreakdownSetupCatalog.TryGet(setup.SetupId, out var resolved));
        Assert.Same(setup, resolved);
        Assert.False(Cna1979BreakdownSetupCatalog.TryGet(Cna1979SetupCatalog.PredeterminedSetupId, out _));
        Assert.DoesNotContain(Cna1979SetupCatalog.Definitions, value => value.SetupId == setup.SetupId);
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        Assert.Equal(artifact.Identity, setup.Content.Pack);
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        Assert.Equal(scenario.ScenarioId, setup.Content.ScenarioId);
        Assert.True(CampaignWorldV6Validator.IsValidInitial(
            CampaignWorldV6Factory.CreateInitial(artifact, scenario), artifact, scenario));
        Assert.Equal(setup.Hash, CampaignSetupSnapshotV6.FromDefinition(setup).SetupHash);
    }
    [Fact]
    public void CertifiedReactionVariantHasIndependentIdentityAndAdmitsPublicCreation()
    {
        var truck = Cna1979SyntheticContentCatalog.ArtifactV6;
        var reaction = Cna1979SyntheticContentCatalog.ArtifactBreakdownReactionV6;
        Assert.Equal("rules-lab.content.breakdown-reaction.v1", reaction.Identity.PackId);
        Assert.Equal(ContentPackV6Definition.SupportedCapabilityProfileId, reaction.Definition.CapabilityProfileId);
        Assert.NotEqual(truck.Identity.Hash, reaction.Identity.Hash);
        Assert.Equal("sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd", reaction.Identity.Hash);
        Assert.Equal(BreakdownContentFixture.Bytes(), truck.GetCanonicalBytes());
        var scenario = Assert.Single(reaction.Definition.LegacyDefinition.Scenarios);
        Assert.Equal("breakdown-reaction-lab", scenario.ScenarioId);
        Assert.Equal("north-east", scenario.InitialPlacements.Single(value => value.ElementId == "commonwealth-infantry").LocationId);
        Assert.Equal("north-west", scenario.InitialPlacements.Single(value => value.ElementId == "axis-infantry").LocationId);
        Assert.True(CampaignWorldV6Validator.IsValidInitial(CampaignWorldV6Factory.CreateInitial(reaction, scenario), reaction, scenario));
        var exact = Cna1979SyntheticContentCatalog.ResolveV6(reaction.Identity.PackId, reaction.Identity.Hash);
        Assert.Same(reaction, exact.Artifact);
        Assert.Equal(ContentCatalogRejectionReason.HashMismatch,
            Cna1979SyntheticContentCatalog.ResolveV6(reaction.Identity.PackId, truck.Identity.Hash).RejectionReason);
        Assert.True(Cna1979BreakdownSetupCatalog.TryGet("rules-lab.breakdown.reaction.v1", out var setup));
        Assert.Contains(Cna1979BreakdownSetupCatalog.Definitions, value => value.SetupId == "rules-lab.breakdown.truck.v1");
        Assert.Contains(Cna1979BreakdownSetupCatalog.Definitions, value => value.SetupId == "rules-lab.breakdown.reaction.v1");
        Assert.Equal("sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e", setup.Hash);
        Assert.Equal(reaction.Identity, setup.Content.Pack);
        Assert.Equal(scenario.ScenarioId, setup.Content.ScenarioId);
        var created = CampaignAuthority.Create(new CampaignCreationRequest(1, "breakdown-reaction-catalog",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash,
            reaction.Identity.PackId, reaction.Identity.Hash, scenario.ScenarioId));
        Assert.True(created.IsCreated, created.RejectionReason.ToString());
        var handle = created.Handle!;
        for (var step = 0; step < 20 && handle.CurrentSnapshot!.CurrentPosition.SequenceContext.SegmentId != LandSegmentIds.Movement; step++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignLegalActions.Query(handle, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            var candidate = set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is ActFirstAction) ?? set.Candidates[0];
            handle = Submit(handle, set, candidate);
        }
        Assert.Equal(LandSegmentIds.Movement, handle.CurrentSnapshot!.CurrentPosition.SequenceContext.SegmentId);
        var moves = CampaignLegalActions.Query(handle, CampaignActionAudience.Axis).ActionSet!;
        var trigger = Assert.Single(moves.Candidates.OfType<MoveElementAction>(), value => value.ElementId == "axis-infantry"
            && value.OriginLocationId == "north-west" && value.DestinationLocationId == "north");
        handle = Submit(handle, moves, trigger);
        Assert.Equal(CampaignPositionV11Kind.Reaction, handle.CurrentSnapshot!.CurrentPosition.Kind);
        Assert.Single(handle.CurrentSnapshot.ReactionWindow!.FrozenOpportunities);
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
