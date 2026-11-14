# Graziani setup contract: source-backed draft

**Status:** draft for review, not frozen or admitted. A fresh-context agent
checked the 11-row source pilot, but broader source double entry, map topology,
reviewed rulings, provenance/rights and roadmap prerequisites remain open. The
[review disposition](../work/plans/graziani-source-review/review-disposition.md)
and [RC work order](../work/plans/2026-11-12-graziani-rc-work-order.md) define
what that evidence does and does not establish.

This session’s [decision reconciliation](../work/plans/2026-11-13-graziani-setup-reconciliation.md)
records the Matruh, 6th NZ and initial-supply boundaries.

## Proposed identity and admission

A future scenario setup must bind a stable scenario ID, schema version,
content-pack identity/hash, source and errata revision, accepted ruleset/ruling
hashes, initial turn/stage, both sides, choices and any seeded setup outcomes.
Sources must identify every datum and reviewed correction. Setup and content must
be independently validated before the authoritative creation event; unknown,
conflicting, unlicensed or unsupported records reject with no partially created
campaign. A digest identifies bytes; it does not prove source correctness, rights
or reader independence. Existing synthetic and historical setup identities and
save/replay readers remain unchanged.

## Source-driven setup families

| Family | Required future content and decisions | Failure-sensitive negatives |
| --- | --- | --- |
| Map and locations | Stable location/edge IDs, map-section aliases, terrain, transport, weather areas, off-map boxes, objective membership and errata. | Reject dangling locations, incompatible aliases, missing seam/terrain data and invalid region choices; a geographic distance is not proof of a legal route. |
| Formations | Distinct unit, parent assignment and current attachment identities, characteristics, composition, starting state and source exceptions. | Reject duplicate unit identity, conflicting assignments, impossible parent/child ownership, cycles and attachment/stacking violations. Reconcile Cirene counter addenda and avoid counting listed attachments twice. |
| Matruh | Garrison, three scenario-listed attachments and two errata-named units, each once, subject to accepted RUL-GRA-03 and legal setup reorganization. | Do not equate assignment with attachment or assume the six-slot rule's resolution; reject impossible choice, unauthorized side and unsupported relationship. Preserve close-assault timing exception separately. |
| Motorization and supply | Initial attached/unattached pools, real/dummy supply units, ammo/fuel values, optional loads and legal placement, with initial versus later arrival provenance. | Do not add replaced detailed supplies or air-facility pools twice; preserve a printed dash as unknown pending ruling, prevent negative balances and invalid optional grouping, and prove conservation. |
| Facilities | Source-backed ownership, location and temporary/major types, capacity and repair prerequisites. | Preserve Matruh and Tobruk working choices as unaccepted until review; reject mixed ruling/version or unsupported repair resource handling. |
| Initiative/weather | Italian predetermined first-turn initiative and source/ruling-backed weather identity and area mapping. | No invented first-turn initiative draw; do not apply the synthetic no-weather-subject policy to historical fuel or map areas. |
| Arrivals | Versioned schedules and constituent records with side, source and exact turn/stage; accepted RUL-GRA-04 determines 6th NZ timing. | Reject duplicate same-slot units, wrong side/time, unknown dates and missing affected source/ruling. An empty land index is not proof of empty convoy, supply or fleet obligations. |
| Victory | Named objective identities and accepted terminal/supply/result policy. | Never import Italian Campaign §60.82 scoring into six-turn §60.81; reject missing route/hold semantics and prevent post-terminal mutation. |

Choices must be recorded with ownership and provenance. Public observations must
not reveal hidden opponent choices, dummy identities or random stream state.
The authoritative ledger must replay the chosen setup exactly and reject changed
source, ruleset, content, setup or ruling identities rather than reinterpret an
existing save.

## Candidate review/implementation order

1. Finish an independent full-field source/errata and rights review. Preserve the
   11-row Matruh/NZ/formation pilot as bounded evidence, not a full review.
2. Resolve and version the map alias/seam, Matruh relationship/facility, 6th NZ,
   Benghazi, abstract supply/repair and victory decisions. Keep unresolved fields
   unsupported rather than encoding guesses or zero values.
3. Freeze typed schema, canonical encoding/hash, reference validation, command,
   event and snapshot migrations with synthetic fixtures and negative oracles.
4. Implement authoritative creation, scheduled arrivals and following actions
   through the accepted policies, retaining old readers and deterministic replay.
5. Admit source data only after independent content cross-check and rights
   disposition; then run the applicable Core/host/privacy/Runner suites and full
   release gates.

The [scheduled-arrival index v1](scheduled-arrival-index-v1.md) implements only a
source-neutral internal lookup with synthetic tests. It does not satisfy any of
these full admission or lifecycle requirements by itself.
