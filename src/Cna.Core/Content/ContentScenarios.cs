namespace Cna.Core.Content;

internal static class ContentScenarioInitialPlacements
{
    internal static IEnumerable<(string ElementId, string LocationId)> Expand(ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return scenario.InitialPlacements.Select(value => (value.ElementId, value.LocationId))
            .Concat(scenario.InitialGroups.SelectMany(group => group.ElementIds
                .Select(id => (ElementId: id, group.LocationId))));
    }
}

public sealed record ContentScenarioBoundary
{
    public ContentScenarioBoundary(int gameTurn, int operationStage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);

        if (operationStage is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(operationStage));
        }

        GameTurn = gameTurn;
        OperationStage = operationStage;
    }

    public int GameTurn { get; }

    public int OperationStage { get; }
}

public sealed record ContentInitialPlacement
{
    public ContentInitialPlacement(
        string elementId,
        string locationId,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        Origin = origin;
    }

    public string ElementId { get; }

    public string LocationId { get; }

    public ContentOrigin Origin { get; }
}

// Printed truck quantities; null means the source does not specify a value.
public sealed record ContentSourceTruckQuantities
{
    public ContentSourceTruckQuantities(int? light, int? medium, int? heavy)
    {
        if (light is null && medium is null && heavy is null)
            throw new ArgumentException("At least one truck quantity must be specified.");
        if (light < 0) throw new ArgumentOutOfRangeException(nameof(light));
        if (medium < 0) throw new ArgumentOutOfRangeException(nameof(medium));
        if (heavy < 0) throw new ArgumentOutOfRangeException(nameof(heavy));
        Light = light;
        Medium = medium;
        Heavy = heavy;
    }

    public int? Light { get; }
    public int? Medium { get; }
    public int? Heavy { get; }
}

// In-memory successor data. Legacy content formats reject nonempty schedules.
public sealed record ContentScheduledArrival
{
    public ContentScheduledArrival(string groupId, string sideId, ContentScenarioBoundary arrival,
        IEnumerable<string> elementIds, string? entryAreaId, ContentOrigin origin, ContentSourceTruckQuantities? trucks = null)
    {
        ArgumentNullException.ThrowIfNull(arrival);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(elementIds);
        var members = elementIds.Select(value => ContentContractGuards.RequireStableId(value, nameof(elementIds))).ToArray();
        if (members.Length == 0 || members.Distinct(StringComparer.Ordinal).Count() != members.Length)
            throw new ArgumentException("An arrival requires distinct members.", nameof(elementIds));
        GroupId = ContentContractGuards.RequireStableId(groupId, nameof(groupId));
        SideId = ContentContractGuards.RequireStableId(sideId, nameof(sideId));
        Arrival = arrival;
        ElementIds = Array.AsReadOnly(members.Order(StringComparer.Ordinal).ToArray());
        EntryAreaId = entryAreaId is null ? null : ContentContractGuards.RequireStableId(entryAreaId, nameof(entryAreaId));
        Origin = origin;
        Trucks = trucks;
    }

    public string GroupId { get; }
    public string SideId { get; }
    public ContentScenarioBoundary Arrival { get; }
    public IReadOnlyList<string> ElementIds { get; }
    public string? EntryAreaId { get; }
    public ContentOrigin Origin { get; }
    public ContentSourceTruckQuantities? Trucks { get; }

    public bool Equals(ContentScheduledArrival? other) => ReferenceEquals(this, other)
        || (other is not null && GroupId == other.GroupId && SideId == other.SideId
            && Arrival == other.Arrival && ElementIds.SequenceEqual(other.ElementIds)
            && EntryAreaId == other.EntryAreaId && Origin == other.Origin && Trucks == other.Trucks);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GroupId, StringComparer.Ordinal);
        hash.Add(SideId, StringComparer.Ordinal);
        hash.Add(Arrival);
        foreach (var elementId in ElementIds) hash.Add(elementId, StringComparer.Ordinal);
        hash.Add(EntryAreaId, StringComparer.Ordinal);
        hash.Add(Origin);
        hash.Add(Trucks);
        return hash.ToHashCode();
    }
}

