using Cna.Core.Actions;

namespace Cna.Core.Exercises;

public sealed class ExerciseStepResult
{
    private ExerciseStepResult(
        ExerciseSession? successorSession,
        ExerciseStepEvidence? evidence,
        CampaignActionSubmissionRejectionReason rejectionReason)
    {
        SuccessorSession = successorSession;
        Evidence = evidence;
        RejectionReason = rejectionReason;
    }

    public bool IsAccepted => SuccessorSession is not null;
    public ExerciseSession? SuccessorSession { get; }
    public ExerciseStepEvidence? Evidence { get; }
    public CampaignActionSubmissionRejectionReason RejectionReason { get; }

    internal static ExerciseStepResult Accepted(
        ExerciseSession successorSession,
        ExerciseStepEvidence evidence) =>
        new(
            successorSession ?? throw new ArgumentNullException(nameof(successorSession)),
            evidence ?? throw new ArgumentNullException(nameof(evidence)),
            CampaignActionSubmissionRejectionReason.None);

    internal static ExerciseStepResult Rejected(
        CampaignActionSubmissionRejectionReason rejectionReason)
    {
        if (rejectionReason == CampaignActionSubmissionRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        return new ExerciseStepResult(null, null, rejectionReason);
    }
}
