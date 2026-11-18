using Cna.Core.Rules;

namespace Cna.Core.Content;

// Partial source-linked facts only: not a complete scenario or an admission catalog.
public static class Cna1979GrazianiSourceFacts
{
    public const string CommonwealthChartsSourceId = "spi-1979-commonwealth-charts";
    public const string AxisChartsSourceId = "spi-1979-axis-charts";
    public const string AxisCounterSheetSourceId = "spi-1979-axis-counter-section-2";
    public const string ErrataSourceId = "spi-1979-september-errata";
    public const string LandRulesSourceId = "spi-1979-land-rules";
    public const string ScenariosSourceId = "spi-1979-scenarios";
    public const string AirLogisticsSourceId = "spi-1979-air-logistics";
    public const string NzFieldArtilleryId = "nz-6-field-artillery";
    public const string SecondRoyalTankRegimentId = "2-rtr";
    public const string SeventhRoyalTankRegimentId = "7-rtr";
    public const string SeventhRoyalTankMatildaComponentId = "7-rtr.matilda";
    public const string FirstFieldArtilleryId = "1-field-artillery";
    public const string ThirdHussarsId = "3-kings-own-hussars";
    public const string CairoAreaId = "cairo";
    public const string NzFieldArtilleryComponentId = "nz-6-field-artillery.18-25-pounder";
    public const string ArtilleryComponentClassId = "land.combat-component.artillery";
    public const string ArmorComponentClassId = Cna1979Combat.ArmorComponentClassId;
    public const string AntiTankComponentClassId = Cna1979Combat.AntiTankComponentClassId;
    public const string ReconnaissanceComponentClassId = Cna1979Combat.ReconnaissanceComponentClassId;
    public const string AntiAirComponentClassId = "land.combat-component.anti-air";
    public const string AntiAirClassificationId = "land.combat-classification.anti-air";
    // RUL-GRA-26: both CCNN division HQs (1-ccnn-hq, 4-ccnn-hq) carry one
    // artillery TOE point from deployment (Axis OA p.17/p.18 "N" plus
    // characteristics ID 'g', OA key p.4 §4.44b, characteristics p.26 §4.46b),
    // but no checked source names its weapon. This sentinel is a deliberate,
    // distinct "unknown" component type - never null (that is the readiness
    // gap this ruling resolves) and never one of the real weapon component
    // ids (e.g. the Legion HQs' "65-17-gun") that CampaignSourceGroupedZoc.
    // TryDefensiveCloseAssaultRating or any other componentType-keyed consumer
    // could mistake for a rated, usable gun. It has no case in that lookup, so
    // a member carrying it correctly falls through to "unknown, don't guess a
    // rating" exactly like any other unmatched (groupId, elementId,
    // componentType, toe) combination.
    public const string CcnnHqUnidentifiedArtilleryComponentType = "unidentified-artillery";

    private static ContentPrintedRating N(int value) => ContentPrintedRating.Number(value);
    private static ContentPrintedRating P(int value) => ContentPrintedRating.Parenthesized(value);
    private static ContentPrintedRating Dash => ContentPrintedRating.Dash;
    private static ContentWeaponSourceProfile Weapon(int cp, ContentPrintedRating aa, ContentPrintedRating antiArmor,
        ContentPrintedRating vulnerability, ContentPrintedRating armor, int fuel, string breakdown) =>
        new(N(cp), aa, antiArmor, vulnerability, armor, N(fuel), breakdown);

