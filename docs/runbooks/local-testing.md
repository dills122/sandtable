# Focused local testing

Use the smallest relevant tests during development, then affected project suites
and boundary checks before review. CI retains exhaustive row coverage; local
selection is not a claim that every affected test was discovered automatically.

`just check` remains the explicit full local .NET gate exactly as it always
has: format, build, boundary tests, then `dotnet test --solution` as one flat
process, roughly an hour on the current development machine (most of that is
one ~29m43s Orleans outlier, see below). It is unchanged by `check-fast`
below; existing references to it (README, CONTRIBUTING, AGENTS.md, prior
research evidence) still mean exactly what they said.

`just check-fast` runs the same prerequisite gates (format, build, boundary),
then runs every CI-compatible shard - the same `fast-*`/`long-*`/`r02-*` Core
chunks, plus `exercise`, `contracts` and the Orleans host suite - as
concurrent local processes instead of one flat `dotnet test --solution`
process, bounded by this machine's real CPU count and available memory (see
`.github/scripts/local_parallel_check.py`; it adapts the same resource-guard
pattern `.github/scripts/ci_group_runner.py` uses for CI, scaled to this
machine rather than a CI-runner-sized assumption). It also drops the one
~29m43s Orleans outlier
(`Cna.OrleansHost.Tests.GrazianiPregameJournalTests.FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame`),
the same test and the same reasoning `ci-nightly-heavy.yml`'s `orleans` job
already excludes it for. `check-fast` still runs every other row `just check`
runs; it is a faster way to get the same coverage, not a smaller-coverage
substitute - use `just check` instead when you specifically want that one
outlier covered too, or a single flat process for simpler reproduction. Do
not rerun either after every bounded edit.

## Commands

```sh
just test-suites
just test-suite host
just test-suite runner
just test-suite contracts
just test-suite boundary
just test-class core '*CampaignContact*'
just test-method host '*InvalidForceProposalAtDeadlineCannotCancelCurrentRound*'
just test-shard fast-01
just test-shard long-03
just test-shard r02-1
just check-fast
just check
```

Patterns must be quoted against shell expansion. Names are xUnit class/method
patterns, not file paths. `core` selects the entire Core project unless narrowed
with `test-class` or `test-method`; it is still expensive. `runner` and CI's
`exercise` alias select the same project. `contracts` always runs both Game and
Intelligence contract assemblies. `boundary` runs all `Boundary=UserSpace` rows.

Project commands build only selected projects and their dependencies, in Release,
before running tests. They do not include formatting; run `just format-check`
for changed C# files. A test filter that matches nothing fails rather than passing.

Advanced options:

```sh
python3 .github/scripts/local_tests.py host --class '*CampaignDurableCreationRetryTests' --configuration Debug
python3 .github/scripts/local_tests.py host --method '*ForceRound*' --no-build
python3 .github/scripts/local_tests.py host --method '*ForceRound*' --dry-run
```

`--no-build` requires up-to-date binaries in the selected configuration; default
builds prevent accidental reuse of old code. Each actual invocation writes logs,
xUnit XML and `run.json` under a unique `artifacts/local-tests/` directory. Failed
builds, failed tests and interrupted runs cannot produce a passed receipt.

## Choosing coverage

| Changed area | Iteration | Before review |
| --- | --- | --- |
| Durable Host/API only | Relevant Host class/method | Host suite; boundary if owner projection/auth/privacy changes |
| Runner only | Relevant Runner class/method | Runner suite; affected Core tests if adjudication changes |
| Core rules, replay, RNG or state | Relevant Core class/method; relevant slow shard | Affected Core groups plus consumers (Host/Runner), boundary, and full CI |
| Typed contracts/protobuf | Relevant contract tests | Both contract assemblies and affected consumer suites |
| Maproom | Existing typecheck/component commands | `just maproom-check` and applicable browser/privacy tests |
| Docs/tooling | Links and focused helper tests | Affected tooling checks; no automatic full simulation run |

Cross-cutting serialization, shared infrastructure, test partitioning or uncertain
impact needs broader coverage. Record exactly which commands ran and which did
not. Full local runs remain useful for release checkpoints and reproducing full
suite failures. Exact-head CI is required before merge.

## CI-compatible chunks

Available Core chunks: `fast-01` through `fast-04`, `long-01` through `long-08`,
and `r02-1` through `r02-3`. They reuse CI's compiled Release discovery, timing
manifest and R02 allocation, including exact parameterized rows. Discovery runs
before each shard. Missing timing weights receive conservative estimates and obsolete weights
are ignored; exact discovery/execution coverage remains mandatory. Ordinary tests need no manifest
registration. See the [timing guide](../../.github/scripts/long-suite-weights.md).

Shard preparation builds the solution and discovery helper. `--dry-run` for a
shard still builds/discovers to print its actual commands; it never runs tests.
`just test-shard <shard>` runs one chunk sequentially, on demand, while you are
iterating on a specific area. CI runs multiple lanes across separate machines,
so CI wall times are not laptop estimates.

