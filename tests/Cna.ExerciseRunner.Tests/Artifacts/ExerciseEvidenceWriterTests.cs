using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseEvidenceWriterTests
{
    private static readonly string[] CompactEventNames =
    [
        "exercise.step-accepted",
        "exercise.completed",
    ];

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
            execution.RunResult,
            checks,
            readjudication);

        Assert.Equal(
            "3b5654d069728ff18a17e2e1d03a0479d67544eb2aff3da2f5c7b19f87505acc",
            Hash(acceptedActions));
        Assert.Equal(
            "0684d3c8a1db50e2afd6521163d0fc45013155653ee270014d6274d1478793c1",
            Hash(canonicalEvents));
        Assert.Equal(
            "feb7f0d6b5629659142e422dfcde6f36b0e498ff21bb968ca03c8e4e04c23601",
            Hash(stepEvidence));
        Assert.Equal(
            "afb8450019eb504713f6d5584f2e9f7b483804a26632eccf3afff5fa4c4de38f",
            Hash(summaryJson));
        Assert.NotEmpty(summaryMarkdown);
        Assert.NotEmpty(diagnostics);
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
    public void DetailTiersAreMonotonicAndPreserveSimulationEvidence()
    {
        var compact = Run(ExerciseDetail.Compact);
        var forensic = Run(ExerciseDetail.Forensic);
        var debug = Run(ExerciseDetail.Debug);

        Assert.Equal(compact.Evidence, forensic.Evidence);
        Assert.Equal(compact.Evidence, debug.Evidence);

        var compactEvents = EventNames(compact.Diagnostics);
        var forensicEvents = EventNames(forensic.Diagnostics);
        var debugEvents = EventNames(debug.Diagnostics);
        Assert.Equal(6, compactEvents.Length);
        Assert.All(compactEvents, name => Assert.Contains(
            name,
            CompactEventNames));
        Assert.True(forensicEvents.Length > compactEvents.Length);
        Assert.Contains("exercise.query-evaluated", forensicEvents);
        Assert.Contains("exercise.controller-selected", forensicEvents);
        Assert.Contains("exercise.check-evaluated", forensicEvents);
        Assert.Contains("exercise.reconstruction-verified", forensicEvents);
        Assert.Contains("exercise.readjudication-verified", forensicEvents);
        var forensicText = Encoding.UTF8.GetString(forensic.Diagnostics);
        Assert.Contains("\"eventStreamHash\":\"sha256:", forensicText, StringComparison.Ordinal);
        Assert.Contains("\"snapshotHash\":\"sha256:", forensicText, StringComparison.Ordinal);
        Assert.True(debugEvents.Length > forensicEvents.Length);
        Assert.Contains("exercise.operation-timing", debugEvents);
        Assert.DoesNotContain("exercise.operation-timing", forensicEvents);
    }

    [Fact]
    public void ForensicDiagnosticsAreDeterministicWhileDebugTimingsAreNoncanonical()
    {
        var first = Run(ExerciseDetail.Forensic);
        var second = Run(ExerciseDetail.Forensic);

        Assert.Equal(first.Diagnostics, second.Diagnostics);

        var debug = Run(ExerciseDetail.Debug);
        using var records = JsonDocument.Parse($"[{Encoding.UTF8.GetString(debug.Diagnostics).Replace("\n", ",", StringComparison.Ordinal).TrimEnd(',')}]");
        var timings = records.RootElement.EnumerateArray()
            .Where(record => record.GetProperty("event").GetString() == "exercise.operation-timing")
            .ToArray();
        Assert.NotEmpty(timings);
        Assert.All(timings, timing => Assert.True(
            timing.GetProperty("elapsedMicroseconds").GetInt64() >= 0));
    }

    [Fact]
    public void DebugDiagnosticsIncludeOuterPhaseTimingAndPreparedPayloadSizing()
    {
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Debug);
        var execution = ExerciseExecutor.Execute(manifest, CancellationToken.None);
        var readjudication = ReadjudicationVerifier.Verify(manifest, execution);
        var checks = execution.CheckResults.WithReadjudication(readjudication);
        var telemetry = new ExerciseDiagnosticTelemetry();
        telemetry.RecordPhase("build-identity", 17);
        telemetry.RecordPreparedPayloads(12, 28_000);

        var diagnostics = ExerciseDiagnosticsWriter.Write(
            manifest,
            execution,
            execution.RunResult,
            checks,
            readjudication,
            telemetry);

        var lines = Encoding.UTF8.GetString(diagnostics)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(lines, line => line.Contains(
            "\"operation\":\"build-identity\",\"stepOrdinal\":null,\"audience\":null,\"elapsedMicroseconds\":17",
            StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains(
            "\"event\":\"exercise.artifact-prepared\"",
            StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains(
            "\"payloadCountBeforeDiagnostics\":12,\"logicalBytesBeforeDiagnostics\":28000",
            StringComparison.Ordinal));
    }

    [Fact]
    public void ForensicFailureDiagnosticsRetainTheUnacceptedDecisionContext()
    {
        var manifest = ExerciseManifestCodecTests.Create(
            terminalBoundary: "land.position.never",
            detail: ExerciseDetail.Forensic);
        var execution = ExerciseExecutor.Execute(
            manifest,
            NoActiveAudienceRuntime.Instance,
            CancellationToken.None);

        var diagnostics = ExerciseDiagnosticsWriter.Write(
            manifest,
            execution,
            execution.RunResult,
            execution.CheckResults,
            null);

        var lines = Encoding.UTF8.GetString(diagnostics)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Count(line => line.Contains(
            "\"event\":\"exercise.query-evaluated\"",
            StringComparison.Ordinal)
            && line.Contains("\"stepOrdinal\":0", StringComparison.Ordinal)));
        Assert.Contains(lines, line => line.Contains(
            "\"event\":\"exercise.controller-selection-failed\"",
            StringComparison.Ordinal)
            && line.Contains("\"failureReason\":\"no-active-audience\"", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains(
            "\"check\":\"active-audience-cardinality\",\"status\":\"failed\"",
            StringComparison.Ordinal)
            && line.Contains("\"stepOrdinal\":0", StringComparison.Ordinal)
            && line.Contains(
                "\"positionId\":\"land.position.initiative-determination\"",
                StringComparison.Ordinal));
    }

    [Fact]
    public void MarkdownSummarySurfacesIdentitySeedOutcomeAndProofStatus()
    {
        var manifest = ExerciseManifestCodecTests.Create(
            detail: ExerciseDetail.Forensic,
            buildMode: ExerciseBuildMode.Baseline);
        var execution = ExerciseExecutor.Execute(
            manifest,
            TestContext.Current.CancellationToken);
        var readjudication = ReadjudicationVerifier.Verify(manifest, execution);
        var checks = execution.CheckResults.WithReadjudication(readjudication);
        var identity = BaselineIdentity();

        var markdown = Encoding.UTF8.GetString(ExerciseSummaryWriter.WriteMarkdown(
            manifest,
            identity,
            execution,
            execution.RunResult,
            checks,
            readjudication));

        Assert.Contains("- Detail: forensic\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Root seed: 0\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Build mode: baseline\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Baseline eligible: yes\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Reproducible: yes\n", markdown, StringComparison.Ordinal);
        Assert.Contains(
            "- Terminal outcome: boundary land.position.operation-1.organization\n",
            markdown,
            StringComparison.Ordinal);
        Assert.Contains("- Reconstruction verified: yes\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Re-adjudication verified: yes\n", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void ControllerConfigurationIdentityHasItsOwnVersionedGolden()
    {
        var hash = ExerciseConfigurationIdentity.ComputeHash(
            ExerciseManifestCodecTests.Create());

        Assert.Equal(
            "sha256:38ed28be6562e5d5967d838b0d264c3b52bcae77a5e61d122a282b7b91c16f0b",
            hash);
    }

    private static string Hash(byte[] value) =>
        Convert.ToHexStringLower(SHA256.HashData(value));

    private static int CountRecords(byte[] value) => value.Count(item => item == (byte)'\n');

    private static DetailRun Run(ExerciseDetail detail)
    {
        var manifest = ExerciseManifestCodecTests.Create(detail: detail);
        var execution = ExerciseExecutor.Execute(manifest, CancellationToken.None);
        var readjudication = ReadjudicationVerifier.Verify(manifest, execution);
        var checks = execution.CheckResults.WithReadjudication(readjudication);
        var evidence = new[]
        {
            ExerciseEvidenceWriter.WriteAcceptedActions(execution),
            ExerciseEvidenceWriter.WriteCanonicalEvents(execution),
            ExerciseEvidenceWriter.WriteStepEvidence(execution),
            execution.InitialSnapshot,
            execution.FinalSnapshot,
            SeedLedgerCodec.Serialize(execution.SeedLedger),
            ExerciseCheckResultsCodec.Serialize(checks),
            ReplayProofCodec.Serialize(execution.Reconstruction!),
            ReplayProofCodec.Serialize(readjudication),
        }.Select(Hash).ToArray();
        return new DetailRun(
            evidence,
            ExerciseDiagnosticsWriter.Write(
                manifest,
                execution,
                execution.RunResult,
                checks,
                readjudication));
    }

    private static string[] EventNames(byte[] diagnostics) =>
        Encoding.UTF8.GetString(diagnostics)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                using var document = JsonDocument.Parse(line);
                return document.RootElement.GetProperty("event").GetString()!;
            })
            .ToArray();

    private static BuildIdentity BaselineIdentity() => new(
        ExerciseBuildMode.Baseline,
        new string('1', 40),
        new string('2', 40),
        false,
        Sha('0'),
        ".NET 10.0.11",
        "arm64",
        "arm64",
        Cna1979Ruleset.Manifest.Hash,
        Sha('b'),
        Sha('c'),
        ExerciseSeedLedger.SchemeId,
        true,
        true,
        [new BuildArtifactIdentity("runner.dll", 12, Sha('d'))]);

    private static string Sha(char value) => $"sha256:{new string(value, 64)}";

    private static CampaignLegalActionSet CreateEmptyActionSet(CampaignLegalActionSet set)
    {
        var constructor = Assert.Single(typeof(CampaignLegalActionSet).GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic),
            value => value.GetParameters().Length == 6);
        return Assert.IsType<CampaignLegalActionSet>(constructor.Invoke(
        [
            set.CampaignId,
            set.StateVersion,
            set.RulesetHash,
            set.PositionId,
            set.Audience,
            Array.Empty<CampaignActionCandidate>(),
        ]));
    }

    private sealed class NoActiveAudienceRuntime : IExerciseExecutionRuntime
    {
        internal static NoActiveAudienceRuntime Instance { get; } = new();

        private readonly CoreExerciseExecutionRuntime inner = CoreExerciseExecutionRuntime.Instance;

        public ExerciseStartResult Begin(CampaignCreationRequest request) => inner.Begin(request);

        public ExerciseCheckpoint QueryCheckpoint(ExerciseSession session) =>
            inner.QueryCheckpoint(session);

        public ExerciseRuntimeQueryResult Query(
            ExerciseSession session,
            CampaignActionAudience audience)
        {
            var set = inner.Query(session, audience).ActionSet!;
            return new ExerciseRuntimeQueryResult(
                true,
                CreateEmptyActionSet(set));
        }

        public ExerciseControllerSelection Select(
            ExerciseControllerManifest policies,
            IReadOnlyList<ExerciseControllerActionSet> actionSets) =>
            inner.Select(policies, actionSets);

        public ExerciseRuntimeStepResult Submit(
            ExerciseSession session,
            CampaignActionSubmission submission) => inner.Submit(session, submission);

        public ReconstructionProof Reconstruct(ExerciseSession session) =>
            inner.Reconstruct(session);
    }

    private sealed record DetailRun(string[] Evidence, byte[] Diagnostics);
}
