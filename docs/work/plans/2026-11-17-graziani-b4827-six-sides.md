# Map B B4827: six-side source inspection

**Finding:** Map B's printed `4827` is a separate inland, rock/gravel-patterned hex, with no visible Benghazi name, major-city blocks or port symbol. Map A's Summary of Important Locations instead lists Benghazi `A4827`, marked major city, port and victory condition. Scenario §§60.31 and 60.92(B) print Benghazi `B4827` and `B 4827` respectively. Preserve that conflict; the visual evidence does not supply an official correction or a seam transform.

## Source and visual reading

Inspected [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 2 (Map B), and p. 1 (terrain key and, in a separate crop, location summary). Map B's `4827` is legible in the crop; its six vertices are approximately `(501,250)`, `(596,302)`, `(596,411)`, `(501,464)`, `(406,411)`, `(406,302)`. Directions below refer to the displayed crop, not compass bearings. Yellow rings and stippling across the hex resemble the key's **Rock/Gravel** symbol. The key depicts major city as blue blocks and port as a circled anchor; neither appears in this cell. The named Benghazi coastal city and port appear on Map A, while the summary identifies it as `A4827`. See [A4827 inspection](2026-11-17-graziani-a4827-six-sides.md) for its separate edges and summary detail.

| Crop side | Neighbor | Observation |
|---|---|---|
| Upper left | `B4927` | Thin grid visible; no distinct crossing feature seen. Neighbor appears clear. |
| Upper right | `B4928` | Thin grid visible; no distinct crossing feature seen. Gravel pattern continues in neighbor. |
| Right | `B4828` | Thin grid visible; no distinct crossing feature seen. Gravel pattern continues in neighbor. |
| Lower right | `B4728` | Thin grid visible; no distinct crossing feature seen. Gravel pattern continues in neighbor. |
| Lower left | `B4727` | Thin grid visible; no distinct crossing feature seen. Gravel pattern continues in neighbor. |
| Left | `B4826` | Thin grid visible; no distinct crossing feature seen. Gravel pattern continues in neighbor. |

Neighbors read from the printed numbers and geometry; no feature crossing is apparent on any of the six sides at this scale. Fine ink can obscure or resemble grid, so absence is bounded by scan quality and is not a certification of legal movement, complete terrain, or a supply route. Dashed track visible farther southwest does not reach these six sides in this crop.

The pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records `B4827` as `gravel`, `settlement: null`, `coastal: false`; the pinned [side data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) contains no record mentioning `B4827`. Missing side records are not proof that every edge is feature-free. These files cite Vassal as their source, and are a comparison, not independent confirmation of the printed scan.

## Reproduction and limits

```sh
pdftoppm -f 2 -l 2 -r 300 -x 4650 -y 2200 -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b4827-local
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 150 -W 3100 -H 3250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b4827-key-summary
pdftoppm -f 1 -l 1 -scale-to 7000 -x 850 -y 2500 -W 1500 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b4827-summary
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/b4827-local.png /private/tmp/b4827-key-summary.png /private/tmp/b4827-summary.png src/Cna.Core/Content/HistoricalMap/hexes.v1.json src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; B crop `00fbd7171e20f757a5430efadf9112eedb9c5dc4b6eb6339c18d9e786d528160`; key crop `60bb1273946b98fe770923349fe808ad6e167cd1f23b0feb90e632cdcfa10f3c`; summary crop `93a67ce6178021fdfec7090c9e4a57fa6a331824c08dd111eafc8bc1cb1a5f46`; hex data `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; side data `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Render hashes may vary with renderer. Crops remain outside Git; hashes do not establish redistribution rights. This informed reinspection uses the same scan as [prior review](2026-11-17-graziani-map-next-review.md), not a separate edition or blind transcription. Scenario and summary readings draw on the [source comparison](2026-11-17-graziani-map-followup.md); the reason for the repeated B prefix remains unresolved.

[Independent review](2026-11-17-graziani-b4827-six-sides-review.md) identified the original key crop as incomplete for the Benghazi summary row and corrected its hash; the separate summary crop above addresses that gap.
