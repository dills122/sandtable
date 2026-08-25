# Operation-Stage Entry v1 Specification

**Status:** Implemented; STG-TASK-001 through STG-TASK-022 and STG-TASK-014A complete

**Date:** 2026-08-24

**Capability:** `STAGE-ENTRY-001`

**Research:** [Operation-Stage Entry v1 source and contract spike](../research/operation-stage-entry-spike.md)

**Technical design:** [Operation-Stage Entry v1](../design/operation-stage-entry-v1.md)

**Predecessors:** [Weather Determination v1](weather-determination-v1.md),
[Legal Actions v1](legal-actions-v1.md)

## Objective

Advance the two current synthetic rules-laboratory campaigns from the implemented Operation Stage 1
Organization barrier through explicitly admitted empty Organization, Naval Convoy Arrival,
Commonwealth Fleet Assignment, and Commonwealth Fleet Repair positions. Stop at the first-acting
side's Reserve Designation decision without implementing Reserve or Movement.

Success is a deterministic, replayable, source-ordered path in which every mandatory phase has its
own current legal system action and authoritative event, no missing domain data is treated as zero,
and any unsupported or stale checkpoint rejects with zero events.

## Accepted implementation decisions

The owner accepted `STG-DEC-001` through `STG-DEC-011` in the research packet:

- v1 is exact-fixture and Operation Stage 1 only;
- all four obligation classes are separately and explicitly empty;
- four mechanic-specific system resolutions are required;
- Organization remains one barrier and Fleet remains two ordered segments;
- stage entry stops at first-player Reserve Designation; and
- positive obligations, later stages, Reserve, and Movement remain unsupported.

If any assumption changes, update the research decision and this specification before coding.

## Commands and repository boundaries

Implementation verification uses native .NET 10 Microsoft.Testing.Platform mode:

```bash
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build
dotnet test --solution Sandtable.slnx --no-build
dotnet format Sandtable.slnx --verify-no-changes --no-restore
just check
```

Expected future implementation paths are `src/Cna.Core/Rules`, `Setups`, `Campaigns`, `Actions`,
`Observations`, and focused `tests/Cna.Core.Tests` counterparts. Contract changes begin in the
ruleset ruling and setup/command/event/snapshot schemas; no protobuf, Intelligence, host,
persistence, Maproom, package, or model change belongs to v1.

## Terminology

| Term | Meaning |
| --- | --- |
| Stage-entry policy | Versioned setup data proving that one exact `(GameTurn, OperationStage)` has no supported Organization, arrival, assignment, or repair obligations. |
| Explicit empty | A positive setup assertion backed by a stable repository-synthetic source; never inferred from absent fields. |
| Organization barrier | The one sequence position at which future player-selected Reorganization, Construction, and Training actions may occur in any valid order. |
| Fleet Assignment / Fleet Repair | Two source-ordered Commonwealth Fleet segment positions retained separately even when explicitly empty. |
| First-acting side | Player A for the current Operation Stage, derived from retained stage order and distinct from initiative holder. The authoritative sequence position retains catalog `ActorRole=FirstActingSide` and null `ActiveSide`; legal/observation projection derives the active audience without mutating that position. |
| Stage-entry completion | Successful Fleet Repair resolution reaching Reserve Designation; it does not resolve Reserve. |

## Scope

### In scope

- one versioned setup-hashed policy for the two current synthetic setups;
- strict canonical serialization and clean-cut contract migration;
- four trusted system candidates, commands, and events;
- exact phase/pair/policy/successor validation;
- replay, projection, snapshot, and current-membership enforcement;
- first-acting-side Reserve successor derivation;
- focused authority, sequence, privacy, and deterministic tests; and
- documentation/roadmap reconciliation after implementation evidence exists.

### Out of scope

- positive Reorganization, Construction, Training, arrivals, Fleet Assignment, or Fleet Repair;
- general Naval Convoy or later-stage Initiative Declaration;
- Reserve designation/release, Movement, combat, logistics, naval topology, ships, projects, morale,
  replacement, ammunition, ports, persistence, UI, services, and Intelligence;
