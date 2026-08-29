# Campaign Observation v1 Specification

**Status:** Implemented; independent implementation review passed

**Date:** 2026-08-16

**Roadmap capability:** `OBS-001`

**Rules target:** `cna-1979.1`

**Predecessors:** [Content Pack v1](content-pack-v1.md),
[Campaign World v1](campaign-world-v1.md)

**Research decision:**
[Observation and Fog Boundary Spike](../research/observation-and-fog-boundary-spike.md)

**Current contract evolution:** The Campaign Observation v1 capability now emits contract 4 with
policy `sandtable.observation.own-elements-only.v2`. Contract 3 added owner Reserve status; contract
4 makes `stateVersion` audience-visible so valid hidden opposing Reserve histories have identical
complete bytes. The approved `BREAKDOWN-001` continuity boundary is now implemented by Movement
Foundation `MOV-TASK-004B`. `MOV-TASK-005` is the next clean cut: it will add own mobility and
operational-ledger facts, the approved minimum apparent opposing presence, and only the acting
side's minimum Breakdown Point/cohort-risk facts while keeping real bindings and other opposing
facts absent. Until Task 005 lands, contract 4 exposes no opposing presence. See
[Reserve Designation v1](reserve-designation-v1.md) and
[Movement Foundation v1](movement-foundation-v1.md).

## Objective

Project one immutable, deterministic, side-safe observation from an admitted Campaign World v1
snapshot and its exact resident Content Pack context. The observation gives a human player,
scripted policy, future Staff implementation, or Maproom adapter enough public campaign, topology,
and own-force information to inspect the current rules-laboratory position without receiving an
authoritative snapshot or any opposing-force truth.

Campaign Observation v1 is deliberately conservative. The source game represents opposing
knowledge through map counters, attached formations, Patrol and Reconnaissance disclosures, and
Dummy Tank Formations. Those mechanics do not yet exist in authoritative state. Version 1
therefore emits no opposing contacts, counts, identifiers, occupancy associations, or placeholders. A later
source-driven knowledge/contact capability must add such information before movement or contact
depends on observed opposing presence.

## User-visible demonstration

1. Create either supported rules-laboratory campaign and project an observation for Axis.
2. Inspect exact campaign/state identity, source-free turn position, public synthetic topology,
   and the two Axis elements with their own static and current-location facts.
3. Project the same snapshot for Commonwealth and inspect the equivalent Commonwealth facts.
4. Compare paired valid positions whose opponent-only force facts differ; the viewer's complete
   observation is unchanged and contains no
   opponent row, occupancy association, count, contact, or placeholder.
5. Reverse caller collection order and repeat the same projection; semantic equality and canonical
   bytes are unchanged.
6. Supply an invalid side, mismatched exact content context, or invalid campaign checkpoint and
   receive a typed rejection with no partial observation.

## Assumptions and accepted boundary

- The first observation is an internal `Cna.Core` value and canonical byte contract, not yet an
  HTTP, protobuf, persistence, Maproom, or Intelligence DTO.
- Stable topology IDs, terrain IDs, and edge facts are public. Presentation labels, source
  coordinates, origins, citations, and source expression are excluded.
- A side receives all currently modeled static and mutable facts for its independently placed own
  combat elements. Attachment-only elements are not separate world pieces and do not appear.
- Initiative holder and the source-free current sequence position are public campaign facts.
- Canonical ruleset identity and scenario ID are public. Exact Content Pack ID/hash remain
  server-side because the complete pack covers opponent-only facts; campaign ID plus state version
  provide the observation's future optimistic-concurrency binding.
- Random state, complete setup/world/content objects, and rules/source metadata remain
  authoritative-only.
- The observer is supplied independently of `CampaignSnapshot.ActiveSide`. Both currently valid
  checkpoints have no active side, and future host authorization—not Core projection—binds a user
  to an allowed campaign side.
- The exact Content Pack is already resident and resolved before projection. Projection performs
  no resolution or I/O.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `OBS-001` | Projection accepts one non-null Campaign World v1 snapshot, its already-resolved exact `CampaignContentContext`, and one defined `LandSide`. It returns either one complete observation or one typed rejection; it never returns a partial observation. Null snapshot/context inputs are programmer errors and throw `ArgumentNullException` rather than becoming domain rejections. |
