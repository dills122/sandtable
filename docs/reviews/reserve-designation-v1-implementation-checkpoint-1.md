# Reserve Designation v1 — Implementation Checkpoint Review 1

**Review instance:** 1 of 3 for the implementation flow
**Target:** complete dirty working-tree delta at `ca3e3385e26551407b09ff17c0629ed7b7a7eaa3`
through `RES-TASK-007A`
**Reviewer mode:** fresh-context, blind-first, read-only
**Verdict:** **Not ready for Task 008**

The earlier planning review completed its separate three-instance limit. This is the first review
of the implementation flow.

## Findings

### RR1-001 — P1 — Task 008 is impossible under its frozen ownership

`RES-TASK-008` requires same-position designation replay and bounded layered validation, but its
exact five-file ownership excludes `CampaignSnapshotValidator.cs`. The current validator rejects
state versions above 10 and maps the admitted versions directly to the first ten sequence
positions. A designation successor at state 11, still at Reserve, therefore cannot pass validation,
projection, or replay. Task 009 owns validator changes one task too late.

**Author disposition:** **Accept.** The intended Task 008/009 split is not executable as written.
Restructure the ownership/dependency boundary before Task 008 begins.

### RR1-002 — P1 — Snapshot v7 context-free decoding accepts unreachable Reserve statuses

`CampaignSnapshotValidator.IsLocallyValid` does not inspect element Reserve status, while snapshot
deserialization admits a parsed checkpoint solely through that local validation layer. A canonical
snapshot at a currently admitted state 1-10 can therefore contain `ReserveI` or `ReserveII` and pass
context-free deserialization. Context-authoritative validation still rejects it through initial
world validation, so this is not an authority-handle or fog leak; it is a strict typed-contract
defect contradicting the completed validation-lane claim.

**Author disposition:** **Accept.** Add snapshot-level strict-decoder tests and enforce current
checkpoint status invariants locally before continuing.

### RR1-003 — P2 — Canonical documentation is contradictory and stale

The Reserve specification says production implementation “begins while” a P0/P1 remains
unresolved, which inverts the intended gate. `tech-design.md` still describes Reserve as
approval-gated and unimplemented despite owner approval and implementation through Task 007A.
Deferring final closeout does not satisfy the repository requirement to keep current architectural
truth synchronized.

**Author disposition:** **Accept.** Correct the gate sentence and synchronize the current
implemented/deferred boundary in architectural and project-map documentation.

## Plan Review

The architecture remains directionally sound, but the task graph is not ready for authoritative
mutation. Task 008 needs validator work that it neither owns nor depends on, and the completed
identity/validation lane does not meet its frozen context-free decoder contract. Task 008 remains
blocked pending remediation.

## Author-Claim Reconciliation

| Author claim | Status | Consequence |
| --- | --- | --- |
| Frozen base/head and complete dirty scope are disclosed | Confirmed | Review scope was verifiable. |
| Core rules/world/observation/action checkpoint is green | Confirmed | Independent gates reproduced the counts. |
| No accepted Reserve event or mutation exists | Confirmed | Current checkpoint remains authority-safe. |
| Strict snapshot decoding enforces frozen status invariants | Contradicted | `RR1-002` sustained. |
| Task 008 can implement the next vertical under the approved graph | Contradicted | `RR1-001` sustained. |
| Canonical docs reflect current implementation status | Contradicted | `RR1-003` sustained. |
| Downstream Exercise/Maneuver v2 migration is deliberately deferred | Confirmed | No separate finding; full-solution green was not claimed. |

## Verification Performed

The reviewer independently ran:

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj` — **392/392 passed**;
- Rules namespace — **93/93 passed**;
- Observations namespace — **37/37 passed**;
- Actions namespace — **35/35 passed**;
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore` — passed after restoring the
  fresh review worktree's dependency/generated inputs;
- `git diff --check` — passed.

No source file was modified by the reviewer.

## Open Questions And Residual Risks

- Decide the corrected task split that gives the designation vertical the validator behavior it
  needs without violating the five-material-file cap.
- Strict local validation must cover all currently admitted checkpoints without prematurely
  claiming the later Reserve/Movement transition family.
- The complete dirty identity lane remains intentionally non-mergeable until downstream migration
  and final gates are complete.

## Recommended Next Actions

1. Keep Task 008 blocked.
2. Add snapshot-deserialization tests proving pre-Reserve `ReserveI` and `ReserveII` reject.
3. Correct local checkpoint/status validation.
4. Restructure Tasks 008/009 ownership/dependencies so designation owns or depends on the required
   validator changes within the file cap.
5. Correct and synchronize the affected specification/architecture/project-map language.
6. Rerun focused, complete Core, format, and diff gates.
7. Submit the materially corrected checkpoint for implementation review instance 2 of 3.
