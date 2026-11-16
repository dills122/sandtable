# Graziani historical map transcription v1

`Cna1979HistoricalMap` embeds `hexes.json` and `hexsides.json` from
[CNA Living Rules](https://github.com/basmith7/cna), revision
`c8769224721feb252cef530a2099cb44efbd6f28`. Its
[data conventions](https://github.com/basmith7/cna/blob/c8769224721feb252cef530a2099cb44efbd6f28/data/README.md)
dedicate the data to CC0. The transcription derives from the VASSAL redraw
with corrections checked by its maintainers against the SPI scan; it is not
independent verification of every hex by Sandtable. Resource SHA-256 hashes
are pinned in the loader; changing data requires a new version and identity.

The original Map D scan independently confirms the road D3615–D3715–D3714.
Opening Fourth Indian movement checks both edges and the origin and destination
terrain against the pinned transcription in addition to its existing source
content identity. The movement and supply trace costs come from the existing
road movement table. They remain subject to all readiness and movement rules.
The Polish arrival path also checks its five source-defined Cairo entry
hexes against the transcription's major-city terrain. Absence from the feature list means only that the transcription records
no feature on a known same-sheet adjacent pair. Missing hexes, nonadjacent
pairs, and cross-sheet pairs return unknown; they do not become clear edges.
Source contact projection may also resolve previously unknown same-sheet
adjacency for known hexes using the printed row and column grid. Existing
contact proofs remain first in the decision path. This fallback is enabled
only for map-bound source turns; version-one replay retains its earlier
geometry. Cross-sheet ambiguity continues to fail closed. Contact geometry
does not grant movement or ZOC.
Hex settlement/coast fields and higher-side direction for slope or escarpment
are retained from the transcription for later adjudication; they are not by
themselves proof of enterability or cost. Two internal slope records,
B1705–B1706 and B1706–B1805, have no higher hex in this revision; preserve
that direction as unknown rather than assuming either traversal cost. The
transcription's [M-747](https://github.com/basmith7/cna/blob/c8769224721feb252cef530a2099cb44efbd6f28/data/map/corrections/M-747.json)
and [M-748](https://github.com/basmith7/cna/blob/c8769224721feb252cef530a2099cb44efbd6f28/data/map/corrections/M-748.json)
report that the scan resolution did not establish the higher side.
An independent color-map scan review on November 15 shows the band on the
B1705 side of B1705–B1706 and the B1805 side of B1706–B1805. If the
transcription's convention that the band marks the lower side is confirmed,
B1706 is the candidate higher hex for both. This observation is not yet an
admitted correction: the convention needs independent source confirmation and
any correction needs a new map identity.

The source's derived seams and corrections are not generally admitted here.
The provisional C/D alignment admits only C3533–D3501 and C4033–D3901 tracks
and C3633–D3601 unfinished railway. Their boundary records agree and source
overlaps were checked against the scans. The unfinished railway is not an
admitted rail route.
Source-turn contract 3 binds the seam policy identity; versions 1 and 2 retain
their earlier geometry. Other seams, aliases and the distance-chart discrepancies still require reconciliation,
and this map does not establish complete grouped ZOC, movement legality,
control, or the safety of GT1. Existing replay identities and blockers remain
unchanged. Terrain and feature facts outside the opening route and Cairo are
available for future source review, not certified movement authority.
New source turns use checkpoint and event version 3, binding the map format,
revision, map hashes and bounded C/D policy hash. Core history and Host journal
replay retain versions 1 and 2 for their respective start events. The embedded v1 map
bytes must remain available for version 2 replay. A complete real source turn
is still blocked, so this binding has not yet been exercised through an
end-to-end source-turn journal. Map-bound source-world creation and successor
construction reject a printed on-map battlefield coordinate absent from the pinned transcription;
off-map areas and unresolved locations retain their existing handling. A map
resource whose bytes do not match its hash fails before parsing.

The transcription's separate grid-distance calculation was compared against
all 190 values in the source-checked distance triangle. It matches 179 and
produces the same 11 disagreements as Sandtable's existing 33-column
candidate: for example, 62 for B5925–A1816 and 78 for D2903–B5504, against
the original chart's 61 and 80. Agreement between implementations does not
determine whether chart values, geometry, or the overlap rule are wrong;
general cross-sheet movement remains blocked.

The transcription additionally infers seven tracks and one road across
sheet joins where the route appears on only one side. These eight inferred
continuations are not authoritative routes in Sandtable; their source-map
evidence and rule treatment must be checked before seam movement is enabled.
