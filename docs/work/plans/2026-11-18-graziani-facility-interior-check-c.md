# Facility interior check C: E3614 and B5331

**Scope:** Independent visual inspection of two named hex interiors in the same retained [map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), compared with the [first-reading batch](2026-11-18-graziani-air-facility-edges-batch.md) and its [review](2026-11-18-graziani-air-facility-edges-batch-review.md). The scenario names Alexandria E3614 and Bomba B5331 as flying-boat basins; that listing is distinct from what the map shows. This check does not certify a facility, terrain classification, sides, ownership, capacity or legal use.

- **E3614:** The printed `3614` sits inside the cell immediately right of the Alexandria city-symbol cell in the displayed crop; surrounding numbered cells and visible grid support the placement. Its interior includes substantial green fill resembling the key's **Delta** sample, a blue oval water shape, pale margins, dark route strokes and overprinted “Alexandria” text. The green and water artwork do not alone determine the whole-hex terrain or legal effect. I do not distinguish a locality dot or a flying-boat-basin symbol in the exposed interior; dense lettering and linework limit negative claims. The nearby circular anchor lies in the adjacent city cell, not a basis to assign a basin to E3614. This supports the first reading's dense artwork and lack of a **distinct identified** basin symbol, not proof that no symbol or feature exists under overprint.
- **B5331:** Printed `5331` is visible in a pale narrow tongue enclosed by the target grid, with blue water covering much of the same cell. A blue dot lies inside that tongue, above the numeral, and the “Bomba” label extends east across the water. The key depicts a blue dot for **Village/Bir** and a separate airplane/anchor symbol for **Flying Boat Basin**. The dot is a positive locality-mark observation; it does not distinguish village from bir or itself certify a basin. I do not identify a distinct basin symbol in the exposed target interior; the name, linework and scale limit an absence claim. This agrees with the first reading's Bomba dot/name and coastline observation without promoting the pinned settlement or coastal field to source-certified classification.

The key uses green partial fill for Delta, blue for Sea and the distinct Village/Bir and basin examples. These graphic comparisons do not define a mixed-hex terrain threshold, a facility-placement rule or a valid route. No airplane, actual unit or player decision is inferred.

## Reproduction and provenance

```sh
pdftoppm -f 5 -l 5 -r 300 -x 2215 -y 4030 -W 800 -H 800 -png -singlefile /tmp/sandtable-maps.pdf /tmp/interior-c-e3614
pdftoppm -f 2 -l 2 -r 300 -x 5505 -y 1345 -W 800 -H 800 -png -singlefile /tmp/sandtable-maps.pdf /tmp/interior-c-b5331
pdftoppm -f 1 -l 1 -r 150 -x 700 -y 150 -W 1500 -H 2450 -png -singlefile /tmp/sandtable-maps.pdf /tmp/interior-c-key
```

Verified map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Same original scan as prior notes, independently rendered and inspected; not an independent physical edition or a full facility/terrain audit. Scenario identities are carried from the [worklist](2026-11-18-graziani-facility-interior-worklist.md), not independently re-read here. No map/data/runtime changes or gameplay tests.
