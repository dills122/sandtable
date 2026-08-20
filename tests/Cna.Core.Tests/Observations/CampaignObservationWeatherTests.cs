using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationWeatherTests
{
    [Fact]
    public void ObservationIsNullBeforeResolutionAndSideSafeAfterResolution()
    {
        var before = ReachWeather();
        var context = CampaignTestHarness.ContextFor(before);
        var beforeObservation = CampaignObservationProjector.Project(
            before, context, LandSide.Axis).Observation!;
        var decision = CampaignEngine.Decide(before,
            new ResolveWeather(before.StateVersion, before.SequencePosition.PositionId), context);
        var after = CampaignProjector.Apply(before, Assert.Single(decision.Events), context);
        var axis = CampaignObservationProjector.Project(after, context, LandSide.Axis).Observation!;
        var commonwealth = CampaignObservationProjector.Project(
            after, context, LandSide.Commonwealth).Observation!;

        Assert.Null(beforeObservation.Weather);
        Assert.Equal(axis.Weather, commonwealth.Weather);
        Assert.Equal(CampaignObservationWeatherSeason.Fall, axis.Weather!.Season);
        Assert.Equal(CampaignObservationWeatherKind.Rainstorm, axis.Weather.Kind);
        Assert.Equal([CampaignObservationWeatherArea.C, CampaignObservationWeatherArea.D],
            axis.Weather.AffectedAreas);
        var json = Encoding.UTF8.GetString(CampaignObservationSerializer.SerializeCanonical(axis));
        Assert.Contains("\"weather\":{\"contractVersion\":1,\"gameTurn\":1," +
            "\"operationStage\":1,\"season\":\"fall\",\"kind\":\"rainstorm\"," +
            "\"scope\":\"listed-areas\",\"affectedAreas\":[\"c\",\"d\"]}",
            json, StringComparison.Ordinal);
        Assert.DoesNotContain("firstDie", json, StringComparison.Ordinal);
        Assert.DoesNotContain("locationDie", json, StringComparison.Ordinal);
        Assert.DoesNotContain("randomCursor", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sourceId", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectorDoesNotLeakWeatherFromAnOlderPair()
    {
        var older = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis,
            WeatherSeason.Fall, 1, 1, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);

        var selected = CampaignObservationWeatherSelector.Select(2, 1, [older]);

        Assert.Null(selected);
    }

    [Theory]
    [InlineData(1, 1, (int)WeatherKind.Normal, (int)WeatherScope.None, null, "",
        "campaign-observation-weather-normal.v1.golden.json")]
    [InlineData(3, 6, (int)WeatherKind.Hot, (int)WeatherScope.Global, null, "",
        "campaign-observation-weather-hot.v1.golden.json")]
    [InlineData(6, 6, (int)WeatherKind.Rainstorm, (int)WeatherScope.ListedAreas, 2, "C,D",
        "campaign-observation-weather-foul.v1.golden.json")]
    public void NestedWeatherContractMatchesCompleteCanonicalGolden(
        int firstDie,
        int secondDie,
        int kindValue,
        int scopeValue,
        int? locationDie,
        string affectedAreaTokens,
        string fixtureName)
    {
        var snapshot = ReachWeather();
        var context = CampaignTestHarness.ContextFor(snapshot);
        var baseline = CampaignObservationProjector.Project(
            snapshot, context, LandSide.Axis).Observation!;
        var affectedAreas = string.IsNullOrEmpty(affectedAreaTokens)
            ? []
            : affectedAreaTokens.Split(',')
                .Select(value => Enum.Parse<WeatherArea>(value, ignoreCase: false))
                .ToArray();
        var authority = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis,
            WeatherSeason.Fall, firstDie, secondDie, (WeatherKind)kindValue,
            (WeatherScope)scopeValue, locationDie, affectedAreas, 0, 0, 0);
        var weather = CampaignObservationWeatherSelector.Select(1, 1, [authority]);
        var observation = new CampaignObservation(CampaignObservation.CurrentContractVersion,
            baseline.PolicyId, baseline.CampaignId, baseline.StateVersion, baseline.RulesetHash,
            baseline.ScenarioId, baseline.Observer, baseline.Position, weather,
            baseline.Locations, baseline.Edges, baseline.OwnElements);

        using var document = JsonDocument.Parse(
            CampaignObservationSerializer.SerializeCanonical(observation));
        var actual = Encoding.UTF8.GetBytes(
            document.RootElement.GetProperty("weather").GetRawText());
        var expected = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Observations", "Fixtures", fixtureName));

        Assert.Equal((byte)'\n', expected[^1]);
        Assert.Equal(expected.AsSpan(0, expected.Length - 1).ToArray(), actual);
    }

    private static CampaignSnapshot ReachWeather()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create("campaign-observation-weather",
                Cna1979Ruleset.Manifest.Hash, 0, setup.SetupId, setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2,
                "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3,
                "land.position.naval-convoy.tactical-shipping"),
            new DeclareInitiativeOrder(4,
                "land.position.operation-1.initiative-declaration", 1,
                LandSide.Axis, InitiativeOrderChoice.ActFirst),
        ];
        return CampaignTestHarness.Execute(commands).Snapshot!;
    }
}
