# WT-03: Cairo-adjacent delta ink check

**Result:** Original Map E shows delta-colored terrain **inside E1729 and E1629**, immediately west of the Cairo/Nile artwork. This adds a bounded source observation for two named hexes; it does not by itself establish whole-hex weather classification, a legal road/rail path or any actual weather result.

## Source evidence

Visually inspected [map package](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 1 terrain key and PDF p. 5 Map E, and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) printed/PDF pp. 14 and 41. Verified local SHA-256 before inspection: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.

The Map A key labels a green-filled sample **Delta**. On the Map E Cairo crop, adjacent cells identified as **1729** and **1629** west of the blue Nile contain substantial matching green fill mixed with uncolored areas and other linework. The [review](2026-11-18-graziani-ledger-c-source-5-review.md) confirms `1629` is clear but finds `1729` partly overprinted; the latter identification uses remaining glyphs and grid sequence. The river, railroad and Cairo artwork overlap or adjoin the cell boundaries. This check targets the **interiors** of those two cells, not whether a particular blue line crosses a specific side or whether all adjoining features are complete. The [prior Cairo review](2026-11-17-graziani-e1730-six-sides-review.md) corroborated the key and noted nearby delta-colored ink without certifying whole-hex classification; these two named interior observations are the narrower follow-up.

Land §29.41 says sandstorms never occur over delta hexes on Map E. Section 29.58 restricts vehicle or motorized-unit entry into a delta hex during rainstorms unless on road or railroad, and states a separate stay-in-hex restriction for units already there at storm start unless on road or railroad. The latter is not an automatic sandstorm rule. Section 8.33 requires connected road or track hexes and a connecting side for movement benefits; it does not certify a crossing from a line drawn somewhere within a cell.

A candidate policy under which visible delta fill identifies a delta hex could use E1729 or E1629 as a discriminator between a map-selected sandstorm and a rainstorm. The inspected key illustrates delta but does not itself state a whole-hex coverage threshold or hierarchy for mixed delta, river and other printed features. Establish that terrain convention, side connectivity and any relevant starting unit and weather snapshot separately. The color match does not prove an actual player action, weather roll, unit location, state of any river or legal route. No policy is selected.

## Reproduction and limits

```sh
pdftoppm -f 5 -l 5 -r 300 -x 5000 -y 7270 -W 1100 -H 900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/delta-source5-cairo
pdftoppm -f 1 -l 1 -r 150 -x 700 -y 150 -W 1500 -H 2450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/delta-source5-key
```

This is an informed reading of the retained original scan, not independent-edition confirmation, a full terrain-rule or later-errata search, or a map-wide delta survey. No runtime/data edits or gameplay tests.
