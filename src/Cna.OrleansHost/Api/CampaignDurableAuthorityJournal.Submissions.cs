using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

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

    internal CampaignOwnerSubmissionResult SubmitOwnerCombatSelection(CampaignSeatCredential credential,
        string actingElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, actingElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.CombatSelection);

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaDecline(CampaignSeatCredential credential,
        string defenderElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, defenderElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.RetreatBeforeAssault);

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaFirstMove(CampaignSeatCredential credential,
        string defenderElementId, long openingRevision, string publicActionRef) =>
        SubmitOwnerActionCore(credential, defenderElementId, openingRevision, publicActionRef,
            CampaignOwnerOpeningKind.RetreatBeforeAssault, retreatMove: true);

    private CampaignOwnerSubmissionResult SubmitOwnerActionCore(CampaignSeatCredential credential,
        string actingElementId, long openingRevision, string publicActionRef,
        CampaignOwnerOpeningKind kind, bool retreatMove = false)
    {
        ArgumentNullException.ThrowIfNull(credential);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var seat = AuthenticateSeat(connection, transaction, credential)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var head = Load(connection, transaction, seat.CampaignId);
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
            if (kind == CampaignOwnerOpeningKind.CombatSelection)
            {
                var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(head.Session,
                    seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
            {
                var draft = CampaignHostAdapter.QueryContactRbaOwnerDraft(head.Session,
                    seat.Audience, actingElementId);
                currentVisible = draft.VisibleBytes;
                actionIds = draft.PrivateActionIds.ToArray();
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
        if (ordinal < 0 || ordinal >= actionIds.Length
            || coreActionId != actionIds[ordinal])
            return CampaignOwnerSubmissionResult.Rejected();
        var expectedRef = Reference(ReadReferenceKey(connection, transaction), seat.CampaignId,
            Side(seat.Audience), actingElementId, seat.Epoch, openingRevision,
            visibleDigest, ordinal);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedRef),
                Encoding.UTF8.GetBytes(publicActionRef)))
            return CampaignOwnerSubmissionResult.Rejected();

        var checkpoint = CampaignExercises.QueryCheckpoint(head.Session);
        var submission = new CampaignActionSubmission(1, seat.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, seat.Audience, coreActionId);
        var result = kind switch
        {
            CampaignOwnerOpeningKind.CombatSelection =>
                CampaignHostAdapter.SubmitContactOwnerCombatSelection(head.Session,
                    submission, actingElementId),
            CampaignOwnerOpeningKind.RetreatBeforeAssault =>
                retreatMove
                    ? CampaignHostAdapter.SubmitContactDefenderRbaFirstMove(head.Session,
                        submission, actingElementId)
                    : CampaignHostAdapter.SubmitContactDefenderRbaDecline(head.Session,
                        submission, actingElementId),
            _ => CampaignHostAdapter.SubmitContactOwnerMovement(head.Session,
                submission, actingElementId),
        };
        if (!result.IsAccepted) return CampaignOwnerSubmissionResult.Rejected();
        PersistAccepted(connection, transaction, seat.CampaignId, head,
            AcceptedKind(kind, retreatMove), actingElementId,
            publicActionRef, seat.Epoch, openingRevision, submission, result);
        if (kind == CampaignOwnerOpeningKind.CombatSelection
            && CampaignHostAdapter.IsNativeCombatVoluntaryFinish(result.SuccessorSession!))
        {
            var noAttack = CampaignHostAdapter.CompleteNativeCombatNoAttack(result.SuccessorSession!);
            if (!noAttack.IsAccepted)
                throw new InvalidOperationException("Accepted voluntary finish cannot complete Combat.");
            var systemHead = Load(connection, transaction, seat.CampaignId);
            var systemCommand = CampaignHostAdapter.NativeCombatNoAttackCommand(
                systemHead.Session, noAttack);
            PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeCombatNoAttackKind, null,
                null, null, null, systemCommand, noAttack);
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
            PersistAccepted(connection, transaction, seat.CampaignId, systemHead,
                CampaignHostAcceptedStep.NativeCombatBarrageKind, null,
                null, null, null, systemCommand, barrage);
        }
        transaction.Commit();
        return CampaignOwnerSubmissionResult.Accepted(
            CampaignActionAcceptanceReceiptSerializer.Serialize(result.Evidence!.Receipt));
    }

    private static string AcceptedKind(CampaignOwnerOpeningKind kind,
        bool retreatMove = false) => kind switch
        {
            CampaignOwnerOpeningKind.Movement => CampaignHostAcceptedStep.ContactMovementKind,
            CampaignOwnerOpeningKind.CombatSelection => CampaignHostAcceptedStep.ContactCombatSelectionKind,
            CampaignOwnerOpeningKind.RetreatBeforeAssault => retreatMove
                ? CampaignHostAcceptedStep.ContactRbaMoveKind
                : CampaignHostAcceptedStep.ContactRbaDeclineKind,
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
