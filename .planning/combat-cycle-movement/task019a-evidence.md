# Task019A verification
Basec9ca2e3 on codex/combat-cycle-movement-plan. Pure trusted-boundary assessment, no source admission.

TDD: initial build caught CA1822 stub property; /tmp/task019a-red2.log then compiled and failed2 tests at NotImplementedException. Initial implementation build corrected enum comparison; /tmp/task019a-green2.log passed2. Expanded test construction corrected read-only ContentHex mutation. /tmp/task019a-expanded2.log passed20/failed2: refusal-loss literal incorrectly expected no relationship; frozen Result2 fixture confirms Contact. /tmp/task019a-expanded3.log passed22 after correcting expectation.

Commands/results:
- Focused: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatContinuationTests' --filter-class '*CombatCycleMovementRulesTests' --filter-class '*CombatCycleMovementTests' --filter-class '*CombatReserveReleaseTests' /bl:/tmp/task019a-focused-{}.binlog`:62 passed,0 skipped; /tmp/task019a-focused.log.
- Build: `dotnet build Sandtable.slnx --no-restore /bl:/tmp/task019a-build-{}.binlog`:0 warnings/errors; /tmp/task019a-build.log.
- Full: `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/task019a-full-{}.binlog`:2,429 passed,0 failed,0 skipped in11m11.063s; /tmp/task019a-full.log.
- Frozen oracle: `python3 docs/specs/verify-combat-cycle-control-v1.py`:PASS19 literals/64 traces/164 cuts/1,748 mutations/700 raw rejects/43 boundaries/216 cost coordinates; /tmp/task019a-oracle.log.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`:exit0; /tmp/task019a-format-verify.log.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/task019a-boundary-{}.binlog`:81 passed,0 skipped; /tmp/task019a-boundary.log.
- `git diff --check`, changed-plan local links and frozen source/test hashes:passed.
- Independent review1of3:Ready, no actionable findings. Full-suite gate subsequently passed with reviewed source/test hashes unchanged. See task019a-review-report.md.

22 new cases include32 source-context iterations, bothowners/slots, immutable World/Release, canonical destinations, Contact/Engaged/none and CP/DP literals, ordinary ceiling, incremental overspend DP, persistent exclusions, distance2/far history, I/II exception ceilings/expiry/retention, malformed scope/member/proof/exception, armed capability hidden behind CP/exclusion and overflow. End-proof and modified Reserve/resource cases are explicitly isolated probes; no new full-history admission or historical golden parity claimed.
