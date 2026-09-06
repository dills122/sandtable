# Combat World7 and settlement values

**Status:** TASK-003B contract packet, input `e3d1a26`, 2026-09-06; parent003 remains open for003C/D.
[POL-001–008](../design/combat-cycle-policy-reconciliation.md), [DES-005](../design/combat-settlement-disclosure-v1.md),
[identity](../design/combat-opportunity-identity-v1.md), [source manifest](../research/combat-source-freeze-v1.md)
and [003A](combat-creation-ledger-v1.md) govern. No production reader, migration or public capability.

## Scope and exact value inventory

The [schema inventory](combat-world-settlement-v1.schema.json) is normative: each object declaration
lists mandatory properties in canonical order and their exact types. `T?` is either null or T;
`T[]` is a non-null collection. Missing, extra and duplicate fields reject. Element, Operational,
Component, Ammunition, Readiness, CP and provenance shapes are exactly003A; their **initial-only**
quantity restrictions do not apply after paid use. This packet adds durable aggregate state and
receipt payloads;003C freezes enclosing event/snapshot/decision/config bindings and actual receipt
identity derivation. These payloads alone are not authenticated commands or complete saved campaigns.

Reserve `CampaignWorldSnapshotV7` contract7, and `CampaignCombat`-prefixed immutable value types:
`UnitKey`, `ComponentKey`, `CohesionCause`, `Relationship`, `CustodyLot`, `GuardAsset`,
`ReplacementEntitlement`, `FutureObligation`, `SettlementState`, `ResultFacts`, `RetreatDisposition`,
`RoleLoss`, `LossReceipt`, `RetreatReceipt`, `CustodyReceipt`, `RelationshipsReceipt`. Their property
names/types/order are the corresponding schema entries. Constructors copy collections and compare
structurally. Reuse existing component TOE and map representation value types, not parallel balances.
World7/these names were unallocated in runtime at input;003A already reserved the World version.

World fields retain existing elements/representations/broken lots, then add cause history, original
participant relationships, custody/guards/entitlements/future work, and settlement state. A settlement
retains immutable post-commit/pre-loss elements and result facts plus its successive receipts.
Historical before/after evidence is distinct from the sole current `elements` ledger. No current
TOE/ammo/CP/Cohesion total is repeated in a second live account. Snapshot/Chronicle must bind the
same creation identity and rederive this World from exact accepted events; a plausible World is
not independently sufficient evidence of legitimate history.

Existing World6 uses [Element5 and operational fields](../../src/Cna.Core/Campaigns/CampaignWorldV5.cs),
[representations](../../src/Cna.Core/Campaigns/CampaignMapRepresentationState.cs) and
[World6 codec](../../src/Cna.Core/Campaigns/CampaignV11CanonicalCodec.cs). Reuse representation order
`representationId,currentLocationId,bindingKind,boundElementIds`; each original independent element
has exactly one `independent-element` binding at its current location. Guards are distinct assets
with their own locations and source links, not extra infantry components or original-unit markers.
Their outward representation and action support are004/020; this contract grants neither.

Existing Breakdown lot/value codecs and non-null operational Breakdown/movement-ended variants stay
unchanged. The closed Content7 fixture has no vehicle cohorts or broken lots; its golden worlds
require those fields null/empty. No cross-profile migration or reset of an old nonempty World is
admitted.003D must specify continuity of movement-ended state, BP/bands/lots where applicable; it
cannot use this empty fixture as evidence that dropping predecessor fields is safe.

## Identity, numbers, ordering and bounds

`UnitKey=(creationBinding,originalSide,elementId)`; component keys add `componentId`. Creation binding
is a stable ID pinned before constructing World, not the hash of an event that already contains
World.003C derives it from nonrecursive creation-request inputs and verifies full campaign/Rules/
Setup/Content context. A changed location, TOE, parent, side-visible alias or guard transfer cannot
change the key. Original side is validated against immutable Content, never changed to captor side.

