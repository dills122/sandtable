# Graziani six-turn schedule: bounded empty cases

**Disposition:** The printed ordinary schedules exclude several *new arrivals*, not the corresponding units, equipment, actions or rules as a whole. These bounds reduce the source-reachable arrival/training queue; they do not establish an empty game state or admit a runtime shortcut. This is an informed same-scan check, not an independent edition or complete exception audit.

## Window and source

Scenario §60.22 starts GT1 Operations Stage 1 and ends at the conclusion of GT6 Operations Stage 3. The previously checked [turn track](2026-11-17-graziani-weather-schedule-next.md) places GT1–2 in September and GT3–6 in October 1940. Scenario §60.47 says no Commonwealth replacement points may arrive before November. Thus the **Commonwealth replacement-point arrival event** is empty throughout this window under the printed scenario instruction. This does not exclude whole-unit reinforcements, initial points or other resources, training of units, or an explicit applicable exception. In particular it does not by itself prove that every Commonwealth replacement training or absorption subject is absent.

Axis reinforcement schedule §4.43b starts at GT9 OpStage 2. Its listed ordinary whole-unit arrivals and attached trucks therefore contain no GT1–6 entry. This does not exclude Axis scenario setup, replacement shipments, pregame convoy plans, or another applicable source of arrivals. Compare [AA lifecycle](2026-11-17-graziani-aa-lifecycle-next.md) for prior checks of these boundaries.

## Ordinary Axis production: first possible schedule entries

Axis chart §20.66 defines Dates as the first turn an item may be planned, with arrival two turns later. The following arithmetic assumes that ordinary rule and the printed rows; actual production, planning, transport, losses and any scenario or later correction remain separate gates. `GT7` means after this scenario's end, not an actual arrival.

| Chart subject | First printed planning turn | Earliest nominal arrival | Bounded implication |
| --- | ---: | ---: | --- |
| Italian infantry replacement | 5 | 7 | No ordinary newly planned receipt during GT1–6. |
| Italian 65/17, 75/27, 75/18, 100/17 and 105/28 artillery replacements | 9 | 11 | No ordinary receipt from these rows during GT1–6. |
| Italian 47/32 anti-tank and 75/46 heavy AA replacements | 10 | 12 | No ordinary receipt from these rows during GT1–6. |
| Italian armored car/recce replacement | 31 | 33 | No ordinary receipt from this row during GT1–6. Scenario §60.31 separately supplies two Autoblinda points, optionally as trained replacements: do not remove those starting subjects. |
| German replacement production | 38 or later | 40 or later | No ordinary chart receipt in GT1–6. The earliest listed row is infantry; this is not a proof that all German pieces are absent by every source. |
| Axis heavy truck replacement | 5 | 7 | No ordinary newly planned heavy truck receipt in GT1–6. Starting trucks remain. |
| Axis medium truck replacement | 6 | 8 | No ordinary newly planned medium truck receipt in GT1–6. Starting trucks and Land-only motorization setup remain. |
| Axis light truck replacement | 4 | 6 | GT1–5 empty for this ordinary receipt; GT6 is conditional, not empty. |
| Italian Light AA replacement | 2 | 4 | GT1–3 empty for ordinary receipt; GT4–6 remain conditional and subject to Land-only monthly-limit ambiguity. |
| Italian CV L.3 or CV 33/35 tank replacement | 3 | 5 | GT1–4 empty for ordinary receipt; GT5–6 remain conditional. |

The table enumerates selected rows, not every equipment type. Printed Italian French 149 mm and 155 mm rows have a special Tunis availability exception to the two-turn delay, at GT39; they do not create a six-turn receipt. September errata §20.66 corrects the explanatory note's M11/39 versus M13/39 reference, not the above dated rows. No claim is made about later errata or alternate editions. Starting inventories, captures, repair, replacement already on map, and scenario-specific exceptions need their own evidence. The ordinary chart date alone cannot exclude those paths.

## Training and phase consequences

Section 20.43's mandatory replacement training and durations remain relevant to eligible existing and later-arriving subjects. Absence of a particular ordinary arrival is not proof that its training, designation, movement or absorption phase is globally empty. The [replacement training follow-up](2026-11-17-graziani-replacement-training-followup.md) documents one/three/six Operations Stage durations, potential arrival-stage credit, and unresolved instructor, interruption and completion timing. For a hypothetical CV replacement arriving GT5 OpStage 1, six possible stages remain through GT6 OpStage 3 inclusive; that count does not certify completion or a legal witness. For a later-stage GT5 arrival, fewer than six stages remain, assuming no earlier credit or special rule. These are conditional counts, not actual arrivals or conclusions about pre-existing replacements.

Scenario §60.6 fixes Italian initiative throughout GT1; the ordinary initiative contest begins GT2. Therefore a GT1 **ordinary initiative contest** is empty, but stage-specific first-player choices and weather remain. See [initiative source check](2026-11-17-graziani-initiative-ratings-next.md) and [weather schedule](2026-11-17-graziani-weather-schedule-next.md). Do not skip the entire turn preamble or infer a GT2 winner.

## Verification and limits

Visually checked [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF pp. 3–4 / printed pp. 27–28, §§60.22, 60.31, 60.47 and 60.6; [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF pp. 2 and 33, §§4.43b and 20.66; and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) PDF p. 2, §20.66. Local SHA-256 values: scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, Axis `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`, errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Stage-count and calendar inputs rely on linked earlier same-scan readings. Reverify any proposed exception and all relevant rules before admission. Matching hashes establish file identity, not provenance, rights, independent review or complete source coverage. No runtime, ruleset or content changes; no gameplay tests run.
