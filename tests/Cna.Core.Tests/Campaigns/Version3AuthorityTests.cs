using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class Version3AuthorityTests
{
    [Fact]
    public void CreationBindsTheRecognizedSetupAndEmbedsReplayCompleteState()
    {
        var setup = Cna1979SetupCatalog.Definitions[1];
        var command = CampaignTestHarness.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.Hash);

        var result = CampaignTestHarness.Decide(null, command);

        Assert.True(result.IsAccepted);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.Equal(4, created.ContractVersion);
        Assert.Equal(setup.Content, created.Setup.Content);
        Assert.Equal(4, created.InitialWorld.Elements.Count);
        Assert.Equal(setup.SetupId, created.Setup.SetupId);
        Assert.Equal(setup.Hash, created.Setup.SetupHash);
        Assert.Equal(setup.InitialInitiative, created.Setup.InitialInitiative);
        Assert.Equal(setup.Weather, created.Setup.Weather);
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
        var result = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
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
        Assert.Equal(3, Cna1979Ruleset.Manifest.ContractVersion);
        Assert.Equal(
            [
                "cna-1979.1.content-vocabulary",
                "cna-1979.1.initiative-ratings",
                "cna-1979.1.land-sequence",
                "cna-1979.1.random-procedure",
                "cna-1979.1.weather-tables",
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

        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignTestHarness.Replay(history));
    }

    [Fact]
    public void ReplayRejectsAnEmbeddedSetupOutsideTheAdmittedCatalogScope()
    {
        var definition = new CampaignSetupDefinition(
            Cna1979SetupCatalog.SchemaVersion,
            "retired.synthetic.setup",
            "Catalog-only display text",
            true,
            1,
            new PredeterminedInitiative(LandSide.Commonwealth),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.WeatherPolicy,
            Cna1979SetupCatalog.Definitions[0].Content,
            [new RuleReference("sandtable-rules-lab", "retired.synthetic.v1")]);
        var created = new CampaignCreated(
            "campaign-retired",
            1,
            Cna1979Ruleset.Manifest.Hash,
            CampaignSetupSnapshot.FromDefinition(definition),
            CampaignWorldFactory.CreateInitial(
                Cna1979SyntheticContentCatalog.Artifact,
                Cna1979SyntheticContentCatalog.Artifact.Definition.Scenarios.Single(
                    scenario => scenario.ScenarioId == "movement-contact-lab")),
            SandtableRandom.Create(7),
            Cna1979LandSequence.CreateTurn(1)[0]);

        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignTestHarness.Replay([created]));
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
            definition.OpeningPreamble,
            definition.Weather,
            definition.Content,
            sources);
        var equivalent = new CampaignSetupSnapshot(
            definition.SchemaVersion,
            definition.SetupId,
            definition.Hash,
            definition.IsSynthetic,
            definition.InitialGameTurn,
            definition.InitialInitiative,
            definition.OpeningPreamble,
            definition.Weather,
            definition.Content,
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
        var snapshot = CampaignTestHarness.Replay([created]);

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
        var result = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));

        return Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
    }
}
