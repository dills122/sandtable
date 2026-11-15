using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class ContentScheduledPackSerializerTests
{
    private static ContentPackDefinition Candidate(int turn = 2, string side = "commonwealth")
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var original = Assert.Single(baseline.Scenarios);
        var testOrigin = ContentTestData.Origin("fixture.unit.nz");
        var source = Cna1979GrazianiSourceFacts.NzFieldArtilleryArrival.Origin;
        // The schedule is source-linked; the remaining force fields are test placeholders,
        // not claims about the historical unit's mobility or combat characteristics.
        var formation = new ContentFormation("nz-formation", "commonwealth", null,
            "land.organization.regiment", testOrigin);
        var element = new ContentCombatElement(Cna1979GrazianiSourceFacts.NzFieldArtilleryId, "commonwealth", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, testOrigin);
        var arrival = new ContentScheduledArrival(Cna1979GrazianiSourceFacts.NzFieldArtilleryArrival.GroupId, side, new(turn, 3),
            [element.ElementId], Cna1979GrazianiSourceFacts.CairoAreaId, source);
        // Placeholder map membership for serialization tests only; it is not the historical map.
        var area = new ContentLocationArea("cairo", ["west", "east"], testOrigin);
        var scenario = new ContentScenario(original.ScenarioId, original.Start, new(6, 3),
            original.InitialPlacements, [arrival], [area], original.Origin);
        return ContentTestData.Copy(baseline, sourceIndex: [.. baseline.SourceIndex,
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, ContentSourceKind.PublishedPrimary)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, element], scenarios: [scenario]);
    }

    [Fact]
    public void SourceLinkedGrazianiScheduleRoundTripsAndBindsContentIdentity()
    {
        var candidate = Candidate();
        Assert.True(ContentScheduledPackSerializer.Validate(candidate).IsValid);
        Assert.False(ContentPackValidator.Validate(candidate).IsValid);
        var bytes = ContentScheduledPackSerializer.SerializeCanonical(candidate);
        var restored = ContentScheduledPackSerializer.Deserialize(bytes);
        var arrival = Assert.Single(Assert.Single(restored.Scenarios).ScheduledArrivals);
        Assert.Equal("nz-6-field-artillery", Assert.Single(arrival.ElementIds));
        Assert.Equal(new ContentScenarioBoundary(2, 3), arrival.Arrival);
        Assert.Equal("cairo", arrival.EntryAreaId);
        Assert.Equal(ContentOriginKind.SourceDerived, arrival.Origin.Kind);
        Assert.Contains(arrival.Origin.References, value => value.SourceId == Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId && value.Locator == "4.43a");
        Assert.Equal(bytes, ContentScheduledPackSerializer.SerializeCanonical(restored));
        Assert.Equal(ContentScheduledPackSerializer.CalculateHash(candidate), ContentScheduledPackSerializer.CalculateHash(restored));
        Assert.NotEqual(ContentScheduledPackSerializer.CalculateHash(candidate), ContentScheduledPackSerializer.CalculateHash(Candidate(3)));
    }

    [Fact]
    public void ScheduledFormatRejectsWrongOwnershipMissingReferencesAndNoncanonicalBytes()
    {
        Assert.Contains(ContentScheduledPackSerializer.Validate(Candidate(side: "axis")).Issues,
            issue => issue.Code == "arrival.element-owner-or-placement");
        Assert.Contains(ContentScheduledPackSerializer.Validate(Candidate(7)).Issues,
            issue => issue.Code == "arrival.outside-scenario");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledPackSerializer.SerializeCanonical(Candidate(side: "axis")));
        var canonical = ContentScheduledPackSerializer.SerializeCanonical(Candidate());
        Assert.Throws<JsonException>(() => ContentScheduledPackSerializer.Deserialize([.. canonical, (byte)' ']));
        Assert.Throws<JsonException>(() => ContentScheduledPackSerializer.Deserialize(Encoding.UTF8.GetBytes("{}")));
        Assert.Throws<JsonException>(() => ContentScheduledPackSerializer.Deserialize([]));
        var candidate = Candidate();
        var withoutSource = ContentTestData.Copy(candidate, sourceIndex: candidate.SourceIndex.Where(value => value.SourceId != Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId));
        Assert.Contains(ContentScheduledPackSerializer.Validate(withoutSource).Issues,
            issue => issue.Code == "content.unknown-reference");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledPackSerializer.SerializeCanonical(withoutSource));
        var withoutMember = ContentTestData.Copy(candidate, elements: candidate.Elements.Where(value => value.ElementId != "nz-6-field-artillery"));
        Assert.Contains(ContentScheduledPackSerializer.Validate(withoutMember).Issues,
            issue => issue.Code == "content.unknown-reference");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledPackSerializer.SerializeCanonical(withoutMember));
        var scenario = Assert.Single(candidate.Scenarios);
        var noArea = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, scenario.ScheduledArrivals, [], scenario.Origin);
        var withoutArea = ContentTestData.Copy(candidate, scenarios: [noArea]);
        Assert.Contains(ContentScheduledPackSerializer.Validate(withoutArea).Issues,
            issue => issue.Code == "content.unknown-reference" && issue.Path.EndsWith("/entryAreaId", StringComparison.Ordinal));
    }

    [Fact]
    public void ScheduledArtifactReopensOnlyUnderItsExactContentIdentity()
    {
        var artifact = ContentScheduledPackArtifact.Create(Candidate());
        var retained = artifact.CanonicalBytes;
        var reopened = ContentScheduledPackArtifact.Read(retained, artifact.Identity);
        Assert.Equal(artifact.Identity, reopened.Identity);
        Assert.Equal(retained, reopened.CanonicalBytes);
        var changed = artifact.CanonicalBytes;
        changed[0] ^= 1;
        Assert.NotEqual(changed, artifact.CanonicalBytes);
        Assert.Throws<JsonException>(() => ContentScheduledPackArtifact.Read(changed, artifact.Identity));
        Assert.Throws<InvalidOperationException>(() => ContentScheduledPackArtifact.Read(retained,
            artifact.Identity with { Hash = ContentScheduledPackSerializer.CalculateHash(Candidate(3)) }));
        Assert.Throws<InvalidOperationException>(() => ContentScheduledPackArtifact.Read(retained,
            artifact.Identity with { PackId = "other-pack" }));
    }

    [Fact]
    public void SameSlotGroupsHaveStableIdentityRegardlessOfInputOrder()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var nz = Assert.Single(scenario.ScheduledArrivals);
        var origin = ContentTestData.Origin("fixture.unit.rtr");
        var formation = new ContentFormation("rtr-formation", "commonwealth", null,
            "land.organization.regiment", origin);
        var unit = new ContentCombatElement("2-rtr", "commonwealth", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, origin);
        var rtr = new ContentScheduledArrival("2-rtr-arrival", "commonwealth", new(2, 3),
            [unit.ElementId], "cairo", nz.Origin);
        ContentPackDefinition Build(ContentScheduledArrival[] arrivals) => ContentTestData.Copy(candidate,
            formations: [.. candidate.Formations, formation], elements: [.. candidate.Elements, unit],
            scenarios: [new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
                scenario.InitialPlacements, arrivals, scenario.EntryAreas, scenario.Origin)]);
        var first = ContentScheduledPackSerializer.SerializeCanonical(Build([nz, rtr]));
        var second = ContentScheduledPackSerializer.SerializeCanonical(Build([rtr, nz]));
        Assert.Equal(first, second);
        var read = ContentScheduledPackSerializer.Deserialize(first);
        Assert.Equal(2, Assert.Single(read.Scenarios).ScheduledArrivals.Count);
    }

    [Fact]
    public void PolishBrigadeSourceRosterSurvivesScheduledContentRoundTrip()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var arrival = Cna1979GrazianiSourceFacts.PolishBrigadeArrival;
        var origin = ContentTestData.Origin("fixture.polish");
        // Placeholder force characteristics only; the source record contributes roster and timing.
        var formation = new ContentFormation("fixture-polish", "commonwealth", null,
            "land.organization.regiment", origin);
        var members = arrival.ElementIds.Select(id => new ContentCombatElement(id, "commonwealth",
            formation.FormationId, "land.organization.battalion", Cna1979Movement.MotorizedMobilityId,
            20, ContentPlacementMode.Independent, origin)).ToArray();
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, [.. scenario.ScheduledArrivals, arrival], scenario.EntryAreas, scenario.Origin);
        var pack = ContentTestData.Copy(candidate, formations: [.. candidate.Formations, formation],
            elements: [.. candidate.Elements, .. members], scenarios: [updated]);
        var restored = ContentScheduledPackSerializer.Deserialize(ContentScheduledPackSerializer.SerializeCanonical(pack));
        var result = Assert.Single(Assert.Single(restored.Scenarios).ScheduledArrivals,
            group => group.GroupId == arrival.GroupId);
        Assert.Equal(arrival.ElementIds, result.ElementIds);
        Assert.Equal(new ContentScenarioBoundary(1, 3), result.Arrival);
    }

    [Fact]
    public void IndianBrigadeTruckValuesRoundTripWithoutTreatingUnspecifiedAsZero()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var arrival = Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival;
        var origin = ContentTestData.Origin("fixture.indian");
        var formation = new ContentFormation("fixture-indian", "commonwealth", null,
            "land.organization.regiment", origin);
        var members = arrival.ElementIds.Select(id => new ContentCombatElement(id, "commonwealth",
            formation.FormationId, "land.organization.battalion", Cna1979Movement.MotorizedMobilityId,
            20, ContentPlacementMode.Independent, origin)).ToArray();
        ContentPackDefinition Build(ContentScheduledArrival group) => ContentTestData.Copy(candidate,
            formations: [.. candidate.Formations, formation], elements: [.. candidate.Elements, .. members],
            scenarios: [new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
                scenario.InitialPlacements, [.. scenario.ScheduledArrivals, group], scenario.EntryAreas, scenario.Origin)]);
        var pack = Build(arrival);
        var restored = ContentScheduledPackSerializer.Deserialize(ContentScheduledPackSerializer.SerializeCanonical(pack));
        var result = Assert.Single(Assert.Single(restored.Scenarios).ScheduledArrivals,
            group => group.GroupId == arrival.GroupId);
        Assert.Equal(new ContentScenarioBoundary(5, 1), result.Arrival);
        Assert.Equal(4, result.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(5, 20, null), result.Trucks);
        var explicitZero = new ContentScheduledArrival(arrival.GroupId, arrival.SideId, arrival.Arrival,
            arrival.ElementIds, arrival.EntryAreaId, arrival.Origin, new ContentSourceTruckQuantities(5, 20, 0));
        Assert.NotEqual(ContentScheduledPackSerializer.CalculateHash(pack), ContentScheduledPackSerializer.CalculateHash(Build(explicitZero)));
        Assert.Throws<ArgumentException>(() => new ContentSourceTruckQuantities(null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentSourceTruckQuantities(-1, null, null));
        var encoded = Encoding.UTF8.GetString(ContentScheduledPackSerializer.SerializeCanonical(pack));
        var negative = encoded.Replace("\"Light\":5,\"Medium\":20", "\"Light\":-1,\"Medium\":20", StringComparison.Ordinal);
        var empty = encoded.Replace("\"Light\":5,\"Medium\":20", "\"Light\":null,\"Medium\":null", StringComparison.Ordinal);
        Assert.NotEqual(encoded, negative);
        Assert.NotEqual(encoded, empty);
        Assert.Throws<JsonException>(() => ContentScheduledPackSerializer.Deserialize(Encoding.UTF8.GetBytes(negative)));
        Assert.Throws<JsonException>(() => ContentScheduledPackSerializer.Deserialize(Encoding.UTF8.GetBytes(empty)));
    }

    [Fact]
    public void SixTurnChartRosterRoundTripsAsPartialContentWithoutCampaignAdmission()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var sourceGroups = Cna1979GrazianiSourceFacts.CommonwealthArrivalChartCandidates;
        var origin = ContentTestData.Origin("fixture.chart-roster");
        var formation = new ContentFormation("fixture-chart-roster", "commonwealth", null,
            "land.organization.regiment", origin);
        // Only IDs and schedules are source facts; these force properties are test placeholders.
        var members = sourceGroups.SelectMany(value => value.ElementIds)
            .Where(id => id != Cna1979GrazianiSourceFacts.NzFieldArtilleryId)
            .Select(id => new ContentCombatElement(id, "commonwealth", formation.FormationId,
                "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
                ContentPlacementMode.Independent, origin)).ToArray();
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, sourceGroups, scenario.EntryAreas, scenario.Origin);
        var pack = ContentTestData.Copy(candidate, formations: [.. candidate.Formations, formation],
            elements: [.. candidate.Elements, .. members], scenarios: [updated]);
        var bytes = ContentScheduledPackSerializer.SerializeCanonical(pack);
        var roundTrip = Assert.Single(ContentScheduledPackSerializer.Deserialize(bytes).Scenarios);
        Assert.Equal(8, roundTrip.ScheduledArrivals.Count);
        Assert.Equal(21, roundTrip.ScheduledArrivals.Sum(value => value.ElementIds.Count));
        Assert.Equal(sourceGroups.OrderBy(value => value.GroupId, StringComparer.Ordinal), roundTrip.ScheduledArrivals);
        var sourceCombatIds = Cna1979GrazianiSourceFacts.CommonwealthArrivalCombatFacts
            .Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(20, sourceCombatIds.Count);
        Assert.Equal([Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentId],
            sourceGroups.SelectMany(value => value.ElementIds).Where(id => !sourceCombatIds.Contains(id)).ToArray());
        Assert.Throws<InvalidContentPackException>(() => ContentPackSerializer.SerializeCanonical(pack));

        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin);
        var withLandSource = ContentTestData.Copy(pack, sourceIndex: [.. pack.SourceIndex,
            new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)]);
        var combat = new ContentPackV5Definition(withLandSource,
            [axis, .. Cna1979GrazianiSourceFacts.CommonwealthArrivalCombatFacts],
            [new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("fixture-axis-toe", 10, origin)]),
                .. Cna1979GrazianiSourceFacts.CreateCommonwealthArrivalToeSeeds(scenario.ScenarioId)]);
        var issues = ContentPackV5Validator.ValidateScheduledContent(combat).Issues;
        Assert.Contains(issues, issue => issue.Code == "content.combat.missing-element" && issue.Path.Contains("7-rtr", StringComparison.Ordinal));
        Assert.DoesNotContain(issues, issue => issue.Code == "content.initial-toe.missing-placement" && issue.Path.Contains("7-rtr", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.Code == "vocabulary.unknown-combat-component");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(combat));
        // Opt-in source interpretation: the OA's Matilda maps to the sole Mk II
        // chart profile, pending independent review. Fixture map and force fields remain placeholders.
        var interpreted = new ContentPackV5Definition(withLandSource,
            [axis, .. Cna1979GrazianiSourceFacts.CommonwealthArrivalCombatFacts,
                Cna1979GrazianiSourceFacts.SeventhRoyalTankMatildaMkIiCandidate],
            [new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("fixture-axis-toe", 10, origin)]),
                .. Cna1979GrazianiSourceFacts.CreateCommonwealthArrivalToeSeeds(scenario.ScenarioId)]);
        var interpretedIssues = ContentPackV5Validator.ValidateScheduledContent(interpreted).Issues;
        Assert.DoesNotContain(interpretedIssues, issue => issue.Code is "content.combat.missing-element" or "content.initial-toe.missing-placement");
        Assert.Contains(interpretedIssues, issue => issue.Code == "vocabulary.unknown-combat-component");
        var combatBytes = ContentScheduledCombatPackSerializer.SerializeCanonical(interpreted);
        var combatRoundTrip = ContentScheduledCombatPackSerializer.Deserialize(combatBytes);
        Assert.Equal(10, Assert.Single(Assert.Single(combatRoundTrip.InitialPlacementCombatFacts,
            value => value.ElementId == Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentId).InitialComponentToes).CurrentToe);
        var polishGun = Assert.Single(combatRoundTrip.ElementCombatFacts, value => value.ElementId == "polish-1-artillery");
        Assert.Equal(0, Assert.Single(polishGun.Components).OffensiveCloseAssaultRating);
        Assert.Equal(7, Assert.Single(polishGun.Components).BarrageRating);
        var polishWeapon = Assert.Single(polishGun.Components).WeaponSourceProfile!;
        Assert.Equal(ContentPrintedRatingKind.Parenthesized, polishWeapon.AntiArmor.Kind);
        Assert.Equal(2, polishWeapon.AntiArmor.Value);
        Assert.Equal(ContentPrintedRatingKind.Dash, polishWeapon.AntiAir.Kind);
        var nzGun = Assert.Single(combatRoundTrip.ElementCombatFacts, value => value.ElementId == Cna1979GrazianiSourceFacts.NzFieldArtilleryId);
        Assert.Equal(8, Assert.Single(nzGun.Components).BarrageRating);
        var nzWeapon = Assert.Single(nzGun.Components).WeaponSourceProfile!;
        Assert.Equal(ContentPrintedRating.Number(15), nzWeapon.CapabilityPoints);
        Assert.Equal(ContentPrintedRating.Number(5), nzWeapon.Vulnerability);
        Assert.Equal(ContentPrintedRatingKind.Dash, nzWeapon.ArmorProtection.Kind);
        var alteredWeapon = new ContentWeaponSourceProfile(nzWeapon.CapabilityPoints, ContentPrintedRating.Number(0),
            nzWeapon.AntiArmor, nzWeapon.Vulnerability, nzWeapon.ArmorProtection, nzWeapon.FuelRate, nzWeapon.BreakdownMarker);
        var oldNz = Assert.Single(nzGun.Components);
        var alteredNz = new ContentCombatComponent(oldNz.ComponentId, oldNz.ComponentClassId, oldNz.MaximumToe,
            oldNz.DefensiveCloseAssaultRating, oldNz.Origin, oldNz.DefensiveRatingParenthesized,
            oldNz.OffensiveCloseAssaultRating, oldNz.OffensiveRatingParenthesized, oldNz.BarrageRating,
            oldNz.BarrageRatingParenthesized, alteredWeapon);
        var alteredFacts = new ContentElementCombatFacts(nzGun.ElementId, nzGun.CombatClassificationId, [alteredNz], nzGun.Origin);
        var alteredCombat = new ContentPackV5Definition(withLandSource,
            interpreted.ElementCombatFacts.Select(value => value.ElementId == nzGun.ElementId ? alteredFacts : value),
            interpreted.InitialPlacementCombatFacts);
        Assert.NotEqual(ContentScheduledCombatPackSerializer.CalculateHash(interpreted),
            ContentScheduledCombatPackSerializer.CalculateHash(alteredCombat));
        Assert.Null(Assert.Single(Assert.Single(combatRoundTrip.ElementCombatFacts, value => value.ElementId == "axis-element").Components).OffensiveCloseAssaultRating);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(combatRoundTrip).Issues,
            issue => issue.Code == "vocabulary.unknown-combat-component");
    }

    [Fact]
    public void SourceHeadquartersRoundTripsWithoutInventingAnInfantryComponent()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var hq = Assert.Single(Cna1979GrazianiSourceFacts.ArrivingHeadquartersCombatFacts,
            value => value.ElementId == "polish-brigade-hq");
        var fixtureOrigin = ContentTestData.Origin("fixture.headquarters");
        var formation = new ContentFormation("fixture-hq-formation", "commonwealth", null,
            "land.organization.regiment", fixtureOrigin);
        var unit = new ContentCombatElement(hq.ElementId, "commonwealth", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, fixtureOrigin);
        var group = new ContentScheduledArrival("fixture-hq-arrival", "commonwealth", new(1, 3),
            [hq.ElementId], "cairo", hq.Origin);
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, [.. scenario.ScheduledArrivals, group], scenario.EntryAreas, scenario.Origin);
        var legacy = ContentTestData.Copy(candidate, formations: [.. candidate.Formations, formation],
            elements: [.. candidate.Elements, unit], scenarios: [updated]);
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, fixtureOrigin)], fixtureOrigin);
        var nz = new ContentElementCombatFacts(Cna1979GrazianiSourceFacts.NzFieldArtilleryId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-nz-toe", Cna1979Combat.InfantryComponentClassId, 6, 1, fixtureOrigin)], fixtureOrigin);
        // Other facts and seeds are placeholders; the headquarters facts are source-linked.
        var definition = new ContentPackV5Definition(legacy, [axis, nz, hq],
        [
            new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("axis-toe", 10, fixtureOrigin)]),
            new ContentInitialPlacementCombatFacts(scenario.ScenarioId, nz.ElementId, [new("fixture-nz-toe", 6, fixtureOrigin)]),
            new ContentInitialPlacementCombatFacts(scenario.ScenarioId, hq.ElementId, []),
        ]);
        var bytes = ContentScheduledCombatPackSerializer.SerializeCanonical(definition);
        var restored = ContentScheduledCombatPackSerializer.Deserialize(bytes);
        var restoredHq = Assert.Single(restored.ElementCombatFacts, value => value.ElementId == hq.ElementId);
        Assert.Equal(Cna1979Combat.HeadquartersClassificationId, restoredHq.CombatClassificationId);
        Assert.Empty(restoredHq.Components);
        Assert.Empty(Assert.Single(restored.InitialPlacementCombatFacts, value => value.ElementId == hq.ElementId).InitialComponentToes);
        Assert.False(ContentPackV5Validator.Validate(definition).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(definition));
        Assert.Equal(bytes, ContentScheduledCombatPackSerializer.SerializeCanonical(restored));
    }

    [Fact]
    public void CairoEntryAreaPreservesFiveSourceListedLocationsAndRejectsMissingMembers()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var area = Cna1979GrazianiSourceFacts.CairoEntryArea;
        var source = area.Origin;
        var fixtureOrigin = ContentTestData.Origin("fixture.cairo-topology");
        // Terrain and edges only satisfy the test pack's structural checks; they are not historical map data.
        var locations = area.LocationIds.Select(id => new ContentHex(id, "land.terrain.clear", null, fixtureOrigin)).ToArray();
        var edges = area.LocationIds.Select(id => new ContentHexEdge("west", id, [], fixtureOrigin)).ToArray();
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, scenario.ScheduledArrivals, [area], scenario.Origin);
        var pack = ContentTestData.Copy(candidate,
            sourceIndex: [.. candidate.SourceIndex, new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.AirLogisticsSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. candidate.Locations, .. locations], edges: [.. candidate.Edges, .. edges], scenarios: [updated]);
        var encoded = ContentScheduledPackSerializer.SerializeCanonical(pack);
        var restored = ContentScheduledPackSerializer.Deserialize(encoded);
        var restoredArea = Assert.Single(Assert.Single(restored.Scenarios).EntryAreas);
        Assert.Equal(area.LocationIds, restoredArea.LocationIds);
        Assert.Equal("cairo", Assert.Single(Assert.Single(restored.Scenarios).ScheduledArrivals).EntryAreaId);
        var alteredArea = new ContentLocationArea("cairo", area.LocationIds.Where(value => value != "e1931"), source);
        var alteredScenario = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, scenario.ScheduledArrivals, [alteredArea], scenario.Origin);
        var alteredPack = ContentTestData.Copy(pack, scenarios: [alteredScenario]);
        Assert.NotEqual(ContentScheduledPackSerializer.CalculateHash(pack), ContentScheduledPackSerializer.CalculateHash(alteredPack));
        var incomplete = ContentTestData.Copy(pack, locations: pack.Locations.Where(value => value.LocationId != "e1931"));
        Assert.Contains(ContentScheduledPackSerializer.Validate(incomplete).Issues,
            issue => issue.Code == "content.unknown-reference" && issue.Path.EndsWith("/entryAreas/cairo/locationIds/e1931", StringComparison.Ordinal));
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledPackSerializer.SerializeCanonical(incomplete));
    }

    [Fact]
    public void TobrukSourceSubsetRoundTripsAsAGroupWithoutInventingAttachments()
    {
        var candidate = Candidate();
        var scenario = Assert.Single(candidate.Scenarios);
        var group = Cna1979GrazianiSourceFacts.TobrukInfantryChartSubset;
        var support = Cna1979GrazianiSourceFacts.TobrukArtilleryAndAaChartSubset;
        var fixtureOrigin = ContentTestData.Origin("fixture.tobruk");
        // The members and co-location are source data; terrain and force fields are placeholders.
        var formation = new ContentFormation("fixture-tobruk", "axis", null, "land.organization.regiment", fixtureOrigin);
        var members = group.ElementIds.Concat(support.ElementIds).Select(id => new ContentCombatElement(id, "axis", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, fixtureOrigin)).ToArray();
        var location = new ContentHex("c4807", "land.terrain.clear", null, fixtureOrigin);
        var edge = new ContentHexEdge("west", "c4807", [], fixtureOrigin);
        ContentPackDefinition Build(ContentInitialGroup[] groups) => ContentTestData.Copy(candidate,
            sourceIndex: [.. candidate.SourceIndex,
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.AxisChartsSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. candidate.Locations, location], edges: [.. candidate.Edges, edge],
            formations: [.. candidate.Formations, formation], elements: [.. candidate.Elements, .. members],
            scenarios: [new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
                scenario.InitialPlacements, scenario.ScheduledArrivals, scenario.EntryAreas, groups, scenario.Origin)]);
        var pack = Build([group, support]);
        var bytes = ContentScheduledPackSerializer.SerializeCanonical(pack);
        var restoredGroups = Assert.Single(ContentScheduledPackSerializer.Deserialize(bytes).Scenarios).InitialGroups;
        var restored = Assert.Single(restoredGroups, value => value.GroupId == group.GroupId);
        Assert.Equal(group, restored);
        Assert.Equal(support, Assert.Single(restoredGroups, value => value.GroupId == support.GroupId));
        Assert.Equal(13, restored.ElementIds.Count);
        Assert.Equal("c4807", restored.LocationId);
        Assert.Equal(bytes, ContentScheduledPackSerializer.SerializeCanonical(Build([support, group])));
        var relocated = new ContentInitialGroup(group.GroupId, group.SideId, "west", group.ElementIds, group.Origin);
        Assert.NotEqual(ContentScheduledPackSerializer.CalculateHash(pack),
            ContentScheduledPackSerializer.CalculateHash(Build([relocated, support])));
        var withTrucks = new ContentInitialGroup(group.GroupId, group.SideId, group.LocationId,
            group.ElementIds, group.Origin, new ContentSourceTruckQuantities(null, 45, 5));
        var withZeroLight = new ContentInitialGroup(group.GroupId, group.SideId, group.LocationId,
            group.ElementIds, group.Origin, new ContentSourceTruckQuantities(0, 45, 5));
        var truckPack = Build([withTrucks, support]);
        var truckRoundTrip = ContentScheduledPackSerializer.Deserialize(ContentScheduledPackSerializer.SerializeCanonical(truckPack));
        Assert.Equal(withTrucks, Assert.Single(Assert.Single(truckRoundTrip.Scenarios).InitialGroups,
            value => value.GroupId == group.GroupId));
        Assert.NotEqual(ContentScheduledPackSerializer.CalculateHash(truckPack),
            ContentScheduledPackSerializer.CalculateHash(Build([withZeroLight, support])));
        Assert.Equal(bytes, ContentScheduledPackSerializer.SerializeCanonical(ContentScheduledPackSerializer.Deserialize(bytes)));
        Assert.Throws<InvalidContentPackException>(() => ContentPackSerializer.SerializeCanonical(pack));
        Assert.Throws<InvalidOperationException>(() => ContentPackSerializer.SerializeDraftDefinition(pack));
        var duplicated = new ContentInitialGroup("duplicate", "axis", "west", [group.ElementIds[0]], fixtureOrigin);
        Assert.Contains(ContentScheduledPackSerializer.Validate(Build([group, support, duplicated])).Issues,
            issue => issue.Code == "initial-group.duplicate-element");
        var wrongSide = new ContentInitialGroup(group.GroupId, "commonwealth", group.LocationId, group.ElementIds, group.Origin);
        Assert.Contains(ContentScheduledPackSerializer.Validate(Build([wrongSide, support])).Issues,
            issue => issue.Code == "initial-group.element-owner");
        var alreadyPlaced = new ContentInitialGroup("placed", "axis", "west", ["axis-element"], fixtureOrigin);
        Assert.Contains(ContentScheduledPackSerializer.Validate(Build([group, support, alreadyPlaced])).Issues,
            issue => issue.Code == "initial-group.initially-placed");
        var alreadyScheduled = new ContentInitialGroup("scheduled", "commonwealth", "west",
            [Cna1979GrazianiSourceFacts.NzFieldArtilleryId], fixtureOrigin);
        Assert.Contains(ContentScheduledPackSerializer.Validate(Build([group, support, alreadyScheduled])).Issues,
            issue => issue.Code == "arrival.initially-grouped");
        var unknown = new ContentInitialGroup("unknown", "axis", "missing-hex", ["missing-member"], fixtureOrigin);
        var unknownIssues = ContentScheduledPackSerializer.Validate(Build([group, support, unknown])).Issues;
        Assert.Contains(unknownIssues, issue => issue.Code == "content.unknown-reference" && issue.Path.EndsWith("/locationId", StringComparison.Ordinal));
        Assert.Contains(unknownIssues, issue => issue.Code == "content.unknown-reference" && issue.Path.EndsWith("/elementIds/missing-member", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new ContentInitialGroup("empty", "axis", "west", [], fixtureOrigin));

        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, fixtureOrigin)], fixtureOrigin);
        var nz = new ContentElementCombatFacts(Cna1979GrazianiSourceFacts.NzFieldArtilleryId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-nz-toe", Cna1979Combat.InfantryComponentClassId, 6, 1, fixtureOrigin)], fixtureOrigin);
        // Fixture baseline facts only; the Tobruk facts and seeds are source-linked.
        var combat = new ContentPackV5Definition(pack,
            [axis, nz, .. Cna1979GrazianiSourceFacts.TobrukInfantryCombatFacts,
                .. Cna1979GrazianiSourceFacts.TobrukSupportCombatFacts],
        [
            new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("fixture-axis-toe", 10, fixtureOrigin)]),
            new ContentInitialPlacementCombatFacts(scenario.ScenarioId, nz.ElementId, [new("fixture-nz-toe", 6, fixtureOrigin)]),
            .. Cna1979GrazianiSourceFacts.CreateTobrukInfantryToeSeeds(scenario.ScenarioId),
            .. Cna1979GrazianiSourceFacts.CreateTobrukSupportToeSeeds(scenario.ScenarioId),
        ]);
        var combatIssues = ContentPackV5Validator.ValidateScheduledContent(combat).Issues;
        Assert.Contains(combatIssues, issue => issue.Code == "content.combat.missing-element" && issue.Path.Contains("31-gaf-sdic-hq", StringComparison.Ordinal));
        Assert.Contains(combatIssues, issue => issue.Code == "content.combat.missing-element" && issue.Path.Contains("2-tobruk-coastal-defense-artillery", StringComparison.Ordinal));
        Assert.Contains(combatIssues, issue => issue.Code == "content.initial-toe.missing-placement" && issue.Path.Contains("31-gaf-sdic-hq", StringComparison.Ordinal));
        Assert.DoesNotContain(combatIssues, issue => issue.Code == "content.initial-toe.missing-placement" && issue.Path.Contains("tobruk-light-aa-battalion", StringComparison.Ordinal));
        Assert.DoesNotContain(combatIssues, issue => issue.Code == "content.unknown-reference");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(combat));
    }

    [Fact]
    public void InitialGroupWithoutArrivalsRequiresCombatSeedsForItsMembers()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var scenario = Assert.Single(baseline.Scenarios);
        var origin = ContentTestData.Origin("fixture.initial-group");
        var member = new ContentCombatElement("grouped-member", "axis", "axis-formation",
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.AttachmentOnly, origin);
        var group = new ContentInitialGroup("group", "axis", "west", [member.ElementId], origin);
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, [], [], [group], scenario.Origin);
        var legacy = ContentTestData.Copy(baseline, elements: [.. baseline.Elements, member], scenarios: [updated]);
        var bytes = ContentScheduledPackSerializer.SerializeCanonical(legacy);
        Assert.Equal(group, Assert.Single(Assert.Single(ContentScheduledPackSerializer.Deserialize(bytes).Scenarios).InitialGroups));
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin);
        var grouped = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("grouped-toe", Cna1979Combat.InfantryComponentClassId, 5, 1, origin)], origin);
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("axis-toe", 10, origin)]);
        var missing = new ContentPackV5Definition(legacy, [axis, grouped], [axisSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(missing).Issues,
            issue => issue.Code == "content.initial-toe.missing-placement" && issue.Path.Contains(member.ElementId, StringComparison.Ordinal));
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(missing));
        var complete = new ContentPackV5Definition(legacy, [axis, grouped],
            [axisSeed, new ContentInitialPlacementCombatFacts(scenario.ScenarioId, member.ElementId, [new("grouped-toe", 5, origin)])]);
        Assert.True(ContentPackV5Validator.ValidateScheduledContent(complete).IsValid);
        Assert.False(ContentPackV5Validator.Validate(complete).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(complete));
        Assert.Equal(ContentScheduledCombatPackSerializer.SerializeCanonical(complete),
            ContentScheduledCombatPackSerializer.SerializeCanonical(ContentScheduledCombatPackSerializer.Deserialize(
                ContentScheduledCombatPackSerializer.SerializeCanonical(complete))));
        var mixed = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
        [
            new ContentCombatComponent("mixed-a", Cna1979Combat.InfantryComponentClassId, 5, 1, origin),
            new ContentCombatComponent("mixed-b", Cna1979Combat.InfantryComponentClassId, 5, 1, origin),
        ], origin, maximumCombinedToe: 6);
        var mixedSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, member.ElementId,
            [new("mixed-a", 3, origin), new("mixed-b", 3, origin)]);
        var capped = new ContentPackV5Definition(legacy, [axis, mixed], [axisSeed, mixedSeed]);
        var cappedBytes = ContentScheduledCombatPackSerializer.SerializeCanonical(capped);
        var cappedRead = ContentScheduledCombatPackSerializer.Deserialize(cappedBytes);
        Assert.Equal(6, Assert.Single(cappedRead.ElementCombatFacts, value => value.ElementId == member.ElementId).MaximumCombinedToe);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(cappedRead).Issues,
            issue => issue.Code == "content.combat.combined-toe-unsupported");
        var excessiveSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, member.ElementId,
            [new("mixed-a", 4, origin), new("mixed-b", 3, origin)]);
        var excessive = new ContentPackV5Definition(legacy, [axis, mixed], [axisSeed, excessiveSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(excessive).Issues,
            issue => issue.Code == "content.initial-toe.over-combined-maximum");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(excessive));
        var pool = new ContentComponentToeLimit("guns", ["mixed-b", "mixed-a"], 6, origin);
        var pooled = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
        [
            new ContentCombatComponent("mixed-a", Cna1979Combat.InfantryComponentClassId, 5, 1, origin),
            new ContentCombatComponent("mixed-b", Cna1979Combat.InfantryComponentClassId, 5, 1, origin),
            new ContentCombatComponent("mixed-aa", Cna1979Combat.InfantryComponentClassId, 3, 1, origin),
        ], origin, maximumCombinedToe: 9, componentToeLimits: [pool]);
        var pooledSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, member.ElementId,
            [new("mixed-a", 3, origin), new("mixed-b", 3, origin), new("mixed-aa", 3, origin)]);
        var pooledPack = new ContentPackV5Definition(legacy, [axis, pooled], [axisSeed, pooledSeed]);
        var pooledBytes = ContentScheduledCombatPackSerializer.SerializeCanonical(pooledPack);
        var pooledRead = ContentScheduledCombatPackSerializer.Deserialize(pooledBytes);
        Assert.Equal(pool, Assert.Single(Assert.Single(pooledRead.ElementCombatFacts,
            value => value.ElementId == member.ElementId).ComponentToeLimits));
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(pooledRead).Issues,
            issue => issue.Code == "content.combat.component-limits-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(pooledPack));
        var expandedPool = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
            pooled.Components, origin, maximumCombinedToe: 9,
            componentToeLimits: [new("guns", ["mixed-a", "mixed-b"], 7, origin)]);
        var expandedPack = new ContentPackV5Definition(legacy, [axis, expandedPool], [axisSeed, pooledSeed]);
        Assert.NotEqual(ContentScheduledCombatPackSerializer.CalculateHash(pooledPack),
            ContentScheduledCombatPackSerializer.CalculateHash(expandedPack));
        var overPoolSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, member.ElementId,
            [new("mixed-a", 4, origin), new("mixed-b", 3, origin), new("mixed-aa", 2, origin)]);
        var overPool = new ContentPackV5Definition(legacy, [axis, pooled], [axisSeed, overPoolSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(overPool).Issues,
            issue => issue.Code == "content.initial-toe.over-component-limit");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(overPool));
        var badPool = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
            pooled.Components, origin, componentToeLimits: [new("unknown", ["not-a-component"], 1, origin)]);
        var invalidPool = new ContentPackV5Definition(legacy, [axis, badPool], [axisSeed, pooledSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(invalidPool).Issues,
            issue => issue.Code == "content.combat.unknown-limited-component");
        var unknownOrigin = new ContentOrigin(ContentOriginKind.SourceDerived, [new RuleReference("missing-source", "4.46b")]);
        var unproven = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
            pooled.Components, origin, componentToeLimits: [new("guns", ["mixed-a"], 5, unknownOrigin)]);
        var unprovenPack = new ContentPackV5Definition(legacy, [axis, unproven], [axisSeed, pooledSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(unprovenPack).Issues,
            issue => issue.Code == "content.unknown-reference" && issue.Path.Contains("componentToeLimits", StringComparison.Ordinal));
        var duplicateLimits = new ContentElementCombatFacts(member.ElementId, Cna1979Combat.CombatUnitClassificationId,
            pooled.Components, origin, componentToeLimits: [pool, pool]);
        var duplicatePack = new ContentPackV5Definition(legacy, [axis, duplicateLimits], [axisSeed, pooledSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(duplicatePack).Issues,
            issue => issue.Code == "content.combat.duplicate-component-limit");
    }

    [Fact]
    public void GrazianiDivisionalArtilleryLimitsAndStrengthsRoundTrip()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var scenario = Assert.Single(baseline.Scenarios);
        var origin = ContentTestData.Origin("fixture.divisional-artillery");
        var facts = Cna1979GrazianiSourceFacts.CireneMarmaricaArtilleryCombatFacts;
        var originalGroups = new[] { Cna1979GrazianiSourceFacts.CireneDivisionChartCandidate,
            Cna1979GrazianiSourceFacts.MarmaricaDivisionChartCandidate };
        // Only the two sourced artillery counters are selected; map and force properties are test placeholders.
        var groups = originalGroups.Select(group => new ContentInitialGroup(group.GroupId, group.SideId,
            group.LocationId, group.ElementIds.Where(id => facts.Any(value => value.ElementId == id)), group.Origin)).ToArray();
        var formation = new ContentFormation("fixture-divisional-artillery", "axis", null, "land.organization.regiment", origin);
        var members = facts.Select(value => new ContentCombatElement(value.ElementId, "axis", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, origin));
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, [], [], groups, scenario.Origin);
        var legacy = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.ErrataSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.AxisChartsSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("c4120", "land.terrain.clear", null, origin),
                new ContentHex("c3918", "land.terrain.clear", null, origin)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "c4120", [], origin),
                new ContentHexEdge("west", "c3918", [], origin)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. members], scenarios: [updated]);
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin);
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element",
            [new("fixture-axis-toe", 10, origin)]);
        var seeds = Cna1979GrazianiSourceFacts.CreateCireneMarmaricaArtilleryToeSeeds(scenario.ScenarioId);
        var pack = new ContentPackV5Definition(legacy, [axis, .. facts], [axisSeed, .. seeds]);
        var bytes = ContentScheduledCombatPackSerializer.SerializeCanonical(pack);
        var restored = ContentScheduledCombatPackSerializer.Deserialize(bytes);
        Assert.All(facts, value => Assert.Equal(value,
            Assert.Single(restored.ElementCombatFacts, item => item.ElementId == value.ElementId)));
        Assert.Equal(bytes, ContentScheduledCombatPackSerializer.SerializeCanonical(restored));
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(restored).Issues,
            issue => issue.Code == "content.combat.component-limits-unsupported");
    }

    [Fact]
    public void BardiaCandidateRequiresCounterSheetProvenance()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var scenario = Assert.Single(baseline.Scenarios);
        var group = Cna1979GrazianiSourceFacts.BardiaGarrisonChartCandidate;
        var origin = ContentTestData.Origin("fixture.bardia");
        var formation = new ContentFormation("fixture-bardia", "axis", null, "land.organization.regiment", origin);
        var members = group.ElementIds.Select(id => new ContentCombatElement(id, "axis", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, origin)).ToArray();
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, [], [], [group], scenario.Origin);
        var pack = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.AxisChartsSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("c4321", "land.terrain.clear", null, origin)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "c4321", [], origin)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. members], scenarios: [updated]);
        Assert.Contains(ContentScheduledPackSerializer.Validate(pack).Issues,
            issue => issue.Code == "content.unknown-reference" && issue.Path.Contains(Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId, StringComparison.Ordinal));
        var withCounter = ContentTestData.Copy(pack, sourceIndex: [.. pack.SourceIndex,
            new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId, ContentSourceKind.PublishedPrimary),
            new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)]);
        var restored = ContentScheduledPackSerializer.Deserialize(ContentScheduledPackSerializer.SerializeCanonical(withCounter));
        Assert.Equal(group, Assert.Single(Assert.Single(restored.Scenarios).InitialGroups));
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin);
        var sourceSeeds = Cna1979GrazianiSourceFacts.CreateBardiaInfantryToeSeeds(scenario.ScenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateBardiaSupportToeSeeds(scenario.ScenarioId)).ToArray();
        var facts = new[] { axis }.Concat(Cna1979GrazianiSourceFacts.BardiaInfantryCombatFacts)
            .Concat(Cna1979GrazianiSourceFacts.BardiaSupportCombatFacts).ToArray();
        var axisSeed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("fixture-axis-toe", 10, origin)]);
        var sourceCombat = new ContentPackV5Definition(withCounter, facts, [axisSeed, .. sourceSeeds]);
        Assert.DoesNotContain(ContentPackV5Validator.ValidateScheduledContent(sourceCombat).Issues,
            issue => issue.Code == "content.initial-toe.over-combined-maximum");
        var excessive = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "9-gaf-artillery",
            [new("9-gaf-artillery.100-17-howitzer", 5, origin), new("9-gaf-artillery.75-27-gun", 5, origin)]);
        var invalid = new ContentPackV5Definition(withCounter, facts,
            [axisSeed, .. sourceSeeds.Select(value => value.ElementId == excessive.ElementId ? excessive : value)]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(invalid).Issues,
            issue => issue.Code == "content.initial-toe.over-combined-maximum" && issue.Path.Contains("9-gaf-artillery", StringComparison.Ordinal));
    }

    [Fact]
    public void GiarabubConditionalAntiAirRatingRoundTripsButCannotEnterCurrentCombat()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var scenario = Assert.Single(baseline.Scenarios);
        var group = Cna1979GrazianiSourceFacts.GiarabubGarrisonChartCandidate;
        var origin = ContentTestData.Origin("fixture.giarabub");
        var formation = new ContentFormation("fixture-giarabub", "axis", null, "land.organization.regiment", origin);
        var members = group.ElementIds.Select(id => new ContentCombatElement(id, "axis", formation.FormationId,
            "land.organization.battalion", Cna1979Movement.MotorizedMobilityId, 20,
            ContentPlacementMode.Independent, origin)).ToArray();
        var updated = new ContentScenario(scenario.ScenarioId, scenario.Start, scenario.End,
            scenario.InitialPlacements, [], [], [group], scenario.Origin);
        // Map and element movement data are placeholders; the six group facts and seeds are source-linked.
        var legacy = ContentTestData.Copy(baseline,
            sourceIndex: [.. baseline.SourceIndex,
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.AxisChartsSourceId, ContentSourceKind.PublishedPrimary),
                new ContentSourceIndexEntry(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary)],
            locations: [.. baseline.Locations, new ContentHex("c1014", "land.terrain.clear", null, origin)],
            edges: [.. baseline.Edges, new ContentHexEdge("west", "c1014", [], origin)],
            formations: [.. baseline.Formations, formation], elements: [.. baseline.Elements, .. members], scenarios: [updated]);
        var axis = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("fixture-axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin);
        var definition = new ContentPackV5Definition(legacy, [axis, .. Cna1979GrazianiSourceFacts.GiarabubCombatFacts],
            [new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("fixture-axis-toe", 10, origin)]),
                .. Cna1979GrazianiSourceFacts.CreateGiarabubToeSeeds(scenario.ScenarioId)]);
        var issues = ContentPackV5Validator.ValidateScheduledContent(definition).Issues;
        Assert.Contains(issues, issue => issue.Code == "content.combat.parenthesized-defense-unsupported");
        Assert.Contains(issues, issue => issue.Code == "vocabulary.unknown-combat-classification");
        var bytes = ContentScheduledCombatPackSerializer.SerializeCanonical(definition);
        var restored = ContentScheduledCombatPackSerializer.Deserialize(bytes);
        var aa = Assert.Single(restored.ElementCombatFacts, value => value.ElementId == "giarabub-light-aa");
        Assert.True(Assert.Single(aa.Components).DefensiveRatingParenthesized);
        Assert.Equal(1, Assert.Single(aa.Components).OffensiveCloseAssaultRating);
        Assert.True(Assert.Single(aa.Components).OffensiveRatingParenthesized);
        var machineGun = Assert.Single(restored.ElementCombatFacts, value => value.ElementId == "giarabub-machine-gun");
        Assert.Equal(4, Assert.Single(machineGun.Components).OffensiveCloseAssaultRating);
        var artillery = Assert.Single(restored.ElementCombatFacts, value => value.ElementId == "giarabub-artillery");
        Assert.Equal(5, Assert.Single(artillery.Components).BarrageRating);
        Assert.Equal(bytes, ContentScheduledCombatPackSerializer.SerializeCanonical(restored));
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(restored).Issues,
            issue => issue.Code == "content.combat.parenthesized-defense-unsupported");
    }

    [Fact]
    public void LegacyCombatContentRejectsConditionalRatingRatherThanDroppingIt()
    {
        var legacy = ContentTestData.CreateMinimalPack();
        var scenario = Assert.Single(legacy.Scenarios);
        var origin = ContentTestData.Origin("fixture.parenthesized");
        var conditional = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin,
                defensiveRatingParenthesized: true)], origin);
        var seed = new ContentInitialPlacementCombatFacts(scenario.ScenarioId, "axis-element", [new("axis-toe", 10, origin)]);
        var definition = new ContentPackV5Definition(legacy, [conditional], [seed]);
        Assert.Contains(ContentPackV5Validator.Validate(definition).Issues,
            issue => issue.Code == "content.combat.parenthesized-defense-unsupported");
        Assert.Contains(Cna1979ContentV5CompatibilityValidator.Validate(definition).Issues,
            issue => issue.Code == "content.combat.parenthesized-defense-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(definition));
        var offensiveZero = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin,
                offensiveCloseAssaultRating: 0)], origin);
        var zeroDefinition = new ContentPackV5Definition(legacy, [offensiveZero], [seed]);
        Assert.Contains(ContentPackV5Validator.Validate(zeroDefinition).Issues,
            issue => issue.Code == "content.combat.offensive-rating-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(zeroDefinition));
        var barrageZero = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin,
                barrageRating: 0)], origin);
        var barrageDefinition = new ContentPackV5Definition(legacy, [barrageZero], [seed]);
        Assert.Contains(ContentPackV5Validator.Validate(barrageDefinition).Issues,
            issue => issue.Code == "content.combat.barrage-rating-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(barrageDefinition));
        var weapon = Assert.Single(Cna1979GrazianiSourceFacts.NzFieldArtilleryCombatFacts.Components).WeaponSourceProfile!;
        var profiled = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin,
                weaponSourceProfile: weapon)], origin);
        var profiledDefinition = new ContentPackV5Definition(legacy, [profiled], [seed]);
        Assert.Contains(ContentPackV5Validator.Validate(profiledDefinition).Issues,
            issue => issue.Code == "content.combat.weapon-source-profile-unsupported");
        Assert.Contains(Cna1979ContentV5CompatibilityValidator.Validate(profiledDefinition).Issues,
            issue => issue.Code == "content.combat.weapon-source-profile-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(profiledDefinition));
        var capped = new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin,
            maximumCombinedToe: 10);
        var cappedDefinition = new ContentPackV5Definition(legacy, [capped], [seed]);
        Assert.Contains(ContentPackV5Validator.Validate(cappedDefinition).Issues,
            issue => issue.Code == "content.combat.combined-toe-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackV5Serializer.SerializeCanonical(cappedDefinition));
        Assert.Throws<ArgumentException>(() => new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId,
            10, 1, origin, offensiveRatingParenthesized: true));
        Assert.Throws<ArgumentException>(() => new ContentCombatComponent("axis-toe", Cna1979Combat.InfantryComponentClassId,
            10, 1, origin, barrageRatingParenthesized: true));
        Assert.Throws<ArgumentException>(() => new ContentPrintedRating(ContentPrintedRatingKind.Dash, 0));
        Assert.NotEqual(ContentPrintedRating.Dash, ContentPrintedRating.Number(0));
        Assert.Throws<ArgumentException>(() => new ContentWeaponSourceProfile(weapon.CapabilityPoints, weapon.AntiAir,
            weapon.AntiArmor, weapon.Vulnerability, weapon.ArmorProtection, weapon.FuelRate, "01R"));
    }

    [Fact]
    public void ScheduledContentDoesNotBypassNormalCampaignAdmission()
    {
        var artifact = ContentScheduledPackArtifact.Create(Candidate());
        var request = new CampaignCreationRequest(1, "graziani-content-test", Cna1979Ruleset.Manifest.Hash, 12345,
            "rules-lab.breakdown.truck.v1", "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
            artifact.Identity.PackId, artifact.Identity.Hash, "minimal-lab");
        var result = CampaignExercises.Begin(request);
        Assert.False(result.IsStarted);
        Assert.Null(result.Session);
        Assert.Null(result.CreationEventBytes);
        Assert.Null(result.InitialSnapshotBytes);
    }

    [Fact]
    public void ScheduledCombatValidationRequiresSeedsForTheArrivingUnit()
    {
        var legacy = Candidate();
        var origin = ContentTestData.Origin("fixture.combat");
        var facts = new[]
        {
            new ContentElementCombatFacts("axis-element", Cna1979Combat.CombatUnitClassificationId,
                [new ContentCombatComponent("axis-component", Cna1979Combat.InfantryComponentClassId, 10, 1, origin)], origin),
            new ContentElementCombatFacts("nz-6-field-artillery", Cna1979Combat.CombatUnitClassificationId,
                [new ContentCombatComponent("nz-component", Cna1979Combat.InfantryComponentClassId, 6, 1, origin)], origin),
        };
        var axis = new ContentInitialPlacementCombatFacts("minimal-lab", "axis-element",
            [new ContentInitialComponentToe("axis-component", 10, origin)]);
        // Test seed only: the source's printed weapon row still needs combat-model interpretation.
        var arriving = new ContentInitialPlacementCombatFacts("minimal-lab", "nz-6-field-artillery",
            [new ContentInitialComponentToe("nz-component", 6, origin)]);
        var complete = new ContentPackV5Definition(legacy, facts, [axis, arriving]);
        Assert.True(ContentPackV5Validator.ValidateScheduledContent(complete).IsValid);
        Assert.False(ContentPackV5Validator.Validate(complete).IsValid);
        var missing = new ContentPackV5Definition(legacy, facts, [axis]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(missing).Issues,
            issue => issue.Code == "content.initial-toe.missing-placement");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(missing));
        var excessive = new ContentInitialPlacementCombatFacts("minimal-lab", "nz-6-field-artillery",
            [new ContentInitialComponentToe("nz-component", 7, origin)]);
        var invalid = new ContentPackV5Definition(legacy, facts, [axis, excessive]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(invalid).Issues,
            issue => issue.Code == "content.initial-toe.over-maximum");
        Assert.Throws<InvalidContentPackException>(() => ContentScheduledCombatPackSerializer.SerializeCanonical(invalid));
        var chartOrigin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a"), new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.47"),
             new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-nz-p18")]);
        var artillery = Cna1979GrazianiSourceFacts.NzFieldArtilleryCombatFacts;
        var sourceSeed = new ContentInitialPlacementCombatFacts("minimal-lab", "nz-6-field-artillery",
            [new ContentInitialComponentToe(Cna1979GrazianiSourceFacts.NzFieldArtilleryComponentId, 6, chartOrigin)]);
        var unsupported = new ContentPackV5Definition(legacy, [facts[0], artillery], [axis, sourceSeed]);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(unsupported).Issues,
            issue => issue.Code == "vocabulary.unknown-combat-component");
        var encoded = ContentScheduledCombatPackSerializer.SerializeCanonical(unsupported);
        var restored = ContentScheduledCombatPackSerializer.Deserialize(encoded);
        var restoredArtillery = restored.ElementCombatFacts.Single(value => value.ElementId == "nz-6-field-artillery");
        var component = Assert.Single(restoredArtillery.Components);
        Assert.Equal("land.combat-component.artillery", component.ComponentClassId);
        Assert.Equal(6, component.MaximumToe);
        Assert.Equal(1, component.DefensiveCloseAssaultRating);
        Assert.Equal(6, Assert.Single(restored.InitialPlacementCombatFacts.Single(value => value.ElementId == "nz-6-field-artillery")
            .InitialComponentToes).CurrentToe);
        Assert.Contains(ContentPackV5Validator.ValidateScheduledContent(restored).Issues,
            issue => issue.Code == "vocabulary.unknown-combat-component");
        Assert.Equal(encoded, ContentScheduledCombatPackSerializer.SerializeCanonical(restored));
        Assert.Throws<JsonException>(() => ContentScheduledCombatPackSerializer.Deserialize([.. encoded, (byte)' ']));
        var alteredSeed = new ContentInitialPlacementCombatFacts("minimal-lab", "nz-6-field-artillery",
            [new ContentInitialComponentToe(Cna1979GrazianiSourceFacts.NzFieldArtilleryComponentId, 5, chartOrigin)]);
        var changed = new ContentPackV5Definition(legacy, [facts[0], artillery], [axis, alteredSeed]);
        Assert.NotEqual(ContentScheduledCombatPackSerializer.CalculateHash(unsupported),
            ContentScheduledCombatPackSerializer.CalculateHash(changed));
        var artifact = ContentScheduledCombatPackArtifact.Create(unsupported);
        Assert.Equal(artifact.Identity, ContentScheduledCombatPackArtifact.Read(artifact.CanonicalBytes, artifact.Identity).Identity);
        Assert.Throws<InvalidOperationException>(() => ContentScheduledCombatPackArtifact.Read(artifact.CanonicalBytes,
            artifact.Identity with { Hash = ContentScheduledCombatPackSerializer.CalculateHash(changed) }));
    }
}
