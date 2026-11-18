# Southern contact hexes: terrain interiors

**Finding:** On the inspected original Map C scan, all four requested hex interiors show the unpatterned base illustrated as **Clear** in the map key, with printed overprints noted below. C3419 and C3019 each contain a blue `Village/Bir` dot; their names and nearby routes do not turn the entire surrounding hexes or edges into villages. This is a visual classification of exposed interior artwork, not a blanket finding that any surrounding hexside is feature-free or legally passable.

| Hex | Interior evidence in source crop | Qualifications |
| --- | --- | --- |
| C3419 | `3419` is partly overprinted; the blue dot is inside this cell at the `Bir el Sheferzen` label. Single dark dashed tracks meet in the interior. Exposed background matches key's unpatterned Clear. | The name runs across the southeast side into C3320; the checkered blue line marks the west side. It is not a solid filled oasis symbol. |
| C3320 | Printed `3320` in the adjacent hex; exposed interior is unpatterned Clear. The `el Sheferzen` label overprints its upper portion. | The blue place dot is in C3419, not in C3320. The label and endpoint conceal parts of the shared edge; this is not proof of a clear edge or absence under ink. |
| C3520 | `3520` is legible; a single dark dashed track crosses the interior. Exposed background is unpatterned Clear. | Track is an overlay, not a second underlying terrain color. Shared diagonal C3419/C3520 carries a visible dash; see [side review](2026-11-17-graziani-map-first-rtr-side-review.md). |
| C3019 | `3019` is partly obscured by name/route ink; the Fort Maddelena blue dot and intersecting single dashed tracks are inside this hex. Exposed background is unpatterned Clear. | The blue checkered side shared with C3020 and the fort's rule implications are separately unresolved; the map dot matches the key's `Village/Bir`, not an independently printed fortification counter. |

[Map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) p. 1 terrain key distinguishes blank **Clear** from patterned Desert, Rock/Gravel and Rough, and gives the blue dot as `Village/Bir`, a single dark dashed stroke as `Track`, and a blue palm graphic as `Oasis`. The map print and scan have a yellow cast; the classification relies on matching **unpatterned** exposed artwork rather than absolute color. The [terrain effects chart](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) PDF p. 23 §8.37 states village/bir/oasis uses the underlying hex terrain for listed purposes. That chart does not itself prove fortification status, occupancy, road continuity, country ownership, or ZOC.

## Reproducibility and scope

Visually checked Map C PDF p. 3 and map-key PDF p. 1 in original scan SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Re-rendered with Poppler; crop pixel coordinates refer to each full page at the indicated DPI:

- Sheferzen / Rifle / RTR: `pdftoppm -f 3 -l 3 -r 600 -x 6600 -y 8700 -W 1700 -H 1450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/south-terrain-sheferzen`, PNG SHA-256 `9bf51282e635228ae349dcccda84906cacf781bb9e3d44177a265af1ea8855e5`.
- Fort Maddelena: `pdftoppm -f 3 -l 3 -r 500 -x 5400 -y 8600 -W 1900 -H 1650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/south-terrain-fort`, PNG SHA-256 `48a1dc731acd9137459c79e015e2f21984e8118b55378d079da18f0bac64bc2a`.

Renderer output hashes are reproducibility aids; the original scan and crop coordinates are the source. The key was inspected from the existing map-key crop, not independently redrawn. [Sheferzen border review](2026-11-17-graziani-map-sheferzen-border.md) and [Fort side review](2026-11-17-graziani-map-fort-maddalena-contact-review.md) describe the checkered mark and ink obscuration. No complete side audit, starting-stack legality or runtime changes are implied.
