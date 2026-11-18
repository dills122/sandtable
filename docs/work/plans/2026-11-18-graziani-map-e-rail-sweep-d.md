# Map E pinned rail sweep D

**Date:** November 18, 2026. **Disposition:** Of 15 previously unreviewed pinned pairs, 13 show local rail crossings, one is uncertain at a river vertex, and one shows no crossing on its named side. The latter is a source/pin discrepancy requiring review, not a map-data correction.

Inspected [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), page 5, and page 1 terrain key. Key distinguishes continuous blue cross-tied railroad, broken cross-tied unfinished railroad, and paired dark road. Printed labels and shared grid sides locate cells. Pin and candidate list were visible: this is an informed reading of the same scan, not blind or independent-edition corroboration. Positive means visible ticked rail crosses the exact side; unknown means exact attribution is insecure. Coordinates are approximate pixels in unrotated crops, origin upper left: M = 2400 × 1900 middle; W = 2000 × 1900 west; N = 1500 × 900 north.

| Exact pair | Scan observation | Pin features |
| --- | --- | --- |
| `E1827–E1828` | Positive: ticked line crosses vertical near M (1445, 1665). | `railroad` |
| `E1827–E1927` | Positive: line crosses diagonal near M (1320, 1595). | `railroad` |
| `E1927–E1928` | Negative: shared vertical near M (1350, 1470–1580) has no visible rail crossing; line instead crosses the lower diagonal into `E1827`. Cairo detail makes the separation clear. | `railroad` |
| `E2127–E2226` | Positive: line crosses diagonal near M (1220, 1140), close to river ink. | `railroad` |
| `E2418–E2419` | Positive: line crosses vertical near W (545, 690); road artwork nearby. | `railroad` |
| `E2419–E2520` | Positive: line crosses diagonal near W (665, 625). | `railroad` |
| `E2421–E2521` | Positive: line crosses diagonal near W (970, 640). | `railroad` |
| `E2520–E2521` | Positive: line crosses vertical near W (827, 580). | `railroad` |
| `E2526–E2625` | Positive: line crosses diagonal near M (1005, 480). | `railroad` |
| `E2624–E2625` | Positive: line crosses vertical near M (880, 390). | `railroad` |
| `E2624–E2725` | Positive: line crosses diagonal near M (830, 310), beside river. | `railroad` |
| `E2725–E2824` | Positive: line crosses diagonal near M (840, 150), beside river. | `railroad` |
| `E2728–E2828` | Positive: line crosses diagonal near N (500, 560), near branch. | `railroad` |
| `E2828–E2829` | Positive: line crosses vertical near N (635, 485). | `railroad` |
| `E2829–E2830` | Unknown: ticked ink reaches river-covered grid at upper vertex near N (825, 415); junction, river and vertex prevent secure side attribution. | `major-river`, `railroad` |

Each exact unordered pair occurs once in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), with `side: null` and `up: null`. Only railroad is assessed; the additional river pin is not independently adjudicated. The negative reading is limited to this scan and selected side; unknown does not mean absence. No unpinned side was surveyed. These observations establish no complete network, legal route, construction or damage condition, service, control, occupancy, or unblocked result.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 3800 -y 5700 -W 2400 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-middle
pdftoppm -f 5 -l 5 -r 300 -x 3000 -y 5700 -W 2000 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-d-west
pdftoppm -f 5 -l 5 -r 300 -x 4800 -y 5300 -W 1500 -H 900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-d-north
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/rail-e-middle.png"); im.crop((1100,1380,1550,1770)).resize((1350,1170)).save("/private/tmp/rail-d-cairo.png")'
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-middle.png /private/tmp/rail-d-west.png /private/tmp/rail-d-north.png /private/tmp/rail-d-cairo.png /private/tmp/kafr-review-key.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256 in command order: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, `ee0df7def4b2ffc194b9c55b822f3e0291e9b1770305cbf70644ef36390bd0b1`, `98645812cf9516c5d20b0fe06f1d41231255f1d102c1b1d0b3c4ce485d94a209`, `b1c7ee4e0058394b16413b3243552077d15dd269278267354c3b5df886f4058c`, `0b5938daea61ad345b122fc19d3b9d1e89d832f1df3cb5179be73a406e1c2c1c`, `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`, `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer and image library; crops remain outside Git. Documentation only; no runtime or map-data changes, no gameplay tests.
