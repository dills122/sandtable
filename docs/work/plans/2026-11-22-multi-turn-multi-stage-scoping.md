# Multi-turn / multi-stage looping — scoping (2026-11-22)

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

## 5. Open design questions for the owner

These are the genuine forks found during this pass — places where more than
one reasonable answer exists, or where a DET-001/DET-002/EVT-001/REL-001
ruling is needed before implementation. None are guessed at below.

**1. Does Stage 2+ get its own `CampaignSnapshotV11`-shaped type, or does
everything past Stage 1 move permanently to the native `(World, Position)`
chain?**
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
