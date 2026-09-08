# Guarded cycle repeat/finish and Movement exception expiry

Status: CMB-TASK-003D2b.2 complete as a bounded private contract checkpoint, input `184b16c`. Governing [plan](../design/combat-cycle-implementation-plan.md),
[cycle design](../design/continual-cycle-reserve-composition-v1.md), accepted POL-004/007/008.
[Inventory](combat-cycle-control-v1.schema.json), [fixture](fixtures/combat-cycle-control-v1.json),
[oracle](verify-combat-cycle-control-v1.py). Private contract experiment; no runtime registration.

## Admitted boundary and evidence

CycleControlBase embeds the independently admitted D2b.1 ReleaseBase and exact input/event suffix.
Replay must reach completed Release, with no pending member or immediate obligation, before any
control action. World must match its retained hash after projecting only accepted Reserve changes.
The base also carries same-scope/current-ordinal Movement-end evidence, retained target uses and
progress references exported from validated current-cycle events. A digest is not authentication:
raw base admission compares against independently reconstructed fixture inputs, not self-consistency.
D2c must replace the synthetic Movement-end/earlier-history evidence with actual accepted successors.

Two explicit profiles: `settled-control` derives C3c result/World/RNG, commitment progress and empty
Release; `isolated-release-control` constructs a synthetic resource/Reserve World and then executes
real D2b.1 dispositions. The latter proves the control contract, not reachable Content7 Reserve
history or a complete campaign. Both pin normal Weather, singleton independent infantry, Clear map,
no positive ZOC, no live Reaction/Breakdown/settlement and no vehicle lots. Guard/escape obligations
remain retained and stop at Truck Convoy entry. No supply, repair or stage housekeeping is invented.

## Assessment

Derive progress from validated C3b commitment receipts and actual D2b.1 I→none, I→II or II→none
status changes. Release open/complete, retain-II, private seals, timers and query/empty receipts do
not count. Progress references bind exact event type/hash/receipt; reject duplicates or substitution.
Earlier Movement/Reaction/Breakdown progress needs its D2c successor adapter before admission; this
packet does not treat arbitrary event names or caller booleans as evidence.

Movement-end locations cover every original unit in canonical key order and include prior phase-local
exclusions. Ordinary eligibility requires an enemy within two graph hexes at that Movement end and
no earlier exclusion. Later Combat retreat/proximity changes cannot restore it. A pending released
Reserve exception applies only to the immediately next ordinal in exactly the same turn/stage/slot/
actual side. It waives proximity for that whole Movement segment; it never waives cost or occupancy.

Enumerate actual adjacent free Clear destinations from current World. Use D2a `terrain_cost`,
`affected` and `cost`: terrain2 plus maximum Contact2/Engaged4, cumulative ordinary150%-CPA/I-CPA/
II-floor(CPA/2) ceiling, immediate incremental excess-CPA DP. Never sum memberships or refund old
overspend. Retained-II cannot move. A witness carries original unit/destination and computed costs;
assessment spends no resources or RNG. Canonical sorting makes witness choice irrelevant to identity.

This bounded surface requires exhausted own ammunition, which proves no supported next assault;
a potentially armed Combat surface is a capability fault, never a false no-continuation result.
Pre-assault/positive released-Reserve Combat and inherited movement capabilities require explicit
adapters/profile admission at D2c/004; no widened Combat capability is claimed here.

| Evidence | Accepted control |
| --- | --- |
| Release incomplete or unsupported/ambiguous capability | Reject before opening; never fake finish. |
| Clear with no legal witness | One system finish, no decision or deadline. |
| Witness but no material progress | One system finish, no wait. |
| Witness and progress | Open one pinned cycle-control deadline; owner repeat/finish. |

Repeat closes old ordinal and opens k+1 atomically, at the same slot/stage/turn Movement catalog
position. New identity uses result authority version and prefix **before** the repeat event. Extend
prefix only after serializing that event. Retain old closure receipt in state; reset only target-use
and new-cycle progress here. Full stage attack history, World, CP/Cohesion/TOE/ammo/BP, memberships,
Release history, RNG and future obligations persist. D2c owns complete disposed-control/route reset
composition; unresolved work can never be discarded as a reset.

Finish closes the cycle and advances once to the exact same-slot Truck Convoy catalog successor.
Expire unused pending next-Movement exceptions with this finish receipt while retaining the complete
release record and all stage/future obligations. Finish does not enter the opponent's phase or
claim execution of Truck Convoy/guard/calendar work. Checked ordinal/version/size overflow is a
contract fault before mutation, never a substituted no-continuation reason. Opening a two-event
owner decision requires room for both opening and closure; a single forced finish needs one version.

