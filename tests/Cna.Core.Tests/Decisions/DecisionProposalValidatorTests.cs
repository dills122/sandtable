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

    [Fact]
    public void ValidateDoesNotObservePlanIdMutationsAfterPendingDecisionCreation()
    {
        var sourcePlanIds = new HashSet<string>(["plan-a"], StringComparer.Ordinal);
        var pending = new PendingDecision("decision-1", 42, "rules-v1", sourcePlanIds);
        sourcePlanIds.Remove("plan-a");
        sourcePlanIds.Add("plan-b");

        var originalPlanResult = DecisionProposalValidator.Validate(pending, CreateProposal());
        var addedPlanResult = DecisionProposalValidator.Validate(
            pending,
            CreateProposal(selectedPlanId: "plan-b"));

        Assert.True(originalPlanResult.IsAccepted);
        Assert.Equal(DecisionProposalRejectionReason.None, originalPlanResult.RejectionReason);
        Assert.False(addedPlanResult.IsAccepted);
        Assert.Equal(DecisionProposalRejectionReason.UnknownPlan, addedPlanResult.RejectionReason);
    }

    [Fact]
    public void ValidateUsesOrdinalPlanIdSemanticsRegardlessOfTheSourceComparer()
    {
        var sourcePlanIds = new HashSet<string>(["plan-a"], StringComparer.OrdinalIgnoreCase);
        var pending = new PendingDecision("decision-1", 42, "rules-v1", sourcePlanIds);
        var proposal = CreateProposal(selectedPlanId: "PLAN-A");

        var result = DecisionProposalValidator.Validate(pending, proposal);

        Assert.False(result.IsAccepted);
        Assert.Equal(DecisionProposalRejectionReason.UnknownPlan, result.RejectionReason);
    }

    [Fact]
    public void PendingDecisionPreservesItsFormerPositionalRecordSourceSurface()
    {
        var pending = new PendingDecision(
            DecisionId: "decision-1",
            StateVersion: 42,
            RulesetHash: "rules-v1",
            ValidPlanIds: new HashSet<string>(["plan-a"], StringComparer.Ordinal));

        var (decisionId, stateVersion, rulesetHash, validPlanIds) = pending;

        Assert.Equal("decision-1", decisionId);
        Assert.Equal(42, stateVersion);
        Assert.Equal("rules-v1", rulesetHash);
        Assert.Contains("plan-a", validPlanIds);
    }

    [Fact]
    public void PendingDecisionWithExpressionFreezesReplacementPlanIds()
    {
        var pending = CreatePendingDecision();
        var replacement = new HashSet<string>(["plan-c"], StringComparer.OrdinalIgnoreCase);

        var updated = pending with { ValidPlanIds = replacement };
        replacement.Clear();
        replacement.Add("plan-d");

        Assert.Contains("plan-c", updated.ValidPlanIds);
        Assert.DoesNotContain("PLAN-C", updated.ValidPlanIds);
        Assert.DoesNotContain("plan-d", updated.ValidPlanIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void PendingDecisionRejectsAnInvalidDecisionId(string? decisionId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PendingDecision(
            decisionId!,
            42,
            "rules-v1",
            new HashSet<string>(["plan-a"], StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void PendingDecisionRejectsAnInvalidRulesetHash(string? rulesetHash)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PendingDecision(
            "decision-1",
            42,
            rulesetHash!,
            new HashSet<string>(["plan-a"], StringComparer.Ordinal)));
    }

    [Fact]
    public void PendingDecisionRejectsANegativeStateVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PendingDecision(
            "decision-1",
            -1,
            "rules-v1",
            new HashSet<string>(["plan-a"], StringComparer.Ordinal)));
    }

    [Fact]
    public void PendingDecisionRejectsANullPlanIdSet()
    {
        Assert.Throws<ArgumentNullException>(() => new PendingDecision(
            "decision-1",
            42,
            "rules-v1",
            null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void PendingDecisionRejectsAnInvalidPlanId(string? planId)
    {
        Assert.Throws<ArgumentException>(() => new PendingDecision(
            "decision-1",
            42,
            "rules-v1",
            new HashSet<string>([planId!], StringComparer.Ordinal)));
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

    private static DecisionProposal CreateProposal(string selectedPlanId = "plan-a") => new(
        "decision-1",
        42,
        "rules-v1",
        selectedPlanId);
}
