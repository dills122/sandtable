# Combat ordinary Movement and break-off receipts

Status: CMB-TASK-003D2a complete as a bounded contract checkpoint at input `3ac1283`. Contract experiment only. Governing
[plan](../design/combat-cycle-implementation-plan.md), [cycle design](../design/continual-cycle-reserve-composition-v1.md)
and [accepted source facts](../research/combat-source-freeze-v1.md#ordinary-break-off-precedence-and-limits).
[Schema](combat-ordinary-movement-v1.schema.json), [fixture](fixtures/combat-ordinary-movement-v1.json),
[oracle](verify-combat-ordinary-movement-v1.py). No new gameplay ruling or independent review round.

## Scope and trusted boundary

Freeze an atomic ordinary move for selected Content7 independent nonmotorized CPA10 infantry,
normal Weather, featureless Clear neighbor, no positive ZOC, no open Reaction/Breakdown, no vehicle
lots and no pending immediate settlement. Current Reserve status must be None. Existing guard,
custody, delayed replacement, attack and resource evidence persists. This packet does not move
guards or prisoners, discharge future obligations, supply ammunition or grant repeat permission.

MovementBase carries exact World7/RNG, D1 cycle authority, materialized Movement position,
configuration/creation bindings via that authority, state version/prefix and stage attack history.
It also binds the complete settled C3c state and CA completion receipt from which fixture Worlds
are derived. A settled World is not permission to enter Movement. The caller must independently
validate actual release/repeat, phase-local proximity, movement-ended and control evidence before
runtime use. D2b/c freeze those requirements; this experiment deliberately cannot certify them.

Fixture boundary profile is `isolated-next-movement`: C3c replay supplies settled World and RNG;
a labeled synthetic prefix/version represents the unimplemented release/repeat gap, then ordinal2
and the same relative slot's Movement position are constructed. This is not an emitted repeat event
or a full campaign trace. No reader accepts a caller's replacement base merely because its hash
matches itself: readback requires comparison with separately replay-derived trusted fixture inputs.
No initial-World reset is applied to current TOE, CP, Cohesion, ammunition or obligations.

## Atomic move and membership semantics

At a trusted admitted boundary, a side-authenticated command binds cycle ID, Movement position,
expected prior version, original UnitKey, origin and destination. Require a live independently
represented own unit, exact origin, actual adjacent featureless Clear destination, no occupancy by
another element or guard, and a current compatible ledger. Selected singleton opposing infantry
has no positive ZOC; empty ZOC is a profile proof, not a caller flag. Wider control/topology/weather,
stacked participation, fractional costs and Reserve-history movement need their own adapters/proofs.

Derive active affected relationships from original unit membership, not destination proximity or
current display-counter identity. A moving bound endpoint leaves each such membership. Each pair
record becomes inactive, with its immutable creation receipt/endpoints retained and a move receipt
and `ordinary-break-off` ending cause appended. Last-counterpart departure therefore leaves the
other endpoint with no active pair. An unrelated pair remains unchanged even if nearby; a later
arrival does not inherit the old pair. Multi-counterpart/pair-kernel probes verify preservation and
cost precedence but do not admit multi-unit Combat or stacking into the selected runtime profile.

Compute terrain cost2 plus the **maximum** applicable active break-off cost: Contact2, Engaged4,
none0. Never sum Contact+Engaged or charge once per counterpart. Inactive records add no cost.
The ordinary cumulative voluntary ceiling is floor(3×CPA/2),15 for this profile. Charge cumulative
spent+cost, never a renewed allowance. For integral values, incremental excess-CPA DP is
`max(0, afterCp-CPA) - max(0, beforeCp-CPA)`; deduct it from current Cohesion immediately, including
previous mandatory overspend without charging old excess again. No post-move Cohesion reset.

Literal map examples include spent5+Engaged4+Clear2=11 with one DP; spent6 reaches12 with
two DP; spent9 reaches15 with five DP; spent10 plus6 rejects. The original Map A chart8.37
confirms Clear2, matching the unchanged current Movement artifact. This corrects the earlier
[Clear1 example](../research/combat-source-freeze-v1.md#d2-terrain-example-correction-2026-09-07). Generic1-CP route research-kernel probes also retain
released I ceilingCPA and II floor(CPA/2), including CPA9/spent3/II leaving only1. Those arithmetic
checks are **not** released-unit history or exception admission; complete event execution rejects
Reserve-history variants untilD2b/c bind their provenance and next-Movement rights.

One event atomically moves the element and its unique map representation, updates CP/Cohesion,
appends one positive DP Cause if needed and ends exact affected pairs. It preserves current TOE,
ammunition, readiness, parents, BP/movement-ended values, historical settlement payloads, guards,
lots, entitlements, future obligations, RNG and full active-stage attack history. Track the ordered
route for the moved UnitKey; this local trace is not a replacement for future movement-completion
and 8.23 eligibility evidence. This slice has no generic advance or finish command.

## Version, byte and receipt contracts

The inventory is a closed typed descriptor with canonical property order, not JSON Schema.
Prospective event `combat-cycle-element-moved`, contractVersion1, has its own exact envelope and
MovementEffect. It does not widen or reinterpret historical ElementMoved3 or its dispatch entry.
D2c/Task008/018 must reconcile the complete sequence5 movement successor and adapter before
registration; this family is not automatically wire-compatible with any existing event type.

`MovementEvent` binds author, campaign, full Rules10/configuration hashes, cycle/position, base hash,
prior/result versions, prior prefix, exact input and computed effect. Effect retains original unit,
representation, origin/destination, all cost/DP before/after values and sorted ended-membership
proofs. Receipt ID is `mov.` plus lowercase hex SHA256 of ASCII
`sandtable.combat.ordinary-movement-receipt.v1`, zero byte, then canonical event fields before the
receiptId field is appended. World relationship/Cause links use that ID; the event does not contain
its resulting World hash, avoiding a circular binding. Cause ID appends `.dp` to the move receipt.

Extend D1 Chronicle prefix with the full canonical event after receiptId. Append the existing C3a
command Receipt shape with command hash, full event hash, receipt ID, authenticated actor and new
version. Exact command+actor retry returns the retained receipt without a new event or debit,
including after later accepted moves; check owner/cycle binding before that lookup. A distinct
stale command fails. Receipt lookup is scoped by this base and cycle, never by version alone.

MovementState contains baseHash, current version/prefix, full current World/RNG/history, ordered
per-unit route tracks and ordered command receipts. Reader replays exact input/event suffixes from
trusted base and compares every canonical state byte; omitted, reordered, duplicated or changed
suffixes fail. This is private authority-fragment readback, not a full Snapshot12 restore adapter.

Bytes are compact ASCII UTF-8 JSON, mandatory nullable fields, fixed inventory field/array order,
no BOM/newline/duplicate/unknown/missing key, floats, alternate number spelling or escapes. Arrays
preserve admitted predecessor order; affected memberships sort by relationId, tracks by original
UnitKey, receipts by authority version, routes by actual traversal. Limits:1MiB per complete state
or event, depth32,512 array values,32 moves per probe. Reject overflow/capacity before mutation;
never truncate history or invent a finish. D2c/009 still owe whole-envelope campaign capacity.

Private errors CMB-MOV-001 shape/size,002 primitive bounds,003 unsupported version/profile,
004 authority/trusted base,005 illegal movement/cost,006 causal replay mismatch,007 resource/overflow,
008 alternate canonical bytes. Their successful use is not public error-contract evidence.

## Traceability and remaining work

This packet advances parent CON-002–004 and CYCLE-COMP-AC-007/009/010: exact break-off arithmetic,
affected-pair atomicity and retained state/readback. D2b still owes Reserve history/choice clocks,
release completion, continuation/progress, repeat/finish and exception expiry. D2c still owes actual
first opening, inherited sequence5 successor declarations, noninitial snapshot arms, unsupported
history rejection and all cross-contract reconciliation before parent003 closes. Task018 implements
ordinary movement;019 must use the same rule for continuation;004 owns public/evidence contracts.
No claim of complete AC coverage, gameplay simulation, production parity or independent review.


## Author verification

`python3 docs/specs/verify-combat-ordinary-movement-v1.py` passes eight literal cases mirrored across
both sides:36 initial/event state cuts,486 event/state/input mutations and120 malformed byte records.
It also checks630 arithmetic coordinates against the retained source arithmetic with explicit
terrain1/2, pair-membership preservation and14 atomicity/overflow/authority guards. Failed moves
leave prior state unchanged; exact old-command retry after a later move returns its original receipt.

Cases cover Contact/Engaged, second movement after break-off, immediate DP at11/12/15 CP, rejected
spend beyond15, retained victory Cohesion, guard/feeding obligations, escape entitlements and prior
mandatory expenditure. Kernel probes cover mixed/multiple counterparts, last-counterpart departure,
unbound arrivals and cumulative Reserve ceilings without claiming those broader histories as
admitted event traces. Missing/reordered suffixes and a modified cost with recomputed receipt fail.

Fixture pins16 base hashes,20 canonical event strings and16 final state byte lengths/hashes.
These regression vectors were recorded after literal behavior assertions; ordinary verification
requires them and never regenerates them. Source hashes pin C3c inputs, World/source arithmetic,
Content7 and the current Movement table; source scan bytes stay outside Git. All twelve predecessor/source/RNG oracles pass, including full Snapshot12 composition. C3c/earlier
contract bytes and all runtime source/tests/scenarios remain unchanged. No .NET tests ran for this Python/
documentation-only packet. Full noninitial Snapshot12 and actual campaign-entry proof remainD2c.
