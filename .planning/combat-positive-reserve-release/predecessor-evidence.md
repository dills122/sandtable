# Held-I no-move predecessor acceptance — 2026-09-22

Branch codex/combat-positive-reserve-release; base7d6916170ec6fed61309f366addc52ab236d91f3.
Primary manifest:2 new Core files,1 focused test file, fixture link in Core.Tests.csproj and canonical
Combat plan. Administrative publication includes README, roadmap, tech design, naming and this
folder's retained review/next-slice artifacts. Original main and untracked September20 handoff untouched.

## Acceptance and limits

Dormant3h prerequisite implemented and locally accepted: actual creation→first opening→ten-event
held-I no-move/no-attack traversal through authority12→22. Both owners match20 event hashes and22
Control hashes/lengths,2 opening bases and2 predecessor digest lists. Every cut and retry retains
World,CP,ammo,TOE,Cohesion,designation,Weather,RNG and ordinal1. Terminal is same-slot Reserve
Release with held-I intact. Public gameplay, positive3i Release, later-II/consumed actual lineage,
HistoryReplay/Snapshot/publication and parent017 remain open. Next exact bridge manifest: bridge-plan.md.

## Verification

- Meaningful compiling RED:2 tests failed at missing direct Movement completion; /tmp/reserve-cycle-red.log.
- Focused7/7,0failed/0skipped23.541s: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatInheritedReserveCycleTests' /bl:/tmp/reserve-cycle-verified-{}.binlog.
- Full solution restore/build: dotnet restore Sandtable.slnx /bl:/tmp/reserve-cycle-restore-{}.binlog;
  dotnet build Sandtable.slnx --no-restore /bl:/tmp/reserve-cycle-restored-build-{}.binlog.
  Build0warnings/0errors2.99s. Initial --no-restore failed because fresh worktree lacked other projects'
  assets; full restore corrected environment. /tmp/reserve-cycle-build.log.
- Full regression: dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/reserve-cycle-full-{}.binlog.
  2,361passed/0failed/0skipped10m24.839s; /tmp/reserve-cycle-full.log.
- Review then strengthened existing weather test with non-Normal witness assertion. Production source
  unchanged. Full-suite binaries had original test; final strengthened test independently passed1/1
  1.889s using --filter-method '*MissingDesignationForeignHistoryAndUnsupportedWeatherNeverAdmit'
  /p:ArtifactsPath=/tmp/reserve-weather-reviewfix /bl:/tmp/reserve-weather-reviewfix-{}.binlog.
  Separate outputs avoided interference with in-flight full regression.
- Boundary=UserSpace:81passed/0failed/0skipped9.933s. dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' --results-directory /tmp/reserve-cycle-boundary-results /bl:/tmp/reserve-cycle-boundary-local-{}.binlog.
  Initial alternate-/tmp-output boundary run failed8 repository-root lookup tests (73passed); existing
  manifest tests locate repo by walking from AppContext.BaseDirectory. Correct repo-local run passed.
- dotnet format Sandtable.slnx --verify-no-changes --no-restore passed on final source.
- python3 -B docs/specs/verify-combat-inherited-reserve-cycle-v1.py passed:2traces/20events/22cuts/
  20retries/1103mutations/46raw/196boundaries/13pins. No fixture generation or frozen changes.
- Five changed Markdown summaries:675local targets/0missing; anchors not checked. Lychee unavailable.
- git diff --check passed. No dependency, ordinary Movement or frozen contract changes.

## Review

Fresh independent review1of3: Ready with non-blocking follow-up. Root accepted and fixed weather
nonvacuity suggestion; targeted test passed. No production correction or new review required.
Reviewer independently ran7focused tests and frozen oracle; verified original source pins and
recorded preliminary findings before author rationale. See predecessor-review1.md and original
predecessor-review1-source.sha256. Final hashes in predecessor-source.sha256 include test-only fix.
Five-axis root review agrees: explicit bounded admission, full replay, owned arrays, exact canonical
parity, preserved authority boundaries and unchanged ordinary paths. No actionable issue remains.

GitHub checks tracked on draft PR after publication; local acceptance does not claim CI success.
