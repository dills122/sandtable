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

The current foundation can create a campaign from an exact ruleset, setup, Content Pack, and
scenario; project the scenario's initial mutable element locations; resolve Initiative
Determination and both admitted no-obligation Naval Convoy checkpoints; let the initiative holder
declare whether to act first or last in Operation Stage 1; resolve Weather; emit authoritative
events; and replay those events to byte-identical state. Ruleset manifest contract 3, Content Pack
schema 2, and Campaign World v1 use an original nine-hex,
nonhistorical rules laboratory to develop game systems without redistributing published assets.
Campaign Observation contract 2 derives deterministic side-safe public topology, turn state, and
own-force facts without exposing the complete Content Pack or any opposing-force row or
association. Legal Actions v1 exposes those mechanics through an opaque campaign-authority handle,
deterministic system/side action sets, exact-audience membership enforcement, and side-safe
acceptance receipts. Weather Determination v1 resolves corrected source-cited Weather through that
same boundary, records pair-keyed evidence, publishes only a source-free Weather summary, and stops
at the same Operation Stage's Organization barrier. Raw snapshots, commands, events, content
context, projection, and replay are not public mutation seams.

The local `Cna.ExerciseRunner` can now drive that synthetic rules-laboratory path as either one
bounded, deterministic **Exercise** or one serial **Maneuver**. An Exercise uses a fresh opaque Core
capability, selects only current legal actions, stops at Organization, proves both event-history
reconstruction and fresh-session re-adjudication, and writes a manifest-last `trusted-authority`
evidence bundle. A Maneuver strictly admits one canonical ordered `serial-unpaired` manifest,
derives explicit child identities from its sole parent root seed, and runs each child in process
through the same coordinator. Each completed child bundle is read once for semantic validation and
identity-matched aggregation. The resulting transactional report separates deterministic counts,
outcomes, and fingerprint material from noncanonical timing/path diagnostics and is strictly read
back before completion is claimed. Compact, forensic, and debug Exercise detail tiers expose
progressively richer evidence without changing simulation truth. Paired comparison, model
controllers, and side-safe exports are not implemented yet.

Organization/stage-entry mechanics, movement, combat, published scenario content, persistence, and
the Maproom player interface remain future work.

The reviewed Player Intent Composer is also future work. After the movement/contact/combat skeleton
proves one representative multi-field decision, a no-model prototype will validate contextual
suggested approaches, a private typed draft, bounded clarification, deterministic Staff planning,
and hot-seat isolation. Deterministic Maproom integration belongs in Sprint 8; Needle or any other
parser remains behind a post-MVP evidence gate and cannot block the playable campaign.

The current delivery boundary is:

| Area | Status |
| --- | --- |
| Ruleset/provenance, synthetic content, campaign authority, deterministic randomness, events, and replay | Implemented foundation |
| Side-safe observations and exact-audience legal actions | Implemented for the current rules-laboratory path |
| Mandatory turn preamble | Partially implemented; Weather is complete and authority stops at Organization |
| Movement/contact and combat loops | Planned; not started |
| Published first-scenario data, remaining Land rules, victory, persistence, and Maproom | Milestone-level; not started |
| Player Intent Composer | Direction reviewed; representative decision after the combat skeleton, no-model prototype before Maproom, optional parser evaluation after deterministic MVP |
| Exercise Harness | Single-Exercise and serial-unpaired Maneuver paths implemented; pairing remains later |

The approved high-level path to a playable game is:

1. Resolve Organization, Naval Convoy Arrival, and Fleet obligations, then stop at Reserve.
2. Implement Reserve and a complete movement/contact/combat loop through the legal-action boundary.
3. Add the remaining Land systems and data required by *Graziani's Offensive*.
4. Validate the representative Player Intent Composer flow without a model.
5. Deliver deterministic Maproom, local hot-seat play, saves, replay, and the reviewed intent flow.
6. Evaluate optional parsing only after the deterministic MVP, then expand into detailed Air and
   Logistics play, later scenarios, and optional intelligence.

The serial-Maneuver portion of Exercise Harness v1 now provides validated local batch-regression
evidence without adding game rules. The next authoritative-engine planning package is
[Operation-Stage Entry research](docs/research/operation-stage-entry-spike.md), its proposed
[specification](docs/specs/operation-stage-entry-v1.md), and proposed
[technical design](docs/design/operation-stage-entry-v1.md). Its final planning review is Ready, but
owner approval and the Task 001 contract freeze still gate implementation. Paired comparison
remains a later evaluation capability and does not block gameplay-engine progress.

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

Run the checked-in deterministic Exercise and write its ignored evidence bundle with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.v1.json \
  --artifact-root artifacts/exercises
```

The command prints the finalized bundle path. The checked-in manifest is explicitly exploratory,
so a dirty development tree is recorded honestly as nonbaseline and nonreproducible.

From a clean checkout, request a fail-closed baseline bundle with the checked baseline twin:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.baseline.v1.json \
  --artifact-root artifacts/exercises
```

