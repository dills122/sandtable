# Tobruk port: independent source recheck

**Status:** unresolved source conflict; no selected implementation policy. This recheck was prompted with the 7/5/3 conflict, so it is independent page inspection, not a blind reading. It does not prove source rights, six-turn reachability, or absence of later official corrections.

## Direct source evidence

Visually inspected [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF pages 3–4 (printed 27–28), [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) PDF/printed page 15, and [Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) PDF pages 27–28 (printed 23–24). SHA-256 respectively `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`, and `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`.

| Locator | Printed fact and scope |
| --- | --- |
| Scenario §§60.22–60.23 | Graziani's Offensive ends after turn 6, stage 3; the Italian Campaign starts at the same turn/stage and ends after turn 20, stage 3. Both are in the first scenario group. The §60.7 setup statement is group setup, not an alternate later-scenario capacity. |
| Scenario §60.7 | All ports start at listed efficiency except Tobruk, stated at efficiency level **7**; the partially sunk San Giorgio blocks its harbor. This says current starting efficiency, not seven tons, seven stacking points, or an undamaged maximum. |
| Chart §55.3 | Tobruk's listed efficiency is **5**; its separate capacity columns are **1 incoming stacking point, 3 outgoing stacking points, and 1,700 tons** per Operations Stage. Footnote says campaign starting efficiency is below five because of the same obstruction. Reinforcements are unaffected by port capacity or current efficiency; stacking-point and tonnage handling do not affect one another. |
| Air §§55.12, 55.14, 55.18 | §55.12 also gives Tobruk efficiency 5. The §55.14 example uses remaining efficiency divided by assigned maximum to scale capacity, rounding reductions upward to the nearest ton. §55.18 limits ordinary recovery to assigned maximum; obstruction has a separate exception. |
| Air §§55.25–55.26 | San Giorgio reduces Tobruk efficiency by **three levels** at game start. Removing harbor blockage requires an eligible friendly engineer battalion or Commonwealth HQ with engineering capability, one level per Operations Stage; Tobruk's listed cost per level is 25 ammunition and 10 stores. Applicability and costs under Land-only abstraction remain separate questions. |

The 7 and 5 are efficiency levels; 1,700 tons and 1/3 stacking points are distinct throughput measures. The three-level rule and chart imply **2 of 5** if both apply literally; that contradicts the explicit starting 7. Seven is already described as the starting level with the obstruction present, so subtracting three again is unsupported. Neither a corrected maximum nor a replacement starting value is printed in these passages. A scenario-precedence policy retaining 7 requires explicit rules for a current level above chart maximum and percentage capacity; using 2 requires choosing chart/obstruction text over the scenario value. Neither choice establishes an official correction.

## Errata and external interpretations

Reviewed searchable text of the four-page [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Its §55.11 correction prefers chart §55.3 when deciding what can be shipped; its §55.3 clarification addresses excess Axis arrivals at Tripoli. Neither explicitly supplies Tobruk's initial efficiency or resolves §60.7. Searchable-text review is limited to this packet and its extraction quality; later errata or rulings remain possible.

[Living Rules R-027](https://basmith.net/cna/rules/scenarios/10-the-italians) selects 2 as a local ruling and acknowledges the printed 7. A separate [simulator's documented user ruling](https://tangled.org/glassparency.bsky.social/cna-agent-sim/blob/28e183a5b24d2d60b4c97004b01cdc1cc08c3b92/docs/rules/graziani-offensive.md) selects initial/max 5/5. These are different project choices, not evidence of an official ruling or source correction.

## Decision boundary

The existing [port conflict analysis](2026-11-12-graziani-ruling-decisions.md#open-conflict-tobruk-port-efficiency) and [independent facility review](2026-11-17-graziani-facilities-independent-decision.md#tobruk-port-no-selected-value) preserve the conflict. Land §32.36's 50% destination threshold makes a ruling potentially material if Commonwealth sea supply to Tobruk is otherwise legal and reachable. Under the conditional 2/5 interpretation and no other modifier, 40% fails that threshold; this is not proof of six-turn capture, shipping legality, or an actual initial gameplay outcome. Decide precedence, capacity above a maximum, abstract-mode clearance, and reachability separately before admitting behavior. Preserve original facts and adopted policy in versioned provenance.
