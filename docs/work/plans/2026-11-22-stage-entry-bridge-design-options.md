# Stage 2 entry bridge — design investigation (2026-11-22)

**Status:** design investigation only, per the owner's instruction — no code in this
pass. Answers the question raised in
[`2026-11-22-mts-a-stage2-role-as-data-implementation.md`](2026-11-22-mts-a-stage2-role-as-data-implementation.md)'s
correction: what native event should represent "Stage 2 has begun, side X may act,"
since `CampaignSnapshotV11` cannot represent it and nothing else currently does either.
Presents concrete options with tradeoffs, not a final design — the owner rules on which
option (or hybrid) to build.

Everything below was re-verified against current code on `mts-a-stage2-role-as-data`
(commit `53fdd588`), not assumed from the prior addendum.

## The precedent, re-derived precisely (this corrects a gap in the prior addendum)

The prior addendum characterized the architecture as "V11 for Stage1-FirstSide, native
chain for everything else." That's directionally right but imprecise in a way that
matters for this design: **`CampaignSnapshotV11` stops being usable far earlier than "the
whole first side's cycle"** — it stops at the exact moment the first Combat selection is
made, not at Reserve Release or Truck Convoy.

### Where V11 actually ends

`Cna1979LandSequenceV4.IsSupportedCheckpoint` (`src/Cna.Core/Rules/Cna1979LandSequenceV4.cs:53-70`,
re-verified unchanged since the first scoping doc) walks the full per-turn catalog and
`break`s the instant it reaches `OperationStage==1 && ActorRole==FirstActingSide &&
StepId==PositionDetermination` without a match. Because `CampaignSnapshotV11Validator.RequireShape`
calls `CampaignSequenceV6Guards.RequireCurrentPosition`
(`src/Cna.Core/Campaigns/CampaignSequenceV6Guards.cs:7-23`), which calls
`IsSupportedCheckpoint` directly and throws if it returns `false`, **this runs on every
single `CampaignSnapshotV11` ever constructed** (the check is in the type's own
constructor via `RequireShape`, `src/Cna.Core/Campaigns/CampaignSnapshotV11.cs:68`). The
catalog order for one side's block (`Cna1979LandSequence.AddPlayerPhase`,
`src/Cna.Core/Rules/Cna1979LandSequence.cs:81-114`) is: Reserve Designation, Movement,
Breakdown, then six Combat steps (**Position Determination**, Barrage, Retreat Before
Assault, Force Assignment, Anti-Armor, Close Assault), then Reserve Release, Truck
Convoy, Rail, Repair (×2), Patrol. The break point is the *first* of those six Combat
steps. Concretely: **a `CampaignSnapshotV11` whose position is "first side, Barrage" or
"first side, Reserve Release" would itself fail validation and the constructor would
throw** — not just Stage 2, not just the second side.

### What actually happens instead, for the first side's own cycle, today

Once the first Combat selection is made, the campaign stops minting new
`CampaignSnapshotV11` objects altogether. `CampaignNativeCombatDecisionV12` is the one
and only V11→native bridge point, used exactly once per campaign: its state type
(`CampaignNativeCombatDecisionStateV12`, `src/Cna.Core/Campaigns/CampaignNativeCombatDecisionV12.cs:110`)
carries a `PriorSnapshot: CampaignSnapshotV11` field. From there, the entire rest of the
first side's Combat round (Barrage → Retreat → Force Assignment → Anti-Armor → Result →
Settlement → Custody → Successor) runs as a chain of native events
(`CampaignNativeCombatBarrageV14` through `CampaignNativeCombatSuccessorV26`), and
`CampaignSnapshotV11` is retained only as a **frozen facts bag** passed along inside
`CampaignCombatSelectionFrame.PriorSnapshot` — used for `CampaignId`/identity checks and
as a fallback `RandomState`/`GameTurn` source, never reconstructed and never required to
represent the campaign's actual current position again. Confirmed directly:
`CampaignNativeCombatSuccessorV26.Project` (`src/Cna.Core/Campaigns/CampaignNativeCombatSuccessorV26.cs:52-100`)
reads `selected.PriorSnapshot.CampaignId` and
`selected.PriorSnapshot.CurrentPosition.SequenceContext.GameTurn` only — then does all
real position work through `Cna1979LandSequenceV4.CreateTurn`/`GetNext` directly, exactly
the same pattern the second-side `V43`-`V47` classes use.

