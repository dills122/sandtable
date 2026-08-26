using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class StrategicObservationContractTests
{
    [Fact]
    public void StrategicObservationCarriesFactAndEventCoordinates()
    {
        var observation = new StrategicObservation
        {
            RelevantFacts = { "fact-1", "fact-2" },
            RecentEvents = { "event-1" },
        };

        var roundTripped = StrategicObservation.Parser.ParseFrom(observation.ToByteArray());

        Assert.Equal(
            1, StrategicObservation.Descriptor.FindFieldByName("relevant_facts").FieldNumber);
        Assert.Equal(
            2, StrategicObservation.Descriptor.FindFieldByName("recent_events").FieldNumber);

        Assert.Equal(["fact-1", "fact-2"], roundTripped.RelevantFacts);
        Assert.Equal(["event-1"], roundTripped.RecentEvents);
    }
}
