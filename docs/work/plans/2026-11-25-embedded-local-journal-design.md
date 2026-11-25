# Embedded local journal mode (owner decision D7): design and spike

**Status:** design plus additive spike, 2026-11-25. Phase 1 of D7. No change to the default
Postgres + Marten path, `Program.cs`, or the Stage 3 hot files.

**Labels:** VERIFIED means read in code or run in this session. DOC means taken from a repo document.
ASSUMED means not checked here.

**Decision being implemented.** D7 (2026-11-25): provide an embedded local mode behind the existing
journal store abstraction for local play; Postgres stays for server and multi-game use.

## 1. Answer in brief

1. There is no `IJournalStore` interface. The "abstraction" is the concrete class
   `CampaignDurableAuthorityJournalStore`, which has three constructors (directory of SQLite files,
   Marten backend, single test journal). (VERIFIED, `CampaignDurableAuthorityJournalStore.cs:73-210`.)
2. The journal's ~6,000 lines of adjudication-orchestration logic are relational SQL over a
   `Func<SqliteConnection>`. The Postgres backend does not reimplement that logic: it loads a
   campaign's persisted state into a private in-memory SQLite database, runs the unchanged journal
   against it, and flushes the result to Postgres in one transaction
   (`CampaignJournalWorkingSet`). (VERIFIED, `CampaignJournalWorkingSet.cs:120-170`,
   `CampaignDurableAuthorityJournal.WorkingSet.cs`.)
3. So an embedded backend is not a new implementation of a storage contract. It is the SQLite file
   store that already exists, running the same SQL against a real file instead of a scratch
   in-memory database. It is the baseline the Marten backend was proven equivalent to. (VERIFIED,
   `CampaignDurableAuthorityJournalTests.MartenEquivalence.cs` header.)
4. **Recommendation: promote the existing file-per-campaign SQLite store to a supported embedded
   mode, with Orleans localhost clustering and the in-memory reminder service, plus one new piece:
   a boot re-arm of durable deadlines.** The re-arm is the only real gap the code review found, and
   the spike implements and tests it.

## 2. What the store actually has to provide

| Requirement | How the file store provides it | Status |
| --- | --- | --- |
| Append with optimistic concurrency | Head compare-and-swap: `UPDATE campaigns SET head_sequence, head_hash WHERE head_sequence=$priorSeq AND head_hash=$priorHash` inside a `BEGIN IMMEDIATE` transaction (`CampaignDurableAuthorityJournal.cs:1195`). Orleans additionally gives one writer per campaign grain. | VERIFIED |
| Per-campaign ordering | `accepted_steps` keyed by `(campaign_id, sequence)`; one file per campaign, so there is no cross-campaign lock. | VERIFIED |
| Hash-chain verification | `ReadVerified` replays every step and checks `prior_hash`/`record_hash` (`InitialHash`, `StepHash`, domain tags `campaign-durable-authority-v3:creation` and `:step`). Verification runs on every read, not only on open. | VERIFIED |
| Deadline timers | Round tables carry write-once timing columns. `CampaignGrain` drains round-open/close signals after commit and arms `CampaignRoundTimeoutGrain`. `GetRoundDeadlineMsAsync` and `TickRoundAsync` already branch on file versus Marten. | VERIFIED |
| Checkpoint/recovery | Crash-safe `CreateNew` (temp file, WAL checkpoint, atomic `File.Move`), `OpenExisting` with in-place migrations from schema 5 to 14, `Pooling=false`, `application_id`/`user_version` guards. Runbook: `docs/runbooks/durable-journal-recovery.md`. | VERIFIED |
| Operator identity | `_host-identity.db` in the same directory, first writer wins (`EnsureHostIdentity`). | VERIFIED |
| Store-level API | The store's `*Async` entry points work in file mode: `RunAsync` runs the body synchronously when `marten is null`. `CampaignGrain` and `MartenCampaignRoundDeadlineSink` are backend-neutral. | VERIFIED |

The grain path already runs against the file store in tests:
`CampaignDurableAuthorityJournalTests.GrainEquivalence.cs` builds a silo over
`ForSingleCampaign(fileJournal)` and drives `CampaignGrain.TickRoundAsync` for all seven round kinds.
(VERIFIED.)

## 3. Orleans needs

