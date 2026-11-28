using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Separate wire family; never change the historical successor serializers.
internal static class CampaignNativeReactionCodec
{
    private const int MaxBytes = 65536;
    private static readonly string[] HeaderFields = ["contractVersion", "eventType", "campaignId", "priorStateVersion",
        "priorEventHash", "gameTurn", "windowId", "reactingSide", "priorWorldHash", "resultWorldHash"];

    internal static byte[] Write(int version, int expectedVersion, string eventType, string campaignId,
        long priorVersion, string priorEventHash, int gameTurn, CampaignReactionWindowId windowId,
        LandSide reactingSide, string priorWorldHash, string resultWorldHash, Action<Utf8JsonWriter> fields)
    {
        if (version != expectedVersion || priorVersion is < 1 or long.MaxValue || gameTurn < 1 || !Enum.IsDefined(reactingSide))
            throw new JsonException("Invalid native Reaction event header.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", version);
            writer.WriteString("eventType", eventType);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(campaignId, nameof(campaignId)));
            writer.WriteNumber("priorStateVersion", priorVersion);
            writer.WriteString("priorEventHash", ContentContractGuards.RequireSha256(priorEventHash, nameof(priorEventHash)));
            writer.WriteNumber("gameTurn", gameTurn);
            writer.WriteString("windowId", windowId.Value);
            writer.WriteString("reactingSide", CampaignSnapshotSerializer.FormatSide(reactingSide));
            writer.WriteString("priorWorldHash", ContentContractGuards.RequireSha256(priorWorldHash, nameof(priorWorldHash)));
            writer.WriteString("resultWorldHash", ContentContractGuards.RequireSha256(resultWorldHash, nameof(resultWorldHash)));
            fields(writer);
            writer.WriteEndObject();
        }
        if (stream.Length > MaxBytes) throw new JsonException("Invalid native Reaction event size.");
        return stream.ToArray();
    }

    internal static T Read<T>(ReadOnlyMemory<byte> bytes, string eventType, string[] fields,
        Func<JsonElement, T> parse, Func<T, byte[]> serialize)
    {
        try
        {
            if (bytes.Length is < 1 or > MaxBytes) throw new JsonException("Invalid native Reaction event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, [.. HeaderFields, .. fields]);
            if (String(root, "eventType") != eventType) throw new JsonException("Invalid native Reaction event type.");
            var parsed = parse(root);
            if (!bytes.Span.SequenceEqual(serialize(parsed))) throw new JsonException("Noncanonical native Reaction event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or NullReferenceException or OverflowException)
        { throw new JsonException("Invalid native Reaction event.", error); }
    }

    internal static string String(JsonElement root, string name) => root.GetProperty(name).GetString()!;

    internal static void RequireWindowBinding(CampaignReactionWindow window, CampaignReactionWindowId windowId,
        LandSide side, int gameTurn, long priorVersion)
    {
        if (window.WindowId != windowId || window.ReactingSide != side
            || window.ReactingPosition.SuspendedMovementPosition.GameTurn != gameTurn
            || window.TriggerAuthority.MoveContractVersion != CampaignNativeSecondMoveV44.CurrentContractVersion
            || window.TriggerCommittedStateVersion > priorVersion)
            throw new JsonException("Native Reaction window does not bind the event header.");
    }
}
