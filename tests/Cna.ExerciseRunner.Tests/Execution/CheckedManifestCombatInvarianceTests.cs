using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

// Task 023A (owner ruling 2026-11-21: Combat always on): making the Runner's runtime Combat-aware
// must not change any checked manifest. Every checked Exercise and Maneuver child is executed with
// the default (Combat-aware) runtime and replayed step by step through the ordinary pre-023
// CampaignExercises.Query/Submit surface; every accepted action, event, receipt and snapshot
// checkpoint must be identical, so every checked bundle is byte-identical to before.
public sealed class CheckedManifestCombatInvarianceTests
{
    [Fact]
    public void EveryCheckedManifestRunsIdenticallyOnTheOrdinarySurface()
    {
        var root = RepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "scenarios", "exercises"), "*.json")
            .Concat(Directory.GetFiles(Path.Combine(root, "scenarios", "maneuvers"), "*.json"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.True(files.Length >= 29);
        var compared = 0;
        foreach (var file in files)
        {
            IReadOnlyList<(Cna.ExerciseRunner.Artifacts.ExerciseManifest Manifest, ExerciseRunIdentity Identity)> cases;
            try { cases = BreakdownFixtureMigrationTests.ReadCases(Path.GetRelativePath(root, file)); }
            catch (Exception exception) when (exception is ArgumentException or FormatException
                or System.Text.Json.JsonException or InvalidDataException)
            {
                continue; // Frozen historical fixtures are rejected before execution on every runtime.
            }
            foreach (var (manifest, identity) in cases)
            {
                var result = ExerciseExecutor.Execute(manifest, identity, TestContext.Current.CancellationToken);
                var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest, identity));
                if (!start.IsStarted)
                {
                    Assert.Empty(result.Steps);
                    continue;
                }
                var session = start.Session!;
                foreach (var step in result.Steps)
                {
                    var set = CampaignExercises.Query(session, step.Audience).ActionSet!;
                    Assert.Contains(set.Candidates, candidate => candidate.ActionId == step.ActionId);
                    var ordinary = CampaignExercises.Submit(session, new CampaignActionSubmission(
                        CampaignActionSubmission.CurrentContractVersion, set.CampaignId, set.StateVersion,
                        set.PositionId, set.Audience, step.ActionId));
                    Assert.True(ordinary.IsAccepted, $"{file}: {manifest.ExerciseId} step {step.Ordinal}");
                    Assert.Equal(step.EventRecords, ordinary.Evidence!.EventRecords);
                    Assert.Equal(step.SnapshotCheckpoint, ordinary.Evidence.SnapshotCheckpoint);
                    Assert.Equal(step.Receipt, ordinary.Evidence.Receipt);
                    session = ordinary.SuccessorSession!;
                }
                compared++;
            }
        }
        Assert.True(compared > 40, $"compared {compared}");
    }

    private static string RepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidOperationException("The repository root was not found.");
    }
}
