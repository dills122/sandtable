# Exercise Harness v1 Specification

**Status:** Implemented and repository-verified through `EXR-TASK-014J`; checked Reserve
Designation adoption and the six-policy controller matrix reach first-side Movement; Task 015
paired comparison remains pending

**Date:** 2026-08-20

**Roadmap capability:** `EXERCISE-001`

**Rules target:** `cna-1979.1`

**Predecessors:** [Legal Actions v1](legal-actions-v1.md),
[Weather Determination v1](weather-determination-v1.md)

**Technical design:** [Exercise Harness v1](../design/exercise-harness-v1.md)

**Next bounded Harness checkpoint:** optional `EXR-TASK-015` paired comparison; it does not block
gameplay-engine work

**Delivered engine track:** [Operation-Stage Entry research](../research/operation-stage-entry-spike.md),
[specification](operation-stage-entry-v1.md), and
[technical design](../design/operation-stage-entry-v1.md), followed by the
[Reserve Designation specification](reserve-designation-v1.md) and
[technical design](../design/reserve-designation-v1.md); Movement behavior remains unsupported

**Research decisions:**
[capability and replay](../research/exercise-capability-and-replay-spike.md),
[evidence artifacts](../research/exercise-evidence-artifact-spike.md), and
[reproducibility and pairing](../research/exercise-reproducibility-and-pairing-spike.md)

## Objective

Create a deterministic local simulation harness that makes one bounded campaign run observable,
reproducible, and diagnosable without creating a second Umpire or weakening fog-of-war boundaries.
One **Exercise** is one run. **Maneuvers** is an ordered collection of Exercises, optionally arranged
as paired variant comparisons.

Version 1 is trusted developer instrumentation. It runs only freshly created synthetic campaigns,
uses the same public legal-action identities and the same internal authoritative execution primitive
as ordinary campaign play, and retains canonical evidence plus separate diagnostics. The original
fixture stops at Organization, the retained Stage Entry fixture stops at Reserve, and the current
Reserve Designation fixture accepts 12 actions through two element designations plus completion to
`land.position.operation-1.first-player.movement-and-combat.movement`. Movement itself remains
unsupported; none of these profiles implies a full victory-capable game.

## User-visible demonstration

1. Run the checked-in rules-laboratory Exercise manifest with a declared root seed and artifact root.
2. Receive the finalized bundle path on standard output; failures use stable nonzero exits and
   standard error without exposing secret-bearing authority values.
3. Receive a finalized trusted artifact bundle containing the complete accepted action transcript,
   canonical events, initial/final snapshots, seed ledger, build identity, checks, and summary.
4. See both replay proofs pass: canonical events reconstruct the final snapshot byte-for-byte, and a
   fresh Exercise session re-adjudicates accepted audience/action identities to the same transcript,
   events, and final snapshot.
5. Select compact, forensic, or debug detail without changing accepted actions or canonical
   simulation evidence. Forensic records query/controller/check/proof evidence; debug additionally
   records noncanonical monotonic timings and post-readback artifact trace data.
6. Run a small serial Maneuver and receive deterministic per-Exercise results plus aggregate counts.
7. Run a paired Maneuver whose variants share declared initial conditions and role-specific initial
   random streams, with reporting that makes no claim that streams remain synchronized after paths
   diverge.
8. Introduce an invalid manifest, replay mismatch, invariant failure, interruption, step limit, or
   artifact fault and receive a nonzero process exit. A failed bundle is retained whenever staging
   and safe finalization remain possible; otherwise stderr explicitly reports that no completed
   bundle exists. No expected-failure field can convert such a failure into success.

## Scope and accepted boundary

- V1 is an in-process local developer tool. It adds no HTTP, gRPC, Orleans, Intelligence, model,
  database, or distributed-execution path.
- `Cna.Core` remains the sole owner of authoritative state, validation, rules, RNG, legal actions,
  adjudication, projection, events, and replay semantics.
- An Exercise starts only from Core-owned campaign creation inputs. It cannot attach to, unwrap,
  convert from, or convert into an ordinary `CampaignAuthorityHandle`.
- The Exercise capability is opaque, sealed, non-record, nonserializable, and scoped to fresh
  simulation. It does not authorize resume, activation, arbitrary snapshot import, or production
  campaign export.
- Core owns trusted domain evidence contracts. The runner owns manifests, controller policies,
  orchestration, filesystem artifacts, reports, process exits, and diagnostic telemetry.
- V1 bundles are classified `trusted-authority`. Side-safe export is a later milestone requiring
  physically independent bundles and whole-tree noninterference tests.
- Canonical evidence is byte-stable and clock-free. Human-readable logs, durations, machine facts,
  and stack traces are diagnostics and never participate in replay equality or result identity.
- Exercise artifacts live beneath `artifacts/exercises/`, which the repository already ignores.

## Commands

The implemented single-Exercise command is:

```text
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.v2.json \
  --artifact-root artifacts/exercises
```

That fixture is explicitly exploratory. A second checked fixture requests the same bounded run as
a clean baseline and therefore fails closed unless the checkout has no tracked or untracked changes:

```text
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.baseline.v2.json \
  --artifact-root artifacts/exercises
```

