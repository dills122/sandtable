using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignSetupV6Codec
{
    public static string CalculateHash(CampaignSetupSnapshotV6 setup) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(SerializeCanonicalHash(setup))).ToLowerInvariant()}";

    public static byte[] Serialize(CampaignSetupSnapshotV6 setup) => Serialize(setup, includeHash: true);

    public static byte[] SerializeCanonicalHash(CampaignSetupSnapshotV6 setup) => Serialize(setup, includeHash: false);

    private static byte[] Serialize(CampaignSetupSnapshotV6 setup, bool includeHash)
    {
        ArgumentNullException.ThrowIfNull(setup);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            WriteFields(writer, setup, includeHash);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSetupSnapshotV6 Deserialize(ReadOnlySpan<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes.ToArray());
        var setup = ParseSetup(document.RootElement);
        if (!bytes.SequenceEqual(Serialize(setup)))
            throw new JsonException("Setup 6 requires byte-identical canonical JSON.");
        return setup;
    }

    public static void WriteSetup(Utf8JsonWriter writer, CampaignSetupSnapshotV6 setup)
    {
        writer.WriteStartObject("setup");
        WriteFields(writer, setup, includeHash: true);
        writer.WriteEndObject();
    }

    private static void WriteFields(Utf8JsonWriter writer, CampaignSetupSnapshotV6 setup, bool includeHash)
    {
        writer.WriteNumber("schemaVersion", setup.SchemaVersion);
        writer.WriteString("setupId", setup.SetupId);
        if (includeHash)
            writer.WriteString("setupHash", setup.SetupHash);
        writer.WriteBoolean("isSynthetic", setup.IsSynthetic);
        writer.WriteString("capabilityProfileId", setup.CapabilityProfileId);
        writer.WriteNumber("initialGameTurn", setup.InitialGameTurn);
        writer.WriteStartObject("initialInitiative");
        CampaignSnapshotSerializer.WriteInitiative(writer, setup.InitialInitiative);
        writer.WriteEndObject();
        CampaignSnapshotSerializer.WriteOpeningPreamble(writer, setup.OpeningPreamble);
        CampaignSnapshotSerializer.WriteWeatherPolicy(writer, setup.Weather);
        CampaignStageEntryPolicyCodec.Write(writer, "stageEntry", setup.StageEntry);
        WriteContent(writer, setup.Content);
        CampaignSnapshotSerializer.WriteSources(writer, setup.Sources);
    }

    public static CampaignSetupSnapshotV6 ParseSetup(JsonElement element)
    {
        CampaignSnapshotSerializer.RequireProperties(element,
            "schemaVersion", "setupId", "setupHash", "isSynthetic", "capabilityProfileId",
            "initialGameTurn", "initialInitiative", "openingPreamble", "weather", "stageEntry", "content", "sources");
        var setup = CampaignSetupSnapshotV6.FromCanonical(
            element.GetProperty("schemaVersion").GetInt32(),
            element.GetProperty("setupId").GetString()!,
            element.GetProperty("setupHash").GetString()!,
            element.GetProperty("isSynthetic").GetBoolean(),
            element.GetProperty("capabilityProfileId").GetString()!,
            element.GetProperty("initialGameTurn").GetInt32(),
            CampaignSnapshotSerializer.ParseInitiative(element.GetProperty("initialInitiative")),
            CampaignSnapshotSerializer.ParseOpeningPreamble(element.GetProperty("openingPreamble")),
            CampaignSnapshotSerializer.ParseWeatherPolicy(element.GetProperty("weather")),
            CampaignStageEntryPolicyCodec.Parse(element.GetProperty("stageEntry")),
            ParseContent(element.GetProperty("content")),
            CampaignSnapshotSerializer.ParseSources(element.GetProperty("sources")));
        if (!Encoding.UTF8.GetBytes(element.GetRawText()).AsSpan().SequenceEqual(Serialize(setup)))
            throw new JsonException("Setup 6 requires byte-identical canonical JSON.");
        return setup;
    }

    private static CampaignContentV6Selection ParseContent(JsonElement content)
    {
        CampaignSnapshotSerializer.RequireProperties(content,
            "schemaVersion", "formatId", "packId", "rulesetId", "hash", "scenarioId");
        return new CampaignContentV6Selection(new ContentPackV6Identity(
            content.GetProperty("schemaVersion").GetInt32(),
            content.GetProperty("formatId").GetString()!,
            content.GetProperty("packId").GetString()!,
            content.GetProperty("rulesetId").GetString()!,
            content.GetProperty("hash").GetString()!),
            content.GetProperty("scenarioId").GetString()!);
    }

    private static void WriteContent(Utf8JsonWriter writer, CampaignContentV6Selection content)
    {
        writer.WriteStartObject("content");
        writer.WriteNumber("schemaVersion", content.Pack.SchemaVersion);
        writer.WriteString("formatId", content.Pack.FormatId);
        writer.WriteString("packId", content.Pack.PackId);
        writer.WriteString("rulesetId", content.Pack.RulesetId);
        writer.WriteString("hash", content.Pack.Hash);
        writer.WriteString("scenarioId", content.ScenarioId);
        writer.WriteEndObject();
    }
}
