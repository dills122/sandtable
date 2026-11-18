# Map E rail: seven unresolved pairs rechecked

**Date:** November 18, 2026. **Disposition:** One provisional source-positive crossing; six remain unknown. This is an informed reinspection of the same scan, not independent-edition or blind corroboration. Candidate pairs, pins, and prior notes were visible. No map-data change follows.

Viewed [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), page 5, and page 1 terrain key. Key distinguishes continuous blue cross-tied railroad, broken unfinished railroad, paired road, and blue river. Printed hex labels and grid locate sides; labels, line width, river, road, city and figure overprints limit precision. Pixel locators below use unrotated crops, origin upper left. A positive describes only the named side, not junction connectivity, route status, legal movement, or occupancy. Unknown means neither positive nor proof of absence.

| Pair | Assessment | Evidence and limit |
| --- | --- | --- |
| `E3023–E3024` | **Source-positive, provisional** | In delta crop, shared vertical is near x=400, from y=340 to y=565; blue ticked alignment crosses near (400, 440), below the upper vertex, with ticks visible on either side. Road and river overlap the crossing, so line identity and exact crossing remain qualified. This is a stronger reading than [sweep C](2026-11-18-graziani-map-e-rail-sweep-c.md)'s unknown and warrants independent visual challenge. |
| `E3210–E3211` | **Unknown** | Cross-tied line meets the junction of vertical and diagonals near (1250, 720) in the 700-dpi `alex-review-detail`; ink width prevents secure side attribution. Agrees with [coast review](2026-11-17-graziani-alex-coastal-rail-extension-review.md). |
| `E3320–E3419` | **Unknown** | In west crop, ticked line, paired road and diagonal grid converge near the vertex around (565, 410); crossing cannot be isolated from overprint with confidence. |
| `E3320–E3420` | **Unknown** | Same west vertex and branching ticked ink; separation from the adjacent diagonal is insufficient for a secure side attribution. Both readings agree with [sweep E review](2026-11-18-graziani-map-e-rail-sweep-e-review.md). |
| `E3513–E3613` | **Unknown** | Alexandria crop: route crosses the shared diagonal near (670, 1060), but road, blue water, city and figure artwork obscure whether rail itself crosses. |
| `E3613–E3714` | **Unknown** | Alexandria crop: side near (950, 810) lies under route, city blocks and figures; blue marks do not establish a separate continuous ticked crossing. |
| `E3714–E3715` | **Unknown** | Alexandria crop: vertical near x=1190 passes through road, figures and blue route near y=650. Ticked rail is clear farther east, but continuity through the side is obscured. These three maintain [E3613](2026-11-17-graziani-e3613-six-sides-review.md) and [E3714](2026-11-17-graziani-e3714-six-sides-review.md) review qualifications. |

No side is classified visually unsupported: crowded ink does not establish absence. No omission survey or gameplay tests; no runtime or data changes.

## Reproduce

```sh
pdftoppm -f 5 -l 5 -r 600 -x 8580 -y 10440 -W 1800 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/unknown-c-delta
pdftoppm -f 5 -l 5 -r 600 -x 6900 -y 9300 -W 1800 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/unknown-c-west
pdftoppm -f 5 -l 5 -r 600 -x 4200 -y 8000 -W 1900 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/unknown-c-alex
pdftoppm -f 5 -l 5 -r 700 -x 3500 -y 11100 -W 2400 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-review-detail
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/unknown-c-delta.png /private/tmp/unknown-c-west.png /private/tmp/unknown-c-alex.png /private/tmp/alex-review-detail.png /private/tmp/kafr-review-key.png
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; delta `ad421dd4b6fd49dd455f7c53092b946385c41007e4f338bc21545c234c0d8164`; west `5952a3ac50edbfd98818576be717e95440d1cf65e4b4df56053b61759539d1cd`; Alexandria `b7111d0c8785ee3fef390462bb8684bf67120909d01b6236b69af56a213ce9d8`; coast `241b8943473b95f9e6fbc29c6d52a60c139db77875745e5e18bfdd5b2d024e27`; key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`. PNG hashes depend on renderer; crops remain outside Git.