| `OBS-002` | Every current observation records observation contract version 4, projection policy ID `sandtable.observation.own-elements-only.v2`, campaign ID, audience-visible state version, canonical ruleset hash, public scenario ID, and viewer side. Owners retain the exact authority revision; at hidden opposing Reserve/Movement checkpoints, the revision excludes opposing designation increments. Exact Content Pack schema, format, ID, ruleset ID, and hash remain server-side and never appear in an observation. |
| `OBS-003` | The observation records a source-free turn position containing position ID, Game Turn, Operation Stage, stage ID, phase ID, nullable segment and step IDs, actor role, active side, and initiative holder. It does not reuse `LandSequencePosition` because that authority value carries rules sources. |
| `OBS-004` | The observation contains every public Content Pack location as stable location ID plus terrain ID, and every public canonical edge as endpoint IDs plus feature ID and nullable direction endpoint. It excludes source coordinate, origin, source index, presentation, and scenario-placement metadata. |
| `OBS-005` | The observation contains exactly the independently placed combat elements whose Content Pack side matches the viewer. Each own element contains element ID, parent formation ID, organization ID, base capability-point allowance, and current location ID. Own elements are joined from exact static content and mutable world state. |
| `OBS-006` | No opponent element ID, formation ID, organization ID, capability value, occupancy association, force count, aggregate, revision delta, contact, dummy, placeholder, or other value derived from opposing force state appears in the observation object or canonical bytes. A location ID may independently appear in public topology; it must never be associated with an opposing element or occupancy fact. The capability represents opponent knowledge as absence, not metadata whose shape or revision leaks a count or type. |
| `OBS-007` | Projection verifies the supplied observer is defined and the complete snapshot—including exact content/scenario context agreement—is valid before reading hidden state into an output. Failures return stable `InvalidObserver` or `InvalidState` reasons in that precedence. Context mismatch is invalid authority state rather than a separately observable privacy oracle. |
| `OBS-008` | Observation values are constructible only through internal Core factories/projectors, defensively copy collection inputs, expose no mutable collection, implement structural equality and hash semantics, and canonically order locations by location ID, edges by endpoint IDs, edge features by feature/direction, and own elements by element ID using ordinal comparison. The aggregate enforces the canonical `cna-1979.1` ruleset hash and cross-collection references before serialization. |
| `OBS-009` | Canonical UTF-8 JSON uses an explicit writer, fixed property order, lower-kebab enum discriminants, canonical integer spelling, and no ambient serializer settings. Equal admitted snapshot, exact context, and side produce byte-identical output across runs, supported platforms, cultures, and input collection orders. |
| `OBS-010` | The observation type graph and serializer contain no `CampaignSnapshot`, `CampaignSetupSnapshot`, `CampaignWorldSnapshot`, `CampaignElementState`, `ContentPackArtifact`, `ContentPackDefinition`, `ContentScenario`, `ContentOrigin`, `ContentSourceCoordinate`, `RuleReference`, or `RandomStreamState` value. Only explicitly approved campaign, state, ruleset, scenario, policy, topology, and own-force scalars are allowed; complete Content Pack identity is prohibited. |
| `OBS-011` | Projection is synchronous and deterministic and performs no file, database, network, clock, random, model, service-container, mutable-catalog, or presentation lookup. It neither mutates input state nor consumes randomness. |
| `OBS-012` | Campaign Observation v1 remains a derived query result. It is not authoritative history, cannot be projected back into campaign state, and does not replace command validation against the current authoritative snapshot. |
| `OBS-013` | Opposing contacts remain explicitly unsupported until a source-driven counter/contact/knowledge capability defines map-piece representation, attachments, Patrol/Reconnaissance results, dummy identity, remembered knowledge, and staleness. The Umpire continues to adjudicate movement/contact against authoritative world and representation truth. Player-visible observations, legal-action presentation, rejection diagnostics, logs, and adapters must expose no opposing presence beyond the approved apparent-contact policy. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `OBS-NFR-001` | No new runtime package, service, database, generated artifact, host dependency, reflection serializer, or source-generated transport contract is introduced. |
| `OBS-NFR-002` | Internal value construction rejects nulls, invalid enums, invalid versions, unstable IDs, noncanonical ruleset identity, duplicate keys, invalid cross-references, and structurally invalid edge directions before an observation can exist. External callers can inspect and serialize observations but cannot construct canonical instances directly. |
| `OBS-NFR-003` | Failure reasons are stable machine-readable enum values. Diagnostic messages, if any, are presentation and do not determine authority. |
| `OBS-NFR-004` | Focused tests are small, single-process, deterministic, and use resident synthetic artifacts without file or network access. |
| `OBS-NFR-005` | Implementation passes analyzers, formatting, build, and the complete test suite with zero warnings and no skipped tests. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `OBS-AC-001` | Project the initial movement/contact laboratory snapshot for Axis | Success contains exact campaign/state, canonical ruleset, public scenario, and observation-policy identity; initial turn facts; all nine public locations and ten public edges; and only the two Axis element rows. Complete Content Pack identity is absent. |
| `OBS-AC-002` | Project the same snapshot for Commonwealth | Success contains the same public identity/topology and only the two Commonwealth element rows. |
| `OBS-AC-003` | Project before and after contested Initiative resolution | Initiative holder and source-free turn position match the admitted snapshot; random seed/cursor and rules sources are absent. |
| `OBS-AC-004` | Compare paired valid artifacts/snapshots that differ only in opposing IDs, force counts, static values, and placements | The viewer's entire semantic observation and canonical payload are byte-identical with no normalized or excluded field. No opposing identifiers, formation/organization/capability facts, occupancy associations, counts, contacts, or placeholders appear. Public topology remains identical, so a public location ID is not incorrectly treated as a leak by itself. |
| `OBS-AC-005` | Inspect the observation type graph and canonical JSON | Complete setup/world/content values, origins, source coordinates, source index, rule references, presentation text, random state, and authoritative input types are absent. |
| `OBS-AC-006` | Reverse locations, edges, features, elements, world rows, and equivalent caller construction order | Observation equality, hash behavior, semantic ordering, and canonical bytes remain unchanged. |
| `OBS-AC-007` | Mutate caller-owned collections and byte arrays after values or serialization are returned | Existing observations, equality/hash behavior, and prior/future canonical output do not change. |
| `OBS-AC-008` | Pass an undefined `LandSide`, then an exact-context mismatch, then an otherwise forged or invalid checkpoint | Projection returns respectively `InvalidObserver`, `InvalidState`, or `InvalidState`, with a null observation and observer validation taking precedence. |
| `OBS-AC-009` | Instrument the exact context and authoritative snapshot before and after repeated projection | Inputs are structurally unchanged, random cursor is unchanged, no catalog/resolver/Intelligence dependency is called, and repeated output bytes match. |
| `OBS-AC-010` | Serialize an accepted observation under non-default cultures and compare with a reviewed golden payload | Exact UTF-8 bytes match the contract's fixed property and collection order and use invariant canonical values. |
| `OBS-AC-011` | Search production project references and public observation members | Observation remains Core-only, no host/transport project exposes authoritative content/world types, and no observation member retains a prohibited authoritative object. |

