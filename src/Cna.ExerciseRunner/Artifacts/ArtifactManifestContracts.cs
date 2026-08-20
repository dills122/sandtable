namespace Cna.ExerciseRunner.Artifacts;

public enum ArtifactBundleStatus
{
    Succeeded,
    Failed,
}

public enum ArtifactBundleProfile
{
    Succeeded,
    FailedPreAdmission,
    FailedAdmitted,
    FailedIdentified,
    FailedExecuted,
    FailedReconstructed,
    FailedReadjudicated,
    FailedSummarized,
}

public sealed record ArtifactManifestEntry
{
    internal ArtifactManifestEntry(
        string path,
        string schemaId,
        long sizeBytes,
        string sha256)
    {
        Path = ArtifactSchema.RequirePayloadPath(path);
        if (!string.Equals(
                schemaId,
                ArtifactSchema.SchemaFor(path),
                StringComparison.Ordinal))
            throw new ArgumentException("The schema ID does not match the fixed path.", nameof(schemaId));
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
        ReplayProofValidation.RequireSha256(sha256, nameof(sha256));
        SchemaId = schemaId;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
    }

    public string Path { get; }
    public string SchemaId { get; }
    public long SizeBytes { get; }
    public string Sha256 { get; }
}

public sealed class ArtifactManifest
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = ArtifactSchema.ArtifactManifestSchemaId;

    internal ArtifactManifest(
        ArtifactBundleProfile profile,
        IEnumerable<ArtifactManifestEntry> files)
    {
        if (!Enum.IsDefined(profile)) throw new ArgumentOutOfRangeException(nameof(profile));
        ArgumentNullException.ThrowIfNull(files);
        var copy = files.ToArray();
        if (copy.Any(value => value is null)
            || copy.Select(value => value.Path).Distinct(StringComparer.Ordinal).Count()
                != copy.Length)
            throw new ArgumentException("Artifact entries must be non-null and path-unique.", nameof(files));
        copy = copy.OrderBy(value => value.Path, StringComparer.Ordinal).ToArray();
        ArtifactProfiles.RequireExact(profile, copy.Select(value => value.Path));
        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        Profile = profile;
        Status = profile == ArtifactBundleProfile.Succeeded
            ? ArtifactBundleStatus.Succeeded
            : ArtifactBundleStatus.Failed;
        Confidentiality = ExerciseConfidentiality.TrustedAuthority;
        Files = Array.AsReadOnly(copy);
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ArtifactBundleProfile Profile { get; }
    public ArtifactBundleStatus Status { get; }
    public ExerciseConfidentiality Confidentiality { get; }
    public IReadOnlyList<ArtifactManifestEntry> Files { get; }
}

internal static class ArtifactProfiles
{
    private static readonly string[] Base =
    [
        ArtifactSchema.CheckResultsPath,
        ArtifactSchema.RunResultPath,
    ];

    internal static void RequireExact(
        ArtifactBundleProfile profile,
        IEnumerable<string> paths)
    {
        var actual = paths.Order(StringComparer.Ordinal).ToArray();
        var required = Required(profile).Order(StringComparer.Ordinal).ToArray();
        var withoutOptional = actual.Where(path => !string.Equals(
            path,
            ArtifactSchema.DiagnosticsPath,
            StringComparison.Ordinal)).ToArray();
        if (!withoutOptional.SequenceEqual(required, StringComparer.Ordinal))
            throw new ArgumentException("Artifact paths do not match the declared bundle profile.");
    }

    private static IEnumerable<string> Required(ArtifactBundleProfile profile)
    {
        IEnumerable<string> result = Base;
        if (profile == ArtifactBundleProfile.FailedPreAdmission) return result;
        result = result.Append(ArtifactSchema.ExerciseManifestPath);
        if (profile == ArtifactBundleProfile.FailedAdmitted) return result;
        result = result.Append(ArtifactSchema.BuildIdentityPath);
        if (profile == ArtifactBundleProfile.FailedIdentified) return result;
        result = result.Concat(
        [
            ArtifactSchema.SeedLedgerPath,
            ArtifactSchema.AcceptedActionsPath,
            ArtifactSchema.CanonicalEventsPath,
            ArtifactSchema.StepEvidencePath,
            ArtifactSchema.InitialSnapshotPath,
            ArtifactSchema.FinalSnapshotPath,
        ]);
        if (profile == ArtifactBundleProfile.FailedExecuted) return result;
        result = result.Append(ArtifactSchema.ReconstructionProofPath);
        if (profile == ArtifactBundleProfile.FailedReconstructed) return result;
        result = result.Append(ArtifactSchema.ReadjudicationProofPath);
        if (profile == ArtifactBundleProfile.FailedReadjudicated) return result;
        result = result.Concat(
        [
            ArtifactSchema.SummaryJsonPath,
            ArtifactSchema.SummaryMarkdownPath,
        ]);
        return result;
    }
}
