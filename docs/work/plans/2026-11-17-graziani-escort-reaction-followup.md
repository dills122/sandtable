# Land-only supply escort and Reaction: source follow-up

**Disposition:** No combined legality rule is established. Section 32.51 strengthens the case for applying ordinary Medium Truck rules, but does not by itself establish whether the carried supply group is a §8.53(a) Truck Convoy or which specific exceptions override general Reaction restrictions. Preserve these as separate versioned rulings before implementation. This is a synthetic rules analysis, not proof of a reachable Graziani contact.

The [independent visual review](2026-11-17-graziani-escort-reaction-followup-review.md)
clarifies the cases below: §8.53(b)'s Close Assault must be declared by the
adjacent moving unit against the reacting non-phasing unit; an unrelated
assault does not satisfy that condition. Section 8.53(a) tests stacking with
friendly combat units at Reaction, separately from §32.33's friendly-phase
escort requirement. The shorthand “escorted” and “announced Close Assault”
below must be read with those exact predicates, not as a continuous-escort
or any-assault test.

## Source and scope

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed pp. 15–16, 43–44, §§8.5, 8.9, 32.18, 32.31–.33 and 32.51–.58; [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), §32.0. This follow-up checks extracted source text and builds on the prior [visual inspection](2026-11-17-graziani-supply-escort-reaction-next.md); it is not an independent scan, full scenario, chart, later-errata or designer-ruling audit. Errata text search finds no entry numbered §8.53 or §32.58; §32.0 warns that abstract rules were untested. Absence in extracted text does not rule out an unnumbered correction or later ruling. Previously recorded SHA-256: Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

## Source-supported distinctions

- §32.33 requires a transported Supply Unit to begin and remain with a friendly combat unit in its owner's Movement/Combat Phase; it explicitly excludes normal non-phasing movement from **that restriction**. It also forbids using both the owner's Movement/Combat and Truck Convoy phases. It does not identify an escort that must remain the same throughout, or say that all other conditions on Reaction disappear.
- §32.51 treats Motorization Points as Medium Truck Points in all aspects except modifications in that major case. §8.98 distinguishes attached trucks moving in Movement/Combat from unattached trucks moving in Truck Convoy Phase, and permits trucks to move during the opposing turn in Reaction, etc. Section 32.33 explicitly permits the supply unit in either friendly phase, so importing §8.98's attached/unattached timing mechanically also needs reconciliation. These provisions support considering convoy restrictions, but do not explicitly classify every supply-carrying allocation as a Truck Convoy for §8.53(a), or establish whether cargo attachment counts as attachment to a historical unit under §8.96.
- §8.51 describes enemy **combat** movement adjacent to a friendly non-phasing **combat** unit; §8.53(a) excludes unescorted Truck Convoys, while §32.58(D) gives transporting Motorization Points permission to react below enemy CPA 35. The specific permission supports reaction for this carrier even though §8.51 describes a combat-unit target, but whether the trigger requires an adjacent friendly combat unit, or instead applies directly to the carrier, remains a separate issue from convoy classification and escort. Do not conflate the three.
- §8.53(b) uses enemy CPA at least six greater **and** a declared Close Assault; with carrier CPA 15 that produces threshold 21. Section 32.58(D) independently names an adjacent unit with CPA 35 or more, without stating an assault condition. Neither explicitly says whether it replaces the general differential or adds another bar. Section 8.54's size exception to pinning complicates the general threshold; carrier size and its applicability need a ruling. Section 8.53(c–d) bars already-in-enemy-ZOC and combat/Engaged cases; §8.55 bars entering enemy ZOC during reaction. No checked passage expressly disposes of all those restrictions for these carriers.
- §32.58(B) returns points forced above CPA 15 to the last hex where they were not above it. Section 8.98 instead says Truck Points forced above CPA in Truck Convoy Phase are captured. The former is a plausible specific modification via §32.51; source does not explicitly settle phase overlap, coupled cargo outcome or order of return, capture and occupancy. Normal Reaction is in the opposing Movement/Combat Phase, so §8.98's Truck Convoy Phase capture text should not automatically be imported into that reaction case.

## Decision cases for a future ruleset

Assume sufficient assigned points, a legal adjacent enemy combat mover, no prior ZOC or combat/Engaged status, a legal destination outside enemy ZOC, and an otherwise eligible triggering context; record each assumption. “Permitted” below means only that the cited condition does not independently bar it, not that a complete move is certified.

| Carrier / enemy | Source distinction and required resolution |
| --- | --- |
| Escorted, enemy CPA 20, with and without announced Close Assault | Neither §8.53(b)'s six-point differential nor §32.58(D)'s 35 threshold bars reaction. Verify actual trigger, escort and all remaining rules. |
| Escorted, enemy CPA 21 or 34, no announced Close Assault | §8.53(b)'s declared-assault condition is absent and §32.58(D)'s threshold not reached. Test both boundary values. |
| Escorted, enemy CPA 21 or 34, announced Close Assault | Conditional §8.53(b) bar versus §32.58(D)'s below-35 permission. Compare cumulative and specific-override readings, with §8.54 applicable-size facts independently settled. |
| Escorted, enemy CPA 35, with and without announced Close Assault | §32.58(D)'s stated exception applies at equality; no invented assault prerequisite. |
| Unescorted, enemy CPA 20 | Separates §32.33's non-phasing exception, §8.53(a)'s convoy restriction, carrier classification, §8.51 trigger, and §32.58(D)'s specific permission. Do not preselect a result. |
| Eligible threshold, but already in enemy ZOC, combat/Engaged, or destination enemy ZOC | Test each independently against §§8.53(c–d), 8.55; any override requires an explicit ruling. |
| Qualifying small moving force against a large reacting force | Check actual organization/strength under §8.54; separately determine whether the carrier has a relevant size and whether that exception affects the selected §32.58 hierarchy. |
| Forced path reaches exactly 15, then exceeds 15 | §32.58(B) returns to last non-exceeding hex on forced overrun. Separately test Truck Convoy Phase capture conflict, cargo, occupied return hex and deterministic replay after selecting policy. |

Add friendly-phase start, continuous escort and mid-route replacement cases; real 29/30 and dummy 5/6 assignment; correct Organization timing and phase history. Test real/dummy views for equivalent public outcomes where identity is hidden, without exposing a minimum-point legality oracle. This note makes no fuel, route, movement, gameplay or implementation changes, and no gameplay tests were run.
