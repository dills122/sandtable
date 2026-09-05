# Breakdown Runner migration and closeout

**Status:** Task 007 implementation, full gate and two matching clean runs verified. [Final review 4](../reviews/brk-final-review-4.md) is **Not ready** for acceptance closeout: Task 006 still needs BRK-AC-009 full current-version audience transcript/progress privacy evidence. Review limit is exhausted at 4 of 4 after explicit user authorization.

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

Candidate source commit: `59679493866d7f23ea0a69964e93bbfb67bd2841`, following implementation `df75b0f`. Final evidence/docs are a later documentation-only commit.

- Clean checkout restore and build passed with zero warnings/errors.
- `dotnet test --solution Sandtable.slnx --no-build`: **1,655 passed**, zero failures/skips.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`: **66 passed**.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`, numeric audit, freeze audit and diff check passed. These execute every constituent of `just check`; builds retained unique binlogs.
- Both public CLI runs completed all **15 manifests / 47 child campaigns**. Every bundle records `dirty=false` and the candidate commit. Three child manifests use baseline build mode; the remaining 44 use exploratory mode, without relabeling.
- All **188** reconstruction/re-adjudication proofs across both runs are verified. Nine canonical files per child match byte-for-byte (**423 comparisons**); all nine deterministic aggregate reports and fingerprints match. Build paths, run IDs and timings are excluded from this deterministic comparison.

The [machine-readable results](breakdown-runner-migration-results.json) retain successor file hashes, all thirteen public-evidence mappings, exact Reaction counts, report fingerprints and comparison summary. Canonical file-hash matrix digest:

```text
sha256:938616c9aa4b9d92e295a4d0b5dfcb1c8e7f8bdf1c9b59f8966bd356c71c50da
```

Retained local roots: `/tmp/brk007-final-run-a`, `/tmp/brk007-final-run-b`; full per-child file-hash matrix: `/tmp/brk007-final-comparison.json`. The detached candidate checkout is `/tmp/sandtable-brk007-clean`.

Earlier RED tests failed for missing catalogs, missing successors and missing policy cases. Integration checks caught test-root lookup, conditional cohort capability, a separate Maneuver decoder, a zero-loss baseline seed and an inexact Combat checkpoint name. The first clean attempt at `df75b0f` completed fourteen migrated manifests but failed the new study because configuration hashing lacked the new policy names. That attempt is excluded from final evidence; regression-tested correction `5967949` precedes both successful repeated runs.

To reproduce from a clean checkout of the candidate, restore/build first, then use fresh artifact roots:

```sh
python3 docs/research/verify-breakdown-runner-runs.py run "$PWD" /tmp/brk-repeat-a
python3 docs/research/verify-breakdown-runner-runs.py run "$PWD" /tmp/brk-repeat-b
python3 docs/research/verify-breakdown-runner-runs.py compare "$PWD" /tmp/brk-repeat-a /tmp/brk-repeat-b 59679493866d7f23ea0a69964e93bbfb67bd2841 /tmp/brk-repeat-comparison.json
```

## Scope and remaining gate

The [historical Reaction study](simulator-reaction-trajectories.md) remains evidence for Rules 8 at its recorded commit, not a current Rules 9 fingerprint claim. Broader positive ZOC, positive cohort Reaction loss, motorized infantry, grouped losses, general placement, capture/towing/repair and later-stage BP reset remain outside this public capability.

[Implementation review 3](../reviews/brk-progress-review-3.md) reviewed through Task 005. [Final review 4](../reviews/brk-final-review-4.md) reviewed Tasks 006–007, independently reran build, tests, format and audits, and checked retained clean-run artifacts. Its **Not ready** verdict leaves AC-009 full current-version transcript/progress privacy coverage open; no runtime disclosure defect was demonstrated. Normative status wording was corrected during review retention. Next work belongs to Core observation/privacy tests under Task 006 and gates Task 007 acceptance. No further review instance starts without renewed explicit authorization.
