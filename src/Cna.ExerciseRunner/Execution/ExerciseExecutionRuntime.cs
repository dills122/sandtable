using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Execution;

internal sealed record ExerciseRuntimeQueryResult(
    bool IsSuccessful,
    CampaignLegalActionSet? ActionSet)
{
    internal static ExerciseRuntimeQueryResult From(CampaignLegalActionQueryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ExerciseRuntimeQueryResult(result.IsSuccessful, result.ActionSet);
    }

    internal static ExerciseRuntimeQueryResult Rejected() => new(false, null);
}

internal sealed record ExerciseRuntimeStepResult(
    bool IsAccepted,
    ExerciseSession? SuccessorSession,
    CampaignActionAcceptanceReceipt? Receipt,
    IReadOnlyList<byte[]> EventRecords,
    byte[] SnapshotCheckpoint,
    CampaignActionSubmissionRejectionReason RejectionReason)
{
    internal static ExerciseRuntimeStepResult From(ExerciseStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsAccepted)
            return Rejected(result.RejectionReason);
        var evidence = result.Evidence!;
        return new ExerciseRuntimeStepResult(
            true,
            result.SuccessorSession,
            evidence.Receipt,
            evidence.EventRecords.Select(value => value.ToArray()).ToArray(),
            evidence.SnapshotCheckpoint,
            CampaignActionSubmissionRejectionReason.None);
    }

    internal static ExerciseRuntimeStepResult Rejected(
        CampaignActionSubmissionRejectionReason rejectionReason)
    {
        if (rejectionReason == CampaignActionSubmissionRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        return new ExerciseRuntimeStepResult(false, null, null, [], [], rejectionReason);
    }
}

internal interface IExerciseExecutionRuntime
{
    ExerciseStartResult Begin(CampaignCreationRequest request);
    ExerciseCheckpoint QueryCheckpoint(ExerciseSession session);
    ExerciseRuntimeQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience);
    ExerciseControllerSelection Select(
        ExerciseControllerManifest policies,
        IReadOnlyList<ExerciseControllerActionSet> actionSets);
    ExerciseRuntimeStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission);
    ReconstructionProof Reconstruct(ExerciseSession session);
}

internal sealed class CoreExerciseExecutionRuntime : IExerciseExecutionRuntime
{
    internal static CoreExerciseExecutionRuntime Instance { get; } = new();

    private CoreExerciseExecutionRuntime()
    {
    }

    public ExerciseStartResult Begin(CampaignCreationRequest request) =>
        CampaignExercises.Begin(request);

    public ExerciseCheckpoint QueryCheckpoint(ExerciseSession session) =>
        CampaignExercises.QueryCheckpoint(session);

    public ExerciseRuntimeQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience) =>
        ExerciseRuntimeQueryResult.From(CampaignExercises.Query(session, audience));

    public ExerciseControllerSelection Select(
        ExerciseControllerManifest policies,
        IReadOnlyList<ExerciseControllerActionSet> actionSets) =>
        ExerciseController.Select(policies, actionSets);

    public ExerciseRuntimeStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission) =>
        ExerciseRuntimeStepResult.From(CampaignExercises.Submit(session, submission));

    public ReconstructionProof Reconstruct(ExerciseSession session) =>
        ReconstructionProof.From(CampaignExercises.Reconstruct(session));
}
