# A2 verification and review evidence

Base `a8eed35`, branch `codex/combat-task008-creation-snapshot`, stacked on A1c PR124.
Scope: creation-only Snapshot12 codec/readback and explicit publication-evidence ownership.

## Contract and planning evidence

`python3 docs/specs/verify-combat-authority-envelope-v1.py` after ownership documentation edits:
exit0, four canonical goldens,67 mutations,36 raw negatives,nine recovery boundaries,12 identity
forks,two turn boundaries,693 nested type rejections. No frozen bytes changed.

Existing HOST-RSH-001 and canonical C2/D1 contracts provide evidence; no provider spike needed.
Chose to preserve Task008 actual-persistence obligation explicitly open under HOST-PUB-001 rather
than relabel pure codecs or memory CAS as proof. Core/Host own real-provider failure matrix after
020–021 and verified023, before production hosting/durable-save acceptance. Checkpoint D/H remain
in-process Core dependency gates. Task025 must retain outstanding publication obligation.

## Runtime checks

Worker RED: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class
'*CombatCreationSnapshotTests' --no-restore '-bl:/tmp/a2-red-{}.binlog'`: expected missing snapshot
implementation CS0103. Initial GREEN compile exposed CA1822 constant-getter analyzers; initialized
readonly properties fixed this without suppression.

Worker GREEN: same command with `'-bl:/tmp/a2-green-{}.binlog'`:34 passed, zero failed/skipped.
Successful binlogs `/tmp/a2-green-20260919-211503--44384--1FxxzM.binlog` and
`/tmp/a2-green-20260919-211508--44384--R61Kc4-dotnet-test.binlog`.

Lead `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: pass, empty `/tmp/a2-format.log`.
Lead `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/a2-build-{}.binlog'`: pass, zero warnings/errors.
Lead `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/a2-suite-{}.binlog'`:1,874 passed,
zero failed/skipped,3m05s923ms. Completed during round2. Added-line local Markdown targets (7)
and new runtime-evidence anchors checked; `git diff --check` passed.

## Dev review

Three primary files, all new/internal. Dedicated creation-only type exposes immutable trusted
Created11 projection; bounded private evidence copy is validated before event hash and domain/NUL/
BE64-length prefix hashing. No array retained; no caller input can mutate completed snapshot.
Entire derived snapshot compared against raw import, including mandatory empty/null fields;
noninitial values reject. Receipt itself is not independently accepted as authority.

Golden and independent prefix tests plus25 frozen negative vectors cover production code. Closed-
admission test demonstrates pure cut rejects fresh creation while retained evidence still reads;
not a host/registry-disable experiment. Three seed boundaries and paired foreign-event/request
substitutions guard rehash laundering. Buffer mutation isolation tested.

No remaining actionable dev finding. Publication docs preserve actual-persistence requirement and
name exact unproved matrix/owner; no storage implementation or changed frozen bytes. Independent
review must assess that allocation against existing plan, not assume full Task008 completion.

## Independent review ledger

Three sequential fresh-context rounds required for A2. Round4 only if blockers remain after three,
following one bounded deep research/experiment. Round1 Ready/no actionable findings; lead accepts.
Rounds2 and3 Ready/no findings; lead accepts all three. Each ran34 focused tests; C2/D1 oracle
checks retained in reports. No round4 required. A2 accepted; source hashes in `a2-source.sha256`.
Post-review documentation updates reconcile completion status only; code/tests unchanged.