The checked Stage Entry Exercise and its clean-checkout baseline twin use
`scenarios/exercises/rules-lab.reserve.v2.json` and
`scenarios/exercises/rules-lab.reserve.baseline.v2.json`. They retain the same artifact contract
while accepting nine actions through the explicit-empty Stage Entry path to Reserve.

The checked Reserve Designation Exercise and baseline twin use
`scenarios/exercises/rules-lab.reserve-designation.v2.json` and
`scenarios/exercises/rules-lab.reserve-designation.baseline.v2.json`. The stateless semantic
controller designates both currently eligible elements in ordinal element-ID order, completes
Reserve, and reaches first-side Movement in exactly 12 accepted actions. Both reconstruction and
fresh-session re-adjudication are required to verify.

The controller-policy matrix extends the same closed v2 controller-token field without adding
authority or changing the manifest shape. Its six deterministic policies cross initiative
declaration `act-first`/`act-last` with Reserve selection `none`/`one`/`all`, then fall back to the
stable first-by-action-ID rule at other checkpoints. Initiative selection requires the exact two
declaration candidates. Reserve selection requires one completion candidate and zero or more
designation candidates; designation order is element ID then action ID. `none` completes
immediately, `one` designates exactly once when its accepted-designation count is zero and then
completes, and `all` designates until no candidate remains and then completes.

The runner supplies each pure controller call with only the count of that audience's previously
accepted Reserve-designation actions in the current Exercise. That count is deterministic
controller history, not authoritative campaign state, is incremented only after an accepted
designation, is never serialized into campaign evidence, and cannot bypass legal-action
membership or submission. A checked six-child serial Maneuver must prove every policy reaches the
exact first-side Movement boundary with strict bundle/report readback.

The manifest's `detail` value is `compact`, `forensic`, or `debug`. Compact retains accepted-step
and completion records. Forensic adds deterministic query candidate counts, controller selection,
ordered checks, proof results/hashes, payload sizing, and progressively assembled context for every
failed in-execution decision. Debug is a strict superset with
noncanonical monotonic operation/phase timings; after manifest-last write and mandatory reader
validation it also prints a structured `trace=` record for artifact finalization/readback timing.
Failed debug runs retain every timing measured before the failure.
No detail tier changes the simulation-evidence subset named by `EXR-022`.

The implemented serial-Maneuver command uses the checked Task 014 serial fixture; the paired fixture
remains Task 015:

```text
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.serial.v2.json \
  --artifact-root artifacts/exercises
```

The checked `scenarios/maneuvers/rules-lab.stage-entry.serial.v2.json` fixture runs both admitted
synthetic setups through Stage Entry to Reserve and aggregates their reader-validated bundles under
the unchanged `serial-unpaired` report contract.

Unknown commands/options, missing values, invalid paths, invalid manifests, and unsupported contract
versions fail before campaign creation with a nonzero exit. CLI spelling may change only before its
golden contract test is accepted; afterward it is versioned user-visible behavior.

### Current serial-Maneuver contract

The current clean-cut identity is `sandtable.maneuver-manifest.v2` in mode `serial-unpaired`. A manifest has this
exact canonical property order and shape; the child objects deliberately match the existing
Exercise manifest except that only the Maneuver owns `rootSeed`:

```json
{"contractVersion":2,"schemeId":"sandtable.maneuver-manifest.v2","maneuverId":"rules-lab.serial","mode":"serial-unpaired","rootSeed":0,"report":{"profile":"trusted-authority"},"exercises":[{"contractVersion":2,"exerciseId":"organization-boundary.first","setupId":"rules-lab.initiative.predetermined","setupHash":"sha256:c1688f8869ca66182b87f487ec34edbef617ff1158f7d8b0d3101fe3993978ef","contentPackId":"rules-lab.content.movement-contact.v1","contentHash":"sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99","scenarioId":"movement-contact-lab","rulesetHash":"beb66b242222f1ccc8bde4a34daacfcd561495b47e3d48391ede34e16830d6e6","terminalBoundary":"land.position.operation-1.organization","maximumSteps":8,"buildMode":"exploratory","confidentiality":"trusted-authority","detail":"forensic","controllers":{"system":"first-by-action-id","axis":"first-by-action-id","commonwealth":"first-by-action-id"},"assertFailureCategory":null}]}
```

The example uses the current admitted repository identities; implementation updates it if those
versioned identities change. The codec rejects unknown versions/schemes/modes/profiles,
missing/extra/duplicate/out-of-order properties, an empty Exercise list, duplicate Exercise IDs,
any Maneuver ID beginning with reserved synthetic namespace `standalone.`, child `rootSeed` or
`campaignId`, and every shape rejected by the standalone Exercise contract. Array order is
semantic. After full Maneuver admission, entry `N` is materialized as the existing normalized
Exercise manifest with the
Maneuver root seed and receives identity `(maneuverId, exerciseOrdinal=N, pairKey=null,
variant=unpaired)`. Paired keys, variants, repetitions, and child seed overrides require the later
Task 015 contract rather than permissive v2 fields.

