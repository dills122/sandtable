using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReplayTests
{
    [Fact]
    public void ReplayProducesByteEquivalentCanonicalState()
    {
        var execution = ExecuteToFirstPlayerMovement(12345);

        var replayed = CampaignProjector.Replay(execution.Events);
        var originalBytes = CampaignSnapshotSerializer.Serialize(execution.Snapshot);
        var replayedBytes = CampaignSnapshotSerializer.Serialize(replayed);

        Assert.Equal(originalBytes, replayedBytes);
        Assert.Equal(execution.Snapshot, CampaignSnapshotSerializer.Deserialize(originalBytes));
    }

    [Fact]
    public void SameSeedAndCommandsProduceTheSameEventSequence()
    {
        var first = ExecuteToFirstPlayerMovement(12345);
        var second = ExecuteToFirstPlayerMovement(12345);

        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void SeedDoesNotChangeNonRandomSequenceTransitions()
    {
        var first = ExecuteToFirstPlayerMovement(12345);
        var second = ExecuteToFirstPlayerMovement(54321);

        Assert.NotEqual(first.Events[0], second.Events[0]);
        Assert.Equal(first.Events.Skip(1), second.Events.Skip(1));
    }

    [Fact]
    public void ReplayRejectsANonContiguousEventVersion()
    {
        var execution = ExecuteToFirstPlayerMovement(12345);
        var history = execution.Events.ToArray();
        var advanced = Assert.IsType<CampaignSequenceAdvanced>(history[1]);
        history[1] = advanced with { StateVersion = 3 };

        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignProjector.Replay(history));
    }

    [Fact]
    public void ReplayReportsAnInvalidFirstPlayerAsInvalidHistory()
    {
        var validPosition = Cna1979LandSequence.CreateTurn(1, LandSide.Axis)[0];
        var created = new CampaignCreated(
            "campaign-1",
            1,
            "ruleset-hash",
            12345,
            (LandSide)999,
            validPosition);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignProjector.Replay([created]));
    }

    [Fact]
    public void SnapshotDeserializerRejectsAPositionOutsideTheRulesetCatalog()
    {
        var execution = ExecuteToFirstPlayerMovement(12345);
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
    public void HarnessExecutesACommandStreamAndExposesItsChronicle()
    {
        var commands = CreateCommandsToFirstPlayerMovement(12345);

        var result = CampaignReplayHarness.Execute(commands);

        Assert.True(result.IsAccepted);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(commands.Count, result.Events.Count);
        Assert.Equal(LandSegmentIds.Movement, result.Snapshot.SegmentId);
        Assert.Equal(
            Enumerable.Range(1, result.Events.Count).Select(value => (long)value),
            result.Events.Select(campaignEvent => campaignEvent.StateVersion));
    }

    private static CampaignExecution ExecuteToFirstPlayerMovement(ulong seed)
    {
        var result = CampaignReplayHarness.Execute(CreateCommandsToFirstPlayerMovement(seed));
        Assert.True(result.IsAccepted);
        return new CampaignExecution(result.Events, Assert.IsType<CampaignSnapshot>(result.Snapshot));
    }

    private static List<CampaignCommand> CreateCommandsToFirstPlayerMovement(ulong seed)
    {
        var positions = Cna1979LandSequence.CreateTurn(1, LandSide.Axis);
        var movementIndex = positions
            .Select((position, index) => (position, index))
            .First(candidate =>
                candidate.position.OperationStage == 1
                && candidate.position.ActiveSide == LandSide.Axis
                && candidate.position.SegmentId == LandSegmentIds.Movement)
            .index;
        var commands = new List<CampaignCommand>
        {
            new CreateCampaign("campaign-1", "ruleset-hash", seed, LandSide.Axis),
        };

        for (var index = 0; index < movementIndex; index++)
        {
            commands.Add(new CompleteCurrentSequenceStep(
                index + 1,
                positions[index].PositionId));
        }

        return commands;
    }

    private sealed record CampaignExecution(
        IReadOnlyList<CampaignEvent> Events,
        CampaignSnapshot Snapshot);
}
