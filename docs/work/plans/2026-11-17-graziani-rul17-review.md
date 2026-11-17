# RUL-GRA-17: local supply range audit

**Disposition:** coherent provisional policy; do not present its inclusive
comparison or whole-CP ceiling as explicit §32.16 rules. The printed recce
example supports the ceiling only under an unnamed-unit CPA assumption. One
qualification concerns the 8-CPA example's classification, below. No gameplay
admission follows from this audit.

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 12, 43, §§6.14–.15, 32.16–.17, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
and [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 23 §4.46a, SHA-256
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`.
Read [earlier range review](2026-11-17-graziani-supply-range-rounding-review.md);
map/terrain chart and errata were not separately inspected for this pass. Same
edition, separate reading; no independent physical copy.

## Findings

* §32.16 says a friendly combat unit may draw on a Supply Unit **within
  one-half of that combat unit's CPA**. It prints 5 CP for non-motorized
  infantry, 10 for a heavy-weapons unit transported by Motorization Points,
  and 23 for typical recce. It does not expressly state whether equality is
  included, nor a general rounding rule. Inclusive comparison and
  `ceil(CPA/2)` are therefore explicit policy choices. The separate §32.17
  destruction case explicitly says round upward; its direction is supporting
  context, not an imported rule for §32.16.
* The chart indeed lists numerous recce codes with CPA 45 and one with CPA 35.
  If the unnamed typical recce example means a 45-CPA unit, exact half is 22.5
  while its printed 23 supports ceiling. The chart does not identify that
  example, and “typical” is not universal; case text and chart do not prove an
  official rounding rule for all units. Under this policy, 22.5 and 23 CP pass
  and 23.5 fails for CPA 45. Calculate actual path cost without rounding each
  edge: ceiling applies to the **range**, not to path segments or the sum.
* §32.16 explicitly measures **from the unit to the Supply Unit**, as if
  medium-truck movement, except non-motorized infantry uses infantry movement.
  Directed terrain can make a reverse calculation differ. The cited case does
  not itself impose an actual medium truck's 20-CPA movement cap on this
  hypothetical distance; such a cap would contradict the policy's 23-CP
  example if imposed universally. Other legal route restrictions still apply.
* The case excludes impassable terrain and enemy ZOCs **unoccupied by Friendly
  units**; it does not limit that wording to combat units. Exact origin and
  destination treatment, occupancy at endpoints and incomplete ZOC coverage
  require policy/source review. Do not equate unknown coverage with safe
  passage, or reveal private opposing positions through route diagnostics.
* §6.14 explicitly describes 1st Royal Northumberland Fusiliers as a
  non-motorized **machinegun battalion** with CPA 8, and §6.15 says a parent
  formation uses its lowest component CPA. Under the proposed actual-CPA
  policy, an applicable 8-CPA unit's inclusive range is 4, so 5 fails. The
  apparent tension with §32.16's generic non-motorized infantry example rests
  on classifying that machinegun battalion as infantry for the exception;
  §6.14 itself does not expressly use the word infantry. Verify unit-type
  classification separately before calling it a source-proven exception or
  assigning infantry trace costs to that specific counter. More generally,
  do not blindly set all units at the generic 5-CP example or use a parent CPA
  for an independently represented subordinate without resolving attachment.

## Boundary vectors

For fully known, legal hypothetical paths and an eligible friendly source:

| Accepted unit CPA | Exact trace cost | Inclusive ceiling policy | Exact-half alternative |
| --- | --- | --- | --- |
| 45 | 22.5 / 23 / 23.5 | pass / pass / fail | pass / fail / fail |
| 15 | 7.5 / 8 / 8.5 | pass / pass / fail | pass / fail / fail |
| 8 | 4 / 5 | pass / fail | pass / fail |

Also compare unit-to-source against reverse travel on a directional edge;
enemy ZOC with friendly occupation, without it and with unknown occupancy;
impassable edge; truck versus verified non-motorized-infantry trace; equal and
over-range paths; and parent composition changes. These are synthetic tests,
not assertions that any actual scenario route exists. Verify stock selection,
privacy and deterministic replay separately. No runtime change or test run.
