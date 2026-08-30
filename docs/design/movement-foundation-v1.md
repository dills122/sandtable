# Movement Foundation v1 Technical Design

**Status:** Active implementation plan; `MOV-TASK-009` executable Movement evidence is implemented
and verified locally, with PR merge and integration evidence provisional; `MOV-TASK-010` remains
blocked until that merge

**Date:** 2026-08-25

**Capability:** `MOVE-001`

**Specification:** [Movement Foundation v1](../specs/movement-foundation-v1.md)

**Research:** [Movement Foundation Spike](../research/movement-foundation-spike.md) and
[Movement Simulator Trajectories](../research/simulator-movement-trajectories.md)

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

`src/Cna.Core/Observations` contract 5/policy
`sandtable.observation.movement-side-safe.v1` projects exact own mobility, stage ledger, Cohesion,
and nullable vehicle Breakdown risk plus the approved minimum apparent opposing presence.
`src/Cna.Core/Actions`
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

The outward `ObservedApparentPresence` type is separate and contains only opaque representation ID,
current apparent location, and `exertsZoc`. It has no binding collection. Current admitted
synthetic rows always use `false`; positive qualification remains `ZOR-TASK-002`.

`ObservedOwnElement` additionally copies `mobilityId`, ledger Game Turn/Operation Stage, exact
`capabilityPointsExpended`, `cohesionLevel`, and nullable `vehicleBreakdownRisk`. A non-null
`ObservedOwnVehicleBreakdownRisk` carries cohort ID, vehicle type ID, profile ID, exact cumulative
and Sandstorm-attributed BP, nullable highest effective checked-band ID, and working/broken counts.
It contains no Content origin, rules provenance, or authoritative Campaign object.

Contract-5 canonical JSON ends with `apparentOpposingPresences`, ordered by representation ID.
`CampaignObservationSerializer.DeserializeCanonical` is a strict, non-authoritative reader: it
accepts only exact v5 field order/shape and canonical exact amounts, then proves byte equality by
reserialization. Legacy v4, missing/extra/duplicate/reordered/injected fields and malformed values
reject.

### Movement action and event

One `MoveElementAction` identifies own element, origin, destination, and a side-safe cost breakdown.
One `CompleteMovementSegmentAction` carries no optional target.

The dormant Task 006 cost value is deliberately explanatory and closed:

- destination terrain ID and exact destination-terrain cost;
- one nullable route adjustment containing route ID, closed `override` or `scale-underlying`
  behavior, and its exact amount;
- crossed-hexside additions in canonical order, each containing feature ID, closed `either`, `up`,
  or `down` direction, and exact added cost; and
- one exact total that must be coherent with the terrain, route behavior, and additions.

Crossed-hexside feature IDs are unique independent of direction. A repeated feature ID, including
contradictory `up` and `down` rows, is unsupported and rejects instead of being charged twice.

Cost coherence is exact and unambiguous: without a route, adjusted terrain equals destination
terrain cost; `override` replaces destination-terrain cost with the route amount;
`scale-underlying` multiplies destination-terrain cost by the route amount. The total then equals
adjusted terrain plus every crossed-hexside addition. Checked overflow rejects the candidate.

Candidate contract version 1 is unchanged. The action ID remains SHA-256 over the complete typed
side-safe canonical semantic preimage excluding the ID, so any element, path, cost-component, order,
or total mutation changes or invalidates the identity. The pure Task 006 deriver consumes only
Campaign Observation contract 5 and emits deterministic dormant vectors; it is not called by the
public legal-action membership switch yet.

Task 006 has no topology-aware ZOC entry/exit adjudicator. It therefore uses a documented
conservative boundary: apparent opposing occupancy at either edge endpoint suppresses that move,
and any positive apparent-ZOC row anywhere suppresses all move candidates. `ZOR-TASK-002` must
replace that global fail-closed behavior with the source-faithful local rule before publication.

Legal-action-set contract 2 and generic submission/acceptance-receipt contracts 1 retain their
existing shapes and versions. Their strict canonical readers require the exact closed property
order and byte-identical canonical reserialization. Read values remain non-authoritative: an action
ID is not authorization, a parsed submission is not current membership, and receipt readback does
not prove or fabricate an accepted move.

