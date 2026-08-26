# Reserve Designation v1 Technical Design

**Status:** Implemented through `RES-TASK-015`; final repository gate and independent review in
progress

**Date:** 2026-08-24

**Specification:** [Reserve Designation v1](../specs/reserve-designation-v1.md)

**Research:** [Reserve Designation v1 source and contract spike](../research/reserve-designation-spike.md)

## Design summary

Add Reserve status to authoritative campaign element state and owner-only observations. At the
current first-player Reserve Designation checkpoint, derive the acting side from retained
operation-stage order, expose one subject-bearing legal candidate for each eligible own element,
and expose one payload-free completion candidate. A designation emits one event, changes exactly
one element to Reserve I, and remains at the current sequence position. Completion emits a separate
event and advances to the exact first-side Movement successor.

The design keeps the existing action-set/submission/receipt envelope. The selected `elementId` is
part of designation candidate semantics and therefore part of its opaque action ID. Replay rebuilds
both events from prior authority. The checkpoint validator becomes phase-aware so multiple accepted
events at one position remain finite and exact. Checked Exercise/Maneuver profiles use a dedicated
semantic, stateless controller to designate every eligible element before completion.

## Authority and control flow

```text
CampaignSnapshot + ContentContext
  -> validate exact Reserve checkpoint
  -> resolve first acting side from CampaignOperationStageOrder
  -> project only that side's own elements + reserve status
  -> generate current candidates
       designate-reserve(elementId) [0..N]
       complete-reserve-designation [1]
  -> submit opaque current action ID
  -> re-query exact membership
  -> map candidate to typed command
  -> CampaignEngine adjudication
       designation: validate owner/status -> one event -> same position
       completion: validate acting side -> one event -> Movement
  -> CampaignProjector recomputation
  -> receipt + Chronicle/Exercise evidence
```

No raw player text, model output, Intelligence proposal, remote I/O, or UI state enters authority.

## Module ownership

| Module | Owns | Must not own |
| --- | --- | --- |
| `Cna.Core.Rules` | Normalized Reserve Designation artifact, empty-selection interpretation, source identity, ruleset hash | Runtime selection or UI wording |
| `Cna.Core.Campaigns` | Reserve state, commands, events, eligibility checks, adjudication, projection/replay, bounded checkpoint validation | Client draft state or model inference |
| `Cna.Core.Actions` | Subject-bearing candidates, canonical IDs, side-safe current membership, candidate-to-command mapping | Hidden opposing state or transition authority |
| `Cna.Core.Observations` | Owner-only reserve status and unchanged opponent privacy policy | Ruleset sources or opponent status |
| `Cna.ExerciseRunner` | Deterministic test controller and trusted evidence/report compatibility | New game rules or privileged bypasses |
| Documentation/tests | Frozen contracts, traceability, deterministic evidence, roadmap truth | Unsupported future lifecycle claims |

## Proposed version matrix

Final digest values are implementation outputs and must not be fabricated in planning.

