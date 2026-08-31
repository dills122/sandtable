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
    public ObservedReactionOpportunity(string opportunityId, string representationId)
    {
        OpportunityId = ContentContractGuards.RequireSha256(
            opportunityId,
            nameof(opportunityId));
        RepresentationId = ContentContractGuards.RequireStableId(
            representationId,
            nameof(representationId));
    }

    public string OpportunityId { get; }

    public string RepresentationId { get; }
}

internal sealed record ObservedReactionParticipant
{
    public ObservedReactionParticipant(string opportunityId, string representationId)
    {
        OpportunityId = ContentContractGuards.RequireSha256(
            opportunityId,
            nameof(opportunityId));
        RepresentationId = ContentContractGuards.RequireStableId(
            representationId,
            nameof(representationId));
    }

    public string OpportunityId { get; }

    public string RepresentationId { get; }
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
                .Distinct(StringComparer.Ordinal).Count() != opportunities.Length
            || opportunities.Select(value => value.RepresentationId)
                .Distinct(StringComparer.Ordinal).Count() != opportunities.Length)
        {
            throw new ArgumentException(
                "Observed own Reaction opportunities must have unique identities and representations.",
                nameof(ownOpportunities));
        }

        var ordered = opportunities
            .OrderBy(value => value.OpportunityId, StringComparer.Ordinal)
            .ToArray();
        if (activeParticipant is not null
            && !ordered.Any(value =>
                string.Equals(value.OpportunityId,
                    activeParticipant.OpportunityId, StringComparison.Ordinal)
                && string.Equals(value.RepresentationId,
                    activeParticipant.RepresentationId, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "The active observed participant must be a current own opportunity.",
                nameof(activeParticipant));
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

        EnsureUnique(locationCopy.Select(value => value.LocationId), nameof(locations));
        EnsureUnique(
            edgeCopy.Select(value => $"{value.FirstLocationId}\0{value.SecondLocationId}"),
            nameof(edges));
        EnsureUnique(ownElementCopy.Select(value => value.ElementId), nameof(ownElements));
        EnsureUnique(
            apparentPresenceCopy.Select(value => value.RepresentationId),
            nameof(apparentOpposingPresences));
        EnsureUnique(controlledCopy, nameof(apparentEnemyControlledLocationIds));

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
                        reacting.ApparentTrigger.DestinationLocationId))))
        {
            throw new ArgumentException(
                "Every successor observation topology reference must name a published location.");
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

    private static void AddValues<T>(ref HashCode hash, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            hash.Add(value);
        }
    }
}
