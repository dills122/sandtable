# Combat side projection v1

Status: `CMB-TASK-004A1` **blocked, unaccepted candidate**; contract evidence only. Shared codec,
selection, RBA and sealed assignment have limited passing vectors; privacy acceptance fails below.
`004A2` settlement and `004A3` Reserve/cycle plus complete Task003 handoff remain open. A1 is not
complete; this packet does not freeze CON-005, close parent004 or activate runtime.

[Combined plan](../design/combat-cycle-implementation-plan.md),
[ordered schema](combat-side-projection-v1.schema.json),
[retained vectors](fixtures/combat-side-projection-v1.json), and
[oracle](verify-combat-side-projection-v1.py) define this bounded checkpoint.

## Known blocker: private seal changes clock-regression outcome

For both attacker-first and defender-first histories, the observing opponent has byte-identical
views before and after the first private seal. At trusted time 3500, the same canonical proposal
is accepted before that seal, with retained high-water 3000, but rejected after it, with hidden
high-water 4000; the underlying round takes its clock-regression cancellation path. The
[retained diagnostic](../../.planning/2026-09-14-overnight-combat-wave-01/evidence/clock-high-water-counterexample.py)
checks both orders and fails `CMB-PRO-AC-010` equal-outcome acceptance.

[Accepted policy register](../design/combat-cycle-policy-reconciliation.md#decision-register),
lines 25 and 27, requires both finite-deadline/regression fallback (`CMB-POL-004`) and equal visible
history preserving accept/reject behavior (`CMB-POL-006`).
[DES-002 submission/revision semantics](../design/combat-sealed-decision-protocol-v1.md#submission-and-revision-semantics),
lines 124–129, forbids hidden bookkeeping from staling an otherwise identical proposal;
[deadline policy](../design/combat-sealed-decision-protocol-v1.md#deadline-fallback-and-race-policy),
lines 155–163, retains the shared accepted-seal high-water and requires cancellation on regression.
The explicit equal-outcome requirement is `CMB-PRO-AC-010`, DES-002 line 274. No accepted exception
reconciles this counterexample.

Owner disposition is required before changing either clock authority or privacy requirements.
Preserve frozen predecessor bytes; do not narrow privacy, alter the clock rule, accept A1, or start
A2/Task005 without that ruling. The focused oracle passes its limited retained vectors, but the
separate privacy acceptance probe fails. Those focused passes are not an A1 acceptance or formal
review pass; A1, parent004A, CON-005 and parent004 remain incomplete.

## Authority and exact admission boundary

Governing decisions are [DES-001 identity](../design/combat-opportunity-identity-v1.md),
[DES-002 protocol](../design/combat-sealed-decision-protocol-v1.md),
[DES-003 steps](../design/combat-step-transitions-v1.md),
[DES-004 resolution](../design/combat-cost-resolution-order-v1.md),
[DES-005 disclosure](../design/combat-settlement-disclosure-v1.md), and
[CYCLE-DES-001](../design/continual-cycle-reserve-composition-v1.md), under accepted
[POL-001–008](../design/combat-cycle-policy-reconciliation.md). No gameplay or source-policy choice
changes here. POL-006 permits apparent target geometry, own facts and phase progress, including
assignment implying RBA decline. It does not permit enemy resources, hidden cause or slot counts.

The executable corpus is exactly five frozen [C3a selection/step](combat-selection-steps-v1.md)
transcripts and four frozen [C3b round](combat-sealed-round-v1.md) transcripts, each observed by
Axis and Commonwealth. Inputs may stop at any exact retained prefix. Every source name, family,
base, separately retained input and canonical event must match that corpus; each event also passes
its predecessor reader. Regenerated round events and terminal state must match historical goldens.
The round starts from the exact accepted-decline predecessor, including its side histories,
revisions and own receipts. It does not restart audience revisions at Force Assignment.

This is synthetic C3 lineage. C3a's trusted boundary/Weather/Breakdown probes do not become genuine
creation-to-Combat history through this projection. `Source` is internal experiment context, not an
outward wire record or production authentication API. Named corpus admission cannot validate an
unseen otherwise-legal history. Later production must authenticate Chronicle and controller inputs,
replay full causal history, certify the complete admitted profile, and derive its private mapping.
A caller-supplied audience string, snapshot hash or event with its own alleged actor proves nothing.

[Task003 composition](combat-authority-composition-v1.md) remains byte-preserved and hash-pinned.
Its 28 creation-rooted traces and exact handoff are not replaced by these positive synthetic traces.
A3 must consume the handoff and combine both evidence families without widening certified gameplay.

## Closed outward records

The schema is an ordered field/type descriptor, not JSON Schema. All listed fields are mandatory;
nullable fields contain explicit null. The following field allowlist is complete for A1:

| Record | Meaning |
| --- | --- |
| `Context` | Campaign, authenticated audience, visible turn/stage/slot/phasing side/ordinal, approved rules and public timing references. |
| `OwnParticipant` | One stable own participant/component reference; own location, current TOE, spent CP, ammunition and Cohesion. No original authority creation key or provenance receipt. |
| `ApparentEnemy` | One approved apparent location and target reference. No enemy unit/component key, exact strength, resources or number of hidden defenders. |
| `Observation` | Context, canonical public cycle, current structural position, audience revision, own/apparent facts, nullable own round/decision, generic status, retained own receipts and side progress history. |
| `Decision` | Own decision kind, opening audience revision, nullable own slot reference, published deadline, exact action-set reference and ordered own actions. No internal high-water or timing config object. |
| `OwnReceipt` | Original public decision/action references and public acceptance receipt. No authority receipt, accepted event version, timestamp, prefix or cancellation reason. |
| `SideChange` | Audience revision, structural position, generic status and nullable newly accepted own receipt reference. Compact progress log; it is not an authoritative event stream or full state reconstruction format. |
| `Submission` | Explicit version, campaign, approved rules/config references, audience, nullable round/slot, decision, opening revision, set/action and typed candidate. |
| `Outcome` | Exactly version1, `accepted` with own receipt, or `rejected` with null receipt. No diagnostic text, JSON path, provider detail or hidden failure category. |

Core/player/model/side Runner/War Diary consumers must use these typed projections. Trusted raw
Exercise evidence remains private. No arbitrary dictionary, narrative field, passthrough raw event,
seat-independent spectator or authority audit audience is admitted by this codec.

### Closed decisions and statuses

| Window / audience | Exact candidates in semantic order |
| --- | --- |
| Selection / phasing side | `select-close-assault` with exact own participant, apparent target and target location; then `finish-without-attack` with no extra fields. |
| Selection / other side | No decision; generic waiting. Private selection acceptance does not advance that side. |
| RBA / selected defender | `decline-retreat-before-assault` with exact own participant. No actual retreat route or pass alias. |
| RBA / attacker | No decision; generic waiting. |
| Assignment / each unsealed owner | `full-close-assault` with exact own participant, own component and `committedToe=10`. No edited quantity, partial assignment or opponent allocation. |
| Assignment / sealed owner | `own-choice-sealed` and retained own receipt, with no action or remaining-opponent count. |
| Prepared / both owners | Preserve each owner's sealed view until approved structural progress changes it; the second private seal does not announce preparation. |
| Cancelled/closed | Generic `closed`, no timed-out party, internal reason or outstanding count. Accepted own receipts remain recoverable. |
| Structural progress | Exact catalog positions; System-only completions never become player candidates. Non-choice work projects `waiting`. |

No-candidate and voluntary no-attack histories traverse all six steps. Positive A1 ends at the
existing commitment cut; result, retreat, custody, relationships and final settlement disclosure
remain A2. Reaching Reserve Release does not offer release or cycle actions in A1. Unsupported
histories reject; they never become a successful empty candidate set.

## Revisions, references and canonical bytes

Audience revision starts at0 at the admitted C3a boundary. Every accepted event that changes the
allowlisted facts or adds an accepted own receipt advances it once. Events that change no
allowlisted facts leave the entire observation and side-history bytes identical. No subtraction
from authority version, hidden-count offset, empty numbered side event or new round-local counter
is allowed. The round inherits the exact terminal C3a observation before its first event.

Decision identity retains its opening audience revision. An opponent's seal cannot rotate or stale
that decision, action set, slot, round, candidate or own submission. Acceptance consumes only the
owner's action set. Readback keeps the original own public receipt even after steps, commitment or
cancellation; an altered payload cannot replace it.

Canonical values are ASCII JSON in inventory property order, with no whitespace, BOM, newline,
duplicate/unknown/missing fields, alternate escapes, floats, exponent notation or repair. Integers
must be ordinary integers, never booleans. UTF-8 outside ASCII rejects in this bounded family.
Arrays preserve semantic order, with duplicate/action-membership checks supplied by exact replay.
The independent literal assignment candidate vectors pin both owners' bytes in the oracle.

For combat-local references:

```text
pub.<lowercase hex SHA256(ASCII(domain) || 0x00 || canonical payload)>
```

Exact domains appear in the inventory. Preimages are closed:

| Domain suffix | Canonical payload |
| --- | --- |
| `participant`, `component` | `UnitSeed`: campaign, audience, own original element ID, null or exact own component ID. No mutable location/version or authority creation hash. |
| `target` | `{context:Context,locationId:id}` in that order. The admitted singleton apparent marker has no hidden ID/count salt. |
| `rules` | `{rulesetHash,profile,policy,candidateCodec}` in that order: exact full public Rules10 raw64 identity, `singleton-infantry-close-assault`, `CMB-POL-006`, integer1. |
| `config` | `{selectionBudgetMilliseconds:30000,rbaBudgetMilliseconds:30000,assignmentBudgetMilliseconds:30000}` in that order. These are this corpus's published finite budgets, not the private configuration hash. |
| `round`, `decision` | `IdentitySeed`: context, canonical public cycle, position, opening audience revision, decision kind, own participant and apparent target. Round uses `force-assignment`. |
| `slot` | `SlotSeed`: own round, audience and attacker/defender role. |
| `action` | `ActionSeed`: public decision and exact typed candidate. |
| `set` | `SetSeed`: public decision, opening audience revision and ordered actions. |
| `receipt` | `ReceiptSeed`: original public decision and accepted action. |

The canonical public cycle is **not** a combat-local JSON hash. It is the exact frozen
[cycle codec1 Public tuple](combat-cycle-sequence-v1.md), domain `sandtable.cycle.public.v1`,
with `sha256:` display prefix. Preserve full Rules10 raw64 `rulesetHash` in its original field;
never substitute the digest of `rulesRef`. These are the already-approved audience-invariant
sequence facts. Rules10 contains public artifacts/rulings/policies, excluding mutable state and
configuration. Its identity is distinct from private authority state/base/prefix/Setup/Content
hashes, which never enter the outward family.

Limits:65,536 bytes/value, depth16, generic arrays64, at most2 actions,3 own receipts and32 side
changes. IDs are1–128 printable ASCII characters; combat-local refs are `pub.` plus64 lowercase
hex. Revisions fit nonnegative signed64; turn1–111, stage1–3, ordinal1–2,147,483,647; timestamps
0–253,402,300,799,999 milliseconds. This C3 corpus has own TOE/ammo/CP/Cohesion0–10 and full
assignment exactly10. Bounds do not expand actual admission. Overflow rejects rather than truncates.

## Submission and private mapping

Authenticate seat first. Decode exact Submission; require explicit audience and every public
campaign/rules/config/round/slot/decision/revision/set/action binding to equal an actually offered
candidate. Selection/RBA round and slot references are explicit null. Recover an already accepted
exact proposal through its original own receipt; reject any changed payload using that decision.
Otherwise require the current action set still equals the offered set.

The internal mapping consists of the retained source base, complete authenticated prefix and exact
public offered record. It resolves the choice back to original unit/component/target/decision/slot
before invoking the predecessor transition at the current authority cut. Opposing private seals
are permitted by replay; changed frozen World/RNG/base/config or forged receipt/event suffix is not.
Unknown, partial, reordered and foreign source history rejects before declassification.

This oracle adjudicates proposals without retaining side effects; it does not claim a durable
publication API. It proves allowed proposals reach the actual predecessor transition and produce
the required acceptance effect. A clock-loss cancellation is not reported as an accepted seal.
At deadline equality, unaccepted choices reject; an exact previously accepted receipt remains
recoverable after deadline. Existing predecessor authority owns clock/fallback events and RNG.
All outward rejection reasons share identical Outcome bytes.

## Compatibility and future Dispatch

Current production contracts remain unchanged: Observation5/6/7; legal action set2 with current
policy `sandtable.legal-actions.v3` and historical v2; candidate/submission/receipt1; projected
history1/2. This packet's local version1 allocates no successor production version, protobuf field,
registry entry, handler, transport capability or migration. Old readers and historical bytes retain
their original meanings; runtime migration/admission/rollback remain later tasks.

Future typed transport must map and echo these bindings explicitly before generated code exists:

| Transport meaning | Outward value / trusted private mapping |
| --- | --- |
| `decision_id` | Public `decisionId`; host maps to exact private selection/RBA/round/slot decision. |
| `state_version` | Decision `openingRevision`, the audience version bound by that offer; never current private authority event count. Current observation revision is separate. |
| Rules binding | Approved exact full Rules10 identity with matching public `rulesRef`; no state/base/history hash. |
| Configuration binding | Public `configRef` for approved budgets/policy only; host retains separate full private configuration identity. |
| Audience | Authenticated controller/seat, checked against explicit audience; never trust payload assertion as authentication. |
| Round/slot/set/candidate | Exact outward fields copied without rebinding; private host map restores original authority references. |

Selection, RBA and assignment remain separate finite decision contexts. Cancellation, deadlines,
deduplication, durable outbox reconciliation and late reply validation stay required in future
Dispatch. Model I/O stays outside authoritative turns. Content-byte equality here does not certify
host timing/traffic privacy, constant latency, restart scheduling or publication atomicity.

## Traceability and verification

| Requirement | A1 evidence | Remaining owner |
| --- | --- | --- |
| CON-005 closed choices/errors/bytes | Four candidate arms, explicit submission context, canonical vectors, common rejection outcome and strict readback mutations | A2/A3 complete remaining arms;020–021 runtime |
| ID-AC-006/009/010; PRO-AC-001/002/003/010 | Public domains/cycle codec, actual opposing seal equality, unchanged success/receipt bytes, changed proposal and authority-source rejection |020–021; hosted traffic deferred |
| STEP-AC-004/005/006/009/010/012 | Five historical selection/step traces, own-only selection/decline receipts, generic closure, all six no-attack positions, inherited C3a→round public history |009–011/020–021 |
| PRO-AC-012 | No production edits;20 source pins preserve predecessor goldens and exact Task003 files |008/020–024 integration and migration |
| Full CON-005; SET/RES/CYCLE projections | Explicitly incomplete in A1 |004A2/004A3; Task004C maps all72 criteria |

Run:

```sh
python3 -B docs/specs/verify-combat-side-projection-v1.py
python3 -B docs/specs/verify-combat-selection-steps-v1.py
python3 -B docs/specs/verify-combat-sealed-round-v1.py
python3 -B docs/specs/verify-combat-cycle-sequence-v1.py
```

Default verification requires the retained fixture; it never recreates missing or changed goldens.
Every retained observation cut has byte count/digest; representative canonical observations retain
all status/decision shapes. Expected own assignment candidate bytes are also literal independent
vectors. Actual equivalent histories cover both opposing seal orders and empty-versus-private-seal
cancellation. Hidden seed/cursor/identity/resource/prefix perturbations are separately labelled
**declassifier-only non-admission probes**; forged source variants reject and never count as valid
campaign histories. Accepted configuration variants, unseen profiles, settlement and full Task003
projection are not claimed by these probes.
