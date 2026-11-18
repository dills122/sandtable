# SU-03: residual Commonwealth reinforcement-track check

**Result:** The previously excluded reinforcement/withdrawal schedule contains a material later-unit timing discrepancy: the OA prints `1/65` for 1st Battalion d'infanterie de marine, while the schedule prints that battalion at **GT65, OpStage 3**. The same schedule lists 1st Battalion de fusiliers at **GT63, OpStage 3**, matching its OA `3/63`. Neither row says the initial French Motor Marine company converts, withdraws or becomes either battalion. Its lifecycle remains open.

## Why this check and what it establishes

The [transition follow-up review](2026-11-17-graziani-marine-transition-followup-review.md) expressly did not search the reinforcement/withdrawal tracks; [later-scenario review](2026-11-18-graziani-marine-source-next-review.md) also leaves that category unchecked. The [source dispositions](2026-11-18-graziani-ledger-c-source-dispositions.md) identify the company-to-later-unit crosswalk and withdrawal as SU-03's remaining question. Checking the specific later OA rows against their scheduled arrivals is therefore a discriminating source comparison, not a current-game-state example. The wider [post-shortlist audit](2026-11-18-graziani-ledger-c-postchecks.md) did not identify it.

Visually checked [Commonwealth chart](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf) PDF pp. 4, 20 and 21:

| Source | Printed entry and bounded implication |
| --- | --- |
| §4.43a, PDF p. 4, GT62/2 | HQ 1 Free French Brigade plus four named infantry battalions. This row does not name the initial company or specify its withdrawal. |
| §4.43a, PDF p. 4, GT63/3 | `2nd NA AT Coy and 1st Bn de fusiliers [both of 1 FF X]`. OA p. 20 prints `1 Fslrs`, ID `ff`, four Light AA, `3/63`; note calls it 1st battalion de fusiliers marine. Their named unit and stage match within that comparison. |
| §4.43a, PDF p. 4, GT65/3 | `1st Bn d'infanterie de marine [1 FF X]; Trucks: 5M`. OA p. 20 prints `1 Marine`, ID `r`, `N`, **`1/65`**. Same named battalion and parent, but different printed stages. A direct correction or applicable precedence is needed before treating either as the authoritative arrival. |
| OA p. 21, company row | `1st Company, Fr Mtr Marine`, counter abbreviation `1`, ID `v`, and note identifying it as a company of 1st Free French Motor Marine Battalion. The checked later schedule rows do not say that this company changes into either later counter. |

Scenario §60.41 places the company at C3926; §60.22 ends Graziani GT6/3. This later timing conflict is outside that six-turn horizon and does not alter the scenario's fixed starting company by itself. Track parent labels and similar names are not proof of continuity or coexistence. No source-selected precedence, withdrawal or transition is asserted.

The September errata's §4.44b item 3 corrects the **company** arrival to `D`; §4.43a adds separate omitted schedule items. The previously reviewed text search for marine/fusilier names found no further named correction; it does not exclude an indirect or later clarification. The printed `1/65`/`65/3` conflict remains open.

## Provenance and limits

Visually inspected Commonwealth chart PDF pp. 4, 20–21 and September errata PDF p. 1; scenario locators and the corrected company date are corroborated by the cited prior reviews. SHA-256 Commonwealth chart `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`; scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. This targets named rows, not a complete unnamed withdrawal, all-scenario or later-edition search. Same-edition scans; no runtime/data edits or gameplay tests.
