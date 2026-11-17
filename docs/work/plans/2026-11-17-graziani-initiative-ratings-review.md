# Graziani initiative ratings: source audit

**Disposition:** [initiative note](2026-11-17-graziani-initiative-ratings-next.md)
correctly transcribes the chart, fixed first-turn holder, and per-stage choice.
Its summary/table should not be read as a complete decision procedure where
Rommel's §31.0 absence rule applies. No actual first-six-turn German arrival,
Rommel presence, die, stage order or winner was proven in this review.

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 12–13 and 42 §§5.2, 7.11–.16, 31.0, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
printed/PDF p. 2 §7.2, SHA-256
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`;
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
printed p. 28 §60.6, SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
No separate errata, reinforcement-track or scenario-start-date audit performed.

## Checked boundaries

* §7.2 lists Commonwealth rating 3 for Game-Turns 1–42 inclusive; thus GT1–6
  are within that row. It lists Axis 6 for Rommel counter on Game-Maps, 3 for
  German **land** combat units but no Rommel counter on Game-Maps, and 1 for
  no German combat units or Rommel counter on Game-Maps. For those chart rows,
  the footnote excludes Tripoli/Tunisia holding boxes on Game-Map A. The
  difference between “land combat units” in the 3 row and “combat units” in
  the 1 row, and the grammar of “or,” justify retaining uncertainty for
  unusual non-land pieces; neither should be silently treated as a qualifying
  German land combat unit. Presence must be established at the relevant time.
* **Rommel condition:** §31.0 says at the beginning of each Game-Turn Rommel
  is present, the Axis player rolls two dice; a 12 means Rommel is in Germany
  and Axis initiative falls to 3 for that Game-Turn. It says he returns next
  Game-Turn unless another 12 is thrown. The source's interaction with the
  chart and exact ordering/return mechanics need review before coding. Do not
  turn “Rommel on Game-Maps = 6” into an unconditional post-§31.0 result or
  claim the reduced 3 requires an additional German combat-unit witness.
  This is a separate conditional roll from the two sides' initiative contest;
  neither is proof that Rommel exists during GT1–6.
* §60.6 fixes the Italian player as initiative holder for the entire first
  Game-Turn and says normal determination begins with the second. Hence no
  GT1 initiative-contest draw is needed; this does not establish that every
  other potential startup procedure or conditional roll is skipped. From GT2,
  §7.14 has each player roll one die and add their rating; higher total wins,
  ties reroll. The 4+1 versus 1+3 example gives 5 versus 4, as transcribed.
* §§7.11–.12 and §5.2 let the holder choose first or last separately for each
  of the three Operations Stages; §7.16 calls whoever moves first A. The fixed
  Italian holder is not a forced first player, and changes of A/B do not
  require another Game-Turn initiative contest.

## Conditional test vectors

1. GT1 Italian holder without a contest; choose Italian last in stage 1 and
   first in stage 2. Preserve two stage choices and a single holder.
2. GT2 with no qualifying German unit or Rommel anywhere on Game-Maps: Axis 1,
   Commonwealth 3; 4 versus 1 on the dice yields 5 versus 4. A tie rerolls;
   persist both pairs without repeating accepted draws after retry.
3. German land combat unit only in an excluded holding box versus actual
   Game-Map; Rommel on Game-Map with no absence result versus §31.0's roll of
   12. Keep the latter as a distinct conditional exception rather than blindly
   reporting 6; resolve return and rule ordering before applying it.
4. German non-land/ambiguous counter and incomplete location coverage: avoid
   inventing a rating. Verify scenario unit eligibility and timing separately.

No runtime changes or gameplay tests; examples are synthetic, not scenario
reachability claims.
