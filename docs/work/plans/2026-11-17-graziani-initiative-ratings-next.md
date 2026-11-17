# Initiative ratings and turn sequence, Graziani GT1–6

**Finding:** Commonwealth rating is **3** throughout GT1–6. The Axis chart
is location- and unit-dependent, with rating 1, 3 or 6 under its stated
conditions; it is not a fixed nationality value. Scenario §60.6 gives Italy
initiative throughout GT1 and begins the normal initiative determination at
GT2. The actual GT2–6 winner and each stage's first player need recorded game
state and choices.

## Original rules and chart

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF p. 13 §§7.11–.16 and p. 12 §5.2, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf),
PDF/printed p. 2 §7.2,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`;
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF p. 4 / printed p. 28 §60.6,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
September errata PDF pp. 1–3,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
no explicit correction to §7.1/.2 or §60.6 found in this bounded inspection.

| Situation | Printed rating | Scope |
| --- | ---: | --- |
| Commonwealth GT1–42 inclusive | 3 | Includes every GT1–6 turn; later chart rows begin GT43 and GT91. |
| Axis Rommel counter on Game-Maps | 6 | Chart footnote excludes Tripoli/Tunisia holding boxes on Map A from Game-Maps for this purpose. |
| Axis German **land combat units** on Game-Maps but no Rommel counter there | 3 | Applies to qualifying presence, not any German item or nationality label alone; same holding-box exclusion. |
| Axis no German combat units or Rommel counter on Game-Maps | 1 | Safest direct witness: neither present. Chart's second row specifies *land* combat units while this last row says combat units; unusual non-land-only situations and interpretation of “or” need separate adjudication. |

These rows are mutually distinguishable for the stated witnesses; the chart
does not determine whether any particular counter or current position exists.
Scenario setup §60.31 lists Italian forces, but subsequent movement, arrival,
loss and any setup additions must be checked before asserting each GT2–6
snapshot. Do not count a German unit or Rommel solely because it is in the
Tripoli/Tunisia holding boxes. General §3.23 and relevant attachment rules
inform “land combat unit”; a nationality marker or empty HQ alone is not a
verified German land combat witness. Rommel's own §31.0 contains an additional
absence/initiative procedure if applicable; no Rommel presence or associated
roll is inferred in these six turns. The [independent review](2026-11-17-graziani-initiative-ratings-review.md)
checks §31.0's separate two-die absence roll and the resulting initiative 3 on
a 12; do not apply chart value 6 unconditionally or require an additional
German-unit witness for that explicit reduction. Ordering and return remain open.

## Timing and choices

Scenario §60.6 fixes the Italian Player as initiative holder for **the entire
first Game-Turn**. Section 60.22 starts at GT1 OpStage 1; no random first-turn
initiative contest is needed to establish that holder, regardless of whether
a separate startup policy admits any other turn preamble. Beginning GT2,
§7.12–.14 determine initiative once at the beginning of each Game-Turn:
each player rolls one die and adds that side's applicable initiative rating;
higher total wins, ties roll again. Determine both ratings from the relevant
state at that decision, not from one player's die or automatic historical
winner. The winner keeps initiative through all three OpStages, but §7.11–.12
and §5.2 allow that holder to choose first or last **in each stage**. §7.16
calls the chosen first player A and the other B. Holder and first player are
therefore distinct, and A/B can change between stages without a new initiative
contest. Preserve exact choices and their valid decision timing; do not assume
GT1 Italian initiative means Italy always moves first.

Dates are GT1 Sep III, GT2 Sep IV, GT3–6 Oct I–IV. All six use the CW 3
chart row. GT1's fixed Italian holder does not establish its die or require a
chart-derived winner; GT2–6 use the applicable decision-time Axis rating and
dice. Source does not supply actual rolls, future counter locations, game-state
changes, or the winners of those contests. If a tie occurs, the source says
roll again; deterministic implementation must record each accepted draw and
chosen replay procedure without silently treating a tie as incumbent victory.
It must not redraw already committed draws after retry.

## Conditional checks

1. With a valid no-German/no-Rommel on-map snapshot in GT2, Axis = 1 and
   Commonwealth = 3. Axis die 4 versus CW die 1 yields 5 versus 4, so Axis
   wins, matching §7.14's October example. Axis die 3 versus CW die 1 yields
   4 versus 4, so roll again. These are synthetic rolls, not actual events.
2. Rommel on map gives 6; qualifying German land combat unit on map without
   Rommel gives 3; neither qualifying presence gives 1. Repeat with a
   qualifying piece in a Tripoli/Tunisia holding box only: it does not satisfy
   the on-map witness. Test an ambiguous non-land piece as unknown rather than
   silently classifying it as a land combat unit.
3. GT1 holder is Italian without using a contest. If Italian chooses last in
   OpStage 1, Commonwealth is A and Italy B for that stage; if it chooses
   first in OpStage 2, Italy is A and Commonwealth B. Do not redraw the
   initiative contest between these stage choices.
4. GT2 tie followed by a non-tie: retain both pairs of recorded dice and the
   resulting holder once, including across retry. Reject malformed die values
   and a second declaration for an already committed stage; preserve privacy
   and visibility under separately accepted source contracts.

No full scenario availability audit, actual die, player decision, outcome, or
runtime change is claimed. Same-edition source review; later clarification
and edge cases remain open.
