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
first-side Movement; open, adjudicate, close, and resume bounded ZOC Reaction interrupts; complete
Movement through Breakdown Determination to unsupported first-side Combat; resolve explicit
route stops with exact BP checks and persistent broken-vehicle lots; and replay those events to byte-identical state. Reserve authority now carries per-element status,
owner-only observation, exact acting-side candidates, closed command mapping, bounded checkpoints,
and canonical designation/completion events. The Movement foundation additionally records exact
per-Operation-Stage expenditure/Cohesion state and opaque one-to-one map representations. It now
also carries typed move/completion candidates, deterministic action identities, an exact side-safe
cost breakdown, strict non-authoritative readback, internal authoritative non-contact move
adjudication/replay, and observation-derived public action membership with exact submission
revalidation. Ruleset manifest contract 9, setup schema 6, snapshot contract 11,
Campaign World snapshot contract 6, Campaign Observation contract 7, legal-action-set contract 2
with policy v3, and Content Pack schema 6 / canonical format v5 use original synthetic
rules laboratories to develop game systems without redistributing published assets.
Campaign Observation derives deterministic side-safe public topology, audience-visible turn
revision, exact own mobility/ledger/Reserve and approved vehicle-risk facts, plus only opaque
opposing representation/location rows and the source-unmapped current-ZOC aggregate. It exposes neither complete Content identity,
real opposing bindings/force facts, nor hidden Reserve counts. Legal Actions v1 exposes those
mechanics through an opaque campaign-authority handle,
deterministic system/side action sets, exact-audience membership enforcement, and side-safe
acceptance receipts. Weather Determination v1 resolves corrected source-cited Weather through that
same boundary and records pair-keyed evidence. Operation-Stage Entry v1 then resolves only the four
explicitly admitted empty obligations through mechanic-specific actions and events. Side-safe
queries derive the Reserve audience from the recorded first/second actor order while its symbolic
sequence position keeps `ActiveSide` unset; current Movement materializes that resolved side because
successor Movement and Reaction identities bind it. Raw snapshots, commands, events, content
context, projection, and replay are not public mutation seams.

Campaign Observation 7 uses the
`sandtable.observation.breakdown-side-safe.v1` policy, one canonical source-unmapped aggregate
of apparent enemy-controlled locations, exact owner-visible Movement-ended membership, and a closed
normal/phasing/reacting/Breakdown-waiting decision-state union. Pending stops expose one
System capability and generic player waiting; own lot summaries contain cohort/location counts
without lot IDs or evidence, and reactor waiting omits owner rows that could reveal bindings. Its reacting view contains only the apparent trigger,
the observer's current state-scoped capability handles with closed current move-option/cost
capabilities, and the optional active own participant. Raw element Movement, ledger, Cohesion,
Reserve, mobility, organization, and stacking inputs remain inside Core. Reacting construction and
readback reject identity-bearing root owner-element rows, so no representation-to-element binding
is published. Admission also recomputes capability-bound opportunity handles, validates published
route/hexside cost claims against their selected edge, and binds reacting/phasing decision labels
to the observer's relationship with the active side. Both sides receive the same audience-safe
window handle, never the authoritative
window identity; phasing receives only generic waiting while retaining its ordinary owner facts. A
versioned disclosure manifest and mandatory `boundary-check` gate register
this outward surface and protect retained cross-state transcripts from copied-fingerprint joins.
A distinct strict projected-history contract retains the same redacted decision state without
authority bindings, source mappings, evidence, or internal reasons. The current action layer derives
topology-local ordinary Movement and first/later Reaction movement, participant
completion, player decline, and reason-specific System close membership with canonical identities,
strict current readback, and unpublished typed submission intents. Public Core query, submission,
checkpoint, serialization, and replay paths now use this complete successor set; bounded Exercise
Runner Reaction controllers implement `ZOR-TASK-007A`: explicit bounded policies support
participant ordering, one/two-step episodes, decline/subset close, and System fallback.
Current Movement completion preserves accepted Reaction costs through the Breakdown boundary;
explicit Breakdown completion advances to unsupported Combat without another draw.
The historical `ZOR-TASK-007B` package closed with strict evidence, matching clean-run fingerprints,
and a Ready independent review; see [historical Reaction trajectories](docs/research/simulator-reaction-trajectories.md).
Owner accepted Breakdown decisions `BRK-DEC-004`–`007`. Tasks 001–005 supplied the frozen contracts,
certified world, BP accounting and deterministic stop lifecycle. [Task 006 public activation](docs/research/breakdown-public-activation.md)
activates that complete identity set, Observation 7, projected history 2 and disclosure manifest 2.
Current creation, checkpoints and event admission reject legacy or mixed contracts; retained
Initiative, Weather and preamble evidence is recomputed, while full history is verified separately
by replay. Public queries stop at first-side Combat entry. Positive ZOC, motorized-infantry losses
and later-stage reset remain outside the certified profile. Task 007 implements fourteen checked
successor manifests and a Truck study, using certified battalion, Reaction and Truck-only inputs.
The [original fixtures](docs/specs/breakdown-fixture-migration.v1.json) remain historical with
unchanged bytes. The profile permits zero cohorts: `land.breakdown-cohorts` is required exactly
when a pack contains a cohort; all other capability, organization and stacking checks remain strict.
[Task 007 closeout](docs/research/breakdown-runner-closeout.md) records 1,655 passing tests and two matching clean runs of 47 campaigns each. Final independent review awaits human authorization after the three-review budget was exhausted; implementation does not imply reviewed closeout.

