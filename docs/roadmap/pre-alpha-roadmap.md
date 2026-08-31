# Sandtable Pre-Alpha Roadmap

**Status:** Active

**Rules target:** `cna-1979.1`

**First playable scenario:** Land-only `Graziani's Offensive`

## Outcome

Deliver a visible, deterministic skeleton quickly, then expand it into a faithful implementation
of the shortest published scenario. Future architecture is protected through typed contracts,
versioned rule data, replay, source provenance, and explicit extension points. Unsupported rules
remain unsupported; they are never replaced with invented approximations.

Sprints below are capability increments rather than calendar promises. The first five are planned
in implementation-sized tasks. Later work remains milestone-level until the skeleton exposes the
real transcription and integration cost.

## Product boundaries

### Working pre-alpha skeleton

The pre-alpha skeleton is complete when a developer can start Sandtable, load a small original
rules-laboratory theater, play one authentic movement/contact/combat loop through the authoritative
Umpire, inspect the resulting Chronicle, and replay to an identical state.

It is explicitly not yet a playable copy of the published scenario.

### First playable MVP

The first MVP is complete when two local players can finish the six-turn Land-only
`Graziani's Offensive` scenario using every rule required by that mode, save and resume, receive
legal-action guidance without hidden-state leakage, and reproduce the campaign from its seed and
event stream.

### Non-goals before MVP

- Detailed Air Game or detailed Logistics Game.
- Model-backed commanders, generated narrative, multiplayer, matchmaking, or cloud saves.
- The 111-turn campaign or later scenario groups.
- Copied original map/counter art or rules prose.
- A reusable engine for unrelated board games.
- Silent balance fixes or speculative interpretations of ambiguous rules.

## Governing requirements

| ID | Requirement |
|----|-------------|
| FID-001 | Adjudication follows the adopted original rule, errata, or recorded ruling. |
| FID-002 | Unsupported mechanics fail explicitly and cannot produce an authoritative event. |
| SRC-001 | Each normalized rule/table/scenario datum carries a stable source reference. |
| DET-001 | A seed plus accepted command stream reproduces the same events and state. |
| DET-002 | Clock, identifiers, and randomness are injectable and deterministic in tests. |
| FOW-001 | Legal actions and observations expose only information available to the acting side. |
| EVT-001 | Authoritative changes occur only through versioned commands and emitted events. |
| REL-001 | Snapshots are replay checkpoints; Chronicle events remain the authoritative history. |
| UX-001 | Automation removes bookkeeping without removing an original decision point. |
| IPR-001 | Copyrighted scans and original artwork remain outside Git unless permission is recorded. |

## Delivery graph

```text
Source baseline and rulings
           |
           v
Replayable Umpire spine
           |
           v
Rules-laboratory content -----> Content/rights gate
           |                           |
           v                           |
World + side-safe actions               |
           |                           |
           v                           |
Mandatory turn preamble                |
           |                           v
           v                  Scenario data ingestion
Movement/Breakdown/Reaction boundary   |
           |                           |
           v                           |
Combat/Contact continual-cycle loop <--+
           |
           v
Working pre-alpha skeleton
           |\
           | +----> Select representative Command intent
           |             |
           |             v
           |       No-model intent prototype
           |             |
           v             v
Remaining Land systems + deterministic minimal Maproom
           |
           v
Six-turn playable MVP
           |
           v
Optional parser evidence gate
```

## Current delivery status

| Capability area | Status | Current boundary / next gate |
| --- | --- | --- |
| Source/ruleset provenance, synthetic content, world, authority, events, and replay foundations | Implemented | Preserve exact identities and deterministic history while mechanics expand |
| Side-safe observations and legal-action enforcement | Implemented for the current synthetic path | Extend only with each new mechanic and its disclosure tests |
| Mandatory turn preamble | Implemented through Reserve Designation completion | Preserve the exact Movement terminal while later mechanics expand |
| Movement and contact | Movement Foundation is complete through merged `MOV-TASK-010` / PR #79; dormant ZOC/Reaction implementation is complete through `ZOR-TASK-003A` | Add dormant Snapshot/event serialization and replay successors in `003B`; Breakdown adjudication remains a separate gate |
| Combat | Research active; source inventory and `CMB-RSH-001` result-surface spike complete; implementation not started | Contract freeze depends on approved Breakdown and ZOC/Reaction boundaries |
| Working pre-alpha skeleton | Not reached | Requires one authentic movement/contact/combat loop with replay |
| First-scenario content and remaining Land systems | Milestone-level; not started | Re-estimate after the skeleton exposes exercised-rule and transcription scope |
| Campaign lifecycle and Maproom | Milestone-level; not started | Requires stable playable authority, Chronicle persistence, and save/resume contracts |
| Player Intent Composer | Reviewed direction; not started | After the Sprint 5 skeleton, select one representative multi-field decision; prototype without a model before Sprint 8; evaluate Needle only after the deterministic MVP path works |
| Exercise Harness | Single-Exercise, serial-unpaired Maneuver, controller/Movement matrices, and optional paired Reserve-policy and Movement-cost comparisons implemented | Preserve strict trusted evidence; side-safe export, model controllers, and parallel/distributed execution remain deferred |

The roadmap deliberately distinguishes foundation maturity from playable breadth. The architecture
is substantially established, but the campaign is not playable while authority stops at the
Breakdown Determination checkpoint and Breakdown adjudication, contact, combat, victory,
persistence, and Maproom remain absent.

### Sprint status summary

