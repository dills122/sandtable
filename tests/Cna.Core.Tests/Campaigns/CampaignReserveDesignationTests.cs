using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Actions;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReserveDesignationTests
{
    [Fact]
    public void CurrentDesignationSubmissionCommitsOneExactEventAndReplays()
    {
        var evidence = ReachReserve();
        var handle = new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
        var actingSide = FirstActingSideResolver.Resolve(evidence.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(actingSide);
        var set = CampaignReserveActionTestData.Query(handle, audience);
        var candidate = set.Candidates.OfType<DesignateReserveAction>().First();
        var submission = CampaignReserveActionTestData.Bind(set, candidate);
        var before = CampaignSnapshotSerializer.Serialize(evidence.Snapshot);

        var execution = CampaignActionExecution.Execute(
            evidence.Snapshot,
            evidence.Context,
            submission);

        Assert.True(execution.IsAccepted);
        var designated = Assert.IsType<ReserveElementDesignated>(execution.AcceptedEvent);
        var successor = execution.SuccessorSnapshot!;
        Assert.Equal(evidence.Snapshot.CampaignId, designated.CampaignId);
        Assert.Equal(11, designated.StateVersion);
        Assert.Equal(evidence.Snapshot.SequencePosition.PositionId, designated.FromPositionId);
        Assert.Equal((1, 1, actingSide),
            (designated.GameTurn, designated.OperationStage, designated.ActingSide));
        Assert.Equal(candidate.ElementId, designated.ElementId);
        Assert.Equal(CampaignElementReserveStatus.None, designated.PriorStatus);
        Assert.Equal(CampaignElementReserveStatus.ReserveI, designated.ResultingStatus);
        Assert.Equal(evidence.Snapshot.SequencePosition, designated.SequencePosition);
        Assert.Equal(ReserveElementDesignated.RequiredSources, designated.Sources);
        Assert.Equal(11, successor.StateVersion);
        Assert.Equal(evidence.Snapshot.SequencePosition, successor.SequencePosition);
        Assert.Equal(CampaignElementReserveStatus.ReserveI,
            successor.World.Elements.Single(value =>
                value.ElementId == candidate.ElementId).ReserveStatus);
        Assert.All(successor.World.Elements.Where(value =>
                value.ElementId != candidate.ElementId),
            value => Assert.Equal(CampaignElementReserveStatus.None, value.ReserveStatus));
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(evidence.Snapshot));

        var canonicalEvent = CampaignEventSerializer.Serialize(designated);
        Assert.Equal(canonicalEvent, CampaignEventSerializer.Serialize(
            CampaignEventSerializer.Deserialize(canonicalEvent)));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(successor),
            CampaignSnapshotSerializer.Serialize(CampaignProjector.Replay(
                evidence.Events.Append(designated),
                evidence.Context)));

        var receipt = Assert.IsType<CampaignActionAcceptanceReceipt>(execution.Receipt);
        Assert.Equal(evidence.Snapshot.StateVersion, receipt.PriorStateVersion);
        Assert.Equal(successor.StateVersion, receipt.CommittedStateVersion);
        var nextSet = CampaignReserveActionTestData.Query(
            new CampaignAuthorityHandle(successor, evidence.Context),
            audience);
        Assert.DoesNotContain(nextSet.Candidates.OfType<DesignateReserveAction>(),
            value => value.ElementId == candidate.ElementId);
        Assert.Single(nextSet.Candidates.OfType<CompleteReserveDesignationAction>());
    }

    [Fact]
    public void BothReserveEventsHaveExactCanonicalClosedContracts()
    {
        var evidence = ReachReserve();
        var reserve = evidence.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(reserve);
        var side = FormatSide(actingSide);
        var elementId = OwnElementIds(evidence, actingSide)[0];
        var designation = CampaignReserveEventFactory.CreateDesignation(
            reserve,
            evidence.Context,
            new DesignateReserveElement(
                reserve.StateVersion,
                reserve.SequencePosition.PositionId,
                actingSide,
                elementId));
        var completion = CampaignReserveEventFactory.CreateCompletion(
            reserve,
            evidence.Context,
            new CompleteReserveDesignation(
                reserve.StateVersion,
                reserve.SequencePosition.PositionId,
                actingSide));
        var reservePosition = PositionJson(reserve.SequencePosition);
        var movementPosition = PositionJson(completion.SequencePosition);
        var designationExpected =
            $"{{\"contractVersion\":1,\"eventType\":\"reserve-element-designated\"," +
            $"\"campaignId\":\"{reserve.CampaignId}\",\"stateVersion\":11," +
            $"\"fromPositionId\":\"{reserve.SequencePosition.PositionId}\"," +
            $"\"gameTurn\":1,\"operationStage\":1,\"actingSide\":\"{side}\"," +
            $"\"elementId\":\"{elementId}\",\"priorStatus\":\"none\"," +
            $"\"resultingStatus\":\"reserve-i\",\"sequencePosition\":{reservePosition}," +
            "\"sources\":[{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"18.11\"}," +
            "{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"18.12\"}," +
            "{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"18.15\"}]}";
        var completionExpected =
            $"{{\"contractVersion\":1,\"eventType\":\"reserve-designation-completed\"," +
            $"\"campaignId\":\"{reserve.CampaignId}\",\"stateVersion\":11," +
            $"\"fromPositionId\":\"{reserve.SequencePosition.PositionId}\"," +
            $"\"gameTurn\":1,\"operationStage\":1,\"actingSide\":\"{side}\"," +
            $"\"sequencePosition\":{movementPosition},\"sources\":[" +
            "{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"18.11\"}," +
            "{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"5.2.reserve-designation\"}]}";

        Assert.Equal(designationExpected, Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(designation)));
        Assert.Equal(completionExpected, Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(completion)));
        Assert.Equal(
            CampaignEventSerializer.Serialize(designation),
            CampaignEventSerializer.Serialize(CampaignEventSerializer.Deserialize(
                CampaignEventSerializer.Serialize(designation))));
        Assert.Equal(
            CampaignEventSerializer.Serialize(completion),
            CampaignEventSerializer.Serialize(CampaignEventSerializer.Deserialize(
                CampaignEventSerializer.Serialize(completion))));

        string[] noncanonicalCompletion =
        [
            completionExpected.Replace(
                "{\"contractVersion\":1,\"eventType\":\"reserve-designation-completed\",",
                "{\"eventType\":\"reserve-designation-completed\",\"contractVersion\":1,",
                StringComparison.Ordinal),
            completionExpected.Replace("\"campaignId\":",
                "\"extra\":true,\"campaignId\":", StringComparison.Ordinal),
            completionExpected.Replace("\"locator\":\"5.2.reserve-designation\"",
                "\"locator\":\"5.2.reserve-release\"", StringComparison.Ordinal),
        ];
        foreach (var value in noncanonicalCompletion)
        {
            Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
                Encoding.UTF8.GetBytes(value)));
        }
    }

    [Fact]
    public void CompletionContractAndCommandEmitTheExactMovementEvent()
    {
        var evidence = ReachReserve();
        var reserve = evidence.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(reserve);
        var command = new CompleteReserveDesignation(
            reserve.StateVersion,
            reserve.SequencePosition.PositionId,
            actingSide);

        var completion = CampaignReserveEventFactory.CreateCompletion(
            reserve,
            evidence.Context,
            command);
        var decision = CampaignEngine.Decide(reserve, command, evidence.Context);

        Assert.Equal(11, completion.StateVersion);
        Assert.Equal(LandPhaseIds.MovementAndCombat, completion.SequencePosition.PhaseId);
        Assert.Equal(LandSegmentIds.Movement, completion.SequencePosition.SegmentId);
        Assert.Equal(ReserveDesignationCompleted.RequiredSources, completion.Sources);
        Assert.True(decision.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.None,
            decision.RejectionReason);
        var actual = Assert.IsType<ReserveDesignationCompleted>(
            Assert.Single(decision.Events));
        Assert.Equal(
            CampaignEventSerializer.Serialize(completion),
            CampaignEventSerializer.Serialize(actual));
    }

    [Fact]
    public void InvalidDesignationCommandsRejectWithZeroEvents()
    {
        var evidence = ReachReserve();
        var reserve = evidence.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(reserve);
        var otherSide = actingSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var ownElementId = OwnElementIds(evidence, actingSide)[0];
        var otherElementId = OwnElementIds(evidence, otherSide)[0];
        var prior = CampaignProjector.Replay(
            evidence.Events.Take(evidence.Events.Count - 1),
            evidence.Context);
        (CampaignSnapshot Snapshot, DesignateReserveElement Command,
            CampaignCommandRejectionReason Reason)[] cases =
        [
            (reserve, new DesignateReserveElement(reserve.StateVersion,
                reserve.SequencePosition.PositionId, actingSide, ownElementId)
                with { ContractVersion = 2 },
                CampaignCommandRejectionReason.InvalidCommand),
            (reserve, new DesignateReserveElement(reserve.StateVersion - 1,
                reserve.SequencePosition.PositionId, actingSide, ownElementId),
                CampaignCommandRejectionReason.StaleState),
            (reserve, new DesignateReserveElement(reserve.StateVersion,
                prior.SequencePosition.PositionId, actingSide, ownElementId),
                CampaignCommandRejectionReason.UnexpectedSequenceStep),
            (reserve, new DesignateReserveElement(reserve.StateVersion,
                reserve.SequencePosition.PositionId, (LandSide)99, ownElementId),
                CampaignCommandRejectionReason.InvalidCommand),
            (reserve, new DesignateReserveElement(reserve.StateVersion,
                reserve.SequencePosition.PositionId, otherSide, ownElementId),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (reserve, new DesignateReserveElement(reserve.StateVersion,
                reserve.SequencePosition.PositionId, actingSide, otherElementId),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (reserve, new DesignateReserveElement(reserve.StateVersion,
                reserve.SequencePosition.PositionId, actingSide, "missing-element"),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (prior, new DesignateReserveElement(prior.StateVersion,
                prior.SequencePosition.PositionId, actingSide, ownElementId),
                CampaignCommandRejectionReason.UnsupportedTransition),
        ];
        var before = CampaignSnapshotSerializer.Serialize(reserve);

        foreach (var (snapshot, command, reason) in cases)
        {
            var decision = CampaignEngine.Decide(snapshot, command, evidence.Context);

            Assert.False(decision.IsAccepted);
            Assert.Equal(reason, decision.RejectionReason);
            Assert.Empty(decision.Events);
        }

        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(reserve));

        var valid = Assert.IsType<ReserveElementDesignated>(Assert.Single(
            CampaignEngine.Decide(
                reserve,
                new DesignateReserveElement(reserve.StateVersion,
                    reserve.SequencePosition.PositionId, actingSide, ownElementId),
                evidence.Context).Events));
        var successor = CampaignProjector.Apply(reserve, valid, evidence.Context);
        var repeated = CampaignEngine.Decide(
            successor,
            new DesignateReserveElement(successor.StateVersion,
                successor.SequencePosition.PositionId, actingSide, ownElementId),
            evidence.Context);
        Assert.False(repeated.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition,
            repeated.RejectionReason);
        Assert.Empty(repeated.Events);
    }

    [Fact]
    public void ReaderAndProjectorRejectNoncanonicalOrForgedDesignationHistory()
    {
        var evidence = ReachReserve();
        var reserve = evidence.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(reserve);
        var ownElementId = OwnElementIds(evidence, actingSide)[0];
        var otherSide = actingSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var otherElementId = OwnElementIds(evidence, otherSide)[0];
        var valid = CampaignReserveEventFactory.CreateDesignation(
            reserve,
            evidence.Context,
            new DesignateReserveElement(reserve.StateVersion,
                reserve.SequencePosition.PositionId, actingSide, ownElementId));
        var canonical = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(valid));
        string[] malformed =
        [
            canonical.Replace("\"contractVersion\":1",
                "\"contractVersion\":2", StringComparison.Ordinal),
            canonical.Replace($"\"campaignId\":\"{reserve.CampaignId}\"",
                "\"campaignId\":\"\"", StringComparison.Ordinal),
            canonical.Replace("\"stateVersion\":11",
                "\"stateVersion\":10", StringComparison.Ordinal),
            canonical.Replace("{\"contractVersion\":1,\"eventType\":",
                "{\"eventType\":\"ignored\",\"contractVersion\":1,\"eventType\":",
                StringComparison.Ordinal),
            canonical.Replace("\"campaignId\":", "\"extra\":true,\"campaignId\":",
                StringComparison.Ordinal),
            canonical.Replace("\"priorStatus\":\"none\"",
                "\"priorStatus\":\"reserve-i\"", StringComparison.Ordinal),
            canonical.Replace("\"resultingStatus\":\"reserve-i\"",
                "\"resultingStatus\":\"reserve-ii\"", StringComparison.Ordinal),
            canonical.Replace("\"locator\":\"18.12\"",
                "\"locator\":\"18.13\"", StringComparison.Ordinal),
        ];

        foreach (var value in malformed)
        {
            Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
                Encoding.UTF8.GetBytes(value)));
        }

        string[] contextForged =
        [
            canonical.Replace($"\"elementId\":\"{ownElementId}\"",
                $"\"elementId\":\"{otherElementId}\"", StringComparison.Ordinal),
            canonical.Replace($"\"actingSide\":\"{FormatSide(actingSide)}\"",
                $"\"actingSide\":\"{FormatSide(otherSide)}\"", StringComparison.Ordinal),
        ];
        foreach (var value in contextForged)
        {
            var forged = CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(value));
            Assert.Throws<InvalidCampaignHistoryException>(() =>
            {
                _ = CampaignProjector.Apply(reserve, forged, evidence.Context);
            });
        }

        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(reserve,
                valid with { CampaignId = "campaign-forged" }, evidence.Context);
        });
        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(reserve,
                valid with { StateVersion = valid.StateVersion + 1 }, evidence.Context);
        });
        var successor = CampaignProjector.Apply(reserve, valid, evidence.Context);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(successor, valid, evidence.Context);
        });
    }

    private static StageEntryCampaignEvidence ReachReserve() =>
        StageEntryCampaignTestData.Execute(
            Cna1979SetupCatalog.Definitions[0],
            12345,
            InitiativeOrderChoice.ActFirst);

    private static string[] OwnElementIds(
        StageEntryCampaignEvidence evidence,
        LandSide side)
    {
        var sideId = FormatSide(side);
        return evidence.Context.Artifact.Definition.Elements
            .Where(value => string.Equals(value.SideId, sideId, StringComparison.Ordinal))
            .Select(value => value.ElementId)
            .Order(StringComparer.Ordinal)
            .ToArray();
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
        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.GetProperty("sequencePosition").GetRawText();
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
