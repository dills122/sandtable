# Graziani Map E: Alexandria E3714, six-side source inspection

Source: *The Campaign for North Africa* map PDF, `/private/tmp/sandtable-maps.pdf`, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. PDF page 5 is Map E; page 1 is Map A with the original terrain key and location summary. Directions below refer to the displayed Map E, north up. Observations describe the printed image, not occupancy, movement permission, or rule effects.

## E3714 and its six sides

The printed `3714` lies in blue coastal water immediately northeast of the other Alexandria hex `3613`. Its outline has a north and south point and vertical east and west sides. Reading clockwise from the northwest edge:

| Side | Adjacent printed hex | Visible evidence at or near shared side |
| --- | --- | --- |
| NW | E3813 | Blue sea on both sides; no distinct route crossing visible. |
| NE | E3814 | Blue sea along most of edge; coastal/route overprint converges near eastern corner. No separate crossing identified with confidence. |
| E | E3715 | Dark paired road crosses near the northern half; blue marks are consistent with railroad artwork, but a continuous railroad crossing cannot be distinguished confidently amid the shoreline and symbols. |
| SE | E3614 | Coastal/city and transport-line junction overprint near edge; dark lines diverge southeast toward E3614. Individual crossings at the junction are difficult to separate. |
| SW | E3613 | Dark paired road crosses amid dense city blocks and coastal linework. Overprint prevents confident certification of a railroad crossing. |
| W | E3713 | Blue sea on both sides; no distinct route crossing visible. |

Within E3714, pale circular anchor, black three-figure training-area symbol, city-block pattern along the coastal strip, blue sea, and a narrow light-colored shore are visible. The printed number and linework are partly covered by symbols. The pale anchor is much less contrasted than the anchor in E3613; it is nevertheless discernible in the detail crop. The training figures overlap the route and city marks. The adjoining hex E3815 contains Aboukir, a separate visible anchor and training symbol; avoid attributing those to E3714.

The Alexandria holding box on Map E explicitly has separate columns `Hex 3613` and `Hex 3714`. Map A's **Summary of Important Locations** lists Alexandria as `E3613/3714`, with X under **Major City**, **Port**, and **Victory Condition**, and a dash under **Assigned Garrison**. These are location-level summary entries; the row does not independently assign each property to each hex.

Map A's **Terrain Key** depicts blue as **Sea**, blue/light division as **Coast**, circular anchor as **Port**, clusters of blue blocks as **Major City**, black parallel lines as **Road**, blue cross-ticked line as **Railroad**, and three figures as **Training Area**. Color and dense overlapping print make tracing individual segments near the junction uncertain; this inspection does not decide route connectivity or legal passage.

## Pinned data and independent review

Pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records E3714 as `terrain: major-city`, `settlement: major-city`, `coastal: true`. Pinned [side data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) records E3613–E3714 and E3714–E3715 as `railroad` and `road`, and E3614–E3714 as `coast` and `road`; no literal E3714 entry appears for E3813, E3814 or E3713. Visible road crossings support the first two road entries; the railroad crossings and individual SE features remain unresolved in this scan. Missing entries do not prove missing printed features.

[Independent review](2026-11-17-graziani-e3714-six-sides-review.md) checked the same scan and crop hash; it is not a separate edition.

## Reproduction

Poppler's page numbering is one-based. This page-5 crop preserves the original page orientation and shows the target, all six neighbors, and adjoining Alexandria/Aboukir print:

```sh
pdftoppm -f 5 -l 5 -scale-to 8800 -x 1800 -y 3300 -W 850 -H 850 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e3714
shasum -a 256 /private/tmp/e3714.png
```

Result on this installation: `88c5bca1d3c1415e4aa26a202fd8273222d7f0da865360068a24e9d76e751fd0`. PNG encoding can differ between Poppler versions; the source PDF hash is the source identity.

Reproduce the page-1 location summary, rotated for reading:

```sh
pdftoppm -f 1 -l 1 -scale-to 7000 -x 900 -y 2520 -W 1420 -H 950 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/summary-raw
sips -r 90 /private/tmp/summary-raw.png --out /private/tmp/summary.png
```

For the original terrain key, render page 1 at `-scale-to 7000`, crop `-x 1540 -y 200 -W 1330 -H 1050`, and rotate 90 degrees clockwise. Coordinates refer to the rendered, automatically page-rotated image, not PDF points.
