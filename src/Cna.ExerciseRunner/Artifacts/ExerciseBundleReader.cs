using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public sealed class ExerciseBundle
{
    private readonly byte[] artifactManifestBytes;
    private readonly byte[]? normalizedManifestBytes;
    private readonly byte[][] canonicalEvents;
    private readonly byte[]? initialSnapshotBytes;
    private readonly byte[]? finalSnapshotBytes;

    internal ExerciseBundle(
        string path,
        ArtifactManifest manifest,
        byte[] artifactManifestBytes,
        string artifactManifestSha256,
        ExerciseRunResult runResult,
        ExerciseCheckResults checkResults,
        ExerciseManifest? normalizedManifest,
        byte[]? normalizedManifestBytes,
        BuildIdentity? buildIdentity,
        ExerciseSeedLedger? seedLedger,
        ReconstructionProof? reconstructionProof,
        ReadjudicationProof? readjudicationProof,
        IReadOnlyList<ExerciseAcceptedActionRecord> acceptedActions,
        IReadOnlyList<ExerciseCanonicalEventRecord> canonicalEvents,
        IReadOnlyList<ExerciseStepEvidenceRecord> stepEvidence,
        byte[]? initialSnapshotBytes,
        byte[]? finalSnapshotBytes)
    {
        Path = path;
        Manifest = manifest;
        this.artifactManifestBytes = artifactManifestBytes.ToArray();
        ArtifactManifestSha256 = artifactManifestSha256;
        RunResult = runResult;
        CheckResults = checkResults;
        NormalizedManifest = normalizedManifest;
        this.normalizedManifestBytes = normalizedManifestBytes?.ToArray();
        BuildIdentity = buildIdentity;
        SeedLedger = seedLedger;
        ReconstructionProof = reconstructionProof;
        ReadjudicationProof = readjudicationProof;
        AcceptedActions = Array.AsReadOnly(acceptedActions.ToArray());
        this.canonicalEvents = canonicalEvents
            .Select(value => value.CanonicalBytes)
            .ToArray();
        StepEvidence = Array.AsReadOnly(stepEvidence.ToArray());
        this.initialSnapshotBytes = initialSnapshotBytes?.ToArray();
        this.finalSnapshotBytes = finalSnapshotBytes?.ToArray();
    }

    public string Path { get; }
    public ArtifactManifest Manifest { get; }
    public byte[] ArtifactManifestBytes => artifactManifestBytes.ToArray();
    public string ArtifactManifestSha256 { get; }
    public ExerciseRunResult RunResult { get; }
    public ExerciseCheckResults CheckResults { get; }
    public ExerciseManifest? NormalizedManifest { get; }
    public byte[]? NormalizedManifestBytes => normalizedManifestBytes?.ToArray();
    public BuildIdentity? BuildIdentity { get; }
    public ExerciseSeedLedger? SeedLedger { get; }
    public ReconstructionProof? ReconstructionProof { get; }
    public ReadjudicationProof? ReadjudicationProof { get; }
    public IReadOnlyList<ExerciseAcceptedActionRecord> AcceptedActions { get; }
    public IReadOnlyList<byte[]> CanonicalEvents => Array.AsReadOnly(
        canonicalEvents.Select(value => value.ToArray()).ToArray());
    public IReadOnlyList<ExerciseStepEvidenceRecord> StepEvidence { get; }
    public byte[]? InitialSnapshotBytes => initialSnapshotBytes?.ToArray();
    public byte[]? FinalSnapshotBytes => finalSnapshotBytes?.ToArray();
}

