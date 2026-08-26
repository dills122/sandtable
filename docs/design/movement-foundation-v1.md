# Movement Foundation v1 Technical Design

**Status:** Approved implementation plan; `MOV-TASK-001` complete and `MOV-TASK-002` ready

**Date:** 2026-08-25

**Capability:** `MOVE-001`

**Specification:** [Movement Foundation v1](../specs/movement-foundation-v1.md)

**Research:** [Movement Foundation Spike](../research/movement-foundation-spike.md)

## Design summary

Movement Foundation v1 extends the existing Core authority vertically. It does not add a service or
let the Exercise Runner adjudicate. `Cna.Core` owns rules data, content admission, representation,
world state, observations, legal actions, commands, events, projection, and replay. The runner only
chooses current side-safe candidates and verifies evidence.

```text
Content mobility + topology       Cna1979 Movement tables
             \                         /
              v                       v
       Campaign content context + world/representation truth
                            |
              +-------------+-------------+
              |                           |
              v                           v
    side-safe observation          authoritative validation
              |                           |
              v                           |
       legal action set ------------------+
              |
              v
      typed submission -> command -> one event -> projector -> successor
                                                        |
                                                        v
                                            replay / Exercise evidence
```

## Architecture boundaries

### Rules

`src/Cna.Core/Rules` owns:

- the mobility vocabulary;
- exact Capability Point arithmetic;
- normalized Movement cost/stacking tables and provenance; and
- pure classification/cost lookup with explicit unsupported results.

Rules never read observations and never mutate a campaign.

### Content

`src/Cna.Core/Content` assigns one supported mobility ID to each element and includes it in strict
canonical content identity. It continues to own topology, terrain/edge assignment, organization,
and base CPA source facts. It does not store runtime expenditure, Cohesion, representation
location, ZOC, or derived costs.

### Campaign authority

`src/Cna.Core/Campaigns` owns:

- current operational ledger state;
- authoritative map representations and real bindings;
- Movement commands/events, transition validation, projection, and completion; and
- snapshot/event canonical contracts.

All mutation occurs through accepted events. Hidden bindings remain internal.

### Observation and legal actions

`src/Cna.Core/Observations` projects own operational facts, including the acting side's mobility
classification, plus the approved minimum apparent opposing presence. `src/Cna.Core/Actions`
generates candidates only from one side's observation and maps validated submissions to internal
commands. An authority-state query may validate that the observation itself is admitted, but
candidate membership must not consult additional hidden truth.

### Exercise Harness

`Cna.ExerciseRunner` receives no resolver or world mutation API. A new closed controller policy may
track accepted own Movement actions in runner-local history, select each eligible mover at most
once, then choose the completion candidate. It submits canonical action IDs through the same Core
path as a future user or Needle adapter.

## Contract shapes

Exact public/internal type names may be refined during the contract task, but their responsibilities
are frozen by the specification.

### Exact amount

`CapabilityPointAmount` is an immutable normalized pair:

```text
numerator: non-negative Int64
denominator: positive Int32
gcd(numerator, denominator) == 1
zero => 0/1
```

Checked arithmetic rejects overflow. Canonical JSON is an object with fixed property order, not a
JSON floating-point number or culture-formatted string.

### Operational state

Each element carries one current `CampaignElementOperationalState`:

```text
ledgerGameTurn
ledgerOperationStage
capabilityPointsExpended
cohesionLevel
```

It is embedded in the element/world contract so a location-changing event cannot update one without
the other. Snapshot validation requires ledger turn/stage equality at supported Operation
checkpoints. Later stage-entry work will reset expenditure through an event; v1 only admits the
already supported Operation Stage 1 path.

### Representation

One `CampaignMapRepresentationState` contains:

```text
representationId
currentLocationId
bindingKind = independent-element
boundElementIds (internal only)
```

The v1 binding kind is closed. Attachment and dummy kinds require a later contract version.
Initial IDs are deterministic from admitted content/scenario data and are recorded in creation
truth so replay does not rely on a later naming implementation.

The outward apparent type is separate and contains only representation ID, location, and supported
ZOC-exertion fact. It has no binding collection.

### Movement action and event

One `MoveElementAction` identifies own element, origin, destination, and a side-safe cost breakdown.
One `CompleteMovementSegmentAction` carries no optional target.

The internal `MoveElement` command repeats expected state version, expected position, acting side,
element, origin, and destination. The Umpire recalculates cost; it never trusts a submitted cost.

