namespace Cna.Core.Content;

public sealed record ContentCombatComponent
{
    public ContentCombatComponent(
        string componentId,
        string componentClassId,
        int maximumToe,
        int defensiveCloseAssaultRating,
        ContentOrigin origin,
        bool defensiveRatingParenthesized = false,
        int? offensiveCloseAssaultRating = null,
        bool offensiveRatingParenthesized = false,
        int? barrageRating = null,
        bool barrageRatingParenthesized = false,
        ContentWeaponSourceProfile? weaponSourceProfile = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumToe, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(defensiveCloseAssaultRating);
        if (offensiveCloseAssaultRating < 0) throw new ArgumentOutOfRangeException(nameof(offensiveCloseAssaultRating));
        if (offensiveRatingParenthesized && offensiveCloseAssaultRating is null)
            throw new ArgumentException("A parenthesized offensive rating requires a value.", nameof(offensiveRatingParenthesized));
        if (barrageRating < 0) throw new ArgumentOutOfRangeException(nameof(barrageRating));
        if (barrageRatingParenthesized && barrageRating is null)
            throw new ArgumentException("A parenthesized barrage rating requires a value.", nameof(barrageRatingParenthesized));
        ArgumentNullException.ThrowIfNull(origin);

        ComponentId = ContentContractGuards.RequireStableId(componentId, nameof(componentId));
        ComponentClassId = ContentContractGuards.RequireStableId(
            componentClassId,
            nameof(componentClassId));
        MaximumToe = maximumToe;
        DefensiveCloseAssaultRating = defensiveCloseAssaultRating;
        Origin = origin;
        DefensiveRatingParenthesized = defensiveRatingParenthesized;
        OffensiveCloseAssaultRating = offensiveCloseAssaultRating;
        OffensiveRatingParenthesized = offensiveRatingParenthesized;
        BarrageRating = barrageRating;
        BarrageRatingParenthesized = barrageRatingParenthesized;
        WeaponSourceProfile = weaponSourceProfile;
    }

    public string ComponentId { get; }

    public string ComponentClassId { get; }

    public int MaximumToe { get; }

    public int DefensiveCloseAssaultRating { get; }

    public ContentOrigin Origin { get; }

    // Source marker only; the runtime does not yet implement its condition.
    public bool DefensiveRatingParenthesized { get; }

    public int? OffensiveCloseAssaultRating { get; }

    public bool OffensiveRatingParenthesized { get; }

    public int? BarrageRating { get; }

    public bool BarrageRatingParenthesized { get; }

    public ContentWeaponSourceProfile? WeaponSourceProfile { get; }
}

public sealed record ContentComponentToeLimit
{
    public ContentComponentToeLimit(string limitId, IEnumerable<string> componentIds, int maximumToe, ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(componentIds);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumToe);
        var members = componentIds.Select(value => ContentContractGuards.RequireStableId(value, nameof(componentIds))).ToArray();
        if (members.Length == 0 || members.Distinct(StringComparer.Ordinal).Count() != members.Length)
            throw new ArgumentException("A component TOE limit requires distinct members.", nameof(componentIds));
        LimitId = ContentContractGuards.RequireStableId(limitId, nameof(limitId));
        ComponentIds = Array.AsReadOnly(members.Order(StringComparer.Ordinal).ToArray());
        MaximumToe = maximumToe;
        Origin = origin;
    }

    public string LimitId { get; }
    public IReadOnlyList<string> ComponentIds { get; }
    public int MaximumToe { get; }
    public ContentOrigin Origin { get; }
    public bool Equals(ContentComponentToeLimit? other) => ReferenceEquals(this, other)
        || (other is not null && LimitId == other.LimitId && MaximumToe == other.MaximumToe
            && ComponentIds.SequenceEqual(other.ComponentIds) && Origin == other.Origin);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(LimitId, StringComparer.Ordinal);
        foreach (var id in ComponentIds) hash.Add(id, StringComparer.Ordinal);
        hash.Add(MaximumToe);
        hash.Add(Origin);
        return hash.ToHashCode();
    }
}

