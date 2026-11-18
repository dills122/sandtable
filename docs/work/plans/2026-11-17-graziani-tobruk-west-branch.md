# Tobruk west branch: source-side inspection

**Finding:** Map C shows a blue unfinished-railroad lead in C4707 toward Tobruk C4807, and dark road strokes converge in the same crowded junction. The pinned C4707–C4807 record calls the crossings `unfinished-railroad` and `unfinished-road`, with `slope`; the scan does not isolate every dark stroke well enough to independently certify that unfinished-road side assignment. No route, movement, or supply proof follows.

## Source reading

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), page 3 (Map C) and page 1 (terrain key). Directions below refer to the **unrotated crop**, not compass bearings. C4807 is Tobruk, identified by the surrounding numbered grid and the page 1 important-locations table; C4707 lies below-left, C4708 below-right, and C4806 to the left. See the [C4807](2026-11-17-graziani-c4807-six-sides.md) and [C4707](2026-11-17-graziani-c4707-six-sides.md) readings for grid anchors.

The key labels a single heavy broken black line **Track**, paired broken black lines **Unfinished Road**, and a broken blue line with cross ties **Unfinished Railroad**. Paired solid black lines denote road. These are separate printed symbols; a black grid line, blue water, gold slope band, lettering, and Axis/city artwork must not be mistaken for routes.

- The blue cross-tied lead runs upward through C4707 toward the C4807–C4707 diagonal, meeting it amid the junction and slope ink. Below, it crosses the C4707–C4607 slope and bends right through C4607 toward C4608. Its exposed form matches the key's unfinished railroad, not solid railroad.
- Paired solid road strokes approach Tobruk from C4806, then curve down-right toward C4708. Near the shared C4807/C4707/C4708 vertex, dark strokes also turn downward through the C4707 area; paired broken strokes are visible farther down toward C4607. The vertex, city blocks, Axis figures, slope and overlapping route strokes prevent a clean independent tracing of the unfinished-road crossing through C4807–C4707. The solid road's passage near the vertex also makes C4708 a competing side for the crowded marks; do not assign every dark stroke to one pair.
- Track is a distinct key label, but this crop does not isolate a track crossing on C4807–C4707. Do not relabel the paired broken unfinished-road marks as track or infer a track connection from this junction. El Adem is labeled farther below; Tobruk labels the city. Neither label by itself establishes a crossing.

## Pinned comparison

[Hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) declares `vassal:CNAv2.1.0`; all records below have `side: null`. `up` is pinned data, not a fresh direction determination here.

| Pair | Pinned features | `up` | Comparison |
| --- | --- | --- | --- |
| C4707–C4807 | slope, unfinished-railroad, unfinished-road | C4707 | Blue lead and slope are visible; dark branch crossing remains crowded. |
| C4708–C4807 | road, slope | C4708 | Solid road and slope converge near the shared vertex; exact separation from neighboring strokes is limited. |
| C4806–C4807 | road, slope | C4806 | Paired solid road approaches city from left. |
| C4607–C4707 | slope, unfinished-railroad, unfinished-road | C4607 | Blue lead and broken paired strokes continue downward through slope area. |
| C4607–C4608 | unfinished-railroad | null | Blue lead bends right across this side. |
| C4707–C4708 | no explicit record | — | Junction touches upper endpoint; absence is not proof of a feature-free side. |

The relevant [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) identifies C4807 as coastal major city and C4707 as clear. These pinned records and map derive from related source material, not independent edition verification. No change to pinned data is proposed from this crowded junction alone.

## Reproduction and hashes

```sh
pdftoppm -f 3 -l 3 -r 500 -x 1200 -y 3200 -W 2100 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4807-six
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 160 -W 1500 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4707/key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Map C crop `59e114a35f0219924f1e6f187c86bda0e4b77af81c5f770a9eaad5dec965bf9c`; key crop `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`; hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Renderer versions can change image bytes. Crops remain outside Git. No runtime edits or gameplay tests.
