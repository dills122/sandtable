# Campaign World v1 Technical Design

**Status:** Implemented; independent implementation review passed

**Date:** 2026-08-16

**Capability:** `WORLD-001`

**Specification:** [Campaign World v1](../specs/campaign-world-v1.md)

## Decision summary

Campaign World v1 introduces one exact-artifact boundary and one atomic creation projection:

```text
version-3 CreateCampaign
        |
        v
resolve exact ID + hash outside the authoritative turn
        |
        v
CampaignContentContext (validated immutable artifact + selected scenario)
        |
        v
admit setup + rules + content + scenario as one consistent request
        |
        v
CampaignEngine emits CampaignCreated v3 with initial mutable positions
        |
        v
CampaignProjector validates against the same exact context
        |
        v
CampaignSnapshot v3 with CampaignWorldSnapshot v1
```

The event records the resulting mutable element positions and exact setup/content selection. It
does not copy topology, terrain, formations, element definitions, presentation labels, or source
expression. Those static facts remain available only through the immutable artifact named by the
event's exact hash.

## Authority and dependency boundaries

### Resolution is outside the Umpire turn

An exact resolver accepts only `(packId, expectedHash)`. It returns the matching validated
`ContentPackArtifact` or a typed `UnknownContent`/`ContentHashMismatch` result. Its contract has no
API for a default or latest artifact.

The local pre-alpha implementation resolves from the in-memory synthetic catalog. A future
Archives adapter may load canonical bytes from persistence, but must finish that I/O, parse and
validate the bytes, and build the immutable artifact before opening an Orleans grain turn. The
authoritative APIs receive only a resident context and remain synchronous and pure.

### Core revalidates admission

Resolution is not authority. Core constructs `CampaignContentContext` only after verifying:

- artifact schema, format, ID, hash, and ruleset ID;
- selected scenario existence and required v1 capabilities;
- scenario start agreement with the setup initial Game Turn; and
- exact agreement among command, setup selection, artifact, scenario, and compiled rules manifest.

`ContentPackArtifact` construction is itself proof that complete structural, capability,
vocabulary, and `cna-1979.1` compatibility validation succeeded. Its factory runs those validators
before canonical bytes or a hash exist. Campaign admission must not define a failure for an
incompatible artifact because such an artifact is unrepresentable. A future Archives loader maps
parse/validation failure while preparing canonical bytes, before it constructs a preloaded exact
resolver or opens an authoritative turn.

The engine then validates that the supplied context matches the command and any prior snapshot.
This makes a buggy or compromised host unable to swap already resident content silently.

### Replay is context-dependent by design

Chronicle history is authoritative but is not self-contained static content. Replay reads the
creation event's exact content request, resolves the matching immutable artifact before projection,
and supplies that context to every projector call. Missing content or an unsupported rules hash
fails before a checkpoint is admitted.

This is intentional: copying the complete pack into creation history would duplicate a potentially
large artifact, blur static and mutable ownership, and still not provide the historical executable
needed to interpret the ruleset hash.

### Hashes identify; the Chronicle boundary authenticates

Ruleset, setup, and content hashes detect semantic differences and bind exact dependencies. They
are unkeyed hashes and do not authenticate who emitted an event. The projector rejects malformed,
structurally inconsistent, and contextually inconsistent creation history, but a fully
self-consistent attacker-authored setup/event cannot be distinguished without trusted event
provenance.

The current projector remains a trusted-history contract. Before persistence, transport, or
another process can supply Chronicle events, that ingestion boundary must authenticate event
origin/integrity and select the exact supported executable/content context. WORLD-001 does not
claim to implement that boundary and must not describe deterministic hash validation as event
authentication.

## Setup and synthetic fixture migration

`CampaignSetupDefinition` moves from schema 1 to schema 2 and gains one immutable content
selection. The setup hash adds the selection after `initialInitiative` and before `sources`:

```text
schemaVersion
setupId
isSynthetic
initialGameTurn
initialInitiative
content {
  schemaVersion
  formatId
  packId
  rulesetId
  hash
  scenarioId
}
sources
```

`DisplayName` remains excluded. Every content-selection field participates because changing any
one changes what campaign can be created.

