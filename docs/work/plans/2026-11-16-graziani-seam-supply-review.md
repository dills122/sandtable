# Graziani map seam and setup source recheck — November 16, 2026

**Status:** bounded, source-based recheck, not map certification or scenario
admission. Supply and truck values were entered from the original scenario
before comparison with the existing first entries. This reader had already
seen planning summaries and some values in the current conversation; the
reading is therefore **not blind**. Reader independence from the earlier
research stream is not established. Retain a genuinely independent review gate.

## Source identities and review method

| Source | Temporary copy | SHA-256 |
|---|---|---|
| Scenario | `/private/tmp/sandtable-scenarios.pdf` | `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66` |
| Five map sheets | `/private/tmp/sandtable-maps.pdf` | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` |
| Land rules | `/private/tmp/sandtable-land-rules.pdf` | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` |
| Air/Logistics rules and charts | `/private/tmp/sandtable-air-rules.pdf` | `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4` |
| September errata | `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |

The scan paths are temporary and no scans are included in Git. Hashes identify
bytes, not origin, rights or independent editions. Scenario PDF pp. 3–5,
Land printed p. 7, Air/Logistics PDF p. 25, and map sheets A–D were inspected;
the map's prior high-resolution seam crops were also reviewed. The [review
status guide](2026-11-13-graziani-review-status.md#reacquiring-source-files)
records reacquisition links.

**November 17 follow-up:** the five-sheet PDF was reacquired from the original
URL and its SHA-256 matched the value above. A same-stream visual recheck of
Map A's Summary of Important Locations reads Benghazi `A4827`. The city/port
symbol still obscures the underlying numbered hex in the inspected crop; this
recheck supports the existing [working coordinate ruling](2026-11-12-graziani-ruling-decisions.md#rul-gra-05-benghazi-setup-coordinate-working-interpretation)
but is not an independent reading, a new seam certification, or resolution of
the conflicting scenario rows. The downloaded copy remains temporary.

## Map seam: bounded observations

Land §4.1, printed p. 7, describes a section's `01xx` row over the preceding
section's `39xx` row. Air/Logistics §37.42, PDF p. 25, states that equal
numbers on adjacent sections are 33 hexes apart. The chart explicitly counts
shortest geometric distance, including hypothetical sea-grid crossings. Neither
statement alone proves a legal crossing, an alias, terrain or route.

Map A/B edge crops show the distinctive track junction near A3233 and B3201,
but the A edge cell is partial and the B label is partly obscured. B/C crops
show a track bend near B3134/C3101, also with partially obscured labels. These
are useful alignment leads, not verified aliases or authority for a whole seam.
Existing C/D edge evidence, including the three bounded admitted crossings, is
recorded in the [map identity pilot](2026-11-12-graziani-map-identity-pilot.md#independent-cd-sheet-edge-check).
This recheck does not extend them. Sheet-edge appearance, Land wording, grid
stagger and chart distance still need reconciliation for each admitted join.

**Additional B/C visual lead:** previously retained B/C edge crops show a
Gazala-area `4934` partial edge cell with a coastal road reaching B's edge;
the C `4901` edge area also shows a coastal road. The local orientation and feature
continuity make this pair useful for a future seam reading, but `C4901` is
partially cut by the image edge and similar coast/road patterns recur. This
observation does not prove an alias, a traversable edge, or that either label
is fully visible in both crops. The crops' derivation was not independently
verified this session. Check neighboring labels and geometry directly on the
hash-matched originals in a blind reading before accepting any relationship.

### Rechecked printed distance cells

After consulting the existing discrepancy list, this reader visually rechecked
all 11 printed cells in §37.42. Thus this is a targeted, non-blind verification
of the printed values, **not** a new independent transcription of all 190 cells
or an independent recalculation of the candidate column. Candidate distances
below are retained from the [existing comparison](2026-11-12-graziani-map-identity-pilot.md#full-distance-chart-comparison).

| Pair | Printed | Earlier 33-column candidate |
|---|---:|---:|
| B5925–A1816 | 61 | 62 |
| C2803–A1816 | 57 | 58 |
| C4807–A1816 | 71 | 72 |
| C4321–A2629 | 67 | 66 |
| C4321–A4130 | 59 | 58 |
| D2903–A4130 | 77 | 78 |
| D2903–B5504 | 80 | 78 |
| E3613–C2803 | 79 | 80 |
| E2418–A4130 | 130 | 129 |
| E2418–C1014 | 76 | 77 |
| E1829–C2803 | 98 | 97 |

In particular, the Barce B5504 / Bir-el-Qatrani D2903 cell prints 80, not the
candidate 78. This verifies the source reading, not which source or model is
wrong. Do not treat a 179/190 match, or the chart's geometric distances, as
proof of legal movement, map identity or resolution of the 11 conflicts.

**Next source work:** obtain a distinct reader's blind field-by-field seam
record from the verified scans: map pair, visible grid labels, cropped source
locator, adjacent hex geometry, terrain/feature continuity, ambiguous or
off-sheet marks, and corrections. Independently reproduce the full 190-cell
chart transcription and candidate calculation, including parity and endpoints.
Reconcile or explicitly block every mismatch and record the selected source
hierarchy before extending coverage. Validate reciprocal neighbors, physical
boundaries, feature continuity, ZOC, route legality and replay under a versioned
content identity. No new seam or readiness flag is authorized by this packet.

**November 17 calculation check:** using the existing 190-cell transcription,
recomputed distances with axial/cube distance `(abs(dq)+abs(dr)+abs(dq+dr))/2`,
testing uniform inter-sheet offsets 31–35 and both row-stagger conventions. The
33-column, `(row + (row & 1))/2` convention reproduced 179/190 matches and the
same 11 mismatches; every other tested combination matched at most 90/190.
This is a calculation cross-check against the same transcription, not a blind
source entry or proof that offsets outside this small family are impossible.
It does not select a seam or adjudicate any printed mismatch.

**Chart consistency check:** the original §37.42 chart (Air/Logistics
PDF p. 25) defines each entry as a shortest hex distance, including hypothetical
sea-grid crossings. It prints Barce `B5504`–Bir-el-Qatrani `D2903` as **80**,
Barce–Tobruk `C4807` as **40**, and Tobruk–Bir-el-Qatrani as **38**. A
single, consistent shortest-path metric must satisfy the triangle inequality,
but `80 > 40 + 38`. Thus these three printed cells cannot all be distances in
the same unweighted connected hex grid, regardless of the chosen section offset.
This check does not identify which cell is wrong, prove a particular seam, or
establish a legal movement route. A [fresh-context reader](2026-11-17-graziani-map-independent-review.md)
subsequently transcribed the same three cells and chart footnote independently;
errata review remains required. The reader also found B4827 inland without a
visible Benghazi city/port mark, while Map A and the distance chart identify
Benghazi as A4827. This supports review of the coordinate ruling but does not
establish a legal seam. The reader recorded map and Air hashes without baselines
in its supplied manifest; this session separately matched them to the hashes above.

For triage, a script checked the existing 20-place, 190-cell transcription
across all 1,140 unordered location triples and found 24 triangle-inequality
violations. A [fresh-context full-chart entry](2026-11-17-graziani-distance-transcription.md)
subsequently matched all 190 values and reproduced the 24 violations; the prior
data and new reading are retained separately. Same-edition agreement is not a
correction of any chart cell or proof of legal map geometry.

## Supply and truck row comparisons

| Packet | New source entry | Raw comparison | Result |
|---|---|---|---|
| Supply: 13 rows and two placement/dummy groups, 50 fields | [reading-b.json](graziani-supply-review/reading-b.json) | [comparison-b.json](graziani-supply-review/comparison-b.json) | 50/50 field values agree; 15 locator-wording differences |
| Trucks: 25 first-line rows and seven pools, 96 fields | [reading-b.json](graziani-truck-review/reading-b.json) | [comparison-b.json](graziani-truck-review/comparison-b.json) | 96/96 field values agree; 32 locator-wording differences |

Both entries were constructed from the scenario image and the blank template
before comparison with `reading-a.json`. The new locators specify the same
printed page, case and row with additional PDF page/case detail; grouped supply
locators name the relevant footnote and paragraph. The comparator treats the
different strings as issues. Retain all 47 raw locator differences for reviewer
disposition rather than silently changing either entry. The comparator's exit
status is 1, as expected for those raw differences; there are no field-value,
unknown, unverified-reader or source-hash issues in these two comparisons.

Source distinctions retained: Derna's ammunition is a printed dash, not a
numeric zero; truck types absent from a row are `not-listed`, not zero. Axis
and Commonwealth placement and dummy-group fields record the source text's
limits, not a ruling on map geometry, proximity inclusivity, allocation or
co-location. Scenario Benghazi `B4827` remains in tension with the map and
location-summary evidence for `A4827`; do not silently normalize the source.
Agreement does not establish legality, rights, complete scenario scope, or
independent review.

**Next source work:** a fresh reader without exposure to either entry or prior
summaries should transcribe the 15 and 32 rows from the hash-matched scan and
disposition locator wording independently. Separately review actual initial
motorization, extra loads, placement constraints, setup choices and source
errata. Keep missing and unsupported data distinct from legitimate zero. Bind
any accepted facts and rulings to a new content identity and exercise replay,
conservation and side privacy before admission.

## Reproduction

The two `comparison-b.json` files retain the unmodified comparator output. To
re-run either comparison, use the commands in the packet README, replacing the
temporary second-reading argument with that packet's `reading-b.json`. The
aggregate status tool continues to use blank templates unless passed explicit
`--reading supply=.../reading-b.json` and `--reading trucks=.../reading-b.json`
overrides. An attributed entry or zero value differences does not prove
independence or accept the source as an implementation contract.
