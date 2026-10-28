# Repeated Custody fallback abrupt-process recovery

Base main `72a167f763f436511b341105751a0a4a506ac89d` (merged PR335).
Branch `codex/rc-custody-crash-recovery`; issue330.
Ruflo task `task-1793150148372-r2bimj`, namespace `sandtable-rc-20261023`.
Status: focused validation complete;14/14 selected tests passed, zero failed/skipped.

## Selected blocker and boundary

Issue330 Host crash reliability remains open beyond the Reserve packet merged in
PR334. This packet targets genuine repeated captured-lot Custody with seed50:
two accepted paid assaults and an accepted repeat create the actual second lot
(axis-infantry-second captures commonwealth-infantry-west, quantity1).
No injected state, invented decline, RNG result or P01 ancestry.

Healthy clone uses an increasing clock. Persisted Cycle entry time identifies the
exact read after Custody timeout, timed-out round, successor and empty Reserve
completion writes, before Cycle insert and transaction commit. Source ordering:
TickCustodyRound -> TimeoutCustodyRound -> AppendNativeCombatSuccessor ->
AppendEmptyReserveRelease -> InitializeCycleRound -> transaction.Commit.
No production hook or hard-coded read index.

Shared test-only crash child now accepts a Custody mode alongside unchanged Reserve
mode. Before-commit case pauses at calibrated read; after-commit case pauses just
after TickCustodyRound returns, before normal reply. Parent inspects committed rows
using read-only SQL, abruptly kills child with OS process termination, and waits
for nonzero exit. Blocking stdin remains open until termination, so managed
transaction disposal cannot stand in for crash recovery. Waits and cleanup bounded.

## Pass criteria

- Interrupted precommit state equals untouched original; postcommit lost-response
  state equals fully committed healthy clone, including visible rows while paused.
- A new process applies leave-unguarded exactly once or observes committed fallback.
- Another new process retries without any accepted event, state-version or hash change.
- All14 logical journal tables, canonical replay bytes and replay hashes match control.
- Exact captured lot, settlement/World binding and deadline preserved. Original paid
  draws/cursor, owner actions/openings and successor custody/RNG hashes preserved.
- Exactly one Custody timeout, no fabricated owner Custody event, two paid assaults;
  resulting campaign collects Cycle control normally.
- Existing Reserve crash, fresh-process Custody and default child paths remain green.

## Validation and limits

Focused Release Host build passed without warnings/errors; changed-file format exit0.

| Focused selector | Passed | Exit | Seconds |
| --- | ---: | ---: | ---: |
| `*RepeatedCustodyCrashAtFallbackBoundary*` | 2 | 0 | 34.731 |
| `*ReserveCrashAtFallbackBoundary*` | 2 | 0 | 55.582 |
| `*FreshProcess*` | 7 | 0 | 119.803 |
| `*RepeatedCustodyTimeoutLeavesActualSecondCapturedLotUnguarded*` | 2 | 0 | 20.225 |
| `*ChildProcessReadsCommittedProbeAfterLostResponse*` | 1 | 0 | 2.693 |

Build: `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`.
Tests: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '<selector>' --report-xunit-xml --results-directory '<results>'`.
Exact commands, counts and source hashes: [ledger](2026-10-27-custody-crash-recovery.json).
Coordinator reviewed transaction ordering, calibrated stop point, OS termination,
replay/owner/RNG assertions, bounded cleanup and unchanged legacy mode defaults.
No production persistence defect exposed; changes add missing failure-boundary proof.
No test processes remain active.
Logs and receipts retained in `artifacts/rc-custody-crash/`.
Initial sandbox build produced no output and was interrupted/terminated before
retry with normal compiler access; it is not passing build evidence. No test killed.

Changes restricted to tests and test child protocol. No Core/Host runtime, schema,
fixture or evidence-pin change. Full solution, Core, Runner, Python/frontend and
hosted exact-head CI unrun for this test-only packet. Coordinator source review;
no new independent review claimed. Earlier review allowances are not reused.
Process-kill proof does not establish arbitrary write/fsync cuts, disk corruption,
or power-loss recovery. Repeated required-Retreat crash boundaries are next bounded
Host candidate; public certification and broader RC gates remain open.
