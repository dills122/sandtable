# Sandtable

> Command the campaign. Let the Umpire handle the paperwork.

## What is Sandtable?

Sandtable is an in-development digital adaptation of SPI's 1979 board wargame *The Campaign for
North Africa: The Desert War, 1940-43*. The original game models the desert war at an extraordinary
level of detail. Players command Axis or Commonwealth forces, maneuver formations across North
Africa, manage scarce supplies and transport, fight battles, and pursue the victory conditions of
the chosen scenario.

Sandtable aims to preserve those decisions and the character of the original game while asking the
computer to handle the rules, calculations, record-keeping, and hidden information. It is not a
simplified game merely wearing the same theme: the rules target is the original 1979 SPI edition,
corrected by the September 1979 errata, with any necessary interpretations recorded explicitly.

The project uses original software and presentation assets. Scans, rules prose, maps, and counter
art from the published game are not distributed in this repository.

## How will it play?

A campaign is overseen by a digital **Umpire**. Players issue orders; the Umpire checks what is
legal, resolves movement and combat, applies uncertainty, reveals only what each side is allowed to
know, and records what happened.

In the simulated campaign calendar, each turn represents one week and contains three Operation
Stages. That does not mean a turn takes a real-world week to play: local sessions advance as the
players make decisions. Initiative shapes which side acts first or last in each stage, and play
moves through repeated movement and combat segments rather than one simple move-then-fight pass.
Over the course of a scenario, players must balance position, combat power, cohesion, supply,
transport, reinforcements, and the need to meet their own victory conditions.

The first playable release is planned as:

- the six-turn, Land-only **Graziani's Offensive** scenario;
- two-player local hot-seat play through the **Maproom** interface;
- an original schematic map with selectable formations and legal-action guidance;
- save and resume support, strict fog of war, and a complete campaign history; and
- deterministic replay from the same starting seed and accepted orders.

Later releases can add the detailed Air and Logistics Games, longer scenarios, the full 111-turn
campaign, remote multiplayer, and optional AI commanders and narrative. AI is intended to advise or
play a side; it will never decide the rules or secretly change the campaign state.

## Where is the project now?

> [!IMPORTANT]
> Sandtable is pre-alpha infrastructure, not yet a playable adaptation of the published game.

The current foundation can create a campaign from a recognized synthetic setup, resolve
Initiative Determination with a seeded random stream, emit authoritative events, and replay those
events to byte-identical state. It also includes Content Pack v1 and an original nine-hex,
nonhistorical rules laboratory for developing the game systems without redistributing published
assets.

Movement, combat, published scenario content, persistence, and the Maproom player interface remain
future work. Campaign-world binding is the next planned capability; side-safe observations and
legal actions follow it.

The high-level path to a playable game is:

1. Bind exact scenario content to an authoritative campaign world.
2. Generate side-safe observations and legal actions.
3. Implement the mandatory turn preamble and a complete movement/contact/combat loop.
4. Add the remaining Land systems and data required by *Graziani's Offensive*.
5. Deliver the Maproom interface, local hot-seat play, saves, and replay.
6. Expand into detailed Air and Logistics play, later scenarios, and optional intelligence.

See the [pre-alpha roadmap](docs/roadmap/pre-alpha-roadmap.md) for the capability-level plan and
completion criteria.

## Up and running

Running the repository today launches the development service scaffold and Aspire dashboard. It
does not yet launch a playable Maproom client.

### Prerequisites

