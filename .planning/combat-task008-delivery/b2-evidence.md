# B2 stage-entry verification and review evidence

Basea0babdb, branch codex/combat-task008-stage-entry, stacked on C PR127.

## Research and baseline
Canonical combat-stage-entry-v1.md/schema/fixture/verifier already complete. Only prerequisite was
accepted C full Weather replay, now retained. Five primary files; reuse existing StageEntry policy
and source helpers without modifying legacy. Check complete4explicit-none gates before predecessor
replay. Require one accepted Weather from full creation/opening chain; preserve all outcomes/cursor,
World/order/holder, receipts5→9. Commonwealth fleet positions retain activeSide; final Reserve
position remains first-acting-side/null, including ActLast. No synthetic cached state authority.

`PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-stage-entry-v1.py`: PASS12 traces,
60cuts,11052leaf mutations,1299raw rejections,3552boundary/retry checks. /tmp/b2-stage-baseline.log.
Contract baseline only; no C# parity claim yet.

## Implementation and reviews
Worker RED missing implementation CS0246; GREEN17 passed after scoped whitespace:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatStageEntryTests' '-bl:/tmp/b2-green-{}.binlog'`.
Final binlogs /tmp/b2-green-20260919-222038--55543--YgSP4Y.binlog and
/tmp/b2-green-20260919-222041--55543--KN+3Sq-dotnet-test.binlog.
12traces/60cuts/228fingerprints, fiveboundarygroups. Lead fixed receipt-mutation test flaw viaworker;
no remaining source findings after devreview. Build `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/b2-build-{}.binlog'`: exit0,
zero warnings/errors; /tmp/b2-build.log. Format `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0; /tmp/b2-format.log.
Full suite `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/b2-suite-{}.binlog'`:1,945 passed,0 failed/skipped,3m16s791ms; /tmp/b2-suite.log; hashes b2-source.sha256.
Three sequential independent rounds pending.
Conditional one experiment/final review only if round3 retains blockers, per user policy.

## Independent rounds
1. Ready; no actionable findings; independent focused17 and complete oracle pass; fingerprints match.
2. Ready; no actionable findings; independent focused17, fingerprints and retained oracle/source review.
3. Ready; no actionable findings; independent focused17, fiveprimary+12canonical fingerprints and retainedoracle review.

All three rounds accepted. No conditional experiment/fourthreview required.

Added-line local doc targets/anchors:1 pass; diff check pass.