| Contract or identity | Merged value | Reserve v1 value | Reason |
| --- | ---: | ---: | --- |
| `Cna1979Ruleset.ContractVersion` | 4 | **5** | Adds normalized Reserve authority and the empty-selection interpretation. |
| `CampaignWorldSnapshot` | 1 | **2** | Each serialized element gains explicit `reserveStatus`. |
| `CampaignSnapshot` | 6 | **7** | Embedded world contract and checkpoint semantics change. |
| `CreateCampaign` / `CampaignCreated` | 5 | **6** | Creation history embeds world v2 and rejects the prior family. |
| `CampaignObservation` | 2 | **4** | Contract 3 added owner Reserve status; contract 4 makes its revision audience-visible so hidden opposing designations do not leak a count. |
| `CampaignElementReserveStatus` | absent | **1 enum shape** | Closed `None`, `ReserveI`, `ReserveII` authority vocabulary. |
| `CampaignObservationReserveStatus` | absent | **1 enum shape** | Distinct public observation vocabulary; authority types remain internal. |
| Reserve commands | absent | **1 each** | New internal mechanic-specific commands. |
| Reserve events | absent | **1 each** | New canonical history evidence. |
| `CampaignActionCandidate` | 1 | **unchanged** | A new discriminated candidate kind does not alter existing-kind semantics. |
| Legal-action set / submission / receipt | 1 / 1 / 1 | **2 / 1 / 1** | Set v2 carries an audience-visible revision; active submissions and authoritative receipts retain their v1 authority binding. |
| Setup/content schemas | 5 / current | **unchanged** | Reserve is mutable world state; all represented phasing-side units are eligible. |
| Land sequence/catalog | 2 / 2 | **unchanged** | Reserve and Movement positions already exist in correct order. |
| Observation policy ID | `own-elements-only.v1` | **`own-elements-only.v2`** | Owner-only fields remain; the new policy also removes hidden-event count leakage from the opponent revision. |
| Exercise Manifest | 1 | **2** | Closed controller policy vocabulary and supported controller semantics expand. |
| Maneuver Manifest | 1 | **2** | Embedded Exercise run definitions admit the new controller policy. |
| Exercise controller configuration identity | 1 / `sandtable.exercise-controller-configuration.v1` | **2 / `sandtable.exercise-controller-configuration.v2`** | The hashed policy vocabulary and behavior expand. |
| Exercise controller candidate view | absent | **1** | New runtime-only closed semantic view carries action ID, kind, and optional element ID. |
| Exercise evidence/report envelopes | 1 | **unchanged** | Shapes remain stable; embedded identities, hashes, events, and checkpoint versions migrate. |

The migration is a clean cut. Current code has no persisted production history compatibility layer;
the implementation accepts only the right-hand versions and updates checked fixtures, evidence
decoders, hashes, and documentation together.

The Task 016 final-review correction preserves authoritative snapshot/event versions and finite
replay arithmetic. Observation v4 projects `stateVersion` as an audience-visible revision: the
owner receives the exact authoritative version, while the opponent receives authoritative version
minus the hidden Reserve-I count at Reserve and Movement. Legal-action-set v2 copies the projected
revision for side audiences. An active candidate set must still equal the authoritative checkpoint
revision before the Exercise controller may submit it; an inactive empty set may carry the older
audience revision but may never exceed authority. This policy is intentionally bounded to the
delivered terminal Movement slice and must be extended contract-first before later side-active
stages are exposed.

## Frozen state shape proposal

```csharp
internal enum CampaignElementReserveStatus
{
    None = 0,
    ReserveI = 1,
    ReserveII = 2,
}

internal sealed record CampaignElementState(
    string ElementId,
    string CurrentLocationId,
    CampaignElementReserveStatus ReserveStatus);
```

Canonical world element order is:

```text
elementId, currentLocationId, reserveStatus
```

Canonical values are `none`, `reserve-i`, and `reserve-ii`. Initial creation explicitly writes
`none`; missing, null, numeric, wrong-case, or unknown values reject. Runtime validation admits only
defined values but the Reserve v1 transition factory emits only `ReserveI`.

The public observation type is frozen as:

```csharp
public enum CampaignObservationReserveStatus
{
    None = 0,
    ReserveI = 1,
    ReserveII = 2,
}
```

`ObservedOwnElement.ReserveStatus` uses this distinct observation-owned enum; the authoritative
enum remains internal. Construction and serialization reject undefined numeric values. Record
equality/hash behavior includes the property. Canonical strings are `none`, `reserve-i`, and
`reserve-ii`, appended as `reserveStatus` after `currentLocationId`. The observation contract
changes to v3; the policy ID does not. V1 projection emits only `None` and `ReserveI`.

## Ruleset identity

Add a normalized artifact with a stable identity such as
`cna-1979.1.reserve-designation` and canonical data for:

- eligible owner: resolved phasing/first side;
- assignment timing: Reserve Designation;
- assignment result: Reserve I;
- assignment cost: zero Capability Points; and
- v1-supported transition: `None → ReserveI`.

Its sources include `spi-1979-land-rules:5.2.reserve-designation`, `18.11`, `18.12`, `18.15`,
`18.21`, and `18.26`.

