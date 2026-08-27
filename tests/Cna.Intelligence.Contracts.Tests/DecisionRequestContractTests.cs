using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class DecisionRequestContractTests
{
    [Fact]
    public void DecisionRequestCarriesCommanderObservationAndCandidateCoordinates()
    {
        var request = new DecisionRequest
        {
            DecisionId = "decision-1",
            GameId = "game-1",
            StateVersion = 42,
            RulesetHash = "rules-v1",
            Commander = new CommanderProfile
            {
                CommanderId = "commander-1",
                PersonaVersion = "persona-v1",
                Traits = { "aggressive", "cautious" },
            },
            Observation = new StrategicObservation
            {
                RelevantFacts = { "fact-1", "fact-2" },
                RecentEvents = { "event-1" },
            },
            Candidates =
            {
                new PlanCandidate
                {
                    PlanId = "plan-a",
                    PlanType = "offense",
                    ObjectiveScore = 0.5,
                    SupplyRisk = 0.1,
                    CasualtyRisk = 0.2,
                    ExpectedValue = 0.75,
                    RelevantFacts = { "fact-1" },
                },
            },
        };

        var roundTripped = DecisionRequest.Parser.ParseFrom(request.ToByteArray());

        Assert.Equal(1, DecisionRequest.Descriptor.FindFieldByName("decision_id").FieldNumber);
        Assert.Equal(2, DecisionRequest.Descriptor.FindFieldByName("game_id").FieldNumber);
        Assert.Equal(3, DecisionRequest.Descriptor.FindFieldByName("state_version").FieldNumber);
        Assert.Equal(4, DecisionRequest.Descriptor.FindFieldByName("ruleset_hash").FieldNumber);
        Assert.Equal(5, DecisionRequest.Descriptor.FindFieldByName("commander").FieldNumber);
        Assert.Equal(6, DecisionRequest.Descriptor.FindFieldByName("observation").FieldNumber);
        Assert.Equal(7, DecisionRequest.Descriptor.FindFieldByName("candidates").FieldNumber);

        Assert.Equal("decision-1", roundTripped.DecisionId);
        Assert.Equal("game-1", roundTripped.GameId);
        Assert.Equal(42, roundTripped.StateVersion);
        Assert.Equal("rules-v1", roundTripped.RulesetHash);
        Assert.Equal("commander-1", roundTripped.Commander.CommanderId);
        Assert.Equal("persona-v1", roundTripped.Commander.PersonaVersion);
        Assert.Equal(["aggressive", "cautious"], roundTripped.Commander.Traits);
        Assert.Equal(["fact-1", "fact-2"], roundTripped.Observation.RelevantFacts);
        Assert.Equal(["event-1"], roundTripped.Observation.RecentEvents);
        var candidate = Assert.Single(roundTripped.Candidates);
        Assert.Equal("plan-a", candidate.PlanId);
        Assert.Equal("offense", candidate.PlanType);
        Assert.Equal(0.5, candidate.ObjectiveScore);
        Assert.Equal(0.1, candidate.SupplyRisk);
        Assert.Equal(0.2, candidate.CasualtyRisk);
        Assert.Equal(0.75, candidate.ExpectedValue);
        Assert.Equal(["fact-1"], candidate.RelevantFacts);
    }
}
