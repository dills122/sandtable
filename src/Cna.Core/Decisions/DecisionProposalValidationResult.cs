namespace Cna.Core.Decisions;

public readonly record struct DecisionProposalValidationResult(
    bool IsAccepted,
    DecisionProposalRejectionReason RejectionReason);
