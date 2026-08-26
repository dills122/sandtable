# Movement Foundation v1 Specification

**Status:** Active implementation baseline; `MOV-TASK-001` through `MOV-TASK-004` complete,
`MOV-TASK-005` ready

**Date:** 2026-08-25

**Roadmap capability:** `MOVE-001`

**Rules target:** `cna-1979.1`

**Predecessors:** [Reserve Designation v1](reserve-designation-v1.md),
[Campaign Observation v1](campaign-observation-v1.md), and
[Exercise Harness v1](exercise-harness-v1.md)

**Research:** [Movement Foundation Spike](../research/movement-foundation-spike.md)

**Technical design:** [Movement Foundation v1](../design/movement-foundation-v1.md)

## Objective

Allow the first-acting side to make replayable, side-safe, non-contact Land Movement decisions from
the implemented Movement checkpoint, charge exact Capability Point costs against authoritative
element state, enforce the supported terrain and stacking subset, and explicitly finish at the
existing Breakdown Determination boundary.

## Capability boundary

Movement Foundation v1 includes:

- initial authoritative map representations for independently placed synthetic combat elements;
- a minimum apparent-opposing-presence observation that makes legal-action filtering fog-safe;
- rules-owned mobility classification and normalized Movement tables for the synthetic lab;
- exact Capability Point amounts, per-Operation-Stage expenditure, and Cohesion state;
- repeatable adjacent, one-element, non-contact Movement;
- explicit Movement completion to Breakdown Determination;
- typed actions, submissions, commands, events, snapshots, observations, replay, and evidence; and
- one checked simulator matrix across existing Reserve `none`/`one`/`all` policies.

It excludes:

- enemy occupancy entry, enemy ZOC entry/exit, Contact, Engaged, Reaction, and break-off;
- moving a stack as one action or ending in an unsupported transient overstack;
- Breakdown adjudication, vehicle classes, Fuel, trucks, or motorization changes;
- combat, Reserve Release, segment repetition, second-side Movement, and later stages;
- attachments, dummy formations, Patrol, reconnaissance knowledge, or published content; and
- any model-backed or free-text interpretation inside authoritative execution.

## Normative decisions

The specification adopts the owner-approved research recommendations:

| Decision | Normative requirement |
| --- | --- |
| `MOV-DEC-001` | Map representation and apparent opposing presence are prerequisites to outward Movement legality. |
| `MOV-DEC-002` | The acting side may observe apparent opposing presence location and whether it exerts a ZOC; real bindings and unsupported force facts remain hidden. |
| `MOV-DEC-003` | Mobility is a rules-owned content classification; stacking value is derived from organization. |
| `MOV-DEC-004` | Capability Point amounts are exact reduced rational values in authority and canonical contracts. |
| `MOV-DEC-005` | v1 accepts only non-contact movement and completes to Breakdown Determination. |
| `MOV-DEC-006` | Authoritative representation bindings and event truth never become outward DTOs. |
| `MOV-DEC-007` | Unsupported categories reject before mutation and emit zero events. |
| `MOV-DEC-008` | The existing Reserve matrix supplies the checked Movement evidence paths. |

## Contract requirements

### `MOV-REQ-001` - Exact Capability Point amount

Authority must use an immutable, non-negative, reduced rational Capability Point amount with a
positive denominator. Equality, ordering, addition, subtraction where non-negative, canonical
serialization, and hashing must be culture-independent and exact.

Zero has one canonical representation. Equivalent inputs such as `2/4` and `1/2` must normalize to
the same value and bytes. Floating-point values are forbidden at the authoritative boundary.

### `MOV-REQ-002` - Rules-owned Movement data

The ruleset must own a closed mobility vocabulary and a versioned normalized table for:

- non-motorized and motorized classification;
- Clear and Desert entry;
- Road and corrected Track traversal;
- Ridge, up-slope, and down-slope crossing;
- supported stacking limits; and
- battalion stacking-point derivation.

Each normalized row must retain exact `RuleReference` provenance. Unsupported terrain, edge,
mobility, organization, or interaction must return a typed unsupported result rather than a
default cost.

### `MOV-REQ-003` - Content mobility fact

Each independently placed combat element admitted to Movement must carry one supported mobility
classification assigned by content and validated against the compatible ruleset. Base CPA remains
a content source fact. Content must not persist a derived current CP balance, Cohesion value,
stacking total, ZOC result, or Movement cost.

