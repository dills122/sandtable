using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignOperationStageWeatherTests
{
    [Fact]
    public void StructuralCodecAllowsRepeatedStageNumbersAcrossTurns()
    {
        CampaignOperationStageWeather[] values = [Normal(1), Normal(2)];
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignOperationStageWeatherCodec.Write(writer, values);
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());

        var roundTrip = CampaignOperationStageWeatherCodec.Parse(
            document.RootElement.GetProperty("operationStageWeather"));

        Assert.Equal(values, roundTrip);
        Assert.True(CampaignOperationStageWeatherCodec.IsStructurallyValid(roundTrip));
    }

    [Fact]
    public void StructuralValidationRejectsDuplicateAndNoncanonicalPairs()
    {
        var first = Normal(1);
        var later = Normal(2);

        Assert.False(CampaignOperationStageWeatherCodec.IsStructurallyValid([first, first]));
        Assert.False(CampaignOperationStageWeatherCodec.IsStructurallyValid([later, first]));
    }

    private static CampaignOperationStageWeather Normal(int gameTurn) => new(
        CampaignOperationStageWeather.CurrentContractVersion,
        gameTurn,
        1,
        LandSide.Axis,
        WeatherSeason.Fall,
        1,
        1,
        WeatherKind.Normal,
        WeatherScope.None,
        null,
        [],
        0,
        0,
        0);
}
