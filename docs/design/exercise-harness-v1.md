# Exercise Harness v1 Technical Design and Delivery Plan

**Status:** In progress; Tasks 001-013 and single-Exercise observability hardening are implemented, Tasks 014-016 remain

**Date:** 2026-08-20

**Specification:** [Exercise Harness v1](../specs/exercise-harness-v1.md)

**Research:** [capability and replay](../research/exercise-capability-and-replay-spike.md),
[evidence artifacts](../research/exercise-evidence-artifact-spike.md), and
[reproducibility and pairing](../research/exercise-reproducibility-and-pairing-spike.md)

## Delivery shape

Deliver `EXERCISE-001` as six dependency-ordered checkpoints. Each checkpoint is independently
testable; no checkpoint claims the behavior of a later one.

```text
Core parity seam
  -> opaque trusted Exercise session
     -> transcript + two replay proofs (memory only)
        -> transactional single-run artifacts + CLI
           -> serial Maneuvers + deterministic pairing/reporting
```

Side-safe exports are not in this v1 chain. They are a separately authorized follow-up after
whole-bundle privacy tests exist.

## Component ownership

| Component | Owns | Must not own |
| --- | --- | --- |
| `Cna.Core.Campaigns` | Existing creation, authority, action execution, adjudication, projection | Filesystem, CLI, reports, controllers |
| `Cna.Core.Exercises` | Fresh-session capability, trusted step evidence, and reconstruction from Core-retained history | Arbitrary authority export, resume, filesystem, runner re-adjudication orchestration, reporting |
| `Cna.ExerciseRunner.Controllers` | Deterministic selection from current legal action sets | Commands, events, snapshots, rules, RNG adjudication |
| `Cna.ExerciseRunner.Execution` | Step loop, bounds, cancellation, terminal/failure classification, and second-session re-adjudication comparison | Authority mutation, history reconstruction, or adjudication semantics |
| `Cna.ExerciseRunner.Artifacts` | Versioned runner/proof envelopes, path confinement, staging/finalization, validation, summaries | Deciding simulation truth or replay semantics |
| `Cna.ExerciseRunner.Commands` | CLI grammar, manifest loading, process-exit mapping | Converting a failed Exercise to success |

No production assembly receives `InternalsVisibleTo`. `Cna.ExerciseRunner` references Core's narrow
public Exercise surface; it is not referenced by AppHost, OrleansHost, DecisionWorker, or
Intelligence projects.

## Authoritative execution flow

```text
creation inputs
  -> shared internal Campaign creation primitive
     -> ordinary path: CampaignAuthorityHandle
     -> Exercise path: ExerciseSession + canonical creation evidence

ExerciseSession + audience + action ID
  -> existing current legal-action generation
  -> shared internal exact-membership/action execution primitive
     -> reject: same session, no event, no cursor/state change
     -> accept: exactly one validated v1 event + projected successor
        -> ordinary path: receipt + CampaignAuthorityHandle
        -> Exercise path: trusted step evidence + ExerciseSession
```

The shared primitives return internal domain values. The v1 primitive rejects any accepted engine
decision whose event count is not exactly one; it never indexes `Events[0]` before that check. Each
outward facade performs only its own projection. Tests compare both outward paths at every supported
checkpoint so future changes cannot silently fork Exercise behavior. A multi-event action requires
a separately versioned ordinary receipt/state-transition contract before either facade accepts it.

## Core Exercise contract

Names are illustrative, but these properties are mandatory:

```csharp
namespace Cna.Core.Exercises;

public static class CampaignExercises
{
    public static ExerciseStartResult Begin(CampaignCreationRequest request);
    public static ExerciseActionQueryResult Query(ExerciseSession session, CampaignActionAudience audience);
    public static ExerciseStepResult Submit(ExerciseSession session, ExerciseActionSubmission submission);
    public static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession);
}

public sealed class ExerciseSession
{
    internal ExerciseSession(/* exact Core state and history */);
    public override string ToString() => nameof(ExerciseSession);
}
```

`ExerciseSession` is a sealed non-record without public state properties, equality, deconstruction,
serialization constructor, conversion operators, or authority-handle conversion. Begin accepts the
same immutable creation request as ordinary creation and retains exact Core-owned history internally.
No method accepts caller-provided event/snapshot bytes for execution.

An accepted step returns:

- successor opaque session;
- accepted audience and action ID;
- prior/committed state version and resulting position;
- a defensive copy of every canonical emitted event record;
- a defensive copy of the canonical resulting snapshot checkpoint.

A rejection returns a closed reason and no successor/evidence. Tests prove the input session's
queryable action bytes remain identical and no random cursor/event changes.

History reconstruction consumes only the session's Core-retained creation/event history, uses the
existing projector/replay semantics, and returns canonical final snapshot bytes plus a closed proof
result. The runner cannot supply or substitute history.

Re-adjudication remains runner-orchestrated but Core-authoritative: begin a second session using the
same creation inputs, query the recorded audience, require recorded action-ID membership, submit it,
and compare every Core-emitted canonical byte record. This deliberately proves the public Exercise
path instead of exposing internal commands.

