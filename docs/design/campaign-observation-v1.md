# Campaign Observation v1 Technical Design

**Status:** Contract 5 implemented, repository-verified, and independently reviewed

**Date:** 2026-08-16

**Capability:** `OBS-001`

**Governing specification:**
[Campaign Observation v1](../specs/campaign-observation-v1.md)

**Research inputs:**
[Observation and Fog Boundary](../research/observation-and-fog-boundary-spike.md),
[Reconnaissance, Contacts, and Dummy Knowledge](../research/recon-contact-knowledge-spike.md)

**Current evolution:** `MOV-TASK-005` clean-cuts the original value model to contract 5 and policy
`sandtable.observation.movement-side-safe.v1`. Contract 3 added owner Reserve status; contract 4
projected an audience-visible revision that excludes hidden opposing Reserve designation increments.
Contract 5 adds exact own mobility, operational-ledger, Cohesion, and nullable Breakdown-risk facts;
canonical three-field apparent opposing presence; and strict, non-authoritative canonical readback.
Current admitted apparent rows always carry `exertsZoc = false`; positive qualification remains
gated on `ZOR-TASK-002`. The contract, implementation, repository verification, and independent
review are complete.

`MOV-TASK-006` now consumes contract-5 observations through a separate pure dormant Movement
candidate deriver. That downstream use changes no observation field, policy, identity, or bytes and
does not make a Movement action public or executable.

## Intent and completion boundary

Add the first enforceable fog-of-war projection boundary to Core. Given one admitted campaign
snapshot, its exact resident content context, and an observing side, Core returns an immutable
observation containing only allowlisted public and own-side facts. The same value has one explicit
canonical JSON representation for deterministic comparison and future adapter input.

This slice is complete when:

- every currently admitted campaign checkpoint projects for both sides;
- public topology and exact own independently placed Movement/risk facts are complete;
- approved apparent opposing rows contain only opaque representation ID, current apparent
  location, and current-supported ZOC;
- hidden opponent changes have no observable effect when the approved apparent rows remain equal;
- prohibited authoritative types and metadata are absent from the value graph and bytes;
- invalid observers and invalid checkpoints produce typed, empty results;
- repeated projection and strict canonical readback are pure and byte-deterministic; and
- the complete repository gate and a fresh independent implementation review pass.

It does not create transport, persistence, Movement legal actions, source-complete contacts,
reconnaissance/knowledge history, positive ZOC qualification, or Movement authority.

## Architecture decision

Create a new `Cna.Core.Observations` namespace. Observation values depend only on approved scalar
domain concepts such as `LandSide` and `LandActorRole`. They do not retain or inherit campaign,
content, world, rules-source, random, or presentation values. Only the projector crosses the
boundary: it reads admitted authoritative inputs and constructs observation-specific copies.

```text
CampaignSnapshot ------------------+
                                    |
exact CampaignContentContext ------+--> CampaignSnapshotValidator.IsValid
                                    |                 |
observing LandSide ----------------+                 v
                                    +--> CampaignObservationProjector
                                                     |
                                                     v
                                        observation-only immutable values
                                                     |
                                                     v
                                        canonical serializer / strict reader
```

Dependency direction is one-way. No campaign command, event, projector, snapshot, or content type
depends on observations. There is no reverse projector.

## Public Core API

The proposed production surface is:

```csharp
namespace Cna.Core.Observations;

public enum CampaignObservationRejectionReason
{
    None,
    InvalidObserver,
    InvalidState,
}

public sealed record CampaignObservationProjectionResult
{
    public bool IsProjected { get; }
    public CampaignObservation? Observation { get; }
    public CampaignObservationRejectionReason RejectionReason { get; }

    public static CampaignObservationProjectionResult Projected(
        CampaignObservation observation);

    public static CampaignObservationProjectionResult Rejected(
        CampaignObservationRejectionReason reason);
}

public static class CampaignObservations
{
    public static CampaignObservationProjectionResult Query(
        CampaignAuthorityHandle handle,
        LandSide observer);
}

public static class CampaignObservationSerializer
{
    public static byte[] SerializeCanonical(CampaignObservation observation);

    public static CampaignObservation DeserializeCanonical(
        ReadOnlySpan<byte> canonicalBytes);
}
```

