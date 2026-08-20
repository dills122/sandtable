# Exercise Evidence And Artifact Protocol Spike

**Status:** Complete; decisions adopted by the proposed specification

**Date:** 2026-08-19

**Decision owner:** Sandtable project owner

**Governing specification:** [Exercise Harness v1](../specs/exercise-harness-v1.md)

**Delivery plan:** [Exercise Harness v1 technical design](../design/exercise-harness-v1.md)

## Decision question

What canonical evidence, confidentiality model, and transactional local artifact protocol make an
Exercise bundle deterministic, reconstructible, fog-safe at later side-export milestones, and
unambiguously successful or failed after interruption?

## Adopted decisions

| ID | Decision |
| --- | --- |
| `DEC-002` | The first runnable bundle is trusted developer instrumentation only; its confidentiality is `trusted-authority`. |
| `DEC-003` | Canonical Exercise evidence extends the repository's explicit ordered `Utf8JsonWriter` and golden-byte style. |
| `DEC-004` | Versioned files remain the source of truth; databases and indexes are deferred derived views. |
| `DEC-007` | Canonical JSON/JSONL framing, ordering, and strict readers are explicit contracts rather than an added generic canonicalization dependency. |
| `DEC-008` | Directory move is final placement, not proof of transaction success; readers require final class, run result, manifest-last presence, schemas, sizes, and hashes. |
| `DEC-013` | V1 retains the complete canonical event/action transcript plus initial and final snapshots before any compaction is considered. |
| `DEC-014` | Future side-safe bundles are physically independent exports with independent manifests and whole-tree noninterference evidence. |

## Why this matters now

The prior plan ordered reporting and privacy outputs before it proved crash-safe finalization.
It also mixed confidentiality with diagnostic verbosity and risked exposing trusted hashes,
filenames, counts, errors, or reproduction metadata through side-specific bundles.

## Authorized scope and prohibited actions

In scope:

- existing Sandtable explicit canonical serializers and privacy tests;
- local filesystem artifact protocols supported by the repository's .NET toolchain;
- Reef report/artifact patterns and Liar's Dice JSONL/report observations;
- contract, failure, path-confinement, classification, and retention decisions.

Out of scope:

- implementing a writer;
- cloud/object storage, databases, telemetry backends, or CI mutation;
- claiming public/side safety from trusted-only instrumentation;
- modifying either reference repository.

## Source hierarchy

1. Current Sandtable serializers, observation contracts/tests, and repository guidance.
2. Official .NET platform documentation for filesystem operations when local code cannot settle
   semantics.
3. Reef and Liar's Dice as labeled implementation observations.

## Decision criteria

- versioned byte-stable canonical framing;
- explicit separation of canonical evidence, diagnostics, and derived analysis;
- separate confidentiality and detail axes with fail-closed derivation;
- path confinement and no manifest self-hash recursion;
- partial runs cannot resemble success;
- all derived payloads are written before manifest-last finalization;
- failed bundles remain reproducible without weakening exit status;
- retention cannot remove replay inputs before both replay proofs pass.

## Stop condition

Stop when artifact directory states, required files, canonical framing, manifest/hash rules,
finalization order, failure/exit semantics, classification propagation, side-manifest constraints,
retention gates, and acceptance/fault cases are explicit enough for implementation tasks.

## Evidence

### Repository facts

- Campaign events, snapshots, observations, legal-action sets, and receipts already use explicit
  `Utf8JsonWriter` codecs with fixed property order and golden-byte tests. The harness can frame
  these canonical payloads without reserializing their internals through ambient JSON settings.
- Observation privacy tests prove type-graph exclusion, canary absence, and opponent-only
  noninterference for one observation payload. They do not cover filenames, manifests, errors,
  diagnostics, Markdown, CSV, or reproduction metadata.
- Trusted snapshot/event bytes contain hidden opposing state, exact content/setup facts, and random
  state. They are never side-safe artifacts.

### External documented facts

- [.NET 10 `Directory.Move`](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.move?view=net-10.0)
  moves a directory, rejects an existing destination, and rejects cross-volume moves. The managed
  contract does not promise crash atomicity.
- [.NET 10 `FileStream.Flush(true)`](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0)
  flushes intermediate file buffers to disk. I/O failures remain observable exceptions.
