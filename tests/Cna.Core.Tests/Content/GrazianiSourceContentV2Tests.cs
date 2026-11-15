using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Tests.Shared;

namespace Cna.Core.Tests.Content;

public sealed class GrazianiSourceContentV2Tests
{
    [Fact]
    public void PolishPrintedStackingPreservesHeadquartersAndCavalryDistinctions()
    {
        var facts = Cna1979GrazianiCounterFacts.PolishBrigadeStacking;
        Assert.Equal(Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds.Order(StringComparer.Ordinal),
            facts.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        var expected = new Dictionary<string, int>
        {
            ["polish-brigade-hq"] = 3,
            ["polish-1-infantry"] = 1,
            ["polish-2-infantry"] = 1,
            ["polish-3-infantry"] = 1,
            ["polish-kopanski-machine-gun"] = 0,
            ["polish-1-cavalry"] = 1,
            ["polish-1-antitank"] = 0,
            ["polish-1-artillery"] = 1,
        };
        Assert.All(facts, fact =>
        {
            Assert.Equal(expected[fact.ElementId], fact.PrintedValue);
            Assert.Equal("commonwealth", fact.SideId);
            Assert.Equal(ContentOriginKind.SourceDerived, fact.Origin.Kind);
            Assert.Contains(fact.Origin.References, source => source.SourceId == Cna1979GrazianiSourceFacts.AirLogisticsSourceId
                && source.Locator == "counter-section-5-p42");
        });
        var hq = Assert.Single(facts, fact => fact.ElementId == "polish-brigade-hq");
        Assert.True(hq.PrintedValueIsFormationConditional);
        Assert.Equal(0, hq.UnattachedHeadquartersValue);
        Assert.All(facts.Where(fact => fact != hq), fact =>
        {
            Assert.False(fact.PrintedValueIsFormationConditional);
            Assert.Null(fact.UnattachedHeadquartersValue);
        });
        var cavalry = Assert.Single(facts, fact => fact.ElementId == "polish-1-cavalry");
        Assert.Equal("ss", cavalry.CharacteristicsCode);
        // Land 32.83's ss anti-air removal applies to Italian forces, not this Commonwealth unit.
        Assert.Equal("commonwealth", cavalry.SideId);
    }

    [Fact]
    public void ProductionCairoExcerptRetainsRealPendingArrivalsAndStableBytes()
    {
        var first = Cna1979GrazianiCairoSourceExcerpt.Build();
        var second = Cna1979GrazianiCairoSourceExcerpt.Build();
        Assert.Equal(first.Identity, second.Identity);
        Assert.Equal(first.CanonicalBytes, second.CanonicalBytes);
        var pack = first.Definition.LegacyDefinition;
        Assert.Empty(pack.Edges);
        Assert.All(pack.Elements, element =>
            Assert.Equal(Cna1979SourceContentVocabulary.UnresolvedMobilityId, element.MobilityId));
        var scenario = Assert.Single(pack.Scenarios);
        Assert.Equal(Cna1979GrazianiCairoSourceExcerpt.ScenarioId, scenario.ScenarioId);
        var polish = Assert.Single(scenario.ScheduledArrivals,
            value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        Assert.Equal(new ContentScenarioBoundary(1, 3), polish.Arrival);
        Assert.Equal(Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds, polish.ElementIds);
    }

    [Fact]
    public void PrintedAdditionalArrivalAllowancesKeepDisputedTimingSeparate()
    {
        var facts = Cna1979GrazianiCounterFacts.AdditionalArrivalCandidates;
        Assert.Equal(9, facts.Count);
        Assert.Equal(20, facts.Single(value => value.ElementId == Cna1979GrazianiSourceFacts.NzFieldArtilleryId)
            .CapabilityPointAllowance);
        Assert.Equal(25, facts.Single(value => value.ElementId == Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentId)
            .CapabilityPointAllowance);
        Assert.Equal(30, facts.Single(value => value.ElementId == "6-nz-brigade-hq").CapabilityPointAllowance);
        Assert.All(facts, value => Assert.Equal(ContentOriginKind.SourceDerived, value.Origin.Kind));
    }

    [Fact]
    public void InitialFormationAllowancesPreserveUnresolvedTrainingAndPrintedExceptions()
    {
        var indian = Cna1979GrazianiCounterFacts.FourthIndianInitialCandidates;
        Assert.Equal(Cna1979GrazianiSourceFacts.FourthIndianDivisionCombatCandidates.Select(value => value.ElementId)
            .Order(StringComparer.Ordinal), indian.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(indian, value => value.ElementId == "central-india-horse");
        var armored = Cna1979GrazianiCounterFacts.SeventhArmoredInitialCandidates;
        Assert.Equal(Cna1979GrazianiSourceFacts.SeventhArmoredDivisionCombatCandidates.Select(value => value.ElementId)
            .Order(StringComparer.Ordinal), armored.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.True(armored.Single(value => value.ElementId == "7-armored-brigade-hq").RetainsAllowanceRegardlessOfAssignedToe);
        Assert.Equal(20, armored.Single(value => value.ElementId == "7-armored-brigade-hq").CapabilityPointAllowance);
        Assert.False(armored.Single(value => value.ElementId == "8-hussars").RetainsAllowanceRegardlessOfAssignedToe);
        var all = Cna1979GrazianiCounterFacts.CairoBrigadeCandidates
            .Concat(Cna1979GrazianiCounterFacts.AdditionalArrivalCandidates)
            .Concat(indian).Concat(armored).Select(value => value.ElementId).ToArray();
        Assert.Equal(42, all.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(42, all.Length);
    }

    [Fact]
    public void RealCairoSourceExcerptRoundTripsWithoutInventingMovementRules()
    {
        var artifact = GrazianiSourceExcerpt.Build();
        Assert.DoesNotContain(artifact.Definition.LegacyDefinition.SourceIndex,
            value => value.Kind == ContentSourceKind.RepositorySynthetic);
        Assert.Equal(16, Cna1979GrazianiCounterFacts.CairoBrigadeCandidates.Count);
        Assert.Equal(45, Cna1979GrazianiCounterFacts.CairoBrigadeCandidates.Single(
            value => value.ElementId == "polish-1-cavalry").CapabilityPointAllowance);
        Assert.Equal(8, Cna1979GrazianiCounterFacts.CairoBrigadeCandidates.Single(
            value => value.ElementId == "polish-kopanski-machine-gun").CapabilityPointAllowance);
        foreach (var fact in Cna1979GrazianiCounterFacts.CairoBrigadeCandidates)
        {
            var element = artifact.Definition.LegacyDefinition.Elements.Single(value => value.ElementId == fact.ElementId);
            Assert.Equal(fact.CapabilityPointAllowance, element.BaseCapabilityPointAllowance);
            Assert.Equal(fact.Origin, element.Origin);
        }
        Assert.Contains(ContentPackValidator.Validate(artifact.Definition.LegacyDefinition).Issues,
            value => value.Code == "topology.disconnected");
        var cairo = artifact.Definition.LegacyDefinition.Locations;
        Assert.Equal(Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds, cairo.Select(value => value.LocationId));
        Assert.All(cairo, value =>
        {
            Assert.Equal(Cna1979SourceContentVocabulary.MajorCityTerrainId, value.TerrainId);
            Assert.False(Cna1979Movement.LookupTerrain(value.TerrainId, Cna1979Movement.NonMotorizedMobilityId).IsSupported);
        });
        Assert.Throws<JsonException>(() => ContentScheduledCombatPackSerializer.Deserialize(artifact.CanonicalBytes));
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackArtifact.Create(artifact.Definition));
        var reopened = ContentSourceScheduledCombatPackArtifact.Read(artifact.CanonicalBytes, artifact.Identity);
        var world = CampaignWorldV8Factory.CreateInitial(reopened, GrazianiSourceExcerpt.ScenarioId,
            new(CapabilityPointAmount.Zero, 0, CampaignElementReserveStatus.None));
        Assert.Equal(4, world.World.Elements.Count);
        Assert.All(world.World.Elements, element => Assert.Equal("e1829", element.CurrentLocationId));
        Assert.Equal(2, world.PendingArrivals.Count);
        var arrival = Assert.Single(world.PendingArrivals, value => value.GroupId ==
            Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId);
        Assert.Equal(Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.Arrival, arrival.Arrival);
        Assert.Equal(Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.ElementIds, arrival.ElementIds);
        Assert.Equal(Cna1979GrazianiSourceFacts.CairoAreaId, arrival.EntryAreaId);
        Assert.Equal(Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.Trucks, arrival.Trucks);
        Assert.DoesNotContain(world.World.Elements, element => arrival.ElementIds.Contains(element.ElementId));
        var polish = Assert.Single(world.PendingArrivals, value => value.GroupId ==
            Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        Assert.Equal(new ContentScenarioBoundary(1, 3), polish.Arrival);
        Assert.Null(polish.Trucks);
        Assert.DoesNotContain(world.World.Elements, element => polish.ElementIds.Contains(element.ElementId));
    }

    [Fact]
    public void SourceIdentityCannotBeReopenedWithChangedBytesOrHash()
    {
        var artifact = GrazianiSourceExcerpt.Build();
        var bytes = artifact.CanonicalBytes;
        bytes[^1] ^= 1;
        Assert.Throws<JsonException>(() => ContentSourceScheduledCombatPackArtifact.Read(bytes, artifact.Identity));
        var changed = artifact.Identity with { Hash = "sha256:" + new string('0', 64) };
        Assert.Throws<InvalidOperationException>(() => ContentSourceScheduledCombatPackArtifact.Read(artifact.CanonicalBytes, changed));
    }

    [Fact]
    public void SourceFormatRejectsSyntheticSourceRecords()
    {
        var definition = GrazianiSourceExcerpt.Build().Definition;
        var pack = definition.LegacyDefinition;
        var mixed = new ContentPackDefinition(pack.SchemaVersion, pack.FormatId, pack.PackId, pack.RulesetId,
            pack.Capabilities, pack.SourceIndex.Append(new ContentSourceIndexEntry("test-synthetic-source",
                ContentSourceKind.RepositorySynthetic)), pack.Locations, pack.WeatherAreaAssignments, pack.Edges,
            pack.Formations, pack.Elements, pack.Scenarios);
        Assert.Throws<InvalidContentPackException>(() => ContentSourceScheduledCombatPackArtifact.Create(
            new ContentPackV5Definition(mixed, definition.ElementCombatFacts, definition.InitialPlacementCombatFacts)));
    }

    [Fact]
    public void IncompleteSourceTopologyStillRejectsUnknownEdgeEndpoints()
    {
        var definition = GrazianiSourceExcerpt.Build().Definition;
        var pack = definition.LegacyDefinition;
        var edge = new ContentHexEdge("e1829", "e9999", [], pack.Locations[0].Origin);
        var invalid = new ContentPackDefinition(pack.SchemaVersion, pack.FormatId, pack.PackId, pack.RulesetId,
            pack.Capabilities, pack.SourceIndex, pack.Locations, pack.WeatherAreaAssignments, [edge],
            pack.Formations, pack.Elements, pack.Scenarios);
        var error = Assert.Throws<InvalidContentPackException>(() => ContentSourceScheduledCombatPackArtifact.Create(
            new ContentPackV5Definition(invalid, definition.ElementCombatFacts, definition.InitialPlacementCombatFacts)));
        Assert.Contains(error.Issues, value => value.Code == "content.unknown-reference");
    }

    [Fact]
    public void NormalCreationFacadeReplaysSourceWorldAndPendingGroup()
    {
        var artifact = GrazianiSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignSourceCreation.RequestVersion, "graziani-source-excerpt-test",
            Cna1979Ruleset.Manifest.Hash, 1940, CampaignSourceCreation.SetupId,
            CampaignSourceCreation.CalculateSetupHash(artifact.Identity, GrazianiSourceExcerpt.ScenarioId, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, GrazianiSourceExcerpt.ScenarioId);
        var created = CampaignHostAdapter.Create(request, artifact, initialization);
        var replayed = CampaignSourceCreation.Replay(request, created.CreationEventBytes, created.InitialSnapshotBytes);
        Assert.Equal(created.Session.InitialWorld, replayed.Session.InitialWorld);
        Assert.Equal(4, replayed.Session.InitialWorld.World.Elements.Count);
        Assert.Equal(new[] { Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId,
                Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId }.Order(StringComparer.Ordinal),
            replayed.Session.PendingArrivalGroupIds);
        var otherSeed = new CampaignCreationRequest(request.ContractVersion, request.CampaignId, request.RulesetHash,
            request.Seed + 1, request.SetupId, request.SetupHash, request.ContentPackId, request.ContentHash, request.ScenarioId);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceCreation.Replay(otherSeed,
            created.CreationEventBytes, created.InitialSnapshotBytes));
        var corruptSnapshot = created.InitialSnapshotBytes.ToArray();
        corruptSnapshot[^1] ^= 1;
        Assert.Throws<InvalidOperationException>(() => CampaignSourceCreation.Replay(request,
            created.CreationEventBytes, corruptSnapshot));
    }
}
