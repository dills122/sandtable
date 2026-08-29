using System.Text;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class ContentBreakdownCohortTests
{
    private const string Capability = "land.breakdown-cohorts";

    [Fact]
    public void CohortIsAnImmutableContentFactAndStoresNoRuntimeBreakdownState()
    {
        var origin = ContentTestData.Origin("content.element.axis.breakdown-cohort");
        var cohort = new ContentBreakdownVehicleCohort(
            "axis-element.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            1,
            Cna1979Breakdown.ProfileTruckId,
            origin);

        Assert.Equal("axis-element.vehicle-cohort.trucks", cohort.CohortId);
        Assert.Equal(Cna1979Breakdown.VehicleTypeTruckId, cohort.VehicleTypeId);
        Assert.Equal(1, cohort.WorkingPointCount);
        Assert.Equal(Cna1979Breakdown.ProfileTruckId, cohort.ProfileId);
        Assert.Same(origin, cohort.Origin);
        Assert.Equal(
            ["CohortId", "Origin", "ProfileId", "VehicleTypeId", "WorkingPointCount"],
            typeof(ContentBreakdownVehicleCohort)
                .GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CohortRequiresStableIdsPositiveWorkingCountAndOrigin()
    {
        var origin = ContentTestData.Origin("content.element.axis.breakdown-cohort");

        Assert.Throws<ArgumentException>(() => Cohort(cohortId: "Axis Trucks", origin: origin));
        Assert.Throws<ArgumentException>(() => Cohort(vehicleTypeId: "Truck", origin: origin));
        Assert.Throws<ArgumentException>(() => Cohort(profileId: "Truck", origin: origin));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cohort(workingPointCount: 0, origin: origin));
        Assert.Throws<ArgumentNullException>(() => new ContentBreakdownVehicleCohort(
            "axis-element.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            1,
            Cna1979Breakdown.ProfileTruckId,
            null!));
    }

    [Fact]
    public void BreakdownCohortsAndCapabilityMustBeDeclaredTogether()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var cohortElement = WithCohort(baseline.Elements[0], Cohort());
        var cohortWithoutCapability = ContentTestData.Copy(
            baseline,
            elements: [cohortElement]);
        var capabilityWithoutCohort = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, Capability]);

        AssertIssue(
            ContentPackValidator.Validate(cohortWithoutCapability),
            "content.missing-capability",
            "/capabilities/land.breakdown-cohorts");
        AssertIssue(
            ContentPackValidator.Validate(capabilityWithoutCohort),
            "content.breakdown-cohort.unexpected-capability",
            "/capabilities/land.breakdown-cohorts");
    }

    [Fact]
    public void CohortIdsAreUniqueAcrossElements()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var first = WithCohort(baseline.Elements[0], Cohort());
        var second = new ContentCombatElement(
            "axis-element-two",
            first.SideId,
            first.ParentFormationId,
            first.OrganizationId,
            first.MobilityId,
            first.BaseCapabilityPointAllowance,
            first.PlacementMode,
            first.Origin,
            Cohort());
        var pack = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, Capability],
            elements: [first, second],
            scenarios: []);

        AssertIssue(
            ContentPackValidator.Validate(pack),
            "content.duplicate-id",
            "/elements/breakdownVehicleCohort/axis-element.vehicle-cohort.trucks");
    }

    [Fact]
    public void NonMotorizedElementsCannotCarryVehicleBreakdownCohorts()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var element = baseline.Elements[0];
        var invalid = new ContentCombatElement(
            element.ElementId,
            element.SideId,
            element.ParentFormationId,
            element.OrganizationId,
            Cna1979Movement.NonMotorizedMobilityId,
            element.BaseCapabilityPointAllowance,
            element.PlacementMode,
            element.Origin,
            Cohort());
        var pack = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, Capability],
            elements: [invalid]);

        AssertIssue(
            ContentPackValidator.Validate(pack),
            "content.breakdown-cohort.nonmotorized-element",
            $"/elements/{element.ElementId}/breakdownVehicleCohort");
    }

    [Fact]
    public void CapabilityRequiresEveryMotorizedElementToDeclareACohort()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var first = WithCohort(baseline.Elements[0], Cohort());
        var second = new ContentCombatElement(
            "axis-element-two",
            first.SideId,
            first.ParentFormationId,
            first.OrganizationId,
            Cna1979Movement.MotorizedMobilityId,
            first.BaseCapabilityPointAllowance,
            first.PlacementMode,
            first.Origin);
        var pack = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, Capability],
            elements: [first, second],
            scenarios: []);

        AssertIssue(
            ContentPackValidator.Validate(pack),
            "content.breakdown-cohort.missing-motorized-element",
            "/elements/axis-element-two/breakdownVehicleCohort");
    }

    [Fact]
    public void CnaCompatibilityRejectsUnknownBreakdownProfileAndVehicleType()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var unknown = WithCohort(
            baseline.Elements[0],
            Cohort(vehicleTypeId: "land.breakdown.vehicle-type.unknown", profileId: "land.breakdown.profile.unknown"));
        var pack = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, Capability],
            elements: [unknown]);

        var result = Cna1979ContentCompatibilityValidator.Validate(pack);

        AssertIssue(
            result,
            "vocabulary.unknown-id",
            $"/elements/{unknown.ElementId}/breakdownVehicleCohort/vehicleTypeId");
        AssertIssue(
            result,
            "vocabulary.unknown-id",
            $"/elements/{unknown.ElementId}/breakdownVehicleCohort/profileId");
        AssertIssue(
            result,
            "content.breakdown-cohort.profile-mismatch",
            $"/elements/{unknown.ElementId}/breakdownVehicleCohort/profileId");
    }

    [Fact]
    public void CanonicalSerializerAlwaysWritesObjectOrNullAndStrictlyReadsTheShape()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var nullCanonical = Encoding.UTF8.GetString(
            ContentPackSerializer.SerializeCanonical(baseline));
        var cohortElement = WithCohort(baseline.Elements[0], Cohort());
        var withCohort = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, Capability],
            elements: [cohortElement]);
        var objectCanonical = Encoding.UTF8.GetString(
            ContentPackSerializer.SerializeCanonical(withCohort));

        Assert.Contains("\"breakdownVehicleCohort\":null", nullCanonical, StringComparison.Ordinal);
        Assert.Contains(
            "\"breakdownVehicleCohort\":{\"cohortId\":\"axis-element.vehicle-cohort.trucks\",\"vehicleTypeId\":\"land.breakdown.vehicle-type.truck\",\"workingPointCount\":1,\"profileId\":\"land.breakdown.profile.truck\",\"origin\":",
            objectCanonical,
            StringComparison.Ordinal);
        Assert.Equal(
            withCohort,
            Assert.IsType<ContentPackDefinition>(
                ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(objectCanonical)).Definition));

        var missing = nullCanonical.Replace(
            ",\"breakdownVehicleCohort\":null",
            string.Empty,
            StringComparison.Ordinal);
        var malformed = objectCanonical.Replace(
            "\"workingPointCount\":1",
            "\"workingPointCount\":0",
            StringComparison.Ordinal);
        var unknownProperty = objectCanonical.Replace(
            "\"workingPointCount\":1,",
            "\"workingPointCount\":1,\"accumulatedBreakdownPoints\":0,",
            StringComparison.Ordinal);

        Assert.Equal(
            "content.missing-property",
            ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(missing)).ErrorCode);
        Assert.Equal(
            "content.invalid-value",
            ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(malformed)).ErrorCode);
        Assert.Equal(
            "content.unknown-property",
            ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(unknownProperty)).ErrorCode);
    }

    [Fact]
    public void LegacySchemaAndFormatAreRejectedByTheCleanCutReader()
    {
        var canonical = Encoding.UTF8.GetString(
            ContentPackSerializer.SerializeCanonical(ContentTestData.CreateMinimalPack()));
        var legacySchema = canonical.Replace("\"schemaVersion\":4", "\"schemaVersion\":3", StringComparison.Ordinal);
        var legacyFormat = canonical.Replace("sandtable.content-json.v3", "sandtable.content-json.v2", StringComparison.Ordinal);

        Assert.Equal(
            "content.unknown-version",
            ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(legacySchema)).ErrorCode);
        Assert.Equal(
            "content.unknown-format",
            ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(legacyFormat)).ErrorCode);
    }

    [Fact]
    public void SyntheticCatalogAssignsOneTruckPointOnlyToMotorizedElements()
    {
        var pack = Cna1979SyntheticContentCatalog.Artifact.Definition;
        var elements = pack.Elements.ToDictionary(element => element.ElementId);

        Assert.Contains(Capability, pack.Capabilities);
        AssertTruckCohort(elements["axis-element-a"]);
        Assert.Null(elements["axis-element-b"].BreakdownVehicleCohort);
        AssertTruckCohort(elements["commonwealth-element-a"]);
        Assert.Null(elements["commonwealth-element-b"].BreakdownVehicleCohort);
    }

    private static ContentBreakdownVehicleCohort Cohort(
        string cohortId = "axis-element.vehicle-cohort.trucks",
        string vehicleTypeId = "land.breakdown.vehicle-type.truck",
        int workingPointCount = 1,
        string profileId = "land.breakdown.profile.truck",
        ContentOrigin? origin = null) => new(
            cohortId,
            vehicleTypeId,
            workingPointCount,
            profileId,
            origin ?? ContentTestData.Origin("content.element.axis.breakdown-cohort"));

    private static ContentCombatElement WithCohort(
        ContentCombatElement element,
        ContentBreakdownVehicleCohort cohort) => new(
            element.ElementId,
            element.SideId,
            element.ParentFormationId,
            element.OrganizationId,
            element.MobilityId,
            element.BaseCapabilityPointAllowance,
            element.PlacementMode,
            element.Origin,
            cohort);

    private static void AssertTruckCohort(ContentCombatElement element)
    {
        var cohort = Assert.IsType<ContentBreakdownVehicleCohort>(element.BreakdownVehicleCohort);
        Assert.Equal($"{element.ElementId}.vehicle-cohort.trucks", cohort.CohortId);
        Assert.Equal(Cna1979Breakdown.VehicleTypeTruckId, cohort.VehicleTypeId);
        Assert.Equal(Cna1979Breakdown.ProfileTruckId, cohort.ProfileId);
        Assert.Equal(1, cohort.WorkingPointCount);
        Assert.NotEqual(element.Origin, cohort.Origin);
    }

    private static void AssertIssue(
        ContentValidationResult result,
        string code,
        string path) => Assert.Contains(
            result.Issues,
            issue => issue.Code == code && issue.Path == path);
}
