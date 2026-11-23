# Stage 3 scoping — verification against actual code (2026-11-23)

**Status:** research/scoping only. No code changed. Written while MTS-TASK-005/006
(Stage 2 Movement/Breakdown/Combat) is still landing, to answer ahead of time whether
Stage 3 genuinely mirrors Stage 2 or hides new surface area — per the owner's request to
get ahead of it while Stage 2 finishes. Verified directly against current `main`
(`ba3a58fc`, includes #518-#522), not against the original multi-turn scoping doc's
pre-#518 estimate alone.

## Verdict: yes, Stage 3 genuinely mirrors Stage 2 — at both the catalog and the
## newly-built native-chain level. One real content wrinkle, no mechanic-level surprise.

## 1. Catalog level — confirmed fully generic, zero stage-specific branching

`Cna1979LandSequence.CreateTurn` (`src/Cna.Core/Rules/Cna1979LandSequence.cs:24-48`):

```csharp
for (var operationStage = 1; operationStage <= 3; operationStage++)
{
    AddOperationPrelude(positions, gameTurn, operationStage);
    AddPlayerPhase(positions, gameTurn, operationStage, "first-player", LandActorRole.FirstActingSide);
    AddPlayerPhase(positions, gameTurn, operationStage, "second-player", LandActorRole.SecondActingSide);
}
```

`AddOperationPrelude` (`:68-79`) unconditionally emits the same six phases regardless of
which `operationStage` is passed: `initiative-declaration`, `weather-determination`,
`organization`, `naval-convoy-arrival`, `commonwealth-fleet.assignment`,
`commonwealth-fleet.repair`. `AddPlayerPhase` (`:81-...`) unconditionally emits
`reserve-designation`, `movement-and-combat.movement`,
`movement-and-combat.breakdown-determination`, and the Combat step sequence, for either
side, for any stage. Neither method takes a stage-conditional branch anywhere in its
body. This confirms, independently and more directly than the original multi-turn
scoping doc (which inferred this from the loop bound alone), that Stage 3's structure is
**not a separate catalog shape** — it's the same six-phase preamble + Movement/
Breakdown/Combat cycle, parameterized by `operationStage`, already proven at the catalog
level for Stage 2 by virtue of being the same code path.

## 2. Native-chain level — confirmed stage-agnostic by construction, not just by luck

The six native preamble classes built this session for the Stage 1→2 bridge
(`CampaignNativeInitiativeDeclarationV49` through `CampaignNativeNoObligationFleetRepairV54`)
resolve their `OperationStage` from the real catalog position, not from a literal.
`CampaignNativeInitiativeDeclarationV49.Create`
(`src/Cna.Core/Campaigns/CampaignNativeInitiativeDeclarationV49.cs:37-66`):

```csharp
var position = Cna1979LandSequenceV4.CreateTurn(gameTurn).Single(value => value.PositionId == positionId);
if (position.ActorRole != LandActorRole.InitiativeHolder || position.PhaseId != LandPhaseIds.InitiativeDeclaration)
    throw new InvalidOperationException("This phase does not admit an Initiative Declaration.");
var next = Cna1979LandSequenceV4.GetNext(position);
...
return new(CurrentContractVersion, campaignId, version, ...,
    position.GameTurn, position.OperationStage, holder, firstSide, secondSide);
```

The class's own code comment is explicit that this was a deliberate correction, not an
accident: it contrasts itself with `CampaignV11Preamble.Create`'s old pattern (gated on
a literal `prior.StateVersion == 4`, "not reusable once StateVersion is in the
thousands") and states it "validates against the real `Cna1979LandSequenceV4` catalog
position, the same way every other native event in this chain already does." Grepped
all six new classes (`V49`-`V54`) for `OperationStage ==` or a literal `== 2`: **zero
hits in any of them.** `CampaignOperationStageOrder.ActingRole(LandSide)`
(`src/Cna.Core/Campaigns/CampaignOperationStageOrder.cs:42-45`) resolves role from the
order's own `FirstSide`/`SecondSide` fields, which already carry `OperationStage` as
data on the record itself — also fully stage-agnostic.

**This means the Stage 2→3 preamble bridge should need zero new native types** — the
same six classes should work unchanged for a Stage 2→3 transition, *provided* the
predecessor-chain type matching resolves correctly. `CampaignNativeInitiativeDeclarationV49.Create`'s
predecessor `switch` currently matches only `CampaignNativePostCombatPassV42` (the class
that closes out a side's Rail/Repair/Patrol block). `CampaignNativePostCombatPassV42.Create`
takes `LandSide side` directly and is already side/stage-generic (confirmed in the prior
multi-turn scoping doc, re-confirmed here by the same absence of stage literals) — so
once Stage 2's own Movement/Breakdown/Combat/Post-Combat-Pass sequence is fully built
(MTS-TASK-005/006, in progress), the Stage 2→3 bridge should fire through the identical
predecessor match that already fires for Stage 1→2, with no code change to the bridge
classes themselves. This is the one load-bearing "provided" in this doc — it's not yet
empirically proven because Stage 2's own Post-Combat-Pass step hasn't landed yet, but
nothing found here suggests it would behave differently.

## 3. What is NOT free: the decision-point classes still carry the per-occurrence tax

This is the one real caveat, and it's freshly confirmed, not theoretical. PR #523
(Stage 2's Breakdown slice, landing now) found that `CampaignNativeSecondBreakdownV46.Create`
reads four `ExerciseSession` fields by name (`CurrentSecondMovementCompletion`,
`SecondMovementCompletionPredecessor`, `SecondMovePredecessor`, `CurrentSecondMoveWorld`)
— hardcoded to which **session slot** holds the predecessor, not to `OperationStage`
itself. The fix was a sibling method (`CreateForStageEntry`), not a generic one. This is
the same cost model the overhaul direction doc's Section 0 describes (`ExerciseSession`'s
pile of `Current*`/`*Predecessor` fields, one per mechanic-occurrence) — and it is **not
reduced by anything built so far**, because the `PendingDecision`/handler-registry
refactor that doc proposes hasn't been built yet (deliberately sequenced to wait until
Stage 2/3 land, per that doc's own Section 6).

**Concrete implication for sizing:** Stage 3's Movement/Breakdown/Combat slice
(equivalent to Stage 2's MTS-TASK-005/006) will cost roughly the same number of
PRs/wiring-triples as Stage 2 did — one new `ExerciseSession` field/predecessor/`WithXxx`
triple, one new `CampaignExercises` dispatch block, one timing-manifest row, per
mechanic, per occurrence — **not less**, until the overhaul's `PendingDecision` union
actually lands and collapses this. The preamble slice (Stage 3's equivalent of
MTS-TASK-002/003) should be cheaper than Stage 2's was, because the six native classes
and their wiring pattern are now a known, copy-pasteable template rather than a
from-scratch design (Stage 2's preamble took a real design-fork resolution first — the
six-vs-three-events entry-bridge decision; Stage 3 has no equivalent open question).

## 4. One genuine Stage-3-specific wrinkle: Polish Arrival content

`CampaignSourceSetupBridge.QueryTurn`'s content-specific branch (`src/Cna.Core/Exercises/CampaignSourceSetupBridge.cs:120`)
gates `CampaignSourcePolishArrival.ValidDestinations` on reaching Naval Convoy Arrival at
Operation Stage 3 specifically — this is Graziani-scenario content (not a generic
mechanic), and it is real, existing code, not a gap. It's also **Game-Turn-1-gated**
(confirmed in the prior multi-turn doc, not re-litigated here), so it fires correctly for
Turn 1's Stage 3 without any change needed. This is the one place Stage 3 is not simply
"Stage 2 again" — it's an additional scenario-content branch layered on top of the same
generic mechanic shape, already built and already working. No new work implied here;
flagging only so "Stage 3 = Stage 2" isn't read as "there is nothing stage-3-specific
anywhere in the codebase."

## 5. Sizing estimate, revised from the original scoping doc's speculative one

- **Stage 3 preamble (MTS-TASK-008 equivalent): smaller than Stage 2's was.** No new
  design fork expected (the entry-bridge pattern is proven and stage-agnostic by
  construction); mostly verification that the predecessor-chain match fires correctly
  once Stage 2's closing Post-Combat-Pass exists, plus the same mechanical wiring tax
  (one `ExerciseSession`/`CampaignExercises`/timing-manifest triple) already paid six
  times for Stage 2.
- **Stage 3 Movement/Breakdown/Combat (MTS-TASK-009 equivalent): same cost as Stage 2's
  MTS-TASK-005/006**, not less — the per-occurrence `ExerciseSession`-field tax is
  unchanged until the overhaul's `PendingDecision` refactor lands. Budget the same number
  of slices/PRs Stage 2 needed.
- **No new research risk identified** — unlike the Stage 1→2 entry-bridge (which needed
  its own design-options doc before implementation could start), Stage 2→3 has no
  equivalent open question found in this pass. This is genuinely "mostly mechanical
  repetition of a proven pattern," which the original scoping doc predicted correctly at
  the catalog level and which this pass now confirms at the native-chain level too.
