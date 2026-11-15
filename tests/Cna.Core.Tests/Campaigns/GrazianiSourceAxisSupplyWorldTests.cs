using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAxisSupplyWorldTests
{
    [Fact]
    public void RealAxisFixedSuppliesRetainSourceAmountsAndUnknownDernaAmmo()
    {
        var world = Create();
        Assert.Equal(16, world.Units.Count);
        Assert.Equal(3, world.Units.Count(value => value.LocationId == "c4807" && value.Ammo == 15 && value.Fuel == 50));
        Assert.Equal(2, world.Units.Count(value => value.LocationId == "c4321" && value.Ammo == 10 && value.Fuel == 40));
        Assert.Single(world.Units, value => value.LocationId == "b4827" && value.Ammo == 10 && value.Fuel == 10);
        Assert.Single(world.Units, value => value.LocationId == "b5925" && value.Ammo is null && value.Fuel == 10);
        Assert.Equal(8, world.Units.Count(value => value.LocationId == "box.tripoli" && value.Ammo == 5 && value.Fuel == 60));
        Assert.Single(world.Units, value => value.LocationId == "c0716" && value.Ammo == 20 && value.Fuel == 6);
        Assert.Equal(["axis-dump-1", "axis-dump-2"], world.PendingDumpIds);
        using var checkpoint = JsonDocument.Parse(world.SerializeCanonical());
        Assert.False(checkpoint.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal("provisional-whole-group-optional", checkpoint.RootElement.GetProperty("dummyOmissionPolicy").GetString());
        Assert.Equal(4, checkpoint.RootElement.GetProperty("curatedDumpSites").GetArrayLength());
        Assert.Equal(7, checkpoint.RootElement.GetProperty("commonwealthMapCPositions").GetArrayLength());
        Assert.Equal("scenario-b4827-map-a4827-unresolved",
            checkpoint.RootElement.GetProperty("benghaziCoordinateStatus").GetString());
        Assert.Equal(JsonValueKind.Null, checkpoint.RootElement.GetProperty("units").EnumerateArray()
            .Single(value => value.GetProperty("locationId").GetString() == "b5925").GetProperty("ammo").ValueKind);
    }

    [Fact]
    public void AxisPlayerPlacesBothRealDumpsWithConservationAndCapacityEnforced()
    {
        var initial = Create();
        Assert.Equal(["c4407", "c4507", "c4607", "c4707"], initial.ValidDestinations("axis-dump-1"));
        Assert.Throws<InvalidOperationException>(() => initial.Place("axis-dump-1", "c4807"));
        Assert.Throws<InvalidOperationException>(() => initial.Place("axis-dump-3", "c4507"));
        var first = initial.Place("axis-dump-1", "c4707");
        Assert.Equal(16, initial.Units.Count);
        Assert.Equal(19, first.Units.Count);
        Assert.Equal(3, first.Units.Count(value => value.LocationId == "c4707" && value.Ammo == 40 && value.Fuel == 50));
        Assert.Equal(["c4407", "c4507", "c4607"], first.ValidDestinations("axis-dump-2"));
        Assert.Throws<InvalidOperationException>(() => first.Place("axis-dump-2", "c4707"));
        Assert.Throws<InvalidOperationException>(() => first.Place("axis-dump-1", "c4507"));
        var completed = first.Place("axis-dump-2", "c4507");
        Assert.Equal(22, completed.Units.Count);
        Assert.Empty(completed.PendingDumpIds);
        Assert.Equal(completed.Hash(), Create().Place("axis-dump-1", "c4707").Place("axis-dump-2", "c4507").Hash());
        Assert.NotEqual(completed.Hash(), initial.Hash());
        Assert.Equal(1036, completed.Units.Sum(value => value.Fuel));
        Assert.Equal(375, completed.Units.Sum(value => value.Ammo ?? 0));
    }

    [Fact]
    public void OptionalDummyGroupsRemainPrivateAndRespectRealCounterCapacity()
    {
        var initial = Create();
        var withDump = initial.Place("axis-dump-1", "c4707");
        Assert.DoesNotContain("c4707", withDump.ValidDummyDestinations("axis-dummy-1"));
        var placed = withDump.DecideDummy("axis-dummy-1", "c4607");
        Assert.Equal(3, placed.Units.Count(unit => unit.IsDummy && unit.LocationId == "c4607"));
        Assert.DoesNotContain("c4607", placed.ValidDestinations("axis-dump-2"));
        Assert.Throws<InvalidOperationException>(() => placed.DecideDummy("axis-dummy-1", null));
        var skipped = placed.DecideDummy("axis-dummy-2", null);
        Assert.Empty(skipped.PendingDummyIds);
        Assert.Equal(3, skipped.Units.Count(unit => unit.IsDummy));
        using var checkpoint = JsonDocument.Parse(skipped.SerializeCanonical());
        Assert.Equal("unresolved", checkpoint.RootElement.GetProperty("generalDummyAllowanceStatus").GetString());
        Assert.Equal(0, checkpoint.RootElement.GetProperty("scenarioDummyGroupsUnresolved").GetInt32());
    }

    private static CampaignSourceAxisSupplyWorld Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-axis-supply", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignSourceAxisSupplyWorld.Create(CampaignPregameSourceState.Create(request, artifact, init));
    }
}
