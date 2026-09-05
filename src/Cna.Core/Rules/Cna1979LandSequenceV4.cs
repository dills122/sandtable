using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Rules;

internal static class Cna1979LandSequenceV4
{
    public const int ContractVersion = 4;
    public const int CatalogSchemaVersion = 4;
    public const string ArtifactId = "cna-1979.1.land-sequence";
    public const string BreakdownStopPositionId = "land.position.breakdown-stop";

    private static readonly IReadOnlyList<RuleReference> StopSources = Array.AsReadOnly<RuleReference>(
        [Cna1979LandSequence.SourceReference, new("spi-1979-land-rules", "21.24-21.26")]);

    public static IReadOnlyList<LandSequencePosition> CreateTurn(int gameTurn) =>
        Array.AsReadOnly(Cna1979LandSequence.CreateTurn(gameTurn)
            .Select(position => Copy(position, position.ActiveSide)).ToArray());

    public static LandSequencePosition GetNext(LandSequencePosition current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var positions = CreateTurn(current.GameTurn);
        var index = positions.ToList().FindIndex(position => position == current);
        if (index < 0)
            throw new ArgumentException("The position must belong to the exact sequence 4 linear catalog.", nameof(current));
        return index == positions.Count - 1
            ? CreateTurn(checked(current.GameTurn + 1))[0]
            : positions[index + 1];
    }

    public static LandSequencePosition CreateBreakdownStopPosition(LandSequencePosition movement)
    {
        RequireMaterializedMovement(movement);
        return new(ContractVersion, BreakdownStopPositionId, movement.GameTurn,
            movement.OperationStage, LandStageIds.Operation, LandPhaseIds.MovementAndCombat,
            LandSegmentIds.BreakdownDetermination, null, LandActorRole.None, null, StopSources);
    }

    public static void RequireMaterializedMovement(LandSequencePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        var catalogPosition = CreateTurn(position.GameTurn)
            .SingleOrDefault(candidate => candidate.PositionId == position.PositionId);
        if (position.ContractVersion != ContractVersion
            || position.SegmentId != LandSegmentIds.Movement
            || position.ActiveSide is null
            || catalogPosition is null
            || Copy(catalogPosition, position.ActiveSide) != position)
            throw new ArgumentException("The position must be an exact materialized sequence 4 Movement segment.", nameof(position));
    }

    public static bool IsSupportedCheckpoint(LandSequencePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        var positions = CreateTurn(position.GameTurn);
        var canonical = position.SegmentId == LandSegmentIds.Movement
            ? Copy(position, null)
            : position;
        // Copy normalizes the version, so reject predecessor identity before comparing.
        if (position.ContractVersion != ContractVersion) return false;
        foreach (var candidate in positions)
        {
            if (candidate == canonical) return true;
            if (candidate.OperationStage == 1
                && candidate.ActorRole == LandActorRole.FirstActingSide
                && candidate.StepId == LandStepIds.PositionDetermination) break;
        }
        return false;
    }

    public static byte[] SerializeCanonicalCatalog()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", CatalogSchemaVersion);
            writer.WriteStartArray("positions");
            foreach (var position in CreateTurn(1)) WritePosition(writer, position);
            writer.WriteEndArray();
            writer.WriteStartArray("interruptPositions");
            var movement = CreateTurn(1).Single(position => position.OperationStage == 1
                && position.ActorRole == LandActorRole.FirstActingSide
                && position.SegmentId == LandSegmentIds.Movement);
            WritePosition(writer, CreateBreakdownStopPosition(Copy(movement, LandSide.Axis)));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static RulesetArtifact CreateArtifact() => new(ArtifactId,
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(SerializeCanonicalCatalog()))}",
        CreateTurn(1).SelectMany(position => position.Sources).Concat(StopSources).Distinct());

    private static LandSequencePosition Copy(LandSequencePosition position, LandSide? activeSide) => new(
        ContractVersion, position.PositionId, position.GameTurn, position.OperationStage,
        position.StageId, position.PhaseId, position.SegmentId, position.StepId,
        position.ActorRole, activeSide, position.Sources);

    private static void WritePosition(Utf8JsonWriter writer, LandSequencePosition position)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", position.ContractVersion);
        writer.WriteString("positionId", position.PositionId);
        writer.WriteNumber("gameTurn", position.GameTurn);
        writer.WriteNumber("operationStage", position.OperationStage);
        writer.WriteString("stageId", position.StageId);
        writer.WriteString("phaseId", position.PhaseId);
        writer.WriteString("segmentId", position.SegmentId);
        writer.WriteString("stepId", position.StepId);
        writer.WriteString("actorRole", position.ActorRole switch
        {
            LandActorRole.None => "none",
            LandActorRole.Commonwealth => "commonwealth",
            LandActorRole.InitiativeHolder => "initiative-holder",
            LandActorRole.FirstActingSide => "first-acting-side",
            LandActorRole.SecondActingSide => "second-acting-side",
            _ => throw new ArgumentOutOfRangeException(nameof(position)),
        });
        writer.WriteString("activeSide", position.ActiveSide switch
        {
            null => null,
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => throw new ArgumentOutOfRangeException(nameof(position)),
        });
        writer.WriteStartArray("sources");
        foreach (var source in position.Sources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
