# Post-merge simulator check-in

Status: complete, 2026-09-06 UTC. Scope: Rules 9, merged PR #87, scripted public Exercise/Maneuver CLI.

## Question and decision

Do the current fixtures still reproduce the accepted pre-merge results, do bounded new seeds replay
exactly, and does the same-host CLI timing comparison reveal a regression worth investigating?

All bounded checks passed. Continue the next approved Combat research/design gate; this study
identified no simulator fix to carry forward. This check-in changes research tooling and evidence only.

## Sources and method

- Current: `5c16fdb61f8854453d5daefc91c6aa9d912b929e`, merged PR #87.
- Comparison: `59679493866d7f23ea0a69964e93bbfb67bd2841`, accepted clean Runner candidate.
- Both Runner checkouts restored and built with .NET SDK 10.0.400 on macOS arm64, Debug configuration,
  zero warnings/errors. Every campaign must record the expected commit and `dirty=false`.
- Fixed suite: all 14 migrated successors plus the supplemental six-child Truck study;
  15 manifests, 47 campaigns per pass, two passes. Compare against retained candidate pass as well.
- Seed sweep: root seeds 0–15, six Truck-study children and 13 bounded Reaction children per seed,
  each run twice. Only root seed changes; Maneuver IDs, child order, policies and inputs stay fixed.
- Timing: six serial pairs of the six-child Truck study, alternating source order, after one warmup
  per source. Fresh CLI processes; includes startup, artifact I/O and proof/readback work.
  No builds or campaign experiments run concurrently with timing.
- Fail closed on unsuccessful children, wrong boundaries/source/input, unverified replay proofs,
  missing or mismatched indexed artifacts, canonical-byte differences, or deterministic-report drift.
  Compare nine canonical files per child; timing/build diagnostics are intentionally excluded.

[Machine summary](simulator-post-merge-checkin.json), [seed dataset](simulator-post-merge-checkin-seeds.csv),
and [reproduction helper](verify-simulator-checkin.py) retain the small reviewable result.
Raw trusted-authority bundles remain local at the paths recorded in the summary; they are not committed.

## Results

The fixed suite matched across both current passes and the retained candidate pass: 423 canonical
files per comparison, plus nine deterministic reports. One pass contains 659 accepted steps,
19 Reaction moves, 49 Breakdown stop resolutions and all four weather kinds.

| Six-child CLI timing | Comparison candidate | Merged main |
| --- | ---: | ---: |
| Wall median | 13,771.683 ms | 13,365.862 ms |
| Wall range | 12,001.207–16,261.507 ms | 12,576.781–16,344.604 ms |
| Maneuver diagnostic median | 13,619.980 ms | 13,223.585 ms |

Median paired wall change was −2.94%. Ranges overlap substantially; this sample shows no consistent
post-merge slowdown and does not establish a speedup. All seven timing pairs, including warmups,
matched canonical bytes and deterministic reports.

All 608 seed-sweep executions succeeded, with 2,736 canonical-file comparisons and all 32 repeated
Maneuver reports matching. The following totals count each seed/case once (304 campaigns), excluding
its repeat:

| Observed metric | Truck study, 96 campaigns | Reaction study, 208 campaigns |
| --- | ---: | ---: |
| Accepted steps | 1,757 | 3,888 |
| Reaction moves | 32 | 272 |
| Breakdown stop resolutions | 216 | 464 |
| Rolled cohort checks | 152 | 0 |
| Explicit no-roll cohort checks | 16 | 0 |
| Rolled checks with zero loss | 75 | 0 |
| Lost vehicle points | 172 | 0 |
| Final cohorts with zero working points | 8 | 0 |

Weather counts were normal 161, hot 75, rainstorm 45 and sandstorm 23. All explicit no-roll checks
had raw BP not above three. Truck-study losses ranged from 6 to 16 points per seed; seed 8 also
matched all 54 canonical files and the deterministic report of the checked Truck fixture.
Terminals were exactly the requested boundaries: 224 Breakdown-determination entries and 80
first-side Combat entries. The Reaction study contains the bounded zero-cohort cases described in
Runner closeout; zero losses do not demonstrate positive-cohort Reaction-loss coverage.

The counted study comprises **786 campaign executions and 1,572 verified proof records**:
94 fixed-suite executions, 608 sweep executions and 84 timing executions. Final re-audit checked
3,159 repeat canonical-file comparisons, 423 prior-candidate comparisons, 378 timing comparisons,
all expected source/input identities, all 32 CSV rows and the saved timing samples against raw evidence.

## Limits and next work

This is a deterministic execution/replay check and descriptive seed sample, not balance validation,
exhaustive random-outcome coverage, or evaluation of model-backed commanders. Fixture names such as
`truck.eligible-zero-loss` describe the checked seed-8 case; arbitrary seeds can produce other outcomes.
Explicit cohort `noRollChecks` exclude empty-cohort stop resolutions that contain no check record.

