# Reserve fallback abrupt-process recovery

Base main4017d78c (merged PR333); branch codex/rc-reserve-crash-recovery.
Issue330; Ruflo task task-1792935320571-nl38a1 in sandtable-rc-20261023.

## Boundary and mechanism

Actual third-cycle Reserve fixture contains two paid assaults, two accepted repeats,
ordered owner choices and historical System I conversion. Healthy clone uses unique
increasing clock milliseconds; persisted Cycle opened_ms identifies Cycle-entry
read. Source TimeoutReserveRound first persists accepted Reserve timeout, updates
Reserve row to timed-out, then calls InitializeCycleRound. TickReserveRound commits
after that call. This clock read is therefore after those transactional writes but
before Cycle insert/commit. No production hook, reflection or guessed read count.

Test-only child mode opens same baseline, verifies replay and arms same stepping
clock. Before-commit case pauses on calibrated clock read with stdout handshake
and blocking stdin. Parent keeps stdin open, inspects committed rows with read-only
SQL, then Process.Kill terminates process; no managed transaction disposal used as
crash substitute. After-commit case pauses immediately after TickReserveRound returns,
before normal response, then receives same abrupt termination. Parent waits for
nonzero exit before reopening authority.

Before-commit visible/reopened state must equal original; after-commit state must
equal fully committed healthy clone. Fresh child retries interrupted operation and
another fresh child retries again. Full14 logical tables/replay hashes/bytes equal
healthy control; third-cycle retain-II, original choices, two paid results and two
historical Reserve timeout events retained exactly. Retry adds no event. Every
pause/read/exit wait bounded; exceptional cleanup kills and reaps child tree.

## Scope

Changes limited to test child mode, optional mode argument on existing child launcher,
new crash test, checklist and handoff. Existing default child protocols unchanged.
No runtime/schema/contract changes. Precommit and lost-response cuts are bounded
process-kill evidence, not disk corruption, partial-fsync or machine power-loss proof.
Database cache need not have flushed uncommitted pages at pause; no such claim made.
Other lanes and crash cuts remain open. Graph coverage checked relied runtime/helper
paths; direct source inspected transaction ordering and read-only inspection strategy.
CCE unavailable; Ruflo and repository retain decisions.

## Validation

All12 selected tests passed, no runtime test failures.

| Check | Result | Wall seconds |
| --- | --- | --- |
| Host Release build | exit0 | 3.835 |
| *ReserveCrashAtFallbackBoundary* | 2/2 exit0 | 57.409 |
| *FreshProcess* | 7/7 exit0 | 123.955 |
| *ThirdCycleReserveTimeout* | 2/2 exit0 | 35.838 |
| *ChildProcessReadsCommittedProbeAfterLostResponse* | 1/1 exit0 | 2.963 |
| Final solution format | exit0 | 29.131 |

Build: `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`.
Tests: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '<selector>'`.
Format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`.

Initial build CA1861 constant-array finding corrected with static readonly field.
Initial format check found object-initializer whitespace; corrected line breaks
only, no executable change after passing build. Both failures retained with final
logs/JSON receipts/source hashes under ignored artifacts/rc-reserve-crash/.
No whole Core/Host/Runner/boundary/oracle/frontend run for this test-only change.
Coordinator source review, no new independent review or CI claimed. No active test
process remains. Local feature commit awaits PR/integration validation.

## Follow-up disposition

Issue330 now distinguishes merged PR333 fresh-process cuts from crash gates.
This packet closes only deterministic Reserve precommit and postcommit/lost-response
process-kill witnesses in the actual third-cycle fixture. Other-lane failures,
arbitrary write/fsync cuts and power-loss remain open. Next priority can shift to
original Breakdown/cycle source-pin reconciliation, with retained failures and
reviewed semantic justification rather than blind pin replacement.
