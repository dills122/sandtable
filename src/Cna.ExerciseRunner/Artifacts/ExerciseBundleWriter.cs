using System.Security.Cryptography;

namespace Cna.ExerciseRunner.Artifacts;

public enum ArtifactWriterFailpoint
{
    StagingCreated,
    BeforePayloadFlush,
    AfterPayloadFlush,
    BeforeRunResultFlush,
    AfterRunResultFlush,
    BeforeManifestFlush,
    AfterManifestFlush,
    BeforeMove,
    AfterMove,
    BeforeReadback,
    AfterReadback,
}

public sealed class ExerciseBundleWriteRequest
{
    private readonly Dictionary<string, byte[]> payloads;

    internal ExerciseBundleWriteRequest(
        ArtifactBundleProfile profile,
        IReadOnlyDictionary<string, byte[]> payloads)
    {
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        ArgumentNullException.ThrowIfNull(payloads);
        this.payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var pair in payloads)
        {
            ArtifactSchema.RequirePayloadPath(pair.Key);
            this.payloads.Add(
                pair.Key,
                pair.Value?.ToArray()
                    ?? throw new ArgumentException("Artifact payloads cannot be null.", nameof(payloads)));
        }
        Profile = profile;
        _ = CreateManifest();
        var result = ExerciseRunResultCodec.Deserialize(this.payloads[ArtifactSchema.RunResultPath]);
        var expectedStatus = result.Completion is ExerciseSucceeded
            ? ArtifactBundleStatus.Succeeded
            : ArtifactBundleStatus.Failed;
        var profileStatus = profile == ArtifactBundleProfile.Succeeded
            ? ArtifactBundleStatus.Succeeded
            : ArtifactBundleStatus.Failed;
        if (expectedStatus != profileStatus)
            throw new ArgumentException("The run result contradicts the bundle profile.", nameof(payloads));
        _ = ExerciseCheckResultsCodec.Deserialize(this.payloads[ArtifactSchema.CheckResultsPath]);
    }

    public ArtifactBundleProfile Profile { get; }

    internal IEnumerable<string> Paths => payloads.Keys;

    internal byte[] GetPayload(string path) => payloads[path].ToArray();

    internal Dictionary<string, byte[]> PayloadCopy() => payloads.ToDictionary(
        pair => pair.Key,
        pair => pair.Value.ToArray(),
        StringComparer.Ordinal);

    internal ArtifactManifest CreateManifest() => new(
        Profile,
        payloads.Select(pair => new ArtifactManifestEntry(
            pair.Key,
            ArtifactSchema.SchemaFor(pair.Key),
            pair.Value.LongLength,
            Hash(pair.Value))));

    private static string Hash(byte[] payload) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}";
}

public sealed class ArtifactWriteFailure
{
    internal ArtifactWriteFailure(
        Exception exception,
        string? partialBundlePath,
        Exception? fallbackException = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        Message = exception.Message;
        PartialBundlePath = partialBundlePath;
        FallbackExceptionType = fallbackException?.GetType().FullName
            ?? fallbackException?.GetType().Name;
        FallbackMessage = fallbackException?.Message;
    }

    public string ExceptionType { get; }
    public string Message { get; }
    public string? PartialBundlePath { get; }
    public string? FallbackExceptionType { get; }
    public string? FallbackMessage { get; }
}

public sealed class ExerciseBundleWriteOutcome
{
    private ExerciseBundleWriteOutcome(
        bool isPrimarySucceeded,
        ExerciseBundle? completedBundle,
        ArtifactWriteFailure? failure)
    {
        IsPrimarySucceeded = isPrimarySucceeded;
        CompletedBundle = completedBundle;
        Failure = failure;
    }

    public bool IsPrimarySucceeded { get; }
    public ExerciseBundle? CompletedBundle { get; }
    public ArtifactWriteFailure? Failure { get; }

    internal static ExerciseBundleWriteOutcome PrimarySucceeded(ExerciseBundle bundle) =>
        new(true, bundle, null);

    internal static ExerciseBundleWriteOutcome Failed(
        ArtifactWriteFailure failure,
        ExerciseBundle? completedFallback) =>
        new(false, completedFallback, failure);
}

public static class ExerciseBundleWriter
{
    public static ExerciseBundle Write(
        string artifactRoot,
        ExerciseBundleWriteRequest request) =>
        Write(
            artifactRoot,
            request,
            static (_, _, _) => { },
            Guid.NewGuid().ToString("N"));

    public static ExerciseBundleWriteOutcome TryWrite(
        string artifactRoot,
        ExerciseBundleWriteRequest primary,
        ExerciseBundleWriteRequest? failedFallback = null) =>
        TryWrite(
            artifactRoot,
            primary,
            failedFallback,
            static (_, _, _) => { },
            Guid.NewGuid().ToString("N"));

