using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
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

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaMove(CampaignSeatCredential credential,
        string defenderElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, defenderElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.RetreatBeforeAssault, retreatMove: true);

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
        CampaignOwnerOpeningKind kind, bool retreatMove = false)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(credential);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var seat = AuthenticateSeat(connection, transaction, credential, out var verifiedHead)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var head = verifiedHead ?? Load(connection, transaction, seat.CampaignId);
        if (kind == CampaignOwnerOpeningKind.ForceAssignment && head.Timing is null)
            return CampaignOwnerSubmissionResult.Rejected();
        if (string.IsNullOrWhiteSpace(actingElementId) || actingElementId.Length > 128
            || !IsPublicReference(publicActionRef)
            || openingRevision < 1)
            return CampaignOwnerSubmissionResult.Rejected();

        using (var retry = connection.CreateCommand())
        {
            retry.Transaction = transaction;
            retry.CommandText = """
                SELECT canonical_receipt FROM accepted_steps
                WHERE campaign_id=$id AND action_kind=$kind
                  AND acting_element_id=$element AND owner_public_ref=$ref
                  AND seat_epoch=$epoch AND owner_revision=$revision
                """;
            retry.Parameters.AddWithValue("$id", seat.CampaignId);
            retry.Parameters.AddWithValue("$kind", AcceptedKind(kind, retreatMove));
            retry.Parameters.AddWithValue("$element", actingElementId);
            retry.Parameters.AddWithValue("$ref", publicActionRef);
            retry.Parameters.AddWithValue("$epoch", seat.Epoch);
            retry.Parameters.AddWithValue("$revision", openingRevision);
            if (retry.ExecuteScalar() is byte[] retainedReceipt)
            {
                RequireBounded(retainedReceipt);
                return CampaignOwnerSubmissionResult.Accepted(retainedReceipt);
            }
        }

        ForceRound? forceRound = null;
        if (kind == CampaignOwnerOpeningKind.ForceAssignment)
        {
            if (schemaVersion != SchemaVersion) return CampaignOwnerSubmissionResult.Rejected();
            (string DecisionActionId, string DeclineEventHash) binding;
            try
            {
                binding = CampaignHostAdapter.QueryNativeCombatForceRoundBinding(head.Session);
            }
            catch (InvalidOperationException)
            {
                return CampaignOwnerSubmissionResult.Rejected();
            }
            if (!ForceRoundExists(connection, transaction, seat.CampaignId,
                binding.DeclineEventHash))
                return CampaignOwnerSubmissionResult.Rejected();
            forceRound = ReadActiveForceRound(connection, transaction, head,
                seat.CampaignId);
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
            if (!reader.Read()) return CampaignOwnerSubmissionResult.Rejected();
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
            return CampaignOwnerSubmissionResult.Rejected();
        byte[] currentVisible;
        string[] actionIds;
        try
        {
            if (kind == CampaignOwnerOpeningKind.PregameAxisSupply)
            {
                var draft = CampaignHostAdapter.QueryPregameAxisSupplyOwnerDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PregameAxisSupplyFinish)
            {
                var draft = CampaignHostAdapter.QueryPregameAxisSupplyFinishDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisDump)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisDumpDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCommonwealthSupply)
            {
                var draft = CampaignHostAdapter.QuerySourceCommonwealthSupplyDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceDummy)
            {
                var draft = CampaignHostAdapter.QuerySourceDummyDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceMotorization)
            {
                var draft = CampaignHostAdapter.QuerySourceMotorizationDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceUnattachedCargo)
            {
                var draft = CampaignHostAdapter.QuerySourceUnattachedCargoDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceActions)
            {
                var draft = CampaignHostAdapter.QuerySourceActionsDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisGt1Supply)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisGt1SupplyDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan)
            {
                var draft = CampaignHostAdapter.QuerySourceCommonwealthSupplyPlanDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAutoblinda)
            {
                var draft = CampaignHostAdapter.QuerySourceAutoblindaDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourcePriorityMotorization)
            {
                var draft = CampaignHostAdapter.QuerySourcePriorityMotorizationDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAttachedInitialLoads)
            {
                var draft = CampaignHostAdapter.QuerySourceAttachedInitialLoadsDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceInitialWrecks)
            {
                var draft = CampaignHostAdapter.QuerySourceInitialWrecksDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisFreeSetup)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisFreeSetupDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisCorpsSetup)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisCorpsSetupDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceMalettiHierarchy)
            {
                var draft = CampaignHostAdapter.QuerySourceMalettiHierarchyDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceGt2Correction)
            {
                var draft = CampaignHostAdapter.QuerySourceGt2CorrectionDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCwPriorityCatalog)
            {
                var draft = CampaignHostAdapter.QuerySourceCwPriorityCatalogDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceMatruhFormation)
            {
                var draft = CampaignHostAdapter.QuerySourceMatruhFormationDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCwFreeGuns)
            {
                var draft = CampaignHostAdapter.QuerySourceCwFreeGunsDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisPriority)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisPriorityDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCairoSetup)
            {
                var draft = CampaignHostAdapter.QuerySourceCairoSetupDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.CombatSelection)
            {
                var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(head.Session,
                    seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ForceAssignment)
            {
                var draft = CampaignHostAdapter.QueryContactForceOwnerDraft(head.Session,
                    seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
            {
                var draft = CampaignHostAdapter.QueryContactRbaOwnerDraft(head.Session,
                    seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
            {
                var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(
                    head.Session, seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.Custody)
            {
                var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(
                    head.Session, seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ReserveRelease)
            {
                var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(
                    head.Session, seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.CycleControl)
            {
                var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(
                    head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.TruckConvoyPass)
            {
                var draft = CampaignHostAdapter.QueryTruckConvoyPassOwnerDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PostCombatPass)
            {
                var draft = CampaignHostAdapter.QueryPostCombatPassOwnerDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SecondReserve)
            {
                var draft = CampaignHostAdapter.QuerySecondReserveOwnerDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SecondMovement)
            {
                var draft = CampaignHostAdapter.QuerySecondMovementOwnerDraft(head.Session, seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SecondMovementCompletion)
            {
                var draft = CampaignHostAdapter.QuerySecondMovementCompletionOwnerDraft(head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RepeatMovement)
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(
                    head.Session, seat.Audience);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RepeatCombatSelection)
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(
                    head.Session, seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else
            {
                var draft = CampaignHostAdapter.QueryContactOwnerDraft(head.Session,
                    seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateMovementActions.OrderBy(value => value.OriginLocationId,
                    StringComparer.Ordinal).ThenBy(value => value.DestinationLocationId,
                    StringComparer.Ordinal).Select(value => value.ActionId).ToArray();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return CampaignOwnerSubmissionResult.Rejected();
        }
        if (!currentVisible.AsSpan().SequenceEqual(visibleBytes))
            return CampaignOwnerSubmissionResult.Rejected();
        if (actionIds.Length != actionCount) return CampaignOwnerSubmissionResult.Rejected();

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
            if (!reader.Read()) return CampaignOwnerSubmissionResult.Rejected();
            ordinal = reader.GetInt32(0);
            coreActionId = reader.GetString(1);
        }
        if (ordinal < 0 || ordinal >= actionIds.Length)
            return CampaignOwnerSubmissionResult.Rejected();
        if (kind == CampaignOwnerOpeningKind.SourceActions)
        {
            if (!CampaignHostAdapter.MatchesIssuedSourceAction(authorityVersion, visibleBytes, ordinal, coreActionId))
                return CampaignOwnerSubmissionResult.Rejected();
            // A hidden opponent decision can advance the source version without changing this
            // authenticated visible offer. Submit its freshly revalidated current identity.
            coreActionId = actionIds[ordinal];
        }
        else if (coreActionId != actionIds[ordinal])
            return CampaignOwnerSubmissionResult.Rejected();
        var expectedRef = Reference(ReadReferenceKey(connection, transaction), seat.CampaignId,
            Side(seat.Audience), actingElementId, seat.Epoch, openingRevision,
            visibleDigest, ordinal);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedRef),
                Encoding.UTF8.GetBytes(publicActionRef)))
            return CampaignOwnerSubmissionResult.Rejected();

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
                return CampaignOwnerSubmissionResult.Rejected();
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
                retreatMove
                    ? CampaignHostAdapter.SubmitContactDefenderRbaMove(head.Session,
                        submission, actingElementId)
                    : CampaignHostAdapter.SubmitContactDefenderRbaDecline(head.Session,
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
        if (!result.IsAccepted) return CampaignOwnerSubmissionResult.Rejected();
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
                return CampaignOwnerSubmissionResult.Rejected();
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
                return CampaignOwnerSubmissionResult.Rejected();
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
                return CampaignOwnerSubmissionResult.Rejected();
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
                return CampaignOwnerSubmissionResult.Rejected();
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
                return CampaignOwnerSubmissionResult.Rejected();
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
                return CampaignOwnerSubmissionResult.Rejected();
            }
        }
        var acceptedHead = PersistAccepted(connection, transaction, seat.CampaignId, head,
            AcceptedKind(kind, retreatMove), actingElementId,
            publicActionRef, seat.Epoch, openingRevision, submission, result);
        if (selectionRound is not null)
            UpdateSelectionRound(connection, transaction, selectionRound with { Status = "completed" });
        if (kind == CampaignOwnerOpeningKind.Movement)
            TryInitializeSelectionRound(connection, transaction, acceptedHead, seat.CampaignId);
        if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault && !retreatMove)
        {
            UpdateRbaRound(connection, transaction, rbaRound! with { Status = "completed" });
            InitializeForceRound(connection, transaction, acceptedHead, seat.CampaignId,
                rbaRound.HighWaterMs);
        }
        if (kind == CampaignOwnerOpeningKind.ReserveRelease
            && CampaignHostAdapter.IsNativeReserveReleaseEmpty(acceptedHead.Session))
            UpdateReserveRound(connection, transaction, reserveRound! with { Status = "completed" });
        if (kind == CampaignOwnerOpeningKind.CycleControl)
            UpdateCycleRound(connection, transaction, cycleRound! with { Status = "completed" });
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
                PersistAccepted(connection, transaction, seat.CampaignId, acceptedHead,
                    CampaignHostAcceptedStep.NativeSecondBreakdownKind, null,
                    null, null, null, command, breakdown);
            }
        }
        if (kind == CampaignOwnerOpeningKind.Custody)
        {
            UpdateCustodyRound(connection, transaction, custodyRound! with { Status = "completed" });
            AppendNativeCombatSuccessor(connection, transaction, seat.CampaignId, acceptedHead);
        }
        if (kind == CampaignOwnerOpeningKind.ReserveRelease)
            AppendEmptyReserveRelease(connection, transaction, seat.CampaignId, acceptedHead);
        if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
        {
            UpdateRetreatRound(connection, transaction, retreatRound! with { Status = "completed" });
            AppendWorldSettlement(connection, transaction, seat.CampaignId, acceptedHead);
        }
        if (kind == CampaignOwnerOpeningKind.CombatSelection
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
        else if (kind == CampaignOwnerOpeningKind.CombatSelection
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

    private static string AcceptedKind(CampaignOwnerOpeningKind kind,
        bool retreatMove = false) => kind switch
        {
            CampaignOwnerOpeningKind.Movement => CampaignHostAcceptedStep.ContactMovementKind,
            CampaignOwnerOpeningKind.CombatSelection => CampaignHostAcceptedStep.ContactCombatSelectionKind,
            CampaignOwnerOpeningKind.RetreatBeforeAssault => retreatMove
                ? CampaignHostAcceptedStep.ContactRbaMoveKind
                : CampaignHostAcceptedStep.ContactRbaDeclineKind,
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

    private CampaignOwnerSubmissionResult(byte[]? canonicalReceipt) =>
        this.canonicalReceipt = canonicalReceipt?.ToArray();

    internal static CampaignOwnerSubmissionResult Accepted(byte[] canonicalReceipt) =>
        new(canonicalReceipt);

    internal static CampaignOwnerSubmissionResult Rejected() => new(null);

    public bool IsAccepted => canonicalReceipt is not null;
    public byte[]? CanonicalReceipt => canonicalReceipt?.ToArray();
}
