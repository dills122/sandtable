# Campaign World v1 Specification

**Status:** Implemented; independent implementation review passed

**Date:** 2026-08-16

**Roadmap capability:** `WORLD-001`

**Rules target:** `cna-1979.1`

**Predecessor:** [Content Pack v1](content-pack-v1.md)

## Objective

Bind a campaign to one exact validated Content Pack and one scenario, then create the smallest
authoritative mutable world from that scenario's initial placements. A developer can create either
supported rules-laboratory campaign, inspect its exact content identity and element locations,
serialize the resulting event and snapshot canonically, and replay the history to byte-identical
state only when the same content artifact and rules executable are available.

This capability closes the gap between immutable scenario declarations and campaign-owned state.
It does not expose that state to a player, generate legal actions, advance beyond Naval Convoy, or
add persistence.

## Developer-visible demonstration

1. Select either recognized synthetic setup and submit a version-3 creation command containing its
   exact setup, ruleset, content-pack, and scenario identities.
2. Resolve the requested content by exact ID and hash before entering the authoritative decision.
3. Create one event containing the admitted identities and the scenario's initial mutable element
   positions, without embedding topology, force definitions, presentation data, or source scans.
4. Project a version-3 campaign snapshot whose world contains exactly the independently placed
   elements in canonical element-ID order.
5. Replay the event using the same immutable content bytes and current matching rules executable,
   obtaining byte-identical canonical state.
6. Repeat with missing content, the wrong hash, an unknown scenario, a setup/content mismatch, a
   nonmatching rules manifest, or forged placement history and observe a typed rejection with no
   partial campaign.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `WLD-001` | Campaign creation contract version 3 carries campaign ID, exact ruleset hash, seed, setup ID/hash, content-pack ID/hash, and scenario ID. No field means “latest,” “default,” or “current.” |
