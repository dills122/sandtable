# Fresh-process third-cycle Reserve recovery

Base merged main `fc4cb9255dd6d891a2e333fc82f42d8e7afe5a2b` (PR329).
Branch `codex/rc-fresh-process-third-reserve`; follow-up issue330;
Ruflo task `task-1792898326345-wwiuik`, namespace `sandtable-rc-20261023`.

## Plan and pass criteria

Reach actual third-cycle Reserve through unchanged fixed-seed12345 setup, two
paid assaults, two accepted repeats, ordered owner releases and historical System
Reserve I conversion. Save pending ordinal3 before any Reserve owner query.
Run existing native deadline child in separate OS processes at deadline−1ms,
exact deadline and retry. Each child opens disk authority, verifies replay,
sweeps background deadlines and verifies replay again; no parent campaign objects
or owner credentials passed. Compare all14 logical journal tables and replay
hashes/bytes with uninterrupted control after every child exit.

Assert no early expiry/renewal; exact third-cycle System retain-II event preserves
World, owner openings/actions, ordered choices and paid-result bytes. Historical
initial conversion remains present; no impossible third-cycle I conversion is
invented. Retry adds nothing. Recovered third cycle remains Finish-only and can
continue identically. Preserve parent-alive versus abrupt-crash distinction.

## Implementation

New isolated partial test file:
`tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.FreshProcessReserve.cs`.
Reuses PrepareThirdCycleReserve, FreshProcessRunDeadlineChild and journal comparison
helpers. No production, schema, child protocol or shared fixture edits. No runtime
gap exposed. No authoritative state/RNG injection or timing sleeps.

Graph Tier2 coverage matching metadata for four existing helper/child files and
new test. Helper trace produced implausible cross-name callees; actual Process.Start,
child protocol, fixture and comparison bodies read directly instead of relying on
those edges. CCE unavailable; decisions recorded in Ruflo and this document.

## Validation

All7 focused tests pass, no failed attempts. Local ignored logs and JSON
command/exit/time receipts: artifacts/rc-fresh-process-reserve/.

| Check | Result | Wall seconds |
| --- | --- | --- |
| Host Release build | exit0, no warnings/errors | 14.524 |
| *FreshProcessThirdCycleReserve* | 1/1 exit0, three child launches | 42.306 |
| *ThirdCycleReserveTimeout* | 2/2 exit0 | 38.560 |
| *FreshProcessNativeDeadline* | 4/4 exit0 | 54.001 |
| Solution format | exit0 | 28.782 |

Build: `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`.
Tests: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '<selector>'`.
Format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`.
No whole Core/Host/Runner/boundary/oracle/frontend rerun for test-only packet.
Coordinator source review checked exact before/due/retry counts, child PID vs
parent, event/World/owner parity and strict replay. Source manifest retained with
receipts. No active test or worker processes remain. New branch not yet reviewed
or validated by CI; earlier PR329 acceptance applies to its own head only.

## Limits and next step

New proof covers one supported fixed-seed actual third-cycle Reserve cut, three
child launches. Initial I conversion is historical authority, not a newly tested
fresh-process initial Reserve cut. Parent stays alive, so abrupt crash/power loss
remains open. Repeated Custody/Retreat fresh-process proofs remain next bounded
recovery work. Public certificates, source pins, dependencies, capacity and
playable product gates remain in issue330. Coordinator review only; prior full-PR
independent acceptance does not automatically cover this new test.
