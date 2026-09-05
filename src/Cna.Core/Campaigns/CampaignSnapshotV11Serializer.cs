using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSnapshotV11Serializer
{
    public static byte[] Serialize(CampaignSnapshotV11 snapshot, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        if (!CampaignSnapshotV11Validator.IsValid(snapshot, artifact, scenario))
            throw new JsonException("Invalid certified Breakdown snapshot authority.");
        return Serialize(snapshot);
    }

    public static CampaignSnapshotV11 Deserialize(ReadOnlyMemory<byte> canonicalJson,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        var snapshot = Deserialize(canonicalJson);
        if (!CampaignSnapshotV11Validator.IsValid(snapshot, artifact, scenario))
            throw new JsonException("Invalid certified Breakdown snapshot authority.");
        return snapshot;
    }

    public static byte[] Serialize(CampaignSnapshotV11 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateContract(snapshot);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", snapshot.ContractVersion);
            writer.WriteString("campaignId", snapshot.CampaignId);
            writer.WriteNumber("stateVersion", snapshot.StateVersion);
            writer.WriteString("rulesetHash", snapshot.RulesetHash);
            CampaignV11CanonicalCodec.WriteSetup(writer, snapshot.Setup);
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", snapshot.World);
            if (snapshot.InitiativeHolder is null)
            {
                writer.WriteNull("initiativeHolder");
            }
            else
            {
                writer.WriteString(
                    "initiativeHolder",
                    CampaignSnapshotSerializer.FormatSide(snapshot.InitiativeHolder.Value));
            }

            CampaignOperationStageOrderCodec.Write(writer, snapshot.OperationStageOrders);
            CampaignOperationStageWeatherCodec.Write(writer, snapshot.OperationStageWeather);
            CampaignSnapshotSerializer.WriteRandomState(writer, snapshot.RandomState);
            CampaignV11CanonicalCodec.WriteCurrentPosition(writer, snapshot.CurrentPosition);
            CampaignV11CanonicalCodec.WriteReactionWindow(
                writer,
                "reactionWindow",
                snapshot.ReactionWindow);
            writer.WritePropertyName("breakdownFlow");
            CampaignBreakdownCodec.WriteFlow(writer, snapshot.BreakdownFlow);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSnapshotV11 Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(
                root,
                "contractVersion",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "setup",
                "world",
                "initiativeHolder",
                "operationStageOrders",
                "operationStageWeather",
                "randomState",
                "currentPosition",
                "reactionWindow", "breakdownFlow");
            var holder = root.GetProperty("initiativeHolder");
            var snapshot = new CampaignSnapshotV11(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                CampaignV11CanonicalCodec.ParseSetup(root.GetProperty("setup")),
                CampaignV11CanonicalCodec.ParseWorld(root.GetProperty("world")),
                holder.ValueKind == JsonValueKind.Null
                    ? null
                    : CampaignSnapshotSerializer.ParseSide(holder.GetString()),
                CampaignOperationStageOrderCodec.Parse(
                    root.GetProperty("operationStageOrders")),
                CampaignOperationStageWeatherCodec.Parse(
                    root.GetProperty("operationStageWeather")),
                CampaignSnapshotSerializer.ParseRandomState(
                    root.GetProperty("randomState")),
                CampaignV11CanonicalCodec.ParseCurrentPosition(
                    root.GetProperty("currentPosition")),
                CampaignV11CanonicalCodec.ParseReactionWindow(
                    root.GetProperty("reactionWindow")),
                CampaignBreakdownCodec.ParseFlow(root.GetProperty("breakdownFlow")));
            ValidateContract(snapshot);
            if (!canonicalJson.Span.SequenceEqual(Serialize(snapshot)))
            {
                throw new JsonException("The Campaign snapshot v11 is not canonical JSON.");
            }

            return snapshot;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or FormatException
            or InvalidOperationException
            or KeyNotFoundException)
        {
            throw new JsonException("The Campaign snapshot v11 JSON is invalid.", exception);
        }
    }

    private static void ValidateContract(CampaignSnapshotV11 snapshot) => CampaignSnapshotV11Validator.RequireShape(snapshot);
}
