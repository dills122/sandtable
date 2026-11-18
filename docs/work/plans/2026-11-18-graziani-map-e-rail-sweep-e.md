# Map E pinned rail sweep E: delta branches

**Date:** November 18, 2026. **Disposition:** Fifteen previously unreviewed pinned pairs inspected. Twelve show rail across the shared side; three remain unknown at city or crowded vertex overprint. These are local scan readings, not certification of a route or game state.

## Source comparison

Inspected [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 5, and terrain key on page 1. The key distinguishes blue cross-tied railroad from broken unfinished railroad and paired dark road. Printed grid labels identify cells. Coordinates are approximate pixels in the indicated unrotated detail crop, origin upper left. Pin and previous notes were visible; this is neither blind nor independent-edition evidence. All listed exact pairs occur in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), with `side` and `up` null. Only rail is assessed; other pinned features are reported without adjudication.

| Pinned pair | Scan observation | Pinned features |
| --- | --- | --- |
| `E2829–E2930` | **Unknown:** ticked ink meets shared diagonal near Zagazig city and river overprint, roughly delta `(475, 650)`; attribution close to vertex is unclear. | `railroad` |
| `E2930–E3030` | Ticked blue line crosses diagonal near delta `(515, 530)` over river artwork. | `railroad` |
| `E3030–E3130` | Ticked blue line crosses diagonal near delta `(522, 428)` beside river artwork. | `railroad` |
| `E3118–E3217` | Ticked line crosses diagonal near west `(130, 370)`. | `railroad` |
| `E3123–E3222` | Ticked line crosses diagonal near west `(770, 360)` beside road and river artwork. | `railroad`, `road` |
| `E3130–E3230` | Ticked line crosses diagonal near delta `(527, 324)` beside river artwork. | `railroad` |
| `E3217–E3318` | Ticked line crosses river-covered diagonal near west `(140, 270)`. | `minor-river`, `railroad` |
| `E3221–E3222` | Ticked line crosses vertical near west `(660, 295)` beside road. | `railroad`, `road` |
| `E3221–E3321` | Ticked line crosses diagonal near west `(566, 263)` beside road and river artwork. | `railroad`, `road` |
| `E3230–E3331` | Ticked line crosses diagonal near delta `(580, 210)`. | `railroad` |
| `E3318–E3417` | Ticked line crosses diagonal near west `(130, 151)` beside road. | `railroad` |
| `E3320–E3321` | Ticked line crosses vertical near west `(470, 185)` amid adjacent road ink. | `railroad`, `road` |
| `E3320–E3419` | **Unknown:** rail and road pass close to shared vertex near west `(407, 136)`; exact side attribution is obscured. | `railroad`, `road` |
| `E3320–E3420` | **Unknown:** branching rail and road converge at same vertex near west `(407, 136)`; exact side attribution is obscured. | `railroad`, `road` |
| `E3328–E3329` | Ticked line crosses vertical near delta `(300, 135)` beside road and river artwork. | `railroad`, `road` |

Positive readings agree with pinned rail, subject to scan and overprint limits. Unknown does not mean absent or positive. No unpinned side was surveyed for omission; no inference follows about continuity through junctions, construction, service, control, occupancy, or legal and unblocked rail network.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 1800 -y 2200 -W 2000 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-west
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/rail-e-west.png"); im.crop((280,900,1100,1400)).save("/private/tmp/rail-e-west-detail.png")'
pdftoppm -f 5 -l 5 -r 200 -x 2500 -y 2400 -W 2200 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/cairo-nile-north.png"); im.crop((760,750,1450,1710)).save("/private/tmp/delta-tanta.png")'
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-west.png /private/tmp/rail-e-west-detail.png /private/tmp/cairo-nile-north.png /private/tmp/delta-tanta.png /private/tmp/kafr-review-key.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; west full `1fd8e9da3d90c9a1cd3d917e6b71b0dcd604350a93617ca4b6e90b8e2f904b54`; west detail `6813736397595be3fdab35887437881426ecda9043cda0dff477679a93ea6571`; delta full `ef345c8066e55295c8146a6539b4b39a88d4f62d3f4670993ae43fa79f117cdc`; delta detail `3320df8825718c8cfb548433ae549e4509da7f7bd2e24b7e179a35ae7c9daaa9`; key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`; pin `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer and image library; crops remain outside Git. Documentation only; no runtime or map-data changes or gameplay tests.
