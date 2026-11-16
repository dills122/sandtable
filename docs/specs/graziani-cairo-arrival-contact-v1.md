# Source-backed Polish arrival contacts

An accepted Polish brigade arrival can create adjacent-presence contacts for
both players. Source-turn replay derives those contacts from the accepted
before/after grouped world and retains the pre-existing contact journal.
Arrival alone must not advance the contact authority without this projection.

The bounded geometry is the six map-E neighbors of Cairo E1730, E1829, E1830,
E1930 and E1931. The original map scan (PDF page 5, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`)
shows these city hexes and the hex grid. The important-locations chart p.26
independently names all five Cairo coordinates. The geometry determines contact
adjacency only: it does not adjudicate terrain, rivers, stacking or movement.
Some map numbers are obscured by the city symbols and Nile. Their coordinates
and neighbor lists are inferred from the printed grid and visible neighboring
labels; a second inspection corroborated grid parity and E1931's neighbors,
but not a direct reading of every obscured label. These source readings remain
provisional until a complete independently checked map is admitted.

Contacts follow `sandtable.visibility.adjacent-contact.v1`: each observer
formation sees the opposing effective counter's presence and location, scoped
to the observer; hidden unit IDs remain private. Grouped counters are projected
once, rather than as their retained World8 members. Their grouped-world ID is
the private representation and observer identity. A detached member has its own
private representation ID `source-detached/{groupId}/{elementId}`. The former
attachment does not prove its assigned parent (Land 19.13); an adjacent Axis
detached counter therefore blocks projection until its observer identity is
established. Detached counters block unless their separation is established;
unknown cross-section identity cannot prove they are distant. Promotion to World8 must
preserve or explicitly reconcile that identity before being supported during a turn.
Independent World8 counters require their exact source-content parent formation;
an unavailable adjacent observer identity fails closed. An event with no adjacent opponents still advances the journal
without deleting any previous contact. Source start, full map topology,
initial contacts, movement contacts and wider contact support remain separate
readiness requirements. In particular, a non-Map-E opposing counter is not
assumed to be outside Cairo's neighborhood: unresolved section identity makes
the arrival contact projection fail closed for either side. An exact pair of
named raw coordinates in the original §37.42 distance chart may establish
nonadjacency when its printed shortest distance exceeds one; a provisional
near-seam candidate or a disagreement with that chart instead forces unknown.
No candidate coordinate alias or unlisted pair is treated as proof.
The chart measures shortest distance in hexes, including paths over sea;
the contact rule requires the same or an adjacent hex. Thus an exact named
pair with a distance above one cannot produce an adjacent-presence contact.
This says nothing about terrain, land reachability, ownership, or any other
coordinate pair. For the small set of chart-anchored triangle bounds used by
movement contacts, including the verified in-section paths and 33/45/66-hex
anchors, see [source motor movement](graziani-source-motor-movement-contract.md).
The same projection also uses the original chart's 103-hex C4807–E1829
distance for the individually listed Map C source positions in
`CampaignSourceInitialContacts.TobrukPathSources`. Their on-sheet grid paths
to C4807 are bounded by `abs(row − 48) + abs(column − 7) + 1` steps; the
verified Cairo graph reaches the other four entry hexes from E1829 in at most
two steps. The resulting lower bounds exceed one. A second source reader
checked the chart, each listed Map C location/path and the Cairo neighbor graph.
Candidate-near or a conflict at the pair or chart anchor still forces unknown.
This proves nonadjacency only and does not certify section aliases, terrain,
control or legal land routes.
For any named coordinate with a printed chart distance to E1829, the same
Cairo graph gives an upper bound of two more hexes to the other entry cells.
The original chart lists Mechili B4921–E1829 as 123 hexes; an independent
reader verified this pair, so the bound to every Cairo entry is at least 121.
No B/C alias is needed. The four C4307/C4308/C4407/C4607 artillery sites and
their in-section paths to C4807 were also independently checked on Map C.

At source-turn start, the initial-contact projector examines every effective
opposing counter pair for each supported observer. It uses the five Cairo
neighborhoods above, the existing pinned Map C neighborhoods at C3320 and
C3617, and the existing conservative same-section coordinate lower bound to
prove pairs are not adjacent. A near pair without a pinned neighborhood, an
unresolved cross-section pair, or an unknown observer identity prevents source
start; no missing contact is assumed absent. Both sides' observer-scoped
presence records are appended to the retained pregame contact journal at the
source-turn start version and must be re-derived on replay. This remains a
bounded start projector, not certification of the full map.

The grouped-source movement contact projector uses the same effective counter
identities, source-pinned geometry and observer-scoped knowledge. Given one
already-adjudicated moved representation, it derives new, updated, lost or
rediscovered adjacent-presence contacts for both sides, preserves stable
contact handles, and rejects unknown adjacency or unknown relevant observer
identity. It must be invoked and replayed by an actual source movement event;
the projector does not authorize movement or prove full-map coverage.

Independent active counters use a separate version 2 observer identity: the
actual source-backed representation ID, scoped to its owning side. Version 1
formation records retain their existing encoding and handles. Version 2 records
use `observerKind: independent-counter` and `observerId`; their handles and
queries are separate from formation observers. The authorized observation omits
private opposing representation IDs. Source reconstruction rederives journal
records and compares their canonical bytes. The activated Scheferzen garrison
and 2 Rifle Brigade supply a source-backed adjacent pair for focused projection
checks. The six activated v61 Axis garrisons likewise use their native
counter IDs as observers; activation alone supplies no adjacency evidence.
The source-active 1 RTR and 11 Hussars likewise use independent counter IDs.
The source-active Giarabub motor machine-gun counter is also an independent
observer; unknown motorization does not affect adjacency observation. Map C
establishes `c1014`–`c1015` as adjacent, without certifying movement terrain.
The Map C pairs with Scheferzen and Fort Maddalena, respectively, support
presence contacts in both directions; neither pair establishes a zone of control.
This does not certify complete initial visibility or make the source
turn playable: unresolved source readiness and geometry still block startup.
