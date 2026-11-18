# Tobruk west branch: independent source review

**Reviewed 2026-11-17.** The [branch reading](2026-11-17-graziani-tobruk-west-branch.md) is appropriately cautious. Map C supports an unfinished-railroad lead toward the C4707–C4807 side and an unfinished-road branch farther south. It does not cleanly resolve every route stroke at the Tobruk junction, so it does not independently prove the pinned unfinished-road crossing on that side.

## Visual evidence

I inspected page 3 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), using the unrotated 500-dpi crop identified in the branch reading, and the page 1 terrain key. In the crop, C4807 is the Tobruk city hex; C4707 lies below-left and C4708 below-right. The C4807–C4707 diagonal meets both C4708 sides at the crowded vertex below the city.

- The blue cross-tied line runs through C4707 toward that diagonal. Its broken blue form matches **Unfinished Railroad** in the key; the southern continuation crosses the C4707–C4607 slope and bends toward C4608. Its upper endpoint merges with slope, route and city ink, limiting confirmation of continuity inside C4807.
- Paired broken dark strokes below the junction match **Unfinished Road**, distinct from the key's single heavy broken **Track**. Those strokes approach the junction through the C4707 area, but overlapping solid road strokes, grid, slope and city artwork prevent a confident independent assignment of their crossing to C4707–C4807.
- Paired solid road strokes approach Tobruk from C4806 and curve toward C4708, close to the shared vertex. The C4708–C4807 side is consequently relevant when interpreting the junction; the crowding prevents treating all dark strokes there as one crossing. No isolated track crossing on C4707–C4807 is established.

## Pinned comparison and limits

The [hexside file](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) identifies its source as `vassal:CNAv2.1.0`. It records C4707–C4807 as `slope`, `unfinished-railroad`, and `unfinished-road`, with `up: C4707`; C4708–C4807 as `road` and `slope`, with `up: C4708`. Both records have `side: null`. These are pinned fields, not independent proof of a printed crossing or a fresh determination of slope direction. No explicit C4707–C4708 record exists; that absence alone does not establish a feature-free side.

The scan and pinned data are related source evidence, not independent edition verification. The rail lead is stronger evidence than the obscured road assignment; neither observation establishes route legality, movement, or supply. No data correction follows from this image alone. A clearer original or independent edition would be needed to settle the obscured crossing.

## Reproducibility

The branch reading supplies crop commands. Checked SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Map C crop `59e114a35f0219924f1e6f187c86bda0e4b77af81c5f770a9eaad5dec965bf9c`; key crop `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`; hexside file `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Renderer versions may change crop hashes. No runtime edits or gameplay tests.
