# Contributing to Sandtable

## Before You Start

1. Create a feature branch; do not work directly on `main`.
2. Read `AGENTS.md`, `tech-design.md`, and `naming-overview.md`.
3. Keep changes inside the owning project boundary.
4. Update contracts and design documentation before dependent implementations.

## Local Setup

Install the .NET SDK selected by `global.json`, then run:

```sh
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
```

`just setup` performs the version check and restore. `just check` runs the normal local quality
gates. Native Microsoft.Testing.Platform requires `--solution` for solution tests and `--project`
for individual test projects; a bare positional path is not supported.

`just check` does not run the retained Python Combat contract oracles. Their known source-pin and
recursive-admission failures remain open, with maintenance deferred; remaining bounded oracle
timeouts are unverified. Read the [verification inventory](docs/research/combat-verification-pin-maintenance.md)
when working on Combat contracts and report each original command's outcome separately from
supplemental semantic probes.

## Development Rules

- Keep Umpire logic deterministic and free of remote I/O.
- Treat intelligence results as untrusted proposals.
- Keep protobuf field numbers stable and reserve removed fields.
- Carry decision ID, state version, and ruleset hash through the decision boundary.
- Use explicit deadlines and cancellation for remote calls.
- Add a focused failing test before implementing behavior.
- Do not add prerelease dependencies without an explicit, documented decision.

## Pull Requests

Before requesting review:

```sh
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --configuration Release --no-restore
dotnet test --solution Sandtable.slnx --configuration Release --no-build
```

Include the motivation, affected boundaries, contract or persistence impact, test evidence, and any
remaining risk. Generated protobuf output belongs under `artifacts/obj` and must not be committed.
