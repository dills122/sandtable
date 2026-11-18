# Durable journal operational diagnostics and recovery

This runbook closes the "operational diagnostics and documented recovery
procedure" requirement of `MVP-GATE-04` (durable local lifecycle) in
[the pre-alpha roadmap](../roadmap/pre-alpha-roadmap.md). It covers the
`Cna.OrleansHost` durable campaign journal only — `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.cs`
and `CampaignDurableAuthorityJournalStore.cs`. It does not cover Maproom,
the intelligence plane, or any other subsystem.

## Topology (as of the per-campaign file split)

Each campaign owns its own SQLite file under one configured directory
(`CampaignJournal:Path` in host configuration — a directory, not a single
file). Files are created lazily on first use and cached for the life of the
host process by `CampaignDurableAuthorityJournalStore`.

One additional small shared file lives in that same directory:
`_host-identity.db`, holding `operator_capability` and `reference_key` as
genuinely host-wide identity, authenticated once, before any campaign file
exists. It is not a per-campaign file and is not touched by per-campaign
recovery steps below.

There is no clustering support yet (§3.2 of the data-layer design doc defers
this): the host runs on a single machine, and all of a host's campaign files
live on that one machine's local disk. There is no remote/shared-filesystem
deployment mode to recover from.

## Normal lifecycle

- **Create**: `CampaignDurableAuthorityJournal.CreateNew` stages the new
  file under a temporary GUID-suffixed name, writes schema and initial
  rows, checkpoints its WAL into one file, then publishes it with an atomic
  `File.Move` to the real path (no-overwrite — it refuses to run at all if
  something already exists there; see "Unrecognized file" below).
- **Checkpoint**: happens automatically as part of every atomic publish
  (`CheckpointAndRemoveWalSidecars` runs `PRAGMA wal_checkpoint(TRUNCATE)`
  before the file is moved into place), so a published journal is always a
  single self-contained file with no `-wal`/`-shm` sidecars left behind.
- **Stop**: the host process exits; no explicit shutdown hook is required
  for journal correctness — every write that has committed is already
  durable and checkpointed per the above. Connection pooling is deliberately
  **off** (`Pooling = false` in `Connect`) specifically so a closed
  connection's native handle doesn't block the next checkpoint — see the
  code comment at that call site for the full history (PR #412 enabled
  pooling, PR #417 reverted it after three independent reproductions of
  broken checkpointing).
- **Resume**: `OpenExisting` re-opens a published file, reads `PRAGMA
  user_version`/`application_id`, and either opens it directly (current
  schema), migrates it in place under the same crash-safety envelope
  `CreateNew` uses (an old-but-recognized schema version), or refuses with
  operator guidance (anything else — see below). This is campaign-file
  resume, not host-process live-state resume: `TemporaryCampaignGrain`'s own
  in-memory state does not survive a process restart today
  (`SystemProgressionStopsForHumanAndRestartLosesTemporaryCampaign` is an
  existing, intentional test of that fact) — only the durable journal itself
  is recoverable across a restart.

## Fault scenarios and how to diagnose them

### The host won't start: "Journal is missing" / schema or application-id errors

`OpenExisting` throws with one of these messages when the file at the
configured path isn't a journal it recognizes:

- **"The file at the configured journal path could not be read as a SQLite
  database (it may be truncated, corrupted, or not a journal at all)."** —
  the file exists but isn't valid SQLite, or the read failed outright.
- **"The file's schema version (N) is not one this host build recognizes.
  This can mean the file was created by a newer host build, or that it
  never finished first-time initialization..."** — `user_version` doesn't
  match any version this build's migration mechanism can carry forward.
- **"The file does not carry this journal's application id; it is not
  recognized as a journal created by this host."** — `application_id`
  isn't `0x5343414A` ("SCAJ"); this file was never a campaign journal at
  all (it may be the shared `_host-identity.db`, application id `0x53434149`
  — "SCAI" — placed at the wrong path, or something unrelated).

**The host never guesses in any of these cases.** Guessing wrong on an
ambiguous file could destroy real campaign authority, so every one of these
is a hard stop with an `InvalidOperationException`, not a silent
reinitialization.

### Diagnose which case you're in

