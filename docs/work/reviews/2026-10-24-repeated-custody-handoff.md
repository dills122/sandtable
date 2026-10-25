# Repeated captured-lot Custody expiry handoff

Status: focused validation passed; no production defect exposed. Base
`753d65c27f38863e7aabe9504d13f11cf35fa9e1`; branch
`codex/rc-overnight-plan-20261023`; [PR329](https://github.com/dills122/sandtable/pull/329).
Ruflo task `task-1792889655752-sydue5`.

## Required distinction

A real repeated paid assault must produce a captured lot and a new Custody round
bound to that settlement. Existing seed5 continuation has an initial lot only;
it remains useful neighboring evidence but cannot close repeated expiry.
No fabricated ordinal, lot, native state, or public support claim.

## Fixture selection

Existing fixed seeds5/6/12345 do not establish second captured lot. Capture depends
on ordered dice outcomes and morale differential, including conditional ninth
capture-share draw. Bounded offline source-RNG/table calculation over first64
seeds selected first eligible deterministic candidate; full accepted campaign
reproduced its paid draws and actual captured lot. No authoritative
state/lot/RNG injection. This is regression branch coverage, not a public profile
certificate or evidence for untested seeds.

Candidate selected: seed50, first eligible0..63 with normal fall weather21.
Prediction: initial paid cursor2→10, dice[1,6,4,5,2,2,4,4], no capture; repeated
paid cursor10→19, dice[2,5,5,2,3,4,1,1], differential0, defender capture with
share die4 (50%). Actual accepted history reproduced these dice/cursors and produced one captured
TOE point in the second settlement.

## Observed behavior

Two actual paid assaults and one accepted Repeat/Movement/Breakdown sequence
produce the repeated lot: captor `axis-infantry-second`, victim
`commonwealth-infantry-west`, quantity1. No initial lot exists for this fixture.
System timeout binds exact second settlement hash and derived lot ID.

Saved-copy reopen precedes any Custody owner query. Deadline−1 preserves accepted
history and original entry/deadline; exact expiry records one leave-unguarded
System event. Explicit custody-state hash proves escaped status, no guard,
replacement entitlement for game turn5/stage1 with training pending. Settled World
hash remains unchanged (no guard TOE debit); owner action rows unchanged, zero
owner Custody receipts, two paid result/Force-completion records retained.
Successor retains actual RNG cursor19 and expected World/Custody state hashes.

Two variants: normal expiry and injected Cycle-row insertion failure. Failed
successor must preserve campaign, accepted history, Custody/Reserve/Cycle rows
and owner actions; remove trigger and retry successfully. Duplicate scheduler
sweep appends nothing; cold reconstruction verifies. This is same-process saved
recovery, not a fresh-process Custody or power-loss proof.

## Validation

All 13 focused cases passed. Final Host test-project build has zero warnings/errors;
solution format passed. Only new test file added; no production or shared fixture
changes. Exact commands/wall times below come from retained exit receipts under
`artifacts/rc-repeated-custody/` (local ignored artifacts, not Git-tracked). The
commit containing this handoff carries validated source; `source-manifest.json`
retains its SHA256 hash.

| Check | Command | Result | Wall time | Receipt |
| --- | --- | --- | --- | --- |
| Repeated captured-lot expiry/rollback | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*RepeatedCustodyTimeout*'` | 2/2, exit0 | 21.097s | `focused-receipt.json` |
| Existing Custody regressions | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*.CustodyTimeout*'` | 11/11, exit0 | 37.660s | `custody-neighbor-receipt.json` |
| Host test-project build | `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore` | exit0; 0 warnings/errors | 3.816s | `build-receipt.json` |
| Solution format | `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | exit0 | 30.171s | `format-receipt.json` |

Initial focused attempt: normal expiry passed; rollback row failed because test
injected a Reserve insert although this history has empty Reserve and creates
Cycle directly. Retained `initial-focused.log` and receipt. Corrected trigger to
actual Cycle insertion, rebuilt and reran both cases green. This was a test
fixture error, not a runtime defect. Settlement binding hashes exact retained
canonical UTF8 bytes.

Bounded selection script `discover_fixture.py` and full candidate table
`discovery.json` retained locally. Search bound0..63 and normal-fall-weather
filter were fixed before calculation; seed50 is first eligible result. This is
native regression branch generation, not certification for other seeds/profiles.
Test itself runs fixed seed50 and asserts actual captured settlement/dice/cursors;
CI does not perform a discovery search.

Not run: full Core/Host/Runner, boundary/contracts, Python source oracles or
frontend suites. No full-suite pass inferred; original source-pin failures remain
unfixed. Exact-head partitioned CI and independent review remain release gates.
All local test/child processes and native workers completed; worktree source
unchanged after final passing checks.

## Review

Coordinator checked actual repeated paid history, distinct second-settlement/lot
binding, source escaped-state/replacement hash, unchanged World (no guard debit),
no owner Custody receipt, successor cursor19, exact deadline and rollback table
equality. No material source finding. No new independent review claimed; prior
packet allowance exhausted. Graph coverage verified new/shared test paths and
V36 source. Exercises.cs partial line147 read directly alongside relied methods.

## Open gates

Exact-entry Custody clock faults, actual repeated required Retreat expiry, remaining
fresh-process cuts/crash faults, original source-pin reconciliation, dependencies,
public certificates and six-turn playable product gates remain separate unless
directly proved. Original inventory44 provisional positive/256 absent/0 admitted.
Coordinator review does not replace independent release review. PR stays draft;
exact-head CI required before merge. No merge performed.

## Next bounded task

Genuine repeated required-Retreat timeout, followed by exact Custody/Retreat-entry
clock failure injection. Keep actual accepted histories, focused checks and
separate proof claims. Fresh-process Custody and abrupt crash recovery remain
open; this saved-copy packet does not close them.
