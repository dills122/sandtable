# Movement Foundation v1 Specification

**Status:** Active implementation baseline; `MOV-TASK-006` implementation and gates are complete,
with independent review and PR delivery pending; `MOV-TASK-007` follows merge

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
- Breakdown adjudication, Fuel, motorization changes, and vehicle classes beyond the one supported
  synthetic Truck Point cohort;
- combat, Reserve Release, segment repetition, second-side Movement, and later stages;
- attachments, dummy formations, Patrol, reconnaissance knowledge, or published content; and
- any model-backed or free-text interpretation inside authoritative execution.

### Current Task 006 delivery boundary

Task 006 freezes dormant contract infrastructure only. Candidate contract 1 now has typed move and
completion outputs, deterministic IDs over complete canonical side-safe semantics, and a pure
observation-to-candidate derivation seam. Legal-action-set contract 2 and generic submission and
receipt contracts 1 gain strict canonical readback without changing their versions. Readback is
non-authoritative: it neither establishes current membership nor creates a command, event, accepted
move, or acceptance receipt. Public Movement action sets remain empty/unsupported until Task 008;
Task 007 is the next internal command/event/adjudication slice.

### Breakdown continuity gate

Movement Foundation v1 remains replay-complete through arrival at the Breakdown Determination
boundary and does not adjudicate that mechanic. On 2026-08-29, the owner approved
`BREAKDOWN-001`: Task 004B adds exact BP rules identity, one supported synthetic Truck Point cohort
for each admitted motorized element, and stage-keyed replay state before Task 005 freezes outward
contracts. No Breakdown action, roll, loss, broken-location consequence, or RNG mutation is
authorized.

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
| `BRK-DEC-001` | Two independent d6 are read sequentially as the `11`-`66` Breakdown table coordinate; they are not arithmetically summed. |
| `BRK-DEC-002` | Minimum Breakdown continuity is versioned before outward Movement contracts; early Movement histories are not deliberately terminal. |

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

The acting-side contract-5 observation uses policy
`sandtable.observation.movement-side-safe.v1`. It exposes the acting side's own supported mobility
ID, ledger Game Turn/Operation Stage, exact capability points expended, Cohesion, and nullable exact
vehicle Breakdown risk. That risk contains cohort/type/profile identity, cumulative and
Sandstorm-attributed BP, nullable highest effective checked band, and working/broken counts without
provenance. It may expose only these approved apparent opposing facts:

- stable apparent representation identity;
- current apparent location; and
- whether that apparent presence exerts a ZOC under the supported rules subset.

Current admitted synthetic rows always use `exertsZoc = false`; positive qualification remains
gated on `ZOR-TASK-002` source-faithful content and rules. Until that topology-aware rule is
implemented, the dormant Task 006 generator fails closed globally: any positive apparent-ZOC row
suppresses every move candidate, even when the row is remote from the tested edge. This conservative
interim policy is deterministic from the observation and is not the final ZOC entry/exit rule.

For an opposing apparent presence it must not expose a real element ID, binding, hidden content
record, Reserve state, CPA, expenditure, Cohesion, mobility classification, or unsupported
stack/face fact. Own mobility, organization, CPA, location, Reserve status, Cohesion, and
expenditure plus the approved own vehicle-risk facts are side-safe inputs needed to make Movement
choices; this does not expose the complete Content Pack.

The canonical reader accepts only exact contract-5 bytes and requires byte-identical canonical
reserialization. Legacy, missing, extra, duplicate, reordered, injected, malformed, and
noncanonical exact-amount forms reject. Readback yields derived observation data only and never
admits Campaign authority.

### `MOV-REQ-006` - Movement candidate

At first-side Operation Stage 1 Movement, the active side receives one canonical candidate for each
supported adjacent non-contact move plus one completion candidate.

A move candidate identifies only:

- the acting side's own element ID;
- origin and adjacent destination IDs;
- an exact, side-safe CP cost breakdown; and
- the candidate's normal action identity/version fields.

The cost breakdown contains exactly the destination terrain ID and exact terrain cost; one nullable
route adjustment with route ID, closed `override` or `scale-underlying` behavior, and its exact
amount; an ordered collection of crossed-hexside additions containing feature ID, closed `either`,
`up`, or `down` direction, and exact added cost; and one exact total coherent with those components.
Crossed-hexside feature IDs are unique regardless of direction; contradictory repeated direction
rows for one feature reject rather than charging twice.
It contains no `RuleReference`, source locator, authority object, or hidden opposing fact.

The candidate generator must exclude an element or path when any of the following is true:

