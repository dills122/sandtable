# Content Pack v1 Specification

**Status:** Implemented foundation; schema 3 Movement mobility extension implemented

**Date:** 2026-08-16

**Rules target:** `cna-1979.1`

**Research:** [Content Pack v1 spike](../research/content-pack-v1-spike.md)

> **Current evolution:** This specification froze the original Content Pack foundation. Subsequent
> `WORLD-001`, `OBS-001`, `ACTION-001`, turn-preamble, Reserve, and Movement-foundation packages
> delivered exact campaign admission, mutable world state, side-safe observations, legal actions,
> and the current Movement terminal. Statements about those capabilities being future work describe
> this specification's historical scope; Movement campaign actions, contact, and combat remain
> unimplemented.

## Objective

Add the smallest authoritative static-content foundation needed by the next Sandtable vertical
slices. A developer can load an original rules-laboratory pack through the same Core path intended
for future source-derived content, receive complete deterministic validation diagnostics, inspect
stable canonical bytes and identity, and query typed topology/entity/setup facts without loading a
scan, service, clock, random source, campaign, or model.

This specification's original change ended at validated content and a synthetic fixture. Campaign
world binding, observations, legal actions, turn-preamble rules, movement, contact, and combat were
separate capabilities; the current-evolution note above records which of those later packages have
since shipped.

## Developer-visible demonstration

1. Resolve `rules-lab.content.movement-contact.v1` from the in-process synthetic catalog.
2. Validate it successfully against the `cna-1979.1` content vocabulary.
3. Serialize it as `sandtable.content-json.v2` schema 3, display its `sha256:` identity, and round-trip it
   through the strict reader to an equal immutable value.
4. Query its nine locations, explicit adjacency edges, two formations, four combat elements, and
   four initial placements.
5. Load representative malformed variants and inspect all stable, sorted issue codes and paths.
6. Change input collection order and observe identical canonical bytes/hash; change a semantic
   fact and observe a different hash.
7. Change only the separate presentation catalog and observe unchanged authoritative bytes/hash.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `CNT-001` | Content has an independent versioned identity consisting of current format ID `sandtable.content-json.v2`, schema version 3, pack ID, compatible ruleset ID, and `sha256:` hash of validated canonical semantic bytes. |
| `CNT-002` | Canonical bytes use explicit fixed object/property order, UTF-8, no insignificant whitespace, exact safe-ASCII hashed-string grammars, frozen lower-kebab tokens, integer-only numeric values, and ordinally normalized keyed collections. The hash field and presentation metadata are excluded. |
| `CNT-003` | Every semantic location, edge, formation, element, scenario, and placement datum declares typed origin metadata: `source-derived` with stable references or `synthetic` with a stable repository locator. |
| `CNT-004` | The source index retains stable source IDs and classifications only. Content contracts, tests, and fixtures contain no scans, component artwork, copied source prose, or raster geometry. |
| `CNT-005` | Version 1 board locations are opaque stable hex IDs. Adjacency is authoritative only through one canonical unordered edge per pair; printed/source coordinates are optional audit metadata and never derive adjacency. |
| `CNT-006` | Edges reference existing distinct hexes and may carry closed rules-owned feature IDs plus an optional direction from one endpoint to the other. Unknown, duplicate, contradictory, malformed, or disconnected fixture topology is rejected. |
| `CNT-007` | Content references closed rules-owned IDs for side, terrain, edge feature, organization, and element mobility semantics. Unknown IDs fail validation; rule tables and derived values such as movement cost or stacking points are not content fields. |
| `CNT-008` | Formations and combat elements have stable pack-local IDs, side ownership, organization and base Capability Point Allowance source facts, one explicit supported mobility assignment per element, and validated parent relationships. Runtime remaining capability, expenditure, cohesion, contact, ZOC, visibility, and current location are not content fields. |
| `CNT-009` | A scenario definition declares stable temporal bounds and initial placements. Placements reference existing elements and hexes, are unique per independently placed element, and cannot place an attachment-only element independently. |
| `CNT-010` | Pack capabilities are a closed, sorted set. The current schema supports hex topology, land formations/elements, element mobility, initial deployment, and weather areas; unsupported published subsystems fail explicitly rather than appearing as optional catch-all data. |
| `CNT-011` | Constructors defensively copy collection inputs and enforce local shape invariants. Pack validation returns every discoverable graph/cross-reference issue as a stable code plus canonical data path, sorted by path then code. |
| `CNT-012` | Canonical serialization and hashing require a valid pack. The strict reader rejects unknown contract versions, missing/unknown properties, duplicate JSON properties, invalid enum/ID/string representations, trailing data, and noncanonical semantic values; accepted input property/collection order is normalized on output. |
| `CNT-013` | The synthetic fixture uses the production contracts, validator, reader/writer, and hashing path. It contains nine connected original hexes, two routes around a restrictive center, two sides, one formation and two elements per side, and four initial placements. |
| `CNT-014` | Every fixture datum is marked synthetic with a stable `sandtable-rules-lab` locator. Separate presentation data uses original labels, identifies the theater as nonhistorical, and cannot change authoritative equality, bytes, or hash. |
| `CNT-015` | Content operations are pure, synchronous, deterministic, and free of file, network, clock, random, model, service-container, campaign, host, and persistence I/O. No new runtime package or service is introduced. |
| `CNT-016` | The full content contract is Core-internal authoritative input and is not reused as a player observation, legal-action, Intelligence, or transport DTO. |
| `CNT-017` | Future campaign admission must resolve a pack by exact ID/hash before an authoritative turn, validate its ruleset compatibility, and record content identity beside exact ruleset identity. Replay must fail explicitly when immutable historical bytes are missing/hash-mismatched or the executing rules manifest does not match the recorded ruleset hash; a generic historical rules interpreter is not required in pre-alpha. |
| `CNT-018` | Authoritative movement cannot be implemented until a separate accepted design advances the current campaign through Naval Convoy, Initiative Declaration, and Weather Determination without skipping mandatory rules. |

