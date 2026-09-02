using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal sealed record CampaignObservationV6AuthorityFacts
{
    public CampaignObservationV6AuthorityFacts(
        IEnumerable<string> apparentEnemyControlledLocationIds,
        IEnumerable<string> apparentZocRepresentationIds)
    {
        ApparentEnemyControlledLocationIds = CopyCanonicalIds(
            apparentEnemyControlledLocationIds,
            nameof(apparentEnemyControlledLocationIds));
        ApparentZocRepresentationIds = CopyCanonicalIds(
            apparentZocRepresentationIds,
            nameof(apparentZocRepresentationIds));
    }

    public IReadOnlyList<string> ApparentEnemyControlledLocationIds { get; }

    public IReadOnlyList<string> ApparentZocRepresentationIds { get; }

    private static System.Collections.ObjectModel.ReadOnlyCollection<string> CopyCanonicalIds(
        IEnumerable<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.Select(value =>
            ContentContractGuards.RequireStableId(value, parameterName)).ToArray();
        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(
                "Side-safe Observation authority IDs must be unique.",
                parameterName);
        }

        return Array.AsReadOnly(copy.Order(StringComparer.Ordinal).ToArray());
    }
}

internal abstract record CampaignObservationDecisionState;

internal sealed record CampaignObservationNormalDecisionState :
    CampaignObservationDecisionState;

internal sealed record CampaignObservationPhasingWaitingDecisionState :
    CampaignObservationDecisionState
{
    public CampaignObservationPhasingWaitingDecisionState(string windowId)
    {
        WindowId = ContentContractGuards.RequireSha256(windowId, nameof(windowId));
    }

    public string WindowId { get; }
}

internal sealed record ObservedApparentReactionTrigger
{
    public ObservedApparentReactionTrigger(
        string apparentRepresentationId,
        string originLocationId,
        string destinationLocationId)
    {
        ApparentRepresentationId = ContentContractGuards.RequireStableId(
            apparentRepresentationId,
            nameof(apparentRepresentationId));
        OriginLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (string.Equals(OriginLocationId, DestinationLocationId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An apparent Reaction trigger must change location.",
                nameof(destinationLocationId));
        }
    }

    public string ApparentRepresentationId { get; }

    public string OriginLocationId { get; }

    public string DestinationLocationId { get; }
}

internal sealed record ObservedReactionOpportunity
{
    public ObservedReactionOpportunity(
        string opportunityId,
        IEnumerable<ObservedReactionMoveOption> moveOptions)
    {
        OpportunityId = ContentContractGuards.RequireSha256(
            opportunityId,
            nameof(opportunityId));
        var options = ContentContractGuards.CopyValues(moveOptions, nameof(moveOptions));
        if (options.Select(value => $"{value.OriginLocationId}\0{value.DestinationLocationId}")
            .Distinct(StringComparer.Ordinal).Count() != options.Length)
        {
            throw new ArgumentException(
                "Observed Reaction move options must be unique by route.",
                nameof(moveOptions));
        }

        MoveOptions = Array.AsReadOnly(options
            .OrderBy(value => value.OriginLocationId, StringComparer.Ordinal)
            .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal)
            .ToArray());
    }

    public string OpportunityId { get; }

    public IReadOnlyList<ObservedReactionMoveOption> MoveOptions { get; }

    public bool Equals(ObservedReactionOpportunity? other) => ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(OpportunityId, other.OpportunityId, StringComparison.Ordinal)
            && MoveOptions.SequenceEqual(other.MoveOptions));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(OpportunityId, StringComparer.Ordinal);
        foreach (var option in MoveOptions)
        {
            hash.Add(option);
        }

        return hash.ToHashCode();
    }
}

internal sealed record ObservedReactionParticipant
{
    public ObservedReactionParticipant(string opportunityId)
    {
        OpportunityId = ContentContractGuards.RequireSha256(
            opportunityId,
            nameof(opportunityId));
    }

    public string OpportunityId { get; }
}