    internal static ExerciseBundleWriteOutcome TryWrite(
        string artifactRoot,
        ExerciseBundleWriteRequest primary,
        ExerciseBundleWriteRequest? failedFallback,
        Action<ArtifactWriterFailpoint, string, string> observer,
        string runDirectoryId)
    {
        ArgumentNullException.ThrowIfNull(primary);
        if (failedFallback?.Profile == ArtifactBundleProfile.Succeeded)
            throw new ArgumentException("A write fallback must be a failed profile.", nameof(failedFallback));
        try
        {
            return ExerciseBundleWriteOutcome.PrimarySucceeded(Write(
                artifactRoot,
                primary,
                observer,
                runDirectoryId));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var partialPath = Path.Combine(
                Path.GetFullPath(artifactRoot),
                ".partial",
                runDirectoryId);
            partialPath = Directory.Exists(partialPath) ? partialPath : null;
            if (failedFallback is null)
                return ExerciseBundleWriteOutcome.Failed(
                    new ArtifactWriteFailure(exception, partialPath),
                    null);
            try
            {
                var fallback = Write(
                    artifactRoot,
                    failedFallback,
                    static (_, _, _) => { },
                    $"{runDirectoryId}-failed");
                return ExerciseBundleWriteOutcome.Failed(
                    new ArtifactWriteFailure(exception, partialPath),
                    fallback);
            }
            catch (Exception fallbackException) when (fallbackException is IOException
                or UnauthorizedAccessException)
            {
                return ExerciseBundleWriteOutcome.Failed(
                    new ArtifactWriteFailure(exception, partialPath, fallbackException),
                    null);
            }
        }
    }

    internal static ExerciseBundle Write(
        string artifactRoot,
        ExerciseBundleWriteRequest request,
        Action<ArtifactWriterFailpoint, string, string> observer,
        string runDirectoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(observer);
        RequireDirectoryId(runDirectoryId);

        var root = Path.GetFullPath(artifactRoot);
        RequireRoot(root);
        var partialRoot = CreateRegularDirectory(Path.Combine(root, ".partial"));
        var statusName = request.Profile == ArtifactBundleProfile.Succeeded
            ? "succeeded"
            : "failed";
        var statusRoot = CreateRegularDirectory(Path.Combine(root, statusName));
        var stagingPath = Path.Combine(partialRoot, runDirectoryId);
        var finalPath = Path.Combine(statusRoot, runDirectoryId);
        if (Directory.Exists(stagingPath) || File.Exists(stagingPath))
            throw new IOException("The unique staging destination already exists.");
        if (Directory.Exists(finalPath) || File.Exists(finalPath))
            throw new IOException("The final bundle destination already exists.");
        Directory.CreateDirectory(stagingPath);
        observer(ArtifactWriterFailpoint.StagingCreated, stagingPath, finalPath);

        foreach (var path in request.Paths
            .Where(path => !string.Equals(
                path,
                ArtifactSchema.RunResultPath,
                StringComparison.Ordinal))
            .Order(StringComparer.Ordinal))
        {
            observer(ArtifactWriterFailpoint.BeforePayloadFlush, stagingPath, finalPath);
            WriteDurableFile(stagingPath, path, request.GetPayload(path));
            observer(ArtifactWriterFailpoint.AfterPayloadFlush, stagingPath, finalPath);
        }

        observer(ArtifactWriterFailpoint.BeforeRunResultFlush, stagingPath, finalPath);
        WriteDurableFile(
            stagingPath,
            ArtifactSchema.RunResultPath,
            request.GetPayload(ArtifactSchema.RunResultPath));
        observer(ArtifactWriterFailpoint.AfterRunResultFlush, stagingPath, finalPath);

        var manifest = CreateManifestFromDisk(stagingPath, request);
        observer(ArtifactWriterFailpoint.BeforeManifestFlush, stagingPath, finalPath);
        WriteDurableFile(
            stagingPath,
            ArtifactSchema.ArtifactManifestPath,
            ArtifactManifestCodec.Serialize(manifest));
        observer(ArtifactWriterFailpoint.AfterManifestFlush, stagingPath, finalPath);

        observer(ArtifactWriterFailpoint.BeforeMove, stagingPath, finalPath);
        Directory.Move(stagingPath, finalPath);
        observer(ArtifactWriterFailpoint.AfterMove, stagingPath, finalPath);
        observer(ArtifactWriterFailpoint.BeforeReadback, stagingPath, finalPath);
        var bundle = ExerciseBundleReader.Read(finalPath);
        observer(ArtifactWriterFailpoint.AfterReadback, stagingPath, finalPath);
        return bundle;
    }

    private static ArtifactManifest CreateManifestFromDisk(
        string stagingPath,
        ExerciseBundleWriteRequest request) =>
        new(
            request.Profile,
            request.Paths.Select(path =>
            {
                var payload = File.ReadAllBytes(Path.Combine(stagingPath, path));
                return new ArtifactManifestEntry(
                    path,
                    ArtifactSchema.SchemaFor(path),
                    payload.LongLength,
                    $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}");
            }));

    private static void WriteDurableFile(string stagingPath, string relativePath, byte[] payload)
    {
        ArtifactSchema.RequireKnownPath(relativePath);
        var finalPath = Path.Combine(stagingPath, relativePath);
        var temporaryPath = Path.Combine(
            stagingPath,
            $".{relativePath}.{Guid.NewGuid():N}.tmp");
        using (var stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            stream.Write(payload);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporaryPath, finalPath);
    }

    private static void RequireRoot(string root)
    {
        if (File.Exists(root)) throw new InvalidDataException("Artifact root cannot be a file.");
        if (Directory.Exists(root)
            && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Artifact root cannot be a symlink or reparse point.");
        Directory.CreateDirectory(root);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Artifact root cannot be a symlink or reparse point.");
    }

    private static string CreateRegularDirectory(string path)
    {
        if (File.Exists(path)) throw new InvalidDataException("Artifact directory cannot be a file.");
        Directory.CreateDirectory(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Artifact directory cannot be a symlink or reparse point.");
        return path;
    }

    private static void RequireDirectoryId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value is "." or ".."
            || value.Contains('/')
            || value.Contains('\\')
            || !string.Equals(value, Path.GetFileName(value), StringComparison.Ordinal))
            throw new ArgumentException("The run directory ID must be one local path segment.", nameof(value));
    }
}
