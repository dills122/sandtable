namespace Cna.Core.Rules;

public static class Cna1979Combat
{
    public const string CombatUnitClassificationId =
        "land.combat-classification.combat-unit";
    public const string HeadquartersClassificationId =
        "land.combat-classification.headquarters";
    public const string TruckConvoyClassificationId =
        "land.combat-classification.truck-convoy";
    public const string AircraftClassificationId =
        "land.combat-classification.aircraft";
    public const string SquadronGroundSupportClassificationId =
        "land.combat-classification.squadron-ground-support";
    public const string WarshipClassificationId =
        "land.combat-classification.warship";
    public const string InformationalMarkerClassificationId =
        "land.combat-classification.informational-marker";
    public const string InfantryComponentClassId =
        "land.combat-component.infantry";

    private static readonly RuleReference CombatUnitSource = new(
        "spi-1979-land-rules",
        "10.11");
    private static readonly RuleReference NonCombatUnitSource = new(
        "spi-1979-land-rules",
        "10.12");
    private static readonly RuleReference InformationalMarkerSource = new(
        "spi-1979-land-rules",
        "10.13");
    private static readonly RuleReference ComponentSource = new(
        "spi-1979-land-rules",
        "3.5");
    private static readonly RuleReference CloseAssaultRatingSource = new(
        "spi-1979-land-rules",
        "11.15");
    private static readonly RuleReference CombatStrengthCalculationSource = new(
        "spi-1979-land-rules",
        "11.3");

    private static readonly IReadOnlyList<ZocCombatClassificationDefinition>
        ClassificationAuthority = Array.AsReadOnly<ZocCombatClassificationDefinition>(
        [
            Classification(
                CombatUnitClassificationId,
                ZocCombatClassificationKind.CombatUnit,
                CombatUnitSource),
            Classification(
                HeadquartersClassificationId,
                ZocCombatClassificationKind.Headquarters,
                CombatUnitSource),
            Classification(
                TruckConvoyClassificationId,
                ZocCombatClassificationKind.TruckConvoy,
                CombatUnitSource),
            Classification(
                AircraftClassificationId,
                ZocCombatClassificationKind.Aircraft,
                NonCombatUnitSource),
            Classification(
                SquadronGroundSupportClassificationId,
                ZocCombatClassificationKind.SquadronGroundSupport,
                NonCombatUnitSource),
            Classification(
                WarshipClassificationId,
                ZocCombatClassificationKind.Warship,
                NonCombatUnitSource),
            Classification(
                InformationalMarkerClassificationId,
                ZocCombatClassificationKind.InformationalMarker,
                InformationalMarkerSource),
        ]);

    private static readonly IReadOnlyList<ZocCombatComponentDefinition>
        ComponentClassAuthority = Array.AsReadOnly<ZocCombatComponentDefinition>(
        [
            new(
                InfantryComponentClassId,
                ZocCombatComponentKind.Infantry,
                [ComponentSource, CloseAssaultRatingSource]),
        ]);

    public static IReadOnlyList<ZocCombatClassificationDefinition> Classifications =>
        ClassificationAuthority;

    public static IReadOnlyList<ZocCombatComponentDefinition> ComponentClasses =>
        ComponentClassAuthority;

    public static bool IsSupportedClassificationId(string? classificationId) =>
        ClassificationAuthority.Any(value => string.Equals(
            value.ClassificationId,
            classificationId,
            StringComparison.Ordinal));

    public static bool IsSupportedComponentClassId(string? componentClassId) =>
        ComponentClassAuthority.Any(value => string.Equals(
            value.ComponentClassId,
            componentClassId,
            StringComparison.Ordinal));

    public static ZocRawDefensiveCloseAssaultResult CalculateRawDefensiveCloseAssaultPoints(
        IEnumerable<ZocDefensiveCloseAssaultComponentFact> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var copy = components.ToArray();
        if (copy.Length == 0 || copy.Any(component => component is null))
        {
            throw new ArgumentException(
                "At least one non-null defensive Close Assault component is required.",
                nameof(components));
        }

        long total = 0;
        foreach (var component in copy)
        {
            if (!IsSupportedComponentClassId(component.ComponentClassId))
            {
                throw new ArgumentException(
                    $"Unsupported combat component class '{component.ComponentClassId}'.",
                    nameof(components));
            }

            var componentPoints = checked(
                (long)component.CurrentToe * component.DefensiveCloseAssaultRating);
            total = checked(total + componentPoints);
        }

        return new ZocRawDefensiveCloseAssaultResult(
            total,
            [CloseAssaultRatingSource, CombatStrengthCalculationSource]);
    }

    internal static ZocCombatClassificationDefinition? FindClassification(
        string classificationId) => ClassificationAuthority.SingleOrDefault(value =>
            string.Equals(
                value.ClassificationId,
                classificationId,
                StringComparison.Ordinal));

    private static ZocCombatClassificationDefinition Classification(
        string classificationId,
        ZocCombatClassificationKind kind,
        params RuleReference[] sources) => new(
            classificationId,
            kind,
            sources);
}