| Sprint | Delivery status | Exit boundary / next gate |
| --- | --- | --- |
| 0 — Source and fidelity baseline | Complete | Preserve the approved source hierarchy, rights posture, and recorded rulings |
| 1 — Replayable Umpire spine | Complete for the admitted synthetic path | Extend versioned authority, event, replay, and deterministic-random contracts with each mechanic |
| 2 — Rules laboratory and legal-action boundary | Complete for the admitted synthetic path | Keep outward actions observation-derived and revalidate exact membership at submission |
| 3 — Mandatory turn preamble | Complete for the admitted synthetic no-obligation path | Positive Organization/convoy/fleet cases remain explicit later-scenario work; authority now reaches the Breakdown Determination checkpoint |
| 4 — Movement, Breakdown, and Reaction boundary | Active | Movement Tasks 001-010 are complete and merged; approved dormant ZOC/Reaction checkpoints are implemented through `ZOR-TASK-003A` and `003B` is next, while Breakdown adjudication remains separate |
| 5 — Combat and continual-cycle loop | Source inventory and `CMB-RSH-001` complete; not implementation-ready | Freeze contracts only after approved Breakdown and ZOC/Reaction boundaries define Contact/Engaged identity |
| 6 — Scenario Group One content | Milestone-level; not started | Begin after the working pre-alpha skeleton measures the exact exercised-rule/data surface |
| 7 — Remaining required Land systems | Milestone-level; not started | Split from the measured first-scenario rule inventory rather than treating it as one task |
| 8 — Minimal Maproom and campaign lifecycle | Milestone-level; not started | Requires stable playable authority, Chronicle persistence/save-resume, and the no-model intent prototype |

Sprint numbers describe dependency order, not equal effort or a project-completion percentage.

## Sprint 0: Source and fidelity baseline

**Goal:** Decide what Sandtable is implementing and how conflicting sources are resolved.

**Deliverables:**

- Source-material spike and classified source index.
- Proposed authority order and `cna-1979.1` identifier.
- Pre-alpha/MVP boundary and sprint roadmap.
- Rights and missing-source gates.

**Acceptance criteria:**

- [x] The first scenario target is supported by primary source evidence.
- [x] Rules, errata, community aids, and the current redesign are not conflated.
- [x] No copyrighted source asset is committed.
- [x] Project owner approves the source baseline, first scenario, and asset posture.

**Verification:** Documentation review and link check.

## Sprint 1: Replayable Umpire spine

**Goal:** Establish an authentic phase hierarchy and an authoritative campaign history that can be
reconstructed exactly without skipping unimplemented mechanics.

**Status:** Replayable foundation and the admitted mandatory preamble through Reserve Designation
are delivered; Movement rules/content/world prerequisites are also merged.
Campaign creation, Initiative, the admitted empty convoy checkpoints, Operation Stage 1 Initiative
Declaration, Weather Determination, the four admitted empty stage-entry obligations, canonical event
serialization, replay, Reserve Designation completion, and internal non-contact Movement
adjudication are authoritative. Public Movement membership and completion remain the next mandatory
unimplemented mechanic.

### Task 1.1 - Ruleset provenance contracts

Define `RulesetManifest`, `RuleReference`, `Ruling`, and stable Land phase/segment identifiers.

**Acceptance criteria:**

- [x] A ruleset hash changes when authoritative normalized data or adopted rulings change.
- [x] Every sequence position is serializable, versioned, and source-cited.
- [x] No contract contains copied rules prose.

**Likely scope:** `src/Cna.Core/Rules`, `tests/Cna.Core.Tests/Rules` (medium).

### Task 1.2 - Campaign commands, events, and snapshots

Define the smallest campaign state with game turn, operation stage, active side, phase, state
version, seed, and ruleset hash. Implement campaign creation and typed advancement rejection
through the first unsupported mandatory mechanic.

**Acceptance criteria:**

- [x] State cannot mutate without an accepted command and event.
- [x] Illegal phase transitions return a typed rejection without changing state.
- [x] Snapshot serialization preserves every authoritative field.

**Likely scope:** `src/Cna.Core/Campaigns`, `tests/Cna.Core.Tests/Campaigns` (medium).

### Task 1.3 - Deterministic replay harness

Rebuild campaign state from events and make every ambient dependency explicit when a rule first
requires it.

**Acceptance criteria:**

- [x] Replaying an event stream produces byte-equivalent canonical state.
- [x] The same seed and commands produce the same event sequence.
- [x] Campaign creation records the declared seed deterministically; no clock or generated
  identifier exists in accepted history.
- [x] Initiative Determination consumes the first versioned random stream and proves different
  seeds affect only its declared random outcomes.

**Verification:** Focused Core tests, full solution build, and full test suite.

### Sprint 1 demonstration

Create a campaign from either recognized synthetic setup, display canonical creation and
Initiative events as Chronicle input, replay them to identical canonical state, and prove the
original Initiative slice stops at Naval Convoy. The contested fixture derives published Initiative Ratings, rolls
Axis then Commonwealth through the versioned SHA-256 counter stream, records complete tie rerolls,
and retains exact source provenance. The predetermined fixture consumes no draw.

The executable Core tests distinguish authoritative adjudication from catalog inspection. Generic
sequence advancement and legacy contract-v1 history cannot cross Initiative Determination;
`ResolveInitiative` is the sole accepted command at that checkpoint and its event is recomputed
during projection. Legal Actions v1 adds only mechanic-specific successors; a human-facing
Chronicle display belongs to the later Maproom slice.

Implementation and acceptance evidence are tracked in the
[Initiative Determination specification](../specs/initiative-determination.md) and
[technical design](../design/initiative-determination.md).

## Cross-cutting developer instrumentation: Exercise Harness v1

**Capability:** `EXERCISE-001`

**Status:** Task 016 implementation and repository gates are complete, including the
single-Exercise, serial-unpaired Task 014, controller-matrix Task 014J, and optional serial-paired
Task 015 paths to first-side Movement. Final delivery status is established by the required
fresh-context review recorded in PR evidence

**Goal:** Run a freshly created bounded rules-laboratory campaign through the authoritative legal
action path, retain deterministic trusted evidence, and prove both event-history reconstruction and
fresh-session re-adjudication before using multi-run evidence for regression or controller
comparison.

**Acceptance boundary:** The first Exercise still stops successfully at the Operation Stage 1
Organization checkpoint. Separate checked Exercises preserve the Reserve boundary and traverse
Reserve Designation to Movement; a two-setup serial Maneuver proves the 12-step Movement-terminal
path for both admitted setups, and a six-child controller matrix proves `act-first`/`act-last`
crossed with Reserve `none`/`one`/`all`. The runnable single-Exercise and serial-Maneuver
CLIs include
fail-closed transactional artifacts, explicit build/run identity, stable failure categories, and
validated aggregate reports. The optional paired fixture runs isolated baseline and candidate arms
sequentially from equal declared inputs and initial random streams. Its report is descriptive and
makes no causal, statistical-significance, gameplay-balance, recommendation, or
synchronized-post-divergence claim. Task 016 re-runs every checked CLI example, strict readback,
the full repository gate, and an independent review. Trusted bundles are not side-safe output, and
this capability does not imply full victory play, model controllers, parallel/distributed
execution, balance conclusions, or production campaign activation.

