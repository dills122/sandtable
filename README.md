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
| Combat and continual cycle | Private Core entry; public activation pending | Task019F3 (PR #169) implements private native actual prepared-round entry and cancelled no-attack Release for both supported opening histories with two independently trusted ledgers. Task019F4 (PR #172) adds private no-cost actual assault admission and pure arithmetic assessment. Task019F5 (PR #175) adds the private actual paid-assault executable reference through immediate settlement and CA completion into same-cycle Reserve Release. Task019F6 (PR #177) implements the merged private native paid-assault consumer with exact reference byte parity and focused native validation. PAC01 (PR #178) freezes the actual-source Release and guarded repeat/finish contract. CS01 implements its private native consumer through the Movement/Convoy boundary. NRM01 adds private native actual repeated Movement with cumulative costs and original provenance. C02 (PR #182) freezes the private actual repeated Movement completion executable contract. C04 adds its private native consumer through mandatory empty System resolution and ordinal2 Breakdown Determination. B02 (PR #185) adds the private actual ordinal2 Breakdown completion reference through one System event into Combat Position Determination. B03 provides its private native seven-ledger consumer and codec; independent review, hosted CI and public activation remain separate gates. R03's authentic moved-repeat Breakdown reference passed 300 terminal replays and vector audit; final cut audit, review, freeze and CI remain open before native consumption. Production ingress, later-II/consumed Reserve lineage, full-cycle proof and public Combat remain open. |
| Exercise and Maneuver tools | Working | Deterministic single runs, multi-run matrices, paired comparisons, strict readback, and evidence bundles are available from CLI. |
| User interface | Angular foundation available | `src/Cna.Maproom` contains `maproom-alpha`, the Field Atlas library and Storybook. Synthetic shell and draft review are local; connected campaign/hot-seat client remains pending. `site/` is project website only. |
| Published scenario | Not started | First target is six-turn, Land-only *Graziani's Offensive* after working Combat loop. |
| Durable save/resume | Not started | Replay/checkpoint contracts exist; user-facing campaign persistence comes later. |
| Model-backed commanders | Scaffold only | Gateway and worker exist, but no provider is configured and AI never owns authority. |

Current boundary in plain language: you can build engine, run full test suite, launch Aspire service
stack, and simulate checked rules-lab Movement/Reaction/Breakdown paths. You cannot yet play a
campaign or resolve Combat through public Runner actions. Combat internals are exercised through
dormant Core tests; they are not yet exposed as playable actions.

The private [actual Reaction-to-Combat entry contract](docs/specs/combat-actual-reaction-combat-entry-v1.md)
and native reader authenticate ordinary outward/return Movement and all three direct Reaction closure
reasons through stop, System resolution, Movement completion and Breakdown into positive Combat entry.
They preserve CP4 and original RNG with separate trusted ledgers and were merged in PR #187.
The private [moved-assault successor](docs/specs/combat-actual-moved-assault-v1.md) carries that actual
entry through Selection, a sealed Round, cumulative paid costs and settlement into same-cycle Reserve
Release entry. It authenticates six independent ledgers and merged in PR #188.
The private [moved continuation](docs/specs/combat-actual-moved-continuation-v1.md)
completes empty Reserve Release for paid and armed parents and derives immutable prospective assessment.
It merged in PR #189 and opens no cycle-control window.
The additive private [moved cycle control](docs/specs/combat-actual-moved-cycle-control-v1.md)
adds eight-ledger control for both modes: real same-slot phase finish or a distinctly typed pending
repeat intent with execution unavailable. Repeat intent retains the current occurrence and history.
N3 merged in PR #190. Its pending intent remains non-executable by itself.
The private [moved repeat Movement successor](docs/specs/combat-actual-moved-repeat-movement-v1.md)
consumes authentic paid and armed pending intents through eight ancestor ledgers and a ninth Movement
ledger. One System publication opens ordinal2; ordinary owner moves retain cumulative CP, apply
breakoff and incremental DP once, and preserve original history and RNG. The original MovementEnd
remains ordinal1 evidence. PR #192 merged at `3a7b626` after independent review and all 3,391 hosted test rows passed.
Post-merge [main CI run 38783352632](https://github.com/dills122/sandtable/actions/runs/38783352632)
verified all 3,391 rows at `3a7b626`, attempt 1. This R01 family remains move-only.

The separate private [repeat Movement completion successor](docs/specs/combat-actual-moved-repeat-movement-completion-v1.md)
(R02) authenticates all 10 independent ledgers. True-zero Movement completes with one owner event;
positive Movement requires owner stop, empty System resolution and owner completion, three events.
It retains both the historical ordinal1 proof and the receipt-bound ordinal2 proof, then enters
Breakdown Determination without executing Breakdown. Local validation passed all 325 rows across 20
fresh class processes; independent implementation review is complete at `07fac9f`.
[PR #194](https://github.com/dills122/sandtable/pull/194) merged at `121baa5`.
[Main CI run 38830848442](https://github.com/dills122/sandtable/actions/runs/38830848442), attempt 1,
verified all 3,716 rows across 26 execution processes. Public Combat,
Runner adoption, acceptance of all 72 criteria and the first release candidate remain open.

Next delivery sequence:

1. Build on CS01's private native [actual post-assault continuation](docs/specs/combat-actual-post-assault-continuation-v1.md)
   and the private [actual repeated Movement reference](docs/specs/combat-actual-repeated-movement-v1.md)
   and [actual completion contract](docs/specs/combat-actual-repeated-movement-completion-v1.md)
   and the [actual Breakdown completion contract](docs/specs/combat-actual-repeated-breakdown-completion-v1.md)
   and the [authentic moved-repeat Breakdown reference](docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.md)
   to continue from C04's private native completion through the private reference Combat-entry boundary into the remaining selected-branch lineage. CS01's full
   paid history and four independent ledgers reach empty Release and guarded repeat/finish;
   native continuation stops at ordinal2 Movement opening; NRM01 consumes the Python contract for up to four safe
   native ordinary moves with cumulative CP/DP and original provenance; C04 completes them with an actual receipt-bound current proof. Complete later-II/consumed Reserve and production
   ingress authentication before activation. Existing synthetic settled contexts do not prove
   reachability from actual opening history.
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

The [local-server and Angular Maproom plan](docs/design/local-game-server-maproom-plan.md) brings
forward a bounded developer playtest lane: Angular/Material/CDK, Tailwind, typed Reactive Forms
and Formly, SignalStore, HTTP and SignalR, with an initial SVG map. The
[Field Atlas design system](docs/design/maproom-design-system-v0.md) and
[operations workspace preview](docs/design/maproom-style-study/index.html) establish warm paper,
ink and restrained unit symbols through a compact map, order workflow and linked formation ledger.
MAP-02 now supplies the theme and initial components; MAP-03A/B/C complete these workflows before expanding the library.
Temporary rules-lab sessions stop at supported
public boundaries; durable hosting and complete scenario play retain their existing gates.

The [Angular workspace](src/Cna.Maproom/README.md) now implements MAP-01 and the MAP-02 foundation.
Run `just maproom-setup`, then `just maproom` (port 4200) and `just field-atlas` (port 6006) in separate
terminals. Storybook owns the live component/pattern reference; both surfaces consume
`@sandtable/field-atlas`. Complete MAP-03 workflows and the campaign connection remain pending.

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

### Optional Ruflo MCP for Codex

Ruflo runs as a project-local stdio MCP server. After installing Ruflo, initialize its runtime
in each checkout and add a project-scoped `.codex/config.toml` with a host-local command:

```toml
[mcp_servers.ruflo]
command = "npx"
args = ["-y", "ruflo@latest", "mcp", "start"]
startup_timeout_sec = 30
```

Run `npx ruflo@latest init --minimal --skip-claude --no-mods --no-codex-detect --no-skills-sh --no-global --no-signup` once in that checkout, then restart Codex. Use `codex mcp get ruflo` to check registration. Local `.claude-flow/`, `.swarm/`, `ruvector.db*`, and `.codex/config.toml` stay out of Git because they can contain checkout-specific state and paths.

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

CI separates expensive tests into eight required long-running suites, alongside four ordinary Core
suites, the three R02 suites, Exercise and Contracts. The matrix permits all 17 suites to run in
parallel, subject to shared GitHub runner availability. Formatting is a separate required job that
runs alongside preparation; aggregate verification requires both formatting and every test suite.
All test jobs retain a 15-minute deadline. The delivery target is a complete required pipeline under
ten minutes, with five to six minutes as a stretch; hosted performance validation is still pending.

The prepared inventory assigns every actual test row exactly once. Fifty-two groups split costly
legacy classes across fresh processes, while ordinary Core classes remain whole in four processes.
Together with 20 R02 processes and Exercise/Contracts, the baseline has 78 execution processes and
3,716 test rows. Long suites permit at most four concurrent child processes. R02 retains its existing
resource limits and the focused support class runs alone after job1's parallel children drain.
Source revision, attempt, SDK, binary and fixture fingerprints, actual selectors, process exits and
XML identities remain part of the required proof. Missing, duplicate or unexpected rows fail the gate.

The historical [Core-B investigation](docs/research/2026-10-13-rmc-core-b-investigation.md) and
[earlier capacity split](docs/research/2026-10-14-ci-core-capacity-proposal.md) explain the preceding
layout; their timings do not establish this layout's performance. The
[implementation checkpoint](docs/work/2026-10-16-ci-long-suite-isolation.md) records the new design
and focused local measurements; [manifest maintenance](.github/scripts/long-suite-weights.md)
explains how test additions update the exhaustive allocation.

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
