# Focused local testing

Use the smallest relevant tests during development, then affected project suites
and boundary checks before review. CI retains exhaustive row coverage; local
selection is not a claim that every affected test was discovered automatically.
`just check` remains the explicit full local .NET gate, roughly an hour on the
current development machine. Do not rerun it after every bounded edit.

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
before each shard and fails on stale/missing manifest rows; use class/method
selection for newly added tests until the reviewed CI manifest is updated.

Shard preparation builds the solution and discovery helper. `--dry-run` for a
shard still builds/discovers to print its actual commands; it never runs tests.
Local groups execute sequentially to keep laptop CPU/memory bounded. CI runs
multiple lanes across separate machines, so CI wall times are not laptop
estimates, and chunking does not reduce total work when all chunks are selected.
Do not launch many shard processes against the same build output concurrently.

CI's Linux resource supervision, exhaustive aggregate, private Python proofs and
exact-head admission remain separate. The local receipt explicitly identifies a
selected local run; it is not a CI coverage certificate. Existing Python Combat
oracles must still be run separately when relevant, retaining source-pin failures.