IDs use Content7 grammar and128-character bound. Settlement IDs are at most80 characters, leaving
room for deterministic suffix IDs. A settlement's commitment/result IDs are distinct and all three
refer to the same occurrence through003C. Payload receipt IDs equal settlement ID plus `.disposition`,
`.losses`, `.retreat`, `.custody`, `.relationships`; dependent asset IDs use `.captives`, `.guard`,
`.replacement`, `.upkeep`, `.training`, `.relation`. Each suffix denotes exactly one object; duplicate
or cross-occurrence references reject. These are internal IDs, never outward tokens or ordering salts.

Integer types/ranges, hashes and canonical UTF-8 follow003A. `futureTurn` is1–115 while playable
`gameTurn` is1–111; all stages1–3. Long/CP arithmetic remains checked and exact, reduced fractions.
Selected settlement requires integer expenditure and CPA10. Guards/current infantry have nonnegative
TOE/ammo; admitted pre-loss TOE10, ammo0 after paid commitment, Cohesion0, attacker post-cost CP5–10,
defender3–10. Current post-loss TOE7–10 before a possible one-TOE donor transfer; guards TOE1.
All static maximum/rating/Morale facts still come from Content. Source values are never adjusted to
fit a selected result. Later post-settlement movement bounds are003D, not limited to CPA10 by this packet.

Canonical arrays sort by schema keys; element/component/reference arrays reuse003A keys. Route is
an **ordered path including its origin**; never sort or deduplicate it. `boundElementIds` sorts by ID.
Cause `ordinal` is a contiguous one-based causal append order, not lexical cause ID order. Duplicate
keys reject before artifact creation. World bytes are minified UTF-8, no BOM/newline, max1,048,576 bytes,
max depth32; canonical readback is mandatory. Maximum selected live inventory is two original
elements, two representations, one settlement, one relation, one lot, one guard, one entitlement,
and one future obligation. Cause capacity4096 includes later compatible movement causes. Prove
remaining capacity for every reachable effect before choice; reaching a serializer bound after
commit is a defect, never a cancellation or silent history truncation. No reset at repeat.

## Ordered receipt and publication contract

Result facts retain ordered-d6 coordinates11–66 with each die1–6, differential−2…+2, both percentages,
raw Engaged, required Retreat0/1, nullable captured role, nullable capture die and its share. Check
all values against the normalized source tables, including accepted three-cell amendment. Capture
role is `attacker`/`defender`/null; no die or share without trigger, and only one trigger is possible.
003C binds real purpose-labelled RNG/cursor evidence to these facts; synthetic fixture inputs are
not proof of a live RNG transcript or reachability after particular Movement commands.

Settlement progress is derived from receipt presence, not another mutable status counter:

| Prefix | Required / permitted fields | Current World effects |
| --- | --- | --- |
| Resolved | all five receipts null | Current equals frozen post-cost/pre-loss state; ammo is already0 |
| Disposition | disposition only | No World mutation; real one-hex route/refusal or system not-required |
| Losses | disposition + losses | Joint TOE loss, cause-specific loss DP, optional pending lot |
| Retreat | preceding + retreat | Actual defender movement/CP/overrun DP, then proved attacker victory RP |
| Custody, only if positive lot | preceding + custody | Atomic relocation/guard transfer or escape/entitlement |
| Relationships | preceding + relationships; custody iff positive capture | Reconcile original pair and satisfy all immediate settlement work |

Null custody is required when captured quantity is0, including triggered capture with rounded loss0.
It is not a missing decision or an invented zero-quantity lot. Relationship closure predecessor is
custody receipt for positive capture, otherwise retreat receipt. Every other predecessor is the
immediately preceding receipt; disposition refers to result ID. Gaps, incompatible extra receipts,
wrong order, wrong occurrence and duplicated publications reject without partial effects.
Retreat/custody decision windows, user/system choice provenance, finite budgets and exact timeout
races remain003C; a receipt records accepted intent, not authorization to bypass that window.
No settlement stage draws RNG or permits an unrelated action. Completed receipts remain after closure.

