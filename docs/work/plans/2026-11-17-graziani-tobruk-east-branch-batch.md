# Tobruk eastern and southern junctions: source-side batch

**Finding, 2026-11-17:** Map C shows paired solid road strokes through the C4708 area, blue unfinished-railroad marks south of C4707, and separate broken black route symbols farther south. The Tobruk junction is crowded: the scan supports local leads but does not independently certify every crossing at its shared vertex. No route, movement, or supply claim follows.

## Source and reading

Inspected page 3 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) and its page 1 terrain key. Directions refer to the unrotated crop. Grid and surrounding labels locate Tobruk C4807, C4707 below-left, C4708 below-right, and C4808 to the right. The [C4807 six-side reading](2026-11-17-graziani-c4807-six-sides.md) and [independent same-scan review](2026-11-17-graziani-c4807-six-sides-independent-review.md) establish the grid anchors; [C4707 reading](2026-11-17-graziani-c4707-six-sides.md), [review](2026-11-17-graziani-c4707-six-sides-review.md), and [west branch review](2026-11-17-graziani-tobruk-west-branch-review.md) qualify the crowded junction.

The key distinguishes paired solid black **road**, paired broken black **unfinished road**, single heavy broken black **track**, and blue cross-tied **unfinished railroad**. Grid, gold slope, coastline, lettering and Axis/city art are separate marks. The solid road bends from Tobruk toward C4708 and continues down-right across the slope toward C4608. The blue cross-tied line is visible in C4707 and bends through C4607 toward C4608; it is not the solid road. Paired broken dark marks approach from below C4707, while heavy broken track marks occur farther south near El Adem. Those southern symbols do not establish a track crossing at the Tobruk vertex.

## Pinned side comparison

[Hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) declares `vassal:CNAv2.1.0`. All explicit rows have `side: null`; `up` below is pinned, not a new direction finding. Map and pinned data are related evidence, not independent edition corroboration.

| Pair | Pinned features; `up` | Visible comparison and limit |
| --- | --- | --- |
| C4708–C4807 | road, slope; C4708 | Paired solid road and gold band meet the lower-right city side close to the shared C4707 vertex; nearby dark strokes and art limit exact separation. |
| C4608–C4708 | road, slope; C4608 | Paired solid road crosses the gold diagonal at the lower-right of C4708; nearby linework and slope obscure parts of the crossing. |
| C4607–C4708 | slope; C4607 | Gold band appears on the lower-left C4708 diagonal. Crowded endpoint and nearby broken strokes do not independently establish a road or track crossing here. |
| C4707–C4807 | slope, unfinished-railroad, unfinished-road; C4707 | Blue cross-tied lead approaches the diagonal; paired broken road strokes converge nearby. Vertex, solid road and overprint prevent clean independent assignment of the unfinished-road crossing. |
| C4607–C4707 | slope, unfinished-railroad, unfinished-road; C4607 | Blue and paired broken marks meet the slope below C4707; overlap limits exact separation at the junction. |
| C4707–C4708 | no explicit record | Exposed vertical grid line; both endpoints are crowded. Absence of a record does not prove a feature-free side. |
| C4807–C4808 | no explicit record | Mostly water on exposed vertical side; no distinct route crossing isolated. Absence does not prove a feature-free side. |
| C4708–C4808 | no explicit record | Coast and water near this diagonal; no distinct route crossing isolated. Absence does not prove a feature-free side. |

The separate [C4707 slope direction review](2026-11-17-graziani-c4707-slope-directions-review.md) supports the visible-part `up` readings on C4707–C4807 and C4607–C4707; it does not certify route crossings. No data correction is proposed from this scan alone. A clearer original or independent edition is needed for obscured crossing assignments.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 500 -x 1200 -y 3200 -W 2100 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4807-six
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 160 -W 1500 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4707/key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Map C crop `59e114a35f0219924f1e6f187c86bda0e4b77af81c5f770a9eaad5dec965bf9c`; key crop `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`; hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Renderer versions can change crop bytes. Crops remain outside Git. No runtime edits or gameplay tests.
