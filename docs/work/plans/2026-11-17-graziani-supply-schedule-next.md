# Graziani abstract supply: six-turn planning and arrival matrix

**Status:** source-based planning matrix; not an adopted ruling, generated
schedule of actual deliveries, or implementation. Die results, pregame plans,
unused allotment and transport outcomes remain distinct unknowns. Source
rechecks use the same edition as prior research, not an independent edition.

## Source rules and calendar

Land §§32.41–.45 (printed/PDF pp. 43–44) give each side scenario starting
supply and additional supply from its availability table. At the beginning of
each Game Turn players plan, Axis two turns ahead and Commonwealth four. Players
may plan as far ahead as needed for possible arrivals on all turns **except the
starting turn**, if die rolls permit. This is a planning/arrival exception, not
proof of a zero setup allocation, automatic starting-turn table arrival, or
positive delivery on every subsequent turn. §32.44 chooses the Axis availability
letter using the **planning month** and determines how many units the player may
bring in two turns hence; §32.45 chooses the Commonwealth period using the
**arrival date**, four turns hence. Both prescribe one die for their respective
table lookup. How to initialize earlier plans, including their exact dates and
rolls, is not specified in the inspected passages.

Scenario §60.22 runs GT1 OpStage 1 through GT6 OpStage 3. Map D's turn track
shows GT1 September III, GT2 September IV and GT3–6 October I–IV, 1940.
Scenario §60.37 requires Axis pregame planning of convoy runs for the
remainder of September, with lanes 2, 3 and 6. That instruction addresses
convoy runs; it does not explicitly establish each side's abstract supply
availability roll, its date, or all October plans. Scenario §60.92(B)/(C)
replaces detailed initial supply with specified Axis and Commonwealth supply
units and dummy groups. These are **initial state**, not a new table roll or
proof that a later shipment arrived; reconcile any additional setup under
§59.66(C) independently.

## Arrival-side matrix

The normal lead-time column gives the arithmetic planning slot for that
arrival. `Pregame` means the slot falls before GT1, **not** that a particular
negative-numbered turn, planning date, die or recorded plan has been proved.
`B?` means B only if a permissible **September 1940** planning date is chosen;
the pregame date is unresolved. Commonwealth period I is conditional on an
applicable roll for the 1940 arrival. Availability is not delivered quantity.

| Arrival GT | Date | Axis ordinary slot / table input | Commonwealth ordinary slot / input | Source-supported status |
| --- | --- | --- | --- | --- |
| 1 | Sep III | Starting-turn exception | Starting-turn exception | §60.92 setup; no routine table arrival inferred. Separate §60.37 pregame convoy scope remains open. |
| 2 | Sep IV | Pregame / B? | Pregame / period I | Later-turn planning allowed; exact initialization and rolls unknown. Axis September convoy runs require pregame plans. |
| 3 | Oct I | GT1 Sep III / B | Pregame / period I | Axis GT1 is the arithmetic offset, but whether its beginning-of-turn plan occurs at startup is unresolved; Commonwealth pregame initialization is also unresolved. |
| 4 | Oct II | GT2 Sep IV / B | Pregame / period I | Axis GT2 lookup is the ordinary offset; Commonwealth pregame initialization unresolved. |
| 5 | Oct III | GT3 Oct I / B | GT1 Sep III / period I | Axis slot is during play; whether the Commonwealth's GT1 plan is played, initialized or skipped remains unresolved. |
| 6 | Oct IV | GT4 Oct II / B | GT2 Sep IV / period I | Both ordinary slots during play. |

For each applicable roll of 1–6, Axis §32.46 column B gives **0, 1, 1, 2,
2, 3**, and Commonwealth §32.47 period I gives **1, 1, 2, 2, 3, 3**.
The Axis §56.4 chart prints B for September and October 1940. Commonwealth
period I covers 1940 through April 1941. These are conditional outcomes, not
actual rolls; other Axis pregame months, optional chart variants, scenario
exceptions and missing plans must not be silently assigned B. September
errata §56.29 offers an optional reordering of convoy-level letters; whether
that detailed-game option is accepted and applicable to §32.44 must be decided
before relying on a fixed B under that option.

## Planning-side matrix and cutoff

