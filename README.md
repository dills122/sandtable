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
events; explicitly resolve empty Organization, Naval Convoy Arrival, Fleet Assignment, and Fleet
Repair obligations; adjudicate the first-acting side's Reserve Designation; execute supported
first-side non-contact Movement; complete Movement to Breakdown Determination; and
replay those events to byte-identical state. Reserve authority now carries per-element status,
owner-only observation, exact acting-side candidates, closed command mapping, bounded checkpoints,
and canonical designation/completion events. The Movement foundation additionally records exact
per-Operation-Stage expenditure/Cohesion state and opaque one-to-one map representations. It now
also carries typed move/completion candidates, deterministic action identities, an exact side-safe
cost breakdown, strict non-authoritative readback, internal authoritative non-contact move
adjudication/replay, and observation-derived public action membership with exact submission
revalidation. Ruleset manifest contract 7, setup schema 5, snapshot contract 9,
Campaign World snapshot contract 4, Campaign Observation contract 5, legal-action-set contract 2,
and Content Pack schema 4 / canonical format v3 use an original nine-hex,
nonhistorical rules laboratory to develop game systems without redistributing published assets.
Campaign Observation derives deterministic side-safe public topology, audience-visible turn
revision, exact own mobility/ledger/Reserve and approved vehicle-risk facts, plus only opaque
opposing representation/location/current-ZOC rows. It exposes neither complete Content identity,
real opposing bindings/force facts, nor hidden Reserve counts. Legal Actions v1 exposes those
mechanics through an opaque campaign-authority handle,
deterministic system/side action sets, exact-audience membership enforcement, and side-safe
acceptance receipts. Weather Determination v1 resolves corrected source-cited Weather through that
same boundary and records pair-keyed evidence. Operation-Stage Entry v1 then resolves only the four
explicitly admitted empty obligations through mechanic-specific actions and events. Side-safe
queries derive the Reserve audience from the recorded first/second actor order while authoritative
`ActiveSide` remains unset. Raw snapshots, commands, events, content context, projection, and replay
are not public mutation seams.

A separate direct-only dormant Campaign Observation 6 successor now freezes the
`sandtable.observation.zoc-reaction-side-safe.v1` policy, one canonical source-unmapped aggregate
of apparent enemy-controlled locations, and a closed normal/phasing/reacting decision-state union.
Its reacting view contains only the apparent trigger, the observer's current opportunities, and
the optional active own participant; phasing receives only a stable window ID and generic waiting.
A distinct strict projected-history contract retains the same redacted decision state without
authority bindings, source mappings, evidence, or internal reasons. These successors are not
registered on the active Observation 5 query or action path.

The local `Cna.ExerciseRunner` supports that synthetic rules-laboratory path as either one
bounded, deterministic **Exercise** or one serial **Maneuver**. An Exercise uses a fresh opaque Core
capability, selects only current legal actions, stops at its exact declared boundary, proves both
event-history reconstruction and fresh-session re-adjudication, and writes a manifest-last
`trusted-authority` evidence bundle. The original Organization fixture and nine-step Reserve
fixture remain regression checkpoints; a checked 12-step Reserve Designation profile now
designates both eligible elements and completes to first-side Movement. A serial-unpaired Maneuver
strictly admits one canonical ordered `serial-unpaired` manifest,
derives explicit child identities from its sole parent root seed, and runs each child in process
through the same coordinator. Each completed child bundle is read once for semantic validation and
identity-matched aggregation; snapshot facts are accepted only after the complete Core-owned
snapshot/world decoder validates their canonical structure. The resulting transactional report
separates deterministic counts, outcomes, and fingerprint material from noncanonical timing/path
diagnostics and is strictly read back before completion is claimed. Compact, forensic, and debug
Exercise detail tiers expose progressively richer evidence without changing simulation truth. The
current two-setup serial-unpaired Maneuver proves the same Movement-terminal path for predetermined
and contested initiative. A separate checked six-child controller matrix crosses
`act-first`/`act-last` with Reserve `none`/`one`/`all`, producing exact 10/11/12-action Movement
trajectories. A checked six-child Movement Maneuver extends those policies through exact first-side
Breakdown Determination with deterministic 2/1/0 move counts. Optional `serial-paired` Maneuvers run
isolated baseline and candidate arms
sequentially from identical declared initial conditions, initial role-specific random streams,
campaign creation inputs, build cohort, and initial snapshot. Its strictly read-back comparison is
descriptive only: trajectories and random consumption may diverge after the first differing choice,
and it makes no causal, statistical-significance, gameplay-balance, recommendation, or
synchronized-post-divergence claim. Model controllers and side-safe exports are not implemented.

