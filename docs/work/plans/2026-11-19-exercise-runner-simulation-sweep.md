# Exercise Runner simulation sweep: 2026-11-19

**Status:** record of a completed sweep. No design decision is needed. One spec drift was fixed in
the same PR, and one coverage gap was filed as [#448](https://github.com/dills122/sandtable/issues/448).

**Base:** `main` @ `f3b13786`. Release build of `src/Cna.ExerciseRunner`.

This continues the simulation-testing sweeps behind #430, #433, #434, #435 and #437. The previous
dispatch left no durable record, so this file records what was run as well as what was found.

## What was run

1. **Every checked manifest, run twice.** That covers all 12 files in `scenarios/exercises/` and
   all 17 in `scenarios/maneuvers/` (`exercise run` / `maneuver run`, separate artifact roots per
   repeat).
2. **Policy soak.** 572 generated Exercise manifests: each of the 11 `rules-lab.breakdown.*`
   setups admitted in `scenarios/` × all 26 controller-policy tokens (same policy for all three
   audiences) × `rootSeed` {0, 7}. The terminal boundary was set to an unreachable ID and
   `maximumSteps` to 400, so each run goes until Core or the controller can't continue. The whole
   soak ran twice.
3. **Seed soak.** 990 more manifests: 11 setups × {`first-by-action-id`,
   `reaction-all-by-action-id`, `act-first-reserve-all-move-each-once-then-complete`} × seeds
   100..129.
4. **CI `exercise` shard equivalent:** `python3 .github/scripts/local_tests.py exercise`
   (`Cna.ExerciseRunner.Tests`, Release). 473/473 passed.

The generated manifests lived under `artifacts/` (gitignored) and were deleted afterwards. To
regenerate them, copy any checked `*.breakdown.v1.json` child's
`setupId`/`setupHash`/`contentPackId`/`contentHash`/`scenarioId`/`rulesetHash`. Then set
`terminalBoundary` to an unknown stable ID and vary `controllers` and `rootSeed`.

## Results

### Checked manifests

| Set | Outcome |
| --- | --- |
| 6 `*.breakdown.v1.json` Exercises | Succeeded (exit 0) |
| 8 `*.breakdown.v1.json` Maneuvers + `rules-lab.breakdown-truck.serial.v1.json` | Succeeded (exit 0) |
| 6 `*.v2.json` Exercises, 6 `*.v2.json` Maneuvers, 2 non-Breakdown `*.paired.v1.json` | `manifest-invalid` (exit 2), **by design**: frozen historical fixtures (`FrozenHistoricalFixtureIsRejectedBeforeCurrentExecution`, `CheckedBaselineFixturesAreExactBuildPolicyTwinsOfExploratoryFixtures`) |

### Determinism (DET-001 / DET-002)

- Exercise bundles from repeated runs matched byte for byte, all files included.
- Maneuver/paired reports had equal `reportFingerprint`s. The only byte differences were in
  `diagnostics.elapsedMicroseconds` and `diagnostics.entries[].observedBundlePath`, which
  `EXR-022` excludes from identity.
- All 572 policy-soak bundles matched byte for byte across the two repeats.
- `rootSeed` does matter: seeds 0 and 7 give different weather dice and different canonical
  events. So the identical repeats are not just a seed being ignored.

### Crashes and settlement outcomes

- **No crashes.** No `unexpected-failure`, `illegal-action`, `reconstruction-mismatch` or
  `readjudication-mismatch` came up in 1,562 probe runs.
- 1,546 probe runs ended at
  `land.position.operation-1.first-player.movement-and-combat.combat.position-determination`, with
  `breakdownFlow` idle and **no legal action for any audience**. The roadmap already documents
  this frontier: "checked Runner evidence remain[s] Rules9 through first-side Combat **entry**".
  Combat Exercise/Runner work is Tasks 022–024, after Task021. Filed as
  [#448](https://github.com/dills122/sandtable/issues/448) so it stays visible: a Runner sweep
  currently says **nothing** about Combat round kinds (Selection/Rba/Force/Retreat/Custody/
  Reserve Release/Cycle). It therefore cannot reach #446
  (`Accepted Combat choice cannot settle normal World`). #446 was not hit in this sweep for that
  reason.
- 16 probe runs ended in `invariant-failed` / `multiple-active-audiences`. All were
  `act-first-reserve-none-move-each-once*` policies on Reaction setups. A Reaction window
  legitimately offers both System close actions and reacting-player actions, and only the
  `reaction-*` policies can handle that. The spec already documents this fail-closed result
  (`docs/specs/exercise-harness-v1.md`, Movement section: Reserve-none children "fail closed when
  Reaction exposes simultaneous reacting-player and System audiences"). Expected, nothing filed.

### Content gaps

Nothing in `scenarios/` asks for more than Core supports. Every current manifest's terminal
boundary is at or before Combat Position Determination, and all are reachable.

### Fixed in this PR

`docs/specs/exercise-harness-v1.md` § Commands told readers to run `*.v2.json` and
`rules-lab.reserve-policy.paired.v1.json`. Admission rejects every one of those with exit 2. The
command blocks now point at the `*.breakdown.v1.json` counterparts. A note explains the
historical-fixture rule and the name mapping, and says the quoted step counts describe the
historical v2 runs.

## Not done

- Combat-reaching simulation: blocked on Tasks 020–024 ([#448](https://github.com/dills122/sandtable/issues/448)).
- No Core or ExerciseRunner code change was warranted.
