using System.Text.Json;

namespace Cna.Core.Campaigns;

internal static class CampaignOperationStageOrderCodec
{
    public static void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CampaignOperationStageOrder> orders)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!IsStructurallyValid(orders))
        {
            throw new JsonException("The operation-stage order collection is invalid.");
        }

        writer.WriteStartArray("operationStageOrders");
        foreach (var order in orders)
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", order.ContractVersion);
            writer.WriteNumber("gameTurn", order.GameTurn);
            writer.WriteNumber("operationStage", order.OperationStage);
            writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(order.FirstSide));
            writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(order.SecondSide));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static CampaignOperationStageOrder[] Parse(JsonElement orders)
    {
        try
        {
            var parsed = orders.EnumerateArray().Select(order =>
            {
                CampaignSnapshotSerializer.RequireProperties(
                    order,
                    "contractVersion",
                    "gameTurn",
                    "operationStage",
                    "firstSide",
                    "secondSide");
                return new CampaignOperationStageOrder(
                    order.GetProperty("contractVersion").GetInt32(),
                    order.GetProperty("gameTurn").GetInt32(),
                    order.GetProperty("operationStage").GetInt32(),
                    CampaignSnapshotSerializer.ParseSide(
                        order.GetProperty("firstSide").GetString()),
                    CampaignSnapshotSerializer.ParseSide(
                        order.GetProperty("secondSide").GetString()));
            }).ToArray();

            return IsStructurallyValid(parsed)
                ? parsed
                : throw new JsonException("The operation-stage order collection is invalid.");
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException)
        {
            throw new JsonException("The operation-stage order collection is invalid.", exception);
        }
    }

    public static bool IsStructurallyValid(
        IReadOnlyList<CampaignOperationStageOrder>? orders)
    {
        if (orders is null
            || orders.Any(order => order is null)
            || orders.Any(order => order.ContractVersion
                != CampaignOperationStageOrder.CurrentContractVersion)
            || orders.Select(order => (order.GameTurn, order.OperationStage)).Distinct().Count()
                != orders.Count)
        {
            return false;
        }

        return orders.SequenceEqual(orders
            .OrderBy(order => order.GameTurn)
            .ThenBy(order => order.OperationStage));
    }
}
