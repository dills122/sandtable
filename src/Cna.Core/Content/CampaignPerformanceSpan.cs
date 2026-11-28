using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Cna.Core.Content;

/// <summary>Opt-in, synchronous diagnostic scope; never participates in authoritative state.</summary>
/// <remarks>Enable the Sandtable-Performance EventSource at informational level. Nested spans
/// are inclusive, and allocation deltas count only the executing managed thread. Do not pass
/// campaign, seat, element, or other data identifiers as labels, or cross an await.</remarks>
public readonly struct CampaignPerformanceSpan : IDisposable
{
    private readonly string? operation;
    private readonly long started;
    private readonly long allocated;

    private CampaignPerformanceSpan(string operation)
    {
        this.operation = operation;
        started = Stopwatch.GetTimestamp();
        allocated = GC.GetAllocatedBytesForCurrentThread();
    }

    public static CampaignPerformanceSpan Start(string operation) =>
        PerformanceEvents.Log.IsEnabled() ? new(operation) : default;

    public void Dispose()
    {
        if (operation is null) return;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var ticks = Stopwatch.GetTimestamp() - started;
        PerformanceEvents.Log.Span(operation, ticks, bytes);
    }

    [EventSource(Name = "Sandtable-Performance")]
    private sealed class PerformanceEvents : EventSource
    {
        internal static readonly PerformanceEvents Log = new();

        [Event(1, Level = EventLevel.Informational)]
        public void Span(string operation, long elapsedTicks, long allocatedBytes) =>
            WriteEvent(1, operation, elapsedTicks, allocatedBytes);
    }
}
