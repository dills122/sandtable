using System.Globalization;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignWorldFactory
{
    public static CampaignWorldSnapshot CreateInitial(
        ContentPackArtifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);

        if (!ContainsScenario(artifact, scenario))
        {
            throw new ArgumentException(
                "The scenario must be selected from the supplied Content Pack artifact.",
                nameof(scenario));
        }

        var placements = scenario.InitialPlacements.ToArray();
        var world = new CampaignWorldSnapshot(
            CampaignWorldSnapshot.CurrentContractVersion,
            placements
                .Select(placement => new CampaignElementState(
                    placement.ElementId,
                    placement.LocationId,
                    CampaignElementReserveStatus.None,
                    new CampaignElementOperationalState(
                        scenario.Start.GameTurn,
                        scenario.Start.OperationStage,
                        CapabilityPointAmount.Zero,
                        0)))
                .ToArray(),
            placements
                .Select((placement, index) => new CampaignMapRepresentationState(
                    CreateInitialRepresentationId(index + 1),
                    placement.LocationId,
                    CampaignMapRepresentationBindingKind.IndependentElement,
                    [placement.ElementId]))
                .ToArray());

        if (!CampaignWorldValidator.IsValidInitial(world, artifact, scenario))
        {
            throw new InvalidOperationException(
                "A validated Content Pack produced an invalid initial campaign world.");
        }

        return world;
    }

    internal static bool ContainsScenario(
        ContentPackArtifact artifact,
        ContentScenario scenario) => artifact.Definition.Scenarios.Any(
            candidate => string.Equals(
                    candidate.ScenarioId,
                    scenario.ScenarioId,
                    StringComparison.Ordinal)
                && candidate == scenario);

    internal static string CreateInitialRepresentationId(int ordinal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, 1);
        return $"map-representation.{ordinal.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}

internal static class CampaignWorldValidator
{
    public static bool IsValidInitial(
        CampaignWorldSnapshot? world,
        ContentPackArtifact artifact,
        ContentScenario scenario) => IsValid(
            world,
            artifact,
            scenario,
            static (_, status) => status == CampaignElementReserveStatus.None);

    public static bool IsValidReserveDesignation(
        CampaignWorldSnapshot? world,
        ContentPackArtifact artifact,
        ContentScenario scenario,
        LandSide firstSide)
    {
        var firstSideId = firstSide switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => null,
        };

        return firstSideId is not null && IsValid(
            world,
            artifact,
            scenario,
            (element, status) => status == CampaignElementReserveStatus.None
                || (status == CampaignElementReserveStatus.ReserveI
                    && string.Equals(
                        element.SideId,
                        firstSideId,
                        StringComparison.Ordinal)));
    }

    private static bool IsValid(
        CampaignWorldSnapshot? world,
        ContentPackArtifact artifact,
        ContentScenario scenario,
        Func<ContentCombatElement, CampaignElementReserveStatus, bool> isValidStatus)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(isValidStatus);

        if (world is null
            || !IsLocallyValid(world, scenario.Start.GameTurn, scenario.Start.OperationStage)
            || !CampaignWorldFactory.ContainsScenario(artifact, scenario))
        {
            return false;
        }

        var elements = artifact.Definition.Elements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);
        var locations = artifact.Definition.Locations
            .Select(location => location.LocationId)
            .ToHashSet(StringComparer.Ordinal);
        var expected = scenario.InitialPlacements.ToDictionary(
            placement => placement.ElementId,
            placement => placement.LocationId,
            StringComparer.Ordinal);

        if (world.Elements.Count != expected.Count)
        {
            return false;
        }

        foreach (var elementState in world.Elements)
        {
            if (!elements.TryGetValue(elementState.ElementId, out var element)
                || element.PlacementMode != ContentPlacementMode.Independent
                || !locations.Contains(elementState.CurrentLocationId)
                || !isValidStatus(element, elementState.ReserveStatus)
                || elementState.OperationalState.LedgerGameTurn != scenario.Start.GameTurn
                || elementState.OperationalState.LedgerOperationStage
                    != scenario.Start.OperationStage
                || elementState.OperationalState.CapabilityPointsExpended
                    != CapabilityPointAmount.Zero
                || elementState.OperationalState.CohesionLevel != 0
                || !expected.TryGetValue(elementState.ElementId, out var expectedLocation)
                || !string.Equals(
                    elementState.CurrentLocationId,
                    expectedLocation,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        var expectedRepresentations = scenario.InitialPlacements
            .Select((placement, index) => new
            {
                RepresentationId = CampaignWorldFactory.CreateInitialRepresentationId(index + 1),
                placement.ElementId,
                placement.LocationId,
            })
            .ToDictionary(value => value.RepresentationId, StringComparer.Ordinal);
        if (world.Representations.Count != expectedRepresentations.Count)
        {
            return false;
        }

        foreach (var representation in world.Representations)
        {
            if (!expectedRepresentations.TryGetValue(
                    representation.RepresentationId,
                    out var expectedRepresentation)
                || representation.BindingKind
                    != CampaignMapRepresentationBindingKind.IndependentElement
                || representation.BoundElementIds.Count != 1
                || !string.Equals(
                    representation.BoundElementIds[0],
                    expectedRepresentation.ElementId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    representation.CurrentLocationId,
                    expectedRepresentation.LocationId,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsLocallyValid(
        CampaignWorldSnapshot? world,
        int ledgerGameTurn,
        int ledgerOperationStage)
    {
        if (world is null
            || world.ContractVersion != CampaignWorldSnapshot.CurrentContractVersion
            || ledgerGameTurn < 1
            || ledgerOperationStage is < 1 or > 3
            || world.Elements.Count != world.Representations.Count)
        {
            return false;
        }

        for (var index = 0; index < world.Elements.Count; index++)
        {
            var element = world.Elements[index];
            var operational = element.OperationalState;
            var representation = world.Representations[index];
            if (operational is null
                || operational.LedgerGameTurn != ledgerGameTurn
                || operational.LedgerOperationStage != ledgerOperationStage
                || operational.CapabilityPointsExpended != CapabilityPointAmount.Zero
                || operational.CohesionLevel != 0
                || representation.BindingKind
                    != CampaignMapRepresentationBindingKind.IndependentElement
                || !string.Equals(
                    representation.RepresentationId,
                    CampaignWorldFactory.CreateInitialRepresentationId(index + 1),
                    StringComparison.Ordinal)
                || representation.BoundElementIds.Count != 1
                || !string.Equals(
                    representation.BoundElementIds[0],
                    element.ElementId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    representation.CurrentLocationId,
                    element.CurrentLocationId,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
