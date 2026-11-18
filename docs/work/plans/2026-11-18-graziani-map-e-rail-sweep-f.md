# Map E pinned rail sweep F: northern delta and Alexandria

**Date:** November 18, 2026. **Disposition:** Sixteen previously unreviewed pinned pairs inspected; all show cross-tied rail crossing their shared side, with river, road, junction and near-vertex qualifications below. These are provisional local scan readings, not a route or game-state certification.

## Source comparison

Inspected [original scan](https://spigames.net/PDFv10/CNA_Maps.pdf), Map E on PDF p. 5, and terrain key on p. 1. Key distinguishes continuous blue cross-tied railroad, broken unfinished railroad and paired dark road. Printed grid numbers locate cells. Pin and prior notes were visible; comparison was not blind or independent-edition verification. Each exact pair below appears in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), with `side` and `up` null. Only rail was assessed; other pinned features are reported, not independently adjudicated. Crop coordinates are approximate pixels from the upper left of the named unrotated image.

| Pinned pair | Source observation | Pinned features |
| --- | --- | --- |
| `E3328–E3427` | Cross-tied blue line crosses diagonal near `(170, 350)` in east crop, with road and river artwork. | `minor-river`, `road`, `railroad` |
| `E3329–E3330` | Cross-tied blue line crosses vertical near `(385, 415)` in east crop, over river band beside road junction. | `minor-river`, `railroad` |
| `E3330–E3331` | Cross-tied blue line crosses vertical near `(510, 400)` in east crop, over river artwork. | `minor-river`, `railroad` |
| `E3331–E3431` | Cross-tied blue line crosses diagonal near `(620, 365)` in east crop. | `railroad` |
| `E3417–E3418` | Cross-tied line crosses vertical near `(838, 985)` in west crop, close to upper vertex beside road and branching ink. | `railroad`, `road` |
| `E3417–E3517` | Cross-tied line crosses diagonal near `(760, 945)` in west crop beside road. | `railroad`, `road` |
| `E3418–E3419` | Cross-tied line crosses vertical near `(963, 1000)` in west crop beside road and printed number. | `railroad`, `road` |
| `E3420–E3421` | Cross-tied line crosses vertical near `(1213, 985)` in west crop beside road. | `railroad`, `road` |
| `E3421–E3522` | Cross-tied line crosses diagonal near `(1295, 940)` in west crop beside road. | `railroad`, `road` |
| `E3426–E3427` | Cross-tied line crosses vertical near `(720, 325)` in mid crop, over river and road artwork. | `railroad`, `road` |
| `E3426–E3526` | Cross-tied line crosses diagonal near `(640, 290)` in mid crop, close to junction and over road. | `railroad`, `road` |
| `E3431–E3432` | Cross-tied line crosses vertical near `(700, 300)` in east crop. | `railroad` |
| `E3432–E3433` | Cross-tied line crosses vertical near `(826, 280)` in east crop. | `railroad` |
| `E3522–E3523` | Cross-tied line crosses vertical near `(1400, 880)` in west crop over river artwork, beside road and junction ink. | `minor-river`, `railroad`, `road` |
| `E3615–E3715` | Cross-tied line crosses diagonal near `(503, 725)` in west crop; nearby city lettering and artwork qualify reading. | `railroad` |
| `E3623–E3624` | Cross-tied line crosses vertical near `(1590, 775)` in west crop, beside road. | `railroad`, `road` |

Readings agree with pinned `railroad`, subject to scan and overprint limits. No unpinned side was surveyed for omission; missing pins do not establish absence of features. These crossings do not prove junction connectivity, complete network, construction or damage status, service, occupancy, or an unblocked legal movement result.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 2400 -y 1600 -W 2400 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-f-map
pdftoppm -f 5 -l 5 -r 200 -x 1400 -y 2200 -W 1800 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-f-west
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/rail-f-map.png"); im.crop((250,1250,1100,1900)).save("/private/tmp/rail-f-mid.png"); im.crop((900,1300,2200,1850)).save("/private/tmp/rail-f-east.png")'
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/rail-f-map.png /private/tmp/rail-f-west.png /private/tmp/rail-f-mid.png /private/tmp/rail-f-east.png /private/tmp/kafr-review-key.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `dbcca5b02f74a7b4e3dc22a90f234ae32fac2f4328b842d0a27d9be46cf34cff`; west `40e8de792ef197b0a29af1b7c0adc32a7f5905ab09c5802a5600b7829d9231fb`; mid `ad412390228a676604148a94aee3c7f9df49b74c17f7a0bba6c4bcd9769b0cdb`; east `470e45fb05ec46ea0a7a78c4f9c9599574d8bde4691163b3a28fcb69d8abe406`; key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`; pin `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer and image library; crops remain outside Git. No runtime or map-data changes; no gameplay tests.
