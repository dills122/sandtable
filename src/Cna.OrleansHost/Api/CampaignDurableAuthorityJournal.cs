using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Durable authority for normal campaigns. Callers must authenticate a seat and bind an owner
/// opening before passing a command here; its local route is disabled unless configured.
/// </summary>
internal sealed partial class CampaignDurableAuthorityJournal
{
    private const int ApplicationId = 0x5343414A; // SCAJ
    private const int SchemaVersion = 12;
    private const int CustodySchemaVersion = 12;
    private const int ReserveSchemaVersion = 11;
    private const int CycleSchemaVersion = 10;
    private const int ForceEntrySchemaVersion = 9;
    private const int RbaSchemaVersion = 8;
    private const int ForceSchemaVersion = 7;
    private const int TimingSchemaVersion = 6;
    private const int LegacySchemaVersion = 5;
    private const int MaxEvidenceBytes = 2_097_152;
    private readonly string path;
    private readonly TimeProvider clock;
    private readonly int schemaVersion;

    private CampaignDurableAuthorityJournal(string path, TimeProvider clock,
        int schemaVersion = SchemaVersion)
    {
        this.path = path;
        this.clock = clock;
        this.schemaVersion = schemaVersion;
    }

    internal static CampaignDurableAuthorityJournal CreateNew(string absolutePath,
        string operatorSecret, TimeProvider? clock = null)
    {
        RequirePath(absolutePath);
        var operatorDigest = CredentialDigest("operator", operatorSecret);
        if (File.Exists(absolutePath)) throw new InvalidOperationException("Journal already exists.");
        var directory = Path.GetDirectoryName(absolutePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute);
        }
        RequirePrivateDirectory(directory);
        var journal = new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System);
        using var connection = journal.Connect(create: true);
        Execute(connection, "PRAGMA journal_mode=WAL");
        Execute(connection, "PRAGMA synchronous=FULL");
        Execute(connection, """
            CREATE TABLE campaigns (
              campaign_id TEXT PRIMARY KEY, contract_version INTEGER NOT NULL,
              ruleset_hash TEXT NOT NULL, seed TEXT NOT NULL, setup_id TEXT NOT NULL,
              setup_hash TEXT NOT NULL, content_pack_id TEXT NOT NULL,
              content_hash TEXT NOT NULL, scenario_id TEXT NOT NULL,
              creation_event BLOB NOT NULL, initial_snapshot BLOB NOT NULL,
              initial_contacts BLOB NOT NULL, timing_configuration BLOB NOT NULL,
              head_sequence INTEGER NOT NULL, head_hash TEXT NOT NULL
            )
            """);
        Execute(connection, """
            CREATE TABLE accepted_steps (
              campaign_id TEXT NOT NULL, sequence INTEGER NOT NULL,
              action_kind TEXT NOT NULL, acting_element_id TEXT,
              owner_public_ref TEXT, seat_epoch INTEGER, owner_revision INTEGER,
              canonical_command BLOB NOT NULL, canonical_event BLOB NOT NULL,
              canonical_receipt BLOB NOT NULL, snapshot_checkpoint BLOB NOT NULL,
              canonical_contacts BLOB NOT NULL,
              prior_hash TEXT NOT NULL, record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,sequence),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE operator_capability (
              singleton INTEGER PRIMARY KEY CHECK(singleton=1), digest BLOB NOT NULL
            )
            """);
        Execute(connection, """
            CREATE UNIQUE INDEX accepted_owner_ref
            ON accepted_steps(campaign_id,owner_public_ref)
            WHERE owner_public_ref IS NOT NULL
            """);
        Execute(connection, """
            CREATE TABLE seats (
              campaign_id TEXT NOT NULL, side TEXT NOT NULL CHECK(side IN ('axis','commonwealth')),
              epoch INTEGER NOT NULL CHECK(epoch>0),
              grant_digest BLOB, grant_expires_ms INTEGER,
              credential_digest BLOB, credential_expires_ms INTEGER,
              PRIMARY KEY(campaign_id,side),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE reference_key (
              singleton INTEGER PRIMARY KEY CHECK(singleton=1), secret BLOB NOT NULL
            )
            """);
        Execute(connection, """
            CREATE TABLE owner_openings (
              campaign_id TEXT NOT NULL, side TEXT NOT NULL, acting_element_id TEXT NOT NULL,
              seat_epoch INTEGER NOT NULL, revision INTEGER NOT NULL,
              visible_digest TEXT NOT NULL, visible_bytes BLOB NOT NULL,
              authority_version INTEGER NOT NULL, action_count INTEGER NOT NULL,
              PRIMARY KEY(campaign_id,side,acting_element_id),
              FOREIGN KEY(campaign_id,side) REFERENCES seats(campaign_id,side)
            )
            """);
        Execute(connection, """
            CREATE TABLE owner_actions (
              campaign_id TEXT NOT NULL, side TEXT NOT NULL, acting_element_id TEXT NOT NULL,
              public_ref TEXT NOT NULL, ordinal INTEGER NOT NULL, core_action_id TEXT NOT NULL,
              PRIMARY KEY(campaign_id,side,acting_element_id,public_ref),
              FOREIGN KEY(campaign_id,side,acting_element_id)
                REFERENCES owner_openings(campaign_id,side,acting_element_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE force_rounds (
              campaign_id TEXT NOT NULL, decision_action_id TEXT NOT NULL,
              decline_event_hash TEXT NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','cancelled')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,decline_event_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE rba_rounds (
              campaign_id TEXT NOT NULL, decision_action_id TEXT NOT NULL,
              barrage_event_hash TEXT NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','cancelled')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,barrage_event_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE cycle_rounds (
              campaign_id TEXT NOT NULL, release_event_hash TEXT NOT NULL,
              entry_state_version INTEGER NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','timed-out')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,release_event_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE reserve_rounds (
              campaign_id TEXT NOT NULL, release_event_hash TEXT NOT NULL,
              entry_state_version INTEGER NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','timed-out')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,release_event_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE custody_rounds (
              campaign_id TEXT NOT NULL, settlement_hash TEXT NOT NULL,
              entry_state_version INTEGER NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','timed-out')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,settlement_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO operator_capability VALUES (1,$digest)";
            command.Parameters.AddWithValue("$digest", operatorDigest);
            command.ExecuteNonQuery();
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO reference_key VALUES (1,$secret)";
            command.Parameters.AddWithValue("$secret", RandomNumberGenerator.GetBytes(32));
            command.ExecuteNonQuery();
        }
        Execute(connection, $"PRAGMA application_id={ApplicationId}");
        Execute(connection, $"PRAGMA user_version={SchemaVersion}");
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(absolutePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return journal;
    }

    internal static CampaignDurableAuthorityJournal OpenExisting(string absolutePath,
        TimeProvider? clock = null)
    {
        RequirePath(absolutePath);
        if (!File.Exists(absolutePath)) throw new InvalidOperationException("Journal is missing.");
        RequirePrivateDirectory(Path.GetDirectoryName(absolutePath)!);
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(absolutePath)
            & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead
               | UnixFileMode.OtherWrite)) != 0)
            throw new InvalidOperationException("Journal permissions are not private.");
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = absolutePath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString());
        connection.Open();
        var version = (int)ScalarInt(connection, "PRAGMA user_version");
        if (ScalarInt(connection, "PRAGMA application_id") != ApplicationId
            || version is not (SchemaVersion or ReserveSchemaVersion or CycleSchemaVersion or ForceEntrySchemaVersion or RbaSchemaVersion or ForceSchemaVersion or TimingSchemaVersion or LegacySchemaVersion))
            throw new InvalidOperationException("Journal schema is not recognized.");
        return new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System,
            version);
    }

    internal ExerciseSession CreateCampaign(CampaignCreationRequest request,
        CampaignDecisionTimingConfigurationV1? timing = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException("Create new campaigns in a current journal.");
        var timingBytes = timing?.SerializeCanonical() ?? [];
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using (var lookup = connection.CreateCommand())
        {
            lookup.Transaction = transaction;
            lookup.CommandText = "SELECT 1 FROM campaigns WHERE campaign_id=$id";
            lookup.Parameters.AddWithValue("$id", request.CampaignId);
            if (lookup.ExecuteScalar() is not null)
            {
                var retained = Load(connection, transaction, request.CampaignId);
                if (retained.Request != request
                    || !retained.TimingBytes.AsSpan().SequenceEqual(timingBytes))
                    throw new InvalidOperationException("Campaign creation conflicts with retained authority.");
                return retained.Session;
            }
        }
        if (timing is not null && (timing.RulesetHash != request.RulesetHash
            || timing.ContentHash != request.ContentHash))
            throw new ArgumentException("Decision timing must bind the campaign rules and content.",
                nameof(timing));
        var created = CampaignHostAdapter.Create(request);
        if (!created.IsStarted) throw new InvalidOperationException("Core rejected campaign creation.");
        var creation = created.CreationEventBytes!;
        var snapshot = created.InitialSnapshotBytes!;
        var contacts = EncodeContactRecords(
            CampaignHostAdapter.QueryContactRecords(created.Session!));
        RequireBounded(creation);
        RequireBounded(snapshot);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO campaigns VALUES ($id,$version,$rules,$seed,$setup,$setupHash,
              $pack,$content,$scenario,$creation,$snapshot,$contacts,$timing,0,$head)
            """;
        command.Parameters.AddWithValue("$id", request.CampaignId);
        command.Parameters.AddWithValue("$version", request.ContractVersion);
        command.Parameters.AddWithValue("$rules", request.RulesetHash);
        command.Parameters.AddWithValue("$seed", request.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$setup", request.SetupId);
        command.Parameters.AddWithValue("$setupHash", request.SetupHash);
        command.Parameters.AddWithValue("$pack", request.ContentPackId);
        command.Parameters.AddWithValue("$content", request.ContentHash);
        command.Parameters.AddWithValue("$scenario", request.ScenarioId);
        command.Parameters.AddWithValue("$creation", creation);
        command.Parameters.AddWithValue("$snapshot", snapshot);
        command.Parameters.AddWithValue("$contacts", contacts);
        command.Parameters.AddWithValue("$timing", timingBytes);
        command.Parameters.AddWithValue("$head", InitialHash(request, creation, snapshot, contacts,
            timingBytes));
        command.ExecuteNonQuery();
        transaction.Commit();
        return created.Session!;
    }

    internal ExerciseSession ReadVerified(string campaignId)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        return Load(connection, transaction, campaignId).Session;
    }

    internal ExerciseStepResult Append(string campaignId, CampaignActionSubmission submission)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(submission);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var head = Load(connection, transaction, campaignId);
        var result = CampaignHostAdapter.Submit(head.Session, submission);
        if (!result.IsAccepted) return result;
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.LegacyKind, null, null, null, null, submission, result);
        transaction.Commit();
        return result;
    }

    private static Loaded PersistAccepted(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId, Loaded head, string kind,
        string? actingElementId, string? ownerPublicRef, long? seatEpoch,
        long? ownerRevision,
        CampaignActionSubmission submission, ExerciseStepResult result)
    {
        var evidence = result.Evidence!;
        var commandBytes = CampaignActionSubmissionSerializer.SerializeCanonical(submission);
        var eventBytes = evidence.EventRecords.Single();
        var receiptBytes = CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt);
        var checkpoint = evidence.SnapshotCheckpoint;
        var contactBytes = EncodeContactRecords(AppendedContactRecords(
            head.Session, result.SuccessorSession!));
        foreach (var bytes in new[] { commandBytes, eventBytes, receiptBytes, checkpoint })
            RequireBounded(bytes);
        var nextSequence = checked(head.Sequence + 1);
        var nextHash = StepHash(head.Hash, nextSequence,
            kind, actingElementId, ownerPublicRef, seatEpoch, ownerRevision,
            commandBytes, eventBytes,
            receiptBytes, checkpoint, contactBytes);
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO accepted_steps VALUES ($id,$seq,$kind,$element,$ref,$epoch,$revision,
                  $command,$event,$receipt,$checkpoint,$contacts,$prior,$hash)
                """;
            insert.Parameters.AddWithValue("$id", campaignId);
            insert.Parameters.AddWithValue("$seq", nextSequence);
            insert.Parameters.AddWithValue("$kind", kind);
            insert.Parameters.AddWithValue("$element", (object?)actingElementId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$ref", (object?)ownerPublicRef ?? DBNull.Value);
            insert.Parameters.AddWithValue("$epoch", (object?)seatEpoch ?? DBNull.Value);
            insert.Parameters.AddWithValue("$revision", (object?)ownerRevision ?? DBNull.Value);
            insert.Parameters.AddWithValue("$command", commandBytes);
            insert.Parameters.AddWithValue("$event", eventBytes);
            insert.Parameters.AddWithValue("$receipt", receiptBytes);
            insert.Parameters.AddWithValue("$checkpoint", checkpoint);
            insert.Parameters.AddWithValue("$contacts", contactBytes);
            insert.Parameters.AddWithValue("$prior", head.Hash);
            insert.Parameters.AddWithValue("$hash", nextHash);
            insert.ExecuteNonQuery();
        }
        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE campaigns SET head_sequence=$seq,head_hash=$hash
                WHERE campaign_id=$id AND head_sequence=$priorSeq AND head_hash=$priorHash
                """;
            update.Parameters.AddWithValue("$id", campaignId);
            update.Parameters.AddWithValue("$seq", nextSequence);
            update.Parameters.AddWithValue("$hash", nextHash);
            update.Parameters.AddWithValue("$priorSeq", head.Sequence);
            update.Parameters.AddWithValue("$priorHash", head.Hash);
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Campaign head changed before commit.");
        }
        return head with { Session = result.SuccessorSession!, Sequence = nextSequence, Hash = nextHash };
    }

    private void RequireWritable()
    {
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException("Legacy journal is read-only; explicit migration is required.");
    }

    private Loaded Load(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        CampaignCreationRequest request;
        byte[] creation;
        byte[] initial;
        byte[] initialContactBytes;
        byte[] timingBytes;
        long headSequence;
        string headHash;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = schemaVersion == LegacySchemaVersion ? """
                SELECT contract_version,ruleset_hash,seed,setup_id,setup_hash,content_pack_id,
                  content_hash,scenario_id,creation_event,initial_snapshot,initial_contacts,
                  head_sequence,head_hash
                FROM campaigns WHERE campaign_id=$id
                """ : """
                SELECT contract_version,ruleset_hash,seed,setup_id,setup_hash,content_pack_id,
                  content_hash,scenario_id,creation_event,initial_snapshot,initial_contacts,
                  timing_configuration,head_sequence,head_hash
                FROM campaigns WHERE campaign_id=$id
                """;
            command.Parameters.AddWithValue("$id", campaignId);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException("Campaign is missing.");
            request = new CampaignCreationRequest(reader.GetInt32(0), campaignId, reader.GetString(1),
                ulong.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetString(7));
            creation = ReadBounded(reader, 8);
            initial = ReadBounded(reader, 9);
            initialContactBytes = ReadBounded(reader, 10);
            timingBytes = schemaVersion == LegacySchemaVersion ? []
                : reader.GetFieldValue<byte[]>(11);
            if (timingBytes.Length > 4096)
                throw new InvalidOperationException("Campaign timing evidence is oversized.");
            headSequence = reader.GetInt64(schemaVersion == LegacySchemaVersion ? 11 : 12);
            headHash = reader.GetString(schemaVersion == LegacySchemaVersion ? 12 : 13);
        }
        var timing = timingBytes.Length == 0 ? null
            : CampaignDecisionTimingConfigurationV1.DeserializeCanonical(timingBytes);
        if (timing is not null && (timing.RulesetHash != request.RulesetHash
            || timing.ContentHash != request.ContentHash))
            throw new InvalidOperationException("Retained timing has a foreign campaign binding.");
        var sequence = 0L;
        var hash = InitialHash(request, creation, initial, initialContactBytes, timingBytes,
            schemaVersion == LegacySchemaVersion);
        var steps = new List<CampaignHostAcceptedStep>();
        var stepContacts = new List<IReadOnlyList<byte[]>>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT sequence,action_kind,acting_element_id,owner_public_ref,seat_epoch,
                  owner_revision,
                  canonical_command,canonical_event,canonical_receipt,snapshot_checkpoint,
                  canonical_contacts,
                  prior_hash,record_hash
                FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence
                """;
            command.Parameters.AddWithValue("$id", campaignId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt64(0) != checked(++sequence) || reader.GetString(11) != hash)
                    throw new InvalidOperationException("Campaign journal order changed.");
                var kind = reader.GetString(1);
                var actingElementId = reader.IsDBNull(2) ? null : reader.GetString(2);
                var ownerPublicRef = reader.IsDBNull(3) ? null : reader.GetString(3);
                var seatEpoch = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4);
                var ownerRevision = reader.IsDBNull(5) ? (long?)null : reader.GetInt64(5);
                var commandBytes = ReadBounded(reader, 6);
                var eventBytes = ReadBounded(reader, 7);
                var receiptBytes = ReadBounded(reader, 8);
                var checkpoint = ReadBounded(reader, 9);
                var contacts = ReadBounded(reader, 10);
                hash = StepHash(hash, sequence, kind, actingElementId, ownerPublicRef,
                    seatEpoch, ownerRevision, commandBytes,
                    eventBytes, receiptBytes, checkpoint, contacts);
                if (reader.GetString(12) != hash)
                    throw new InvalidOperationException("Campaign journal record changed.");
                steps.Add(new CampaignHostAcceptedStep(commandBytes, [eventBytes], receiptBytes,
                    checkpoint, kind, actingElementId));
                stepContacts.Add(DecodeContactRecords(contacts));
            }
        }
        if (sequence != headSequence || hash != headHash)
            throw new InvalidOperationException("Campaign journal head changed.");
        var imported = CampaignHostAdapter.Import(request, creation, initial, steps,
            DecodeContactRecords(initialContactBytes), stepContacts, timing);
        if (!imported.IsVerified)
            throw new InvalidOperationException("Core rejected retained campaign authority.");
        VerifyForceRounds(connection, transaction, campaignId, steps, timing);
        VerifyRbaRounds(connection, transaction, campaignId, steps, timing);
        VerifyCycleRounds(connection, transaction, campaignId, steps, timing);
        VerifyReserveRounds(connection, transaction, campaignId, steps, timing);
        VerifyCustodyRounds(connection, transaction, campaignId, steps, timing);
        return new Loaded(imported.Session!, sequence, hash, timing, request, timingBytes);
    }

    private SqliteConnection Connect(bool create)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        }.ToString());
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON");
        Execute(connection, "PRAGMA busy_timeout=5000");
        return connection;
    }

    private static byte[] ReadBounded(SqliteDataReader reader, int ordinal)
    {
        var length = reader.GetBytes(ordinal, 0, null, 0, 0);
        if (length is < 1 or > MaxEvidenceBytes)
            throw new InvalidOperationException("Campaign evidence length is invalid.");
        var bytes = new byte[(int)length];
        if (reader.GetBytes(ordinal, 0, bytes, 0, bytes.Length) != length)
            throw new InvalidOperationException("Campaign evidence read was incomplete.");
        return bytes;
    }

    private static void RequireBounded(byte[] bytes)
    {
        if (bytes.Length is < 1 or > MaxEvidenceBytes)
            throw new InvalidOperationException("Campaign evidence length is invalid.");
    }

    private static string InitialHash(CampaignCreationRequest request, byte[] creation,
        byte[] initial, byte[] contacts, byte[] timing, bool legacy = false) => Hash(writer =>
    {
        writer.Write(legacy ? "campaign-durable-authority-v2:creation"
            : "campaign-durable-authority-v3:creation");
        writer.Write(request.ContractVersion);
        writer.Write(request.CampaignId);
        writer.Write(request.RulesetHash);
        writer.Write(request.Seed);
        writer.Write(request.SetupId);
        writer.Write(request.SetupHash);
        writer.Write(request.ContentPackId);
        writer.Write(request.ContentHash);
        writer.Write(request.ScenarioId);
        WriteBytes(writer, creation);
        WriteBytes(writer, initial);
        WriteBytes(writer, contacts);
        if (!legacy) WriteBytes(writer, timing);
    });

    private static string StepHash(string prior, long sequence, string kind,
        string? actingElementId, string? ownerPublicRef, long? seatEpoch,
        long? ownerRevision,
        byte[] command, byte[] eventBytes,
        byte[] receipt, byte[] checkpoint, byte[] contacts) => Hash(writer =>
    {
        writer.Write("campaign-durable-authority-v3:step");
        writer.Write(prior);
        writer.Write(sequence);
        writer.Write(kind);
        writer.Write(actingElementId ?? string.Empty);
        writer.Write(ownerPublicRef ?? string.Empty);
        writer.Write(seatEpoch ?? 0);
        writer.Write(ownerRevision ?? 0);
        WriteBytes(writer, command);
        WriteBytes(writer, eventBytes);
        WriteBytes(writer, receipt);
        WriteBytes(writer, checkpoint);
        WriteBytes(writer, contacts);
    });

    private static string Hash(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) write(writer);
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static void WriteBytes(BinaryWriter writer, byte[] bytes)
    {
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static void RequirePath(string path)
    {
        if (!Path.IsPathFullyQualified(path) || Path.GetFileName(path).Length == 0)
            throw new ArgumentException("Journal path must be an absolute file path.", nameof(path));
    }

    private static void RequirePrivateDirectory(string directory)
    {
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(directory)
            & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
               | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidOperationException("Journal directory permissions are not private.");
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long ScalarInt(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    private sealed record Loaded(ExerciseSession Session, long Sequence, string Hash,
        CampaignDecisionTimingConfigurationV1? Timing, CampaignCreationRequest Request, byte[] TimingBytes);
}