**`CampaignNativeReserveReleaseStateV27.Initialize`
(`src/Cna.Core/Campaigns/CampaignNativeReserveReleaseV27.cs:148-174`) is the direct
precedent for "a native event bridging from the previous phase into the next."** It takes
a `CampaignNativeCombatSuccessorV26` predecessor plus its `Projection`, asserts
`projection.NextPosition.ActorRole != LandActorRole.FirstActingSide` and
`projection.NextPosition.SegmentId != LandSegmentIds.ReserveRelease`, and — this is new,
not previously flagged — reads `projection.PriorSnapshot.OperationStageOrders.Single().FirstSide`,
i.e. it **also** hardcodes "there is exactly one `CampaignOperationStageOrder` in
existence," the same single-stage assumption found elsewhere, just via `.Single()`
instead of an explicit `== 1` check. This is a second, independent occurrence of the same
class of hardcoding Section 2 of the original scoping doc already named — noted here for
completeness, not re-litigated.

### The actually-relevant precedent: `CampaignNativePostCombatPassV42` → `CampaignNativeSecondReserveV43`

This is the real template for "one side-block ends, the next side-block begins" — already
built, already proven, already side-generic (per `#518`). `CampaignNativePostCombatPassV42.Create`
(`src/Cna.Core/Campaigns/CampaignNativePostCombatPassV42.cs:16-43`) computes its `next`
field via the fully generic `Cna1979LandSequenceV4.GetNext(position)` — **this already
correctly computes the Stage 2 Initiative Declaration position as the successor to
Stage 1's second side's Patrol phase, today, with no change needed.** The data is already
right; nothing consumes it yet, because:

1. `Cna1979LandSequence.AddOperationPrelude` (`src/Cna.Core/Rules/Cna1979LandSequence.cs:68-79`)
   places six preamble positions — Initiative Declaration, Weather Determination,
   Organization, Naval Convoy Arrival, Fleet Assignment, Fleet Repair — **between** one
   stage's last side-block and the next stage's first Reserve Designation. A
   `PostCombatPassV42`'s `NextPositionId` pointing at Stage 2 points at
   `operation-2.initiative-declaration`, not directly at a Reserve position — so
   `CampaignNativeSecondReserveV43.Create`'s own successor-matching cannot simply be fed
   that value; something has to resolve those six positions first.
2. Nothing in the native chain does that today. The only place these six mechanics are
   implemented at all is `CampaignV11Preamble.Create`/`Apply`
   (`src/Cna.Core/Campaigns/CampaignV11Preamble.cs:17-147`), which is unconditionally
   V11-rooted and **also** gates every case on a literal `prior.StateVersion == N` (`N` =
   1 through 9, `CampaignV11Preamble.cs:30,33,35,41,45,49,53,57,61`) — i.e. it assumes
   these are *the 2nd through 10th events ever accepted in the whole campaign's history*,
   not just "Stage 1." This dispatch style is structurally non-reusable for a native,
   catalog-position-driven chain regardless of the `OperationStage` question — the native
   chain's own convention (confirmed throughout `V41`-`V49`) is to validate against the
   actual `Cna1979LandSequenceV4` catalog position, never a literal StateVersion number.

**So "the Stage 2 entry bridge" and "Stage 2's preamble" are the same body of work, not
two separate problems**, and Initiative Declaration's own role in that chain is more
central than its name suggests: `InitiativeOrderDeclared`'s handling in
`CampaignV11Preamble.Apply` (`:122`) is what *produces* the `CampaignOperationStageOrder`
(`FirstSide`/`SecondSide`) that `#518`'s `CampaignOperationStageOrder.ActingRole` depends
on. Nothing downstream (Reserve, Movement, Breakdown, Combat selection) can run for Stage
2 until something produces that stage's order natively.