public sealed record ContentElementCombatFacts
{
    public ContentElementCombatFacts(
        string elementId,
        string combatClassificationId,
        IEnumerable<ContentCombatComponent> components,
        ContentOrigin origin,
        int? maximumCombinedToe = null, IEnumerable<ContentComponentToeLimit>? componentToeLimits = null)
        : this(elementId, combatClassificationId, components, origin, allowEmptyNoncombat: false,
            maximumCombinedToe: maximumCombinedToe, componentToeLimits: componentToeLimits)
    {
    }

    internal static ContentElementCombatFacts CreateV6(
        string elementId,
        string combatClassificationId,
        IEnumerable<ContentCombatComponent> components,
        ContentOrigin origin) =>
        new(elementId, combatClassificationId, components, origin, allowEmptyNoncombat: true);

    // Scheduled source interchange only; empty headquarters do not imply runtime support.
    public static ContentElementCombatFacts CreateScheduledHeadquarters(string elementId, ContentOrigin origin) =>
        new(elementId, Rules.Cna1979Combat.HeadquartersClassificationId, [], origin,
            allowEmptyNoncombat: false, allowEmptyHeadquarters: true);

    internal static ContentElementCombatFacts CreateScheduled(string elementId, string combatClassificationId,
        IEnumerable<ContentCombatComponent> components, ContentOrigin origin, int? maximumCombinedToe,
        IEnumerable<ContentComponentToeLimit>? componentToeLimits) =>
        new(elementId, combatClassificationId, components, origin, allowEmptyNoncombat: true,
            allowEmptyHeadquarters: true, maximumCombinedToe: maximumCombinedToe, componentToeLimits: componentToeLimits);

    private ContentElementCombatFacts(
        string elementId,
        string combatClassificationId,
        IEnumerable<ContentCombatComponent> components,
        ContentOrigin origin,
        bool allowEmptyNoncombat,
        bool allowEmptyHeadquarters = false,
        int? maximumCombinedToe = null, IEnumerable<ContentComponentToeLimit>? componentToeLimits = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        if (maximumCombinedToe <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCombinedToe));
        var componentCopy = ContentContractGuards.CopyValues(
            components,
            nameof(components));
        if (componentCopy.Length == 0
            && !(allowEmptyNoncombat
                && combatClassificationId == Rules.Cna1979Combat.TruckConvoyClassificationId)
            && !(allowEmptyHeadquarters
                && combatClassificationId == Rules.Cna1979Combat.HeadquartersClassificationId))
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
        MaximumCombinedToe = maximumCombinedToe;
        ComponentToeLimits = Array.AsReadOnly(ContentContractGuards.CopyValues(componentToeLimits ?? [], nameof(componentToeLimits))
            .OrderBy(value => value.LimitId, StringComparer.Ordinal).ToArray());
    }

    public string ElementId { get; }

    public string CombatClassificationId { get; }

    public IReadOnlyList<ContentCombatComponent> Components { get; }

    public ContentOrigin Origin { get; }

    public int? MaximumCombinedToe { get; }

    public IReadOnlyList<ContentComponentToeLimit> ComponentToeLimits { get; }

    public bool Equals(ContentElementCombatFacts? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(ElementId, other.ElementId, StringComparison.Ordinal)
            && string.Equals(
                CombatClassificationId,
                other.CombatClassificationId,
                StringComparison.Ordinal)
            && Components.SequenceEqual(other.Components)
            && MaximumCombinedToe == other.MaximumCombinedToe
            && ComponentToeLimits.SequenceEqual(other.ComponentToeLimits)
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
        hash.Add(MaximumCombinedToe);
        foreach (var limit in ComponentToeLimits) hash.Add(limit);
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

public sealed class ContentPackV5Definition : ICampaignCombatContentDefinition
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
