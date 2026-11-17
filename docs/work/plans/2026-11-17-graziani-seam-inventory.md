# Graziani A/B and B/C seam inventory

**Status: reconnaissance, not an independent blind reading or admission.** This
reader consulted prior seam findings before examining the sheets. The table
records separate visible marks on each sheet; a row never asserts that its two
locations are identical, adjacent, reachable, or traversable. Partial labels,
artwork, and unexamined hexsides remain unknown. No crossing is certified.

Later blind strip readings are retained for [A/B](2026-11-17-graziani-seam-ab-independent.md)
and [B/C](2026-11-17-graziani-seam-bc-independent.md). They corroborate
several vicinity observations without establishing cell identity or movement.
The A `0433` line's last dash appears to stop short of the boundary; `A2634`
below is a vicinity locator, not a confirmed printed outer label. Independent
A/B observations also identify leads near `A3033`/`B3001` and the El Abiar
area `A4934`/`B4901`. Individual line endings and partial cells still need
source-level resolution.

## Sources and method

Local SHA-256 checks on November 17 match the previously recorded copies:

| Temporary source | SHA-256 | Inspection |
| --- | --- | --- |
| `/private/tmp/sandtable-maps.pdf` | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` | PDF pp. 1–3, Map A right edge, Map B left and right edges, Map C left edge; 100–110 dpi renders with four vertical crops per edge. |
| `/private/tmp/sandtable-land-rules.pdf` | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` | PDF and printed p. 7, §4.1, visual reading. |
| `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` | Text search for map, section, row, escarpment and Gazala; not an exhaustive visual or edition search. |

