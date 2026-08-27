using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class NarrativeChunkContractTests
{
    [Fact]
    public void NarrativeChunkCarriesTextCoordinate()
    {
        var chunk = new NarrativeChunk
        {
            Text = "The advance continues.",
        };

        var roundTripped = NarrativeChunk.Parser.ParseFrom(chunk.ToByteArray());

        Assert.Equal(1, NarrativeChunk.Descriptor.FindFieldByName("text").FieldNumber);
        Assert.Equal("The advance continues.", roundTripped.Text);
    }
}