The existing `movement-contact-lab` scenario remains the Game Turn 1 selection for the
predetermined setup. The synthetic pack adds a second original scenario,
`initiative-contested-lab`, with the same laboratory topology/forces and a Game Turn 43 start for
the contested setup. Its placements receive separate stable synthetic origin locators. Adding the
scenario deliberately changes the Content Pack canonical bytes/hash; both schema-2 setup hashes
then bind the new exact artifact.

This preserves both Sprint 1 initiative demonstrations without pretending a Game Turn 1 scenario
starts at Game Turn 43. It is test content, not a published scenario claim.

## Proposed contracts

Names define ownership and serialized semantics. Small mechanical C# adjustments are acceptable
if the requirement IDs and canonical shapes remain unchanged.

### Content selection and resident context

```csharp
public sealed record CampaignContentSelection(
    ContentPackIdentity Pack,
    string ScenarioId);

public sealed class CampaignContentContext
{
    public ContentPackArtifact Artifact { get; }

    public ContentScenario Scenario { get; }
}
```

`CampaignContentSelection` is an immutable value and is serialized as the nested `content` object.
`CampaignContentContext` is runtime-only: it is never serialized, never sent to a player, and never
stored as mutable campaign state. Construction defensively requires that `Scenario` is the exact
scenario instance/value selected from `Artifact.Definition`.

The preloaded resolver seam is deliberately narrow:

```csharp
public interface IContentPackResolver
{
    ContentCatalogResolution Resolve(string packId, string expectedHash);
}
```

Implementations passed to campaign admission must perform no I/O during `Resolve`. Future storage
loads bytes before constructing a scoped resolver. The synthetic catalog can implement or adapt to
this interface without introducing a service or dependency-injection requirement in Core.

### Creation command

```csharp
public sealed record CreateCampaign(
    string CampaignId,
    string RulesetHash,
    ulong Seed,
    string SetupId,
    string SetupHash,
    string ContentPackId,
    string ContentHash,
    string ScenarioId) : CampaignCommand(3, 0);
```

The caller supplies references, not authoritative setup/content copies. Admission resolves the
setup by exact ID/hash and content by exact ID/hash. `ScenarioId` must equal the scenario selected
by the setup. Empty or malformed fields are `InvalidCommand`; known-but-inconsistent references
use the more precise admission reasons below.

One application-facing coordinator owns preparation plus existing-state precedence and returns the
existing command result algebra:

```csharp
public static CampaignCommandResult DecideCreation(
    CampaignSnapshot? snapshot,
    CreateCampaign command,
    IContentPackResolver resolver);
```

`CampaignCommandRejectionReason` adds `UnknownSetup`, `SetupHashMismatch`, `UnknownContent`,
`ContentHashMismatch`, `UnsupportedRuleset`, `UnknownScenario`, `SetupContentMismatch`, and
`ScenarioStartMismatch`. Existing `InvalidState` and `CampaignAlreadyCreated` remain the results
for the snapshot-first branch. Every rejection returns a `CampaignCommandResult` with zero events;
only an accepted engine decision contains `CampaignCreated`. This gives command-shape, admission,
existing-state precedence, and authority one public assertion surface without treating resolution
as an event-producing decision.

Existing creation rejection precedence remains:

1. if a prior snapshot exists, resolve context from that snapshot's embedded selection rather than
   from the new command;
2. an unresolvable or noncanonical prior snapshot is `InvalidState`;
3. any valid existing campaign is `CampaignAlreadyCreated` without inspecting or resolving the new
   create command;
4. only a null snapshot reaches new-command admission; and
5. resolution or compatibility failure emits no event.

### Setup snapshot

```csharp
public sealed record CampaignSetupSnapshot(
    int SchemaVersion,
    string SetupId,
    string SetupHash,
    bool IsSynthetic,
    int InitialGameTurn,
    InitiativePolicy InitialInitiative,
    CampaignContentSelection Content,
    IReadOnlyList<RuleReference> Sources);
```

The snapshot embeds the complete setup policy required for replay plus the content selection. It
does not embed `DisplayName`, the Content Pack, or scenario presentation. Projector validation
recomputes the schema-2 setup hash from all fields.

