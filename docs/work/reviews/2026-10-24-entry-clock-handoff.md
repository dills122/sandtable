# Custody and Retreat entry-clock fault handoff

Status: four exact-boundary witnesses and 22 neighboring tests pass. Test-only packet; no runtime gap exposed. Base
`69175f16fefa3da50171ab9758dbfb4c428b656c`; branch
`codex/rc-overnight-plan-20261023`; [PR329](https://github.com/dills122/sandtable/pull/329).
Ruflo task `task-1792895685734-7va0ib`.

## Required distinction

Clock fails at entry of real Custody/required Retreat after paid combat completes.
Earlier Force cancellation or later polling fault cannot close this gate.

Healthy saved twin records unique increasing clock milliseconds. Its persisted
round opened_ms identifies exact entry read. Fault twin starts from identical
pending authority/clock, throws only at matching read, then recovers. Expected
fallback is custody-unavailable/retreat-unavailable with−1 timing and no invented
observed timestamp. Paid result and Force completion must match healthy branch;
no target round or Force cancellation may be fabricated.

## Validation

New test: `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.EntryClock.cs`.
Cases: initial Custody seed5, initial required Retreat seed6, repeated Custody
seed50 and repeated required Retreat seed4. Repeated cases traverse two actual
paid assaults; no authoritative state/RNG injection. Healthy/fault paid-result
bytes match; target timeout binds the actual settlement/result and configuration.
System fallback retains paid RNG, strict cold replay verifies and retry returns
same Force receipt. Recovered-clock sweep appends no duplicate history.

| Check | Result | Wall seconds |
| --- | --- | --- |
| Host Release build | exit0 | 4.616 |
| `*EntryClockFailure*` | 4/4, exit0 | 25.842 |
| `*.CustodyTimeout*` | 11/11, exit0 | 31.873 |
| `*.RetreatTimeout*` | 11/11, exit0 | 31.742 |

Exact build: `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`.
Each test selector uses `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '<selector>'`.
Final format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore` — exit0, 27.328 seconds.

Local ignored logs/JSON receipts: `artifacts/rc-entry-clock/`. Initial build failed
missing Campaigns import; second failed xUnit2031 predicate-overload analyzer.
Both corrected; failures retained as initial-build/analyzer-build logs/receipts.
No runtime test failures. Full Core/Host, Runner, boundaries, contracts, Python
oracles and frontend checks not rerun for this test-only packet.

Coordinator reviewed source and calibrated injection against InitializeCustodyRound
and InitializeRetreatRound: successful entry read directly supplies persisted
opened_ms. Graph coverage matching metadata, best-effort; no reported gaps on
new proof or relied round implementations. No fresh independent review claimed.
Same-process saved-copy reopen is not a fresh-process or abrupt-crash proof.
No production/shared fixture changes. Worker finished; no test process remains
after final format completed.

## Open gates

Remaining fresh-process/crash cuts, source-pin reconciliation, dependencies,
public certificates and six-turn playable product gates remain open. Original
inventory44 provisional positive/256 absent/0 admitted. Coordinator review does
not replace independent release review; prior packet allowance exhausted.
Exact-head CI required before merge; PR remains draft. No merge performed.