Admission is all-or-nothing and occurs before the first child starts. Execution then runs exactly
one admitted child at a time in manifest order. A valid identity-matched failed Exercise bundle is
counted as that Exercise's failure and does not stop later entries. Identity matching requires a
seed ledger whose root seed, Maneuver ID, ordinal, and null pair key equal the admitted entry;
only `succeeded`, `failed-executed`, `failed-reconstructed`, and `failed-readjudicated` profiles are
aggregate-eligible. `failed-pre-admission`, `failed-admitted`, and `failed-identified` contain no
ledger and cannot be attributed to a Maneuver child; `failed-summarized` is also ineligible even
when it retains a ledger. These unsupported profiles stop aggregation as an identity failure. The
one non-attribution exception is cancellation observed after the scheduler check but before Core
begin: a successfully reopened `failed-identified` bundle with coordinator exit `cancelled`, exact
admitted manifest/build identity, a cancelled run result, and no ledger makes the current entry and
tail `not-run/cancelled`; it does not fabricate a child outcome. Reader corruption still takes
precedence and stops as `bundle-invalid`. Cancellation stops before the next entry. Any other
missing, invalid, or identity-mismatched completed bundle stops execution because no trusted
aggregate fact exists for that entry; remaining entries are retained as explicit `not-run` records
rather than omitted.

### Task 014 report and completion protocol

One canonical `maneuver-report.json` is the completed Maneuver artifact. It is staged beneath
`<artifact-root>/maneuvers/.partial/<unique-id>/`, durably flushed, moved to exactly one
`succeeded/<unique-id>/` or `failed/<unique-id>/` directory, and strictly reopened before the CLI
prints its path. A completed report directory contains that one regular file and no links,
subdirectories, or unlisted files. Partial and corrupt reports are never claimed as completed and
are not automatically deleted. Child Exercise bundles remain independently finalized evidence.

The report's exact top-level order is `contractVersion`, `schemeId`, `deterministic`,
`reportFingerprint`, `diagnostics`. `schemeId` is `sandtable.maneuver-report.v1` and
`reportFingerprint` is `sha256:` plus lowercase SHA-256 of the exact canonical bytes produced by
serializing the typed `deterministic` object alone. The strict reader reconstructs those bytes,
verifies the fingerprint, rejects noncanonical input, and verifies that directory placement agrees
with deterministic status. Diagnostics are visible and validated but never fingerprint input.

The deterministic object's exact property order is `manifest`, `status`, `counts`,
`terminalCounts`, `failureCounts`, `aggregationFailureCounts`, `entries`. It contains:

- the complete normalized Maneuver manifest;
- overall status: `succeeded`, `exercise-failed`, `aggregation-failed`, or `cancelled`;
- a `counts` object with exact property order `requestedExerciseCount`, `attemptedExerciseCount`,
  `validatedExerciseCount`, `succeededExerciseCount`, `failedExerciseCount`,
  `aggregationFailedExerciseCount`, `notRunExerciseCount`;
- terminal-count entries with exact order `kind`, `positionId`, `victor`, `count`, aggregated and
  ordered by terminal kind then the populated ordinal value;
- Exercise failure-count entries with exact order `category`, `count`, containing every closed
  `ExerciseFailureCategory` in catalog order, including zeros;
- all aggregation categories in fixed order: `completed-bundle-missing`, `bundle-invalid`, and
  `bundle-identity-mismatch`, as `category`, `count` entries including zeros; and
- one entry per manifest ordinal, never fewer, with exact property order `ordinal`, `exerciseId`,
  `variant`, `status`, `terminalOutcome`, `failureCategory`, `aggregationFailureCategory`,
  `notRunReason`, `acceptedStepCount`, `passedCheckCount`, `failedCheckCount`,
  `normalizedManifestSha256`, `seedLedgerSha256`; `variant` is always `unpaired` and status is
  `succeeded`/`failed`/`aggregation-failed`/`not-run`. Terminal outcomes use the existing exact
  `kind`, `positionId`, `victor` shape.

The entry state matrix is normative; `required` hashes use canonical lowercase `sha256:` values and
`count` means a nonnegative JSON integer:

| Entry status | `terminalOutcome` | `failureCategory` | `aggregationFailureCategory` | `notRunReason` | accepted/passed/failed counts | manifest/ledger hashes |
| --- | --- | --- | --- | --- | --- | --- |
| `succeeded` | required | null | null | null | required; failed checks must be zero | both required |
| `failed` | null | required | null | null | all required | both required |
| `aggregation-failed` | null | null | required | null | all null | both null |
| `not-run` | null | null | null | `cancelled` or `aggregation-stopped` | all null | both null |

Only seed-ledger-bearing, semantically validated executed-or-later profiles can produce
`succeeded` or `failed` entries. Terminal-count kind catalog order is exactly `boundary-reached`,
then reserved `victory-reached`; within one kind entries sort by `positionId` or `victor` using
ordinal string order. A `boundary-reached` count requires `positionId` and null `victor`.
Task 014 permits only `boundary-reached`: every successful child outcome and terminal-count entry
must carry the exact admitted child `terminalBoundary`. `victory-reached` is rejected during
semantic bundle readback and cannot appear in a Task 014 entry or terminal count; enabling it
requires the existing reviewed victory-semantics contract gate.

