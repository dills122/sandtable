# Content Pack v1 Spike

**Status:** Approved and implemented

**Date:** 2026-08-16

**Decision owner:** Project owner

**Rules target:** `cna-1979.1`

## Recommendation

Build Content Pack v1 as a plain `Cna.Core.Content` domain module with four responsibilities:

1. immutable, versioned definitions for board topology, formations, combat elements, and one
   scenario deployment;
2. strict local and graph validation with stable, path-addressed issue codes;
3. a repository-owned canonical JSON byte contract and independent SHA-256 content identity; and
4. one original, visibly nonhistorical nine-hex rules-laboratory fixture.

Content assigns stable rules-owned vocabulary IDs to source-cited or explicitly synthetic facts.
It does not contain movement costs, stacking calculations, runtime positions, visibility, copied
rules prose, or published artwork. Printed map coordinates are audit metadata; explicit edges are
the authority for adjacency.

Future campaign admission will resolve an immutable pack by ID and hash before entering an
authoritative turn, validate it against the selected ruleset, and record both identities. Replay
will require the same hash-addressed pack from a historical resolver. The Umpire will never fetch
content over a network or silently substitute another version. Pre-alpha replay also requires the
historical executable/rules implementation whose manifest matches the recorded ruleset hash; a
generic historical rules interpreter is not part of this package.

Before implementing movement, add a separate turn-preamble capability for Naval Convoy,
Initiative Declaration, and Weather Determination. The synchronized roadmap now puts that
capability before movement so the authoritative sequence cannot skip unsupported decisions.

## Question and scope

This spike asks:

> What is the smallest versioned, source-explainable content contract and original rules-laboratory
> fixture that can support side-safe observations and the first authentic movement/contact loop
> without embedding published assets, coupling content identity to ruleset identity, or forcing an
> early campaign-schema rewrite?

In scope:

- authoritative static board, formation, element, and scenario-deployment facts;
- topology, identity, provenance, validation, canonical bytes, and content hash;
- rules/content/world ownership boundaries;
- a rights-safe synthetic fixture shaped for later movement/contact tests;
- the future campaign admission and replay seam as a design constraint; and
- delivery order through turn preamble, movement, and combat.

Out of scope:

- production implementation in this change;
- published scenario transcription or source scans in Git;
- mutable campaign world state, observation DTOs, legal actions, movement, contact, or combat;
- Naval Convoy, Initiative Declaration, or Weather Determination mechanics;
- persistence, Orleans activation, Maproom, transport contracts, or Intelligence integration; and
- a general board-game content engine.

## Method and stop condition

The research:

- inspected the merged ruleset, setup, campaign, canonical serialization, replay, and roadmap
  boundaries;
- rendered and visually inspected the relevant image-only Land Rules, Scenario Rules, common
  charts, and map sections outside Git;
- compared the current explicit JSON pattern with RFC 8785 and official .NET 10
  `Utf8JsonWriter` behavior; and
- separated documented source facts, repository observations, design inferences, and unknowns.

Source priority was original SPI rules/maps/charts, adopted repository rulings, official technical
documentation, then repository evidence. Community material was not needed for this decision.

The spike stopped at a reviewed decision packet, normative specification, technical design,
traceability plan, and corrected roadmap. The owner subsequently approved that package and the
Content Pack v1 implementation followed it.

## Decision criteria

| Priority | Criterion |
| --- | --- |
| 1 | Deterministic identity and replay-safe future campaign binding |
| 2 | Complete load-time integrity with actionable typed failures |
| 3 | Per-datum provenance and unambiguous synthetic labeling |
| 4 | Only the data exercised by the next mechanics |
| 5 | Clear fog-of-war ownership and no accidental player-facing full-state contract |
| 6 | Rights-safe repository contents and external source workflow |
| 7 | Small test-first implementation checkpoints without a new service or dependency |

## Source and repository findings

### Documented facts

| Fact | Stable source |
| --- | --- |
| The physical Land game uses five lettered map sections with printed hex identifiers. | `spi-1979-land-rules:scan-page-7.map-sections` |
| Counters distinguish formation/element identity, organization/type, parent relationships, and other category facts. | `spi-1979-land-rules:scan-pages-7-10.counter-data` |
| Some elements are attached without an independent map counter, while Holding Boxes and off-map areas are meaningful locations. | `spi-1979-land-rules:scan-page-9.attachments-holding-boxes` |
| Movement depends on both destination terrain and features crossed between adjacent hexes; some hexside features are directional. | `spi-1979-land-rules:scan-pages-14-15.terrain-movement` |
| Clear and Desert are distinct hex-terrain terms; Road/Track are connected movement features; Slope has direction and Ridge does not. | `spi-1979-land-rules:8.33`; `spi-1979-land-rules:8.35`; `spi-1979-land-rules:8.43`; `spi-1979-land-rules:8.45`; `spi-1979-land-rules:8.46`; `spi-1979-land-rules:8.47` |
| Regiment and Battalion are organization sizes, and Battalion supplies a stacking-table input. | `spi-1979-land-rules:4.23.organization-size-key`; `spi-1979-common-charts:9.4.stacking-point-values` |
| Capability Point Allowance is a counter/source fact; Rule 8.17 derives the non-motorized threshold from it. | `spi-1979-land-rules:3.5.capability-point-allowance`; `spi-1979-land-rules:4.21.sample-units`; `spi-1979-land-rules:8.17` |
| Published scenario setup includes temporal bounds, formations/elements, hex and off-map deployments, and many systems not needed by the first skeleton. | `spi-1979-scenario-rules:printed-pages-27-28.graziani-setup` |
| Stacking value is derived from organization/type/attachment facts by a rules table. | `spi-1979-common-charts:chart-page-2.stacking-point-values` |
| Map pages also contain Holding Boxes and play aids; one physical page is not one gameplay object. | `spi-1979-map-a`, `spi-1979-map-b` |

