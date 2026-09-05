using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class StageEntryEventContractTests
{
    private const string OrganizationType = "no-obligation-organization-resolved";
    private const string ArrivalType = "no-obligation-naval-convoy-arrival-resolved";
    private const string AssignmentType = "no-obligation-fleet-assignment-resolved";
    private const string RepairType = "no-obligation-fleet-repair-resolved";

    [Fact]
    public void FourMechanicsHaveDistinctFrozenRecordAndConstructorContracts()
    {
        Type[] eventTypes =
        [
            typeof(NoObligationOrganizationResolved),
            typeof(NoObligationNavalConvoyArrivalResolved),
            typeof(NoObligationFleetAssignmentResolved),
            typeof(NoObligationFleetRepairResolved),
        ];
        string[] expectedParameterNames =
        [
            "campaignId", "stateVersion", "fromPositionId", "gameTurn", "operationStage",
            "sequencePosition", "sources",
        ];
        Type[] expectedParameterTypes =
        [
            typeof(string), typeof(long), typeof(string), typeof(int), typeof(int),
            typeof(LandSequencePosition), typeof(IReadOnlyList<RuleReference>),
        ];

        Assert.Equal(4, eventTypes.Distinct().Count());
        foreach (var eventType in eventTypes)
        {
            Assert.True(eventType.IsNotPublic);
            Assert.True(eventType.IsSealed);
            Assert.True(eventType.IsAssignableTo(typeof(CampaignEvent)));
            var constructor = Assert.Single(eventType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                value => value.GetParameters().Length == expectedParameterNames.Length);
            Assert.Equal(expectedParameterNames, constructor.GetParameters().Select(value => value.Name));
            Assert.Equal(expectedParameterTypes, constructor.GetParameters().Select(value => value.ParameterType));
            Assert.DoesNotContain(eventType.GetProperties(), property =>
                property.PropertyType == typeof(bool) || property.Name.Contains("Audience", StringComparison.Ordinal)
                || property.Name.Contains("Subject", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void FourMechanicsUseExactCanonicalBytesAndRoundTripAsTheirDistinctTypes()
    {
        foreach (var (campaignEvent, eventType, expected) in Cases())
        {
            var canonical = CampaignEventSerializer.Serialize(campaignEvent);
            var deserialized = CampaignEventSerializer.Deserialize(canonical);

            Assert.Equal(expected, Encoding.UTF8.GetString(canonical));
            Assert.Equal(campaignEvent, deserialized);
            Assert.Equal(campaignEvent.GetType(), deserialized.GetType());
            Assert.Equal(1, campaignEvent.ContractVersion);
            Assert.Contains($"\"eventType\":\"{eventType}\"", expected, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WriterRejectsForgedInheritedAuthority()
    {
        foreach (var (campaignEvent, _, _) in Cases())
        {
            Assert.Throws<JsonException>(() => CampaignEventSerializer.Serialize(
                campaignEvent with { ContractVersion = 2 }));
            Assert.Throws<JsonException>(() => CampaignEventSerializer.Serialize(
                campaignEvent with { CampaignId = string.Empty }));
            Assert.Throws<JsonException>(() => CampaignEventSerializer.Serialize(
                campaignEvent with { StateVersion = campaignEvent.StateVersion + 1 }));
        }
    }

    [Fact]
    public void ReaderRejectsNoncanonicalEventBytes()
    {
        foreach (var (_, eventType, canonical) in Cases())
        {
            string[] noncanonical =
            [
                $"{canonical}\n",
                $" {canonical}",
                canonical.Replace(
                    $"{{\"contractVersion\":1,\"eventType\":\"{eventType}\",",
                    $"{{\"eventType\":\"{eventType}\",\"contractVersion\":1,",
                    StringComparison.Ordinal),
            ];

            foreach (var value in noncanonical)
            {
                Assert.Throws<JsonException>(() => Deserialize(value));
            }
        }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    [InlineData("tr-TR")]
    public void CanonicalBytesAreCultureInvariant(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            foreach (var (campaignEvent, _, expected) in Cases())
            {
                Assert.Equal(expected, Encoding.UTF8.GetString(
                    CampaignEventSerializer.Serialize(campaignEvent)));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void ReaderRejectsNoncanonicalEnvelopeAndMalformedAuthorityValues()
    {
        var canonical = Cases()[0].Expected;
        string[] malformed =
        [
            canonical.Replace("\"contractVersion\":1", "\"contractVersion\":2", StringComparison.Ordinal),
            canonical.Replace(OrganizationType, "No-Obligation-Organization-Resolved", StringComparison.Ordinal),
            canonical.Replace("\"campaignId\":\"campaign-stage-entry\"", "\"campaignId\":\"\"", StringComparison.Ordinal),
            canonical.Replace("\"stateVersion\":7", "\"stateVersion\":6", StringComparison.Ordinal),
            canonical.Replace("\"fromPositionId\":\"land.position.operation-1.organization\"",
                "\"fromPositionId\":\"land.position.operation-1.weather-determination\"", StringComparison.Ordinal),
            canonical.Replace("\"gameTurn\":1", "\"gameTurn\":0", StringComparison.Ordinal),
            canonical.Replace("\"operationStage\":1", "\"operationStage\":2", StringComparison.Ordinal),
            canonical.Replace("\"phaseId\":\"land.phase.naval-convoy-arrival\"",
                "\"phaseId\":\"land.phase.organization\"", StringComparison.Ordinal),
            canonical.Replace("\"actorRole\":\"none\"", "\"actorRole\":\"initiative-holder\"", StringComparison.Ordinal),
            canonical.Replace("{\"contractVersion\":1,\"eventType\":",
                "{\"eventType\":\"ignored\",\"contractVersion\":1,\"eventType\":", StringComparison.Ordinal),
            canonical.Replace("\"campaignId\":\"campaign-stage-entry\",", string.Empty, StringComparison.Ordinal),
            canonical.Replace("\"campaignId\":\"campaign-stage-entry\",",
                "\"campaignId\":\"campaign-stage-entry\",\"extra\":true,", StringComparison.Ordinal),
        ];

        foreach (var value in malformed)
        {
            Assert.Throws<JsonException>(() => Deserialize(value));
        }
    }

    [Fact]
    public void ReaderRejectsAlteredReorderedMissingOrExtraMechanicSources()
    {
        foreach (var (_, _, canonical) in Cases())
        {
            using var document = JsonDocument.Parse(canonical);
            var eventSources = document.RootElement.GetProperty("sources");
            var sourceArray = eventSources.GetRawText();
            var synthetic = eventSources[0].GetRawText();
            var primary = eventSources[1].GetRawText();
            var reorderedSynthetic = "{\"locator\":\"stage-entry.no-obligations.v1\","
                + "\"sourceId\":\"sandtable-rules-lab\"}";
            var extraSynthetic = synthetic[..^1] + ",\"extra\":true}";
            string[] malformed =
            [
                canonical.Replace("stage-entry.no-obligations.v1", "stage-entry.no-obligations.v2", StringComparison.Ordinal),
                canonical.Replace(primary, primary.Replace("5.2.", "5.3.", StringComparison.Ordinal),
                    StringComparison.Ordinal),
                canonical.Replace(sourceArray, $"[{primary}]", StringComparison.Ordinal),
                canonical.Replace(sourceArray, $"[{primary},{synthetic}]", StringComparison.Ordinal),
                canonical.Replace(sourceArray, $"[{synthetic},{synthetic},{primary}]", StringComparison.Ordinal),
                canonical.Replace(sourceArray, $"[{reorderedSynthetic},{primary}]", StringComparison.Ordinal),
                canonical.Replace(sourceArray, $"[{extraSynthetic},{primary}]", StringComparison.Ordinal),
            ];

            foreach (var value in malformed)
            {
                Assert.Throws<JsonException>(() => Deserialize(value));
            }
        }
    }

    [Fact]
    public void ReaderRejectsUsingOneMechanicTypeForAnotherMechanicsPayload()
    {
        var cases = Cases();

        for (var index = 0; index < cases.Count; index++)
        {
            var nextType = cases[(index + 1) % cases.Count].EventType;
            var forged = cases[index].Expected.Replace(cases[index].EventType, nextType,
                StringComparison.Ordinal);

            Assert.Throws<JsonException>(() => Deserialize(forged));
        }
    }

    private static IReadOnlyList<(CampaignEvent Event, string EventType, string Expected)> Cases()
    {
        var positions = Cna1979LandSequence.CreateTurn(1);
        var organizationSuccessor = positions.Single(value =>
            value.OperationStage == 1 && value.PhaseId == LandPhaseIds.NavalConvoyArrival);
        var arrivalSuccessor = positions.Single(value =>
            value.OperationStage == 1 && value.SegmentId == LandSegmentIds.FleetAssignment);
        var assignmentSuccessor = positions.Single(value =>
            value.OperationStage == 1 && value.SegmentId == LandSegmentIds.FleetRepair);
        var repairSuccessor = positions.Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.ReserveDesignation
            && value.ActorRole == LandActorRole.FirstActingSide);

        return
        [
            (new NoObligationOrganizationResolved("campaign-stage-entry", 7,
                "land.position.operation-1.organization", 1, 1, organizationSuccessor,
                OrganizationSources()), OrganizationType,
                Expected(OrganizationType, 7, "land.position.operation-1.organization",
                    OrganizationSuccessorJson, "5.2.organization")),
            (new NoObligationNavalConvoyArrivalResolved("campaign-stage-entry", 8,
                "land.position.operation-1.naval-convoy-arrival", 1, 1, arrivalSuccessor,
                ArrivalSources()), ArrivalType,
                Expected(ArrivalType, 8, "land.position.operation-1.naval-convoy-arrival",
                    ArrivalSuccessorJson, "5.2.naval-convoy-arrival")),
            (new NoObligationFleetAssignmentResolved("campaign-stage-entry", 9,
                "land.position.operation-1.commonwealth-fleet.assignment", 1, 1,
                assignmentSuccessor, FleetSources()), AssignmentType,
                Expected(AssignmentType, 9,
                    "land.position.operation-1.commonwealth-fleet.assignment",
                    AssignmentSuccessorJson, "5.2.commonwealth-fleet")),
            (new NoObligationFleetRepairResolved("campaign-stage-entry", 10,
                "land.position.operation-1.commonwealth-fleet.repair", 1, 1,
                repairSuccessor, FleetSources()), RepairType,
                Expected(RepairType, 10, "land.position.operation-1.commonwealth-fleet.repair",
                    RepairSuccessorJson, "5.2.commonwealth-fleet")),
        ];
    }

    private static RuleReference[] OrganizationSources() =>
    [
        new("sandtable-rules-lab", "stage-entry.no-obligations.v1"),
        new("spi-1979-land-rules", "5.2.organization"),
    ];

    private static RuleReference[] ArrivalSources() =>
    [
        new("sandtable-rules-lab", "stage-entry.no-obligations.v1"),
        new("spi-1979-land-rules", "5.2.naval-convoy-arrival"),
    ];

    private static RuleReference[] FleetSources() =>
    [
        new("sandtable-rules-lab", "stage-entry.no-obligations.v1"),
        new("spi-1979-land-rules", "5.2.commonwealth-fleet"),
    ];

    private static string Expected(string eventType, int stateVersion, string fromPositionId,
        string positionJson, string primaryLocator) =>
        $"{{\"contractVersion\":1,\"eventType\":\"{eventType}\"," +
        $"\"campaignId\":\"campaign-stage-entry\",\"stateVersion\":{stateVersion}," +
        $"\"fromPositionId\":\"{fromPositionId}\",\"gameTurn\":1," +
        $"\"operationStage\":1,\"sequencePosition\":{positionJson}," +
        "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\"," +
        "\"locator\":\"stage-entry.no-obligations.v1\"},{" +
        "\"sourceId\":\"spi-1979-land-rules\"," +
        $"\"locator\":\"{primaryLocator}\"}}]}}";

    private const string OrganizationSuccessorJson =
        "{\"contractVersion\":3,\"positionId\":\"land.position.operation-1.naval-convoy-arrival\"," +
        "\"gameTurn\":1,\"operationStage\":1,\"stageId\":\"land.stage.operation\"," +
        "\"phaseId\":\"land.phase.naval-convoy-arrival\",\"segmentId\":null,\"stepId\":null," +
        "\"actorRole\":\"none\",\"activeSide\":null,\"sources\":[{" +
        "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"5.2\"}]}";

    private const string ArrivalSuccessorJson =
        "{\"contractVersion\":3," +
        "\"positionId\":\"land.position.operation-1.commonwealth-fleet.assignment\"," +
        "\"gameTurn\":1,\"operationStage\":1,\"stageId\":\"land.stage.operation\"," +
        "\"phaseId\":\"land.phase.commonwealth-fleet\"," +
        "\"segmentId\":\"land.segment.fleet-assignment\",\"stepId\":null," +
        "\"actorRole\":\"commonwealth\",\"activeSide\":\"commonwealth\"," +
        "\"sources\":[{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"5.2\"}]}";

    private const string AssignmentSuccessorJson =
        "{\"contractVersion\":3," +
        "\"positionId\":\"land.position.operation-1.commonwealth-fleet.repair\"," +
        "\"gameTurn\":1,\"operationStage\":1,\"stageId\":\"land.stage.operation\"," +
        "\"phaseId\":\"land.phase.commonwealth-fleet\"," +
        "\"segmentId\":\"land.segment.fleet-repair\",\"stepId\":null," +
        "\"actorRole\":\"commonwealth\",\"activeSide\":\"commonwealth\"," +
        "\"sources\":[{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"5.2\"}]}";

    private const string RepairSuccessorJson =
        "{\"contractVersion\":3," +
        "\"positionId\":\"land.position.operation-1.first-player.reserve-designation\"," +
        "\"gameTurn\":1,\"operationStage\":1,\"stageId\":\"land.stage.operation\"," +
        "\"phaseId\":\"land.phase.reserve-designation\",\"segmentId\":null,\"stepId\":null," +
        "\"actorRole\":\"first-acting-side\",\"activeSide\":null,\"sources\":[{" +
        "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"5.2\"},{" +
        "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"7.11\"},{" +
        "\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"7.14\"}]}";

    private static CampaignEvent Deserialize(string value) =>
        CampaignEventSerializer.Deserialize(Encoding.UTF8.GetBytes(value));
}