Task 007 adds an internal `MoveElement` command repeating expected state version, expected position,
acting side, dormant candidate identity, element, origin, and destination. It carries no cost. The
Umpire resolves the admitted Content element and topology, recalculates every cost and provenance
value from `Cna1979Movement`, and rejects a stale or forged candidate identity after recalculation.

The accepted contract-1 `ElementMoved` event contains the prior/committed state versions, validated
real representation binding, unchanged Movement position, and exact ledger delta. Internal cost
values retain mobility, destination terrain, optional route, crossed-hexside, and per-component
`RuleReference` provenance without copied prose. V1 accepts only a resulting cumulative expenditure
at or below base CPA, so before/after Cohesion values are equal and no Disorganization conversion
exists. The projector recreates and byte-compares the event before atomically replacing the element,
representation, and expenditure state.

`MovementSegmentCompleted` preserves the world and records the exact Movement and Breakdown
positions.

## Validation order

The executable Movement submission vertical will validate in this order and emit nothing until all
checks pass. Task 006 implements only context-free contract/identity validation and dormant
observation derivation; it maps no command.

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

### Approved Breakdown continuity boundary

The merged Task 004 world records exact stage-associated CP expenditure and Cohesion but no vehicle
composition, Breakdown Point accumulation, Sandstorm attribution, or prior checked-band state. On
2026-08-29 the owner approved minimum BP continuity, sequential d6 coordinates, and Table 21.38's
Sandstorm-attributed-BP threshold. Task 004B therefore versions Rules, Content, and World before
Task 005 fixes its own-observation shape. This still does not authorize Breakdown adjudication.

The source-locked Truck profile shifts two columns left. Authority retains exact total and
Sandstorm-attributed BP, where the subtotal is between zero and total inclusive. Future adjudication
may apply the Sandstorm shift when twice the subtotal is at least the total. The full percentage
outcome matrix, roll, losses, and RNG remain deferred.

## Task graph

```text
MOV-TASK-001 source/ruling lock [complete]
                  |
                  v
MOV-TASK-002 amount/table [complete]
                  |
                  v
MOV-TASK-003 content mobility [complete]
                  |
                  v
MOV-TASK-004 world + representation contracts [complete]
                  |
                  v
BREAKDOWN-001 decisions [approved]
                  |
                  v
MOV-TASK-004B BP continuity [complete]
                  |
                  v
MOV-TASK-005 observation/apparent presence [complete]
                  |
                  v
MOV-TASK-006 dormant action/submission contracts [complete; merged in PR #69]
                  |
                  v
MOV-TASK-007 move command/event/adjudication [complete]
                  |
                  v
MOV-TASK-008 completion to Breakdown [complete]
                  |
                  v
MOV-TASK-009 end-to-end + Exercise/Maneuver evidence [implemented; merge provisional]
                  |
                  v
MOV-TASK-010 synchronization + independent review
```

Tasks 001 through 009 are implemented. Tasks 009 through 010 remain serial because each
freezes a versioned contract consumed by the next layer. In particular, Content admission in Task 003
consumes the closed mobility vocabulary and ruleset identity completed by Task 002. Tasks 006 and
007 keep all public Movement membership dormant; Task 008 now atomically exposes executable move
and completion actions so no intermediate checkpoint can strand a campaign at Movement.
`BREAKDOWN-001` is approved. Task 004B implements the required predecessor of Task 005 while
preserving the no-adjudication boundary. Its repository gate and two fresh-context review
instances are complete. Tasks 005 through 008 are implemented and verified. Task 007 completes
internal adjudication while preserving Task 006's dormant public boundary; Task 008 atomically
publishes move and completion membership and admits the exact Breakdown successor. Task 009 adopts
that public vertical through Runner-local bounded selection, strict evidence, and retained simulator
trajectories. Its local verification is complete, but Task 010 remains blocked until the Task 009 PR
merges and integration evidence is retained.

The Task 001-004 foundation merged in PR #29 after `just check` passed with a warning-clean build,
format verification, and 746/746 solution tests. The roadmap groups the remaining work into a
Task 005 fog-contract checkpoint, a Task 006-008 atomic action vertical, and a Task 009-010 evidence
and review closeout; those checkpoints do not relax the serial contract dependencies below.

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