## Canonical observation semantics

The top-level semantic groups are fixed for version 1:

1. contract version;
2. campaign and state identity;
3. canonical public ruleset, scenario, and observation-policy identity;
4. viewer side;
5. public source-free turn state;
6. public topology;
7. own independently placed elements.

Canonical property names and exact nested ordering are defined by the technical design before
implementation. Version 1 has no `opponents`, `contacts`, `unknowns`, `visibleEnemies`, or similar
collection. Adding one changes semantics and requires an explicit contract version and the
source-driven knowledge/contact predecessor.

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
  Observations/            side-safe values, result/rejection, projector, canonical writer

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
        string currentLocationId)
    {
        ElementId = RequireStableId(elementId, nameof(elementId));
        ParentFormationId = RequireStableId(parentFormationId, nameof(parentFormationId));
        OrganizationId = RequireStableId(organizationId, nameof(organizationId));
        ArgumentOutOfRangeException.ThrowIfLessThan(baseCapabilityPointAllowance, 1);
        BaseCapabilityPointAllowance = baseCapabilityPointAllowance;
        CurrentLocationId = RequireStableId(currentLocationId, nameof(currentLocationId));
    }

    public string ElementId { get; }
    public string ParentFormationId { get; }
    public string OrganizationId { get; }
    public int BaseCapabilityPointAllowance { get; }
    public string CurrentLocationId { get; }
}
```

Prefer sealed records/classes, explicit constructors, ordinal comparison, exact rejection enums,
nullable annotations, and `Utf8JsonWriter`. Do not introduce generic redaction, reflection-based
copying, mutable DTOs, automapper-style projection, or ambient serialization configuration.

## Testing strategy

- Follow red-green-refactor for every implementation checkpoint.
- Derive tests from acceptance scenarios, especially negative privacy and metamorphic tests.
- Build paired valid synthetic privacy fixtures that preserve public topology and own-side facts
  while changing only opponent IDs, counts, static values, and placements. Compare the complete
  complete semantic observation and canonical bytes without excluding or normalizing any field.
- Assert semantic and byte-level non-interference plus targeted absence of opponent-only sentinel
  values. A serializer test alone cannot prove that an unsafe object member is never consumed
  elsewhere, while an object test alone cannot prove a future writer does not add restricted data.
- Prefer exact real Core values and resident artifacts over mocks. Use controlled copied Content
  Packs for ordering and opponent-sentinel tests.
- Freeze one reviewed canonical observation golden payload after the contract is approved.
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
- Expose opposing contacts from live combat-element truth before the knowledge model exists. This
  does not restrict the Umpire from adjudicating against authoritative truth.
- Resolve latest/default content or perform I/O during projection.
- Consume random bytes, call Intelligence, mutate authority, or accept an observation as a command.
- Log or expose the authoritative input in a projection failure.

## Traceability

| Requirement group | Governing decision | Planned evidence |
| --- | --- | --- |
| `OBS-001`, `OBS-007`, `OBS-011`, `OBS-NFR-003` | `FOW-001`, `DET-001`, exact-artifact boundary | `OBS-AC-008`, `OBS-AC-009` |
| `OBS-002`, `OBS-003`, `OBS-009` | `DET-001`, versioned-contract policy | `OBS-AC-001`, `OBS-AC-003`, `OBS-AC-006`, `OBS-AC-010` |
| `OBS-004`, `OBS-005`, `OBS-008`, `OBS-NFR-002` | approved conservative observation decision | `OBS-AC-001`, `OBS-AC-002`, `OBS-AC-006`, `OBS-AC-007` |
| `OBS-006`, `OBS-010`, `OBS-013` | `FOW-001`; Patrol/Reconnaissance and Dummy source findings | `OBS-AC-004`, `OBS-AC-005`, `OBS-AC-011` |
| `OBS-012` | Command/Umpire authority hierarchy | API shape review and no reverse projector/command use |
| `OBS-NFR-001`, `OBS-NFR-004`, `OBS-NFR-005` | repository architecture and quality rules | project-reference inspection, focused tests, full repository gate |

## Explicit deferrals and open questions

Deferred from version 1:

- opposing counters, contacts, uncertainty, remembered or stale knowledge;
- attachment visibility and parent-counter representation;
- Patrol and Reconnaissance commands, costs, events, disclosures, and losses;
- Dummy Tank Formation identity and lifecycle;
- legal-action generation and optimistic-concurrency enforcement (`ACTION-001`);
- movement/contact adjudication (`MOVE-001`);
- HTTP/protobuf/Maproom/Intelligence DTOs, authorization, caching, notifications, and persistence;
- spectator, administrator, completed-game replay, and War Diary redaction policy.

The technical design must resolve these implementation-shape questions without changing the
approved semantics:

- whether public turn state is one nested value or flat fields;
- the smallest typed result wrapper consistent with current Core command/replay results; and
- exact canonical JSON property names and golden bytes.

## Success and exit criteria

The specification is implementation-ready when the project owner approves it, its technical
design has no blocking independent-review finding, every in-scope requirement maps to an ordered
implementation task and executable verification, and every deferral remains visible.

The implementation is complete only when `OBS-AC-001` through `OBS-AC-011` pass, the full
repository gate passes, governing documentation is synchronized, and a fresh implementation
review finds no blocking correctness, fog-of-war, authority, replay, or plan-traceability defect.
