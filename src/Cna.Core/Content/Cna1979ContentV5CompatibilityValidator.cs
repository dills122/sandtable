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
            if (facts.ComponentToeLimits.Count != 0)
                Add(issues, "content.combat.component-limits-unsupported", $"/elements/{facts.ElementId}/componentToeLimits",
                    "The current combat capability cannot enforce component TOE limits.");
            if (facts.MaximumCombinedToe is not null)
                Add(issues, "content.combat.combined-toe-unsupported", $"/elements/{facts.ElementId}/maximumCombinedToe",
                    "The current combat capability cannot enforce a combined TOE limit.");
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
                if (component.DefensiveRatingParenthesized)
                {
                    Add(issues, "content.combat.parenthesized-defense-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/defensiveCloseAssaultRating",
                        "The current combat capability cannot interpret a parenthesized defensive rating.");
                }
                if (component.OffensiveCloseAssaultRating is not null)
                {
                    Add(issues, "content.combat.offensive-rating-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/offensiveCloseAssaultRating",
                        "The current combat capability cannot interpret an offensive rating in this profile.");
                }
                if (component.BarrageRating is not null)
                {
                    Add(issues, "content.combat.barrage-rating-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/barrageRating",
                        "The current combat capability cannot interpret a barrage rating in this profile.");
                }
                if (component.WeaponSourceProfile is not null)
                {
                    Add(issues, "content.combat.weapon-source-profile-unsupported",
                        $"/elements/{facts.ElementId}/components/{component.ComponentId}/weaponSourceProfile",
                        "The current combat capability cannot interpret a weapon source profile.");
                }
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