## Terminal and failure model

The runner uses disjoint exhaustive result types:

```text
ExerciseCompletion
  Succeeded
    BoundaryReached(positionId)
    VictoryReached(victor)             # reserved until Core implements victory
  Failed
    ManifestInvalid
    BuildIdentityUnavailable
    ControllerFailed
    NoUniqueLegalAction
    IllegalAction
    InvariantFailed
    ReconstructionMismatch
    ReadjudicationMismatch
    StepLimitExceeded
    Cancelled
    ArtifactFailed
    UnexpectedFailure
```

Exact names may be adjusted before golden tests, but categories cannot overlap with success.
Negative-test manifests can state `assertFailureCategory`; the assertion passes only when the exact
category occurs, while `completion` remains failed and process exit remains nonzero. Maneuver
aggregation counts failed assertions separately but never moves them into success counts.

## Canonical evidence contracts

Extend the existing explicit `Utf8JsonWriter` pattern. Each contract has a strict reader, explicit
contract or scheme version, fixed property order, ordinal ordering, no ignored properties, and
golden bytes. JSONL framing is exact payload plus LF.

Canonical files:

| File | Source and equality role |
| --- | --- |
| `exercise-manifest.json` | Normalized accepted input; hash anchors run identity |
| `build-identity.json` | Git/build/runtime/ruleset/configuration identity and baseline eligibility |
| `run-result.json` | Closed terminal or failure result; no duration/timestamp |
| `seed-ledger.json` | Every canonical seed preimage, digest, derived value, domain, and role |
| `accepted-actions.jsonl` | Step ordinal, audience, action ID, concurrency coordinates |
| `canonical-events.jsonl` | Existing raw canonical event JSON values, one per LF-framed record |
| `step-evidence.jsonl` | Checkpoint identities and hashes binding action/event/snapshot evidence |
| `initial-snapshot.json` | Existing canonical initial snapshot bytes |
| `final-snapshot.json` | Existing canonical terminal/failure checkpoint snapshot when available |
| `reconstruction-proof.json` | Input hashes, reconstructed hash, expected hash, exact checks |
| `readjudication-proof.json` | Transcript/event/final hashes and exact checks from second session |
| `check-results.json` | Ordered invariant/assertion results; failures cannot be waived |
| `summary.json` | Deterministic machine summary derived from validated canonical files |

`summary.md` is deterministic derived presentation and belongs in the artifact manifest, but is not
an input to replay equality. `diagnostics.jsonl` and console logs are explicitly diagnostic: they
may carry timestamp/duration/machine data and are excluded from canonical repeatability claims.
Changing detail level intentionally changes the normalized manifest, its build-identity reference,
diagnostic/summary content, and the artifact manifest. It must not change the simulation-evidence
subset: accepted actions, canonical events, step evidence, initial/final snapshots, seed ledger,
check results, reconstruction proof, or re-adjudication proof.

The table above is the successful-bundle profile. A failed-bundle profile always requires
`run-result.json`, `check-results.json`, and every artifact safely completed before failure.
`exercise-manifest.json` is required only after strict manifest admission; build, execution,
snapshot, seed, and replay files are required only after their stages begin or complete. The
artifact manifest names the exact profile and readers enforce its required/forbidden combinations.
Raw invalid manifest contents are not automatically retained.

All byte arrays returned by Core are defensively copied. Runner evidence writers do not parse and
rewrite raw canonical campaign events/snapshots; they retain exact bytes and hash them.

## Artifact transaction

Given explicit root `artifacts/exercises`, the runner:

1. resolves and validates the root once;
2. allocates a unique same-volume `<root>/.partial/<run-directory-id>` directory whose local ID is
   diagnostic placement metadata and is absent from canonical evidence;
3. opens the staging root without following caller-selected symlinks;
4. validates every fixed schema-relative path for confinement, normalized uniqueness, and type;
5. writes each payload to a temporary file, flushes it, and places it at its final staging path;
6. writes and flushes `run-result.json` with its closed `succeeded` or `failed` status after all
   available payloads; this is the sole bundle-state authority;
7. computes sizes and SHA-256 hashes from closed on-disk files;
8. writes and durably flushes `artifact-manifest.json` last;
9. moves the staging directory to `<root>/succeeded/<run-directory-id>` or
   `<root>/failed/<run-directory-id>` on the same volume;
10. reopens through the bundle reader and validates status, manifest, schemas, sizes, and hashes
    before reporting its final path.

The writer never overwrites an existing destination. File modes may be owner-only defense in depth
on Unix, but confidentiality depends on explicit trusted classification and user-chosen root, not
platform-specific permissions. `.partial` directories survive interruption for diagnosis and are
never enumerated as complete. Cleanup is a later explicit command, not automatic behavior.

Writer failpoints in tests cover before/after each flush, manifest creation, and move. The reader
rejects missing/extra manifest entries, unlisted files except fixed platform metadata explicitly
documented by contract, bad status/location combinations, symlinks, changed bytes, and unknown
versions. Directory move is placement, not the transaction proof.