internal sealed record ObservedReactionMoveOption
{
    public ObservedReactionMoveOption(
        string originLocationId,
        string destinationLocationId,
        MovementActionCostBreakdown costBreakdown)
    {
        OriginLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        if (string.Equals(OriginLocationId, DestinationLocationId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An observed Reaction move option must change location.",
                nameof(destinationLocationId));
        }

        CostBreakdown = costBreakdown ?? throw new ArgumentNullException(nameof(costBreakdown));
    }

    public string OriginLocationId { get; }
    public string DestinationLocationId { get; }
    public MovementActionCostBreakdown CostBreakdown { get; }
}

internal sealed record CampaignObservationReactingDecisionState :
    CampaignObservationDecisionState
{
    public CampaignObservationReactingDecisionState(
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

    public bool Equals(CampaignObservationReactingDecisionState? other) =>
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

internal sealed record CampaignObservationV6
{
    public const int CurrentContractVersion = 6;
    public const string CurrentPolicyId =
        "sandtable.observation.zoc-reaction-side-safe.v1";

    public CampaignObservationV6(
        int contractVersion,
        string policyId,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        string scenarioId,
        LandSide observer,
        CampaignObservationPosition position,
        CampaignObservationWeather? weather,
        IEnumerable<CampaignObservationLocation> locations,
        IEnumerable<CampaignObservationEdge> edges,
        IEnumerable<ObservedOwnElement> ownElements,
        IEnumerable<ObservedApparentPresence> apparentOpposingPresences,
        IEnumerable<string> apparentEnemyControlledLocationIds,
        IEnumerable<string> movementEndedElementIds,
        CampaignObservationDecisionState decisionState)
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
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
        {
            throw new ArgumentException(
                "The successor observation ruleset hash must identify the canonical ruleset.",
                nameof(rulesetHash));
        }

        if (!Enum.IsDefined(observer))
        {
            throw new ArgumentOutOfRangeException(nameof(observer));
        }

        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(decisionState);
        EnsureDecisionAudienceMatchesPosition(observer, position, decisionState);
        CampaignObservationV6DisclosureIdentity.EnsureOpportunityIdentities(
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

        if (decisionState is CampaignObservationReactingDecisionState
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
            || (decisionState is CampaignObservationReactingDecisionState reacting
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

        if (decisionState is CampaignObservationReactingDecisionState reactingDecision
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

    public LandSide Observer { get; }

    public CampaignObservationPosition Position { get; }

    public CampaignObservationWeather? Weather { get; }

    public IReadOnlyList<CampaignObservationLocation> Locations { get; }

    public IReadOnlyList<CampaignObservationEdge> Edges { get; }

    public IReadOnlyList<ObservedOwnElement> OwnElements { get; }

    public IReadOnlyList<ObservedApparentPresence> ApparentOpposingPresences { get; }

    public IReadOnlyList<string> ApparentEnemyControlledLocationIds { get; }

    public IReadOnlyList<string> MovementEndedElementIds { get; }

    public CampaignObservationDecisionState DecisionState { get; }

    public bool Equals(CampaignObservationV6? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(PolicyId, other.PolicyId, StringComparison.Ordinal)
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && string.Equals(RulesetHash, other.RulesetHash, StringComparison.Ordinal)
            && string.Equals(ScenarioId, other.ScenarioId, StringComparison.Ordinal)
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
        CampaignObservationDecisionState decisionState)
    {
        var coherent = decisionState switch
        {
            CampaignObservationPhasingWaitingDecisionState =>
                position.ActiveSide == observer,
            CampaignObservationReactingDecisionState =>
                position.ActiveSide is not null && position.ActiveSide != observer,
            _ => true,
        };
        if (!coherent)
        {
            throw new ArgumentException(
                "The successor decision state must agree with the observation audience and active phasing side.",
                nameof(decisionState));
        }
    }

    private static bool HasIncoherentReactionMoveOption(
        CampaignObservationReactingDecisionState reacting,
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
