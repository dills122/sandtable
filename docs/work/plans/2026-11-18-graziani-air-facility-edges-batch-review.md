# Graziani air-facility edges: independent review

**Date:** November 18, 2026. Reviewed [batch](2026-11-18-graziani-air-facility-edges-batch.md) against its original map PDF and 300 dpi crops, plus the scenario PDF p. 4 / printed p. 28. Same source edition and scan, independent reading; not independent source corroboration.

## Result

The 60 rows cover six distinct geometric neighbors for each of ten targets. All 60 pinned-side descriptions match the pinned `hexsides.v1.json` records, including absent records. The twelve positive row claims represent eleven unique pairs because `E2132`–`E2133` appears from both ends. Source linework supports those bounded positive readings:

| Unique pair | Source observation |
| --- | --- |
| `E2132`–`E2133` | Blue cross-ticked railroad. |
| `E2132`–`E2031` | Blue cross-ticked railroad. |
| `E2133`–`E2134` | Blue cross-ticked railroad. |
| `A4728`–`A4628` | Blue railroad and dashed track cross near one another. |
| `A4728`–`A4627` | Paired road. |
| `B5526`–`B5527` | Paired dashed unfinished road. |
| `B5526`–`B5525` | Paired dashed unfinished road. |
| `B5825`–`B5925` | Paired road. |
| `B5825`–`B5726` | Paired road and brown slope band. |
| `E3614`–`E3615` | Paired road over blue water. |
| `B5331`–`B5430` | Dashed track crossing near upper corner of Bomba cell; retain vertex caution. |

These observations do not establish a complete inventory of features on any positive side. Other 48 rows remain unresolved, including rows with pinned features; absence of a pair record is not a clear-side assertion. No negative classification or route certification follows.

## Amiriya discrepancy

Scenario §60.5 explicitly prints **Amiriya E2212**. Map E2212 is pale and unnamed. Separate Alexandria-area map crop shows the Amiriya label overlapping the vicinity of E3312/E3313, but its black place dot lies **inside E3212**, just below the E3312/E3313/E3212 vertex. Batch wording “near 3312” describes the label vicinity, not the dot's cell; it must not be promoted to an E3312 place coordinate. Map dot and scenario facility row are different evidence: do not silently replace scenario E2212 with E3212 or treat them as aliases. Further source/errata review needed before any facility-location correction.

## Provenance and limits

Checked map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, all ten crop hashes, and both pinned data hashes against batch. Amiriya detail reproducible with `pdftoppm -f 5 -l 5 -r 300 -x 1700 -y 4650 -W 1100 -H 900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/air-review-amiriya-map`. Checked six-neighbor geometry and pinned feature lists mechanically; inspected positive crossings visually. This review does not certify all interior attributes, absent symbols, scenario aircraft placement, capacity, control, terrain, or movement. No runtime/data edits or gameplay tests.