## Contract invariants

- Contract/schema versions are positive recognized integers. IDs match
  `^[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*$`.
  Source/coordinate atoms match `^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$`. Every other
  authoritative string is a frozen token or lowercase hash. Quotes, backslashes, controls,
  whitespace inside string values, and non-ASCII characters are rejected before hashing.
- Pack ID is unique in a catalog. One ID resolves to one hash; retaining another version requires a
  new pack ID or an immutable historical hash-addressed entry.
- Content hash is lowercase `sha256:` followed by 64 hexadecimal digits and equals the hash of the
  exact canonical bytes excluding the hash itself.
- Keyed collections are duplicate-free under ordinal comparison and exposed in canonical ID order.
- Origin collections are nonempty, duplicate-free, and canonical. `source-derived` and `synthetic`
  are mutually exclusive for one datum.
- Every adjacency pair is stored with the ordinally lesser hex ID first. An edge cannot join a hex
  to itself.
- The initial fixture graph is connected, and a hex has at most six distinct adjacent hexes.
- A directional feature names exactly one endpoint as `from`; nondirectional features have no
  direction. A feature kind determines whether direction is required or forbidden.
- Formation parent relationships are acyclic and cannot cross side ownership.
- Element parent formation and side agree. Attachment-only elements have no initial independent
  placement; independently placed elements have exactly one initial placement.
- Scenario start is not later than its end under canonical Game Turn/Operation Stage ordering.
- Presentation records may be absent and may change independently. Their keys must reference known
  pack IDs when a presentation catalog is validated, but they are never serialized by the
  authoritative writer.

## Validation behavior

Validation has three deterministic layers:

1. constructors reject null, blank, undefined, out-of-range, or locally contradictory values;
2. `ContentPackValidator` returns graph, uniqueness, capability, and cross-reference issues without
   throwing for user-authored content mistakes; and
3. `Cna1979ContentCompatibilityValidator` returns unknown/incompatible vocabulary issues.

An unrecognized serialized contract or malformed JSON is a parse failure, distinct from a parsed
pack with validation issues. Canonical bytes/hash are unavailable until both pack and ruleset
compatibility validation succeed. Issue messages are diagnostic presentation; stable issue code
and path are the machine contract.

Initial stable issue codes include:

```text
content.duplicate-id
content.unknown-reference
content.unsupported-capability
content.invalid-origin
topology.self-edge
topology.duplicate-edge
topology.too-many-neighbors
topology.disconnected
topology.invalid-direction
formation.parent-cycle
formation.side-mismatch
placement.duplicate-element
placement.attachment-only
element.invalid-base-cpa
scenario.invalid-bounds
vocabulary.unknown-id
```