`CampaignObservations.Query` is the public, handle-based façade. The internal
`CampaignObservationProjector.Project(snapshot, context, observer)` performs the validated
authority-to-observation mapping for Core and its tests; snapshots and exact content context are
not public query inputs.

The contract-5 serializer adds a strict canonical reader. It accepts only exact contract-5 bytes,
constructs the same derived observation value, and requires reserialization to match the supplied
bytes exactly. This is compatibility/readback validation, not trusted history or persistence
admission: neither the reader nor a future transport adapter may turn an observation into authority.

## Observation value model

### Aggregate

```csharp
public sealed record CampaignObservation
{
    public const int CurrentContractVersion = 5;
    public const string CurrentPolicyId =
        "sandtable.observation.movement-side-safe.v1";

    internal CampaignObservation(
        int contractVersion,
        string policyId,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        string scenarioId,
        LandSide observer,
        CampaignObservationPosition position,
        CampaignObservationWeather? weather,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<ObservedOwnElement> ownElements,
        IReadOnlyList<ObservedApparentPresence> apparentOpposingPresences)
    {
        // Validate, defensively copy, and canonicalize as specified below.
    }
}
```

The internal constructor requires the current contract version and policy ID, a stable campaign
and scenario ID, positive state version, the exact `Cna1979Ruleset.Manifest.Hash`, defined observer,
non-null nested values, unique collection keys, and own/apparent locations present in the public
location set. It defensively copies and ordinally orders every collection, including apparent rows
by representation ID. Exact Content Pack
schema, format, ID, ruleset ID, and hash are intentionally absent: the complete pack hash changes
with opponent-only facts and would be a player-visible fingerprint.

All observation value constructors are internal. External callers can inspect, canonically
serialize, or strictly read a projected observation but cannot manufacture one or treat one as
authority. Tests use the existing `InternalsVisibleTo` relationship to exercise constructor
invariants directly.

### Public turn position

```csharp
public sealed record CampaignObservationPosition
{
    internal CampaignObservationPosition(
        string positionId,
        int gameTurn,
        int operationStage,
        string stageId,
        string phaseId,
        string? segmentId,
        string? stepId,
        LandActorRole actorRole,
        LandSide? activeSide,
        LandSide? initiativeHolder)
    {
        // Validate and assign source-free public turn facts.
    }
}
```

The value copies the public semantic fields from `LandSequencePosition` but omits contract version
and rules sources. The observation contract owns the nested shape. Game Turn is positive.
Operation Stage is 0 through 3: `0` is the mandatory pre-operation Initiative/Convoy sequence and
`1` through `3` are player-operation stages. All enums are defined. It preserves the existing sequence
invariants that an actor role of `None` has no active side and a `Commonwealth` actor resolves only
to Commonwealth. A nullable Initiative holder must be a defined side.

`ActorRole` is retained because it is public turn semantics and distinguishes relative roles such
as Initiative holder and first/second acting side. It is not rules provenance.

### Public topology

```csharp
public sealed record CampaignObservationLocation
{
    internal CampaignObservationLocation(
        string locationId,
        string terrainId)
    {
        // Validate and assign public topology facts.
    }
}

public sealed record CampaignObservationEdgeFeature
{
    internal CampaignObservationEdgeFeature(
        string featureId,
        string? directionFromLocationId)
    {
        // Validate and assign the public edge feature.
    }
}

public sealed record CampaignObservationEdge
{
    internal CampaignObservationEdge(
        string firstLocationId,
        string secondLocationId,
        IReadOnlyList<CampaignObservationEdgeFeature> features)
    {
        // Validate, defensively copy, and canonicalize the edge.
    }
}
```

An edge orders its two distinct endpoints ordinally, validates any direction endpoint against that
pair, copies features, rejects duplicate feature/direction pairs, and orders features by feature ID
then null/directed status then direction endpoint. The aggregate rejects duplicate endpoint pairs
and endpoints absent from `Locations`.

These values intentionally omit `ContentSourceCoordinate`, `ContentOrigin`, source indexes, and
presentation labels.

### Own elements and exact Movement risk