If failure occurs before a staging directory exists, or an artifact fault makes status/manifest
finalization impossible, the CLI returns nonzero and emits an explicit stderr diagnostic but does
not claim a completed bundle. Once staging exists, the writer attempts a failed profile only while
it can still satisfy the same manifest-last/readback rules; best effort never means accepting an
invalid final directory.

## Build identity and baseline eligibility

Before a requested baseline simulation, capture and verify:

- raw `git status --porcelain=v1 -z --untracked-files=all` bytes are empty;
- HEAD commit and tree identity;
- SHA-256 for every executed Sandtable assembly and relevant deps manifest;
- .NET runtime/SDK and OS architecture identity;
- canonical ruleset, configuration, normalized manifest, and seed-scheme identities.

For the first runner, configuration identity is the versioned
`sandtable.exercise-controller-configuration.v1` canonical material for the three fixed audience
controller policies. It is distinct from the hash of the complete normalized Exercise manifest.

Missing Git, an unresolved HEAD commit or tree, a dirty status, unreadable assembly, or a hash
mismatch fails before Core creation. Detached HEAD is valid when `HEAD^{commit}` and `HEAD^{tree}`
resolve exactly; worktrees commonly use that state. The result may be finalized as a failed
diagnostic bundle, but never as baseline evidence.

An explicit exploratory-dirty mode records HEAD, `dirty: true`, SHA-256 of the raw NUL-delimited
porcelain bytes, and executed assembly hashes. It writes `baselineEligible: false` and
`reproducible: false`. It does not capture filenames into canonical summaries, patch text, source
files, or untracked contents. That protects secrets and avoids a false reconstruction claim.

## Seed derivation and paired Maneuvers

Seed scheme `sandtable.exercise-seeds.v1` serializes these fixed canonical properties:

```json
{"contractVersion":1,"schemeId":"sandtable.exercise-seeds.v1","rootSeed":0,"maneuverId":"maneuver-id","exerciseOrdinal":0,"pairKey":null,"domain":"umpire","role":null}
```

Hash the exact UTF-8 bytes with SHA-256 and interpret digest bytes 0-7 as one unsigned big-endian
64-bit value. `rootSeed` is a JSON number within the unsigned 64-bit range; `pairKey` and `role` are
JSON null when not applicable, never empty-string sentinels. Domains are closed and include
`umpire`, `controller`, `artifact-sampling`, and `diagnostic-sampling`; only controller material uses
roles `system`, `axis`, or `commonwealth`. The ledger retains the exact canonical material bytes,
full digest, and derived decimal value. Golden vectors cover zero, maximum root seed, null/non-null
pair keys, and every domain/role.

A standalone Exercise uses `maneuverId = "standalone." + exerciseId`, `exerciseOrdinal = 0`, and
`pairKey = null`; its Exercise manifest owns the root seed. A Maneuver owns the sole root seed and
its child entries cannot provide one. Unpaired entries use their zero-based manifest ordinal and a
null pair key. Both variants in pair repetition N use the same stable pair key and the same
pair-local `exerciseOrdinal = N`; execution order and variant identity are not seed inputs.

Campaign ID uses a separate `sandtable.exercise-campaign-id.v1` canonical preimage containing
contract version, scheme ID, Maneuver/standalone ID, exercise ordinal, and nullable pair key. It is
`exercise-` plus the lowercase SHA-256 digest. Variant/controller identity is excluded, so paired
sessions have byte-identical `CampaignCreationRequest` values, including campaign ID and the
Umpire-derived seed. The manifest cannot provide a free-form campaign ID.

Paired variants therefore have the same pair key, exact creation-input hash, pair-local ordinal,
and initial role/domain seeds. Controller implementation/configuration identity is recorded
elsewhere but excluded from role seed material. This permits the exact claim:

> Paired variants began with identical declared initial conditions and identical initial
> role-specific random streams; their trajectories and random consumption may diverge after their
> behavior diverges.

V1 reports counts and descriptive deltas only. It makes no causal, significance, confidence,
balance, or post-divergence synchronization claim.

## Manifest shape

The strict standalone Exercise manifest contains contract version, stable Exercise ID,
setup/content/scenario/ruleset identities, controller policy per audience, terminal boundary,
maximum steps, root seed, build mode, artifact confidentiality/detail, and optional exact
negative-test assertion. Campaign ID is derived, not supplied. A Maneuver manifest contains its own
version/ID, ordered unpaired entries or paired variants, its sole root seed, pairing keys, and report
settings; nested Exercise entries omit root seed and campaign ID.

Paths in manifests are repository-relative inputs resolved before artifact staging. Manifests may
not select arbitrary Core types, commands, events, snapshot bytes, reflection targets, controller
assemblies, or output filenames. The checked-in controller policies are closed deterministic
values, such as exact action kind or an explicitly specified stable first-by-action-ID policy.

## Acting audience and invariant catalog

