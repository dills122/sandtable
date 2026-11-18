using System.Diagnostics;
using System.Text.Json;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData("after-temp-file-created")]
    [InlineData("mid-schema")]
    [InlineData("before-version-marking")]
    [InlineData("before-publish")]
    public async Task InterruptedFirstTimeInitializationNeverPublishesAPartialJournalAndRecoversOnRetry(
        string checkpoint)
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "sandtable-authority-create-crash-" + Guid.NewGuid().ToString("N"));
        // Deliberately not pre-created: CreateNew itself must create and chmod it (same as every
        // other fresh-journal path in this suite), so this matches the real first-run shape.
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            await RunJournalCreateCrashChild(path, OperatorSecret, checkpoint);

            // The real configured path must never show a half-initialized file, no matter how far
            // CreateNew got before being killed -- including the very last instant before the
            // atomic publish (checkpoint "before-publish": schema, metadata and the version marker
            // are all already written, the file is already permissioned and WAL-checkpointed --
            // the only thing that has not happened yet is the rename).
            Assert.False(File.Exists(path));
            // The killed attempt's temp file (if it got far enough to create one) is left behind
            // under its private, never-the-real-path name; it is swept on the next CreateNew below.
            // EnsureHostIdentity's own small shared host-identity file (design doc §3: operator
            // capability is host-wide, not per-campaign -- see its remarks) is a legitimate,
            // already-fully-published file by this point, not a leftover: it completes before
            // CreateNew's per-campaign checkpoints (all four InlineData values here) even start.
            var hostIdentityPath = CampaignDurableAuthorityJournal.HostIdentityPathForDirectory(directory);
            Assert.All(Directory.GetFiles(directory),
                leftover => Assert.True(leftover == hostIdentityPath
                    || Path.GetFileName(leftover).Contains(".creating-", StringComparison.Ordinal),
                    $"Unexpected leftover file: {leftover}"));

            // Retrying CreateNew in this (fresh, non-faulted) process must recover cleanly.
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, OperatorSecret);
            Assert.True(File.Exists(path));
            if (!OperatingSystem.IsWindows())
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
            // Orphaned temp artifact from the killed attempt is swept; only the published campaign
            // file and the (already-published, untouched-by-this-retry) host-identity file remain.
            Assert.Equal([hostIdentityPath, path], Directory.GetFiles(directory).OrderBy(file => file,
                StringComparer.Ordinal));

            var session = journal.CreateCampaign(Request());
            Assert.NotNull(session);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path);
            Assert.NotNull(reopened.ReadVerified(Request().CampaignId));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExistingValidJournalIsNeverTouchedByARepeatedCreateNewAttempt()
    {
        using var fixture = new JournalFixture();
        fixture.Create();
        var before = File.ReadAllBytes(fixture.Path);
        Assert.Throws<InvalidOperationException>(
            () => CampaignDurableAuthorityJournal.CreateNew(fixture.Path, OperatorSecret));
        Assert.Equal(before, File.ReadAllBytes(fixture.Path));
        var reopened = CampaignDurableAuthorityJournal.OpenExisting(fixture.Path);
        Assert.NotNull(reopened.CreateCampaign(Request()));
    }

    [Fact]
    public void UnrecognizedGarbageAtTheJournalPathIsRejectedWithoutBeingTouchedByEitherEntryPoint()
    {
        var directory = Path.Combine(Path.GetTempPath(),
            "sandtable-authority-garbage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(directory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(directory, "campaign.sqlite");
        var garbage = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        File.WriteAllBytes(path, garbage);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        try
        {
            Assert.Throws<InvalidOperationException>(
                () => CampaignDurableAuthorityJournal.CreateNew(path, OperatorSecret));
            Assert.Equal(garbage, File.ReadAllBytes(path));
            var openException = Assert.Throws<InvalidOperationException>(
                () => CampaignDurableAuthorityJournal.OpenExisting(path));
            Assert.Contains("could not be read as a SQLite database", openException.Message);
            Assert.Equal(garbage, File.ReadAllBytes(path));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FutureOrUnrecognizedSchemaVersionAtTheJournalPathIsRejectedWithGuidanceAndLeftUntouched()
    {
        using var fixture = new JournalFixture();
        fixture.Create();
        using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version=999999";
            command.ExecuteNonQuery();
        }
        var before = File.ReadAllBytes(fixture.Path);
        var openException = Assert.Throws<InvalidOperationException>(
            () => CampaignDurableAuthorityJournal.OpenExisting(fixture.Path));
        Assert.Contains("not one this host build recognizes", openException.Message);
        Assert.Equal(before, File.ReadAllBytes(fixture.Path));
        Assert.Throws<InvalidOperationException>(
            () => CampaignDurableAuthorityJournal.CreateNew(fixture.Path, OperatorSecret));
        Assert.Equal(before, File.ReadAllBytes(fixture.Path));
    }

    private static async Task RunJournalCreateCrashChild(string path, string operatorSecret,
        string stopAtCheckpoint)
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
        start.ArgumentList.Add("--native-journal-create-crash-probe");
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Cannot start journal create crash child.");
        var stderr = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(
            new { Path = path, OperatorSecret = operatorSecret, StopAtCheckpoint = stopAtCheckpoint }));
        process.StandardInput.Close();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string? line;
        try { line = await process.StandardOutput.ReadLineAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Journal create crash child produced no output within 30 seconds. stderr={await stderr}");
        }
        if (line is null)
        {
            using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(exitTimeout.Token);
            throw new InvalidOperationException(
                $"Journal create crash child exited without pausing at '{stopAtCheckpoint}'. "
                + $"Exit code {process.ExitCode}. stderr={await stderr}");
        }
        using var paused = JsonDocument.Parse(line);
        Assert.Equal(stopAtCheckpoint, paused.RootElement.GetProperty("Phase").GetString());
        process.Kill(entireProcessTree: true);
        using var killTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await process.WaitForExitAsync(killTimeout.Token);
    }
}