Changing authoritative mobility facts requires a Content Pack schema/format/hash version change.

### `MOV-REQ-004` - Authoritative element operational state

Every admitted element at a Movement-capable checkpoint must have:

- current location and Reserve status;
- current Cohesion;
- exact Capability Points expended in the current Operation Stage; and
- an unambiguous association with the snapshot's current Game Turn and Operation Stage ledger.

The synthetic setup seeds Cohesion `0` and expenditure `0` explicitly. These are fixture facts, not
defaults for future published scenarios. Cohesion may not exceed `+10`; expenditure is
non-negative. Cross-version validation must reject a Movement snapshot whose operational ledger is
missing, duplicated, belongs to another stage, or contradicts the admitted content/world.

### `MOV-REQ-005` - Map representation and apparent presence

Each independently placed synthetic element must have one stable authoritative map representation
whose real-element binding remains internal. Initial representation and location must reconstruct
from the creation history and exact content identity.

The acting-side observation exposes the acting side's own supported mobility ID together with its
own exact Cohesion and expenditure. It may expose only these approved apparent opposing facts:

- stable apparent representation identity;
- current apparent location; and
- whether that apparent presence exerts a ZOC under the supported rules subset.

For an opposing apparent presence it must not expose a real element ID, binding, hidden content
record, Reserve state, CPA, expenditure, Cohesion, mobility classification, or unsupported
stack/face fact. Own mobility, organization, CPA, location, Reserve status, Cohesion, and
expenditure are side-safe inputs needed to make Movement choices; this does not expose the complete
Content Pack.

### `MOV-REQ-006` - Movement candidate

At first-side Operation Stage 1 Movement, the active side receives one canonical candidate for each
supported adjacent non-contact move plus one completion candidate.

A move candidate identifies only:

- the acting side's own element ID;
- origin and adjacent destination IDs;
- an exact, side-safe CP cost breakdown; and
- the candidate's normal action identity/version fields.

The candidate generator must exclude an element or path when any of the following is true:

- wrong side, wrong phase, wrong actor, or stale/invalid authority;
- Reserve status is not `None`;
- Cohesion is `-26` or worse;
- origin/destination is missing or nonadjacent;
- enemy occupancy, enemy ZOC entry/exit, or another deferred contact rule applies;
- destination/traversal stacking would exceed the supported conservative limit;
- the move's resulting cumulative Operation-Stage expenditure would exceed base CPA; or
- terrain, edge, mobility, organization, or cost behavior is unsupported.

Candidate presence or absence must be explainable from the acting-side observation. Two authority
states with byte-identical acting-side observations must produce byte-identical acting-side action
sets.

### `MOV-REQ-007` - Movement submission and command

A Movement submission maps only to a typed internal command carrying contract version, expected
state version, expected position ID, acting side, own element ID, origin, destination, and the
candidate identity needed by the existing action boundary.

Submission must revalidate every invariant against current authority. A stale, forged,
out-of-audience, altered-origin, altered-destination, hidden-binding, unsupported, or no-longer-legal
submission rejects with zero events and no state change.

### `MOV-REQ-008` - Accepted Movement event and projection

One accepted move emits exactly one semantic Movement event and increments state version once. The
event must contain enough internal truth to validate and replay:

- campaign/event contract identity and prior state version;
- sequence position and acting side;
- real element/representation binding identity needed by authority;
- origin and destination;
- normalized terrain/edge/mobility cost components and their rule references;
- exact total cost;
- before/after cumulative Operation-Stage expenditure;
- equal before/after Cohesion values; and
- the unchanged Movement sequence position.

Projection must update location, representation location, and expenditure while preserving
Cohesion atomically.
No event may produce a half-applied world. V1 excludes every candidate and rejects every submission
whose resulting cumulative expenditure would exceed base CPA, whether the excess is integral or
fractional. Accepted v1 Movement therefore preserves Cohesion and records no Disorganization
conversion. Over-CPA expenditure requires a later approved contract version.

### `MOV-REQ-009` - Movement completion

The active side always receives one explicit completion candidate at the supported first-side
Movement checkpoint, including when all elements are in Reserve or no supported move exists.

An accepted completion emits exactly one semantic completion event, increments state version once,
preserves world and random state, and advances exactly to:

`land.position.operation-1.first-player.movement-and-combat.breakdown-determination`.

The successor exposes no legal Breakdown action in this capability. A repeated, stale, forged, or
wrong-side completion rejects with zero events.

