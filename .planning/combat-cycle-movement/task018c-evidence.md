# Task018C verification

Base4f1b51c on codex/combat-guarded-repeat-finish, retained as user requested. Frozen3l one-move
released-I admission only; completion/expiry remains3m.

- Compiled RED: two NotImplementedException failures, `/tmp/task018c-red.log`.
- Initial GREEN: two passed, `/tmp/task018c-green.log`.
- Both exact golden traces passed, `/tmp/task018c-golden.log`.
- Expanded tests initially failed compilation because immutable UnitKey properties cannot be set
  with a record initializer; changed test construction to the existing validated constructor.
  `/tmp/task018c-expanded.log`; rerun `/tmp/task018c-expanded2.log`: eight passed, zero failures/skips,52.216s.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatInheritedReserveMovementTests' --filter-class '*CombatInheritedCycleControlTests' --filter-class '*CombatCycleMovementTests' --filter-class '*CombatCycleMovementRulesTests' /bl:/tmp/task018c-focused-{}.binlog`: 50 passed, zero failures/skips,1m23.368s; `/tmp/task018c-focused.log`.
- `dotnet build Sandtable.slnx --no-restore /bl:/tmp/task018c-build-{}.binlog`: zero warnings/errors; `/tmp/task018c-build.log`.
- `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/task018c-full-{}.binlog`: 2,457 passed, zero failures/skips,12m51.490s; exit0; `/tmp/task018c-full.log`.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0; `/tmp/task018c-format-verify.log`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/task018c-boundary-{}.binlog`: 81 passed, zero failures/skips,13.161s; `/tmp/task018c-boundary.log`.
- Four frozen direct oracles passed (details below).
- Independent review instance1of3: no actionable code/plan findings. Delivery conditional on full-suite success and format exit confirmation. Format/Boundary pipeline returned exit0 (session49705); full suite passed. All review delivery conditions fulfilled; frozen hashes unchanged. See task018c-review-report.md.

Exact old source APIs, World constructor, frozen schemas and fixtures unchanged. No public or
Snapshot activation. Unrelated Serena data and existing handoff excluded.

- `python3 docs/specs/verify-combat-inherited-reserve-movement-v1.py`: combat inherited Reserve Movement v1: 2 traces, 8 readbacks, 2 retries, 1392 mutations, 24 raw rejects, 11 boundary rejects
- `python3 docs/specs/verify-combat-inherited-cycle-control-v1.py`: combat inherited cycle control v1: 4 traces, 24 readbacks, 12 retries, 1648 mutations, 84 raw rejects, 14 boundary checks
- `python3 docs/specs/verify-combat-inherited-armed-continuation-v1.py`: combat inherited armed continuation v1: 2 proofs, 2 readbacks, 180 deep mutations, 8 raw variants, 18 boundary rejects
- `python3 docs/specs/verify-combat-ordinary-movement-v1.py`: PASS: 8 literal cases/both sides; 36 movement cuts, 486 mutations, 120 raw rejections; 630 source arithmetic coordinates, membership kernel and 14 atomic/overflow guards; contract-only synthetic repeat boundary.

Final diff check and plan local links passed. Local acceptance complete; no remote CI claim.
