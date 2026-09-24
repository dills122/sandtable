# Task018B verification
Base0dfea4d; branch codex/combat-cycle-movement-plan. Primary scope/hashes: task018b-review-bootstrap.md.

- TDD: /tmp/task018b-red.log compiled2 failing successor stubs; /tmp/task018b-projection.log passed2. /tmp/task018b-event-red.log passed2/failed2 native stubs. /tmp/task018b-green.log caught tagged cycle-hash validation defect; fixed dedicated SHA256 validation, /tmp/task018b-green2.log passed4. /tmp/task018b-expanded.log passed10.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatCycleMovementTests' --filter-class '*CombatCycleMovementRulesTests' --filter-class '*CombatWorldTests' --filter-class '*CombatResultReleaseTests' /bl:/tmp/task018b-focused-{}.binlog`:66 passed,0 skipped (/tmp/task018b-focused.log).
- `dotnet build Sandtable.slnx --no-restore /bl:/tmp/task018b-build-{}.binlog`:0 warnings/errors (/tmp/task018b-build.log).
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`:exit0 (/tmp/task018b-format-verify.log).
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/task018b-boundary-{}.binlog`:81 passed,0 skipped (/tmp/task018b-boundary.log).
- `python3 docs/specs/verify-combat-ordinary-movement-v1.py`:PASS8 literal cases/both sides,36 cuts,486 mutations,120 raw rejects,630 source arithmetic coordinates,membership kernel,14 atomic/overflow guards. Frozen contract-only synthetic boundary; not native historical parity evidence.
- `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/task018b-full-{}.binlog`:2,407 passed,0 failed,0 skipped in10m27.773s (/tmp/task018b-full.log).
- Independent review instance1/3:Ready with non-blocking follow-ups, no actionable findings. Full-suite condition satisfied; reviewed source/test SHA256 unchanged. See task018b-review-report.md.

New10 cases loop over32 authenticated current Result2 source contexts (both owners and seal orders). Independent complete World comparison protects unchanged resources; tests check3 cumulative moves, ended relation not recharged,CP15 limit,all replay cuts and original-byte retries, altered/re-signed events, base/state/cache mutations, stale/foreign/missing/capacity histories, occupied destinations and generic World constructor still strict. No public gameplay/actual repeat admission claim. Parent017–019 open.

Final `git diff --check` passed; changed plan local links resolve. Source/test hashes match reviewed freeze. Local commit only; public activation and parent017–019 stay open.
