# Sandtable Pre-Alpha Roadmap

**Status:** Active

**Rules target:** `cna-1979.1`

**First playable scenario:** Land-only `Graziani's Offensive`

## Outcome

Deliver a visible, deterministic skeleton quickly, then expand it into a faithful implementation
of the shortest published scenario. Future architecture is protected through typed contracts,
versioned rule data, replay, source provenance, and explicit extension points. Unsupported rules
remain unsupported; they are never replaced with invented approximations.

Sprints below are capability increments rather than calendar promises. The first four are planned
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
           v                           v
Movement/contact loop        Scenario data ingestion
           |                           |
           v                           |
Combat loop <--------------------------+
           |
           v
Working pre-alpha skeleton
           |
           v
Remaining Land systems + minimal Maproom
           |
           v
Six-turn playable MVP
```

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

**Status:** Foundation delivered. Campaign creation and replay are authoritative; advancement is
blocked at the first mandatory unimplemented mechanic. RNG proof remains deferred to the first
mechanic that legitimately consumes randomness.

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
- [x] Campaign creation records the declared seed deterministically; no clock, generated identifier,
  transition, or random draw exists in the accepted history for this slice.
- [ ] The first mechanic that consumes randomness proves different seeds affect only its declared
  random outcomes.

**Verification:** Focused Core tests, full solution build, and full test suite.

### Sprint 1 demonstration

Create a campaign, display its creation event as Chronicle input, replay it to the same state, and
prove that progression at Initiative Determination is rejected without emitting an event. Separately
rehearse the source-cited sequence outline through the first player Movement segment.

The executable Core tests distinguish authoritative adjudication from sequence inspection. The
accepted creation event is replayed to canonical state; the mandatory Initiative Determination
mechanic rejects generic advancement until its real decision and random outcome are modeled. The
catalog rehearsal produces no Chronicle events. A human-facing Chronicle display belongs to the
later Maproom slice.

## Sprint 2: Rules laboratory and legal-action boundary

**Goal:** Load an original, license-safe test theater and expose only legal, side-safe actions.

### Task 2.1 - Versioned content schemas

Define schemas for hexes/hexsides, terrain, sides, formations, combat elements, placements, and
scenario metadata. Each record carries provenance or is marked as a synthetic test fixture.

**Acceptance criteria:**

- Invalid coordinates, references, duplicate IDs, and impossible placements fail at load time.
- Canonical serialization produces a stable content hash.
- Schema evolution is versioned independently from scenario content.

### Task 2.2 - Rules-laboratory fixture

Create a small original hex theater containing only the terrain and units needed to exercise the
first vertical slice. It must be visibly labeled nonhistorical and not presented as a CNA scenario.

**Acceptance criteria:**

- The fixture uses original placeholder names and visuals.
- It loads through the same path intended for published-scenario data.
- No test depends on original map or counter artwork.

### Task 2.3 - Observation and legal-action queries

Generate side-specific observations and currently legal commands without mutating state.

**Acceptance criteria:**

- Hidden opposing details never appear in an observation or candidate action.
- Every accepted command was present in the acting side's legal-action set.
- Stale actions are rejected by state version.

**Verification:** Schema validation tests, fog-of-war negative tests, full repository gate.

### Sprint 2 demonstration

Load the rules laboratory, inspect one side's redacted state, and select an action from the legal
set while the opposing hidden state remains absent from logs and responses.

## Sprint 3: Continual movement and contact

**Goal:** Complete the movement half of one authentic movement/contact/combat loop.

### Task 3.1 - Capability Points and cohesion ledger

Implement source-cited Capability Point expenditure, over-extension, disorganization, and
reorganization as events.

**Acceptance criteria:**

- CP cost and cohesion changes can be explained from emitted ledger entries.
- Rejected expenditure emits no authoritative event.
- Replaying the ledger recreates the same remaining capability and cohesion.

### Task 3.2 - Terrain, stacking, and zones of control

Implement only the terrain and unit categories present in the rules laboratory, using normalized
tables rather than resolver constants.

**Acceptance criteria:**

- Terrain and hexside costs are table-driven and source-cited.
- Stacking and ZOC violations are rejected before movement.
- Adding an unsupported terrain/unit category fails explicitly.

### Task 3.3 - Continual movement, reaction, and contact

Allow repeatable movement segments, contact state, reaction, and break-off decisions within the
correct player phase.

**Acceptance criteria:**

- A unit can move, contact, resolve the permitted response, and continue only when rules allow.
- Legal actions change after each accepted event and state-version increment.
- Opposing reactions receive only side-safe observations.

**Verification:** Table-driven deterministic tests for legal, illegal, boundary, and replay cases.

### Sprint 3 demonstration

Move a formation across different terrain, enter contact, resolve a reaction, spend capability,
and show the resulting cohesion and Chronicle explanation.

## Sprint 4: One complete combat loop

**Goal:** Resolve a representative combat from declaration through final state.

### Task 4.1 - Combat declaration and force assignment

Model position determination, barrage declaration, retreat-before-assault, and secret force
assignment without exposing the opponent's hidden choice.

### Task 4.2 - Seeded combat resolution

Implement the minimum source-cited barrage, anti-armor, and close-assault table rows needed by the
fixture. Record dice, modifiers, table coordinates, and outcomes as events.

### Task 4.3 - Losses, retreat, and reserve release

Apply casualties, cohesion/morale effects, retreat/surrender conditions used by the fixture, and
reserve release in sequence.

**Acceptance criteria:**

- Resolution follows the published segment order.
- Simultaneous outcomes are computed from the same pre-resolution state.
- Every random result and table lookup is inspectable and replayable.
- Unsupported table rows or special cases stop adjudication explicitly.

**Verification:** Golden event-stream tests plus full repository gate.

### Sprint 4 demonstration and pre-alpha checkpoint

Complete movement, contact, combat, loss application, and reserve release in the rules laboratory;
restart from the seed/event log and obtain the same result. This is the working pre-alpha skeleton.

## Post-skeleton milestones toward the first playable MVP

### Sprint 5 - Scenario Group One content

- Establish the approved local/external content-pack workflow.
- Normalize and independently verify the required map topology, terrain, order of battle, initial
  placements, supplies, trucks, arrivals, and victory data.
- Load `Graziani's Offensive` with no dangling references and a reproducible content hash.
- Cross-check setup against original sources, September errata, and the VASSAL/community aids.