**Status:** Complete (2026-08-25)

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

**Status:** Complete (2026-08-25)

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

**Status:** Complete (2026-08-25)

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

### `MOV-TASK-004B` - Version Breakdown continuity

**Status:** Complete (approved and verified 2026-08-29)

**Advances:** `MOV-REQ-013`, `MOV-REQ-014`, `MOV-REQ-015`; `MOV-AC-014` through `MOV-AC-017`

**Dependencies:** `MOV-TASK-004`; approved `BRK-DEC-001` through `BRK-DEC-003` (satisfied)

**Owned modules:** exact BP value and normalized continuity artifact; Content vehicle cohort and
synthetic catalog; Campaign operational state, creation, snapshot, validation, and replay; all
coordinated identity fixtures

**Observable output:** one source-backed synthetic Truck Point cohort per admitted motorized
element and initial stage-keyed exact total/Sandstorm BP, checked-band, and working/broken state

**Acceptance:** canonical Rules/Content/World identities change exactly once; strict readers reject
legacy, malformed, unknown, and cross-context mismatches; creation replay is byte-identical; no
Breakdown action, outcome matrix, roll, result, loss, BP mutation, or RNG path exists

**Verification:** focused Rules/Content/Campaign TDD suites, canonical mutation/golden tests,
coordinated identity search, full repository gate, and fresh independent review

### `MOV-TASK-005` - Add apparent presence to observation

**Status:** Complete; repository-verified and independently reviewed

**Advances:** `MOV-REQ-005`, `MOV-REQ-011`; `MOV-AC-005`, `MOV-AC-006`

**Dependencies:** `MOV-TASK-004B`

**Owned modules:** Observation contracts, projector, policy/version, serializer/reader, privacy and
dependency tests, and every affected observation fixture

**Observable output:** observation contract 5 with policy
`sandtable.observation.movement-side-safe.v1`; exact own mobility, ledger, Cohesion, and nullable
vehicle-risk facts; canonical `apparentOpposingPresences` containing only opaque representation ID,
current location, and current false ZOC; and strict non-authoritative canonical readback

**Acceptance:** own mobility/ledger/Cohesion and cohort/type/profile/BP/check/count risk are exact
and canonical; real element binding, opposing CPA/Cohesion/Reserve/mobility/content/cohort/BAR/BP/
check history, provenance, and raw event truth are absent by API shape, dependency graph, and bytes;
same-apparent hidden authority is byte-identical, approved apparent changes yield only an apparent
delta, false-only ZOC is enforced, and strict readback rejects legacy/noncanonical bytes

**Verification:** projection/golden/privacy/differential-observation tests

### `MOV-TASK-006` - Freeze Movement action and submission contracts

**Status:** Complete and merged in PR #69 (2026-08-29)

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

**Status:** Complete (2026-08-29)

**Advances:** `MOV-REQ-007`, `MOV-REQ-008`, `MOV-REQ-010`, `MOV-REQ-011`; `MOV-AC-007`,
`MOV-AC-008`, `MOV-AC-010`, `MOV-AC-011`

**Dependencies:** `MOV-TASK-006` and the already selected/implemented `BREAKDOWN-001` boundary

**Owned modules:** internal Movement command, event, event codec, resolver/adjudicator, internal
command execution dispatch, projector transition, snapshot/event validation, replay, and Campaign
tests. Public Legal Actions membership/submission wiring remains dormant until Task 008.

**Observable output:** the internal supported move vertical atomically changes location and the
exact ledger while remaining at Movement; the public current action set still advertises no
Movement action

**Acceptance:** every failure class emits zero events; event and successor independently validate;
multi-move replay is byte-identical; every resulting expenditure above base CPA, contact, and
enemy-ZOC behavior remain typed unsupported; public Movement membership remains absent

**Verification:** TDD red/green command, event, canonical codec, authoritative table/provenance,
projection, zero/one/many replay, stale/forged/over-CPA, moved-checkpoint, and public-dormancy tests;
warning-clean Core build, repository format/diff gates, and full `just check`

**Delivery slices:**

1. `MOV-007A` freezes the internal command/event contracts and their immutable exact-value tests.
2. After 007A, `MOV-007B` authoritative calculation/event creation and `MOV-007C` phase-specific
   world/snapshot admission may proceed in parallel. The validator slice must admit coherent moved
   first-side non-Reserve state without weakening strict pre-Movement checkpoints.