Count invariants are exact: requested equals entry count, attempted plus not-run, and succeeded plus
failed plus aggregation-failed plus not-run; attempted equals validated plus aggregation-failed;
validated equals succeeded plus failed. Terminal counts sum to succeeded, Exercise failure counts
sum to failed, and aggregation-failure counts sum to aggregation-failed. For identity-matched
executed profiles, `acceptedStepCount` is the validated `accepted-actions.jsonl` record count
retained during bundle readback; aggregate-eligible failures before any accepted step use zero,
while aggregation-failed and not-run entries use null.

Every terminal, failure, check, manifest-hash, and seed-ledger fact comes from defensive typed values
or semantically validated record counts retained by one successful `ExerciseBundleReader` call.
The aggregator does not trust its in-memory execution result and does not reread child files after
validation. It requires the retained normalized child manifest to equal the expected materialized
bytes, an exact seed-ledger identity match, and the available build-identity manifest/ruleset/
configuration hashes to match the expected entry before accepting it.
Intended manifest identity may label a missing/corrupt entry, but no unverified bundle outcome is
attributed to that Exercise.

For every aggregate-eligible executed-or-later bundle, reader validation is semantic and
cross-artifact, not merely hash/JSON-shape validation:

- accepted-action and step-evidence records use strict canonical codecs with exact
  version/scheme/property order/types, contiguous zero-based ordinals, stable audience/action/
  campaign values, and one-version receipt continuity;
- accepted-action, canonical-event, and step-evidence record counts are equal; corresponding
  action/step records agree on ordinal, campaign, audience, action, committed state, and position;
  each step event hash matches its canonical event record and the final step snapshot hash matches
  `final-snapshot.json`;
- reconstruction/readjudication expected event, transcript, and final-snapshot hashes recompute
  from retained canonical evidence; `succeeded` requires both proofs verified and terminal,
  reconstruction, and readjudication checks passed;
- for `succeeded`, the run-result outcome must be `BoundaryReached` with `positionId` exactly equal
  to the expected materialized manifest's `terminalBoundary`; canonical deserialization of
  `final-snapshot.json` must yield that same position. With accepted steps, the final step receipt,
  step evidence, and snapshot checkpoint must also yield that position. With zero accepted steps,
  initial and final canonical snapshot bytes must be identical and decode to that position. A
  `VictoryReached` outcome is non-aggregate-eligible in Task 014 even though the shared result codec
  can parse the reserved type;
- aggregate eligibility is closed to `succeeded`, `failed-executed`, `failed-reconstructed`, and
  `failed-readjudicated`. Every completed accepted step has the full passed step-check catalog.
  `failed-executed` has no replay proof, ends with failed check `terminal-boundary` /
  `terminal-boundary-not-reached`, and permits only the following preceding category/check/failure
  combinations: `controller-failed` with `selected-action-membership` /
  `selected-action-not-current`; `no-unique-legal-action` with `active-audience-cardinality` /
  `no-active-audience`; `illegal-action` with `accepted-event-cardinality` / `action-rejected`;
  `invariant-failed` with `authority-query-valid` / (`authority-query-rejected` or
  `authority-query-coordinate-mismatch`), `active-audience-cardinality` /
  `multiple-active-audiences`, `accepted-event-cardinality` / `event-cardinality-mismatch`, or
  `checkpoint-continuity` / (`campaign-mismatch`, `ruleset-mismatch`,
  `state-version-discontinuity`, or `position-mismatch`); and `step-limit-exceeded` or `cancelled`
  with no preceding failed step check. `failed-reconstructed` requires category
  `reconstruction-mismatch`, terminal passed, `history-reconstruction` /
  `reconstruction-mismatch` failed, and one unverified reconstruction proof.
  `failed-readjudicated` requires category `readjudication-mismatch`, terminal and history passed,
  a verified reconstruction proof, `readjudication` / `readjudication-mismatch` failed, and one
  unverified readjudication proof. `succeeded` requires terminal, history, and readjudication passed
  plus verified reconstruction and readjudication proofs. `manifest-invalid` from rejected Core
  begin remains `failed-identified` because no initial snapshot exists, so it has no serialized seed
  ledger and is non-attributable. Every other profile is non-attributable or
  non-aggregate-eligible in Task 014; and
- tests alter each trusted fact, regenerate sizes/hashes and `artifact-manifest.json`, and require
  readback rejection, including changing a successful boundary outcome and fabricating a victory
  outcome, so a rehashed internally contradictory tree cannot become aggregate input.

These checks prove canonical internal consistency under the trusted-developer artifact model; v1
does not claim cryptographic origin/authenticity against an actor who can coherently rewrite every
payload and hash. Signing, remote attestation, and hostile artifact ingestion remain out of scope.

The diagnostics object's exact property order is `elapsedMicroseconds`, `throughput`, `entries`.
Throughput contains `validatedExerciseCount`, then `elapsedMicroseconds`, rather than a floating
value. Each diagnostic entry contains `ordinal`, nullable `elapsedMicroseconds`, nullable
`observedBundlePath`, and nullable `artifactManifestSha256` in that order. An observed path may be
retained for corrupt or mismatched evidence but is never presented as trusted child outcome.
`elapsedMicroseconds` measures admitted Maneuver scheduling through aggregate construction; report
write/readback timing is emitted only in the post-readback command trace to avoid self-rewrite.
Durations, throughput, local paths, staging/final GUIDs, machine/runtime identity, and
artifact-manifest hashes are excluded from `reportFingerprint`.