The contract, research decisions, implementation tasks, and acceptance evidence are in
the [Exercise Harness v1 specification](../specs/exercise-harness-v1.md) and
[technical design](../design/exercise-harness-v1.md).

Tasks 014, 014J, 015, and the Task 016 repository-close implementation are instrumentation work, not
additional gameplay. The Harness v1 implementation boundary is complete; final delivery requires
the independent-review gate recorded in PR evidence. Any side-safe export, model controller,
persistence index, or parallel/distributed scheduler requires separate authorization and does not
block the authoritative gameplay dependency graph. The implemented engine work retains the
[Operation-Stage Entry research](../research/operation-stage-entry-spike.md),
[specification](../specs/operation-stage-entry-v1.md), and
[technical design](../design/operation-stage-entry-v1.md), followed by the implemented
[Reserve Designation package](../design/reserve-designation-v1.md). Public observation-derived
non-contact Movement and completion to Breakdown Determination are implemented in `MOV-TASK-008`;
checked Harness adoption is merged in `MOV-TASK-009`, and `MOV-TASK-010` closeout is merged in PR
#79.

## Cross-cutting future player interaction: Player Intent Composer v1

**Capability:** `INTENT-001`

**Status:** Product direction and delivery plan independently reviewed; implementation not
authorized and not part of the current sprint

**Goal:** Let a player express bounded, combinatorial Command intent through contextual suggested
approaches, short language, map/list interaction, or structured fields that all edit one visible
private typed draft. Deterministic validation may surface at most two automatic clarification
questions. The player confirms intent before deterministic Staff planning and separately confirms a
current legal order before Umpire adjudication.

This track attaches to demonstrated gameplay decisions rather than preceding them:

1. **After the Sprint 5 skeleton:** select one representative multi-field decision and classify each
   field as Command intent, Staff planning choice, or Umpire rule. Resolve starter source/precedence
   and the future plan-to-Legal-Action binding before production contracts.
2. **Between Sprint 5 and Sprint 8:** run an isolated no-model rules-lab prototype with synthetic
   Staff-plan fixtures. Prove starters are understood as non-exhaustive, the typed interpretation is
   correctable, every task works through deterministic controls, and local hot-seat transitions
   isolate outgoing strategic data.
3. **During Sprint 8:** integrate one deterministic vertical slice through authorized observation,
   intent opportunity, private draft, validation, Staff plan, final review, and current legal-action
   revalidation. The first playable MVP cannot depend on a model.
4. **After the deterministic MVP path works:** build the retained corpus and deterministic baseline,
   then evaluate Needle behind the replaceable parser adapter. Parser adoption or production rollout
   requires the separate evidence gate below and must not block rules, campaign, or Maproom delivery.

The reviewed [research](../research/player-intent-input-and-needle-feasibility.md),
[specification](../specs/player-intent-composer-v1.md), and
[technical delivery plan](../design/player-intent-composer-v1.md) govern this future track. Its
private drafts and parser are not Core authority, autonomous commander `DecisionProposal` values, or
an alternate Intelligence submission path.

## Sprint 2: Rules laboratory and legal-action boundary

**Goal:** Load an original, license-safe test theater and expose only legal, side-safe actions.

Content Pack v1 is governed by the
[research packet](../research/content-pack-v1-spike.md),
[specification](../specs/content-pack-v1.md), and
[technical design](../design/content-pack-v1.md). The owner-approved Content Pack v1, original rules
laboratory, campaign admission, side-safe observation boundary, and Legal Actions v1 are delivered.
Sprint 2 is complete for the current synthetic path; later mechanics must extend the same boundaries
rather than create alternate player or Intelligence mutation seams.

### Task 2.1 - Versioned content schemas

Define schemas for hexes/hexsides, terrain, sides, formations, combat elements, placements, and
scenario metadata. Each record carries provenance or is marked as a synthetic test fixture.

**Acceptance criteria:**

- [x] Invalid coordinates, references, duplicate IDs, and impossible placements fail at load time.
- [x] Canonical serialization produces a stable content hash.
- [x] Schema evolution is versioned independently from scenario content.

### Task 2.2 - Rules-laboratory fixture

Create a small original hex theater containing only the terrain and units needed to exercise the
first vertical slice. It must be visibly labeled nonhistorical and not presented as a CNA scenario.

**Acceptance criteria:**

- [x] The fixture uses original placeholder names and visuals.
- [x] It loads through the same path intended for published-scenario data.
- [x] No test depends on original map or counter artwork.

### Task 2.3 - Content admission and initial campaign world

Bind the exact content ID/hash and selected scenario to a versioned campaign world and project its
initial entity positions through authoritative creation history.

**Status:** Implemented; independent implementation review passed.

**Acceptance criteria:**

- Missing or mismatched historical content rejects before campaign creation and replay never
  resolves a mutable “latest” catalog entry.
- Creation records exact ruleset/content identities and replay recreates byte-identical initial
  world state using the matching historical executable and content bytes.

Requirements, contract migration, implementation checkpoints, and acceptance evidence are defined
in the [Campaign World v1 specification](../specs/campaign-world-v1.md) and
[technical design](../design/campaign-world-v1.md).

### Task 2.4 - Side-safe observation projection

Project the authoritative content/world for one acting side without mutating state.

**Status:** Implemented; independent implementation review passed.

**Acceptance criteria:**

- Hidden opposing details never appear in an observation.
- Full Content Pack/world values never appear in a player or Intelligence DTO.
- Equal state and side produce byte-equivalent observations.

Requirements, privacy decisions, executable checkpoints, and deferrals are defined in the
[Campaign Observation v1 specification](../specs/campaign-observation-v1.md) and
[technical design](../design/campaign-observation-v1.md). Future opposing-contact work is separated
in the [reconnaissance/contact research](../research/recon-contact-knowledge-spike.md).

### Task 2.5 - Legal-action generation and enforcement

