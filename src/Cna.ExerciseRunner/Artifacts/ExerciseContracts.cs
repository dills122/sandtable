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
        RequireStableId(exerciseId, nameof(exerciseId));
        RequireStableId(setupId, nameof(setupId));
        RequireSha256(setupHash, nameof(setupHash));
        RequireStableId(contentPackId, nameof(contentPackId));
        RequireSha256(contentHash, nameof(contentHash));
        RequireStableId(scenarioId, nameof(scenarioId));
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
            throw new ArgumentException("The ruleset hash is unsupported.", nameof(rulesetHash));
        RequireStableId(terminalBoundary, nameof(terminalBoundary));
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

    private static void RequireStableId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!IsAsciiLowerOrDigit(value[0]) || !IsAsciiLowerOrDigit(value[^1]))
            throw new ArgumentException(
                "A stable ID must begin and end with a lowercase ASCII letter or digit.",
                parameterName);

        var previousWasSeparator = false;
        foreach (var character in value)
        {
            if (IsAsciiLowerOrDigit(character))
            {
                previousWasSeparator = false;
                continue;
            }
            if (character is '-' or '.' && !previousWasSeparator)
            {
                previousWasSeparator = true;
                continue;
            }
            throw new ArgumentException(
                "A stable ID must use lowercase ASCII letters, digits, and nonadjacent separators.",
                parameterName);
        }
    }

    private static void RequireSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 71 || !value.StartsWith("sha256:", StringComparison.Ordinal))
            throw new ArgumentException(
                "A SHA-256 value must contain 64 lowercase hexadecimal digits.",
                parameterName);
        foreach (var character in value.AsSpan(7))
        {
            if (character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
                throw new ArgumentException(
                    "A SHA-256 value must contain 64 lowercase hexadecimal digits.",
                    parameterName);
        }
    }

    private static bool IsAsciiLowerOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';
}
