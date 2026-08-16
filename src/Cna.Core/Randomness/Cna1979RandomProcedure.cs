using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Randomness;

public static class Cna1979RandomProcedure
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.random-procedure";

    public static RuleReference OpposedDiceSourceReference { get; } = new(
        "spi-1979-land-rules",
        "7.14");

    public static RuleReference NormalizationSourceReference { get; } = new(
        "sandtable-random-procedure",
        "sha256-counter.v1");

    public static RandomProcedureDefinition CanonicalDefinition { get; } = new(
        SchemaVersion,
        SandtableRandom.AlgorithmId,
        SandtableRandom.DomainAscii,
        0,
        "unsigned-64-big-endian",
        SandtableRandom.BlockBytes,
        SandtableRandom.D6AcceptBelow,
        SandtableRandom.D6Modulo,
        SandtableRandom.D6Offset,
        ["axis", "commonwealth"],
        [NormalizationSourceReference, OpposedDiceSourceReference]);

    public static RulesetArtifact CreateArtifact() => new(
        ArtifactId,
        CalculateContentHash(CanonicalDefinition),
        CanonicalDefinition.Sources);

    public static string CalculateContentHash(RandomProcedureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            writer.WriteString("algorithmId", definition.AlgorithmId);
            writer.WriteString("domainAscii", definition.DomainAscii);
            writer.WriteNumber("separatorByte", definition.SeparatorByte);
            writer.WriteString("integerEncoding", definition.IntegerEncoding);
            writer.WriteNumber("blockBytes", definition.BlockBytes);
            writer.WriteNumber("d6AcceptBelow", definition.D6AcceptBelow);
            writer.WriteNumber("d6Modulo", definition.D6Modulo);
            writer.WriteNumber("d6Offset", definition.D6Offset);
            writer.WriteStartArray("initiativeDrawOrder");

            foreach (var side in definition.InitiativeDrawOrder)
            {
                writer.WriteStringValue(side);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("sources");

            foreach (var source in definition.Sources)
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
}
