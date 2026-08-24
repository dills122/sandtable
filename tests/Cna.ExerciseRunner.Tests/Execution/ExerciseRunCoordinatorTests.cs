using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseRunCoordinatorTests : IDisposable
{
    private readonly string artifactRoot = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-exercise-coordinator-{Guid.NewGuid():N}");

    [Fact]
    public void AdmittedStandaloneRunReturnsAValidatedBundleWithoutAConsoleBoundary()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var identity = ExerciseRunIdentity.Standalone(
            manifest.ExerciseId,
            manifest.RootSeed);

        var result = ExerciseRunCoordinator.Execute(Request(
            manifest,
            identity,
            TestContext.Current.CancellationToken));

        Assert.Equal(ExerciseProcessExitCode.Succeeded, result.ExitCode);
        Assert.Null(result.FailureMessage);
        Assert.Null(result.ArtifactTrace);
        var bundle = ExerciseBundleReader.Read(Assert.IsType<string>(result.CompletedBundlePath));
        Assert.Equal(ArtifactBundleProfile.Succeeded, bundle.Manifest.Profile);
        Assert.Equal(identity, bundle.SeedLedger!.Identity);
        Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
    }

    [Fact]
    public void AdmittedManeuverIdentityReachesTheSharedFinalizedChildBundle()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            "rules-lab.serial",
            3,
            null);

        var result = ExerciseRunCoordinator.Execute(Request(
            manifest,
            identity,
            TestContext.Current.CancellationToken));

        Assert.Equal(ExerciseProcessExitCode.Succeeded, result.ExitCode);
        var bundle = ExerciseBundleReader.Read(Assert.IsType<string>(result.CompletedBundlePath));
        Assert.Equal(identity, bundle.SeedLedger!.Identity);
        Assert.All(bundle.AcceptedActions, action => Assert.Equal(
            ExerciseCampaignId.Derive(identity),
            action.CampaignId));
    }

    [Fact]
    public void PreExecutionCancellationCannotReturnAnAttributableChildLedger()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            "rules-lab.serial",
            4,
            null);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

#pragma warning disable xUnit1051 // An already-cancelled token is the behavior under test.
        var result = ExerciseRunCoordinator.Execute(Request(
            manifest,
            identity,
            cancellation.Token));
#pragma warning restore xUnit1051

        Assert.Equal(ExerciseProcessExitCode.Cancelled, result.ExitCode);
        Assert.NotNull(result.FailureMessage);
        var bundle = ExerciseBundleReader.Read(Assert.IsType<string>(result.CompletedBundlePath));
        Assert.Equal(ArtifactBundleProfile.FailedIdentified, bundle.Manifest.Profile);
        Assert.Null(bundle.SeedLedger);
    }

    public void Dispose()
    {
        if (Directory.Exists(artifactRoot)) Directory.Delete(artifactRoot, recursive: true);
        GC.SuppressFinalize(this);
    }

    private ExerciseRunCoordinatorRequest Request(
        ExerciseManifest manifest,
        ExerciseRunIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var telemetry = new ExerciseDiagnosticTelemetry();
        telemetry.RecordPhase("manifest-admission", 0);
        return new ExerciseRunCoordinatorRequest(
            manifest,
            ExerciseManifestCodec.Serialize(manifest),
            identity,
            FindRepositoryRoot(AppContext.BaseDirectory),
            artifactRoot,
            telemetry,
            cancellationToken);
    }

    private static string FindRepositoryRoot(string start)
    {
        for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidOperationException("The repository root was not found.");
    }
}