- a generic sequence completion command/event; and
- interpreting absent fields as proof of no obligations.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `STG-001` | Setup contract contains one versioned Stage Entry policy bound to an exact Game Turn and Operation Stage and four closed obligation assertions: Organization, Naval Convoy Arrival, Fleet Assignment, and Fleet Repair. |
| `STG-002` | V1 admits only `explicit-none` for all four assertions and only for the two setup-hashed synthetic fixtures at their Operation Stage 1 checkpoint. |
| `STG-003` | Policy sources are nonempty, unique, canonically ordered, and exactly match the adopted repository-synthetic source. Missing, altered, extra, or duplicate sources reject. |
| `STG-004` | The policy participates in setup canonical bytes/hash, campaign creation history, snapshot bytes, projection validation, and replay preparation. The new executable rejects prior contract versions rather than guessing a default. |
| `STG-005` | At an admitted Organization barrier, the system audience receives exactly one `resolve-no-obligation-organization` candidate; side audiences receive none. |
| `STG-006` | Accepted Organization resolution emits exactly one mechanic-specific event, increments state version once, stays in the same pair, and advances exactly to Naval Convoy Arrival. It does not define or execute positive Organization segment order. |
| `STG-007` | At admitted Naval Convoy Arrival, the system audience receives exactly one `resolve-no-obligation-naval-convoy-arrival` candidate; side audiences receive none. |
| `STG-008` | Accepted arrival resolution emits exactly one event and advances exactly to Commonwealth Fleet Assignment without creating units, replacements, ammunition, ports, or logistics facts. |
| `STG-009` | At admitted Fleet Assignment, the system audience receives exactly one `resolve-no-obligation-fleet-assignment` candidate; side audiences receive none. |
| `STG-010` | Accepted empty assignment emits exactly one event and advances exactly to Fleet Repair without assigning ships or implying that positive assignment is a system choice. |
| `STG-011` | At admitted Fleet Repair, the system audience receives exactly one `resolve-no-obligation-fleet-repair` candidate; side audiences receive none. |
| `STG-012` | Accepted empty repair emits exactly one event and advances to the unchanged catalog Reserve Designation position with actor role `FirstActingSide` and null stored active side. It does not expose or accept a Reserve action. |
| `STG-013` | Preserve Legal Actions v1 binding: the action-set envelope carries current campaign ID, state version, ruleset hash, position ID, audience, and ordered candidates; each payload-free Stage Entry candidate carries contract version, action ID, and closed kind; submission carries contract version, campaign ID, expected state version, expected position ID, audience, and action ID. |
| `STG-014` | Submission is accepted only when it belongs to the exact current system action set and its matching command/event factory rechecks policy, pair, phase, and successor. |
| `STG-015` | Stale, wrong-audience, malformed, unsupported-version, wrong-pair, wrong-phase, missing-policy, altered-policy, unexpected-successor, and already-resolved submissions reject with a typed reason and zero events. |
| `STG-016` | Each event retains from-position, exact catalog successor position, stable mechanic-specific sources, and the admitted pair. Event deserialization and projection reject any impossible or reordered transition, including a Reserve successor with a materialized `ActiveSide`. |
| `STG-017` | Accepted transitions preserve campaign/content/ruleset/setup identities, initiative holder, complete stage order, Weather record, world state, random stream/cursor, and all unrelated authoritative values byte-for-byte. |
| `STG-018` | Projection from creation history recomputes every stage-entry event rather than trusting serialized event payloads; replay produces a byte-identical snapshot and Chronicle sequence. |
| `STG-019` | Side observations reveal only the already public position/active-audience semantics. At the catalog Reserve successor, observation and legal-action projection derive the active side from the retained pair-keyed stage order while authority keeps null `SequencePosition.ActiveSide`. Policy internals and rules provenance do not cross the observation boundary. |
| `STG-020` | A current synthetic campaign reaches Reserve Designation only after all four stage-entry events occur in exact order. No public/internal generic command can bypass an event. |
| `STG-021` | V1 rejects Operation Stages 2-3, later turns, missing policy, any recognized non-`explicit-none` obligation-policy kind, or any checkpoint without retained valid stage order; it does not silently reuse Stage 1 admission or claim to detect unmodelled positive domain subjects. |
| `STG-022` | Stage-entry events are distinct from opening Naval Convoy Schedule/Tactical Shipping events and cannot satisfy or replace their policies. |
| `STG-023` | The first-acting Reserve audience is derived from the current pair-keyed `CampaignOperationStageOrder` in legal-action and observation projection. The catalog sequence position remains byte/equality compatible for snapshot validation and future `GetNext`; initiative holder remains unchanged and may differ from first-acting side. |
| `STG-024` | No model, remote I/O, wall clock, database, filesystem, unseeded randomness, or optional service participates in generation, validation, execution, projection, or replay. |
| `STG-025` | The ruleset manifest contains the approved explicit-empty-stage-entry ruling with exact conflict, alternatives, selected behavior, protecting IDs `STG-AC-001`, `STG-AC-002`, `STG-AC-004`, `STG-AC-005`, `STG-AC-006`, `STG-AC-009`, and `STG-AC-010`, and canonical source set. Mechanic events use the frozen per-mechanic source sets from the research decision. The ruleset hash migration is retained and replay-validated. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `STG-NFR-001` | All new contracts are closed, immutable, versioned, strictly serialized, culture-independent, and reject unknown, missing, reordered, duplicate, or noncanonical data. |
| `STG-NFR-002` | Equal admitted input produces byte-identical action sets, events, snapshots, projection, and replay independent of process, culture, collection order, clock, or machine. |
| `STG-NFR-003` | No public authority-bearing snapshot, event, setup policy, command, or projector becomes accessible outside existing safe Core facades and test-only friend boundaries. |
| `STG-NFR-004` | Side legal-action and observation bytes are invariant under valid opponent-only hidden-force mutations at every system-only stage-entry position and at the Reserve successor. |
| `STG-NFR-005` | New behavior is covered primarily by deterministic unit/contract tests plus focused end-to-end campaign/replay tests; no timing-sensitive assertion is permitted. |
| `STG-NFR-006` | Implementation tasks target at most five material files each. Tasks 002-003 and 011 onward require focused RED/GREEN evidence. The clean-cut identity lane may leave the shared test project uncompilable or its old goldens invalid during Tasks 004-009, including Task 007A; those tasks retain an exact unresolved-failure inventory plus available source/Core-build evidence and must not claim executable GREEN. Task 010 must resolve that inventory and leave the existing Weather and Exercise suites green; every later checkpoint must remain green. No intermediate identity-lane task is mergeable. |

