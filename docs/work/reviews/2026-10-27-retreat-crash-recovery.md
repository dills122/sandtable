# Repeated required-Retreat abrupt-process recovery

Base `611a4be298c9f13ff1e0954d57215028a9a2710f` (local validated Custody packet),
on merged PR335 main `72a167f7`. Branch `codex/rc-retreat-crash-recovery` retains
both packets; no prior commit overwritten. Issue330; Ruflo task
`task-1793150798579-jn3r67`, namespace `sandtable-rc-20261023`.
Status: focused validation complete;16/16 selected tests passed, zero failed/skipped.

## History and crash boundaries

Existing seed4 fixture traverses first paid combat and an accepted repeat, then
finishes the second paid assault normally. Accepted facts require one Retreat,
defenderPercent5 and no captured role. Paid RNG advances cursor10 to18 with exact
retained dice. Contract22 and result-hash binding establish genuine required
Retreat; no fabricated source state, owner refusal, dice or P01 ancestry.

A separate copy queries and submits legal owner refusal. Its settled World is the
reference for timeout's source-defined refusal; no timeout/crash branch queries
its Retreat owner. Fallback must bind the actual paid result/decision/World and
persist neither owner action nor destination.

Healthy clone uses increasing clock milliseconds. Persisted Cycle opened_ms locates
the exact read after Retreat timeout, timed-out row, World settlement, no-captive
System disposition, native successor and empty Reserve completion writes. Source
path is TickRetreatRound -> TimeoutRetreatRound -> AppendWorldSettlement ->
AppendNativeCombatSuccessor -> AppendEmptyReserveRelease -> InitializeCycleRound;
the enclosing transaction commits only after this path returns.

Before-commit child pauses at that calibrated read; after-commit child pauses when
TickRetreatRound returns, before normal response. Parent reads committed tables
while paused, kills process via OS termination, and waits for nonzero exit before
reopening. Stdin remains open until kill; no managed exception/disposal substitutes
for a crash. Shared child harness bounds handshakes and process cleanup.

## Pass criteria

- Precommit committed rows/reopened authority equal untouched original; postcommit
  lost-response state equals fully committed healthy control.
- Fresh process recovers/refuses once; second fresh process retries without event,
  version or replay-hash changes.
- Full14 logical tables and canonical reconstructed bytes/hashes match control.
- Settlement World equals independent legal owner-refusal branch, differs from
  paid World and has no pending captives. Paid events/RNG and owner records unchanged.
- Exactly one System Retreat timeout, zero fabricated owner Retreat events, two
  paid assaults and normal collecting Cycle continuation.
- Existing Reserve/Custody crash modes, fresh-process proofs, repeated Retreat
  rollback/timeout and default legacy child remain green.

## Validation and scope

Host Release build and changed-file format passed, zero build warnings/errors.

| Focused check | Passed | Exit | Seconds |
| --- | ---: | ---: | ---: |
| `*RepeatedRequiredRetreatCrashAtFallbackBoundary*` | 2 | 0 | 34.623 |
| `*RepeatedCustodyCrashAtFallbackBoundary*` | 2 | 0 | 31.75 |
| `*ReserveCrashAtFallbackBoundary*` | 2 | 0 | 57.337 |
| `*FreshProcess*` | 7 | 0 | 114.094 |
| `*RepeatedRetreatTimeoutRefusesActualSecondRequiredRetreat*` | 2 | 0 | 20.167 |
| `*ChildProcessReadsCommittedProbeAfterLostResponse*` | 1 | 0 | 2.53 |

Exact commands/counts/source hashes: [ledger](2026-10-27-retreat-crash-recovery.json).
Coordinator reviewed actual-history preconditions, transaction ordering, calibrated
stop point, OS termination, owner-refusal World reference, replay/RNG assertions
and unchanged shared child defaults. No production defect exposed. No active tests remain.
Retained local logs/receipts: `artifacts/rc-retreat-crash/`.
Changes confined to test child mode, two new tests and documentation. No runtime,
schema, contract, fixture, source-pin or timing-catalog change. Core/Runner/full Host,
whole solution, Python/frontend and hosted CI unrun. Coordinator source review only;
no new independent review claimed or prior review allowance reused.

Evidence closes these two process-kill points for this actual history. Earlier
writes, arbitrary fsync cuts, disk corruption and machine power loss remain separate
obligations. No public admission or playable RC claim. Next integration step: review
and PR the retained Custody+Retreat packets, with exact-head CI before merge.
