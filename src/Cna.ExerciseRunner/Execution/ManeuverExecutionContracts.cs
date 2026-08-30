using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal sealed class ManeuverExecutionDependencies
{
    internal ManeuverExecutionDependencies(
        Func<ExerciseRunCoordinatorRequest, ExerciseRunCoordinatorResult> runChild,
        Func<string, ManeuverChildBundleView> readChildBundle)
    {
        RunChild = runChild ?? throw new ArgumentNullException(nameof(runChild));
        ReadChildBundle = readChildBundle
            ?? throw new ArgumentNullException(nameof(readChildBundle));
    }

    internal Func<ExerciseRunCoordinatorRequest, ExerciseRunCoordinatorResult> RunChild { get; }
    internal Func<string, ManeuverChildBundleView> ReadChildBundle { get; }

    internal static ManeuverExecutionDependencies Default { get; } = new(
        ExerciseRunCoordinator.Execute,
        path => ManeuverChildBundleView.From(ExerciseBundleReader.Read(path)));
}

internal sealed class ManeuverChildBundleView
{
    private readonly byte[]? normalizedManifestBytes;
    private readonly byte[]? initialSnapshotBytes;

    internal ManeuverChildBundleView(
        string path,
        ArtifactBundleProfile profile,
        string artifactManifestSha256,
        byte[]? normalizedManifestBytes,
        BuildIdentity? buildIdentity,
        ExerciseSeedLedger? seedLedger,
        ExerciseRunResult runResult,
        ExerciseCheckResults checkResults,
        int acceptedStepCount,
        IEnumerable<ExerciseAcceptedActionRecord>? acceptedActions = null,
        byte[]? initialSnapshotBytes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        ReplayProofValidation.RequireSha256(
            artifactManifestSha256,
            nameof(artifactManifestSha256));
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(checkResults);
        ArgumentOutOfRangeException.ThrowIfNegative(acceptedStepCount);

        Path = path;
        Profile = profile;
        ArtifactManifestSha256 = artifactManifestSha256;
        this.normalizedManifestBytes = normalizedManifestBytes?.ToArray();
        BuildIdentity = buildIdentity;
        SeedLedger = seedLedger;
        RunResult = runResult;
        CheckResults = checkResults;
        AcceptedStepCount = acceptedStepCount;
        AcceptedActions = Array.AsReadOnly((acceptedActions ?? []).ToArray());
        if (acceptedActions is not null && AcceptedActions.Count != acceptedStepCount)
            throw new ArgumentException(
                "Accepted-action evidence must match the retained count.",
                nameof(acceptedActions));
        this.initialSnapshotBytes = initialSnapshotBytes?.ToArray();
    }

    internal string Path { get; }
    internal ArtifactBundleProfile Profile { get; }
    internal string ArtifactManifestSha256 { get; }
    internal byte[]? NormalizedManifestBytes => normalizedManifestBytes?.ToArray();
    internal BuildIdentity? BuildIdentity { get; }
    internal ExerciseSeedLedger? SeedLedger { get; }
    internal ExerciseRunResult RunResult { get; }
    internal ExerciseCheckResults CheckResults { get; }
    internal int AcceptedStepCount { get; }
    internal IReadOnlyList<ExerciseAcceptedActionRecord> AcceptedActions { get; }
    internal byte[]? InitialSnapshotBytes => initialSnapshotBytes?.ToArray();

    internal static ManeuverChildBundleView From(ExerciseBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        return new ManeuverChildBundleView(
            bundle.Path,
            bundle.Manifest.Profile,
            bundle.ArtifactManifestSha256,
            bundle.NormalizedManifestBytes,
            bundle.BuildIdentity,
            bundle.SeedLedger,
            bundle.RunResult,
            bundle.CheckResults,
            bundle.AcceptedActions.Count,
            bundle.AcceptedActions,
            bundle.InitialSnapshotBytes);
    }
}
