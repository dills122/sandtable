using Cna.Core.Decisions;

namespace Cna.Core.Tests.Decisions;

public sealed class DecisionProposalValidatorTests
{
    [Fact]
    public void ValidateAcceptsAMatchingActivePlan()
    {
        var pending = CreatePendingDecision();
        var proposal = CreateProposal();

        var result = DecisionProposalValidator.Validate(pending, proposal);

        Assert.True(result.IsAccepted);
        Assert.Equal(DecisionProposalRejectionReason.None, result.RejectionReason);
    }

    [Theory]
    [InlineData("other-decision", 42, "rules-v1", "plan-a", DecisionProposalRejectionReason.DecisionIdMismatch)]
    [InlineData("decision-1", 41, "rules-v1", "plan-a", DecisionProposalRejectionReason.StaleState)]
    [InlineData("decision-1", 42, "rules-v2", "plan-a", DecisionProposalRejectionReason.RulesetMismatch)]
    [InlineData("decision-1", 42, "rules-v1", "plan-c", DecisionProposalRejectionReason.UnknownPlan)]
    public void ValidateRejectsAProposalThatNoLongerMatchesThePendingDecision(
        string decisionId,
        long stateVersion,
        string rulesetHash,
        string selectedPlanId,
        DecisionProposalRejectionReason expectedReason)
    {
        var pending = CreatePendingDecision();
        var proposal = new DecisionProposal(
            decisionId,
            stateVersion,
            rulesetHash,
            selectedPlanId);

        var result = DecisionProposalValidator.Validate(pending, proposal);

        Assert.False(result.IsAccepted);
        Assert.Equal(expectedReason, result.RejectionReason);
    }

    private static PendingDecision CreatePendingDecision() => new(
        "decision-1",
        42,
        "rules-v1",
        new HashSet<string>(["plan-a", "plan-b"], StringComparer.Ordinal));

    private static DecisionProposal CreateProposal() => new(
        "decision-1",
        42,
        "rules-v1",
        "plan-a");
}
