# MTS-A: role-as-data refactor for Stage 2 — implementation record (2026-11-22)

**Status:** first implementation slice of the multi-turn/multi-stage effort scoped in
[`2026-11-22-multi-turn-multi-stage-scoping.md`](2026-11-22-multi-turn-multi-stage-scoping.md).
Not scoping — this is a real code change, built and test-verified. Follows the owner's
ruling recorded in that document's Addendum: Option 2 (generalize the already-proven
native `(CampaignWorldSnapshotV6, LandSequencePosition)` chain) via the hybrid (prove
the "role as data" refactor on Stage 2 only, with a defined fallback to hand-building if
it doesn't generalize cleanly). `CampaignSnapshotV11` and `CampaignSnapshotV11Validator`
were not modified, per that ruling.

## What this slice does

Generalizes the 7 call sites enumerated in the scoping doc's Addendum from hardcoding
`LandActorRole.SecondActingSide` to accepting whichever acting role (`FirstActingSide` or
`SecondActingSide`) actually corresponds to the given side for that `(GameTurn,
OperationStage)`. This does **not** change any serialized wire format: every one of these
record types already carries `LandSide Side` as data (not `LandActorRole`); `ActorRole` is
re-derived at `Create`/replay time from `Cna1979LandSequenceV4.CreateTurn(...)`'s position
catalog lookup, which was already fully generic over `GameTurn`/`OperationStage` before
this change (`Cna1979LandSequence.CreateTurn`'s `for (var operationStage = 1; operationStage
<= 3; operationStage++)` loop, confirmed in the scoping doc's Section 1). The only thing
that was hardcoded was the *validation* — "this must specifically be the second acting
side" — not the data model. No contract version was bumped; no event-type string changed;
no existing test needed its expectation changed (confirmed by searching for any existing
test asserting a `FirstActingSide` rejection from these methods — none exists, see
"Verification" below).

### New shared helper

`CampaignOperationStageOrder.ActingRole(LandSide side)` (`src/Cna.Core/Campaigns/CampaignOperationStageOrder.cs`)
— returns the `LandActorRole?` (`FirstActingSide`/`SecondActingSide`/`null`) that `side`
holds for that stage order, resolved from the order's own `FirstSide`/`SecondSide` fields.
Added here rather than duplicated at each of the 4 call sites that have an `order` in
scope; the remaining 3 call sites don't have an `order` available and instead use the
already-proven `is not (LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)`
shape that `CampaignNativePostCombatPassV42`/`CampaignNativeCycleControlV28` already use
(per the scoping doc's enumeration of already-side-generic classes).

### The 7 sites, before -> after

| # | Site | Before | After |
| - | --- | --- | --- |
| 1 | `CampaignContactOwnerMovementCut.ProjectSecondMovement` (`src/Cna.Core/Visibility/CampaignContactOwnerMovementCut.cs`) | `position.ActorRole != LandActorRole.SecondActingSide` | `position.ActorRole is not (LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)` |
| 2 | `CampaignContactOwnerMovementCut.ProjectSecondCombatOwner` (same file) | same shape | same generalization |
| 3 | `CampaignReactionWindow` constructor (`src/Cna.Core/Campaigns/CampaignReactionWindow.cs`) | `reactingPosition.SuspendedMovementPosition.ActorRole != LandActorRole.SecondActingSide` | `... is not (LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)` |
| 4 | `CampaignNativeSecondReserveV43.Create` (`src/Cna.Core/Campaigns/CampaignNativeSecondReserveV43.cs`) | `position.ActorRole != LandActorRole.SecondActingSide \|\| side != order.SecondSide` (entry), `next.ActorRole != LandActorRole.SecondActingSide` (Movement successor) | both replaced by `order.ActingRole(side)`, computed once as `actingRole` |
| 5 | `CampaignNativeSecondMoveV44.Source` (`src/Cna.Core/Campaigns/CampaignNativeSecondMoveV44.cs`) | `order.SecondSide != side \|\| catalog.ActorRole != LandActorRole.SecondActingSide` | `order.ActingRole(side)` |
| 6 | `CampaignNativeSecondMovementCompletedV45.Create` (`src/Cna.Core/Campaigns/CampaignNativeSecondMovementCompletedV45.cs`) | `next.ActorRole != LandActorRole.SecondActingSide` | `next.ActorRole != position.ActorRole` (reuses the role `Source` already validated — a side-block's acting role is constant across its own Movement -> Breakdown transition, confirmed from `Cna1979LandSequence.AddPlayerPhase` passing one `actorRole` through the whole side-block) |
| 7 | `CampaignNativeSecondBreakdownV46.Create` (`src/Cna.Core/Campaigns/CampaignNativeSecondBreakdownV46.cs`) | `order.SecondSide != completed.Side \|\| movement.ActorRole != LandActorRole.SecondActingSide` (Movement check), `next.ActorRole != LandActorRole.SecondActingSide` (Combat successor) | both replaced by `order.ActingRole(completed.Side)`, computed once as `actingRole` |

`CampaignNativeSecondCombatDecisionV47`/`CampaignNativeSecondCombatDecisionStateV47` needed
no direct edit — re-reading them confirmed (as the scoping doc's Addendum already found)
that they delegate their position/role validity entirely to `ProjectSecondCombatOwner`
(site #2) and `QuerySecondCombatWorldAndPosition` (`CampaignExercises.cs`, which derives its
position from `breakdown.NextPositionId` — already correct generically once site #7 is
fixed, since it never independently re-checks `ActorRole`).

## Build and test verification

- `dotnet restore Sandtable.slnx` then `dotnet build Sandtable.slnx --no-restore`: **succeeded,
  0 errors** (1 pre-existing, unrelated `ASPIRE010` warning on `Cna.AppHost`).
- Targeted test runs (per this project's own `just test-class`/`local_tests.py` convention,
  not a full-suite run):
  - `python3 .github/scripts/local_tests.py core --class "*Second*"`: **17/17 passed.**
  - `python3 .github/scripts/local_tests.py core --class "*Reaction*"`: **161/161 passed**
    (covers `CampaignReactionWindow`, site #3 above, broadly).
  - `python3 .github/scripts/local_tests.py host --method "*SecondSide*"`: **29/29 passed**
    (`Cna.OrleansHost.Tests`, including
    `CampaignDurableAuthorityJournalTests.SecondCombatSelection.cs`'s
    `SecondSideVoluntaryFinishIsLegalPersistedColdReplayableAndRetried`,
    `SecondSideCombatSelectionRejectsAnActionNotOfferedAtTheCut`, and
    `SecondSideRealAssaultSelectionIsLegalPersistedColdReplayableAndRetried` — real HTTP,
    cold-replay-checked, second-side Combat selection coverage).
  - `python3 .github/scripts/local_tests.py core --class "*Breakdown*"`: **801/801 passed**
    (confirmatory only — these methods are never on the first-side Breakdown path this
    change didn't touch, run for extra confidence given the size of this file).
- No existing test was found that asserts a `FirstActingSide` position/rejection from any
  of the 7 methods (`search_code` for `FirstActingSide` in
  `tests/Cna.Core.Tests/Observations/CampaignContactOwnerMovementCutTests.cs` and a broader
  `ActorRole` sweep across `tests/Cna.Core.Tests` returned no such assertion), so this change
  does not flip any existing test's expected outcome — it only widens what these methods
  accept, and nothing exercised the now-newly-accepted case before (there is no Stage 2
  content/fixture yet to exercise it with).

## What this slice does *not* do, and why

**Stage 2's preamble (Initiative Declaration, Weather, Organization, Naval Convoy Arrival,
Fleet Assignment/Repair) is not built in this slice**, despite being named alongside this
work. Re-checking during implementation: the 7 classes above are all *Movement/Breakdown/
Combat-cycle* decision points (the second-acting-side continuation from `#394`), not
preamble mechanics. Core's only existing preamble implementation,
`CampaignV11Preamble.Create`/`Apply` (`src/Cna.Core/Campaigns/CampaignV11Preamble.cs`), is
built entirely on `CampaignSnapshotV11` — it hardcodes stage `1` explicitly
(`candidate.OperationStage == 1` at `CampaignV11Preamble.cs:41`, and a literal `1` written
into the constructed `CampaignOperationStageOrder` at `:122`) and has never had a native,
non-V11 analog built for it, because until now preamble only ever happened once per
campaign (Stage 1 of Turn 1) — there is no "second preamble" the way there's a "second
side" for Movement/Combat, so there is no existing `#394`-style prior art to generalize
here the way there was for the 7 classes above.

Building Stage 2's preamble under the owner's Option-2 ruling (not touching
`CampaignSnapshotV11`) means it would need an **entirely new native-chain preamble
pattern**, invented from scratch, not a generalization of something already proven twice.
That is a materially different, higher-uncertainty kind of task than the one the owner
ruled on, and per this project's own standing discipline ("verify everything... surface
forks rather than resolve them unilaterally"), it is flagged back explicitly rather than
guessed at:

**Open question (new, found during this implementation pass): should Stage 2's preamble
be built as a new native-chain pattern (mirroring how `#394` built the Movement/Combat
native chain, but for Initiative/Weather/Organization/Convoy/Fleet instead), or does the
owner want to revisit the Option 1/Option 2 fork specifically for preamble, since preamble
has no existing native prior art the way Movement/Combat did?**

## Correction (2026-11-22, same day): MTS-TASK-005/006 is also gated on this, not independent

The statement above this heading originally claimed starting `MTS-TASK-005`/`006` was
**not** blocked by the preamble question. Re-investigated before starting that work, per
the instruction to stop and report on any new real fork rather than press forward on an
unverified assumption. **That claim was wrong, and is corrected here rather than silently
fixed**, per this project's own citation/correction discipline.

What's actually true, re-verified by reading the entry path each of the 7 generalized
classes' predecessors require:

- `CampaignNativeSecondReserveV43.Create`'s `predecessor switch`
  (`CampaignNativeSecondReserveV43.cs:26-33`) accepts exactly two predecessor shapes:
  `CampaignNativePostCombatPassV42` (a side's own Rail/Repair/Patrol pass, which can only
  exist *after* that side's own full Reserve-through-TruckConvoy cycle has already run)
  or an incomplete `CampaignNativeSecondReserveV43` itself. **Neither shape can represent
  "Stage 2 preamble just finished, first acting side is now ready to begin Reserve
  Designation"** — that predecessor type does not exist, because it is the preamble
  question's own open architecture decision (what does Stage 2's native-chain entry point
  even look like) under a different name.
- This is not specific to the first-generalized class: `CampaignNativeTruckConvoyPassV41.Create`
  (`CampaignNativeTruckConvoyPassV41.cs:16-33`) requires a completed
  `CampaignNativeReserveReleaseStateV27`/`CampaignNativeCycleControlV28` pair with
  `Choice == Finish` — i.e. the *entire* Reserve/Movement/Breakdown/Combat/ReserveRelease
  cycle for that side must already have run through the native chain before Truck Convoy
  is reachable, which is circular for "build the first acting side's cycle" if there is no
  entry point into that cycle in the first place.
- The second acting side of Stage 2 is gated transitively on the first: it needs the first
  side's own `CampaignNativePostCombatPassV42` as its predecessor (per the table above),
  so it cannot be live/reachable before the first side's cycle is.

**Re-investigated separately and confirmed good news, so the #518 refactor is not
wasted:** the *deeper* Combat sub-step machinery — Barrage through Settlement/Custody/
Successor (`CampaignNativeCombatBarrageV14` through `CampaignNativeCombatSuccessorV26`),
driven by `CampaignNativeRepeatAssaultRuntime` — is **already side/stage-generic by
design**, not hardcoded. `CampaignNativeRepeatAssaultRuntime.ReplaySecond`
(`CampaignNativeRepeatAssaultRuntime.cs:132-156`) operates on a `CampaignCombatSelectionFrame`
(a facts bag: `TurnFacts`/`World`/`RandomState`/`Decision`), not a hardcoded role, and its
`isSecondSide` constructor flag (confirmed by reading `Apply`/the class's own comments,
e.g. `:93-94`, `:9-11`) controls *which continuation path* (native Truck
Convoy/Post-Combat Pass vs. the V11-rooted repeat path), not literally "is this the second
side" — so this method is actually already the right substrate for Stage 2's *first*
acting side too, once something can reach it. `CampaignNativeReserveReleaseV27`/
`CampaignNativeCycleControlV28`/`CampaignNativeTruckConvoyPassV41`/
`CampaignNativePostCombatPassV42` were separately confirmed already side-generic in the
original addendum. **So the remaining gap for MTS-TASK-005/006 is concentrated entirely in
the entry bridge, not in the combat mechanics themselves** — which is useful risk
information for scoping, even though it does not unblock the task today.

This is the same open architecture question as the preamble one, not a second,
independent question: both boil down to "what event/type represents 'Stage 2 has begun,
side X may now act' on the native chain, since `CampaignSnapshotV11` cannot represent
it." Answering it for preamble answers it for `MTS-TASK-005`/`006` too (or vice versa) —
they should be decided together, not separately. No bridge design is proposed here; that
would be guessing at exactly the architecture the owner asked not to be guessed at.

## Next steps

- Await the owner's ruling on the preamble/entry-bridge open question before starting
  live/wired work on `MTS-TASK-002`-`006`. All of Stage 2's mechanics (preamble and
  Movement/Breakdown/Combat alike) are gated on the same unresolved question: what native
  event represents entering Stage 2 at all.
- The 7-class role-as-data refactor in this PR stands as real, valuable, independently
  tested progress regardless of how the bridge question resolves — it removes a hardcoded
  assumption that would have blocked Stage 2 either way, and the re-investigation above
  confirms the deeper combat machinery needs no further generalization once the bridge
  exists.
- Not attempted in this pass: a synthetic/isolated Core test that drives the native chain
  through a hand-constructed Stage 2 `CampaignOperationStageOrder` and a fabricated
  predecessor (bypassing the live bridge question entirely, the way this project's own
  history used "synthetic earlier Movement" for early Combat evidence, e.g. Task019D1).
  That could produce real "Implemented," not-yet-"Activated-and-witnessed" evidence (per
  this project's own four-state status model in the roadmap) without resolving the bridge
  — flagged as a real option for a future slice, not started here since it was not what
  was asked for this round and risks presupposing a predecessor shape the owner hasn't
  ruled on.