At each step the runner first compares the current position with the exact terminal boundary. If it
is not terminal, it queries `system`, `axis`, and `commonwealth` in that fixed order. Every query
must succeed. Exactly one audience must have a nonempty candidate set; zero produces
`NoUniqueLegalAction`, while more than one violates `active-audience-cardinality` and produces
`InvariantFailed`. Only the active audience's closed controller policy runs, and it must select one
current candidate. Simultaneous-action scheduling is outside v1 rather than resolved by priority.

Check scheme `sandtable.exercise-checks.v1` writes results in this fixed catalog order:

1. `authority-query-valid` for each queried audience in fixed audience order;
2. `active-audience-cardinality` for the nonterminal checkpoint;
3. `selected-action-membership` before submission;
4. `accepted-event-cardinality` requiring exactly one v1 event;
5. `checkpoint-continuity` for campaign/ruleset identity and exact one-version advancement;
6. `terminal-boundary` for exact declared position;
7. `history-reconstruction` for final canonical snapshot equality;
8. `readjudication` for transcript, event, and final snapshot equality.

Each result contains contract version, scheme ID, check ID, optional step/audience scope, and a
closed pass/fail value plus stable failure code. Required failures always fail the Exercise.
Canonical record order is exact:

1. step-level checks 1-5 sort by ascending `stepOrdinal`, then catalog ordinal;
2. repeated audience records within one check sort `system`, `axis`, `commonwealth`; a non-audience
   record follows those values, while checks 3-5 carry only the active audience;
3. run-level checks 6-8 follow every step record in catalog order with null step/audience.

Only attempted checks are serialized. A failing record is present; later checks in that step and
all later steps are absent. `terminal-boundary` is attempted once whenever execution began and is
appended pass or fail. Reconstruction and re-adjudication records exist only after successful
domain-terminal and terminal-boundary checks; otherwise they are absent, never synthesized as
skipped/pass. A pre-execution failed profile may therefore contain an empty versioned check result.
New/reordered checks, different ordering, or different absent/skipped semantics require a new scheme
version and updated goldens.

## Diagnostics and reporting

Each diagnostic record has a stable event name and available correlation fields: Maneuver ID,
Exercise ID, variant, step, campaign/state/position, audience, action ID, check, and failure category.
It may add wall-clock timestamps, elapsed monotonic duration, process/machine facts, and stack traces
only at authorized detail levels. These fields never feed controller selection or canonical output.

The implemented single-Exercise tiers are monotonic:

- `compact` records accepted steps and terminal completion;
- `forensic` adds fixed-order audience-query candidate counts, active-audience/controller selection,
  every attempted invariant check, progressively assembled context for failed query/controller/
  submission/cardinality/continuity decisions, reconstruction/re-adjudication results and hashes,
  and prepared payload counts/bytes;
- `debug` adds monotonic Core begin/query/controller/submit/reconstruction timings plus outer
  manifest-admission, build-identity, execution, re-adjudication, and artifact-preparation timings.
  Failed executions retain every timing measured before the failure.

The trusted diagnostics file is assembled before manifest-last finalization. To avoid a circular
rewrite, debug mode emits the actual artifact write/readback elapsed time and validated payload
totals as a structured console `trace=` record only after the final bundle reader succeeds. This
trace is noncanonical, contains no authority payload values, and cannot affect bundle status.

Aggregate reports open and validate finalized per-Exercise bundles rather than trusting in-memory
counters. Deterministic sections contain ordered terminal/failure counts and scenario/controller
identities. Nondeterministic duration/throughput sections are visibly separate and excluded from
stable report fingerprints. A report lists missing/corrupt bundles as failures, never omissions.

## Implementation checkpoints and tasks

Each task begins with failing tests and normally changes no more than about five primary files.
Generated/solution metadata and golden fixtures are listed separately and do not authorize unrelated
refactors. File names are expected targets; minor splits require the same ownership and acceptance.

### Checkpoint A — shared Core parity seam

#### `EXR-TASK-001` — Extract shared creation primitive

- **Depends on:** none.
- **Primary files:** `src/Cna.Core/Campaigns/CampaignAuthority.cs`, new
  `src/Cna.Core/Campaigns/CampaignCreationExecution.cs`, and focused existing creation test file.
- **Work:** write parity/nonmutation tests; extract the internal creation result used by the existing
  facade without changing public behavior or canonical bytes.
- **Accept:** all existing creation goldens remain exact; ordinary facade output is unchanged; the
  primitive returns enough internal creation event/projection evidence for the later Exercise facade.
- **Verify:** `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build`.

#### `EXR-TASK-002` — Extract shared accepted-action primitive

- **Depends on:** `EXR-TASK-001`.
- **Primary files:** `src/Cna.Core/Actions/CampaignLegalActions.cs`, new
  `src/Cna.Core/Actions/CampaignActionExecution.cs`, and focused submission test file.
- **Work:** move membership rebinding, closed command mapping, adjudication, event-count validation,
  projection, and successor construction behind one internal result.
- **Accept:** all existing rejection precedence/receipts/goldens remain exact; rejections return no
  successor/event; accepted v1 decisions require exactly one event before indexing/projection and
  never truncate a multi-event collection.
- **Verify:** Core test project plus targeted canonical action tests.

