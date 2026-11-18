# Review: Map E railway east branch from E3412

**Date:** November 18, 2026. **Disposition:** Agree with four counted crossings and conservative stop at Kafr el Douwar. No correction identified within this trace.

Reviewed [followup](2026-11-18-graziani-alex-rail-fork-followup.md) against PDF page 5 of the [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. This is a separate visual review of the same scan, not independent source corroboration; followup and pinned data were visible during review.

Rendered the followup's 700 dpi detail and a wider western crop to verify the starting label:

```sh
pdftoppm -f 5 -l 5 -r 700 -x 5450 -y 10400 -W 2200 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-fork-detail
pdftoppm -f 5 -l 5 -r 700 -x 4900 -y 10400 -W 1800 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-fork-review-west
```

Detail SHA-256 `ae1c57c72c45956ec7959fc3d3633ae9fbd4494a7ce528e914aebda804e37b58`; western crop SHA-256 `cb6dd91bba2558c3585a48dab91f5570bce8d3c027eda2449decb84148cc3014`. Renderer versions can change PNG bytes. Crops remain outside Git. Coordinates below refer to the detail.

| Side | Visual check |
| --- | --- |
| E3412–E3413 | Western crop shows printed 3412 and 3413 labels. Eastbound blue cross-tied line crosses their vertical boundary near `(180, 895)`, above its lower vertex. |
| E3413–E3414 | Cross-tied line crosses river and vertical boundary near `(620, 800)`. |
| E3414–E3415 | Same line crosses vertical boundary near `(1065, 715)`. |
| E3415–E3516 | Same line crosses river and diagonal boundary near `(1380, 590)`, away from the adjacent vertex. |

No counted crossing is on a vertex. At Kafr el Douwar, the cross-tied trace meets other rail and road ink near `(1640, 420)`; stop before attributing an onward branch. The scan labels the nearby location Kafr el Douwar and prints 3516 in the hex containing the junction.

Checked `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`: all four counted keys include `railroad`; E3413–E3414 and E3415–E3516 also include `minor-river`. E3516–E3517 includes `railroad` and `road`, but is not counted in this trace.

Other sides, northbound fork, map seams, operational service, and gameplay reachability remain outside this review. No runtime or map data changes; no gameplay tests.
