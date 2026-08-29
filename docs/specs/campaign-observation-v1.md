# Campaign Observation v1 Specification

**Status:** Contract 5 implemented, repository-verified, and independently reviewed

**Date:** 2026-08-16

**Roadmap capability:** `OBS-001`

**Rules target:** `cna-1979.1`

**Predecessors:** [Content Pack v1](content-pack-v1.md),
[Campaign World v1](campaign-world-v1.md)

**Research decision:**
[Observation and Fog Boundary Spike](../research/observation-and-fog-boundary-spike.md)

**Current contract evolution:** `MOV-TASK-005` advances Campaign Observation to contract 5 with
policy `sandtable.observation.movement-side-safe.v1`. Contract 3 added owner Reserve status;
contract 4 made `stateVersion` audience-visible so hidden opposing Reserve designation increments
did not change complete owner bytes. Contract 5 adds exact own mobility, Operation-Stage ledger,
Cohesion, and nullable vehicle Breakdown-risk facts plus the approved three-field apparent opposing
presence. It adds strict, non-authoritative canonical readback and preserves the real-binding and
hidden-force boundary. The contract, implementation, verification, and independent review are
complete. See
[Reserve Designation v1](reserve-designation-v1.md) and
[Movement Foundation v1](movement-foundation-v1.md).

## Objective

Project one immutable, deterministic, side-safe observation from an admitted Campaign World v1
snapshot and its exact resident Content Pack context. The observation gives a human player,
scripted policy, future Staff implementation, or Maproom adapter enough public campaign, topology,
exact own-force, and approved apparent-presence information to inspect the current rules-laboratory
position without receiving an authoritative snapshot, a real opposing binding, or hidden opposing
force state.

Campaign Observation remains deliberately conservative. Contract 5 exposes one apparent row for
each admitted opposing synthetic map representation, but only as its opaque representation ID,
current apparent location, and current supported ZOC boolean. It does not claim source-complete
counter knowledge, attachments, Patrol/Reconnaissance disclosures, remembered or stale contacts,
or Dummy Tank Formation semantics. The current synthetic fixture supports only `exertsZoc = false`;
positive qualification remains gated on `ZOR-TASK-002` source-faithful content and rules.

## User-visible demonstration

1. Create either supported rules-laboratory campaign and project an observation for Axis.
2. Inspect exact campaign/state identity, source-free turn position, public synthetic topology,
   and the two Axis elements with their own static, mobility, ledger, Cohesion, and nullable
   vehicle-risk facts.
3. Project the same snapshot for Commonwealth and inspect the equivalent Commonwealth facts.
4. Inspect the two apparent opposing rows. Each contains only an opaque representation ID, current
   apparent location, and `exertsZoc = false`; it carries no real binding or hidden force facts.
5. Compare paired valid positions whose hidden opponent facts differ while their approved apparent
   facts remain identical; the viewer's complete observation and canonical bytes are unchanged.
   Deliberately change an approved apparent fact and observe a delta confined to that collection.
6. Strictly read the canonical contract-5 bytes and obtain the same observation; mutate version,
   policy, fields, order, or exact-amount spelling and receive a rejection.
7. Reverse caller collection order and repeat the same projection; semantic equality and canonical
   bytes are unchanged.
8. Supply an invalid side, mismatched exact content context, or invalid campaign checkpoint and
   receive a typed rejection with no partial observation.

## Assumptions and accepted boundary

- The first observation is an internal `Cna.Core` value and canonical byte contract, not yet an
  HTTP, protobuf, persistence, Maproom, or Intelligence DTO.
- Stable topology IDs, terrain IDs, and edge facts are public. Presentation labels, source
  coordinates, origins, citations, and source expression are excluded.
- A side receives all explicitly approved static and mutable observation facts for its independently
  placed own combat elements. Attachment-only elements are not separate world pieces and do not
  appear.
- A side receives only the approved opaque ID, apparent location, and supported ZOC boolean for an
  opposing representation. This is deliberately less than a real binding or contact history.
- Initiative holder and the source-free current sequence position are public campaign facts.
- Canonical ruleset identity and scenario ID are public. Exact Content Pack ID/hash remain
  server-side because the complete pack covers opponent-only facts; campaign ID plus state version
  provide the observation's future optimistic-concurrency binding.
- Random state, complete setup/world/content objects, and rules/source metadata remain
  authoritative-only.
