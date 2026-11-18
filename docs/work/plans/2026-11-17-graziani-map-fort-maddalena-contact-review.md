# Fort Maddalena initial pair: independent map review

**Disposition:** corroborate [contact lead](2026-11-17-graziani-map-fort-maddalena-contact.md). Scenario names Fort Maddalena Garrison C3019 and 11th Hussars C3020; original Map C places the two adjacent across a blue-checkered vertical side. No broad escarpment band is visible there, but the mark and its rule effects are not certified clear. Adjacency does not establish a ZOC, attack or final legal setup.

## Source observations

[Scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27, §60.31 locates Fort Maddalena Garrison C3019; §60.41 places 11th Hussars (`R`, `7Spt/7`) C3020 with three medium trucks. These are fixed printed entries, not proof of the full post-choice stacks.

Independently rendered [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, at 600 dpi. The Fort Maddelena dot occupies the hex numbered `3019`; the adjoining `3020` is immediately east. Their shared vertical boundary has alternating blue blocks. No thick, splashed escarpment band appears along the exposed side, though the blue overprint and junctions prevent declaring every detail of the side clear. The blue dot in **C3020** lies under the separate `Bir el esc Scegga` label; it is not the Fort Maddelena dot. This local crop does not establish another similarly named location or identify a `Gialo` label; avoid replacing the explicit scenario coordinates with a name-based guess. The pinned hexside file has no literal `C3019|C3020` record; that omission is not source proof that the side has no feature.

The checkered line continues beyond the crop, and the separately rendered regional view in [boundary research](2026-11-17-graziani-border-mark-next.md) places it between `LIBYA` west and `EGYPT` east. The Libya–Egypt border reading and west/east country assignment are well-supported interpretations, not an explicit matching blue symbol in the map key. They require accepted regional membership and do not establish a movement or ZOC penalty. [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) p. 18 §10.21(b) excludes ZOC through an escarpment, while §10.21(c) independently requires that the projecting unit could enter the adjacent hex. Absence of an obvious band alone supplies neither positive projection nor permission to cross. Verify each stack's composition, strength, size, special conditions, ZOC, terrain, timing and §§10.31–.32 separately.

## Reproduction and limits

```sh
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/sandtable-scenarios.pdf /private/tmp/sandtable-land-rules.pdf src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
pdftoppm -f 3 -l 3 -r 600 -x 6300 -y 10100 -W 2400 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-fort-mad-review
```

SHA-256: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same-scan informed review, not blind or an independent edition; hash match does not grant rights. No runtime, map data, readiness flags or gameplay tests changed.