### Checkpoint B — opaque Exercise capability and memory proofs

#### `EXR-TASK-003` — Add Exercise-only session and Begin/Query

- **Depends on:** `EXR-TASK-001`.
- **Primary files:** new `src/Cna.Core/Exercises/ExerciseSession.cs`,
  `CampaignExercises.cs`, `ExerciseStartResult.cs`, one Core Exercise test file, and
  `tests/Cna.Core.Tests/Campaigns/AuthorityBoundaryTests.cs`.
- **Work:** mint fresh opaque sessions from creation inputs and expose existing legal-action query
  results without accepting authority handles.
- **Accept:** capability reflection/type-graph/compile tests prove nonconversion, nonserialization,
  no resume/import, no authority getters, and defensive ownership.
- **Verify:** `EXR-AC-003` focused tests.

#### `EXR-TASK-004` — Add Exercise Submit and trusted step evidence

- **Depends on:** `EXR-TASK-002`, `EXR-TASK-003`.
- **Primary files:** `src/Cna.Core/Exercises/CampaignExercises.cs`, new
  `ExerciseStepResult.cs`, `ExerciseStepEvidence.cs`, and one parity test file.
- **Work:** route submissions through the shared action primitive and expose defensive canonical
  event/checkpoint evidence only on acceptance.
- **Accept:** ordinary and Exercise paths match event/successor bytes at all current checkpoints;
  rejection leaves query/evidence unchanged; no mutable internal type crosses the surface.
- **Verify:** `EXR-AC-002` and Core authority-boundary tests.

#### `EXR-TASK-005` — Add history reconstruction proof

- **Depends on:** `EXR-TASK-004`.
- **Primary files:** new `src/Cna.Core/Exercises/ExerciseReconstructionResult.cs`, update
  `CampaignExercises.cs`, and one reconstruction test file.
- **Work:** reconstruct from Core-retained session history using the existing projector/replay path.
- **Accept:** exact final snapshot match passes; removed/reordered/changed history in test-only
  internal fixtures fails; no caller-supplied history is accepted publicly.
- **Verify:** reconstruction-focused Core tests and existing replay tests.

### Checkpoint C — runner contracts, deterministic identities, and in-memory execution

#### `EXR-TASK-006` — Scaffold runner and strict manifest/result contracts

- **Depends on:** `EXR-TASK-005`.
- **Primary files:** new runner/test `.csproj` files, `Program.cs`,
  `Artifacts/ExerciseContracts.cs`; metadata: `Sandtable.slnx` and central package props only if
  required.
- **Work:** add the non-service executable and tests; implement closed result/failure types plus
  strict version-1 manifest/result codecs; update the real-project boundary allowlist so the runner
  is the only new Core consumer and receives no friend access.
- **Accept:** unknown/extra/missing/duplicate/contradictory values reject; no failure can construct a
  success; CLI has stable exit mapping and no AppHost registration.
- **Verify:** both new project build and contract golden/negative tests.

#### `EXR-TASK-013` — Implement seed derivation, campaign identity, and ledger

- **Depends on:** `EXR-TASK-006`.
- **Primary files:** new `Execution/ExerciseSeedDeriver.cs`, `Execution/ExerciseCampaignId.cs`,
  `Artifacts/SeedLedgerCodec.cs`, one golden-vector test file, and a golden fixture.
- **Work:** exact canonical SHA-256/big-endian seed scheme with closed domains/roles plus the
  versioned deterministic campaign-ID scheme and standalone/Maneuver/pair ordinal rules.
- **Accept:** all golden/edge vectors match; standalone sentinel and nulls are exact; paired variants
  get identical campaign IDs/creation inputs/role seeds; domain/role separation holds; no runtime
  hash/random API participates; ledger round-trips strictly.
- **Verify:** seed-focused unit tests and culture/order variants.

#### `EXR-TASK-007` — Implement deterministic in-memory Exercise loop

- **Depends on:** `EXR-TASK-006`, `EXR-TASK-013`.
- **Primary files:** new `Execution/ExerciseExecutor.cs`, `Controllers/ExerciseController.cs`,
  `Execution/ExerciseCompletion.cs`, `Execution/ExerciseCheckCatalog.cs`, and one executor test file.
- **Work:** terminal-check, fixed-order all-audience query, require one active audience, select,
  submit, and check until exact boundary, failure, cancellation, or maximum step; collect canonical
  transcript and versioned ordered check results in memory.
- **Accept:** checked-in synthetic inputs reach only the declared Organization boundary; zero/multiple
  selections and step limit fail closed; diagnostics do not influence selection.
- **Verify:** executor tests for every terminal/failure branch.

#### `EXR-TASK-008` — Implement in-memory re-adjudication proof

- **Depends on:** `EXR-TASK-007`.
- **Primary files:** new `Execution/ReadjudicationVerifier.cs`, `Artifacts/ReplayProofContracts.cs`,
  and one verifier test file.
- **Work:** run a second fresh session from the same input/seed and submit recorded audience/action
  identities; compare transcript, events, and final snapshot independently of reconstruction.