The implementation may add more precise codes, but it must not collapse these independent failure
classes into one generic exception.

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `CNT-NFR-001` | Equal semantic inputs produce byte-identical canonical JSON and hash across supported platforms, cultures, process runs, and collection insertion orders. |
| `CNT-NFR-002` | Public immutable values use nullable annotations, defensive copies, structural equality/hash semantics, no mutable collection exposure, and no public memory view over the private canonical byte buffer. |
| `CNT-NFR-003` | The complete fixture and every canonical artifact remain small enough for direct golden-byte review; no generated artifact churn is committed. |
| `CNT-NFR-004` | Canonical reader/writer and validator APIs make invalid states unrepresentable or explicitly unavailable; no “best effort” normalization hides bad source data. |
| `CNT-NFR-005` | Source locators are precise enough to reconcile one datum without embedding copyrighted expression. |
| `CNT-NFR-006` | New code passes repository analyzers, formatting, build, and test gates with zero warnings and no skipped tests. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `CNT-AC-001` | Load the canonical rules-laboratory pack | Validation succeeds; exact expected counts, capabilities, identities, origins, and topology are present. |
| `CNT-AC-002` | Serialize, hash, read, validate, and serialize the fixture again | Complete golden UTF-8 bytes and independent SHA-256 vector match; semantic value and second bytes are identical. Safe-ASCII boundary vectors are accepted and non-ASCII/control/quote/backslash vectors are rejected. |
| `CNT-AC-003` | Construct equal packs with every input collection reversed/shuffled | Canonical bytes/hash and semantic equality/hash code are identical. |
| `CNT-AC-004` | Mutate one semantic terrain, edge, organization, mobility, base CPA, parent, temporal, placement, origin, capability, or ruleset-ID fact | Content hash changes in every selected mutation. |
| `CNT-AC-005` | Change fixture presentation label/notice only | Authoritative equality, bytes, and hash remain unchanged. |
| `CNT-AC-006` | Mutate caller-owned lists or any byte array returned by an artifact after construction | Constructed values, equality, private canonical bytes, future byte copies, and hash do not change. |
| `CNT-AC-007` | Duplicate an ID/edge/element placement or introduce a missing reference | All applicable stable issue code/path pairs are returned in canonical order. |
| `CNT-AC-008` | Add a self-edge, seventh neighbor, disconnected hex, wrong directional feature, formation cycle, side mismatch, nonpositive base CPA, or attachment-only placement | The precise topology/formation/element/placement issue is returned; hash is unavailable. |
| `CNT-AC-009` | Use an unknown terrain, edge, side, organization, mobility, capability, or ruleset ID | Compatibility validation rejects with `vocabulary.unknown-id` or `content.unsupported-capability`; no fallback is selected. |
| `CNT-AC-010` | Feed malformed, duplicate-property, unknown-property, trailing-data, unknown-version, or invalid-value JSON to the strict reader; separately reorder valid properties/collections | Invalid input is rejected, while valid reordered input emits the one canonical output. |
| `CNT-AC-011` | Inspect the repository and fixture origin/presentation data | No source scan/artwork/prose is committed; every datum is synthetic; all names/geometry are original and nonhistorical. |
| `CNT-AC-012` | Search project references and serialize future-facing DTO fixtures | Content pack types are absent from Intelligence/contracts/transport and player-observation surfaces. |
| `CNT-AC-013` | Run content operations under a plain unit test with no host/container/files | All operations succeed deterministically without I/O or ambient dependencies. |
| `CNT-AC-014` | Attempt the later campaign binding with missing/mismatched content identity or a nonmatching rules manifest | Deferred campaign-admission/replay tests reject before creation/projection; they cannot substitute a catalog default or current ruleset. |
| `CNT-AC-015` | Attempt to implement movement while the campaign still stops at Naval Convoy | Roadmap/design review blocks the slice until turn-preamble acceptance is complete. |

## Repository structure

The implementation is expected to remain inside existing projects:

```text
src/Cna.Core/
  Content/                 immutable contracts, origins, validation, canonical I/O/hash
  Rules/                   cna-1979.1 content vocabulary meanings and provenance
  Setups/                  existing campaign setup seam; unchanged in Content Pack v1

tests/Cna.Core.Tests/
  Content/                 contract, validator, canonical, fixture, and rights-boundary tests
  Rules/                   vocabulary/provenance/hash tests when rules artifacts change
```