```csharp
public sealed record ObservedOwnElement
{
    internal ObservedOwnElement(
        string elementId,
        string parentFormationId,
        string organizationId,
        int baseCapabilityPointAllowance,
        string currentLocationId,
        CampaignObservationReserveStatus reserveStatus,
        string mobilityId,
        int ledgerGameTurn,
        int ledgerOperationStage,
        CapabilityPointAmount capabilityPointsExpended,
        int cohesionLevel,
        ObservedOwnVehicleBreakdownRisk? vehicleBreakdownRisk)
    {
        // Validate and assign own-force facts.
    }
}

public sealed record ObservedOwnVehicleBreakdownRisk
{
    internal ObservedOwnVehicleBreakdownRisk(
        string cohortId,
        string vehicleTypeId,
        string profileId,
        BreakdownPointAmount cumulativeBreakdownPoints,
        BreakdownPointAmount sandstormAttributedBreakdownPoints,
        string? highestEffectiveCheckedBandId,
        int workingPointCount,
        int brokenPointCount)
    {
        // Validate and copy exact owner-visible risk facts without provenance.
    }
}
```

Contract 5 exposes the approved static and mutable Movement facts on an independently placed own
combat element. Exact CP/BP values are normalized and the risk value preserves cohort/type/profile,
cumulative and Sandstorm-attributed BP, checked band, and working/broken counts. Current
non-motorized rows carry `null`. The aggregate rejects duplicate element IDs and locations absent
from topology.

This is deliberately more useful than exposing only element ID and location. A player must be able
to understand their own modeled force and risk. Adding them does not widen opponent visibility
because selection occurs only after full checkpoint admission and each row is copied into a
dedicated own-side type. No Content origin, rules provenance, or authoritative Campaign object is
retained.

### Apparent opposing presence

```csharp
public sealed record ObservedApparentPresence
{
    internal ObservedApparentPresence(
        string representationId,
        string currentLocationId,
        bool exertsZoc)
    {
        // Validate only the three-field apparent allowlist.
    }
}
```

The projector copies only opaque representation identity and current location from admitted
opposing representations; it never copies the real binding. All currently admitted synthetic rows
use `exertsZoc = false`. Organization and CPA are not substitutes for source-faithful ZOC
qualification; positive derivation requires `ZOR-TASK-002`.

## Canonical JSON contract

The exact top-level property order is:

```text
contractVersion
policyId
campaignId
stateVersion
rulesetHash
scenarioId
observer
position
weather
locations
edges
ownElements
apparentOpposingPresences
```

Nested order is fixed as follows:

```text
position: positionId, gameTurn, operationStage, stageId, phaseId,
          segmentId, stepId, actorRole, activeSide, initiativeHolder

weather: contractVersion, gameTurn, operationStage, season, kind, scope, affectedAreas

location: locationId, terrainId

edge: firstLocationId, secondLocationId, features

feature: featureId, directionFromLocationId

ownElement: elementId, parentFormationId, organizationId,
            baseCapabilityPointAllowance, currentLocationId, reserveStatus,
            mobilityId, ledgerGameTurn, ledgerOperationStage, capabilityPointsExpended,
            cohesionLevel, vehicleBreakdownRisk

capabilityPointsExpended: numerator, denominator

vehicleBreakdownRisk: cohortId, vehicleTypeId, profileId,
                      cumulativeBreakdownPoints,
                      sandstormAttributedBreakdownPoints,
                      highestEffectiveCheckedBandId,
                      workingPointCount, brokenPointCount

cumulativeBreakdownPoints: numerator, denominator
sandstormAttributedBreakdownPoints: numerator, denominator

apparentOpposingPresence: representationId, currentLocationId, exertsZoc
```

Enums use these exact lower-kebab values:

```text
LandSide: axis | commonwealth
LandActorRole: none | commonwealth | initiative-holder |
               first-acting-side | second-acting-side
```

Nullable fields are written explicitly as JSON `null`. Integers and booleans use canonical JSON
tokens; both exact amount types use normalized `numerator` then `denominator`. Output is compact
UTF-8 with no BOM or trailing newline. The executable contract-5 golden is the complete byte
authority and includes all nine locations, ten edges, exact owner rows, and the canonical apparent
rows; this design intentionally does not duplicate a partial JSON payload.

