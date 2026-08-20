using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignOperationStageOrderTests
{
    [Fact]
    public void PairCodecRoundTripsRepeatedStageNumbersAcrossTurnsCanonically()
    {
        CampaignOperationStageOrder[] orders =
        [
            Order(1, 1, LandSide.Axis, LandSide.Commonwealth),
            Order(2, 1, LandSide.Commonwealth, LandSide.Axis),
        ];

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignOperationStageOrderCodec.Write(writer, orders);
            writer.WriteEndObject();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var document = JsonDocument.Parse(json);
        var roundTrip = CampaignOperationStageOrderCodec.Parse(
            document.RootElement.GetProperty("operationStageOrders"));

        Assert.Equal(
            "{\"operationStageOrders\":[{\"contractVersion\":2,\"gameTurn\":1," +
            "\"operationStage\":1,\"firstSide\":\"axis\"," +
            "\"secondSide\":\"commonwealth\"},{\"contractVersion\":2," +
            "\"gameTurn\":2,\"operationStage\":1," +
            "\"firstSide\":\"commonwealth\",\"secondSide\":\"axis\"}]}",
            json);
        Assert.Equal(orders, roundTrip);
        Assert.True(CampaignOperationStageOrderCodec.IsStructurallyValid(roundTrip));
    }

    [Fact]
    public void StructuralValidationRejectsDuplicateAndNoncanonicalPairs()
    {
        var first = Order(1, 1, LandSide.Axis, LandSide.Commonwealth);
        var later = Order(2, 1, LandSide.Commonwealth, LandSide.Axis);

        Assert.False(CampaignOperationStageOrderCodec.IsStructurallyValid([first, first]));
        Assert.False(CampaignOperationStageOrderCodec.IsStructurallyValid([later, first]));
    }

    [Theory]
    [InlineData("[{\"contractVersion\":2,\"gameTurn\":1,\"operationStage\":1,\"firstSide\":\"axis\",\"secondSide\":\"commonwealth\"},{\"contractVersion\":2,\"gameTurn\":1,\"operationStage\":1,\"firstSide\":\"commonwealth\",\"secondSide\":\"axis\"}]")]
    [InlineData("[{\"contractVersion\":2,\"gameTurn\":2,\"operationStage\":1,\"firstSide\":\"axis\",\"secondSide\":\"commonwealth\"},{\"contractVersion\":2,\"gameTurn\":1,\"operationStage\":1,\"firstSide\":\"commonwealth\",\"secondSide\":\"axis\"}]")]
    [InlineData("[{\"contractVersion\":1,\"operationStage\":1,\"firstSide\":\"axis\",\"secondSide\":\"commonwealth\"}]")]
    public void PairCodecRejectsDuplicateNoncanonicalAndLegacyCollections(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Throws<JsonException>(() =>
            CampaignOperationStageOrderCodec.Parse(document.RootElement));
    }

    private static CampaignOperationStageOrder Order(
        int gameTurn,
        int operationStage,
        LandSide firstSide,
        LandSide secondSide) => new(
            CampaignOperationStageOrder.CurrentContractVersion,
            gameTurn,
            operationStage,
            firstSide,
            secondSide);
}