The checked Exercise and serial-unpaired Maneuver profiles use manifest v2, with unpaired report
scheme `sandtable.maneuver-report.v1`; the separate paired Maneuver uses
`sandtable.paired-maneuver-manifest.v1` and
`sandtable.paired-maneuver-report.v1`. All use ruleset v7, snapshot v9, world v4, strict
`trusted-authority` evidence admission, and deterministic v2 controller configuration identity.

The first two retained simulator studies now verify repeated Movement-terminal determinism,
counterbalanced-order timing, and contested root seeds 0-31. Every sampled run passed strict
readback; the results also show that future back-testing needs explicit act-last and Reserve
none/one/all controller profiles rather than seed variation alone. See
[Baseline 1](docs/research/simulator-baseline-1.md) and
[Baseline 2](docs/research/simulator-baseline-2.md). The follow-on
[controller-policy matrix](docs/research/simulator-controller-matrix.md) closes that explicit
coverage gap with 6/6 strictly read-back trajectories and a repeatable aggregate fingerprint.
The merged [Movement trajectory study](docs/research/simulator-movement-trajectories.md) adds 48
repeat trajectories across six controllers and four deliberate seed probes. The follow-on
[Movement cost-sensitivity study](docs/research/simulator-movement-cost-sensitivity.md) adds a
checked paired stable-route/lowest-public-cost comparison; both arms reach Breakdown in 13 actions,
while the first route changes from exact cost 8 to 1/2 under equal initial evidence.

Breakdown adjudication, contact, combat, published scenario content, persistence, and the Maproom
player interface remain future work.

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
| Mandatory turn preamble | Implemented through Reserve Designation completion; authority reaches first-side Movement |
| Movement/contact and combat loops | Movement Foundation is complete through merged `MOV-TASK-010` / PR #79; approved ZOC/Reaction checkpoints `ZOR-TASK-002A`-`004A` now include dormant Rules/Content, World 5/creation 9, Snapshot 10, `ElementMoved` v2 replay, and Observation 6/policy/history seams while the complete legacy runtime identity set remains authoritative; `004B` is next |
| Published first-scenario data, remaining Land rules, victory, persistence, and Maproom | Milestone-level; not started |
| Player Intent Composer | Direction reviewed; representative decision after the combat skeleton, no-model prototype before Maproom, optional parser evaluation after deterministic MVP |
| Exercise Harness | Single-Exercise, serial-unpaired two-setup/controller/Movement Maneuvers, and optional serial-paired Reserve-policy and Movement-cost descriptive comparisons implemented with strict readback |

The approved high-level path to a playable game is:

1. Implement a complete movement/contact/combat loop through the legal-action boundary.
2. At the combat-skeleton checkpoint, select the representative Player Intent Composer decision;
   run its no-model prototype alongside first-scenario data work.
3. Add the remaining Land systems, source-verified content, and victory rules required by
   *Graziani's Offensive*.
4. Deliver deterministic Maproom, local hot-seat play, saves, replay, and the validated no-model
   intent flow.
5. Evaluate optional parsing only after the deterministic MVP, then expand into detailed Air and
   Logistics play, later scenarios, and optional intelligence.

