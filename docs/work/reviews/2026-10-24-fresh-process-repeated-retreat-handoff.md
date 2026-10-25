# Fresh-process genuine repeated required-Retreat recovery

Base `99478cd9428e2bef8da6e3a422616995d3ca7b7b`, branch
`codex/rc-fresh-process-third-reserve`; stacked after Reserve and Custody proofs.
Issue330; Ruflo task `task-1792899139735-r5dgyq`, namespace
`sandtable-rc-20261023`.

## Plan and proof boundary

Reuse seed4 actual accepted first combat, Reserve release, Cycle repeat, Movement,
west assault selection and RBA decline. Complete second Force normally; its paid
result requires Retreat. No manufactured snapshots/results/RNG or seed search.
Save pending version22 before querying its Retreat owner. Separate reference copy
executes legal owner refusal, while control and recovered expiry copies never
query that owner.

Three fresh OS child processes open disk state and verify replay before/after
background sweep: deadline−1ms, exact deadline, retry. Compare all14 logical
journal tables and replay bytes/hashes to uninterrupted control. Bind fallback to
actual second result/defender/paid World and original immutable clock fields.
Assert explicit System refuse-retreat, no owner action/destination, unchanged
owner records and paid event, successor RNG18 and opened Cycle. Resulting settled
World matches actual legal owner-refusal branch and differs from pre-settlement
paid World. No duplicate event on retry.

V24 exposes World hashes rather than numeric TOE/distance. This proves parity
with legal owner refusal plus verified replay, not a direct numeric loss assertion.
Parent remains alive; fresh child recovery is not abrupt-crash/power-loss proof.

## Scope and review

New isolated FreshProcessRetreat.cs test partial; no production, schema, child
protocol or shared fixture change. Graph Tier2 exact existing helper paths have
matching metadata; direct source confirms fixture/child/comparison behavior.
CCE unavailable, decisions retained in Ruflo/repository. Coordinator review only;
prior PR329 independent review does not automatically cover new tests.

## Validation

All18 focused tests passed, no failed attempts. Local ignored logs/JSON receipts
under artifacts/rc-fresh-process-retreat/ retain exact commands, exits and times.

| Check | Result | Wall seconds |
| --- | --- | --- |
| Host Release build | exit0 | 13.680 |
| *FreshProcessRepeatedRequiredRetreat* | 1/1 exit0, three child launches | 35.483 |
| *RepeatedRetreatTimeout* | 2/2 exit0 | 21.253 |
| *.RetreatTimeout* | 11/11 exit0 | 30.731 |
| *FreshProcessNativeDeadline* | 4/4 exit0 | 56.259 |
| Solution format | exit0 | 35.280 |

Build: `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`.
Tests: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '<selector>'`.
Format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`.
No runtime defect exposed. Full Core/Host/Runner, boundary, contracts, Python
oracles and frontend not rerun for test-only packet. Coordinator reviewed exact
fallback binding, owner-refusal parity, process boundaries and retry assertions;
source hash retained with receipts. No active test processes remain. Local commit
only; packet has not received new CI or independent review.

## Remaining RC work

Other native fresh-process cuts and abrupt crash recovery remain open. No public
profile admission, original-root certificate, source-pin reconciliation, dependency,
capacity, migration or playable-product gate is closed by this test. Next checkpoint
can package the three local recovery commits for PR validation; issue330 remains
canonical follow-up tracker with measurable pass criteria.
