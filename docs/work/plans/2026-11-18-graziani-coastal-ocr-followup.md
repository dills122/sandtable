# Coastal hex: Land rules OCR follow-up

**Result:** A full 47-page Vision OCR pass and targeted text search found uses of
“coastal hex” but no express geometric definition in the matches. This broadens
the [bounded source check](2026-11-18-graziani-coastal-hex-source.md); it does
not resolve shoreline-in-hex, side or vertex contact, or neighboring water.
OCR errors and search terms can miss relevant wording, so this is not proof
that the book contains no definition.

| Land rules locator | Observation | Limit |
| --- | --- | --- |
| PDF/printed p. 41 §30.14 | Naval units stack in coastal or all-sea hexes. | Operational use, not a definition of the coastal boundary. |
| p. 41 §30.21 | Ships can bombard the coastal hex they occupy; battleships and heavy cruisers can also bombard an adjacent hex. | Differentiates an occupied coastal hex from an adjacent target, but does not define coastal geometry. The [review](2026-11-18-graziani-coastal-ocr-followup-review.md) confirms no inland-only qualification in this case. |
| pp. 39–40 §§27.72, 27.77, 27.81 | Layforce placement, landing/retreat and SAS debarkation refer to coastal hexes. | OCR search leads, not independently visually checked here; none of the matched wording defines the boundary. |
| pp. 12, 37, 41 | Fleet assignment, flying-boat construction and sandstorm rules use coastal hexes. | Previously identified uses; no adjacency rule follows. |

Visually checked p. 41 against the scan for §§30.14 and 30.21. Search covered
`coast`, common OCR variants, `shore`, `shoreline`, `sea hex` and `sea-side`
in per-page OCR text; results also included coastal shipping and artillery.
Vision recognized all 47 pages after running outside the sandbox; sandboxed
requests returned `nilError`. OCR is imperfect and layout may affect recognition
and ordering. Source PDF SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`
matched the recorded Land rules hash before transcription. Scan, rendered page,
script and bulk OCR remain temporary under `/private/tmp`; no runtime or map data
changes and no gameplay tests.
