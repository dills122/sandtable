using System.Diagnostics;

namespace Cna.LoadHarness;

// Sample once at start/end and every 250ms during the timed campaign workload.
internal sealed class RunMetrics(PostgresBacking? postgres) : IDisposable
{
    private readonly Process process = Process.GetCurrentProcess();
    private readonly int[] initialCollections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
    private readonly TimeSpan initialPause = GC.GetTotalPauseDuration();
    private long heapPeak;
    private long rssPeak;
    private int connectionsPeak;
    private int samples;

    internal async Task SampleAsync(CancellationToken cancellationToken)
    {
        heapPeak = Math.Max(heapPeak, GC.GetTotalMemory(false));
        process.Refresh();
        rssPeak = Math.Max(rssPeak, process.WorkingSet64);
        if (postgres is not null)
            connectionsPeak = Math.Max(connectionsPeak, await postgres.CountConnectionsAsync(cancellationToken).ConfigureAwait(false));
        samples++;
    }

    internal async Task PollAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                await SampleAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    internal object Snapshot() => new
    {
        postgresConnectionPeak = postgres is null ? (int?)null : connectionsPeak,
        rssPeakBytes = rssPeak,
        metricSamples = samples,
        gc = new
        {
            gen0 = GC.CollectionCount(0) - initialCollections[0],
            gen1 = GC.CollectionCount(1) - initialCollections[1],
            gen2 = GC.CollectionCount(2) - initialCollections[2],
            pauseMs = (GC.GetTotalPauseDuration() - initialPause).TotalMilliseconds,
            heapPeakBytes = heapPeak,
            heapBytes = GC.GetTotalMemory(false),
        },
    };

    public void Dispose() => process.Dispose();
}
