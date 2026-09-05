# Exercise Harness v1 Technical Design and Delivery Plan

**Status:** Task 016 remains complete; merged `MOV-TASK-009` compatibly extends the checked Harness
through non-contact Movement to exact first-side Breakdown Determination. A post-adoption paired
lowest-cost controller instrument is implemented and verified locally. `ZOR-TASK-006C` activates
Reaction in Core; Runner Reaction selection remains `ZOR-TASK-007A`

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
    public static ExerciseCheckpoint ReadCheckpoint(ReadOnlyMemory<byte> canonicalSnapshot);
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
No method accepts caller-provided event/snapshot bytes for execution. `ReadCheckpoint` is a
validation-only decoder: it returns the existing scalar checkpoint view and cannot create, resume,
or mutate an Exercise session.

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
`sandtable.exercise-controller-configuration.v2` canonical material for the three fixed audience
controller policies. It is distinct from the hash of the complete normalized Exercise manifest.
Task 009 retains that identity and the existing Exercise and serial-unpaired Maneuver v2 contracts.
Its six Movement policies are additive closed values. Task 009 advanced the Runner-local controller
candidate to v2 for Movement semantic IDs. The post-adoption cost-sensitivity instrument advances
the current trusted candidate to v3 by requiring the exact positive public total cost on every move
and adds one lowest-cost policy token. Existing policy bytes and selection behavior do not change;
v1/v2 candidates reject at the current boundary.

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
negative-test assertion. Campaign ID is derived, not supplied.

The current clean-cut identity is `sandtable.maneuver-manifest.v2` with exact top-level order
`contractVersion`, `schemeId`, `maneuverId`, `mode`, `rootSeed`, `report`, `exercises`. The only v2
mode is `serial-unpaired` and the only report profile is `trusted-authority`. Each ordered child is
the standalone Exercise shape without `rootSeed`; it can never contain `campaignId`, `pairKey`, or
variant fields. Exercise IDs are unique within the Maneuver, the list is nonempty, and Maneuver IDs
cannot begin with the reserved synthetic standalone namespace `standalone.`. The governing
specification contains the exact JSON shape and rejection rules. Task 015 implements its separate
versioned paired extension without weakening this reader.

Paths in manifests are repository-relative inputs resolved before artifact staging. Manifests may
not select arbitrary Core types, commands, events, snapshot bytes, reflection targets, controller
assemblies, or output filenames. The checked-in controller policies are closed deterministic
values, such as exact action kind or an explicitly specified stable first-by-action-ID policy.

Maneuver admission is all-or-nothing before Core creation. It parses and normalizes every child,
then materializes entry N with the Maneuver root seed and explicit `ExerciseRunIdentity(rootSeed,
maneuverId, N, null)`. The normalized child Exercise manifest is the existing v1 byte contract, so
build identity, existing bundle schemas, and standalone tooling do not gain a second child-manifest
format. The `unpaired` diagnostic/report variant is derived from the admitted Maneuver mode; it is
not a seed input.

## Reusable Exercise run boundary

`ExerciseRunCommand` currently owns admission, build identity, execution, re-adjudication, payload
assembly, bundle finalization, console output, and exit mapping in one method. Task 014 extracts an
internal `ExerciseRunCoordinator` only for the post-admission portion. Its request carries the
admitted normalized Exercise manifest, explicit `ExerciseRunIdentity`, repository root, artifact
root, cancellation token, and telemetry boundary. It owns build capture, Core execution, both
proofs, summaries/diagnostics, fallback construction, and transactional bundle writing.

The coordinator performs no console I/O and returns the child process classification plus the
completed bundle path when one was safely finalized. It does not return trusted aggregate facts.
Standalone `exercise run` retains its existing pre-admission errors, derives the standalone
identity, invokes the coordinator, and preserves exact output/exit behavior. `maneuver run`
materializes an explicit Maneuver identity and invokes the same coordinator directly—never a
subprocess, temporary manifest, or duplicated execution pipeline.

Explicit identity must reach every consumer. `ExerciseExecutor` accepts it rather than recreating
standalone identity. Re-adjudication uses the original ledger identity. JSON/Markdown summaries and
all correlation records use the same identity even for zero-step failures. This closes the current
standalone fallbacks in executor, re-adjudication, summary, diagnostics, and command trace paths.

## Trusted child-bundle aggregation view

`ExerciseBundleReader` remains the only authority for finalized child artifacts. Its defensive
return value is extended to retain, when the bundle profile provides them, the already parsed
normalized Exercise manifest bytes/value, build identity, seed ledger, run result, check results,
strictly parsed accepted-action/step records, canonical-event records, and artifact-manifest
bytes/hash. Arrays and byte values are copied and returned read-only. Payload validation, record
counting, typed parsing, and cross-artifact semantic validation happen during one read; callers
cannot request a later file reread.

