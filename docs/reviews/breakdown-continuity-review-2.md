# Breakdown Continuity Independent Review 2

**Review instance:** 2 of 3

**Date:** 2026-08-29

**Reviewed target:** remediated dirty working-tree delta on `codex/breakdown-continuity` from
`7f676942d467c91ea0357836d28943074cacbfa3`

**Verdict:** Ready

## Findings and reconciliation

No actionable P0-P3 findings remain. No finding required acceptance, dispute, or deferral, and no
heavy pivot was required. Review instance 3 is unnecessary unless later base reconciliation
materially changes reviewed behavior, contracts, or canonical identities.

The reviewer independently verified every review-1 correction:

- Truck `2L` provenance uses Land Rules 21.11-21.14 and Common Charts Table 21.38 without falsely
  attaching errata 21.12, which corrects the Italian M13/40 row.
- Current documentation consistently reports ruleset 7, Content schema 4/canonical format v3,
  world 4, snapshot 9, and creation event 8.
- The raw `0-3` band remains part of the source table but is not check-eligible and cannot be stored
  as remembered checked-band state.
- Full total/profile/weather/prior-history checked-band coherence remains an explicit prerequisite
  for the later task that first writes a non-null checked band.

## Plan review

The reviewer found `MOV-REQ-013` through `MOV-REQ-015` and `MOV-AC-014` through `MOV-AC-017`
satisfied. Rules authority owns exact rational BP values, normalized source-bearing definitions,
the nine bands, weather/BAR behavior, and the complete 36-coordinate sequential-d6 domain without
introducing a resolver or RNG path. Content owns the strict optional cohort contract and the two
supported synthetic Truck cohorts. Campaign authority owns creation-seeded, replay-complete zero
continuity and rejects forged pre-Movement BP/check/loss state.

The delivery plan, clean-cut compatibility policy, identity migration, verification gates, and
bounded deferrals were judged coherent. Movement BP accrual, outward observation, Breakdown
results/RNG/losses, and tuning remain intentionally outside Task 004B.

## Independent verification

- Release solution build passed with 0 warnings and 0 errors.
- Native Microsoft.Testing.Platform solution tests passed 852/852.
- Core tests passed 529/529; ExerciseRunner tests passed 313/313.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore` passed.
- `git diff --check` passed.
- Changed Exercise, Maneuver, and golden JSON parsed successfully.
- Canonical Breakdown hash and all derived identities matched their checked fixtures; stale
  superseded identities were absent.
- Source claims were visually checked against the Land Rules, Common Charts, and September 1979
  errata PDFs.
- No Breakdown action, result/loss resolver, RNG use, or BP mutation path was found.

The first sandboxed Release build stalled on local IPC and was canceled; the identical
non-sandboxed build completed successfully. The reviewer did not invoke `just check` as one exact
command, but independently repeated its format/build/test constituents. Map A terrain rows were not
revalidated visually in this pass and remain covered by the source-locked packet, golden tests, and
prior review evidence.

## Closeout

Task 004B is ready. The next implementation boundary is `MOV-TASK-005`, which adds the approved
side-safe own mobility/operational/BP-risk facts and apparent opposing presence without exposing
hidden bindings or opposing exact state.
