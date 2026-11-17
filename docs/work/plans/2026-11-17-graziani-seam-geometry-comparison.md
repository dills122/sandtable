# Graziani seam geometry: Living Rules and Sandtable

**Result:** the inspected Living Rules algorithm and Sandtable's existing
33-column candidate are geometrically compatible on the checked cases. Their
important difference is the **scope and evidence required for admission**, not
a demonstrated competing offset. Neither provides a source-certified complete
seam. No external map data or art was imported, and no local content, policy or
readiness flag changed.

## Sources and scope

- Original Land rules PDF and printed p. 7, §4.1, visually rechecked from
  `/private/tmp/sandtable-land-rules.pdf`, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  It places A–E west to east and says each following section's `01xx` hexrow
  overlays the preceding section's `39xx` hexrow.
- Original five-sheet map PDF SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
  physical A/B and B/C edge observations in the independent
  [A/B](2026-11-17-graziani-seam-ab-independent.md) and
  [B/C](2026-11-17-graziani-seam-bc-independent.md) transcriptions, and
  bounded C/D observations in the [identity pilot](2026-11-12-graziani-map-identity-pilot.md#independent-cd-sheet-edge-check).
  This comparison uses those observations; it is not another blind full-edge read.
- Air/Logistics §37.42 chart and note, PDF p. 25 of
  `/private/tmp/sandtable-air-rules.pdf`, SHA-256
  `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`,
  as transcribed in the [distance decision](2026-11-17-graziani-map-distance-decision.md).
- Living Rules repository `main` resolved on November 17 to commit
  [`c8769224721feb252cef530a2099cb44efbd6f28`](https://github.com/basmith7/cna/commit/c8769224721feb252cef530a2099cb44efbd6f28).
  Examined pinned [`tools/map_seams.py`](https://github.com/basmith7/cna/blob/c8769224721feb252cef530a2099cb44efbd6f28/tools/map_seams.py),
  [`tools/map_geom.py`](https://github.com/basmith7/cna/blob/c8769224721feb252cef530a2099cb44efbd6f28/tools/map_geom.py),
  and [`data/map/sheets.json`](https://github.com/basmith7/cna/blob/c8769224721feb252cef530a2099cb44efbd6f28/data/map/sheets.json).
  The [data conventions](https://basmith.net/cna/data/) explain its grid and
  provenance. The upstream work uses a VASSAL redraw checked against an SPI
  scan, so is a distinct analysis but not an independent original edition.

## Exact geometry and representation

Living Rules reads `RRCC` as row, column; rows rise north, columns east. Its
sheet bounds retain columns 01–33 on A–D and 01–34 on E; odd numbered rows
shift west relative to even rows. Its generator joins each western `RR33` to
next sheet's same-row `RR01`; for an **even** western row it additionally joins
`(RR−1)01` and `(RR+1)01` where those hexes exist. It filters to its existing
hex records. This is a 33-column section translation: next `RR01` is at the
geometric position western `RR34` would occupy, not the same position as
western `RR33`. For example, it predicts C3533–D3501 and C3633–D3601
same-row adjacency and C4033–D3901 diagonal adjacency. These match the three
narrowly admitted C/D pairs listed in the local pilot; this does not establish
agreement on every feature or pair.

The original physical A and B right margins contain alternating printed
`xx33`/`xx34` cells, while the following sheet begins with `xx01` cells.
Some edge cells are clipped and marks obscure labels. On the 33-column
hypothesis a printed western `RR34` and next `RR01` are overlapping copies of
one geometric position; `RR33` is an adjacent cell. Living Rules retains only
its 01–33 western canonical columns rather than explicitly representing those
printed `RR34` copies; its treatment is compatible with this hypothesis but
is not itself proof of an alias, nor does it preserve every original label.
The visual similarities and label limits in the independent strip notes remain
material. D/E's complete physical strip was not independently reviewed here;
upstream also describes Map E `00` as half-hex overlap with D `33`, an assertion
not validated in this comparison.

Both approaches conflict **at the wording level** with Land §4.1's `01xx` /
`39xx` instruction: the cited scan prints those numbers in that order, while
the physical edge observations and both computational conventions use the
last two digits as columns and involve 33/34 and 01. The source does not state
how to translate its wording into a corrected transform. A typo, axis reversal,
or alternate intended overlay cannot be selected solely from this comparison.
Air §37.42's statement that identical numbers on neighboring sections are 33
hexes apart is consistent with the 33-column candidate, but chart agreement
is not seam or movement certification.

As an independent arithmetic check of the **algorithm**, not a new source
transcription, applying Living Rules' doubled-coordinate formula to the 11
printed/candidate discrepancy pairs in the local pilot reproduces all 11 local
candidate numbers, including Barce B5504–Bir-el-Qatrani D2903 **78** against
printed **80**. It therefore does not resolve the inconsistent chart cells,
prove a preferred correction, or establish a legal land route.

## Policy difference and limits

Living Rules generates pair records across A/B, B/C, C/D and D/E and unions the
features recorded on the two opposing sides; its generator marks a feature
present on one side only and retains it as a likely sheet-cut artifact. Missing
edge records contribute an empty feature set. These are explicit upstream
inference and data-completion policies, not direct proofs of every crossing.
Sandtable's [provisional decision](2026-11-17-graziani-map-distance-decision.md)
keeps unverified identities and crossings unknown and unavailable, preserving
only the three specified C/D crossings in its existing contract. Its unfinished
railway crossing is not a usable rail route. A geometric match therefore does
not authorize upstream's generated pairs, feature union, aliases or terrain for
Sandtable gameplay.

Outstanding: independently inspect complete partial-cell geometry and feature
terminations on each applicable sheet edge, D/E included; reconcile §4.1 and
applicable corrections, chart inconsistencies, boundary and off-map treatment,
and per-edge feature conflicts. Establish reviewed terrain and feature
completeness, reciprocal edges, reachability and rule-specific traversal,
incoming ZOC and control before expanding admission. Existing C/D contract is
bounded project policy based on specific observations, not certification of an
entire seam. No tests were run for this documentation-only comparison.
