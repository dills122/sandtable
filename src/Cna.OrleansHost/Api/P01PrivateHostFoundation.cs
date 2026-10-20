using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

// Disabled private storage primitive. No service registration or HTTP route creates this store.
// Probe records are never P01 owner inputs, game events, receipts or admitted actions.
internal sealed class P01PrivateHostFoundation
{
    private const int ApplicationId = 0x50303146; // P01F
    private const int SchemaVersion = 1;
    private const int MaxProbeBytes = 65_536;
    private static readonly TimeSpan GrantLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CredentialLifetime = TimeSpan.FromHours(2);
    private static readonly Regex CampaignPattern = new(@"\A[a-z0-9][a-z0-9._-]{0,127}\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex HashPattern = new(@"\Asha256:[0-9a-f]{64}\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly (string Table, string Sql)[] TableDefinitions =
    [
        ("operator_capability", """
            CREATE TABLE operator_capability (
              singleton INTEGER PRIMARY KEY CHECK(singleton=1), secret_digest BLOB NOT NULL
            )
            """),
        ("campaigns", """
            CREATE TABLE campaigns (
              campaign_id TEXT PRIMARY KEY, checkpoint_hash TEXT NOT NULL,
              head_sequence INTEGER NOT NULL, head_hash TEXT NOT NULL,
              activation_fence INTEGER NOT NULL, activation_digest BLOB NOT NULL
            )
            """),
        ("seats", """
            CREATE TABLE seats (
              campaign_id TEXT NOT NULL, side TEXT NOT NULL, authority_id TEXT NOT NULL UNIQUE,
              grant_generation INTEGER NOT NULL, seat_epoch INTEGER NOT NULL,
              grant_digest BLOB, grant_expiry_ms INTEGER,
              credential_digest BLOB, credential_expiry_ms INTEGER,
              revoked INTEGER NOT NULL,
              PRIMARY KEY(campaign_id,side),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """),
        ("probe_records", """
            CREATE TABLE probe_records (
              campaign_id TEXT NOT NULL, sequence INTEGER NOT NULL, prior_hash TEXT NOT NULL,
              record_hash TEXT NOT NULL, record_bytes BLOB NOT NULL,
              authority_id TEXT NOT NULL, seat_epoch INTEGER NOT NULL, request_id TEXT NOT NULL,
              PRIMARY KEY(campaign_id,sequence),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """),
        ("probe_requests", """
            CREATE TABLE probe_requests (
              campaign_id TEXT NOT NULL, authority_id TEXT NOT NULL, request_id TEXT NOT NULL,
              input_hash TEXT NOT NULL, record_hash TEXT NOT NULL, sequence INTEGER NOT NULL,
              PRIMARY KEY(campaign_id,authority_id,request_id),
              FOREIGN KEY(campaign_id,sequence) REFERENCES probe_records(campaign_id,sequence)
            )
            """),
    ];
    private readonly string path;
    private readonly TimeProvider clock;

    private P01PrivateHostFoundation(string path, TimeProvider clock)
    {
        this.path = path;
        this.clock = clock;
    }

    internal static P01PrivateHostFoundation CreateNew(string absolutePath,
        string operatorSecret, TimeProvider? clock = null)
    {
        ValidatePath(absolutePath);
        var secret = ParseSecret(operatorSecret);
        var directory = Path.GetDirectoryName(absolutePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(directory, UnixFileMode.UserRead
                    | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        RequirePrivateDirectory(directory);
        if (File.Exists(absolutePath)) throw new InvalidOperationException("Store already exists.");
        var store = new P01PrivateHostFoundation(absolutePath, clock ?? TimeProvider.System);
        using var connection = store.Connect(create: true);
        Execute(connection, "PRAGMA journal_mode=WAL");
        Execute(connection, "PRAGMA synchronous=FULL");
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            foreach (var (_, sql) in TableDefinitions) Execute(connection, sql, transaction);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO operator_capability VALUES (1, $digest)";
            command.Parameters.AddWithValue("$digest", Digest("operator", secret));
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        Execute(connection, $"PRAGMA application_id={ApplicationId}");
        Execute(connection, $"PRAGMA user_version={SchemaVersion}");
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(absolutePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return store;
    }

    internal static P01PrivateHostFoundation OpenExisting(string absolutePath,
        TimeProvider? clock = null)
    {
        ValidatePath(absolutePath);
        if (!File.Exists(absolutePath)) throw new InvalidOperationException("Store is missing.");
        RequirePrivateDirectory(Path.GetDirectoryName(absolutePath)!);
        if (!OperatingSystem.IsWindows())
        {
            var mode = File.GetUnixFileMode(absolutePath);
            if ((mode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead
                    | UnixFileMode.OtherWrite)) != 0)
                throw new InvalidOperationException("Store file permissions are not private.");
        }
        var store = new P01PrivateHostFoundation(absolutePath, clock ?? TimeProvider.System);
        using var connection = store.Connect(create: false);
        if (ScalarLong(connection, "PRAGMA application_id") != ApplicationId
            || ScalarLong(connection, "PRAGMA user_version") != SchemaVersion
            || ScalarString(connection, "PRAGMA journal_mode") != "wal"
            || ScalarString(connection, "PRAGMA integrity_check") != "ok")
            throw new InvalidOperationException("Unrecognized or damaged private store.");
        RequireSchema(connection);
        using (var check = Command(connection, null, "PRAGMA foreign_key_check"))
        using (var violations = check.ExecuteReader())
            if (violations.Read())
                throw new InvalidOperationException("Private store foreign key violation.");
        return store;
    }

    // Test-only bootstrap slot. A future adapter must require a Core-validated A1 checkpoint.
    internal void SeedUnissuableCampaign(string campaignId, string checkpointHash,
        string operatorSecret)
    {
        ValidateCampaign(campaignId);
        ValidateHash(checkpointHash);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var headHash = HashText("campaign-binding", campaignId, checkpointHash);
        using var command = Command(connection, transaction,
            "INSERT INTO campaigns VALUES ($campaign, $checkpoint, 0, $head, 0, zeroblob(32))");
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$checkpoint", checkpointHash);
        command.Parameters.AddWithValue("$head", headHash);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    internal P01ActivationLease AcquireActivationFence(string campaignId, string operatorSecret)
    {
        ValidateCampaign(campaignId);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var secret = NewSecret();
        var nextOperatorSecret = NewSecret();
        using var command = Command(connection, transaction,
            """
            UPDATE campaigns SET activation_fence=activation_fence+1,
                activation_digest=$digest
            WHERE campaign_id=$campaign RETURNING activation_fence
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$digest", ActivationDigest(campaignId, secret));
        var fence = command.ExecuteScalar() as long?
            ?? throw new InvalidOperationException("Unknown campaign.");
        using (var rotate = Command(connection, transaction,
            "UPDATE operator_capability SET secret_digest=$digest WHERE singleton=1"))
        {
            rotate.Parameters.AddWithValue("$digest", Digest("operator", ParseSecret(nextOperatorSecret)));
            if (rotate.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Missing operator capability.");
        }
        transaction.Commit();
        return new P01ActivationLease(campaignId, fence, secret, nextOperatorSecret);
    }

    internal P01PrivateGrant IssueGrant(string campaignId, string side, P01ActivationLease lease,
        string operatorSecret)
    {
        ValidateCampaign(campaignId);
        ValidateSide(side);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        RequireLease(connection, transaction, campaignId, lease);
        var authorityId = Guid.NewGuid().ToString("D");
        var secret = NewSecret();
        var expiry = clock.GetUtcNow().Add(GrantLifetime).ToUnixTimeMilliseconds();
        using var command = Command(connection, transaction, """
            INSERT INTO seats (campaign_id,side,authority_id,grant_generation,seat_epoch,
                grant_digest,grant_expiry_ms,credential_digest,credential_expiry_ms,revoked)
            VALUES ($campaign,$side,$authority,1,0,$digest,$expiry,NULL,NULL,0)
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$side", side);
        command.Parameters.AddWithValue("$authority", authorityId);
        command.Parameters.AddWithValue("$digest", SeatDigest("grant", campaignId, side,
            authorityId, 1, secret));
        command.Parameters.AddWithValue("$expiry", expiry);
        command.ExecuteNonQuery();
        transaction.Commit();
        return new P01PrivateGrant(campaignId, side, authorityId, secret, expiry);
    }

    // Operator-only lost-response recovery. A new authority and epoch invalidate both the
    // consumed grant and any replacement credential whose response was not delivered.
    internal P01PrivateGrant RecoverGrant(string campaignId, string side,
        P01ActivationLease lease, string operatorSecret)
    {
        ValidateCampaign(campaignId);
        ValidateSide(side);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        RequireLease(connection, transaction, campaignId, lease);
        var authorityId = Guid.NewGuid().ToString("D");
        var secret = NewSecret();
        var expiry = clock.GetUtcNow().Add(GrantLifetime).ToUnixTimeMilliseconds();
        using var command = Command(connection, transaction, """
            UPDATE seats SET authority_id=$authority, grant_generation=grant_generation+1,
                seat_epoch=seat_epoch+1, grant_digest=$digest, grant_expiry_ms=$expiry,
                credential_digest=NULL, credential_expiry_ms=NULL
            WHERE campaign_id=$campaign AND side=$side AND revoked=0
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$side", side);
        command.Parameters.AddWithValue("$authority", authorityId);
        // The stored generation advances so every recovery has an independent digest domain.
        var seat = ReadSeat(connection, transaction, campaignId, side)
            ?? throw new InvalidOperationException("Unknown seat.");
        if (seat.Revoked) throw new InvalidOperationException("Revoked seat cannot be recovered.");
        command.Parameters.AddWithValue("$digest", SeatDigest("grant", campaignId, side,
            authorityId, checked(seat.GrantGeneration + 1), secret));
        command.Parameters.AddWithValue("$expiry", expiry);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Seat recovery failed.");
        transaction.Commit();
        return new P01PrivateGrant(campaignId, side, authorityId, secret, expiry);
    }

    internal P01PrivateCredential? RedeemGrant(P01PrivateGrant grant, P01ActivationLease lease)
    {
        ValidateCampaign(grant.CampaignId);
        ValidateSide(grant.Side);
        ValidateUuid(grant.AuthorityId);
        ParseSecret(grant.Secret);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireLease(connection, transaction, grant.CampaignId, lease);
        var seat = ReadSeat(connection, transaction, grant.CampaignId, grant.Side);
        if (seat is null || seat.Value.Revoked || seat.Value.AuthorityId != grant.AuthorityId
            || seat.Value.GrantDigest is null || seat.Value.GrantExpiryMs <= NowMs()
            || !CryptographicOperations.FixedTimeEquals(seat.Value.GrantDigest,
                SeatDigest("grant", grant.CampaignId, grant.Side, grant.AuthorityId,
                    seat.Value.GrantGeneration, grant.Secret)))
            return null;
        var replacement = NewSecret();
        var expiry = clock.GetUtcNow().Add(CredentialLifetime).ToUnixTimeMilliseconds();
        using var command = Command(connection, transaction, """
            UPDATE seats SET grant_digest=NULL, grant_expiry_ms=NULL,
                credential_digest=$digest, credential_expiry_ms=$expiry
            WHERE campaign_id=$campaign AND side=$side
            """);
        command.Parameters.AddWithValue("$campaign", grant.CampaignId);
        command.Parameters.AddWithValue("$side", grant.Side);
        command.Parameters.AddWithValue("$digest", SeatDigest("credential", grant.CampaignId,
            grant.Side, grant.AuthorityId, seat.Value.GrantGeneration, replacement));
        command.Parameters.AddWithValue("$expiry", expiry);
        command.ExecuteNonQuery();
        transaction.Commit();
        return new P01PrivateCredential(grant.CampaignId, grant.Side, grant.AuthorityId,
            replacement, seat.Value.SeatEpoch, expiry);
    }

    internal P01PrivateSeatFacts? Authenticate(P01PrivateCredential credential)
    {
        ValidateCampaign(credential.CampaignId);
        ValidateSide(credential.Side);
        ValidateUuid(credential.AuthorityId);
        ParseSecret(credential.Secret);
        using var connection = Connect(create: false);
        var seat = ReadSeat(connection, null, credential.CampaignId, credential.Side);
        return ValidCredential(seat, credential) ? new P01PrivateSeatFacts(
            credential.CampaignId, credential.Side, credential.AuthorityId,
            seat!.Value.SeatEpoch, Guid.NewGuid().ToString("D")) : null;
    }

    internal bool Revoke(string campaignId, string side, P01ActivationLease lease,
        string operatorSecret)
    {
        ValidateCampaign(campaignId);
        ValidateSide(side);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        RequireLease(connection, transaction, campaignId, lease);
        using var command = Command(connection, transaction, """
            UPDATE seats SET seat_epoch=seat_epoch+1, revoked=1,
                grant_digest=NULL, grant_expiry_ms=NULL,
                credential_digest=NULL, credential_expiry_ms=NULL
            WHERE campaign_id=$campaign AND side=$side AND revoked=0
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$side", side);
        var changed = command.ExecuteNonQuery() == 1;
        transaction.Commit();
        return changed;
    }

    internal P01ProbeResult AppendProbe(P01PrivateCredential credential, string requestId,
        byte[] recordBytes, long expectedSequence, string expectedHeadHash,
        P01ActivationLease lease)
    {
        ValidateUuid(requestId);
        ValidateHash(expectedHeadHash);
        if (recordBytes.Length is < 1 or > MaxProbeBytes)
            throw new ArgumentOutOfRangeException(nameof(recordBytes));
        ValidateCampaign(credential.CampaignId);
        ValidateSide(credential.Side);
        ValidateUuid(credential.AuthorityId);
        ParseSecret(credential.Secret);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        if (!ValidLease(connection, transaction, credential.CampaignId, lease))
            return new(P01ProbeStatus.Stale, null);
        var seat = ReadSeat(connection, transaction, credential.CampaignId, credential.Side);
        if (!ValidCredential(seat, credential)) return new(P01ProbeStatus.Unauthorized, null);
        var inputHash = HashBytes("probe-input", recordBytes);
        var previous = ReadRequest(connection, transaction, credential.CampaignId,
            credential.AuthorityId, requestId);
        if (previous is not null)
        {
            if (previous.Value.InputHash != inputHash)
                return new(P01ProbeStatus.Conflict, null);
            var retained = ReadRecord(connection, transaction, credential.CampaignId,
                previous.Value.Sequence);
            return retained is not null && retained.Value.RecordHash == previous.Value.RecordHash
                   && retained.Value.RecordHash == ProbeRecordHash(credential.CampaignId,
                       previous.Value.Sequence, retained.Value.PriorHash,
                       retained.Value.RecordBytes)
                   && retained.Value.RecordBytes.AsSpan().SequenceEqual(recordBytes)
                ? new(P01ProbeStatus.Duplicate, previous.Value.Sequence)
                : new(P01ProbeStatus.Unavailable, null);
        }
        var head = ReadHead(connection, transaction, credential.CampaignId);
        if (head is null || head.Value.Fence != lease.Fence
            || head.Value.Sequence != expectedSequence || head.Value.Hash != expectedHeadHash
            || seat!.Value.SeatEpoch != credential.SeatEpoch)
            return new(P01ProbeStatus.Stale, null);
        var nextSequence = checked(expectedSequence + 1);
        var recordHash = ProbeRecordHash(credential.CampaignId, nextSequence,
            expectedHeadHash, recordBytes);
        using (var insert = Command(connection, transaction, """
            INSERT INTO probe_records VALUES
                ($campaign,$sequence,$prior,$hash,$bytes,$authority,$epoch,$request)
            """))
        {
            insert.Parameters.AddWithValue("$campaign", credential.CampaignId);
            insert.Parameters.AddWithValue("$sequence", nextSequence);
            insert.Parameters.AddWithValue("$prior", expectedHeadHash);
            insert.Parameters.AddWithValue("$hash", recordHash);
            insert.Parameters.AddWithValue("$bytes", recordBytes);
            insert.Parameters.AddWithValue("$authority", credential.AuthorityId);
            insert.Parameters.AddWithValue("$epoch", seat.Value.SeatEpoch);
            insert.Parameters.AddWithValue("$request", requestId);
            insert.ExecuteNonQuery();
        }
        using (var insert = Command(connection, transaction, """
            INSERT INTO probe_requests VALUES ($campaign,$authority,$request,$input,$hash,$sequence)
            """))
        {
            insert.Parameters.AddWithValue("$campaign", credential.CampaignId);
            insert.Parameters.AddWithValue("$authority", credential.AuthorityId);
            insert.Parameters.AddWithValue("$request", requestId);
            insert.Parameters.AddWithValue("$input", inputHash);
            insert.Parameters.AddWithValue("$hash", recordHash);
            insert.Parameters.AddWithValue("$sequence", nextSequence);
            insert.ExecuteNonQuery();
        }
        using (var update = Command(connection, transaction, """
            UPDATE campaigns SET head_sequence=$sequence, head_hash=$hash
            WHERE campaign_id=$campaign AND head_sequence=$priorSequence
                AND head_hash=$priorHash AND activation_fence=$fence
            """))
        {
            update.Parameters.AddWithValue("$sequence", nextSequence);
            update.Parameters.AddWithValue("$hash", recordHash);
            update.Parameters.AddWithValue("$campaign", credential.CampaignId);
            update.Parameters.AddWithValue("$priorSequence", expectedSequence);
            update.Parameters.AddWithValue("$priorHash", expectedHeadHash);
            update.Parameters.AddWithValue("$fence", lease.Fence);
            if (update.ExecuteNonQuery() != 1) return new(P01ProbeStatus.Stale, null);
        }
        transaction.Commit();
        var readback = ReadCommittedProbe(credential.CampaignId, credential.AuthorityId,
            requestId, recordBytes);
        return readback is not null && readback.Sequence == nextSequence
            && readback.RecordHash == recordHash
            ? new(P01ProbeStatus.Committed, nextSequence)
            : new(P01ProbeStatus.Unavailable, null);
    }

    internal P01ProbeReadback? FindProbe(P01PrivateCredential credential, string requestId,
        byte[] exactBytes, P01ActivationLease lease)
    {
        ValidateUuid(requestId);
        ValidateCampaign(credential.CampaignId);
        ValidateSide(credential.Side);
        ValidateUuid(credential.AuthorityId);
        ParseSecret(credential.Secret);
        using var connection = Connect(create: false);
        if (!ValidLease(connection, null, credential.CampaignId, lease)) return null;
        if (!ValidCredential(ReadSeat(connection, null, credential.CampaignId,
                credential.Side), credential)) return null;
        return ReadCommittedProbe(credential.CampaignId, credential.AuthorityId,
            requestId, exactBytes);
    }

    internal P01PrivateHead? ReadHeadForTest(string campaignId)
    {
        ValidateCampaign(campaignId);
        using var connection = Connect(create: false);
        var head = ReadHead(connection, null, campaignId);
        return head is null ? null : new P01PrivateHead(head.Value.Sequence,
            head.Value.Hash, head.Value.Fence);
    }

    private P01ProbeReadback? ReadCommittedProbe(string campaignId, string authorityId,
        string requestId, byte[] exactBytes)
    {
        using var connection = Connect(create: false);
        var request = ReadRequest(connection, null, campaignId, authorityId, requestId);
        if (request is null || request.Value.InputHash != HashBytes("probe-input", exactBytes))
            return null;
        var record = ReadRecord(connection, null, campaignId, request.Value.Sequence);
        return record is not null && record.Value.RecordHash == request.Value.RecordHash
               && record.Value.RecordHash == ProbeRecordHash(campaignId,
                   request.Value.Sequence, record.Value.PriorHash, record.Value.RecordBytes)
               && record.Value.RecordBytes.AsSpan().SequenceEqual(exactBytes)
            ? new P01ProbeReadback(request.Value.Sequence, request.Value.RecordHash)
            : null;
    }

    private SqliteConnection Connect(bool create)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite,
            Pooling = false,
            DefaultTimeout = 5,
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON");
        Execute(connection, "PRAGMA busy_timeout=5000");
        Execute(connection, "PRAGMA synchronous=FULL");
        return connection;
    }

    private static void ValidatePath(string path)
    {
        if (!Path.IsPathFullyQualified(path) || Path.GetFileName(path).Length == 0)
            throw new ArgumentException("An absolute store file path is required.", nameof(path));
    }

    private static void RequirePrivateDirectory(string directory)
    {
        if (OperatingSystem.IsWindows()) return;
        var mode = File.GetUnixFileMode(directory);
        if ((mode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                | UnixFileMode.GroupExecute | UnixFileMode.OtherRead
                | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidOperationException("Store directory permissions are not private.");
    }

    private static void RequireSchema(SqliteConnection connection)
    {
        // Pin complete creator-owned DDL, including CHECK constraints. PRAGMA checks below
        // also provide independent field-level validation of keys and their actions.
        foreach (var (table, sql) in TableDefinitions)
        {
            using var command = Command(connection, null,
                "SELECT sql FROM sqlite_master WHERE type='table' AND name=$table");
            command.Parameters.AddWithValue("$table", table);
            if (command.ExecuteScalar() is not string actual || actual.Trim() != sql.Trim())
                throw new InvalidOperationException("Unrecognized private store table definition.");
        }
        // Each tuple is name, declared type, NOT NULL bit, primary-key ordinal.
        // SQLite reports 0 for NOT NULL on a single-column INTEGER/TEXT PRIMARY KEY.
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["operator_capability"] = ["singleton|INTEGER|0|1", "secret_digest|BLOB|1|0"],
            ["campaigns"] = ["campaign_id|TEXT|0|1", "checkpoint_hash|TEXT|1|0",
                "head_sequence|INTEGER|1|0", "head_hash|TEXT|1|0",
                "activation_fence|INTEGER|1|0", "activation_digest|BLOB|1|0"],
            ["seats"] = ["campaign_id|TEXT|1|1", "side|TEXT|1|2",
                "authority_id|TEXT|1|0", "grant_generation|INTEGER|1|0",
                "seat_epoch|INTEGER|1|0", "grant_digest|BLOB|0|0",
                "grant_expiry_ms|INTEGER|0|0", "credential_digest|BLOB|0|0",
                "credential_expiry_ms|INTEGER|0|0", "revoked|INTEGER|1|0"],
            ["probe_records"] = ["campaign_id|TEXT|1|1", "sequence|INTEGER|1|2",
                "prior_hash|TEXT|1|0", "record_hash|TEXT|1|0",
                "record_bytes|BLOB|1|0", "authority_id|TEXT|1|0",
                "seat_epoch|INTEGER|1|0", "request_id|TEXT|1|0"],
            ["probe_requests"] = ["campaign_id|TEXT|1|1", "authority_id|TEXT|1|2",
                "request_id|TEXT|1|3", "input_hash|TEXT|1|0",
                "record_hash|TEXT|1|0", "sequence|INTEGER|1|0"],
        };
        var foreignKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["operator_capability"] = [],
            ["campaigns"] = [],
            ["seats"] = ["0|0|campaigns|campaign_id|campaign_id|NO ACTION|NO ACTION|NONE"],
            ["probe_records"] = ["0|0|campaigns|campaign_id|campaign_id|NO ACTION|NO ACTION|NONE"],
            ["probe_requests"] = ["0|0|probe_records|campaign_id|campaign_id|NO ACTION|NO ACTION|NONE",
                "0|1|probe_records|sequence|sequence|NO ACTION|NO ACTION|NONE"],
        };
        foreach (var (table, columns) in expected)
        {
            using (var command = Command(connection, null, "PRAGMA table_info(" + table + ")"))
            using (var reader = command.ExecuteReader())
            {
                var actual = new List<string>();
                while (reader.Read())
                    actual.Add($"{reader.GetString(1)}|{reader.GetString(2)}|"
                        + $"{reader.GetInt64(3)}|{reader.GetInt64(5)}");
                if (!actual.SequenceEqual(columns))
                    throw new InvalidOperationException("Unrecognized private store schema.");
            }
            using (var command = Command(connection, null, "PRAGMA foreign_key_list(" + table + ")"))
            using (var reader = command.ExecuteReader())
            {
                var actual = new List<string>();
                while (reader.Read())
                    actual.Add($"{reader.GetInt64(0)}|{reader.GetInt64(1)}|"
                        + $"{reader.GetString(2)}|{reader.GetString(3)}|{reader.GetString(4)}|"
                        + $"{reader.GetString(5)}|{reader.GetString(6)}|{reader.GetString(7)}");
                if (!actual.Order(StringComparer.Ordinal).SequenceEqual(
                        foreignKeys[table].Order(StringComparer.Ordinal)))
                    throw new InvalidOperationException("Unrecognized private store foreign key.");
            }
        }
        // The authority ID must identify one current seat globally, including across sides.
        var uniqueIndexNames = new List<string>();
        using (var indexCommand = Command(connection, null, "PRAGMA index_list(seats)"))
        using (var indexes = indexCommand.ExecuteReader())
        {
            while (indexes.Read())
            {
                if (indexes.GetInt64(2) != 1 || indexes.GetString(3) != "u") continue;
                var indexName = indexes.GetString(1);
                if (indexName.StartsWith("sqlite_autoindex_seats_", StringComparison.Ordinal))
                    uniqueIndexNames.Add(indexName);
            }
        }
        var uniqueAuthority = false;
        foreach (var indexName in uniqueIndexNames)
        {
            using var columnsCommand = Command(connection, null,
                "PRAGMA index_info(" + indexName + ")");
            using var indexColumns = columnsCommand.ExecuteReader();
            var names = new List<string>();
            while (indexColumns.Read()) names.Add(indexColumns.GetString(2));
            uniqueAuthority |= names.SequenceEqual(["authority_id"]);
        }
        if (!uniqueAuthority)
            throw new InvalidOperationException("Missing unique seat authority constraint.");
    }

    private static void ValidateCampaign(string campaignId)
    {
        if (!CampaignPattern.IsMatch(campaignId))
            throw new ArgumentException("Invalid campaign ID.", nameof(campaignId));
    }

    private static void ValidateSide(string side)
    {
        if (side is not ("axis" or "commonwealth"))
            throw new ArgumentException("Invalid side.", nameof(side));
    }

    private static void ValidateUuid(string value)
    {
        if (!Guid.TryParseExact(value, "D", out var parsed) || parsed.ToString("D") != value)
            throw new ArgumentException("Invalid request or authority ID.", nameof(value));
    }

    private static void ValidateHash(string value)
    {
        if (!HashPattern.IsMatch(value))
            throw new ArgumentException("Invalid SHA-256 hash.", nameof(value));
    }

    private static string NewSecret() => Convert.ToHexString(
        RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static byte[] ParseSecret(string secret)
    {
        if (secret.Length != 64 || secret.Any(c => !Uri.IsHexDigit(c) || char.IsUpper(c)))
            throw new ArgumentException("Invalid secret.", nameof(secret));
        return Convert.FromHexString(secret);
    }

    private static byte[] Digest(string domain, byte[] secret) => SHA256.HashData(
        Encoding.ASCII.GetBytes("sandtable.p01." + domain + ".v1\0")
            .Concat(secret).ToArray());

    private static byte[] SeatDigest(string domain, string campaignId, string side,
        string authorityId, long generation, string secret) => SHA256.HashData(
        Encoding.ASCII.GetBytes("sandtable.p01." + domain + ".v1\0" + campaignId
            + "\0" + side + "\0" + authorityId + "\0" + generation + "\0")
            .Concat(ParseSecret(secret)).ToArray());

    private static byte[] ActivationDigest(string campaignId, string secret) =>
        SHA256.HashData(Encoding.ASCII.GetBytes("sandtable.p01.activation.v1\0"
                + campaignId + "\0")
            .Concat(ParseSecret(secret)).ToArray());

    private static string ProbeRecordHash(string campaignId, long sequence,
        string priorHash, byte[] bytes) => HashBytes("probe-record",
        Encoding.ASCII.GetBytes(campaignId + "\0" + sequence + "\0" + priorHash + "\0")
            .Concat(bytes).ToArray());

    private static string HashText(string domain, params string[] fields) => HashBytes(domain,
        Encoding.UTF8.GetBytes(string.Join('\0', fields)));

    private static string HashBytes(string domain, byte[] bytes) => "sha256:"
        + Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(
            "sandtable.p01." + domain + ".v1\0").Concat(bytes).ToArray()))
            .ToLowerInvariant();

    private long NowMs() => clock.GetUtcNow().ToUnixTimeMilliseconds();

    private static void RequireOperator(SqliteConnection connection,
        SqliteTransaction transaction, string operatorSecret)
    {
        using var command = Command(connection, transaction,
            "SELECT secret_digest FROM operator_capability WHERE singleton=1");
        var expected = command.ExecuteScalar() as byte[];
        if (expected is null || !CryptographicOperations.FixedTimeEquals(expected,
                Digest("operator", ParseSecret(operatorSecret))))
            throw new UnauthorizedAccessException("Operator capability rejected.");
    }

    private static void RequireLease(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId, P01ActivationLease lease)
    {
        if (!ValidLease(connection, transaction, campaignId, lease))
            throw new InvalidOperationException("Stale campaign activation fence.");
    }

    private static bool ValidLease(SqliteConnection connection,
        SqliteTransaction? transaction, string campaignId, P01ActivationLease lease)
    {
        if (lease.CampaignId != campaignId) return false;
        ValidateCampaign(lease.CampaignId);
        var expected = ActivationDigest(campaignId, lease.Secret);
        using var command = Command(connection, transaction, """
            SELECT activation_fence,activation_digest FROM campaigns WHERE campaign_id=$campaign
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        using var reader = command.ExecuteReader();
        return reader.Read() && reader.GetInt64(0) == lease.Fence
            && CryptographicOperations.FixedTimeEquals((byte[])reader[1], expected);
    }

    private static (string AuthorityId, long GrantGeneration, long SeatEpoch,
        byte[]? GrantDigest, long? GrantExpiryMs, byte[]? CredentialDigest,
        long? CredentialExpiryMs, bool Revoked)? ReadSeat(SqliteConnection connection,
        SqliteTransaction? transaction, string campaignId, string side)
    {
        using var command = Command(connection, transaction, """
            SELECT authority_id,grant_generation,seat_epoch,grant_digest,grant_expiry_ms,
                   credential_digest,credential_expiry_ms,revoked
            FROM seats WHERE campaign_id=$campaign AND side=$side
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$side", side);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2),
            reader.IsDBNull(3) ? null : (byte[])reader[3],
            reader.IsDBNull(4) ? null : reader.GetInt64(4),
            reader.IsDBNull(5) ? null : (byte[])reader[5],
            reader.IsDBNull(6) ? null : reader.GetInt64(6), reader.GetInt64(7) != 0) : null;
    }

    private bool ValidCredential((string AuthorityId, long GrantGeneration, long SeatEpoch,
        byte[]? GrantDigest, long? GrantExpiryMs, byte[]? CredentialDigest,
        long? CredentialExpiryMs, bool Revoked)? seat, P01PrivateCredential credential) =>
        seat is not null && !seat.Value.Revoked
        && seat.Value.AuthorityId == credential.AuthorityId
        && seat.Value.SeatEpoch == credential.SeatEpoch
        && seat.Value.CredentialDigest is not null
        && seat.Value.CredentialExpiryMs > NowMs()
        && CryptographicOperations.FixedTimeEquals(seat.Value.CredentialDigest,
            SeatDigest("credential", credential.CampaignId, credential.Side,
                credential.AuthorityId, seat.Value.GrantGeneration, credential.Secret));

    private static (long Sequence, string Hash, long Fence)? ReadHead(
        SqliteConnection connection, SqliteTransaction? transaction, string campaignId)
    {
        using var command = Command(connection, transaction,
            "SELECT head_sequence,head_hash,activation_fence FROM campaigns WHERE campaign_id=$campaign");
        command.Parameters.AddWithValue("$campaign", campaignId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2)) : null;
    }

    private static (string InputHash, string RecordHash, long Sequence)? ReadRequest(
        SqliteConnection connection, SqliteTransaction? transaction, string campaignId,
        string authorityId, string requestId)
    {
        using var command = Command(connection, transaction, """
            SELECT input_hash,record_hash,sequence FROM probe_requests
            WHERE campaign_id=$campaign AND authority_id=$authority AND request_id=$request
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$authority", authorityId);
        command.Parameters.AddWithValue("$request", requestId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetString(0), reader.GetString(1), reader.GetInt64(2)) : null;
    }

    private static (string PriorHash, string RecordHash, byte[] RecordBytes)? ReadRecord(
        SqliteConnection connection, SqliteTransaction? transaction, string campaignId,
        long sequence)
    {
        using var command = Command(connection, transaction, """
            SELECT prior_hash,record_hash,record_bytes FROM probe_records
            WHERE campaign_id=$campaign AND sequence=$sequence
            """);
        command.Parameters.AddWithValue("$campaign", campaignId);
        command.Parameters.AddWithValue("$sequence", sequence);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetString(0), reader.GetString(1),
            (byte[])reader[2]) : null;
    }

    private static SqliteCommand Command(SqliteConnection connection,
        SqliteTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static void Execute(SqliteConnection connection, string sql,
        SqliteTransaction? transaction = null)
    {
        using var command = Command(connection, transaction, sql);
        command.ExecuteNonQuery();
    }

    private static long ScalarLong(SqliteConnection connection, string sql)
    {
        using var command = Command(connection, null, sql);
        return (long)command.ExecuteScalar()!;
    }

    private static string ScalarString(SqliteConnection connection, string sql)
    {
        using var command = Command(connection, null, sql);
        return (string)command.ExecuteScalar()!;
    }
}

internal sealed record P01PrivateGrant(string CampaignId, string Side, string AuthorityId,
    string Secret, long ExpiresAtMs)
{
    public override string ToString() => $"P01PrivateGrant({CampaignId},{Side},{AuthorityId},<redacted>)";
}
// OperatorSecret is host-only successor custody. Activation rotates it atomically: a stale
// process holding its prior value cannot acquire a later activation fence.
internal sealed record P01ActivationLease(string CampaignId, long Fence, string Secret,
    string OperatorSecret)
{
    public override string ToString() => $"P01ActivationLease({CampaignId},{Fence},<redacted>)";
}
internal sealed record P01PrivateCredential(string CampaignId, string Side, string AuthorityId,
    string Secret, long SeatEpoch, long ExpiresAtMs)
{
    public override string ToString() => $"P01PrivateCredential({CampaignId},{Side},{AuthorityId},<redacted>)";
}
internal sealed record P01PrivateSeatFacts(string CampaignId, string Side, string AuthorityId,
    long SeatEpoch, string SessionGeneration);
internal sealed record P01PrivateHead(long Sequence, string Hash, long ActivationFence);
internal sealed record P01ProbeReadback(long Sequence, string RecordHash);
internal enum P01ProbeStatus { Committed, Duplicate, Conflict, Unauthorized, Stale, Unavailable }
internal sealed record P01ProbeResult(P01ProbeStatus Status, long? Sequence);
