# Land-only supply expenditure: source audit

**Conclusion:** the [expenditure note](2026-11-17-graziani-supply-expenditure-next.md)
accurately gives the inspected amounts and preserves the major unresolved
accounting questions. Two implementation cautions below sharpen its wording;
neither establishes a different source amount. This is a separate visual pass
over the same edition, not evidence of a different edition or a gameplay test.

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed p. 43 §§32.15–.16, 32.21–.25, scan SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
The scenario, errata, characteristic charts and actual units were not
independently rechecked for this audit.

## Checked transcription

* §32.21 indeed sets non-phasing battalion-equivalent assault at **1 ammo**
  and barrage at **2**; phasing costs are twice the corresponding amounts,
  **2** and **4**. The source says to count individual battalion-equivalents,
  with a division expending by component counters. §32.22 says expenditure is
  at the instant of combat. Neither passage specifies an automatic aggregate
  parent cost or the combination for multiple activities.
* §32.23 distinguishes land from rail and sea, states the first applicable
  movement charge in each Operations Stage, allows combinations of halts and
  movement up to CPA, and requires additional fuel at the instant CPA is
  exceeded. It does not explicitly give a recurring formula for later CPA
  multiples or address rounding. The opening “basically” sentence is qualified
  by §32.24; it is not a universal charge on every land-moving unit.
* §32.24 prints tank battalion **2 fuel**, tank company **1**, and gun-class
  or recce-type battalion **1**, excluding camel reconnaissance. It also says
  Motorization Points transporting infantry units (not Replacement Points),
  gun-unit battalion-equivalents, or a real Supply Unit require **1 fuel**.
  Other company-equivalents and Motorization Points used for other purposes do
  not require fuel. The tank-company sentence and general company exception
  conflict if interpreted without context; honoring the explicit tank-company
  charge is the natural provisional reading, as the prior note does. The
  sentence does not settle whether each motorization point, each load, or some
  grouping is the billing unit. It also does not by itself settle whether a
  carried gun's own cost combines with its transport cost.
* §32.25 exempts patrolling from supply expenditure. No movement cost may be
  inferred merely because a patrol is declared.
* §32.16 gives the three checked ranges: non-motorized infantry **5 CP**, a
  heavy-weapons unit carried by Motorization Points **10 CP**, and typical
  recce **23 CP**. These are examples, not a universal rounding convention.
  It directs measuring from combat unit to friendly Supply Unit as medium
  truck movement, except non-motorized infantry uses infantry movement, and
  bars impassable terrain and enemy ZOCs unoccupied by friendly units. A trace
  is costed movement distance, not simply a number of hexes; the text says
  “Friendly units,” not specifically friendly combat units.

## Review cautions and vectors

1. The note's camel-recce and non-tank-company negative vector should be read
   as **no standalone charge solely under the listed §32.24 categories**, not
   proof that all associated motorization, other simultaneous units or other
   rules are free. Conversely, a real supply unit transported with qualifying
   motorization is specifically included; a dummy is not listed as real, but
   that distinction alone does not prove its complete treatment elsewhere.
2. “Additional Fuel Points” after exceeding CPA does not state in §32.23 that
   the additional payment is necessarily identical to the first charge, or
   that it recurs at every CPA multiple. Preserve those as distinct questions
   rather than encode a fixed repeat threshold or amount. Likewise, §32.16
   phrases draw permission in terms of a **combat unit**; do not assume that
   this alone resolves how a supply unit's own movement fuel selects a source.
3. Synthetic examples, assuming otherwise eligible combat and available valid
   stock: non-phasing assault 1 versus phasing 2; non-phasing barrage 2 versus
   phasing 4; tank battalion 2 versus tank company 1 for first qualifying land
   move. Compare halt/resume within CPA against crossing CPA; assert only an
   additional expense at the latter boundary until its amount is ruled.
4. Compare equal hex-distance paths with different movement CP, enemy ZOC with
   and without friendly occupancy, non-motorized infantry versus truck trace,
   and source just within versus beyond half CPA. Selection among sources,
   split payments, shortage, fractional boundaries and simultaneous debits
   remain unproven by the cited passages. Section 32.15 removes a Supply Unit
   only when both ammo and fuel are zero; preserve atomic resource accounting.

No code changed and no tests ran. Check the remaining scenario and errata
claims against their own cited sources before using this as full acceptance.