The source scans are image-only and were inspected as temporary research artifacts. Their raster
coordinates, colors, layouts, artwork, and prose are evidence, not repository content or
authoritative fields.

### Repository observations

- `Cna.Core` has no package references and already uses explicit `Utf8JsonWriter` property order,
  strict readers, SHA-256 identities, immutable values, and source references.
- The ruleset manifest, campaign setup hash, and campaign history are separate identities. Setup
  presentation text is already excluded from authoritative identity.
- Campaign creation validates a current catalog entry, then records replay-complete setup facts.
  `CampaignSnapshot` does not yet contain world or content identity.
- The Initiative slice stops at Naval Convoy with no map, entity, placement, observation, or legal
  action state.
- Before this decision package, roadmap Sprint 3 began movement even though Naval Convoy,
  Initiative Declaration, and weather were deferred to Sprint 6. The synchronized roadmap now
  inserts the mandatory turn preamble before movement.

### Design inferences

- Coordinate arithmetic alone is not a safe adjacency contract across map seams, Holding Boxes,
  and future overlays. Opaque IDs plus explicit edges are simpler and more faithful.
- Terrain/category meaning belongs to the ruleset; assigning a category to a location or element
  belongs to content; current contact/cohesion/placement belongs to campaign world state.
- Persisting both source facts and a rules-derived stacking value would create contradictory
  authority.
- Optional fields for every published subsystem would promise unverified behavior. A v1 capability
  list and closed records should reject unsupported categories.
- A content hash without an immutable historical resolver is not a replay design. Embedding a full
  map in every event is also unnecessary duplication.
- Full authoritative content must never become the player observation type. Later observation
  projection consumes content plus world state and emits a separate side-safe contract.

## Options considered

### Canonical bytes

| Option | Strength | Failure mode | Decision |
| --- | --- | --- | --- |
| Default/reflection JSON | Little code | Serializer defaults and declaration order become accidental authority | Reject |
| RFC 8785/JCS | General cross-language standard | Adopts wider number, string, and sorting obligations than this integer-only domain needs | Defer |
| Explicit `sandtable.content-json.v1` writer | Matches current repository; exact and dependency-free | Requires a maintained byte/string specification and golden vectors | Adopt |

The adopted contract uses fixed object property order, ordinally sorted keyed collections,
semantic order only where specified, a closed safe-ASCII repertoire for every hashed string,
integer-only numeric values, no insignificant whitespace, UTF-8, and SHA-256. It rejects Unicode,
control characters, quotes, and backslashes at the authoritative boundary rather than relying on
runtime-variable escaping. Presentation remains a separate Unicode-capable contract. The format is
intentionally not advertised as JCS.

### Topology

| Option | Strength | Failure mode | Decision |
| --- | --- | --- | --- |
| Axial/offset coordinate arithmetic | Compact for regular generated boards | Unverified against printed numbering, seams, and future off-map areas | Reject as authority |
| Raster geometry and color | Mirrors a scan visually | Fragile, rights-sensitive, and not semantic | Reject |
| Stable IDs plus explicit graph edges | Handles seams and synthetic boards; validates directly | Slightly more data | Adopt |

A hex carries an optional source/display coordinate. Each adjacency is stored once as the canonical
ordered location-ID pair. Edge features can identify direction from one endpoint to the other.
Holding Boxes and off-map areas require a later typed schema/capability extension and are not v1
locations.

### Content identity and replay

| Option | Strength | Failure mode | Decision |
| --- | --- | --- | --- |
| Fold content into ruleset hash | One value | Unrelated content/rule edits invalidate each other and blur ownership | Reject |
| Embed complete pack in every campaign event | Self-contained events | Large duplicated immutable payloads and early campaign rewrite | Reject |
| Record pack ID/hash and resolve immutable bytes historically | Independent identities and scalable replay | Requires an explicit resolver/archive contract | Adopt |

The pack names its compatible ruleset ID and references rules-owned vocabulary IDs. Its hash does
not contain the complete current ruleset hash. Future campaign admission validates the exact
ruleset hash and content hash together, including every vocabulary reference, then records both.
Historical replay resolves the exact content bytes and runs under a rules implementation whose
manifest matches the recorded ruleset hash; a mismatch fails before projection.

