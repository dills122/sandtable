namespace Cna.ExerciseRunner.Artifacts;

public enum PairedManeuverMode
{
    SerialPaired,
}

public sealed record PairedManeuverPairManifest
{
    public const int CurrentContractVersion = 1;

    public PairedManeuverPairManifest(
        int contractVersion,
        string pairKey,
        int repetition,
        ManeuverExerciseManifest baseline,
        ManeuverExerciseManifest candidate)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        StableIdValidation.Require(pairKey, nameof(pairKey));
        ArgumentOutOfRangeException.ThrowIfNegative(repetition);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        if (string.Equals(
                baseline.ExerciseId,
                candidate.ExerciseId,
                StringComparison.Ordinal))
            throw new ArgumentException("Paired arm Exercise IDs must be distinct.");
        RequireEqualDeclaredInputs(baseline, candidate);

        ContractVersion = contractVersion;
        PairKey = pairKey;
        Repetition = repetition;
        Baseline = baseline;
        Candidate = candidate;
    }

    public int ContractVersion { get; }
    public string PairKey { get; }
    public int Repetition { get; }
    public ManeuverExerciseManifest Baseline { get; }
    public ManeuverExerciseManifest Candidate { get; }

    public ExerciseManifest MaterializeBaseline(ulong rootSeed) =>
        Baseline.Materialize(rootSeed);

    public ExerciseManifest MaterializeCandidate(ulong rootSeed) =>
        Candidate.Materialize(rootSeed);

    private static void RequireEqualDeclaredInputs(
        ManeuverExerciseManifest baseline,
        ManeuverExerciseManifest candidate)
    {
        if (baseline.ContractVersion != candidate.ContractVersion
            || !string.Equals(baseline.SetupId, candidate.SetupId, StringComparison.Ordinal)
            || !string.Equals(baseline.SetupHash, candidate.SetupHash, StringComparison.Ordinal)
            || !string.Equals(
                baseline.ContentPackId,
                candidate.ContentPackId,
                StringComparison.Ordinal)
            || !string.Equals(
                baseline.ContentHash,
                candidate.ContentHash,
                StringComparison.Ordinal)
            || !string.Equals(
                baseline.ScenarioId,
                candidate.ScenarioId,
                StringComparison.Ordinal)
            || !string.Equals(
                baseline.RulesetHash,
                candidate.RulesetHash,
                StringComparison.Ordinal)
            || !string.Equals(
                baseline.TerminalBoundary,
                candidate.TerminalBoundary,
                StringComparison.Ordinal)
            || baseline.MaximumSteps != candidate.MaximumSteps
            || baseline.BuildMode != candidate.BuildMode
            || baseline.Confidentiality != candidate.Confidentiality
            || baseline.Detail != candidate.Detail
            || baseline.AssertFailureCategory != candidate.AssertFailureCategory)
            throw new ArgumentException(
                "Paired arms must have equal declared inputs outside Exercise and controller identity.");
    }
}

public sealed class PairedManeuverManifest
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.paired-maneuver-manifest.v1";

    public PairedManeuverManifest(
        int contractVersion,
        string schemeId,
        string maneuverId,
        PairedManeuverMode mode,
        ulong rootSeed,
        ManeuverReportOptions report,
        IEnumerable<PairedManeuverPairManifest> pairs)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        if (!string.Equals(schemeId, SchemeId, StringComparison.Ordinal))
            throw new ArgumentException("The paired Maneuver scheme is unsupported.", nameof(schemeId));
        StableIdValidation.Require(maneuverId, nameof(maneuverId));
        if (maneuverId.StartsWith(
                ManeuverManifest.ReservedStandalonePrefix,
                StringComparison.Ordinal))
            throw new ArgumentException(
                "The standalone Maneuver-ID namespace is reserved.",
                nameof(maneuverId));
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(pairs);
        var copy = pairs.ToArray();
        if (copy.Length == 0 || copy.Any(value => value is null))
            throw new ArgumentException(
                "A paired Maneuver must contain at least one non-null pair.",
                nameof(pairs));

        RequireCanonicalPairIdentities(copy);
        var exerciseIds = copy.SelectMany(value => new[]
        {
            value.Baseline.ExerciseId,
            value.Candidate.ExerciseId,
        }).ToArray();
        if (exerciseIds.Distinct(StringComparer.Ordinal).Count() != exerciseIds.Length)
            throw new ArgumentException(
                "Paired Maneuver Exercise IDs must be globally unique.",
                nameof(pairs));

        ContractVersion = contractVersion;
        ContractSchemeId = schemeId;
        ManeuverId = maneuverId;
        Mode = mode;
        RootSeed = rootSeed;
        Report = report;
        Pairs = Array.AsReadOnly(copy);
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public string ManeuverId { get; }
    public PairedManeuverMode Mode { get; }
    public ulong RootSeed { get; }
    public ManeuverReportOptions Report { get; }
    public IReadOnlyList<PairedManeuverPairManifest> Pairs { get; }
    public int ExerciseCount => checked(Pairs.Count * 2);

    private static void RequireCanonicalPairIdentities(
        PairedManeuverPairManifest[] pairs)
    {
        var nextRepetition = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            var expected = nextRepetition.GetValueOrDefault(pair.PairKey);
            if (pair.Repetition != expected)
                throw new ArgumentException(
                    "Pair repetitions must be contiguous and zero-based in manifest order.",
                    nameof(pairs));
            nextRepetition[pair.PairKey] = checked(expected + 1);
        }
    }
}
