# Combat selection and pre-assignment step contracts

**Status:** `CMB-TASK-003C3a`, contract-only slice at input `568027c`, 2026-09-07.
[Plan](../design/combat-cycle-implementation-plan.md) retains parent003C3 and checkpoint B.
[DES-003](../design/combat-step-transitions-v1.md) and
[sealed protocol](../design/combat-sealed-decision-protocol-v1.md) govern transitions;
[003C1](combat-rules-inputs-v1.md), [003D1](combat-cycle-sequence-v1.md),
[003C2](combat-authority-envelope-v1.md) and [World7](combat-world-settlement-v1.md) supply dependencies.
[Schema inventory](combat-selection-steps-v1.schema.json),
[literal traces](fixtures/combat-selection-steps-v1.json) and
[oracle](verify-combat-selection-steps-v1.py) freeze this bounded control fragment.

This slice supplies exact command/event/control bytes for voluntary selection, no-attack traversal,
explicit RBA decline/cancellation and handoff at Force Assignment. C3b freezes prepared/sealed/commit
states; C3c freezes result/settlement/CA closure and full noninitial Snapshot12 composition. No new
C# type, registry, hosted adapter or universal snapshot reader is activated. Existing C2 creation
bytes remain unchanged. Null/empty fields in creation state are not reinterpreted as lost history.

## Trusted opening boundary

Boundary is a retained **input value**, not a new Chronicle event. An authoritative caller must
first validate exact creation/Rules/Setup/Content/config, completed Breakdown, no Reaction, actual
cycle/order and the complete prior history. The oracle receives that boundary and authenticated
commands independently of the bytes under review. Hash equality is binding, not authentication.

Boundary order/types appear in inventory. It includes full World7/RNG, cycle Authority, first acting
side, prior authority version/prefix, completed Breakdown receipt, exact Position Determination
position, applicable weather, idle Breakdown and null Reaction. It is frozen throughout this segment.
`creationBinding` and cycle provenance must match003C2's exact compatible identities. Cycle's original
opening prefix remains distinct from the later segment-opening prior prefix. Require cycle opening
version≤current prior version. No event can replace this context from its own payload.

C3a admits first relative slot, stage1, turn1, ordinal1 only; resolved side may be Axis or Commonwealth
according to retained first-side order. Other scopes require C3b/c/D2 admission evidence, even though
catalog5 can structurally name them. This oracle binds the retained C2 fixture campaign/Setup/Config
and seed; it does not generalize campaign or configuration admission by accepting matching names.

Current World is the certified initial infantry inventory at its certified initial positions, with
ordinary integer CP0…10 per original unit. Current CP may differ from initial CP0; it is not reset.
All other current values, provenance, representations and empty history/obligation arrays match the
selected compatible fixture. Movement histories explaining that expenditure are caller prerequisites,
not fabricated here. Broader locations, loss/relationship state, overrun, Reserve or later cycles are
not admitted by this boundary validator. This deliberate subset cannot replace D2's full movement
and cycle history contracts.

`ApplicableWeather` retains turn/stage, attacker/defender applicable kind and exact Weather receipt
hash. Kinds are `normal|hot|sandstorm|rainstorm`; receipt/scope and area applicability must come from
validated current Weather history, using existing weather codecs. This is an authority projection
of that history, not a replacement dice result or a caller-selected forecast. Only both `normal`
permit the positive candidate. Other supported kinds take the no-candidate path without rerolling.
The isolated probes retain synthetic boundary/Weather/Breakdown hashes explicitly labelled as such;
they do not certify a real creation-to-Combat trace or claim live admission is implemented.

Derive zero or one provisional candidate from verified World/Content: one independent non-Reserve
infantry battalion per side, one full10-TOE component, ammo10, Cohesion0, stage readiness, original
parent, no gun/armor/ZOC/mandatory attack/relationship or outstanding obligation. Adjacency, initial
certified retreat/custody geometry and Normal Weather must hold. Attacker current CP≤5, defender≤7.
Content7 validation and retained source/World contracts establish the fixed capabilities and result
surface; C3c/009 must implement full reachable-result certification before any live offer.

Candidate binds original UnitKey, map representation, current location and component IDs for both
roles, plus exact target hex and `voluntary-adjacent` basis. No user-supplied empty list, forged
candidate, changed resource or undisclosed capable unit proves absence of work. Candidate selection
is provisional intent; final opportunity/base is constructed later at Force Assignment after decline.

## Identity, framing and canonical grammar

