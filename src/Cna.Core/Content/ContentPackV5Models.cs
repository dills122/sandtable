namespace Cna.Core.Content;

public sealed record ContentCombatComponent
{
    public ContentCombatComponent(
        string componentId,
        string componentClassId,
        int maximumToe,
        int defensiveCloseAssaultRating,
        ContentOrigin origin)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumToe, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(defensiveCloseAssaultRating);
        ArgumentNullException.ThrowIfNull(origin);

        ComponentId = ContentContractGuards.RequireStableId(componentId, nameof(componentId));
        ComponentClassId = ContentContractGuards.RequireStableId(
            componentClassId,
            nameof(componentClassId));
        MaximumToe = maximumToe;
        DefensiveCloseAssaultRating = defensiveCloseAssaultRating;
        Origin = origin;
    }

    public string ComponentId { get; }

    public string ComponentClassId { get; }

    public int MaximumToe { get; }

    public int DefensiveCloseAssaultRating { get; }

    public ContentOrigin Origin { get; }
}

public sealed record ContentElementCombatFacts
{
    public ContentElementCombatFacts(
        string elementId,
        string combatClassificationId,
        IEnumerable<ContentCombatComponent> components,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        var componentCopy = ContentContractGuards.CopyValues(
            components,
            nameof(components));
        if (componentCopy.Length == 0)
        {
            throw new ArgumentException(
                "At least one combat component is required.",
                nameof(components));
        }

        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        CombatClassificationId = ContentContractGuards.RequireStableId(
            combatClassificationId,
            nameof(combatClassificationId));
        Components = Array.AsReadOnly(componentCopy
            .OrderBy(component => component.ComponentId, StringComparer.Ordinal)
            .ToArray());
        Origin = origin;
    }

    public string ElementId { get; }

    public string CombatClassificationId { get; }

    public IReadOnlyList<ContentCombatComponent> Components { get; }

    public ContentOrigin Origin { get; }

    public bool Equals(ContentElementCombatFacts? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(ElementId, other.ElementId, StringComparison.Ordinal)
            && string.Equals(
                CombatClassificationId,
                other.CombatClassificationId,
                StringComparison.Ordinal)
            && Components.SequenceEqual(other.Components)
            && Origin == other.Origin);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ElementId, StringComparer.Ordinal);
        hash.Add(CombatClassificationId, StringComparer.Ordinal);
        foreach (var component in Components)
        {
            hash.Add(component);
        }

        hash.Add(Origin);
        return hash.ToHashCode();
    }
}

public sealed record ContentInitialComponentToe
{
    public ContentInitialComponentToe(
        string componentId,
        int currentToe,
        ContentOrigin origin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentToe);
        ArgumentNullException.ThrowIfNull(origin);
        ComponentId = ContentContractGuards.RequireStableId(componentId, nameof(componentId));
        CurrentToe = currentToe;
        Origin = origin;
    }

    public string ComponentId { get; }

    public int CurrentToe { get; }

    public ContentOrigin Origin { get; }
}

public sealed record ContentInitialPlacementCombatFacts
{
    public ContentInitialPlacementCombatFacts(
        string scenarioId,
        string elementId,
        IEnumerable<ContentInitialComponentToe> initialComponentToes)
    {
        var toeCopy = ContentContractGuards.CopyValues(
            initialComponentToes,
            nameof(initialComponentToes));
        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        InitialComponentToes = Array.AsReadOnly(toeCopy
            .OrderBy(value => value.ComponentId, StringComparer.Ordinal)
            .ToArray());
    }

    public string ScenarioId { get; }

    public string ElementId { get; }

    public IReadOnlyList<ContentInitialComponentToe> InitialComponentToes { get; }

    public bool Equals(ContentInitialPlacementCombatFacts? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(ScenarioId, other.ScenarioId, StringComparison.Ordinal)
            && string.Equals(ElementId, other.ElementId, StringComparison.Ordinal)
            && InitialComponentToes.SequenceEqual(other.InitialComponentToes));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ScenarioId, StringComparer.Ordinal);
        hash.Add(ElementId, StringComparer.Ordinal);
        foreach (var toe in InitialComponentToes)
        {
            hash.Add(toe);
        }

        return hash.ToHashCode();
    }
}

public sealed class ContentPackV5Definition
{
    public const int SchemaVersion = 5;
    public const string CanonicalFormatId = "sandtable.content-json.v4";
    public const string CombatCapabilityId = "land.combat-components";

    public ContentPackV5Definition(
        ContentPackDefinition legacyDefinition,
        IEnumerable<ContentElementCombatFacts> elementCombatFacts,
        IEnumerable<ContentInitialPlacementCombatFacts> initialPlacementCombatFacts)
    {
        ArgumentNullException.ThrowIfNull(legacyDefinition);
        var elementCopy = ContentContractGuards.CopyValues(
            elementCombatFacts,
            nameof(elementCombatFacts));
        var placementCopy = ContentContractGuards.CopyValues(
            initialPlacementCombatFacts,
            nameof(initialPlacementCombatFacts));

        LegacyDefinition = legacyDefinition;
        ContractSchemaVersion = SchemaVersion;
        FormatId = CanonicalFormatId;
        Capabilities = Array.AsReadOnly(legacyDefinition.Capabilities
            .Append(CombatCapabilityId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray());
        ElementCombatFacts = Array.AsReadOnly(elementCopy
            .OrderBy(value => value.ElementId, StringComparer.Ordinal)
            .ToArray());
        InitialPlacementCombatFacts = Array.AsReadOnly(placementCopy
            .OrderBy(value => value.ScenarioId, StringComparer.Ordinal)
            .ThenBy(value => value.ElementId, StringComparer.Ordinal)
            .ToArray());
    }

    public int ContractSchemaVersion { get; }

    public string FormatId { get; }

    public string PackId => LegacyDefinition.PackId;

    public string RulesetId => LegacyDefinition.RulesetId;

    public ContentPackDefinition LegacyDefinition { get; }

    public IReadOnlyList<string> Capabilities { get; }

    public IReadOnlyList<ContentElementCombatFacts> ElementCombatFacts { get; }

    public IReadOnlyList<ContentInitialPlacementCombatFacts> InitialPlacementCombatFacts { get; }
}