Task 002 freezes the artifact identity as `cna-1979.1.reserve-designation`, schema v1, with exact
canonical property order:

```text
schemaVersion, eligibleOwner, assignmentTiming, assignmentResult,
capabilityPointCost, supportedTransition { from, to }, sources
```

The exact values are `resolved-first-acting-side`, `reserve-designation`, `reserve-i`, zero,
and `none → reserve-i`. Its implementation-derived content hash is
`sha256:3d5fb13758e4539a14c89f0c884abd230f8a3a14f0e57be68aa914716278c0ca`.

Because the source states permission but no minimum, add an adopted ruling:

```text
ruling: cna-1979.1.ruling.empty-reserve-designation
conflict: cna-1979.1.conflict.reserve-designation-minimum
alternatives:
  - require-at-least-one-reserve-designation
  - allow-empty-reserve-designation
selected: allow-empty-reserve-designation
protects: RES-AC-002, RES-AC-006, RES-AC-009
sources: spi-1979-land-rules:18.11, spi-1979-land-rules:5.2.reserve-designation
```

The artifact/ruling changes the canonical ruleset hash but not setup or content hashes.
The historical implementation-derived ruleset v5 hash after Reserve Task 002 was
`beb66b242222f1ccc8bde4a34daacfcd561495b47e3d48391ede34e16830d6e6`.

## Candidate contract and IDs

Refactor the candidate base's private semantic writer so a closed subtype may add subject fields
without changing existing candidates. Exact designation semantics are:

```json
{"contractVersion":1,"kind":"designate-reserve","elementId":"axis-element-a"}
```

Exact completion semantics are:

```json
{"contractVersion":1,"kind":"complete-reserve-designation"}
```

The public legal-action serializer adds `elementId` only for `DesignateReserveAction`. It must fail
closed for any unsupported payload-bearing subtype. Existing action ID goldens must remain byte-for-
byte unchanged after the refactor.

Side candidate generation runs only when:

- the observation is current and belongs to the audience;
- the position is Operation Stage 1 first-player Reserve Designation;
- its derived active side equals the observer; and
- each designation subject is present in `OwnElements` with status `None`.

The completion candidate is included even when there are zero element candidates. System and the
other side receive empty sets.

## Commands, events, and projection

### Commands

```csharp
DesignateReserveElement(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide ActingSide,
    string ElementId)

CompleteReserveDesignation(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide ActingSide)
```

Both are internal contract v1 commands. Candidate-to-command mapping derives the side from the
submitting audience and the element from the current candidate; it accepts no separate client
payload.

### Events

`ReserveElementDesignated` canonical order:

```text
contractVersion, eventType, campaignId, stateVersion, fromPositionId,
gameTurn, operationStage, actingSide, elementId, priorStatus,
resultingStatus, sequencePosition, sources
```

`ReserveDesignationCompleted` canonical order:

```text
contractVersion, eventType, campaignId, stateVersion, fromPositionId,
gameTurn, operationStage, actingSide, sequencePosition, sources
```

Stable event types are `reserve-element-designated` and `reserve-designation-completed`.
Designation sources are Sections 18.11, 18.12, and 18.15; completion sources are the sequence
locator plus the empty-selection ruling. Sources use normal canonical ordering.

### Adjudication invariants

For designation, the event factory must:

1. validate the complete current snapshot/context;
2. require exact Reserve position and resolved first side;
3. require command bindings to current state/position/side;
4. locate exactly one independent represented content/world element;
5. require content ownership by acting side and current `None` status;
6. preserve the current sequence position;
7. emit `None → ReserveI` and state version `current + 1`; and
8. consume no random value or Capability Point.

For completion, it must:

1. validate the same current checkpoint and side binding;
2. allow selected count from zero through the current eligible bound;
3. recompute `Cna1979LandSequence.GetNext(current)`;
4. require first-side Movement for the same game turn/stage;
5. preserve the world and all unrelated authority; and
6. emit state version `current + 1`.

Projection recomputes the expected event and compares canonical bytes before applying it. A
designation replaces one immutable world element in canonical element-ID order. Completion changes
only state version and sequence position.

