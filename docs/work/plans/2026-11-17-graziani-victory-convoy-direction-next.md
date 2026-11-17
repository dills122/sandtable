# Victory convoy direction and physical movement

**Status:** source boundary for victory trace policy. Scenario wording gives
some routes an explicit direction, while the Land rules separately constrain
actual truck and Commonwealth land-unit movement. The checked sources do not
expressly identify a victory witness with a real dispatched convoy or waive
all physical constraints for a hypothetical route. Preserve that distinction
until a versioned decision is reviewed.

## Printed text and implications

* [Scenario §60.81](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
  printed p. 28 requires Italian strategic victory to take and hold either
  Alexandria or Cairo, any hex, with a combat unit that **can be supplied by
  convoy from any hex on Game-Map D**. Its decisive condition names Mersa
  Matruh and Siwa, with a combat unit that can be supplied by convoy **from
  Tobruk**. Tactical refers back to the Tobruk supply condition while
  distinguishing taking Sidi Birani and retaining three other places.
  “From” supplies directional evidence for those source-to-objective claims;
  a known reverse path is not automatically a forward witness where movement
  is directional. The row does not expressly require a specific available
  convoy, current cargo, fuel, or actual shipment event.
* Commonwealth tactical says take and hold Sollum, Halfaya Pass and Siwa,
  **tracing supply by truck convoy back to Alexandria or Cairo**; decisive
  and strategic say supply as above. “Tracing ... back to” gives textual
  evidence for an objective-to-source **trace**, distinct from physical
  transport toward an objective. Whether its directional passability follows
  trace direction, physical delivery direction, or another explicit rule is
  an interpretation. Tactical locations and later rows do not establish
  whether the same or separate unit/source must witness each objective.
* Land §8.98 says attached trucks move with their units in Movement and Combat
  and unattached trucks in Truck Convoy Phase; convoy points beginning and
  ending in the same hex and following the same path must move as a group.
  §8.33 requires connecting road/track hexes through a connecting road/track
  hexside to use the route benefit. §8.35/.42 give directed escarpment
  movement. These are evidence of real convoy movement and direction-sensitive
  terrain, not text specifying victory trace's time horizon or that a current
  convoy has actually travelled.
* §8.19 says no Commonwealth **Land unit may be moved** west of Marble Arch
  A2109 at any time, for any reason. This applies to actual unit movement as
  printed; whether a hypothetical victory trace or a hypothetical convoy
  witness inherits the restriction is a separate interpretive question. Do
  not extend it mechanically to a purely graph-theoretic path, or declare an
  actually moved qualifying Commonwealth land unit exempt. Scope and
  representation of abstract Motorization Points also need review.
* Land §32.16 describes a different, local supply draw: trace from combat
  unit to friendly Supply Unit within half CPA as medium-truck movement,
  except non-motorized infantry traces as infantry; it bars impassable terrain
  and enemy ZOCs unoccupied by friendly units. It does not explicitly replace
  scenario §60.81's convoy victory test or impose its half-CPA bound on that
  test. §32.51 treats Motorization Points as Medium Truck Points except as
  modified, but alone does not settle victory convoy representation.
* For a policy requiring an actual Supply Unit transported by Motorization
  Points, §32.33 allows the owner's Movement/Combat **or** Truck Convoy Phase,
  not both, and requires a friendly combat escort throughout Movement/Combat.
  §32.58 assigns CPA 15 to points transporting a Supply Unit. These are
  conditional physical-transport rules: neither establishes that every convoy
  carries a Supply Unit, that §60.81 requires an actual shipment, or that a
  hypothetical victory trace inherits the carrier's 15-CPA movement allowance.
  Reaction, fuel and continued escort identity need separate source review.

## Conditional policies and negative witnesses

For a finite synthetic directed network with legal endpoints and complete
coverage, compare (A) named trace-direction passability, (B) actual physical
delivery direction from source to objective, and (C) a source-to-objective
existence test requiring an actually available convoy under all movement,
transport and supply rules. Scenario language supports the stated “from” and
“back to” phrases; no checked case chooses all A/B/C details or resolves
capacity, weather, ZOC, route length, stock or event history. A directed edge
passable in one direction only distinguishes these policies: reverse-only is
not a source-to-objective witness; physical arrival cannot be presumed from
an objective-to-source trace. A blocked candidate does not prove failure
unless all eligible alternatives and boundary coverage are exhausted.

Bind objective, holding combat unit if required, subject hex, source and path
on one authoritative snapshot; distinguish zero-edge source/subject from a
source elsewhere. Treat those as project contract choices, not newly quoted
source rules. Keep unsupported terrain, side control, hidden occupation and
continuations unknown; project only authorized final observations and account
for timing of publication. No actual historical route or result is asserted.

## Evidence

Visually checked scenario PDF p. 4 / printed p. 28 §60.81,
SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 14–16, 43–44 §§8.19, 8.33, 8.35, 8.42, 8.98, 32.16, 32.33, 32.51, 32.58,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
No full §60.81 source hierarchy, errata, actual transport inventory or map
coverage audit. No runtime changes or tests.
