using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
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

    [Fact]
    public void ANaturallyFailedNoUniqueLegalActionRunProducesAStrictlyReadableBundle()
    {
        // Regression for a second-side-combat simulation sweep finding: pushing the checked
        // Reaction/adjacent-contact fixture's terminal boundary past first-side Breakdown and
        // Combat into the second player's own Combat position-determination (never previously
        // exercised -- every checked manifest stops at or before first-side Breakdown) runs out
        // of legal actions for every audience once the Runner's generic first-by-action-id
        // fallback outruns the fixture's content before the second side acts, producing a
        // genuine failed-executed/no-unique-legal-action run. The strict semantic reader must
        // still accept that bundle: it previously rejected every real no-unique-legal-action
        // (and controller-failed/illegal-action) failed-executed bundle because its validity
        // check compared the terminal decision check's StepOrdinal/Audience against defaulted
        // nulls that no genuine decision check ever has.
        var manifest = new ExerciseManifest(
            ExerciseManifest.CurrentContractVersion,
            "second-side-combat.no-unique-legal-action-regression",
            "rules-lab.breakdown.reaction.adjacent.v1",
            "sha256:977695c69112f752fddc2f2ca48da71d9d8b4aa38c8c589ff4370044ca61a200",
            "rules-lab.content.breakdown-reaction.adjacent.v1",
            "sha256:8dc4a1fa9aa6fc244eefe3343d473f6ea547e76ca2f1035461e0996f2bd3a56b",
            "breakdown-reaction-adjacent",
            Cna.Core.Rules.Cna1979Ruleset.Manifest.Hash,
            "land.position.operation-1.second-player.movement-and-combat.combat.position-determination",
            40,
            0,
            ExerciseBuildMode.Exploratory,
            ExerciseConfidentiality.TrustedAuthority,
            ExerciseDetail.Forensic,
            new ExerciseControllerManifest(
                ExerciseControllerPolicy.ReactionAllByActionId,
                ExerciseControllerPolicy.ReactionAllByActionId,
                ExerciseControllerPolicy.ReactionAllByActionId),
            null);
        var identity = ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed);

        var result = ExerciseRunCoordinator.Execute(Request(
            manifest,
            identity,
            TestContext.Current.CancellationToken));

        Assert.Equal(ExerciseProcessExitCode.NoUniqueLegalAction, result.ExitCode);
        var bundle = ExerciseBundleReader.Read(Assert.IsType<string>(result.CompletedBundlePath));
        Assert.Equal(ArtifactBundleProfile.FailedExecuted, bundle.Manifest.Profile);
        var failed = Assert.IsType<ExerciseFailed>(bundle.RunResult.Completion);
        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, failed.Failure.Category);
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