The comparison commits have identical `src` and `scenarios` trees. The central package change is
Google.Protobuf 3.36.0 to 3.36.1. Timing therefore checks post-merge behavior and host variation;
it does not isolate the performance cost of Movement, Reaction or Breakdown enhancements.
Earlier [baseline timing](simulator-baseline-2.md) stopped at earlier boundaries and is not comparable.
Six timing pairs provide exploratory diagnostics, not a statistical performance guarantee.

Coverage remains bounded by [Runner closeout](breakdown-runner-closeout.md): first-side Combat entry,
without Combat execution, positive ZOC, positive-cohort Reaction loss, motorized infantry or later-stage
BP reset. Continue the next approved Combat research/design gate after this checkpoint; do not treat
this report as authorization to broaden the current Breakdown capability.

## Reproduce

Use two clean worktrees at the source commits above. Build each Runner before invoking the helper:

```sh
dotnet restore src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj '-bl:artifacts/binlogs/checkin-restore-{}.binlog'
dotnet build src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj --no-restore '-bl:artifacts/binlogs/checkin-build-{}.binlog'
python3 /path/to/verify-simulator-checkin.py /path/to/current /path/to/comparison /tmp/new-checkin
```

Run each build command in its respective worktree. Python 3.9+ and the repository-pinned .NET SDK are
required. Output directory must not exist. The helper places seed inputs under Git-ignored
`artifacts/simulator-checkin-inputs`; public CLI admission requires repository-relative canonical JSON.
Optional `--prior-run` compares the fixed suite against retained candidate artifacts.
Optional `--checked-runs` re-audits a completed current `checked-a`/`checked-b` pair instead of rerunning it.
The published evidence used both options. Current artifact paths appear in the machine summary;
the retained comparison pass was `/tmp/brk007-final-run-a`.

Two harness admission mistakes were corrected before the seed study: an external manifest path and
trailing JSON whitespace. Both failed before campaign execution. The initial 84-campaign timing series
and a six-campaign seed-zero smoke check are excluded from the published study totals. Fixed runs from the
second attempt were retained and re-audited; the final timing series was rerun with durable samples.

## Helper verification

Six negative controls (dirty source, unverified proof, failed result, corrupted indexed artifact,
missing artifact index entries and wrong requested seed) were rejected; restored six-child evidence
passed. Python syntax/help checks, exact input identity checks, complete artifact-index checks, final
artifact/summary/CSV/timing re-audit and `git diff --check` passed. CSV uses LF endings so Git preserves
the recorded dataset hash; the original CRLF export is retained locally. This documentation/helper
change does not rerun or claim a new full-solution .NET test gate.

## Combat contract branch smoke check

A bounded follow-up at clean commit `a15a0a929039c291e36aaf09aff1a3a408579558` repeated
`scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json` twice. This is additional evidence;
it does not replace or increase the earlier study's786-execution total. The feature diff against
merged main `d59446e` changes no `src`, `scenarios` or `Directory.Packages.props` files.

Runner restore/build passed with zero build warnings/errors. Both unique binlogs remain under
`artifacts/binlogs/pr-smoke-{restore,build}-*.binlog`. The existing reproduction helper checked
clean source identity, exact inputs and requested terminals, both proof statuses, all indexed
artifact lengths/hashes, nine canonical files per child and deterministic aggregate reports.

- 12 campaign executions succeeded;24 proof records verified.
- 54 canonical files matched byte-for-byte between passes; deterministic reports matched.
- One pass:106 accepted steps,2 Reaction moves,12 Breakdown stop resolutions,8 rolled checks,
  1 explicit no-roll check, and15 lost vehicle points. Terminals:1 Breakdown entry and5 Combat
  entries. Weather:3 hot,2 normal,1 rainstorm.
- Canonical hash-ledger digest:
  `sha256:4a1e1e90e4090c5a3835d7790ccb7a061e9acc95e0f16c17a9745a5fef5e7550`.

Raw evidence, CLI logs, hash ledgers and `summary.json` are retained locally under
`/tmp/sandtable-combat-pr-smoke-a15a0a9`; they are trusted-authority artifacts, not player output.
Documentation-only planning edits follow the tested commit. No runtime changes, full-solution test
rerun, cross-commit performance comparison, new seed sweep, Combat execution, or balance/AI
validation is claimed. Next Combat-specific simulation work is TASK022–024 after public Core
activation; see the [current gates](../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates).

To repeat the bounded check, build the Runner in a clean worktree as above, then run this from its
root with a fresh output directory. Python imports the existing helper; no new verifier is required.

```python
import importlib.util
from pathlib import Path

repo = Path.cwd().resolve()
spec = importlib.util.spec_from_file_location(
    "checkin", repo / "docs/research/verify-simulator-checkin.py")
checkin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checkin)
commit = checkin.identity(repo)
output = Path("/tmp/sandtable-combat-smoke-repeat")
output.mkdir(exist_ok=False)
manifest = repo / checkin.STUDIES[0]
first = checkin.execute(repo, manifest, output / "pass-a", commit)
second = checkin.execute(repo, manifest, output / "pass-b", commit)
assert checkin.compare(first, second) == 54
print("PASS: 12 campaigns, 24 verified proofs, 54 matching canonical files")
```
