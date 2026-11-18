# Data/persistence layer design for a 1000-concurrent-campaign ceiling

**Status:** design accepted; rollout steps 1-2 implemented. §9 (schema
evolution/migration) landed as #416; §3/§6 (per-campaign file topology,
`CampaignDurableAuthorityJournalStore`) landed as
[PR #427](https://github.com/dills122/sandtable/pull/427). Stress testing
the new topology (2026-11-18) also surfaced and fixed a real TOCTOU race in
directory bootstrap under concurrent first-access — see
[PR #429](https://github.com/dills122/sandtable/pull/429) — unrelated to
scaling but found via this rollout's own verification. Steps 3-6 (hot/cold
archival, sweep→per-grain reminders, snapshotting, load-test tuning) remain
not started; see §11. RC remains local-only, single-player, low-scale and is
**not** blocked on anything remaining in here landing — this is a post-RC
scaling design.

**Scope note on sources:** everything below that describes current behavior
was re-read directly from `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.cs`
(and its per-lane partial-class siblings), `CampaignCombatTimeoutService.cs`,
`LocalSessionStore.cs`, and `P01PrivateHostFoundation.cs` in this worktree,
plus issue #330 and PR #408 via `gh`, rather than taken on faith from the
request that prompted this document. One claim in that request did not hold
up and is corrected in [Appendix A](#appendix-a-corrections-to-the-originating-request).

**Note on scope versus the original brief:** after an initial draft, the
direction was sharpened with three explicit architectural requirements —
locking should fall out of grain ownership rather than being layered on top
of a shared file; the hot store should be bounded by *active* campaigns, with
completed campaigns aggregated into a separate cold store; and the right
mental model is **N stores split by domain and interaction level**, not one
database serving everything. Those three ideas turn out to be consistent with
and sharpen the direction this document was already heading (file-per-campaign,
grain-scoped locking) — none of them conflict with a hard constraint found
while reading the code. They are woven into every section below, led by a
new §2 that states the underlying principle before the rest of the document
applies it.

## 0. Assumption this whole document leans on

**Pre-RC, no real production data exists yet.** Every campaign journal file
on disk anywhere today is test/dev data. This document therefore does not
design a zero-downtime migration path, a dual-write bridge, or a backward-compatible
on-disk format for *existing* files — there is nothing real to preserve. If
that assumption is wrong (e.g. there's a pilot cohort's data somewhere that
matters), everything in §9 needs to be revisited before implementation starts.
Flagging this explicitly so a human can correct it.

---

## 1. What's actually there today (verified)

### 1.1 Schema and files

One SQLite file per Orleans host (`Microsoft.Data.Sqlite`), `ApplicationId =
0x5343414A`, `SchemaVersion = 14`. Created with `PRAGMA journal_mode=WAL` and
`PRAGMA synchronous=FULL` (`CampaignDurableAuthorityJournal.cs:64-65`). Every
connection (`Connect()`, line 1672-1685) additionally sets `PRAGMA
foreign_keys=ON`, `PRAGMA busy_timeout=5000`, and opens with `Pooling=false`,
`Cache=SqliteCacheMode.Private` — confirmed directly, not inferred. There is
no `wal_checkpoint` pragma anywhere in this file or any sibling in
`src/Cna.OrleansHost/Api/` — WAL growth and checkpointing are left entirely to
SQLite's automatic default (checkpoint at ~1000 pages). That is a real
operational gap at sustained write volume: an unbounded or badly-timed
auto-checkpoint pauses writers.

Tables: `campaigns` (one row per campaign, head pointer — **no terminal/status
column at all**, see §4.1), `accepted_steps` (full event-sourced history,
`PRIMARY KEY(campaign_id,sequence)`, hash-chained with SHA-256 —
`prior_hash`/`record_hash` columns, chain verified in `Load()`), and seven
"round" tables — `selection_rounds`, `rba_rounds`, `force_rounds`,
`cycle_rounds`, `reserve_rounds`, `custody_rounds`, `retreat_rounds` — each
`PRIMARY KEY(campaign_id, <round-specific hash column>)`, each with a `status`
column (`CHECK(status IN ('collecting','completed',...))`) and **no secondary
index** (`CampaignDurableAuthorityJournal.cs:135-217`). A fix adding
`status`/`campaign_id` indexes to these seven tables is already dispatched
elsewhere; this design assumes it lands and does not duplicate it — but see
§4 for why that fix only speeds up scanning an ever-growing pile rather than
stopping the pile from growing.

### 1.2 `Load()` is the dominant cost, and it's worse than "just slow"

`Load()` (`CampaignDurableAuthorityJournal.cs:1436-1611`) runs on **every**
write path and even on read paths (`ReadVerified`, line ~401). For a normal
(non-pregame-source) campaign it:

1. Reads the `campaigns` row.
2. Runs `SELECT ... FROM accepted_steps WHERE campaign_id=$id ORDER BY sequence`
   with no `LIMIT`, no cursor, no cache (line 1542-1549) — every row, every call.
3. Recomputes the SHA-256 hash chain row by row (`StepHash`, called once per
   row at line 1570) and checks it against the stored `record_hash`.
4. Replays the entire history through `CampaignHostAdapter.ImportWithContentResolver`
   (or the pregame-source/source variants).
5. Then calls `VerifyForceRounds`, `VerifyRbaRounds`, `VerifyCycleRounds`,
   `VerifyReserveRounds`, `VerifyCustodyRounds`, `VerifyRetreatRounds`,
   `VerifySelectionRounds` (lines 1603-1609) — each of which (see
   `VerifySelectionRounds`, `CampaignDurableAuthorityJournal.SelectionRounds.cs:217-320`)
   walks the **entire in-memory `steps` list again**, plus a full
   `SELECT ... FROM <round_table> WHERE campaign_id=$id` per lane with no
   `LIMIT`. So one `Load()` call is O(history) in at least nine separate
   passes, not one.

Measured (per the originating investigation, consistent with the shape of
the code above): ~200ms/call flat, independent of history length within the
range tested. **This genuinely does not depend on how much happened in a
single turn** — it depends on total campaign history since creation, which
only grows, forever, for the life of the file (§4.1).

### 1.3 The lock nests the expensive part inside itself, across unrelated campaigns

Confirmed directly at every call site, not just the pregame-source one cited
in the originating request — I counted 48 occurrences of
`connection.BeginTransaction(deferred: false)` in this one file (`grep -n`),
and **every single one** is immediately followed by a call to `Load(connection,
transaction, campaignId)` before any actual mutation happens. Two examples:
`CreatePregameSourceCampaign` (`CampaignDurableAuthorityJournal.cs:351-368`)
and `TickSelectionRound`
(`CampaignDurableAuthorityJournal.SelectionRounds.cs:87-101`). `BEGIN
IMMEDIATE` (what `deferred: false` produces) takes SQLite's single
database-wide `RESERVED`/write lock up front. So the O(history) replay in
§1.2 — all nine-plus full-table scans and the entire hash chain recompute —
happens *while holding the one write lock for the whole file*, for every
write **and every read** that goes through `Load()`. Two read-only entry
points (`ReadVerified`, `ReadSourceVerified` at lines 401/408) use
`BeginTransaction()` without `deferred: false`, so at least those two don't
force an immediate write lock — worth confirming in implementation whether
that's intentional, since it's the one place the pattern isn't uniform.

Crucially: **campaign A's write never logically needs to wait on campaign B's
write.** Nothing about the hash chain, the round-table state machine, or the
Orleans grain model requires cross-campaign ordering. The lock contention
described above exists purely because all 1000 campaigns' rows live in one
SQLite file, and SQLite grants exactly one writer per file at a time. That
distinction — real logical constraint versus an artifact of today's storage
layout — is the organizing idea for the rest of this document; see §2.

### 1.4 The sweep architecture

`CampaignCombatTimeoutService` (`src/Cna.OrleansHost/CampaignCombatTimeoutService.cs`)
is a `BackgroundService` with a 1-second `PeriodicTimer`. Each tick,
`SweepOnce` calls `SweepLane` seven times — once per round-table lane
(selection, rba, force, retreat, custody, reserve, cycle; lines 36-42).
`SweepLane` calls the lane's `CollectingXCampaigns(cursor, BatchSize)` to list
up to `BatchSize = 16` campaigns with `status='collecting'` past a cursor
(e.g. `CollectingSelectionCampaigns`,
`CampaignDurableAuthorityJournal.SelectionRounds.cs:103-119`:
`SELECT DISTINCT campaign_id FROM selection_rounds WHERE status='collecting'
AND campaign_id > $after COLLATE BINARY ORDER BY campaign_id COLLATE BINARY
LIMIT $limit`), then calls `TickXRound` once per campaign in that batch. Each
`TickXRound` call is a full `BeginTransaction(deferred: false)` → `Load()` →
maybe-write → `Commit()` cycle. So one sweep tick does up to `7 lanes × 16
campaigns = 112` separate `Load()` calls, each taking the single global write
lock. At ~200ms/`Load()`, 16 campaigns in one lane alone costs ~3.2-3.4s —
already longer than the 1-second tick period, confirming the stated
measurement.

### 1.5 Append-only forever, and never archived

Confirmed via `grep -rn "DELETE FROM" src/Cna.OrleansHost/Api/*.cs`: the only
`DELETE FROM` anywhere is in
`CampaignDurableAuthorityJournal.Openings.cs:607`, against `owner_actions` — an
unrelated table (grant/owner-opening management, not history). `accepted_steps`
and all seven round tables are never deleted from, and — confirmed by reading
the `campaigns` table definition at lines 67-75 — there is **no column
anywhere recording that a campaign has reached a terminal/finished/cancelled
state**, let alone any code path that acts on one. Table size — and `Load()`'s
and the per-lane verify passes' scan cost — grows monotonically with total
historical rounds and steps *ever* recorded, for every campaign that has ever
existed on that host, whether it's still being played or finished months ago.
This is the single biggest structural problem at 1000-campaign scale, and
indexing the round tables (the fix already in flight) does not touch it —
an index makes scanning a growing pile faster, it does not stop the pile from
growing. §4 proposes an actual answer.

### 1.6 Crash safety — corrected from the request (see Appendix A)

`CreateNew` and `OpenExisting` as they exist in this worktree today
(`CampaignDurableAuthorityJournal.cs:46-259`) do **not** yet have the
temp-path-plus-atomic-`File.Move` publish or the `RecoveryGuidance` classifier
described in the request that prompted this document. That work exists as PR
#408 (`fix(host): crash-safe first-time durable journal initialization`),
which is currently **open, not merged** (`gh pr view 408`: `state: OPEN`,
`mergedAt: null`). `CreateNew` today builds the schema directly at the real
configured path across multiple unguarded `Execute()` calls (lines 63-233) —
exactly the gap PR #408 describes fixing. Whatever design comes out of this
document should assume PR #408's intent (atomic publish, explicit
non-auto-repairing recovery classification) is the baseline to preserve or
improve on, since that's clearly the direction the project is already headed,
and this document's file-per-campaign/per-shard proposal (§2-§3) multiplies
how many files need exactly this guarantee, not fewer.

### 1.7 The dormant parallel schema, and prior art for both checkpointing and domain separation

`P01PrivateHostFoundation.cs` is a separate, disabled SQLite schema (own
`ApplicationId = 0x50303146`, `SchemaVersion = 3`), header-commented: "Disabled
private storage primitive. No service registration or HTTP route creates this
store." (lines 9-10). It's a different `campaigns` table shape (adds
`checkpoint_hash`, `origin_kind`, `activation_fence`/`activation_digest`) and a
separate `a1_imports` table for retained import/control/checkpoint/attestation
bytes per campaign. It is not live and nothing here depends on it, but it is
useful prior art in two ways: (a) a past design direction explored storing a
*single checkpoint blob per campaign* rather than only an event log —
directionally the same thing §5 proposes — and (b) it's a second, entirely
separate SQLite schema living beside the main journal, which is itself a small
precedent for "not everything goes in one file," even though it was never
wired in to actually run concurrently with the main journal.