Generate typed current system and side actions, derive side candidates from the side-safe
observation boundary, and enforce optimistic concurrency and exact-audience membership. The first
non-empty side set reaches Operation Stage 1 Initiative Declaration through explicit
no-obligation resolutions for both mandatory Naval Convoy checkpoints and stops at Weather.

**Status:** Implemented; verification and independent implementation review passed.

**Acceptance criteria:**

- Hidden opposing details never appear in a candidate action.
- Every accepted outward player or Intelligence action submission was present in that exact
  audience's current legal-action set; raw player-command decision is not a public adapter seam.
- The initiative holder receives a non-empty first-or-last choice whose complete set is unchanged
  by valid opponent-only authority mutations.
- Stale actions are rejected by state version.
- Querying legal actions does not mutate state or consume randomness.

**Verification:** Schema validation tests, fog-of-war negative tests, full repository gate.

The source boundary, synthetic no-obligation ruling, and explicit deferrals are defined in the
[turn-preamble action research](../research/turn-preamble-action-boundary-spike.md). The corrected
behavioral contract is in the [Legal Actions v1 specification](../specs/legal-actions-v1.md).

### Sprint 2 demonstration

Load the rules laboratory, inspect one side's redacted state, and select an action from the legal
set while the opposing hidden state remains absent from logs and responses.

The current checkpoint loads, validates, hashes, resolves, and round-trips the complete synthetic
rules laboratory, projects the implemented side-safe observation, exposes the current exact-audience
action set, and accepts the holder's first-or-last choice without exposing opposing authority. The
Sprint 2 demonstration is executable in Core tests; a human-facing Maproom remains future work.

## Sprint 3: Mandatory turn preamble

**Goal:** Advance from Naval Convoy to the first player-execution phase without skipping a
mandatory rule or deleting the initiative holder's choices.

**Status:** Complete for the admitted synthetic Operation Stage 1 no-obligation path through
Reserve Designation. Positive convoy/Organization/fleet cases and later-stage declarations remain
explicit Sprint 7/scenario-driven work before any content that exercises them is admitted.

### Task 3.1 - Turn-preamble source and contract spike

Determine the smallest source-faithful Naval Convoy, Initiative Declaration, and Weather
Determination capability exercised by the rules laboratory. Define commands, events, normalized
tables, required content/world facts, and explicit unsupported cases before implementation.

The `ACTION-001` research resolved the opening sequence and first side-choice boundary. Weather's
table/random contract is now implemented. General convoy obligations and every-stage Initiative
Declaration coverage remain deferred requirements before a future checkpoint or scenario may
depend on them; they do not reopen the completed admitted synthetic path.

**Status:** Decision package approved 2026-08-19; Weather implementation complete.

The [Operation-Stage Preamble spike](../research/operation-stage-preamble-spike.md) found that the
earlier stage-entry wording compressed mandatory source boundaries. Weather has immediate
fuel/water, well, and grounded-aircraft consequences; Organization permits player-selected segment
order; Fleet Assignment and Reserve Designation are side decisions. The implemented
[Weather Determination v1 specification](../specs/weather-determination-v1.md) and
[technical design](../design/weather-determination-v1.md) established the Task 3.3-3.5 split
below. The project owner approved `TURN-DEC-001` through `TURN-DEC-006` on 2026-08-19, activating
the replacement tasks. The decision also covers
pair-keyed `(GameTurn, OperationStage)` history and explicit rejection of the source chart's omitted
Game Turn 111 rather than silent extrapolation.

### Task 3.2 - Naval Convoy and Initiative Declaration

Complete the required Naval Convoy decision/resolution beyond `ACTION-001`'s admitted synthetic
no-obligation case and support the initiative holder's separate first-or-last declaration for every
reachable Operation Stage. No generic sequence command may bypass either mechanic.

**Dependency status:** Deferred and non-blocking for Task 3.3. `ACTION-001` already supplies the two
exact admitted synthetic Weather checkpoints, including the resolved opening convoy cases and
retained Operation Stage 1 initiative order required by `WEATHER-001`. General convoy obligations
and every-stage declaration coverage resume before any later-stage checkpoint depends on them;
they were not prerequisites to Weather v1 or the exact admitted empty Operation Stage 1 path.

### Task 3.3 - Weather Determination v1 (implemented)

Implement `WEATHER-001`: resolve the source-cited Weather procedure through the versioned random
stream, publish the exact public Weather value, and stop at the same Operation Stage's Organization
barrier. Do not complete Organization, Fleet, Reserve, or Movement.

**Status:** Implemented and verified 2026-08-19. Exact repository-gate evidence is recorded in the
Weather specification.

### Task 3.4 - Organization and stage entry (implemented explicit-empty slice)

`STAGE-ENTRY-001` resolves the exactly admitted empty Organization, Naval Convoy Arrival, Fleet
Assignment, and Fleet Repair obligations through four mechanic-specific legal actions and four
distinct authoritative event types. It stops at the first-acting side's Reserve Designation
decision; the authoritative `ActiveSide` remains unset and the side audience is derived from the
recorded Operation Stage order. Positive obligations remain unsupported and cannot use this path.

**Status:** Implemented and verified 2026-08-24. The retained source/ruling decisions, governing
specification, technical design, task ledger, review reconciliation, and acceptance evidence define
the exact delivered boundary.

### Task 3.5 - Reserve Designation (implemented)

Implement `RESERVE-001`: let the first-acting side designate and complete its reserves through the
legal-action boundary. Stop at Movement; do not begin Movement as part of Reserve completion.

The implemented interaction generates one current legal candidate per eligible own element, accepts
zero or more incremental Reserve I designations, and requires an explicit completion action. Each
designation remains at the Reserve position; completion advances once to Movement. Reserve Release,
Reserve II production, movement restrictions, and Movement behavior remain separate capabilities.

**Delivery status:** Complete. Ruleset/world/snapshot/observation/action contracts, finite Reserve
and Movement validation, designation/completion events, replay, v2 Exercise/Maneuver identities,
checked standalone/two-setup 12-step evidence, the final repository gate, and independent
implementation review are implemented through `RES-TASK-016`.

**Acceptance criteria:**

- Every accepted transition is produced by a mechanic-specific command/event and replays exactly.
- Unsupported Naval Convoy, Weather, Organization, Fleet, or Reserve cases stop with typed
  rejection and zero events.