Disposition `kind` is `retreat`, `refuse-retreat`, or `not-required`. Required=planned+unfulfilled;
retreat route is the certified two-node one-hex path, planned1/unfulfilled0; refusal route contains
only origin, planned0/unfulfilled1; not-required has all distances0 and origin-only route.
No later route revision or renewed choice is allowed after its receipt. Geometry must revalidate
against frozen authority: Clear featureless edge, farther from attacker and toward defender's
certified nearest-friendly-supply direction; no entering blocked/enemy-controlled hex. Content's
initial geometry does not certify arbitrary later positions or hidden-equivalent worlds (009/020).

Losses has exactly attacker/defender role records, each bound to its original component. Let U be
unfulfilled distance. `L_A=ceil(10*P_A/100)`, `L_D=floor(10*(P_D+10*U)/100)`.
`C_s=ceil(L_s*share/100)` only for captured role, otherwise0; `otherLoss=L-C`, `remaining=10-L`.
Always `10=remaining+captured+otherLoss`. Captured TOE is included once in L, never deducted again.
Loss DP is3 iff `100*L>=30*10`, otherwise0. Guards do not alter L or its threshold.

Retreat has `kind` matching disposition, exact route, completed distance0/1, defender CP before/after,
new excess DP and attacker victory RP. Completed retreat adds1 CP and
`max(0,E+1-10)-max(0,E-10)` DP immediately, including10→11. Refusal/not-required adds neither.
Only actual full defender evacuation caused by this assault grants attacker3 RP, after loss DP,
capped at+10. Cause records retain `kind=loss-dp|retreat-excess-dp|assault-victory-rp`, original
unit ID, receipt/scope, positive points and exact before/after Cohesion; omit zero causes.
Append loss causes in attacker/defender order, retreat DP, then victory RP. DP subtracts points;
RP uses `min(10,before+points)`. Never net loss and recovery into an anonymous zero delta.
003D extends source causes with ordinary break-off/terrain overrun under this same single ledger.

## Custody, guard and future obligations

Lot quantity1–3, origin exactly victim's pre-retreat location, original component key retained,
captor the opposite original unit. After loss its status is `pending`, current location=origin;
no guard/escape link. Pending lot exposes only its settlement continuation, never free movement.

`relocate-and-guard` route is the certified origin-to-post-retreat-captor path, at most3 edges;
it may leave enemy-occupied origin, but entered nodes cannot be enemy occupied/controlled. Custody
receipt binds loss lot and donor TOE before/after (difference1); guard ID is required, entitlement
null. Publish free prisoner relocation, donor transfer and guard formation atomically. Lot status
`guarded`, location=guard=donor, guard link set and escape link null. Guard inherits donor's post-retreat
ledger CP/Cohesion, readiness and initial provenance, receives0 ammo, and records donor component /
formation receipt. Its source CPA10 and ratings0/1 are explicit fixed guard facts. No extra combat
loss, reset ledger, remote donor, second component or inherited Contact/Engaged marker.
Moving custody ratio1:5 and quantity≤3 make exactly one transferred guard sufficient.

`leave-unguarded` uses the certified origin-to-surviving-original-unit reunion route,≤8 CP under
Normal Clear terrain, ignoring enemy units/ZOC for this source distance test. No guard transfer:
donor before=after, guard link null. Lot status `escaped`, current location null, escape receipt set;
original victim still has its post-loss TOE. The entitlement retains original component, quantity,
reunion location, earned scope, delay12 Operation Stages, eligible scope and status
`awaiting-eligibility-and-training`. No immediate TOE credit, training or attachment occurs.

Calendar uses `(earnedTurn-1)*3+earnedStage-1+12`, then inverse ordinal conversion. Valid earned
turn111/stage3 produces eligible115/stage3: retain it beyond playable horizon, never clamp, wrap,
refuse an otherwise admissible late capture, or extend campaign support. Exact future eligible
phase, training and Reorganization absorption remain unsupported source/activation gates.