The accepted `ElementMoved` event contains the validated real binding and exact ledger delta. Cost
components use closed semantic kinds such as destination terrain, route override, and crossed
hexside. The event stores `RuleReference` values required to explain the adjudication; it does not
store copied prose. V1 accepts only a resulting cumulative expenditure at or below base CPA, so its
before/after Cohesion values are equal and it carries no Disorganization conversion.

`MovementSegmentCompleted` preserves the world and records the exact Movement and Breakdown
positions.

## Validation order

Movement submission validates in this order and emits nothing until all checks pass:

1. contract/version and canonical action identity;
2. expected campaign state version and exact position ID;
3. first-acting side and audience ownership;
4. admitted ruleset/setup/content/context and current snapshot;
5. own element, representation binding, current origin, Reserve, Cohesion, and ledger stage;
6. adjacent destination and supported topology direction;
7. apparent-presence/contact boundary;
8. mobility/terrain/edge table support and exact cost;
9. resulting cumulative expenditure at or below base CPA;
10. conservative destination/traversal stacking; and
11. exact successor/event invariants.

Rejection reasons remain coarse enough not to reveal more opposing information than the acting
observation. Detailed internal diagnostics may exist only in trusted authority evidence.

## Fog invariant

The strongest testable invariant is:

> If two admitted campaign authorities project byte-identical observations for a side, querying
> legal actions for that side returns byte-identical canonical action-set bytes.

This prevents a future resolver from consulting a hidden binding after projection and accidentally
turning candidate omission into reconnaissance. Command adjudication may still reject a stale
candidate after authority changed, but the rejection exposed to the side remains nonrevealing.

## Sequence behavior

Accepted moves keep the exact current Movement position. The player may move the same eligible unit
again because continual movement is not a one-move allowance. The runner's bounded policy, not the
Umpire, chooses to move each element at most once in the first checked Maneuver.

Completion uses `Cna1979LandSequence.GetNext` and additionally asserts the exact supported
Breakdown Determination successor. It does not use a generic “next phase” event and it exposes no
Breakdown candidate.

## Task graph

```text
MOV-TASK-001 source/ruling lock [complete]
                  |
                  v
MOV-TASK-002 amount/table
                  |
                  v
MOV-TASK-003 content mobility
                  |
                  v
MOV-TASK-004 world + representation contracts
                  |
                  v
MOV-TASK-005 observation/apparent presence
                  |
                  v
MOV-TASK-006 action/submission contracts
                  |
                  v
MOV-TASK-007 move command/event/adjudication
                  |
                  v
MOV-TASK-008 completion to Breakdown
                  |
                  v
MOV-TASK-009 end-to-end + Exercise/Maneuver evidence
                  |
                  v
MOV-TASK-010 synchronization + independent review
```

Task 001 is complete. Tasks 002 through 010 are intentionally serial because each freezes a
versioned contract consumed by the next layer. In particular, Content admission in Task 003
consumes the closed mobility vocabulary and ruleset identity completed by Task 002. Tasks 006 and
007 keep all public Movement membership dormant; Task 008 atomically exposes executable move and
completion actions so no intermediate checkpoint can strand a campaign at Movement.

## Implementation tasks

### `MOV-TASK-001` - Lock sources and owner rulings

**Status:** Complete (2026-08-25)

**Advances:** `MOV-REQ-001`, `MOV-REQ-002`, `MOV-REQ-005`; `MOV-DEC-001` through `MOV-DEC-008`

**Dependencies:** owner-approved research/specification/design package (satisfied 2026-08-25)

**Owned output:** approved decision status, recorded visibility ruling, normalized source locator
inventory, explicit all-over-CPA/Disorganization deferral including fractional excess, and updated
task authorization

**Acceptance:** no open decision changes an outward or authoritative v1 field; the source table and
errata precedence are independently checkable

**Verification:** documentation traceability review and source-vector checklist

### `MOV-TASK-002` - Implement exact amounts and normalized rules tables

**Advances:** `MOV-REQ-001`, `MOV-REQ-002`; `MOV-AC-001`, `MOV-AC-002`

**Dependencies:** `MOV-TASK-001`

**Owned modules:** `src/Cna.Core/Rules` and matching `tests/Cna.Core.Tests/Rules`

**Observable output:** exact rational value object, mobility vocabulary, closed Movement lookup
results, normalized table artifact/serializer if retained, golden source vectors, and ruleset hash
update

**Acceptance:** corrected Track vectors, canonical normalization, overflow/unsupported negatives,
and per-row provenance pass before any campaign mutation code exists

