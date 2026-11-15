using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed class GrazianiHostStressTests
{
    private const string Secret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SourceOwnerProcessDeathBeforeWriteOrAfterCommitRecoversOnce(bool afterCommit)
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-source-crash-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-source-crash", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, initialization),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, initialization);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var finish = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, Assert.Single(finish.PublicActionRefs)).IsAccepted);
            var opening = journal.QuerySourceActionsOpening(axis);
            var actionRef = opening.PublicActionRefs[ChoiceIndexes(opening.VisibleBytes, "axis-dump")[0]];

            // The before-write pause is at seat authentication, before insertion.
            // The after-commit pause occurs after submission returns but before
            // the child sends an owner response. Neither claims a mid-write kill.
            await KillSourceOwnerProbe(path, axis, opening.Revision, actionRef, afterCommit);
            Assert.Equal(afterCommit ? 3 : 2, StepCount(path, request.CampaignId));
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            Assert.Equal(afterCommit ? 4 : 3, CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId)).StateVersion);
            var accepted = reopened.SubmitSourceActions(axis, opening.Revision, actionRef);
            Assert.True(accepted.IsAccepted);
            var retry = CampaignDurableAuthorityJournal.OpenExisting(path).SubmitSourceActions(axis, opening.Revision, actionRef);
            Assert.True(retry.IsAccepted);
            Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
            Assert.Equal(3, StepCount(path, request.CampaignId));
            await AssertFreshProcessReplay(path, request.CampaignId, 4);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("BEFORE")]
    [InlineData("AFTER")]
    public async Task SourceHeadWriteAbortRollsBackEventAndAllowsExactlyOnceRetry(string timing)
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-write-abort-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-write-abort", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, initialization),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, initialization);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var finish = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, Assert.Single(finish.PublicActionRefs)).IsAccepted);
            var opening = journal.QuerySourceActionsOpening(axis);
            var actionRef = opening.PublicActionRefs[ChoiceIndexes(opening.VisibleBytes, "axis-dump")[0]];

            // SQLite aborts the head update after accepted_steps was inserted. AFTER
            // also executes the head update before aborting. These are transaction
            // rollback tests, not claims about process death at either boundary.
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var sql = connection.CreateCommand();
                sql.CommandText = $"CREATE TRIGGER source_abort {timing} UPDATE OF head_sequence ON campaigns BEGIN SELECT RAISE(ABORT, 'source-head-abort'); END";
                sql.ExecuteNonQuery();
            }
            var failure = Assert.Throws<SqliteException>(() => journal.SubmitSourceActions(axis, opening.Revision, actionRef));
            Assert.Contains("source-head-abort", failure.Message, StringComparison.Ordinal);
            Assert.Equal(2, StepCount(path, request.CampaignId));
            Assert.Equal(3, CampaignExercises.QueryCheckpoint(CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId)).StateVersion);
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var sql = connection.CreateCommand();
                sql.CommandText = "DROP TRIGGER source_abort";
                sql.ExecuteNonQuery();
            }
            var accepted = journal.SubmitSourceActions(axis, opening.Revision, actionRef);
            Assert.True(accepted.IsAccepted);
            var retry = CampaignDurableAuthorityJournal.OpenExisting(path).SubmitSourceActions(axis, opening.Revision, actionRef);
            Assert.True(retry.IsAccepted);
            Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
            Assert.Equal(3, StepCount(path, request.CampaignId));
            await AssertFreshProcessReplay(path, request.CampaignId, 4);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task HiddenAxisChoicePreservesPreviouslyIssuedCommonwealthOfferAcrossRestart()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-cw-offer-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-cw-offer", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, initialization),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, initialization);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var finish = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, Assert.Single(finish.PublicActionRefs)).IsAccepted);
            var cw = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, Secret)));
            var cwOpening = journal.QuerySourceActionsOpening(cw);
            var cwRef = cwOpening.PublicActionRefs[ChoiceIndexes(cwOpening.VisibleBytes, "cw-supply")[0]];
            var axisOpening = journal.QuerySourceActionsOpening(axis);
            var axisRef = axisOpening.PublicActionRefs[ChoiceIndexes(axisOpening.VisibleBytes, "axis-dump")[0]];
            Assert.True(journal.SubmitSourceActions(axis, axisOpening.Revision, axisRef).IsAccepted);
            var afterHidden = CampaignExercises.QuerySourceActionsDraft(journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
            Assert.Equal(cwOpening.VisibleBytes, afterHidden.VisibleBytes);

            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            var accepted = reopened.SubmitSourceActions(cw, cwOpening.Revision, cwRef);
            Assert.True(accepted.IsAccepted);
            var retry = CampaignDurableAuthorityJournal.OpenExisting(path).SubmitSourceActions(cw, cwOpening.Revision, cwRef);
            Assert.True(retry.IsAccepted);
            Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
            Assert.Equal(4, StepCount(path, request.CampaignId));
            Assert.Equal(5, CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId)).StateVersion);
            await AssertFreshProcessReplay(path, request.CampaignId, 5);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task HiddenOpposingChoicePreservesOwnerOfferConcurrentRetriesAndTamperChecks()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-host-stress-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var content = Cna1979GrazianiCairoSourceExcerpt.Build();
            var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                "graziani-host-stress", Cna1979Ruleset.Manifest.Hash, 0,
                CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, initialization),
                content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            journal.CreatePregameSourceCampaign(request, content, initialization);
            var axis = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret)));
            var roll = journal.QueryPregameAxisSupplyOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupply(axis, roll.Revision, Assert.Single(roll.PublicActionRefs)).IsAccepted);
            var finish = journal.QueryPregameAxisSupplyFinishOpening(axis);
            Assert.True(journal.SubmitPregameAxisSupplyFinish(axis, finish.Revision, Assert.Single(finish.PublicActionRefs)).IsAccepted);
            var cw = Assert.IsType<CampaignSeatCredential>(journal.RedeemSeatGrant(
                journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, Secret)));
            var axisOpening = journal.QuerySourceActionsOpening(axis);
            var cwOpening = journal.QuerySourceActionsOpening(cw);
            var axisIndex = ChoiceIndexes(axisOpening.VisibleBytes, "axis-dump").First();
            var cwIndex = ChoiceIndexes(cwOpening.VisibleBytes, "cw-supply").First();
            var axisRef = axisOpening.PublicActionRefs[axisIndex];
            var cwRef = cwOpening.PublicActionRefs[cwIndex];
            Assert.False(journal.SubmitSourceActions(cw, cwOpening.Revision, axisRef).IsAccepted);
            Assert.Equal(2, StepCount(path, request.CampaignId));
            Assert.True(journal.SubmitSourceActions(cw, cwOpening.Revision, cwRef).IsAccepted);
            var afterHiddenAction = CampaignExercises.QuerySourceActionsDraft(journal.ReadVerified(request.CampaignId), CampaignActionAudience.Axis);
            Assert.Equal(axisOpening.VisibleBytes, afterHiddenAction.VisibleBytes);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstAxis = Task.Run(async () => { await start.Task; return journal.SubmitSourceActions(axis, axisOpening.Revision, axisRef); });
            var secondAxis = Task.Run(async () => { await start.Task; return journal.SubmitSourceActions(axis, axisOpening.Revision, axisRef); });
            start.SetResult();
            var results = await Task.WhenAll(firstAxis, secondAxis);
            Assert.All(results, result => Assert.True(result.IsAccepted));
            Assert.Equal(results[0].CanonicalReceipt, results[1].CanonicalReceipt);
            Assert.Equal(5, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
            Assert.Equal(4, StepCount(path, request.CampaignId));

            var refreshedAxis = journal.QuerySourceActionsOpening(axis);
            var alternatives = ChoiceIndexes(refreshedAxis.VisibleBytes, "axis-dump").Take(2).ToArray();
            Assert.Equal(2, alternatives.Length);
            start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var competing = alternatives.Select(index => Task.Run(async () =>
            {
                await start.Task;
                return journal.SubmitSourceActions(axis, refreshedAxis.Revision, refreshedAxis.PublicActionRefs[index]);
            })).ToArray();
            start.SetResult();
            var competingResults = await Task.WhenAll(competing);
            Assert.Single(competingResults, result => result.IsAccepted);
            Assert.Equal(6, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
            Assert.Equal(5, StepCount(path, request.CampaignId));
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            Assert.Equal(6, CampaignExercises.QueryCheckpoint(reopened.ReadVerified(request.CampaignId)).StateVersion);
            await AssertFreshProcessReplay(path, request.CampaignId, 6);
            var beforeTamper = reopened.QuerySourceActionsOpening(cw);
            Assert.DoesNotContain("axis-dump", System.Text.Encoding.UTF8.GetString(beforeTamper.VisibleBytes), StringComparison.Ordinal);

            var currentAxis = reopened.QuerySourceActionsOpening(axis);
            var currentRef = currentAxis.PublicActionRefs[0];
            string retainedId;
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var sql = connection.CreateCommand();
                sql.CommandText = "SELECT core_action_id FROM owner_actions WHERE campaign_id=$id AND public_ref=$ref";
                sql.Parameters.AddWithValue("$id", request.CampaignId);
                sql.Parameters.AddWithValue("$ref", currentRef);
                retainedId = Assert.IsType<string>(sql.ExecuteScalar());
                sql.CommandText = "UPDATE owner_actions SET core_action_id=$forged WHERE campaign_id=$id AND public_ref=$ref";
                sql.Parameters.AddWithValue("$forged", "sha256:" + new string('0', 64));
                Assert.Equal(1, sql.ExecuteNonQuery());
            }
            Assert.False(reopened.SubmitSourceActions(axis, currentAxis.Revision, currentRef).IsAccepted);
            Assert.Equal(5, StepCount(path, request.CampaignId));
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var sql = connection.CreateCommand();
                sql.CommandText = "UPDATE owner_actions SET core_action_id=$original WHERE campaign_id=$id AND public_ref=$ref";
                sql.Parameters.AddWithValue("$id", request.CampaignId);
                sql.Parameters.AddWithValue("$ref", currentRef);
                sql.Parameters.AddWithValue("$original", retainedId);
                Assert.Equal(1, sql.ExecuteNonQuery());
            }
            var openingFilter = "campaign_id=$id AND side='axis' AND acting_element_id='land.graziani-source-actions'";
            var actionFilter = "campaign_id=$id AND public_ref=$ref";
            var originalVersion = (long)ReadCell(path, $"SELECT authority_version FROM owner_openings WHERE {openingFilter}", request.CampaignId, currentRef);
            var originalOrdinal = (long)ReadCell(path, $"SELECT ordinal FROM owner_actions WHERE {actionFilter}", request.CampaignId, currentRef);
            var originalDigest = (string)ReadCell(path, $"SELECT visible_digest FROM owner_openings WHERE {openingFilter}", request.CampaignId, currentRef);
            var originalVisible = (byte[])ReadCell(path, $"SELECT visible_bytes FROM owner_openings WHERE {openingFilter}", request.CampaignId, currentRef);
            Assert.True(originalVersion > 1);
            WriteCell(path, $"UPDATE owner_openings SET authority_version=$value WHERE {openingFilter}", request.CampaignId, currentRef, originalVersion - 1);
            Assert.False(reopened.SubmitSourceActions(axis, currentAxis.Revision, currentRef).IsAccepted);
            WriteCell(path, $"UPDATE owner_openings SET authority_version=$value WHERE {openingFilter}", request.CampaignId, currentRef, originalVersion);
            WriteCell(path, $"UPDATE owner_actions SET ordinal=$value WHERE {actionFilter}", request.CampaignId, currentRef, originalOrdinal + 1);
            Assert.False(reopened.SubmitSourceActions(axis, currentAxis.Revision, currentRef).IsAccepted);
            WriteCell(path, $"UPDATE owner_actions SET ordinal=$value WHERE {actionFilter}", request.CampaignId, currentRef, originalOrdinal);
            var malformed = "{}"u8.ToArray();
            var malformedDigest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(malformed));
            WriteCell(path, $"UPDATE owner_openings SET visible_bytes=$value WHERE {openingFilter}", request.CampaignId, currentRef, malformed);
            WriteCell(path, $"UPDATE owner_openings SET visible_digest=$value WHERE {openingFilter}", request.CampaignId, currentRef, malformedDigest);
            Assert.False(reopened.SubmitSourceActions(axis, currentAxis.Revision, currentRef).IsAccepted);
            WriteCell(path, $"UPDATE owner_openings SET visible_bytes=$value WHERE {openingFilter}", request.CampaignId, currentRef, originalVisible);
            WriteCell(path, $"UPDATE owner_openings SET visible_digest=$value WHERE {openingFilter}", request.CampaignId, currentRef, originalDigest);
            Assert.Equal(5, StepCount(path, request.CampaignId));
            Assert.True(reopened.SubmitSourceActions(axis, currentAxis.Revision, currentRef).IsAccepted);
            Assert.Equal(6, StepCount(path, request.CampaignId));

            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var sql = connection.CreateCommand();
                sql.CommandText = "UPDATE accepted_steps SET canonical_event=x'00' WHERE campaign_id=$id AND sequence=1";
                sql.Parameters.AddWithValue("$id", request.CampaignId);
                Assert.Equal(1, sql.ExecuteNonQuery());
            }
            Assert.Throws<InvalidOperationException>(() => CampaignDurableAuthorityJournal.OpenExisting(path).ReadVerified(request.CampaignId));
            Assert.Throws<UnauthorizedAccessException>(() => reopened.QuerySourceActionsOpening(cw));
            Assert.Throws<InvalidOperationException>(() => reopened.SubmitSourceActions(axis, axisOpening.Revision, axisRef));
            Assert.Equal(6, StepCount(path, request.CampaignId));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static int[] ChoiceIndexes(byte[] visible, string family)
    {
        using var json = JsonDocument.Parse(visible);
        return json.RootElement.GetProperty("choices").EnumerateArray().Select((item, index) => (item, index))
            .Where(value => value.item.GetProperty("family").GetString() == family).Select(value => value.index).ToArray();
    }

    private static long StepCount(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var sql = connection.CreateCommand();
        sql.CommandText = "SELECT count(*) FROM accepted_steps WHERE campaign_id=$id";
        sql.Parameters.AddWithValue("$id", campaignId);
        return (long)sql.ExecuteScalar()!;
    }

    private static object ReadCell(string path, string statement, string campaignId, string publicRef)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var sql = connection.CreateCommand();
        sql.CommandText = statement;
        sql.Parameters.AddWithValue("$id", campaignId);
        sql.Parameters.AddWithValue("$ref", publicRef);
        return sql.ExecuteScalar() ?? throw new InvalidOperationException("Missing stress-test row.");
    }

    private static void WriteCell(string path, string statement, string campaignId, string publicRef, object value)
    {
        using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        connection.Open();
        using var sql = connection.CreateCommand();
        sql.CommandText = statement;
        sql.Parameters.AddWithValue("$id", campaignId);
        sql.Parameters.AddWithValue("$ref", publicRef);
        sql.Parameters.AddWithValue("$value", value);
        Assert.Equal(1, sql.ExecuteNonQuery());
    }

    private static async Task AssertFreshProcessReplay(string path, string campaignId, long expectedVersion)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child));
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add("--source-authority-read");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start replay child.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { Path = path, CampaignId = campaignId }));
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(timeout.Token);
            Assert.True(process.ExitCode == 0, $"Replay child failed: {await stderr}");
            using var json = JsonDocument.Parse(await stdout);
            Assert.Equal(process.Id, json.RootElement.GetProperty("ProcessId").GetInt32());
            Assert.NotEqual(Environment.ProcessId, process.Id);
            Assert.Equal(expectedVersion, json.RootElement.GetProperty("StateVersion").GetInt64());
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }

    private static async Task KillSourceOwnerProbe(string path, CampaignSeatCredential credential,
        long revision, string reference, bool afterCommit)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child));
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add("--source-owner-crash-probe");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start source crash child.");
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new
            {
                Path = path,
                Credential = credential,
                Revision = revision,
                Reference = reference,
                NowUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                AfterCommit = afterCommit
            }));
            await process.StandardInput.FlushAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var marker = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (marker is null) throw new InvalidOperationException($"Crash child exited before pause: {await stderr}");
            using var json = JsonDocument.Parse(marker);
            Assert.Equal(afterCommit ? "source-after-commit" : "clock-read", json.RootElement.GetProperty("Phase").GetString());
            process.Kill(true);
            await process.WaitForExitAsync(timeout.Token);
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }
}