`just check-fast` is the one exception to "do not launch many shard processes
concurrently": it is exactly that, done deliberately and safely.
`.github/scripts/local_parallel_check.py` plans every shard listed above as
its own job, takes a real resource snapshot of this machine the same way
`ci_group_runner.py` does for CI, and runs as many jobs at once as real spare
CPU and memory support (one CPU and a fixed memory reserve are always held
back for the host; each job is budgeted against the rest) - never a
CI-runner-sized constant. It prints the snapshot and the chosen concurrency
before starting, and `--dry-run` prints every planned job and command without
running anything. Do not separately launch multiple `test-shard` invocations
by hand against the same build output; use `check-fast` for that instead.

## Graziani source setup soak

For a longer, opt-in real-source determinism probe, run:

```sh
SANDTABLE_GRAZIANI_SOAK=1 dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --filter-class '*GrazianiSourceAuthorityTests*'
```

In addition to the ordinary source-authority checks, this executes fixed seeds
104, 345 and 1979 for 32 alternating Axis/Commonwealth actions each. It repeats
each sequence from the same source state, checks canonical checkpoints, RNG,
pending arrivals and grouped formations, probes rejected stale and forged
submissions for no state changes, and reconstructs both histories. Failures
include the seed and step. This is a setup soak, not playable GT1 or arrival
execution; its extra runtime is excluded from ordinary CI timing weights.

CI's Linux resource supervision, exhaustive aggregate, private Python proofs and
exact-head admission remain separate. The local receipt explicitly identifies a
selected local run; it is not a CI coverage certificate. Existing Python Combat
oracles must still be run separately when relevant, retaining source-pin failures.

## Shared Postgres for concurrent workers

`Cna.OrleansHost.Tests` normally starts its own ephemeral Postgres container
(Testcontainers, `postgres:16-alpine`) per test process. That's fine for CI or
a single local run, but several concurrent worker sessions (e.g. Orchestrator
app workers, each in its own git worktree) each starting a container plus a
concurrent Release build can overload one machine.

Set `SANDTABLE_TEST_POSTGRES_URL` to opt a worker into sharing ONE long-lived,
externally-started Postgres instance instead:

```sh
scripts/shared-test-postgres.sh up    # idempotent; prints the export line below
export SANDTABLE_TEST_POSTGRES_URL="Host=localhost;Port=5433;Username=postgres;Password=postgres;Database=postgres"
scripts/shared-test-postgres.sh down  # when no worker needs it anymore
```

Leaving the variable unset keeps today's default behavior exactly as it was:
each process starts and owns its own container. When it's set, the affected
fixtures (`PostgresContainerFixture`, `SharedPostgres` in
`MartenJournalTestSupport.cs`) skip starting a container, point at that
connection string instead, and never stop it on teardown since other workers
may still be using it; `PostgresContainerFixture.CreateDatabaseAsync` also
uniquifies the database name it actually creates (fixed per-test-class
literal names would otherwise collide across workers sharing one instance),
transparently to every caller.

## Trunk.io flaky-test uploads

`architecture-tests`, `orleans`, and `tests` (CI) each run a
`trunk-io/analytics-uploader@v3` step (`continue-on-error: true`, so a Trunk
outage or a missing secret never fails the actual test gate). The
`architecture-tests` and `orleans` jobs' own `dotnet test` calls emit a
genuine JUnit-schema report (`--report-junit`, alongside the unchanged
`--report-xunit-xml` report) that step uploads. The `tests` job's real,
sharded execution (`.github/scripts/long_suite_partition.py` /
`r02_partition.py` / `ci_group_runner.py` / `test_partition.py`) does not yet
emit one -- only this repo's local-only `local_parallel_check.py` convenience
script does, so that job's upload step is currently a no-op on hosted CI;
wiring its real command builders is tracked as follow-up work (see the PR
that added this section for why it was deferred). Until the repo owner sets
the variables/secret below, every upload step above is a no-op regardless:

| Name | Kind | Where to get it |
| --- | --- | --- |
| `TRUNK_ORG_URL_SLUG` | repo variable | app.trunk.io -> Settings -> Organization -> General |
| `TRUNK_TEST_COLLECTION_ID` | repo variable | app.trunk.io -> Flaky Tests -> Collections (create one for this repo) |
| `TRUNK_API_KEY` | repo secret | app.trunk.io -> Settings -> Developer -> API Keys (needs "Upload test results" permission) |

Set repo variables/secrets at GitHub -> repo -> Settings -> Secrets and
variables -> Actions (variables and secrets are separate tabs there).

`Microsoft.Testing.Extensions.JUnitReport` is currently a **preview/prerelease**
NuGet package (pinned in `Directory.Packages.props`); its surface may still
change upstream.