- Initiative holder, first-acting side, and second-acting side remain separate authoritative facts.
- A side-safe legal-action query exposes only the decisions available at the current preamble step.
- The campaign reaches a movement-capable player phase only after all required preamble mechanics
  are complete.

**Verification:** Source/table boundary tests, forged-history negatives, deterministic replay, and
checked Exercise/Maneuver runs through the ordinary legal-action path, and the full repository gate.

The implemented boundary is documented in the
[research packet](../research/reserve-designation-spike.md),
[specification](../specs/reserve-designation-v1.md), and
[technical design](../design/reserve-designation-v1.md).

### Sprint 3 demonstration

Continue the synthetic campaign from Naval Convoy, make the available preamble decisions, resolve
Weather, Organization/stage-entry obligations, and Reserve in their separately gated capabilities,
then stop at Movement with a replay-identical Chronicle. Task 3.3 independently stops at
Organization; the implemented Task 3.4 explicit-empty checkpoint independently stops at Reserve;
Task 3.5 reaches Movement. Sprint 4 is active: Movement Foundation is complete through merged
`MOV-TASK-010` / PR #79, and approved dormant ZOC/Reaction implementation is complete through
`ZOR-TASK-003A`.

## Sprint 4: Movement, Breakdown, and Reaction boundary

**Goal:** Complete replayable Movement plus its immediate Breakdown and enemy-ZOC/Reaction
boundaries without inventing the later Movement-Segment-start Contact or Close-Assault-result
Engaged state.

Sprint 4 is the active delivery sprint. Its first bounded package has an approved source inventory,
exact command/event/state contracts, and task-sized implementation plan in the Movement Foundation
[research](../research/movement-foundation-spike.md),
[specification](../specs/movement-foundation-v1.md), and
[technical design](../design/movement-foundation-v1.md). The project owner approved the
apparent-presence visibility ruling, exact CP representation, non-contact boundary, Breakdown
terminal, and task graph on 2026-08-25. The source/ruling lock, exact amount/table foundation, and
Content Pack mobility and replay-complete internal world/representation work in `MOV-TASK-001`
through `MOV-TASK-004` are complete. The owner approved sequential-d6 coordinates,
continuity-now, and the Table 21.38 Sandstorm BP basis on 2026-08-29. `MOV-TASK-004B` implements
the exact BP/cohort/world seam and has passed its repository and independent-review gates.
`MOV-TASK-005` implements side-safe own operational state, apparent opposing presence, and strict
canonical readback. `MOV-TASK-006` freezes the dormant move/completion, action-ID, cost, derivation,
and strict action/submission/receipt readback contracts without making Movement public or
executable. `MOV-TASK-007` implements internal adjudication, canonical events, atomic projection,
and deterministic replay. `MOV-TASK-008` atomically publishes observation-derived move and
completion membership, exact submission mapping, canonical completion to Breakdown Determination,
fog equivalence, and zero/one/many-move replay; `MOV-TASK-009` checked evidence is merged in PR #78,
and `MOV-TASK-010` synchronization/review is merged in PR #79. The next authority package is the
approved ZOC/Reaction [specification](../specs/zoc-reaction-v1.md) and
[technical design](../design/zoc-reaction-v1.md); dormant checkpoints `ZOR-TASK-002A`-`003A` are
implemented and `003B` is next.

The approved delivery sequence adds a necessary fog-safety gate before the original task list:

1. lock the digital apparent-presence/ZOC visibility ruling and initial map representation
   (**complete**);
2. normalize exact CP, mobility, terrain, edge, and stacking contracts (**complete**);
3. implement approved minimum Breakdown continuity and replay-complete observation contracts
   (**Tasks 004B and 005 complete**);
4. freeze dormant typed Movement candidates, exact costs, identity, pure derivation, and strict
   action/submission/receipt readback (**Task 006 complete; public membership remains empty**);
5. implement and atomically publish repeatable non-contact Movement plus explicit completion to
   Breakdown Determination (**Tasks 007-008**); and
6. use the existing Reserve `none`/`one`/`all` matrix for checked Movement evidence.

Enemy-ZOC entry, Reaction, Contact, Engaged, break-off, Breakdown adjudication, and Combat remain
later Sprint 4/5 slices rather than hidden additions to the first Movement resolver.

### Sprint 4 delivery checkpoints

| Checkpoint | Tasks | Repository boundary | Review/merge gate |
| --- | --- | --- | --- |
| 4A — Movement authority foundation | `MOV-TASK-001`-`004` | Exact rules/content/world contracts exist; no public Movement action | Complete and merged in PR #29 |
| 4A.5 — Breakdown continuity | `BREAKDOWN-001`, `MOV-TASK-004B` | Approved sequential d6, continuity-now, and Table 21.38 Sandstorm BP basis; Rules/Content/World migration implemented | Complete; repository gate and two fresh-context review instances passed |
| 4B — Side-safe outward contract | `MOV-TASK-005` | Own mobility/ledger and apparent opposing presence plus own BP/cohort risk project without hidden leakage | Complete; repository and independent-review gates passed |
| 4C — Atomic non-contact Movement vertical | `MOV-TASK-006`-`008` | Dormant contracts, internal adjudication, and atomic public move/completion publication through Breakdown Determination are complete | Complete; focused/full gates and independent implementation review passed |
| 4D — Checked evidence and closeout | `MOV-TASK-009`-`010` | Reserve policy matrix executes supported Movement to Breakdown with strict readback and synchronized evidence | Complete; Task 009 merged in PR #78 and Task 010 in PR #79 |
| 4E — ZOC/Reaction interruption | accepted `CONTACT-001` and approved `ZOR-TASK-002A`-`007B` plan | Adjacency-triggered phasing Movement opens an opponent decision, exact Reaction/decline resolves, and Movement resumes deterministically | `ZOR-TASK-002A`-`003A` implement dormant Rules/Content/fixture and Campaign World/creation seams; `003B` is next; Contact/Engaged remain Sprint 5 |

### Current parallel execution window

Movement closeout is complete through merged `MOV-TASK-010` / PR #79. The serial authority lane is
the approved ZOC/Reaction package, implemented through `ZOR-TASK-003A`. Its Rules, Content, Campaign,
observation, action, and execution contracts remain serial through `ZOR-TASK-006C`; bounded Runner
adoption follows only after the public authority vertical exists.

