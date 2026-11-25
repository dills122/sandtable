using System.Collections.Frozen;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

/// <summary>
/// One Host owner-opening stage as a client sees it. <see cref="ElementKey"/> is the fixed
/// <c>actingElementId</c> for a singleton stage; it is null for per-element stages, whose ids come
/// from the audience's own elements. The opening stage and action stage differ for exactly one
/// kind today (Retreat Before Assault), which is why both are data here rather than a client rule.
/// </summary>
internal sealed record CampaignOpeningStageDescriptor(
    string OpeningStage, string ActionStage, CampaignOwnerOpeningKind Kind, string? ElementKey);

/// <summary>One entry of the read-only "open for my seat" answer.</summary>
internal sealed record CampaignOpenSeatEntry(
    string OpeningStage, string ActionStage, IReadOnlyList<string> ActingElementIds, bool RuleNotImplemented);

internal sealed record CampaignOpenSeatResult(
    string CampaignId, CampaignActionAudience Audience, IReadOnlyList<CampaignOpenSeatEntry> Open);

internal sealed partial class CampaignDurableAuthorityJournal
{
    /// <summary>
    /// Every owner-opening kind exactly once. A test asserts that this covers
    /// <see cref="CampaignOwnerOpeningKind"/> completely and that every stage a
    /// <see cref="CampaignOpenSeatCatalog"/> row names exists here, so a new kind must register
    /// in this one table to be discoverable.
    /// </summary>
    internal static readonly IReadOnlyList<CampaignOpeningStageDescriptor> OpeningStages =
    [
        // Gameplay chain.
        new("movement", "movement", CampaignOwnerOpeningKind.Movement, null),
        new("initiative-order", "initiative-order", CampaignOwnerOpeningKind.InitiativeOrder, InitiativeOrderOpeningKey),
        new("reserve-designation", "reserve-designation", CampaignOwnerOpeningKind.ReserveDesignation, ReserveDesignationOpeningKey),
        new("reaction", "reaction", CampaignOwnerOpeningKind.Reaction, ReactionOpeningKey),
        new("movement-segment-completion", "movement-segment-completion", CampaignOwnerOpeningKind.MovementSegmentCompletion, MovementSegmentCompletionOpeningKey),
        new("combat-selection", "combat-selection", CampaignOwnerOpeningKind.CombatSelection, null),
        new("retreat-before-assault", "rba-decline", CampaignOwnerOpeningKind.RetreatBeforeAssault, null),
        new("force-assignment", "force-assignment", CampaignOwnerOpeningKind.ForceAssignment, null),
        new("post-assault-retreat", "post-assault-retreat", CampaignOwnerOpeningKind.PostAssaultRetreat, null),
        new("custody", "custody", CampaignOwnerOpeningKind.Custody, null),
        new("reserve-release", "reserve-release", CampaignOwnerOpeningKind.ReserveRelease, null),
        new("cycle-control", "cycle-control", CampaignOwnerOpeningKind.CycleControl, CycleControlOpeningKey),
        new("truck-convoy-pass", "truck-convoy-pass", CampaignOwnerOpeningKind.TruckConvoyPass, TruckConvoyPassOpeningKey),
        new("post-combat-pass", "post-combat-pass", CampaignOwnerOpeningKind.PostCombatPass, PostCombatPassOpeningKey),
        new("second-reserve", "second-reserve", CampaignOwnerOpeningKind.SecondReserve, SecondReserveOpeningKey),
        new("second-movement", "second-movement", CampaignOwnerOpeningKind.SecondMovement, null),
        new("second-movement-completion", "second-movement-completion", CampaignOwnerOpeningKind.SecondMovementCompletion, SecondMovementCompletionOpeningKey),
        new("stage-entry-initiative-declaration", "stage-entry-initiative-declaration", CampaignOwnerOpeningKind.StageEntryInitiativeDeclaration, StageEntryInitiativeDeclarationOpeningKey),
        new("repeat-movement", "repeat-movement", CampaignOwnerOpeningKind.RepeatMovement, RepeatMovementOpeningKey),
        new("repeat-combat-selection", "repeat-combat-selection", CampaignOwnerOpeningKind.RepeatCombatSelection, null),
        // Pregame and source setup.
        new("pregame-axis-supply", "pregame-axis-supply", CampaignOwnerOpeningKind.PregameAxisSupply, PregameAxisSupplyOpeningKey),
        new("pregame-axis-supply-finish", "pregame-axis-supply-finish", CampaignOwnerOpeningKind.PregameAxisSupplyFinish, PregameAxisSupplyFinishOpeningKey),
        new("source-axis-dump", "source-axis-dump", CampaignOwnerOpeningKind.SourceAxisDump, SourceAxisDumpOpeningKey),
        new("source-commonwealth-supply", "source-commonwealth-supply", CampaignOwnerOpeningKind.SourceCommonwealthSupply, SourceCommonwealthSupplyOpeningKey),
        new("source-dummy", "source-dummy", CampaignOwnerOpeningKind.SourceDummy, SourceDummyOpeningKey),
        new("source-motorization", "source-motorization", CampaignOwnerOpeningKind.SourceMotorization, SourceMotorizationOpeningKey),
        new("source-unattached-cargo", "source-unattached-cargo", CampaignOwnerOpeningKind.SourceUnattachedCargo, SourceUnattachedCargoOpeningKey),
        new("source-actions", "source-actions", CampaignOwnerOpeningKind.SourceActions, SourceActionsOpeningKey),
        new("source-axis-gt1-supply", "source-axis-gt1-supply", CampaignOwnerOpeningKind.SourceAxisGt1Supply, SourceAxisGt1SupplyOpeningKey),
        new("source-commonwealth-supply-plan", "source-commonwealth-supply-plan", CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan, SourceCommonwealthSupplyPlanOpeningKey),
        new("source-autoblinda", "source-autoblinda", CampaignOwnerOpeningKind.SourceAutoblinda, SourceAutoblindaOpeningKey),
        new("source-priority-motorization", "source-priority-motorization", CampaignOwnerOpeningKind.SourcePriorityMotorization, SourcePriorityMotorizationOpeningKey),
        new("source-attached-initial-loads", "source-attached-initial-loads", CampaignOwnerOpeningKind.SourceAttachedInitialLoads, SourceAttachedInitialLoadsOpeningKey),
        new("source-initial-wrecks", "source-initial-wrecks", CampaignOwnerOpeningKind.SourceInitialWrecks, SourceInitialWrecksOpeningKey),
        new("source-axis-free-setup", "source-axis-free-setup", CampaignOwnerOpeningKind.SourceAxisFreeSetup, SourceAxisFreeSetupOpeningKey),
        new("source-axis-corps-setup", "source-axis-corps-setup", CampaignOwnerOpeningKind.SourceAxisCorpsSetup, SourceAxisCorpsSetupOpeningKey),
        new("source-maletti-hierarchy", "source-maletti-hierarchy", CampaignOwnerOpeningKind.SourceMalettiHierarchy, SourceMalettiHierarchyOpeningKey),
        new("source-gt2-correction", "source-gt2-correction", CampaignOwnerOpeningKind.SourceGt2Correction, SourceGt2CorrectionOpeningKey),
        new("source-cw-priority-catalog", "source-cw-priority-catalog", CampaignOwnerOpeningKind.SourceCwPriorityCatalog, SourceCwPriorityCatalogOpeningKey),
        new("source-matruh-formation", "source-matruh-formation", CampaignOwnerOpeningKind.SourceMatruhFormation, SourceMatruhFormationOpeningKey),
        new("source-cw-free-guns", "source-cw-free-guns", CampaignOwnerOpeningKind.SourceCwFreeGuns, SourceCwFreeGunsOpeningKey),
        new("source-axis-priority-motorization", "source-axis-priority-motorization", CampaignOwnerOpeningKind.SourceAxisPriority, SourceAxisPriorityOpeningKey),
        new("source-cairo-setup", "source-cairo-setup", CampaignOwnerOpeningKind.SourceCairoSetup, SourceCairoSetupOpeningKey),
    ];

