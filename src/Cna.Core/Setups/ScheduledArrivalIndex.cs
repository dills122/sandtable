using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

// A source-neutral value slice; it is not admitted as campaign authority.
internal sealed class ScheduledArrivalGroup
{
    public ScheduledArrivalGroup(
        string groupId,
        LandSide owner,
        int gameTurn,
        int operationStage,
        IEnumerable<string> unitIds,
        IEnumerable<RuleReference> sources)
    {
        GroupId = ContentContractGuards.RequireStableId(groupId, nameof(groupId));
        if (!Enum.IsDefined(owner))
        {
            throw new ArgumentOutOfRangeException(nameof(owner));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(gameTurn, 111);
        ArgumentOutOfRangeException.ThrowIfLessThan(operationStage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(operationStage, 3);
        ArgumentNullException.ThrowIfNull(unitIds);

        var units = unitIds.Select(id => ContentContractGuards.RequireStableId(id, nameof(unitIds))).ToArray();
        if (units.Length == 0 || units.Distinct(StringComparer.Ordinal).Count() != units.Length)
        {
            throw new ArgumentException("At least one distinct unit ID is required.", nameof(unitIds));
        }

        Owner = owner;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        UnitIds = Array.AsReadOnly(units.Order(StringComparer.Ordinal).ToArray());
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string GroupId { get; }
    public LandSide Owner { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public IReadOnlyList<string> UnitIds { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
}

// Empty lookups only describe this index; they do not prove a phase has no obligations.
internal sealed class ScheduledArrivalIndex
{
    private readonly IReadOnlyList<ScheduledArrivalGroup> groups;

    public ScheduledArrivalIndex(IEnumerable<ScheduledArrivalGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var copy = groups.ToArray();
        if (copy.Any(group => group is null))
        {
            throw new ArgumentException("Arrival groups must not contain null.", nameof(groups));
        }

        if (copy.Select(group => group.GroupId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException("Arrival group IDs must be unique.", nameof(groups));
        }

        var unitsBySlot = new HashSet<(int Turn, int Stage, string UnitId)>();
        foreach (var group in copy)
        {
            foreach (var unitId in group.UnitIds)
            {
                if (!unitsBySlot.Add((group.GameTurn, group.OperationStage, unitId)))
                {
                    throw new ArgumentException("A unit may occur only once in an arrival slot.", nameof(groups));
                }
            }
        }

        this.groups = Array.AsReadOnly(copy
            .OrderBy(group => group.GameTurn)
            .ThenBy(group => group.OperationStage)
            .ThenBy(group => group.Owner)
            .ThenBy(group => group.GroupId, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<ScheduledArrivalGroup> Find(int gameTurn, int operationStage, LandSide owner)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(gameTurn, 111);
        ArgumentOutOfRangeException.ThrowIfLessThan(operationStage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(operationStage, 3);
        if (!Enum.IsDefined(owner))
        {
            throw new ArgumentOutOfRangeException(nameof(owner));
        }

        return Array.AsReadOnly(groups
            .Where(group => group.GameTurn == gameTurn
                && group.OperationStage == operationStage
                && group.Owner == owner)
            .ToArray());
    }
}