- **Accept:** exact proof passes; changed action/event/final bytes each fail the correct check; neither
  proof status can mask the other.
- **Verify:** `EXR-AC-004` focused tests.

### Checkpoint D — first runnable, crash-safe single Exercise

#### `EXR-TASK-009` — Implement path-confined artifact schema and reader

- **Depends on:** `EXR-TASK-006`, `EXR-TASK-013`.
- **Primary files:** new `Artifacts/ArtifactSchema.cs`, `ArtifactManifestCodec.cs`,
  `ExerciseBundleReader.cs`, and one reader/path test file.
- **Work:** fixed relative paths, strict manifest/status, confinement/symlink/normalization checks,
  hashes/sizes/schema validation.
- **Accept:** traversal, absolute, duplicate, symlink, corruption, missing/extra, status/location, and
  version cases all reject without trusting partial data.
- **Verify:** `EXR-AC-007`, `EXR-AC-012` artifact reader tests.

#### `EXR-TASK-010` — Implement manifest-last transactional writer

- **Depends on:** `EXR-TASK-009`.
- **Primary files:** new `Artifacts/ExerciseBundleWriter.cs`, `ArtifactWriteFailure.cs`,
  `ArtifactWriterFailpoint.cs` (internal/test), and one lifecycle test file.
- **Work:** unique same-volume staging, per-file flush/place, status then manifest-last flush, final
  move, reopen validation, safe failure finalization.
- **Accept:** every failpoint leaves only rejected partial data or a valid failed bundle; writer never
  overwrites; valid success/failed bundles reopen; no automatic deletion.
- **Verify:** `EXR-AC-005`, `EXR-AC-006`, `EXR-AC-007`.

#### `EXR-TASK-011` — Implement build identity and baseline gate

- **Depends on:** `EXR-TASK-009`.
- **Primary files:** new `Execution/BuildIdentityCapture.cs`,
  `Artifacts/BuildIdentityCodec.cs`, and one identity test file.
- **Work:** exact `--porcelain=v1 -z --untracked-files=all` capture, resolvable HEAD commit/tree
  (including detached HEAD), assembly/runtime/ruleset/config/manifest hashes, clean fail-closed
  baseline, and explicit dirty exploration.
- **Accept:** clean verified begins; dirty baseline/unavailable/mismatch fail before Core; explicit
  dirty mode records hashes and false eligibility/reproducibility without patch/source capture.
- **Verify:** `EXR-AC-008` with fake process/filesystem seams and one repository integration test.

#### `EXR-TASK-012` — Wire single-run CLI, fixture, and summaries

- **Depends on:** `EXR-TASK-007`, `EXR-TASK-008`, `EXR-TASK-010`, `EXR-TASK-011`, and
  `EXR-TASK-013`.
- **Primary files:** `Program.cs`, new `Commands/ExerciseRunCommand.cs`,
  `Artifacts/ExerciseSummaryWriter.cs`, checked-in Exercise manifest, and one CLI integration test.
- **Work:** connect manifest validation, baseline gate, executor, both proofs, writer, diagnostics,
  summaries, and exact exit mapping.
- **Accept:** documented command creates a reader-validated success bundle; each failure returns
  nonzero and retains valid evidence when possible; inability to create/finalize staging makes no
  bundle claim; two clean runs into separate roots have byte-identical canonical files.
- **Verify:** `EXR-AC-001`, `EXR-AC-005`, `EXR-AC-011`, then both test projects.

#### Single-Exercise observability hardening — implemented

- **Depends on:** `EXR-TASK-012` and the post-merge single-run shakeout.
- **Primary files:** `ExerciseDiagnosticsWriter.cs`, `ExerciseExecutor.cs`,
  `ExerciseSummaryWriter.cs`, `ExerciseRunCommand.cs`, the checked baseline fixture, and focused
  evidence/CLI tests.
- **Work:** make the three accepted detail tiers operational, add deterministic correlation and
  noncanonical timings, surface build/seed/outcome/proof state in Markdown, and make a clean
  baseline run possible without first authoring an untracked manifest.
- **Accept:** forensic/debug are meaningful monotonic supersets on successful and failed executions;
  failed attempts retain available query/controller/action/submission context and timing; the nine-file simulation-evidence
  subset remains byte-identical across detail; forensic bytes remain repeatable; debug timings never
  feed adjudication; both checked fixtures admit under their declared build policy.
- **Verify:** focused cross-detail, summary, fixture, CLI, and complete runner-project tests.

### Checkpoint E — serial Maneuvers and honest pairing

#### `EXR-TASK-014` — Implement serial Maneuver and aggregate report

- **Depends on:** `EXR-TASK-012`, `EXR-TASK-013`.
- **Primary files:** new `Execution/ManeuverExecutor.cs`, `Commands/ManeuverRunCommand.cs`,
  `Artifacts/ManeuverReportWriter.cs`, checked-in Maneuver manifest, and one integration test.
- **Work:** execute ordered Exercises serially, derive seeds, reopen bundles, aggregate outcomes and
  separate timing diagnostics.
