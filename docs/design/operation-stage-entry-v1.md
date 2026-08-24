# Operation-Stage Entry v1 Technical Design

**Status:** Planning review Ready; owner decision and Task 001 contract freeze required; implementation not authorized

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

Implementation is gated on the owner disposition of `STG-DEC-001` through `STG-DEC-011`. The design
applies them as follows:

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

## Contract sketches

These are design shapes, not frozen C# signatures. Phase 0 freezes exact names, numeric versions,
field order, action identifiers, and enum ordinals against the then-current main branch.

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
campaign/state/ruleset/position/audience; the candidate carries only contract version, action ID,
kind, and the existing optional operation-stage field. Submission preserves Legal Actions v1:
contract version, campaign ID, expected state/position, audience, and opaque action ID. It does not
duplicate ruleset hash or kind. The current position and admitted setup policy contain all required
semantics. Unknown or payload-bearing variants reject.

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
to at most five material files with focused RED/GREEN evidence. Generated goldens/fixtures do not
authorize unrelated refactoring.

The file lists below are the frozen anticipated material boundaries. Task 001 must revise this plan
before implementation if rebase drift makes any boundary inaccurate; there are no conditional
“split later” tasks.

| Task | Outcome and evidence | Material files (maximum five) | Depends on |
| --- | --- | --- | --- |
| `STG-TASK-001` | Freeze owner decisions, versions, enum ordinals, field order, sources, file map, and complete traceability; rerun an exhaustive constructor/serializer/version/hash consumer inventory before freezing the map. | `docs/research/operation-stage-entry-spike.md`; `docs/specs/operation-stage-entry-v1.md`; `docs/design/operation-stage-entry-v1.md` | accepted `EXR-TASK-014`; owner approval |
| `STG-TASK-002` | Add the exact ruling, migrate ruleset version/hash, and update strict manifest goldens. | `src/Cna.Core/Rules/Cna1979Ruleset.cs`; `src/Cna.Core/Rules/RulesetManifest.cs`; `tests/Cna.Core.Tests/Rules/RulesetManifestTests.cs`; `tests/Cna.Core.Tests/Rules/StageEntryRulingTests.cs` | 001 |
| `STG-TASK-003` | Define and strictly encode the closed pair-bound policy; verify malformed, source-order, culture, and unsupported-kind matrices. | `src/Cna.Core/Setups/CampaignStageEntryPolicy.cs`; `src/Cna.Core/Setups/CampaignStageEntryPolicyCodec.cs`; `tests/Cna.Core.Tests/Setups/CampaignStageEntryPolicyTests.cs` | 002 |
| `STG-TASK-004` | Bind policy into setup definition/catalog/hash and both synthetic setup goldens; migrate the opening-preamble definition constructor with no compatibility default. | `src/Cna.Core/Setups/CampaignSetupDefinition.cs`; `src/Cna.Core/Setups/Cna1979SetupCatalog.cs`; `src/Cna.Core/Setups/CampaignSetupHash.cs`; `tests/Cna.Core.Tests/Setups/CampaignSetupTests.cs`; `tests/Cna.Core.Tests/Campaigns/OpeningPreambleCampaignTests.cs` | 003 |
| `STG-TASK-005` | Clean-cut create-command/event versions, event dispatch, and command admission; old versions reject. Embedded setup-policy encoding remains Task 006. | `src/Cna.Core/Campaigns/CampaignCommand.cs`; `src/Cna.Core/Campaigns/CampaignEvent.cs`; `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignEventSerializer.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignCreationAdmissionTests.cs` | 004 |
| `STG-TASK-006` | Own strict embedded-policy writing/parsing and omitted/default rejection across creation/snapshot bytes; projection/replay never invent policy. Expand checkpoint validation to exact catalog states 1–10 so Tasks 015–018 can project successors; Task 018 adds the Reserve-specific invariant. | `src/Cna.Core/Campaigns/CampaignSetupSnapshot.cs`; `src/Cna.Core/Campaigns/CampaignSnapshotSerializer.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `src/Cna.Core/Campaigns/CampaignSnapshotValidator.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignReplayPreparationTests.cs` | 005 |
| `STG-TASK-007` | Migrate Core creation/replay identity goldens, direct setup-snapshot constructors, and current-version assertions to the accepted contracts and identities. | `tests/Cna.Core.Tests/Campaigns/CampaignEventSerializationTests.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignReplayTests.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignTests.cs`; `tests/Cna.Core.Tests/Campaigns/Version3AuthorityTests.cs` | 006 |
| `STG-TASK-007A` | Migrate observation identity goldens and the shared observation setup/snapshot constructor helper to the accepted contracts and identities. | `tests/Cna.Core.Tests/Observations/CampaignObservationSerializationTests.cs`; `tests/Cna.Core.Tests/Observations/Fixtures/campaign-observation-axis.v1.golden.json`; `tests/Cna.Core.Tests/Observations/CampaignObservationTestData.cs` | 006 |
| `STG-TASK-008` | Migrate all checked Exercise/Maneuver manifests and their governing identity examples to the accepted identities and exact canonical bytes. | `scenarios/exercises/rules-lab.organization.v1.json`; `scenarios/exercises/rules-lab.organization.baseline.v1.json`; `scenarios/maneuvers/rules-lab.serial.v1.json`; `docs/specs/exercise-harness-v1.md`; `docs/specs/weather-determination-v1.md` | 006 |
| `STG-TASK-009` | Migrate ExerciseRunner contract/report test literals to the accepted identities. | `tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseManifestCodecTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ManeuverManifestCodecTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ManeuverReportCodecTests.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ManeuverReportLifecycleTests.cs`; `tests/Cna.ExerciseRunner.Tests/Execution/ManeuverExecutorTests.cs` | 006 |
| `STG-TASK-010` | Migrate ExerciseRunner's strict snapshot decoder contract and remaining Exercise CLI fixture identity, with focused semantic-reader version rejection/acceptance evidence; then close the non-mergeable identity lane with full Weather/Exercise gates. | `src/Cna.ExerciseRunner/Artifacts/ExerciseEvidenceCodec.cs`; `tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseBundleSemanticValidatorTests.cs`; `tests/Cna.ExerciseRunner.Tests/Commands/ManeuverRunCommandTests.cs` | 007, 007A, 008-009 |
| `STG-TASK-011` | Characterize then make system dispatch semantic while preserving existing Initiative/convoy/Weather bytes. | `src/Cna.Core/Actions/CampaignLegalActions.cs`; `tests/Cna.Core.Tests/Actions/CampaignLegalActionsTests.cs` | 010 |
| `STG-TASK-012` | Add four closed payload-free candidates and strict canonical candidate codec evidence. | `src/Cna.Core/Actions/CampaignActionCandidate.cs`; `src/Cna.Core/Actions/CampaignLegalActionSerializer.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryActionContractTests.cs` | 010 |
| `STG-TASK-013` | Add four distinct event contracts and strict canonical event codec evidence with exact per-mechanic sources. | `src/Cna.Core/Campaigns/StageEntryEvents.cs`; `src/Cna.Core/Campaigns/CampaignEventSerializer.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryEventContractTests.cs` | 005, 010 |
| `STG-TASK-014` | Wire exact system membership, commands, and candidate-to-command mapping; unsupported policy/pair/audience yields zero executable events. | `src/Cna.Core/Actions/CampaignLegalActions.cs`; `src/Cna.Core/Actions/CampaignActionExecution.cs`; `src/Cna.Core/Campaigns/StageEntryCommands.cs`; `tests/Cna.Core.Tests/Actions/CampaignStageEntryMembershipTests.cs` | 011-013 |
| `STG-TASK-015` | Organization vertical transition: exact policy/factory/projection, one event, authority preservation, and forged-history negatives. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryOrganizationTests.cs` | 014 |
| `STG-TASK-016` | Arrival vertical transition with exact successor/source and no invented arrivals or logistics facts. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryArrivalTests.cs` | 015 |
| `STG-TASK-017` | Fleet Assignment vertical transition, separate from Repair and with no invented ships/choice. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryFleetAssignmentTests.cs` | 016 |
| `STG-TASK-018` | Fleet Repair reaches the exact catalog Reserve position with null stored active side and no Reserve action. | `src/Cna.Core/Campaigns/CampaignEngine.cs`; `src/Cna.Core/Campaigns/CampaignProjector.cs`; `src/Cna.Core/Campaigns/CampaignSnapshotValidator.cs`; `tests/Cna.Core.Tests/Campaigns/StageEntryFleetRepairTests.cs` | 017 |
| `STG-TASK-019` | One pure campaign-authority resolver serves legal actions and observations; act-first/last and `GetNext` continuity pass. | `src/Cna.Core/Campaigns/FirstActingSideResolver.cs`; `src/Cna.Core/Actions/CampaignLegalActions.cs`; `src/Cna.Core/Observations/CampaignObservationProjector.cs`; `tests/Cna.Core.Tests/Observations/CampaignReserveAudienceTests.cs` | 018 |
| `STG-TASK-020` | Close two-setup end-to-end, fresh replay, hidden-opponent byte invariance, and public-surface evidence. | `tests/Cna.Core.Tests/Campaigns/StageEntryCampaignTests.cs`; `tests/Cna.Core.Tests/Campaigns/CampaignReplayPreparationTests.cs`; `tests/Cna.Core.Tests/Observations/CampaignObservationPrivacyTests.cs`; `tests/Cna.Core.Tests/Actions/CampaignLegalActionsTests.cs`; `tests/Cna.Core.Tests/Campaigns/AuthorityBoundaryTests.cs` | 019 |
| `STG-TASK-021` | Reconcile the governing repository map, roadmap, naming, and architectural rationale with delivered behavior. | `README.md`; `docs/roadmap/pre-alpha-roadmap.md`; `naming-overview.md`; `tech-design.md` | 020 |
| `STG-TASK-022` | Reconcile planning statuses/evidence, run Core/solution/`just check`, and obtain independent implementation review. | `docs/research/operation-stage-entry-spike.md`; `docs/specs/operation-stage-entry-v1.md`; `docs/design/operation-stage-entry-v1.md` | 021 |

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
| `STG-005`-`016`, `STG-020`-`024` | 011-019 / foundation and authority | exact system candidates, action/event codecs, membership/stale/order/policy/successor tests, one-event transitions |
| `STG-017`-`019` | 015-020 / authority and completion | authority-preservation digest, forged-history replay, fog byte pairs, public-surface/reference tests |
| `STG-025` | 001-010, 013 / identity and foundation | exact ruling/protecting IDs/source sets, ruleset-hash golden, retained creation/snapshot/event/replay/Exercise identities |
| `STG-NFR-001`-`002` | 002-013 / identity and foundation | strict/canonical contract matrices and cross-process/culture equality |
| `STG-NFR-003`-`005` | 014-020 / authority and completion | public-surface, fog invariance, deterministic unit/end-to-end/replay evidence |
| `STG-NFR-006` | 001-010 including 007A, and every later task; 022 / all checkpoints | frozen maximum-five-file map; focused per-slice RED/GREEN; full Weather/Exercise gates at identity checkpoint 010 and every later checkpoint |
| `STG-AC-001`-`012` | 014-022 / completion | two-setup end-to-end cases, act-first/last Reserve audience, `GetNext`, fresh-session replay, full repository gate |

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

## Independent planning review

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
follow-up was corrected afterward. Owner approval and Task 001 still gate implementation.
