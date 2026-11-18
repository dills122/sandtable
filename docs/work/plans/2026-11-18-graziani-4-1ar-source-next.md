# SU-02: additional counter-section check for 4/1AR

**Result:** A bounded check of two **additional** Italian counter areas found no further face labeled `4/1AR`. This does not resolve §60.31's repeated text or missing punctuation, prove a complete counter inventory, or establish an official correction. The prior [4/1AR source note](2026-11-17-graziani-4-1ar-next.md) and [review](2026-11-17-graziani-4-1ar-review.md) already checked the positive Section 2 counter, OA and scenario line; this follow-up did not present those same readings as new evidence.

## New bounded check

Visually inspected the [original counter PDF](https://spigames.net/PDFv10/CNA_Counters.pdf) PDF p. 1 **Counter Section 1**, both division-counter blocks, and PDF p. 2 **Counter Section 3's Italian Blackshirt land-counter area**. Section 1's header says quantity of that identical section is one; its visible faces are organized under named formations and I did not identify a `4/1AR` face in either block. The Section 3 Blackshirt land area likewise shows named CCNN and Blackshirt formation counters, with no `4/1AR` face identified in that inspected area. These are bounded negative observations of the reproduced faces, not a machine-readable census; obscured print, another section, reverse faces, separate correction sheets or another edition could matter.

The positive `4/1AR` face previously found in Section 2 and the single OA row still support **one candidate identity**, not a source-proven publisher correction of the repeated scenario occurrence. Section 1 and the Blackshirt block are different areas of the **same** scan, not an independent copy of the original counter sheet. Their negative results do not establish that the second scenario occurrence is a typo, that no second differently marked identity exists, or that `4/2AR` was intended. The explicit erratum identifying duplicate XXI Corps artillery remains separate. Retain both raw scenario occurrences, exact missing separator and the open source conflict until a relevant correction or independently sourced explanation is found.

## Reproduction and limits

Verified counter PDF SHA-256 `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`. Rendered the areas using:

```sh
pdftoppm -f 1 -l 1 -r 200 -x 0 -y 0 -W 2200 -H 1400 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/fourone-section1
pdftoppm -f 2 -l 2 -r 200 -x 0 -y 400 -W 2200 -H 700 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/fourone-section3
```

The first crop ends at Section 1's lower boundary; the second includes the Blackshirt area, not every Section 3 counter. Other sections, backs and alternate scans were not comprehensively inventoried. Prior sources were consulted for context; scenario, OA and errata were not freshly re-read for this bounded check. Same-edition source bytes are not independent provenance or redistribution permission. No runtime/data edits or gameplay tests.
