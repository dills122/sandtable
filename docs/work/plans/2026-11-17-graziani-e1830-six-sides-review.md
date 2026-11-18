# E1830 six-side source review

**Disposition:** Support the [six-side note](2026-11-17-graziani-e1830-six-sides.md) as a bounded visual comparison. The E1829–E1830 missing-road discrepancy is a credible candidate for source-data review; it is not a movement ruling or a runtime correction.

I checked Map E's Cairo area, the printed terrain key and location summary in `/private/tmp/sandtable-maps.pdf` (SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`). A fresh rendering with the note's page 5 detail command produced SHA-256 `7f2cf5ef1d2790ab81673b39979c15494a770cb85d2454d92fe37a884fc33467`, matching its stated crop. The obscured center is located by the neighboring grid and labels. Both dark parallel lines are visible across the west boundary through the river and figures; their continuation matches the printed road key. A corresponding road crosses the east side. City and river artwork obscure parts of other sides, so the note appropriately avoids treating missing entries as proof of absent features.

Direct inspection of pinned JSON finds E1830 `major-city` terrain and settlement, `coastal: false`; its only pair records are E1829 (`minor-river`), E1831 (`road`) and E1930 (`minor-river`). The six named neighbors and orientations match the displayed grid. The printed key identifies the cited symbols, including soldier figures as training area, and the Cairo summary gives E1730/1829/1830/1930/1931 with X for major city and victory condition, dashes for port and assigned garrison.

This review used the same scan and supplied crop locator, not an independent edition or physical map. It does not authenticate the scan, establish rights, certify all obscured features, or establish unit occupancy or legal routes. No runtime data changed.
