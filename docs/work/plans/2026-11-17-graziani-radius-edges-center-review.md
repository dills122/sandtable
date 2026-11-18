# Graziani: C0716 radius center edges review

17 November 2026. Reviewed [center edge audit](2026-11-17-graziani-radius-edges-center.md) against [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and pinned `hexsides.v1.json`. This is a separate visual check after reading the audit, using the same scan, not a blind transcription, independent edition, or movement ruling.

**Disposition:** No contradiction found requiring a table change. Retain vertex and concealed-artwork qualifications; the audit is not a clear-edge certificate or authorization to change map data.

- The 13 listed cells have six listed neighbors each: 78 incidences, representing 55 unique pairs, with 23 shared pairs counted twice. Grid and printed numbers support the neighbor assignments and displayed ordering. Repeated pairs have matching visual and pinned labels; all 78 pinned labels match literal pair features in the JSON. This checks the listed pairs, not completeness of the whole radius dataset.
- Dashed route visibly crosses C0812–C0713 and continues toward C0714 and down the C07 sequence. The C0713–C0813 line lies close to the route; the vertex qualification is appropriate. Around C0716 and C0815, route ink, grid junctions and lettering prevent independently assigning every pinned vertex crossing with confidence. `V` must not be promoted to a confirmed crossing.
- Brown bands follow C0813–C0914 and C0813–C0814. The blue/black band follows C0719–C0618, C0719–C0718 and C0719–C0818; dashed track crosses the middle side. These observations alone do not establish terrain-key meaning, uphill direction, border semantics or a missing data feature.
- Yellow polygon art, oasis symbols and names overlap portions of this grid. `U` correctly means no positive side observation established, including obscured areas; it does not establish absence. Image resolution and shared scan limit confidence in fine crossing placement and terrain attribution.

Re-rendered Map C at 500 dpi using `pdftoppm -f 3 -l 3 -r 500 -x 3900 -y 13300 -W 2200 -H 3200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/radius-center-independent`, and checked the audit's 600 dpi center and west crops. PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; the audit's crop hashes and pinned hexside/hex hashes also match. No runtime or data changes; no gameplay tests.
