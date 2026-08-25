# Operation-Stage Entry v1 Technical Design

**Status:** Implemented; STG-TASK-001 through STG-TASK-022 and STG-TASK-014A complete

**Date:** 2026-08-24

**Specification:** [Operation-Stage Entry v1](../specs/operation-stage-entry-v1.md)

**Research:** [Operation-Stage Entry v1 source and contract spike](../research/operation-stage-entry-spike.md)

## Design summary

Add an exact setup-hashed Stage Entry policy proving that one admitted synthetic
`(GameTurn, OperationStage)` has no Organization, Naval Convoy Arrival, Fleet Assignment, or Fleet
Repair obligations. At each mandatory sequence position, the trusted system audience receives one
closed candidate. Current membership and a mechanic-specific command/event advance exactly one
position. The fourth event reaches the unchanged catalog `FirstActingSide` Reserve Designation and
stops. Authority retains null `SequencePosition.ActiveSide`; legal-action and observation
projection derive the active audience from retained stage order.

The design adds no positive stage-entry subjects, generic sequence primitive, random operation,
side submission, adapter, persistence, or UI. It preserves Command/Umpire authority and treats the
policy as synthetic admission evidence rather than missing-data inference.

## Control and authority flow

```text
setup catalog
  -> canonical Stage Entry policy for exact pair
  -> campaign creation / snapshot / replay retention

trusted system query
  -> current snapshot validity
  -> exact phase + pair + policy assertion
  -> one closed candidate
  -> current-membership submission
  -> mechanic-specific command/event factory
  -> exact successor recomputation
  -> one event / one state-version increment

Organization
  -> Naval Convoy Arrival
  -> Fleet Assignment
  -> Fleet Repair
  -> catalog FirstActingSide Reserve Designation (stop)
       -> active audience derived in legal/observation projection
```

There is no side-client-to-system-action edge and no stage-entry-to-Reserve execution edge.

## Module ownership

| Module | Owns | Must not own |
| --- | --- | --- |
| `Cna.Core.Setups` | Closed Stage Entry policy, source identity, exact pair, setup canonical/hash admission | Runtime phase inference, positive subject state, rules execution |
| `Cna.Core.Rules` | Existing source-ordered positions and explicit-empty ruling manifest identity | Generic advancement or player decisions |
| `Cna.Core.Actions` | Closed system candidates, current-membership query/submission, stale/wrong-audience rejection | Hidden side choice, policy creation, transition authority |
| `Cna.Core.Campaigns` | Commands, events, factory invariants, projection/replay recomputation, successor snapshot | UI interaction, remote I/O, positive deferred mechanics |
| `Cna.Core.Observations` | Source-free position/audience projection, including derived `FirstActingSide` audience at Reserve | Stage Entry policy, sources, system candidates, authority state |
| Core tests | Contract, transition, history, fog, replay, and public-surface evidence | Production escape hatches |

## Proposed decisions

The owner accepted `STG-DEC-001` through `STG-DEC-011` on 2026-08-24. The design applies them as
follows:

- a new separate policy contract avoids silently changing the meaning of opening-preamble or Weather
  policy values;
- four assertions are closed and independent even though v1 admits only `explicit-none`;
- four action/event identities make exact skipped/reordered-phase detection possible;
- one Organization barrier preserves future player-selected ordering;
- two Fleet positions preserve source order and future separation of assignment from repair;
- retained `CampaignOperationStageOrder` determines the projected Reserve audience without changing
  the catalog sequence position;
- the Stage 1 fixture policy cannot be reused for another pair; and
- the ruleset manifest retains the approved explicit-empty procedure ruling rather than leaving the
  setup policy to invent mandatory-phase semantics by itself.

## STG-TASK-001 technical freeze candidate

This section is the post-merge technical overview and exact contract proposal. It was audited at
`a022b784cda90be90ff6af9802c0c0352b9f89a6`. It freezes what later tasks are allowed to build, but
does not itself authorize production implementation.

### Authority and trust boundary

The setup catalog is the sole author of Stage Entry admission. Its policy is retained in setup hash,
creation history, snapshot, projection, and replay. Legal Actions may advertise one current trusted
system candidate, but candidate membership does not grant transition authority: the matching
Campaign command/event factory revalidates the exact pair, phase, policy, ruleset/setup identities,
and catalog successor. The Umpire remains authoritative; no player text, model output, remote I/O,
or Intelligence contract enters this path.

The feature is deliberately asymmetric:

```text
synthetic setup policy (authority-only)
  -> system candidate (current membership)
  -> mechanic command/event (one phase only)
  -> replay/projector recomputation
  -> Reserve position (public audience derived; Reserve behavior absent)
```

### Frozen version matrix

| Contract or identity | Merged value | Stage Entry v1 value | Reason |
| --- | ---: | ---: | --- |
| `Cna1979Ruleset.ContractVersion` | 3 | **4** | New adopted ruling changes ruleset hash identity. |
| `Cna1979SetupCatalog.SchemaVersion` | 4 | **5** | Required Stage Entry policy changes setup canonical identity. |
| `CreateCampaign` / `CampaignCreated` | 4 | **5** | Creation history now retains setup schema 5 and rejects v4. |
| `CampaignSnapshot` | 5 | **6** | Snapshot setup bytes now require the Stage Entry policy and reject v5. |
| `CampaignStageEntryPolicy` | absent | **1** | New closed pair-bound policy contract. |
| Four Stage Entry commands | absent | **1 each** | New internal, mechanic-specific commands. |
| Four Stage Entry events | absent | **1 each** | New replay evidence; no shared generic event version. |
| `CampaignActionCandidate` | 1 | **1 (unchanged)** | Existing payload-free candidate envelope already fits. |
| Legal Action set / submission | 1 / 1 | **unchanged** | Existing concurrency and membership binding remains authoritative. |
| Land sequence / catalog | 2 / 2 | **unchanged** | Required positions and order already exist. |
| `CampaignOperationStageOrder` | 2 | **unchanged** | Existing pair-keyed order derives the Reserve audience. |
| Campaign Observation | 2 | **unchanged** | No new public field; audience is derived in projection. |
| Opening-preamble / Weather policy | 1 / 1 | **unchanged** | Separate Stage Entry policy avoids redefining them. |

