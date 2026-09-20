# F2 participant lifecycle evidence

Review target: branch codex/combat-task008-reaction-lifecycle, base32e4e6d/F1 PR135.
Final status: accepted after dev review and all three independent rounds Ready; chronology below retains earlier pending states.
Scope f2-scope.md and f2-dispatch.md. Two traces/eight events/ten cuts/28artifacts.
Canonical baseline: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-lifecycle-v1.py
exit0 /tmp/f2-reaction-baseline.log:2traces8events10cuts8retries754mutations72raw76boundaries16pins.
Worker TDD/implementation active; dev/integration/three independent reviews pending.

TDD RED /tmp/f2-red.log: expected missing F2 types. Initial golden tests reconstruct actual F1 and all four successors.
Five-file feasibility confirmed by implementation worker; F1 source remains untouched.

First green /tmp/f2-green.log: two owner theories passed all28artifacts/everycut/terminalretry.
Root models/projector/codec dev read no blocker; final adversarial tests and freeze pending.
F1 PR135 remote checks all pass.

Expanded /tmp/f2-expanded.log:9cases,6pass/3fail. Root diagnosis sent worker: track records
wrap new collections after full replay, so compare structural route content; valid-shaped changed
input source atoms must be rejected at Apply authority boundary, not necessarily shape-only codec.
Await worker confirmation/fix/final run; no implementation defect established by these failures.

Worker confirmed test-harness diagnosis; no production changes. Mutation-only retest
/tmp/f2-mutation-fix.log exit0 one test. Final all-actor/cross-kind and alteredcreation/trigger
negatives inspected; root dev review complete, no remaining findings. Final focused run pending.

Frozen final focused16/16 (F2nine+F1seven),0failed/skipped,20.058s; /tmp/f2-final-focused.log.
Command: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class 'Cna.Core.Tests.Campaigns.CombatReactionLifecycleTests' 'Cna.Core.Tests.Campaigns.CombatReactionTriggerTests' '-bl:/tmp/f2-final-focused-{}.binlog'.
Scoped format initially hit sandbox BuildHost pipe denial /tmp/f2-format.log; approved escalation
passed /tmp/f2-format-escalated.log. git diff --check pass. No worker processes remain.
Root full integration and three independent reviews pending.

Root build exit0,0warnings/errors /tmp/f2-build.log. Full suite /tmp/f2-suite.log and fullformat /tmp/f2-format-full.log running. Independent round1 launched against frozen hashes.

Full format first exit2: IMPORTS ordering in test using directives only. Root swapped Content/Observations imports and refreshed test hash; no semantic code change. Reverification pending.

Full format recheck exit0 /tmp/f2-format-full-final.log; imports-only correction complete.

Independent round1 Ready with no findings. Narrow retrieval avoided author/priorreview narrative
before preliminary ledger; refreshed imports-only test hash verified. Round2 active, round3 pending.

Root fullsuite exit0 /tmp/f2-suite.log:2,060passed,0failed/skipped,3m33s132ms.
Exact command: dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f2-suite-{}.binlog'.
Build: dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f2-build-{}.binlog'.
Format: dotnet format Sandtable.slnx --verify-no-changes --no-restore (final exit0 log above).
Added-line local links and git diff --check pass. Imports-only source change does not alter compiled
logic tested by fullsuite; no semantic source edits since worker final focused build.

Closeout: rounds2 and3 Ready, no actionable findings. Lead read all reports and accepted their
code/plan reconciliation. Five frozen source hashes pass. Merge repair put exact F1 tree on main
`dd22088`; F2 publication uses main as base with identical source tree, excluding old stack history.
Retained full2060/focused16, zero-warning build and final format suffice: no semantic changes since
verification, only acceptance/merge-status documentation. F3 may proceed; F/H parent remains open.
