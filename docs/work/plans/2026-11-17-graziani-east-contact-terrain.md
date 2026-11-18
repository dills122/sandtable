# Eastern contact hexes: interior terrain leads

**Disposition:** Map C's visible interior supports clear-background terrain for C3020 and C3920, and a coastal land/sea reading for C3922 under the separately reviewed coordinate identification. These are bounded visual leads, not complete hexside certificates, ZOC outcomes or legal movement decisions. Named text must not be assigned to a hex solely because letters overlap it.

## Source observations

[Original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 Map C and p. 1 terrain key, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. The key depicts clear as an uncolored pale hex, coast as land plus blue water, village/bir as a blue dot, and separates roads, tracks, escarpments and other side features from background. Original scan, not a separate edition.

| Hex | Visible interior and naming | Limits |
| --- | --- | --- |
| C3020 | Number legible immediately east of Fort Maddalena C3019. Pale ground resembles key's clear background; the blue dot and `Bir el esc Scegga` label lie in/at this hex, supporting a village/bir mark. Black dashed track approaches the dot. | The blue checkered common side with C3019 is visible; political identity and effect remain interpreted separately. A village/bir mark is not automatically a different background terrain or proof of a complete route. See [Fort contact](2026-11-17-graziani-map-fort-maddalena-contact.md) and [border review](2026-11-17-graziani-contact-border-rules-review.md). |
| C3920 | Number legible; pale interior resembles clear. Dashed track touches/crosses its northern part; no blue village dot is visible in its interior. `Fort Capuzzo` lettering extends nearby, but the blue dot is in the neighboring C4020 hex under the coordinate reading. | Text overlap or a neighboring dot does not make C3920 the Fort Capuzzo village. Line crossings and nearby blue checkered boundary need separate side tracing. |
| C3922 | Neighbor immediately east of legible C3921 under the [Halfaya–Sollum side analysis](2026-11-17-graziani-map-halfaya-sollum-side.md); its own numeral is obscured. The shoreline cuts the northeastern part, with pale land and blue sea consistent with the key's coast symbol. `Halfaya Pass` text and road/side bands cross the local area; no separate interior blue settlement dot is established by this crop. | Coordinate is inferred from adjacent grid and source locators, not directly read. Crowded escarpment, shore, road and text prevent treating any side as fully certified; nearby Sollum's dot belongs to C4021 under that separate reading. |

Reproduce existing source crops (renderer differences can alter image bytes):

```sh
pdftoppm -f 3 -l 3 -r 500 -x 5400 -y 8600 -W 1900 -H 1650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/fort-contact-crop
pdftoppm -f 3 -l 3 -r 500 -x 6100 -y 6050 -W 1800 -H 1550 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/halfaya-contact-crop
```

Compare interior classification, dot location and identity with independent transcription and original scan before changing content. Preserve blue ink, coast, line features and unknowns separately; Land §8.37 terrain effects and §10.21 unit-entry and hexside restrictions cannot be determined from a background label alone. No claim of full edge coverage, player setup, positive ZOC, runtime change or executed test.