All new objects use contract1. Schema is a mandatory-field descriptor, not JSON Schema. Closed
objects reject unknown/missing/duplicate properties and wrong types; nullable fields require explicit
null. Arrays preserve semantic order in this packet; initial World/Content ordering follows earlier
contracts. Do not sort step history, accepted input order or receipts. IDs/hashes/numbers reuse003C2;
new `actor` is `axis|commonwealth|system`, and typed UTC follows003C1. Maximum whole value1MiB,
depth32, arrays512 and16 accepted events/receipts for this selected control fragment. Tighter step
and participant counts apply. Refuse an operation before publication if version/capacity would exceed
bounds. These limits must be reconciled with full Snapshot12 in C3c/D2, never used to truncate history.

Canonical bytes have exact inventory property order, no whitespace/BOM/newline, noncanonical escapes,
floating-point integers, unknown tags or repair. Error checks are shape/bounds, canonical spelling,
then trusted-state semantics. This order differs from some predecessor oracles and is explicit.

```text
segmentId = "seg." + hex(SHA256(ASCII("sandtable.combat.segment.v1") || 0 || canonicalBoundary))
selectionDecisionId = segmentId + ".selection"
rbaDecisionId = segmentId + ".rba"
receiptId = "cmb." + hex(SHA256(ASCII("sandtable.combat.step-receipt.v1") || 0 || eventWithoutReceiptId))
```

Hex is lowercase. Segment/receipt IDs are68 characters; decision IDs remain within128. Each domain
has one canonical JSON payload after its zero separator. `eventWithoutReceiptId` has all Event fields
except final `receiptId`, in declared order. Event includes its input/effect and **prior** prefix,
not its own hash or resulting prefix. After complete event bytes E exist, compute003D1
`Pnext=D(prefix.event,H(Pprior)||U64(byteLength(E))||E)`. One accepted event increments authority once.
No recursive digest, extra step event, second live resource balance, or public token is introduced.

## Commands and trusted admission

All Command fields are mandatory. The following table lists fields permitted non-null beyond
`contractVersion,kind,segmentId`; every other field must be null. Required combinations are validated
against live state. This is a closed tagged union represented by one fixed-order record, not a generic
parameters dictionary.

| Kind | Permitted fields / caller |
| --- | --- |
| `open-segment` | `expectedPriorVersion`; system only, unopened boundary |
| `close-empty-selection` | `expectedPriorVersion`; system-only no-window outcome |
| `choose-selection` | `decisionId,choice,candidate`; authenticated phasing side; candidate required only for `select-close-assault`, null for `finish-without-attack` |
| `open-rba` | `fromPositionId,expectedPriorVersion`; system at RBA after live selection |
| `decline-rba` | `decisionId,participant`; authenticated selected defender, exact original UnitKey |
| `complete-step` | `fromPositionId,expectedPriorVersion`; system only |
| `expire-window` | `decisionId`; trusted system wakeup |
| `controller-unavailable` | `decisionId`; trusted adapter, never player/model asserted |

`Input=(command,actor,admittedAt,clockAvailable)` is trusted admission evidence. `actor` must be derived
from authenticated seat/controller context outside the command. It is not an authentication mechanism
for untrusted callers that set their own actor. Future outward actions map to this private contract
only after004 and typed transport/authentication work. A reviewer/replay caller must supply retained
trusted input separately; taking both event and its alleged authenticated input from the same attacker
would not authenticate history.

For live choices, require exact current decision, owner, candidate/participant and admitted time
strictly before deadline, with no clock regression below retained high-water. Config/window budgets
copy003C1 exactly; acceptance only changes retained high-water, never opening or deadline. Selection
and RBA have separate windows. RBA opening time cannot precede last accepted selection high-water.
Missing/invalid time while claiming reliable clock rejects before opening; lost confidence follows
trusted unavailability. System structural completions take null time and available=true; no wall-clock
read is added to replay.

Exact canonical command hash plus authenticated actor returns retained receipt without a new event,
including after response loss or expiry. A changed payload after the decision was consumed rejects;
it cannot edit an accepted selection or decline. Old/mismatched system timer or unavailable decision
IDs are no-ops and cannot affect another live window. Timer before deadline is a no-op; equality
expires. Rejected/read-only/no-op calls preserve all fields. Pre-opening RBA unavailability matches
that segment's exact `.rba` ID and current RBA state; it is not an arbitrary cancellation capability.

## Events and transition effects

