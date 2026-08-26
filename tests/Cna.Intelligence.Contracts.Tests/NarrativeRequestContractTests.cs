using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class NarrativeRequestContractTests
{
    [Fact]
    public void NarrativeRequestCarriesGameStateAndEventSummaryCoordinates()
    {
        var request = new NarrativeRequest
        {
            GameId = "game-1",
            StateVersion = 42,
            EventSummaries = { "summary-1", "summary-2" },
        };

        var roundTripped = NarrativeRequest.Parser.ParseFrom(request.ToByteArray());

        Assert.Equal(1, NarrativeRequest.Descriptor.FindFieldByName("game_id").FieldNumber);
        Assert.Equal(
            2, NarrativeRequest.Descriptor.FindFieldByName("state_version").FieldNumber);
        Assert.Equal(
            3, NarrativeRequest.Descriptor.FindFieldByName("event_summaries").FieldNumber);

        Assert.Equal("game-1", roundTripped.GameId);
        Assert.Equal(42, roundTripped.StateVersion);
        Assert.Equal(["summary-1", "summary-2"], roundTripped.EventSummaries);
    }
}
