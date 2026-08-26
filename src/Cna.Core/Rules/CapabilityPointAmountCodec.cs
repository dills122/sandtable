using System.Text.Json;

namespace Cna.Core.Rules;

internal static class CapabilityPointAmountCodec
{
    public static byte[] SerializeCanonical(CapabilityPointAmount amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(writer, amount);
        }

        return stream.ToArray();
    }

    public static CapabilityPointAmount Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 4,
                });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Expected a Capability Point amount object.");
            }

            var properties = root.EnumerateObject().ToArray();
            if (!properties.Select(value => value.Name).SequenceEqual(
                ["numerator", "denominator"]))
            {
                throw new JsonException(
                    "Capability Point amount properties are missing, extra, or reordered.");
            }

            if (properties[0].Value.ValueKind != JsonValueKind.Number
                || !properties[0].Value.TryGetInt64(out var numerator)
                || properties[1].Value.ValueKind != JsonValueKind.Number
                || !properties[1].Value.TryGetInt32(out var denominator))
            {
                throw new JsonException(
                    "Capability Point amount members must be supported integers.");
            }

            var amount = new CapabilityPointAmount(numerator, denominator);
            if (!utf8Json.SequenceEqual(SerializeCanonical(amount)))
            {
                throw new JsonException("The Capability Point amount is not canonical JSON.");
            }

            return amount;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or FormatException
                or OverflowException)
        {
            throw new JsonException("The Capability Point amount is invalid.", exception);
        }
    }

    internal static void WriteCanonical(Utf8JsonWriter writer, CapabilityPointAmount amount)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(amount);

        writer.WriteStartObject();
        writer.WriteNumber("numerator", amount.Numerator);
        writer.WriteNumber("denominator", amount.Denominator);
        writer.WriteEndObject();
    }
}
