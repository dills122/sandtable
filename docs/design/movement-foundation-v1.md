# Movement Foundation v1 Technical Design

**Status:** Active implementation plan; `MOV-TASK-006` implementation, gates, and independent review
are complete in PR #69 and await merge; `MOV-TASK-007` follows merge

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

Task 007 will add an internal `MoveElement` command repeating expected state version, expected
position, acting side, element, origin, and destination. The Umpire will recalculate cost; it will
never trust a submitted cost.

The future accepted `ElementMoved` event contains the validated real binding and exact ledger
delta. Cost components use closed semantic kinds such as destination terrain, route override, and
crossed hexside. The event stores `RuleReference` values required to explain the adjudication; it does not
store copied prose. V1 accepts only a resulting cumulative expenditure at or below base CPA, so its
before/after Cohesion values are equal and it carries no Disorganization conversion.

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
MOV-TASK-006 dormant action/submission contracts [reviewed in PR #69; awaiting merge]
                  |
                  v
MOV-TASK-007 move command/event/adjudication [next]
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

Tasks 001 through 005 are complete. Task 006 implementation, gates, and independent review are
complete in PR #69 and await merge. Tasks 007 through 010 are intentionally serial because
each freezes a versioned contract consumed by the next layer. In particular, Content admission in Task 003
consumes the closed mobility vocabulary and ruleset identity completed by Task 002. Tasks 006 and
007 keep all public Movement membership dormant; Task 008 atomically exposes executable move and
completion actions so no intermediate checkpoint can strand a campaign at Movement.
`BREAKDOWN-001` is approved. Task 004B implements the required predecessor of Task 005 while
preserving the no-adjudication boundary. Its repository gate and two fresh-context review
instances are complete. Tasks 005 and 006 are implemented and verified. Task 006 remains a dormant
contract slice: Task 007 now owns internal adjudication, and Task 008 still owns atomic public
membership.

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

**Status:** Implementation, gates, and independent review complete in PR #69 (2026-08-29); awaiting merge

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

**Status:** Next; ready to begin

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

**Verification:** TDD red/green unit, command, event, projection, replay, stale/forged, table, and fog
tests

### `MOV-TASK-008` - Publish the complete Movement action vertical

**Status:** Blocked by `MOV-TASK-007`

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

**Status:** Blocked by `MOV-TASK-008`

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

**Status:** Blocked by `MOV-TASK-009`

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
| `MOV-REQ-001` exact CP | `MOV-DEC-004` | 001-002 | rational/golden tests | Implemented in Task 002 |
| `MOV-REQ-002` rules data | 003, 007 | 001-002 | source vectors | Implemented in Task 002 |
| `MOV-REQ-003` content mobility | 003 | 003 | content identity/validation | Implemented in Task 003 |
| `MOV-REQ-004` operational state | 004-005 | 004, 007 | snapshot/replay | Authoritative state implemented in Task 004; contract-5 outward projection active in Task 005 |
| `MOV-REQ-005` representation/contact | 001-002, 006 | 001, 004-005 | privacy/differential/readback tests | Internal representation implemented in Task 004; three-field apparent projection and exact own risk active in Task 005 |
| `MOV-REQ-006` candidates | 005, 007 | 006, 008 | action/fog tests | Dormant typed candidates, exact costs, identities, and pure vectors implemented in Task 006; public membership remains Task 008 |
| `MOV-REQ-007` submission/command | 005-007 | 006-008 | forged/stale tests | Generic submission strict readback implemented in Task 006; internal command/adjudication and public mapping remain Tasks 007-008 |
| `MOV-REQ-008` move event | 004-007 | 007 | event/projector/replay | Approved; implementation pending |
| `MOV-REQ-009` completion | 005 | 008 | exact successor tests | Approved; implementation pending |
| `MOV-REQ-010` canonical contracts | 004, 006 | 002-008 | golden/strict reader tests | Rules/world/history implemented through 004B; observation-v5 and action-set/submission/receipt strict readers active through Task 006 |
| `MOV-REQ-011` replay/fog | 001, 006 | 004-008 | replay/privacy tests | Conditional same-apparent privacy complete in Task 005; Movement replay pending |
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
