# Combat exercise child evidence v1 — B2

Status: UNVERIFIED day-stop checkpoint. Final focused verification interrupted at user request;
retained fixture predates final oracle/schema and must be regenerated before acceptance.
Private, unregistered, checkpoint-scoped contract evidence. No runtime registration,
historical wire-label change, public export, actual artifact publication, or full fresh-session
parity claim. B1 remains the authority for the three admitted execution scopes and 134 sources.

## Source and prospective manifest

The closed schema defines Manifest, Preferences, trusted Clock schedule, failure-injection Fault,
AcceptedStep, FailedAttempt, Check, Result, Proof, Artifact inventory and Child. Exact ASCII JSON
property order follows descriptor order. Unknown/missing/duplicate/reordered fields, trailing data,
float/bool integers and bounds violations reject. Limits: 32 MiB, depth 48, 1,024 array items and
B1's 64 accepted transitions. Full native strings are authenticated against closed B1 source/native
readers, not trusted because a caller supplies a matching digest. Existing source artifacts are
pinned unchanged. Every public child admission validates pins and exact source before using data.

Manifest binds child identity, native setup/content IDs and hashes, source scenario, explicit oracle-source-pins build identity, initial RNG
seed-state hash, B1 request (rules/configuration/source-prefix/full-reference/start/checkpoint,
terminal/schedule/limit), private confidentiality, exact trusted actor/time/availability schedule,
controller preferences, evidence scope and optional expected failure. Build and seed kinds explicitly identify oracle dependency pins and initial RNG state, not a runtime executable or a claimed original campaign root seed.
Build identity hashes a closed ordered descriptor containing current B2 oracle/schema byte hashes and sorted B1 dependency pins. Fixture and Child bytes are excluded; reading current code hashes introduces no self-referential embedded digest. The root may be explicitly synthetic. Historical terminal profile retains full native prefix and
executes zero transitions. It cannot acquire a controller merely from a matching position.

The fixture-time mapping selects a preference profile before execution. The controller receives
only current audience observation and that manifest-bound record. It chooses a unique offered
candidate: select Close Assault, decline retreat before assault, full Close Assault assignment,
configured retreat/refusal, configured guard/escape, inherited release-I and configured repeat/finish.
Guard maps to actual `relocate-and-guard`; escape maps to `leave-unguarded`. It preserves the offered
payload and action ID. Missing or ambiguous membership fails. Controller cannot inspect source name,
private World/RNG, later inputs, future events or a desired arithmetic branch. Trusted System script
is separately bound by full source reference and schedule. Manifest admission binds the supported preference record and role order for its exact retained reference. Other well-typed configurations fail before execution, with no steps/checks/proofs; they are not mislabeled membership failures. Equal-prefix guard/escape or opposite-seal-order references may still pair, each with its own supported configuration. Both actual dual-slot role orders are
supported; all other slots preserve native single-owner/System semantics.

## Fresh execution and semantic actions

Execution starts by authenticating complete B1 source and reconstructing its declared starting
state. Native modules receive freshly generated owner commands or the bound trusted System script.
Every transition runs the real native `transition` function on local state. Native Steps events omit an author field; evidence retains null instead of conflating input actor with event author. Fragment seams use
native initializers and the accepted exact predecessor/base; generated event and state must match
the admitted supported source cut. No arbitrary extension beyond B1 is admitted.

Public projections are regenerated from the accumulated fresh states, inputs and events. The
oracle invokes the pinned pure projection function bodies with explicit local frame suppliers;
accepted-side replay/projection caches and future events do not supply those views. Function globals
are copied locally; no shared module state is patched. Controller preference and schedule are
recomputed at each cut. Native sources remain the only authority for choices and arithmetic.

AcceptedStep means one published authority event, with contiguous zero-based ordinal, actual
initiator, public schedule, exact trusted input, optional side proposal/outcome, semantic action,
native authority family, optional event author, native transition's receipt ID, canonical event and successor
full-checkpoint hash. Native receipt return values are identifiers; the complete native event and
checkpoint control retain their source bindings and receipt records. Exactly one event advances the
ordinal. Retry/no-op emits no event and adds no step. Invalid attempts cannot create accepted steps. FailedAttempt records only the actual actor/time/availability supplied to public admission, copied into trustedClock, plus the rejected proposal/outcome. Membership rejection submits no native command and emits no native event; an unsubmitted translated command is not evidence.

An accepted public proposal uses its exact audience/action ID as semantic action. An owner proposal
whose trusted clock fails may still publish a System cancellation: retain actual owner input,
rejected outward outcome/null public receipt, native receipt and System event. Its semantic audience
is System. Do not replace owner input with a fabricated System request.