- [.NET 10 `File.SetUnixFileMode`](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.setunixfilemode?view=net-10.0)
  can set owner-only modes on Unix and is explicitly unsupported on Windows.

### Reference-repository observations

- Reef keeps stable report fingerprints separate from latency and wall-clock diagnostics, hashes
  artifact files with normalized relative paths, and offers compact/full report shapes. The
  inspected files are clean relative to Reef's current branch, although the repository has many
  unrelated working-tree changes.
- Liar's Dice emits exact per-game JSON/CSV plus Markdown summaries and can retain JSONL engine
  traces. Its trace can contain all private hands, demonstrating why a Sandtable trace must be
  classified trusted rather than treated as a side-safe log. Its paired runner is modified in the
  inspected working tree, so it is an observation rather than committed precedent.

### Inferences

- Directory rename is useful for final placement but is not sufficient proof of transactionality.
  A reader must validate directory state, run status, manifest presence, and every payload hash.
- Writing a side manifest inside a shared trusted bundle still leaks trusted artifact names/counts
  and complicates access. Later side bundles should be physically independent export roots with
  independent manifests.
- A canary scan is necessary but insufficient; whole-bundle noninterference must compare complete
  side bundle bytes after controlled opponent-only changes.
- Retaining all canonical events, the accepted transcript, and initial/final snapshots is safer in
  v1 than introducing snapshot sampling/compaction before reconstruction is proven.

## Options and recommendation

### Evidence model

Keep three evidence classes and two independent policy axes:

1. **Canonical evidence:** manifest identity, accepted action transcript, raw canonical event
   JSONL, step coordinates, initial/final canonical snapshots, fixed check results, and the two
   replay proofs. It excludes clock, duration, filesystem path, machine, trace, and allocation
   facts.
2. **Diagnostics:** structured logs/traces, elapsed time, allocations, runtime/OS/machine facts, and
   diagnostic sampling decisions. Enabling diagnostics cannot change canonical bytes or consume
   authoritative randomness.
3. **Derived analysis:** summaries, CSV, Markdown, aggregates, performance distributions, and
   outlier reports. Each output records provenance and inherits the strictest confidentiality of
   every input.

Axes:

- `ConfidentialityScope`: `trusted-authority`, `axis`, `commonwealth`, and later genuinely
  cross-side `public` only when the domain defines such facts.
- `DetailLevel`: `compact`, `forensic`, `debug`.

`debug` is never permission to cross a confidentiality boundary.

### V1 trusted bundle

The first runnable increment produces only a trusted bundle:

```text
artifacts/exercises/
  .partial/<temporary-name>/
  succeeded/<run-directory-id>/
  failed/<run-directory-id>/
```

Payloads inside one finalized bundle:

```text
exercise-manifest.json
build-identity.json
run-result.json
accepted-actions.jsonl
canonical-events.jsonl
step-evidence.jsonl
initial-snapshot.json
final-snapshot.json
reconstruction-proof.json
readjudication-proof.json
check-results.json
seed-ledger.json
diagnostics.jsonl                 # optional, nondeterministic
summary.json
summary.md
artifact-manifest.json            # written last
```

- `canonical-events.jsonl` is each existing canonical event JSON object followed by one LF; no
  wrapper reserializes the event.
- `step-evidence.jsonl` maps step ordinal, audience, selected action ID, state versions, event
  sequence start/count, snapshot hashes, and accepted/rejected result. Rejected diagnostic probes
  have zero events and identical before/after checkpoint hashes.
- Initial/final snapshots are required in v1. Full per-step snapshots are optional trusted debug
  payloads, not canonical requirements.
- All files use hard-coded schema-owned relative names. User input never supplies an artifact
  relative path.
- A successful bundle requires every canonical file above. A failed bundle requires
  `run-result.json`, `check-results.json`, and every earlier artifact that was safely available;
  `exercise-manifest.json` exists only after manifest admission, and replay/snapshot files exist
  only after the relevant execution stage. The artifact manifest declares the applicable profile.
- If failure occurs before staging can be created, or the artifact fault prevents safe
  finalization, the only realizable result is a nonzero exit plus stderr diagnostics and no claimed
  completed bundle. “Retain a failed bundle” always means when safe finalization remains possible.

