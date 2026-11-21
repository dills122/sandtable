using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

// Task 023A (#448; owner ruling 2026-11-21: Harness v1 interim amendment, Combat always on). A real
// `exercise run` CLI invocation carries a run through first-cycle Combat and publishes a strictly
// readable bundle whose final checkpoint is a Combat state document. That checkpoint is trusted only
// if Core's replay of the bundle's own canonical events reproduces it byte for byte.
// Test-harness capability only: not Combat activation or certification.
public sealed class CombatBundleReadbackTests : IDisposable
{
    private const string TruckConvoy = "land.position.operation-1.first-player.truck-convoy-movement";
    private const string CloseAssault =
        "land.position.operation-1.first-player.movement-and-combat.combat.close-assault";

    private const string DoubleFresh =
        "\"setupId\":\"rules-lab.breakdown.reaction.combat-double-fresh.v1\",\"setupHash\":\"sha256:d02a9996e2c87b4f5feda75bd0b6cf4a6756b0abbb83abb36ffe037e6fac8b5d\",\"contentPackId\":\"rules-lab.content.breakdown-reaction.combat-double-fresh.v1\",\"contentHash\":\"sha256:44a76f21f1b492493c898495721f5f684acc83a14c391289c5e8d66220b6a648\",\"scenarioId\":\"breakdown-reaction-combat-double-fresh\"";

    private const string Assault =
        "\"setupId\":\"rules-lab.breakdown.reaction.combat-assault.v1\",\"setupHash\":\"sha256:69db7f2c961f88f5469627a9e82f76c6a8b85330aa48a94ee3eaf0d22d18bbb3\",\"contentPackId\":\"rules-lab.content.breakdown-reaction.combat-assault.v1\",\"contentHash\":\"sha256:d2d569a3f04f4915108e2220ac27ddfc25c97fcc047a4d4a67b496a6d62ca19c\",\"scenarioId\":\"breakdown-reaction-combat-assault\"";

