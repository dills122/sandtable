using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignEventSerializationTests
{
    [Fact]
    public void CampaignCreatedUsesExactCanonicalBytesAndRoundTrips()
    {
        var created = CreateHistory(Cna1979SetupCatalog.Definitions[0], 12345)[0];

        var bytes = CampaignEventSerializer.Serialize(created);
        var actual = Encoding.UTF8.GetString(bytes);
        var expected = "{\"contractVersion\":4,\"eventType\":\"campaign-created\"," +
            "\"campaignId\":\"campaign-1\",\"stateVersion\":1,\"rulesetHash\":\"" +
            Cna1979Ruleset.Manifest.Hash +
            "\",\"setup\":{\"schemaVersion\":4," +
            "\"setupId\":\"rules-lab.initiative.predetermined\"," +
            "\"setupHash\":\"sha256:5ecf84d21a7ff95112b9b662915f6858926532d30be5a0eee3f1a45752fdc80a\"," +
            "\"isSynthetic\":true,\"initialGameTurn\":1," +
            "\"initialInitiative\":{\"kind\":\"predetermined\",\"holder\":\"axis\"}," +
            "\"openingPreamble\":{\"contractVersion\":1," +
            "\"kind\":\"no-opening-naval-convoy-obligations\"," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"opening-preamble.no-naval-convoy-obligations.v1\"}]}," +
            "\"weather\":{\"contractVersion\":1," +
            "\"kind\":\"no-immediate-weather-effect-subjects\"," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"weather.no-immediate-effect-subjects.v1\"}]}," +
            "\"content\":{\"schemaVersion\":2,\"formatId\":\"sandtable.content-json.v1\"," +
            "\"packId\":\"rules-lab.content.movement-contact.v1\",\"rulesetId\":\"cna-1979.1\"," +
            "\"hash\":\"sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99\"," +
            "\"scenarioId\":\"movement-contact-lab\"}," +
            "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"initiative.predetermined-axis.v1\"}]}," +
            "\"initialWorld\":{" +
            "\"contractVersion\":1,\"elements\":[" +
            "{\"elementId\":\"axis-element-a\",\"currentLocationId\":\"west\"}," +
            "{\"elementId\":\"axis-element-b\",\"currentLocationId\":\"north-west\"}," +
            "{\"elementId\":\"commonwealth-element-a\",\"currentLocationId\":\"east\"}," +
            "{\"elementId\":\"commonwealth-element-b\",\"currentLocationId\":\"south-east\"}]}," +
            "\"randomState\":{\"contractVersion\":1," +
            "\"algorithmId\":\"sandtable.sha256-counter.v1\",\"seed\":12345," +
            "\"nextByteCursor\":0},\"sequencePosition\":{\"contractVersion\":2," +
            "\"positionId\":\"land.position.initiative-determination\"," +
            "\"gameTurn\":1,\"operationStage\":0," +
            "\"stageId\":\"land.stage.initiative-determination\"," +
            "\"phaseId\":\"land.phase.initiative-determination\"," +
            "\"segmentId\":null,\"stepId\":null,\"actorRole\":\"none\"," +
            "\"activeSide\":null,\"sources\":[{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"5.2\"}]}}";

        Assert.Equal(expected, actual);
        Assert.Equal(created, CampaignEventSerializer.Deserialize(bytes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void InitiativeDeterminedRoundTripsAndReplaysCanonically(ulong seed)
    {
        var history = CreateHistory(Cna1979SetupCatalog.Definitions[1], seed);
        var canonicalEvents = history
            .Select(CampaignEventSerializer.Serialize)
            .ToArray();
        var deserialized = canonicalEvents
            .Select(bytes => CampaignEventSerializer.Deserialize(bytes))
            .ToArray();

        var original = CampaignTestHarness.Replay(history);
        var replayed = CampaignTestHarness.Replay(deserialized);

        Assert.Equal(history, deserialized);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(original),
            CampaignSnapshotSerializer.Serialize(replayed));
    }

    [Fact]
    public void MultiTieInitiativeEventUsesExactCanonicalBytes()
    {
        var history = CreateHistory(Cna1979SetupCatalog.Definitions[1], 7);
        var actual = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(history[1]));
        var expected = "{\"contractVersion\":2,\"eventType\":\"initiative-determined\"," +
            "\"campaignId\":\"campaign-1\",\"stateVersion\":2," +
            "\"fromPositionId\":\"land.position.initiative-determination\"," +
            "\"outcome\":{\"kind\":\"contested\",\"axisFacts\":{" +
            "\"rommelLocation\":\"off-map-or-unavailable\"," +
            "\"germanLandCombatUnitLocations\":[\"qualifying-game-map\"]}," +
            "\"axisPresence\":\"german-land-combat-unit-on-qualifying-game-map\"," +
            "\"rounds\":[{\"round\":1,\"axisDie\":5,\"axisRating\":3," +
            "\"axisTotal\":8,\"commonwealthDie\":4,\"commonwealthRating\":4," +
            "\"commonwealthTotal\":8},{\"round\":2,\"axisDie\":5," +
            "\"axisRating\":3,\"axisTotal\":8,\"commonwealthDie\":6," +
            "\"commonwealthRating\":4,\"commonwealthTotal\":10}]," +
            "\"holder\":\"commonwealth\"}," +
            "\"randomAlgorithmId\":\"sandtable.sha256-counter.v1\"," +
            "\"randomCursorBefore\":0,\"randomCursorAfter\":5," +
            "\"sequencePosition\":{\"contractVersion\":2," +
            "\"positionId\":\"land.position.naval-convoy.schedule\"," +
            "\"gameTurn\":43,\"operationStage\":0," +
            "\"stageId\":\"land.stage.naval-convoy\"," +
            "\"phaseId\":\"land.phase.naval-convoy-schedule\"," +
            "\"segmentId\":null,\"stepId\":null,\"actorRole\":\"none\"," +
            "\"activeSide\":null,\"sources\":[{\"sourceId\":\"spi-1979-land-rules\"," +
            "\"locator\":\"5.2\"}]},\"sources\":[{" +
            "\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"initiative.contested-turn-43.v1\"},{" +
            "\"sourceId\":\"spi-1979-common-charts\"," +
            "\"locator\":\"initiative-ratings\"},{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"7.12\"},{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"7.13\"},{" +
            "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"7.14\"}]}";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IdenticalInputsProduceIdenticalBytesAndSeedsOnlyChangeRandomConsequences()
    {
        var first = CreateHistory(Cna1979SetupCatalog.Definitions[1], 0);
        var repeated = CreateHistory(Cna1979SetupCatalog.Definitions[1], 0);
        var different = CreateHistory(Cna1979SetupCatalog.Definitions[1], 7);

        var firstDetermined = Assert.IsType<InitiativeDetermined>(first[1]);
        var differentDetermined = Assert.IsType<InitiativeDetermined>(different[1]);
        var firstOutcome = Assert.IsType<ContestedInitiativeOutcome>(firstDetermined.Outcome);
        var differentOutcome = Assert.IsType<ContestedInitiativeOutcome>(
            differentDetermined.Outcome);

        var firstBytes = CampaignEventSerializer.Serialize(first[1]);
        var repeatedBytes = CampaignEventSerializer.Serialize(repeated[1]);
        var differentBytes = CampaignEventSerializer.Serialize(different[1]);

        Assert.Equal(firstBytes, repeatedBytes);
        Assert.NotEqual(firstBytes, differentBytes);
        Assert.Equal(firstDetermined.ContractVersion, differentDetermined.ContractVersion);
        Assert.Equal(firstDetermined.CampaignId, differentDetermined.CampaignId);
        Assert.Equal(firstDetermined.StateVersion, differentDetermined.StateVersion);
        Assert.Equal(firstDetermined.FromPositionId, differentDetermined.FromPositionId);
        Assert.Equal(
            firstDetermined.RandomAlgorithmId,
            differentDetermined.RandomAlgorithmId);
        Assert.Equal(firstDetermined.RandomCursorBefore, differentDetermined.RandomCursorBefore);
        Assert.NotEqual(firstDetermined.RandomCursorAfter, differentDetermined.RandomCursorAfter);
        Assert.Equal(firstDetermined.SequencePosition, differentDetermined.SequencePosition);
        Assert.Equal(firstDetermined.Sources, differentDetermined.Sources);
        Assert.Equal(firstOutcome.AxisFacts, differentOutcome.AxisFacts);
        Assert.Equal(firstOutcome.AxisPresence, differentOutcome.AxisPresence);
        Assert.NotEqual(firstOutcome.Rounds, differentOutcome.Rounds);
        Assert.Equal(firstOutcome.Holder, differentOutcome.Holder);
    }

    [Fact]
    public void EventReaderRejectsExtraUnknownAndInconsistentSemanticValues()
    {
        var history = CreateHistory(Cna1979SetupCatalog.Definitions[1], 0);
        var created = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(history[0]));
        var determined = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(history[1]));
        var extra = created.Replace(
            "{\"contractVersion\":2,",
            "{\"extra\":true,\"contractVersion\":2,",
            StringComparison.Ordinal);
        var unknownType = created.Replace(
            "\"campaign-created\"",
            "\"unknown-event\"",
            StringComparison.Ordinal);
        var missingWeather = created.Replace(
            "\"weather\":",
            "\"missingWeather\":",
            StringComparison.Ordinal);
        var unknownPresence = determined.Replace(
            "\"german-land-combat-unit-on-qualifying-game-map\"",
            "\"unknown-presence\"",
            StringComparison.Ordinal);
        var forgedTotal = determined.Replace(
            "\"axisTotal\":9",
            "\"axisTotal\":10",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => Deserialize(extra));
        Assert.Throws<JsonException>(() => Deserialize(unknownType));
        Assert.Throws<JsonException>(() => Deserialize(missingWeather));
        Assert.Throws<JsonException>(() => Deserialize(unknownPresence));
        Assert.Throws<JsonException>(() => Deserialize(forgedTotal));
    }

    [Fact]
    public void ReplayRejectsDeserializedCursorAndSourceUnionForgeries()
    {
        var history = CreateHistory(Cna1979SetupCatalog.Definitions[1], 0);
        var created = CampaignEventSerializer.Deserialize(
            CampaignEventSerializer.Serialize(history[0]));
        var determined = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(history[1]));
        var forgedCursor = determined.Replace(
            "\"randomCursorAfter\":2",
            "\"randomCursorAfter\":3",
            StringComparison.Ordinal);
        var forgedSource = determined.Replace(
            "\"locator\":\"7.13\"",
            "\"locator\":\"7.99\"",
            StringComparison.Ordinal);

        var cursorEvent = Deserialize(forgedCursor);
        var sourceEvent = Deserialize(forgedSource);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([created, cursorEvent]));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Replay([created, sourceEvent]));
    }

    private static CampaignEvent[] CreateHistory(CampaignSetupDefinition setup, ulong seed)
    {
        var createResult = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                setup.SetupId,
                setup.Hash));
        var created = Assert.IsType<CampaignCreated>(Assert.Single(createResult.Events));
        var initial = CampaignTestHarness.Apply(null, created);
        var initiativeResult = CampaignTestHarness.Decide(
            initial,
            new ResolveInitiative(initial.StateVersion, initial.SequencePosition.PositionId));
        var determined = Assert.IsType<InitiativeDetermined>(
            Assert.Single(initiativeResult.Events));
        return [created, determined];
    }

    private static CampaignEvent Deserialize(string json) =>
        CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(json));
}