    private static readonly FrozenDictionary<string, CampaignOpeningStageDescriptor> OpeningStagesByName =
        OpeningStages.ToFrozenDictionary(stage => stage.OpeningStage, StringComparer.Ordinal);

    /// <summary>
    /// Read-only derivation of which owner openings are open for the credential's seat. Drains
    /// pending System steps on an in-memory copy of the committed head (Core sessions are immutable
    /// values) and probes the pure draft builders of the candidate stages. Nothing is persisted: no
    /// owner_openings/owner_actions upsert, no round high-water, no journal event, and the SQL
    /// transaction is never committed. The answer is advisory; the opening/action calls remain the
    /// authority (round gates and timeouts are applied there).
    /// </summary>
    internal CampaignOpenSeatResult QueryOpenForSeat(CampaignSeatCredential credential)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        var seat = AuthenticateSeat(connection, transaction, credential, out var verifiedHead)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        // Load failures (missing row, integrity/replay errors) must surface as errors, not as
        // "nothing open"; only a source-initialization head with no gameplay session means empty.
        var head = verifiedHead ?? Load(connection, transaction, seat.CampaignId);
        if (head.LegacySession is not { } session)
            return new CampaignOpenSeatResult(seat.CampaignId, seat.Audience, []);
        session = PreviewDrainSystemSteps(session);
        var candidates = CampaignOpenSeatCatalog.CandidatesFor(session);
        var stages = candidates.IsLegacyChain
            ? OpeningStages
            : candidates.Stages.Select(name => OpeningStagesByName.TryGetValue(name, out var found) ? found
                : throw new InvalidOperationException($"Open-seat catalog names unknown stage '{name}'.")).ToArray();
        var elements = stages.Any(stage => stage.ElementKey is null)
            ? CampaignOpenSeatCatalog.OwnedElementIds(session, seat.Audience) : [];
        var open = new List<CampaignOpenSeatEntry>();
        foreach (var stage in stages)
        {
            var ids = new List<string>();
            var ruleNotImplemented = false;
            foreach (var element in stage.ElementKey is { } key ? [key] : elements)
            {
                try
                {
                    var draft = openingQueries[stage.Kind](session, seat.Audience, element);
                    if (draft.ActionIds.Length > 0) ids.Add(element);
                }
                catch (CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException)
                {
                    ids.Add(element);
                    ruleNotImplemented = true;
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    // Not open at this cut for this seat/element.
                }
            }
            if (ids.Count > 0)
                open.Add(new CampaignOpenSeatEntry(stage.OpeningStage, stage.ActionStage, ids, ruleNotImplemented));
        }
        return new CampaignOpenSeatResult(seat.CampaignId, seat.Audience, open);
    }

    private static ExerciseSession PreviewDrainSystemSteps(ExerciseSession session)
    {
        while (true)
        {
            var query = CampaignHostAdapter.QuerySystem(session);
            if (!query.IsSuccessful || query.ActionSet is not { } set || set.Candidates.Count == 0)
                return session;
            var candidate = set.Candidates.Count == 1 ? set.Candidates[0]
                : OneAmbiguousForcedSystemCandidate(set.Candidates);
            var submission = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, CampaignActionAudience.System, candidate.ActionId);
            var result = CampaignHostAdapter.Submit(session, submission);
            if (!result.IsAccepted || result.SuccessorSession is not { } next)
                throw new InvalidOperationException("Core rejected its own forced System step.");
            session = next;
        }
    }
}
