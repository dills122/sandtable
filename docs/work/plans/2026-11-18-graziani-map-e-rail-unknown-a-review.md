# Map E rail unknowns: visual challenge

**Date:** November 18, 2026. **Disposition:** Support five qualified side crossings and retention of one unknown in [unknowns recheck A](2026-11-18-graziani-map-e-rail-unknown-a.md). This is a separate visual assessment with the candidate pairs and prior reading visible, using the same scan; it is neither blind nor independent-edition corroboration. No map-data change follows automatically.

Compared page 5 of the [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf) with its page 1 terrain key. The key depicts railroad as a blue continuous line with cross-ties, unfinished railroad with a broken centerline, and road as paired dark lines. Grid labels and vertices identify the sides. These findings concern visible local crossings, not a route or gameplay consequence.

| Pair | Assessment | Visual basis and limit |
| --- | --- | --- |
| `E0929–E1029` | Support, qualified | Blue cross-tied line intersects the diagonal shared by the labeled hexes, away from its endpoints. Parallel dark road ink crowds the intersection. |
| `E1130–E1229` | Support, qualified | Blue line and ties cross the diagonal between the labeled hexes, between its vertices. Dark road strokes overlap the local trace. |
| `E1927–E2026` | Support, qualified | Ticked blue centerline crosses the shared diagonal on the `E1927` side of the three-hex vertex, rather than the adjoining `E2026–E2027` side. The gap to the vertex is small relative to the grid and printed line widths, so exact placement remains qualified. |
| `E1929–E2029` | Support, qualified | Northbound blue ticked line intersects the shared diagonal left of the city-side vertex. Paired road strokes accompany it; city and training artwork obscure the continuation. This does not establish a connection through the city. |
| `E1930–E2029` | Unknown | The named diagonal runs from the city-side vertex toward the river. The visible northbound line crosses the adjacent `E1929–E2029` side before turning toward the city; overprinted city, road and river artwork prevent a confident complete reading of the named side. Neither crossing nor absence is established. |
| `E2124–E2223` | Support, qualified | Cross-tied blue centerline intersects the named diagonal before its river-side vertex, on the `E2124–E2223` side rather than the adjoining `E2124–E2224` side. Proximity and printed line width limit positional precision. |

## Evidence and reproducibility

Inspected unrotated 300-dpi page 5 crops L, M and C and page 1 key crop as specified in the [recheck's reproduction commands](2026-11-18-graziani-map-e-rail-unknown-a.md#reproduction), including enlarged local views. Enlargement does not add source detail. Source PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Crop SHA-256 values: L `8eb60a0f4581a81263324910ce4a57f99d529e80274e35d810d26e2c9ee52816`; M `ee0df7def4b2ffc194b9c55b822f3e0291e9b1770305cbf70644ef36390bd0b1`; C `71d0a06117292c44109b0c95176ca8d8e1f0c937253941f5fbe412618ed6c5ef`; key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`. Crop bytes depend on renderer.

Side readings do not establish adjoining crossings, junction connectivity, service, control, occupancy, or movement legality. No runtime or map-data edits; no gameplay tests.