- [.NET SDK 10.0.302 or a later .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Just](https://just.systems/) for the shortest command workflow (optional)
- Docker (optional; required only when future Aspire resources need containers)

`global.json` pins .NET 10 and selects Microsoft.Testing.Platform for `dotnet test`.

### Quick start with Just

```sh
git clone https://github.com/dills122/sandtable.git
cd sandtable
just setup
just check
just run
```

Open the Aspire dashboard URL printed in the terminal to inspect the Orleans host, decision worker,
and intelligence gateway. Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to stop the application.

### Manual .NET commands

```sh
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
```

The intelligence gateway currently reports that no model provider is configured. This is expected:
model-backed commanders are not part of the pre-alpha gameplay target, and authoritative play must
always have a deterministic scripted fallback.

Run `just --list` to see all available command recipes. See
[CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow and
[SECURITY.md](SECURITY.md) for vulnerability reporting.

Codex-managed worktrees use `.codex/environments/environment.toml` to seed the repository's
Git-ignored AI Central skills and steering before a task starts. The setup expects AI Central at
`$HOME/.ai-central` by default; set `AI_CENTRAL_HOME` when the shared checkout lives elsewhere.
The seeder copies only its allowlisted AI context and does not overwrite worktree-owned files.

## How it works

Sandtable separates the authoritative simulation from optional external services. The **Umpire**
owns the game state and rules. **Command** and **Staff** choose objectives and plans. **Dispatch**
carries orders and reports. The **Chronicle** records authoritative history, and the **Maproom**
presents the campaign to players.

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers.
> Maproom shows.

The intelligence gateway receives redacted observations and returns untrusted proposals. It never
owns game state, resolves rules, sees hidden opposing state, or holds an authoritative Orleans grain
turn open while a model responds. If a model-backed service is unavailable, play falls back to a
deterministic scripted decision rather than failing the turn.

### Architecture

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
| `Cna.Intelligence.Contracts.Tests` | Protobuf compatibility tests on xUnit v3 and MTP |

The current Umpire foundation is intentionally pure and in-process:

- `Cna.Core.Rules` owns source references, adopted-ruling metadata, the canonical `cna-1979.1`
  ruleset hash, Initiative Ratings, and the hierarchical Land sequence catalog.
- `Cna.Core.Randomness` owns the versioned deterministic random stream and published golden vectors
  used by authoritative mechanics.
- `Cna.Core.Setups` owns recognized provenance-bearing synthetic setup fixtures; callers cannot
  supply free-form initiative inputs.
- `Cna.Core.Content` owns validated immutable topology, force structure, scenario declarations,
  per-datum origins, canonical bytes and hashes, and the original nonhistorical rules laboratory.
- `Cna.Core.Campaigns` owns versioned commands and events, typed rejection, canonical snapshots,
  deterministic Initiative adjudication, serialization, and replay.
- The sequence catalog cites the original Land Rules without embedding copyrighted rules prose or
  component art. Inspecting that catalog is not authoritative adjudication.

## Repository standards

- Package versions are centralized in `Directory.Packages.props`.
- Shared C# and analyzer settings are in `Directory.Build.props` and `.editorconfig`.
- Build output is isolated under `artifacts/`.
- CI runs restore, formatting verification, a Release build, and all MTP tests.
- Protobuf changes must preserve field numbers and reserve removed fields.
- Warnings are errors; do not suppress diagnostics without a documented reason.

## Design and research

- [Technical design](tech-design.md)
- [Naming and domain vocabulary](naming-overview.md)
- [Campaign for North Africa source-material spike](docs/research/cna-source-material-spike.md)
- [Commander personas spike](docs/research/commander-personas-spike.md)
- [Pre-alpha roadmap](docs/roadmap/pre-alpha-roadmap.md)
- [Initiative Determination research](docs/research/initiative-determination-spike.md)
- [Initiative Determination specification](docs/specs/initiative-determination.md)
- [Initiative Determination technical design](docs/design/initiative-determination.md)
- [Content Pack v1 research](docs/research/content-pack-v1-spike.md)
- [Content Pack v1 specification](docs/specs/content-pack-v1.md)
- [Content Pack v1 technical design](docs/design/content-pack-v1.md)
- [Microsoft Orleans documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [ASP.NET Core gRPC services](https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0)
- [Aspire AppHost and ServiceDefaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/aspire-sdk-templates)
- [Microsoft.Testing.Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)

## License

No license has been selected yet. All rights are reserved until a license file is added.