The serial-Maneuver portion of Exercise Harness v1 now provides validated local multi-run regression
evidence without adding game rules. The implemented Operation-Stage Entry package retains its
[research](docs/research/operation-stage-entry-spike.md),
[specification](docs/specs/operation-stage-entry-v1.md), and
[technical design](docs/design/operation-stage-entry-v1.md). Reserve Designation is the latest
completed player-action vertical before Movement. Its
[research](docs/research/reserve-designation-spike.md),
[specification](docs/specs/reserve-designation-v1.md), and
[technical design](docs/design/reserve-designation-v1.md) define an incremental designation flow
that stops at Movement. Rules, state, owner projection, legal candidates, command mapping,
designation/completion events, finite checkpoint validation, replay, and checked harness evidence
are implemented. The completed engine package is the approved Movement Foundation
[research](docs/research/movement-foundation-spike.md),
[specification](docs/specs/movement-foundation-v1.md), and
[technical design](docs/design/movement-foundation-v1.md). It defines a fog-safe apparent-presence
gate followed by exact CP/Cohesion state, normalized lab terrain and stacking, repeatable
non-contact moves, and explicit completion to Breakdown Determination. The plan is owner-approved;
its source/ruling lock, exact Rules foundation, `MOV-TASK-003` Content mobility contract, and
`MOV-TASK-004` replay-complete world/representation contracts are complete. Task 004 records exact
Cohesion/expenditure and opaque internal representation
bindings in the Task 004 snapshot v8/world v3 creation history. On 2026-08-29 the owner approved sequential-d6
Breakdown coordinates, continuity-now, and the Table 21.38 Sandstorm-attributed-BP basis.
`MOV-TASK-004B` implements the exact Rules/Content/World seam and passed the repository gate plus
two fresh-context review instances. `MOV-TASK-005` implements the contract-5 owner/apparent
projection and strict canonical readback. `MOV-TASK-006` freezes dormant move/completion
candidates, exact cost semantics, deterministic IDs, pure observation-derived vectors, and strict
non-authoritative action/submission/receipt readback while preserving the existing contract
versions. `MOV-TASK-007` adds the internal move command and canonical event, authoritative
cost/provenance recalculation, engine dispatch, atomic projection, and deterministic replay.
`MOV-TASK-008` atomically publishes observation-derived move and completion membership, maps only
exact current submissions, adds canonical Movement completion through the Breakdown Determination
checkpoint, and preserves deterministic fog-equivalent actions and zero/one/many-move replay.
`MOV-TASK-009` is merged in PR #78 and adopts that supported Movement path in checked
Exercise/Maneuver evidence. `MOV-TASK-010` completed synchronization and independent review and is
merged in PR #79. Breakdown public actions and adjudication remain absent. The approved next engine
package is the ZOC/Reaction
[specification](docs/specs/zoc-reaction-v1.md) and
[technical design](docs/design/zoc-reaction-v1.md). `ZOR-TASK-002A`-`004A` implement dormant
Rules/Content/fixture, Campaign World 5/creation 9, Snapshot 10, and `ElementMoved` v2 successors,
including exact current-TOE provenance, nullable/empty Reaction-window truth, strict canonical
readback, atomic projection, checkpoint replay, and the side-safe Observation 6/policy and redacted
decision-history contracts described above. Active runtime identities remain unchanged;
`ZOR-TASK-004B` is next.
The optional paired comparison is implemented Runner instrumentation and does not block
gameplay-engine progress.
Combat research has progressed beyond the initial source inventory: `CMB-RSH-001` now retains the
first bounded rules/result-surface normalization. Combat contracts and implementation remain gated
on approved Breakdown and ZOC/Reaction boundaries.

See the [pre-alpha roadmap](docs/roadmap/pre-alpha-roadmap.md) for the capability-level plan and
completion criteria.

## Up and running

Running the repository today launches the development service scaffold and Aspire dashboard. It
does not yet launch a playable Maproom client.

### Prerequisites