The following bounded lanes can proceed without colliding with that serial authority path:

| Lane | Scope now | Collision / merge gate |
| --- | --- | --- |
| ZOC/Reaction authority lane | Stable approved requirements, technical design, task slices, and traceability from accepted `CONTACT-001` rulings | Dormant checkpoints `ZOR-TASK-002A`-`003A` are implemented; `003B` is next; approval did not collapse the dependency graph |
| Breakdown adjudication research/design | Normalize the percentage outcome table, action/result/loss vocabulary, RNG evidence, and decision packet against implemented BP state | Research/design only; do not edit shared Core contracts concurrently, and reconcile after `ZOR-TASK-003B` freezes the Campaign seam |
| `CMB-RSH-003`-`004`, `CYCLE-RSH-001`, `RESREL-RSH-001` | Continue bounded mutable-state, RNG, cycle-identity, and Reserve Release research | Research only; the ZOC package adopts only the minimum approved static component/current-TOE foundation and no Combat resolution |
| `CIH-IMP-004` offline Markdown links | Add a repository-local offline link gate with reviewed exclusions and a broken-link negative | Begin after the central documentation sync; keep shared architecture/status docs under one owner |

Maproom, campaign hosting/dispatch, provider-backed Intelligence, and their behavior-level
observability remain held until their roadmap triggers exist. Current service projects are
scaffolds, not unfinished work that should compete with the authoritative Movement path.

Movement Tasks 005-010 are complete. The owner approved the bounded Rules/Content/World BP
continuity lane as Task 004B before Task 005 and subsequently approved the ZOC/Reaction governing
package. Its dependency-ordered dormant implementation is complete through `ZOR-TASK-003A`.

The [Sprint 4-5 research-gate audit](../research/sprint-4-5-research-gates.md) opens three bounded
packets without authorizing their implementations:

| Research gate | Status | Decision/implementation dependency |
| --- | --- | --- |
| `BREAKDOWN-001` | Approved and completed in `MOV-TASK-004B` | Exact BP, synthetic Truck cohort, Sandstorm-attributed world continuity, identity migration, and review are closed; Task 005 projects the approved owner subset |
| `CONTACT-001` | Decision-complete; all five rulings accepted in PR #71; ZOC/Reaction spec/design approved after PR #80 merged | Dormant implementation is complete through `ZOR-TASK-003A`; `003B` is next; Contact/Engaged remain Sprint 5 |
| `COMBAT-CYCLE-001` | Source inventory complete; contract freeze waits for Breakdown and ZOC/Reaction | Replace provisional Sprint 5 headings with reviewed private-choice, simultaneous-resolution, Reserve Release, and repeat-cycle contracts |

### Task 4.1 - Capability Points and the initial cohesion ledger

**Status:** Foundation partially implemented. Exact amounts/tables and initial replayable
expenditure/Cohesion state are complete in `MOV-TASK-002` and `MOV-TASK-004`; accepted Movement
ledger mutation and authority-side over-CPA rejection are complete in `MOV-TASK-007`; public
Movement membership and completion to Breakdown Determination are complete in `MOV-TASK-008`.
Approved BP
continuity is implemented and independently reviewed in `MOV-TASK-004B`; Task 005 freezes outward
observation and Task 006 freezes dormant action contracts.

Implement source-cited exact Capability Point expenditure and the replay-complete Operation-Stage
cohesion ledger for the approved at-or-below-CPA Movement Foundation boundary. Every move whose
resulting cumulative expenditure would exceed base CPA remains unavailable and rejects with zero
events; over-CPA Disorganization conversion and end-stage reorganization remain later Sprint 4
capabilities after their source contract and Breakdown adjudication are designed.

**Acceptance criteria:**

- CP cost and the unchanged initial Cohesion value can be explained from emitted ledger entries.
- Rejected expenditure emits no authoritative event.
- Replaying the ledger recreates the same remaining capability and cohesion.
- Integral or fractional over-CPA expenditure, Disorganization conversion, and reorganization
  reject or remain unavailable rather than entering the first Movement resolver implicitly.

### Task 4.2 - Terrain, stacking, and zones of control

**Status:** Normalized supported terrain/edge/stacking tables are implemented in `MOV-TASK-002`.
Dormant candidate contracts (`MOV-TASK-006`), internal command/event/adjudication/replay
(`MOV-TASK-007`), public legal-action membership/completion (`MOV-TASK-008`), and checked Harness
adoption (`MOV-TASK-009`) are complete, and Task 010 closeout is merged. Enemy-ZOC behavior is
outside Movement Foundation v1 and proceeds only through the approved ZOC/Reaction task sequence.

Implement only the terrain and unit categories present in the rules laboratory, using normalized
tables rather than resolver constants.

**Acceptance criteria:**

- Terrain and hexside costs are table-driven and source-cited.
- Stacking and ZOC violations are rejected before movement.
- Adding an unsupported terrain/unit category fails explicitly.

### Task 4.3 - ZOC and Reaction interruption

**Status:** Decision-complete research exists and all five owner rulings are accepted in PR #71.
The [ZOC/Reaction specification](../specs/zoc-reaction-v1.md) and
[technical design](../design/zoc-reaction-v1.md) are approved; `ZOR-TASK-002A`-`003A` are implemented
as dormant Rules/Content/fixture and Campaign World/creation seams and leave ruleset 7 / Content
schema 4 / World 4 / CampaignCreated 8 active. `003B` is next.

Allow enemy-ZOC entry to end the mover, open a persisted non-phasing Reaction window, accept
side-safe Reaction or deterministic decline, and resume the phasing Movement position. Use the
accepted research task graph in the
[ZOC/Reaction ruling lock](../research/contact-reaction-zoc-source-ruling-lock.md) as its source
input. The approved dependency-ordered `ZOR-TASK-002A`-`007B` plan does not authorize collapsing
those slices or bypassing their individual proof gates.

**Acceptance criteria:**

- A triggering move commits before any opponent choice; no authoritative turn waits on external I/O.
- Every Reaction/decline increments state and regenerates exact-side candidates without leaking
  hidden eligibility.
- Reaction reuses approved CP/BP accounting and resumes phasing Movement deterministically.