The following is the normal §32.43 arithmetic offset, not a claim that every future roll
has present effects or must be processed after scenario termination. §32.43's
“at the beginning of each Game-Turn” creates a prima facie planning obligation
through GT6; it does not explicitly waive future-target planning in a short
scenario. Whether the engine may omit plans for targets beyond GT6 is a
scenario/terminal policy decision, not a printed empty case. No roll is selected
here. Scenario §60.22 starts at GT1 OpStage 1, so whether its beginning-of-turn
planning actually occurs at startup or is initialized as pregame is a separate
unresolved entry-point decision. Target dates GT7–10 are November I–IV on the
printed track.

| Planning GT | Axis target / planning-month letter | Commonwealth target / arrival-period |
| --- | --- | --- |
| 1 Sep III | GT3 Oct I / B | GT5 Oct III / I |
| 2 Sep IV | GT4 Oct II / B | GT6 Oct IV / I |
| 3 Oct I | GT5 Oct III / B | GT7 Nov I / I |
| 4 Oct II | GT6 Oct IV / B | GT8 Nov II / I |
| 5 Oct III | GT7 Nov I / B | GT9 Nov III / I |
| 6 Oct IV | GT8 Nov II / B | GT10 Nov IV / I |

Pre-start planning for later arrivals is additional to the above regular
slots and may affect which rolls are initialized at setup. The count and order
of those draws, secrecy and sequencing are undecided. Do not invent historical
pre-GT1 track entries or duplicate a plan when reconciling pregame §60.37.

## Availability, placement and carryover decisions

- Axis table describes number available for that Game Turn; §32.44 says the
  player *may* bring in the result, and §32.42 sends units by convoy. §32.61–.65
  govern simplified convoy cargo, lanes and possible attacks; available does
  not mean embarked, landed, or intact. Decide whether an unused entitlement
  expires, can be deferred or can be split across stages, and how a lost or
  delayed shipment is represented. Do not equate unused capacity with a
  delivered supply unit or reroll it.
- Commonwealth §32.42 defaults placement to Cairo. When **more than three**
  are received in one Game Turn, one per three received *may* be placed in
  Alexandria. Floor of complete groups is a plausible proposal, not a stated
  rounding rule. Decide whether this ratio includes earlier receipts, how to
  place before a later same-turn receipt, and any deferred placement. The
  printed 1–3 result for one period-I roll alone gives no Alexandria option;
  this does not prove a turn's total cannot exceed three if other authorized
  receipts or carryover exist. Starting scenario supply already in Alexandria
  is a separate placement, not evidence of table-receipt eligibility.
- A supply unit already placed persists subject to expenditure, movement,
  capture, destruction and other rules; this is not evidence that an unused
  *availability result* carries forward. Preserve source, original target,
  status, actual delivery, remainder and state separately. The inspected text
  gives no settled general policy on expiration or carryover of undelivered
  availability. Missing plans and unrolled tables remain unknown, not zero.
- §32.31 permits land or rail movement in the arrival OpStage subject to
  relevant transport restrictions; it does not itself determine exact arrival
  stage, stacking, loss or all placement timing. Decide those independently.

## Validation and source scope

For each in-scenario target, verify correct planning slot and table input;
exercise every die row, actual Axis B zero versus missing/unrolled state, and
GT2 pregame unknowns. Verify October Axis plans still use the planning month,
Commonwealth uses the arrival period, and changing an arrival across a period
boundary changes only the latter lookup. Test setup versus delivery, multiple
receipts at Alexandria thresholds 3/4/6, no duplicate rolls or arrivals,
undelivered availability versus persisted on-map inventory, convoy loss,
terminal cutoff and replay of private plans. These are proposed vectors, not
executed tests or determinations of historical outcomes.

Visually checked hash-matched originals: [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 43–44 (`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`);
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 3–5 / printed pp. 27–29 (`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`);
[Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF pp. 34, 36
(`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`);
[Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf) PDF p. 31
(`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`);
and [maps](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 4
(`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`).
Searched extracted text of [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
(`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`)
for §§32.41–.47 and supply/convoy/scenario terms; §32.0 warns abstract rules
were untested, §56.29 offers an optional convoy-level variant, and no explicit
correction to these planning horizons appeared in that bounded text search.
This does not exclude OCR omissions, other relevant cases or later errata.
Related prior analysis: [planning packet](2026-11-13-graziani-supply-planning.md)
and [logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md).
An [independent source review](2026-11-17-graziani-supply-schedule-review.md)
confirms the chart rows, calendar and offset arithmetic, and identifies the
GT1 startup qualification incorporated above.
