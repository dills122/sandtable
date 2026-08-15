# Sandtable

Sandtable is a deterministic campaign simulation with an optional intelligence plane. The
authoritative game remains reproducible and playable when model-backed services are unavailable.

The repository is at its architecture-baseline stage: the service boundaries, contracts, build,
tests, local orchestration, and CI are ready; campaign rules and player-facing Maproom features are
not implemented yet.

## Architecture

| Project | Responsibility |
|---------|----------------|
| `Cna.Core` | Pure Umpire domain, authoritative decisions, rules, and events |
| `Cna.OrleansHost` | Authoritative campaign activation and grain hosting |
| `Cna.DecisionWorker` | External decision dispatch outside grain turns |
| `Cna.Intelligence.Contracts` | Versioned protobuf and generated gRPC contracts |
| `Cna.Intelligence.Gateway` | Non-authoritative model/provider gateway |
| `Cna.ServiceDefaults` | Shared discovery, resilience, health, and telemetry defaults |
| `Cna.AppHost` | Aspire development orchestration |
| `Cna.Core.Tests` | Deterministic Umpire unit tests on xUnit v3 and MTP |
| `Cna.Intelligence.Contracts.Tests` | Protobuf boundary compatibility tests on xUnit v3 and MTP |

The governing rule is:

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers.
> Maproom shows.

The intelligence gateway receives redacted observations and returns proposals. It never owns game
state, resolves rules, sees hidden opposing state, or blocks an authoritative Orleans grain turn.

## Prerequisites

- [.NET SDK 10.0.302 or a later .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Just](https://just.systems/) (optional command shortcuts)
- Docker (optional; required only when future Aspire resources need containers)

`global.json` pins .NET 10 and selects Microsoft.Testing.Platform for `dotnet test`. .NET 10 is the
active LTS release; Microsoft supports it through November 2028.

## Get Started

```sh
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
```

Or use the command recipes:

```sh
just setup
just check
just run
```

The current gateway deliberately reports that no intelligence provider is configured. Callers must
use the deterministic scripted fallback until a provider adapter is implemented.

## Repository Standards

- Package versions are centralized in `Directory.Packages.props`.
- Shared C# and analyzer settings are in `Directory.Build.props` and `.editorconfig`.
- Build output is isolated under `artifacts/`.
- CI runs restore, formatting verification, a Release build, and all MTP tests.
- Protobuf changes must preserve field numbers and reserve removed fields.
- Warnings are errors; do not suppress diagnostics without a documented reason.

Run `just --list` to see local commands. See [CONTRIBUTING.md](CONTRIBUTING.md) for the development
workflow and [SECURITY.md](SECURITY.md) for vulnerability reporting.

## Design Sources

- [Technical design](tech-design.md)
- [Naming and domain vocabulary](naming-overview.md)
- [Microsoft Orleans documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [ASP.NET Core gRPC services](https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0)
- [Aspire AppHost and ServiceDefaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/aspire-sdk-templates)
- [Microsoft.Testing.Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)

## License

No license has been selected yet. All rights are reserved until a license file is added.
