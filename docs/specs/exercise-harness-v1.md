# Exercise Harness v1 Specification

**Status:** Partially implemented; the single-Exercise CLI and trusted bundle path are implemented, while Maneuvers and pairing remain pending

**Date:** 2026-08-20

**Roadmap capability:** `EXERCISE-001`

**Rules target:** `cna-1979.1`

**Predecessors:** [Legal Actions v1](legal-actions-v1.md),
[Weather Determination v1](weather-determination-v1.md)

**Technical design:** [Exercise Harness v1](../design/exercise-harness-v1.md)

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
as ordinary campaign play, and retains canonical evidence plus separate diagnostics. It stops at the
first currently implemented terminal boundary,
`land.position.operation-1.organization`; it does not imply a full victory-capable game.

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
  exercise run --manifest scenarios/exercises/rules-lab.organization.v1.json \
  --artifact-root artifacts/exercises
```

That fixture is explicitly exploratory. A second checked fixture requests the same bounded run as
a clean baseline and therefore fails closed unless the checkout has no tracked or untracked changes:

```text
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.baseline.v1.json \
  --artifact-root artifacts/exercises
```

The manifest's `detail` value is `compact`, `forensic`, or `debug`. Compact retains accepted-step
and completion records. Forensic adds deterministic query candidate counts, controller selection,
ordered checks, proof results/hashes, payload sizing, and progressively assembled context for every
failed in-execution decision. Debug is a strict superset with
noncanonical monotonic operation/phase timings; after manifest-last write and mandatory reader
validation it also prints a structured `trace=` record for artifact finalization/readback timing.
Failed debug runs retain every timing measured before the failure.
No detail tier changes the simulation-evidence subset named by `EXR-022`.

The planned serial-Maneuver command is not implemented yet:

```text
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuvers run --manifest scenarios/maneuvers/rules-lab.paired.v1.json \
  --artifact-root artifacts/exercises
```

Unknown commands/options, missing values, invalid paths, invalid manifests, and unsupported contract
versions fail before campaign creation with a nonzero exit. CLI spelling may change only before its
golden contract test is accepted; afterward it is versioned user-visible behavior.

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

## Open questions

There are no architecture-blocking open questions. Exact public type and file names within the
provisional `Cna.Core.Exercises` namespace may be refined during test-first implementation without
changing capability boundaries, contracts, or acceptance criteria. Any material change requires a
spec amendment and review before implementation.
