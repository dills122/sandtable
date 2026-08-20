using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseSummaryWriter
{
    public const int CurrentContractVersion = 1;

    public static byte[] WriteJson(
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult,
        ExerciseCheckResults checks,
        ReadjudicationProof? readjudication)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(checks);
        var campaignId = execution.Steps.Count > 0
            ? execution.Steps[0].Receipt.CampaignId
            : ExerciseCampaignId.Derive(
                ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("schemeId", ArtifactSchema.SummaryJsonSchemaId);
            writer.WriteString("exerciseId", manifest.ExerciseId);
            writer.WriteString("campaignId", campaignId);
            writer.WriteString(
                "status",
                runResult.Completion is ExerciseSucceeded ? "succeeded" : "failed");
            if ((runResult.Completion as ExerciseSucceeded)?.Outcome is BoundaryReached boundary)
                writer.WriteString("boundaryPositionId", boundary.PositionId);
            else writer.WriteNull("boundaryPositionId");
            if ((runResult.Completion as ExerciseFailed)?.Failure is { } failure)
                writer.WriteString(
                    "failureCategory",
                    ExerciseContractText.FormatFailure(failure.Category));
            else writer.WriteNull("failureCategory");
            writer.WriteNumber("stepsAccepted", execution.Steps.Count);
            if (execution.Reconstruction is null) writer.WriteNull("reconstructionVerified");
            else writer.WriteBoolean(
                "reconstructionVerified",
                execution.Reconstruction.IsVerified);
            if (readjudication is null) writer.WriteNull("readjudicationVerified");
            else writer.WriteBoolean("readjudicationVerified", readjudication.IsVerified);
            writer.WriteNumber("checksPassed", checks.Results.Count(value => value.IsPassed));
            writer.WriteNumber("checksFailed", checks.Results.Count(value => !value.IsPassed));
            writer.WriteString("confidentiality", "trusted-authority");
            writer.WriteString("detail", manifest.Detail switch
            {
                ExerciseDetail.Compact => "compact",
                ExerciseDetail.Forensic => "forensic",
                ExerciseDetail.Debug => "debug",
                _ => throw new ArgumentOutOfRangeException(nameof(manifest)),
            });
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static byte[] WriteMarkdown(
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult,
        ExerciseCheckResults checks)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(runResult);
        ArgumentNullException.ThrowIfNull(checks);
        var status = runResult.Completion is ExerciseSucceeded ? "succeeded" : "failed";
        var text = $"# Exercise {manifest.ExerciseId}\n\n"
            + $"- Status: {status}\n"
            + $"- Accepted steps: {execution.Steps.Count}\n"
            + $"- Passed checks: {checks.Results.Count(value => value.IsPassed)}\n"
            + $"- Failed checks: {checks.Results.Count(value => !value.IsPassed)}\n"
            + "- Confidentiality: trusted-authority\n";
        return Encoding.UTF8.GetBytes(text);
    }
}