**Verification:** focused Rules namespace tests plus golden-byte/hash vectors

### `MOV-TASK-003` - Version Content Pack mobility facts

**Advances:** `MOV-REQ-003`; `MOV-AC-003`

**Dependencies:** `MOV-TASK-002`

**Owned modules:** Content authoritative contracts, validators, canonical codec, synthetic catalog,
content compatibility, every affected setup/content identity fixture, and matching Content tests

**Observable output:** one required mobility ID per combat element under the new content version;
the synthetic lab explicitly classifies its elements

**Acceptance:** missing/unknown mobility rejects; canonical identity changes exactly once; no
derived stacking/cost/runtime fields enter content

**Verification:** Content contract/validation/canonical/hash tests and fixture readback

### `MOV-TASK-004` - Version world, snapshot, creation, and representation contracts

**Advances:** `MOV-REQ-004`, `MOV-REQ-005`, `MOV-REQ-010`; `MOV-AC-004`

**Dependencies:** `MOV-TASK-002`, `MOV-TASK-003`

**Owned modules:** Campaign element/world/representation/snapshot contracts, creation event and
world factory, serializers, projector, local and context-authoritative validators, replay admission,
and all affected Campaign fixtures/tests

**Observable output:** creation seeds Cohesion/expenditure and one internal independent-element
representation per placed element; canonical snapshot/history round-trips the complete state

**Acceptance:** no validator or serializer is deferred to a later task; forged binding, ledger,
location, version, stage, and content mismatches reject

**Verification:** creation/world/snapshot/event/replay tests plus complete golden migration vectors

### `MOV-TASK-005` - Add apparent presence to observation

**Advances:** `MOV-REQ-005`, `MOV-REQ-011`; `MOV-AC-005`, `MOV-AC-006`

**Dependencies:** `MOV-TASK-004`

**Owned modules:** Observation contracts, projector, policy/version, serializer/reader, privacy and
dependency tests, and every affected observation fixture

**Observable output:** own mobility and operational ledger plus approved apparent opposing
representation location/ZOC facts, with internal bindings absent

**Acceptance:** own mobility is present and canonical; real element binding, opposing
CPA/Cohesion/Reserve/mobility/content, and raw event truth are absent by API shape, dependency
graph, and canonical bytes

**Verification:** projection/golden/privacy/differential-observation tests

### `MOV-TASK-006` - Freeze Movement action and submission contracts

**Advances:** `MOV-REQ-006`, `MOV-REQ-007`, `MOV-REQ-010`; `MOV-AC-006`, `MOV-AC-008`

**Dependencies:** `MOV-TASK-005`

**Owned modules:** Action candidate/submission/receipt contracts, canonical action codec, action-ID
derivation, internal pure observation-to-candidate vectors, strict validators, and Actions tests.
Public current-action membership and command mapping remain unchanged.

**Observable output:** canonical move and completion output types, submissions, IDs, codecs, and
pure derivation evidence; no public Movement member and no accepted move yet

**Acceptance:** the current public Movement action set remains empty/unsupported; pure derivation is
a function of observation; byte-identical observations produce byte-identical derived vectors;
malformed/forged contracts fail before commands

**Verification:** Actions namespace contract, golden, mutation, audience, pure-derivation, and
public-empty-set tests

### `MOV-TASK-007` - Implement non-contact Movement adjudication

**Advances:** `MOV-REQ-007`, `MOV-REQ-008`, `MOV-REQ-010`, `MOV-REQ-011`; `MOV-AC-007`,
`MOV-AC-008`, `MOV-AC-010`, `MOV-AC-011`

**Dependencies:** `MOV-TASK-006`

**Owned modules:** internal Movement command, event, event codec, resolver/adjudicator, internal
command execution dispatch, projector transition, snapshot/event validation, replay, and Campaign
tests. Public Legal Actions membership/submission wiring remains dormant until Task 008.

**Observable output:** the internal supported move vertical atomically changes location and the
exact ledger while remaining at Movement; the public current action set still advertises no
Movement action

**Acceptance:** every failure class emits zero events; event and successor independently validate;
multi-move replay is byte-identical; every resulting expenditure above base CPA, contact, and
enemy-ZOC behavior remain typed unsupported; public Movement membership remains absent

**Verification:** TDD red/green unit, command, event, projection, replay, stale/forged, table, and fog
tests

### `MOV-TASK-008` - Publish the complete Movement action vertical

**Advances:** `MOV-REQ-006`, `MOV-REQ-007`, `MOV-REQ-009`, `MOV-REQ-010`, `MOV-REQ-011`;
`MOV-AC-006` through `MOV-AC-011`

