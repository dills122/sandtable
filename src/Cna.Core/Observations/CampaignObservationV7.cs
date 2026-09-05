using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public abstract record CampaignObservationV7DecisionState;

public sealed record CampaignObservationV7NormalDecisionState(ObservedMovementRoute? ActiveMovement = null) :
    CampaignObservationV7DecisionState;

public sealed record CampaignObservationV7BreakdownWaitingDecisionState : CampaignObservationV7DecisionState;

public sealed record CampaignObservationV7PhasingWaitingDecisionState :
    CampaignObservationV7DecisionState
{
    public CampaignObservationV7PhasingWaitingDecisionState(string windowId)
    {
        WindowId = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
    }

    public string WindowId { get; }
}

public sealed record CampaignObservationV7ReactingDecisionState :
    CampaignObservationV7DecisionState
{
    public CampaignObservationV7ReactingDecisionState(
        string windowId,
        ObservedApparentReactionTrigger apparentTrigger,
        IEnumerable<ObservedReactionOpportunity> ownOpportunities,
        ObservedReactionParticipant? activeParticipant)
    {
        WindowId = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
        ArgumentNullException.ThrowIfNull(apparentTrigger);
        var opportunities = ContentContractGuards.CopyValues(
            ownOpportunities,
            nameof(ownOpportunities));
        if (opportunities.Select(value => value.OpportunityId)
                .Distinct(StringComparer.Ordinal).Count() != opportunities.Length)
        {
            throw new ArgumentException(
                "Observed own Reaction opportunities must have unique identities.",
                nameof(ownOpportunities));
        }

        var ordered = opportunities
            .OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
            .ToArray();
        if (activeParticipant is not null
            && !ordered.Any(value =>
                string.Equals(value.OpportunityId,
                    activeParticipant.OpportunityId, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "The active observed participant must be a current own opportunity.",
                nameof(activeParticipant));
        }

        if (ordered.Any(value =>
                value.MoveOptions.Count == 0
                && (activeParticipant is null
                    || !string.Equals(
                        value.OpportunityId,
                        activeParticipant.OpportunityId,
                        StringComparison.Ordinal))))
        {
            throw new ArgumentException(
                "Only the active observed Reaction participant may have no current move options.",
                nameof(ownOpportunities));
        }

        ApparentTrigger = apparentTrigger;
        OwnOpportunities = Array.AsReadOnly(ordered);
        ActiveParticipant = activeParticipant;
    }

    public string WindowId { get; }

    public ObservedApparentReactionTrigger ApparentTrigger { get; }

    public IReadOnlyList<ObservedReactionOpportunity> OwnOpportunities { get; }

    public ObservedReactionParticipant? ActiveParticipant { get; }

    public bool Equals(CampaignObservationV7ReactingDecisionState? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(WindowId, other.WindowId, StringComparison.Ordinal)
            && ApparentTrigger == other.ApparentTrigger
            && OwnOpportunities.SequenceEqual(other.OwnOpportunities)
            && ActiveParticipant == other.ActiveParticipant);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(WindowId, StringComparer.Ordinal);
        hash.Add(ApparentTrigger);
        foreach (var opportunity in OwnOpportunities)
        {
            hash.Add(opportunity);
        }

        hash.Add(ActiveParticipant);
        return hash.ToHashCode();
    }
}

public sealed record CampaignObservationV7
{
    public const int CurrentContractVersion = 7;
    public const string CurrentPolicyId =
        "sandtable.observation.breakdown-side-safe.v1";