Set the manifest's `detail` to `compact`, `forensic`, or `debug`. Forensic adds correlated audience
queries, controller selection, checks, proofs, payload sizing, and the progressively assembled
context of failed query/controller/submission decisions. Debug also retains every available
monotonic phase timing on failure and prints a structured post-readback artifact trace. These
diagnostics are trusted local instrumentation and never participate in replay equality.

Run the checked two-child serial Maneuver with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.serial.v1.json \
  --artifact-root artifacts/exercises
```

The command prints each validated child bundle path in manifest order, followed by the strictly
read-back aggregate report path and deterministic report fingerprint. The report's local paths and
timings are diagnostics and do not participate in that fingerprint.

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
| `Cna.ExerciseRunner` | Local deterministic Exercise and serial-Maneuver orchestration with trusted artifacts |
| `Cna.Core.Tests` | Deterministic Umpire unit tests on xUnit v3 and MTP |
| `Cna.ExerciseRunner.Tests` | Exercise contracts, replay, artifact, and CLI tests on xUnit v3 and MTP |
| `Cna.Intelligence.Contracts.Tests` | Protobuf compatibility tests on xUnit v3 and MTP |

The current Umpire foundation is intentionally pure and in-process:

- `Cna.Core.Rules` owns source references, adopted-ruling metadata, the canonical `cna-1979.1`
  ruleset hash, Initiative Ratings, and the hierarchical Land sequence catalog.
- `Cna.Core.Randomness` owns the versioned deterministic random stream and published golden vectors
  used by authoritative mechanics.
- `Cna.Core.Setups` owns recognized provenance-bearing synthetic setup fixtures; callers cannot
  supply free-form initiative inputs.
- `Cna.Core.Content` owns validated immutable topology, force structure, scenario declarations,
  per-datum origins, canonical bytes/hash, and the original nonhistorical rules laboratory.
- `Cna.Core.Campaigns` owns internal exact-content authority, mechanic commands/events, deterministic
  Initiative and opening-preamble adjudication, canonical history, replay, and the public opaque
  authority handle/creation facade.
- `Cna.Core.Observations` owns the output-only Campaign Observation v1 allowlist, typed projection
  result, handle-based query facade, structural values, and canonical side-safe JSON.
- `Cna.Core.Actions` owns typed system/side candidates, canonical action identity, observation-only
  side generation, exact-audience query/submission enforcement, and side-safe receipts.
- `Cna.Core.Exercises` owns the fresh-only opaque simulation capability, immutable trusted step
  evidence, and reconstruction from its retained canonical event history.
- `Cna.ExerciseRunner` owns deterministic controllers, bounded execution, re-adjudication,
  versioned Exercise/Maneuver evidence contracts, build identity, transactional bundles and
  reports, summaries, and CLI exits.
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
- [Campaign World v1 specification](docs/specs/campaign-world-v1.md)
- [Campaign World v1 technical design](docs/design/campaign-world-v1.md)
- [Observation and fog boundary research](docs/research/observation-and-fog-boundary-spike.md)
- [Reconnaissance and contact knowledge research](docs/research/recon-contact-knowledge-spike.md)
- [Campaign Observation v1 specification](docs/specs/campaign-observation-v1.md)
- [Campaign Observation v1 technical design](docs/design/campaign-observation-v1.md)
- [Turn-preamble action-boundary research](docs/research/turn-preamble-action-boundary-spike.md)
- [Legal Actions v1 specification](docs/specs/legal-actions-v1.md)
- [Legal Actions v1 technical design](docs/design/legal-actions-v1.md)
- [Player intent input and Needle feasibility research](docs/research/player-intent-input-and-needle-feasibility.md)
- [Player Intent Composer v1 proposed specification](docs/specs/player-intent-composer-v1.md)
- [Player Intent Composer v1 proposed technical design and delivery plan](docs/design/player-intent-composer-v1.md)
- [Operation-Stage Preamble research](docs/research/operation-stage-preamble-spike.md)
- [Weather Determination v1 specification](docs/specs/weather-determination-v1.md)
- [Weather Determination v1 technical design](docs/design/weather-determination-v1.md)
- [Exercise Harness capability and replay research](docs/research/exercise-capability-and-replay-spike.md)
- [Exercise Harness evidence artifact research](docs/research/exercise-evidence-artifact-spike.md)
- [Exercise Harness reproducibility and pairing research](docs/research/exercise-reproducibility-and-pairing-spike.md)
- [Exercise Harness v1 specification](docs/specs/exercise-harness-v1.md)
- [Exercise Harness v1 technical design and delivery plan](docs/design/exercise-harness-v1.md)
- [Operation-Stage Entry source and contract research](docs/research/operation-stage-entry-spike.md)
- [Operation-Stage Entry v1 proposed specification](docs/specs/operation-stage-entry-v1.md)
- [Operation-Stage Entry v1 proposed technical design and delivery plan](docs/design/operation-stage-entry-v1.md)
- [Microsoft Orleans documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [ASP.NET Core gRPC services](https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0)
- [Aspire AppHost and ServiceDefaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/aspire-sdk-templates)
- [Microsoft.Testing.Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)

## License

No license has been selected yet. All rights are reserved until a license file is added.