public sealed record ContentLocationArea
{
    public ContentLocationArea(string areaId, IEnumerable<string> locationIds, ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(locationIds);
        ArgumentNullException.ThrowIfNull(origin);
        var members = locationIds.Select(value => ContentContractGuards.RequireStableId(value, nameof(locationIds))).ToArray();
        if (members.Length == 0 || members.Distinct(StringComparer.Ordinal).Count() != members.Length)
            throw new ArgumentException("An area requires distinct locations.", nameof(locationIds));
        AreaId = ContentContractGuards.RequireStableId(areaId, nameof(areaId));
        LocationIds = Array.AsReadOnly(members.Order(StringComparer.Ordinal).ToArray());
        Origin = origin;
    }

    public string AreaId { get; }
    public IReadOnlyList<string> LocationIds { get; }
    public ContentOrigin Origin { get; }
    public bool Equals(ContentLocationArea? other) => ReferenceEquals(this, other)
        || (other is not null && AreaId == other.AreaId && LocationIds.SequenceEqual(other.LocationIds)
            && Origin == other.Origin);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AreaId, StringComparer.Ordinal);
        foreach (var locationId in LocationIds) hash.Add(locationId, StringComparer.Ordinal);
        hash.Add(Origin);
        return hash.ToHashCode();
    }
}

// Source grouping records shared location only; it does not establish attachment.
public sealed record ContentInitialGroup
{
    public ContentInitialGroup(string groupId, string sideId, string locationId,
        IEnumerable<string> elementIds, ContentOrigin origin, ContentSourceTruckQuantities? trucks = null)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        ArgumentNullException.ThrowIfNull(origin);
        var members = elementIds.Select(value => ContentContractGuards.RequireStableId(value, nameof(elementIds))).ToArray();
        if (members.Length == 0 || members.Distinct(StringComparer.Ordinal).Count() != members.Length)
            throw new ArgumentException("An initial group requires distinct members.", nameof(elementIds));
        GroupId = ContentContractGuards.RequireStableId(groupId, nameof(groupId));
        SideId = ContentContractGuards.RequireStableId(sideId, nameof(sideId));
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        ElementIds = Array.AsReadOnly(members.Order(StringComparer.Ordinal).ToArray());
        Origin = origin;
        Trucks = trucks;
    }

    public string GroupId { get; }
    public string SideId { get; }
    public string LocationId { get; }
    public IReadOnlyList<string> ElementIds { get; }
    public ContentOrigin Origin { get; }
    public ContentSourceTruckQuantities? Trucks { get; }

    public bool Equals(ContentInitialGroup? other) => ReferenceEquals(this, other)
        || (other is not null && GroupId == other.GroupId && SideId == other.SideId
            && LocationId == other.LocationId && ElementIds.SequenceEqual(other.ElementIds)
            && Origin == other.Origin && Trucks == other.Trucks);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GroupId, StringComparer.Ordinal);
        hash.Add(SideId, StringComparer.Ordinal);
        hash.Add(LocationId, StringComparer.Ordinal);
        foreach (var elementId in ElementIds) hash.Add(elementId, StringComparer.Ordinal);
        hash.Add(Origin);
        hash.Add(Trucks);
        return hash.ToHashCode();
    }
}

public sealed record ContentScenario
{
    public ContentScenario(
        string scenarioId,
        ContentScenarioBoundary start,
        ContentScenarioBoundary end,
        IEnumerable<ContentInitialPlacement> initialPlacements,
        ContentOrigin origin)
        : this(scenarioId, start, end, initialPlacements, [], origin)
    {
    }

