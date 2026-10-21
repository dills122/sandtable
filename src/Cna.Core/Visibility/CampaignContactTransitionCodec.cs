using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

internal static class CampaignContactTransitionCodec
{
    public static byte[] SerializeCanonical(CampaignContactTransition value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ContractVersion != CampaignContactKnowledge.CurrentContractVersion
            || !Enum.IsDefined(value.Side) || !Enum.IsDefined(value.Kind)
            || !Enum.IsDefined(value.Identification) || !Enum.IsDefined(value.Access)
            || value.AuthorityVersion < 1
            || string.IsNullOrWhiteSpace(value.CampaignId)
            || string.IsNullOrWhiteSpace(value.SourceEventId)
            || string.IsNullOrWhiteSpace(value.SourceRuleId)
            || string.IsNullOrWhiteSpace(value.ContactHandle)
            || string.IsNullOrWhiteSpace(value.BoundElementId)
            || string.IsNullOrWhiteSpace(value.ObserverFormationId)
            || string.IsNullOrWhiteSpace(value.ApparentLocationId))
            throw new ArgumentException("Invalid contact transition contract.", nameof(value));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", value.ContractVersion);
            writer.WriteString("campaignId", value.CampaignId);
            writer.WriteString("side", Side(value.Side));
            writer.WriteNumber("authorityVersion", value.AuthorityVersion);
            writer.WriteString("sourceEventId", value.SourceEventId);
            writer.WriteString("sourceRuleId", value.SourceRuleId);
            writer.WriteString("kind", Kind(value.Kind));
            writer.WriteString("contactHandle", value.ContactHandle);
            writer.WriteString("boundElementId", value.BoundElementId);
            writer.WriteString("observerFormationId", value.ObserverFormationId);
            writer.WriteString("apparentLocationId", value.ApparentLocationId);
            writer.WriteString("identification", Identification(value.Identification));
            writer.WriteString("access", Access(value.Access));
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignContactTransition DeserializeCanonical(ReadOnlySpan<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes.ToArray());
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 13)
            throw new JsonException("Contact transition must have the exact version 1 shape.");
        var value = new CampaignContactTransition(
            root.GetProperty("contractVersion").GetInt32(),
            root.GetProperty("campaignId").GetString()!,
            ParseSide(root.GetProperty("side").GetString()),
            root.GetProperty("authorityVersion").GetInt64(),
            root.GetProperty("sourceEventId").GetString()!,
            root.GetProperty("sourceRuleId").GetString()!,
            ParseKind(root.GetProperty("kind").GetString()),
            root.GetProperty("contactHandle").GetString()!,
            root.GetProperty("boundElementId").GetString()!,
            root.GetProperty("observerFormationId").GetString()!,
            root.GetProperty("apparentLocationId").GetString()!,
            ParseIdentification(root.GetProperty("identification").GetString()),
            ParseAccess(root.GetProperty("access").GetString()));
        if (!bytes.SequenceEqual(SerializeCanonical(value)))
            throw new JsonException("Contact transition is not canonical.");
        return value;
    }

    private static string Side(LandSide value) => value switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown side."),
    };

    private static LandSide ParseSide(string? value) => value switch
    {
        "axis" => LandSide.Axis,
        "commonwealth" => LandSide.Commonwealth,
        _ => throw new JsonException("Unknown side."),
    };

    private static string Kind(CampaignContactTransitionKind value) => value switch
    {
        CampaignContactTransitionKind.Observed => "observed",
        CampaignContactTransitionKind.Updated => "updated",
        CampaignContactTransitionKind.Lost => "lost",
        CampaignContactTransitionKind.Rediscovered => "rediscovered",
        _ => throw new JsonException("Unknown contact transition."),
    };

    private static CampaignContactTransitionKind ParseKind(string? value) => value switch
    {
        "observed" => CampaignContactTransitionKind.Observed,
        "updated" => CampaignContactTransitionKind.Updated,
        "lost" => CampaignContactTransitionKind.Lost,
        "rediscovered" => CampaignContactTransitionKind.Rediscovered,
        _ => throw new JsonException("Unknown contact transition."),
    };

    private static string Identification(CampaignContactIdentification value) => value switch
    {
        CampaignContactIdentification.Presence => "presence",
        CampaignContactIdentification.FormationType => "formation-type",
        CampaignContactIdentification.Identified => "identified",
        _ => throw new JsonException("Unknown identification."),
    };

    private static CampaignContactIdentification ParseIdentification(string? value) => value switch
    {
        "presence" => CampaignContactIdentification.Presence,
        "formation-type" => CampaignContactIdentification.FormationType,
        "identified" => CampaignContactIdentification.Identified,
        _ => throw new JsonException("Unknown identification."),
    };

    private static string Access(CampaignContactAccess value) => value switch
    {
        CampaignContactAccess.Observer => "observer",
        CampaignContactAccess.Side => "side",
        _ => throw new JsonException("Unknown contact access."),
    };

    private static CampaignContactAccess ParseAccess(string? value) => value switch
    {
        "observer" => CampaignContactAccess.Observer,
        "side" => CampaignContactAccess.Side,
        _ => throw new JsonException("Unknown contact access."),
    };
}