`DeserializeCanonical` requires one root object with exactly the fields above in that order. It
rejects legacy contract 4/policy bytes, duplicate/missing/extra/reordered fields, malformed enums,
IDs, topology references or values, hidden-field injection, and reducible/noncanonical exact
amounts. After construction it reserializes and requires exact input-byte equality.

## Projection flow and validation precedence

`CampaignObservationProjector.Project` executes exactly this flow:

1. Throw `ArgumentNullException` for null snapshot or context. These violate the non-null API
   contract and are programmer errors.
2. If `observer` is not a defined `LandSide`, return `InvalidObserver` with null observation.
3. Call `CampaignSnapshotValidator.IsValid(snapshot, context)`. Any ruleset, setup, content,
   scenario, world, random, Initiative, sequence, or state-version inconsistency returns
   `InvalidState` with null observation.
4. Map observer to the closed content side ID `axis` or `commonwealth`.
5. Build internal ordinal lookups from admitted world element/representation IDs to their current
   state and location.
6. Copy public topology from the exact artifact into observation-only values.
7. Select exact content elements whose side matches the observer and whose placement mode is
   independent; join each to its required admitted world row and copy mobility, Reserve, exact
   ledger, Cohesion, and nullable vehicle-risk facts into outward values.
8. Select admitted opposing representations through their internal bindings, then discard the
   binding and copy only representation ID, current location, and current false ZOC.
9. Copy campaign/state identity, canonical public ruleset hash, public scenario ID, policy ID, and
   public turn state into the aggregate. Do not copy Content Pack identity.
10. Return `Projected(observation)`.

The projector does not accept a resolver. It does not catch unexpected invariant exceptions and
turn them into successful or partially redacted output. Given an admitted context, a missing own
world row would indicate an internal invariant defect already contradicted by snapshot validation;
it fails closed rather than fabricating a row.

`InvalidState` intentionally combines exact-context mismatch with other checkpoint invalidity. The
projection boundary does not expose which hidden authority check failed. Invalid observer
precedence remains stable so an undefined enum is rejected before authority is inspected.

## Fog-of-war proof strategy

Simple negative substring tests are insufficient:

- public topology legitimately contains a location ID occupied by an opponent;
- organization vocabulary IDs may be shared by both sides; and
- exact Content Pack hash necessarily changes when hidden content changes and is therefore not an
  observation field.

The merge-blocking privacy proof therefore uses paired valid authorities with identical public
topology, viewer-owned facts, and apparent representation ID/location/ZOC rows while hidden
opponent bindings, IDs, static/operational/cohort/BP facts, origins, and complete Content/setup
identity differ. Tests require the entire semantic observation and canonical JSON to be equal
without excluding or normalizing a field. A separate fixture changes an approved apparent fact and
requires a visible delta confined to `apparentOpposingPresences`; this makes the guarantee
conditional and avoids falsely treating approved presence as a leak.

Targeted canary assertions remain useful for unique enemy element/formation IDs, numeric CPA,
source locators, seed/cursor, presentation labels, and prohibited property names. The suite tests
both the in-memory type graph and serialized bytes.

## Determinism and immutability

- All unordered inputs are copied and ordinally canonicalized in value constructors.
- The projector reads already-canonical content/world collections but does not rely on their input
  construction order.
- Structural equality and hash implementations enumerate the same canonical collection order.
- `SerializeCanonical` allocates and returns a new byte array for every call.
- `DeserializeCanonical` accepts only bytes whose strict parse and canonical reserialization are
  identical; it produces no authoritative Campaign value.
- Mutation tests change caller lists and returned bytes and then verify prior values and later
  serialization remain unchanged.
- Projection reads but never advances `RandomState`; before/after canonical snapshot bytes prove
  input isolation.
- Culture tests run the same golden under invariant, `fr-FR`, and `ar-SA` cultures.

## Security and authority boundaries

- Observation is an allowlist projection. No generic copy/redact or reflection traversal exists.
- The result is derived data and cannot create events or commands.
- The canonical public ruleset hash establishes rules identity and consistency, not authentication.
- Core accepts an observer side as a domain query input. A future host/API must authorize that the
  caller may observe that side; OBS-001 does not claim user authentication.
