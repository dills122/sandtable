using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class CapabilitiesRequestContractTests
{
    [Fact]
    public void CapabilitiesRequestIsAFieldlessMessageThatRoundTrips()
    {
        var request = new CapabilitiesRequest();

        var bytes = request.ToByteArray();
        var roundTripped = CapabilitiesRequest.Parser.ParseFrom(bytes);

        Assert.Empty(CapabilitiesRequest.Descriptor.Fields.InFieldNumberOrder());
        Assert.Empty(bytes);
        Assert.Equal(request, roundTripped);
    }
}
