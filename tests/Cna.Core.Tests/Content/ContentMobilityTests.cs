using System.Text;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class ContentMobilityTests
{
    [Fact]
    public void ContentContractVersionsAndCapabilityAdvertiseElementMobility()
    {
        var pack = ContentTestData.CreateMinimalPack();

        Assert.Equal(4, ContentPackDefinition.CurrentSchemaVersion);
        Assert.Equal("sandtable.content-json.v3", ContentPackDefinition.CanonicalFormatId);
        Assert.Contains("land.element-mobility", pack.Capabilities);
    }

    [Fact]
    public void ElementsRequireTheMobilityCapabilityDeclaration()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var withoutCapability = ContentTestData.Copy(
            baseline,
            capabilities: baseline.Capabilities.Where(
                capability => capability != "land.element-mobility"));

        var result = ContentPackValidator.Validate(withoutCapability);

        Assert.Contains(
            result.Issues,
            issue => issue.Code == "content.missing-capability"
                && issue.Path == "/capabilities/land.element-mobility");
    }

    [Fact]
    public void CombatElementRequiresOneStableMobilityIdAndStoresNoDerivedMovementState()
    {
        var origin = ContentTestData.Origin("content.element.mobility");
        var element = new ContentCombatElement(
            "axis-element",
            "axis",
            "axis-formation",
            "land.organization.battalion",
            Cna1979Movement.MotorizedMobilityId,
            20,
            ContentPlacementMode.Independent,
            origin);

        Assert.Equal(Cna1979Movement.MotorizedMobilityId, element.MobilityId);
        Assert.Throws<ArgumentException>(() => new ContentCombatElement(
            "axis-element",
            "axis",
            "axis-formation",
            "land.organization.battalion",
            string.Empty,
            20,
            ContentPlacementMode.Independent,
            origin));
        Assert.Equal(
            [
                "BaseCapabilityPointAllowance",
                "BreakdownVehicleCohort",
                "ElementId",
                "MobilityId",
                "OrganizationId",
                "Origin",
                "ParentFormationId",
                "PlacementMode",
                "SideId",
            ],
            typeof(ContentCombatElement)
                .GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CnaCompatibilityRejectsMobilityOutsideTheRulesOwnedClosedSet()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var element = baseline.Elements[0];
        var unknown = new ContentCombatElement(
            element.ElementId,
            element.SideId,
            element.ParentFormationId,
            element.OrganizationId,
            "land.mobility.unknown",
            element.BaseCapabilityPointAllowance,
            element.PlacementMode,
            element.Origin);

        var result = Cna1979ContentCompatibilityValidator.Validate(
            ContentTestData.Copy(baseline, elements: [unknown]));
        var exception = Assert.Throws<InvalidContentPackException>(() =>
            ContentPackArtifact.Create(ContentTestData.Copy(baseline, elements: [unknown])));

        Assert.Contains(
            result.Issues,
            issue => issue.Code == "vocabulary.unknown-id"
                && issue.Path == $"/elements/{element.ElementId}/mobilityId");
        Assert.Contains(
            exception.Issues,
            issue => issue.Code == "vocabulary.unknown-id"
                && issue.Path == $"/elements/{element.ElementId}/mobilityId");
    }

    [Fact]
    public void CanonicalBytesCarryMobilityAndStrictReadbackRejectsLegacyElements()
    {
        var pack = ContentTestData.CreateMinimalPack();
        var canonical = Encoding.UTF8.GetString(ContentPackSerializer.SerializeCanonical(pack));
        var mobilityProperty =
            $"\"mobilityId\":\"{Cna1979Movement.MotorizedMobilityId}\",";

        Assert.Contains(mobilityProperty, canonical, StringComparison.Ordinal);

        var legacy = canonical.Replace(mobilityProperty, string.Empty, StringComparison.Ordinal);
        var result = ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(legacy));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Definition);
        Assert.Equal("content.missing-property", result.ErrorCode);
    }

    [Fact]
    public void MobilityIsAnAuthoritativeContentFactThatChangesCanonicalIdentity()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var element = baseline.Elements[0];
        var changedElement = new ContentCombatElement(
            element.ElementId,
            element.SideId,
            element.ParentFormationId,
            element.OrganizationId,
            Cna1979Movement.NonMotorizedMobilityId,
            element.BaseCapabilityPointAllowance,
            element.PlacementMode,
            element.Origin);
        var changed = ContentTestData.Copy(baseline, elements: [changedElement]);

        Assert.NotEqual(
            ContentPackArtifact.Create(baseline).Identity.Hash,
            ContentPackArtifact.Create(changed).Identity.Hash);
    }

    [Fact]
    public void SyntheticCatalogAssignsBothSupportedMobilityClassesExplicitly()
    {
        var mobilityByElement = Cna1979SyntheticContentCatalog.Artifact.Definition.Elements
            .ToDictionary(element => element.ElementId, element => element.MobilityId);

        Assert.Equal(Cna1979Movement.MotorizedMobilityId, mobilityByElement["axis-element-a"]);
        Assert.Equal(Cna1979Movement.NonMotorizedMobilityId, mobilityByElement["axis-element-b"]);
        Assert.Equal(
            Cna1979Movement.MotorizedMobilityId,
            mobilityByElement["commonwealth-element-a"]);
        Assert.Equal(
            Cna1979Movement.NonMotorizedMobilityId,
            mobilityByElement["commonwealth-element-b"]);
    }
}