| `WLD-002` | Campaign setup schema version 2 binds one exact Content Pack identity and scenario ID while retaining setup-owned initial-turn, initiative-policy, synthetic-status, and source facts. Its hash covers every semantic field, including content and scenario selection. |
| `WLD-003` | Content resolution is an explicit pre-decision step keyed by exact pack ID and hash. Unknown ID and hash mismatch are distinct typed failures; neither can substitute another artifact. |
| `WLD-004` | Admission verifies command, setup, compiled rules manifest, exact artifact identity, scenario existence, and agreement between every duplicated identity or temporal fact. `ContentPackArtifact` proves that structural, capability, vocabulary, and ruleset-ID compatibility validation already passed; invalid content cannot acquire canonical bytes/hash or enter the exact resolver. |
| `WLD-005` | The authoritative engine and projector receive an already resolved immutable content context and perform no file, database, network, clock, model, service-container, or mutable-catalog lookup. |
| `WLD-006` | Creation projects one mutable element state for every scenario placement of an independently placed combat element. Each state contains only stable element ID and current location ID; static side, formation, organization, capability, terrain, and topology facts remain in exact content. |
| `WLD-007` | Initial world state is immutable, defensively copied, structurally comparable, canonically ordered by element ID, and rejects null, duplicate, unknown, missing, attachment-only, or invalid-location entries. |
| `WLD-008` | `CampaignCreated` contract version 3 records the embedded setup snapshot, exact initial world, random state, and initial sequence position. The event embeds neither the complete Content Pack nor presentation metadata. |
| `WLD-009` | The creation projector recomputes the embedded setup's deterministic hash, validates its agreement with exact replay context, derives the expected world, random invariants, and sequence position, and rejects every structurally or contextually inconsistent creation event. Setup/content hashes are identities, not event authentication; untrusted Chronicle ingestion requires authenticated provenance before projection. |
| `WLD-010` | `CampaignSnapshot` contract version 3 retains the setup/content selection and mutable world at every state. Snapshot admission validates the world against the exact content context before command execution or checkpoint use. |
| `WLD-011` | Replay first discovers the event's exact content request, resolves that immutable artifact outside projection, verifies the recorded ruleset is supported by the executable, and only then projects. Preparation returns stable `InvalidHistory`, `MissingContent`, `ContentHashMismatch`, or `UnsupportedRuleset` reasons; it never returns partial state. Invalid raw content bytes fail through the predecessor parse/validation contract before an exact resolver can exist. |
| `WLD-012` | Equal command, setup, content bytes, scenario, seed, and rules executable produce byte-identical creation-event and snapshot JSON across supported platforms, cultures, runs, and catalog insertion orders. |
| `WLD-013` | The current predetermined and contested initiative campaign paths remain executable. Each setup binds a synthetic scenario whose starting Game Turn equals the setup's initial Game Turn. |
| `WLD-014` | Contract-version-2 `CreateCampaign`, `CampaignCreated`, and `CampaignSnapshot` plus setup schema 1 are not silently upgraded by the version-3 executable. `InitiativeDetermined` remains event contract version 2 and is valid after version-3 creation because its serialized semantics do not change. Because no persisted user campaigns exist, the creation cutover is explicit; the prior Git revision is the historical executable for prior creation fixtures. |
| `WLD-015` | Campaign world contracts remain internal authoritative Core values and are not reused as Maproom, Intelligence, observation, legal-action, protobuf, or persistence DTOs. |
| `WLD-016` | Campaign creation still emits no advancement beyond Initiative Determination; Initiative resolution still stops at Naval Convoy. World initialization cannot bypass an unsupported mandatory mechanic. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `WLD-NFR-001` | Admission, decision, projection, validation, and serialization are deterministic and synchronous once exact artifacts are resident in memory. |
| `WLD-NFR-002` | Public collection-bearing values defensively copy inputs and implement structural equality/hash semantics without mutable collection exposure. |
| `WLD-NFR-003` | Canonical serializers use explicit writers/readers, fixed property order, ordinal ordering, lower-kebab identifiers, integer-only version fields, and strict unknown/missing/extra-property rejection. |
| `WLD-NFR-004` | Failure APIs carry stable machine-readable reasons; messages remain diagnostic presentation rather than authority. |
| `WLD-NFR-005` | No new runtime package, service, database, generated artifact, or host dependency is introduced. |
| `WLD-NFR-006` | The implementation passes analyzers, formatting, build, and all tests with zero warnings and no skipped tests. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `WLD-AC-001` | Create the predetermined rules-laboratory campaign with exact identities | One version-3 event is accepted; its setup selects `movement-contact-lab`; the world contains the four expected element/location pairs; state version is 1 at Initiative Determination. |
| `WLD-AC-002` | Create the contested initiative rules-laboratory campaign | Its setup selects a separate synthetic Game Turn 43 scenario; initial world is valid and contested Initiative resolution remains deterministic. |
| `WLD-AC-003` | Resolve an unknown content ID or the right ID with a wrong hash | Typed `UnknownContent` or `ContentHashMismatch`; the engine is not called and no event/state exists. |
| `WLD-AC-004` | Supply an unknown setup/scenario, wrong setup hash, content identity not selected by setup, scenario/start mismatch, or command rules-hash mismatch through controlled admission fixtures | Admission returns respectively `UnknownSetup`, `UnknownScenario`, `SetupHashMismatch`, `SetupContentMismatch`, `ScenarioStartMismatch`, or `UnsupportedRuleset`; no event/state exists. Separate predecessor tests prove unsupported capability, vocabulary, ruleset-ID, or structural content cannot produce an artifact/hash or resolver entry. |
| `WLD-AC-005` | Replay accepted creation with the exact artifact and executable | Projected snapshot bytes equal the originally projected canonical snapshot bytes. |
| `WLD-AC-006` | Replay with malformed creation history, missing content, a wrong expected hash, or a nonmatching executable rules manifest | `CampaignReplayPreparationResult` returns respectively `InvalidHistory`, `MissingContent`, `ContentHashMismatch`, or `UnsupportedRuleset` before a snapshot is admitted. Invalid raw content bytes are already rejected by Content Pack parsing/artifact creation and cannot enter the resolver. No mutable “latest” lookup is observed. |
| `WLD-AC-007` | Forge, remove, duplicate, or add an initial element state; change a location; include an attachment-only element | Projector rejects the creation event as invalid history even when its local JSON shape is valid. |
| `WLD-AC-008` | Reverse setup sources, content resolver entries, scenario placements, and caller world inputs | Admitted event/snapshot semantic equality and canonical bytes remain identical. |
| `WLD-AC-009` | Mutate caller-owned world collections or returned content bytes after admission | Admitted context, event, snapshot, future serialization, equality, and hash behavior do not change. |
| `WLD-AC-010` | Serialize and deserialize version-3 creation and snapshots | Exact golden JSON round-trips; missing, extra, reordered, duplicate, or invalid values reject. Contextual validity is rechecked before authority use. |
| `WLD-AC-011` | Inspect canonical creation event and snapshot JSON plus outward project references | Exact content identity, scenario ID, and current element locations are present; complete pack, labels, opponent observation data, and source expression are absent. |
| `WLD-AC-012` | Attempt generic sequence completion after world-aware creation or after Initiative resolution | It remains `UnsupportedTransition`; state, world, and random cursor are unchanged. |
| `WLD-AC-013` | Pass a structurally valid snapshot with a world or content context inconsistent with its setup | Engine returns `InvalidState`; checkpoint admission/projector rejects equivalent history. |
| `WLD-AC-014` | Run admission and replay under a plain unit test using a preloaded exact resolver | All work completes without host/container/files/network and no ambient dependency is consulted. |
| `WLD-AC-015` | Submit a malformed create request while a valid campaign snapshot already exists, using separate probes for snapshot-context and new-request resolution | The snapshot's exact context is resolved and validated first; result is `CampaignAlreadyCreated`; the new command is not inspected and its resolver probe is never called. An invalid or unresolvable prior snapshot instead returns `InvalidState`. |

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
  Content/                 exact preloaded artifact resolution; synthetic scenario additions
  Setups/                  schema-2 content/scenario-bound setup definitions and hashing
  Campaigns/               admission context, world values, command/event/snapshot/replay changes

tests/Cna.Core.Tests/
  Content/                 updated synthetic artifact/hash/golden evidence
  Setups/                  setup identity and content-selection hash evidence
  Campaigns/               admission, world, authority, replay, and canonical contract tests

