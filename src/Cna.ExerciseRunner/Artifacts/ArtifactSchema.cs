using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public static class ArtifactSchema
{
    public const string ArtifactManifestPath = "artifact-manifest.json";
    public const string ExerciseManifestPath = "exercise-manifest.json";
    public const string BuildIdentityPath = "build-identity.json";
    public const string RunResultPath = "run-result.json";
    public const string SeedLedgerPath = "seed-ledger.json";
    public const string AcceptedActionsPath = "accepted-actions.jsonl";
    public const string CanonicalEventsPath = "canonical-events.jsonl";
    public const string StepEvidencePath = "step-evidence.jsonl";
    public const string InitialSnapshotPath = "initial-snapshot.json";
    public const string FinalSnapshotPath = "final-snapshot.json";
    public const string ReconstructionProofPath = "reconstruction-proof.json";
    public const string ReadjudicationProofPath = "readjudication-proof.json";
    public const string CheckResultsPath = "check-results.json";
    public const string SummaryJsonPath = "summary.json";
    public const string SummaryMarkdownPath = "summary.md";
    public const string DiagnosticsPath = "diagnostics.jsonl";

    public const string ArtifactManifestSchemaId = "sandtable.exercise-artifacts.v1";
    public const string ExerciseManifestSchemaId = "sandtable.exercise-manifest.v1";
    public const string BuildIdentitySchemaId = "sandtable.exercise-build-identity.v1";
    public const string RunResultSchemaId = ExerciseRunResult.SchemeId;
    public const string SeedLedgerSchemaId = ExerciseSeedLedger.SchemeId;
    public const string AcceptedActionsSchemaId = "sandtable.exercise-accepted-actions.v1";
    public const string CanonicalEventsSchemaId = "sandtable.exercise-canonical-events.v1";
    public const string StepEvidenceSchemaId = "sandtable.exercise-step-evidence.v1";
    public const string SnapshotSchemaId = "sandtable.campaign-snapshot.v1";
    public const string ReconstructionProofSchemaId = ReconstructionProof.SchemeId;
    public const string ReadjudicationProofSchemaId = ReadjudicationProof.SchemeId;
    public const string CheckResultsSchemaId = ExerciseCheckResults.SchemeId;
    public const string SummaryJsonSchemaId = "sandtable.exercise-summary.v1";
    public const string SummaryMarkdownSchemaId = "sandtable.exercise-summary-markdown.v1";
    public const string DiagnosticsSchemaId = "sandtable.exercise-diagnostics.v1";

    private static readonly Dictionary<string, string> Schemas =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ArtifactManifestPath] = ArtifactManifestSchemaId,
            [ExerciseManifestPath] = ExerciseManifestSchemaId,
            [BuildIdentityPath] = BuildIdentitySchemaId,
            [RunResultPath] = RunResultSchemaId,
            [SeedLedgerPath] = SeedLedgerSchemaId,
            [AcceptedActionsPath] = AcceptedActionsSchemaId,
            [CanonicalEventsPath] = CanonicalEventsSchemaId,
            [StepEvidencePath] = StepEvidenceSchemaId,
            [InitialSnapshotPath] = SnapshotSchemaId,
            [FinalSnapshotPath] = SnapshotSchemaId,
            [ReconstructionProofPath] = ReconstructionProofSchemaId,
            [ReadjudicationProofPath] = ReadjudicationProofSchemaId,
            [CheckResultsPath] = CheckResultsSchemaId,
            [SummaryJsonPath] = SummaryJsonSchemaId,
            [SummaryMarkdownPath] = SummaryMarkdownSchemaId,
            [DiagnosticsPath] = DiagnosticsSchemaId,
        };

    public static string RequireKnownPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Path.IsPathRooted(path)
            || path.Contains('\\')
            || path.Contains('/')
            || path is "." or ".."
            || !Schemas.ContainsKey(path))
            throw new ArgumentException("The artifact path is not a fixed canonical v1 path.", nameof(path));
        return path;
    }

    internal static string RequirePayloadPath(string path)
    {
        RequireKnownPath(path);
        if (string.Equals(path, ArtifactManifestPath, StringComparison.Ordinal))
            throw new ArgumentException("The artifact manifest cannot list itself.", nameof(path));
        return path;
    }

    internal static string SchemaFor(string path) => Schemas[RequireKnownPath(path)];
}