The local `Cna.ExerciseRunner` supports that synthetic rules-laboratory path as either one
bounded, deterministic **Exercise** or one serial **Maneuver**. An Exercise uses a fresh opaque Core
capability, selects only current legal actions, stops at its exact declared boundary, proves both
event-history reconstruction and fresh-session re-adjudication, and writes a manifest-last
`trusted-authority` evidence bundle. The original Organization, Reserve and Reaction checked fixtures are historical. Current regression
tests and checked `.breakdown.v1` successors use certified battalion, Truck and contact inputs. A serial-unpaired Maneuver
strictly admits one canonical ordered `serial-unpaired` manifest,
derives explicit child identities from its sole parent root seed, and runs each child in process
through the same coordinator. Each completed child bundle is read once for semantic validation and
identity-matched aggregation; snapshot facts are accepted only after the complete Core-owned
snapshot/world decoder validates their canonical structure. The resulting transactional report
separates deterministic counts, outcomes, and fingerprint material from noncanonical timing/path
diagnostics and is strictly read back before completion is claimed. Compact, forensic, and debug
Exercise detail tiers expose progressively richer evidence without changing simulation truth. The
current two-setup serial-unpaired Maneuver retains predetermined and contested initiative paths.
A checked six-child controller matrix crosses `act-first`/`act-last` with Reserve
`none`/`one`/`all`, using two non-cohort battalions per side so all three choices remain distinct.
The corresponding Movement matrix includes explicit route stops and System resolution before
Breakdown entry. The [thirteen-child Reaction successor](scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json)
retains ordering, one/two-step episodes, decline, active System closure and later-trigger recurrence
with separated battalion reactors. The two historical positive-ZOC children remain deferred from
public authority. Optional `serial-paired` Maneuvers run
isolated baseline and candidate arms
sequentially from identical declared initial conditions, initial role-specific random streams,
campaign creation inputs, build cohort, and initial snapshot. Its strictly read-back comparison is
descriptive only: trajectories and random consumption may diverge after the first differing choice,
and it makes no causal, statistical-significance, gameplay-balance, recommendation, or
synchronized-post-divergence claim. Runner model controllers and side-safe exports are not
implemented.

The checked Exercise and serial-unpaired Maneuver profiles use manifest v2, with unpaired report
scheme `sandtable.maneuver-report.v1`; the separate paired Maneuver uses
`sandtable.paired-maneuver-manifest.v1` and
`sandtable.paired-maneuver-report.v1`. Current successors use Ruleset 9, Snapshot 11, World 6, strict
`trusted-authority` evidence admission, and deterministic v2 controller configuration identity.

The first two historical simulator studies recorded repeated Movement-terminal determinism,
counterbalanced-order timing, and contested root seeds 0-31. Every sampled run passed strict
readback; the results also show that future back-testing needs explicit act-last and Reserve
none/one/all controller profiles rather than seed variation alone. See
[Baseline 1](docs/research/simulator-baseline-1.md) and
[Baseline 2](docs/research/simulator-baseline-2.md). The follow-on
[controller-policy matrix](docs/research/simulator-controller-matrix.md) closes that explicit
coverage gap with 6/6 strictly read-back trajectories and a repeatable aggregate fingerprint.
The merged [Movement trajectory study](docs/research/simulator-movement-trajectories.md) retains its
pre-Reaction 48-trajectory baseline across six controllers and four deliberate seed probes. Under
the historical Rules 8 authority, Reserve-none repeats stopped at the opened Reaction window while
the other profiles retained exact Breakdown evidence. The historical follow-on
[Movement cost-sensitivity study](docs/research/simulator-movement-cost-sensitivity.md) compared
stable-route and lowest-public-cost policies: its stable arm failed at Reaction after a cost-8 move,
while its lowest-cost arm completed a 1/2-plus-1 route. Current paired cost successors use
unladen Trucks and retain exact CP, BP and stop evidence. The added
`act-first-reserve-all-move-each-once-by-lowest-cost-then-complete` policy selects one lowest-cost
public move per eligible element. `act-first-reserve-all-repeat-highest-cost-stops-then-complete`
repeats highest-cost public moves, stops after each edge, and drains each pending System resolution.
These are bounded simulator policies; Core still determines legality and every result.

