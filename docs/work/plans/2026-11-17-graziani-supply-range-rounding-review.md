# Independent review: half-CPA supply range

**Disposition:** The proposed two readings and arithmetic are supported as
bounded alternatives. The 23-CP recce example is meaningful evidence for
rounding upward if its assumed recce CPA is 45; it is not an explicit general
rounding instruction. A possible additional example tension depends on
whether §6.14's eight-CPA machine-gun battalion qualifies for §32.16's
infantry-specific five-CP example; that classification is not yet established.

## Source checks

- Land §32.16, printed/PDF p. 43, requires a friendly Supply Unit within
  half the combat unit's CPA. It gives non-motorized infantry **5 CP**, a
  heavy-weapons unit transported by Motorization Points **10 CP**, and a
  typical recce unit **23 CP**. The 23 is the printed example, not a value
  independently computed by the review. It does not name a particular recce
  code or explicitly say how to round. Commonwealth chart §4.46a, PDF p. 23,
  gives recce battalion-equivalent codes hh, jj, kk, ll, mm, nn, pp, qq, rr,
  ss CPA **45**, and tt CPA **35**. Thus 45/2 = 22.5 and ceiling is 23;
  a 35-CPA recce does not support a universal 23 threshold. The inference
  depends on treating the unnamed typical recce as one of the 45-CPA cases.
- **Possible additional example tension:** §32.16 says a non-motorized infantry unit
  must be within 5 CP, while §6.14, p. 12, explicitly gives the initially
  non-motorized 1st Royal Northumberland Fusiliers CPA **8**. Literal half
  of that CPA is 4, not 5. Section 6.14 calls it a non-motorized machine-gun
  battalion, not explicitly infantry; whether the infantry-specific example
  applies to it needs separate classification. The five-CP statement fits an
  assumed CPA of 10, but this comparison alone does not prove an infantry
  conflict. See the [ruling audit](2026-11-17-graziani-rul17-review.md).
- §32.17, same page, explicitly rounds **upward** when paying half of all
  acting units' CPA for a distinct supply-destruction attempt. This is useful
  evidence that the author knew how to state upward rounding; it does not
  establish that §32.16 silently incorporates that instruction. The proposed
  exact half and whole-CP ceiling comparisons remain distinguishable source
  interpretations, not two independently printed rules.
- Chart §8.37 on original Map A, PDF p. 1, gives motorized road travel
  **1/2 CP**, with road note 6 governing hexside effects. §8.33 requires
  connecting road hexes through a connecting road hexside. Hence fractional
  costs can matter. A synthetic 46-edge connected road path would cost 23 CP
  under those stated assumptions, but no actual scenario path, road coverage,
  legal weather or complete movement certificate follows from that vector.
  Do not round each edge or infer that an actual medium truck must physically
  traverse it: §32.16 describes a cost calculation *as if* traversed by a
  medium truck, with infantry exception. The printed 23-CP recce example
  also cautions against imposing an unexplained 20-CPA truck movement cap on
  the hypothetical trace.
- Trace direction is **from the unit to the Supply Unit** in §32.16. Trace
  as medium truck except non-motorized infantry traces as infantry; it cannot
  pass through impassable terrain or enemy ZOCs unoccupied by friendly units.
  Preserve direction where directional terrain changes cost or passability,
  and do not infer that every route is reversible. Exact endpoint handling and
  what counts as friendly occupation at an enemy-ZOC endpoint need separate
  source policy; avoid assuming a reverse route proves this one.
- §3.32, p. 6, gives an HQ/parent the CPA of its slowest attached unit;
  §6.15, p. 12, refers to the lowest CPA among units comprising the parent
  formation. Determine the represented combat unit and actual attached
  composition before choosing the range. Do not automatically substitute a
  parent CPA for each separately represented subordinate, or assume a unit's
  chart CPA remains unchanged after motorization/attachment. §32.16 does not
  resolve all nested formation and expense-attribution questions.

## Boundary vectors

Assume valid friendly source, legal directional paths and no other restrictions;
these are synthetic costs, not map certificates. Using an inclusive comparison
is itself the proposed interpretation of “within.”

| CPA | Trace CP | Exact half | Ceiling of half |
| --- | --- | --- | --- |
| 45 | 22.5 | pass | pass |
| 45 | 23 | fail | pass |
| 45 | 23.5 | fail | fail |
| 15 | 7.5 / 8 | pass / fail | pass / pass |
| 20 | 10 / 10.5 | pass / fail | pass / fail |
| 8 | 4 / 5 | pass / fail | pass / fail |

The 8-CPA row exposes the generic five-CP example independently of rounding.
As a parent check, a valid parent comprised of 45- and 15-CPA units has CPA
15 under §§3.32/6.15; compare its range to the recce acting as a separately
represented unit only if the actual representation and attachment permit it.
Costs should use exact rational or half-CP integer arithmetic where all edges
are known to be halves; corrected track halving or other rules may require
finer precision, so do not assume the whole game always has half-CP granularity.

## Evidence and limits

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 6, 12, 14, 43 §§3.32, 6.14–.15, 8.33, 32.16–.17, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 23 §4.46a, `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`;
original map/terrain chart PDF p. 1,
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
September errata PDF pp. 1–2,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
This is a bounded same-edition review, not proof that no other convention or
later clarification exists. No runtime changes or gameplay tests.