public static class ExerciseBundleReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static ExerciseBundle Read(string bundleDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleDirectory);
        try
        {
            var fullPath = Path.GetFullPath(bundleDirectory);
            RequireRegularDirectory(fullPath);
            var parentName = Directory.GetParent(fullPath)?.Name
                ?? throw new InvalidDataException("The bundle has no status parent directory.");
            var expectedStatus = parentName switch
            {
                "succeeded" => ArtifactBundleStatus.Succeeded,
                "failed" => ArtifactBundleStatus.Failed,
                _ => throw new InvalidDataException("The bundle is not in a final status directory."),
            };
            if (Directory.EnumerateDirectories(fullPath).Any())
                throw new InvalidDataException("Version-1 bundles cannot contain directories.");

            var manifestPath = Path.Combine(fullPath, ArtifactSchema.ArtifactManifestPath);
            RequireRegularFile(manifestPath);
            var artifactManifestBytes = File.ReadAllBytes(manifestPath);
            var manifest = ArtifactManifestCodec.Deserialize(artifactManifestBytes);
            if (manifest.Status != expectedStatus)
                throw new InvalidDataException("Artifact status does not match final placement.");

            var diskFiles = Directory.EnumerateFiles(fullPath)
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var expectedFiles = manifest.Files.Select(value => value.Path)
                .Append(ArtifactSchema.ArtifactManifestPath)
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (!diskFiles.SequenceEqual(expectedFiles, StringComparer.Ordinal))
                throw new InvalidDataException("Listed and on-disk artifact files do not match.");

            var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var entry in manifest.Files)
            {
                var payloadPath = Path.Combine(fullPath, entry.Path);
                RequireConfined(fullPath, payloadPath);
                RequireRegularFile(payloadPath);
                var payload = File.ReadAllBytes(payloadPath);
                if (payload.LongLength != entry.SizeBytes
                    || !string.Equals(Hash(payload), entry.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Artifact size or hash does not match its manifest.");
                ValidatePayload(entry.Path, payload);
                payloads.Add(entry.Path, payload);
            }

            var runResult = ExerciseRunResultCodec.Deserialize(payloads[ArtifactSchema.RunResultPath]);
            var resultStatus = runResult.Completion is ExerciseSucceeded
                ? ArtifactBundleStatus.Succeeded
                : ArtifactBundleStatus.Failed;
            if (resultStatus != manifest.Status)
                throw new InvalidDataException("Run result is the sole status authority and disagrees.");
            var checks = ExerciseCheckResultsCodec.Deserialize(
                payloads[ArtifactSchema.CheckResultsPath]);
            var normalizedManifestBytes = PayloadOrNull(
                payloads,
                ArtifactSchema.ExerciseManifestPath);
            var normalizedManifest = normalizedManifestBytes is null
                ? null
                : ExerciseManifestCodec.Deserialize(normalizedManifestBytes);
            var buildIdentityBytes = PayloadOrNull(payloads, ArtifactSchema.BuildIdentityPath);
            var buildIdentity = buildIdentityBytes is null
                ? null
                : BuildIdentityCodec.Deserialize(buildIdentityBytes);
            var seedLedgerBytes = PayloadOrNull(payloads, ArtifactSchema.SeedLedgerPath);
            var seedLedger = seedLedgerBytes is null
                ? null
                : SeedLedgerCodec.Deserialize(seedLedgerBytes);
            var acceptedActions = payloads.TryGetValue(
                ArtifactSchema.AcceptedActionsPath,
                out var acceptedActionBytes)
                ? ExerciseEvidenceCodec.DeserializeAcceptedActions(acceptedActionBytes)
                : [];
            var canonicalEvents = payloads.TryGetValue(
                ArtifactSchema.CanonicalEventsPath,
                out var canonicalEventBytes)
                ? ExerciseEvidenceCodec.DeserializeCanonicalEvents(canonicalEventBytes)
                : [];
            var stepEvidence = payloads.TryGetValue(
                ArtifactSchema.StepEvidencePath,
                out var stepEvidenceBytes)
                ? ExerciseEvidenceCodec.DeserializeStepEvidence(stepEvidenceBytes)
                : [];
            var initialSnapshot = PayloadOrNull(payloads, ArtifactSchema.InitialSnapshotPath);
            var finalSnapshot = PayloadOrNull(payloads, ArtifactSchema.FinalSnapshotPath);
            var reconstructionBytes = PayloadOrNull(
                payloads,
                ArtifactSchema.ReconstructionProofPath);
            var reconstruction = reconstructionBytes is null
                ? null
                : ReplayProofCodec.DeserializeReconstruction(reconstructionBytes);
            var readjudicationBytes = PayloadOrNull(
                payloads,
                ArtifactSchema.ReadjudicationProofPath);
            var readjudication = readjudicationBytes is null
                ? null
                : ReplayProofCodec.DeserializeReadjudication(readjudicationBytes);
            ExerciseBundleSemanticValidator.Validate(
                manifest.Profile,
                normalizedManifestBytes,
                normalizedManifest,
                buildIdentity,
                seedLedger,
                runResult,
                checks,
                acceptedActions,
                canonicalEvents,
                stepEvidence,
                initialSnapshot,
                finalSnapshot,
                reconstruction,
                readjudication);
            return new ExerciseBundle(
                fullPath,
                manifest,
                artifactManifestBytes,
                Hash(artifactManifestBytes),
                runResult,
                checks,
                normalizedManifest,
                normalizedManifestBytes,
                buildIdentity,
                seedLedger,
                reconstruction,
                readjudication,
                acceptedActions,
                canonicalEvents,
                stepEvidence,
                initialSnapshot,
                finalSnapshot);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or ArgumentException
            or ArithmeticException
            or DecoderFallbackException)
        {
            throw new InvalidDataException("The Exercise bundle is not trusted.", exception);
        }
    }

    private static byte[]? PayloadOrNull(
        Dictionary<string, byte[]> payloads,
        string path) => payloads.TryGetValue(path, out var payload) ? payload : null;

    private static void ValidatePayload(string path, byte[] payload)
    {
        switch (path)
        {
            case ArtifactSchema.ExerciseManifestPath:
                _ = ExerciseManifestCodec.Deserialize(payload);
                break;
            case ArtifactSchema.RunResultPath:
                _ = ExerciseRunResultCodec.Deserialize(payload);
                break;
            case ArtifactSchema.SeedLedgerPath:
                _ = SeedLedgerCodec.Deserialize(payload);
                break;
            case ArtifactSchema.ReconstructionProofPath:
                _ = ReplayProofCodec.DeserializeReconstruction(payload);
                break;
            case ArtifactSchema.ReadjudicationProofPath:
                _ = ReplayProofCodec.DeserializeReadjudication(payload);
                break;
            case ArtifactSchema.CheckResultsPath:
                _ = ExerciseCheckResultsCodec.Deserialize(payload);
                break;
            case ArtifactSchema.BuildIdentityPath:
                _ = BuildIdentityCodec.Deserialize(payload);
                break;
            case ArtifactSchema.InitialSnapshotPath:
            case ArtifactSchema.FinalSnapshotPath:
            case ArtifactSchema.SummaryJsonPath:
                RequireJsonObject(payload);
                break;
            case ArtifactSchema.AcceptedActionsPath:
            case ArtifactSchema.CanonicalEventsPath:
            case ArtifactSchema.StepEvidencePath:
            case ArtifactSchema.DiagnosticsPath:
                RequireJsonLines(payload);
                break;
            case ArtifactSchema.SummaryMarkdownPath:
                _ = StrictUtf8.GetString(payload);
                break;
            default:
                throw new InvalidDataException("The artifact path has no schema validator.");
        }
    }

    private static void RequireJsonObject(byte[] payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The artifact must contain one JSON object.");
    }

    private static void RequireJsonLines(byte[] payload)
    {
        if (payload.Length == 0) return;
        if (payload[^1] != (byte)'\n')
            throw new InvalidDataException("JSONL must end every record with LF.");
        var start = 0;
        for (var index = 0; index < payload.Length; index++)
        {
            if (payload[index] != (byte)'\n') continue;
            var length = index - start;
            if (length == 0 || payload[index - 1] == (byte)'\r')
                throw new InvalidDataException("JSONL records must be nonempty and LF-framed.");
            using var document = JsonDocument.Parse(payload.AsMemory(start, length));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("JSONL records must contain JSON objects.");
            start = index + 1;
        }
    }

    private static void RequireRegularDirectory(string path)
    {
        if (!Directory.Exists(path)) throw new InvalidDataException("Bundle directory does not exist.");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("A bundle directory cannot be a symlink or reparse point.");
    }

    private static void RequireRegularFile(string path)
    {
        if (!File.Exists(path)) throw new InvalidDataException("A required artifact file is missing.");
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException("Artifacts must be regular files, not links or directories.");
    }

    private static void RequireConfined(string root, string path)
    {
        var relative = Path.GetRelativePath(root, Path.GetFullPath(path));
        if (Path.IsPathRooted(relative)
            || relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            throw new InvalidDataException("An artifact path escapes its bundle root.");
    }

    private static string Hash(byte[] payload) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}";
}
