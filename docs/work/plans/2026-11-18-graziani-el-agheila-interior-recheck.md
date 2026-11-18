# A1816: El Agheila dot near grid vertex

**Result:** The same retained scan supports the prior reading that the blue El Agheila dot is on the **A1816 side** of the lower grid junction. Its visible upper portion lies inside the target cell; the dot's lower edge and the neighboring boundary are crossed or crowded by paired road ink. The image is not sufficiently clean to certify every pixel of the dot as contained in A1816 or settle route-side attribution. No terrain or facility classification follows.

In the [Map A](https://spigames.net/PDFv10/CNA_Maps.pdf) crop, printed `1816` lies in the coastal cell between `1815` and `1817`, immediately above the `1716`/`1717` cells. The blue dot sits just above and slightly right of the shared lower vertex. The visible upper arc is separated from the lower junction by pale background; the bottom of the blue mark intersects the paired dark road, and those strokes crowd/obscure the nearby sloping grid boundaries. The northern blue water, pale land strip and El Agheila lettering supply orientation, not a complete terrain classification. The key's blue **Village/Bir** dot is consistent with the mark's visual form but does not distinguish village from bir or establish a landing strip.

This refines rather than overturns the [batch's near-junction observation](2026-11-18-graziani-landing-strip-edges-a.md) and [independent review](2026-11-18-graziani-landing-strip-edges-a-review.md): positive visible ink on the A1816 side is supported, while full-mark containment and exact A1716/A1717 road-side crossings remain uncertain. It does not authorize a pin correction, certify a map-printed facility symbol, or establish actual location, control or movement.

## Provenance

Re-rendered the earlier 300-dpi crop and a doubled 600-dpi view from the same PDF; `pdfimages -list` reports the embedded Map A image at **300 × 300 ppi**, so the 600-dpi render magnifies existing pixels rather than adding independent detail. Inspected the p. 1 terrain key separately. Commands:

```sh
pdftoppm -f 1 -l 1 -r 300 -x 2560 -y 6928 -W 1000 -H 1000 -png -singlefile /tmp/sandtable-maps.pdf /tmp/el-agheila-300
pdftoppm -f 1 -l 1 -r 600 -x 5120 -y 13856 -W 2000 -H 2000 -png -singlefile /tmp/sandtable-maps.pdf /tmp/el-agheila-600
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; 300-dpi crop `d77a953882a5594929a99534d2adf1d5fe7ffadec9330c56eddd72ccddaa5320`; 600-dpi crop `7c1a16d03d142f0fa848c117105ba9c7877852522c70ae5adde655a607cddc95` (renderer-dependent). Same scan, not a separate edition or physical-sheet check. No runtime/data edits or gameplay tests.