Before aggregation, the Maneuver requires the retained normalized manifest to equal the expected
materialized child bytes, an exact seed-ledger Maneuver ID/root seed/ordinal/null-pair match, and the
available build-identity build mode plus manifest/ruleset/controller-configuration hashes to match.
Profiles before `failed-executed` normally omit the ledger, so Task 014 cannot attribute them to a
child even if their manifest/build bytes match; they become `bundle-identity-mismatch`. The sole
non-attribution exception is cancellation observed after scheduling but before Core begin: a
strictly reopened `failed-identified` bundle with coordinator exit `cancelled`, exact admitted
manifest/build identity, cancelled run result, no seed ledger, and an empty execution footprint is
represented as `not-run/cancelled`, never as a child outcome. Early build-identity failure,
unexpected execution failure, artifact fallback, or any contradictory cancellation evidence stops
aggregation. A missing path becomes `completed-bundle-missing`; a reader rejection becomes
`bundle-invalid`. These categories are separate from the Exercise failure catalog.

Task 014 also closes the current gap between structurally valid and semantically trusted bundles.
New strict codecs parse accepted-action and step-evidence JSONL into defensive typed records. A
bundle semantic validator requires canonical encodings, contiguous ordinals, receipt/version
continuity, equal action/event/step counts, matching action/step coordinates, per-step event hashes,
and the final step snapshot hash. A succeeded result must be `BoundaryReached` at the admitted
`terminalBoundary`, and the decoded final snapshot plus final accepted step when present must anchor
that same position; a zero-step success requires identical initial/final snapshots already at that
boundary. `VictoryReached` is rejected until the separately reviewed Core victory contract exists.
The validator recomputes reconstruction/readjudication expected transcript, event, and final-snapshot
hashes from retained evidence and enforces the exact aggregate-eligible profile/result/check/proof
matrix in the specification. `succeeded` requires both proofs verified; the two replay-failure
profiles require their matching failed proof/check/category. Tests mutate each fact—including the
boundary outcome and a fabricated victory—and regenerate the artifact manifest so hash-valid but
contradictory bundles fail.

Before extracting campaign, state, ruleset, or position facts from either snapshot, the reader
retains exact byte-canonicality checks and calls the Core-owned `CampaignExercises.ReadCheckpoint`
capability. That capability runs the complete current snapshot/world strict decoder and returns only
opaque checkpoint coordinates. Consequently an expected reconstruction mismatch cannot convert a
structurally incomplete snapshot into trusted failed-reconstruction evidence.

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
counters. One `ManeuverExecutor` loops synchronously in manifest order. After each coordinator
return it immediately opens the completed path once through `ExerciseBundleReader`, validates the
retained identity, and derives the entry result. An ordinary identity-matched Exercise failure does
not stop the loop. Cancellation, a missing completed path, a reader rejection, or identity mismatch
does stop it; the executor deterministically appends a `not-run` record for every remaining ordinal.

Task 014 deliberately uses a single-file Maneuver transaction instead of inventing a second
multi-file artifact-manifest protocol. `maneuver-report.json` is written beneath
`maneuvers/.partial/<unique-id>`, flushed, moved to `maneuvers/succeeded|failed/<unique-id>`, and
strictly read back before being returned. Its canonical top-level order is `contractVersion`,
`schemeId`, `deterministic`, `reportFingerprint`, `diagnostics`.

The deterministic object's exact order is `manifest`, `status`, `counts`, `terminalCounts`,
`failureCounts`, `aggregationFailureCounts`, `entries`. It contains the complete normalized
Maneuver manifest; reconciled requested/attempted/validated/succeeded/failed/aggregation-failed/
not-run counts; terminal counts; the complete fixed-order Exercise failure catalog; the fixed-order
aggregation-failure catalog; and one outcome record per manifest entry. Because the normalized
manifest is embedded, the report retains scenario/setup/content/ruleset/controller identities
without duplicating potentially contradictory copies. Child records add only the ordered
outcome/evidence facts and hashes defined exactly in the spec.

`reportFingerprint` hashes the canonical serialization of the typed deterministic object alone.
The outer diagnostics object carries total/per-entry monotonic microseconds, observed child paths,
artifact-manifest hashes, and a rational validated-count/elapsed pair in the exact shape defined by
the spec. It contains no floating-point throughput. Timing/path/GUID/machine
variance can therefore change report bytes while leaving the deterministic fingerprint stable.
Build identity remains authoritative in each child bundle; machine-dependent build-identity bytes
never enter the report fingerprint.

The report reader verifies exact properties/order, contract/scheme/enums, canonical reserialization,
the fingerprint, all count/entry invariants, nullability rules, and final-directory status. The
writer accepts only the typed completed execution model, uses failpoints around create/write/flush/
move/readback, and never reports a path after failed readback. It leaves `.partial` or invalid final
evidence untouched for diagnosis. Child bundles are independent and are never rolled back because
report finalization failed.

Overall status precedence is `aggregation-failed`, `cancelled`, `exercise-failed`, `succeeded`.
Maneuver exits are separately closed: 0 success, 2 usage/admission, 13 ordinary child failure, 14
aggregation failure, 11 report artifact failure, 12 unexpected command failure, and 130
cancellation. The report retains exact child failure categories; the coarser Maneuver exit is not an
information substitute. CLI stdout lists identity-matched child bundles in ordinal order, followed
by the validated report path and fingerprint. Missing/corrupt/unverified bundles are failures,
never omissions or printed completed paths.

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
- **Accept:** the original checked synthetic input reaches its declared Organization boundary;
  later checked profiles may reach only an exact implemented downstream boundary. Zero/multiple
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