### Mutable world

```csharp
public sealed record CampaignElementState
{
    public string ElementId { get; }

    public string CurrentLocationId { get; }
}

public sealed record CampaignWorldSnapshot
{
    public const int CurrentContractVersion = 1;

    public int ContractVersion { get; }

    public IReadOnlyList<CampaignElementState> Elements { get; }
}
```

The constructor copies and sorts elements by `ElementId` and rejects duplicates. Stable-ID shape
is checked locally. Content-aware validation additionally requires:

- exactly one world entry for every scenario placement;
- every entry names the placement's independently placeable element;
- every location exists in the exact pack; and
- the creation location equals the scenario placement.

No side, parent formation, organization, base/remaining Capability Point Allowance, contact,
cohesion, ZOC, visibility, or terrain value is copied into world v1. Future mechanics add mutable
facts only when their governing rule is implemented. Static facts are joined by stable element and
location IDs through the exact content context.

### Creation event and snapshot

```csharp
public sealed record CampaignCreated(
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    CampaignWorldSnapshot InitialWorld,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition)
    : CampaignEvent(3, CampaignId, StateVersion);

public sealed record CampaignSnapshot(
    int ContractVersion,
    string CampaignId,
    long StateVersion,
    string RulesetHash,
    CampaignSetupSnapshot Setup,
    CampaignWorldSnapshot World,
    LandSide? InitiativeHolder,
    RandomStreamState RandomState,
    LandSequencePosition SequencePosition);
```

The snapshot contract is version 3. `CampaignCreated.StateVersion` remains 1, random cursor remains
0, and sequence position remains Initiative Determination for the setup/scenario start Game Turn.
World initialization is part of this one atomic state transition; no intermediate campaign exists
without its initial elements.

`InitiativeDetermined` remains contract version 2 because its own serialized semantics do not
change. Applying it to a version-3 snapshot retains `World` byte-for-byte and produces snapshot
state version 2 at Naval Convoy.

## Admission flow

The application-facing coordinator first preserves existing-state precedence without mutating
campaign state:

```text
if snapshot exists
    -> resolve exact context from snapshot.Setup.Content
    -> resolution or validation failure: InvalidState
    -> otherwise: CampaignAlreadyCreated
    -> do not inspect or resolve the new CreateCampaign content request
else
    -> execute new-command admission below
```

For a null snapshot, new-command admission is:

```text
1. Validate CreateCampaign v3 local shape.
2. Resolve setup ID from the fixed setup catalog and compare setup hash.
3. Resolve content pack by exact ID + hash from the supplied preloaded resolver.
4. Build CampaignContentContext from the already validated artifact and verify exact identities.
5. Resolve scenario from that artifact; never from another catalog or pack.
6. Compare command selection with setup selection and scenario start with setup turn.
7. Call CampaignEngine.Decide(null, command, context).
8. Engine repeats identity/invariant checks and emits exactly one CampaignCreated.
```

The public admission overload always supplies `Cna1979SetupCatalog.Definitions`. A test-only
internal overload accepts a controlled setup list so rejection branches such as
`ScenarioStartMismatch` can be exercised without adding a deliberately invalid setup to the
production catalog or weakening its authority.

The engine constructs the setup snapshot and initial world itself. Neither is caller-controlled
command data.

Relevant stable creation-command rejection reasons:

```text
None
CampaignAlreadyCreated
InvalidCommand
InvalidState
UnknownSetup
SetupHashMismatch
UnknownContent
ContentHashMismatch
UnsupportedRuleset
UnknownScenario
SetupContentMismatch
ScenarioStartMismatch
```

The implementation may share existing `ContentCatalogRejectionReason` internally, but the
coordinator maps it to these exact `CampaignCommandRejectionReason` values. An admission failure
returns a rejected `CampaignCommandResult` with no event or snapshot because authority was never
entered.

## Creation and projection algorithm

For an admitted request, decision:

