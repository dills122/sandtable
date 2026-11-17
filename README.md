# Sandtable

> Command the campaign. Let the Umpire handle the paperwork.

Sandtable is a digital adaptation of SPI's 1979 board wargame *The Campaign for North Africa: The
Desert War, 1940–43*. The original game simulates the desert war at an extreme level of detail:
formations, supply, transport, hidden information, all of it. Sandtable keeps those decisions
intact for the player and hands the bookkeeping to the computer: legality checks, dice, hidden
information, and the campaign record.

This is not a simplified game using the same theme. The rules target is the original 1979 SPI
edition, corrected by the September 1979 errata. Any place that required a judgment call is
recorded explicitly rather than quietly decided.

The original game's scans, rules text, maps, and counter art are not part of this repository. They
stay with the published game.

## How a game plays

A campaign runs through a digital Umpire. Players issue orders. The Umpire checks whether each
order is legal, resolves movement and combat, applies the game's dice and hidden information, and
keeps the official record of what happened. Neither player sees more than their side is entitled to
know.

Each turn represents one week and is split into three Operation Stages. That is calendar time
inside the simulation, not real time at the table: a session moves as fast as the players make
decisions. Initiative decides which side acts first or last in each stage. A turn is not one simple
move-then-fight pass; it moves through repeated movement and combat segments. Across a scenario,
players balance position, combat power, cohesion, supply, transport, reinforcements, and their
scenario's victory conditions.

The first playable release is planned to include:

- the six-turn, Land-only Graziani's Offensive scenario
- two-player local hot-seat play through the Maproom interface
- an original schematic map with selectable formations and legal-action guidance
- save and resume support, strict fog of war, and a complete campaign history
- deterministic replay from the same starting seed and accepted orders

Later releases can add the Air and Logistics Games, longer scenarios, the full 111-turn campaign,
remote multiplayer, and optional AI commanders. AI is intended to advise or play a side. It will not
decide the rules or change campaign state on its own.

The proposed [durable Orleans hosting plan](docs/work/plans/2026-10-24-orleans-hosting.md)
records the path to multi-server campaigns and capacity benchmarks. Production hosting and
capacity limits remain unimplemented.

## Tech stack

**Simulation engine and backend.** C# on .NET 10, run locally through .NET Aspire. Campaign hosting
runs on Orleans grains backed by SQLite. The rules engine (`Cna.Core`) has no dependency on either
of those: it is pure, in-process C# so it can be tested and replayed in isolation. An Intelligence
Gateway speaks gRPC, keeping any future model-backed opponent on the other side of a firm boundary
from authoritative game state.

**Maproom (the UI).** Angular 22 with Angular Material and the CDK, Tailwind for styling, NgRx
Signals for state, Reactive Forms through `@ngx-formly`, and Vite as the build tool. Storybook,
backed by the `@sandtable/field-atlas` design-system package, hosts the component library
separately from the app shell.