One more pre-existing, narrower checkpoint mechanism, worth noting so this
document doesn't ignore it: `accepted_steps.snapshot_checkpoint` already
stores a per-step checkpoint blob, and there's a one-shot
`AdmitSourceCompactCheckpoint` / `POST /{campaignId}/source-compact-checkpoint`
path (`CampaignDurableAuthorityJournal.cs:662-679`,
`CampaignDurableOutwardApi.cs:208-215`). That's scoped to the one-time
pregame-source admission flow, not a general runtime snapshotting strategy for
ordinary gameplay replay — it doesn't reduce `Load()`'s cost for a normal
campaign. It does establish that storing a verified mid-stream checkpoint
blob next to the step row is an existing, tested pattern in this codebase,
which §5 builds on rather than invents from nothing.

### 1.8 Session/auth state is already a separate, non-SQLite domain

`LocalSessionStore` (`src/Cna.OrleansHost/Api/LocalSessionStore.cs:1-16`) is a
process-local, **in-memory** store (`Dictionary<string,...>` fields guarded by
a plain `lock (gate)`, not SQLite at all) for sessions, grants, and
redemptions — identity/auth scaffolding, bounded at `MaxSessions = 256`,
`MaxGrants = 512`, `MaxRedemptions = 256`. It shares no table, no file, and no
code path with `CampaignDurableAuthorityJournal`. This is confirmation, not
speculation: the project has already, independently, kept this domain
separate from campaign-journal storage. §2.3/§6 take this as an existing
constraint to preserve (and generalize) rather than a new idea to introduce —
if `LocalSessionStore` is ever made durable for a scaled deployment, it should
get its own dedicated store, never be folded into the campaign journal's
schema or file.

