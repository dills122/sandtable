# Persisted RBA timeout handoff

Branch: `codex/rc-overnight-plan-20261023`; change base `7554bb22`.
Ruflo task `task-1792877172812-3loke8`, namespace `sandtable-rc-20261023`.

## Delivered behavior

Native V33 cancels unanswered Retreat Before Assault into same-slot Reserve
Release without a fabricated decline, Force seal, ammunition debit or combat roll.
Accepted retreat movement and RNG remain unchanged. Initial and repeated assault
paths retain cancellation through checkpoint/replay.

Schema 8 persists RBA opening/deadline atomically with Barrage. Reopening and
retreat moves retain the original deadline; valid decline closes it. Invalid,
stale or foreign proposals cannot consume the deadline. A one-second hosted tick
uses the same clock and processes bounded pages, isolating corrupt campaigns.
Missing timing/clock at entry uses explicit unavailable evidence. Legacy schemas
5/6/7 remain readable but cannot be mutated; no implicit upgrade is provided.

## Verification

- Focused Core: 17/17 passed, 12.781 seconds; receipt/XML in `artifacts/rc-rba-core`.
- Focused Host RBA: 13/13 passed, 39.920 seconds; XML in `artifacts/rc-rba/host-focused`.
  Covers genuine second/third assault histories, restart, two retreat moves,
  invalid late proposal, exact deadline, clock loss/regression, tampering, schema 7,
  rollback and corrupt-seed isolation/fairness.
- Boundary: 81/81 passed, 7.504 seconds.
- Release solution build: exit 0, zero warnings/errors. Format check: exit 0.
- Python partition helpers: 194 passed. Initial sandbox supervisor error retained;
  elevated rerun passed without helper source changes.
- Actual-row discovery: exit 0; all 3,606 Core rows accounted for (3,281 ordinary
  plus 325 R02). Six new measured timing rows accepted; no shard executed.
- Runner: 470/470 passed, 2m02.383s.
- Whole Host: 141/141 passed, 3m00.832s; XML in `artifacts/rc-rba/host-all`.
- Full Core/`just check` and hosted exact-head CI: not run for this change.
- Original Breakdown/cycle source-pin oracles: not rerun; prior failures remain
  open. This change does not repair or relabel their evidence.

Initial sandbox build startup stall, four analyzer failures and an invalid MTP
invocation selecting zero tests were diagnosed before corrected runs. None are
counted as passing evidence. Logs/receipts retained under `artifacts/rc-rba*` and
`artifacts/rc-rba/host-evidence.md`; build/startup failures retained there.

Fast iteration commands:

```sh
just test-class core '*CampaignNativeRbaCancellationTests'
just test-method host '*Rba*'
```

## Independent review and open gates

Review instance 3 of 3: report `2026-10-24-rba-timeout-independent-review.md`.
Scheduler corruption-isolation finding accepted and fixed with bounded exception
handling and malformed/overflow seed regressions. Final verdict: Ready with non-blocking follow-ups.
Actual hosted timer start/stop lifecycle is not directly tested; deterministic
sweep and fair pagination are covered. No throughput certification is claimed.

Public HOST-PUB/020–025, original P01 certificates (44 provisional positive,
256 absent, zero admitted), dependency remediation and other missing fallback
windows remain open. This is a bounded native runtime fix, not an RC declaration.
No PR, push or merge performed. Exact-head CI remains required before merge.

Next bounded task: characterize the next configured fallback window lacking a
persisted deadline and native cancellation, using the same focused-suite workflow.
All validation processes finished. No active implementation workers or test runs
remain at handoff. Commit contains this report; identify it with
`git log -1 --format=%H -- docs/work/reviews/2026-10-24-rba-timeout-handoff.md`.

Suggested PR title: `Persist native RBA deadlines and cancel unanswered combat decisions`.
