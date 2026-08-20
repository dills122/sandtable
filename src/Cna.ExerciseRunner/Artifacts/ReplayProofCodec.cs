using System.Text.Json;
using Cna.Core.Exercises;

namespace Cna.ExerciseRunner.Artifacts;

public static class ReplayProofCodec
{
    private static readonly string[] ReconstructionProperties =
    [
        "contractVersion", "schemeId", "eventStreamHash", "expectedSnapshotHash",
        "reconstructedSnapshotHash", "historyAccepted", "finalSnapshotMatches", "status",
    ];

    private static readonly string[] ReadjudicationProperties =
    [
        "contractVersion", "schemeId", "expectedTranscriptHash", "readjudicatedTranscriptHash",
        "expectedEventsHash", "readjudicatedEventsHash", "expectedFinalSnapshotHash",
        "readjudicatedFinalSnapshotHash", "transcriptMatches", "eventsMatch",
        "finalSnapshotMatches", "status",
    ];

    public static byte[] Serialize(ReconstructionProof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", proof.ContractVersion);
            writer.WriteString("schemeId", proof.ContractSchemeId);
            writer.WriteString("eventStreamHash", proof.EventStreamHash);
            writer.WriteString("expectedSnapshotHash", proof.ExpectedSnapshotHash);
            if (proof.ReconstructedSnapshotHash is null)
                writer.WriteNull("reconstructedSnapshotHash");
            else writer.WriteString("reconstructedSnapshotHash", proof.ReconstructedSnapshotHash);
            writer.WriteBoolean("historyAccepted", proof.HistoryAccepted);
            writer.WriteBoolean("finalSnapshotMatches", proof.FinalSnapshotMatches);
            writer.WriteString("status", proof.IsVerified ? "verified" : "failed");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static byte[] Serialize(ReadjudicationProof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", proof.ContractVersion);
            writer.WriteString("schemeId", proof.ContractSchemeId);
            writer.WriteString("expectedTranscriptHash", proof.ExpectedTranscriptHash);
            writer.WriteString("readjudicatedTranscriptHash", proof.ReadjudicatedTranscriptHash);
            writer.WriteString("expectedEventsHash", proof.ExpectedEventsHash);
            writer.WriteString("readjudicatedEventsHash", proof.ReadjudicatedEventsHash);
            writer.WriteString("expectedFinalSnapshotHash", proof.ExpectedFinalSnapshotHash);
            writer.WriteString(
                "readjudicatedFinalSnapshotHash",
                proof.ReadjudicatedFinalSnapshotHash);
            writer.WriteBoolean("transcriptMatches", proof.TranscriptMatches);
            writer.WriteBoolean("eventsMatch", proof.EventsMatch);
            writer.WriteBoolean("finalSnapshotMatches", proof.FinalSnapshotMatches);
            writer.WriteString("status", proof.IsVerified ? "verified" : "failed");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ReconstructionProof DeserializeReconstruction(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, ReconstructionProperties);
            RequireHeader(
                root,
                ReconstructionProof.CurrentContractVersion,
                ReconstructionProof.SchemeId);
            var historyAccepted = root.GetProperty("historyAccepted").GetBoolean();
            var finalMatches = root.GetProperty("finalSnapshotMatches").GetBoolean();
            var reconstructed = ReadNullableString(root.GetProperty("reconstructedSnapshotHash"));
            var reason = (historyAccepted, finalMatches) switch
            {
                (true, true) => ExerciseReconstructionFailureReason.None,
                (false, false) => ExerciseReconstructionFailureReason.InvalidHistory,
                (true, false) => ExerciseReconstructionFailureReason.SnapshotMismatch,
                _ => throw new JsonException("The reconstruction checks are contradictory."),
            };
            var proof = new ReconstructionProof(
                reason,
                RequireString(root, "eventStreamHash"),
                RequireString(root, "expectedSnapshotHash"),
                reconstructed);
            RequireStatus(root, proof.IsVerified);
            return proof;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException
            or FormatException)
        {
            throw new JsonException("The reconstruction proof is invalid.", exception);
        }
    }

    public static ReadjudicationProof DeserializeReadjudication(
        ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, ReadjudicationProperties);
            RequireHeader(
                root,
                ReadjudicationProof.CurrentContractVersion,
                ReadjudicationProof.SchemeId);
            var proof = new ReadjudicationProof(
                RequireString(root, "expectedTranscriptHash"),
                RequireString(root, "readjudicatedTranscriptHash"),
                RequireString(root, "expectedEventsHash"),
                RequireString(root, "readjudicatedEventsHash"),
                RequireString(root, "expectedFinalSnapshotHash"),
                RequireString(root, "readjudicatedFinalSnapshotHash"));
            if (root.GetProperty("transcriptMatches").GetBoolean() != proof.TranscriptMatches
                || root.GetProperty("eventsMatch").GetBoolean() != proof.EventsMatch
                || root.GetProperty("finalSnapshotMatches").GetBoolean()
                    != proof.FinalSnapshotMatches)
                throw new JsonException("The re-adjudication checks are contradictory.");
            RequireStatus(root, proof.IsVerified);
            return proof;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException
            or FormatException)
        {
            throw new JsonException("The re-adjudication proof is invalid.", exception);
        }
    }

    private static void RequireHeader(JsonElement root, int contractVersion, string schemeId)
    {
        if (root.GetProperty("contractVersion").GetInt32() != contractVersion)
            throw new JsonException("Unknown proof contract version.");
        if (!string.Equals(
                root.GetProperty("schemeId").GetString(),
                schemeId,
                StringComparison.Ordinal))
            throw new JsonException("Unknown proof scheme.");
    }

    private static void RequireStatus(JsonElement root, bool isVerified)
    {
        var expected = isVerified ? "verified" : "failed";
        if (!string.Equals(
                root.GetProperty("status").GetString(),
                expected,
                StringComparison.Ordinal))
            throw new JsonException("The proof status is contradictory.");
    }

    private static string RequireString(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName).GetString()
        ?? throw new JsonException($"{propertyName} must be a string.");

    private static string? ReadNullableString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => element.GetString(),
        _ => throw new JsonException("The hash must be a string or null."),
    };
}