- No observation maps into the existing free-form protobuf `StrategicObservation.relevant_facts`.
  A future transport contract must use an independently reviewed typed allowlist.
- Core tests cannot prove future adapters never log authority. Logging/transport negative tests are
  required when those adapters exist.
- Player-visible source-complete contacts remain absent. Contract 5 exposes only opaque current
  representation/location/false-ZOC rows; knowledge history and positive ZOC must later derive from
  source-faithful rules and side-specific knowledge, never from leaked live bindings. The Umpire
  still adjudicates against authoritative world/representation truth.

## Decisions and rejected alternatives

| Decision | Rationale | Rejected alternative |
| --- | --- | --- |
| New `Cna.Core.Observations` namespace | Makes the fog boundary visible and keeps domain contracts separate | Add DTOs under `Campaigns`, which blurs authority and derived views |
| Dedicated copied values with internal construction | Enforces an allowlist by type and makes the projector the sole canonical factory | Reuse authority values or expose public constructors that can create incoherent canonical-looking observations |
| Own static facts included | Gives the player a useful own-force inspection surface | Only ID/location, which is safe but unnecessarily weak and immediately needs expansion |
| Contract-5 three-field apparent collection | Makes the approved current representation/location fact visible without exposing real bindings | Reuse authority representations or infer contact/positive-ZOC semantics |
| Public ruleset/scenario plus policy ID; no Content Pack identity | Identifies public campaign mode and redaction semantics without fingerprinting opponent-bearing content | Expose complete pack ID/hash or normalize it away in privacy tests |
| Strict canonical readback remains non-authoritative | Proves v5 compatibility and canonical equality without admitting history | Permissive DTO deserialization or treating observations as authority |
| `InvalidObserver` and `InvalidState` only | Stable small failure surface; context mismatch does not become an oracle | Separate context/hash/setup reasons at a derived privacy boundary |
| Metamorphic non-interference tests | Proves hidden changes do not affect approved visible data | Raw substring scans alone, which misclassify public/shared values and miss aggregates |
| No protobuf/host integration | Keeps the first contract pure and reviewable | Stringify observation into generic Intelligence facts, which defeats structural allowlisting |

## Current `MOV-TASK-005` clean cut

The Task 005 implementation versions the aggregate, owner row, serializer, golden identity, and
privacy fixtures together. That observation slice adds no Movement action or authority mutation.

| Work | Output | Verification | Status |
| --- | --- | --- | --- |
| Contract values | Contract 5/policy, exact own Movement/risk facts, three-field apparent rows | constructor, equality/order, cross-reference, prohibited-dependency tests | Complete |
| Projector/privacy | both-side owner facts, binding-free apparent rows, false-only ZOC | same-apparent hidden variation plus approved-visible-delta fixtures | Complete |
| Canonical codec | explicit v5 writer, strict non-authoritative reader, golden identity | round-trip and legacy/missing/extra/reordered/injected/noncanonical mutation matrix | Complete |
| Integration | unchanged authority/action identities and synchronized governing docs | focused tests, full gates, independent review | Complete |

`MOV-TASK-005` is complete. Task 006 now consumes its output through a dormant pure candidate
deriver; Task 007 is the next Movement task.

## Historical `OBS-001` implementation tasks

The tasks below record the original contract-1 delivery and later contract-4 remediation. Their
no-opponent/output-only statements are historical and are superseded for current contract 5 by the
clean cut above.

### `OBS-IMP-001` — Observation scalar and collection values

**Requirements:** `OBS-002`-`005`, `OBS-008`, `OBS-010`, `OBS-NFR-002`

**Deliverable:** Add public position, topology, and own-element values with internal constructor
validation, defensive copying, canonical order, structural equality, and no retained authoritative
objects.

**Likely files:**

- `src/Cna.Core/Observations/CampaignObservationPosition.cs`
- `src/Cna.Core/Observations/CampaignObservationTopology.cs`
- `src/Cna.Core/Observations/ObservedOwnElement.cs`
- `tests/Cna.Core.Tests/Observations/CampaignObservationContractTests.cs`

**Red evidence:** focused contract tests fail because the new types do not exist.

