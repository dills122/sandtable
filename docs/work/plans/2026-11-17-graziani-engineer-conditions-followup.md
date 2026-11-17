# CCNN engineer parenthesized strength: source conditions

**Disposition:** Errata corrects the engineer-specific stack restriction and
clarifies the general attack condition. It does not explicitly reconcile every
mixed stack with the Axis chart's ammunition-qualified key. Preserve the
printed ratings and retain a ruling gate for contested cases; do not flatten
`(1)` to an unconditional `1` or treat missing ammo as automatic permission.
This is a source review, not an admitted runtime rule.

## Identified units and source text

Axis organization/arrival charts §4.44b, PDF p. 17, list **201st Engineer
Battalion** under 1st CCNN and **203rd Engineer Battalion** under 3rd CCNN;
PDF p. 18 lists **204th Engineer Battalion** under 4th CCNN. Each row has
counter abbreviation `201`, `203`, or `204`, respectively, ID code `bbb`,
TOE `N`, and arrival `D`. The 203rd is labeled an independent battalion in
the chart note; don't assume all three share a parent or placement. Axis
Italian characteristics §4.46b, PDF p. 27, `bbb` row gives CPA 25,
close-assault offensive/defensive `0/(1)`, and maximum TOE `(1)`. Parentheses
on maximum TOE and defensive rating are separate source marks. No conclusion
about current strength, loss handling, or placement follows solely from them.

| Source | Distinct condition or constraint |
| --- | --- |
| Land §3.4, printed p. 6; September errata p. 1 §3.4 | Corrected general wording permits parenthesized strengths only when the unit is attacked alone in a hex or with other units having **only parenthesized strengths**. Attack is a condition; simple enemy adjacency is not proof of an attack. |
| Land §23.11, printed p. 35; September errata p. 2 §23.11 | The erratum corrects “stacked with” to **not stacked with a friendly combat unit** for engineers' parenthesized strengths. It separately permits entry into friendly-occupied, enemy-controlled hexes. The original calls engineer counters non-combat and gives them no ZOC. |
| Land §3.23, printed p. 6 | Some non-combat units can count as combat units for particular determinations. It does not by itself specify how every parent, attachment, replacement point, or mixed stack counts for §23.11. |
| Axis §4.46b chart key, PDF p. 27 | A parenthesized rating cannot be used if any units in the hex have **both ammo and a non-parenthesized number for that rating**; replacement points do not prevent its use. This is a condition on other units' ammo and the particular rating, not proof that the engineer needs no ammo under other rules. |

The corrected §23.11 restriction is engineer-specific and directly replaces
the contrary printed clause. Corrected §3.4 supplies an additional general
enemy-attack/stack condition; read both as necessary constraints pending a
specific authoritative exception. The chart key names a narrower,
ammo-qualified blocker; the errata do not explicitly say whether it overrides,
supplements, or is superseded by the broad corrected stack wording in every
case. Applying all three conjunctively is a conservative **candidate** where
their predicates can be evaluated, not a source-proven universal hierarchy.
In particular, corrected §23.11 blocks a friendly combat unit even if that
unit lacks ammo or has only parenthesized ratings; the chart key's ammo
qualifier does not erase the explicit engineer-specific correction.

## Bounded cases for adjudication

| Situation | Source-backed disposition |
| --- | --- |
| Enemy attacks engineer alone; no other applicable restriction | §3.4 and corrected §23.11 permit the stack/attack conditions for defensive `(1)`; check all other combat and supply rules before resolving. |
| No attack, including enemy adjacency or an enemy-controlled hex | §3.4's attack condition is not established; do not activate `(1)` from proximity alone. Entry permission is a separate movement question. |
| Enemy attacks engineer stacked with a friendly combat unit, with or without ammo, parenthesized or not | Corrected §23.11 says engineer may not use the parenthesized strength. Determine combat-unit classification for that specific situation before applying it. |
| Enemy attacks engineer with only other non-combat units having parenthesized strengths | General and engineer conditions appear compatible, but determine what “only parenthesized strengths” means for `0/(1)`, maximum TOE, and units without a rating before approving. |
| Enemy attacks engineer with a non-combat unit having an unparenthesized defensive number but no ammo | Chart key alone does not block on that unit; corrected §3.4's “only parenthesized strengths” points the other way. Unresolved without a hierarchy ruling. A friendly combat unit would separately trigger §23.11 regardless of ammo. |
| Enemy attacks engineer with a unit having ammo and an unparenthesized defensive number | Chart key blocks `(1)`; if that unit is friendly combat, corrected §23.11 independently blocks it. |
| Enemy attacks engineer with replacement points only | Chart key says replacement points do not prevent use; whether they meet corrected §3.4's “other units” language is not expressly settled by the key. |

`0/(1)` supplies zero offensive and conditional one defensive close-assault
rating; it does not establish an independent engineer offensive-assault
permission. This review does not decide how normal ammunition expenditure,
attachment representation, losses, or other combat effects apply.

## Focused test vectors after a ruling

1. Preserve `bbb`, `0/(1)` and maximum TOE `(1)` through import,
   serialization and replay for all three distinct engineer identities.
2. For a valid enemy attack, compare engineer alone with engineer plus a
   confirmed friendly combat unit; vary that unit's ammo and rating parentheses
   and require the corrected engineer restriction in every such case.
3. Check no attack versus actual attack, enemy adjacency versus enemy-controlled
   entry, and separate Naval or other non-assault events from the condition.
4. Exercise mixed non-combat stacks with parenthesized/unparenthesized
   defensive ratings and ammo present/absent, plus replacement points. Until
   classification and precedence are accepted, return a typed unsupported
   result rather than silently choosing a value; after a ruling, assert each
   selected outcome and rejected alternative.
5. Check a represented parent or attachment whose combat-unit status changes,
   and that offensive zero is not turned into an offensive right. Verify
   deterministic replay and no state mutation on a blocked adjudication.

## Evidence and limits

Visually inspected [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
(PDF pp. 17–18, 27; SHA-256
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`),
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
(printed/PDF pp. 6, 35; SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`),
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
(PDF pp. 1–2; SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`).
These are same-edition source copies; no complete errata or other combat-rule
survey, independent edition, executable test, or runtime change is claimed.
