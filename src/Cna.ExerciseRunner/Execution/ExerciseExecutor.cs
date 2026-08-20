using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Execution;

public sealed class ExerciseAcceptedStep
{
    private readonly byte[][] eventRecords;
    private readonly byte[] snapshotCheckpoint;

    internal ExerciseAcceptedStep(
        int ordinal,
        CampaignActionAcceptanceReceipt receipt,
        IReadOnlyList<byte[]> eventRecords,
        byte[] snapshotCheckpoint)
    {
        Ordinal = ordinal;
        Receipt = receipt;
        this.eventRecords = eventRecords.Select(value => value.ToArray()).ToArray();
        this.snapshotCheckpoint = snapshotCheckpoint.ToArray();
    }

    public int Ordinal { get; }
    public CampaignActionAcceptanceReceipt Receipt { get; }
    public CampaignActionAudience Audience => Receipt.Audience;
    public string ActionId => Receipt.ActionId;
    public IReadOnlyList<byte[]> EventRecords => Array.AsReadOnly(
        eventRecords.Select(value => value.ToArray()).ToArray());
    public byte[] SnapshotCheckpoint => snapshotCheckpoint.ToArray();
}

public sealed class ExerciseExecutionResult
{
    private readonly byte[] initialSnapshot;
    private readonly byte[] finalSnapshot;

    internal ExerciseExecutionResult(
        ExerciseRunResult runResult,
        IEnumerable<ExerciseAcceptedStep> steps,
        byte[] initialSnapshot,
        byte[] finalSnapshot,
        ReconstructionProof? reconstruction,
        ExerciseCheckResults checkResults,
        ExerciseSeedLedger seedLedger)
    {
        RunResult = runResult ?? throw new ArgumentNullException(nameof(runResult));
        Steps = Array.AsReadOnly(steps.ToArray());
        this.initialSnapshot = initialSnapshot.ToArray();
        this.finalSnapshot = finalSnapshot.ToArray();
        Reconstruction = reconstruction;
        CheckResults = checkResults ?? throw new ArgumentNullException(nameof(checkResults));
        SeedLedger = seedLedger ?? throw new ArgumentNullException(nameof(seedLedger));
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
        var identity = ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed);
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
        var start = CampaignExercises.Begin(request);
        if (!start.IsStarted)
            return Failed(
                ExerciseFailureCategory.ManifestInvalid,
                manifest,
                [],
                start.InitialSnapshotBytes ?? [],
                start.InitialSnapshotBytes ?? [],
                checks,
                seedLedger);

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
                    seedLedger);
            }
            var checkpoint = CampaignExercises.QueryCheckpoint(session);
            if (string.Equals(
                    checkpoint.PositionId,
                    manifest.TerminalBoundary,
                    StringComparison.Ordinal))
            {
                checks.Add(ExerciseCheckResult.Passed(
                    ExerciseCheckId.TerminalBoundary,
                    null,
                    null));
                var reconstruction = ReconstructionProof.From(CampaignExercises.Reconstruct(session));
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
                        seedLedger)
                    : Failed(
                        ExerciseFailureCategory.ReconstructionMismatch,
                        manifest,
                        steps,
                        initial,
                        current,
                        checks,
                        seedLedger,
                        reconstruction);
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
                    seedLedger);
            }

            var stepOrdinal = steps.Count;
            var queries = new List<CampaignLegalActionQueryResult>();
            foreach (var audience in AudienceOrder)
            {
                var query = CampaignExercises.Query(session, audience);
                var queryFailure = QueryFailure(checkpoint, audience, query);
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
                    AppendTerminalFailure(checks);
                    return Failed(
                        ExerciseFailureCategory.InvariantFailed,
                        manifest,
                        steps,
                        initial,
                        current,
                        checks,
                        seedLedger);
                }
                queries.Add(query);
            }

            var selection = ExerciseController.Select(
                manifest.Controllers,
                queries.Select(result => new ExerciseControllerActionSet(
                    result.ActionSet!.Audience,
                    result.ActionSet.Candidates.Select(candidate => candidate.ActionId))).ToArray());
            if (selection.FailureReason == ExerciseControllerSelectionFailure.NoActiveAudience)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.ActiveAudienceCardinality,
                    stepOrdinal,
                    null,
                    ExerciseCheckFailureCode.NoActiveAudience));
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.NoUniqueLegalAction,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
            }
            if (selection.FailureReason
                == ExerciseControllerSelectionFailure.MultipleActiveAudiences)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.ActiveAudienceCardinality,
                    stepOrdinal,
                    null,
                    ExerciseCheckFailureCode.MultipleActiveAudiences));
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.InvariantFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
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
                    selection.Audience,
                    ExerciseCheckFailureCode.SelectedActionNotCurrent));
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.ControllerFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
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
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.ControllerFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
            }
            var submission = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                set.CampaignId,
                set.StateVersion,
                set.PositionId,
                set.Audience,
                candidate.ActionId);
            var submitted = CampaignExercises.Submit(session, submission);
            if (!submitted.IsAccepted)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.AcceptedEventCardinality,
                    stepOrdinal,
                    set.Audience,
                    ExerciseCheckFailureCode.ActionRejected));
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.IllegalAction,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
            }
            var evidence = submitted.Evidence!;
            if (evidence.EventRecords.Count != 1)
            {
                checks.Add(ExerciseCheckResult.Failed(
                    ExerciseCheckId.AcceptedEventCardinality,
                    stepOrdinal,
                    set.Audience,
                    ExerciseCheckFailureCode.EventCardinalityMismatch));
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.InvariantFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
            }
            checks.Add(ExerciseCheckResult.Passed(
                ExerciseCheckId.AcceptedEventCardinality,
                stepOrdinal,
                set.Audience));
            var successor = submitted.SuccessorSession!;
            var resultingCheckpoint = CampaignExercises.QueryCheckpoint(successor);
            var continuityFailure = ContinuityFailure(
                checkpoint,
                resultingCheckpoint,
                evidence.Receipt);
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
                AppendTerminalFailure(checks);
                return Failed(
                    ExerciseFailureCategory.InvariantFailed,
                    manifest,
                    steps,
                    initial,
                    current,
                    checks,
                    seedLedger);
            }
            current = evidence.SnapshotCheckpoint;
            steps.Add(new ExerciseAcceptedStep(
                steps.Count,
                evidence.Receipt,
                evidence.EventRecords,
                current));
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
        ReconstructionProof? reconstruction = null) =>
        new(
            ExerciseRunResult.Failed(category, manifest.AssertFailureCategory),
            steps,
            initial,
            final,
            reconstruction,
            new ExerciseCheckResults(checks),
            seedLedger);

    private static void AppendTerminalFailure(List<ExerciseCheckResult> checks) =>
        checks.Add(ExerciseCheckResult.Failed(
            ExerciseCheckId.TerminalBoundary,
            null,
            null,
            ExerciseCheckFailureCode.TerminalBoundaryNotReached));

    private static ExerciseCheckFailureCode QueryFailure(
        ExerciseCheckpoint checkpoint,
        CampaignActionAudience audience,
        CampaignLegalActionQueryResult query)
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
}
