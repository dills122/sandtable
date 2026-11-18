# Map E eastern-edge rail: Bilbeis branch

**Date:** November 18, 2026. **Disposition:** Eight connected printed rail sides lead from `E2429` through Bilbeis toward the eastern sheet edge and branch northwest toward Zagazig. These are bounded source-side candidates, not a complete Suez or Ismailia rail survey. The sheet does not show a connecting rail from `E2134` to this branch or to an off-map facility.

## Source and pinned comparison

Inspected page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), using its page 1 key for the blue transverse-ticked rail symbol. Compared literal unordered pairs in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). Source crop coordinates below refer to the unrotated 300 dpi PNG, with origin at its upper left. Numbering follows printed labels and grid continuity; pinned data were available during reading, so this is not a blind or independent-edition check.

| Side | Printed source observation; approximate crop crossing | Pinned features |
| --- | --- | --- |
| `E2429–E2530` | Ticked line crosses diagonal east of river junction, near `(640, 1470)`; near a vertex. | `railroad` |
| `E2530–E2531` | Ticked line crosses vertical side near `(1040, 1310)`. | `railroad` |
| `E2531–E2631` | Ticked line crosses diagonal near `(1300, 1155)`; close to vertex. | `railroad` |
| `E2631–E2632` | Ticked line crosses vertical side near `(1600, 1000)`. | `railroad` |
| `E2632–E2633` | Ticked line crosses vertical side east of Bilbeis near `(1980, 930)`, with town lettering nearby. | `railroad` |
| `E2632–E2732` | Branch from Bilbeis crosses diagonal northwest near `(1750, 800)`. | `railroad` |
| `E2732–E2831` | Ticked branch crosses diagonal near `(1470, 510)`; attribution near vertex warrants review. | `railroad` |
| `E2830–E2831` | Ticked branch crosses vertical side near `(1230, 400)` toward Zagazig. | `railroad` |

All eight pinned records have `side: null` and `up: null`. Source ink and pin agree on these candidate pairs, subject to vertex and overprint qualifications; a nearby vertex does not certify either adjoining side. No other features or negative sides were systematically checked. The ticked line in `E2633` reaches the sheet edge; no adjacent off-map hex or onward connection follows from that mark. Separately, the Heliopolis line ends at the sheet edge in `E2134`. Scenario §60.5 calls Ismailia and other facilities off-map; coordinate-like references such as Abu Seier `E3433` and Port Said `E4033` do not place them in visible hexes or establish any rail connection. Sheet seams, off-map continuation, line construction or damage, service, control and actual occupancy remain unresolved. This is not proof of an occupied witness or of absence elsewhere.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 5200 -y 5600 -W 3000 -H 2600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/suez-survey
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/suez-survey.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `095d93258c06f05bcab7d2ad77451c81d02862c88d93db309ffde6608590229e` (renderer-dependent); pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crop remain outside Git. No runtime or map-data changes; no gameplay tests.
