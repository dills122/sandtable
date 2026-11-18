# Graziani Cairo rail: northward extension and first uncertain junction

**Date:** November 17, 2026. **Disposition:** Eight additional local sides examined in original Map E and pinned data. Northbound ticked blue ink runs beside road artwork; a westward ticked branch crosses a Nile channel near `E2129`. City artwork and river/road overprint limit exact attribution. Stop at the next river junction: the scan does not settle a rail crossing of `E2329–E2428`, and its pinned record has no railroad feature. These observations neither establish an occupied hex nor certify a continuous usable route.

## Source comparison

Read [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), page 5, and its page 1 symbol key, against literal unordered pairs in [pinned hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). This extends the bounded [Cairo rail batch](2026-11-17-graziani-east-rail-cairo-batch.md) and [same-scan review](2026-11-17-graziani-east-rail-cairo-batch-review.md); their seed and eight candidate sides are not re-certified here. Labels and grid continuity identify sides; river ink, road lines and city silhouettes can hide the exact crossing.

| Side | Source reading | Pinned features |
| --- | --- | --- |
| `E1930–E2029` | Northward rail appears to enter the Cairo city/river complex; dense artwork obscures its precise side crossing. Candidate only. | `railroad` |
| `E1929–E2029` | Rail, road and river crowd the northern edge of the same city complex. Exact rail-side attribution and relation to the preceding candidate remain uncertain. | `railroad`, `road` |
| `E2029–E2129` | Ticked blue northbound line runs beside road artwork across the diagonal. | `railroad`, `road` |
| `E2129–E2229` | Ticked blue line continues north beside the road across the diagonal. | `railroad`, `road` |
| `E2229–E2329` | Ticked line continues north alongside road across the diagonal; converging ink limits precision. | `railroad`, `road` |
| `E2128–E2129` | Westward ticked branch crosses the Nile channel and the shared side; river ink overlaps the rail. | `minor-river`, `railroad` |
| `E2127–E2128` | Westward ticked branch crosses the vertical side within the river bend. | `railroad` |
| `E2329–E2428` | Northward continuation reaches a river and road junction; overlapping lines and a nearby vertex make exact rail-side crossing uncertain. Do not infer railroad membership from apparent continuation. | `major-river`, `road` |

All eight records have `side: null` and `up: null`. Pinned river and road features are literal comparisons, not independent adjudications of their classifications. In particular, a rail drawn across water is a printed crossing observation, not proof of bridge condition or service. The west branch's `E2128–E2129` river crossing is distinct from the unresolved northward river junction; neither establishes a crossing at any adjoining side. The city figures are printed training-area artwork, not evidence of actual scenario occupancy.

These are finite printed-ink candidates, not an eastern-rail census or an Axis witness. Construction, destruction, occupancy, combat classification, snapshot, and geographic west relation to the selected Matruh hex remain separate prerequisites. No negative witness conclusion follows. Same-scan checking is not independent-edition verification or a rights grant.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 2600 -y 4050 -W 1750 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-rail-junction
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-rail-junction.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `cdebc04ec593d0a74b9ca6cd486d7c15b89a8f77d98792f9edd1dc516dfe67a9` (renderer-dependent); pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crop remain outside Git. No runtime or map-data changes; no gameplay tests.
