# Testing And Quality Gates

Testing must protect determinism, authority, wire contracts, and failure isolation.

## Required Coverage

- Seeded simulations replay to the same authoritative event sequence and result.
- Illegal commands, stale state versions, unknown plans, invalid parameters, and ruleset-hash
  mismatches are rejected by Umpire.
- Intelligence observations contain no opposing hidden state.
- Gateway timeouts, cancellation, malformed output, unavailable providers, and duplicate decision
  IDs resolve through bounded failure handling and deterministic fallback.
- Orleans tests prove external inference does not block an authoritative grain turn and stale
  proposals cannot mutate state.
- Protobuf changes preserve wire compatibility and generated client/server interoperability.
- Persistence tests round-trip events and snapshots without changing semantics.
- Telemetry tests avoid secrets, full prompts, hidden game state, and unbounded-cardinality labels.

## Test Shape

- Keep domain tests pure, deterministic, and fast.
- Inject seeded RNG, clocks, IDs, and provider adapters.
- Use in-process ASP.NET Core/gRPC hosts for contract and middleware tests.
- Add multi-silo Orleans tests for scheduling, reentrancy, persistence, serialization, and delivery
  behavior when those features are introduced.
- Use scripted intelligence as the baseline for headless simulation and model comparisons.
- Prefer provider-realistic database or transport integration tests to mocks at important
  boundaries.

## Commands

- Restore: `dotnet restore Sandtable.slnx`
- Build: `dotnet build Sandtable.slnx --no-restore`
- Unit/integration tests: `dotnet test --solution Sandtable.slnx --no-build`
- Formatting: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`
- Full local gate: `just check`

Native MTP test commands require `--solution` or `--project`; bare positional paths are invalid.
If a gate cannot run, state why and what risk remains; never report an unexecuted gate as passing.

## Before Finishing Work

- Run the smallest reliable affected test set, then the relevant root gate for shared contracts or
  architecture changes.
- Confirm no unrelated formatting or generated-code churn.
- Update protobuf, architecture docs, and generated artifacts together when contracts change.
- Record any nondeterministic test, skipped integration environment, or external dependency as an
  explicit residual risk.