Each Event binds exact campaign/rules/config/cycle/segment, prior/result version, prior prefix,
trusted input, closed Effect and derived receipt ID. `eventType="combat-"+effect.kind`. Tags:

| Effect | State change and required evidence |
| --- | --- |
| `segment-opened` | Same Position Determination. Bound boundary hash; zero candidates or trusted unavailable opening gets no window and requires system no-selection; otherwise one phasing selection window. |
| `selection-closed` | Exactly selected or no-selection; optional candidate/timing only as state permits. Save receipt; remain at Position Determination. No commitment, target use or RNG. |
| `rba-opened` | At RBA with selected intent and prior Barrage step receipt. Save one defender window; no new selection or position advance. |
| `rba-declined` | Exact live defender choice/participant/selection receipt and admitted timing. Save decline receipt; remain at RBA. No move, CP/ammo cost or inferred Contact. |
| `selection-cancelled` | Live RBA unavailable/expired: retain selection and cancelled receipt, no decline. Timing required if window exists; null only for trusted unavailability before RBA window could open. |
| `step-completed` | Exact from/to, previous step/opening receipt, current disposition receipt and derived proof; increment structural step once. |

If clock confidence is unavailable at segment opening, no deadline is invented: persist opened
context with null window, then system no-selection. If unavailable before an RBA window opens,
record cancellation with null timing and no RBA window. Missing clock cannot leave a selected
attempt permanently waiting to mint a fresh deadline. Neither branch fabricates a decline. Existing
open windows cancel at regression/unavailability with high-water retained (or advanced for a later
valid UTC input); no clamp changes an opening/deadline.

No-selection/cancelled paths prove `no-attack` and complete all six exact catalog steps to same-slot
Reserve Release. No reselect, retarget, reseal, resource debit or attacked-history/target-use entry.
Positive path closes Position (`no-gun-positions`), Barrage (`no-barrage-work`), and RBA
(`accepted-decline`) before stopping at Force Assignment. A pending RBA cannot complete its step.
C3a rejects positive FA completion as unsupported until C3b's prepared proof exists; it cannot infer
both assignments or close a committed assault. C3b/c extend the prospective event/control family
before checkpoint B and preserve these frozen cases. No extra SegmentCompleted event advances again.
Arrival at Reserve Release releases nothing, resets no ledger and does not finish the stage/cycle.

Control is a typed fragment with boundary/segment identity, version/prefix, current step, selection
outcome, preserved choice/window/receipt evidence, ordered step receipts and accepted-command ledger.
Closed windows remain recorded; active window is derived from state. `closed=true` occurs only after
six no-attack completions here. Restore compares exact Control bytes with independently supplied
boundary, inputs and accepted events. It is not a full Snapshot12, archive authentication, actual
restart, cumulative stage history or post-loss recovery reader.

## Diagnostics, evidence and remaining gates

Private codes `CMB-STP-001` decode/type/shape/size; `002` primitive bounds; `003` unsupported version,
tag or command-field combination; `004` context/actor/participant/candidate mismatch; `005` clock;
`006` stale/lifecycle/event/control mismatch; `007` dependent prepared/result contract unavailable;
`008` noncanonical bytes. Decoder paths empty, field errors use JSON Pointer. No outward mapping
is defined here. Unsupported input is never repaired into a successful pass.

`/opt/homebrew/bin/python3 -B docs/specs/verify-combat-selection-steps-v1.py` checks two literal
boundary probes, five independent literal event traces (41 events), five final Control goldens,
41 event/control cuts,246 event mutations,164 raw-byte rejections and exact duplicate receipts.
Additional checks cover just-before/equal/after deadlines, backward clock, unavailability before and
after opening, stale selection versus RBA timers, defender-only decline, changed-payload retry,
step reorder, missing receipt, Normal/non-Normal weather, certified geometry and positive FA gate.
Generator wrote literal events independently; verifier reconstructs effects from trusted inputs and
prior state. Final Control fixture uses a separate event fold. Shared prior fixture/research lineage
is disclosed; none is a production campaign transcript or independent review verdict.

DES003 AC001/002/004/005/006/009/010/011 gain bounded contract evidence. Positive full-six-step AC003,
prepared trace AC007, completed settlement AC008 and side-privacy AC012 remain C3b/c/004 and runtime
consumers. Broader admission, genuine completed-Breakdown/Weather/Movement evidence, lost-publication
races, full Snapshot12 capacity/causal replay and hosted dispatch are still explicit gates.
Independent review count remains8of8, previously used for C2; this slice has author checks only.
