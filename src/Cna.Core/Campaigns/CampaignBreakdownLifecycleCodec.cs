using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownLifecycleCodec
{
    public static byte[] Serialize(CampaignSuccessorEvent value)
    {
        Validate(value);
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject();
            switch (value)
            {
                case ElementMovementStopped e:
                    w.WriteNumber("contractVersion", 1);
                    w.WriteString("eventType", "element-movement-stopped");
                    w.WriteString("campaignId", e.CampaignId);
                    w.WriteNumber("stateVersion", e.StateVersion);
                    w.WriteNumber("priorStateVersion", e.PriorStateVersion);
                    w.WriteString("rulesetHash", e.RulesetHash);
                    w.WriteString("fromPositionId", e.FromPositionId);
                    w.WriteString("actionId", e.ActionId);
                    w.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(e.ActingSide));
                    w.WriteString("submittedRouteId", e.SubmittedRouteId);
                    w.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(w, e.BreakdownFlowAfter);
                    break;
                case ReactionParticipantCompletedV2 e:
                    w.WriteNumber("contractVersion", 2);
                    w.WriteString("eventType", "reaction-participant-completed");
                    w.WriteString("campaignId", e.CampaignId);
                    w.WriteNumber("stateVersion", e.StateVersion);
                    w.WriteNumber("priorStateVersion", e.PriorStateVersion);
                    w.WriteString("fromPositionId", e.FromPositionId);
                    w.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(e.ActingSide));
                    w.WriteString("actionId", e.ActionId);
                    w.WriteString("submittedWindowId", e.SubmittedWindowId);
                    w.WriteString("submittedOpportunityId", e.SubmittedOpportunityId);
                    w.WriteString("windowId", e.WindowId.Value);
                    w.WriteString("opportunityId", e.OpportunityId.Value);
                    CampaignV11CanonicalCodec.WriteReactionWindow(w, "reactionWindowAfter", e.ReactionWindowAfter);
                    w.WriteString("rulesetHash", e.RulesetHash);
                    w.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(w, e.BreakdownFlowAfter);
                    break;
                case ReactionWindowClosedV2 e:
                    w.WriteNumber("contractVersion", 2);
                    w.WriteString("eventType", "reaction-window-closed");
                    w.WriteString("campaignId", e.CampaignId);
                    w.WriteNumber("stateVersion", e.StateVersion);
                    w.WriteNumber("priorStateVersion", e.PriorStateVersion);
                    w.WriteString("fromPositionId", e.FromPositionId);
                    if (e.ActingSide is null) w.WriteNull("actingSide"); else w.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(e.ActingSide.Value));
                    w.WriteString("actionId", e.ActionId);
                    w.WriteString("submittedWindowId", e.SubmittedWindowId);
                    w.WriteString("windowId", e.WindowId.Value);
                    w.WriteString("reason", FormatReason(e.Reason));
                    w.WriteStartArray("closedOpportunityIds"); foreach (var id in e.ClosedOpportunityIds) w.WriteStringValue(id.Value); w.WriteEndArray();
                    CampaignV11CanonicalCodec.WritePosition(w, "suspendedSequencePosition", e.SuspendedSequencePosition);
                    w.WriteString("rulesetHash", e.RulesetHash);
                    w.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(w, e.BreakdownFlowAfter);
                    break;
                case MovementSegmentCompletedV2 e:
                    w.WriteNumber("contractVersion", 2);
                    w.WriteString("eventType", "movement-segment-completed");
                    w.WriteString("campaignId", e.CampaignId);
                    w.WriteNumber("stateVersion", e.StateVersion);
                    w.WriteNumber("priorStateVersion", e.PriorStateVersion);
                    w.WriteString("fromPositionId", e.FromPositionId);
                    w.WriteNumber("gameTurn", e.GameTurn);
                    w.WriteNumber("operationStage", e.OperationStage);
                    w.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(e.ActingSide));
                    CampaignV11CanonicalCodec.WritePosition(w, "sequencePosition", e.SequencePosition);
                    w.WriteString("rulesetHash", e.RulesetHash);
                    w.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(w, e.BreakdownFlowAfter);
                    break;
                case BreakdownSegmentCompleted e:
                    w.WriteNumber("contractVersion", 1);
                    w.WriteString("eventType", "breakdown-segment-completed");
                    w.WriteString("campaignId", e.CampaignId);
                    w.WriteNumber("stateVersion", e.StateVersion);
                    w.WriteNumber("priorStateVersion", e.PriorStateVersion);
                    w.WriteString("rulesetHash", e.RulesetHash);
                    w.WriteString("fromPositionId", e.FromPositionId);
                    w.WriteString("actionId", e.ActionId);
                    CampaignV11CanonicalCodec.WritePosition(w, "sequencePosition", e.SequencePosition);
                    w.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(w, e.BreakdownFlowAfter);
                    w.WriteStartArray("sources"); foreach (var source in e.Sources) { w.WriteStartObject(); w.WriteString("sourceId", source.SourceId); w.WriteString("locator", source.Locator); w.WriteEndObject(); }
                    w.WriteEndArray();
                    break;
                default: throw new JsonException("Unsupported Breakdown lifecycle event.");
            }
            w.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignSuccessorEvent Deserialize(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray());
            var r = document.RootElement;
            CampaignSuccessorEvent result = (r.GetProperty("eventType").GetString(), r.GetProperty("contractVersion").GetInt32()) switch
            {
                ("element-movement-stopped", 1) => ParseElementMovementStopped(r),
                ("reaction-participant-completed", 2) => ParseReactionParticipantCompletedV2(r),
                ("reaction-window-closed", 2) => ParseReactionWindowClosedV2(r),
                ("movement-segment-completed", 2) => ParseMovementSegmentCompletedV2(r),
                ("breakdown-segment-completed", 1) => ParseBreakdownSegmentCompleted(r),
                _ => throw new JsonException("Unsupported event type/version pair."),
            };
            if (!bytes.SequenceEqual(Serialize(result))) throw new JsonException("Noncanonical lifecycle event.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or ArithmeticException or KeyNotFoundException)
        { throw new JsonException("Invalid Breakdown lifecycle event.", exception); }
    }
    private static ElementMovementStopped ParseElementMovementStopped(JsonElement r)
    {
        CampaignSnapshotSerializer.RequireProperties(r, "contractVersion", "eventType", "campaignId", "stateVersion", "priorStateVersion", "rulesetHash", "fromPositionId", "actionId", "actingSide", "submittedRouteId", "breakdownFlowAfter");
        return new(r.GetProperty("campaignId").GetString()!,
            r.GetProperty("stateVersion").GetInt64(),
            r.GetProperty("priorStateVersion").GetInt64(),
            r.GetProperty("rulesetHash").GetString()!,
            r.GetProperty("fromPositionId").GetString()!,
            r.GetProperty("actionId").GetString()!,
            CampaignSnapshotSerializer.ParseSide(r.GetProperty("actingSide").GetString()),
            r.GetProperty("submittedRouteId").GetString()!,
            CampaignBreakdownCodec.ParseFlow(r.GetProperty("breakdownFlowAfter")));
    }
    private static ReactionParticipantCompletedV2 ParseReactionParticipantCompletedV2(JsonElement r)
    {
        CampaignSnapshotSerializer.RequireProperties(r, "contractVersion", "eventType", "campaignId", "stateVersion", "priorStateVersion", "fromPositionId", "actingSide", "actionId", "submittedWindowId", "submittedOpportunityId", "windowId", "opportunityId", "reactionWindowAfter", "rulesetHash", "breakdownFlowAfter");
        return new(r.GetProperty("campaignId").GetString()!,
            r.GetProperty("stateVersion").GetInt64(),
            r.GetProperty("priorStateVersion").GetInt64(),
            r.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParseSide(r.GetProperty("actingSide").GetString()),
            r.GetProperty("actionId").GetString()!,
            r.GetProperty("submittedWindowId").GetString()!,
            r.GetProperty("submittedOpportunityId").GetString()!,
            new CampaignReactionWindowId(r.GetProperty("windowId").GetString()!),
            new CampaignReactionOpportunityId(r.GetProperty("opportunityId").GetString()!),
            CampaignV11CanonicalCodec.ParseReactionWindow(r.GetProperty("reactionWindowAfter")) ?? throw new JsonException("Window required."),
            r.GetProperty("rulesetHash").GetString()!,
            CampaignBreakdownCodec.ParseFlow(r.GetProperty("breakdownFlowAfter")));
    }
    private static ReactionWindowClosedV2 ParseReactionWindowClosedV2(JsonElement r)
    {
        CampaignSnapshotSerializer.RequireProperties(r, "contractVersion", "eventType", "campaignId", "stateVersion", "priorStateVersion", "fromPositionId", "actingSide", "actionId", "submittedWindowId", "windowId", "reason", "closedOpportunityIds", "suspendedSequencePosition", "rulesetHash", "breakdownFlowAfter");
        return new(r.GetProperty("campaignId").GetString()!,
            r.GetProperty("stateVersion").GetInt64(),
            r.GetProperty("priorStateVersion").GetInt64(),
            r.GetProperty("fromPositionId").GetString()!,
            r.GetProperty("actingSide").ValueKind == JsonValueKind.Null ? null : CampaignSnapshotSerializer.ParseSide(r.GetProperty("actingSide").GetString()),
            r.GetProperty("actionId").GetString()!,
            r.GetProperty("submittedWindowId").GetString()!,
            new CampaignReactionWindowId(r.GetProperty("windowId").GetString()!),
            ParseReason(r.GetProperty("reason").GetString()),
            r.GetProperty("closedOpportunityIds").EnumerateArray().Select(x => new CampaignReactionOpportunityId(x.GetString()!)),
            CampaignV11CanonicalCodec.ParsePosition(r.GetProperty("suspendedSequencePosition")),
            r.GetProperty("rulesetHash").GetString()!,
            CampaignBreakdownCodec.ParseFlow(r.GetProperty("breakdownFlowAfter")));
    }
    private static MovementSegmentCompletedV2 ParseMovementSegmentCompletedV2(JsonElement r)
    {
        CampaignSnapshotSerializer.RequireProperties(r, "contractVersion", "eventType", "campaignId", "stateVersion", "priorStateVersion", "fromPositionId", "gameTurn", "operationStage", "actingSide", "sequencePosition", "rulesetHash", "breakdownFlowAfter");
        return new(r.GetProperty("campaignId").GetString()!,
            r.GetProperty("stateVersion").GetInt64(),
            r.GetProperty("priorStateVersion").GetInt64(),
            r.GetProperty("fromPositionId").GetString()!,
            r.GetProperty("gameTurn").GetInt32(),
            r.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(r.GetProperty("actingSide").GetString()),
            CampaignV11CanonicalCodec.ParsePosition(r.GetProperty("sequencePosition")),
            r.GetProperty("rulesetHash").GetString()!,
            CampaignBreakdownCodec.ParseFlow(r.GetProperty("breakdownFlowAfter")));
    }
    private static BreakdownSegmentCompleted ParseBreakdownSegmentCompleted(JsonElement r)
    {
        CampaignSnapshotSerializer.RequireProperties(r, "contractVersion", "eventType", "campaignId", "stateVersion", "priorStateVersion", "rulesetHash", "fromPositionId", "actionId", "sequencePosition", "breakdownFlowAfter", "sources");
        return new(r.GetProperty("campaignId").GetString()!,
            r.GetProperty("stateVersion").GetInt64(),
            r.GetProperty("priorStateVersion").GetInt64(),
            r.GetProperty("rulesetHash").GetString()!,
            r.GetProperty("fromPositionId").GetString()!,
            r.GetProperty("actionId").GetString()!,
            CampaignV11CanonicalCodec.ParsePosition(r.GetProperty("sequencePosition")),
            CampaignBreakdownCodec.ParseFlow(r.GetProperty("breakdownFlowAfter")),
            CampaignSnapshotSerializer.ParseSources(r.GetProperty("sources")));
    }
    private static string FormatReason(CampaignReactionWindowCloseReason value) => value switch
    {
        CampaignReactionWindowCloseReason.PlayerDecline => "player-decline",
        CampaignReactionWindowCloseReason.ScriptedUnavailable => "scripted-unavailable",
        CampaignReactionWindowCloseReason.Timeout => "timeout",
        CampaignReactionWindowCloseReason.NoEligibleReactor => "no-eligible-reactor",
        _ => throw new JsonException("Invalid close reason."),
    };
    private static CampaignReactionWindowCloseReason ParseReason(string? value) => value switch
    {
        "player-decline" => CampaignReactionWindowCloseReason.PlayerDecline,
        "scripted-unavailable" => CampaignReactionWindowCloseReason.ScriptedUnavailable,
        "timeout" => CampaignReactionWindowCloseReason.Timeout,
        "no-eligible-reactor" => CampaignReactionWindowCloseReason.NoEligibleReactor,
        _ => throw new JsonException("Invalid close reason."),
    };

    internal static void Validate(CampaignSuccessorEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var (prior, rules, from, flow) = value switch
        {
            ElementMovementStopped e => (e.PriorStateVersion, e.RulesetHash, e.FromPositionId, e.BreakdownFlowAfter),
            ReactionParticipantCompletedV2 e => (e.PriorStateVersion, e.RulesetHash, e.FromPositionId, e.BreakdownFlowAfter),
            ReactionWindowClosedV2 e => (e.PriorStateVersion, e.RulesetHash, e.FromPositionId, e.BreakdownFlowAfter),
            MovementSegmentCompletedV2 e => (e.PriorStateVersion, e.RulesetHash, e.FromPositionId, e.BreakdownFlowAfter),
            BreakdownSegmentCompleted e => (e.PriorStateVersion, e.RulesetHash, e.FromPositionId, e.BreakdownFlowAfter),
            _ => throw new JsonException("Unsupported lifecycle event."),
        };
        ContentContractGuards.RequireStableId(from, nameof(value));
        if (prior < 1 || checked(prior + 1) != value.StateVersion || !Cna1979BreakdownRuleset.IsCanonicalHash(rules))
            throw new JsonException("Lifecycle identity or version is invalid.");
        _ = CampaignBreakdownCodec.Serialize(flow);
        switch (value)
        {
            case ElementMovementStopped e:
                Hash(e.ActionId); Hash(e.SubmittedRouteId);
                if (!Enum.IsDefined(e.ActingSide) || flow is not CampaignBreakdownFlow.PhasingStop deliberate
                    || deliberate.Stop.Reason != CampaignBreakdownStopReason.Deliberate
                    || deliberate.Stop.Route.Owner != e.ActingSide || deliberate.Stop.RecordedStateVersion != e.StateVersion)
                    throw new JsonException("Invalid deliberate stop.");
                deliberate.Stop.ValidateIdentity(e.CampaignId, rules);
                break;
            case ReactionParticipantCompletedV2 e:
                Hash(e.ActionId); Hash(e.SubmittedWindowId); Hash(e.SubmittedOpportunityId);
                Cna1979LandSequenceV4.RequireMaterializedMovement(e.ReactionWindowAfter.ReactingPosition.SuspendedMovementPosition);
                if (flow is not CampaignBreakdownFlow.ReactorStopOpen open || e.ReactionWindowAfter.ActiveOpportunityId is not null
                    || !e.ReactionWindowAfter.ResolvedOpportunityIds.Contains(e.OpportunityId)
                    || !e.ReactionWindowAfter.FrozenOpportunities.Any(x => x.OpportunityId == e.OpportunityId)
                    || e.ReactionWindowAfter.WindowId != e.WindowId || e.ReactionWindowAfter.ReactingSide != e.ActingSide
                    || from != e.ReactionWindowAfter.ReactingPosition.SuspendedMovementPosition.PositionId
                    || open.Stop.Route.Owner != e.ActingSide || open.Stop.Reason != CampaignBreakdownStopReason.ReactionCompleted
                    || open.Stop.RecordedStateVersion != e.StateVersion)
                    throw new JsonException("Invalid participant completion.");
                open.Stop.ValidateIdentity(e.CampaignId, rules);
                break;
            case ReactionWindowClosedV2 e:
                Hash(e.ActionId); Hash(e.SubmittedWindowId);
                Cna1979LandSequenceV4.RequireMaterializedMovement(e.SuspendedSequencePosition);
                if (!Enum.IsDefined(e.Reason) || (e.ActingSide is not null && !Enum.IsDefined(e.ActingSide.Value))
                    || (e.Reason == CampaignReactionWindowCloseReason.PlayerDecline) != (e.ActingSide is not null)
                    || from != e.SuspendedSequencePosition.PositionId
                    || flow is not (CampaignBreakdownFlow.Moving or CampaignBreakdownFlow.PhasingStop or CampaignBreakdownFlow.ReactorStopClosed))
                    throw new JsonException("Invalid window close.");
                if (flow is CampaignBreakdownFlow.ReactorStopClosed closed)
                {
                    var expectedReason = e.Reason switch
                    {
                        CampaignReactionWindowCloseReason.Timeout => CampaignBreakdownStopReason.ReactionTimeout,
                        CampaignReactionWindowCloseReason.ScriptedUnavailable => CampaignBreakdownStopReason.ReactionUnavailable,
                        _ => throw new JsonException("Active close must be a System fallback."),
                    };
                    if (closed.Stop.Reason != expectedReason || closed.Stop.RecordedStateVersion != e.StateVersion)
                        throw new JsonException("Invalid closed stop.");
                    closed.Stop.ValidateIdentity(e.CampaignId, rules);
                }
                break;
            case MovementSegmentCompletedV2 e:
                if (!Enum.IsDefined(e.ActingSide) || e.GameTurn != e.SequencePosition.GameTurn
                    || e.OperationStage != 1 || e.SequencePosition.OperationStage != 1
                    || e.SequencePosition.ActorRole != LandActorRole.FirstActingSide
                    || e.SequencePosition.SegmentId != LandSegmentIds.BreakdownDetermination
                    || flow is not CampaignBreakdownFlow.Idle || !Cna1979LandSequenceV4.IsSupportedCheckpoint(e.SequencePosition))
                    throw new JsonException("Invalid Movement completion.");
                RequirePredecessor(from, e.SequencePosition, LandSegmentIds.Movement);
                break;
            case BreakdownSegmentCompleted e:
                Hash(e.ActionId);
                if (e.SequencePosition.OperationStage != 1 || e.SequencePosition.ActorRole != LandActorRole.FirstActingSide
                    || e.SequencePosition.StepId != LandStepIds.PositionDetermination || flow is not CampaignBreakdownFlow.Idle
                    || !Cna1979LandSequenceV4.IsSupportedCheckpoint(e.SequencePosition)
                    || e.ActionId != CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId())
                    throw new JsonException("Invalid Breakdown completion.");
                var predecessor = RequirePredecessor(from, e.SequencePosition, LandSegmentIds.BreakdownDetermination);
                if (!predecessor.Sources.SequenceEqual(e.Sources)) throw new JsonException("Invalid completion sources.");
                break;
        }
    }
    private static LandSequencePosition RequirePredecessor(string from, LandSequencePosition next, string segment)
    {
        var position = Cna1979LandSequenceV4.CreateTurn(next.GameTurn).SingleOrDefault(x => x.PositionId == from);
        if (position is null || position.SegmentId != segment || Cna1979LandSequenceV4.GetNext(position) != next)
            throw new JsonException("Completion must advance exactly one canonical segment.");
        return position;
    }
    private static void Hash(string value) => ContentContractGuards.RequireSha256(value, nameof(value));
}