The cut is intentionally strict. The new executable accepts only the right-hand versions; it does
not synthesize a policy for old creation or snapshot bytes. Exact resulting ruleset and setup hashes
are golden outputs of Tasks 002 and 004 respectively; Task 001 freezes their inputs and canonical
order rather than fabricating preimplementation digest values.

### Frozen policy contract

The exact type identity is `CampaignStageEntryPolicy`, contract 1. The exact discriminator is
`StageEntryObligationKind` with numeric ordinals `ExplicitNone = 1` and `HasObligations = 2`.
Zero and every other numeric value reject. Canonical strings are `explicit-none` and
`has-obligations`; `HasObligations` is recognized for strict decoding but unsupported for v1
admission.

The C# constructor/property order and canonical JSON property order are identical:

```text
contractVersion, gameTurn, operationStage, organization,
navalConvoyArrival, fleetAssignment, fleetRepair, sources
```

`gameTurn` is the setup's initial Game Turn and `operationStage` is exactly `1`. All four assertions
must be `ExplicitNone`. `sources` contains exactly one entry with canonical source object order
`sourceId`, `locator` and value
`sandtable-rules-lab:stage-entry.no-obligations.v1`. Sources are sorted ordinally by source ID then
locator before retention; missing, duplicate, extra, reordered, or altered values reject.

`CampaignSetupDefinition`, setup-hash input, and `CampaignSetupSnapshot` insert the required
`stageEntry` value after `weather` and before `content`. The embedded canonical setup order becomes:

```text
schemaVersion, setupId, setupHash (snapshot only), isSynthetic, initialGameTurn,
initialInitiative, openingPreamble, weather, stageEntry, content, sources
```

The setup-hash form omits `setupHash`, as it does today. No overload or default may create a setup
without the required policy.

### Frozen actions, commands, and events

All four candidates remain contract 1, payload-free, and omit `operationStage`; the exact semantic
bytes are `contractVersion`, `kind`. Their stable kinds and derived IDs are:

| Kind | Action ID |
| --- | --- |
| `resolve-no-obligation-organization` | `sha256:2200e6c4cef001d344d85de78fc7a10c13b32c12975d905c633ca430c3c4bd4c` |
| `resolve-no-obligation-naval-convoy-arrival` | `sha256:a49ff99f7e52193fdee44b50751e64025121cb9a2a75a054fdf2ad045e013632` |
| `resolve-no-obligation-fleet-assignment` | `sha256:c2d7dae34d20f826d2e7e682b8d3b437224e42b522857f63b3869d1a1bf3bcc5` |
| `resolve-no-obligation-fleet-repair` | `sha256:ea4fe4f27344a8659c81b05fd84df2e260bb22da1edf1115cd4d75dfd89d7d3e` |

Each internal command is contract 1 and carries derived-record constructor order
`ExpectedStateVersion`, `ExpectedPositionId`; commands have no external serializer. Event type
strings are exact and follow the existing mechanic-event convention:

```text
no-obligation-organization-resolved
no-obligation-naval-convoy-arrival-resolved
no-obligation-fleet-assignment-resolved
no-obligation-fleet-repair-resolved
```

Each derived event record hard-codes contract 1 through `CampaignEvent` and uses this constructor
order (the inherited `ContractVersion` is not a parameter and `eventType` is serializer-only):

```text
campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
sequencePosition, sources
```

The distinct record names are `NoObligationOrganizationResolved`,
`NoObligationNavalConvoyArrivalResolved`, `NoObligationFleetAssignmentResolved`, and
`NoObligationFleetRepairResolved`. Their canonical JSON property order is:

```text
contractVersion, eventType, campaignId, stateVersion, fromPositionId,
gameTurn, operationStage, sequencePosition, sources
```

Starting from the merged Weather-at-Organization snapshot at state version 6, the four accepted
events commit versions 7, 8, 9, and 10 respectively. No other event may occupy those checkpoints in
v1, and a rejected action leaves the current version unchanged.

The mechanic source set is exactly the repository-synthetic source plus one primary locator. Its
canonical retained order is source ID then locator:

| Event | Primary locator |
| --- | --- |
| Organization | `spi-1979-land-rules:5.2.organization` |
| Naval Convoy Arrival | `spi-1979-land-rules:5.2.naval-convoy-arrival` |
| Fleet Assignment | `spi-1979-land-rules:5.2.commonwealth-fleet` |
| Fleet Repair | `spi-1979-land-rules:5.2.commonwealth-fleet` |

The ruleset ruling identity, alternatives, selected behavior, protecting IDs, and two-source set in
the next section are also exact. Canonical ruleset serialization sorts ruling IDs, string sets, and
sources ordinally; no insertion-order meaning is introduced.

### Inventory method and result

The Task 001 audit searched every C# constructor of `CampaignSetupDefinition`,
`CampaignSetupSnapshot`, `CampaignCreated`, and `CampaignSnapshot`; every setup/snapshot/event
serializer and strict version check; every `CampaignSetupHash.Calculate` call; every current
ruleset/setup hash literal; and the Exercise semantic snapshot decoder. The frozen file map below
covers all direct constructor, serializer, validator, version, checked-literal, and hash consumers
found at the audited commit.

