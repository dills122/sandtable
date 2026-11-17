# Half-CPA supply range: rounding source search

**Result:** §32.16's typical-recce example supports rounding a 45 CPA unit's half-range up to 23 CP, but does not state a general rule or explicitly describe the arithmetic. Exact half-CPA comparison gives 22.5. Since roads permit half-CP movement costs, the readings can produce different legal supply traces. Preserve both until a versioned interpretation is chosen.

## Evidence

| Source | Observation | Bound |
| --- | --- | --- |
| Land §32.16, printed p. 43 | A friendly Supply Unit must be within one-half the combat unit's CPA; trace costs are those of a medium truck, except non-motorized infantry traces as infantry. It gives examples: non-motorized infantry 5 CP, motorized heavy-weapons 10 CP, typical recce **23 CP**. | No explicit rounding instruction in §32.16. 5 and 10 do not distinguish rounding when their assumed CPAs are 10 and 20. |
| Commonwealth §4.46a, chart PDF p. 23 | Typical reconnaissance battalion-equivalent codes `hh` through `ss` list CPA **45**; code `tt` instead lists 35. | 45/2 = **22.5**, so the printed typical-recce 23 example is positive evidence for rounding up, not proof that every recce or other odd-CPA case has the same rule. The example does not explicitly identify a particular code. |
| Map terrain-effects chart §8.37, PDF p. 1, road row/note 6 | Motorized road travel costs **1/2 CP** per hex with stated hexside treatment. | Fractional trace costs are real source values. A route costing exactly 23 versus a 22.5 exact half is not merely a representation difference. Route construction and other terrain restrictions must be verified. |
| Land §32.17, same page | A separate destruction attempt spends one-half of all acting units' CPA, **rounding upwards**. | Nearby explicit upward rounding is supporting analogy, but applies to a different action and base. It is not a printed §32.16 cross-reference. |
| Land §21.31, p. 33; §32.65, p. 44 | Breakdown-column lookup rounds fractions up; abstract convoy loss has specified down/up rules by loss category. | Rounding is operation-specific in these examples; do not transplant a rounding instruction globally from either section. |
| Land glossary pp. 5–7 and September 1979 errata pp. 1–2 | Focused visual review found no general half-CPA supply-range rounding convention or explicit correction to §32.16. | This is a bounded source search, not proof no convention exists elsewhere. The errata §32.0 note characterizes abstract rules as untested. |

Section 32.16 still requires a **friendly** supply unit, legal trace, no impassable terrain and no enemy ZOC unoccupied by friendly units. CPA is the combat unit's CPA, while the route is costed as the specified tracing unit; parent-unit CPA may depend on its slowest attached unit (§3.32). Do not count hexes instead of CP or confuse the cost of actual movement with a certified supply trace.

## Candidate interpretations

1. **Exact fractional half:** compare exact rational path cost `≤ CPA / 2`. This follows literal half and does not add a printed rounding step, but differs from taking the 23 CP recce example as an exact permitted threshold.
2. **Ceiling to a whole CP:** compare `≤ ceil(CPA / 2)`. This matches a typical CPA 45 recce's example and the direction of nearby §32.17, but generalization remains a project choice. It should not be described as an explicit general printed rule.

The example does not prove whether “23” is an approximation, a ceiling applied to range, a ceiling applied to CPA or route cost, or a general convention. These choices differ when costs have halves; document precisely what is rounded and at what point. Neither one decides insufficient supply, source selection, path eligibility or dynamic CPA.

The [independent source review](2026-11-17-graziani-supply-range-rounding-review.md)
also compares the generic five-CP infantry example with §6.14's 1st Royal
Northumberland Fusiliers at CPA 8. Section 6.14 calls it a non-motorized
machine-gun battalion; its eligibility for the infantry-specific example needs
separate classification, as the [ruling audit](2026-11-17-graziani-rul17-review.md)
notes. The review confirms
the trace runs **from unit to supply**, and notes that corrected track costs can
require finer precision than half-CP units. Preserve direction and use exact
rational arithmetic unless all applicable cost denominators are established.

## Distinguishing vectors

- For an eligible 45-CPA typical recce with otherwise valid routes of 22.5, 23 and 23.5 CP, exact-half accepts only 22.5; ceiling-range accepts 22.5 and 23. Neither accepts 23.5. Use exact integer half-CP arithmetic, not binary floating-point or rounded individual edges.
- For a 15-CPA combat unit, compare legal 7.5 and 8 CP traces: exact half accepts only 7.5; ceiling range accepts both. A 20-CPA unit accepts a 10-CP trace and rejects a 10.5-CP trace under either reading.
- Independently reject a trace exceeding the selected range, through impassable terrain or unoccupied enemy ZOC, or to an enemy supply unit. Exercise non-motorized infantry's alternate trace cost and actual attachment-adjusted parent CPA; do not use an ordinary recce CPA for every recce code.
- Capture policy identity and unrounded source CPA, route cost and selected source in deterministic authoritative evidence. Visibility must not disclose hidden supply locations to an unauthorized opponent.

## Provenance and search limits

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF pp. 5–7, 33, 43–44, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf), PDF p. 23, SHA-256 `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`; original map/terrain chart PDF p. 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; September 1979 errata PDF pp. 1–2, SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Automated full-book OCR was unavailable on this host; focused visual search cannot exclude other clauses or later errata. No runtime changes or gameplay tests.