Overall status precedence is aggregation failure, cancellation, ordinary Exercise failure, then
success. A valid cancelled child is a validated Exercise failure, selects overall `cancelled`, and
marks later entries `not-run` with reason `cancelled`. Aggregation stop marks later entries
`not-run` with reason `aggregation-stopped`. Invalid Maneuver admission creates no child and no
completed Maneuver report. Report-finalization failure creates no completed-report claim; already
finalized child bundles remain available. Cancellation observed after admission but before the next
child starts also writes a cancelled report with that child and its tail marked not-run; it does not
synthesize a cancelled Exercise bundle.

The Maneuver command uses stable process exits: `0` success, `2` manifest/usage invalid, `13` one
or more ordinary Exercise failures, `14` aggregate evidence failure, `11` report artifact failure,
`12` unexpected command failure, and `130` cancellation. Attributable child failure categories stay
lossless in the report rather than being collapsed into more process exit codes. On completion the
CLI prints `exerciseBundle[N]=<path>` in ordinal order for each identity-matched finalized child,
then `report=<path>` and `reportFingerprint=<sha256>`. Failure detail goes to stderr; it never
prints a report or bundle path that did not pass readback.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `EXR-001` | One Exercise is one deterministic, bounded simulation from fresh Core creation inputs to one declared successful domain terminal outcome or one unconditional failure. One Maneuver is an ordered collection of Exercise specifications. |
| `EXR-002` | Core exposes a dedicated Exercise-only session in a clearly isolated public namespace. The session is minted only from admitted creation inputs, is non-convertible to ordinary authority handles, and exposes no general snapshot import/export or resume capability. |
| `EXR-003` | Ordinary campaign creation and Exercise start share one internal creation primitive. Ordinary action submission and Exercise action submission share one internal membership-validation, command-mapping, adjudication, exact-one-event v1 validation, and projection primitive. The primitive checks event cardinality and never truncates an unchecked collection. No runner code reimplements rules or mutates authority. |
| `EXR-004` | Exercise query uses the existing legal-action generation semantics. Submission identifies the exact current audience and action ID; Core re-derives current membership before executing it. Rejected probes create no event, no successor session, and no random/state change. |
| `EXR-005` | An accepted Exercise step returns a successor Exercise session and defensive-copy canonical evidence sufficient to retain the accepted action identity, emitted event bytes, and resulting checkpoint. No runner-facing type exposes mutable Core authority. |
| `EXR-006` | The checked-in first Exercise uses an admitted rules-laboratory setup and succeeds only on exact boundary `land.position.operation-1.organization`. Future full-game success uses a distinct `VictoryReached` outcome only after Core implements victory. |
| `EXR-007` | Successful domain outcomes are a closed type containing only `BoundaryReached(positionId)` and the future `VictoryReached(victor)` shape. Invalid manifests, illegal/no actions, invariant failures, replay mismatches, step limits, cancellation/interruption, artifact failures, controller failures, and unexpected exceptions are closed failure categories and always fail the Exercise and process. |
| `EXR-008` | The manifest cannot declare any failure category as expected success. Negative testing asserts an exact failure category while the containing Exercise remains failed and the command remains nonzero. |
| `EXR-009` | Every completed successful Exercise proves history reconstruction by replaying its canonical events inside Core and comparing the reconstructed final canonical snapshot bytes with the recorded final snapshot bytes. |
| `EXR-010` | Every completed successful Exercise proves re-adjudication by starting a second fresh Exercise session with identical creation inputs and seed ledger, then submitting the recorded accepted audience/action IDs. Canonical accepted-action transcript, events, and final snapshot bytes must all match. |
| `EXR-011` | Canonical evidence uses explicit versioned `Utf8JsonWriter` codecs, fixed property ordering, ordinal collection ordering, UTF-8 without BOM, and LF framing. JSONL files contain one canonical JSON value followed by one LF per record. Canonical bytes contain no timestamps, elapsed time, local paths, machine identity, stack traces, or locale-sensitive values. |
| `EXR-012` | A successful trusted v1 bundle contains the admitted normalized manifest, build identity, run result, accepted actions, canonical events, step evidence, initial/final snapshots, both replay proofs, invariant/check results, seed ledger, machine and Markdown summaries, optional diagnostics, and an `artifact-manifest.json` written last. A failed-bundle profile requires the failed run result, checks, and every stage artifact safely available; the normalized manifest exists only after admission and execution/replay artifacts only after those stages. |
| `EXR-013` | Bundle writing is path-confined and rejects absolute paths, traversal, symlink escapes, and duplicate normalized paths. It stages on the destination volume under `.partial`, durably flushes payloads and `run-result.json` before writing/flushing the manifest last, then moves to `succeeded` or `failed`. `run-result.status` is the sole bundle-state authority. A reader trusts only final-directory bundles whose status, manifest profile, schemas, sizes, and hashes all validate. Rename alone is not proof of success. |
| `EXR-014` | Interrupted/incomplete `.partial` bundles are never reported as completed and are not deleted automatically in v1. A failed final bundle is retained when safe finalization succeeds. Failure before staging, or an artifact fault that prevents safe finalization, returns nonzero plus explicit stderr and makes no completed-bundle claim. |
| `EXR-015` | Confidentiality and detail are separate closed axes. V1 supports `trusted-authority` confidentiality with `compact`, `forensic`, or `debug` diagnostics. Classification is derived across every file, error, report, manifest, filename, count, and hash; callers cannot downgrade it. |
| `EXR-016` | Side-safe export is deferred until Axis and Commonwealth bundles are physically independent, have independent manifests, and pass whole-tree opponent-change noninterference tests. V1 exposes no flag whose name implies public or side-safe trusted bundles. |
| `EXR-017` | Seed derivation contract `sandtable.exercise-seeds.v1` hashes fixed canonical material in this order: `contractVersion`, `schemeId`, numeric unsigned-64 `rootSeed`, `maneuverId`, nonnegative `exerciseOrdinal`, nullable `pairKey`, `domain`, and nullable `role`; the derived seed is the first eight digest bytes interpreted unsigned big-endian. Standalone runs use canonical Maneuver ID `standalone.<exercise-id>` and ordinal 0. In a Maneuver, its root seed is authoritative and child entries cannot override it. Paired variants share pair key and pair-local exercise ordinal. Every derivation is recorded in a seed ledger. |
| `EXR-018` | Campaign ID is deterministically derived by versioned canonical SHA-256 material from Maneuver/standalone ID, pair key, and pair-local exercise ordinal, excluding variant/controller identity. Paired variants therefore use byte-identical creation inputs and identical initial role/domain streams. Reports state that trajectories and random consumption may diverge and make no causal, balance, significance, or synchronized-stream claim. |
| `EXR-019` | A baseline-eligible run requires empty `git status --porcelain=v1 -z --untracked-files=all`, resolved HEAD commit/tree identity, executed assembly hashes, ruleset/configuration hashes, runtime identity, manifest hash, and seed ledger. Detached HEAD is valid when commit/tree resolve exactly. If any required identity cannot be captured or verified, a requested baseline run fails before simulation. |
| `EXR-020` | Dirty worktrees may run only as explicit exploratory runs. They record HEAD, dirty status, a hash of the raw NUL-delimited porcelain bytes, and executed assembly hashes, and are labeled nonbaseline and nonreproducible. The harness never automatically captures patch content or untracked files. |
| `EXR-021` | V1 Maneuvers execute serially in manifest order. Aggregate reports derive only from validated per-Exercise bundles, separate deterministic counts/outcomes from nondeterministic duration data, and preserve individual failure categories. |
| `EXR-022` | Structured diagnostics correlate Maneuver ID, Exercise ID, variant, step ordinal, campaign/state/position, audience, action ID, and check name. Secret-bearing authority values are not written to console by default. Changing diagnostic detail changes its normalized manifest/build/artifact identity by design, but cannot alter action selection or the simulation-evidence subset: accepted actions, events, step evidence, snapshots, seeds, checks, and replay proofs. |
| `EXR-023` | Manifests, results, evidence, seeds, proofs, and artifact manifests carry explicit contract/scheme versions. Readers reject unknown versions, missing/duplicate/extra required properties, invalid enum values, noncanonical paths, hash mismatches, and contradictory status/outcome combinations. |
| `EXR-024` | The checked-in fixture and all examples are repository-synthetic, deterministic, bounded by an explicit maximum step count, and contain no remote dependency. No unspecified legal-action selection is permitted: a controller policy must select a unique current action or fail. |
| `EXR-025` | At each nonterminal step the runner queries `system`, `axis`, and `commonwealth` in that fixed order, requires all queries to succeed and exactly one audience to have a nonempty set, then invokes only that audience's controller. Zero active audiences is `NoUniqueLegalAction`; multiple active audiences is `InvariantFailed`. Simultaneous-action scheduling is unsupported in v1. |
| `EXR-026` | Check contract `sandtable.exercise-checks.v1` is an ordered closed catalog: authority-query validity, active-audience cardinality, selected-action membership, accepted-event cardinality, checkpoint continuity, exact terminal boundary, history reconstruction, and re-adjudication. Each result records contract version, check ID, scope/step, and pass/fail; required failures cannot be waived. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `EXR-NFR-001` | Repeating a clean baseline Exercise with the same executable identities, manifest, and root seed produces byte-identical canonical evidence and proof files. Diagnostics and wall-clock timing are excluded from that equality. |
| `EXR-NFR-002` | The runner propagates cancellation, returns stable nonzero exits for all failure categories, and retains the most complete safely finalized failure evidence available. |
| `EXR-NFR-003` | Core capability/type-graph tests prove the Exercise session cannot expose or convert to ordinary authority, mutable snapshots, commands, contexts, random state, or unrestricted replay APIs. |
| `EXR-NFR-004` | New serializers have golden-byte, strict-reader, culture, collection-order, and round-trip tests. Artifact tests cover traversal, symlinks where supported, corruption, partial bundles, failure finalization, and manifest-last validation. |
| `EXR-NFR-005` | New code passes repository analyzers, formatting, Release build, and the complete Microsoft.Testing.Platform suite with no warnings or skipped acceptance tests. |

