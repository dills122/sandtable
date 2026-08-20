using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public sealed record CampaignObservation
{
    public const int CurrentContractVersion = 2;
    public const string CurrentPolicyId = "sandtable.observation.own-elements-only.v1";

    internal CampaignObservation(
        int contractVersion,
        string policyId,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        string scenarioId,
        LandSide observer,
        CampaignObservationPosition position,
        CampaignObservationWeather? weather,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<ObservedOwnElement> ownElements)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);

        if (!string.Equals(policyId, CurrentPolicyId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The only supported observation policy is '{CurrentPolicyId}'.",
                nameof(policyId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);

        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
        {
            throw new ArgumentException(
                "The observation ruleset hash must identify the canonical ruleset.",
                nameof(rulesetHash));
        }

        if (!Enum.IsDefined(observer))
        {
            throw new ArgumentOutOfRangeException(nameof(observer));
        }

        ArgumentNullException.ThrowIfNull(position);
        var locationCopy = ContentContractGuards.CopyValues(locations, nameof(locations));
        var edgeCopy = ContentContractGuards.CopyValues(edges, nameof(edges));
        var ownElementCopy = ContentContractGuards.CopyValues(ownElements, nameof(ownElements));

        EnsureUnique(
            locationCopy.Select(location => location.LocationId),
            "Observation location IDs must be unique.",
            nameof(locations));
        EnsureUnique(
            edgeCopy.Select(edge => $"{edge.FirstLocationId}\0{edge.SecondLocationId}"),
            "Observation edge endpoint pairs must be unique.",
            nameof(edges));
        EnsureUnique(
            ownElementCopy.Select(element => element.ElementId),
            "Observed own-element IDs must be unique.",
            nameof(ownElements));

        var knownLocations = locationCopy
            .Select(location => location.LocationId)
            .ToHashSet(StringComparer.Ordinal);

        if (edgeCopy.Any(edge => !knownLocations.Contains(edge.FirstLocationId)
            || !knownLocations.Contains(edge.SecondLocationId)
            || edge.Features.Any(feature => feature.DirectionFromLocationId is not null
                && !knownLocations.Contains(feature.DirectionFromLocationId))))
        {
            throw new ArgumentException(
                "Every observation edge and direction must reference a known location.",
                nameof(edges));
        }

        if (ownElementCopy.Any(element => !knownLocations.Contains(element.CurrentLocationId)))
        {
            throw new ArgumentException(
                "Every observed own element must occupy a known location.",
                nameof(ownElements));
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
            .OrderBy(location => location.LocationId, StringComparer.Ordinal)
            .ToArray());
        Edges = Array.AsReadOnly(edgeCopy
            .OrderBy(edge => edge.FirstLocationId, StringComparer.Ordinal)
            .ThenBy(edge => edge.SecondLocationId, StringComparer.Ordinal)
            .ToArray());
        OwnElements = Array.AsReadOnly(ownElementCopy
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .ToArray());
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

    public bool Equals(CampaignObservation? other) =>
        ReferenceEquals(this, other)
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
            && OwnElements.SequenceEqual(other.OwnElements));

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
        return hash.ToHashCode();
    }

    private static void EnsureUnique(
        IEnumerable<string> values,
        string message,
        string parameterName)
    {
        var copy = values.ToArray();

        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(message, parameterName);
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