Run these against the file directly (replace `<path>`):

```bash
sqlite3 <path> "PRAGMA application_id;"      # expect 1398030666 (0x5343414A) for a campaign file
sqlite3 <path> "PRAGMA user_version;"        # the schema version this file claims
sqlite3 <path> "PRAGMA integrity_check;"     # structural corruption check
sqlite3 <path> "PRAGMA quick_check;"         # cheaper structural check
ls -la <path>*                               # check for leftover -wal/-shm sidecars
```

A published journal should never have `-wal`/`-shm` sidecars sitting next to
it (checkpointing removes them before publish) — their presence next to a
file that's supposedly "published" is itself a signal something didn't
finish cleanly, independent of what `integrity_check` says.

### The recovery decision tree

This is deliberately manual — there is no automated repair path, by design:

1. **File fails `integrity_check`, or isn't readable as SQLite at all**:
   treat as corrupt. Move it aside (don't delete — keep it for post-mortem),
   then either restore a known-good backup to the real path, or let the
   host create a fresh journal there (campaign history is lost either way
   unless a backup exists).
2. **`application_id` doesn't match (`0x5343414A` for a campaign file,
   `0x53434149` for `_host-identity.db`)**: this is not a journal this host
   wrote. Don't let the host touch it. Move it aside and investigate how it
   got there (likely a misconfigured path, or leftover file from an
   unrelated process) before deciding whether to restore a journal or let a
   fresh one be created.
3. **`user_version` is a recognized-but-old schema version**: nothing to do
   — `OpenExisting` migrates this automatically, under the same crash-safe
   staging/atomic-publish envelope `CreateNew` uses. If migration itself
   fails partway, the original file is untouched (the migration writes to
   its own temp file first) — rerun, or escalate if it fails repeatedly.
4. **`user_version` is unrecognized and not old-but-migratable**: likely one
   of two things — a newer host build's journal (if you're running an older
   build against newer data, redeploy the matching/newer host build instead
   of touching the file), or a file left by an **old, pre-fix** host build
   that crashed during first-time initialization (PR #408 made `CreateNew`
   itself crash-safe going forward; it does not retroactively repair a file
   written by a host build that predates that fix). In the second case, the
   file is not a complete journal and should be moved aside so a fresh one
   can be created.
5. **A campaign's file is simply missing (host restarted, file never
   existed)**: `CreateNew` will create it on next use; nothing to recover
   unless a backup should have existed and doesn't.

### A WAL/checkpoint timing failure during a schema migration

If `RunMigrations` or `CreateNew` throws "could not fully checkpoint its
write-ahead log; refusing to publish a journal that is not yet a single
self-contained file" — this is the exact symptom of the PR #412/#417
pooling regression if it ever recurs (a pooled connection's native handle
staying parked after `Dispose()` can block the checkpoint). Confirm
`Pooling = false` is actually in effect at the `Connect` call site before
assuming it's a new issue; this failure mode is specifically what that
setting prevents.

## Known gaps — be honest about these, don't over-claim

- **Arbitrary crash cuts are not fully proven.** The crash-safety envelope
  is tested at specific, deliberate kill points (temp-file-created,
  mid-schema, before-version-marking, before-publish — see
  `CreateNewCrashHook`'s call sites), not at every possible byte-level
  interruption point. A real power-loss event could still land somewhere
  untested.
- **No clustering, no remote filesystem support.** Everything above assumes
  one host process on one machine with local disk. There is no
  storage-follows-grain or shared-storage story yet (§3.2 of the data-layer
  design doc defers this explicitly).
- **No automated backup/restore tooling.** "Restore a known-good backup" in
  the decision tree above assumes an operator already has one from outside
  this system (e.g. a filesystem-level snapshot of the journal directory) —
  nothing in this codebase takes or manages backups today.

## See also

- `docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md` —
  the design this topology implements, including the capacity model and
  what's deliberately deferred (archival, per-grain reminders, snapshotting).
- `CampaignDurableAuthorityJournal`'s own doc comments, especially
  `RecoveryGuidance`, `CreateNew`, and `OpenExisting` — the exact source of
  the decision tree above; if behavior ever drifts from this document,
  the code comments are the more current source of truth and this file
  should be updated to match.
