# Combat exercise parent evidence v1 — B3

Accepted 2026-09-18: private, unregistered contract evidence. No runtime registration, artifact publication,
new source profile or gameplay authority. Uses accepted [B2 child evidence](combat-exercise-child-evidence-v1.md)
and [B1 occurrence evidence](combat-exercise-occurrence-v1.md) without changing their bytes.

## Materialization and child admission

ParentManifest binds an ordered list of ChildPlan records and their exact canonical materialized
B2 manifests, plus ordered baseline/candidate references. Child IDs and pair IDs are unique;
self-pairs and unknown references reject. At most 32 children and 16 pairs. ChildPlan permits only
child ID, admitted source ID, accepted-transition limit, fault record and expected-failure assertion.
Materialization obtains all native provenance, initial RNG, current pinned B2 build, source/reference,
terminal, public controller configuration and trusted schedule from accepted B2 `manifest_for`.
The source-selected controller and role order are not caller override fields. Materialized manifest
must match exact bytes retained by ParentManifest, including explicit failure inputs.

Each supplied child is parsed and authenticated by B2 `validate_child` against its exact expected
manifest and authenticated source before any child count, hash, result or action summary is derived.
Missing and invalid entries retain identity/validation only; every dependent field is null. Validated
failed children remain failed and count as failed even when their expected-failure assertion matches.
The four standalone Reserve completion witnesses are not children and cannot enter these counts.
Unknown payload keys reject. An absent expected key produces a missing entry; an explicitly
supplied null or other non-byte value is invalid.

The optional in-process validation cache is keyed by complete immutable child bytes, materialized
manifest bytes and complete authenticated source bytes. Current own/dependency source pins and full
source authentication run before cache lookup. Only successful B2 validations are cached; returned
records are defensive copies. A cached record never makes modified input authoritative.

## Pair equality and divergence

Pair equality binds exact initial checkpoint bytes, lineage/profile, root, source-prefix identity,
execution cut, evidence scope/privacy, native setup/content/scenario, B2 build, initial RNG identity,
occurrence, rules/configuration, requested terminal, transition bound, clock policy, fault and
expected-failure assertion. Prefix identity includes native history and capability family. Equal
World alone is insufficient. Catalog name, full future reference hash, controller preference and
future role/clock schedule are excluded from pair equality, while each remains authenticated by
its own expected child manifest and source. Historical zero-execution source history is not invented.

Both children must first validate and contain initial checkpoint evidence. Otherwise comparison is
unavailable with all derived comparison fields null. Exact binding mismatch also makes comparison
unavailable, retaining the first mismatched closed PairKey field as diagnostic metadata. Valid failed
children with present equal initial evidence may compare; failure does not imply invalid evidence.

Action streams retain contiguous actual AcceptedStep ordinals and B2 semantic audience/action ID.
This preserves System semantics for owner-triggered rejected clock fallback and owner semantics for
accepted intent. Trusted input differences alone do not imply action divergence. First divergence is
the lowest ordinal with unequal audience or action ID. If one stream ends after an identical prefix,
that ordinal equals the shared length and the missing arm is null. Identical streams have null
firstDivergence. No random-purpose alignment or causal interpretation is claimed after divergence.

Actual current equal-start/equal-terminal completed reference pairs have equal lengths. Unequal
16/20-transition fallback references cannot share a larger bound under B2 admission. The unequal
null-arm algorithm is tested on authenticated stream prefixes separately; this is not an invented
admissible unequal-length child pair. Pair fault and limit equality remain mandatory.

## Parent report and readback

Counts distinguish expected, validated, missing, invalid, succeeded and failed children; accepted
transitions sum only validated children. Pair counts distinguish compared/unavailable. Parent status
is succeeded only when every child validates and succeeds and every requested pair compares.
An unavailable pair or a validated failed child makes parent status failed. Comparison may still
be available in that failed parent. Terminal equality compares actual final position/occurrence/
closure; failure equality compares actual child failure category.

Parent fingerprint is SHA-256 over ASCII `sandtable.combat-exercise-parent.report.v1`, NUL, then
exact canonical Deterministic bytes. Initial evidence uses its separate declared domain. Diagnostics,
paths, elapsed time and process IDs are absent. Reordering declared children or pairs changes identity.
Strict readback recomputes all entries/counts/comparisons/fingerprint from the supplied authenticated
children and exact manifest. Rehashing forged summaries or divergence cannot pass.

Schema descriptors define exact property order and closed fields. Raw input rejects unknown/missing/
duplicate/reordered fields, non-ASCII bytes, floats or booleans used as integers, trailing data and
oversized/deep arrays or records. Limits: 8 MiB parent bytes, depth 52, 128 array items, 32 children,
16 pairs and signed step delta -64 through 64. B2 retains its own child limits/readers. Retained
fixture uses exact ASCII two-space JSON plus final newline; numeric or whitespace mutations reject.

## Verification

Focused oracle covers exact repeated identity, changed role order, equal-prefix guard/escape,
same-fault validated failures, real System fallback, missing/forged/foreign children, pair binding
mismatches, strict raw/fixture readback, returned-copy isolation and unequal-stream helper boundaries.
Selected children are freshly generated and validated through B2. Full 134-source B2 native evidence
remains accepted predecessor evidence; this packet does not claim to repeat it.
