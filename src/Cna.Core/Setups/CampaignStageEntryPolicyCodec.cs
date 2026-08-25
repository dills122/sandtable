using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal static class CampaignStageEntryPolicyCodec
{
    public static byte[] SerializeCanonical(CampaignStageEntryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteValue(writer, policy);
        }

        return stream.ToArray();
    }

    public static CampaignStageEntryPolicy DeserializeCanonical(
        ReadOnlySpan<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(
                canonicalJson.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 8,
                });
            var policy = Parse(document.RootElement);

            if (!canonicalJson.SequenceEqual(SerializeCanonical(policy)))
            {
                throw new JsonException("The Stage Entry policy is not canonical JSON.");
            }

            return policy;
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
            throw new JsonException("The Stage Entry policy is invalid.", exception);
        }
    }

    internal static void Write(
        Utf8JsonWriter writer,
        string propertyName,
        CampaignStageEntryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(policy);
        writer.WritePropertyName(propertyName);
        WriteValue(writer, policy);
    }

    internal static CampaignStageEntryPolicy Parse(JsonElement root)
    {
        RequireProperties(
            root,
            "contractVersion",
            "gameTurn",
            "operationStage",
            "organization",
            "navalConvoyArrival",
            "fleetAssignment",
            "fleetRepair",
            "sources");
        var sources = root.GetProperty("sources");

        if (sources.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Stage Entry policy sources must be an array.");
        }

        return new CampaignStageEntryPolicy(
            root.GetProperty("contractVersion").GetInt32(),
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            ParseKind(root.GetProperty("organization").GetString()),
            ParseKind(root.GetProperty("navalConvoyArrival").GetString()),
            ParseKind(root.GetProperty("fleetAssignment").GetString()),
            ParseKind(root.GetProperty("fleetRepair").GetString()),
            sources.EnumerateArray().Select(ParseSource).ToArray());
    }

    private static void WriteValue(
        Utf8JsonWriter writer,
        CampaignStageEntryPolicy policy)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", policy.ContractVersion);
        writer.WriteNumber("gameTurn", policy.GameTurn);
        writer.WriteNumber("operationStage", policy.OperationStage);
        writer.WriteString("organization", FormatKind(policy.Organization));
        writer.WriteString(
            "navalConvoyArrival",
            FormatKind(policy.NavalConvoyArrival));
        writer.WriteString("fleetAssignment", FormatKind(policy.FleetAssignment));
        writer.WriteString("fleetRepair", FormatKind(policy.FleetRepair));
        writer.WriteStartArray("sources");

        foreach (var source in policy.Sources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static RuleReference ParseSource(JsonElement source)
    {
        RequireProperties(source, "sourceId", "locator");
        return new RuleReference(
            source.GetProperty("sourceId").GetString()!,
            source.GetProperty("locator").GetString()!);
    }

    private static string FormatKind(StageEntryObligationKind kind) => kind switch
    {
        StageEntryObligationKind.ExplicitNone => "explicit-none",
        StageEntryObligationKind.HasObligations => "has-obligations",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static StageEntryObligationKind ParseKind(string? kind) => kind switch
    {
        "explicit-none" => StageEntryObligationKind.ExplicitNone,
        "has-obligations" => StageEntryObligationKind.HasObligations,
        _ => throw new JsonException($"Unknown Stage Entry obligation kind '{kind}'."),
    };

    private static void RequireProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.EnumerateObject()
                .Select(property => property.Name)
                .SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new JsonException("The Stage Entry policy property contract is invalid.");
        }
    }
}
