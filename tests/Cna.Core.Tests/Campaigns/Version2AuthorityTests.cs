using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class Version2AuthorityTests
{
    [Fact]
    public void CreationBindsTheRecognizedSetupAndEmbedsReplayCompleteState()
    {
        var setup = Cna1979SetupCatalog.Definitions[1];
        var command = new CreateCampaign(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.Hash);

        var result = CampaignEngine.Decide(null, command);

        Assert.True(result.IsAccepted);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.Equal(2, created.ContractVersion);
        Assert.Equal(setup.SetupId, created.Setup.SetupId);
        Assert.Equal(setup.Hash, created.Setup.SetupHash);
        Assert.Equal(setup.InitialInitiative, created.Setup.InitialInitiative);
        Assert.Equal(setup.Sources, created.Setup.Sources);
        Assert.Equal(new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0), created.RandomState);
        Assert.Equal(LandActorRole.None, created.SequencePosition.ActorRole);
        Assert.Null(created.SequencePosition.ActiveSide);
    }

    [Theory]
    [InlineData("rules-lab.unknown", "sha256:unknown")]
    [InlineData("rules-lab.initiative.contested", "sha256:wrong")]
    public void CreationRejectsUnknownOrMismatchedSetupIdentity(
        string setupId,
        string setupHash)
    {
        var result = CampaignEngine.Decide(
            null,
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setupId,
                setupHash));

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.InvalidCommand, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void NormalizedSequenceUsesUnresolvedActorRolesInsteadOfGuessedSides()
    {
        var positions = Cna1979LandSequence.CreateTurn(43);
        var declaration = positions.First(position =>
            position.OperationStage == 1
            && position.PhaseId == LandPhaseIds.InitiativeDeclaration);
        var firstActor = positions.First(position =>
            position.OperationStage == 1
            && position.PhaseId == LandPhaseIds.ReserveDesignation
            && position.PositionId.Contains(".first-player.", StringComparison.Ordinal));
        var secondActor = positions.First(position =>
            position.OperationStage == 1
            && position.PhaseId == LandPhaseIds.ReserveDesignation
            && position.PositionId.Contains(".second-player.", StringComparison.Ordinal));

        Assert.Equal(LandActorRole.InitiativeHolder, declaration.ActorRole);
        Assert.Equal(LandActorRole.FirstActingSide, firstActor.ActorRole);
        Assert.Equal(LandActorRole.SecondActingSide, secondActor.ActorRole);
        Assert.Null(declaration.ActiveSide);
        Assert.Null(firstActor.ActiveSide);
        Assert.Null(secondActor.ActiveSide);
        Assert.Contains(Cna1979LandSequence.InitiativeSideSourceReference, firstActor.Sources);
        Assert.Contains(Cna1979LandSequence.StageChoiceSourceReference, firstActor.Sources);
    }

    [Fact]
    public void CanonicalManifestCutsOverAllAuthoritativeArtifacts()
    {
        Assert.Equal(2, Cna1979Ruleset.Manifest.ContractVersion);
        Assert.Equal(
            [
                "cna-1979.1.content-vocabulary",
                "cna-1979.1.initiative-ratings",
                "cna-1979.1.land-sequence",
                "cna-1979.1.random-procedure",
            ],
            Cna1979Ruleset.Manifest.Artifacts
                .Select(artifact => artifact.ArtifactId)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ReplayRejectsLegacyGenericAdvancementAcrossInitiative()
    {
        var created = CreateEvent();
        var nextPosition = Cna1979LandSequence.GetNext(created.SequencePosition);
        CampaignEvent[] history =
        [
            created,
            new CampaignSequenceAdvanced(
                created.CampaignId,
                2,
                created.SequencePosition.PositionId,
                nextPosition),
        ];

        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignProjector.Replay(history));
    }

    [Fact]
    public void ReplayUsesTheEmbeddedSetupWithoutCurrentCatalogLookup()
    {
        var definition = new CampaignSetupDefinition(
            1,
            "retired.synthetic.setup",
            "Catalog-only display text",
            true,
            1,
            new PredeterminedInitiative(LandSide.Commonwealth),
            [new RuleReference("sandtable-rules-lab", "retired.synthetic.v1")]);
        var created = new CampaignCreated(
            "campaign-retired",
            1,
            Cna1979Ruleset.Manifest.Hash,
            CampaignSetupSnapshot.FromDefinition(definition),
            SandtableRandom.Create(7),
            Cna1979LandSequence.CreateTurn(1)[0]);

        var snapshot = CampaignProjector.Replay([created]);

        Assert.Equal("retired.synthetic.setup", snapshot.Setup.SetupId);
        Assert.False(Cna1979SetupCatalog.TryGet(snapshot.Setup.SetupId, out _));
    }

    [Fact]
    public void EmbeddedSetupDefensivelyCopiesAndComparesSourcesStructurally()
    {
        var definition = Cna1979SetupCatalog.Definitions[0];
        var sources = definition.Sources.ToList();
        var first = new CampaignSetupSnapshot(
            definition.SchemaVersion,
            definition.SetupId,
            definition.Hash,
            definition.IsSynthetic,
            definition.InitialGameTurn,
            definition.InitialInitiative,
            sources);
        var equivalent = new CampaignSetupSnapshot(
            definition.SchemaVersion,
            definition.SetupId,
            definition.Hash,
            definition.IsSynthetic,
            definition.InitialGameTurn,
            definition.InitialInitiative,
            sources.ToArray());

        sources.Clear();

        Assert.Equal(definition.Sources, first.Sources);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void SnapshotRoundTripPreservesEveryVersion2FieldAndExcludesDisplayText()
    {
        var created = CreateEvent();
        var snapshot = CampaignProjector.Replay([created]);

        var bytes = CampaignSnapshotSerializer.Serialize(snapshot);
        var json = Encoding.UTF8.GetString(bytes);
        var roundTrip = CampaignSnapshotSerializer.Deserialize(bytes);

        Assert.Equal(snapshot, roundTrip);
        Assert.Contains("\"setup\"", json, StringComparison.Ordinal);
        Assert.Contains("\"randomState\"", json, StringComparison.Ordinal);
        Assert.Contains("\"actorRole\":\"none\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("displayName", json, StringComparison.Ordinal);
        Assert.DoesNotContain(created.Setup.SetupId + ".display", json, StringComparison.Ordinal);
    }

    private static CampaignCreated CreateEvent()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var result = CampaignEngine.Decide(
            null,
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));

        return Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
    }
}
