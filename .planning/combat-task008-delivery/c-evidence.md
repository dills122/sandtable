# C Weather verification and review evidence

Base22f8da6, branch codex/combat-task008-weather, stacked on B1 PR126.

## Research and baseline
Read-only scope established fiveprimaryfiles; canonical combat-weather-v1.md/schema/fixture/verifier.
Use actual accepted B1 four-event replay; no supplied state as authority. Existing Cna1979Weather,
SandtableRandom, WeatherEventFactory sources and nested Weather1 codec retained unchanged.
Fixed artifact hash and Rules10/source provenance required. Both orders retain Axis determining side.
State6 Organization entry; all outcomes retained and RNG advanced only once; no immediate subjects.

`PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-weather-v1.py`: PASS34 creation-rooted
traces,68 replay/state cuts,11516leaf mutations,921raw rejections,1651boundary/retry checks,
3960Rules coordinates. /tmp/c-weather-baseline.log. Oracle only, not C# parity.

## Implementation and review
Worker RED missing implementation observed; GREEN42 passed after scoped whitespace formatting:
`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatWeatherTests' '-bl:/tmp/c-weather-green-{}.binlog'`.
Final binlogs /tmp/c-weather-green-20260919-215942--53137--p51Xzc.binlog and
/tmp/c-weather-green-20260919-215945--53137--NFqljG-dotnet-test.binlog.
34chains/68cuts/306frozen fingerprints, independently hashed RNG block/rejectedbytes, coherent
forged Weather and cache, raw canonical negatives, Snapshot12/oldreader rejection all pass.
Lead dev review complete, no remaining actionable findings. Build `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/c-build-{}.binlog'`: exit0,
zero warnings/errors; /tmp/c-build.log.
Format `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0; /tmp/c-format.log.
Full suite `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/c-suite-{}.binlog'`:1,928 passed,0 failed/skipped;3m14s594ms. /tmp/c-suite.log.
Source fingerprints in c-source.sha256. Three sequential independent rounds pending.
User's conditional final experiment/review applies only if blockers remain after round3.

Added-line local doc targets/anchors:1 pass. `git diff --check`: pass.

## Independent rounds
1. Ready; no actionable findings; independent focused42, oracle and source fingerprints pass.
2. Ready; no actionable findings; independent focused42, oracle and fingerprints pass.
3. Ready; no actionable findings; independent focused42, oracle and fingerprints pass.

All three rounds accepted. No conditional experiment/fourth review required.