### Bundle state machine and commit protocol

1. Resolve the configured artifact root to an absolute path. Refuse path traversal, symlink escape,
   an existing final destination, or a destination outside the configured root.
2. Create a unique directory beneath `.partial` on the same volume as final destinations. The
   temporary name is operational metadata and never canonical evidence.
3. On Unix, request and verify owner-only directory/file modes where the platform supports them.
   On Windows, record inherited-permission assurance; local filesystem ACLs are defense in depth,
   not the proof of fog safety.
4. Write canonical payloads, replay/check results, diagnostics, and every derived report. Close
   each stream; use `Flush(true)` for the final payload and manifest writes where supported.
5. Write `run-result.json` as `succeeded` or `failed`. A negative test can record an expected
   failure code but cannot change `failed` to `succeeded`.
6. Enumerate payloads by normalized ordinal relative path. Write `artifact-manifest.json` last with
   schema/media type, confidentiality, detail, size, and lowercase SHA-256 for every payload. The
   manifest does not list or hash itself; no self-hash sidecar is required in v1.
7. Close and flush the manifest. Move the directory to a unique
   `succeeded/<run-directory-id>` only for a successful result, otherwise
   `failed/<run-directory-id>`. The local directory ID is absent from canonical evidence; source
   and destination share one parent volume.
8. Readers accept a bundle only when it is outside `.partial`, its directory class matches
   `run-result.status`, the manifest exists, all paths are normalized/confined, all sizes/hashes
   match, required schemas are supported, and both replay proofs have the required status for
   success.
9. Any interruption before complete validation leaves an invalid `.partial` or invalid final
   directory. It is never treated as a successful Exercise. Cleanup is explicit and recoverable;
   v1 does not automatically delete material evidence.

This is a validated commit protocol, not a claim that `Directory.Move` alone is crash-atomic on
every filesystem.

### Side-safe export milestone

Side export is a later implementation checkpoint:

- Axis and Commonwealth exports are generated independently from Core-produced observations,
  action sets, and receipts; they never filter trusted snapshots/events.
- Each side receives a physically separate bundle and manifest. It contains no trusted path,
  trusted hash, authoritative snapshot hash, hidden activity count, or cross-partition artifact
  index.
- Errors, reproduction metadata, diagnostics, Markdown, CSV, and filenames use allowlisted
  side-safe fields.
- Controlled opponent-only changes must leave the entire export tree byte-identical, including
  manifest and derived reports, unless the changed fact has a domain-visible consequence.
- Side bundles are evidence views, not sufficient inputs for authoritative reconstruction.

### Options rejected

| Option | Failure mode | Decision |
| --- | --- | --- |
| Write directly into final directories | Crash can resemble a completed run | Reject |
| Treat rename alone as atomic proof | Not guaranteed by the managed contract | Reject |
| Write manifest before Markdown/CSV | Later outputs are unhashed mutations | Reject |
| One shared manifest for trusted and side artifacts | Leaks names, counts, and hashes across scopes | Reject |
| Store every per-step snapshot by default | Premature volume/privacy cost | Defer |
| Compact events before replay verification | Can destroy reconstruction inputs | Reject |

## Limitations and unknowns

- Exact filesystem durability after sudden power loss varies by OS/filesystem. V1 guarantees
  fail-closed validation, not universal power-loss atomicity.
- Windows ACL verification needs platform-specific tests if trusted bundles are used on shared
  Windows hosts. The first implementation may record permission assurance and document the local
  trust boundary rather than add a package dependency.
- Symlink-race hardening beyond pre-write path resolution may require handle-relative APIs not
  currently planned. V1 artifact roots are local developer-controlled directories; writers still
  reject discovered links and never follow manifest-supplied paths.
- Retention compaction, compression, and object storage remain deferred until bundle volume is
  measured.

## Implementation consequences and next gate

The first runnable delivery must include the minimal writer transaction, basic interruption/write
failure tests, path confinement, manifest-last validation, and failed-bundle semantics. Exhaustive
corruption/fault injection can be a later hardening task, but reports may not precede the basic
writer.

The governing spec should make trusted-only v1 explicit. Side-safe export receives its own task and
acceptance gate after trusted replay evidence works. Production implementation should not begin
until the final independent plan review accepts these choices.
