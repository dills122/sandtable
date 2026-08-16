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

        var replayed = CampaignProjector.Replay(execution.Events);
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
            CampaignProjector.Replay([created]));
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
            valid.Setup.Sources);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignProjector.Replay([valid with { Setup = setup }]));
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
            "{\"contractVersion\":2,",
            "{\"extra\":true,\"contractVersion\":2,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            CampaignSnapshotSerializer.Deserialize(Encoding.UTF8.GetBytes(extra)));
    }

    [Fact]
    public void CreationSnapshotUsesTheExactCanonicalVersion2Shape()
    {
        var execution = ExecuteCreation(12345);
        var actual = Encoding.UTF8.GetString(
            CampaignSnapshotSerializer.Serialize(execution.Snapshot));
        var expected = "{\"contractVersion\":2,\"campaignId\":\"campaign-1\"," +
            "\"stateVersion\":1,\"rulesetHash\":\"" +
            Cna1979Ruleset.Manifest.Hash +
            "\",\"setup\":{\"schemaVersion\":1," +
            "\"setupId\":\"rules-lab.initiative.predetermined\"," +
            "\"setupHash\":\"sha256:ef7dd9cf4cf78616f5b8e2c95408c7fbf03eae46c934238be541565390e2520f\"," +
            "\"isSynthetic\":true,\"initialGameTurn\":1," +
            "\"initialInitiative\":{\"kind\":\"predetermined\",\"holder\":\"axis\"}," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"initiative.predetermined-axis.v1\"}]}," +
            "\"initiativeHolder\":null,\"randomState\":{\"contractVersion\":1," +
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
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash),
            new CompleteCurrentSequenceStep(1, "land.position.initiative-determination"),
        ];

        var result = CampaignReplayHarness.Execute(commands);

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
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                setup.SetupId,
                setup.Hash),
        ];
        var result = CampaignReplayHarness.Execute(commands);
        Assert.True(result.IsAccepted);
        return new CampaignExecution(result.Events, Assert.IsType<CampaignSnapshot>(result.Snapshot));
    }

    private sealed record CampaignExecution(
        IReadOnlyList<CampaignEvent> Events,
        CampaignSnapshot Snapshot);
}
