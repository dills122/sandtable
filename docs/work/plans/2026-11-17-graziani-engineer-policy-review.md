# Engineer defense proposal: independent source review

**Disposition:** retain `gra-engineer-defense-v1-proposed` as an unadmitted
candidate; revise the replacement-point predicate and HQ vector before
implementation. The selected stack hierarchy is a defensible project policy,
not a source-proven reconciliation. This review inspects the same edition and
scans as the proposal, not an independent edition or publisher clarification.

## Findings

1. **The chart-key check must preserve the replacement exception.** Step 4
   exempts separately identified replacement points from the proposed §3.4
   stack test, but step 5 then applies the chart's ammo-qualified blocker to
   “all relevant other units” without saying whether replacement points are
   excluded. The chart key itself says replacement points do not prevent use
   of the rating. Make the exception explicit in *both* predicates, while
   retaining the proposal's separate rule that a carrier or parent combat unit
   is not itself exempt. Otherwise an implementation that treats points as
   units with an unparenthesized rating and ammo can undo the stated exception.
   Test a separately represented replacement point under any representable
   ammo/rating combination, then the same point incorporated in or carried
   with a separately blocking combat unit. The choice to limit the exception to
   separately represented points, and its precedence over corrected §3.4,
   remain interpretations: the chart's sentence does not itself define that
   representation boundary or resolve later errata.

2. **Distinguish absent combat attachments from a bare HQ counter.** The
   vector “empty HQ representing an attached friendly combat unit” is
   ambiguous: §3.31 makes an HQ representing attached combat units a combat
   unit, while §3.33 says an HQ with **no combat units attached** is no longer
   a combat unit. If “empty” means no attachments, the proposed blocked result
   conflicts with §3.33; if it means the parent counter is alone on the map but
   represents an attached unit, say that explicitly. Test both a bare parent
   counter representing a combat attachment (blocked under §23.11) and an HQ
   with no combat attachments (not blocked by that clause alone, subject to
   its own rating and all other checks). Neither test establishes how
   non-combat attachments, directly assigned equipment or case-specific §3.23
   classifications affect the remaining predicates.

3. **Dash means not applicable, but neutrality remains a policy.** Axis chart
   p. 27 expressly defines `–` as **not applicable**; the candidate need not
   leave the symbol's meaning unknown for entries governed by this key. It
   correctly treats an unparsed/missing field as different from a verified
   dash. The additional choice that a different non-combat unit's dash is
   neutral under errata §3.4's “only parenthesized strengths” is not explicitly
   settled by that definition. Retain that limitation and a competing
   interpretation; do not equate a verified dash with numeric zero. Verify the
   legend governing the actual other unit before applying this Axis key.

4. **Ammo-qualified chart and broad erratum remain distinct.** The chart key
   blocks a rating when units in the hex have *both* ammo and an
   unparenthesized number for that rating; it explicitly exempts replacement
   points. Corrected §3.4 instead requires an attacked unit alone or with other
   units having only parenthesized strengths. The proposal chooses the latter
   as a cumulative, same-defensive-rating test, including an unparenthesized
   `0` without ammo as a blocker. That outcome is a policy choice, not a direct
   result of the chart key; its alternate chart-only interpretation remains
   viable pending review. Corrected §23.11 separately blocks an engineer
   stacked with a friendly combat unit regardless of ammo. Keep ammo and unit
   identity in the authoritative snapshot only when needed by applicable
   predicates, and do not claim that lack of ammo permits the engineer's own
   attack or defense. Given the current cumulative step 3, every non-exempt
   unparenthesized numeric defensive rating already blocks; the chart-ammo
   check is redundant for those cases, not independent evidence of eligibility.
   Add known and unknown ammo tests to verify known blockers dominate missing
   inputs and no redundant ammo check converts a fully determined result to
   `unsupported`.

5. **Attack scope and unsupported results need precise meanings.** The
   candidate properly confines this decision to a defensive close assault and
   leaves barrage, anti-armor and own ammunition requirements elsewhere. Its
   “no attack: not usable” vector is an eligibility answer only; no combat
   resolution should be fabricated. “Known disqualifier dominates unknown”
   applies to eligibility and does not authorize resolution when unrelated
   attack, loss or supply inputs are missing. Confirm authoritative attack
   event identity and represented stack at the same snapshot, distinguish a
   known different attack type from an unclassified event, and preserve no
   mutation on unsupported resolution. These are contract requirements, not
   additional source findings.

## Vector assessment

Engineer alone, with another friendly combat unit regardless of ammo, and
adjacency without an attack match the stated source conditions, subject to
ordinary combat prerequisites. The parenthesized non-combat, zero without
ammo, verified dash and separately represented replacement-point vectors
follow the **proposed interpretation**, not unambiguous printed results.
The unknown-input vector is sound only when a known blocker determines
eligibility; it must not bypass other unresolved combat steps. Recheck the
ambiguous HQ vector as above. Tests involving abstract point objects are
synthetic until point representation and legal stacks are defined.

## Source and scope

Visually inspected hash-matched [Axis chart](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF p. 27, row `bbb` and key (SHA-256
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`);
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 6 and 35, §§3.23, 3.31–.33, 3.4 and 23.11 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`);
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF pp. 1–2, §§3.4 and 23.11 (SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`).
The chart prints CPA 25, `0/(1)` and maximum TOE `(1)`; the latter is a
separate mark, not a starting-strength conclusion. Reviewed the
[candidate](2026-11-17-graziani-engineer-policy-next.md) and
[prior conditions review](2026-11-17-graziani-engineer-conditions-followup.md).
No full attack, ammo, attachment, other-unit chart or historical reachability
audit was performed; no runtime changes or gameplay tests.
