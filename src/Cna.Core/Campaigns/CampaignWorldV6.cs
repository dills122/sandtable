using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignWorldSnapshotV6
{
    public const int CurrentContractVersion = 6;

    public CampaignWorldSnapshotV6(
        int contractVersion,
        IEnumerable<CampaignElementStateV5> elements,
        IEnumerable<CampaignMapRepresentationState> representations,
        IEnumerable<CampaignBrokenVehicleLot> brokenVehicleLots)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        var elementCopy = ContentContractGuards.CopyValues(elements, nameof(elements));
        var representationCopy = ContentContractGuards.CopyValues(
            representations,
            nameof(representations));
        if (elementCopy.Select(element => element.ElementId)
                .Distinct(StringComparer.Ordinal).Count() != elementCopy.Length)
        {
            throw new ArgumentException("Campaign element IDs must be unique.", nameof(elements));
        }

        if (representationCopy.Select(value => value.RepresentationId)
                .Distinct(StringComparer.Ordinal).Count() != representationCopy.Length)
        {
            throw new ArgumentException(
                "Map representation IDs must be unique.",
                nameof(representations));
        }

        var lots = ContentContractGuards.CopyValues(brokenVehicleLots, nameof(brokenVehicleLots));
        if (lots.Select(lot => lot.LotId).Distinct(StringComparer.Ordinal).Count() != lots.Length
            || lots.Select(lot => lot.CheckId).Distinct(StringComparer.Ordinal).Count() != lots.Length)
            throw new ArgumentException("Lot and check IDs must be unique.", nameof(brokenVehicleLots));
        BrokenVehicleLots = Array.AsReadOnly(lots.OrderBy(lot => lot.LotId, StringComparer.Ordinal).ToArray());
        ContractVersion = contractVersion;
        Elements = Array.AsReadOnly(elementCopy
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .ToArray());
        Representations = Array.AsReadOnly(representationCopy
            .OrderBy(value => value.RepresentationId, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyList<CampaignBrokenVehicleLot> BrokenVehicleLots { get; }

    public int ContractVersion { get; }

    public IReadOnlyList<CampaignElementStateV5> Elements { get; }

    public IReadOnlyList<CampaignMapRepresentationState> Representations { get; }

    public bool Equals(CampaignWorldSnapshotV6? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && Elements.SequenceEqual(other.Elements)
            && Representations.SequenceEqual(other.Representations)
            && BrokenVehicleLots.SequenceEqual(other.BrokenVehicleLots));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        foreach (var element in Elements)
        {
            hash.Add(element);
        }

        foreach (var representation in Representations)
        {
            hash.Add(representation);
        }

        foreach (var lot in BrokenVehicleLots) hash.Add(lot);
        return hash.ToHashCode();
    }
}

internal static class CampaignWorldV6Factory
{
    public static CampaignWorldSnapshotV6 CreateInitial(
        ContentPackV6Artifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        CampaignWorldV6Validator.RequireValidContent(artifact);

        if (!CampaignWorldV6Validator.ContainsScenario(artifact, scenario))
        {
            throw new ArgumentException(
                "The scenario must be selected from the supplied Content Pack v6 artifact.",
                nameof(scenario));
        }

        var legacyArtifact = ContentPackArtifact.Create(artifact.Definition.LegacyDefinition);
        var legacyWorld = CampaignWorldFactory.CreateInitial(legacyArtifact, scenario);
        var seeds = artifact.Definition.InitialPlacementCombatFacts
            .Where(value => string.Equals(
                value.ScenarioId,
                scenario.ScenarioId,
                StringComparison.Ordinal))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var world = new CampaignWorldSnapshotV6(
            CampaignWorldSnapshotV6.CurrentContractVersion,
            legacyWorld.Elements.Select(element => new CampaignElementStateV5(
                element.ElementId,
                element.CurrentLocationId,
                element.ReserveStatus,
                new CampaignElementOperationalStateV5(
                    element.OperationalState.LedgerGameTurn,
                    element.OperationalState.LedgerOperationStage,
                    element.OperationalState.CapabilityPointsExpended,
                    element.OperationalState.CohesionLevel,
                    element.OperationalState.VehicleBreakdownState,
                    null),
                seeds[element.ElementId].InitialComponentToes.Select(seed =>
                    new CampaignComponentToeState(
                        seed.ComponentId,
                        seed.CurrentToe,
                        seed.Origin)))),
            legacyWorld.Representations, []);

        if (!CampaignWorldV6Validator.IsValidInitial(world, artifact, scenario))
        {
            throw new InvalidOperationException(
                "A validated Content Pack v6 produced an invalid initial campaign World v6.");
        }

        return world;
    }
}

internal static class CampaignWorldV6Validator
{
    public static bool IsValidInitial(
        CampaignWorldSnapshotV6? world,
        ContentPackV6Artifact artifact,
        ContentScenario scenario) => IsValidCore(world, artifact, scenario, requireInitial: true);

    public static bool IsValid(
        CampaignWorldSnapshotV6? world,
        ContentPackV6Artifact artifact,
        ContentScenario scenario) => IsValidCore(world, artifact, scenario, requireInitial: false);

    internal static void RequireValidContent(ContentPackV6Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        if (!ContentPackV6Validator.Validate(artifact.Definition).IsValid
)
        {
            throw new ArgumentException(
                "The Content Pack v6 artifact is not valid for the CNA 1979 ruleset.",
                nameof(artifact));
        }
    }

    internal static bool ContainsScenario(
        ContentPackV6Artifact artifact,
        ContentScenario scenario) => artifact.Definition.LegacyDefinition.Scenarios.Any(
            candidate => string.Equals(
                    candidate.ScenarioId,
                    scenario.ScenarioId,
                    StringComparison.Ordinal)
                && candidate == scenario);

    private static bool IsValidCore(
        CampaignWorldSnapshotV6? world,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        bool requireInitial)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        if (world is null
            || world.ContractVersion != CampaignWorldSnapshotV6.CurrentContractVersion
            || !ContentPackV6Validator.Validate(artifact.Definition).IsValid

            || !ContainsScenario(artifact, scenario))
        {
            return false;
        }

        if (requireInitial && world.BrokenVehicleLots.Count != 0) return false;
        var definition = artifact.Definition;
        var placements = scenario.InitialPlacements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var contentElements = definition.LegacyDefinition.Elements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var combatFacts = definition.ElementCombatFacts.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var seeds = definition.InitialPlacementCombatFacts
            .Where(value => string.Equals(value.ScenarioId, scenario.ScenarioId,
                StringComparison.Ordinal))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var locations = definition.LegacyDefinition.Locations
            .Select(value => value.LocationId).ToHashSet(StringComparer.Ordinal);
        var expectedInitialStates = requireInitial
            ? CampaignWorldFactory.CreateInitial(
                    ContentPackArtifact.Create(definition.LegacyDefinition),
                    scenario)
                .Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal)
            : null;
        if (world.Elements.Count != placements.Count
            || world.Representations.Count != placements.Count)
        {
            return false;
        }

        foreach (var state in world.Elements)
        {
            if (!placements.TryGetValue(state.ElementId, out var placement)
                || !contentElements.TryGetValue(state.ElementId, out var contentElement)
                || !combatFacts.TryGetValue(state.ElementId, out var facts)
                || !seeds.TryGetValue(state.ElementId, out var initial)
                || contentElement.PlacementMode != ContentPlacementMode.Independent
                || !locations.Contains(state.CurrentLocationId)
                || !HasValidComponents(state.Components, facts.Components, initial.InitialComponentToes)
                || state.OperationalState.CapabilityPointsExpended > new CapabilityPointAmount(contentElement.BaseCapabilityPointAllowance, 1)
                || state.OperationalState.LedgerGameTurn != scenario.Start.GameTurn
                || state.OperationalState.LedgerOperationStage != scenario.Start.OperationStage
                || (state.OperationalState.MovementEnded is not null
                    && state.OperationalState.MovementEnded.SequenceContractVersion != 4)
                || !HasConservedCohort(state, contentElement, world.BrokenVehicleLots)
                || (contentElement.BreakdownVehicleCohort is not null && state.ReserveStatus != CampaignElementReserveStatus.None)
                || (requireInitial
                    && (!expectedInitialStates!.TryGetValue(state.ElementId, out var expectedState)
                        || state.OperationalState.VehicleBreakdownState
                            != expectedState.OperationalState.VehicleBreakdownState))
                || (requireInitial && (!string.Equals(
                        state.CurrentLocationId,
                        placement.LocationId,
                        StringComparison.Ordinal)
                    || state.ReserveStatus != CampaignElementReserveStatus.None
                    || state.OperationalState.CapabilityPointsExpended > new CapabilityPointAmount(contentElement.BaseCapabilityPointAllowance, 1)
                || state.OperationalState.LedgerGameTurn != scenario.Start.GameTurn
                    || state.OperationalState.LedgerOperationStage != scenario.Start.OperationStage
                    || state.OperationalState.CapabilityPointsExpended != CapabilityPointAmount.Zero
                    || state.OperationalState.CohesionLevel != 0
                    || state.OperationalState.MovementEnded is not null
                    || !HasInitialToe(state.Components, initial.InitialComponentToes))))
            {
                return false;
            }
        }

        var selectedCohorts = world.Elements.Select(state => contentElements[state.ElementId])
            .Where(element => element.BreakdownVehicleCohort is not null)
            .ToDictionary(element => element.BreakdownVehicleCohort!.CohortId, StringComparer.Ordinal);
        if (world.BrokenVehicleLots.Any(lot => !selectedCohorts.TryGetValue(lot.CohortId, out var element)
                || CampaignSnapshotSerializer.FormatSide(lot.Owner) != element.SideId
                || lot.VehicleTypeId != element.BreakdownVehicleCohort!.VehicleTypeId
                || !locations.Contains(lot.LocationId))) return false;
        var combatGroups = world.Elements.Where(state =>
                combatFacts[state.ElementId].CombatClassificationId != Cna1979Combat.TruckConvoyClassificationId)
            .GroupBy(state => (contentElements[state.ElementId].SideId, state.CurrentLocationId));
        if (combatGroups.Any(group => group.Count() > 1)) return false;

        var stateById = world.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var expectedRepresentations = scenario.InitialPlacements.Select((placement, index) => new
        {
            Id = CampaignWorldFactory.CreateInitialRepresentationId(index + 1),
            placement.ElementId,
        }).ToDictionary(value => value.Id, StringComparer.Ordinal);
        return world.Representations.All(representation =>
            expectedRepresentations.TryGetValue(representation.RepresentationId, out var expected)
            && representation.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && representation.BoundElementIds.Count == 1
            && string.Equals(representation.BoundElementIds[0], expected.ElementId,
                StringComparison.Ordinal)
            && stateById.TryGetValue(expected.ElementId, out var state)
            && string.Equals(representation.CurrentLocationId, state.CurrentLocationId,
                StringComparison.Ordinal));
    }

    private static bool HasConservedCohort(CampaignElementStateV5 state, ContentCombatElement element,
        IReadOnlyList<CampaignBrokenVehicleLot> lots)
    {
        var cohort = element.BreakdownVehicleCohort;
        var ledger = state.OperationalState.VehicleBreakdownState;
        if (cohort is null) return ledger is null;
        if (ledger is null || ledger.CohortId != cohort.CohortId) return false;
        var broken = lots.Where(lot => lot.CohortId == cohort.CohortId).Sum(lot => (long)lot.PointCount);
        return broken == ledger.BrokenPointCount && broken + ledger.WorkingPointCount == cohort.WorkingPointCount;
    }

    private static bool HasValidComponents(
        IReadOnlyList<CampaignComponentToeState> states,
        IReadOnlyList<ContentCombatComponent> components,
        IReadOnlyList<ContentInitialComponentToe> seeds)
    {
        if (states.Count != components.Count || states.Count != seeds.Count)
        {
            return false;
        }

        var contentById = components.ToDictionary(value => value.ComponentId, StringComparer.Ordinal);
        var seedsById = seeds.ToDictionary(value => value.ComponentId, StringComparer.Ordinal);
        return states.All(state =>
            contentById.TryGetValue(state.ComponentId, out var component)
            && seedsById.TryGetValue(state.ComponentId, out var seed)
            && state.CurrentToe <= component.MaximumToe
            && state.InitialToeOrigin == seed.Origin);
    }

    private static bool HasInitialToe(
        IReadOnlyList<CampaignComponentToeState> states,
        IReadOnlyList<ContentInitialComponentToe> seeds)
    {
        var seedsById = seeds.ToDictionary(value => value.ComponentId, StringComparer.Ordinal);
        return states.All(state => state.CurrentToe == seedsById[state.ComponentId].CurrentToe);
    }
}