V1 owns the deterministic Core system-close action and its exact unavailable/timeout reason, but no
clock or scheduler. OrleansHost/DecisionWorker pending-decision activation, deadlines, and automatic
submission remain a separately approved hosting/dispatch task; callers may already submit the
current system action without holding an Umpire transition open.

**Verification:** Table-driven deterministic tests for legal, illegal, boundary, and replay cases.

### Task 4.4 - Contact/Engaged and continual-cycle handoff

**Status:** Moved to the Sprint 5 Combat-cycle package. Immediate enemy-ZOC entry creates neither
relationship: Contact is derived from enemy-ZOC presence at the beginning of a Movement Segment,
while Engaged is a Close Assault result. Neither is a field in the current non-contact Movement
resolver.

### Sprint 4 demonstration

The Movement Foundation demonstration moves supported non-contact elements, spends exact
capability, replays the resulting world/ledger, and stops honestly at unsupported Breakdown. The
complete Sprint 4 demonstration additionally enters enemy ZOC, resolves the resulting Reaction
window and Breakdown through approved packages, resumes the phasing side, and stops before the
later Movement-Segment-start Contact and Close-Assault-result Engaged handling while showing the
resulting state and Chronicle explanation.

## Sprint 5: Combat and continual-cycle loop

**Goal:** Resolve a representative combat through Reserve Release, then explicitly repeat or finish
the Movement/Breakdown/Combat/Reserve Release cycle.

Sprint 5 is currently a capability plan. Its source/table inventory, hidden-choice contract, exact
segment state, and task-sized implementation plan must be approved after the movement/contact state
model is proven.

**Status:** Source inventory complete; contract/task freeze is blocked by approved
`BREAKDOWN-001` and ZOC/Reaction boundaries. The
[Combat-cycle inventory](../research/combat-cycle-source-inventory.md) replaces the former three
oversized executable-looking tasks with explicit research and design gates.

`CMB-RSH-001` is complete in the
[Combat rules and result surface spike](../research/combat-rules-result-surface-spike.md). Research
that may proceed now: `CMB-RSH-002` through `004`, `CYCLE-RSH-001`, and `RESREL-RSH-001` normalize
the remaining content and mutable-state choices, RNG evidence, cycle identity, and Reserve Release
rules.

After Breakdown and ZOC/Reaction boundaries are implemented, `CMB-DES-001` through `005` and `CYCLE-DES-001` freeze
Contact-derived opportunity identity, trusted-Umpire sealed choices, simultaneous/sequential
resolution boundaries, losses/retreat/disclosure, and Reserve Release repeat/finish authority.
Only then may a reviewed specification/design cut implementation-sized Sprint 5 tasks.

**Planning acceptance gate:** every reachable random result for an admitted table coordinate is
implemented or the campaign rejects before mutation under an approved bounded rule; legality never
depends on a favorable future seed.

### Sprint 5 demonstration and pre-alpha checkpoint

Complete movement, contact, combat, loss application, and reserve release in the rules laboratory;
restart from the seed/event log and obtain the same result. This is the working pre-alpha skeleton.

At this checkpoint, execute Player Intent Composer Phase 0: select the representative multi-field
decision, classify its Command/Staff/Umpire ownership, and revalidate the intent plan against the
proven action vocabulary. This is a planning gate, not permission to delay or couple Sprint 5 to UI
or model work.

## Post-skeleton milestones toward the first playable MVP

### Sprint 6 - Scenario Group One content

- Establish the approved local/external content-pack workflow.
- Normalize and independently verify the required map topology, terrain, order of battle, initial
  placements, supplies, trucks, arrivals, and victory data.
- Load `Graziani's Offensive` with no dangling references and a reproducible content hash.
- Cross-check setup against original sources, September errata, and the VASSAL/community aids.

### Sprint 7 - Remaining required Land systems

- Any initiative, convoy, or weather cases not exercised by the pre-alpha rules laboratory.
- Organization/reorganization, reserve, breakdown/repair, patrol, and construction cases exercised
  by the first scenario.
- Published abstract Air/Logistics behavior required for Land-only play, with known uncertainty
  labeled and covered by adopted rulings.
- Scenario termination and all published victory levels.

This sprint should be split after the scenario's exercised-rule inventory is measured; it is not a
commitment to force every subsystem into one oversized change.

### Sprint 8 - Minimal Maproom and campaign lifecycle

- Start or resume the first scenario through the authoritative host.
- Render an original schematic map, selectable formations, legal actions, phase status, and
  Chronicle explanations.
- For one representative strategic decision, provide contextual non-exhaustive suggested
  approaches, a custom language path, direct map/list/form editing, and one synchronized private
  typed intent draft.
- Use deterministic validation and at most two automatically surfaced clarification questions;
  keep every task complete through structured controls when no parser exists.
- Keep intent confirmation, deterministic Staff-plan review, and final current legal-action
  submission as distinct state-bound steps. Do not invent a plan/batch authority shape inside
  Maproom; integrate only after the governing Legal Actions extension is approved.
- Support local hot-seat play, save/checkpoint, restart, and deterministic replay.
- Treat every local seat handoff as a privacy boundary: invalidate prior-seat work and isolate
  drafts, utterances, parser/worker state, late responses, DOM/live-region content, and browser
  storage before the incoming seat can interact.
- Instrument or record decision cadence, consecutive interaction bursts, pauses, and routine versus
  meaningful player choices needed by `WEB-PACE-EVAL-001`; this evidence is product research, not
  authority inside the game.
- Keep model-backed Intelligence disabled; a deterministic scripted policy may be added only after
  the legal-action surface and fallback behavior are complete. Needle is not required for Sprint 8
  or the first playable MVP.

### MVP fidelity gate

- Every rule exercised by the six-turn scenario is implemented, cited, and tested.
- Known ambiguities have adopted rulings; unsupported mechanics are zero.
- Setup and victory outputs have been independently cross-checked.
- Two local players can complete all six turns through Maproom.
- Every decision, including the representative intent-composer slice, remains completable without a
  model; the next hot-seat player cannot recover the prior player's private composition state.
- Save/resume and seed/event replay reproduce the same campaign.
- Fog-of-war and hidden choices pass negative disclosure tests.
- Rights-sensitive assets have documented permission or original replacements.

### Post-MVP optional parser evidence gate (`INTENT-PARSER-EVAL-001`)

