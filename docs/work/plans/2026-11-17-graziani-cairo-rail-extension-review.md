# Graziani Cairo rail extension: independent same-scan review

**Date:** November 17, 2026. **Disposition:** No correction to the eight listed pinned records. The scan supports the stated local observations, with the two Cairo city sides and the northern river junction kept uncertain. This is a separate reading of the same scan, not independent-edition verification.

## Checks

Compared page 5 of the [map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) with the eight literal unordered pairs in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). The crop and PDF hashes match the [extension record](2026-11-17-graziani-cairo-rail-extension.md). Blue transverse ticks distinguish the printed rail from the paired dark road lines and broad blue river ink; overlaps still prevent precise attribution in crowded areas.

| Side | Independent scan reading | Pinned features |
| --- | --- | --- |
| `E1930–E2029` | Northbound ticked ink enters crowded Cairo artwork; exact crossing obscured. Candidate only. | `railroad` |
| `E1929–E2029` | City and river artwork obscure the exact rail-side crossing. Candidate only; relationship to the preceding side is not established. | `railroad`, `road` |
| `E2029–E2129` | Ticked line and road run north through the shared side. | `railroad`, `road` |
| `E2129–E2229` | Ticked line and road cross the diagonal shared side. | `railroad`, `road` |
| `E2229–E2329` | Ticked line follows road through the shared side; close ink limits precision. | `railroad`, `road` |
| `E2128–E2129` | Westward ticked line crosses the shared side at the Nile channel. River overprint complicates the ink. | `minor-river`, `railroad` |
| `E2127–E2128` | Westward ticked line crosses the vertical shared side inside the river bend. | `railroad` |
| `E2329–E2428` | Northern rail, road, river and grid converge near a vertex. Scan does not establish rail crossing this particular side. | `major-river`, `road`; no `railroad` |

Each listed record has `side: null` and `up: null`. Pinned feature names are data observations, not independent validation of river or road classification. The branch crossing does not prove bridge condition or service, and does not resolve the northern junction. These local observations establish neither occupancy nor a continuous usable route; no route or negative-witness conclusion follows.

## Evidence

Source PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Reviewed crop SHA-256: `cdebc04ec593d0a74b9ca6cd486d7c15b89a8f77d98792f9edd1dc516dfe67a9` (renderer-dependent). Pinned hexside data SHA-256: `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop command and provenance appear in the extension record. No runtime or map-data changes; gameplay tests not applicable.
