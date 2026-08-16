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
Movement/contact loop                   |
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

**Status:** Foundation and Initiative Determination vertical slice delivered. Campaign creation,
the first seeded mechanic, canonical event serialization, and replay are authoritative;
advancement is blocked at Naval Convoy, the next mandatory unimplemented mechanic.

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
Initiative events as Chronicle input, replay them to identical canonical state, and prove that the
campaign stops at Naval Convoy. The contested fixture derives published Initiative Ratings, rolls
Axis then Commonwealth through the versioned SHA-256 counter stream, records complete tie rerolls,
and retains exact source provenance. The predetermined fixture consumes no draw.

The executable Core tests distinguish authoritative adjudication from catalog inspection. Generic
sequence advancement and legacy contract-v1 history cannot cross Initiative Determination;
`ResolveInitiative` is the sole accepted command and its event is recomputed during projection. A
human-facing Chronicle display belongs to the later Maproom slice.

Implementation and acceptance evidence are tracked in the
[Initiative Determination specification](../specs/initiative-determination.md) and
[technical design](../design/initiative-determination.md).

## Sprint 2: Rules laboratory and legal-action boundary

**Goal:** Load an original, license-safe test theater and expose only legal, side-safe actions.

Content Pack v1 is governed by the
[research packet](../research/content-pack-v1-spike.md),
[specification](../specs/content-pack-v1.md), and
[technical design](../design/content-pack-v1.md). A fresh independent readback returned `Ready`;
the production implementation begins only after project-owner approval.

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

### Task 2.3 - Content admission and initial campaign world

Bind the exact content ID/hash and selected scenario to a versioned campaign world and project its
initial entity positions through authoritative creation history.

**Acceptance criteria:**

- Missing or mismatched historical content rejects before campaign creation and replay never
  resolves a mutable “latest” catalog entry.
- Creation records exact ruleset/content identities and replay recreates byte-identical initial
  world state using the matching historical executable and content bytes.

### Task 2.4 - Side-safe observation projection

Project the authoritative content/world for one acting side without mutating state.

**Acceptance criteria:**

- Hidden opposing details never appear in an observation.
- Full Content Pack/world values never appear in a player or Intelligence DTO.
- Equal state and side produce byte-equivalent observations.

### Task 2.5 - Legal-action generation and enforcement

Generate currently legal commands from side-safe state and enforce optimistic concurrency.

**Acceptance criteria:**

- Hidden opposing details never appear in a candidate action.
- Every accepted command was present in the acting side's legal-action set.
- Stale actions are rejected by state version.
- Querying legal actions does not mutate state or consume randomness.

**Verification:** Schema validation tests, fog-of-war negative tests, full repository gate.

### Sprint 2 demonstration

Load the rules laboratory, inspect one side's redacted state, and select an action from the legal
set while the opposing hidden state remains absent from logs and responses.

## Sprint 3: Mandatory turn preamble

**Goal:** Advance from Naval Convoy to the first player-execution phase without skipping a
mandatory rule or deleting the initiative holder's choices.

### Task 3.1 - Turn-preamble source and contract spike

Determine the smallest source-faithful Naval Convoy, Initiative Declaration, and Weather
Determination capability exercised by the rules laboratory. Define commands, events, normalized
tables, required content/world facts, and explicit unsupported cases before implementation.

### Task 3.2 - Naval Convoy and Initiative Declaration

Implement the required Naval Convoy decision/resolution and the initiative holder's separate
first-or-last declaration for each Operation Stage. No generic sequence command may bypass either
mechanic.

### Task 3.3 - Weather Determination and stage entry

Resolve the required source-cited weather procedure through the versioned random stream and enter
the declared first-acting side's first player phase.

**Acceptance criteria:**

