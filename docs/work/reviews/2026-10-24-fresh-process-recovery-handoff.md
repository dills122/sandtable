# Native fresh-process recovery handoff

Status: local focused validation passed; no production defect exposed. Base
`72775390f6cf2014dccf4713834141fe55d3acca`; branch
`codex/rc-overnight-plan-20261023`; [PR329](https://github.com/dills122/sandtable/pull/329).
Ruflo task `task-1792887938731-phxmmz`.

## Intended proof

Four cuts from genuine accepted history: initial required Retreat and repeated
Selection, RBA and one-seal Force. A child opens the saved native journal using a
controlled clock and invokes the background scheduler without owner queries.
Before expiry, at expiry and duplicate retry use separate process launches.
Exact logical journal/replay/contact parity is compared with an identical
in-process baseline. Child exit alone is not sufficient evidence.

Existing private P01 probe readback remains a separate default child mode. This
packet uses explicit native mode; no private P01 probe evidence is substituted
for native campaign recovery or original-root certification.

## Exact cut matrix

| Saved cut | Genuine history before cut | Due outcome required |
| --- | --- | --- |
| Initial required Retreat | Initial paid assault, fixed seed 6, checkpoint 22 | Exactly one native Retreat timeout |
| Repeated Selection | Initial Retreat settled, Reserve released, Cycle Repeat and repeat Movement accepted | Exactly one native Selection timeout |
| Repeated RBA | Repeated Selection accepts actual west assault offer | Exactly one native RBA cancellation |
| Repeated one-seal Force | Defender declines RBA; attacker Force seal retained | Exactly one native Force cancellation |

Each case launches children at persisted deadline minus 1ms, exact deadline, and
same-time retry. Before expiry: no new accepted step and entry/deadline unchanged;
normal clock high-water updates are compared against control. At expiry and retry:
compare all accepted history, owner records, credentials/keys, seven deadline
lanes, replay hashes/bytes and contacts. These comparisons use shared baseline
identities, not separately generated campaigns or physical SQLite file bytes.

## Validation

All nine focused tests passed. Four new cases launch 12 native children; existing
private compatibility test launches 2 legacy children. No production source changed.
Final Host test-project build: zero warnings/errors. Full solution format passed.

Exact commands and wall times below come from retained exit receipts under
`artifacts/rc-fresh-process/` (local ignored artifacts, not Git-tracked). The commit
containing this handoff carries the validated source. `source-manifest.json`
retains SHA256 hashes of changed test sources.

| Check | Command | Result | Wall time | Receipt |
| --- | --- | --- | --- | --- |
| New process recovery | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*FreshProcessNativeDeadlineRecovery*'` | 4/4, exit0 | 65.158s | `focused-receipt.json` |
| Legacy child compatibility | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*ChildProcessReadsCommittedProbeAfterLostResponse*'` | 1/1, exit0 | 2.655s | `legacy-p01-child-receipt.json` |
| Saved-copy integration | `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore -- --filter-method '*IntegratedCampaignDurableSaveResumeMatchesUninterruptedAuthority*'` | 4/4, exit0 | 82.063s | `integrated-neighbor-receipt.json` |
| Final Host build | `dotnet build tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-restore` | exit0; 0 warnings/errors | 3.406s | `build-final-receipt.json` |
| Solution format | `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | exit0 | 34.552s | `format-receipt.json` |

Initial format check exit2 reported import ordering in child Program only; fixed
and rechecked green. Original failure retained in `format-initial.log` and
`format-initial-receipt.json`. Tests ran before that import-only reorder; final
source rebuilt successfully. No behavior changed after passing tests.

Not run for this test-only packet: full Core/Host/Runner, boundary/contract suites,
Python oracles or frontend suites. Historical passes do not certify this head;
release-head partitioned CI remains required. Original source-pin failures remain
unfixed. No running local checks or child processes remain after validation.

## Review and limits

Coordinator reviewed child mode and four-cut assertions: child opens disk state,
verifies before/after, emits its own process identity and replay hash, and parent
requires exactly one expected timeout plus full logical parity. Final validation passed.
No new independent review is claimed; prior packet allowance was exhausted. Exact-head CI and independent release review remain
required before merge. No merge authorized.

Creator test process remains alive, but child receives no journal/session objects;
its input contains only path, campaign ID and controlled UTC. This proves a new
process can recover independently from saved state. It does not prove terminating
the creator at an arbitrary transaction boundary.

This is graceful saved-journal recovery in a new process, not abrupt process-kill
or power-loss transaction proof. Four cuts do not prove every deadline lane.
Third-cycle Reserve, repeated captured-lot Custody/required Retreat, exact
Custody/Retreat entry clock faults, source-pin failures, dependency remediation,
public certificates and six-turn playable RC gates remain open. Original ancestry
inventory remains 44 provisional positive/256 absent/0 admitted.

## Handoff and next bounded task

Both code additions are test harness changes: explicit native child mode and
four recovery cases. Existing default private-probe mode remains compatible.
Fresh-process proof gate closes only these four cuts. PR329 remains draft;
exact pushed head and CI disposition are reported with the PR update, not inferred
from these local checks. No merge performed.

Next concrete integrated gap: genuine third-cycle Host Reserve timeout with
convert-I/retain-II effects, or evidence that this cut is unreachable within the
declared profile. Repeated captured-lot Custody/required Retreat and entry clock
fault cuts remain separate. Use focused suites and retain real accepted history.
