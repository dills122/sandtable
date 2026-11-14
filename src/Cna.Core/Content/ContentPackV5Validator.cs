namespace Cna.Core.Content;

public static class ContentPackV5Validator
{
    public static ContentValidationResult Validate(ContentPackV5Definition definition)
        => Validate(definition, allowEmptyNoncombat: false);

    internal static ContentValidationResult Validate(
        ContentPackV5Definition definition,
        bool allowEmptyNoncombat) => ValidateCore(definition, allowEmptyNoncombat, scheduled: false, requireCompatibility: false);

    // Checks content and current combat vocabulary only; it does not admit a campaign.
    public static ContentValidationResult ValidateScheduledContent(ContentPackV5Definition definition)
        => ValidateCore(definition, allowEmptyNoncombat: false, scheduled: true, requireCompatibility: true);

    internal static ContentValidationResult ValidateScheduledStructure(ContentPackV5Definition definition,
        bool allowEmptyNoncombat = false) => ValidateCore(definition, allowEmptyNoncombat, scheduled: true, requireCompatibility: false);

    internal static ContentValidationResult ValidateSourceScheduledStructure(ContentPackV5Definition definition,
        bool allowEmptyNoncombat = false) => ValidateCore(definition, allowEmptyNoncombat, scheduled: true,
            requireCompatibility: false, source: true);

    private static ContentValidationResult ValidateCore(ContentPackV5Definition definition,
        bool allowEmptyNoncombat, bool scheduled, bool requireCompatibility, bool source = false)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = new List<ContentValidationIssue>();
        issues.AddRange((scheduled ? source ? ContentScheduledPackSerializer.ValidateSource(definition.LegacyDefinition)
                : ContentScheduledPackSerializer.Validate(definition.LegacyDefinition)
            : ContentPackValidator.Validate(definition.LegacyDefinition)).Issues);

        ValidateLegacyBoundary(definition, issues);
        ValidateCombatFacts(definition, issues);
        ValidatePlacementSeeds(definition, issues, scheduled);
        ValidateOrigins(definition, issues);
        if (requireCompatibility)
            issues.AddRange(Cna1979ContentV5CompatibilityValidator.Validate(definition).Issues);

        if (!scheduled)
        {
            foreach (var facts in definition.ElementCombatFacts.Where(value => value.ComponentToeLimits.Count != 0))
                Add(issues, "content.combat.component-limits-unsupported", $"/elements/{facts.ElementId}/componentToeLimits",
                    "This capability cannot encode component TOE limits.");
            foreach (var facts in definition.ElementCombatFacts.Where(value => value.MaximumCombinedToe is not null))
                Add(issues, "content.combat.combined-toe-unsupported", $"/elements/{facts.ElementId}/maximumCombinedToe",
                    "This capability cannot encode a combined TOE limit.");
            foreach (var facts in definition.ElementCombatFacts)
                foreach (var component in facts.Components.Where(value => value.DefensiveRatingParenthesized))
                    Add(issues, "content.combat.parenthesized-defense-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/defensiveCloseAssaultRating",
                        "This capability cannot interpret a parenthesized defensive rating.");
            foreach (var facts in definition.ElementCombatFacts)
                foreach (var component in facts.Components.Where(value => value.OffensiveCloseAssaultRating is not null))
                    Add(issues, "content.combat.offensive-rating-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/offensiveCloseAssaultRating",
                        "This capability cannot encode an offensive rating.");
            foreach (var facts in definition.ElementCombatFacts)
                foreach (var component in facts.Components.Where(value => value.BarrageRating is not null))
                    Add(issues, "content.combat.barrage-rating-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/barrageRating",
                        "This capability cannot encode a barrage rating.");
            foreach (var facts in definition.ElementCombatFacts)
                foreach (var component in facts.Components.Where(value => value.WeaponSourceProfile is not null))
                    Add(issues, "content.combat.weapon-source-profile-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/weaponSourceProfile",
                        "This capability cannot encode a weapon source profile.");
        }

        foreach (var facts in definition.ElementCombatFacts.Where(facts =>
            facts.Components.Count == 0
            && !(allowEmptyNoncombat
                && facts.CombatClassificationId == Rules.Cna1979Combat.TruckConvoyClassificationId)
            && !(scheduled
                && facts.CombatClassificationId == Rules.Cna1979Combat.HeadquartersClassificationId)))
        {
            Add(issues, "content.combat.missing-components", $"/elements/{facts.ElementId}/components",
                "At least one combat component is required by this contract.");
        }

