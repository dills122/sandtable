# Phase C3b (#439) fork: the journal holds durable state the Marten schema has no home for

> **Superseded by the design spike**
> [`2026-11-19-phase-c3b-durable-state-design-spike.md`](2026-11-19-phase-c3b-durable-state-design-spike.md).
> The project owner asked for a spike to find the right storage shape before committing to this
> doc's A/B/C. The fork below is left unedited, per this project's convention of keeping forks
> visible.

**Status: stop-and-report**, per issue #439's standing discipline ("stop-and-report on genuine
architectural forks rather than guessing") and this phase's own brief (item 6). Written at the
start of Phase C3b, after reading the design doc, C1/C2/C4/C3a code, and the journal in full. **No
storage code accompanies this document.** The SQLite path is untouched;
`SqliteCampaignRoundDeadlineSink` is deliberately *not* renamed, because its backing store is still
SQLite and a `Marten...` name would be false.

## 1. What C3b asked for

Swap `CampaignDurableAuthorityJournal` (+ its partial-class siblings + `CampaignDurableAuthorityJournalStore`)
from raw SQLite onto "the Marten event store / projections already built in C1/C2", keeping the
journal's contract unchanged, one Marten stream per campaign (C1's convention: the campaign id
itself as a string stream key -- `CampaignEventStreamSchema`'s remarks), then rename the deadline
sink.

## 2. What C1/C2 actually cover versus what the journal actually persists

C1/C2 model exactly one SQLite table: `accepted_steps` → `CampaignAcceptedStepEvent` (+ hash-chain
headers), with `CampaignStateDocument` and seven `XRoundsDocument` projections folded from it. The
journal's schema (`CampaignDurableAuthorityJournal.cs:191-338`) has 13 tables. Re-verified per table:

| SQLite state | Written by | Event-derivable? | Home in C1/C2 schema |
| --- | --- | --- | --- |
| `accepted_steps` | `PersistAccepted` (`.cs:1951`) | yes (it *is* the stream) | `CampaignAcceptedStepEvent` |
| `campaigns` genesis: request fields, `creation_event`, `initial_snapshot`, `initial_contacts`, `timing_configuration` | `CreateCampaign*` | **no** -- it is the stream's *seed*, hashed into `InitialHash` (`Load`, `.cs:2073`) before step 1 exists | **none** (C1 has no genesis event; the stream's first event is accepted step 1) |
| `campaigns.head_sequence/head_hash` | `PersistAccepted` CAS (`.cs:1998-2011`) | yes | Marten stream version + headers (fine) |
| `seats` (epoch, grant/credential digests + expiries) | `Seats.cs:86,155` (`ON CONFLICT ... DO UPDATE`) | **no** -- auth state, mutated without any accepted step | **none** |
| `owner_openings` (revision, visible bytes, authority version) / `owner_actions` | `Openings.cs:587,618` (upsert) | **no** | **none** |
| `reference_key` (per-campaign 32-byte secret) | `CreateNew` (`.cs:350`) | **no** (random) | **none** |
| 7 round tables: status + hash bindings | `InitializeXRound`, `Submissions.cs` | yes | C2 `XRoundsProjection` |
| 7 round tables: `opened_ms`/`deadline_ms`/`high_water_ms`, `configuration_hash` on normal path | `InitializeXRound`, `GateXRound` | **no** (wall-clock reads between events -- C2 fork doc §2) | **none, by C2's accepted decision** |
| `_host-identity.db` `operator_capability` | `EnsureHostIdentity` | n/a (host-wide) | **none** |

Most of that is a mechanical "store it as a plain Marten document" judgment call (§4). **One row is
not**: the round-timing columns.

## 3. The actual fork: C2's accepted resolution rests on a premise C4/C3a did not build

C2's fork resolution (`2026-11-19-phase-c2-round-table-projection-fork.md`, "Status: resolved")
dropped `opened_ms`/`deadline_ms`/`high_water_ms` from Marten with this stated, user-confirmed
rationale: *"once C4 lands, live deadline state lives in the owning grain's own memory, not in a
round-table read model."* Checking that premise against what C4 and C3a actually built:

1. **The timeout grain explicitly keeps no durable state and re-derives the deadline from the
   store.** `CampaignRoundTimeoutGrain.OnActivateAsync` and `ReceiveReminder` both call
   `sink.GetDeadlineAsync(...)` -- "the grain keeps no durable state of its own, so re-derive
   whatever is current from the sink". `ICampaignRoundDeadlineSink.GetDeadlineAsync`'s contract:
   "The current *durable* deadline". C3a's implementation reads it from `deadline_ms`
   (`SelectionRounds.cs:46-54` and six siblings). The reminder backstop -- the whole point of the
   design doc §4.2 hybrid -- only works if a durable deadline survives a silo crash. If the
   deadline lives only in grain memory, the backstop has nothing to re-arm from after the exact
   failure it exists for.
2. **The timing values are adjudication inputs, not just bookkeeping.** Every timeout path passes
   `round.OpenedMs, round.DeadlineMs, round.HighWaterMs` into Core
   (`CampaignHostAdapter.TimeoutNativeCombatSelection(...)`, `SelectionRounds.cs:84-97`; the six
   siblings at `CustodyRounds.cs:83`, `CycleRounds.cs:83`, `ForceRounds.cs:69`, `RbaRounds.cs:84`,
   `ReserveRounds.cs:103`, `RetreatRounds.cs:83`). Core embeds them in the hash-chained timeout
   event (`openedAtUnixMilliseconds`, etc.). Losing them on restart means a post-crash timeout
   event carries *fabricated* evidence -- a `DET-001`/`EVT-001` concern, not a style choice.