- **Clustering.** `UseLocalhostClustering(gatewayPort: 0)` is already what Development uses
  (`TemporaryCampaignHostConfiguration.cs`). It needs no store and rejects remote clients, which is
  right for a single-process local host. (VERIFIED.)
- **Reminders.** `UseInMemoryReminderService()` is already registered in the same method. In-memory
  reminders work for a single silo but lose every registration when the process exits.
  (VERIFIED.)
- **Consequence (the real gap).** In Postgres mode a restart re-activates each
  `CampaignRoundTimeoutGrain` through its durable reminder, and activation re-derives the timer from the
  store. In embedded mode nothing activates those grains after a restart, so a round deadline that
  was open at shutdown never fires until some request touches that campaign. The negative-control
  test below demonstrates this. (VERIFIED by test.)
- **Today's Development host has no journal at all.** `Program.cs` registers the store and sink only
  inside `if (!string.IsNullOrWhiteSpace(hotConnectionString))`, and throws on `CampaignJournal:Path`.
  Local play without Postgres therefore has no durable path through the real host yet. (VERIFIED,
  `Program.cs:27-52`.) `Cna.LocalCampaignSimulator` is the only non-test caller of the file store.

## 4. Options compared

| Option | What it is | Verdict |
| --- | --- | --- |
| **A. SQLite file per campaign (legacy class)** | The existing store, PR #427 topology (`<campaignId>.sqlite` plus `_host-identity.db`). | **Recommended.** Zero new journal logic; same SQL, same hashes, already exercised by the whole default test suite. |
| B. Marten on SQLite | Reuse the Marten backend with an embedded database. | Rejected. Marten is PostgreSQL-only (ASSUMED from Marten's documentation; nothing in `Directory.Packages.props` offers a SQLite provider). It would also add a second relational layer under the in-memory SQLite working set. |
| C. Flat file per campaign (JSONL or custom log) | A new append-only file codec. | Rejected. It would require reimplementing the relational journal logic (round tables, seats, openings) or a second working-set loader, and a second equivalence proof. Cost is large and benefit is nil for local play. Note that "file-per-campaign (PR #427 topology)" in the task wording is option A, not a new format. |
| D. Orleans localhost clustering plus in-memory reminders | The Orleans half only, not a journal store. | **Adopted alongside A.** It is the Orleans configuration already used in Development; it needs the boot re-arm to match Postgres reminder semantics. |
| E. One shared SQLite file for all campaigns | The pre-#427 topology. | Rejected. One global write lock across campaigns (data-layer doc section 1.3). Irrelevant at local-play scale but it is the worse of the two SQLite shapes with no upside. |

Two-store consequence: embedded mode makes the repo own two backends, A and Marten, behind one
journal. The shared journal logic keeps drift small, but each future schema change (for example
schema 15) must be made in both the SQL schema/migration path and the Marten mapping.

## 5. What must stay byte-identical (EVT-001, DET-001/002)

EVT-001 ("authoritative changes occur only through versioned commands and emitted events") and the
determinism requirements are properties of the event stream and hash chain, not of the engine that
stores it. The embedded backend must not alter any of the following, and cannot, because it runs the
same code:

1. `InitialHash` and `StepHash` input layout and domain tags (`campaign-durable-authority-v3:*`).
2. The stored bytes of `creation`, `canonical_event`, `receipt`, `checkpoint`, `contacts`, and
   `timing_configuration` per step.
3. Per-step `sequence`, `prior_hash`, `record_hash`, and `campaigns.head_sequence`/`head_hash`.
4. Round-row write-once timing columns (`deadline_ms` and siblings) and statuses.
5. Journal schema version 14 semantics and the `application_id`/`user_version` markers (these are
   SQLite-file concerns and do not exist on the Marten side).
6. Time and randomness inputs (`TimeProvider`, `randomBytes`), because replay determinism depends on
   them being injected, which both stores already do.

What may differ by design: file layout, pragma settings, connection handling, retry behaviour (the
file store serializes with `BEGIN IMMEDIATE`; Marten uses optimistic concurrency with retries), and
the operator-capability storage location.

## 6. Equivalence test strategy

"Same events produce identical replay" is already a gate, and it already covers embedded mode
because embedded mode is the baseline side:

1. **Existing, VERIFIED:** `MartenEquivalence.cs` scripts each scenario once, runs it against the
   SQLite file journal and the Marten backend with the same `ManualClock` and `DeterministicRandom`,
   then diffs every persisted table (`campaigns`, `accepted_steps` bytes and hashes, seats, openings,
   reference key, all seven round tables), the round deadlines, and the projection read side.
2. **Existing, VERIFIED:** `SANDTABLE_JOURNAL_TEST_BACKEND=marten` re-runs the whole
   `CampaignDurableAuthorityJournalTests` suite against Postgres. The default is the SQLite file.
   Nothing under `.github/` sets the variable (VERIFIED by grep), so the all-suite Marten run is a
   manual or local gate, and CI exercises the embedded-equivalent path on every PR by default.
3. **New in the spike:** the embedded-specific behaviour (boot re-arm) has three tests (section 8).
4. **Proposed (task E3):** pin literal golden values (head hash and per-step `record_hash` list) for
   one fixed scenario and assert the same literals in both the embedded and the Marten equivalence
   runs. This turns "equal to each other" into "equal to a recorded EVT-001 value", so a change that
   moves both backends together is also caught.
5. **Proposed (task E10):** a nightly job that runs the full journal suite with the Marten switch, so
   the second backend cannot silently rot while embedded is the default for tests.

## 7. Gaps and risks

| # | Gap | Evidence | Proposed handling |
| --- | --- | --- | --- |
| G1 | `Program.cs` has no embedded path and rejects `CampaignJournal:Path`. | VERIFIED | New explicit mode setting; keep Postgres the default; task E2. |
| G2 | In-memory reminders lose registrations on restart, so open deadlines stall. | VERIFIED by negative-control test | `EmbeddedDeadlineRearm`, implemented in this spike. |
| G3 | Embedded mode is not a Development-only concept, but `/api/v2` is Development plus loopback gated. | VERIFIED (gate exists); relaxation is out of scope per owner decision | **Open question for the owner, not decided here:** a packaged local runtime either runs with the Development environment name or needs a separate, explicit, loopback-only local-play mode. Do not relax the gate. |
| G4 | `RunAsync` file branch runs the journal body synchronously on the grain's scheduler thread. | VERIFIED | Acceptable for one local player; document it. Server mode is unaffected. |
| G5 | Replay is O(history) on every call in both backends. | DOC (`concurrent-simulation-plan.md`) | L10b (grain-resident verified working set) should sit above the store so embedded gets it for free. ASSUMED that L10b is designed backend-neutral; confirm in that chain. |
| G6 | Dual-backend maintenance tax on every schema change. | VERIFIED structurally | Nightly Marten suite (E10), golden values (E3). |
| G7 | First-run operator secret and data directory (platform user-data path) are undefined. | VERIFIED (`OperatorSecret` is required config) | Generate and persist a random secret on first run in a private directory; task E5. |
| G8 | Directory permissions and file locking on Windows are unproven. `RequirePrivateDirectory` is Unix-mode oriented (`File.SetUnixFileMode` guarded by `!IsWindows`). | VERIFIED (Unix guard); Windows behaviour ASSUMED | Supported-platform matrix (R2) decides scope; task E5 adds a Windows check. |
| G9 | Two host processes on one data directory would each run timers and could double-tick a round. | ASSUMED (not exercised) | Process-level directory lock; task E5. |
| G10 | No path from embedded campaigns to a Postgres server. | VERIFIED (none exists) | Optional export tool: load a file into a working set and flush it (the mapping is 1:1); task E9, deferred. |
| G11 | `LocalSessionStore` is in-memory in both modes. | VERIFIED | Not backend-specific. Seat credentials are durable (`seats` table); only browser sessions reset on restart. |
| G12 | Runbook supersede banner says SQLite procedures apply only to the test baseline. | VERIFIED | Update when embedded ships; task E5. |
| G13 | `CampaignCombatTimeoutService` (cross-file sweep) is still in the tree but not registered in `Program.cs`. | VERIFIED | Leave. The boot re-arm reuses its seven `CollectingXCampaigns` enumeration lanes once at startup instead of polling. |

## 8. Spike contents (this PR)

All new files; no existing file changed.

- `src/Cna.OrleansHost/Embedded/EmbeddedDeadlineRearm.cs`: `EmbeddedDeadlineRearm` enumerates every
  collecting round in all seven lanes (paged, cursor-based, per-campaign failures logged and
  skipped) and arms the matching `CampaignRoundTimeoutGrain` from the durable deadline.
  `EmbeddedDeadlineRearmParticipant` runs it once at silo `ServiceLifecycleStage.Active`. A deadline
  that has already passed fires immediately, so a campaign that timed out while the process was down
  resolves at boot, identically to the retired sweep.
- `src/Cna.OrleansHost/Embedded/EmbeddedCampaignJournalExtensions.cs`:
  `AddEmbeddedCampaignJournal(directory, operatorSecret, ...)` registers the file store, the
  deadline sink, and the re-arm. `AddEmbeddedDeadlineRearm()` registers the sink and re-arm over an
  existing store so tests can supply one. Not yet called from `Program.cs`.
- `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.EmbeddedBackend.cs` (no Docker
  needed), real in-process silo, `ManualClock`:
  1. `EmbeddedRestartWithoutRearmLeavesAPassedDeadlineUnfired` is the negative control: with only the
     sink registered, a Selection deadline that passed during "downtime" does not fire.
  2. `EmbeddedBootRearmFiresADeadlinePassedWhileTheProcessWasDown`: with the re-arm, the same
     scenario produces the timeout step (contract version 27) with no owner query, and the
     journal still reconstructs as verified.
  3. `EmbeddedBootRearmArmsOnlyCollectingRoundsWithTheirDurableDeadline`: the timeout grain's armed
     deadline equals the durable deadline, and an explicit second pass arms exactly one grain.

Verified in this session: the three spike tests pass, the existing `CampaignDurableAuthorityJournalStoreTests`
(10 tests) still pass, and the Orleans host test project builds with no warnings. Not run: the
full journal suite (hour-scale, deliberately avoided), the Postgres-backed equivalence tests (need
Docker), and a Windows run.

## 9. Phased task list

Sizes: XS under 2h, S half a day, M 1-2 days, L more.

| ID | Task | Size | Depends on |
| --- | --- | --- | --- |
| E1 | This PR: design plus boot re-arm spike | done | none |
| E2 | `Program.cs`: explicit `CampaignJournal:Mode` (`Postgres` default, `Embedded`), validation, embedded Orleans config (localhost clustering, in-memory reminders) in any environment name the owner approves (G3), keep the retired-path guard message accurate | S | owner answer on G3; rebase after #615 (ASSUMED no overlap: #615 is described as touching the adapter, journal and API endpoints, not `Program.cs`) |
| E3 | Golden EVT-001 values pinned and asserted in both the embedded and Marten equivalence runs | S | none |
| E4 | `LocalCampaignSimulator` switches to `AddEmbeddedCampaignJournal` so it can host more than one campaign (also unblocks an in-process load-test drive without Docker) | S | E2 |
| E5 | Operations: first-run data directory and operator secret, process-level directory lock, Windows permission check, runbook rewrite for the supported embedded mode | M | E2 |
| E6 | Process-kill recovery proof for embedded mode with boot re-arm (extend the `FoundationChild` pattern) | M | E2 |
| E7 | README and local-testing docs: running without Docker; AGENTS.md note on which backend each test lane uses | XS | E2 |
| E8 | Confirm L10b (grain-resident verified working set) is placed above the store so it benefits both backends | S (review) | L10b chain |
| E9 | Optional: embedded-to-Postgres export (load file into a working set, flush) | M | defer until a user asks |
| E10 | Nightly lane running the full journal suite with `SANDTABLE_JOURNAL_TEST_BACKEND=marten` | S | Docker on the nightly runner |

Critical path to a usable embedded mode: E2, then E5 and E6. Estimated 3-4 working days for E2+E5+E6
after the G3 answer.

## 10. Decision needed from the owner

**G3:** under the D7 rule not to relax the `/api/v2` Development plus loopback gate, how does a
packaged local-play build run? Two clean answers: (a) the local runtime launches the host with the
Development environment name, loopback only; or (b) add a distinct, explicit loopback-only
"local play" mode that is not Development. Option (a) needs no gate change but ships a Development
label; option (b) needs a reviewed gate change. This design assumes neither and blocks only E2.
