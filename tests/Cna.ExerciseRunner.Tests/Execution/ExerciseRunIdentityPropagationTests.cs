using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseRunIdentityPropagationTests
{
    [Fact]
    public void ManeuverIdentityBindsExecutionLedgerCampaignAndReadjudication()
    {
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Forensic);
        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            "rules-lab.serial",
            2,
            null);

        var execution = ExerciseExecutor.Execute(
            manifest,
            identity,
            TestContext.Current.CancellationToken);
        var proof = ReadjudicationVerifier.Verify(manifest, execution);

        Assert.True(execution.IsSucceeded);
        Assert.Equal(identity, execution.SeedLedger.Identity);
        Assert.All(execution.Steps, step => Assert.Equal(
            ExerciseCampaignId.Derive(identity),
            step.Receipt.CampaignId));
        Assert.True(proof.IsVerified);

        var diagnostics = ExerciseDiagnosticsWriter.Write(
            manifest,
            execution,
            execution.RunResult,
            execution.CheckResults,
            proof);
        Assert.All(JsonLines(diagnostics), record =>
        {
            Assert.Equal("rules-lab.serial", record.GetProperty("maneuverId").GetString());
            Assert.Equal("organization-boundary", record.GetProperty("exerciseId").GetString());
            Assert.Equal(manifest.RootSeed, record.GetProperty("rootSeed").GetUInt64());
            Assert.Equal(2, record.GetProperty("exerciseOrdinal").GetInt32());
            Assert.Equal(JsonValueKind.Null, record.GetProperty("pairKey").ValueKind);
            Assert.Equal("unpaired", record.GetProperty("variant").GetString());
        });
    }

    [Fact]
    public void ZeroStepManeuverFailureUsesLedgerIdentityInSummariesAndDiagnostics()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            "rules-lab.serial",
            4,
            null);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

#pragma warning disable xUnit1051 // An already-cancelled token is the behavior under test.
        var execution = ExerciseExecutor.Execute(manifest, identity, cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Empty(execution.Steps);
        using var summary = JsonDocument.Parse(ExerciseSummaryWriter.WriteJson(
            manifest,
            execution,
            execution.RunResult,
            execution.CheckResults,
            null));
        Assert.Equal(
            ExerciseCampaignId.Derive(identity),
            summary.RootElement.GetProperty("campaignId").GetString());
        Assert.Equal("rules-lab.serial", summary.RootElement.GetProperty("maneuverId").GetString());
        Assert.Equal(manifest.RootSeed, summary.RootElement.GetProperty("rootSeed").GetUInt64());
        Assert.Equal(4, summary.RootElement.GetProperty("exerciseOrdinal").GetInt32());
        Assert.Equal(JsonValueKind.Null, summary.RootElement.GetProperty("pairKey").ValueKind);
        Assert.Equal("unpaired", summary.RootElement.GetProperty("variant").GetString());

        var markdown = Encoding.UTF8.GetString(ExerciseSummaryWriter.WriteMarkdown(
            manifest,
            execution,
            execution.RunResult,
            execution.CheckResults));
        Assert.Contains("- Maneuver: rules-lab.serial\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Exercise ordinal: 4\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Pair key: none\n", markdown, StringComparison.Ordinal);
        Assert.Contains("- Variant: unpaired\n", markdown, StringComparison.Ordinal);

        var completion = Assert.Single(JsonLines(ExerciseDiagnosticsWriter.Write(
            manifest,
            execution,
            execution.RunResult)));
        Assert.Equal("rules-lab.serial", completion.GetProperty("maneuverId").GetString());
        Assert.Equal(manifest.RootSeed, completion.GetProperty("rootSeed").GetUInt64());
        Assert.Equal(4, completion.GetProperty("exerciseOrdinal").GetInt32());
        Assert.Equal(JsonValueKind.Null, completion.GetProperty("pairKey").ValueKind);
        Assert.Equal("unpaired", completion.GetProperty("variant").GetString());
    }

    [Fact]
    public void ExplicitStandaloneIdentityPreservesExistingExecutionAndArtifactBytes()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var identity = ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed);

        var existing = ExerciseExecutor.Execute(
            manifest,
            TestContext.Current.CancellationToken);
        var explicitIdentity = ExerciseExecutor.Execute(
            manifest,
            identity,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            SeedLedgerCodec.Serialize(existing.SeedLedger),
            SeedLedgerCodec.Serialize(explicitIdentity.SeedLedger));
        Assert.Equal(
            ExerciseSummaryWriter.WriteJson(
                manifest,
                existing,
                existing.RunResult,
                existing.CheckResults,
                null),
            ExerciseSummaryWriter.WriteJson(
                manifest,
                explicitIdentity,
                explicitIdentity.RunResult,
                explicitIdentity.CheckResults,
                null));
        Assert.Equal(
            ExerciseDiagnosticsWriter.Write(manifest, existing, existing.RunResult),
            ExerciseDiagnosticsWriter.Write(
                manifest,
                explicitIdentity,
                explicitIdentity.RunResult));
    }

    private static JsonElement[] JsonLines(byte[] value) => Encoding.UTF8.GetString(value)
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(ParseJson)
        .ToArray();

    private static JsonElement ParseJson(string value)
    {
        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