- **Accept:** stable order/counts; failures remain visible; corrupt/missing bundle fails aggregation;
  deterministic report fingerprint excludes durations.
- **Verify:** `EXR-AC-009` and new Maneuver tests.

#### `EXR-TASK-015` — Implement paired comparison contract/report

- **Depends on:** `EXR-TASK-014`.
- **Primary files:** update `ManeuverExecutor.cs`, new `Artifacts/PairedReportWriter.cs`, paired manifest,
  and one paired integration test.
- **Work:** validate pair creation-input equality, assign matching initial role/domain seeds, record
  controller identities separately, and emit constrained descriptive comparisons.
- **Accept:** initial ledgers match by role/domain; implementation IDs do not perturb seeds; report
  includes exact divergence limitation and contains no prohibited causal/significance/balance claim.
- **Verify:** `EXR-AC-010` plus report golden tests.

### Checkpoint F — repository reconciliation and close

#### `EXR-TASK-016` — Documentation and repository gates

- **Depends on:** `EXR-TASK-015`.
- **Primary files:** `README.md`, `docs/roadmap/pre-alpha-roadmap.md`, `tech-design.md`,
  `naming-overview.md`, this design, and the governing specification. This six-document closeout is
  the explicit small-file exception to the normal five-primary-file task target.
- **Work:** change statuses from proposed only for delivered checkpoints; add project/command map,
  exact architecture boundaries, contract versions, artifact classification, and honest deferrals.
- **Accept:** no document calls Exercise a batch; no planned feature is described as implemented;
  examples run; requirement/task/evidence traceability is current.
- **Verify:** `git diff --check`, link/ID trace checks, `just check`, and one independent review.

## Dependency graph

```text
001 -> 002 -> 004
001 -> 003 -> 004 -> 005 -> 006 -> 013 -> 007 -> 008
                                013 -> 009 -> 010
                                       009 -> 011
                    007 + 008 + 010 + 011 + 013 -> 012
                                                   012 -> 014 -> 015 -> 016
```

No runner CLI is declared runnable before `EXR-TASK-010` or without `EXR-TASK-013` deterministic
seed/campaign identities; transactional artifacts and exact run identity are part of the first
runnable increment, not later polish.

## Prior-review finding reconciliation

| Prior finding | Resolution in this plan | Closure evidence |
| --- | --- | --- |
| Domain terminal success was mixed with expected failure states. | `EXR-007` and `EXR-008` use disjoint closed success/failure types; a negative-test assertion never changes completion or process exit. | `EXR-TASK-006`, 007, 012; `EXR-AC-005`. |
| The proposed facade could become a second authority or leak an ordinary campaign handle. | `EXR-002`-`EXR-005` require a fresh, opaque, non-convertible Exercise session and one shared internal creation/action execution path. Runner contracts remain outside Core domain ownership. | `EXR-TASK-001`-005; `EXR-AC-002`, 003. |
| Transactional artifacts were deferred until after runnable behavior. | Checkpoint D makes reader/confinement and manifest-last writer prerequisites of the first single-run CLI. Rename is only placement; final readback validates status, schemas, sizes, and hashes. | `EXR-TASK-009`, 010 before 012; `EXR-AC-006`, 007. |
| Dirty builds could be presented as reproducible baselines. | `EXR-019` fails baseline requests closed; `EXR-020` permits only explicitly nonbaseline/nonreproducible dirty exploration without source capture. | `EXR-TASK-011`, 012; `EXR-AC-008`. |
| The original first phase combined too much behavior to verify safely. | Delivery is divided into shared-Core parity, capability/proofs, runner contracts/in-memory execution, transactional first CLI, Maneuvers/pairing, and reconciliation checkpoints with 16 dependency-ordered tasks. | Task dependency graph and per-task focused verification. |
| Repository-map updates were vague and Exercise terminology contradicted itself. | The contradiction is corrected now; `EXR-TASK-016` owns synchronized implemented-state updates to `README.md`, `tech-design.md`, `naming-overview.md`, spec, and design. | Current link/terminology checks; `EXR-AC-013`; final `just check` and review. |

## Traceability matrix

Status distinguishes the implemented single-Exercise boundary from deferred Maneuver work.

