using System.Diagnostics;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Execution;

internal sealed record ExerciseQueryDiagnostic(
    CampaignActionAudience Audience,
    int CandidateCount,
    ExerciseCheckFailureCode FailureCode,
    long ElapsedMicroseconds);

internal enum ExerciseDecisionFailureStage
{
    AuthorityQuery,
    ControllerSelection,
    SelectedActionMembership,
    ActionSubmission,
    EventCardinality,
    CheckpointContinuity,
}

internal sealed class ExerciseDecisionDiagnostic
{
    private readonly List<ExerciseQueryDiagnostic> queries = [];

    internal ExerciseDecisionDiagnostic(
        int ordinal,
        ExerciseCheckpoint checkpoint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentNullException.ThrowIfNull(checkpoint);
        Ordinal = ordinal;
        CampaignId = checkpoint.CampaignId;
        StateVersion = checkpoint.StateVersion;
        PositionId = checkpoint.PositionId;
    }

    internal int Ordinal { get; }
    internal string CampaignId { get; }
    internal long StateVersion { get; }
    internal string PositionId { get; }
    internal IReadOnlyList<ExerciseQueryDiagnostic> Queries => queries.AsReadOnly();
    internal int ActiveAudienceCount { get; private set; }
    internal ExerciseControllerSelectionFailure? SelectionFailure { get; private set; }
    internal CampaignActionAudience? SelectedAudience { get; private set; }
    internal string? SelectedActionId { get; private set; }
    internal long? ControllerElapsedMicroseconds { get; private set; }
    internal bool SubmissionAttempted { get; private set; }
    internal bool? SubmissionAccepted { get; private set; }
    internal CampaignActionSubmissionRejectionReason? SubmissionRejectionReason { get; private set; }
    internal int? SubmittedEventCount { get; private set; }
    internal long? SubmissionElapsedMicroseconds { get; private set; }
    internal ExerciseDecisionFailureStage FailureStage { get; private set; }
    internal ExerciseCheckFailureCode FailureCode { get; private set; }

    internal void RecordQuery(ExerciseQueryDiagnostic query)
    {
        ArgumentNullException.ThrowIfNull(query);
        queries.Add(query);
        ActiveAudienceCount = queries.Count(value => value.CandidateCount > 0);
    }

