# Phase C2 (#439) fork: the seven round tables are not pure event folds

**Status:** stop-and-report, per this project's standing discipline (issue #439's "Phase C
breakdown" comment: "stop-and-report on genuine architectural forks rather than guessing").
Written during Phase C2 implementation (campaign-state projection landed in the same change;
see `src/Cna.OrleansHost/Persistence/CampaignStateProjection.cs`). No round-table projection code
accompanies this document -- that is exactly the piece being held pending a decision.

## 1. What Phase C2 asked for

Issue #439's Phase C breakdown: "C2 — Projections. `SingleStreamProjection<T>` (Inline lifecycle)
for campaign state... projections for the seven round tables, replacing the `VerifyXRounds`
full-table-scan functions." The design doc (`2026-11-18-postgres-migration-design.md` §3.2)
frames this as mechanical: "The seven round tables... are, conceptually, derived/materialized
views over the event stream — exactly what projections are for."

## 2. What the actual code does, re-verified directly (not taken on faith from the design doc)

Traced one round table in full (`selection_rounds`,
`src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.SelectionRounds.cs`) and confirmed the
same shape in the other six (`ForceRounds.cs`, `RbaRounds.cs`, `CycleRounds.cs`,
`ReserveRounds.cs`, `CustodyRounds.cs`, `RetreatRounds.cs` — all declare the same
`OpenedMs`/`DeadlineMs`/`HighWaterMs` triple and the same `RequireXRoundBinding` bounds checks,
confirmed by grep across all seven files).

**`opened_ms`/`deadline_ms`/`high_water_ms` are wall-clock reads taken between accepted-step
events, not data carried by the accepted event stream for the common (non-timeout) completion
path:**

- `InitializeSelectionRound` (`SelectionRounds.cs:16-39`) opens a round by calling
  `TryTrustedNow(out var opened)` (`:20`) -- a live wall-clock read -- and computes
  `deadline = opened + timing.Budget("selection")` (`:28`). Neither value is written into any
  event; the round row is inserted directly (`:30-37`).
- `GateSelectionRound` (`:142-154`) advances `HighWaterMs` on each poll/tick
  (`TryTrustedNow(out var now)`, `:145`), again a live wall-clock read with no corresponding event
  append when the gate doesn't cancel (`UpdateSelectionRound` at `:151` writes straight to the
  SQLite row).
- **The common "completes normally" path never emits timing data as an event at all.**
  `CampaignDurableAuthorityJournal.Submissions.cs:764`'s
  `UpdateSelectionRound(connection, transaction, selectionRound with { Status = "completed" })`
  (and the five sibling calls at `:769,:775,:777,:801,:808,:935` for the other round tables) writes
  directly to the round table; no event in `accepted_steps`/`CampaignAcceptedStepEvent` carries
  `opened_ms`/`deadline_ms`/`high_water_ms` for this case.
- The *only* place these three fields appear inside an event payload is on a **timeout/cancellation**
  event -- confirmed by grep: `openedAtUnixMilliseconds` appears in event bodies only in
  `CampaignNativeCombatSelectionTimeoutV38.cs`, `CampaignNativeCombatForceCancellationV19.cs`,
  `CampaignNativeCombatRbaCancellationV33.cs`, `CampaignNativeReserveReleaseTimeoutV35.cs`,
  `CampaignNativeCombatRetreatTimeoutV37.cs`, `CampaignNativeCombatCustodyTimeoutV36.cs`,
  `CampaignNativeCycleControlTimeoutV34.cs` -- i.e. exactly the seven tables' timeout twins, and
  only on that path. Even there, `VerifySelectionRounds` (`SelectionRounds.cs:226-329`) doesn't
  independently re-derive these values from the stream; it reads the round table's *own already-
  stored* values and checks the timeout event's embedded evidence against them
  (`:322-324`) -- a self-referential consistency check, not a from-events derivation.