## Structural and context-authoritative validation

Let:

- `B = 10`, the merged state version at initial first-side Reserve;
- `N = count of represented independent elements owned by the resolved first side`; and
- `Rw = count of all world elements whose status is ReserveI`; and
- `R = count of resolved first-side represented independent elements whose status is ReserveI`.

Validation is deliberately layered.

`CampaignSnapshotValidator.IsLocallyValid` remains context-free and is the gate used by strict
snapshot parsing/canonical round-trip. It proves:

- exact contract versions and structural fields;
- defined Reserve status values, stable unique element IDs, and stable location IDs;
- every pre-Reserve checkpoint retains status `None` and its existing exact position/state mapping;
- at Reserve, `Rw = stateVersion - B`; at Movement, `Rw = stateVersion - B - 1`;
- `Rw` is nonnegative and does not exceed total world
  element count; and
- no admitted Reserve/Movement snapshot contains `ReserveII`.

This layer may accept a structurally coherent but ownership-invalid decoded snapshot. Deserialization
does not create an authority handle and must not be treated as authoritative admission.

`CampaignSnapshotValidator.IsValid(snapshot, context)` performs context-authoritative validation.
It first requires local validity, then exact content/setup/world membership, represented independent
placement, retained first-side resolution, `Rw = R`, and the owner-specific bound `R ≤ N`. At both Reserve and
Movement, only represented independent elements owned by the resolved first side may be
`ReserveI`; all other elements must be `None`; no element may be `ReserveII`.

The authority consumers are exact:

| Consumer | Required validation |
| --- | --- |
| Snapshot parse/canonical round-trip | Structural/local only; result remains non-authoritative. |
| Campaign authority handle, legal-action query, command execution, and observation projection | Context-authoritative. |
| Event deserialization | Strict event structure/canonical bytes only; no prior snapshot is available. |
| Event projection and replay | Context-authoritative prior state plus recomputed expected event and authoritative successor. |

For the implemented current slice, context-authoritative checkpoint invariants are:

| Position | Required invariant |
| --- | --- |
| Any pre-Reserve checkpoint | Existing exact position/state invariants remain unchanged; all reserve statuses are `None`. |
| First-side Reserve Designation | `stateVersion = B + R`, `0 ≤ R ≤ N`, only acting-side elements may be `ReserveI`, no element is `ReserveII`. |
| First-side Movement | `stateVersion = B + R + 1`, `0 ≤ R ≤ N`, only first-side represented independent elements may be `ReserveI`, every other status is `None`, no status is `ReserveII`, and no later position is admitted. |

The authoritative validator derives ownership from `CampaignContentContext`; it does not accept a
client-provided count. For the current fixtures, `N=2`, so valid completion versions are 11–13.
This removes the one-event-per-position shortcut without opening an unbounded or generic advancement
path. Exact world preservation across completion is historical behavior proven by expected-event
recomputation and projection; the stateless snapshot validator proves only the resulting shape.

`CampaignWorldValidator.IsValidInitial` remains stricter than runtime validation: exact scenario
placements and every status `None`.

## Exercise Harness adoption

Add exact enum member `DesignateAllReservesThenFirstByActionId` with canonical manifest/configuration
string `designate-all-reserves-then-first-by-action-id`.

The runtime-only controller view is contract 1 and contains:

```text
ExerciseControllerCandidate:
  contractVersion, actionId, kind, elementId?

ExerciseControllerActionSet:
  audience, candidates[]
```

`elementId` is required exactly when `kind=designate-reserve` and absent for every current
payload-free kind. Candidate views are built in `ExerciseExecutor` from the current Core legal-action
set; the controller receives no snapshot, content context, or privileged mutation seam.

The policy is stateless and deterministic:

1. outside Reserve, retain the existing deterministic legal-action behavior;
2. when Reserve candidates are present, require exactly one completion candidate and zero or more
   well-formed designation candidates;
3. while designation candidates remain, select the ordinally first by `elementId`, then `actionId`;
4. once none remain, select `complete-reserve-designation`; and
5. fail the policy for a malformed mixture rather than guessing; otherwise fall back to the existing
   first-by-action-ID behavior for non-Reserve positions; and
