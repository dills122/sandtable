using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static CampaignCreationRequest ScheduledWorld6Request()
    {
        var supported = Request();
        return new CampaignCreationRequest(supported.ContractVersion, supported.CampaignId,
            supported.RulesetHash, supported.Seed, "rules-lab.scheduled-world6.v1",
            "sha256:f1a99a29b552f2cdc87ea80ac6c35061a97663d7933bb54b97a19d4f3935f109",
            supported.ContentPackId, supported.ContentHash, supported.ScenarioId);
    }

    [Fact]
    public void UnadmittedArrivalProfileCannotCreateOrReserveDurableCampaign()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var supported = Request();
        var candidate = new CampaignCreationRequest(supported.ContractVersion, supported.CampaignId,
            supported.RulesetHash, supported.Seed, "rules-lab.scheduled-world6.v1",
            "sha256:f1a99a29b552f2cdc87ea80ac6c35061a97663d7933bb54b97a19d4f3935f109",
            supported.ContentPackId, supported.ContentHash, supported.ScenarioId);
        Assert.Throws<InvalidOperationException>(() => journal.CreateCampaign(candidate));
        var created = journal.CreateCampaign(supported);
        var restored = fixture.Open().ReadVerified(supported.CampaignId);
        Assert.Equal(CampaignExercises.QueryCheckpoint(created).StateVersion,
            CampaignExercises.QueryCheckpoint(restored).StateVersion);
    }

    [Fact]
    public void ScheduledArrivalCommitsOnceAndRecoversOwnerReceiptAcrossRestart()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        journal.CreateScheduledWorld6(request);
        var axisGrant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var axis = journal.RedeemSeatGrant(axisGrant)!;
        var commonwealthGrant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret);
        var commonwealth = journal.RedeemSeatGrant(commonwealthGrant)!;
        Assert.Empty(journal.QueryScheduledWorld6(commonwealth));
        var opening = Assert.Single(journal.QueryScheduledWorld6(axis));
        Assert.Equal("north-west", opening.DestinationId);
        Assert.Equal(7, opening.ExpectedVersion);
        Assert.DoesNotContain("axis-infantry", opening.Reference, StringComparison.Ordinal);
        Assert.Null(journal.SubmitScheduledWorld6(commonwealth, opening.Reference));
        Assert.Null(journal.SubmitScheduledWorld6(axis, "act.invalid"));
        var receipt = journal.SubmitScheduledWorld6(axis, opening.Reference);
        Assert.NotNull(receipt);
        var reopened = fixture.Open();
        Assert.Empty(reopened.QueryScheduledWorld6(axis));
        Assert.Empty(reopened.QueryScheduledWorld6(commonwealth));
        Assert.Equal(receipt, reopened.SubmitScheduledWorld6(axis, opening.Reference));
        reopened.CreateScheduledWorld6(request);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(7L, (long)query.ExecuteScalar()!);
        var replacement = reopened.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var newCredential = reopened.RedeemSeatGrant(replacement)!;
        Assert.Throws<UnauthorizedAccessException>(() => reopened.SubmitScheduledWorld6(axis, opening.Reference));
        Assert.Null(reopened.SubmitScheduledWorld6(newCredential, opening.Reference));
    }

    [Fact]
    public void ScheduledArrivalColdReplayRejectsRehashedForgedReceipt()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        journal.CreateScheduledWorld6(request);
        var grant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = Assert.Single(journal.QueryScheduledWorld6(credential));
        Assert.NotNull(journal.SubmitScheduledWorld6(credential, opening.Reference));
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT prior_hash,sequence,action_kind,acting_element_id,owner_public_ref,seat_epoch,owner_revision,
              canonical_command,canonical_event,canonical_receipt,snapshot_checkpoint,canonical_contacts
            FROM accepted_steps WHERE campaign_id=$id AND sequence=7
            """;
        query.Parameters.AddWithValue("$id", request.CampaignId);
        object?[] hashArguments;
        byte[] forgedReceipt;
        using (var reader = query.ExecuteReader())
        {
            Assert.True(reader.Read());
            forgedReceipt = (byte[])reader.GetValue(9);
            forgedReceipt[0] ^= 1;
            hashArguments = [reader.GetString(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), (long?)reader.GetInt64(5), (long?)reader.GetInt64(6),
                (byte[])reader.GetValue(7), (byte[])reader.GetValue(8), forgedReceipt,
                (byte[])reader.GetValue(10), (byte[])reader.GetValue(11)];
        }
        var method = typeof(Cna.OrleansHost.Api.CampaignDurableAuthorityJournal).GetMethod("StepHash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var forgedHash = (string)method.Invoke(null, hashArguments)!;
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE accepted_steps SET canonical_receipt=$receipt,record_hash=$hash WHERE campaign_id=$id AND sequence=7";
        update.Parameters.AddWithValue("$receipt", forgedReceipt);
        update.Parameters.AddWithValue("$hash", forgedHash);
        update.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1, update.ExecuteNonQuery());
        update.CommandText = "UPDATE campaigns SET head_hash=$hash WHERE campaign_id=$id";
        Assert.Equal(1, update.ExecuteNonQuery());
        transaction.Commit();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().QueryScheduledWorld6(credential));
    }

    [Fact]
    public void LegacyDigestOnlyPreambleRemainsReadableAfterNativeEventUpgrade()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        journal.CreateScheduledWorld6(request);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        string priorHash;
        byte[] contacts;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = "SELECT prior_hash,canonical_contacts FROM accepted_steps WHERE campaign_id=$id AND sequence=1";
            query.Parameters.AddWithValue("$id", request.CampaignId);
            using var reader = query.ExecuteReader();
            Assert.True(reader.Read());
            priorHash = reader.GetString(0);
            contacts = (byte[])reader.GetValue(1);
        }
        var hashMethod = typeof(Cna.OrleansHost.Api.CampaignDurableAuthorityJournal).GetMethod("StepHash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var session = CampaignScheduledWorld6Bridge.Begin(request).Session;
        for (long sequence = 1; sequence <= 6; sequence++)
        {
            var step = CampaignScheduledWorld6Bridge.AdvancePreamble(session);
            var hash = (string)hashMethod.Invoke(null, [priorHash, sequence, "scheduled-world6-preamble", null,
                null, null, null, step.CommandBytes, step.EventBytes, step.ReceiptBytes, step.CheckpointBytes, contacts])!;
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE accepted_steps SET action_kind='scheduled-world6-preamble',canonical_command=$command,
                  canonical_event=$event,canonical_receipt=$receipt,snapshot_checkpoint=$checkpoint,
                  prior_hash=$prior,record_hash=$hash WHERE campaign_id=$id AND sequence=$seq
                """;
            update.Parameters.AddWithValue("$command", step.CommandBytes);
            update.Parameters.AddWithValue("$event", step.EventBytes);
            update.Parameters.AddWithValue("$receipt", step.ReceiptBytes);
            update.Parameters.AddWithValue("$checkpoint", step.CheckpointBytes);
            update.Parameters.AddWithValue("$prior", priorHash);
            update.Parameters.AddWithValue("$hash", hash);
            update.Parameters.AddWithValue("$id", request.CampaignId);
            update.Parameters.AddWithValue("$seq", sequence);
            Assert.Equal(1, update.ExecuteNonQuery());
            priorHash = hash;
            session = step.Session;
        }
        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = "UPDATE campaigns SET head_hash=$hash WHERE campaign_id=$id";
            update.Parameters.AddWithValue("$hash", priorHash);
            update.Parameters.AddWithValue("$id", request.CampaignId);
            Assert.Equal(1, update.ExecuteNonQuery());
        }
        transaction.Commit();
        var reopened = fixture.Open();
        var grant = reopened.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var credential = reopened.RedeemSeatGrant(grant)!;
        var offer = Assert.Single(reopened.QueryScheduledWorld6(credential));
        Assert.NotNull(reopened.SubmitScheduledWorld6(credential, offer.Reference));
        Assert.Empty(fixture.Open().QueryScheduledWorld6(credential));
    }

    [Fact]
    public async Task ConcurrentScheduledArrivalRetriesCommitOnce()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        journal.CreateScheduledWorld6(request);
        var grant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = Assert.Single(journal.QueryScheduledWorld6(credential));
        var replies = await Task.WhenAll(
            Task.Run(() => fixture.Open().SubmitScheduledWorld6(credential, opening.Reference)),
            Task.Run(() => fixture.Open().SubmitScheduledWorld6(credential, opening.Reference)));
        Assert.NotNull(replies[0]);
        Assert.Equal(replies[0], replies[1]);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(7L, (long)query.ExecuteScalar()!);
    }

    [Fact]
    public async Task ScheduledArrivalRecoversAndRetriesInFreshProcesses()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        journal.CreateScheduledWorld6(request);
        var grant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = Assert.Single(journal.QueryScheduledWorld6(credential));
        var first = await RunChild();
        var retry = await RunChild();
        Assert.NotEqual(Environment.ProcessId, first.ProcessId);
        Assert.NotEqual(first.ProcessId, retry.ProcessId);
        Assert.Equal(1, first.OfferCount);
        Assert.Equal(0, retry.OfferCount);
        Assert.NotNull(first.Receipt);
        Assert.Equal(first.Receipt, retry.Receipt);
        Assert.Equal(Convert.FromBase64String(first.Receipt!), fixture.Open().SubmitScheduledWorld6(credential, opening.Reference));
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(7L, (long)query.ExecuteScalar()!);

        async Task<ScheduledArrivalChildResult> RunChild()
        {
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
            var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
                "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
            Assert.True(File.Exists(child));
            var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            start.ArgumentList.Add(child);
            start.ArgumentList.Add("--scheduled-arrival-retry");
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start recovery child.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try
            {
                await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new
                { Path = fixture.Path, Credential = credential, Reference = opening.Reference }));
                process.StandardInput.Close();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                await process.WaitForExitAsync(timeout.Token);
                var output = await stdout;
                var errors = await stderr;
                Assert.True(process.ExitCode == 0, $"Recovery child failed: {errors}");
                var result = JsonSerializer.Deserialize<ScheduledArrivalChildResult>(output)
                    ?? throw new InvalidOperationException("Missing recovery result.");
                Assert.Equal(process.Id, result.ProcessId);
                return result;
            }
            finally
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
        }
    }

    private sealed record ScheduledArrivalChildResult(int ProcessId, int OfferCount, string? Receipt);

    [Fact]
    public void ScheduledCreationAndArrivalFailuresRollbackBeforeRetry()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TRIGGER fail_scheduled_preamble BEFORE INSERT ON accepted_steps
            WHEN NEW.action_kind='scheduled-world6-preamble-v2' AND NEW.sequence=3
            BEGIN SELECT RAISE(ABORT,'test failure'); END
            """;
        command.ExecuteNonQuery();
        Assert.Throws<SqliteException>(() => journal.CreateScheduledWorld6(request));
        command.CommandText = "SELECT COUNT(*) FROM campaigns WHERE campaign_id=$id";
        command.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(0L, (long)command.ExecuteScalar()!);
        command.CommandText = "DROP TRIGGER fail_scheduled_preamble";
        command.ExecuteNonQuery();
        journal.CreateScheduledWorld6(request);
        var grant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = Assert.Single(journal.QueryScheduledWorld6(credential));
        command.CommandText = """
            CREATE TRIGGER fail_scheduled_arrival BEFORE INSERT ON accepted_steps
            WHEN NEW.action_kind='scheduled-world6-arrival'
            BEGIN SELECT RAISE(ABORT,'test failure'); END
            """;
        command.ExecuteNonQuery();
        Assert.Throws<SqliteException>(() => journal.SubmitScheduledWorld6(credential, opening.Reference));
        Assert.Equal(opening, Assert.Single(fixture.Open().QueryScheduledWorld6(credential)));
        command.CommandText = "DROP TRIGGER fail_scheduled_arrival";
        command.ExecuteNonQuery();
        Assert.NotNull(fixture.Open().SubmitScheduledWorld6(credential, opening.Reference));
        command.CommandText = "SELECT COUNT(*) FROM accepted_steps WHERE campaign_id=$id";
        Assert.Equal(7L, (long)command.ExecuteScalar()!);
    }

    [Fact]
    public void ScheduledArrivalRejectsRehashedEventWithForeignCommandId()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ScheduledWorld6Request();
        journal.CreateScheduledWorld6(request);
        var grant = journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, OperatorSecret);
        var credential = journal.RedeemSeatGrant(grant)!;
        var opening = Assert.Single(journal.QueryScheduledWorld6(credential));
        Assert.NotNull(journal.SubmitScheduledWorld6(credential, opening.Reference));

        var synthetic = CampaignScheduledWorld6Bridge.Begin(request).Session;
        for (var index = 0; index < 6; index++) synthetic = CampaignScheduledWorld6Bridge.AdvancePreamble(synthetic).Session;
        var foreign = CampaignScheduledWorld6Bridge.Arrive(synthetic, "foreign-command", 7, LandSide.Axis, "north-west");
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT prior_hash,sequence,action_kind,acting_element_id,owner_public_ref,seat_epoch,owner_revision,canonical_contacts
            FROM accepted_steps WHERE campaign_id=$id AND sequence=7
            """;
        query.Parameters.AddWithValue("$id", request.CampaignId);
        object?[] hashArguments;
        using (var reader = query.ExecuteReader())
        {
            Assert.True(reader.Read());
            hashArguments = [reader.GetString(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), (long?)reader.GetInt64(5), (long?)reader.GetInt64(6), foreign.CommandBytes,
                foreign.EventBytes, foreign.ReceiptBytes, foreign.CheckpointBytes, (byte[])reader.GetValue(7)];
        }
        var method = typeof(Cna.OrleansHost.Api.CampaignDurableAuthorityJournal).GetMethod("StepHash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var forgedHash = (string)method.Invoke(null, hashArguments)!;
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE accepted_steps SET canonical_command=$command,canonical_event=$event,canonical_receipt=$receipt,
              snapshot_checkpoint=$checkpoint,record_hash=$hash WHERE campaign_id=$id AND sequence=7
            """;
        update.Parameters.AddWithValue("$command", foreign.CommandBytes);
        update.Parameters.AddWithValue("$event", foreign.EventBytes);
        update.Parameters.AddWithValue("$receipt", foreign.ReceiptBytes);
        update.Parameters.AddWithValue("$checkpoint", foreign.CheckpointBytes);
        update.Parameters.AddWithValue("$hash", forgedHash);
        update.Parameters.AddWithValue("$id", request.CampaignId);
        Assert.Equal(1, update.ExecuteNonQuery());
        update.CommandText = "UPDATE campaigns SET head_hash=$hash WHERE campaign_id=$id";
        Assert.Equal(1, update.ExecuteNonQuery());
        transaction.Commit();
        Assert.Throws<InvalidOperationException>(() => fixture.Open().QueryScheduledWorld6(credential));
    }
}
