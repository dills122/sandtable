# D3516–D3517 rail conflict: source recheck

**November 18, 2026. Disposition:** Map D supports the earlier [positive rail reading](2026-11-17-graziani-d3516-d3517-rail-review.md). The [batch B review](2026-11-18-graziani-landing-strip-edges-b-review.md) entry saying no distinct crossing is established for D3516–D3517 is contradicted by the visible crossing. The batch B [source table](2026-11-18-graziani-landing-strip-edges-b.md) likewise omits the rail in its side description. This corrects those entries only; it does not establish which side either batch B reader actually inspected or why the crossing was missed.

In the batch's 500-dpi crop, printed `3516` lies inside the Gerawla cell left of the shared side; `3517` lies in the adjoining cell to its right. Labels are rotated relative to the crop; identify cells by their enclosed grid, not by the direction in which the numerals read. Their shared near-vertical side runs approximately `(855, 605)` to `(855, 790)`. A blue continuous centerline with repeated cross-ties passes through it near `(855, 688)`, between the vertices. The road and coast crowd the upper vertex. The lower-right diagonal from the lower vertex borders D3416, not D3517. Side confusion is possible but not demonstrable from the batch notes.

The same crossing appears near `(580, 580)` on the shared side `(580, 450)`–`(580, 740)` in the earlier 800-dpi crop. PDF p. 1 key depicts continuous blue cross-tied line as railroad and broken blue cross-tied line as unfinished railroad. The local centerline appears continuous; overprint and scan quality limit finer interpretation. This supports a **candidate missing railroad feature** on this one side, not a complete assessment of road membership, construction or service status, rail continuity, control, or legal movement. Road attribution near the upper vertex remains unresolved.

## Provenance and reproduction

Informed visual recheck of [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D PDF p. 4 and terrain key p. 1; earlier and batch notes were available beforehand. This is the same scan, not an independent edition or blind review. Local PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Re-rendered both map crops:

```sh
pdftoppm -f 4 -l 4 -r 500 -x 4195 -y 7050 -W 1400 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-d3516-batch
pdftoppm -f 4 -l 4 -r 800 -x 7500 -y 11800 -W 1300 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-d3516-conflict
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

Map crop SHA-256 values in order: `bb50045612a467a9a673038c78198aa45ad018d867430fe0cabf20ff5e208268` and `7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`. Inspected existing key crop SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`; command reproduces its stated crop. PNG hashes may vary by renderer. Pixel coordinates are approximate and crop-relative.

Pinned [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`, contains neither pair-key order. Absence means unrepresented, not verified featureless. Any admission needs separately reviewed versioned map data and replay assessment, preserving unknown other features. No runtime or map data changed; no gameplay tests run.
