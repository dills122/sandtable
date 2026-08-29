using System.Numerics;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

public sealed record MovementActionRouteAdjustment
{
    internal MovementActionRouteAdjustment(
        string routeId,
        MovementRouteCostKind costKind,
        CapabilityPointAmount amount)
    {
        if (!Enum.IsDefined(costKind))
        {
            throw new ArgumentOutOfRangeException(nameof(costKind));
        }

        ArgumentNullException.ThrowIfNull(amount);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            amount,
            CapabilityPointAmount.Zero);

        RouteId = ContentContractGuards.RequireStableId(routeId, nameof(routeId));
        CostKind = costKind;
        Amount = amount;
    }

    public string RouteId { get; }
    public MovementRouteCostKind CostKind { get; }
    public CapabilityPointAmount Amount { get; }
}

public sealed record MovementActionHexsideCost
{
    internal MovementActionHexsideCost(
        string hexsideId,
        MovementHexsideDirection direction,
        CapabilityPointAmount addedCost)
    {
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        ArgumentNullException.ThrowIfNull(addedCost);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            addedCost,
            CapabilityPointAmount.Zero);

        HexsideId = ContentContractGuards.RequireStableId(hexsideId, nameof(hexsideId));
        Direction = direction;
        AddedCost = addedCost;
    }

    public string HexsideId { get; }
    public MovementHexsideDirection Direction { get; }
    public CapabilityPointAmount AddedCost { get; }
}

public sealed record MovementActionCostBreakdown
{
    internal MovementActionCostBreakdown(
        string destinationTerrainId,
        CapabilityPointAmount destinationTerrainCost,
        MovementActionRouteAdjustment? routeAdjustment,
        IReadOnlyList<MovementActionHexsideCost> crossedHexsideCosts,
        CapabilityPointAmount totalCost)
    {
        ArgumentNullException.ThrowIfNull(destinationTerrainCost);
        ArgumentNullException.ThrowIfNull(crossedHexsideCosts);
        ArgumentNullException.ThrowIfNull(totalCost);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            destinationTerrainCost,
            CapabilityPointAmount.Zero);

        var crossed = crossedHexsideCosts.ToArray();
        if (crossed.Any(value => value is null)
            || crossed.Select(value => value.HexsideId)
                .Distinct(StringComparer.Ordinal).Count() != crossed.Length)
        {
            throw new ArgumentException(
                "Crossed hexside costs must be non-null and unique by feature ID.",
                nameof(crossedHexsideCosts));
        }

        var orderedCrossed = crossed
            .OrderBy(value => value.HexsideId, StringComparer.Ordinal)
            .ThenBy(value => value.Direction)
            .ToArray();
        var adjustedTerrain = routeAdjustment switch
        {
            null => destinationTerrainCost,
            { CostKind: MovementRouteCostKind.Override } => routeAdjustment.Amount,
            { CostKind: MovementRouteCostKind.ScaleUnderlying } => Multiply(
                destinationTerrainCost,
                routeAdjustment.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(routeAdjustment)),
        };
        var expectedTotal = orderedCrossed.Aggregate(
            adjustedTerrain,
            (current, value) => current + value.AddedCost);
        if (totalCost != expectedTotal)
        {
            throw new ArgumentException(
                "The Movement total cost does not match its canonical breakdown.",
                nameof(totalCost));
        }

        DestinationTerrainId = ContentContractGuards.RequireStableId(
            destinationTerrainId,
            nameof(destinationTerrainId));
        DestinationTerrainCost = destinationTerrainCost;
        RouteAdjustment = routeAdjustment;
        CrossedHexsideCosts = Array.AsReadOnly(orderedCrossed);
        TotalCost = totalCost;
    }

    public string DestinationTerrainId { get; }
    public CapabilityPointAmount DestinationTerrainCost { get; }
    public MovementActionRouteAdjustment? RouteAdjustment { get; }
    public IReadOnlyList<MovementActionHexsideCost> CrossedHexsideCosts { get; }
    public CapabilityPointAmount TotalCost { get; }

    public bool Equals(MovementActionCostBreakdown? other) => ReferenceEquals(this, other)
        || (other is not null
            && DestinationTerrainId == other.DestinationTerrainId
            && DestinationTerrainCost == other.DestinationTerrainCost
            && RouteAdjustment == other.RouteAdjustment
            && CrossedHexsideCosts.SequenceEqual(other.CrossedHexsideCosts)
            && TotalCost == other.TotalCost);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DestinationTerrainId, StringComparer.Ordinal);
        hash.Add(DestinationTerrainCost);
        hash.Add(RouteAdjustment);
        foreach (var value in CrossedHexsideCosts) hash.Add(value);
        hash.Add(TotalCost);
        return hash.ToHashCode();
    }

    private static CapabilityPointAmount Multiply(
        CapabilityPointAmount left,
        CapabilityPointAmount right)
    {
        var numerator = (BigInteger)left.Numerator * right.Numerator;
        var denominator = (BigInteger)left.Denominator * right.Denominator;
        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;
        if (numerator > long.MaxValue || denominator > int.MaxValue)
        {
            throw new OverflowException(
                "The exact Movement cost is outside the supported representation.");
        }

        return new CapabilityPointAmount((long)numerator, (int)denominator);
    }
}

