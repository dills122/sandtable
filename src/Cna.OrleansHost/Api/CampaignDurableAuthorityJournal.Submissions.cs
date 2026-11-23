using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Timeouts;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    /// <summary>
    /// Accepts a reference from a previously queried owner opening. Authentication, current
    /// contact offers, Core adjudication and retained evidence share one SQLite transaction.
    /// A retry of the same accepted reference returns the retained receipt after cold replay.
    /// </summary>
    internal CampaignOwnerSubmissionResult SubmitOwnerMovement(CampaignSeatCredential credential,
        string actingElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, actingElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.Movement);

    internal CampaignOwnerSubmissionResult SubmitOwnerInitiativeOrder(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, InitiativeOrderOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.InitiativeOrder);

    internal CampaignOwnerSubmissionResult SubmitOwnerReserveDesignation(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, ReserveDesignationOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.ReserveDesignation);

    internal CampaignOwnerSubmissionResult SubmitOwnerReaction(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, ReactionOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.Reaction);

    internal CampaignOwnerSubmissionResult SubmitOwnerMovementSegmentCompletion(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, MovementSegmentCompletionOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.MovementSegmentCompletion);

    internal CampaignOwnerSubmissionResult SubmitPregameAxisSupply(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, PregameAxisSupplyOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.PregameAxisSupply);

    internal CampaignOwnerSubmissionResult SubmitPregameAxisSupplyFinish(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, PregameAxisSupplyFinishOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.PregameAxisSupplyFinish);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisDump(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAxisDumpOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAxisDump);

    internal CampaignOwnerSubmissionResult SubmitSourceCommonwealthSupply(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceCommonwealthSupplyOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceCommonwealthSupply);

    internal CampaignOwnerSubmissionResult SubmitSourceDummy(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceDummyOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceDummy);

    internal CampaignOwnerSubmissionResult SubmitSourceMotorization(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceMotorizationOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceMotorization);

    internal CampaignOwnerSubmissionResult SubmitSourceUnattachedCargo(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceUnattachedCargoOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceUnattachedCargo);

    internal CampaignOwnerSubmissionResult SubmitSourceActions(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceActionsOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceActions);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisGt1Supply(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAxisGt1SupplyOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAxisGt1Supply);

    internal CampaignOwnerSubmissionResult SubmitSourceCommonwealthSupplyPlan(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceCommonwealthSupplyPlanOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan);

    internal CampaignOwnerSubmissionResult SubmitSourceAutoblinda(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAutoblindaOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAutoblinda);

    internal CampaignOwnerSubmissionResult SubmitSourcePriorityMotorization(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourcePriorityMotorizationOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourcePriorityMotorization);

    internal CampaignOwnerSubmissionResult SubmitSourceAttachedInitialLoads(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAttachedInitialLoadsOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAttachedInitialLoads);

    internal CampaignOwnerSubmissionResult PlaceSourceInitialWreck(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceInitialWrecksOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceInitialWrecks);

    internal CampaignOwnerSubmissionResult PlaceSourceAxisFreeUnit(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAxisFreeSetupOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAxisFreeSetup);

    internal CampaignOwnerSubmissionResult PlaceSourceAxisCorpsUnit(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAxisCorpsSetupOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAxisCorpsSetup);

    internal CampaignOwnerSubmissionResult SubmitSourceMalettiHierarchy(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceMalettiHierarchyOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceMalettiHierarchy);

    internal CampaignOwnerSubmissionResult SubmitSourceGt2Correction(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceGt2CorrectionOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceGt2Correction);

    internal CampaignOwnerSubmissionResult SubmitSourceCwPriorityCatalog(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceCwPriorityCatalogOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceCwPriorityCatalog);

    internal CampaignOwnerSubmissionResult SubmitSourceMatruhFormation(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceMatruhFormationOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceMatruhFormation);

    internal CampaignOwnerSubmissionResult PlaceSourceCwFreeGun(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceCwFreeGunsOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceCwFreeGuns);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisPriority(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceAxisPriorityOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceAxisPriority);

    internal CampaignOwnerSubmissionResult SubmitSourceCairoUnit(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SourceCairoSetupOpeningKey, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.SourceCairoSetup);

    internal CampaignOwnerSubmissionResult SubmitOwnerCombatSelection(CampaignSeatCredential credential,
        string actingElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, actingElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.CombatSelection);

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaDecline(CampaignSeatCredential credential,
        string defenderElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, defenderElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.RetreatBeforeAssault);

    // Task 020D (owner ruling 2026-11-20): the Host RBA route is decline-only under the bounded
    // CMB-POL-001 profile. There is no owner RBA move submission; retained contact-rba-move steps
    // from earlier histories still replay through CampaignHostAdapter import.

    internal CampaignOwnerSubmissionResult SubmitOwnerForceSeal(CampaignSeatCredential credential,
        string elementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, elementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.ForceAssignment);

    internal CampaignOwnerSubmissionResult SubmitOwnerPostAssaultRetreat(
        CampaignSeatCredential credential, string defenderElementId,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, defenderElementId, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.PostAssaultRetreat);

    internal CampaignOwnerSubmissionResult SubmitOwnerCustody(
        CampaignSeatCredential credential, string captorElementId,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, captorElementId, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.Custody);

    internal CampaignOwnerSubmissionResult SubmitOwnerReserveRelease(
        CampaignSeatCredential credential, string reserveElementId,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, reserveElementId, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.ReserveRelease);

    internal CampaignOwnerSubmissionResult SubmitOwnerCycleControl(
        CampaignSeatCredential credential, long openingRevision,
        string publicActionRef) =>
        SubmitOwnerActionCore(credential, CycleControlOpeningKey, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.CycleControl);

    internal CampaignOwnerSubmissionResult SubmitOwnerTruckConvoyPass(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, TruckConvoyPassOpeningKey, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.TruckConvoyPass);

    internal CampaignOwnerSubmissionResult SubmitOwnerPostCombatPass(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, PostCombatPassOpeningKey, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.PostCombatPass);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondReserve(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SecondReserveOpeningKey, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.SecondReserve);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondMovement(CampaignSeatCredential credential,
        string elementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, elementId, openingRevision, publicActionRef, CampaignOwnerOpeningKind.SecondMovement);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondMovementCompletion(CampaignSeatCredential credential,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, SecondMovementCompletionOpeningKey, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.SecondMovementCompletion);

    internal CampaignOwnerSubmissionResult SubmitOwnerRepeatMovement(
        CampaignSeatCredential credential, long openingRevision,
        string publicActionRef) =>
        SubmitOwnerActionCore(credential, RepeatMovementOpeningKey, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.RepeatMovement);

    internal CampaignOwnerSubmissionResult SubmitOwnerRepeatCombat(
        CampaignSeatCredential credential, string actingElementId,
        long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, actingElementId, openingRevision,
            publicActionRef, CampaignOwnerOpeningKind.RepeatCombatSelection);

    private CampaignOwnerSubmissionResult SubmitOwnerActionCore(CampaignSeatCredential credential,
        string actingElementId, long openingRevision, string publicActionRef,
        CampaignOwnerOpeningKind kind)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(credential);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var seat = AuthenticateSeat(connection, transaction, credential, out var verifiedHead)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        // UX-001 (PR #504): same drain as the query side -- see
        // CampaignDurableAuthorityJournal.cs's DrainPendingSystemSteps remarks. A submission
        // against a stale owner_openings row (because System steps advanced since it was
        // queried) still fails the usual revision/visible-cut checks below, exactly as any
        // other authority advance between query and submit already does.
        var head = DrainPendingSystemSteps(connection, transaction, seat.CampaignId,
            verifiedHead ?? Load(connection, transaction, seat.CampaignId));
        if (kind == CampaignOwnerOpeningKind.ForceAssignment && head.Timing is null)
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.ForceRoundUnavailableCode);
        if (string.IsNullOrWhiteSpace(actingElementId) || actingElementId.Length > 128
            || !IsPublicReference(publicActionRef)
            || openingRevision < 1)
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.InvalidRequestCode);

        using (var retry = connection.CreateCommand())
        {
            retry.Transaction = transaction;
            // owner_side is bound alongside the pre-existing match columns (never a StepHash
            // input -- see PersistAccepted's own remarks) specifically so this idempotency
            // shortcut cannot hand a DIFFERENT credential the accepted side's own cached
            // receipt merely because the (element,ref,epoch,revision) tuple it submitted
            // happens to coincide with that side's -- the epoch is a per-side counter, so
            // "coincide" includes the ordinary case of both sides' very first action each
            // being epoch 1. Without this, a resubmission under a foreign credential that
            // somehow replays the correct side's own ref would short-circuit here and
            // observe that side's receipt instead of the rejection every
            // *RejectsStaleForgedAndWrongAudienceSubmissions-style test asserts.
            retry.CommandText = """
                SELECT canonical_receipt FROM accepted_steps
                WHERE campaign_id=$id AND action_kind=$kind
                  AND acting_element_id=$element AND owner_public_ref=$ref
                  AND seat_epoch=$epoch AND owner_revision=$revision AND owner_side=$side
                """;
            retry.Parameters.AddWithValue("$id", seat.CampaignId);
            retry.Parameters.AddWithValue("$kind", AcceptedKind(kind));
            retry.Parameters.AddWithValue("$element", actingElementId);
            retry.Parameters.AddWithValue("$ref", publicActionRef);
            retry.Parameters.AddWithValue("$epoch", seat.Epoch);
            retry.Parameters.AddWithValue("$revision", openingRevision);
            retry.Parameters.AddWithValue("$side", Side(seat.Audience));
            if (retry.ExecuteScalar() is byte[] retainedReceipt)
            {
                RequireBounded(retainedReceipt);
                return CampaignOwnerSubmissionResult.Accepted(retainedReceipt);
            }
        }

        ForceRound? forceRound = null;
        if (kind == CampaignOwnerOpeningKind.ForceAssignment)
        {
            if (schemaVersion != SchemaVersion) return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.SchemaMismatchCode);
            (string DecisionActionId, string DeclineEventHash) binding;
            try
            {
                binding = CampaignHostAdapter.QueryNativeCombatForceRoundBinding(head.Session);
            }
            catch (InvalidOperationException)
            {
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.ForceRoundUnavailableCode);
            }
            if (!ForceRoundExists(connection, transaction, seat.CampaignId,
                binding.DeclineEventHash))
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.ForceRoundUnavailableCode);
            forceRound = ReadActiveForceRound(connection, transaction, head,
                seat.CampaignId);
        }

        // Reaction (#394 gap 3, PR #501): while a window opened by a second-side Movement
        // cannot be resolved into an owner opening at all, no owner_openings row for it is
        // ever persisted either (the query that would persist one throws first) -- so the
        // ordinary revision/visible-cut lookup below can never match and would otherwise fall
        // through to the ordinary StaleOpeningCode rejection below (API shape-review,
        // 2026-11-23) -- accurate as far as it goes ("no such opening row exists"), but still
        // not the real reason: the decision genuinely cannot be resolved yet, not merely stale.
        // Checking this specific, already-distinguishable case here, before that lookup, is
        // what lets a real player's submission get the same explicit rule-not-implemented
        // signal the query side already gives, instead of an ordinary-looking rejection that
        // would silently approximate a real, open decision as a no-op (FID-002).
        if (kind == CampaignOwnerOpeningKind.Reaction)
        {
            try { CampaignHostAdapter.QueryReactionOwnerDraft(head.Session, seat.Audience); }
            catch (CampaignHostAdapter.CampaignReactionWindowRuleNotImplementedException)
            { return CampaignOwnerSubmissionResult.RuleNotImplemented(); }
            catch (InvalidOperationException) { /* no window at all; fall through as usual */ }
        }

        long authorityVersion;
        string visibleDigest;
        byte[] visibleBytes;
        long actionCount;
        using (var opening = connection.CreateCommand())
        {
            opening.Transaction = transaction;
            opening.CommandText = """
                SELECT visible_digest,visible_bytes,authority_version,action_count
                FROM owner_openings
                WHERE campaign_id=$id AND side=$side AND acting_element_id=$element
                  AND seat_epoch=$epoch AND revision=$revision
                """;
            BindOpeningKey(opening, seat.CampaignId, Side(seat.Audience), actingElementId);
            opening.Parameters.AddWithValue("$epoch", seat.Epoch);
            opening.Parameters.AddWithValue("$revision", openingRevision);
            using var reader = opening.ExecuteReader();
            if (!reader.Read()) return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.StaleOpeningCode);
            visibleDigest = reader.GetString(0);
            visibleBytes = ReadBounded(reader, 1);
            authorityVersion = reader.GetInt64(2);
            actionCount = reader.GetInt64(3);
        }
        // A hidden accepted step may advance private authority without changing this owner's
        // visible opening. Revalidate the current visible cut and action map below instead of
        // making that private version change an observable rejection.
        if (authorityVersion < 1
            || authorityVersion > CampaignExercises.QueryCheckpoint(head.Session).StateVersion
            || visibleDigest != "sha256:" + Convert.ToHexStringLower(SHA256.HashData(visibleBytes)))
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.StaleOpeningCode);
        byte[] currentVisible;
        string[] actionIds;
        try
        {
            var draft = openingQueries[kind](head.Session, seat.Audience, actingElementId);
            currentVisible = draft.VisibleBytes;
            actionIds = draft.ActionIds;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.StaleOpeningCode);
        }
        if (!currentVisible.AsSpan().SequenceEqual(visibleBytes))
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.StaleOpeningCode);
        if (actionIds.Length != actionCount) return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.StaleOpeningCode);

        int ordinal;
        string coreActionId;
        using (var action = connection.CreateCommand())
        {
            action.Transaction = transaction;
            action.CommandText = """
                SELECT ordinal,core_action_id FROM owner_actions
                WHERE campaign_id=$id AND side=$side AND acting_element_id=$element
                  AND public_ref=$ref
                """;
            BindOpeningKey(action, seat.CampaignId, Side(seat.Audience), actingElementId);
            action.Parameters.AddWithValue("$ref", publicActionRef);
            using var reader = action.ExecuteReader();
            if (!reader.Read()) return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.UnknownActionReferenceCode);
            ordinal = reader.GetInt32(0);
            coreActionId = reader.GetString(1);
        }
        if (ordinal < 0 || ordinal >= actionIds.Length)
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.UnknownActionReferenceCode);
        if (kind == CampaignOwnerOpeningKind.SourceActions)
        {
            if (!CampaignHostAdapter.MatchesIssuedSourceAction(authorityVersion, visibleBytes, ordinal, coreActionId))
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.UnknownActionReferenceCode);
            // A hidden opponent decision can advance the source version without changing this
            // authenticated visible offer. Submit its freshly revalidated current identity.
            coreActionId = actionIds[ordinal];
        }
        else if (coreActionId != actionIds[ordinal])
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.UnknownActionReferenceCode);
        var expectedRef = Reference(ReadReferenceKey(connection, transaction), seat.CampaignId,
            Side(seat.Audience), actingElementId, seat.Epoch, openingRevision,
            visibleDigest, ordinal);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedRef),
                Encoding.UTF8.GetBytes(publicActionRef)))
            return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.InvalidReferenceCode);

        // Only a current, authenticated owner offer may trigger clock cancellation.
        // Retained accepted retries have already returned their original receipt above.
        if (forceRound is not null)
        {
            var gate = GateForceRound(connection, transaction, forceRound);
            forceRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                CancelForceRound(connection, transaction, head, forceRound,
                    reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }

        var checkpoint = CampaignExercises.QueryCheckpoint(head.Session);
        var submission = new CampaignActionSubmission(1, seat.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, seat.Audience, coreActionId);
        var result = kind switch
        {
            CampaignOwnerOpeningKind.PregameAxisSupply => CampaignHostAdapter.Submit(head.Session, submission),
            CampaignOwnerOpeningKind.PregameAxisSupplyFinish => CampaignHostAdapter.FinishPregameAxisSupply(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAxisDump => CampaignHostAdapter.PlaceSourceAxisDump(head.Session, submission),
            CampaignOwnerOpeningKind.SourceCommonwealthSupply => CampaignHostAdapter.PlaceSourceCommonwealthSupply(head.Session, submission),
            CampaignOwnerOpeningKind.SourceDummy => CampaignHostAdapter.DecideSourceDummy(head.Session, submission),
            CampaignOwnerOpeningKind.SourceMotorization => CampaignHostAdapter.AdjustSourceMotorization(head.Session, submission),
            CampaignOwnerOpeningKind.SourceUnattachedCargo => CampaignHostAdapter.SubmitSourceUnattachedCargo(head.Session, submission),
            CampaignOwnerOpeningKind.SourceActions => CampaignHostAdapter.Submit(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAxisGt1Supply => CampaignHostAdapter.SubmitSourceAxisGt1Supply(head.Session, submission),
            CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan => CampaignHostAdapter.SubmitSourceCommonwealthSupplyPlan(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAutoblinda => CampaignHostAdapter.SubmitSourceAutoblinda(head.Session, submission),
            CampaignOwnerOpeningKind.SourcePriorityMotorization => CampaignHostAdapter.AdjustSourcePriorityMotorization(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAttachedInitialLoads => CampaignHostAdapter.SubmitSourceAttachedInitialLoads(head.Session, submission),
            CampaignOwnerOpeningKind.SourceInitialWrecks => CampaignHostAdapter.PlaceSourceInitialWreck(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAxisFreeSetup => CampaignHostAdapter.PlaceSourceAxisFreeUnit(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAxisCorpsSetup => CampaignHostAdapter.PlaceSourceAxisCorpsUnit(head.Session, submission),
            CampaignOwnerOpeningKind.SourceMalettiHierarchy => CampaignHostAdapter.SubmitSourceMalettiHierarchy(head.Session, submission),
            CampaignOwnerOpeningKind.SourceGt2Correction => CampaignHostAdapter.SubmitSourceGt2Correction(head.Session, submission),
            CampaignOwnerOpeningKind.SourceCwPriorityCatalog => CampaignHostAdapter.SubmitSourceCwPriorityCatalog(head.Session, submission),
            CampaignOwnerOpeningKind.SourceMatruhFormation => CampaignHostAdapter.SubmitSourceMatruhFormation(head.Session, submission),
            CampaignOwnerOpeningKind.SourceCwFreeGuns => CampaignHostAdapter.PlaceSourceCwFreeGun(head.Session, submission),
            CampaignOwnerOpeningKind.SourceAxisPriority => CampaignHostAdapter.SubmitSourceAxisPriority(head.Session, submission),
            CampaignOwnerOpeningKind.SourceCairoSetup => CampaignHostAdapter.PlaceSourceCairoUnit(head.Session, submission),
            CampaignOwnerOpeningKind.CombatSelection =>
                CampaignHostAdapter.SubmitContactOwnerCombatSelection(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.RetreatBeforeAssault =>
                CampaignHostAdapter.SubmitContactDefenderRbaDecline(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.ForceAssignment =>
                CampaignHostAdapter.SubmitContactOwnerForceSeal(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.PostAssaultRetreat =>
                CampaignHostAdapter.SubmitContactPostAssaultRetreat(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.Custody =>
                CampaignHostAdapter.SubmitNativeCombatCustody(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.ReserveRelease =>
                CampaignHostAdapter.SubmitNativeReserveReleaseChoice(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.CycleControl =>
                CampaignHostAdapter.SubmitNativeCycleControl(head.Session, submission),
            CampaignOwnerOpeningKind.TruckConvoyPass =>
                CampaignHostAdapter.SubmitTruckConvoyPass(head.Session, submission),
            CampaignOwnerOpeningKind.InitiativeOrder =>
                CampaignHostAdapter.SubmitInitiativeOrder(head.Session, submission),
            CampaignOwnerOpeningKind.ReserveDesignation =>
                CampaignHostAdapter.SubmitReserveDesignation(head.Session, submission),
            CampaignOwnerOpeningKind.Reaction =>
                CampaignHostAdapter.SubmitReaction(head.Session, submission),
            CampaignOwnerOpeningKind.MovementSegmentCompletion =>
                CampaignHostAdapter.SubmitMovementSegmentCompletion(head.Session, submission),
            CampaignOwnerOpeningKind.PostCombatPass =>
                CampaignHostAdapter.SubmitPostCombatPass(head.Session, submission),
            CampaignOwnerOpeningKind.SecondReserve =>
                CampaignHostAdapter.SubmitSecondReserve(head.Session, submission),
            CampaignOwnerOpeningKind.SecondMovement =>
                CampaignHostAdapter.SubmitSecondMovement(head.Session, submission, actingElementId),
            CampaignOwnerOpeningKind.SecondMovementCompletion =>
                CampaignHostAdapter.SubmitSecondMovementCompletion(head.Session, submission),
            CampaignOwnerOpeningKind.RepeatMovement =>
                CampaignHostAdapter.SubmitNativeRepeatMovement(head.Session, submission),
            CampaignOwnerOpeningKind.RepeatCombatSelection =>
                CampaignHostAdapter.SubmitNativeRepeatCombat(head.Session,
                    submission, actingElementId),
            _ => CampaignHostAdapter.SubmitContactOwnerMovement(head.Session,
                submission, actingElementId),
        };
        if (!result.IsAccepted)
            return result.RejectionReason == CampaignActionSubmissionRejectionReason.RuleNotImplemented
                ? CampaignOwnerSubmissionResult.RuleNotImplemented()
                : CampaignOwnerSubmissionResult.Rejected(
                    CampaignOwnerSubmissionResult.CoreRejectionCode(result.RejectionReason));
        RbaRound? rbaRound = null;
        if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
        {
            rbaRound = ReadActiveRbaRound(connection, transaction, head, seat.CampaignId);
            var gate = GateRbaRound(connection, transaction, rbaRound);
            rbaRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                CancelRbaRound(connection, transaction, head, rbaRound, reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }
        CycleRound? cycleRound = null;
        if (kind == CampaignOwnerOpeningKind.CycleControl)
        {
            cycleRound = ReadActiveCycleRound(connection, transaction, head, seat.CampaignId);
            var gate = GateCycleRound(connection, transaction, cycleRound);
            cycleRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                TimeoutCycleRound(connection, transaction, head, cycleRound, reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }
        SelectionRound? selectionRound = null;
        if (kind == CampaignOwnerOpeningKind.CombatSelection
            || kind == CampaignOwnerOpeningKind.RepeatCombatSelection)
        {
            selectionRound = ReadActiveSelectionRound(connection, transaction, head, seat.CampaignId);
            var gate = GateSelectionRound(connection, transaction, selectionRound);
            selectionRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                TimeoutSelectionRound(connection, transaction, head, selectionRound, reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }
        RetreatRound? retreatRound = null;
        if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
        {
            retreatRound = ReadActiveRetreatRound(connection, transaction, head, seat.CampaignId);
            var gate = GateRetreatRound(connection, transaction, retreatRound);
            retreatRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                TimeoutRetreatRound(connection, transaction, head, retreatRound, reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }
        CustodyRound? custodyRound = null;
        if (kind == CampaignOwnerOpeningKind.Custody)
        {
            custodyRound = ReadActiveCustodyRound(connection, transaction, head, seat.CampaignId);
            var gate = GateCustodyRound(connection, transaction, custodyRound);
            custodyRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                TimeoutCustodyRound(connection, transaction, head, custodyRound, reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }
        ReserveRound? reserveRound = null;
        if (kind == CampaignOwnerOpeningKind.ReserveRelease)
        {
            reserveRound = ReadActiveReserveRound(connection, transaction, head, seat.CampaignId);
            var gate = GateReserveRound(connection, transaction, reserveRound);
            reserveRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                TimeoutReserveRound(connection, transaction, head, reserveRound, reason, gate.CancelledAt);
                transaction.Commit();
                return CampaignOwnerSubmissionResult.Rejected(CampaignOwnerSubmissionResult.RoundCancelledCode);
            }
        }
        var acceptedHead = PersistAccepted(connection, transaction, seat.CampaignId, head,
            AcceptedKind(kind), actingElementId,
            publicActionRef, seat.Epoch, openingRevision, submission, result);
        if (selectionRound is not null)
        {
            UpdateSelectionRound(connection, transaction, selectionRound with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Selection);
        }
        // Movement Segment Completion is the first side's own analog of SecondMovementCompletion
        // (#394 gap 4's own precedent below): completing it, like completing an ordinary
        // Movement cut, opens the way to a Combat Selection round, so it needs this exact same
        // round-initialization call -- not just the dedicated MoveElementAction/
        // StopElementMovementAction path (CampaignOwnerOpeningKind.Movement) completing a move.
        if (kind is CampaignOwnerOpeningKind.Movement or CampaignOwnerOpeningKind.MovementSegmentCompletion)
            TryInitializeSelectionRound(connection, transaction, acceptedHead, seat.CampaignId);
        if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
        {
            UpdateRbaRound(connection, transaction, rbaRound! with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Rba);
            InitializeForceRound(connection, transaction, acceptedHead, seat.CampaignId,
                rbaRound.HighWaterMs);
        }
        if (kind == CampaignOwnerOpeningKind.ReserveRelease
            && CampaignHostAdapter.IsNativeReserveReleaseEmpty(acceptedHead.Session))
        {
            UpdateReserveRound(connection, transaction, reserveRound! with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Reserve);
        }
        if (kind == CampaignOwnerOpeningKind.CycleControl)
        {
            UpdateCycleRound(connection, transaction, cycleRound! with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Cycle);
        }
        if (kind == CampaignOwnerOpeningKind.RepeatMovement)
            AppendNativeRepeatBreakdown(connection, transaction, seat.CampaignId);
        if (kind == CampaignOwnerOpeningKind.SecondMovementCompletion)
        {
            // Preserve the player's accepted completion even when the engine cannot
            // yet prove that Breakdown has no outstanding obligations.
            var breakdown = CampaignHostAdapter.CompleteSecondBreakdown(acceptedHead.Session);
            if (breakdown.IsAccepted)
            {
                var command = CampaignHostAdapter.SecondBreakdownCommand(acceptedHead.Session, breakdown);
                var breakdownHead = PersistAccepted(connection, transaction, seat.CampaignId, acceptedHead,
                    CampaignHostAcceptedStep.NativeSecondBreakdownKind, null,
                    null, null, null, command, breakdown);
                // #394 gap 4: SecondMovementCompletion is the second-side
                // analog of Movement (both complete a movement and open the
                // way to a Combat selection round), not of Custody -- match
                // that sibling's pattern instead of leaving nothing to
                // initialize the successor Selection round.
                TryInitializeSelectionRound(connection, transaction, breakdownHead, seat.CampaignId);
            }
        }
        if (kind == CampaignOwnerOpeningKind.Custody)
        {
            UpdateCustodyRound(connection, transaction, custodyRound! with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Custody);
            AppendNativeCombatSuccessor(connection, transaction, seat.CampaignId, acceptedHead);
        }
        if (kind == CampaignOwnerOpeningKind.ReserveRelease)
            AppendEmptyReserveRelease(connection, transaction, seat.CampaignId, acceptedHead);
        if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
        {
            UpdateRetreatRound(connection, transaction, retreatRound! with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Retreat);
            AppendWorldSettlement(connection, transaction, seat.CampaignId, acceptedHead);
        }
        // #394 extra-3: guarded against a second-side session whose stale
        // first-side CurrentCombatDecision happens to carry the same Choice
        // string as the decision the second side actually just made --
        // without this, this first-side-only check could alias onto a
        // genuinely second-side submission and run the wrong continuation.
        if (kind == CampaignOwnerOpeningKind.CombatSelection
            && !CampaignHostAdapter.IsSecondSideCombatPending(result.SuccessorSession!)
            && CampaignHostAdapter.IsNativeCombatVoluntaryFinish(result.SuccessorSession!))
        {
            var noAttack = CampaignHostAdapter.CompleteNativeCombatNoAttack(result.SuccessorSession!);
            if (!noAttack.IsAccepted)
                throw new InvalidOperationException("Accepted voluntary finish cannot complete Combat.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeCombatNoAttackCommand(
                systemHead.Session, noAttack);
            var reserveHead = PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeCombatNoAttackKind, null,
                null, null, null, systemCommand, noAttack);
            AppendEmptyReserveRelease(connection, transaction, seat.CampaignId, reserveHead);
        }
        // #394 extra-3: second-side analog of the voluntary-finish
        // auto-continuation above. Uses the side-correct
        // IsNativeSecondCombatVoluntaryFinish/CompleteSecondCombatNoAttack
        // pair (extra-3) instead of the first-side checks, which would read
        // whatever the *first* side chose, not this decision. The second
        // side's own Reserve Release then follows from its V13 completion.
        else if (kind == CampaignOwnerOpeningKind.CombatSelection
            && CampaignHostAdapter.IsNativeSecondCombatVoluntaryFinish(result.SuccessorSession!))
        {
            var noAttack = CampaignHostAdapter.CompleteSecondCombatNoAttack(result.SuccessorSession!);
            if (!noAttack.IsAccepted)
                throw new InvalidOperationException("Accepted second-side voluntary finish cannot complete Combat.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.SecondCombatNoAttackCommand(
                systemHead.Session, noAttack);
            var reserveHead = PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeSecondCombatNoAttackKind, null,
                null, null, null, systemCommand, noAttack);
            // The second side's own Reserve Release now initializes from its
            // V13 completion (CampaignNativeReserveReleaseStateV27.InitializeAfterSecondNoAttack).
            AppendEmptyReserveRelease(connection, transaction, seat.CampaignId, reserveHead);
        }
        // A second-side assault pick enters its own Barrage exactly like the
        // first side's: the V14-V26 stages replay from the V47 selection
        // (CampaignNativeRepeatAssaultRuntime.ReplaySecond) and open the
        // defender's RBA round.
        else if (kind == CampaignOwnerOpeningKind.CombatSelection
            && CampaignHostAdapter.IsSecondSideCombatPending(result.SuccessorSession!)
            && CampaignHostAdapter.IsNativeSecondCombatSelectedAssault(result.SuccessorSession!))
        {
            var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(result.SuccessorSession!);
            if (!barrage.IsAccepted)
                throw new InvalidOperationException("Accepted second-side assault cannot complete Barrage.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeCombatBarrageCommand(
                systemHead.Session, barrage);
            var barrageHead = PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeCombatBarrageKind, null,
                null, null, null, systemCommand, barrage);
            InitializeRbaRound(connection, transaction, barrageHead, seat.CampaignId);
        }
        // Guarded so a stale first-side "select-contact-assault" pick can
        // never alias onto a second-side submission.
        else if (kind == CampaignOwnerOpeningKind.CombatSelection
            && !CampaignHostAdapter.IsSecondSideCombatPending(result.SuccessorSession!)
            && CampaignHostAdapter.IsNativeCombatSelectedAssault(result.SuccessorSession!))
        {
            var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(result.SuccessorSession!);
            if (!barrage.IsAccepted)
                throw new InvalidOperationException("Accepted infantry assault cannot complete Barrage.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeCombatBarrageCommand(
                systemHead.Session, barrage);
            var barrageHead = PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeCombatBarrageKind, null,
                null, null, null, systemCommand, barrage);
            InitializeRbaRound(connection, transaction, barrageHead, seat.CampaignId);
        }
        else if (kind == CampaignOwnerOpeningKind.RepeatCombatSelection
            && CampaignHostAdapter.IsNativeRepeatCombatVoluntaryFinish(
                result.SuccessorSession!))
        {
            var completion = CampaignHostAdapter.CompleteNativeRepeatCombatNoAttack(
                result.SuccessorSession!);
            if (!completion.IsAccepted)
                throw new InvalidOperationException(
                    "Accepted repeated finish cannot complete Combat.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeRepeatCombatNoAttackCommand(
                systemHead.Session, completion);
            var reserveHead = PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind, null,
                null, null, null, systemCommand, completion);
            AppendEmptyReserveRelease(connection, transaction, seat.CampaignId, reserveHead);
        }
        else if (kind == CampaignOwnerOpeningKind.RepeatCombatSelection
            && CampaignHostAdapter.IsNativeRepeatCombatSelectedAssault(
                result.SuccessorSession!))
        {
            var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(
                result.SuccessorSession!);
            if (!barrage.IsAccepted)
                throw new InvalidOperationException(
                    "Accepted repeated assault cannot enter Barrage.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeCombatBarrageCommand(
                systemHead.Session, barrage);
            var barrageHead = PersistAccepted(connection, transaction, seat.CampaignId,
                systemHead, CampaignHostAcceptedStep.NativeCombatBarrageKind,
                null, null, null, null, systemCommand, barrage);
            InitializeRbaRound(connection, transaction, barrageHead, seat.CampaignId);
        }
        else if (kind == CampaignOwnerOpeningKind.ForceAssignment
            && CampaignHostAdapter.IsNativeCombatForcePrepared(result.SuccessorSession!))
        {
            var completion = CampaignHostAdapter.CompleteNativeCombatForceAssignment(
                result.SuccessorSession!);
            if (!completion.IsAccepted)
                throw new InvalidOperationException("Two accepted seals cannot complete Force Assignment.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeCombatForceCompletionCommand(
                systemHead.Session, completion);
            PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeCombatForceCompletionKind, null,
                null, null, null, systemCommand, completion);
            UpdateForceRound(connection, transaction, forceRound! with { Status = "completed" });
            RecordRoundClosed(CampaignRoundKind.Force);
            AppendEmptyAntiArmor(connection, transaction, seat.CampaignId);
        }
        transaction.Commit();
        return CampaignOwnerSubmissionResult.Accepted(
            CampaignActionAcceptanceReceiptSerializer.Serialize(result.Evidence!.Receipt));
    }

    private void AppendNativeRepeatBreakdown(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId)
    {
        var head = Load(connection, transaction, campaignId);
        var completed = CampaignHostAdapter.CompleteNativeRepeatBreakdown(
            head.Session);
        if (!completed.IsAccepted)
            throw new InvalidOperationException(
                "Accepted repeated Movement cannot complete Breakdown.");
        var command = CampaignHostAdapter.NativeRepeatBreakdownCommand(
            head.Session, completed);
        var completedHead = PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.NativeRepeatBreakdownKind,
            null, null, null, null, command, completed);
        InitializeSelectionRound(connection, transaction, completedHead, campaignId);
    }

    private static string AcceptedKind(CampaignOwnerOpeningKind kind) => kind switch
    {
        CampaignOwnerOpeningKind.Movement => CampaignHostAcceptedStep.ContactMovementKind,
        CampaignOwnerOpeningKind.InitiativeOrder => CampaignHostAcceptedStep.ContactInitiativeOrderKind,
        CampaignOwnerOpeningKind.ReserveDesignation => CampaignHostAcceptedStep.ContactReserveDesignationKind,
        CampaignOwnerOpeningKind.Reaction => CampaignHostAcceptedStep.ContactReactionKind,
        CampaignOwnerOpeningKind.MovementSegmentCompletion => CampaignHostAcceptedStep.ContactMovementSegmentCompletionKind,
        CampaignOwnerOpeningKind.CombatSelection => CampaignHostAcceptedStep.ContactCombatSelectionKind,
        CampaignOwnerOpeningKind.RetreatBeforeAssault => CampaignHostAcceptedStep.ContactRbaDeclineKind,
        CampaignOwnerOpeningKind.ForceAssignment => CampaignHostAcceptedStep.ContactForceSealKind,
        CampaignOwnerOpeningKind.PostAssaultRetreat =>
            CampaignHostAcceptedStep.ContactPostAssaultRetreatKind,
        CampaignOwnerOpeningKind.Custody =>
            CampaignHostAcceptedStep.ContactCombatCustodyKind,
        CampaignOwnerOpeningKind.ReserveRelease =>
            CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind,
        CampaignOwnerOpeningKind.CycleControl =>
            CampaignHostAcceptedStep.ContactCycleControlKind,
        CampaignOwnerOpeningKind.TruckConvoyPass =>
            CampaignHostAcceptedStep.ContactTruckConvoyPassKind,
        CampaignOwnerOpeningKind.PostCombatPass =>
            CampaignHostAcceptedStep.ContactPostCombatPassKind,
        CampaignOwnerOpeningKind.SecondReserve =>
            CampaignHostAcceptedStep.ContactSecondReserveKind,
        CampaignOwnerOpeningKind.SecondMovement =>
            CampaignHostAcceptedStep.ContactSecondMovementKind,
        CampaignOwnerOpeningKind.SecondMovementCompletion =>
            CampaignHostAcceptedStep.ContactSecondMovementCompletionKind,
        CampaignOwnerOpeningKind.RepeatMovement =>
            CampaignHostAcceptedStep.ContactRepeatMovementKind,
        CampaignOwnerOpeningKind.RepeatCombatSelection =>
            CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind,
        CampaignOwnerOpeningKind.PregameAxisSupply => CampaignHostAcceptedStep.PregameAxisSupplyKind,
        CampaignOwnerOpeningKind.PregameAxisSupplyFinish => CampaignHostAcceptedStep.PregameAxisSupplyFinishKind,
        CampaignOwnerOpeningKind.SourceAxisDump => CampaignHostAcceptedStep.SourceAxisDumpKind,
        CampaignOwnerOpeningKind.SourceCommonwealthSupply => CampaignHostAcceptedStep.SourceCommonwealthSupplyKind,
        CampaignOwnerOpeningKind.SourceDummy => CampaignHostAcceptedStep.SourceDummyKind,
        CampaignOwnerOpeningKind.SourceMotorization => CampaignHostAcceptedStep.SourceMotorizationKind,
        CampaignOwnerOpeningKind.SourceUnattachedCargo => CampaignHostAcceptedStep.SourceUnattachedCargoKind,
        CampaignOwnerOpeningKind.SourceActions => CampaignHostAcceptedStep.SourceActionsKind,
        CampaignOwnerOpeningKind.SourceAxisGt1Supply => CampaignHostAcceptedStep.SourceAxisGt1SupplyKind,
        CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan => CampaignHostAcceptedStep.SourceCommonwealthSupplyPlanKind,
        CampaignOwnerOpeningKind.SourceAutoblinda => CampaignHostAcceptedStep.SourceAutoblindaKind,
        CampaignOwnerOpeningKind.SourcePriorityMotorization => CampaignHostAcceptedStep.SourcePriorityMotorizationChoiceKind,
        CampaignOwnerOpeningKind.SourceAttachedInitialLoads => CampaignHostAcceptedStep.SourceAttachedInitialLoadsChoiceKind,
        CampaignOwnerOpeningKind.SourceInitialWrecks => CampaignHostAcceptedStep.SourceInitialWrecksChoiceKind,
        CampaignOwnerOpeningKind.SourceAxisFreeSetup => CampaignHostAcceptedStep.SourceAxisFreeSetupChoiceKind,
        CampaignOwnerOpeningKind.SourceAxisCorpsSetup => CampaignHostAcceptedStep.SourceAxisCorpsSetupChoiceKind,
        CampaignOwnerOpeningKind.SourceMalettiHierarchy => CampaignHostAcceptedStep.SourceMalettiHierarchyChoiceKind,
        CampaignOwnerOpeningKind.SourceGt2Correction => CampaignHostAcceptedStep.SourceGt2CorrectionChoiceKind,
        CampaignOwnerOpeningKind.SourceCwPriorityCatalog => CampaignHostAcceptedStep.SourceCwPriorityCatalogChoiceKind,
        CampaignOwnerOpeningKind.SourceMatruhFormation => CampaignHostAcceptedStep.SourceMatruhFormationChoiceKind,
        CampaignOwnerOpeningKind.SourceCwFreeGuns => CampaignHostAcceptedStep.SourceCwFreeGunsChoiceKind,
        CampaignOwnerOpeningKind.SourceAxisPriority => CampaignHostAcceptedStep.SourceAxisPriorityChoiceKind,
        CampaignOwnerOpeningKind.SourceCairoSetup => CampaignHostAcceptedStep.SourceCairoUnitKind,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static bool IsPublicReference(string? value)
    {
        if (value is null || value.Length != 68
            || !value.StartsWith("act.", StringComparison.Ordinal)) return false;
        for (var index = 4; index < value.Length; index++)
        {
            if (value[index] is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f')) return false;
        }
        return true;
    }
}

internal sealed class CampaignOwnerSubmissionResult
{
    private readonly byte[]? canonicalReceipt;

    private CampaignOwnerSubmissionResult(byte[]? canonicalReceipt, string? rejectionCode = null)
    {
        this.canonicalReceipt = canonicalReceipt?.ToArray();
        RejectionCode = rejectionCode;
    }

    internal const string RuleNotImplementedCode = "rule-not-implemented";
    // Reason codes for SubmitOwnerActionCore's Rejected() call sites (owner shape-review,
    // 2026-11-23): named from the real, already-distinguished branches in that method, not
    // invented categories. "rejected" (the HTTP-side fallback for a null RejectionCode) stays
    // the catch-all for anything not categorized below.
    internal const string InvalidRequestCode = "invalid-request";
    internal const string SchemaMismatchCode = "schema-mismatch";
    internal const string ForceRoundUnavailableCode = "force-round-unavailable";
    internal const string StaleOpeningCode = "stale-opening";
    internal const string UnknownActionReferenceCode = "unknown-action-reference";
    internal const string InvalidReferenceCode = "invalid-reference";
    internal const string RoundCancelledCode = "round-cancelled";

    internal static CampaignOwnerSubmissionResult Accepted(byte[] canonicalReceipt) =>
        new(canonicalReceipt);

    internal static CampaignOwnerSubmissionResult Rejected() => new(null);

    internal static CampaignOwnerSubmissionResult Rejected(string reasonCode) => new(null, reasonCode);

    // Maps Core's own already-distinguished rejection reason (CampaignActionSubmissionRejectionReason)
    // to the same kebab-case reason-code shape used above, instead of discarding it at the HTTP
    // boundary. RuleNotImplemented is handled separately by its own branch at the call site.
    internal static string CoreRejectionCode(CampaignActionSubmissionRejectionReason reason) => reason switch
    {
        CampaignActionSubmissionRejectionReason.InvalidSubmission => "invalid-submission",
        CampaignActionSubmissionRejectionReason.InvalidAuthority => "invalid-authority",
        CampaignActionSubmissionRejectionReason.CampaignMismatch => "campaign-mismatch",
        CampaignActionSubmissionRejectionReason.StaleState => "stale-state",
        CampaignActionSubmissionRejectionReason.UnexpectedPosition => "unexpected-position",
        CampaignActionSubmissionRejectionReason.ActionNotLegal => "action-not-legal",
        _ => "rejected",
    };

    // A source-legal choice the engine has not built yet (FID-002): refused,
    // but distinguishable from an illegal or stale submission.
    internal static CampaignOwnerSubmissionResult RuleNotImplemented() =>
        new(null, RuleNotImplementedCode);

    public bool IsAccepted => canonicalReceipt is not null;
    public string? RejectionCode { get; }
    public byte[]? CanonicalReceipt => canonicalReceipt?.ToArray();
}
