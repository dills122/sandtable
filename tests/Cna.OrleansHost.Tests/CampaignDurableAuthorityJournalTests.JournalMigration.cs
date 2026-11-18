using System.Diagnostics;
using System.Text.Json;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task RegisteredMigrationStepsActuallyRunOnOpenExistingAndCarryTheFileForward()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "sandtable-authority-migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(directory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            using var result = await RunJournalMigrationChild(path, OperatorSecret);
            Assert.True(result.RootElement.GetProperty("Completed").GetBoolean());
            var baseline = result.RootElement.GetProperty("BaselineSchemaVersion").GetInt64();

            // The two-step synthetic chain (see FoundationChild's RegisterSyntheticMigrationSteps)
            // must have carried the file all the way to baseline+2, not stopped partway.
            Assert.Equal(baseline + 2, result.RootElement.GetProperty("FinalUserVersion").GetInt64());
            Assert.Equal(1L, result.RootElement.GetProperty("MarkerCount").GetInt64());

            // A completely separate, fresh process (this test process -- its MigrationSteps list is
            // empty, exactly like production) must still see the migration's effect when it opens
            // the real configured path directly: proof the migration was actually published there,
            // not merely visible inside the child's own in-memory registration.
            using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                connection.Open();
                using var versionCommand = connection.CreateCommand();
                versionCommand.CommandText = "PRAGMA user_version";
                Assert.Equal(baseline + 2, (long)versionCommand.ExecuteScalar()!);

                using var markerCommand = connection.CreateCommand();
                markerCommand.CommandText = "SELECT migration_marker FROM campaigns";
                Assert.Equal(1L, (long)markerCommand.ExecuteScalar()!);

                using var indexCommand = connection.CreateCommand();
                indexCommand.CommandText = "SELECT 1 FROM sqlite_master WHERE type='index' "
                    + "AND name='campaigns_migration_marker'";
                Assert.NotNull(indexCommand.ExecuteScalar());
            }

            // No leftover ".migrating-" temp artifacts from the migration itself.
            Assert.DoesNotContain(Directory.GetFiles(directory),
                leftover => Path.GetFileName(leftover).Contains(".migrating-"));

            // Note: this test's synthetic target version (baseline+2) only exists as a recognized
            // version inside a process that registered the same throwaway MigrationSteps (the child
            // above did). Re-opening it from this ordinary test process -- which registers none, like
            // production -- is deliberately NOT exercised here: that would correctly be rejected as
            // "not one this host build recognizes," which is exactly the intended safety behavior for
            // an unrecognized future schema version, not a bug.
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("after-temp-snapshot-created")]
    [InlineData("after-step-1")]
    [InlineData("after-step-2")]
    [InlineData("before-version-marking")]
    [InlineData("before-publish")]
    public async Task InterruptedMigrationNeverPublishesAHalfMigratedJournalAndRecoversOnRetry(
        string checkpoint)
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "sandtable-authority-migration-crash-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(directory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            await RunJournalMigrationCrashChild(path, OperatorSecret, checkpoint);

            // The real configured path must never show a half-migrated file, no matter how far the
            // migration got before being killed -- including the very last instant before the
            // atomic publish (checkpoint "before-publish": every step has committed into the
            // temporary copy and it is already WAL-checkpointed into a single file; the only thing
            // that has not happened yet is the rename). It must still be openable by a completely
            // ordinary, migration-step-free process and show the ORIGINAL, pre-migration schema.
            var stillOriginal = CampaignDurableAuthorityJournal.OpenExisting(path);
            Assert.NotNull(stillOriginal.ReadVerified("campaign-migration-probe"));
            using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                connection.Open();
                using var columnCommand = connection.CreateCommand();
                columnCommand.CommandText = "SELECT 1 FROM pragma_table_info('campaigns') "
                    + "WHERE name='migration_marker'";
                Assert.Null(columnCommand.ExecuteScalar());
            }
            // The "still pre-migration" verification just above legitimately opens its own
            // connections to `path` from this (long-lived) test process -- and Connect()'s pooling
            // can keep one parked afterward. A WAL-mode TRUNCATE checkpoint needs no OTHER process
            // to be holding the file open at all (that is the whole point of the retry child's own
            // crash-safety checkpoint before it republishes), so clear this process's pools before
            // handing off to the retry child, or its cross-process checkpoint could see this test
            // process itself as a still-attached reader and refuse to fully truncate.
            SqliteConnection.ClearAllPools();

            // Retrying the migration (a fresh child process, run to completion) must recover cleanly
            // and actually finish the job.
            using var retried = await RunJournalMigrationChildAgainstExistingFile(path, OperatorSecret);
            Assert.True(retried.RootElement.GetProperty("Completed").GetBoolean());
            var baseline = retried.RootElement.GetProperty("BaselineSchemaVersion").GetInt64();
            Assert.Equal(baseline + 2, retried.RootElement.GetProperty("FinalUserVersion").GetInt64());

            // No leftover ".migrating-" temp artifacts: the killed attempt's orphan (if it got far
            // enough to create one) is swept by the retry's own orphan sweep.
            Assert.DoesNotContain(Directory.GetFiles(directory),
                leftover => Path.GetFileName(leftover).Contains(".migrating-"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LegacySchemaVersionFileIsStillHandledByExistingReadOnlyBranchesNotTheNewMigrationPath()
    {
        // No migration steps are registered in this (ordinary, shared) test process -- exactly like
        // production today -- so a file at a genuinely historical version (predating this
        // mechanism) must be completely unaffected by the new code path: still opened read-only via
        // the existing legacy branches, never routed through RunMigrations, never mutated in place.
        // Downgrading to a real schema-5 file (including recomputing its legacy-format head_hash)
        // mirrors SchemaFiveCampaignRemainsReadableButCannotAcquireImplicitTiming's own fixture setup
        // elsewhere in this test class.
        using var fixture = new JournalFixture();
        var request = Request();
        fixture.Create().CreateCampaign(request);
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            byte[][] evidence;
            using (var query = connection.CreateCommand())
            {
                query.CommandText = "SELECT creation_event,initial_snapshot,initial_contacts "
                    + "FROM campaigns WHERE campaign_id=$id";
                query.Parameters.AddWithValue("$id", request.CampaignId);
                using var reader = query.ExecuteReader();
                Assert.True(reader.Read());
                evidence = [reader.GetFieldValue<byte[]>(0), reader.GetFieldValue<byte[]>(1),
                    reader.GetFieldValue<byte[]>(2)];
            }
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("campaign-durable-authority-v2:creation");
                writer.Write(request.ContractVersion);
                writer.Write(request.CampaignId);
                writer.Write(request.RulesetHash);
                writer.Write(request.Seed);
                writer.Write(request.SetupId);
                writer.Write(request.SetupHash);
                writer.Write(request.ContentPackId);
                writer.Write(request.ContentHash);
                writer.Write(request.ScenarioId);
                foreach (var bytes in evidence)
                {
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }
            }
            using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE campaigns SET head_hash=$hash WHERE campaign_id=$id";
                update.Parameters.AddWithValue("$hash", "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(stream.ToArray())));
                update.Parameters.AddWithValue("$id", request.CampaignId);
                Assert.Equal(1, update.ExecuteNonQuery());
            }
            using var change = connection.CreateCommand();
            change.CommandText = "ALTER TABLE campaigns DROP COLUMN timing_configuration; "
                + "PRAGMA user_version=5";
            change.ExecuteNonQuery();
        }
        var legacy = fixture.Open();
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        // Still read-only: the legacy write-gate still fires exactly as before this change.
        Assert.Throws<InvalidOperationException>(() => legacy.CreateCampaign(request));
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "PRAGMA user_version";
            // The new migration mechanism never engaged for this version: still exactly 5, not
            // carried forward to anything else.
            Assert.Equal(5L, (long)versionCommand.ExecuteScalar()!);
        }
    }

    private static async Task<JsonDocument> RunJournalMigrationChild(string path, string operatorSecret)
    {
        return await RunFoundationChild("--native-journal-migration-probe",
            JsonSerializer.Serialize(new { Path = path, OperatorSecret = operatorSecret }),
            expectPause: false, stopAtCheckpoint: null);
    }

    private static async Task<JsonDocument> RunJournalMigrationChildAgainstExistingFile(
        string path, string operatorSecret)
    {
        // Unlike RunJournalMigrationChild, this does not CreateNew -- `path` already has a
        // pre-migration file sitting at it (left behind by a killed migration attempt), and
        // CreateNew would refuse to run at all since something already exists there. This mode
        // registers the same synthetic steps and migrates the existing file directly.
        return await RunFoundationChild("--native-journal-migration-retry-probe",
            JsonSerializer.Serialize(new { Path = path, OperatorSecret = operatorSecret }),
            expectPause: false, stopAtCheckpoint: null);
    }

    private static async Task RunJournalMigrationCrashChild(string path, string operatorSecret,
        string stopAtCheckpoint)
    {
        await RunFoundationChild("--native-journal-migration-crash-probe",
            JsonSerializer.Serialize(
                new { Path = path, OperatorSecret = operatorSecret, StopAtCheckpoint = stopAtCheckpoint }),
            expectPause: true, stopAtCheckpoint: stopAtCheckpoint);
    }

    private static async Task<JsonDocument> RunFoundationChild(string mode, string inputJson,
        bool expectPause, string? stopAtCheckpoint)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child), $"Built native child missing: {child}");
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add(mode);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Cannot start journal migration child.");
        var stderr = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteLineAsync(inputJson);
        process.StandardInput.Close();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string? line;
        try { line = await process.StandardOutput.ReadLineAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Journal migration child produced no output within 30 seconds. stderr={await stderr}");
        }
        if (line is null)
        {
            using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(exitTimeout.Token);
            throw new InvalidOperationException(
                $"Journal migration child exited without producing output. "
                + $"Exit code {process.ExitCode}. stderr={await stderr}");
        }
        if (!expectPause)
        {
            using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(exitTimeout.Token);
            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    $"Journal migration child exited with {process.ExitCode}. stderr={await stderr}");
            return JsonDocument.Parse(line);
        }
        using var paused = JsonDocument.Parse(line);
        Assert.Equal(stopAtCheckpoint, paused.RootElement.GetProperty("Phase").GetString());
        process.Kill(entireProcessTree: true);
        using var killTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await process.WaitForExitAsync(killTimeout.Token);
        return JsonDocument.Parse("{}");
    }
}
