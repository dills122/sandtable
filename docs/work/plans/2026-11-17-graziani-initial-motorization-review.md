# Axis initial motorization subtotal: independent source review

**Disposition:** The proposed **305 + 305 = 610** arithmetic matches the
explicit truck figures inspected in the original scenario, under §59.63's
Land-only conversion. Treat 610 as a **bounded subtotal**, not a certified
complete starting total. Individual assignment, locations, unlisted types,
other applicable setup provisions and corrections remain unverified. This is
an informed, separate visual reading of the same edition, not a blind check or
independent edition; no runtime admission follows.

## First-line row-by-row check

Scenario §60.31, printed p. 27 / PDF p. 3, lists the following explicit
medium and heavy truck points. A dash below means the type was **not listed in
that row**, not an independent finding that its quantity is zero. The computed
subtotal counts only printed numeric values. §59.63(A), printed/PDF p. 26,
converts each listed medium or heavy first-line truck point to one attached
motorization point, calculated hex by hex.

| Source row | Medium | Heavy | Listed medium + heavy |
| --- | ---: | ---: | ---: |
| C4218, 1 CCNN | 25 | 10 | 35 |
| C4120, 63 Cirene | 45 | 5 | 50 |
| C4020, 1 Libyan | 15 | — | 15 |
| C3920, 2 Libyan | 15 | — | 15 |
| C3919, Aresca | 5 | 5 | 10 |
| C3918, 62 Marmarica | 40 | 5 | 45 |
| C3617, Maletti | 30 | 5 | 35 |
| C4707, 64 Catanzaro | 30 | 5 | 35 |
| C4507, 4 CCNN | 25 | 10 | 35 |
| Bardia C4321 | 20 | — | 20 |
| Tobruk C4807 | 5 | — | 5 |
| Barce B5504 | 5 | — | 5 |
| **Listed subtotal** | **260** | **45** | **305** |

The near-C0716 detachment lists five **light** trucks, not five medium; it
adds zero to this *listed* medium/heavy subtotal. Other §60.31 rows without
explicit truck figures cannot by themselves prove complete absence. The source
also lists light points at C4218, C4020, C3920, C3617 and C4507; §59.63(A)'s
conversion names medium and heavy, so these light figures do not contribute to
this calculation. §59.42 requires initial first-line trucks to be assigned to
units in their listed hex; the row subtotal does not decide the owner's valid
allocation among those units.

## Separate pools and initial versus incoming

Scenario §60.33, same printed page, separately lists second/third-line pools:
Tripoli 140 medium + 40 heavy = **180**, and anywhere in Libya 100 medium +
25 heavy = **125**. Under §59.63(B)'s same conversion these supply **305**
unattached points before owner placement or load choices. They are distinct
from the above §60.31 first-line rows; summing them once gives 610. Do not
also retain or add their source trucks as a second motorization inventory.

The same §60.33 table lists 10 light and 50 medium for any air facility.
§59.63(B) says ignore trucks available at air facilities when the Air Game is
abstracted. The 50 medium and 10 light are thus excluded in this Land-only
calculation; the light points in the Tripoli and Libya rows are excluded by
the medium/heavy conversion as well. Do not infer that the listed 610 proves
all historical pools, omissions or corrections have been exhausted.

Scenario §60.92(A), printed p. 28 / PDF p. 4, gives the players' **ability to
bring in** motorization points according to their ability to bring in medium
truck points, one-for-one. This is distinct from §59.63(A)'s initial
medium-and-heavy conversion. The inspected text does not instruct deletion of
initial heavy-derived points, establish any actual arriving point or specify
all incoming eligibility/timing. Reconcile later arrival rules independently;
never apply both an initial setup conversion and a later receipt to the same
source item.

## Optional loads and extra supply

§59.66(A) permits initial motorization to be loaded with supplies beyond
scenario dump quantities. §59.66(B) qualifies attached points: those not
initially transporting men or guns may start with one ammo or three fuel
points, with consumption and detachment restrictions. Loading does not create
a new motorization point.

§59.66(C) permits each group of 30 **other unattached** motorization points
in a hex to start carrying one additional regular supply unit. Its example
uses only complete groups, and it expressly counts Tripoli/Tunisia boxes as
hexes for this case. The separate **air-facility** case allows 50 points for a
depot supply unit **or 30 for a regular** unit; it is excluded here when Air
is abstracted. The candidate's shorthand “different 50-point rule for air
facility points” should not be read as denying the distinct 30-point regular
option in a profile where air-facility points are present.

For the candidate's hypothetical lawful placement of 125 other unattached
points in one hex, four groups of 30 leave five; for a split 29 and 96, the
per-hex maxima are zero and three. A 60/65 split yields two and two. These are
conditional arithmetic results, not evidence that a particular distribution,
stacking, eligible recipient or initial extra unit is legal or selected.
§59.65's unavoidable setup overstacking rule and other location restrictions
still apply. A source example of an 80-point air facility group and 100 other
points supports separate group accounting but does not authorize pooling
remainders across hexes or optional categories.

## Admission vectors

- Recalculate all 12 listed first-line rows individually, retaining unknown
  type fields, and confirm C4507 `25 medium + 10 heavy → 35`; compare §59.63(A)'s
  own 4 CCNN example. Sum once to 305 attached, plus 180 Tripoli and 125
  Libya unattached; add no air-facility, light or duplicate truck pool.
- Keep 610 explicitly labeled subtotal until setup source coverage, scenario
  overrides, errata, owner allocations and any unlisted quantities are
  accepted. Snapshot each source row, type, resulting points and owner choice.
- Test 29/30/59/60 and split 29+96 as other unattached points, plus a Tripoli
  box treated as a hex; require eligible locations, optional selection and
  stacking decisions before creating a supply unit. Keep air-facility 30/50
  rules conditional on a profile where those points actually exist.
- Distinguish attached points carrying men/guns from points eligible for
  §59.66(B)'s initial load, enforce its separate use and detachment rules,
  and prevent a point from supplying two 30-point groups or being recreated
  on replay. Incoming medium-based ability is not itself a delivery.

## Sources and limits

Visually inspected hash-matched originals:

- [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  `/private/tmp/sandtable-scenarios.pdf`, PDF pp. 2–4 / printed pp. 26–28,
  §§59.42–.44, 59.63, 59.65–.66, 60.31, 60.33, 60.92(A), SHA-256
  `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  `/private/tmp/sandtable-land-rules.pdf`, PDF/printed pp. 43–44, §§32.0,
  32.32, 32.51–.52, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.

Did not exhaust other scenario rows, errata, chart/formation omissions, specific
legal allocations, or additional supplies and incoming schedules. The sums
check printed numbers only and do not certify complete setup. No gameplay tests
were run; no runtime or existing content changed.
