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
    private readonly bool includeCombat;

    internal static CoreExerciseExecutionRuntime Instance { get; } = new(includeCombat: false);

    /// <summary>
    /// Task 022A (#448): also drives first-cycle Combat Selection and its System step through
    /// CampaignExercises.QueryWithCombat/SubmitWithCombat. Test-harness capability only, not Combat
    /// activation. Staged as a Runner-internal opt-in: the CLI and checked manifests keep
    /// <see cref="Instance"/> until a manifest opt-in (T022-Q9) and bundle readback of Combat-state
    /// snapshots (T022-Q8) are ruled.
    /// </summary>
    internal static CoreExerciseExecutionRuntime WithCombat { get; } = new(includeCombat: true);

    private CoreExerciseExecutionRuntime(bool includeCombat)
    {
        this.includeCombat = includeCombat;
    }

    public ExerciseStartResult Begin(CampaignCreationRequest request) =>
        CampaignExercises.Begin(request);

    public ExerciseCheckpoint QueryCheckpoint(ExerciseSession session) =>
        CampaignExercises.QueryCheckpoint(session);

    public ExerciseRuntimeQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience) =>
        ExerciseRuntimeQueryResult.From(includeCombat
            ? CampaignExercises.QueryWithCombat(session, audience)
            : CampaignExercises.Query(session, audience));

    public ExerciseControllerSelection Select(
        ExerciseControllerManifest policies,
        IReadOnlyList<ExerciseControllerActionSet> actionSets) =>
        ExerciseController.Select(policies, actionSets);

    public ExerciseRuntimeStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission) =>
        ExerciseRuntimeStepResult.From(includeCombat
            ? CampaignExercises.SubmitWithCombat(session, submission)
            : CampaignExercises.Submit(session, submission));

    public ReconstructionProof Reconstruct(ExerciseSession session) =>
        ReconstructionProof.From(CampaignExercises.Reconstruct(session));
}
