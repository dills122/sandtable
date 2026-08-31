using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignComponentToeState
{
    public CampaignComponentToeState(
        string componentId,
        int currentToe,
        ContentOrigin initialToeOrigin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentToe);
        ArgumentNullException.ThrowIfNull(initialToeOrigin);
        ComponentId = ContentContractGuards.RequireStableId(componentId, nameof(componentId));
        CurrentToe = currentToe;
        InitialToeOrigin = initialToeOrigin;
    }

    public string ComponentId { get; }

    public int CurrentToe { get; }

    public ContentOrigin InitialToeOrigin { get; }
}

internal sealed record CampaignElementOperationalStateV5
{
    public CampaignElementOperationalStateV5(
        int ledgerGameTurn,
        int ledgerOperationStage,
        CapabilityPointAmount capabilityPointsExpended,
        int cohesionLevel,
        CampaignVehicleBreakdownState? vehicleBreakdownState,
        CampaignMovementEndedState? movementEnded)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ledgerGameTurn, 1);
        if (ledgerOperationStage is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(ledgerOperationStage));
        }

        ArgumentNullException.ThrowIfNull(capabilityPointsExpended);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cohesionLevel, 10);
        if (movementEnded is not null
            && (movementEnded.GameTurn != ledgerGameTurn
                || movementEnded.OperationStage != ledgerOperationStage))
        {
            throw new ArgumentException(
                "Movement-ended state must belong to the operational ledger turn and stage.",
                nameof(movementEnded));
        }

        LedgerGameTurn = ledgerGameTurn;
        LedgerOperationStage = ledgerOperationStage;
        CapabilityPointsExpended = capabilityPointsExpended;
        CohesionLevel = cohesionLevel;
        VehicleBreakdownState = vehicleBreakdownState;
        MovementEnded = movementEnded;
    }

    public int LedgerGameTurn { get; }

    public int LedgerOperationStage { get; }

    public CapabilityPointAmount CapabilityPointsExpended { get; }

    public int CohesionLevel { get; }

    public CampaignVehicleBreakdownState? VehicleBreakdownState { get; }

    public CampaignMovementEndedState? MovementEnded { get; }
}

internal sealed record CampaignElementStateV5
{
    public CampaignElementStateV5(
        string elementId,
        string currentLocationId,
        CampaignElementReserveStatus reserveStatus,
        CampaignElementOperationalStateV5 operationalState,
        IEnumerable<CampaignComponentToeState> components)
    {
        if (!Enum.IsDefined(reserveStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(reserveStatus));
        }

        ArgumentNullException.ThrowIfNull(operationalState);
        var componentCopy = ContentContractGuards.CopyValues(components, nameof(components));
        if (componentCopy.Select(component => component.ComponentId)
                .Distinct(StringComparer.Ordinal).Count() != componentCopy.Length)
        {
            throw new ArgumentException("Campaign component IDs must be unique.", nameof(components));
        }

        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
        ReserveStatus = reserveStatus;
        OperationalState = operationalState;
        Components = Array.AsReadOnly(componentCopy
            .OrderBy(component => component.ComponentId, StringComparer.Ordinal)
            .ToArray());
    }

    public string ElementId { get; }

    public string CurrentLocationId { get; }

    public CampaignElementReserveStatus ReserveStatus { get; }

    public CampaignElementOperationalStateV5 OperationalState { get; }

    public IReadOnlyList<CampaignComponentToeState> Components { get; }

    public bool Equals(CampaignElementStateV5? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(ElementId, other.ElementId, StringComparison.Ordinal)
            && string.Equals(CurrentLocationId, other.CurrentLocationId, StringComparison.Ordinal)
            && ReserveStatus == other.ReserveStatus
            && OperationalState == other.OperationalState
            && Components.SequenceEqual(other.Components));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ElementId, StringComparer.Ordinal);
        hash.Add(CurrentLocationId, StringComparer.Ordinal);
        hash.Add(ReserveStatus);
        hash.Add(OperationalState);
        foreach (var component in Components)
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }
}

internal sealed record CampaignWorldSnapshotV5
{
    public const int CurrentContractVersion = 5;

    public CampaignWorldSnapshotV5(
        int contractVersion,
        IEnumerable<CampaignElementStateV5> elements,
        IEnumerable<CampaignMapRepresentationState> representations)
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

        ContractVersion = contractVersion;
        Elements = Array.AsReadOnly(elementCopy
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .ToArray());
        Representations = Array.AsReadOnly(representationCopy
            .OrderBy(value => value.RepresentationId, StringComparer.Ordinal)
            .ToArray());
    }

    public int ContractVersion { get; }

    public IReadOnlyList<CampaignElementStateV5> Elements { get; }

    public IReadOnlyList<CampaignMapRepresentationState> Representations { get; }

    public bool Equals(CampaignWorldSnapshotV5? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && Elements.SequenceEqual(other.Elements)
            && Representations.SequenceEqual(other.Representations));

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

        return hash.ToHashCode();
    }
}