## Timing, identity and replay

Open/expire/unavailable/fallback-step require system; repeat/finish require actual cycle owner.
Check actor/control/cycle binding before retry lookup. Exact canonical command+actor retries return
the original receipt even after closure; changed stale commands reject. Old/early timers are no-ops.
Equality with deadline rejects owner input; regression/unavailability produces system finish.
Opening clock loss stores no fabricated Timing and allows only deterministic finish fallback.
Persist accepted high-water; no restart or owner command renews the budget.

Control ID is `ctl.` plus domain-separated SHA256 of canonical base bytes. Decision ID appends
`.decision`. Event names are `movement-combat-control-opened`, `movement-combat-cycle-repeated`,
`movement-combat-phase-finished`, prospective contractVersion1. Receipt is `cc.` plus hash of the
fixed canonical event fields before receiptId under the receipt domain in the inventory. Append
receipt-linked expiry/closure only after deriving it, avoiding self-hashing. Reuse C3a Receipt shape.
State reader replays the exact suffix from independently admitted base and compares all bytes.

Closed inventory is a typed canonical descriptor, not JSON Schema. ASCII UTF-8 JSON, mandatory
nulls, fixed field/array order; reject alternate bytes, duplicate/missing/extra keys, floats,
bool-as-int, unknown tags, truncation/BOM/newline. Limits:1MiB complete record, depth32,512 array
items,32 members, at most two control events. Full campaign Snapshot12 capacity remains D2c/009.
Private CMB-CYC-001 shape/size,002 primitive,003 unsupported capability,004 authority/base,
005 action/timing,006 replay/order,007 overflow/capacity,008 alternate bytes.

## Movement-completion projection

MovementExpiryInput is an internal adapter input at an independently accepted next Movement
completion, not a second completion command/event. Bind exact cycle/slot/ordinal, prior Movement-end
proof, complete final locations and the real completion receipt. Expire matching pending exceptions
once; retain already expired receipts; derive new ordinary exclusions by union of earlier exclusions
and own units beyond two hexes at completion. An exception does not erase exclusion history.
Projection does not alter World/CP or emit a second authority increment. Its output has a closed
canonical shape and fixtures; D2c's Movement successor must derive/authenticate the supplied
completion receipt, final World locations and predecessor proof. Wrong scope/ordinal, omitted units,
changed prior proof and a forged result fail readback against the trusted input.

## Traceability and next gates

Advances CYCLE-COMP-AC-001/004/006/007/009/010 and CON-002–004 for this private boundary. D2c still
owns full inherited sequence5 successors, first opening, actual Movement-end/positive Reserve World/
Snapshot integration and complete progress/continuation capability reconciliation. Task004 owns
public privacy/errors, AC map and capability admission; checkpoint B and runtime remain gated.
No new policy or independent-review round; review9of9 remains exhausted.

## Author verification

`python3 docs/specs/verify-combat-cycle-control-v1.py` passes19 mandatory literal cases across64
side/slot traces,164 replay cuts,1748 event/state/base mutations,700 malformed-byte checks,43
timing/authority/expiry/boundary checks and216 exact cost coordinates. Both actual sides and relative
slots are exercised; settled C3c cases retain their certified first-slot limit. Witnesses include
both destinations when a retreat vacates the opposing hex. Zero-loss Engaged yields11CP/1DP,
Contact yields9CP, and exhausted-ammo attacker custody history still permits12CP/2DP movement.
No-progress, no-continuation, persistent exclusion, I/II cumulative limits and expiry after finish
are literal assertions. Every repeated trace also checks Movement-completion projection.

The initial assessment stub failed before implementation. The first source adapter probe caught
the distinction between commitment ID and event receipt; final tests require both identities.
Regression hashes were recorded only after literal checks passed. Normal execution requires all19
cases, nine pinned predecessor file hashes,64 base hashes,164 state hashes/byte lengths,100 event
hashes and Movement-expiry output hashes. These remain single-implementation regression evidence,
not independent serializers or actual inherited campaign history.

A focused boundary RED test reproduced an opening at the penultimate authority version that left
no version for closure. Opening now requires two available versions before mutation; forced finish
requires one. Forged deadline, witness cost and Reserve ceiling state also fail suffix readback.

All15 predecessor/research oracles pass unchanged, including complete Snapshot12 composition.
Python syntax, JSON inventory/fixture parsing, links/anchors and stable25-task/72-AC/8-policy checks
pass. No production source/test/scenario or pre-existing frozen spec bytes changed. No .NET or
simulator run is claimed for this Python/documentation-only slice.
