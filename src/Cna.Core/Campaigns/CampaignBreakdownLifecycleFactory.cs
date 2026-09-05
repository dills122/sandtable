using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownLifecycleFactory
{
    public static ElementMovementStopped CreateStop(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        Require(prior, artifact, scenario);
        var route = RequireMoving(prior);
        return CreateStop(prior, artifact, scenario, route.Owner, CreateRouteCapability(prior), CreateStopActionId(prior));
    }

    public static ElementMovementStopped CreateStop(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact,
        ContentScenario scenario, LandSide actingSide, string submittedRouteId, string actionId)
    {
        Require(prior, artifact, scenario);
        var route = RequireMoving(prior);
        if (route.Owner != actingSide || submittedRouteId != CreateRouteCapability(prior) || actionId != CreateStopActionId(prior))
            throw Unsupported("Stop does not match the current owner route capability.");
        return new(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion, prior.RulesetHash,
            prior.CurrentPosition.SequenceContext.PositionId, actionId, actingSide, submittedRouteId,
            new CampaignBreakdownFlow.PhasingStop(RecordStop(prior, artifact, route, CampaignBreakdownStopReason.Deliberate)));
    }

    public static string CreateRouteCapability(CampaignSnapshotV11 prior)
    {
        var route = RequireMoving(prior);
        return Hash(writer =>
        {
            WriteIdentity(writer, prior, "sandtable.observation.movement-route.v1", CampaignSnapshotSerializer.FormatSide(route.Owner));
            writer.WriteString("elementId", route.ElementId);
            writer.WriteString("originLocationId", route.OriginLocationId);
            writer.WriteString("currentLocationId", route.CurrentLocationId);
        });
    }
    public static string CreateStopActionId(CampaignSnapshotV11 prior) => Action("stop-element-movement", "routeId", CreateRouteCapability(prior));
    public static string CreateSystemStopCapability(CampaignSnapshotV11 prior) => Hash(writer =>
        WriteIdentity(writer, prior, "sandtable.action.breakdown-stop.v1", "system"));
    public static string CreateResolveActionId(CampaignSnapshotV11 prior) => Action("resolve-breakdown-stop", "stopId", CreateSystemStopCapability(prior));
    public static string CreateBreakdownCompletionActionId() => Hash(writer =>
    {
        writer.WriteNumber("contractVersion", 1);
        writer.WriteString("kind", "complete-breakdown-segment");
        writer.WriteNumber("operationStage", 1);
    });

    public static ReactionParticipantCompletedReplayInput CreateReactionCompletionInput(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        RequireReaction(prior, artifact, scenario);
        var window = prior.ReactionWindow!;
        var flow = (CampaignBreakdownFlow.Reacting)prior.BreakdownFlow;
        if (window.ActiveOpportunityId is not { } active || flow.ReactorRoute is null)
            throw Unsupported("Only an active Reaction participant may complete.");
        var opportunity = window.FrozenOpportunities.Single(x => x.OpportunityId == active);
        var options = CampaignReactingElementMovedV2Factory.MoveOptions(prior, artifact, scenario, opportunity);
        var publicWindow = CampaignReactingElementMovedV2Factory.CreateWindowCapability(prior);
        var publicOpportunity = CampaignObservationV6DisclosureIdentity.CreateOpportunity(publicWindow, prior.StateVersion,
            CampaignObservationV6DisclosureIdentity.CreateCapabilityKey(options));
        var action = new CompleteReactionParticipantAction(publicWindow, publicOpportunity);
        return new(prior.CampaignId, prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId,
            window.ReactingSide, action.ActionId, publicWindow, publicOpportunity, window.WindowId, active);
    }

    public static ReactionParticipantCompletedV2 CreateReactionCompletion(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario, ReactionParticipantCompletedReplayInput input)
    {
        if (input != CreateReactionCompletionInput(prior, artifact, scenario))
            throw Unsupported("Completion is not the exact current participant capability.");
        var window = prior.ReactionWindow!;
        var flow = (CampaignBreakdownFlow.Reacting)prior.BreakdownFlow;
        var after = new CampaignReactionWindow(window.WindowId, window.TriggerCommittedStateVersion,
            window.PhasingSide, window.ReactingSide, window.ReactingPosition, window.TriggerAuthority,
            window.ApparentTrigger, window.FrozenOpportunities, window.ResolvedOpportunityIds.Append(input.OpportunityId), null);
        return new(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion, input.FromPositionId,
            input.ActingSide, input.ActionId, input.SubmittedWindowId, input.SubmittedOpportunityId, input.WindowId,
            input.OpportunityId, after, prior.RulesetHash, new CampaignBreakdownFlow.ReactorStopOpen(flow.PhasingContinuation,
                RecordStop(prior, artifact, flow.ReactorRoute!, CampaignBreakdownStopReason.ReactionCompleted)));
    }

    public static ReactionWindowClosedReplayInput CreateReactionCloseInput(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario, CampaignReactionWindowCloseReason reason)
    {
        RequireReaction(prior, artifact, scenario);
        var window = prior.ReactionWindow!;
        var active = window.ActiveOpportunityId is not null;
        var anyOptions = window.FrozenOpportunities.Where(x => !window.ResolvedOpportunityIds.Contains(x.OpportunityId))
            .Any(x => CampaignReactingElementMovedV2Factory.MoveOptions(prior, artifact, scenario, x).Length > 0);
        var noEligible = !active && !anyOptions;
        if (!Enum.IsDefined(reason)
            || (reason == CampaignReactionWindowCloseReason.NoEligibleReactor) != noEligible
            || (reason == CampaignReactionWindowCloseReason.PlayerDecline && active))
            throw Unsupported("The requested close reason is not a current legal capability.");
        var publicWindow = CampaignReactingElementMovedV2Factory.CreateWindowCapability(prior);
        ReactionWindowAction action = reason switch
        {
            CampaignReactionWindowCloseReason.PlayerDecline => new DeclineReactionWindowAction(publicWindow),
            CampaignReactionWindowCloseReason.Timeout => new CloseReactionWindowTimeoutAction(publicWindow),
            CampaignReactionWindowCloseReason.ScriptedUnavailable => new CloseReactionWindowUnavailableAction(publicWindow),
            CampaignReactionWindowCloseReason.NoEligibleReactor => new CloseReactionWindowNoEligibleAction(publicWindow),
            _ => throw Unsupported("Unsupported Reaction close reason."),
        };
        return new(prior.CampaignId, prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId,
            reason == CampaignReactionWindowCloseReason.PlayerDecline ? window.ReactingSide : null,
            action.ActionId, publicWindow, window.WindowId, reason);
    }

    public static ReactionWindowClosedV2 CreateReactionClose(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario, ReactionWindowClosedReplayInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input != CreateReactionCloseInput(prior, artifact, scenario, input.Reason))
            throw Unsupported("Close is not the exact current window capability.");
        var window = prior.ReactionWindow!;
        var flow = (CampaignBreakdownFlow.Reacting)prior.BreakdownFlow;
        CampaignBreakdownFlow next = flow.ReactorRoute is { } route
            ? new CampaignBreakdownFlow.ReactorStopClosed(flow.PhasingContinuation, RecordStop(prior, artifact, route,
                input.Reason == CampaignReactionWindowCloseReason.Timeout ? CampaignBreakdownStopReason.ReactionTimeout
                    : CampaignBreakdownStopReason.ReactionUnavailable))
            : ResumePhasing(flow.PhasingContinuation);
        return new(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion, input.FromPositionId,
            input.ActingSide, input.ActionId, input.SubmittedWindowId, input.WindowId, input.Reason,
            window.FrozenOpportunities.Select(x => x.OpportunityId).Except(window.ResolvedOpportunityIds),
            window.ReactingPosition.SuspendedMovementPosition, prior.RulesetHash, next);
    }

    public static MovementSegmentCompletedV2 CreateMovementCompletion(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        Require(prior, artifact, scenario);
        if (prior.BreakdownFlow is not CampaignBreakdownFlow.Idle || prior.ReactionWindow is not null
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence)
            throw Unsupported("Movement completion requires idle sequence authority.");
        var position = prior.CurrentPosition.SequenceContext;
        Cna1979LandSequenceV4.RequireMaterializedMovement(position);
        if (position.OperationStage != 1 || position.ActorRole != LandActorRole.FirstActingSide)
            throw Unsupported("Only first-side Movement completion is admitted.");
        var canonical = Cna1979LandSequenceV4.CreateTurn(position.GameTurn).Single(x => x.PositionId == position.PositionId);
        return new(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion, position.PositionId,
            position.GameTurn, position.OperationStage, position.ActiveSide!.Value, Cna1979LandSequenceV4.GetNext(canonical),
            prior.RulesetHash, new CampaignBreakdownFlow.Idle());
    }

    public static BreakdownSegmentCompleted CreateBreakdownCompletion(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario, string actionId)
    {
        Require(prior, artifact, scenario);
        var position = prior.CurrentPosition.SequenceContext;
        if (prior.BreakdownFlow is not CampaignBreakdownFlow.Idle || prior.ReactionWindow is not null
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence
            || position.SegmentId != LandSegmentIds.BreakdownDetermination || position.OperationStage != 1
            || position.ActorRole != LandActorRole.FirstActingSide || actionId != CreateBreakdownCompletionActionId())
            throw Unsupported("Breakdown completion requires the exact idle System capability.");
        return new(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion, prior.RulesetHash,
            position.PositionId, actionId, Cna1979LandSequenceV4.GetNext(position), new CampaignBreakdownFlow.Idle(), position.Sources);
    }

    internal static CampaignBreakdownFlow ResumePhasing(CampaignPhasingContinuation continuation) => continuation switch
    {
        CampaignPhasingContinuation.ResumeRoute resume => new CampaignBreakdownFlow.Moving(resume.Route),
        CampaignPhasingContinuation.ResolveStop resolve => new CampaignBreakdownFlow.PhasingStop(resolve.Stop),
        _ => throw Unsupported("Unknown phasing continuation."),
    };

    private static CampaignBreakdownStop RecordStop(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact,
        CampaignBreakdownRoute route, CampaignBreakdownStopReason reason)
    {
        var element = prior.World.Elements.Single(x => x.ElementId == route.ElementId);
        var content = artifact.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == route.ElementId);
        var ledger = element.OperationalState.VehicleBreakdownState;
        CampaignBreakdownCheckInput[] inputs = ledger is null ? [] : [new(ledger.CohortId,
            content.BreakdownVehicleCohort!.VehicleTypeId, content.BreakdownVehicleCohort.ProfileId,
            ledger.WorkingPointCount, ledger.CumulativeBreakdownPoints, ledger.SandstormAttributedBreakdownPoints,
            ledger.HighestEffectiveCheckedBandId)];
        return CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, checked(prior.StateVersion + 1), route,
            reason, CampaignSnapshotV11Validator.ApplicableWeather(prior, artifact, route.CurrentLocationId), inputs);
    }

    private static CampaignBreakdownRoute RequireMoving(CampaignSnapshotV11 prior) =>
        prior.CurrentPosition.Kind == CampaignPositionV11Kind.Sequence && prior.ReactionWindow is null
        && prior.BreakdownFlow is CampaignBreakdownFlow.Moving moving ? moving.Route
            : throw Unsupported("An open phasing route is required.");
    private static void Require(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        if (!CampaignSnapshotV11Validator.IsValid(prior, artifact, scenario))
            throw Unsupported("Lifecycle event requires certified Breakdown authority.");
    }
    private static void RequireReaction(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        Require(prior, artifact, scenario);
        if (prior.CurrentPosition.Kind != CampaignPositionV11Kind.Reaction || prior.ReactionWindow is null
            || prior.BreakdownFlow is not CampaignBreakdownFlow.Reacting)
            throw Unsupported("Lifecycle event requires an executable Reaction window.");
    }
    private static void WriteIdentity(Utf8JsonWriter writer, CampaignSnapshotV11 prior, string domain, string audience)
    {
        writer.WriteString("domain", domain); writer.WriteString("campaignId", prior.CampaignId);
        writer.WriteString("rulesetHash", prior.RulesetHash); writer.WriteNumber("stateVersion", prior.StateVersion);
        writer.WriteString("audience", audience);
    }
    private static string Action(string kind, string name, string value) => Hash(writer =>
    {
        writer.WriteNumber("contractVersion", 1); writer.WriteString("kind", kind); writer.WriteString(name, value);
    });
    private static string Hash(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) { writer.WriteStartObject(); write(writer); writer.WriteEndObject(); }
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }
    private static InvalidOperationException Unsupported(string message) => new(message);
}
