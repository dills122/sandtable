# Graziani landing-strip batch B: independent visual challenge

**Subsequent correction:** A [targeted recheck](2026-11-18-graziani-d3516-rail-conflict.md)
re-renders the D3516–D3517 vertical side and supports the earlier blue
railroad reading. This review's table below missed that crossing; it does not
invalidate its other separately qualified observations.

**Date:** November 18, 2026. Independent reading of the same [original map scan and key](https://spigames.net/PDFv10/CNA_Maps.pdf), not an independent edition. Compared batch B against the pinned `hexsides.v1.json` and `hexes.v1.json` transcribed in [batch B](2026-11-18-graziani-landing-strip-edges-b.md). Re-rendered its ten 500 dpi crops using its stated coordinates. Key distinguishes solid double road, dashed double unfinished road, single dashed track, solid railroad, dotted unfinished railroad, and village/bir dot; a dot alone does not distinguish village from bir. Scenario identity comes from its list, not a printed landing-strip symbol. Edge order and pair IDs follow batch B; `—` is absence of a pin record, not proof of no feature. `up` directions not adjudicated.

## Material challenges

- **D3231–D3232:** both black parallel road lines and blue cross-tied railroad cross the vertical grid side. Pin has railroad only. This is stronger than the draft's railroad-only observation; check against the source and transcription policy before changing data.
- **B5917–B5918:** the two black lines crossing the side appear continuous, matching key's **road**, not dashed-double unfinished road pinned for that pair. The scan and terrain overprint warrant a second look, but the draft's generic “parallel route” hides the apparent type disagreement.
- **B5229–B5130:** the thick single dashed approach is the key's **track**, not unfinished road. Its last dash ends at the road near the shared lower-right vertex; a crossing of the B5229–B5130 side is not clearly established. Pin contains wadi and unfinished-road. Wadi follows the side; route type and side assignment remain disputed.
- **D3903 coastal:** shoreline is visibly inside northern neighbors D4002/D4003, separated from D3903 by their grid sides; no blue water or shoreline is visible inside D3903. Pin's `coastal: true` therefore needs an explicit classification rule or correction; proximity alone is insufficient evidence for the key's coast hex.
- **C4419 Menastir:** the printed number and road/track junction are clear, but no village/bir dot or Menastir label appears within this hex in the crop. Scenario's named landing-strip row does not prove a map settlement symbol. Pin `bir` remains unsupported by this map crop; key's single dot would not distinguish village from bir even if found.
- **B5229 Tmimi:** blue settlement dot lies inside 5229, just left of its right boundary and over the road, and the adjacent name reads Tmimi. This positively supports a settlement in the cell, although key alone does not distinguish village from bir. Shoreline sits in B5230; coastal classification of B5229 requires the pin's classification rule, not merely nearby coast.
- **D3231 coastal:** shoreline is in northern neighbors, very close to the upper vertex of D3231. Its contact with that vertex cannot be settled confidently through the thick overprinted grid and colored scan; pin `coastal: true` needs rule and better source evidence. Unlike D3903, do not treat this as a clear absence.

## Side-by-side visual challenge

Each row covers all six listed neighbors in batch B order. “Consistent” means visible marks support the named feature, not a full terrain or movement certification. Unmentioned sides in a grouped entry remain bounded by scan quality and overprint.

| Cell | Six-side reading against pin |
| --- | --- |
| C4419 | C4519: road consistent. C4520: track consistent. C4420: slope consistent. C4320: slope and road consistent, crossing crowded. C4319: no distinct crossing seen. C4418: no distinct crossing seen. |
| D3227 | D3327, D3328, D3228: no distinct crossing seen. D3128 and D3127: Qotifiya text crosses/crowds sides; no distinct terrain or route crossing established. D3226: no distinct crossing seen. Dot clearly inside 3227. |
| D3418 | D3518: track consistent. D3519, D3419, D3319: rough fill/grid, no distinct side feature established. D3318: slope and track near vertex consistent but side attribution crowded. D3417: rough fill/grid, no distinct side feature established. Dot lies within 3418. |
| B5229 | B5329: wadi consistent. B5330: wadi and road consistent, road close to vertex. B5230: road/dot crowd right boundary; no clear crossing into B5230 established. B5130: wadi consistent; see track/unfinished-road challenge above. B5129: wadi consistent. B5228: label overprint; no distinct crossing established. |
| B5917 | B6016 and B6017: no distinct route or side feature established through text/terrain. B5918: apparent solid road conflicts with unfinished-road pin, above. B5817: no distinct side feature established. B5816: single dashed track consistent. B5916: solid road consistent. |
| C4119 | C4218: single dashed track and blue cross-tied unfinished railroad cross, consistent, though close together. C4219 and C4120: no distinct crossing seen. C4019: both route marks cross close to vertex, consistent but attribution crowded. C4018 and C4118: no distinct crossing seen. |
| D3231 | D3331: road crosses diagonal; blue rail close to junction, attribution crowded. D3332: road/shoreline near upper vertex; no separate crossing established. D3232: road and railroad cross; road missing from pin, above. D3132: brown terrain near side, no separate feature established. D3131: ridge-like brown band follows side, consistent. D3230: blue railroad crosses vertical side close to upper vertex, consistent. |
| D3416 | D3516 and D3517: rough/grid; no distinct crossing established. D3417: brown fill near side, no distinct band established. D3317, D3316 and D3415: brown slope bands follow respective sides, consistent; `up` not checked. |
| D3516 | D3615 and D3616: dashed route and rail/road cluster crowd upper sides; exact assignment remains uncertain. D3517 and D3416: rough/grid; no distinct side feature established. D3415: slope and track consistent. D3515: slope consistent. Gerawla dot lies inside 3516. |
| D3903 | D4002: road crosses diagonal, consistent. D4003: coast stays north of cell; no crossing established. D3904: road crosses vertical side, consistent. D3803, D3802 and D3902: no distinct crossing seen. |

Visual observations are not route, control, legal movement, or setup conclusions. No runtime or map data changed; no gameplay tests run.
