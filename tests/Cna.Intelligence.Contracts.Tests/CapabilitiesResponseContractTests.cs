using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class CapabilitiesResponseContractTests
{
    [Fact]
    public void CapabilitiesResponseCarriesProviderAvailabilityCoordinates()
    {
        var response = new CapabilitiesResponse
        {
            DecisionProviderAvailable = true,
            NarrativeProviderAvailable = false,
            Providers = { "provider-a", "provider-b" },
        };

        var roundTripped = CapabilitiesResponse.Parser.ParseFrom(response.ToByteArray());

        Assert.Equal(
            1,
            CapabilitiesResponse.Descriptor.FindFieldByName("decision_provider_available")
                .FieldNumber);
        Assert.Equal(
            2,
            CapabilitiesResponse.Descriptor.FindFieldByName("narrative_provider_available")
                .FieldNumber);
        Assert.Equal(
            3, CapabilitiesResponse.Descriptor.FindFieldByName("providers").FieldNumber);

        Assert.True(roundTripped.DecisionProviderAvailable);
        Assert.False(roundTripped.NarrativeProviderAvailable);
        Assert.Equal(["provider-a", "provider-b"], roundTripped.Providers);
    }
}