- **Status:** implementation and reconciliation complete through slices 014A-014I. Focused,
  project, and solution evidence is green; the checked fixture fingerprint is repeatable;
  `just check` passes; and the pre-PR implementation review verdict is Ready.
- **Depends on:** `EXR-TASK-012`, `EXR-TASK-013`.
- **Scope:** runner contracts/orchestration/artifacts only. No `Cna.Core` behavior, parallelism,
  pairing/statistics, new controller, service, database, dashboard, or side-safe export.
- **Work:** admit one strict serial-unpaired contract; propagate explicit child identity through the
  existing Exercise pipeline; reopen each finalized child exactly once; create a stable canonical
  aggregate; finalize and read back one report; expose a checked CLI/fixture.
- **Accept:** `EXR-AC-009`; all order/count/status invariants reconcile; every aggregate fact is
  traceable to an identity-matched trusted child bundle; missing/corrupt/unverifiable evidence stops
  honestly; timing/path variance cannot change the deterministic fingerprint; standalone behavior
  remains exact.
- **Project verification:**
  `dotnet test --project tests/Cna.ExerciseRunner.Tests/Cna.ExerciseRunner.Tests.csproj --no-build`,
  `dotnet test --solution Sandtable.slnx --no-build`, and `just check` in native .NET 10 MTP mode.

##### Task 014 activation decisions

| Decision | Accepted boundary |
| --- | --- |
| `EXR-014-DEC-001` | Task 014 admits ordered unpaired entries only. The manifest carries an explicit serial mode so Task 015 can add paired behavior without silently changing unpaired semantics. |
| `EXR-014-DEC-002` | The Maneuver owns the sole root seed. Nested Exercise specifications omit root seed and campaign ID and are materialized only after Maneuver admission. |
| `EXR-014-DEC-003` | A semantically valid, seed-ledger-identity-matched failed Exercise bundle does not stop later entries. Cancellation stops scheduling; inability to finalize, semantically validate, and identity-match trusted evidence stops execution and fails aggregation. A corroborated cancellation after scheduling but before Core begin is represented as not-run rather than as an attributable child failure. |
| `EXR-014-DEC-004` | The report fingerprint covers only a canonical deterministic section. Durations, throughput, local paths, GUIDs, and machine data remain visibly separate diagnostics. |
| `EXR-014-DEC-005` | Every aggregate fact comes from bytes retained by one successful `ExerciseBundleReader` validation. Aggregation neither trusts in-memory counters nor rereads files after validation. |
| `EXR-014-DEC-006` | Any failed Exercise makes the Maneuver process nonzero while still retaining its report. Missing, corrupt, or unfinalizable evidence is an explicit aggregate failure, never a success or omission. |
| `EXR-014-DEC-007` | Task 014 changes no `Cna.Core` rule, state, authority, event, or replay contract. |
| `EXR-014-DEC-008` | The existing normalized Exercise manifest remains the child evidence contract. Maneuver admission materializes it with the parent seed; no second child-manifest bundle schema is introduced. |
| `EXR-014-DEC-009` | Post-admission single-Exercise work is extracted behind one internal no-console coordinator. Both commands use it directly; Maneuver execution does not spawn a process or write a temporary manifest. |
| `EXR-014-DEC-010` | The completed Maneuver artifact is one canonical report file with its own partial/final placement and strict readback. Child bundles are independent transactions and are never rolled back. |
| `EXR-014-DEC-011` | Aggregation failure categories are distinct from Exercise failures. Only `succeeded`, `failed-executed`, `failed-reconstructed`, and `failed-readjudicated` are aggregate-eligible. Profiles without exact seed-ledger identity and `failed-summarized` cannot be attributed and stop aggregation, except the closed pre-Core cancellation shape in Decision 012. |
| `EXR-014-DEC-012` | Cancellation stops before the next entry, selects the dedicated cancellation exit/status, and retains later entries as explicit not-run records. When cancellation lands after the scheduler check but before Core begin, a successfully reopened `failed-identified` bundle with exact admitted manifest/build identity, cancelled coordinator/result, and no ledger marks the current entry and tail not-run/cancelled without attribution. Reader corruption retains aggregation precedence. Ordinary attributable child failures continue. |
| `EXR-014-DEC-013` | Maneuver admission is all-or-nothing before Core creation; duplicate Exercise IDs and child seed/campaign/pair/variant fields are rejected. |
| `EXR-014-DEC-014` | `standalone.` is a reserved synthetic Maneuver-ID prefix. User-authored Maneuver IDs cannot enter that namespace, preventing collision with `ExerciseRunIdentity.Standalone`. |
| `EXR-014-DEC-015` | Aggregate eligibility requires semantic cross-artifact validation, not only profile/path/hash/JSON-shape validation. Hash-valid reassembled contradictory bundles must fail readback. |
| `EXR-014-DEC-016` | Task 014 success is only `BoundaryReached` at the admitted `terminalBoundary`, corroborated by canonical final evidence. The shared reserved `VictoryReached` shape is rejected until the separately reviewed Core victory contract exists. |

##### Task 014 implementation slices

Each slice begins with focused failing tests and leaves the existing single-Exercise command green.

