# Content Pack v1 Technical Design

**Status:** Implemented foundation; schema 3 Movement mobility extension implemented

**Date:** 2026-08-16

**Specification:** [Content Pack v1](../specs/content-pack-v1.md)

## Design summary

Add one dependency-free Core content path:

```text
authoring JSON or in-process fixture
                 |
                 v
       strict contract reader
                 |
                 v
  local immutable value invariants
                 |
                 v
  pack graph/reference validation
                 |
                 v
cna-1979.1 vocabulary validation
                 |
                 v
 validated ContentPackArtifact
        |                 |
        v                 v
canonical UTF-8 JSON   typed Core queries
        |
        v
 independent SHA-256 content identity
```

The path performs no I/O. A host or future Archives adapter may supply bytes, but `Cna.Core.Content`
only accepts `ReadOnlySpan<byte>` or immutable values and returns values/issues/bytes.

## Ownership model

| Owner | Static or mutable authority |
| --- | --- |
| `Cna.Core.Rules` | Meanings and provenance of supported side, terrain, edge-feature, organization, and mobility IDs; stacking and other rule procedures/tables |
| `Cna.Core.Content` | Versioned static topology, category and per-element mobility assignments, force structure, scenario temporal bounds, initial deployment declarations, provenance, validation, canonical bytes/hash |
| `Cna.Core.Setups` | Campaign admission policy and selected setup identity; unchanged by Content Pack v1 |
| `Cna.Core.Campaigns` | Future exact content binding, mutable world positions/status, command legality, accepted events, snapshots, replay |
| Maproom presentation | Separate labels/visuals keyed by content IDs; never authoritative bytes |
| Intelligence/transport | No access to full content pack; receives only later side-safe observation contracts |

`Cna.Core.Content` is a domain module, not a service. The Umpire remains the only authority that can
turn static content and a command into state-changing events.

## Contract versions and identities

The initial contract used schema 1 and `sandtable.content-json.v1`. Weather-area assignments
advanced the schema to 2 without changing the canonical format. Movement mobility is an
authoritative element fact, so the current contract advances both schema and canonical format:

```text
Content schema version: 3
Canonical format ID:    sandtable.content-json.v2
Fixture pack ID:        rules-lab.content.movement-contact.v1
Compatible ruleset ID:  cna-1979.1
```

`ContentPackDefinition` does not contain its own hash. A successful factory returns:

```csharp
public sealed record ContentPackIdentity(
    int SchemaVersion,
    string FormatId,
    string PackId,
    string RulesetId,
    string Hash);

public sealed class ContentPackArtifact
{
    public ContentPackDefinition Definition { get; }
    public ContentPackIdentity Identity { get; }
    public int CanonicalByteCount { get; }
    public byte[] GetCanonicalBytes();
    public void CopyCanonicalBytes(Span<byte> destination);
}
```

`ContentPackArtifact.Create` is the only public hash-producing path. It requires successful pack
and ruleset-compatibility validation, writes canonical bytes, computes SHA-256, and retains a
private copied buffer. `GetCanonicalBytes` returns a fresh array on every call and
`CopyCanonicalBytes` copies into caller-owned storage of the exact declared length; no public
memory view exposes the private buffer. Tests mutate returned arrays and prove later copies and
`Identity.Hash` are unchanged. The hash is `sha256:` plus 64 lowercase hex digits. The hash itself
is absent from the hashed JSON, avoiding a circular contract.

Schema version changes when the semantic object contract changes. Format ID changes when canonical
byte rules change. Pack ID changes when a catalog author publishes a new intended content version;
old ID/hash/bytes remain available to historical resolution. Ruleset ID identifies the compatible
semantic vocabulary. Exact current ruleset hash stays an independent campaign identity.

## Core value model

Names define responsibilities and serialized semantics. Minor C# factoring is acceptable if the
specification, canonical shapes, and issue codes remain unchanged.

### Origin and source index