## A second gap this investigation surfaced: where does the native chain's "orders" list live?

Every one of the `#518`-generalized classes takes `orders: IReadOnlyList<CampaignOperationStageOrder>`
as an **explicit parameter** supplied by the caller (`CampaignExercises.cs`), and every
real caller today passes `session.CurrentSnapshot.OperationStageOrders` — the frozen
genesis snapshot's list, which (since `CampaignSnapshotV11` is never touched, per the
ruling) will forever contain only Stage 1's single order. A Stage 2 order, once produced
by whatever Initiative Declaration mechanism is chosen below, has nowhere established to
live: `ExerciseSession` has no field today analogous to `CurrentSecondReserve` etc. for
"the native chain's own accumulated `CampaignOperationStageOrder` list." This needs an
answer regardless of which option below is chosen — flagged as part of the design, not
decided here.

## Options for the native preamble chain

All options assume the same outer shape, since it's already proven: a chain of native
event types, each validated against `Cna1979LandSequenceV4.CreateTurn`/`GetNext`
positions (never a `StateVersion` literal), carrying `CampaignWorldSnapshotV6`/World-hash
state forward the way `V41`-`V46` already do, beginning from a new predecessor-switch arm
recognizing `CampaignNativePostCombatPassV42` (additive, no change to `V42` itself), and
ending in a new predecessor-switch arm added to `CampaignNativeSecondReserveV43.Create`
(additive, no change to the other existing arms). They differ only in how many discrete
native event *types* carry the six preamble mechanics.

### Option A — six chained events, one per mechanic (maximum precedent fidelity)

One new native type per mechanic (Initiative Declaration, Weather, Organization, Naval
Convoy Arrival, Fleet Assignment, Fleet Repair), each the same shape as `V41`/`V42`
(`ContractVersion`, `CampaignId`, `PriorStateVersion`, `PriorEventHash`, `PositionId`,
`NextPositionId`, `WorldHash`, plus mechanic-specific fields), chained predecessor →
successor exactly like the existing chain.

- **Matches established convention exactly.** Every other native event in this codebase
  (`V12`-`V49`) is one mechanic per type; the original V11 preamble is also one event per
  mechanic (`InitiativeOrderDeclared`, `WeatherDetermined`,
  `NoObligationOrganizationResolved`, etc., each a distinct `CampaignEvent` subtype).
  Weather's dice draw (`Cna1979Weather.Resolve(turn, randomState)`,
  `CampaignV11Preamble.cs:34`) is fully reusable as-is — it only needs `turn` and a
  `RandomState`, both already threaded natively elsewhere.
- **Cost:** 6 new types/files, each needing its own focused tests — this project's own
  "if a task exceeds five files... split it before editing" convention
  (`docs/design/combat-cycle-implementation-plan.md:103-104`) means this is naturally
  3 implementation-sized slices (e.g. Initiative+Weather, Organization+Convoy,
  Fleet×2), not one.
- **DET/REL risk: purely additive**, same shape as `#518` and the rest of the native
  chain — new files, new switch arms, zero changes to `V42`'s or `V43`'s other existing
  behavior. The highest-fidelity, lowest-architectural-risk option; the cost is file
  count, not correctness risk.

### Option A′ — three events: Initiative, Weather, one bundled "no-obligation stage entry"

