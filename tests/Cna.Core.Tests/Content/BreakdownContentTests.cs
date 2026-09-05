using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class BreakdownContentTests
{
    [Fact]
    public void CertifiedTruckFixtureRoundTripsWithEmptyNoncombatComponents()
    {
        var bytes = BreakdownContentFixture.Bytes();
        var parsed = ContentPackV6Serializer.Deserialize(bytes);
        Assert.True(parsed.IsSuccess, parsed.Message);
        var artifact = ContentPackV6Artifact.Create(parsed.Definition!);
        Assert.Equal(bytes, artifact.GetCanonicalBytes());
        Assert.Equal(6, artifact.Identity.SchemaVersion);
        Assert.Equal("sandtable.content-json.v5", artifact.Identity.FormatId);
        Assert.Equal("sandtable.capability.breakdown-truck-battalion.v1", artifact.Definition.CapabilityProfileId);
        var trucks = artifact.Definition.ElementCombatFacts.Where(facts =>
            facts.CombatClassificationId == Cna1979Combat.TruckConvoyClassificationId).ToArray();
        Assert.Equal(2, trucks.Length);
        Assert.All(trucks, facts => Assert.Empty(facts.Components));
        Assert.All(artifact.Definition.InitialPlacementCombatFacts.Where(facts =>
            trucks.Any(truck => truck.ElementId == facts.ElementId)), facts => Assert.Empty(facts.InitialComponentToes));
    }

    [Theory]
    [InlineData("unknown-profile")]
    [InlineData("missing-profile")]
    [InlineData("unknown-capability")]
    [InlineData("missing-capability")]
    [InlineData("legacy-version")]
    [InlineData("mixed-format")]
    [InlineData("unknown-root")]
    [InlineData("unknown-cargo")]
    [InlineData("truck-components")]
    [InlineData("truck-classification")]
    [InlineData("duplicate-cohort")]
    [InlineData("two-cohorts-per-side")]
    [InlineData("unsupported-vehicle")]
    [InlineData("unsupported-profile")]
    [InlineData("zero-points")]
    [InlineData("truck-nonmotorized")]
    [InlineData("combat-motorized")]
    [InlineData("regimental-shell")]
    [InlineData("parent-chain")]
    [InlineData("attachment")]
    [InlineData("combat-stack")]
    [InlineData("hidden-invalid-scenario")]
    [InlineData("missing-weather")]
    [InlineData("unknown-component")]
    [InlineData("unknown-origin")]
    [InlineData("nonsynthetic-scenario")]
    [InlineData("missing-toe")]
    [InlineData("over-maximum-toe")]
    public void RejectsUncertifiedOrMixedContent(string mutation)
    {
        var root = JsonNode.Parse(BreakdownContentFixture.Bytes())!.AsObject();
        var elements = root["elements"]!.AsArray();
        var infantry = elements[0]!;
        var truck = elements[1]!;
        var scenarios = root["scenarios"]!.AsArray();
        var placements = scenarios[0]!["initialPlacements"]!.AsArray();
        switch (mutation)
        {
            case "unknown-profile": root["capabilityProfileId"] = "unsupported.profile"; break;
            case "missing-profile": root.Remove("capabilityProfileId"); break;
            case "unknown-capability": root["capabilities"]!.AsArray().Add("land.cargo"); break;
            case "missing-capability": root["capabilities"]!.AsArray().RemoveAt(0); break;
            case "legacy-version": root["schemaVersion"] = 5; break;
            case "mixed-format": root["formatId"] = "sandtable.content-json.v4"; break;
            case "unknown-root": root["loadCapacity"] = 1; break;
            case "unknown-cargo": truck["cargo"] = new JsonArray(); break;
            case "truck-components": truck["components"]!.AsArray().Add(infantry["components"]![0]!.DeepClone()); break;
            case "truck-classification": truck["combatClassificationId"] = Cna1979Combat.CombatUnitClassificationId; break;
            case "duplicate-cohort": elements[3]!["breakdownVehicleCohort"]!["cohortId"] = truck["breakdownVehicleCohort"]!["cohortId"]!.DeepClone(); break;
            case "two-cohorts-per-side": elements[3]!["sideId"] = "axis"; root["formations"]![3]!["sideId"] = "axis"; break;
            case "unsupported-vehicle": truck["breakdownVehicleCohort"]!["vehicleTypeId"] = "land.vehicle.unknown"; break;
            case "unsupported-profile": truck["breakdownVehicleCohort"]!["profileId"] = "land.profile.unknown"; break;
            case "zero-points": truck["breakdownVehicleCohort"]!["workingPointCount"] = 0; break;
            case "truck-nonmotorized": truck["mobilityId"] = Cna1979Movement.NonMotorizedMobilityId; break;
            case "combat-motorized": infantry["mobilityId"] = Cna1979Movement.MotorizedMobilityId; break;
            case "regimental-shell": root["formations"]![0]!["organizationId"] = "land.organization.regiment"; break;
            case "parent-chain": root["formations"]![0]!["parentFormationId"] = root["formations"]![1]!["formationId"]!.DeepClone(); break;
            case "attachment": infantry["placementMode"] = "attachment-only"; break;
            case "combat-stack":
            case "hidden-invalid-scenario":
                elements[2]!["sideId"] = "axis";
                root["formations"]![2]!["sideId"] = "axis";
                if (mutation == "hidden-invalid-scenario")
                {
                    scenarios.Add(scenarios[0]!.DeepClone());
                    scenarios[1]!["scenarioId"] = "other-scenario";
                    placements = scenarios[1]!["initialPlacements"]!.AsArray();
                }
                placements[2]!["locationId"] = placements[0]!["locationId"]!.DeepClone();
                break;
            case "missing-weather": root["weatherAreaAssignments"]!.AsArray().RemoveAt(0); break;
            case "unknown-component": infantry["components"]![0]!["componentClassId"] = "land.component.unknown"; break;
            case "unknown-origin": infantry["combatOrigin"]!["references"]![0]!["sourceId"] = "unknown-source"; break;
            case "nonsynthetic-scenario": scenarios[0]!["origin"]!["kind"] = "source-derived"; break;
            case "missing-toe": placements[0]!["initialComponentToes"] = new JsonArray(); break;
            case "over-maximum-toe": placements[0]!["initialComponentToes"]![0]!["currentToe"] = 6; break;
            default: throw new InvalidOperationException(mutation);
        }

        Assert.False(ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())).IsSuccess);
    }

    [Fact]
    public void RejectsNoncanonicalAndDuplicateJson()
    {
        var json = Encoding.UTF8.GetString(BreakdownContentFixture.Bytes());
        Assert.False(ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(json + "\n")).IsSuccess);
        Assert.False(ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(
            "\"schemaVersion\":6", "\"schemaVersion\":6,\"schemaVersion\":6", StringComparison.Ordinal))).IsSuccess);
        Assert.False(ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(
            "\"workingPointCount\":12", "\"workingPointCount\":12.0", StringComparison.Ordinal))).IsSuccess);
    }

    [Theory]
    [InlineData("profile", "BRK-CERT-001")]
    [InlineData("missing-profile", "BRK-CERT-001")]
    [InlineData("truck-component", "BRK-CERT-002")]
    [InlineData("formation", "BRK-CERT-003")]
    [InlineData("origin", "BRK-CERT-004")]
    [InlineData("cohort-origin", "BRK-CERT-004")]
    [InlineData("formation-origin", "BRK-CERT-004")]
    public void ReportsTrustedStaticCertificationClass(string mutation, string expected)
    {
        var root = JsonNode.Parse(BreakdownContentFixture.Bytes())!.AsObject();
        switch (mutation)
        {
            case "profile": root["capabilityProfileId"] = "unknown-profile"; break;
            case "missing-profile": root.Remove("capabilityProfileId"); break;
            case "truck-component": root["elements"]![1]!["components"]!.AsArray().Add(root["elements"]![0]!["components"]![0]!.DeepClone()); break;
            case "formation": root["formations"]![0]!["organizationId"] = "land.organization.regiment"; break;
            case "origin": root["elements"]![0]!["combatOrigin"]!["references"]![0]!["sourceId"] = "missing-source"; break;
            case "cohort-origin": root["elements"]![1]!["breakdownVehicleCohort"]!["origin"]!["references"]![0]!["sourceId"] = "missing-source"; break;
            case "formation-origin": root["formations"]![0]!["origin"]!["references"]![0]!["sourceId"] = "missing-source"; break;
            default: throw new InvalidOperationException(mutation);
        }

        var result = ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.ErrorCode);
    }

    [Fact]
    public void V5BoundaryRetainsItsNonemptyComponentGrammar()
    {
        var origin = Cna1979SyntheticContentCatalog.ArtifactV5.Definition.ElementCombatFacts[0].Origin;
        Assert.Throws<ArgumentException>(() => new ContentElementCombatFacts(
            "truck", Cna1979Combat.TruckConvoyClassificationId, [], origin));
        Assert.False(ContentPackV5Serializer.Deserialize(BreakdownContentFixture.Bytes()).IsSuccess);
        Assert.False(ContentPackV6Serializer.Deserialize(Cna1979SyntheticContentCatalog.ArtifactV5.GetCanonicalBytes()).IsSuccess);
        var successor = BreakdownContentFixture.Artifact().Definition;
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Artifact.Create(new ContentPackV5Definition(
            successor.LegacyDefinition, successor.ElementCombatFacts, successor.InitialPlacementCombatFacts)));
    }
}

internal static class BreakdownContentFixture
{
    internal static byte[] Bytes() => File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Content", "Fixtures", "rules-lab.content.breakdown-truck.v1.golden.json"));

    internal static ContentPackV6Artifact Artifact() => ContentPackV6Artifact.Create(
        ContentPackV6Serializer.Deserialize(Bytes()).Definition!);
}
