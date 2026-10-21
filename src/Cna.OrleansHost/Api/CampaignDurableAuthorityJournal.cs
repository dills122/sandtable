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
/// opening before passing a command here; this type has no public route or service registration.
/// </summary>
internal sealed partial class CampaignDurableAuthorityJournal
{
    private const int ApplicationId = 0x5343414A; // SCAJ
    private const int SchemaVersion = 2;
    private const int MaxEvidenceBytes = 2_097_152;
    private readonly string path;
    private readonly TimeProvider clock;

    private CampaignDurableAuthorityJournal(string path, TimeProvider clock)
    {
        this.path = path;
        this.clock = clock;
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
              head_sequence INTEGER NOT NULL, head_hash TEXT NOT NULL
            )
            """);
        Execute(connection, """
            CREATE TABLE accepted_steps (
              campaign_id TEXT NOT NULL, sequence INTEGER NOT NULL,
              canonical_command BLOB NOT NULL, canonical_event BLOB NOT NULL,
              canonical_receipt BLOB NOT NULL, snapshot_checkpoint BLOB NOT NULL,
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
            CREATE TABLE seats (
              campaign_id TEXT NOT NULL, side TEXT NOT NULL CHECK(side IN ('axis','commonwealth')),
              epoch INTEGER NOT NULL CHECK(epoch>0),
              grant_digest BLOB, grant_expires_ms INTEGER,
              credential_digest BLOB, credential_expires_ms INTEGER,
              PRIMARY KEY(campaign_id,side),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO operator_capability VALUES (1,$digest)";
            command.Parameters.AddWithValue("$digest", operatorDigest);
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
        var journal = new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System);
        using var connection = journal.Connect(create: false);
        if (ScalarInt(connection, "PRAGMA application_id") != ApplicationId
            || ScalarInt(connection, "PRAGMA user_version") != SchemaVersion)
            throw new InvalidOperationException("Journal schema is not recognized.");
        return journal;
    }

    internal ExerciseSession CreateCampaign(CampaignCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var created = CampaignHostAdapter.Create(request);
        if (!created.IsStarted) throw new InvalidOperationException("Core rejected campaign creation.");
        var creation = created.CreationEventBytes!;
        var snapshot = created.InitialSnapshotBytes!;
        RequireBounded(creation);
        RequireBounded(snapshot);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO campaigns VALUES ($id,$version,$rules,$seed,$setup,$setupHash,
              $pack,$content,$scenario,$creation,$snapshot,0,$head)
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
        command.Parameters.AddWithValue("$head", InitialHash(request, creation, snapshot));
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
        ArgumentNullException.ThrowIfNull(submission);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var head = Load(connection, transaction, campaignId);
        var result = CampaignHostAdapter.Submit(head.Session, submission);
        if (!result.IsAccepted) return result;
        var evidence = result.Evidence!;
        var commandBytes = CampaignActionSubmissionSerializer.SerializeCanonical(submission);
        var eventBytes = evidence.EventRecords.Single();
        var receiptBytes = CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt);
        var checkpoint = evidence.SnapshotCheckpoint;
        foreach (var bytes in new[] { commandBytes, eventBytes, receiptBytes, checkpoint })
            RequireBounded(bytes);
        var nextSequence = checked(head.Sequence + 1);
        var nextHash = StepHash(head.Hash, nextSequence, commandBytes, eventBytes,
            receiptBytes, checkpoint);
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO accepted_steps VALUES ($id,$seq,$command,$event,$receipt,$checkpoint,$prior,$hash)
                """;
            insert.Parameters.AddWithValue("$id", campaignId);
            insert.Parameters.AddWithValue("$seq", nextSequence);
            insert.Parameters.AddWithValue("$command", commandBytes);
            insert.Parameters.AddWithValue("$event", eventBytes);
            insert.Parameters.AddWithValue("$receipt", receiptBytes);
            insert.Parameters.AddWithValue("$checkpoint", checkpoint);
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
        transaction.Commit();
        return result;
    }

    private static Loaded Load(SqliteConnection connection, SqliteTransaction transaction,
        string campaignId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        CampaignCreationRequest request;
        byte[] creation;
        byte[] initial;
        long headSequence;
        string headHash;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT contract_version,ruleset_hash,seed,setup_id,setup_hash,content_pack_id,
                  content_hash,scenario_id,creation_event,initial_snapshot,head_sequence,head_hash
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
            headSequence = reader.GetInt64(10);
            headHash = reader.GetString(11);
        }
        var sequence = 0L;
        var hash = InitialHash(request, creation, initial);
        var steps = new List<CampaignHostAcceptedStep>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT sequence,canonical_command,canonical_event,canonical_receipt,
                  snapshot_checkpoint,prior_hash,record_hash
                FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence
                """;
            command.Parameters.AddWithValue("$id", campaignId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt64(0) != checked(++sequence) || reader.GetString(5) != hash)
                    throw new InvalidOperationException("Campaign journal order changed.");
                var commandBytes = ReadBounded(reader, 1);
                var eventBytes = ReadBounded(reader, 2);
                var receiptBytes = ReadBounded(reader, 3);
                var checkpoint = ReadBounded(reader, 4);
                hash = StepHash(hash, sequence, commandBytes, eventBytes, receiptBytes, checkpoint);
                if (reader.GetString(6) != hash)
                    throw new InvalidOperationException("Campaign journal record changed.");
                steps.Add(new CampaignHostAcceptedStep(commandBytes, [eventBytes], receiptBytes,
                    checkpoint));
            }
        }
        if (sequence != headSequence || hash != headHash)
            throw new InvalidOperationException("Campaign journal head changed.");
        var imported = CampaignHostAdapter.Import(request, creation, initial, steps);
        if (!imported.IsVerified)
            throw new InvalidOperationException("Core rejected retained campaign authority.");
        return new Loaded(imported.Session!, sequence, hash);
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
        byte[] initial) => Hash(writer =>
    {
        writer.Write("campaign-durable-authority-v1:creation");
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
    });

    private static string StepHash(string prior, long sequence, byte[] command, byte[] eventBytes,
        byte[] receipt, byte[] checkpoint) => Hash(writer =>
    {
        writer.Write("campaign-durable-authority-v1:step");
        writer.Write(prior);
        writer.Write(sequence);
        WriteBytes(writer, command);
        WriteBytes(writer, eventBytes);
        WriteBytes(writer, receipt);
        WriteBytes(writer, checkpoint);
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

    private sealed record Loaded(ExerciseSession Session, long Sequence, string Hash);
}
