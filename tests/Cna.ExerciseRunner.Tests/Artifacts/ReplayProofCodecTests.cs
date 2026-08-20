using System.Text;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ReplayProofCodecTests
{
    private const string HashA = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string HashC = "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string HashD = "sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";

    [Fact]
    public void ReconstructionProofHasExactCanonicalBytesAndStrictRoundTrip()
    {
        var proof = new ReconstructionProof(
            ExerciseReconstructionFailureReason.None,
            HashA,
            HashB,
            HashB);

        var bytes = ReplayProofCodec.Serialize(proof);

        Assert.Equal(
            $"{{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-reconstruction-proof.v1\",\"eventStreamHash\":\"{HashA}\",\"expectedSnapshotHash\":\"{HashB}\",\"reconstructedSnapshotHash\":\"{HashB}\",\"historyAccepted\":true,\"finalSnapshotMatches\":true,\"status\":\"verified\"}}",
            Encoding.UTF8.GetString(bytes));
        Assert.Equal(proof, ReplayProofCodec.DeserializeReconstruction(bytes));
    }

    [Fact]
    public void ReadjudicationProofHasExactCanonicalBytesAndIndependentChecks()
    {
        var proof = new ReadjudicationProof(
            HashA,
            HashA,
            HashB,
            HashC,
            HashD,
            HashD);

        var bytes = ReplayProofCodec.Serialize(proof);

        Assert.Equal(
            $"{{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-readjudication-proof.v1\",\"expectedTranscriptHash\":\"{HashA}\",\"readjudicatedTranscriptHash\":\"{HashA}\",\"expectedEventsHash\":\"{HashB}\",\"readjudicatedEventsHash\":\"{HashC}\",\"expectedFinalSnapshotHash\":\"{HashD}\",\"readjudicatedFinalSnapshotHash\":\"{HashD}\",\"transcriptMatches\":true,\"eventsMatch\":false,\"finalSnapshotMatches\":true,\"status\":\"failed\"}}",
            Encoding.UTF8.GetString(bytes));
        var roundTrip = ReplayProofCodec.DeserializeReadjudication(bytes);
        Assert.Equal(proof, roundTrip);
        Assert.False(roundTrip.IsVerified);
        Assert.True(roundTrip.TranscriptMatches);
        Assert.False(roundTrip.EventsMatch);
        Assert.True(roundTrip.FinalSnapshotMatches);
    }

    [Fact]
    public void ReadersRejectUnknownReorderedExtraAndContradictoryProofShapes()
    {
        var proof = new ReadjudicationProof(HashA, HashA, HashB, HashB, HashC, HashC);
        var json = Encoding.UTF8.GetString(ReplayProofCodec.Serialize(proof));
        string[] invalid =
        [
            json.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1,\"schemeId\"", "\"schemeId\":\"wrong\",\"contractVersion\":1,\"schemeId\"", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-readjudication-proof.v1\"", "\"schemeId\":\"sandtable.exercise-readjudication-proof.v1\",\"contractVersion\":1", StringComparison.Ordinal),
            json.Replace("\"transcriptMatches\":true", "\"transcriptMatches\":false", StringComparison.Ordinal),
            json.Replace("\"status\":\"verified\"", "\"status\":\"failed\"", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ReplayProofCodec.DeserializeReadjudication(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void ReconstructionReaderRejectsContradictoryChecksAndHashes()
    {
        var proof = new ReconstructionProof(
            ExerciseReconstructionFailureReason.None,
            HashA,
            HashB,
            HashB);
        var json = Encoding.UTF8.GetString(ReplayProofCodec.Serialize(proof));
        string[] invalid =
        [
            json.Replace("\"finalSnapshotMatches\":true", "\"finalSnapshotMatches\":false", StringComparison.Ordinal),
            json.Replace("\"reconstructedSnapshotHash\":\"" + HashB + "\"", "\"reconstructedSnapshotHash\":null", StringComparison.Ordinal),
            json.Replace("\"status\":\"verified\"", "\"status\":\"failed\"", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ReplayProofCodec.DeserializeReconstruction(Encoding.UTF8.GetBytes(value))));
    }
}
