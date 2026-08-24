namespace Cna.ExerciseRunner.Artifacts;

public enum ManeuverMode
{
    SerialUnpaired,
}

public enum ManeuverReportProfile
{
    TrustedAuthority,
}

public sealed record ManeuverReportOptions
{
    public ManeuverReportOptions(ManeuverReportProfile profile)
    {
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        Profile = profile;
    }

    public ManeuverReportProfile Profile { get; }
}

public sealed record ManeuverExerciseManifest
{
    public ManeuverExerciseManifest(
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
        ExerciseBuildMode buildMode,
        ExerciseConfidentiality confidentiality,
        ExerciseDetail detail,
        ExerciseControllerManifest controllers,
        ExerciseFailureCategory? assertFailureCategory)
    {
        var validated = new ExerciseManifest(
            contractVersion,
            exerciseId,
            setupId,
            setupHash,
            contentPackId,
            contentHash,
            scenarioId,
            rulesetHash,
            terminalBoundary,
            maximumSteps,
            0,
            buildMode,
            confidentiality,
            detail,
            controllers,
            assertFailureCategory);

        ContractVersion = validated.ContractVersion;
        ExerciseId = validated.ExerciseId;
        SetupId = validated.SetupId;
        SetupHash = validated.SetupHash;
        ContentPackId = validated.ContentPackId;
        ContentHash = validated.ContentHash;
        ScenarioId = validated.ScenarioId;
        RulesetHash = validated.RulesetHash;
        TerminalBoundary = validated.TerminalBoundary;
        MaximumSteps = validated.MaximumSteps;
        BuildMode = validated.BuildMode;
        Confidentiality = validated.Confidentiality;
        Detail = validated.Detail;
        Controllers = validated.Controllers;
        AssertFailureCategory = validated.AssertFailureCategory;
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
    public ExerciseBuildMode BuildMode { get; }
    public ExerciseConfidentiality Confidentiality { get; }
    public ExerciseDetail Detail { get; }
    public ExerciseControllerManifest Controllers { get; }
    public ExerciseFailureCategory? AssertFailureCategory { get; }

    internal ExerciseManifest Materialize(ulong rootSeed) => new(
        ContractVersion,
        ExerciseId,
        SetupId,
        SetupHash,
        ContentPackId,
        ContentHash,
        ScenarioId,
        RulesetHash,
        TerminalBoundary,
        MaximumSteps,
        rootSeed,
        BuildMode,
        Confidentiality,
        Detail,
        Controllers,
        AssertFailureCategory);
}

public sealed class ManeuverManifest
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.maneuver-manifest.v1";
    public const string ReservedStandalonePrefix = "standalone.";

    public ManeuverManifest(
        int contractVersion,
        string schemeId,
        string maneuverId,
        ManeuverMode mode,
        ulong rootSeed,
        ManeuverReportOptions report,
        IEnumerable<ManeuverExerciseManifest> exercises)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        if (!string.Equals(schemeId, SchemeId, StringComparison.Ordinal))
            throw new ArgumentException("The Maneuver scheme is unsupported.", nameof(schemeId));
        RequireStableId(maneuverId, nameof(maneuverId));
        if (maneuverId.StartsWith(ReservedStandalonePrefix, StringComparison.Ordinal))
            throw new ArgumentException(
                "The standalone Maneuver-ID namespace is reserved.",
                nameof(maneuverId));
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(exercises);
        var copy = exercises.ToArray();
        if (copy.Length == 0 || copy.Any(value => value is null))
            throw new ArgumentException(
                "A Maneuver must contain at least one non-null Exercise.",
                nameof(exercises));
        if (copy.Select(value => value.ExerciseId)
            .Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException(
                "Maneuver Exercise IDs must be unique.",
                nameof(exercises));

        ContractVersion = contractVersion;
        ContractSchemeId = schemeId;
        ManeuverId = maneuverId;
        Mode = mode;
        RootSeed = rootSeed;
        Report = report;
        Exercises = Array.AsReadOnly(copy);
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public string ManeuverId { get; }
    public ManeuverMode Mode { get; }
    public ulong RootSeed { get; }
    public ManeuverReportOptions Report { get; }
    public IReadOnlyList<ManeuverExerciseManifest> Exercises { get; }

    public ExerciseManifest MaterializeExercise(int ordinal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ordinal, Exercises.Count);
        return Exercises[ordinal].Materialize(RootSeed);
    }

    public IReadOnlyList<ExerciseManifest> MaterializeExercises() =>
        Array.AsReadOnly(Enumerable.Range(0, Exercises.Count)
            .Select(MaterializeExercise)
            .ToArray());

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

    private static bool IsAsciiLowerOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';
}
