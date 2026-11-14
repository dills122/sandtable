using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>A source reading, not a certificate of playable movement or complete setup.</summary>
public sealed record GrazianiCounterAllowance(string ElementId, int CapabilityPointAllowance, ContentOrigin Origin,
    bool RetainsAllowanceRegardlessOfAssignedToe = false);

/// <summary>Printed stacking number and, for an HQ, its value with no attached combat units.</summary>
public sealed record GrazianiCounterStackingFact(string ElementId, string SideId, string CharacteristicsCode,
    int PrintedValue, bool PrintedValueIsFormationConditional, int? UnattachedHeadquartersValue, ContentOrigin Origin);

/// <summary>Counter allowances joined from the Commonwealth OA and unit-characteristics charts.</summary>
public static class Cna1979GrazianiCounterFacts
{
    // Printed counter values are evidence, not a resolved attachment layout. In
    // particular, the HQ's parenthesized 3 represents its brigade, not 3 plus
    // its attached units; without combat attachments its stacking value is 0.
    public static IReadOnlyList<GrazianiCounterStackingFact> PolishBrigadeStacking { get; } = Array.AsReadOnly(new[]
    {
        PolishStacking("polish-brigade-hq", "a", 3, true, 0),
        PolishStacking("polish-1-infantry", "s", 1),
        PolishStacking("polish-2-infantry", "s", 1),
        PolishStacking("polish-3-infantry", "s", 1),
        PolishStacking("polish-kopanski-machine-gun", "w", 0),
        PolishStacking("polish-1-cavalry", "ss", 1),
        PolishStacking("polish-1-antitank", "dd", 0),
        PolishStacking("polish-1-artillery", "y", 1),
    });

    public static IReadOnlyList<GrazianiCounterAllowance> CairoBrigadeCandidates { get; } = Array.AsReadOnly(new[]
    {
        Fact("16-infantry-brigade-hq", 30, "oa-70-infantry-p9", "c"),
        Fact("2-queens-royal-regiment", 10, "oa-70-infantry-p9", "m"),
        Fact("2-leicesters", 10, "oa-70-infantry-p9", "m"),
        Fact("1-argyll-sutherland", 10, "oa-70-infantry-p9", "m"),
        Fact("7-indian-brigade-hq", 30, "oa-indian-p15", "a"),
        Fact("1-royal-sussex", 10, "oa-indian-p15", "p"),
        Fact("4-11-sikhs", 10, "oa-indian-p15", "p"),
        Fact("4-16-punjab", 10, "oa-indian-p15", "p"),
        Fact("polish-brigade-hq", 30, "oa-polish-p21", "a"),
        Fact("polish-1-infantry", 10, "oa-polish-p21", "s"),
        Fact("polish-2-infantry", 10, "oa-polish-p21", "s"),
        Fact("polish-3-infantry", 10, "oa-polish-p21", "s"),
        Fact("polish-kopanski-machine-gun", 8, "oa-polish-p21", "w"),
        Fact("polish-1-cavalry", 45, "oa-polish-p21", "ss"),
        Fact("polish-1-antitank", 15, "oa-polish-p21", "dd"),
        Fact("polish-1-artillery", 20, "oa-polish-p21", "y"),
    });

    // These are printed base allowances, not a ruling on motorization, weapon
    // effects, or the disputed 6th NZ Brigade arrival date.
    public static IReadOnlyList<GrazianiCounterAllowance> AdditionalArrivalCandidates { get; } = Array.AsReadOnly(new[]
    {
        Fact(Cna1979GrazianiSourceFacts.NzFieldArtilleryId, 20, "oa-nz-p18", "x"),
        Fact(Cna1979GrazianiSourceFacts.FirstFieldArtilleryId, 20, "oa-indian-p15", "x"),
        Fact(Cna1979GrazianiSourceFacts.SecondRoyalTankRegimentId, 25, "oa-british-p11", "g"),
        Fact(Cna1979GrazianiSourceFacts.SeventhRoyalTankRegimentId, 25, "oa-british-p11", "g"),
        Fact(Cna1979GrazianiSourceFacts.ThirdHussarsId, 25, "oa-british-p11", "g"),
        Fact("6-nz-brigade-hq", 30, "oa-nz-p18", "a"),
        Fact("24-nz-infantry", 10, "oa-nz-p18", "m"),
        Fact("25-nz-infantry", 10, "oa-nz-p18", "m"),
        Fact("26-nz-infantry", 10, "oa-nz-p18", "m"),
    });

    // Central India Horse is deliberately absent: its initial training state
    // determines which source row applies and has not been established.
    public static IReadOnlyList<GrazianiCounterAllowance> FourthIndianInitialCandidates { get; } = Array.AsReadOnly(new[]
    {
        Fact("4-indian-division-hq", 30, "oa-indian-p15", "a"),
        Fact("5-indian-brigade-hq", 30, "oa-indian-p15", "a"),
        Fact("11-indian-brigade-hq", 30, "oa-indian-p15", "a"),
        Fact("1-royal-fusiliers", 10, "oa-indian-p15", "p"),
        Fact("3-1-punjab", 10, "oa-indian-p15", "p"),
        Fact("4-6-rajputana", 10, "oa-indian-p15", "p"),
        Fact("2-camerons", 10, "oa-indian-p15", "p"),
        Fact("1-6-rajputana", 10, "oa-indian-p15", "p"),
        Fact("4-7-rajput", 10, "oa-indian-p15", "p"),
        Fact("25-field-artillery", 20, "oa-indian-p15", "x"),
    });

    public static IReadOnlyList<GrazianiCounterAllowance> SeventhArmoredInitialCandidates { get; } = Array.AsReadOnly(new[]
    {
        Fact("7-armored-division-hq", 30, "oa-7-armored-p6", "b", fixedAllowance: true),
        Fact("4-armored-brigade-hq", 30, "oa-7-armored-p6", "b", fixedAllowance: true),
        Fact("6-rtr", 25, "oa-7-armored-p6", "g"),
        Fact("7-queens-own-hussars", 25, "oa-7-armored-p6", "g"),
        Fact("7-armored-brigade-hq", 20, "oa-7-armored-p6", "d", fixedAllowance: true),
        Fact("8-hussars", 25, "oa-7-armored-p6", "g"),
        Fact("7-support-group-hq", 30, "oa-7-armored-p6", "a"),
    });

    private static GrazianiCounterStackingFact PolishStacking(string id, string code, int printed,
        bool formationConditional = false, int? unattachedHeadquartersValue = null) =>
        new(id, "commonwealth", code, printed, formationConditional, unattachedHeadquartersValue,
            new ContentOrigin(ContentOriginKind.SourceDerived,
            [
                new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-polish-p21"),
                new RuleReference(Cna1979GrazianiSourceFacts.AirLogisticsSourceId, "counter-section-5-p42"),
                new RuleReference(Cna1979GrazianiSourceFacts.LandRulesSourceId, "9.12-9.25"),
            ]));

    private static GrazianiCounterAllowance Fact(string id, int allowance, string oaLocator, string code,
        bool fixedAllowance = false) =>
        new(id, allowance, new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, oaLocator),
            new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a-id-" + code),
        ]), fixedAllowance);
}