public sealed record MoveElementAction : CampaignActionCandidate
{
    internal MoveElementAction(
        string elementId,
        string originLocationId,
        string destinationLocationId,
        MovementActionCostBreakdown costBreakdown)
        : base(
            "move-element",
            WriteSemantics(elementId, originLocationId, destinationLocationId, costBreakdown))
    {
        var origin = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        var destination = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (origin == destination)
        {
            throw new ArgumentException(
                "A Movement candidate must change location.",
                nameof(destinationLocationId));
        }

        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        OriginLocationId = origin;
        DestinationLocationId = destination;
        CostBreakdown = costBreakdown;
    }

    public string ElementId { get; }
    public string OriginLocationId { get; }
    public string DestinationLocationId { get; }
    public MovementActionCostBreakdown CostBreakdown { get; }

    internal static byte[] WriteSemantics(
        string elementId,
        string originLocationId,
        string destinationLocationId,
        MovementActionCostBreakdown costBreakdown)
    {
        _ = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        var origin = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        var destination = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (origin == destination)
        {
            throw new ArgumentException(
                "A Movement candidate must change location.",
                nameof(destinationLocationId));
        }

        ArgumentNullException.ThrowIfNull(costBreakdown);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("kind", "move-element");
            writer.WriteString("elementId", elementId);
            writer.WriteString("originLocationId", origin);
            writer.WriteString("destinationLocationId", destination);
            MovementActionJson.WriteCostBreakdown(writer, costBreakdown);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}

public sealed record CompleteMovementSegmentAction : CampaignActionCandidate
{
    internal CompleteMovementSegmentAction() : base("complete-movement-segment") { }
}

internal static class MovementActionJson
{
    public static void WriteCostBreakdown(
        Utf8JsonWriter writer,
        MovementActionCostBreakdown breakdown)
    {
        writer.WriteStartObject("costBreakdown");
        writer.WriteString("destinationTerrainId", breakdown.DestinationTerrainId);
        writer.WritePropertyName("destinationTerrainCost");
        CapabilityPointAmountCodec.WriteCanonical(writer, breakdown.DestinationTerrainCost);
        if (breakdown.RouteAdjustment is null)
        {
            writer.WriteNull("routeAdjustment");
        }
        else
        {
            writer.WriteStartObject("routeAdjustment");
            writer.WriteString("routeId", breakdown.RouteAdjustment.RouteId);
            writer.WriteString(
                "costKind",
                FormatRouteCostKind(breakdown.RouteAdjustment.CostKind));
            writer.WritePropertyName("amount");
            CapabilityPointAmountCodec.WriteCanonical(writer, breakdown.RouteAdjustment.Amount);
            writer.WriteEndObject();
        }

        writer.WriteStartArray("crossedHexsideCosts");
        foreach (var cost in breakdown.CrossedHexsideCosts)
        {
            writer.WriteStartObject();
            writer.WriteString("hexsideId", cost.HexsideId);
            writer.WriteString("direction", FormatHexsideDirection(cost.Direction));
            writer.WritePropertyName("addedCost");
            CapabilityPointAmountCodec.WriteCanonical(writer, cost.AddedCost);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName("totalCost");
        CapabilityPointAmountCodec.WriteCanonical(writer, breakdown.TotalCost);
        writer.WriteEndObject();
    }

    public static string FormatRouteCostKind(MovementRouteCostKind kind) => kind switch
    {
        MovementRouteCostKind.Override => "override",
        MovementRouteCostKind.ScaleUnderlying => "scale-underlying",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static MovementRouteCostKind ParseRouteCostKind(string? kind) => kind switch
    {
        "override" => MovementRouteCostKind.Override,
        "scale-underlying" => MovementRouteCostKind.ScaleUnderlying,
        _ => throw new JsonException($"Unknown Movement route cost kind '{kind}'."),
    };

    public static string FormatHexsideDirection(MovementHexsideDirection direction) => direction switch
    {
        MovementHexsideDirection.Either => "either",
        MovementHexsideDirection.Up => "up",
        MovementHexsideDirection.Down => "down",
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    public static MovementHexsideDirection ParseHexsideDirection(string? direction) => direction switch
    {
        "either" => MovementHexsideDirection.Either,
        "up" => MovementHexsideDirection.Up,
        "down" => MovementHexsideDirection.Down,
        _ => throw new JsonException($"Unknown Movement hexside direction '{direction}'."),
    };
}