### `MOV-REQ-010` - Canonical serialization and validation

Command, event, snapshot, observation, legal-action, content, and Exercise artifacts changed by
this capability must use explicit contract versions and canonical property order. Strict readers
must reject unknown, missing, duplicate, reordered-where-semantic, noncanonical-rational, invalid
enum/ID, and cross-field-inconsistent values as applicable.

Context-free parsing may enforce structural/local invariants. Context-authoritative admission must
also validate the ruleset, setup, content, representation binding, stage ledger, exact sequence,
and event-history transition.

### `MOV-REQ-011` - Replay and fog

Replaying creation through any accepted Movement sequence and completion must recreate canonical
snapshot bytes exactly. Re-adjudicating retained side-safe submissions must reproduce the same
events and receipts.

Negative dependency and serialization tests must prove that opposing real element IDs/bindings,
hidden operational state, complete Content Pack objects, random state, and authoritative events do
not enter outward observations or candidates.

### `MOV-REQ-012` - Exercise evidence

The checked Movement Maneuver must cross act-first/act-last with Reserve none/one/all using ordinary
legal-action queries and submissions. Its bounded controller moves each eligible non-Reserve
element at most once on a deterministic supported route and then completes Movement.

For each child it must retain:

- exact accepted action/event count;
- exact moved element/location and CP/Cohesion ledger facts;
- zero/one/two Reserve-I designations as selected;
- Breakdown Determination terminal evidence;
- reconstruction and re-adjudication equality; and
- strict artifact/report readback.

The expected Movement action counts and aggregate fingerprint are frozen only after implementation
and review; this draft does not invent them.

## Acceptance criteria

| ID | Acceptance behavior |
| --- | --- |
| `MOV-AC-001` | Exact rational CP values normalize, compare, serialize, and hash deterministically; malformed/noncanonical values reject. |
| `MOV-AC-002` | Golden rules vectors prove Clear/Desert/Road/corrected-Track/Ridge/Slope costs and battalion/terrain stacking inputs with source references. |
| `MOV-AC-003` | Content mobility changes identity; unknown or missing mobility fails admission before campaign creation/Movement. |
| `MOV-AC-004` | Creation and every Movement checkpoint contain valid stage-associated expenditure and Cohesion state. |
| `MOV-AC-005` | Own observation includes the supported mobility ID and exact ledger inputs; apparent opposing presence exposes only approved location/ZOC facts, with real bindings and hidden force state absent by bytes and dependencies. |
| `MOV-AC-006` | Byte-identical acting-side observations generate byte-identical action sets even when hidden authority differs. |
| `MOV-AC-007` | Supported adjacent non-contact moves charge exact CP, update location atomically, emit one event, and remain at Movement. |
| `MOV-AC-008` | Reserve, depleted Cohesion, over-CPA expenditure, nonadjacency, stacking, contact/ZOC, unsupported table, stale, forged, and wrong-side cases reject with zero events. |
| `MOV-AC-009` | Completion with zero or more accepted moves advances exactly once to Breakdown Determination and preserves the world. |
| `MOV-AC-010` | Snapshot/event/observation/action canonical bytes and strict readers cover every new versioned field and invariant. |
| `MOV-AC-011` | Replay and re-adjudication produce exact canonical equality for no-move and multi-move histories. |
| `MOV-AC-012` | The checked six-policy Maneuver reaches Breakdown in every child with expected moved/reserved/ledger facts and strict readback. |
| `MOV-AC-013` | Full Core, Exercise Runner, solution, format, and diff gates pass warning-clean. |

## Owner approval

On 2026-08-25, the project owner approved the apparent-presence/ZOC visibility ruling, exact
rational Capability Point amounts, the non-contact v1 boundary, the Breakdown terminal, the
over-CPA/Disorganization deferral, and the linked task graph. The completed source/ruling lock and
Rules foundation make `MOV-TASK-002` complete with exact rational amounts, a closed mobility vocabulary, normalized
source-backed Movement tables, strict canonical artifact readback, and ruleset v6 identity.
`MOV-TASK-003` is complete with required per-element mobility, Content schema 3 / canonical format
v2, strict legacy and unknown-ID rejection, and migrated deterministic identities. `MOV-TASK-004`
is complete with world v3, snapshot v8, creation event v7, exact initial Cohesion/expenditure, and
opaque internal one-to-one representation bindings. `MOV-TASK-005` is the next delivery gate.
