# BRK-TASK-002 — Dormant Breakdown outcome rules

**Status:** Complete; full gate passed and independent review Ready.
**Branch:** `codex/breakdown-adjudication-design`. **Base:** `78c9b0d`. **Implementation commit:** `4d3acaf`.

[Task 001](../specs/breakdown-adjudication-v1.md) now has a dormant Rules implementation for chart
outcomes, accepted exact fractions, single-check-unit loss arithmetic and check eligibility. No public
campaign path calls it. Ruleset 8, Breakdown schema 1, sequence 3 and all existing fixture hashes
remain active and unchanged.

## Implementation and traceability

| Scope | Implementation / proof | Status |
| --- | --- | --- |
| BRK-AC-001 / REQ-001 | `Cna1979BreakdownAdjudication` expands 36 sequential coordinates across nine bands. Tests compare all 324 cells to the source-locked expanded table and reject invalid coordinates/bands. | Rules portion implemented |
| BRK-AC-003 / REQ-006 | Pure eligibility validates all inputs, then applies ordered working/raw-threshold/effective-band/checked-memory conditions. Rainstorm is neutral at check time; prior route transformation remains Task 004. | Rules portion implemented; no event/RNG proof yet |
| BRK-AC-004 / REQ-001 | Exact fractions, ceiling losses, one-working-point/10% exception, explicit source/ruling identity. Tests cover 618 count/label combinations including `int.MaxValue` plus source examples. | Arithmetic implemented; checked-memory mutation and zero-loss RNG remain Task 005 |
| BRK-AC-010/011 foundations | Strict schema 2 artifact codec/golden, semantic mutations, duplicate/missing cells, unknown fields, noncanonical fractions and cross-version rejection. | Rules artifact only; campaign/Runner admission remains later work |
| DEC-004–007 provenance | Four dormant ruling factories retain selected/rejected alternatives, source references and acceptance IDs. | Prepared, not registered with active manifest |

Single-unit loss calculation is a reusable primitive, with no global cohort slot or campaign-specific
size assumption. The collection-based stop/check transaction remains Task 005, after Task 003 freezes
its typed runtime inputs. No grouping/allocation, working-point mutation, lots, stops, RNG draws,
Content 6, sequence 4, Observation 7 or activation is implemented in this slice.

The successor artifact inherits exact validated schema 1 continuity fields, inserts `outcomeFractions`
and `outcomeCells` before `sources`, and emits schema 2. Its strict reader admits only the exact closed
authority bytes and returns the immutable canonical definition; it does not need to parse arbitrary
JSON into a permissive intermediate model. Old codec behavior is untouched.

Production C# stores normalized chart bounds; it never reads the research JSON or Python checker.
The new golden was assembled separately from the frozen schema 1 golden and the source-locked
research table, then compared byte-for-byte to C# output. These share the reviewed source transcription;
they are not two independent source readings. New canonical artifact hash:
`sha256:0459f4a9ebb4ece6237f789000d3ec4fcddbf7ee3d341012d81ce77fb6ef3e08`.

A complete Ruleset 9 hash is deliberately not published yet: its sequence 4 artifact belongs to
Task 003. Final coupled manifest registration and current-identity switch remain Task 006. This
avoids creating a purported current successor manifest containing the predecessor sequence.

## Verification

- RED: 18 focused tests failed against unimplemented arithmetic/lookup/eligibility stubs.
- GREEN: 72 outcome/artifact tests passed. Full gate rebuilt the final implementation: 1,288 solution
  tests and 48 explicit user-space boundary tests passed, zero failed/skipped; build and format passed.
- Gate commands (equivalent to `just check`, with unique MSBuild logs):

  ```bash
  dotnet restore Sandtable.slnx '-bl:artifacts/binlogs/brk-002-restore-{}.binlog'
  dotnet build Sandtable.slnx --no-restore '-bl:artifacts/binlogs/brk-002-build-{}.binlog'
  dotnet format Sandtable.slnx --verify-no-changes --no-restore
  dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'
  dotnet test --solution Sandtable.slnx --no-build
  ```

- Focused command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Rules.BreakdownOutcome*'`.
- Source research hash remains `f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401`.
- Active ruleset hash remains `0e80a8ba917113b401ea709f9f2a6cd7fb7cfec03b8adbdae978f1b219e141e0`;
  active Breakdown artifact remains `sha256:c7061325838dfcdd2f2388be3c6f6ec998bfa96df14b4cd6e733dd1c5d16c747`.

Native .NET tests and formatting require a local IPC socket, blocked in the sandbox; authorized
escalated local runs were used. One early public test signature exposed an internal enum and one
build hit CA1861 array-style checks; both corrected before green. An initial wildcard selected zero
tests and was replaced by the exact supported class-prefix filter. No skipped tests or hidden failures.
Build logs retained under `artifacts/binlogs/brk-002-*`.

**Next:** BRK-TASK-003 — dormant World/Snapshot/creation/lot/stop contracts and certified Truck fixture.

## Independent review reconciliation

[Review 1](../reviews/brk-task-002-review-1.md), instance 1 of 3 for this production slice, returned
**Ready** with no actionable findings. It independently ran all 72 focused tests, compared all 324
cells and six fractions to the retained source surface, checked canonical hashes and confirmed the
seven frozen production files. Parent executed the full 1,288-test/48-boundary gate; reviewer correctly
retains that as parent-reported evidence rather than claiming a second full run.

No findings require a fix or deferral. Review confirmed scalar loss arithmetic as the per-unit
primitive for Task 005's later collection transaction, and deferred complete Ruleset 9 registration
until the coupled sequence/contracts exist. No additional review instance needed; prior research/design
budget remains closed. Source bytes in implementation commit `4d3acaf` match the reviewed hashes.

Doc checks: numeric research and frozen fixture/schema audits pass; local link and Git diff checks
pass. Existing fixture bytes, active ruleset and user-owned changes remain preserved.
