# Map E pinned rail sweep C: western Tanta approaches

**Date:** November 18, 2026. **Disposition:** Nine previously unreviewed pinned pairs checked locally. Eight show ticked rail crossing their shared side; the crowded `E3023–E3024` vertex remains unknown. These observations are provisional source-side readings, not certification of a route or game state.

## Source comparison

Inspected [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 5, and terrain key on p. 1. Key distinguishes continuous blue cross-tied railroad from broken unfinished railroad and paired dark road. Printed numbers and grid identify cells; coordinates below are approximate pixels in the unrotated detail crop, with origin at upper left. Pin and prior notes were visible during inspection; this is neither blind nor independent-edition evidence. Each pair below is present in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), with `side` and `up` null. Only rail is assessed; additional pinned road and river features are reported, not independently adjudicated.

| Pinned pair | Source observation | Pinned features |
| --- | --- | --- |
| `E2823–E2923` | Ticked blue line crosses diagonal near `(400, 830)`. | `railroad` |
| `E2923–E3023` | Ticked line crosses diagonal near `(400, 730)`; blue river artwork underlies it. | `railroad` |
| `E2824–E2924` | Ticked line crosses diagonal near `(540, 840)`; river artwork and bend qualify reading. | `railroad` |
| `E2924–E3024` | Ticked line crosses diagonal near `(530, 730)`. | `railroad` |
| `E3023–E3024` | Ticked ink approaches crosses toward shared vertical near `(494, 650)` close to its upper vertex, under road and river; exact side attribution **unknown**. | `minor-river`, `railroad`, `road` |
| `E3023–E3123` | Ticked diagonal line crosses shared side near `(405, 620)` beside road; junction and river overprint qualify reading. | `railroad`, `road` |
| `E3024–E3025` | Ticked line crosses vertical grid near `(620, 680)` beside road. | `railroad`, `road` |
| `E3025–E3026` | Ticked line crosses vertical grid near `(745, 680)` beside road. | `railroad`, `road` |
| `E3026–E3027` | Ticked line crosses vertical grid near `(870, 680)`, amid road and blue river artwork near Tanta. | `minor-river`, `railroad`, `road` |

The eight source-positive readings agree with pinned `railroad`, subject to overprint and scan limits; the unknown row is not a contradiction. No unpinned side was surveyed for omission. These local crossings do not prove complete network, connectivity through junctions, construction or damage status, service, occupancy, or an unblocked §8.78 result. Other sheets and candidates remain outside scope.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 2500 -y 2400 -W 2200 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/cairo-nile-north.png"); im.crop((0,560,950,1500)).save("/private/tmp/rail-c-detail.png")'
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north.png /private/tmp/rail-c-detail.png /private/tmp/kafr-review-key.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; full crop `ef345c8066e55295c8146a6539b4b39a88d4f62d3f4670993ae43fa79f117cdc`; detail `ef0ec377e5653855e225d59889f6438a31856691d28efed5e4eaf1c90bfe1ae5`; key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`; pin `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer and image library; crops remain outside Git. No runtime or map-data changes; no gameplay tests.
