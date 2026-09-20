# B1 verification and review evidence

Base `b67b4f9`, branch `codex/combat-task008-opening-preamble`, stacked on A2 PR125.

## Scope and research

Next bounded opening-preamble adapter only, four transitions to Weather entry/state5.
Canonical `combat-opening-preamble-v1.md` requires entire trusted creation chain; Weather then consumes
all four events; `combat-stage-entry-v1.md` requires completed Weather before its four transitions.
Therefore Task008 B parent splits B1→C→B2, then D. No wire bytes or requirement weakened.

Read-only explorer confirmed exact dependencies and five-primary-file shape. Source guidance:
opening spec77/101, Weather65/80, stage-entry53/85. A cached/synthetic Weather state is not a valid
runtime predecessor. B1 keeps fixed selected Setup/config/Content profile and accepts campaign ID
and unsigned seed as defined; no broad admission of all C2-valid variants.

## Baseline

`PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-opening-preamble-v1.py`: exit0;
six creation-rooted traces,30 cuts,1,050 event mutations,3,348 state mutations,192 raw rejections,
204 boundary/retry checks. Contract-only baseline, not yet C# replay evidence.

## Implementation and review

Worker TDD, integration and dev review complete. Independent rounds tracked below. Fourth round only after
unresolved blockers in round3 plus one bounded deep research/experiment, per user policy.

Worker GREEN12 tests;60 frozen fingerprints/30cuts. Dev review no remaining finding.
Build: `dotnet build Sandtable.slnx --no-restore` with /tmp/b1-build-*.binlog; zero warnings/errors.
Full suite `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/b1-suite-{}.binlog'`:
1,886 passed,0 failed,0 skipped;3m15s313ms. /tmp/b1-suite.log.
Format `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: exit0; /tmp/b1-format.log.
First format attempt reported whitespace-only issues; scoped formatter corrected those before
successful final check. No semantic code changes after tested build.
Added-line local doc target/anchor check:6 passed. `git diff --check`: pass.
Source fingerprints frozen in b1-source.sha256 and verified.

## Independent rounds

1. Ready; no actionable findings. Independently focused12/12 and oracle pass. Report retained.
2. Ready; no actionable findings. Independently focused12/12, oracle and fingerprints pass.
3. Ready; no actionable findings. Independently focused12/12, oracle and fingerprints pass.

All three rounds accepted; no conditional experiment/fourth review required.