| Slice | Observable output | Dependencies | Acceptance and verification | Current status |
| --- | --- | --- | --- | --- |
| `EXR-014A` — strict Maneuver admission | New `Artifacts/ManeuverManifestContracts.cs`, `ManeuverManifestCodec.cs`, codec tests, and canonical fixture/golden | Tasks 012-013 | Start with strict/golden/culture/order failures; prove exact bytes, nonempty/unique ordered entries, reserved `standalone.` namespace, parent-only seed, and full pre-run admission | Implemented; admission/golden coverage is green within 252/252 ExerciseRunner tests. |
| `EXR-014B` — explicit identity propagation | `ExerciseExecutor.cs`, `ReadjudicationVerifier.cs`, `ExerciseDiagnosticsWriter.cs`, `ExerciseSummaryWriter.cs`, and identity-focused tests | Tasks 012-013 | Start with Maneuver-identity failures; prove executor, ledger, campaign, re-adjudication, zero-step summary, and diagnostics never synthesize standalone identity | Implemented; identity and diagnostics propagation coverage is green within 252/252. |
| `EXR-014C` — reusable run coordinator | New `Execution/ExerciseRunCoordinator.cs`, `ExerciseRunCommand.cs`, coordinator tests, and existing command tests | 014B | Start with standalone parity tests; extract post-admission work with no console I/O; preserve every existing standalone bundle/profile/exit/stdout/stderr/trace case | Implemented; coordinator and standalone parity coverage is green within 252/252. |
| `EXR-014D` — trusted semantic bundle view | `ExerciseBundleReader.cs`, new `ExerciseEvidenceCodec.cs`, new `ExerciseBundleSemanticValidator.cs`, reader tests, and semantic-validator tests | Tasks 012-013 | Start with rehashed-tampering/defensive-value tests; strictly retain manifest/build/ledger/result/check/proof/action/event/step facts from one read; enforce the exact profile/result/check/proof and cross-evidence matrix; bind success to admitted boundary/final evidence and reject victory; mutation/corruption/symlink/internal contradiction fail closed | Implemented; focused semantic suite 41/41 green. |
| `EXR-014E` — report contract and fingerprint | New `ManeuverReportContracts.cs`, `ManeuverReportCodec.cs`, codec tests, and report golden | 014A | Start with invariant/fingerprint failures; prove exact canonical bytes, terminal/failure catalog order, the complete per-entry state/null matrix, one record per ordinal, count reconciliation, culture/order stability, and diagnostics exclusion | Implemented; included in focused report suite 17/17 green. |
| `EXR-014F` — serial execution | New `Execution/ManeuverExecutionContracts.cs`, `ManeuverExecutor.cs`, and executor tests | 014A, 014C-014E | Start with scheduling/evidence failures; prove one-at-a-time manifest order, immediate one-read reopen, mandatory seed-ledger identity, ordinary eligible-failure continuation, early-profile/cancellation/aggregation stop, and explicit not-run tail | Implemented; focused executor suite 23/23 green, including review-fix profile and cancellation matrices. |
| `EXR-014G` — report transaction | New `ManeuverReportWriter.cs`, `ManeuverReportReader.cs`, and lifecycle tests | 014E | Start with failpoint/readback failures; prove confinement, regular-file-only exact tree, flush/move/readback, status placement, retained partial evidence, and no completed claim after failure | Implemented; focused report contract/transaction suite 17/17 green. |
| `EXR-014H` — command and checked fixture | New `ManeuverRunCommand.cs`, `Program.cs`, command tests, and `scenarios/maneuvers/rules-lab.serial.v2.json` | 014F-014G | Start with CLI golden/integration failures; checked fixture has at least two unique successful entries; prove exact exits/output, manifest order, success and mixed-failure reports, cancellation, aggregate corruption injection, fixture smoke, and unchanged `exercise run` | Implemented; focused command/fixture suite 10/10 green. |
| `EXR-014I` — reconciliation and gate | `README.md`, roadmap, `tech-design.md`, `naming-overview.md`, this design/spec, and retained command evidence | 014H | Update implemented status only after evidence; run focused project tests, solution MTP suite, fixture twice with fingerprint comparison, `just check`, `git diff --check`, and one normal pre-PR review | Complete; 252/252 ExerciseRunner and 562/562 solution tests green, `just check` passes, two fixture runs share the same fingerprint, and pre-PR review is Ready. |

The normal five-primary-file target applies to 014A-014H; fixtures/goldens are generated evidence,
not refactor permission. Slice 014I is the explicit documentation-close exception. Checkpoints are:

1. after 014A, freeze manifest bytes before orchestration consumes them;
2. after 014B-014E, require standalone parity plus trusted-reader and report-codec goldens;
3. after 014F-014G, require all lifecycle/failure/cancellation tests before exposing a command; and
4. after 014H-014I, run the complete repository gate and one implementation review.

##### Task 014 requirement-to-evidence map