**Testing and tooling.** xUnit on Microsoft.Testing.Platform, the native test runner (not
`dotnet vstest`). [Just](https://just.systems/) for short project commands. Lychee for checking
documentation links. A deterministic Exercise Runner CLI for scripted, replayable simulation runs
outside any UI.

## Project layout

| Path | What lives there |
| --- | --- |
| `src/Cna.Core` | The deterministic rules engine. No I/O, no UI, no network. |
| `src/Cna.OrleansHost` | Campaign hosting: Orleans grains, durable SQLite-backed state, the trusted server side. |
| `src/Cna.AppHost` | .NET Aspire entry point that wires up the local development service stack. |
| `src/Cna.Maproom` | The Angular frontend: map, orders, formation ledger. See its own [README](src/Cna.Maproom/README.md). |
| `src/Cna.Game.Contracts` | The typed HTTP contract between Maproom and the host, including a generated client. |
| `src/Cna.Intelligence.Gateway` / `Cna.Intelligence.Contracts` | The gRPC boundary for any future model-backed commander. |
| `src/Cna.ExerciseRunner` | CLI for running and comparing deterministic simulation scenarios without a UI. |
| `src/Cna.DecisionWorker`, `Cna.ServiceDefaults` | Supporting services for the Aspire stack. |
| `docs/` | Specs, design docs, research notes, and the current roadmap. |

## Where things stand

See the [RC checklist](docs/work/plans/2026-10-24-rc-checklist.md) for current
implementation evidence, open release gates and explicit pass criteria.
The [six-turn working inventory](docs/work/plans/2026-11-12-graziani-playable-rc-inventory.md)
maps scenario obligations and the remaining path to a playable release. The
[setup draft](docs/specs/graziani-setup-draft.md) records open admission decisions;
a source-neutral [scheduled-arrival index](docs/specs/scheduled-arrival-index-v1.md)
now has synthetic tests but does not yet provide campaign arrival authority. A
[synthetic obligation laboratory](docs/specs/scheduled-arrival-lab-v1.md) exercises
owner choices, other obligations and replay without advancing a campaign. A
[content draft analyzer](docs/specs/campaign-scheduled-arrival-content-draft-v1.md)
checks deferred units against real content definitions without admitting them.
A [draft identity and world contribution](docs/specs/campaign-arrival-draft-identity-v1.md)
bind candidate content and materialize synthetic arrival values without campaign authority.
An internal [arrival replay draft](docs/specs/campaign-arrival-replay-draft-v1.md)
exercises canonical event bytes and receipt recovery without providing durable campaign storage.
The [synthetic arrival profile](docs/specs/campaign-synthetic-arrival-profile-v1.md)
fixes one repository-owned fixture; it is not an admitted campaign setup.
An isolated [synthetic arrival phase](docs/specs/campaign-synthetic-arrival-phase-v1.md)
exercises a Land-sequence transition, side-specific offers and replayed checkpoints.
An internal [World6 arrival cut](docs/specs/campaign-arrival-world6-successor-v1.md)
and [durable journal path](docs/specs/campaign-scheduled-world6-durable-host-v1.md)
exercise synthetic creation, replay and owner receipt recovery. New journal records retain
native preamble event bytes while older digest-only records remain readable; no public campaign
route uses this cut.
The production [scenario content model](docs/specs/campaign-scenario-arrival-content-v1.md)
can describe sourced scheduled arrivals and grouped initial counters. Its versioned content
formats preserve and hash those records, source-listed truck quantities, partial combat and weapon-source facts, and TOE seeds; legacy formats
reject the new records. An internal [initial world constructor](docs/specs/campaign-world-v8.md)
places grouped counters and retains deferred arrivals using those records; it does not
admit a campaign. The normal durable journal can now persist and cold-replay a
[source-backed initial world](docs/specs/campaign-source-creation-v1.md) with its
pending groups and exact content bytes. This initialization-only path does not
execute arrivals or make a scenario playable. Historical campaign admission remains unsupported.
The production `Cna1979GrazianiCairoSourceExcerpt` builder provides the source-backed
Cairo excerpt used in tests; its map and formation data remain incomplete.
A separate provisional pre-game setup slice records an Axis GT2 supply availability
roll and partial lane allocations through the normal session and durable Host journal.
The development-only local API exposes that slice to an authenticated Axis seat.
An operator can create it without uploading a content pack via
`POST /api/v2/campaigns/pregame-source/graziani`; see
[the pre-game supply spec](docs/specs/graziani-pregame-supply-v1.md) for its
required initialization fields and incomplete, provisional scope.
The [version 4 source authority](docs/specs/graziani-source-authority-v4.md) commits
that plan and records bounded, private Axis and Commonwealth supply setup choices
in the same journal, with source-backed initial-force evidence.
Optional operator [source-force upgrades](docs/specs/graziani-source-forces-v5.md)
retain further historical Axis and Commonwealth evidence, player setup choices,
and selected source-backed units in the World8 state. Grouped formations, map and
logistics remain incomplete. These upgrades cannot complete setup, advance the
campaign, or execute the Polish arrival.
The Commonwealth can confirm a source-backed 4th Indian Division grouping and
reserve its existing motor pool for Central India Horse. The same grouped source
world can retain the confirmed Matruh garrison and Maletti, including their selected
detachments, and propose both Libyan divisions for Axis confirmation. This state is
replayable but is not yet accepted by native movement or combat.
The Host can also admit proposed 62 Marmarica, 63 Cirene and 64 Catanzaro groups for Axis
confirmation, and 7th Armored and 16th Brigade groups for Commonwealth
confirmation, through normal source setup actions and journal replay.
Axis can optionally preplan GT3–4 supply arrivals in September through the same
setup actions; those arrivals are in October. Other convoy cargo remains
unresolved; no delivery is implied.
Both owners can finish or reopen setup privately. A source-turn authority now
retains the grouped state and random stream and includes bounded GT1 planning,
tactical-shipping declines, initiative declaration, weather, selected convoy
deliveries and a single-hex Polish arrival. Its start gate still rejects the
incomplete historical map, control and contact coverage, so those turn and
arrival transitions cannot yet be reached through a real campaign.
After Australian training is interrupted and both sides finish opening
Organization choices, a journaled GT1 completion step applies the provisional
no-prior-construction ruling; the existing campaign-start gate still applies.
GT1 stage-one convoy arrivals can then close only after all due Axis lanes are
delivered, no units remain due, and the Commonwealth has skipped opening supply.
The following stage-one fleet step applies the scenario's GT1 movement ban and
undamaged starting state; later fleet operations remain unsupported.
The first acting player can then decline reserve designation and enter its
movement segment through the normal owner-action interface. Version-3 turns
also let that player finish movement and advance to breakdown determination.
The bounded no-breakdown successor supports either no movement or the exact
4th Indian opening move; other movement histories remain blocked there.
A [pinned historical map transcription](docs/specs/graziani-historical-map-transcription.md)
now supplies the opening-road and Cairo-entry consistency checks, and resolves
same-sheet contact geometry for map-bound source turns. Apart from the three
verified C/D crossings below, remaining seam edges and grouped control are unadmitted.
New source-turn checkpoints bind the pinned map identity and three provisionally
approved C/D crossings; legacy turn records retain their original geometry
and checkpoint version during replay. General cross-sheet movement remains blocked.
Supported source setup choices can also be queried and submitted through the normal legal-action
interface; the owning player receives its setup context and provisional rulings.
The Commonwealth can assign the initial 1 RTR and 11 Hussars truck pools and
choose their initial loads through this interface. Finishing with empty trucks
clears their load-specific blocker; loaded-truck consumption and detachment
remain unsupported. Other blockers still prevent source-turn start.
The optional compact source checkpoint reduces new journal rows while retaining a
requirement to replay the complete journal from genesis.
Normal gameplay creation and journal replay can use a supplied, identity-checked Content 6
resolver; the default Host still uses the synthetic catalog, and resolver content must remain
available across restarts.

> [!IMPORTANT]
> Sandtable is a tested pre-alpha simulation engine, not yet a playable adaptation of the published
> game. Current scenarios are synthetic rules-laboratory fixtures, not released campaign content.

| Area | Status | What that means |
| --- | --- | --- |
| Deterministic Umpire | Working | Versioned rules, seeded randomness, canonical commands and events, replay, checkpoints, and side-safe action boundaries are implemented. |
| Playable rule path | Working through Combat entry | A runner executes Initiative, stage preamble, Reserve Designation, Movement, bounded ZOC/Reaction, and Breakdown, then stops before Combat adjudication. |
| Combat and the continual turn cycle | Bounded native implementation | Contact-based Core and normal durable Host cover an initial assault plus two selected repeated assaults, settlement, custody, and Reserve Release. Development loopback access is not public Combat certification; broader support and evidence gates remain open. |
| Exercise and Maneuver tools | Working | Deterministic single runs, multi-run matrices, paired comparisons, and evidence bundles, all from the CLI. |
| User interface (Maproom) | Angular foundation, local read-only preview | A synthetic fixture study and a local, URL-backed campaign read mode. No live multiplayer actions yet. |
| Published scenario | Source data in progress | Six-turn Graziani's Offensive is the first target; historical campaign admission and the required Combat loop remain incomplete. |
| Durable save and resume | Local durable Host implemented; product gate open | SQLite journal and cold replay support bounded local campaigns; schema 14 persists Selection, RBA, Force, Retreat, Custody, Reserve Release and cycle-control deadlines at decision entry; background fallback needs no owner query. HOST-PUB-001 publication certification and user-facing campaign lifecycle acceptance remain open. |
| Model-backed commanders | Scaffold only | The gateway and worker processes exist. No model provider is configured, and AI will not hold authority over the rules. |

In plain terms: you can build the engine, run the full test suite, launch the Aspire service stack,
and simulate the currently checked Movement, Reaction, and Breakdown paths. Native Combat is
implemented for bounded local paths; public certification remains open. You cannot yet play a public campaign end to end.
When an initiative holder elects to act last, the normal Combat owner and its
timeout use the recorded first-acting side; older decisions remain replayable.
World6 now calculates source-qualified ZOC, although its admitted content profile
still excludes the stacks needed to produce one. Historical armor,
reconnaissance and anti-tank ratings can contribute to that calculation without
being admitted for combat; artillery position and player-safe ZOC disclosure
remain open.
An owner can explicitly pass the first side's convoy, rail, towing, maintenance
and patrol phases through the normal journal; the checkpoint then reaches the
second side's Reserve Designation. That side can designate eligible units and
complete its reserve phase through the same journal. An eligible unit can then
move one hex and stop, with contacts and any Reaction window retained through
replay. An explicit owner completion advances to Breakdown when no Reaction is
pending. Reaction handling, Breakdown and those optional activities remain incomplete.
The [October 24 baseline evidence matrix](docs/work/reviews/2026-10-24-rc-baseline-evidence.md)
separates these paths from original P01 certification and remaining release gates.

For full detail on every contract, every merged slice, and every open gate, see the
[current roadmap](docs/roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates), the
[first-release audit](docs/research/2026-10-05-first-release-audit.md), and the
[Combat implementation plan](docs/design/combat-cycle-implementation-plan.md).

## Getting started

### Prerequisites

- [.NET SDK 10.0.302 or later in the .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), pinned in `global.json`
- [Just](https://just.systems/), optional; the commands below assume it
- Git
- Docker, only if you want the containerized Proving Ground preview

Check your SDK with `dotnet --version`. This repo uses Microsoft.Testing.Platform's native runner,
so `dotnet test` needs `--solution` for the whole solution or `--project` for one test project. A
bare positional test path is not supported.

### First checkout

```sh
git clone https://github.com/dills122/sandtable.git
cd sandtable
just setup
just check
```

Without Just:

```sh
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
```

### Run the service stack

```sh
just run
# or: dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
```

This opens the Aspire dashboard at the URL printed in the terminal, showing the Orleans host,
Decision Worker, and Intelligence Gateway. It launches the development service stack, not a
playable Maproom on its own. Stop with <kbd>Ctrl</kbd>+<kbd>C</kbd>.
AppHost uses Aspire SDK 13.6.5. Builds may report ASPIRE010 when the optional CLI bundle is disabled.

### Run the Maproom UI

```sh
just maproom-setup
just maproom       # Angular app on port 4200
just field-atlas   # Storybook on port 6006, in a second terminal
```

For the local campaign read mode, open <http://127.0.0.1:4300/> and select Local campaign read
mode, or go directly to `/?mode=campaign`. The
[connected local-read recipe](docs/design/maproom-connected-local-read.md) covers the exact
two-port setup (4300 and 5087); it does not change the 4200 preview.

### Run a deterministic simulation from the CLI

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

This prints the resulting bundle paths, an aggregate report, and a deterministic fingerprint you
can diff against a previous run. More manifests and diagnostic modes are in the
[Exercise Runner runbook](docs/runbooks/exercise-runner.md).

### Try the containerized preview

With Docker Desktop running:

```sh
just proving-ground-up     # builds and starts the Maproom shell (4200) and a dev Orleans host (5180)
just proving-ground-smoke  # health-checks both
just proving-ground-down   # tears it down
```

The [Proving Ground runbook](docs/runbooks/proving-ground.md) covers what each service does and
where the current rules-lab seed and evidence commands live.

### Preview the project website

```sh
python3 -m http.server 4173
```

Open `http://localhost:4173/site/`. It is dependency-free project documentation, not game UI.

## Contributing

The full workflow is in the [contributor guide](CONTRIBUTING.md). The short version:

1. `git switch -c codex/my-change`. Do not commit directly to `main`.
2. Read [`tech-design.md`](tech-design.md) for architecture and authority boundaries, and
   [`naming-overview.md`](naming-overview.md) for terminology, before changing a boundary you
   have not worked in.
3. Update contracts before their consumers. Add a focused, deterministic test before changing
   behavior.
4. Keep remote and model I/O out of the authoritative turn path. The Umpire stays deterministic.
5. Run focused tests while iterating, then affected project suites and formatting before review.
   Use the [local testing guide](docs/runbooks/local-testing.md) to choose coverage. CI checks
   exhaustive coverage before merge; `just check` remains the full local release checkpoint.

| Command | What it does |
| --- | --- |
| `just --list` | Show every available recipe |
| `just setup` | Check the SDK version and restore dependencies |
| `just build` | Restore and build the solution |
| `just test` | Build and run all tests |
| `just test-suites` | List focused suites and CI-compatible chunks |
| `just test-suite host` | Build and test only Host and its dependencies |
| `just test-class core '*CampaignContact*'` | Run matching Core test classes |
| `just test-method host '*ForceRound*'` | Run matching Host test methods |
| `just test-shard fast-01` | Run one CI-compatible Core chunk locally |
| `just boundary-check` | Run the user-space disclosure boundary suite |
| `just format-check` | Verify formatting without changing anything |
| `just check` | The full local gate: format, build, boundary tests, all tests |
| `just check-fast` | Same gate, with every CI-compatible shard run concurrently and the known ~29m43s Orleans outlier excluded |
| `just run` | Launch the Aspire development stack |
| `just docs-links` | Check tracked Markdown links, requires Lychee 0.24.2 |

`just check` runs the .NET gates only. It does not run the retained Python Combat contract oracles,
some of which currently fail due to known source-pin drift. Check the
[verification inventory](docs/research/combat-verification-pin-maintenance.md) before treating an
older passing run as current evidence on Combat contracts specifically.

CI splits the suite across parallel jobs to keep the required pipeline fast; the target is under
ten minutes. Adding a large test class may require updating the
[long-suite manifest](.github/scripts/long-suite-weights.md). An optional, label-triggered PR
review pass for high-risk changes is also available; see the
[runbook](docs/runbooks/optional-code-review.md). It supplements ordinary CI and review, and does
not replace either.

## Setup and configuration notes

- **.NET SDK pin.** `global.json` pins the exact SDK feature band (10.0.302 or later) and selects
  Microsoft.Testing.Platform as the test runner. If `dotnet --version` does not match, start there.
- **Build artifacts.** Everything under `artifacts/` is generated. Do not commit `artifacts/bin` or
  `artifacts/obj`.
- **Optional: Ruflo MCP server for Codex.** If you use Codex and want Ruflo's tools available,
  install Ruflo, then add a project-scoped `.codex/config.toml`:

  ```toml
  [mcp_servers.ruflo]
  command = "npx"
  args = ["-y", "ruflo@latest", "mcp", "start"]
  startup_timeout_sec = 30
  ```

  Then run this once per checkout and restart Codex:

  ```sh
  npx ruflo@latest init --minimal --skip-claude --no-mods --no-codex-detect --no-skills-sh --no-global --no-signup
  ```

  `codex mcp get ruflo` confirms registration. The `.claude-flow/`, `.swarm/`, `ruvector.db*`, and
  `.codex/config.toml` files it creates are checkout-specific and intentionally untracked.
- **Security reports.** See [`SECURITY.md`](SECURITY.md).

## Further reading

- [Private Combat evidence revisions](docs/specs/combat-evidence-revisions-v1.md): frozen legacy/PR334/Snapshot dependency sets, replay compatibility, consumption-time rechecks, and explicit original P01 archive selection.

- [Documentation index](docs/README.md): every spec, design doc, and research note
- [Technical design](tech-design.md): architecture, authority boundaries, integration rationale
- [Naming and vocabulary](naming-overview.md): what things are called and why
- [Contributor guide](CONTRIBUTING.md): the full branch, verification, and PR workflow
- [Roadmap](docs/roadmap/pre-alpha-roadmap.md): current checkpoint and what is gated behind what

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers. Maproom
> shows.

## License

No license has been selected yet. All rights are reserved until a license file is added.