This gate does not block the six-turn deterministic MVP. It blocks adding Needle or another parser
to production Maproom after the no-model composer is complete.

- Retain a consent-safe, versioned corpus covering explicit, ambiguous, contradictory, unsupported,
  typo/speech-like, and adversarial orders for the representative schemas.
- Compare manual structured entry, contextual starters, deterministic aliases, base Needle, and a
  larger constrained model only when needed to diagnose a size-versus-interaction failure.
- Require the semantic, refusal, correction, privacy, accessibility, offline, latency, memory, and
  upgrade thresholds in the Player Intent Composer v1 specification.
- Pin provider/model/schema versions and prove unavailable or disabled parsing leaves the complete
  deterministic composer and campaign path unchanged.
- Produce a decision record that adopts, narrows, or rejects each deployment target. Failure keeps
  the hybrid composer and removes the parser from the release plan without changing authority.

### Post-MVP web-play pacing evidence gate (`WEB-PACE-EVAL-001`)

This gate does not block calling the local six-turn scenario playable. It blocks generalizing the
Campaign Cruise and Engagement Session hypothesis to longer scenarios or starting hosted timing,
delegation, and notification implementation without evidence.

- Record every human decision barrier in at least two complete six-turn playthroughs, including
  elapsed response rhythm, consecutive high-interaction bursts, safe decision boundaries, and
  decisions that players judge routine enough for optional deterministic Staff.
- Determine whether the scenario actually exercises Cruise-like gaps, Engagement-like bursts,
  human/Staff handoff, timeout/pause pressure, and fog-sensitive workflow metadata.
- If it does not, retain the evidence gap and add a bounded rules-laboratory rehearsal or a longer
  scenario study. Do not infer 111-turn campaign behavior from six turns merely because the MVP
  completed.
- Review proposed numeric Cruise deadlines, Engagement duration, invitation response window,
  reminder/grace sequence, quiet hours, and digest behavior with players before selecting defaults.
- Produce a decision record that accepts, revises, or rejects the pacing hypothesis and links its
  evidence. Only an accepted result may unblock `WEB-PACE-001` implementation planning.

## Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Rules contradictions and omissions | Incorrect authoritative behavior | Versioned ruling ledger, source precedence, golden cases |
| Full scenario data transcription dominates delivery | Delayed MVP and silent data errors | Prove schemas first, validate references, double-entry/cross-check critical data |
| Abstract-mode rules are untested | Scenario imbalance or dead ends | Preserve published behavior, label uncertainty, record only necessary rulings |
| Map/counter/rule-text rights are unclear | Distribution or rework risk | External source pack, original visuals, early rights decision |
| Premature generic architecture | Complexity without product value | Build for CNA; extract reuse only from demonstrated duplication |
| Large mutable campaign state | Replay and concurrency defects | Events as authority, snapshots as checkpoints, state-version validation |
| Hidden-state leakage | Invalid game and security failure | Side-specific observations, negative tests, no provider access to hidden state |
| UI arrives before rules stabilize | Polished but incorrect demo | Developer harness first, Maproom after the complete combat skeleton |
| Parser work starts before the interaction is proven | Provider-driven UI, rework, and MVP delay | No-model prototype after Sprint 5; deterministic MVP first; `INTENT-PARSER-EVAL-001` afterward |

## Decision checkpoints

1. **Before Sprint 1:** approve the source baseline, first scenario, and asset posture.
2. **After Sprint 2:** review schemas and legal-action/fog boundary before mechanics expand.
3. **Before Sprint 4:** verify the turn preamble reaches a legal player phase without a generic
   sequence bypass.
4. **After Sprint 5:** review the working skeleton and re-estimate scenario ingestion.
5. **After Sprint 5:** select the representative Player Intent Composer decision and approve its
   Command/Staff/Umpire classification before experimental contracts.
6. **Before Sprint 6:** approve the published content-pack and rights workflow.
7. **Before Sprint 8:** pass the no-model intent-composer interaction and hot-seat isolation gate;
   unresolved plan-to-Legal-Action binding blocks the real Staff slice, not deterministic Maproom.
8. **At MVP:** perform rules-fidelity, security, and code-quality reviews before calling it playable.
9. **After MVP, before parser integration:** execute `INTENT-PARSER-EVAL-001`; an insufficient result
   leaves the deterministic composer intact and blocks model/runtime adoption.
10. **After MVP, before hosted lifecycle work:** execute `WEB-PACE-EVAL-001`; insufficient coverage
   remains a recorded gap and cannot silently approve Cruise/Engagement timing for long campaigns.

## Traceability

The evidence and source classifications behind this roadmap are retained in the
[source-material spike](../research/cna-source-material-spike.md). Each implementation sprint must
link its tests and adopted rulings back to the governing requirement IDs above. Content Pack v1
requirements and deferrals are traced in its
[specification](../specs/content-pack-v1.md) and
[technical design](../design/content-pack-v1.md).
The accepted web-play decisions, independent-review reconciliation, and `WEB-017` through
`WEB-022` consequences are retained in the
[persona/web-play synthesis](../research/persona-models-and-web-play-synthesis.md) and
[web-play shape spike](../research/sandtable-web-play-shape-spike.md). `WEB-PACE-EVAL-001` is the
roadmap evidence gate for `WEB-022`.
The reviewed Player Intent Composer direction, requirement/acceptance IDs, staged tasks, and parser
gates are retained in its [research](../research/player-intent-input-and-needle-feasibility.md),
[specification](../specs/player-intent-composer-v1.md), and
[technical design](../design/player-intent-composer-v1.md). `INTENT-PARSER-EVAL-001` maps to the
specification's parser-adoption thresholds and begins only after deterministic Maproom play.
The accepted `CONTACT-001` rulings are retained in the
[ZOC/Reaction ruling lock](../research/contact-reaction-zoc-source-ruling-lock.md). Their stable
`ZOR-REQ-*`, `ZOR-AC-*`, and dependency-ordered `ZOR-TASK-002A`-`007B` production plan is in the
[ZOC/Reaction specification](../specs/zoc-reaction-v1.md) and
[technical design](../design/zoc-reaction-v1.md); the package is approved, dormant implementation
is complete through `003A`, and `003B` is the next bounded checkpoint.