    public CampaignObservationV7(
        int contractVersion,
        string policyId,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        string scenarioId,
        string capabilityProfileId,
        LandSide observer,
        CampaignObservationPosition position,
        CampaignObservationWeather? weather,
        IEnumerable<CampaignObservationLocation> locations,
        IEnumerable<CampaignObservationEdge> edges,
        IEnumerable<ObservedOwnElement> ownElements,
        IEnumerable<ObservedApparentPresence> apparentOpposingPresences,
        IEnumerable<string> apparentEnemyControlledLocationIds,
        IEnumerable<string> movementEndedElementIds,
        CampaignObservationV7DecisionState decisionState,
        IEnumerable<ObservedBrokenVehicleLot> ownBrokenVehicleLots)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        if (!string.Equals(policyId, CurrentPolicyId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The only supported successor observation policy is '{CurrentPolicyId}'.",
                nameof(policyId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash))
        {
            throw new ArgumentException(
                "The successor observation ruleset hash must identify the canonical ruleset.",
                nameof(rulesetHash));
        }

        if (!Enum.IsDefined(observer))
        {
            throw new ArgumentOutOfRangeException(nameof(observer));
        }

        if (capabilityProfileId != ContentPackV6Definition.SupportedCapabilityProfileId)
            throw new ArgumentException("Unsupported capability profile.", nameof(capabilityProfileId));
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(decisionState);
        EnsureDecisionAudienceMatchesPosition(observer, position, decisionState);
        CampaignObservationV7DisclosureIdentity.EnsureOpportunityIdentities(
            stateVersion,
            decisionState);
        var locationCopy = ContentContractGuards.CopyValues(locations, nameof(locations));
        var edgeCopy = ContentContractGuards.CopyValues(edges, nameof(edges));
        var ownElementCopy = ContentContractGuards.CopyValues(ownElements, nameof(ownElements));
        var apparentPresenceCopy = ContentContractGuards.CopyValues(
            apparentOpposingPresences,
            nameof(apparentOpposingPresences));
        ArgumentNullException.ThrowIfNull(apparentEnemyControlledLocationIds);
        var controlledCopy = apparentEnemyControlledLocationIds.Select(value =>
            ContentContractGuards.RequireStableId(
                value,
                nameof(apparentEnemyControlledLocationIds))).ToArray();
        ArgumentNullException.ThrowIfNull(movementEndedElementIds);
        var movementEndedCopy = movementEndedElementIds.Select(value =>
            ContentContractGuards.RequireStableId(
                value,
                nameof(movementEndedElementIds))).ToArray();

        EnsureUnique(locationCopy.Select(value => value.LocationId), nameof(locations));
        EnsureUnique(
            edgeCopy.Select(value => $"{value.FirstLocationId}\0{value.SecondLocationId}"),
            nameof(edges));
        EnsureUnique(ownElementCopy.Select(value => value.ElementId), nameof(ownElements));
        EnsureUnique(
            apparentPresenceCopy.Select(value => value.RepresentationId),
            nameof(apparentOpposingPresences));
        EnsureUnique(controlledCopy, nameof(apparentEnemyControlledLocationIds));
        EnsureUnique(movementEndedCopy, nameof(movementEndedElementIds));
        var ownElementIds = ownElementCopy.Select(value => value.ElementId)
            .ToHashSet(StringComparer.Ordinal);
        if (movementEndedCopy.Any(value => !ownElementIds.Contains(value)))
        {
            throw new ArgumentException(
                "Movement-ended elements must belong to the observation audience.",
                nameof(movementEndedElementIds));
        }

        if (decisionState is CampaignObservationV7ReactingDecisionState
            && (ownElementCopy.Length != 0 || movementEndedCopy.Length != 0))
        {
            throw new ArgumentException(
                "A reacting observation cannot contain identity-bearing owner rows.",
                nameof(decisionState));
        }

        var knownLocations = locationCopy.Select(value => value.LocationId)
            .ToHashSet(StringComparer.Ordinal);
        if (edgeCopy.Any(edge =>
                !knownLocations.Contains(edge.FirstLocationId)
                || !knownLocations.Contains(edge.SecondLocationId)
                || edge.Features.Any(feature =>
                    feature.DirectionFromLocationId is not null
                    && !knownLocations.Contains(feature.DirectionFromLocationId)))
            || ownElementCopy.Any(value => !knownLocations.Contains(value.CurrentLocationId))
            || apparentPresenceCopy.Any(value =>
                !knownLocations.Contains(value.CurrentLocationId))
            || controlledCopy.Any(value => !knownLocations.Contains(value))
            || (decisionState is CampaignObservationV7ReactingDecisionState reacting
                && (!knownLocations.Contains(reacting.ApparentTrigger.OriginLocationId)
                    || !knownLocations.Contains(
                        reacting.ApparentTrigger.DestinationLocationId)
                    || reacting.OwnOpportunities.Any(value =>
                        value.MoveOptions.Any(option =>
                            !knownLocations.Contains(option.OriginLocationId)
                            || !knownLocations.Contains(option.DestinationLocationId))))))
        {
            throw new ArgumentException(
                "Every successor observation topology reference must name a published location.");
        }

        if (decisionState is CampaignObservationV7ReactingDecisionState reactingDecision
            && HasIncoherentReactionMoveOption(
                reactingDecision,
                locationCopy,
                edgeCopy,
                apparentPresenceCopy,
                controlledCopy))
        {
            throw new ArgumentException(
                "A reacting move option must agree with the published topology and visible blocking facts.",
                nameof(decisionState));
        }

        var lotCopy = ContentContractGuards.CopyValues(ownBrokenVehicleLots, nameof(ownBrokenVehicleLots));
        EnsureUnique(lotCopy.Select(value => $"{value.CohortId}\0{value.LocationId}"), nameof(ownBrokenVehicleLots));
        var ownCohorts = ownElementCopy.Where(value => value.VehicleBreakdownRisk is not null)
            .Select(value => value.VehicleBreakdownRisk!.CohortId).ToHashSet(StringComparer.Ordinal);
        if (lotCopy.Any(value => !knownLocations.Contains(value.LocationId) || !ownCohorts.Contains(value.CohortId)))
            throw new ArgumentException("Observed lots require published own cohort and location.", nameof(ownBrokenVehicleLots));
        var lotTotals = lotCopy.GroupBy(value => value.CohortId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key,
                group => group.Aggregate(0, (total, value) => checked(total + value.PointCount)), StringComparer.Ordinal);
        if (ownElementCopy.Where(value => value.VehicleBreakdownRisk is not null)
            .Select(value => value.VehicleBreakdownRisk!)
            .Any(value => lotTotals.GetValueOrDefault(value.CohortId) != value.BrokenPointCount))
            throw new ArgumentException("Observed lot totals must equal published own broken counts.", nameof(ownBrokenVehicleLots));
        if (decisionState is CampaignObservationV7NormalDecisionState { ActiveMovement: { } route }
            && (position.ActiveSide != observer || position.SegmentId != LandSegmentIds.Movement
                || !knownLocations.Contains(route.OriginLocationId) || !knownLocations.Contains(route.CurrentLocationId)
                || !ownElementCopy.Any(value => value.ElementId == route.ElementId && value.CurrentLocationId == route.CurrentLocationId)
                || route.RouteId != CampaignObservationV7DisclosureIdentity.CreateRoute(campaignId, rulesetHash, stateVersion,
                    observer, route.ElementId, route.OriginLocationId, route.CurrentLocationId)))
            throw new ArgumentException("Active movement must bind exact own public route capability.", nameof(decisionState));
        CapabilityProfileId = capabilityProfileId;
        OwnBrokenVehicleLots = Array.AsReadOnly(lotCopy.OrderBy(value => value.CohortId, StringComparer.Ordinal)
            .ThenBy(value => value.LocationId, StringComparer.Ordinal).ToArray());
        ContractVersion = contractVersion;
        PolicyId = policyId;
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
        Observer = observer;
        Position = position;
        Weather = weather;
        Locations = Array.AsReadOnly(locationCopy
            .OrderBy(value => value.LocationId, StringComparer.Ordinal).ToArray());
        Edges = Array.AsReadOnly(edgeCopy
            .OrderBy(value => value.FirstLocationId, StringComparer.Ordinal)
            .ThenBy(value => value.SecondLocationId, StringComparer.Ordinal).ToArray());
        OwnElements = Array.AsReadOnly(ownElementCopy
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
        ApparentOpposingPresences = Array.AsReadOnly(apparentPresenceCopy
            .OrderBy(value => value.RepresentationId, StringComparer.Ordinal).ToArray());
        ApparentEnemyControlledLocationIds = Array.AsReadOnly(controlledCopy
            .Order(StringComparer.Ordinal).ToArray());
        MovementEndedElementIds = Array.AsReadOnly(movementEndedCopy
            .Order(StringComparer.Ordinal).ToArray());
        DecisionState = decisionState;
    }

    public int ContractVersion { get; }

    public string PolicyId { get; }

    public string CampaignId { get; }

    public long StateVersion { get; }

    public string RulesetHash { get; }

    public string ScenarioId { get; }

    public string CapabilityProfileId { get; }

    public IReadOnlyList<ObservedBrokenVehicleLot> OwnBrokenVehicleLots { get; }

    public LandSide Observer { get; }

    public CampaignObservationPosition Position { get; }

    public CampaignObservationWeather? Weather { get; }

    public IReadOnlyList<CampaignObservationLocation> Locations { get; }

    public IReadOnlyList<CampaignObservationEdge> Edges { get; }

    public IReadOnlyList<ObservedOwnElement> OwnElements { get; }

    public IReadOnlyList<ObservedApparentPresence> ApparentOpposingPresences { get; }

    public IReadOnlyList<string> ApparentEnemyControlledLocationIds { get; }

    public IReadOnlyList<string> MovementEndedElementIds { get; }

    public CampaignObservationV7DecisionState DecisionState { get; }

    public bool Equals(CampaignObservationV7? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(PolicyId, other.PolicyId, StringComparison.Ordinal)
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && string.Equals(RulesetHash, other.RulesetHash, StringComparison.Ordinal)
            && string.Equals(ScenarioId, other.ScenarioId, StringComparison.Ordinal)
            && CapabilityProfileId == other.CapabilityProfileId
            && OwnBrokenVehicleLots.SequenceEqual(other.OwnBrokenVehicleLots)
            && Observer == other.Observer
            && Position == other.Position
            && Weather == other.Weather
            && Locations.SequenceEqual(other.Locations)
            && Edges.SequenceEqual(other.Edges)
            && OwnElements.SequenceEqual(other.OwnElements)
            && ApparentOpposingPresences.SequenceEqual(other.ApparentOpposingPresences)
            && ApparentEnemyControlledLocationIds.SequenceEqual(
                other.ApparentEnemyControlledLocationIds)
            && MovementEndedElementIds.SequenceEqual(other.MovementEndedElementIds)
            && DecisionState == other.DecisionState);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(PolicyId, StringComparer.Ordinal);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(RulesetHash, StringComparer.Ordinal);
        hash.Add(ScenarioId, StringComparer.Ordinal);
        hash.Add(CapabilityProfileId, StringComparer.Ordinal);
        AddValues(ref hash, OwnBrokenVehicleLots);
        hash.Add(Observer);
        hash.Add(Position);
        hash.Add(Weather);
        AddValues(ref hash, Locations);
        AddValues(ref hash, Edges);
        AddValues(ref hash, OwnElements);
        AddValues(ref hash, ApparentOpposingPresences);
        AddValues(ref hash, ApparentEnemyControlledLocationIds);
        AddValues(ref hash, MovementEndedElementIds);
        hash.Add(DecisionState);
        return hash.ToHashCode();
    }

