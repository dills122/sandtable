using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignElementMovedV2ReplayTests
{
    [Fact]
    public void EventRoundTripsAndProjectsAtomicMoveAndWindowTruth()
    {
        var fixture = CampaignV10TestData.Create();
        var eventBytes = CampaignSuccessorEventSerializer.Serialize(fixture.TriggeringMove);
        var roundTripped = Assert.IsType<ElementMovedV2>(
            CampaignSuccessorEventSerializer.Deserialize(eventBytes));

        var projected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            roundTripped,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);

        Assert.Equal(2, roundTripped.ContractVersion);
        Assert.Equal(12, projected.StateVersion);
        Assert.Equal("east", projected.World.Elements.Single(element =>
            element.ElementId == roundTripped.ElementId).CurrentLocationId);
        Assert.Equal("east", projected.World.Representations.Single(value =>
            value.RepresentationId == roundTripped.RepresentationId).CurrentLocationId);
        Assert.Equal(roundTripped.OpenedReactionWindow, projected.ReactionWindow);
        Assert.Equal(CampaignPositionV10Kind.Reaction, projected.CurrentPosition.Kind);
        Assert.Equal(
            eventBytes,
            CampaignSuccessorEventSerializer.Serialize(roundTripped));
    }

    [Fact]
    public void ReplayReconstructsAgainstHistoricalPreStateAndRejectsSemanticTampering()
    {
        var fixture = CampaignV10TestData.Create();
        var actual = CampaignV10TestData.CreateTriggeringMove(
            fixture.MovementSnapshot,
            apparentRepresentationId: "apparent-forged");
        var before = CampaignSnapshotV10Serializer.Serialize(fixture.MovementSnapshot);
        var calls = 0;

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyMovement(
                fixture.MovementSnapshot,
                actual,
                fixture.Artifact,
                fixture.Scenario,
                (prior, input) =>
                {
                    calls++;
                    Assert.Equal(fixture.MovementSnapshot, prior);
                    Assert.Equal(fixture.TriggeringMove.ElementId, input.ElementId);
                    Assert.Equal(fixture.TriggeringMove.DestinationLocationId,
                        input.DestinationLocationId);
                    return fixture.TriggeringMove;
                }));

        Assert.Equal(1, calls);
        Assert.Equal(before, CampaignSnapshotV10Serializer.Serialize(fixture.MovementSnapshot));
    }

    [Fact]
    public void CheckpointReplayIsByteIdenticalAndDuplicateApplicationRejects()
    {
        var fixture = CampaignV10TestData.Create();
        var canonicalEvent = CampaignSuccessorEventSerializer.Deserialize(
            CampaignSuccessorEventSerializer.Serialize(fixture.TriggeringMove));
        var move = Assert.IsType<ElementMovedV2>(canonicalEvent);
        var expected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            move,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);
        var replayed = CampaignV10Projector.ReplayMovementCheckpoint(
            fixture.MovementSnapshot,
            [move],
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);

        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(expected),
            CampaignSnapshotV10Serializer.Serialize(replayed));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyMovement(
                replayed,
                move,
                fixture.Artifact,
                fixture.Scenario,
                (_, _) => fixture.TriggeringMove));
    }

    [Fact]
    public void MovementEndedAndComponentToeProvenanceRoundTripThroughReplay()
    {
        var fixture = CampaignV10TestData.Create();
        var ended = new CampaignMovementEndedState(
            fixture.TriggeringMove.SequencePosition);
        var moved = CampaignV10TestData.CopyMove(
            fixture.TriggeringMove,
            ended,
            fixture.TriggeringMove.OpenedReactionWindow);
        var roundTripped = Assert.IsType<ElementMovedV2>(
            CampaignSuccessorEventSerializer.Deserialize(
                CampaignSuccessorEventSerializer.Serialize(moved)));
        var projected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            roundTripped,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => moved);
        var before = fixture.MovementSnapshot.World.Elements.Single(value =>
            value.ElementId == moved.ElementId);
        var after = projected.World.Elements.Single(value =>
            value.ElementId == moved.ElementId);

        Assert.Equal(ended, after.OperationalState.MovementEnded);
        Assert.Equal(before.Components, after.Components);
        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(projected),
            CampaignSnapshotV10Serializer.Serialize(
                CampaignSnapshotV10Serializer.Deserialize(
                    CampaignSnapshotV10Serializer.Serialize(projected))));
    }

    [Fact]
    public void RehashedFrozenOpportunityTamperingStillRejects()
    {
        var fixture = CampaignV10TestData.Create();
        var window = fixture.TriggeringMove.OpenedReactionWindow!;
        var original = Assert.Single(window.FrozenOpportunities);
        var forgedRepresentation = new CampaignMapRepresentationState(
            "map-representation.9999",
            original.ReactingRepresentation.CurrentLocationId,
            original.ReactingRepresentation.BindingKind,
            original.ReactingRepresentation.BoundElementIds);
        var forgedOpportunity = new CampaignFrozenReactionOpportunity(
            CampaignReactionIdentity.CreateOpportunity(window.WindowId, forgedRepresentation),
            forgedRepresentation,
            original.AdjacencyEvidence);
        var forgedWindow = new CampaignReactionWindow(
            window.WindowId,
            window.TriggerCommittedStateVersion,
            window.PhasingSide,
            window.ReactingSide,
            window.ReactingPosition,
            window.TriggerAuthority,
            window.ApparentTrigger,
            [forgedOpportunity],
            [],
            null);
        var forged = CampaignV10TestData.CopyMove(
            fixture.TriggeringMove,
            fixture.TriggeringMove.MovementEndedAfter,
            forgedWindow);
        var canonicalForged = Assert.IsType<ElementMovedV2>(
            CampaignSuccessorEventSerializer.Deserialize(
                CampaignSuccessorEventSerializer.Serialize(forged)));

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyMovement(
                fixture.MovementSnapshot,
                canonicalForged,
                fixture.Artifact,
                fixture.Scenario,
                (_, _) => fixture.TriggeringMove));
    }

    [Fact]
    public void EventReaderRejectsNonCanonicalAndActiveReaderRejectsSuccessor()
    {
        var fixture = CampaignV10TestData.Create();
        var canonical = Encoding.UTF8.GetString(
            CampaignSuccessorEventSerializer.Serialize(fixture.TriggeringMove));
        var extra = canonical.Replace(
            "{\"contractVersion\":2,",
            "{\"contractVersion\":2,\"unexpected\":true,",
            StringComparison.Ordinal);
        var reordered = canonical.Replace(
            "{\"contractVersion\":2,\"eventType\":\"element-moved\"",
            "{\"eventType\":\"element-moved\",\"contractVersion\":2",
            StringComparison.Ordinal);
        var missing = canonical.Replace(
            "\"movementEndedAfter\":null,",
            string.Empty,
            StringComparison.Ordinal);
        var duplicate = canonical.Replace(
            "{\"contractVersion\":2,",
            "{\"contractVersion\":2,\"contractVersion\":2,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(extra)));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(reordered)));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(missing)));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(duplicate)));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical)));
    }
}