    public ContentScenario(
        string scenarioId,
        ContentScenarioBoundary start,
        ContentScenarioBoundary end,
        IEnumerable<ContentInitialPlacement> initialPlacements,
        IEnumerable<ContentScheduledArrival> scheduledArrivals,
        ContentOrigin origin)
        : this(scenarioId, start, end, initialPlacements, scheduledArrivals, [], origin)
    {
    }

    public ContentScenario(
        string scenarioId,
        ContentScenarioBoundary start,
        ContentScenarioBoundary end,
        IEnumerable<ContentInitialPlacement> initialPlacements,
        IEnumerable<ContentScheduledArrival> scheduledArrivals,
        IEnumerable<ContentLocationArea> entryAreas,
        ContentOrigin origin)
        : this(scenarioId, start, end, initialPlacements, scheduledArrivals, entryAreas, [], origin)
    {
    }

    public ContentScenario(
        string scenarioId,
        ContentScenarioBoundary start,
        ContentScenarioBoundary end,
        IEnumerable<ContentInitialPlacement> initialPlacements,
        IEnumerable<ContentScheduledArrival> scheduledArrivals,
        IEnumerable<ContentLocationArea> entryAreas,
        IEnumerable<ContentInitialGroup> initialGroups,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        ArgumentNullException.ThrowIfNull(origin);
        var placementCopy = ContentContractGuards.CopyValues(
            initialPlacements,
            nameof(initialPlacements));
        var arrivalCopy = ContentContractGuards.CopyValues(scheduledArrivals, nameof(scheduledArrivals));
        var areaCopy = ContentContractGuards.CopyValues(entryAreas, nameof(entryAreas));
        var groupCopy = ContentContractGuards.CopyValues(initialGroups, nameof(initialGroups));

        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
        Start = start;
        End = end;
        InitialPlacements = Array.AsReadOnly(placementCopy
            .OrderBy(placement => placement.ElementId, StringComparer.Ordinal)
            .ThenBy(placement => placement.LocationId, StringComparer.Ordinal)
            .ToArray());
        ScheduledArrivals = Array.AsReadOnly(arrivalCopy.OrderBy(value => value.GroupId, StringComparer.Ordinal).ToArray());
        EntryAreas = Array.AsReadOnly(areaCopy.OrderBy(value => value.AreaId, StringComparer.Ordinal).ToArray());
        InitialGroups = Array.AsReadOnly(groupCopy.OrderBy(value => value.GroupId, StringComparer.Ordinal).ToArray());
        Origin = origin;
    }

    public string ScenarioId { get; }

    public ContentScenarioBoundary Start { get; }

    public ContentScenarioBoundary End { get; }

    public IReadOnlyList<ContentInitialPlacement> InitialPlacements { get; }

    public IReadOnlyList<ContentScheduledArrival> ScheduledArrivals { get; }

    public IReadOnlyList<ContentLocationArea> EntryAreas { get; }

    public IReadOnlyList<ContentInitialGroup> InitialGroups { get; }

    public ContentOrigin Origin { get; }

    public bool Equals(ContentScenario? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(ScenarioId, other.ScenarioId, StringComparison.Ordinal)
            && Start == other.Start
            && End == other.End
            && InitialPlacements.SequenceEqual(other.InitialPlacements)
            && ScheduledArrivals.SequenceEqual(other.ScheduledArrivals)
            && EntryAreas.SequenceEqual(other.EntryAreas)
            && InitialGroups.SequenceEqual(other.InitialGroups)
            && Origin == other.Origin);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ScenarioId, StringComparer.Ordinal);
        hash.Add(Start);
        hash.Add(End);

        foreach (var placement in InitialPlacements)
        {
            hash.Add(placement);
        }

        foreach (var arrival in ScheduledArrivals) hash.Add(arrival);
        foreach (var area in EntryAreas) hash.Add(area);
        foreach (var group in InitialGroups) hash.Add(group);

        hash.Add(Origin);
        return hash.ToHashCode();
    }
}
