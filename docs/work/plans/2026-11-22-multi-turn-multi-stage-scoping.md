# Multi-turn / multi-stage looping — scoping (2026-11-22)

> **2026-11-26 follow-up:** This is a dated baseline, not the current implementation ledger.
> #640, #645, #648–#650 and #658 cover bounded synthetic Stage 2/3 HTTP progression and real
> Combat rounds; #646, #651 and #652 prove Turn 1 through the next turn’s open Reserve.
> Turn 2 acceptance and later turns, native Reaction (#655) and executor migration remain open.
> See the [current roadmap](../../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).

**Status:** research/scoping only. No code or tests were changed to produce this
document. Does not modify or supersede
[`2026-11-21-initial-engine-release-scope.md`](2026-11-21-initial-engine-release-scope.md)
or [`2026-11-22-engine-release-session-summary.md`](2026-11-22-engine-release-session-summary.md),
which remain the coordinator's record of the just-shipped Operation Stage 1
release. This document answers the question those docs explicitly deferred:
what would it take to go from "one Operation Stage 1, both sides" to "all 6
turns," and how big/risky is that really.

Every claim below was re-verified against current code on this branch
(`main` at `61e80aaf`, this worktree's `HEAD`), not against the prior agent's
report. Where a prior citation turned out stale or imprecise, that is called
out explicitly rather than silently corrected.

## Headline finding

**The gap is real, but it is not "Core has no idea what Operation Stage 2
looks like."** It is narrower and more specific than that, which is both good
news (there's a real design pattern already proven out, twice) and bad news
(the pattern the project has used twice already does not generalize by
itself — it multiplies).

Three separate things are true simultaneously, and conflating them is the
easiest way to get this estimate wrong:

1. **The rules-sequence catalog (`Cna1979LandSequence.CreateTurn`) already
   enumerates the entire 6-turn structure** — all 3 Operation Stages, both
   acting-side blocks, Rail/Repair/Patrol, End of Turn, and the turn-to-turn
   wraparound via `GetNext` — for any `gameTurn` you ask it for. This is
   *dormant*, not missing: nobody has to invent a new phase catalog.
2. **The canonical runtime snapshot type, `CampaignSnapshotV11`, is
   structurally hard-wired to reject anything outside Game Turn 1 / Operation
   Stage 1.** This is enforced by its own validator, not just by a
   `CurrentCombatDecision`-shaped gate. It is not a feature flag or a
   `TODO`; it is an `Invalid("Mixed identity or stage context.")` thrown by
   `CampaignSnapshotV11Validator.RequireShape`.
3. **Everything the project has already built to go past that point (the
   second acting side of Stage 1) explicitly stopped using
   `CampaignSnapshotV11` at all**, and instead built a parallel,
   purpose-built "native event chain" (`CampaignNativeSecondReserveV43`
   through `V49`, `CampaignNativeTruckConvoyPassV41`,
   `CampaignNativePostCombatPassV42`, ...) carrying `(CampaignWorldSnapshotV6,
   LandSequencePosition)` pairs directly. This is the project's **actual,
   working answer** to "how do you make a mechanic happen again for a
   different occurrence without touching the 92-field, append-only
   `ExerciseSession`" — and it was arrived at deliberately, with the
   rejected alternative (giving `ExerciseSession` fields real reset
   semantics) named and declined in writing
   (`docs/work/plans/2026-11-18-combat-continuation-second-side-design-proposal.md`).

So multi-turn/multi-stage is not an unsolved architecture problem — it is a
**proven pattern that was only ever built for exactly one occurrence
("second acting side, Stage 1, Turn 1")** and would need to be re-executed,
by hand, once per (Turn × Stage × Side) combination that the six-turn
scenario actually visits, because the pattern as built is hardcoded to that
one occurrence rather than parameterized over turn/stage. That is a large
amount of mechanical, well-understood work, not a research problem — which
matches, and sharpens, the prior session's "large, previously-unscoped
architecture gap, not a bug" framing. See Section 2 for why it doesn't
generalize for free, and Section 4 for what doing it 1-occurrence-at-a-time
actually looks like sliced into tasks.

---

## 1. Exact gap inventory

### What "Core has a representation" means here

Three distinct levels, used consistently below:

- **Catalog-level**: does `Cna1979LandSequence`'s `LandSequencePosition`
  catalog contain a position for this step at all?
- **Snapshot-level**: can `CampaignSnapshotV11` (the canonical, replayable,
  fully-general runtime state type) represent being at this position?
- **Native-event-level**: does a dedicated, non-generic native class/event
  exist that can actually execute a decision at this position, Host-wired
  end to end?

### Stage 1 (both sides) → Stage 2's first step (Initiative Declaration)

- **Catalog-level: yes, dormant.** `Cna1979LandSequence.CreateTurn`
  (`src/Cna.Core/Rules/Cna1979LandSequence.cs:24-48`) loops
  `for (var operationStage = 1; operationStage <= 3; operationStage++)` and
  calls `AddOperationPrelude` (Initiative Declaration, Weather, Organization,
  Naval Convoy Arrival, Commonwealth Fleet Assignment/Repair) then
  `AddPlayerPhase` for both sides, for every stage 1-3, unconditionally. A
  Stage 2 Initiative Declaration position exists in the returned list the
  moment you call `CreateTurn(1)` — nothing has to be added to the catalog
  itself.
- **Snapshot-level: no, structurally rejected.**
  `CampaignSnapshotV11Validator.RequireShape`
  (`src/Cna.Core/Campaigns/CampaignSnapshotV11Validator.cs:25-31`) throws
  `Invalid("Mixed identity or stage context.")` whenever
  `snapshot.Setup.StageEntry.OperationStage != 1` or any
  `OperationStageOrders`/`OperationStageWeather` entry has
  `OperationStage != 1`. No `CampaignSnapshotV11` claiming Stage 2 can exist
  and pass validation, by construction.
- **Native-event-level: no.** `CampaignV11Preamble.Create`
  (`src/Cna.Core/Campaigns/CampaignV11Preamble.cs:41`) gates
  `ActFirstAction`/`ActLastAction` (Initiative Declaration) with
  `candidate.OperationStage == 1` explicitly, in the one place that
  implements the mechanic generically (not just the Graziani-content
  bridge). There is no Stage-2 equivalent anywhere.
- `Cna1979LandSequenceV4.IsSupportedCheckpoint`
  (`src/Cna.Core/Rules/Cna1979LandSequenceV4.cs:53-70`) — **re-verified,
  citation still accurate**: it walks the per-turn catalog and `break`s the
  moment it reaches `OperationStage == 1 && ActorRole ==
  LandActorRole.FirstActingSide && StepId == PositionDetermination`,
  without having matched; every candidate from that point on in the list
  (including all of Stage 1's second-side block, Stage 2, Stage 3, End of
  Turn) is unreachable by this method and returns `false`. (Line numbers are
  unchanged from the prior citation; a new method,
  `CampaignContactOwnerMovementCut.ProjectSecondCombatOwner`, was added
  elsewhere in the meantime for the #394 remediation, which is why some
  *other* citations below have shifted.)

**Verdict: fully unmodeled at the executable level.** The data (position
identity) exists; nothing can hold, validate, or act on it.

### Stage 2 → Stage 3

Same three-level answer, same citations (`CreateTurn`'s loop already
produces Stage 3 positions; `CampaignSnapshotV11Validator` rejects
`OperationStage != 1` regardless of whether the target is 2 or 3; no native
class exists). No new finding here — Stage 2 and Stage 3 fail for the
identical structural reason, not two different reasons. This matters for
Section 4: Stage 2 and Stage 3 are not independent risks: fixing the
snapshot/validator boundary (if that path is chosen) or building the
native-chain extension (if that path is chosen) addresses both at once,
architecturally, even though each stage's mechanics still need their own
native event classes per Section 3.

### Stage 3 → End of Turn

- **Catalog-level: yes, dormant.** `CreateTurn` appends one `AddPhase` call
  for `end-of-turn` / `LandStageIds.EndOfTurn` / `LandPhaseIds.EndOfTurn`
  after the `operationStage` loop (`Cna1979LandSequence.cs:38`). It carries
  no segment/step/actor role — it is a single bare phase position in the
  catalog.
- **Snapshot/native-level: no**, same reasons as above, plus: no event type,
  no `CampaignNativeXxx` class, and no legal-action derivation exists
  anywhere in the graph for a position with `PhaseId ==
  LandPhaseIds.EndOfTurn` (confirmed by `search_graph`/`search_code`: no
  hits for an End-of-Turn-specific event/native class). This is the one
  transition that is not just "gated off" — there is no code of any kind
  downstream of the catalog entry, dormant or otherwise. Victory/termination
  is explicitly confirmed absent too (per the frozen scope doc, item under
  "Deferred"), and End of Turn is presumably where a victory check would
  eventually need to live, which is itself an open design surface, not just
  an implementation gap.

### End of Turn → Turn 2's Initiative Declaration

- **Catalog-level: yes, dormant, and the one transition that's already
  fully wired at this level.** `Cna1979LandSequence.GetNext`
  (`Cna1979LandSequence.cs:50-66`) and its V4 wrapper
  (`Cna1979LandSequenceV4.GetNext`, `Cna1979LandSequenceV4.cs:19-27`) both
  already handle "last position of turn N → first position of turn N+1"
  generically: `currentIndex == positions.Count - 1 ? CreateTurn(current.GameTurn
  + 1)[0] : positions[currentIndex + 1]`. Turn-to-turn advancement is not a
  gap in the sequencing logic itself — it already works for arbitrary N.
- **Snapshot/native-level: no**, for the same structural reason as every
  other post-Stage-1 position: `CampaignSnapshotV11Validator` additionally
  enforces `sequence.GameTurn != snapshot.Setup.InitialGameTurn` as an
  independent rejection condition (`CampaignSnapshotV11Validator.cs:29`) —
  so even if Stage/side gating were somehow solved, the snapshot type still
  cannot represent a second Game Turn without that check also being revisited.

### Summary table

| Transition | Catalog (dormant data) | `CampaignSnapshotV11` | Native event class | Verdict |
| --- | --- | --- | --- | --- |
| Stage 1 (2nd side) → Stage 2 start | Yes (`CreateTurn` loop) | No (`OperationStage != 1` rejected) | No | Unmodeled, executable level |
| Stage 2 → Stage 3 | Yes | No (same check) | No | Same cause as above, not a second problem |
| Stage 3 → End of Turn | Yes (bare phase) | No | No — and no legal-action/event code of any kind | Unmodeled, including dormant code |
| End of Turn → Turn 2 Initiative | Yes, **and already generic** (`GetNext`) | No (`GameTurn != InitialGameTurn` rejected) | No | Sequencing logic ready; snapshot/native layer is the only blocker |

---

## 2. Core's session/state model and its re-entrancy constraints

### What was asserted, and what's actually true

The prior agent's report asserted `ExerciseSession` "cannot represent a
second Operation Stage without becoming re-entrant." That is true as stated,
but it undersells what's really going on, because `ExerciseSession` is not
one state model — it is two, glued together at one point in time:

1. **The `CampaignSnapshotV11`-backed first act.** `ExerciseSession`'s
   `CurrentSnapshot` (`src/Cna.Core/Exercises/ExerciseSession.cs:792-793`) is
   a `private` field, assigned exactly once — in the constructor — and never
   reassigned by any method in the class (confirmed: `grep` across the file
   for assignments to `currentSnapshot` finds only the one constructor
   assignment; every `With*` method that builds a successor via
   `MemberwiseClone()` inherits the parent's reference unchanged). This half
   of the session is exactly as re-entrant as `CampaignSnapshotV11` itself —
   i.e., not at all, because of the validator rejection described in Section 1.
2. **The native "V41-V49" event-chain second act**, which does not touch
   `CurrentSnapshot` at all. `WithSecondBreakdown`
   (`ExerciseSession.cs:827-849`) carries an explicit code comment making
   this deliberate, not an oversight:

   > `#394 gap 3: CurrentSnapshot is intentionally left untouched here -- a
   > direct CampaignSnapshotV11 pointing at the second side's post-breakdown
   > Combat/PositionDetermination position is rejected by
   > Cna1979LandSequenceV4.IsSupportedCheckpoint (confirmed by testing), so no
   > such snapshot is ever constructed. Gap 1/2/4 below instead carry the
   > second side's own (World, Position) pair directly...`

   This confirms, independently and in the project's own words, both the
   `IsSupportedCheckpoint` finding above and that the chosen fix was **not**
   "make the snapshot type represent more" but "stop using the snapshot type
   for this and carry the raw ingredients instead."

### Is this the same single-slot-field pattern as `#394` Gap 1, or worse?

**Confirmed: the same pattern, and multi-turn/multi-stage would make it
worse by multiplication, not by addition.**

`ExerciseSession` has 92 members total (`get_code_snippet` member count),
the large majority one-slot-per-mechanic fields (`CurrentCombatDecision`,
`CurrentCombatBarrage`, `CurrentCombatRetreatDecline`,
`CurrentCombatForceSeals`, `CurrentCombatSettlement`,
`CurrentCombatCustodyState`, ... — the full first-side Combat family alone is
~15 fields, per the `#394` design doc's own count, which this pass does not
need to re-run since the fields are unchanged). Every successor method
builds via `(ExerciseSession)MemberwiseClone()` and no field is ever reset to
null once set — confirmed again on this pass, not just inherited from the
prior report.

The `#394` design doc proposed two options for Gap 1 (`CurrentCombatDecision`
never clearing for the second side): Option A (give the field real reset
semantics — flagged as "the first field in this 56-field... class ever given
explicit reset/mutation semantics," a precedent change) or Option B (add
parallel side-scoped fields, leaving the existing ~70 call sites on the first
field untouched).

**Verified: Option B is what was actually built.** The class now carries, as
of this session, a full parallel field family for the second side:
`CurrentSecondReserve`/`CurrentSecondReserveWorld`, `CurrentSecondMove`/
`CurrentSecondMoveWorld`, `CurrentSecondMovementCompletion`,
`CurrentSecondBreakdown`, `CurrentSecondCombatDecision`,
`CurrentSecondNoAttackCompletion` (`ExerciseSession.cs:802-825`), each with
its own `WithSecond*` method, its own predecessor-chain field, and (for
Combat specifically) its own dedicated type
(`CampaignNativeSecondCombatDecisionStateV47`, not a reused
`CampaignNativeCombatDecisionStateV12`) — the code comment at
`ExerciseSession.cs:816-823` explains this was chosen specifically because
the second-side decision is keyed to a `(World, Position)` pair instead of a
`CampaignSnapshotV11`. The `CurrentCombatDecision` field and its ~70
first-side call sites are untouched, exactly as Option B predicted. The
class's "never reset once set" invariant — which the design doc called
"almost certainly load-bearing for the determinism/replay guarantees this
project cares about" — was preserved, not broken, confirming that risk
assessment was sound and the safer option was the one actually taken.

**Why this is worse, not just "the same," for multi-turn:** Option B's cost
model was "one new field family, audited against ~70 existing first-side call
sites, for one additional occurrence (second side, same stage, same turn)."
Generalizing to all 6 turns × 3 stages × 2 sides means the *same kind* of
parallel-field-family exercise would need to repeat for each additional
occurrence the scenario actually visits — not because the pattern breaks, but
because nothing about it is parameterized over turn/stage. `CurrentSecondMove`
is not "the move for whichever side/stage/turn is currently second"; it is
specifically and only "the second side's move, within Stage 1, within the
turn this session was constructed for" — confirmed by
`CampaignContactOwnerMovementCut.ProjectSecondMovement`
(`src/Cna.Core/Visibility/CampaignContactOwnerMovementCut.cs:63-103`)
hard-requiring `position.ActorRole != LandActorRole.SecondActingSide` (not
"whichever role is pending") as one of its validity checks — there is no
generic "nth occurrence" parameter anywhere in this family. A literal
continuation of the established pattern across a 6-turn, 3-stage scenario
would mean a new field family, a new native type, and a new
`With*`/projection method set *per mechanic, per stage, per turn* that needs
to repeat — a combinatorial growth in surface area, not the same fixed cost
paid once. This is the concrete, verified shape of "systemic pattern made
much worse," as opposed to a vague restatement of the prior claim.

### Net verdict for Section 2

- The re-entrancy claim is correct, but its cause is narrower and more
  precise than "the class can't represent it": `CampaignSnapshotV11` cannot
  represent it (by validator design), and `ExerciseSession`'s own one-shot
  `CurrentSnapshot` field inherits that limit permanently once constructed.
- This is the *same* systemic single-slot-field pattern as `#394` Gap 1, not
  a different or newly-discovered problem.
- The project already has a working, owner-implicitly-endorsed (via the
  Reaction ruling's "same pattern Combat mechanics already use for their own
  special-cased paths") answer to it: dedicated native wrapper types per
  occurrence, bypassing `CampaignSnapshotV11` and the generic
  `CampaignExercises.Query`/`Submit` gates entirely. That answer does not
  need to be re-litigated for multi-turn — but it needs to be *repeated*,
  deliberately, once per new occurrence, which is the actual size of this
  effort.

---

## 3. What's reusable vs. net-new

### Preamble mechanics (Weather, Organization, Naval Convoy, Fleet
Assignment/Repair, Reserve Designation, Initiative)

**Hard-coded to Operation Stage 1, confirmed at the generic (non-content)
implementation layer, not just the Graziani-content bridge.**
`CampaignV11Preamble.Create` (`src/Cna.Core/Campaigns/CampaignV11Preamble.cs:41`)
gates Initiative Declaration (`ActFirstAction`/`ActLastAction`) on
`candidate.OperationStage == 1` explicitly, in the actual Core mechanic
implementation used regardless of scenario content. The same file's `Apply`
method hard-codes `1` as the stage number when constructing
`CampaignOperationStageOrder`/event records
(`CampaignV11Preamble.cs:122`: `new CampaignOperationStageOrder(..., position.GameTurn,
1, ...)`), rather than reading it from `position.OperationStage`. A grep for
`OperationStage (!=|==) 1` across `src/Cna.Core` returns 104 matches across
40 files, including `CampaignSnapshotV11Admission.cs`, `StageEntryEvents.cs`,
`CampaignCombatStageEntry.cs`, `CampaignCombatWeather.cs`,
`CampaignLegalActions.cs`, `CampaignMovementActionDerivation.cs`,
`CampaignObservationV6/V7ActionDerivation.cs` — i.e. the hard-coding is
pervasive through the action-derivation and validation layers, not
concentrated in one or two gate functions. **This scoping pass did not
individually re-verify all 104 occurrences** (that is its own audit-sized
task, see Section 4) but did spot-check several of the highest-leverage ones
above and found every one hard-coding `1`, not reading the ambient stage.

Separately, `CampaignSourceSetupBridge.QueryTurn`
(`src/Cna.Core/Exercises/CampaignSourceSetupBridge.cs:12-125`) is the
Graziani-scenario-content-specific bridge (Fourth Indian Movement opening,
Gt3/Gt5 Supply scheduling, Polish Arrival, tactical-shipping decline, etc.),
separate from the generic mechanic above and wired into the real Host path
via `CampaignExercises`/`CampaignHostAdapter`/
`CampaignDurableAuthorityJournal.DrainPendingSystemSteps`. **Correction to
the prior citation**, which characterized this file as "only handles Game
Turn 1 / Operation Stage 1": re-reading the full method shows its last branch
explicitly handles `position.OperationStage == 3` (Polish Arrival at Naval
Convoy Arrival, Stage 3) — so it is not Stage-1-only. It is, however,
**Game-Turn-1-only**: every branch, including that Stage 3 one, requires
`position.GameTurn == 1` (or falls through to the final unconditional
rejection, which itself only accepts `GameTurn == 1 && OperationStage == 3`).
Stage 2 has no branch here at all. Net correction: this bridge is gated on
Game Turn, not strictly on Operation Stage — a different axis than the
original citation implied, and relevant because it means the *content*
layer's hard-coding and the *generic mechanic* layer's hard-coding
(`CampaignV11Preamble`) are two independent gates that would both need
addressing, on different axes (turn vs. stage), not one shared gate.

### Movement / Breakdown / Combat

Mixed — some of the supporting plumbing is already turn/stage-generic;
the actual entry gates for ordinary (first-side) play are not.

- `CampaignContactOwnerMovementCut.DeriveMovementOffers` (the path for
  **ordinary first-side Movement**, i.e. what every real player uses in
  Stage 1 today) hard-codes `sequence.OperationStage != 1` as one of its
  rejection conditions (confirmed in current source; this is the
  substantive content of the original `CampaignContactOwnerMovementCut.cs:159`
  citation — the exact line number has shifted because a new method,
  `ProjectSecondCombatOwner`, was inserted earlier in the file as part of
  the `#394` Gap 2/3 remediation since that citation was written, but the
  check itself is unchanged in substance).
- By contrast, `ProjectSecondMovement` and `Cna1979LandSequenceV4.
  RequireMaterializedMovement` (which it calls) are **not** hard-coded to a
  specific `GameTurn`/`OperationStage` value — they look the position up
  generically in `CreateTurn(position.GameTurn)` and validate it matches the
  catalog for whatever turn is passed in. The hard-coding on this path is
  narrower: `ProjectSecondMovement` requires `ActorRole ==
  LandActorRole.SecondActingSide` specifically (not "whichever side is
  pending"), so it is turn/stage-generic but occurrence-specific, which
  matches the Section 2 finding exactly — the plumbing under the native
  classes is often fine; the classes themselves are built for one named
  occurrence.
- `CampaignNativePostCombatPassV42.Create`
  (`src/Cna.Core/Campaigns/CampaignNativePostCombatPassV42.cs:16-43`) —
  **correction to a stale citation.** The task brief flagged this as
  possibly stale ("the owner opening for Post-Combat Pass was later
  confirmed to work for both sides via a different mechanism"). Confirmed:
  it does. The method takes an explicit `LandSide side` parameter and its
  own code comment states plainly: *"Both acting sides pass their own rail,
  repair and patrol phases. The second side's patrol pass ends Operation
  Stage 1's last side-block; nothing past its successor position is
  admitted this release."* Its validity check gates on
  `position.ActorRole is not (LandActorRole.FirstActingSide or
  LandActorRole.SecondActingSide)`, i.e. side-agnostic by design, not
  first-side-only. The "nothing past its successor position is admitted"
  half of that same comment is itself independent, additional confirmation
  of the Section 1 finding — the class's own author already knew and wrote
  down that the next position (Stage 2) is out of scope.
- Combat's generic throwing gates (`QueryContactOwnerMovementCut`/
  `QueryContactOwnerCombatCut` in `CampaignExercises.cs`, keyed off
  `CurrentCombatDecision is not null`) were **not** the path used to deliver
  second-side combat-to-a-result this session; per the frozen scope doc item
  5 and the Reaction ruling (item 3'), second-side Combat and Reaction both
  shipped via **dedicated non-generic native wrappers that sidestep those
  gates entirely**, consistent with Section 2's finding that this is the
  project's chosen, repeatable pattern rather than a one-off trick.

### Net reusability verdict

The **rules/content data** (position catalog, weather tables, movement cost
tables, combat tables) is already fully turn/stage-general — nothing about
*what* Operation Stage 2 Movement costs or *which* weather table applies
needs new source research. The **mechanical glue** (native event classes,
`ExerciseSession` fields, Host wiring, HTTP stages, legal-action derivation)
is what's occurrence-specific and needs rebuilding per stage/turn, following
the now-proven pattern. This means Section 4's slices are implementation
work with a known template, not open research — which is good news for
estimating risk, but does not shrink the number of slices needed.

---

## 4. Proposed phased task breakdown

Following this project's own convention for large efforts (see
`docs/design/combat-cycle-implementation-plan.md`'s `CMB-TASK-NNN` /
lettered-checkpoint structure, and the roadmap's `020`-`025` task numbering):
propose a new task prefix, `MTS-TASK-NNN` ("Multi-Turn/Stage"), organized
into checkpoints. Sizes are rough (S/M/L) in the same spirit as the Combat
plan's "3-5 files" sizing, not estimates in hours — this project's own
convention explicitly avoids calendar/effort promises at this stage.

### Dependency shape (the two independent axes)

**Operation Stage (1→2→3) and Game Turn (N→N+1) are genuinely independent
axes, confirmed by the evidence above — but Stage must be solved before Turn
can be exercised**, because Turn 2 cannot be *reached* without first getting
through Stage 1→2→3 of Turn 1 (the `GetNext` wraparound only fires after
Stage 3 → End of Turn, per Section 1's catalog walk). So the dependency is:

```text
Stage loop (1→2→3→End of Turn), Turn 1
           |
           v
Turn wraparound (End of Turn → Turn 2 Initiative) -- already generic, Section 1
           |
           v
Repeat the Stage loop for Turn 2 -- same mechanics, new occurrence per Section 2/3
           |
           v
... through Turn 6
```

Within a single stage, however, the generic preamble mechanics
(Weather/Organization/Convoy/Fleet/Reserve/Initiative) and the
Movement/Breakdown/Combat cycle are themselves two separable slices — the
Stage 1 build already proved this (preamble shipped in Sprint 3, Combat in
Sprint 5, separately). So within each new stage, preamble-first is also the
established order.

### Checkpoint MTS-A — unblock Stage 2 preamble (smallest end-to-end slice)

| Task | Output | Dependencies |
| --- | --- | --- |
| `MTS-TASK-001` (S) | Decide and record the `CampaignSnapshotV11`-vs-native-chain fork for Stage 2+ (see Open Question 1 below) — this gates every other task in this section and should not be guessed at. | None |
| `MTS-TASK-002` (M) | Generalize `CampaignV11Preamble.Create`/`Apply`'s Initiative Declaration path (and Weather/Organization/Convoy/Fleet alongside it) to read `OperationStage` from the position instead of hard-coding `1`, per the chosen MTS-TASK-001 approach. Audit the 104-match `OperationStage` grep for the subset that's actually load-bearing for the preamble mechanics specifically (not Movement/Combat, which are MTS-B). | `001` |
| `MTS-TASK-003` (M) | Host wiring + HTTP stage(s) for Stage 2's Initiative Declaration through Reserve Designation, following the same owner-opening pattern as `InitiativeOrder`/`ReserveDesignation` did for Stage 1 (PRs #497/#499). | `002` |
| `MTS-TASK-004` (M) | End-to-end HTTP test: both sides through Stage 2's preamble only (stop at Stage 2 Movement), cold-replay verified — mirroring the discipline that caught the System-audience and Movement-completion gaps this session. | `003` |

### Checkpoint MTS-B — Stage 2 Movement/Breakdown/Combat

| Task | Output | Dependencies |
| --- | --- | --- |
| `MTS-TASK-005` (L) | New native event-class family for Stage 2 ordinary (first-acting-side) Movement/Breakdown/Combat, following the `CampaignContactOwnerMovementCut`/native-V-series pattern — this is the "repeat the #394 exercise" work named in Section 2, for first-side-of-Stage-2 specifically. | MTS-A complete |
| `MTS-TASK-006` (L) | Second-acting-side equivalent for Stage 2, reusing the `#394`-proven side-scoped-field pattern directly (not rediscovering it). | `005` |
| `MTS-TASK-007` (M) | Host/HTTP wiring + end-to-end two-sided HTTP test through Stage 2, cold-replay verified. | `006` |

### Checkpoint MTS-C — Stage 3 (repeat of B's shape, plus Rail/Repair/Patrol's real content)

| Task | Output | Dependencies |
| --- | --- | --- |
| `MTS-TASK-008` (M) | Stage 3 preamble, same shape as `MTS-TASK-002`/`003` but also the Polish Arrival content-bridge path already partially present in `CampaignSourceSetupBridge` (Section 3) — confirm whether that existing Stage-3 branch is reusable as-is or needs rework once Stage 2 exists between it and Stage 1. | MTS-B complete |
| `MTS-TASK-009` (L) | Stage 3 Movement/Breakdown/Combat, both sides, same shape as `005`/`006`. | `008` |
| `MTS-TASK-010` (S) | Rail, Repair (×2), Patrol — currently confirmed **not started at all** (absent from `CampaignOwnerOpeningKind`, per the frozen scope doc items 9-11) with an explicit no-op/rejection path acceptable per that doc's own required-but-no-op bar. Decide now whether a legal no-op is acceptable at Stage 3 too, or whether these need real rules before Turn 2 can be meaningfully exercised (Open Question 4). | `009` |

### Checkpoint MTS-D — End of Turn and the Turn wraparound

| Task | Output | Dependencies |
| --- | --- | --- |
| `MTS-TASK-011` (M) | Design and implement an End-of-Turn checkpoint: currently zero code of any kind exists past the catalog entry (Section 1). Needs its own command/event, and a ruling on whether any end-of-turn bookkeeping (supply, cohesion recovery, etc.) is in scope for the engine-release bar or deferred like victory/termination. | MTS-C complete |
| `MTS-TASK-012` (S) | Wire the already-generic `GetNext` turn wraparound through the Host/HTTP path into Turn 2's Initiative Declaration. Lower risk than `011` because the sequencing logic itself needs no new work (Section 1) — this is almost entirely Host/HTTP plumbing once `011` lands. | `011` |
| `MTS-TASK-013` (M) | End-to-end HTTP test: full Turn 1 (all 3 stages, both sides) through Turn 2's Initiative Declaration, cold-replay verified. This is the actual proof that the wraparound works, not just that each piece does. | `012` |

### Checkpoint MTS-E — repeat for Turns 2-6

Per the dependency graph above, once one full Turn (1→Stage3→EndOfTurn→Turn2
start) is proven, Turns 2-6 are the **same mechanics repeated**, not new
unknowns — but "repeated" here means repeating the MTS-A/B/C pattern's
*mechanical* work (new native classes, new Host wiring) once per turn,
because of Section 2's finding that nothing in the current pattern is
parameterized over turn number either. Whether that repetition can be
collapsed (e.g., by generalizing the native classes to take a turn number
once the shape is proven, rather than hand-building Turn-3-specific classes)
is itself a design decision worth making explicitly before starting Turn 3's
work — see Open Question 2.

| Task | Output | Dependencies |
| --- | --- | --- |
| `MTS-TASK-014` (L, likely splits per-turn) | Turns 2-6, each turn's full Stage 1-3 + End of Turn, either by repeating MTS-A through D's pattern per turn, or by generalizing the native classes from MTS-A/B/C to be turn-parametric if `MTS-TASK-001`'s fork and Open Question 2 favor that — a decision that should be made once, after MTS-D, not re-litigated per turn. | MTS-D complete |

### What this breakdown deliberately does not include

- Victory/termination logic (confirmed absent; explicitly deferred by the
  frozen scope doc; no change recommended here).
- Rail/Repair/Patrol's *real* rule fidelity beyond a legal no-op, unless
  Open Question 4 is ruled the other way.
- Any Maproom/UI, content/scenario-accuracy, or packaging work — this
  breakdown is engine-only, matching the existing release's own scope
  boundary.

---

## Addendum (2026-11-22): the snapshot-representation fork, in concrete detail

**Owner ruling received:** sequencing — start the multi-turn engine work now;
Sprint 6/7 content-surface measurement does not need to be held for (resolves
Open Question 6 below). The snapshot-representation fork (Open Question 1)
is not ready to be ruled on yet; the owner asked for the concrete tradeoffs
before deciding. This section is that detail. No code was written to produce
it — same research-only discipline as the rest of this document. Both
options below are compared against current `main` at `366126fc` (this
document's own merged commit), not against a hypothetical.

### Framing: what "Option 1" concretely means

`CampaignSnapshotV11` is a single, versioned, sealed `record` type
(`CurrentContractVersion = 11`). There would not literally be a new .NET
type per stage (3 stages × 6 turns of distinct types would be absurd and
nobody is proposing it). "Its own `CampaignSnapshotV11`-shaped type"
concretely means one of:

- **1a.** Widen the *existing* `CampaignSnapshotV11` type's validator
  (`CampaignSnapshotV11Validator.RequireShape`) to accept any
  `(GameTurn, OperationStage)` the rest of the system is prepared to produce,
  without changing the record's shape at all — a validator-only change, no
  new contract version.
- **1b.** Freeze a new contract version (`CampaignSnapshotV12`, per this
  project's own versioning convention — every prior contract-affecting
  change in this file's history bumped a version number, e.g. V10 → V11)
  that is structurally identical to V11 but whose validator is the widened
  one from 1a.

The distinction between 1a/1b doesn't change the consumer-impact analysis
below (both still mean "the one canonical snapshot type now accepts a wider
range of `(GameTurn, OperationStage)` values") — it only changes whether
existing V11 history needs an explicit migration/compatibility story (1b) or
not (1a, since nothing about already-accepted V11 bytes changes meaning).
Given this project's own stated compatibility discipline ("old readers
reject new contracts... preserve old admission and historical fixtures,"
`combat-cycle-implementation-plan.md`), 1a is almost certainly the safer
reading. The rest of this addendum evaluates "Option 1" as 1a unless noted.

### Concrete consumer count for each option

**Baseline, re-verified this pass:** `CampaignSnapshotV11` is referenced in
exactly **75 files** project-wide: **40 files under `src/Cna.Core`** (142
individual occurrences), **15 files under `tests/Cna.Core.Tests`** (55
occurrences), and the remainder are historical design docs (not consumers).
**Zero occurrences in `src/Cna.OrleansHost` and zero in
`src/Cna.ExerciseRunner`** — confirmed by direct search, not inferred. Both
the real Host and the Exercise Harness consume `ExerciseSession`/
`CampaignExercises`'s `Query`/`Submit`/`Reconstruct` surface, never the
snapshot type directly:

- `IExerciseExecutionRuntime` (`src/Cna.ExerciseRunner/Execution/ExerciseExecutionRuntime.cs:54-68`)
  exposes only `Begin`/`QueryCheckpoint`/`Query`/`Select`/`Submit`/
  `Reconstruct` — none of these signatures mention `CampaignSnapshotV11`.
- `CampaignHostAdapter.cs` (4,612 lines, in `src/Cna.Core/Hosting`) has only
  **2** `OperationStage`-related occurrences in the entire file — it is a
  thin pass-through onto `CampaignExercises`, not an independent source of
  stage-hardcoding.

**This means the real, load-bearing consumer surface for either option is
entirely inside `Cna.Core`, and the Host/Harness need no changes under
either option** — a materially smaller blast radius than "75 files" sounds
like at first glance. The 40 `Cna.Core` files break down by role:

| Role | Files (representative, not exhaustive) | Option 1 (widen validator) impact | Option 2 (extend native chain) impact |
| --- | --- | --- | --- |
| The type + its validator/admission | `CampaignSnapshotV11.cs`, `CampaignSnapshotV11Validator.cs`, `CampaignSnapshotV11Admission.cs` | **Must change** — this is the actual fork | Untouched |
| Serialization | `CampaignSnapshotV11Serializer.cs` | Likely untouched (shape doesn't change under 1a) | Untouched |
| Event projectors (build next snapshot from an event) | `CampaignV11Preamble.cs`, `CampaignV11MoveProjector.cs`, `CampaignV11BreakdownProjector.cs`, `CampaignElementMovedV3Factory.cs`, `CampaignReactingElementMovedV2Factory.cs`, `CampaignBreakdownStopResolvedFactory.cs`, `CampaignBreakdownLifecycleFactory.cs`, `CampaignCreationV10.cs`, `CampaignCreationExecution.cs` | **Each must be individually audited and very likely changed** — `CampaignV11Preamble.Create` alone hard-codes stage `1` at `CampaignV11Preamble.cs:41` and `:122`; this pattern needs checking per file, not assumed fixed by the validator change alone | Untouched — these projectors stay exactly as they are, frozen to Stage 1/Turn 1 forever, because Option 2 never asks them to represent anything else |
| First-side Combat native classes keyed to V11 | `CampaignNativeCombatDecisionV12.cs`, `CampaignNativeCombatCustodyProjectionV25.cs`, `CampaignNativeCombatCustodyDispositionV25.cs`, `CampaignNativeCombatCustodyTimeoutV36.cs`, `CampaignNativeCombatSelectionTimeoutV38.cs`, `CampaignNativeReserveReleaseV27.cs`, `CampaignNativeCombatRetreatMovement.cs`, `CampaignNativePostRepeatRuntime.cs`, `CampaignNativeCombatSuccessorV26.cs`, `CampaignCombatSelectionFrame.cs`, `CampaignNativeRepeatAssaultRuntime.cs` (11 files) | Each takes/returns a `CampaignSnapshotV11` and would need auditing for stage-1 assumptions the same way `CampaignContactOwnerMovementCut.DeriveMovementOffers` does (Section 3) | Untouched if Option 2 also builds dedicated per-stage native classes for Combat (the likely shape, per Section 2's finding that this family already special-cases itself) |
| Legal actions / observation derivation | `CampaignLegalActions.cs`, `CampaignCurrentActionExecution.cs`, `CampaignObservations.cs`, `CampaignObservationV7Projector.cs`, `CampaignContactJournal.cs`, `CampaignContactAdjacencySource.cs` | Must audit for stage-1 hardcoding (these files also appear in Section 3's 104-match `OperationStage` grep) | Untouched for Stage 1's own paths; Stage 2+ gets its own parallel derivation, same shape as the existing `CampaignContactOwnerMovementCut.ProjectSecondMovement`/`ProjectSecondCombatOwner` |
| Session/orchestration | `ExerciseSession.cs`, `CampaignExercises.cs`, `CampaignCombatExerciseActions.cs`, `ExerciseCheckpoint.cs` | `Reconstruct`/`ReconstructOne`'s V11-rooted base case (the `chain.Count == 0` branch, `CampaignExercises.cs:7426`) stays as-is; no new branches needed since Option 1 doesn't add new native types | **Must add one new dispatcher branch per new native class** in both `Reconstruct` (`CampaignExercises.cs:7398-7435`) and `ReconstructOne` (`CampaignExercises.cs:8425-9434`) — mechanical, additive, confirmed by direct reading (see below) |
| Hosting | `CampaignAudienceOutcomeSource.cs`, `CampaignHostAdapter.cs` | Thin pass-through; low direct impact either way | Thin pass-through; low direct impact either way |
| Second-side bridge (already non-V11-rooted) | `CampaignNativeSecondCombatDecisionV47.cs`, `CampaignContactCombatSelectionCut.cs` (`ProjectSecondCombat`), `CampaignContactOwnerMovementCut.cs` (`ProjectSecondCombatOwner`) | Untouched (these already don't use V11 internally — they reference it only to assert they are NOT building one, per `ExerciseSession.cs:827-849`'s comment) | **This is the pattern to extend** — see enumeration below |

**Concrete count, Option 1:** at minimum the 3 validator/admission files plus
a stage-1-hardcoding audit of the ~9-file projector group and the ~6-file
legal-action/observation group — **roughly 18 files with a near-certain
required change**, plus an audit obligation (not necessarily a change) across
the remaining ~13 Combat/session files, before a single Stage 2 mechanic can
be implemented on top.

**Concrete count, Option 2:** **zero existing files change.** Every file in
the table above that's "untouched" stays byte-for-byte as it is. The work is
additive: new files (new native classes, modeled on the 7 enumerated below)
plus new `if`-branches appended to `Reconstruct`/`ReconstructOne`/`Query`/
`Submit`'s existing dispatch chains.

### DET-001/DET-002/REL-001 risk profile

**Option 1 (widen `CampaignSnapshotV11`) carries real, non-additive replay
risk.** `CampaignSnapshotV11Validator.RequireShape` is called from the
`CampaignSnapshotV11` constructor itself (`CampaignSnapshotV11.cs:68`,
`CampaignSnapshotV11Validator.RequireShape(this);`) — **every single V11
snapshot ever constructed, including replaying all of the already-accepted
Stage 1/Turn 1 history, runs through this exact method.** Widening it is not
adding a new accepted case alongside the old ones in isolation; it is
changing the one gate that *every* historical and future snapshot passes
through. Three independent hard-coded `== 1` checks were found in this one
method on this pass (`RequireShape`'s top-level `OperationStage != 1` check
at `CampaignSnapshotV11Validator.cs:~15`, the `OperationStageOrders`/
`OperationStageWeather` `Any(value => value.OperationStage != 1)` checks
immediately after, and a third, independent, easy-to-miss one deeper in the
same method: the `MovementEnded` marker check does
`snapshot.OperationStageOrders.SingleOrDefault(value => value.GameTurn ==
sequence.GameTurn && value.OperationStage == 1)` — a filter that would
silently return the wrong (or no) order once more than one stage's orders
can coexist in that list, unless it's also updated to filter by the
*current* stage, not literally `1`). Missing even one of these three while
widening would either wrongly reject valid Stage 2+ history or — the more
dangerous failure mode — wrongly *accept* something that should have been
rejected, which is exactly the shape of bug `#511`'s "two independent,
incomplete step-kind allowlist copies" finding (frozen scope doc item 2')
and the kind of defect DET-001/DET-002 exist to catch. **This is not purely
additive**: it requires re-reasoning about, and re-verifying, every existing
accepted-history invariant this validator currently enforces for Stage 1,
to make sure widening the gate doesn't also loosen something that was
coincidentally only true *because* Stage was pinned at 1 (the `ApplicableWeather`
method's `snapshot.OperationStageWeather.Single()` call, used by Breakdown's
weather-kind lookup, is one concrete example: it assumes exactly one weather
record exists in the list at all times, which is only true today because
only one stage's weather can ever be recorded; the moment a second stage's
weather can coexist, `.Single()` needs to become a filtered lookup, or it
throws on every post-Stage-1 Breakdown check). REL-001 ("snapshots are
replay checkpoints") is directly implicated: a less-complete widening could
produce a snapshot that replays to one state from the live event stream but
to a different (or throws on) state when reconstructed from history through
a slightly different, now-ambiguous validator path — exactly the class of
bug the project's own discipline this session was built to catch.

**Option 2 (extend the native chain) is genuinely additive for already-accepted
history, confirmed by direct reading of the replay dispatcher.**
`CampaignExercises.Reconstruct` (`CampaignExercises.cs:7398-7435`) walks
backward through a `switch`-like chain of `is not null` checks
(`CurrentSecondBreakdown`, `CurrentSecondMovementCompletion`,
`CurrentSecondMove`, `CurrentSecondReserve`, `CurrentPostCombatPass`,
`CurrentTruckConvoyPass`, each paired with its own predecessor field) down to
the one case that isn't a native successor, which falls through to
`ReconstructOne` rooted in `CampaignSnapshotV11` — and `ReconstructOne`
itself (`CampaignExercises.cs:8425-9434`) has the exact same shape: one `if
(completedSession.CurrentXxx is { } ...)` block per native type, each
independently replaying via that type's own `Create`, comparing serialized
bytes, and returning. **Confirmed directly in source (not inferred): these
blocks do not share mutable state or fall-through logic with each other** —
each is a self-contained `if` with its own `try`/`catch` and `return`. Adding
a Stage-2-first-side-Movement equivalent means adding one more such block,
modeled on the `CurrentSecondMove` block already there, which does not
require touching the five other existing blocks or the V11-rooted base case.
**The realistic risk under Option 2 is not "it breaks Stage 1 history"** (it
structurally cannot, since nothing about the existing blocks changes) **but
"the new block itself has a replay bug the first time it's written"** — the
same class of risk that `#511`'s already-encountered "two independent,
incomplete step-kind allowlist copies" bug represents, which is a real risk
but a *contained, per-occurrence* one, caught by this project's own
established practice of an immediate end-to-end HTTP+cold-replay test per
new native class (exactly as done for every `V4x` class shipped this
session). Net: **Option 1's risk is concentrated in one shared,
load-bearing gate that all history depends on; Option 2's risk is
distributed into N independent, individually-testable additions that cannot
regress each other.** This matches, and sharpens with a direct code
citation, the qualitative judgment in Section 2's "Net verdict."

One caveat that applies to **both** options equally, found during this pass
and not previously flagged: every native-chain `Create` method inspected
(`CampaignNativeSecondReserveV43.Create`, `CampaignNativeSecondMoveV44.Create`,
`CampaignNativeSecondMovementCompletedV45.Create`) and `ReconstructOne`'s
calls into them take their `gameTurn` value from
`predecessor.CurrentSnapshot.Setup.InitialGameTurn` — a field fixed forever
at campaign creation — rather than from a tracked "current game turn."
`CampaignSnapshotV11Validator.RequireShape` enforces the identical
assumption independently (`sequence.GameTurn != snapshot.Setup.InitialGameTurn`).
**This is a third, turn-specific hardcoding, orthogonal to the
stage-specific one, and it is shared infrastructure both options depend on**
— neither option avoids needing to resolve "what does `gameTurn` mean once
the campaign has advanced past its initial turn" before Turn 2 work (not
just Stage 2) can proceed. Flagging this now because it wasn't visible from
the Section 1/2 analysis alone and affects the Checkpoint `MTS-E` (Turns
2-6) estimate regardless of which fork is chosen here.

### Enumerating the "built for exactly one occurrence" spots (as requested)

Per the task's example (`ProjectSecondMovement` requiring `ActorRole ==
LandActorRole.SecondActingSide` specifically), a direct search for
`LandActorRole.SecondActingSide` across `src/Cna.Core` was run and each hit
classified by whether the containing method is **hardcoded to "second"
specifically** (cost: a new, differently-named method needed per future
occurrence) or **already side-generic** (cost: likely zero — same method
reusable as-is once fed a different position):

**Hardcoded to exactly "second acting side" (7 spots — these are the ones
that would need a same-shaped sibling per new occurrence under Option 2):**

1. `CampaignNativeSecondReserveV43.Create` (`CampaignNativeSecondReserveV43.cs:37`)
   — requires `position.ActorRole != LandActorRole.SecondActingSide` to
   reject; also requires its *successor* position's `ActorRole ==
   SecondActingSide` at `:46`.
2. `CampaignNativeSecondMoveV44.Source` (`CampaignNativeSecondMoveV44.cs:41`)
   — same shape, on the Movement owner-opening.
3. `CampaignNativeSecondMovementCompletedV45.Create`
   (`CampaignNativeSecondMovementCompletedV45.cs:32`) — requires the
   *successor* position's `ActorRole == SecondActingSide`.
4. `CampaignNativeSecondBreakdownV46.Create`
   (`CampaignNativeSecondBreakdownV46.cs:70,79`) — requires both the
   Movement position *and* its Combat successor to be `SecondActingSide`.
5. `CampaignContactOwnerMovementCut.ProjectSecondMovement`
   (`CampaignContactOwnerMovementCut.cs:73`) — the citation already in
   Section 3.
6. `CampaignContactOwnerMovementCut.ProjectSecondCombatOwner`
   (`CampaignContactOwnerMovementCut.cs:121`) — same file, Combat variant.
7. `CampaignReactionWindow`'s constructor identity check
   (`CampaignReactionWindow.cs:229`) — a narrower, Reaction-specific version
   of the same pattern, gating on `CampaignNativeSecondMoveV44`'s contract
   version specifically.

**Already side-generic (6 spots — good news: this is existing, working
prior art for Option 2's "generalize" direction, not a second problem to
solve):**

1. `CampaignNativePostCombatPassV42.Create` — takes `LandSide side`
   directly and checks `position.ActorRole is not (FirstActingSide or
   SecondActingSide)`, i.e. accepts either (Section 3's correction).
2. `CampaignNativeCycleControlV28.Create` — same `is not (FirstActingSide or
   SecondActingSide)` shape; the one side-specific behavior (rejecting
   "Repeat" for the second side) is an explicit, commented, owner-ruled
   carve-out (`CampaignNativeCycleControlV28.cs`, "Owner ruling 2026-11-21"),
   not a structural inability to represent the second side.
3. `CampaignReactionIdentity.CampaignSequenceV5Guards.RequireMaterializedMovement`
   /`RequireCurrentPosition` — both accept either role generically,
   resolving the correct side via the `CampaignOperationStageOrder`'s
   `FirstSide`/`SecondSide` fields rather than hardcoding one.
4. `CampaignCombatResultRelease.Derive` / `CampaignCombatCycleMovement.Derive`
   / `CampaignCombatReserveReleaseCodec.Validate` — all three resolve
   `LandActorRole` from a `PlayerPhaseSlot` *string* field (`"first-acting-side"`
   / `"second-acting-side"`) carried on their own cycle/boundary record,
   rather than hardcoding a role — a role-as-data pattern, not role-as-code.
5. `CampaignSourceTurnAuthority.ResolvePhasingSide` — resolves either role
   generically from `StageOneFirstSide`.
6. `CampaignNativeReserveReleaseStateV27.SecondSidePosition` — this one
   *is* second-side-named specifically, but it's a narrow, self-contained
   helper (13 lines) inside a class that otherwise already handles both
   sides via the `PlayerPhaseSlot` pattern above — i.e. the "hardcoded" part
   here is small and already surrounded by generic infrastructure.

**What this enumeration actually shows:** the project's own pattern visibly
*evolved* over the course of the `#394` work, from "write a dedicated
`Second*`-named type per owner-opening decision" (spots 1-7, all from the
earlier `V43`-`V47` wave — the actual player-facing decision points: Reserve,
Move, Movement-completion, Breakdown, Combat selection) to "take the role/side
as data and branch generically" (spots 1-6 in the second list, mostly the
*later*-landed continuation/infrastructure pieces: Post-Combat Pass, Cycle
Control, Reaction identity, Reserve Release derivation). **The
"hardcoded" cost under Option 2 is concentrated specifically in the 7
player-decision-point classes, not spread evenly across the whole native
chain** — and there is already-proven, working prior art inside this exact
codebase for how to make a decision-point class side/role-generic (the
`PlayerPhaseSlot`-as-data pattern), it just hasn't been applied yet to the 7
decision-point classes themselves.

### How much of the native-chain generalization carries over per phase

Given the above, a concrete estimate for Section 4's Checkpoints:

- **If Option 2 is built "hand-build it" style** (repeating the `#394`
  wave's original pattern literally, i.e. naming new types per occurrence
  — `CampaignNativeStage2FirstMoveVxx`, `CampaignNativeStage2SecondMoveVxx`,
  etc.): **zero carryover between phases for the 7 decision-point classes.**
  Each of `MTS-TASK-005`/`006` (Stage 2 Movement/Breakdown/Combat, both
  sides) and `MTS-TASK-009` (Stage 3, same) would need its own full set of
  Reserve/Move/MovementCompletion/Breakdown/CombatSelection classes, each
  with its own `Reconstruct`/`ReconstructOne` dispatcher branch. The
  continuation/infrastructure pieces (Post-Combat Pass, Cycle Control,
  Reaction identity, Reserve Release derivation) **do carry over for free
  today**, because they're already side-generic and most of them
  (`CampaignNativePostCombatPassV42`, `CampaignNativeCycleControlV28`) key
  off the actual `LandSequencePosition` passed in via `Cna1979LandSequenceV4.CreateTurn`/`GetNext`
  rather than a hardcoded stage number — confirmed by re-reading
  `CampaignNativePostCombatPassV42.Create` (Section 3): it derives `position`
  and `next` generically from whatever `positionId`/`gameTurn` it's given.
- **If Option 2 is built "generalize now" style** (applying the
  `PlayerPhaseSlot`-as-data pattern to the 7 decision-point classes before
  Stage 2 work starts, i.e. a `MTS-TASK-001b` that refactors
  `CampaignNativeSecondReserveV43`-style classes into side/stage-generic
  ones first): the continuation pieces need no work (already generic); the
  7 decision-point classes become a one-time refactor cost paid once, and
  `MTS-TASK-005/006/009` collapse from "write N new classes" to "call the
  now-generic classes with a different position" — much closer to the
  additive, low-risk shape Option 2 already has for the continuation
  pieces. This is the generalize-the-native-chain analog of Option 1's
  validator-widening, but confined to 7 already-identified, already-small
  classes instead of a shared gate every snapshot passes through — a
  meaningfully smaller and more contained generalization task than Option
  1's.

### Hybrid option (not previously explored)

**Prove the "generalize now" native-chain refactor on Stage 2 only, before
deciding whether to carry it through Stages 3 and Turns 2-6.** Concretely:
do `MTS-TASK-005`/`006` (Stage 2 Movement/Breakdown/Combat) once, the
"generalize now" way — refactor the 7 decision-point classes to take
side/stage as data (the `PlayerPhaseSlot` pattern already proven out
elsewhere in this exact codebase) rather than naming a new type — and treat
that as the real-world proof of whether the pattern generalizes cleanly a
*second* time (it has, so far, only ever been generalized once, for
Post-Combat Pass/Cycle Control/Reaction identity, none of which are the
decision-point classes themselves). If it holds up cleanly for Stage 2, the
same generalized classes should carry forward into Stage 3 and Turns 2-6
largely for free, collapsing most of Checkpoint `MTS-E`'s combinatorial
repetition. If it does not hold up cleanly — if Stage 2's actual mechanics
turn out to need something the `PlayerPhaseSlot` pattern can't express
generically — that's discovered on the smallest possible slice (one stage),
not after committing the pattern across 3 stages × 6 turns, and the fallback
is simply to finish Stage 2 "hand-build it" style and reconsider before
Stage 3. This hybrid does not touch `CampaignSnapshotV11` or its validator
at all, so it carries none of Option 1's shared-gate replay risk regardless
of how it resolves, and it turns "should we generalize the native chain" from
a single all-or-nothing bet into a one-stage experiment with a defined
fallback.

### Recommendation

**Option 2 (permanently generalize the native `(CampaignWorldSnapshotV6,
LandSequencePosition)` chain), via the hybrid sequencing above, not Option
1.** Reasoning, in order of weight:

1. **Risk shape is the deciding factor, not effort.** Option 1 requires
   changing a single gate (`CampaignSnapshotV11Validator.RequireShape`) that
   every unit of already-accepted Stage 1/Turn 1 history is replayed
   through, with at least three independently-hardcoded `== 1` checks found
   in that one method and at least one further `.Single()`-shaped latent
   assumption (`ApplicableWeather`) that only happens to be safe today
   *because* Stage is pinned at 1. That is a direct REL-001/DET-001 exposure
   on the one thing this project has spent the most verification effort
   protecting all session. Option 2's risk is structurally contained to new,
   independently-testable, per-occurrence additions that cannot regress
   already-accepted history, confirmed by direct reading of `Reconstruct`/
   `ReconstructOne`'s dispatch shape, not asserted.
2. **Option 2 already has more real prior art in this codebase.** Six of
   the thirteen `SecondActingSide`-touching spots found this pass are
   already side-generic, proving the pattern Option 2 needs (role-as-data,
   keyed off the real `LandSequencePosition`) already works here for the
   continuation/infrastructure layer. Option 1 has no comparable prior art
   for "widen `CampaignSnapshotV11`'s validity envelope safely" — it would
   be a first-of-its-kind change to the single most load-bearing gate in the
   replay pipeline.
3. **The consumer blast radius is genuinely smaller for Option 2.** Roughly
   18 files need a near-certain change under Option 1 before any Stage 2
   mechanic can ship, against a well-defined audit obligation on another
   13; Option 2 changes zero existing files.
4. **This does not foreclose Option 1 later.** Nothing about building Stage
   2/3 on a generalized native chain forecloses a future decision to widen
   `CampaignSnapshotV11` for some other reason (e.g. if the native chain
   itself starts accumulating its own version of the "92-field, append-only"
   problem `ExerciseSession` already has — a real possibility worth
   watching for, not confirmed, given the chain's own classes already number
   in the double digits). It is the lower-risk default to start from, not
   claimed to be unconditionally correct forever.

This recommendation is this document's own synthesis, offered because the
owner asked for one — it is not a unilateral decision on the fork itself.
The owner may weigh the "Option 1 is more conventional/less surprising
architecturally" consideration differently, and nothing above should be
read as foreclosing that judgment call.

### Owner ruling (2026-11-22)

**Option 2, via the hybrid proposed above.** Reasoning, in the owner's own
words (paraphrased, recorded here for the project's record per this
document's own citation discipline — this is a decision record, not a new
finding):

> The native chain isn't a new thing being introduced, it's already proven.
> Option 1's unified-snapshot architecture might be cleaner in the abstract,
> but rewriting the shared validator that all already-accepted history
> replays through is a real-risk change for a payoff that's architectural
> tidiness, not new capability right now. Choosing the hybrid doesn't
> foreclose consolidating to Option 1 later if the native-chain approach
> starts accumulating real debt as more stages/turns land — that becomes an
> informed decision later instead of a guess now.

This directly adopts the recommendation's reasoning (risk shape over
cleanliness, existing prior art, smaller blast radius, non-foreclosure of
Option 1 later) rather than overriding it. **`CampaignSnapshotV11` and
`CampaignSnapshotV11Validator` are not to be modified as part of this
work.** Implementation proceeds per the hybrid: apply the "role as data"
refactor to the 7 enumerated decision-point classes, scoped to Stage 2
only, with the defined fallback (finish Stage 2 "hand-build it" style) if
the refactor does not generalize cleanly. See
[`2026-11-22-mts-a-stage2-role-as-data-implementation.md`](2026-11-22-mts-a-stage2-role-as-data-implementation.md)
for the implementation record.

---

## 5. Open design questions for the owner

These are the genuine forks found during this pass — places where more than
one reasonable answer exists, or where a DET-001/DET-002/EVT-001/REL-001
ruling is needed before implementation. None are guessed at below.

**Owner ruling received 2026-11-22:** sequencing (Open Question 6) —
start the multi-turn engine work now; Sprint 6/7 content-surface measurement
does not need to be held for. Open Question 1 is addressed in detail in the
new Addendum section above, with a recommendation; still awaiting the
owner's actual ruling.

**1. Does Stage 2+ get its own `CampaignSnapshotV11`-shaped type, or does
everything past Stage 1 move permanently to the native `(World, Position)`
chain?** **See the Addendum above for the concrete tradeoff detail and a
recommendation.**
Section 2 found that the project's actual, working answer to "mechanic
happens again" has so far always been "bypass `CampaignSnapshotV11`, carry
`(CampaignWorldSnapshotV6, LandSequencePosition)` directly." That answer
was made once, implicitly, under `#394`'s narrower scope (second side of
Stage 1 only) — it has never been explicitly ratified as the general answer
for *all* future occurrences, including Stage 2/3 and Turn 2-6. The
alternative — loosening `CampaignSnapshotV11Validator`'s `OperationStage !=
1`/`GameTurn != InitialGameTurn` checks to accept a wider, still-structurally-
validated range — was not attempted or evaluated by this pass and may or may
not be safer; no claim is made either way. This is the single highest-
leverage decision in this entire scoping effort, because every task in
Section 4 depends on which one is chosen, and reversing course after
building several stages/turns on one model would be expensive. This also
touches REL-001 directly (snapshots as replay checkpoints) and DET-001/002
(deterministic reconstruction) — a wrong or informal answer here risks a
replay/determinism regression the same way `#394` Gap 1 almost did.

**2. Should the native V-series classes be generalized to be
turn/stage-parametric, or hand-built once per occurrence as `#394` was?**
Section 2/4 found the current pattern (`ProjectSecondMovement`,
`CampaignNativeSecondCombatDecisionV47`, etc.) is hardcoded to one named
occurrence rather than parameterized. Generalizing them now (before Stage 2
work starts) front-loads design risk but could collapse Section 4's
per-turn repetition into a much smaller amount of work; keeping the proven
"one occurrence at a time" pattern is lower-risk per slice but implies the
full combinatorial task count in Section 4's Checkpoint E. This is a real
engineering-tradeoff fork, not a correctness question, and the project has
not stated a preference either way in anything this pass read.

**3. What is the actual definition of "End of Turn" for this engine, beyond
a catalog phase marker?**
Section 1 found genuinely zero code — not even a dormant/rejected
attempt — downstream of the End-of-Turn catalog entry. Real CNA-1979 rules
likely require some end-of-turn bookkeeping (supply, cohesion/readiness
recovery, ammunition resupply, etc. — this pass did not re-derive the
specific rules, since that is a source/content question, not a code
question). Before `MTS-TASK-011` can be scoped precisely, the owner needs to
rule on whether any such bookkeeping is in scope for engine-release
"doesn't silently halt or fake an answer" bar, or whether End of Turn can be
a true no-op transition for this release the same way Rail/Repair/Patrol are
allowed to be.

**4. Do Rail, Repair, and Patrol need real rule fidelity to reach Turn 2, or
does the existing "legal no-op, FID-002-compliant rejection" bar (already
accepted for this release per the frozen scope doc items 9-11) extend
unchanged through all 6 turns?**
The frozen scope doc already rules these acceptable as no-ops for the
Stage-1-only release. This pass found no evidence that reaching Turn 2-6
changes that calculus technically — but it is plausible that some turn
later in the 6-turn Graziani scenario has scripted content (arrivals, supply
changes) that depends on a Repair or Patrol actually having happened, which
would turn "legal no-op" into a silent-approximation risk (FID-002) rather
than a safe deferral. This pass did not check the Graziani source content
for that dependency (out of scope for an engine-architecture pass; belongs
with the Sprint 6/7 content work per the roadmap) — flagging it here so it
isn't silently assumed either way before that content work happens.

**5. Is a six-turn HTTP end-to-end test (the Section 4 pattern of "build
the mechanic, then immediately prove it with zero backdoors over real HTTP")
required at every checkpoint, or only at full-turn boundaries?**
This session's own retrospective (`2026-11-22-engine-release-session-summary.md`)
credits exactly this discipline for catching the System-audience and
Movement-completion gaps that five separate "done" claims had missed. Section
4 assumes it applies at every checkpoint (MTS-A through E each end in an
HTTP test), which is the safer and more expensive choice. The owner may
prefer a lighter-weight gate for intermediate stages with a full HTTP
proof only at Turn boundaries — a real cost/rigor tradeoff, not a
correctness question, and not decided here.

**6. Is the Sprint 6/7 roadmap boundary ("split after the scenario's
exercised-rule inventory is measured," `pre-alpha-roadmap.md:1138-1139`)
meant to happen before or after this multi-turn engine work?**
The roadmap's own Sprint 7 section explicitly defers its own splitting until
the six-turn surface is measured, and nothing in the roadmap currently
sequences "engine can loop all 6 turns" against "measure the six-turn
content surface." These could proceed in parallel (content measurement
doesn't need a working engine loop) or the owner may want the engine
capability proven first so content measurement isn't done against an engine
that can't yet execute it. No existing roadmap text rules on this either
way — confirmed by reading the full Sprint 6/7 sections, not inferred.
