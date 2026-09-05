using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignBreakdownRoute
{
    public CampaignBreakdownRoute(string routeId, long firstMoveStateVersion, string elementId,
        string representationId, LandSide owner, string originLocationId, string currentLocationId,
        IEnumerable<string> cohortIds)
    {
        RouteId = ContentContractGuards.RequireSha256(routeId, nameof(routeId));
        ArgumentOutOfRangeException.ThrowIfLessThan(firstMoveStateVersion, 2);
        FirstMoveStateVersion = firstMoveStateVersion;
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        RepresentationId = ContentContractGuards.RequireStableId(representationId, nameof(representationId));
        if (!Enum.IsDefined(owner)) throw new ArgumentOutOfRangeException(nameof(owner));
        Owner = owner;
        OriginLocationId = ContentContractGuards.RequireStableId(originLocationId, nameof(originLocationId));
        CurrentLocationId = ContentContractGuards.RequireStableId(currentLocationId, nameof(currentLocationId));
        var ids = ContentContractGuards.CopyValues(cohortIds, nameof(cohortIds));
        foreach (var id in ids) ContentContractGuards.RequireStableId(id, nameof(cohortIds));
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("Route cohort IDs must be unique.", nameof(cohortIds));
        CohortIds = Array.AsReadOnly(ids.Order(StringComparer.Ordinal).ToArray());
    }

    public string RouteId { get; }
    public long FirstMoveStateVersion { get; }
    public string ElementId { get; }
    public string RepresentationId { get; }
    public LandSide Owner { get; }
    public string OriginLocationId { get; }
    public string CurrentLocationId { get; }
    public IReadOnlyList<string> CohortIds { get; }

    public static CampaignBreakdownRoute Create(string campaignId, string rulesetHash,
        long firstMoveStateVersion, string elementId, string representationId, LandSide owner,
        string originLocationId, string currentLocationId, IEnumerable<string> cohortIds)
    {
        var route = new CampaignBreakdownRoute(CampaignBreakdownCodec.EmptyHash, firstMoveStateVersion,
            elementId, representationId, owner, originLocationId, currentLocationId, cohortIds);
        return new CampaignBreakdownRoute(CampaignBreakdownCodec.RouteId(campaignId, rulesetHash, route),
            firstMoveStateVersion, elementId, representationId, owner, originLocationId, currentLocationId, route.CohortIds);
    }

    public CampaignBreakdownRoute AtLocation(string locationId) => new(RouteId,
        FirstMoveStateVersion, ElementId, RepresentationId, Owner, OriginLocationId, locationId, CohortIds);

    public void ValidateIdentity(string campaignId, string rulesetHash)
    {
        if (RouteId != CampaignBreakdownCodec.RouteId(campaignId, rulesetHash, this))
            throw new ArgumentException("Invalid Breakdown route identity.");
    }

    public bool Equals(CampaignBreakdownRoute? other) => ReferenceEquals(this, other)
        || (other is not null && RouteId == other.RouteId && FirstMoveStateVersion == other.FirstMoveStateVersion
            && ElementId == other.ElementId && RepresentationId == other.RepresentationId && Owner == other.Owner
            && OriginLocationId == other.OriginLocationId && CurrentLocationId == other.CurrentLocationId
            && CohortIds.SequenceEqual(other.CohortIds));
    public override int GetHashCode() => HashCode.Combine(RouteId, CurrentLocationId);
}

internal sealed record CampaignBreakdownCheckInput
{
    public CampaignBreakdownCheckInput(string cohortId, string vehicleTypeId, string profileId,
        int workingPointCount, BreakdownPointAmount cumulativeBreakdownPoints,
        BreakdownPointAmount sandstormAttributedBreakdownPoints, string? highestEffectiveCheckedBandId)
    {
        var ledger = new CampaignVehicleBreakdownState(cohortId, cumulativeBreakdownPoints,
            sandstormAttributedBreakdownPoints, highestEffectiveCheckedBandId, workingPointCount, 0);
        CohortId = ledger.CohortId;
        VehicleTypeId = ContentContractGuards.RequireStableId(vehicleTypeId, nameof(vehicleTypeId));
        ProfileId = ContentContractGuards.RequireStableId(profileId, nameof(profileId));
        WorkingPointCount = ledger.WorkingPointCount;
        CumulativeBreakdownPoints = ledger.CumulativeBreakdownPoints;
        SandstormAttributedBreakdownPoints = ledger.SandstormAttributedBreakdownPoints;
        HighestEffectiveCheckedBandId = ledger.HighestEffectiveCheckedBandId;
    }
    public string CohortId { get; }
    public string VehicleTypeId { get; }
    public string ProfileId { get; }
    public int WorkingPointCount { get; }
    public BreakdownPointAmount CumulativeBreakdownPoints { get; }
    public BreakdownPointAmount SandstormAttributedBreakdownPoints { get; }
    public string? HighestEffectiveCheckedBandId { get; }
}

