# Land-only anti-air lifecycle: source review

**Disposition:** The source categories, GT2 earliest ordinary Italian light-AA
planning and GT4 nominal arrival arithmetic in the [candidate](2026-11-17-graziani-aa-lifecycle-next.md)
match the inspected original. The one-per-month rule remains a timing and
allocation decision; its printed “effectively halving” parenthetical creates
an additional ambiguity that should be preserved. No actual arrival, loss,
starting point count or readiness follows from the arithmetic.

## Findings

1. **Production row and chronology check out.** Axis Italian Production
   Chart §20.66, PDF p. 33, prints Light AA `15 / 2 / 2…24 / 6`, then
   `45 / 1 / 25` in Number/Max/Dates/Tonnage columns. Its key says the date is
   the first Game-Turn when the item may be **planned**, arriving two turns
   later. Thus GT2 is earliest ordinary plan and GT4 earliest nominal arrival;
   GT2→4, GT3→5, GT4→6, GT5→7 and GT6→8 are correct. The chart's own example
   describes the first 15 Light AA points as brought in during GT4–26, which
   is consistent with the two-turn offset. A plan does not prove an eligible
   convoy, successful receipt, chosen arrival OpStage or source-correct
   six-turn state. Scenario GT1 entry and §60.37's pregame convoy instruction
   require separate initialization handling; they do not by themselves prove
   a GT1 plan despite the chart's GT2 starting date.

2. **Month convention and “halving” are not fully reconciled.** Map D's turn
   track, map PDF p. 4, shows GT1–2 September III–IV, GT3–6 October I–IV and
   GT7–10 November I–IV. Land §32.83(5), printed/PDF p. 45, says light-AA
   replacements may be brought in at **one per month**, adding that this
   effectively halves the number available. Axis chart §20.66 gives the
   earlier Light AA row a maximum of **two per Game-Turn**, with its key using
   “planned/brought in.” A literal monthly rate, a halving of a different
   availability measure, and their interaction with the chart do not
   transparently yield the same quantity. Preserve both the rate and the
   parenthetical as a source tension; do not convert the parenthetical into a
   proven one-per-turn or one-month entitlement. The candidate correctly
   selects no automatic receipts and distinguishes planning-month from
   receipt-month caps; these are alternatives requiring explicit precedence
   and a versioned decision.

3. **The proposed September/October examples are conditional.** Under an
   accepted two-turn schedule, one plan in GT2 and one in GT3 could nominally
   arrive GT4 and GT5, both October. If the chosen rule debits *accepted
   plans* by planning month, the two planning months differ; if it debits
   *receipts* by arrival month, the two October receipts exceed a one-per-month
   cap. This is a distinguishing synthetic case, not proof both plans or
   receipts are legal. The candidate's final test says the cap counts
   authorized planned or received points “not merely attempts”; the original
   §32.83(5) does not decide whether shipment scheduled, embarked, attempted,
   sunk or successfully received consumes a rate allowance. Leave failed or
   attempted convoy treatment open until the chosen event and loss policy
   specifies it. No-roll/no-plan, zero, lost shipment and suppressed arrival
   must remain distinct.

4. **Nationality, point type and event timing remain essential.** Land
   §§32.81–.84, printed/PDF pp. 44–45, match the candidate's key distinctions:
   Commonwealth AA units, light/heavy AA TOE and AA replacement points are
   removed; captured Axis AA TOE is immediately removed; German `z`/`bb` units
   and light AA categories are removed but heavy AA TOE and replacements are
   unaffected; Italian `ss`/`tt`/`uu` units are removed, `pp` emplaced guns
   lose assigned AA points, `kk` artillery loses assigned **heavy** AA TOE,
   Italian heavy AA replacements are lost, and Italian light AA is rate
   limited. §32.84 explicitly preserves non-AA TOE/replacement points that
   merely have AA ratings. Those clauses do not expressly define an event hook
   for every future reinforcement or reverse capture. Do not infer that an
   Italian `kk` artillery unit or all its light/non-AA payload disappears, or
   apply the Italian replacement rule to German heavy AA.

5. **Scenario restrictions have side and scope.** Scenario §60.47,
   printed p. 28 / PDF p. 4, occurs in the Commonwealth forces section and
   bars **Commonwealth** replacement-point arrivals before November. It does
   not automatically forbid Italian GT4/GT5 shipments. The ordinary Axis land
   reinforcement schedule §4.43b, Axis charts PDF p. 2, begins at GT9 OpStage 2;
   that bounds whole-unit track entries, not replacement production or every
   scenario-specific exception. The Commonwealth track's six in-scenario
   rows do not expressly name an AA unit; constituent OA and errata still need
   their own check. Scenario §60.41's 9th Heavy AA, 15th Light AA and 57th
   Light AA counters are named starting units subject to §32.81; a full count
   of all assigned AA TOE or future categories was not established here.

6. **Errata scope stays bounded.** September errata PDF p. 2 gives a §20.66
   tank-weapon correction and §32.0 warns that abstract rules were untested;
   it does not explicitly choose the light-AA month event in the inspected
   pages. This does not exclude OCR gaps, scenario overrides or later
   corrections. Do not transform the absence of a named correction into an
   official lifecycle rule.

## Acceptance vectors

- Preserve the chart's `2…24` **planning** dates separately from nominal
  GT4–26 arrivals. Reject a synthetic ordinary GT1 plan under the stated chart
  policy; check GT2→4, GT3→5 and GT4→6 with actual capacity and state still
  required. Neither missing plans nor failed receipts become automatic zero or
  automatic one-point grants.
- Compare receipt-month, accepted-plan-month and any separately proposed
  shipment/attempt-counting policies using GT2 and GT3 plans and their two
  October targets. Include a sunk shipment, cancelled plan, unused September
  allowance, retry and no-rollover versus rollover; no unselected alternative
  should be silently presented as source fact. Keep the “halving” tension in
  the immutable policy rationale.
- Test `pp` retaining the emplaced gun after AA-point removal, `kk` retaining
  light and non-AA artillery when only heavy AA is removed, German heavy AA
  unaffected, Commonwealth AA categories removed, and non-AA points with AA
  ratings preserved. Compare capture directions and a scheduled later unit
  under one-time versus continuous-removal policies; count no unproven unit.
- Scenario §60.47 bars pre-November Commonwealth replacement receipts;
  independently verify any exception and do not apply that bar to Axis merely
  by side confusion. Journal classification, event timing and source identity
  without leaking hidden equipment or rerolling a blocked convoy.

## Sources and scope

Visually inspected hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF pp. 44–45, §§32.81–.84, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF pp. 2
and 33, §§4.43b and 20.66,
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`;
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF pp. 3–4 / printed pp. 27–28,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[map package](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4 turn track,
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
PDF p. 2,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
The prior Commonwealth-track reading is not freshly double-entered here.
This informed same-edition reread did not exhaust all charts, errata, captured
state, detailed-logistics overrides or actual game events. No runtime/data
changed and no gameplay tests ran.
