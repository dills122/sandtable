# Graziani setup reconciliation: Matruh, 6th NZ and initial supply

**Status:** decision support, not approved scenario data. This pass connects
existing source observations to implementation decisions. The independent
11-row pilot verifies bounded transcriptions, not the working rulings; no new
independent reader or rights clearance is claimed here.

| Question | Evidence and planning disposition | Remaining admission and test obligations |
| --- | --- | --- |
| Matruh assignment | Scenario §60.41 lists the garrison and three attached units; errata §4.44b assigns two named units; the OA says no assigned units. RUL-GRA-03 treats the specific erratum as an exception. | Retain separate assignment and attachment. Resolve setup reorganization, six-slot treatment, co-location, HQ close-assault exception and unit identity. Verify source and chosen policy, then test cost, capacity, ownership, replay and duplicate rejection. |
| 6th NZ | The reinforcement track and brigade OA place HQ and three battalions at turn 6/stage 3; the separately listed artillery is turn 2/stage 3. Land §20.11's example puts both on turn 2. RUL-GRA-04 follows the track and OA for planning. | The 11-row independent transcription covers these printed conflicts, not a second edition or ruling acceptance. Check errata and the remaining arrival roster, placement, training and movement; bind accepted result to setup/rules identity and prove timing, duplicates and replay. |
| Axis initial supply | Scenario §60.92(B) replaces §60.34. Printed pp. 28–29 list fixed units and two player-placed dumps; each dump's units share one eligible map-C Libya hex, with a Commonwealth proximity restriction. A dash appears for Derna ammunition. | This session rechecked the original pages against the [pilot](2026-11-12-graziani-supply-accounting-pilot.md); it is the same research stream, not independent double entry. Do not decide the dash is zero; verify map identities, placement, dummies, additional loads, supply conservation, rights and errata. |
| Commonwealth initial supply and repair | §60.92(C) states that §60.44 is replaced and lists the Land-only supply; §60.44 also includes a Matruh temporary repair facility. RUL-GRA-01 retains that separate facility for planning, based on the supply-specific replacement heading. | Literal replacement of the whole numbered case is a real alternative. Do not encode the facility until reviewed; independently check source, repair spending in abstract logistics, facility type/control, replacement-vs-addition and save identity. |

The visual supply recheck used local scenario PDF SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
PDF pages 4–5 / printed pp. 28–29. It reconfirmed the 13 fixed row values,
including the printed dash, and the descriptions of dummy groups; it did not
verify the map, legal placement, trucks, errata, provenance or ownership rights.
The arithmetic checker recomputes totals from the existing research JSON and is
not a second source transcription.

## Immediate implementation boundary

The [laboratory contract](../../specs/scheduled-arrival-lab-v1.md) specifies a
synthetic obligation exercise. It cannot approve historic source data or upgrade
current campaign support. Current stage-entry events hard-code initial version and
stage, while `CampaignSnapshotValidator` relates early sequence position to state
version; simply inserting an event would invalidate or reinterpret old histories.
A new admitted lifecycle needs versioned creation/setup and event/snapshot readers,
a complete non-land obligation policy, actual world creation, and replay/host
receipts before it can be considered campaign authority.