- wrong side, wrong phase, wrong actor, or stale/invalid authority;
- Reserve status is not `None`;
- Cohesion is `-26` or worse;
- origin/destination is missing or nonadjacent;
- apparent enemy occupancy exists at the origin or destination, the interim global positive-ZOC
  fail-closed rule applies, or another deferred contact rule applies;
- destination/traversal stacking would exceed the supported conservative limit;
- the move's resulting cumulative Operation-Stage expenditure would exceed base CPA; or
- terrain, edge, mobility, organization, or cost behavior is unsupported.

Candidate presence or absence must be explainable from the acting-side observation. Two authority
states with byte-identical acting-side observations must produce byte-identical acting-side action
sets.

Task 006 exposes that rule as a dormant pure vector and deliberately does not add its results to the
current public action set. Task 008 owns that publication after internal move and completion paths
are executable.

### `MOV-REQ-007` - Movement submission and command

A Movement submission maps only to a typed internal command carrying contract version, expected
state version, expected position ID, acting side, own element ID, origin, destination, and the
candidate identity needed by the existing action boundary.

The outward submission remains the payload-free generic contract-1 envelope: candidate identity
binds element, origin, destination, and cost rather than duplicating caller-editable fields.
Task 006 strictly reads that envelope but does not map a Movement candidate to a command. The
internal typed command and its authority revalidation begin in Task 007; public exact-membership
mapping remains Task 008.

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

### `MOV-REQ-013` - Exact Breakdown Point rules identity

Authority must use a distinct immutable, non-negative, reduced rational Breakdown Point amount.
The ruleset owns artifact `cna-1979.1.breakdown-tables` with the exact nine accumulated-BP bands
`0-3`, `4-10`, `11-20`, `21-30`, `31-40`, `41-50`, `51-60`, `61-70`, and `71+`; the complete
36-value sequential-dice domain; explicit upward band selection; and the supported
`land.breakdown.profile.truck`/`land.breakdown.vehicle-type.truck` identity.

The Truck profile shifts two columns left. Normal weather has no column shift, Hot shifts one right,
and an applicable Sandstorm shifts one right when Sandstorm-attributed BP is at least half the exact
total BP. Rainstorm treats Road BP as Track BP rather than shifting a column. Effective
columns clamp at the table edges. The approved dice identity maps two sequential d6 to `11`-`66`
(`3,3 => 33`); Task 004B exposes no roll or RNG path. Every normalized row retains primary-source
provenance. The percentage outcome matrix remains deferred to Breakdown adjudication.

### `MOV-REQ-014` - Supported vehicle-cohort content

Each admitted motorized synthetic element must carry exactly one immutable Breakdown cohort with a
stable cohort ID, supported vehicle-type ID, positive working point count, supported rules profile
ID, and explicit synthetic origin. `axis-element-a` and `commonwealth-element-a` each carry one
working Truck Point; admitted non-motorized elements carry canonical `null` and may not carry a
cohort. Cohort IDs are unique across the pack. Content persists source/synthetic facts only, never
accumulated BP, a checked band, or broken runtime state.

This clean cut retains pack ID `rules-lab.content.movement-contact.v1`, advances Content schema
`3 -> 4` and canonical format `v2 -> v3`, and fails closed on legacy shape, unknown profile/type,
capability mismatch, non-positive count, duplicate cohort, or mobility/cohort contradiction.

### `MOV-REQ-015` - Replay-complete Breakdown operational state

The existing Operation-Stage ledger is the single stage key. A cohort-bearing element's operational
state must contain the same cohort ID, exact cumulative BP, exact Sandstorm-attributed BP, nullable
highest effective checked band, and non-negative working/broken point counts whose total equals the
admitted Content count. Sandstorm-attributed BP must be non-negative and no greater than total BP. Creation
seeds exact BP zero, no checked band, the Content working count, and zero broken points. An element
without a Content cohort has canonical `null` Breakdown state.

World `4`, snapshot `9`, and `CampaignCreated` event `8` carry this state canonically. Creation
replay must reproduce byte-identical state; pre-Movement events preserve it. Legacy versions and
content/world/profile/count/stage mismatches reject. Task 004B introduces no pre-Movement event,
Movement BP mutation, Breakdown result, loss placement, action, or RNG consumption.

Before any later task may write a non-null checked band, its context-authoritative validator must
derive the effective band from exact total BP, the admitted vehicle profile, applicable weather,
and prior check history. Task 004B rejects non-check-eligible remembered bands but does not claim
that future mutation-time coherence validator.

## Acceptance criteria