Consumers that only pass catalog-derived `SetupId`/`SetupHash` at runtime need no contract edit and
remain regression evidence. Build identity codecs also retain values supplied by a completed run and
need no schema change. The identity lane in Tasks 002-010 is one non-mergeable clean-cut workstream.
Tasks 002-003 produce focused RED/GREEN contract evidence. Tasks 004-009 record their exact owned
changes, expected unresolved test-project compiler/golden failures, and any available source/Core
build evidence; they must not claim executable GREEN while later-owned required-constructor and
identity consumers remain unmigrated. Task 010 resolves the recorded failure inventory and is the
first required green test-project build/full checkpoint. No partial identity-lane commit may merge.

### Ruleset ruling identity

The ruleset migration is a prerequisite, not an incidental consequence of setup serialization. It
adds this exact manifest ruling before any policy, action, or event golden is accepted:

- **ruling:** `cna-1979.1.ruling.explicit-empty-stage-entry-resolution`;
- **conflict:** `cna-1979.1.conflict.empty-stage-entry-phase`;
- **alternatives:** `reject-empty-stage-entry-as-unsupported` and
  `resolve-explicitly-admitted-empty-stage-entry`;
- **selected behavior:** `resolve-explicitly-admitted-empty-stage-entry`;
- **protecting acceptance IDs:** `STG-AC-001`, `STG-AC-002`, `STG-AC-004`, `STG-AC-005`,
  `STG-AC-006`, `STG-AC-009`, and `STG-AC-010`; and
- **canonical ruling sources:** `spi-1979-land-rules:5.2` and
  `sandtable-rules-lab:stage-entry.no-obligations.v1`.

Event source sets are also exact: Organization uses
`spi-1979-land-rules:5.2.organization`; arrival uses
`spi-1979-land-rules:5.2.naval-convoy-arrival`; Fleet Assignment and Fleet Repair each use
`spi-1979-land-rules:5.2.commonwealth-fleet`. Every event set also includes the same repository
synthetic source above, in canonical order. The resulting ruleset version/hash is retained by
creation, legal-action, event, snapshot, and replay evidence.

## Frozen contract shapes

These shapes are subordinate to, and must be read with, the Task 001 freeze above. Type/record names,
numeric versions, enum ordinals, constructor field order, canonical JSON order, action identifiers,
and nullability called out by the freeze are exact. Implementation may refine private helper
structure and file-local organization only; it may not change those contract semantics.

### Setup policy

```csharp
internal sealed record CampaignStageEntryPolicy(
    int ContractVersion,
    int GameTurn,
    int OperationStage,
    StageEntryObligationKind Organization,
    StageEntryObligationKind NavalConvoyArrival,
    StageEntryObligationKind FleetAssignment,
    StageEntryObligationKind FleetRepair,
    IReadOnlyList<RuleReference> Sources);

internal enum StageEntryObligationKind
{
    ExplicitNone = 1,
    HasObligations = 2,
}
```

`HasObligations` is a recognized closed value so the unsupported-capability boundary is executable;
v1 admission requires positive Game Turn, Operation Stage 1, all four values equal to
`ExplicitNone`, one exact repository-synthetic source, canonical source ordering, and no
duplicate/extra source. The policy is part of setup equality, hash, canonical JSON, creation event,
snapshot, and replay preparation. This discriminator does not model or identify any positive
subject.

### System action candidates

```text
resolve-no-obligation-organization
resolve-no-obligation-naval-convoy-arrival
resolve-no-obligation-fleet-assignment
resolve-no-obligation-fleet-repair
```

Each candidate uses the existing closed action-set envelope and has no payload. The envelope carries
campaign/state/ruleset/position/audience; the candidate carries contract version, action ID, and
kind. Although the shared base type retains a nullable `OperationStage` property, all four Stage
Entry candidates leave it null, omit it from canonical JSON, and derive their exact IDs from only
`contractVersion` and `kind`. Submission preserves Legal Actions v1: contract version, campaign ID,
expected state/position, audience, and opaque action ID. It does not duplicate ruleset hash or kind.
The current position and admitted setup policy contain all required semantics. Unknown or
payload-bearing variants reject.

### Commands and events

```text
ResolveNoObligationOrganization
  -> NoObligationOrganizationResolved

ResolveNoObligationNavalConvoyArrival
  -> NoObligationNavalConvoyArrivalResolved

ResolveNoObligationFleetAssignment
  -> NoObligationFleetAssignmentResolved

ResolveNoObligationFleetRepair
  -> NoObligationFleetRepairResolved
```

Every command carries the existing authority binding required by internal execution. Every event
has a distinct stable type/contract version and retains:

- campaign ID and next state version;
- exact from-position ID;
- exact successor `LandSequencePosition`;
- admitted Game Turn and Operation Stage; and
- canonical source references.

No event contains a boolean “was empty,” positive subject collection, client audience, policy body,
world payload, or Reserve choice. Its event type itself records the admitted empty resolution.

## State-transition invariants

For every transition, the factory must:

1. validate the complete current snapshot/context;
2. require the exact setup policy and current pair;
3. require the expected phase/segment and system action kind;
4. recompute `Cna1979LandSequence.GetNext(current)`;
5. require the exact expected successor phase/segment and same pair;
6. for Fleet Repair, require the exact catalog successor with actor role `FirstActingSide` and null
   `ActiveSide`; validate that the current pair-keyed `CampaignOperationStageOrder` can resolve its
   audience without materializing that side into the position or changing initiative holder;
7. construct one event with state version `current + 1`; and
8. project that event while preserving every unrelated authority value.

The allowed transition table is closed:

