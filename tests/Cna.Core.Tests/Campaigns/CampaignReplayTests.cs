using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

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
    public void CreationEventPreservesTheDeclaredSeed()
    {
        var first = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(12345).Events));
        var second = Assert.IsType<CampaignCreated>(Assert.Single(ExecuteCreation(54321).Events));

        Assert.Equal(12345UL, first.Seed);
        Assert.Equal(54321UL, second.Seed);
        Assert.Equal(first with { Seed = second.Seed }, second);
    }

    [Fact]
    public void ReplayRejectsANonContiguousEventVersion()
    {
        var execution = ExecuteCreation(12345);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(execution.Events));
        var nextPosition = Cna1979LandSequence.GetNext(
            created.SequencePosition,
            created.FirstPlayer);
        CampaignEvent[] history =
        [
            created,
            new CampaignSequenceAdvanced(
                created.CampaignId,
                3,
                created.SequencePosition.PositionId,
                nextPosition),
        ];

        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignProjector.Replay(history));
    }

    [Fact]
    public void ReplayReportsAnInvalidFirstPlayerAsInvalidHistory()
    {
        var validPosition = Cna1979LandSequence.CreateTurn(1, LandSide.Axis)[0];
        var created = new CampaignCreated(
            "campaign-1",
            1,
            Cna1979Ruleset.Manifest.Hash,
            12345,
            (LandSide)999,
            validPosition);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignProjector.Replay([created]));
    }

    [Fact]
    public void ReplayRejectsANonCanonicalRulesetHash()
    {
        var initialPosition = Cna1979LandSequence.CreateTurn(1, LandSide.Axis)[0];
        var created = new CampaignCreated(
            "campaign-1",
            1,
            "ruleset-hash",
            12345,
            LandSide.Axis,
            initialPosition);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignProjector.Replay([created]));
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
    public void SnapshotSerializationPreservesEveryPositionSourceReference()
    {
        var position = Cna1979LandSequence
            .CreateTurn(1, LandSide.Axis)
            .First(value => value.PositionId.Contains(".first-player.", StringComparison.Ordinal));
        var snapshot = new CampaignSnapshot(
            1,
            "campaign-1",
            2,
            Cna1979Ruleset.Manifest.Hash,
            12345,
            LandSide.Axis,
            position);

        var serialized = CampaignSnapshotSerializer.Serialize(snapshot);
        var deserialized = CampaignSnapshotSerializer.Deserialize(serialized);

        Assert.Equal(position.Sources, deserialized.SequencePosition.Sources);
        Assert.Equal(snapshot, deserialized);
    }

    [Fact]
    public void HarnessStopsAtTheFirstMandatoryUnimplementedMechanic()
    {
        CampaignCommand[] commands =
        [
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                LandSide.Axis),
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
        CampaignCommand[] commands =
        [
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                LandSide.Axis),
        ];
        var result = CampaignReplayHarness.Execute(commands);
        Assert.True(result.IsAccepted);
        return new CampaignExecution(result.Events, Assert.IsType<CampaignSnapshot>(result.Snapshot));
    }

    private sealed record CampaignExecution(
        IReadOnlyList<CampaignEvent> Events,
        CampaignSnapshot Snapshot);
}
