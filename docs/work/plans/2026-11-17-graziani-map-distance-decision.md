# Graziani map distances and seam identity: provisional decision

**Decision:** retain printed chart values as attributed source observations, not
as a mutually consistent distance oracle or evidence of traversable edges. Keep
unverified cross-sheet edges and overlapping identities **unknown and unavailable**
for source gameplay; do not convert unknown to impassable terrain or infer a
route, alias, or new crossing from a distance match. Preserve the three narrowly
admitted C/D crossings under their existing versioned contract, without extending
their scope. This decision does not certify the map or remove the source-gameplay
readiness blocker. See [map identity pilot](2026-11-12-graziani-map-identity-pilot.md#independent-cd-sheet-edge-check).

## Source and scope

On November 17, this reader checked the original scans after consulting the
existing comparison, so this is **not a blind or independent full-chart
transcription**. Local SHA-256 checks matched the previously recorded baselines:

| Temporary source | SHA-256 | Relevant source |
| --- | --- | --- |
| `/private/tmp/sandtable-maps.pdf` | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` | Map C, PDF p. 3; map sheets and chart copy |
| `/private/tmp/sandtable-air-rules.pdf` | `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4` | PDF p. 25, §37.42 chart and footnote |
| `/private/tmp/sandtable-land-rules.pdf` | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` | PDF/printed p. 7, §4.1 |
| `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` | September errata, text search only |

Hashes identify bytes, not edition independence or rights. PDF scans are not
committed. Reacquisition is described in the [review status guide](2026-11-13-graziani-review-status.md#reacquiring-source-files).
The errata text search for `37.42`, `distance`, `map section`, `4.1`, `hex row`,
`hexrow`, `39xx`, and `01xx` found no relevant correction; this is not an
exhaustive visual errata or edition search.

## Source contradiction

The chart defines shortest hex distance, excluding origin and including
destination, and explicitly permits an assumed sea grid. Its displayed values
include these two independently checkable contradictions:

| Three chart locations | Printed direct distance | Printed two-leg sum |
| --- | ---: | ---: |
| Barce B5504, Tobruk C4807, Bir-el-Qatrani D2903 | Barce–Bir 80 | Barce–Tobruk 40 + Tobruk–Bir 38 = 78 |
| Agedabia A2629, C2803, Bardia C4321 | Agedabia–Bardia 67 | Agedabia–C2803 41 + C2803–Bardia 25 = 66 |

Both direct values exceed their two-leg sums. No single shortest-path metric
can satisfy either triple, regardless of seam offset, terrain or endpoint-count
convention. This establishes at least one inconsistent cell in each triple; it
does not identify which cell is wrong. The earlier [independent reader](2026-11-17-graziani-map-independent-review.md#distance-chart-cells)
also transcribed the first triple. The second triple was checked here after
seeing earlier research and is not an independent blind reading.

Existing [190-cell comparison](2026-11-12-graziani-map-identity-pilot.md#full-distance-chart-comparison)
reports 179 matches under one 33-column odd-row model and 11 mismatches. Its
[calculation cross-check](2026-11-16-graziani-seam-supply-review.md#rechecked-printed-distance-cells)
tried offsets 31–35 and both row-stagger conventions, but reused the same
transcription. These results favor investigation of that candidate, not a
unique correction, exhaustive model search, full independent transcription,
or proof of legal paths. In particular, matching 179 values cannot select the
faulty member of a contradictory triple.

Land §4.1 says each following section's `01xx` hexrow overlays the preceding
section's `39xx` row. The chart footnote says identical numbers on adjacent
sections are 33 hexes apart. The scanned grid and partial edge cells need a
field-by-field alignment before translating either statement into aliases or
edges. The apparent conflict remains open; neither a typo in §4.1 nor a
particular transform is established. Similar track/coast features are leads,
not identity proof. C/D's existing three crossings are bounded separately by
source-turn contract 3; its unfinished railway is not a usable rail route.

## Source policy and admission tests

1. Preserve exact printed values and locators separately from calculated
   geometry and any adjudicated correction. Applicable verified errata and
   explicit, versioned rulings may correct source facts; no silent overwrite or
   majority vote. A chart cell cannot itself create a neighbor or legal route.
2. Admit a cross-sheet identity or edge only from a reviewed source record of
   both sheet labels, full hex geometry/stagger, terrain and hexside features,
   and corrections, with ambiguity explicitly resolved. Record provenance and
   content identity. Absence of proof stays unknown, not a historical barrier.
3. Reject or report unavailable any movement, supply, control, ZOC, or victory
   proof needing an unknown crossing; don't infer no real-world route exists.
   Preserve existing C/D contract's exact three crossings, and don't infer
   additional aliases or grouped ZOC from them.
4. Test both directions of each admitted neighbor; distinguish overlap identity
   from adjacency and legal movement; test terrain and feature continuity,
   domain-specific route effects, unknown-edge refusal, wrong or stale content
   identity, and deterministic replay. Include a negative test that chart
   agreement alone never admits a seam. Treat the two printed triangles above
   as source-consistency regression cases, not expected runtime distances.
5. Before wider admission, compare independently entered row/column labels and
   values and independently recompute candidate
   geometry, investigate all discrepancies and triangle violations, and document
   source hierarchy and unresolved corrections. Perform independent seam reads
   and review applicable corrections and rights. Reassess readiness only after
   all required reachable topology and rule dependencies are verified.

**Independent full-chart follow-up:** the [fresh-context transcription](2026-11-17-graziani-distance-transcription.md)
now matches all 190 prior numeric values and reproduces 24 triangle violations.
The two readings use the same printed edition; coordinate/label matching,
candidate geometry and actual seam admission remain separate checks.

## Reproduce bounded checks

```sh
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/sandtable-air-rules.pdf /private/tmp/sandtable-land-rules.pdf /private/tmp/sandtable-cna-errata.pdf
pdftoppm -f 25 -l 25 -r 160 -png -singlefile /private/tmp/sandtable-air-rules.pdf /private/tmp/graziani-chart-distance
pdftoppm -f 7 -l 7 -r 160 -png -singlefile /private/tmp/sandtable-land-rules.pdf /private/tmp/graziani-land-seven
python3 - <<'PY'
for direct, first, second in ((80, 40, 38), (67, 41, 25)):
    assert direct > first + second
    print(f"{direct} > {first} + {second} = {first + second}")
PY
```

These checks validate the two transcribed inequalities only; visual inspection
of row and column intersections is necessary to validate the six source values.