6. require terminal position first-side Movement.

Progress is derived solely from regenerated current legal actions: each accepted designation removes
its candidate. The controller retains no counter, prior selection, fixture element count, or hidden
authority. For both current setups it designates two elements, so the Movement-terminal run accepts
12 actions total: nine to Reserve, two designations, and one completion.

This controller is test instrumentation, not authority. It selects only advertised candidates and
uses the normal submit/receipt/event path. A baseline/exploratory Exercise twin and the two-setup
serial Maneuver prove deterministic repetition. Existing Organization and Reserve-boundary fixtures
remain regression checkpoints.

Because the controller policy is a closed persisted vocabulary, Exercise Manifest and Maneuver
Manifest clean-cut from v1 to v2. Controller configuration identity clean-cuts to contract/scheme v2.
Existing evidence/report envelope shapes remain v1, but their embedded manifest versions,
configuration hashes, ruleset hashes, events, and snapshot checkpoints migrate. Seed derivation is
unchanged because the policy uses no randomness.

Trusted evidence recognition must add the two Reserve event shapes. Step-evidence decoding must
accept snapshot v8/world v3 and still reject prior or malformed variants. Campaign Observation v4
is not persisted in Exercise evidence and remains a Core observation-contract/test concern. Exact
fixture hashes and report fingerprints are recorded only after implementation.

## Implementation evidence

The completed pre-review implementation reaches the exact first-side Movement boundary after 12
accepted actions: nine retained preamble actions, two element designations, and explicit completion.
The checked standalone baseline/exploratory twins and the predetermined/contested serial Maneuver
verify event-history reconstruction and fresh-session re-adjudication. The Maneuver report
fingerprint is
`sha256:423db9b41bea444bffc918c2ec11717579257cf7f71c93bde2bd9546188763e1`.

The 2026-08-25 Task 016 pre-review gate produced this evidence:

- solution restore succeeded with all projects current;
- solution build succeeded with 0 warnings and 0 errors;
- `Cna.Core.Tests` passed 408/408;
- `Cna.ExerciseRunner.Tests` passed 268/268;
- the full solution passed 677/677 with no skipped tests;
- format verification and `git diff --check` passed; and
- the repository `just check` wrapper passed format, build, and all 677 tests.

The first final-review pass then sustained `RDV1-001` through `RDV1-003`; these pre-remediation counts
are retained as historical evidence rather than a readiness verdict. Task 016 now adds valid-history
opponent byte-invariance tests, strict nested-world rejection, current durable-plan state, and a
corrected-target review before closure.

The corrected Task 016A target has the following author-side evidence:

- focused valid-history opponent invariance passed 2/2 for both acting-side choices;
- the trusted-evidence semantic-validator class passed 45/45, including rehashed initial and final
  `world:{}` bundles;
- `Cna.Core.Tests` passed 410/410;
- `Cna.ExerciseRunner.Tests` passed 270/270;
- the full solution passed 681/681 with no skipped tests;
- solution build remained at 0 warnings and 0 errors;
- format verification, `git diff --check`, and the targeted stale-version/status scan passed; and
- `just check` passed its integrated restore, format, build, and 681-test gate.

That corrected review closed `RDV1-001` through `RDV1-003` and then reproduced `RDV1-004` P1: a
failed-reconstruction bundle could carry a structurally incomplete World v2 object when every
dependent hash and proof field was internally consistent. Task 016B now routes snapshot fact
extraction through Core's complete strict snapshot decoder before the runner classifies any
executed-or-later failure profile. The exact end-to-end regression is green; Core passes 411/411,
ExerciseRunner passes 271/271, the full solution passes 683/683 with no skips, build remains at zero
warnings/errors, and format, `git diff --check`, plus integrated `just check` pass. The same
reviewer's closure was the final implementation gate.

The same reviewer returned final verdict **Ready**: `RDV1-004` is closed, the original three
findings remain closed, no new finding was identified, and the complete 46-item security pass found
no candidates or coverage gaps. The durable result is recorded in
[the final implementation review](../reviews/reserve-designation-v1-implementation-final.md).

