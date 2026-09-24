# Task019C verification

Base5a8bc41, branch codex/combat-guarded-repeat-finish. Frozen3k inherited guarded repeat/finish only.

- RED: initial test analyzer fixes (CA1707/xUnit1026/CA1861), then four compiled NotImplementedException failures; `/tmp/task019c-red3.log`.
- Initial GREEN: corrected missing Content namespace and concrete JSON return types; four passed, `/tmp/task019c-green2.log`.
- Four exact golden traces passed; `/tmp/task019c-golden.log`.
- Expanded twelve cases passed, zero failures/skips,49.963s; `/tmp/task019c-expanded.log`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatInheritedCycleControlTests' --filter-class '*CombatArmedContinuationTests' --filter-class '*CombatInheritedReserveReleaseTests' /bl:/tmp/task019c-focused-{}.binlog`: 36 passed, zero failed/skipped, 1m00.374s; `/tmp/task019c-focused.log`.
- `dotnet build Sandtable.slnx --no-restore /bl:/tmp/task019c-build-{}.binlog`: zero warnings/errors; `/tmp/task019c-build.log`.
- `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/task019c-full-{}.binlog`: 2,449 passed, zero failures/skips,13m01.652s; exit0; `/tmp/task019c-full.log`.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0; `/tmp/task019c-format-verify.log`.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/task019c-boundary-{}.binlog`: 81 passed, zero failures/skips,14.932s; `/tmp/task019c-boundary.log`.
- `git diff --check`, plan local links and all four frozen source/project hashes passed.
- All four direct frozen Python oracles passed:
- Independent review instance1of3: no actionable findings; implementation/plan pass. Delivery verdict conditional on final full/format/Boundary gates. Full, format and Boundary gates passed; all reviewed hashes unchanged. Reviewer conditions fulfilled without source changes. See task019c-review-report.md.

No frozen schema/fixture or predecessor changes. No actual Movement execution or public activation.

- `python3 docs/specs/verify-combat-inherited-cycle-control-v1.py`: combat inherited cycle control v1: 4 traces, 24 readbacks, 12 retries, 1648 mutations, 84 raw rejects, 14 boundary checks
- `python3 docs/specs/verify-combat-inherited-armed-continuation-v1.py`: combat inherited armed continuation v1: 2 proofs, 2 readbacks, 180 deep mutations, 8 raw variants, 18 boundary rejects
- `python3 docs/specs/verify-combat-inherited-reserve-release-v1.py`: PASS inherited Reserve Release: 2 traces, 6 events, 8 cuts, 6 retries, 658 mutations, 24 raw rejections, 31 boundaries, 10 recovery paths, 7 source pins
- `python3 docs/specs/verify-combat-cycle-control-v1.py`: PASS: 19 literal cases; {'traces': 64, 'cuts': 164, 'mutations': 1748, 'rawRejects': 700, 'boundaryChecks': 43, 'costCoordinates': 216}. Private repeat/finish and Movement expiry; D2c inherited integration remains gated.
