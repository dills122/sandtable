# Graziani Saharan setup radius: geometric policy proposal

**Status: proposed for planning, 17 November 2026; not an admitted ruling or runtime behavior.** Select `gra-saharan-radius-inclusive-v1-proposed`: a candidate cell satisfies the radius predicate exactly when its shortest geometric hex-step distance from C0716 is **0 through 3 inclusive**. This includes both C0716 and the distance-three ring. It does not select a placement or establish that any candidate is a legal deployment hex. The [independent planning review](2026-11-17-graziani-saharan-radius-policy-review.md) recalculated the set from the stated adjacency, without independently re-rendering the source scans or establishing a new edition.

## Evidence and choice

[Scenario §60.31](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27, lists Saharan Det (I) within three hexes of C0716 and five light trucks on that row. The [source note](2026-11-17-graziani-c0716-radius-source.md) and [independent same-scan review](2026-11-17-graziani-c0716-radius-source-review.md) support the row, local geometry and 37-cell calculation. Neither source explicitly defines inclusion of the reference hex or the outer ring. The map's §37.42 counting instruction concerns a distance chart, not an explicit setup rule; it is supporting context, not authority that settles this wording. The review is a separate reading of the same scans, not a new edition. The checked September 1979 errata entry concerns other units; later or indirect corrections remain possible.

Select the inclusive interpretation as a bounded planning convention: distance zero is within the stated area, and a maximum of three includes the named boundary. Preserve alternatives for review: exclude the origin but include distance three (36 cells); include the origin but exclude distance three (19 cells); or exclude both (18 cells). A different interpretation changes the candidate set and requires a new policy version and renewed review. None is presented as an official correction.

## Exact geometric set

Shortest distance means minimum number of adjacent hex transitions on the local Map C grid, independent of terrain, routes, movement cost, ownership and political boundaries. From C0716, neighbors are C0615, C0616, C0715, C0717, C0815 and C0816. Odd numbered columns connect to adjacent columns at row offsets −1 and 0; even columns at 0 and +1. No sheet seam is involved. The origin and rings one, two and three contain 1, 6, 12 and 18 cells respectively. Inclusive ranges below give exactly **37** cells:

| Column | Rows |
| --- | --- |
| C04 | 14–17 |
| C05 | 14–18 |
| C06 | 13–18 |
| C07 | 13–19 |
| C08 | 13–18 |
| C09 | 14–18 |
| C10 | 14–17 |

C0716 is distance 0, C0713 and C1014 are distance 3, and C0712 and C1018 are distance 4. A valid geometric result for C1014 does not prove co-location with the separately listed Giaribub garrison. The separate C0716 supply dump does not fix the detachment's position.

## Separate gates

- **Region:** No Libya-only filter is selected by this radius policy. The Saharan row follows a separate “Anywhere in Libya” row; its own wording does not repeat that restriction. [Boundary research](2026-11-17-graziani-saharan-radius-border.md) and its [independent same-scan review](2026-11-17-graziani-saharan-radius-border-review.md) conditionally place C0719 on the Egypt side and the other 36 on the Libya side. That geographic reading is not a certified boundary or proof that C0719 is legal; other applicable rules or corrections may constrain placement.
- **Terrain and map:** Every candidate appears on the sheet, but the [interior review](2026-11-17-graziani-c0716-radius-interiors-review.md) records obscured and mixed artwork. Classify terrain and relevant features under applicable rules; missing evidence remains unknown. Geometric inclusion does not establish entry, traversability or setup eligibility.
- **Organization and occupancy:** Resolve formation membership, whether constituents may be distributed across the radius, attachment, stacking, occupancy, and scenario exceptions independently. The [setup rule analysis](2026-11-17-graziani-saharan-setup-rules.md) and [source review](2026-11-17-graziani-saharan-source-review.md) do not establish forced co-location or free dispersal. Optional scenario augmentation also remains a player choice subject to its own rules.
- **Trucks and profile:** Five light trucks are listed on the formation row. Determine assignment, loads and any distribution under applicable setup rules; do not duplicate or select them by radius alone. Their abstract motorization contribution is zero under the cited medium/heavy conversion, without excluding other sources. See [composition and trucks](2026-11-17-graziani-saharan-detachment-source.md).
- **Admission and provenance:** Before implementing or enabling setup, record the versioned scenario and map inputs, governing rules and correction checks, the policy approver, accepted endpoint convention, authority and provenance, and results for every applicable gate. Review acceptance does not itself promote this proposal to an official ruling or runtime configuration. Record each result as pass, fail or unknown; unknown blocks a positive legal-placement result. Check a player's selection against every applicable gate and current authoritative state, and revalidate when inputs or state change. This proposal grants no runtime admission.
- **Authority and privacy:** Umpire validates authoritative constraints. Any future candidate or placement view must use only the requesting side's authorized information. Do not send hidden opposing placements, occupancy or other hidden state to the intelligence plane, including through candidate lists, rejection reasons, logs or advisory requests. Return only information allowed by fog-of-war policy; geometric membership does not establish public legality.

## Verification for a later implementation

Use synthetic local-grid tests for ring counts and the exact 37-cell set, origin and distance-three inclusion, distance-four exclusion, and odd/even column adjacency. Check the alternatives distinguish origin and boundary separately. Test the radius predicate independently of terrain and region, then verify unknown or failed setup gates cannot produce a legal placement; test co-location, distribution and truck allocation only after their own rules are decided. Check stale selections, changed state and inputs, side-filtered results and error paths; add authority and privacy boundary tests for any integration. These are proposed tests, not executed results. No runtime changes or gameplay tests accompany this document.