- The observer is supplied independently of `CampaignSnapshot.ActiveSide`; future host
  authorization—not Core projection—binds a user to an allowed campaign side.
- The exact Content Pack is already resident and resolved before projection. Projection performs
  no resolution or I/O.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `OBS-001` | Projection accepts one non-null Campaign World v1 snapshot, its already-resolved exact `CampaignContentContext`, and one defined `LandSide`. It returns either one complete observation or one typed rejection; it never returns a partial observation. Null snapshot/context inputs are programmer errors and throw `ArgumentNullException` rather than becoming domain rejections. |
| `OBS-002` | Every current observation records observation contract version 5, projection policy ID `sandtable.observation.movement-side-safe.v1`, campaign ID, audience-visible state version, canonical ruleset hash, public scenario ID, and viewer side. Owners retain the exact authority revision; at hidden opposing Reserve/Movement checkpoints, the revision excludes opposing designation increments. Exact Content Pack schema, format, ID, ruleset ID, and hash remain server-side and never appear in an observation. |
| `OBS-003` | The observation records a source-free turn position containing position ID, Game Turn, Operation Stage, stage ID, phase ID, nullable segment and step IDs, actor role, active side, and initiative holder. It does not reuse `LandSequencePosition` because that authority value carries rules sources. |
| `OBS-004` | The observation contains every public Content Pack location as stable location ID plus terrain ID, and every public canonical edge as endpoint IDs plus feature ID and nullable direction endpoint. It excludes source coordinate, origin, source index, presentation, and scenario-placement metadata. |
| `OBS-005` | The observation contains exactly the independently placed combat elements whose Content Pack side matches the viewer. Each own element contains element ID, parent formation ID, organization ID, mobility ID, base capability-point allowance, current location ID, Reserve status, ledger Game Turn, ledger Operation Stage, exact capability points expended, Cohesion level, and nullable `ObservedOwnVehicleBreakdownRisk`. A non-null risk contains cohort ID, vehicle type ID, profile ID, exact cumulative Breakdown Points, exact Sandstorm-attributed Breakdown Points, nullable highest effective checked-band ID, and working/broken point counts. It retains no Content origin, rules provenance, or authoritative Campaign object. |
| `OBS-006` | The observation contains canonical `apparentOpposingPresences` ordered by opaque representation ID. Each `ObservedApparentPresence` contains exactly representation ID, current apparent location ID, and `exertsZoc`. The current admitted synthetic projection requires `exertsZoc = false`; positive qualification is deferred to `ZOR-TASK-002`. No real opponent element ID or binding, formation, organization, CPA, Reserve, expenditure, Cohesion, mobility, cohort, Breakdown amount/check history, Content identity, raw event truth, or unsupported stack/face fact appears in the value graph or bytes. Privacy is conditional on the approved apparent facts: authorities with identical apparent ID/location/ZOC rows and viewer-visible facts must produce byte-identical observations, while a changed approved apparent fact must produce an outward delta confined to `apparentOpposingPresences`. |
| `OBS-007` | Projection verifies the supplied observer is defined and the complete snapshot—including exact content/scenario context agreement—is valid before reading hidden state into an output. Failures return stable `InvalidObserver` or `InvalidState` reasons in that precedence. Context mismatch is invalid authority state rather than a separately observable privacy oracle. |
| `OBS-008` | Observation values are constructible only through internal Core factories/projectors, defensively copy collection inputs, expose no mutable collection, implement structural equality and hash semantics, and canonically order locations by location ID, edges by endpoint IDs, edge features by feature/direction, own elements by element ID, and apparent opposing presences by representation ID using ordinal comparison. The aggregate enforces the canonical `cna-1979.1` ruleset hash and all topology/own/apparent cross-references before serialization. |
| `OBS-009` | Canonical UTF-8 JSON uses an explicit writer, fixed property order, lower-kebab enum discriminants, canonical integer and exact-amount spelling, and no ambient serializer settings. `CampaignObservationSerializer.DeserializeCanonical` accepts only the exact contract-5 shape and proves byte-for-byte canonical equality by reserialization. Legacy version 4, missing, extra, duplicate, or reordered fields, malformed values, hidden-field injection, and noncanonical amount forms reject. Readback creates only the non-authoritative observation value; it cannot admit, recreate, or mutate Campaign authority. |
| `OBS-010` | The observation type graph and serializer contain no `CampaignSnapshot`, `CampaignSetupSnapshot`, `CampaignWorldSnapshot`, `CampaignElementState`, `CampaignMapRepresentationState`, `CampaignVehicleBreakdownState`, `ContentPackArtifact`, `ContentPackDefinition`, `ContentScenario`, `ContentOrigin`, `ContentSourceCoordinate`, `RuleReference`, or `RandomStreamState` value. Only explicitly approved campaign, state, ruleset, scenario, policy, topology, own-force, and apparent-presence scalars/observation values are allowed; complete Content Pack identity and authoritative real bindings are prohibited. |
| `OBS-011` | Projection is synchronous and deterministic and performs no file, database, network, clock, random, model, service-container, mutable-catalog, or presentation lookup. It neither mutates input state nor consumes randomness. |
| `OBS-012` | Campaign Observation v1 remains a derived query result. It is not authoritative history, cannot be projected back into campaign state, and does not replace command validation against the current authoritative snapshot. |
| `OBS-013` | Contract 5's apparent opposing rows are current-position representation facts, not source-complete contacts, real occupancy bindings, remembered knowledge, reconnaissance results, or proof of ZOC qualification. Attachments, Patrol/Reconnaissance, dummy identity, staleness, and positive ZOC require later source-driven capabilities. The Umpire continues to adjudicate movement/contact against authoritative world and representation truth. Player-visible observations, legal-action presentation, rejection diagnostics, logs, and adapters must expose no opposing fact beyond the approved three-field apparent policy. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `OBS-NFR-001` | No new runtime package, service, database, generated artifact, host dependency, reflection serializer, or source-generated transport contract is introduced. |
| `OBS-NFR-002` | Internal value construction rejects nulls, invalid enums, invalid versions, unstable IDs, noncanonical ruleset identity, duplicate keys, invalid cross-references, and structurally invalid edge directions before an observation can exist. External callers can inspect, serialize, and strictly read canonical observations but cannot directly construct or treat them as authority. |
| `OBS-NFR-003` | Failure reasons are stable machine-readable enum values. Diagnostic messages, if any, are presentation and do not determine authority. |
| `OBS-NFR-004` | Focused tests are small, single-process, deterministic, and use resident synthetic artifacts without file or network access. |
| `OBS-NFR-005` | Implementation passes analyzers, formatting, build, and the complete test suite with zero warnings and no skipped tests. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `OBS-AC-001` | Project the initial movement/contact laboratory snapshot for Axis | Success contains exact campaign/state, canonical ruleset, public scenario, contract-5 policy identity, initial turn facts, all nine public locations and ten public edges, the two exact Axis owner rows, and two canonical apparent Commonwealth representation rows with false ZOC. Complete Content Pack identity and real opposing bindings are absent. |
| `OBS-AC-002` | Project the same snapshot for Commonwealth | Success contains the same public identity/topology, the two exact Commonwealth owner rows, and only the three-field apparent Axis representation rows. |
| `OBS-AC-003` | Project before and after contested Initiative resolution | Initiative holder and source-free turn position match the admitted snapshot; random seed/cursor and rules sources are absent. |
| `OBS-AC-004` | Compare paired valid authorities that preserve viewer-visible facts and apparent representation ID/location/ZOC rows while changing hidden opponent IDs, bindings, organization, CPA, mobility, cohort/profile/BP/check data, origins, and complete Content/setup identity | The viewer's entire semantic observation and canonical payload are byte-identical with no normalized or excluded field. Hidden canaries are absent. A separate deliberate apparent-location/count change produces a visible difference confined to `apparentOpposingPresences`, proving the privacy guarantee is conditional rather than suppressing approved presence. |
| `OBS-AC-005` | Inspect the observation type graph and canonical JSON | Complete setup/world/content values, real representation bindings, authoritative vehicle state, origins, source coordinates/index, rule references, presentation text, random state, and authoritative input types are absent. Owner mobility/ledger/Cohesion/risk and the three apparent-presence scalars are exact and complete. |
| `OBS-AC-006` | Reverse locations, edges, features, elements, world rows, and equivalent caller construction order | Observation equality, hash behavior, semantic ordering, and canonical bytes remain unchanged. |
| `OBS-AC-007` | Mutate caller-owned collections and byte arrays after values or serialization are returned | Existing observations, equality/hash behavior, and prior/future canonical output do not change. |
| `OBS-AC-008` | Pass an undefined `LandSide`, then an exact-context mismatch, then an otherwise forged or invalid checkpoint | Projection returns respectively `InvalidObserver`, `InvalidState`, or `InvalidState`, with a null observation and observer validation taking precedence. |
| `OBS-AC-009` | Instrument the exact context and authoritative snapshot before and after repeated projection | Inputs are structurally unchanged, random cursor is unchanged, no catalog/resolver/Intelligence dependency is called, and repeated output bytes match. |
| `OBS-AC-010` | Serialize an accepted observation under non-default cultures, compare with a reviewed golden payload, and strictly read it back | Exact UTF-8 bytes match the contract's fixed property and collection order and use invariant canonical values. Readback equals the source and reserializes identically; version-4, field mutation/order/injection, malformed-value, and noncanonical-amount matrices reject. |
| `OBS-AC-011` | Search production project references and public observation members | Observation remains Core-only, no host/transport project exposes authoritative content/world types, and no observation member retains a prohibited authoritative object. |