- Every accepted transition is produced by a mechanic-specific command/event and replays exactly.
- Unsupported Naval Convoy or weather cases stop with typed rejection and zero events.
- Initiative holder, first-acting side, and second-acting side remain separate authoritative facts.
- A side-safe legal-action query exposes only the decisions available at the current preamble step.
- The campaign reaches a movement-capable player phase only after all required preamble mechanics
  are complete.

**Verification:** Source/table boundary tests, forged-history negatives, deterministic replay, and
the full repository gate.

### Sprint 3 demonstration

Continue the synthetic campaign from Naval Convoy, make the available preamble decisions, resolve
weather, and stop at the correct first-acting player phase with a replay-identical Chronicle.

## Sprint 4: Continual movement and contact

**Goal:** Complete the movement half of one authentic movement/contact/combat loop.

### Task 4.1 - Capability Points and cohesion ledger

Implement source-cited Capability Point expenditure, over-extension, disorganization, and
reorganization as events.

**Acceptance criteria:**

- CP cost and cohesion changes can be explained from emitted ledger entries.
- Rejected expenditure emits no authoritative event.
- Replaying the ledger recreates the same remaining capability and cohesion.

### Task 4.2 - Terrain, stacking, and zones of control

Implement only the terrain and unit categories present in the rules laboratory, using normalized
tables rather than resolver constants.

**Acceptance criteria:**

- Terrain and hexside costs are table-driven and source-cited.
- Stacking and ZOC violations are rejected before movement.
- Adding an unsupported terrain/unit category fails explicitly.

### Task 4.3 - Continual movement, reaction, and contact

Allow repeatable movement segments, contact state, reaction, and break-off decisions within the
correct player phase.

**Acceptance criteria:**

- A unit can move, contact, resolve the permitted response, and continue only when rules allow.
- Legal actions change after each accepted event and state-version increment.
- Opposing reactions receive only side-safe observations.

**Verification:** Table-driven deterministic tests for legal, illegal, boundary, and replay cases.

### Sprint 4 demonstration

Move a formation across different terrain, enter contact, resolve a reaction, spend capability,
and show the resulting cohesion and Chronicle explanation.

## Sprint 5: One complete combat loop

**Goal:** Resolve a representative combat from declaration through final state.

### Task 5.1 - Combat declaration and force assignment

Model position determination, barrage declaration, retreat-before-assault, and secret force
assignment without exposing the opponent's hidden choice.

### Task 5.2 - Seeded combat resolution

Implement the minimum source-cited barrage, anti-armor, and close-assault table rows needed by the
fixture. Record dice, modifiers, table coordinates, and outcomes as events.

### Task 5.3 - Losses, retreat, and reserve release

Apply casualties, cohesion/morale effects, retreat/surrender conditions used by the fixture, and
reserve release in sequence.

**Acceptance criteria:**

- Resolution follows the published segment order.
- Simultaneous outcomes are computed from the same pre-resolution state.
- Every random result and table lookup is inspectable and replayable.
- Unsupported table rows or special cases stop adjudication explicitly.

**Verification:** Golden event-stream tests plus full repository gate.

### Sprint 5 demonstration and pre-alpha checkpoint

Complete movement, contact, combat, loss application, and reserve release in the rules laboratory;
restart from the seed/event log and obtain the same result. This is the working pre-alpha skeleton.

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
3. **Before Sprint 4:** verify the turn preamble reaches a legal player phase without a generic
   sequence bypass.
4. **After Sprint 5:** review the working skeleton and re-estimate scenario ingestion.
5. **Before Sprint 6:** approve the published content-pack and rights workflow.
6. **At MVP:** perform rules-fidelity, security, and code-quality reviews before calling it playable.

## Traceability

The evidence and source classifications behind this roadmap are retained in the
[source-material spike](../research/cna-source-material-spike.md). Each implementation sprint must
link its tests and adopted rulings back to the governing requirement IDs above. Content Pack v1
requirements and deferrals are traced in its
[specification](../specs/content-pack-v1.md) and
[technical design](../design/content-pack-v1.md).
