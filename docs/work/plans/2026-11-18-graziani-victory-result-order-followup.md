# Graziani victory result: shared-objective constraints

**Date:** November 18, 2026. Bounded follow-up to the [result-composition proposal](2026-11-17-graziani-victory-independent-decision.md). The printed objective lists give conditional constraints on some simultaneous row matches, but no general cross-side ranking or tie-break. This note selects no result policy and proves no six-turn reachability.

## Source and conditional consequence

Scenario §60.81 names **Sollum (C4021)** among locations the Italian must retain for tactical victory, and also requires the Commonwealth to take and hold Sollum for both its tactical and decisive victories. It names **Siwa** for Italian decisive victory and **Siwa (C0127)** for Commonwealth tactical and decisive victories. The Italian decisive condition expressly uses occupation; the Commonwealth rows say take and hold. Section 60.5 also lists Siwa C0127. These shared names and printed locators are source evidence of overlap, not a complete possession algorithm.

Under an explicitly assumed terminal interpretation where each required take/hold or retain predicate entails **exclusive current possession of that same single objective hex**, two sides cannot satisfy both predicates at that hex in the same snapshot. Accordingly these four row-pair combinations would be mutually exclusive under that assumption:

| Italian row | Commonwealth row | Shared objective |
| --- | --- | --- |
| Tactical | Tactical | Sollum C4021 |
| Tactical | Decisive | Sollum C4021 |
| Decisive | Tactical | Siwa C0127 |
| Decisive | Decisive | Siwa C0127 |

Land §8.13 states that a unit may not enter an enemy-occupied hex and references §27.4 as an exception. That supports ordinary opposing occupancy as a relevant constraint, but it is an entry rule with an explicit exception; it is not a complete formal definition of terminal location control or proof that all extraordinary cases are impossible. The scenario does not define every detail of retain, take/hold, control, occupation or timing. If a later accepted predicate permits different temporal or shared-control readings, the table's inference must be reconsidered. These conditional exclusions do not justify rejecting independent row evaluation before those predicates are established.

This observation does **not** rule out all cross-side combinations: the two strategic rows name different objectives, and the printed lists alone do not establish that they can or cannot simultaneously be fulfilled. Nor does an unmatched pair establish which side wins. Supply, actual control, terrain, legal reachability and the terminal snapshot remain independent gates. Even a positive row and a known incompatibility with some opposing rows do not settle other unknown rows.

## Ordering boundary

Scenario §60.22 ends Graziani at the conclusion of GT6 OpStage 3, while §60.81 prints six labeled conditions without a cross-side precedence or simultaneous-result procedure. Land §5.2 orders player A's F–L phases before B's F–L, but that phase order does not itself make an earlier achieved objective the winner or instruct players to score after each actor. Choosing highest levels, resolving simultaneous matches, handling no matches, and defining publication remain policies, not direct consequences of phase order or the exclusions above. Scenario §60.82's points belong to the separate Italian Campaign; do not import them without a ruling.

Possible checks after accepting possession semantics: each of the four pairs above with one shared objective, different valid objectives, an unknown possession or route, and separately both strategic rows. These are logical test vectors under the named assumption, not executed tests or reachable game states.

## Provenance

Visually checked [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF pp. 3–4 / printed pp. 27–28, §§60.22, 60.5 and 60.81–.82, SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF pp. 12 and 14, §§5.2 and 8.13, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. Same source scans, not an independent edition. No exhaustive errata, exception, possession or reachability review. Documentation only; no data/runtime changes or gameplay tests.