Same shape as A, but Organization + Naval Convoy Arrival + Fleet Assignment + Fleet
Repair collapse into one native event, since — for the admitted synthetic/lab content
today — all four are already trivial no-obligation resolutions in the V11 original
(`NoObligationOrganizationResolved`, `NoObligationNavalConvoyArrivalResolved`,
`NoObligationFleetAssignmentResolved`, `NoObligationFleetRepairResolved`, each with no
real decision inside it today — confirmed by `CampaignV11Preamble.cs:45-61`, none of
which branch on anything but checking for scheduled arrivals
(`HasDueOrOverdueArrival`, `:148-153`)). Initiative Declaration (a real owner decision,
act-first/act-last) and Weather (a real dice roll with gameplay consequences, per the
roadmap's Sprint 3 notes on Weather's "immediate fuel/water, well, and grounded-aircraft
consequences") stay their own events.

- **Cost:** 2 implementation-sized slices instead of 3; fewer files overall.
- **Real tradeoff, not free:** the roadmap already flags that "positive
  Organization/convoy/fleet cases remain explicit later-scenario work"
  (`docs/roadmap/pre-alpha-roadmap.md`, Sprint 3/Sprint 7 status notes). If a later
  scenario needs one of those four mechanics to carry a real decision (not just a
  no-obligation resolution), a bundled event would need to be split apart again —
  a contract-version churn cost deferred, not avoided. Option A's four separate types
  could each version independently later without touching the others.
- **DET/REL risk:** same additive shape as A.

### Option B — one combined native "stage entry" event for all six mechanics

A single new native type resolves Initiative Declaration + Weather + Organization +
Convoy + Fleet×2 atomically in one event.

- **Cost:** lowest file count of the three (1 new type).
- **Departs furthest from established convention.** This project has never modeled a
  multi-mechanic decision as one event anywhere in `V11`-`V49` or the original preamble;
  doing so here would be a new pattern, not a reuse of one. It also merges a *real* owner
  decision (Initiative Declaration) with no-obligation auto-resolutions into one
  committed unit, which sits awkwardly next to this project's own `UX-001` ("automation
  removes bookkeeping without removing an original decision point") — Initiative
  Declaration is specifically not auto-resolvable the way `CampaignActionAudience.System`
  steps are (confirmed by the frozen scope doc's item 0: initiative holder's choice has
  no System-audience path and was never meant to), so folding it into an otherwise
  largely-automatic bundle risks looking like it's being silently resolved even though the
  real implementation would still require the owner's actual choice as an input.
- **DET/REL risk:** still additive in the sense that nothing existing changes, but a
  single larger event is a single larger unit to get right the first time, and the
  "explicit no-obligation, zero events" distinction the original V11 preamble carefully
  preserves per-mechanic would need to be re-derived inside one type instead of reusing
  four already-separate ones.

### What was *not* proposed, and why

A "skip preamble, auto-resolve everything into the bridge event" option was considered
and rejected outright rather than listed as a real option: Initiative Declaration is a
genuine owner decision (the initiative holder's act-first/act-last choice), not a
no-obligation step, and nothing in this codebase's `CampaignActionAudience.System`
auto-drain mechanism (`#508`, per the engine-release session summary) was ever meant to
cover it. Silently resolving it as part of a generic bridge would recreate exactly the
kind of "looks automatic but actually deletes a real decision point" problem `UX-001`
exists to prevent.

## Recommendation

**Option A′.** Reasoning: it keeps the two mechanics with real decision/randomness
content (Initiative, Weather) at full precedent fidelity and independent versionability,
while accepting a bounded, named, explicitly-flagged future cost (re-splitting the
no-obligation bundle if a later scenario needs a real Organization/Convoy/Fleet decision)
in exchange for one fewer implementation slice today. Option A is the safer default if
the owner weighs "never have to re-split a bundled event later" above "one fewer slice
now" — both are legitimate; this is a real judgment call, not a correctness question.
Option B is not recommended: it departs from an otherwise completely consistent codebase
convention for a marginal file-count saving, and risks the `UX-001` optics problem named
above.

Whichever option is chosen, the "where does the native `orders` list live" sub-question
(a new `ExerciseSession` field vs. a self-contained field on the terminal preamble event)
is independent and should be decided alongside it, not deferred — every one of `#518`'s
generalized classes needs an answer before any of them can actually run for Stage 2.

## What this investigation deliberately does not do

- No code was written. No new types, no new switch arms, no new `ExerciseSession` fields.
- Does not pick Option A vs. A′ vs. B, or resolve the `orders`-list sub-question — both
  are the owner's call.
- Does not re-examine `CampaignSnapshotV11`/`CampaignSnapshotV11Validator` — untouched,
  per the standing ruling.

## Owner ruling (2026-11-22)

**Option A — six separate native events, one per mechanic.** Reasoning, for the record:
Organization/Naval Convoy Arrival/Fleet Assignment/Fleet Repair are already flagged by the
roadmap as likely needing real decisions for later scenarios; splitting a bundled event
apart once it has live accepted history is a permanent new-version cost under `EVT-001`
("authoritative changes occur only through versioned commands and emitted events" —
changing what one committed event type means, after real history exists under it, is the
kind of migration this project's own compatibility discipline treats as expensive and
separately gated). Three extra implementation slices now is cheaper than that
forever-liability.

**`orders`-list sub-question:** the owner's lean — self-contained on the terminal event of
the six-event chain (Fleet Repair's own event carries the resolved
`CampaignOperationStageOrder` fields directly), consistent with the native chain's general
pattern of passing forward what's needed through the event chain itself rather than a
mutable session-wide field, avoiding the exact single-mutable-slot-field problem class
this whole effort has been fixing — explicitly asked to be verified against the
established pattern before committing, not assumed.

**Verified: this is the obvious fit, not a fork.** Every `#518`-generalized class already
takes `orders: IReadOnlyList<CampaignOperationStageOrder>` as an *externally supplied*
parameter (e.g. `CampaignNativeSecondReserveV43.Create`,
`CampaignNativeSecondReserveV43.cs:17-19`) — never self-discovered from an
`ExerciseSession` field. Today's only real source for that parameter is
`session.CurrentSnapshot.OperationStageOrders` (the frozen genesis snapshot). The
additive, zero-signature-change way to extend this for Stage 2+ is for the caller
(whatever Submit method drives the first Stage-2 Reserve action) to build the `orders`
list it passes in by concatenating `session.CurrentSnapshot.OperationStageOrders`
(Stage 1's order, unchanged) with the order resolved by the native Initiative Declaration
event, read directly off whichever native predecessor object is in hand — exactly the
same way every other native-chain value (`WorldHash`, `Side`, `PositionId`) is read
directly off a concrete predecessor type via its own fields, never a dedicated
`ExerciseSession` mutable-state field. No new `ExerciseSession` field is needed for the
order *value* itself. (`ExerciseSession` does still need the usual per-mechanic
`CurrentXxx`/`XxxPredecessor` field pair for each of the six new event types, the same as
every other native chain step — that is the established pattern for chain-position
tracking, not a special case.)

**A second, related design decision made during implementation, recorded here since it
surfaced the same class of question and the owner's reasoning above applies directly to
it: threading `RandomStreamState` through the new chain.** Only Weather Determination
consumes randomness among the six mechanics (confirmed: `CampaignV11Preamble.Create`'s
`ActFirstAction`/`ActLastAction`/Organization/Naval-Convoy/Fleet cases never touch
`prior.RandomState`; only `ResolveWeatherAction` does, via `Cna1979Weather.Resolve(turn,
prior.RandomState)`, `CampaignV11Preamble.cs:34`). `CampaignNativePostCombatPassV42`
carries no `RandomState` field at all (Rail/Repair/Patrol never draw randomness), so the
"current cursor" by the time Weather needs one cannot be read off the immediate
predecessor. The one existing precedent for this exact problem,
`ExerciseSession.SecondAssaultFrame` (`ExerciseSession.cs:889-897`), resolves it by
walking `CurrentHistory` for the most recent event of a specific RNG-advancing type and
falling back to the frozen genesis snapshot's `RandomState` if none exists yet — not a
generic "any RNG-advancing type" resolver (there isn't one), but a narrow, explicitly
commented, one-more-`OfType<T>()`-case extension of exactly the same technique used here.
Decision: extend that same technique with one more case for the new Weather event type,
documented with the same kind of explanatory comment `SecondAssaultFrame` already carries
— not a new pattern, and backed by the same safety net that already protects it:
`CampaignExercises.ReconstructOne`'s independent replay/hash-chain re-verification would
surface a wrong cursor as a reconstruction failure, not a silent determinism bug. Treated
as an obvious extension of established precedent rather than a fork requiring a separate
ruling; flagged here for the record per this document's own citation discipline.

Implementation proceeds per this ruling in
[`2026-11-22-mts-task-002-slice-1-implementation.md`](2026-11-22-mts-task-002-slice-1-implementation.md).