| ID | Acceptance behavior |
| --- | --- |
| `MOV-AC-001` | Exact rational CP values normalize, compare, serialize, and hash deterministically; malformed/noncanonical values reject. |
| `MOV-AC-002` | Golden rules vectors prove Clear/Desert/Road/corrected-Track/Ridge/Slope costs and battalion/terrain stacking inputs with source references. |
| `MOV-AC-003` | Content mobility changes identity; unknown or missing mobility fails admission before campaign creation/Movement. |
| `MOV-AC-004` | Creation and every Movement checkpoint contain valid stage-associated expenditure and Cohesion state. |
| `MOV-AC-005` | Contract 5 / policy `sandtable.observation.movement-side-safe.v1` includes exact own mobility, ledger, Cohesion, and nullable cohort/BP risk; apparent opposing presence exposes only opaque representation ID, location, and current false ZOC, with real bindings and hidden force state absent by shape, bytes, and dependencies. Strict readback round-trips canonical bytes and rejects legacy/noncanonical mutations without creating authority. |
| `MOV-AC-006` | Authorities with byte-identical viewer facts and apparent ID/location/ZOC rows generate byte-identical observations and action sets even when hidden authority differs. Deliberately changed approved apparent facts produce an observation delta confined to `apparentOpposingPresences`. |
| `MOV-AC-007` | Supported adjacent non-contact moves charge exact CP, update location atomically, emit one event, and remain at Movement. |
| `MOV-AC-008` | Reserve, depleted Cohesion, over-CPA expenditure, nonadjacency, stacking, contact/ZOC, unsupported table, stale, forged, and wrong-side cases reject with zero events. |
| `MOV-AC-009` | Completion with zero or more accepted moves advances exactly once to Breakdown Determination and preserves the world. |
| `MOV-AC-010` | Snapshot/event/observation/action canonical bytes and strict readers cover every new versioned field and invariant. |
| `MOV-AC-011` | Replay and re-adjudication produce exact canonical equality for no-move and multi-move histories. |
| `MOV-AC-012` | The checked six-policy Maneuver reaches Breakdown in every child with expected moved/reserved/ledger facts and strict readback. |
| `MOV-AC-013` | Full Core, Exercise Runner, solution, format, and diff gates pass warning-clean. |
| `MOV-AC-014` | Exact BP amounts, nine bands, Truck BAR, weather rules, attribution basis, rounding, sequential-dice domain, provenance, canonical artifact, and mutation-sensitive hash pass positive/negative golden tests without an outcome matrix or RNG/resolver path. |
| `MOV-AC-015` | Content schema 4 / format v3 admits exactly the two supported synthetic Truck Point cohorts, keeps non-motorized cohorts null, and rejects legacy, unknown, duplicate, non-positive, or contradictory shapes. |
| `MOV-AC-016` | Campaign creation seeds context-matching BP state; world 4 / snapshot 9 / creation 8 canonical round-trip and creation replay are byte-identical while pre-Movement events preserve the state. |
| `MOV-AC-017` | Task 004B exposes no Breakdown action, consumes no RNG, mutates no BP after creation, and rejects legacy or forged cohort/profile/count/stage state. |

## Owner approval

On 2026-08-25, the project owner approved the apparent-presence/ZOC visibility ruling, exact
rational Capability Point amounts, the non-contact v1 boundary, the Breakdown terminal, the
over-CPA/Disorganization deferral, and the linked task graph. The completed source/ruling lock and
Rules foundation make the historical `MOV-TASK-002` clean cut complete with exact rational amounts,
a closed mobility vocabulary, normalized source-backed Movement tables, strict canonical artifact
readback, and ruleset v6 identity. The historical `MOV-TASK-003` clean cut is complete with required
per-element mobility, Content schema 3 / canonical format v2, strict legacy and unknown-ID rejection,
and migrated deterministic identities. The historical `MOV-TASK-004` clean cut is complete with
world v3, snapshot v8, creation event v7, exact initial Cohesion/expenditure, and
opaque internal one-to-one representation bindings. On 2026-08-29 the owner approved sequential-die
coordinates, continuity-now, and the Table 21.38 Sandstorm BP basis in `BREAKDOWN-001`; the
source-locked `MOV-TASK-004B` migration is implemented with ruleset v7, Content schema 4 / canonical
format v3, world v4, snapshot v9, and creation event v8. `MOV-TASK-005` freezes observation
contract 5 and the Movement-side-safe policy. `MOV-TASK-006` freezes dormant typed Movement
candidates, exact side-safe cost structure, deterministic identity, pure derivation, and strict
non-authoritative legal-action/submission/receipt readback while keeping public Movement membership
empty. Both tasks are implemented and verified; `MOV-TASK-007` is next.