### 1.9 Issue #330 independently names this exact problem

Issue #330 ("RC follow-ups after native Combat deadline reliability
checkpoint") already has an open checklist item: "**Host scaling:** benchmark
scheduler sweep latency and immediate-transaction hold time across increasing
history lengths and >16 campaigns per lane. Define supported capacity/latency
budget before broader hosting... Optimize without weakening replay
verification." and a second: "**Archives compatibility:** define supported
historical read-only recovery and explicit migration policy for schemas 5-13
before promising writable upgrades; no implicit migration or fabricated
deadlines." This document is, in effect, the design-level answer to both of
those bullets — the second one lines up almost exactly with the hot/cold split
in §4. It should be linked from #330, not treated as a parallel untracked
effort.

---

## 2. The organizing principle: make the storage boundary match the concurrency boundary

This section states the architectural thesis that the rest of the document
applies. It responds directly to the three sharpened requirements.

### 2.1 Locking is an artifact of today's file layout, not a property of the data

Restating §1.3's finding as a general principle: **`accepted_steps` rows for
campaign A and campaign B are never in a race with each other.** Each
campaign already runs as exactly one Orleans grain activation, and Orleans
guarantees single-threaded execution per activation — so all writes to one
campaign's append stream are already serialized *before any SQL runs*, by the
grain, in memory. There is no real concurrent-write hazard on one campaign's
own history that a database lock is protecting against. The only reason a
lock currently touches cross-campaign concurrency at all is that SQLite's
single-file model forces every campaign through one shared
`RESERVED`/exclusive write lock, regardless of whether their writes have
anything to do with each other.