Contact, combat, published scenario content, persistence, and the Maproom
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
| Movement/contact and combat loops | Current Ruleset 9, Content/Setup/World 6, Created 10, Snapshot 11 and Observation 7 support bounded Movement, adjacency-triggered Reaction and Breakdown through first-side Combat entry; Contact and Combat adjudication remain deferred. Task 007 adds checked successors; two matching clean runs are verified; final independent review remains pending |
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
merged in PR #79. At that historical milestone, Breakdown public actions and adjudication were absent.
The subsequent ZOC/Reaction package follows the
[specification](docs/specs/zoc-reaction-v1.md) and
[technical design](docs/design/zoc-reaction-v1.md). `ZOR-TASK-002A`-`006B` implement dormant
Rules/Content/fixture, Campaign World 5/creation 9, Snapshot 10, and `ElementMoved` v2 successors,
including exact current-TOE provenance, nullable/empty Reaction-window truth, strict canonical
readback, atomic projection, checkpoint replay, and the side-safe Observation 6/policy and redacted
decision-history contracts described above. Dormant topology-local Movement/Reaction candidate,
strict current-readback, stable-identity, unpublished mapping, move-option capability,
manifest-registration, semantic-admission, and retained-transcript contracts are also complete.
The direct-only authority path reconstructs atomic move/window truth, freezes only individually
adjacent eligible reactors, applies topology-local enemy-ZOC entry/exit semantics, and closes
player-declined, unavailable, timed-out, or empty windows with exact Movement resumption and no
cost/RNG mutation. It also selects the first participant atomically with its move, keeps later
steps bound to that active participant, accumulates exact shared Movement CP/provenance, and resolves
participants without World or RNG mutation. `ZOR-TASK-006C` now activates the complete successor
identity set on public Core creation, observation, action, checkpoint, and replay paths; legacy
creation and Movement roots reject. Bounded Runner adoption in `ZOR-TASK-007A` is implemented; `007B` verification and independent review are complete.
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

The repository also includes a dependency-free project website under `site/`. From the repository
root, preview it with:

```sh
python3 -m http.server 4173
```

Then open `http://localhost:4173/site/`. The website explains the intended player loop, authority
model, current implementation frontier, and developer quick start. It is project documentation and
outreach—not the future authoritative Maproom client.

The website deploys to `https://dills122.github.io/sandtable/` through the dedicated GitHub Pages
workflow whenever website files land on `main`. The workflow can also be run manually from the
repository's Actions page. It uploads only `site/`; the simulation source and build artifacts are
not part of the published site.

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

The checked Runner commands below use certified Task 007 successors. Original Rules 8 manifests
remain frozen historical inputs and are rejected by current admission. The [closeout evidence](docs/research/breakdown-runner-closeout.md) records two clean runs of these current commands.

Run the Organization-boundary Exercise:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

The command prints the finalized bundle path. The checked-in manifest is explicitly exploratory,
so a dirty development tree is recorded honestly as nonbaseline and nonreproducible.

The corresponding Stage Entry profile runs all nine accepted actions to the Reserve boundary:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.reserve.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

From a clean checkout, request a fail-closed baseline bundle with the checked baseline twin:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.baseline.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

The Reserve profile has the corresponding clean-checkout twin
`scenarios/exercises/rules-lab.reserve.baseline.breakdown.v1.json`. Run the 12-step Reserve Designation path
through first-side Movement with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.reserve-designation.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Its clean-checkout twin is
`scenarios/exercises/rules-lab.reserve-designation.baseline.breakdown.v1.json`.

Set the manifest's `detail` to `compact`, `forensic`, or `debug`. Forensic adds correlated audience
queries, controller selection, checks, proofs, payload sizing, and the progressively assembled
context of failed query/controller/submission decisions. Debug also retains every available
monotonic phase timing on failure and prints a structured post-readback artifact trace. These
diagnostics are trusted local instrumentation and never participate in replay equality.

Run the checked two-child serial Maneuver with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the two-setup Stage Entry regression Maneuver to Reserve with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.stage-entry.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the two-setup Reserve Designation Maneuver through Movement with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reserve-designation.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the six-policy Movement-entry matrix with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.controller-matrix.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the optional serial-paired Reserve-policy comparison with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reserve-policy.paired.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

The pair runs baseline then candidate sequentially in isolated Exercise sessions. Its report may
describe first divergence and outcome/count deltas only; it cannot support causal, statistical,
balance, recommendation, or synchronized-post-divergence conclusions.

Run the paired Movement route-cost sensitivity comparison with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.movement-cost.paired.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

This unladen-Truck pair keeps declared inputs and initial evidence equal while comparing stable-route
and lowest-public-cost controllers, including each move's BP accounting and explicit stop
resolution. It is simulator instrumentation, not an Umpire rule or gameplay recommendation.

The command prints each validated child bundle path in manifest order, followed by the strictly
read-back aggregate report path and deterministic report fingerprint. The report's local paths and
timings are diagnostics and do not participate in that fingerprint.

Run the bounded Reaction successor or repeated Truck-stop study with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises

dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json \
  --artifact-root artifacts/exercises
```

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
| `site` | Static project website for developers and prospective players; non-authoritative and separate from Maproom |
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
