# Independent source review: D3516 / D3517 railway

**Disposition:** The visible railway crossing and missing pinned record are corroborated. This is one candidate map-data omission, not proof of a complete or usable rail route. Road membership near the northern vertex remains separate and unresolved.

The wider Map D crop places the printed `3516` in the Gerawla cell and `3517` in its eastern neighbor. In the 800-dpi source crop their shared near-vertical grid side runs approximately from `(580,450)` to `(580,740)`. The blue line with repeated cross-ties crosses that side near `(580,570)`, away from both endpoints. The map key identifies that symbol as a railroad. This is a crossing of the side's interior, not merely a symbol touching a three-hex vertex.

At the northern vertex, the road, coastline and grid intersect or overlap; this check does not certify whether the road crosses the same side, whether another feature is present, or whether the side is passable. The railroad mark itself does not establish construction status, the contiguous line beyond this local side, legal rail use, unit positions or reachability. Any change requires a separately reviewed versioned map-data update and replay assessment.

The pinned `hexsides.v1.json` contains neither `D3516|D3517` nor its reversed key in the inspected file. Absence means **unrepresented**, not a verified clear side; candidate's proposed source comparison is accurate. Independent inspection followed the prior note and used the same source scan and existing crop, so it is a second visual reading, not blind transcription or a second edition.

**Provenance:** [Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D PDF p. 4 and key PDF p. 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; inspected 800-dpi crop `/private/tmp/graziani-mapcheck/d3615east800.png`, SHA-256 `7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`, and 500-dpi contextual crop. Pinned map-data file SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No map edits or gameplay tests.
