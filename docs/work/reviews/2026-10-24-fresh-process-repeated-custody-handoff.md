# Fresh-process repeated Custody recovery

Base `17050b1312a0566bdbaa76a51b31a24be69a3bd1`, branch
`codex/rc-fresh-process-third-reserve` (stacked after Reserve proof).
Issue330; Ruflo task `task-1792898682845-mmlwyj`, namespace
`sandtable-rc-20261023`.

## Plan and evidence boundary

Use existing seed50 accepted history: first paid assault, owner Reserve release,
accepted Cycle repeat and Movement, west assault selection, RBA decline and second
Force completion. Second settlement yields actual one-TOE captured lot. No native
state/RNG injection or runtime seed search. Reuse existing final-seal fixture,
complete normally and save pending Custody before any owner Custody query.

Launch existing native recovery child three times at deadline−1ms, exact deadline
and retry. Each separate OS process opens disk authority and verifies before/after
scheduler replay; only path/campaign/time passed, no parent objects or credentials.
Compare all14 logical journal tables plus replay bytes/hashes against uninterrupted
control at every boundary. Assert exact original opened/deadline timestamps,
System leave-unguarded event tied to second settlement/lot, explicit escaped state
with no guard and delayed replacement (turn5/stage1/training pending), unchanged
settled World/owner records/paid-result bytes and successor RNG19. Retry adds no
record. Successor opens Cycle normally.

## Scope and review

One isolated new partial test file, FreshProcessCustody.cs. No production, schema,
child protocol or shared fixture changes. Existing graph paths clean metadata;
new file initially not tracked, direct source used. CCE unavailable; Ruflo retains
decision. Coordinator review only; earlier PR329 review is not new-test acceptance.

## Validation

All18 focused tests passed, no failed attempts. Logs and JSON command/exit/time
receipts in local ignored artifacts/rc-fresh-process-custody/.

| Check | Result | Wall seconds |
| --- | --- | --- |
| Host Release build | exit0 | 19.495 |
| *FreshProcessRepeatedCustody* | 1/1 exit0, three child launches | 35.460 |
| *RepeatedCustodyTimeout* | 2/2 exit0 | 20.777 |
| *.CustodyTimeout* | 11/11 exit0 | 31.074 |
| *FreshProcessNativeDeadline* | 4/4 exit0 | 56.997 |
| Solution format | exit0 | 40.857 |

Build: `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore`.
Tests: `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '<selector>'`.
Format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`.
No full Core/Host/Runner, boundary, contracts, Python oracles or frontend rerun
for test-only change. No runtime defect exposed. Coordinator checked source
assertions and retained source hash. No active test processes remain. Local
commit only; new packet has not run CI or received independent review.

## Limits and next bounded work

Parent process remains alive; not an abrupt-crash or power-loss proof. Bounded
seed50 captured-lot profile does not admit other lots, routes or public roots.
Fresh-process genuine repeated required-Retreat remains next recovery cut.
Other Reserve cuts, crashes, source pins, public certificates, dependencies,
capacity, schema migration and playable product gates remain tracked in issue330.