1. Revalidate current compiled rules hash, setup hash, content identity, and all cross-agreements.
2. Select the exact scenario from the context.
3. Map each scenario placement to `CampaignElementState(elementId, locationId)`.
4. Construct canonical `CampaignWorldSnapshot` and validate it against the context.
5. Create the initial deterministic random state from `Seed`.
6. Select the first Initiative Determination position for `Setup.InitialGameTurn`.
7. Emit exactly one `CampaignCreated` v3 with state version 1.

Projection never trusts the event's initial world. Given null prior state and exact context, it
recomputes the embedded setup hash, verifies every setup/content/scenario agreement, derives the
expected world, random invariants, and sequence position, and compares every checkable semantic
field. A mismatch throws typed invalid history. Only then does it produce the snapshot. Setup
authenticity still belongs to the Chronicle ingestion boundary described above.

This is stronger than merely validating that event element IDs exist: an attacker cannot relocate
an element or omit one while retaining syntactically valid history.

## Replay preparation and checkpoint admission

Replay becomes a two-stage API:

```text
untrusted/canonical event bytes
        |
        v
strict local parse of CampaignCreated v3
        |
        v
read exact ruleset/content/scenario selection
        |
        v
resolve supported executable + exact immutable content
        |
        v
CampaignReplayContext
        |
        v
history-aware CampaignProjector.Replay(events, context)
```

Preparation has its own non-command result algebra:

```csharp
public enum CampaignReplayPreparationRejectionReason
{
    None,
    InvalidHistory,
    MissingContent,
    ContentHashMismatch,
    UnsupportedRuleset,
}

public sealed record CampaignReplayPreparationResult(
    CampaignReplayContext? Context,
    CampaignReplayPreparationRejectionReason RejectionReason);
```

The mappings are exact: malformed/missing creation metadata is `InvalidHistory`; unknown exact pack
ID is `MissingContent`; known ID with a different artifact hash is `ContentHashMismatch`; and a
recorded rules hash unsupported by the executable is `UnsupportedRuleset`. A prepared result has
non-null `Context` and reason `None`; every rejected result has null context. Raw bytes that cannot
parse/validate into an artifact fail through `ContentPackParseResult` or
`InvalidContentPackException` before a resolver exists. Projector semantic inconsistencies after
successful preparation remain `InvalidCampaignHistoryException`.

Strict deserialization proves the closed local contract shape but cannot prove that an external
artifact exists. Contextual validity is therefore a separate, mandatory authority gate. Serializers
must not call a mutable global catalog implicitly.

`CampaignReplayContext` may reuse `CampaignContentContext`; it must also assert that the compiled
`Cna1979Ruleset.Manifest.Hash` equals history. A future executable registry belongs outside Core.
The current executable supports exactly its current canonical hash.

Snapshot checkpoints follow the same rule. Deserialization constructs an untrusted value after
strict local checks. Before engine use, `CampaignSnapshotValidator` receives exact context and
validates:

- version, campaign/setup/rules/random/sequence invariants;
- context identity equal to snapshot setup selection;
- complete element-ID membership and valid current locations; and
- exact scenario placement at every state reachable by the currently supported event set.

At state versions 1 and 2, world must equal the scenario initial projection because the only
supported transition is Initiative Determination and it preserves `World` byte-for-byte.
`WORLD-001` has no event capable of changing a location. A future movement capability may relax
checkpoint placement only in the same change that adds and validates the location-changing events;
the exact element set must still remain content-bound.

## Canonical JSON

Explicit writers freeze these orders.

`CampaignCreated` v3:

```text
contractVersion, eventType, campaignId, stateVersion, rulesetHash,
setup, initialWorld, randomState, sequencePosition
```

`CampaignSnapshot` v3:

```text
contractVersion, campaignId, stateVersion, rulesetHash,
setup, world, initiativeHolder, randomState, sequencePosition
```

Nested setup:

```text
schemaVersion, setupId, setupHash, isSynthetic, initialGameTurn,
initialInitiative, content, sources
```

Nested content selection:

```text
schemaVersion, formatId, packId, rulesetId, hash, scenarioId
```

Nested world:

```text
contractVersion, elements
```

Each element:

```text
elementId, currentLocationId
```

Element order is ordinal by `elementId`. Setup sources retain source-ID/locator ordering. Existing
initiative, random-state, and sequence-position shapes remain unchanged.

