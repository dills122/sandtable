# Graziani abstract fleet and anti-air: source review

**Correction:** Commonwealth fleet chart §30.6 allows **any two of the seven
listed destroyers** to deploy at Valletta, Malta—not one. The separate cruiser
footnote allows any one of the three marked cruisers there. With no other
exception and treating “may” as optional, 8–11 of the 11 listed initial ships
start at Alexandria; actual assignments and later presence remain choices and
state. Correct the [source inventory](2026-11-17-graziani-abstract-aa-fleet-next.md)
and its test cases before admission. The discarded scenario list must not be
used to replace the chart's footnotes.

## Source boundary

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 12, 42, 44–45 §§5.2, 30.34–.37, 32.71–.84; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.45; [Commonwealth chart](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 31 §30.6; [common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
p. 12 §41.5; and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 3 §§41.5 and 60.45. SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`,
and `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Prompted inspection of the same edition; no complete naval, weather,
reinforcement or errata audit and no independent physical edition.

## Findings

1. **Fleet list and retained setup.** §30.6's initial list is Valiant;
   Arethusa, Dido and Euryalus; Airedale, Bedouin, Jervis, Lance, Ledbury,
   Mohawk and Nubian. One marked cruiser **and two** marked destroyers may
   separately be placed at Malta; the chart directs other ships to Alexandria.
   It lists the next fleet reinforcement at `3/8`, after the six-turn scenario.
   Errata §60.45 retains the first paragraph of the scenario fleet case and
   discards text beginning “The following units are available.” The retained
   paragraph refers to the Fleet Schedule, says initial ships need no repairs,
   and prohibits moving them until GT2 OpStage 1. Initial deployment choice is
   not a later movement. The §30.6 location and §60.45 timing do not by
   themselves establish the precise target set at every later attack phase.
2. **Anti-air categories.** §32.81 removes Commonwealth anti-air units and
   light/heavy AA TOE and replacement points, and immediately removes captured
   Axis AA TOE points. §32.82 removes German `z`/`bb` AA units plus assigned
   light AA TOE and light AA replacement points, while leaving heavy AA TOE
   and replacement points unaffected. §32.83 removes Italian `ss`/`tt`/`uu`
   AA units; AA TOE points on `pp` emplaced-gun units; assigned heavy AA TOE
   points on `kk` artillery units; and heavy AA replacements, with light AA
   replacement points limited to one per month. §32.84 protects **non-AA** TOE
   and replacement points that happen to have an AA rating. These statements
   are category-specific: do not delete all of a `pp` or `kk` gun unit or
   assume a different nationality follows the German row. Initial versus
   future-arrival lifecycle, source type and specific units remain independent
   acceptance gates. The source also parenthetically characterizes the Italian
   light replacement limit as effectively halving available points; how that
   sentence interacts with monthly supply and timing needs review, not an
   invented extra point.
3. **Axis abstract attack.** §32.72's secret single die sets the number of
   permitted attacks for the scenario, with +2 **if** it is a Campaign Game;
   §32.74(A) separately rolls a die for the bomb-point column and also says +2
   if Campaign. Do not conflate these two dice, apply the modifier without a
   profile decision, or treat the scenario allowance as a fresh per-stage
   allowance. §32.73 permits at most one attack per OpStage, plotted in the
   Commonwealth Fleet Assignment Segment, with execution in the Tactical Naval
   Movement Segment. The latter segment is not separately named in the §5.2
   Land outline; preserve the sequence-mapping question. §32.75 bars an attack
   when **section E's weather** is rainstorm or sandstorm. Do not substitute
   a ship's local-hex status, infer a delta exception to the section-level
   attack rule, or assume a weather result without resolving its scope.
4. **Chart sample and damage.** In the 1–20 bomb-points column, §41.5's
   Commonwealth Fleet row reads 0 for sequential `11–62` and 1 for `63–66`;
   thus non-Campaign column die `1`, then `62 → 0` and `63 → 1` checks out.
   Sequential d6 digits must each be 1–6. Errata §41.5 changes the barrage
   header and flak terminology; it does not expressly change those bomb-point
   cells. §32.74 distributes resulting damage points as the Axis player
   desires among ships in Alexandria Harbor; resolve empty harbor and excess
   allocation separately. §30.34 takes ship damage from Gun Rating and its
   example leaves AA unchanged, but its last sentence refers to reduced Gun
   **and AA** ratings if any; do not generalize the example into an independently
   proven universal rule that AA rating can never be reduced. §30.36 distinguishes
   falling below half from repair to better than half, so the equality case
   warrants the candidate's explicit gate. §30.37 describes six consecutive
   Alexandria OpStages for one Gun Rating level, restarting if the ship moves
   early; ordering with an attack and repair needs a stage contract.

## Negative checks before admission

Test zero, one and **two** selected Malta destroyers, an invalid three, zero or
one Malta cruiser and invalid two; verify remaining initial vessels against
§30.6 and don't import discarded §60.45 rows. Exercise every nationality/type
AA boundary, current versus future material, a non-AA point with an AA rating,
and unknown classification. Test both Campaign-dependent dice separately,
per-scenario allowance versus one-per-stage limit, clear/storm E, delta/section
scope, empty harbor, hidden planning, `1/62` and `1/63`, actual target
allocation, ship damage and exact 50% repair state. These are unexecuted
vectors and not six-turn reachability evidence. No runtime or readiness changes;
no gameplay tests ran.
