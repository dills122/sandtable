using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Initial state for scheduled content. This is not yet an admitted campaign snapshot.
internal sealed record CampaignWorldSnapshotV8
{
    public const int CurrentContractVersion = 8;

    public CampaignWorldSnapshotV8(ContentScheduledPackIdentity contentIdentity, string scenarioId,
        CampaignWorldV8Initialization initialization, CampaignWorldSnapshotV6 world,
        IEnumerable<ContentScheduledArrival> pendingArrivals)
    {
        ArgumentNullException.ThrowIfNull(contentIdentity);
        ArgumentNullException.ThrowIfNull(initialization);
        ArgumentNullException.ThrowIfNull(world);
        var pending = ContentContractGuards.CopyValues(pendingArrivals, nameof(pendingArrivals));
        var pendingIds = pending.SelectMany(value => value.ElementIds).ToArray();
        if (pending.Select(value => value.GroupId).Distinct(StringComparer.Ordinal).Count() != pending.Length
            || pendingIds.Distinct(StringComparer.Ordinal).Count() != pendingIds.Length
            || pendingIds.Intersect(world.Elements.Select(value => value.ElementId), StringComparer.Ordinal).Any()
            || pendingIds.Intersect(world.Representations.SelectMany(value => value.BoundElementIds), StringComparer.Ordinal).Any())
            throw new ArgumentException("Pending arrivals must be unique and absent from the world.", nameof(pendingArrivals));
        ContentIdentity = contentIdentity;
        ContractVersion = CurrentContractVersion;
        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
        Initialization = initialization;
        World = world;
        PendingArrivals = Array.AsReadOnly(pending.OrderBy(value => value.GroupId, StringComparer.Ordinal).ToArray());
    }

    public ContentScheduledPackIdentity ContentIdentity { get; }
    public int ContractVersion { get; }
    public string ScenarioId { get; }
    public CampaignWorldV8Initialization Initialization { get; }
    public CampaignWorldSnapshotV6 World { get; }
    public IReadOnlyList<ContentScheduledArrival> PendingArrivals { get; }

    public bool Equals(CampaignWorldSnapshotV8? other) => ReferenceEquals(this, other)
        || (other is not null && ContentIdentity == other.ContentIdentity && ScenarioId == other.ScenarioId
            && Initialization == other.Initialization && World == other.World
            && PendingArrivals.SequenceEqual(other.PendingArrivals));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContentIdentity);
        hash.Add(ScenarioId, StringComparer.Ordinal);
        hash.Add(Initialization);
        hash.Add(World);
        foreach (var arrival in PendingArrivals) hash.Add(arrival);
        return hash.ToHashCode();
    }
}

internal sealed record CampaignWorldV8Initialization(
    CapabilityPointAmount CapabilityPointsExpended,
    int CohesionLevel,
    CampaignElementReserveStatus ReserveStatus);

internal static class CampaignWorldV8Factory
{
    public static CampaignWorldSnapshotV8 CreateInitial(
        ContentScheduledCombatPackArtifact artifact,
        string scenarioId,
        CampaignWorldV8Initialization initialization)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return CreateInitialCore(artifact.Definition, artifact.Identity, scenarioId, initialization, source: false);
    }

    public static CampaignWorldSnapshotV8 CreateInitial(
        ContentSourceScheduledCombatPackArtifact artifact,
        string scenarioId,
        CampaignWorldV8Initialization initialization)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        return CreateInitialCore(artifact.Definition, artifact.Identity, scenarioId, initialization, source: true);
    }

    private static CampaignWorldSnapshotV8 CreateInitialCore(ContentPackV5Definition definition,
        ContentScheduledPackIdentity identity, string scenarioId, CampaignWorldV8Initialization initialization, bool source)
    {
        ArgumentNullException.ThrowIfNull(initialization);
        ArgumentNullException.ThrowIfNull(initialization.CapabilityPointsExpended);
        if (!Enum.IsDefined(initialization.ReserveStatus) || initialization.CohesionLevel > 10)
            throw new ArgumentException("Initial operational values are invalid.", nameof(initialization));
        if (initialization.ReserveStatus != CampaignElementReserveStatus.None)
            throw new InvalidOperationException("Initial reserve designation requires a side-specific policy.");
        var validation = source ? ContentPackV5Validator.ValidateSourceScheduledStructure(definition, allowEmptyNoncombat: true)
            : ContentPackV5Validator.ValidateScheduledStructure(definition, allowEmptyNoncombat: true);
        if (!validation.IsValid)
            throw new InvalidContentPackException(validation.Issues);

        var scenario = definition.LegacyDefinition.Scenarios.SingleOrDefault(
            value => string.Equals(value.ScenarioId, scenarioId, StringComparison.Ordinal))
            ?? throw new ArgumentException("Unknown scenario.", nameof(scenarioId));
        var placements = ContentScenarioInitialPlacements.Expand(scenario)
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        var elements = definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (placements.Any(placement => elements[placement.ElementId].PlacementMode != ContentPlacementMode.Independent))
            throw new InvalidOperationException("Initial attachment-only elements require an attachment policy.");
        if (placements.Any(placement => initialization.CapabilityPointsExpended
            > new CapabilityPointAmount(elements[placement.ElementId].BaseCapabilityPointAllowance, 1)))
            throw new InvalidOperationException("Initial expenditure exceeds an element's capability allowance.");
        if (placements.Any(placement => elements[placement.ElementId].BreakdownVehicleCohort is not null))
            throw new InvalidOperationException("Initial vehicle breakdown state requires an explicit policy.");
        var legacyWorld = CampaignWorldFactory.ConstructInitial(definition.LegacyDefinition, scenario, placements,
            initialization.CapabilityPointsExpended, initialization.CohesionLevel, initialization.ReserveStatus);
        var world = CampaignWorldV6Factory.CreateFromLegacyWorld(legacyWorld, definition, scenarioId);
        var facts = definition.ElementCombatFacts.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var seeds = definition.InitialPlacementCombatFacts.Where(value => value.ScenarioId == scenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (world.Elements.Any(element => !CampaignWorldV6Validator.HasValidComponents(
                element.Components, facts[element.ElementId], seeds[element.ElementId].InitialComponentToes)))
            throw new InvalidOperationException("Initial component state exceeds its source limits.");
        return new CampaignWorldSnapshotV8(identity, scenario.ScenarioId, initialization,
            world,
            Array.AsReadOnly(scenario.ScheduledArrivals.ToArray()));
    }
}
