using Cna.Core.Content;

namespace Cna.Core.Campaigns;

public static class CampaignWorldFactory
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

        var world = new CampaignWorldSnapshot(
            CampaignWorldSnapshot.CurrentContractVersion,
            scenario.InitialPlacements
                .Select(placement => new CampaignElementState(
                    placement.ElementId,
                    placement.LocationId))
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
}

public static class CampaignWorldValidator
{
    public static bool IsValidInitial(
        CampaignWorldSnapshot? world,
        ContentPackArtifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);

        if (world is null
            || world.ContractVersion != CampaignWorldSnapshot.CurrentContractVersion
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
                || !expected.TryGetValue(elementState.ElementId, out var expectedLocation)
                || !string.Equals(
                    elementState.CurrentLocationId,
                    expectedLocation,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