**Green evidence:** constructor rejection, copy/order, equality/hash, and prohibited-type tests pass.

**Dependencies:** Approved specification.

### `OBS-IMP-002` — Aggregate, result, and pure projector

**Requirements:** `OBS-001`-`008`, `OBS-011`, `OBS-012`, `OBS-NFR-003`, `OBS-NFR-004`

**Deliverable:** Add the aggregate, typed projection result/reasons, and projector using full
checkpoint admission and the closed observer-to-content-side mapping.

**Likely files:**

- `src/Cna.Core/Observations/CampaignObservation.cs`
- `src/Cna.Core/Observations/CampaignObservationProjectionResult.cs`
- `src/Cna.Core/Observations/CampaignObservationProjector.cs`
- `tests/Cna.Core.Tests/Observations/CampaignObservationProjectionTests.cs`

**Red evidence:** both-side baseline, pre/post-Initiative, invalid observer, context mismatch,
forged checkpoint, and input-isolation tests fail because projection does not exist.

**Green evidence:** all projection tests pass with no event, mutation, resolver, or partial output.

**Dependencies:** `OBS-IMP-001`.

### `OBS-IMP-003` — Adversarial privacy fixtures and non-interference

**Requirements:** `OBS-006`, `OBS-010`, `OBS-013`, `OBS-NFR-004`

**Deliverable:** Add paired valid artifact/snapshot fixtures and tests that vary only hidden
opponent facts and prove complete visible non-interference without excluding any observation field.

**Likely files:**

- `tests/Cna.Core.Tests/Observations/CampaignObservationTestData.cs`
- `tests/Cna.Core.Tests/Observations/CampaignObservationPrivacyTests.cs`

**Red evidence:** privacy tests either cannot project or expose an opponent-derived difference.

**Green evidence:** ID, formation, organization, CPA, count, and placement metamorphisms produce
identical complete observations/bytes; prohibited canaries and Content Pack identity remain absent.

**Dependencies:** `OBS-IMP-002`.

### `OBS-IMP-004` — Canonical output contract

**Requirements:** `OBS-002`-`006`, `OBS-008`-`010`, `OBS-NFR-001`

**Deliverable:** Add the explicit output-only canonical writer and freeze one complete reviewed
Axis golden payload.

**Likely files:**

- `src/Cna.Core/Observations/CampaignObservationSerializer.cs`
- `tests/Cna.Core.Tests/Observations/CampaignObservationSerializationTests.cs`

**Red evidence:** exact bytes, culture, order, byte-mutation, and prohibited-property tests fail
because no writer exists.

**Green evidence:** repeated canonical bytes match the full golden under all tested cultures and
contain only the property allowlist.

**Dependencies:** `OBS-IMP-002`; privacy assertions reconcile with `OBS-IMP-003`.

### `OBS-IMP-005` — Documentation, traceability, and integration gate

**Requirements:** `OBS-NFR-005` and completion boundary

**Deliverable:** Synchronize capability status and architecture maps, record executed evidence,
and run the full gate plus independent implementation review.

**Likely files:**

- `README.md`
- `tech-design.md`
- `naming-overview.md`
- `docs/roadmap/pre-alpha-roadmap.md`
- `docs/specs/campaign-observation-v1.md`
- `docs/design/campaign-observation-v1.md`

**Verification:** `just check`, `git diff --check`, reference searches, and a fresh-context review.

**Dependencies:** `OBS-IMP-001` through `OBS-IMP-004`.

## Historical `OBS-001` checkpoints and execution order

```text
OBS-IMP-001 values
        |
        v
OBS-IMP-002 projection
        |
        +----------------+
        |                |
        v                v
OBS-IMP-003 privacy   OBS-IMP-004 canonical writer
        |                |
        +-------+--------+
                v
OBS-IMP-005 docs, full gate, independent review
```

Implementation is sequential through the shared contract/projector seam. After `OBS-IMP-002`, the
privacy and serializer test work is logically parallel but should remain coordinated because both
inspect canonical observation shape. Every task uses red-green-refactor and leaves focused tests
green before the next task.

Checkpoint after `OBS-IMP-002`:

- both sides and both current checkpoints project;
- invalid inputs reject without output;
- no authoritative object is retained; and
- focused observation tests and Core build pass.

