using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class PlanCandidateContractTests
{
    [Fact]
    public void PlanCandidateCarriesScoringAndRiskCoordinates()
    {
        var candidate = new PlanCandidate
        {
            PlanId = "plan-a",
            PlanType = "offense",
            ObjectiveScore = 0.5,
            SupplyRisk = 0.125,
            CasualtyRisk = 0.25,
            ExpectedValue = 0.75,
            RelevantFacts = { "fact-1", "fact-2" },
        };

        var roundTripped = PlanCandidate.Parser.ParseFrom(candidate.ToByteArray());

        Assert.Equal(1, PlanCandidate.Descriptor.FindFieldByName("plan_id").FieldNumber);
        Assert.Equal(2, PlanCandidate.Descriptor.FindFieldByName("plan_type").FieldNumber);
        Assert.Equal(3, PlanCandidate.Descriptor.FindFieldByName("objective_score").FieldNumber);
        Assert.Equal(4, PlanCandidate.Descriptor.FindFieldByName("supply_risk").FieldNumber);
        Assert.Equal(5, PlanCandidate.Descriptor.FindFieldByName("casualty_risk").FieldNumber);
        Assert.Equal(6, PlanCandidate.Descriptor.FindFieldByName("expected_value").FieldNumber);
        Assert.Equal(7, PlanCandidate.Descriptor.FindFieldByName("relevant_facts").FieldNumber);

        Assert.Equal("plan-a", roundTripped.PlanId);
        Assert.Equal("offense", roundTripped.PlanType);
        Assert.Equal(0.5, roundTripped.ObjectiveScore);
        Assert.Equal(0.125, roundTripped.SupplyRisk);
        Assert.Equal(0.25, roundTripped.CasualtyRisk);
        Assert.Equal(0.75, roundTripped.ExpectedValue);
        Assert.Equal(["fact-1", "fact-2"], roundTripped.RelevantFacts);
    }
}
