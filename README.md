# Sandtable

> Command the campaign. Let the Umpire handle the paperwork.

Sandtable is an independent, unofficial implementation of the rules of the 1979 board wargame *The
Campaign for North Africa: The Desert War, 1940–43* (see [IP and non-affiliation](#ip-and-non-affiliation)).
The original game simulates the desert war at an extreme level of detail:
formations, supply, transport, hidden information, all of it. Sandtable keeps those decisions
intact for the player and hands the bookkeeping to the computer: legality checks, dice, hidden
information, and the campaign record.

This is not a simplified game using the same theme. The rules target is the original 1979 SPI
edition, corrected by the September 1979 errata. Any place that required a judgment call is
recorded explicitly rather than quietly decided.

The original game's box art, logos, counter art, scans, and page images are not part of this
repository. They stay with the published game. Transcribed charts, tables, rosters, and a map
derived from third-party data are part of this repository; see
[IP and non-affiliation](#ip-and-non-affiliation).

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

The [durable Orleans hosting plan](docs/work/plans/2026-10-24-orleans-hosting.md) records the
path to multi-server campaigns. Its storage and scheduling groundwork has landed:

- The durable campaign journal runs on PostgreSQL + Marten
  ([migration design](docs/work/plans/2026-11-18-postgres-migration-design.md),
  [issue #439](https://github.com/dills122/sandtable/issues/439)).
- Outside Development, Orleans clustering and reminders use Postgres through AdoNet, so the Host
  is no longer limited to a single dev machine for clustering.
- A measured [capacity budget](docs/work/plans/2026-11-20-phase-d-capacity-latency-benchmark.md)
  supports about 100 active campaigns per silo with histories under about 50 accepted steps. It
  requires Server GC and a capped Postgres connection pool.

There is still no released deployment: publication certification and release packaging remain
open.

## Tech stack

**Simulation engine and backend.** C# on .NET 10, run locally through .NET Aspire. Campaign hosting
runs on Orleans grains. The durable campaign journal is stored in PostgreSQL through Marten, and
round deadlines are enforced by per-campaign Orleans grain timers with a reminder backstop. The
rules engine (`Cna.Core`) has no dependency on any of that: it is pure, in-process C# so it can be
tested and replayed in isolation. An Intelligence Gateway speaks gRPC, keeping any future
model-backed opponent on the other side of a firm boundary from authoritative game state.

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
| `src/Cna.OrleansHost` | Campaign hosting: Orleans grains, the durable PostgreSQL + Marten campaign journal, the trusted server side. |
| `src/Cna.AppHost` | .NET Aspire entry point that wires up the local development service stack. |
| `src/Cna.Maproom` | The Angular frontend: map, orders, formation ledger. See its own [README](src/Cna.Maproom/README.md). |
| `src/Cna.Game.Contracts` | The typed HTTP contract between Maproom and the host, including a generated client. |
| `src/Cna.Intelligence.Gateway` / `Cna.Intelligence.Contracts` | The gRPC boundary for any future model-backed commander. |
| `src/Cna.ExerciseRunner` | CLI for running and comparing deterministic simulation scenarios without a UI. |
| `tools/Cna.LoadHarness` | [Development-only in-process Host load harness](tools/Cna.LoadHarness/README.md); standalone build outside the solution. |
| `src/Cna.DecisionWorker`, `Cna.ServiceDefaults` | Supporting services for the Aspire stack. |
| `docs/` | Specs, design docs, research notes, and the current roadmap. |

## Where things stand

> [!IMPORTANT]
> Sandtable is a tested pre-alpha simulation engine, not yet a playable adaptation of the published
> game. Current scenarios are synthetic rules-laboratory fixtures, not released campaign content.

| Area | Status | What that means |
| --- | --- | --- |
| Deterministic Umpire | Working | Versioned rules, seeded randomness, canonical commands and events, replay, checkpoints, and side-safe action boundaries are implemented. |
| Playable rule path | Working through Combat entry | A runner executes Initiative, stage preamble, Reserve Designation, Movement, bounded ZOC/Reaction, and Breakdown, then stops before Combat adjudication. |
| Combat and the continual turn cycle | Bounded native implementation | Contact-based Core and normal durable Host cover an initial assault plus two selected repeated assaults, settlement, custody, and Reserve Release. Development loopback access is not public Combat certification; broader support and evidence gates remain open. |
| Exercise and Maneuver tools | Working | Deterministic single runs, multi-run matrices, paired comparisons, and evidence bundles, all from the CLI. |
| User interface (Maproom) | Angular foundation, local read-only preview, plus a wired but unreachable action-submission path | A synthetic fixture study and a local, URL-backed campaign read mode. A real HTTP action-submission adapter and a real backend trust signal (bootstrap's `actionSubmissionTrusted`) are now wired end to end into `canSubmit` and covered by a browser test. No mounted route yet advances a campaign past its first System-only decision, so the bundled fixture never reaches a human decision point: nothing is actually clickable yet. |
| Published scenario | Source data in progress | Six-turn Graziani's Offensive is the first target; historical campaign admission and the required Combat loop remain incomplete. |
| Durable save and resume | Durable Host implemented; product gate open | PostgreSQL + Marten journal and cold replay support bounded campaigns. Journal schema 14 persists Selection, RBA, Force, Retreat, Custody, Reserve Release and cycle-control deadlines at decision entry. Per-campaign Orleans grain timers, backed by reminders, expire those deadlines without any owner query. HOST-PUB-001 publication certification and user-facing campaign lifecycle acceptance remain open. |
| Model-backed commanders | Scaffold only | The gateway and worker processes exist. No model provider is configured, and AI will not hold authority over the rules. |

### In plain terms

What you can do today:

- build the engine and run the full test suite
- launch the Aspire service stack
- simulate the currently checked Movement, Reaction, and Breakdown paths
- exercise native Combat on bounded local paths (public Combat certification remains open)

**You cannot yet play a public campaign end to end.**

Recent rule-path changes:

- **Acting last.** When an initiative holder elects to act last, the normal Combat owner and its
  timeout use the recorded first-acting side. Older decisions remain replayable.
- **Zones of control (World6).** World6 can calculate source-qualified ZOC, but its admitted content
  profile still excludes the stacks needed to produce one. Historical armor, reconnaissance and
  anti-tank ratings can feed that calculation without being admitted for combat. Artillery
  position and player-safe ZOC disclosure remain open.
- **The second acting side.** Through the normal journal:
  1. An owner can explicitly pass the first side's convoy, rail, towing, maintenance and patrol
     phases. The checkpoint then reaches the second side's Reserve Designation.
  2. The second side can designate eligible units and complete its reserve phase.
  3. An eligible unit can move one hex and stop. Contacts and any Reaction window are retained
     through replay.
  4. An explicit owner completion advances to Breakdown when no Reaction is pending. A bounded
     no-breakdown step (V46) then leads into the second side's Combat selection (V47).

  Still incomplete: Reaction handling after a second-side move, vehicle breakdown, and the optional
  convoy, rail, towing, maintenance and patrol activities themselves.
- **Exercise Runner and Combat.** Runner setups still stop at Combat entry
  ([#448](https://github.com/dills122/sandtable/issues/448)). Task 020 is adding a Core-owned,
  audience-safe Combat decision surface so the runner can eventually drive full games through
  Combat. That work is partial and not certified; see the
  [Task 020 scoping note](docs/work/plans/2026-11-20-task-020-combat-legal-actions-scoping.md).

The [October 24 baseline evidence matrix](docs/work/reviews/2026-10-24-rc-baseline-evidence.md)
separates these paths from original P01 certification and remaining release gates.

### Graziani's Offensive: scenario and setup progress

The first published scenario is being assembled from source data. Most of this work is internal,
synthetic, or development-only. None of it makes the scenario playable yet, and historical
campaign admission remains unsupported.

Planning documents:

- The [RC checklist](docs/work/plans/2026-10-24-rc-checklist.md) holds current implementation
  evidence, open release gates and explicit pass criteria.
- The [six-turn working inventory](docs/work/plans/2026-11-12-graziani-playable-rc-inventory.md)
  maps scenario obligations and the remaining path to a playable release.
- The [setup draft](docs/specs/graziani-setup-draft.md) records open admission decisions.

**Scheduled arrivals (synthetic and internal groundwork).**

- A source-neutral [scheduled-arrival index](docs/specs/scheduled-arrival-index-v1.md) has
  synthetic tests but does not yet provide campaign arrival authority.
- A [synthetic obligation laboratory](docs/specs/scheduled-arrival-lab-v1.md) exercises owner
  choices, other obligations and replay without advancing a campaign.
- A [content draft analyzer](docs/specs/campaign-scheduled-arrival-content-draft-v1.md) checks
  deferred units against real content definitions without admitting them.
- A [draft identity and world contribution](docs/specs/campaign-arrival-draft-identity-v1.md)
  binds candidate content and materializes synthetic arrival values, without campaign authority.
- An internal [arrival replay draft](docs/specs/campaign-arrival-replay-draft-v1.md) exercises
  canonical event bytes and receipt recovery. It does not provide durable campaign storage.
- The [synthetic arrival profile](docs/specs/campaign-synthetic-arrival-profile-v1.md) fixes one
  repository-owned fixture. It is not an admitted campaign setup.
- An isolated [synthetic arrival phase](docs/specs/campaign-synthetic-arrival-phase-v1.md)
  exercises a Land-sequence transition, side-specific offers and replayed checkpoints.
- An internal [World6 arrival cut](docs/specs/campaign-arrival-world6-successor-v1.md) and
  [durable journal path](docs/specs/campaign-scheduled-world6-durable-host-v1.md) exercise
  synthetic creation, replay and owner receipt recovery. New journal records retain native
  preamble event bytes; older digest-only records remain readable. No public campaign route uses
  this cut.

**Scenario content and the initial world.**

- The production [scenario content model](docs/specs/campaign-scenario-arrival-content-v1.md) can
  describe sourced scheduled arrivals and grouped initial counters. Its versioned content formats
  preserve and hash those records, plus source-listed truck quantities, partial combat and
  weapon-source facts, and TOE seeds. Legacy formats reject the new records.
- An internal [initial world constructor](docs/specs/campaign-world-v8.md) places grouped counters
  and retains deferred arrivals from those records. It does not admit a campaign.
- The normal durable journal can persist and cold-replay a
  [source-backed initial world](docs/specs/campaign-source-creation-v1.md) with its pending groups
  and exact content bytes. This path covers initialization only: it does not execute arrivals or
  make a scenario playable.
- The production `Cna1979GrazianiCairoSourceExcerpt` builder provides the source-backed Cairo
  excerpt used in tests. Its map and formation data remain incomplete.

**Pre-game setup (provisional).**

- A provisional pre-game slice records an Axis GT2 supply availability roll and partial lane
  allocations through the normal session and durable Host journal.
- The development-only local API exposes that slice to an authenticated Axis seat. An operator can
  create it without uploading a content pack via `POST /api/v2/campaigns/pregame-source/graziani`.
  The [pre-game supply spec](docs/specs/graziani-pregame-supply-v1.md) lists its required
  initialization fields and its incomplete, provisional scope.
- The [version 4 source authority](docs/specs/graziani-source-authority-v4.md) commits that plan
  and records bounded, private Axis and Commonwealth supply setup choices in the same journal,
  with source-backed initial-force evidence.
- Optional operator [source-force upgrades](docs/specs/graziani-source-forces-v5.md) retain further
  historical Axis and Commonwealth evidence, player setup choices, and selected source-backed
  units in the World8 state. They cannot complete setup, advance the campaign, or execute the
  Polish arrival. Grouped formations, map and logistics remain incomplete.
- Formation groupings available through normal source setup actions and journal replay:
  - The Commonwealth can confirm a source-backed 4th Indian Division grouping and reserve its
    existing motor pool for Central India Horse.
  - The same grouped source world can retain the confirmed Matruh garrison and Maletti, including
    their selected detachments, and propose both Libyan divisions for Axis confirmation.
  - The Host can admit proposed 62 Marmarica, 63 Cirene and 64 Catanzaro groups for Axis
    confirmation, and 7th Armored and 16th Brigade groups for Commonwealth confirmation.

  The 4th Indian, Matruh, Maletti and Libyan grouping state is replayable but is not yet accepted
  by native movement or combat.
- Axis can optionally preplan GT3–4 supply arrivals in September through the same setup actions;
  those arrivals land in October. Other convoy cargo remains unresolved, and no delivery is
  implied.
- Both owners can finish or reopen setup privately.
- Supported source setup choices can also be queried and submitted through the normal legal-action
  interface; the owning player receives its setup context and provisional rulings.
- Through that interface the Commonwealth can assign the initial 1 RTR and 11 Hussars truck pools
  and choose their initial loads. Finishing with empty trucks clears their load-specific blocker.
  Loaded-truck consumption and detachment remain unsupported, and other blockers still prevent
  the source turn from starting.

**The first game turn (source-turn authority).** A source-turn authority retains the grouped
state and random stream. It includes bounded GT1 planning, tactical-shipping declines, initiative
declaration, weather, selected convoy deliveries and a single-hex Polish arrival. **Its start
gate still rejects the incomplete historical map, control and contact coverage, so none of the
steps below can yet be reached through a real campaign.**

1. After Australian training is interrupted and both sides finish opening Organization choices, a
   journaled GT1 completion step applies the provisional no-prior-construction ruling.
2. GT1 stage-one convoy arrivals can close only after all due Axis lanes are delivered, no units
   remain due, and the Commonwealth has skipped opening supply.
3. The following stage-one fleet step applies the scenario's GT1 movement ban and undamaged
   starting state. Later fleet operations remain unsupported.
4. The first acting player can decline reserve designation and enter its movement segment through
   the normal owner-action interface. Version-3 turns also let that player finish movement and
   advance to breakdown determination.
5. The bounded no-breakdown successor supports either no movement or the exact 4th Indian opening
   move. Other movement histories remain blocked there.

**Map.** A [pinned historical map transcription](docs/specs/graziani-historical-map-transcription.md)
supplies the opening-road and Cairo-entry consistency checks, and resolves same-sheet contact
geometry for map-bound source turns. New source-turn checkpoints bind the pinned map identity and
three provisionally approved C/D crossings; legacy turn records keep their original geometry and
checkpoint version during replay. Apart from those three crossings, remaining seam edges and
grouped control are unadmitted, and general cross-sheet movement remains blocked.

**Journal and content plumbing.**

- The optional compact source checkpoint reduces new journal rows, but the complete journal must
  still be replayed from genesis.
- Normal gameplay creation and journal replay can use a supplied, identity-checked Content 6
  resolver. The default Host still uses the synthetic catalog, and resolver content must remain
  available across restarts.

### Synthetic HTTP turn-boundary checkpoint

The opt-in `rules-lab.breakdown.turn-boundary.gt1.v1` and `rules-lab.breakdown.turn-boundary.gt12.v1`
setups drive a complete initial turn and the successor turn's prelude to its first Reserve
Designation opening. The second setup crosses the Fall/Winter boundary. Existing setups are
unchanged and refuse this rollover path. The bound is the lower of the declared scenario end and
Weather's maximum game turn; terminal scoring and later-turn Reserve submission are not wired.
The Development/loopback HTTP host replays and drains these boundaries through the existing
stage-entry declaration and Reserve opening names. Later-turn Reserve submission remains unsupported.

### Further detail

For every contract, every merged slice, and every open gate, see the
[current roadmap](docs/roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates), the
[first-release audit](docs/research/2026-10-05-first-release-audit.md), and the
[Combat implementation plan](docs/design/combat-cycle-implementation-plan.md).

## Getting started

### Prerequisites

- [.NET SDK 10.0.302 or later in the .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), pinned in `global.json`
- [Just](https://just.systems/), optional; the commands below assume it
- Git
- Docker, for the `Cna.OrleansHost` tests (they start a disposable PostgreSQL container through
  Testcontainers) and for the containerized Proving Ground preview

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

The durable campaign journal is off unless `SANDTABLE_POSTGRES_HOT_CONNECTION_STRING` and
`CampaignJournal:OperatorSecret` are set. Copy [`.env.example`](.env.example) to `.env` for local
Postgres values. Outside Development the Host also requires
`SANDTABLE_POSTGRES_ORLEANS_CONNECTION_STRING` for clustering and reminders; Development keeps
localhost clustering.

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

CI runs in three tiers:

- **Pull requests** run only the `fast-*`/`r02-*`/`exercise`/`contracts` shards (`long-*`
  excluded), targeting under ten minutes. PRs confined to `site/`, `src/Cna.Maproom/`,
  `tools/`, `deploy/`, or documentation-only changes skip .NET work; `format` and `verify`
  still report success. Closing a PR cancels superseded CI runs.
- **Pushes to `main`** run the full suite as the post-merge safety net; new pushes cancel older
  runs.
- **Nightly** CI runs the full suite in a separate concurrency group, including `long-*`.
  The separate `.github/workflows/ci-nightly-heavy.yml` runs the six `p01-*` proof families
  and Orleans. Offline documentation links run on Markdown changes or manual dispatch.

Adding tests requires registering each new row by adding one new entry file under
`.github/scripts/long-suite-weights/` (see the
[long-suite manifest guide](.github/scripts/long-suite-weights.md)). An optional, label-triggered PR
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

- [Documentation index](docs/README.md): every spec, design doc, and research note
- [Technical design](tech-design.md): architecture, authority boundaries, integration rationale
- [Naming and vocabulary](naming-overview.md): what things are called and why
- [Contributor guide](CONTRIBUTING.md): the full branch, verification, and PR workflow
- [Roadmap](docs/roadmap/pre-alpha-roadmap.md): current checkpoint and what is gated behind what
- [Private Combat evidence revisions](docs/specs/combat-evidence-revisions-v1.md): frozen
  legacy/PR334/Snapshot dependency sets, replay compatibility, consumption-time rechecks, and
  explicit original P01 archive selection

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers. Maproom
> shows.

## License

The Sandtable source code is licensed under the [Apache License, Version 2.0](LICENSE). See
[NOTICE](NOTICE) for attribution and the non-affiliation statement. Contributions are accepted under
the same license (section 5 of the license).

### Material not licensed under Apache-2.0

The Apache-2.0 license covers original Sandtable code and original engineering writing. It does
**not** cover material that reproduces or closely restates the published game's charts, tables,
rosters, scenario setups, or map, nor related research notes. Rights-holder review of that
source-derived data has not been completed, so no license is granted to view, copy, redistribute, or
reuse it, and being publicly visible in this repository does not change that. This is a provisional,
conservative position, not legal advice, and is subject to owner and counsel review.

Not licensed under Apache-2.0 (provisional):

- Rules tables in `src/Cna.Core/Rules/`: `Cna1979CombatAdjudication.cs`, `Cna1979Weather.cs`,
  `Cna1979Breakdown.cs`, `Cna1979Movement.cs`, `Cna1979InitiativeRatings.cs`,
  `Cna1979HistoricalTerrainEffects.cs`
- Rosters and setup facts in `src/Cna.Core/Content/`: `Cna1979GrazianiSourceFacts.cs`,
  `Cna1979GrazianiCounterFacts.cs`, `Cna1979GrazianiCairoSourceExcerpt.cs`
- Map: `src/Cna.Core/Content/HistoricalMap/` (data) and `Cna1979HistoricalMap.cs`
- `src/Cna.Core/Campaigns/CampaignSource*.cs` (source-derived campaign setups; conservative glob)
- Tests and fixtures that embed the above: `tests/Cna.Core.Tests/Rules/Fixtures/*.golden.json` and
  test files that reference `ContentOriginKind.SourceDerived`, `spi-1979-` source ids, or
  `Cna1979GrazianiSourceFacts`
- Source fixtures and evidence: `docs/research/fixtures/`, `docs/specs/fixtures/` (including the
  `combat-evidence-*.zip` bundles and `combat-content-v7.*.json`)
- Source-derived research and notes: `docs/work/plans/*graziani*`,
  `docs/work/plans/verify-graziani-*.py`, `docs/research/*source*`,
  `docs/research/combat-source-freeze-v1.md`, `docs/specs/graziani-*.md`,
  `docs/specs/weather-determination-v1.md`, `docs/specs/combat-weather-v1.md`

Apache-2.0 applies to everything else, including the remainder of `docs/` (design, contracts,
roadmap, runbooks, and CI notes), `.planning/` (internal working notes with no game content),
`scenarios/`, the synthetic rules-lab fixtures in `src/Cna.Core/Content/Fixtures/`, and the
`*ContentCatalog.cs` loaders. Third-party fonts and icons (Newsreader, IBM Plex Sans, Lucide) are
distributed under their own licenses; texts are in `src/Cna.Maproom/projects/field-atlas/licenses/`.

## IP and non-affiliation

Sandtable is an independent, unofficial implementation. It is not affiliated with, endorsed by, or
sponsored by SPI or any current or former rights holder of *The Campaign for North Africa*. Names
are used only to identify the game that Sandtable implements.

The repository contains no original box art, logos, counter art, map or chart images, scans, or
rulebook pages. It does contain transcribed charts and tables, unit rosters, and a map derived from
third-party data, plus notes that paraphrase rules with section citations. Rights-holder review of
that source-derived data has not been completed; see the carve-out above.

Short form for the site footer: *Independent, unofficial project. Not affiliated with or endorsed by
SPI or any rights holder of The Campaign for North Africa. No original art, scans, or rulebook pages
included.*
