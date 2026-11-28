using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Cna.Core.Content;

namespace Cna.Core.Tests.Content;

public sealed class CampaignPerformanceSpanTests
{
    [Fact]
    public void EnabledScopeMeasuresAllocationsAndEmitsOnlyDiagnosticFields()
    {
        using var listener = new SpanListener();
        using (CampaignPerformanceSpan.Start("test-scope"))
        {
            GC.KeepAlive(new byte[4096]);
        }

        var span = Assert.Single(listener.Spans, value => Equals(value[0], "test-scope"));
        Assert.Equal(3, span.Count);
        Assert.True(Assert.IsType<long>(span[1]) >= 0);
        Assert.True(Assert.IsType<long>(span[2]) >= 4096);
    }

    private sealed class SpanListener : EventListener
    {
        internal ConcurrentQueue<IReadOnlyList<object?>> Spans { get; } = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Sandtable-Performance")
                EnableEvents(eventSource, EventLevel.Informational);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == 1 && eventData.Payload is { } payload)
                Spans.Enqueue(payload);
        }
    }
}
