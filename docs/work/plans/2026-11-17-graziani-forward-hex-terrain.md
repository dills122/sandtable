# Three forward Commonwealth hexes: printed terrain

**Finding:** Original Map C shows a blue settlement dot and pale clear-looking land in each of `C3721`, `C3926`, and `C4131`; the latter two also contain coastline. The observed dots and coastal status agree with the pinned hex records. These localized readings do not certify every side, route continuity or movement/ZOC legality.

## Visual observations

Compared [Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. Key depicts clear as pale unpatterned land, village/bir as a blue dot, coast as land meeting blue sea, track as a dark single dashed line, unfinished road as paired dashed lines, and road as paired solid lines.

| Hex | Visible map content | Pinned `hexes.v1.json` |
| --- | --- | --- |
| `C3721` | `Sidi Suleiman` blue dot inside pale land; a dark dashed track traverses the hex. Name ink overlaps part of the southern boundary; blue cross-tied unfinished railway is visible in the neighboring area and must not be assumed to traverse this hex from its proximity. | `clear`, `village`, `coastal: false` |
| `C3926` | `Buq Buq` blue dot on the land side close to the eastern hex boundary; pale land meets sea inside the hex. Paired dashed road marks and a single dashed track occur locally; label/ink overlap limits precise junction and side claims. | `clear`, `village`, `coastal: true` |
| `C4131` | `Sidi Barrani` blue dot within pale land near its southern boundary; coastline crosses the northern part. Paired road marks and a single dashed track appear locally, with the name obscuring part of the road and junction. | `clear`, `village`, `coastal: true` |

The pale land resembles the key's clear swatch; coastline remains a separate visible feature rather than proof that the entire coastal hex is land or sea. `Village/Bir` is the key's symbol category, not independent proof of population or a special facility. Text and thick marks obscure small portions; do not treat a clear-looking interior as certification of all six sides, unobserved terrain, a completed road, or a traversable route. Source listing of the respective starting units is in [fixed-contact inventory](2026-11-17-graziani-fixed-contact-inventory.md); this note does not establish their completed stacks.

## Reproduction and provenance

Original map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hex JSON SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Render Map C at 400 dpi from `/private/tmp/sandtable-maps.pdf`:

```sh
pdftoppm -f 3 -l 3 -r 400 -x 4850 -y 5480 -W 700 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/forward-3721
pdftoppm -f 3 -l 3 -r 400 -x 6100 -y 4950 -W 800 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/forward-3926
pdftoppm -f 3 -l 3 -r 400 -x 7450 -y 4500 -W 800 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/forward-4131
```

Crop SHA-256 respectively `f3e547ec01da7bac9be5df57bd63e24f48a0d2b2b101e717014d6b7ce438e53f`, `6b789e56e5036812f8d38a6bae124311fd2c291efe1bfb6b36d895e999df6d96`, `d3e5ef898a9dfaa11438e273434f482096766512b27b9e42172187f43dcd7fc5`; renderer versions may alter PNG bytes. Scans and crops remain outside Git. Same edition, not independent source or rights grant; no runtime changes or gameplay tests.