| Current | Required policy assertion | Successor |
| --- | --- | --- |
| Organization phase barrier | Organization `ExplicitNone` | Naval Convoy Arrival phase |
| Naval Convoy Arrival phase | Arrival `ExplicitNone` | Commonwealth Fleet / Fleet Assignment |
| Commonwealth Fleet / Fleet Assignment | Assignment `ExplicitNone` | Commonwealth Fleet / Fleet Repair |
| Commonwealth Fleet / Fleet Repair | Repair `ExplicitNone` | catalog `FirstActingSide` Reserve Designation with null stored active side |

Any other tuple is an internal invariant failure during event construction and an invalid-history
failure during projection/replay. In particular, a Reserve successor with a populated stored
`ActiveSide` is invalid even when it names the correct side.

## System action generation

Current code selects several system actions by raw state version. Stage Entry should first
characterize existing behavior and then route implemented system candidates by semantic position and
required admitted policy. State version remains part of the candidate's concurrency binding, not the
meaning of the phase.

Recommended pure dispatch shape:

```csharp
private static IReadOnlyList<CampaignActionCandidate> GenerateSystemCandidates(
    CampaignSnapshot snapshot) => snapshot.SequencePosition switch
{
    { PhaseId: LandPhaseIds.Organization } when HasAdmittedOrganizationPolicy(snapshot)
        => [new ResolveNoObligationOrganizationAction()],
    // exact remaining positions...
    _ => ExistingCandidates(snapshot),
};
```

The final implementation should avoid hiding validation inside pattern guards: candidate generation
may return none for unsupported state, while submission/event factories independently revalidate
the complete boundary. Existing Initiative/convoy/Weather behavior and canonical action bytes must
remain exact.

## Projection, replay, and migration

### Clean-cut migration

Freeze exact next version numbers only after `EXR-TASK-014` merges. At minimum, the clean cut must
update every contract that directly serializes or hashes the new setup policy:

- setup schema and canonical hash;
- create-campaign command and campaign-created event;
- setup snapshot and campaign snapshot; and
- any creation/replay preparation envelope that enumerates setup fields.

The new executable rejects the prior versions. Historical replay remains the prior Git revision's
responsibility; do not add permissive missing-policy defaults.

### Projection

Projection re-derives the expected event from the previous snapshot, setup policy, and command
semantics or validates an equivalent closed event factory result. It rejects changed event type,
from/successor position, pair, source list, state version, or impossible Reserve audience.

### Replay

Replay preparation validates exact content/ruleset/setup bytes and reconstructs the four transitions
from retained history. A fresh-session replay must end with the same snapshot canonical bytes,
authority digest, Chronicle bytes, and current Reserve audience as the original run.

## Fog, privacy, and public surface

- Stage Entry policy and sources remain internal authority/setup data.
- System candidates are never returned in side action sets.
- Side observation adds no policy or subject data. At Reserve, it derives the source-free active
  audience from the current pair-keyed stage order while retaining the canonical catalog position
  in authority; legal-action audience resolution uses the same pure helper.
- Paired hidden-opponent mutations must produce identical side observation and side legal-action
  bytes at every system-only position and the Reserve successor.
- No command/event/snapshot/projector becomes public to production assemblies. Existing safe
  creation, observation, legal-query, and submission facades remain the complete outward Core seam.
- No raw authority appears in logs, diagnostics, exceptions, or `ToString` output.

## Failure behavior

| Failure | Required behavior |
| --- | --- |
| Missing/altered/wrong-pair policy | No candidate or typed invalid-state rejection; zero events |
| Side submits system action | Wrong audience/current-membership rejection; zero events |
| Stale or duplicate action | Stale/current-membership rejection; zero events |
| Out-of-order action/event | Invalid action/history; zero accepted events |
| Unknown contract/action/event kind | Strict deserialize/validation rejection |
| Changed from/successor/pair/source/state version | Projection/replay rejection |
| Recognized non-empty/unsupported policy kind appears | Unsupported capability; never auto-resolve |
| Stage 2/3 or later turn | Unsupported without a separately admitted policy and completed predecessors |

## Delivery plan

No implementation task begins until the specification completion gate passes. Each task is refined
to at most five material files. Tasks 002-003 and 011 onward require focused RED/GREEN evidence.
Tasks 004-009 use the explicitly deferred identity-lane evidence contract above; Task 010 must turn
the complete recorded failure inventory green before the lane can merge. Generated goldens/fixtures
do not authorize unrelated refactoring.

The file lists below are the review-candidate material boundaries. Owner acceptance freezes them.
After acceptance, any rebase drift must return to Task 001 before implementation; there are no
conditional “split later” tasks.

