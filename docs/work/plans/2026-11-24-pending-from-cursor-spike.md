# Spike S1: can `ExerciseSession.Pending` be derived from a sequence cursor? (2026-11-24)

> **2026-11-26 status note:** This is a dated design spike. The synthetic HTTP checkpoint through open Turn 2 Reserve is implemented (#652), but the executor migration proposed here is not complete; Turn 2 acceptance and further progression remain open.

**Status:** spike report. New files only: this doc and
`tests/Cna.Core.Tests/Exercises/PendingFromCursorSpikeTests.cs`. No production source
changed, including the three hot files. Executes §3.6 of the synthesis doc (proposal 3's
de-risking spike).

## 0. Verdict

**GO, conditionally.** 32 of the 35 `PendingDecision` subtypes (91.4%) are derived by a
pure function of the catalog cursor plus a two-value last-event flag. The other 3 are the
`*NoAttack` subtypes, which are shadowed arms: they can be deleted instead of derived.
Of the 32, 29 were matched against real driven sessions with zero mismatches; the 3
`*Move` subtypes are derivable but were not driven (see §3).

*Rebase note:* the spike was first written against 29 subtypes (26 derivable, 89.7%).
PR #570 (Stage 3 preamble bridge) then merged, adding 6 `ThirdStage*` subtypes, which
the oracle flagged immediately (its subtype-count pin failed). I added their rows to the
preamble table and drove the Stage 3 preamble in the real session; all 6 matched. The
verdict is unchanged and the margin is now on the right side of 90%.

The 90% line is cleared on the raw count (91.4%) and on the 32 live arms (100%). The exceptions are explained by data (§2), not by "the field is still
non-null". Read this as "feasible", with the caveats in §3 attached.

## 1. What the spike answered

1. **Does `ExerciseCheckpoint` expose a position id?** Yes. `ExerciseCheckpoint.PositionId`
   is a public property (`ExerciseCheckpoint.cs:758`), filled by every constructor. It is
   the **next** position to act at, not the position of the event just completed
   (e.g. after Initiative Declaration it is `...weather-determination`).
2. **Fixtures enumerated** (all on `origin/main`):
   - A real, driven session through Operation Stage 1 (both sides) and Stage 2 (preamble
     and both sides), mirroring `CampaignStageThreeEntryVerificationTests`. 71 samples
     (`Pending` plus `QueryCheckpoint().PositionId` after every accepted step), 29 distinct
     subtypes observed.
   - `ExerciseSessionPendingTests` bare reflection sessions: not usable for the position
     side. `QueryCheckpoint` needs real native records, and a bare session has none, so
     there is no position to compare. They remain the only place the staleness shapes are
     built synthetically.
   - Orleans fixtures: not separately enumerated. They drive the same `ExerciseSession`
     through `CampaignHostAdapter`, so their states are a subset of what the Core driver
     reaches; sampling them needs a hook in a hot file, which the spike may not touch.
3. **The function** `PendingFromCursor.Derive(turn, checkpointPositionId, lastEvent)` is in
   the test file. Mechanics:
   - Find the catalog position the checkpoint points at, then take the **previous** catalog
     position: the one the last event completed.
   - Subtype = occurrence prefix (a 4-row table keyed by `(operationStage, actorRole)`:
     legacy, `Second`, `StageEntry`, `StageEntrySecond`) + mechanic (a lookup keyed by the
     completed position's phase/segment/step) + `Pending`.
   - Stage-2 preamble names come from a 6-row table keyed by phase/segment. The names are
     irregular (`StageEntryWeather` next to `NoObligation*`), so they are data.
   - The "side role" input is already carried by the catalog position (`LandActorRole`),
     so it needs no separate argument.
   - No per-subtype branch. The only exception list is the 3 `NoAttack` names.

## 2. Results

| Bucket | Count | Detail |
| --- | --- | --- |
| Mechanical, verified against real sessions | 29 | Every one of the 71 samples matched (Stage 1 second side, Stage 2 preamble and both sides, Stage 3 preamble, legacy tail arms); includes states with 36 non-null `Current*` fields. |
| Mechanical, derived but not driven | 3 | `SecondMove`, `StageEntryMove`, `StageEntrySecondMove`. Need `LastEvent.MoveWithinPosition` (see below). |
| Explicit exception | 3 | `SecondNoAttack`, `StageEntryNoAttack`, `StageEntrySecondNoAttack`. |
| Total | 35 | |

**Why the last-event type is needed, and only there.** A Move does not advance the
position, so Reserve-completion and Move both leave the checkpoint at the Movement
position. Exactly three catalog positions are ambiguous this way
(`operation-1.second`, `operation-2.first`, `operation-2.second` Movement); a test pins that
list. Every other state is a pure function of the cursor. `CurrentHistory` already ends in
the last event, so the flag needs no new field.

**Why the NoAttack arms are exceptions.** `WithSecondNoAttack` (`ExerciseSession.cs:940`)
and its two siblings install the Assault runtime in the same transition, with non-empty
`Events`. `ComputePending` checks the Assault arm first, so in every driven session the
state after the no-attack completion already reports `*AssaultPending`; the NoAttack arm
is never selected. They are reachable only by hand-setting fields, as the bare fixtures do.
I read this as dead arms, not as state a cursor cannot represent. Deleting them rather
than deriving them is the clean outcome, but that is a decision for the migration PR.

**The staleness guards cost nothing under a cursor.** `ComputePending` had 13 guarded arms at spike time (#570 has since added Stage 3 arms; recount before S3)
(7 gated on `CurrentInitiativeDeclaration is null`, 6 on `CurrentStageEntryReserve is
null`), not the eight the synthesis doc expected. A cursor is one value, so a stale field
cannot shadow a later one. The real-session samples show this directly: late samples carry
up to 36 non-null `Current*` fields and the derivation still matches. These guards are not
exceptions; they disappear.

**Other findings.**
- `TruckConvoyPassPending` and `PostCombatPassPending` are only reachable in the legacy
  Operation Stage 1 first side. Everywhere else the Assault arm shadows them (the passes
  accrete inside the Assault runtime). The occurrence table captures this as one
  `TailOwner` flag, not as 6 extra arms.
- The pre-#394 legacy chain (Stage 1 first side, Reserve through Combat) has no
  `PendingDecision` at all. The table encodes it as a `null` prefix.
- Operation Stage 3's preamble now has real subtypes (#570) and is covered by 6 preamble
  table rows, verified against the real session. Its side occurrences (Reserve onward) have
  no occurrence row yet; `Derive` returns `null` there. Adding them becomes table rows
  (prefix plus a tail-owner flag) instead of arms per method.

## 3. What this does not prove (read before relying on the GO)

- **Move was not driven.** The Core `Query` returns no action set in those states, and
  reaching a legal Move needs `QueryStageEntryMovementCut` plus a real movement choice.
  I stopped there to keep the spike in scope. The 3 Move subtypes are derivable on paper
  and covered only by the ambiguity test, not by a real session.
- **Only the no-attack path was driven.** No real attack (Assault with `Events` from real
  combat), no repeat cycles, no Retreat Before Assault, no reserve-release choices. Repeat
  combat probably revisits combat positions, so the cursor may repeat inside the Assault
  family; the last-event flag would then need more than two values. Unverified.
- **`QueryCheckpoint`'s other fields** (`StateVersion`, contract version, ruleset hash)
  still come from the retained event, not from the position. Deriving the position id
  replaces the dispatch, not the checkpoint payload.
- **Not generated from the catalog.** `MechanicOf` and the two small tables are hand
  written (about 20 rows), keyed by catalog ids. They are data, but not emitted by the
  catalog itself.
- **Orleans and bare fixtures** were not sampled, per §1.2.
- **Prefix lookup uses the last completed position's role**, so Stage 3 needs its own rows
  before this covers it.

## 4. Sizing of the first migration PRs (if go)

The function is already about 120 lines, so the executor is cheaper than the synthesis
doc's worst case. I would stage it as follows (no hot-file work happens until the Stage 2
to 3 bridge chain has merged, per the synthesis ordering):

1. **PR A, shadow derivation + oracle (small, about a day).** Promote `PendingFromCursor`
   into `src/` as a new file; add a test that asserts derived equals `session.Pending`
   for every state of the driven session. Extend the driver to cover Move, an attack path,
   and a repeat cycle first; those are the open risks in §3. No hot-file edit except,
   if needed, exposing the last history event.
2. **PR B, switch `ComputePending` to the derived value (medium).** Replace the 35-arm
   cascade and delete the 13 staleness guards. Hot file; needs exclusive ownership.
   Requires the §3 gaps closed or the NoAttack arms removed first.
3. **PR C, `QueryCheckpoint` and `ReconstructOne` dispatch off the derived key (medium).**
   Both already switch on `Pending`; they keep constructing the checkpoint from the event
   but select by cursor.
4. **PR D, delete the field pairs and the 35 subtypes (large, mechanical).** Only after C.

So the first migration PR (A) is 1 PR and the `ComputePending` swap (B) is a second; the
deletion work (C, D) is separate. That matches the "1 to 2 PRs for the first migration"
branch of the synthesis doc, with C and D as follow-ups, not blockers.

## 5. Executor plan (owner decision: Turns 2-6 are in scope this quarter)

The owner has confirmed Turns 2-6 are in scope, so the executor is planned work. §4's
sketch is superseded by this section. Everything below is conditional on the §3 gaps
closing in step 1; nothing here was built beyond the spike.

### 5.1 (a) Staged plan, PR-sized

| Step | PR | Content | Size | Files |
| --- | --- | --- | --- | --- |
| 0 | S0 | Close the spike gaps: drive a real Move, a real attack (Assault with `Events` from combat), a repeat cycle and Retreat Before Assault in the test driver; record which last-event values appear. | 0.5-1 day | new test files only |
| 1 | S1 | Shadow derivation: promote `PendingFromCursor` into `src/Cna.Core/Exercises/` as a new internal type, with the occurrence/preamble/mechanic tables as separate data files. Nothing calls it in production. | 0.5 day | new `src` files |
| 2 | S2 | Equivalence oracle: the differential test of §5.3, run in the normal test project; fails on any divergence and on any `PendingDecision` subtype with no table row. | 1 day | new test files |
| 3 | S3 | Migrate `ComputePending`: body becomes `PendingFromCursor.Derive(...)` mapped to a subtype; delete the 13 staleness guards. Keeps the subtypes and field pairs so the diff is the cascade only. | 0.5-1 day | **hot:** `ExerciseSession.cs` |
| 4 | S4 | Migrate `QueryCheckpoint` and `ReconstructOne`: key their 35-case switches off the derived occurrence + mechanic instead of the subtype type; both must pick the same arm for the same history (DET-001/002). Checkpoint payload still comes from the retained event. | 1-1.5 days | **hot:** `CampaignExercises.cs` |
| 5 | S5 | Occurrence-keyed event store: replace the per-occurrence `Current*`/`*Predecessor` field pairs with one map keyed by (occurrence, mechanic). Includes the 4-way `CurrentRepeatAssault` coalesce and `WithRepeatAssaultEvent`. Largest step; split per occurrence family (preamble, Stage 1 second, Stage 2 first, Stage 2 second). | 2-3 days, 3-4 PRs | **hot:** `ExerciseSession.cs`, `CampaignExercises.cs` |
| 6 | S6 | Delete `PendingDecision.cs` subtypes and the per-occurrence `With*` methods now unreferenced. | 0.5 day | **hot:** `PendingDecision.cs` and others |
| 7 | S7 | Tier-2 and Host builders (§5.4). Independent of S5 order; best after S4. | 2-3 days | **hot** plus `CampaignHostAdapter.cs` |

Rough total 8-11 worker-days across 10-12 PRs; step 3 is the first PR that changes
production behavior of a hot method. Every PR keeps `SerializeCanonical()` output, the
rules-artifact hashes and event order unchanged (EVT-001); `PendingDecision` is in-memory
only, so none of it touches the wire. The executor adds no clock, `Guid`, RNG or unordered
iteration (DET-001/002): tables are ordered arrays or dictionaries read by key only.

### 5.2 (b) What can run in parallel with the Stage 3 chain, and what cannot

The Stage 3 chain keeps all edits to `ExerciseSession.cs`, `CampaignExercises.cs` and
`PendingDecision.cs` until S3-9 lands.

**Safe in parallel (new files only): S0, S1, S2.**
- S0 and S2 are new test files and need no production edit. S2 is deliberately robust to
  Stage 3: it enumerates `PendingDecision` subtypes by reflection and fails with a list of
  unmapped names, so each Stage 3 arm that lands without a table row turns the oracle red
  with an actionable message, instead of silently drifting. The Stage 3 worker only adds a
  table row per new arm (one line each); they do not need to know the executor.
- S1 is a new `src` file reading existing members. It needs `session.CurrentHistory` and
  `QueryCheckpoint` to be reachable from a new file in `Cna.Core`; both appear to be
  (spike tests reach them via `InternalsVisibleTo`, and `Cna.Core` internals are visible
  inside the assembly). Verify at build time; if a member is `private`, S1 waits.
- Table rows for Stage 3 can be written by whoever lands each Stage 3 slice, in the new
  data file, with no hot-file contention.

**Must wait for exclusive ownership of the hot files (after S3-9): S3, S4, S5, S6, S7.**
They rewrite methods the Stage 3 chain is editing at every slice (`ComputePending`,
`QueryCheckpoint`, `ReconstructOne`, the Tier-2 methods). Interleaving would force
repeated rebases across cascades that are hand-ordered, which is the bug class PR #552
was. One owner, serial PRs, no concurrent Stage 3 work on those files.

**Cost of waiting:** none of S0-S2 is wasted if the chain's final shape differs, and they
convert Stage 3's per-arm hand-ordering risk into a failing test in the meantime.

### 5.3 (c) Differential-test design

- **Old resolver:** the existing `ExerciseSession.Pending` (the cascade). **New resolver:**
  `PendingFromCursor.Derive` over `Cna1979LandSequenceV4.CreateTurn(gameTurn)`.
- **Fixture set (the same list the spike enumerated, plus what S0 adds):**
  1. the real driven Stage 1 + Stage 2 session, sampled after every accepted step;
  2. Stage 3 entry session from `CampaignStageThreeEntryVerificationTests` (after #570);
  3. S0's Move, attack, repeat-cycle and Retreat Before Assault sessions;
  4. Orleans fixtures, through an extension of the same recorder reached via a read-only
     hook once S4 gives a legitimate place to expose it (until then they are covered by
     proxy, since they drive the same session type);
  5. the `ExerciseSessionPendingTests` bare sessions as a staleness regression set; they
     have no position, so for these only the old resolver is asserted and the case is
     expressed as "cursor model has no stale-field input".
- **Per sample, compare:** (i) subtype name, (ii) `QueryCheckpoint().PositionId`,
  (iii) after S4, the arm `QueryCheckpoint` and `ReconstructOne` each select, which must be
  equal to each other and to the derived key (the DET-001 check), (iv) after S5,
  `SerializeCanonical()` bytes and the canonical event stream hash of the whole session, to
  prove the field-pair removal is byte-identical (EVT-001).
- **Completeness checks:** the reflection count of `PendingDecision` subtypes equals
  (derived + explicit exceptions); every subtype is observed in at least one driven sample
  or is on a named "not driven" list that shrinks to zero by S3; every catalog position is
  visited by some sample or listed as unvisited.
- **Failure output:** label, actual, derived, position id, and the non-null `Current*`
  field list, so a divergence points straight at a stale field or a missing row.
- **Run policy:** the real-session driver takes about 3 seconds, so the oracle runs in the
  normal Core test project filter, not a long suite. Targeted filter only.

### 5.4 (d) What the executor does not cover, and the cost to migrate it

Source: scoping doc §1.4 and the direction doc's explicit exclusions; the builder check
below was done by reading `CampaignHostAdapter.cs`.

**The 11 Tier-2 Combat methods** in `CampaignExercises.cs`: `QueryNativeReserveReleaseState`,
`SubmitNativeReserveReleaseChoice`, `CompleteNativeReserveRelease`,
`QueryPendingNativeCycleControl`, `TimeoutNativeReserveRelease`, `TimeoutNativeCycleControl`,
`QueryPostCombatPassSource`, `SubmitPostCombatPass`, `QueryTruckConvoyPassSource`,
`SubmitTruckConvoyPass`, `SubmitNativeCycleControl`.
- **Why they are outside:** they do not dispatch on `Pending`. They read the per-occurrence
  Assault runtime directly (`CurrentSecondAssault`, `CurrentStageEntryAssault`, ...) on
  purpose, because the generic `CurrentRepeatAssault` stops resolving once a side's Patrol
  finishes, and Reserve Release must still see that side's `.Release`. Each has a
  hand-written `isStageEntry`/`isSecondSide` branch set, checked newest-first.
- **What migration takes:** an occurrence resolver. The cursor already yields
  (operation stage, acting-side role); S5's occurrence-keyed store would expose
  `AssaultFor(occurrence)`, including the "finished" case, as a single accessor. Each
  Tier-2 method then replaces its boolean branches with one lookup. Per method this is a
  small mechanical edit, about 30-60 lines removed each, but the 11 must move together
  with S5 or they will still reference the old fields. Size: 1.5-2 days, 2 PRs (Reserve
  Release/Cycle Control group; Truck/Post-Combat group). Risk is medium: these are the
  methods #551/#552 broke.
- **Without migrating them:** each Stage 3 Combat slice (S3-4, S3-5, S3-9) still adds one
  branch to each of the 11. The executor does not remove that cost; it only stops it
  from compounding with the cascade.

**The Host's 20 `NativeXxxCommand` builders** (`CampaignHostAdapter.cs`).
- Read of `NativeCombatNoAttackCommand` and `SecondCombatNoAttackCommand`: the bodies are
  identical (call `QueryCheckpoint`, build a `CampaignActionSubmission` from the receipt);
  they differ only in the guard that names which `Current*` field must be non-null. About
  12 references to `CurrentStageEntry*`/`CurrentSecond*` in the file are those guards.
  I only read two of the 20; the others (timeout builders, successor/settlement builders)
  may carry real per-occurrence logic. Treat "20 identical bodies" as unverified.
- **What migration takes:** one generic builder keyed by the accepted step's kind, with the
  guard expressed as "the accepted step produced an event for this occurrence", from the
  cursor/occurrence key. The 20 public methods become thin wrappers or are deleted where
  callers can use the generic one. Size: 1-1.5 days, 1-2 PRs, plus Host/Orleans test
  re-runs. Low-to-medium risk because the builders do no rule logic, but they are a public
  surface of `CampaignHostAdapter`: check callers (including Orleans) before deleting.

**Net:** the executor (S1-S6) removes the `Pending` cascade tax and the staleness bug
class. It removes the Combat per-occurrence tax only if S7 is also done. If the owner
wants Turns 2-6 at the lowest per-Turn cost, S7 is not optional; budget S5 and S7 together.

## 6. Reproduce

```
dotnet test tests/Cna.Core.Tests --filter "FullyQualifiedName~PendingFromCursorSpikeTests"
# optional row-by-row dump:
PENDING_SPIKE_DUMP=/tmp/spike-out.txt dotnet test tests/Cna.Core.Tests --filter "FullyQualifiedName~PendingFromCursorSpikeTests"
```

Three tests: the real-session oracle (71 samples, 0 mismatches, 29 distinct subtypes), the
catalog-reachability check (32 reachable, 3 exceptions pinned), and the Move ambiguity pin.
