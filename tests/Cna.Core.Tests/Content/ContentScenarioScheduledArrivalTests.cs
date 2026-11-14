using System.Text;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class ContentScenarioScheduledArrivalTests
{
    [Fact]
    public void LibyanDivisionsRetainTheirActualInitialGroupsAndInfantryStrengths()
    {
        var first = Cna1979GrazianiSourceFacts.FirstLibyanDivisionChartCandidate;
        var second = Cna1979GrazianiSourceFacts.SecondLibyanDivisionChartCandidate;
        Assert.Equal("c4020", first.LocationId);
        Assert.Equal("c3920", second.LocationId);
        Assert.Equal(11, first.ElementIds.Count);
        Assert.Equal(11, second.ElementIds.Count);
        Assert.Contains("ix-libyan-light-tank", second.ElementIds);
        Assert.Equal(new ContentSourceTruckQuantities(10, 15, null), first.Trucks);
        Assert.Equal(new ContentSourceTruckQuantities(15, 15, null), second.Trucks);
        var facts = Cna1979GrazianiSourceFacts.LibyanDivisionInfantryCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateLibyanDivisionInfantryToeSeeds("graziani");
        Assert.Equal(12, facts.Count);
        Assert.Equal(12, seeds.Count);
        Assert.All(seeds, seed =>
        {
            Assert.Contains(seed.ElementId, first.ElementIds.Concat(second.ElementIds));
            Assert.Equal(5, Assert.Single(seed.InitialComponentToes).CurrentToe);
        });
        Assert.DoesNotContain(facts, value => value.ElementId == "ix-libyan-light-tank");
        var tank = Cna1979GrazianiSourceFacts.AttachedNinthLibyanTankCombatFacts;
        var tankComponent = Assert.Single(tank.Components);
        Assert.Equal(Cna1979GrazianiSourceFacts.ArmorComponentClassId, tankComponent.ComponentClassId);
        Assert.Equal(2, tankComponent.DefensiveCloseAssaultRating);
        Assert.Equal(9, Assert.Single(Cna1979GrazianiSourceFacts.CreateAttachedNinthLibyanTankToeSeed("graziani")
            .InitialComponentToes).CurrentToe);
        var support = Cna1979GrazianiSourceFacts.LibyanDivisionSupportCombatFacts;
        var headquarters = support.Where(value => value.CombatClassificationId == Cna1979Combat.HeadquartersClassificationId).ToArray();
        Assert.Equal(4, headquarters.Length);
        Assert.All(headquarters, value => Assert.Equal(Cna1979GrazianiSourceFacts.ArtilleryComponentClassId,
            Assert.Single(value.Components).ComponentClassId));
        var mixed = Assert.Single(support, value => value.ElementId == "2-libyan-artillery");
        Assert.Equal(12, mixed.MaximumCombinedToe);
        var supportSeeds = Cna1979GrazianiSourceFacts.CreateLibyanDivisionSupportToeSeeds("graziani");
        var secondArtillery = Assert.Single(supportSeeds, value => value.ElementId == mixed.ElementId);
        Assert.Equal(9, Assert.Single(secondArtillery.InitialComponentToes,
            value => value.ComponentId.EndsWith(".75-27-gun", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(3, Assert.Single(secondArtillery.InitialComponentToes,
            value => value.ComponentId.EndsWith(".100-17-howitzer", StringComparison.Ordinal)).CurrentToe);
    }

    [Fact]
    public void BenghaziGarrisonRetainsSourceStrengthsAndUnresolvedEngineer()
    {
        var group = Cna1979GrazianiSourceFacts.BenghaziGarrisonChartCandidate;
        Assert.Equal("a4827", group.LocationId);
        Assert.Equal(4, group.ElementIds.Count);
        Assert.Contains("8-2-benghazi-engineers", group.ElementIds);
        var facts = Cna1979GrazianiSourceFacts.BenghaziCombatFacts;
        Assert.Equal(3, facts.Count);
        Assert.DoesNotContain(facts, value => value.ElementId == "8-2-benghazi-engineers");
        var seeds = Cna1979GrazianiSourceFacts.CreateBenghaziToeSeeds("graziani");
        Assert.Equal(5, Assert.Single(Assert.Single(seeds, x => x.ElementId == "2-libyan-coastal-defense-battalion").InitialComponentToes).CurrentToe);
        Assert.Equal(6, Assert.Single(Assert.Single(seeds, x => x.ElementId == "7-benghazi-coastal-defense-aa").InitialComponentToes).CurrentToe);
        Assert.Equal(6, Assert.Single(Assert.Single(seeds, x => x.ElementId == "20-corps-artillery").InitialComponentToes).CurrentToe);
        Assert.True(Assert.Single(Assert.Single(facts, x => x.ElementId == "7-benghazi-coastal-defense-aa").Components).DefensiveRatingParenthesized);
        Assert.Contains(group.Origin.References, x => x.SourceId == Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId);
    }

    [Fact]
    public void TobrukVichyGunUsesItalianWeaponChartVulnerability()
    {
        var facts = Assert.Single(Cna1979GrazianiSourceFacts.TobrukSupportCombatFacts,
            value => value.ElementId == "6-tobruk-artiglieria");
        var profile = Assert.Single(facts.Components).WeaponSourceProfile;
        Assert.NotNull(profile);
        Assert.Equal(ContentPrintedRating.Number(5), profile.Vulnerability);
    }

    [Fact]
    public void CommonwealthSixtyPounderUsesWeaponChartAssaultRatings()
    {
        var facts = Assert.Single(Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts,
            value => value.ElementId == "7-medium-artillery");
        var component = Assert.Single(facts.Components);
        Assert.Equal(1, component.OffensiveCloseAssaultRating);
        Assert.Equal(0, component.DefensiveCloseAssaultRating);
    }

    [Fact]
    public void CommonwealthInitialGroupsPreservePrintedSharedLocationsAndTrucks()
    {
        var groups = Cna1979GrazianiSourceFacts.CommonwealthInitialGroupCandidates;
        var placements = Cna1979GrazianiSourceFacts.CommonwealthInitialPlacementCandidates;
        Assert.Equal(4, groups.Count);
        Assert.Equal(9, groups.Sum(group => group.ElementIds.Count));
        Assert.All(groups, group => Assert.All(group.ElementIds, id =>
            Assert.Contains(placements, placement => placement.ElementId == id && placement.LocationId == group.LocationId)));
        Assert.Equal(new ContentSourceTruckQuantities(null, 10, null),
            Assert.Single(groups, group => group.LocationId == "c4131").Trucks);
        Assert.Equal(new ContentSourceTruckQuantities(null, 15, 5),
            Assert.Single(groups, group => group.LocationId == "c3922").Trucks);
        Assert.Equal(new ContentSourceTruckQuantities(5, 5, null),
            Assert.Single(groups, group => group.LocationId == "c3721").Trucks);
    }

    [Fact]
    public void FrenchMotorMarineCandidateUsesCorrectedDeploymentAndSourceStrength()
    {
        var group = Cna1979GrazianiSourceFacts.FrenchMotorMarineInitialCandidate;
        var facts = Cna1979GrazianiSourceFacts.FrenchMotorMarineCombatCandidate;
        var seed = Cna1979GrazianiSourceFacts.CreateFrenchMotorMarineToeCandidate("graziani");
        Assert.Equal("c3926", group.LocationId);
        Assert.Equal(facts.ElementId, Assert.Single(group.ElementIds));
        Assert.Equal(new ContentSourceTruckQuantities(null, 2, null), group.Trucks);
        Assert.Contains(group.Origin.References, reference =>
            reference.SourceId == Cna1979GrazianiSourceFacts.ErrataSourceId && reference.Locator == "4.44b");
        var component = Assert.Single(facts.Components);
        Assert.Equal(1, component.MaximumToe);
        Assert.Equal(1, component.DefensiveCloseAssaultRating);
        Assert.Equal(1, component.OffensiveCloseAssaultRating);
        Assert.Equal(1, Assert.Single(seed.InitialComponentToes).CurrentToe);
        Assert.Contains(Cna1979GrazianiSourceFacts.CommonwealthInitialPlacementCandidates,
            placement => placement.ElementId == facts.ElementId && placement.LocationId == group.LocationId);
        Assert.Equal(seed, Assert.Single(Cna1979GrazianiSourceFacts.CreateCommonwealthInitialToeSeeds("graziani"),
            value => value.ElementId == facts.ElementId));
    }

    [Fact]
    public void SixteenthBrigadeCandidateKeepsDeployedMembersAndGunStrength()
    {
        var group = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate;
        var facts = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateSixteenthInfantryBrigadeToeCandidates("graziani");
        Assert.Equal("e1829", group.LocationId);
        Assert.Equal(new ContentSourceTruckQuantities(null, 20, null), group.Trucks);
        Assert.Equal(group.ElementIds.Order(StringComparer.Ordinal), facts.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.Equal(group.ElementIds.Order(StringComparer.Ordinal), seeds.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain("16-infantry-brigade-antitank", group.ElementIds);
        var hq = Assert.Single(facts, value => value.ElementId == "16-infantry-brigade-hq");
        Assert.Equal(Cna1979Combat.HeadquartersClassificationId, hq.CombatClassificationId);
        var gun = Assert.Single(hq.Components);
        Assert.Equal(3, gun.MaximumToe);
        Assert.Equal(7, gun.BarrageRating);
        Assert.Equal(0, gun.DefensiveCloseAssaultRating);
        Assert.Equal(3, Assert.Single(Assert.Single(seeds, value => value.ElementId == hq.ElementId).InitialComponentToes).CurrentToe);
        Assert.All(facts.Where(value => value.ElementId != hq.ElementId), value =>
        {
            var infantry = Assert.Single(value.Components);
            Assert.Equal(6, infantry.MaximumToe);
            Assert.Equal(2, infantry.DefensiveCloseAssaultRating);
            Assert.Equal(2, infantry.OffensiveCloseAssaultRating);
        });
    }

    [Fact]
    public void FourthIndianCandidateExcludesDetachedAndLaterArrivals()
    {
        var group = Cna1979GrazianiSourceFacts.FourthIndianDivisionInitialCandidate;
        var facts = Cna1979GrazianiSourceFacts.FourthIndianDivisionCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateFourthIndianDivisionToeCandidates("graziani");
        Assert.Equal("d3615", group.LocationId);
        Assert.Equal(11, group.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(5, 20, 2), group.Trucks);
        Assert.Contains("central-india-horse", group.ElementIds);
        Assert.DoesNotContain("31-field-artillery", group.ElementIds);
        Assert.DoesNotContain("7-indian-brigade-hq", group.ElementIds);
        Assert.DoesNotContain("1-field-artillery", group.ElementIds);
        Assert.Equal(10, facts.Count);
        Assert.DoesNotContain(facts, value => value.ElementId == "central-india-horse");
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.Equal(3, facts.Count(value => value.CombatClassificationId == Cna1979Combat.HeadquartersClassificationId));
        Assert.All(facts.Where(value => value.CombatClassificationId == Cna1979Combat.HeadquartersClassificationId),
            value => Assert.Empty(Assert.Single(seeds, seed => seed.ElementId == value.ElementId).InitialComponentToes));
        var gun = Assert.Single(Assert.Single(facts, value => value.ElementId == "25-field-artillery").Components);
        Assert.Equal(7, gun.BarrageRating);
        Assert.Equal(6, Assert.Single(Assert.Single(seeds, value => value.ElementId == "25-field-artillery").InitialComponentToes).CurrentToe);
    }

    [Fact]
    public void SeventhArmoredCandidateKeepsRemainingDeployedUnitsAndConditionalHqTanks()
    {
        var group = Cna1979GrazianiSourceFacts.SeventhArmoredDivisionInitialCandidate;
        var facts = Cna1979GrazianiSourceFacts.SeventhArmoredDivisionCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateSeventhArmoredDivisionToeCandidates("graziani");
        Assert.Equal("d3612", group.LocationId);
        Assert.Equal(new ContentSourceTruckQuantities(5, 5, 5), group.Trucks);
        Assert.Equal(7, group.ElementIds.Count);
        Assert.Equal(group.ElementIds.Order(StringComparer.Ordinal), facts.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain("1-rtr", group.ElementIds);
        Assert.DoesNotContain("7-motor-brigade-hq", group.ElementIds);
        Assert.All(facts.Where(value => value.ElementId is "7-armored-division-hq" or "4-armored-brigade-hq" or "7-armored-brigade-hq"), value =>
        {
            var tank = Assert.Single(value.Components);
            Assert.True(tank.DefensiveRatingParenthesized);
            Assert.True(tank.OffensiveRatingParenthesized);
        });
        Assert.Empty(Assert.Single(seeds, value => value.ElementId == "7-support-group-hq").InitialComponentToes);
        Assert.Equal(7, Assert.Single(Assert.Single(seeds, value => value.ElementId == "8-hussars").InitialComponentToes).CurrentToe);
        Assert.Equal(10, Assert.Single(Assert.Single(seeds, value => value.ElementId == "6-rtr").InitialComponentToes).CurrentToe);
    }

    [Fact]
    public void Cv33WeaponProfilePreservesPrintedVulnerabilityDash()
    {
        var component = Assert.Single(Cna1979GrazianiSourceFacts.AttachedNinthLibyanTankCombatFacts.Components);
        Assert.Equal(ContentPrintedRating.Dash, component.WeaponSourceProfile!.Vulnerability);
    }

    [Fact]
    public void LibyanTankRegimentCandidatesPreserveDetachedAndConditionalCases()
    {
        var aresca = Cna1979GrazianiSourceFacts.ArescaTankRegimentInitialCandidate;
        var tripoli = Cna1979GrazianiSourceFacts.TripoliTankRegimentInitialSubset;
        var facts = Cna1979GrazianiSourceFacts.LibyanTankRegimentCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateLibyanTankRegimentToeCandidates("graziani");
        Assert.Equal("c3919", aresca.LocationId);
        Assert.Equal(new ContentSourceTruckQuantities(null, 5, 5), aresca.Trucks);
        Assert.Contains("xxi-libyan-light-tank", aresca.ElementIds);
        Assert.Equal("c4321", tripoli.LocationId);
        Assert.Null(tripoli.Trucks); // Bardia's 20 medium trucks are shared with the garrison.
        Assert.DoesNotContain("ix-libyan-light-tank", tripoli.ElementIds);
        Assert.DoesNotContain("lx-libyan-light-tank", tripoli.ElementIds);
        Assert.Equal(aresca.ElementIds.Concat(tripoli.ElementIds).Order(StringComparer.Ordinal),
            facts.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.Equal(facts.Select(value => value.ElementId).Order(StringComparer.Ordinal),
            seeds.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.All(facts, value =>
        {
            var component = Assert.Single(value.Components);
            var headquarters = value.CombatClassificationId == Cna1979Combat.HeadquartersClassificationId;
            Assert.Equal(headquarters ? 1 : 9, component.MaximumToe);
            Assert.Equal(headquarters, component.DefensiveRatingParenthesized);
            Assert.Equal(headquarters, component.OffensiveRatingParenthesized);
            Assert.Equal(component.MaximumToe, Assert.Single(Assert.Single(seeds,
                seed => seed.ElementId == value.ElementId).InitialComponentToes).CurrentToe);
        });
    }

    [Fact]
    public void MalettiSourceCandidatesKeepActualStrengthAndUnresolvedSplit()
    {
        var group = Cna1979GrazianiSourceFacts.MalettiInitialChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.MalettiCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateMalettiToeCandidates("graziani");
        Assert.Equal("c3617", group.LocationId);
        Assert.Equal(15, group.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(5, 30, 5), group.Trucks);
        Assert.Equal(11, facts.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.DoesNotContain(facts, value => value.ElementId.Contains("headquarters", StringComparison.Ordinal)
            || value.ElementId.EndsWith("-hq", StringComparison.Ordinal));
        Assert.DoesNotContain(group.ElementIds, value => value.Contains("lx-libyan", StringComparison.Ordinal));
        int Toe(string id) => Assert.Single(Assert.Single(seeds, value => value.ElementId == id).InitialComponentToes).CurrentToe;
        Assert.Equal(3, Toe("1-maletti-artillery"));
        Assert.Equal(2, Toe("2-maletti-artillery"));
        Assert.Equal(7, Toe("ii-medium-tank"));
        var tank = Assert.Single(Assert.Single(facts, value => value.ElementId == "ii-medium-tank").Components);
        Assert.Equal(ContentPrintedRating.Number(2), tank.WeaponSourceProfile!.ArmorProtection);
        Assert.Contains(tank.Origin.References, value => value.Locator == "oa-libyan-tank-command-p6");
        Assert.All(facts.Where(value => value.ElementId.Contains("sahariano-camel", StringComparison.Ordinal)), value =>
            Assert.Equal(Cna1979Combat.ReconnaissanceComponentClassId, Assert.Single(value.Components).ComponentClassId));
    }

    [Fact]
    public void CatanzaroCandidateRetainsMixedArtilleryAndUnresolvedMembers()
    {
        var group = Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.CatanzaroCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateCatanzaroToeCandidates("graziani");
        Assert.Equal("c4707", group.LocationId);
        Assert.Equal(13, group.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(null, 30, 5), group.Trucks);
        Assert.Equal(11, facts.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.DoesNotContain(facts, value => value.ElementId is "64-catanzaro-hq" or "64-engineer");
        var artillery = Assert.Single(facts, value => value.ElementId == "203-artillery");
        Assert.Equal(12, artillery.MaximumCombinedToe);
        Assert.Equal(9, Assert.Single(artillery.ComponentToeLimits).MaximumToe);
        var artillerySeeds = Assert.Single(seeds, value => value.ElementId == artillery.ElementId).InitialComponentToes;
        Assert.Equal(6, Assert.Single(artillerySeeds, value => value.ComponentId.EndsWith(".75-18-gun-howitzer", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(3, Assert.Single(artillerySeeds, value => value.ComponentId.EndsWith(".105-28-gun", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(3, Assert.Single(artillerySeeds, value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.True(Assert.Single(artillery.Components, value => value.ComponentClassId == Cna1979GrazianiSourceFacts.AntiAirComponentClassId)
            .DefensiveRatingParenthesized);
    }

    [Fact]
    public void FirstCcnnCandidateKeepsAttachedTankAndConditionalHeavyAa()
    {
        var group = Cna1979GrazianiSourceFacts.FirstCcnnInitialChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.FirstCcnnCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateFirstCcnnToeCandidates("graziani");
        Assert.Equal("c4218", group.LocationId);
        Assert.Equal(14, group.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(10, 25, 10), group.Trucks);
        Assert.Equal(12, facts.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.DoesNotContain(facts, value => value.ElementId is "1-ccnn-hq" or "201-ccnn-engineer");
        var artillery = Assert.Single(facts, value => value.ElementId == "201-ccnn-artillery");
        Assert.Equal(12, artillery.MaximumCombinedToe);
        Assert.Equal(9, Assert.Single(artillery.ComponentToeLimits).MaximumToe);
        var aa = Assert.Single(artillery.Components, value => value.ComponentId.EndsWith(".heavy-aa", StringComparison.Ordinal));
        Assert.True(aa.DefensiveRatingParenthesized);
        Assert.Equal(ContentPrintedRating.Parenthesized(5), aa.WeaponSourceProfile!.AntiArmor);
        var artillerySeeds = Assert.Single(seeds, value => value.ElementId == artillery.ElementId).InitialComponentToes;
        Assert.Equal(6, Assert.Single(artillerySeeds, value => value.ComponentId.EndsWith(".75-27-gun", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(3, Assert.Single(artillerySeeds, value => value.ComponentId.EndsWith(".100-17-howitzer", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(2, Assert.Single(artillerySeeds, value => value.ComponentId.EndsWith(".heavy-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(7, Assert.Single(Assert.Single(seeds, value => value.ElementId == "i-medium-tank").InitialComponentToes).CurrentToe);
    }

    [Fact]
    public void FourthCcnnCandidatePreservesItsUnderfilledHeavyAaComponent()
    {
        var group = Cna1979GrazianiSourceFacts.FourthCcnnInitialChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.FourthCcnnCombatCandidates;
        var seeds = Cna1979GrazianiSourceFacts.CreateFourthCcnnToeCandidates("graziani");
        Assert.Equal("c4507", group.LocationId);
        Assert.Equal(12, group.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(10, 25, 10), group.Trucks);
        Assert.Equal(10, facts.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.DoesNotContain(facts, value => value.ElementId is "4-ccnn-hq" or "204-ccnn-engineer");
        var artillery = Assert.Single(facts, value => value.ElementId == "204-ccnn-artillery");
        Assert.Equal(12, artillery.MaximumCombinedToe);
        Assert.Equal(9, Assert.Single(artillery.ComponentToeLimits).MaximumToe);
        var components = Assert.Single(seeds, value => value.ElementId == artillery.ElementId).InitialComponentToes;
        Assert.Equal(6, Assert.Single(components, value => value.ComponentId.EndsWith(".75-18-gun-howitzer", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(3, Assert.Single(components, value => value.ComponentId.EndsWith(".105-28-gun", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(2, Assert.Single(components, value => value.ComponentId.EndsWith(".heavy-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.True(Assert.Single(artillery.Components, value => value.ComponentClassId == Cna1979GrazianiSourceFacts.AntiAirComponentClassId)
            .DefensiveRatingParenthesized);
    }

    [Fact]
    public void GrazianiDivisionCandidatesRetainErrataMembersAndPrintedInfantryStrengths()
    {
        var cirene = Cna1979GrazianiSourceFacts.CireneDivisionChartCandidate;
        var marmarica = Cna1979GrazianiSourceFacts.MarmaricaDivisionChartCandidate;
        Assert.Equal("c4120", cirene.LocationId);
        Assert.Equal("c3918", marmarica.LocationId);
        Assert.Equal(15, cirene.ElementIds.Count);
        Assert.Equal(14, marmarica.ElementIds.Count);
        Assert.Equal(new ContentSourceTruckQuantities(null, 45, 5), cirene.Trucks);
        Assert.Equal(new ContentSourceTruckQuantities(null, 40, 5), marmarica.Trucks);
        Assert.Contains("lxiii-light-tank", cirene.ElementIds);
        Assert.Contains("lxii-light-tank", marmarica.ElementIds);
        Assert.Contains(cirene.Origin.References,
            value => value.SourceId == Cna1979GrazianiSourceFacts.ErrataSourceId);

        var facts = Cna1979GrazianiSourceFacts.CireneMarmaricaInfantryCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateCireneMarmaricaInfantryToeSeeds("graziani");
        Assert.Equal(14, facts.Count);
        Assert.Equal(14, seeds.Count);
        Assert.All(facts, fact =>
        {
            Assert.Contains(fact.ElementId, cirene.ElementIds.Concat(marmarica.ElementIds));
            var component = Assert.Single(fact.Components);
            Assert.Equal(fact.ElementId.EndsWith("machine-gun", StringComparison.Ordinal) ? 2 : 7,
                component.MaximumToe);
            Assert.Contains(component.Origin.References,
                value => value.SourceId == Cna1979GrazianiSourceFacts.ErrataSourceId);
            var seed = Assert.Single(seeds, value => value.ElementId == fact.ElementId);
            Assert.Equal(component.MaximumToe, Assert.Single(seed.InitialComponentToes).CurrentToe);
        });
        var antitank = Cna1979GrazianiSourceFacts.CireneMarmaricaAntiTankCombatFacts;
        var antitankSeeds = Cna1979GrazianiSourceFacts.CreateCireneMarmaricaAntiTankToeSeeds("graziani");
        Assert.Equal(2, antitank.Count);
        Assert.All(antitank, fact =>
        {
            var component = Assert.Single(fact.Components);
            Assert.Equal(Cna1979GrazianiSourceFacts.AntiTankComponentClassId, component.ComponentClassId);
            Assert.Equal(2, Assert.Single(antitankSeeds, seed => seed.ElementId == fact.ElementId)
                .InitialComponentToes.Single().CurrentToe);
            Assert.NotNull(component.WeaponSourceProfile);
        });
        var artillery = Cna1979GrazianiSourceFacts.CreateCireneMarmaricaArtilleryToeSeeds("graziani");
        var artilleryFacts = Cna1979GrazianiSourceFacts.CireneMarmaricaArtilleryCombatFacts;
        Assert.Equal(2, artillery.Count);
        Assert.Equal(2, artilleryFacts.Count);
        Assert.All(artilleryFacts, facts =>
        {
            Assert.Equal(12, facts.MaximumCombinedToe);
            var gunLimit = Assert.Single(facts.ComponentToeLimits);
            Assert.Equal(9, gunLimit.MaximumToe);
            Assert.Equal(2, gunLimit.ComponentIds.Count);
            Assert.DoesNotContain($"{facts.ElementId}.light-aa", gunLimit.ComponentIds);
            var seed = Assert.Single(artillery, value => value.ElementId == facts.ElementId);
            Assert.Equal(9, seed.InitialComponentToes.Where(value => gunLimit.ComponentIds.Contains(value.ComponentId))
                .Sum(value => value.CurrentToe));
            Assert.Equal(12, seed.InitialComponentToes.Sum(value => value.CurrentToe));
            var aa = Assert.Single(facts.Components, value => value.ComponentId == $"{facts.ElementId}.light-aa");
            Assert.True(aa.DefensiveRatingParenthesized);
        });
        var cireneGuns = Assert.Single(artillery, seed => seed.ElementId == "45-artillery");
        Assert.Equal(6, Assert.Single(cireneGuns.InitialComponentToes,
            value => value.ComponentId == "45-artillery.75-18-gun-howitzer").CurrentToe);
        Assert.Equal(3, Assert.Single(cireneGuns.InitialComponentToes,
            value => value.ComponentId == "45-artillery.light-aa").CurrentToe);
        var marmaricaGuns = Assert.Single(artillery, seed => seed.ElementId == "44-artillery");
        Assert.Equal(6, Assert.Single(marmaricaGuns.InitialComponentToes,
            value => value.ComponentId == "44-artillery.75-27-gun").CurrentToe);
        Assert.Equal(3, Assert.Single(marmaricaGuns.InitialComponentToes,
            value => value.ComponentId == "44-artillery.105-28-gun").CurrentToe);
    }

    [Fact]
    public void GrazianiSourceFactsExposeTheBoundedNzAndCairoRecords()
    {
        var arrival = Cna1979GrazianiSourceFacts.NzFieldArtilleryArrival;
        var area = Cna1979GrazianiSourceFacts.CairoEntryArea;
        Assert.Equal("commonwealth", arrival.SideId);
        Assert.Equal(new ContentScenarioBoundary(2, 3), arrival.Arrival);
        Assert.Equal(Cna1979GrazianiSourceFacts.NzFieldArtilleryId, Assert.Single(arrival.ElementIds));
        Assert.Equal(area.AreaId, arrival.EntryAreaId);
        Assert.Collection(area.LocationIds,
            value => Assert.Equal("e1730", value), value => Assert.Equal("e1829", value),
            value => Assert.Equal("e1830", value), value => Assert.Equal("e1930", value),
            value => Assert.Equal("e1931", value));
        Assert.Equal(ContentOriginKind.SourceDerived, arrival.Origin.Kind);
        Assert.Equal(ContentOriginKind.SourceDerived, area.Origin.Kind);
    }

    [Fact]
    public void GrazianiNzArtilleryCanBeRepresentedWithoutSilentlyAdmittingIt()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var existing = Assert.Single(baseline.Scenarios);
        var source = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference("cna-commonwealth-charts", "4.43a"),
             new RuleReference("cna-commonwealth-charts", "oa-nz-p18")]);
        var arrival = new ContentScheduledArrival("graziani-nz-6-field-artillery", "commonwealth",
            new ContentScenarioBoundary(2, 3), ["nz-6-field-artillery"], "cairo", source);
        var scenario = new ContentScenario(existing.ScenarioId, existing.Start, existing.End,
            existing.InitialPlacements, [arrival], existing.Origin);
        var candidate = ContentTestData.Copy(baseline, scenarios: [scenario]);

        Assert.Equal(2, arrival.Arrival.GameTurn);
        Assert.Equal(3, arrival.Arrival.OperationStage);
        Assert.Equal("cairo", arrival.EntryAreaId);
        Assert.Equal("nz-6-field-artillery", Assert.Single(arrival.ElementIds));
        var issues = ContentPackValidator.Validate(candidate).Issues;
        Assert.Contains(issues, issue => issue.Code == "scenario.scheduled-arrivals-unsupported");
        Assert.Contains(issues, issue => issue.Code == "arrival.outside-scenario");
        Assert.Contains(issues, issue => issue.Code == "content.unknown-reference"
            && issue.Path.EndsWith("/elementIds/nz-6-field-artillery", StringComparison.Ordinal));
        Assert.Contains(issues, issue => issue.Code == "content.unknown-reference"
            && issue.Path.EndsWith("/references/cna-commonwealth-charts", StringComparison.Ordinal));
        Assert.Throws<InvalidContentPackException>(() => ContentPackSerializer.SerializeCanonical(candidate));
        Assert.Throws<InvalidOperationException>(() => ContentPackSerializer.SerializeDraftDefinition(candidate));
    }

    [Fact]
    public void EmptySchedulePreservesLegacyScenarioIdentityAndBytes()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var existing = Assert.Single(baseline.Scenarios);
        var same = new ContentScenario(existing.ScenarioId, existing.Start, existing.End,
            existing.InitialPlacements, [], existing.Origin);
        Assert.Equal(existing, same);
        Assert.Equal(existing.GetHashCode(), same.GetHashCode());
        Assert.Equal(ContentPackSerializer.SerializeCanonical(baseline),
            ContentPackSerializer.SerializeCanonical(ContentTestData.Copy(baseline, scenarios: [same])));
    }

    [Fact]
    public void LegacyParserRejectsUnrecognizedScheduleInsteadOfDroppingIt()
    {
        var baseline = ContentPackSerializer.SerializeCanonical(ContentTestData.CreateMinimalPack());
        var json = Encoding.UTF8.GetString(baseline).Replace("\"initialPlacements\":", "\"scheduledArrivals\":[],\"initialPlacements\":", StringComparison.Ordinal);
        Assert.False(ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(json)).IsSuccess);
        var grouped = Encoding.UTF8.GetString(baseline).Replace("\"initialPlacements\":", "\"initialGroups\":[],\"initialPlacements\":", StringComparison.Ordinal);
        Assert.False(ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(grouped)).IsSuccess);
    }

    [Fact]
    public void CurrentCampaignContentVersionCannotDiscardSchedule()
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        var legacy = artifact.Definition.LegacyDefinition;
        var existing = Assert.Single(legacy.Scenarios);
        var arrival = new ContentScheduledArrival("nz-artillery", "commonwealth", new(2, 3),
            ["nz-6-field-artillery"], "cairo", ContentTestData.Origin("arrival.nz"));
        var scenario = new ContentScenario(existing.ScenarioId, existing.Start, existing.End,
            existing.InitialPlacements, [arrival], existing.Origin);
        var modified = new ContentPackV6Definition(ContentTestData.Copy(legacy, scenarios: [scenario]),
            artifact.Definition.ElementCombatFacts, artifact.Definition.InitialPlacementCombatFacts,
            artifact.Definition.CapabilityProfileId);
        Assert.False(ContentPackV6Validator.Validate(modified).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(modified));
    }

    [Fact]
    public void CurrentCampaignContentVersionCannotDiscardParenthesizedRating()
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        var oldFacts = artifact.Definition.ElementCombatFacts.First(value => value.Components.Count > 0);
        var oldComponent = oldFacts.Components[0];
        var marked = new ContentCombatComponent(oldComponent.ComponentId, oldComponent.ComponentClassId,
            oldComponent.MaximumToe, oldComponent.DefensiveCloseAssaultRating, oldComponent.Origin,
            defensiveRatingParenthesized: true);
        var replacement = new ContentElementCombatFacts(oldFacts.ElementId, oldFacts.CombatClassificationId,
            [marked, .. oldFacts.Components.Skip(1)], oldFacts.Origin);
        var modified = new ContentPackV6Definition(artifact.Definition.LegacyDefinition,
            artifact.Definition.ElementCombatFacts.Select(value => value == oldFacts ? replacement : value),
            artifact.Definition.InitialPlacementCombatFacts, artifact.Definition.CapabilityProfileId);
        Assert.False(ContentPackV6Validator.Validate(modified).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(modified));
        var offensive = new ContentCombatComponent(oldComponent.ComponentId, oldComponent.ComponentClassId,
            oldComponent.MaximumToe, oldComponent.DefensiveCloseAssaultRating, oldComponent.Origin,
            offensiveCloseAssaultRating: 0);
        var offensiveFacts = new ContentElementCombatFacts(oldFacts.ElementId, oldFacts.CombatClassificationId,
            [offensive, .. oldFacts.Components.Skip(1)], oldFacts.Origin);
        var offensiveDefinition = new ContentPackV6Definition(artifact.Definition.LegacyDefinition,
            artifact.Definition.ElementCombatFacts.Select(value => value == oldFacts ? offensiveFacts : value),
            artifact.Definition.InitialPlacementCombatFacts, artifact.Definition.CapabilityProfileId);
        Assert.False(ContentPackV6Validator.Validate(offensiveDefinition).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(offensiveDefinition));
        var barrage = new ContentCombatComponent(oldComponent.ComponentId, oldComponent.ComponentClassId,
            oldComponent.MaximumToe, oldComponent.DefensiveCloseAssaultRating, oldComponent.Origin,
            barrageRating: 0);
        var barrageFacts = new ContentElementCombatFacts(oldFacts.ElementId, oldFacts.CombatClassificationId,
            [barrage, .. oldFacts.Components.Skip(1)], oldFacts.Origin);
        var barrageDefinition = new ContentPackV6Definition(artifact.Definition.LegacyDefinition,
            artifact.Definition.ElementCombatFacts.Select(value => value == oldFacts ? barrageFacts : value),
            artifact.Definition.InitialPlacementCombatFacts, artifact.Definition.CapabilityProfileId);
        Assert.False(ContentPackV6Validator.Validate(barrageDefinition).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(barrageDefinition));
        var weapon = Assert.Single(Cna1979GrazianiSourceFacts.NzFieldArtilleryCombatFacts.Components).WeaponSourceProfile!;
        var profiled = new ContentCombatComponent(oldComponent.ComponentId, oldComponent.ComponentClassId,
            oldComponent.MaximumToe, oldComponent.DefensiveCloseAssaultRating, oldComponent.Origin,
            weaponSourceProfile: weapon);
        var profiledFacts = new ContentElementCombatFacts(oldFacts.ElementId, oldFacts.CombatClassificationId,
            [profiled, .. oldFacts.Components.Skip(1)], oldFacts.Origin);
        var profiledDefinition = new ContentPackV6Definition(artifact.Definition.LegacyDefinition,
            artifact.Definition.ElementCombatFacts.Select(value => value == oldFacts ? profiledFacts : value),
            artifact.Definition.InitialPlacementCombatFacts, artifact.Definition.CapabilityProfileId);
        Assert.False(ContentPackV6Validator.Validate(profiledDefinition).IsValid);
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(profiledDefinition));
        var limitedFacts = new ContentElementCombatFacts(oldFacts.ElementId, oldFacts.CombatClassificationId,
            oldFacts.Components, oldFacts.Origin,
            componentToeLimits: [new("limit", [oldComponent.ComponentId], oldComponent.MaximumToe, oldFacts.Origin)]);
        var limitedDefinition = new ContentPackV6Definition(artifact.Definition.LegacyDefinition,
            artifact.Definition.ElementCombatFacts.Select(value => value == oldFacts ? limitedFacts : value),
            artifact.Definition.InitialPlacementCombatFacts, artifact.Definition.CapabilityProfileId);
        Assert.Contains(ContentPackV6Validator.Validate(limitedDefinition).Issues,
            issue => issue.Path.EndsWith("/componentToeLimits", StringComparison.Ordinal));
        Assert.Throws<InvalidContentPackException>(() => ContentPackV6Serializer.SerializeCanonical(limitedDefinition));
    }

    [Fact]
    public void ArrivalMembersAreImmutableDistinctAndCanonical()
    {
        var origin = ContentTestData.Origin("arrival.test");
        var members = new[] { "unit-b", "unit-a" };
        var arrival = new ContentScheduledArrival("arrival-a", "commonwealth", new(2, 3), members, null, origin);
        members[0] = "changed";
        Assert.Collection(arrival.ElementIds, value => Assert.Equal("unit-a", value), value => Assert.Equal("unit-b", value));
        Assert.Null(arrival.EntryAreaId);
        Assert.Throws<ArgumentException>(() => new ContentScheduledArrival("arrival-a", "commonwealth",
            new(2, 3), ["unit-a", "unit-a"], null, origin));
        Assert.Throws<ArgumentException>(() => new ContentScheduledArrival("arrival-a", "commonwealth",
            new(2, 3), [], null, origin));
    }

    [Fact]
    public void GrazianiTurnTwoArrivalMayContainSeveralSeparateGroups()
    {
        var source = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference("cna-commonwealth-charts", "4.43a")]);
        var slot = new ContentScenarioBoundary(2, 3);
        var groups = new[]
        {
            Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentArrival,
            Cna1979GrazianiSourceFacts.NzFieldArtilleryArrival,
            Cna1979GrazianiSourceFacts.SecondRoyalTankRegimentArrival,
        };
        var scenario = new ContentScenario("graziani", new(1, 1), new(6, 3), [], groups, source);
        Assert.Collection(scenario.ScheduledArrivals,
            value => Assert.Equal("graziani-2-rtr", value.GroupId),
            value => Assert.Equal("graziani-7-rtr", value.GroupId),
            value => Assert.Equal("graziani-nz-6-field-artillery", value.GroupId));
        Assert.All(scenario.ScheduledArrivals, value => Assert.Equal(slot, value.Arrival));
    }

    [Fact]
    public void GrazianiLaterSingleUnitArrivalsKeepPrintedBoundaries()
    {
        var artillery = Cna1979GrazianiSourceFacts.FirstFieldArtilleryArrival;
        var hussars = Cna1979GrazianiSourceFacts.ThirdHussarsArrival;
        Assert.Equal(new ContentScenarioBoundary(3, 3), artillery.Arrival);
        Assert.Equal(Cna1979GrazianiSourceFacts.FirstFieldArtilleryId, Assert.Single(artillery.ElementIds));
        Assert.Equal(new ContentScenarioBoundary(4, 3), hussars.Arrival);
        Assert.Equal(Cna1979GrazianiSourceFacts.ThirdHussarsId, Assert.Single(hussars.ElementIds));
        Assert.All(new[] { artillery, hussars }, arrival =>
        {
            Assert.Equal("commonwealth", arrival.SideId);
            Assert.Equal(Cna1979GrazianiSourceFacts.CairoAreaId, arrival.EntryAreaId);
            Assert.Equal(ContentOriginKind.SourceDerived, arrival.Origin.Kind);
        });
    }

    [Fact]
    public void GrazianiPolishBrigadeArrivalPreservesEightDistinctOaUnits()
    {
        var arrival = Cna1979GrazianiSourceFacts.PolishBrigadeArrival;
        Assert.Equal(new ContentScenarioBoundary(1, 3), arrival.Arrival);
        Assert.Equal("commonwealth", arrival.SideId);
        Assert.Equal(Cna1979GrazianiSourceFacts.CairoAreaId, arrival.EntryAreaId);
        Assert.Equal(8, arrival.ElementIds.Count);
        Assert.Equal(8, arrival.ElementIds.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("polish-brigade-hq", arrival.ElementIds);
        Assert.Contains("polish-kopanski-machine-gun", arrival.ElementIds);
        Assert.Contains("polish-1-artillery", arrival.ElementIds);
        Assert.Contains(arrival.Origin.References, reference => reference.Locator == "oa-polish-p21");
    }

    [Fact]
    public void SixthNzBrigadeCandidatePreservesChartTimingAndTrucks()
    {
        var candidate = Cna1979GrazianiSourceFacts.SixthNzBrigadeChartCandidate;
        Assert.Equal(new ContentScenarioBoundary(6, 3), candidate.Arrival);
        Assert.Equal(4, candidate.ElementIds.Count);
        Assert.Contains("6-nz-brigade-hq", candidate.ElementIds);
        Assert.Contains("24-nz-infantry", candidate.ElementIds);
        Assert.Contains("25-nz-infantry", candidate.ElementIds);
        Assert.Contains("26-nz-infantry", candidate.ElementIds);
        Assert.Equal(new ContentSourceTruckQuantities(25, 85, 5), candidate.Trucks);
        Assert.Contains(candidate.Origin.References, reference => reference.Locator == "oa-nz-p18");
    }

    [Fact]
    public void TankComponentSourceFactsKeepArmorAndPrintedDefensiveRatings()
    {
        var second = Assert.Single(Cna1979GrazianiSourceFacts.SecondRoyalTankRegimentCombatFacts.Components);
        var hussars = Assert.Single(Cna1979GrazianiSourceFacts.ThirdHussarsCombatFacts.Components);
        Assert.Equal(Cna1979GrazianiSourceFacts.ArmorComponentClassId, second.ComponentClassId);
        Assert.Equal(10, second.MaximumToe);
        Assert.Equal(3, second.DefensiveCloseAssaultRating);
        Assert.Equal(Cna1979GrazianiSourceFacts.ArmorComponentClassId, hussars.ComponentClassId);
        Assert.Equal(10, hussars.MaximumToe);
        Assert.Equal(2, hussars.DefensiveCloseAssaultRating);
        Assert.False(Cna1979Combat.IsSupportedComponentClassId(second.ComponentClassId));
        Assert.False(Cna1979Combat.IsSupportedComponentClassId(hussars.ComponentClassId));
        var fieldGun = Assert.Single(Cna1979GrazianiSourceFacts.FirstFieldArtilleryCombatFacts.Components);
        Assert.Equal(Cna1979GrazianiSourceFacts.ArtilleryComponentClassId, fieldGun.ComponentClassId);
        Assert.Equal(6, fieldGun.MaximumToe);
        Assert.Equal(1, fieldGun.DefensiveCloseAssaultRating);
        Assert.False(Cna1979Combat.IsSupportedComponentClassId(fieldGun.ComponentClassId));
        var seventh = Cna1979GrazianiSourceFacts.SeventhRoyalTankMatildaMkIiCandidate;
        Assert.Equal(Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentId, seventh.ElementId);
        Assert.Equal((10, 4), (Assert.Single(seventh.Components).MaximumToe, Assert.Single(seventh.Components).DefensiveCloseAssaultRating));
        Assert.DoesNotContain(Cna1979GrazianiSourceFacts.CommonwealthArrivalCombatFacts, value => value.ElementId == seventh.ElementId);
    }

    [Fact]
    public void InfantrySourceFactsRetainDifferentOaMaximumsWithoutAssumingArrivalStrength()
    {
        var facts = Cna1979GrazianiSourceFacts.ArrivingInfantryCombatFacts;
        Assert.Equal(10, facts.Count);
        var polish = Assert.Single(facts, value => value.ElementId == "polish-1-infantry");
        var indian = Assert.Single(facts, value => value.ElementId == "1-royal-sussex");
        var nz = Assert.Single(facts, value => value.ElementId == "24-nz-infantry");
        var machineGun = Assert.Single(facts, value => value.ElementId == "polish-kopanski-machine-gun");
        Assert.Equal((5, 2), (Assert.Single(polish.Components).MaximumToe, Assert.Single(polish.Components).DefensiveCloseAssaultRating));
        Assert.Equal((6, 2), (Assert.Single(indian.Components).MaximumToe, Assert.Single(indian.Components).DefensiveCloseAssaultRating));
        Assert.Equal((6, 2), (Assert.Single(nz.Components).MaximumToe, Assert.Single(nz.Components).DefensiveCloseAssaultRating));
        Assert.Equal((2, 6), (Assert.Single(machineGun.Components).MaximumToe, Assert.Single(machineGun.Components).DefensiveCloseAssaultRating));
        Assert.All(facts, value => Assert.Contains(value.Origin.References, reference => reference.Locator == "4.46a"));
        var members = new[]
        {
            Cna1979GrazianiSourceFacts.PolishBrigadeArrival,
            Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival,
            Cna1979GrazianiSourceFacts.SixthNzBrigadeChartCandidate,
        }.SelectMany(value => value.ElementIds).ToHashSet(StringComparer.Ordinal);
        Assert.All(facts, value => Assert.Contains(value.ElementId, members));
        Assert.Equal(facts.Count, facts.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PolishSupportFactsKeepClassesAndCapacityDistinctFromArrivalStrength()
    {
        var cavalry = Assert.Single(Cna1979GrazianiSourceFacts.PolishCavalryCombatFacts.Components);
        var antitank = Assert.Single(Cna1979GrazianiSourceFacts.PolishAntiTankCombatFacts.Components);
        var artillery = Assert.Single(Cna1979GrazianiSourceFacts.PolishArtilleryCombatFacts.Components);
        Assert.Equal((Cna1979GrazianiSourceFacts.ReconnaissanceComponentClassId, 4, 2),
            (cavalry.ComponentClassId, cavalry.MaximumToe, cavalry.DefensiveCloseAssaultRating));
        Assert.Null(cavalry.WeaponSourceProfile);
        Assert.Equal((Cna1979GrazianiSourceFacts.AntiTankComponentClassId, 2, 1),
            (antitank.ComponentClassId, antitank.MaximumToe, antitank.DefensiveCloseAssaultRating));
        Assert.Equal((Cna1979GrazianiSourceFacts.ArtilleryComponentClassId, 4, 1),
            (artillery.ComponentClassId, artillery.MaximumToe, artillery.DefensiveCloseAssaultRating));
        Assert.All(new[] { cavalry, antitank, artillery }, component =>
            Assert.False(Cna1979Combat.IsSupportedComponentClassId(component.ComponentClassId)));
    }

    [Fact]
    public void SourceArrivalSeedsDistinguishNormalAndUnderstrengthOaEntries()
    {
        var seeds = Cna1979GrazianiSourceFacts.CreateCommonwealthArrivalToeSeeds("graziani");
        Assert.Equal(21, seeds.Count);
        Assert.All(seeds, seed => Assert.Equal("graziani", seed.ScenarioId));
        int CurrentToe(string id) => Assert.Single(Assert.Single(seeds, value => value.ElementId == id).InitialComponentToes).CurrentToe;
        Assert.Equal(1, CurrentToe("polish-kopanski-machine-gun"));
        Assert.Equal(1, CurrentToe("polish-1-antitank"));
        Assert.Equal(5, CurrentToe("polish-1-infantry"));
        Assert.Equal(6, CurrentToe("1-royal-sussex"));
        Assert.Equal(6, CurrentToe("24-nz-infantry"));
        Assert.Equal(10, CurrentToe(Cna1979GrazianiSourceFacts.SecondRoyalTankRegimentId));
        Assert.Equal(6, CurrentToe(Cna1979GrazianiSourceFacts.NzFieldArtilleryId));
        Assert.Empty(Assert.Single(seeds, value => value.ElementId == "polish-brigade-hq").InitialComponentToes);
        Assert.Equal(10, CurrentToe(Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentId));
        Assert.All(seeds.SelectMany(seed => seed.InitialComponentToes), toe =>
            Assert.Contains(toe.Origin.References, reference =>
                reference.SourceId == Cna1979GrazianiSourceFacts.LandRulesSourceId && reference.Locator == "4.45"));
        Assert.Throws<ArgumentException>(() => Cna1979GrazianiSourceFacts.CreateCommonwealthArrivalToeSeeds(""));
    }

    [Fact]
    public void InitialPlacementCandidatesPreserveSourceColocation()
    {
        var placements = Cna1979GrazianiSourceFacts.CommonwealthInitialPlacementCandidates;
        Assert.Equal(12, placements.Count);
        Assert.Equal("c4131", Assert.Single(placements, value => value.ElementId == "2-scots-guards").LocationId);
        Assert.Equal("c4131", Assert.Single(placements, value => value.ElementId == "31-field-artillery").LocationId);
        Assert.Equal("c3520", Assert.Single(placements, value => value.ElementId == "1-rtr").LocationId);
        Assert.Equal("c3020", Assert.Single(placements, value => value.ElementId == "11-hussars").LocationId);
        Assert.All(placements, value => Assert.Contains(value.Origin.References, reference =>
            reference.SourceId == Cna1979GrazianiSourceFacts.ScenariosSourceId && reference.Locator == "60.41"));
        Assert.Equal(4, placements.Count(value => value.LocationId == "c3922"));
    }

    [Fact]
    public void TobrukOaLandSubsetsKeepDistinctCountersWithoutFlatteningTheGarrison()
    {
        var infantry = Cna1979GrazianiSourceFacts.TobrukInfantryChartSubset;
        var artillery = Cna1979GrazianiSourceFacts.TobrukArtilleryAndAaChartSubset;
        Assert.Equal("c4807", infantry.LocationId);
        Assert.Equal("c4807", artillery.LocationId);
        Assert.Equal("axis", infantry.SideId);
        Assert.Equal("axis", artillery.SideId);
        Assert.Equal(13, infantry.ElementIds.Count);
        Assert.Equal(9, artillery.ElementIds.Count);
        Assert.Empty(infantry.ElementIds.Intersect(artillery.ElementIds, StringComparer.Ordinal));
        Assert.Contains("31-gaf-sdic-hq", infantry.ElementIds);
        Assert.Contains("tobruk-light-aa-battalion", artillery.ElementIds);
        Assert.DoesNotContain("san-giorgio", artillery.ElementIds);
        Assert.Contains(artillery.Origin.References, reference => reference.Locator == "oa-tobruk-p12");
    }

    [Fact]
    public void TobrukSupportSeedsKeepMixedGunAndAaQuantities()
    {
        var group = Cna1979GrazianiSourceFacts.TobrukArtilleryAndAaChartSubset;
        var seeds = Cna1979GrazianiSourceFacts.CreateTobrukSupportToeSeeds("graziani");
        Assert.Equal(9, seeds.Count);
        Assert.Equal(group.ElementIds, seeds.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        var second = Assert.Single(seeds, value => value.ElementId == "2-tobruk-coastal-defense-artillery");
        Assert.Equal(6, Assert.Single(second.InitialComponentToes, value => value.ComponentId.EndsWith(".100-17-howitzer", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(6, Assert.Single(second.InitialComponentToes, value => value.ComponentId.EndsWith(".i-light-aa", StringComparison.Ordinal)).CurrentToe);
        var fourth = Assert.Single(seeds, value => value.ElementId == "4-tobruk-coastal-defense-artillery");
        Assert.Equal(6, Assert.Single(fourth.InitialComponentToes, value => value.ComponentId.EndsWith(".105-28-gun", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(4, Assert.Single(fourth.InitialComponentToes, value => value.ComponentId.EndsWith(".i-heavy-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.All(seeds.SelectMany(value => value.InitialComponentToes), toe => Assert.Contains(toe.Origin.References,
            reference => reference.SourceId == Cna1979GrazianiSourceFacts.AxisChartsSourceId && reference.Locator == "oa-tobruk-p12"));
        var facts = Cna1979GrazianiSourceFacts.TobrukSupportCombatFacts;
        Assert.Equal(4, facts.Count);
        var seventh = Assert.Single(facts, value => value.ElementId == "7-tobruk-artiglieria");
        Assert.Equal(15, Assert.Single(seventh.Components).BarrageRating);
        Assert.True(Assert.Single(Assert.Single(facts, value => value.ElementId == "tobruk-light-aa-battalion").Components).DefensiveRatingParenthesized);
        Assert.DoesNotContain(facts, value => value.ElementId == "2-tobruk-coastal-defense-artillery");
    }

    [Fact]
    public void TobrukInfantryUsesItalianIdChartAndLeavesConditionalHeadquartersUnmodeled()
    {
        var group = Cna1979GrazianiSourceFacts.TobrukInfantryChartSubset;
        var facts = Cna1979GrazianiSourceFacts.TobrukInfantryCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateTobrukInfantryToeSeeds("graziani");
        Assert.Equal(10, facts.Count);
        Assert.Equal(10, seeds.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.DoesNotContain(facts, value => value.ElementId == "31-gaf-sdic-hq");
        var first = Assert.Single(facts, value => value.ElementId == "1-31-gaf-sdic");
        var second = Assert.Single(facts, value => value.ElementId == "1-32-gaf-sdic");
        Assert.Equal((5, 1), (Assert.Single(first.Components).MaximumToe, Assert.Single(first.Components).DefensiveCloseAssaultRating));
        Assert.Equal((6, 1), (Assert.Single(second.Components).MaximumToe, Assert.Single(second.Components).DefensiveCloseAssaultRating));
        Assert.Equal(5, Assert.Single(Assert.Single(seeds, value => value.ElementId == first.ElementId).InitialComponentToes).CurrentToe);
        Assert.Equal(6, Assert.Single(Assert.Single(seeds, value => value.ElementId == second.ElementId).InitialComponentToes).CurrentToe);
        Assert.All(facts, value => Assert.Contains(value.Origin.References, reference =>
            reference.SourceId == Cna1979GrazianiSourceFacts.AxisChartsSourceId && reference.Locator == "4.46b"));
    }

    [Fact]
    public void BardiaChartCandidatePreservesRosterAndItalianInfantryStrengths()
    {
        var group = Cna1979GrazianiSourceFacts.BardiaGarrisonChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.BardiaInfantryCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateBardiaInfantryToeSeeds("graziani");
        Assert.Equal("c4321", group.LocationId);
        Assert.Equal(11, group.ElementIds.Count);
        Assert.Contains("30-gaf-sdic-hq", group.ElementIds);
        Assert.Equal(4, facts.Count);
        Assert.Equal(4, seeds.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        var infantry = Assert.Single(facts, value => value.ElementId == "1-30-gaf-sdic");
        var machineGun = Assert.Single(facts, value => value.ElementId == "vec-machine-gun-battalion");
        Assert.Equal((7, 1), (Assert.Single(infantry.Components).MaximumToe, Assert.Single(infantry.Components).DefensiveCloseAssaultRating));
        Assert.Equal((3, 3), (Assert.Single(machineGun.Components).MaximumToe, Assert.Single(machineGun.Components).DefensiveCloseAssaultRating));
        Assert.Equal(7, Assert.Single(Assert.Single(seeds, value => value.ElementId == infantry.ElementId).InitialComponentToes).CurrentToe);
        Assert.Equal(3, Assert.Single(Assert.Single(seeds, value => value.ElementId == machineGun.ElementId).InitialComponentToes).CurrentToe);
        Assert.Contains(group.Origin.References, reference => reference.Locator == "oa-bardia-p13");
        Assert.Contains(group.Origin.References, reference => reference.SourceId == Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId);
    }

    [Fact]
    public void BardiaSupportSeedsRetainMixedWeaponsAndUnmodeledRules()
    {
        var group = Cna1979GrazianiSourceFacts.BardiaGarrisonChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.BardiaSupportCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateBardiaSupportToeSeeds("graziani");
        Assert.Equal(5, facts.Count);
        Assert.Equal(6, seeds.Count);
        Assert.All(seeds, value => Assert.Contains(value.ElementId, group.ElementIds));
        var eighth = Assert.Single(facts, value => value.ElementId == "8-gaf-artillery");
        Assert.Equal((9, 0), (Assert.Single(eighth.Components).MaximumToe, Assert.Single(eighth.Components).DefensiveCloseAssaultRating));
        Assert.True(Assert.Single(Assert.Single(facts, value => value.ElementId == "17-bardia-antiair").Components).DefensiveRatingParenthesized);
        var ninth = Assert.Single(seeds, value => value.ElementId == "9-gaf-artillery");
        Assert.Equal(4, Assert.Single(ninth.InitialComponentToes, value => value.ComponentId.EndsWith(".100-17-howitzer", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(2, Assert.Single(ninth.InitialComponentToes, value => value.ComponentId.EndsWith(".75-27-gun", StringComparison.Ordinal)).CurrentToe);
        Assert.Equal(3, Assert.Single(seeds, value => value.ElementId == "5-bardia-coastal-defense-artillery").InitialComponentToes.Count);
        var ninthFacts = Assert.Single(facts, value => value.ElementId == "9-gaf-artillery");
        Assert.Equal(9, ninthFacts.MaximumCombinedToe);
        var gun = Assert.Single(ninthFacts.Components, value => value.ComponentId.EndsWith(".75-27-gun", StringComparison.Ordinal));
        Assert.Equal(ContentPrintedRatingKind.Unknown, gun.WeaponSourceProfile!.AntiArmor.Kind);
    }

    [Fact]
    public void GiarabubFactsKeepUnderstrengthAndConditionalAaSeparate()
    {
        var group = Cna1979GrazianiSourceFacts.GiarabubGarrisonChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.GiarabubCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateGiarabubToeSeeds("graziani");
        Assert.Equal("c1014", group.LocationId);
        Assert.Equal(6, group.ElementIds.Count);
        Assert.Equal(6, facts.Count);
        Assert.Equal(6, seeds.Count);
        Assert.All(facts, value => Assert.Contains(value.ElementId, group.ElementIds));
        Assert.True(Assert.Single(Assert.Single(facts, value => value.ElementId == "giarabub-light-aa").Components).DefensiveRatingParenthesized);
        var mg = Assert.Single(facts, value => value.ElementId == "giarabub-machine-gun");
        Assert.Equal((6, 6), (Assert.Single(mg.Components).MaximumToe, Assert.Single(mg.Components).DefensiveCloseAssaultRating));
        int Toe(string id) => Assert.Single(Assert.Single(seeds, value => value.ElementId == id).InitialComponentToes).CurrentToe;
        Assert.Equal(4, Toe("giarabub-machine-gun"));
        Assert.Equal(2, Toe("giarabub-antitank"));
        Assert.Equal(1, Toe("giarabub-artillery"));
        Assert.Equal(3, Toe("giarabub-light-aa"));
        Assert.All(facts, value => Assert.Contains(value.Origin.References, reference => reference.Locator == "4.46b"));
    }

    [Fact]
    public void MinorGarrisonFactsRespectSingleCountersAndUnderstrengthRows()
    {
        var placements = Cna1979GrazianiSourceFacts.AxisMinorGarrisonPlacementCandidates;
        var facts = Cna1979GrazianiSourceFacts.AxisMinorGarrisonCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateAxisMinorGarrisonToeSeeds("graziani");
        Assert.Equal(7, placements.Count);
        Assert.Equal(7, facts.Count);
        Assert.Equal(7, seeds.Count);
        Assert.Equal("c3019", Assert.Single(placements, value => value.ElementId == "fort-maddalena-garrison").LocationId);
        Assert.Equal("a4829", Assert.Single(placements, value => value.ElementId == "benina-aa-battery").LocationId);
        Assert.Equal("a4130", Assert.Single(placements, value => value.ElementId == "soluch-antitank-company").LocationId);
        var soluchArtillery = Assert.Single(placements, value => value.ElementId == "soluch-artillery-regiment");
        Assert.Equal("a4130", soluchArtillery.LocationId);
        Assert.Contains(soluchArtillery.Origin.References,
            value => value.SourceId == Cna1979GrazianiSourceFacts.AxisCounterSheetSourceId);
        Assert.True(Assert.Single(Assert.Single(facts, value => value.ElementId == "benina-aa-battery").Components).DefensiveRatingParenthesized);
        var fort = Assert.Single(facts, value => value.ElementId == "fort-maddalena-garrison");
        Assert.Equal((3, 3), (Assert.Single(fort.Components).MaximumToe, Assert.Single(fort.Components).DefensiveCloseAssaultRating));
        int Toe(string id) => Assert.Single(Assert.Single(seeds, value => value.ElementId == id).InitialComponentToes).CurrentToe;
        Assert.Equal(2, Toe("fort-maddalena-garrison"));
        Assert.Equal(2, Toe("mechili-garrison"));
        Assert.Equal(1, Toe("scheferzen-garrison"));
        Assert.Equal(2, Toe("benina-aa-battery"));
        Assert.Equal(2, Toe("soluch-antitank-company"));
        Assert.Equal(12, Toe("soluch-artillery-regiment"));
        Assert.All(placements, value => Assert.Contains(value.Origin.References, reference => reference.Locator == "oa-minor-garrisons-p14"));
    }

    [Fact]
    public void DernaRetainsOneTerritorialCounterAndPrintedCoastalStrengths()
    {
        var group = Cna1979GrazianiSourceFacts.DernaGarrisonChartCandidate;
        var facts = Cna1979GrazianiSourceFacts.DernaTerritorialCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateDernaToeSeeds("graziani");
        Assert.Equal("b5925", group.LocationId);
        Assert.Equal(4, group.ElementIds.Count);
        Assert.Equal(4, seeds.Count);
        Assert.Equal("54-territorial-infantry", facts.ElementId);
        Assert.True(Assert.Single(Cna1979GrazianiSourceFacts.DernaAntiAirCombatFacts.Components).DefensiveRatingParenthesized);
        Assert.Equal((10, 1), (Assert.Single(facts.Components).MaximumToe, Assert.Single(facts.Components).DefensiveCloseAssaultRating));
        int Toe(string id) => Assert.Single(Assert.Single(seeds, value => value.ElementId == id).InitialComponentToes).CurrentToe;
        Assert.Equal(10, Toe("54-territorial-infantry"));
        Assert.Equal(5, Toe("4-derna-coastal-defense-artillery"));
        Assert.Equal(2, Toe("6-derna-coastal-defense-artillery"));
        Assert.Equal(6, Toe("15-derna-coastal-defense-aa"));
        Assert.All(seeds, value => Assert.Contains(value.ElementId, group.ElementIds));
    }

    [Fact]
    public void InitialUnitFactsAndSeedsMatchSourcePlacementsAndUnderstrengthRows()
    {
        var placements = Cna1979GrazianiSourceFacts.CommonwealthInitialPlacementCandidates;
        var facts = Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts;
        var seeds = Cna1979GrazianiSourceFacts.CreateCommonwealthInitialToeSeeds("graziani");
        Assert.Equal(12, facts.Count);
        Assert.Equal(12, seeds.Count);
        Assert.Equal(placements.Select(value => value.ElementId).Order(StringComparer.Ordinal),
            facts.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        Assert.Equal(placements.Select(value => value.ElementId).Order(StringComparer.Ordinal),
            seeds.Select(value => value.ElementId).Order(StringComparer.Ordinal));
        var rtr = Assert.Single(facts, value => value.ElementId == "1-rtr");
        var hussars = Assert.Single(facts, value => value.ElementId == "11-hussars");
        var antitank = Assert.Single(facts, value => value.ElementId == "3-royal-horse-artillery-antitank");
        Assert.Equal(10, Assert.Single(rtr.Components).MaximumToe);
        Assert.Equal(8, Assert.Single(hussars.Components).MaximumToe);
        Assert.Equal(8, Assert.Single(antitank.Components).MaximumToe);
        int Toe(string id) => Assert.Single(Assert.Single(seeds, value => value.ElementId == id).InitialComponentToes).CurrentToe;
        Assert.Equal(7, Toe("1-rtr"));
        Assert.Equal(6, Toe("11-hussars"));
        Assert.Equal(6, Toe("3-royal-horse-artillery-antitank"));
        Assert.Equal(3, Toe("1-royal-northumberland-fusiliers"));
        Assert.All(seeds.SelectMany(seed => seed.InitialComponentToes), toe => Assert.Contains(toe.Origin.References,
            reference => reference.SourceId == Cna1979GrazianiSourceFacts.LandRulesSourceId && reference.Locator == "4.45"));
    }

    [Fact]
    public void ValidatorReportsConflictingScheduleMembershipAndOwnership()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var existing = Assert.Single(baseline.Scenarios);
        var origin = ContentTestData.Origin("arrival.test");
        var first = new ContentScheduledArrival("arrival-a", "commonwealth", new(1, 2), ["axis-element"], null, origin);
        var second = new ContentScheduledArrival("arrival-a", "invalid-side", new(1, 3), ["axis-element"], null, origin);
        var scenario = new ContentScenario(existing.ScenarioId, existing.Start, existing.End,
            existing.InitialPlacements, [first, second], existing.Origin);
        var issues = ContentPackValidator.Validate(ContentTestData.Copy(baseline, scenarios: [scenario])).Issues;
        Assert.Contains(issues, value => value.Code == "arrival.duplicate-group");
        Assert.Contains(issues, value => value.Code == "arrival.invalid-side");
        Assert.Contains(issues, value => value.Code == "arrival.duplicate-element");
        Assert.Contains(issues, value => value.Code == "arrival.initially-placed");
        Assert.Contains(issues, value => value.Code == "arrival.element-owner-or-placement");
    }

    [Fact]
    public void LegacyFormatCannotSilentlyDropEntryAreas()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var existing = Assert.Single(baseline.Scenarios);
        var area = new ContentLocationArea("cairo", ["west"], ContentTestData.Origin("fixture.area"));
        var scenario = new ContentScenario(existing.ScenarioId, existing.Start, existing.End,
            existing.InitialPlacements, [], [area], existing.Origin);
        var candidate = ContentTestData.Copy(baseline, scenarios: [scenario]);
        Assert.Contains(ContentPackValidator.Validate(candidate).Issues, value => value.Code == "scenario.entry-areas-unsupported");
        Assert.Throws<InvalidContentPackException>(() => ContentPackSerializer.SerializeCanonical(candidate));
        Assert.Throws<InvalidOperationException>(() => ContentPackSerializer.SerializeDraftDefinition(candidate));
    }
}