- [.NET SDK 10.0.302 or a later .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Just](https://just.systems/) for the shortest command workflow (optional)
- Docker (optional; required only when future Aspire resources need containers)

`global.json` requires .NET 10.0.302 or later and rolls forward to the highest installed .NET 10
feature band. It also selects Microsoft.Testing.Platform for `dotnet test`.

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

Run the Organization-boundary Exercise and
write its ignored evidence bundle with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.v2.json \
  --artifact-root artifacts/exercises
```

The command prints the finalized bundle path. The checked-in manifest is explicitly exploratory,
so a dirty development tree is recorded honestly as nonbaseline and nonreproducible.

The corresponding Stage Entry profile runs all nine accepted actions to the Reserve boundary:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.reserve.v2.json \
  --artifact-root artifacts/exercises
```

From a clean checkout, request a fail-closed baseline bundle with the checked baseline twin:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.baseline.v2.json \
  --artifact-root artifacts/exercises
```

The Reserve profile has the corresponding clean-checkout twin
`scenarios/exercises/rules-lab.reserve.baseline.v2.json`. Run the 12-step Reserve Designation path
through first-side Movement with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.reserve-designation.v2.json \
  --artifact-root artifacts/exercises
```

Its clean-checkout twin is
`scenarios/exercises/rules-lab.reserve-designation.baseline.v2.json`.

Set the manifest's `detail` to `compact`, `forensic`, or `debug`. Forensic adds correlated audience
queries, controller selection, checks, proofs, payload sizing, and the progressively assembled
context of failed query/controller/submission decisions. Debug also retains every available
monotonic phase timing on failure and prints a structured post-readback artifact trace. These
diagnostics are trusted local instrumentation and never participate in replay equality.

Run the checked two-child serial Maneuver with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.serial.v2.json \
  --artifact-root artifacts/exercises
```

Run the two-setup Stage Entry regression Maneuver to Reserve with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.stage-entry.serial.v2.json \
  --artifact-root artifacts/exercises
```

Run the two-setup Reserve Designation Maneuver through Movement with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reserve-designation.serial.v2.json \
  --artifact-root artifacts/exercises
```

Run the six-policy Movement-entry matrix with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.controller-matrix.serial.v2.json \
  --artifact-root artifacts/exercises
```

Run the optional serial-paired Reserve-policy comparison with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reserve-policy.paired.v1.json \
  --artifact-root artifacts/exercises
```

The pair runs baseline then candidate sequentially in isolated Exercise sessions. Its report may
describe first divergence and outcome/count deltas only; it cannot support causal, statistical,
balance, recommendation, or synchronized-post-divergence conclusions.

Run the paired Movement route-cost sensitivity comparison with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json \
  --artifact-root artifacts/exercises
```

This pair keeps declared inputs and initial evidence equal while comparing the existing stable-route
controller with an additive lowest-public-cost controller. It is simulator instrumentation, not an
Umpire rule or gameplay recommendation.

The command prints each validated child bundle path in manifest order, followed by the strictly
read-back aggregate report path and deterministic report fingerprint. The report's local paths and
timings are diagnostics and do not participate in that fingerprint.

The intelligence gateway currently reports that no model provider is configured and its decision
and narrative RPCs return gRPC `Unavailable`. The Decision Worker is a service-discovery/client
scaffold; it does not yet dispatch live campaign decisions or execute fallback policy. This is
expected because model-backed commanders are outside the pre-alpha gameplay target. Future gameplay
integration must keep inference outside authoritative turns and use a deterministic scripted
decision whenever a model-backed service is unavailable.

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

The planned intelligence path sends only redacted observations and accepts only untrusted proposals.
It must never own game state, resolve rules, see hidden opposing state, or hold an authoritative
Orleans grain turn open while a model responds. The current gateway and Decision Worker are
scaffolds; when live decision dispatch is implemented, an unavailable model-backed service must
select a deterministic scripted decision rather than fail the turn.

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
| `Cna.ExerciseRunner` | Local deterministic single-Exercise, serial-unpaired, and optional serial-paired Maneuver orchestration with trusted artifacts |
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
- `Cna.Core.Observations` owns the Campaign Observation v1 allowlist, typed projection result,
  handle-based query facade, structural values, and canonical side-safe JSON writer/strict reader.
- `Cna.Core.Actions` owns typed system/side candidates, canonical action identity, observation-only
  side generation, exact-audience query/submission enforcement, and side-safe receipts.
- `Cna.Core.Exercises` owns the fresh-only opaque simulation capability, immutable trusted step
  evidence, strict canonical snapshot-to-checkpoint decoding, and reconstruction from its retained
  canonical event history.
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

- [Documentation index](docs/README.md)
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
- [Simulator controller policy matrix evidence](docs/research/simulator-controller-matrix.md)
- [Operation-Stage Entry source and contract research](docs/research/operation-stage-entry-spike.md)
- [Operation-Stage Entry v1 specification](docs/specs/operation-stage-entry-v1.md)
- [Operation-Stage Entry v1 technical design and delivery plan](docs/design/operation-stage-entry-v1.md)
- [Reserve Designation v1 source and contract research](docs/research/reserve-designation-spike.md)
- [Reserve Designation v1 specification](docs/specs/reserve-designation-v1.md)
- [Reserve Designation v1 technical design and delivery plan](docs/design/reserve-designation-v1.md)
- [Movement Foundation v1 source and contract research](docs/research/movement-foundation-spike.md)
- [Sprint 4-5 research-gate audit](docs/research/sprint-4-5-research-gates.md)
- [Breakdown continuity decision packet](docs/research/breakdown-continuity-spike.md)
- [ZOC and Reaction interruption research](docs/research/contact-reaction-zoc-spike.md)
- [CONTACT-001 accepted ZOC and Reaction rulings](docs/research/contact-reaction-zoc-source-ruling-lock.md)
- [Combat and continual-cycle source inventory](docs/research/combat-cycle-source-inventory.md)
- [Combat rules and result-surface spike](docs/research/combat-rules-result-surface-spike.md)
- [Movement Foundation v1 specification](docs/specs/movement-foundation-v1.md)
- [Movement Foundation v1 technical design and delivery plan](docs/design/movement-foundation-v1.md)
- [Microsoft Orleans documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [ASP.NET Core gRPC services](https://learn.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-10.0)
- [Aspire AppHost and ServiceDefaults](https://aspire.dev/get-started/aspire-sdk-templates/)
- [Microsoft.Testing.Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)

## License

No license has been selected yet. All rights are reserved until a license file is added.