```csharp
public enum ContentOriginKind
{
    SourceDerived,
    Synthetic,
}

public enum ContentSourceKind
{
    PublishedPrimary,
    AdoptedRuling,
    RepositorySynthetic,
}

public sealed record ContentSourceIndexEntry(
    string SourceId,
    ContentSourceKind Kind);

public sealed record ContentOrigin(
    ContentOriginKind Kind,
    IReadOnlyList<RuleReference> References);
```

References are nonempty, duplicate-free, sorted by `SourceId` then `Locator`, and must resolve to a
pack source-index entry. A synthetic origin may reference only `RepositorySynthetic` entries; a
source-derived origin may not. The fixture source index has one entry:
`sandtable-rules-lab` / `RepositorySynthetic`.

Rules vocabulary rows keep their own `RuleReference` provenance in `Cna.Core.Rules`. Content origin
explains the assignment or structure fact. A terrain assignment can therefore be synthetic while
the meaning of `land.terrain.clear` remains source-derived rules data.

### Locations and edges

```csharp
public sealed record ContentHex(
    string LocationId,
    string TerrainId,
    ContentSourceCoordinate? SourceCoordinate,
    ContentOrigin Origin);

public sealed record ContentSourceCoordinate(
    string SectionId,
    string Label);

public sealed record ContentEdgeFeature(
    string FeatureId,
    string? DirectionFromLocationId,
    ContentOrigin Origin);

public sealed record ContentHexEdge(
    string FirstLocationId,
    string SecondLocationId,
    IReadOnlyList<ContentEdgeFeature> Features,
    ContentOrigin Origin);
```

`ContentHexEdge` normalizes endpoints ordinally; callers cannot use endpoint order as direction.
A directional feature explicitly names `DirectionFromLocationId`. The rules vocabulary declares
whether each feature requires or forbids direction. Features are sorted by feature ID and direction
and cannot duplicate that pair.

Source setups prove that Holding Boxes and off-map areas will be needed later. They are not part of
v1 because the fixture exercises neither their placement nor transfer semantics. Before published
scenario ingestion, add them through a named schema/capability extension and advance the format ID
if its canonical byte shape changes.

### Force structure

```csharp
public sealed record ContentFormation(
    string FormationId,
    string SideId,
    string? ParentFormationId,
    string OrganizationId,
    ContentOrigin Origin);

public enum ContentPlacementMode
{
    Independent,
    AttachmentOnly,
}

public sealed record ContentCombatElement(
    string ElementId,
    string SideId,
    string ParentFormationId,
    string OrganizationId,
    string MobilityId,
    int BaseCapabilityPointAllowance,
    ContentPlacementMode PlacementMode,
    ContentOrigin Origin);
```

No combat strength, current steps, stacking value, remaining capability, cohesion, expenditure,
supply, ZOC, contact, visibility, or current location appears here. Organization, base Capability
Point Allowance, and mobility assignment are static content facts. Mobility uses the closed
rules-owned IDs but is not inferred from CPA; the compatibility validator rejects an unsupported
assignment before a Content artifact can be admitted.
`ParentFormationId` is required for v1 combat elements.

### Scenario and placements

```csharp
public sealed record ContentScenarioBoundary(
    int GameTurn,
    int OperationStage);

public sealed record ContentInitialPlacement(
    string ElementId,
    string LocationId,
    ContentOrigin Origin);

public sealed record ContentScenario(
    string ScenarioId,
    ContentScenarioBoundary Start,
    ContentScenarioBoundary End,
    IReadOnlyList<ContentInitialPlacement> InitialPlacements,
    ContentOrigin Origin);
```

Operation Stage is 1 through 3. Temporal bounds describe a content scenario; they do not advance a
campaign or bypass the current Naval Convoy stop. `ContentInitialPlacement` is a declaration used
to initialize later world state, not mutable state itself.

### Pack and presentation

```csharp
public sealed record ContentPackDefinition(
    int SchemaVersion,
    string FormatId,
    string PackId,
    string RulesetId,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<ContentSourceIndexEntry> SourceIndex,
    IReadOnlyList<ContentHex> Locations,
    IReadOnlyList<ContentHexEdge> Edges,
    IReadOnlyList<ContentFormation> Formations,
    IReadOnlyList<ContentCombatElement> Elements,
    IReadOnlyList<ContentScenario> Scenarios);

public sealed record ContentPresentationCatalog(
    string PackId,
    string DisplayName,
    string Notice,
    IReadOnlyDictionary<string, string> Labels);
```

