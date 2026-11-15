using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSyntheticArrivalPhaseTests
{
    private static CampaignSyntheticArrivalCommand Command() => new(1, "campaign-a", CampaignSyntheticArrivalPhase.SetupHash,
        "request-1", 0, LandSide.Axis, "axis-arrival-group", "south-west");

    [Fact]
    public void SetupResolvesOnlyExactCanonicalRepositoryProfile()
    {
        var setup = CampaignSyntheticArrivalSetup.Instance;
        var bytes = setup.CopyCanonicalBytes();
        Assert.Same(setup, CampaignSyntheticArrivalSetup.Resolve(bytes, setup.Hash));
        Assert.Throws<ArgumentException>(() => CampaignSyntheticArrivalSetup.Resolve(bytes, "sha256:invalid"));
        bytes[0] = 0;
        Assert.Throws<ArgumentException>(() => CampaignSyntheticArrivalSetup.Resolve(bytes, setup.Hash));
        Assert.Same(setup, CampaignSyntheticArrivalSetup.Resolve(setup.CopyCanonicalBytes(), setup.Hash));
    }

    [Fact]
    public void ExactPhaseCreatesArrivalAndAdvancesOnceWithReplayAndRetry()
    {
        var initial = CampaignSyntheticArrivalPhase.Start("campaign-a", CampaignSyntheticArrivalPhase.ArrivalPosition());
        var command = Command();
        Assert.DoesNotContain(initial.World.Elements, element => element.ElementId == "axis-arrival-lab");
        var accepted = initial.Decide(command);
        Assert.Equal(1, accepted.State.Version);
        Assert.Equal(LandPhaseIds.CommonwealthFleet, accepted.State.Position.PhaseId);
        Assert.Equal(LandSegmentIds.FleetAssignment, accepted.State.Position.SegmentId);
        Assert.Contains(accepted.State.World.Elements, element => element.ElementId == "axis-arrival-lab" && element.CurrentLocationId == "south-west");
        var replayed = CampaignSyntheticArrivalPhase.Replay("campaign-a", [accepted.Event]);
        Assert.Equal(accepted.State.Position, replayed.Position);
        Assert.Equal(accepted.State.World.Elements.Select(value => value.ElementId), replayed.World.Elements.Select(value => value.ElementId));
        Assert.True(replayed.Decide(command).IsRetry);
        Assert.Throws<ArgumentException>(() => replayed.Decide(command with { DestinationId = "west" }));
        Assert.Throws<ArgumentException>(() => replayed.Decide(command with { CommandId = "different", ExpectedVersion = 1 }));
    }

    [Fact]
    public void WrongPhaseActorSetupOrForgedHistoryCannotAdvance()
    {
        var position = CampaignSyntheticArrivalPhase.ArrivalPosition();
        Assert.Throws<ArgumentException>(() => CampaignSyntheticArrivalPhase.Start("campaign-a", Cna1979LandSequence.GetNext(position)));
        var state = CampaignSyntheticArrivalPhase.Start("campaign-a", position);
        var command = Command();
        Assert.Throws<ArgumentException>(() => state.Decide(command with { Actor = LandSide.Commonwealth }));
        Assert.Throws<ArgumentException>(() => state.Decide(command with { CampaignId = "campaign-b" }));
        Assert.Throws<ArgumentException>(() => state.Decide(command with { SetupHash = "sha256:incorrect" }));
        Assert.Throws<ArgumentException>(() => state.Decide(command with { ExpectedVersion = 1 }));
        Assert.Equal(0, state.Version);
        var accepted = state.Decide(command);
        Assert.Throws<InvalidOperationException>(() => CampaignSyntheticArrivalPhase.Replay("campaign-b", [accepted.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignSyntheticArrivalPhase.Replay("campaign-a", [accepted.Event, accepted.Event]));
        Assert.Throws<InvalidOperationException>(() => CampaignSyntheticArrivalPhase.Replay("campaign-a",
            [new CampaignSyntheticArrivalEvent(1, CampaignSyntheticArrivalPhase.SetupHash, command, 1, ["axis-arrival-lab"], "wrong-position")]));
        Assert.Throws<InvalidOperationException>(() => CampaignSyntheticArrivalPhase.Replay("campaign-a",
            [new CampaignSyntheticArrivalEvent(1, CampaignSyntheticArrivalPhase.SetupHash, command, 1, ["wrong-unit"], accepted.Event.SuccessorPositionId)]));
    }

    [Fact]
    public void CanonicalEventRoundTripsAndReplayRejectsAlteredEffects()
    {
        var accepted = CampaignSyntheticArrivalPhase.Start("campaign-a", CampaignSyntheticArrivalPhase.ArrivalPosition()).Decide(Command());
        var bytes = CampaignSyntheticArrivalEventCodec.Encode(accepted.Event);
        var decoded = CampaignSyntheticArrivalEventCodec.Decode(bytes);
        var replayed = CampaignSyntheticArrivalPhase.Replay("campaign-a", [decoded]);
        Assert.Equal(accepted.State.Position, replayed.Position);
        Assert.True(replayed.Decide(Command()).IsRetry);
        Assert.Throws<JsonException>(() => CampaignSyntheticArrivalEventCodec.Decode([.. bytes, 0x20]));
        var forged = new CampaignSyntheticArrivalEvent(1, CampaignSyntheticArrivalPhase.SetupHash, Command(),
            1, ["wrong-unit"], accepted.Event.SuccessorPositionId);
        var forgedBytes = CampaignSyntheticArrivalEventCodec.Encode(forged);
        Assert.Throws<InvalidOperationException>(() => CampaignSyntheticArrivalPhase.Replay("campaign-a", [CampaignSyntheticArrivalEventCodec.Decode(forgedBytes)]));
    }

    [Fact]
    public void CheckpointRebuildsReceiptAndRejectsClaimedState()
    {
        var initial = CampaignSyntheticArrivalPhase.Start("campaign-a", CampaignSyntheticArrivalPhase.ArrivalPosition());
        var restoredInitial = CampaignSyntheticArrivalCheckpointCodec.Decode(CampaignSyntheticArrivalCheckpointCodec.Encode(initial), "campaign-a");
        Assert.Equal(0, restoredInitial.Version);
        var accepted = initial.Decide(Command());
        var bytes = CampaignSyntheticArrivalCheckpointCodec.Encode(accepted.State);
        var restored = CampaignSyntheticArrivalCheckpointCodec.Decode(bytes, "campaign-a");
        Assert.Throws<JsonException>(() => CampaignSyntheticArrivalCheckpointCodec.Decode(bytes, "campaign-b"));
        Assert.Equal(accepted.State.Position, restored.Position);
        Assert.Equal(accepted.State.World.Elements.Select(value => value.ElementId), restored.World.Elements.Select(value => value.ElementId));
        Assert.True(restored.Decide(Command()).IsRetry);
        var json = Encoding.UTF8.GetString(bytes);
        var forged = Encoding.UTF8.GetBytes(json.Replace("\"StateVersion\":1", "\"StateVersion\":2", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignSyntheticArrivalCheckpointCodec.Decode(forged, "campaign-a"));
        Assert.Throws<JsonException>(() => CampaignSyntheticArrivalCheckpointCodec.Decode([.. bytes, 0x20], "campaign-a"));
    }

    [Fact]
    public void OpeningExposesEligibleOfferOnlyToItsOwner()
    {
        var initial = CampaignSyntheticArrivalPhase.Start("campaign-a", CampaignSyntheticArrivalPhase.ArrivalPosition());
        var ownerOffer = Assert.Single(initial.QueryOffers(LandSide.Axis));
        Assert.Equal("axis-arrival-group", ownerOffer.GroupId);
        Assert.Equal("south-west", ownerOffer.DestinationId);
        Assert.Empty(initial.QueryOffers(LandSide.Commonwealth));
        var accepted = initial.Decide(Command());
        Assert.Empty(accepted.State.QueryOffers(LandSide.Axis));
        Assert.Empty(accepted.State.QueryOffers(LandSide.Commonwealth));
        Assert.Throws<ArgumentOutOfRangeException>(() => initial.QueryOffers((LandSide)999));
    }
}