| Task | Outcome and evidence | Material files (maximum five) | Depends on |
| --- | --- | --- | --- |
| `STG-TASK-001` | Freeze owner decisions, versions, enum ordinals, field order, sources, file map, and complete traceability; rerun an exhaustive constructor/serializer/version/hash consumer inventory before freezing the map. Completion requires owner approval and a current independent review with no unresolved P0/P1. | `docs/research/operation-stage-entry-spike.md`; `docs/specs/operation-stage-entry-v1.md`; `docs/design/operation-stage-entry-v1.md` | accepted `EXR-TASK-014` |
| `STG-TASK-002` | Add the exact ruling, migrate ruleset version/hash, and update strict manifest goldens. | `src/Cna.Core/Rules/Cna1979Ruleset.cs`; `src/Cna.Core/Rules/RulesetManifest.cs`; `tests/Cna.Core.Tests/Rules/RulesetManifestTests.cs`; `tests/Cna.Core.Tests/Rules/StageEntryRulingTests.cs` | 001 |
| `STG-TASK-003` | Define and strictly encode the closed pair-bound policy; verify malformed, source-order, culture, and unsupported-kind matrices. | `src/Cna.Core/Setups/CampaignStageEntryPolicy.cs`; `src/Cna.Core/Setups/CampaignStageEntryPolicyCodec.cs`; `tests/Cna.Core.Tests/Setups/CampaignStageEntryPolicyTests.cs` | 002 |
| `STG-TASK-004` | Bind policy into setup definition/catalog/hash and both synthetic setup goldens; migrate the opening-preamble definition constructor with no compatibility default. | `src/Cna.Core/Setups/CampaignSetupDefinition.cs`; `src/Cna.Core/Setups/Cna1979SetupCatalog.cs`; `src/Cna.Core/Setups/CampaignSetupHash.cs`; `tests/Cna.Core.Tests/Setups/CampaignSetupTests.cs`; `tests/Cna.Core.Tests/Campaigns/OpeningPreambleCampaignTests.cs` | 003 |
| `STG-TASK-005` | Clean-cut create-command/event versions, event dispatch, and command admission; old versions reject. Embedded setup-policy encoding remains Task 006. | `src/Cna.Core/Campaigns/CampaignCommand.cs`; `src/Cna.Core/Campaigns/CampaignEvent.cs`; `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignEventSerializer.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignCreationAdmissionTests.cs` | 004 |
| `STG-TASK-006` | Own strict embedded-policy writing/parsing and omitted/default rejection across creation/snapshot bytes; projection/replay never invent policy. Expand checkpoint validation to exact catalog states 1–10 so Tasks 015–018 can project successors; Task 018 adds the Reserve-specific invariant. | `src/Cna.Core/Campaigns/CampaignSetupSnapshot.cs`; `src/Cna.Core/Campaigns/CampaignSnapshotSerializer.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `src/Cna.Core/Campaigns/CampaignSnapshotValidator.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignReplayPreparationTests.cs` | 005 |
| `STG-TASK-007` | Migrate Core creation/replay identity goldens, direct setup-snapshot constructors, and current-version assertions to the accepted contracts and identities. | `tests/Cna.Core.Tests/Campaigns/CampaignEventSerializationTests.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignReplayTests.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignTests.cs`; `tests/Cna.Core.Tests/Campaigns/Version3AuthorityTests.cs` | 006 |
| `STG-TASK-007A` | Migrate observation identity goldens and the shared observation setup/snapshot constructor helper to the accepted contracts and identities. | `tests/Cna.Core.Tests/Observations/CampaignObservationSerializationTests.cs`; `tests/Cna.Core.Tests/Observations/Fixtures/campaign-observation-axis.v1.golden.json`; `tests/Cna.Core.Tests/Observations/CampaignObservationTestData.cs` | 006 |
| `STG-TASK-008` | Migrate all checked Exercise/Maneuver manifests and their governing identity examples to the accepted identities and exact canonical bytes. | `scenarios/exercises/rules-lab.organization.v1.json`; `scenarios/exercises/rules-lab.organization.baseline.v1.json`; `scenarios/maneuvers/rules-lab.serial.v1.json`; `docs/specs/exercise-harness-v1.md`; `docs/specs/weather-determination-v1.md` | 006 |
| `STG-TASK-009` | Migrate ExerciseRunner contract/report test literals to the accepted identities. | `tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseManifestCodecTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ManeuverManifestCodecTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ManeuverReportCodecTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ManeuverReportLifecycleTests.cs`; `tests/Cna.ExerciseRunner.Tests/Execution/ManeuverExecutorTests.cs` | 006 |
| `STG-TASK-010` | Migrate ExerciseRunner's strict snapshot decoder contract, remaining Exercise CLI fixture identity, and the derived step-evidence hash golden, with focused semantic-reader version rejection/acceptance evidence; then close the non-mergeable identity lane with full Weather/Exercise gates. | `src/Cna.ExerciseRunner/Artifacts/ExerciseEvidenceCodec.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseBundleSemanticValidatorTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseEvidenceWriterTests.cs`; `tests/Cna.ExerciseRunner.Tests/Commands/ManeuverRunCommandTests.cs` | 007, 007A, 008-009 |
| `STG-TASK-011` | Characterize then make system dispatch semantic while preserving existing Initiative/convoy/Weather bytes. | `src/Cna.Core/Actions/CampaignLegalActions.cs`; `tests/Cna.Core.Tests/Actions/CampaignLegalActionsTests.cs` | 010 |
| `STG-TASK-012` | Add four closed payload-free candidates and strict canonical candidate codec evidence. | `src/Cna.Core/Actions/CampaignActionCandidate.cs`; `src/Cna.Core/Actions/CampaignLegalActionSerializer.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryActionContractTests.cs` | 010 |
| `STG-TASK-013` | Add four distinct event contracts and strict canonical event codec evidence with exact per-mechanic sources. | `src/Cna.Core/Campaigns/StageEntryEvents.cs`; `src/Cna.Core/Campaigns/CampaignEventSerializer.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryEventContractTests.cs` | 005, 010 |
| `STG-TASK-014` | Wire exact system membership, commands, and candidate-to-command mapping; unsupported policy/pair/audience yields zero executable events. Update the existing post-Weather system-set assertion to the new Organization membership boundary without accepting the transition before Task 015. | `src/Cna.Core/Actions/CampaignLegalActions.cs`; `src/Cna.Core/Actions/CampaignActionExecution.cs`; `src/Cna.Core/Campaigns/StageEntryCommands.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryMembershipTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignLegalActionsTests.cs` | 011-013 |
| `STG-TASK-014A` | Decouple ExerciseRunner's zero-active-audience failure fixtures from the evolving Core terminal boundary so Tasks 014-018 retain stable failure-path evidence and full gates. | `tests/Cna.ExerciseRunner.Tests/Execution/ExerciseExecutorTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseEvidenceWriterTests.cs` | 014 |
| `STG-TASK-015` | Organization vertical transition: exact policy/factory/projection, one event, authority preservation, and forged-history negatives. Update the existing membership frontier so Organization is accepted while Arrival, Assignment, and Repair remain unsupported. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryOrganizationTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryMembershipTests.cs` | 014A |
| `STG-TASK-016` | Arrival vertical transition with exact successor/source and no invented arrivals or logistics facts. Update the existing membership frontier so Arrival joins Organization as accepted while Assignment and Repair remain unsupported. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryArrivalTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryMembershipTests.cs` | 015 |
| `STG-TASK-017` | Fleet Assignment vertical transition, separate from Repair and with no invented ships/choice. Update the existing membership frontier so Assignment joins Organization and Arrival as accepted while Repair remains unsupported. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryFleetAssignmentTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryMembershipTests.cs` | 016 |
| `STG-TASK-018` | Fleet Repair reaches the exact catalog Reserve position with null stored active side and no Reserve action. Update the existing membership frontier so all four Stage Entry transitions are accepted. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `src/Cna.Core/Campaigns/CampaignSnapshotValidator.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryFleetRepairTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryMembershipTests.cs` | 017 |
| `STG-TASK-019` | One pure campaign-authority resolver serves legal actions and observations; act-first/last and `GetNext` continuity pass. | `src/Cna.Core/Campaigns/FirstActingSideResolver.cs`; `src/Cna.Core/Actions/CampaignLegalActions.cs`; `src/Cna.Core/Observations/CampaignObservationProjector.cs`; `tests/Cna.Core.Tests/Observations/CampaignReserveAudienceTests.cs` | 018 |
| `STG-TASK-020` | Close two-setup end-to-end, fresh replay, hidden-opponent byte invariance, and public-surface evidence. | `tests/Cna.Core.Tests/Campaigns/StageEntryCampaignTests.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignReplayPreparationTests.cs`; `tests/Cna.Core.Tests/Observations/CampaignObservationPrivacyTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignLegalActionsTests.cs`; `tests/Cna.Core.Tests/Campaigns/AuthorityBoundaryTests.cs` | 019 |
| `STG-TASK-021` | Reconcile the governing repository map, roadmap, naming, and architectural rationale with delivered behavior. | `README.md`; `docs/roadmap/pre-alpha-roadmap.md`; `naming-overview.md`; `tech-design.md` | 020 |
| `STG-TASK-022` | Reconcile planning statuses/evidence, run Core/solution/`just check`, and close the already-obtained final independent implementation review; the 3-of-3 review limit permits no new instance. | `docs/research/operation-stage-entry-spike.md`; `docs/specs/operation-stage-entry-v1.md`; `docs/design/operation-stage-entry-v1.md` | 021 |