| Governing requirement / decision | Owning slices | Required evidence before the checkpoint closes | Current evidence/status |
| --- | --- | --- | --- |
| `EXR-001`, `EXR-017`, `EXR-023`, `EXR-024`; `EXR-014-DEC-001`, 002, 008, 013-014 | 014A-014B | Manifest golden/strict-reader/reserved-namespace matrix; parent-seed materialization and Maneuver campaign/ledger identity goldens; no Core start on any invalid child | Implemented and green within the complete 252/252 ExerciseRunner suite. |
| `EXR-009`, `EXR-010`, `EXR-022`; `EXR-014-DEC-007`, 009 | 014B-014C | Re-adjudication/summary/diagnostic identity tests and complete standalone command parity suite; diff contains no `Cna.Core` behavior change | Implemented; explicit run identity, campaign/ledger, re-adjudication, zero-step summary, diagnostics, coordinator, and standalone parity coverage is green within 252/252; Task 014 made no Core behavior change. |
| `EXR-009`-`EXR-014`, `EXR-021`, `EXR-023`; `EXR-014-DEC-003`, 005, 008, 011, 015-016 | 014D, 014F | One-read semantic bundle view; exact manifest/mandatory ledger/build checks; proof/action/event/step/check/outcome coherence; altered-boundary/fabricated-victory and other rehashed tampering, mixed outcome, corrupt, missing, mismatch, early-profile, and not-run-tail tests | Implemented; semantic suite 41/41 and executor suite 23/23 green, including authenticated genuine replay-failure profiles, early-profile and executed manifest/build controls, the closed profile matrix, and a semantically empty post-scheduling/pre-Core cancellation race. |
| `EXR-011`, `EXR-015`, `EXR-021`, `EXR-023`; `EXR-014-DEC-004`, 006, 010, 012 | 014E, 014G | Report golden/strict-reader/count/nullability/catalog tests; timing/path variance fingerprint test; writer failpoint tree and final placement/readback tests | Implemented; report contract/transaction suite 17/17 green. |
| `EXR-NFR-002`, `EXR-NFR-004`, `EXR-NFR-005`, `EXR-AC-009` | 014H-014I | Exact CLI exit/output tests; checked fixture and mixed-failure integration; cancellation and aggregate-failure cases; project/solution/`just check` gates and retained smoke fingerprints | Complete: command/fixture 10/10, ExerciseRunner 252/252, solution 562/562, warning-free build, `just check`, identical twice-run smoke fingerprint, and Ready pre-PR review. |

##### Task 014 risks and stop conditions

- **Standalone drift:** coordinator extraction can silently alter existing bundle profiles or output.
  Slice 014C is blocked until characterization tests cover every current success and failure path;
  any intentional byte/exit change requires a reviewed contract amendment.
- **False attribution:** a valid artifact is not necessarily the intended child. Aggregation accepts
  it only after an aggregate-eligible profile, exact expected-manifest, mandatory seed-ledger
  identity, and build comparisons. Pre-executed and `failed-summarized` profiles are aggregation
  identity failures. The closed pre-Core cancellation case is not attributed: after successful
  readback it creates only a cancelled not-run tail. The `standalone.` namespace is reserved; no
  in-memory outcome may fill a missing trusted field.
- **Hash-valid semantic contradiction:** artifact hashes prove retained bytes, not that those bytes
  agree. Slice 014D adds strict evidence codecs, cross-artifact hashes/counts/coordinates, the closed
  profile/result/check/proof matrix, admitted-boundary/final-snapshot/outcome anchoring, explicit
  victory rejection, and mutate-then-rehash rejection tests before aggregation. This is
  internal-consistency validation for trusted local artifacts, not signature-based authenticity
  against a coherent full-tree rewrite.
- **Report self-reference:** report timing, final paths, and file hashes cannot be inside their own
  fingerprint material. The deterministic subobject is serialized and hashed first; diagnostics
  are appended outside it and finalization/readback timing stays in the post-readback command trace.
- **Cancellation evidence loss:** cancellation stops scheduling but does not cancel safe child or
  report finalization. If safe finalization itself fails, artifact/aggregation precedence applies
  and no completed path is claimed.
- **Scope creep into pairing/performance claims:** Task 014 produces serial descriptive evidence
  only. Any pair key/variant comparison, parallel scheduler, significance metric, threshold, or
  optimization based on these reports stops for Task 015 or a separately reviewed amendment.

The three accepted research spikes settle capability, artifact, and seed/pair boundaries. The
original fresh-context
review of this specification/design diff is complete and reconciled. Renewed user direction
authorized exactly one fresh-context final review of the corrected diff. It was not a recursive
review tree, and no further planning review starts without another explicit request.

##### Task 014 independent-review reconciliation

The single fresh review task was `01a021e0-9514-7a31-ad2e-0fb221844eac`. It performed a blind pass,
recorded its preliminary ledger, then received the separate author explanation. Its verdict was
`Not Ready`; all three findings are accepted and incorporated into the plan without starting a
second review task.

| Severity | Finding | Reconciliation |
| --- | --- | --- |
| P1 | Early failed profiles could not prove Maneuver ID/ordinal, and user Maneuver IDs could collide with synthetic `standalone.<exerciseId>`. | Accepted. Aggregate attribution now requires an exact seed-ledger identity, every pre-executed profile becomes `bundle-identity-mismatch`, and `standalone.` is reserved. |
| P1 | Hash-valid reader output did not semantically validate accepted actions, proof status, or cross-artifact relationships used by aggregation. | Accepted. Slice 014D now owns strict evidence codecs, a closed semantic validator, recomputed evidence/proof hashes, count/coordinate/check/profile coherence, and mutate-then-rehash rejection tests. |
| P2 | Entry nullability/not-run values and terminal-kind ordering were incomplete for a canonical strict report. | Accepted. The spec now has a normative four-state matrix, exact not-run reasons, required/null count/hash behavior, and `boundary-reached` before `victory-reached` ordering. |

