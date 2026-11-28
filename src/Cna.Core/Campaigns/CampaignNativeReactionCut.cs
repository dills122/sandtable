using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// The caller supplies the current turn and weather derived from accepted history. No legacy
// snapshot or initial setup cursor participates in this cut.
internal sealed record CampaignNativeReactionCut(string CampaignId, long Version, string PriorEventHash,
    string WorldHash, CampaignWorldSnapshotV6 World, CampaignReactionWindow Window,
    ContentPackV6Artifact Artifact, ContentScenario Scenario, CampaignOperationStageWeather Weather)
{
    internal static CampaignNativeReactionCut Bind(object predecessor, CampaignWorldSnapshotV6 world,
        CampaignContentContext context, int gameTurn, string rulesetHash, CampaignOperationStageWeather weather,
        CampaignReactionWindowId windowId)
    {
        var (campaignId, version, expectedWorldHash, window) = predecessor switch
        {
            CampaignNativeSecondMoveV44 { ReactionWindow: { } opened } trigger =>
                (trigger.CampaignId, trigger.StateVersion, trigger.ResultWorldHash, opened),
            CampaignNativeReactionMoveV59 moved =>
                (moved.CampaignId, moved.StateVersion, moved.ResultWorldHash, moved.WindowAfter),
            CampaignNativeReactionClosedV60 { WindowAfter: { } remaining } closed =>
                (closed.CampaignId, closed.StateVersion, closed.ResultWorldHash, remaining),
            _ => throw new InvalidOperationException("Native Reaction requires an open window predecessor."),
        };
        var position = window.ReactingPosition.SuspendedMovementPosition;
        var actualWorldHash = CampaignNativeCombatPaidWorldV22.Hash(world);
        if (window.WindowId != windowId || position.GameTurn != gameTurn || weather.GameTurn != gameTurn
            || weather.OperationStage != position.OperationStage || actualWorldHash != expectedWorldHash
            || world.Elements.Any(x => x.OperationalState.LedgerGameTurn != position.GameTurn
                || x.OperationalState.LedgerOperationStage != position.OperationStage)
            || context.ArtifactV6 is not { } artifact || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new InvalidOperationException("Native Reaction does not bind the current window, ledger scope and World.");
        window.ValidateIdentities(campaignId, rulesetHash);
        if (predecessor is CampaignNativeSecondMoveV44 move)
        {
            var input = new ElementMovedV3ReplayInput(campaignId, move.PriorStateVersion, position.PositionId,
                move.Side, move.ElementId, move.OriginLocationId, move.DestinationLocationId);
            var representation = world.Representations.Single(x => x.RepresentationId == move.RepresentationId);
            var expected = CampaignElementMovedV3Factory.CreateWindowForMovement(campaignId, rulesetHash,
                move.PriorStateVersion, position, artifact, context.Scenario, input, representation, world,
                window.ReactingSide, CampaignNativeSecondMoveV44.CurrentContractVersion);
            if (window != expected)
                throw new InvalidOperationException("Native Reaction trigger does not bind the frozen opportunities.");
        }
        return new(campaignId, version, TurnBoundaryCanonical.Hash(CampaignCurrentEventSerializer.Serialize(predecessor)),
            actualWorldHash, world, window, artifact, context.Scenario, weather);
    }

    internal ObservedReactionMoveOption[] MoveOptions(CampaignFrozenReactionOpportunity opportunity)
    {
        if (Window.ResolvedOpportunityIds.Contains(opportunity.OpportunityId)
            || (Window.ActiveOpportunityId is { } active && active != opportunity.OpportunityId)
            || opportunity.ReactingRepresentation.BoundElementIds.Count != 1)
            return [];
        var id = opportunity.ReactingRepresentation.BoundElementIds.Single();
        var element = World.Elements.Single(x => x.ElementId == id);
        var position = Window.ReactingPosition.SuspendedMovementPosition;
        if (element.OperationalState.LedgerGameTurn != position.GameTurn
            || element.OperationalState.LedgerOperationStage != position.OperationStage)
            return [];
        return CampaignReactingElementMovedV2Factory.MoveOptions(World, Artifact, Scenario, Window, opportunity,
            Window.ActiveOpportunityId is null ? null : (id, opportunity.ReactingRepresentation.RepresentationId), Accounting);
    }

    internal IReadOnlyList<CampaignBreakdownStep> Accounting(ContentCombatElement content, CampaignElementStateV5 element,
        ContentHexEdge edge, ContentHex destination) => CampaignReactingElementMovedV2Factory.CalculateAccounting(
            content, element, edge, destination, () => ApplicableWeather(destination.LocationId));

    // Match the legacy weather mapping without changing the frozen V11 validator.
    private BreakdownWeatherKind ApplicableWeather(string locationId)
    {
        if (Weather.Kind == WeatherKind.Normal) return BreakdownWeatherKind.Normal;
        if (Weather.Kind == WeatherKind.Hot) return BreakdownWeatherKind.Hot;
        var assigned = Artifact.Definition.LegacyDefinition.WeatherAreaAssignments.Single(x => x.LocationId == locationId).WeatherArea;
        var area = assigned switch
        {
            ContentWeatherArea.A => WeatherArea.A,
            ContentWeatherArea.B => WeatherArea.B,
            ContentWeatherArea.C => WeatherArea.C,
            ContentWeatherArea.D => WeatherArea.D,
            ContentWeatherArea.E => WeatherArea.E,
            _ => throw new ArgumentException("Unknown weather area."),
        };
        if (!Weather.AffectedAreas.Contains(area)) return BreakdownWeatherKind.Normal;
        return Weather.Kind switch
        {
            WeatherKind.Sandstorm => BreakdownWeatherKind.Sandstorm,
            WeatherKind.Rainstorm => BreakdownWeatherKind.Rainstorm,
            _ => throw new ArgumentException("Unknown stop weather."),
        };
    }
}
