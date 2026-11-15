using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceCommonwealthSupplyWorldTests
{
    [Fact]
    public void MatruhAndSidiSuppliesAreRealAndOtherChoicesStayPending()
    {
        var world = Create();
        Assert.Equal(6, world.Units.Count);
        Assert.Equal(5, world.Units.Count(value => value.LocationId == "d3714" && value.Ammo == 40 && value.Fuel == 60));
        Assert.Single(world.Units, value => value.LocationId == "c4131" && value.Ammo == 40 && value.Fuel == 60);
        Assert.Equal(7, world.PendingPlacementIds.Count);
        Assert.Equal(["d3611", "d3612"], world.ValidDestinations("cw-dump-1"));
        Assert.Throws<InvalidOperationException>(() => world.Place("cw-dump-1", "d3714"));
        using var snapshot = JsonDocument.Parse(world.SerializeCanonical());
        Assert.False(snapshot.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal("provisional-whole-group-optional", snapshot.RootElement.GetProperty("dummyOmissionPolicy").GetString());
        Assert.Equal(2, snapshot.RootElement.GetProperty("alexandriaSites").GetArrayLength());
        Assert.Equal(5, snapshot.RootElement.GetProperty("cairoSites").GetArrayLength());
        Assert.Equal(1, snapshot.RootElement.GetProperty("scenarioDummyGroupsUnresolved").GetInt32());
    }

    [Fact]
    public void OptionalDummyGroupCannotOverfillTheRealDumpHex()
    {
        var initial = Create();
        var withDump = initial.Place("cw-dump-1", "d3612");
        Assert.Equal(["d3611"], withDump.ValidDummyDestinations());
        Assert.Throws<InvalidOperationException>(() => withDump.DecideDummy("d3612"));
        var placed = withDump.DecideDummy("d3611");
        Assert.Equal(3, placed.Units.Count(unit => unit.IsDummy && unit.LocationId == "d3611"));
        Assert.Throws<InvalidOperationException>(() => placed.DecideDummy(null));
        var skipped = initial.DecideDummy(null);
        Assert.DoesNotContain(skipped.Units, unit => unit.IsDummy);
        using var snapshot = JsonDocument.Parse(skipped.SerializeCanonical());
        Assert.Equal(0, snapshot.RootElement.GetProperty("scenarioDummyGroupsUnresolved").GetInt32());
        Assert.Equal("unresolved", snapshot.RootElement.GetProperty("generalDummyAllowanceStatus").GetString());
        Assert.Equal("neighbor-and-summary-inference", snapshot.RootElement.GetProperty("cairoE1931Status").GetString());
    }

    [Fact]
    public void CityCountersCanBeSplitAndDumpRemainsTogether()
    {
        var initial = Create();
        var world = initial.Place("cw-dump-1", "d3612")
            .Place("cw-alexandria-1", "e3613")
            .Place("cw-alexandria-2", "e3714")
            .Place("cw-alexandria-3", "e3613")
            .Place("cw-cairo-1", "e1730")
            .Place("cw-cairo-2", "e1829")
            .Place("cw-cairo-3", "e1931");
        Assert.Equal(6, initial.Units.Count);
        Assert.Equal(15, world.Units.Count);
        Assert.Equal(3, world.Units.Count(value => value.LocationId == "d3612" && value.Ammo == 40 && value.Fuel == 30));
        Assert.Equal(2, world.Units.Count(value => value.LocationId == "e3613"));
        Assert.Single(world.Units, value => value.LocationId == "e3714");
        Assert.Empty(world.PendingPlacementIds);
        Assert.Equal(600, world.Units.Sum(value => value.Ammo));
        Assert.Equal(810, world.Units.Sum(value => value.Fuel));
        Assert.Throws<InvalidOperationException>(() => world.Place("cw-cairo-1", "e1829"));
        Assert.Throws<InvalidOperationException>(() => initial.Place("cw-cairo-1", "e3613"));
        Assert.NotEqual(initial.Hash(), world.Hash());
    }

    private static CampaignSourceCommonwealthSupplyWorld Create()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "real-graziani-commonwealth-supply", Cna1979Ruleset.Manifest.Hash, 0,
            CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, init),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        return CampaignSourceCommonwealthSupplyWorld.Create(CampaignPregameSourceState.Create(request, artifact, init));
    }
}
