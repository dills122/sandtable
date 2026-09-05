# BRK-TASK-002 independent review

Review instance: 1 of 3. Production slice only; prior research/design review closed.

## Preliminary Blind Ledger

Recorded before opening `author-explanation-1.md`. CCE retrieval incidentally returned its title and intent paragraph; no approach or rationale from that packet was read before this ledger. Repository research evidence was inspected as part of scope reconstruction.

- Verified branch `codex/breakdown-adjudication-design`, HEAD/base `78c9b0d61782ddab5b740d6bc260c338ae213f78`, and all seven frozen production SHA-256 values. Dirty user context files excluded; shared status documents remain parent-owned.
- Inspected canonical REQ-001/006, Rules wire delta, Task 002 ordering, both new test classes, three production files, predecessor band/weather logic and rational amount type.
- No confirmed defect found. Six exact fractions, ceiling arithmetic, one-point exception, 324 source cells, sequential-face validation, no-roll precedence and canonical-byte rejection match their scoped requirements.
- Verify independently: research-to-golden table agreement, focused test execution, predecessor artifact preservation, strict codec behavior and reported gate evidence.
- Scope questions for reconciliation: scalar loss primitive versus later collection transaction; dormant ruling factories versus coupled manifest registration. Existing task ordering supports deferring transaction/state/RNG and activation, but claims must not present those acceptance criteria as complete.

## Findings

No actionable findings. No P0–P3 defects identified in frozen Task 002 production slice.

`Cna1979BreakdownAdjudication.CalculateLoss` uses bounded exact integer multiplication and quotient/remainder ceiling; `int.MaxValue` remains safe. `EvaluateEligibility` validates inputs before no-roll selection, independently checks exact raw BP, and compares effective band indices. Rainstorm normalization preserves predecessor route semantics without introducing a weather shift. All authority collections reachable from the canonical definition are read-only; record copies cannot replace singleton authority. Codec equality against this closed definition rejects altered semantics and noncanonical bytes.

## Plan Review

Task 002 covers REQ-001 arithmetic/table authority and REQ-006 pure eligibility, with scoped AC-001/003/004 evidence. AC-010/011 evidence is an artifact foundation, not campaign replay or activation proof. Parent evidence document states this distinction explicitly.

Task order remains coherent: typed stop/lot contracts in 003, movement accounting in 004, collection transaction/RNG/state mutation in 005, coupled public activation in 006, Runner evidence in 007. Scalar `CalculateLoss` is a reusable per-unit arithmetic primitive with no singleton cohort storage or admission assumption; collection-based transaction remains required in 005. Deferring full Ruleset 9 registration until sequence 4 and dependent contracts exist avoids a mixed identity set. Four ruling factories match frozen IDs, alternatives, acceptance IDs and source references.

No migration or rollback action is needed for dormant internal code. Existing activation rollback boundary remains whole-version-set reversion. Shared closeout documents and checklist were still being updated by parent; checked-in completion should record actual final gate/review evidence before commit. No scope expansion or heavy pivot needed.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| All 324 cells and six accepted fractions implemented | `CreateDefinition`, expanded table test, independent Python comparison against research bounds | Confirmed | Source-to-production representation agrees |
| Loss calculation exact, including one-point 10% and large counts | `CalculateLoss`, 618 count/label combinations with `BigInteger` ceiling inequalities, explicit examples | Confirmed | No overflow or rounding defect found |
| Ordered no-roll reasoning validates malformed inputs first | `EvaluateEligibility`, predecessor `SelectEffectiveCheckBand`/point-share validation, focused boundary tests | Confirmed | Invalid inputs cannot become successful no-roll results |
| Strict schema 2 preserves historical continuity and rejects forged authority | Codec, golden comparison, 13 semantic mutation cases, duplicate/whitespace/cross-version tests | Confirmed | Closed canonical comparison is sufficient for this fixed authority |
| No current activation, randomness or World dependency | Source reference search, unchanged active manifest/artifact hash assertions, freeze audit | Confirmed | Existing campaign path remains dormant with respect to new code |
| Golden hash `sha256:0459f4a9ebb4ece6237f789000d3ec4fcddbf7ee3d341012d81ce77fb6ef3e08` | Independently hashed golden without terminal newline; focused C# equality test | Confirmed | Canonical bytes and reported identity agree |
| 72 focused tests pass | Independent reviewer execution below | Confirmed | All selected tests executed, none skipped |
| Earlier 18 RED failures and full restore/build/format/48-boundary/1288-test gate | Parent handoff and `.planning/2026-09-05-brk-task-002-outcome-rules/progress.md` | Unverified independently | Reported evidence retained; full gate not repeated unnecessarily |

## Verification Performed

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`: expected branch/base and explicit uncommitted scope; excluded user-owned files preserved.
- Python SHA-256 audit of all seven entries in `review-source-hashes.json`: all matched.
- `dotnet --version`: `10.0.400`; `global.json`, test project and central properties/packages confirm SDK-style native MTP/xUnit runner.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Rules.BreakdownOutcome*'`: passed, 72 total, 72 succeeded, 0 failed, 0 skipped. Escalated local IPC execution; no source mutation or rebuild.
- `python3 docs/research/verify-breakdown-outcomes.py`: passed 324 cells, unique complete ranges, monotonic columns, 9 probes, 606 research arithmetic combinations; retained transcription hash matched.
- `python3 docs/research/verify-breakdown-contract-freeze.py`: passed 14 historical fixture hashes, 15 Reaction children, 15 predecessor schema hashes, 10 requirements/12 acceptance mappings and 25 links. Specification audit only.
- Read-only Python comparison of research bounds to v2 golden: all 324 `(band, coordinate, label)` triples, six fraction mappings and predecessor continuity fields matched; canonical hash confirmed.
- `git diff --check`: passed.

## Open Questions And Residual Risks

No blocking open questions. Primary source chart review belongs to closed research/design phase and was not reopened; both production/golden derive from that reviewed transcription. Current focused tests prove pure Rules behavior, not future stop batching, actual RNG cursor replay, conservation, disclosure or identity activation. Those remain explicit later-task obligations. Canonical serialization allocates fixed-size buffers on each call; modest cost is acceptable for dormant fixed tables and warrants profiling only if later use becomes hot.

## Verdict

**Ready** for dormant BRK-TASK-002 slice. No approval for public Breakdown activation implied.

## Recommended Next Actions

Parent should record review reconciliation, finish status/evidence synchronization and commit scoped changes on feature branch. Continue ordered Task 003 when authorized; no additional review instance needed absent material source changes.
