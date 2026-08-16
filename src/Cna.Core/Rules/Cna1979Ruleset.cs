using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Rules;

public static class Cna1979Ruleset
{
    public const string RulesetId = "cna-1979.1";
    public const int ContractVersion = 1;

    private const string LandSequenceArtifactId = "cna-1979.1.land-sequence";

    private static readonly RuleReference[] LandSequenceSources =
    [
        Cna1979LandSequence.SourceReference,
        Cna1979LandSequence.OperationStageOrderSourceReference,
    ];

    public static RulesetManifest Manifest { get; } = CreateManifest();

    public static bool IsCanonicalHash(string? hash) => string.Equals(
        hash,
        Manifest.Hash,
        StringComparison.Ordinal);

    public static string CalculateLandSequenceContentHash(
        IEnumerable<LandSequencePosition> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        var positionCopy = positions.ToArray();

        if (positionCopy.Length == 0)
        {
            throw new ArgumentException(
                "At least one Land sequence position is required.",
                nameof(positions));
        }

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteStartArray("positions");

            foreach (var position in positionCopy)
            {
                ArgumentNullException.ThrowIfNull(position);

                writer.WriteStartObject();
                writer.WriteNumber("contractVersion", position.ContractVersion);
                writer.WriteString("positionId", position.PositionId);
                writer.WriteNumber("gameTurn", position.GameTurn);
                writer.WriteNumber("operationStage", position.OperationStage);
                writer.WriteString("stageId", position.StageId);
                writer.WriteString("phaseId", position.PhaseId);
                WriteNullableString(writer, "segmentId", position.SegmentId);
                WriteNullableString(writer, "stepId", position.StepId);
                writer.WriteStartArray("sources");

                foreach (var source in position.Sources
                    .OrderBy(value => value.SourceId, StringComparer.Ordinal)
                    .ThenBy(value => value.Locator, StringComparer.Ordinal))
                {
                    writer.WriteStartObject();
                    writer.WriteString("sourceId", source.SourceId);
                    writer.WriteString("locator", source.Locator);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                WriteNullableString(writer, "activeSide", position.ActiveSide?.ToString());
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return FormatSha256(stream.ToArray());
    }

    private static RulesetManifest CreateManifest()
    {
        var completeCatalog = Cna1979LandSequence
            .CreateTurn(1, LandSide.Axis)
            .Concat(Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth));
        var catalogHash = CalculateLandSequenceContentHash(completeCatalog);
        var artifact = new RulesetArtifact(
            LandSequenceArtifactId,
            catalogHash,
            LandSequenceSources);

        return new RulesetManifest(RulesetId, ContractVersion, [artifact], []);
    }

    private static string FormatSha256(ReadOnlySpan<byte> content) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()}";

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }
}
