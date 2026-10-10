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

## Project at a glance

> [!IMPORTANT]
> Sandtable is a tested pre-alpha simulation engine, not yet a playable adaptation of the published
> game. Current scenarios are synthetic rules-laboratory fixtures, not released campaign content.

| Area | Current state | Meaning |
| --- | --- | --- |
| Deterministic Umpire | Working | Versioned rules, seeded randomness, canonical commands/events, replay, checkpoints, and side-safe action boundaries are implemented. |
| Playable rule path | Working through Combat entry | Runner can execute Initiative, stage preamble, Reserve Designation, Movement, bounded ZOC/Reaction, and Breakdown, then stops before Combat adjudication. |
| Combat and continual cycle | Private Core adapters; public activation pending | Reviewed internals cover settlement, Reserve Release, bounded released-I Movement and guarded repeat/finish. Native settled control retains synthetic earlier Movement. Task019F1 (merged in PR #165) implements the private actual-selection contract for both supported opening histories through Force Assignment after defender decline, or seven no-attack Reserve Release fallbacks per owner. Force Assignment completion and production input authentication remain gated. Actual round/result, full-cycle proof and public Combat remain open. |
| Exercise and Maneuver tools | Working | Deterministic single runs, multi-run matrices, paired comparisons, strict readback, and evidence bundles are available from CLI. |
| User interface | Not started | `site/` is project website only. Maproom hot-seat client is future work. |
| Published scenario | Not started | First target is six-turn, Land-only *Graziani's Offensive* after working Combat loop. |
| Durable save/resume | Not started | Replay/checkpoint contracts exist; user-facing campaign persistence comes later. |
| Model-backed commanders | Scaffold only | Gateway and worker exist, but no provider is configured and AI never owns authority. |

Current boundary in plain language: you can build engine, run full test suite, launch Aspire service
stack, and simulate checked rules-lab Movement/Reaction/Breakdown paths. You cannot yet play a
campaign or resolve Combat through public Runner actions. Combat internals are exercised through
dormant Core tests; they are not yet exposed as playable actions.

Next delivery sequence:

1. Define the actual Force Assignment completion and first-round entry contract, then join actual
   round/result settlement and complete later-II/consumed Reserve lineage (remaining Tasks017–019).
   The existing actual-selection contract stops at Force Assignment after defender decline; it does not complete Force Assignment or
   activate gameplay. Existing synthetic settled contexts do not prove that those paths are
   reachable from actual opening history.
2. Activate certified, side-safe public Combat actions (Tasks020–021), then prove Exercise/Runner
   replay and repeatability (Tasks022–024).
3. Reconcile all 72 acceptance criteria and demonstrate the authentic continual cycle (Task025).
   Actual publication evidence remains a separate obligation under HOST-PUB-001.
4. Measure and implement the exact six-turn scenario content and remaining Land/victory rules,
   then add durable save/resume and minimal hot-seat Maproom. MVP exit requires two complete,
   reproducible six-turn games.

See the [first-release audit](docs/research/2026-10-05-first-release-audit.md) for the measured release gaps,
and [current roadmap](docs/roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates) for
authoritative status and [Combat plan](docs/design/combat-cycle-implementation-plan.md) for detailed
task graph.

## Start, run, and develop

### Prerequisites

- [.NET SDK 10.0.302 or later .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Just](https://just.systems/) for short commands (optional)
- Git
- Docker only for future container-backed Aspire resources; current stack does not require it

`global.json` selects .NET 10 and Microsoft.Testing.Platform. Check installed SDK with
`dotnet --version`. Use `dotnet test --solution` for the solution and `--project` for a test project;
do not pass a bare positional test path.

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

### Run service stack

```sh
just run
```

Equivalent command:

```sh
dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
```

Open Aspire dashboard URL printed in terminal. It shows Orleans host, Decision Worker, and
Intelligence Gateway. This launches development services—not a playable Maproom. Stop with
<kbd>Ctrl</kbd>+<kbd>C</kbd>.

### Run deterministic simulation

Run current bounded Reaction Maneuver and write validated artifacts under `artifacts/exercises`:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Runner prints child bundle paths, aggregate report path, and deterministic fingerprint. More
checked manifests and diagnostic modes are listed in [Exercise Runner runbook](docs/runbooks/exercise-runner.md).

### Preview project website

```sh
python3 -m http.server 4173
```

Open `http://localhost:4173/site/`. Website is dependency-free project documentation, not game UI.

### Daily development loop

```sh
git switch -c codex/my-change
just check
```

Use feature branch; never commit directly to `main`. Read [contributor guide](CONTRIBUTING.md),
[architecture](tech-design.md), and [vocabulary](naming-overview.md) before changing boundaries.
Update contracts before consumers, add focused deterministic test before behavior, keep remote/model
I/O outside authoritative turns, then run `just check` before PR.

| Command | Purpose |
| --- | --- |
| `just --list` | Show repository recipes |
| `just setup` | Check SDK and restore dependencies |
| `just build` | Restore and build solution |
| `just test` | Build and run all tests |
| `just boundary-check` | Run user-space disclosure boundary suite |
| `just format-check` | Verify formatting without edits |
| `just check` | Full local gate: format, build, boundary tests, all tests |
| `just run` | Launch Aspire development stack |
| `just docs-links` | Check tracked Markdown links; requires Lychee 0.24.2 |

`just check` runs the .NET gates; it does not run the retained Python Combat contract oracles.
Some of those oracles currently fail: Breakdown and cycle-sequence source pins have drifted,
recursive Snapshot admission rejects that drift, and outward composition has a separate Content
pin failure. Pin maintenance is deferred; other bounded oracle timeouts remain unverified. See the
[verification inventory](docs/research/combat-verification-pin-maintenance.md) before interpreting
historical passing checks as current evidence. Record failures separately from any supplemental
semantic checks.

Build artifacts live under `artifacts/`. Do not commit generated `artifacts/bin` or `artifacts/obj`
content. See [security policy](SECURITY.md) for vulnerability reports.

## Architecture and documentation

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers.
> Maproom shows.

The pure, in-process `Cna.Core` owns authoritative simulation. ExerciseRunner uses its legal-action
and replay path for local developer instrumentation. AppHost launches Orleans host, Decision
Worker and Intelligence Gateway scaffolds; campaign hosting and live decision dispatch are future
integration work. Gateway decision/narrative RPCs currently return `Unavailable`.

- [Documentation index](docs/README.md): capability specifications, designs, research and reviews
- [Technical design](tech-design.md): architecture, authority boundaries and integration rationale
- [Naming and vocabulary](naming-overview.md): product names and module responsibilities
- [Contributor guide](CONTRIBUTING.md): branch, verification and PR workflow
- [Historical implementation ledger](docs/research/2026-10-05-project-documentation-snapshot.md): retained checkpoint evidence

## License

No license has been selected yet. All rights are reserved until a license file is added.