## Contract and compatibility policy

Exercise contracts begin at version 1 and do not change existing campaign event, snapshot, action,
observation, or ruleset versions merely to expose evidence. The implementation extends existing
internal canonical writers or adds dedicated wrapper codecs without changing their established
bytes. If sharing the creation/action primitive reveals an unavoidable existing-contract change,
implementation stops for a reviewed spec amendment before changing bytes or versions.

Within Exercise v1, adding a required property, changing canonical order/framing, seed material,
path semantics, terminal classification, or equality definition requires a new contract or scheme
version and golden fixtures. Readers reject unknown future versions rather than guessing.

## Project structure

```text
src/Cna.Core/
  Exercises/                  # capability, trusted evidence, Core replay proofs
src/Cna.ExerciseRunner/
  Commands/                   # CLI parsing and exit mapping
  Controllers/                # deterministic action selection policies
  Execution/                  # Exercise/Maneuver orchestration
  Artifacts/                  # transactional writer/reader and reports
tests/Cna.Core.Tests/
  Exercises/                  # authority parity, capability, evidence, replay
tests/Cna.ExerciseRunner.Tests/
  Commands/ Execution/ Artifacts/
scenarios/exercises/          # checked-in single-run manifests
scenarios/maneuvers/          # checked-in batch/paired manifests
artifacts/exercises/          # ignored generated output
```

