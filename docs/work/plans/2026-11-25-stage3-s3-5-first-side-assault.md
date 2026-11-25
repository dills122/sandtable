# Stage 3 S3-5: first-acting-side Combat Assault / No-Attack

Date: 2026-11-25. Follows S3-4 (#613) and the shadow-derivation fix (#629). Template: Stage 2 first-side Assault (#529).

## What changed

- `ExerciseSession`: new `CurrentThirdStageAssault` / `ThirdStageAssaultPredecessor` / `WithThirdStageAssaultEvent`,
  `CurrentThirdStageNoAttackCompletion` / `ThirdStageNoAttackPredecessor` / `WithThirdStageNoAttack`.
  `WithThirdStageCombatSelection` now spins up the Stage 3 assault runtime (zero events), exactly as
  `WithStageEntryCombatSelection` does. `CurrentRepeatAssault` / `WithRepeatAssaultEvent` resolve Stage 3's own runtime
  first; once the Stage 3 Decision exists they never fall through to the never-reset Stage 2 runtimes.
- `PendingDecision`: `ThirdStageAssaultPending`, `ThirdStageNoAttackPending` (the latter is unobservable, same reason as
  the Stage 1/2 NoAttack subtypes: the no-attack completion installs a non-empty Assault runtime, which `Pending` checks first).
- `CampaignExercises`: `CompleteThirdStageCombatNoAttack`; `QueryCheckpoint` and `ReconstructOne` cases for both subtypes;
  each Tier 2 method gained one Stage 3 branch checked first (Reserve Release query/choose/complete/timeout, Cycle Control
  query/submit/timeout, Truck Convoy source/submit, Post Combat Pass source/submit). Tier 1 methods needed no change.
- Shadow derivation: `CombatArms` for Stage 3 flipped; `ThirdStageNoAttackPending` added to `PendingExceptionTable.Unobservable`.
- `CampaignOpenSeatCatalog`: both new subtypes `Unwired(Stage3)`.
- Existing events and rules-artifact hashes are untouched (EVT-001); no new clock, Guid, or RNG (DET-001/002). Selection and
  no-attack do not draw; dice cursors still come from `ExerciseSession.LatestCursor`.

## Stale-field checklist (section 5 of the Stage 3 decision audit)

| Item | State after S3-5 |
|---|---|
| `Classify` reads Stage 1 fields | Still returns `Stage.None` for any Stage 3 session (`IsStageThreeCombatUnwired`). Not driven by the Runner/Host until the Stage 3 Host kinds exist. Left for a later slice. |
| `QueryNativeCombatSelectionRoundBinding` / `TimeoutNativeCombatSelection` key off `CurrentSecondBreakdown` | Fail closed in Stage 3 (unchanged guard, tested). Stage 3 selection timeout is left for a later slice. |
| `SubmitPostCombatPass` lacked the `CurrentInitiativeDeclaration is null` guard | Added to the Stage 1 `CurrentSecondAssault` branch; Stage 3 branch is checked first and the old Stage 3 unwired guard now admits Stage 3 only once its assault runtime exists. Tested. |
| `InvalidNativeHistory` hashes a Stage 1 head for Stage Entry failures | Not touched by this slice; left for a later slice. |
| `QueryNativeReserveReleaseState`, `QueryPostCombatPassSource`, `QueryTruckConvoyPassSource` could fall through to stale Stage 2 sources | Once the Stage 3 runtime exists they answer from it or throw, never from Stage 2. Tested. |

## Observed, deliberately not fixed (later slices)

- Runner exit 11, Defect B (`2026-11-25-l8-second-side-reserve-release-exit-11.md`): `CompleteThirdStageCombatNoAttack` copies
  Stage 2's evidence shape (`SerializeStateCanonical` as the checkpoint bytes) while `Reconstruct` of the resulting
  `ThirdStageAssaultPending` returns plain event bytes. A Runner finalization of a Stage 3 no-attack step would hit the same
  byte mismatch. Not driven by the Runner today (`Classify` returns None for Stage 3), so it does not fire.
- Stage-ladder gap near `CampaignCombatExerciseActions.cs:219-229` (`SecondPlayer` frontier after the second-side
  no-attack): unchanged, belongs to the second-side slices.
- After the fourth Post Combat Pass `CurrentRepeatAssault` is null for a finish-without-attack chain; Repeat cycles in Stage 3
  are not exercised (the derivation's Repeat arm is UNVERIFIED for non-legacy occurrences, as before).

## What the new tests prove

- `CampaignStageThreeFirstSideTests`: real-session fixture drives Decision, No-Attack, Reserve Release, Cycle Control Finish,
  Truck Convoy and four Post Combat Passes. Reconstruct verifies every observable Assault cut and its bytes; QueryCheckpoint
  names a Stage 3 position at every cut; a one-byte flip of the Assault step's own retained event, and of the Stage 3 Decision
  seen from the terminal head, yields `InvalidHistory` (no exception). Stale Stage 2 runtimes stay untouched and never answer.
- They do not prove: Reconstruct of `ThirdStageNoAttackPending` (unobservable, allow-listed), a Repeat cycle, a real attack
  (only the finish-without-attack path is driven), or any Host/Runner flow.
- Oracle (`PendingEquivalenceOracleTests`) now covers the Stage 3 first-side Assault/Truck/Post-Combat cuts against the
  cursor derivation; unvisited catalog positions pin 38 -> 31.
