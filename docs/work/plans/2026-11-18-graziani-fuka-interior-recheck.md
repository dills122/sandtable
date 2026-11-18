# Fuka D3323: locality-dot containment

**Result:** The Fuka-associated round blue mark is inside the numbered D3323 cell in the retained map scan. This answers the specific containment question left open in the [interior worklist](2026-11-18-graziani-facility-interior-worklist.md); it does not settle whole-cell terrain, a landing-strip symbol, control, route ownership or placement legality.

Visually re-rendered [Map D](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 4 at 300 dpi, matching the `pdfimages -list` embedded page-image resolution of 300 × 300 ppi, and checked the page-1 key. Printed `3323` is partly crossed by the blue railroad; neighboring `3322`, `3324`, `3423` and `3223` and the surrounding grid place its hex. In the 1000-pixel crop, the blue round mark is approximately `(518, 257)`, within the 3323 polygon: its upper vertex is near `(507, 195)`, right upper corner near `(600, 248)`, and left vertical near `x=413`. The mark lies below the top diagonal and left of the right boundary, not on the junction. These positions are approximate crop-relative visual locators, not a precision survey.

Fuka lettering, paired road ink and a dark dashed route crowd or overlap the mark; blue cross-ticked railroad runs immediately below. The mark is consistent with the key's blue **Village/Bir** symbol, whose shared sample does not distinguish those categories. A dot is not the key's separate airfield symbol. Overprint limits any claim that no other symbol lies underneath. The prior [first reading](2026-11-18-graziani-landing-strip-edges-a.md) recorded the location and side leads, while its [independent review](2026-11-18-graziani-landing-strip-edges-a-review.md) reported the visible dot without explicitly locating it in the target. This informed same-scan check supplies that bounded containment observation, not an independent edition; prior near-vertex route cautions remain.

## Provenance

Original PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; p. 4 crop SHA-256 `857f20d41f45acefc10a275f7ba133e2b8ee8c3a9611e8c06a404c98d2e99b53`, matching the first reading. Key crop SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`.

```sh
pdftoppm -f 4 -l 4 -r 300 -x 3748 -y 4672 -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/fuka-native-review
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

PNG hashes depend on renderer. No runtime/data changes or gameplay tests.