    private static void EnsureUnique(IEnumerable<string> values, string parameterName)
    {
        var copy = values.ToArray();
        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(
                "Successor observation collection identities must be unique.",
                parameterName);
        }
    }

    private static void EnsureDecisionAudienceMatchesPosition(
        LandSide observer,
        CampaignObservationPosition position,
        CampaignObservationV7DecisionState decisionState)
    {
        var coherent = decisionState switch
        {
            CampaignObservationV7PhasingWaitingDecisionState =>
                position.ActiveSide == observer,
            CampaignObservationV7ReactingDecisionState =>
                position.ActiveSide is not null && position.ActiveSide != observer,
            CampaignObservationV7BreakdownWaitingDecisionState =>
                position.PositionId == "land.position.breakdown-stop" && position.ActorRole == LandActorRole.None && position.ActiveSide is null,
            CampaignObservationV7NormalDecisionState => position.PositionId != "land.position.breakdown-stop",
            _ => false,
        };
        if (!coherent)
        {
            throw new ArgumentException(
                "The successor decision state must agree with the observation audience and active phasing side.",
                nameof(decisionState));
        }
    }

    private static bool HasIncoherentReactionMoveOption(
        CampaignObservationV7ReactingDecisionState reacting,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<ObservedApparentPresence> apparentOpposingPresences,
        IReadOnlyList<string> controlledLocationIds)
    {
        var terrainByLocation = locations.ToDictionary(
            value => value.LocationId,
            value => value.TerrainId,
            StringComparer.Ordinal);
        var blocked = apparentOpposingPresences.Select(value => value.CurrentLocationId)
            .Concat(controlledLocationIds)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var opportunity in reacting.OwnOpportunities)
        {
            if (opportunity.MoveOptions.Select(value => value.OriginLocationId)
                .Distinct(StringComparer.Ordinal).Skip(1).Any())
            {
                return true;
            }

            foreach (var option in opportunity.MoveOptions)
            {
                var edge = edges.SingleOrDefault(value =>
                    (string.Equals(value.FirstLocationId,
                        option.OriginLocationId, StringComparison.Ordinal)
                        && string.Equals(value.SecondLocationId,
                            option.DestinationLocationId, StringComparison.Ordinal))
                    || (string.Equals(value.SecondLocationId,
                        option.OriginLocationId, StringComparison.Ordinal)
                        && string.Equals(value.FirstLocationId,
                            option.DestinationLocationId, StringComparison.Ordinal)));
                if (blocked.Contains(option.OriginLocationId)
                    || blocked.Contains(option.DestinationLocationId)
                    || !string.Equals(
                        terrainByLocation[option.DestinationLocationId],
                        option.CostBreakdown.DestinationTerrainId,
                        StringComparison.Ordinal)
                    || edge is null
                    || !CostMatchesPublishedTraversal(option, edge))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CostMatchesPublishedTraversal(
        ObservedReactionMoveOption option,
        CampaignObservationEdge edge) => Cna1979Movement.Mobility.Any(mobility =>
            CostMatchesPublishedTraversal(option, edge, mobility.MobilityId));

    private static bool CostMatchesPublishedTraversal(
        ObservedReactionMoveOption option,
        CampaignObservationEdge edge,
        string mobilityId)
    {
        var cost = option.CostBreakdown;
        var terrain = Cna1979Movement.LookupTerrain(
            cost.DestinationTerrainId,
            mobilityId);
        if (!terrain.IsSupported || terrain.Value.Cost != cost.DestinationTerrainCost)
        {
            return false;
        }

        MovementActionRouteAdjustment? expectedRoute = null;
        var expectedHexsides = new List<MovementActionHexsideCost>();
        foreach (var feature in edge.Features)
        {
            var route = Cna1979Movement.LookupRoute(feature.FeatureId, mobilityId);
            if (route.IsSupported)
            {
                if (feature.DirectionFromLocationId is not null || expectedRoute is not null)
                {
                    return false;
                }

                expectedRoute = new MovementActionRouteAdjustment(
                    feature.FeatureId,
                    route.Value.CostKind,
                    route.Value.Amount);
                continue;
            }

            var direction = feature.DirectionFromLocationId switch
            {
                null => MovementHexsideDirection.Either,
                var from when string.Equals(
                    from,
                    option.OriginLocationId,
                    StringComparison.Ordinal) => MovementHexsideDirection.Up,
                var from when string.Equals(
                    from,
                    option.DestinationLocationId,
                    StringComparison.Ordinal) => MovementHexsideDirection.Down,
                _ => (MovementHexsideDirection?)null,
            };
            if (direction is null)
            {
                return false;
            }

            var hexside = Cna1979Movement.LookupHexside(
                feature.FeatureId,
                direction.Value,
                mobilityId);
            if (!hexside.IsSupported)
            {
                return false;
            }

            expectedHexsides.Add(new MovementActionHexsideCost(
                feature.FeatureId,
                direction.Value,
                hexside.Value.AddedCost));
        }

        return expectedRoute == cost.RouteAdjustment
            && expectedHexsides
                .OrderBy(value => value.HexsideId, StringComparer.Ordinal)
                .ThenBy(value => value.Direction)
                .SequenceEqual(cost.CrossedHexsideCosts);
    }

    private static void AddValues<T>(ref HashCode hash, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            hash.Add(value);
        }
    }
}

public sealed record ObservedMovementRoute
{
    public ObservedMovementRoute(string routeId, string elementId, string originLocationId, string currentLocationId)
    {
        RouteId = ContentContractGuards.RequireSha256(routeId, nameof(routeId));
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        OriginLocationId = ContentContractGuards.RequireStableId(originLocationId, nameof(originLocationId));
        CurrentLocationId = ContentContractGuards.RequireStableId(currentLocationId, nameof(currentLocationId));
    }
    public string RouteId { get; }
    public string ElementId { get; }
    public string OriginLocationId { get; }
    public string CurrentLocationId { get; }
}

public sealed record ObservedBrokenVehicleLot
{
    public ObservedBrokenVehicleLot(string cohortId, string locationId, int pointCount)
    {
        CohortId = ContentContractGuards.RequireStableId(cohortId, nameof(cohortId));
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        ArgumentOutOfRangeException.ThrowIfLessThan(pointCount, 1);
        PointCount = pointCount;
    }
    public string CohortId { get; }
    public string LocationId { get; }
    public int PointCount { get; }
}