    private static readonly ContentWeaponSourceProfile Commonwealth1825Pounder = Weapon(15, Dash, N(2), N(5), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Commonwealth18Pounder = Weapon(15, Dash, P(2), N(4), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Commonwealth2Pounder = Weapon(15, Dash, N(4), N(2), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Commonwealth60Pounder = Weapon(15, Dash, N(1), N(7), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Commonwealth37Howitzer = Weapon(15, Dash, N(0), N(3), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile CommonwealthA9 = Weapon(25, N(1), N(3), Dash, N(1), 2, "1R");
    private static readonly ContentWeaponSourceProfile CommonwealthA10 = Weapon(15, N(0), N(3), Dash, N(2), 2, "1R");
    private static readonly ContentWeaponSourceProfile CommonwealthA13 = Weapon(30, N(0), N(3), Dash, N(2), 2, "1R");
    private static readonly ContentWeaponSourceProfile CommonwealthMarkVi = Weapon(35, N(1), N(0), Dash, N(1), 1, "0");
    private static readonly ContentWeaponSourceProfile CommonwealthMatildaIi = Weapon(15, N(0), N(3), Dash, N(6), 3, "1R");
    private static readonly ContentWeaponSourceProfile Italian4732 = Weapon(15, Dash, N(4), N(2), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Italian6517 = Weapon(15, Dash, N(0), N(3), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Italian7518 = Weapon(15, Dash, N(0), N(5), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Italian10017 = Weapon(15, Dash, N(0), N(5), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Italian7527 = Weapon(15, Dash, ContentPrintedRating.Unknown, N(4), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile ItalianCv33 = Weapon(25, N(0), N(0), Dash, N(1), 1, "2R");
    private static readonly ContentWeaponSourceProfile ItalianM1139 = Weapon(20, N(1), N(2), Dash, N(2), 2, "1R");
    private static readonly ContentWeaponSourceProfile Italian10528 = Weapon(15, Dash, N(1), N(7), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Italian149Vichy = Weapon(15, Dash, N(1), N(5), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile Italian14913 = Weapon(15, Dash, N(0), N(5), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile ItalianLightAa = Weapon(15, N(1), N(2), N(2), Dash, 1, "-");
    private static readonly ContentWeaponSourceProfile ItalianHeavyAa = Weapon(15, N(2), P(5), N(2), Dash, 1, "-");

    // This source record is not an admission capability: the current combat engine
    // rejects artillery components, and V5 does not represent the full gun profile.
    public static ContentElementCombatFacts NzFieldArtilleryCombatFacts { get; } = new(
        NzFieldArtilleryId, Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent(NzFieldArtilleryComponentId, ArtilleryComponentClassId, 6, 1,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-nz-p18"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 1, barrageRating: 8, weaponSourceProfile: Commonwealth1825Pounder)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-nz-p18")]));

    public static ContentElementCombatFacts SecondRoyalTankRegimentCombatFacts { get; } = new(
        SecondRoyalTankRegimentId, Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent("2-rtr.a13-cruiser", ArmorComponentClassId, 10, 3,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 2, weaponSourceProfile: CommonwealthA13)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-british-p11")]));

    // The OA says Matilda and the weapon chart lists only Matilda Mk II. This
    // source-backed mapping is a candidate pending review, not a default fact.
    public static ContentElementCombatFacts SeventhRoyalTankMatildaMkIiCandidate { get; } = new(
        SeventhRoyalTankRegimentId, Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent(SeventhRoyalTankMatildaComponentId, ArmorComponentClassId, 10, 4,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 3, weaponSourceProfile: CommonwealthMatildaIi)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-british-p11")]));

    public static ContentElementCombatFacts FirstFieldArtilleryCombatFacts { get; } = new(
        FirstFieldArtilleryId, Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent("1-field-artillery.18-25-pounder", ArtilleryComponentClassId, 6, 1,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 1, barrageRating: 8, weaponSourceProfile: Commonwealth1825Pounder)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15")]));

    public static ContentElementCombatFacts ThirdHussarsCombatFacts { get; } = new(
        ThirdHussarsId, Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent("3-hussars.mark-vi-light", ArmorComponentClassId, 10, 2,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 2, weaponSourceProfile: CommonwealthMarkVi)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-british-p11")]));

    public static ContentElementCombatFacts PolishCavalryCombatFacts { get; } = new(
        "polish-1-cavalry", Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent("polish-1-cavalry.reconnaissance", ReconnaissanceComponentClassId, 4, 2,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
            ]), offensiveCloseAssaultRating: 1)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21")]));

    // The OA lists one 2-pounder; the unit chart permits up to two. This is
    // maximum TOE, not a claim that the company arrives with two points.
    public static ContentElementCombatFacts PolishAntiTankCombatFacts { get; } = new(
        "polish-1-antitank", Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent("polish-1-antitank.2-pounder", AntiTankComponentClassId, 2, 1,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 1, weaponSourceProfile: Commonwealth2Pounder)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21")]));

    public static ContentElementCombatFacts PolishArtilleryCombatFacts { get; } = new(
        "polish-1-artillery", Cna1979Combat.CombatUnitClassificationId,
        [new ContentCombatComponent("polish-1-artillery.18-pounder", ArtilleryComponentClassId, 4, 1,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"),
            ]), offensiveCloseAssaultRating: 0, barrageRating: 7, weaponSourceProfile: Commonwealth18Pounder)],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21")]));

    // Partial component facts, not full unit state or arrival strength. The
    // published ID-code chart supplies maximum TOE and defensive rating.
    public static IReadOnlyList<ContentElementCombatFacts> ArrivingInfantryCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            InfantryFacts("polish-1-infantry", 5, 2, "oa-polish-p21", 1),
            InfantryFacts("polish-2-infantry", 5, 2, "oa-polish-p21", 1),
            InfantryFacts("polish-3-infantry", 5, 2, "oa-polish-p21", 1),
            InfantryFacts("polish-kopanski-machine-gun", 2, 6, "oa-polish-p21", 4),
            InfantryFacts("1-royal-sussex", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("4-11-sikhs", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("4-16-punjab", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("24-nz-infantry", 6, 2, "oa-nz-p18", 2),
            InfantryFacts("25-nz-infantry", 6, 2, "oa-nz-p18", 2),
            InfantryFacts("26-nz-infantry", 6, 2, "oa-nz-p18", 2),
        });

    public static IReadOnlyList<ContentElementCombatFacts> ArrivingHeadquartersCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            HeadquartersFacts("polish-brigade-hq", "oa-polish-p21"),
            HeadquartersFacts("7-indian-brigade-hq", "oa-indian-p15"),
            HeadquartersFacts("6-nz-brigade-hq", "oa-nz-p18"),
        });

    public static ContentScheduledArrival NzFieldArtilleryArrival { get; } = new(
        "graziani-nz-6-field-artillery", "commonwealth", new ContentScenarioBoundary(2, 3),
        [NzFieldArtilleryId], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-nz-p18"),
        ]));

    public static ContentScheduledArrival SecondRoyalTankRegimentArrival { get; } = new(
        "graziani-2-rtr", "commonwealth", new ContentScenarioBoundary(2, 3),
        [SecondRoyalTankRegimentId], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
        ]));

    public static ContentScheduledArrival SeventhRoyalTankRegimentArrival { get; } = new(
        "graziani-7-rtr", "commonwealth", new ContentScenarioBoundary(2, 3),
        [SeventhRoyalTankRegimentId], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
        ]));

    public static ContentScheduledArrival FirstFieldArtilleryArrival { get; } = new(
        "graziani-1-field-artillery", "commonwealth", new ContentScenarioBoundary(3, 3),
        [FirstFieldArtilleryId], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15"),
        ]));

    public static ContentScheduledArrival ThirdHussarsArrival { get; } = new(
        "graziani-3-kings-own-hussars", "commonwealth", new ContentScenarioBoundary(4, 3),
        [ThirdHussarsId], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
        ]));

    // Unit membership follows the eight OA rows. Deployment, formation relations,
    // truck allocation and combat characteristics remain separate obligations.
    public static ContentScheduledArrival PolishBrigadeArrival { get; } = new(
        "graziani-polish-brigade", "commonwealth", new ContentScenarioBoundary(1, 3),
        [
            "polish-brigade-hq", "polish-1-infantry", "polish-2-infantry", "polish-3-infantry",
            "polish-kopanski-machine-gun", "polish-1-cavalry", "polish-1-antitank", "polish-1-artillery",
        ], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-polish-p21"),
        ]));

    public static ContentScheduledArrival SeventhIndianBrigadeArrival { get; } = new(
        "graziani-7-indian-brigade", "commonwealth", new ContentScenarioBoundary(5, 1),
        ["7-indian-brigade-hq", "1-royal-sussex", "4-11-sikhs", "4-16-punjab"], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15"),
        ]), new ContentSourceTruckQuantities(light: 5, medium: 20, heavy: null));

    // Chart candidate only: Land §20.11's example conflicts with this timing.
    // No scenario may treat this as an accepted ruling merely by loading it.
    public static ContentScheduledArrival SixthNzBrigadeChartCandidate { get; } = new(
        "graziani-6-nz-brigade", "commonwealth", new ContentScenarioBoundary(6, 3),
        ["6-nz-brigade-hq", "24-nz-infantry", "25-nz-infantry", "26-nz-infantry"], CairoAreaId,
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, "4.43a"),
            new RuleReference(CommonwealthChartsSourceId, "oa-nz-p18"),
        ]), new ContentSourceTruckQuantities(light: 25, medium: 85, heavy: 5));

    public static ContentLocationArea CairoEntryArea { get; } = new(
        CairoAreaId, ["e1730", "e1829", "e1830", "e1930", "e1931"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(AirLogisticsSourceId, "important-locations-p26")]));

    // A few individually named placements from scenario 60.41. Not a complete
    // setup, formation hierarchy, map or legal stacking determination.
    public static IReadOnlyList<ContentInitialPlacement> CommonwealthInitialPlacementCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            InitialPlacement("2-scots-guards", "c4131"),
            InitialPlacement("31-field-artillery", "c4131"),
            InitialPlacement("1-french-motor-marines", "c3926"),
            InitialPlacement("1-royal-northumberland-fusiliers", "c3721"),
            InitialPlacement("1-rtr", "c3520"),
            InitialPlacement("2-rifle-brigade", "c3320"),
            InitialPlacement("11-hussars", "c3020"),
            InitialPlacement("3-coldstream-guards", "c3922"),
            InitialPlacement("1-kings-royal-rifle-corps", "c3922"),
            InitialPlacement("4-royal-horse-artillery", "c3922"),
            InitialPlacement("7-medium-artillery", "c3922"),
            InitialPlacement("3-royal-horse-artillery-antitank", "c3721"),
        });

    // Alternative grouped transcription of selected placements above;
    // a scenario must choose one representation, not include both.
    public static IReadOnlyList<ContentInitialGroup> CommonwealthInitialGroupCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            new ContentInitialGroup("sidi-barrani-source-group", "commonwealth", "c4131",
                ["2-scots-guards", "31-field-artillery"],
                new ContentOrigin(ContentOriginKind.SourceDerived, [new RuleReference(ScenariosSourceId, "60.41")]),
                new ContentSourceTruckQuantities(null, 10, null)),
            new ContentInitialGroup("halfaya-source-group", "commonwealth", "c3922",
                ["3-coldstream-guards", "1-kings-royal-rifle-corps", "4-royal-horse-artillery", "7-medium-artillery"],
                new ContentOrigin(ContentOriginKind.SourceDerived, [new RuleReference(ScenariosSourceId, "60.41")]),
                new ContentSourceTruckQuantities(null, 15, 5)),
            new ContentInitialGroup("c3721-source-group", "commonwealth", "c3721",
                ["1-royal-northumberland-fusiliers", "3-royal-horse-artillery-antitank"],
                new ContentOrigin(ContentOriginKind.SourceDerived, [new RuleReference(ScenariosSourceId, "60.41")]),
                new ContentSourceTruckQuantities(5, 5, null)),
            new ContentInitialGroup("c3926-french-motor-marine-source-candidate", "commonwealth", "c3926",
                ["1-french-motor-marines"],
                new ContentOrigin(ContentOriginKind.SourceDerived,
                    [new RuleReference(ScenariosSourceId, "60.41"), new RuleReference(ErrataSourceId, "4.44b")]),
                new ContentSourceTruckQuantities(null, 2, null)),
        });

    public static ContentInitialGroup FrenchMotorMarineInitialCandidate =>
        CommonwealthInitialGroupCandidates.Single(value => value.LocationId == "c3926");

    public static ContentElementCombatFacts FrenchMotorMarineCombatCandidate { get; } =
        InfantryFacts("1-french-motor-marines", 1, 1, "oa-unassigned-allied-p21", 1);

    public static ContentInitialPlacementCombatFacts CreateFrenchMotorMarineToeCandidate(string scenarioId)
    {
        var component = FrenchMotorMarineCombatCandidate.Components.Single();
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
        return new ContentInitialPlacementCombatFacts(scenarioId, FrenchMotorMarineCombatCandidate.ElementId,
            [new ContentInitialComponentToe(component.ComponentId, 1, origin)]);
    }

    // Scenario 60.41 deploys the brigade at Cairo. Its OA lists three deployed
    // battalions and an HQ with three howitzers; the anti-tank company is a
    // later arrival and is not part of this initial candidate.
    public static ContentInitialGroup SixteenthInfantryBrigadeInitialCandidate { get; } = new(
        "16-infantry-brigade-source-candidate", "commonwealth", "e1829",
        ["16-infantry-brigade-hq", "2-queens-royal-regiment", "2-leicesters", "1-argyll-sutherland"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.41"), new RuleReference(CommonwealthChartsSourceId, "oa-70-infantry-p9")]),
        new ContentSourceTruckQuantities(null, 20, null));

    public static IReadOnlyList<ContentElementCombatFacts> SixteenthInfantryBrigadeCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            new ContentElementCombatFacts("16-infantry-brigade-hq", Cna1979Combat.HeadquartersClassificationId,
                [new ContentCombatComponent("16-infantry-brigade-hq.3-7-howitzer", ArtilleryComponentClassId, 3, 0,
                    new ContentOrigin(ContentOriginKind.SourceDerived,
                        [new RuleReference(CommonwealthChartsSourceId, "oa-70-infantry-p9"),
                            new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                            new RuleReference(CommonwealthChartsSourceId, "4.47")]),
                    offensiveCloseAssaultRating: 0, barrageRating: 7, weaponSourceProfile: Commonwealth37Howitzer)],
                new ContentOrigin(ContentOriginKind.SourceDerived,
                    [new RuleReference(CommonwealthChartsSourceId, "oa-70-infantry-p9")])),
            InfantryFacts("2-queens-royal-regiment", 6, 2, "oa-70-infantry-p9", 2),
            InfantryFacts("2-leicesters", 6, 2, "oa-70-infantry-p9", 2),
            InfantryFacts("1-argyll-sutherland", 6, 2, "oa-70-infantry-p9", 2),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateSixteenthInfantryBrigadeToeCandidates(string scenarioId) =>
        Array.AsReadOnly(SixteenthInfantryBrigadeCombatCandidates.Select(facts =>
        {
            var component = facts.Components.Single();
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, component.MaximumToe, origin)]);
        }).ToArray());

    // The OA's Central India Horse changes characteristics when trained; its
    // initial training state is not inferred here. Later arrivals and detached
    // 31st Field Artillery are excluded from this source group.
    public static ContentInitialGroup FourthIndianDivisionInitialCandidate { get; } = new(
        "4-indian-division-source-candidate", "commonwealth", "d3615",
        ["4-indian-division-hq", "5-indian-brigade-hq", "1-royal-fusiliers", "3-1-punjab", "4-6-rajputana",
            "11-indian-brigade-hq", "2-camerons", "1-6-rajputana", "4-7-rajput",
            "central-india-horse", "25-field-artillery"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.41"), new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15")]),
        new ContentSourceTruckQuantities(5, 20, 2));

    public static IReadOnlyList<ContentElementCombatFacts> FourthIndianDivisionCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            HeadquartersFacts("4-indian-division-hq", "oa-indian-p15"),
            HeadquartersFacts("5-indian-brigade-hq", "oa-indian-p15"),
            HeadquartersFacts("11-indian-brigade-hq", "oa-indian-p15"),
            InfantryFacts("1-royal-fusiliers", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("3-1-punjab", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("4-6-rajputana", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("2-camerons", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("1-6-rajputana", 6, 2, "oa-indian-p15", 1),
            InfantryFacts("4-7-rajput", 6, 2, "oa-indian-p15", 1),
            new ContentElementCombatFacts("25-field-artillery", Cna1979Combat.CombatUnitClassificationId,
                [new ContentCombatComponent("25-field-artillery.3-7-howitzer", ArtilleryComponentClassId, 6, 0,
                    new ContentOrigin(ContentOriginKind.SourceDerived,
                        [new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15"),
                            new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                            new RuleReference(CommonwealthChartsSourceId, "4.47")]),
                    offensiveCloseAssaultRating: 0, barrageRating: 7, weaponSourceProfile: Commonwealth37Howitzer)],
                new ContentOrigin(ContentOriginKind.SourceDerived,
                    [new RuleReference(CommonwealthChartsSourceId, "oa-indian-p15")])),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateFourthIndianDivisionToeCandidates(string scenarioId) =>
        Array.AsReadOnly(FourthIndianDivisionCombatCandidates.Select(facts =>
            new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                facts.Components.Select(component => new ContentInitialComponentToe(component.ComponentId,
                    component.MaximumToe, new ContentOrigin(ContentOriginKind.SourceDerived,
                        [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]))))).ToArray());

    // Scenario 60.41 detaches all the 7th Armored units listed individually
    // above it. The later 7th Motor Brigade HQ is not deployed at the start.
    public static ContentInitialGroup SeventhArmoredDivisionInitialCandidate { get; } = new(
        "7-armored-division-source-candidate", "commonwealth", "d3612",
        ["7-armored-division-hq", "4-armored-brigade-hq", "6-rtr", "7-queens-own-hussars",
            "7-armored-brigade-hq", "8-hussars", "7-support-group-hq"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.41"), new RuleReference(CommonwealthChartsSourceId, "oa-7-armored-p6")]),
        new ContentSourceTruckQuantities(5, 5, 5));

    public static IReadOnlyList<ContentElementCombatFacts> SeventhArmoredDivisionCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            CommonwealthTankHeadquartersFacts("7-armored-division-hq", "mark-vi-light", 2, 2, CommonwealthMarkVi),
            CommonwealthTankHeadquartersFacts("4-armored-brigade-hq", "mark-vi-light", 2, 2, CommonwealthMarkVi),
            CommonwealthTankHeadquartersFacts("7-armored-brigade-hq", "a9-cruiser", 3, 3, CommonwealthA9),
            InitialComponentFacts("6-rtr", "mark-vi-light", ArmorComponentClassId, 10, 2, "oa-7-armored-p6", true, 2, weaponSourceProfile: CommonwealthMarkVi),
            InitialComponentFacts("7-queens-own-hussars", "mark-vi-light", ArmorComponentClassId, 10, 2, "oa-7-armored-p6", true, 2, weaponSourceProfile: CommonwealthMarkVi),
            InitialComponentFacts("8-hussars", "a10-cruiser", ArmorComponentClassId, 10, 3, "oa-7-armored-p6", true, 3, weaponSourceProfile: CommonwealthA10),
            HeadquartersFacts("7-support-group-hq", "oa-7-armored-p6"),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateSeventhArmoredDivisionToeCandidates(string scenarioId) =>
        Array.AsReadOnly(SeventhArmoredDivisionCombatCandidates.Select(facts =>
            new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                facts.Components.Select(component => new ContentInitialComponentToe(component.ComponentId,
                    facts.ElementId == "8-hussars" ? 7 : component.MaximumToe,
                    new ContentOrigin(ContentOriginKind.SourceDerived,
                        [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]))))).ToArray());

    // These are co-located chart members, not an assertion that the current
    // engine can create the formations or adjudicate their attachments.
    public static ContentInitialGroup CireneDivisionChartCandidate { get; } = new(
        "cirene-division-source-candidate", "axis", "c4120",
        [
            "63-cirene-hq", "157-infantry-hq", "1-157-infantry", "2-157-infantry",
            "3-157-infantry", "158-infantry-hq", "1-158-infantry", "2-158-infantry",
            "3-158-infantry", "63-machine-gun", "63-antitank", "45-artillery",
            "63-engineer", "63-bersaglieri", "lxiii-light-tank",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(ErrataSourceId, "oa-cirene-p4")]),
        new ContentSourceTruckQuantities(null, 45, 5));

    public static ContentInitialGroup MarmaricaDivisionChartCandidate { get; } = new(
        "marmarica-division-source-candidate", "axis", "c3918",
        [
            "62-marmarica-hq", "115-infantry-hq", "1-115-infantry", "2-115-infantry",
            "3-115-infantry", "116-infantry-hq", "1-116-infantry", "2-116-infantry",
            "3-116-infantry", "62-machine-gun", "62-antitank", "44-artillery",
            "62-engineer", "lxii-light-tank",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(ErrataSourceId, "oa-marmarica-p4")]),
        new ContentSourceTruckQuantities(null, 40, 5));

    // OA members are co-located by scenario deployment; this does not assign
    // attached vehicles or resolve how headquarters and the tank unit operate.
    public static ContentInitialGroup FirstLibyanDivisionChartCandidate { get; } = new(
        "1-libyan-division-source-candidate", "axis", "c4020",
        ["1-libyan-division-hq", "1-libyan-regiment-hq", "8-1-libyan-infantry",
            "9-1-libyan-infantry", "10-1-libyan-infantry", "2-libyan-regiment-hq",
            "11-2-libyan-infantry", "12-2-libyan-infantry", "13-2-libyan-infantry",
            "1-libyan-artillery", "1-libyan-antitank"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-libyan-p5")]),
        new ContentSourceTruckQuantities(10, 15, null));

    public static ContentInitialGroup SecondLibyanDivisionChartCandidate { get; } = new(
        "2-libyan-division-source-candidate", "axis", "c3920",
        ["2-libyan-division-hq", "3-libyan-regiment-hq", "2-3-libyan-infantry",
            "6-3-libyan-infantry", "7-3-libyan-infantry", "4-libyan-regiment-hq",
            "14-4-libyan-infantry", "15-4-libyan-infantry", "16-4-libyan-infantry",
            "2-libyan-artillery", "ix-libyan-light-tank"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-libyan-p5")]),
        new ContentSourceTruckQuantities(15, 15, null));

    public static IReadOnlyList<ContentElementCombatFacts> LibyanDivisionInfantryCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            "8-1-libyan-infantry", "9-1-libyan-infantry", "10-1-libyan-infantry",
            "11-2-libyan-infantry", "12-2-libyan-infantry", "13-2-libyan-infantry",
            "2-3-libyan-infantry", "6-3-libyan-infantry", "7-3-libyan-infantry",
            "14-4-libyan-infantry", "15-4-libyan-infantry", "16-4-libyan-infantry",
        }.Select(id => AxisInfantryFacts(id, 5, 1, "oa-libyan-p5", 1)).ToArray());

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateLibyanDivisionInfantryToeSeeds(string scenarioId) =>
        Array.AsReadOnly(LibyanDivisionInfantryCombatFacts.Select(facts =>
        {
            var component = facts.Components.Single();
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, component.MaximumToe, origin)]);
        }).ToArray());

    public static IReadOnlyList<ContentElementCombatFacts> LibyanDivisionSupportCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            LibyanRegimentalHeadquartersFacts("1-libyan-regiment-hq"),
            LibyanRegimentalHeadquartersFacts("2-libyan-regiment-hq"),
            LibyanRegimentalHeadquartersFacts("3-libyan-regiment-hq"),
            LibyanRegimentalHeadquartersFacts("4-libyan-regiment-hq"),
            AxisWeaponFacts("1-libyan-antitank", "47-32-gun", AntiTankComponentClassId, 2, 1,
                "oa-libyan-p5", 1, weaponSourceProfile: Italian4732),
            AxisWeaponFacts("1-libyan-artillery", "75-27-gun", ArtilleryComponentClassId, 12, 1,
                "oa-libyan-p5", 1, 6, Italian7527),
            LibyanSecondArtilleryFacts(),
        });

    public static ContentElementCombatFacts AttachedNinthLibyanTankCombatFacts { get; } =
        NinthLibyanTankFacts();

    public static ContentInitialPlacementCombatFacts CreateAttachedNinthLibyanTankToeSeed(string scenarioId)
    {
        var component = AttachedNinthLibyanTankCombatFacts.Components.Single();
        return new ContentInitialPlacementCombatFacts(scenarioId, AttachedNinthLibyanTankCombatFacts.ElementId,
            [new ContentInitialComponentToe(component.ComponentId, 9, component.Origin)]);
    }

    // Aresca retains XXI(L); LXII(L), LXIII(L), and I(M) begin attached
    // elsewhere. The partially detached LX(L) belongs to Tripoli instead.
    public static ContentInitialGroup ArescaTankRegimentInitialCandidate { get; } = new(
        "aresca-tank-regiment-source-candidate", "axis", "c3919",
        ["aresca-tank-regiment-hq", "xxi-libyan-light-tank"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-libyan-tank-command-p6")]),
        new ContentSourceTruckQuantities(null, 5, 5));

    public static ContentInitialGroup TripoliTankRegimentInitialSubset { get; } = new(
        "tripoli-tank-regiment-source-subset", "axis", "c4321",
        ["tripoli-tank-regiment-hq", "xx-libyan-light-tank", "lxi-libyan-light-tank"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-libyan-tank-command-p6")]));

    public static IReadOnlyList<ContentElementCombatFacts> LibyanTankRegimentCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            LibyanTankRegimentFacts("aresca-tank-regiment-hq", headquarters: true),
            LibyanTankRegimentFacts("xxi-libyan-light-tank", headquarters: false),
            LibyanTankRegimentFacts("tripoli-tank-regiment-hq", headquarters: true),
            LibyanTankRegimentFacts("xx-libyan-light-tank", headquarters: false),
            LibyanTankRegimentFacts("lxi-libyan-light-tank", headquarters: false),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateLibyanTankRegimentToeCandidates(string scenarioId) =>
        Array.AsReadOnly(LibyanTankRegimentCombatCandidates.Select(facts =>
        {
            var component = facts.Components.Single();
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, component.MaximumToe, component.Origin)]);
        }).ToArray());

    // The OA and scenario agree on these listed members; the separate LX(L)
    // company mentioned by the tank-command OA still needs split-unit authority.
    public static ContentInitialGroup MalettiInitialChartCandidate { get; } = new(
        "maletti-source-candidate", "axis", "c3617",
        ["maletti-hq", "1-libyan-motor-regiment-hq", "1-1-libyan-motor-infantry",
            "3-1-libyan-motor-infantry", "5-1-libyan-motor-infantry", "5-libyan-regiment-hq",
            "17-5-libyan-infantry", "19-5-libyan-infantry", "sahariano-regiment-hq",
            "1-sahariano-camel", "2-sahariano-camel", "3-sahariano-camel",
            "1-maletti-artillery", "2-maletti-artillery", "ii-medium-tank"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-maletti-p7"),
                new RuleReference(AxisChartsSourceId, "oa-libyan-tank-command-p6")]),
        new ContentSourceTruckQuantities(5, 30, 5));

    // Partial component facts: the four headquarters and the split LX(L)
    // company remain unresolved rather than being assigned empty components.
    public static IReadOnlyList<ContentElementCombatFacts> MalettiCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("1-1-libyan-motor-infantry", 5, 1, "oa-maletti-p7", 1),
            AxisInfantryFacts("3-1-libyan-motor-infantry", 5, 1, "oa-maletti-p7", 1),
            AxisInfantryFacts("5-1-libyan-motor-infantry", 5, 1, "oa-maletti-p7", 1),
            AxisInfantryFacts("17-5-libyan-infantry", 5, 1, "oa-maletti-p7", 1),
            AxisInfantryFacts("19-5-libyan-infantry", 5, 1, "oa-maletti-p7", 1),
            MalettiCamelFacts("1-sahariano-camel"),
            MalettiCamelFacts("2-sahariano-camel"),
            MalettiCamelFacts("3-sahariano-camel"),
            AxisWeaponFacts("1-maletti-artillery", "75-27-gun", ArtilleryComponentClassId, 4, 1,
                "oa-maletti-p7", 1, 6, Italian7527),
            AxisWeaponFacts("2-maletti-artillery", "75-18-gun-howitzer", ArtilleryComponentClassId, 4, 0,
                "oa-maletti-p7", 1, 6, Italian7518),
            AxisWeaponFacts("ii-medium-tank", "m11-39", ArmorComponentClassId, 7, 3,
                "oa-libyan-tank-command-p6", 3, weaponSourceProfile: ItalianM1139),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateMalettiToeCandidates(string scenarioId) =>
        Array.AsReadOnly(MalettiCombatCandidates.Select(facts =>
        {
            var component = facts.Components.Single();
            var toe = facts.ElementId switch
            {
                "1-maletti-artillery" => 3,
                "2-maletti-artillery" => 2,
                _ => component.MaximumToe,
            };
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, toe, origin)]);
        }).ToArray());

    public static ContentInitialGroup CatanzaroInitialChartCandidate { get; } = new(
        "catanzaro-source-candidate", "axis", "c4707",
        ["64-catanzaro-hq", "141-infantry-hq", "1-141-infantry", "2-141-infantry", "3-141-infantry",
            "142-infantry-hq", "1-142-infantry", "2-142-infantry", "3-142-infantry", "64-machine-gun",
            "64-antitank", "203-artillery", "64-engineer"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-catanzaro-p10")]),
        new ContentSourceTruckQuantities(null, 30, 5));

    // Division HQ and engineer characteristics need their own conditional
    // component semantics. The remaining source facts are still candidates.
    public static IReadOnlyList<ContentElementCombatFacts> CatanzaroCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            AxisRegimentalHeadquartersFacts("141-infantry-hq", "oa-catanzaro-p10"),
            AxisRegimentalHeadquartersFacts("142-infantry-hq", "oa-catanzaro-p10"),
            AxisInfantryFacts("1-141-infantry", 7, 1, "oa-catanzaro-p10", 1),
            AxisInfantryFacts("2-141-infantry", 7, 1, "oa-catanzaro-p10", 1),
            AxisInfantryFacts("3-141-infantry", 7, 1, "oa-catanzaro-p10", 1),
            AxisInfantryFacts("1-142-infantry", 7, 1, "oa-catanzaro-p10", 1),
            AxisInfantryFacts("2-142-infantry", 7, 1, "oa-catanzaro-p10", 1),
            AxisInfantryFacts("3-142-infantry", 7, 1, "oa-catanzaro-p10", 1),
            AxisInfantryFacts("64-machine-gun", 2, 7, "oa-catanzaro-p10", 5),
            AxisWeaponFacts("64-antitank", "47-32-gun", AntiTankComponentClassId, 2, 1,
                "oa-catanzaro-p10", 1, weaponSourceProfile: Italian4732),
            DivisionArtilleryFacts("203-artillery", "oa-catanzaro-p10", "75-18-gun-howitzer", Italian7518, 0, 6,
                "105-28-gun", Italian10528, 1, 9, AxisChartsSourceId),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateCatanzaroToeCandidates(string scenarioId) =>
        Array.AsReadOnly(CatanzaroCombatCandidates.Select(facts =>
        {
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [new RuleReference(AxisChartsSourceId, "oa-catanzaro-p10"), new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                facts.Components.Select(component => new ContentInitialComponentToe(component.ComponentId,
                    facts.ElementId == "203-artillery" ? component.ComponentId.EndsWith(".75-18-gun-howitzer", StringComparison.Ordinal) ? 6 : 3
                        : component.MaximumToe, origin)));
        }).ToArray());

    public static ContentInitialGroup FirstCcnnInitialChartCandidate { get; } = new(
        "1-ccnn-source-candidate", "axis", "c4218",
        ["1-ccnn-hq", "219-ccnn-legion-hq", "129-ccnn-infantry", "133-ccnn-infantry", "146-ccnn-infantry",
            "233-ccnn-legion-hq", "114-ccnn-infantry", "116-ccnn-infantry", "118-ccnn-infantry",
            "201-ccnn-machine-gun", "201-ccnn-artillery", "201-ccnn-engineer", "201-ccnn-bersaglieri",
            "i-medium-tank"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-ccnn-p17"),
                new RuleReference(AxisChartsSourceId, "oa-libyan-tank-command-p6")]),
        new ContentSourceTruckQuantities(10, 25, 10));

    // The division HQ's one artillery TOE point is recorded separately by
    // CreateFirstCcnnToeCandidates/CcnnHqUnidentifiedArtillerySeed under
    // RUL-GRA-26 - its weapon model remains permanently unidentified, so it
    // is deliberately not a ContentElementCombatFacts/ContentCombatComponent
    // entry here (that record requires a defensive rating no source supports).
    // The engineer's conditional combat rules remain unresolved.
    public static IReadOnlyList<ContentElementCombatFacts> FirstCcnnCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            AxisRegimentalHeadquartersFacts("219-ccnn-legion-hq", "oa-ccnn-p17"),
            AxisRegimentalHeadquartersFacts("233-ccnn-legion-hq", "oa-ccnn-p17"),
            AxisInfantryFacts("129-ccnn-infantry", 5, 1, "oa-ccnn-p17", 1),
            AxisInfantryFacts("133-ccnn-infantry", 5, 1, "oa-ccnn-p17", 1),
            AxisInfantryFacts("146-ccnn-infantry", 5, 1, "oa-ccnn-p17", 1),
            AxisInfantryFacts("114-ccnn-infantry", 5, 1, "oa-ccnn-p17", 1),
            AxisInfantryFacts("116-ccnn-infantry", 5, 1, "oa-ccnn-p17", 1),
            AxisInfantryFacts("118-ccnn-infantry", 5, 1, "oa-ccnn-p17", 1),
            AxisInfantryFacts("201-ccnn-machine-gun", 2, 7, "oa-ccnn-p17", 5),
            FirstCcnnArtilleryFacts(),
            FirstCcnnBersaglieriFacts(),
            AxisWeaponFacts("i-medium-tank", "m11-39", ArmorComponentClassId, 7, 3,
                "oa-libyan-tank-command-p6", 3, weaponSourceProfile: ItalianM1139),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateFirstCcnnToeCandidates(string scenarioId) =>
        Array.AsReadOnly(FirstCcnnCombatCandidates.Select(facts =>
        {
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. facts.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                facts.Components.Select(component => new ContentInitialComponentToe(component.ComponentId,
                    facts.ElementId == "201-ccnn-artillery"
                        ? component.ComponentId.EndsWith(".75-27-gun", StringComparison.Ordinal) ? 6
                            : component.ComponentId.EndsWith(".100-17-howitzer", StringComparison.Ordinal) ? 3 : 2
                        : component.MaximumToe, origin)));
        }).Append(CcnnHqUnidentifiedArtillerySeed(scenarioId, "1-ccnn-hq", "oa-ccnn-p17")).ToArray());

    public static ContentInitialGroup FourthCcnnInitialChartCandidate { get; } = new(
        "4-ccnn-source-candidate", "axis", "c4507",
        ["4-ccnn-hq", "240-ccnn-legion-hq", "70-ccnn-infantry", "119-ccnn-infantry", "176-ccnn-infantry",
            "270-ccnn-legion-hq", "3-ccnn-infantry", "17-ccnn-infantry", "170-ccnn-infantry",
            "204-ccnn-machine-gun", "204-ccnn-artillery", "204-ccnn-engineer"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-ccnn-p18")]),
        new ContentSourceTruckQuantities(10, 25, 10));

    public static IReadOnlyList<ContentElementCombatFacts> FourthCcnnCombatCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            AxisRegimentalHeadquartersFacts("240-ccnn-legion-hq", "oa-ccnn-p18"),
            AxisRegimentalHeadquartersFacts("270-ccnn-legion-hq", "oa-ccnn-p18"),
            AxisInfantryFacts("70-ccnn-infantry", 5, 1, "oa-ccnn-p18", 1),
            AxisInfantryFacts("119-ccnn-infantry", 5, 1, "oa-ccnn-p18", 1),
            AxisInfantryFacts("176-ccnn-infantry", 5, 1, "oa-ccnn-p18", 1),
            AxisInfantryFacts("3-ccnn-infantry", 5, 1, "oa-ccnn-p18", 1),
            AxisInfantryFacts("17-ccnn-infantry", 5, 1, "oa-ccnn-p18", 1),
            AxisInfantryFacts("170-ccnn-infantry", 5, 1, "oa-ccnn-p18", 1),
            AxisInfantryFacts("204-ccnn-machine-gun", 2, 7, "oa-ccnn-p18", 5),
            AxisHeavyDivisionArtilleryFacts("204-ccnn-artillery", "oa-ccnn-p18",
                "75-18-gun-howitzer", Italian7518, 0, 6, "105-28-gun", Italian10528, 1, 9),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateFourthCcnnToeCandidates(string scenarioId) =>
        Array.AsReadOnly(FourthCcnnCombatCandidates.Select(facts =>
        {
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. facts.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                facts.Components.Select(component => new ContentInitialComponentToe(component.ComponentId,
                    facts.ElementId == "204-ccnn-artillery"
                        ? component.ComponentId.EndsWith(".75-18-gun-howitzer", StringComparison.Ordinal) ? 6
                            : component.ComponentId.EndsWith(".105-28-gun", StringComparison.Ordinal) ? 3 : 2
                        : component.MaximumToe, origin)));
        }).Append(CcnnHqUnidentifiedArtillerySeed(scenarioId, "4-ccnn-hq", "oa-ccnn-p18")).ToArray());

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateLibyanDivisionSupportToeSeeds(string scenarioId)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(AxisChartsSourceId, "oa-libyan-p5"), new RuleReference(LandRulesSourceId, "4.45")]);
        ContentInitialPlacementCombatFacts Seed(string id, params (string Suffix, int Toe)[] components) =>
            new(scenarioId, id, components.Select(component =>
                new ContentInitialComponentToe($"{id}.{component.Suffix}", component.Toe, origin)));
        return Array.AsReadOnly(new[]
        {
            Seed("1-libyan-regiment-hq", ("65-17-gun", 1)),
            Seed("2-libyan-regiment-hq", ("65-17-gun", 1)),
            Seed("3-libyan-regiment-hq", ("65-17-gun", 1)),
            Seed("4-libyan-regiment-hq", ("65-17-gun", 1)),
            Seed("1-libyan-antitank", ("47-32-gun", 2)),
            Seed("1-libyan-artillery", ("75-27-gun", 6)),
            Seed("2-libyan-artillery", ("75-27-gun", 9), ("100-17-howitzer", 3)),
        });
    }

    public static IReadOnlyList<ContentElementCombatFacts> CireneMarmaricaInfantryCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("1-157-infantry", 7, 1, "oa-cirene-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("2-157-infantry", 7, 1, "oa-cirene-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("3-157-infantry", 7, 1, "oa-cirene-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("1-158-infantry", 7, 1, "oa-cirene-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("2-158-infantry", 7, 1, "oa-cirene-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("3-158-infantry", 7, 1, "oa-cirene-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("63-machine-gun", 2, 7, "oa-cirene-p4", 5, sourceId: ErrataSourceId),
            AxisInfantryFacts("1-115-infantry", 7, 1, "oa-marmarica-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("2-115-infantry", 7, 1, "oa-marmarica-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("3-115-infantry", 7, 1, "oa-marmarica-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("1-116-infantry", 7, 1, "oa-marmarica-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("2-116-infantry", 7, 1, "oa-marmarica-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("3-116-infantry", 7, 1, "oa-marmarica-p4", 1, sourceId: ErrataSourceId),
            AxisInfantryFacts("62-machine-gun", 2, 7, "oa-marmarica-p4", 5, sourceId: ErrataSourceId),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateCireneMarmaricaInfantryToeSeeds(string scenarioId) =>
        Array.AsReadOnly(CireneMarmaricaInfantryCombatFacts.Select(facts =>
        {
            var component = facts.Components.Single();
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, component.MaximumToe, origin)]);
        }).ToArray());

    public static IReadOnlyList<ContentElementCombatFacts> CireneMarmaricaAntiTankCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisWeaponFacts("63-antitank", "47-32-gun", AntiTankComponentClassId, 2, 1,
                "oa-cirene-p4", 1, weaponSourceProfile: Italian4732, sourceId: ErrataSourceId),
            AxisWeaponFacts("62-antitank", "47-32-gun", AntiTankComponentClassId, 2, 1,
                "oa-marmarica-p4", 1, weaponSourceProfile: Italian4732, sourceId: ErrataSourceId),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateCireneMarmaricaAntiTankToeSeeds(string scenarioId) =>
        Array.AsReadOnly(CireneMarmaricaAntiTankCombatFacts.Select(facts =>
        {
            var component = facts.Components.Single();
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, 2, component.Origin)]);
        }).ToArray());

    // Partial weapon choices only; neither artillery nor AA is admitted by the current engine.
    public static IReadOnlyList<ContentElementCombatFacts> CireneMarmaricaArtilleryCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            DivisionArtilleryFacts("45-artillery", "oa-cirene-p4", "75-18-gun-howitzer", Italian7518, 0, 6,
                "100-17-howitzer", Italian10017, 0, 8),
            DivisionArtilleryFacts("44-artillery", "oa-marmarica-p4", "75-27-gun", Italian7527, 1, 6,
                "105-28-gun", Italian10528, 1, 9),
        });

    // Printed starting strengths; other weapon types remain outside these partial records.
    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateCireneMarmaricaArtilleryToeSeeds(string scenarioId)
    {
        ContentInitialPlacementCombatFacts Seed(string elementId, string locator,
            params (string Suffix, int Toe)[] components)
        {
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [new RuleReference(ErrataSourceId, locator), new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, elementId,
                components.Select(component => new ContentInitialComponentToe(
                    $"{elementId}.{component.Suffix}", component.Toe, origin)));
        }

        return Array.AsReadOnly(new[]
        {
            Seed("45-artillery", "oa-cirene-p4", ("75-18-gun-howitzer", 6),
                ("100-17-howitzer", 3), ("light-aa", 3)),
            Seed("44-artillery", "oa-marmarica-p4", ("75-27-gun", 6),
                ("105-28-gun", 3), ("light-aa", 3)),
        });
    }

    // This source subset intentionally omits Tobruk's artillery, AA and naval
    // entries. It does not assert these counters are independent or attached.
    public static ContentInitialGroup TobrukInfantryChartSubset { get; } = new(
        "tobruk-garrison-infantry-source-subset", "axis", "c4807",
        [
            "31-gaf-sdic-hq", "1-31-gaf-sdic", "2-31-gaf-sdic", "3-31-gaf-sdic",
            "32-gaf-sdic-hq", "1-32-gaf-sdic", "2-32-gaf-sdic", "3-32-gaf-sdic",
            "marine-regiment-hq", "1-marine-battalion", "2-marine-battalion", "3-marine-battalion",
            "31-libyan-coastal-defense-battalion",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-tobruk-p12"),
        ]));

    // The OA's San Giorgio entry is naval and is intentionally not represented
    // as a land element. Component restrictions remain separate source work.
    public static ContentInitialGroup TobrukArtilleryAndAaChartSubset { get; } = new(
        "tobruk-garrison-artillery-aa-source-subset", "axis", "c4807",
        [
            "5-tobruk-artiglieria", "6-tobruk-artiglieria", "7-tobruk-artiglieria",
            "1-tobruk-coastal-defense-artillery", "2-tobruk-coastal-defense-artillery",
            "3-tobruk-coastal-defense-artillery", "4-tobruk-coastal-defense-artillery",
            "5-tobruk-coastal-defense-artillery", "tobruk-light-aa-battalion",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-tobruk-p12"),
        ]));

    // Raw printed TOE only. Parentheses limit some land barrage; gun emplacement,
    // AA motorization, eligible targets and AA conditions are not modeled here.
    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateTobrukSupportToeSeeds(string scenarioId)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(AxisChartsSourceId, "oa-tobruk-p12"),
            new RuleReference(LandRulesSourceId, "4.45"),
        ]);
        ContentInitialPlacementCombatFacts Seed(string elementId, params (string Suffix, int Toe)[] components) =>
            new(scenarioId, elementId, components.Select(component => new ContentInitialComponentToe(
                $"{elementId}.{component.Suffix}", component.Toe, origin)));
        return Array.AsReadOnly(new[]
        {
            Seed("5-tobruk-artiglieria", ("100-17-howitzer", 2)),
            Seed("6-tobruk-artiglieria", ("149mm-vichy-gun", 2)),
            Seed("7-tobruk-artiglieria", ("149-13-howitzer", 2)),
            Seed("1-tobruk-coastal-defense-artillery", ("149-13-howitzer", 2)),
            Seed("2-tobruk-coastal-defense-artillery", ("100-17-howitzer", 6), ("i-light-aa", 6)),
            Seed("3-tobruk-coastal-defense-artillery", ("75-18-gun-howitzer", 4), ("i-light-aa", 8)),
            Seed("4-tobruk-coastal-defense-artillery", ("105-28-gun", 6), ("i-heavy-aa", 4)),
            Seed("5-tobruk-coastal-defense-artillery", ("149-13-howitzer", 6), ("i-heavy-aa", 2)),
            Seed("tobruk-light-aa-battalion", ("i-light-aa", 8)),
        });
    }

    public static IReadOnlyList<ContentElementCombatFacts> TobrukSupportCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisWeaponFacts("5-tobruk-artiglieria", "100-17-howitzer", ArtilleryComponentClassId, 4, 0,
                "oa-tobruk-p12", 1, 8, Italian10017),
            AxisWeaponFacts("6-tobruk-artiglieria", "149mm-vichy-gun", ArtilleryComponentClassId, 4, 1,
                "oa-tobruk-p12", 0, 10, Italian149Vichy),
            AxisWeaponFacts("7-tobruk-artiglieria", "149-13-howitzer", ArtilleryComponentClassId, 4, 0,
                "oa-tobruk-p12", 1, 15, Italian14913),
            AxisAntiAirFacts("tobruk-light-aa-battalion", "i-light-aa", 8, "oa-tobruk-p12"),
        });

    // Italian x/v codes use the Italian chart, not identically named
    // Commonwealth codes. Conditional headquarters are intentionally absent.
    public static IReadOnlyList<ContentElementCombatFacts> TobrukInfantryCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("1-31-gaf-sdic", 5, offensiveRating: 1), AxisInfantryFacts("2-31-gaf-sdic", 5, offensiveRating: 1),
            AxisInfantryFacts("3-31-gaf-sdic", 5, offensiveRating: 1), AxisInfantryFacts("1-32-gaf-sdic", 6, offensiveRating: 1),
            AxisInfantryFacts("2-32-gaf-sdic", 6, offensiveRating: 1), AxisInfantryFacts("3-32-gaf-sdic", 6, offensiveRating: 1),
            AxisInfantryFacts("1-marine-battalion", 5, offensiveRating: 1), AxisInfantryFacts("2-marine-battalion", 5, offensiveRating: 1),
            AxisInfantryFacts("3-marine-battalion", 5, offensiveRating: 1), AxisInfantryFacts("31-libyan-coastal-defense-battalion", 5, offensiveRating: 1),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateTobrukInfantryToeSeeds(string scenarioId) =>
        Array.AsReadOnly(TobrukInfantryCombatFacts.Select(facts =>
        {
            if (facts.ElementId is not ("1-31-gaf-sdic" or "2-31-gaf-sdic" or "3-31-gaf-sdic"
                or "1-32-gaf-sdic" or "2-32-gaf-sdic" or "3-32-gaf-sdic"
                or "1-marine-battalion" or "2-marine-battalion" or "3-marine-battalion"
                or "31-libyan-coastal-defense-battalion"))
                throw new InvalidOperationException("Source Tobruk strength has not been transcribed.");
            var component = facts.Components.Single();
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, component.MaximumToe, origin)]);
        }).ToArray());

    // The OA names the 30th HQ but prints 32 GaF. The counter sheet labels
    // 30 GaF as Bardia and 32 GaF as Tobruk; source acceptance remains open.
    public static ContentInitialGroup BardiaGarrisonChartCandidate { get; } = new(
        "bardia-garrison-source-candidate", "axis", "c4321",
        [
            "30-gaf-sdic-hq", "1-30-gaf-sdic", "2-30-gaf-sdic", "3-30-gaf-sdic",
            "vec-machine-gun-battalion", "30-gaf-antitank-battalion",
            "8-gaf-artillery", "9-gaf-artillery", "10-gaf-artillery",
            "5-bardia-coastal-defense-artillery", "17-bardia-antiair",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-bardia-p13"),
            new RuleReference(AxisCounterSheetSourceId, "30-gaf-bardia-32-gaf-tobruk"),
          ]));

    public static IReadOnlyList<ContentElementCombatFacts> BardiaInfantryCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("1-30-gaf-sdic", 7, 1, "oa-bardia-p13", 1),
            AxisInfantryFacts("2-30-gaf-sdic", 7, 1, "oa-bardia-p13", 1),
            AxisInfantryFacts("3-30-gaf-sdic", 7, 1, "oa-bardia-p13", 1),
            AxisInfantryFacts("vec-machine-gun-battalion", 3, 3, "oa-bardia-p13", 2),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateBardiaInfantryToeSeeds(string scenarioId) =>
        Array.AsReadOnly(BardiaInfantryCombatFacts.Select(facts =>
        {
            if (facts.ElementId is not ("1-30-gaf-sdic" or "2-30-gaf-sdic" or "3-30-gaf-sdic"
                or "vec-machine-gun-battalion"))
                throw new InvalidOperationException("Source Bardia strength has not been transcribed.");
            var component = facts.Components.Single();
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, component.MaximumToe, origin)]);
        }).ToArray());

    public static IReadOnlyList<ContentElementCombatFacts> BardiaSupportCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisWeaponFacts("30-gaf-antitank-battalion", "47-32-gun", AntiTankComponentClassId, 6, 1, "oa-bardia-p13", 1, weaponSourceProfile: Italian4732),
            AxisWeaponFacts("8-gaf-artillery", "100-17-howitzer", ArtilleryComponentClassId, 9, 0, "oa-bardia-p13", 1, 8, Italian10017),
            BardiaMixedArtilleryFacts("9-gaf-artillery", "75-27-gun", Italian7527, 6),
            BardiaMixedArtilleryFacts("10-gaf-artillery", "105-28-gun", Italian10528, 9),
            AxisAntiAirFacts("17-bardia-antiair", "light-aa", 8, "oa-bardia-p13"),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateBardiaSupportToeSeeds(string scenarioId)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(AxisChartsSourceId, "oa-bardia-p13"),
            new RuleReference(LandRulesSourceId, "4.45"),
        ]);
        ContentInitialPlacementCombatFacts Seed(string elementId, params (string Suffix, int Toe)[] components) =>
            new(scenarioId, elementId, components.Select(component => new ContentInitialComponentToe(
                $"{elementId}.{component.Suffix}", component.Toe, origin)));
        return Array.AsReadOnly(new[]
        {
            Seed("30-gaf-antitank-battalion", ("47-32-gun", 6)),
            Seed("8-gaf-artillery", ("100-17-howitzer", 4)),
            Seed("9-gaf-artillery", ("100-17-howitzer", 4), ("75-27-gun", 2)),
            Seed("10-gaf-artillery", ("105-28-gun", 4), ("100-17-howitzer", 3)),
            Seed("5-bardia-coastal-defense-artillery", ("75-27-gun", 1), ("100-17-howitzer", 1), ("105-28-gun", 1)),
            Seed("17-bardia-antiair", ("light-aa", 8)),
        });
    }

    // The scenario prints B4827; the OA and location references print A4827.
    // The counter sheet supports reading the OA's bb engineer code as bbb.
    // Both corrections remain candidates, not an admitted setup or ruling.
    public static ContentInitialGroup BenghaziGarrisonChartCandidate { get; } = new(
        "benghazi-garrison-source-candidate", "axis", "a4827",
        ["2-libyan-coastal-defense-battalion", "7-benghazi-coastal-defense-aa",
            "20-corps-artillery", "8-2-benghazi-engineers"],
        new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-benghazi-p12"),
            new RuleReference(AxisCounterSheetSourceId, "8-2-benghazi-engineer"),
        ]));

    // The engineer's conditional bbb chart entry is not represented as infantry.
    public static IReadOnlyList<ContentElementCombatFacts> BenghaziCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("2-libyan-coastal-defense-battalion", 5, 1, "oa-benghazi-p12", 1),
            AxisAntiAirFacts("7-benghazi-coastal-defense-aa", "i-light-aa", 6, "oa-benghazi-p12"),
            AxisWeaponFacts("20-corps-artillery", "100-17-howitzer", ArtilleryComponentClassId,
                9, 0, "oa-benghazi-p12", 1, 8, Italian10017),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateBenghaziToeSeeds(string scenarioId) =>
        Array.AsReadOnly(BenghaziCombatFacts.Select(facts =>
        {
            var component = facts.Components.Single();
            var currentToe = facts.ElementId switch
            {
                "2-libyan-coastal-defense-battalion" => component.MaximumToe,
                "7-benghazi-coastal-defense-aa" or "20-corps-artillery" => 6,
                _ => throw new InvalidOperationException("Source Benghazi strength has not been transcribed."),
            };
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, currentToe, origin)]);
        }).ToArray());

    public static ContentInitialGroup DernaGarrisonChartCandidate { get; } = new(
        "derna-garrison-source-candidate", "axis", "b5925",
        [
            "54-territorial-infantry", "4-derna-coastal-defense-artillery",
            "6-derna-coastal-defense-artillery", "15-derna-coastal-defense-aa",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-derna-p13"),
        ]));

    public static ContentElementCombatFacts DernaTerritorialCombatFacts { get; } =
        AxisInfantryFacts("54-territorial-infantry", 10, 1, "oa-derna-p13", 1);

    public static ContentElementCombatFacts DernaAntiAirCombatFacts { get; } =
        AxisAntiAirFacts("15-derna-coastal-defense-aa", "light-aa", 6, "oa-derna-p13");

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateDernaToeSeeds(string scenarioId)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(AxisChartsSourceId, "oa-derna-p13"),
            new RuleReference(AxisChartsSourceId, "4.46b"),
            new RuleReference(LandRulesSourceId, "4.45"),
        ]);
        return Array.AsReadOnly(new[]
        {
            new ContentInitialPlacementCombatFacts(scenarioId, "54-territorial-infantry",
                [new ContentInitialComponentToe("54-territorial-infantry.infantry", 10, origin)]),
            new ContentInitialPlacementCombatFacts(scenarioId, "4-derna-coastal-defense-artillery",
                [new ContentInitialComponentToe("4-derna-coastal-defense-artillery.75-18-gun-howitzer", 5, origin)]),
            new ContentInitialPlacementCombatFacts(scenarioId, "6-derna-coastal-defense-artillery",
                [new ContentInitialComponentToe("6-derna-coastal-defense-artillery.75-18-gun-howitzer", 2, origin)]),
            new ContentInitialPlacementCombatFacts(scenarioId, "15-derna-coastal-defense-aa",
                [new ContentInitialComponentToe("15-derna-coastal-defense-aa.light-aa", 6, origin)]),
        });
    }

    public static ContentInitialGroup GiarabubGarrisonChartCandidate { get; } = new(
        "giarabub-garrison-source-candidate", "axis", "c1014",
        [
            "giarabub-motor-machine-gun", "giarabub-infantry", "giarabub-machine-gun",
            "giarabub-antitank", "giarabub-artillery", "giarabub-light-aa",
        ], new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-giarabub-p14"),
        ]));

    public static IReadOnlyList<ContentElementCombatFacts> GiarabubCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("giarabub-motor-machine-gun", 1, 7, "oa-giarabub-p14", 5),
            AxisInfantryFacts("giarabub-infantry", 1, 1, "oa-giarabub-p14", 1),
            AxisInfantryFacts("giarabub-machine-gun", 6, 6, "oa-giarabub-p14", 4),
            AxisWeaponFacts("giarabub-antitank", "47-32-gun", AntiTankComponentClassId, 2, 1, "oa-giarabub-p14", 1, weaponSourceProfile: Italian4732),
            AxisWeaponFacts("giarabub-artillery", "65-17-gun", ArtilleryComponentClassId, 4, 1, "oa-giarabub-p14", 1, 5, Italian6517),
            AxisAntiAirFacts("giarabub-light-aa", "light-aa", 3, "oa-giarabub-p14"),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateGiarabubToeSeeds(string scenarioId) =>
        Array.AsReadOnly(GiarabubCombatFacts.Select(facts =>
        {
            var currentToe = facts.ElementId switch
            {
                "giarabub-motor-machine-gun" or "giarabub-infantry" or "giarabub-artillery" => 1,
                "giarabub-machine-gun" => 4,
                "giarabub-antitank" => 2,
                "giarabub-light-aa" => 3,
                _ => throw new InvalidOperationException("Source Giarabub strength has not been transcribed."),
            };
            var component = facts.Components.Single();
            if (currentToe > component.MaximumToe)
                throw new InvalidOperationException("Source Giarabub strength exceeds its component maximum.");
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, currentToe, origin)]);
        }).ToArray());

    // These are single counters according to the minor-garrison OA, unlike the
    // composite Tobruk, Bardia and Giarabub formations.
    public static IReadOnlyList<ContentInitialPlacement> AxisMinorGarrisonPlacementCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            AxisMinorPlacement("scheferzen-garrison", "c3419"),
            AxisMinorPlacement("fort-maddalena-garrison", "c3019"),
            AxisMinorPlacement("el-grein-garrison", "c1715"),
            AxisMinorPlacement("mechili-garrison", "b4921"),
            AxisMinorPlacement("benina-aa-battery", "a4829"),
            AxisMinorPlacement("soluch-antitank-company", "a4130"),
            new ContentInitialPlacement("soluch-artillery-regiment", "a4130",
                new ContentOrigin(ContentOriginKind.SourceDerived,
                [new RuleReference(ScenariosSourceId, "60.31"),
                    new RuleReference(AxisChartsSourceId, "oa-minor-garrisons-p14"),
                    new RuleReference(AxisCounterSheetSourceId, "soluch-artillery-and-antitank")])),
        });

    public static IReadOnlyList<ContentElementCombatFacts> AxisMinorGarrisonCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            AxisInfantryFacts("scheferzen-garrison", 1, 2, "oa-minor-garrisons-p14", 2, 5),
            AxisInfantryFacts("fort-maddalena-garrison", 3, 3, "oa-minor-garrisons-p14", 3, 6),
            AxisInfantryFacts("el-grein-garrison", 1, 2, "oa-minor-garrisons-p14", 2),
            AxisInfantryFacts("mechili-garrison", 3, 2, "oa-minor-garrisons-p14", 1, 6),
            AxisWeaponFacts("soluch-antitank-company", "47-32-gun", AntiTankComponentClassId,
                2, 1, "oa-minor-garrisons-p14", 1, weaponSourceProfile: Italian4732),
            AxisWeaponFacts("soluch-artillery-regiment", "100-17-howitzer", ArtilleryComponentClassId,
                12, 0, "oa-minor-garrisons-p14", 1, 8, Italian10017),
            AxisAntiAirFacts("benina-aa-battery", "light-aa", 3, "oa-minor-garrisons-p14"),
        });

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateAxisMinorGarrisonToeSeeds(string scenarioId) =>
        Array.AsReadOnly(AxisMinorGarrisonCombatFacts.Select(facts =>
        {
            var currentToe = facts.ElementId switch
            {
                "scheferzen-garrison" or "el-grein-garrison" => 1,
                "soluch-artillery-regiment" => 12,
                "fort-maddalena-garrison" or "mechili-garrison" or "soluch-antitank-company" or "benina-aa-battery" => 2,
                _ => throw new InvalidOperationException("Source minor-garrison strength has not been transcribed."),
            };
            var component = facts.Components.Single();
            if (currentToe > component.MaximumToe)
                throw new InvalidOperationException("Source minor-garrison strength exceeds its component maximum.");
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, currentToe, origin)]);
        }).ToArray());

    public static IReadOnlyList<ContentElementCombatFacts> CommonwealthInitialCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            FrenchMotorMarineCombatCandidate,
            InfantryFacts("2-scots-guards", 6, 2, "oa-british-p11", 1),
            InfantryFacts("3-coldstream-guards", 6, 2, "oa-british-p11", 1),
            InfantryFacts("1-royal-northumberland-fusiliers", 3, 6, "oa-british-p11", 4),
            InfantryFacts("1-kings-royal-rifle-corps", 6, 2, "oa-7-armored-p6", 1),
            InfantryFacts("2-rifle-brigade", 6, 2, "oa-7-armored-p6", 1),
            InitialComponentFacts("1-rtr", "a9-cruiser", ArmorComponentClassId, 10, 3, "oa-7-armored-p6", true, 3, weaponSourceProfile: CommonwealthA9),
            InitialComponentFacts("11-hussars", "reconnaissance", ReconnaissanceComponentClassId, 8, 3, "oa-7-armored-p6", false, 2),
            InitialComponentFacts("3-royal-horse-artillery-antitank", "2-pounder", AntiTankComponentClassId, 8, 1, "oa-7-armored-p6", true, 1, weaponSourceProfile: Commonwealth2Pounder),
            InitialComponentFacts("4-royal-horse-artillery", "18-25-pounder", ArtilleryComponentClassId, 6, 1, "oa-7-armored-p6", true, 1, 8, Commonwealth1825Pounder),
            InitialComponentFacts("7-medium-artillery", "60-pounder", ArtilleryComponentClassId, 6, 0, "oa-british-artillery-p12", true, 1, 12, Commonwealth60Pounder),
            InitialComponentFacts("31-field-artillery", "18-pounder", ArtilleryComponentClassId, 6, 1, "oa-indian-p15", true, 0, 7, Commonwealth18Pounder),
        });

    // Partial source inventory. Includes the disputed NZ chart candidate; this
    // collection is neither a scenario artifact nor an admission decision.
    public static IReadOnlyList<ContentScheduledArrival> CommonwealthArrivalChartCandidates { get; } =
        Array.AsReadOnly(new[]
        {
            PolishBrigadeArrival, SecondRoyalTankRegimentArrival, SeventhRoyalTankRegimentArrival,
            NzFieldArtilleryArrival, FirstFieldArtilleryArrival, ThirdHussarsArrival,
            SeventhIndianBrigadeArrival, SixthNzBrigadeChartCandidate,
        });

    public static IReadOnlyList<ContentElementCombatFacts> CommonwealthArrivalCombatFacts { get; } =
        Array.AsReadOnly(new[]
        {
            NzFieldArtilleryCombatFacts, FirstFieldArtilleryCombatFacts,
            SecondRoyalTankRegimentCombatFacts, ThirdHussarsCombatFacts,
            PolishCavalryCombatFacts, PolishAntiTankCombatFacts, PolishArtilleryCombatFacts,
        }.Concat(ArrivingInfantryCombatFacts).Concat(ArrivingHeadquartersCombatFacts).ToArray());

    // Land 4.45 defines N as maximum TOE and U@1 as one point. OA weapon
    // quantities give the tank and gun seeds; 7 RTR's Matilda variant is unresolved.
    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateCommonwealthArrivalToeSeeds(string scenarioId) =>
        Array.AsReadOnly(CommonwealthArrivalCombatFacts.Select(facts =>
        {
            if (facts.Components.Count > 1)
                throw new InvalidOperationException("Source arrival seed needs an explicit component-level reading.");
            if (facts.Components.Count == 0 && facts.ElementId is not
                ("polish-brigade-hq" or "7-indian-brigade-hq" or "6-nz-brigade-hq"))
                throw new InvalidOperationException("Empty source arrival seed has not been transcribed.");
            var seeds = facts.Components.Select(component =>
            {
                var currentToe = facts.ElementId switch
                {
                    "polish-kopanski-machine-gun" or "polish-1-antitank" => 1,
                    "polish-1-infantry" or "polish-2-infantry" or "polish-3-infantry"
                        or "polish-1-cavalry" or "polish-1-artillery" or "1-royal-sussex"
                        or "4-11-sikhs" or "4-16-punjab" or "24-nz-infantry"
                        or "25-nz-infantry" or "26-nz-infantry" or NzFieldArtilleryId
                        or FirstFieldArtilleryId or SecondRoyalTankRegimentId or ThirdHussarsId
                        => component.MaximumToe,
                    _ => throw new InvalidOperationException("Source arrival seed has not been transcribed."),
                };
                var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
                return new ContentInitialComponentToe(component.ComponentId, currentToe, origin);
            });
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId, seeds);
        }).Concat(
        [
            new ContentInitialPlacementCombatFacts(scenarioId, SeventhRoyalTankRegimentId,
            [
                new ContentInitialComponentToe(SeventhRoyalTankMatildaComponentId, 10,
                    new ContentOrigin(ContentOriginKind.SourceDerived,
                    [
                        new RuleReference(CommonwealthChartsSourceId, "oa-british-p11"),
                        new RuleReference(LandRulesSourceId, "4.45"),
                    ])),
            ]),
        ]).ToArray());

    public static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateCommonwealthInitialToeSeeds(string scenarioId) =>
        Array.AsReadOnly(CommonwealthInitialCombatFacts.Select(facts =>
        {
            var currentToe = facts.ElementId switch
            {
                "1-french-motor-marines" => 1,
                "1-rtr" => 7,
                "11-hussars" or "3-royal-horse-artillery-antitank" => 6,
                "2-scots-guards" or "3-coldstream-guards" or "1-kings-royal-rifle-corps"
                    or "2-rifle-brigade" or "4-royal-horse-artillery" or "7-medium-artillery"
                    or "31-field-artillery" => 6,
                "1-royal-northumberland-fusiliers" => 3,
                _ => throw new InvalidOperationException("Source initial strength has not been transcribed."),
            };
            var component = facts.Components.Single();
            if (currentToe > component.MaximumToe)
                throw new InvalidOperationException("Source initial strength exceeds its component maximum.");
            var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
                [.. component.Origin.References, new RuleReference(LandRulesSourceId, "4.45")]);
            return new ContentInitialPlacementCombatFacts(scenarioId, facts.ElementId,
                [new ContentInitialComponentToe(component.ComponentId, currentToe, origin)]);
        }).ToArray());

    private static ContentElementCombatFacts InfantryFacts(string elementId, int maximumToe,
        int defensiveRating, string oaLocator, int offensiveRating)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(CommonwealthChartsSourceId, oaLocator),
            new RuleReference(CommonwealthChartsSourceId, "4.46a"),
        ]);
        return new ContentElementCombatFacts(elementId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{elementId}.infantry", Cna1979Combat.InfantryComponentClassId,
                maximumToe, defensiveRating, origin, offensiveCloseAssaultRating: offensiveRating)], origin);
    }

    private static ContentElementCombatFacts HeadquartersFacts(string elementId, string oaLocator) =>
        ContentElementCombatFacts.CreateScheduledHeadquarters(elementId,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(CommonwealthChartsSourceId, oaLocator),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
            ]));

    private static ContentElementCombatFacts CommonwealthTankHeadquartersFacts(string id, string weapon,
        int offensiveRating, int defensiveRating, ContentWeaponSourceProfile profile)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(CommonwealthChartsSourceId, "oa-7-armored-p6"),
                new RuleReference(CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(CommonwealthChartsSourceId, "4.47"), new RuleReference(LandRulesSourceId, "4.46")]);
        return new ContentElementCombatFacts(id, Cna1979Combat.HeadquartersClassificationId,
            [new ContentCombatComponent($"{id}.{weapon}", ArmorComponentClassId, 1, defensiveRating, origin,
                defensiveRatingParenthesized: true, offensiveCloseAssaultRating: offensiveRating,
                offensiveRatingParenthesized: true, weaponSourceProfile: profile)], origin);
    }

    private static ContentElementCombatFacts AxisInfantryFacts(string elementId, int maximumToe,
        int defensiveRating = 1, string oaLocator = "oa-tobruk-p12", int? offensiveRating = null,
        int? barrageRating = null, string? sourceId = null)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(sourceId ?? AxisChartsSourceId, oaLocator),
            new RuleReference(AxisChartsSourceId, "4.46b"),
        ]);
        return new ContentElementCombatFacts(elementId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{elementId}.infantry", Cna1979Combat.InfantryComponentClassId,
                maximumToe, defensiveRating, origin, offensiveCloseAssaultRating: offensiveRating,
                barrageRating: barrageRating)], origin);
    }

    private static ContentElementCombatFacts AxisWeaponFacts(string elementId, string componentSuffix,
        string componentClassId, int maximumToe, int defensiveRating, string oaLocator, int offensiveRating,
        int? barrageRating = null, ContentWeaponSourceProfile? weaponSourceProfile = null,
        string? sourceId = null)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(sourceId ?? AxisChartsSourceId, oaLocator),
            new RuleReference(AxisChartsSourceId, "4.46b"),
            new RuleReference(AxisChartsSourceId, "4.48"),
        ]);
        return new ContentElementCombatFacts(elementId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{elementId}.{componentSuffix}", componentClassId,
                maximumToe, defensiveRating, origin, offensiveCloseAssaultRating: offensiveRating,
                barrageRating: barrageRating, weaponSourceProfile: weaponSourceProfile)], origin);
    }

    private static ContentElementCombatFacts AxisAntiAirFacts(string elementId, string componentSuffix,
        int maximumToe, string oaLocator)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(AxisChartsSourceId, oaLocator),
            new RuleReference(AxisChartsSourceId, "4.46b"),
            new RuleReference(AxisChartsSourceId, "4.48"),
        ]);
        return new ContentElementCombatFacts(elementId, AntiAirClassificationId,
            [new ContentCombatComponent($"{elementId}.{componentSuffix}", AntiAirComponentClassId,
                maximumToe, 1, origin, defensiveRatingParenthesized: true,
                offensiveCloseAssaultRating: 1, offensiveRatingParenthesized: true)], origin);
    }

    private static ContentElementCombatFacts BardiaMixedArtilleryFacts(string elementId, string secondWeapon,
        ContentWeaponSourceProfile secondProfile, int secondBarrage)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(AxisChartsSourceId, "oa-bardia-p13"),
            new RuleReference(AxisChartsSourceId, "4.46b"),
            new RuleReference(AxisChartsSourceId, "4.48"),
        ]);
        return new ContentElementCombatFacts(elementId, Cna1979Combat.CombatUnitClassificationId,
        [
            new ContentCombatComponent($"{elementId}.100-17-howitzer", ArtilleryComponentClassId, 9, 0, origin,
                offensiveCloseAssaultRating: 1, barrageRating: 8, weaponSourceProfile: Italian10017),
            new ContentCombatComponent($"{elementId}.{secondWeapon}", ArtilleryComponentClassId, 9, 1, origin,
                offensiveCloseAssaultRating: 1, barrageRating: secondBarrage, weaponSourceProfile: secondProfile),
        ], origin, maximumCombinedToe: 9);
    }

    private static ContentElementCombatFacts LibyanSecondArtilleryFacts()
    {
        const string id = "2-libyan-artillery";
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(AxisChartsSourceId, "oa-libyan-p5"),
            new RuleReference(AxisChartsSourceId, "4.46b"), new RuleReference(AxisChartsSourceId, "4.48")]);
        return new ContentElementCombatFacts(id, Cna1979Combat.CombatUnitClassificationId,
        [
            new ContentCombatComponent($"{id}.75-27-gun", ArtilleryComponentClassId, 12, 1, origin,
                offensiveCloseAssaultRating: 1, barrageRating: 6, weaponSourceProfile: Italian7527),
            new ContentCombatComponent($"{id}.100-17-howitzer", ArtilleryComponentClassId, 12, 0, origin,
                offensiveCloseAssaultRating: 1, barrageRating: 8, weaponSourceProfile: Italian10017),
        ], origin, maximumCombinedToe: 12);
    }

    private static ContentElementCombatFacts LibyanRegimentalHeadquartersFacts(string id) =>
        AxisRegimentalHeadquartersFacts(id, "oa-libyan-p5");

    // RUL-GRA-26 planning seed: records the division HQ's one known artillery
    // TOE point without inventing a weapon model, rating, or any other
    // combat characteristic. This deliberately bypasses AxisWeaponFacts/
    // ContentCombatComponent (which require a defensive rating) because no
    // source supports assigning one; see CcnnHqUnidentifiedArtilleryComponentType.
    private static ContentInitialPlacementCombatFacts CcnnHqUnidentifiedArtillerySeed(string scenarioId, string elementId, string oaLocator)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(AxisChartsSourceId, oaLocator),
            new RuleReference(AxisChartsSourceId, "4.44b"),
            new RuleReference(AxisChartsSourceId, "4.46b"),
        ]);
        return new ContentInitialPlacementCombatFacts(scenarioId, elementId,
            [new ContentInitialComponentToe($"{elementId}.{CcnnHqUnidentifiedArtilleryComponentType}", 1, origin)]);
    }

    private static ContentElementCombatFacts AxisRegimentalHeadquartersFacts(string id, string locator)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(AxisChartsSourceId, locator),
            new RuleReference(AxisChartsSourceId, "4.46b"), new RuleReference(AxisChartsSourceId, "4.48")]);
        return new ContentElementCombatFacts(id, Cna1979Combat.HeadquartersClassificationId,
            [new ContentCombatComponent($"{id}.65-17-gun", ArtilleryComponentClassId, 1, 1, origin,
                offensiveCloseAssaultRating: 1, barrageRating: 5, weaponSourceProfile: Italian6517)], origin);
    }

    private static ContentElementCombatFacts NinthLibyanTankFacts()
    {
        const string id = "ix-libyan-light-tank";
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-libyan-p5"),
            new RuleReference(AxisChartsSourceId, "oa-libyan-tank-command-p6"),
            new RuleReference(AxisChartsSourceId, "4.46b"), new RuleReference(AxisChartsSourceId, "4.48")]);
        return new ContentElementCombatFacts(id, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{id}.cv33", ArmorComponentClassId, 9, 2, origin,
                offensiveCloseAssaultRating: 1, weaponSourceProfile: ItalianCv33)], origin);
    }

    private static ContentElementCombatFacts LibyanTankRegimentFacts(string id, bool headquarters)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.31"), new RuleReference(AxisChartsSourceId, "oa-libyan-tank-command-p6"),
                new RuleReference(AxisChartsSourceId, "4.46b"), new RuleReference(AxisChartsSourceId, "4.48"),
                new RuleReference(LandRulesSourceId, "4.46")]);
        return new ContentElementCombatFacts(id, headquarters ? Cna1979Combat.HeadquartersClassificationId
                : Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{id}.cv33", ArmorComponentClassId, headquarters ? 1 : 9, 2, origin,
                defensiveRatingParenthesized: headquarters, offensiveCloseAssaultRating: 1,
                offensiveRatingParenthesized: headquarters, weaponSourceProfile: ItalianCv33)], origin);
    }

    private static ContentElementCombatFacts MalettiCamelFacts(string id)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(AxisChartsSourceId, "oa-maletti-p7"), new RuleReference(AxisChartsSourceId, "4.46b")]);
        return new ContentElementCombatFacts(id, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{id}.reconnaissance", ReconnaissanceComponentClassId, 5, 1,
                origin, offensiveCloseAssaultRating: 1)], origin);
    }

    private static ContentElementCombatFacts FirstCcnnBersaglieriFacts()
    {
        const string id = "201-ccnn-bersaglieri";
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(AxisChartsSourceId, "oa-ccnn-p17"), new RuleReference(AxisChartsSourceId, "4.46b")]);
        return new ContentElementCombatFacts(id, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{id}.reconnaissance", ReconnaissanceComponentClassId, 1, 3,
                origin, offensiveCloseAssaultRating: 4)], origin);
    }

    private static ContentElementCombatFacts FirstCcnnArtilleryFacts() =>
        AxisHeavyDivisionArtilleryFacts("201-ccnn-artillery", "oa-ccnn-p17",
            "75-27-gun", Italian7527, 1, 6, "100-17-howitzer", Italian10017, 0, 8);

    private static ContentElementCombatFacts AxisHeavyDivisionArtilleryFacts(string id, string locator,
        string firstWeapon, ContentWeaponSourceProfile firstProfile, int firstDefense, int firstBarrage,
        string secondWeapon, ContentWeaponSourceProfile secondProfile, int secondDefense, int secondBarrage)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(AxisChartsSourceId, locator), new RuleReference(AxisChartsSourceId, "4.46b"),
                new RuleReference(AxisChartsSourceId, "4.48")]);
        var first = $"{id}.{firstWeapon}";
        var second = $"{id}.{secondWeapon}";
        return new ContentElementCombatFacts(id, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent(first, ArtilleryComponentClassId, 9, firstDefense, origin,
                offensiveCloseAssaultRating: 1, barrageRating: firstBarrage, weaponSourceProfile: firstProfile),
                new ContentCombatComponent(second, ArtilleryComponentClassId, 9, secondDefense, origin,
                    offensiveCloseAssaultRating: 1, barrageRating: secondBarrage, weaponSourceProfile: secondProfile),
                new ContentCombatComponent($"{id}.heavy-aa", AntiAirComponentClassId, 3, 1, origin,
                    defensiveRatingParenthesized: true, offensiveCloseAssaultRating: 1,
                    offensiveRatingParenthesized: true, weaponSourceProfile: ItalianHeavyAa)], origin,
            maximumCombinedToe: 12,
            componentToeLimits: [new ContentComponentToeLimit("artillery", [first, second], 9, origin)]);
    }

    private static ContentElementCombatFacts DivisionArtilleryFacts(string elementId, string locator,
        string firstWeapon, ContentWeaponSourceProfile firstProfile, int firstDefense, int firstBarrage,
        string secondWeapon, ContentWeaponSourceProfile secondProfile, int secondDefense, int secondBarrage,
        string? sourceId = null)
    {
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived,
        [new RuleReference(sourceId ?? ErrataSourceId, locator), new RuleReference(AxisChartsSourceId, "4.46b"),
            new RuleReference(AxisChartsSourceId, "4.48")]);
        var firstId = $"{elementId}.{firstWeapon}";
        var secondId = $"{elementId}.{secondWeapon}";
        return new ContentElementCombatFacts(elementId, Cna1979Combat.CombatUnitClassificationId,
        [
            new ContentCombatComponent(firstId, ArtilleryComponentClassId, 9, firstDefense, origin,
                offensiveCloseAssaultRating: 1, barrageRating: firstBarrage, weaponSourceProfile: firstProfile),
            new ContentCombatComponent(secondId, ArtilleryComponentClassId, 9, secondDefense, origin,
                offensiveCloseAssaultRating: 1, barrageRating: secondBarrage, weaponSourceProfile: secondProfile),
            new ContentCombatComponent($"{elementId}.light-aa", AntiAirComponentClassId, 3, 1, origin,
                defensiveRatingParenthesized: true, offensiveCloseAssaultRating: 1,
                offensiveRatingParenthesized: true, weaponSourceProfile: ItalianLightAa),
        ], origin, maximumCombinedToe: 12,
            componentToeLimits: [new ContentComponentToeLimit("artillery", [firstId, secondId], 9, origin)]);
    }

    private static ContentElementCombatFacts InitialComponentFacts(string elementId, string componentSuffix,
        string componentClassId, int maximumToe, int defensiveRating, string oaLocator, bool weaponChart, int offensiveRating,
        int? barrageRating = null, ContentWeaponSourceProfile? weaponSourceProfile = null)
    {
        var references = new List<RuleReference>
        {
            new(CommonwealthChartsSourceId, oaLocator),
            new(CommonwealthChartsSourceId, "4.46a"),
        };
        if (weaponChart) references.Add(new RuleReference(CommonwealthChartsSourceId, "4.47"));
        var origin = new ContentOrigin(ContentOriginKind.SourceDerived, references);
        return new ContentElementCombatFacts(elementId, Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent($"{elementId}.{componentSuffix}", componentClassId,
                maximumToe, defensiveRating, origin, offensiveCloseAssaultRating: offensiveRating,
                barrageRating: barrageRating, weaponSourceProfile: weaponSourceProfile)], origin);
    }

    private static ContentInitialPlacement InitialPlacement(string elementId, string locationId) =>
        new(elementId, locationId, new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(ScenariosSourceId, "60.41")]));

    private static ContentInitialPlacement AxisMinorPlacement(string elementId, string locationId) =>
        new(elementId, locationId, new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(ScenariosSourceId, "60.31"),
            new RuleReference(AxisChartsSourceId, "oa-minor-garrisons-p14"),
        ]));
}