Because those corrections tightened trust and canonical-contract behavior after a `Not Ready`
verdict, production implementation remained blocked until the authorized final review below.

##### Task 014 final independent-review reconciliation

The one user-authorized final review task was `01a021f9-4dc0-7d20-997e-37c9126735b9`. It verified
the frozen two-file snapshot and diff hash, recorded its blind preliminary ledger, then received the
separate corrected author explanation. Its verdict was `Not ready` with one new P1 and no other
actionable defects. The finding is accepted and corrected here; no additional review pass was
started.

| Severity | Finding | Reconciliation |
| --- | --- | --- |
| P1 | Semantic validation did not bind a successful run-result terminal outcome to the admitted terminal boundary and final evidence, so a rehashed changed `BoundaryReached` or fabricated `VictoryReached` could corrupt terminal counts. | Accepted. Task 014 success now requires `BoundaryReached(manifest.terminalBoundary)`, the decoded final snapshot and final step when present must agree, zero-step success must begin/end at that boundary, `VictoryReached` is rejected, and both mutations receive rehashed-tampering tests. |

The final planning reviewer independently confirmed that the early-profile identity/`standalone.`
finding and the report state/nullability/order finding were closed. It found semantic aggregate
validation only partially closed because of the terminal anchor above; implementation now enforces
that invariant and its rehashed-tampering coverage is green. Preliminary implementation-review
findings about the post-scheduling cancellation race and artifact-profile eligibility were also
corrected and are covered by the 23/23 executor suite. The final implementation review additionally
closed replay-proof authentication, failure-profile, maximum-step, and CLI-confinement gaps before
returning a Ready verdict.

The later user-requested final-product independent review is a separate loop bounded to three fresh
instances. Instances 1 and 2 returned `Not ready` and exposed additional manifest-control,
replay/cancellation-evidence, build-mode, documentation, and downstream Stage Entry migration-map
gaps. All findings are accepted: the semantic suite now covers the complete execution-control and
empty pre-Core profile matrix, the executor requires the same empty footprint, and the Stage Entry
task map includes the audited snapshot/version consumers. Instance 3 returned `Ready with
non-blocking follow-up`: its sole P2 was reader-level early-profile manifest/build consistency,
which was then corrected with focused rehashed tests. The independent-review limit is exhausted;
the bounded post-review patch received a separate Ready code-quality verdict.

Task 015 paired comparison is a separate implemented track and is not a dependency of Reserve
engine work. The delivered Stage Entry engine track is documented in the
[Operation-Stage Entry research](../research/operation-stage-entry-spike.md),
[specification](../specs/operation-stage-entry-v1.md), and
[technical design](operation-stage-entry-v1.md), followed by the
[Reserve Designation specification](../specs/reserve-designation-v1.md) and
[technical design](reserve-designation-v1.md). The checked Reserve-terminal regression profile and
the 12-step Reserve Designation profile prove the path through canonical first-side Movement using
the unchanged harness authority boundary. Movement Foundation Tasks 001-010 now provide the merged
rules/content/world, public observation-derived move/completion actions, internal non-contact
adjudication/replay, checked Harness evidence, simulator study, and synchronized review without
changing the Harness authority boundary.

#### `EXR-TASK-014J` — Implement the controller-policy coverage matrix

- **Status:** implemented and repository-verified; 293/293 ExerciseRunner and 705/705 solution tests
  pass, the build is warning-free, and two checked runs share the same report fingerprint.
- **Depends on:** `EXR-TASK-014` and the implemented Reserve Designation vertical.
- **Primary files:** `ExerciseController.cs`, the controller manifest codecs/configuration identity,
  one checked six-child Maneuver, and focused controller/command tests.
- **Scope:** ExerciseRunner policy and evidence coverage only. No `Cna.Core` rule, authority,
  command, event, observation, snapshot, or replay behavior changes.
- **Work:** add six closed controller tokens crossing `act-first`/`act-last` with Reserve
  `none`/`one`/`all`; provide the pure selector with the per-audience count of previously accepted
  designation actions; fail closed on malformed initiative/Reserve candidate sets; retain one
  strictly read-back aggregate over all six trajectories.
- **Accept:** `EXR-AC-016`; existing controller tokens remain byte- and behavior-compatible; the
  `one` policy is history-count based rather than tied to the current two-element fixture; no
  controller choice can bypass exact legal-action membership and ordinary submission.
- **Verify:** focused controller, manifest, configuration-identity, executor, and checked-fixture
  tests; complete ExerciseRunner and solution suites; one six-child Maneuver run; `just check` and
  `git diff --check`.

#### `EXR-TASK-015` — Implement paired comparison contract/report

- **Status:** implemented and repository-verified; 347/347 ExerciseRunner and 922/922 solution
  tests pass, the build is warning-free, and `just check` is green. It remains optional
  instrumentation and does not block the gameplay-engine dependency graph.
