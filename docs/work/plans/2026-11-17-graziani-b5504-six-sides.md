# Barce B5504: six printed sides

**Finding:** Original Map B shows two escarpment sides and four sides crossed by routes at Barce. The pinned side file has entries for all six pairs, but its `unfinished-road` entry for B5404 needs review: the visible pair at that crossing appears continuous, like the key's road, with additional dark ink overlapping it. Track and railroad overprint each other toward B5403. This is a source comparison, not a legal movement or clear-side certificate.

## Six sides

Visually inspected [original five-sheet map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 2 Map B, and p. 1 terrain key. The blue Barce dot sits in the `5504` cell; label and route ink overlap the number and several grid lines. Neighbor labels and outlines identify the six cells below. Directions describe the displayed crop.

| Neighbor | Crop side | Original observation | Pinned pair record |
| --- | --- | --- | --- |
| `B5603` | upper left diagonal | Dark escarpment band follows the side; band and terrain obscure the grid. | `escarpment`, `up: B5504` |
| `B5604` | upper right diagonal | Dark escarpment band follows the side; band obscures the grid. | `escarpment`, `up: B5504` |
| `B5505` | right vertical | Paired continuous dark lines cross the side, consistent with the key's road; name and route ink overprint the crossing. | `road` |
| `B5404` | lower right diagonal | Paired dark lines appear continuous across the side, consistent with road; extra dark linework runs with them and the junction and grid overlap the crossing. The key's unfinished road is paired **dashes**, not continuous lines. The exact overprinted marks need independent review. | `unfinished-road` |
| `B5403` | lower left diagonal | Blue line with cross ties and adjacent dark dashed marks cross the side; local overprint complicates separation of rail, track and underlying grid. | `track` |
| `B5503` | left vertical | Paired continuous dark lines cross the side, consistent with road; printed `5504` and route ink cover portions. | `road` |

The p. 1 key distinguishes continuous paired **road**, paired dashed **unfinished road**, single heavy dashed **track**, blue cross-tied **railroad**, and dark **escarpment**. The black band on the two upper sides matches the escarpment sample, but this visual check does not independently determine which side is up. The pinned `up` values are reported as data, not verified direction. The blue village dot and brown rough terrain around Barce match the pinned B5504 hex record (`rough`, `village`, `coastal: false`); terrain printing and labels cover portions of the cell. The side file's B5403 `track` entry does not by itself record the visible railroad; it may be represented separately. No conclusion about legal entry, ZOC, direction, or complete route connectivity follows from these local observations.

## Reproduction and provenance

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Pinned records identify source `vassal:CNAv2.1.0`, distinct from original map scan. Barce crop SHA-256 `c87c0e290f04f0c9ec3c9ec777427eb7c163078530e4ff28f940694cb401edac`; key crop SHA-256 `58824b3034255e7a0dd24187e6419df4c792b4fc7a04e17e1700a81e69df33bb`. PNG bytes may vary with renderer.

```sh
pdftoppm -f 2 -l 2 -r 600 -x 900 -y 2200 -W 1700 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b5504-sides
pdftoppm -f 1 -l 1 -r 600 -x 4600 -y 600 -W 4200 -H 3100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a-key
```

Same scan and existing pinned transcription, not an independent physical edition or exhaustive errata check. No runtime data, behavior, tests or readiness flags changed.