        return new ContentValidationResult(issues);
    }

    private static void ValidateLegacyBoundary(
        ContentPackV5Definition definition,
        List<ContentValidationIssue> issues)
    {
        if (definition.LegacyDefinition.Capabilities.Contains(
            ContentPackV5Definition.CombatCapabilityId,
            StringComparer.Ordinal))
        {
            Add(
                issues,
                "content.v5.mixed-legacy-capability",
                $"/legacyDefinition/capabilities/{ContentPackV5Definition.CombatCapabilityId}",
                "The dormant combat capability belongs to the v5 envelope, not the legacy definition.");
        }
    }

    private static void ValidateCombatFacts(
        ContentPackV5Definition definition,
        List<ContentValidationIssue> issues)
    {
        var elements = definition.LegacyDefinition.Elements
            .Select(element => element.ElementId)
            .ToHashSet(StringComparer.Ordinal);
        var factsByElement = definition.ElementCombatFacts
            .GroupBy(value => value.ElementId, StringComparer.Ordinal)
            .ToArray();

        foreach (var group in factsByElement)
        {
            if (group.Count() > 1)
            {
                Add(
                    issues,
                    "content.combat.duplicate-element",
                    $"/elements/{group.Key}/combat",
                    $"Element '{group.Key}' declares combat facts more than once.");
            }

            if (!elements.Contains(group.Key))
            {
                Add(
                    issues,
                    "content.combat.unknown-element",
                    $"/elements/{group.Key}/combat",
                    $"Combat facts reference unknown element '{group.Key}'.");
            }
        }

        foreach (var elementId in elements.Except(
            factsByElement.Select(group => group.Key),
            StringComparer.Ordinal))
        {
            Add(
                issues,
                "content.combat.missing-element",
                $"/elements/{elementId}/combat",
                $"Element '{elementId}' requires one combat fact declaration.");
        }

        foreach (var group in definition.ElementCombatFacts
            .SelectMany(value => value.Components)
            .GroupBy(component => component.ComponentId, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                Add(
                    issues,
                    "content.combat.duplicate-component",
                    $"/components/{group.Key}",
                    $"Component ID '{group.Key}' must be unique across the Content Pack.");
            }
        }

        foreach (var facts in definition.ElementCombatFacts)
        {
            var ids = facts.Components.Select(value => value.ComponentId).ToHashSet(StringComparer.Ordinal);
            foreach (var duplicate in facts.ComponentToeLimits.GroupBy(value => value.LimitId, StringComparer.Ordinal)
                .Where(group => group.Count() > 1))
                Add(issues, "content.combat.duplicate-component-limit", $"/elements/{facts.ElementId}/componentToeLimits/{duplicate.Key}",
                    "Component TOE limit IDs must be unique within an element.");
            foreach (var limit in facts.ComponentToeLimits)
                foreach (var id in limit.ComponentIds.Where(id => !ids.Contains(id)))
                    Add(issues, "content.combat.unknown-limited-component", $"/elements/{facts.ElementId}/componentToeLimits/{limit.LimitId}/{id}",
                        "A component TOE limit must reference a component of the same element.");
        }
    }

    private static void ValidatePlacementSeeds(
        ContentPackV5Definition definition,
        List<ContentValidationIssue> issues,
        bool scheduled)
    {
        var placements = definition.LegacyDefinition.Scenarios
            .SelectMany(scenario => scenario.InitialPlacements.Select(placement => (
                scenario.ScenarioId,
                placement.ElementId)))
            .ToHashSet();
        if (scheduled)
        {
            placements.UnionWith(definition.LegacyDefinition.Scenarios.SelectMany(scenario =>
                scenario.ScheduledArrivals.SelectMany(arrival => arrival.ElementIds
                    .Select(elementId => (scenario.ScenarioId, ElementId: elementId)))));
            placements.UnionWith(definition.LegacyDefinition.Scenarios.SelectMany(scenario =>
                scenario.InitialGroups.SelectMany(group => group.ElementIds
                    .Select(elementId => (scenario.ScenarioId, ElementId: elementId)))));
        }
        var factsByElement = definition.ElementCombatFacts
            .GroupBy(value => value.ElementId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var seedGroups = definition.InitialPlacementCombatFacts
            .GroupBy(value => (value.ScenarioId, value.ElementId))
            .ToArray();

        foreach (var group in seedGroups)
        {
            var path = PlacementPath(group.Key.ScenarioId, group.Key.ElementId);
            if (group.Count() > 1)
            {
                Add(
                    issues,
                    "content.initial-toe.duplicate-placement",
                    path,
                    "An initial placement may declare component TOE seeds only once.");
            }

            if (!placements.Contains(group.Key))
            {
                Add(
                    issues,
                    "content.initial-toe.unknown-placement",
                    path,
                    "Component TOE seeds must bind an existing scenario placement.");
                continue;
            }

            if (group.Count() != 1
                || !factsByElement.TryGetValue(group.Key.ElementId, out var elementFacts))
            {
                continue;
            }

            ValidatePlacementSeedSet(group.Single(), elementFacts, path, issues);
        }

        foreach (var missing in placements.Except(seedGroups.Select(group => group.Key)))
        {
            Add(
                issues,
                "content.initial-toe.missing-placement",
                PlacementPath(missing.ScenarioId, missing.ElementId),
                scheduled
                    ? "Every initially placed, grouped or arriving element requires one component TOE seed declaration."
                    : "Every initial placement requires one component TOE seed declaration.");
        }
    }

    private static void ValidatePlacementSeedSet(
        ContentInitialPlacementCombatFacts placement,
        ContentElementCombatFacts elementFacts,
        string path,
        List<ContentValidationIssue> issues)
    {
        var components = elementFacts.Components
            .GroupBy(component => component.ComponentId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var seedGroups = placement.InitialComponentToes
            .GroupBy(seed => seed.ComponentId, StringComparer.Ordinal)
            .ToArray();

        if (elementFacts.MaximumCombinedToe is { } maximum
            && placement.InitialComponentToes.Sum(seed => (long)seed.CurrentToe) > maximum)
            Add(issues, "content.initial-toe.over-combined-maximum", path,
                "The sum of component TOE seeds exceeds the element's combined maximum.");

        foreach (var limit in elementFacts.ComponentToeLimits)
        {
            var ids = limit.ComponentIds.ToHashSet(StringComparer.Ordinal);
            if (placement.InitialComponentToes.Where(seed => ids.Contains(seed.ComponentId))
                .Sum(seed => (long)seed.CurrentToe) > limit.MaximumToe)
                Add(issues, "content.initial-toe.over-component-limit", $"{path}/componentToeLimits/{limit.LimitId}",
                    "The sum of component TOE seeds exceeds the component limit.");
        }

        foreach (var group in seedGroups)
        {
            var seedPath = $"{path}/initialComponentToes/{group.Key}";
            if (group.Count() > 1)
            {
                Add(
                    issues,
                    "content.initial-toe.duplicate-component",
                    seedPath,
                    $"Component '{group.Key}' has more than one initial TOE seed.");
            }

            if (!components.TryGetValue(group.Key, out var component))
            {
                Add(
                    issues,
                    "content.initial-toe.unknown-component",
                    seedPath,
                    $"Initial TOE seed references unknown component '{group.Key}'.");
                continue;
            }

            foreach (var seed in group.Where(seed => seed.CurrentToe > component.MaximumToe))
            {
                Add(
                    issues,
                    "content.initial-toe.over-maximum",
                    $"{seedPath}/currentToe",
                    $"Initial TOE {seed.CurrentToe} exceeds maximum TOE {component.MaximumToe}.");
            }
        }

        foreach (var componentId in components.Keys.Except(
            seedGroups.Select(group => group.Key),
            StringComparer.Ordinal))
        {
            Add(
                issues,
                "content.initial-toe.missing-component",
                $"{path}/initialComponentToes/{componentId}",
                $"Component '{componentId}' requires one explicit initial TOE seed.");
        }
    }

    private static void ValidateOrigins(
        ContentPackV5Definition definition,
        List<ContentValidationIssue> issues)
    {
        var sources = definition.LegacyDefinition.SourceIndex
            .GroupBy(entry => entry.SourceId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(entry => entry.Kind).First(),
                StringComparer.Ordinal);

        foreach (var (origin, path) in EnumerateOrigins(definition))
        {
            foreach (var reference in origin.References)
            {
                var referencePath = $"{path}/origin/references/{reference.SourceId}";
                if (!sources.TryGetValue(reference.SourceId, out var source))
                {
                    Add(
                        issues,
                        "content.unknown-reference",
                        referencePath,
                        $"Origin references unknown source '{reference.SourceId}'.");
                    continue;
                }

                var kindMatches = origin.Kind switch
                {
                    ContentOriginKind.Synthetic =>
                        source.Kind == ContentSourceKind.RepositorySynthetic,
                    ContentOriginKind.SourceDerived =>
                        source.Kind is ContentSourceKind.PublishedPrimary
                            or ContentSourceKind.AdoptedRuling,
                    _ => false,
                };

                if (!kindMatches)
                {
                    Add(
                        issues,
                        "content.invalid-origin",
                        referencePath,
                        $"Origin kind '{origin.Kind}' is incompatible with source kind '{source.Kind}'.");
                }
            }
        }
    }

    private static IEnumerable<(ContentOrigin Origin, string Path)> EnumerateOrigins(
        ContentPackV5Definition definition)
    {
        foreach (var facts in definition.ElementCombatFacts)
        {
            var path = $"/elements/{facts.ElementId}/combat";
            yield return (facts.Origin, path);

            foreach (var component in facts.Components)
            {
                yield return (component.Origin, $"{path}/components/{component.ComponentId}");
            }
            foreach (var limit in facts.ComponentToeLimits)
                yield return (limit.Origin, $"{path}/componentToeLimits/{limit.LimitId}");
        }

        foreach (var placement in definition.InitialPlacementCombatFacts)
        {
            var path = PlacementPath(placement.ScenarioId, placement.ElementId);
            foreach (var seed in placement.InitialComponentToes)
            {
                yield return (seed.Origin, $"{path}/initialComponentToes/{seed.ComponentId}");
            }
        }
    }

    private static string PlacementPath(string scenarioId, string elementId) =>
        $"/scenarios/{scenarioId}/initialPlacements/{elementId}";

    private static void Add(
        List<ContentValidationIssue> issues,
        string code,
        string path,
        string message) => issues.Add(new ContentValidationIssue(code, path, message));
}