The exact file split follows the small delivery tasks in the technical design. New projects are
added to `Sandtable.slnx`; the runner is not added to AppHost and no production service references it.

## Code-style exemplar

Closed results make success and failure impossible to conflate:

```csharp
public abstract record ExerciseCompletion;

public sealed record ExerciseSucceeded(ExerciseTerminalOutcome Outcome) : ExerciseCompletion;

public sealed record ExerciseFailed(ExerciseFailure Failure) : ExerciseCompletion;
```

Concrete terminal and failure types are closed, validated values; they are not caller-provided
strings or a boolean plus optional reason. Production names may vary, but the type separation and
exhaustive exit mapping are required.

## Testing strategy

- Write focused failing tests before each behavior change.
- Prove ordinary creation/submission and Exercise creation/submission produce the same canonical
  event and successor checkpoint through one shared internal primitive.
- Use seeded RNG, fake diagnostic clocks where needed, temporary directories, and in-process Core.
- Use golden canonical bytes and corrupt-one-field tables for strict contracts.
- Test history reconstruction and re-adjudication independently; neither proof substitutes for the
  other.
- Test failure categories as failures, including negative-test manifests.
- Test artifact crash states through writer failpoints around payload, status, manifest, flush, and
  move boundaries; readers must reject every incomplete/corrupt state.
- Test repeated clean runs after normalizing only explicitly diagnostic files.
- Defer side-safe claims until whole-bundle noninterference tests vary opponent-only facts and compare
  the entire independent export tree.

