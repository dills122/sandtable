# Commonwealth replacement production planning, GT1–6

**Result:** The sources disagree on lead time and production lookup. Scenario §60.47 prohibits Commonwealth replacement-point arrivals before November 1940, so no such receipt is established within GT1–6 (September III–October IV). It does not expressly waive advance planning for November and later. A planning workflow therefore cannot safely skip all six turns merely because no replacement points arrive during the scenario, nor invent plans or dice that were not recorded.

## Source comparison

| Source | Printed instruction | Consequence / conflict |
| --- | --- | --- |
| Land §5.2, p. 11, Naval Convoy Schedule Phase | Commonwealth consults its Production Table, determines replacement points **two turns hence**, and plans arrival. | Two-turn lead. Sequence places this in the Naval Convoy Stage, before the Operations Stages; scenario starts GT1 OpStage 1, so whether its preceding planning phase exists or requires explicit initialization is separate. |
| Land §20.21, p. 31 | Commonwealth production replacement arrivals in player-determined stages must be determined **four game-turns in advance**. | Direct conflict with §5.2. Arrival is in Naval Convoy Arrival Phase, not the planning phase. |
| Land §20.72, p. 32 | Opening says **two months** in advance, but its example plans August arrivals in first July Naval Convoy Schedule Phase and its subsequent text selects arrival month's table. | Internally inconsistent even before errata. |
| September 1979 errata §20.72, PDF p. 2 | Corrects opening to **one month**, not two; states production tables use the month/turn in which the player **plans** arrivals, contrary to printed §20.72's arrival-month lookup. | Correction favors planning-time lookup. It does not expressly edit §5.2's two-turn lead or §20.21's four-turn lead, nor define every partial-month scenario-start case. |
| Commonwealth §20.78A/B/C, chart PDF p. 30 | Truck table says result arrives **four game-turns hence**. Infantry table's Date is the turn on which points are planned, arriving **four game-turns later**; infantry begins GT3. General production chart Date is first/last game-turn on which point may be **planned**. | Specific tables and §20.21 independently support four-turn planning for the two random tables. §20.78C's date key explicitly uses planning turn; apply item-specific availability and period caps, not one universal eligibility date. |
| Scenario §60.47, printed p. 28 | Uses Commonwealth reinforcement track; no Replacement Points may arrive before November 1940. | Arrival prohibition, not a printed prohibition on prior planning or an instruction that all charts are zero. Track whole-unit arrivals are distinct from replacement points. Land-only §60.92(A) ties motorization ability to ability to bring in medium Truck Points; its interaction with the scenario prohibition and truck table requires separate interpretation. |

The errata changes the table lookup to planning time, not simply a new lead time. The production chart itself labels availability dates by planning turn: e.g. armored recce/armored car starts GT3; light AA GT7; 2-pounder GT5; Mark VI Light, A9 and A10 GT3. These are printed eligibility boundaries, **not** mandated choices or actual shipment counts. Land-only §32.81 removes Commonwealth AA replacement points from play; chart availability by itself does not override it. §20.78B/C and errata do not erase differing source rules on which planning phase is reached.

An [independent source review](2026-11-17-graziani-cw-replacements-review.md)
also finds that §20.73 describes random infantry and truck production by
arrival date, another conflict with the corrected planning-time lookup. The
truck chart's three type-specific rolls and Alexandria limit require separate
Land-only applicability review; they are not automatically carried over.

## Conditional six-turn matrix

The original map track gives GT1 Sep III, GT2 Sep IV and GT3–6 Oct I–IV. The two candidate offsets below are arithmetic alternatives, not a declaration that any turn's phase was actually played. Four game-turns corresponds to the errata's one month in this four-week calendar for these rows; this does not prove every possible monthly phase alignment.

| Planning GT | Calendar | Two-turn candidate arrival | Four-turn candidate arrival | Consequence with scenario §60.47 |
| --- | --- | --- | --- | --- |
| 1 | Sep III | GT3, Oct I | GT5, Oct III | Both pre-November; no replacement-point receipt. GT1 pre-OpStage planning status separately unresolved. |
| 2 | Sep IV | GT4, Oct II | GT6, Oct IV | Both pre-November; no replacement-point receipt. |
| 3 | Oct I | GT5, Oct III | GT7, Nov I | Two-turn target barred; four-turn target beyond scenario. Infantry table first becomes plan-eligible at GT3. |
| 4 | Oct II | GT6, Oct IV | GT8, Nov II | Two-turn target barred; four-turn target beyond scenario. |
| 5 | Oct III | GT7, Nov I | GT9, Nov III | Both targets beyond scenario; different potential planning obligation, no in-scenario receipt. |
| 6 | Oct IV | GT8, Nov II | GT10, Nov IV | Both targets beyond scenario; different potential planning obligation, no in-scenario receipt. |

Treat any action called an obligation as **conditional on** reaching the applicable schedule phase, relevant production category, scenario boundary policy, and selected versioned lead-time ruling. Sources do not establish whether a finite scenario ending at GT6 must process plans exclusively for after its end, whether those plans may be omitted as irrelevant, or whether the GT1 preamble was already handled. They also do not supply dice outcomes or a player allocation. A zero receipt is not evidence of a zero production-table roll, and a post-scenario target is not a reason to invent an in-scenario delivery.

## Tests that distinguish policies

1. Scenario gate: reject any ordinary Commonwealth replacement-point arrival GT1–6, while retaining whole-unit reinforcement schedule events; test the truck/motorization interaction only under a separately selected scope ruling.
2. At GT3, two-turn candidate GT5 is barred by §60.47; four-turn GT7 is after scenario. Verify no replacement arrives GT5 and no rolled result is invented. At GT5 distinguish targets GT7 and GT9 without actually executing outside-scenario events.
3. Feed different available entries for planning and target periods: corrected §20.72 uses planning time; original §20.72 arrival-time alternative gives a different result. Verify §20.78 date fields index planning GT, including infantry before/at GT3 and 2-pounder before/at GT5.
4. For a selected four-turn interpretation, one recorded truck or infantry production outcome targets exactly four game-turns later; do not confuse phase of scheduling with Naval Convoy Arrival Phase or training. Defer stage split, assignment and receipt until applicable rules and actual recorded choices are available.
5. Test GT1 OpStage 1 startup separately from a fully played GT2 turn and from a hypothetical pregame plan. Unknown pregame phase or missing plan is not a zero plan; state and event versions should carry policy identity for deterministic replay and keep secret player plans private.

## Provenance

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed pp. 11, 31–32, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf), PDF p. 30, `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`; [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF pp. 3–4 / printed pp. 27–28, `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; September errata PDF p. 2, `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Calendar is the previously inspected original Map D track. Same edition, no later clarification asserted; no runtime changes or gameplay tests.
