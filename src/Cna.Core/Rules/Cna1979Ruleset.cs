using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Randomness;

namespace Cna.Core.Rules;

public static class Cna1979Ruleset
{
    public const string RulesetId = "cna-1979.1";
    public const int ContractVersion = 2;

    private const string LandSequenceArtifactId = "cna-1979.1.land-sequence";

    internal const string EmptyOpeningConvoyRulingId =
        "cna-1979.1.ruling.explicit-empty-opening-convoy-resolution";

    private static readonly RuleReference[] LandSequenceSources =
    [
        Cna1979LandSequence.SourceReference,
        Cna1979LandSequence.InitiativeSideSourceReference,
        Cna1979LandSequence.StageChoiceSourceReference,
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
                writer.WriteString("actorRole", FormatActorRole(position.ActorRole));
                WriteNullableString(writer, "activeSide", position.ActiveSide is null
                    ? null
                    : FormatSide(position.ActiveSide.Value));
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
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return FormatSha256(stream.ToArray());
    }

    private static RulesetManifest CreateManifest()
    {
        var completeCatalog = Cna1979LandSequence.CreateTurn(1);
        var catalogHash = CalculateLandSequenceContentHash(completeCatalog);
        var sequenceArtifact = new RulesetArtifact(
            LandSequenceArtifactId,
            catalogHash,
            LandSequenceSources);

        return new RulesetManifest(
            RulesetId,
            ContractVersion,
            [
                sequenceArtifact,
                Cna1979InitiativeRatings.CreateArtifact(),
                Cna1979ContentVocabulary.CreateArtifact(),
                Cna1979RandomProcedure.CreateArtifact(),
            ],
            [CreateEmptyOpeningConvoyRuling()]);
    }

    private static Ruling CreateEmptyOpeningConvoyRuling() => new(
        EmptyOpeningConvoyRulingId,
        "cna-1979.1.conflict.empty-opening-convoy-phase",
        [
            "reject-empty-opening-convoy-as-unsupported",
            "resolve-explicitly-admitted-empty-opening-convoy",
        ],
        "resolve-explicitly-admitted-empty-opening-convoy",
        ["ACT-AC-002", "ACT-AC-003", "ACT-AC-016"],
        [
            Cna1979LandSequence.SourceReference,
            new RuleReference("spi-1979-land-rules", "32.43"),
            new RuleReference("spi-1979-land-rules", "32.61"),
            new RuleReference(
                "sandtable-rules-lab",
                "opening-preamble.no-naval-convoy-obligations.v1"),
        ]);

    private static string FormatSha256(ReadOnlySpan<byte> content) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()}";

    private static string FormatActorRole(LandActorRole role) => role switch
    {
        LandActorRole.None => "none",
        LandActorRole.Commonwealth => "commonwealth",
        LandActorRole.InitiativeHolder => "initiative-holder",
        LandActorRole.FirstActingSide => "first-acting-side",
        LandActorRole.SecondActingSide => "second-acting-side",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

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
