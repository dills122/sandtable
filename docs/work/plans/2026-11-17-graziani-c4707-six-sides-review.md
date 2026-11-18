# C4707 six-side source review

**Verdict: Ready for scoped source observation.** No actionable findings in reviewed reading. This is not independent source corroboration: review used same original map PDF and pinned data.

## Evidence checked

- `/private/tmp/sandtable-maps.pdf` SHA-256 matches document. Existing Tobruk, key and detail crops match all three stated hashes; inspected detail and terrain key visually.
- Six neighbor orientations and broad terrain bands match visible crop: escarpment on C4706 side, slopes on C4606, C4607 and C4807 sides. Exposed portions of C4806 and C4708 sides show no broad band. Overprints and crowded vertices prevent reliable route endpoint assignment.
- Checked pinned hexside entries and `up` values against `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`; listed records and absent entries agree. Both historical-map JSON hashes match document.
- Road and railway crossings at C4807 and C4607 remain appropriately qualified; `up` values explicitly attributed to pinned data rather than visual verification. No conclusion follows about movement, ZOC, or hidden marks.

## Residual limits

Original scan shares source with prior research, and obscured endpoints and terrain direction remain unverified. Crop hashes verify inspected files, not a separate map edition. No runtime tests run: documentation-only source review.
