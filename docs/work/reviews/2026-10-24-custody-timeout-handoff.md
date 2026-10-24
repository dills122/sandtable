# Custody timeout handoff

Base `ac1cab5403637e85970f3d425885ca92171f5c37`, branch
`codex/rc-overnight-plan-20261023`. Ruflo task `task-1792883677865-f620ih`,
namespace `sandtable-rc-20261023`.

## Behavior

Native V36 `combat-custody-timeout` records an explicit System leave-unguarded
disposition. Existing source projection marks the captive lot escaped and creates
deferred replacement eligibility with training pending. No guard TOE debit,
owner receipt or random result is fabricated. Native owner V25 bytes remain
unchanged; Host accepted-step version30 is a separate version domain.

Schema12 starts an immutable Custody deadline at accepted settlement. Background
expiry needs no owner query and survives restart. Owner submission validates
before clock mutation; accepted disposition cannot be overridden and retries
retain its receipt. Timeout, round closure and Combat successor/Reserve entry
persist atomically. Missing clocks/configuration use explicit unavailable
evidence. Old schemas5–11 remain read-only with historical round verification.

Supported native path has exactly one captive lot. Current source rules put the
victim at captive origin or one adjacent clear-terrain retreat hex, so the legal
leave-unguarded route fits existing bounds. No future multi-lot/multi-hex profile
or public admission is implied; unavailable source-legal fallback rejects before
a collecting deadline can be committed. See native spec for source constraints.

## Validation

Selected validation completed successfully.

- Whole Host187/187: exit0,300.776 seconds.
- Final focused Host11/11: exit0,47.052 seconds.
- Core focused35/35, including nine new cases: exit0,30.205 seconds.
- Runner470/470: exit0,143.216 seconds.
- Boundary81/81: exit0,8.147 seconds.
- Release solution build: exit0,20.604 seconds,zero warnings/errors.
- Format verification: exit0,36.737 seconds.
- Nine measured new Core rows added to CI timing manifest; existing weights and
  baseline metadata preserved. Actual discovery reconciled3,631 Core rows
  (3,306 ordinary plus325 R02), dry-run exit0; no full shard executed.

Bounded fixed-seed5 Host experiment proved first timeout and a genuine second
assault, but the second result had no pending captive lot. Its original expected-
capture assertion failed (exit2) and is retained in `artifacts/rc-custody-host/repeat.log`.
The final continuation regression explicitly expects no second lot/timeout; no
seed scan or genuine second/third-cycle captured-lot timeout proof is claimed.
Shared RepeatAssault/PostRepeat replay dispatch received source review. Retained
repeated captured-lot expiry remains an integration evidence gap. Host tests
cover clock loss/regression on an open round; unavailable entry evidence is tested
in Core, not a separate Host fault injected exactly at settlement entry.

Coordinator source/diff review follows graph coverage checks and direct source
fallback for changed/new and partially indexed adapter/session paths. Prior
independent review allowance remains exhausted3/3; no new independent acceptance
claimed. Full Core and hosted CI not claimed for this focused iteration.

Evidence directories: `artifacts/rc-custody-core`, `artifacts/rc-custody-host`,
`artifacts/rc-custody-timeout`, and referenced `artifacts/local-tests` receipts.
Source-pin failures, P01/public certification, dependency remediation and playable
scenario/UI/distribution gates remain open. No push, PR, merge or public RC claim.

Suggested PR title: `Apply persisted Custody leave-unguarded timeout`.

No active implementation builds or test processes remain. This report belongs to
the feature commit; identify it with `git log -1 --format=%H -- docs/work/reviews/2026-10-24-custody-timeout-handoff.md`.
