# Commonwealth initial motorization: independent source review

**Disposition:** All 12 listed first-line rows and the four second/third-line
rows match the original scenario. The proposal's **147 attached + 115
unattached = 262** is correct as a bounded Land-only subtotal. The excluded
air-facility row contains 30 medium and 20 heavy, not an additional 50
admitted points. This check does not establish complete setup or actual owner
allocations. Same-edition informed rereading, not a separate edition or a
runtime admission.

## Independent row check

Scenario §60.41, printed p. 27 / PDF p. 3, lists first-line trucks by location;
§59.63(A), printed/PDF p. 26, converts each listed medium and heavy truck point
to one attached motorization point. `—` means that truck type is not listed in
the row, not a certified zero.

| Location | Light | Medium | Heavy | Medium + heavy |
| --- | ---: | ---: | ---: | ---: |
| C4131 | — | 10 | — | 10 |
| C3926 | — | 2 | — | 2 |
| C3922 | — | 15 | 5 | 20 |
| C3721 | 5 | 5 | — | 5 |
| C3520 | — | 5 | — | 5 |
| C3320 | — | 10 | — | 10 |
| C3020 | — | 3 | — | 3 |
| D3714 | 5 | 15 | 5 | 20 |
| D3612 | 5 | 5 | 5 | 10 |
| D3615 | 5 | 20 | 2 | 22 |
| E1829 | — | 20 | — | 20 |
| Alexandria E3613 and/or E3714 | 10 | 15 | 5 | 20 |
| **Printed numeric sums** | **30** | **125** | **22** | **147** |

The source prints the Alexandria locations as `E3613 &/or 3714`; the row's
15 medium and 5 heavy are counted **once** across the owner's legal allocation,
not replicated at each hex. The C3926 figure is two medium, and the D3615
heavy figure is two. §59.42 requires listed first-line trucks to be assigned
among units in their listed hex; numerical conversion alone does not resolve
which unit receives which point. Cairo/Helwan and `Anywhere on Map D or E`
unit rows do not themselves give truck quantities in this listing.

Scenario §60.43 on the same page gives the separate second/third-line rows:
Cairo `40 medium + 10 heavy = 50`, Alexandria `20 medium = 20`, and anywhere
maps `40 medium + 5 heavy = 45`. The total is **100 medium + 15 heavy = 115**.
Its air-facility row separately prints five light, 30 medium and 20 heavy;
§59.63(B) says ignore air-facility trucks when Air is abstracted. Neither
those 50 medium/heavy nor the light figures enter the Land-only subtotal.
§60.43's non-air row locations are constraints and owner allocations, not
multiple grants or proof of final legal placement. The two categories' source
rows are distinct, and each original truck point is converted once.

## Loading and arrival boundaries

§59.66(C) permits each complete group of 30 **other unattached** motorization
points in a hex to start carrying an additional regular supply unit. A
conditional 50-point group in one eligible Cairo hex supports one unit with 20
points ungrouped; an isolated 20-point Alexandria allocation supports none.
Combining points from distinct source pools only works after actual legal
co-location and allocation, and cannot reuse a point or pool fractions across
hexes. These are optional carried units, additional to scenario supply,
not newly created motorization points. The source explicitly counts
Tripoli/Tunisia boxes as hexes for §59.66(C); it does not give an analogous
blanket permission to aggregate different Cairo or Alexandria hexes.

The separate air-facility provision, applicable only where such points exist,
allows 50 per depot **or 30 per regular** supply unit; the ignored Land-only
air pool supplies neither. §59.66(B)'s attached-point loading applies only to
points not initially carrying men or guns and has additional consumption and
detachment rules. Existing §60.92(C) setup supplies, stacking, dummy markers
and extra optional carried units require separate reconciliation.

§60.92(A) describes **ability to bring in** one motorization point per medium
truck point. It does not replace §59.63's medium-plus-heavy initial conversion
or prove a later delivery. The 262 is the sum of the enumerated starting
figures only: no conclusion here that missing truck types are zero, all other
source/correction scope is exhausted, or every point has a legal allocation.

## Acceptance vectors

- Recalculate each printed row, including C3926 `2 medium`, D3615 `20 medium +
  2 heavy`, and Alexandria `15 + 5` once across both possible locations.
  Preserve not-listed fields; do not apply the calculation twice to the same
  source pool. Verify 147 + 115 = 262 and exclude 30 medium + 20 heavy at air
  facilities, plus all light trucks, in Land-only mode.
- Check 29/30/59/60 other unattached points in a legal single hex, split across
  different hexes, and explicit co-location from separate pools. Confirm
  optional versus declined extra units, no point reuse, attached-load
  restrictions and separately accepted stacking/dummy policies.
- Distinguish initial medium and heavy conversion from medium-based future
  ability, actual receipt and loss; check rule/version identity, duplicate
  import and replay without exposing opponent-private load details.

## Sources and limitations

Visually inspected hash-matched [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF pp. 2–4 / printed pp. 26–28, §§59.42, 59.63, 59.65–.66, 60.41, 60.43,
60.92(A), SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF pp. 43–44 §§32.0, 32.32, 32.51–.52, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Candidate was read before the source pass. No exhaustive errata, later edition,
actual formation allocation, all supply scenarios or historical reachability
check. No gameplay tests ran; no code, data or readiness flags changed.
