# Graziani facility geography: correction and unknown queue

**November 18, 2026.** Coding handoff from existing readings, not a new map reading or authorization to change data. Prioritize the source-versus-pin discrepancies below, preserving partial observations and unresolved questions. The [coverage audit](2026-11-18-graziani-facility-side-coverage-audit.md) reconciles three reviewed batches: 30 distinct facility targets, 180 directed incidences and 178 unordered pairs. Those figures are coverage of notes, not counts of clear sides, confirmed features or legal connections. Other observations remain in the linked batches; this queue is not a complete map correction inventory.

## Positive discrepancy candidates

| Priority item | Evidence and current pin | Conditional next action and boundary |
| --- | --- | --- |
| Gazala `B4933|B4934` | [Candidate and same-scan review](2026-11-18-graziani-gazala-road-candidate-review.md) find solid road crossing side interior. Pinned sides mention neither cell; pinned hexes contain `B4933` but no `B4934`. | Resolve `B4934` identity, representation and B/C seam before considering road-positive in a new map version. Sheet-edge lead does not establish an alias, continuation or playable endpoint. Other side features and crowded western junction remain unknown. |
| `B5917|B5918` | [Critical recheck](2026-11-18-graziani-landing-b-critical-recheck.md) finds paired continuous strokes matching road; pin says unfinished road. | Reconcile source provenance, version and symbol semantics before a road-type correction. Do not infer other side features or route legality. |
| `D3231|D3232` | [Prior priority handoff](2026-11-17-graziani-priority-map-corrections.md) records paired road strokes and blue rail across side; pin has railroad only. Batch B review supports road plus rail. | Consider adding road while retaining railroad in a new version, preserving unknown other features. Do not remove `D3231|D3331` railway based on this evidence. |
| `D3516|D3517` | [Prior handoff](2026-11-17-graziani-priority-map-corrections.md) and [targeted recheck](2026-11-18-graziani-d3516-rail-conflict.md) support rail across side; pin has no pair record. Recheck contradicts batch B's original omission and review's no-crossing statement. | Consider railroad-positive only with unknown other membership preserved. Road at crowded vertex remains unresolved; missing record does not mean clear. |

## Unknowns: research or policy before correction

| Priority item | What is supported | Next decision; do not infer |
| --- | --- | --- |
| `B5229|B5130` | [Recheck](2026-11-18-graziani-landing-b-critical-recheck.md) identifies track in `B5130` approaching a road, but does not establish crossing the diagonal side. | Resolve side attribution and pin conflict with better evidence or explicit vertex policy. Neither move the track to this side nor treat an unobserved crossing as disproved. |
| `C4419` settlement | Pin says `bir`; same recheck sees no distinct dot or label at Menastir, with overprint limiting negative evidence. Scenario names a strip; key uses shared village/bir dot. | Seek independent evidence or explicit transcription policy before changing settlement. Named strip is not proof of a settlement, and unseen dot is not proof of absence. |
| `D3903` coastal | Same recheck locates shoreline beyond upper cell boundaries and sees no water inside; classification depends on meaning of `coastal`. | Establish field semantics and source-to-field rule before changing pin. Geometry alone does not settle a potentially nonvisual definition. |
| Amiriya / `RUL-GRA-27` | §60.5 prints `E2212`; separate §61.33 prints `E3212`. Map mark near label has uncertain identity; bounded 1979 errata check found no direct correction. [Review](2026-11-18-graziani-amiriya-coordinate-conflict-review.md), [ruling audit](2026-11-18-graziani-rul27-audit.md). | Ruling selects literal `E2212` for **planning** with both source statements preserved, not runtime admission or verified correction. Revisit upon verified applicable correction; do not alias coordinates, silently move facility, or infer airfield, country or control from map mark. |

## Admission and validation gates

Recheck exact HEAD, source and pin records, rights, endpoint identity and applicable semantics. Reviews are informed readings of the same scan, not independent editions; cited source hashes and crop reproduction appear in linked notes. Preserve v1 bytes and replay identity; use a separately reviewed versioned map or ruleset for admitted interpretations. If encoding or consumers treat omitted features as absent, defer partial positives until coverage can distinguish unknown from clear. A missing record, unreadable feature or unreviewed side is not a negative.

Review map, route, setup, persistence and legal-action consumers as applicable, including authority and fog-of-war boundaries. Focus deterministic tests on both side orientations, preserved positives, unknown versus clear, corrected type, old/new identity and replay; test affected consumers and run relevant Core and boundary suites. Facility name or geographic candidate does not establish control, capacity, occupancy, legal route or transport. [Facility geography review](2026-11-18-graziani-facility-geography-mode-scope.md) separates §32.37 settlement eligibility from facility status and mode-specific setup.

This synthesis made no runtime or map-data changes and ran no gameplay tests.