The separate blind-first independent implementation-review result is recorded by Task 016 after the
reviewer returns a frozen ledger and verdict.

## Implementation task graph

Tasks are deliberately vertical and TDD-first. `RES-TASK-002` through `RES-TASK-005` form the Core
clean-cut identity lane; Tasks 010-013 form the Exercise controller/identity/evidence lane. Partial
identity-lane commits may be locally red and must not merge alone.

| Task | Outcome and owned scope | Depends on |
| --- | --- | --- |
| `RES-TASK-001` | Freeze owner decisions, versions, enum/string/property order, source identities, layered validation boundary, semantic controller contract, complete consumer inventory, and traceability; close planning review with no unresolved P0/P1. Owns the three Reserve docs. | owner approval |
| `RES-TASK-002` | RED/GREEN normalized Reserve rules artifact and empty-selection ruling; use exact Capability Point terminology, bump ruleset version/hash, and update focused manifest goldens. Owns `Cna1979Ruleset.cs`, new Reserve artifact/codec files, and two focused Rules test files. | 001 |
| `RES-TASK-002A` | Reconcile retained Rules-layer identity consumers discovered by the Task 002 namespace regression: update `ContentVocabularyTests.cs` for the sixth manifest artifact and `StageEntryRulingTests.cs` for ruleset contract v5, then require the complete `Cna.Core.Tests.Rules` namespace green. | 002 |
| `RES-TASK-003` | RED/GREEN world v2 status state, strict world codec, structural world validation, exact initial `None`, and malformed/unknown/ReserveII tests. Owns `CampaignElementState.cs`, `CampaignWorldSnapshot.cs`, `CampaignWorldFactory.cs`, `CampaignSnapshotSerializer.cs`, and `CampaignWorldTests.cs`. | 002A |
| `RES-TASK-004` | Clean-cut create v6/snapshot v7 and split local structural from context-authoritative checkpoint validation. Owns `CampaignSnapshot.cs`, `CampaignCommand.cs`, `CampaignEvent.cs`, `CampaignEngine.cs`, and `CampaignSnapshotValidator.cs`; all create-command checks are current before the lane advances. | 003 |
| `RES-TASK-004A` | Close the Core identity lane before candidate work: migrate `CampaignEventSerializer.cs` and `CampaignProjector.cs`, then prove creation/event round-trip, snapshot parse, replay, context-free malformed-state, and context-authoritative forged-ownership/ReserveII rejection in at most three focused Campaign test files. | 004 |
| `RES-TASK-004B` | Reconcile the retained replay-preparation snapshot-v6 consumer discovered by the complete Campaign namespace gate. Owns only `CampaignReplayPreparationTests.cs`, updates its projected snapshot/version-rejection expectations to v7/v6, and requires the entire Campaign namespace green. | 004A |
| `RES-TASK-005` | RED/GREEN observation v3 with exact public `CampaignObservationReserveStatus`, owner status projection, canonical strings, unknown-value rejection, equality, goldens, and opponent byte invariance. Owns `ObservedOwnElement.cs`, `CampaignObservation.cs`, `CampaignObservationSerializer.cs`, `CampaignObservationProjector.cs`, and focused observation tests. | 004B |
| `RES-TASK-006` | RED/GREEN subject-bearing candidate semantics/serialization while proving every existing candidate ID unchanged. Owns `CampaignActionCandidate.cs`, `CampaignLegalActionSerializer.cs`, and focused action-contract tests. | 005 |
| `RES-TASK-007` | RED/GREEN current Reserve membership and candidate-to-command mapping, including audience/subject/stale negatives but no accepted state mutation. Owns `CampaignLegalActions.cs`, `CampaignActionExecution.cs`, new Reserve commands, and two focused action test files. | 006 |
| `RES-TASK-007A` | Reconcile the four retained pre-Reserve empty-side-set assertions discovered by the Actions/Core regression gates. Owns only `CampaignLegalActionsTests.cs`, `StageEntryCampaignTests.cs`, `StageEntryFleetRepairTests.cs`, and `CampaignReserveAudienceTests.cs`; preserves their Stage Entry, audience, opponent-invariance, and authority-immutability evidence while updating the terminal Reserve expectation to three candidates only for the resolved first side. | 007 |
| `RES-TASK-007B` | Correct implementation-checkpoint findings `RR1-001`/`RR1-002`: enforce strict `None` status at states 1–10, admit only finite same-position Reserve designation checkpoints using `stateVersion = 10 + ReserveI count`, and require context-authoritative exact membership/location plus resolved-first-side ownership. Owns `CampaignSnapshotValidator.cs`, `CampaignWorldFactory.cs`, and one focused `CampaignReserveCheckpointValidationTests.cs` file. Movement admission remains Task 009. | 007A |
| `RES-TASK-008` | Vertical designation transition: both frozen Reserve event contracts/codecs/factories, one-element projection, same-position replay through Task 007B's bounded validation, and forged-history negatives. Owns one new `CampaignReserveEvents.cs` file containing both Reserve event records and their factory, `CampaignEventSerializer.cs`, `CampaignEngine.cs`, `CampaignProjector.cs`, and one focused `CampaignReserveDesignationTests.cs` file. The serializer's closed write/read switches must support both designation and completion events here, before either event can enter canonical history. | 007B |
| `RES-TASK-009` | Vertical completion transition: zero/one/all selected paths, exact Movement successor, preserved world via replay, Movement status constraints, and no Movement candidates. Owns engine/projector/validator plus focused completion and end-to-end Campaign tests. | 008 |
| `RES-TASK-010` | RED/GREEN runtime semantic controller view and stateless designate-all policy. Owns `ExerciseController.cs`, `ExerciseExecutionRuntime.cs`, `ExerciseExecutor.cs`, `ExerciseControllerTests.cs`, and `ExerciseExecutorTests.cs`. | 009 |
| `RES-TASK-011` | Clean-cut Exercise Manifest/controller configuration identity to v2 with exact policy string and unchanged first-by-ID behavior. Owns `ExerciseContracts.cs`, `ExerciseManifestCodec.cs`, `ExerciseConfigurationIdentity.cs`, `ExerciseManifestCodecTests.cs`, and a focused configuration-identity test. | 010 |
| `RES-TASK-011B` | Before any checked command-profile GREEN gate, clean-cut trusted evidence structural admission to the current ruleset v5, snapshot v7, world v2, and admitted event family. Owns `ExerciseEvidenceCodec.cs` and the focused `ExerciseBundleSemanticValidatorTests.cs` identity/legacy-rejection cases. This makes the subsequent fixture migrations independently executable; remaining semantic/configuration reconciliation stays in Task 013. | 011 |
| `RES-TASK-011A` | Rename and migrate all checked Exercise fixtures from `.v1.json` to `.v2.json`: Organization baseline/exploratory and Reserve-boundary baseline/exploratory. Update every `ExerciseRunCommandTests.cs` filename/reference and exact configuration golden, including the current ruleset v5 identity, and require its command tests GREEN. Owns those four manifests plus that command test. | 011B |
| `RES-TASK-012` | Clean-cut Maneuver Manifest to v2 and migrate materialization/configuration consumers. Owns `ManeuverManifestContracts.cs`, `ManeuverManifestCodec.cs`, `ManeuverExecutor.cs`, `ManeuverManifestCodecTests.cs`, and `ManeuverExecutorTests.cs`. | 011A |
| `RES-TASK-012A` | Rename and migrate both checked serial Maneuvers from `.v1.json` to `.v2.json` (`rules-lab.serial` and `rules-lab.stage-entry.serial`), update all `ManeuverRunCommandTests.cs` references/goldens, and remove stale v1 filenames. Owns the two manifests and that command test. | 012 |
| `RES-TASK-013` | Reconcile the remaining trusted evidence semantic readers/writers with final Exercise/Maneuver configuration identities while keeping envelope shapes v1, and refresh remaining configuration/hash goldens. Owns `ExerciseBundleSemanticValidator.cs`, `ExerciseEvidenceWriter.cs`, and their focused semantic/writer tests; it does not defer the structural snapshot/world admission required by 011A/012A. | 012A |
| `RES-TASK-014` | Add new checked standalone baseline/exploratory Movement-terminal v2 profiles, 12-step command evidence, and synchronized Harness spec/design. Owns two new Exercise manifests, `ExerciseRunCommandTests.cs`, and the two Harness docs. | 013 |
| `RES-TASK-015` | Add the new two-setup Movement-terminal serial Maneuver v2, command/report fingerprint evidence, and public project-map synchronization. Owns the new Maneuver manifest, `ManeuverRunCommandTests.cs`, `README.md`, roadmap, and `tech-design.md`. | 014 |
| `RES-TASK-016` | Reconcile Reserve research/spec/design, naming, traceability, review records, all focused/full/`just check` evidence, and the final independent implementation review. Owns the three Reserve docs, `naming-overview.md`, and the final review artifact. | 015 |
| `RES-TASK-016A` | Correct sustained final-review findings `RDV1-001`–`003`: clean-cut observation/action-set audience revision contracts without changing authority/replay versions, normalize missing nested-world trusted evidence, reconcile the durable plan, migrate exact consumers/docs, rerun full gates, and return the corrected target to the same reviewer. Owns the affected observation/action-set/runner/evidence files, focused regressions, exact goldens/current docs, and durable Task 016 plan. | 016 preliminary ledger and Author Explanation |

