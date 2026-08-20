using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseEvidenceWriter
{
    public const int CurrentContractVersion = 1;

    public static byte[] WriteAcceptedActions(ExerciseExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        return WriteJsonLines(execution.Steps.Select(step => WriteJson(writer =>
        {
            var receipt = step.Receipt;
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("schemeId", ArtifactSchema.AcceptedActionsSchemaId);
            writer.WriteNumber("stepOrdinal", step.Ordinal);
            writer.WriteString("campaignId", receipt.CampaignId);
            writer.WriteNumber("priorStateVersion", receipt.PriorStateVersion);
            writer.WriteNumber("committedStateVersion", receipt.CommittedStateVersion);
            writer.WriteString("resultingPositionId", receipt.ResultingPositionId);
            writer.WriteString("audience", FormatAudience(receipt.Audience));
            writer.WriteString("actionId", receipt.ActionId);
            writer.WriteEndObject();
        })));
    }

    public static byte[] WriteCanonicalEvents(ExerciseExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        using var stream = new MemoryStream();
        foreach (var record in execution.Steps.SelectMany(step => step.EventRecords))
        {
            stream.Write(record);
            stream.WriteByte((byte)'\n');
        }
        return stream.ToArray();
    }

    public static byte[] WriteStepEvidence(ExerciseExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        return WriteJsonLines(execution.Steps.Select(step => WriteJson(writer =>
        {
            var receipt = step.Receipt;
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("schemeId", ArtifactSchema.StepEvidenceSchemaId);
            writer.WriteNumber("stepOrdinal", step.Ordinal);
            writer.WriteString("campaignId", receipt.CampaignId);
            writer.WriteNumber("stateVersion", receipt.CommittedStateVersion);
            writer.WriteString("positionId", receipt.ResultingPositionId);
            writer.WriteString("audience", FormatAudience(receipt.Audience));
            writer.WriteString("actionId", receipt.ActionId);
            writer.WriteString("eventsHash", ReplayEvidenceHasher.HashRecords(step.EventRecords));
            writer.WriteString("snapshotHash", Hash(step.SnapshotCheckpoint));
            writer.WriteEndObject();
        })));
    }

    private static byte[] WriteJson(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        return stream.ToArray();
    }

    private static byte[] WriteJsonLines(IEnumerable<byte[]> records)
    {
        using var stream = new MemoryStream();
        foreach (var record in records)
        {
            stream.Write(record);
            stream.WriteByte((byte)'\n');
        }
        return stream.ToArray();
    }

    private static string FormatAudience(CampaignActionAudience value) => value switch
    {
        CampaignActionAudience.System => "system",
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Hash(byte[] value) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(value))}";
}