### Sprint 6 - Remaining required Land systems

- Initiative and weather.
- Organization/reorganization, reserve, breakdown/repair, patrol, and construction cases exercised
  by the first scenario.
- Published abstract Air/Logistics behavior required for Land-only play, with known uncertainty
  labeled and covered by adopted rulings.
- Scenario termination and all published victory levels.

This sprint should be split after the scenario's exercised-rule inventory is measured; it is not a
commitment to force every subsystem into one oversized change.

### Sprint 7 - Minimal Maproom and campaign lifecycle

- Start or resume the first scenario through the authoritative host.
- Render an original schematic map, selectable formations, legal actions, phase status, and
  Chronicle explanations.
- Support local hot-seat play, save/checkpoint, restart, and deterministic replay.
- Keep model-backed Intelligence disabled; a deterministic scripted policy may be added only after
  the legal-action surface and fallback behavior are complete.

### MVP fidelity gate

- Every rule exercised by the six-turn scenario is implemented, cited, and tested.
- Known ambiguities have adopted rulings; unsupported mechanics are zero.
- Setup and victory outputs have been independently cross-checked.
- Two local players can complete all six turns through Maproom.
- Save/resume and seed/event replay reproduce the same campaign.
- Fog-of-war and hidden choices pass negative disclosure tests.
- Rights-sensitive assets have documented permission or original replacements.

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

## Decision checkpoints

1. **Before Sprint 1:** approve the source baseline, first scenario, and asset posture.
2. **After Sprint 2:** review schemas and legal-action/fog boundary before mechanics expand.
3. **After Sprint 4:** review the working skeleton and re-estimate scenario ingestion.
4. **Before Sprint 5:** approve the content-pack and rights workflow.
5. **At MVP:** perform rules-fidelity, security, and code-quality reviews before calling it playable.

## Traceability

The evidence and source classifications behind this roadmap are retained in the
[source-material spike](../research/cna-source-material-spike.md). Each implementation sprint must
link its tests and adopted rulings back to the governing requirement IDs above.
