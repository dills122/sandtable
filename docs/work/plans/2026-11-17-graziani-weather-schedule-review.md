# Graziani weather schedule: independent source check

**Disposition:** The [schedule proposal](2026-11-17-graziani-weather-schedule-next.md)'s
fall row, dice counts, six-turn calendar and conditional extra die check out.
The previously unverified location mapping is now directly corroborated by the
original common-chart **p. 8 §29.7**. This does not admit the weather effects,
season ruling, map coverage or startup contract. Same-edition prompted reading,
not independent publisher material or an exhaustive errata audit.

## Sources and findings

Visually inspected hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
p. 41 §29.0–.1, [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
pp. 6 and 8 §§29.61 and 29.7, [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 2 §§29.1 and 29.61, [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3 / printed p. 27 §60.22, and [maps](https://spigames.net/PDFv10/CNA_Maps.pdf)
PDF p. 4 Map D turn track. Their SHA-256 values respectively match
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
and `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.

1. **Calendar and season.** Map D's first six boxes read September III,
   September IV, October I, October II, October III and October IV 1940.
   Scenario §60.22 starts GT1 stage 1 and ends GT6 stage 3. Land §29.1 puts
   fall at September III through December II inclusive, so all six turns are
   fall under the selected Land-text boundary. The chart instead assigns
   GT1–12 to its spring row. Errata §29.61 calls that season chart backwards
   and points to §29.1, but its parenthetical starts spring March IV rather
   than Land §29.1 and errata §29.1's March III. The existing project selection
   is an interpretation of this conflicting source, not an official corrected
   full calendar. It is reasonable to preserve that tension and the candidate's
   explicitly hypothetical one-week-shift alternative.
2. **Fall d66.** Common-chart §29.61 prints normal `11–35`, hot `36–54`,
   sandstorm `55–61`, rainstorm `62–66`. On ordered pairs of d6 faces these
   cover respectively 17, 11, 3 and 5 outcomes out of 36; intervals do not
   include invalid digits such as `40` or `57`. Boundary examples `35/36`,
   `54/55`, `61/62` are correct. Those numbers are the row's printed mapping;
   selection of that row for GT1–6 depends on the above ruling.
3. **Extra die and location table.** Land §29.1 says one additional die is
   rolled after a sandstorm or rainstorm result to determine affected sections;
   unaffected sections have normal weather. Common-chart p. 8 is explicitly
   labeled **§29.7 Foul Weather Location Table**, notwithstanding §29.61's
   footnote pointing to §29.62. Its printed mapping exactly matches the prior
   transcription:

   | Additional d6 | Sections |
   | --- | --- |
   | 1 | A, B |
   | 2 | C, D |
   | 3 | D, E |
   | 4 | B, C |
   | 5 | B, D |
   | 6 | B, C, D |

   The table marks D/E with an asterisk: a sandstorm in a Nile Delta hex is
   treated as normal weather. It also says sandstorms do not extend north of
   coastal hexes. Land §29.1 separately names off-map exclusions and treats
   Allied off-map airfields under map E weather; mode applicability remains
   separate. No location die is specified for normal or hot. Over all 18
   stages, assuming each reaches determination, this gives 36 base d6 draws
   plus 0–18 location draws; neither actual draw values nor complete stage
   reachability is established.

## Follow-up and tests

Update future source locators to common-chart p. 8 §29.7; do not treat the
§29.62 footnote as proof of a second conflicting mapping. Independently
version the chosen season boundary and adjudicate delta membership, coast,
off-map and Land-only weather effects. Test all 36 valid ordered pairs,
all six location rolls for both foul types, invalid dice, no location draw
for normal/hot, delta exclusion for sandstorm but not automatically rainstorm,
and stage identity, replay and effects at both GT1/stage 1 and GT6/stage 3.
These are unexecuted design vectors. No code, state or readiness flags changed;
no gameplay tests ran.
