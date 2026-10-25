# Force entry deadline handoff

Base `80d2881d`, branch `codex/rc-overnight-plan-20261023`.
Ruflo task `task-1792880135655-ctru56`, namespace `sandtable-rc-20261023`.

## Delivered scope

Schema 9 persists Force deadline atomically with accepted RBA decline, using the
trusted decline gate timestamp and pinned Force budget. Owner queries read the
round; they cannot start or renew its deadline. Background cancellation can advance
combat even when neither owner opens a Force view. Restart and retries preserve
original timing. Missing round evidence rejects rather than healing silently.

Legacy schemas 5/6/7/8 journals remain readable under historical verification and read-only.
Schema 8 RBA checks remain active. No implicit migration or invented historical
entry timestamp. Core cancellation and schema of its events remain unchanged.

## Validation

RED before fix: two no-query/late-first-query cases failed, exit 2, 6.004 seconds.
Logs/XML retained in `artifacts/rc-force-entry/red` and `red.log`.
Initial two cases passed after fix (7.079 seconds). Expanded repeated-assault
checks exposed a cancelled-round adapter binding defect; original failure log
retained. A narrow terminal-state guard is included, with affected Core/Runner
checks added. Final focused checks: 10/10 passed in 60.301 seconds. Focused Core: 17/17 passed in
8.250 seconds; boundary: 81/81 passed in 6.536 seconds. Release solution build passed
(exit 0, zero warnings/errors, 13.83 seconds); format verification passed (exit 0).
Whole Host exposed two old assertions expecting an exception for late seals after
repeated cancellation. The terminal-state guard now returns a clean rejection;
assertions updated while retaining unchanged-state checks. Full-run result retained;
only affected theory cases rerun after this test-only correction. Full Host: 150 passed, 2 failed, zero skipped, exit 2, 204.266 seconds.
The exact two corrected cases passed (exit 0, 38.063 seconds). XML identity
comparison confirms these are exactly the failed rows: all 152 Host cases covered
across the full run and targeted rerun, not a single green full-suite rerun.
See `artifacts/rc-force-entry/host-coverage-receipt.json`. Runner 470/470
passed in 116.575 seconds. Production code was unchanged after the full Host run.

Coordinator source/diff review verified atomic entry ordering, schema8 compatibility,
round completeness and transcript-bound RBA-to-Force timestamp equality. Changed
graph paths used direct source/diff fallback; adapter coverage partial outside the
reviewed binding method. No unresolved source finding. No new independent acceptance claimed; earlier
independent review budget exhausted at 3/3. Full Core and hosted CI not run; focused Core and affected Runner
checks cover the narrow adapter guard. Exact-head hosted CI remains required before merge.

Original certificate/source-pin/dependency gaps remain open. No public RC claim,
no PR/push/merge. Final checks, commit and active-process status recorded below.

Suggested PR title: `Start Force deadlines at accepted decision entry`.

No active workers, builds or tests remain. This report is included in the feature
commit; identify it with `git log -1 --format=%H -- docs/work/reviews/2026-10-24-force-entry-deadline-handoff.md`.
