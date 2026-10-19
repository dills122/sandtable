# Private P01 ordinal-two selected assault through RBA v1

**Status:** bounded private reference only. The [schema](combat-p01-selected-assault-v1.schema.json),
[reference](verify-combat-p01-selected-assault-v1.py), and
[tests](test-combat-p01-selected-assault-v1.py) do not admit P01, expose an
action, resolve Force Assignment, draw a result, or activate a native/hosted
consumer. The frozen nonadjacent [explicit-RBA v1](combat-p01-explicit-rba-v1.md)
remains unchanged. This is a separate successor for the adjacent armed subset.

## Evidence and choice

The reference replays the original CreationRequest, ten separate predecessor
input ledgers and the R03 Breakdown input ledger. It derives its candidate
only from that authenticated Control, selected content hash, original-unit and
representation membership, current target-use absence, exact adjacency,
Normal Weather event receipt/hash, and current World. The chosen target is the
single opposing battalion's occupied hex. Both infantry components have
maximum and current TOE 10, full ammunition, clear terrain, no current or
prior attack commitment and no settlement. The candidate binds original
UnitKeys, target, World/RNG hashes, weather evidence and predecessor receipt.
It also checks defender CP headroom and at least one clear unoccupied retreat
edge without authorizing movement.
The full pinned original 300-row catalogue has 44 such adjacent armed rows
and 256 candidate-absent rows. This count is **not** an action-exhaustiveness
or P01 support certificate.

Land §11 permits the adjacent phasing assault. Land §13 permits defender
Retreat Before Assault after Barrage. Accepted CMB-POL-001 bounds this private
exercise to defender decline; actual movement remains unsupported. Under
CMB-POL-004, timeout or trusted clock unavailability cancels the selected
intent, without inventing a decline. Voluntary surrender is excluded by
CMB-POL-009; automatic surrender must still fail closed at an actual trigger.
This slice has no result or settlement trigger.

The authority layer supplies trusted actor identity, participant, clock
availability and admitted timestamp. These are not client-selected claims.
System opens a selection window with a fixed decision ID and deadline.
Only the acting original side may select the exact derived target and
attacker participant before that deadline. Position Determination and
Barrage then complete as no-gun/no-barrage work. System may open the
defender-owned RBA window only after the Barrage receipt. Only the opposing
original side and exact defender participant may decline before its deadline.
An accepted decline completes the RBA step and stops at the **same slot's
Force Assignment** position. Force Assignment and result adjudication remain
open.

System may instead cancel after an expired or unavailable RBA decision,
or before opening when trusted clock is unavailable. Cancellation has a
different event/receipt and no decline receipt. It converts subsequent
structural steps to the no-attack path, ending at the same slot's Reserve
Release without committing, paying, drawing or settling an assault.
No timeout becomes a defender decision. No movement command is accepted.

Every input binds cycle, prior version/prefix, current position and previous
receipt. Selection also binds decision ID, exact target and attacker
participant; decline binds defender decision and participant. Source carries
canonical original predecessor bytes and canonical own events, while own
input ledger remains separate. Replay derives each event anew and requires
byte equality. Identical input retry returns its receipt without a new event.
Each event increments version/prefix once. World, RNG and resources remain
equal to the authenticated predecessor at every cut. All states retain
admitted=false and publicActions=[].

## Remaining gates

This does not prove complete source-legal action enumeration, actual RBA
movement, selected-assault Force Assignment/result/settlement, automatic
surrender at a trigger, approved outward owner mapping, equal-authorized
hidden-history privacy, all-row cuts/restart/readback or full resource caps.
It cannot license P02/P03, public Maproom submission or a playable Combat
loop.

## Local verification

    python3 -B docs/specs/test-combat-p01-selected-assault-v1.py
    python3 -B docs/specs/verify-combat-p01-selected-assault-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
