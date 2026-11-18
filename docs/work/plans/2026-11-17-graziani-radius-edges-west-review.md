# Western radius edge audit: source review

Reviewed 17 November 2026 against [western audit](2026-11-17-graziani-radius-edges-west.md), Map C and key in the same [map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), pages 3 and 1, and the pinned hexside JSON. **Disposition: no correction found in the assigned observations.** This checks the scan reading, not a separate edition or gameplay ruling.

- **Coverage and geometry:** 15 distinct assigned cells yield 90 listed side incidences, representing 60 distinct pairs because 30 internal pairs appear twice. All six neighbors in each row agree with the displayed staggered grid and reciprocal adjacency, including sides outside the assigned cells. Count is not 90 independent edges.
- **Visible marks:** The checked line follows C0618/C0719 and C0618/C0619. Yellow desert artwork covers the C04/C05 area and extends unevenly into C06; its jagged boundary is not itself an identified hexside feature. I found no obvious route crossing or broad side band on the other assigned sides in the crop. This does not settle fine, obscured, or coincident marks, or terrain and movement effects.
- **Line identity:** Key's `Border` example does not show the blue checks. The linked regional reading treats the line as the Libya–Egypt boundary on geographic evidence, and appropriately makes that identification conditional. This review does not establish an independent movement restriction.
- **Pinned data:** JSON declares `vassal:CNAv2.1.0`; no entry has any assigned cell as either endpoint. Missing sparse entries do not prove clear sides or corroborate the scan's negative observations.

Checked the audit's PDF, map crop, key crop and both JSON SHA-256 values against local files; all match. Visually inspected the map and key crops, checked all neighbor rows against staggered-grid arithmetic, and searched parsed JSON endpoints for all 15 cells. Same scan and crop limit independence; no runtime change or gameplay test.
