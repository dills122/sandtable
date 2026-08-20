using System.Text.Json;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseDiagnosticsWriter
{
    public static byte[] Write(
        ExerciseManifest manifest,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(runResult);
        using var stream = new MemoryStream();
        foreach (var step in execution.Steps)
        {
            WriteRecord(stream, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("event", "exercise.step-accepted");
                writer.WriteString("exerciseId", manifest.ExerciseId);
                writer.WriteNumber("stepOrdinal", step.Ordinal);
                writer.WriteString("campaignId", step.Receipt.CampaignId);
                writer.WriteNumber("stateVersion", step.Receipt.CommittedStateVersion);
                writer.WriteString("positionId", step.Receipt.ResultingPositionId);
                writer.WriteString("audience", step.Audience.ToString().ToLowerInvariant());
                writer.WriteString("actionId", step.ActionId);
                writer.WriteEndObject();
            });
        }
        WriteRecord(stream, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.completed");
            writer.WriteString("exerciseId", manifest.ExerciseId);
            writer.WriteString(
                "status",
                runResult.Completion is ExerciseSucceeded ? "succeeded" : "failed");
            writer.WriteNumber("stepsAccepted", execution.Steps.Count);
            writer.WriteEndObject();
        });
        return stream.ToArray();
    }

    private static void WriteRecord(MemoryStream stream, Action<Utf8JsonWriter> write)
    {
        using var record = new MemoryStream();
        using (var writer = new Utf8JsonWriter(record)) write(writer);
        stream.Write(record.ToArray());
        stream.WriteByte((byte)'\n');
    }
}