## Acceptance scenarios

| ID | Scenario | Expected result |
| --- | --- | --- |
| `STG-AC-001` | Create either current synthetic campaign, resolve through Weather, and query system actions at Organization | Exactly one admitted Organization resolution appears; both side sets are empty. |
| `STG-AC-002` | Submit each of the four current system actions in order | Four events advance Organization → Arrival → Assignment → Repair → first-side Reserve, one state version at a time. |
| `STG-AC-003` | Inspect authority after each accepted event | World, initiative, stage order, Weather, RNG, setup/ruleset/content identity, and prior history are unchanged except version/position/new event. |
| `STG-AC-004` | Remove, alter, misbind, or add a Stage Entry policy source/assertion | Creation/projection/query/submission rejects before advancement; no event or random draw occurs. |
| `STG-AC-005` | Submit a stale, duplicate, side-audience, wrong-position, or out-of-order stage-entry action | Typed rejection and zero events. |
| `STG-AC-006` | Rehash a history with skipped/reordered event, changed from/successor position, wrong pair, or fabricated positive outcome | Projection/replay rejects as invalid history. |
| `STG-AC-007` | Change hidden opposing formation/element facts while preserving valid public state | Complete side observations and side legal-action sets remain semantically and byte identical. |
| `STG-AC-008` | Initiative holder elects to act last, then stage entry completes | Authority retains the catalog Reserve position with null stored active side; legal/observation projection selects the opponent as first-acting audience; initiative holder remains unchanged; subsequent sequence lookup remains valid. |
| `STG-AC-009` | Attempt v1 at Operation Stage 2/3, another turn, with missing policy, or with a recognized unsupported obligation-policy kind | Unsupported typed rejection; zero events; no policy reuse. This does not claim detection of unmodelled positive domain subjects. |
| `STG-AC-010` | Replay the accepted four-event sequence from creation history and in a fresh session | Snapshot, event bytes, action succession, and authority digest are byte identical. |
| `STG-AC-011` | Inspect public Core surface and production project references | No new raw authority, policy, event, command, or replay mutation seam is public or referenced by DecisionWorker/Intelligence. |
| `STG-AC-012` | Query/project at Reserve after Fleet Repair | Stage-entry system set is empty; no Reserve behavior is present; authority position remains the exact catalog value; projected active audience is the first-acting side; `GetNext` can still locate the position. |