    internal void RecordSelection(
        ExerciseControllerSelection selection,
        long elapsedMicroseconds,
        CampaignActionAudience? attemptedAudience = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMicroseconds);
        SelectionFailure = selection.FailureReason == ExerciseControllerSelectionFailure.None
            ? null
            : selection.FailureReason;
        SelectedAudience = selection.Audience ?? attemptedAudience;
        SelectedActionId = selection.ActionId;
        ControllerElapsedMicroseconds = elapsedMicroseconds;
    }

    internal void RecordSubmission(
        ExerciseRuntimeStepResult result,
        long elapsedMicroseconds)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMicroseconds);
        SubmissionAttempted = true;
        SubmissionAccepted = result.IsAccepted;
        SubmissionRejectionReason = result.IsAccepted ? null : result.RejectionReason;
        SubmittedEventCount = result.IsAccepted ? result.EventRecords.Count : null;
        SubmissionElapsedMicroseconds = elapsedMicroseconds;
    }

    internal void MarkFailed(
        ExerciseDecisionFailureStage stage,
        ExerciseCheckFailureCode failureCode)
    {
        if (!Enum.IsDefined(stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        if (failureCode == ExerciseCheckFailureCode.None || !Enum.IsDefined(failureCode))
            throw new ArgumentOutOfRangeException(nameof(failureCode));
        FailureStage = stage;
        FailureCode = failureCode;
    }
}

public sealed class ExerciseAcceptedStep
{
    private readonly byte[][] eventRecords;
    private readonly byte[] snapshotCheckpoint;
    private readonly ExerciseQueryDiagnostic[] queryDiagnostics;

    internal ExerciseAcceptedStep(
        int ordinal,
        CampaignActionAcceptanceReceipt receipt,
        IReadOnlyList<byte[]> eventRecords,
        byte[] snapshotCheckpoint,
        string? priorPositionId = null,
        IEnumerable<ExerciseQueryDiagnostic>? queryDiagnostics = null,
        int activeAudienceCount = 0,
        long controllerElapsedMicroseconds = 0,
        long submissionElapsedMicroseconds = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(activeAudienceCount);
        ArgumentOutOfRangeException.ThrowIfNegative(controllerElapsedMicroseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(submissionElapsedMicroseconds);
        Ordinal = ordinal;
        Receipt = receipt;
        this.eventRecords = eventRecords.Select(value => value.ToArray()).ToArray();
        this.snapshotCheckpoint = snapshotCheckpoint.ToArray();
        PriorPositionId = priorPositionId ?? receipt.ResultingPositionId;
        this.queryDiagnostics = queryDiagnostics?.ToArray() ?? [];
        ActiveAudienceCount = activeAudienceCount;
        ControllerElapsedMicroseconds = controllerElapsedMicroseconds;
        SubmissionElapsedMicroseconds = submissionElapsedMicroseconds;
    }

    public int Ordinal { get; }
    public CampaignActionAcceptanceReceipt Receipt { get; }
    public CampaignActionAudience Audience => Receipt.Audience;
    public string ActionId => Receipt.ActionId;
    public IReadOnlyList<byte[]> EventRecords => Array.AsReadOnly(
        eventRecords.Select(value => value.ToArray()).ToArray());
    public byte[] SnapshotCheckpoint => snapshotCheckpoint.ToArray();
    internal string PriorPositionId { get; }
    internal IReadOnlyList<ExerciseQueryDiagnostic> QueryDiagnostics =>
        Array.AsReadOnly(queryDiagnostics);
    internal int ActiveAudienceCount { get; }
    internal long ControllerElapsedMicroseconds { get; }
    internal long SubmissionElapsedMicroseconds { get; }
}

public sealed class ExerciseExecutionResult
{
    private readonly byte[] initialSnapshot;
    private readonly byte[] finalSnapshot;
    private readonly ExerciseDecisionDiagnostic[] failedDecisions;

    internal ExerciseExecutionResult(
        ExerciseRunResult runResult,
        IEnumerable<ExerciseAcceptedStep> steps,
        byte[] initialSnapshot,
        byte[] finalSnapshot,
        ReconstructionProof? reconstruction,
        ExerciseCheckResults checkResults,
        ExerciseSeedLedger seedLedger,
        long? beginElapsedMicroseconds = null,
        long? reconstructionElapsedMicroseconds = null,
        IEnumerable<ExerciseDecisionDiagnostic>? failedDecisions = null)
    {
        if (beginElapsedMicroseconds is < 0)
            throw new ArgumentOutOfRangeException(nameof(beginElapsedMicroseconds));
        if (reconstructionElapsedMicroseconds is < 0)
            throw new ArgumentOutOfRangeException(nameof(reconstructionElapsedMicroseconds));
        RunResult = runResult ?? throw new ArgumentNullException(nameof(runResult));
        Steps = Array.AsReadOnly(steps.ToArray());
        this.initialSnapshot = initialSnapshot.ToArray();
        this.finalSnapshot = finalSnapshot.ToArray();
        Reconstruction = reconstruction;
        CheckResults = checkResults ?? throw new ArgumentNullException(nameof(checkResults));
        SeedLedger = seedLedger ?? throw new ArgumentNullException(nameof(seedLedger));
        BeginElapsedMicroseconds = beginElapsedMicroseconds;
        ReconstructionElapsedMicroseconds = reconstructionElapsedMicroseconds;
        this.failedDecisions = failedDecisions?.ToArray() ?? [];
    }

    public ExerciseRunResult RunResult { get; }
    public bool IsSucceeded => RunResult.Completion is ExerciseSucceeded;
    public string? BoundaryPositionId =>
        (RunResult.Completion as ExerciseSucceeded)?.Outcome is BoundaryReached boundary
            ? boundary.PositionId
            : null;
    public ExerciseFailureCategory? FailureCategory =>
        (RunResult.Completion as ExerciseFailed)?.Failure.Category;
    public IReadOnlyList<ExerciseAcceptedStep> Steps { get; }
    public byte[] InitialSnapshot => initialSnapshot.ToArray();
    public byte[] FinalSnapshot => finalSnapshot.ToArray();
    public ReconstructionProof? Reconstruction { get; }
    public ExerciseCheckResults CheckResults { get; }
    public ExerciseSeedLedger SeedLedger { get; }
    internal long? BeginElapsedMicroseconds { get; }
    internal long? ReconstructionElapsedMicroseconds { get; }
    internal IReadOnlyList<ExerciseDecisionDiagnostic> FailedDecisions =>
        Array.AsReadOnly(failedDecisions);
}

public static class ExerciseExecutor
{
    private static readonly CampaignActionAudience[] AudienceOrder =
    [
        CampaignActionAudience.System,
        CampaignActionAudience.Axis,
        CampaignActionAudience.Commonwealth,
    ];

    public static ExerciseExecutionResult Execute(ExerciseManifest manifest) =>
        Execute(manifest, CancellationToken.None);

    public static ExerciseExecutionResult Execute(
        ExerciseManifest manifest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return Execute(
            manifest,
            ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed),
            cancellationToken);
    }

    internal static ExerciseExecutionResult Execute(
        ExerciseManifest manifest,
        ExerciseRunIdentity identity,
        CancellationToken cancellationToken) =>
        Execute(manifest, identity, CoreExerciseExecutionRuntime.Instance, cancellationToken);

    internal static ExerciseExecutionResult Execute(
        ExerciseManifest manifest,
        IExerciseExecutionRuntime runtime,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return Execute(
            manifest,
            ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed),
            runtime,
            cancellationToken);
    }

    internal static ExerciseExecutionResult Execute(
        ExerciseManifest manifest,
        ExerciseRunIdentity identity,
        IExerciseExecutionRuntime runtime,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(runtime);
        if (identity.RootSeed != manifest.RootSeed)
            throw new ArgumentException(
                "The run identity root seed must match the admitted Exercise manifest.",
                nameof(identity));
        var seedLedger = ExerciseSeedLedger.Create(identity);
        var request = CreateRequest(manifest, identity);
        var checks = new List<ExerciseCheckResult>();
        if (cancellationToken.IsCancellationRequested)
            return Failed(
                ExerciseFailureCategory.Cancelled,
                manifest,
                [],
                [],
                [],
                checks,
                seedLedger);
        var beginStarted = Stopwatch.GetTimestamp();
        var start = runtime.Begin(request);
        var beginElapsedMicroseconds = ElapsedMicroseconds(beginStarted);
        if (!start.IsStarted)
            return Failed(
                ExerciseFailureCategory.ManifestInvalid,
                manifest,
                [],
                start.InitialSnapshotBytes ?? [],
                start.InitialSnapshotBytes ?? [],
                checks,
                seedLedger,
                beginElapsedMicroseconds: beginElapsedMicroseconds);

        var initial = start.InitialSnapshotBytes!;
        var current = initial;
        var session = start.Session!;
        var steps = new List<ExerciseAcceptedStep>();

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.Cancelled,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            var checkpoint = runtime.QueryCheckpoint(session);
            if (string.Equals(
                    checkpoint.PositionId,
                    manifest.TerminalBoundary,
                    StringComparison.Ordinal))
            {
                checks.Add(ExerciseCheckResult.Passed(
                    ExerciseCheckId.TerminalBoundary,
                    null,
                    null));
                var reconstructionStarted = Stopwatch.GetTimestamp();
                var reconstruction = runtime.Reconstruct(session);
                var reconstructionElapsedMicroseconds = ElapsedMicroseconds(reconstructionStarted);
                checks.Add(reconstruction.IsVerified
                    ? ExerciseCheckResult.Passed(
                        ExerciseCheckId.HistoryReconstruction,
                        null,
                        null)
                    : ExerciseCheckResult.Failed(
                        ExerciseCheckId.HistoryReconstruction,
                        null,
                        null,
                        ExerciseCheckFailureCode.ReconstructionMismatch));
                return reconstruction.IsVerified
                    ? new ExerciseExecutionResult(
                        ExerciseRunResult.Succeeded(new BoundaryReached(checkpoint.PositionId)),
                        steps,
                        initial,
                        current,
                        reconstruction,
                        new ExerciseCheckResults(checks),
                        seedLedger,
                        beginElapsedMicroseconds,
                        reconstructionElapsedMicroseconds)
                    : Failed(
                        ExerciseFailureCategory.ReconstructionMismatch,
                        manifest,
                        steps,
                        initial,
                        current,
                        checks,
                        seedLedger,
                        reconstruction,
                        beginElapsedMicroseconds: beginElapsedMicroseconds,
                        reconstructionElapsedMicroseconds: reconstructionElapsedMicroseconds);
            }
            if (steps.Count >= manifest.MaximumSteps)
            {
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.StepLimitExceeded,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }

            var stepOrdinal = steps.Count;
            var queries = new List<ExerciseRuntimeQueryResult>();
            var queryDiagnostics = new List<ExerciseQueryDiagnostic>();
            var decision = new ExerciseDecisionDiagnostic(stepOrdinal, checkpoint);
            foreach (var audience in AudienceOrder)
            {
                var queryStarted = Stopwatch.GetTimestamp();
                var query = runtime.Query(session, audience);
                var queryElapsedMicroseconds = ElapsedMicroseconds(queryStarted);
                var queryFailure = QueryFailure(checkpoint, audience, query);
                var queryDiagnostic = new ExerciseQueryDiagnostic(
                    audience,
                    query.ActionSet?.Candidates.Count ?? 0,
                    queryFailure,
                    queryElapsedMicroseconds);
                queryDiagnostics.Add(queryDiagnostic);
                decision.RecordQuery(queryDiagnostic);
                checks.Add(queryFailure == ExerciseCheckFailureCode.None
                    ? ExerciseCheckResult.Passed(
                        ExerciseCheckId.AuthorityQueryValid,
                        stepOrdinal,
                        audience)
                    : ExerciseCheckResult.Failed(
                        ExerciseCheckId.AuthorityQueryValid,
                        stepOrdinal,
                        audience,
                        queryFailure));
                if (queryFailure != ExerciseCheckFailureCode.None)
                {
                    decision.MarkFailed(ExerciseDecisionFailureStage.AuthorityQuery, queryFailure);
                    AppendTerminalFailure(checks);
                    return Failed(
                        ExerciseFailureCategory.InvariantFailed,
                        manifest,
                        steps,
                        initial,
                        current,
                        checks,
                        seedLedger,
                        failedDecisions: [decision],
                        beginElapsedMicroseconds: beginElapsedMicroseconds);
                }
                queries.Add(query);
            }

            var controllerStarted = Stopwatch.GetTimestamp();
            var selection = runtime.Select(
                manifest.Controllers,
                queries.Select(result => new ExerciseControllerActionSet(
                    result.ActionSet!.Audience,
                    result.ActionSet.Candidates.Select(candidate => candidate.ActionId))).ToArray());
            var controllerElapsedMicroseconds = ElapsedMicroseconds(controllerStarted);
            var activeAudiences = queryDiagnostics
                .Where(value => value.CandidateCount > 0)
                .Select(value => value.Audience)
                .ToArray();
            decision.RecordSelection(
                selection,
                controllerElapsedMicroseconds,
                activeAudiences.Length == 1 ? activeAudiences[0] : null);
            if (selection.FailureReason == ExerciseControllerSelectionFailure.NoActiveAudience)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.ActiveAudienceCardinality,
                    stepOrdinal,
                    null,
                    ExerciseCheckFailureCode.NoActiveAudience));
                decision.MarkFailed(
                    ExerciseDecisionFailureStage.ControllerSelection,
                    ExerciseCheckFailureCode.NoActiveAudience);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.NoUniqueLegalAction,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            if (selection.FailureReason
                == ExerciseControllerSelectionFailure.MultipleActiveAudiences)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.ActiveAudienceCardinality,
                    stepOrdinal,
                    null,
                    ExerciseCheckFailureCode.MultipleActiveAudiences));
                decision.MarkFailed(
                    ExerciseDecisionFailureStage.ControllerSelection,
                    ExerciseCheckFailureCode.MultipleActiveAudiences);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.InvariantFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            if (!selection.IsSelected)
            {
                checks.Add(ExerciseCheckResult.Passed(
                    ExerciseCheckId.ActiveAudienceCardinality,
                    stepOrdinal,
                    null));
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.SelectedActionMembership,
                    stepOrdinal,
                    decision.SelectedAudience,
                    ExerciseCheckFailureCode.SelectedActionNotCurrent));
                decision.MarkFailed(
                    selection.FailureReason == ExerciseControllerSelectionFailure.PolicyFailed
                        ? ExerciseDecisionFailureStage.ControllerSelection
                        : ExerciseDecisionFailureStage.SelectedActionMembership,
                    ExerciseCheckFailureCode.SelectedActionNotCurrent);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.ControllerFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            checks.Add(ExerciseCheckResult.Passed(
                ExerciseCheckId.ActiveAudienceCardinality,
                stepOrdinal,
                null));
            var set = queries.Single(result =>
                result.ActionSet!.Audience == selection.Audience).ActionSet!;
            var candidate = set.Candidates.SingleOrDefault(value => string.Equals(
                value.ActionId,
                selection.ActionId,
                StringComparison.Ordinal));
            checks.Add(candidate is not null
                ? ExerciseCheckResult.Passed(
                    ExerciseCheckId.SelectedActionMembership,
                    stepOrdinal,
                    set.Audience)
                : ExerciseCheckResult.Failed(
                    ExerciseCheckId.SelectedActionMembership,
                    stepOrdinal,
                    set.Audience,
                    ExerciseCheckFailureCode.SelectedActionNotCurrent));
            if (candidate is null)
            {
                decision.MarkFailed(
                    ExerciseDecisionFailureStage.SelectedActionMembership,
                    ExerciseCheckFailureCode.SelectedActionNotCurrent);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.ControllerFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            var submission = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                set.CampaignId,
                set.StateVersion,
                set.PositionId,
                set.Audience,
                candidate.ActionId);
            var submissionStarted = Stopwatch.GetTimestamp();
            var submitted = runtime.Submit(session, submission);
            var submissionElapsedMicroseconds = ElapsedMicroseconds(submissionStarted);
            decision.RecordSubmission(submitted, submissionElapsedMicroseconds);
            if (!submitted.IsAccepted)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.AcceptedEventCardinality,
                    stepOrdinal,
                    set.Audience,
                    ExerciseCheckFailureCode.ActionRejected));
                decision.MarkFailed(
                    ExerciseDecisionFailureStage.ActionSubmission,
                    ExerciseCheckFailureCode.ActionRejected);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.IllegalAction,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            if (submitted.EventRecords.Count != 1)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.AcceptedEventCardinality,
                    stepOrdinal,
                    set.Audience,
                    ExerciseCheckFailureCode.EventCardinalityMismatch));
                decision.MarkFailed(
                    ExerciseDecisionFailureStage.EventCardinality,
                    ExerciseCheckFailureCode.EventCardinalityMismatch);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.InvariantFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            checks.Add(ExerciseCheckResult.Passed(
                ExerciseCheckId.AcceptedEventCardinality,
                stepOrdinal,
                set.Audience));
            var successor = submitted.SuccessorSession!;
            var resultingCheckpoint = runtime.QueryCheckpoint(successor);
            var continuityFailure = ContinuityFailure(
                checkpoint,
                resultingCheckpoint,
                submitted.Receipt!);
            checks.Add(continuityFailure == ExerciseCheckFailureCode.None
                ? ExerciseCheckResult.Passed(
                    ExerciseCheckId.CheckpointContinuity,
                    stepOrdinal,
                    set.Audience)
                : ExerciseCheckResult.Failed(
                    ExerciseCheckId.CheckpointContinuity,
                    stepOrdinal,
                    set.Audience,
                    continuityFailure));
            if (continuityFailure != ExerciseCheckFailureCode.None)
            {
                decision.MarkFailed(
                    ExerciseDecisionFailureStage.CheckpointContinuity,
                    continuityFailure);
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.InvariantFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger,
                    failedDecisions: [decision],
                    beginElapsedMicroseconds: beginElapsedMicroseconds);
            }
            current = submitted.SnapshotCheckpoint;
            steps.Add(new ExerciseAcceptedStep(
                steps.Count,
                submitted.Receipt!,
                submitted.EventRecords,
                current,
                checkpoint.PositionId,
                queryDiagnostics,
                queryDiagnostics.Count(value => value.CandidateCount > 0),
                controllerElapsedMicroseconds,
                submissionElapsedMicroseconds));
            session = successor;
        }
    }

    internal static CampaignCreationRequest CreateRequest(ExerciseManifest manifest)
    {
        var identity = ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed);
        return CreateRequest(manifest, identity);
    }

    internal static CampaignCreationRequest CreateRequest(
        ExerciseManifest manifest,
        ExerciseRunIdentity identity)
    {
        var umpireSeed = ExerciseSeedDeriver.Derive(
            identity,
            ExerciseSeedDomain.Umpire,
            null).DerivedSeed;
        return new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            ExerciseCampaignId.Derive(identity),
            manifest.RulesetHash,
            umpireSeed,
            manifest.SetupId,
            manifest.SetupHash,
            manifest.ContentPackId,
            manifest.ContentHash,
            manifest.ScenarioId);
    }

    private static ExerciseExecutionResult Failed(
        ExerciseFailureCategory category,
        ExerciseManifest manifest,
        IEnumerable<ExerciseAcceptedStep> steps,
        byte[] initial,
        byte[] final,
        IEnumerable<ExerciseCheckResult> checks,
        ExerciseSeedLedger seedLedger,
        ReconstructionProof? reconstruction = null,
        IEnumerable<ExerciseDecisionDiagnostic>? failedDecisions = null,
        long? beginElapsedMicroseconds = null,
        long? reconstructionElapsedMicroseconds = null) =>
        new(
            ExerciseRunResult.Failed(category, manifest.AssertFailureCategory),
            steps,
            initial,
            final,
            reconstruction,
            new ExerciseCheckResults(checks),
            seedLedger,
            beginElapsedMicroseconds,
            reconstructionElapsedMicroseconds,
            failedDecisions: failedDecisions);

    private static void AppendTerminalFailure(List<ExerciseCheckResult> checks) =>
        checks.Add(ExerciseCheckResult.Failed(
            ExerciseCheckId.TerminalBoundary,
            null,
            null,
            ExerciseCheckFailureCode.TerminalBoundaryNotReached));

    private static ExerciseCheckFailureCode QueryFailure(
        ExerciseCheckpoint checkpoint,
        CampaignActionAudience audience,
        ExerciseRuntimeQueryResult query)
    {
        if (!query.IsSuccessful) return ExerciseCheckFailureCode.AuthorityQueryRejected;
        var set = query.ActionSet!;
        return set.Audience != audience
            || !string.Equals(set.CampaignId, checkpoint.CampaignId, StringComparison.Ordinal)
            || set.StateVersion != checkpoint.StateVersion
            || !string.Equals(set.RulesetHash, checkpoint.RulesetHash, StringComparison.Ordinal)
            || !string.Equals(set.PositionId, checkpoint.PositionId, StringComparison.Ordinal)
            ? ExerciseCheckFailureCode.AuthorityQueryCoordinateMismatch
            : ExerciseCheckFailureCode.None;
    }

    private static ExerciseCheckFailureCode ContinuityFailure(
        ExerciseCheckpoint prior,
        ExerciseCheckpoint committed,
        CampaignActionAcceptanceReceipt receipt)
    {
        if (!string.Equals(prior.CampaignId, receipt.CampaignId, StringComparison.Ordinal)
            || !string.Equals(committed.CampaignId, receipt.CampaignId, StringComparison.Ordinal))
            return ExerciseCheckFailureCode.CampaignMismatch;
        if (!string.Equals(prior.RulesetHash, committed.RulesetHash, StringComparison.Ordinal))
            return ExerciseCheckFailureCode.RulesetMismatch;
        if (receipt.PriorStateVersion != prior.StateVersion
            || receipt.CommittedStateVersion != checked(prior.StateVersion + 1)
            || committed.StateVersion != receipt.CommittedStateVersion)
            return ExerciseCheckFailureCode.StateVersionDiscontinuity;
        return string.Equals(
            committed.PositionId,
            receipt.ResultingPositionId,
            StringComparison.Ordinal)
            ? ExerciseCheckFailureCode.None
            : ExerciseCheckFailureCode.PositionMismatch;
    }

    private static long ElapsedMicroseconds(long started) =>
        Math.Max(0, Stopwatch.GetElapsedTime(started).Ticks / 10);
}
