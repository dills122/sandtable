# Persisted Force background timeout handoff

Branch `codex/rc-overnight-plan-20261023`, base `cbc6954d`.
Ruflo `task-1792878995710-3j4qn7`, namespace `sandtable-rc-20261023`.

## Problem and scope

Already-persisted Force Assignment rounds could remain collecting indefinitely
when owners disconnected: `TickForceRound` had only test callers. Extend hosted
combat ticking to these rounds, with independent bounded RBA/Force cursors and
per-campaign error isolation. Existing Core cancellation and replay remain intact.

Force opening remains lazy. No deadline is invented for a decision that has never
opened a Force round. That lifecycle gap remains a separate next bounded task.
No Core contracts, SQLite schema, certificates or source pins change here.

## Verification

Regression before fix: two disconnected-round cases (zero/one retained seal)
failed after deadline/restart, leaving checkpoints15/17 rather than27. Exit2,
7.993seconds, `artifacts/rc-force-background/red.log` and RED XML. These failures
are retained evidence, not counted as passing tests.

After fix: three scheduler cases passed, exit0, 10.122seconds;
`artifacts/rc-force-background/green.log` and XML. Release solution build passed
(exit0, zero warnings/errors, 23.75seconds); format verification passed (exit0).
Whole Host passed144/144, zero failures/skips, exit0, 3m30.195seconds.
Exact commands, logs and XML: `artifacts/rc-force-background/receipt.json`.

## Review and release limits

Coordinator source/diff review found no actionable defect in the bounded change.
Verified independent cursor state, per-lane enumeration error return, shared
per-campaign exception isolation, current-schema guard, existing cancellation
transaction and hosted registration. Graph coverage for newly added/changed files
was not yet current, so exact source and diff were read directly. Prior independent-review
budget exhausted at3/3; no new independent review or acceptance claimed here.
No full Core/solution test run or hosted exact-head CI claimed. Existing original
P01/certificate/source-pin/dependency gaps remain open. No public RC claim.
No push, PR or merge. Require exact-head CI before any later merge.

Suggested PR title: `Expire persisted Force rounds through background combat ticks`.

All worker/test processes finished. Working change ready for commit; identify
commit with `git log -1 --format=%H -- docs/work/reviews/2026-10-24-force-background-timeout-handoff.md`.

Focused iteration command: `just test-method host '*ForceScheduler*'`.