## Dependency graph and checkpoints

```text
001 -> 002 -> 003 -> 004 -> 005 -> 006
006 -> {007, 007A, 008, 009} -> 010
010 -> {011, 012}
{005, 010} -> 013
{011, 012, 013} -> 014 -> 015 -> 016 -> 017 -> 018 -> 019 -> 020 -> 021 -> 022
```

1. **Contract checkpoint after 001:** owner decisions, exact version/source identities, and the
   complete identity-bearing file inventory freeze.
2. **Non-mergeable identity-migration checkpoint after 002-010 (including 007A):** ruleset/setup/create/snapshot
   contracts and every Core/Exercise checked consumer move together. Focused tests drive each task;
   the full Weather and Exercise gates must be green at 010 before any slice is merged.
3. **Foundation checkpoint after 011-014:** action/event goldens and existing system action
   characterization pass before transition behavior.
4. **Authority checkpoint after 015-019:** exact sequence, zero-event negatives, act-first/last
   Reserve cutoff, and replay projection pass.
5. **Completion checkpoint after 020-022:** Core/solution/`just check`, Weather and Exercise
   checkpoint regression gates, documentation traceability, and independent implementation review
   pass.

## Requirement-to-task traceability

| Requirement group | Owning tasks/checkpoint | Required evidence |
| --- | --- | --- |
| `STG-001`-`004` | 003-010 / identity migration | policy/setup/creation/snapshot strict goldens, source/pair/hash/order/old-version matrices, all identity consumers migrated |
| `STG-005`-`016` | 011-019 / foundation and authority | exact system candidates, action/event codecs, membership/stale/order/policy/successor tests, one-event transitions |
| `STG-020`-`024` | 014-020 / authority and completion | exact four-event ordering, unsupported-pair checks, convoy distinction, derived Reserve audience, deterministic/public-surface end-to-end evidence |
| `STG-017`-`019` | 015-020 / authority and completion | authority-preservation digest, forged-history replay, fog byte pairs, public-surface/reference tests |
| `STG-025` | 001-010, 013 / identity and foundation | exact ruling/protecting IDs/source sets, ruleset-hash golden, retained creation/snapshot/event/replay/Exercise identities |
| `STG-NFR-001` | 002-013 / identity and foundation | strict/canonical contract matrices, version/unknown/reordered rejection, and culture/collection-order equality |
| `STG-NFR-002` | 002-020 / identity through completion | canonical identity/action/event evidence plus accepted-transition, projection, fresh-replay, culture, collection-order, and byte-equality checks |
| `STG-NFR-003`-`005` | 014-020 / authority and completion | public-surface, fog invariance, deterministic unit/end-to-end/replay evidence |
| `STG-NFR-006` | 001-010 including 007A, and every later task; 022 / all checkpoints | frozen maximum-five-file map; focused GREEN at 002-003; recorded non-green inventory/source evidence at 004-009; full GREEN at 010; focused RED/GREEN and checkpoint gates thereafter |
| `STG-AC-001` | 003-004, 011-015, 020 / checkpoint 022 | admitted policy plus exact system/empty-side action sets at Organization in both setups |
| `STG-AC-002` | 012-020 / checkpoint 022 | four candidate/event contracts and exact accepted transition sequence |
| `STG-AC-003` | 015-020 / checkpoint 022 | per-transition authority-preservation digest |
| `STG-AC-004` | 003-006, 014-020 / checkpoint 022 | policy/source mutation at creation, projection, query, and submission |
| `STG-AC-005` | 011-020 / checkpoint 022 | stale, duplicate, audience, position, and ordering rejection with zero events |
| `STG-AC-006` | 005-007, 013, 015-020 / checkpoint 022 | strict event/history identity plus skipped, reordered, pair, and successor forgery rejection |
| `STG-AC-007` | 019-020 / checkpoint 022 | semantic and byte-equal side views under hidden-opponent mutation |
| `STG-AC-008` | 018-020 / checkpoint 022 | act-last first-side audience, null authoritative active side, unchanged initiative, and `GetNext` |
| `STG-AC-009` | 003, 006, 014, 020 / checkpoint 022 | strict unsupported-kind decoding and unsupported pair/stage/turn rejection |
| `STG-AC-010` | 005-010, 013-020 / checkpoint 022 | migrated creation/snapshot identities, four event bytes, and fresh-session replay equality |
| `STG-AC-011` | 020 / checkpoint 022 | public-surface and production-reference boundary evidence |
| `STG-AC-012` | 018-020 / checkpoint 022 | exact Reserve authority position, derived audience, empty system set, and no Reserve behavior |

