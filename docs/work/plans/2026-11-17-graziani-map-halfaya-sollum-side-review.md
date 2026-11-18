# Halfaya–Sollum shared side: independent visual review

**Disposition:** corroborate the [candidate observation](2026-11-17-graziani-map-halfaya-sollum-side.md): the C3922–C4021 diagonal appears as a narrow grid line rather than a continuous escarpment band, while broad bands follow the other two sides at its western vertex. The pinned escarpment remains a candidate false positive, not an admitted correction. Overprint and the vertex prevent certifying every pixel or all other features on the side.

## Source reading

Rendered [original map](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 3 (Map C) at 600 dpi, with a wider local view. C3921 west of Halfaya is legible; 3923 east and nearby 3821/3823, 4022/4023 and 4122 support the sequence locating the intervening Halfaya hex as C3922 and the Sollum-dot hex north as C4021. The two target numerals are obscured by map ink, so this is sequence-based identification, not a separate clear reading of both labels.

At the three-hex vertex east of C3921, a broad dark escarpment band arrives from the northwest along C3921–C4021 and another descends south along C3921–C3922. The third ray runs northeast toward the coastal vertex, separating the inferred C3922 and C4021; it appears as a comparatively thin grid line, without a comparable broad continuous band. Dark irregular marks gather around its western endpoint and paired dashed road marks cross near the coast, so the exact endpoint and overprinted stretch remain uncertain. Those dashed marks are consistent with an unfinished-road lead, but this view does not prove its full continuity or rule out every other feature. The inspected `C3922|C4021` pinned record contains `escarpment`, `unfinished-road` and `up: C4021`.

Land rules PDF/printed p. 18 §10.21(b) exclude ZOC across escarpment sides. If a separately reviewed data correction removes the escarpment, that particular block would not apply on this side; it would not establish positive ZOC, either movement direction, traversability or wider map coverage. Preserve unknown terrain, source-unit eligibility, occupancy and control separately. A future correction needs versioned provenance, reciprocal lookup and both-direction ZOC tests, plus replay assessment; none is performed here.

## Reproduction and limits

```sh
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/sandtable-land-rules.pdf src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
pdftoppm -f 3 -l 3 -r 600 -x 7200 -y 7100 -W 2400 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-halfaya-side-review
```

SHA-256: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same scan and candidate were known before review: informed independent visual check, not blind transcription or an independent edition. Hash matches do not establish rights. No runtime data, readiness flags or gameplay tests changed.