Exact project commands after implementation:

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build
dotnet test --project tests/Cna.ExerciseRunner.Tests/Cna.ExerciseRunner.Tests.csproj --no-build
just check
```

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `EXR-AC-001` | Run the checked-in Organization-boundary Exercise twice from a clean identical build into separate artifact roots | Both succeed at the exact boundary; canonical evidence, seeds, and proofs are byte-identical. Artifact-manifest entries for canonical files match; diagnostic entries and durations may differ. |
| `EXR-AC-002` | Compare ordinary and Exercise creation plus every accepted action in the fixture | Canonical event bytes and successor checkpoints match at each step, proving the shared primitives have no semantic fork. |
| `EXR-AC-003` | Reflect and compile against the Exercise capability | It cannot be created except from fresh admitted inputs, converted to/from an authority handle, serialized, resumed, or used to expose arbitrary production authority. |
| `EXR-AC-004` | Complete one Exercise and independently run both replay proofs | Reconstruction final snapshot matches exactly; re-adjudication transcript, events, and final snapshot match exactly. Each proof reports its own contract and checks. |
| `EXR-AC-005` | Trigger each failure family, including an exact expected negative-test category | Result remains failed, bundle status is failed when safely finalizable, and process exit is nonzero. An expected exact category affects the assertion report only. |
| `EXR-AC-006` | Interrupt/fault the writer at each lifecycle boundary and corrupt each finalized artifact class | No partial/corrupt bundle is accepted as complete; valid failed bundles remain inspectable; manifest-last/hash validation detects corruption; faults that prevent staging/finalization return nonzero and explicitly claim no completed bundle. |
| `EXR-AC-007` | Attempt traversal, absolute, duplicate-normalized, and symlink-escape paths | Writer rejects before escape and writes nothing outside its unique staging root. |
| `EXR-AC-008` | Request baseline from clean attached/detached HEAD, dirty, unavailable-Git, and identity-mismatch states | Both clean resolvable HEAD forms start as baseline; dirty explicit exploration is labeled nonbaseline/nonreproducible; unavailable/mismatched identity fails closed. |
| `EXR-AC-009` | Run a serial Maneuver with success and failure fixtures | Execution order and deterministic aggregate counts are stable; one failure cannot be averaged away or relabeled success. |
| `EXR-AC-010` | Run paired variants with the same root seed | Derived campaign IDs, complete creation inputs, and initial per-role/domain seeds match; ledgers show domain separation and reports include the required post-divergence limitation. |
| `EXR-AC-011` | Enable compact, forensic, and debug detail for otherwise identical simulation inputs | Action selection and the simulation-evidence subset—accepted actions, events, step evidence, snapshots, seeds, checks, and replay proofs—remain byte-identical. The normalized manifest, build identity, diagnostics, summaries, and artifact manifest may differ because detail is an explicit identity-bearing input; all outputs remain trusted-authority. |
| `EXR-AC-012` | Feed unknown versions, extra/missing/duplicate properties, invalid paths/enums, contradictory outcomes, and hash mismatches to readers | Every invalid contract is rejected deterministically with no partial trusted result. |
| `EXR-AC-013` | Inspect checked-in docs, roadmap, project map, and terminology after implementation | `README.md`, roadmap, `tech-design.md`, `naming-overview.md`, solution/project map, commands, contracts, and implemented milestone agree. |
| `EXR-AC-014` | Query all three audiences at each current nonterminal checkpoint, plus test-only zero-active and multi-active fixtures | Query order is fixed; exactly one active audience selects; zero fails `NoUniqueLegalAction`; multiple fails the named invariant; no priority silently resolves simultaneous actions. |
| `EXR-AC-015` | Exercise every check in `sandtable.exercise-checks.v1` and vary caller/culture/order inputs | Check order, IDs, scope, bytes, and failure mapping match goldens; every required failed check fails the Exercise and cannot be waived. |
| `EXR-AC-016` | Run the checked controller-policy matrix across `act-first`/`act-last` and Reserve `none`/`one`/`all` | All six children select only current legal actions, reach first-side Movement in exactly 10/11/12 accepted actions by Reserve policy, retain zero/one/two Reserve-I designations, reconstruct and re-adjudicate exactly, and aggregate through strict readback. |

## Delivery boundaries

### Always

- Preserve Core as sole authority and reuse shared internal creation/action primitives.
- Keep canonical evidence and diagnostic telemetry separate.
- Fail closed for baseline identity, artifacts, replay, invariants, and unsupported contracts.
- Record every deferred claim honestly and keep trusted output visibly classified.
- Update tests and the three repository-wide design documents with implementation changes.

### Ask before changing

- Existing campaign/event/snapshot/action/observation canonical bytes or version numbers.
- The exact terminal boundary or addition of victory semantics.
- Any public network/service adapter, persistent database, model-backed controller, or CI gate.
- Enabling side-safe/public exports or automated patch/source capture.
- Parallel/distributed Maneuvers or statistical/causal claims.

### Never in v1

- Accept an ordinary authority handle, arbitrary snapshot, or persisted production campaign as an
  Exercise start.
- Reimplement adjudication, legal-action generation, RNG, projection, or validation in the runner.
- Send hidden opposing state to Intelligence or claim trusted bundles are side-safe.
- Turn an expected defect/failure into process success.
- Include timestamps, durations, local paths, or machine identity in canonical evidence.
- Trust a bundle based only on directory name or rename success.

## Explicit non-goals

- Full campaign completion, victory evaluation, balance conclusions, or performance thresholds.
- Model/LLM controllers, remote providers, asynchronous decisions, or fallback evaluation.
- Orleans grains, AppHost orchestration, distributed or parallel execution.
- SQLite/ReplayDB, dashboards, Maproom visualization, artifact upload, or permanent archives.
- Mid-campaign resume, production replay activation, snapshot import, or mutation debugging APIs.
- Public/Axis/Commonwealth bundle export in the first runnable increment.
- Causal inference, significance testing, paired-stream synchronization after divergent choices.

## Success criteria

`EXERCISE-001` is complete only when all `EXR-AC-*` scenarios pass, both replay proofs are retained,
artifact failure modes fail closed, clean reproducibility is demonstrated, the checked-in Exercise
and Maneuver are runnable with the documented commands, the full repository gate passes, and the
repository-wide documents describe implemented—not planned—behavior accurately.

## Current Task 014 evidence

Task 014 feature implementation is complete and its focused evidence is green: warning-free
solution build; 10/10 command/fixture tests; 41/41 semantic bundle-validation tests; 23/23
serial-executor tests; 17/17 report contract/transaction tests; and 252/252 tests in the complete
ExerciseRunner project.
This verifies serial aggregation, explicit identity and diagnostics propagation, one-read semantic
validation, report finalization/readback, the checked CLI fixture, cancellation, and failure
precedence within the runner scope. The complete 562-test solution gate and `just check` pass; two
checked-fixture runs retained the identical deterministic report fingerprint
`sha256:8cc5d2fbfb907f83edc7bb51a7ec98eb57f7338c072a0325ff5ca4a685b19f06`; and the pre-PR
implementation review verdict is Ready.

Post-adoption trusted-evidence hardening also proves that initial and final snapshots are decoded by
Core's complete current snapshot/world contract before any executed-or-later success or failure
profile is trusted. Rehashed `world:{}` and `world:{"contractVersion":2}` cases reject through the
public reader, including a failed-reconstruction bundle with internally consistent dependent hashes
and proof fields.

## Open questions

There are no architecture-blocking open questions. Exact public type and file names within the
provisional `Cna.Core.Exercises` namespace may be refined during test-first implementation without
changing capability boundaries, contracts, or acceptance criteria. Any material change requires a
spec amendment and review before implementation.