### Parallelization

After Task 001, source/ruleset work is the root. Tasks 003–005 are the serial Core identity lane.
Once Task 005 is green, candidate/action work (Tasks 006–007) and RED-only semantic controller test
design for Task 010 may proceed in parallel; controller production waits for candidate semantics.
Tasks 007B, 008, and 009 remain serial because designation events depend on finite same-position
validation and completion validation depends on designation-state semantics. Tasks 011–013,
including 011B/011A/012A, are a serial Exercise identity/fixture/evidence lane after the controller and final
Core identities are green. Documentation reconciliation may begin alongside Task 015 once final
digests and report fingerprints exist.

## Requirement-to-task traceability

| Requirements | Implementation tasks | Acceptance evidence |
| --- | --- | --- |
| `RES-REQ-001`–`004` | 006–007 | `RES-AC-001`, `003`, `005`, `008` |
| `RES-REQ-005`, `007`–`009` | 007B–008 | `RES-AC-003`–`006` |
| `RES-REQ-006`, `015` | 009 | `RES-AC-002`, `004`, `006`, `011` |
| `RES-REQ-010`–`012` | 003–005, 007B–009 | `RES-AC-006`–`008` |
| `RES-REQ-013` | 002 | ruleset manifest goldens, `RES-AC-008` |
| `RES-REQ-014` | 010–015 | `RES-AC-009`, `010` |
| all | 016 | `RES-AC-012`, final review |

