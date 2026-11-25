using System.Text;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

// L8 investigation (concurrent-simulation plan section 4, issue #431 / #448 triage).
// Drives the battalion-matrix Breakdown setup under first-by-action-id until the Runner stops, then
// compares the final checkpoint bytes the Executor retains with the bytes Core's Reconstruct yields.
// Characterization (green today): it pins the exit-11 ("final checkpoint does not equal Core's reconstruction of the
// canonical events") defect at the second side's Combat no-attack completion. Delete or invert it when
// the Stage 3 chain owner fixes the mismatch. See docs/work/2026-11-25-l8-second-side-reserve-release-exit-11.md.
public sealed class SecondSideReserveReleaseFinalizationTests
{
    private const string SecondReserveRelease =
        "land.position.operation-1.second-player.movement-and-combat.reserve-release";

    private static ExerciseManifest Manifest() =>
        ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes("""
            {"contractVersion":2,"exerciseId":"reserve-boundary.breakdown-v1","setupId":"rules-lab.breakdown.battalion-matrix.v1","setupHash":"sha256:9bbd5cdd437fdc574bc65c5d5a90a1ae130827c5c57084474258db9d8dc7cd7d","contentPackId":"rules-lab.content.breakdown-battalion-matrix.v1","contentHash":"sha256:1be529d7079b836f4d26d920c4bfda0ce95d74169ed9ba0b62baf100cec7efa1","scenarioId":"breakdown-battalion-matrix","rulesetHash":"17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a","terminalBoundary":"land.position.unreachable","maximumSteps":400,"rootSeed":0,"buildMode":"exploratory","confidentiality":"trusted-authority","detail":"compact","controllers":{"system":"first-by-action-id","axis":"first-by-action-id","commonwealth":"first-by-action-id"},"assertFailureCategory":null}
            """));

    [Fact]
    public void RunStopsAtSecondSideReserveReleaseWithNoLegalAction()
    {
        var result = ExerciseExecutor.Execute(Manifest(), TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, result.FailureCategory);
        Assert.Equal(SecondReserveRelease, Assert.Single(result.FailedDecisions).PositionId);
    }

    // KNOWN DEFECT (characterization): flips to failing when the mismatch is fixed. Then invert it to
    // Assert.True(equal) and drop this note. Not failing-by-design in CI because a red row would block
    // every PR; the pin is the Assert.False below.
    [Fact]
    public void KnownDefectFinalCheckpointDiffersFromCoreReconstructionAtTheStop()
    {
        var manifest = Manifest();
        var result = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        var session = Replay(manifest, result);

        var reconstruction = CampaignExercises.Reconstruct(session);

        Assert.True(reconstruction.IsVerified);
        var reconstructed = reconstruction.ReconstructedSnapshotBytes!;
        Assert.False(
            reconstructed.AsSpan().SequenceEqual(result.FinalSnapshot),
            "Mismatch fixed: invert this test. Executor checkpoint: "
            + $"{Head(result.FinalSnapshot)} | Reconstruct: {Head(reconstructed)}");
        Assert.StartsWith("{\"contractVersion\":13,\"eventType\":\"combat-no-attack-completed\"",
            Encoding.UTF8.GetString(reconstructed));
        Assert.Contains("\"priorDecisionState\"", Encoding.UTF8.GetString(result.FinalSnapshot));
    }

    private static string Head(byte[] bytes) => Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 90));

    private static ExerciseSession Replay(ExerciseManifest manifest, ExerciseExecutionResult result)
    {
        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest));
        Assert.True(start.IsStarted);
        var session = start.Session!;
        foreach (var step in result.Steps)
        {
            var set = CampaignExercises.QueryWithCombat(session, step.Audience).ActionSet!;
            var submitted = CampaignExercises.SubmitWithCombat(session, new(
                CampaignActionSubmission.CurrentContractVersion, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, step.ActionId));
            Assert.True(submitted.IsAccepted);
            session = submitted.SuccessorSession!;
        }
        return session;
    }
}
