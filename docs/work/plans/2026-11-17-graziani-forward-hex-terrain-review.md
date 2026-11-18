# Forward Commonwealth hex terrain: source review

**Disposition:** The [forward terrain note](2026-11-17-graziani-forward-hex-terrain.md) correctly locates the three blue dots and reads clear-looking exposed land, with coast in C3926 and C4131. These observations agree with the three pinned hex records; they do not certify six sides, route continuity or legal movement.

## Original scan checks

Visually checked [Map C and its terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, using the crop coordinates in the note. Key distinguishes unpatterned Clear, blue-dot Village/Bir, land/sea Coast, single-dashed Track, paired-dashed unfinished road and paired-solid road.

- `C3721`: Sidi Suleiman dot falls inside the numbered hex on pale unpatterned land, with a dark dashed track. The nearby blue cross-tied railway lies east in the adjoining area; visual proximity does not establish a railway through C3721. Name ink overlaps the boundary.
- `C3926`: Buq Buq dot lies on the land side close to the eastern boundary but inside the numbered hex; shoreline crosses its northern part. Paired dashes and a single dashed track are visible in the local junction. Dot/line and text overlap warrant preserving route and side uncertainty.
- `C4131`: Sidi Barrani dot sits in the numbered hex close to its southern edge; sea and land both occupy the hex. Paired road marks, a dashed track and name text occupy the local area. Label overlap limits precise connection and side claims.

Checked `src/Cna.Core/Content/HistoricalMap/hexes.v1.json`: the three records respectively say `clear/village/false`, `clear/village/true` and `clear/village/true` for terrain, settlement and coastal status. That comparison confirms agreement at those fields only; it does not independently prove absence of obscured features or any edge data. Map scan SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; cited note records pinned hex-file hash. Same-scan informed review, not a new edition, exhaustive terrain audit, runtime change or gameplay test.