    private readonly string temp = Path.Combine(Path.GetTempPath(), $"sandtable-combat-bundle-{Guid.NewGuid():N}");
    private readonly string repositoryManifestDirectory = Path.Combine(
        ".planning", "combat-bundle-tests", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("double-fresh", 5ul)]
    [InlineData("assault", 6ul)]
    public void CliPublishesAStrictlyReadableBundleForAFullFirstCycleLoop(string setup, ulong seed)
    {
        var manifest = Manifest(setup, seed, TruckConvoy);
        var (exitCode, bundlePath) = RunCli(manifest, $"{setup}-{seed}");

        Assert.Equal(ExerciseProcessExitCode.Succeeded, exitCode);
        var bundle = ExerciseBundleReader.Read(bundlePath!);
        Assert.Equal(ArtifactBundleStatus.Succeeded, bundle.Manifest.Status);
        Assert.Equal(ArtifactBundleProfile.Succeeded, bundle.Manifest.Profile);
        var completion = Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
        Assert.Equal(TruckConvoy, Assert.IsType<BoundaryReached>(completion.Outcome).PositionId);

        // The published final checkpoint is a Combat state document (no V11 decode), equal to the
        // executor's own final checkpoint.
        var final = File.ReadAllBytes(Path.Combine(bundlePath!, ArtifactSchema.FinalSnapshotPath));
        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeSnapshot(final));
        var execution = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        Assert.Equal(execution.FinalSnapshot, final);
        Assert.Contains(EventTypes(bundlePath!), type => type == "movement-combat-cycle-control");
    }

    [Fact]
    public void CliPublishesAReadableFailedExecutedBundleAtACombatStoppingPoint()
    {
        // Seed 7's result requires a post-assault retreat, a contract-v2 stage the harness does not
        // cover: the run fails no-unique-legal-action at the result, after real Combat steps.
        var (exitCode, bundlePath) = RunCli(Manifest("double-fresh", 7, "land.position.unreachable"), "stop");

        Assert.Equal(ExerciseProcessExitCode.NoUniqueLegalAction, exitCode);
        var bundle = ExerciseBundleReader.Read(bundlePath!);
        Assert.Equal(ArtifactBundleProfile.FailedExecuted, bundle.Manifest.Profile);
        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction,
            Assert.IsType<ExerciseFailed>(bundle.RunResult.Completion).Failure.Category);
        Assert.Equal("combat-assault-resolved", EventTypes(bundlePath!)[^1]);
        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeSnapshot(
            File.ReadAllBytes(Path.Combine(bundlePath!, ArtifactSchema.FinalSnapshotPath))));
    }

    [Fact]
    public void ACombatFinalCheckpointIsTrustedOnlyWhenCoreReplayReproducesIt()
    {
        var manifest = Manifest("double-fresh", 5, TruckConvoy);
        var execution = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        Assert.True(execution.IsSucceeded);
        var identity = execution.SeedLedger.Identity;
        var events = ExerciseEvidenceCodec.DeserializeCanonicalEvents(ExerciseEvidenceWriter.WriteCanonicalEvents(execution));
        var final = execution.FinalSnapshot;

        var facts = ExerciseBundleSemanticValidator.DeserializeFinalCheckpoint(manifest, identity, events, final);
        Assert.Equal(TruckConvoy, facts.PositionId);
        Assert.Equal(execution.Steps[^1].Receipt.CommittedStateVersion, facts.StateVersion);

        var flipped = final.ToArray();
        var digit = Array.FindLastIndex(flipped, value => value is >= (byte)'0' and <= (byte)'8');
        flipped[digit]++;
        var penultimate = execution.Steps[^2].SnapshotCheckpoint;
        var withoutLast = events.Take(events.Count - 1).ToArray();
        (IReadOnlyList<ExerciseCanonicalEventRecord> Events, byte[] Final)[] tampered =
        [
            (events, flipped),
            (events, penultimate),
            (withoutLast, final),
            (events.Reverse().ToArray(), final),
        ];
        Assert.All(tampered, value => Assert.Throws<InvalidDataException>(() =>
            ExerciseBundleSemanticValidator.DeserializeFinalCheckpoint(manifest, identity, value.Events, value.Final)));
    }

    [Fact]
    public void RehashedTamperedCombatFinalCheckpointIsRejectedByTheReader()
    {
        var (_, bundlePath) = RunCli(Manifest("double-fresh", 5, TruckConvoy), "tamper");
        var finalPath = Path.Combine(bundlePath!, ArtifactSchema.FinalSnapshotPath);
        var bytes = File.ReadAllBytes(finalPath);
        var digit = Array.FindLastIndex(bytes, value => value is >= (byte)'0' and <= (byte)'8');
        bytes[digit]++;
        File.WriteAllBytes(finalPath, bytes);
        RehashManifest(bundlePath!);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath!));
    }

    public void Dispose()
    {
        if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        var repositoryDirectory = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory), repositoryManifestDirectory);
        if (Directory.Exists(repositoryDirectory)) Directory.Delete(repositoryDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private (ExerciseProcessExitCode ExitCode, string? BundlePath) RunCli(ExerciseManifest manifest, string name)
    {
        var relative = Path.Combine(repositoryManifestDirectory, $"{name}.json");
        var path = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory), relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, ExerciseManifestCodec.Serialize(manifest));
        var output = new StringWriter();
        var error = new StringWriter();
        var exitCode = ExerciseRunCommand.Execute(
            ["exercise", "run", "--manifest", relative, "--artifact-root", Path.Combine(temp, name)],
            output, error, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Artifact finalization failed", error.ToString(), StringComparison.Ordinal);
        if (exitCode == ExerciseProcessExitCode.Succeeded) Assert.Equal(string.Empty, error.ToString());
        var line = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .SingleOrDefault(value => value.StartsWith("bundle=", StringComparison.Ordinal));
        return (exitCode, line?["bundle=".Length..]);
    }

    private static ExerciseManifest Manifest(string setup, ulong seed, string terminal) =>
        ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes(
            $$"""{"contractVersion":2,"exerciseId":"combat-selection.c",{{(setup == "assault" ? Assault : DoubleFresh)}},"rulesetHash":"17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a","terminalBoundary":"{{terminal}}","maximumSteps":200,"rootSeed":{{seed}},"buildMode":"exploratory","confidentiality":"trusted-authority","detail":"compact","controllers":{"system":"reaction-all-by-action-id","axis":"reaction-all-by-action-id","commonwealth":"reaction-all-by-action-id"},"assertFailureCategory":null}"""));

    private static string[] EventTypes(string bundlePath) =>
        Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(bundlePath, ArtifactSchema.CanonicalEventsPath)))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                using var record = JsonDocument.Parse(line);
                var root = record.RootElement;
                var payload = root.TryGetProperty("event", out var nested) ? nested : root;
                return payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("eventType", out var type)
                    ? type.GetString()!
                    : FindEventType(root);
            })
            .ToArray();

    private static string FindEventType(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("eventType")) return property.Value.GetString()!;
            if (property.Value.ValueKind == JsonValueKind.Object) return FindEventType(property.Value);
        }
        return "";
    }

    private static void RehashManifest(string bundlePath)
    {
        var manifestPath = Path.Combine(bundlePath, ArtifactSchema.ArtifactManifestPath);
        var manifest = ArtifactManifestCodec.Deserialize(File.ReadAllBytes(manifestPath));
        var entries = manifest.Files.Select(entry =>
        {
            var payload = File.ReadAllBytes(Path.Combine(bundlePath, entry.Path));
            return new ArtifactManifestEntry(entry.Path, entry.SchemaId, payload.LongLength,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}");
        });
        File.WriteAllBytes(manifestPath, ArtifactManifestCodec.Serialize(new ArtifactManifest(manifest.Profile, entries)));
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
