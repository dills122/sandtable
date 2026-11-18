# C3419–C3520: Sheferzen and 1st RTR side

**Finding:** Original Map C shows a dark dashed track crossing the shared C3419/C3520 diagonal, consistent with the pinned `track` feature. The blue checkered national-boundary lead follows other sides to the west and does not appear on this diagonal. No escarpment band is visible on this side; unit eligibility, entry and complete source coverage remain separate ZOC gates. The source marks do not establish movement, control or compulsory combat.

## Reproduction and source identity

Reverified [original maps](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, PDF p. 3 Map C and p. 1 terrain key; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, PDF/printed p. 18 §10.21; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, PDF p. 3 §§60.31 and 60.41. Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` contains `C3419|C3520` as `track`, `up: null`.

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6600 -y 8700 -W 1700 -H 1450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/first-rtr-side/detail
```

Rendered PNG SHA-256 `9bf51282e635228ae349dcccda84906cacf781bb9e3d44177a265af1ea8855e5`; rendering versions may change bytes. Crop and source stay outside Git. This is an informed same-scan check following the [fixed-contact inventory](2026-11-17-graziani-fixed-contact-inventory.md), not a blind reading or independent edition.

## Physical observation

Printed `3419` identifies the Bir el Sheferzen dot hex; printed `3520` is immediately northeast. Their shared diagonal runs approximately `(580,690)` to `(770,800)` in the 600-dpi crop. A single broad dark dash crosses the grid near `(690,755)`, with its ink visibly extending to both sides. Further aligned dashes lead northeast through C3520 and southwest into the C3419 junction. The key's single dark dashed line is `Track`, distinct from paired unfinished-road marks and blue cross-tied unfinished railway. This bounded source reading supports the pinned feature; it does not certify further track sides or the whole route.

The side itself appears as thin grid except where crossed by that dash. The key depicts escarpment as a broad, dark band with splash; no such band is visible on the diagonal. No blue checkered border mark follows this side. The separate checkered line runs along C3419's western side and west of C3520; the prior [boundary check](2026-11-17-graziani-map-sheferzen-border.md) supports interpreting that line as Libya–Egypt boundary, while the key's gray border sample does not directly identify its blue pattern. Both cell interiors appear on its eastern side under that geographic interpretation. The source reading does not certify absence of all other subtle or hidden features or decide country membership for every hex.

Scenario §60.31 lists the Sheferzen Garrison C3419; §60.41 lists 1st RTR C3520, with five medium trucks. Land §10.21(b) blocks ZOC through escarpments and §10.21(c) separately bars projection into a hex a unit cannot enter. Observed lack of an escarpment is only one conditional input. The track does not create a ZOC by itself: actual unit and stack strength, size, cohesion, current setup, entry, side policy and all other relevant rules need accepted evidence. Nationality boundary, movement cost and political placement are separate from a ZOC ruling.

Independent review should verify labels, the dash crossing and any subtle side features, then check each initial stack and map policy before admitting ZOC or movement. Suggested tests cover reciprocal feature lookup, track crossing with both directions, no inferred escalation to full route, independently qualifying stacks and unknown/known incoming ZOC. No runtime data, readiness or gameplay tests changed.
