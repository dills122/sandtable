using Cna.Core.Rules;

namespace Cna.ExerciseRunner.Artifacts;

public enum ExerciseBuildMode
{
    Baseline,
    Exploratory,
}

public enum ExerciseConfidentiality
{
    TrustedAuthority,
}

public enum ExerciseDetail
{
    Compact,
    Forensic,
    Debug,
}

public enum ExerciseControllerPolicy
{
    FirstByActionId,
    DesignateAllReservesThenFirstByActionId,
    ActFirstReserveNoneThenFirstByActionId,
    ActFirstReserveOneThenFirstByActionId,
    ActFirstReserveAllThenFirstByActionId,
    ActLastReserveNoneThenFirstByActionId,
    ActLastReserveOneThenFirstByActionId,
    ActLastReserveAllThenFirstByActionId,
}

public enum ExerciseFailureCategory
{
    ManifestInvalid,
    BuildIdentityUnavailable,
    ControllerFailed,
    NoUniqueLegalAction,
    IllegalAction,
    InvariantFailed,
    ReconstructionMismatch,
    ReadjudicationMismatch,
    StepLimitExceeded,
    Cancelled,
    ArtifactFailed,
    UnexpectedFailure,
}

public sealed record ExerciseControllerManifest(
    ExerciseControllerPolicy System,
    ExerciseControllerPolicy Axis,
    ExerciseControllerPolicy Commonwealth);

public sealed record ExerciseManifest
{
    public const int CurrentContractVersion = 2;

    public ExerciseManifest(
        int contractVersion,
        string exerciseId,
        string setupId,
        string setupHash,
        string contentPackId,
        string contentHash,
        string scenarioId,
        string rulesetHash,
        string terminalBoundary,
        int maximumSteps,
        ulong rootSeed,
        ExerciseBuildMode buildMode,
        ExerciseConfidentiality confidentiality,
        ExerciseDetail detail,
        ExerciseControllerManifest controllers,
        ExerciseFailureCategory? assertFailureCategory)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        StableIdValidation.Require(exerciseId, nameof(exerciseId));
        StableIdValidation.Require(setupId, nameof(setupId));
        ReplayProofValidation.RequireSha256(setupHash, nameof(setupHash));
        StableIdValidation.Require(contentPackId, nameof(contentPackId));
        ReplayProofValidation.RequireSha256(contentHash, nameof(contentHash));
        StableIdValidation.Require(scenarioId, nameof(scenarioId));
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
            throw new ArgumentException("The ruleset hash is unsupported.", nameof(rulesetHash));
        StableIdValidation.Require(terminalBoundary, nameof(terminalBoundary));
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumSteps, 1);
        if (!Enum.IsDefined(buildMode)) throw new ArgumentOutOfRangeException(nameof(buildMode));
        if (!Enum.IsDefined(confidentiality))
            throw new ArgumentOutOfRangeException(nameof(confidentiality));
        if (!Enum.IsDefined(detail)) throw new ArgumentOutOfRangeException(nameof(detail));
        ArgumentNullException.ThrowIfNull(controllers);
        if (!Enum.IsDefined(controllers.System)
            || !Enum.IsDefined(controllers.Axis)
            || !Enum.IsDefined(controllers.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(controllers));
        if (assertFailureCategory.HasValue && !Enum.IsDefined(assertFailureCategory.Value))
            throw new ArgumentOutOfRangeException(nameof(assertFailureCategory));

        ContractVersion = contractVersion;
        ExerciseId = exerciseId;
        SetupId = setupId;
        SetupHash = setupHash;
        ContentPackId = contentPackId;
        ContentHash = contentHash;
        ScenarioId = scenarioId;
        RulesetHash = rulesetHash;
        TerminalBoundary = terminalBoundary;
        MaximumSteps = maximumSteps;
        RootSeed = rootSeed;
        BuildMode = buildMode;
        Confidentiality = confidentiality;
        Detail = detail;
        Controllers = controllers;
        AssertFailureCategory = assertFailureCategory;
    }

    public int ContractVersion { get; }
    public string ExerciseId { get; }
    public string SetupId { get; }
    public string SetupHash { get; }
    public string ContentPackId { get; }
    public string ContentHash { get; }
    public string ScenarioId { get; }
    public string RulesetHash { get; }
    public string TerminalBoundary { get; }
    public int MaximumSteps { get; }
    public ulong RootSeed { get; }
    public ExerciseBuildMode BuildMode { get; }
    public ExerciseConfidentiality Confidentiality { get; }
    public ExerciseDetail Detail { get; }
    public ExerciseControllerManifest Controllers { get; }
    public ExerciseFailureCategory? AssertFailureCategory { get; }
}
