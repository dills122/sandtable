using System.Text.Json;

namespace Cna.Core.Campaigns;

// V2 retains native bytes while preserving the independently versioned V1 envelope.
internal static class CampaignScheduledWorld6NativePreambleCodec
{
    private sealed record WireEvent(string Format, string PreambleBase64, string NativeEventBase64);
    private const string Format = "sandtable.scheduled-world6-native-preamble.v2";

    public static byte[] Encode(CampaignScheduledWorld6PreambleEvent preamble, string nativeEventBase64) =>
        JsonSerializer.SerializeToUtf8Bytes(new WireEvent(Format,
            Convert.ToBase64String(CampaignScheduledWorld6PreambleEventCodec.Encode(preamble)), nativeEventBase64));

    public static bool IsNative(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1048576) return false;
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray());
            return document.RootElement.TryGetProperty("Format", out var format) && format.GetString() == Format;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        { return false; }
    }
}
