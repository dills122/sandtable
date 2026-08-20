using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class WeatherCampaignTests
{
    [Fact]
    public void WeatherEventUsesExactCanonicalBytes()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var weather = ReachWeather(setup, 0);
        var determined = Assert.IsType<WeatherDetermined>(Assert.Single(
            CampaignTestHarness.Decide(weather,
                new ResolveWeather(weather.StateVersion, weather.SequencePosition.PositionId)).Events));
        var actual = System.Text.Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(determined));

        var expected = "{\"contractVersion\":1,\"eventType\":\"weather-determined\"," +
            "\"campaignId\":\"campaign-weather\",\"stateVersion\":6," +
            "\"fromPositionId\":\"land.position.operation-1.weather-determination\"," +
            "\"gameTurn\":1,\"operationStage\":1,\"determiningSide\":\"axis\"," +
            "\"season\":\"fall\",\"firstDie\":6,\"secondDie\":6," +
            "\"kind\":\"rainstorm\",\"scope\":\"listed-areas\",\"locationDie\":2," +
            "\"affectedAreas\":[\"c\",\"d\"],\"fuelWaterReductionSubjectCount\":0," +
            "\"restoredWellCount\":0,\"damagedGroundedAircraftCount\":0," +
            "\"randomCursorAfter\":3,\"sequencePosition\":{\"contractVersion\":2," +
            "\"positionId\":\"land.position.operation-1.organization\",\"gameTurn\":1," +
            "\"operationStage\":1,\"stageId\":\"land.stage.operation\"," +
            "\"phaseId\":\"land.phase.organization\",\"segmentId\":null,\"stepId\":null," +
            "\"actorRole\":\"none\",\"activeSide\":null,\"sources\":[{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"5.2\"}]}," +
            "\"sources\":[{\"sourceId\":\"cna-1979.1.ruling.weather-season-boundary\"," +
            "\"locator\":\"selected-behavior\"},{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"weather.no-immediate-effect-subjects.v1\"},{" +
            "\"sourceId\":\"spi-1979-common-charts\",\"locator\":\"29.61\"},{" +
            "\"sourceId\":\"spi-1979-common-charts\",\"locator\":\"29.7\"},{" +
            "\"sourceId\":\"spi-1979-errata\",\"locator\":\"29.1\"},{" +
            "\"sourceId\":\"spi-1979-errata\",\"locator\":\"29.61\"},{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"29.0\"},{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"29.1\"},{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"29.53\"}]}";

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0UL, 1, (int)WeatherSeason.Fall)]
    [InlineData(7UL, 43, (int)WeatherSeason.Summer)]
    public void BothSyntheticSetupsResolveWeatherAndStopAtOrganization(
        ulong seed,
        int expectedGameTurn,
        int expectedSeason)
    {
        var setup = Cna1979SetupCatalog.Definitions.Single(value =>
            value.InitialGameTurn == expectedGameTurn);
        var weather = ReachWeather(setup, seed);
        var result = CampaignTestHarness.Decide(
            weather,
            new ResolveWeather(weather.StateVersion, weather.SequencePosition.PositionId));

        Assert.True(result.IsAccepted);
        var determined = Assert.IsType<WeatherDetermined>(Assert.Single(result.Events));
        var projected = CampaignTestHarness.Apply(weather, determined);
        var record = Assert.Single(projected.OperationStageWeather);
        Assert.Equal(expectedGameTurn, record.GameTurn);
        Assert.Equal((WeatherSeason)expectedSeason, record.Season);
        Assert.Equal(LandPhaseIds.Organization, projected.PhaseId);
        Assert.Equal(weather.OperationStage, projected.OperationStage);
        Assert.Equal(determined.RandomCursorAfter, projected.RandomState.NextByteCursor);
        var eventBytes = CampaignEventSerializer.Serialize(determined);
        Assert.Equal(determined, CampaignEventSerializer.Deserialize(eventBytes));
        var snapshotBytes = CampaignSnapshotSerializer.Serialize(projected);
        Assert.Equal(projected, CampaignSnapshotSerializer.Deserialize(snapshotBytes));
        Assert.Equal(
            snapshotBytes,
            CampaignSnapshotSerializer.Serialize(CampaignTestHarness.Replay(
                [.. CreateHistoryToWeather(setup, seed), determined])));
        var reused = CampaignTestHarness.Decide(projected,
            new ResolveWeather(projected.StateVersion, projected.SequencePosition.PositionId));
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition, reused.RejectionReason);
        Assert.Empty(reused.Events);
    }

    [Fact]
    public void StaleWeatherCommandRejectsWithoutEventsOrRandomMutation()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var weather = ReachWeather(setup, 0);

        var result = CampaignTestHarness.Decide(
            weather,
            new ResolveWeather(weather.StateVersion - 1, weather.SequencePosition.PositionId));

        Assert.Equal(CampaignCommandRejectionReason.StaleState, result.RejectionReason);
        Assert.Empty(result.Events);
        Assert.Equal(0UL, weather.RandomState.NextByteCursor);
    }

    [Fact]
    public void ReadersAndProjectionRejectForgedWeatherEvidence()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var weather = ReachWeather(setup, 0);
        var determined = Assert.IsType<WeatherDetermined>(Assert.Single(
            CampaignTestHarness.Decide(weather,
                new ResolveWeather(weather.StateVersion, weather.SequencePosition.PositionId)).Events));
        var canonical = System.Text.Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(determined));
        var forgedSeason = canonical.Replace(
            "\"season\":\"fall\"",
            "\"season\":\"winter\"",
            StringComparison.Ordinal);
        var forgedSource = canonical.Replace(
            "\"locator\":\"29.53\"",
            "\"locator\":\"29.54\"",
            StringComparison.Ordinal);
        var forgedCursor = canonical.Replace(
            "\"randomCursorAfter\":3",
            "\"randomCursorAfter\":4",
            StringComparison.Ordinal);
        var reorderedEnvelope = canonical.Replace(
            "{\"contractVersion\":1,\"eventType\":\"weather-determined\",",
            "{\"eventType\":\"weather-determined\",\"contractVersion\":1,",
            StringComparison.Ordinal);

        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignEventSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(forgedSeason)));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignEventSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(forgedSource)));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CampaignEventSerializer.Deserialize(
                System.Text.Encoding.UTF8.GetBytes(reorderedEnvelope)));
        var cursorEvent = CampaignEventSerializer.Deserialize(
            System.Text.Encoding.UTF8.GetBytes(forgedCursor));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(weather, cursorEvent));
    }

    private static CampaignSnapshot ReachWeather(CampaignSetupDefinition setup, ulong seed) =>
        CampaignTestHarness.Replay(CreateHistoryToWeather(setup, seed));

    private static CampaignEvent[] CreateHistoryToWeather(
        CampaignSetupDefinition setup,
        ulong seed)
    {
        CampaignCommand[] commands =
        [
            CampaignTestHarness.Create(
                "campaign-weather",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                setup.SetupId,
                setup.Hash),
            new ResolveInitiative(1, "land.position.initiative-determination"),
            new ResolveNoObligationNavalConvoySchedule(2, "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(
                3,
                "land.position.naval-convoy.tactical-shipping"),
        ];
        var preamble = CampaignTestHarness.Execute(commands);
        var declaration = CampaignTestHarness.Decide(
            preamble.Snapshot,
            new DeclareInitiativeOrder(
                4,
                "land.position.operation-1.initiative-declaration",
                1,
                preamble.Snapshot!.InitiativeHolder!.Value,
                InitiativeOrderChoice.ActFirst));

        return [.. preamble.Events, Assert.Single(declaration.Events)];
    }
}
