using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class BreakdownBundleTamperingTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(), $"sandtable-breakdown-bundle-tampering-{Guid.NewGuid():N}");

    [Fact]
    public void GenuineTruckLossBundlePassesStrictReadbackAndFreshReadjudication()
    {
        var fixture = CreateBundle();

        var bundle = ExerciseBundleReader.Read(fixture.Path);

        Assert.True(bundle.ReconstructionProof!.IsVerified);
        Assert.True(bundle.ReadjudicationProof!.IsVerified);
        Assert.True(ReadjudicationVerifier.Verify(fixture.Manifest, fixture.Execution).IsVerified);
    }

    [Theory]
    [InlineData("dice")]
    [InlineData("cursor")]
    [InlineData("fraction")]
    [InlineData("printed-label")]
    [InlineData("bp")]
    [InlineData("lot-location")]
    [InlineData("check-memory")]
    [InlineData("continuation")]
    public void RehashedBreakdownForgeryFailsStrictReadbackAndFreshReadjudication(string mutation)
    {
        var fixture = CreateBundle();
        var forged = TamperAndRehash(fixture, mutation);

        var readjudication = ReadjudicationVerifier.Verify(fixture.Manifest, forged);
        Assert.True(readjudication.TranscriptMatches);
        Assert.True(readjudication.FinalSnapshotMatches);
        Assert.False(readjudication.EventsMatch);
        Assert.False(readjudication.IsVerified);
        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(fixture.Path));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private BundleFixture CreateBundle()
    {
        var template = ExerciseManifestCodecTests.Create(
            maximumSteps: 30,
            terminalBoundary:
                "land.position.operation-1.first-player.movement-and-combat.breakdown-determination",
            controllerPolicy: ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete);
        var manifest = new ExerciseManifest(template.ContractVersion, template.ExerciseId,
            template.SetupId, template.SetupHash, template.ContentPackId, template.ContentHash,
            template.ScenarioId, template.RulesetHash, template.TerminalBoundary, template.MaximumSteps,
            1, template.BuildMode, template.Confidentiality, template.Detail, template.Controllers,
            template.AssertFailureCategory);
        var execution = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        Assert.True(execution.IsSucceeded, Encoding.UTF8.GetString(
            ExerciseRunResultCodec.Serialize(execution.RunResult)));
        Assert.True(execution.Reconstruction!.IsVerified);
        var resolution = execution.Steps.SelectMany(step => step.EventRecords)
            .Select(bytes => JsonNode.Parse(bytes)!)
            .Single(node => node["eventType"]!.GetValue<string>() == "breakdown-stop-resolved"
                && node["createdLots"]!.AsArray().Count > 0);
        Assert.True(resolution["checks"]![0]!["roll"]!["lossCount"]!.GetValue<int>() > 0);
        Assert.Equal("center", resolution["createdLots"]![0]!["locationId"]!.GetValue<string>());
        var proof = ReadjudicationVerifier.Verify(manifest, execution);
        Assert.True(proof.IsVerified);
        Assert.Equal(proof.ExpectedEventsHash, HashRecords(execution.Steps.SelectMany(step => step.EventRecords)));
        var started = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest, execution.SeedLedger.Identity));
        Assert.True(started.IsStarted);
        Assert.Equal(execution.Reconstruction.EventStreamHash,
            Hash(FrameLines(new[] { started.CreationEventBytes! }.Concat(execution.Steps.SelectMany(step => step.EventRecords)))));
        var normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [ArtifactSchema.AcceptedActionsPath] = ExerciseEvidenceWriter.WriteAcceptedActions(execution),
            [ArtifactSchema.BuildIdentityPath] = BuildIdentityCodec.Serialize(BuildIdentityFor(manifest, normalizedManifest)),
            [ArtifactSchema.CanonicalEventsPath] = ExerciseEvidenceWriter.WriteCanonicalEvents(execution),
            [ArtifactSchema.CheckResultsPath] = ExerciseCheckResultsCodec.Serialize(execution.CheckResults.WithReadjudication(proof)),
            [ArtifactSchema.DiagnosticsPath] = [],
            [ArtifactSchema.ExerciseManifestPath] = normalizedManifest,
            [ArtifactSchema.FinalSnapshotPath] = execution.FinalSnapshot,
            [ArtifactSchema.InitialSnapshotPath] = execution.InitialSnapshot,
            [ArtifactSchema.ReadjudicationProofPath] = ReplayProofCodec.Serialize(proof),
            [ArtifactSchema.ReconstructionProofPath] = ReplayProofCodec.Serialize(execution.Reconstruction),
            [ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(execution.RunResult),
            [ArtifactSchema.SeedLedgerPath] = SeedLedgerCodec.Serialize(execution.SeedLedger),
            [ArtifactSchema.StepEvidencePath] = ExerciseEvidenceWriter.WriteStepEvidence(execution),
            [ArtifactSchema.SummaryJsonPath] = "{}"u8.ToArray(),
            [ArtifactSchema.SummaryMarkdownPath] = [],
        };
        Directory.CreateDirectory(root);
        var path = ExerciseBundleWriter.Write(root,
            new ExerciseBundleWriteRequest(ArtifactBundleProfile.Succeeded, payloads)).Path;
        Assert.True(ExerciseBundleReader.Read(path).ReadjudicationProof!.IsVerified);
        return new BundleFixture(path, manifest, execution);
    }

    private static ExerciseExecutionResult TamperAndRehash(BundleFixture fixture, string mutation)
    {
        var changed = false;
        var steps = fixture.Execution.Steps.Select(step =>
        {
            var records = step.EventRecords.Select(bytes =>
            {
                var node = JsonNode.Parse(bytes)!;
                var type = node["eventType"]!.GetValue<string>();
                var target = mutation == "bp"
                    ? type == "element-moved" && node["breakdownAccounting"]!.AsArray().Count > 0
                    : type == "breakdown-stop-resolved" && node["createdLots"]!.AsArray().Count > 0;
                if (changed || !target) return bytes;
                Mutate(node, mutation);
                var forgedBytes = Encoding.UTF8.GetBytes(node.ToJsonString());
                Assert.False(bytes.AsSpan().SequenceEqual(forgedBytes));
                changed = true;
                return forgedBytes;
            }).ToArray();
            return new ExerciseAcceptedStep(step.Ordinal, step.Receipt, records, step.SnapshotCheckpoint);
        }).ToArray();
        Assert.True(changed, $"No genuine event matched mutation {mutation}.");
        var original = fixture.Execution;
        var forged = new ExerciseExecutionResult(original.RunResult, steps,
            original.InitialSnapshot, original.FinalSnapshot, original.Reconstruction,
            original.CheckResults, original.SeedLedger);

        // Attacker writes bytes directly: strict writer admission must remain enabled.
        var events = steps.SelectMany(step => step.EventRecords).ToArray();
        Write(fixture.Path, ArtifactSchema.CanonicalEventsPath, FrameLines(events));
        var stepNodes = Encoding.UTF8.GetString(Read(fixture.Path, ArtifactSchema.StepEvidencePath))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => JsonNode.Parse(line)!).ToArray();
        for (var index = 0; index < steps.Length; index++)
            stepNodes[index]["eventsHash"] = HashRecords(steps[index].EventRecords);
        Write(fixture.Path, ArtifactSchema.StepEvidencePath,
            FrameLines(stepNodes.Select(node => Encoding.UTF8.GetBytes(node.ToJsonString()))));

        var transcriptHash = HashRecords(steps.Select(step =>
            CampaignActionAcceptanceReceiptSerializer.Serialize(step.Receipt)));
        var eventsHash = HashRecords(events);
        var finalHash = Hash(original.FinalSnapshot);
        Write(fixture.Path, ArtifactSchema.ReadjudicationProofPath,
            ReplayProofCodec.Serialize(new ReadjudicationProof(transcriptHash, transcriptHash,
                eventsHash, eventsHash, finalHash, finalHash)));
        var started = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(
            fixture.Manifest, original.SeedLedger.Identity));
        Assert.True(started.IsStarted);
        Write(fixture.Path, ArtifactSchema.ReconstructionProofPath,
            ReplayProofCodec.Serialize(new ReconstructionProof(ExerciseReconstructionFailureReason.None,
                Hash(FrameLines(new[] { started.CreationEventBytes! }.Concat(events))), finalHash, finalHash)));
        RehashManifest(fixture.Path);
        AssertAllHashesMatch(fixture.Path, steps, eventsHash);
        return forged;
    }

    private static void Mutate(JsonNode node, string mutation)
    {
        if (mutation == "bp")
        {
            var accounting = node["breakdownAccounting"]![0]!;
            accounting["delta"]!["numerator"] = accounting["delta"]!["numerator"]!.GetValue<int>() + 1;
            accounting["after"]!["numerator"] = accounting["after"]!["numerator"]!.GetValue<int>() + 1;
            return;
        }
        var check = node["checks"]![0]!;
        var roll = check["roll"]!;
        switch (mutation)
        {
            case "dice":
                roll["firstDie"] = roll["firstDie"]!.GetValue<int>() % 6 + 1;
                roll["coordinate"] = roll["firstDie"]!.GetValue<int>() * 10
                    + roll["secondDie"]!.GetValue<int>();
                break;
            case "cursor":
                node["randomStateAfter"]!["nextByteCursor"] =
                    node["randomStateAfter"]!["nextByteCursor"]!.GetValue<ulong>() + 1;
                break;
            case "fraction":
                roll["fraction"]!["numerator"] = 0;
                roll["fraction"]!["denominator"] = 1;
                break;
            case "printed-label":
                roll["printedLabel"] = 0;
                break;
            case "lot-location":
                node["createdLots"]![0]!["locationId"] = "west";
                break;
            case "check-memory":
                check["highestEffectiveCheckedBandIdAfter"] = "land.breakdown.band.71-plus";
                break;
            case "continuation":
                node["breakdownFlowAfter"] = new JsonObject
                {
                    ["kind"] = "phasing-stop",
                    ["stop"] = node["stop"]!.DeepClone(),
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }

    private static void RehashManifest(string path)
    {
        var manifest = ArtifactManifestCodec.Deserialize(Read(path, ArtifactSchema.ArtifactManifestPath));
        var entries = manifest.Files.Select(entry =>
        {
            var bytes = Read(path, entry.Path);
            return new ArtifactManifestEntry(entry.Path, entry.SchemaId, bytes.LongLength, Hash(bytes));
        });
        Write(path, ArtifactSchema.ArtifactManifestPath,
            ArtifactManifestCodec.Serialize(new ArtifactManifest(manifest.Profile, entries)));
    }

    private static void AssertAllHashesMatch(string path, ExerciseAcceptedStep[] steps, string eventsHash)
    {
        var manifest = ArtifactManifestCodec.Deserialize(Read(path, ArtifactSchema.ArtifactManifestPath));
        foreach (var entry in manifest.Files)
        {
            var bytes = Read(path, entry.Path);
            Assert.Equal(bytes.LongLength, entry.SizeBytes);
            Assert.Equal(Hash(bytes), entry.Sha256);
        }
        var evidence = ExerciseEvidenceCodec.DeserializeStepEvidence(Read(path, ArtifactSchema.StepEvidencePath));
        for (var index = 0; index < steps.Length; index++)
            Assert.Equal(HashRecords(steps[index].EventRecords), evidence[index].EventsHash);
        var proof = ReplayProofCodec.DeserializeReadjudication(Read(path, ArtifactSchema.ReadjudicationProofPath));
        Assert.True(proof.IsVerified);
        Assert.Equal(eventsHash, proof.ExpectedEventsHash);
        Assert.True(ReplayProofCodec.DeserializeReconstruction(Read(path, ArtifactSchema.ReconstructionProofPath)).IsVerified);
    }

    private static byte[] FrameLines(IEnumerable<byte[]> records)
    {
        using var stream = new MemoryStream();
        foreach (var record in records)
        {
            stream.Write(record);
            stream.WriteByte((byte)'\n');
        }
        return stream.ToArray();
    }

    private static string HashRecords(IEnumerable<byte[]> records)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var record in records)
        {
            BinaryPrimitives.WriteInt32BigEndian(length, record.Length);
            hash.AppendData(length);
            hash.AppendData(record);
        }
        return $"sha256:{Convert.ToHexStringLower(hash.GetHashAndReset())}";
    }

    private static string Hash(byte[] bytes) => $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";
    private static byte[] Read(string path, string file) => File.ReadAllBytes(Path.Combine(path, file));
    private static void Write(string path, string file, byte[] bytes) => File.WriteAllBytes(Path.Combine(path, file), bytes);

    private static BuildIdentity BuildIdentityFor(ExerciseManifest manifest, byte[] normalizedManifest) => new(
        ExerciseBuildMode.Exploratory, new string('1', 40), new string('2', 40), true,
        $"sha256:{new string('3', 64)}", ".NET 10.0.11", "arm64", "arm64",
        Cna1979Ruleset.Manifest.Hash, ExerciseConfigurationIdentity.ComputeHash(manifest),
        Hash(normalizedManifest), ExerciseSeedLedger.SchemeId, false, false,
        [new BuildArtifactIdentity("Cna.Core.dll", 12, $"sha256:{new string('4', 64)}"),
         new BuildArtifactIdentity("Cna.ExerciseRunner.dll", 13, $"sha256:{new string('5', 64)}")]);

    private sealed record BundleFixture(
        string Path,
        ExerciseManifest Manifest,
        ExerciseExecutionResult Execution);
}
