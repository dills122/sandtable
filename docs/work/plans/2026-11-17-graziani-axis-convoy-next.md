# Graziani simplified Axis convoy bombing: six-turn lookup boundary

**Status:** bounded source transcription and conditional matrix, not an
admitted combat rule, actual convoy plan, proven table result or six-turn
reachability certificate. No runtime or readiness change. The table depends on
positions and non-shell status **at the relevant attack**, not simply the
scenario's initial HQ labels. Unresolved no-qualifier, threshold, position,
cargo and loss questions remain gates.

## Source and procedure

Visually checked hash-matched [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF p. 34 §32.66, [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 17 and 44 §§9.21, 9.26, 32.61–.65, [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3 / printed p. 27 §§60.22, 60.31, 60.37, 60.41, and
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 3 §56.25. SHA-256 values respectively
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
and `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Same-edition scans, not independent editions; no exhaustive survey of later
errata, map geometry or all formation corrections.

Land §32.61 includes the Axis turn's Supply Units, Motorization Points and
Replacement Points in simplified naval convoys; items can be distributed
among available lanes without a tonnage limit. §32.62 caps each lane in one
Naval Convoy Stage at three Supply Units and 20 Motorization Points, with
replacement-pool limits separate. §32.63 has the Axis announce lanes used;
the Commonwealth may attack each arriving convoy separately regardless of
**map weather**. This does not prove every lane has cargo, that an attack is
mandatory, or that unrelated shipping and port rules disappear.

For an attacked convoy, §32.64 selects the bomb-point column from its lane and
the westernmost qualifying Commonwealth division. Chart §32.66 defines a
qualifier as a complete **non-shell** Allied division east of, or in the same
east–west hexrow as, at least one **other** Allied non-shell division. Both
are required; a lone complete division does not qualify. The chart says `xx`
permits any north–south hex and its suffix names the east–west hexrow, then
instructs reading down the lane until the westernmost hexrow beyond which a
qualifying division is situated. Land §9.26 defines a division shell using its
attached brigade-equivalents; §9.27 excludes an attached shell from the
required lower-level count. Actual formation, representation and current
attachments must be resolved under the complete organization rules. Candidate
support can be in the same hexrow; mere proximity, a brigade, or an unverified
HQ label does not establish the pair. Among different rows, the westernmost
*qualifying* division need not be the westernmost division overall, because
the qualifier itself needs another non-shell division at or west of its row.
Ties must not generate multiple attacks or arbitrary divergent results.

The wording “beyond which” and exact boundary equality need a reviewed
coordinate/threshold interpretation; do not silently equate a printed threshold
with a source-proven inclusive comparison. Cross-sheet east–west ordering,
absence of a qualifying pair, unavailable position or unknown shell status must
be resolved explicitly; do not default a missing qualifying division to the
first row or to zero bomb points. The selected row names a **bomb-point
column**, not a random number in a range. Resolve each attacked convoy against
the separate §41.5 Axis Naval Convoys results table with sequential dice;
§32.65 then modifies loss handling. No additional RNG is justified merely by
the printed bomb-point range. The [loss-group review](2026-11-17-graziani-convoy-loss-groups-review.md)
records 5%, 10%, 20%+ and grouping uncertainties.

## Printed route columns

The following are the §32.66 printed cells for scenario §60.37's pre-start
September lanes 2, 3 and 6. They are **threshold labels**, not assertions that
the relevant pair actually occupies or crosses any row. A dash is printed in
that table cell; its operational meaning and the no-match case need an explicit
contract. Do not infer a route restriction for October solely from §60.37's
September planning sentence.

| Bomb points | Lane 2 | Lane 3 | Lane 6 |
| --- | --- | --- | --- |
| 21–40 | Dxx01 | Dxx01 | Dxx11 |
| 41–80 | Cxx16 | Cxx21 | Dxx01 |
| 81–120 | Cxx01 | Cxx11 | Cxx21 |
| 121–160 | Bxx16 | Cxx01 | Cxx11 |
| 161–200 | Bxx01 | Bxx26 | Cxx01 |
| 201–260 | Axx18 | Bxx20 | Bxx26 |
| 261–320 | — | Bxx10 | Bxx20 |
| 321–390 | — | Bxx01 | Bxx10 |
| 391–470 | — | Axx18 | Bxx01 |
| 471+ | — | — | Axx18 |

§60.37 says the Italian player plans convoy runs for the remainder of September
before starting and may use lanes 2, 3 and 6. Errata §56.25 says arriving
tonnage may be allocated to any OpStage in its turn unless otherwise
specified; its detailed-mode relationship to simplified planning and whether
the stage was fixed in a particular pregame plan remain separate questions.
The printed restriction does not establish that a shipment exists on every
turn or lane.

## Conditional six-turn matrix

Scenario §60.22 covers GT1 OpStage 1 through GT6 OpStage 3. The four-week
calendar yields GT1 September III, GT2 September IV, GT3–6 October I–IV.
The same source procedure runs in each stage, subject to accepted action
availability and plans; rows below are **not** computed bomb-point results.

| Turn | Calendar | Source-backed window and conditional lookup |
| --- | --- | --- |
| 1 | September III | Pregame plan's September lane constraint applies; for each actual arriving convoy, use its announced lane and current qualifying pair. Initial positions below are a starting snapshot only. |
| 2 | September IV | Same September planning scope; use GT2 stage-specific current pair, not GT1 positions or a presumed shipment. |
| 3 | October I | §60.37's rest-of-September restriction alone does not establish October lane availability. Use the actual lawful lane and current pair. |
| 4 | October II | Same conditional procedure; changes to unit composition or location can change the result. |
| 5 | October III | Same; scheduled reinforcement or movement does not automatically make a complete division or qualify a row. |
| 6 | October IV | Same through stage 3; do not use post-scenario state to choose the final-stage row. |

At setup §60.41 places the 7th Armored Division HQ at D3612 with listed units
detached, the 4th Indian Division HQ at D3615 with listed detachments, and
2nd New Zealand Division HQ at Alexandria E3613 and/or E3714 with listed
forces. These placements do **not** alone certify non-shell status or a
qualifying pair; the HQ's actual represented attached brigades, OA structure,
setup exceptions and §9.26 thresholds must be established. Nor does a current
HQ hex automatically prove where all separately represented division units
are. Even if a pair is eventually established, source coordinates and the
ordering rule must be certified before assigning its printed row. No GT1–6
specific bomb strength, convoy attack or pair is asserted here.

## Verification and unresolved questions

- Test two non-shell divisions in the same row; two in distinct rows; only one
  non-shell; shell plus non-shell; qualifying status changing after attachment,
  loss or movement; uncertain formation or map seam; and no pair. Verify the
  westernmost *qualifier*, not simply westernmost division, and specify exact
  row equality and out-of-range handling before expected numerical results.
- Test lanes independently, a declined attack, multiple cargo convoys, valid
  sequential dice and replay, along with rainstorm versus clear map weather
  under §32.63. Weather independence applies to this abstract attack; it does
  not waive other rules. Retain lane announcement, hidden cargo and side-safe
  projection; do not expose private plans through reason codes or event timing.
- Check source-specific planned supply/motorization/replacement availability,
  lane limits, destination cancellation/control, and any applicable port
  capacity separately. Use actual pregame commitments, not hypothetical cargo,
  as proof of an arrival. Verify the 5% and 25% result cells and loss rules
  independently of this bomb-point lookup.

No gameplay tests ran. Table transcription needs independent comparison;
movement, divisional eligibility, boundaries, planning and loss policy remain
admission gates.

An [independent source review](2026-11-17-graziani-axis-convoy-review.md)
confirms all 30 cells above and separately records lanes 1, 4 and 5. It also
identifies the §9.27 shell exclusion and the need to use the naval snapshot,
not a later movement-block phasing side. These source checks do not admit the
unresolved threshold or no-qualifier behavior.