System identity is the new private domain over the closed actual key:
`authorityFamily, scopeId, decisionId, effectKind, reason, disposition`. Result2 disposition-recorded/custody-settled map disposition from actual payload.kind; Reserve unit-disposition maps effect.choice. Other admitted tags retain null. Optional absent values are
explicit null. In particular, a null command decision ID remains null; a release/control ID is never
substituted for it. Raw time, prefix, receipt, audit maximum, rejected proposal ID and initiator are
not direct seed fields. Equal actual keys must yield equal IDs; native decision identities may vary
indirectly between lineages. Event authorship alone does not determine accepted controller intent.

Four native A3a later-complete-intent ledger witnesses separately prove accepted owner completion
with a System-authored owner-complete-release event, public receipt and idempotent retry. They are
isolated classifier evidence, excluded from full-World registry, child counts and terminal proofs.

## Proofs, checks and failure matrix

Transcript and event hashes use distinct new domains. Framing is ASCII domain, NUL, then for each
record a big-endian signed Int32 byte length followed by exact record bytes. It is neither JSONL/LF
nor cycle U64 framing. Empty streams retain their actual domain-separated framed-empty identity only
when a proof exists. Absent proof is null, never an invented empty hash. Failed verification retains an explicit unverified attempted proof with failure, attempted transcript hash and nullable observed transcript/final-checkpoint hashes. Unavailable observations remain null.

Reconstruction authenticates complete source prefix and separately replays recorded execution input
and event bytes through native transitions, comparing receipt IDs, every native successor and full
checkpoint hash. Re-adjudication restarts native execution, regenerates public views/controller
choices/schedule and compares complete accepted steps and final checkpoint. Matching claimed hashes
alone cannot validate a child. `validate_child` also requires exact expected manifest when supplied,
recomputes fixed artifact inventory and reruns complete semantics.

Seven ordered passed checks accompany every accepted step: System query, Axis query, Commonwealth
query, eligible cardinality, exact membership, one-event cardinality and continuity. Ordered run
checks are terminal, reconstruction, re-adjudication. Unknown, duplicate, skipped, reordered and
post-failure checks reject through exact semantic regeneration.

| Outcome | Evidence |
| --- | --- |
| Admission rejected | No execution/checkpoint/checks/proofs |
| Execution rejected | Accepted prefix, optional failed attempt, failed membership check, failed terminal; no proofs |
| Cancellation or step limit | Accepted prefix and failed terminal; no failed-attempt checks/proofs |
| Reconstruction failure | Passed terminal, failed reconstruction and unverified reconstruction proof; no re-adjudication proof |
| Re-adjudication failure | Passed terminal/reconstruction and verified reconstruction proof; failed re-adjudication and unverified attempted proof |
| Success | Passed terminal and both independently verified proof records |

Fault controls are explicit bounded oracle test injections: cancellation at a cut, invalid offered
action ID, corrupt reconstruction event bytes, or re-adjudication mismatch. Run-level none/reconstruction/re-adjudication require index zero. Step faults must target a retained transition strictly before the cap; invalid-controller fault also requires an actual owner offer. Historical zero-execution sources cannot admit step faults. They do not change native
gameplay or fabricate successful transitions. Expected failure match is a separate boolean; every
failure stays failed. Eight admitted A2 fallback prefixes execute their real fallback, stop at exact
16/20 transition limits and remain failed with unsatisfied closed ReserveRelease1 terminal. An incomplete reference rejects a maximum larger than its retained suffix before execution; exhausting retained history is not exhausting a larger configured limit. A completed reference may admit larger bounds and stop at its already satisfied terminal.

## Artifact inventory and API

Distinct `ArtifactManifest` envelope lists exact fixed path, closed scheme, byte count and SHA-256 for manifest,
result, framed checks/steps and present source/checkpoint/attempt/proof records. Its `run-manifest.json` entry is the prospective run configuration. The publication inventory itself (`artifact-manifest.json`) is excluded, so no self-hash cycle exists. Manifest-last, atomic filesystem publication is prospective and belongs to later runtime
work; this packet creates no published child directories. Diagnostic wall time/log path is outside
deterministic Child bytes.

Primary APIs: `manifest_for(source)`, `admit_manifest(manifest, source)`,
`run(manifest, source)`, `validate_child(bytes, source, expected_manifest=None)`, `child_hash(child)`.
`execute` returns fresh local execution; `reconstruct` independently checks recorded transitions.
`controller(view, preferences)` accepts no private source argument. `system_key` and
`semantic_action` expose the closed classifier; `completion_witnesses` is ledger-only evidence.

Run `python3 -B docs/specs/verify-combat-exercise-child-evidence-v1.py`. Programmatic groups cover
all admitted sources, failure/proof/raw mutations, public controller/System identity invariants and
four native completion witnesses. Fixture stores authenticated source/reference/retained-event summaries for all 134 sources, plus twelve freshly generated canonical child representatives covering success, fallback, inherited, historical and injected failures. Reference summaries are not child-execution or proof claims. Programmatic execution still covers all 134 sources; ordinary checking compares exact retained bytes. B3 consumes only validated children.
