# Graziani six-turn exclusions: planning, fleet and dated changes

**Disposition:** Additional narrow source bounds, separate from [batch 1](2026-11-17-graziani-six-turn-exclusions-batch.md). These exclude the specified chart entry, action or dated transition, not all ways an item can exist or all phases in which it can matter. Informed same-scan research; no independent review, complete exception audit, runtime admission or gameplay test. Scenario §60.22 runs GT1 Operations Stage 1 through GT6 Operations Stage 3; the [turn-track reading](2026-11-17-graziani-weather-schedule-next.md) identifies GT1–2 as September III–IV and GT3–6 as October I–IV 1940.

## Commonwealth production planning, not receipt

Commonwealth chart §20.78C, PDF p. 30, defines Date(s) as the first/last **planning** Game-Turn. For these chart rows, ordinary chart planning before the listed first turn is excluded, regardless of which eligible quantity the player would otherwise choose. It does **not** follow that existing TOE or replacement points of those types are absent; scenario §60.47's pre-November replacement-arrival prohibition is a separate restriction. The chart's Tiger convoy, explicitly additional to its replacements, is GT32. Other exceptions and later corrections require separate checking.

| Printed chart rows | First planning GT | Bounded empty planning window |
| --- | ---: | --- |
| Light AA | 7 | GT1–6 |
| Heavy AA; 25-pounders | 11 | GT1–6 |
| 4.5-inch guns; 5.5-inch howitzers; 155 mm howitzers | 76; 83; 51 | GT1–6 |
| 6-pounders; SP 6-pounders; 17-pounders | 75; 76; 103 | GT1–6 |
| A13 Cruiser; Crusader Mk I, II, III; Matilda; Valentine; Stuart; Grant; Sherman; Churchill | 9; 31; 41; 88; 9; 39; 35; 66; 89; 90 respectively | GT1–6 |
| 2-pounders | 5 | GT1–4 only; GT5–6 planning eligibility remains conditional. |
| Armored recce/armored car; Mark VI Light; A9 Cruiser; A10 Cruiser | 3 each | GT1–2 only; GT3–6 planning eligibility remains conditional. |

Section 20.78B on the same page gives infantry production's first column GT3–30 and dates its results by the planning turn, with four-turn arrival. Thus its **ordinary infantry production-table planning lookup** has no GT1–2 column; GT3–6 are not empty. Section 20.78A separately starts its truck table at GT1: do not extend the infantry result to truck production or infer a zero roll. Land-only §32.81 removes Commonwealth AA replacement points; do not convert the generic chart's GT7 into a Land-only future entitlement. The [planning follow-up](2026-11-17-graziani-cw-replacement-planning-next.md) retains the lead-time, actual phase and pregame initialization questions. A first eligible date is neither a mandatory plan nor proof that the relevant phase occurred.

## Fleet and fixed dated changes

| Bounded subject | Source and consequence | Limits |
| --- | --- | --- |
| Initial Commonwealth warship movement in GT1 | Scenario §60.45 first paragraph, PDF p. 4 / printed p. 28, prohibits moving its initial warships until GT2 OpStage 1. September errata PDF p. 3 §60.45 retains that paragraph and discards text beginning “The following units are available.” | Initial deployment at Alexandria or player-selected Malta is setup, not subsequent movement. This does not prohibit fire, AA use or an Axis harbor attack; see [fleet source review](2026-11-17-graziani-abstract-aa-fleet-review.md). Do not use discarded scenario fleet lists. |
| Ordinary Commonwealth fleet reinforcement schedule | Commonwealth chart §30.6, PDF p. 31, has initial `Deploy`, then first subsequent row `3/8`: Barham, York, Ajax, Marne and Partridge. No post-deployment entry in that printed schedule occurs during GT1–6. | Initial vessels and player-selectable Malta placement remain; the bound concerns this schedule, not all hypothetical special events or ships' damage and repair. |
| Matruh Garrison's dated conversion | Commonwealth OA, PDF p. 10, dates its becoming Selby Force to GT12 OpStage 2; its dated disbandment is GT21 OpStage 3 Organization Phase. Neither dated transition falls within GT1–6. | The same OA permits voluntary HQ movement in the Operations Stage in which it is attacked in a Close Assault, even before conversion. Do not turn the date into an unconditional movement ban or resolve subordinate attachments. |
| Selected Australian ordinary arrivals | Commonwealth OA PDF p. 13 lists 19th Australian Brigade HQ and 2/4, 2/8 and 2/11 battalions `2/9`, plus 2/2 Australian Field Artillery `1/7` and 2/1 Australian Field Artillery `1/11`; the OA legend defines notation as stage/turn. None of those **dated OA arrivals** occurs during GT1–6. | The 6th Australian HQ, 16th and 17th brigades and their listed battalions are `D` and present at setup; do not exclude the division. Scenario setup and track, any corrections or distinct exceptions control actual eligibility. Attachment choices can change shell status without accelerating these printed rows. |
| Later Free French OA rows | Commonwealth OA PDF p. 20 lists 1st Bn d'inf de marine `1/65` and 1st Bn de fusiliers `3/63`; their printed ordinary dated arrivals fall beyond GT6. | These are distinct later rows, not proof that the corrected starting French Motor Marine company is absent or that no lifecycle transition exists. See [marine follow-up](2026-11-17-graziani-marine-transition-followup.md). |

## Detailed-air calendar bounds

Commonwealth chart §34.86, PDF p. 31, restricts its monthly **pilot arrival-table roll** to November 1940 through October 1942 inclusive. The printed table therefore calls for no such monthly roll in this September–October scenario. This does not exclude the initial pilots in §60.46, pilot losses, another source of pilots, or a distinct air procedure. Detailed Air is outside a Land-only profile; this observation does not make an abstract air or fleet phase empty.

Scenario §60.46, PDF p. 4, says construction increasing Malta air-facility capacity may begin in October 1940. Under that explicit restriction, starting such an increase is unavailable in GT1–2 September; October GT3–6 remain conditional. Initial facility capacity and other construction activities are distinct. Check the applicable game mode and construction conditions before using this boundary.

## Source identity and limits

Visually checked [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf), PDF pp. 10, 13, 20, 30–31; [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 4 / printed p. 28; and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p. 3 §60.45. Verified SHA-256 respectively `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`, `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`; Land rules hash `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` matched, with §32.81 and calendar drawn from the linked prior source checks. Matching hashes prove file identity, not rights, independence or exhaustiveness. Recheck scenario overrides, corrections, modes, legal setup and alternative sources before treating any stronger absence as proved.