3. **`high_water_ms` is a durable anti-clock-rollback gate.** `GateXRound` (`SelectionRounds.cs:158-169`
   and siblings) rejects `now < HighWaterMs` as `"clock-regression"` and persists each advance.
   Held only in memory, a restart resets the gate and the regression check silently weakens.
4. **`VerifyXRounds` on every `Load` cross-checks the timeout event's embedded evidence against the
   retained round row** (`SelectionRounds.cs:337-339`). Without durable timing that check has no
   counterpart.

So "timing lives in grain memory" is not true of the code that shipped, and cannot be made true
without either weakening crash recovery (the §4.2 backstop) or the timeout evidence chain. Picking
a durable home for the timing columns is a reversal or re-interpretation of an explicitly accepted
decision -- not mine to make unilaterally.

## 4. Options

**A. Plain Marten documents for all non-event state, including round timing (recommended).**
One `IDocumentSession` per journal operation replaces one `BEGIN IMMEDIATE` SQLite transaction:
the event append, the C1/C2 inline projections, and upserts of `CampaignGenesisDocument`,
`CampaignSeatDocument`, `OwnerOpeningDocument`/`OwnerActionDocument`, a per-campaign reference-key
document, and a `RoundTimingDocument` per (campaign, round kind, round hash) carrying
`opened_ms`/`deadline_ms`/`high_water_ms`/`configuration_hash`/`record_hash` all commit atomically
in one Postgres transaction. Faithful 1:1 to today's semantics; `GetDeadlineAsync`/`GateXRound`/
`VerifyXRounds` keep working unchanged in shape; C2 projections keep owning status/bindings.
*Cost:* it reintroduces mutable round-timing state outside the projections, i.e. re-reads C2's
resolution as "projections don't own timing" rather than "nothing durable owns timing". That
reading needs explicit sign-off. Genesis as a document (rather than a new event type) keeps C1's
one-event-type envelope and the `InitialHash` derivation unchanged.

**B. Make timing event-derivable: new stream events for genesis, round-open, and high-water
advances.** Projections could then own timing purely. *Cost:* those events are not Core-accepted
steps -- they would sit in the campaign stream without Core adjudication (`EVT-001` / "Core
remains sole adjudication authority" tension), must be either inside the hash chain (changing
`StepHash`/`sequence` semantics and C1's sequence==version invariant) or outside it (a stream that
is no longer uniformly hash-chained). High-water advances on every poll would also bloat streams.
Real C1 schema change; not recommended.

**C. Keep C2's premise literally: timing in grain memory only.** Breaks the C4 backstop after a
crash (§3.1), fabricates timeout evidence after restart (§3.2), weakens the clock gate (§3.3).
Not viable without relaxing crash-safety requirements that design doc §1/§6 explicitly keep.

## 5. Secondary findings -- judgment calls I would make under option A, listed for visibility

None of these need escalation on their own, but they change the size and shape of C3b:

- **Sync → async contract change.** Marten 9.62.0 has no synchronous `SaveChanges` (only
  `SaveChangesAsync`; confirmed by inspecting `Marten.dll` metadata), and `LoadAsync`/
  `FetchStreamAsync` are async-only outside batch queries. Every journal/store entry point
  (~150 forwarders in `CampaignDurableAuthorityJournalStore.cs:272-723`) is synchronous today and
  runs Load → Core adjudication → persist inside one transaction. Keeping that contract literally
  means sync-over-async on Orleans scheduler threads; the honest swap makes the store async and
  propagates through `CampaignGrain`, `CampaignDurableOutwardApi`, and the deadline sink. The
  brief says "public API/behavior unchanged" -- behavior can be, signatures cannot.
- **Legacy read-only schema versions 5-13** (`Load`'s `LegacySchemaVersion` branches, 53
  `schemaVersion` gates across the partials) have no Marten equivalent. Design doc §7 ("no real
  production data exists, no migration path needed") supports dropping them rather than porting.
- **File-level crash-safety/migration machinery** (`CreateNew` temp-file+rename, `RunMigrations`
  `VACUUM INTO`, WAL checkpointing, `CreateNewCrashHook`/`MigrationCrashHook`) is SQLite-file
  specific and is superseded by Postgres transactions + Marten schema tooling (design doc §3.5).
  The corresponding subprocess crash tests (`*Crash.cs`, `JournalCreateCrash.cs`,
  `JournalMigration.cs`, `DirectoryBootstrapRace.cs`, `FreshProcess*.cs` -- 26 test files reference
  `FoundationChild`/`SweepOnce`) would need rewriting as Postgres equivalents, not porting.
- **Host identity** (`_host-identity.db`, operator capability digest) becomes one host-level Marten
  document (or configuration); `CollectingXCampaigns` cross-file enumeration is only used by the
  retired sweep and its tests and could be served by a Marten query or retired with it.
- **Equivalence gate.** Under option A the old/new comparison is direct: drive the same realistic
  scenarios (the `GrainEquivalence.cs`/`RoundTimerArming.cs` style) against both backends and
  compare canonical receipts, the full accepted-step byte sequence + hash chain, seat/opening
  outcomes, and every round's timing tuple -- all of which exist on both sides only under option A.

## 6. What is needed to proceed

A decision on §4 (A, B, or something else) for the round-timing columns, and acceptance (or
rejection) of the sync→async signature change in §5. With option A + async accepted, C3b is
implementable as a direct storage swap under the existing data model, gated by an
old-vs-new equivalence suite before the SQLite path is removed.