The design implication is not "find a cleverer lock" — it's **don't put
unrelated campaigns in the same lock's scope in the first place.** Append-only
streams (`accepted_steps`, and the round-table event rows, which are also only
ever appended-to or status-flipped by their own campaign's grain) need no
cross-request/cross-campaign locking at all once each is scoped to its owning
campaign. Within one campaign, a lock still has a job to do (see §6) — but
that job is "serialize this campaign against itself in the rare case two
activations briefly overlap," not "serialize 1000 unrelated campaigns against
each other," which is what happens today.

### 2.2 N stores split by domain and interaction level, not one monolithic database

Once storage is scoped to match the concurrency boundary, "one SQLite file"
stops being the natural unit, and several different stores fall out, each
sized and shaped for what actually touches it:

| Store | Scope | Interaction pattern | Who writes |
| --- | --- | --- | --- |
| **Active-campaign (hot) store** | One campaign, or a small shard of campaigns | Frequent, small, append-mostly writes; occasional full replay on load | The owning Orleans grain(s) only |
| **Historical (cold) store** | Completed/cancelled campaigns, aggregated | Rare writes (one, on archival), occasional reads (audit/export/dispute) | The archival process only, once per campaign |
| **Session/auth store** | Process-wide identity/grant state | Frequent, tiny, latency-sensitive reads/writes; no relationship to campaign history at all | `LocalSessionStore` (already separate today, §1.8) |
| *(future, if ever needed)* **Cross-campaign catalog/index** | All active campaigns | Read-heavy, admin/product queries ("how many campaigns are stuck") | Kept in sync by hot-store writers, queried by tooling |

This is the frame the rest of the document works within: §3 picks a
technology and a sharding granularity for the hot tier; §4 defines the
hot→cold transition; §5 is the snapshotting mechanism that keeps the hot tier
cheap *while a campaign is still active*; §6 shows how per-campaign scoping
makes the locking model in §2.1 concrete; §7-§8 are the indexing and
connection consequences; §9 is schema evolution across however many hot-tier
files now exist; §10 recomputes capacity against "active campaigns," not
"campaigns ever created."

No hard constraint found while reading the code conflicts with this framing.
The hash-chain integrity requirement is a property of a stream of bytes, not
of which file that stream lives in — splitting storage by campaign or moving
a finished campaign's stream to a different file doesn't change what the
chain proves, provided the chain itself is moved intact (§4.3). Orleans's
grain-per-campaign model is, if anything, what *enables* this split rather
than something it has to work around.

---

## 3. Technology recommendation

**Recommendation: keep SQLite, but change its topology from file-per-host to
file-per-campaign-or-shard for the hot tier, plus a separate SQLite store for
the cold tier. Do not move to Postgres.**

Reasoning, tied to §2 and this project's actual constraints:

- **The real bottleneck measured in §1.3/§1.4 is "1000 campaigns forced
  through one file's one global write lock," not "SQLite can't handle 1000
  writers."** Per §2.1, cross-campaign writes were never actually
  contending for the same logical resource — only for the same *file*.
  Splitting to one file per campaign (or a small shard of campaigns, if
  per-campaign file-handle count becomes a real OS constraint — see §8)
  removes that artificial contention without a new database engine: each
  campaign's grain owns its own file, so 1000 campaigns means 1000
  independently-lockable files instead of 1000-way contention on one.
- **The hash-chain is a correctness mechanism, not an audit log, and that
  argues for keeping storage simple, not for a server DB.** The chain's job is
  to let the host prove to itself (and to recovery/export logic) that a
  specific ordered sequence of bytes produced a specific result,
  deterministically. A server-based DB doesn't make that verification any
  cheaper or any more correct — the chain still has to be walked and
  SHA-256'd by the host process either way. What a server DB would buy is
  concurrent access to the *same* rows from *multiple* processes — which
  this architecture doesn't need, because each campaign is single-writer by
  Orleans's own grain model, not multi-writer. Postgres would be solving a
  concurrency problem SQLite plus correct scoping already solves for free.
- **The local-RC-vs-scaled-later duality favors not introducing an
  operational dependency the local build doesn't have.** RC ships
  SQLite-based, local-only, no server process to administer. Introducing
  Postgres post-RC would permanently fork the persistence code path (or force
  local single-player users to run a server for no local benefit). Changing
  SQLite's *topology* gets the scaling benefit without that fork.
- **1000 concurrent *active* campaigns is not a "big data" problem, especially
  once completed campaigns move to cold storage (§4).** Each campaign's active
  history (six-turn scenarios, bounded Combat profiles per
  `docs/roadmap/pre-alpha-roadmap.md`) is a few thousand rows at most. 1000
  small SQLite files is exactly what SQLite is good at; many concurrent
  writers to *one* file is what it struggles with — and per-campaign scoping
  means there is only ever one writer per file, matching §2.1 exactly.
- **The caveat, sharpened by §2.2's table:** this recommendation is
  specifically for the campaign-journal hot tier. The cold tier (§4) is a
  different interaction pattern (write-once, read-rarely) and could
  reasonably be a single shared SQLite file *if* its own write pattern (one
  archival transaction at a time, from one process) never becomes a
  contention point — worth confirming once archival volume is known, but
  unlike the hot tier it isn't competing with 1000 live grains for one lock,
  so the same urgency doesn't apply. If a genuine cross-campaign product
  query need emerges later (the catalog row in §2.2's table), a thin
  separate index — even in Postgres — is a far smaller addition than moving
  the source of truth off SQLite.

### 3.1 Why not just "scale the one file harder" (WAL tuning, etc.)

Rejected as the primary fix, though some of it (checkpoint policy, §8) still
matters even in the new topology. WAL mode already helps readers not block on
the writer, but SQLite still allows only one writer at a time *per file*, and
the write lock is what's being held for ~200ms+ per `Load()` call today.
Tuning `synchronous`/WAL checkpoint intervals changes durability/latency
trade-offs but cannot remove the fundamental one-writer-per-file ceiling that
file-per-host imposes once you have genuinely concurrent campaigns — that
ceiling is exactly the artifact §2.1 describes, and the fix is removing the
shared file, not tuning around it.

### 3.2 Clustering and grain placement (resolved by human decision, 2026-11-18)

A PR review of this document's first draft flagged a real gap: §3's
file-per-campaign recommendation is justified entirely by "Orleans already
serializes one grain's writes in-memory," which holds on a single host but
says nothing about what happens once the host is clustered. A campaign's
grain activation could, in principle, reactivate on a different silo; a local
SQLite file left on the old silo's disk would not follow it, and SQLite (even
in WAL mode) is not safe to run over a shared/network filesystem.

**Decision: for now, pin each campaign's grain permanently to the silo that
created it.** This keeps §3's file-per-campaign recommendation valid as-is —
one campaign, one grain, one silo, one local file — and avoids designing a
storage-follows-grain or shared-storage mechanism before there's a concrete
need for one. Concretely, this requires a placement mechanism keyed by
`campaign_id` that always resolves to the same silo for that campaign's
lifetime (e.g. a custom `IPlacementDirector`, or recording an explicit
`owning_host_id` on the `campaigns` row and routing calls there) — an
implementation detail for whoever builds the clustering phase, not resolved
further in this document.

**Explicitly accepted trade-off, not solved here:** this decision does not
provide failover. If the silo a campaign is pinned to goes down, that
campaign is unavailable until that silo comes back — it is not migrated to
another silo automatically. That is an acceptable gap *for now*, per the
human decision above, not an oversight; revisit if/when availability during a
single-silo outage becomes a real requirement (at that point, a
storage-follows-grain or shared-storage design would need to be built, and
§3's "keep SQLite" recommendation itself may need to be revisited alongside
it, since durable shared storage is exactly the kind of multi-writer problem
§3 argued SQLite doesn't need to solve).

---

## 4. Hot/cold split scoped per campaign, with archival on completion

This is treated as more fundamental than indexing (§7) or snapshotting (§5):
those make the existing ever-growing pile faster to scan or faster to replay,
but neither stops it from growing. This section is about keeping the pile
from growing in the first place, for data that no longer needs to be "hot."

### 4.1 There is currently no concept of a campaign being "done"

Checked directly: the `campaigns` table (`CampaignDurableAuthorityJournal.cs:67-75`)
has no status/outcome/terminal column, and no code path in
`src/Cna.OrleansHost/Api/*.cs` ever marks a campaign as finished, cancelled, or
otherwise concluded. This is a real gap the design has to fill, not an
existing lever to flip. Concretely, this document proposes:

- Add a `campaign_state` concept — most simply, a nullable `terminal_at_ms`/
  `terminal_reason` pair on the hot-tier `campaigns` row (or a new
  single-row-per-campaign `campaign_lifecycle` table if keeping `campaigns`'
  shape stable matters for other reasons) — written exactly once, by the
  owning grain, when `Cna.Core`'s engine reports that no further legal action
  can advance the campaign (victory condition met, explicit cancellation, or
  whatever the core engine's own terminal-state signal turns out to be —
  this document does not attempt to define that signal itself; it's a
  `Cna.Core` question, not a storage question, and is called out here as a
  dependency, not solved here).
- A campaign transitions to terminal **at most once**, written inside the same
  transaction as the step that caused the transition (no separate "detect
  terminal state" sweep needed — the grain already knows, the moment it
  happens, because it just replayed the step that caused it).

### 4.2 Hot store holds only active campaigns; cold store holds the rest

- **Hot tier** (§3's file-per-campaign-or-shard SQLite stores): campaigns
  without a terminal marker. This is the set the timeout sweep/reminders
  (§7), the write path, and `Load()` all operate against. Its size is bounded
  by **concurrently active campaigns**, not by total campaigns ever created —
  this is the capacity-model-relevant claim and is what makes the 1000-campaign
  ceiling a stable target rather than a moving one (§10).
- **Cold tier**: a separate store (plausibly one shared SQLite file, or one
  file per archived campaign under a different directory — either is fine
  given the write-once/read-rarely pattern noted in §3's caveat) holding the
  full, verified `accepted_steps` history, final round-table state, and the
  final head hash for every campaign that has reached terminal state. Nothing
  in the cold tier is ever mutated after archival; it exists for audit,
  export, dispute resolution, and "show me how this campaign actually
  unfolded," not for active gameplay.
- **Archival path**, triggered once by the terminal-state write in §4.1,
  inside the owning grain, as the last thing it does before it can be allowed
  to deactivate and stay deactivated:
  1. Re-run `Load()` one final time (or reuse the state already in hand from
     the step that just caused the terminal transition) to get the fully
     verified final state and head hash.
  2. Write the complete `accepted_steps` history (or, if a snapshot exists
     per §5, the latest verified snapshot plus only the steps since it — the
     cold copy still needs to be independently replayable from genesis, so
     either copy the full step history or copy enough snapshots to
     reconstruct it; copying the full step history is simpler and this
     project's six-turn-scenario campaign sizes make that cheap) plus final
     round-table rows into the cold store, inside one transaction.
  3. Verify the copy by replaying it from the cold-store copy and confirming
     the head hash matches **before** touching the hot-tier file — archival
     must never be the operation that loses data.
  4. Only after that verification succeeds, delete the hot-tier file (or, if
     sharded, delete that campaign's rows) and let the grain deactivate for
     good.
  5. If the process crashes between steps 2 and 4, the hot-tier file is still
     intact and still authoritative — the archival job is idempotent and can
     simply be retried; nothing is lost by re-running it, because step 3
     always re-verifies before step 4 ever deletes anything.
- **Reads of an archived campaign** (an operator or export tool asking "what
  happened in campaign X") are served from the cold store, not by reactivating
  the grain against a hot-tier file that no longer exists. This is a separate,
  simpler read path than the live `ReadVerified` entry point — it doesn't need
  write-lock semantics at all, since the cold copy is immutable once written.

### 4.3 This does not weaken the hash-chain guarantee

The chain is a property of the byte sequence, verified by replaying it and
recomputing SHA-256 at each step — moving that exact byte sequence from one
file to another, verified on both ends (write to cold, replay from cold,
compare head hash, *then* delete hot), does not change what it proves. The
one thing this *does* require that today's design doesn't need: the archival
step must be atomic-enough that a crash never leaves a campaign in a state
where it exists in neither tier, or exists in a way that disagrees between
tiers. §4.2 step 3-5 above (verify-before-delete, idempotent retry) is the
answer; it's the same "never let the real path show a half-done write" logic
PR #408 already establishes for `CreateNew` (§1.6), applied to a different
operation.

---

## 5. Snapshotting/checkpointing design

Goal: bound `Load()`'s cost independent of total history length *for a still-active
campaign*, without weakening the hash-chain's ability to prove the full
history is intact. This matters even with the hot/cold split in §4, because a
campaign can stay active (and keep accumulating history) for a long time
before it ever reaches terminal state — archival bounds the *number* of
campaigns in the hot tier, snapshotting bounds the *replay cost* of each one
still in it.

### 5.1 Mechanism: periodic signed snapshot rows, chain stays authoritative

Add a new `snapshots` table (one per campaign-file under the file-per-campaign
topology of §3):

```
CREATE TABLE snapshots (
  campaign_id TEXT NOT NULL,
  sequence INTEGER NOT NULL,       -- the accepted_steps.sequence this snapshot covers through
  chain_hash TEXT NOT NULL,        -- record_hash of accepted_steps row at `sequence` (the chain value being vouched for)
  state_bytes BLOB NOT NULL,       -- serialized replay-equivalent state (reuses the existing snapshot_checkpoint shape)
  round_state_bytes BLOB NOT NULL, -- serialized status/high-water for all 7 lanes as of `sequence`
  snapshot_hash TEXT NOT NULL,     -- sha256 over (campaign_id, sequence, chain_hash, state_bytes, round_state_bytes)
  PRIMARY KEY(campaign_id, sequence)
)
```

- A snapshot is **not** a replacement for any `accepted_steps` row and nothing
  is ever deleted from the hot tier while a campaign is active (that only
  happens at archival, §4.2, and even then only after the cold copy is
  verified). The chain is still the ground truth; a snapshot is a cached,
  independently verifiable claim about what replaying the chain up to
  `sequence` produces. `snapshot_hash` binds the snapshot to the exact chain
  value at that point (`chain_hash`), so a snapshot can never be silently
  substituted for a different history — if the chain and the snapshot
  disagree, that's a hard integrity failure, exactly like today's "Campaign
  journal record changed" check.
- `Load()` changes to: find the newest `snapshots` row, verify `snapshot_hash`
  and that `chain_hash` matches the `accepted_steps` row at that `sequence`
  (**one row read, not a full-table scan** — O(1) instead of O(history) for
  this step), deserialize `state_bytes`/`round_state_bytes` as the starting
  point, then replay only `accepted_steps WHERE campaign_id=$id AND sequence >
  $snapshotSequence ORDER BY sequence` — i.e., only the steps since the last
  snapshot. Cost becomes O(steps-since-snapshot), not O(total-history).
- **This preserves full verifiability.** A slower, explicit "verify from
  genesis" operation (export/audit/dispute-resolution tooling, or the cold-tier
  archival-verification step in §4.2) can ignore `snapshots` entirely and
  replay `accepted_steps` from sequence 1, recomputing the whole chain exactly
  as `Load()` does today. The snapshot is purely a performance cache for the
  hot path; the full chain remains on disk, append-only, for as long as the
  campaign stays in the hot tier, and remains the thing that's actually
  authoritative.
- **When to take a snapshot:** after any write where
  `current_sequence - last_snapshot_sequence >= N` (tunable; start around
  N=50-100). Taking a snapshot is cheap: a single-row insert using state the
  write path already has in memory immediately after a successful `Load()` +
  replay — no extra query needed.
- **Why not reuse `accepted_steps.snapshot_checkpoint`/the existing
  compact-checkpoint flow (§1.7) directly instead of a new table:** that
  column/flow is scoped to the one-time pregame-source admission path and
  isn't indexed or queried as "give me the latest one for this campaign" —
  repurposing it would conflate a setup-phase concept with an ongoing
  runtime-performance concept.
- **Pruning old snapshots (not old `accepted_steps` rows):** once a newer
  snapshot exists and has been durably committed, older snapshot rows add no
  value and can be deleted — this is a derived/cache table, not the
  event-sourced history, so deleting from it doesn't compromise the
  append-only guarantee in §1.5. Keeping the last 2-3 snapshots rather than
  just 1 is a reasonable safety margin against a corrupt/unverifiable latest
  snapshot.

### 5.2 What this does and doesn't fix

Fixes: `Load()`'s cost on the hot path stops growing with how long a campaign
has been active. Combined with per-campaign scoping (§3), a heavily-played
campaign no longer taxes *other* campaigns at all (no shared lock) and no
longer taxes *itself* unboundedly either (bounded replay window) while it
remains active.

Doesn't fix on its own: the nine-plus-pass structure inside `Load()` (full
history replay plus seven separate `VerifyXRounds` passes, each walking the
same `steps` list again, §1.2 point 5). Snapshotting the round-lane state
(`round_state_bytes` above) alongside the engine replay state means those
`VerifyXRounds` calls also only need to check consistency for steps since the
snapshot — but the verify functions themselves need to be adapted to accept a
"verify from here" starting index rather than always starting from `steps[0]`.
That's real implementation work, not a free consequence of adding a
`snapshots` table; flagging it so it isn't assumed away.

---

## 6. Concurrency/locking model

**Per-campaign exclusive write locking, no global lock, falling directly out
of §2.1 rather than being designed as a scheme layered on top of a shared
file.**

- Each campaign's SQLite file has its own independent SQLite-level write
  lock. Orleans already guarantees single-threaded access per grain
  activation, so in the common case there is *never* lock contention on a
  campaign's file — the grain is the only thing that ever opens it for
  writing, and Orleans has already serialized concurrent calls into that
  grain before any SQL runs. The lock SQLite still takes is now doing exactly
  the job described in §2.1 — guarding one campaign against itself in a rare
  edge case — not the job it's doing today, which is serializing 1000
  unrelated campaigns against each other.
- `BEGIN IMMEDIATE` per write is still appropriate *within* a single
  campaign's file — it protects against, e.g., two activations of the same
  grain briefly overlapping during deactivation/reactivation, or an
  out-of-band tool (export, archival-verification) touching the file
  concurrently with the live grain. The difference from today is what's
  *inside* that lock: with snapshotting (§5), the lock is held for "verify one
  snapshot row + replay a bounded tail," not "replay and re-verify the entire
  history across nine passes." The nesting problem described in the
  originating request (expensive replay inside the exclusive lock) is
  addressed by shrinking what's expensive and shrinking the lock's blast
  radius to one campaign, not by trying to take the lock later — taking the
  lock later would reintroduce a race where the replay result could be stale
  by the time the lock is acquired, which is worse.
- **Read/write separation:** WAL mode already lets readers (e.g.
  `ReadVerified`) proceed concurrently with an in-progress writer without
  blocking, within one campaign's file. That property is kept as-is. The two
  read-only entry points noted in §1.3 that don't use `deferred: false`
  already get this benefit today and should keep not forcing an immediate
  write lock.
- **Sweep/reminder concurrency:** with per-campaign files, the timeout
  mechanism's potential 112 `Load()`+write calls per tick (§1.4) each touch a
  *different* file (or no file at all, if replaced by per-grain reminders —
  see §7). There is no shared lock between them, so they run genuinely
  concurrently, bounded only by a worker pool / Orleans's own scheduling —
  not by one file's single writer. This is the single biggest lever on the
  capacity model in §10: today's cost is dominated by forced serialization on
  one lock; removing that lets the per-campaign cost (now also bounded by
  snapshotting) become the only remaining limit, and that cost parallelizes
  trivially across campaigns since, per §2.1, they share nothing to contend
  over.

---

## 7. Indexing/query-pattern review

Assuming the in-flight `status`/`campaign_id` index fix on the seven round
tables lands (not duplicated here, and understood per §4 to be necessary but
not sufficient — it speeds up scanning the pile, it doesn't stop the pile
growing): is "cursor-paginated full-table sweep every second" still the right
shape at 1000-campaign scale? **No — this is the one place this document
recommends changing today's query shape outright, not just tuning it,** and
the hot/cold and per-campaign-file decisions above force the question anyway.

- **Forced by §3/§6:** today's sweep queries (`CollectingSelectionCampaigns`
  etc.) run `SELECT DISTINCT campaign_id FROM selection_rounds WHERE
  status='collecting' ...` against the single host file. Under
  file-per-campaign, there is no longer one file to run that query against —
  "which campaigns currently have a collecting selection round" becomes a
  cross-file question with no single table to scan.
- **Recommendation: replace the cross-file poll with per-grain Orleans
  reminders, rather than inventing a cross-file catalog to keep polling.**
  Since each campaign's deadline state already lives in its own grain
  activation (Orleans holds it in memory while the grain is active), the
  grain can self-schedule its own `TickXRound` via a reminder/timer tied to
  its own `deadline_ms`, instead of a central service asking every file,
  every second, "are you still collecting?" Orleans reminders are designed to
  survive grain deactivation specifically so a deadline still fires for an
  idle campaign — which covers the one case a pure in-memory-only scheme
  would miss. This removes the sweep's per-tick fan-out entirely for active
  grains and sidesteps needing a cross-campaign catalog just to answer a
  scheduling question.
  - A denormalized cross-campaign catalog (`(campaign_id, lane, status,
    deadline_ms)`) is still a reasonable thing to build *if* a real product
    need for cross-campaign queries shows up (admin dashboards, "how many
    campaigns are stuck") — but that's the §2.2 "future, if ever needed" row,
    a different requirement than deadline scheduling, and shouldn't be built
    just to keep today's sweep pattern alive past the point where the
    topology it was built for still exists.
- **Unforced, still worth a human decision:** `BatchSize = 16` and the
  1-second tick were presumably tuned empirically against today's
  single-file contention. If per-grain reminders are adopted, those
  constants stop being meaningful (each grain schedules itself); they aren't
  reused as-is in the new model.

---

## 8. Connection management model

**Per-campaign-grain-activation connection, `Pooling=false` kept, no shared
pool across campaigns or across the hot/cold boundary.**

- Today's `Pooling=false` (confirmed, §1.1) presumably avoids SQLite's pool
  handing back a connection that still thinks it holds a stale lock on a file
  another part of the process also opens — reasonable self-defense when every
  campaign shares one file and pooling could blur which logical operation
  "owns" the lock at a given moment.
- Under per-campaign files, that specific risk mostly disappears — each
  campaign's grain activation is the sole owner of its file's connections,
  and Orleans's single-threaded-per-activation guarantee means there's no
  concurrent caller within one campaign to confuse a pooled connection about
  lock ownership. Pooling *could* become safe to enable, per-campaign-file,
  but the benefit is small: SQLite connection open/close against a small
  local file is cheap, and the grain activation lifecycle (create on
  activation, dispose on deactivation) is already a natural connection
  lifetime that doesn't need pooling to be efficient. Recommendation: **leave
  `Pooling=false`, scoped per campaign file rather than per host** — don't
  spend effort re-introducing pooling for a workload where it buys little.
- **At 1000 concurrent *active* campaigns, that's potentially 1000
  simultaneously open SQLite file handles** (one per active grain
  activation). §4's hot/cold split directly reduces this exposure versus a
  design with no archival: the 1000-campaign number is "active," and
  campaigns that finish stop holding a hot-tier handle at all once archived.
  Still worth an explicit budget check against OS file-descriptor limits and
  Orleans's own grain activation/idle-collection policy — a parameter to tune
  once real concurrency numbers exist, not solved in this document.
- The cold store (§4.2) is written by a separate, infrequent archival
  process, not by campaign grains, so it doesn't compete for the same
  per-grain connection budget at all — another reason to keep it a distinct
  store (§2.2) rather than co-located with the hot tier.

---

## 9. Schema evolution/migration strategy (answering #330)

Today: zero mechanism. "Migration" means parallel read-only legacy-schema
branches inside `Load()`/`OpenExisting()` (the `LegacySchemaVersion`,
`TimingSchemaVersion`, ... `SelectionSchemaVersion` constants and the
`version is not (SchemaVersion or RetreatSchemaVersion or ...)` check at
`CampaignDurableAuthorityJournal.cs:255`) — never promoting an old file
forward. That's sustainable only as long as the number of legacy branches
stays small and nobody needs to write to an old file.

Given §0's assumption (no real production data exists yet), and now with
potentially many hot-tier files plus a cold tier instead of one file, the
proposal is:

- **Adopt an explicit, numbered forward-migration mechanism now, before the
  snapshotting/per-campaign-file/hot-cold changes ship, so those changes *are*
  the first real migrations run through it** rather than becoming more
  special-cased legacy branches. Concretely: a small ordered list of
  `(fromVersion, toVersion, Action<SqliteConnection, SqliteTransaction>)`
  steps, applied transactionally on `OpenExisting` when `user_version` is
  older than current — actually `ALTER TABLE`/data-transform the file in
  place, inside the crash-safety envelope PR #408 is building (temp-file
  staging, atomic publish), rather than only ever adding new read branches.
  This mechanism needs to run per hot-tier file (there could be many now) and
  separately needs a decision about whether the cold tier is ever migrated at
  all — its records are immutable history, not live state, so a strong
  default is "cold-tier records are versioned and read with the schema they
  were written under, forever, rather than migrated" (simpler, and consistent
  with never mutating archived data), revisited only if that turns out to be
  unworkable.
- Because there's no real data to protect yet, this can be built and tested
  against synthetic fixtures at every prior schema version without any
  zero-downtime or dual-write complexity — exactly the over-engineering this
  document is explicitly told to avoid. If real data exists by the time this
  is implemented, that assumption needs re-checking and the migration
  mechanism likely needs a dry-run/backup-first mode added — but that's a
  future amendment, not a reason to delay building the mechanism itself.
- Keep today's legacy read-only branches as-is for files already at old
  versions that predate this mechanism's introduction; new schema changes
  from this point forward go through the migration mechanism instead of
  growing another legacy branch.

---

## 10. Capacity model (back-of-envelope)

Inputs, all from §1's direct measurements/reads:

- `Load()` today: ~200ms flat, O(history), **serialized** behind one
  global write lock.
- Sweep: 7 lanes × 16 campaigns/lane/tick = up to 112 `Load()`+write cycles
  per 1-second tick, all serialized today → ~22.4s of lock-holding work
  crammed into a 1-second budget at just 112 campaigns touched — already
  failing badly below 1000.

**After the status-index fix alone (already in flight, no `Load()` or
topology change):** helps the `CollectingXCampaigns` *enumeration* queries
(index-seeks instead of table-scans) but does **nothing** for `Load()` itself
(dominated by replay/verify passes, not the status lookup), nothing for the
cross-campaign lock nesting (§1.3), and nothing for unbounded host-lifetime
growth (§1.5/§4.1). **This is the headline finding here: the index fix in
flight fixes discovery, not the bottleneck that actually dominates cost, and
not the bottleneck that actually dominates long-term size.**

**After per-campaign files (§3) + snapshotting (§5) + per-grain reminders
(§7) + hot/cold archival (§4):**

- Lock contention across campaigns: eliminated by construction (separate
  files, §2.1/§6).
- `Load()` cost per active campaign: bounded by steps-since-last-snapshot
  (tunable N≈50-100), not total history — low-single-digit-ms to tens-of-ms
  depending on per-step payload size and N, down from ~200ms flat. (Estimate,
  not a measurement — needs a real benchmark once implemented, per issue
  #330's own ask.)
- **Hot-tier size: bounded by concurrently-active campaigns (the actual
  1000-campaign target), not by total campaigns ever created.** This is the
  capacity-model-level payoff of §4: without archival, "1000 concurrent
  campaigns" silently becomes "however many thousand campaigns have ever been
  played, all still sitting in the hot path" after enough calendar time —
  which is a moving, worsening target, not a stable ceiling. With archival, it
  stays exactly what the 1000 number is meant to mean.
- At 1000 concurrent active campaigns, each doing occasional (not constant)
  decision-round activity, with no single shared resource serializing them,
  the realistic story becomes "however many campaigns' worth of occasional
  per-campaign work the host's CPU/IO/Orleans activation budget can carry
  concurrently" — a normal scaling question, not "one lock, 1000 contenders."
- **Remaining bottleneck(s) even after all of the above:**
  1. **WAL checkpoint behavior is still unspecified** (§1.1) — up to 1000 hot
     files each relying on SQLite's implicit default checkpoint timing is
     1000 independent "surprise pause while a writer waits for an
     auto-checkpoint" risks instead of one. Needs an explicit policy (e.g.
     periodic `PRAGMA wal_checkpoint(PASSIVE)` per file) before this is
     "solved," not just "better." Archival in §4 also needs its own WAL
     checkpoint before a file is deleted, so the two bottlenecks share a fix.
  2. **File-descriptor and grain-activation budget** (§8) — up to 1000
     potentially-concurrently-active campaign files is a real OS/process
     resource. Reduced in absolute terms by archival (§4) versus a
     no-archival design, but still needs measurement against actual Orleans
     activation idle-collection settings and host OS limits.
  3. **The archival path itself (§4.2) becomes a new piece of machinery that
     has to be correct** — it's the thing standing between "hot tier stays
     bounded" and "hot tier silently grows again because archival is buggy or
     never runs." Worth its own test coverage and monitoring (e.g. "count of
     terminal-but-not-yet-archived campaigns" as an operational metric) once
     built — flagging as a new operational surface this design introduces,
     not something that comes for free.
  4. **The nine-plus-pass structure inside `Load()`/`VerifyXRounds`** (§5.2) —
     snapshotting bounds the *history length* each pass walks, but someone
     still has to thread a "start from here, not from step 0" parameter
     through all seven `VerifyXRounds` functions. Real implementation work,
     not a free consequence of adding a `snapshots` table.

---

## 11. Phased rollout plan

**Nothing here blocks RC.** RC is local-only, single-player, SQLite-based,
and low-scale by its own definition (`docs/roadmap/pre-alpha-roadmap.md`
MVP-GATE-04: "durable local lifecycle" for bounded local campaigns, not
concurrent-at-scale hosting). The honest ordering:

**Before/at RC (already in flight or already scoped, not new work from this
document):**
- The status/campaign_id index fix on the seven round tables (in flight,
  don't duplicate).
- PR #408's crash-safe `CreateNew`/`OpenExisting` (currently open — land or
  don't independently of this document; it's unrelated to scaling and is
  valuable at any scale, and its guarantee needs to extend to however many
  hot-tier files exist post-RC).
- Issue #330's existing "Host scaling: benchmark..." and "Archives
  compatibility..." checklist items are explicitly **not** satisfied by this
  document alone — this document is the design; #330 should link to it, and
  the benchmark it asks for should happen against a prototype before anyone
  commits to the numbers in §10.

**Post-RC, purely a scaling milestone (this document's actual payload, in
dependency order):**

**Re-sequenced 2026-11-18 — see [Appendix B](#appendix-b-snapshotting-5-is-blocked-not-step-1):
§5 as originally scoped is blocked on a `Cna.Core` capability that does not
exist. It is no longer step 1. The order below reflects that.**

1. **Schema evolution/migration mechanism (§9)** — **Done**, #416.
   Independent of everything else, and every later step here is itself a
   schema/topology change existing code has no mechanism to apply to an
   existing file (issue #330's "no implicit migration" concern).
2. **Per-campaign (or per-shard) file topology (§3/§6)** — **Done**,
   [PR #427](https://github.com/dills122/sandtable/pull/427)
   (`CampaignDurableAuthorityJournalStore`, host-identity routing/caching).
   The real concurrency unlock. Verified via dedicated stress testing at
   N=10 through N=1000 (2026-11-18): lock-contention elimination holds,
   `_host-identity.db` is not a new bottleneck, no FD leaks at N=1000. That
   same stress test found and led to fixing a genuine TOCTOU race in
   directory bootstrap — [PR #429](https://github.com/dills122/sandtable/pull/429).
3. **Define the terminal-state signal and the hot/cold archival path (§4)**
   third — depends on (2) existing (archival deletes/moves a hot-tier file,
   which needs to already be per-campaign for that to be a clean operation)
   and on a `Cna.Core` answer for "what counts as terminal," which is a
   dependency this document surfaces but doesn't resolve.
4. **Replace the cross-file sweep with per-grain reminders (§7)** fourth —
   naturally follows (2), since the sweep's current shape only makes sense
   while there's one file to poll.
5. **Snapshotting (§5)** — demoted from step 1 to here, and now contingent on
   a separate `Cna.Core`-owned design spike (Appendix B) rather than being a
   storage-layer implementation task. May never land in the form originally
   scoped; (1)-(4) above do not depend on it.
6. **WAL checkpoint policy and FD/activation budget tuning (§10 remaining
   bottlenecks 1-2), plus archival observability (§10 bottleneck 3)**
   alongside real load testing, once 1-4 exist to load-test against.

---

## Appendix A: corrections to the originating request

In the interest of not restating unverified claims as fact (per this
document's own instruction to read the code directly rather than trust a
summary):

- **PR #408 is not merged.** The request describing this design task stated
  "The journal has real crash-safety work already merged (PR #408)." Verified
  via `gh pr view 408 --repo dills122/sandtable`: `state: OPEN`, `mergedAt:
  null`. Confirmed independently by `grep` for `RecoveryGuidance`/`File.Move`
  across `src/Cna.OrleansHost/` in this worktree: no matches. §1.6 above
  treats PR #408's described *design intent* as the right baseline to build
  on regardless, since merge status doesn't change whether the design is
  sound — but implementers picking this up later should re-check #408's
  status rather than assume it has landed.
- Everything else in the originating request's "Current architecture" section
  (table names, line ranges, the sweep's `BatchSize`/tick period, the lack of
  indexes on the seven round tables, the single `DELETE FROM` on
  `owner_actions`, `Pooling=false`, `busy_timeout=5000`) was independently
  re-verified against this worktree while writing this document and held up
  as described, with two additional nuances the request's summary didn't
  distinguish: (a) two read-only entry points (`ReadVerified`/`ReadSourceVerified`)
  don't force an immediate write lock (§1.3), and (b) there is no terminal/status
  concept on the `campaigns` table at all today (§4.1), which matters once
  archival is on the table.

---

## Appendix B: snapshotting (§5) is blocked, not step 1

An implementation attempt on §5 (2026-11-18) stopped before writing any
journal-layer code, and correctly so. It traced the actual engine code rather
than inferring from this document's prose, and found that §5.1's load-bearing
assumption — that `Load()` can "deserialize `state_bytes`/`round_state_bytes`
as the starting point, then replay only steps since the snapshot" — does not
hold against `Cna.Core` as it exists today:

- `CampaignHostAdapter.ImportWithContentResolver` → `ImportCore` → `ImportStarted`
  always calls `Create(request, resolver)` (a **fresh** engine instance from
  genesis) and then replays the **entire** provided `steps` list. No
  skip-ahead/resume-from-mid-stream entry point exists anywhere in that call
  chain.
- `ExerciseSession`'s `CurrentHistory` (the full parsed event history since
  creation) is not disposable bookkeeping — `CampaignExercises.cs` computes
  `HashFramed(CurrentHistory)` as the engine's own re-adjudication/verification
  hash in `ReconstructOne`/`Reconstruct`. Bounding replay cost by discarding
  pre-snapshot history would change what that hash proves, not just how fast
  it's computed.
- `ExerciseSession`'s gameplay constructor also takes ~25 optional
  "mid-round-resolution" fields (`combatSelection`, `combatBarrage`,
  `combatRetreatMove`, `combatForceSeals`, `combatCustodyState`, `repeatMove`,
  `cycleControl`, etc.), none of which have a serialize/deserialize format
  today. A snapshot taken at an arbitrary point (every N steps, per §5.1) can
  land mid-sequence in any of the seven round lanes, and nothing today can
  capture or restore that in-flight state.
- The one existing checkpoint precedent this document cites in §1.7
  (`AdmitSourceCompactCheckpoint`) does not contradict this — it still appends
  to `PregameHistory` rather than compacting it, and gates "no further pregame
  edits," not "skip replay." It is not a usable precedent for general
  mid-campaign resumption.

**Consequence:** the `snapshots` table schema in §5.1 remains sound as a
*storage shape*, but there is nothing in `Cna.Core` for `Load()` to call that
would make it bound replay cost. Building the table without a consuming
mechanism would be dead weight, or worse, an invitation to wire up an unsafe
shortcut later. §5 as scoped is not implementable as a storage-layer task.

**What would actually be needed, if this is still wanted:** a `Cna.Core`-owned
design spike, not a follow-up implementation PR, resolving two separable
questions first: (a) what hash scope the seven lanes' completion/verification
hashes actually require — full-genesis, or can a window-since-snapshot scope
be made equally sound — and (b) whether snapshot points can be restricted to
provably "clean" lane-boundary instants (no in-flight mid-round-resolution
state), or whether all ~25 mid-round fields need a real serialization format.
Either answer is substantial, correctness-adjacent engine work owned by
whoever knows the native-combat/round state machine best — not something a
storage/journal-layer PR should decide unilaterally.

**Update 2026-11-18 — that spike is done:** see
[`2026-11-18-campaign-replay-snapshotting-design.md`](2026-11-18-campaign-replay-snapshotting-design.md).
It re-verifies the three findings above against the current code (confirming
finding 1 as stated; refining finding 2 — `HashFramed`/`Reconstruct` turn out
to be consumed only by `Cna.ExerciseRunner`'s export tooling, never by
`Load()`, so the real full-history blocker on the hot path is
`CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents` instead, a dependency
Appendix B did not name; and confirming finding 3 as an undercount — it's 33
constructor fields, not ~25, plus eight more fields that are recursive
pointers to whole prior `ExerciseSession` objects, added by #394 after this
Appendix was first written). It answers (a) yes, a window-since-snapshot scope
is achievable with no weakening of `DET-001`/`DET-002`/`EVT-001` — `REL-001`
is what licenses the split between `Load()`'s hot resume and a from-genesis
audit path — provided the ammunition ledger is seeded explicitly instead of
re-derived; and (b) clean lane-boundary instants occur often enough to make
"snapshot only at clean boundaries" the recommended policy, so no mid-round
serialization format needs building. It also names a tradeoff this Appendix's
original framing left implicit: `Load()` moves from "every call re-proves
correctness from genesis" to "every call trusts the last snapshot, with only
a separately-scheduled from-genesis audit able to catch a wrong-but-consistent
snapshot" — see that document's §5.4.

This does not block anything else in §11's rollout — items 1-4 there do not
depend on §5, and are resequenced ahead of it accordingly.