Readers reject version-2 `CampaignCreated` and `CampaignSnapshot` shapes,
reordered/duplicate/extra/missing properties, unknown format/version, malformed hashes/IDs,
duplicate elements, and locally invalid values. The unchanged version-2 `InitiativeDetermined`
reader remains supported after version-3 creation. Projector/context admission rejects semantic
content mismatches that a standalone reader cannot know.

## Failure behavior

| Condition | Boundary and result |
| --- | --- |
| Unknown pack ID | pre-decision `UnknownContent`; engine not entered |
| Right ID, wrong expected hash | pre-decision `ContentHashMismatch`; no fallback |
| Command ruleset hash does not equal the executable manifest hash | pre-decision `UnsupportedRuleset` |
| Raw historical bytes fail parsing, structural/capability validation, or `cna-1979.1` compatibility | `ContentPackParseResult` failure or `InvalidContentPackException` before a resolver/replay-preparation call exists |
| Unknown scenario within exact pack | pre-decision `UnknownScenario` |
| Command selection differs from setup | pre-decision `SetupContentMismatch` |
| Scenario start differs from setup turn | pre-decision `ScenarioStartMismatch` |
| Context differs from admitted command or snapshot | `InvalidCommand` on create or `InvalidState` for existing state |
| Forged initial world in event | typed `InvalidCampaignHistoryException` |
| Missing exact replay artifact | replay preparation returns `MissingContent` before projection |
| Known replay pack ID with the wrong artifact hash | replay preparation returns `ContentHashMismatch` before projection |
| Nonmatching historical rules hash | replay preparation returns `UnsupportedRuleset` before projection |
| Missing/malformed creation metadata | replay preparation returns `InvalidHistory` before projection |
| Malformed version-3 JSON passed directly to the strict reader | `JsonException`; no partial value |
| Strict-reader failure while preparing replay | preparation catches/maps it to `InvalidHistory`; no context |
| Version-2 creation or snapshot JSON | explicit unsupported contract/version failure; no upgrade |
| Version-2 `InitiativeDetermined` after valid version-3 creation | accepted and projected through its unchanged contract |
| Generic advance at Initiative or Naval Convoy | existing `UnsupportedTransition`; no state change |

## Contract migration policy

The current repository has no persisted user campaign, production Chronicle, public campaign JSON
API, or compatibility promise for version-2 creation fixtures. WORLD-001 therefore performs a
clean pre-alpha creation cutover:

- `CreateCampaign`, `CampaignCreated`, and `CampaignSnapshot` become version 3;
- setup definitions/snapshots become schema 2;
- `InitiativeDetermined` remains event contract version 2 and its reader remains supported;
- canonical goldens and all in-repository call sites migrate together;
- creation/snapshot readers reject old shapes instead of guessing missing content/world fields;
  and
- no converter, dual-write, compatibility event, or default-content adapter is added.

The Git revision containing version-2 creation remains the executable evidence for those
historical test fixtures. Before real persistence or external clients exist, Sandtable must define
a retained executable/content release policy rather than relying on Git archaeology.

## Rejected alternatives

### Embed the complete Content Pack in `CampaignCreated`

Rejected because it duplicates large immutable static data in every campaign, risks leaking
authoritative hidden definitions into later outward DTO reuse, and does not solve historical
rules-executable availability. Exact hash-addressed content is the replay dependency.

### Store only content identity and derive world silently during projection

Rejected because Chronicle should state the authoritative mutable result. Recording initial
positions makes creation auditable, while projector recomputation prevents forgery.

### Emit a separate `CampaignWorldInitialized` event

Rejected for v1 because it creates an unnecessary intermediate campaign lacking a world and shifts
all current state-version assumptions. A future large or staged setup mechanic can introduce its
own event when rules require player decisions during deployment.

### Resolve from the global synthetic catalog inside engine/projector

Rejected because it creates a mutable-current dependency, blocks historical replay, and makes a
future storage call tempting inside an authoritative turn.

### Preserve version-2 creation contracts through optional content fields

Rejected because optional authoritative identity/world data creates two meanings for one contract
and invites accidental default selection. Explicit version rejection is safer during pre-alpha.

### Add future mutable unit statistics now

