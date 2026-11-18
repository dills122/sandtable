# Buq Buq C3926 six-side check: review

**Disposition:** The [six-side note](2026-11-17-graziani-c3926-six-sides.md) matches the original crop's labeled hex and three pinned route leads. Shoreline, name ink and the dot/junction limit what can be concluded about complete sides and legal movement.

Visually checked [Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, using the note's 500-dpi crop. `3926` is legible in the coastal hex; the Buq Buq dot lies close to its lower-right vertex. The key distinguishes single-dashed track from paired-dashed unfinished road.

| Neighbor | Original image | Pinned comparison |
| --- | --- | --- |
| C4025 | Upper-left diagonal shows thin grid in water. | No literal pair. |
| C4026 | Upper-right diagonal shows water/grid, with shoreline and name/route ink near eastern vertex. | No literal pair. |
| C3927 | Paired dashes cross the right vertical side near overprinted `Buq Buq` text. | `unfinished-road`. |
| C3826 | Single dashed track departs the dot/junction across the lower-right diagonal near the vertex; overlapping ink crowds the exact intersection. | `track`. |
| C3825 | Paired dashes cross the lower-left diagonal, aligned on either side. | `unfinished-road`. |
| C3925 | Left vertical grid side is crossed by the shoreline; no distinct route crossing seen in its exposed portion. | No literal pair. |

Direct inspection of `hexsides.v1.json` finds exactly these three named feature records involving C3926, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No record does not prove no source feature; a partly watery side is not automatically an all-sea side. The map dot and local lines establish neither full route continuity nor unit-specific entry or ZOC. Preserve obscured portions as unknown.

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Same-scan informed review, not another edition, a complete side/route audit, runtime change or gameplay test.
