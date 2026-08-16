using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public static class CampaignSnapshotSerializer
{
    public static byte[] Serialize(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Validate(snapshot);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", snapshot.ContractVersion);
            writer.WriteString("campaignId", snapshot.CampaignId);
            writer.WriteNumber("stateVersion", snapshot.StateVersion);
            writer.WriteString("rulesetHash", snapshot.RulesetHash);
            writer.WriteNumber("seed", snapshot.Seed);
            writer.WriteString("firstPlayer", FormatSide(snapshot.FirstPlayer));
            writer.WriteStartObject("sequencePosition");
            writer.WriteNumber("contractVersion", snapshot.SequencePosition.ContractVersion);
            writer.WriteString("positionId", snapshot.SequencePosition.PositionId);
            writer.WriteNumber("gameTurn", snapshot.GameTurn);
            writer.WriteNumber("operationStage", snapshot.OperationStage);
            writer.WriteString("stageId", snapshot.SequencePosition.StageId);
            writer.WriteString("phaseId", snapshot.PhaseId);

            if (snapshot.SegmentId is null)
            {
                writer.WriteNull("segmentId");
            }
            else
            {
                writer.WriteString("segmentId", snapshot.SegmentId);
            }

            if (snapshot.SequencePosition.StepId is null)
            {
                writer.WriteNull("stepId");
            }
            else
            {
                writer.WriteString("stepId", snapshot.SequencePosition.StepId);
            }

            writer.WriteStartObject("source");
            writer.WriteString("sourceId", snapshot.SequencePosition.Source.SourceId);
            writer.WriteString("locator", snapshot.SequencePosition.Source.Locator);
            writer.WriteEndObject();

            if (snapshot.ActiveSide is null)
            {
                writer.WriteNull("activeSide");
            }
            else
            {
                writer.WriteString("activeSide", FormatSide(snapshot.ActiveSide.Value));
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSnapshot Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        using var document = JsonDocument.Parse(canonicalJson);
        var root = document.RootElement;
        var position = root.GetProperty("sequencePosition");
        var activeSideElement = position.GetProperty("activeSide");
        var segmentElement = position.GetProperty("segmentId");
        var stepElement = position.GetProperty("stepId");
        var sourceElement = position.GetProperty("source");
        var sequencePosition = new LandSequencePosition(
            position.GetProperty("contractVersion").GetInt32(),
            position.GetProperty("positionId").GetString()!,
            position.GetProperty("gameTurn").GetInt32(),
            position.GetProperty("operationStage").GetInt32(),
            position.GetProperty("stageId").GetString()!,
            position.GetProperty("phaseId").GetString()!,
            segmentElement.ValueKind == JsonValueKind.Null ? null : segmentElement.GetString(),
            stepElement.ValueKind == JsonValueKind.Null ? null : stepElement.GetString(),
            new RuleReference(
                sourceElement.GetProperty("sourceId").GetString()!,
                sourceElement.GetProperty("locator").GetString()!),
            activeSideElement.ValueKind == JsonValueKind.Null
                ? null
                : ParseSide(activeSideElement.GetString()));

        var snapshot = new CampaignSnapshot(
            root.GetProperty("contractVersion").GetInt32(),
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("rulesetHash").GetString()!,
            root.GetProperty("seed").GetUInt64(),
            ParseSide(root.GetProperty("firstPlayer").GetString()),
            sequencePosition);

        Validate(snapshot);
        return snapshot;
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static LandSide ParseSide(string? side) => side switch
    {
        "axis" => LandSide.Axis,
        "commonwealth" => LandSide.Commonwealth,
        _ => throw new JsonException($"Unknown Land side '{side}'."),
    };

    private static void Validate(CampaignSnapshot snapshot)
    {
        if (snapshot.ContractVersion != 1
            || snapshot.StateVersion < 1
            || string.IsNullOrWhiteSpace(snapshot.CampaignId)
            || string.IsNullOrWhiteSpace(snapshot.RulesetHash)
            || !Enum.IsDefined(snapshot.FirstPlayer)
            || snapshot.SequencePosition.ContractVersion != Cna1979LandSequence.ContractVersion)
        {
            throw new JsonException("The campaign snapshot contract is invalid.");
        }

        var validPositions = Cna1979LandSequence.CreateTurn(
            snapshot.GameTurn,
            snapshot.FirstPlayer);

        if (!validPositions.Contains(snapshot.SequencePosition))
        {
            throw new JsonException(
                "The campaign snapshot position is not part of the declared ruleset sequence.");
        }
    }
}