internal static class CampaignWorldV5Factory
{
    public static CampaignWorldSnapshotV5 CreateInitial(
        ContentPackV5Artifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        CampaignWorldV5Validator.RequireValidContent(artifact);

        if (!CampaignWorldV5Validator.ContainsScenario(artifact, scenario))
        {
            throw new ArgumentException(
                "The scenario must be selected from the supplied Content Pack v5 artifact.",
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
        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
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
            legacyWorld.Representations);

        if (!CampaignWorldV5Validator.IsValidInitial(world, artifact, scenario))
        {
            throw new InvalidOperationException(
                "A validated Content Pack v5 produced an invalid initial campaign World v5.");
        }

        return world;
    }
}

internal static class CampaignWorldV5Validator
{
    public static bool IsValidInitial(
        CampaignWorldSnapshotV5? world,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => IsValidCore(world, artifact, scenario, requireInitial: true);

    public static bool IsValid(
        CampaignWorldSnapshotV5? world,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => IsValidCore(world, artifact, scenario, requireInitial: false);

    internal static void RequireValidContent(ContentPackV5Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        if (!ContentPackV5Validator.Validate(artifact.Definition).IsValid
            || !Cna1979ContentV5CompatibilityValidator.Validate(artifact.Definition).IsValid)
        {
            throw new ArgumentException(
                "The Content Pack v5 artifact is not valid for the CNA 1979 ruleset.",
                nameof(artifact));
        }
    }

    internal static bool ContainsScenario(
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => artifact.Definition.LegacyDefinition.Scenarios.Any(
            candidate => string.Equals(
                    candidate.ScenarioId,
                    scenario.ScenarioId,
                    StringComparison.Ordinal)
                && candidate == scenario);

    private static bool IsValidCore(
        CampaignWorldSnapshotV5? world,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        bool requireInitial)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        if (world is null
            || world.ContractVersion != CampaignWorldSnapshotV5.CurrentContractVersion
            || !ContentPackV5Validator.Validate(artifact.Definition).IsValid
            || !Cna1979ContentV5CompatibilityValidator.Validate(artifact.Definition).IsValid
            || !ContainsScenario(artifact, scenario))
        {
            return false;
        }

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
                || (requireInitial
                    && (!expectedInitialStates!.TryGetValue(state.ElementId, out var expectedState)
                        || state.OperationalState.VehicleBreakdownState
                            != expectedState.OperationalState.VehicleBreakdownState))
                || (requireInitial && (!string.Equals(
                        state.CurrentLocationId,
                        placement.LocationId,
                        StringComparison.Ordinal)
                    || state.ReserveStatus != CampaignElementReserveStatus.None
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

internal static class CampaignWorldV5CombatDerivation
{
    public static ZocRawDefensiveCloseAssaultResult CalculateRawDefensiveCloseAssaultPoints(
        CampaignWorldSnapshotV5 world,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        IEnumerable<string> representationIds)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(representationIds);
        CampaignWorldV5Validator.RequireValidContent(artifact);
        if (!CampaignWorldV5Validator.IsValid(world, artifact, scenario))
        {
            throw new InvalidOperationException(
                "Current raw defensive capability requires a valid World v5 and matching Content v5 artifact.");
        }

        var requested = representationIds
            .Select(value => ContentContractGuards.RequireStableId(
                value,
                nameof(representationIds)))
            .ToArray();
        if (requested.Length == 0
            || requested.Distinct(StringComparer.Ordinal).Count() != requested.Length)
        {
            throw new ArgumentException(
                "At least one unique authoritative representation is required.",
                nameof(representationIds));
        }

        var representations = world.Representations.ToDictionary(
            value => value.RepresentationId,
            StringComparer.Ordinal);
        if (requested.Any(value => !representations.ContainsKey(value)))
        {
            throw new InvalidOperationException(
                "Current raw defensive capability cannot use an unknown representation.");
        }

        var selected = requested.Select(value => representations[value]).ToArray();
        if (selected.Select(value => value.CurrentLocationId)
                .Distinct(StringComparer.Ordinal).Count() != 1)
        {
            throw new InvalidOperationException(
                "A represented force must occupy one authoritative location.");
        }

        var states = world.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var facts = artifact.Definition.ElementCombatFacts.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var contentElements = artifact.Definition.LegacyDefinition.Elements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var representedElementIds = selected
            .SelectMany(representation => representation.BoundElementIds)
            .ToArray();
        if (representedElementIds.Select(elementId => contentElements[elementId].SideId)
                .Distinct(StringComparer.Ordinal).Count() != 1)
        {
            throw new InvalidOperationException(
                "A represented force cannot combine opposing sides.");
        }

        return Cna1979Combat.CalculateRawDefensiveCloseAssaultPoints(
            representedElementIds.SelectMany(elementId =>
                {
                    var state = states[elementId];
                    var contentById = facts[elementId].Components.ToDictionary(
                        value => value.ComponentId,
                        StringComparer.Ordinal);
                    return state.Components.Select(component =>
                    {
                        var content = contentById[component.ComponentId];
                        return new ZocDefensiveCloseAssaultComponentFact(
                            content.ComponentClassId,
                            component.CurrentToe,
                            content.DefensiveCloseAssaultRating);
                    });
                }));
    }
}
