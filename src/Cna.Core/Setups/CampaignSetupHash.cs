using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal static class CampaignSetupHash
{
    public static string Calculate(CampaignSetupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return FormatSha256(SerializeCanonical(definition));
    }

    internal static byte[] SerializeCanonical(CampaignSetupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return SerializeCanonical(
            definition.SchemaVersion,
            definition.SetupId,
            definition.IsSynthetic,
            definition.InitialGameTurn,
            definition.InitialInitiative,
            definition.OpeningPreamble,
            definition.Weather,
            definition.StageEntry,
            writer => WriteContent(writer, definition.Content),
            definition.Sources);
    }

    public static string Calculate(
        int schemaVersion,
        string setupId,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        CampaignContentSelection content,
        IReadOnlyList<RuleReference> sources) => FormatSha256(SerializeCanonical(
            schemaVersion,
            setupId,
            isSynthetic,
            initialGameTurn,
            initialInitiative,
            openingPreamble,
            weather,
            stageEntry,
            writer => WriteContent(writer, content),
            sources));

    internal static string CalculateV5(
        int schemaVersion,
        string setupId,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        ContentPackV5Identity pack,
        string scenarioId,
        IReadOnlyList<RuleReference> sources) => FormatSha256(SerializeCanonical(
            schemaVersion,
            setupId,
            isSynthetic,
            initialGameTurn,
            initialInitiative,
            openingPreamble,
            weather,
            stageEntry,
            writer => WriteContent(writer, pack, scenarioId),
            sources));

    private static byte[] SerializeCanonical(
        int schemaVersion,
        string setupId,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        Action<Utf8JsonWriter> writeContent,
        IReadOnlyList<RuleReference> sources)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", schemaVersion);
            writer.WriteString("setupId", setupId);
            writer.WriteBoolean("isSynthetic", isSynthetic);
            writer.WriteNumber("initialGameTurn", initialGameTurn);
            writer.WriteStartObject("initialInitiative");
            WriteInitiative(writer, initialInitiative);
            writer.WriteEndObject();
            writer.WriteStartObject("openingPreamble");
            writer.WriteNumber("contractVersion", openingPreamble.ContractVersion);
            writer.WriteString(
                "kind",
                openingPreamble.Kind switch
                {
                    CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations =>
                        "no-opening-naval-convoy-obligations",
                    _ => throw new ArgumentOutOfRangeException(nameof(openingPreamble)),
                });
            writer.WriteStartArray("sources");

            foreach (var source in openingPreamble.Sources)
            {
                writer.WriteStartObject();
                writer.WriteString("sourceId", source.SourceId);
                writer.WriteString("locator", source.Locator);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteStartObject("weather");
            writer.WriteNumber("contractVersion", weather.ContractVersion);
            writer.WriteString(
                "kind",
                weather.Kind switch
                {
                    CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects =>
                        "no-immediate-weather-effect-subjects",
                    _ => throw new ArgumentOutOfRangeException(nameof(weather)),
                });
            writer.WriteStartArray("sources");

            foreach (var source in weather.Sources)
            {
                writer.WriteStartObject();
                writer.WriteString("sourceId", source.SourceId);
                writer.WriteString("locator", source.Locator);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            CampaignStageEntryPolicyCodec.Write(writer, "stageEntry", stageEntry);
            writeContent(writer);
            writer.WriteStartArray("sources");

            foreach (var source in sources)
            {
                writer.WriteStartObject();
                writer.WriteString("sourceId", source.SourceId);
                writer.WriteString("locator", source.Locator);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static string FormatSha256(ReadOnlySpan<byte> canonical) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}";

    private static void WriteContent(
        Utf8JsonWriter writer,
        CampaignContentSelection content) => WriteContent(
            writer,
            content.Pack.SchemaVersion,
            content.Pack.FormatId,
            content.Pack.PackId,
            content.Pack.RulesetId,
            content.Pack.Hash,
            content.ScenarioId);

    private static void WriteContent(
        Utf8JsonWriter writer,
        ContentPackV5Identity pack,
        string scenarioId) => WriteContent(
            writer,
            pack.SchemaVersion,
            pack.FormatId,
            pack.PackId,
            pack.RulesetId,
            pack.Hash,
            scenarioId);

    private static void WriteContent(
        Utf8JsonWriter writer,
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        string hash,
        string scenarioId)
    {
        writer.WriteStartObject("content");
        writer.WriteNumber("schemaVersion", schemaVersion);
        writer.WriteString("formatId", formatId);
        writer.WriteString("packId", packId);
        writer.WriteString("rulesetId", rulesetId);
        writer.WriteString("hash", hash);
        writer.WriteString("scenarioId", scenarioId);
        writer.WriteEndObject();
    }

    private static void WriteInitiative(Utf8JsonWriter writer, InitiativePolicy policy)
    {
        switch (policy)
        {
            case PredeterminedInitiative predetermined:
                writer.WriteString("kind", "predetermined");
                writer.WriteString("holder", FormatSide(predetermined.Holder));
                break;
            case ContestedInitiative contested:
                writer.WriteString("kind", "contested");
                writer.WriteStartObject("axisFacts");
                writer.WriteString(
                    "rommelLocation",
                    FormatLocation(contested.AxisFacts.RommelLocation));
                writer.WriteStartArray("germanLandCombatUnitLocations");

                foreach (var location in contested.AxisFacts.GermanLandCombatUnitLocations)
                {
                    writer.WriteStringValue(FormatLocation(location));
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                break;
            default:
                throw new ArgumentException(
                    "The initiative policy is not supported.",
                    nameof(policy));
        }
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static string FormatLocation(AxisInitiativeLocation location) => location switch
    {
        AxisInitiativeLocation.QualifyingGameMap => "qualifying-game-map",
        AxisInitiativeLocation.TripoliTunisiaHoldingBox =>
            "tripoli-tunisia-holding-box",
        AxisInitiativeLocation.OffMapOrUnavailable => "off-map-or-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(location)),
    };
}