internal enum CampaignBreakdownStopReason
{
    Deliberate, MovementEnded, CpExhausted, ReactionCompleted, ReactionUnavailable, ReactionTimeout,
}

internal sealed record CampaignBreakdownStop
{
    public CampaignBreakdownStop(string stopId, long recordedStateVersion, CampaignBreakdownRoute route,
        CampaignBreakdownStopReason reason, BreakdownWeatherKind weatherKind,
        IEnumerable<CampaignBreakdownCheckInput> cohortInputs)
    {
        StopId = ContentContractGuards.RequireSha256(stopId, nameof(stopId));
        ArgumentNullException.ThrowIfNull(route);
        ArgumentOutOfRangeException.ThrowIfLessThan(recordedStateVersion, route.FirstMoveStateVersion);
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (!Enum.IsDefined(weatherKind)) throw new ArgumentOutOfRangeException(nameof(weatherKind));
        var inputs = ContentContractGuards.CopyValues(cohortInputs, nameof(cohortInputs))
            .OrderBy(value => value.CohortId, StringComparer.Ordinal).ToArray();
        if (!inputs.Select(value => value.CohortId).SequenceEqual(route.CohortIds))
            throw new ArgumentException("Stop inputs must equal the route cohort membership.", nameof(cohortInputs));
        RecordedStateVersion = recordedStateVersion;
        Route = route;
        Reason = reason;
        WeatherKind = weatherKind;
        CohortInputs = Array.AsReadOnly(inputs);
    }
    public string StopId { get; }
    public long RecordedStateVersion { get; }
    public CampaignBreakdownRoute Route { get; }
    public CampaignBreakdownStopReason Reason { get; }
    public BreakdownWeatherKind WeatherKind { get; }
    public IReadOnlyList<CampaignBreakdownCheckInput> CohortInputs { get; }

    public static CampaignBreakdownStop Create(string campaignId, string rulesetHash, long stateVersion,
        CampaignBreakdownRoute route, CampaignBreakdownStopReason reason, BreakdownWeatherKind weather,
        IEnumerable<CampaignBreakdownCheckInput> inputs)
    {
        route.ValidateIdentity(campaignId, rulesetHash);
        var stop = new CampaignBreakdownStop(CampaignBreakdownCodec.EmptyHash, stateVersion, route, reason, weather, inputs);
        return new CampaignBreakdownStop(CampaignBreakdownCodec.StopId(campaignId, rulesetHash, stop),
            stateVersion, route, reason, weather, stop.CohortInputs);
    }
    public void ValidateIdentity(string campaignId, string rulesetHash)
    {
        Route.ValidateIdentity(campaignId, rulesetHash);
        if (StopId != CampaignBreakdownCodec.StopId(campaignId, rulesetHash, this))
            throw new ArgumentException("Invalid Breakdown stop identity.");
    }
    public bool Equals(CampaignBreakdownStop? other) => ReferenceEquals(this, other)
        || (other is not null && StopId == other.StopId && RecordedStateVersion == other.RecordedStateVersion
            && Route == other.Route && Reason == other.Reason && WeatherKind == other.WeatherKind
            && CohortInputs.SequenceEqual(other.CohortInputs));
    public override int GetHashCode() => HashCode.Combine(StopId, Route);
}

internal abstract record CampaignPhasingContinuation
{
    private CampaignPhasingContinuation() { }
    internal sealed record ResumeRoute(CampaignBreakdownRoute Route) : CampaignPhasingContinuation;
    internal sealed record ResolveStop(CampaignBreakdownStop Stop) : CampaignPhasingContinuation;
}

