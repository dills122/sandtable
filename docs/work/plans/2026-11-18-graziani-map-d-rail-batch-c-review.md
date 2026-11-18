# Map D pinned rail batch C: source review

**Disposition:** Supports [batch C](2026-11-18-graziani-map-d-rail-batch-c.md)'s exact-side generic rail crossings. The candidate's class-unknown disposition is cautious, but its description of a largely continuous local centerline does not match this reader's enlarged view: repeated short gaps are visible along the blue run, including near the diagonal, consistent with the key's unfinished-rail symbol. This is evidence for the pinned class, qualified by scan softness, ties, numbers and grid; reconcile the differing readings before promoting all four class judgments.

- `D3606|D3607`, `D3607|D3608`, `D3608|D3609`: blue cross-tied ink intersects each shared side near its middle, away from vertices. In the unrotated crop these sides are vertical; candidate's horizontal description uses rotated view. Grid, ties and numbers crowd crossings. The nearby blue centerline shows repeated separated segments between ties, especially visible at 600 dpi.
- `D3609|D3710`: blue cross-tied line bends and crosses the shared diagonal away from its vertices, closer to the vertex adjoining D3610. Local gaps appear in the blue line; grid and a cross-tie overlap the crossing. Paired black road strokes lie farther into D3710; do not attribute them to this crossing from proximity.
- Key shows an interrupted blue centerline for unfinished railroad and continuous centerline for railroad. These observations concern printed ink only. Same scan, informed second reading after the candidate, not an independent edition. The candidate's unknown class disposition remains a legitimate conservative treatment of scan ambiguity; this review does not certify physical construction or a route.

Inspected [Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1. Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` checked during batch A. Reproduce unrotated detail views:

```sh
pdftoppm -f 4 -l 4 -r 600 -x 1700 -y 8800 -W 2400 -H 450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-c-review
pdftoppm -f 4 -l 4 -r 600 -x 3100 -y 8350 -W 1100 -H 650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-c-diagonal-review
```

First PNG SHA-256 `877b9d3d06f30206e60621e1da475da98c29c6b1f451948e6f8801a8aa3d5a11`. PNG bytes depend on renderer. No data or runtime changes; no gameplay tests.
