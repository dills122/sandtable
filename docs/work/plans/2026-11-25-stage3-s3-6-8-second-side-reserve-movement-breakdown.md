# Stage 3 S3-6..S3-8: second-acting-side Reserve Designation, Movement + Movement Completion, Breakdown

Date: 2026-11-25. Follows S3-5 (#635). Template: Stage 2 second side (#531, #537, #539) and Stage 3 first side (#600).
Consolidated into one PR; the three slices share every touched file and the boundaries are mechanical (S3-6 Reserve, S3-7 Movement, S3-8 Breakdown).

## What changed

- `ExerciseSession`: `CurrentThirdStageSecondReserve`(+`World`), `CurrentThirdStageSecondMove`(+`World`),
  `CurrentThirdStageSecondMovementCompletion`, `CurrentThirdStageSecondBreakdown` with their `*Predecessor` fields and
  `WithThirdStageSecond*` methods. Reserve is guarded on the first side's `CurrentThirdStageAssault` having at least one
  Post Combat Pass; Move/Completion/Breakdown use the stronger first-side guards (Reserve `Completed`, Completion/Breakdown not
  already present). `ComputePending` checks the four new cuts first.
- `PendingDecision`: `ThirdStageSecond{Reserve,Move,MovementCompletion,Breakdown}Pending`.
- `CampaignExercises`: Slots (`ThirdStageSecondMovementSourceSlot`, `...MovementCutSlot`, `...ReserveSourceSlot`) and
  `QueryThirdStageSecond*Source`, `SubmitThirdStageSecondReserve`, `CompleteThirdStageSecondMovement`,
  `QueryThirdStageSecondMovementCut`, `SubmitThirdStageSecondMovement`, `CompleteThirdStageSecondBreakdown`;
  `QueryCheckpoint` and `ReconstructOne` cases; `InvalidNativeHistory` heads. `ThirdStageOrders` is reused unchanged.
- `CampaignNativeSecondBreakdownV46`: `ThirdStageSecondSlot` / `CreateForThirdStageSecondSide`.
- Shadow derivation: one `PendingOccurrenceTable` row `(3, SecondActingSide) -> "ThirdStageSecond"` with `CombatArms = false`;
  `PendingFromCursor` now also gates `CombatSelection` on `CombatArms` so positions after the second-side Breakdown derive to null
  (no `ThirdStageSecondCombatSelectionPending` subtype exists until S3-9).
- `CampaignOpenSeatCatalog`: four `Unwired(Stage3)` rows.
- Existing events and rules-artifact hashes untouched (EVT-001); no clock, Guid, RNG (DET-001/002). None of the new steps draws dice;
  a test pins `LatestCursor` unchanged across the second-side chain.

## Stale-field checklist (section 5 of the Stage 3 decision audit)

| Item | State after S3-6..8 |
|---|---|
| First-side Combat entry points (`QueryContactOwnerCombatCut`, `SubmitContactOwnerCombatSelection`) keyed off `CurrentThirdStageBreakdown` | Now fail closed explicitly once `CurrentThirdStageSecondReserve` exists (second-side Combat is S3-9). Tested at every second-side cut. |
| `Classify`, round-binding, selection timeout, `SubmitPostCombatPass`, `SubmitTruckConvoyPass`, `QueryNativeReserveReleaseState` | Still closed for Stage 3 (unchanged guards), re-tested at every new cut. |
| `HasAdvancedCombatSelection` | Second side falls into the first-side branch (true) so it also fails closed; S3-9 must add a `CurrentThirdStageSecondCombatDecision` branch ahead of it. |
| `InvalidNativeHistory` head | Extended with the four second-side heads. First-side Stage 1 head hashing for Stage Entry failures still not touched. |
| `Classify` reads Stage 1 fields | Unchanged (`Stage.None` for Stage 3). |

## Left for S3-9

`ThirdStageSecondCombatSelectionPending`/`Assault`/`NoAttack`, flipping `CombatArms` for the second-side row, the Combat Tier 2 branches,
`HasAdvancedCombatSelection`, the Combat world/position helper for the second side, and the Defect B byte-shape mismatch noted in the S3-5 design note.

## What the new tests prove

`CampaignStageThreeSecondSideTests` (sessions from the shared `pregame-no-attack` scenario): Reconstruct verifies Reserve, Movement Completion and Breakdown
(and the retained bytes equal the last event); `QueryCheckpoint` names a Stage 3 position; a one-byte flip of each record's own bytes gives
`InvalidHistory` (no exception), as does a flipped second-side Reserve seen from the Breakdown head; wrong-side, System, duplicate and
out-of-order submissions are rejected; first-side and Stage 2 slots are untouched; Combat/tail entry points fail closed at every cut; `Pending` precedence
over stale first-side and Stage 2 fields. `PendingDecisionReconstructCoverageTests`, the oracle, shadow, spike and catalog tests cover each new subtype.

They do NOT prove: `ThirdStageSecondMovePending` (no own element has a legal movement offer in the only drivable setup; allow-listed and listed `NotDriven`,
same as S3-2, so there is no end-to-end Move fixture and its `ReconstructOne` arm is a line-for-line mirror), any second-side Combat step, or Host/Runner flows.
Unvisited catalog positions pin 31 -> 28.
