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

The source-turn authority has a replay-derived clock and a bounded movement
successor with mutable supply, but source setup cannot yet start that clock:
map/control, general operational transport and other readiness requirements
remain incomplete. Setup cargo and motor reservations alone are not evidence
that a movement or supply trace is legal.

For a future real-state test, original Map D (maps PDF page 4, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`)
shows road edges `d3615`–`d3715`–`d3714`, where the scenario initially places
Commonwealth supply. The map key identifies the paired black lines as a road;
the terrain chart §8.37 charges a motorized unit half a CP per road hex.
A medium-truck trace over those two edges would therefore cost one CP before
checking current ZOC and any applicable restrictions. This is source geometry,
not proof that a live campaign has an eligible supply unit or an open route.

For contact projection, the Air/Logistics Land Distance Chart §37.42 says
equally numbered hexes in adjacent sections are 33 hexes apart. This slice
uses that statement only for C3615/D3615, C3714/D3714 and C3715/D3715,
whose cells are independently visible on the source maps. Separately, the chart
prints 66 hexes from Derna B5925 to
Matruh D3714. The original Map D shows the D3714–D3715–D3615 path, at most
two hex steps, so the triangle inequality proves those three cells are more
than one hex from B5925. These are distance proofs, not seam-edge or legal movement proofs.
The chart also puts Tobruk C4807 45 hexes from Matruh D3714; Map C shows
the C4407–C4807 run, at most five steps using the conservative coordinate
bound. Subtracting that and the two-step Map D path still leaves more than
one hex. An anchor whose printed distance conflicts with the candidate is
not used. No unlisted cross-section coordinate is treated as an alias.

The one supported opening candidate is narrower than general movement. Scenario
§60.31 places Axis land units initially on maps A–C, in off-map boxes, or in
Libya. The original Map C shows the Libya/Egypt boundary and the C/D join;
Map D shows D3615, D3715 and D3714 well inside Egypt. The source transition
requires the Commonwealth first acting slot of GT1 Op1 and the exact retained
initial grouped and battlefield hashes, excluding any prior land-world
successor. GT1 Axis naval arrivals do not put Axis combat counters in these
hexes. However, section identity and cross-map separation remain unresolved;
the route cannot yet be certified free of an Axis ZOC from those facts. The
player-facing offer and execution stay blocked until a source-backed seam
certificate and contact projection make both legality and acceptance safe.

For this intact fourth-Indian roster, the motorized infantry's six assigned
abstract points and the towed artillery require one fuel point each under
§32.24. Empty attached motorization points require none. Land §32.55 exempts
abstract motorization points from breakdown; §21.0 excludes the vehicles
assigned abstractly to gun units, and §21.11 identifies the remaining
breakdown-eligible vehicle classes. The bounded roster contains no tank,
armored reconnaissance or self-propelled gun cohort. Other changes to that
roster or transport/load state fall outside this supported transition.

The opening transition requires all six horse motor points. A zero-point
allocation would leave enough of this formation's pool unused to motorize that
historically motorized unit, so it must not be offered as an opening move while
the Land §32.54 priority is unresolved. Partial assignments remain unsupported.
