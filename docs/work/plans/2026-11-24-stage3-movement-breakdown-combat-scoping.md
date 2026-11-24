# Stage 3 Movement/Breakdown/Combat: per-mechanic scoping against current code (2026-11-24)

**Status:** scoping/design doc only. No engine code changed. Written against current
`main`-equivalent state of this worktree (tip `5d659b20`, PR #566 merged — every slice of
`2026-11-24-maintainability-refactor-direction.md` landed and merged per
`2026-11-24-maintainability-refactor-completion-summary.md`, carried on open, unmerged PR
#567). Verified directly against source for every claim below (file:line citations), not
inferred from either prior doc's description. Answers the question `2026-11-23-stage-3-scoping-verification.md`
left open: does the Nov 24 maintainability refactor change Stage 3's Movement/Breakdown/Combat
(MTS-TASK-009-equivalent) sizing, given that doc explicitly said the per-occurrence tax was
"not reduced by anything built so far" because the `PendingDecision` refactor "hasn't been
built yet"? **It has, since.** This doc re-verifies the sizing with that refactor now landed.

## 0. Verdict, up front

The Nov 23 doc's bottom line — "same cost as Stage 2's MTS-TASK-005/006, not less" — is
**partially corrected here**, not confirmed as-is. The maintainability refactor landed
*between* that doc and now, and it changed the picture unevenly:

- **Breakdown's native-class layer is now cheap** (Slot-ized, Slice 1, PR #560): adding a
  Stage 3 entry point is a 4th `Slot` row + one thin entry method, not a hand-rediscovered
  deviation the way Stage 2's own Breakdown slice (PR #523) had to find and fix.
- **Movement's and part of Reserve's `CampaignExercises`-layer query duplication is now
  cheap** (Slot-ized, Slice 2, PR #562): adding a Stage 3 source-query is a new `Slot` row
  reusing an existing `*Core` method, not a new hand-written method body.
- **The dominant cost — the per-occurrence `ExerciseSession` field pair, its matching
  `PendingDecision` subtype, and the `ComputePending`/`QueryCheckpoint`/`ReconstructOne`
  cascade-insertion tax — is untouched by any of this and is identical for every mechanic,
  Stage 2 or Stage 3**, confirmed by direct read of `ComputePending` (§1.5 below): it is
  still one hand-written `if` branch per occurrence, in the same method, for all 29 current
  occurrences, with no table/registry collapsing it.
- **Combat Decision/Assault/No-Attack's dispatch layer is completely untouched** — explicitly
  out of scope for the maintainability refactor (direction doc Section 2.1, Section 6) — so
  this one mechanic genuinely is "same cost as Stage 2, not less," exactly as the Nov 23 doc
  predicted, and remains the dominant driver of total Stage 3 cycle cost.

Net effect: **fewer, not dramatically cheaper, PRs are plausible** for Reserve/Movement/
Breakdown (the Slot technique turns "write a new method body" into "add a config row," which
both shrinks each slice and reduces the reason to isolate it in its own PR for risk control).
Combat Decision/Assault/No-Attack should budget the same as Stage 2 paid. See §5 for the
revised PR estimate.

## 1. Per-mechanic structural determination — verified against current code, not assumed

Confirmed complete mechanic list from the precedent docs (9 PRs, read in full): Reserve
Designation, Movement (+ Movement Completion), Breakdown, Combat Decision, Combat
Assault/No-Attack — for **both** acting sides. This matches the task brief's list; no sixth
mechanic found.

### 1.1 Reserve Designation — (a) mostly cheap, with one real caveat

`CampaignExercises.cs:7143-7181` (`ReserveSourceSlot` + `QueryReserveSourceCore`): Slot-ized,
but **only 2 of 3 occurrences are in the table** — `StageEntryReserveSourceSlot` (line 7152)
and `StageEntrySecondReserveSourceSlot` (line 7161). `QuerySecondReserveSource`
(`CampaignExercises.cs:7023-7032`) is **deliberately excluded** (comment at 7136-7142): its
fallback reaches `QueryNativeReserveReleaseState`, the pre-`#394` Reserve-Release/Cycle-Control
cascade that both the maintainability direction doc and this doc treat as permanently
out-of-scope hand-written code. A Stage 3 Reserve slot is a **new row in the existing table**
(cheap, reuses `QueryReserveSourceCore`) — same classification as (a) in the task's framing.

**Real caveat, not free:** each `ReserveSourceSlot` row's `FallbackPredecessor`/`FallbackWorld`
pair is genuinely different (StageEntry's reads `CurrentNoObligationFleetRepair`, i.e. the
Stage 1→2 preamble's terminal event; StageEntrySecond's reads
`CurrentStageEntryAssault`'s last `PostCombatPasses` entry). Stage 3's first-side Reserve needs
its own "what completed just before this" investigation — the Stage 2→3 preamble's own
terminal event — the same one-sane-answer-not-a-design-fork investigation
`2026-11-23-mts-task-006-stage-entry-second-reserve.md` did for Stage 2's second side. **This
investigation step is not eliminated by Slot-ization; only the resulting code's shape is
cheaper to write once the answer is known.**

The native class (`CampaignNativeSecondReserveV43`) needs no change at all — confirmed
unchanged since #518, predecessor matched by type (`CampaignExercises.cs` callers pass
`predecessor` positionally; no session-slot hardcoding exists in the native class itself).

### 1.2 Movement (+ Movement Completion) — (a) cheap, no caveat found

`CampaignExercises.cs:6858-6907` (`MovementSourceSlot` + `QueryMovementSourceCore`) and
`:6966-6988` (`MovementCutSlot` + `QueryMovementCutCore`): both fully Slot-ized, **all 3
current occurrences** (Second/StageEntry/StageEntrySecond) already in both tables. A Stage 3
variant is purely a new `Slot` row in each table, reusing both `*Core` methods unchanged — no
outlier found, no caveat. The native classes (`CampaignNativeSecondMoveV44`,
`CampaignNativeSecondMovementCompletedV45`) take explicit, type-matched predecessor
parameters (confirmed by every Stage 2 precedent doc's own "checked first" pass) — zero
native-class change regardless of stage count.

### 1.3 Breakdown — (a) cheap, native-class layer; thin hand-written wrapper still needed at the CampaignExercises layer

`CampaignNativeSecondBreakdownV46.cs:29-80`: Slot-ized (Slice 1, PR #560), confirmed by direct
read (§ above, full file read). `Create`/`CreateForStageEntry`/`CreateForStageEntrySecondSide`
are three one-line entry points over a shared `CreateCore(session, slot)` and three
`private static readonly Slot` values (`SecondSlot`, `StageEntrySlot`, `StageEntrySecondSlot`,
lines 39-67). A Stage 3 entry point is a 4th (and, for the second side, 5th) `Slot` value plus
a one-line entry method — this is **exactly** the task brief's scenario (a), confirmed by
reading the actual post-Slice-1 file, not inferred from the completion summary's prose.

**Not free:** `CampaignExercises.cs`'s own `CompleteSecondBreakdown` (6942-6958),
`CompleteStageEntryBreakdown` (7447+), `CompleteStageEntrySecondBreakdown` (7350+) are three
separate, ~15-line, hand-written methods — **not** Slot-ized, confirmed by grep (no
`BreakdownCompletionSlot`-shaped table exists). This was a deliberate scope cut, not an
oversight: these wrappers are thin enough (call the native class, advance contacts, build a
receipt) that the maintainability refactor never flagged them as a registry target. Stage 3
needs two more hand-written copies here, same size as every prior one — cheap by line count,
but not a Slot-table addition.

### 1.4 Combat Decision / Assault / No-Attack — (b) same cost as Stage 2, confirmed unreduced

`ExerciseSession.cs:1110-1114` (`CurrentRepeatAssault`, confirmed by direct read): a 4-way
null-coalesce today — `CurrentStageEntrySecondAssault ?? CurrentStageEntryAssault ??
CurrentSecondAssault ?? firstSideRepeatAssault` — exactly where Stage 2's own Assault slices
(PRs #529, #540) left it. No Slot/table abstraction wraps this; it is the same hand-grown
null-coalesce chain, confirmed untouched since #540. `WithRepeatAssaultEvent`
(`ExerciseSession.cs:722-...`) is the same shape, one `if` branch per occurrence.

The 11 Tier 2 methods identified by `2026-11-23-mts-task-005-006-stage-entry-assault.md`
(`QueryNativeReserveReleaseState` at `CampaignExercises.cs:6602`,
`SubmitNativeReserveReleaseChoice`:6650, `CompleteNativeReserveRelease`:6691,
`QueryPendingNativeCycleControl`:6729, `TimeoutNativeReserveRelease`:6757,
`TimeoutNativeCycleControl`:6802, `QueryPostCombatPassSource`:7465, `SubmitPostCombatPass`:7475,
`QueryTruckConvoyPassSource`/`SubmitTruckConvoyPass`:7563, `SubmitNativeCycleControl`:7597) all
confirmed still present, still hand-written, still hard-coded to the current 2-stage/2-side set
of explicit branches (`isStageEntry`/`isSecondSide`-style booleans) — **zero Slot/registry
abstraction**, consistent with the maintainability direction doc's own explicit exclusion
("`CampaignHostAdapter.cs`'s 20 `NativeXxxCommand` methods ... tracked as future
`PendingDecision` consumers, not this plan's concern" and "the pre-`#394` Combat/Reserve-
Release/Cycle-Control/Repeat chain ... explicitly out of scope"). A Stage 3 occurrence needs
the same per-site branch addition every one of Stage 2's Combat slices (PRs #526 and #529 for
the first side, #540 for the second side) already paid for. The native classes themselves
(`CampaignNativeSecondCombatDecisionV47.Select`, the repeat-assault runtime) remain fully
parameterized and need no change — confirmed by the same "checked first" pattern every prior
Combat slice used, not re-litigated here since nothing in the maintainability refactor touched
these classes.

**This is the one mechanic where "brand-new sibling wiring, same cost Stage 2 paid" (scenario
b in the task's framing) is the correct and unreduced classification.**

### 1.5 The dominant, genuinely-unreduced cost: the `ExerciseSession`/`PendingDecision`/cascade tax

`PendingDecision.cs` (full file read, 64 lines): a flat `abstract record PendingDecision` with
**29** `sealed record` one-line subtypes, one per `Current*`/`*Predecessor` field pair
currently in `ExerciseSession.cs` — 7 for `StageEntrySecond*` (Stage 2's own second side), 7
for `StageEntry*` (Stage 2's own first side), 7 for `Second*` (Stage 1's `#394` second side), 6
for the Stage 1→2 preamble chain, 2 for the prior stage's close-out. This was built by PR #548,
**after** Stage 2's own MTS-TASK-006 finished (#540) — it is a retrofit that gave the existing
per-occurrence fields a typed name, not a collapse of the per-occurrence count.

`ExerciseSession.cs:1777-1860` (`ComputePending`, full method read): confirmed to be exactly
one hand-written `if` branch per subtype, in strict chronological-descending order, each
branch's own comment (1797-1810, 1825-1832) explicitly documenting a staleness guard against
the next-older chain shadowing it. **A Stage 3 first-side-and-second-side pair adds 14 more
such branches to this one method** (7 per side, mirroring the existing `StageEntry*`/
`StageEntrySecond*` groups) — this is not reduced by `PendingDecision`'s existence; it is the
same shape, now also requiring a matching `PendingDecision` subtype declaration per branch.
`QueryCheckpoint` (`CampaignExercises.cs:4114`, confirmed to switch on `session.Pending`, per
its own comment at `:9157-9159` and `2026-11-24...completion-summary.md`'s note on PR #565) and
`ReconstructOne` (`CampaignExercises.cs:9150`) both switch on the same `PendingDecision`
subtype, one case each, **confirmed to remain switch *statements*, not expressions, specifically
because C# cannot treat a sealed-record hierarchy as closed for switch-expression
exhaustiveness** (PR #565, comment-only fix, no functional change) — so there is no registry
abstraction here to extend either; 14 more hand-written cases in each of these two methods, same
cost per case as the 29 already there.

**Correction to the Nov 23 doc's own framing, now that this is directly verified against
`PendingDecision.cs` rather than inferred:** the per-occurrence tax was never really "the
`ExerciseSession`-field tax" in isolation — it is a four-part tax paid identically for every
mechanic occurrence regardless of which stage: (1) the `Current*`/`*Predecessor` field pair
and its `With*` method, (2) the matching `PendingDecision` subtype declaration, (3) the
`ComputePending` branch (with its own staleness-guard reasoning to get right), (4) the
`QueryCheckpoint` and `ReconstructOne` cases. None of these four are Slot-ized or
registry-ized by the maintainability refactor — confirmed by direct read of all three files
it touched (§ above) and the two it explicitly declined to touch (Combat's dispatch layer).

## 2. Per-mechanic, per-side slice list

Each slice assumes the **gate in §4** (Stage 2→3 preamble bridge, MTS-TASK-008-equivalent)
has landed first, since every first-side entry point below needs that preamble's terminal
event as its predecessor — not something this cycle's own slices can supply.

| # | Mechanic | Side | Files | Pattern reused | Depends on |
|---|---|---|---|---|---|
| S3-1 | Reserve Designation | 1st | `ExerciseSession.cs` (field pair, `With*`), `CampaignExercises.cs` (new `ReserveSourceSlot` row, thin `Submit*`), `PendingDecision.cs` (1 subtype) | (a) Slot row + (4-part tax, unreduced) | Stage 2→3 preamble bridge (external gate, §4) |
| S3-2 | Movement + Movement Completion | 1st | same 3 files; new `MovementSourceSlot`/`MovementCutSlot` rows | (a) Slot row + tax | S3-1 (same-file ordering, same logical entry) |
| S3-3 | Breakdown | 1st | `CampaignNativeSecondBreakdownV46.cs` (new `Slot` row + entry point), `ExerciseSession.cs`, `CampaignExercises.cs` (new thin `Complete*`) | (a) Slot row (native + query layer) + tax | S3-2 |
| S3-4 | Combat Decision | 1st | `ExerciseSession.cs`, `CampaignExercises.cs` (`SubmitContactOwnerCombatSelection` gains one more mutually-exclusive `isXxx` branch, `QueryStageEntryXxxCombatWorldAndPosition` new method) | (b) net-new branch, same as PR #526 | S3-3 |
| S3-5 | Combat Assault/No-Attack | 1st | `ExerciseSession.cs` (`CurrentRepeatAssault`/`WithRepeatAssaultEvent` grow one branch), `CampaignExercises.cs` (11 Tier 2 methods each gain one branch; Tier 1 needs none, confirmed by the Assault slice's own "future-proofing payoff" claim, re-verified here as still true) | (b) net-new, same as PRs #529/#540 | S3-4 |
| S3-6 | Reserve Designation | 2nd | same shape as S3-1, entered from S3-5's own `PostCombatPasses` tail | (a) Slot row + tax | S3-5 (logical: this stage's own second side cannot start before its first side's Assault/Post-Combat-Pass tail resolves — confirmed below, not assumed) |
| S3-7 | Movement + Movement Completion | 2nd | same shape as S3-2 | (a) Slot row + tax | S3-6 |
| S3-8 | Breakdown | 2nd | same shape as S3-3 | (a) Slot row + tax | S3-7 |
| S3-9 | Combat Decision/Assault/No-Attack | 2nd | same shape as S3-4+S3-5 combined (Stage 2's own 2nd-side slice, PR #540, combined Decision+Assault into one PR since the 4-way-branch extension is mechanical once the 1st side's 3-way exists) | (b) net-new, same as PR #540 | S3-8 |

**First-side vs second-side independence — verified against the actual Stage 2 PRs, not
assumed:** `2026-11-23-mts-task-006-stage-entry-second-reserve.md`'s own words: "Follows ...
PR #529, which completed MTS-TASK-005 ... This slice begins MTS-TASK-006." Second-side Reserve
(#531) is explicitly entered from `CurrentStageEntryAssault.PostCombatPasses[^1]` — the first
side's own terminal artifact. **Second side is sequential-after-first-side, never
independent**, for both Stage 2 (confirmed) and Stage 3 (same dependency shape, no reason to
expect otherwise — the entry mechanism is structural, not stage-specific).

**Within a side, mechanics are sequential, confirmed by the actual PR chain:** `git log`
shows #519→#520→#523→#526→#529 (first side) and #531→#537→#539→#540 (second side) as one
continuous chain, each PR's own doc opening with "Follows [prior PR]." This is not merely
convention — each mechanic's entry point is literally the previous mechanic's completion field
(Movement reads `CurrentStageEntryReserve`; Breakdown reads `CurrentStageEntryMovementCompletion`;
etc.), so S3-1 through S3-5 and S3-6 through S3-9 are each a true logical chain, not just a
house-style choice to slice one-at-a-time.

## 3. Parallel vs. sequential — a materially different answer than the maintainability plan's

**None of S3-1 through S3-9 can run as parallel worktree agents against each other.** This is
a different answer than `2026-11-24-maintainability-refactor-direction.md`'s own Section 5,
which found Slices 2b/3a/4a/5/6 parallel — and the difference is structural, not a weaker
analysis: those slices touched disjoint files (`CampaignSourceAuthority.cs`,
`CampaignSourceBattlefield.cs`, `CampaignDurableAuthorityJournalStore.cs`, test fixtures).
Every slice in this plan touches the **same** `ExerciseSession.cs` (`ComputePending`'s single
`if`-cascade method) and the **same** `CampaignExercises.cs` (`QueryCheckpoint`'s and
`ReconstructOne`'s single switch methods) — confirmed by direct read of all three (§1.5). Two
slices both inserting a branch "ahead of" a named existing branch in the same method, per this
codebase's own explicit ordering discipline (every precedent doc's "same ordering discipline as
every cascade in this file"), is a textual collision even when there is no deeper logical
conflict — and here there also **is** a logical conflict (§2's dependency chain). **Verified
against how Stage 2 actually shipped, not assumed:** the real PR history (519, 520, 523, 526,
529, 531, 537, 539, 540) is one strictly sequential chain with zero parallel branches at any
point, for exactly this reason.

**What can run in parallel with this plan** (zero file overlap, confirmed): the Stage 2→3
preamble bridge work itself (MTS-TASK-008-equivalent — different files, the six
`CampaignNative*V49`-`V54` preamble classes and their own `ExerciseSession` slots), and any
unrelated workstream not touching `ExerciseSession.cs`/`CampaignExercises.cs`/
`CampaignNativeSecondBreakdownV46.cs`. Nothing currently open does (§4).

## 4. Gates

**PR #567** (`docs/maintainability-refactor-completion-summary`, open, unmerged): confirmed by
direct diff read to add exactly one new file, `docs/work/plans/2026-11-24-maintainability-refactor-completion-summary.md`
— a documentation-only completion record of already-merged PRs #551-566. **Not a code gate.**
It does not need to merge before any slice in this plan starts, and nothing in this plan
touches it.

**`gh pr list --state open`** (checked 2026-11-24): returns only PR #567. **No other open PR
exists**, so there is no in-flight collision with `ExerciseSession.cs`, `CampaignExercises.cs`,
or `CampaignNativeSecondBreakdownV46.cs` today (unlike the situation
`2026-11-24-maintainability-refactor-direction.md` found with PRs #551/#552).

**The real gate, not external to this repo's own backlog:** this plan's S3-1 (first-side
Reserve) needs the Stage 2→3 preamble bridge's own terminal event as its entry predecessor —
confirmed by direct analogy to how `QueryStageEntryReserveSource` itself is entered from
`CurrentNoObligationFleetRepair` (the Stage 1→2 preamble's terminal field,
`CampaignExercises.cs:7152-7159`). That terminal field does not yet exist for a *second*
occurrence of the six-event preamble chain — confirmed by grep: no `CurrentThirdStage*` or
equivalent second-preamble field exists anywhere in `ExerciseSession.cs` today. **This plan's
S3-1 cannot start until that preamble-bridge slice (MTS-TASK-008-equivalent, out of this plan's
scope per the task brief) lands.** The Nov 23 scoping-verification doc called this preamble
work "smaller than Stage 2's was" and found no new design fork — PR #547 (widening
`CampaignStageEntryPolicy`'s `operationStage` range to 1-3, confirmed by `git show`) is already
one small piece of that preamble-adjacent prep, landed ahead of time. Flagging this as a real
dependency this doc must not silently assume away, not scheduling the preamble slice itself
here.

## 5. Revised sizing estimate

**Stage 2's actual PR count for this cycle, counted from git history, not estimated:** 9 PRs —
#519, #520, #523, #526, #529 (first side: Reserve, Movement, Breakdown, Combat Decision, Combat
Assault/No-Attack), #531, #537, #539, #540 (second side: Reserve, Movement, Breakdown, Combat
Decision+Assault+No-Attack combined). **Rounds: 9, strictly sequential, 0 parallelism** —
confirmed by git log, every PR's own doc opens "Follows [immediately prior PR]."

**Stage 3 revised estimate:** the same 9-slice shape (S3-1 through S3-9 above) is the safe
baseline, but real cost reduction exists for 6 of the 9 (Reserve ×2, Movement ×2, Breakdown ×2)
now that the Slot technique is proven and requires no fresh investigation for *how* to extract
the pattern — only for *what the new stage's own entry predecessor is* (§1.1's caveat, §4's
gate). This makes consolidating Reserve+Movement+Breakdown per side into fewer, larger PRs a
reasonable call that Stage 2's own authors did not have available to them (they had to
discover each native-class deviation — or its absence — one mechanic at a time, which is why
they stayed at one-mechanic-per-PR). A plausible consolidation: Reserve+Movement+Breakdown
per side as 2 PRs (one per side) instead of 6, with Combat Decision and Combat Assault/No-Attack
staying separate per side (matching Stage 2's own real split, since that is where the actual
risk/size lives) — **6 PRs total** (2 consolidated + 2 Combat-Decision + 2 Combat-Assault,
or 2 consolidated + 2 combined-Combat if the second side again combines Decision+Assault the
way #540 did) **in place of Stage 2's 9, same strict sequential ordering, still 0 parallel
rounds.** This is a reduction in PR *count* and in the *investigation* cost already paid once
by Stage 2, not a reduction in the underlying wiring tax (§1.5), which is identical per
occurrence regardless of how the work is sliced into PRs.

**Not in this estimate, per the task's own scope boundary:** the preamble bridge (§4's gate)
and any Host/HTTP wiring — confirmed, by grep of every Stage 2 precedent doc, that **zero** of
Stage 2's 9 PRs touched `CampaignHostAdapter.cs` or any Host-layer file for any Stage-entry
mechanic; Host/HTTP wiring (MTS-TASK-003/007-equivalent) remained explicitly out of scope for
all of them, and this plan inherits that same boundary.

## 6. Explicit scope boundaries

- **EVT-001, absolute:** no slice changes any `CampaignNative*.SerializeCanonical()`/
  `Deserialize` byte output. Every native-class change in this plan (Breakdown's 4th `Slot`
  row) is additive parameterization over already-tested control flow, never a change to what
  gets serialized — same discipline the maintainability refactor's own Slice 1 already proved
  safe for this exact class.
- **DET-001/DET-002, absolute:** no slice introduces non-deterministic clock/id/random
  behavior; every new entry point reuses the existing deterministic seed/contract-version
  plumbing unchanged.
- **No `CampaignSnapshotV11` widening** — Stage 3's own order lives in the locally-built
  `StageEntryOrders`-style list, never in `CampaignSnapshotV11.OperationStageOrders`, same
  reasoning `2026-11-22-mts-task-005-stage-entry-reserve.md` already established and protected
  by `CampaignSnapshotV11SurfaceTests.cs`.
- **No Host/HTTP wiring** (`MTS-TASK-003`/`007`-equivalent) — confirmed out of scope for every
  one of Stage 2's 9 precedent PRs; this plan does not change that boundary.
- **No `CampaignHostAdapter.cs` changes** — its 20 `NativeXxxCommand` nullable-branching
  methods remain tracked as future `PendingDecision` consumers per the maintainability
  direction doc's own Section 2.1/4, not touched by this plan.
- **Does not schedule the Stage 2→3 preamble bridge** (§4's gate) — that is its own,
  separately-sized slice (MTS-TASK-008-equivalent), a precondition for S3-1, not a deliverable
  of this doc.
- **Does not re-open the pre-`#394` Reserve-Release/Cycle-Control ternary cascade** — the 9-site
  duplication the maintainability direction doc (Section 1) flagged and explicitly declined to
  schedule; this plan's Combat slices (S3-5, S3-9) add one more branch to each of the 11 Tier 2
  methods identified there, the same way Stage 2's own Assault slices did, without attempting
  to collapse the cascade itself.

## Critical files for implementation

- src/Cna.Core/Exercises/ExerciseSession.cs
- src/Cna.Core/Exercises/CampaignExercises.cs
- src/Cna.Core/Exercises/PendingDecision.cs
- src/Cna.Core/Campaigns/CampaignNativeSecondBreakdownV46.cs
- src/Cna.Core/Campaigns/CampaignNativeSecondReserveV43.cs (read-only; confirmed no change needed)
- src/Cna.Core/Campaigns/CampaignNativeSecondMoveV44.cs (read-only; confirmed no change needed)
- src/Cna.Core/Campaigns/CampaignNativeSecondMovementCompletedV45.cs (read-only; confirmed no change needed)
- src/Cna.Core/Campaigns/CampaignNativeSecondCombatDecisionV47.cs (read-only; confirmed no change needed)
- src/Cna.Core/Campaigns/CampaignNativeRepeatAssaultRuntime.cs (read-only; confirmed no change needed)
