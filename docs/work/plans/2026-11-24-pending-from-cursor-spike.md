# Spike S1: can `ExerciseSession.Pending` be derived from a sequence cursor? (2026-11-24)

**Status:** spike report. New files only: this doc and
`tests/Cna.Core.Tests/Exercises/PendingFromCursorSpikeTests.cs`. No production source
changed, including the three hot files. Executes §3.6 of the synthesis doc (proposal 3's
de-risking spike).

## 0. Verdict

**GO, conditionally.** 26 of the 29 `PendingDecision` subtypes (89.7%) are derived by a
pure function of the catalog cursor plus a two-value last-event flag. The other 3 are the
`*NoAttack` subtypes, which are shadowed arms: they can be deleted instead of derived.
Of the 26, 23 were matched against real driven sessions with zero mismatches; the 3
`*Move` subtypes are derivable but were not driven (see §3).

The 90% line is cleared on the 26 live arms (100%) and missed by 0.3 points on the raw
count of 29. The exceptions are explained by data (§2), not by "the field is still
non-null". Read this as "feasible", with the caveats in §3 attached.

## 1. What the spike answered

1. **Does `ExerciseCheckpoint` expose a position id?** Yes. `ExerciseCheckpoint.PositionId`
   is a public property (`ExerciseCheckpoint.cs:758`), filled by every constructor. It is
   the **next** position to act at, not the position of the event just completed
   (e.g. after Initiative Declaration it is `...weather-determination`).
2. **Fixtures enumerated** (all on `origin/main`):
   - A real, driven session through Operation Stage 1 (both sides) and Stage 2 (preamble
     and both sides), mirroring `CampaignStageThreeEntryVerificationTests`. 65 samples
     (`Pending` plus `QueryCheckpoint().PositionId` after every accepted step), 23 distinct
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
| Mechanical, verified against real sessions | 23 | Every one of the 65 samples matched; includes states with 30 non-null `Current*` fields. |
| Mechanical, derived but not driven | 3 | `SecondMove`, `StageEntryMove`, `StageEntrySecondMove`. Need `LastEvent.MoveWithinPosition` (see below). |
| Explicit exception | 3 | `SecondNoAttack`, `StageEntryNoAttack`, `StageEntrySecondNoAttack`. |
| Total | 29 | |

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

**The staleness guards cost nothing under a cursor.** `ComputePending` has 13 guarded arms
(7 gated on `CurrentInitiativeDeclaration is null`, 6 on `CurrentStageEntryReserve is
null`), not the eight the synthesis doc expected. A cursor is one value, so a stale field
cannot shadow a later one. The real-session samples show this directly: late samples carry
30 non-null `Current*` fields and the derivation still matches. These guards are not
exceptions; they disappear.

**Other findings.**
- `TruckConvoyPassPending` and `PostCombatPassPending` are only reachable in the legacy
  Operation Stage 1 first side. Everywhere else the Assault arm shadows them (the passes
  accrete inside the Assault runtime). The occurrence table captures this as one
  `TailOwner` flag, not as 6 extra arms.
- The pre-#394 legacy chain (Stage 1 first side, Reserve through Combat) has no
  `PendingDecision` at all. The table encodes it as a `null` prefix.
- Operation Stage 3 has no occurrence row. `Derive` returns `null` there. Adding Stage 3
  becomes table rows (prefix plus a tail-owner flag) instead of 14 arms per method.

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
2. **PR B, switch `ComputePending` to the derived value (medium).** Replace the 29-arm
   cascade and delete the 13 staleness guards. Hot file; needs exclusive ownership.
   Requires the §3 gaps closed or the NoAttack arms removed first.
3. **PR C, `QueryCheckpoint` and `ReconstructOne` dispatch off the derived key (medium).**
   Both already switch on `Pending`; they keep constructing the checkpoint from the event
   but select by cursor.
4. **PR D, delete the field pairs and the 29 subtypes (large, mechanical).** Only after C.

So the first migration PR (A) is 1 PR and the `ComputePending` swap (B) is a second; the
deletion work (C, D) is separate. That matches the "1 to 2 PRs for the first migration"
branch of the synthesis doc, with C and D as follow-ups, not blockers.

## 5. Reproduce

```
dotnet test tests/Cna.Core.Tests --filter "FullyQualifiedName~PendingFromCursorSpikeTests"
# optional row-by-row dump:
PENDING_SPIKE_DUMP=/tmp/spike-out.txt dotnet test tests/Cna.Core.Tests --filter "FullyQualifiedName~PendingFromCursorSpikeTests"
```

Three tests: the real-session oracle (65 samples, 0 mismatches, 23 distinct subtypes), the
catalog-reachability check (26 reachable, 3 exceptions pinned), and the Move ambiguity pin.