**Dependencies:** `MOV-TASK-007`

**Owned modules:** completion command/event/codec/execution/projection/validation; observation-only
public candidate generation; move and completion action membership/submission mapping; exact
sequence successor tests; fog-equivalence tests; and no-move/post-move replay tests

**Observable output:** the public current action set atomically gains executable supported moves
and the always-present completion action; one accepted completion reaches first-side Breakdown
Determination with no world/random mutation

**Acceptance:** every advertised move and completion is executable through the ordinary submission
path; completion is present at every supported Movement checkpoint, including when no move exists;
zero/one/many moves may precede completion; repeated/stale/wrong-side/forged cases reject; no
Breakdown action appears

**Verification:** focused Campaign and Actions completion/replay tests

### `MOV-TASK-009` - Adopt Movement in Exercise and Maneuver evidence

**Advances:** `MOV-REQ-012`; `MOV-AC-012`, `MOV-AC-013`

**Dependencies:** `MOV-TASK-008`

**Owned modules:** closed controller policy and manifest tokens, runner-local accepted-move history,
checked Movement Exercise/Maneuver fixtures, artifact semantic/readjudication validation, runner
tests, and retained simulator study

**Observable output:** six children cross act-first/act-last and Reserve none/one/all, move each
eligible element at most once on deterministic supported routes, and reach Breakdown

**Acceptance:** expected action/event/move/Reserve/ledger counts, reconstruction, re-adjudication,
strict readback, and aggregate fingerprint are frozen from two matching runs

**Verification:** Exercise Runner project tests, full solution tests, two CLI Maneuver runs, and
retained evidence artifact

### `MOV-TASK-010` - Synchronize and review the completed package

**Advances:** all requirements and acceptance criteria

**Dependencies:** `MOV-TASK-009`

**Owned output:** synchronized README, roadmap, technical design, naming rationale, spec/design
statuses, executed test evidence, traceability table, and independent-review ledger

**Acceptance:** no P0/P1 findings remain; every in-scope requirement maps to an implemented task and
executed evidence; every deferral remains explicit

**Verification:** Core/Runner/full-solution tests, warning-clean build, format check, `git diff
--check`, independent review, and post-fix rerun if required

## Traceability

| Requirement | Decisions | Tasks | Verification | Current status |
| --- | --- | --- | --- | --- |
| `MOV-REQ-001` exact CP | `MOV-DEC-004` | 001-002 | rational/golden tests | Task 001 complete; implementation pending |
| `MOV-REQ-002` rules data | 003, 007 | 001-002 | source vectors | Task 001 complete; implementation pending |
| `MOV-REQ-003` content mobility | 003 | 003 | content identity/validation | Approved; implementation pending |
| `MOV-REQ-004` operational state | 004-005 | 004, 007 | snapshot/replay | Approved; implementation pending |
| `MOV-REQ-005` representation/contact | 001-002, 006 | 001, 004-005 | privacy/differential tests | Task 001 complete; implementation pending |
| `MOV-REQ-006` candidates | 005, 007 | 006, 008 | action/fog tests | Approved; implementation pending |
| `MOV-REQ-007` submission/command | 005-007 | 006-008 | forged/stale tests | Approved; implementation pending |
| `MOV-REQ-008` move event | 004-007 | 007 | event/projector/replay | Approved; implementation pending |
| `MOV-REQ-009` completion | 005 | 008 | exact successor tests | Approved; implementation pending |
| `MOV-REQ-010` canonical contracts | 004, 006 | 002-008 | golden/strict reader tests | Approved; implementation pending |
| `MOV-REQ-011` replay/fog | 001, 006 | 004-008 | replay/privacy tests | Approved; implementation pending |
| `MOV-REQ-012` Exercise evidence | 008 | 009 | project/full tests + CLI runs | Approved; implementation pending |

## Review focus

The first review should challenge:

- whether the apparent-presence/ZOC fact is the minimum safe digital visibility ruling;
- whether rational CP is the correct long-lived contract versus a narrower fixed unit;
- whether operational ledger state belongs inside each element or a separate stage ledger;
- whether initial representation should be recorded in creation truth or deterministically derived;
- whether conservative destination stacking is sufficiently explicit; and
- whether stopping at Breakdown is both source-faithful and simulator-useful.

The owner approved these decisions on 2026-08-25. Review findings may refine the plan, but a
material architecture or scope change returns to the owner rather than being inferred during
implementation.
