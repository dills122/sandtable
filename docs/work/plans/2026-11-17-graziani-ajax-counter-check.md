# Ajax counter and Land damage example: source check

**Finding:** The scanned Ajax counter reads `CL 3/6`, supporting initial Gun 3 and AA 6. Land §30.34 instead calls Ajax a 4, then gives Gun 2 and AA 4 after two damage points. This is a counter/example discrepancy, not evidence that Ajax starts with AA 8 or that damage leaves its AA unchanged.

## Evidence and interpretation

- [Counter sheets](https://spigames.net/PDFv10/CNA_Counters.pdf), PDF p. 3, Counter Section 6, Commonwealth block, bottom-right fleet rows: `Ajax` appears between `Arethusa` and `Euryalus`, marked `CL 3/6`. The neighboring `QnElizabeth` counter reads `BB 6/12`. The name and digits were visually checked at 300 dpi, not inferred from OCR.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 41 §30.12, identifies the two strengths as Gun and AA and gives Queen Elizabeth initial Gun 6 and AA 12. Her matching counter establishes the `6/12` field order, supporting Ajax Gun 3, AA 6. Section 30.13 says AA points decrease in ratio to lost Gun points.
- Land p. 42 §30.34 describes damage in Gun Rating Points and its hypothetical Ajax as Gun 4 before two damage, Gun 2 and AA 4 afterward. The example itself does not state initial AA. A hypothetical starting Gun 4/AA 8 yields its printed result under proportional reduction, but the scanned Ajax face instead says 3/6. Applying the same proportional reading to that face and two Gun damage yields Gun 1/AA 2; this is a calculation, not the printed example or an actual played event.
- Land §30.23 also calls Ajax a heavy cruiser, whereas its face says `CL`. The same fleet row labels York and Fiji `CA 4/8`; this comparison helps identify the discrepancy but does not prove which source should control or that either ship was intended in the example.

Do not silently change the counter, rewrite the example, infer a general rounding policy, or treat the example as a correction. This check does not establish the counter's provenance beyond the scanned package, any later correction, or the ship's availability in a particular scenario. Independent physical-copy or edition evidence and a documented ruling remain useful before source admission.

## Reproduction and identity

Scanned packages checked locally on November 17, 2026:

| Package | SHA-256 |
| --- | --- |
| Counter sheets | `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9` |
| Land rules | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` |

Hashes identify inspected bytes, not authenticity, licensing, or independent editions. Source images remain outside Git. For a local matching copy:

```sh
pdftoppm -f 3 -l 3 -r 300 -x 1650 -y 3650 -W 1650 -H 750 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/ajax-counter-check
```

This follows the [damage review](2026-11-17-graziani-aa-damage-followup-review.md), which correctly left Ajax's initial AA unresolved before counter identification. No runtime or adjudication change is made here.