3. After 007B/007C converge, `MOV-007D` strict event codec work and `MOV-007E` atomic
   projection/replay may proceed in parallel under the frozen event contract.
4. `MOV-007F` integrates Campaign engine dispatch and runs the dormancy checkpoint: accepted
   internal moves emit exactly one event while public Movement action sets remain empty.

`CampaignEngine`, `CampaignEventSerializer`, `CampaignProjector`, `CampaignSnapshotValidator`, and
`CampaignWorldValidator` are single-owner convergence files. Parallel slices must not edit the same
one concurrently.

### `MOV-TASK-008` - Publish the complete Movement action vertical

**Status:** Complete (2026-08-29)

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

**Implemented evidence:** immutable completion command/event/factory and exact canonical-codec
tests; zero/one/four-move completion and strict replay; observation-derived public membership for
zero/one/two Reserve selections; exact command mapping; ordinary move-then-completion submission;
stale, repeated, wrong-audience, forged, malformed, and no-longer-current rejection; byte-identical
public action sets for apparent-equivalent authorities; no early or successor Breakdown action;
and the complete Core suite.

**Delivery slices:** freeze completion command/event/factory first; then complete codec and
projection/engine work under separate ownership; only after both converge publish move and
completion membership together; finish with the public fog/replay vertical. Task 008 remains one
serial integration gate even where its codec and projector work can be prepared concurrently.

### `MOV-TASK-009` - Adopt Movement in Exercise and Maneuver evidence

**Status:** Implemented and verified locally, including two matching clean CLI executions; PR merge
and integration evidence remain provisional

**Advances:** `MOV-REQ-012`; `MOV-AC-012`, `MOV-AC-013`

**Dependencies:** `MOV-TASK-008`

**Owned modules:** closed controller policy and manifest tokens, runner-local accepted-move history,
checked Movement Exercise/Maneuver fixtures, artifact semantic/readjudication validation, runner
tests, and retained simulator study

**Observable output:** six children cross act-first/act-last and Reserve none/one/all, move each
eligible element at most once on deterministic supported routes, and reach Breakdown

**Frozen compatibility:** Exercise manifest, `sandtable.maneuver-manifest.v2`, and
`sandtable.exercise-controller-configuration.v2` remain unchanged. Six additive controller tokens
append `-move-each-once-then-complete` to the existing act-first/act-last and Reserve
none/one/all dimensions. The Runner-local controller candidate advances to v2: Reserve designation
retains its existing own `elementId`, while a move candidate requires own `elementId`,
`originLocationId`, and `destinationLocationId`. Existing v1 candidates reject; existing controller
tokens retain their bytes and behavior.

**Implemented policy:** the executor records an element ID only after an accepted `move-element`
action. At Movement the controller filters the current observation-derived public candidate set by
that history, orders supported moves deterministically, submits one current action through the
ordinary path, and completes with the exact advertised completion after every eligible supported
element has moved once. Core continues to permit repeat movement and receives no controller state.
The frozen ordinal order is `elementId`, `destinationLocationId`, `originLocationId`, then
`actionId`; changing that order requires a new controller token/configuration identity.

**Acceptance:** every child accepts exactly 13 actions/events, records 94 passed checks, and reaches
exact first-side Breakdown Determination. For each initiative policy, Reserve `none`/`one`/`all`
retains 0/1/2 designations and 2/1/0 moves. The `none` path moves A to the center at CP cost 8 and B
on its supported route at cost 1; `one` moves B at cost 1; `all` moves neither. Aggregate evidence
therefore contains 78 actions/events, six Reserve designations, six Reserve completions, six moves,
six Movement completions, and exact final CP expenditure 20. Reconstruction and fresh-session
re-adjudication must reproduce every child, and strict readers must reject malformed, inconsistent,
noncanonical, or tampered Movement evidence.