Presentation validates keys and immutable structure separately but is never passed to
`ContentPackSerializer` or `ContentPackArtifact.Create`. `Notice` for the fixture must state that
it is original, synthetic, and nonhistorical.

## Rules-owned content vocabulary

Add `Cna1979ContentVocabulary` in `Cna.Core.Rules`. Each row has schema version, stable ID,
applicable semantic kind, direction policy where relevant, and the frozen source references below.
The source terms/policies were visually rechecked during this decision package; implementation
consumes these IDs rather than deciding or renaming them.

| Kind | Stable ID | Direction policy | Frozen source locator |
| --- | --- | --- | --- |
| side | `axis` | n/a | `spi-1979-common-charts:7.2.initiative-ratings` |
| side | `commonwealth` | n/a | `spi-1979-common-charts:7.2.initiative-ratings` |
| terrain | `land.terrain.clear` | n/a | `spi-1979-land-rules:8.45.clear-hex` |
| terrain | `land.terrain.desert` | n/a | `spi-1979-land-rules:8.45.desert-hex` |
| edge feature | `land.edge.road` | forbidden | `spi-1979-land-rules:8.33`; `spi-1979-land-rules:8.47` |
| edge feature | `land.edge.track` | forbidden | `spi-1979-land-rules:8.33`; `spi-1979-land-rules:8.46` |
| edge feature | `land.edge.slope` | required | `spi-1979-land-rules:8.35`; `spi-1979-land-rules:8.43` |
| edge feature | `land.edge.ridge` | forbidden | `spi-1979-land-rules:8.35`; `spi-1979-land-rules:8.43` |
| organization | `land.organization.regiment` | n/a | `spi-1979-land-rules:4.23.organization-size-key` |
| organization | `land.organization.battalion` | n/a | `spi-1979-land-rules:4.23.organization-size-key`; `spi-1979-common-charts:9.4.stacking-point-values` |

The content schema, rather than the ruleset vocabulary, owns these frozen v1 capability tokens:

```text
land.hex-topology
land.formations
land.element-mobility
land.initial-deployment
land.weather-areas
```

Base Capability Point Allowance is a positive integer source fact defined at
`spi-1979-land-rules:3.5.capability-point-allowance` and shown on counters at
`spi-1979-land-rules:4.21.sample-units`. The current Movement source/ruling lock establishes that
CPA alone does not determine motorization, so every element carries one explicit supported
mobility ID.

The canonical ruleset manifest gains a `cna-1979.1.content-vocabulary` artifact. Its normalized
hash covers schema version, category kind, stable ID, direction policy, and sorted sources. Any
semantic vocabulary/source correction changes the ruleset hash, while changing a content
assignment changes only the content hash.

## Canonical byte contract

### Hashed string grammar and tokens

Every authoritative string is validated before serialization. Stable IDs use this ASCII grammar:

```text
^[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*$
```

Source-reference locators and source-coordinate section/label atoms use:

```text
^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$
```

The format/hash fields have their exact constant/`sha256:` shapes. No hashed string may contain a
quote, backslash, control, whitespace, `<`, `>`, `&`, non-ASCII code point, or any other character
outside its grammar. Consequently the writer never relies on a .NET release's Unicode block list
or escaping policy. Presentation labels/notices remain Unicode-capable because they are never
authoritative input or canonical bytes.

Serialized discriminants are frozen explicitly:

```text
content origin:  source-derived | synthetic
source kind:     published-primary | adopted-ruling | repository-synthetic
location kind:   hex
placement mode:  independent | attachment-only
```

`ContentPackSerializer.SerializeCanonical` uses `Utf8JsonWriter` with validation enabled and no
indentation. It supplies only prevalidated safe-ASCII strings, fixed property names, and frozen
tokens, so writer escaping cannot vary the resulting bytes. Numeric fields are signed base-10
integers within validated ranges.

Top-level property order is exact:

```text
schemaVersion
formatId
packId
rulesetId
capabilities
sourceIndex
locations
edges
formations
elements
scenarios
```

Canonical collection order:

| Collection | Order |
| --- | --- |
| capabilities | ordinal ID |
| sourceIndex | `sourceId` |
| locations | `locationId` |
| edges | `firstLocationId`, `secondLocationId` |
| edge features | `featureId`, then nullable `directionFromLocationId` with null first |
| formations | `formationId` |
| elements | `elementId` |
| scenarios | `scenarioId` |
| placements | `elementId`, then `locationId` |
| origin references | `sourceId`, then `locator` |

Variant property order:

```text
source index:  sourceId, kind
origin:        kind, references
reference:     sourceId, locator
hex:           locationId, kind, terrainId, sourceCoordinate, origin
coordinate:    sectionId, label
edge:          firstLocationId, secondLocationId, features, origin
edge feature:  featureId, directionFromLocationId, origin
formation:     formationId, sideId, parentFormationId, organizationId, origin
element:       elementId, sideId, parentFormationId, organizationId,
               baseCapabilityPointAllowance, placementMode, origin
scenario:      scenarioId, start, end, initialPlacements, origin
boundary:      gameTurn, operationStage
placement:     elementId, locationId, origin
```

Nullable fields are emitted as explicit `null`; absent and null never become two contracts.
Polymorphic `kind` values are lower-kebab strings. The serializer never writes presentation or
hash fields.

This contract is Sandtable-specific, not RFC 8785/JCS. Any byte-affecting change requires a new
format ID and golden-vector migration. Golden tests include the allowed grammar boundaries and
rejection of non-ASCII, control, quote, backslash, and encoder-sensitive characters.

### Strict reader

`ContentPackSerializer.Deserialize` uses `Utf8JsonReader` directly or an equivalently strict
explicit parser. It:

- accepts object-property and keyed-collection input in any order;
- rejects duplicate/unknown/missing properties, trailing tokens, unsupported version/format,
  invalid discriminants, and invalid numeric forms;
- constructs immutable values only after local parsing succeeds; and
- returns a parse result distinct from pack/compatibility validation results.

Successfully read input is not considered canonical. Re-serialization produces the one canonical
order and hash.

## Validation design

### Results

```csharp
public sealed record ContentValidationIssue(
    string Code,
    string Path,
    string Message);

public sealed class ContentValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ContentValidationIssue> Issues { get; }
}
```

`Message` is not used for program flow or canonical bytes. Issues sort ordinally by `Path`, then
`Code`, then `Message`. Paths use a stable JSON-Pointer-like form keyed by ID, for example:

```text
/edges/west|center/features/land.edge.slope
/elements/axis-element-a/parentFormationId
/scenarios/movement-contact-lab/initialPlacements/axis-element-a
```

IDs are escaped with the JSON Pointer `~0`/`~1` rules if necessary, though v1 IDs do not contain
either character.

### Pack validator passes

The validator never mutates/repairs input and attempts all passes whose prerequisites are present:

1. index duplicate IDs by category and source;
2. validate origins against source index and origin kind;
3. validate capabilities against fields actually present;
4. validate hex/edge endpoints, canonical pair uniqueness, feature duplicates/direction,
   neighbor count, and graph connectivity for declared hex topology;
5. validate formation parents, cycles, and side consistency;
6. validate element parent/side, mobility capability, positive base CPA, and placement mode;
7. validate scenario bounds, placement references, uniqueness, and hex eligibility; and
8. sort/deduplicate issue values.

Canonical hashing is unavailable when any issue exists.

### Ruleset compatibility validator

`Cna1979ContentCompatibilityValidator` first requires exact ruleset ID `cna-1979.1`, then checks
every semantic vocabulary reference against `Cna1979ContentVocabulary` and every element mobility
against the closed `Cna1979Movement` vocabulary; the pack validator checks capabilities against the
closed content-schema tokens. Compatibility does not look up movement costs, infer mobility from
CPA, or calculate stacking. The same validator is called by the synthetic catalog and campaign
admission.