Checkpoint after `OBS-IMP-004`:

- metamorphic privacy suite passes;
- full golden/culture/order suite passes;
- existing Campaign/Content/Initiative tests remain green; and
- no transport or host reference was added.

## Historical `OBS-001` traceability

| Requirement | Decision/task | Executable evidence | Status |
| --- | --- | --- | --- |
| `OBS-001` | typed total result; `OBS-IMP-002` | null, invalid-observer, defined-rejection, success/failure exclusivity tests | Implemented |
| `OBS-002`, `OBS-003` | public identity/policy/position copies; `OBS-IMP-001`, `002`, `004` | baseline, complete-pack-identity absence, Initiative checkpoint, golden tests | Implemented |
| `OBS-004` | topology-specific values; `OBS-IMP-001`, `002` | exact 9-location/10-edge and prohibited-metadata tests | Implemented |
| `OBS-005` | own-element allowlist; `OBS-IMP-001`, `002` | Axis/Commonwealth exact-row tests | Implemented |
| `OBS-006` | absence plus non-interference; `OBS-IMP-003` | paired opponent ID/static/count/location metamorphisms for both observers | Implemented |
| `OBS-007` | full checkpoint validator and two reasons; `OBS-IMP-002` | precedence, context mismatch, forged checkpoint matrix | Implemented |
| `OBS-008` | copied canonical values; `OBS-IMP-001`, `002` | reverse-order, mutation, equality/hash tests | Implemented |
| `OBS-009` | explicit writer; `OBS-IMP-004` | exact golden, repeated bytes, culture tests | Implemented |
| `OBS-010` | dedicated type graph; `OBS-IMP-001`, `003`, `004` | recursive prohibited-type and JSON property-allowlist tests | Implemented |
| `OBS-011` | pure projector; `OBS-IMP-002` | input snapshot/content bytes and random cursor unchanged | Implemented |
| `OBS-012` | derived query only; all tasks | reference/API inspection; no reverse projector or command use | Implemented |
| `OBS-013` | no contact shape; `OBS-IMP-003`, docs | opponent-count metamorphism and explicit deferral search | Implemented |
| `OBS-NFR-001`-`005` | current stack, focused tests, `OBS-IMP-005` | dependency diff, build/analyzers/tests/format/diff gate | Implemented |

## Risks, mitigations, and maintenance costs

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Hidden association leaks while raw values look public | Invalid fog boundary | Complete paired-state non-interference, not substring-only tests |
| New outward contract accidentally retains authority | Future transport/log exposure | Dedicated value graph plus recursive prohibited-type test |
| Exact Content Pack hash fingerprints opponent-bearing content | Direct fog side channel | Keep complete pack schema/format/ID/hash server-side and prove total non-interference with no normalized field |
| Own static fields later change | Contract version pressure | Version/policy identifiers and explicit canonical writer |
| Current snapshot validator accepts only initial world/two checkpoints | Projection fails after movement work | Keep correct now; generalize authority validator before world-changing events |
| Free-form Intelligence protobuf tempts unsafe mapping | Hidden state becomes strings | Explicit no-mapping boundary and future typed transport review |
| Public observer argument is mistaken for authorization | User sees wrong side in host | Document Core/domain distinction; enforce campaign-side authorization in adapter |
| Duplicate scalar validation rules drift | Maintenance cost | Reuse existing internal stable-ID guard where safe; keep observation-specific hash/version checks focused |

## Deliberate deferrals

- public Movement candidate membership and command/event/adjudication (`MOV-TASK-007`/`008`);
  dormant Task 006 candidate/submission/receipt contracts are complete;
- user-to-side authorization and any HTTP/protobuf/Maproom adapter;
- observation persistence, caching, signing, or hashing;
- events/recent history, narrative, labels, notifications, and War Diary output;
- source-complete counter/stack/contact knowledge, attachments, and dummies;
- positive ZOC qualification (`ZOR-TASK-002`);
- Patrol/Reconnaissance and remembered knowledge;
- movement/contact world validation beyond the currently admitted initial world;
- spectator, administrator, and completed-replay visibility policies.

