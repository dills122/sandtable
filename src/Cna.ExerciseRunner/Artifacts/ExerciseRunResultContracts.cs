namespace Cna.ExerciseRunner.Artifacts;

public abstract record ExerciseTerminalOutcome
{
    private protected ExerciseTerminalOutcome() { }
}

public sealed record BoundaryReached : ExerciseTerminalOutcome
{
    public BoundaryReached(string positionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positionId);
        PositionId = positionId;
    }

    public string PositionId { get; }
}

public sealed record VictoryReached : ExerciseTerminalOutcome
{
    public VictoryReached(string victor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(victor);
        Victor = victor;
    }

    public string Victor { get; }
}

public abstract record ExerciseCompletion
{
    private protected ExerciseCompletion() { }
}

public sealed record ExerciseSucceeded : ExerciseCompletion
{
    public ExerciseSucceeded(ExerciseTerminalOutcome outcome) =>
        Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));

    public ExerciseTerminalOutcome Outcome { get; }
}

public sealed record ExerciseFailure
{
    public ExerciseFailure(ExerciseFailureCategory category)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        Category = category;
    }

    public ExerciseFailureCategory Category { get; }
}

public sealed record ExerciseFailed : ExerciseCompletion
{
    public ExerciseFailed(ExerciseFailure failure) =>
        Failure = failure ?? throw new ArgumentNullException(nameof(failure));

    public ExerciseFailure Failure { get; }
}

public sealed record ExerciseFailureAssertion
{
    internal ExerciseFailureAssertion(
        ExerciseFailureCategory expectedCategory,
        ExerciseFailureCategory actualCategory)
    {
        if (!Enum.IsDefined(expectedCategory))
            throw new ArgumentOutOfRangeException(nameof(expectedCategory));
        if (!Enum.IsDefined(actualCategory))
            throw new ArgumentOutOfRangeException(nameof(actualCategory));
        ExpectedCategory = expectedCategory;
        Matches = expectedCategory == actualCategory;
    }

    public ExerciseFailureCategory ExpectedCategory { get; }
    public bool Matches { get; }
}

public sealed record ExerciseRunResult
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.exercise-result.v1";

    private ExerciseRunResult(
        ExerciseCompletion completion,
        ExerciseFailureAssertion? failureAssertion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if (completion is ExerciseSucceeded && failureAssertion is not null)
            throw new ArgumentException(
                "A successful completion cannot carry a failure assertion result.",
                nameof(failureAssertion));
        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        Completion = completion;
        FailureAssertion = failureAssertion;
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ExerciseCompletion Completion { get; }
    public ExerciseFailureAssertion? FailureAssertion { get; }

    public static ExerciseRunResult Succeeded(ExerciseTerminalOutcome outcome) =>
        new(new ExerciseSucceeded(outcome), null);

    public static ExerciseRunResult Failed(
        ExerciseFailureCategory category,
        ExerciseFailureCategory? expectedCategory) =>
        new(
            new ExerciseFailed(new ExerciseFailure(category)),
            expectedCategory.HasValue
                ? new ExerciseFailureAssertion(expectedCategory.Value, category)
                : null);
}

public enum ExerciseProcessExitCode
{
    Succeeded = 0,
    ManifestInvalid = 2,
    BuildIdentityUnavailable = 3,
    ControllerFailed = 4,
    NoUniqueLegalAction = 5,
    IllegalAction = 6,
    InvariantFailed = 7,
    ReconstructionMismatch = 8,
    ReadjudicationMismatch = 9,
    StepLimitExceeded = 10,
    ArtifactFailed = 11,
    UnexpectedFailure = 12,
    Cancelled = 130,
}

public static class ExerciseExitCodeMapper
{
    public static ExerciseProcessExitCode Map(ExerciseRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Completion switch
        {
            ExerciseSucceeded => ExerciseProcessExitCode.Succeeded,
            ExerciseFailed { Failure.Category: var category } => category switch
            {
                ExerciseFailureCategory.ManifestInvalid => ExerciseProcessExitCode.ManifestInvalid,
                ExerciseFailureCategory.BuildIdentityUnavailable =>
                    ExerciseProcessExitCode.BuildIdentityUnavailable,
                ExerciseFailureCategory.ControllerFailed => ExerciseProcessExitCode.ControllerFailed,
                ExerciseFailureCategory.NoUniqueLegalAction =>
                    ExerciseProcessExitCode.NoUniqueLegalAction,
                ExerciseFailureCategory.IllegalAction => ExerciseProcessExitCode.IllegalAction,
                ExerciseFailureCategory.InvariantFailed => ExerciseProcessExitCode.InvariantFailed,
                ExerciseFailureCategory.ReconstructionMismatch =>
                    ExerciseProcessExitCode.ReconstructionMismatch,
                ExerciseFailureCategory.ReadjudicationMismatch =>
                    ExerciseProcessExitCode.ReadjudicationMismatch,
                ExerciseFailureCategory.StepLimitExceeded =>
                    ExerciseProcessExitCode.StepLimitExceeded,
                ExerciseFailureCategory.Cancelled => ExerciseProcessExitCode.Cancelled,
                ExerciseFailureCategory.ArtifactFailed => ExerciseProcessExitCode.ArtifactFailed,
                ExerciseFailureCategory.UnexpectedFailure =>
                    ExerciseProcessExitCode.UnexpectedFailure,
                _ => throw new ArgumentOutOfRangeException(nameof(result)),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
    }
}
