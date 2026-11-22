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
has no existing native prior art the way Movement/Combat did?** This blocks `MTS-TASK-002`/
`003`/`004` (Stage 2 preamble through Reserve Designation) as scoped in the original
document until it's answered. It does not block continuing the Movement/Breakdown/Combat
generalization (`MTS-TASK-005`/`006`, which this slice's refactor directly feeds) if the
owner wants that to proceed independently.

## Next steps

- Await the owner's ruling on the preamble open question above before starting
  `MTS-TASK-002`-`004`.
- `MTS-TASK-005`/`006` (Stage 2 Movement/Breakdown/Combat, both sides) can now build on
  the 7 generalized classes above rather than hand-building side-specific siblings, per
  the hybrid's intended payoff — not yet started in this slice.
