using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSnapshotV11Validator
{
    internal static IReadOnlyList<RuleReference> LotSources { get; } = Array.AsReadOnly<RuleReference>(
        [new("spi-1979-land-rules", "21.41")]);

    private static IReadOnlyList<RuleReference> ReactionAdjacencySources { get; } = Array.AsReadOnly<RuleReference>(
        [new("spi-1979-land-rules", "8.51")]);

    public static void RequireShape(CampaignSnapshotV11 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var position = snapshot.CurrentPosition;
        var sequence = position.SequenceContext;
        var world = snapshot.World;
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(snapshot.RulesetHash)
            || snapshot.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || snapshot.RandomState.AlgorithmId != SandtableRandom.AlgorithmId
            || sequence.GameTurn != snapshot.Setup.InitialGameTurn
            || snapshot.Setup.StageEntry.OperationStage != 1
            || snapshot.OperationStageOrders.Any(value => value.GameTurn != sequence.GameTurn || value.OperationStage != 1)
            || snapshot.OperationStageWeather.Any(value => value.GameTurn != sequence.GameTurn || value.OperationStage != 1))
            Invalid("Mixed identity or stage context.");
        CampaignSequenceV6Guards.RequireCurrentPosition(sequence, snapshot.OperationStageOrders);
        if (sequence.SegmentId is LandSegmentIds.Movement or LandSegmentIds.BreakdownDetermination or LandSegmentIds.Combat
            && snapshot.OperationStageWeather.Count != 1)
            Invalid("Movement and later checkpoints require retained stage weather.");
        foreach (var lot in world.BrokenVehicleLots)
        {
            lot.ValidateIdentity(snapshot.CampaignId, snapshot.RulesetHash);
            if (lot.CreatedStateVersion > snapshot.StateVersion || !lot.Sources.SequenceEqual(LotSources))
                Invalid("Lot version or sources do not match authority.");
        }
        foreach (var element in world.Elements)
        {
            var ended = element.OperationalState.MovementEnded;
            if (ended is null) continue;
            var source = Cna1979LandSequenceV4.CreateTurn(sequence.GameTurn)
                .First(value => value.SegmentId == LandSegmentIds.Movement);
            var order = snapshot.OperationStageOrders.SingleOrDefault(value => value.GameTurn == sequence.GameTurn && value.OperationStage == 1);
            if (order is null || ended.SequenceContractVersion != 4 || ended.GameTurn != sequence.GameTurn
                || ended.OperationStage != 1 || ended.PositionId != source.PositionId || ended.StageId != source.StageId
                || ended.PhaseId != source.PhaseId || ended.SegmentId != source.SegmentId || ended.PhasingSide != order.FirstSide)
                Invalid("Movement-ended marker does not bind the suspended first-side Movement.");
        }
        if (snapshot.StateVersion == 1 && (position.Kind != CampaignPositionV11Kind.Sequence
                || sequence != Cna1979LandSequenceV4.CreateTurn(snapshot.Setup.InitialGameTurn)[0]
                || snapshot.InitiativeHolder is not null || snapshot.OperationStageOrders.Count != 0
                || snapshot.OperationStageWeather.Count != 0 || snapshot.RandomState.NextByteCursor != 0))
            Invalid("Invalid initial snapshot state.");

        switch (snapshot.BreakdownFlow)
        {
            case CampaignBreakdownFlow.Idle:
                if (position.Kind != CampaignPositionV11Kind.Sequence || snapshot.ReactionWindow is not null)
                    Invalid("Idle requires a sequence position without a Reaction window.");
                break;
            case CampaignBreakdownFlow.Moving moving:
                RequireMovement(snapshot);
                if (position.Kind != CampaignPositionV11Kind.Sequence || snapshot.ReactionWindow is not null)
                    Invalid("Moving requires ordinary Movement authority.");
                RequireRoute(snapshot, moving.Route, sequence.ActiveSide!.Value);
                if (world.Elements.Single(value => value.ElementId == moving.Route.ElementId).OperationalState.MovementEnded is not null)
                    Invalid("Movement-ended phasing route requires its recorded stop.");
                break;
            case CampaignBreakdownFlow.Reacting reacting:
                RequireMovement(snapshot);
                var activeWindow = RequireWindow(snapshot);
                if (position.Kind != CampaignPositionV11Kind.Reaction || position.ReactingPosition != activeWindow.ReactingPosition)
                    Invalid("Reacting requires its exact Reaction position.");
                RequireContinuation(snapshot, reacting.PhasingContinuation, activeWindow.PhasingSide);
                RequireTriggerRoute(reacting.PhasingContinuation, activeWindow);
                if ((activeWindow.ActiveOpportunityId is null) != (reacting.ReactorRoute is null))
                    Invalid("Active opportunity and reactor route must agree.");
                if (reacting.ReactorRoute is not null)
                {
                    RequireRoute(snapshot, reacting.ReactorRoute, activeWindow.ReactingSide);
                    var active = activeWindow.FrozenOpportunities.Single(value => value.OpportunityId == activeWindow.ActiveOpportunityId);
                    RequireEpisodeRoute(reacting.ReactorRoute, active, activeWindow.TriggerCommittedStateVersion);
                }
                break;
            case CampaignBreakdownFlow.ReactorStopOpen open:
                RequireMovement(snapshot);
                var openWindow = RequireWindow(snapshot);
                RequireStopPosition(snapshot);
                RequireContinuation(snapshot, open.PhasingContinuation, openWindow.PhasingSide);
                RequireTriggerRoute(open.PhasingContinuation, openWindow);
                RequireStop(snapshot, open.Stop, openWindow.ReactingSide);
                if (open.Stop.Reason != CampaignBreakdownStopReason.ReactionCompleted || openWindow.ActiveOpportunityId is not null)
                    Invalid("Open reactor stop requires participant completion and no active slot.");
                var completed = openWindow.FrozenOpportunities.SingleOrDefault(value =>
                    value.ReactingRepresentation.RepresentationId == open.Stop.Route.RepresentationId);
                if (completed is null || !openWindow.ResolvedOpportunityIds.Contains(completed.OpportunityId))
                    Invalid("Stopped reactor must already be resolved in the retained window.");
                RequireEpisodeRoute(open.Stop.Route, completed!, openWindow.TriggerCommittedStateVersion);
                break;
            case CampaignBreakdownFlow.ReactorStopClosed closed:
                RequireMovement(snapshot);
                RequireStopPosition(snapshot);
                if (snapshot.ReactionWindow is not null) Invalid("Closed reactor stop cannot retain a window.");
                RequireContinuation(snapshot, closed.PhasingContinuation, sequence.ActiveSide!.Value);
                RequireStop(snapshot, closed.Stop, Opposite(sequence.ActiveSide.Value));
                if (closed.Stop.Reason is not (CampaignBreakdownStopReason.ReactionUnavailable or CampaignBreakdownStopReason.ReactionTimeout))
                    Invalid("Closed reactor stop requires active System closure.");
                break;
            case CampaignBreakdownFlow.PhasingStop phasing:
                RequireMovement(snapshot);
                RequireStopPosition(snapshot);
                if (snapshot.ReactionWindow is not null) Invalid("Phasing stop cannot retain a window.");
                RequireStop(snapshot, phasing.Stop, sequence.ActiveSide!.Value);
                RequirePhasingReason(phasing.Stop);
                break;
            default: Invalid("Unknown Breakdown flow."); break;
        }
    }

    public static bool IsValid(CampaignSnapshotV11? snapshot, ContentPackV6Artifact artifact, ContentScenario scenario) =>
        GetDiagnostic(snapshot, artifact, scenario) is null;

    public static string? GetDiagnostic(CampaignSnapshotV11? snapshot, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(artifact); ArgumentNullException.ThrowIfNull(scenario);
        if (snapshot is null || snapshot.Setup.Content.Pack != artifact.Identity
            || snapshot.Setup.Content.ScenarioId != scenario.ScenarioId || snapshot.Setup.InitialGameTurn != scenario.Start.GameTurn
            || snapshot.Setup.StageEntry.OperationStage != scenario.Start.OperationStage
            || snapshot.Setup.CapabilityProfileId != artifact.Definition.CapabilityProfileId)
            return BreakdownCapabilityDiagnostics.Identity;
        if (!CampaignWorldV6Validator.IsValid(snapshot.World, artifact, scenario)
            || (snapshot.StateVersion == 1 && !CampaignWorldV6Validator.IsValidInitial(snapshot.World, artifact, scenario)))
            return BreakdownCapabilityDiagnostics.Conservation;
        try
        {
            RequireShape(snapshot);
            var elements = artifact.Definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
            var locations = artifact.Definition.LegacyDefinition.Locations.Select(value => value.LocationId).ToHashSet(StringComparer.Ordinal);
            foreach (var route in Routes(snapshot.BreakdownFlow))
            {
                var content = elements[route.ElementId];
                var cohort = content.BreakdownVehicleCohort;
                if (CampaignSnapshotSerializer.FormatSide(route.Owner) != content.SideId || !locations.Contains(route.OriginLocationId)
                    || !route.CohortIds.SequenceEqual(cohort is null ? [] : new[] { cohort.CohortId }))
                    return BreakdownCapabilityDiagnostics.Flow;
            }
            var activePhasing = ActivePhasingRoute(snapshot.BreakdownFlow);
            if (activePhasing is not null)
            {
                var state = snapshot.World.Elements.Single(value => value.ElementId == activePhasing.ElementId);
                if (state.OperationalState.CapabilityPointsExpended >= new CapabilityPointAmount(elements[activePhasing.ElementId].BaseCapabilityPointAllowance, 1)
                    || state.OperationalState.VehicleBreakdownState?.WorkingPointCount == 0)
                    return BreakdownCapabilityDiagnostics.Flow;
            }
            foreach (var stop in Stops(snapshot.BreakdownFlow))
            {
                var state = snapshot.World.Elements.Single(value => value.ElementId == stop.Route.ElementId);
                var ended = state.OperationalState.MovementEnded is not null;
                var exhausted = state.OperationalState.CapabilityPointsExpended >= new CapabilityPointAmount(elements[stop.Route.ElementId].BaseCapabilityPointAllowance, 1);
                if ((stop.Reason == CampaignBreakdownStopReason.MovementEnded && !ended)
                    || (stop.Reason == CampaignBreakdownStopReason.CpExhausted && (!exhausted || ended))
                    || (stop.Reason == CampaignBreakdownStopReason.Deliberate && (ended || exhausted)))
                    return BreakdownCapabilityDiagnostics.Flow;
                if (stop.WeatherKind != ApplicableWeather(snapshot, artifact, stop.Route.CurrentLocationId))
                    return BreakdownCapabilityDiagnostics.Flow;
                foreach (var input in stop.CohortInputs)
                {
                    var cohort = elements[stop.Route.ElementId].BreakdownVehicleCohort!;
                    if (input.VehicleTypeId != cohort.VehicleTypeId || input.ProfileId != cohort.ProfileId)
                        return BreakdownCapabilityDiagnostics.Flow;
                }
            }
            var window = snapshot.ReactionWindow;
            if (window is not null)
            {
                var facts = artifact.Definition.ElementCombatFacts.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
                if (!HasSide(window.TriggerAuthority.TriggeringRepresentation, window.PhasingSide, true)
                    || Cna1979Combat.FindClassification(facts[window.TriggerAuthority.ElementId].CombatClassificationId)?.Kind
                        is not (ZocCombatClassificationKind.CombatUnit or ZocCombatClassificationKind.Headquarters)
                    || window.FrozenOpportunities.Any(value => !HasSide(value.ReactingRepresentation, window.ReactingSide, false)
                        || CampaignElementMovedV2Factory.FindEdge(artifact.Definition.LegacyDefinition,
                            value.AdjacencyEvidence.TriggerLocationId, value.AdjacencyEvidence.CommittedDestinationLocationId) is null
                        || !value.AdjacencyEvidence.Sources.SequenceEqual(ReactionAdjacencySources)
                        || value.ReactingRepresentation.BoundElementIds.Any(id =>
                            facts[id].CombatClassificationId != Cna1979Combat.CombatUnitClassificationId)))
                    return BreakdownCapabilityDiagnostics.Flow;
            }
            return null;

            bool HasSide(CampaignMapRepresentationState representation, LandSide side, bool locationMatch)
            {
                var current = snapshot.World.Representations.SingleOrDefault(value => value.RepresentationId == representation.RepresentationId);
                return current is not null && current.BindingKind == representation.BindingKind
                    && current.BoundElementIds.SequenceEqual(representation.BoundElementIds)
                    && (!locationMatch || current.CurrentLocationId == representation.CurrentLocationId)
                    && representation.BoundElementIds.All(id => elements[id].SideId == CampaignSnapshotSerializer.FormatSide(side));
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or ArithmeticException)
        {
            return BreakdownCapabilityDiagnostics.Flow;
        }
    }

    private static CampaignBreakdownRoute? ActivePhasingRoute(CampaignBreakdownFlow flow)
    {
        if (flow is CampaignBreakdownFlow.Moving moving) return moving.Route;
        var continuation = flow switch
        {
            CampaignBreakdownFlow.Reacting reacting => reacting.PhasingContinuation,
            CampaignBreakdownFlow.ReactorStopOpen open => open.PhasingContinuation,
            CampaignBreakdownFlow.ReactorStopClosed closed => closed.PhasingContinuation,
            _ => null,
        };
        return continuation is CampaignPhasingContinuation.ResumeRoute resume ? resume.Route : null;
    }

    internal static BreakdownWeatherKind ApplicableWeather(CampaignSnapshotV11 snapshot, ContentPackV6Artifact artifact, string locationId)
    {
        var weather = snapshot.OperationStageWeather.Single();
        if (weather.Kind == WeatherKind.Normal) return BreakdownWeatherKind.Normal;
        if (weather.Kind == WeatherKind.Hot) return BreakdownWeatherKind.Hot;
        var assigned = artifact.Definition.LegacyDefinition.WeatherAreaAssignments.Single(value => value.LocationId == locationId).WeatherArea;
        var area = assigned switch
        {
            ContentWeatherArea.A => WeatherArea.A,
            ContentWeatherArea.B => WeatherArea.B,
            ContentWeatherArea.C => WeatherArea.C,
            ContentWeatherArea.D => WeatherArea.D,
            ContentWeatherArea.E => WeatherArea.E,
            _ => throw new ArgumentException("Unknown weather area."),
        };
        if (!weather.AffectedAreas.Contains(area)) return BreakdownWeatherKind.Normal;
        return weather.Kind switch
        {
            WeatherKind.Sandstorm => BreakdownWeatherKind.Sandstorm,
            WeatherKind.Rainstorm => BreakdownWeatherKind.Rainstorm,
            _ => throw new ArgumentException("Unknown stop weather."),
        };
    }

    private static void RequireMovement(CampaignSnapshotV11 snapshot) => Cna1979LandSequenceV4.RequireMaterializedMovement(snapshot.CurrentPosition.SequenceContext);
    private static void RequireStopPosition(CampaignSnapshotV11 snapshot)
    {
        if (snapshot.CurrentPosition.Kind != CampaignPositionV11Kind.BreakdownStop) Invalid("Recorded stop requires System stop position.");
    }
    private static CampaignReactionWindow RequireWindow(CampaignSnapshotV11 snapshot)
    {
        var window = snapshot.ReactionWindow ?? throw new ArgumentException("Missing Reaction window.");
        window.ValidateIdentities(snapshot.CampaignId, snapshot.RulesetHash);
        if (window.ReactingPosition.SuspendedMovementPosition != snapshot.CurrentPosition.SequenceContext
            || window.TriggerCommittedStateVersion > snapshot.StateVersion) Invalid("Reaction context does not match snapshot.");
        return window;
    }
    private static void RequireContinuation(CampaignSnapshotV11 snapshot, CampaignPhasingContinuation continuation, LandSide owner)
    {
        switch (continuation)
        {
            case CampaignPhasingContinuation.ResumeRoute resume:
                RequireRoute(snapshot, resume.Route, owner);
                if (snapshot.World.Elements.Single(value => value.ElementId == resume.Route.ElementId).OperationalState.MovementEnded is not null)
                    Invalid("Movement-ended phasing route cannot resume after Reaction.");
                break;
            case CampaignPhasingContinuation.ResolveStop resolve:
                RequireStop(snapshot, resolve.Stop, owner);
                if (resolve.Stop.Reason is not (CampaignBreakdownStopReason.MovementEnded or CampaignBreakdownStopReason.CpExhausted))
                    Invalid("Only a forced phasing stop can be deferred by its triggered window.");
                break;
            default: Invalid("Invalid phasing continuation."); break;
        }
    }
    private static void RequireTriggerRoute(CampaignPhasingContinuation continuation, CampaignReactionWindow window)
    {
        var route = ContinuationRoute(continuation);
        if (route.FirstMoveStateVersion > window.TriggerCommittedStateVersion
            || (continuation is CampaignPhasingContinuation.ResolveStop resolve
                && resolve.Stop.RecordedStateVersion != window.TriggerCommittedStateVersion)
            || route.ElementId != window.TriggerAuthority.ElementId
            || route.RepresentationId != window.TriggerAuthority.TriggeringRepresentation.RepresentationId
            || route.CurrentLocationId != window.TriggerAuthority.DestinationLocationId)
            Invalid("Phasing continuation must retain the triggering route.");
    }
    private static void RequireEpisodeRoute(CampaignBreakdownRoute route, CampaignFrozenReactionOpportunity opportunity, long triggerStateVersion)
    {
        if (route.FirstMoveStateVersion <= triggerStateVersion
            || route.RepresentationId != opportunity.ReactingRepresentation.RepresentationId
            || !opportunity.ReactingRepresentation.BoundElementIds.Contains(route.ElementId)
            || route.OriginLocationId != opportunity.ReactingRepresentation.CurrentLocationId)
            Invalid("Reactor route must start from its frozen opportunity.");
    }
    private static void RequireRoute(CampaignSnapshotV11 snapshot, CampaignBreakdownRoute route, LandSide owner)
    {
        ArgumentNullException.ThrowIfNull(route);
        route.ValidateIdentity(snapshot.CampaignId, snapshot.RulesetHash);
        var element = snapshot.World.Elements.SingleOrDefault(value => value.ElementId == route.ElementId);
        var representation = snapshot.World.Representations.SingleOrDefault(value => value.RepresentationId == route.RepresentationId);
        if (route.FirstMoveStateVersion > snapshot.StateVersion || route.Owner != owner || element is null || representation is null
            || representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
            || representation.BoundElementIds.Count != 1 || representation.BoundElementIds[0] != route.ElementId
            || representation.CurrentLocationId != route.CurrentLocationId || element.CurrentLocationId != route.CurrentLocationId)
            Invalid("Route binding, owner, version or location is invalid.");
    }
    private static void RequireStop(CampaignSnapshotV11 snapshot, CampaignBreakdownStop stop, LandSide owner)
    {
        ArgumentNullException.ThrowIfNull(stop);
        RequireRoute(snapshot, stop.Route, owner);
        stop.ValidateIdentity(snapshot.CampaignId, snapshot.RulesetHash);
        if (stop.RecordedStateVersion > snapshot.StateVersion) Invalid("Stop cannot be recorded in the future.");
        var ledger = snapshot.World.Elements.Single(value => value.ElementId == stop.Route.ElementId).OperationalState.VehicleBreakdownState;
        if ((ledger is null && stop.CohortInputs.Count != 0) || (ledger is not null && (stop.CohortInputs.Count != 1
            || stop.CohortInputs[0].CohortId != ledger.CohortId || stop.CohortInputs[0].WorkingPointCount != ledger.WorkingPointCount
            || stop.CohortInputs[0].CumulativeBreakdownPoints != ledger.CumulativeBreakdownPoints
            || stop.CohortInputs[0].SandstormAttributedBreakdownPoints != ledger.SandstormAttributedBreakdownPoints
            || stop.CohortInputs[0].HighestEffectiveCheckedBandId != ledger.HighestEffectiveCheckedBandId)))
            Invalid("Recorded stop inputs must equal the current cohort ledger.");
    }
    private static void RequirePhasingReason(CampaignBreakdownStop stop)
    {
        if (stop.Reason is not (CampaignBreakdownStopReason.Deliberate or CampaignBreakdownStopReason.MovementEnded or CampaignBreakdownStopReason.CpExhausted))
            Invalid("Invalid phasing stop reason.");
    }
    private static LandSide Opposite(LandSide side) => side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
    private static void Invalid(string message) => throw new ArgumentException($"{BreakdownCapabilityDiagnostics.Flow}: {message}");
    private static CampaignBreakdownRoute ContinuationRoute(CampaignPhasingContinuation continuation) => continuation switch
    {
        CampaignPhasingContinuation.ResumeRoute resume => resume.Route,
        CampaignPhasingContinuation.ResolveStop resolve => resolve.Stop.Route,
        _ => throw new ArgumentException("Invalid continuation."),
    };
    internal static IEnumerable<CampaignBreakdownRoute> Routes(CampaignBreakdownFlow flow) => flow switch
    {
        CampaignBreakdownFlow.Idle => [],
        CampaignBreakdownFlow.Moving moving => [moving.Route],
        CampaignBreakdownFlow.Reacting reacting => reacting.ReactorRoute is null
            ? [ContinuationRoute(reacting.PhasingContinuation)] : [ContinuationRoute(reacting.PhasingContinuation), reacting.ReactorRoute],
        CampaignBreakdownFlow.ReactorStopOpen open => [ContinuationRoute(open.PhasingContinuation), open.Stop.Route],
        CampaignBreakdownFlow.ReactorStopClosed closed => [ContinuationRoute(closed.PhasingContinuation), closed.Stop.Route],
        CampaignBreakdownFlow.PhasingStop stop => [stop.Stop.Route],
        _ => throw new ArgumentException("Invalid flow."),
    };
    internal static IEnumerable<CampaignBreakdownStop> Stops(CampaignBreakdownFlow flow)
    {
        CampaignPhasingContinuation? continuation = flow switch
        {
            CampaignBreakdownFlow.Reacting reacting => reacting.PhasingContinuation,
            CampaignBreakdownFlow.ReactorStopOpen open => open.PhasingContinuation,
            CampaignBreakdownFlow.ReactorStopClosed closed => closed.PhasingContinuation,
            _ => null,
        };
        if (continuation is CampaignPhasingContinuation.ResolveStop resolve) yield return resolve.Stop;
        var stop = flow switch
        {
            CampaignBreakdownFlow.ReactorStopOpen open => open.Stop,
            CampaignBreakdownFlow.ReactorStopClosed closed => closed.Stop,
            CampaignBreakdownFlow.PhasingStop phasing => phasing.Stop,
            _ => null,
        };
        if (stop is not null) yield return stop;
    }
}
