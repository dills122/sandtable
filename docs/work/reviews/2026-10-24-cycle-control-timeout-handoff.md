# Cycle-control timeout handoff

Base `7d0f34fd`, branch `codex/rc-overnight-plan-20261023`.
Ruflo task `task-1792881286899-jfjd9v`, namespace `sandtable-rc-20261023`.

## Behavior and boundaries

Completed Reserve Release starts an immutable cycle-control deadline. Expiry
records native V34 `movement-combat-cycle-timeout`, an explicit System
`finish-phase` fallback into same-slot Truck Convoy Movement. World, RNG and cycle
ordinal remain unchanged. The derived event shares V28 finish projection logic,
but retains distinct canonical timing evidence; no owner Finish is fabricated.
Owner Finish/Repeat event bytes remain unchanged. Accepted-step version 28 is a
separate version domain from native event 34.

Schema 10 persists cycle rounds atomically at entry. Queries/restarts cannot renew
them; valid owner choice closes the round. Invalid proposals cannot consume it.
Background scheduling uses an independent bounded cycle cursor alongside RBA and
Force. Explicit unavailable entry records absent clocks rather than invented time.
Legacy schemas 5–9 journals stay read-only with historical checks: schema 9 Force entry and
schema 8 RBA verification remain active.

## Validation and review

RED before fix: no-query completed Release remained at 27 after clock advance and
scheduler sweep; one failure, exit 2, 17.419 seconds. Retained under
`artifacts/rc-cycle-timeout/red`. Validation completed so far:

- Core focused: 24/24 passed, 17.932 seconds. Final seven new cases (including
  added foreign-configuration assertions) passed again in 9.019 seconds.
- Host focused: 12 cycle cases passed in 71.161 seconds; four genuine repeated
  histories passed in 64.399 seconds; clock-loss compatibility passed in 9.894 seconds.
- Boundary: 81/81 passed, 20.315 seconds. Runner: 470/470 passed, 169.258 seconds.
- Release solution build passed with zero warnings/errors (13.60 seconds).
  Final format verification passed after an import-order correction.
- Actual discovery: all 3,613 Core rows accounted for (3,288 ordinary plus 325 R02).
  Seven measured timing rows added; existing weights and baseline metadata unchanged.
- Whole Host: 164/164 passed, exit 0, zero failures/skips, 260.317 seconds.
  No full Core or hosted CI run claimed.

Logs, XML and receipts: `artifacts/rc-cycle-timeout`, `artifacts/rc-cycle-core`,
`artifacts/rc-cycle-core-final` and referenced `artifacts/local-tests` directories.
Initial missing-using build failure, zero-test filter invocations (exit 5) and
format failure are retained and are not counted as passes.

Coordinator source/diff review checks canonical distinction, terminal guards,
replay across repeated paths, atomic entry/closure and historical schema guards.
Prior independent-review budget remains exhausted at 3/3; no new independent
acceptance claimed. Full Core and hosted exact-head CI not claimed. New Core rows
were measured and reconciled with actual discovery before commit.

Original P01/public certificates, source-pin failures, dependency remediation and
other fallback windows remain open. No public RC claim, push, PR or merge.

Suggested PR title: `Finish unanswered combat cycles through persisted deadlines`.

Focused iteration commands:

```sh
just test-class core '*CampaignNativeCycleControlTimeoutTests'
just test-method host '*CycleTimeout*'
```

No active implementation workers, builds or tests remain. This report is part of
the feature commit; identify it with `git log -1 --format=%H -- docs/work/reviews/2026-10-24-cycle-control-timeout-handoff.md`.
