# Breakdown Runner migration and closeout

**Status:** Task 007 implementation and focused evidence in place. Full gate passed before the final adjacent-enemy fixture addition; focused checks and two clean runs in progress. Final independent review awaits authorization: implementation review budget is exhausted at 3 of 3.

The frozen [migration inventory](../specs/breakdown-fixture-migration.v1.json) remains unchanged. Fourteen named successor files preserve the earlier checkpoint/controller purposes within the certified Truck/battalion profile. The original fourteen files remain byte-identical historical artifacts. Thirteen Reaction children retain bounded episode, close and continuation behavior; positive local and remote ZOC remain deferred.

Ten new content packs and eleven setups cover independent battalion policy matrices, separated reactors, recurrence, HQ and noncombat neighbors, last-CP interruption, and Truck cost/one-point studies. Exact capability validation now requires the cohort capability iff the pack contains a cohort; zero-cohort battalion packs were already allowed by the profile but previously impossible to admit. Existing two catalog fixture identities are unchanged.

Two additive controller policies choose only published action semantics: reserve-all/lowest-cost-once and reserve-all/repeated-highest-cost with an explicit stop after each route. Positive CP costs bound repeated movement; no hidden BP, cohort, lot or random-state data enters controller selection. Manifest schema versions remain unchanged.

The [Truck study](../../scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json) uses root seed 8 and real maneuver/ordinal identities. Truck children terminate at `land.position.operation-1.first-player.movement-and-combat.combat.position-determination`; they do not execute Combat. A sixth Truck child moves beside an actual adjacent enemy combat unit without opening Reaction. Its fifth child proves deferred last-CP phasing resolution after all reactors finish.

| Truck child | BP at stops | Dice coordinates | Loss counts | Observable evidence |
| --- | --- | --- | --- | --- |
| no-roll | 1/2 | none | 0 | No RNG advance or checked memory |
| eligible-zero-loss | 26, 32, 58 | 21, 66, 56 | 0, 6, 2 | Zero-loss roll advances cursor and memory; later higher bands roll |
| repeat-positive-loss | 26, 32, 58 | 32, 63, 43 | 0, 3, 1 | Survivors leave the three-point west lot stationary |
| exhaustion | 26 | 66 | 1 | Zero working points remove every Move with 12 of 20 CP still unused |

`BreakdownFixtureMigrationTests` executes real standalone, serial and paired identities; it checks historical hashes, all named successors, exact Reserve and Reaction counts, recurrence CP, immediate reactor resolution, active unavailable/timeout continuation and last-CP ordering. `BreakdownTruckStudyTests` checks the table above, conservation, first-side Combat entry and public legal actions immediately after loss. `BreakdownBundleTamperingTests` checks eight forgeries (dice, cursor, label, fraction, BP, lot location, memory and continuation) after refreshing every relevant event, step, proof and artifact hash. Fresh re-adjudication still matches actions and final snapshot but rejects the altered events; strict bundle readback rejects each forgery.

## Verification

Pending final gate and clean-run evidence; see subsequent update. Earlier RED tests failed for missing catalogs, missing successor manifests and missing controller policies. Integration checks caught and fixed test-root lookup, conditional cohort capability, a separate Maneuver decoder, a zero-loss baseline seed and an inexact Combat checkpoint name. These failures were not treated as passing evidence.

## Scope and remaining gate

The [historical Reaction study](simulator-reaction-trajectories.md) remains evidence for Rules 8 at its recorded commit, not a current Rules 9 fingerprint claim. Broader positive ZOC, positive cohort Reaction loss, motorized infantry, grouped losses, general placement, capture/towing/repair and later-stage BP reset remain outside this public capability.

Task 007 final independent checkpoint is pending. [Implementation review 3](../reviews/brk-progress-review-3.md) reviewed through Task 005; it did not review Task 006 public activation or Task 007 migration. No new reviewer has been started and no Ready verdict is claimed for those changes.
