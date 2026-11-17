# Graziani map: independent check of remaining Map D correction

**Scope:** targeted source recheck by a different reader, after consulting the
existing map queue and pilot. This is independent corroboration of one source
observation, not a blind transcription, a separate edition, or map admission.
A/B and B/C strips and distance-chart triangles were outside this check.

## Sources and observations

On November 17, local SHA-256 checks matched the prior recorded source copies:

| Source | SHA-256 | Inspected material |
| --- | --- | --- |
| `/private/tmp/sandtable-maps.pdf` | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` | PDF p. 4, Map D, around printed hexes 2228 and 2328; rendered at 80 dpi and enlarged for inspection. |
| `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` | September errata p. 1, **MAPS**, Map D correction. |

The errata explicitly says Map D hexside **2228/2328** should have an
escarpment. Map D identifies both hexes, which share a sloping side; that side
lacks the heavy dark escarpment marking present on nearby sides in the scan.
The erratum therefore supplies a feature absent from the inspected original map
printing. This confirms the prior [map identity pilot](2026-11-12-graziani-map-identity-pilot.md#map-d-correction)
for this bounded observation. Hashes identify file bytes, not rights, provenance,
or independent editions.

The pinned third-party transcription already lists the pair once as
`D2228|D2328` with feature `escarpment` in
[`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json).
That is agreement with the corrected source, not evidence that this edge is
admitted into authoritative gameplay. Its `up` field names `D2328`; this check
did not independently validate that field's semantics. No content change is
proposed solely to repeat an existing feature.

## Consequences and limits

- When this edge is admitted, retain a single reciprocal physical side and
  preserve the erratum as provenance. Test both directions against the applicable
  rule-specific movement, ZOC, sight and supply contracts; do not infer all
  effects from the feature name. Land §10.21's ZOC restriction is separately
  summarized in [source control map](../../specs/graziani-source-control-map.md#land-zones-of-control).
- Neither source observation establishes that either hex is reachable during
  this scenario, that neighboring edges are complete, or that a complete
  playable map exists. The current read does not establish whether the errata
  package is exhaustive or check other printings. Preserve unknown coverage.
- No source content, content hash, runtime flag or test was changed. The scan
  remains outside Git pending provenance and rights review. Remaining map/setup
  discrepancies and boundary semantics remain open as catalogued in the
  [research queue](2026-11-17-graziani-remaining-source-research.md#source-facts-and-decisions-still-needed).