## 3. Why this is a fork, not a shape-variation to resolve case by case

A Marten `SingleStreamProjection<TDoc,TId>`'s `Apply`/`Create` methods must be **pure functions of
the event (plus prior projection state)** -- that is what makes Marten's projection rebuild
(`martendb.io/events/optimizing`, cited in the design doc's own sources) a safe, repeatable
operation, and it is also what `DET-001`/`DET-002` require of anything claiming to be a
deterministic fold over the campaign's command stream. `opened_ms`/`deadline_ms`/`high_water_ms`
for the common completion path exist **only** as side-channel wall-clock reads interleaved between
events today -- they are not reconstructable from `CampaignAcceptedStepEvent` data at all for that
path, by construction, not by omission.

Building the seven round-table projections as asked would force one of three choices, each a real
design decision beyond "implement the mechanical translation," none safe to make unilaterally in
this phase:

1. **Have the projection's `Apply` method read the wall clock itself.** Breaks determinism/replay
   safety outright (a projection rebuild days later would compute a different `opened_ms` than the
   original live run) -- a direct `DET-001`/`DET-002` violation, not a style choice.
2. **Drop the timing columns from the projection**, keeping only what *is* event-derivable
   (`status`, entry/barrage/declination hash bindings, `configuration_hash`). This is a materially
   different read model than "replaces `VerifyXRounds`, same semantics" -- it cannot answer "is
   this round's deadline approaching," which is the entire reason the hand-rolled tables carry
   those columns, and item 4's acceptance test ("projection output matches what the old
   `VerifyXRounds` scan would produce") would need to be weakened to match, not satisfied as
   stated.
3. **Extend the event schema to carry round-open/tick timestamps as first-class event data**,
   so a projection has something to fold. This is a real schema change rippling back into Phase
   C1's envelope (`CampaignAcceptedStepEvent`, PR #442) and/or `Cna.Core`'s event shapes, and is
   exactly the kind of cross-phase scope expansion the issue's phased-PR discipline asks to avoid
   deciding unilaterally mid-phase.

None of these is a "minor implementation judgment call." Each is a real tradeoff among
correctness, scope, and what "replacing `VerifyXRounds`" is even allowed to mean, and the design
doc's own §3.2 did not anticipate it -- it assumed the round tables were already pure derived
views, which §2 above shows is not the case for the live (non-timeout) completion path.

## 4. One more data point worth naming

Design doc §4.2 ("Consequence for the round tables: with the sweep gone, they only ever need
per-`campaign_id` lookups") suggests the *long-run* intent is for Phase C4's Orleans grain-local
timers to take over the wall-clock bookkeeping entirely, at which point the round tables'
remaining job really would shrink to the event-derivable parts (status/bindings) this document
flags as the only safe scope for a pure projection today. If that is the intended resolution, it
is still a decision -- "the seven round-table projections intentionally drop timing columns
because C4 obsoletes them" is materially different from "same semantics as `VerifyXRounds`," and
should be stated and accepted explicitly by whoever owns #439's sequencing, not assumed by this
phase's implementer.

## 5. What this phase delivered instead

`CampaignStateProjection`/`CampaignStateDocument`
(`src/Cna.OrleansHost/Persistence/CampaignStateProjection.cs`), registered inline via
`CampaignEventStreamSchema.Configure` — the part of Phase C2 that *is* a pure fold of
`CampaignAcceptedStepEvent` (sequence, hash-chain position, latest `SnapshotCheckpoint`/
`CanonicalContacts`), with no wall-clock dependency, proven against real Postgres
(`tests/Cna.OrleansHost.Tests/Persistence/CampaignStateProjectionTests.cs`) including the
genesis-vs-resumed-batches equivalence item 3 of this phase asked for. The seven round-table
projections are held pending a decision among §3's three options (or an explicit acceptance of
§4's "C4 will obsolete the timing columns anyway").
