# Coastal OCR follow-up: review

**Disposition: remove the added inland restriction.** The [follow-up](2026-11-18-graziani-coastal-ocr-followup.md)'s §30.21 line was changed after my initial low-resolution review incorrectly requested an inland qualifier. At 300 dpi, the original p. 41 says battleships and heavy cruisers (4-pointers) may bombard **any hex adjacent** to their occupied coastal hex; it does not say the target must be inland. My earlier request to add that word was erroneous. The original note's generic adjacent-target scope was accurate; identifying eligible ship types is a useful qualification.

Section 30.14 says naval units stack freely in coastal or all-sea hexes; §30.21 lets Commonwealth ships bombard the coastal hex occupied and grants those two ship types the additional adjacent-hex reach. Neither passage defines what makes a hex coastal. The map p. 1 key's Coast example shows water and land within the outlined hex, but its illustration alone does not settle adjacency or vertex-only geometry.

The 47 per-page `/private/tmp/sandtable-land-ocr-*.txt` files exist; a repeat literal search for coast, shore, sea hex and sea-side returns relevant uses, including pp. 12, 37 and 39–41. OCR p. 41 also correctly says any adjacent hex. OCR can miss other words, glyphs or arrangements; the candidate's bounded absence of an **express definition in matched text** does not establish a full-book or all-edition negative. Its pp. 39–40 references are expressly identified as OCR leads, not newly visually verified holdings.

## Provenance

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) p. 41 at 300 dpi and the retained map key crop; verified Land SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and key crop SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`. Same sources; no map/data/runtime edits or gameplay tests.

## Recheck

The updated §30.21 row removes the inland-only qualification and specifies battleships and heavy cruisers for the adjacent target, consistent with the printed case. The review issue is corrected; the geometric definition remains unresolved by these passages.