**Implemented evidence:** checked fixture
`scenarios/maneuvers/rules-lab.movement.serial.v2.json`; strict Movement event and ledger semantic
validation; child reconstruction and re-adjudication; 48 in-process trajectories across seeds 0, 1,
`ulong.MaxValue / 2`, and `ulong.MaxValue`, six controllers, and two repeats; and local aggregate
fingerprint `sha256:c1c20270dcd3402886931c28851bea7f23cd1e0778b45f94c43d85ed01d41c4b`,
reconfirmed by two clean CLI executions into separate artifact roots. The fingerprint does not bind
detailed child event/ledger bytes, so strict child validation is not optional.

**Verification:** Exercise Runner project tests, full solution tests, two CLI Maneuver runs, and
retained evidence artifact

**Delivery slices:** first freeze the Runner manifest/controller version decision. Controller
selection and executor-local accepted-move history then proceed under that contract while strict
Movement evidence-reader support may proceed independently. Converge on the six-child checked
fixture, strict semantic evidence, and two matching fingerprints. Merge optional Harness
`EXR-TASK-015` paired comparison before this task because both touch Runner execution, fixtures, and
validation.

### `MOV-TASK-010` - Synchronize and review the completed package

**Status:** Blocked until the `MOV-TASK-009` PR merges and integration checks complete

**Advances:** all requirements and acceptance criteria

**Dependencies:** `MOV-TASK-009`

**Owned output:** synchronized README, roadmap, technical design, naming rationale, spec/design
statuses, executed test evidence, traceability table, and independent-review ledger

**Acceptance:** no P0/P1 findings remain; every in-scope requirement maps to an implemented task and
executed evidence; every deferral remains explicit

**Verification:** Core/Runner/full-solution tests, warning-clean build, format check, `git diff
--check`, independent review, and post-fix rerun if required

**Delivery slices:** after Task 009, the Movement capability/traceability docs, cross-domain
developer/user-facing docs, and a read-only five-axis review may proceed in parallel. Reconcile all
three before the full gate and brand-new independent review.

## Traceability

| Requirement | Decisions | Tasks | Verification | Current status |
| --- | --- | --- | --- | --- |
| `MOV-REQ-001` exact CP | `MOV-DEC-004` | 001-002 | rational/golden tests | Implemented in Task 002 |
| `MOV-REQ-002` rules data | 003, 007 | 001-002 | source vectors | Implemented in Task 002 |
| `MOV-REQ-003` content mobility | 003 | 003 | content identity/validation | Implemented in Task 003 |
| `MOV-REQ-004` operational state | 004-005 | 004, 007 | snapshot/replay | Initial state and outward projection implemented through Task 005; phase-specific moved-state admission and replay implemented in Task 007 |
| `MOV-REQ-005` representation/contact | 001-002, 006 | 001, 004-005 | privacy/differential/readback tests | Internal representation implemented in Task 004; three-field apparent projection and exact own risk active in Task 005 |
| `MOV-REQ-006` candidates | 005, 007 | 006, 008 | action/fog tests | Typed candidates, exact costs, identities, pure observation derivation, and atomic public membership implemented through Task 008 |
| `MOV-REQ-007` submission/command | 005-007 | 006-008 | forged/stale tests | Generic submission readback, exact public membership mapping, and internal command/adjudication implemented through Task 008 |
| `MOV-REQ-008` move event | 004-007 | 007 | event/projector/replay | Canonical event, authoritative creation, atomic projection, and repeatable replay implemented in Task 007 |
| `MOV-REQ-009` completion | 005 | 008 | exact successor tests | Completion command/event/codec/execution/projection implemented in Task 008 with exact Breakdown successor and no public Breakdown action |
| `MOV-REQ-010` canonical contracts | 004, 006 | 002-008 | golden/strict reader tests | Rules/world/history implemented through 004B; outward strict readers active through Task 006; `ElementMoved` and `MovementSegmentCompleted` strict codecs plus moved/completed snapshots active through Task 008 |
| `MOV-REQ-011` replay/fog | 001, 006 | 004-008 | replay/privacy tests | Same-apparent public-action equivalence, zero/one/many Movement-plus-completion replay, and exact current-membership submission revalidation complete through Task 008 |
| `MOV-REQ-012` Exercise evidence | 008 | 009 | checked six-child fixture, strict semantic bundle/report readback, reconstruction/re-adjudication, 48-trajectory retained study, project/full tests, and two clean CLI runs | Implemented and locally verified, including matching clean-run fingerprints; PR merge and integration remain provisional |

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