## Canonical observation semantics

The top-level semantic groups are fixed for current contract 5:

1. contract version;
2. campaign and state identity;
3. canonical public ruleset, scenario, and observation-policy identity;
4. viewer side;
5. public source-free turn state;
6. public topology;
7. own independently placed elements, including exact Movement ledger and optional vehicle risk;
8. apparent opposing presences.

Canonical property names and exact nested ordering are defined by the technical design and frozen
by executable golden evidence. `apparentOpposingPresences` is the only opponent-facing collection;
aliases such as `opponents`, `contacts`, `unknowns`, or `visibleEnemies` are unsupported. Adding
fields or a second collection changes semantics and requires an explicit contract/policy version
and the applicable source-driven predecessor.

## Tech stack and commands

No stack or dependency change is planned. Implementation remains C# on .NET 10 in `Cna.Core`,
with xUnit v3 tests running through Microsoft.Testing.Platform.

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

`just check` may be used as the equivalent full local gate.

## Repository structure

```text
src/Cna.Core/
  Campaigns/               admitted authoritative snapshots and exact content context
  Observations/            side-safe values, result/rejection, projector, canonical writer/reader

tests/Cna.Core.Tests/
  Observations/            projection, privacy, canonical-byte, and contract-boundary tests

docs/research/             source and decision evidence
docs/specs/                governing requirements and acceptance scenarios
docs/design/               contract, flow, tasks, and traceability
docs/roadmap/              capability ordering and completion status
```

