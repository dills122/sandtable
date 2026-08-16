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
        return Calculate(
            definition.SchemaVersion,
            definition.SetupId,
            definition.IsSynthetic,
            definition.InitialGameTurn,
            definition.InitialInitiative,
            definition.Content,
            definition.Sources);
    }

    public static string Calculate(
        int schemaVersion,
        string setupId,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignContentSelection content,
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
            writer.WriteStartObject("content");
            writer.WriteNumber("schemaVersion", content.Pack.SchemaVersion);
            writer.WriteString("formatId", content.Pack.FormatId);
            writer.WriteString("packId", content.Pack.PackId);
            writer.WriteString("rulesetId", content.Pack.RulesetId);
            writer.WriteString("hash", content.Pack.Hash);
            writer.WriteString("scenarioId", content.ScenarioId);
            writer.WriteEndObject();
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

        return $"sha256:{Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant()}";
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