Rejected because movement, contact, and combat rules have not yet defined their state transitions.
World v1 owns only current placement, the first proven mutable distinction from static content.

## Implementation checkpoints

Every checkpoint follows red-green-refactor, owns one coherent file cluster, and ends with focused
Core tests. The version-3 command/event/snapshot cutover is deliberately one larger atomic cluster
because no compile-green or semantically valid worldless intermediate contract exists. Status
remains `Pending` until executed evidence exists.

### `WLD-IMP-001` — Content-bound setup and fixture migration

**Requirements:** `WLD-002`, `WLD-004`, `WLD-013`, `WLD-014`

**Depends on:** Content Pack v1

**Status:** Complete

- Add the Game Turn 43 synthetic scenario and update canonical Content Pack bytes/hash evidence.
- Add content selection to setup schema 2 and setup hashing.
- Bind both setup definitions to exact artifact/scenario identities.
- Prove setup content/scenario/start invariants and hash participation in focused setup tests;
  campaign-admission mismatch tests wait for `WLD-IMP-003`.

**Likely files:** content catalog, setup definition/hash/catalog, focused Content/Setup tests.

**Verify:** focused Content and Setup tests; independent hash vector; `git diff --check`.

### `WLD-IMP-002` — Immutable world contracts and pure initial projection

**Requirements:** `WLD-006`, `WLD-007`, `WLD-NFR-002`

**Depends on:** `WLD-IMP-001`

**Status:** Complete

- Add element/world values with defensive copying and structural equality.
- Add a pure factory that derives the exact initial world from a validated scenario.
- Cover unknown, duplicate, missing, attachment-only, invalid-location, ordering, and mutation cases.

**Likely files:** two or three Campaign world files and one focused test file.

**Verify:** focused world tests; Core test project.

### `WLD-IMP-003` — Exact admission and atomic version-3 campaign cutover

**Requirements:** `WLD-001`, `WLD-003`-`WLD-010`, `WLD-012`-`WLD-016`,
`WLD-NFR-001`-`WLD-NFR-004`

**Depends on:** `WLD-IMP-001`, `WLD-IMP-002`

**Status:** Complete

- Introduce the exact preloaded resolver seam and typed admission result.
- Build/revalidate `CampaignContentContext` without I/O or ambient lookup.
- Cut `CreateCampaign`, `CampaignCreated`, and `CampaignSnapshot` atomically to version 3; creation
  must never emit a temporarily worldless event/snapshot.
- Update engine, snapshot validator, creation projector, and Initiative projection together so the
  complete repository remains green and Initiative retains `World` byte-for-byte.
- Migrate the coupled event/snapshot serializers, strict readers, exact version-3 goldens, all
  current campaign call sites/tests, and the retained version-2 `InitiativeDetermined` reader in
  the same cutover. No old constructor or context-free projector call may remain to break the
  compile-green checkpoint.
- Prove every admission failure emits no event and invokes no authoritative mutation.
- Prove `ScenarioStartMismatch` through the controlled internal catalog-input seam while the
  public overload remains fixed to the canonical setup catalog.
- Prove a valid existing snapshot returns `CampaignAlreadyCreated` before a malformed new command
  is inspected or its content resolver is called.

**Likely files:** resolver/context/admission and world values; campaign command/event/snapshot,
engine/validator/projector; focused admission/authority tests. This is one deliberately atomic
cross-contract checkpoint: splitting its public types would require a forbidden worldless
intermediate campaign.

**Verify:** focused campaign admission, creation, canonical serialization, replay-object, and
Initiative tests; complete Core test project.

### `WLD-IMP-004` — Replay preparation and canonical history authority

**Requirements:** `WLD-009`-`WLD-011`, `WLD-014`, `WLD-NFR-004`

**Depends on:** `WLD-IMP-003`

**Status:** Complete

- Add `CampaignReplayPreparationResult` and exact rejection mappings for malformed history,
  missing/mismatched exact content, and unsupported executable identity.
- Use the strict version-3 creation reader delivered in `WLD-IMP-003` to discover selection, then
  project prepared history through exact context.
