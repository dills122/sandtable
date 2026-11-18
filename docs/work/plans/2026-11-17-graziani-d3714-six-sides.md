# Matruh D3714: six-side visual source audit

**Finding:** Original Map D places Mersa Matruh's village dot in D3714. Its six geometric neighbors include two sea-side cells. Pinned side data contains only the southwest road/slope and east road/railroad records; source overprint and coastline limit negative readings. This is a visual source comparison, not certification of legal edges, movement, or network connectivity.

## Original map and pinned data

Inspected [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4 Map D and p. 1 terrain key. In the crop below, D3714's approximate vertices are `(355,770)`, `(515,680)`, `(670,770)`, `(670,950)`, `(515,1040)`, `(355,950)`. Directions describe the displayed crop. Labels `3813`, `3814`, `3715`, `3614` and `3613` are visible; `3713` is obscured by text/terrain, so that neighbor follows the printed grid sequence and geometry. Hexside entries are literal records from [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), sourced from `vassal:CNAv2.1.0`; no record means no entry in that file, not proof of a featureless or traversable side.

| Neighbor | Shared side | Source marks and pinned comparison |
| --- | --- | --- |
| `D3813` | upper-left diagonal | Grid line largely visible beneath the Matruh lettering; coastline runs close to its upper vertex. No distinct route or slope mark seen on the side. No pinned record. |
| `D3814` | upper-right diagonal | Blue coast/harbor artwork closely overlaps grid and obscures parts of the side; port anchor sits in the neighboring sea area. No pinned record. Underlying marks cannot be ruled out. |
| `D3715` | right vertical | Paired dark road lines and blue cross-tied railway cross near `(670,850)` and `(670,875)`; text and route marks overlap. Pinned `road`, `railroad`. |
| `D3614` | lower-right diagonal | Grid visible across much of side; rough brown artwork in D3614 and slope/road ink near the lower-left vertex crowd the junction. No distinct crossing identified on the exposed portion. No pinned record. |
| `D3613` | lower-left diagonal | Brown splash-edged slope follows side and paired road lines cross near `(465,1010)`. Splash extends into D3714; pinned `slope`, `road`, `up: D3613`. Land §8.35's down-side convention was checked in the earlier [Fourth Indian source review](2026-11-17-graziani-fourth-indian-edges-next.md); this is not an elevation measurement. |
| `D3713` | left vertical | Grid and brown terrain are crowded by Mersa Matruh text, especially toward the lower vertex; no distinct crossing identified in the exposed portion. No pinned record. |

The original key distinguishes road, railroad, slope, coast, Village/Bir and port. D3714 contains a blue dot matching Village/Bir; the anchor is a separate port mark. Pinned [hexes.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) describes D3714 as `clear`, `village`, `coastal: true`. Shoreline, name, port and road/rail art overlap grid or nearby marks; apparent absence beneath them is not established. This informed check uses the same scan and earlier local observations, not an independent edition.

## Reproduction and provenance

```sh
mkdir -p /private/tmp/d3714-six
pdftoppm -f 4 -l 4 -r 500 -x 3750 -y 6350 -W 1550 -H 1550 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/d3714-six/map
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/d3714-six/key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `51e978cf0d3627bfd7c413b76dc481303c40dbdc111433769ee28c2189471ae3`; key crop `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`; pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Crop bytes may vary with renderer. Crops remain outside Git. No runtime changes or gameplay tests.