docs/specs/                governing requirements and acceptance scenarios
docs/design/               contract and implementation design
docs/roadmap/              capability status and ordering
```

## Code style

Use explicit immutable technical values and constructor validation consistent with current Core
contracts. Unordered inputs are copied and ordinally canonicalized at construction.

```csharp
public sealed record CampaignElementState
{
    public CampaignElementState(string elementId, string currentLocationId)
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
    }

    public string ElementId { get; }

    public string CurrentLocationId { get; }
}
```

Prefer sealed records/classes, explicit constructors, `StringComparer.Ordinal`, exact discriminated
failure enums, nullable annotations, and explicit JSON writers. Do not introduce reflection-based
serialization, generic repositories, service locators, or mutable domain collections.

## Testing strategy

- Follow red-green-refactor for every implementation checkpoint.
- Test exact resolution and admission separately from command decision and replay projection.
- Derive authority tests from the acceptance scenarios, especially negative history and context
  mismatch cases; do not mirror private implementation branches.
- Freeze complete canonical version-3 creation-event and snapshot bytes plus the new synthetic
  Content Pack hash.
- Independently calculate at least one expected content/setup hash rather than asking production
  code for both sides of an assertion.
- Preserve focused Initiative tests for predetermined, contested, tie, stale, forged-event, and
  unsupported-transition behavior after the contract cutover.
- Run the complete repository gate after focused Core tests.

## Boundaries

Always:

- Resolve exact content before the authoritative decision and revalidate its identity in Core.
- Emit all authoritative initial world changes through `CampaignCreated`.
- Require exact replay context before admitting history or a snapshot checkpoint.
- Require exact scenario placement at every state reachable before a validated location-changing
  event exists.
- Keep static content separate from mutable campaign world state.
- Update canonical goldens and governing docs when any contract field changes.

Ask first:

- Support version-2 creation histories in the new executable or introduce a history converter.
- Add Archives/database persistence, Orleans activation, transport schemas, or a new dependency.
- Add source-derived or published scenario data.
- Add mutable element facts beyond current location.

Never:

- Resolve “latest,” fall back to a default pack/scenario, or silently repair an identity mismatch.
- Perform remote or persistence I/O inside `CampaignEngine` or `CampaignProjector`.
- Embed a complete Content Pack, presentation labels, scans, or copyrighted expression in campaign
  history.
- Expose `CampaignWorldSnapshot` as a player or Intelligence DTO.
- Advance past an unimplemented mandatory mechanic.

## Traceability

| Requirement group | Governing requirement | Planned evidence |
| --- | --- | --- |
| `WLD-001`-`WLD-005`, `WLD-011`, `WLD-NFR-001`, `WLD-NFR-004` | `DET-001`, `EVT-001`, `REL-001`, `CNT-017` | `WLD-AC-003`-`006`, `013`-`015` |
| `WLD-006`-`WLD-010`, `WLD-NFR-002`, `WLD-NFR-003` | `DET-001`, `EVT-001`, `REL-001` | `WLD-AC-001`, `005`, `007`-`011`, `013` |
| `WLD-002`, `WLD-004`, `WLD-013` | `FID-001`, `FID-002`, `SRC-001` | `WLD-AC-001`, `002`, `004` |
| `WLD-012`, `WLD-NFR-001`, `WLD-NFR-003`, `WLD-NFR-006` | `DET-001`, repository quality rules | `WLD-AC-005`, `008`, `010`, `014`, full gate |
| `WLD-014` | versioned-contract and historical-executable policy | version-2 creation/snapshot rejection, retained Initiative-v2 round trip, and documented cutover |
| `WLD-015` | `FOW-001` | `WLD-AC-011`; behavioral redaction remains `OBS-001` |
| `WLD-016` | `FID-002`, `EVT-001` | `WLD-AC-012` |

## Open questions and owner choices

No decision blocks implementation after approval of this specification. The deliberate pre-alpha
choices are:

- use one atomic `CampaignCreated` event rather than a second world-initialized event;
- embed only initial mutable element positions, while requiring exact content for static facts;
- add a second synthetic scenario to preserve the Game Turn 43 contested Initiative path; and
- cut over creation/setup/snapshot contracts without an in-runtime version-2 creation migration,
  while retaining the unchanged version-2 `InitiativeDetermined` event.

Hashes in these contracts provide deterministic identity and consistency checks, not authenticity.
Authenticated Chronicle provenance remains a prerequisite for any future untrusted event-ingestion
boundary.

Persistence of historical content bytes and historical executable packaging remains deferred to
Archives/hosting work. `OBS-001` will define the first side-safe outward projection.

## Success and exit criteria

The specification is implementation-ready when the project owner approves it, its technical
design has no blocking independent-review finding, every in-scope requirement maps to an ordered
implementation task and executable verification, and all explicit deferrals remain visible.

The implementation is complete only when `WLD-AC-001` through `WLD-AC-015` pass, the full
repository gate passes, canonical documentation is synchronized, and a fresh implementation
review reports no blocking correctness or plan gap.
