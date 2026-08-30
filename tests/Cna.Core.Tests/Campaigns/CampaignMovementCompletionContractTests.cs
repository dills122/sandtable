using System.Reflection;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementCompletionContractTests
{
    [Fact]
    public void CompletionCommandAndEventFreezeTheExactAuthorityShape()
    {
        AssertCommandShape();

        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var command = new CompleteMovementSegment(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId,
            evidence.ActingSide);
        var completed = CampaignMovementEventFactory.CreateCompletion(
            snapshot,
            evidence.Context,
            command);

        Assert.Equal(1, command.ContractVersion);
        Assert.Equal(snapshot.StateVersion, command.ExpectedStateVersion);
        Assert.Equal(snapshot.SequencePosition.PositionId, command.ExpectedPositionId);
        Assert.Equal(evidence.ActingSide, command.ActingSide);
        Assert.Equal(1, completed.ContractVersion);
        Assert.Equal(snapshot.CampaignId, completed.CampaignId);
        Assert.Equal(snapshot.StateVersion + 1, completed.StateVersion);
        Assert.Equal(snapshot.StateVersion, completed.PriorStateVersion);
        Assert.Equal(snapshot.SequencePosition.PositionId, completed.FromPositionId);
        Assert.Equal(snapshot.GameTurn, completed.GameTurn);
        Assert.Equal(snapshot.OperationStage, completed.OperationStage);
        Assert.Equal(evidence.ActingSide, completed.ActingSide);
        Assert.Equal(Cna1979LandSequence.GetNext(snapshot.SequencePosition),
            completed.SequencePosition);
        Assert.Equal(LandSegmentIds.BreakdownDetermination,
            completed.SequencePosition.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide,
            completed.SequencePosition.ActorRole);
        Assert.Null(completed.SequencePosition.ActiveSide);
    }

    [Fact]
    public void CompletionEventHasCanonicalStrictReadback()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var completed = CampaignMovementEventFactory.CreateCompletion(
            snapshot,
            evidence.Context,
            new CompleteMovementSegment(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                evidence.ActingSide));

        var canonical = CampaignEventSerializer.Serialize(completed);
        var read = Assert.IsType<MovementSegmentCompleted>(
            CampaignEventSerializer.Deserialize(canonical));

        Assert.Equal(completed, read);
        Assert.Equal(canonical, CampaignEventSerializer.Serialize(read));

        var json = Encoding.UTF8.GetString(canonical);
        var expected =
            "{\"contractVersion\":1,\"eventType\":\"movement-segment-completed\"," +
            $"\"campaignId\":\"{snapshot.CampaignId}\",\"stateVersion\":12," +
            "\"priorStateVersion\":11," +
            $"\"fromPositionId\":\"{snapshot.SequencePosition.PositionId}\"," +
            $"\"gameTurn\":{snapshot.GameTurn},\"operationStage\":1," +
            $"\"actingSide\":\"{FormatSide(evidence.ActingSide)}\"," +
            $"\"sequencePosition\":{PositionJson(completed.SequencePosition)}}}";
        Assert.Equal(expected, json);

        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(
                json.Replace(
                    "\"contractVersion\":1",
                    "\"contractVersion\":2",
                    StringComparison.Ordinal))));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(
                json.Replace(
                    "\"priorStateVersion\":11,",
                    string.Empty,
                    StringComparison.Ordinal))));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(
                json.Replace(
                    "\"priorStateVersion\":11",
                    "\"priorStateVersion\":10",
                    StringComparison.Ordinal))));
    }

    [Fact]
    public void CompletionEventStrictReaderRejectsTheFullContractMutationMatrix()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var completed = CampaignMovementEventFactory.CreateCompletion(
            snapshot,
            evidence.Context,
            new CompleteMovementSegment(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                evidence.ActingSide));
        var canonical = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(completed));
        var side = FormatSide(evidence.ActingSide);

        string[] mutations =
        [
            canonical.Replace(
                "\"campaignId\":",
                "\"hiddenAuthority\":true,\"campaignId\":",
                StringComparison.Ordinal),
            canonical.Replace(
                "\"campaignId\":",
                "\"campaignId\":\"duplicate\",\"campaignId\":",
                StringComparison.Ordinal),
            canonical.Replace(
                "\"stateVersion\":12,\"priorStateVersion\":11,",
                "\"priorStateVersion\":11,\"stateVersion\":12,",
                StringComparison.Ordinal),
            canonical.Replace(
                $"\"actingSide\":\"{side}\",",
                string.Empty,
                StringComparison.Ordinal),
            canonical.Replace(
                $"\"actingSide\":\"{side}\"",
                "\"actingSide\":\"unsupported-side\"",
                StringComparison.Ordinal),
            canonical.Replace(
                $"\"fromPositionId\":\"{snapshot.SequencePosition.PositionId}\"",
                "\"fromPositionId\":\"bad id\"",
                StringComparison.Ordinal),
            canonical.Replace(
                $"\"gameTurn\":{snapshot.GameTurn}",
                "\"gameTurn\":0",
                StringComparison.Ordinal),
            canonical.Replace(
                "\"operationStage\":1",
                "\"operationStage\":2",
                StringComparison.Ordinal),
            canonical.Replace(
                "\"segmentId\":\"land.segment.breakdown-determination\"",
                "\"segmentId\":\"land.segment.movement\"",
                StringComparison.Ordinal),
            canonical.Replace(
                "\"actorRole\":\"first-acting-side\"",
                "\"actorRole\":\"none\"",
                StringComparison.Ordinal),
        ];

        Assert.All(mutations, mutation => Assert.Throws<JsonException>(() =>
            CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(mutation))));
    }

    private static void AssertCommandShape()
    {
        var commandType = typeof(CompleteMovementSegment);
        Assert.True(commandType.IsNotPublic);
        Assert.True(commandType.IsSealed);
        Assert.True(commandType.IsAssignableTo(typeof(CampaignCommand)));
        var constructor = Assert.Single(commandType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            candidate => candidate.GetParameters().Length == 3);
        Assert.Equal(
            ["ExpectedStateVersion", "ExpectedPositionId", "ActingSide"],
            constructor.GetParameters().Select(parameter => parameter.Name));
        Assert.Equal(
            [typeof(long), typeof(string), typeof(LandSide)],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            ["ExpectedPositionId", "ActingSide"],
            commandType.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(property => property.Name));
    }

    private static string PositionJson(LandSequencePosition position)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignSnapshotSerializer.WritePosition(writer, position);
            writer.WriteEndObject();
        }

        const string prefix = "{\"sequencePosition\":";
        var envelope = Encoding.UTF8.GetString(stream.ToArray());
        return envelope[prefix.Length..^1];
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
