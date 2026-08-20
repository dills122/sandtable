using System.Security.Cryptography;
using System.Text;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseEvidenceWriterTests
{
    [Fact]
    public void CanonicalEvidenceAndReportsMatchVersionOneGoldens()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var execution = ExerciseExecutor.Execute(
            manifest,
            TestContext.Current.CancellationToken);
        var readjudication = ReadjudicationVerifier.Verify(manifest, execution);
        var checks = execution.CheckResults.WithReadjudication(readjudication);

        var acceptedActions = ExerciseEvidenceWriter.WriteAcceptedActions(execution);
        var canonicalEvents = ExerciseEvidenceWriter.WriteCanonicalEvents(execution);
        var stepEvidence = ExerciseEvidenceWriter.WriteStepEvidence(execution);
        var summaryJson = ExerciseSummaryWriter.WriteJson(
            manifest,
            execution,
            execution.RunResult,
            checks,
            readjudication);
        var summaryMarkdown = ExerciseSummaryWriter.WriteMarkdown(
            manifest,
            execution,
            execution.RunResult,
            checks);
        var diagnostics = ExerciseDiagnosticsWriter.Write(
            manifest,
            execution,
            execution.RunResult);

        Assert.Equal(
            "3b5654d069728ff18a17e2e1d03a0479d67544eb2aff3da2f5c7b19f87505acc",
            Hash(acceptedActions));
        Assert.Equal(
            "0684d3c8a1db50e2afd6521163d0fc45013155653ee270014d6274d1478793c1",
            Hash(canonicalEvents));
        Assert.Equal(
            "4379bc5b7a900d1ed28f3b01110d3586765f81a2345121d1b6bd2a3137e0180a",
            Hash(stepEvidence));
        Assert.Equal(
            "afb8450019eb504713f6d5584f2e9f7b483804a26632eccf3afff5fa4c4de38f",
            Hash(summaryJson));
        Assert.Equal(
            "0d8c005222607adeb91ecf6bd5a98a36e1ff00d644433a14445d8d09381bd29b",
            Hash(summaryMarkdown));
        Assert.Equal(
            "ca729d6db35cc35c435fe8224a4f88fdc9f02c8807429d85d0cbed0b30c647e7",
            Hash(diagnostics));
        Assert.Equal(5, CountRecords(acceptedActions));
        Assert.Equal(5, CountRecords(canonicalEvents));
        Assert.Equal(5, CountRecords(stepEvidence));
        Assert.Equal(6, CountRecords(diagnostics));
        Assert.DoesNotContain('\r', Encoding.UTF8.GetString(acceptedActions));
        Assert.Equal(
            "# Exercise organization-boundary\n\n"
                + "- Status: succeeded\n"
                + "- Accepted steps: 5\n"
                + "- Passed checks: 38\n"
                + "- Failed checks: 0\n"
                + "- Confidentiality: trusted-authority\n",
            Encoding.UTF8.GetString(summaryMarkdown));
    }

    [Fact]
    public void ControllerConfigurationIdentityHasItsOwnVersionedGolden()
    {
        var hash = ExerciseConfigurationIdentity.ComputeHash(
            ExerciseManifestCodecTests.Create());

        Assert.Equal(
            "sha256:1a5b64805ccc6531434c3a37d3346c6e7797f2da132c020fd7f61e03870ee769",
            hash);
    }

    private static string Hash(byte[] value) =>
        Convert.ToHexStringLower(SHA256.HashData(value));

    private static int CountRecords(byte[] value) => value.Count(item => item == (byte)'\n');
}