### Decision-to-requirement traceability

| Decision | Requirements / acceptance protected | Implementation tasks | Review evidence at Task 001 |
| --- | --- | --- | --- |
| `STG-DEC-001` | `STG-001`-`002`, `STG-021`; `STG-AC-001`, `009` | 003-004, 014-020 | Exact two-setup, Stage 1 policy and fail-closed pair boundary frozen. |
| `STG-DEC-002` | `STG-001`-`004`; `STG-AC-004`, `009` | 003-010, 014-018 | Four independent assertions, exact source, ordinals, and field order frozen. |
| `STG-DEC-003` | `STG-005`-`016`, `STG-020`; `STG-AC-002`, `005` | 011-020 | Four action/command/event identities, replay closure, and no generic bypass frozen. |
| `STG-DEC-004` | `STG-005`-`006`; `STG-AC-001`-`003` | 014-015, 020 | Organization remains one empty barrier; positive ordering is absent. |
| `STG-DEC-005` | `STG-009`-`012`, `STG-016`; `STG-AC-002`, `006` | 013-018, 020 | Assignment and Repair retain distinct positions and events. |
| `STG-DEC-006` | `STG-012`, `019`, `023`; `STG-AC-008`, `012` | 018-020 | Exact null-active-side Reserve successor and derived audience frozen. |
| `STG-DEC-007` | `STG-021`-`022`; `STG-AC-009` | 014, 020 | Later-stage/general-convoy reuse explicitly rejects. |
| `STG-DEC-008` | `STG-002`-`003`, `021`; `STG-AC-004`, `009` | 003-004, 014, 020 | Synthetic evidence and recognized unsupported kind are explicit. |
| `STG-DEC-009` | `STG-025`; `STG-AC-001`, `002`, `004`-`006`, `009`-`010` | 002, 007-010, 013, 020 | Exact ruling, alternatives, protecting IDs, and sources frozen. |
| `STG-DEC-010` | `STG-013`-`015`; `STG-AC-001`, `005` | 011-014, 020 | Existing Legal Actions versions/field ownership stay unchanged. |
| `STG-DEC-011` | `STG-021`; `STG-AC-009` | 003, 014, 020-022 | Test claims stop at modeled policy-kind negatives. |

### Task 001 acceptance and verification

- **Acceptance:** the merged predecessor commit is recorded; all eleven decisions have an explicit
  recommended disposition; every changed or intentionally unchanged contract has an exact version;
  the new enum, strings, action IDs, field order, and source identities are fixed; the file map has
  no task above five material files; and each requirement/decision reaches a task and evidence gate.
- **Verification:** rerun repository `rg` inventories for constructors, serializers, strict version
  checks, hash literals, and Exercise snapshot decoding; check all three planning documents for
  conflict; run Markdown whitespace/diff checks; and complete the bounded independent review cycle.
- **Status:** complete. `EXR-TASK-014` is merged, the owner accepted all eleven decisions, and final
  independent review instance 3 of 3 returned `Ready` with no actionable P0-P3 findings.

## Risks and stop conditions

| Risk | Stop condition / mitigation |
| --- | --- |
| Missing data becomes implicit zero | Stop on any policy default, nullable omission, or generator that advances without exact admitted assertion. |
| Generic system dispatch becomes a bypass | Preserve existing action characterization and require mechanic-specific candidate/event types plus independent factory checks. |
| Positive Organization semantics leak into empty v1 | Stop if a segment order, attachment, project, training, or movement restriction is inferred or mutated. |
| Fleet assignment authority is blurred | Empty resolution is system-only only because policy proves no choice; positive assignment remains Commonwealth-owned and unsupported. |
| Reserve is accidentally completed | Fleet Repair successor may expose the Reserve position/audience only; any Reserve command/event/action blocks the slice. |
| Contract migration grows beyond reviewable size | Split setup, creation, snapshot, action, and event families before implementation; never combine clean-cut churn with positive behavior. |
| Task 3.2 dependency is hidden | Reject any checkpoint lacking valid Stage 1 order or exact policy; later-stage/general convoy work remains a named predecessor. |

## Rollback

Before release, rollback is ordinary branch/commit removal. After a clean-cut version ships, the old
executable remains the historical reader for the old contract family; do not make the new executable
accept missing policy. A failed implementation review rolls back all Stage Entry candidates/events
and retains the current Weather-at-Organization boundary. No campaign should be migrated partially.

## Review challenge points

