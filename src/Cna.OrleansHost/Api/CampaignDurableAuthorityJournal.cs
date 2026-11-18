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
    // Host-identity store (operator_capability only -- reference_key stays per-campaign, see
    // ReadReferenceKey's remarks): see EnsureHostIdentity's remarks.
    // A different extension than CampaignFileExtension so a directory listing for campaign files
    // (CollectingAcrossSiblingFiles) never mistakes this for one.
    private const string HostIdentityFileName = "_host-identity.db";
    private const int HostIdentityApplicationId = 0x53434149; // SCAI
    private const int HostIdentitySchemaVersion = 1;
    // Per-campaign hot-tier file extension (design doc §3: file-per-campaign topology). Shared by
    // CampaignDurableAuthorityJournalStore (which names each campaign's file) and
    // CollectingAcrossSiblingFiles (which globs for them) so the two can never drift apart.
    internal const string CampaignFileExtension = ".sqlite";
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

    /// <summary>
    /// Crash-safety test seam. When set, invoked with a checkpoint name at well-defined points
    /// during <see cref="CreateNew"/> (see call sites below). Production code leaves this null.
    /// Only ever set by the out-of-process crash-probe child (<c>Cna.OrleansHost.FoundationChild</c>);
    /// never set from inside the main test process, because this is static mutable state and other
    /// test classes call <see cref="CreateNew"/> concurrently under xUnit's default parallelization.
    /// </summary>
    internal static Action<string>? CreateNewCrashHook;

    private const string CreationTempInfix = ".creating-";

    /// <summary>
    /// Creates a brand-new durable journal at <paramref name="absolutePath"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Crash safety (what this guarantees).</b> First-time initialization writes schema,
    /// metadata and the application/schema version marker to a private temporary file in the same
    /// directory, and only atomically renames it onto <paramref name="absolutePath"/>
    /// (<see cref="File.Move(string, string)"/> on the same volume) once that temporary file is
    /// completely valid. <see cref="File.Move(string, string)"/> without <c>overwrite</c> also
    /// refuses to replace a file that already exists at the destination, so this never clobbers
    /// something that showed up at the real path while we were initializing. The practical effect:
    /// if this process is interrupted (killed, crashes, or hits an I/O failure) at <i>any</i> point
    /// before that rename completes, <paramref name="absolutePath"/> is left exactly as it was
    /// before this call -- either still absent (safe to retry <see cref="CreateNew"/> again) or,
    /// if something already existed there, completely untouched. There is never a window where a
    /// half-initialized file is visible at the real configured path.</para>
    /// <para><b>What this does NOT guarantee.</b> This is process-termination/I/O-failure safety,
    /// not power-loss durability: it does not call <c>fsync</c>/<c>fdatasync</c> on the temporary
    /// file or its containing directory before the rename, so on a true power loss (as opposed to a
    /// killed process or an in-process I/O exception) the OS/filesystem's own durability semantics
    /// for recently renamed files apply, and that is a separate claim this code does not make.</para>
    /// <para><b>Operator recovery when something unrecognized already sits at the path.</b> This
    /// method refuses to run at all when <paramref name="absolutePath"/> already exists, and never
    /// deletes or overwrites it, because an existing file might hold real campaign authority. If
    /// that file is not actually a usable journal, <see cref="OpenExisting"/> is the code path that
    /// classifies and reports why (see its remarks for the operator decision tree); it never
    /// auto-repairs anything, because a wrong automatic guess between "valid journal from a newer
    /// build", "true garbage", and "a half-written file left by a pre-fix host build" could destroy
    /// real campaign authority. An operator must inspect the file (and a backup, if one exists)
    /// and move it aside before a fresh journal can be created at that path.</para>
    /// </remarks>
    internal static CampaignDurableAuthorityJournal CreateNew(string absolutePath,
        string operatorSecret, TimeProvider? clock = null, IContentPackV6Resolver? contentResolver = null)
    {
        RequirePath(absolutePath);
        // Validate the secret's shape early and fail fast, before touching the filesystem.
        // EnsureHostIdentity (below) is what actually stores its digest, since operator
        // capability is host-identity state now, not per-campaign state -- see its remarks.
        _ = CredentialDigest("operator", operatorSecret);
        if (File.Exists(absolutePath))
            throw new InvalidOperationException(RecoveryGuidance(absolutePath,
                "Journal already exists."));
        var directory = Path.GetDirectoryName(absolutePath)!;
        // Bootstrap+check is serialized through HostIdentityGate (the same first-time-setup gate
        // EnsureHostIdentity uses just below) because Directory.CreateDirectory makes
        // Directory.Exists true before the follow-up File.SetUnixFileMode has actually run: an
        // unguarded concurrent caller could observe the directory mid-creation, in its transient
        // default-umask state, and fail RequirePrivateDirectory on a directory that is microseconds
        // from being correctly locked down (see this method's own concurrent-first-creation bug).
        // The unlocked fast check below is safe because once a directory is fully created+chmodded
        // it is never touched again by this code, so it can only ever produce a false negative
        // (falling through to the lock), never a false positive.
        if (!TryRequirePrivateDirectory(directory))
        {
            lock (HostIdentityGate)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    if (!OperatingSystem.IsWindows())
                        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite
                            | UnixFileMode.UserExecute);
                }
                RequirePrivateDirectory(directory);
            }
        }
        // Host-identity (operator_capability only) is shared across every campaign file in this
        // directory, not duplicated per campaign -- see EnsureHostIdentity's remarks for why.
        // Ensured before anything else so an operator-gated call (including one that creates a
        // brand-new campaign, which has no file yet) can always authenticate, even against a
        // directory that has never held a single campaign file before.
        EnsureHostIdentity(HostIdentityPathForDirectory(directory), operatorSecret);
        // Orphaned temp files from a previous interrupted CreateNew can never hold real campaign
        // authority -- nothing is ever published to absolutePath until the file is fully valid --
        // so it is always safe to clear them before starting a fresh attempt.
        TryDeleteOrphanedCreationTempFiles(absolutePath, directory);
        var tempPath = Path.Combine(directory,
            Path.GetFileName(absolutePath) + CreationTempInfix + Guid.NewGuid().ToString("N"));
        var journal = new CampaignDurableAuthorityJournal(tempPath, clock ?? TimeProvider.System,
            contentResolver: contentResolver);
        try
        {
            using var connection = journal.Connect(create: true);
            CreateNewCrashHook?.Invoke("after-temp-file-created");
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
            CREATE UNIQUE INDEX accepted_owner_ref
            ON accepted_steps(campaign_id,owner_public_ref)
            WHERE owner_public_ref IS NOT NULL
            """);
            CreateNewCrashHook?.Invoke("mid-schema");
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
                // Independently generated per campaign file -- not shared host-wide like
                // operator_capability (see EnsureHostIdentity's remarks). Reference() always takes
                // campaignId as an explicit input and is only ever checked within one campaign's
                // own scope, so nothing requires this to match across campaigns; keeping it inside
                // the campaign's own file means it travels correctly with that one file if it is
                // ever copied/backed up/restored elsewhere, which a shared host-identity file would
                // not (found by FreshProcessThirdCycleReserveRecoveryRetainsConvertedReserveAndIsIdempotent
                // and similar tests that back up a single campaign file to a new location).
                command.CommandText = "INSERT INTO reference_key VALUES (1,$secret)";
                command.Parameters.AddWithValue("$secret", RandomNumberGenerator.GetBytes(32));
                command.ExecuteNonQuery();
            }
            CreateNewCrashHook?.Invoke("before-version-marking");
            Execute(connection, $"PRAGMA application_id={ApplicationId}");
            Execute(connection, $"PRAGMA user_version={SchemaVersion}");
        }
        catch
        {
            TryDeleteCreationTempFiles(tempPath);
            throw;
        }
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        try
        {
            // Consolidate the WAL into the single main file so the atomic rename below publishes
            // one self-contained file, never a main-file/WAL pair that a crash between two
            // separate renames could tear apart.
            CheckpointAndRemoveWalSidecars(tempPath);
            CreateNewCrashHook?.Invoke("before-publish");
            // Atomic on the same volume. File.Move without `overwrite` also refuses to replace a
            // file that already exists at absolutePath, so this can never clobber real campaign
            // authority that showed up at the real path while we were initializing.
            File.Move(tempPath, absolutePath);
        }
        catch
        {
            TryDeleteCreationTempFiles(tempPath);
            throw;
        }
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(absolutePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System,
            contentResolver: contentResolver);
    }

    private static void TryDeleteCreationTempFiles(string tempPath)
    {
        foreach (var candidate in new[] { tempPath, tempPath + "-wal", tempPath + "-shm", tempPath + "-journal" })
        {
            try { if (File.Exists(candidate)) File.Delete(candidate); }
            catch
            {
                // Best effort. A temp file that was never published to absolutePath can never
                // hold real campaign authority -- nothing opens or writes campaigns against it --
                // so a stale leftover here is a cleanliness issue, not a correctness one; the next
                // CreateNew attempt uses a fresh GUID-suffixed name and also sweeps orphans itself.
            }
        }
    }

    private static void TryDeleteOrphanedCreationTempFiles(string absolutePath, string directory)
    {
        var prefix = Path.GetFileName(absolutePath) + CreationTempInfix;
        string[] entries;
        try { entries = Directory.GetFiles(directory); }
        catch { return; }
        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (name.EndsWith("-wal", StringComparison.Ordinal) || name.EndsWith("-shm", StringComparison.Ordinal)
                || name.EndsWith("-journal", StringComparison.Ordinal))
                continue; // handled alongside its main file below
            TryDeleteCreationTempFiles(entry);
        }
    }

    /// <summary>
    /// Checkpoints <paramref name="dbPath"/>'s write-ahead log fully into its single main file and
    /// removes the now-empty <c>-wal</c>/<c>-shm</c> sidecars, so it is one self-contained file
    /// rather than a main-file/WAL pair. Used by <see cref="CreateNew"/> and <see
    /// cref="RunMigrations"/> on the temporary file each builds right before publishing it, and by
    /// <see cref="RunMigrations"/> a second time on the real configured path itself, immediately
    /// before replacing it with that temporary file (see <see cref="RunMigrations"/>'s remarks for
    /// why). Throws rather than letting a caller publish/leave a file with WAL content that did not
    /// fully checkpoint -- that would mean the single file in question is missing data, or (for the
    /// real path) risks a stale WAL surviving next to content that supersedes it.
    /// </summary>
    private static void CheckpointAndRemoveWalSidecars(string dbPath)
    {
        // A WAL checkpoint needs no other reader/writer holding dbPath open to fully TRUNCATE.
        // RunMigrations calls this against the real configured path immediately after recovering
        // from (or retrying) a crash, which can mean a few milliseconds of OS-level lag releasing a
        // just-terminated process's shared-memory mapping of that same path's -shm sidecar before a
        // fresh checkpoint sees it as free -- observed directly while verifying this change. Retry
        // briefly rather than failing on that transient lag. This does not weaken the guarantee: if
        // the WAL genuinely cannot be fully checkpointed (a real other connection, not just OS
        // cleanup lag), every attempt below observes the same non-empty sidecar and this still
        // throws, refusing to publish, exactly as before this retry loop existed.
        InvalidOperationException? lastFailure = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (attempt > 0) Thread.Sleep(20);
            try
            {
                CheckpointAndRemoveWalSidecarsOnce(dbPath);
                return;
            }
            catch (InvalidOperationException ex)
            {
                lastFailure = ex;
            }
        }
        throw lastFailure!;
    }

    private static void CheckpointAndRemoveWalSidecarsOnce(string dbPath)
    {
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        }.ToString()))
        {
            connection.Open();
            Execute(connection, "PRAGMA wal_checkpoint(TRUNCATE)");
        }
        foreach (var sidecar in new[] { dbPath + "-wal", dbPath + "-shm" })
        {
            if (!File.Exists(sidecar)) continue;
            if (new FileInfo(sidecar).Length > 0)
                throw new InvalidOperationException(
                    "Journal initialization could not fully checkpoint its write-ahead log; " +
                    "refusing to publish a journal that is not yet a single self-contained file.");
            try { File.Delete(sidecar); }
            catch { /* best effort; an empty sidecar left behind is harmless */ }
        }
    }

    /// <summary>
    /// A single explicit, numbered forward-migration step: given a connection already inside an
    /// open transaction against a private temporary copy of the journal, mutates the schema/data in
    /// place (<c>ALTER TABLE</c>, data transforms, etc.) to carry it from <see cref="FromVersion"/>
    /// to <see cref="ToVersion"/>. Does not set <c>PRAGMA user_version</c> itself -- <see
    /// cref="RunMigrations"/> does that once, after every applicable step has committed.
    /// </summary>
    internal readonly record struct MigrationStep(int FromVersion, int ToVersion,
        Action<SqliteConnection, SqliteTransaction> Apply);

    /// <summary>
    /// Explicit, numbered forward-migration steps for the hot/live journal, applied in order by
    /// <see cref="RunMigrations"/> when <see cref="OpenExisting"/> finds an on-disk file whose
    /// <c>user_version</c> is behind <see cref="CurrentSchemaVersion"/> but still within this
    /// mechanism's scope (some step's <see cref="MigrationStep.FromVersion"/>).
    /// </summary>
    /// <remarks>
    /// <para><b>Scope boundary</b> (answering issue #330's "Archives compatibility" checklist item):
    /// this mechanism is for schema changes <b>from this point forward</b> only. The historical
    /// versions recognized by <see cref="RetreatSchemaVersion"/> down through
    /// <see cref="LegacySchemaVersion"/> predate this mechanism's introduction and are deliberately
    /// left exactly as they were, on the existing parallel read-only legacy branches in
    /// <see cref="Load"/> and the <c>schemaVersion != CurrentSchemaVersion</c> write-gates (e.g.
    /// <see cref="RequireWritable"/>) -- this list never runs against them, and never will.
    /// Retroactively migrating those historical versions is explicitly out of scope ("no implicit
    /// migration or fabricated deadlines").</para>
    /// <para>Empty in production today: nothing has changed <see cref="SchemaVersion"/>'s shape (14)
    /// yet, so there is nothing to migrate from. A future real schema change bumps
    /// <see cref="SchemaVersion"/> (and <see cref="CreateNew"/>'s DDL) to the new value and, in the
    /// same change, registers a step here from the old value to the new one, so on-disk files
    /// written under the old value get carried forward the next time they are opened. Tests register
    /// throwaway steps through this same list (see <c>Cna.OrleansHost.FoundationChild</c>'s
    /// migration-probe modes) to exercise the mechanism itself without waiting for a real schema
    /// change -- always from a short-lived, single-purpose child process, never from inside the
    /// shared test process, for the same reason <see cref="CreateNewCrashHook"/> is restricted that
    /// way: this is static mutable state, and other test classes open journals concurrently under
    /// xUnit's default parallelization.</para>
    /// <para><b>Cold-tier note.</b> This registry and <see cref="RunMigrations"/> apply to the
    /// hot/live journal only. The design doc
    /// (<c>docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md</c> &#167;9)
    /// proposes that a future cold/archived tier never migrates its records -- they are read forever
    /// under whatever schema version they were written with, which is simpler and consistent with
    /// never mutating archived data. That tier does not exist yet; a future implementer of it should
    /// not assume this mechanism extends there.</para>
    /// </remarks>
    internal static readonly List<MigrationStep> MigrationSteps = [];

    /// <summary>
    /// Test-only accessor for <see cref="SchemaVersion"/> (today's baseline, before this
    /// mechanism's introduction), so a migration-probe test registering a synthetic step doesn't
    /// need to hardcode that baseline as a magic number -- and stays correct if it is ever bumped
    /// for a real schema change.
    /// </summary>
    internal static int SchemaVersionForMigrationTests => SchemaVersion;

    /// <summary>
    /// The schema version <see cref="OpenExisting"/>'s migration mechanism treats as fully migrated:
    /// <see cref="SchemaVersion"/> when <see cref="MigrationSteps"/> is empty (production today), or
    /// the last registered step's <see cref="MigrationStep.ToVersion"/> otherwise. Every write-gate
    /// that used to compare against <see cref="SchemaVersion"/> directly now compares against this
    /// instead, so a freshly migrated file is treated identically to one created directly at the
    /// newest version.
    /// </summary>
    private static int CurrentSchemaVersion => MigrationSteps.Count == 0
        ? SchemaVersion : MigrationSteps[^1].ToVersion;

    /// <summary>
    /// True when <paramref name="version"/> is a version <see cref="MigrationSteps"/> knows how to
    /// carry forward to <see cref="CurrentSchemaVersion"/> -- i.e. it equals some registered step's
    /// <see cref="MigrationStep.FromVersion"/>. Also validates that the registered chain is
    /// contiguous (each step's <see cref="MigrationStep.FromVersion"/> equal to the previous step's
    /// <see cref="MigrationStep.ToVersion"/>, starting at <see cref="SchemaVersion"/>), so a caller
    /// that gets <see langword="true"/> back can trust that running the chain always lands exactly on
    /// <see cref="CurrentSchemaVersion"/>, never short of it.
    /// </summary>
    private static bool IsMigratableVersion(int version)
    {
        var expected = SchemaVersion;
        var found = false;
        foreach (var step in MigrationSteps)
        {
            if (step.FromVersion != expected)
                throw new InvalidOperationException(
                    "Registered migration steps are not a contiguous chain from SchemaVersion.");
            if (step.FromVersion == version) found = true;
            expected = step.ToVersion;
        }
        return found;
    }

    private const string MigrationTempInfix = ".migrating-";

    /// <summary>
    /// Crash-safety test seam for <see cref="RunMigrations"/>, invoked with a checkpoint name at
    /// well-defined points (see call sites below) -- the migration analogue of
    /// <see cref="CreateNewCrashHook"/>, with the same restriction: only ever set by an
    /// out-of-process crash-probe child, never from inside the shared test process.
    /// </summary>
    internal static Action<string>? MigrationCrashHook;

    /// <summary>
    /// Carries an existing journal file forward from <paramref name="fromVersion"/> to
    /// <see cref="CurrentSchemaVersion"/> by running every applicable <see cref="MigrationSteps"/>
    /// entry in order, inside the same crash-safety envelope <see cref="CreateNew"/> uses for
    /// first-time initialization.
    /// </summary>
    /// <remarks>
    /// <para><b>Crash safety (what this guarantees).</b> Every schema/data change this method makes
    /// lives in a private temporary file in the same directory, built via SQLite's <c>VACUUM INTO</c>
    /// against a read-only connection to <paramref name="absolutePath"/> -- which, per SQLite's own
    /// documentation, never requires (and never takes) write access to the source database, and also
    /// fully consolidates any not-yet-checkpointed write-ahead log content into the copy. So no
    /// migration step, and no schema/data mutation of any kind, is ever applied to
    /// <paramref name="absolutePath"/> directly -- only to the temporary copy. Every migration step
    /// runs in its own committed transaction against that copy; only once every step has committed
    /// and the temporary file has been fully WAL-checkpointed into a single self-contained file
    /// (<see cref="CheckpointAndRemoveWalSidecars"/>, reused from <see cref="CreateNew"/>) does this
    /// method atomically replace <paramref name="absolutePath"/> with it
    /// (<see cref="File.Move(string, string, bool)"/> with <c>overwrite: true</c> -- atomic on the
    /// same volume, the same guarantee <see cref="CreateNew"/>'s remarks describe for its own publish
    /// step; the only difference here is a destination that already exists, which is exactly why
    /// <c>overwrite: true</c> is needed instead of the no-overwrite publish <see cref="CreateNew"/>
    /// uses). Immediately before that rename, this method also runs the same
    /// <see cref="CheckpointAndRemoveWalSidecars"/> helper against <paramref name="absolutePath"/>
    /// itself -- the one operation this method performs directly against the original file, and a
    /// non-destructive one: a WAL checkpoint only persists already-committed data into the main file,
    /// changing no logical content (same schema, same rows, same hash chain), so it does not
    /// contradict "never half-migrated." It is required, not optional: <see cref="File.Move"/> only
    /// replaces the main file, so a stale pre-migration <c>-wal</c>/<c>-shm</c> pair left sitting next
    /// to the newly-published content would make SQLite replay that stale WAL (self-consistently
    /// validated against its own matching <c>-shm</c>) over the new file's different page layout on
    /// the very next open, corrupting reads of the migrated file -- a failure mode this method
    /// verified by reproducing it before adding this step. If this process is interrupted at any
    /// point before the rename completes, <paramref name="absolutePath"/> is left exactly as it was
    /// before this call in every way that matters -- fully pre-migration, openable by the old logic
    /// exactly as before -- never half-migrated. There is never a window where a partially migrated
    /// file is visible at the real configured path.</para>
    /// </remarks>
    private static void RunMigrations(string absolutePath, int fromVersion)
    {
        var directory = Path.GetDirectoryName(absolutePath)!;
        // An orphaned temp file from a previous interrupted migration attempt can never have been
        // published to absolutePath -- nothing is published until it is fully built and verified --
        // so it is always safe to clear before starting a fresh attempt, exactly like CreateNew's
        // own orphan sweep.
        TryDeleteOrphanedMigrationTempFiles(absolutePath, directory);
        var tempPath = Path.Combine(directory,
            Path.GetFileName(absolutePath) + MigrationTempInfix + Guid.NewGuid().ToString("N"));
        try
        {
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = absolutePath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private,
                Pooling = false,
            }.ToString()))
            {
                source.Open();
                using var vacuum = source.CreateCommand();
                vacuum.CommandText = "VACUUM INTO $dest";
                vacuum.Parameters.AddWithValue("$dest", tempPath);
                vacuum.ExecuteNonQuery();
            }
            MigrationCrashHook?.Invoke("after-temp-snapshot-created");
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = tempPath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Private,
                Pooling = false,
            }.ToString()))
            {
                connection.Open();
                Execute(connection, "PRAGMA journal_mode=WAL");
                Execute(connection, "PRAGMA synchronous=FULL");
                Execute(connection, "PRAGMA foreign_keys=ON");
                var version = fromVersion;
                var stepIndex = 0;
                foreach (var step in MigrationSteps)
                {
                    if (step.FromVersion < version) continue;
                    if (step.FromVersion != version)
                        throw new InvalidOperationException(
                            "Registered migration steps are not a contiguous chain from SchemaVersion.");
                    using var transaction = connection.BeginTransaction(deferred: false);
                    step.Apply(connection, transaction);
                    transaction.Commit();
                    version = step.ToVersion;
                    stepIndex++;
                    // Named by ordinal position, not by version number, so a crash-probe test can
                    // reference a fixed checkpoint name without depending on SchemaVersion's actual
                    // current value.
                    MigrationCrashHook?.Invoke($"after-step-{stepIndex}");
                }
                if (version != CurrentSchemaVersion)
                    throw new InvalidOperationException(
                        "Migration steps did not reach the current schema version.");
                MigrationCrashHook?.Invoke("before-version-marking");
                Execute(connection, $"PRAGMA user_version={CurrentSchemaVersion}");
            }
        }
        catch
        {
            TryDeleteMigrationTempFiles(tempPath);
            throw;
        }
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        try
        {
            // Same reasoning as CreateNew: consolidate the WAL into the single main file first, so
            // the atomic rename below publishes one self-contained file, never a main-file/WAL pair
            // a crash between two separate renames could tear apart.
            CheckpointAndRemoveWalSidecars(tempPath);
            // Also fold and clear any not-yet-checkpointed WAL content sitting at absolutePath's OWN
            // sidecars (left over from ordinary prior use -- a live WAL-mode file commonly still has
            // these). This is non-destructive: a WAL checkpoint only persists already-committed data
            // into the main file, changing no logical content, so it does not conflict with "the
            // original file is never half-migrated" -- it stays byte-for-byte equivalent in every way
            // that matters (schema, rows, hash chain). It is required before the rename, not optional:
            // File.Move only replaces the main file, so a stale pre-migration -wal/-shm pair left
            // sitting next to the newly-published content would make SQLite replay that stale WAL
            // (validated self-consistently against its own matching -shm) over the NEW file's
            // different page layout on the very next open, corrupting reads of the migrated file --
            // confirmed by reproduction during this change's own verification.
            CheckpointAndRemoveWalSidecars(absolutePath);
            MigrationCrashHook?.Invoke("before-publish");
            // Atomic on the same volume, same as CreateNew's publish -- overwrite:true because,
            // unlike CreateNew, the destination already exists (the pre-migration file being
            // replaced).
            File.Move(tempPath, absolutePath, overwrite: true);
        }
        catch
        {
            TryDeleteMigrationTempFiles(tempPath);
            throw;
        }
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(absolutePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static void TryDeleteMigrationTempFiles(string tempPath)
    {
        foreach (var candidate in new[] { tempPath, tempPath + "-wal", tempPath + "-shm", tempPath + "-journal" })
        {
            try { if (File.Exists(candidate)) File.Delete(candidate); }
            catch
            {
                // Best effort -- see TryDeleteCreationTempFiles for the same reasoning: a temp file
                // never published to absolutePath can never hold real campaign authority.
            }
        }
    }

    private static void TryDeleteOrphanedMigrationTempFiles(string absolutePath, string directory)
    {
        var prefix = Path.GetFileName(absolutePath) + MigrationTempInfix;
        string[] entries;
        try { entries = Directory.GetFiles(directory); }
        catch { return; }
        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (name.EndsWith("-wal", StringComparison.Ordinal) || name.EndsWith("-shm", StringComparison.Ordinal)
                || name.EndsWith("-journal", StringComparison.Ordinal))
                continue;
            TryDeleteMigrationTempFiles(entry);
        }
    }

    /// <summary>
    /// Operator-facing guidance for a file sitting at the configured journal path that this host
    /// will not touch automatically. Shared by <see cref="CreateNew"/> (refuses to run at all when
    /// something already exists there) and <see cref="OpenExisting"/> (refuses to guess at an
    /// unrecognized file's meaning). The three states this cannot tell apart on its own are: a
    /// genuine journal from a newer host build (future schema version), a half-written file left
    /// by a pre-fix host build that crashed during first-time initialization (this fix removes that
    /// possibility going forward, but does not retroactively repair a file written by old code),
    /// and unrelated garbage. Recovery is manual: inspect the file (and any backup) out of band,
    /// then either move/rename it aside so a fresh journal can be created at this path, restore a
    /// known-good backup to this path, or -- if it is confirmed to be a valid newer-build journal
    /// this host build should not touch -- redeploy a host build that recognizes its schema version.
    /// </summary>
    private static string RecoveryGuidance(string absolutePath, string detail) =>
        detail + " Refusing to touch '" + absolutePath + "' automatically: an unrecognized file at a " +
        "configured journal path could be a valid journal (e.g. from a newer host build), a half-written " +
        "file left by an interrupted first-time initialization, or unrelated garbage, and guessing wrong " +
        "could destroy real campaign authority. An operator must inspect the file (and any backup) and " +
        "move it aside, or restore a known-good backup, before this path can be initialized or opened. " +
        $"See the {nameof(CampaignDurableAuthorityJournal)} doc comments for the full recovery decision tree.";

    /// <summary>
    /// Opens an existing durable journal at <paramref name="absolutePath"/>.
    /// </summary>
    /// <remarks>
    /// Never auto-repairs or replaces the file. If it cannot be read as a SQLite database at all,
    /// or does not carry a recognized application id/schema version, this throws with guidance
    /// instead of silently treating it as absent or rewriting it -- see
    /// <see cref="RecoveryGuidance"/> and the remarks on <see cref="CreateNew"/> for the operator
    /// recovery decision tree covering the states this cannot tell apart on its own.
    /// </remarks>
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
        int version;
        int applicationId;
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = absolutePath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString()))
        {
            try
            {
                connection.Open();
                version = (int)ScalarInt(connection, "PRAGMA user_version");
                applicationId = (int)ScalarInt(connection, "PRAGMA application_id");
            }
            catch (SqliteException ex)
            {
                throw new InvalidOperationException(RecoveryGuidance(absolutePath,
                    "The file at the configured journal path could not be read as a SQLite " +
                    "database (it may be truncated, corrupted, or not a journal at all)."), ex);
            }
        }
        // A version this mechanism knows how to carry forward (some registered step's FromVersion,
        // short of CurrentSchemaVersion) is recognized even though it is not literally
        // CurrentSchemaVersion or one of the historical legacy constants below -- it gets migrated
        // in place, under crash safety, a few lines down.
        var migratable = applicationId == ApplicationId && version != CurrentSchemaVersion
            && IsMigratableVersion(version);
        if (applicationId != ApplicationId
            || (!migratable && version != CurrentSchemaVersion
                && version is not (RetreatSchemaVersion or CustodySchemaVersion or ReserveSchemaVersion or CycleSchemaVersion or ForceEntrySchemaVersion or RbaSchemaVersion or ForceSchemaVersion or TimingSchemaVersion or LegacySchemaVersion)))
            throw new InvalidOperationException(RecoveryGuidance(absolutePath, applicationId == ApplicationId
                ? $"The file's schema version ({version}) is not one this host build recognizes. " +
                  "This can mean the file was created by a newer host build, or that it never " +
                  "finished first-time initialization (the version marker is written last, so an " +
                  "interrupted pre-fix initialization can leave a file here with no recognizable " +
                  "version at all)."
                : "The file does not carry this journal's application id; it is not recognized as " +
                  "a journal created by this host."));
        if (migratable)
        {
            RunMigrations(absolutePath, version);
            version = CurrentSchemaVersion;
        }
        return new CampaignDurableAuthorityJournal(absolutePath, clock ?? TimeProvider.System,
            version, contentResolver);
    }

    internal ExerciseSession CreateCampaign(CampaignCreationRequest request,
        CampaignDecisionTimingConfigurationV1? timing = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (schemaVersion != CurrentSchemaVersion)
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
        if (schemaVersion != CurrentSchemaVersion)
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        RequireOperator(operatorSecret);
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
        if (schemaVersion != CurrentSchemaVersion)
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
        if (schemaVersion != CurrentSchemaVersion)
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
        if (schemaVersion != CurrentSchemaVersion)
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
        // Pooling is deliberately off: PR #412 enabled it, but a pooled connection's native
        // handle stays parked after Dispose() in this Microsoft.Data.Sqlite build, which left
        // CreateNew's own post-write wal_checkpoint(TRUNCATE) unable to fully truncate the WAL
        // -- reproduced deterministically on a clean checkout, confirmed independently, and
        // confirmed fixed by this revert. The connection-reuse benefit was already assessed as
        // small (cheap local-file open/close, a grain's own lifecycle already bounds connection
        // lifetime); re-enabling pooling needs that interaction solved first, e.g. an explicit
        // SqliteConnection.ClearPool() before any checkpoint-dependent path runs.
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

    internal static string HostIdentityPathForDirectory(string directory) =>
        Path.Combine(directory, HostIdentityFileName);

    /// <summary>
    /// The host-identity file sitting beside this instance's own per-campaign file, in the same
    /// directory. See <see cref="EnsureHostIdentity"/>'s remarks for why operator capability and
    /// the campaign-reference HMAC key live here rather than inside every per-campaign file.
    /// </summary>
    private string HostIdentityPath() =>
        HostIdentityPathForDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

    private static readonly object HostIdentityGate = new();

    /// <summary>
    /// Ensures a small, shared, host-wide identity file exists at <paramref name="hostIdentityPath"/>,
    /// holding exactly what <c>operator_capability</c> held inside the single shared journal file
    /// before the design doc's &#167;3 file-per-campaign split
    /// (<c>docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md</c>). Deliberately
    /// <i>not</i> <c>reference_key</c> too -- see <see cref="ReadReferenceKey"/>'s remarks for why
    /// that one stays per-campaign.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is not simply duplicated into every per-campaign file.</b> Unlike
    /// <c>campaigns</c>/<c>accepted_steps</c>/the round tables, <c>operator_capability</c> was never
    /// per-campaign data to begin with -- it is host-wide administrative identity that happened to
    /// live in the one shared file purely because, before this change, there was only ever one file
    /// to put anything in. Two call sites make this concrete: <c>CampaignDurableOutwardApi</c>'s
    /// <c>/source</c> and <c>/pregame-source*</c> routes authenticate the operator
    /// (<see cref="RequireOperator"/>) <i>before</i> the campaign they are about to create has a
    /// file at all, so there is no per-campaign file to check this against even in principle.
    /// Per the design doc's &#167;2.2 (the same reasoning that already keeps
    /// <c>LocalSessionStore</c>, &#167;1.8, out of the campaign journal entirely) and &#167;1.8's
    /// explicit "never folded into the campaign journal's schema or file" guidance, this host
    /// identity gets its own small shared store instead -- one file, written once per secret
    /// (at most a few bytes, a handful of times over a host's lifetime), never contended by the
    /// 1000-campaigns-at-once problem &#167;3 exists to solve, so sharing one file for it carries
    /// none of the risk sharing one file for campaign history did. <c>reference_key</c> does not
    /// share this requirement (nothing authenticates against it before a campaign exists), and
    /// sharing it the same way turned out to be actively wrong -- see
    /// <see cref="ReadReferenceKey"/>'s remarks.</para>
    /// <para><b>Crash safety.</b> Built at a private temporary path and published with the same
    /// atomic, no-overwrite <see cref="File.Move(string, string)"/> <see cref="CreateNew"/> uses,
    /// so a process killed mid-bootstrap never leaves a half-written identity file visible at
    /// <paramref name="hostIdentityPath"/>.</para>
    /// <para><b>Concurrency.</b> Called from <see cref="CreateNew"/> every time <i>any</i> campaign
    /// file is first created, and eagerly once by <c>CampaignDurableAuthorityJournalStore</c>'s
    /// constructor so it exists before the host ever serves a request. The common case (already
    /// exists) is a single <see cref="File.Exists"/> check with no lock. Only the rare first-ever
    /// creation takes <see cref="HostIdentityGate"/>, and a concurrent loser's
    /// <see cref="File.Move(string, string)"/> (no overwrite) simply fails because the winner's
    /// publish already landed -- treated as success, not an error, since the end state (a valid
    /// identity file at the path) is identical either way.</para>
    /// </remarks>
    internal static void EnsureHostIdentity(string hostIdentityPath, string operatorSecret)
    {
        if (File.Exists(hostIdentityPath)) return;
        lock (HostIdentityGate)
        {
            if (File.Exists(hostIdentityPath)) return;
            var operatorDigest = CredentialDigest("operator", operatorSecret);
            var directory = Path.GetDirectoryName(hostIdentityPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite
                        | UnixFileMode.UserExecute);
            }
            RequirePrivateDirectory(directory);
            var tempPath = Path.Combine(directory,
                Path.GetFileName(hostIdentityPath) + CreationTempInfix + Guid.NewGuid().ToString("N"));
            try
            {
                using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = tempPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Private,
                    Pooling = false,
                }.ToString()))
                {
                    connection.Open();
                    Execute(connection, "PRAGMA journal_mode=WAL");
                    Execute(connection, "PRAGMA synchronous=FULL");
                    Execute(connection, """
                    CREATE TABLE operator_capability (
                      singleton INTEGER PRIMARY KEY CHECK(singleton=1), digest BLOB NOT NULL
                    )
                    """);
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO operator_capability VALUES (1,$digest)";
                        command.Parameters.AddWithValue("$digest", operatorDigest);
                        command.ExecuteNonQuery();
                    }
                    Execute(connection, $"PRAGMA application_id={HostIdentityApplicationId}");
                    Execute(connection, $"PRAGMA user_version={HostIdentitySchemaVersion}");
                }
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                CheckpointAndRemoveWalSidecars(tempPath);
                File.Move(tempPath, hostIdentityPath);
            }
            catch (IOException) when (File.Exists(hostIdentityPath))
            {
                // Lost a race with a concurrent creator (a different process; same-process races
                // are already serialized by HostIdentityGate above) between our own existence
                // check and our publish. The identity file is there now either way -- benign.
                TryDeleteCreationTempFiles(tempPath);
            }
            catch
            {
                TryDeleteCreationTempFiles(tempPath);
                throw;
            }
            if (!OperatingSystem.IsWindows() && File.Exists(hostIdentityPath))
                File.SetUnixFileMode(hostIdentityPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// Cross-file aggregation for the seven <c>CollectingXCampaigns</c> sweep-enumeration entry
    /// points, forced by the design doc's &#167;3 file-per-campaign topology: under the old
    /// file-per-host layout, "which campaigns are collecting" was one query against one file. There
    /// is no longer one file to run that query against (&#167;7 names this directly), so this lists
    /// every sibling campaign file in this instance's own directory -- every <see
    /// cref="CampaignFileExtension"/> file beside this one; the host-identity file deliberately uses
    /// a different extension so it is never mistaken for a campaign file here -- opens each with
    /// <see cref="OpenExisting"/>, asks it for every one of <i>its own</i> matching campaign ids via
    /// <paramref name="queryOneFile"/> (ordinarily zero or one, since production never creates more
    /// than one campaign per file, but this does not assume that -- a few existing tests
    /// deliberately stuff several synthetic campaigns into one file to exercise sweep-cursor
    /// behavior, and this aggregator produces the same cursor/limit semantics either way), and
    /// merges the union in the same order the single-file SQL used (ordinal/<c>BINARY</c>),
    /// honoring <paramref name="after"/> and <paramref name="limit"/> over the merged set.
    /// </summary>
    /// <remarks>
    /// Deliberately unoptimized: this opens every sibling file on every call (today, once per
    /// second per lane, from <c>CampaignCombatTimeoutService</c>). The design doc's &#167;7 names
    /// the real fix -- replacing this cross-file poll with per-grain Orleans reminders -- as the
    /// next phased-rollout step after this one (&#167;11), not part of this change; &#167;10 already
    /// expects the sweep to need its own pass once per-campaign files exist. This aggregator exists
    /// so the sweep keeps working correctly in the interim, not to make it fast.
    /// </remarks>
    private List<string> CollectingAcrossSiblingFiles(string after, int limit,
        Func<CampaignDurableAuthorityJournal, string, int, IReadOnlyList<string>> queryOneFile)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        string[] siblings;
        try { siblings = Directory.GetFiles(directory, "*" + CampaignFileExtension); }
        catch (DirectoryNotFoundException) { siblings = []; }
        var merged = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var sibling in siblings)
        {
            CampaignDurableAuthorityJournal instance;
            try { instance = OpenExisting(sibling, clock, contentResolver); }
            catch (InvalidOperationException)
            {
                // A file mid-publish (temp-file rename hasn't landed, so this never actually
                // matched the glob above anyway), one that just got deleted by a concurrent
                // archival pass, or one this build does not recognize. The sweep already tolerates
                // and retries individual campaigns next tick (CampaignCombatTimeoutService.SweepLane);
                // the same tolerance applies to a whole file it could not even open.
                continue;
            }
            foreach (var id in queryOneFile(instance, "", int.MaxValue))
                if (string.CompareOrdinal(id, after) > 0) merged.Add(id);
        }
        return limit >= merged.Count ? [.. merged] : merged.Take(limit).ToList();
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

    /// <summary>
    /// Non-throwing fast-path probe for <see cref="RequirePrivateDirectory"/>, used by <see
    /// cref="CreateNew"/>'s directory bootstrap to decide whether the locked slow path is needed.
    /// Never a source of false positives: a directory's permissions are set once, at creation, and
    /// never touched again by this code, so the only way this can disagree with a subsequent locked
    /// check is by under-reporting (e.g. observing a directory another thread is still creating, or
    /// one that does not exist yet at all) -- never by wrongly reporting a bad directory as good.
    /// </summary>
    private static bool TryRequirePrivateDirectory(string directory)
    {
        try
        {
            RequirePrivateDirectory(directory);
            return true;
        }
        catch
        {
            return false;
        }
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
