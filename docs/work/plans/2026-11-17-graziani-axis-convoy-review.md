# Simplified Axis convoy lookup: independent source review

**Disposition:** the candidate's 30 printed lane-2/3/6 cells match chart
§32.66. Pair direction and the same-row possibility also match its key. Keep
the threshold, missing-qualifier and historical-reachability gates; add the
remaining lane columns if the matrix is used for October rather than only
the specified pregame September plans. This is a separate source read of the
same scan edition, not an independent edition or official clarification.

## Cell check

Compared every row and each cell in the candidate's ten-row table against
[Axis chart](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF p. 34
§32.66: **30/30 lane-2/3/6 cells agree**, including dashes, and all ten bomb
point labels agree. They are column labels to use on the separate bombardment
table, not numbers to randomize within. A dash is faithfully transcribed, but
the inspected key does not explicitly define its operational outcome.

The source also prints the following other 30 cells. They were not claimed
in the candidate's explicitly September-lane table, so their absence there is
not a transcription error; they matter if a later accepted plan permits lane
1, 4 or 5 in October. Their inclusion here does not establish those lanes'
actual availability or shipments.

| Bomb points | Lane 1 | Lane 4 | Lane 5 |
| --- | --- | --- | --- |
| 21–40 | Cxx01 | Exx01 | Exx11 |
| 41–80 | Bxx16 | Dxx21 | Exx01 |
| 81–120 | Bxx01 | Dxx11 | Dxx21 |
| 121–160 | Axx18 | Dxx01 | Dxx11 |
| 161–200 | — | Cxx21 | Dxx01 |
| 201–260 | — | Cxx11 | Cxx21 |
| 261–320 | — | Cxx01 | Cxx11 |
| 321–390 | — | Bxx26 | Cxx01 |
| 391–470 | — | Bxx20 | Bxx26 |
| 471+ | — | Bxx10 | Bxx20 |

## Qualifier, threshold and stage

- Chart §32.66 defines a qualifying division as a complete, i.e. non-shell,
  Allied division **east of or in the same east–west hexrow as at least one
  other Allied non-shell division**. The candidate preserves both non-shell
  conditions and the direction. A lone complete division is insufficient;
  with exactly two non-shell divisions in different rows, the eastern one
  qualifies and the strictly western one does not on that pair alone. With
  both in the same row, each meets the chart's pair relation. Select the
  westernmost *qualifying* division, not necessarily the westernmost division.
  This is a source relationship, not a claim that the setup supplies either
  division or that chart position is known.
- Strengthen the candidate's shell test by citing **both** Land §§9.26 and
  9.27. A division is a shell when 50% or less of the total brigade-equivalents
  that may be assigned to it are attached; §9.27 says an attached shell does
  not count among the necessary lower-level units. Current assigned count,
  physical counter count and a printed division HQ label cannot substitute for
  the allowed total and eligible actual attachments. Test the 50% boundary,
  a non-shell versus shell attached brigade, detachment and unknown status;
  evaluate the full organization rules before asserting a specific formation's
  qualification.
- The chart directs reading down the lane to the westernmost hexrow **beyond
  which** a qualifying division is situated; its `xx` denotes any north–south
  hex while the suffix identifies the east–west hexrow. It does not define an
  explicit equality comparator in these passages. The candidate correctly
  defers exact threshold equality, ordering across map sheets, no qualifying
  pair, out-of-range positions and dash behavior. None is safely converted to
  zero bomb points, the first row, or a free extra die. Test equality and both
  sides of each selected threshold, not just between-row samples.
- Land §§32.63–.64 put the optional Commonwealth attack in the Naval Convoy
  Phase of the OpStage and resolve each arriving convoy separately, independent
  of map weather. Land §5.2 places Naval Convoy Arrival before player A/B
  reserve and movement blocks. No inspected rule conditions this attack on
  which player is phasing in a later movement block; do not add such a
  restriction or use positions reached later in that stage. Define the exact
  authoritative attack snapshot and timing within the naval phase before
  implementation. The candidate's “same source procedure runs in each stage”
  should be read as a **conditional opportunity**: no actual arriving convoy
  or a declined attack is not an automatic bomb lookup or an extra attack.

Scenario §60.37's lanes 2, 3 and 6 apply to its pre-start planning of runs
for the remainder of September; neither those choices nor §60.22 prove an
actual convoy in every turn or every OpStage. Do not extend that specific
restriction to October without an applicable rule. The source says the Axis
announces lanes used and the Commonwealth may attack each arriving convoy;
preserve private cargo and other independently applicable disclosures. The
candidate properly treats initial division locations as a starting snapshot,
not a proof of non-shell status or a qualifying pair.

## Suggested checks before admission

- Verify all six route columns against their printed labels, then test each
  candidate threshold at equality and on either side after adopting a direction
  policy. Exercise top/bottom and dash cells, missing and multiple qualifiers,
  cross-sheet ordering, unknown coverage and no qualifying pair; no unreviewed
  default may become a bomb-point result.
- Test two non-shell divisions in a common row, two at different rows, a lone
  division, a shell support, and 50% versus above-50% attached non-shell
  brigade-equivalents. Include a shell brigade that is attached but excluded
  by §9.27, and checkpoint a formation change before the naval snapshot.
- For both A/B initiative selections, use identical valid naval snapshots and
  check the same attack availability and chart lookup. Compare declined attack,
  absent arrival, independently attacked convoys and map weather; then use
  exactly the separate sequential bombardment dice for each chosen attack.
  Test hidden-state projection and replay independently of results/loss-group
  policy.

## Sources and limits

Visually inspected [Axis chart](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF p. 34 §32.66 (SHA-256
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`),
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 12, 17 and 44, §§5.2, 9.21, 9.26–.27 and 32.61–.64
(`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`), and
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3 / printed p. 27 §§60.22 and 60.37
(`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`).
Compared the [candidate](2026-11-17-graziani-axis-convoy-next.md). This is not
an exhaustive errata, map, cargo, formation or attack-loss audit; no gameplay
tests ran and no runtime changes were made.
