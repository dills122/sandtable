using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Owned, bounded evidence. Returned buffers are copies, never authority aliases.</summary>
internal sealed class CampaignCombatRetainedHistory
{
    private readonly byte[] created;
    private readonly byte[][] events;

    private CampaignCombatRetainedHistory(byte[] created, byte[][] events)
    {
        this.created = created;
        this.events = events;
    }

    public int Count => events.Length;
    public byte[] Created => created.ToArray();
    public IReadOnlyList<byte[]> Events => Array.AsReadOnly(events.Select(bytes => bytes.ToArray()).ToArray());
    internal ReadOnlySpan<byte> CreatedSpan => created;
    internal byte[][] CopyRange(int start, int count) => events.Skip(start).Take(count).Select(bytes => bytes.ToArray()).ToArray();

    public static CampaignCombatRetainedHistory Capture(ReadOnlySpan<byte> created, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var count = events.Count;
        if (count is < 0 or > 512) throw new JsonException("Retained history exceeds 512 events.");
        if (created.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized independently retained Created11.");
        // Validate the entire input before any payload copying or causal replay. Retain references
        // once so a caller-provided list cannot swap an element between validation and ownership.
        var validated = new byte[count][];
        long aggregate = 0;
        for (var index = 0; index < count; index++)
        {
            var bytes = events[index];
            if (bytes is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized retained event.");
            aggregate += bytes.Length;
            if (aggregate > 16_777_216) throw new JsonException("Retained history exceeds aggregate byte limit.");
            validated[index] = bytes;
        }
        return new(created.ToArray(), validated.Select(bytes => bytes.ToArray()).ToArray());
    }
}
