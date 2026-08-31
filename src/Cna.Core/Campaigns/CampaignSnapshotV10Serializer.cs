using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSnapshotV10Serializer
{
    public static byte[] Serialize(CampaignSnapshotV10 snapshot)
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
            CampaignV10CanonicalCodec.WriteSetup(writer, snapshot.Setup);
            CampaignV10CanonicalCodec.WriteWorld(writer, "world", snapshot.World);
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
            CampaignV10CanonicalCodec.WriteCurrentPosition(writer, snapshot.CurrentPosition);
            CampaignV10CanonicalCodec.WriteReactionWindow(
                writer,
                "reactionWindow",
                snapshot.ReactionWindow);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSnapshotV10 Deserialize(ReadOnlyMemory<byte> canonicalJson)
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
                "reactionWindow");
            var holder = root.GetProperty("initiativeHolder");
            var snapshot = new CampaignSnapshotV10(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                CampaignV10CanonicalCodec.ParseSetup(root.GetProperty("setup")),
                CampaignV10CanonicalCodec.ParseWorld(root.GetProperty("world")),
                holder.ValueKind == JsonValueKind.Null
                    ? null
                    : CampaignSnapshotSerializer.ParseSide(holder.GetString()),
                CampaignOperationStageOrderCodec.Parse(
                    root.GetProperty("operationStageOrders")),
                CampaignOperationStageWeatherCodec.Parse(
                    root.GetProperty("operationStageWeather")),
                CampaignSnapshotSerializer.ParseRandomState(
                    root.GetProperty("randomState")),
                CampaignV10CanonicalCodec.ParseCurrentPosition(
                    root.GetProperty("currentPosition")),
                CampaignV10CanonicalCodec.ParseReactionWindow(
                    root.GetProperty("reactionWindow")));
            ValidateContract(snapshot);
            if (!canonicalJson.Span.SequenceEqual(Serialize(snapshot)))
            {
                throw new JsonException("The Campaign snapshot v10 is not canonical JSON.");
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
            throw new JsonException("The Campaign snapshot v10 JSON is invalid.", exception);
        }
    }

    private static void ValidateContract(CampaignSnapshotV10 snapshot)
    {
        if (snapshot.ContractVersion != CampaignSnapshotV10.CurrentContractVersion
            || snapshot.Setup.Content.Pack.SchemaVersion != 5
            || !string.Equals(snapshot.Setup.Content.Pack.FormatId,
                "sandtable.content-json.v4", StringComparison.Ordinal)
            || snapshot.World.ContractVersion != CampaignWorldSnapshotV5.CurrentContractVersion
            || !Cna1979Ruleset.IsCanonicalHash(snapshot.RulesetHash)
            || snapshot.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(snapshot.RandomState.AlgorithmId,
                SandtableRandom.AlgorithmId, StringComparison.Ordinal))
        {
            throw new JsonException("The Campaign snapshot v10 contract is invalid.");
        }

        try
        {
            snapshot.ReactionWindow?.ValidateIdentities(
                snapshot.CampaignId,
                snapshot.RulesetHash);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("The Campaign snapshot v10 Reaction identity is invalid.",
                exception);
        }
    }
}
