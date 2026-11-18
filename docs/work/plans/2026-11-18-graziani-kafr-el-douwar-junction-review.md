# Independent visual review: Kafr el Douwar junction

**Date:** November 18, 2026. **Disposition:** The three surveyed railway sides and the two road crossings are plausible and match the pinned entries. The E3516–E3616 side also visibly carries a blue river-like band, but has no explicit pinned pair. Retain that discrepancy for review; this reading alone does not authorize a data correction.

Reviewed the [junction survey](2026-11-18-graziani-kafr-el-douwar-junction.md), [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 5, and terrain key on p. 1. Printed 3516 and neighboring numbers establish the sides in the survey's unrotated crop. The survey and pin were available during this reading; this is not a blind comparison or independent edition.

| E3516 neighbor | Visual check | Pinned features |
| --- | --- | --- |
| E3515 | No obvious road or railway across left side. | No explicit pair. |
| E3615 | Blue cross-tied line and paired dark road lines cross upper-left side; blue band follows the side beneath them. Lettering and linework overlap near the crossing. | `minor-river`, `railroad`, `road` |
| E3616 | Blue band follows upper-right diagonal from its western vertex to the eastern vertex; no obvious road or railway crossing. This is a local river-like appearance, not a cross-tied line. | No explicit pair. |
| E3517 | Cross-tied line and paired dark lines cross right side, with crowded ink and number near the railway. | `railroad`, `road` |
| E3416 | No obvious road or railway across lower-right side; lettering overlaps it. | No explicit pair. |
| E3415 | Cross-tied line crosses lower-left side; blue band follows the side. | `minor-river`, `railroad` |

The key shows solid blue cross-tied **Railroad**, paired dark continuous **Road**, and distinct **Minor River** and **Major River** symbols. The E3516–E3616 band looks closer to the one-sided minor-river example than to the major-river example, but scan, scale, and overprinting limit confident classification. The neighboring E3615 and E3415 river-bearing sides appear similar and are pinned `minor-river`; that comparison is corroboration, not an independent classification. Missing pairs do not certify absence or clear terrain. Overlapping railway, road, settlement dot and text near the junction do not establish branch connectivity, a continuous Alexandria route, or movement legality.

Reproduced the survey's 700-dpi crop using its command and rendered the key:

```sh
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; survey crop SHA-256 `852e8c72f591c3025e4eec13a7c51fe04e53d0207bf845144862f974b9550443`; key crop SHA-256 `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`; pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes can vary by renderer. Pin identifies `vassal:CNAv2.1.0`; scan and pin are distinct source representations. No runtime or data changes; no gameplay tests.
