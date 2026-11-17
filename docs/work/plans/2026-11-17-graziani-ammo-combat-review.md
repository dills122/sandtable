# Land-only ammunition combat: source recheck

**Correction:** [ammo-combat note](2026-11-17-graziani-ammo-combat-next.md)
overstates the evidence when it calls §14.24 evidence that anti-armor consumes
ammunition in Land-only mode and suggests a zero-cost result is necessarily
wrong. §32.11 says to ignore other references to ammo usage and expenditure;
only the §32 supply rules below apply. §14.24 is ordinary-game evidence of
intent and vocabulary, not a standalone Land-only debit. §32.21's meaning of
“Assault” determines whether its rate covers anti-armor; that remains a
versioned interpretation, not a source-proven additional charge. This is a
fresh self-audit by the note's author, not an independent reader.

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 19–23 and 43, §§11.21–.25, 12.15–.18, 14.0–.26, 15.0–.16,
32.11, 32.21–.22; scan SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Rechecked [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3 §§50.12 and 50.2; SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
No separate full errata or detailed-Logistics audit performed.

* §32.21's non-phasing battalion-equivalent assault cost is 1 ammo, barrage 2;
  phasing cost doubles the applicable amount. §32.22 says expend at combat.
  Neither passage expressly states a separate anti-armor line. §11.21 describes
  “any form of Assault (Anti-Tank, Close)” for CP; errata §50.12 says
  “assaulted (either anti-armor or close)” in a separate detailed context.
  These support broad inclusion but do not expressly define §32.21's unit of
  account, and the erratum's detailed §50.2 rate is not automatically applicable.
* §14.26 bars the same TOE point from being assigned to anti-armor and assault
  in the same segment, while a unit can divide points between those functions.
  §12.18 permits forward artillery to participate in anti-tank **or** close
  assault in the same segment as barrage when properly rated and supplied.
  These are allocation facts, not a proof of one charge per role, target,
  battalion, TOE fraction, segment or stage. The note correctly leaves those
  rates and repeated combat unresolved.
* §15.15's no-ammunition close-assault consequences and errata §50.12's
  surrender correction require a separate interaction analysis with the
  §32.11 abstraction. Do not automatically apply either full set of detailed
  logistics restrictions, or conclude every no-ammo combat restriction is
  waived, from the checked excerpts alone.

Tests should compare two explicit provisional interpretations for
anti-armor-only combat (covered as assault versus no separately specified
abstract debit) without pretending either is printed as a separate cost.
Check phasing/non-phasing distinction, split TOE, barrage plus later combat,
multiple targets and repeat segments. When §32.21 does apply, debit at the
source's combat instant; prevent duplicate source deductions on replay.
Remaining legal-action, stock-shortage and player visibility rules require
separate decisions. No runtime changes or tests.