## Review challenge points

Independent planning review should specifically challenge:

1. whether allowing zero designations is adequately source-backed and explicitly recorded;
2. whether keeping candidate/submission/receipt v1 is compatible when one new kind gains a subject;
3. whether world/create/snapshot/observation version changes are complete and no strict decoder is
   omitted;
4. whether the validator formula admits only reachable Reserve histories and rejects forged counts;
5. whether owner-only status disclosure is sufficient for interaction without leaking opponent
   state;
6. whether the completion event is necessary replay evidence rather than a generic advance; and
7. whether the harness controller proves a positive designation through ordinary legal actions.

Review instance 1 sustained four findings. The corrected freeze must additionally prove exact
Capability Point terminology, the stateless semantic controller and v2 persisted identities,
structural versus context-authoritative validation consumers, and the public observation-owned enum.

## Approval and implementation gate

The project owner accepted `RES-DEC-001` through `RES-DEC-012` on 2026-08-24. Review instances 1–3
resolved every P0/P1/P2 finding, including explicit ownership of both Reserve-event serializer
branches. Tasks 001–015 are complete. Task 016 owns the full repository gate and separate
blind-first implementation review; its first final pass sustained `RDV1-001`–`003`, so bounded Task
016A remediation and corrected-target review are required before closure. No additional planning
review instance may be created.