Hashes identify bytes, not provenance, rights or edition independence. Scans
are not committed; reacquisition is described in [review status](2026-11-13-graziani-review-status.md#reacquiring-source-files).
Source coordinates below use the printed four-digit labels, with the sheet
letter added. Descriptions follow the scan orientation: coast is toward the
top and desert toward the bottom; A's right margin faces B's left margin and
B's right margin faces C's left margin. These are image directions, not a
selected grid transform.

Land §4.1 says sections A–E are arranged west to east and the following
section's `01xx` hexrow overlays its predecessor's `39xx` row. The printed
edge labels inspected here have `33`/`34` as their *last* two digits on A and
B, and `01` on B and C. Interpreting this wording alongside the artwork,
stagger, and the 33-hex statement in Air/Logistics §37.42 remains unresolved.
The [distance decision](2026-11-17-graziani-map-distance-decision.md) records
inconsistent chart cells and the limits of that chart. September errata's
map item identifies a D2228/D2328 escarpment; the targeted text search did not
find an A/B or B/C correction. This does not establish that no applicable
correction exists.

## Edge labels and source coverage

- **A right, PDF p. 1:** coastal edge includes A5633, A5534, A5433 and
  A5334; toward the south the edge continues in staggered `xx33`/`xx34`
  cells to A0134. Some cells are bounded at the margin or markings obscure
  numerals. The sea and coastal end is not a uniform land boundary.
- **B left, PDF p. 2:** visible edge labels include B5701 in sea, B5501
  in coastal area, and B5301 through B0101 southward. Several cells at the
  physical left image boundary are clipped; this is not a complete enumeration
  of edge hexsides.
- **B right, PDF p. 2:** sea includes B5134 and B5033; Gazala coastal
  edge includes B4934. Southward, the grid continues through `xx33`/`xx34`
  cells to B0134. No B5034 was visible on this scan; do not substitute it for
  B4934. The prior [identity pilot](2026-11-12-graziani-map-identity-pilot.md#potential-map-seam-conflict)
  records withdrawal of an earlier alleged B5034/C5001 discrepancy.
- **C left, PDF p. 3:** C5001 is sea, C4901 coastal, and edge labels
  continue south through C0101. The left boundary clips portions of some
  cells and features. Sea, coast and incomplete cells must be handled separately.

These ranges describe visible extent, not an inventory of all printed labels
or all on-map hexsides. A full per-hex transcription, including both staggered
neighbors and off-sheet boundaries, has not been performed.

## Feature leads: A/B

| Map A right edge (p. 1) | Map B left edge (p. 2) | Observation and unresolved question |
| --- | --- | --- |
| Coastal A5534 area | Coastal B5501 area | Coastal strip and parallel linework run near respective margins; dark escarpment-like marks flank nearby land. Exact edge labels and line/hexside correspondence need higher-resolution comparison; no road or railway join inferred. |
| A4334 area | B4301 area | Dashed linework reaches edgeward through brown terrain on both sheets. Line branches and labels are crowded. Need exact endpoints and all relevant hexsides before treating marks as a single track. |
| A4134 and A3934 areas | B4101 and B3901 areas | Dashed marks and dark/green linear features occur at respective margins. The brown/clear boundaries and local geometry require separate tracing; similarity is not identity or a legal crossing. |
| Near A3233/A3334 | Near B3201 | Dashed junction at A edge and dashed junction at B edge are a plausible alignment lead noted in the [prior review](2026-11-16-graziani-seam-supply-review.md#map-seam-bounded-observations). A edge cell/label and B label are partially obscured. Determine junction geometry and whether both sheets depict the same physical mark. |
| A2633/A2634 vicinity | B2601 vicinity | Dashed line reaches edgeward through yellow patterned terrain on both sides. Verify label, exact line termination and terrain key; no connection established. |
| A1934 vicinity | B1901 vicinity | Dashed line approaches outer edge in A; B has a dashed diagonal at its clipped left boundary. Need full hex geometry and source feature classification. |
| A0433 vicinity | B0401 vicinity | A shows a dashed horizontal segment near its rightmost cell, apparently ending before the outer hex outline in this render; B shows a dashed segment entering at its left image boundary. A stop, scan artifact, or overlap requires a higher-resolution check. Do not assert continuity. |

## Feature leads: B/C

| Map B right edge (p. 2) | Map C left edge (p. 3) | Observation and unresolved question |
| --- | --- | --- |
| B4934 Gazala coast | C4901 coast | Coastal land and road-like linework appear at both margins; B5033 and C5001 are sea. Partial cells and competing feature alignment prevent a verified coastal alias or road route. |
| B4734 vicinity | C4701 vicinity | Yellow patterned terrain appears near both margins. Establish exact cell and boundary pattern; pattern alone does not identify a hex or movement cost. |
| B4534 vicinity | C4501 vicinity | Dashed linework near both margins. Check its exact intersection with each hexside, nearby branches, and whether land/terrain overlap agrees. |
| B4334 vicinity | C4301 vicinity | Dashed linework approaches both margins, with junctions and neighboring brown terrain; endpoints and labels need higher-resolution tracing. |
| B3934 vicinity | C3901 vicinity | Dashed diagonal approaches B's outer edge and a dashed diagonal enters C's clipped margin. Determine actual neighboring cells and crossing side before considering contact or movement. |
| B3134 vicinity | C3101 vicinity | Dashed bend at both margins, with labels partly obscured, is an existing alignment lead. Exact hex identity, sides and route admission remain unverified. |
| B1534 vicinity | C1501 vicinity | Green linear feature reaches B's edge and appears at C's left edge near corresponding row. Need terrain-key classification, label and side matching, and movement effects. |
| Southern B edge near 09–13 rows | Southern C edge near 09–13 rows | Multiple dashed segments reach or approach margins, with branches and partially clipped ends. Reconstruct each separately; similar tracks are not sufficient to pair them. |
| B and C 01–07 rows | Same | Broad yellow desert pattern reaches both edges. No conclusion on individual terrain cells, edge legality or reachable extent. |

## Remaining work and admission boundary

1. Independently read each sheet from the hash-matched original before seeing
   this inventory. At higher resolution, record every visible label and its
   complete/partial status, hex vertices and stagger, each feature's exact
   side and termination, terrain and coast, and what falls outside the scan.
2. Compare neighboring cells on both sheets and resolve `01xx`/`39xx` wording,
   33/34 versus 01 geometry, source chart contradictions, and applicable errata
   using an explicit source hierarchy. A matching visual feature is a lead,
   not enough to infer overlap identity, reciprocal adjacency, or a route.
3. Establish scenario-reachable coverage, including southern boundaries,
   contact/ZOC, movement, supply, road and rail effects, and distinguish
   unknown from impassable. Bind any reviewed policy to a versioned content
   identity and test reciprocal topology, terrain effects and deterministic
   replay. The separately bounded existing C/D contract is unchanged.

This record provides leads for review only. It neither establishes complete
scenario coverage nor removes the historical map readiness blocker.
