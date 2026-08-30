namespace Cna.Core.Rules;

public enum ZocCombatClassificationKind
{
    CombatUnit,
    Headquarters,
    TruckConvoy,
    Aircraft,
    SquadronGroundSupport,
    Warship,
    InformationalMarker,
}

public enum ZocCombatComponentKind
{
    Infantry,
}

public sealed record ZocCombatClassificationDefinition
{
    public ZocCombatClassificationDefinition(
        string classificationId,
        ZocCombatClassificationKind kind,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classificationId);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ClassificationId = classificationId;
        Kind = kind;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string ClassificationId { get; }

    public ZocCombatClassificationKind Kind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}

public sealed record ZocCombatComponentDefinition
{
    public ZocCombatComponentDefinition(
        string componentClassId,
        ZocCombatComponentKind kind,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentClassId);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ComponentClassId = componentClassId;
        Kind = kind;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string ComponentClassId { get; }

    public ZocCombatComponentKind Kind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}

public sealed record ZocDefensiveCloseAssaultComponentFact
{
    public ZocDefensiveCloseAssaultComponentFact(
        string componentClassId,
        int currentToe,
        int defensiveCloseAssaultRating)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentClassId);
        ArgumentOutOfRangeException.ThrowIfNegative(currentToe);
        ArgumentOutOfRangeException.ThrowIfNegative(defensiveCloseAssaultRating);
        ComponentClassId = componentClassId;
        CurrentToe = currentToe;
        DefensiveCloseAssaultRating = defensiveCloseAssaultRating;
    }

    public string ComponentClassId { get; }

    public int CurrentToe { get; }

    public int DefensiveCloseAssaultRating { get; }
}

public sealed record ZocRawDefensiveCloseAssaultResult
{
    public ZocRawDefensiveCloseAssaultResult(
        long rawDefensiveCloseAssaultPoints,
        IEnumerable<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rawDefensiveCloseAssaultPoints);
        RawDefensiveCloseAssaultPoints = rawDefensiveCloseAssaultPoints;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public long RawDefensiveCloseAssaultPoints { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}
