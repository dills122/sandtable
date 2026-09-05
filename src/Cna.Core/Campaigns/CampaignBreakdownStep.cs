using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignBreakdownCrossedHexside
{
    public CampaignBreakdownCrossedHexside(string hexsideId, BreakdownHexsideDirection direction,
        BreakdownPointAmount addedPoints, IEnumerable<RuleReference> sources)
    {
        HexsideId = ContentContractGuards.RequireStableId(hexsideId, nameof(hexsideId));
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        ArgumentNullException.ThrowIfNull(addedPoints);
        Direction = direction; AddedPoints = addedPoints;
        Sources = CampaignBreakdownStep.CopySources(sources);
    }
    public string HexsideId { get; }
    public BreakdownHexsideDirection Direction { get; }
    public BreakdownPointAmount AddedPoints { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignBreakdownCrossedHexside? other) => ReferenceEquals(this, other)
        || (other is not null && HexsideId == other.HexsideId && Direction == other.Direction
            && AddedPoints == other.AddedPoints && Sources.SequenceEqual(other.Sources));
    public override int GetHashCode() => HashCode.Combine(HexsideId, Direction, AddedPoints);
}

internal sealed record CampaignBreakdownStep
{
    public CampaignBreakdownStep(string cohortId, string vehicleTypeId, string profileId,
        string destinationTerrainId, string? inputRouteId, string? effectiveRouteId,
        IEnumerable<CampaignBreakdownCrossedHexside> hexsides, BreakdownWeatherKind weatherKind,
        BreakdownPointAmount before, BreakdownPointAmount delta, BreakdownPointAmount after,
        BreakdownPointAmount sandstormBefore, BreakdownPointAmount sandstormDelta,
        BreakdownPointAmount sandstormAfter, IEnumerable<RuleReference> sources)
    {
        CohortId = ContentContractGuards.RequireStableId(cohortId, nameof(cohortId));
        VehicleTypeId = ContentContractGuards.RequireStableId(vehicleTypeId, nameof(vehicleTypeId));
        ProfileId = ContentContractGuards.RequireStableId(profileId, nameof(profileId));
        DestinationTerrainId = ContentContractGuards.RequireStableId(destinationTerrainId, nameof(destinationTerrainId));
        InputRouteId = inputRouteId is null ? null : ContentContractGuards.RequireStableId(inputRouteId, nameof(inputRouteId));
        EffectiveRouteId = effectiveRouteId is null ? null : ContentContractGuards.RequireStableId(effectiveRouteId, nameof(effectiveRouteId));
        if ((inputRouteId is null) != (effectiveRouteId is null)) throw new ArgumentException("Route evidence must be paired.");
        if (!Enum.IsDefined(weatherKind)) throw new ArgumentOutOfRangeException(nameof(weatherKind));
        var copy = ContentContractGuards.CopyValues(hexsides, nameof(hexsides));
        if (copy.Select(value => (value.HexsideId, value.Direction)).Distinct().Count() != copy.Length)
            throw new ArgumentException("Duplicate crossed hexside.", nameof(hexsides));
        Hexsides = Array.AsReadOnly(copy.OrderBy(value => value.HexsideId, StringComparer.Ordinal)
            .ThenBy(value => CampaignBreakdownStepCodec.FormatDirection(value.Direction), StringComparer.Ordinal).ToArray());
        ArgumentNullException.ThrowIfNull(before); ArgumentNullException.ThrowIfNull(delta); ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(sandstormBefore); ArgumentNullException.ThrowIfNull(sandstormDelta); ArgumentNullException.ThrowIfNull(sandstormAfter);
        if (before + delta != after || sandstormBefore + sandstormDelta != sandstormAfter
            || sandstormBefore > before || sandstormAfter > after
            || sandstormDelta != (weatherKind == BreakdownWeatherKind.Sandstorm ? delta : BreakdownPointAmount.Zero))
            throw new ArgumentException("Invalid Breakdown running totals.");
        WeatherKind = weatherKind; Before = before; Delta = delta; After = after;
        SandstormBefore = sandstormBefore; SandstormDelta = sandstormDelta; SandstormAfter = sandstormAfter;
        Sources = CopySources(sources);
    }
    public string CohortId { get; }
    public string VehicleTypeId { get; }
    public string ProfileId { get; }
    public string DestinationTerrainId { get; }
    public string? InputRouteId { get; }
    public string? EffectiveRouteId { get; }
    public IReadOnlyList<CampaignBreakdownCrossedHexside> Hexsides { get; }
    public BreakdownWeatherKind WeatherKind { get; }
    public BreakdownPointAmount Before { get; }
    public BreakdownPointAmount Delta { get; }
    public BreakdownPointAmount After { get; }
    public BreakdownPointAmount SandstormBefore { get; }
    public BreakdownPointAmount SandstormDelta { get; }
    public BreakdownPointAmount SandstormAfter { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignBreakdownStep? other) => ReferenceEquals(this, other)
        || (other is not null && CohortId == other.CohortId && VehicleTypeId == other.VehicleTypeId
            && ProfileId == other.ProfileId && DestinationTerrainId == other.DestinationTerrainId
            && InputRouteId == other.InputRouteId && EffectiveRouteId == other.EffectiveRouteId
            && WeatherKind == other.WeatherKind && Before == other.Before && Delta == other.Delta
            && After == other.After && SandstormBefore == other.SandstormBefore
            && SandstormDelta == other.SandstormDelta && SandstormAfter == other.SandstormAfter
            && Hexsides.SequenceEqual(other.Hexsides) && Sources.SequenceEqual(other.Sources));
    public override int GetHashCode() => HashCode.Combine(CohortId, VehicleTypeId, ProfileId, After, SandstormAfter);

    internal static IReadOnlyList<RuleReference> CopySources(IEnumerable<RuleReference> sources) =>
        new ContentOrigin(ContentOriginKind.SourceDerived, sources).References;
}
