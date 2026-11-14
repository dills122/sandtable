using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Source-linked Cairo excerpt only, not a complete campaign or playable map.
/// Mobility, operational defaults, and source review remain unresolved.</summary>
public static class Cna1979GrazianiCairoSourceExcerpt
{
    public const string ScenarioId = "graziani-cairo-source-excerpt";
    public static ContentSourceScheduledCombatPackArtifact Build()
    {
        var scenarioOrigin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.22"),
                new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41")]);
        var chartOrigin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a"),
                new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-70-infantry-p9"),
                new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-indian-p15")]);
        var mapOrigin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference("spi-1979-map-e", "cairo-city-hexes"),
                new RuleReference(Cna1979GrazianiSourceFacts.AirLogisticsSourceId, "important-locations-p26")]);
        var initial = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate;
        var arrival = Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival;
        var polish = Cna1979GrazianiSourceFacts.PolishBrigadeArrival;
        var scenario = new ContentScenario(ScenarioId, new(1, 1), new(6, 3), [], [arrival, polish],
            [Cna1979GrazianiSourceFacts.CairoEntryArea], [initial], scenarioOrigin);
        var initialFormation = new ContentFormation("16-infantry-brigade", "commonwealth", null,
            Cna1979SourceContentVocabulary.BrigadeOrganizationId, chartOrigin);
        var arrivingFormation = new ContentFormation("7-indian-brigade", "commonwealth", null,
            Cna1979SourceContentVocabulary.BrigadeOrganizationId, chartOrigin);
        var polishOrigin = new ContentOrigin(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-polish-p21")]);
        var polishFormation = new ContentFormation("polish-brigade", "commonwealth", null,
            Cna1979SourceContentVocabulary.BrigadeOrganizationId, polishOrigin);
        var elements = initial.ElementIds.Select(id => Element(id, initialFormation.FormationId))
            .Concat(arrival.ElementIds.Select(id => Element(id, arrivingFormation.FormationId)))
            .Concat(polish.ElementIds.Select(id => Element(id, polishFormation.FormationId))).ToArray();
        var pack = new ContentPackDefinition(ContentPackDefinition.CurrentSchemaVersion, ContentPackDefinition.CanonicalFormatId,
            "graziani-cairo-source-excerpt", Cna1979Ruleset.RulesetId,
            ["land.hex-topology", "land.formations", "land.element-mobility", "land.initial-deployment"],
            [new(Cna1979GrazianiSourceFacts.ScenariosSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.LandRulesSourceId, ContentSourceKind.PublishedPrimary),
                new(Cna1979GrazianiSourceFacts.AirLogisticsSourceId, ContentSourceKind.PublishedPrimary),
                new("spi-1979-map-e", ContentSourceKind.PublishedPrimary)],
            Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds.Select(id =>
                new ContentHex(id, Cna1979SourceContentVocabulary.MajorCityTerrainId, null, mapOrigin)), [], [],
            [initialFormation, arrivingFormation, polishFormation], elements, [scenario]);
        var facts = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeCombatCandidates
            .Concat(Cna1979GrazianiSourceFacts.CommonwealthArrivalCombatFacts)
            .Where(value => initial.ElementIds.Contains(value.ElementId) || arrival.ElementIds.Contains(value.ElementId)
                || polish.ElementIds.Contains(value.ElementId));
        var seeds = Cna1979GrazianiSourceFacts.CreateSixteenthInfantryBrigadeToeCandidates(ScenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateCommonwealthArrivalToeSeeds(ScenarioId))
            .Where(value => initial.ElementIds.Contains(value.ElementId) || arrival.ElementIds.Contains(value.ElementId)
                || polish.ElementIds.Contains(value.ElementId));
        try { return ContentSourceScheduledCombatPackArtifact.Create(new ContentPackV5Definition(pack, facts, seeds)); }
        catch (InvalidContentPackException error)
        {
            throw new InvalidOperationException(string.Join("; ", error.Issues.Select(value => $"{value.Code}: {value.Path}")), error);
        }
    }

    private static ContentCombatElement Element(string id, string parent)
    {
        var organization = id switch
        {
            _ when id.EndsWith("-hq", StringComparison.Ordinal) => Cna1979SourceContentVocabulary.BrigadeOrganizationId,
            "polish-kopanski-machine-gun" or "polish-1-antitank" => Cna1979SourceContentVocabulary.CompanyOrganizationId,
            "polish-1-cavalry" or "polish-1-artillery" => Cna1979SourceContentVocabulary.RegimentOrganizationId,
            _ => "land.organization.battalion",
        };
        var allowance = Cna1979GrazianiCounterFacts.CairoBrigadeCandidates.Single(value => value.ElementId == id);
        return new ContentCombatElement(id, "commonwealth", parent,
            organization,
            Cna1979SourceContentVocabulary.UnresolvedMobilityId, allowance.CapabilityPointAllowance,
            ContentPlacementMode.Independent, allowance.Origin);
    }
}