internal abstract record CampaignBreakdownFlow
{
    private CampaignBreakdownFlow() { }
    internal sealed record Idle : CampaignBreakdownFlow;
    internal sealed record Moving(CampaignBreakdownRoute Route) : CampaignBreakdownFlow;
    internal sealed record Reacting(CampaignPhasingContinuation PhasingContinuation,
        CampaignBreakdownRoute? ReactorRoute) : CampaignBreakdownFlow;
    internal sealed record ReactorStopOpen(CampaignPhasingContinuation PhasingContinuation,
        CampaignBreakdownStop Stop) : CampaignBreakdownFlow;
    internal sealed record ReactorStopClosed(CampaignPhasingContinuation PhasingContinuation,
        CampaignBreakdownStop Stop) : CampaignBreakdownFlow;
    internal sealed record PhasingStop(CampaignBreakdownStop Stop) : CampaignBreakdownFlow;
}

internal sealed record CampaignBrokenVehicleLot
{
    public CampaignBrokenVehicleLot(string lotId, string stopId, string checkId, LandSide owner,
        string cohortId, string vehicleTypeId, int pointCount, string locationId,
        long createdStateVersion, IEnumerable<RuleReference> sources)
    {
        LotId = ContentContractGuards.RequireSha256(lotId, nameof(lotId));
        StopId = ContentContractGuards.RequireSha256(stopId, nameof(stopId));
        CheckId = ContentContractGuards.RequireSha256(checkId, nameof(checkId));
        if (!Enum.IsDefined(owner)) throw new ArgumentOutOfRangeException(nameof(owner));
        Owner = owner;
        CohortId = ContentContractGuards.RequireStableId(cohortId, nameof(cohortId));
        VehicleTypeId = ContentContractGuards.RequireStableId(vehicleTypeId, nameof(vehicleTypeId));
        ArgumentOutOfRangeException.ThrowIfLessThan(pointCount, 1);
        PointCount = pointCount;
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        ArgumentOutOfRangeException.ThrowIfLessThan(createdStateVersion, 2);
        CreatedStateVersion = createdStateVersion;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }
    public string LotId { get; }
    public string StopId { get; }
    public string CheckId { get; }
    public LandSide Owner { get; }
    public string CohortId { get; }
    public string VehicleTypeId { get; }
    public int PointCount { get; }
    public string LocationId { get; }
    public long CreatedStateVersion { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
    public static CampaignBrokenVehicleLot Create(string campaignId, string rulesetHash, string stopId,
        LandSide owner, string cohortId, string vehicleTypeId, int pointCount, string locationId,
        long createdStateVersion, IEnumerable<RuleReference> sources)
    {
        var checkId = CampaignBreakdownCodec.CheckId(campaignId, rulesetHash, stopId, cohortId);
        var lot = new CampaignBrokenVehicleLot(CampaignBreakdownCodec.EmptyHash, stopId, checkId,
            owner, cohortId, vehicleTypeId, pointCount, locationId, createdStateVersion, sources);
        return new CampaignBrokenVehicleLot(CampaignBreakdownCodec.LotId(campaignId, rulesetHash, lot),
            stopId, checkId, owner, cohortId, vehicleTypeId, pointCount, locationId, createdStateVersion, lot.Sources);
    }
    public void ValidateIdentity(string campaignId, string rulesetHash)
    {
        if (CheckId != CampaignBreakdownCodec.CheckId(campaignId, rulesetHash, StopId, CohortId)
            || LotId != CampaignBreakdownCodec.LotId(campaignId, rulesetHash, this))
            throw new ArgumentException("Invalid Breakdown lot identity.");
    }
    public bool Equals(CampaignBrokenVehicleLot? other) => ReferenceEquals(this, other)
        || (other is not null && LotId == other.LotId && StopId == other.StopId && CheckId == other.CheckId
            && Owner == other.Owner && CohortId == other.CohortId && VehicleTypeId == other.VehicleTypeId
            && PointCount == other.PointCount && LocationId == other.LocationId
            && CreatedStateVersion == other.CreatedStateVersion && Sources.SequenceEqual(other.Sources));
    public override int GetHashCode() => HashCode.Combine(LotId, PointCount);
}