| Requirements / decisions | Delivery tasks | Verification / retained evidence | Status |
| --- | --- | --- | --- |
| `EXR-001`, `EXR-006`, `EXR-007`, `EXR-008`; terminal/failure separation | 006, 007, 012 | Manifest/result goldens, CLI exits, success/cancellation/admission/artifact-failure bundles | implemented for one Exercise |
| `EXR-002`, `EXR-003`, `EXR-004`, `EXR-005`; isolated capability and shared primitives | 001-004 | Core parity, reflection/type-graph, rejection nonmutation tests | implemented |
| `EXR-009`; history reconstruction | 005, 008, 012 | Reconstruction proof file and corrupt-history tests | implemented |
| `EXR-010`; action-identity re-adjudication | 008, 012 | Re-adjudication proof and independent action/event/final mismatch tests | implemented |
| `EXR-011`, `EXR-012`, `EXR-023`; canonical/versioned evidence | 006, 008-010, 012-014 | Golden/strict-reader/order tests and reader-validated CLI bundles; Maneuver evidence pending | implemented for one Exercise |
| `EXR-013`, `EXR-014`; manifest-last transaction | 009, 010, 012 | Writer failpoint trees, rejected partials, reopened success/failure bundles | implemented |
| `EXR-015`; confidentiality/detail separation | 006, 009, 010, 012 + observability hardening | Cross-detail evidence-invariance and monotonic-tier tests; all files remain trusted-authority | implemented for one Exercise |
| `EXR-016`; side-safe export deferred | 016 and future separately authorized task | Spec non-goal; absence/public-API tests; future whole-tree noninterference evidence | deferred |
| `EXR-017`; seed domain separation | 013, 014 | Standalone seed goldens/ledger/culture/order implemented; Maneuver derivation pending | partially implemented |
| `EXR-018`; honest pairing | 013-015 | `EXR-AC-010`; paired campaign/seed goldens, ledgers, and report golden | planned |
| `EXR-019`, `EXR-020`; clean baseline and dirty exploration | 011, 012 + observability hardening | Fake/integration identity cases, emitted build identity, and separate checked baseline/exploratory fixtures | implemented |
| `EXR-021`; serial validated aggregation | 014, 015 | `EXR-AC-009`, 010; bundle-reader aggregation tests and reports | planned |
| `EXR-022`; separate correlated diagnostics | 007, 012, 014 + observability hardening | Successful and failed query/controller/submission/check/proof correlation, debug failure timings, artifact readback trace, and command-boundary cross-detail evidence test; Maneuver correlation pending | implemented for one Exercise |
| `EXR-024`, `EXR-025`; deterministic selection and single active audience | 006, 007, 012 | Executor controller/cardinality/step-bound tests and checked-in fixture | implemented |
| `EXR-026`; ordered invariant catalog | 006-008, 012 | Strict check codec, ordering, scope, failure, and emitted-bundle tests | implemented |
| `EXR-NFR-001`-`005`; reproducibility, reliability, boundaries, quality | 001-016 | All acceptance scenarios, two clean bundles, `just check`, final review | planned |
| `DEC-001`-`DEC-016` research decisions | 001-016 as mapped above | Three retained research spikes, governing spec, tests/bundles after implementation | accepted/planned |

## Final independent-review reconciliation

The single final review task was `01a01d01-06b0-7f02-ad8e-1681765c0a52`. Its preliminary ledger
was reconciled before the author-aware pass. The reviewer then identified the following additional
items; all are accepted and corrected without starting another review pass.

| Severity | Finding | Disposition and retained correction |
| --- | --- | --- |
| P1 | Cross-detail acceptance required all canonical files to match even though detail is an identity-bearing manifest input. | Accepted. `EXR-AC-011`, `EXR-022`, and this design now define the byte-stable simulation-evidence subset and permit manifest/build/diagnostic/summary/artifact-manifest differences. |
| P2 | Repeated `check-results` ordering and skipped behavior were ambiguous. | Accepted. The design now fixes step-major, catalog, and audience order; run-level placement; failure truncation; conditional replay checks; and empty pre-execution profiles. |
| P2 | Roadmap wording, Task 016 ownership, and three trace rows lagged delivery. | Accepted. The roadmap distinguishes first single-run CLI from later Maneuver; Task 016 owns the roadmap; `EXR-018`, `EXR-024`/025, and `EXR-026` rows name their missing task/acceptance evidence. |
| Delivery-order issue found during the pass | Seed/campaign identity task followed the first runnable CLI. | Accepted before verdict. Task 013 now precedes executor/artifact work, Tasks 007/009/012 depend on it, and the graph forbids a CLI without identity plus transactional artifacts. |

## Deferred work and entry gates

| Deferred capability | Entry gate |
| --- | --- |
| Axis/Commonwealth/public bundles | Independent physical bundle schemas plus whole-tree opponent-change noninterference tests and explicit authorization |
| Full victory outcome | Core victory adjudication/event/projection contract and scenario capable of reaching it |
| Model-backed controllers | Versioned proposal contract, timeout/deduplication/fallback path, provider-realistic tests, no I/O in authority turn |
| Parallel/distributed Maneuvers | Serial v1 correctness, deterministic scheduling/identity design, bounded resources, cancellation and partial-failure contract |
| ReplayDB/SQLite/indexes | Stable file contracts, migration/version policy, proof that files remain source of truth |
| Performance gates | Representative workload, stable measurement environment, separately reviewed thresholds; never canonical evidence |
| Production activation/resume | Authenticated Chronicle/Archives provenance and a dedicated activation-only capability that cannot become an outward mutation path |

## Implementation verification sequence

At every task, run its focused project tests. At each checkpoint, run:

```text
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
dotnet format Sandtable.slnx --verify-no-changes --no-restore
```

At `EXR-TASK-016`, run `just check`, execute both documented commands from a verified clean checkout,
validate their bundles through the reader, compare the two clean single-run canonical trees, and
retain exact command/output evidence in the PR. No remote provider or timing threshold participates
in the gate.