## Testing strategy

- **Contract tests:** setup/policy, action, event, snapshot, strict JSON, golden bytes, culture and
  collection-order invariance, unsupported-version and malformed matrices.
- **Pure transition tests:** each factory/resolver enforces exact phase/pair/policy/successor and
  preserves unrelated state.
- **Legal-action tests:** exact system candidates, empty side sets, stale/wrong-audience membership,
  and no generic successor.
- **History/replay tests:** recomputation, forged/reordered/skipped event negatives, fresh-session
  replay, byte-identical authority digest.
- **Fog tests:** paired opponent-only mutations compare semantic values and canonical bytes at each
  new position and the Reserve successor.
- **Public-surface tests:** no new authority-bearing public API or production reference.

## Boundaries

### Always

- update setup/command/event/snapshot contracts before consumers;
- preserve mechanic-specific legal membership and zero-event rejection;
- derive Reserve audience from retained stage order;
- run narrow tests before repository gates; and
- update research/spec/design before changing an approved decision.

### Ask first

- any positive Organization/arrival/Fleet behavior;
- support beyond the exact Stage 1 fixture pair;
- a new public/protobuf/persistence/host contract;
- a dependency or ruleset ruling not listed in the research packet; or
- combining this capability with Reserve or Movement.

### Never

- infer no obligations from missing data;
- add a generic advance/complete-preamble command;
- let a side client submit a trusted system action;
- expose policy/rules provenance or hidden state through observations; or
- skip Fleet Assignment, Fleet Repair, or Reserve to reach Movement.

## Completion gate

Implementation began only after:

1. the owner approves or revises `STG-DEC-001` through `STG-DEC-011`; **satisfied by owner
   acceptance on 2026-08-24**;
2. `EXR-TASK-014` is complete and the planning branch is based on merged commit
   `a022b784cda90be90ff6af9802c0c0352b9f89a6`; **satisfied by STG-TASK-001**;
3. contract migration versions and exact policy source identity are frozen; **satisfied by
   STG-TASK-001**;
4. every task in the technical design is refined to at most five material files; **satisfied by
   STG-TASK-001 and retained through delivery**;
5. requirement → task/checkpoint → evidence traceability is complete; **satisfied and closed by
   STG-TASK-020 through STG-TASK-022**; and
6. one independent planning review finds no unresolved P0/P1 issue; **satisfied by Task 001 review
   instance 3 of 3 (`Ready`, no P0-P3 findings)**.

## Owner approval gate

`STG-TASK-001` resolved both former open questions as follows:

- use a separate `CampaignStageEntryPolicy` contract v1 rather than changing the meanings of the
  opening-preamble or Weather policy contracts; and
- clean-cut ruleset contract `3→4`, setup schema `4→5`, create-command and campaign-created event
  `4→5`, and campaign snapshot `5→6`. New Stage Entry policy, commands, events, and candidates begin
  at contract 1; Land sequence, operation-stage order, Legal Action set/submission, observation,
  opening-preamble policy, and Weather policy versions do not change.

The exact fields, ordinals, identifiers, sources, inventory boundary, and task ownership are frozen
in the independently reviewed technical design. The project owner accepted `STG-DEC-001` through
`STG-DEC-011` as written on 2026-08-24.

System action dispatch is no longer open: `STG-TASK-011` characterizes existing behavior and makes
semantic position/policy dispatch a prerequisite to the Stage Entry transition slices.

## Implementation closure

The delivered capability satisfies the frozen explicit-empty boundary for both admitted setups and
stops at Reserve without implementing Reserve behavior. The final authority review found no
production or test defect; its sole planning-state finding was accepted and corrected, and the
3-of-3 review limit is closed. Final `just check` passed format verification, built the solution
with 0 warnings and 0 errors, and passed 619/619 tests with no failures or skips.
