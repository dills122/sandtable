# E1829: delta artwork at Cairo

**Bounded observation:** The identifiable unobscured interior of E1829 shows pale background, blue city and river artwork, and overprinted lines and symbols. I cannot identify a clear patch of the key's green delta fill **inside** that interior; green fill reaches the outside of its western boundary. Heavy overprinting prevents a reliable assertion that no green exists anywhere in the cell. This observation does not settle whether the hex is a delta hex.

## Coordinates and source

The [map package](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 1 key labels a green sample Delta; PDF p. 5 shows the Cairo area. E1829 is the cell immediately upper-left of E1730 in the orientation of the crop below. Its numeral is obscured by city, unit-symbol and route artwork; identification follows the adjacent grid and the previously reviewed [six-side check](2026-11-17-graziani-e1730-six-sides-review.md), not an independently legible four-digit label. That review also records the map's city summary listing Cairo E1730/1829/1830/1930/1931. Map markings are not evidence of an actual unit stack.

The visible interior is unlike the substantial exposed green in nearby [E1729 and E1629](2026-11-18-graziani-ledger-c-source-5-review.md). At E1829, blue city blocks, black figures, river and route lines obscure material parts of the cell; the western green abuts a dark boundary with pale margin on the city side. No conclusion about unseen ink follows. The map key sample does not state a whole-hex mixed-terrain threshold, precedence between city and delta, or classification based on neighboring cells.

[Land §29.41](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 41, says sandstorms never occur over delta hexes on Map E. Applying that rule to E1829 still requires a supported terrain classification or explicit interpretation. This check establishes no storm, die result, legal action, route, unit state or additional map-side feature. The [opening-weather review](2026-11-18-graziani-weather-stage-scope-next-review.md) remains conditional.

## Reproduction and limits

```sh
pdftoppm -f 5 -l 5 -r 300 -x 5000 -y 6500 -W 1200 -H 1500 -png -singlefile /tmp/sandtable-maps.pdf /tmp/e1829-delta-source
pdftoppm -f 1 -l 1 -r 150 -x 700 -y 150 -W 1500 -H 2450 -png -singlefile /tmp/sandtable-maps.pdf /tmp/e1829-delta-key
```

Visually inspected the map crop and key, and Land p. 41. Local SHA-256: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. Same retained scan as prior reviews; no independent edition, full terrain-convention search, runtime/data edits or gameplay tests.