Unknown data fails explicitly. There is no “custom,” “other,” integer enum fallback, ignored
extension dictionary, or nearest category.

## Synthetic fixture

`Cna1979SyntheticContentCatalog` owns one immutable artifact and one separate presentation catalog.
The authoritative graph is:

```text
 north-west ---- north ---- north-east
     |                         |
   west ------ center ------ east
     |                         |
 south-west ---- south ---- south-east
```

Canonical locations:

| ID | Terrain | Purpose |
| --- | --- | --- |
| `north-west` | clear | north route |
| `north` | clear | north route |
| `north-east` | clear | north route |
| `west` | clear | Axis approach |
| `center` | desert | restrictive direct route |
| `east` | clear | Commonwealth approach |
| `south-west` | clear | south route |
| `south` | clear | south route |
| `south-east` | clear | south route |

Canonical edges and features:

| Pair | Features |
| --- | --- |
| `center`—`east` | ridge |
| `center`—`west` | slope directed from `west` |
| `east`—`north-east` | road |
| `east`—`south-east` | track |
| `north`—`north-east` | road |
| `north`—`north-west` | road |
| `north-west`—`west` | road |
| `south`—`south-east` | track |
| `south`—`south-west` | track |
| `south-west`—`west` | track |

The direct and outer paths create terrain/edge alternatives without copying any published map.
The graph is deliberately not represented as a geographic North Africa layout.

Force structure:

| ID | Side | Parent | Organization | Mobility | Base CPA | Initial location |
| --- | --- | --- | --- | --- | --- | --- |
| `axis-lab-formation` | Axis | none | regiment | n/a | n/a | n/a |
| `axis-element-a` | Axis | `axis-lab-formation` | battalion | motorized | 20 | `west` |
| `axis-element-b` | Axis | `axis-lab-formation` | battalion | non-motorized | 10 | `north-west` |
| `commonwealth-lab-formation` | Commonwealth | none | regiment | n/a | n/a | n/a |
| `commonwealth-element-a` | Commonwealth | `commonwealth-lab-formation` | battalion | motorized | 20 | `east` |
| `commonwealth-element-b` | Commonwealth | `commonwealth-lab-formation` | battalion | non-motorized | 10 | `south-east` |

These are explicit synthetic fixture assignments. Their alignment with the fixture CPA values is
not a production derivation rule.

Scenario ID is `movement-contact-lab`, with synthetic temporal bounds Game Turn 1 Operation Stage
1 through Game Turn 1 Operation Stage 3. Those bounds exercise the contract only and do not claim
the campaign can yet advance there.

All origins use unique `sandtable-rules-lab` locators rooted at
`content.movement-contact.v1`. Presentation labels use original exercise names such as “Amber Wadi
Exercise,” “Copper Group,” and “Azure Group,” plus this required notice:

> Original synthetic rules laboratory; nonhistorical and not a CNA scenario.

The fixture declares `land.hex-topology`, `land.formations`, and `land.initial-deployment`.

## Catalog behavior

The synthetic catalog is an immutable in-process dictionary keyed by pack ID. Lookup requires ID
and expected hash; an unknown ID or mismatch returns a typed rejection and no default. Catalog
construction itself creates the artifact through full validation and fails fast during static
initialization if repository fixture data is invalid.

The catalog is an integration fixture and current admission source, not the historical persistence
design. Tests must also create packs directly through the same factory, proving there is no
fixture-only bypass.

## Future campaign and replay seam

Content Pack v1 does not change commands/events/snapshots. The future campaign-content binding must
add versioned contracts deliberately:

```text
CreateCampaign(setupId, setupHash, contentPackId, contentHash, scenarioId)
             |
             v
resolve immutable ContentPackArtifact outside authoritative grain turn
             |
             v
validate setup + scenario + exact ruleset/content compatibility
             |
             v
CampaignCreated records ruleset hash + content identity + scenario ID
             |
             v
project initial mutable world state through validated creation event(s)
```

A future pure `IContentPackResolver` (or concrete equivalent) resolves exact ID/hash to immutable
canonical bytes/artifact. Archives may implement storage around it, but remote I/O completes before
the Umpire decision. Replay receives the resolver as explicit context and fails with typed invalid
history if content is missing or mismatched. It never asks the current catalog for “latest.”

