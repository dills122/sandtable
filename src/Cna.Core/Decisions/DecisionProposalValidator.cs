namespace Cna.Core.Decisions;

public static class DecisionProposalValidator
{
    public static DecisionProposalValidationResult Validate(
        PendingDecision pending,
        DecisionProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(proposal);

        if (!string.Equals(pending.DecisionId, proposal.DecisionId, StringComparison.Ordinal))
        {
            return Reject(DecisionProposalRejectionReason.DecisionIdMismatch);
        }

        if (pending.StateVersion != proposal.BasedOnStateVersion)
        {
            return Reject(DecisionProposalRejectionReason.StaleState);
        }

        if (!string.Equals(pending.RulesetHash, proposal.RulesetHash, StringComparison.Ordinal))
        {
            return Reject(DecisionProposalRejectionReason.RulesetMismatch);
        }

        return pending.ValidPlanIds.Contains(proposal.SelectedPlanId)
            ? new DecisionProposalValidationResult(true, DecisionProposalRejectionReason.None)
            : Reject(DecisionProposalRejectionReason.UnknownPlan);
    }

    private static DecisionProposalValidationResult Reject(
        DecisionProposalRejectionReason reason) => new(false, reason);
}