Do not create a Content service, storage adapter, general schema framework, code generator, or new
NuGet dependency. Prefer small sealed records/classes, explicit constructors, ordinal comparers,
and the repository's existing JSON/hash conventions.

## Testing strategy

- Follow red-green-refactor for every behavior checkpoint.
- Start with contract immutability and local invariant tests.
- Add table-driven graph/cross-reference/capability validation tests.
- Freeze the complete fixture as readable golden JSON bytes and a separately calculated hash.
- Test reader rejection separately from semantic validation.
- Use mutation tests for every authoritative field and negative tests for presentation-only fields.
- Test the synthetic catalog only through the same public path later sourced packs will use.
- Keep future campaign binding and observation tests explicitly deferred; do not create fake APIs
  solely to make those rows appear green.

Exact verification commands:

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

`just check` may be used as the full local gate when it covers the same repository commands.

## Boundaries and non-goals

In scope for the first implementation:

- closed rules-owned vocabulary required by the fixture;
- content origins, immutable values, validation, strict canonical reader/writer/hash;
- separate presentation metadata contract;
- in-process synthetic catalog and one complete fixture; and
- focused documentation and tests.

Explicitly deferred:

- campaign command/event/snapshot migration and historical content persistence;
- mutable world/entity state, current placement, observations, legal actions, and fog projection;
- turn-preamble, movement, contact, reaction, capability, cohesion, ZOC, stacking, and combat rules;
- published scenario/map/counter transcription, ingestion tooling, or data-entry UI;
- named Holding Boxes, off-map areas, their placements/transfers, and attachment behavior beyond
  validating v1 declarations;
- source-derived non-synthetic pack content; and
- Maproom presentation beyond the standalone presentation value and original fixture labels.

## Traceability

| Requirement group | Governing roadmap requirements | Planned evidence |
| --- | --- | --- |
| `CNT-001`, `CNT-002`, `CNT-012`, `CNT-NFR-001`, `CNT-NFR-004` | `DET-001`, `REL-001` | `CNT-AC-002`-`004`, `010` |
| `CNT-003`, `CNT-004`, `CNT-014`, `CNT-NFR-005` | `SRC-001`, `IPR-001` | `CNT-AC-001`, `004`, `011` |
| `CNT-005`, `CNT-006` | `FID-001`, `FID-002` | `CNT-AC-001`, `007`, `008` |
| `CNT-007`, `CNT-008`, `CNT-009`, `CNT-010` | `FID-001`, `FID-002`, `UX-001` | `CNT-AC-001`, `004`, `007`-`009`, especially `008` |
| `CNT-011`, `CNT-NFR-002` | repository quality rules | `CNT-AC-003`, `006`-`009` |
| `CNT-013`, `CNT-NFR-003` | Sprint 2 rules-laboratory goal | `CNT-AC-001`, `002`, `011` |
| `CNT-015`, `CNT-NFR-006` | `DET-001`, repository quality rules | `CNT-AC-013`, full gate |
| `CNT-016` | `FOW-001` | `CNT-AC-012` covered the original static boundary; complete side-safe behavior was delivered by `OBS-001` |
| `CNT-017` | `DET-001`, `EVT-001`, `REL-001` | Complete in `WORLD-001`; see [Campaign World v1](campaign-world-v1.md) |
| `CNT-018` | `FID-001`, `FID-002`, `EVT-001` | Preamble gate complete for the admitted synthetic path; Movement campaign actions remain pending |

## Open questions and owner choices

The project owner approved the nine-hex/four-element fixture and the separate presentation catalog
boundary. No Content Pack v1 owner choice remains open.

Persistence technology for historical content and the exact Naval Convoy capability are deliberate
later decisions.

## Success and exit criteria

The Content Pack v1 implementation is complete when `CNT-AC-001` through `CNT-AC-013` pass,
`CNT-AC-014` and `CNT-AC-015` remain visibly deferred to their named capabilities, the complete
repository gate passes, the roadmap/design documents remain synchronized, no copyrighted asset is
present, and a fresh independent reviewer reports no blocking correctness or implementation-plan
gap.

Project-owner approval was received before production implementation. Fresh implementation review
and full-gate evidence remain required before merge.
