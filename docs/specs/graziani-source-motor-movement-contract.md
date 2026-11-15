# Source motor movement: event requirements

This contract is a design for the missing source-runtime successor; it does not admit
an existing source setup as a game in progress. A source movement is admissible only
from a replay-derived operation-stage position, an owning side's legal opening, a
complete source-backed map and occupancy view for the adjudicated route, and retained
transport and supply state. Unknown route, opposing control, unit characteristics,
stock or phase means that the move cannot be adjudicated.

The event binds the predecessor checkpoint and source-content identities, acting
side and unit, origin and destination, operation turn and stage, and before/after
state for movement, supply and the paid-stage ledger. All facts are re-derived from
retained authority on replay; commands do not provide a clock, world, path legality,
fuel quantity or opponent state. Pending arrivals and all unaffected units, loads,
stocks and random cursor survive the successor unchanged. Owner projections must
not reveal opposing stocks, source routes or identities.

Original Land sections 32.23–32.24 require motor fuel on a unit's first land move in
each operation stage and again at the instant its CPA is exceeded; one fuel point is
required for each battalion-equivalent infantry unit transported by motorization,
with company-equivalents exempt. The authoritative ledger must distinguish first
payment, the separate CPA-threshold payment, and subsequent moves in the same stage.
It must not derive a paid state from a caller-supplied turn number.

Section 32.16 requires a qualifying source within half CPA along the applicable
medium-truck cost path, except that nonmotorized infantry traces as infantry; an
enemy ZOC unoccupied by a friendly unit blocks the trace. A partial map or incomplete
enemy occupancy cannot prove either reachability or absence of a blocking ZOC.
Section 59.66B requires the unit's carried initial fuel to be consumed before a
supply unit is drawn on; that load cannot be transferred to a supply unit. Its
loaded points also retain the rule's detachment restriction. The event must debit
the actual selected source and carried load atomically, without regenerating initial
stocks from the pregame ledger on later moves.

The present source world has no replay-derived game clock, complete map/control,
general operational transport or mutable runtime stock. Existing setup cargo and
motor reservations are not evidence that a movement or supply trace is legal.

For a future real-state test, original Map D (maps PDF page 4, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`)
shows road edges `d3615`–`d3715`–`d3714`, where the scenario initially places
Commonwealth supply. The map key identifies the paired black lines as a road;
the terrain chart §8.37 charges a motorized unit half a CP per road hex.
A medium-truck trace over those two edges would therefore cost one CP before
checking current ZOC and any applicable restrictions. This is source geometry,
not proof that a live campaign has an eligible supply unit or an open route.
