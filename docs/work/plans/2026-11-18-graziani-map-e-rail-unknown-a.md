# Map E rail unknowns: lower Nile and Cairo recheck

**Date:** November 18, 2026. **Disposition:** Five crossings supported with the local qualifications below; one remains unknown. This is an informed recheck of the same scan, with candidate pairs and prior readings visible, not blind or independent-edition corroboration. No map-data correction follows automatically.

Inspected [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), page 5, and page 1 terrain key. The key distinguishes blue continuous cross-tied railroad, broken cross-tied unfinished railroad, and paired dark road. Printed hex labels and grid establish the shared sides. Coordinates below are approximate pixels from the upper left of the unrotated 300-dpi crops in the commands, not geographic coordinates. Enlargements clarify the existing pixels, not independent source detail. Centerline continuity is locally visible but overprinting limits finer symbol distinctions.

| Pair | Recheck | Evidence and limit |
| --- | --- | --- |
| `E0929–E1029` | Positive, qualified | Blue cross-tied centerline crosses the shared diagonal near L (750, 950), separated from its nearest vertex; adjacent dark road ink crowds the crossing. |
| `E1130–E1229` | Positive, qualified | Blue centerline crosses the shared diagonal near L (865, 640), between its vertices, alongside dark road strokes; color and ties establish the local rail trace despite overlap. |
| `E1927–E2026` | Positive, qualified | Cross-tied blue line crosses the shared diagonal near M (1230, 1435). Crossing is close to, but visibly displaced from, the vertex at roughly M (1255, 1420); scan width limits precision. |
| `E1929–E2029` | Positive, qualified | Blue ticked northbound line crosses their diagonal near C (1590, 1070), left of the Cairo city and training symbols. Dark paired road runs alongside it; route through later city artwork is not inferred. |
| `E1930–E2029` | Remains unknown | Their diagonal extends from the city-side vertex toward the river. Northbound rail and road turn through the neighboring side below that vertex; city, road and river artwork complicate a complete assessment of the named side. This does not establish a crossing or its absence. |
| `E2124–E2223` | Positive, qualified | Cross-tied blue line crosses their diagonal near M (660, 1110), visibly short of the river-side vertex around M (685, 1095); proximity and ink width qualify exact placement. |

These readings differ from [sweep A](2026-11-18-graziani-map-e-rail-sweep-a.md), [sweep B](2026-11-18-graziani-map-e-rail-sweep-b.md), and [Cairo extension](2026-11-17-graziani-cairo-rail-extension.md), which retained these sides as uncertain. The near-road or near-vertex positive readings merit an independent visual challenge, particularly where grid and colored ink overlap. A crossing is attributed to the named side only; it does not establish crossings of adjoining sides, junction connectivity, service, control, occupancy, or movement legality.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 4700 -y 7800 -W 1600 -H 2600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e-lower-rail
pdftoppm -f 5 -l 5 -r 300 -x 3800 -y 5700 -W 2400 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-middle
pdftoppm -f 5 -l 5 -r 300 -x 3900 -y 6075 -W 2000 -H 2100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-unknown-a-cairo
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/e-lower-rail.png /private/tmp/rail-e-middle.png /private/tmp/kafr-review-key.png
```

L = `e-lower-rail`, M = `rail-e-middle`, C = `rail-unknown-a-cairo`. Verified PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; L `8eb60a0f4581a81263324910ce4a57f99d529e80274e35d810d26e2c9ee52816`; M `ee0df7def4b2ffc194b9c55b822f3e0291e9b1770305cbf70644ef36390bd0b1`; key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`. Crop bytes are renderer-dependent. No runtime or map-data edits; no gameplay tests.