Pre-alpha replay also requires the executable/rules implementation whose canonical manifest hash
matches the history's recorded ruleset hash. The currently compiled `Cna1979Ruleset` does not
interpret an older hash. A mismatch fails before projection. A generic historical ruleset
interpreter/resolver is not part of this design; release/archive metadata must retain which
historical executable is required.

The existing setup schema will require a new version. Its future entry references content pack ID,
hash, and scenario ID while retaining setup-owned initiative/admission policy until source-derived
scenario contracts model that policy explicitly. This avoids mixing Content Pack v1 into the
current campaign snapshot as a hidden additive field.

Do not embed the entire pack in `CampaignCreated`. Do record enough creation facts/events to build
mutable initial world state deterministically. Static terrain/topology remains a required exact
replay dependency addressed by content hash.

## Observation and fog seam

Content is complete authoritative input. A later projection combines:

```text
validated content + authoritative world state + acting side
                              |
                              v
                    side-safe observation
```

The output contains only observed/remembered public facts and legal action identifiers. It never
contains `ContentPackDefinition`, opponent formation definitions merely because they exist in the
pack, source scans, random state, or hidden placements. Intelligence receives only that output.

Content Pack v1 tests enforce the dependency direction statically; the observation slice later
adds behavioral negative tests for both sides.

## Delivery ordering correction

The next authoritative capabilities are:

```text
Content Pack v1 + synthetic fixture
                 |
                 v
content admission + initial world
                 |
                 v
side-safe observations
                 |
                 v
legal actions + stale enforcement
                 |
                 v
turn preamble: Naval Convoy -> Initiative Declaration -> Weather
                 |
                 v
movement/contact
                 |
                 v
combat
```

Content work can be implemented before the turn-preamble mechanics because it does not advance a
campaign. Authoritative movement cannot.

## Implementation checkpoints

Each checkpoint uses red-green-refactor, owns at most a small file cluster, and ends with focused
tests plus the full Core test project.

**Implementation status:** `CNT-IMP-001` through `CNT-IMP-005` are delivered on the Content Pack v1
feature branch. The complete synthetic artifact is 9,897 canonical UTF-8 bytes with frozen identity
`sha256:0cf3b3ff21f7a8a8fbcd2667a6b4b3db83b4dab00495add723e5c2f355cf2800`.

### `CNT-IMP-001` — Rules vocabulary artifact

**Owns:** `Rules/Cna1979ContentVocabulary.cs`, ruleset-manifest integration, focused Rules tests.

- Implement the frozen source-cited vocabulary rows above and canonical artifact hashing.
- Prove unknown IDs and every semantic/source mutation affect compatibility/ruleset identity.

**Requirements:** `CNT-007`, `CNT-010`, `CNT-NFR-005`.

### `CNT-IMP-002` — Immutable content and origin contracts

**Owns:** content definition/origin value files and `ContentContractsTests`.

- Add closed values, ID validation, defensive copying, structural equality, and local invariants.
- Enforce the exact authoritative string grammars and frozen enum tokens.
- Add separate presentation values with no authoritative dependency.

**Requirements:** `CNT-003`-`005`, `CNT-008`-`011`, `CNT-014`, `CNT-NFR-002`.

### `CNT-IMP-003` — Graph and compatibility validation

**Owns:** validation issue/result, pack validator, compatibility validator, validator tests.

- Implement deterministic multi-issue passes and stable paths/codes.
- Cover all topology, force hierarchy, placement, capability, origin, and vocabulary negatives.

**Requirements:** `CNT-006`-`012`.

### `CNT-IMP-004` — Canonical reader/writer and artifact hash

**Owns:** serializer, artifact factory, canonical tests/golden vector.

- Write exact JSON shape and hash.
- Add strict order-tolerant input parser.
- Prove order normalization, round trip, semantic mutation, invalid-hash unavailability, and
  presentation exclusion.
- Prove safe-ASCII accept/reject vectors and copy-on-read canonical-byte mutation resistance.