### Presentation metadata

| Option | Strength | Failure mode | Decision |
| --- | --- | --- | --- |
| Hash labels and visuals with content | One document | Renaming a display label changes simulation identity | Reject |
| Exclude selected fields from one mixed model | Familiar | Easy to serialize a presentation field accidentally | Reject |
| Separate presentation record keyed by stable IDs | Clean authority boundary | Requires a join in Maproom | Adopt |

Content Pack v1 therefore has no authoritative display names or artwork. A synthetic fixture can
ship a separate presentation catalog with original labels and a conspicuous nonhistorical notice.

## Recommended contract boundary

The authoritative pack contains:

- schema version, format ID, pack ID, compatible ruleset ID, capability IDs, and content hash;
- a source index containing identifiers only, never scans or copied prose;
- hex definitions with explicit origin metadata;
- one canonical adjacency edge per neighboring hex pair;
- side IDs from the selected ruleset;
- formation and combat-element source facts plus parent relationships;
- one scenario definition with temporal bounds and initial placement declarations; and
- closed stable vocabulary IDs for the categories exercised by the pack.

Every semantic datum has an origin discriminant:

- `source-derived` with one or more stable `RuleReference` values; or
- `synthetic` with a stable repository locator and no historical claim.

Rules own the definitions and procedures behind terrain, edge, organization, side, stacking, and
mobility classification. Content owns category assignment and source facts such as base Capability
Point Allowance; it does not store a second mobility result. World state later copies initial
placements into mutable entity positions and owns remaining capability, contact, cohesion, ZOC,
and visibility.

Named Holding Boxes and off-map areas are proven future requirements but are not exercised by the
first fixture. They are a deliberate schema/capability extension before published scenario
ingestion, not part of minimal Content Pack v1.

## Rules-laboratory fixture decision

The first pack is `rules-lab.content.movement-contact.v1`. It contains:

- nine original hexes forming two routes around a restrictive center;
- clear and desert terrain assignments;
- a route feature, one directional edge feature, and one nondirectional edge feature;
- Axis and Commonwealth as rules-owned side IDs;
- one original formation and two original combat elements per side;
- four initial hex placements; and
- two source-fact CPA values for the later rules-owned mobility classifier.

The exact graph and labels will be frozen as golden data during implementation. All datum origins
are synthetic repository locators. The fixture is not North Africa, is not a historical scenario,
and contains no map/counter art, source text, combat ratings, victory conditions, supply, shipping,
ports, reinforcement schedules, facilities, air/naval entities, or detailed TOE data.

## Consequences and risks

### Positive consequences

- Published content can later enter through the same validator and canonicalizer without entering
  the first implementation.
- A source coordinate can be corrected without changing topology unless the semantic edge changes.
- Rules tables remain single authority and can explain derived values.
- Content is usable by the Umpire without becoming a remote service or a player-visible DTO.
- The fixture is small enough for exhaustive invalid-graph and canonical-order tests.

### Costs and mitigations

| Risk | Mitigation |
| --- | --- |
| Hand-maintained canonical writer drifts | Golden complete bytes, independent SHA-256 vector, mutation tests, contract version bump on change |
| Stable string IDs become an untyped escape hatch | Closed constructors, rules-owned vocabulary catalog, ordinal normalization, unknown-ID rejection |
| Historical replay prerequisites are deferred and later forgotten | Normative exact-content resolver plus matching-historical-executable requirements and roadmap checkpoint before world binding |
| Graph transcription errors | Per-edge origins, duplicate/self/missing endpoint validation, connected-fixture tests, later source cross-check |
| “Synthetic” is treated as low-integrity | Same production loader, validator, canonicalizer, and hash path as future sourced packs |
| Full content leaks through observations | Separate observation contract and negative tests; no content contract in Intelligence/transport surfaces |

## Remaining unknowns

- Which source-verified Naval Convoy capability is the smallest safe continuation from the current
  Initiative slice. This is a separate turn-preamble spike, not a content-v1 blocker.
- Whether Archives stores canonical content bytes directly or behind a content-addressed object
  interface. The deterministic resolver behavior is fixed; persistence technology is not.
- Exact published-map transcription tooling and double-entry verification. That belongs to the
  later content/rights gate.

## Owner decision

The project owner approved the recommended boundary and linked specification/design for the
test-first Content Pack v1 implementation. That approval does not authorize published scenario
transcription or campaign/world schema changes.

## Sources

- [Original Land Rules scan](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
- [Original Scenario Rules scan indexed by SPI](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
- [Original common charts scan](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
- [Preserved map scan](https://spigames.net/PDFv10/CNA_Maps.pdf)
- [SPI preservation index](https://spigames.net/rules_downloads.htm)
- [RFC 8785: JSON Canonicalization Scheme](https://www.rfc-editor.org/rfc/rfc8785.html)
- [.NET 10 `Utf8JsonWriter`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonwriter?view=net-10.0)
- [System.Text.Json character encoding](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding)
