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
        string actingElementId, long openingRevision, string publicActionRef)
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
            retry.Parameters.AddWithValue("$kind", CampaignHostAcceptedStep.ContactMovementKind);
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
        CampaignHostContactOwnerDraft draft;
        try
        {
            draft = CampaignHostAdapter.QueryContactOwnerDraft(head.Session,
                seat.Audience, actingElementId);
        }
        catch (ArgumentException)
        {
            return CampaignOwnerSubmissionResult.Rejected();
        }
        if (!draft.VisibleBytes.AsSpan().SequenceEqual(visibleBytes))
            return CampaignOwnerSubmissionResult.Rejected();
        var actions = draft.PrivateMovementActions.OrderBy(value => value.OriginLocationId,
            StringComparer.Ordinal).ThenBy(value => value.DestinationLocationId,
            StringComparer.Ordinal).ToArray();
        if (actions.Length != actionCount) return CampaignOwnerSubmissionResult.Rejected();

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
        if (ordinal < 0 || ordinal >= actions.Length
            || coreActionId != actions[ordinal].ActionId)
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
        var result = CampaignHostAdapter.SubmitContactOwnerMovement(head.Session,
            submission, actingElementId);
        if (!result.IsAccepted) return CampaignOwnerSubmissionResult.Rejected();
        PersistAccepted(connection, transaction, seat.CampaignId, head,
            CampaignHostAcceptedStep.ContactMovementKind, actingElementId,
            publicActionRef, seat.Epoch, openingRevision, submission, result);
        transaction.Commit();
        return CampaignOwnerSubmissionResult.Accepted(
            CampaignActionAcceptanceReceiptSerializer.Serialize(result.Evidence!.Receipt));
    }

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
