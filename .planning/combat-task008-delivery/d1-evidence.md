# D1 Reserve designation verification and review evidence

Base3ded1eb, branch codex/combat-task008-reserve-designation, stacked on B2 PR128.

## Scope and research
See d-scope.md. D1 accepts only full real stage-entry state10 and optional own none→I designation.
D2 owns completioncodec including cycleidentity;019A applies sameevent atomically. D1 precompletion
scope cannot prove terminalReserve/Movement or full008. InitialWorldcodec invariant remains intact.

Baseline `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-reserve-designation-v1.py`:
16traces40cuts2342leaf733raw1578boundary4frozenkernel14pins pass; /tmp/d-reserve-baseline.log.
This includes futurecompletion contract evidence, not D1 C# implementation acceptance.

## Implementation and review
Worker RED missingtypes; first runtime parity caught legacy reserve-i instead of frozen I. Fixed
only bounded Reserve writer. Finalfocused23/23 pass after scopedwhitespace, log/tmp/d1-final.log;
binlogs /tmp/d1-final-20260919-224353--57977--N0fhNi.binlog and
/tmp/d1-final-20260919-224355--57977--ZuAocx-dotnet-test.binlog.
Command `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d1-final-{}.binlog'`.
16fixture rows/24precompletioncuts/88fingerprints;8designationevents. TypedforgedWorld checks,
inputmutationApply correction, limits/completiontag checks added in dev review. No remaining source
findings. Source hashes d1-source.sha256. Build `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/d1-build-{}.binlog'` exit0, zero warnings/errors, /tmp/d1-build.log. Format `dotnet format Sandtable.slnx --verify-no-changes --no-restore` exit0,/tmp/d1-format.log. Fullsuite `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/d1-suite-{}.binlog'`:1,968 passed,0 failed/skipped;3m10s456ms,/tmp/d1-suite.log.
Three sequential independent rounds pending.
Conditional one experiment+finalreview only if blockers remain after round3.

Added-line local doc targets/anchors:4 passed.

## Independent rounds
1. Ready; no actionable findings; source/planreview, fivefingerprints and retainedfocused23/oracle inspected.
2. Ready; no actionable findings; independentfocused23, fivefilefingerprints+14canonicalpins pass.
3. Ready; no actionable findings; independentfocused23/sourcefingerprints+retainedfullsuite confirmed.

All three rounds accepted; no conditional experiment/finalfourthreview required.
