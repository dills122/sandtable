# Tobruk C4807: six-side source comparison

**Finding:** Original Map C depicts Tobruk at coastal C4807, with port and major-city marks, gold slope ink and converging route marks. Three pinned side records overlap visible slope or route leads; the city, Axis overprint, coastline and junction obscure exact intersections. This is a local visual comparison, not a legal movement conclusion.

## Identity and source

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1. Scenario fixed Axis placement at C4807 is listed in the [fixed-coordinate inventory](2026-11-17-graziani-fixed-contact-inventory.md). In the crop below, C4807 vertices clockwise from top are approximately `(1030,850)`, `(1185,940)`, `(1185,1120)`, `(1030,1210)`, `(875,1120)`, `(875,940)`. Its number is covered by artwork; surrounding grid and labels `4806`, `4808`, `4907`, `4908`, `4707`, and `4708` locate it. Directions refer to the displayed crop.

The key depicts major city as blue blocks, port as a circled anchor, coast as blue and pale land, slope as a gold band, road as paired solid lines, unfinished road as paired dashed lines, and unfinished railroad as blue line with separated cross ties. Blue blocks are visible beneath the Tobruk Axis figures and lettering, consistent with major city; they are distinct from the circled anchor at the upper vertex, which marks a port. The anchor straddles grid lines, so its position alone does not unambiguously assign it to a hex. The Map A “Summary of Important Locations” on PDF page 1 independently lists Tobruk at `C4807`, with an `X` under both `Major City` and `Port`; that table supplies the port assignment. The pinned [`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) says `terrain: major-city`, `settlement: major-city`, `coastal: true`; it has no port field in this record. Do not infer port status from `settlement` alone. See the [independent same-scan review](2026-11-17-graziani-c4807-six-sides-independent-review.md).

| Neighbor | Shared side and source marks | Pinned pair |
| --- | --- | --- |
| `C4907` | Upper-left diagonal runs through blue water, approaching coast at its left endpoint. Anchor overlaps the opposite vertex; no distinct road or slope crossing is isolated on exposed line. | No explicit pair. |
| `C4908` | Upper-right diagonal runs through blue water; circled anchor covers its upper endpoint. No distinct route or slope crossing isolated on exposed line. | No explicit pair. |
| `C4808` | Right vertical side is mostly blue, with Axis figure and Tobruk name nearby; coastline approaches lower endpoint. No distinct route crossing isolated. | No explicit pair. |
| `C4708` | Lower-right diagonal carries gold slope-like band; paired solid road leaves the city and crosses near its lower-left endpoint, among gold and other ink. Coast reaches the side farther right. | `road`, `slope`; `up: C4708`. |
| `C4707` | Lower-left diagonal carries gold slope-like ink. Blue cross-tied unfinished railroad approaches and meets this side near its left endpoint; dark route marks converge nearby. Overprint and vertex proximity prevent clean separation of all strokes or an independent precise placement of the unfinished-road crossing. | `slope`, `unfinished-railroad`, `unfinished-road`; `up: C4707`. |
| `C4806` | Left vertical side carries gold ink and paired solid road crossing near its middle. City artwork and route junction obscure portions. | `road`, `slope`; `up: C4806`. |

All three explicit records have `side: null`. No explicit pair does not certify a feature-free side. Gold band, grid, shoreline, anchor, lettering and figures overlap; assigning a feature at a vertex to one particular side or independently confirming the pinned `up` direction requires further evidence. No claim about route continuity, legal movement, ZOC or admission follows.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 500 -x 1200 -y 3200 -W 2100 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4807-six
pdftoppm -f 1 -l 1 -r 70 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapkey70
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `59e114a35f0219924f1e6f187c86bda0e4b77af81c5f770a9eaad5dec965bf9c`; key render SHA-256 `337402ef1f89db6a76a5ffdd79f04240c560e4f2c5e2cf2e81727f948b57d508`. Renderer versions can change PNG bytes. Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crops stay outside Git. This informed same-scan inspection is not independent edition verification. No runtime edits or gameplay tests.