The current apparent rows satisfy only Task 005's Movement visibility prerequisite. They do not
replace later `PATROL-001` knowledge or `ZOR-TASK-002` qualification.

## Review challenge points

The independent reviewer should apply particular skepticism to:

1. whether every own-field inclusion is justified and no source/provenance object is retained;
2. whether `CampaignSnapshotValidator.IsValid` is sufficient admission for both current states;
3. whether aggregate constructors close duplicate/missing-location edge cases;
4. whether same-apparent metamorphic fixtures vary only intended hidden facts and a deliberately
   changed apparent fact produces only the approved visible delta;
5. whether complete Content Pack identity is absent everywhere and paired privacy tests compare
   every observation field without normalization;
6. whether canonical JSON property/collection order and strict readback reject every legacy or
   noncanonical form without implying authority;
7. whether validation precedence can inspect or expose authority before observer rejection;
8. whether any reference to the generic Intelligence protobuf undermines the allowlist boundary;
9. whether task sizes and dependencies genuinely allow red-green checkpoints; and
10. whether roadmap and top-level architecture updates remain synchronized at completion.

## Independent design-review remediation

The first fresh-context design review returned `Not ready`. All four findings are accepted:

| Finding | Resolution | Evidence to re-review |
| --- | --- | --- |
| P1: complete Content Pack identity is an opponent-dependent fingerprint | Removed the entire Content Pack identity from the observation contract and canonical JSON. Paired privacy tests now compare every field/byte without normalization. | `OBS-002`, `OBS-006`, `OBS-AC-001`, `OBS-AC-004`; JSON contract and `OBS-IMP-003` |
| P1: `OBS-013` restricted authoritative adjudication | Rewritten so the Umpire adjudicates from authoritative world/representation truth while only outward observations, legal-action presentation, diagnostics, logs, and adapters are side-safe. | `OBS-013` and research decision boundary |
| P2: public construction allowed incoherent canonical values | All observation constructors are internal, the projector is the sole canonical factory, the aggregate requires the current canonical ruleset hash, and internal invariant tests remain planned. | value-model API and `OBS-NFR-002` |
| P2: completion task omitted the governing specification | Added the specification to `OBS-IMP-005` and retained the requirement to synchronize both spec/design status and evidence. | `OBS-IMP-005` likely files and verification |
| P2 follow-up: `OBS-002`/`OBS-003` were reused as future capability names | Renamed the future research capabilities `CONTACT-001` and `KNOW-001`; current requirement IDs remain unambiguous. | reconnaissance/contact research sequence and task list |

The fresh-context re-review returned `Ready with non-blocking follow-ups`. Its identifier-collision
follow-up was accepted and future apparent-contact and historical-knowledge capabilities were
renamed `CONTACT-001` and `KNOW-001` before production implementation began.

## Historical independent implementation review

The evidence below covers the earlier Observation contracts. It does not claim that contract 5 or
`MOV-TASK-005` has passed its required current review.

The fresh-context implementation review returned `Ready with non-blocking follow-ups` after an
independent 205-test full gate. Both evidence findings were accepted: the aggregate permutation
test was expanded to multiple locations, edges, features, and own elements with exact canonical
byte comparison, and projection isolation added exact Content Pack byte checks. The same reviewer
independently reran 21 focused observation tests and the complete 205-test gate after remediation,
confirmed zero warnings, errors, failures, or skips, and returned a final `Ready` verdict with no
actionable findings.

A separate post-merge audit of PR #14 found two P2 contract-evidence gaps: the public rejection
factory accepted undefined enum values, and opponent-only non-interference was exercised only from
the Axis observer's perspective. The follow-up rejects `None` and every undefined reason and runs
the complete object, hash, and canonical-byte metamorphism for both Axis and Commonwealth
observers. The same reviewer independently verified the exact four-code-file remediation, 9
contract tests, 4 privacy tests, 23 focused observation tests, and the complete 207-test gate with
zero warnings, errors, failures, or skips, then returned `Ready` with no new findings.

## Design exit criteria

The design is implementation-ready only when the project owner accepts any material choices and a
fresh-context independent review returns `Ready` or `Ready with non-blocking follow-ups`. Any
blocking finding is corrected in the governing spec/design and reviewed again before production
code begins.
