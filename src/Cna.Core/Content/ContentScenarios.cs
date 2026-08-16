namespace Cna.Core.Content;

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

public sealed record ContentScenario
{
    public ContentScenario(
        string scenarioId,
        ContentScenarioBoundary start,
        ContentScenarioBoundary end,
        IEnumerable<ContentInitialPlacement> initialPlacements,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        ArgumentNullException.ThrowIfNull(origin);
        var placementCopy = ContentContractGuards.CopyValues(
            initialPlacements,
            nameof(initialPlacements));

        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
        Start = start;
        End = end;
        InitialPlacements = Array.AsReadOnly(placementCopy
            .OrderBy(placement => placement.ElementId, StringComparer.Ordinal)
            .ThenBy(placement => placement.LocationId, StringComparer.Ordinal)
            .ToArray());
        Origin = origin;
    }

    public string ScenarioId { get; }

    public ContentScenarioBoundary Start { get; }

    public ContentScenarioBoundary End { get; }

    public IReadOnlyList<ContentInitialPlacement> InitialPlacements { get; }

    public ContentOrigin Origin { get; }

    public bool Equals(ContentScenario? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(ScenarioId, other.ScenarioId, StringComparison.Ordinal)
            && Start == other.Start
            && End == other.End
            && InitialPlacements.SequenceEqual(other.InitialPlacements)
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

        hash.Add(Origin);
        return hash.ToHashCode();
    }
}
