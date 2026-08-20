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

    [Theory]
    [InlineData("\"season\":\"fall\"", "\"season\":\"Fall\"")]
    [InlineData("\"kind\":\"normal\"", "\"kind\":\"Normal\"")]
    public void StructuralCodecRejectsNoncanonicalWeatherTokens(
        string canonicalToken,
        string noncanonicalToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignOperationStageWeatherCodec.Write(writer, [Normal(1)]);
            writer.WriteEndObject();
        }
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray())
            .Replace(canonicalToken, noncanonicalToken, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(json);

        Assert.Throws<JsonException>(() => CampaignOperationStageWeatherCodec.Parse(
            document.RootElement.GetProperty("operationStageWeather")));
    }

    [Fact]
    public void StructuralCodecRejectsNoncanonicalWeatherAreaToken()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignOperationStageWeatherCodec.Write(writer, [Rainstorm()]);
            writer.WriteEndObject();
        }
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray())
            .Replace("\"affectedAreas\":[\"c\",\"d\"]",
                "\"affectedAreas\":[\"C\",\"d\"]", StringComparison.Ordinal);
        using var document = JsonDocument.Parse(json);

        Assert.Throws<JsonException>(() => CampaignOperationStageWeatherCodec.Parse(
            document.RootElement.GetProperty("operationStageWeather")));
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

    private static CampaignOperationStageWeather Rainstorm() => new(
        CampaignOperationStageWeather.CurrentContractVersion,
        1,
        1,
        LandSide.Axis,
        WeatherSeason.Fall,
        6,
        6,
        WeatherKind.Rainstorm,
        WeatherScope.ListedAreas,
        2,
        [WeatherArea.C, WeatherArea.D],
        0,
        0,
        0);
}