- Recompute and compare every context-derived creation fact; reject inconsistent world/history.
- Preserve the unchanged version-2 `InitiativeDetermined` reader and prove it applies after
  version-3 creation.
- Preserve stale/unsupported transition and deterministic Initiative behavior.

**Likely files:** replay context/preparation/harness, projector authority checks, and focused
history tests.

**Verify:** campaign authority/replay/Initiative tests; Core test project.

### `WLD-IMP-005` — Acceptance hardening, boundaries, and synchronized documentation

**Requirements:** `WLD-012`, `WLD-015`, `WLD-NFR-003`, `WLD-NFR-005`, `WLD-NFR-006`

**Depends on:** `WLD-IMP-004`

**Status:** Complete

- Complete cross-run/order/mutation determinism cases beyond the contract goldens delivered in
  `WLD-IMP-003` and reconcile every acceptance ID with executed evidence.
- Search outward projects for forbidden authoritative world/content types.
- Synchronize README, technical design, naming, roadmap, specification, and this design.

**Likely files:** acceptance/boundary tests and governing docs.

**Verify:** serialization/golden and dependency-boundary tests, then `just check`.

## Requirement-to-task traceability

| Requirement | Tasks | Verification | Status/evidence |
| --- | --- | --- | --- |
| `WLD-001`-`WLD-005` | `WLD-IMP-001`, `WLD-IMP-003` | setup hash vectors and `CampaignCreationAdmissionTests` | Complete |
| `WLD-006`, `WLD-007` | `WLD-IMP-002` | `CampaignWorldTests` | Complete |
| `WLD-008`-`WLD-010`, `WLD-013`, `WLD-015`, `WLD-016` | `WLD-IMP-003` | admission, world, serialization, and Initiative campaign tests | Complete |
| `WLD-009`-`WLD-011` | `WLD-IMP-004` | `CampaignReplayPreparationTests` and forged-history tests | Complete |
| `WLD-012` | `WLD-IMP-003`, `WLD-IMP-005` | strict reader/version-cutover tests | Complete |
| `WLD-013` | `WLD-IMP-001`, `WLD-IMP-003` | two-scenario synthetic fixture and exact setup bindings | Complete |
| `WLD-014` | `WLD-IMP-001`, `WLD-IMP-003`, `WLD-IMP-004` | version-2 creation/snapshot rejection plus retained Initiative-v2 round trip | Complete |
| `WLD-015` | `WLD-IMP-003`, `WLD-IMP-005` | Core-only world types and project-reference search | Complete |
| `WLD-016` | `WLD-IMP-003` | generic sequence-completion rejection before and after Initiative resolution | Complete |
| `WLD-NFR-001`-`006` | all checkpoints | focused tests plus full repository gate | Complete |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Resolver abstraction accidentally permits I/O in a grain turn | Contract resolver as preloaded/synchronous, construct context before engine entry, and keep host/storage adapters out of this slice. |
| Setup/content/event duplicate identities drift | Setup hash covers selection; admission and engine compare all copies; projector recomputes expected creation. |
| Event grows with future campaign size | Record only mutable placements once; do not copy static definitions. Revisit chunking only with measured production content. |
| Snapshot validation becomes content-free and accepts impossible locations | Require exact context before engine/checkpoint use and add negative mismatch tests. |
| Contract cutover obscures prior history | Reject old versions explicitly and document the exact pre-alpha cutover; do not claim production compatibility. |
| Fixture change is mistaken for source-derived CNA data | Keep every new scenario/placement synthetic, original, and separately source-located. |
| World type leaks hidden state into outward APIs | Keep it in Core and add static project-reference/serialization boundary tests before `OBS-001`. |

## Verification and completion gate

Implementation verification commands are:

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

Project-owner approval was received before implementation. The implementation and its remediation
passed fresh independent review on 2026-08-16 with no P0-P3 findings. The final independent gate
reproduced 184 passing tests, zero warnings/errors, and clean formatting/diff checks.

The fresh design readback on 2026-08-16 reported `Ready with non-blocking follow-ups`, with no
P0-P2 finding. Its sole P3 requested that direct `JsonException` behavior be distinguished from the
replay-preparation `InvalidHistory` mapping; the failure table above now makes that boundary exact.