Guarded branch retains one obligation `kind=guard-priority-upkeep`, subject=guard ID,
earned scope=current, eligibleScope=null, activationGate=`before-prisoner-upkeep-or-guard-action`.
Null does not mean no obligation; water/stores priority is required from creation of custody and
must be carried to the next applicable Logistics/guard-action boundary. This packet does not invent
that boundary's executable phase mapping. Escape branch retains `kind=replacement-training-gate`,
subject=entitlement ID, same earned/eligible scopes, activationGate=`before-replacement-eligibility-training`.
Both have status `retained-unimplemented`. Same-slot Truck Convoy **entry** may retain them;
unsupported execution beyond these gates is forbidden. Round closure requires zero immediate work,
not zero future work. One-month eligibility is not trained/rejoined strength.

Relationships evaluate only original surviving pair at final positions: raw Retreat suppresses
Engaged even on refusal; adjacent originals get Contact then. Without Retreat, raw Engaged yields
Engaged; otherwise adjacent gives Contact, separated gives neither. Relation kind `contact` or
`engaged`, active=true, ending fields null at settlement. Even no relation needs the relationship
receipt with null ID/kind. Future ended records retain original endpoints and ending receipt/cause;
003D freezes break-off/stage-end transitions and cannot erase attack history or unrelated membership.

## Verification boundary and restart cuts

[Oracle](verify-combat-world-settlement-v1.py) and [vectors](fixtures/combat-world-settlement-v1.json)
validate this selected contract using source-derived result inputs and literal canonical World
examples. Receipt payloads can be reconstructed from trusted prior state; every stored cut is
compared with its independently supplied pre-loss/result fixture. A changed prior/result alongside
changed World must be rejected by enclosing history validation in003C; no World-only self-consistency
check proves that binding. Restoring a cut continues its same retained disposition/custody choice;
it does not repeat a debit or re-open a decision. Failed publication leaves the prior cut intact.

| Cut / attack | Required validation / later implementation |
| --- | --- |
| Before/after every receipt | Exact null/present suffix, role references, current resources, causes, lots and obligations;003C/008/014–016 prove real publication, retries and timed decisions |
| Loss with captured subset | One casualty debit; guard transfer separate; escape never restores TOE |
| Actual retreat from CP10 | CP11 and1 DP after loss; frozen result unchanged; victory once |
| Guarded / escaped closure | No pending lot, original-only relationships and retained future work; no new RNG |
| Tampered amount, route, predecessor, scope or absence | Reject before effects, never fallback to repaired history |
| Later ordinary movement / repeat |003D extends current-location and cause/history replay; never run initial003A validation as a reset |

POL-001/003/005 and CMB-SET-AC-002/003/004/006/007/009/012 map directly to these arithmetic/value
checks. AC-001's live/public certification, AC-005/008's deadlines/retries, AC-010's real replay cuts,
and AC-011 privacy remain explicit003C/D/004 and runtime008–020 obligations. Parent003 is not complete.
No new Rules hash, full Snapshot12 or Created11 bytes, public projection or runtime test claim.

## Executed evidence

`python3 docs/specs/verify-combat-world-settlement-v1.py`: six literal canonical World goldens,
57 single-edit rejection vectors,112 isolated receipt-cut readbacks across20 role/choice scenarios,
8,840 source arithmetic combinations and eight calendar boundaries pass. Both capture roles and
attack orientations, both custody choices, retreat/refusal, CP10→11, raw Retreat/Engaged precedence
and zero-loss relations are included. Source enumeration remains arithmetic evidence, not8,840
executed campaigns. Cut readbacks and prefix comparison are not a production restart test.

World diagnostics: `CMB-WLD-001` JSON/shape/byte/depth; `002` primitive or selected input bounds;
`006` value/reference/causal mismatch against trusted input and cut; `008` noncanonical bytes.
All are trusted-only, with JSON Pointer paths. No outward error mapping is established.
A missing non-null receipt initially exposed an unchecked nullable comparison in the oracle; it
now rejects as006 and remains a negative vector. Future C# tests must use independently supplied
expected source/cut facts, not regenerate assertions from production projectors.
