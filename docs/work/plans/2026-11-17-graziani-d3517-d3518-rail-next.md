# Map D D3517–D3518 railway crossing

**Finding:** Original Map D shows a blue cross-tied railway crossing the
D3517–D3518 shared side, consistent with the pinned `railroad` feature. The
side also shows the parallel-line road crossing, consistent with its pinned
`road` feature. This checks one side only, not a complete railway route or
service.

## Source and geometry

Original [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D PDF
p. 4 and terrain key p. 1, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
The key identifies railroad by a blue line with cross-ties and road by parallel
dark lines. In the north-up Poppler rendering, D3517 is the coastal cell left
of D3518; the wider crop in the prior
[D3516–D3517 check](2026-11-17-graziani-d3516-d3517-rail-next.md) locates
D3517's printed label. The crop below shows `3518` in the cell to the right
and `3519` farther right.

```sh
pdftoppm -f 4 -l 4 -r 800 -x 8200 -y 12000 -W 1100 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/d3517east800
```

The shared vertical grid side is around `x=380`, from the northern vertex near
`y=250` to the southern vertex near `y=540`, in these crop pixels. The dark
road's two lines cross it around `y=405–455`. The blue cross-tied centerline
crosses it around `y=510`, just above the southern vertex; despite nearby blue
tie and grid overprint, the line continues on both sides. These are approximate
locators, not surveyed geometry. The crop SHA-256 is
`a3ac860b6a24d16b754b0df7e029d860d694bbe45f387590cda368ad48309d7d`;
renderer differences may change image bytes. Scan crops stay outside Git
pending rights.

## Dataset comparison and boundaries

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`
contains `D3517|D3518` with `road` and `railroad`, consistent with these
visible crossings. The record was known before reading the scan, so this is an
informed source check, not a blind transcription. It does not certify other
features or prove the coastal side passable.

The adjacent road, shoreline and grid line overlap, and the railway crossing
lies close to the lower vertex. The railroad centerline nonetheless appears on
both sides of the vertical boundary above that vertex. An independent review
should confirm cell labels, side endpoints and crossing, particularly that the
blue ink is not assigned to either diagonal side below the vertex. Source
geometry alone does not establish actual construction status, ownership, rail
phase, occupancy, ZOC, legal movement, or connectivity beyond this side.

## Conditional checks

1. Independently trace the blue centerline across the shared vertical edge and
   compare the original terrain key. Confirm the edge's lower vertex before
   attributing the blue crossing; do not infer from a tie alone.
2. Compare the same side's visible dark road crossing with pinned `road`.
   Preserve any unverified terrain or additional side features as unknown.
3. Test a future side lookup with the known `railroad` feature while retaining
   separate construction, legal-route and visibility gates. Do not regard this
   positive local observation as approval of a full Matruh route.

No data/runtime edits, source admission, or gameplay tests.
