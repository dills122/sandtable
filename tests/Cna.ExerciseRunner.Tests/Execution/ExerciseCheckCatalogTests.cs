using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseCheckCatalogTests
{
    [Fact]
    public void SuccessfulExecutionProducesTheExactOrderedCatalog()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var result = Execute(manifest);

        Assert.True(result.IsSucceeded);
        Assert.Equal(37, result.CheckResults.Results.Count);
        for (var step = 0; step < result.Steps.Count; step++)
        {
            var offset = step * 7;
            Assert.Equal(
                new[]
                {
                    (ExerciseCheckId.AuthorityQueryValid, (CampaignActionAudience?)CampaignActionAudience.System),
                    (ExerciseCheckId.AuthorityQueryValid, CampaignActionAudience.Axis),
                    (ExerciseCheckId.AuthorityQueryValid, CampaignActionAudience.Commonwealth),
                    (ExerciseCheckId.ActiveAudienceCardinality, null),
                    (ExerciseCheckId.SelectedActionMembership, result.Steps[step].Audience),
                    (ExerciseCheckId.AcceptedEventCardinality, result.Steps[step].Audience),
                    (ExerciseCheckId.CheckpointContinuity, result.Steps[step].Audience),
                },
                result.CheckResults.Results.Skip(offset).Take(7)
                    .Select(check => (check.CheckId, check.Audience)));
            Assert.All(
                result.CheckResults.Results.Skip(offset).Take(7),
                check =>
                {
                    Assert.Equal(step, check.StepOrdinal);
                    Assert.True(check.IsPassed);
                });
        }
        Assert.DoesNotContain(
            result.CheckResults.Results,
            check => check.StepOrdinal == result.Steps.Count);
        Assert.Equal(
            ExerciseCheckId.TerminalBoundary,
            result.CheckResults.Results[^2].CheckId);
        Assert.Equal(
            ExerciseCheckId.HistoryReconstruction,
            result.CheckResults.Results[^1].CheckId);

        var proof = ReadjudicationVerifier.Verify(manifest, result);
        var complete = result.CheckResults.WithReadjudication(proof);

        Assert.Equal(38, complete.Results.Count);
        Assert.Equal(ExerciseCheckId.Readjudication, complete.Results[^1].CheckId);
        Assert.True(complete.Results[^1].IsPassed);
        Assert.Equal(
            SeedLedgerCodec.Serialize(ExerciseSeedLedger.Create(
                ExerciseRunIdentity.Standalone(manifest.ExerciseId, manifest.RootSeed))),
            SeedLedgerCodec.Serialize(result.SeedLedger));
        var bytes = ExerciseCheckResultsCodec.Serialize(complete);
        Assert.Equal(bytes, ExerciseCheckResultsCodec.Serialize(
            ExerciseCheckResultsCodec.Deserialize(bytes)));
    }

    [Fact]
    public void StepLimitAppendsOneFailedTerminalCheckAndStops()
    {
        var result = Execute(ExerciseManifestCodecTests.Create(maximumSteps: 4));

        Assert.False(result.IsSucceeded);
        Assert.Equal(29, result.CheckResults.Results.Count);
        var terminal = result.CheckResults.Results[^1];
        Assert.Equal(ExerciseCheckId.TerminalBoundary, terminal.CheckId);
        Assert.False(terminal.IsPassed);
        Assert.Equal(
            ExerciseCheckFailureCode.TerminalBoundaryNotReached,
            terminal.FailureCode);
        Assert.DoesNotContain(
            result.CheckResults.Results,
            check => check.CheckId is ExerciseCheckId.HistoryReconstruction
                or ExerciseCheckId.Readjudication);
    }

    [Fact]
    public void CheckResultHasExactStrictCanonicalVersionOneShape()
    {
        var checks = new ExerciseCheckResults(
            [ExerciseCheckResult.Passed(ExerciseCheckId.TerminalBoundary, null, null)]);

        var bytes = ExerciseCheckResultsCodec.Serialize(checks);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Equal(
            "{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-checks.v1\",\"results\":[{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-checks.v1\",\"checkId\":\"terminal-boundary\",\"stepOrdinal\":null,\"audience\":null,\"status\":\"passed\",\"failureCode\":null}]}",
            json);
        Assert.Equal(json, Encoding.UTF8.GetString(ExerciseCheckResultsCodec.Serialize(
            ExerciseCheckResultsCodec.Deserialize(bytes))));

        string[] invalid =
        [
            json.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1,\"schemeId\"", "\"schemeId\":\"duplicate\",\"contractVersion\":1,\"schemeId\"", StringComparison.Ordinal),
            json.Replace("\"checkId\":\"terminal-boundary\"", "\"checkId\":\"unknown\"", StringComparison.Ordinal),
            json.Replace("\"status\":\"passed\"", "\"status\":\"failed\"", StringComparison.Ordinal),
            json.Replace("\"stepOrdinal\":null", "\"stepOrdinal\":0", StringComparison.Ordinal),
        ];
        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ExerciseCheckResultsCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void CatalogAllowsOnlyTheRequiredTerminalRecordAfterAStepFailure()
    {
        ExerciseCheckResult[] prefix =
        [
            ExerciseCheckResult.Passed(
                ExerciseCheckId.AuthorityQueryValid,
                0,
                CampaignActionAudience.System),
            ExerciseCheckResult.Passed(
                ExerciseCheckId.AuthorityQueryValid,
                0,
                CampaignActionAudience.Axis),
            ExerciseCheckResult.Passed(
                ExerciseCheckId.AuthorityQueryValid,
                0,
                CampaignActionAudience.Commonwealth),
        ];
        var failed = ExerciseCheckResult.Failed(
            ExerciseCheckId.ActiveAudienceCardinality,
            0,
            null,
            ExerciseCheckFailureCode.NoActiveAudience);
        var terminal = ExerciseCheckResult.Failed(
            ExerciseCheckId.TerminalBoundary,
            null,
            null,
            ExerciseCheckFailureCode.TerminalBoundaryNotReached);
        var later = ExerciseCheckResult.Passed(
            ExerciseCheckId.HistoryReconstruction,
            null,
            null);

        _ = new ExerciseCheckResults([.. prefix, failed, terminal]);
        Assert.Throws<ArgumentException>(() =>
            new ExerciseCheckResults([.. prefix, failed, terminal, later]));
    }

    private static ExerciseExecutionResult Execute(ExerciseManifest manifest) =>
        ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
}
