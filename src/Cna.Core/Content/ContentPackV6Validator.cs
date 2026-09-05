using Cna.Core.Rules;

namespace Cna.Core.Content;

internal static class BreakdownCapabilityDiagnostics
{
    public const string Identity = "BRK-CERT-001";
    public const string Cohort = "BRK-CERT-002";
    public const string Organization = "BRK-CERT-003";
    public const string Facts = "BRK-CERT-004";
    public const string Conservation = "BRK-CERT-005";
    public const string Flow = "BRK-CERT-006";
}

internal static class ContentPackV6Validator
{
    private static readonly string[] RequiredCapabilities =
    [
        "land.breakdown-cohorts",
        "land.element-mobility",
        "land.formations",
        "land.hex-topology",
        "land.initial-deployment",
        "land.weather-areas",
    ];

    public static ContentValidationResult Validate(ContentPackV6Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = new List<ContentValidationIssue>();
        var legacy = definition.LegacyDefinition;
        var hasCohorts = legacy.Elements.Any(element => element.BreakdownVehicleCohort is not null);
        var requiredCapabilities = RequiredCapabilities.Where(capability =>
            capability != "land.breakdown-cohorts" || hasCohorts);
        if (definition.CapabilityProfileId != ContentPackV6Definition.SupportedCapabilityProfileId
            || legacy.RulesetId != Cna1979Ruleset.RulesetId
            || !legacy.Capabilities.SequenceEqual(requiredCapabilities, StringComparer.Ordinal))
        {
            Add(issues, BreakdownCapabilityDiagnostics.Identity, "/capabilityProfileId",
                "Content requires the supported profile, ruleset and exact predecessor capability set.");
        }

        // Reuse frozen structural, source-reference, vocabulary and initial-TOE checks without
        // manufacturing components or changing any predecessor artifact bytes.
        var predecessor = new ContentPackV5Definition(
            legacy, definition.ElementCombatFacts, definition.InitialPlacementCombatFacts);
        foreach (var issue in ContentPackV5Validator.Validate(predecessor, allowEmptyNoncombat: true).Issues
            .Concat(Cna1979ContentV5CompatibilityValidator.Validate(predecessor).Issues))
        {
            Add(issues, DiagnosticFor(issue), issue.Path, issue.Message);
        }

        var elements = legacy.Elements.GroupBy(element => element.ElementId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var facts = definition.ElementCombatFacts.GroupBy(value => value.ElementId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        foreach (var formation in legacy.Formations)
        {
            var children = legacy.Elements.Where(element => element.ParentFormationId == formation.FormationId).ToArray();
            if (formation.ParentFormationId is not null
                || formation.OrganizationId != "land.organization.battalion"
                || children.Length != 1
                || children[0].PlacementMode != ContentPlacementMode.Independent)
            {
                Add(issues, BreakdownCapabilityDiagnostics.Organization, $"/formations/{formation.FormationId}",
                    "Every formation must be a root battalion with exactly one independent child.");
            }
        }

        foreach (var element in legacy.Elements)
        {
            var path = $"/elements/{element.ElementId}";
            if (element.OrganizationId != "land.organization.battalion"
                || element.PlacementMode != ContentPlacementMode.Independent)
            {
                Add(issues, BreakdownCapabilityDiagnostics.Organization, path,
                    "Every element must be an independently represented battalion.");
            }

            if (!facts.TryGetValue(element.ElementId, out var combat))
                continue;
            var isTruck = combat.CombatClassificationId == Cna1979Combat.TruckConvoyClassificationId;
            if (isTruck)
            {
                if (element.BreakdownVehicleCohort is not { } cohort
                    || cohort.VehicleTypeId != Cna1979Breakdown.VehicleTypeTruckId
                    || cohort.ProfileId != Cna1979Breakdown.ProfileTruckId
                    || combat.Components.Count != 0)
                {
                    Add(issues, BreakdownCapabilityDiagnostics.Cohort, path,
                        "Standalone Trucks require one supported Truck cohort and no combat components.");
                }
            }
            else if (element.BreakdownVehicleCohort is not null
                || combat.CombatClassificationId is not (Cna1979Combat.CombatUnitClassificationId
                    or Cna1979Combat.HeadquartersClassificationId))
            {
                Add(issues, BreakdownCapabilityDiagnostics.Cohort, path,
                    "Only standalone Trucks may have cohorts; other elements must be combat or headquarters units.");
            }

            if (!isTruck && combat.Components.Count == 0)
            {
                Add(issues, BreakdownCapabilityDiagnostics.Facts, path,
                    "Combat and headquarters elements require supported combat components.");
            }

            if (element.MobilityId != (isTruck
                ? Cna1979Movement.MotorizedMobilityId
                : Cna1979Movement.NonMotorizedMobilityId))
            {
                Add(issues, BreakdownCapabilityDiagnostics.Facts, $"{path}/mobilityId",
                    "Only Trucks use motorized movement in this profile.");
            }

            foreach (var location in legacy.Locations)
            {
                if (!Cna1979Movement.LookupTerrain(location.TerrainId, element.MobilityId).IsSupported)
                {
                    Add(issues, BreakdownCapabilityDiagnostics.Facts, $"/locations/{location.LocationId}",
                        "Every static terrain/mobility pair requires supported movement facts.");
                }
            }
        }

        foreach (var scenario in legacy.Scenarios)
        {
            var path = $"/scenarios/{scenario.ScenarioId}";
            if (scenario.Origin.Kind != ContentOriginKind.Synthetic)
            {
                Add(issues, BreakdownCapabilityDiagnostics.Identity, path,
                    "Only synthetic scenarios are supported.");
            }

            var placed = scenario.InitialPlacements.Where(placement => elements.ContainsKey(placement.ElementId)).ToArray();
            foreach (var group in placed.Where(placement => elements[placement.ElementId].BreakdownVehicleCohort is not null)
                .GroupBy(placement => elements[placement.ElementId].SideId, StringComparer.Ordinal))
            {
                if (group.Select(placement => placement.ElementId).Distinct(StringComparer.Ordinal).Count() > 1)
                {
                    Add(issues, BreakdownCapabilityDiagnostics.Cohort, path,
                        "Each scenario admits at most one vehicle cohort per side.");
                }
            }

            foreach (var stack in placed.Where(placement => facts.TryGetValue(placement.ElementId, out var combat)
                    && combat.CombatClassificationId is Cna1979Combat.CombatUnitClassificationId
                        or Cna1979Combat.HeadquartersClassificationId)
                .GroupBy(placement => (elements[placement.ElementId].SideId, placement.LocationId)))
            {
                var stackingValue = stack.Select(placement => placement.ElementId)
                    .Distinct(StringComparer.Ordinal).Sum(elementId =>
                    {
                        var rule = Cna1979Movement.LookupStackingValue(elements[elementId].OrganizationId);
                        return rule.IsSupported ? (long)rule.Value.StackingValue : 2;
                    });
                if (stackingValue > 1)
                {
                    Add(issues, BreakdownCapabilityDiagnostics.Organization, path,
                        "Same-side co-located combat and headquarters leaves may total at most one battalion.");
                }
            }
        }

        return new ContentValidationResult(issues);
    }

    private static string DiagnosticFor(ContentValidationIssue issue)
    {
        if (issue.Path.Contains("/origin/references/", StringComparison.Ordinal))
            return BreakdownCapabilityDiagnostics.Facts;
        if (issue.Path.Contains("breakdownVehicleCohort", StringComparison.Ordinal)
            || issue.Code.Contains("breakdown-cohort", StringComparison.Ordinal))
            return BreakdownCapabilityDiagnostics.Cohort;
        if (issue.Path.Contains("/formations/", StringComparison.Ordinal)
            || issue.Path.EndsWith("/parentFormationId", StringComparison.Ordinal)
            || issue.Path.EndsWith("/organizationId", StringComparison.Ordinal)
            || issue.Code.StartsWith("placement.", StringComparison.Ordinal))
            return BreakdownCapabilityDiagnostics.Organization;
        if (issue.Path.Contains("/capabilities", StringComparison.Ordinal)
            || issue.Path == "/rulesetId")
            return BreakdownCapabilityDiagnostics.Identity;
        return BreakdownCapabilityDiagnostics.Facts;
    }

    private static void Add(List<ContentValidationIssue> issues, string code, string path, string message) =>
        issues.Add(new ContentValidationIssue(code, path, message));
}
