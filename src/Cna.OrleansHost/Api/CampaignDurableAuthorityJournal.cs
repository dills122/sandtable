using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
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
    private const int SchemaVersion = 14;
    private const int SelectionSchemaVersion = 14;
    private const int RetreatSchemaVersion = 13;
    private const int CustodySchemaVersion = 12;
    private const int ReserveSchemaVersion = 11;
    private const int CycleSchemaVersion = 10;
    private const int ForceEntrySchemaVersion = 9;
    private const int RbaSchemaVersion = 8;
    private const int ForceSchemaVersion = 7;
    private const int TimingSchemaVersion = 6;
    private const int LegacySchemaVersion = 5;
    private const int MaxEvidenceBytes = 2_097_152;
    private const int MaxSourceCreationBytes = 24 * 1024 * 1024;
    private readonly string path;
    private readonly TimeProvider clock;
    private readonly int schemaVersion;
    private readonly IContentPackV6Resolver contentResolver;

    private CampaignDurableAuthorityJournal(string path, TimeProvider clock,
        int schemaVersion = SchemaVersion, IContentPackV6Resolver? contentResolver = null)
    {
        this.path = path;
        this.clock = clock;
        this.schemaVersion = schemaVersion;
        this.contentResolver = contentResolver ?? Cna1979SyntheticContentResolver.Instance;
    }

    internal static CampaignDurableAuthorityJournal CreateNew(string absolutePath,
        string operatorSecret, TimeProvider? clock = null, IContentPackV6Resolver? contentResolver = null)
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
        var journal = new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System,
            contentResolver: contentResolver);
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
        Execute(connection, """
            CREATE TABLE retreat_rounds (
              campaign_id TEXT NOT NULL, result_hash TEXT NOT NULL,
              entry_state_version INTEGER NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','timed-out')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,result_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        Execute(connection, """
            CREATE TABLE selection_rounds (
              campaign_id TEXT NOT NULL, entry_hash TEXT NOT NULL,
              entry_state_version INTEGER NOT NULL, configuration_hash TEXT NOT NULL,
              opened_ms INTEGER NOT NULL, deadline_ms INTEGER NOT NULL,
              high_water_ms INTEGER NOT NULL,
              status TEXT NOT NULL CHECK(status IN ('collecting','completed','timed-out')),
              record_hash TEXT NOT NULL,
              PRIMARY KEY(campaign_id,entry_hash),
              FOREIGN KEY(campaign_id) REFERENCES campaigns(campaign_id)
            )
            """);
        journal.EnsureRoundIndexes(connection);
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
        TimeProvider? clock = null, IContentPackV6Resolver? contentResolver = null)
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
            || version is not (SchemaVersion or RetreatSchemaVersion or CustodySchemaVersion or ReserveSchemaVersion or CycleSchemaVersion or ForceEntrySchemaVersion or RbaSchemaVersion or ForceSchemaVersion or TimingSchemaVersion or LegacySchemaVersion))
            throw new InvalidOperationException("Journal schema is not recognized.");
        return new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System,
            version, contentResolver);
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
        var created = CampaignHostAdapter.Create(request, contentResolver);
        if (!created.IsStarted) throw new InvalidOperationException("Core rejected campaign creation.");
        var creation = created.CreationEventBytes!;
        var snapshot = created.InitialSnapshotBytes!;
        var contacts = EncodeContactRecords(
            CampaignHostAdapter.QueryContactRecords(created.Session!));
        RequireBounded(creation);
        RequireBounded(snapshot);
        InsertCampaign(connection, transaction, request, creation, snapshot, contacts, timingBytes);
        transaction.Commit();
        return created.Session!;
    }

    internal CampaignSourceSession CreateCampaign(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(initialization);
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException("Create new campaigns in a current journal.");
        var created = CampaignHostAdapter.Create(request, content, initialization);
        RequireBounded(created.CreationEventBytes, MaxSourceCreationBytes);
        RequireBounded(created.InitialSnapshotBytes, MaxSourceCreationBytes);
        var contacts = EncodeContactRecords([]);
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
                if (retained.Request != request || retained.SourceSession is null
                    || !retained.SourceSession.Content.CanonicalBytes.AsSpan().SequenceEqual(content.CanonicalBytes))
                    throw new InvalidOperationException("Campaign creation conflicts with retained authority.");
                return retained.SourceSession;
            }
        }
        InsertCampaign(connection, transaction, request, created.CreationEventBytes,
            created.InitialSnapshotBytes, contacts, []);
        transaction.Commit();
        return created.Session;
    }

    internal ExerciseSession CreatePregameSourceCampaign(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(initialization);
        RequireWritable();
        if (request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new ArgumentException("Pregame source requires its own creation contract.", nameof(request));
        var created = CampaignHostAdapter.CreatePregameSource(request, content, initialization);
        if (!created.IsStarted) throw new InvalidOperationException("Core rejected pregame source creation.");
        var creation = created.CreationEventBytes!;
        var snapshot = created.InitialSnapshotBytes!;
        var contacts = EncodeContactRecords(CampaignHostAdapter.QueryContactRecords(created.Session!));
        RequireBounded(creation, MaxSourceCreationBytes);
        RequireBounded(snapshot, MaxSourceCreationBytes);
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
                if (retained.Request != request || retained.TimingBytes.Length != 0)
                    throw new InvalidOperationException("Campaign creation conflicts with retained authority.");
                return retained.Session;
            }
        }
        InsertCampaign(connection, transaction, request, creation, snapshot, contacts, []);
        transaction.Commit();
        return created.Session!;
    }

    private static void InsertCampaign(SqliteConnection connection, SqliteTransaction transaction,
        CampaignCreationRequest request, byte[] creation, byte[] snapshot, byte[] contacts, byte[] timingBytes)
    {
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
    }

    internal ExerciseSession ReadVerified(string campaignId)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        return Load(connection, transaction, campaignId).Session;
    }

    internal CampaignSourceSession ReadSourceVerified(string campaignId)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        return Load(connection, transaction, campaignId).SourceSession
            ?? throw new InvalidOperationException("Campaign does not use source initialization.");
    }

    internal ExerciseStepResult Append(string campaignId, CampaignActionSubmission submission)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(submission);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion == CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Pregame owner choices require an authenticated owner opening.");
        var result = CampaignHostAdapter.Submit(head.Session, submission);
        if (!result.IsAccepted) return result;
        var selectionPending = true;
        try { _ = CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(head.Session); }
        catch (InvalidOperationException) { selectionPending = false; }
        SelectionRound? selectionRound = selectionPending
            ? ReadActiveSelectionRound(connection, transaction, head, campaignId) : null;
        if (selectionRound is not null)
        {
            var gate = GateSelectionRound(connection, transaction, selectionRound);
            selectionRound = gate.Round;
            if (gate.CancellationReason is { } reason)
            {
                TimeoutSelectionRound(connection, transaction, head, selectionRound, reason, gate.CancelledAt);
                var closed = Load(connection, transaction, campaignId);
                var rejected = CampaignHostAdapter.Submit(closed.Session, submission);
                if (rejected.IsAccepted) throw new InvalidOperationException("Expired Selection proposal remained valid.");
                transaction.Commit();
                return rejected;
            }
        }
        var acceptedHead = PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.LegacyKind, null, null, null, null, submission, result);
        if (selectionRound is not null)
            UpdateSelectionRound(connection, transaction, selectionRound with { Status = "completed" });
        TryInitializeSelectionRound(connection, transaction, acceptedHead, campaignId);
        transaction.Commit();
        return result;
    }

    internal bool AdmitSourceAxisSupplement(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceAxisSupplementAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceAxisSupplement(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source supplement is not available at this position.");
        var command = CampaignHostAdapter.SourceAxisSupplementCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisSupplementKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCommonwealthSupplement(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCommonwealthSupplementAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCommonwealthSupplement(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source supplement is not available at this position.");
        var command = CampaignHostAdapter.SourceCommonwealthSupplementCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCommonwealthSupplementKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourcePriorityMotorization(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourcePriorityMotorizationAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourcePriorityMotorization(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourcePriorityMotorizationCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourcePriorityMotorizationKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceAttachedInitialLoads(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceAttachedInitialLoadsAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceAttachedInitialLoads(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceAttachedInitialLoadsCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceInitialWrecks(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceInitialWrecksAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceInitialWrecks(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceInitialWrecksCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceInitialWrecksKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceAxisFreeSetup(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceAxisFreeSetupAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceAxisFreeSetup(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceAxisFreeSetupCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisFreeSetupKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceAxisCorpsSetup(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceAxisCorpsSetupAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceAxisCorpsSetup(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceAxisCorpsSetupCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisCorpsSetupKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceMalettiHierarchy(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceMalettiHierarchyAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceMalettiHierarchy(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceMalettiHierarchyCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceMalettiHierarchyKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceGt2Correction(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceGt2CorrectionAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceGt2Correction(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceGt2CorrectionCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceGt2CorrectionKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCwPriorityCatalog(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCwPriorityCatalogAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCwPriorityCatalog(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceCwPriorityCatalogCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCwPriorityCatalogKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCompactCheckpoint(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCompactCheckpointAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCompactCheckpoint(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceCompactCheckpointCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCompactCheckpointKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceFourthIndianWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceFourthIndianWorldAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceFourthIndianWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceFourthIndianWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceFourthIndianWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceMatruhGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceMatruhGroupedWorldAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceMatruhGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceMatruhGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceMalettiGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceMalettiGroupedWorldAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceMalettiGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceMalettiGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceLibyanGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceLibyanGroupedWorldAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceLibyanGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceLibyanGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceItalianDivisionsGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceItalianDivisionsGroupedAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceItalianDivisionsGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceItalianDivisionsGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceSeventhArmoredGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceSeventhArmoredGroupedAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceSeventhArmoredGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceSeventhArmoredGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceSixteenthBrigadeGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceSixteenthBrigadeGroupedAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceSixteenthBrigadeGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceSixteenthBrigadeGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCatanzaroGroupedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCatanzaroGroupedAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCatanzaroGroupedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceCatanzaroGroupedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool StartSourceTurn(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceTurnStartKind);

    internal bool ResolveSourceInitiative(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceTurnInitiativeKind);

    internal bool CompleteSourceGt1Scheduling(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind);

    internal bool CompleteSourceGt1Organization(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind);

    internal bool CompleteSourceGt1StageOneArrivals(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind);

    internal bool CompleteSourceGt1StageOneFleet(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1FleetCompleteKind);

    internal bool CompleteSourceGt1FirstBreakdown(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind);

    internal bool ResolveSourceGt1Weather(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1WeatherKind);

    internal bool AnnounceSourceGt1Lane2(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind);

    internal bool AnnounceSourceGt1Lane3(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind);

    internal bool CorrectSourceSeventhShell(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind);

    internal bool ActivateSourceAxisMinorGarrisons(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind);

    internal bool ActivateSourceAxisGarrisonRemainder(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind);

    internal bool ActivateSourceGiarabubMotorMachineGun(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind);

    internal bool ActivateSourceCwFrontline(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceCwFrontlineKind);

    internal bool AdmitSourceGiarabubMotorPlacementPolicy(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind);

    internal bool AnnounceSourceGt1MultiLanes(string campaignId, string operatorSecret) =>
        ApplySourceTurnSystemStep(campaignId, operatorSecret, CampaignHostAcceptedStep.SourceGt1MultiLanesKind);

    private bool ApplySourceTurnSystemStep(string campaignId, string operatorSecret, string kind)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not source-backed.");
        var alreadyApplied = kind switch
        {
            CampaignHostAcceptedStep.SourceTurnStartKind => CampaignExercises.IsSourceTurnStarted(head.Session),
            CampaignHostAcceptedStep.SourceTurnInitiativeKind => CampaignExercises.IsSourceInitiativeResolved(head.Session),
            CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind => CampaignExercises.IsSourceGt1SchedulingCompleted(head.Session),
            CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind => CampaignExercises.IsSourceGt1OrganizationCompleted(head.Session),
            CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind => CampaignExercises.IsSourceGt1StageOneArrivalsCompleted(head.Session),
            CampaignHostAcceptedStep.SourceGt1FleetCompleteKind => CampaignExercises.IsSourceGt1StageOneFleetCompleted(head.Session),
            CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind => CampaignExercises.IsSourceGt1FirstBreakdownCompleted(head.Session),
            CampaignHostAcceptedStep.SourceGt1WeatherKind => CampaignExercises.IsSourceGt1WeatherResolved(head.Session),
            CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind => CampaignExercises.IsSourceGt1Lane2Announced(head.Session),
            CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind => CampaignExercises.IsSourceGt1Lane3Announced(head.Session),
            CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind => CampaignExercises.IsSourceSeventhShellCorrected(head.Session),
            CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind => CampaignExercises.IsSourceAxisMinorGarrisonsActivated(head.Session),
            CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind => CampaignExercises.IsSourceAxisGarrisonRemainderActivated(head.Session),
            CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind => CampaignExercises.IsSourceGiarabubMotorMachineGunActivated(head.Session),
            CampaignHostAcceptedStep.SourceCwFrontlineKind => CampaignExercises.IsSourceCwFrontlineActivated(head.Session),
            CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind => CampaignExercises.IsSourceGiarabubMotorPlacementPolicyAdmitted(head.Session),
            CampaignHostAcceptedStep.SourceGt1MultiLanesKind => CampaignExercises.IsSourceGt1MultiLanesAnnounced(head.Session),
            _ => throw new ArgumentException("Unknown source turn step.", nameof(kind)),
        };
        if (alreadyApplied)
            return false;
        var accepted = kind switch
        {
            CampaignHostAcceptedStep.SourceTurnStartKind => CampaignHostAdapter.StartSourceTurn(head.Session),
            CampaignHostAcceptedStep.SourceTurnInitiativeKind => CampaignHostAdapter.ResolveSourceInitiative(head.Session),
            CampaignHostAcceptedStep.SourceGt1SchedulingCompleteKind => CampaignHostAdapter.CompleteSourceGt1Scheduling(head.Session),
            CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind => CampaignHostAdapter.CompleteSourceGt1Organization(head.Session),
            CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind => CampaignHostAdapter.CompleteSourceGt1StageOneArrivals(head.Session),
            CampaignHostAcceptedStep.SourceGt1FleetCompleteKind => CampaignHostAdapter.CompleteSourceGt1StageOneFleet(head.Session),
            CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind => CampaignHostAdapter.CompleteSourceGt1FirstBreakdown(head.Session),
            CampaignHostAcceptedStep.SourceGt1WeatherKind => CampaignHostAdapter.ResolveSourceGt1Weather(head.Session),
            CampaignHostAcceptedStep.SourceGt1Lane2AnnounceKind => CampaignHostAdapter.AnnounceSourceGt1Lane2(head.Session),
            CampaignHostAcceptedStep.SourceGt1Lane3AnnounceKind => CampaignHostAdapter.AnnounceSourceGt1Lane3(head.Session),
            CampaignHostAcceptedStep.SourceSeventhShellCorrectionKind => CampaignHostAdapter.CorrectSourceSeventhShell(head.Session),
            CampaignHostAcceptedStep.SourceAxisMinorGarrisonsActivationKind => CampaignHostAdapter.ActivateSourceAxisMinorGarrisons(head.Session),
            CampaignHostAcceptedStep.SourceAxisGarrisonRemainderKind => CampaignHostAdapter.ActivateSourceAxisGarrisonRemainder(head.Session),
            CampaignHostAcceptedStep.SourceGiarabubMotorMachineGunKind => CampaignHostAdapter.ActivateSourceGiarabubMotorMachineGun(head.Session),
            CampaignHostAcceptedStep.SourceCwFrontlineKind => CampaignHostAdapter.ActivateSourceCwFrontline(head.Session),
            CampaignHostAcceptedStep.SourceGiarabubMotorPlacementPolicyKind => CampaignHostAdapter.AdmitSourceGiarabubMotorPlacementPolicy(head.Session),
            CampaignHostAcceptedStep.SourceGt1MultiLanesKind => CampaignHostAdapter.AnnounceSourceGt1MultiLanes(head.Session),
            _ => throw new ArgumentException("Unknown source turn step.", nameof(kind)),
        };
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source turn step is unavailable at this position.");
        var command = CampaignHostAdapter.SourceTurnSystemCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head, kind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceMatruhFormation(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceMatruhFormationAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceMatruhFormation(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceMatruhFormationCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceMatruhFormationKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCwFreeGuns(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCwFreeGunsAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCwFreeGuns(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceCwFreeGunsCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCwFreeGunsKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceAxisPriorityMotorization(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceAxisPriorityMotorizationAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceAxisPriorityMotorization(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source policy is not available at this position.");
        var command = CampaignHostAdapter.SourceAxisPriorityMotorizationCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceAxisMinorGarrisons(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceAxisMinorGarrisons(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source garrisons are not available at this position.");
        var command = CampaignHostAdapter.SourceAxisMinorGarrisonsCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceBattlefield(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceBattlefieldAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceBattlefield(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source battlefield is not available at this position.");
        var command = CampaignHostAdapter.SourceBattlefieldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceBattlefieldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCairoSetup(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCairoSetupAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCairoSetup(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Cairo setup is not available at this position.");
        var command = CampaignHostAdapter.SourceCairoSetupCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCairoSetupKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceCairoWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceCairoWorldAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceCairoWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Cairo World requires all supported player placements.");
        var command = CampaignHostAdapter.SourceCairoWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCairoWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceMaletti(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceMaletti(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceMaletti(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Maletti promotion is not available at this position.");
        var command = CampaignHostAdapter.SourceMalettiCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceMalettiKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceIndependent(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceIndependent(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceIndependent(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Independent force promotion is unavailable.");
        var command = CampaignHostAdapter.SourceIndependentCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceIndependentKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceExpandedIndependent(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceExpandedIndependent(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceExpandedIndependent(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Expanded independent force promotion is unavailable.");
        var command = CampaignHostAdapter.SourceExpandedIndependentCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceExpandedIndependentKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceAxisFreeWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceAxisFreeWorld(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceAxisFreeWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Axis free-world promotion is unavailable.");
        var command = CampaignHostAdapter.SourceAxisFreeWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisFreeWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceAxisCorpsWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceAxisCorpsWorld(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceAxisCorpsWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Axis corps-world promotion is unavailable.");
        var command = CampaignHostAdapter.SourceAxisCorpsWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceAxisCorpsWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceCertifiedMalettiWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceCertifiedMalettiWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("certified Maletti world promotion is unavailable.");
        var command = CampaignHostAdapter.SourceCertifiedMalettiWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceMatruhDetachedWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceMatruhDetachedWorld(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceMatruhDetachedWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Matruh detached world promotion is unavailable.");
        var command = CampaignHostAdapter.SourceMatruhDetachedWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceCwPriorityWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceCwPriorityWorld(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceCwPriorityWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Commonwealth priority world promotion is unavailable.");
        var command = CampaignHostAdapter.SourceCwPriorityWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCwPriorityWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool PromoteSourceCwFreeGunsWorld(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!CampaignExercises.CanPromoteSourceCwFreeGunsWorld(head.Session))
            return false;
        var accepted = CampaignHostAdapter.PromoteSourceCwFreeGunsWorld(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Commonwealth priority world promotion is unavailable.");
        var command = CampaignHostAdapter.SourceCwFreeGunsWorldCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    internal bool AdmitSourceLocalMap(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (CampaignExercises.IsSourceLocalMapAdmitted(head.Session))
            return false;
        var accepted = CampaignHostAdapter.AdmitSourceLocalMap(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException("Source map is unavailable at this position.");
        var command = CampaignHostAdapter.SourceLocalMapCommand(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            CampaignHostAcceptedStep.SourceLocalMapKind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    // Apply only deterministic source-profile migrations. Player setup choices
    // and game-clock advancement are never part of this operator operation.
    internal int AdmitAvailableSourceProfiles(string campaignId, string operatorSecret)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        (Func<ExerciseSession, bool> AlreadyAdmitted, Func<ExerciseSession, ExerciseStepResult> Admit,
            Func<ExerciseSession, ExerciseStepResult, CampaignActionSubmission> Command, string Kind)[] migrations =
        [
            (CampaignExercises.IsSourceAxisSupplementAdmitted, CampaignHostAdapter.AdmitSourceAxisSupplement,
                CampaignHostAdapter.SourceAxisSupplementCommand, CampaignHostAcceptedStep.SourceAxisSupplementKind),
            (CampaignExercises.IsSourceCommonwealthSupplementAdmitted, CampaignHostAdapter.AdmitSourceCommonwealthSupplement,
                CampaignHostAdapter.SourceCommonwealthSupplementCommand, CampaignHostAcceptedStep.SourceCommonwealthSupplementKind),
            (CampaignExercises.IsSourcePriorityMotorizationAdmitted, CampaignHostAdapter.AdmitSourcePriorityMotorization,
                CampaignHostAdapter.SourcePriorityMotorizationCommand, CampaignHostAcceptedStep.SourcePriorityMotorizationKind),
            (CampaignExercises.IsSourceAxisPriorityMotorizationAdmitted, CampaignHostAdapter.AdmitSourceAxisPriorityMotorization,
                CampaignHostAdapter.SourceAxisPriorityMotorizationCommand, CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind),
            (CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted, CampaignHostAdapter.AdmitSourceAxisMinorGarrisons,
                CampaignHostAdapter.SourceAxisMinorGarrisonsCommand, CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind),
            (CampaignExercises.IsSourceBattlefieldAdmitted, CampaignHostAdapter.AdmitSourceBattlefield,
                CampaignHostAdapter.SourceBattlefieldCommand, CampaignHostAcceptedStep.SourceBattlefieldKind),
            (CampaignExercises.IsSourceCairoSetupAdmitted, CampaignHostAdapter.AdmitSourceCairoSetup,
                CampaignHostAdapter.SourceCairoSetupCommand, CampaignHostAcceptedStep.SourceCairoSetupKind),
        ];
        var count = 0;
        foreach (var migration in migrations)
        {
            if (migration.AlreadyAdmitted(head.Session)) continue;
            var accepted = migration.Admit(head.Session);
            if (!accepted.IsAccepted)
                throw new InvalidOperationException("Source setup must first reach a supported migration predecessor.");
            var command = migration.Command(head.Session, accepted);
            head = PersistAccepted(connection, transaction, campaignId, head,
                migration.Kind, null, null, null, null, command, accepted);
            count++;
        }
        transaction.Commit();
        return count;
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
        foreach (var bytes in new[] { commandBytes, eventBytes, receiptBytes })
            RequireBounded(bytes);
        // Source checkpoints retain pinned content artifacts; they can be larger
        // than ordinary events, but remain bounded on both write and replay.
        RequireBounded(checkpoint, head.Request.ContractVersion == CampaignPregameSourceCreation.RequestVersion
            ? MaxSourceCreationBytes : MaxEvidenceBytes);
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
        return head with { LegacySession = result.SuccessorSession!, Sequence = nextSequence, Hash = nextHash };
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
            var initialLimit = request.ContractVersion is CampaignSourceCreation.RequestVersion
                or CampaignPregameSourceCreation.RequestVersion
                ? MaxSourceCreationBytes : MaxEvidenceBytes;
            creation = ReadBounded(reader, 8, initialLimit);
            initial = ReadBounded(reader, 9, initialLimit);
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
        if (request.ContractVersion == CampaignPregameSourceCreation.RequestVersion)
        {
            if (timing is not null) throw new InvalidOperationException("Pregame source timing is not supported.");
            IEnumerable<(CampaignHostAcceptedStep Step, IReadOnlyList<byte[]> Contacts)> ReadSourceSteps()
            {
                using var sourceCommand = connection.CreateCommand();
                sourceCommand.Transaction = transaction;
                sourceCommand.CommandText = """
                    SELECT sequence,action_kind,acting_element_id,owner_public_ref,seat_epoch,owner_revision,
                      canonical_command,canonical_event,canonical_receipt,snapshot_checkpoint,canonical_contacts,
                      prior_hash,record_hash
                    FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence
                    """;
                sourceCommand.Parameters.AddWithValue("$id", campaignId);
                using var reader = sourceCommand.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetInt64(0) != checked(++sequence) || reader.GetString(11) != hash)
                        throw new InvalidOperationException("Campaign journal order changed.");
                    var kind = reader.GetString(1);
                    var element = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var publicRef = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var epoch = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4);
                    var revision = reader.IsDBNull(5) ? (long?)null : reader.GetInt64(5);
                    var commandBytes = ReadBounded(reader, 6);
                    var eventBytes = ReadBounded(reader, 7);
                    var receiptBytes = ReadBounded(reader, 8);
                    var checkpoint = ReadBounded(reader, 9, MaxSourceCreationBytes);
                    var contacts = ReadBounded(reader, 10);
                    hash = StepHash(hash, sequence, kind, element, publicRef, epoch, revision,
                        commandBytes, eventBytes, receiptBytes, checkpoint, contacts);
                    if (reader.GetString(12) != hash)
                        throw new InvalidOperationException("Campaign journal record changed.");
                    yield return (new CampaignHostAcceptedStep(commandBytes, [eventBytes], receiptBytes,
                        checkpoint, kind, element), DecodeContactRecords(contacts));
                }
                if (sequence != headSequence || hash != headHash)
                    throw new InvalidOperationException("Campaign journal head changed.");
            }
            var sourceImport = CampaignHostAdapter.ImportPregameSourceStream(request, creation, initial,
                ReadSourceSteps(), DecodeContactRecords(initialContactBytes));
            if (!sourceImport.IsVerified)
                throw new InvalidOperationException("Core rejected retained campaign authority.");
            VerifyPregameAuxiliaryAuthority(connection, transaction, campaignId,
                CampaignExercises.IsSourceSetupSeat(sourceImport.Session!, CampaignActionAudience.Commonwealth)
                || CampaignExercises.IsSourceTurnSeat(sourceImport.Session!, CampaignActionAudience.Commonwealth));
            return new Loaded(sourceImport.Session!, sequence, hash, null, request, timingBytes);
        }
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
                if (request.ContractVersion == CampaignSourceCreation.RequestVersion)
                    throw new InvalidOperationException("Source initialization does not admit gameplay steps.");
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
                var checkpoint = ReadBounded(reader, 9,
                    request.ContractVersion == CampaignPregameSourceCreation.RequestVersion
                        ? MaxSourceCreationBytes : MaxEvidenceBytes);
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
        if (request.ContractVersion == CampaignSourceCreation.RequestVersion)
        {
            if (timing is not null || !initialContactBytes.AsSpan().SequenceEqual(EncodeContactRecords([])))
                throw new InvalidOperationException("Source initialization has unsupported retained authority.");
            VerifySourceHasNoAuxiliaryAuthority(connection, transaction, campaignId);
            var source = CampaignSourceCreation.Replay(request, creation, initial);
            return new Loaded(null, sequence, hash, null, request, timingBytes, source.Session);
        }
        if (request.ContractVersion == CampaignPregameSourceCreation.RequestVersion && timing is not null)
            throw new InvalidOperationException("Pregame source timing is not supported.");
        var imported = request.ContractVersion == CampaignPregameSourceCreation.RequestVersion
            ? CampaignHostAdapter.ImportPregameSource(request, creation, initial, steps,
                DecodeContactRecords(initialContactBytes), stepContacts)
            : CampaignHostAdapter.ImportWithContentResolver(request, creation, initial, steps, contentResolver,
                DecodeContactRecords(initialContactBytes), stepContacts, timing);
        if (!imported.IsVerified)
            throw new InvalidOperationException("Core rejected retained campaign authority.");
        if (request.ContractVersion == CampaignPregameSourceCreation.RequestVersion)
            VerifyPregameAuxiliaryAuthority(connection, transaction, campaignId,
                CampaignExercises.IsSourceSetupSeat(imported.Session!, CampaignActionAudience.Commonwealth)
                || CampaignExercises.IsSourceTurnSeat(imported.Session!, CampaignActionAudience.Commonwealth));
        VerifyForceRounds(connection, transaction, campaignId, steps, timing);
        VerifyRbaRounds(connection, transaction, campaignId, steps, timing);
        VerifyCycleRounds(connection, transaction, campaignId, steps, timing);
        VerifyReserveRounds(connection, transaction, campaignId, steps, timing);
        VerifyCustodyRounds(connection, transaction, campaignId, steps, timing);
        VerifyRetreatRounds(connection, transaction, campaignId, steps, timing);
        VerifySelectionRounds(connection, transaction, campaignId, steps, timing);
        return new Loaded(imported.Session!, sequence, hash, timing, request, timingBytes);
    }

    private void VerifySourceHasNoAuxiliaryAuthority(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId)
    {
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException("Source initialization requires the current journal schema.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 1 FROM seats WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM owner_openings WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM owner_actions WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM force_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM rba_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM cycle_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM reserve_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM custody_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM retreat_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM selection_rounds WHERE campaign_id=$id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$id", campaignId);
        if (command.ExecuteScalar() is not null)
            throw new InvalidOperationException("Source initialization has unsupported retained authority.");
    }

    private void VerifyPregameAuxiliaryAuthority(SqliteConnection connection,
        SqliteTransaction transaction, string campaignId, bool sourceSetup)
    {
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException("Pregame source requires the current journal schema.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 1 FROM seats WHERE campaign_id=$id AND side<>'axis' AND (side<>'commonwealth' OR $source=0)
            UNION ALL SELECT 1 FROM owner_openings WHERE campaign_id=$id AND NOT
              ((side='axis' AND acting_element_id IN ('land.pregame-axis-supply','land.pregame-axis-supply-finish'))
               OR ($source=1 AND side='axis' AND acting_element_id IN ('land.graziani-axis-dumps','land.graziani-axis-gt1-supply','land.graziani-axis-autoblinda','land.graziani-axis-priority-motorization','land.graziani-axis-free-setup','land.graziani-axis-corps-setup','land.graziani-axis-maletti-hierarchy','land.graziani-axis-gt2-correction'))
               OR ($source=1 AND side IN ('axis','commonwealth') AND acting_element_id IN ('land.graziani-dummy-groups','land.graziani-motorization','land.graziani-unattached-cargo','land.graziani-source-actions'))
               OR ($source=1 AND side='commonwealth' AND acting_element_id IN ('land.graziani-commonwealth-supply','land.graziani-commonwealth-supply-plan','land.graziani-commonwealth-priority-motorization','land.graziani-commonwealth-cairo-setup','land.graziani-commonwealth-attached-initial-loads','land.graziani-commonwealth-initial-wrecks','land.graziani-commonwealth-matruh-formation','land.graziani-commonwealth-priority-catalog','land.graziani-commonwealth-free-guns')))
            UNION ALL SELECT 1 FROM owner_actions WHERE campaign_id=$id AND NOT
              ((side='axis' AND acting_element_id IN ('land.pregame-axis-supply','land.pregame-axis-supply-finish'))
               OR ($source=1 AND side='axis' AND acting_element_id IN ('land.graziani-axis-dumps','land.graziani-axis-gt1-supply','land.graziani-axis-autoblinda','land.graziani-axis-priority-motorization','land.graziani-axis-free-setup','land.graziani-axis-corps-setup','land.graziani-axis-maletti-hierarchy','land.graziani-axis-gt2-correction'))
               OR ($source=1 AND side IN ('axis','commonwealth') AND acting_element_id IN ('land.graziani-dummy-groups','land.graziani-motorization','land.graziani-unattached-cargo','land.graziani-source-actions'))
               OR ($source=1 AND side='commonwealth' AND acting_element_id IN ('land.graziani-commonwealth-supply','land.graziani-commonwealth-supply-plan','land.graziani-commonwealth-priority-motorization','land.graziani-commonwealth-cairo-setup','land.graziani-commonwealth-attached-initial-loads','land.graziani-commonwealth-initial-wrecks','land.graziani-commonwealth-matruh-formation','land.graziani-commonwealth-priority-catalog','land.graziani-commonwealth-free-guns')))
            UNION ALL SELECT 1 FROM force_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM rba_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM cycle_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM reserve_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM custody_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM retreat_rounds WHERE campaign_id=$id
            UNION ALL SELECT 1 FROM selection_rounds WHERE campaign_id=$id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$id", campaignId);
        command.Parameters.AddWithValue("$source", sourceSetup ? 1 : 0);
        if (command.ExecuteScalar() is not null)
            throw new InvalidOperationException("Pregame source has unsupported retained authority.");
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
        // CreateNew's own bootstrap connection (create: true) runs before the round tables
        // exist, so only ensure indexes on the path taken by every other caller — including a
        // journal reopened via OpenExisting against a database written by a build that
        // predates these indexes. There is no ALTER TABLE/migration mechanism in this codebase
        // (see issue #330), so CREATE INDEX IF NOT EXISTS is how older on-disk journals catch up.
        if (!create) EnsureRoundIndexes(connection);
        return connection;
    }

    /// <summary>
    /// Ensures the (status, campaign_id) index each round table needs exists. The
    /// CollectingXCampaigns queries filter on status='collecting' and sort by campaign_id with
    /// no secondary index on these append-only, ever-growing tables, which forces a full table
    /// scan on every 1-second poll from CampaignCombatTimeoutService for all 7 lanes. Idempotent
    /// via IF NOT EXISTS so it is safe to call on every connection open. Each statement is gated
    /// on the schema version that introduced that round table, since an older on-disk journal
    /// opened via OpenExisting may not have it at all (there is no migration mechanism here to
    /// add the table itself -- only to index one that already exists).
    /// </summary>
    private void EnsureRoundIndexes(SqliteConnection connection)
    {
        if (schemaVersion >= ForceSchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS force_rounds_status_campaign "
                + "ON force_rounds(status,campaign_id)");
        if (schemaVersion >= RbaSchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS rba_rounds_status_campaign "
                + "ON rba_rounds(status,campaign_id)");
        if (schemaVersion >= CycleSchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS cycle_rounds_status_campaign "
                + "ON cycle_rounds(status,campaign_id)");
        if (schemaVersion >= ReserveSchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS reserve_rounds_status_campaign "
                + "ON reserve_rounds(status,campaign_id)");
        if (schemaVersion >= CustodySchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS custody_rounds_status_campaign "
                + "ON custody_rounds(status,campaign_id)");
        if (schemaVersion >= RetreatSchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS retreat_rounds_status_campaign "
                + "ON retreat_rounds(status,campaign_id)");
        if (schemaVersion >= SelectionSchemaVersion)
            Execute(connection, "CREATE INDEX IF NOT EXISTS selection_rounds_status_campaign "
                + "ON selection_rounds(status,campaign_id)");
    }

    private static byte[] ReadBounded(SqliteDataReader reader, int ordinal, int maximum = MaxEvidenceBytes)
    {
        var length = reader.GetBytes(ordinal, 0, null, 0, 0);
        if (length < 1 || length > maximum)
            throw new InvalidOperationException("Campaign evidence length is invalid.");
        var bytes = new byte[(int)length];
        if (reader.GetBytes(ordinal, 0, bytes, 0, bytes.Length) != length)
            throw new InvalidOperationException("Campaign evidence read was incomplete.");
        return bytes;
    }

    private static void RequireBounded(byte[] bytes, int maximum = MaxEvidenceBytes)
    {
        if (bytes.Length < 1 || bytes.Length > maximum)
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

    private sealed record Loaded(ExerciseSession? LegacySession, long Sequence, string Hash,
        CampaignDecisionTimingConfigurationV1? Timing, CampaignCreationRequest Request, byte[] TimingBytes,
        CampaignSourceSession? SourceSession = null)
    {
        internal ExerciseSession Session => LegacySession
            ?? throw new InvalidOperationException("Source initialization has no admitted gameplay session.");
    }
}
