# Task017A1 verification facts — in progress

Base afdcd9e. Four source/test paths only; canonical plan and administrative docs/evidence separate. Root development review complete with no remaining findings after raw canonical-order and attack cycle-hash fixes. Worker focused/shared20passed,0failed/skipped22.774s; six new Release facts plus existing seals/custody. Final worker evidence/pins pending.

Root unchanged contract oracle: `python3 -B docs/specs/verify-combat-reserve-release-v1.py` passed13literal cases/48side-slot traces,188cuts,2368mutations,840raw rejects,20timing and27boundary checks. Log `/tmp/task017a1-root-oracle.log`; process closed. This is complete historical contract evidence, not C#A1 transition coverage. RuntimeA1 proves44isolated base hashes, explicitly excludes4historical settled rows and claims zero event/state transitions.

Root full build/tests/Boundary/format, immutable-candidate CI and three fresh sequential independent reviews pending. Third independently rebuilds full solution. No acceptance yet.

19:20UTC worker handoff complete: post-format20passed/0failed/skipped26.284s; scoped format/diff clean, owned processes closed. Four source pins retained and checked. Root full gates starting; no further source edits planned. Whitespace/escaping/-0 regression added with fix, no separate RED claimed; first golden mismatch and attack-cycle regression have actual executed RED evidence.

Candidate77ef166f03674a7e5875acb8c1ebfde36b0d406a frozen/pushedPR138. Root full build0warnings/errors2.72s; full format exit0empty log; four pins pass. Full suite/Boundary still running. Review1 dispatched fresh clone;2/3notstarted. Root commands: `dotnet build Sandtable.slnx --no-restore -bl:/tmp/task017a1-root-build.binlog`; `dotnet test --solution Sandtable.slnx --no-build -bl:/tmp/task017a1-root-suite.binlog`; `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' -bl:/tmp/task017a1-root-boundary.binlog`; `dotnet format Sandtable.slnx --verify-no-changes --no-restore`. Logs use same stem.log; format log/tmp/task017a1-root-format.log.

19:25UTC review1 Ready; root read complete report/blind ledger and accepts no-findings verdict. Independent6Release/81Boundary, frozen oracle and diff passed; all processes closed. Invocation sandbox/zero-filter failures disclosed. Review2 dispatched fresh clone;3pending.

19:30UTC review2 Ready; root read full report/blind ledger and accepts no-findings verdict. Independent6Release/21shared-cycle/81Boundary, oracle/scopedformat/diff passed. Review3 fresh full-solution rebuild dispatched. No source remediation required; candidate remains77ef166.

19:31UTC root full gate complete:2,337passed/0failed/0skipped9m52.898s; Core9m52.643s,Runner3m35.820s,Contracts643ms. Boundary81/0/0passed9.705s. Build0warnings/errors2.72s; fullformat exit0. All root build/test/format sessions closed. Review1/2Ready,3active; exactCIverify pending.

19:34UTC exact-candidate CI all eight checks successful; verify106134206104 completed19:33:29UTC. Final review3 remains acceptance gate.

19:36UTC review3 Ready; root read complete report/preliminary and accepts no-findings verdict. Independent full rebuild0warnings/errors14.59s;6Release/40shared/81Boundary, oracle/scopedformat/diff passed. Four independently computed pins agree. All root and reviewer processes closed. Task017A1 accepted at77ef166 after full gates/threeReady/exactCI. No source remediation after freeze.
