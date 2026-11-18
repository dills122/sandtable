# Independent visual review: Map E railway east of E3108

**Date:** November 17, 2026. **Disposition:** Six proposed sides are plausible readings of the printed railway and agree with pinned data. The E3210–E3211 attribution is especially uncertain: ink meets a three-hex vertex. Treat all six as visual candidates, not a verified route or a basis for a data edit. This review uses the same scan and had the proposed sides and pinned data available; it is not a blind transcription or independent source edition.

## Evidence and method

Viewed [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 5 (Map E), and the [extension assessment](2026-11-17-graziani-alex-coastal-rail-extension.md). Locally checked SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. The assessment's 350 dpi crop also matched its reported hash. Rendered a separate detail for vertex inspection:

```sh
pdftoppm -f 5 -l 5 -r 700 -x 3500 -y 11100 -W 2400 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-review-detail
```

Detail PNG SHA-256 `241b8943473b95f9e6fbc29c6d52a60c139db77875745e5e18bfdd5b2d024e27`; renderer versions may change bytes. Crop remains outside Git. Blue cross-tied line is the railway symbol; dark parallel lines near Amiriya are separate road ink.

| Candidate side | Independent visual assessment | Pinned feature |
| --- | --- | --- |
| E3108–E3109 | Cross-tied line crosses vertical boundary under Burg el Arab lettering. | `railroad` |
| E3109–E3209 | Cross-tied line crosses diagonal boundary amid settlement and road marks. | `railroad` |
| E3209–E3210 | Cross-tied line crosses vertical boundary. | `railroad` |
| E3210–E3211 | Line passes essentially through the junction of the vertical boundary and two diagonals. Thick ink and grid prevent a confident side-only attribution; candidate remains plausible. | `railroad` |
| E3211–E3312 | Line crosses diagonal close to its western vertex. Separation is visible at higher resolution, but line width limits precision. | `railroad` |
| E3312–E3412 | Line crosses diagonal near its eastern vertex; railway and grid are distinguishable despite nearby road. Attribution remains sensitive to ink width. | `railroad` |

The apparent junction of the northbound and eastbound cross-tied lines lies inside E3412, east of the last candidate crossing. Stopping there avoids choosing a branch. Pinned E3412–E3513 and E3412–E3413 each contain `railroad`; their continuation crossings were not independently surveyed here.

## Limits

One scan and local visual interpretation cannot settle a vertex convention, prove railway operation, establish a Matruh–Alexandria route or D/E seam, or establish a qualifying gameplay witness. Other features and sides were not exhaustively checked. No map or runtime changes; no gameplay tests.
