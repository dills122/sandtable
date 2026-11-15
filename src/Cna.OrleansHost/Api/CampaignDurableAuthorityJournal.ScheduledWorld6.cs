using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed record ScheduledWorld6Offer(string Reference, string DestinationId, long ExpectedVersion);

internal sealed partial class CampaignDurableAuthorityJournal
{
    private const string ScheduledWorld6SetupId = "rules-lab.scheduled-world6.v1";
    private const string ScheduledWorld6PreambleKind = "scheduled-world6-preamble";
    private const string ScheduledWorld6NativePreambleKind = "scheduled-world6-preamble-v2";
    private const string ScheduledWorld6ArrivalKind = "scheduled-world6-arrival";
    private const string ScheduledWorld6Subject = "scheduled-world6.axis-infantry";
    private const string ScheduledWorld6Destination = "north-west";
    private const long ScheduledWorld6OpeningRevision = 1;
    private static readonly string ScheduledWorld6VisibleDigest = "sha256:" + Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes("sandtable.scheduled-world6-owner-offer.v1\0axis-infantry\0north-west\07")));

    // Internal synthetic cut only; no general campaign route calls this method.
    internal void CreateScheduledWorld6(CampaignCreationRequest request)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(request);
        if (schemaVersion != SchemaVersion) throw new InvalidOperationException("Current journal schema is required.");
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using (var lookup = connection.CreateCommand())
        {
            lookup.Transaction = transaction;
            lookup.CommandText = "SELECT 1 FROM campaigns WHERE campaign_id=$id";
            lookup.Parameters.AddWithValue("$id", request.CampaignId);
            if (lookup.ExecuteScalar() is not null)
            {
                var retained = LoadScheduledWorld6(connection, transaction, request.CampaignId);
                if (retained.Request != request) throw new InvalidOperationException("Campaign creation conflicts with retained authority.");
                return;
            }
        }
        var created = CampaignScheduledWorld6Bridge.Begin(request);
        var creation = created.EventBytes;
        var initial = created.CheckpointBytes;
        var contacts = EncodeContactRecords([]);
        var timing = Array.Empty<byte>();
        RequireBounded(creation);
        RequireBounded(initial);
        var hash = InitialHash(request, creation, initial, contacts, timing);
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO campaigns VALUES ($id,$version,$rules,$seed,$setup,$setupHash,
                  $pack,$content,$scenario,$creation,$snapshot,$contacts,$timing,0,$head)
                """;
            insert.Parameters.AddWithValue("$id", request.CampaignId);
            insert.Parameters.AddWithValue("$version", request.ContractVersion);
            insert.Parameters.AddWithValue("$rules", request.RulesetHash);
            insert.Parameters.AddWithValue("$seed", request.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            insert.Parameters.AddWithValue("$setup", request.SetupId);
            insert.Parameters.AddWithValue("$setupHash", request.SetupHash);
            insert.Parameters.AddWithValue("$pack", request.ContentPackId);
            insert.Parameters.AddWithValue("$content", request.ContentHash);
            insert.Parameters.AddWithValue("$scenario", request.ScenarioId);
            insert.Parameters.AddWithValue("$creation", creation);
            insert.Parameters.AddWithValue("$snapshot", initial);
            insert.Parameters.AddWithValue("$contacts", contacts);
            insert.Parameters.AddWithValue("$timing", timing);
            insert.Parameters.AddWithValue("$head", hash);
            insert.ExecuteNonQuery();
        }
        var session = created.Session;
        var sequence = 0L;
        for (var index = 0; index < 6; index++)
        {
            var step = CampaignScheduledWorld6Bridge.AdvanceNativePreamble(session);
            hash = PersistScheduledWorld6(connection, transaction, request.CampaignId, sequence,
                hash, ScheduledWorld6NativePreambleKind, null, null, null, null, step);
            sequence++;
            session = step.Session;
        }
        transaction.Commit();
    }

    internal IReadOnlyList<ScheduledWorld6Offer> QueryScheduledWorld6(CampaignSeatCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        var seat = AuthenticateSeat(connection, transaction, credential)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var head = LoadScheduledWorld6(connection, transaction, seat.CampaignId);
        var side = seat.Audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var offers = CampaignScheduledWorld6Bridge.QueryOffers(head.Session, side);
        if (offers.Count == 0) return Array.Empty<ScheduledWorld6Offer>();
        if (offers.Count != 1 || offers[0].ElementId != "axis-infantry" || offers[0].DestinationId != ScheduledWorld6Destination || head.Sequence != 6)
            throw new InvalidOperationException("Unexpected scheduled World6 opening.");
        return Array.AsReadOnly(new[] { new ScheduledWorld6Offer(ScheduledWorld6Reference(connection, transaction, seat.CampaignId, seat.Epoch),
            ScheduledWorld6Destination, 7) });
    }

    internal byte[]? SubmitScheduledWorld6(CampaignSeatCredential credential, string publicReference)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(credential);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var seat = AuthenticateSeat(connection, transaction, credential)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var head = LoadScheduledWorld6(connection, transaction, seat.CampaignId);
        if (seat.Audience != CampaignActionAudience.Axis || !IsPublicReference(publicReference)) return null;
        var expected = ScheduledWorld6Reference(connection, transaction, seat.CampaignId, seat.Epoch);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(publicReference), Encoding.UTF8.GetBytes(expected))) return null;
        if (head.Sequence == 7)
            return head.ArrivalEpoch == seat.Epoch && head.ArrivalReference == publicReference ? head.ArrivalReceipt!.ToArray() : null;
        if (head.Sequence != 6 || CampaignScheduledWorld6Bridge.QueryOffers(head.Session, LandSide.Axis).Count != 1) return null;
        var accepted = CampaignScheduledWorld6Bridge.Arrive(head.Session, publicReference, 7, LandSide.Axis, ScheduledWorld6Destination);
        _ = PersistScheduledWorld6(connection, transaction, seat.CampaignId, head.Sequence, head.Hash,
            ScheduledWorld6ArrivalKind, ScheduledWorld6Subject, publicReference, seat.Epoch, ScheduledWorld6OpeningRevision, accepted);
        transaction.Commit();
        return accepted.ReceiptBytes;
    }

    private static string ScheduledWorld6Reference(SqliteConnection connection, SqliteTransaction transaction, string campaignId, long epoch) =>
        Reference(ReadReferenceKey(connection, transaction), campaignId, "axis", ScheduledWorld6Subject, epoch,
            ScheduledWorld6OpeningRevision, ScheduledWorld6VisibleDigest, 0);

    private static bool IsScheduledWorld6(SqliteConnection connection, SqliteTransaction transaction, string campaignId)
    {
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT setup_id FROM campaigns WHERE campaign_id=$id";
        query.Parameters.AddWithValue("$id", campaignId);
        return query.ExecuteScalar() is string setupId && setupId == ScheduledWorld6SetupId;
    }

    private sealed record ScheduledWorld6Loaded(CampaignCreationRequest Request, CampaignScheduledWorld6Session Session,
        long Sequence, string Hash, string? ArrivalReference, long? ArrivalEpoch, byte[]? ArrivalReceipt);

    private static ScheduledWorld6Loaded LoadScheduledWorld6(SqliteConnection connection, SqliteTransaction transaction, string campaignId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        CampaignCreationRequest request;
        byte[] creation;
        byte[] initial;
        byte[] initialContacts;
        byte[] timing;
        long headSequence;
        string headHash;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = """
                SELECT contract_version,ruleset_hash,seed,setup_id,setup_hash,content_pack_id,
                  content_hash,scenario_id,creation_event,initial_snapshot,initial_contacts,
                  timing_configuration,head_sequence,head_hash
                FROM campaigns WHERE campaign_id=$id
                """;
            query.Parameters.AddWithValue("$id", campaignId);
            using var reader = query.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException("Campaign is missing.");
            request = new CampaignCreationRequest(reader.GetInt32(0), campaignId, reader.GetString(1),
                ulong.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7));
            creation = ReadBounded(reader, 8);
            initial = ReadBounded(reader, 9);
            initialContacts = ReadBounded(reader, 10);
            if (reader.GetBytes(11, 0, null, 0, 0) != 0)
                throw new InvalidOperationException("Scheduled World6 does not admit timing evidence.");
            timing = [];
            headSequence = reader.GetInt64(12);
            headHash = reader.GetString(13);
        }
        var emptyContacts = EncodeContactRecords([]);
        if (request.SetupId != ScheduledWorld6SetupId || timing.Length != 0
            || !initialContacts.AsSpan().SequenceEqual(emptyContacts))
            throw new InvalidOperationException("Invalid scheduled World6 creation metadata.");
        var hash = InitialHash(request, creation, initial, initialContacts, timing);
        var key = ReadReferenceKey(connection, transaction);
        var evidence = new List<CampaignScheduledWorld6Evidence>();
        string? arrivalReference = null;
        long? arrivalEpoch = null;
        byte[]? arrivalReceipt = null;
        var sequence = 0L;
        string? preambleKind = null;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = """
                SELECT sequence,action_kind,acting_element_id,owner_public_ref,seat_epoch,
                  owner_revision,canonical_command,canonical_event,canonical_receipt,
                  snapshot_checkpoint,canonical_contacts,prior_hash,record_hash
                FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence
                """;
            query.Parameters.AddWithValue("$id", campaignId);
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt64(0) != checked(++sequence) || sequence > 7 || reader.GetString(11) != hash)
                    throw new InvalidOperationException("Scheduled World6 journal order changed.");
                var kind = reader.GetString(1);
                var subject = reader.IsDBNull(2) ? null : reader.GetString(2);
                var ownerRef = reader.IsDBNull(3) ? null : reader.GetString(3);
                var epoch = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4);
                var revision = reader.IsDBNull(5) ? (long?)null : reader.GetInt64(5);
                var command = ReadBounded(reader, 6);
                var eventBytes = ReadBounded(reader, 7);
                var receipt = ReadBounded(reader, 8);
                var checkpoint = ReadBounded(reader, 9);
                var contacts = ReadBounded(reader, 10);
                if (!contacts.AsSpan().SequenceEqual(emptyContacts))
                    throw new InvalidOperationException("Scheduled World6 contact evidence changed.");
                if (sequence <= 6)
                {
                    preambleKind ??= kind;
                    if (kind != preambleKind || kind is not (ScheduledWorld6PreambleKind or ScheduledWorld6NativePreambleKind)
                        || CampaignScheduledWorld6Bridge.IsNativePreambleEvent(eventBytes) != (kind == ScheduledWorld6NativePreambleKind)
                        || subject is not null || ownerRef is not null || epoch is not null || revision is not null)
                        throw new InvalidOperationException("Invalid scheduled World6 preamble metadata.");
                }
                else
                {
                    if (kind != ScheduledWorld6ArrivalKind || subject != ScheduledWorld6Subject || ownerRef is null
                        || epoch is null or < 1 || revision != ScheduledWorld6OpeningRevision
                        || ownerRef != Reference(key, campaignId, "axis", ScheduledWorld6Subject, epoch.Value,
                            ScheduledWorld6OpeningRevision, ScheduledWorld6VisibleDigest, 0)
                        || !CampaignScheduledWorld6Bridge.IsArrivalEventBound(eventBytes, campaignId, ownerRef))
                        throw new InvalidOperationException("Invalid scheduled World6 arrival metadata.");
                    arrivalReference = ownerRef;
                    arrivalEpoch = epoch;
                    arrivalReceipt = receipt;
                }
                hash = StepHash(hash, sequence, kind, subject, ownerRef, epoch, revision,
                    command, eventBytes, receipt, checkpoint, contacts);
                if (reader.GetString(12) != hash) throw new InvalidOperationException("Scheduled World6 record changed.");
                evidence.Add(new CampaignScheduledWorld6Evidence(command, eventBytes, receipt, checkpoint));
            }
        }
        if (sequence is < 6 or > 7 || sequence != headSequence || hash != headHash)
            throw new InvalidOperationException("Scheduled World6 journal head changed.");
        var session = CampaignScheduledWorld6Bridge.Restore(request, creation, initial, evidence);
        var axisOffers = CampaignScheduledWorld6Bridge.QueryOffers(session, LandSide.Axis);
        if (axisOffers.Count != (sequence == 6 ? 1 : 0)
            || CampaignScheduledWorld6Bridge.QueryOffers(session, LandSide.Commonwealth).Count != 0)
            throw new InvalidOperationException("Scheduled World6 replay produced an invalid opening.");
        return new ScheduledWorld6Loaded(request, session, sequence, hash, arrivalReference, arrivalEpoch, arrivalReceipt);
    }

    private static string PersistScheduledWorld6(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId, long priorSequence, string priorHash, string kind, string? subject,
        string? ownerRef, long? epoch, long? revision, CampaignScheduledWorld6Step step)
    {
        if (step.IsRetry) throw new InvalidOperationException("A retry cannot append a journal record.");
        var command = step.CommandBytes;
        var eventBytes = step.EventBytes;
        var receipt = step.ReceiptBytes;
        var checkpoint = step.CheckpointBytes;
        var contacts = EncodeContactRecords([]);
        foreach (var bytes in new[] { command, eventBytes, receipt, checkpoint }) RequireBounded(bytes);
        var sequence = checked(priorSequence + 1);
        var hash = StepHash(priorHash, sequence, kind, subject, ownerRef, epoch, revision,
            command, eventBytes, receipt, checkpoint, contacts);
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO accepted_steps VALUES ($id,$seq,$kind,$element,$ref,$epoch,$revision,
                  $command,$event,$receipt,$checkpoint,$contacts,$prior,$hash)
                """;
            insert.Parameters.AddWithValue("$id", campaignId);
            insert.Parameters.AddWithValue("$seq", sequence);
            insert.Parameters.AddWithValue("$kind", kind);
            insert.Parameters.AddWithValue("$element", (object?)subject ?? DBNull.Value);
            insert.Parameters.AddWithValue("$ref", (object?)ownerRef ?? DBNull.Value);
            insert.Parameters.AddWithValue("$epoch", (object?)epoch ?? DBNull.Value);
            insert.Parameters.AddWithValue("$revision", (object?)revision ?? DBNull.Value);
            insert.Parameters.AddWithValue("$command", command);
            insert.Parameters.AddWithValue("$event", eventBytes);
            insert.Parameters.AddWithValue("$receipt", receipt);
            insert.Parameters.AddWithValue("$checkpoint", checkpoint);
            insert.Parameters.AddWithValue("$contacts", contacts);
            insert.Parameters.AddWithValue("$prior", priorHash);
            insert.Parameters.AddWithValue("$hash", hash);
            insert.ExecuteNonQuery();
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE campaigns SET head_sequence=$seq,head_hash=$hash
            WHERE campaign_id=$id AND head_sequence=$priorSeq AND head_hash=$priorHash
            """;
        update.Parameters.AddWithValue("$id", campaignId);
        update.Parameters.AddWithValue("$seq", sequence);
        update.Parameters.AddWithValue("$hash", hash);
        update.Parameters.AddWithValue("$priorSeq", priorSequence);
        update.Parameters.AddWithValue("$priorHash", priorHash);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Scheduled World6 head changed before commit.");
        return hash;
    }
}
