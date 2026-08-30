using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class Cna1979ContentV5CompatibilityValidator
{
    public static ContentValidationResult Validate(ContentPackV5Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = new List<ContentValidationIssue>();
        issues.AddRange(
            Cna1979ContentCompatibilityValidator.Validate(definition.LegacyDefinition).Issues);

        foreach (var facts in definition.ElementCombatFacts)
        {
            if (!Cna1979Combat.IsSupportedClassificationId(facts.CombatClassificationId))
            {
                Add(
                    issues,
                    "vocabulary.unknown-combat-classification",
                    $"/elements/{facts.ElementId}/combatClassificationId",
                    $"Unknown combat classification ID '{facts.CombatClassificationId}'.");
            }

            foreach (var component in facts.Components)
            {
                if (!Cna1979Combat.IsSupportedComponentClassId(component.ComponentClassId))
                {
                    Add(
                        issues,
                        "vocabulary.unknown-combat-component",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/componentClassId",
                        $"Unknown combat component class ID '{component.ComponentClassId}'.");
                }
            }
        }

        return new ContentValidationResult(issues);
    }

    private static void Add(
        List<ContentValidationIssue> issues,
        string code,
        string path,
        string message) => issues.Add(new ContentValidationIssue(code, path, message));
}
