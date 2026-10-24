# Reserve Release timeout handoff

Base `d7993d7737f761a7d486411c5714a6ad8e221370`, branch
`codex/rc-overnight-plan-20261023`. Ruflo task `task-1792882630464-o1ta19`,
namespace `sandtable-rc-20261023`.

## Delivered behavior

Native V35 `reserve-release-timeout` records one explicit System disposition
batch and completes Reserve Release. Due first-cycle Reserve I converts to
Reserve II; due later-cycle Reserve II stays in reserve. Accepted owner choices,
movement allowances, other members, location, operational state and components
are preserved. No owner release/retain receipts or new movement allowances are
fabricated. RNG remains unchanged. Existing owner V27 bytes stay unchanged.
Host accepted-step version29 is separate from native event version35.

Schema11 persists the deadline atomically at Reserve Release entry. Partial
choices, queries and restarts never renew it. Background scheduling uses a fourth
independent bounded cursor. Timeout and the next cycle-control entry persist in
one transaction. Empty release queues complete directly without a Reserve round.
Unavailable entry records absent clocks explicitly. Rejected stale/invalid
proposals cannot consume the deadline. Legacy5–10 journals remain read-only;
historical RBA8/Force9/cycle10 verification remains active.

## Validation and review

Selected validation completed successfully.

- Focused Core: 27/27 passed, exit0, 5.047 seconds, including nine new cases.
- Focused Host: 12/12 passed, exit0, 28.548 seconds, including genuine cycle2
  retention, no-query restart, partial owner receipt preservation, clock failures,
  invalid late proposals, rollback, altered/missing rows and legacy10 verification.
- Whole Host suite: 176/176 passed, exit0, 287.809 seconds.
- Final Runner suite: 470/470 passed, exit0, 119.312 seconds.
- Final boundary suite: 81/81 passed, exit0, 12.431 seconds.
- Release solution build: exit0, zero warnings/errors, 5.789 seconds.
- Nine new CI timing rows measured from passing Release XML; existing weights
  and baseline metadata preserved. Actual discovery accounts for 3,622 Core rows
  (3,297 ordinary plus 325 R02); dry-run exit0, no shard executed.
- Final format verification: exit0, 32.659 seconds after whitespace-only fix.
Coordinator review found and corrected an inherited 8192-byte dispatch limit
that would reject larger V35 batches. Legacy V27 retains its original bound;
V35 uses its own bounded batch codec. Regression coverage included. Focused Host tests also exposed a real later-cycle
replay bypass that rejected V35. PostRepeat now uses canonical Reserve Release
replay, and the genuine cycle2 regression passes. Initial fixture/analyzer/helper
compile failures and formatter whitespace failure remain in local evidence;
they are not counted as passes or claimed as production RED evidence.

New Host continuation test follows a genuine first assault, accepted partial
release, first-cycle conversion, cycle2 movement/no-attack and Reserve II
retention through cold replay. Ordinal3 fallback has focused Core state coverage;
no new genuine third-cycle Host Reserve fallback fixture is claimed.

Evidence: `artifacts/rc-reserve-timeout`, `artifacts/rc-reserve-core-evidence`,
`artifacts/rc-reserve-host`, and referenced `artifacts/local-tests` receipts.
An earlier Runner/boundary pass preceded the PostRepeat correction; final
validation uses the refreshed build, without treating earlier binaries as proof.

Graph coverage at base generation2026-10-24T22:51:34Z was best-effort. Changed
and new source received direct source/diff review; partial adapter/session index
coverage was not treated as exhaustive. Earlier independent review allowance
remains exhausted at3/3; no new independent acceptance claimed.

Full Core and exact-head hosted CI not run for this focused iteration. Original
P01/public certificates, source-pin failures and dependency remediation remain
open. No public RC claim, push, PR or merge.

Suggested PR title: `Apply persisted Reserve Release timeout fallback`.

Next bounded candidate: persisted Custody `leave-unguarded` fallback, requiring
its own versioned lot/disposition effects and durable decision-entry deadline;
not implemented or newly audited in this packet.

No implementation worker builds or test processes remain. This handoff is part
of the feature commit; identify it with `git log -1 --format=%H -- docs/work/reviews/2026-10-24-reserve-release-timeout-handoff.md`.