- Is one combined policy with four assertions clearer and safer than four independent policy values?
- Are four empty events necessary evidence or excessive ceremony?
- Does semantic system-action dispatch preserve every current canonical action byte?
- Can Fleet Repair reach the correct first-side Reserve position without creating any new state?
- Does any proposed public/internal type reopen the sealed authority boundary?
- Are clean-cut contract migrations separated enough to remain reviewable?
- Does the Stage 1-only policy make every deferred Task 3.2 dependency explicit and fail closed?

## Prior independent planning review

Three bounded fresh-context passes concluded with a **Ready** verdict and no unresolved P0/P1
finding. Reconciliation fixed the Reserve catalog/audience model, preserved Legal Actions v1
binding, assigned the ruleset ruling and every identity consumer, added the executable
`HasObligations` rejection boundary, moved the audience resolver to Campaigns, and replaced
conditional slices with the non-mergeable 23-task plan above. The final P2/P3 follow-ups are also
incorporated: Task 006 owns embedded-policy serialization and checkpoint states 1–10, Task 005 owns
creation version/dispatch, and semantic dispatch is Task 011. Readiness does not authorize
implementation; the specification completion gate and owner decision still apply.

A later combined implementation-and-plan independent review found additional current-repository
consumers after the original planning review. The corrected map now assigns the opening-preamble
setup constructor to Task 004, separates Core and observation identity migration across Tasks 007
and 007A, assigns direct setup/snapshot constructors and current-version assertions, and gives Task
010 ownership of the strict ExerciseRunner snapshot decoder plus semantic-reader evidence. Task 001
must rerun this exhaustive inventory before freezing versions and files. Final-product review
instance 3 of 3 confirmed the corrected plan is implementation-ready as a gated plan. Its only
remaining P2 concerned Exercise early-profile readback, not this task graph, and that implementation
follow-up was corrected afterward. Those historical reviews did not inspect this post-merge Task
 001 freeze. Owner approval still gates implementation.

### STG-TASK-001 review cycle

The post-merge freeze used the maximum three bounded fresh-context instances. Instances 1 and 2
returned `Not ready` without P0/P1 findings; all findings were accepted and corrected. Reconciliation
completed completion-lane and acceptance-scenario traceability, separated event record parameters
from canonical JSON, made the hash inventory reproducible, removed stale unfrozen-shape language,
fixed Stage Entry candidates to null/omitted `OperationStage`, and made the atomic identity-lane
evidence contract executable. Final instance 3 of 3 returned **Ready** with no actionable P0-P3
findings. The project owner accepted `STG-DEC-001` through `STG-DEC-011` and this freeze as written
on 2026-08-24. `STG-TASK-001` through `STG-TASK-019` plus `STG-TASK-014A` are complete: the
clean-cut identity lane is green, all four Stage Entry candidate/event/command contracts have strict
evidence, semantic system membership and mapping are wired, and ExerciseRunner failure fixtures no
longer depend on the moving Core terminal boundary. Organization now advances through one exact
event to Naval Convoy Arrival with projection/replay recomputation and authority preservation;
Naval Convoy Arrival now also advances through one exact event to Fleet Assignment without
inventing arrival or logistics facts. Fleet Assignment advances through its own payload-free event
to Fleet Repair without inventing ships or a player choice. Fleet Repair now reaches the exact
catalog first-acting-side Reserve position while retaining a null stored active side and exposing no
Reserve action. One pure pair-keyed campaign resolver now supplies both observation and legal-action
projection; act-first and act-last histories expose the correct source-free Reserve audience while
authority remains catalog-exact and `GetNext` remains valid. Completion of `STG-TASK-019` made
`STG-TASK-020` dependency-eligible. Authority-checkpoint review instance 3 of 3 found no production
or test defect and one P2 planning-state inconsistency because the pre-review record called Task 020
authorized rather than eligible. That finding is accepted and reconciled; this post-review record
now authorizes `STG-TASK-020` under the dependency graph and evidence checkpoints above. The
implementation foundation review found one
P2 Stage Entry codec strictness defect; the accepted two-file correction passed review instance 2
of 3 with no actionable findings before Task 015 began.
The owner-requested implementation checkpoint was the one-off fourth and final review instance; it
returned **Ready with non-blocking follow-ups** for Tasks 002-003, with no P0-P2 findings.

`STG-TASK-020` is complete. Both admitted setups reach Reserve through the exact ten-event history
and current action succession; canonical event round-trips reproduce byte-identical authority and
legal-action sets at every prefix. Opponent-only changes leave both side observations and side
legal-action bytes identical through Reserve, raw Stage Entry authority remains internal and absent
from DecisionWorker/Intelligence source, and the legacy generic command remains rejected at every
Stage Entry checkpoint. `STG-TASK-021` is authorized.

`STG-TASK-021` is complete. `README.md`, the pre-alpha roadmap, naming overview, and architectural
rationale now distinguish the engine's delivered explicit-empty path to Reserve from the checked
Exercise fixture's intentionally earlier Organization stop. They identify snapshot contract 6,
ruleset manifest contract 4, setup schema 5, the four mechanic-specific Stage Entry events, null
authoritative `ActiveSide` with derived Reserve audience, and Reserve as the next engine package.
The contradiction scan and repository diff check are clean. `STG-TASK-022` is authorized.

`STG-TASK-022` closes the package. The already-obtained final authority review (instance 3 of 3)
found no production or test defect; its sole P2 planning-state finding was accepted and corrected
before Task 020 began. No additional review instance was created. Final `just check` restored the
solution, passed format verification, built with 0 warnings and 0 errors, and passed 619/619 tests
with no failures or skips. All Stage Entry v1 tasks and evidence checkpoints are complete; Reserve
remains explicitly out of scope and is the next authoritative-engine planning package.