**Requirements:** `CNT-001`, `CNT-002`, `CNT-012`, `CNT-015`, `CNT-NFR-001`, `CNT-NFR-004`.

### `CNT-IMP-005` — Synthetic catalog and demonstration

**Owns:** synthetic catalog/fixture, presentation entry, fixture/rights/boundary tests, synchronized
docs.

- Freeze exact graph, entities, placements, origins, canonical bytes, and hash.
- Resolve it through full artifact/catalog paths.
- Verify original/nonhistorical labeling and absence from outward DTO projects.

**Requirements:** `CNT-004`, `CNT-013`-`016`, `CNT-NFR-003`, `CNT-NFR-006`.

### Later named checkpoints

- `WORLD-001`: versioned exact content admission, historical resolver, and initial world projection
  (`CNT-017`), now specified in [Campaign World v1](campaign-world-v1.md).
- `OBS-001`: side-safe observation projection and negative hidden-state tests (`CNT-016`).
- `ACTION-001`: legal-action generation, accepted-command membership, and stale-action enforcement.
- `TURN-001`: source spike/design and implementation for Naval Convoy, Initiative Declaration, and
  Weather Determination (`CNT-018`).
- `MOVE-001`: first movement/contact capability only after `WORLD-001`, `OBS-001`, `ACTION-001`,
  and `TURN-001` pass.

These are explicit deferrals, not part of Content Pack v1 completion.

## Requirement-to-task-to-test traceability

| Requirements | Task | Acceptance evidence |
| --- | --- | --- |
| `CNT-001`, `CNT-002`, `CNT-012`, `CNT-NFR-001`, `CNT-NFR-004` | `CNT-IMP-004` | `CNT-AC-002`, `CNT-AC-003`, `CNT-AC-004`, `CNT-AC-010` |
| `CNT-003`, `CNT-004`, `CNT-005`, `CNT-008`, `CNT-009`, `CNT-011`, `CNT-NFR-002` | `CNT-IMP-002` | `CNT-AC-003`, `CNT-AC-004`, `CNT-AC-005`, `CNT-AC-006`, `CNT-AC-007`, `CNT-AC-008` |
| `CNT-006`, `CNT-010`, `CNT-011`, `CNT-012` | `CNT-IMP-003` | `CNT-AC-007`, `CNT-AC-008`, `CNT-AC-009`, `CNT-AC-010` |
| `CNT-007`, `CNT-010`, `CNT-NFR-005` | `CNT-IMP-001`, `CNT-IMP-003` | `CNT-AC-004`, `CNT-AC-009`, `CNT-AC-011` |
| `CNT-004`, `CNT-013`, `CNT-014`, `CNT-015`, `CNT-016`, `CNT-NFR-003`, `CNT-NFR-006` | `CNT-IMP-005` | `CNT-AC-001`, `CNT-AC-002`, `CNT-AC-005`, `CNT-AC-011`, `CNT-AC-012`, `CNT-AC-013`, full gate |
| `CNT-016` | `OBS-001` | side-safe behavioral tests deferred |
| `CNT-017` | `WORLD-001` | Complete; Campaign World v1 binds exact content and projects initial world state |
| `CNT-018` | `TURN-001`, then `MOVE-001` | `CNT-AC-015` planning gate |

## Failure behavior

- Parse failures never return a partial pack.
- User-authored semantic errors return all discoverable typed issues; they do not throw generic
  exceptions or auto-repair data.
- Invalid/incompatible packs cannot produce canonical bytes, a content hash, a catalog entry, or a
  future campaign event.
- Catalog ID/hash mismatch never falls back to another pack.
- Unknown capability/vocabulary/category is unsupported, not ignored.
- Missing historical content will make future replay fail explicitly rather than project a partial
  state.
- No content path reads files, performs network I/O, resolves services, or calls Intelligence.

## Verification

Focused work uses .NET 10 Microsoft.Testing.Platform project selection/filters. Final verification:

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

Implementation is not ready to merge until every in-scope requirement maps to a passing test,
deferred rows remain named in roadmap/docs, the repository contains no source asset, and a fresh
independent review has no blocking finding.
