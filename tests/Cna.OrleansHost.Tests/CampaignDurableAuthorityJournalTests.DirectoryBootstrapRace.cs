using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    /// <summary>
    /// Regression for the TOCTOU race in <see cref="CampaignDurableAuthorityJournal.CreateNew"/>'s
    /// directory bootstrap: <c>Directory.CreateDirectory</c> makes <c>Directory.Exists</c> true
    /// before the follow-up <c>File.SetUnixFileMode</c> actually lands, so an unguarded concurrent
    /// caller could observe the directory mid-creation -- in its transient, default-umask
    /// (non-private) state -- and fail with "Journal directory permissions are not private." even
    /// though the directory was microseconds from being correctly locked down.
    /// </summary>
    /// <remarks>
    /// Many threads race <see cref="CampaignDurableAuthorityJournal.CreateNew"/> with distinct
    /// per-campaign paths into one fresh, never-before-used directory, released as simultaneously
    /// as possible via a <see cref="Barrier"/> so the race window is actually exercised rather than
    /// merely hoped for. Raw <see cref="Thread"/>s are used instead of <c>Task.Run</c>/the thread
    /// pool: a barrier this wide can starve under the thread pool's slow injection rate, which would
    /// serialize the releases and mask the very race this test exists to catch. 800 creators was
    /// confirmed, while verifying this fix, to reproduce the pre-fix failure within one or two
    /// attempts on this machine (e.g. 11/800 failing with "Journal directory permissions are not
    /// private." before the fix; 0 after, across repeated runs).
    /// </remarks>
    [Fact]
    public void ConcurrentFirstTimeCreatorsNeverFailDirectoryBootstrapRace()
    {
        const int creators = 800;
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "sandtable-directory-bootstrap-race-" + Guid.NewGuid().ToString("N"));
        Assert.False(Directory.Exists(directory));
        try
        {
            using var start = new Barrier(creators);
            var failures = new Exception?[creators];
            var threads = new Thread[creators];
            for (var i = 0; i < creators; i++)
            {
                var index = i;
                threads[index] = new Thread(() =>
                {
                    var path = System.IO.Path.Combine(directory, $"campaign-{index}.sqlite");
                    start.SignalAndWait();
                    failures[index] = Record.Exception(() =>
                        CampaignDurableAuthorityJournal.CreateNew(path, OperatorSecret));
                });
            }
            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();
            var permissionFailures = failures
                .Where(failure => failure is InvalidOperationException
                    && failure.Message.Contains("private", StringComparison.Ordinal))
                .ToList();
            Assert.Empty(permissionFailures);
            Assert.All(failures, failure => Assert.Null(failure));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
