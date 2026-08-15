using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class DecisionResponseContractTests
{
    [Fact]
    public void DecisionResponseCarriesProposalValidationCoordinates()
    {
        var response = new DecisionResponse
        {
            DecisionId = "decision-1",
            BasedOnStateVersion = 42,
            RulesetHash = "rules-v1",
            SelectedPlanId = "plan-a",
        };

        var roundTripped = DecisionResponse.Parser.ParseFrom(response.ToByteArray());

        Assert.Equal(7, DecisionResponse.Descriptor.FindFieldByName("ruleset_hash").FieldNumber);
        Assert.Equal("decision-1", roundTripped.DecisionId);
        Assert.Equal(42, roundTripped.BasedOnStateVersion);
        Assert.Equal("rules-v1", roundTripped.RulesetHash);
        Assert.Equal("plan-a", roundTripped.SelectedPlanId);
    }
}
