using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReplayTests
{
    [Fact]
    public void AcceptedCreationReplaysToByteEquivalentCanonicalState()
    {
        var execution = ExecuteCreation(12345);

        var replayed = CampaignTestHarness.Replay(execution.Events);
        var originalBytes = CampaignSnapshotSerializer.Serialize(execution.Snapshot);
        var replayedBytes = CampaignSnapshotSerializer.Serialize(replayed);

        Assert.Equal(originalBytes, replayedBytes);
        Assert.Equal(execution.Snapshot, CampaignSnapshotSerializer.Deserialize(originalBytes));
    }

    [Fact]
    public void SameSeedAndCommandProduceTheSameCreationEvent()
    {
        var first = ExecuteCreation(12345);
        var second = ExecuteCreation(12345);

        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void CreationEventPreservesTheDeclaredRandomState()
    {
        var first = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(12345).Events));
        var second = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(54321).Events));

        Assert.Equal(12345UL, first.RandomState.Seed);
        Assert.Equal(54321UL, second.RandomState.Seed);
        Assert.Equal(first with { RandomState = second.RandomState }, second);
    }

    [Fact]
    public void ReplayRejectsANonCanonicalRulesetHash()
    {
        var valid = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(12345).Events));
        var created = valid with { RulesetHash = "ruleset-hash" };

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([created]));
    }

    [Fact]
    public void StrictContractsRejectANonStableCampaignId()
    {
        var execution = ExecuteCreation(12345);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(execution.Events));
        var invalidCreated = created with { CampaignId = "Invalid ID" };
        var invalidSnapshot = execution.Snapshot with { CampaignId = "Invalid ID" };

        Assert.Throws<JsonException>(() => CampaignEventSerializer.Serialize(invalidCreated));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([invalidCreated]));
        Assert.Throws<JsonException>(() => CampaignSnapshotSerializer.Serialize(invalidSnapshot));
    }

    [Fact]
    public void ReplayRejectsAnInvalidEmbeddedSetupHashWithoutCatalogInterpretation()
    {
        var valid = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(12345).Events));
        var setup = new CampaignSetupSnapshot(
            valid.Setup.SchemaVersion,
            valid.Setup.SetupId,
            "sha256:wrong",
            valid.Setup.IsSynthetic,
            valid.Setup.InitialGameTurn,
            valid.Setup.InitialInitiative,
            valid.Setup.OpeningPreamble,
            valid.Setup.Weather,
            valid.Setup.Content,
            valid.Setup.Sources);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([valid with { Setup = setup }]));
    }

    [Fact]
    public void ReplayRejectsAnEmbeddedSetupWhoseTurnDiffersFromTheExactScenario()
    {
        var valid = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(12345).Events));
        var mismatched = new CampaignSetupDefinition(
            Cna1979SetupCatalog.SchemaVersion,
            "retired.start-mismatch",
            "Retired start-mismatch fixture",
            true,
            valid.Setup.InitialGameTurn + 1,
            valid.Setup.InitialInitiative,
            valid.Setup.OpeningPreamble,
            valid.Setup.Weather,
            valid.Setup.Content,
            valid.Setup.Sources);
        var forged = valid with
        {
            Setup = CampaignSetupSnapshot.FromDefinition(mismatched),
            SequencePosition = Cna1979LandSequence.CreateTurn(mismatched.InitialGameTurn)[0],
        };

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([forged]));
    }

    [Fact]
    public void ReplayRejectsAFieldForgedInitialWorld()
    {
        var valid = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(12345).Events));
        var forgedWorld = new CampaignWorldSnapshot(
            1,
            valid.InitialWorld.Elements
                .Where(element => element.ElementId != "axis-element-a")
                .Append(new CampaignElementState("axis-element-a", "east"))
                .ToArray());

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([valid with { InitialWorld = forgedWorld }]));
    }

    [Fact]
    public void StrictReadersRejectVersion3CreationAndSnapshotContracts()
    {
        var execution = ExecuteCreation(12345);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(execution.Events));
        var eventJson = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(created))
            .Replace("{\"contractVersion\":4,", "{\"contractVersion\":3,", StringComparison.Ordinal);
        var snapshotJson = Encoding.UTF8.GetString(
                CampaignSnapshotSerializer.Serialize(execution.Snapshot))
            .Replace("{\"contractVersion\":4,", "{\"contractVersion\":3,", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(eventJson)));
        Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(snapshotJson)));
    }

    [Fact]
    public void SnapshotDeserializerRejectsAPositionOutsideTheRulesetCatalog()
    {
        var execution = ExecuteCreation(12345);
        var canonicalJson = Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot));
        var invalidJson = canonicalJson.Replace(
            execution.Snapshot.SequencePosition.PositionId,
            "land.position.invalid",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(invalidJson)));
    }

    [Fact]
    public void SnapshotDeserializerRejectsExtraOrReorderedProperties()
    {
        var execution = ExecuteCreation(12345);
        var canonicalJson = Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot));
        var extra = canonicalJson.Replace(
            "{\"contractVersion\":4,",
            "{\"extra\":true,\"contractVersion\":4,",
            StringComparison.Ordinal);
        var reordered = canonicalJson.Replace(
            "{\"contractVersion\":4,\"campaignId\":\"campaign-1\",",
            "{\"campaignId\":\"campaign-1\",\"contractVersion\":4,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(extra)));
        Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(reordered)));
    }

    [Fact]
    public void SnapshotDeserializerNormalizesNonIntegerMetadataToJsonException()
    {
        var execution = ExecuteCreation(12345);
        var canonicalJson = Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot));
        var malformed = canonicalJson.Replace(
            "\"stateVersion\":1,",
            "\"stateVersion\":1.5,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(malformed)));
    }

    [Fact]
    public void CreationSnapshotUsesTheExactCanonicalVersion4Shape()
    {
        var execution = ExecuteCreation(12345);
        var actual = Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot));
        var expected = "{\"contractVersion\":4,\"campaignId\":\"campaign-1\"," +
            "\"stateVersion\":1,\"rulesetHash\":\"" +
            Cna1979Ruleset.Manifest.Hash +
            "\",\"setup\":{\"schemaVersion\":4," +
            "\"setupId\":\"rules-lab.initiative.predetermined\"," +
            "\"setupHash\":\"sha256:5ecf84d21a7ff95112b9b662915f6858926532d30be5a0eee3f1a45752fdc80a\"," +
            "\"isSynthetic\":true,\"initialGameTurn\":1," +
            "\"initialInitiative\":{\"kind\":\"predetermined\",\"holder\":\"axis\"}," +
            "\"openingPreamble\":{\"contractVersion\":1," +
            "\"kind\":\"no-opening-naval-convoy-obligations\"," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"opening-preamble.no-naval-convoy-obligations.v1\"}]}," +
            "\"weather\":{\"contractVersion\":1," +
            "\"kind\":\"no-immediate-weather-effect-subjects\"," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"weather.no-immediate-effect-subjects.v1\"}]}," +
            "\"content\":{\"schemaVersion\":2,\"formatId\":\"sandtable.content-json.v1\"," +
            "\"packId\":\"rules-lab.content.movement-contact.v1\",\"rulesetId\":\"cna-1979.1\"," +
            "\"hash\":\"sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99\"," +
            "\"scenarioId\":\"movement-contact-lab\"}," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"initiative.predetermined-axis.v1\"}]}," +
            "\"world\":{" +
            "\"contractVersion\":1,\"elements\":[" +
            "{\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"}," +
            "{\"elementId\":\"axis-element-b\",\"currentLocationId\":\"north-west\"}," +
            "{\"elementId\":\"commonwealth-element-a\",\"currentLocationId\":\"east\"}," +
            "{\"elementId\":\"commonwealth-element-b\",\"currentLocationId\":\"south-east\"}]}," +
            "\"initiativeHolder\":null,\"operationStageOrders\":[]," +
            "\"randomState\":{\"contractVersion\":1," +
            "\"algorithmId\":\"sandtable.sha256-counter.v1\",\"seed\":12345," +
            "\"nextByteCursor\":0},\"sequencePosition\":{\"contractVersion\":2," +
            "\"positionId\":\"land.position.initiative-determination\"," +
            "\"gameTurn\":1,\"operationStage\":0," +
            "\"stageId\":\"land.stage.initiative-determination\"," +
            "\"phaseId\":\"land.phase.initiative-determination\"," +
            "\"segmentId\":null,\"stepId\":null,\"actorRole\":\"none\"," +
            "\"activeSide\":null,\"sources\":[{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"5.2\"}]}}";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HarnessStopsAtTheFirstMandatoryUnimplementedMechanic()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash),
            new CompleteCurrentSequenceStep(1, "land.position.initiative-determination"),
        ];

        var result = CampaignTestHarness.Execute(commands);

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition, result.RejectionReason);
        Assert.Equal(1, result.RejectedCommandIndex);
        Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.NotNull(result.Snapshot);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, result.Snapshot.PhaseId);
        Assert.Equal(1, result.Snapshot.StateVersion);
    }

    private static CampaignExecution ExecuteCreation(ulong seed)
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                setup.SetupId,
                setup.Hash),
        ];
        var result = CampaignTestHarness.Execute(commands);
        Assert.True(result.IsAccepted);
        return new CampaignExecution(result.Events, Assert.IsType<CampaignSnapshot>(result.Snapshot));
    }

    private sealed record CampaignExecution(
        IReadOnlyList<CampaignEvent> Events,
        CampaignSnapshot Snapshot);
}