## Code style

Use explicit immutable values and dedicated outward-safe types. Canonicalize unordered inputs in
constructors and never retain authoritative objects merely because they are already immutable.

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
        // Validate and copy only the contract-5 owner allowlist.
    }
}

public sealed record ObservedApparentPresence
{
    internal ObservedApparentPresence(
        string representationId,
        string currentLocationId,
        bool exertsZoc)
    {
        // Validate only the opaque identity, public location, and supported boolean.
    }
}
```

Prefer sealed records/classes, explicit constructors, ordinal comparison, exact rejection enums,
nullable annotations, and `Utf8JsonWriter`. Do not introduce generic redaction, reflection-based
copying, mutable DTOs, automapper-style projection, or ambient serialization configuration.

## Testing strategy

- Follow red-green-refactor for every implementation checkpoint.
- Derive tests from acceptance scenarios, especially negative privacy and metamorphic tests.
- Build paired valid synthetic privacy fixtures that preserve public topology, own-side facts, and
  the complete approved apparent ID/location/ZOC rows while changing only hidden opposing facts.
  Compare the complete semantic observation and canonical bytes without excluding or normalizing
  any field. Separately vary an approved apparent fact and require a delta confined to the apparent
  collection.
- Assert semantic and byte-level non-interference plus targeted absence of opponent-only sentinel
  values. A serializer test alone cannot prove that an unsafe object member is never consumed
  elsewhere, while an object test alone cannot prove a future writer does not add restricted data.
- Prefer exact real Core values and resident artifacts over mocks. Use controlled copied Content
  Packs for ordering and opponent-sentinel tests.
- Freeze one reviewed canonical observation golden payload after the contract is approved, then
  round-trip it through the strict reader and reject every noncanonical mutation class.
- Preserve existing Campaign World, Content Pack, Initiative, replay, and authority tests.
- Run the complete repository gate after focused observation tests.

## Boundaries

Always:

- Validate viewer, exact context agreement, and checkpoint before constructing output.
- Copy only explicitly approved scalar/value facts into dedicated observation values.
- Keep campaign ID, state version, canonical ruleset hash, and public scenario ID so later actions
  can bind to the observed state without exposing complete Content Pack identity.
- Test both sides and use negative assertions against unique opponent-only values.
- Keep canonical serialization independent of process culture and ambient options.
- Treat strict readback as validation of derived contract bytes only, never as authority admission.
- Update this specification, its design, the roadmap, `README.md`, `tech-design.md`, and
  `naming-overview.md` when the retained contract or architecture changes.

Ask first:

- Add any opponent contact, remembered knowledge, uncertainty, dummy, or reconnaissance result.
- Add presentation labels, player-private notes, spectator views, or replay-redaction policy.
- Add transport, persistence, caching, hashing/signing, or host/service integration.
- Generalize Campaign World validity beyond currently implemented initial placement.
- Change public/private status of topology or turn fields.

Never:

- Reuse an authoritative snapshot, world, setup, complete Content Pack, or sequence position as an
  observation/transport value.
- Map the observation into the existing free-form Intelligence `relevant_facts` strings; a future
  typed, allowlisted transport mapping requires its own contract review.
- Copy a complete object and redact fields afterward.
- Expose real opposing bindings or derive unsupported contact/ZOC/knowledge facts from live combat
  elements. The approved three-field apparent representation is the complete current exception and
  does not restrict the Umpire from adjudicating against authoritative truth.
- Resolve latest/default content or perform I/O during projection.
- Consume random bytes, call Intelligence, mutate authority, or accept an observation as a command.
- Log or expose the authoritative input in a projection failure.

## Traceability

| Requirement group | Governing decision | Planned evidence |
| --- | --- | --- |
| `OBS-001`, `OBS-007`, `OBS-011`, `OBS-NFR-003` | `FOW-001`, `DET-001`, exact-artifact boundary | `OBS-AC-008`, `OBS-AC-009` |
| `OBS-002`, `OBS-003`, `OBS-009` | `DET-001`, versioned-contract policy | `OBS-AC-001`, `OBS-AC-003`, `OBS-AC-006`, `OBS-AC-010` |
| `OBS-004`, `OBS-005`, `OBS-008`, `OBS-NFR-002` | approved owner Movement/risk allowlist | `OBS-AC-001`, `OBS-AC-002`, `OBS-AC-006`, `OBS-AC-007` |
| `OBS-006`, `OBS-010`, `OBS-013` | `FOW-001`; `MOV-DEC-002`, `MOV-DEC-006`; false-only current ZOC | `OBS-AC-004`, `OBS-AC-005`, `OBS-AC-011` |
| `OBS-012` | Command/Umpire authority hierarchy | API shape review and no reverse projector/command use |
| `OBS-NFR-001`, `OBS-NFR-004`, `OBS-NFR-005` | repository architecture and quality rules | project-reference inspection, focused tests, full repository gate |

## Explicit deferrals and open questions

Still deferred after current contract 5:

- source-complete opposing counters/contacts, uncertainty, and remembered or stale knowledge;
- attachment visibility and parent-counter representation;
- Patrol and Reconnaissance commands, costs, events, disclosures, and losses;
- Dummy Tank Formation identity and lifecycle;
- positive `exertsZoc` derivation until `ZOR-TASK-002` supplies qualifying rules/content;
- Movement candidate/submission contracts (`MOV-TASK-006`) and Movement adjudication (`MOVE-001`);
- HTTP/protobuf/Maproom/Intelligence DTOs, authorization, caching, notifications, and persistence;
- spectator, administrator, completed-game replay, and War Diary redaction policy.

Historical version-1 questions about nested public turn state, the result wrapper, and canonical
property names are resolved by the implemented contracts. Any contract-5 implementation choice
must preserve the frozen semantics above; verification and review remain active.

## Success and exit criteria

The specification is implementation-ready when the project owner approves it, its technical
design has no blocking independent-review finding, every in-scope requirement maps to an ordered
implementation task and executable verification, and every deferral remains visible.

The implementation is complete only when `OBS-AC-001` through `OBS-AC-011` pass, the full
repository gate passes, governing documentation is synchronized, and a fresh implementation
review finds no blocking correctness, fog-of-war, authority, replay, or plan-traceability defect.