- **Depends on:** `EXR-TASK-014`.
- **Primary files:** paired manifest/report codecs and contracts, `PairedManeuverExecutor.cs`,
  `Artifacts/PairedReportWriter.cs`, the checked paired fixture, and focused paired tests.
- **Work:** preserve the strict serial-unpaired v2 contract; admit a separate serial-paired v1
  contract; validate equal declared and observed creation inputs, build cohort, initial snapshots,
  and complete role/domain seed ledgers; run isolated arms sequentially; record controller
  configuration identities separately; retain the compact accepted-action identities needed to
  recompute exact first divergence on standalone report readback; and emit constrained descriptive
  divergence. Child summary/diagnostic `variant` stays the `paired` execution-mode token; the
  parent report alone assigns `baseline`/`candidate`, keeping that controlled dimension out of run
  identity and initial evidence.
- **Accept:** initial ledgers match by role/domain; implementation IDs do not perturb seeds; strict
  readback rejects malformed, ambiguous, contradictory, or noncanonical paired artifacts; report
  recomputes manifest-derived creation identity, requires equal per-arm initial-snapshot
  commitments, and verifies the claimed first divergence from its retained transcripts; report
  includes the exact divergence limitation and no prohibited causal/significance/balance claim.
- **Verify:** `EXR-AC-010`, manifest/report canonical and strict-reader tests, lifecycle tests,
  executor mismatch/isolation/repeatability tests, twice-run checked integration, complete
  ExerciseRunner and solution suites, `just check`, and independent review.

#### `MOV-TASK-009` — Adopt executable Movement evidence

- **Status:** complete and merged in PR #78, including two matching clean CLI executions.
- **Depends on:** the merged public Movement vertical from `MOV-TASK-008`; existing serial-unpaired
  and optional serial-paired contracts remain compatible.
- **Primary files:** `ExerciseController.cs`, `ExerciseExecutor.cs`, manifest/configuration codecs,
  strict bundle/report semantic readers, `scenarios/maneuvers/rules-lab.movement.serial.v2.json`,
  focused tests, and the retained Movement simulator study.
- **Frozen contract:** retain Exercise manifest v2, `sandtable.maneuver-manifest.v2`, and
  `sandtable.exercise-controller-configuration.v2`; add only the six
  `act-{first,last}-reserve-{none,one,all}-move-each-once-then-complete` tokens. Advance the
  Runner-local controller candidate to v2: Reserve designation keeps its existing own `elementId`;
  move candidates require own `elementId`, `originLocationId`, and `destinationLocationId`; reject
  existing v1 candidates.
- **Controller/history:** the executor owns a per-audience set of element IDs whose move submission
  was accepted. The controller selects a deterministic supported current move for an eligible
  element absent from that set, then selects the exact current Movement completion. History never
  becomes campaign authority, evidence input, or permission to bypass membership/submission.
  Selection uses ordinal `elementId`, `destinationLocationId`, `originLocationId`, then `actionId`;
  changing that order requires a new controller token/configuration identity.
- **Strict evidence:** validate every Movement event's identity, origin/destination, exact cost,
  before/after CP and Cohesion, unique moved-element policy, Reserve state, exact Breakdown terminal,
  and aggregate reconciliation. Reopen bundles and reports canonically and reject malformed,
  inconsistent, noncanonical, or rehashed tampering. Reconstruction and fresh-session
  re-adjudication must reproduce each child's retained transcript, events, and final snapshot.
- **Checked fixture:** six children cross act-first/act-last with Reserve none/one/all. Each accepts
  13 actions/events, records 94 passed checks, and reaches exact first-side Breakdown Determination.
  Per initiative branch Reserve counts are 0/1/2 and move counts 2/1/0. The none path moves A to the
  center for CP cost 8 and B on its supported route for cost 1; one moves B for cost 1; all moves
  neither. Aggregate evidence is 78 actions/events, six Reserve designations, six Reserve
  completions, six moves, six Movement completions, and exact final CP expenditure 20.
- **Successor compatibility:** those checked-fixture counts describe merged `MOV-TASK-009` before
  Reaction activation. With `ZOR-TASK-006C`, Reserve-none opens Reaction after its first move and
  the Runner deterministically fails closed after 11 accepted steps; Reserve-one/all retain their
  exact 13-step Breakdown evidence. `ZOR-TASK-007A` owns participant/System controller selection.
- **Fingerprint/study:** two clean CLI executions into separate artifact roots reconfirmed
  `sha256:c1c20270dcd3402886931c28851bea7f23cd1e0778b45f94c43d85ed01d41c4b`; it does not bind detailed
  child event/ledger bytes. `MovementSimulatorStudyTests` executes four boundary/interior seeds by
  six controllers by two repeats (48 trajectories), proves exact within-seed evidence equality, and
  observes route invariance. This is a determinism/sensitivity probe, not a statistical sample.
- **Verify:** focused Runner tests, complete solution tests, strict artifact readback, two clean CLI
  Maneuver runs, `git diff --check`, `just check`, and fresh-context independent review.

### Checkpoint F — repository reconciliation and close

#### `EXR-TASK-016` — Documentation and repository gates

