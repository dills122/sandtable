namespace Cna.LoadHarness;

// Admission is across all workers, including setup and discovery; stop admitting requests
// when a configured cliff is crossed. Already admitted requests finish normally.
internal sealed class HttpRunControl(HarnessConfig config, CancellationTokenSource stop) : IDisposable
{
    private readonly SemaphoreSlim slots = new(config.MaxInflight);
    private readonly object gate = new();
    private readonly List<double> submits = [];
    private int inflight;
    private int peak;
    private int calls;
    private int errors;
    private string? reason;

    internal bool StoppedOnCliff => reason is not null;

    internal async Task EnterAsync()
    {
        await slots.WaitAsync(stop.Token).ConfigureAwait(false);
        lock (gate)
        {
            if (stop.IsCancellationRequested)
            {
                slots.Release();
                stop.Token.ThrowIfCancellationRequested();
            }
            peak = Math.Max(peak, ++inflight);
        }
    }

    internal void Finish(string operation, string outcome, double elapsedMs)
    {
        lock (gate)
        {
            inflight--;
            calls++;
            if (outcome is not ("ok" or "unavailable" or "reaction-refused")) errors++;
            if (operation == "submit" && outcome == "ok" && config.StopSubmitP95Ms is { } limit)
            {
                var index = submits.BinarySearch(elapsedMs);
                submits.Insert(index < 0 ? ~index : index, elapsedMs);
                if (reason is null && submits[(int)Math.Ceiling(submits.Count * .95) - 1] > limit)
                    reason = "submit-p95";
            }
            if (reason is null && config.StopErrorRate is { } rate && (double)errors / calls > rate)
                reason = "error-rate";
            if (reason is not null && !stop.IsCancellationRequested) stop.Cancel();
        }
        slots.Release();
    }

    internal object Snapshot(IReadOnlyList<Diagnostic> rows) => new
    {
        peakInflight = peak,
        discoveryProbes = rows.Count(d => d.Operation == "discover"),
        openingProbes = rows.Count(d => d.Operation == "open"),
        fallbackProbes = rows.Count(d => d.Operation == "probe"),
        expected409s = rows.Count(d => d.Outcome == "unavailable"),
        errorCount = errors,
        errorRate = calls == 0 ? 0 : (double)errors / calls,
        stoppedOnCliff = reason is not null,
        cliffReason = reason,
    };

    public void Dispose() => slots.Dispose();
}
