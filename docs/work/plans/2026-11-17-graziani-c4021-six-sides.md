# Sollum C4021: six-side source comparison

**Finding:** In the local grid, the Sollum dot falls in candidate `C4021`. Its side with `C3921` carries the broad dark escarpment; its side with `C4020` carries a gold slope, blue checks and paired solid road lines. On the disputed `C3922` diagonal, exposed grid appears thin and paired road dashes cross near shore, while the broad escarpment clearly follows the adjoining C3921 side. Crowded junction, splash, road and coastline prevent a conclusive negative reading of the diagonal. Pinned data records an escarpment there: a candidate discrepancy, not a source correction or proof of a clear side.

## Identity and inspection

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. In the crop below, candidate C4021 vertices are approximately `(763,790)`, `(920,710)`, `(1080,790)`, `(1080,980)`, `(920,1060)`, `(763,980)`; directions describe the image. Number `4021` is partly obscured by the blue checks, slope and road; the surrounding numbered grid and Sollum dot support identification. `C3922` is inferred between legible `3921` and `3923`; its own number is hidden under Halfaya artwork. Some neighboring labels also overlap terrain. Grid geometry here is a visual reading, not independently registered source geometry.

Key shows broad stippled dark escarpment, gold slope, paired solid road lines, paired dashed unfinished road, and blue sea. Blue checkered line is visible; its interpretation and rule effect remain separate from this side inventory.

| Neighbor | Visible source, viewed from C4021 | Literal pinned pair comparison |
| --- | --- | --- |
| C4121 | Upper-left diagonal follows blue checks. Their junction and nearby terrain obscure portions; no distinct road crossing is isolated. | No pair record. Blue checks remain source evidence despite no record. |
| C4122 | Upper-right diagonal runs toward coast; thin grid exposed, with checks at western endpoint and shore/water near eastern endpoint. | No pair record. Endpoint and coast prevent a feature-free certification. |
| C4022 | Right vertical side largely crosses blue water and shoreline; grid visible. Sollum name overlaps lower area. | No pair record. Partial observation does not prove all-sea or clear classification. |
| C3922 | Lower-right diagonal runs from escarpment junction toward coastline. Exposed portion appears thin; paired dashes cross near the shore. Heavy band follows adjoining C3921 side; splash and crossing obscure junction ownership. | `escarpment`, `unfinished-road`, up `C4021`. Road lead visible; escarpment remains disputed, not disproved. |
| C3921 | Lower-left diagonal carries broad dark escarpment with splash toward C4021. Blue checks meet western vertex. | `escarpment`, up `C3921`; feature agrees with image, subject to identity and direction admission. |
| C4020 | Left vertical side carries gold slope and blue checks; paired solid road lines cross through them. | `road`, `slope`, up `C4020`; visual marks agree, with blue checks additional source information. |

Pinned file is [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. `up` values above are data, not independently adjudicated directional permissions. Missing pair records are file facts, not proof no source mark exists. This image comparison establishes no legal movement, ZOC, route continuity, border effect or edge admission. See [C3922 review](2026-11-17-graziani-c3922-six-sides-review.md) for the same disputed diagonal from its other side.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 500 -x 5700 -y 5500 -W 2200 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4021
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rendered crop SHA-256 `cf2999c3191acdb1e93482f1064330b2f4baff2a711ddd12d123fa65078a005e` (renderer-dependent). This is informed same-scan reinspection, not independent edition or rights verification. No runtime changes or gameplay tests.
