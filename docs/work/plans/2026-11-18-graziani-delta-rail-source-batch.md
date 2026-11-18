# Map E delta railway: Tanta branch survey

**Date:** November 18, 2026. **Disposition:** Seven consecutive local sides north of the previously unresolved `E2529` vertex show ticked rail ink. Six have a pinned `railroad` feature; `E2927–E3027` instead has `unfinished-road` and `track`. Stop at the crowded junction in `E3329`; no onward side is assigned. This is a bounded scan reading, not a map correction.

## Evidence

Inspected page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), following the printed line from `E2628` through Tanta toward `E3329`. This starts beyond, and does not resolve, the [previously reviewed vertex](2026-11-18-graziani-cairo-nile-rail-followup-review.md). Blue transverse ticks mark rail; nearby paired dark lines mark road. Compared literal unordered endpoint pairs in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), available during inspection; comparison was not blind. Coordinates below refer to the detail crop, with north upward.

| Side | Scan observation | Pinned features |
| --- | --- | --- |
| `E2628–E2728` | Ticked line and adjacent road cross diagonal near bottom of detail. | `railroad`, `road` |
| `E2728–E2827` | Ticked line crosses diagonal near `(215, 780)`, south of branching ink. | `railroad`, `road` |
| `E2827–E2927` | Ticked line crosses diagonal near `(155, 635)` beside road. | `railroad`, `road` |
| `E2927–E3027` | Ticked line crosses diagonal near `(140, 535)`, south of Tanta city dot; road ink crosses nearby. | `unfinished-road`, `track` |
| `E3027–E3128` | Ticked line leaves Tanta and crosses diagonal near `(200, 435)` beside road. | `railroad`, `road` |
| `E3128–E3228` | Ticked line and road cross diagonal near `(265, 325)`. | `railroad`, `road` |
| `E3228–E3329` | Ticked line crosses diagonal near `(345, 220)` amid road artwork. | `railroad`, `road` |

All seven records have `side: null` and `up: null`. At the junction inside `E3329`, ticked branches and road overlap; this trace stops before choosing an onward branch. Pinned `E3328–E3329` and `E3329–E3330` both contain `railroad`, but neither is counted as an onward crossing here. The discrepancy at `E2927–E3027` is a same-scan comparison requiring independent review, not a proposed data edit. No inference about the unresolved southern vertex follows from this reading. Printed features do not establish service, control, occupancy, or a continuous usable route; other sides were not systematically checked.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 2500 -y 2400 -W 2200 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/cairo-nile-north.png"); im.crop((760,750,1450,1710)).save("/private/tmp/delta-tanta.png")'
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north.png /private/tmp/delta-tanta.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; full crop `ef345c8066e55295c8146a6539b4b39a88d4f62d3f4670993ae43fa79f117cdc`; detail `3320df8825718c8cfb548433ae549e4509da7f7bd2e24b7e179a35ae7c9daaa9`; pinned data `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG hashes depend on renderer and image library. Same-scan evidence is not independent-edition verification. No runtime or map-data changes; no gameplay tests.
