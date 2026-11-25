using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Opt-in like the capacity benchmark. The SQLite fixture exercises the same synchronous
    // Load body as the Marten working set, without measuring a network/database round trip.
    [Fact]
    public void CodecReplayBenchmarkHistoryGrowth()
    {
        if (Environment.GetEnvironmentVariable("SANDTABLE_CODEC_REPLAY_BENCHMARK") != "1") return;
        Assert.False(JournalFixture.UsesMarten);
        using var fixture = new JournalFixture();
        var request = Request();
        var journal = fixture.Create();
        var session = journal.CreateCampaign(request);
        var historyLengths = new[] { 1, 4, 8, 12 };
        var acceptedSteps = 0;
        foreach (var length in historyLengths)
        {
            while (acceptedSteps < length)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                    .FirstOrDefault(value => value?.Candidates.Count > 0);
                Assert.NotNull(set);
                var submission = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                    set.PositionId, set.Audience, set.Candidates[0].ActionId);
                var accepted = journal.Append(request.CampaignId, submission);
                Assert.True(accepted.IsAccepted);
                session = accepted.SuccessorSession!;
                acceptedSteps++;
            }

            var reopened = fixture.Open();
            // Warm the exact path before collecting samples; keep correctness checks outside the window.
            var expected = CampaignExercises.Reconstruct(reopened.ReadVerified(request.CampaignId));
            Assert.True(expected.IsVerified);
            const int iterations = 20;
            var (elapsed, allocated, retained) = MeasureCodecReplay(reopened, request.CampaignId, length, iterations);
            var reconstructed = CampaignExercises.Reconstruct(retained);
            Assert.True(reconstructed.IsVerified);
            Assert.Equal(expected.ReconstructedSnapshotBytes, reconstructed.ReconstructedSnapshotBytes);
            BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                $"{{\"kind\":\"codec-replay\",\"steps\":{length},\"iterations\":{iterations},\"loadMs\":{elapsed / iterations:F3},\"loadAllocatedBytes\":{allocated / iterations}}}"));
        }
    }

    // A stable stack frame isolates the measured read from setup, append and verification work
    // when processing EventPipe samples. Markers separate each history length in one trace.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (double Elapsed, long Allocated, ExerciseSession Retained) MeasureCodecReplay(
        CampaignDurableAuthorityJournal journal, string campaignId, int length, int iterations)
    {
        CodecReplayEvents.Log.Start(length);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        ExerciseSession retained = null!;
        for (var iteration = 0; iteration < iterations; iteration++)
            retained = journal.ReadVerified(campaignId);
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        CodecReplayEvents.Log.Stop(length);
        return (elapsed, allocated, retained);
    }

    [EventSource(Name = "Sandtable-CodecReplayBenchmark")]
    private sealed class CodecReplayEvents : EventSource
    {
        internal static readonly CodecReplayEvents Log = new();

        [Event(1)]
        internal void Start(int length) => WriteEvent(1, length);

        [Event(2)]
        internal void Stop(int length) => WriteEvent(2, length);
    }
}