- **Status:** implementation and repository gates are complete on the post-PR-73 baseline; all
  checked commands finalize only reader-validated bundles/reports, 347/347 focused and 946/946
  solution tests pass with 0 skipped, and the build has 0 warnings/errors. Fresh-context independent
  review is a separate required delivery gate; its verdict and any fixes belong in the PR evidence.
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

Status records the implemented single-Exercise, serial-unpaired Maneuver, optional
paired-comparison, and repository-reconciliation boundary; PR evidence records the final review
verdict.

| Requirements / decisions | Delivery tasks | Verification / retained evidence | Status |
| --- | --- | --- | --- |
| `EXR-001`, `EXR-006`, `EXR-007`, `EXR-008`; terminal/failure separation | 006, 007, 012, 014 | Manifest/result goldens, CLI exits, success/cancellation/admission/artifact-failure bundles and serial reports | implemented through serial Maneuvers |
| `EXR-002`, `EXR-003`, `EXR-004`, `EXR-005`; isolated capability and shared primitives | 001-004 | Core parity, reflection/type-graph, rejection nonmutation tests | implemented |
| `EXR-009`; history reconstruction | 005, 008, 012 | Reconstruction proof file and corrupt-history tests | implemented |
| `EXR-010`; action-identity re-adjudication | 008, 012 | Re-adjudication proof and independent action/event/final mismatch tests | implemented |
| `EXR-011`, `EXR-012`, `EXR-023`; canonical/versioned evidence | 006, 008-010, 012-015 | Golden/strict-reader/order tests, reader-validated CLI bundles, semantic child validation, canonical serial reports, and separate paired report readback | implemented through paired Maneuvers |
| `EXR-013`, `EXR-014`; manifest-last transaction | 009, 010, 012 | Writer failpoint trees, rejected partials, reopened success/failure bundles | implemented |
| `EXR-015`; confidentiality/detail separation | 006, 009, 010, 012, 014 + observability hardening | Cross-detail evidence-invariance and monotonic-tier tests; child bundles and Maneuver reports remain trusted-authority | implemented through serial Maneuvers |
| `EXR-016`; side-safe export deferred | 016 and future separately authorized task | Spec non-goal; absence/public-API tests; future whole-tree noninterference evidence | deferred |
| `EXR-017`; seed domain separation | 013-015 | Standalone, serial, and paired identity/seed-ledger goldens; culture/order, campaign, re-adjudication, summary, diagnostics, and equal-ledger integration tests | implemented |
| `EXR-018`; honest pairing | 013-015 | `EXR-AC-010`; paired manifest/report canonical tests, equal campaign/seed/initial-snapshot integration, first-divergence evidence, fixed limitation | implemented |
| `EXR-019`, `EXR-020`; clean baseline and dirty exploration | 011, 012 + observability hardening | Fake/integration identity cases, emitted build identity, and separate checked baseline/exploratory fixtures | implemented |
| `EXR-021`; serial validated aggregation | 014, 015 | `EXR-AC-009`, `EXR-AC-010`; existing serial suites plus paired admission/report/lifecycle/executor/checked-fixture tests | implemented for serial-unpaired and serial-paired modes |
| `EXR-022`; separate correlated diagnostics | 007, 012, 014 + observability hardening | Successful and failed query/controller/submission/check/proof correlation, explicit Maneuver identity, aggregate noncanonical diagnostics, debug failure timings, artifact readback trace, and command-boundary cross-detail evidence | implemented through serial Maneuvers |
| `EXR-024`, `EXR-025`; deterministic selection and single active audience | 006, 007, 012 | Executor controller/cardinality/step-bound tests and checked-in fixture | implemented |
| `EXR-026`; ordered invariant catalog | 006-008, 012 | Strict check codec, ordering, scope, failure, and emitted-bundle tests | implemented |
| `EXR-027`; bounded Movement selection and semantic evidence | `MOV-TASK-009` plus post-adoption sensitivity instrument | Six-child checked Movement fixture, accepted-move history tests, strict event/ledger tamper tests, reconstruction/re-adjudication, 48-trajectory study, two clean CLI fingerprints, and a paired stable-route/lowest-cost comparison | Task 009 merged; paired sensitivity instrument locally verified with repeatable fingerprint |
| `EXR-NFR-001`-`005`; reproducibility, reliability, boundaries, quality | 001-016 | Warning-free build, complete focused/solution suites, repeatable serial and paired fingerprints, `just check`, runnable-example readback, and independent review | implementation/repository gates verified; final review verdict retained in PR evidence |
| `DEC-001`-`DEC-016` research decisions | 001-016 as mapped above | Three retained research spikes, governing spec, serial bundles/reports, paired equality/divergence evidence, and synchronized repository maps | accepted and implemented through Task 016 |

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

At `EXR-TASK-016`, run `just check`, execute every documented checked manifest from a verified clean
checkout—including the single-Exercise, serial-unpaired, controller-matrix, and serial-paired
boundaries—validate every emitted child bundle and parent report through its strict reader, compare
two clean single-run canonical trees, and retain exact command/output evidence in the PR. No remote
provider, timing threshold, causal inference, significance test, balance conclusion,
recommendation, or synchronized-post-divergence assumption participates in the gate.
